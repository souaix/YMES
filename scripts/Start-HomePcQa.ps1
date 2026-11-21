[CmdletBinding()]
param(
    [string]$PpkPath = (Join-Path $env:USERPROFILE 'Desktop\DigitalOcean.ppk')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$plinkPath = 'D:\software\PuTTY\plink.exe'
$nodePath = (Get-Command 'node.exe' -ErrorAction Stop).Source
$apiDll = Join-Path $repoRoot 'src\YMes.Api\bin\Debug\net9.0\YMes.Api.dll'
$apiProjectDll = Join-Path $repoRoot 'src\YMes.Api\bin\Debug\net9.0\Npgsql.dll'
$viteEntry = Join-Path $repoRoot 'web\node_modules\vite\bin\vite.js'
$logDir = Join-Path $env:TEMP 'ymes-local-release-qa'

foreach ($requiredPath in @($plinkPath, $PpkPath, $apiDll, $apiProjectDll, $viteEntry)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required home PC QA file is missing: $requiredPath"
    }
}

New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$nullInputFile = Join-Path $logDir 'empty.stdin'
if (-not (Test-Path -LiteralPath $nullInputFile -PathType Leaf)) {
    New-Item -ItemType File -Path $nullInputFile | Out-Null
}

function Get-ListeningProcessId {
    param([Parameter(Mandatory)][int]$Port)

    $pidFound = netstat -ano | ForEach-Object {
        if ($_ -match "^\s*TCP\s+\S+:$Port\s+\S+\s+LISTENING\s+(?<pid>\d+)\s*$") {
            [int]$Matches['pid']
        }
    } | Select-Object -First 1

    if ($null -eq $pidFound) { return $null }
    return [int]$pidFound
}

function Get-HttpStatus {
    param([Parameter(Mandatory)][string]$Uri)

    $response = & curl.exe --noproxy '*' -sS -o NUL -w '%{http_code}' `
        --connect-timeout 2 --max-time 8 $Uri 2>$null
    if ($LASTEXITCODE -ne 0) { return 0 }

    $status = 0
    if ([int]::TryParse(($response -join '').Trim(), [ref]$status)) { return $status }
    return 0
}

function Wait-ForListener {
    param([Parameter(Mandatory)][int]$Port, [int]$TimeoutSeconds = 15)

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $pidFound = Get-ListeningProcessId -Port $Port
        if ($pidFound) { return $pidFound }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    return $null
}

function Wait-ForHttp {
    param([Parameter(Mandatory)][string]$Uri, [int]$TimeoutSeconds = 20)

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $status = Get-HttpStatus -Uri $Uri
        if ($status -eq 200) { return $status }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    return $status
}

# Reuse the tunnel only when the listener is PuTTY. Never stop an unknown listener.
$tunnelPid = Get-ListeningProcessId -Port 15432
if ($tunnelPid) {
    $tunnelProcess = Get-Process -Id $tunnelPid -ErrorAction SilentlyContinue
    if ($tunnelProcess.ProcessName -ne 'plink') {
        throw "Port 15432 is occupied by PID $tunnelPid ($($tunnelProcess.ProcessName)); no process was stopped."
    }
} else {
    $tunnelProcess = Start-Process -FilePath $plinkPath `
        -ArgumentList @(
            '-batch', '-i', $PpkPath, '-N', '-L',
            '127.0.0.1:15432:127.0.0.1:5432', 'root@152.42.195.128'
        ) `
        -WindowStyle Hidden `
        -RedirectStandardInput $nullInputFile `
        -RedirectStandardOutput (Join-Path $logDir 'pg-tunnel.log') `
        -RedirectStandardError (Join-Path $logDir 'pg-tunnel.err') `
        -PassThru
    $tunnelPid = Wait-ForListener -Port 15432
    if (-not $tunnelPid) {
        throw "SSH tunnel did not open on 15432. Review $logDir\pg-tunnel.err"
    }
}

# If the API is already bound, let it reconnect through the tunnel and inspect readiness.
$apiPid = Get-ListeningProcessId -Port 5080
if (-not $apiPid) {
    $connectionOutput = & $plinkPath -batch -i $PpkPath -ssh 'root@152.42.195.128' `
        "sed -n '/^YMES_CONNECTION_STRING=/p' /opt/ymes/source/deploy/.env" 2>$null
    $connectionLine = @($connectionOutput | Where-Object { $_ -match '^YMES_CONNECTION_STRING=' }) |
        Select-Object -First 1
    if (-not $connectionLine) { throw 'Could not read the home PostgreSQL connection setting over SSH.' }

    $connectionText = $connectionLine.Substring($connectionLine.IndexOf('=') + 1).Trim().Trim('"').Trim("'")
    Add-Type -Path $apiProjectDll
    $connectionBuilder = [Npgsql.NpgsqlConnectionStringBuilder]::new($connectionText)
    $connectionBuilder.Host = '127.0.0.1'
    $connectionBuilder.Port = 15432
    # SSH provides the authenticated encrypted transport for this local PostgreSQL hop.
    $connectionBuilder.SslMode = [Npgsql.SslMode]::Disable
    $connectionBuilder.TrustServerCertificate = $false

    $apiEnvironment = @{
        ASPNETCORE_ENVIRONMENT = 'Development'
        ASPNETCORE_URLS = 'http://127.0.0.1:5080'
        YMES_LOCAL_DEMO_AUTH = 'false'
        YMES_MASTER_DATA_PROVIDER = 'postgres'
        YMES_ENVIRONMENT = 'development'
        YMES_ALLOWED_ORIGINS = 'http://127.0.0.1:18749;http://localhost:18749'
        ConnectionStrings__YMes = $connectionBuilder.ConnectionString
    }
    $apiProcess = Start-Process -FilePath (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe') `
        -ArgumentList @($apiDll, '--urls', 'http://127.0.0.1:5080') `
        -WorkingDirectory $repoRoot `
        -WindowStyle Hidden `
        -Environment $apiEnvironment `
        -RedirectStandardInput $nullInputFile `
        -RedirectStandardOutput (Join-Path $logDir 'api.stdout.log') `
        -RedirectStandardError (Join-Path $logDir 'api.stderr.log') `
        -PassThru
    $apiPid = Wait-ForListener -Port 5080
    if (-not $apiPid) { throw "YMES API did not open 5080. Review $logDir\api.stderr.log" }
}

$apiHealthUri = 'http://127.0.0.1:5080/health/ready'
$apiHealth = Wait-ForHttp -Uri $apiHealthUri
if ($apiHealth -ne 200) {
    throw "YMES API PID $apiPid is listening but readiness is HTTP $apiHealth. The script did not stop it."
}
$apiBody = (& curl.exe --noproxy '*' -sS --max-time 5 $apiHealthUri 2>$null) -join ''
$apiHealthData = try { $apiBody | ConvertFrom-Json } catch { $null }
if ($apiHealthData.mode -eq 'local-demo') {
    throw "YMES API PID $apiPid is using local-demo auth and cannot serve the PostgreSQL-backed menu."
}

# Reuse the existing Vite process; start it only if the expected local port is free.
$webPid = Get-ListeningProcessId -Port 18749
if (-not $webPid) {
    $webProcess = Start-Process -FilePath $nodePath `
        -ArgumentList @($viteEntry, '--host', '127.0.0.1', '--port', '18749', '--strictPort') `
        -WorkingDirectory (Join-Path $repoRoot 'web') `
        -WindowStyle Hidden `
        -RedirectStandardInput $nullInputFile `
        -RedirectStandardOutput (Join-Path $logDir 'vite.stdout.log') `
        -RedirectStandardError (Join-Path $logDir 'vite.stderr.log') `
        -PassThru
    $webPid = Wait-ForListener -Port 18749
    if (-not $webPid) { throw "YMES Vite did not open 18749. Review $logDir\vite.stderr.log" }
}

$webHealth = Wait-ForHttp -Uri 'http://127.0.0.1:18749/YMES/'
$proxyHealth = Wait-ForHttp -Uri 'http://127.0.0.1:18749/YMES/API/health/ready'
if ($webHealth -ne 200 -or $proxyHealth -ne 200) {
    throw "YMES local readiness failed: web=$webHealth, API proxy=$proxyHealth. No unknown process was stopped."
}

[pscustomobject]@{
    DatabaseTunnelPid = $tunnelPid
    ApiPid = $apiPid
    ApiReadiness = $apiHealth
    VitePid = $webPid
    WebReadiness = $webHealth
    WebApiProxyReadiness = $proxyHealth
    Url = 'http://127.0.0.1:18749/YMES/'
}

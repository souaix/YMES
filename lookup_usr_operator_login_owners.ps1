$ErrorActionPreference = 'Stop'
$profilePath = 'C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt'
$profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
$connectionString = [string]$profile.DatabaseConfigs.CS.Test.Mes.ConnectionString
$assemblyPath = 'D:\iMES\CSiMES\STD\MES_S_DLL\Oracle.ManagedDataAccess.dll'
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
$connection = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($connectionString)
$names = @('TBLUSROPERATORLOGINSTATE', 'TBLUSROPERATORLOGINLOG')
try {
    $connection.Open()
    foreach ($name in $names) {
        $command = $connection.CreateCommand()
        $command.BindByName = $true
        $command.CommandText = 'SELECT OWNER, TABLE_NAME FROM ALL_TABLES WHERE TABLE_NAME=:name ORDER BY OWNER'
        [void]$command.Parameters.Add('name', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['name'].Value = $name
        $reader = $command.ExecuteReader()
        while ($reader.Read()) { Write-Output ('TABLE|' + $reader.GetString(0) + '|' + $reader.GetString(1)) }
        $reader.Close(); $command.Dispose()

        $command = $connection.CreateCommand()
        $command.BindByName = $true
        $command.CommandText = 'SELECT OWNER, SYNONYM_NAME, TABLE_OWNER, TABLE_NAME, NVL(DB_LINK, '''') FROM ALL_SYNONYMS WHERE SYNONYM_NAME=:name ORDER BY OWNER'
        [void]$command.Parameters.Add('name', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['name'].Value = $name
        $reader = $command.ExecuteReader()
        while ($reader.Read()) { Write-Output ('SYNONYM|' + $reader.GetString(0) + '|' + $reader.GetString(1) + '|' + $reader.GetString(2) + '|' + $reader.GetString(3) + '|' + $reader.GetString(4)) }
        $reader.Close(); $command.Dispose()
    }
}
catch {
    Write-Output 'Oracle dictionary lookup failed; exception details suppressed to protect connection settings.'
    exit 2
}
finally {
    if ($connection.State -eq [System.Data.ConnectionState]::Open) { $connection.Close() }
    $connection.Dispose()
}

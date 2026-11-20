$ErrorActionPreference = 'Stop'
$profilePath = 'C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt'
$profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
$connectionString = [string]$profile.DatabaseConfigs.CS.Test.Mes.ConnectionString
$assemblyPath = 'D:\iMES\CSiMES\STD\MES_S_DLL\Oracle.ManagedDataAccess.dll'
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
$connection = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($connectionString)
$tables = @('TBLUSROPERATORLOGINSTATE', 'TBLUSROPERATORLOGINLOG')
$outputDirectory = Join-Path $PSScriptRoot 'database\reference'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$ddlLines = New-Object System.Collections.Generic.List[string]
$indexLines = New-Object System.Collections.Generic.List[string]
$metadataLines = New-Object System.Collections.Generic.List[string]

function New-BoundCommand($Connection, [string]$Sql, [string]$Name, [string]$Value) {
    $command = $Connection.CreateCommand()
    $command.BindByName = $true
    $command.CommandText = $Sql
    $parameter = $command.Parameters.Add($Name, [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
    $parameter.Value = $Value
    return $command
}

try {
    $connection.Open()
    foreach ($tableName in $tables) {
        $metadataLines.Add("TABLE|$tableName")
        $command = New-BoundCommand $connection 'SELECT COUNT(*) FROM ALL_TABLES WHERE OWNER = ''MESTEST'' AND TABLE_NAME = :tableName' 'tableName' $tableName
        $exists = [int]$command.ExecuteScalar()
        $command.Dispose()
        if ($exists -eq 0) {
            $ddlLines.Add("-- TABLE NOT FOUND: MESTEST.$tableName")
            $metadataLines.Add('SUMMARY|table-not-found')
            continue
        }

        $command = New-BoundCommand $connection 'SELECT DBMS_METADATA.GET_DDL(''TABLE'', :tableName, ''MESTEST'') FROM DUAL' 'tableName' $tableName
        $ddlLines.Add("-- MESTEST.$tableName")
        $ddlLines.Add([string]$command.ExecuteScalar())
        $command.Dispose()

        $command = New-BoundCommand $connection 'SELECT COLUMN_NAME, DATA_TYPE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE, NULLABLE, DATA_DEFAULT FROM ALL_TAB_COLUMNS WHERE OWNER=''MESTEST'' AND TABLE_NAME=:tableName ORDER BY COLUMN_ID' 'tableName' $tableName
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 7; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('COLUMN|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose()

        $command = New-BoundCommand $connection 'SELECT C.CONSTRAINT_NAME,C.CONSTRAINT_TYPE,C.STATUS,CC.COLUMN_NAME,CC.POSITION FROM ALL_CONSTRAINTS C LEFT JOIN ALL_CONS_COLUMNS CC ON CC.OWNER=C.OWNER AND CC.CONSTRAINT_NAME=C.CONSTRAINT_NAME AND CC.TABLE_NAME=C.TABLE_NAME WHERE C.OWNER=''MESTEST'' AND C.TABLE_NAME=:tableName ORDER BY C.CONSTRAINT_NAME,CC.POSITION' 'tableName' $tableName
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 5; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('CONSTRAINT|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose()

        $command = New-BoundCommand $connection 'SELECT I.INDEX_NAME,I.UNIQUENESS,I.STATUS,IC.COLUMN_NAME,IC.COLUMN_POSITION FROM ALL_INDEXES I LEFT JOIN ALL_IND_COLUMNS IC ON IC.INDEX_OWNER=I.OWNER AND IC.INDEX_NAME=I.INDEX_NAME AND IC.TABLE_NAME=I.TABLE_NAME WHERE I.OWNER=''MESTEST'' AND I.TABLE_NAME=:tableName ORDER BY I.INDEX_NAME,IC.COLUMN_POSITION' 'tableName' $tableName
        $reader = $command.ExecuteReader()
        $indexNames = New-Object System.Collections.Generic.List[string]
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 5; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('INDEX|' + ($values -join '|'))
            if (-not $reader.IsDBNull(0) -and -not $indexNames.Contains([string]$reader.GetValue(0))) { $indexNames.Add([string]$reader.GetValue(0)) }
        }
        $reader.Close(); $command.Dispose()
        foreach ($indexName in $indexNames) {
            $command = $connection.CreateCommand()
            $command.BindByName = $true
            $command.CommandText = 'SELECT DBMS_METADATA.GET_DDL(''INDEX'', :indexName, ''MESTEST'') FROM DUAL'
            $parameter = $command.Parameters.Add('indexName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
            $parameter.Value = $indexName
            $indexLines.Add("-- MESTEST.$indexName")
            $indexLines.Add([string]$command.ExecuteScalar())
            $command.Dispose()
        }

        $command = New-BoundCommand $connection 'SELECT TRIGGER_NAME,STATUS,TRIGGERING_EVENT FROM ALL_TRIGGERS WHERE OWNER=''MESTEST'' AND TABLE_NAME=:tableName ORDER BY TRIGGER_NAME' 'tableName' $tableName
        $reader = $command.ExecuteReader()
        $triggerCount = 0
        while ($reader.Read()) {
            $triggerCount++
            $values = for ($i = 0; $i -lt 3; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('TRIGGER|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose()
        $metadataLines.Add("SUMMARY|triggers=$triggerCount")
    }
}
catch {
    Write-Output 'Oracle metadata query failed; exception details suppressed to protect connection settings.'
    exit 2
}
finally {
    if ($connection.State -eq [System.Data.ConnectionState]::Open) { $connection.Close() }
    $connection.Dispose()
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_usr_operator_login_source_ddl.sql'), $ddlLines, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_usr_operator_login_source_indexes.sql'), $indexLines, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_usr_operator_login_metadata.txt'), $metadataLines, $utf8)
Write-Output 'Oracle structural DDL and dictionary metadata extracted for the requested user operator login tables; no business rows were queried.'

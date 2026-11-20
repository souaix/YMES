$ErrorActionPreference = 'Stop'
$profilePath = 'C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt'
$profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
$connectionString = [string]$profile.DatabaseConfigs.CS.Test.Mes.ConnectionString
$assemblyPath = 'D:\iMES\CSiMES\STD\MES_S_DLL\Oracle.ManagedDataAccess.dll'
[void][Reflection.Assembly]::LoadFrom($assemblyPath)
$connection = New-Object Oracle.ManagedDataAccess.Client.OracleConnection($connectionString)
$tables = @('TBLEMSDISPATCHSTATE', 'TBLWIPAUTOMERGE', 'TBLWIPCONT_EMPLOYEE', 'TBLWIPLOTOPECNRECORD', 'TBLWIPOSBASIS', 'TBLWIPOSDETAIL')
$outputDirectory = Join-Path $PSScriptRoot 'database\reference'
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
        $command = New-BoundCommand $connection 'SELECT OWNER FROM ALL_TABLES WHERE TABLE_NAME=:tableName ORDER BY CASE WHEN OWNER=''MESTEST'' THEN 0 ELSE 1 END, OWNER' 'tableName' $tableName
        $reader = $command.ExecuteReader()
        $owners = New-Object System.Collections.Generic.List[string]
        while ($reader.Read()) { $owners.Add([string]$reader.GetValue(0)) }
        $reader.Close(); $command.Dispose()
        if ($owners.Count -eq 0) {
            $metadataLines.Add('SUMMARY|table-not-found-in-visible-owners')
            $command = New-BoundCommand $connection 'SELECT OWNER,TABLE_OWNER,DB_LINK FROM ALL_SYNONYMS WHERE SYNONYM_NAME=:tableName ORDER BY CASE WHEN OWNER=''MESTEST'' THEN 0 ELSE 1 END, OWNER' 'tableName' $tableName
            $reader = $command.ExecuteReader()
            while ($reader.Read()) {
                $values = for ($i = 0; $i -lt 3; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
                $metadataLines.Add('SYNONYM|' + ($values -join '|'))
            }
            $reader.Close(); $command.Dispose()
            $ddlLines.Add("-- TABLE NOT FOUND IN VISIBLE OWNERS: $tableName")
            continue
        }

        $owner = $owners[0]
        $metadataLines.Add("OWNER|$owner")
        foreach ($visibleOwner in $owners) { $metadataLines.Add("VISIBLE_OWNER|$visibleOwner") }
        $command = $connection.CreateCommand()
        $command.BindByName = $true
        $command.CommandText = 'SELECT DBMS_METADATA.GET_DDL(''TABLE'', :tableName, :ownerName) FROM DUAL'
        [void]$command.Parameters.Add('tableName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['tableName'].Value = $tableName; $command.Parameters['ownerName'].Value = $owner
        $ddlLines.Add("-- $owner.$tableName")
        $ddlLines.Add([string]$command.ExecuteScalar())
        $command.Dispose()

        $command = $connection.CreateCommand(); $command.BindByName = $true
        $command.CommandText = 'SELECT COLUMN_NAME,DATA_TYPE,DATA_LENGTH,DATA_PRECISION,DATA_SCALE,NULLABLE,DATA_DEFAULT FROM ALL_TAB_COLUMNS WHERE OWNER=:ownerName AND TABLE_NAME=:tableName ORDER BY COLUMN_ID'
        [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        [void]$command.Parameters.Add('tableName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['ownerName'].Value = $owner; $command.Parameters['tableName'].Value = $tableName
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 7; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('COLUMN|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose()

        $command = $connection.CreateCommand(); $command.BindByName = $true
        $command.CommandText = 'SELECT C.CONSTRAINT_NAME,C.CONSTRAINT_TYPE,C.STATUS,CC.COLUMN_NAME,CC.POSITION FROM ALL_CONSTRAINTS C LEFT JOIN ALL_CONS_COLUMNS CC ON CC.OWNER=C.OWNER AND CC.CONSTRAINT_NAME=C.CONSTRAINT_NAME AND CC.TABLE_NAME=C.TABLE_NAME WHERE C.OWNER=:ownerName AND C.TABLE_NAME=:tableName ORDER BY C.CONSTRAINT_NAME,CC.POSITION'
        [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        [void]$command.Parameters.Add('tableName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['ownerName'].Value = $owner; $command.Parameters['tableName'].Value = $tableName
        $reader = $command.ExecuteReader()
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 5; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('CONSTRAINT|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose()

        $command = $connection.CreateCommand(); $command.BindByName = $true
        $command.CommandText = 'SELECT I.INDEX_NAME,I.UNIQUENESS,I.STATUS,IC.COLUMN_NAME,IC.COLUMN_POSITION FROM ALL_INDEXES I LEFT JOIN ALL_IND_COLUMNS IC ON IC.INDEX_OWNER=I.OWNER AND IC.INDEX_NAME=I.INDEX_NAME AND IC.TABLE_NAME=I.TABLE_NAME WHERE I.OWNER=:ownerName AND I.TABLE_NAME=:tableName ORDER BY I.INDEX_NAME,IC.COLUMN_POSITION'
        [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        [void]$command.Parameters.Add('tableName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['ownerName'].Value = $owner; $command.Parameters['tableName'].Value = $tableName
        $reader = $command.ExecuteReader()
        $indexNames = New-Object System.Collections.Generic.List[string]
        while ($reader.Read()) {
            $values = for ($i = 0; $i -lt 5; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('INDEX|' + ($values -join '|'))
            if (-not $reader.IsDBNull(0) -and -not $indexNames.Contains([string]$reader.GetValue(0))) { $indexNames.Add([string]$reader.GetValue(0)) }
        }
        $reader.Close(); $command.Dispose()
        foreach ($indexName in $indexNames) {
            $command = $connection.CreateCommand(); $command.BindByName = $true
            $command.CommandText = 'SELECT DBMS_METADATA.GET_DDL(''INDEX'', :indexName, :ownerName) FROM DUAL'
            [void]$command.Parameters.Add('indexName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
            [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
            $command.Parameters['indexName'].Value = $indexName; $command.Parameters['ownerName'].Value = $owner
            $indexLines.Add("-- $owner.$indexName"); $indexLines.Add([string]$command.ExecuteScalar()); $command.Dispose()
        }

        $command = $connection.CreateCommand(); $command.BindByName = $true
        $command.CommandText = 'SELECT TRIGGER_NAME,STATUS,TRIGGERING_EVENT FROM ALL_TRIGGERS WHERE OWNER=:ownerName AND TABLE_NAME=:tableName ORDER BY TRIGGER_NAME'
        [void]$command.Parameters.Add('ownerName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        [void]$command.Parameters.Add('tableName', [Oracle.ManagedDataAccess.Client.OracleDbType]::Varchar2, 128)
        $command.Parameters['ownerName'].Value = $owner; $command.Parameters['tableName'].Value = $tableName
        $reader = $command.ExecuteReader(); $triggerCount = 0
        while ($reader.Read()) {
            $triggerCount++
            $values = for ($i = 0; $i -lt 3; $i++) { if ($reader.IsDBNull($i)) { '' } else { [string]$reader.GetValue($i) } }
            $metadataLines.Add('TRIGGER|' + ($values -join '|'))
        }
        $reader.Close(); $command.Dispose(); $metadataLines.Add("SUMMARY|triggers=$triggerCount")
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

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_wip_wp207_source_ddl.sql'), $ddlLines, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_wip_wp207_source_indexes.sql'), $indexLines, $utf8)
[System.IO.File]::WriteAllLines((Join-Path $outputDirectory 'mestest_wip_wp207_metadata.txt'), $metadataLines, $utf8)
Write-Output 'Oracle structural DDL and dictionary metadata extracted for WP207; no business rows were queried.'

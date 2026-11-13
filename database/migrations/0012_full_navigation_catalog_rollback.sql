-- Restores the pre-0012 values and removes only catalog rows first introduced by migration 0012.
BEGIN;

LOCK TABLE "YMES".tblsysmodule, "YMES".tblsysmenuitem, "YMES".tblsysfunction IN EXCLUSIVE MODE;

DELETE FROM "YMES".tblsysfunction AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysfunction'
  AND backup.row_data IS NULL
  AND target.functionno = backup.row_key::jsonb ->> 0;

DELETE FROM "YMES".tblsysmenuitem AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmenuitem'
  AND backup.row_data IS NULL
  AND target.moduleno = backup.row_key::jsonb ->> 0
  AND target.menuno = backup.row_key::jsonb ->> 1;

DELETE FROM "YMES".tblsysmodule AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmodule'
  AND backup.row_data IS NULL
  AND target.moduleno = backup.row_key::jsonb ->> 0;

DELETE FROM "YMES".tblsysfunction AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysfunction'
  AND backup.row_data IS NOT NULL
  AND target.functionno = backup.row_key::jsonb ->> 0;

INSERT INTO "YMES".tblsysfunction
SELECT (jsonb_populate_record(NULL::"YMES".tblsysfunction, backup.row_data)).*
FROM "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysfunction'
  AND backup.row_data IS NOT NULL;

DELETE FROM "YMES".tblsysmenuitem AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmenuitem'
  AND backup.row_data IS NOT NULL
  AND target.moduleno = backup.row_key::jsonb ->> 0
  AND target.menuno = backup.row_key::jsonb ->> 1;

INSERT INTO "YMES".tblsysmenuitem
SELECT (jsonb_populate_record(NULL::"YMES".tblsysmenuitem, backup.row_data)).*
FROM "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmenuitem'
  AND backup.row_data IS NOT NULL;

DELETE FROM "YMES".tblsysmodule AS target
USING "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmodule'
  AND backup.row_data IS NOT NULL
  AND target.moduleno = backup.row_key::jsonb ->> 0;

INSERT INTO "YMES".tblsysmodule
SELECT (jsonb_populate_record(NULL::"YMES".tblsysmodule, backup.row_data)).*
FROM "YMES".ym_migration_0012_navigation_backup AS backup
WHERE backup.table_name = 'tblsysmodule'
  AND backup.row_data IS NOT NULL;

DROP TABLE "YMES".ym_migration_0012_navigation_backup;

COMMIT;
-- Rollback only if no inspection history/detail rows have been imported.
BEGIN;

DO $rollback_guard$
BEGIN
    IF to_regclass('"YMES".tblwipeqpqclistlog') IS NULL
       OR to_regclass('"YMES".tblwipeqpqclistdetail') IS NULL THEN
        RAISE EXCEPTION 'WP306 source tables are incomplete; inspect manually before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipeqpqclistlog LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipeqpqclistdetail LIMIT 1) THEN
        RAISE EXCEPTION 'WP306 tables contain rows; refusing destructive rollback';
    END IF;
END
$rollback_guard$;

DROP INDEX "YMES".ix_wp306_qc_log_equipment_serial;
DROP INDEX "YMES".ix_wp306_qc_log_created_serial;
DROP TABLE "YMES".tblwipeqpqclistdetail;
DROP TABLE "YMES".tblwipeqpqclistlog;

COMMIT;

-- Rollback only when both newly created WP303 source tables are still empty.
BEGIN;

DO $rollback_guard$
BEGIN
    IF to_regclass('"YMES".tblwipeqpconsumestatus') IS NULL
       OR to_regclass('"YMES".tblwipeqpconsumelog') IS NULL THEN
        RAISE EXCEPTION 'WP303 source tables are incomplete; inspect manually before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipeqpconsumestatus LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipeqpconsumelog LIMIT 1) THEN
        RAISE EXCEPTION 'WP303 tables contain rows; refusing destructive rollback';
    END IF;
END
$rollback_guard$;

DROP INDEX "YMES".ix_wp304_consume_log_equipment_created;
DROP INDEX "YMES".ix_wp304_consume_log_consume_created;
DROP INDEX "YMES".ix_wp304_consume_log_created;
DROP TABLE "YMES".tblwipeqpconsumelog;
DROP TABLE "YMES".tblwipeqpconsumestatus;

COMMIT;

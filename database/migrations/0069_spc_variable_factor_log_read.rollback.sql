-- Rollback for migration 0069. Refuse to remove replicated business rows.
BEGIN;

DO $guard$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblspcvarfactorlog LIMIT 1) THEN
        RAISE EXCEPTION 'SPC factor-log data exists; refusing destructive rollback';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE "YMES".tblspcvarfactorlog FROM ymes_app;
DROP TABLE "YMES".tblspcvarfactorlog;

COMMIT;

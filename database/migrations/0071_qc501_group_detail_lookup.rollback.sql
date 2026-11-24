-- Rollback only if the SC501 lookup table has not accumulated data.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tblqcitemgroupdetail') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblqcitemgroupdetail is missing; refusing SC501 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblqcitemgroupdetail LIMIT 1) THEN
        RAISE EXCEPTION 'YMES.tblqcitemgroupdetail contains data; refusing destructive rollback';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE "YMES".tblqcitemgroupdetail FROM ymes_app;
DROP TABLE "YMES".tblqcitemgroupdetail;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwipcont_resource') IS NULL THEN
        RAISE EXCEPTION 'RT605 rollback stopped: YMES.tblwipcont_resource is missing';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipcont_resource LIMIT 1) THEN
        RAISE EXCEPTION 'RT605 rollback stopped because YMES.tblwipcont_resource contains data';
    END IF;
END
$$;

REVOKE SELECT ON "YMES".tblwipcont_resource FROM ymes_app;
DROP TABLE "YMES".tblwipcont_resource;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwipcont_material') IS NULL
       OR to_regclass('"YMES".tblwipcont_materiallot') IS NULL THEN
        RAISE EXCEPTION 'RT601/RT602 rollback stopped: one or more migration 0095 tables are missing';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipcont_material LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_materiallot LIMIT 1) THEN
        RAISE EXCEPTION 'RT601/RT602 rollback stopped because a table contains data';
    END IF;
END
$$;

REVOKE SELECT ON "YMES".tblwipcont_material, "YMES".tblwipcont_materiallot FROM ymes_app;
DROP TABLE "YMES".tblwipcont_material;
DROP TABLE "YMES".tblwipcont_materiallot;

COMMIT;

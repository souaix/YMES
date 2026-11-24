-- Rollback for migration 0070. Refuse to delete replicated MES business rows.
BEGIN;

DO $guard$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblwiperfbasis LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiperfbatchlot LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiplotbasis LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiplotstate LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblmtlmaterialvendor LIMIT 1) THEN
        RAISE EXCEPTION 'SC401 source data exists; refusing destructive rollback';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE
    "YMES".tblwiperfbasis,
    "YMES".tblwiperfbatchlot,
    "YMES".tblwiplotbasis,
    "YMES".tblwiplotstate,
    "YMES".tblmtlmaterialvendor
FROM ymes_app;

DROP TABLE "YMES".tblwiperfbasis;
DROP TABLE "YMES".tblwiperfbatchlot;
DROP TABLE "YMES".tblwiplotstate;
DROP TABLE "YMES".tblwiplotbasis;
DROP TABLE "YMES".tblmtlmaterialvendor;

COMMIT;

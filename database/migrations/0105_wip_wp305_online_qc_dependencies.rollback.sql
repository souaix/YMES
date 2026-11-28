-- Rollback only while the WP305 dependency tables contain no production rows.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tblwipeqpqctask') IS NULL
       OR to_regclass('"YMES".tbleqpqclistsetup') IS NULL
       OR to_regclass('"YMES".tblwiptemp_equipment') IS NULL THEN
        RAISE EXCEPTION 'WP305 dependency tables are incomplete; inspect manually before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipeqpqctask LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tbleqpqclistsetup LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiptemp_equipment LIMIT 1) THEN
        RAISE EXCEPTION 'WP305 rollback stopped because dependency table(s) contain rows';
    END IF;
END
$guard$;

REVOKE SELECT ON "YMES".tbleqpqclistbasis,
    "YMES".tbleqpqclistdetail,
    "YMES".tbleqpqclistsetup,
    "YMES".tblwipeqpqctask,
    "YMES".tblwiptemp_equipment FROM ymes_app;
DROP TABLE "YMES".tblwiptemp_equipment;
DROP TABLE "YMES".tbleqpqclistsetup;
DROP TABLE "YMES".tblwipeqpqctask;

COMMIT;

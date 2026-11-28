-- WP206 rollback is refused if any source or idempotency rows were written.
-- Export/archive business rows and obtain a reviewed recovery plan before considering a destructive rollback.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tblopabnormal') IS NULL
       OR to_regclass('"YMES".tblqcabnormalbasis') IS NULL
       OR to_regclass('"YMES".tblqcabnormalposition') IS NULL
       OR to_regclass('"YMES".tblwipcont_abnormal') IS NULL
       OR to_regclass('"YMES".ym_wp206_request') IS NULL THEN
        RAISE EXCEPTION 'One or more WP206 migration objects are missing; inspect before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblopabnormal)
       OR EXISTS (SELECT 1 FROM "YMES".tblqcabnormalbasis)
       OR EXISTS (SELECT 1 FROM "YMES".tblqcabnormalposition)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_abnormal)
       OR EXISTS (SELECT 1 FROM "YMES".ym_wp206_request) THEN
        RAISE EXCEPTION 'WP206 source or idempotency records exist; refusing data-destructive rollback';
    END IF;
END
$guard$;

REVOKE SELECT ON "YMES".tblopabnormal,
    "YMES".tblqcabnormalbasis,
    "YMES".tblqcabnormalposition FROM ymes_app;
REVOKE INSERT ON "YMES".tblwipcont_abnormal FROM ymes_app;
REVOKE SELECT, INSERT ON "YMES".ym_wp206_request FROM ymes_app;

DROP TABLE "YMES".ym_wp206_request;
DROP TABLE "YMES".tblwipcont_abnormal;
DROP TABLE "YMES".tblqcabnormalposition;
DROP TABLE "YMES".tblqcabnormalbasis;
DROP TABLE "YMES".tblopabnormal;

COMMIT;

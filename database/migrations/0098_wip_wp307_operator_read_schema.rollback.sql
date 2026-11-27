BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblsmdareashift') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblsmdareashift) THEN
        RAISE EXCEPTION 'WP307 rollback stopped: tblsmdareashift contains rows';
    END IF;
    IF to_regclass('"YMES".tblsmdareaoperator') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblsmdareaoperator) THEN
        RAISE EXCEPTION 'WP307 rollback stopped: tblsmdareaoperator contains rows';
    END IF;
    IF to_regclass('"YMES".tblwipareaoperatorstate') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblwipareaoperatorstate) THEN
        RAISE EXCEPTION 'WP307 rollback stopped: tblwipareaoperatorstate contains rows';
    END IF;
    IF to_regclass('"YMES".tblwipareaoperatorlog') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblwipareaoperatorlog) THEN
        RAISE EXCEPTION 'WP307 rollback stopped: tblwipareaoperatorlog contains rows';
    END IF;
END
$$;

REVOKE SELECT ON "YMES".tblsmdareashift,
                 "YMES".tblsmdareaoperator,
                 "YMES".tblwipareaoperatorstate,
                 "YMES".tblwipareaoperatorlog,
                 "YMES".tblusrshiftbasis FROM ymes_app;
DROP TABLE IF EXISTS "YMES".tblwipareaoperatorlog;
DROP TABLE IF EXISTS "YMES".tblwipareaoperatorstate;
DROP TABLE IF EXISTS "YMES".tblsmdareaoperator;
DROP TABLE IF EXISTS "YMES".tblsmdareashift;

COMMIT;

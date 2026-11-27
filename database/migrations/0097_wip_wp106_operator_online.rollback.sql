BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwipoperatorstate') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblwipoperatorstate) THEN
        RAISE EXCEPTION 'WP106 rollback stopped: tblwipoperatorstate contains rows';
    END IF;
    IF to_regclass('"YMES".tblwipoperatorlog') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblwipoperatorlog) THEN
        RAISE EXCEPTION 'WP106 rollback stopped: tblwipoperatorlog contains rows';
    END IF;
    IF to_regclass('"YMES".tblwiplotloginstate') IS NOT NULL
       AND EXISTS (SELECT 1 FROM "YMES".tblwiplotloginstate) THEN
        RAISE EXCEPTION 'WP106 rollback stopped: tblwiplotloginstate contains rows';
    END IF;
END
$$;

REVOKE ALL ON "YMES".tblwipoperatorstate,
              "YMES".tblwipoperatorlog,
              "YMES".tblwiplotloginstate FROM ymes_app;
DROP TABLE IF EXISTS "YMES".tblwiplotloginstate;
DROP TABLE IF EXISTS "YMES".tblwipoperatorlog;
DROP TABLE IF EXISTS "YMES".tblwipoperatorstate;

COMMIT;

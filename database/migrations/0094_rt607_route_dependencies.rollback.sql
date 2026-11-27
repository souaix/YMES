BEGIN;

DO $$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'tblwiplotprocess',
        'tblprsnoderelation',
        'tblprdopdescription',
        'tblprdruntimesetup',
        'tblprdproductprocess',
        'tblprdoparea',
        'tblwiplotoparea'
    ] LOOP
        IF to_regclass(format('"YMES".%I', table_name)) IS NULL THEN
            RAISE EXCEPTION 'RT607 rollback stopped: "YMES".% does not exist', table_name;
        END IF;
        IF EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE oid = to_regclass(format('"YMES".%I', table_name)) AND relkind <> 'r') THEN
            RAISE EXCEPTION 'RT607 rollback stopped: "YMES".% is not an ordinary table', table_name;
        END IF;
    END LOOP;
END
$$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblwiplotprocess LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblprsnoderelation LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblprdopdescription LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblprdruntimesetup LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblprdproductprocess LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblprdoparea LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiplotoparea LIMIT 1) THEN
        RAISE EXCEPTION 'RT607 rollback stopped because at least one dependency table contains data';
    END IF;
END
$$;

REVOKE SELECT ON "YMES".tblwiplotprocess,
                 "YMES".tblprsnoderelation,
                 "YMES".tblprdopdescription,
                 "YMES".tblprdruntimesetup,
                 "YMES".tblprdproductprocess,
                 "YMES".tblprdoparea,
                 "YMES".tblwiplotoparea FROM ymes_app;

DROP TABLE "YMES".tblwiplotprocess;
DROP TABLE "YMES".tblprsnoderelation;
DROP TABLE "YMES".tblprdopdescription;
DROP TABLE "YMES".tblprdruntimesetup;
DROP TABLE "YMES".tblprdproductprocess;
DROP TABLE "YMES".tblprdoparea;
DROP TABLE "YMES".tblwiplotoparea;

COMMIT;

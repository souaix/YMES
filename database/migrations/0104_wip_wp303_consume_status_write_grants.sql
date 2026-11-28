BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WP303 permission migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP303 permission migration';
    END IF;
    IF to_regclass('"YMES".tblwipeqpconsumestatus') IS NULL
       OR to_regclass('"YMES".tblwipeqpconsumelog') IS NULL THEN
        RAISE EXCEPTION 'WP303 Status/Log source tables are missing; refusing permission migration';
    END IF;
    IF has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumestatus', 'SELECT')
       OR has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumestatus', 'INSERT')
       OR has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumestatus', 'UPDATE')
       OR has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumestatus', 'DELETE') THEN
        RAISE EXCEPTION 'ymes_app already has WP303 Status privileges; review current grants before applying';
    END IF;
    IF has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumelog', 'INSERT')
       OR has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumelog', 'UPDATE')
       OR has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumelog', 'DELETE') THEN
        RAISE EXCEPTION 'ymes_app already has WP303 Log write privileges; review current grants before applying';
    END IF;
    IF NOT has_table_privilege('ymes_app', '"YMES".tblwipeqpconsumelog', 'SELECT') THEN
        RAISE EXCEPTION 'ymes_app lacks WP303 Log SELECT required for duplicate ConsumeSeq checks';
    END IF;
END
$guard$;

-- WP303 API writes only current status and appends immutable history rows.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tblwipeqpconsumestatus TO ymes_app;
GRANT INSERT ON TABLE "YMES".tblwipeqpconsumelog TO ymes_app;

COMMIT;

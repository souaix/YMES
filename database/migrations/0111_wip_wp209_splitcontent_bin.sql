-- WP209: create only the MESTEST source table proven absent from YMES.
-- No business rows are copied; LED sorter bind data is not part of this migration.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP209:0111', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0111 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0111';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0111';
    END IF;
    IF to_regclass('"YMES".tblwipsplitcontent_bin') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipsplitcontent_bin already exists; compare live structure before 0111';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwipsplitcontent_bin'
          AND n.nspname NOT IN ('pg_catalog','information_schema')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An object named tblwipsplitcontent_bin exists outside YMES; inspect before 0111';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE relkind='i' AND lower(relname)='pk_tblwipsplitcontent_bin') THEN
        RAISE EXCEPTION 'Source PK index name already exists; inspect before 0111';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipsplitcontent_bin (
    tolotserial varchar(55) NOT NULL,
    frombin1 numeric(6,0) NOT NULL DEFAULT 0,
    frombin2 numeric(6,0) NOT NULL DEFAULT 0,
    frombin3 numeric(6,0) NOT NULL DEFAULT 0,
    frombin4 numeric(6,0) NOT NULL DEFAULT 0,
    frombin5 numeric(6,0) NOT NULL DEFAULT 0,
    frombin6 numeric(6,0) NOT NULL DEFAULT 0,
    frombin7 numeric(6,0) NOT NULL DEFAULT 0,
    frombin8 numeric(6,0) NOT NULL DEFAULT 0,
    tobin1 numeric(6,0) NOT NULL DEFAULT 0,
    tobin2 numeric(6,0) NOT NULL DEFAULT 0,
    tobin3 numeric(6,0) NOT NULL DEFAULT 0,
    tobin4 numeric(6,0) NOT NULL DEFAULT 0,
    tobin5 numeric(6,0) NOT NULL DEFAULT 0,
    tobin6 numeric(6,0) NOT NULL DEFAULT 0,
    tobin7 numeric(6,0) NOT NULL DEFAULT 0,
    tobin8 numeric(6,0) NOT NULL DEFAULT 0,
    frompassqty numeric(6,0) NOT NULL DEFAULT 0,
    fromfailqty numeric(6,0) NOT NULL DEFAULT 0,
    topassqty numeric(6,0) NOT NULL DEFAULT 0,
    tofailqty numeric(6,0) NOT NULL DEFAULT 0,
    fromlotserial varchar(55) NOT NULL DEFAULT 'N/A',
    CONSTRAINT pk_tblwipsplitcontent_bin PRIMARY KEY (tolotserial)
);
COMMENT ON TABLE "YMES".tblwipsplitcontent_bin IS 'MES Oracle source: MESTEST.TBLWIPSPLITCONTENT_BIN';
REVOKE ALL ON "YMES".tblwipsplitcontent_bin FROM PUBLIC;

COMMIT;

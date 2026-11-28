-- Create only the source-backed WP505 FutureWait table missing from live YMES.
-- No business rows or application grants are added.
-- Sources: mestest_wip_wp201_dependency_source_ddl.sql / mestest_wip_wp201_create.sql.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP505:0121', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0121 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app schema USAGE is missing; refusing 0121';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0121';
    END IF;
    IF to_regclass('"YMES".tblwipfuturewait') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipfuturewait already exists; compare its structure before 0121';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwipfuturewait'
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A tblwipfuturewait relation exists outside YMES; inspect before 0121';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipfuturewait (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    waittime numeric(1,0) NOT NULL,
    description varchar(3000),
    creator varchar(10),
    createdate timestamp without time zone,
    lotserial varchar(55),
    CONSTRAINT pk_tblwipfuturewait PRIMARY KEY (lotno, opno, waittime)
);
COMMENT ON TABLE "YMES".tblwipfuturewait IS 'MES Oracle source: MESTEST.TBLWIPFUTUREWAIT';
REVOKE ALL PRIVILEGES ON TABLE "YMES".tblwipfuturewait FROM PUBLIC, ymes_app;

COMMIT;

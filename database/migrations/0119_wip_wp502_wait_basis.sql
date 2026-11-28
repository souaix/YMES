-- Create only the source-backed WP502 WaitBasis table missing from live YMES.
-- No business rows or app DML grants.
-- Source: mestest_wip_wp201_additional_source_ddl.sql / mestest_wip_wp201_create.sql.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP502:0119', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0119 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app schema USAGE is missing; refusing 0119';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0119';
    END IF;
    IF to_regclass('"YMES".tblwipwaitbasis') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipwaitbasis already exists; compare its structure before 0119';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwipwaitbasis'
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A tblwipwaitbasis relation exists outside YMES; inspect before 0119';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipwaitbasis (
    waitno varchar(20) NOT NULL,
    baselotno varchar(50) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    waitsource numeric(1,0) NOT NULL,
    waittype numeric(1,0) NOT NULL,
    waittime numeric(1,0),
    status numeric(2,0) NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    createqty numeric(12,4) NOT NULL,
    unitno varchar(30) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    customerno varchar(50),
    productno varchar(50),
    createdepartmentno varchar(20),
    ruleno varchar(20),
    waitdescription varchar(3000),
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    modulenodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    modulestageno varchar(50) DEFAULT 'N/A' NOT NULL,
    releaseqty numeric(12,4),
    releaser varchar(10),
    releasedate timestamp without time zone,
    contloggroupserial varchar(50),
    batchwaitno varchar(20),
    mono varchar(50),
    CONSTRAINT pk_tblwipwaitbasis PRIMARY KEY (waitno)
);
COMMENT ON TABLE "YMES".tblwipwaitbasis IS 'MES Oracle source: MESTEST.TBLWIPWAITBASIS';
REVOKE ALL PRIVILEGES ON "YMES".tblwipwaitbasis FROM PUBLIC, ymes_app;

COMMIT;

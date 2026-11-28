-- Create only the two WP501 source-backed ERF staging/archive tables that are
-- absent from the live YMES database. No business rows or app DML grants.
-- Source: database/reference/mestest_wip_wp501_create.sql and Oracle metadata.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP501:0118', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0118 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app schema USAGE is missing; refusing 0118';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0118';
    END IF;
    IF to_regclass('"YMES".tblwiperfcreatedata') IS NOT NULL
       OR to_regclass('"YMES".bktblwiperfcreatedata') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP501 staging table already exists in YMES; compare structure before 0118';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname) IN ('tblwiperfcreatedata','bktblwiperfcreatedata')
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A WP501 staging relation exists outside YMES; inspect before 0118';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwiperfcreatedata (
    repairno varchar(60) NOT NULL,
    lotno varchar(50) NOT NULL,
    erftype numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    erfqty numeric(12,4) NOT NULL,
    holduser varchar(10) NOT NULL,
    holddate timestamp without time zone NOT NULL,
    erfsource numeric(1,0) DEFAULT 0 NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    spcserial varchar(1500),
    qcitemtype numeric(1,0),
    customerno varchar(50),
    productno varchar(50),
    holddescription text,
    holdtime numeric(1,0),
    CONSTRAINT pk_tblwiperfcreatedata PRIMARY KEY (repairno)
);
COMMENT ON TABLE "YMES".tblwiperfcreatedata IS 'MES Oracle source: MESTEST.TBLWIPERFCREATEDATA';
REVOKE ALL PRIVILEGES ON "YMES".tblwiperfcreatedata FROM PUBLIC, ymes_app;

CREATE TABLE "YMES".bktblwiperfcreatedata (
    repairno varchar(60) NOT NULL,
    lotno varchar(50) NOT NULL,
    erftype numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    erfqty numeric(12,4) NOT NULL,
    holduser varchar(50) NOT NULL,
    holddate timestamp without time zone NOT NULL,
    erfsource numeric(1,0) DEFAULT 0 NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    spcserial varchar(1500),
    qcitemtype numeric(1,0),
    customerno varchar(50),
    productno varchar(50),
    holddescription text,
    holdtime numeric(1,0)
);
CREATE INDEX i_bktblwiperfcreatedata_01
    ON "YMES".bktblwiperfcreatedata (lotno);
COMMENT ON TABLE "YMES".bktblwiperfcreatedata IS 'MES Oracle source: MESTEST.BKTBLWIPERFCREATEDATA';
REVOKE ALL PRIVILEGES ON "YMES".bktblwiperfcreatedata FROM PUBLIC, ymes_app;

COMMIT;

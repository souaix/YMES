-- Create only source-backed WP504 Future Hold tables missing from live YMES.
-- No business rows or application grants are added.
-- Sources: mestest_wip_wp201_dependency_source_ddl.sql,
--          mestest_wip_wp201_create.sql, mestest_wip_wp504_source_ddl.sql,
--          mestest_wip_wp504_source_indexes.sql, mestest_wip_wp504_create.sql.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP504:0120', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0120 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app schema USAGE is missing; refusing 0120';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0120';
    END IF;
    IF to_regclass('"YMES".tblwipfuturehold') IS NOT NULL
       OR to_regclass('"YMES".tblwipfutureholdreason') IS NOT NULL
       OR to_regclass('"YMES".tblwipfutureholdlog') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP504 table already exists in YMES; compare its structure before 0120';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname) IN ('tblwipfuturehold','tblwipfutureholdreason','tblwipfutureholdlog')
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A WP504 relation exists outside YMES; inspect before 0120';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipfuturehold (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    mclassno varchar(30),
    holddescription varchar(4000),
    creator varchar(10),
    createdate timestamp without time zone,
    lotserial varchar(55),
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    futureholdbatchno varchar(20),
    futureholdserial varchar(20) DEFAULT 'N/A' NOT NULL,
    lockflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblwipfuturehold PRIMARY KEY
        (lotno, moduleno, moduleversion, opno, holdtime, futureholdserial)
);
COMMENT ON TABLE "YMES".tblwipfuturehold IS 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLD';
REVOKE ALL PRIVILEGES ON TABLE "YMES".tblwipfuturehold FROM PUBLIC, ymes_app;

CREATE TABLE "YMES".tblwipfutureholdreason (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    holditemno varchar(50) NOT NULL,
    lotserial varchar(55),
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    futureholdserial varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblwipfutureholdreason PRIMARY KEY
        (holditemno, holdtime, lotno, moduleno, moduleversion, opno, futureholdserial)
);
COMMENT ON TABLE "YMES".tblwipfutureholdreason IS 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDREASON';
REVOKE ALL PRIVILEGES ON TABLE "YMES".tblwipfutureholdreason FROM PUBLIC, ymes_app;

CREATE TABLE "YMES".tblwipfutureholdlog (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    mclassno varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    erfno varchar(20) NOT NULL,
    holddescription varchar(4000) NOT NULL
);
CREATE INDEX i_tblwipfutureholdlog_01 ON "YMES".tblwipfutureholdlog (lotno);
COMMENT ON TABLE "YMES".tblwipfutureholdlog IS 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDLOG; downstream ERFType=3 lifecycle';
REVOKE ALL PRIVILEGES ON TABLE "YMES".tblwipfutureholdlog FROM PUBLIC, ymes_app;

COMMIT;

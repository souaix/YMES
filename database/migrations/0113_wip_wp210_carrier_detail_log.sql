-- WP210: create the source-backed carrier detail log proven absent from YMES.
-- This is an empty source table only; no business rows are copied.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP210:0113', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0113 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0113';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0113';
    END IF;
    IF to_regclass('"YMES".tblwipcarrierstatedetaillog') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipcarrierstatedetaillog already exists; compare live structure before 0113';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwipcarrierstatedetaillog'
          AND n.nspname NOT IN ('pg_catalog','information_schema')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An object named tblwipcarrierstatedetaillog exists outside YMES; inspect before 0113';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipcarrierstatedetaillog (
    lotserial varchar(50) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    carrierno varchar(50) NOT NULL,
    carrierposition numeric(3,0) NOT NULL,
    bcip varchar(50) NOT NULL,
    contentid varchar(50) NOT NULL,
    flag numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    putintime timestamp without time zone,
    takeouttime timestamp without time zone,
    s_bindmode numeric(1,0),
    s_bindopno varchar(20),
    s_bindtype numeric(1,0),
    s_releasebindmode numeric(1,0),
    s_releasebindopno varchar(20),
    s_releasebindtype numeric(1,0),
    s_releaseuserno varchar(10)
);
COMMENT ON TABLE "YMES".tblwipcarrierstatedetaillog IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATEDETAILLOG';
REVOKE ALL PRIVILEGES ON "YMES".tblwipcarrierstatedetaillog FROM PUBLIC, ymes_app;

COMMIT;

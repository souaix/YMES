-- Create the source-backed WP510 wait disposition detail table missing from live YMES.
-- No business rows or application grants are added.
-- Source: Oracle MESTEST.TBLWIPWAITLOTDISPOSITION and its WAITNO non-unique index.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP510:0122', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0122 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app role/USAGE is missing; refusing 0122';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0122';
    END IF;
    IF to_regclass('"YMES".tblwipwaitlotdisposition') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipwaitlotdisposition already exists; compare it before 0122';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwipwaitlotdisposition'
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A tblwipwaitlotdisposition relation exists outside YMES; inspect before 0122';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipwaitlotdisposition (
    waitno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotdisptype numeric(2,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    description varchar(255),
    nodeid varchar(50),
    inventoryno varchar(50),
    returntype numeric(1,0),
    returnnodeid varchar(50),
    moduleno varchar(50),
    moduleversion varchar(3),
    startnodeid varchar(50)
);
CREATE INDEX i_tblwipwaitlotdisposition_01
    ON "YMES".tblwipwaitlotdisposition (waitno);
COMMENT ON TABLE "YMES".tblwipwaitlotdisposition
    IS 'MES Oracle source: MESTEST.TBLWIPWAITLOTDISPOSITION';
REVOKE ALL PRIVILEGES ON TABLE "YMES".tblwipwaitlotdisposition FROM PUBLIC, ymes_app;

COMMIT;

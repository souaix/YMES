-- WP209: create only the two source-backed carrier tables proven absent from YMES.
-- The detail table intentionally has no source PK/unique constraint.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP209:0112', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0112 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0112';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0112';
    END IF;
    IF to_regclass('"YMES".tblwipcarrierstate') IS NOT NULL
       OR to_regclass('"YMES".tblwipcarrierstatedetail') IS NOT NULL THEN
        RAISE EXCEPTION 'A YMES carrier source table already exists; compare live structures before 0112';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname) = ANY (ARRAY['tblwipcarrierstate','tblwipcarrierstatedetail'])
          AND n.nspname NOT IN ('pg_catalog','information_schema')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A carrier object exists outside YMES; inspect before 0112';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE relkind='i' AND lower(relname) = ANY (ARRAY[
        'pk_tblwipcarrierstate','i_tblwipcarrierstatedetail_01'
    ])) THEN
        RAISE EXCEPTION 'A 0112 source index name already exists; inspect before migration';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipcarrierstate (
    carrierno varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    carriertype varchar(50) NOT NULL,
    carrierstate numeric(1,0),
    description varchar(255),
    lotno varchar(50),
    CONSTRAINT pk_tblwipcarrierstate PRIMARY KEY (carrierno)
);
COMMENT ON TABLE "YMES".tblwipcarrierstate IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATE';

CREATE TABLE "YMES".tblwipcarrierstatedetail (
    carrierno varchar(50) NOT NULL,
    carrierposition numeric(3,0) NOT NULL DEFAULT 1,
    bcip varchar(50) NOT NULL,
    contentid varchar(50) NOT NULL,
    flag numeric(1,0) NOT NULL DEFAULT 0,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    eventtime timestamp without time zone,
    s_bindmode numeric(1,0),
    s_bindopno varchar(20),
    s_bindtype numeric(1,0)
);
CREATE INDEX i_tblwipcarrierstatedetail_01 ON "YMES".tblwipcarrierstatedetail (componentno);
COMMENT ON TABLE "YMES".tblwipcarrierstatedetail IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATEDETAIL';

REVOKE ALL ON "YMES".tblwipcarrierstate,
    "YMES".tblwipcarrierstatedetail FROM PUBLIC;

COMMIT;

-- WP914 authenticated atomic HotLot/Priority update idempotency ledger.
-- Source tables are pre-existing Oracle mirrors; this migration adds no MES source table.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP914:0115', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0115 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE')
       OR has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app schema permissions are not in the expected state; refusing 0115';
    END IF;
    IF NOT has_table_privilege('ymes_app', '"YMES".tblwiplotbasis', 'SELECT')
       OR NOT has_table_privilege('ymes_app', '"YMES".tblwiplotstate', 'SELECT')
       OR NOT has_table_privilege('ymes_app', '"YMES".tblopbasis', 'SELECT') THEN
        RAISE EXCEPTION 'WP914 read sources are not SELECT-accessible to ymes_app; refusing 0115';
    END IF;
    IF has_column_privilege('ymes_app', '"YMES".tblwiplotbasis', 'hotlot', 'UPDATE')
       OR has_column_privilege('ymes_app', '"YMES".tblwiplotbasis', 'priority', 'UPDATE') THEN
        RAISE EXCEPTION 'ymes_app already has a WP914 update permission; review grants before 0115';
    END IF;
    IF to_regclass('"YMES".ym_wp914_request') IS NOT NULL
       OR EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)='ym_wp914_request'
              AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
       ) THEN
        RAISE EXCEPTION 'A WP914 ledger relation already exists; inspect before 0115';
    END IF;
END
$guard$;

CREATE TABLE "YMES".ym_wp914_request (
    requestid uuid NOT NULL,
    actor varchar(10) NOT NULL,
    payloadsha256 varchar(64) NOT NULL,
    resultjson jsonb NOT NULL,
    createdate timestamp with time zone NOT NULL,
    CONSTRAINT pk_ym_wp914_request PRIMARY KEY (requestid)
);
COMMENT ON TABLE "YMES".ym_wp914_request IS 'YMES-only WP914 idempotency ledger; not an Oracle MES table';

REVOKE ALL PRIVILEGES ON "YMES".ym_wp914_request FROM PUBLIC, ymes_app;
GRANT SELECT, INSERT ON "YMES".ym_wp914_request TO ymes_app;
GRANT UPDATE (hotlot, priority) ON "YMES".tblwiplotbasis TO ymes_app;

COMMIT;

-- Revoke the WP507 read grant only. This rollback does not delete data.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP507:0126', 0));

DO $guard$
DECLARE
    future_wait_oid oid := to_regclass('"YMES".tblwipfuturewait');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0126 rollback requires writable PostgreSQL 14 on ymes as postgres';
    END IF;
    IF future_wait_oid IS NULL
       OR obj_description(future_wait_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPFUTUREWAIT' THEN
        RAISE EXCEPTION '0126 rollback target is missing or source marker differs';
    END IF;
    IF NOT has_table_privilege('ymes_app',future_wait_oid,'SELECT')
       OR has_table_privilege('ymes_app',future_wait_oid,'INSERT')
       OR has_table_privilege('ymes_app',future_wait_oid,'UPDATE')
       OR has_table_privilege('ymes_app',future_wait_oid,'DELETE') THEN
        RAISE EXCEPTION '0126 rollback expects SELECT-only ymes_app access';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE "YMES".tblwipfuturewait FROM ymes_app;
COMMIT;

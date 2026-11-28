-- Remove the WP914 row-lock helper. Roll back the API release first, then apply 0116 rollback.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP914:0117', 0));

DO $guard$
DECLARE
    routine_oid oid;
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0117 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    routine_oid := to_regprocedure('"YMES".ym_wp914_lock_selected_lots(character varying[],character varying[],character varying[],numeric[],numeric[])');
    IF routine_oid IS NULL
       OR (SELECT prosecdef FROM pg_catalog.pg_proc WHERE oid = routine_oid) IS NOT TRUE
       OR (SELECT proowner FROM pg_catalog.pg_proc WHERE oid = routine_oid) <> 'postgres'::regrole THEN
        RAISE EXCEPTION 'WP914 lock routine is missing or its owner/security changed; refusing 0117 rollback';
    END IF;
END
$guard$;

REVOKE ALL PRIVILEGES ON FUNCTION "YMES".ym_wp914_lock_selected_lots(
    character varying[], character varying[], character varying[], numeric[], numeric[]
) FROM PUBLIC, ymes_app;
DROP FUNCTION "YMES".ym_wp914_lock_selected_lots(
    character varying[], character varying[], character varying[], numeric[], numeric[]
);

COMMIT;

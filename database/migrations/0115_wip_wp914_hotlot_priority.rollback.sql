-- Remove only the empty WP914 idempotency ledger and its column update grant.
-- No MES source table or source data is removed.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP914:0115', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0115 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".ym_wp914_request') IS NULL THEN
        RAISE EXCEPTION 'YMES.ym_wp914_request is missing; refusing 0115 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".ym_wp914_request LIMIT 1) THEN
        RAISE EXCEPTION '0115 rollback refused because the idempotency ledger contains request history';
    END IF;
    IF (SELECT count(*) FROM pg_attribute
        WHERE attrelid=to_regclass('"YMES".ym_wp914_request') AND attnum>0 AND NOT attisdropped) <> 5 THEN
        RAISE EXCEPTION '0115 ledger structure differs from the expected structure; refusing rollback';
    END IF;
END
$guard$;

REVOKE UPDATE (hotlot, priority) ON "YMES".tblwiplotbasis FROM ymes_app;
REVOKE ALL PRIVILEGES ON "YMES".ym_wp914_request FROM PUBLIC, ymes_app;
DROP TABLE "YMES".ym_wp914_request;
COMMIT;

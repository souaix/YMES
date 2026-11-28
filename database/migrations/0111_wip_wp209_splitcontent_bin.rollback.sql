-- Remove only the empty source table created by 0111. No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP209:0111', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0111 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipsplitcontent_bin') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwipsplitcontent_bin is missing; refusing 0111 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipsplitcontent_bin LIMIT 1) THEN
        RAISE EXCEPTION '0111 rollback refused because the source table contains rows';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipsplitcontent_bin;
COMMIT;

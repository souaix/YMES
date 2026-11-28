-- Remove only the empty WP502 WaitBasis table created by 0119. No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP502:0119', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0119 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipwaitbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwipwaitbasis is missing; refusing rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipwaitbasis LIMIT 1) THEN
        RAISE EXCEPTION '0119 rollback refused because tblwipwaitbasis contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwipwaitbasis') AND attnum>0 AND NOT attisdropped) <> 30
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblwipwaitbasis')
          AND contype='p' AND pg_get_constraintdef(oid)='PRIMARY KEY (waitno)'
       ) THEN
        RAISE EXCEPTION '0119 table structure differs from the expected source structure; refusing rollback';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipwaitbasis;
COMMIT;

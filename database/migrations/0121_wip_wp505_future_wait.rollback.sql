-- Remove only the empty source-backed WP505 FutureWait table created by 0121.
-- No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP505:0121', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0121 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipfuturewait') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwipfuturewait is missing; refusing 0121 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipfuturewait LIMIT 1) THEN
        RAISE EXCEPTION '0121 rollback refused because tblwipfuturewait contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwipfuturewait') AND attnum>0 AND NOT attisdropped) <> 7
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblwipfuturewait')
          AND contype='p' AND pg_get_constraintdef(oid)='PRIMARY KEY (lotno, opno, waittime)'
       )
       OR obj_description(to_regclass('"YMES".tblwipfuturewait'),'pg_class')
          IS DISTINCT FROM 'MES Oracle source: MESTEST.TBLWIPFUTUREWAIT' THEN
        RAISE EXCEPTION '0121 table structure differs from the expected source structure; refusing rollback';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipfuturewait;
COMMIT;

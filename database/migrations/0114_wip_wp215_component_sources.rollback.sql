-- Remove only the empty source tables created by 0114. No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP215:0114', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0114 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipcomponentbasis') IS NULL
       OR to_regclass('"YMES".tblwipcont_component') IS NULL THEN
        RAISE EXCEPTION 'An 0114 table is missing; refusing rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipcomponentbasis LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_component LIMIT 1) THEN
        RAISE EXCEPTION '0114 rollback refused because a source table contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_attribute WHERE attrelid=to_regclass('"YMES".tblwipcomponentbasis') AND attnum>0 AND NOT attisdropped) <> 32
       OR (SELECT count(*) FROM pg_attribute WHERE attrelid=to_regclass('"YMES".tblwipcont_component') AND attnum>0 AND NOT attisdropped) <> 27 THEN
        RAISE EXCEPTION '0114 table structure differs from the expected source structure; refusing rollback';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipcont_component;
DROP TABLE "YMES".tblwipcomponentbasis;
COMMIT;

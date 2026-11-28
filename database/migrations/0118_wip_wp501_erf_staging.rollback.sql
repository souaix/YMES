-- Remove only the two empty tables created by 0118. No CASCADE or app grant changes.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP501:0118', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0118 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwiperfcreatedata') IS NULL
       OR to_regclass('"YMES".bktblwiperfcreatedata') IS NULL THEN
        RAISE EXCEPTION 'An 0118 table is missing; refusing rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwiperfcreatedata LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".bktblwiperfcreatedata LIMIT 1) THEN
        RAISE EXCEPTION '0118 rollback refused because a staging table contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwiperfcreatedata') AND attnum>0 AND NOT attisdropped) <> 15
       OR (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".bktblwiperfcreatedata') AND attnum>0 AND NOT attisdropped) <> 15
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_index i
        JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid
        WHERE i.indrelid=to_regclass('"YMES".bktblwiperfcreatedata')
          AND ic.relname='i_bktblwiperfcreatedata_01'
       ) THEN
        RAISE EXCEPTION '0118 table structure differs from the expected source structure; refusing rollback';
    END IF;
END
$guard$;

DROP TABLE "YMES".bktblwiperfcreatedata;
DROP TABLE "YMES".tblwiperfcreatedata;
COMMIT;

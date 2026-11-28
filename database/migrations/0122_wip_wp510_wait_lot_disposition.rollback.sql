-- Remove only the empty source-backed WP510 detail table created by migration 0122.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP510:0122', 0));

DO $guard$
DECLARE
    target_oid oid := to_regclass('"YMES".tblwipwaitlotdisposition');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0122 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF target_oid IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwipwaitlotdisposition is missing; refusing 0122 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipwaitlotdisposition LIMIT 1) THEN
        RAISE EXCEPTION '0122 rollback refused because tblwipwaitlotdisposition contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=target_oid AND attnum>0 AND NOT attisdropped) <> 13
       OR obj_description(target_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPWAITLOTDISPOSITION' THEN
        RAISE EXCEPTION '0122 rollback refused because table shape or source marker differs';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_constraint WHERE conrelid=target_oid)
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_index i
                  WHERE i.indrelid=target_oid AND i.indisunique)
       OR (SELECT count(*) FROM pg_catalog.pg_index i
           JOIN pg_catalog.pg_class ix ON ix.oid=i.indexrelid
           WHERE i.indrelid=target_oid AND NOT i.indisprimary) <> 1
       OR NOT EXISTS (
            SELECT 1 FROM pg_catalog.pg_index i
            JOIN pg_catalog.pg_class ix ON ix.oid=i.indexrelid
            JOIN pg_catalog.pg_attribute a ON a.attrelid=target_oid AND a.attnum=i.indkey[0]
            WHERE i.indrelid=target_oid AND NOT i.indisunique
              AND ix.relname='i_tblwipwaitlotdisposition_01'
              AND a.attname='waitno' AND i.indnkeyatts=1
        ) THEN
        RAISE EXCEPTION '0122 rollback refused because table constraints or index differ';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipwaitlotdisposition;
COMMIT;

-- Remove only the empty source-backed WP504 tables created by migration 0120.
-- No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP504:0120', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0120 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipfuturehold') IS NULL
       OR to_regclass('"YMES".tblwipfutureholdreason') IS NULL
       OR to_regclass('"YMES".tblwipfutureholdlog') IS NULL THEN
        RAISE EXCEPTION 'A WP504 table is missing; refusing 0120 rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipfuturehold LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipfutureholdreason LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipfutureholdlog LIMIT 1) THEN
        RAISE EXCEPTION '0120 rollback refused because a WP504 table contains rows';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwipfuturehold') AND attnum>0 AND NOT attisdropped) <> 13
       OR (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwipfutureholdreason') AND attnum>0 AND NOT attisdropped) <> 8
       OR (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=to_regclass('"YMES".tblwipfutureholdlog') AND attnum>0 AND NOT attisdropped) <> 10
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblwipfuturehold')
          AND contype='p'
          AND pg_get_constraintdef(oid)='PRIMARY KEY (lotno, moduleno, moduleversion, opno, holdtime, futureholdserial)'
       )
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblwipfutureholdreason')
          AND contype='p'
          AND pg_get_constraintdef(oid)='PRIMARY KEY (holditemno, holdtime, lotno, moduleno, moduleversion, opno, futureholdserial)'
       )
       OR EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblwipfutureholdlog') AND contype IN ('p','u','f')
       )
       OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_indexes
        WHERE schemaname='YMES' AND tablename='tblwipfutureholdlog'
          AND indexname='i_tblwipfutureholdlog_01'
          AND indexdef='CREATE INDEX i_tblwipfutureholdlog_01 ON "YMES".tblwipfutureholdlog USING btree (lotno)'
       ) THEN
        RAISE EXCEPTION '0120 structure differs from the expected source structure; refusing rollback';
    END IF;
    IF obj_description(to_regclass('"YMES".tblwipfuturehold'),'pg_class')
          IS DISTINCT FROM 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLD'
       OR obj_description(to_regclass('"YMES".tblwipfutureholdreason'),'pg_class')
          IS DISTINCT FROM 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDREASON'
       OR obj_description(to_regclass('"YMES".tblwipfutureholdlog'),'pg_class')
          IS DISTINCT FROM 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDLOG; downstream ERFType=3 lifecycle' THEN
        RAISE EXCEPTION '0120 source comments differ; refusing rollback';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipfutureholdlog;
DROP TABLE "YMES".tblwipfutureholdreason;
DROP TABLE "YMES".tblwipfuturehold;
COMMIT;

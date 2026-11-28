-- Remove only the empty WP509 table created by 0125; never cascade.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP509:0125', 0));

DO $guard$
DECLARE
    privilege_oid oid := to_regclass('"YMES".xx_tblqcholdpriv');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0125 rollback requires writable PostgreSQL 14 on ymes as postgres';
    END IF;
    IF privilege_oid IS NULL THEN
        RAISE EXCEPTION '0125 rollback target is missing; refusing rollback';
    END IF;
    IF obj_description(privilege_oid,'pg_class') IS DISTINCT FROM
           'MES Oracle source: MESTEST.XX_TBLQCHOLDPRIV; group based ERF lot disposition permissions'
       OR (SELECT count(*) FROM pg_catalog.pg_attribute
           WHERE attrelid=privilege_oid AND attnum>0 AND NOT attisdropped) <> 18 THEN
        RAISE EXCEPTION '0125 rollback target differs from expected source table';
    END IF;
    IF (SELECT count(*) FROM "YMES".xx_tblqcholdpriv) <> 0 THEN
        RAISE EXCEPTION '0125 rollback requires an empty table; refusing to delete privilege rows';
    END IF;
    IF has_table_privilege('ymes_app',privilege_oid,'SELECT')
       OR has_table_privilege('ymes_app',privilege_oid,'INSERT')
       OR has_table_privilege('ymes_app',privilege_oid,'UPDATE')
       OR has_table_privilege('ymes_app',privilege_oid,'DELETE') THEN
        RAISE EXCEPTION '0125 rollback requires no application grants';
    END IF;
END
$guard$;

DROP TABLE "YMES".xx_tblqcholdpriv;
COMMIT;

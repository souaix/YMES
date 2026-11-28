-- Revoke only the WP510 read grants added by migration 0123; preserve all table data/schema.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP510:0123', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0123 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipwaitbasis') IS NULL
       OR to_regclass('"YMES".tblwipwaitlotdisposition') IS NULL
       OR obj_description(to_regclass('"YMES".tblwipwaitbasis'),'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPWAITBASIS'
       OR obj_description(to_regclass('"YMES".tblwipwaitlotdisposition'),'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPWAITLOTDISPOSITION' THEN
        RAISE EXCEPTION '0123 rollback refused because WP510 table identity changed';
    END IF;
    IF has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','DELETE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','DELETE') THEN
        RAISE EXCEPTION '0123 rollback refused because unexpected WP510 DML access exists';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE "YMES".tblwipwaitbasis, "YMES".tblwipwaitlotdisposition FROM ymes_app;
COMMIT;

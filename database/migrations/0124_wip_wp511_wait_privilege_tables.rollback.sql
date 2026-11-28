-- Remove only the two schema-only WP511 tables created by migration 0124.
-- Refuses rollback if either table has acquired any data or changed its source identity.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP511:0124', 0));

DO $guard$
DECLARE
    user_group_oid oid := to_regclass('"YMES".tblusrusergroup');
    wait_priv_oid oid := to_regclass('"YMES".xxtblwaitpriv');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0124 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF user_group_oid IS NULL OR wait_priv_oid IS NULL
       OR obj_description(user_group_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLUSRUSERGROUP'
       OR obj_description(wait_priv_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.XX_TBLWAITPRIV' THEN
        RAISE EXCEPTION '0124 rollback refused because table identity changed';
    END IF;
    IF (SELECT count(*) FROM "YMES".tblusrusergroup) <> 0
       OR (SELECT count(*) FROM "YMES".xxtblwaitpriv) <> 0 THEN
        RAISE EXCEPTION '0124 rollback refused because a WP511 table contains rows';
    END IF;
END
$guard$;

DROP TABLE "YMES".xxtblwaitpriv;
DROP TABLE "YMES".tblusrusergroup;
COMMIT;

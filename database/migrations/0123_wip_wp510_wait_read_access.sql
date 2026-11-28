-- Grant only the SELECT access needed by the authenticated WP510 read API.
-- No INSERT/UPDATE/DELETE/DDL permission or business data is added.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP510:0123', 0));

DO $guard$
DECLARE
    wait_oid oid := to_regclass('"YMES".tblwipwaitbasis');
    disposition_oid oid := to_regclass('"YMES".tblwipwaitlotdisposition');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0123 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF wait_oid IS NULL OR disposition_oid IS NULL
       OR to_regclass('"YMES".tblprsnodebasis') IS NULL
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE')
       OR has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'WP510 source tables or restricted YMES schema privileges are not ready';
    END IF;
    IF obj_description(wait_oid,'pg_class') IS DISTINCT FROM 'MES Oracle source: MESTEST.TBLWIPWAITBASIS'
       OR obj_description(disposition_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPWAITLOTDISPOSITION'
       OR NOT has_table_privilege('ymes_app','"YMES".tblprsnodebasis','SELECT') THEN
        RAISE EXCEPTION 'WP510 source markers or NodeBasis SELECT access differ; review before 0123';
    END IF;
    IF has_table_privilege('ymes_app',wait_oid,'SELECT')
       OR has_table_privilege('ymes_app',disposition_oid,'SELECT') THEN
        RAISE EXCEPTION 'WP510 SELECT is already present; compare its grant origin before 0123';
    END IF;
    IF has_table_privilege('ymes_app',wait_oid,'INSERT') OR has_table_privilege('ymes_app',wait_oid,'UPDATE')
       OR has_table_privilege('ymes_app',wait_oid,'DELETE') OR has_table_privilege('ymes_app',disposition_oid,'INSERT')
       OR has_table_privilege('ymes_app',disposition_oid,'UPDATE') OR has_table_privilege('ymes_app',disposition_oid,'DELETE') THEN
        RAISE EXCEPTION 'Unexpected WP510 application DML access exists; review before 0123';
    END IF;
END
$guard$;

GRANT SELECT ON TABLE "YMES".tblwipwaitbasis, "YMES".tblwipwaitlotdisposition TO ymes_app;

DO $verify$
BEGIN
    IF NOT has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','SELECT')
       OR NOT has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','SELECT')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitbasis','DELETE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipwaitlotdisposition','DELETE') THEN
        RAISE EXCEPTION '0123 did not produce the intended read-only WP510 table privileges';
    END IF;
END
$verify$;

COMMIT;

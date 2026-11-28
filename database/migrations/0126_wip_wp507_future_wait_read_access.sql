-- Grant WP507 bounded read access to the source-backed future-wait table only.
-- Does not enable DELETE/Release; no data or schema is changed.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP507:0126', 0));

DO $guard$
DECLARE
    future_wait_oid oid := to_regclass('"YMES".tblwipfuturewait');
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0126 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF future_wait_oid IS NULL
       OR obj_description(future_wait_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLWIPFUTUREWAIT' THEN
        RAISE EXCEPTION 'TBLWIPFUTUREWAIT is missing or has a different source marker; inspect before 0126';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE')
       OR has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app role/schema privileges do not match the expected restricted setup';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=future_wait_oid AND attnum>0 AND NOT attisdropped) <> 7
       OR (SELECT pg_catalog.pg_get_constraintdef(oid) FROM pg_catalog.pg_constraint
           WHERE conrelid=future_wait_oid AND contype='p') <> 'PRIMARY KEY (lotno, opno, waittime)' THEN
        RAISE EXCEPTION 'TBLWIPFUTUREWAIT columns or primary key differ from source';
    END IF;
    IF has_table_privilege('ymes_app',future_wait_oid,'SELECT')
       OR has_table_privilege('ymes_app',future_wait_oid,'INSERT')
       OR has_table_privilege('ymes_app',future_wait_oid,'UPDATE')
       OR has_table_privilege('ymes_app',future_wait_oid,'DELETE') THEN
        RAISE EXCEPTION 'TBLWIPFUTUREWAIT access changed since audit; inspect before 0126';
    END IF;
END
$guard$;

REVOKE INSERT, UPDATE, DELETE ON TABLE "YMES".tblwipfuturewait FROM PUBLIC, ymes_app;
GRANT SELECT ON TABLE "YMES".tblwipfuturewait TO ymes_app;

DO $verify$
BEGIN
    IF NOT has_table_privilege('ymes_app','"YMES".tblwipfuturewait','SELECT')
       OR has_table_privilege('ymes_app','"YMES".tblwipfuturewait','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipfuturewait','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipfuturewait','DELETE') THEN
        RAISE EXCEPTION '0126 did not establish SELECT-only app access';
    END IF;
END
$verify$;

COMMIT;

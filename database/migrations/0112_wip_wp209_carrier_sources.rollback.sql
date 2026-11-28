-- Remove only the two empty carrier source tables created by 0112. No CASCADE.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP209:0112', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0112 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblwipcarrierstate') IS NULL
       OR to_regclass('"YMES".tblwipcarrierstatedetail') IS NULL THEN
        RAISE EXCEPTION 'A 0112 carrier table is missing; refusing partial rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwipcarrierstate LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcarrierstatedetail LIMIT 1) THEN
        RAISE EXCEPTION '0112 rollback refused because a source table contains rows';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwipcarrierstatedetail;
DROP TABLE "YMES".tblwipcarrierstate;
COMMIT;

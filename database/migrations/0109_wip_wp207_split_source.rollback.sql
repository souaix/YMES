-- Remove only the six empty WP207 source tables created by 0109.
-- No CASCADE: dependent objects cause rollback instead of being removed.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP207:0109', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0109 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblemsdispatchstate') IS NULL
       OR to_regclass('"YMES".tblwipautomerge') IS NULL
       OR to_regclass('"YMES".tblwipcont_employee') IS NULL
       OR to_regclass('"YMES".tblwiplotopecnrecord') IS NULL
       OR to_regclass('"YMES".tblwiposbasis') IS NULL
       OR to_regclass('"YMES".tblwiposdetail') IS NULL THEN
        RAISE EXCEPTION 'One or more 0109 tables are missing; refusing partial rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblemsdispatchstate LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipautomerge LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_employee LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiplotopecnrecord LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiposbasis LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiposdetail LIMIT 1) THEN
        RAISE EXCEPTION '0109 rollback refused because a source table contains rows';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwiposdetail;
DROP TABLE "YMES".tblwiposbasis;
DROP TABLE "YMES".tblwiplotopecnrecord;
DROP TABLE "YMES".tblwipcont_employee;
DROP TABLE "YMES".tblwipautomerge;
DROP TABLE "YMES".tblemsdispatchstate;
COMMIT;

-- Remove only the nine empty WP207/WP208 source tables created by 0110.
-- No CASCADE: dependent objects cause rollback instead of being removed.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP207-WP208:0110', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0110 rollback requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF to_regclass('"YMES".tblprdopattrib') IS NULL
       OR to_regclass('"YMES".tblwipcomponentproperty') IS NULL
       OR to_regclass('"YMES".tblwipcomponentstate') IS NULL
       OR to_regclass('"YMES".tblwipcont_attrib') IS NULL
       OR to_regclass('"YMES".tblwipcont_componentattrib') IS NULL
       OR to_regclass('"YMES".tblwiplotproperty') IS NULL
       OR to_regclass('"YMES".tblwiptemp_attrib') IS NULL
       OR to_regclass('"YMES".tblwiptemp_componentattrib') IS NULL
       OR to_regclass('"YMES".tblwiptemp_lotstate') IS NULL THEN
        RAISE EXCEPTION 'One or more 0110 tables are missing; refusing partial rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblprdopattrib LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcomponentproperty LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcomponentstate LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_attrib LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwipcont_componentattrib LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiplotproperty LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiptemp_attrib LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiptemp_componentattrib LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwiptemp_lotstate LIMIT 1) THEN
        RAISE EXCEPTION '0110 rollback refused because a source table contains rows';
    END IF;
END
$guard$;

DROP TABLE "YMES".tblwiptemp_lotstate;
DROP TABLE "YMES".tblwiptemp_componentattrib;
DROP TABLE "YMES".tblwiptemp_attrib;
DROP TABLE "YMES".tblwiplotproperty;
DROP TABLE "YMES".tblwipcont_componentattrib;
DROP TABLE "YMES".tblwipcont_attrib;
DROP TABLE "YMES".tblwipcomponentproperty;
DROP TABLE "YMES".tblwipcomponentstate;
DROP TABLE "YMES".tblprdopattrib;
COMMIT;

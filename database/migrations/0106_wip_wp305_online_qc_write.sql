-- WP305 standalone inspection writer permissions and its request idempotency ledger.
-- The ledger is YMES API support metadata, not an MES source table or business-data replica.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WP305 writer migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP305 writer migration';
    END IF;
    IF to_regclass('"YMES".tblwipeqpqclistlog') IS NULL
       OR to_regclass('"YMES".tblwipeqpqclistdetail') IS NULL
       OR to_regclass('"YMES".tblwipeqpqctask') IS NULL THEN
        RAISE EXCEPTION 'WP305 log/task tables are missing; refusing writer migration';
    END IF;
    IF to_regclass('"YMES".ym_wp305_request') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.ym_wp305_request already exists; inspect before migrating';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='ym_wp305_request'
          AND n.nspname<>'YMES'
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'ym_wp305_request exists outside YMES; inspect before migrating';
    END IF;
    IF has_table_privilege('ymes_app','"YMES".tblwipeqpqclistlog','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipeqpqclistlog','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipeqpqclistlog','DELETE')
       OR has_table_privilege('ymes_app','"YMES".tblwipeqpqclistdetail','INSERT')
       OR has_table_privilege('ymes_app','"YMES".tblwipeqpqclistdetail','UPDATE')
       OR has_table_privilege('ymes_app','"YMES".tblwipeqpqclistdetail','DELETE')
       OR has_column_privilege('ymes_app','"YMES".tblwipeqpqctask','status','UPDATE')
       OR has_column_privilege('ymes_app','"YMES".tblwipeqpqctask','qcserialno','UPDATE') THEN
        RAISE EXCEPTION 'WP305 writer privileges already exist; inspect ACL before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".ym_wp305_request (
    requestid uuid NOT NULL,
    creator varchar(10) NOT NULL,
    payloadsha256 varchar(64) NOT NULL,
    qclistserial varchar(100) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_ym_wp305_request PRIMARY KEY (requestid)
);
COMMENT ON TABLE "YMES".ym_wp305_request IS
    'YMES API idempotency ledger for WP305 standalone inspection saves; not an MES source table';

GRANT INSERT ON "YMES".tblwipeqpqclistlog TO ymes_app;
GRANT INSERT ON "YMES".tblwipeqpqclistdetail TO ymes_app;
GRANT UPDATE (status, qcserialno) ON "YMES".tblwipeqpqctask TO ymes_app;
GRANT SELECT, INSERT ON "YMES".ym_wp305_request TO ymes_app;

COMMIT;

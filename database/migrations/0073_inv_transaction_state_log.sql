-- Shared inventory transaction engine tables, copied from the verified WP605
-- PostgreSQL CREATE-only reference. No MES business rows are imported.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing inventory transaction migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing inventory transaction migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing inventory transaction migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY['tblinvtransactionstate','tblinvtransactionlog'] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name
              AND n.nspname<>'YMES'
              AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblinvtransactionstate (
    transactionid varchar(50) NOT NULL,
    source numeric(2,0) NOT NULL,
    ticketno varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblinvtransactionstate PRIMARY KEY (transactionid)
);

CREATE TABLE "YMES".tblinvtransactionlog (
    transactionid varchar(50) NOT NULL,
    source numeric(2,0) NOT NULL,
    ticketno varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblinvtransactionlog PRIMARY KEY (transactionid)
);

COMMENT ON TABLE "YMES".tblinvtransactionstate IS 'MES Oracle source: MESTEST.TBLINVTRANSACTIONSTATE';
COMMENT ON TABLE "YMES".tblinvtransactionlog IS 'MES Oracle source: MESTEST.TBLINVTRANSACTIONLOG';

GRANT SELECT ON "YMES".tblinvtransactionstate TO ymes_app;
GRANT SELECT ON "YMES".tblinvtransactionlog TO ymes_app;

COMMIT;

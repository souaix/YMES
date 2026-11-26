-- RT603/RT604 shared WIP lot log source table.
-- Exact schema and indexes are in database/reference/mestest_wip_lot_log_create.sql,
-- based on the read-only MESTEST Oracle DDL in the QC409 dependency reference.
-- Create the empty table only; no MES business rows are loaded or modified.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WIP lot log migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WIP lot log migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WIP lot log migration';
    END IF;
    IF to_regclass('"YMES".tblwiplotlog') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwiplotlog already exists; compare its live structure before migrating';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblwiplotlog'
          AND n.nspname<>'YMES'
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An object named tblwiplotlog exists outside YMES; compare it before migrating';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'pk_tblwiplotlog','i_tblwiplotlog_01','i_tblwiplotlog_02'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            WHERE lower(c.relname)=object_name AND c.relkind='i'
        ) THEN
            RAISE EXCEPTION 'WIP lot log index name % already exists; inspect before migrating', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblwiplotlog (
    lotserial varchar(55) NOT NULL,
    lotno varchar(50),
    opno varchar(20),
    status numeric(1,0) DEFAULT 0,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    nodeid varchar(50),
    baselotno varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    preopendtime timestamp without time zone,
    areano varchar(20),
    moduleno varchar(50),
    moduleversion varchar(3),
    inputqty numeric(12,4),
    inputunitno varchar(30),
    loggroupserial varchar(50),
    opgroupno varchar(20) DEFAULT 'N/A',
    pdlineno varchar(50) DEFAULT 'N/A',
    defectqty numeric(12,4) DEFAULT 0,
    CONSTRAINT pk_tblwiplotlog PRIMARY KEY (lotserial)
);

CREATE INDEX i_tblwiplotlog_01 ON "YMES".tblwiplotlog (loggroupserial);
CREATE INDEX i_tblwiplotlog_02 ON "YMES".tblwiplotlog (lotno);

COMMENT ON TABLE "YMES".tblwiplotlog IS
    'MES Oracle source: MESTEST.TBLWIPLOTLOG; see source DDL and CREATE-only reference';

GRANT SELECT ON "YMES".tblwiplotlog TO ymes_app;

COMMIT;

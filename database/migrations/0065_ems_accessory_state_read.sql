-- ES405 read-only schema support, sourced only from checked MESTEST Oracle DDL.
-- Read-only catalog audit (2026-11-23): YMES.tblemsaccessorystate and
-- YMES.tbleqpaccessorylife are absent in every schema; YMES.tbleqpaccstatebasis
-- exists and has matching source columns/types/PK, but ymes_app lacks SELECT.
-- Existing state-basis defaults differ from Oracle (statetype/datastamp); this
-- read-only migration does not alter its schema or data.
-- No reasons table is created because Oracle source DDL/metadata is unavailable.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing EMS schema migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing EMS schema migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing EMS schema migration';
    END IF;
    IF to_regclass('"YMES".tbleqpaccstatebasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tbleqpaccstatebasis is missing; refusing EMS schema migration';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind IN ('r', 'p', 'v', 'm')
          AND c.relname IN ('tblemsaccessorystate', 'tbleqpaccessorylife')
    ) THEN
        RAISE EXCEPTION 'An EMS migration target already exists in a schema; compare it before proceeding';
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_attribute a
        WHERE a.attrelid = to_regclass('"YMES".tbleqpaccstatebasis')
          AND a.attname = 'statename' AND NOT a.attisdropped
    ) OR NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_attribute a
        WHERE a.attrelid = to_regclass('"YMES".tbleqpaccstatebasis')
          AND a.attname = 'accessorystate' AND NOT a.attisdropped
    ) THEN
        RAISE EXCEPTION 'YMES.tbleqpaccstatebasis is incompatible; refusing EMS schema migration';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblemsaccessorystate (
    accserialno varchar(20) NOT NULL,
    accessoryno varchar(50) NOT NULL,
    accessoryversion varchar(5) NOT NULL,
    accessorystate numeric(2,0) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    applytime timestamp without time zone,
    accumulateqty numeric(12,4) DEFAULT 0,
    accspareqty numeric(8,0) DEFAULT 0,
    accrepairqty numeric(8,0) DEFAULT 0,
    acctotalusedqty numeric(12,4) DEFAULT 0,
    timecyclestart timestamp without time zone,
    CONSTRAINT pk_tblemsaccessorystate PRIMARY KEY (accessoryno, accessoryversion)
);
COMMENT ON TABLE "YMES".tblemsaccessorystate IS 'MES Oracle source: MESTEST.TBLEMSACCESSORYSTATE';

CREATE TABLE "YMES".tbleqpaccessorylife (
    accessory varchar(50) NOT NULL,
    lifemanage numeric(2,0) DEFAULT 0 NOT NULL,
    lifescript varchar(200),
    repairable numeric(2,0),
    repaircycle numeric(8,0),
    scraptype numeric(2,0),
    scraplife numeric(8,0),
    lifetolerance numeric(2,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    timecycle numeric(8,0),
    alerttime numeric(8,0),
    alertcount numeric(8,0),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tbleqpaccessorylife PRIMARY KEY (accessory)
);
COMMENT ON TABLE "YMES".tbleqpaccessorylife IS 'MES Oracle source: MESTEST.TBLEQPACCESSORYLIFE';

GRANT SELECT ON "YMES".tblemsaccessorystate,
                "YMES".tbleqpaccessorylife,
                "YMES".tbleqpaccstatebasis
TO ymes_app;

COMMIT;

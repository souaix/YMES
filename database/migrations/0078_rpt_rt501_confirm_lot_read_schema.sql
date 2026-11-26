-- RT501 status predicates require the MES scrap/fail-bin source structures.
-- Source: MESTEST DDL and PostgreSQL CREATE-only references listed in the
-- RT501 module document. Creates empty structures; no MES business rows.
-- ymes_app receives SELECT only. Two extra supporting indexes serve the
-- BaseLotNo existence checks against inventory and scrap sources.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing RT501 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing RT501 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing RT501 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblinvwip_scrap','tblinvwip_failbin','tblinvwip_componentscrap'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name
              AND n.nspname<>'YMES'
              AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;

    FOREACH object_name IN ARRAY ARRAY[
        'i_tblinvwip_componentscrap_01','i_tblinvwip_failbin_01',
        'i_rt501_invsemi_baselotno','i_rt501_wipscrap_baselotno'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            WHERE lower(c.relname)=object_name AND c.relkind='i'
        ) THEN
            RAISE EXCEPTION 'Index name % already exists; inspect before migrating', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblinvwip_scrap (
    inventoryno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    unitno varchar(30) NOT NULL,
    scrapqty numeric(12,4) DEFAULT 0,
    baselotno varchar(50) NOT NULL,
    lossqty numeric(12,4) DEFAULT 0,
    inputdate timestamp without time zone,
    loggroupserial varchar(50),
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwip_scrap PRIMARY KEY (inventoryno, lotserial, locatorno)
);

CREATE TABLE "YMES".tblinvwip_componentscrap (
    inventoryno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    unitno varchar(30) NOT NULL,
    goodqty numeric(12,4) NOT NULL,
    scrapqty numeric(12,4) NOT NULL,
    baselotno varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    s_color varchar(100),
    s_cycleno varchar(100),
    s_mocvdeqpno varchar(100),
    s_runno varchar(100),
    s_substrate_id varchar(100),
    s_wafersize varchar(100),
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwip_componentscrap PRIMARY KEY (inventoryno, lotserial, componentno, locatorno)
);

CREATE TABLE "YMES".tblinvwip_failbin (
    inventoryno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    opno varchar(20) NOT NULL,
    unitno varchar(30) NOT NULL,
    baselotno varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    bin1 numeric(6,0) DEFAULT 0,
    bin2 numeric(6,0) DEFAULT 0,
    bin3 numeric(6,0) DEFAULT 0,
    bin4 numeric(6,0) DEFAULT 0,
    bin5 numeric(6,0) DEFAULT 0,
    bin6 numeric(6,0) DEFAULT 0,
    bin7 numeric(6,0) DEFAULT 0,
    bin8 numeric(6,0) DEFAULT 0,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwip_failbin PRIMARY KEY (inventoryno, lotserial, locatorno)
);

-- Source indexes from WP603 plus API access-path indexes on BaseLotNo.
CREATE INDEX i_tblinvwip_componentscrap_01
    ON "YMES".tblinvwip_componentscrap (baselotno);
CREATE INDEX i_tblinvwip_failbin_01
    ON "YMES".tblinvwip_failbin (baselotno);
CREATE INDEX i_rt501_invsemi_baselotno
    ON "YMES".tblinvwipinventory_semi (baselotno);
CREATE INDEX i_rt501_wipscrap_baselotno
    ON "YMES".tblinvwip_scrap (baselotno);

COMMENT ON TABLE "YMES".tblinvwip_scrap IS
    'MES Oracle source: MESTEST.TBLINVWIP_SCRAP';
COMMENT ON TABLE "YMES".tblinvwip_componentscrap IS
    'MES Oracle source: MESTEST.TBLINVWIP_COMPONENTSCRAP';
COMMENT ON TABLE "YMES".tblinvwip_failbin IS
    'MES Oracle source: MESTEST.TBLINVWIP_FAILBIN';

GRANT SELECT ON "YMES".tblinvwip_scrap,
    "YMES".tblinvwip_componentscrap,
    "YMES".tblinvwip_failbin TO ymes_app;

COMMIT;

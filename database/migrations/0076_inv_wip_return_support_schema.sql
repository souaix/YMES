-- Verified IV304 source/support tables whose PostgreSQL CREATE-only references
-- already exist in this repository. This does not create IV304 return or RAW
-- destination tables; those still require company-supplied source DDL/metadata.
-- No business rows are imported or created.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing IV304 support migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing IV304 support migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing IV304 support migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblinvwipinventory_raw','tblinvsemiinventory',
        'tblinvsemiinventorycomponent','tbloemomaterialstate',
        'tbloemomtlstate_component','tbloemocomponent'
    ] LOOP
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

CREATE TABLE "YMES".tblinvwipinventory_raw (
    inventoryno varchar(20) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(500) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(16,8) NOT NULL,
    materialtype varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    expiredate timestamp without time zone,
    opendate timestamp without time zone,
    frommateriallotno varchar(500),
    acclimationdate timestamp without time zone,
    manufacturedate timestamp without time zone,
    gracedate timestamp without time zone,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    frozendate timestamp without time zone,
    executor varchar(10),
    gumtypestatus numeric(1,0),
    acctimes numeric(2,0),
    frozentimes numeric(2,0),
    vendorno varchar(50),
    xx_status numeric(1,0),
    sap_charg varchar(20),
    sap_aufnr varchar(25),
    sap_zzsublot varchar(30),
    sap_bwart varchar(3),
    xx_createdate timestamp without time zone,
    xx_if_matr varchar(3),
    xx_closeuser varchar(15),
    xx_recycle_date timestamp without time zone,
    xx_receiveuser varchar(15),
    xx_remain_amt numeric(38,5),
    xx_recycleuser varchar(15),
    xx_datasource varchar(10),
    xx_id numeric,
    xx_if_irno varchar(1) DEFAULT 'N',
    xx_acclimationdate_end timestamp without time zone,
    CONSTRAINT pk_tblinvwipinventory_raw PRIMARY KEY (inventoryno, materialno, materiallotno, locatorno)
);

CREATE TABLE "YMES".tblinvsemiinventory (
    inventoryno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    unitno varchar(30),
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    locqty numeric(12,4) NOT NULL,
    belongtotype numeric(1,0) DEFAULT -1,
    belongtono varchar(50),
    inputdate timestamp without time zone,
    baselotno varchar(50),
    semiinno varchar(20),
    CONSTRAINT pk_tblinvsemiinventory PRIMARY KEY (inventoryno, lotno, locatorno)
);

CREATE TABLE "YMES".tblinvsemiinventorycomponent (
    inventoryno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    bookingflag numeric(1,0) DEFAULT 0,
    s_fixid varchar(75),
    componentlotno varchar(50),
    s_lasermark varchar(50),
    CONSTRAINT pk_invsemiinventorycomponent PRIMARY KEY (inventoryno, lotno, locatorno, componentno)
);

CREATE TABLE "YMES".tbloemomaterialstate (
    mono varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(16,8) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialtype varchar(50),
    inputdate timestamp without time zone,
    substitutematerialno varchar(50) DEFAULT 'N/A' NOT NULL,
    substitutemateriallevel numeric(1,0) DEFAULT 0,
    substitutematerialqty numeric(16,8),
    description varchar(200),
    expiredate timestamp without time zone,
    opendate timestamp without time zone,
    manufacturedate timestamp without time zone,
    gracedate timestamp without time zone,
    CONSTRAINT pk_tbloemomaterialstate PRIMARY KEY (mono, materialno, materiallotno, substitutematerialno)
);

CREATE TABLE "YMES".tbloemomtlstate_component (
    mono varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    unitno varchar(30) NOT NULL,
    componentno varchar(50) NOT NULL,
    componentlotno varchar(50),
    lasermark varchar(50),
    cassetteno varchar(50),
    slotno numeric(5,0),
    goodqty numeric(12,4),
    useflag numeric(1,0) DEFAULT 0 NOT NULL,
    compunitno varchar(30),
    scrapqty numeric(12,4) DEFAULT 0,
    bookingflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tbloemomtlstate_component PRIMARY KEY (mono, materialno, materiallotno, componentno)
);

CREATE TABLE "YMES".tbloemocomponent (
    mono varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    unitno varchar(30),
    releasestate numeric(1,0) DEFAULT 0,
    ex_mocomp1 varchar(50),
    ex_mocomp2 varchar(50),
    inventoryno varchar(20),
    materialno varchar(50),
    materiallotno varchar(50),
    slotno varchar(3),
    vendorno varchar(50),
    componentlotno varchar(50),
    s_lasermark varchar(50),
    waferidtext varchar(20),
    inventorytype numeric(2,0),
    b_cassetteno varchar(50),
    b_orgcomponentno varchar(50),
    scrapqty numeric(12,4) DEFAULT 0,
    bookingflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tbloemocomponent PRIMARY KEY (mono, componentno)
);

COMMENT ON TABLE "YMES".tblinvwipinventory_raw IS 'MES Oracle source: MESTEST.TBLINVWIPINVENTORY_RAW (WP201)';
COMMENT ON TABLE "YMES".tblinvsemiinventory IS 'MES Oracle source: MESTEST.TBLINVSEMIINVENTORY (WP201)';
COMMENT ON TABLE "YMES".tblinvsemiinventorycomponent IS 'MES Oracle source: MESTEST.TBLINVSEMIINVENTORYCOMPONENT (WP604)';
COMMENT ON TABLE "YMES".tbloemomaterialstate IS 'MES Oracle source: MESTEST.TBLOEMOMATERIALSTATE (OE material reference)';
COMMENT ON TABLE "YMES".tbloemomtlstate_component IS 'MES Oracle source: MESTEST.TBLOEMOMTLSTATE_COMPONENT (OE component reference)';
COMMENT ON TABLE "YMES".tbloemocomponent IS 'MES Oracle source: MESTEST.TBLOEMOCOMPONENT (OE work-order reference)';

-- These tables are empty source/target foundations. Expose reads only until the
-- IV304 writer is implemented with explicit transaction and permission scope.
GRANT SELECT ON "YMES".tblinvwipinventory_raw,"YMES".tblinvsemiinventory,
    "YMES".tblinvsemiinventorycomponent,"YMES".tbloemomaterialstate,
    "YMES".tbloemomtlstate_component,"YMES".tbloemocomponent TO ymes_app;

COMMIT;

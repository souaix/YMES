-- IV502 draft and its verified inventory / packaging dependencies.
-- Definitions are copied from the referenced PostgreSQL CREATE-only files;
-- TBLWIPGOODBINSTATE is derived from its verified Oracle MESTEST source DDL.
-- No business rows are created or imported.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing IV502 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing IV502 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing IV502 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblinvfgdinbasis','tblinvfgdindetail','tblinvfgdincomponent',
        'tblinvsemiinbasis','tblinvsemiindetail','tblinvsemiincomponent',
        'tblinvwipinventory_semi','tblinvwipinventory_component',
        'tblinvfgdinventory','tblinvfgdinventorycomponent',
        'tblwipmolinepackrelation','tblprdpackingrulebasis','tblsysbin',
        'tblwipreversehistory','tblwipgoodbinstate'
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

CREATE TABLE "YMES".tblinvfgdinbasis (
    fgdinno varchar(20) NOT NULL,
    state numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone NOT NULL,
    inventoryno varchar(20) NOT NULL,
    inputdate timestamp without time zone,
    source numeric(2,0) NOT NULL,
    frominventoryno varchar(20),
    reviser varchar(10),
    revisedate timestamp without time zone,
    orgstate numeric(1,0),
    canceluser varchar(10),
    canceldate timestamp without time zone,
    erpno varchar(20),
    toerp numeric(1,0) DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblinvfgdinbasis PRIMARY KEY (fgdinno)
);

CREATE TABLE "YMES".tblinvfgdindetail (
    fgdinno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    lotno varchar(50) NOT NULL,
    qty numeric(14,6) NOT NULL,
    unitno varchar(30) NOT NULL,
    locatorno varchar(20) NOT NULL,
    baselotno varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    frominventoryno varchar(20),
    CONSTRAINT pk_tblinvfgdindetail PRIMARY KEY (fgdinno, lotno, locatorno)
);

CREATE TABLE "YMES".tblinvfgdincomponent (
    fgdinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    CONSTRAINT pk_tblinvfgdincomponent PRIMARY KEY (fgdinno, lotno, componentno)
);

CREATE TABLE "YMES".tblinvsemiinbasis (
    semiinno varchar(20) NOT NULL,
    state numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    inventoryno varchar(20) NOT NULL,
    inputdate timestamp without time zone,
    source numeric(2,0) NOT NULL,
    frominventoryno varchar(20),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblinvsemiinbasis PRIMARY KEY (semiinno)
);

CREATE TABLE "YMES".tblinvsemiindetail (
    semiinno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    lotno varchar(50) NOT NULL,
    qty numeric(14,6) NOT NULL,
    unitno varchar(30) NOT NULL,
    locatorno varchar(20) NOT NULL,
    baselotno varchar(50) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblinvsemiindetail PRIMARY KEY (semiinno, lotno, locatorno)
);

CREATE TABLE "YMES".tblinvsemiincomponent (
    semiinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    CONSTRAINT pk_tblinvsemiincomponent PRIMARY KEY (semiinno, lotno, componentno)
);

CREATE TABLE "YMES".tblinvwipinventory_semi (
    inventoryno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    lotno varchar(50) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(14,6) NOT NULL,
    inputdate timestamp without time zone,
    baselotno varchar(50),
    boughten numeric(1,0) NOT NULL,
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    productversion varchar(5),
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwipinventory_semi PRIMARY KEY (inventoryno, lotno, locatorno)
);

CREATE TABLE "YMES".tblinvwipinventory_component (
    inventoryno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    bookingflag numeric(1,0) DEFAULT 0,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    componentlotno varchar(50),
    vendorno varchar(50),
    s_lasermark varchar(50),
    b_cassetteno varchar(50),
    slotno varchar(3),
    CONSTRAINT pk_tblinvwipinventory_component PRIMARY KEY (inventoryno, lotno, componentno, locatorno)
);

CREATE TABLE "YMES".tblinvfgdinventory (
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
    fgdinno varchar(20),
    CONSTRAINT pk_tblinvfgdinventory PRIMARY KEY (inventoryno, lotno, locatorno)
);

CREATE TABLE "YMES".tblinvfgdinventorycomponent (
    inventoryno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    bookingflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblinvfgdinventorycomponent PRIMARY KEY (inventoryno, lotno, locatorno, componentno)
);

CREATE TABLE "YMES".tblwipmolinepackrelation (
    from_packingno varchar(50) NOT NULL,
    from_packinglevel numeric(1,0) NOT NULL,
    to_packingno varchar(50) NOT NULL,
    to_packinglevel numeric(1,0) NOT NULL,
    mono varchar(50)
);
CREATE INDEX i_tblwipmolinepackrelation_01 ON "YMES".tblwipmolinepackrelation (to_packingno);
CREATE INDEX i_tblwipmolinepackrelation_02 ON "YMES".tblwipmolinepackrelation (from_packingno);

CREATE TABLE "YMES".tblprdpackingrulebasis (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    curversion numeric(1,0) DEFAULT 0 NOT NULL,
    levelmax numeric(1,0) NOT NULL,
    l1_name varchar(50) NOT NULL,
    l1_qty numeric(12,4) NOT NULL,
    l1_soppath varchar(500),
    l2_name varchar(50),
    l2_qty numeric(12,4),
    l2_soppath varchar(500),
    l3_name varchar(50),
    l3_qty numeric(12,4),
    l3_soppath varchar(500),
    l4_name varchar(50),
    l4_qty numeric(12,4),
    l4_soppath varchar(500),
    l5_name varchar(50),
    l5_qty numeric(12,4),
    l5_soppath varchar(500),
    l6_name varchar(50),
    l6_qty numeric(12,4),
    l6_soppath varchar(500),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    l1_unitno varchar(30) NOT NULL,
    l2_unitno varchar(30),
    l3_unitno varchar(30),
    l4_unitno varchar(30),
    l5_unitno varchar(30),
    l6_unitno varchar(30),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    l1_num numeric(3,0),
    l2_num numeric(3,0),
    l3_num numeric(3,0),
    l4_num numeric(3,0),
    l5_num numeric(3,0),
    l6_num numeric(3,0),
    inlinepackingflag numeric(1,0) DEFAULT 1 NOT NULL,
    CONSTRAINT pk_tblprdpackingrulebasis PRIMARY KEY (packingruleno, packingruleversion)
);

CREATE TABLE "YMES".tblsysbin (
    binno varchar(10) NOT NULL,
    binorder numeric(3,0) DEFAULT 99,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblsysbin PRIMARY KEY (binno)
);

CREATE TABLE "YMES".tblwipreversehistory (
    lotno varchar(50) NOT NULL,
    reverseid numeric(6,0) NOT NULL,
    reversetype numeric(2,0) NOT NULL,
    lotserial varchar(55) NOT NULL,
    opno varchar(20) NOT NULL,
    commitstate numeric(1,0) NOT NULL DEFAULT 0,
    orglotno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    commitdesc varchar(255),
    CONSTRAINT pk_tblwipreversehistory PRIMARY KEY (lotno, reverseid)
);
CREATE INDEX i_tblwipreversehistory_01 ON "YMES".tblwipreversehistory (lotserial);

CREATE TABLE "YMES".tblwipgoodbinstate (
    lotno varchar(50) NOT NULL,
    lotserial varchar(55),
    bin1 numeric(6,0) DEFAULT 0,
    bin2 numeric(6,0) DEFAULT 0,
    bin3 numeric(6,0) DEFAULT 0,
    bin4 numeric(6,0) DEFAULT 0,
    bin5 numeric(6,0) DEFAULT 0,
    bin6 numeric(6,0) DEFAULT 0,
    bin7 numeric(6,0) DEFAULT 0,
    bin8 numeric(6,0) DEFAULT 0,
    CONSTRAINT pk_tblwipgoodbinstate PRIMARY KEY (lotno)
);

COMMENT ON TABLE "YMES".tblinvfgdinbasis IS 'MES Oracle source: MESTEST.TBLINVFGDINBASIS (WP605)';
COMMENT ON TABLE "YMES".tblinvfgdindetail IS 'MES Oracle source: MESTEST.TBLINVFGDINDETAIL (WP605)';
COMMENT ON TABLE "YMES".tblinvfgdincomponent IS 'MES Oracle source: MESTEST.TBLINVFGDINCOMPONENT (WP605)';
COMMENT ON TABLE "YMES".tblinvsemiinbasis IS 'MES Oracle source: MESTEST.TBLINVSEMIINBASIS (WP604)';
COMMENT ON TABLE "YMES".tblinvsemiindetail IS 'MES Oracle source: MESTEST.TBLINVSEMIINDETAIL (WP604)';
COMMENT ON TABLE "YMES".tblinvsemiincomponent IS 'MES Oracle source: MESTEST.TBLINVSEMIINCOMPONENT (WP604)';
COMMENT ON TABLE "YMES".tblinvwipinventory_semi IS 'MES Oracle source: MESTEST.TBLINVWIPINVENTORY_SEMI (WP201)';
COMMENT ON TABLE "YMES".tblinvwipinventory_component IS 'MES Oracle source: MESTEST.TBLINVWIPINVENTORY_COMPONENT (WP603)';
COMMENT ON TABLE "YMES".tblinvfgdinventory IS 'MES Oracle source: MESTEST.TBLINVFGDINVENTORY (WP201)';
COMMENT ON TABLE "YMES".tblinvfgdinventorycomponent IS 'MES Oracle source: MESTEST.TBLINVFGDINVENTORYCOMPONENT (WP605)';
COMMENT ON TABLE "YMES".tblwipmolinepackrelation IS 'MES Oracle source: MESTEST.TBLWIPMOLINEPACKRELATION (WP601)';
COMMENT ON TABLE "YMES".tblprdpackingrulebasis IS 'MES Oracle source: MESTEST.TBLPRDPACKINGRULEBASIS (PRD112)';
COMMENT ON TABLE "YMES".tblsysbin IS 'MES Oracle source: MESTEST.TBLSYSBIN (PRD Bin)';
COMMENT ON TABLE "YMES".tblwipreversehistory IS 'MES Oracle source: MESTEST.TBLWIPREVERSEHISTORY (WP215)';
COMMENT ON TABLE "YMES".tblwipgoodbinstate IS 'MES Oracle source: MESTEST.TBLWIPGOODBINSTATE (QC409 dependency)';

GRANT SELECT ON "YMES".tblinvfgdinbasis,"YMES".tblinvfgdindetail,"YMES".tblinvfgdincomponent,
    "YMES".tblinvsemiinbasis,"YMES".tblinvsemiindetail,"YMES".tblinvsemiincomponent,
    "YMES".tblinvwipinventory_semi,"YMES".tblinvwipinventory_component,
    "YMES".tblinvfgdinventory,"YMES".tblinvfgdinventorycomponent,
    "YMES".tblwipmolinepackrelation,"YMES".tblprdpackingrulebasis,"YMES".tblsysbin,
    "YMES".tblwipreversehistory,"YMES".tblwipgoodbinstate TO ymes_app;
GRANT INSERT, UPDATE ON "YMES".tblinvfgdinbasis,"YMES".tblinvsemiinbasis TO ymes_app;
GRANT INSERT, UPDATE, DELETE ON "YMES".tblinvfgdindetail,"YMES".tblinvfgdincomponent,
    "YMES".tblinvsemiindetail,"YMES".tblinvsemiincomponent TO ymes_app;
GRANT UPDATE ON "YMES".tblinvwipinventory_semi,"YMES".tblinvwipinventory_component,
    "YMES".tblwipreversehistory TO ymes_app;

COMMIT;

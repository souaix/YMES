-- PostgreSQL CREATE-only reference for WP603.
-- Source: Oracle MESTEST metadata, see wp603_source_ddl.sql.
-- Compare live YMES schema and migration history before making any migration.
-- This file is not a deployment script and must not replace existing objects.

CREATE TABLE IF NOT EXISTS ymes.tblinvwipinventory_component (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvwip_componentscrap (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvwip_failbin (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvwip_incoming (
    inventoryno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(12,4) NOT NULL,
    baselotno varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    frombuno varchar(50),
    productno varchar(50),
    productversion varchar(5),
    customerno varchar(50),
    priority numeric(2,0),
    motypeno numeric(2,0),
    releasestate numeric(1,0),
    CONSTRAINT pk_tblinvwip_incoming PRIMARY KEY (lotserial)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipcont_error (
    lotserial varchar(55),
    opno varchar(20),
    errorno varchar(20),
    errorlevel numeric(1,0),
    errorqty numeric(12,4),
    componentno varchar(50),
    loggroupserial varchar(50),
    reasontype numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    reasonbase varchar(20),
    judgestate numeric(1,0)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvscrinbasis (
    scrinno varchar(20) NOT NULL,
    state numeric(1,0) NOT NULL,
    description varchar(50),
    creator varchar(50),
    createdate timestamp without time zone,
    inventoryno varchar(20) NOT NULL,
    inputdate timestamp without time zone,
    source numeric(2,0) NOT NULL,
    frominventoryno varchar(20) NOT NULL,
    orgstate numeric(1,0),
    canceluser varchar(10),
    canceldate timestamp without time zone,
    erpno varchar(20),
    toerp numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblinvscrinbasis PRIMARY KEY (scrinno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvscrindetail (
    scrinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    opno varchar(20) NOT NULL,
    scrapqty numeric(12,4) DEFAULT 0,
    lossqty numeric(12,4) DEFAULT 0,
    unitno varchar(30) NOT NULL,
    baselotno varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    source numeric(2,0) NOT NULL,
    frominventoryno varchar(20),
    CONSTRAINT pk_tblinvscrindetail PRIMARY KEY (scrinno, lotserial, source)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvscrincomponent (
    scrinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    componentno varchar(50) NOT NULL,
    scrapqty numeric(12,4) DEFAULT 0,
    goodqty numeric(12,4) DEFAULT 0,
    unitno varchar(30) NOT NULL,
    CONSTRAINT pk_tblinvscrincomponent PRIMARY KEY (scrinno, lotserial, componentno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvscrinfailbin (
    scrinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    bin1 numeric(6,0) DEFAULT 0,
    bin2 numeric(6,0) DEFAULT 0,
    bin3 numeric(6,0) DEFAULT 0,
    bin4 numeric(6,0) DEFAULT 0,
    bin5 numeric(6,0) DEFAULT 0,
    bin6 numeric(6,0) DEFAULT 0,
    bin7 numeric(6,0) DEFAULT 0,
    bin8 numeric(6,0) DEFAULT 0,
    CONSTRAINT pk_tblinvscrinfailbin PRIMARY KEY (scrinno, lotserial)
);

-- Standalone Oracle indexes in addition to each table's primary key.
CREATE UNIQUE INDEX IF NOT EXISTS pk_tblwipinv_component
    ON ymes.tblinvwipinventory_component (inventoryno, lotno, componentno);
CREATE INDEX IF NOT EXISTS i_tblinvwip_componentscrap_01
    ON ymes.tblinvwip_componentscrap (baselotno);
CREATE INDEX IF NOT EXISTS i_tblinvwip_failbin_01
    ON ymes.tblinvwip_failbin (baselotno);
CREATE INDEX IF NOT EXISTS i_tblwipcont_error_01
    ON ymes.tblwipcont_error (loggroupserial, errorno);
CREATE INDEX IF NOT EXISTS i_tblwipcont_error_02
    ON ymes.tblwipcont_error (lotserial);

COMMENT ON TABLE ymes.tblinvwipinventory_component IS 'MES Oracle source: MESTEST.TBLINVWIPINVENTORY_COMPONENT';
COMMENT ON TABLE ymes.tblinvwip_componentscrap IS 'MES Oracle source: MESTEST.TBLINVWIP_COMPONENTSCRAP';
COMMENT ON TABLE ymes.tblinvwip_failbin IS 'MES Oracle source: MESTEST.TBLINVWIP_FAILBIN';
COMMENT ON TABLE ymes.tblinvwip_incoming IS 'MES Oracle source: MESTEST.TBLINVWIP_INCOMING';
COMMENT ON TABLE ymes.tblwipcont_error IS 'MES Oracle source: MESTEST.TBLWIPCONT_ERROR; no primary key declared';
COMMENT ON TABLE ymes.tblinvscrinbasis IS 'MES Oracle source: MESTEST.TBLINVSCRINBASIS';
COMMENT ON TABLE ymes.tblinvscrindetail IS 'MES Oracle source: MESTEST.TBLINVSCRINDETAIL';
COMMENT ON TABLE ymes.tblinvscrincomponent IS 'MES Oracle source: MESTEST.TBLINVSCRINCOMPONENT';
COMMENT ON TABLE ymes.tblinvscrinfailbin IS 'MES Oracle source: MESTEST.TBLINVSCRINFAILBIN';

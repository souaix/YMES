-- PostgreSQL CREATE-only reference for WP605.
-- Oracle source: database/reference/mestest_wip_wp605_source_ddl.sql
-- Do not execute as a replacement for existing YMES objects. Home PC must compare
-- PostgreSQL live schema and migration history before writing a forward migration.
-- Column nullability, defaults and primary keys follow the Oracle MESTEST metadata.
-- Oracle reported no foreign keys, triggers, or non-PK indexes on these six tables.

CREATE TABLE IF NOT EXISTS ymes.tblinvfgdinventorycomponent (
    inventoryno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    bookingflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblinvfgdinventorycomponent
        PRIMARY KEY (inventoryno, lotno, locatorno, componentno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvfgdinbasis (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvfgdindetail (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvfgdincomponent (
    fgdinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    CONSTRAINT pk_tblinvfgdincomponent PRIMARY KEY (fgdinno, lotno, componentno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvtransactionstate (
    transactionid varchar(50) NOT NULL,
    source numeric(2,0) NOT NULL,
    ticketno varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblinvtransactionstate PRIMARY KEY (transactionid)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvtransactionlog (
    transactionid varchar(50) NOT NULL,
    source numeric(2,0) NOT NULL,
    ticketno varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblinvtransactionlog PRIMARY KEY (transactionid)
);

COMMENT ON TABLE ymes.tblinvfgdinventorycomponent IS 'MES Oracle source: MESTEST.TBLINVFGDINVENTORYCOMPONENT';
COMMENT ON TABLE ymes.tblinvfgdinbasis IS 'MES Oracle source: MESTEST.TBLINVFGDINBASIS';
COMMENT ON TABLE ymes.tblinvfgdindetail IS 'MES Oracle source: MESTEST.TBLINVFGDINDETAIL';
COMMENT ON TABLE ymes.tblinvfgdincomponent IS 'MES Oracle source: MESTEST.TBLINVFGDINCOMPONENT';
COMMENT ON TABLE ymes.tblinvtransactionstate IS 'MES Oracle source: MESTEST.TBLINVTRANSACTIONSTATE';
COMMENT ON TABLE ymes.tblinvtransactionlog IS 'MES Oracle source: MESTEST.TBLINVTRANSACTIONLOG';

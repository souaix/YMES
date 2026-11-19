-- PostgreSQL CREATE-only reference for WP604.
-- Source: Oracle MESTEST metadata; see wp604_source_ddl.sql.
-- Compare the YMES live schema and migration history before creating anything.
-- Do not run this file as a deployment migration or replace existing tables.

CREATE TABLE IF NOT EXISTS ymes.tblinvsemiinventorycomponent (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvsemiinbasis (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvsemiindetail (
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

CREATE TABLE IF NOT EXISTS ymes.tblinvsemiincomponent (
    semiinno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    CONSTRAINT pk_tblinvsemiincomponent PRIMARY KEY (semiinno, lotno, componentno)
);

COMMENT ON TABLE ymes.tblinvsemiinventorycomponent IS 'MES Oracle source: MESTEST.TBLINVSEMIINVENTORYCOMPONENT';
COMMENT ON TABLE ymes.tblinvsemiinbasis IS 'MES Oracle source: MESTEST.TBLINVSEMIINBASIS';
COMMENT ON TABLE ymes.tblinvsemiindetail IS 'MES Oracle source: MESTEST.TBLINVSEMIINDETAIL';
COMMENT ON TABLE ymes.tblinvsemiincomponent IS 'MES Oracle source: MESTEST.TBLINVSEMIINCOMPONENT';

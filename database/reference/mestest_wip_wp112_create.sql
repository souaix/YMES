-- PostgreSQL CREATE-only reference from Oracle MESTEST WP112 direct tables.
-- Verify YMES live schema/history before writing a forward migration. Do not run against an existing schema as a blind migration.
-- ZPPT0025A/B/C1 below reflect MESTEST local table DDL; the original Service writes THSAP tables through an Oracle DB link.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwipcont_materiallot (
    lotserial varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    lotqty numeric(16,8) NOT NULL,
    equipmentno varchar(50),
    loggroupserial varchar(50) NOT NULL,
    undistributeqty numeric(16,8),
    createdate timestamp without time zone NOT NULL DEFAULT LOCALTIMESTAMP,
    ex_materiallot1 varchar(20),
    ex_materiallot2 varchar(20),
    ex_materiallot3 varchar(20),
    eventtime timestamp without time zone,
    sap_charg varchar(10),
    scrap_rate numeric(8,5),
    xx_remark varchar(50),
    xx_dmr_flag varchar(1),
    xx_change_type varchar(40),
    xx_equipment varchar(50)
);
CREATE INDEX IF NOT EXISTS i_tblwipcont_materiallot_01 ON ymes.tblwipcont_materiallot (loggroupserial, materialno);
CREATE INDEX IF NOT EXISTS i_tblwipcont_materiallot_02 ON ymes.tblwipcont_materiallot (lotserial);

CREATE TABLE IF NOT EXISTS ymes.tblsysserialcreatemax (
    serialtype varchar(50) NOT NULL,
    serialstring varchar(50) NOT NULL,
    seriallength numeric(2,0) NOT NULL,
    decimaltype numeric(1,0) NOT NULL,
    maxserialno numeric(19,0),
    xx_bu varchar(50),
    CONSTRAINT pk_tblsysserialcreatemax PRIMARY KEY (serialtype, serialstring, seriallength, decimaltype)
);

-- Oracle source uses VARCHAR2(n CHAR), so the lengths here are characters, not ALL_TAB_COLUMNS.DATA_LENGTH bytes.
CREATE TABLE IF NOT EXISTS ymes.xx_tbloemo_os_bu1 (
    mandt varchar(3) NOT NULL,
    werks varchar(4) NOT NULL,
    aufnr varchar(12) NOT NULL,
    vornr varchar(4) NOT NULL,
    opno varchar(20),
    ltxa1 varchar(40),
    steus varchar(4),
    banfn varchar(10),
    bnfpo varchar(5),
    meinh varchar(3),
    CONSTRAINT xx_tbloemo_os_bu1_pk PRIMARY KEY (mandt, werks, aufnr, vornr)
);

CREATE TABLE IF NOT EXISTS ymes.zppt0025a (
    idbsno varchar(20) NOT NULL,
    werks varchar(4) NOT NULL,
    aufnr varchar(12) NOT NULL,
    vornr varchar(4) NOT NULL,
    rmzhl varchar(8) NOT NULL,
    rwkind varchar(1) NOT NULL,
    lmnga numeric(13,3),
    xmnga numeric(13,3),
    budat varchar(8),
    mandt varchar(3) NOT NULL,
    CONSTRAINT zppt0025a_pk PRIMARY KEY (mandt, idbsno, werks, aufnr, vornr, rmzhl, rwkind)
);

CREATE TABLE IF NOT EXISTS ymes.zppt0025b (
    idbsno varchar(20) NOT NULL,
    werks varchar(4) NOT NULL,
    aufnr varchar(12) NOT NULL,
    vornr varchar(4) NOT NULL,
    rmzhl varchar(8) NOT NULL,
    matnr varchar(18) NOT NULL,
    charg varchar(10) NOT NULL,
    mandt varchar(3) NOT NULL,
    CONSTRAINT zppt0025b_pk PRIMARY KEY (mandt, idbsno, werks, aufnr, vornr, rmzhl, matnr, charg)
);

CREATE TABLE IF NOT EXISTS ymes.zppt0025c1 (
    idbsno varchar(20) NOT NULL,
    werks varchar(4) NOT NULL,
    aufnr varchar(12) NOT NULL,
    vornr varchar(4) NOT NULL,
    rmzhl varchar(8) NOT NULL,
    exetyp varchar(1) NOT NULL,
    getdat timestamp without time zone,
    exedat timestamp without time zone,
    ststyp varchar(1),
    mandt varchar(3) NOT NULL,
    CONSTRAINT zppt0025c1_pk PRIMARY KEY (mandt, idbsno, werks, aufnr, vornr, rmzhl, exetyp)
);

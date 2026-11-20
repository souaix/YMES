-- Source: Oracle MESTEST DBMS_METADATA.GET_DDL, queried read-only on 2026-11-20.
-- Target: PostgreSQL schema ymes. Empty-object schema reference only; no MES business rows.
-- This file also covers three WIP/equipment side-effect tables absent from the YMES reference set.
-- Compare the YMES live schema and migration history before adding any migration.
-- Oracle source has no foreign keys or triggers for these tables. Do not invent a key for TBLWIPCONT_EQUIPMENT.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwipeqpqclistlog (
    qclistserial varchar(100) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    qclistno varchar(100) NOT NULL,
    lotserial varchar(55),
    loggroupserial varchar(55),
    qcresult numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    qctaskno varchar(30),
    insptype numeric(2,0),
    shiftno varchar(20),
    lotno varchar(50),
    inspstarttime varchar(20),
    qctaskstatus numeric(2,0),
    belongdate timestamp without time zone,
    planqctime timestamp without time zone,
    CONSTRAINT pk_tblwipeqpqclistlog PRIMARY KEY (qclistserial)
);
COMMENT ON TABLE ymes.tblwipeqpqclistlog IS 'MES Oracle source: MESTEST.TBLWIPEQPQCLISTLOG';
COMMENT ON COLUMN ymes.tblwipeqpqclistlog.qcresult IS '-1=partial, 0=pass, 1=fail; recalculated from QC details by the server';
COMMENT ON COLUMN ymes.tblwipeqpqclistlog.qctaskstatus IS 'Task status captured when the inspection task was completed';

CREATE TABLE IF NOT EXISTS ymes.tblwipeqpqclistdetail (
    qclistserial varchar(120) NOT NULL,
    qcitem varchar(1000) NOT NULL,
    qcresult numeric(1,0) NOT NULL,
    inputvalue varchar(50),
    stdvalue varchar(12),
    maxivalue varchar(12),
    minivalue varchar(12),
    inputdatacount numeric(2,0) DEFAULT 1 NOT NULL,
    qcorder numeric(2,0) DEFAULT -99 NOT NULL,
    qctype numeric(1,0),
    d01 varchar(50),
    d02 varchar(50),
    d03 varchar(50),
    d04 varchar(50),
    d05 varchar(50),
    d06 varchar(50),
    d07 varchar(50),
    d08 varchar(50),
    d09 varchar(50),
    d10 varchar(50),
    CONSTRAINT pk_tblwipeqpqclistdetail PRIMARY KEY (qclistserial, qcitem)
);
COMMENT ON TABLE ymes.tblwipeqpqclistdetail IS 'MES Oracle source: MESTEST.TBLWIPEQPQCLISTDETAIL';
COMMENT ON COLUMN ymes.tblwipeqpqclistdetail.qctype IS '0=exact standard, 1=range, 2=operator confirmation, 3=text input';
COMMENT ON COLUMN ymes.tblwipeqpqclistdetail.qcresult IS '-1=not completed, 0=pass, 1=fail';
COMMENT ON COLUMN ymes.tblwipeqpqclistdetail.inputdatacount IS 'WinForms supports one InputValue or up to ten D01-D10 measurements';

CREATE TABLE IF NOT EXISTS ymes.tblwipeqpqctask (
    qctaskno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    qctaskclass numeric(2,0) NOT NULL,
    insptype numeric(2,0) NOT NULL,
    shiftno varchar(20),
    lotno varchar(50),
    inspstarttime varchar(20),
    qcno varchar(100),
    memo varchar(255),
    status numeric(2,0) DEFAULT 0 NOT NULL,
    belongdate timestamp without time zone NOT NULL,
    planqctime timestamp without time zone,
    qcserialno varchar(120),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwipeqpqctask PRIMARY KEY (qctaskno)
);
COMMENT ON TABLE ymes.tblwipeqpqctask IS 'MES Oracle source: MESTEST.TBLWIPEQPQCTASK';
COMMENT ON COLUMN ymes.tblwipeqpqctask.status IS '0=waiting, 1=completed, 2=recheck';
COMMENT ON COLUMN ymes.tblwipeqpqctask.qctaskclass IS 'WP305 queries production inspection tasks with class 1';

CREATE TABLE IF NOT EXISTS ymes.tblemsequipmentstatelog (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    endtime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblemsequipmentstatelog PRIMARY KEY (eqpserialno)
);
CREATE INDEX IF NOT EXISTS i_equipmentstatelog_01 ON ymes.tblemsequipmentstatelog (starttime);
CREATE INDEX IF NOT EXISTS i_tblemsequipmentstatelog_01 ON ymes.tblemsequipmentstatelog (equipmentno, starttime, endtime);
COMMENT ON TABLE ymes.tblemsequipmentstatelog IS 'MES Oracle source: MESTEST.TBLEMSEQUIPMENTSTATELOG';

CREATE TABLE IF NOT EXISTS ymes.tblwipcont_equipment (
    lotserial varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50),
    recipegroup varchar(50),
    recipeversion numeric(2,0),
    equipmentclass varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    loadport numeric(2,0),
    loggroupserial varchar(50),
    inputqty numeric(12,4) DEFAULT 0,
    outputqty numeric(12,4) DEFAULT 0,
    eventtime timestamp without time zone,
    fromlotno varchar(50),
    fromloggroupserial varchar(50),
    erpflag numeric(1,0),
    summaryflag numeric(1,0),
    workreportid varchar(30),
    planfinishtime timestamp without time zone,
    chamberflag numeric(1,0) DEFAULT 0,
    s_batchid varchar(150)
);
CREATE INDEX IF NOT EXISTS i_tblwipcont_equipment_01 ON ymes.tblwipcont_equipment (loggroupserial, equipmentno);
CREATE INDEX IF NOT EXISTS i_tblwipcont_equipment_02 ON ymes.tblwipcont_equipment (lotserial);
COMMENT ON TABLE ymes.tblwipcont_equipment IS 'MES Oracle source: MESTEST.TBLWIPCONT_EQUIPMENT; source declares no primary key';

CREATE TABLE IF NOT EXISTS ymes.tblwiplotloginstate (
    datatype numeric(1,0) NOT NULL,
    datasource numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    userno varchar(10) NOT NULL,
    username varchar(50) NOT NULL,
    checkintime timestamp without time zone NOT NULL,
    cicreator varchar(10) NOT NULL,
    cicreatedate timestamp without time zone NOT NULL,
    checkouttime timestamp without time zone,
    cocreator varchar(10),
    cocreatedate timestamp without time zone,
    loginplaceno varchar(50) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    CONSTRAINT pk_tblwiplotloginstate PRIMARY KEY (lotno, opno, userno, checkintime, loginplaceno)
);
CREATE INDEX IF NOT EXISTS i_tblwiplotloginstate_01 ON ymes.tblwiplotloginstate (loggroupserial);
COMMENT ON TABLE ymes.tblwiplotloginstate IS 'MES Oracle source: MESTEST.TBLWIPLOTLOGINSTATE';

-- These tables have no source foreign keys or triggers. Primary-key indexes are represented by constraints;
-- the five non-unique indexes above reproduce the source index metadata.
-- WP305 can also use existing QC-list/equipment/WIP references; this file is not a PostgreSQL migration.

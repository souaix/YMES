-- PostgreSQL empty-table DDL converted from Oracle MESTEST DDL.
-- Source: database/reference/mestest_accessory_basis_source_ddl.sql
-- Schema only. Compare with the YMES target before applying; preserve existing extra columns.
CREATE SCHEMA IF NOT EXISTS "YMES";
SET search_path TO "YMES", public;

CREATE TABLE IF NOT EXISTS tbleqpaccessorybasis (
    accessoryno varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    vendorno varchar(50) NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    assetno varchar(50),
    accessoryversion varchar(3),
    curversion numeric(1,0) DEFAULT 0 NOT NULL,
    accessorycategory varchar(50),
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    xx_para1 varchar(100), xx_para2 varchar(100), xx_para3 varchar(100), xx_para4 varchar(100),
    xx_para5 varchar(100), xx_para6 varchar(100), xx_para7 varchar(100), xx_para8 varchar(100),
    xx_para9 varchar(100), xx_para10 varchar(100), xx_para11 varchar(100), xx_para12 varchar(100),
    xx_para13 varchar(100), xx_para14 varchar(100), xx_para15 varchar(100), xx_para16 varchar(100),
    xx_para17 varchar(100), xx_para18 varchar(100), xx_para19 varchar(100), xx_para20 varchar(100),
    xx_para21 varchar(100), xx_para22 varchar(100), xx_para23 varchar(100), xx_para24 varchar(100),
    getansno varchar(50),
    ansstatus varchar(20),
    acc_file_path1 varchar(500),
    acc_file_path2 varchar(500),
    acc_file_path3 varchar(500),
    accessorystate numeric(2,0) DEFAULT 0,
    getgroupno varchar(20),
    maintenancetime timestamp without time zone,
    begroupmainten varchar(1),
    CONSTRAINT n_tbleqpaccessorybasis_06 CHECK (accessoryversion IS NOT NULL),
    CONSTRAINT pk_tbleqpaccessorybasis PRIMARY KEY (accessoryno, accessoryversion)
);
CREATE INDEX IF NOT EXISTS idx_eqpacc_getgroupno ON tbleqpaccessorybasis (getgroupno);

CREATE TABLE IF NOT EXISTS tblemsaccessorystate (
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

CREATE TABLE IF NOT EXISTS tblemsaccessorystatelog (
    accserialno varchar(20) NOT NULL,
    accessoryno varchar(50) NOT NULL,
    accessoryversion varchar(5) NOT NULL,
    accessorystate numeric(2,0) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    endtime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    applytime timestamp without time zone,
    accumulateqty numeric(12,4) DEFAULT 0,
    timecyclestart timestamp without time zone,
    accapplyserialno varchar(20),
    CONSTRAINT pk_tblemsaccessorystatelog PRIMARY KEY (accserialno)
);

CREATE TABLE IF NOT EXISTS tbleqpaccstatecontrol (
    controltype varchar(50) NOT NULL,
    controlno varchar(50) NOT NULL,
    accessorystate numeric(2,0) NOT NULL,
    CONSTRAINT pk_tbleqpaccstatecontrol PRIMARY KEY (controltype, controlno)
);

CREATE TABLE IF NOT EXISTS tblemseqpaccstate (
    equipmenttype varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    accessoryno varchar(50) NOT NULL,
    userno varchar(10),
    starttime timestamp without time zone,
    accessoryversion varchar(3) NOT NULL,
    accapplyserialno varchar(20),
    CONSTRAINT pk_tblemseqpaccstate PRIMARY KEY (accessoryno)
);
CREATE INDEX IF NOT EXISTS i_tblemseqpaccstate_01 ON tblemseqpaccstate (equipmentno);

CREATE TABLE IF NOT EXISTS tblemseqpaccstatelog (
    accserialno varchar(20) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    accessoryno varchar(50) NOT NULL,
    accessoryversion varchar(3) NOT NULL,
    userno varchar(10),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    CONSTRAINT pk_tblemseqpaccstatelog PRIMARY KEY (accserialno)
);

COMMENT ON TABLE tbleqpaccessorybasis IS 'Oracle source: MESTEST.TBLEQPACCESSORYBASIS; schema checked 2026-11-16.';
COMMENT ON TABLE tblemsaccessorystate IS 'Oracle source: MESTEST.TBLEMSACCESSORYSTATE; schema checked 2026-11-16.';
COMMENT ON TABLE tblemsaccessorystatelog IS 'Oracle source: MESTEST.TBLEMSACCESSORYSTATELOG; schema checked 2026-11-16.';
COMMENT ON TABLE tbleqpaccstatecontrol IS 'Oracle source: MESTEST.TBLEQPACCSTATECONTROL; schema checked 2026-11-16.';
COMMENT ON TABLE tblemseqpaccstate IS 'Oracle source: MESTEST.TBLEMSEQPACCSTATE; schema checked 2026-11-16.';
COMMENT ON TABLE tblemseqpaccstatelog IS 'Oracle source: MESTEST.TBLEMSEQPACCSTATELOG; schema checked 2026-11-16.';

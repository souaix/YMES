-- PostgreSQL YMES CREATE-only reference adapted from the direct MESTEST DDL
-- in mestest_qc501_inspection_machine_source_ddl.sql.
-- Covers the shared inspection machine setup used by QC501-QC503.
-- This is a schema comparison aid, not a migration. Compare YMES live schema
-- before adding forward migrations. Do not execute against MES Oracle.
-- Oracle contains PK indexes only; no FK constraints were found in table DDL.

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspmachinetype (
    inspmachinetype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    picturename varchar(50),
    CONSTRAINT pk_tblqcinspmachinetype PRIMARY KEY (inspmachinetype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspmachineparametertype (
    inspmachinetype varchar(50) NOT NULL,
    insptype numeric(1,0) NOT NULL,
    baudrate numeric(16,0),
    paritybit varchar(10),
    databit numeric(1,0),
    stopbit numeric(1,0),
    endcode varchar(6),
    format1 varchar(50),
    format2 varchar(50),
    chno varchar(3),
    inspdriverno varchar(25),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0,
    inspmachinetypename varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_qcinspmachineparametertype PRIMARY KEY (inspmachinetype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspmachineproperty (
    inspmachine varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    defaultvalue varchar(255),
    propertysequence numeric(2,0),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcinspmachineproperty PRIMARY KEY (inspmachine, propertyno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspdriver (
    inspdriverno varchar(25) NOT NULL,
    functionname varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcinspdriver PRIMARY KEY (inspdriverno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspmachinebasis (
    inspmachineno varchar(50) NOT NULL,
    inspmachinetype varchar(50) NOT NULL,
    vendorno varchar(50) NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    assetno varchar(50),
    inspdriverno varchar(25),
    CONSTRAINT pk_tblqcinspmachinebasis PRIMARY KEY (inspmachineno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcinspmachineparameter (
    inspmachineno varchar(50) NOT NULL,
    inspmachinetype varchar(50),
    insptype numeric(1,0) NOT NULL,
    baudrate numeric(16,0),
    paritybit varchar(10),
    databit numeric(1,0),
    stopbit numeric(1,0),
    endcode varchar(6),
    format1 varchar(50),
    format2 varchar(50),
    chno varchar(3),
    inspdriverno varchar(25),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0,
    inspmachinename varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcinspmachineparameter PRIMARY KEY (inspmachineno)
);

-- Source Oracle DDL has only the primary-key unique index for each table.
-- The table definitions intentionally add no FK or extra index.

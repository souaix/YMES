-- YMES PostgreSQL schema references converted from direct Oracle MESTEST metadata.
-- Compare live YMES schema/migrations first. Preserve YMES-specific columns and data.
-- The primary TBLQCITEMFORATTRIBUTES definition is already included in
-- mestest_mtl_qc_item_tables_create.sql; it is not duplicated here.
-- This file contains the directly maintained QC201 child tables only.
-- No foreign keys or non-PK indexes were found on these tables.

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemparameter (
    qcitemno varchar(25) NOT NULL,
    parameterno varchar(20) NOT NULL,
    parametervalue varchar(255),
    CONSTRAINT pk_tblqcitemparameter PRIMARY KEY (qcitemno, parameterno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemfactor (
    qcitemno varchar(25) NOT NULL,
    factorno varchar(30) NOT NULL,
    factoronline varchar(1) NOT NULL,
    factorspec varchar(1) NOT NULL,
    opno varchar(20),
    tablename varchar(60),
    fieldname varchar(30),
    factorshortrun varchar(1),
    CONSTRAINT pk_tblqcitemfactor PRIMARY KEY (qcitemno, factorno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemreasonmap (
    qcitemno varchar(25) NOT NULL,
    reasonno varchar(20) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemreasonmap PRIMARY KEY (qcitemno, reasonno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcsampleplanchange (
    qcitemno varchar(25) NOT NULL,
    oldsampleplan varchar(25) NOT NULL,
    acclotqty numeric(3,0) NOT NULL DEFAULT -1,
    rejlotqty numeric(3,0) NOT NULL DEFAULT -1,
    newsampleplan varchar(25),
    factorno varchar(100) NOT NULL DEFAULT '*',
    lotrange numeric(3,0) DEFAULT -1,
    aqlchkforsplanchg numeric(1,0) DEFAULT 0,
    qcitemtype numeric(1,0) DEFAULT 0,
    CONSTRAINT n_tblqcsampleplanchange_06 CHECK (lotrange IS NOT NULL),
    CONSTRAINT n_tblqcsampleplanchange_07 CHECK (aqlchkforsplanchg IS NOT NULL),
    CONSTRAINT pk_tblqcsampleplanchange PRIMARY KEY (qcitemno, oldsampleplan, acclotqty, rejlotqty, lotrange)
);

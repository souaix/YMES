-- QC201 direct child schema translated from MESTEST source DDL.
-- The main table, TBLOPQCITEM and shared lookups already exist in YMES.
-- Do not create QC201 deletion-history/support tables from the raw Oracle extract.
BEGIN;

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

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemforattributes TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemparameter TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemfactor TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemreasonmap TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcsampleplanchange TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblopqcitem TO ymes_app;

COMMIT;

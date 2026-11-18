BEGIN;

-- YMES live-schema audit on 2026-11-18 found all six QC501-QC503 shared
-- inspection setup tables absent. Definitions follow the Oracle-derived
-- PostgreSQL comparison in mestest_qc501_inspection_machine_create.sql.
-- No business rows, extra indexes, or foreign keys are created here.
CREATE TABLE "YMES".tblqcinspmachinetype (
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

CREATE TABLE "YMES".tblqcinspmachineparametertype (
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

CREATE TABLE "YMES".tblqcinspmachineproperty (
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

CREATE TABLE "YMES".tblqcinspdriver (
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

CREATE TABLE "YMES".tblqcinspmachinebasis (
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

CREATE TABLE "YMES".tblqcinspmachineparameter (
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

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspmachinetype TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspmachineparametertype TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspmachineproperty TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspdriver TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspmachinebasis TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspmachineparameter TO ymes_app;

COMMIT;

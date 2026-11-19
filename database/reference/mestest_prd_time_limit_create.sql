-- PostgreSQL CREATE-only comparison. Check the live schema and version widths before adding a migration.

CREATE TABLE IF NOT EXISTS "YMES".tblprdlimitedtimecontrol (
    boundary varchar(5) NOT NULL,
    referenceno varchar(50) NOT NULL,
    referenceversion varchar(5) NOT NULL,
    opreference numeric(1,0) NOT NULL,
    motypeno numeric(2,0) NOT NULL,
    fromopno varchar(20) NOT NULL,
    toopno varchar(20) NOT NULL,
    validminute numeric(7,2) NOT NULL,
    mclassno varchar(30),
    holddescription varchar(4000),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    allowbetwenophold numeric(1,0) DEFAULT 0,
    frombrno varchar(20) DEFAULT 'N/A' NOT NULL,
    tobrno varchar(20) DEFAULT 'N/A' NOT NULL,
    eqpavailablecontrol numeric(1,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblprdlimitedtimecontrol PRIMARY KEY (boundary, referenceno, referenceversion, motypeno, fromopno, toopno, frombrno, tobrno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdlimitedtimecontrollog (
    boundary varchar(5) NOT NULL,
    referenceno varchar(50) NOT NULL,
    referenceversion varchar(5) NOT NULL,
    validminute numeric(7,2) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    fromopno varchar(20) NOT NULL,
    toopno varchar(20) NOT NULL,
    motypeno numeric(2,0) DEFAULT -1 NOT NULL,
    allowbetwenophold numeric(1,0) DEFAULT 0,
    frombrno varchar(20) DEFAULT 'N/A' NOT NULL,
    tobrno varchar(20) DEFAULT 'N/A' NOT NULL,
    eqpavailablecontrol numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblprdlimitedcontrollog PRIMARY KEY (boundary, referenceno, referenceversion, motypeno, fromopno, toopno, createdate, frombrno, tobrno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdlimitedtimeholdreason (
    boundary varchar(5) NOT NULL,
    referenceno varchar(50) NOT NULL,
    referenceversion varchar(3) NOT NULL,
    holditemno varchar(50) NOT NULL,
    fromopno varchar(20) NOT NULL,
    toopno varchar(20) NOT NULL,
    motypeno numeric(2,0) DEFAULT -1 NOT NULL,
    frombrno varchar(20) DEFAULT 'N/A' NOT NULL,
    tobrno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblprdlimitedtimeholdreason PRIMARY KEY (boundary, referenceno, referenceversion, holditemno, fromopno, toopno, motypeno, frombrno, tobrno)
);

-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tbl_pprdwaferruleimportsummary (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    deviceno varchar(50),
    opno varchar(20) NOT NULL,
    serialno varchar(50) NOT NULL,
    waferrulecategory numeric(1, 0) NOT NULL,
    waferruleno varchar(50) NOT NULL,
    waferruledescription varchar(500),
    waferruleholdcode varchar(50),
    controlattribno varchar(20),
    calattribno varchar(50),
    calattribscript varchar(500),
    rulescript varchar(3000),
    importtype numeric(1, 0),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    CONSTRAINT pk_pprdwruleimportsummary PRIMARY KEY (productno, productversion, serialno, waferruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_pprdwaferruleimportdetail (
    productno varchar(50) NOT NULL,
    deviceno varchar(50),
    opno varchar(20) NOT NULL,
    waferrulecategory numeric(1, 0) NOT NULL,
    waferruleno varchar(50) NOT NULL,
    waferruledescription varchar(500),
    ratiolimit numeric(6, 2),
    countlimit numeric(6, 0),
    logicoperator varchar(255),
    controlbinno varchar(10),
    controlbindesc varchar(500),
    rowindex numeric(6, 0) NOT NULL,
    columnindex numeric(6, 0) NOT NULL,
    importfilename varchar(100) NOT NULL,
    importtype numeric(1, 0),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    CONSTRAINT pk_pprdwruleimportdetail PRIMARY KEY (productno, rowindex, columnindex, importfilename)
);

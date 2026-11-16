-- Source: MESTEST Oracle DDL and dictionary queried read-only with PowerShell 5.1 + ODP.NET on 2026-11-16.
-- Target: PostgreSQL schema ymes. Empty tables only; no MES business data.
-- Oracle DATE maps to timestamp without time zone. NUMBER maps to numeric.
-- No Oracle foreign keys or non-PK indexes were found. The old delete service removes
-- customer shipping addresses and contactors explicitly before deleting the basis row.
-- Compare the actual YMES schema before applying; IF NOT EXISTS does not reconcile existing tables.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblentcustomerbasis (
    customerno varchar(50) CONSTRAINT n_tblentcustomerbasis_01 NOT NULL,
    customername varchar(100) CONSTRAINT n_tblentcustomerbasis_02 NOT NULL,
    customersname varchar(100) CONSTRAINT n_tblentcustomerbasis_03 NOT NULL,
    telno varchar(25),
    faxno varchar(25),
    www varchar(50),
    taxcode varchar(25),
    address varchar(255),
    director varchar(50),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0,
    address2 varchar(255),
    country varchar(50),
    status varchar(5),
    currency varchar(6),
    invtypeno varchar(1),
    invaddr varchar(255),
    invaddr2 varchar(255),
    invcustno varchar(20),
    invremark varchar(100),
    custtype varchar(1),
    artype varchar(1),
    freightterms varchar(20),
    paymentterms varchar(20),
    acctno varchar(50),
    deliveryterm varchar(50),
    forwarderassigned varchar(50),
    brokerassigned varchar(50),
    insurancecoverage varchar(50),
    reviser varchar(20),
    revisedate timestamp without time zone,
    customerename varchar(50),
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    sap_fiskn varchar(50),
    xx_name1 varchar(35),
    xx_name2 varchar(35),
    CONSTRAINT pk_tblentcustomerbasis PRIMARY KEY (customerno)
);

CREATE TABLE IF NOT EXISTS ymes.tblentcustomercontactor (
    contactorname varchar(50) CONSTRAINT n_tblentcustomercontactor_01 NOT NULL,
    customerno varchar(50) CONSTRAINT n_tblentcustomercontactor_02 NOT NULL,
    telno varchar(25),
    faxno varchar(25),
    email varchar(50),
    title varchar(25),
    deptname varchar(25),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentcustomercontactor PRIMARY KEY (customerno, contactorname)
);

CREATE TABLE IF NOT EXISTS ymes.tblentcustomershipaddr (
    shipaddr varchar(255) CONSTRAINT n_tblentcustomershipaddr_01 NOT NULL,
    customerno varchar(50) CONSTRAINT n_tblentcustomershipaddr_02 NOT NULL,
    telno varchar(25),
    contactorname varchar(25),
    shipcustomername varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    flow varchar(10) DEFAULT 'N/A' CONSTRAINT n_tblentcustomershipaddr_03 NOT NULL,
    declarationflag varchar(1) DEFAULT '0' CONSTRAINT n_tblentcustomershipaddr_04 NOT NULL,
    overseaflag varchar(1),
    faxno varchar(25),
    email varchar(255),
    shipaddr2 varchar(500),
    country varchar(50),
    companyname varchar(150),
    takerno varchar(50),
    forwarderno varchar(50),
    broker varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentcustomershipaddr PRIMARY KEY (customerno, shipaddr)
);

-- Source table has no PK/unique index/FK. The WinForms service identifies rows
-- by CustomerNo + ShipTo + Flow + Overseas/Declaration/Bond/HandCarry flags.
CREATE TABLE IF NOT EXISTS ymes.tblentcustomershipdeclaration (
    customerno varchar(50) CONSTRAINT n_customershipdeclaration_01 NOT NULL,
    shipto varchar(10) CONSTRAINT n_customershipdeclaration_02 NOT NULL,
    flow varchar(10) CONSTRAINT n_customershipdeclaration_03 NOT NULL,
    overseaflag varchar(1) CONSTRAINT n_customershipdeclaration_04 NOT NULL,
    declarationflag varchar(1) CONSTRAINT n_customershipdeclaration_05 NOT NULL,
    bondflag varchar(1) CONSTRAINT n_customershipdeclaration_06 NOT NULL,
    handcarryflag varchar(1) CONSTRAINT n_customershipdeclaration_07 NOT NULL,
    declarationway varchar(50) CONSTRAINT n_customershipdeclaration_08 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone
);

COMMENT ON TABLE ymes.tblentcustomerbasis IS 'MES Oracle source: MESTEST.TBLENTCUSTOMERBASIS';
COMMENT ON COLUMN ymes.tblentcustomerbasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN ymes.tblentcustomerbasis.datastamp IS 'Optimistic concurrency stamp';
COMMENT ON TABLE ymes.tblentcustomercontactor IS 'MES Oracle source: MESTEST.TBLENTCUSTOMERCONTACTOR';
COMMENT ON TABLE ymes.tblentcustomershipaddr IS 'MES Oracle source: MESTEST.TBLENTCUSTOMERSHIPADDR';
COMMENT ON TABLE ymes.tblentcustomershipdeclaration IS 'MES Oracle source: MESTEST.TBLENTCUSTOMERSHIPDECLARATION; source has no PK/index';

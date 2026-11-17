-- PostgreSQL empty-table structure converted from Oracle MESTEST source DDL.
-- Compare the target YMES schema and migrations before use. No MES business data is included.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tblmtlmaterialvendor (
    vendorno varchar(20) CONSTRAINT n_tblmtlmaterialvendor_01 NOT NULL,
    vendorname varchar(220) CONSTRAINT n_tblmtlmaterialvendor_02 NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblmtlmaterialvendor_03 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialvendor PRIMARY KEY (vendorno)
);
COMMENT ON TABLE "YMES".tblmtlmaterialvendor IS 'MES Oracle source: MESTEST.TBLMTLMATERIALVENDOR';

CREATE TABLE IF NOT EXISTS "YMES".tblmtlmaterialvendorcontactor (
    vendorno varchar(20) CONSTRAINT n_materialvendorcontactor_01 NOT NULL,
    contactorname varchar(50) CONSTRAINT n_materialvendorcontactor_02 NOT NULL,
    telno varchar(20),
    faxno varchar(20),
    title varchar(20),
    address varchar(50),
    email varchar(50),
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_mtlmaterialvendorcontactor PRIMARY KEY (vendorno, contactorname)
);
COMMENT ON TABLE "YMES".tblmtlmaterialvendorcontactor IS 'MES Oracle source: MESTEST.TBLMTLMATERIALVENDORCONTACTOR';

-- Oracle has no FK or non-PK indexes for these tables. Do not use this file to replace a migration.
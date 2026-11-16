-- Source: Oracle MESTEST DDL and dictionary queried read-only on 2026-11-16.
-- Target: PostgreSQL schema ymes. Empty tables only; no MES business data.
-- Oracle DATE maps to timestamp without time zone; NUMBER maps to numeric.
-- No Oracle foreign key or non-PK index was found. The legacy service explicitly
-- deletes contact rows before deleting an eligible subcontractor basis row.
-- Compare the actual YMES schema before applying; IF NOT EXISTS does not reconcile existing tables.

CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblentsubcontractor (
    subcontractorno varchar(20) CONSTRAINT n_tblentsubcontractor_01 NOT NULL,
    subcontractorname varchar(220) CONSTRAINT n_tblentsubcontractor_02 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblentsubcontractor_03 NOT NULL,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentsubcontractor PRIMARY KEY (subcontractorno)
);

CREATE TABLE IF NOT EXISTS ymes.tblentsubcontractorcont (
    subcontractorno varchar(20) CONSTRAINT n_tblentsubcontractorcont_01 NOT NULL,
    contactorname varchar(50) CONSTRAINT n_tblentsubcontractorcont_02 NOT NULL,
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
    CONSTRAINT pk_tblentsubcontractorcont PRIMARY KEY (subcontractorno, contactorname)
);

COMMENT ON TABLE ymes.tblentsubcontractor IS 'Subcontractor master structure converted from Oracle MESTEST.TBLENTSUBCONTRACTOR.';
COMMENT ON TABLE ymes.tblentsubcontractorcont IS 'Subcontractor contact structure converted from Oracle MESTEST.TBLENTSUBCONTRACTORCONT.';

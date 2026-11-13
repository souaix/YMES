-- PostgreSQL CREATE TABLE conversion from live MES Oracle MESTEST data dictionary.
-- Home-PC handoff: equipment vendor master and its contactors only.
-- No business rows are included. Apply through a reviewed home-PC migration.
CREATE SCHEMA IF NOT EXISTS "YMES";
SET search_path TO "YMES", public;

CREATE TABLE IF NOT EXISTS tbleqpvendor (
    vendorno varchar(20) NOT NULL,
    vendorname varchar(220) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpvendor PRIMARY KEY (vendorno)
);
COMMENT ON TABLE tbleqpvendor IS 'MES Oracle source: MESTEST.TBLEQPVENDOR';

CREATE TABLE IF NOT EXISTS tbleqpvendorcontactor (
    vendorno varchar(20) NOT NULL,
    contactorname varchar(50) NOT NULL,
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
    CONSTRAINT pk_tbleqpvendorcontactor PRIMARY KEY (vendorno, contactorname)
);
COMMENT ON TABLE tbleqpvendorcontactor IS 'MES Oracle source: MESTEST.TBLEQPVENDORCONTACTOR';

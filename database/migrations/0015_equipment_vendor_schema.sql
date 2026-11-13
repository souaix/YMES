BEGIN;

-- MES MESTEST source: database/reference/mestest_equipment_vendor_create.sql
-- These tables contain schema only; do not import production vendor rows.
CREATE TABLE IF NOT EXISTS "YMES".tbleqpvendor (
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

CREATE TABLE IF NOT EXISTS "YMES".tbleqpvendorcontactor (
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

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tbleqpvendor,
    "YMES".tbleqpvendorcontactor
TO ymes_app;

COMMIT;

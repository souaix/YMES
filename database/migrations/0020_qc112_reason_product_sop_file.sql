-- YMES QC112 schema-only migration translated from the MESTEST source DDL.
-- This migration creates only the missing reason master and SOP association table.
-- It does not import MES business data or add source-absent foreign keys/indexes.
BEGIN;

CREATE TABLE IF NOT EXISTS "YMES".tblqcreasonbasis (
    reasonno varchar(20) NOT NULL,
    reasonname varchar(100) NOT NULL,
    reasontype numeric(1,0) NOT NULL,
    reasonlevel numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    reasonsubtype varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcreasonbasis PRIMARY KEY (reasonno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcreasonprdsopfile (
    reasonno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    sopfilename varchar(100),
    sopfilepath varchar(2000),
    CONSTRAINT pk_tblqcreasonprdsopfile PRIMARY KEY (reasonno, productno)
);

GRANT SELECT ON "YMES".tblqcreasonbasis TO ymes_app;
GRANT SELECT, INSERT, DELETE ON "YMES".tblqcreasonprdsopfile TO ymes_app;

COMMIT;

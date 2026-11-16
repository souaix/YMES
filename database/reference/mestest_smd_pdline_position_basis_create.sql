-- PostgreSQL empty-table structure converted from Oracle MESTEST source DDL.
-- Compare the target YMES schema and migrations before use. This file contains no MES business data.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tblsmdpdlinepositionbasis (
    pdlineno varchar(50) NOT NULL,
    positionno varchar(50) NOT NULL,
    description varchar(50),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdpdlinepositionbasis PRIMARY KEY (pdlineno, positionno)
);
COMMENT ON TABLE "YMES".tblsmdpdlinepositionbasis IS 'MES Oracle source: MESTEST.TBLSMDPDLINEPOSITIONBASIS';

-- PostgreSQL empty-table structure converted from Oracle MESTEST source DDL.
-- Compare the target YMES schema and migrations before use. This file contains no MES business data.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tblsmdareashift (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    managerno varchar(10) NOT NULL,
    exceptiontime numeric(4,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareashift PRIMARY KEY (areano, departmentno, shiftno)
);
COMMENT ON TABLE "YMES".tblsmdareashift IS 'MES Oracle source: MESTEST.TBLSMDAREASHIFT';

CREATE TABLE IF NOT EXISTS "YMES".tblsmdareaoperator (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsmdareaoperator PRIMARY KEY (areano, departmentno, shiftno, userno)
);
COMMENT ON TABLE "YMES".tblsmdareaoperator IS 'MES Oracle source: MESTEST.TBLSMDAREAOPERATOR';

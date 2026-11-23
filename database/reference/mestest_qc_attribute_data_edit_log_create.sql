-- PostgreSQL CREATE-only reference derived from
-- database/reference/mestest_qc_item_attributes_source_ddl.sql
-- Oracle source: MESTEST.TBLSPCATTEDITLOG
-- No primary key, foreign key, or secondary index was present in that source DDL.
-- This file is a schema handoff reference; it is not a migration and has not been applied.

CREATE TABLE IF NOT EXISTS "YMES".tblspcatteditlog (
    userid varchar(10) NOT NULL,
    recorddate timestamp without time zone NOT NULL,
    spcserial varchar(1500) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    lotserial varchar(55),
    originalvalue varchar(1000),
    newvalue varchar(1000),
    description varchar(255),
    loggroupserial varchar(50)
);

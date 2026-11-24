-- PostgreSQL CREATE-only reference derived from the verified MESTEST source DDL.
-- Source: database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql
-- The Oracle table has only the LOTNO primary key; BIN1-BIN8 are nullable NUMBER(6,0) DEFAULT 0.
CREATE TABLE IF NOT EXISTS "YMES".tblwipgoodbinstate (
    lotno varchar(50) NOT NULL,
    lotserial varchar(55),
    bin1 numeric(6,0) DEFAULT 0,
    bin2 numeric(6,0) DEFAULT 0,
    bin3 numeric(6,0) DEFAULT 0,
    bin4 numeric(6,0) DEFAULT 0,
    bin5 numeric(6,0) DEFAULT 0,
    bin6 numeric(6,0) DEFAULT 0,
    bin7 numeric(6,0) DEFAULT 0,
    bin8 numeric(6,0) DEFAULT 0,
    CONSTRAINT pk_tblwipgoodbinstate PRIMARY KEY (lotno)
);

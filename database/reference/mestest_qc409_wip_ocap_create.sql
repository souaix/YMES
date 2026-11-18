-- PostgreSQL YMES CREATE-only reference adapted from the direct MESTEST DDL
-- in mestest_qc409_wip_ocap_source_ddl.sql.
-- Schema comparison aid only: compare YMES live schema before adding a
-- forward migration. Do not execute against MES Oracle.
-- Oracle DDL contains PK indexes only; no FK or extra indexes were found.

CREATE TABLE IF NOT EXISTS "YMES".tblwiperfocapdisposition (
    erfno varchar(20) NOT NULL,
    holditemno varchar(200) NOT NULL,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    disporder numeric(5,0) NOT NULL,
    ocapnodeid varchar(50) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    dispuserno varchar(10),
    ocapgroupno varchar(30),
    ocapnextgroupno varchar(30),
    dispdate timestamp without time zone,
    ocaplinkname varchar(20),
    ocapnextdisptype varchar(5),
    description varchar(255),
    CONSTRAINT pk_tblwiperfocapdisposition PRIMARY KEY (erfno, holditemno, ocapno, ocapversion, disporder)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwiperfocapstate (
    erfno varchar(20) NOT NULL,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    holditemno varchar(200) NOT NULL,
    ocapstatus numeric(2,0) NOT NULL,
    ocapnodeid varchar(50) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    ocapgroupno varchar(50) NOT NULL,
    eventtime timestamp without time zone,
    CONSTRAINT pk_tblwiperfocapstate PRIMARY KEY (erfno, ocapno, ocapversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwiperfocapgroupstate (
    erfno varchar(20) NOT NULL,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    ocapnodeid varchar(50) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    grouptype numeric(1,0) NOT NULL,
    executeflag numeric(1,0) NOT NULL DEFAULT 0,
    groupnodeid varchar(50) NOT NULL,
    CONSTRAINT pk_tblwiperfocapgroupstate PRIMARY KEY (erfno, ocapno, ocapversion, ocapnodeid)
);

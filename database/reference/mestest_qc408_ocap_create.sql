-- PostgreSQL YMES CREATE-only reference adapted from the direct MESTEST DDL
-- in mestest_qc408_ocap_source_ddl.sql.
-- Schema comparison aid only: compare the YMES live schema before adding a
-- forward migration. Never execute this file against MES Oracle.
-- Oracle source has PK indexes only; no FK or additional indexes were found.

CREATE TABLE IF NOT EXISTS "YMES".tblqcocapbasis (
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ocapxmlstring text,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    curversion numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcocapbasis PRIMARY KEY (ocapno, ocapversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcocapnodebasis (
    ocapnodeid varchar(50) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    ocapnodetype numeric(1,0) NOT NULL,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    ocapgroupno varchar(30),
    CONSTRAINT pk_tblqcocapnodebasis PRIMARY KEY (ocapnodeid)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcocapnoderelation (
    fromnodeid varchar(50) NOT NULL,
    tonodeid varchar(50) NOT NULL,
    ocaplinkname varchar(20) NOT NULL,
    fromnodeno varchar(50),
    tonodeno varchar(50),
    ocapno varchar(50),
    ocapversion varchar(5),
    CONSTRAINT pk_tblqcocapnoderelation PRIMARY KEY (fromnodeid, tonodeid, ocaplinkname)
);

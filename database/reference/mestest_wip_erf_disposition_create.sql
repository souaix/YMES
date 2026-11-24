-- PostgreSQL CREATE-only reference for shared WIP/SPC ERF disposition history.
-- Source: MESTEST DDL in mestest_qc409_wip_disposition_dependencies_source_ddl.sql.
-- Reference only: inspect live YMES PostgreSQL schema and migration history before use.

CREATE TABLE IF NOT EXISTS ymes.tblwiperfdisposition (
    erfno varchar(20) NOT NULL,
    disporder numeric(2,0) NOT NULL,
    disptype numeric(2,0) NOT NULL,
    dispdescription varchar(500) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    nextdisptype numeric(2,0) NOT NULL,
    lotserial varchar(55),
    groupno varchar(20) NOT NULL,
    nextgroupno varchar(20),
    contactorname varchar(50),
    CONSTRAINT pk_tblwiperfdisposition PRIMARY KEY (erfno, disporder)
);

CREATE INDEX IF NOT EXISTS i_tblwiperfdisposition_01
    ON ymes.tblwiperfdisposition (lotserial);

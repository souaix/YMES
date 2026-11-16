-- Oracle source: MESTEST.TBLSMDPDLINEBASIS; directly checked 2026-11-16.
-- PostgreSQL empty-table reference only. Compare YMES schema/migrations first; do not run as an unconditional migration.
-- Oracle DATE maps to timestamp without time zone. No Oracle foreign key or additional index exists.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblsmdpdlinebasis (
    pdlineno varchar(50) NOT NULL,
    pdlinename varchar(50) NOT NULL,
    pdlinestatus numeric(1,0) DEFAULT 1 NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdpdlinebasis PRIMARY KEY (pdlineno)
);

COMMENT ON TABLE ymes.tblsmdpdlinebasis IS 'MES Oracle source: MESTEST.TBLSMDPDLINEBASIS';
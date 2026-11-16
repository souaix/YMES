-- YMES PostgreSQL empty-table schema based on the read-only Oracle source DDL.
-- Creates structure only. Do not insert or migrate MESTEST business rows here.
-- Reconcile against the existing YMES schema before applying a migration.

CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblsmdcomputerareabasis (
    computerno varchar(20) NOT NULL,
    factoryno varchar(20) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdcomputerareabasis PRIMARY KEY (computerno, factoryno)
);

COMMENT ON TABLE ymes.tblsmdcomputerareabasis IS '過帳電腦之區域主檔；Oracle source: MESTEST.TBLSMDCOMPUTERAREABASIS';

CREATE TABLE IF NOT EXISTS ymes.tblsmdcomputerareadetail (
    computerno varchar(20) NOT NULL,
    factoryno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    CONSTRAINT pk_tblsmdcomputerareadetail PRIMARY KEY (computerno, factoryno, areano)
);

COMMENT ON TABLE ymes.tblsmdcomputerareadetail IS '過帳電腦區域明細；Oracle source: MESTEST.TBLSMDCOMPUTERAREADETAIL';

-- Oracle source has no foreign key constraints or indexes beyond PK indexes.

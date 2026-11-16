CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLSMDAREABASIS, TBLSMDAREAINVENTORY and TBLSMDAREAMAP DDL/data dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table definitions only; no MES business records.
-- Oracle DATE is represented as timestamp without time zone. Oracle BLOB is represented as bytea.
-- The Oracle source has no foreign keys; only its primary-key indexes are recreated here.

CREATE TABLE IF NOT EXISTS ymes.tblsmdareabasis (
    areano varchar(20) NOT NULL,
    areaname varchar(50) NOT NULL,
    areatype numeric(1,0) DEFAULT 0 NOT NULL,
    areaclass numeric(1,0) DEFAULT 0 NOT NULL,
    baseareano varchar(20),
    factoryno varchar(20) NOT NULL,
    departmentno varchar(20),
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    areacompose numeric(1,0) DEFAULT 0 NOT NULL,
    backgroundimage bytea,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareabasis PRIMARY KEY (areano)
);
COMMENT ON TABLE ymes.tblsmdareabasis IS 'MES Oracle source: MESTEST.TBLSMDAREABASIS';

CREATE TABLE IF NOT EXISTS ymes.tblsmdareainventory (
    areano varchar(50) NOT NULL,
    inventoryno varchar(50) NOT NULL,
    baseareano varchar(50) NOT NULL,
    defaultinv numeric(1,0) DEFAULT 0 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareainventory PRIMARY KEY (areano, inventoryno)
);
COMMENT ON TABLE ymes.tblsmdareainventory IS 'MES Oracle source: MESTEST.TBLSMDAREAINVENTORY';

CREATE TABLE IF NOT EXISTS ymes.tblsmdareamap (
    baseareano varchar(50) NOT NULL,
    parentareano varchar(50) NOT NULL,
    childareano varchar(50) NOT NULL,
    parentareatype numeric(11,0) NOT NULL,
    childareatype numeric(11,0) NOT NULL,
    toppdlineareano varchar(50),
    CONSTRAINT pk_tblsmdareamap PRIMARY KEY (baseareano, parentareano, childareano)
);
COMMENT ON TABLE ymes.tblsmdareamap IS 'MES Oracle source: MESTEST.TBLSMDAREAMAP';

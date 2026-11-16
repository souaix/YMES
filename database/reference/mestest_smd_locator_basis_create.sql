-- Source: Oracle MESTEST.TBLINVLOCATORBASIS DBMS_METADATA and dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table only; no MES business rows.
-- Oracle DATE maps to timestamp without time zone. Oracle has no foreign key on this table.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblinvlocatorbasis (
    inventoryno varchar(20) NOT NULL,
    locatorno varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    capacity numeric(12,4) DEFAULT 0,
    locatorname varchar(50),
    erpno varchar(50),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblinvlocatorbasis PRIMARY KEY (inventoryno, locatorno)
);

COMMENT ON TABLE ymes.tblinvlocatorbasis IS 'MES Oracle source: MESTEST.TBLINVLOCATORBASIS';
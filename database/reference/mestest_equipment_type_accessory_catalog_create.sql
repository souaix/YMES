CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLEQPACCESSORYTYPE DBMS_METADATA DDL and data dictionary, queried 2026-11-16.
-- Target: PostgreSQL, schema YMES. Empty table only; no MES business records.
-- Oracle DATE maps to timestamp without time zone. No foreign key or secondary index exists in the source.
CREATE TABLE IF NOT EXISTS ymes.tbleqpaccessorytype (
    accessorytype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    accessorycategory varchar(50) DEFAULT 'NA' NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpaccessorytype PRIMARY KEY (accessorytype)
);
COMMENT ON TABLE ymes.tbleqpaccessorytype IS 'MES Oracle source: MESTEST.TBLEQPACCESSORYTYPE';

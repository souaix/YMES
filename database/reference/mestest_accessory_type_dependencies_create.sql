CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST DDL/data dictionary, queried read-only 2026-11-16.
-- Target: PostgreSQL YMES. Empty tables only; no MES business records.
-- Oracle DATE maps to timestamp without time zone; Oracle NUMBER(p,0) maps to numeric(p,0).
-- No foreign keys or secondary indexes exist in these source tables.
CREATE TABLE IF NOT EXISTS ymes.tbleqpaccessorycategory (
    accessorycategory varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpaccessorycategory PRIMARY KEY (accessorycategory)
);
COMMENT ON TABLE ymes.tbleqpaccessorycategory IS 'MES Oracle source: MESTEST.TBLEQPACCESSORYCATEGORY';

CREATE TABLE IF NOT EXISTS ymes.tbleqpaccessoryproperty (
    accessory varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    defaultvalue varchar(255),
    propertysequence numeric(2,0),
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    accessoryversion varchar(3) NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpaccessoryproperty PRIMARY KEY (accessory, propertyno, accessoryversion)
);
COMMENT ON TABLE ymes.tbleqpaccessoryproperty IS 'MES Oracle source: MESTEST.TBLEQPACCESSORYPROPERTY';

CREATE TABLE IF NOT EXISTS ymes.tbleqpaccessorylife (
    accessory varchar(50) NOT NULL,
    lifemanage numeric(2,0) DEFAULT 0 NOT NULL,
    lifescript varchar(200),
    repairable numeric(2,0),
    repaircycle numeric(8,0),
    scraptype numeric(2,0),
    scraplife numeric(8,0),
    lifetolerance numeric(2,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    timecycle numeric(8,0),
    alerttime numeric(8,0),
    alertcount numeric(8,0),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tbleqpaccessorylife PRIMARY KEY (accessory)
);
COMMENT ON TABLE ymes.tbleqpaccessorylife IS 'MES Oracle source: MESTEST.TBLEQPACCESSORYLIFE';

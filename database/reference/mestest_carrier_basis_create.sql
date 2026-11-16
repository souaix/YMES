CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLEQPCARRIERBASIS DBMS_METADATA and dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table only; no MES business records.
-- Oracle DATE maps to timestamp without time zone. Logical category/type/factory references are not Oracle foreign keys.
CREATE TABLE IF NOT EXISTS ymes.tbleqpcarrierbasis (
    carrierno varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    carriertype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    factoryno varchar(20),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    getgroupno varchar(50),
    getansno varchar(50),
    maintenancetime timestamp without time zone,
    begroupmainten varchar(50),
    CONSTRAINT pk_tbleqpcarrierbasis PRIMARY KEY (carrierno)
);

COMMENT ON TABLE ymes.tbleqpcarrierbasis IS 'MES Oracle source: MESTEST.TBLEQPCARRIERBASIS';

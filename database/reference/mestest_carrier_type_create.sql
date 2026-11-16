CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLEQPCARRIERTYPE DBMS_METADATA and dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table only; no MES business records.
-- Oracle DATE maps to timestamp without time zone. The logical CarrierCategory relation is not an Oracle foreign key.
CREATE TABLE IF NOT EXISTS ymes.tbleqpcarriertype (
    carriertype varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    uselayer numeric(1,0) NOT NULL,
    carrierlayer numeric(3,0) DEFAULT 1,
    carriercapacity numeric(3,0) DEFAULT 1,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    startposition numeric(3,0) DEFAULT 1,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpcarriertype PRIMARY KEY (carriertype)
);

COMMENT ON TABLE ymes.tbleqpcarriertype IS 'MES Oracle source: MESTEST.TBLEQPCARRIERTYPE';

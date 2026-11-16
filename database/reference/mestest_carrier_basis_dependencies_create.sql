CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLWIPCARRIERSTATE DBMS_METADATA and dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table only; no MES business records.
CREATE TABLE IF NOT EXISTS ymes.tblwipcarrierstate (
    carrierno varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    carriertype varchar(50) NOT NULL,
    carrierstate numeric(1,0),
    description varchar(255),
    lotno varchar(50),
    CONSTRAINT pk_tblwipcarrierstate PRIMARY KEY (carrierno)
);
COMMENT ON TABLE ymes.tblwipcarrierstate IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATE';

-- Source: Oracle MESTEST.TBLSMDFACTORYBASIS DBMS_METADATA and dictionary, queried 2026-11-16.
-- Target: PostgreSQL schema YMES. Empty table only; no MES business records.
CREATE TABLE IF NOT EXISTS ymes.tblsmdfactorybasis (
    factoryno varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    fabcode varchar(10),
    factoryname varchar(50),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdfactorybasis PRIMARY KEY (factoryno)
);
COMMENT ON TABLE ymes.tblsmdfactorybasis IS 'MES Oracle source: MESTEST.TBLSMDFACTORYBASIS';

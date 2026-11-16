-- Source: Oracle MESTEST.TBLUSRUSERGROUP DBMS_METADATA.GET_DDL and data dictionary, queried 2026-11-16.
-- Target: PostgreSQL, schema YMES. Empty table only; no MES business records.
-- The primary key creates the sole unique index. No other indexes or foreign keys exist in the source.
CREATE TABLE IF NOT EXISTS ymes.tblusrusergroup (
    userno varchar(10) NOT NULL,
    groupno varchar(20) NOT NULL,
    grouptype numeric(1,0) NOT NULL DEFAULT 0,
    agentof varchar(10) NOT NULL,
    agentdate timestamp without time zone,
    agentcreatedate timestamp without time zone,
    CONSTRAINT pk_tblusrusergroup PRIMARY KEY (userno, groupno, grouptype, agentof)
);

COMMENT ON TABLE ymes.tblusrusergroup IS 'MES Oracle source: MESTEST.TBLUSRUSERGROUP';
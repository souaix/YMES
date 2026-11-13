-- Source: Oracle MESTEST.TBLEQPENGINEERGROUPBASIS DBMS_METADATA.GET_DDL
-- Target: PostgreSQL, schema YMES. No MES business records are included.
-- This table is also present in mestest_first_phase_create_tables.sql.
CREATE TABLE IF NOT EXISTS tbleqpengineergroupbasis (
    engineergroupno varchar(20) NOT NULL,
    engineergroupname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpengineergroupbasis PRIMARY KEY (engineergroupno)
);

COMMENT ON TABLE tbleqpengineergroupbasis IS 'MES Oracle source: MESTEST.TBLEQPENGINEERGROUPBASIS';
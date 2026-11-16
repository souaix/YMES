-- PostgreSQL empty-table DDL converted from MESTEST.TBLEQPACCSTATEBASIS.
-- Source Oracle DDL: mestest_accessory_state_source_ddl.sql
-- No MES business data is included. Compare the YMES schema before applying.
CREATE TABLE IF NOT EXISTS tbleqpaccstatebasis (
    accessorystate numeric(2,0) NOT NULL,
    statetype numeric(2,0) DEFAULT 1 NOT NULL,
    statename varchar(50) NOT NULL,
    statecolor numeric(11,0) NOT NULL,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpaccstatebasis PRIMARY KEY (accessorystate)
);

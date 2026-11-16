-- PostgreSQL empty-table DDL converted from MESTEST.TBLEQPACCCATEGORYSTATEBASIS.
-- Source Oracle DDL: mestest_accessory_category_state_source_ddl.sql
-- No MES business data is included. Compare the YMES schema before applying.
CREATE TABLE IF NOT EXISTS tbleqpacccategorystatebasis (
    accessorycategory varchar(50) NOT NULL,
    accessorystate numeric(2,0) NOT NULL,
    accessorystatename varchar(50) NOT NULL,
    initialflag numeric(1,0) DEFAULT 0 NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    operationname varchar(50) NOT NULL,
    interfacename varchar(50),
    executionfile varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpacccategorystatebasis
        PRIMARY KEY (accessorycategory, accessorystate, operationname)
);

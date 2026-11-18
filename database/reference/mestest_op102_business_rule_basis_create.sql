-- PostgreSQL structure-only reference mapped from the Oracle source DDL.
-- Not a migration. Compare the YMES live schema and migrations before adding a forward migration.
-- Oracle DATE maps to timestamp without time zone; NUMBER(p,0) maps to numeric(p,0).
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tblopbusinessrulebasis (
    brno varchar(20) NOT NULL,
    brname varchar(50) NOT NULL,
    interfacename varchar(50),
    functionname varchar(50),
    interfaceurl varchar(100),
    ruletype numeric(1, 0) DEFAULT 0,
    rulemethod numeric(1, 0) DEFAULT 0,
    issuestate numeric(1, 0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    kernelmodule varchar(100),
    kernelfunctionname varchar(100),
    executionfile varchar(50),
    activemodule varchar(50),
    allowtimelimitcontrol numeric(1, 0) DEFAULT 0,
    brprodtype numeric(1, 0),
    semi_wafer numeric(1, 0) DEFAULT 0,
    ic_cp_pkg numeric(1, 0) DEFAULT 0,
    ic_ft_pkg numeric(1, 0) DEFAULT 0,
    discrete_pkg numeric(1, 0) DEFAULT 0,
    led_pkg numeric(1, 0) DEFAULT 0,
    ic_ingot numeric(1, 0) DEFAULT 0,
    epi_wafer numeric(1, 0) DEFAULT 0,
    ic_smt numeric(1, 0),
    datastamp numeric(11, 0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopbusinessrulebasis PRIMARY KEY (brno)
);

-- Oracle metadata: one PK unique index; no foreign keys or additional indexes.

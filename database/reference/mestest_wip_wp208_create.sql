-- WP208 source-structure reference only. Compare YMES live schema/history before a forward migration.
-- Source: MESTEST.TBLSYSPARAMETER, Oracle DBMS_METADATA/ALL_* queried read-only 2026-11-20.
-- Oracle has one-column PK (PARAMETERNO), no foreign keys, no triggers, no extra indexes.
CREATE TABLE IF NOT EXISTS ymes.tblsysparameter (
    parametertype varchar(50) NOT NULL,
    parameterno varchar(50) NOT NULL,
    parametervalue varchar(255) NOT NULL,
    parametervisable numeric(1,0) NOT NULL DEFAULT 0,
    parametercategory varchar(50),
    categoryorder numeric(3,0),
    parameterorder numeric(3,0) DEFAULT 0,
    parameterdatatype numeric(1,0),
    parameterinputtype numeric(1,0),
    encryptionflag numeric(1,0) DEFAULT 0,
    validlist varchar(2000),
    activemodule varchar(50),
    ic_ingot numeric(1,0) DEFAULT 0,
    epi_wafer numeric(1,0) DEFAULT 0,
    semi_wafer numeric(1,0) DEFAULT 0,
    ic_cp_pkg numeric(1,0) DEFAULT 0,
    ic_ft_pkg numeric(1,0) DEFAULT 0,
    discrete_pkg numeric(1,0) DEFAULT 0,
    led_pkg numeric(1,0) DEFAULT 0,
    ic_smt numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsysparameter PRIMARY KEY (parameterno)
);
COMMENT ON TABLE ymes.tblsysparameter IS 'MES Oracle source: MESTEST.TBLSYSPARAMETER; structure reference only';

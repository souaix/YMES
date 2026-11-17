-- PostgreSQL schema-only reference translated from MESTEST metadata.
-- Compare YMES live schema and migrations before creating anything; preserve YMES extensions and data.

CREATE TABLE IF NOT EXISTS "YMES".tblsysmesparameter (
    parameterno varchar(30) NOT NULL,
    parametername varchar(50) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    condition varchar(50),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    parametertype numeric(1,0) NOT NULL DEFAULT 0,
    functionname varchar(50),
    parameterclass numeric(1,0) NOT NULL,
    showname varchar(50) DEFAULT 'N/A',
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmesparameter PRIMARY KEY (parameterno)
);

CREATE TABLE IF NOT EXISTS "YMES".fc_posionlist (
    posion_no varchar(50) NOT NULL,
    CONSTRAINT pk_fc_posionlist PRIMARY KEY (posion_no)
);

CREATE TABLE IF NOT EXISTS "YMES".fc_qcformcallbackitemmap (
    fieldname varchar(30) NOT NULL,
    qcitemno varchar(50) NOT NULL,
    description varchar(500),
    CONSTRAINT pk_fc_qcformcallbackitemmap PRIMARY KEY (fieldname)
);

CREATE TABLE IF NOT EXISTS "YMES".fc_thickness (
    spec varchar(50) NOT NULL,
    CONSTRAINT pk_fc_thickness PRIMARY KEY (spec)
);

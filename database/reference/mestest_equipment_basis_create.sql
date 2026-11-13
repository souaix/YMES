-- Source: Oracle MESTEST.TBLEQPEQUIPMENTBASIS DDL supplied by the user on 2026-11-13.
-- Target: PostgreSQL schema "YMES". Structure only; no MES business rows are included.
-- Oracle tablespace/storage clauses are omitted. The primary key creates its unique index in PostgreSQL.
-- The source DDL declares no foreign keys or non-PK indexes.
-- Compare the existing YMES table column by column before applying; IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tbleqpequipmentbasis (
    equipmentno varchar(50) CONSTRAINT n_tbleqpequipmentbasis_01 NOT NULL,
    equipmenttype varchar(50) CONSTRAINT n_tbleqpequipmentbasis_02 NOT NULL,
    capacity numeric(8,0) DEFAULT -1,
    vendorno varchar(50) CONSTRAINT n_tbleqpequipmentbasis_03 NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tbleqpequipmentbasis_04 NOT NULL,
    engineergroupno varchar(20),
    assetno varchar(50),
    equipmentclass varchar(50),
    loadport numeric(2,0) DEFAULT 0,
    autoflag numeric(1,0),
    eacontroller varchar(200),
    eqprecipe numeric(1,0),
    qclistno varchar(50),
    maxtime numeric(6,2),
    fixeqptime numeric(6,2) DEFAULT 0,
    vareqptime numeric(6,2) DEFAULT 0,
    countopunitqty numeric(6,0) DEFAULT 1,
    counteqpunitqty numeric(6,0) DEFAULT 1,
    counter numeric(5,0),
    equipmentname varchar(255),
    erpno varchar(50),
    queueserver varchar(50),
    requestmsgqueue varchar(100),
    responsemsgqueue varchar(100),
    chamberflag numeric(1,0) DEFAULT 0,
    belongequipmentno varchar(50),
    equipmentclassno numeric(3,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    requestmsgqueue_mes varchar(100),
    responsemsgqueue_eap varchar(100),
    xx_keeperno varchar(50),
    CONSTRAINT pk_tbleqpequipmentbasis PRIMARY KEY (equipmentno)
);

COMMENT ON TABLE "YMES".tbleqpequipmentbasis IS 'Structure converted from Oracle MESTEST.TBLEQPEQUIPMENTBASIS DDL supplied by the user.';

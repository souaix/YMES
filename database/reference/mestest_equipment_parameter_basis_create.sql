-- Source: Oracle MESTEST.TBLEQPPARAMETERBASIS DDL supplied by the user on 2026-11-13.
-- Target: PostgreSQL schema "YMES". Structure only; no MES business rows are included.
-- Oracle tablespace/storage clauses are omitted. The primary key creates its unique index in PostgreSQL.
-- The source DDL declares no foreign keys or non-PK indexes.
-- Compare the existing YMES table column by column before applying; IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tbleqpparameterbasis (
    equipmentno varchar(50) CONSTRAINT n_tbleqpparameterbasis_01 NOT NULL,
    seqno numeric(2,0) CONSTRAINT n_tbleqpparameterbasis_02 NOT NULL,
    parameterno varchar(20) CONSTRAINT n_tbleqpparameterbasis_03 NOT NULL,
    spectype numeric(1,0) CONSTRAINT n_tbleqpparameterbasis_04 NOT NULL,
    parametervalue numeric(12,6),
    reviser varchar(10) CONSTRAINT n_tbleqpparameterbasis_05 NOT NULL,
    revisedate timestamp without time zone CONSTRAINT n_tbleqpparameterbasis_06 NOT NULL,
    eqpparamdate timestamp without time zone,
    CONSTRAINT pk_tbleqpparameterbasis PRIMARY KEY (equipmentno, parameterno)
);

COMMENT ON TABLE "YMES".tbleqpparameterbasis IS 'Structure converted from Oracle MESTEST.TBLEQPPARAMETERBASIS DDL supplied by the user.';

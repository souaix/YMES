-- YMES PostgreSQL schema proposal for QC203.
-- Derived from the read-only Oracle source DDL in mestest_qc_attribute_rule_map_source_ddl.sql.
-- Reference only: compare the live YMES schema and migration history before applying.
-- Do not execute this file against the MES Oracle database.
--
-- Oracle stores QCItem factor values as dynamically-added columns. YMES uses factorvalues
-- JSONB so API callers cannot turn a user-supplied FactorNo into an SQL identifier.
-- Oracle has no FK between the three source tables. The API must replace/delete the child
-- rows transactionally and clean up Parameter rows as well as Detail rows.

CREATE SEQUENCE "YMES".qcattspserial_seq
    AS bigint
    START WITH 1
    INCREMENT BY 1
    NO CYCLE;

CREATE TABLE "YMES".tblspcqattrulemapbasis (
    qcattspsserial varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1, 0) DEFAULT 0,
    factorvalues jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_tblspcqattrulemapbasis PRIMARY KEY (qcattspsserial),
    CONSTRAINT uq_tblspcqattrulemapbasis_item_factors UNIQUE (qcitemno, factorvalues)
);

CREATE INDEX i_tblspcqattrulemapbasis_01
    ON "YMES".tblspcqattrulemapbasis (qcitemno);

CREATE TABLE "YMES".tblspcqattrulemapdetail (
    qcattspsserial varchar(50) NOT NULL,
    retestlot numeric(2, 0) NOT NULL DEFAULT 0,
    planno varchar(25),
    lotruleno varchar(20),
    comruleno varchar(20),
    CONSTRAINT pk_tblspcqattrulemapdetail PRIMARY KEY (qcattspsserial, retestlot)
);

CREATE TABLE "YMES".tblspcqattrulemapparameter (
    qcattspsserial varchar(50) NOT NULL,
    retestlot numeric(2, 0) NOT NULL DEFAULT 0,
    parameterno varchar(20) NOT NULL,
    parametervalue varchar(255),
    CONSTRAINT pk_tblspcqattrulemapparameter PRIMARY KEY (qcattspsserial, retestlot, parameterno)
);

COMMENT ON TABLE "YMES".tblspcqattrulemapbasis IS
    'QC203 判別法則對應主檔；factorvalues JSONB is a YMES adaptation for Oracle dynamic factor columns.';
COMMENT ON COLUMN "YMES".tblspcqattrulemapbasis.factorvalues IS
    'Keys are exactly the selected QC Item FactorOnline=Y FactorNo values; each value is a string.';
COMMENT ON COLUMN "YMES".tblspcqattrulemapbasis.issuestate IS
    'Oracle source DEFAULT 0; source column is nullable.';
COMMENT ON TABLE "YMES".tblspcqattrulemapdetail IS
    'QC203 RetestLot to sampling-plan and rule mapping; no Oracle foreign key.';
COMMENT ON TABLE "YMES".tblspcqattrulemapparameter IS
    'QC203 parameter values by QCAttSPSerial and RetestLot; no Oracle foreign key.';
COMMENT ON SEQUENCE "YMES".qcattspserial_seq IS
    'Optional YMES ID source for SP plus an eight-digit number. Check the existing shared serial service before use.';

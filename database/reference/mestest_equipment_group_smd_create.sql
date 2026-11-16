CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST.TBLSMDAREARELATION and TBLSMDSMTAREAEQPMAP DDL/data dictionary, queried 2026-11-16.
-- Target: PostgreSQL, schema YMES. Empty tables only; no MES business records.
-- No foreign keys exist in the Oracle source. Preserve the source secondary index on TBLSMDAREARELATION.
CREATE TABLE IF NOT EXISTS ymes.tblsmdarearelation (
    baseareano varchar(50) NOT NULL,
    containareano varchar(50) NOT NULL,
    objectno varchar(50) NOT NULL,
    objecttype numeric(1,0) NOT NULL,
    objecttext varchar(50) NOT NULL,
    objectx numeric(11,0) NOT NULL,
    objecty numeric(11,0) NOT NULL,
    objectlength numeric(11,0) NOT NULL,
    objectwidth numeric(11,0) NOT NULL,
    objectcolor numeric(11,0) NOT NULL,
    objectsequence numeric(11,0),
    objectareatype numeric(11,0),
    bordercolor numeric(11,0),
    borderstyle numeric(1,0),
    textcolor numeric(11,0),
    CONSTRAINT pk_tblsmdarearelation PRIMARY KEY (baseareano, objectno)
);
CREATE INDEX IF NOT EXISTS i_tblsmdarearelation_01
    ON ymes.tblsmdarearelation (containareano, objecttype);
COMMENT ON TABLE ymes.tblsmdarearelation IS 'MES Oracle source: MESTEST.TBLSMDAREARELATION';

CREATE TABLE IF NOT EXISTS ymes.tblsmdsmtareaeqpmap (
    baseareano varchar(50) NOT NULL,
    areano varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    eqpsequence numeric(11,0),
    equipmentgroup varchar(50),
    CONSTRAINT pk_tblsmdsmtareaeqpmap PRIMARY KEY (baseareano, areano, equipmentno)
);
COMMENT ON TABLE ymes.tblsmdsmtareaeqpmap IS 'MES Oracle source: MESTEST.TBLSMDSMTAREAEQPMAP';
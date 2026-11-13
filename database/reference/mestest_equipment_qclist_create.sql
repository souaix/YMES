-- Source: MES Oracle MESTEST DBMS_METADATA DDL for TBLEQPQCLISTBASIS and TBLEQPQCLISTDETAIL.
-- Target: PostgreSQL schema ymes. Empty objects only; no MES QC list data is copied.
-- The Oracle tables do not declare foreign keys; the MES Service manages both records together.
-- Compare existing YMES tables column by column before applying; IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tbleqpqclistbasis (
    qclistno varchar(100) NOT NULL,
    qclistname varchar(100) NOT NULL,
    qctype numeric(2,0) NOT NULL,
    unvalidaction numeric(2,0),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    aftereventqc numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpqclistbasis PRIMARY KEY (qclistno)
);
COMMENT ON TABLE ymes.tbleqpqclistbasis IS 'MES Oracle source: MESTEST.TBLEQPQCLISTBASIS';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.qclistno IS 'Equipment QC list identifier';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.qclistname IS 'Equipment QC list display name';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.qctype IS 'QC list schedule/type ValueList value';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.unvalidaction IS 'EquipmentState to set when QC is invalid; -1 means No Action';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.description IS 'QC list description';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.creator IS 'Creator user number';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.createdate IS 'Creation timestamp';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.aftereventqc IS 'After-event QC flag; UI enables only for QCType 0';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.datastamp IS 'Optimistic concurrency version';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.reviser IS 'Last reviser user number';
COMMENT ON COLUMN ymes.tbleqpqclistbasis.revisedate IS 'Last revision timestamp';

CREATE TABLE IF NOT EXISTS ymes.tbleqpqclistdetail (
    qclistno varchar(100) NOT NULL,
    qcorder numeric(2,0) NOT NULL,
    qcitem varchar(1000) NOT NULL,
    qctype numeric(1,0) NOT NULL,
    stdvalue varchar(12),
    maxivalue varchar(12),
    minivalue varchar(12),
    inputdatacount numeric(2,0) DEFAULT 1 NOT NULL,
    inspectionmethod varchar(100),
    inspectionstandards varchar(100),
    CONSTRAINT pk_tbleqpqclistdetail PRIMARY KEY (qclistno, qcorder)
);
COMMENT ON TABLE ymes.tbleqpqclistdetail IS 'MES Oracle source: MESTEST.TBLEQPQCLISTDETAIL';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.qclistno IS 'Parent QC list identifier';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.qcorder IS 'Display and execution sequence';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.qcitem IS 'QC check item text';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.qctype IS '0=StdValue, 1=Range, 2=Message, 3=InputData';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.stdvalue IS 'Expected value when QCType is 0';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.maxivalue IS 'Range maximum; * means unbounded';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.minivalue IS 'Range minimum; * means unbounded';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.inputdatacount IS 'Input/sample count; WinForms control range is 1 to 10';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.inspectionmethod IS 'Inspection method';
COMMENT ON COLUMN ymes.tbleqpqclistdetail.inspectionstandards IS 'Inspection acceptance standard';

-- The source DDL has no FK/index beyond each table primary key.
-- Parent/detail replacement and deletion are grouped by the legacy Service call.

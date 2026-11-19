BEGIN;
-- 2026-11-19 PPK read-only catalog audit: table absent in all schemas.
-- Oracle source: mestest_qc205_theil_custom_source_ddl.sql (27 columns).
-- PG comparison: mestest_qc210_variable_measurement_log_delete_create.sql.
-- Only this shared measurement log is needed by the QC301 deletion guard.
-- Preserve the captured source fields, including the original POSION spelling.
-- No business rows, foreign keys, dynamic ALTER, or other QC210 tables.

CREATE TABLE "YMES".tblspcvardetaillog (
    spcserial varchar(1500) NOT NULL,
    factorserial varchar(50),
    qcitemno varchar(25) NOT NULL,
    serialno numeric(5,0) NOT NULL,
    testvalue numeric(38,20) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    lotserial varchar(55),
    memo varchar(255),
    componentno varchar(50),
    loggroupserial varchar(50),
    x varchar(10),
    y varchar(10),
    thickness varchar(500),
    posion varchar(500),
    shift varchar(500),
    line varchar(500),
    version varchar(500),
    material varchar(500),
    eqptype varchar(500),
    equipment varchar(500),
    process varchar(500),
    op varchar(500),
    customer varchar(500),
    employee varchar(500),
    product varchar(500),
    fm varchar(500),
    al_spec varchar(500),
    CONSTRAINT pk_tblspcvardetaillog PRIMARY KEY (spcserial, serialno)
);

-- Oracle I_TBLSPCVARDETAILLOG_01/02/03; the PK creates its own unique index.
CREATE INDEX i_tblspcvardetaillog_01 ON "YMES".tblspcvardetaillog (factorserial);
CREATE INDEX i_tblspcvardetaillog_02 ON "YMES".tblspcvardetaillog (lotserial);
CREATE INDEX i_tblspcvardetaillog_03 ON "YMES".tblspcvardetaillog (qcitemno);

GRANT SELECT ON "YMES".tblspcvardetaillog TO ymes_app;
COMMIT;

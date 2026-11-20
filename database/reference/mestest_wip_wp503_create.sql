-- PostgreSQL CREATE-only reference for the ERFBatchHoldGenType=0 branch of WP503.
-- Oracle source: mestest_qc409_wip_disposition_dependencies_source_ddl.sql.
-- Compare YMES live schema and migration history before making a forward migration.

CREATE TABLE IF NOT EXISTS ymes.tblwiperfbatchlot (
    erfno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    reflotflag numeric(1,0) NOT NULL,
    loggroupserial varchar(50),
    CONSTRAINT pk_tblwiperfbatchlot PRIMARY KEY (erfno, lotno, opno)
);

COMMENT ON TABLE ymes.tblwiperfbatchlot IS 'MES Oracle source: MESTEST.TBLWIPERFBATCHLOT';

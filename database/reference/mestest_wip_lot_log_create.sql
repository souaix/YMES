-- Oracle source: MESTEST.TBLWIPLOTLOG in mestest_qc409_wip_disposition_dependencies_source_ddl.sql.
-- PostgreSQL CREATE-only reference for existing YMES schema comparison; not a migration.
-- The Oracle source declares only PK(LOTSERIAL), I_TBLWIPLOTLOG_01(LOGGROUPSERIAL),
-- and I_TBLWIPLOTLOG_02(LOTNO). No additional key or foreign key is inferred here.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwiplotlog (
    lotserial varchar(55) NOT NULL,
    lotno varchar(50),
    opno varchar(20),
    status numeric(1,0) DEFAULT 0,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    nodeid varchar(50),
    baselotno varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    preopendtime timestamp without time zone,
    areano varchar(20),
    moduleno varchar(50),
    moduleversion varchar(3),
    inputqty numeric(12,4),
    inputunitno varchar(30),
    loggroupserial varchar(50),
    opgroupno varchar(20) DEFAULT 'N/A',
    pdlineno varchar(50) DEFAULT 'N/A',
    defectqty numeric(12,4) DEFAULT 0,
    CONSTRAINT pk_tblwiplotlog PRIMARY KEY (lotserial)
);

CREATE INDEX IF NOT EXISTS i_tblwiplotlog_01 ON ymes.tblwiplotlog (loggroupserial);
CREATE INDEX IF NOT EXISTS i_tblwiplotlog_02 ON ymes.tblwiplotlog (lotno);

COMMENT ON TABLE ymes.tblwiplotlog IS 'MES Oracle source: MESTEST.TBLWIPLOTLOG';

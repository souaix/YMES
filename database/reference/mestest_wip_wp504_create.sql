-- PostgreSQL CREATE-only reference for WP504 Future Hold setup and downstream application.
-- Oracle source: mestest_wip_wp504_source_ddl.sql / _source_indexes.sql / _metadata.txt.
-- Compare YMES live schema and migrations before creating a forward migration.

CREATE TABLE IF NOT EXISTS ymes.tblwipfutureholdreason (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    holditemno varchar(50) NOT NULL,
    lotserial varchar(55),
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    futureholdserial varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblwipfutureholdreason PRIMARY KEY (holditemno, holdtime, lotno, moduleno, moduleversion, opno, futureholdserial)
);

COMMENT ON TABLE ymes.tblwipfutureholdreason IS 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDREASON';

-- Written by the later ERFType=3 application path, not by WP504 Set itself.
-- Oracle has no primary key or unique index for this table.
CREATE TABLE IF NOT EXISTS ymes.tblwipfutureholdlog (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    mclassno varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    erfno varchar(20) NOT NULL,
    holddescription varchar(4000) NOT NULL
);

CREATE INDEX IF NOT EXISTS i_tblwipfutureholdlog_01 ON ymes.tblwipfutureholdlog (lotno);

COMMENT ON TABLE ymes.tblwipfutureholdlog IS 'MES Oracle source: MESTEST.TBLWIPFUTUREHOLDLOG; downstream ERFType=3 lifecycle';

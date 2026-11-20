-- PostgreSQL CREATE-only structural reference from Oracle MESTEST DDL.
-- Compare live YMES schema and migration history before writing a forward migration.
-- TBLWIPERFCREATEDATA, TBLWIPERFHOLDREASON and BKTBLWIPERFCREATEDATA
-- were queried from MESTEST for WP501. The other three source definitions exist in
-- mestest_qc409_wip_disposition_dependencies_source_ddl.sql.

CREATE TABLE IF NOT EXISTS ymes.tblwiperfcreatedata (
    repairno varchar(60) NOT NULL,
    lotno varchar(50) NOT NULL,
    erftype numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    erfqty numeric(12,4) NOT NULL,
    holduser varchar(10) NOT NULL,
    holddate timestamp without time zone NOT NULL,
    erfsource numeric(1,0) DEFAULT 0 NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    spcserial varchar(1500),
    qcitemtype numeric(1,0),
    customerno varchar(50),
    productno varchar(50),
    holddescription text,
    holdtime numeric(1,0),
    CONSTRAINT pk_tblwiperfcreatedata PRIMARY KEY (repairno)
);

COMMENT ON TABLE ymes.tblwiperfcreatedata IS 'MES Oracle source: MESTEST.TBLWIPERFCREATEDATA';

CREATE TABLE IF NOT EXISTS ymes.tblwiperfholdreason (
    erfno varchar(20) NOT NULL,
    holditemno varchar(50) NOT NULL,
    qty numeric(12,4) DEFAULT 0,
    lotserial varchar(55),
    componentno varchar(50) DEFAULT 'N/A' NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    description varchar(255),
    areano varchar(10) NOT NULL,
    CONSTRAINT pk_tblwiperfholdreason PRIMARY KEY (erfno, componentno, holditemno, areano)
);

CREATE INDEX IF NOT EXISTS i_tblwiperfholdreason_01
    ON ymes.tblwiperfholdreason (lotserial);

COMMENT ON TABLE ymes.tblwiperfholdreason IS 'MES Oracle source: MESTEST.TBLWIPERFHOLDREASON';

CREATE TABLE IF NOT EXISTS ymes.tblwiperfbasis (
    erfno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    erftype numeric(1,0) NOT NULL,
    status numeric(2,0) DEFAULT 0 NOT NULL,
    ruleno varchar(4000),
    opno varchar(20) NOT NULL,
    customerno varchar(50),
    mclassno varchar(30) NOT NULL,
    scrapqty numeric(12,4) NOT NULL,
    unitno varchar(30) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    curgroupno varchar(100) NOT NULL,
    erfsource numeric(1,0) DEFAULT 0 NOT NULL,
    productno varchar(50),
    lotisvalid numeric(1,0) NOT NULL,
    holdtime numeric(1,0),
    createdepartmentno varchar(20),
    dutydepartmentno varchar(20) DEFAULT 'N/A' NOT NULL,
    createerf numeric(1,0) NOT NULL,
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    modulenodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    modulestageno varchar(50) DEFAULT 'N/A' NOT NULL,
    createqty numeric(12,4),
    releaseqty numeric(12,4),
    releaser varchar(10),
    releasedate timestamp without time zone,
    dispdescription varchar(4000),
    contloggroupserial varchar(50),
    baselotno varchar(50),
    batchlot varchar(4000),
    lotdisptypelimit varchar(30),
    mono varchar(50),
    curqty numeric(12,4),
    holddescription text,
    nodeid varchar(50),
    materialno varchar(50) DEFAULT 'N/A' NOT NULL,
    vendorno varchar(20) DEFAULT 'N/A' NOT NULL,
    qrsno varchar(50),
    CONSTRAINT pk_tblwiperfbasis PRIMARY KEY (erfno)
);

CREATE INDEX IF NOT EXISTS i_tblwiperfbasis_01 ON ymes.tblwiperfbasis (lotserial);
COMMENT ON TABLE ymes.tblwiperfbasis IS 'MES Oracle source: MESTEST.TBLWIPERFBASIS';

CREATE TABLE IF NOT EXISTS ymes.tblwiperfholdreasondisp (
    erfno varchar(20) NOT NULL,
    holditemno varchar(50) NOT NULL,
    qty numeric(12,4) DEFAULT 0,
    holddisposition varchar(100),
    lotserial varchar(55),
    componentno varchar(50) DEFAULT 'N/A' NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    description varchar(255),
    areano varchar(10) NOT NULL,
    CONSTRAINT pk_tblwiperfholdreasondisp PRIMARY KEY (erfno, componentno, holditemno, areano)
);

CREATE INDEX IF NOT EXISTS i_tblwiperfholdreasondisp_01 ON ymes.tblwiperfholdreasondisp (lotserial);
COMMENT ON TABLE ymes.tblwiperfholdreasondisp IS 'MES Oracle source: MESTEST.TBLWIPERFHOLDREASONDISP';

CREATE TABLE IF NOT EXISTS ymes.tblwiperf_attachfile (
    lotno varchar(50) NOT NULL,
    erfno varchar(20) NOT NULL,
    attachtype varchar(10) NOT NULL,
    attachname varchar(50) NOT NULL,
    status numeric(1,0) DEFAULT 0 NOT NULL,
    attachpoint numeric(1,0) DEFAULT 0 NOT NULL,
    attachbody bytea,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwiperf_attachfile PRIMARY KEY (lotno, erfno, attachname)
);

COMMENT ON TABLE ymes.tblwiperf_attachfile IS 'MES Oracle source: MESTEST.TBLWIPERF_ATTACHFILE';

-- The backup table has no PK or unique constraint in Oracle. HOLDUSER is
-- varchar(50), unlike TBLWIPERFCREATEDATA.HOLDUSER varchar(10).
CREATE TABLE IF NOT EXISTS ymes.bktblwiperfcreatedata (
    repairno varchar(60) NOT NULL,
    lotno varchar(50) NOT NULL,
    erftype numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    erfqty numeric(12,4) NOT NULL,
    holduser varchar(50) NOT NULL,
    holddate timestamp without time zone NOT NULL,
    erfsource numeric(1,0) DEFAULT 0 NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    spcserial varchar(1500),
    qcitemtype numeric(1,0),
    customerno varchar(50),
    productno varchar(50),
    holddescription text,
    holdtime numeric(1,0)
);

CREATE INDEX IF NOT EXISTS i_bktblwiperfcreatedata_01 ON ymes.bktblwiperfcreatedata (lotno);
COMMENT ON TABLE ymes.bktblwiperfcreatedata IS 'MES Oracle source: MESTEST.BKTBLWIPERFCREATEDATA';

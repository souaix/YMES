CREATE SCHEMA IF NOT EXISTS ymes;

-- PostgreSQL CREATE-only reference generated from MESTEST dictionary metadata on 2026-11-19.
-- Empty schema only. Compare YMES live schema and migrations before applying; do not replace shared tables.
-- Oracle source defaults retained from DBMS_METADATA.
-- Oracle DATE is mapped to timestamp without time zone; Oracle NUMBER maps to numeric.

-- Source: MESTEST.TBLWIPLOTBASIS. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotbasis (
    baselotno varchar(50) NOT NULL,
    orglotno varchar(50) NOT NULL,
    lotstate numeric(3,0) NOT NULL,
    rono varchar(25) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    mono varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    customerlotno varchar(50),
    customerno varchar(50),
    inputqty numeric(12,4) NOT NULL,
    inputunitno varchar(30) NOT NULL,
    priority numeric(2,0) DEFAULT 99,
    hotlot numeric(2,0) DEFAULT 0,
    wipdate timestamp without time zone,
    description varchar(255),
    createdate timestamp without time zone,
    creator varchar(10),
    lotserial varchar(55),
    engno varchar(30) DEFAULT 'N/A' NOT NULL,
    engversion varchar(3) DEFAULT 'N/A' NOT NULL,
    deviceno varchar(50),
    ex_lotbasis1 varchar(20),
    ex_lotbasis2 varchar(20),
    ex_lotbasis3 varchar(20),
    planfinishdate timestamp without time zone,
    returnno varchar(20) DEFAULT 'N/A',
    packinglevel numeric(1,0),
    g_ingotno varchar(50),
    xx_subbigqty numeric,
    xx_lotqty numeric,
    xx_suborderqty numeric,
    org_sap_pcsqty numeric(22,0),
    org_sap_argqty numeric(22,0),
    org_sap_prdqty numeric(22,0),
    sap_pcsqty numeric(22,0),
    sap_argqty numeric(22,0),
    sap_prdqty numeric(22,0),
    CONSTRAINT pk_tblwiplotbasis PRIMARY KEY (baselotno)
);
COMMENT ON TABLE ymes.tblwiplotbasis IS 'MES Oracle source: MESTEST.TBLWIPLOTBASIS';

-- Source: MESTEST.TBLWIPLOTSTATE. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotstate (
    lotno varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    orglotno varchar(50) NOT NULL,
    status numeric(2,0) NOT NULL,
    lotserial varchar(55) NOT NULL,
    areano varchar(20) NOT NULL,
    psno varchar(50) NOT NULL,
    processno varchar(30) NOT NULL,
    opno varchar(20) NOT NULL,
    nodeid varchar(50) NOT NULL,
    curqty numeric(12,4) NOT NULL,
    curunitno varchar(30) NOT NULL,
    sysqty numeric(12,4) NOT NULL,
    sysunitno varchar(30) NOT NULL,
    brno varchar(20) NOT NULL,
    phaseno numeric(2,0) DEFAULT 0 NOT NULL,
    havecomponent numeric(1,0) NOT NULL,
    havelevel numeric(1,0) NOT NULL,
    psorder numeric(2,0) NOT NULL,
    linkname varchar(20),
    reverseid numeric(6,0),
    eventtime timestamp without time zone,
    lotstamp numeric(11,0) DEFAULT 0 NOT NULL,
    gostatus numeric(2,0),
    processversion varchar(3) DEFAULT 'N/A' NOT NULL,
    nodeversion varchar(3) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    opreference numeric(1,0) DEFAULT 0 NOT NULL,
    loggroupserial varchar(50),
    preopendtime timestamp without time zone,
    arrivetime timestamp without time zone,
    opstatus numeric(1,0),
    engno varchar(30) DEFAULT 'N/A' NOT NULL,
    engversion varchar(3) DEFAULT 'N/A' NOT NULL,
    recipeno varchar(300) DEFAULT 'N/A' NOT NULL,
    recipeversion varchar(15) DEFAULT 'N/A' NOT NULL,
    recipesql varchar(400) DEFAULT 'N/A' NOT NULL,
    batchserial varchar(50),
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    modulenodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    modulestageno varchar(50) DEFAULT 'N/A' NOT NULL,
    modulesequence numeric(4,0) DEFAULT '0' NOT NULL,
    mainprocessno varchar(30),
    mainprocessversion varchar(3),
    eventuserno varchar(10),
    eventdescription varchar(255),
    ex_lotstate1 varchar(20),
    ex_lotstate2 varchar(20),
    ex_lotstate3 varchar(20),
    factoryno varchar(20) DEFAULT 'N/A' NOT NULL,
    opgroupno varchar(20) DEFAULT 'N/A',
    golinkname varchar(20),
    pdlineno varchar(50),
    carrierno varchar(50),
    cleanlinestate numeric(1,0),
    ecntype_basis varchar(10),
    ecnno_basis varchar(20),
    ecntype_process varchar(10),
    ecnno_process varchar(20),
    ecntype_op varchar(10),
    ecnno_op varchar(20),
    xx_scrapstate varchar(50) DEFAULT ' ' NOT NULL,
    CONSTRAINT pk_tblwiplotstate PRIMARY KEY (lotno, opno, status)
);
COMMENT ON TABLE ymes.tblwiplotstate IS 'MES Oracle source: MESTEST.TBLWIPLOTSTATE';

-- Source: MESTEST.TBLWIPCOMPONENTSTATE. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwipcomponentstate (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    executestate numeric(1,0) DEFAULT 0,
    status numeric(1,0) DEFAULT 0 NOT NULL,
    lotserial varchar(55),
    componentlotno varchar(50),
    slotno varchar(3),
    s_batchid varchar(150),
    s_cassetteno varchar(50),
    s_color varchar(100),
    s_cycleno varchar(100),
    s_epi_invqprun varchar(10),
    s_feedbackstate varchar(100),
    s_hoopno varchar(50),
    s_lasermark varchar(50),
    s_mocvdeqpno varchar(100),
    s_position varchar(100),
    s_runno varchar(100),
    s_substrate_id varchar(100),
    s_wafercode varchar(50),
    s_waferfeedbackstatus varchar(2) DEFAULT 'W',
    s_wafersize varchar(100),
    s_formbatchseqno numeric(3,0),
    s_glotseqno numeric(3,0),
    s_glotid varchar(50),
    s_glotserialno varchar(50),
    s_batchseqno numeric(3,0),
    s_rungroupid varchar(50),
    s_rungroupseqno numeric(3,0),
    s_rungroupserialno varchar(50),
    g_lasermark varchar(50),
    CONSTRAINT pk_tblwipcomponentstate PRIMARY KEY (lotno, componentno)
);
COMMENT ON TABLE ymes.tblwipcomponentstate IS 'MES Oracle source: MESTEST.TBLWIPCOMPONENTSTATE';

-- Source: MESTEST.TBLWIPLOTPROCESS. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotprocess (
    lotno varchar(50) NOT NULL,
    psno varchar(50) NOT NULL,
    processorder numeric(2,0) NOT NULL,
    processno varchar(30) NOT NULL,
    opno varchar(50) NOT NULL,
    nodeid varchar(50) NOT NULL,
    psorder numeric(2,0) NOT NULL,
    havecomponent numeric(1,0) NOT NULL,
    havelevel numeric(1,0) NOT NULL,
    lotserial varchar(55),
    processversion varchar(3) DEFAULT 'N/A' NOT NULL,
    nodeversion varchar(3) DEFAULT 'N/A' NOT NULL,
    startnodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblwiplotprocess PRIMARY KEY (lotno, processorder)
);
COMMENT ON TABLE ymes.tblwiplotprocess IS 'MES Oracle source: MESTEST.TBLWIPLOTPROCESS';

-- Source: MESTEST.TBLWIPLOTNOTEXECUTEOP. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotnotexecuteop (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL
);
COMMENT ON TABLE ymes.tblwiplotnotexecuteop IS 'MES Oracle source: MESTEST.TBLWIPLOTNOTEXECUTEOP';

-- Source: MESTEST.TBLWIPLOTPROPERTY_RECIVE. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotproperty_recive (
    baselotno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    CONSTRAINT pk_tblwiplotproperty_recive PRIMARY KEY (baselotno, propertyno)
);
COMMENT ON TABLE ymes.tblwiplotproperty_recive IS 'MES Oracle source: MESTEST.TBLWIPLOTPROPERTY_RECIVE';

-- Source: MESTEST.TBLWIPLOTPROPERTY. Oracle table definition and dictionary, queried 2026-11-19.
CREATE TABLE IF NOT EXISTS ymes.tblwiplotproperty (
    lotno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblwiplotproperty PRIMARY KEY (lotno, propertyno)
);
COMMENT ON TABLE ymes.tblwiplotproperty IS 'MES Oracle source: MESTEST.TBLWIPLOTPROPERTY';

-- Non-constraint Oracle indexes; PK indexes are represented by table constraints above.
CREATE INDEX IF NOT EXISTS i_tblwiplotbasis_01 ON ymes.tblwiplotbasis (mono);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_01 ON ymes.tblwiplotstate (baselotno);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_02 ON ymes.tblwiplotstate (psno);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_03 ON ymes.tblwiplotstate (areano);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_04 ON ymes.tblwiplotstate (opno);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_05 ON ymes.tblwiplotstate (status);
CREATE INDEX IF NOT EXISTS i_tblwiplotstate_06 ON ymes.tblwiplotstate (batchserial);
CREATE INDEX IF NOT EXISTS i_tblwipcomponentstate_01 ON ymes.tblwipcomponentstate (s_glotid);
CREATE INDEX IF NOT EXISTS i_tblwipcomponentstate_02 ON ymes.tblwipcomponentstate (s_cassetteno);

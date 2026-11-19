-- PostgreSQL CREATE-only translation from Oracle MESTEST metadata; retrieved 2026-11-19.
-- Reference only: not a migration. Compare YMES live schema/migrations before use.
-- Translates columns, simple literal defaults, primary and unique keys.
-- Oracle indexes, check/foreign-key constraints, tablespaces and storage clauses are not translated.
-- Simple Oracle defaults are translated; all 39 column counts match Oracle ALL_TAB_COLUMNS.
CREATE SCHEMA IF NOT EXISTS ymes;

-- Oracle source table: TBLEQPEQUIPMENTBASIS
CREATE TABLE IF NOT EXISTS ymes.tbleqpequipmentbasis (
    equipmentno varchar(50) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    capacity numeric(8,0) DEFAULT -1,
    vendorno varchar(50) NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    engineergroupno varchar(20),
    assetno varchar(50),
    equipmentclass varchar(50),
    loadport numeric(2,0) DEFAULT 0,
    autoflag numeric(1,0),
    eacontroller varchar(200),
    eqprecipe numeric(1,0),
    qclistno varchar(50),
    maxtime numeric(6,2),
    fixeqptime numeric(6,2) DEFAULT 0,
    vareqptime numeric(6,2) DEFAULT 0,
    countopunitqty numeric(6,0) DEFAULT 1,
    counteqpunitqty numeric(6,0) DEFAULT 1,
    counter numeric(5,0),
    equipmentname varchar(255),
    erpno varchar(50),
    queueserver varchar(50),
    requestmsgqueue varchar(100),
    responsemsgqueue varchar(100),
    chamberflag numeric(1,0) DEFAULT 0,
    belongequipmentno varchar(50),
    equipmentclassno numeric(3,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    requestmsgqueue_mes varchar(100),
    responsemsgqueue_eap varchar(100),
    xx_keeperno varchar(50),
    CONSTRAINT pk_tbleqpequipmentbasis PRIMARY KEY (equipmentno)
);

-- Oracle source table: TBLEQPGROUPDETAIL
CREATE TABLE IF NOT EXISTS ymes.tbleqpgroupdetail (
    equipmentgroup varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpgroupdetail PRIMARY KEY (equipmentgroup, equipmentno)
);

-- Oracle source table: TBLINVFGDINVENTORY
CREATE TABLE IF NOT EXISTS ymes.tblinvfgdinventory (
    inventoryno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    unitno varchar(30),
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    locqty numeric(12,4) NOT NULL,
    belongtotype numeric(1,0) DEFAULT -1,
    belongtono varchar(50),
    inputdate timestamp without time zone,
    baselotno varchar(50),
    fgdinno varchar(20),
    CONSTRAINT pk_tblinvfgdinventory PRIMARY KEY (inventoryno, lotno, locatorno)
);

-- Oracle source table: TBLINVSEMIINVENTORY
CREATE TABLE IF NOT EXISTS ymes.tblinvsemiinventory (
    inventoryno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    unitno varchar(30),
    lotno varchar(50) NOT NULL,
    locatorno varchar(20) NOT NULL,
    locqty numeric(12,4) NOT NULL,
    belongtotype numeric(1,0) DEFAULT -1,
    belongtono varchar(50),
    inputdate timestamp without time zone,
    baselotno varchar(50),
    semiinno varchar(20),
    CONSTRAINT pk_tblinvsemiinventory PRIMARY KEY (inventoryno, lotno, locatorno)
);

-- Oracle source table: TBLINVWIP_SCRAP
CREATE TABLE IF NOT EXISTS ymes.tblinvwip_scrap (
    inventoryno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    unitno varchar(30) NOT NULL,
    scrapqty numeric(12,4) DEFAULT 0,
    baselotno varchar(50) NOT NULL,
    lossqty numeric(12,4) DEFAULT 0,
    inputdate timestamp without time zone,
    loggroupserial varchar(50),
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwip_scrap PRIMARY KEY (inventoryno, lotserial, locatorno)
);

-- Oracle source table: TBLINVWIPINVENTORY_RAW
CREATE TABLE IF NOT EXISTS ymes.tblinvwipinventory_raw (
    inventoryno varchar(20) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(500) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(16,8) NOT NULL,
    materialtype varchar(50) NOT NULL,
    inputdate timestamp without time zone,
    expiredate timestamp without time zone,
    opendate timestamp without time zone,
    frommateriallotno varchar(500),
    acclimationdate timestamp without time zone,
    manufacturedate timestamp without time zone,
    gracedate timestamp without time zone,
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    frozendate timestamp without time zone,
    executor varchar(10),
    gumtypestatus numeric(1,0),
    acctimes numeric(2,0),
    frozentimes numeric(2,0),
    vendorno varchar(50),
    xx_status numeric(1,0),
    sap_charg varchar(20),
    sap_aufnr varchar(25),
    sap_zzsublot varchar(30),
    sap_bwart varchar(3),
    xx_createdate timestamp without time zone,
    xx_if_matr varchar(3),
    xx_closeuser varchar(15),
    xx_recycle_date timestamp without time zone,
    xx_receiveuser varchar(15),
    xx_remain_amt numeric(38,5),
    xx_recycleuser varchar(15),
    xx_datasource varchar(10),
    xx_id numeric,
    xx_if_irno varchar(1) DEFAULT 'N',
    xx_acclimationdate_end timestamp without time zone,
    CONSTRAINT pk_tblinvwipinventory_raw PRIMARY KEY (inventoryno, materialno, materiallotno, locatorno)
);

-- Oracle source table: TBLINVWIPINVENTORY_SEMI
CREATE TABLE IF NOT EXISTS ymes.tblinvwipinventory_semi (
    inventoryno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    lotno varchar(50) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(14,6) NOT NULL,
    inputdate timestamp without time zone,
    baselotno varchar(50),
    boughten numeric(1,0) NOT NULL,
    bookingflag numeric(1,0) DEFAULT 0 NOT NULL,
    productversion varchar(5),
    locatorno varchar(20) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblinvwipinventory_semi PRIMARY KEY (inventoryno, lotno, locatorno)
);

-- Oracle source table: TBLPRDENGRECIPE_ATTRIBUTE
CREATE TABLE IF NOT EXISTS ymes.tblprdengrecipe_attribute (
    recipeno varchar(60) NOT NULL,
    recipeversion varchar(3) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribsource numeric(1,0) DEFAULT 0 NOT NULL,
    attribsequence numeric(2,0) NOT NULL,
    attribvalue varchar(255) DEFAULT ' ',
    attribscript varchar(3000) DEFAULT ' ',
    lotpropertyno varchar(20),
    engshow numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblprdengrecipe_attribute PRIMARY KEY (recipeno, recipeversion, attribno)
);

-- Oracle source table: TBLPRDLIMITEDTIMECONTROL
CREATE TABLE IF NOT EXISTS ymes.tblprdlimitedtimecontrol (
    boundary varchar(5) NOT NULL,
    referenceno varchar(50) NOT NULL,
    referenceversion varchar(5) NOT NULL,
    opreference numeric(1,0) NOT NULL,
    motypeno numeric(2,0) NOT NULL,
    fromopno varchar(20) NOT NULL,
    toopno varchar(20) NOT NULL,
    validminute numeric(7,2) NOT NULL,
    mclassno varchar(30),
    holddescription varchar(4000),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    allowbetwenophold numeric(1,0) DEFAULT 0,
    frombrno varchar(20) DEFAULT 'N/A' NOT NULL,
    tobrno varchar(20) DEFAULT 'N/A' NOT NULL,
    eqpavailablecontrol numeric(1,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblprdlimitedtimecontrol PRIMARY KEY (boundary, referenceno, referenceversion, motypeno, fromopno, toopno, frombrno, tobrno)
);

-- Oracle source table: TBLSPCIPQCFORM
CREATE TABLE IF NOT EXISTS ymes.tblspcipqcform (
    qcformno varchar(30) NOT NULL,
    productno varchar(50) NOT NULL,
    qty numeric(16,8) NOT NULL,
    status numeric(1,0) NOT NULL,
    lotno varchar(30),
    customerno varchar(20),
    creator varchar(10),
    createdate timestamp without time zone,
    opno varchar(20) NOT NULL,
    productversion varchar(5) NOT NULL,
    finishtime timestamp without time zone,
    callbacktype varchar(20),
    s_offlinehold varchar(25) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    moduletype numeric(1,0) DEFAULT 0 NOT NULL,
    CONSTRAINT pk_tblspcipqcform PRIMARY KEY (qcformno)
);

-- Oracle source table: TBLSYSCOMPONENTPROPERTY
CREATE TABLE IF NOT EXISTS ymes.tblsyscomponentproperty (
    propertyno varchar(20) NOT NULL,
    propertyname varchar(50),
    datatype numeric(1,0) NOT NULL,
    transfertype numeric(1,0) DEFAULT 0 NOT NULL,
    tabletype numeric(1,0) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    updatetype numeric(1,0) DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsyscomponentproperty PRIMARY KEY (propertyno)
);

-- Oracle source table: TBLSYSLOTPROPERTYBASIS
CREATE TABLE IF NOT EXISTS ymes.tblsyslotpropertybasis (
    propertyno varchar(20) NOT NULL,
    propertyname varchar(50),
    datatype numeric(1,0) NOT NULL,
    transfertype numeric(1,0) DEFAULT 0 NOT NULL,
    tabletype numeric(1,0) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    updatetype numeric(1,0) DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsyslotpropertybasis PRIMARY KEY (propertyno)
);

-- Oracle source table: TBLSYSMOTYPEBASIS
CREATE TABLE IF NOT EXISTS ymes.tblsysmotypebasis (
    motypeno numeric(2,0) NOT NULL,
    motypename varchar(20) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    motypecode varchar(10),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmotypebasis PRIMARY KEY (motypeno)
);

-- Oracle source table: TBLWIPCOMPONENTPROPERTY
CREATE TABLE IF NOT EXISTS ymes.tblwipcomponentproperty (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblwipcomponentproperty PRIMARY KEY (lotno, componentno, propertyno)
);

-- Oracle source table: TBLWIPCONT_ATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwipcont_attrib (
    lotserial varchar(55),
    attribno varchar(20),
    attribvalue varchar(3000),
    loggroupserial varchar(50),
    eventtime timestamp without time zone,
    equipmenttype varchar(50)
);

-- Oracle source table: TBLWIPCONT_COMPONENT
CREATE TABLE IF NOT EXISTS ymes.tblwipcont_component (
    lotserial varchar(55),
    componentno varchar(50),
    cpass numeric(1,0),
    in_goodqty numeric(12,4),
    in_scrapqty numeric(12,4),
    out_goodqty numeric(12,4),
    out_scrapqty numeric(12,4),
    unitno varchar(30),
    loggroupserial varchar(50),
    in_carrierno varchar(50),
    in_slotno varchar(3),
    out_carrierno varchar(50),
    out_slotno varchar(3),
    chamberno varchar(50),
    chamber_starttime timestamp without time zone,
    chamber_endtime timestamp without time zone,
    s_batchid varchar(150),
    s_cycleno varchar(100),
    s_endtime timestamp without time zone,
    s_feedbackstate numeric(1,0),
    s_mocvdeqpno varchar(100),
    s_position varchar(100),
    s_runno varchar(100),
    s_starttime timestamp without time zone,
    s_substrate_id varchar(100),
    s_formbatchseqno numeric(3,0),
    s_batchseqno numeric(3,0)
);

-- Oracle source table: TBLWIPCONT_COMPONENTATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwipcont_componentattrib (
    lotserial varchar(55),
    componentno varchar(50),
    attribno varchar(20),
    attribvalue varchar(255),
    loggroupserial varchar(50),
    eventtime timestamp without time zone
);

-- Oracle source table: TBLWIPCONT_MATERIAL
CREATE TABLE IF NOT EXISTS ymes.tblwipcont_material (
    lotserial varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialtype varchar(50) NOT NULL,
    useqty numeric(16,8) NOT NULL,
    unitno varchar(30) NOT NULL,
    description varchar(255),
    loggroupserial varchar(50) NOT NULL,
    undistributeqty numeric(16,8),
    eventtime timestamp without time zone
);

-- Oracle source table: TBLWIPECNATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwipecnattrib (
    ecnno varchar(50) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribvalue varchar(255) NOT NULL,
    opno varchar(20) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblwipecnattrib PRIMARY KEY (ecnno, attribno)
);

-- Oracle source table: TBLWIPECNBASIS
CREATE TABLE IF NOT EXISTS ymes.tblwipecnbasis (
    ecnno varchar(50) NOT NULL,
    startdate timestamp without time zone,
    enddate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    customerno varchar(50) DEFAULT '*' NOT NULL,
    productno varchar(50) DEFAULT '*' NOT NULL,
    productversion varchar(5) DEFAULT '*' NOT NULL,
    mono varchar(50) DEFAULT '*' NOT NULL,
    CONSTRAINT pk_tblwipecnbasis PRIMARY KEY (ecnno)
);

-- Oracle source table: TBLWIPFUTUREHOLD
CREATE TABLE IF NOT EXISTS ymes.tblwipfuturehold (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    mclassno varchar(30),
    holddescription varchar(4000),
    creator varchar(10),
    createdate timestamp without time zone,
    lotserial varchar(55),
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    futureholdbatchno varchar(20),
    futureholdserial varchar(20) DEFAULT 'N/A' NOT NULL,
    lockflag numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblwipfuturehold PRIMARY KEY (lotno, moduleno, moduleversion, opno, holdtime, futureholdserial)
);

-- Oracle source table: TBLWIPFUTUREOS
CREATE TABLE IF NOT EXISTS ymes.tblwipfutureos (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(250),
    moduleno varchar(45) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    lotserial varchar(55),
    osno varchar(20) NOT NULL,
    CONSTRAINT pk_tblwipfutureos PRIMARY KEY (lotno, opno, moduleno, moduleversion, osno)
);

-- Oracle source table: TBLWIPFUTUREWAIT
CREATE TABLE IF NOT EXISTS ymes.tblwipfuturewait (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    waittime numeric(1,0) NOT NULL,
    description varchar(3000),
    creator varchar(10),
    createdate timestamp without time zone,
    lotserial varchar(55),
    CONSTRAINT pk_tblwipfuturewait PRIMARY KEY (lotno, opno, waittime)
);

-- Oracle source table: TBLWIPLIMITEDTIMECONTROL
CREATE TABLE IF NOT EXISTS ymes.tblwiplimitedtimecontrol (
    lotno varchar(50) NOT NULL,
    boundary varchar(5) NOT NULL,
    referenceno varchar(50) NOT NULL,
    referenceversion varchar(3) NOT NULL,
    fromopendtime timestamp without time zone NOT NULL,
    fromopno varchar(20) NOT NULL,
    toopno varchar(20) NOT NULL,
    motypeno numeric(2,0) DEFAULT -1 NOT NULL,
    frombrno varchar(20) DEFAULT 'N/A' NOT NULL,
    tobrno varchar(20) DEFAULT 'N/A' NOT NULL,
    ruleviolation numeric(1,0),
    CONSTRAINT pk_tblwiplimitedtimecontrol PRIMARY KEY (lotno, boundary, referenceno, referenceversion, fromopno, toopno, motypeno, frombrno, tobrno)
);

-- Oracle source table: TBLWIPLOTECNATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwiplotecnattrib (
    lotno varchar(50) NOT NULL,
    ecnno varchar(50) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribvalue varchar(255) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    activestartdate timestamp without time zone,
    activeenddate timestamp without time zone,
    CONSTRAINT pk_tblwiplotecnattrib PRIMARY KEY (lotno, ecnno, attribno)
);

-- Oracle source table: TBLWIPLOTLOG_REPORT
CREATE TABLE IF NOT EXISTS ymes.tblwiplotlog_report (
    loggroupserial varchar(50),
    baselotno varchar(50),
    lotno varchar(50),
    opno varchar(20),
    status numeric(1,0),
    preopendtime timestamp without time zone,
    arrivetime timestamp without time zone,
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    inputqty numeric(12,4),
    inputunitno varchar(30),
    goodqty numeric(12,4),
    failqty numeric(12,4),
    goodunitno varchar(30),
    engno varchar(30) DEFAULT 'N/A' NOT NULL,
    engversion varchar(3) DEFAULT 'N/A' NOT NULL,
    recipeno varchar(300) DEFAULT 'N/A' NOT NULL,
    recipeversion varchar(15) DEFAULT 'N/A' NOT NULL,
    recipesql varchar(400) DEFAULT 'N/A' NOT NULL,
    nodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    factoryno varchar(20) DEFAULT 'N/A' NOT NULL,
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    productno varchar(50),
    productversion varchar(5),
    customerlotno varchar(50),
    deviceno varchar(50),
    areano varchar(20) DEFAULT 'N/A' NOT NULL,
    lossqty numeric(12,4),
    completeflag numeric(1,0) DEFAULT 0 NOT NULL,
    checkintime timestamp without time zone,
    checkouttime timestamp without time zone,
    opgroupno varchar(20) DEFAULT 'N/A',
    serialno varchar(50),
    mono varchar(50),
    pdlineno varchar(50) DEFAULT 'N/A',
    erpno varchar(50),
    defectqty numeric(12,4) DEFAULT 0,
    ecntype_basis varchar(10),
    ecnno_basis varchar(20),
    ecntype_process varchar(10),
    ecnno_process varchar(20),
    ecntype_op varchar(10),
    ecnno_op varchar(20),
    i_failbinqty numeric(12,4),
    eqpsetuplistno varchar(20),
    sap_upload varchar(1) DEFAULT 'N',
    sap_rmzhl varchar(8)
);

-- Oracle source table: TBLWIPLOTOPAREA
CREATE TABLE IF NOT EXISTS ymes.tblwiplotoparea (
    lotno varchar(50) NOT NULL,
    nodeid varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    lotserial varchar(55),
    CONSTRAINT pk_tblwiplotoparea PRIMARY KEY (lotno, nodeid)
);

-- Oracle source table: TBLWIPOPMATERIALSTATE
CREATE TABLE IF NOT EXISTS ymes.tblwipopmaterialstate (
    opno varchar(20) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    unitno varchar(30) NOT NULL,
    qty numeric(14,6) NOT NULL,
    materialtype varchar(50),
    inputdate timestamp without time zone,
    CONSTRAINT pk_tblwipopmaterialstate PRIMARY KEY (opno, materialno, materiallotno)
);

-- Oracle source table: TBLWIPTEMP_ATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_attrib (
    lotno varchar(50) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribvalue varchar(3000) DEFAULT ' ',
    attribsource numeric(1,0) NOT NULL,
    attribphase numeric(2,0) NOT NULL,
    attribsequence numeric(2,0) NOT NULL,
    savetolotpropertyno varchar(20),
    lotserial varchar(55),
    qcitemno varchar(25),
    orgattribvalue varchar(3000),
    opno varchar(20),
    nodeid varchar(50),
    equipmenttype varchar(50)
);

-- Oracle source table: TBLWIPTEMP_COMPONENTATTRIB
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_componentattrib (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribvalue varchar(255) DEFAULT ' ',
    attribsource numeric(1,0) NOT NULL,
    attribphase numeric(2,0) NOT NULL,
    attribsequence numeric(2,0) NOT NULL,
    savetolotpropertyno varchar(20),
    lotserial varchar(55),
    opno varchar(20),
    nodeid varchar(50)
);

-- Oracle source table: TBLWIPTEMP_EQUIPMENT
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_equipment (
    lotno varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50),
    recipegroup varchar(50),
    recipeversion numeric(2,0),
    lotserial varchar(55),
    equipmentclass varchar(50),
    loadport numeric(2,0),
    loggroupserial varchar(50),
    inputqty numeric(12,4) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    planfinishtime timestamp without time zone,
    s_batchid varchar(150)
);

-- Oracle source table: TBLWIPTEMP_LOTSTATE
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_lotstate (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    inventoryno varchar(20),
    curqty numeric(12,4),
    curunitno varchar(30),
    mono varchar(50),
    lotserial varchar(50),
    productno varchar(50),
    productversion varchar(5),
    phaseno numeric(2,0),
    reverseid numeric(6,0),
    serialno varchar(50),
    baselotno varchar(50),
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    opreference numeric(1,0) DEFAULT 0 NOT NULL,
    nodeid varchar(50)
);

-- Oracle source table: TBLWIPTEMP_MATERIAL
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_material (
    lotno varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    stdqty numeric(16,8) NOT NULL,
    unitno varchar(30) NOT NULL,
    decreaserate numeric(3,2) NOT NULL,
    specified numeric(1,0) NOT NULL,
    putinplacetype numeric(1,0) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialtype varchar(50) NOT NULL,
    countway numeric(1,0) NOT NULL,
    checklotno numeric(1,0) NOT NULL,
    lotserial varchar(55),
    description varchar(255),
    opno varchar(20),
    nodeid varchar(50)
);

-- Oracle source table: TBLWIPTEMP_QCITEM
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_qcitem (
    lotno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    executeflag numeric(1,0) NOT NULL,
    executeorder numeric(2,0) DEFAULT 0,
    reinspwith2ndinspplan numeric(1,0) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    executeflag_fai numeric(1,0) DEFAULT 0,
    executeflag_ipqc numeric(1,0) DEFAULT 0,
);

-- Oracle source table: TBLWIPTEMP_SAMPLEBIN
CREATE TABLE IF NOT EXISTS ymes.tblwiptemp_samplebin (
    lotno varchar(50),
    lotserial varchar(55),
    bin1 numeric(6,0) DEFAULT 0,
    bin2 numeric(6,0) DEFAULT 0,
    bin3 numeric(6,0) DEFAULT 0,
    bin4 numeric(6,0) DEFAULT 0,
    bin5 numeric(6,0) DEFAULT 0,
    bin6 numeric(6,0) DEFAULT 0,
    bin7 numeric(6,0) DEFAULT 0,
    bin8 numeric(6,0) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    loggroupserial varchar(50)
);

-- Oracle source table: TBLWIPWAITBASIS
CREATE TABLE IF NOT EXISTS ymes.tblwipwaitbasis (
    waitno varchar(20) NOT NULL,
    baselotno varchar(50) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    waitsource numeric(1,0) NOT NULL,
    waittype numeric(1,0) NOT NULL,
    waittime numeric(1,0),
    status numeric(2,0) NOT NULL,
    lotisvalid numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    createqty numeric(12,4) NOT NULL,
    unitno varchar(30) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    customerno varchar(50),
    productno varchar(50),
    createdepartmentno varchar(20),
    ruleno varchar(20),
    waitdescription varchar(3000),
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    modulenodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    modulestageno varchar(50) DEFAULT 'N/A' NOT NULL,
    releaseqty numeric(12,4),
    releaser varchar(10),
    releasedate timestamp without time zone,
    contloggroupserial varchar(50),
    batchwaitno varchar(20),
    mono varchar(50),
    CONSTRAINT pk_tblwipwaitbasis PRIMARY KEY (waitno)
);

-- Oracle source table: XX_TBLLOTMARITX_THEIL_BU1
CREATE TABLE IF NOT EXISTS ymes.xx_tbllotmaritx_theil_bu1 (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    matrix_code varchar(3000) NOT NULL,
    CONSTRAINT xx_tbllotmaritx_theil_bu1 PRIMARY KEY (lotno, opno, matrix_code)
);

-- Oracle source table: XX_TBLSPCWAIVE
CREATE TABLE IF NOT EXISTS ymes.xx_tblspcwaive (
    lotno varchar(50) NOT NULL,
    opno varchar(50) NOT NULL,
    status numeric(1,0) DEFAULT 0 NOT NULL,
    mono varchar(50) NOT NULL,
    product varchar(50) NOT NULL,
    productname varchar(50) NOT NULL,
    version varchar(50) NOT NULL,
    reason varchar(100) NOT NULL,
    release varchar(100),
    creator varchar(50) NOT NULL,
    creatdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblspcwaive PRIMARY KEY (lotno, opno)
);

-- Oracle source table: XX_TBLWIPLOTMAT_EXCLUDE_BU1
CREATE TABLE IF NOT EXISTS ymes.xx_tblwiplotmat_exclude_bu1 (
    lotno varchar(25) NOT NULL,
    opno varchar(7) NOT NULL,
    materialno varchar(40) NOT NULL,
    createdate timestamp without time zone,
    createuser varchar(20),
    wono varchar(12) NOT NULL,
    stype varchar(20) NOT NULL,
    CONSTRAINT xx_tblwiplotmat_exclude_bu1_pk PRIMARY KEY (lotno, opno, materialno, stype)
);

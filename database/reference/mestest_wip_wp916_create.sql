-- WP916 CREATE-only from MESTEST dictionary for previously uncaptured CloseLot dependencies.
-- Verify legacy backup-table activation, defaults, BYTE limits and live compatibility before use.
-- NUMBER with unspecified precision is represented as numeric.
CREATE TABLE IF NOT EXISTS ymes.bktblemsequipmentstate (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    starttime timestamp(0) without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblemsequipmentstate_01 ON ymes.bktblemsequipmentstate (lotserial);
CREATE INDEX IF NOT EXISTS i_bktblemsequipmentstate_02 ON ymes.bktblemsequipmentstate (eqpserialno);
CREATE INDEX IF NOT EXISTS i_bktblemsequipmentstate_03 ON ymes.bktblemsequipmentstate (equipmentno);

CREATE TABLE IF NOT EXISTS ymes.bktbloemobasis (
    mono varchar(50) NOT NULL,
    rono varchar(25) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    mostate numeric(2,0) NOT NULL,
    moqty numeric(12,4) NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5),
    priority numeric(2,0) NOT NULL,
    planfinishdate timestamp(0) without time zone,
    belongtomono varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    issuestate numeric(1,0) NOT NULL,
    unreleaselotqty numeric(12,4) NOT NULL,
    mounitno varchar(30) NOT NULL,
    lotserial varchar(55),
    motypeno numeric(2,0) NOT NULL,
    mosource numeric(2,0),
    engno varchar(30) NOT NULL,
    engversion varchar(3) NOT NULL,
    customerno varchar(50),
    incomingkey varchar(55),
    customerlotno varchar(50),
    componentfrominv numeric(1,0),
    factoryno varchar(20) NOT NULL,
    returnno varchar(20),
    orgmostate numeric(2,0),
    moclosedate timestamp(0) without time zone,
    planstartdate timestamp(0) without time zone,
    actualstartdate timestamp(0) without time zone,
    actualfinishdate timestamp(0) without time zone,
    erpmolineno varchar(50),
    moclass varchar(20),
    stockinlotno varchar(50),
    s_wafersource numeric(1,0),
    mofinishqty numeric(12,4),
    mofinishunitno varchar(30),
    moprocesstype varchar(10),
    sap_closestatus varchar(1),
    sap_fiskn varchar(10),
    sap_fistxt varchar(30),
    sap_kdmat varchar(35),
    sap_zkdmat_ii varchar(100),
    sap_plnal varchar(2),
    sap_dispo varchar(4),
    sap_erdat varchar(8),
    sap_ftrmi varchar(8),
    sap_paenr varchar(12),
    sap_saenr varchar(12),
    sap_getdat timestamp(0) without time zone,
    xx_assign_bin varchar(20),
    sap_assign_bin varchar(20)
);
CREATE INDEX IF NOT EXISTS i_bktbloemobasis_01 ON ymes.bktbloemobasis (lotserial);
CREATE INDEX IF NOT EXISTS i_bktbloemobasis_02 ON ymes.bktbloemobasis (mono);

CREATE TABLE IF NOT EXISTS ymes.bktbloerobasis (
    rono varchar(25) NOT NULL,
    customerno varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    issuestate numeric(1,0) NOT NULL,
    lotserial varchar(55),
    rostate numeric(2,0) NOT NULL,
    duedate timestamp(0) without time zone
);
CREATE INDEX IF NOT EXISTS i_bktbloerobasis_01 ON ymes.bktbloerobasis (lotserial);
CREATE INDEX IF NOT EXISTS i_bktbloerobasis_02 ON ymes.bktbloerobasis (rono);

CREATE TABLE IF NOT EXISTS ymes.bktbloerodetail (
    rono varchar(25) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    productno varchar(50) NOT NULL,
    orderqty numeric(12,4) NOT NULL,
    unreleaseqty numeric(12,4) NOT NULL,
    state numeric(2,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    rounitno varchar(30) NOT NULL,
    lotserial varchar(55),
    customerno varchar(50),
    ordertype varchar(10),
    shipqty numeric(12,4) NOT NULL,
    priority numeric(2,0) NOT NULL,
    bookingflag numeric(1,0) NOT NULL,
    shippingflag numeric(1,0) NOT NULL,
    unitprice numeric(8,2),
    unitmeasure varchar(10),
    currency varchar(10),
    orderdate timestamp(0) without time zone,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    customername varchar(100),
    CONSTRAINT pk_bktbloerodetail PRIMARY KEY (rono, itemno)
);
CREATE INDEX IF NOT EXISTS i_bktbloerodetail_01 ON ymes.bktbloerodetail (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipabnormalstate (
    lotno varchar(50) NOT NULL,
    abnormalno varchar(20) NOT NULL,
    position varchar(50) NOT NULL,
    linkname varchar(20),
    priority numeric(2,0),
    lotserial varchar(55),
    abnormalqty numeric(6,0) NOT NULL
);
CREATE INDEX IF NOT EXISTS i_bktblwipabnormalstate_01 ON ymes.bktblwipabnormalstate (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipabnormalstate_02 ON ymes.bktblwipabnormalstate (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipautomerge (
    fromlotno varchar(50) NOT NULL,
    tolotno varchar(50) NOT NULL,
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwipautomerge_01 ON ymes.bktblwipautomerge (fromlotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipautomerge_02 ON ymes.bktblwipautomerge (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipcomponentproperty (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwipcomponentproperty_01 ON ymes.bktblwipcomponentproperty (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipcomponentproperty_02 ON ymes.bktblwipcomponentproperty (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipcomponentstate (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    unitno varchar(30),
    executestate numeric(1,0),
    status numeric(1,0) NOT NULL,
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
    s_waferfeedbackstatus varchar(2),
    s_wafersize varchar(100),
    s_formbatchseqno numeric(3,0),
    s_glotseqno numeric(3,0),
    s_glotid varchar(50),
    s_glotserialno varchar(50),
    s_batchseqno numeric(3,0),
    s_rungroupid varchar(50),
    s_rungroupseqno numeric(3,0),
    s_rungroupserialno varchar(50),
    g_lasermark varchar(50)
);
CREATE INDEX IF NOT EXISTS i_bktblwipcomponentstate_01 ON ymes.bktblwipcomponentstate (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipcomponentstate_02 ON ymes.bktblwipcomponentstate (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipfuturehold (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    mclassno varchar(30),
    holddescription varchar(4000),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    lotserial varchar(55),
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    futureholdbatchno varchar(20),
    futureholdserial varchar(20),
    lockflag numeric(1,0)
);
CREATE INDEX IF NOT EXISTS i_bktblwipfuturehold_01 ON ymes.bktblwipfuturehold (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipfuturehold_02 ON ymes.bktblwipfuturehold (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipfutureholdreason (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    holdtime numeric(1,0) NOT NULL,
    holditemno varchar(50) NOT NULL,
    lotserial varchar(55),
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    futureholdserial varchar(20)
);
CREATE INDEX IF NOT EXISTS i_bktblwipfutureholdreason_01 ON ymes.bktblwipfutureholdreason (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipfutureholdreason_02 ON ymes.bktblwipfutureholdreason (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipfutureos (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    description varchar(250),
    moduleno varchar(45) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    lotserial varchar(55),
    osno varchar(20) NOT NULL
);
CREATE INDEX IF NOT EXISTS i_bktblwipfutureos_01 ON ymes.bktblwipfutureos (lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipfutureos_02 ON ymes.bktblwipfutureos (lotno);

CREATE TABLE IF NOT EXISTS ymes.bktblwipfuturewait (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    waittime numeric(1,0) NOT NULL,
    description varchar(100),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwipfuturewait_01 ON ymes.bktblwipfuturewait (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipfuturewait_02 ON ymes.bktblwipfuturewait (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwipgoodbinstate (
    lotno varchar(50) NOT NULL,
    lotserial varchar(55),
    bin1 numeric(6,0),
    bin2 numeric(6,0),
    bin3 numeric(6,0),
    bin4 numeric(6,0),
    bin5 numeric(6,0),
    bin6 numeric(6,0),
    bin7 numeric(6,0),
    bin8 numeric(6,0)
);
CREATE INDEX IF NOT EXISTS i_bktblwipgoodbinstate_01 ON ymes.bktblwipgoodbinstate (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipgoodbinstate_02 ON ymes.bktblwipgoodbinstate (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotbasis (
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
    priority numeric(2,0),
    hotlot numeric(2,0),
    wipdate timestamp(0) without time zone,
    description varchar(400),
    createdate timestamp(0) without time zone,
    creator varchar(10),
    lotserial varchar(55),
    engno varchar(30) NOT NULL,
    engversion varchar(3) NOT NULL,
    deviceno varchar(50),
    ex_lotbasis1 varchar(20),
    ex_lotbasis2 varchar(20),
    ex_lotbasis3 varchar(20),
    planfinishdate timestamp(0) without time zone,
    returnno varchar(20),
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
    xx_marking varchar(1024)
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotbasis_01 ON ymes.bktblwiplotbasis (lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiplotbasis_02 ON ymes.bktblwiplotbasis (baselotno);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotecnattrib (
    lotno varchar(50) NOT NULL,
    ecnno varchar(50) NOT NULL,
    attribno varchar(40) NOT NULL,
    attribvalue varchar(255) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    opno varchar(20) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    activestartdate timestamp(0) without time zone,
    activeenddate timestamp(0) without time zone
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotecnattrib_01 ON ymes.bktblwiplotecnattrib (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiplotecnattrib_02 ON ymes.bktblwiplotecnattrib (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotoparea (
    lotno varchar(50) NOT NULL,
    nodeid varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotoparea_01 ON ymes.bktblwiplotoparea (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiplotoparea_02 ON ymes.bktblwiplotoparea (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotprocess (
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
    processversion varchar(3) NOT NULL,
    nodeversion varchar(3) NOT NULL,
    startnodeid varchar(50) NOT NULL
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotprocess_01 ON ymes.bktblwiplotprocess (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiplotprocess_02 ON ymes.bktblwiplotprocess (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotproperty (
    lotno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotproperty_01 ON ymes.bktblwiplotproperty (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiplotproperty_02 ON ymes.bktblwiplotproperty (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiplotstate (
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
    phaseno numeric(2,0) NOT NULL,
    havecomponent numeric(1,0) NOT NULL,
    havelevel numeric(1,0) NOT NULL,
    psorder numeric(2,0) NOT NULL,
    linkname varchar(20),
    reverseid numeric(6,0),
    eventtime timestamp(0) without time zone,
    lotstamp numeric(11,0) NOT NULL,
    gostatus numeric(2,0),
    processversion varchar(3) NOT NULL,
    nodeversion varchar(3) NOT NULL,
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    opreference numeric(1,0) NOT NULL,
    loggroupserial varchar(50),
    preopendtime timestamp(0) without time zone,
    arrivetime timestamp(0) without time zone,
    opstatus numeric(1,0),
    engno varchar(30) NOT NULL,
    engversion varchar(3) NOT NULL,
    recipeno varchar(300) NOT NULL,
    recipeversion varchar(15) NOT NULL,
    recipesql varchar(400) NOT NULL,
    batchserial varchar(50),
    moduleserial varchar(50) NOT NULL,
    modulenodeid varchar(50) NOT NULL,
    modulestageno varchar(50) NOT NULL,
    modulesequence numeric(4,0) NOT NULL,
    mainprocessno varchar(30),
    mainprocessversion varchar(3),
    eventuserno varchar(10),
    eventdescription varchar(255),
    ex_lotstate1 varchar(20),
    ex_lotstate2 varchar(20),
    ex_lotstate3 varchar(20),
    factoryno varchar(20) NOT NULL,
    opgroupno varchar(20),
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
    xx_scrapstate varchar(50) NOT NULL,
    xx_barcode varchar(20),
    xx_dmr_flag varchar(1),
    xx_datecode varchar(10),
    xx_assign_bin varchar(20),
    xx_tracecode varchar(30)
);
CREATE INDEX IF NOT EXISTS i_bktblwiplotstate_01 ON ymes.bktblwiplotstate (lotno, reverseid);

CREATE TABLE IF NOT EXISTS ymes.bktblwipopgroupstate (
    lotno varchar(50) NOT NULL,
    groupno varchar(30) NOT NULL,
    opno varchar(20) NOT NULL,
    nodeid varchar(50) NOT NULL,
    grouptype numeric(1,0) NOT NULL,
    executeflag numeric(1,0) NOT NULL,
    groupnodeid varchar(50) NOT NULL,
    lotserial varchar(55)
);
CREATE INDEX IF NOT EXISTS i_bktblwipopgroupstate_01 ON ymes.bktblwipopgroupstate (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwipopgroupstate_02 ON ymes.bktblwipopgroupstate (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_attrib (
    lotno varchar(50) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    attribno varchar(40) NOT NULL,
    attribvalue varchar(3000),
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
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_attrib_01 ON ymes.bktblwiptemp_attrib (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_attrib_02 ON ymes.bktblwiptemp_attrib (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_componentattrib (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    attribno varchar(40) NOT NULL,
    attribvalue varchar(255),
    attribsource numeric(1,0) NOT NULL,
    attribphase numeric(2,0) NOT NULL,
    attribsequence numeric(2,0) NOT NULL,
    savetolotpropertyno varchar(20),
    lotserial varchar(55),
    opno varchar(20),
    nodeid varchar(50)
);
CREATE INDEX IF NOT EXISTS i_wiptemp_componentattrib_01 ON ymes.bktblwiptemp_componentattrib (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_wiptemp_componentattrib_02 ON ymes.bktblwiptemp_componentattrib (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_equipment (
    lotno varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50),
    recipegroup varchar(50),
    recipeversion numeric(2,0),
    lotserial varchar(55),
    equipmentclass varchar(50),
    loadport numeric(2,0),
    loggroupserial varchar(50),
    inputqty numeric(12,4),
    opno varchar(20),
    nodeid varchar(50),
    planfinishtime timestamp(0) without time zone,
    s_batchid varchar(150)
);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_equipment_01 ON ymes.bktblwiptemp_equipment (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_equipment_02 ON ymes.bktblwiptemp_equipment (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_lotstate (
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
    moduleno varchar(50) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    opreference numeric(1,0) NOT NULL,
    nodeid varchar(50)
);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_lotstate_01 ON ymes.bktblwiptemp_lotstate (lotno, reverseid);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_material (
    lotno varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    stdqty numeric(14,6) NOT NULL,
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
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_material_01 ON ymes.bktblwiptemp_material (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_material_02 ON ymes.bktblwiptemp_material (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_qcitem (
    lotno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    executeflag numeric(1,0) NOT NULL,
    executeorder numeric(2,0),
    reinspwith2ndinspplan numeric(1,0),
    opno varchar(20),
    nodeid varchar(50),
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    executeflag_fai numeric(1,0),
    executeflag_ipqc numeric(1,0)
);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_qcitem_01 ON ymes.bktblwiptemp_qcitem (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_qcitem_02 ON ymes.bktblwiptemp_qcitem (lotserial);

CREATE TABLE IF NOT EXISTS ymes.bktblwiptemp_samplebin (
    lotno varchar(50),
    lotserial varchar(55),
    bin1 numeric(6,0),
    bin2 numeric(6,0),
    bin3 numeric(6,0),
    bin4 numeric(6,0),
    bin5 numeric(6,0),
    bin6 numeric(6,0),
    bin7 numeric(6,0),
    bin8 numeric(6,0),
    opno varchar(20),
    nodeid varchar(50),
    loggroupserial varchar(50)
);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_samplebin_01 ON ymes.bktblwiptemp_samplebin (lotno, lotserial);
CREATE INDEX IF NOT EXISTS i_bktblwiptemp_samplebin_02 ON ymes.bktblwiptemp_samplebin (lotserial);

CREATE TABLE IF NOT EXISTS ymes.tblemsdispatchcutinlot (
    equipmentno varchar(50) NOT NULL,
    lotno varchar(50) NOT NULL,
    cutinorder numeric(3,0) NOT NULL,
    CONSTRAINT pk_tblemsdispatchcutinlot PRIMARY KEY (equipmentno, lotno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipabnormalstate (
    lotno varchar(50) NOT NULL,
    abnormalno varchar(20) NOT NULL,
    position varchar(50) NOT NULL,
    linkname varchar(20),
    priority numeric(2,0),
    lotserial varchar(55),
    abnormalqty numeric(6,0) NOT NULL,
    CONSTRAINT pk_tblwipabnormalstate PRIMARY KEY (lotno, abnormalno, position)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipsmstatelog (
    lotno varchar(50) NOT NULL,
    fromlotno varchar(50) NOT NULL,
    frombaselotno varchar(50) NOT NULL,
    fromlotserial varchar(55) NOT NULL,
    fromlotqty numeric(12,4) NOT NULL,
    fromscrapqty numeric(12,4) NOT NULL,
    fromotherqty numeric(12,4) NOT NULL,
    tolotno varchar(50) NOT NULL,
    tobaselotno varchar(50) NOT NULL,
    tolotserial varchar(55) NOT NULL,
    tolotqty numeric(12,4) NOT NULL,
    toscrapqty numeric(12,4) NOT NULL,
    tootherqty numeric(12,4) NOT NULL,
    reflotno varchar(50) NOT NULL,
    majorlot numeric(1,0) NOT NULL,
    smtype numeric(2,0) NOT NULL,
    eventtime timestamp(0) without time zone NOT NULL,
    erpflag numeric(1,0) NOT NULL
);
CREATE INDEX IF NOT EXISTS i_tblwipsmstatelog_01 ON ymes.tblwipsmstatelog (lotno);

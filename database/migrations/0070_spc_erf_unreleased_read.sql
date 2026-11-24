-- Empty MES-source structures needed by the SC401 read-only query.
-- Reference DDL: mestest_wip_wp501_create.sql, mestest_wip_wp101_create.sql,
-- mestest_mtl_material_vendor_create.sql, and QC409 source DDL for BatchLot.
-- This migration never imports MES business rows and grants ymes_app SELECT only.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing SC401 read migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing SC401 read migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing SC401 read migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblwiperfbasis','tblwiperfbatchlot','tblwiplotbasis','tblwiplotstate','tblmtlmaterialvendor'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;

    IF to_regclass('"YMES".tblprdproductbasis') IS NULL
       OR to_regclass('"YMES".tblopbasis') IS NULL
       OR to_regclass('"YMES".tblmtlmaterialbasis') IS NULL
       OR to_regclass('"YMES".tblentcustomerbasis') IS NULL THEN
        RAISE EXCEPTION 'An existing SC401 product, OP, material, or customer lookup is missing';
    END IF;
    IF NOT has_table_privilege('ymes_app','"YMES".tblprdproductbasis','SELECT')
       OR NOT has_table_privilege('ymes_app','"YMES".tblopbasis','SELECT')
       OR NOT has_table_privilege('ymes_app','"YMES".tblmtlmaterialbasis','SELECT')
       OR NOT has_table_privilege('ymes_app','"YMES".tblentcustomerbasis','SELECT') THEN
        RAISE EXCEPTION 'ymes_app lacks SELECT on an existing SC401 lookup table';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwiperfbasis (
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
CREATE INDEX i_tblwiperfbasis_01 ON "YMES".tblwiperfbasis (lotserial);
COMMENT ON TABLE "YMES".tblwiperfbasis IS 'MES Oracle source: MESTEST.TBLWIPERFBASIS';

CREATE TABLE "YMES".tblwiperfbatchlot (
    erfno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    reflotflag numeric(1,0) NOT NULL,
    loggroupserial varchar(50),
    CONSTRAINT pk_tblwiperfbatchlot PRIMARY KEY (erfno,lotno,opno)
);
COMMENT ON TABLE "YMES".tblwiperfbatchlot IS 'MES Oracle source: MESTEST.TBLWIPERFBATCHLOT';

-- These two tables follow the existing WP101 PostgreSQL source references.
CREATE TABLE "YMES".tblwiplotbasis (
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
CREATE INDEX i_tblwiplotbasis_01 ON "YMES".tblwiplotbasis (mono);
COMMENT ON TABLE "YMES".tblwiplotbasis IS 'MES Oracle source: MESTEST.TBLWIPLOTBASIS';

CREATE TABLE "YMES".tblwiplotstate (
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
    modulesequence numeric(4,0) DEFAULT 0 NOT NULL,
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
    CONSTRAINT pk_tblwiplotstate PRIMARY KEY (lotno,opno,status)
);
CREATE INDEX i_tblwiplotstate_01 ON "YMES".tblwiplotstate (baselotno);
CREATE INDEX i_tblwiplotstate_02 ON "YMES".tblwiplotstate (psno);
CREATE INDEX i_tblwiplotstate_03 ON "YMES".tblwiplotstate (areano);
CREATE INDEX i_tblwiplotstate_04 ON "YMES".tblwiplotstate (opno);
CREATE INDEX i_tblwiplotstate_05 ON "YMES".tblwiplotstate (status);
CREATE INDEX i_tblwiplotstate_06 ON "YMES".tblwiplotstate (batchserial);
COMMENT ON TABLE "YMES".tblwiplotstate IS 'MES Oracle source: MESTEST.TBLWIPLOTSTATE';

CREATE TABLE "YMES".tblmtlmaterialvendor (
    vendorno varchar(20) NOT NULL,
    vendorname varchar(220) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialvendor PRIMARY KEY (vendorno)
);
COMMENT ON TABLE "YMES".tblmtlmaterialvendor IS 'MES Oracle source: MESTEST.TBLMTLMATERIALVENDOR';

GRANT SELECT ON TABLE
    "YMES".tblwiperfbasis,
    "YMES".tblwiperfbatchlot,
    "YMES".tblwiplotbasis,
    "YMES".tblwiplotstate,
    "YMES".tblmtlmaterialvendor
TO ymes_app;

COMMIT;

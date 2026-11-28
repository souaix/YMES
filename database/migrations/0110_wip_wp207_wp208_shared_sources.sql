-- WP207/WP208: create only the nine MESTEST source tables proven absent from YMES.
-- Translated from WP101/WP201/PRD source references; no business rows are copied.
-- Temporary/attribute tables intentionally keep the source's lack of PKs/indexes.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP207-WP208:0110', 0));

DO $guard$
DECLARE object_name text;
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0110 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0110';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0110';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblprdopattrib', 'tblwipcomponentproperty', 'tblwipcomponentstate',
        'tblwipcont_attrib', 'tblwipcont_componentattrib', 'tblwiplotproperty',
        'tblwiptemp_attrib', 'tblwiptemp_componentattrib', 'tblwiptemp_lotstate'
    ] LOOP
        IF to_regclass(format('%I.%I', 'YMES', object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare live structure before 0110', object_name;
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE lower(c.relname) = object_name
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; inspect before 0110', object_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE c.relkind = 'i' AND lower(c.relname) = ANY (ARRAY[
            'pk_tblprdopattrib', 'pk_tblwipcomponentproperty', 'pk_tblwipcomponentstate',
            'i_tblwipcomponentstate_01', 'i_tblwipcomponentstate_02', 'pk_tblwiplotproperty'
        ])
    ) THEN
        RAISE EXCEPTION 'A 0110 source index name already exists; inspect before migration';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblprdopattrib (
    serialno varchar(50) NOT NULL,
    attribtype numeric(1,0) NOT NULL,
    attribno varchar(20) NOT NULL,
    attribsource numeric(1,0) NOT NULL,
    attribphase numeric(2,0) NOT NULL,
    attribsequence numeric(2,0) NOT NULL,
    attribvalue varchar(3000),
    attribscript varchar(3000),
    savetolotpropertyno varchar(20),
    qcitemno varchar(25),
    ecnno varchar(20) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    allownull numeric(1,0),
    sequence numeric(2,0),
    CONSTRAINT pk_tblprdopattrib PRIMARY KEY (serialno, attribno, ecnno, equipmenttype)
);
COMMENT ON TABLE "YMES".tblprdopattrib IS 'MES Oracle source: MESTEST.TBLPRDOPATTRIB';

CREATE TABLE "YMES".tblwipcomponentproperty (
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblwipcomponentproperty PRIMARY KEY (lotno, componentno, propertyno)
);
COMMENT ON TABLE "YMES".tblwipcomponentproperty IS 'MES Oracle source: MESTEST.TBLWIPCOMPONENTPROPERTY';

CREATE TABLE "YMES".tblwipcomponentstate (
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
CREATE INDEX i_tblwipcomponentstate_01 ON "YMES".tblwipcomponentstate (s_glotid);
CREATE INDEX i_tblwipcomponentstate_02 ON "YMES".tblwipcomponentstate (s_cassetteno);
COMMENT ON TABLE "YMES".tblwipcomponentstate IS 'MES Oracle source: MESTEST.TBLWIPCOMPONENTSTATE';

CREATE TABLE "YMES".tblwipcont_attrib (
    lotserial varchar(55),
    attribno varchar(20),
    attribvalue varchar(3000),
    loggroupserial varchar(50),
    eventtime timestamp without time zone,
    equipmenttype varchar(50)
);
COMMENT ON TABLE "YMES".tblwipcont_attrib IS 'MES Oracle source: MESTEST.TBLWIPCONT_ATTRIB';

CREATE TABLE "YMES".tblwipcont_componentattrib (
    lotserial varchar(55),
    componentno varchar(50),
    attribno varchar(20),
    attribvalue varchar(255),
    loggroupserial varchar(50),
    eventtime timestamp without time zone
);
COMMENT ON TABLE "YMES".tblwipcont_componentattrib IS 'MES Oracle source: MESTEST.TBLWIPCONT_COMPONENTATTRIB';

CREATE TABLE "YMES".tblwiplotproperty (
    lotno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblwiplotproperty PRIMARY KEY (lotno, propertyno)
);
COMMENT ON TABLE "YMES".tblwiplotproperty IS 'MES Oracle source: MESTEST.TBLWIPLOTPROPERTY';

CREATE TABLE "YMES".tblwiptemp_attrib (
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
COMMENT ON TABLE "YMES".tblwiptemp_attrib IS 'MES Oracle source: MESTEST.TBLWIPTEMP_ATTRIB';

CREATE TABLE "YMES".tblwiptemp_componentattrib (
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
COMMENT ON TABLE "YMES".tblwiptemp_componentattrib IS 'MES Oracle source: MESTEST.TBLWIPTEMP_COMPONENTATTRIB';

CREATE TABLE "YMES".tblwiptemp_lotstate (
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
COMMENT ON TABLE "YMES".tblwiptemp_lotstate IS 'MES Oracle source: MESTEST.TBLWIPTEMP_LOTSTATE';

REVOKE ALL ON "YMES".tblprdopattrib,
    "YMES".tblwipcomponentproperty,
    "YMES".tblwipcomponentstate,
    "YMES".tblwipcont_attrib,
    "YMES".tblwipcont_componentattrib,
    "YMES".tblwiplotproperty,
    "YMES".tblwiptemp_attrib,
    "YMES".tblwiptemp_componentattrib,
    "YMES".tblwiptemp_lotstate FROM PUBLIC;

COMMIT;

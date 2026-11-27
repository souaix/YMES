BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; stop before RT607 migration 0094';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before RT607 migration 0094';
    END IF;
    IF to_regclass('"YMES".tblwiplotprocess') IS NOT NULL
       OR to_regclass('"YMES".tblprsnoderelation') IS NOT NULL
       OR to_regclass('"YMES".tblprdopdescription') IS NOT NULL
       OR to_regclass('"YMES".tblprdruntimesetup') IS NOT NULL
       OR to_regclass('"YMES".tblprdproductprocess') IS NOT NULL
       OR to_regclass('"YMES".tblprdoparea') IS NOT NULL
       OR to_regclass('"YMES".tblwiplotoparea') IS NOT NULL THEN
        RAISE EXCEPTION 'An RT607 dependency table already exists; compare it with the verified source DDL before migration 0094';
    END IF;
END
$$;

-- Empty PostgreSQL replicas of verified MESTEST tables required to expand an RT607 run card.
-- No source business rows, foreign keys, or indexes beyond Oracle primary keys are copied.
CREATE TABLE "YMES".tblwiplotprocess (
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
COMMENT ON TABLE "YMES".tblwiplotprocess IS 'MES Oracle source: MESTEST.TBLWIPLOTPROCESS';

CREATE TABLE "YMES".tblprsnoderelation (
    fromnodeid varchar(50) NOT NULL,
    tonodeid varchar(50) NOT NULL,
    linkname varchar(30) NOT NULL,
    fromnodeno varchar(50),
    tonodeno varchar(50),
    processno varchar(30),
    processversion varchar(3),
    CONSTRAINT pk_tblprsnoderelation PRIMARY KEY (fromnodeid, tonodeid, linkname)
);
COMMENT ON TABLE "YMES".tblprsnoderelation IS 'MES Oracle source: MESTEST.TBLPRSNODERELATION';

CREATE TABLE "YMES".tblprdopdescription (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdopdescription PRIMARY KEY (productno, productversion, opno)
);
COMMENT ON TABLE "YMES".tblprdopdescription IS 'Oracle source MESTEST.TBLPRDOPDESCRIPTION';

CREATE TABLE "YMES".tblprdruntimesetup (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    areano varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    equipmentno varchar(50),
    equipmenttype varchar(50),
    stdunitemptime numeric(6,2) NOT NULL DEFAULT 0,
    stduniteqptime numeric(6,2) NOT NULL DEFAULT 0,
    counteqpunitqty numeric(6,0) NOT NULL DEFAULT 1,
    stdunitruntime numeric(6,2) NOT NULL DEFAULT 0,
    countopunitqty numeric(6,0) NOT NULL DEFAULT 1,
    stdqueuetime numeric(6,2) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    fixemptime numeric(6,2) DEFAULT 0,
    varemptime numeric(6,2) DEFAULT 0,
    fixeqptime numeric(6,2) DEFAULT 0,
    vareqptime numeric(6,2) DEFAULT 0,
    workpricetype numeric(1,0) DEFAULT 1,
    workprice numeric(6,2)
);
COMMENT ON TABLE "YMES".tblprdruntimesetup IS 'Oracle source MESTEST.TBLPRDRUNTIMESETUP; source reports no PK, unique key, or index';

CREATE TABLE "YMES".tblprdproductprocess (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    psno varchar(50) NOT NULL,
    psorder numeric(2,0) NOT NULL,
    processno varchar(30) NOT NULL,
    defaultprocess numeric(1,0) DEFAULT 0,
    havecomponent numeric(1,0) NOT NULL,
    havelevel numeric(1,0) NOT NULL,
    motypeno numeric(2,0),
    processversion varchar(3) NOT NULL,
    equipmentgroup varchar(50),
    apsfixeqptime numeric(8,0),
    apsvareqptime numeric(8,0),
    stdworktimeqty numeric(8,0),
    transferqty numeric(8,0),
    lotreartime numeric(8,0),
    CONSTRAINT pk_tblprdproductprocess PRIMARY KEY (productno, productversion, motypeno, processno, processversion)
);
COMMENT ON TABLE "YMES".tblprdproductprocess IS 'Oracle source MESTEST.TBLPRDPRODUCTPROCESS';

CREATE TABLE "YMES".tblprdoparea (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    defaultarea numeric(1,0),
    CONSTRAINT pk_tblprdoparea PRIMARY KEY (productno, productversion, opno, areano)
);
COMMENT ON TABLE "YMES".tblprdoparea IS 'Oracle source MESTEST.TBLPRDOPAREA';

CREATE TABLE "YMES".tblwiplotoparea (
    lotno varchar(50) NOT NULL,
    nodeid varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    lotserial varchar(55),
    CONSTRAINT pk_tblwiplotoparea PRIMARY KEY (lotno, nodeid)
);
COMMENT ON TABLE "YMES".tblwiplotoparea IS 'Oracle source MESTEST.TBLWIPLOTOPAREA';

GRANT SELECT ON "YMES".tblwiplotprocess,
                "YMES".tblprsnoderelation,
                "YMES".tblprdopdescription,
                "YMES".tblprdruntimesetup,
                "YMES".tblprdproductprocess,
                "YMES".tblprdoparea,
                "YMES".tblwiplotoparea TO ymes_app;

COMMIT;

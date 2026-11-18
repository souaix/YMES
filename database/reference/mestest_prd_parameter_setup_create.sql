-- CREATE-only PostgreSQL reference translated from Oracle MESTEST dictionary metadata.
-- Compare YMES live schema and migrations before any migration. No sample rows are included.
CREATE SCHEMA IF NOT EXISTS "YMES";

-- Oracle source table: MESTEST.TBLPRDOP
CREATE TABLE IF NOT EXISTS "YMES"."tblprdop" (
    "serialno" varchar(50) NOT NULL,
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "nodeid" varchar(50),
    "opscript" varchar(3000),
    "psno" varchar(50) NOT NULL,
    "oporder" numeric(6,0) NOT NULL,
    CONSTRAINT "pk_tblprdop" PRIMARY KEY ("serialno")
);
CREATE INDEX IF NOT EXISTS "i_tblprdop_01" ON "YMES"."tblprdop" ("productno", "productversion", "opno");
COMMENT ON TABLE "YMES"."tblprdop" IS 'Oracle source MESTEST.TBLPRDOP';

-- Oracle source table: MESTEST.TBLPRDOPATTRIB
CREATE TABLE IF NOT EXISTS "YMES"."tblprdopattrib" (
    "serialno" varchar(50) NOT NULL,
    "attribtype" numeric(1,0) NOT NULL,
    "attribno" varchar(20) NOT NULL,
    "attribsource" numeric(1,0) NOT NULL,
    "attribphase" numeric(2,0) NOT NULL,
    "attribsequence" numeric(2,0) NOT NULL,
    "attribvalue" varchar(3000),
    "attribscript" varchar(3000),
    "savetolotpropertyno" varchar(20),
    "qcitemno" varchar(25),
    "ecnno" varchar(20) NOT NULL,
    "equipmenttype" varchar(50) NOT NULL,
    "allownull" numeric(1,0),
    "sequence" numeric(2,0),
    CONSTRAINT "pk_tblprdopattrib" PRIMARY KEY ("serialno", "attribno", "ecnno", "equipmenttype")
);
COMMENT ON TABLE "YMES"."tblprdopattrib" IS 'Oracle source MESTEST.TBLPRDOPATTRIB';

-- Oracle source table: MESTEST.TBLPRDOPRULE
CREATE TABLE IF NOT EXISTS "YMES"."tblprdoprule" (
    "serialno" varchar(50) NOT NULL,
    "ruletype" numeric(1,0) NOT NULL,
    "ruleno" varchar(20) NOT NULL,
    "rulesequence" numeric(2,0),
    "rulephase" numeric(2,0),
    "rulescript" varchar(3000),
    "ruleclass" numeric(1,0) NOT NULL,
    "rulemode" numeric(1,0),
    "rulemodeqty" numeric(2,0),
    CONSTRAINT "pk_tblprdoprule" PRIMARY KEY ("serialno", "ruleno")
);
COMMENT ON TABLE "YMES"."tblprdoprule" IS 'Oracle source MESTEST.TBLPRDOPRULE';

-- Oracle source table: MESTEST.TBLPRDOPEQPRECIPE
CREATE TABLE IF NOT EXISTS "YMES"."tblprdopeqprecipe" (
    "serialno" varchar(50) NOT NULL,
    "equipmentno" varchar(50) NOT NULL,
    "recipegroup" varchar(50)
);
CREATE INDEX IF NOT EXISTS "i_tblprdopeqprecipe_01" ON "YMES"."tblprdopeqprecipe" ("serialno", "equipmentno", "recipegroup");
COMMENT ON TABLE "YMES"."tblprdopeqprecipe" IS 'Oracle source MESTEST.TBLPRDOPEQPRECIPE';

-- Oracle source table: MESTEST.TBLPRDOPRULEXML_CLOB
CREATE TABLE IF NOT EXISTS "YMES"."tblprdoprulexml_clob" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "rulexmlstring" text,
    CONSTRAINT "pk_tblprdoprulexml_clob" PRIMARY KEY ("productno", "productversion", "opno")
);
COMMENT ON TABLE "YMES"."tblprdoprulexml_clob" IS 'Oracle source MESTEST.TBLPRDOPRULEXML_CLOB';

-- Oracle source table: MESTEST.TBLPRDOPBUSINESSRULE
CREATE TABLE IF NOT EXISTS "YMES"."tblprdopbusinessrule" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "rulexmlstring" text,
    CONSTRAINT "pk_tblprdopbusinessrule" PRIMARY KEY ("productno", "productversion", "opno")
);
COMMENT ON TABLE "YMES"."tblprdopbusinessrule" IS 'Oracle source MESTEST.TBLPRDOPBUSINESSRULE';

-- Oracle source table: MESTEST.TBLPRDOPBUSINESSRULERELATION
CREATE TABLE IF NOT EXISTS "YMES"."tblprdopbusinessrulerelation" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "fromnode" varchar(20) NOT NULL,
    "tonode" varchar(20) NOT NULL,
    "linkname" varchar(20) NOT NULL,
    "phaseno" numeric(2,0) NOT NULL,
    CONSTRAINT "pk_prdopbusinessrulerelation" PRIMARY KEY ("productno", "productversion", "opno", "fromnode", "tonode", "linkname")
);
COMMENT ON TABLE "YMES"."tblprdopbusinessrulerelation" IS 'Oracle source MESTEST.TBLPRDOPBUSINESSRULERELATION';

-- Oracle source table: MESTEST.TBLPRDOPAREA
CREATE TABLE IF NOT EXISTS "YMES"."tblprdoparea" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "areano" varchar(20) NOT NULL,
    "defaultarea" numeric(1,0),
    CONSTRAINT "pk_tblprdoparea" PRIMARY KEY ("productno", "productversion", "opno", "areano")
);
COMMENT ON TABLE "YMES"."tblprdoparea" IS 'Oracle source MESTEST.TBLPRDOPAREA';

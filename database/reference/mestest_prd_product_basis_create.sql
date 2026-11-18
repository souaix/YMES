-- CREATE-only PostgreSQL reference translated from Oracle MESTEST product master metadata.
-- Not a migration. Compare the live YMES schema and migration history before use.
-- ProductProperty Oracle DDL is reconstructed from dictionary metadata because DBMS_METADATA disconnected.
CREATE SCHEMA IF NOT EXISTS "YMES";

-- Oracle source table: MESTEST.TBLPRDPRODUCTBASIS
CREATE TABLE IF NOT EXISTS "YMES".tblprdproductbasis (
    "productversion" varchar(5) NOT NULL,
    "productname" varchar(300) NOT NULL,
    "producttype" varchar(50) NOT NULL,
    "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "description" varchar(1000),
    "curversion" numeric(1,0) DEFAULT 0 NOT NULL,
    "unitno" varchar(30),
    "unittype" varchar(30),
    "specno" varchar(120) DEFAULT 'N/A' NOT NULL,
    "cartonqty" numeric(8,0) DEFAULT 0 NOT NULL,
    "palletqty" numeric(8,0) DEFAULT 0 NOT NULL,
    "packoipath" varchar(500),
    "boxqty" numeric(8,4) DEFAULT 0,
    "productcode" varchar(10),
    "serialtypeno_lot" varchar(50),
    "serialtypeno_comp" varchar(50),
    "picturename" varchar(50),
    "lotstdqty" numeric(12,4) DEFAULT 0,
    "erpno" varchar(50),
    "user_defined01" varchar(255),
    "user_defined02" varchar(255),
    "user_defined03" varchar(255),
    "user_defined04" varchar(255),
    "user_defined05" varchar(255),
    "user_defined06" varchar(255),
    "user_defined07" varchar(255),
    "user_defined08" varchar(255),
    "user_defined09" varchar(255),
    "user_defined10" varchar(255),
    "user_defined11" numeric(23,8),
    "user_defined12" numeric(23,8),
    "user_defined13" numeric(23,8),
    "user_defined14" numeric(23,8),
    "user_defined15" numeric(23,8),
    "user_defined16" numeric(23,8),
    "user_defined17" numeric(23,8),
    "user_defined18" numeric(23,8),
    "user_defined19" numeric(23,8),
    "user_defined20" numeric(23,8),
    "user_defined21" timestamp without time zone,
    "user_defined22" timestamp without time zone,
    "user_defined23" timestamp without time zone,
    "user_defined24" timestamp without time zone,
    "user_defined25" timestamp without time zone,
    "user_defined26" timestamp without time zone,
    "user_defined27" timestamp without time zone,
    "user_defined28" timestamp without time zone,
    "user_defined29" timestamp without time zone,
    "user_defined30" timestamp without time zone,
    "user_defined31" varchar(255),
    "user_defined32" varchar(255),
    "user_defined33" numeric(23,8),
    "user_defined34" numeric(23,8),
    "graphno" varchar(255),
    "qccategory" varchar(50),
    "itemspec" varchar(255),
    "customerno" varchar(50) DEFAULT 'N/A',
    "deviceno" varchar(50) DEFAULT 'N/A',
    "ecncontrol" varchar(1),
    "i_customerno" varchar(50),
    "batchuserno" varchar(10),
    "batchecnlock" numeric(1,0) DEFAULT 0,
    "datastamp" numeric(11,0) DEFAULT 0,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    "sap_plnal" varchar(2),
    "sap_ktext" varchar(40),
    "sap_aennr" varchar(12),
    "sap_datuv" varchar(12),
    "sap_upload" varchar(1) DEFAULT 'N',
    "sap_uploaddate" timestamp without time zone,
    "sap_bomaennr" varchar(12),
    "xx_sopspec" varchar(50),
    "xx_soprev" varchar(40),
    "sopspec" varchar(50),
    "soprev" varchar(10),
    "sap_werks" varchar(10),
    "sap_ieupload" varchar(20),
    "sap_ieuploaddate" timestamp without time zone,
    "sap_ieuploaduser" varchar(10),
    "sap_sapupdatedatetime" timestamp without time zone,
    CONSTRAINT pk_tblprdproductbasis PRIMARY KEY ("productno", "productversion")
);
COMMENT ON TABLE "YMES".tblprdproductbasis IS 'Oracle source MESTEST.TBLPRDPRODUCTBASIS';

-- Oracle source table: MESTEST.TBLPRDPRODUCTPROCESS
CREATE TABLE IF NOT EXISTS "YMES".tblprdproductprocess (
    "productversion" varchar(5) NOT NULL,
    "psno" varchar(50) NOT NULL,
    "psorder" numeric(2,0) NOT NULL,
    "processno" varchar(30) NOT NULL,
    "defaultprocess" numeric(1,0) DEFAULT 0,
    "havecomponent" numeric(1,0) NOT NULL,
    "havelevel" numeric(1,0) NOT NULL,
    "motypeno" numeric(2,0),
    "processversion" varchar(3) NOT NULL,
    "equipmentgroup" varchar(50),
    "apsfixeqptime" numeric(8,0),
    "apsvareqptime" numeric(8,0),
    "stdworktimeqty" numeric(8,0),
    "transferqty" numeric(8,0),
    "lotreartime" numeric(8,0),
    CONSTRAINT pk_tblprdproductprocess PRIMARY KEY ("productno", "productversion", "motypeno", "processno", "processversion")
);
COMMENT ON TABLE "YMES".tblprdproductprocess IS 'Oracle source MESTEST.TBLPRDPRODUCTPROCESS';

-- Oracle source table: MESTEST.TBLPRDBOM
CREATE TABLE IF NOT EXISTS "YMES".tblprdbom (
    "productversion" varchar(5) NOT NULL,
    "nodeno" varchar(50) NOT NULL,
    "nodetype" numeric(1,0) NOT NULL,
    "stdqty" numeric(16,8) NOT NULL,
    "unitno" varchar(30) NOT NULL,
    "decreaserate" numeric(3,2) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "specified" numeric(1,0) DEFAULT 0 NOT NULL,
    "description" varchar(255),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "nodeversion" varchar(3) NOT NULL,
    "putinplacetype" numeric(1,0) NOT NULL,
    "xx_mat_group" varchar(50),
    "xx_sawversion" varchar(6),
    "mat_group" varchar(50),
    CONSTRAINT pk_tblprdbom PRIMARY KEY ("productno", "productversion", "nodeno", "opno")
);
COMMENT ON TABLE "YMES".tblprdbom IS 'Oracle source MESTEST.TBLPRDBOM';

-- Oracle source table: MESTEST.TBLPRDBOMMATERIALGROUP
CREATE TABLE IF NOT EXISTS "YMES".tblprdbommaterialgroup (
    "productversion" varchar(5) NOT NULL,
    "nodeno" varchar(50) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "materialno" varchar(50) NOT NULL,
    CONSTRAINT pk_tblprdbommaterialgroup PRIMARY KEY ("productno", "productversion", "nodeno", "opno", "materialno")
);
COMMENT ON TABLE "YMES".tblprdbommaterialgroup IS 'Oracle source MESTEST.TBLPRDBOMMATERIALGROUP';

-- Oracle source table: MESTEST.TBLPRDEQPCONSTRAINT
CREATE TABLE IF NOT EXISTS "YMES".tblprdeqpconstraint (
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(50) NOT NULL,
    "equipmentno" varchar(50) NOT NULL,
    "description" varchar(255),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    CONSTRAINT pk_tblprdeqpconstraint PRIMARY KEY ("productno", "productversion", "opno", "equipmentno")
);
COMMENT ON TABLE "YMES".tblprdeqpconstraint IS 'Oracle source MESTEST.TBLPRDEQPCONSTRAINT';

-- Oracle source table: MESTEST.TBLPRDPRODUCT_PICTURE
CREATE TABLE IF NOT EXISTS "YMES".tblprdproduct_picture (
    "picturebody" bytea NOT NULL,
    CONSTRAINT pk_tblprdproduct_picture PRIMARY KEY ("picturename")
);
COMMENT ON TABLE "YMES".tblprdproduct_picture IS 'Oracle source MESTEST.TBLPRDPRODUCT_PICTURE';

-- Oracle source table: MESTEST.TBLPRDPRODUCTPROPERTY
CREATE TABLE IF NOT EXISTS "YMES".tblprdproductproperty (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "propertyno" varchar(20) NOT NULL,
    "defaultvalue" varchar(255),
    "propertysequence" numeric(2,0),
    "description" varchar(255),
    "issuestate" numeric(1,0) NOT NULL,
    "datastamp" numeric(11,0),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT pk_tblprdproductproperty PRIMARY KEY ("productno", "productversion", "propertyno")
);
COMMENT ON TABLE "YMES".tblprdproductproperty IS 'Oracle source MESTEST.TBLPRDPRODUCTPROPERTY';

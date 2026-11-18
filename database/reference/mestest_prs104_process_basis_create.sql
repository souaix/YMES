-- PostgreSQL CREATE-only reference adapted from Oracle MESTEST PS104 metadata.
-- Not a migration or production deployment script. Compare YMES live schema and migrations first.
-- This reference covers the nine source tables read by PS104 list/edit/copy/delete/graph/property/APS paths.
-- Oracle reports PK unique indexes only; no foreign keys or additional indexes.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblprsprocessbasis" (
  "processno" varchar(30) NOT NULL,
  "processclass" numeric(2,0) NOT NULL,
  "psno" varchar(50) NOT NULL,
  "processtype" varchar(20) NOT NULL,
  "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "description" varchar(255),
  "nodexmlstring" text,
  "tuningprocess" numeric(1,0) DEFAULT 0,
  "processversion" varchar(3) NOT NULL,
  "curversion" numeric(1,0) DEFAULT 0 NOT NULL,
  "partialtype" numeric(1,0) DEFAULT 0,
  "datastamp" numeric(11,0) DEFAULT 0,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprsprocessbasis" PRIMARY KEY ("processno", "processversion")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsnodebasis" (
  "nodeid" varchar(50) NOT NULL,
  "nodeno" varchar(50) NOT NULL,
  "nodetype" numeric(1,0) DEFAULT 0 NOT NULL,
  "processno" varchar(30),
  "groupno" varchar(30),
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "description" varchar(50),
  "processversion" varchar(3),
  "nodeversion" varchar(3) DEFAULT 'N/A',
  "stageno" varchar(50),
  "sequence" numeric(4,0),
  "sap_vornr" varchar(4),
  "sap_steus" varchar(4),
  "sap_infnr" varchar(10),
  "xx_matrix_code" varchar(3000),
  "xx_sawversion" varchar(10),
  CONSTRAINT "pk_tblprsnodebasis" PRIMARY KEY ("nodeid")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsnoderelation" (
  "fromnodeid" varchar(50) NOT NULL,
  "tonodeid" varchar(50) NOT NULL,
  "linkname" varchar(30) NOT NULL,
  "fromnodeno" varchar(50),
  "tonodeno" varchar(50),
  "processno" varchar(30),
  "processversion" varchar(3),
  CONSTRAINT "pk_tblprsnoderelation" PRIMARY KEY ("fromnodeid", "tonodeid", "linkname")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsnodexml_clob" (
  "processno" varchar(40) NOT NULL,
  "processversion" varchar(3) NOT NULL,
  "nodexmlstring" text,
  CONSTRAINT "pk_tblprsnodexml_clob" PRIMARY KEY ("processno", "processversion")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsprocessproperty" (
  "processno" varchar(50) NOT NULL,
  "propertyno" varchar(20) NOT NULL,
  "defaultvalue" varchar(255),
  "propertysequence" numeric(2,0),
  "description" varchar(255),
  "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
  "processversion" varchar(3) NOT NULL,
  "datastamp" numeric(11,0) DEFAULT 0,
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprsprocessproperty" PRIMARY KEY ("processno", "processversion", "propertyno")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsopapsbase" (
  "processno" varchar(50) NOT NULL,
  "processversion" varchar(5) NOT NULL,
  "opno" varchar(20) NOT NULL,
  "nodeid" varchar(50) NOT NULL,
  "equipmentgroup" varchar(50) DEFAULT 'N/A' NOT NULL,
  "stdworktimeqty" numeric(8,0) NOT NULL,
  "apsfixeqptime" numeric(8,0) DEFAULT 0 NOT NULL,
  "apsvareqptime" numeric(8,0) DEFAULT 0 NOT NULL,
  "transferqty" numeric(8,0) DEFAULT 0 NOT NULL,
  "lotreartime" numeric(8,0) DEFAULT 0 NOT NULL,
  "schedule" numeric(1,0) NOT NULL,
  "subcontractorno" varchar(30) NOT NULL,
  "batchop" numeric(1,0) DEFAULT 0 NOT NULL,
  "batchmaxqty" numeric(9,0) DEFAULT 999999 NOT NULL,
  "description" varchar(255),
  "creator" varchar(10) NOT NULL,
  "createdate" timestamp without time zone NOT NULL,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprsopapsbase" PRIMARY KEY ("nodeid")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsopapsdetail" (
  "processno" varchar(50) NOT NULL,
  "processversion" varchar(5) NOT NULL,
  "opno" varchar(20) NOT NULL,
  "nodeid" varchar(50) NOT NULL,
  "equipmentno" varchar(50) NOT NULL,
  "stdworktimeqty" numeric(8,0) NOT NULL,
  "apsfixeqptime" numeric(8,0) DEFAULT 0 NOT NULL,
  "apsvareqptime" numeric(8,0) DEFAULT 0 NOT NULL,
  "schedule" numeric(1,0) NOT NULL,
  "batchop" numeric(1,0) DEFAULT 0 NOT NULL,
  "batchmaxqty" numeric(9,0) DEFAULT 999999 NOT NULL,
  "description" varchar(255),
  "creator" varchar(10) NOT NULL,
  "createdate" timestamp without time zone NOT NULL,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprsopapsdetail" PRIMARY KEY ("nodeid", "equipmentno")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblprsproductionsegment" (
  "psno" varchar(50) NOT NULL,
  "psname" varchar(50) NOT NULL,
  "psorder" numeric(2,0) DEFAULT 0 NOT NULL,
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "description" varchar(255),
  "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
  "havecomponent" numeric(1,0) DEFAULT 0 NOT NULL,
  "havelevel" numeric(1,0) DEFAULT 0 NOT NULL,
  "serialtypeno_lot" varchar(50),
  "serialtypeno_comp" varchar(50),
  "ispartial" numeric(1,0) DEFAULT 0 NOT NULL,
  "isnetflow" numeric(1,0) DEFAULT 0,
  "datastamp" numeric(11,0) DEFAULT 0,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprsproductionsegment" PRIMARY KEY ("psno")
);
-- Oracle also reports a redundant SYS_C0014330 CHECK enforcing ISPARTIAL NOT NULL; PostgreSQL NOT NULL has the same effect.

CREATE TABLE IF NOT EXISTS "YMES"."tblprstype" (
  "processtype" varchar(20) NOT NULL,
  "description" varchar(255),
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
  "datastamp" numeric(11,0) DEFAULT 0,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "pk_tblprstype" PRIMARY KEY ("processtype")
);

COMMENT ON TABLE "YMES"."tblprsprocessbasis" IS 'MES source MESTEST.TBLPRSPROCESSBASIS';
COMMENT ON COLUMN "YMES"."tblprsprocessbasis"."nodexmlstring" IS 'Legacy iMESXMLFlow XML snapshot; preserve raw payload until compatible serializer is available';
COMMENT ON TABLE "YMES"."tblprsnodebasis" IS 'MES source MESTEST.TBLPRSNODEBASIS';
COMMENT ON TABLE "YMES"."tblprsnoderelation" IS 'MES source MESTEST.TBLPRSNODERELATION';
COMMENT ON TABLE "YMES"."tblprsnodexml_clob" IS 'MES source MESTEST.TBLPRSNODEXML_CLOB';
COMMENT ON TABLE "YMES"."tblprsprocessproperty" IS 'MES source MESTEST.TBLPRSPROCESSPROPERTY';
COMMENT ON TABLE "YMES"."tblprsopapsbase" IS 'MES source MESTEST.TBLPRSOPAPSBASE';
COMMENT ON TABLE "YMES"."tblprsopapsdetail" IS 'MES source MESTEST.TBLPRSOPAPSDETAIL';
COMMENT ON TABLE "YMES"."tblprsproductionsegment" IS 'MES source MESTEST.TBLPRSPRODUCTIONSEGMENT';
COMMENT ON TABLE "YMES"."tblprstype" IS 'MES source MESTEST.TBLPRSTYPE';

-- No migration is included. Compare current YMES schema, extensions, and data before applying forward migrations.

-- PostgreSQL CREATE-only reference derived from the MESTEST Oracle source DDL.
-- Check YMES live schema and migrations first. Do not use this file to replace or rebuild an existing table.
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

-- PostgreSQL CREATE-only reference translated from Oracle MESTEST metadata.
-- Compare YMES live schema and migrations before adding an application migration.
-- Empty table only; no sample data and no Oracle DDL/DML.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblprdaccessory" (
    "productno" varchar(50) NOT NULL,
    "accessorytype" varchar(50) NOT NULL,
    "accessoryno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) DEFAULT '*' NOT NULL,
    "opname" varchar(200),
    CONSTRAINT "pk_tblprdaccessory" PRIMARY KEY ("productno", "productversion", "opno", "accessorytype", "accessoryno")
);
COMMENT ON TABLE "YMES"."tblprdaccessory" IS 'Oracle source MESTEST.TBLPRDACCESSORY';

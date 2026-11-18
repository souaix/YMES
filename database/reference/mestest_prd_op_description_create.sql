-- CREATE-only PostgreSQL reference translated from direct Oracle MESTEST metadata.
-- Compare YMES live schema and migrations before adding an application migration.
-- This file contains no sample data and must not be executed against Oracle.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblprdopdescription" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "description" varchar(255),
    "datastamp" numeric(11,0) DEFAULT 0,
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT "pk_tblprdopdescription" PRIMARY KEY ("productno", "productversion", "opno")
);

COMMENT ON TABLE "YMES"."tblprdopdescription" IS 'Oracle source MESTEST.TBLPRDOPDESCRIPTION';

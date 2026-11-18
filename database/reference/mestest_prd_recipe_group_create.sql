-- CREATE-only PostgreSQL reference translated from direct Oracle MESTEST metadata.
-- Compare YMES live schema and migrations before adding an application migration.
-- This reference has no sample data and must not be executed against Oracle.
CREATE SCHEMA IF NOT EXISTS "YMES";

-- Product/version to product recipe group assignment. Oracle has no foreign key.
CREATE TABLE IF NOT EXISTS "YMES"."tblprdrecipe" (
    "productno" varchar(50) NOT NULL,
    "productversion" varchar(5) NOT NULL,
    "recipegroup" varchar(50) NOT NULL,
    CONSTRAINT "pk_tblprdrecipe" PRIMARY KEY ("productno", "productversion", "recipegroup")
);
COMMENT ON TABLE "YMES"."tblprdrecipe" IS 'Oracle source MESTEST.TBLPRDRECIPE';

-- Lookup/master table used by the Add dialog. Create only if YMES has no equivalent.
CREATE TABLE IF NOT EXISTS "YMES"."tblprdrecipebasis" (
    "recipegroup" varchar(50) NOT NULL,
    "recipeversion" numeric(2,0) DEFAULT 0 NOT NULL,
    "description" varchar(255),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "issuestate" numeric(1,0) DEFAULT 0,
    "curversion" numeric(1,0) DEFAULT 0 NOT NULL,
    "datastamp" numeric(11,0) DEFAULT 0,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT "pk_tblprdrecipebasis" PRIMARY KEY ("recipegroup", "recipeversion")
);
COMMENT ON TABLE "YMES"."tblprdrecipebasis" IS 'Oracle source MESTEST.TBLPRDRECIPEBASIS';
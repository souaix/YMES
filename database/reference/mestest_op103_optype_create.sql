-- PostgreSQL structure-only reference derived from MESTEST.TBLOPTYPE.
-- This is not a migration. Compare the YMES live schema before adding a forward migration.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tbloptype" (
    "optype" varchar(20) NOT NULL,
    "description" varchar(255),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "issuestate" numeric(1, 0) NOT NULL DEFAULT 0,
    "datastamp" numeric(11, 0) DEFAULT 0,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT "pk_tbloptype" PRIMARY KEY ("optype")
);

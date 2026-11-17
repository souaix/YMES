-- PostgreSQL structure-only reference derived from MESTEST.TBLOPCONTROLRULE.
-- This is not a migration. Compare the YMES live schema and existing migrations before adding a forward migration.
-- Oracle DATE is represented as timestamp without time zone; nullable Oracle DataStamp remains nullable.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblopcontrolrule" (
    "ruleno" varchar(20) NOT NULL,
    "rulename" varchar(50) NOT NULL,
    "rulescript" varchar(3000) NOT NULL,
    "description" varchar(255),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "holddispgroup" varchar(50),
    "sendmail" numeric(1, 0) DEFAULT 0,
    "ruleclass" numeric(1, 0) NOT NULL DEFAULT 0,
    "ruletype" numeric(1, 0) NOT NULL DEFAULT 0,
    "rulemode" numeric(1, 0) DEFAULT 0,
    "rulemodeqty" numeric(2, 0),
    "datastamp" numeric(11, 0) DEFAULT 0,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT "pk_tblopcontrolrule" PRIMARY KEY ("ruleno")
);

-- Oracle metadata reported only the PK unique index and no FK/additional indexes.

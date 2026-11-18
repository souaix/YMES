-- PostgreSQL CREATE-only reference adapted from Oracle MESTEST.TBLPRSTYPE.
-- Not a migration or production deployment script. Compare YMES live schema/migrations first.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblprstype" (
  "processtype" varchar(20) NOT NULL,
  "description" varchar(255),
  "creator" varchar(10),
  "createdate" timestamp without time zone,
  "issuestate" numeric(1,0) DEFAULT 0 NOT NULL,
  "datastamp" numeric(11,0) DEFAULT 0,
  "reviser" varchar(10),
  "revisedate" timestamp without time zone,
  CONSTRAINT "PK_TBLPRSTYPE" PRIMARY KEY ("processtype")
);

-- Oracle has one PK unique index only. Do not add FK/extra indexes without new evidence.

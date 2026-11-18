-- PostgreSQL CREATE-only reference derived from MESTEST.TBLOPECNGROUPBASIS and TBLOPECNGROUPDETAIL.
-- This is not a migration. Compare YMES live schema first; create only genuinely missing tables.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblopecngroupbasis" (
    "opgroupno" varchar(20) NOT NULL,
    "opgroupname" varchar(50) NOT NULL,
    "psno" varchar(50) NOT NULL,
    "opgrouporder" numeric(6, 0),
    "description" varchar(255),
    "issuestate" numeric(1, 0) NOT NULL,
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "datastamp" numeric(11, 0) DEFAULT 0,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    CONSTRAINT "pk_tblopecngroupbasis" PRIMARY KEY ("opgroupno")
);

CREATE TABLE IF NOT EXISTS "YMES"."tblopecngroupdetail" (
    "opgroupno" varchar(20) NOT NULL,
    "opno" varchar(20) NOT NULL,
    CONSTRAINT "pk_tblopecngroupdetail" PRIMARY KEY ("opno")
);

-- Preserve source semantics: no default on IssueState; DataStamp is nullable DEFAULT 0.
-- Oracle detail PK is OPNo alone (globally unique), not (OPGroupNo, OPNo).
-- No FK or non-PK index exists in Oracle source metadata.

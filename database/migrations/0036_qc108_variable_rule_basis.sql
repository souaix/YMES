BEGIN;
-- 2026-11-19 PPK SSH live audit: missing source-aligned tables; no business data copied.
-- PostgreSQL schema-only reference translated from MESTEST.TBLQCRULEBASIS.
-- Compare YMES live schema before creating a migration; preserve YMES-only columns and data.
CREATE TABLE "YMES".tblqcrulebasis (
    ruleserial varchar(30) NOT NULL,
    ruleno varchar(30) NOT NULL,
    rulename varchar(50) NOT NULL,
    functionname varchar(50),
    ruletype numeric(1,0) DEFAULT 0,
    issuestate numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    ruleoption numeric(1,0),
    testoutof numeric(2,0),
    testcount numeric(2,0),
    comparevalue1 varchar(10),
    comparevalue2 varchar(10),
    relation varchar(10),
    wetype numeric(1,0) DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcrulebasis PRIMARY KEY (ruleserial)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcrulebasis TO ymes_app;
COMMIT;

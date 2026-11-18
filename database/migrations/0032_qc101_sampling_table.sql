BEGIN;
-- Live audit 2026-11-18: absent table; source composite PK, no additional FK/index.
CREATE TABLE "YMES".tblqcsamplingtable (
    samplingtable varchar(25) NOT NULL,
    minnum numeric(12,4) NOT NULL,
    maxnum numeric(12,4) NOT NULL,
    samplenum numeric(12,4) NOT NULL,
    rejectnum numeric(12,4),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblqcsamplingtable PRIMARY KEY (samplingtable, minnum, maxnum)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcsamplingtable TO ymes_app;
COMMIT;

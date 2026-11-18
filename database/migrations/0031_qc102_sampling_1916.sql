BEGIN;
-- Live audit 2026-11-18: absent table. Preserve the source unique index (no PK).
CREATE TABLE "YMES".tblqcsampling1916 (
    samplingtype varchar(25) NOT NULL,
    minnum numeric(8,0) NOT NULL,
    maxnum numeric(8,0) NOT NULL,
    slevel varchar(6) NOT NULL,
    samplenum numeric(6,0),
    rejectnum numeric(6,0),
    kvalue numeric(10,3),
    fvalue numeric(10,3),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone
);
CREATE UNIQUE INDEX pk_tblqcsampling1916
    ON "YMES".tblqcsampling1916 (samplingtype, minnum, maxnum, slevel);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcsampling1916 TO ymes_app;
COMMIT;

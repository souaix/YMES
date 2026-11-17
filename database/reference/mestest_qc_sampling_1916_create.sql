-- PostgreSQL empty-table reference translated from MESTEST.TBLQCSAMPLING1916.
-- Compare YMES live schema and migrations before applying; retain YMES additions.
-- Oracle NUMBER(p,s) is represented as numeric(p,s); Oracle DATE as timestamp.
-- Source has a separate unique index, not a declared PRIMARY KEY constraint.
-- No MES business data is included.

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

COMMENT ON TABLE "YMES".tblqcsampling1916 IS 'QC 抽樣計劃設定_1916；來源 MESTEST.TBLQCSAMPLING1916';
COMMENT ON COLUMN "YMES".tblqcsampling1916.datastamp IS '來源 nullable DEFAULT 0；修改時遞增並用舊值比對';

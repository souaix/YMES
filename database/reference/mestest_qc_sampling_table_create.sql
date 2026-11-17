-- PostgreSQL empty-table reference translated from the MESTEST Oracle source DDL.
-- Compare YMES live schema and migrations before applying; do not execute blindly.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone.
-- Source has one composite primary key, implemented by its unique PK index; no FK.
-- No MES business data is included.

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

COMMENT ON TABLE "YMES".tblqcsamplingtable IS 'QC 抽樣表格；來源 MESTEST.TBLQCSAMPLINGTABLE';
COMMENT ON COLUMN "YMES".tblqcsamplingtable.datastamp IS '來源 nullable DEFAULT 0；修改時遞增';

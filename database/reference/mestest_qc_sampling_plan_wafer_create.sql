-- PostgreSQL empty-table reference translated from MESTEST source DDL.
-- Check YMES live schema/migrations before adding a forward migration.
-- Preserve YMES additions; this reference contains no MES business data.
-- Oracle NUMBER(p,s) is represented as numeric(p,s); Oracle DATE as timestamp.
-- Source has no foreign keys. TBLQCSAMPLINGRANGE has no primary/index constraint.

CREATE TABLE "YMES".tblqcsamplingplan_wafer (
    planno varchar(25) NOT NULL,
    sampletype numeric(1,0) NOT NULL,
    samplerate numeric(3,0) NOT NULL DEFAULT 0,
    everynum numeric(4,0) NOT NULL DEFAULT 0,
    samplepiecenum numeric(4,0) NOT NULL DEFAULT 0,
    samplepointnum numeric(6,0) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    functionname varchar(50),
    aqltype varchar(25),
    aql varchar(10),
    qclevel varchar(6),
    rejectqty numeric(4,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcsamplingplan_wafer PRIMARY KEY (planno)
);

CREATE TABLE "YMES".tblqcsamplingrange (
    planno varchar(25) NOT NULL,
    lowerlimit numeric(12,0) NOT NULL DEFAULT 0,
    upperlimit numeric(12,0) NOT NULL DEFAULT 0,
    samplenum numeric(12,0) NOT NULL DEFAULT 0
);

CREATE TABLE "YMES".tblqcsamplingarea_wafer (
    planno varchar(25) NOT NULL,
    area varchar(20) NOT NULL,
    areatype numeric(18,0) NOT NULL DEFAULT 0,
    areano varchar(50) DEFAULT '0',
    CONSTRAINT pk_tblqcsamplingarea_wafer PRIMARY KEY (planno, area)
);

COMMENT ON TABLE "YMES".tblqcsamplingplan_wafer IS 'QC104 Wafer 抽樣計劃主檔；來源 MESTEST.TBLQCSAMPLINGPLAN_WAFER';
COMMENT ON COLUMN "YMES".tblqcsamplingplan_wafer.datastamp IS 'nullable DEFAULT 0；修改/簽核/停用依舊值 CAS';
COMMENT ON TABLE "YMES".tblqcsamplingrange IS 'Wafer 抽樣區間；來源無 PK/index/FK，YMES 若已存在需保留既有 schema';
COMMENT ON TABLE "YMES".tblqcsamplingarea_wafer IS 'QC104 Wafer 抽樣區域；來源 MESTEST.TBLQCSAMPLINGAREA_WAFER';

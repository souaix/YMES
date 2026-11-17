-- PostgreSQL schema-only reference for QC211 二次檢驗抽樣計畫.
-- Oracle source: mestest_qc211_second_inspection_sampling_source_ddl.sql.
-- Compare YMES live schema/migrations first. Apply only a forward migration for missing structures.
-- Source has a standalone unique index, not a declared PK constraint; it has no FK.

CREATE TABLE IF NOT EXISTS "YMES".tblqcsamplingfor2ndinsp (
    "qcitemno" varchar(25) NOT NULL,
    "opno" varchar(20) NOT NULL,
    "qcitemtype" numeric(1,0) DEFAULT 0 NOT NULL,
    "oldsampleplan" varchar(25) NOT NULL,
    "newsampleplan" varchar(25) NOT NULL,
    "datastamp" numeric(11,0) DEFAULT 0,
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone
);

CREATE UNIQUE INDEX IF NOT EXISTS u_tblqcsamplingfor2ndinsp_01
    ON "YMES".tblqcsamplingfor2ndinsp ("qcitemno", "qcitemtype", "opno", "oldsampleplan");

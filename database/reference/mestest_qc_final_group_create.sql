-- PostgreSQL empty-table reference translated from the MESTEST Oracle source DDL.
-- Compare YMES live schema and migrations before applying; do not execute blindly.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone.
-- Source has only its composite primary-key unique index and no foreign keys.
-- No MES business data is included.

CREATE TABLE "YMES".tblqcfinalgroup (
    mclassno varchar(30) NOT NULL,
    opno varchar(20) NOT NULL,
    groupno varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcfinalgroup PRIMARY KEY (mclassno, opno)
);

COMMENT ON TABLE "YMES".tblqcfinalgroup IS 'QC 最後處理群組；來源 MESTEST.TBLQCFINALGROUP';
COMMENT ON COLUMN "YMES".tblqcfinalgroup.opno IS '作業站編號；* 代表通用作業站設定';
COMMENT ON COLUMN "YMES".tblqcfinalgroup.issuestate IS '0 未簽核，1 簽核中，2 已簽核，-1 不使用';
COMMENT ON COLUMN "YMES".tblqcfinalgroup.datastamp IS '來源 nullable DEFAULT 0';

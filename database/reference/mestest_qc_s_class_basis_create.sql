-- YMES PostgreSQL empty-table reference translated from MESTEST Oracle source DDL.
-- Compare live YMES schema and migrations before applying; do not run blindly.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone.
-- Oracle source has only the SClassNo primary-key unique index and no foreign keys.
-- The form requires an approved parent MClass, but Oracle does not enforce it with an FK.
-- No MES business data is included.

CREATE TABLE "YMES".tblqcsclassbasis (
    sclassno varchar(30) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    mclassno varchar(30),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcsclassbasis PRIMARY KEY (sclassno)
);

COMMENT ON TABLE "YMES".tblqcsclassbasis IS 'QC402 次分類基本資料；來源 MESTEST.TBLQCSCLASSBASIS';
COMMENT ON COLUMN "YMES".tblqcsclassbasis.issuestate IS '0 未簽核，1 簽核中，2 已簽核，-1 不使用';
COMMENT ON COLUMN "YMES".tblqcsclassbasis.datastamp IS '來源 nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tblqcsclassbasis.mclassno IS '主分類編號；來源沒有 FK，UI/API 僅允許已簽核主分類';

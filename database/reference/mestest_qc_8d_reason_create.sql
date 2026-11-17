-- YMES PostgreSQL empty-table reference translated from MESTEST Oracle source DDL.
-- Compare live YMES schema and migrations before applying; do not run blindly.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone.
-- Source tables have only their primary-key unique indexes and no foreign keys.
-- No MES business data is included.

CREATE TABLE "YMES".tblqcreasonbasis_8d (
    reasonno varchar(55) NOT NULL,
    reasonname varchar(100) NOT NULL,
    reasontype varchar(50) NOT NULL,
    reasonsubtype varchar(50) NOT NULL,
    reasonlevel numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcreasonbasis_8d PRIMARY KEY (reasonno)
);

CREATE TABLE "YMES".tblqcreasonsubtype_8d (
    reasontype numeric(1,0) NOT NULL,
    reasonsubtype varchar(50) NOT NULL,
    reasonsubtypename varchar(100),
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcreasonsubtype_8d PRIMARY KEY (reasonsubtype)
);

COMMENT ON TABLE "YMES".tblqcreasonbasis_8d IS '8D 原因明細；來源 MESTEST.TBLQCREASONBASIS_8D';
COMMENT ON COLUMN "YMES".tblqcreasonbasis_8d.reasontype IS '來源為 VARCHAR2(50)，舊表單以數字代碼寫入';
COMMENT ON COLUMN "YMES".tblqcreasonbasis_8d.reasonsubtype IS '原因子類別代碼；來源未宣告 FK';
COMMENT ON COLUMN "YMES".tblqcreasonbasis_8d.datastamp IS '來源 nullable DEFAULT 0';
COMMENT ON TABLE "YMES".tblqcreasonsubtype_8d IS '8D 原因子類別；來源 MESTEST.TBLQCREASONSUBTYPE_8D';
COMMENT ON COLUMN "YMES".tblqcreasonsubtype_8d.issuestate IS '0 未簽核，1 簽核中，2 已簽核，-1 不使用';
COMMENT ON COLUMN "YMES".tblqcreasonsubtype_8d.datastamp IS '來源 nullable DEFAULT 0';
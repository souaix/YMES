-- PostgreSQL empty-table references translated from the MESTEST Oracle source DDL.
-- These two QC tables are lookup dependencies for QC406; compare YMES live schema and migrations first.
-- Do not execute blindly or overwrite YMES extensions. No MES business rows are included.

CREATE TABLE "YMES".tblqcmclassbasis (
    mclassno varchar(30) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcmclassbasis PRIMARY KEY (mclassno)
);

CREATE TABLE "YMES".tblqcholddispgroupbasis (
    groupno varchar(20) NOT NULL,
    groupname varchar(50) NOT NULL,
    departmentno varchar(20),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcholddispgroupbasis PRIMARY KEY (groupno)
);

COMMENT ON TABLE "YMES".tblqcmclassbasis IS 'QC 主分類基本資料；來源 MESTEST.TBLQCMCLASSBASIS';
COMMENT ON TABLE "YMES".tblqcholddispgroupbasis IS 'QC 異常處理群組；來源 MESTEST.TBLQCHOLDDISPGROUPBASIS';
COMMENT ON COLUMN "YMES".tblqcmclassbasis.datastamp IS '來源 nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tblqcholddispgroupbasis.datastamp IS '來源 nullable DEFAULT 0';
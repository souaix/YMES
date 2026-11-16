-- PostgreSQL empty-table reference translated from MESTEST Oracle source DDL.
-- Compare YMES live schema and migrations before applying; do not execute blindly.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone.
-- HoldDescriptionID is the primary key; MClassNo has one nonunique index. No FK.
-- No MES business data is included.

CREATE TABLE "YMES".tblqcholddescription (
    mclassno varchar(30) NOT NULL,
    holddescription varchar(4000) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    holddescriptionid varchar(50) NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcholddescription PRIMARY KEY (holddescriptionid)
);

CREATE INDEX i_tblqcholddescription_01 ON "YMES".tblqcholddescription (mclassno);

COMMENT ON TABLE "YMES".tblqcholddescription IS 'QC 異常說明；來源 MESTEST.TBLQCHOLDDESCRIPTION';
COMMENT ON COLUMN "YMES".tblqcholddescription.issuestate IS '0 未簽核，1 簽核中，2 已簽核，-1 不使用';
COMMENT ON COLUMN "YMES".tblqcholddescription.datastamp IS '來源 nullable DEFAULT 0';

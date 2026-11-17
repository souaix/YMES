-- PostgreSQL empty-table reference translated from MESTEST source DDL.
-- Compare YMES live schema/migrations before applying; retain YMES additions.
-- Oracle NUMBER(p,s) is represented as numeric(p,s); Oracle DATE as timestamp.
-- Source tables have PK constraints/indexes and no foreign key.
-- No MES business data is included.

CREATE TABLE "YMES".tblqcsamplingplan (
    planno varchar(25) NOT NULL,
    planname varchar(50) NOT NULL,
    plantype numeric(1,0) NOT NULL,
    functionname varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    sqtycanbbiggerthenpqty numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcsamplingplan PRIMARY KEY (planno)
);

CREATE TABLE "YMES".tblqcsamplingparameter (
    planno varchar(25) NOT NULL,
    parameterno varchar(20) NOT NULL,
    parametervalue varchar(255),
    CONSTRAINT pk_tblqcsamplingparameter PRIMARY KEY (planno, parameterno)
);

COMMENT ON TABLE "YMES".tblqcsamplingplan IS 'QC 抽樣計劃主檔；來源 MESTEST.TBLQCSAMPLINGPLAN';
COMMENT ON COLUMN "YMES".tblqcsamplingplan.datastamp IS '來源 nullable DEFAULT 0；Edit/簽核以舊值 CAS 後遞增';
COMMENT ON TABLE "YMES".tblqcsamplingparameter IS 'QC 抽樣計劃參數；來源 MESTEST.TBLQCSAMPLINGPARAMETER';
COMMENT ON COLUMN "YMES".tblqcsamplingparameter.parametervalue IS 'nullable；空白參數值由舊系統存為 NULL';

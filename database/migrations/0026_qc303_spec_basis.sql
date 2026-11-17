BEGIN;

-- QC303 specification basis/detail. Live YMES schema audit confirmed both
-- source tables were absent. Oracle's fourteen factor columns are represented
-- as a JSONB dictionary; keys are validated against QC Item FactorSpec rows.
CREATE TABLE "YMES".tblqcspecbasis (
    qcitemno       varchar(25) NOT NULL,
    qcspecserialno varchar(35) NOT NULL,
    issuestate     numeric(1,0) NOT NULL DEFAULT 0,
    datastamp      numeric(11,0) DEFAULT 0,
    description    varchar(255),
    creator        varchar(10),
    createdate     timestamp without time zone,
    reviser        varchar(10),
    revisedate     timestamp without time zone,
    factorvalues   jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_tblqcspecbasis PRIMARY KEY (qcspecserialno),
    CONSTRAINT uq_tblqcspecbasis_qcitem_factors UNIQUE (qcitemno, factorvalues)
);

CREATE TABLE "YMES".tblqcspecdetail (
    spcid          numeric(2,0) NOT NULL DEFAULT 0,
    us             numeric(12,4),
    ls             numeric(12,4),
    cs             numeric(12,4),
    qcspecserialno varchar(35) NOT NULL,
    ucl            varchar(15),
    cl             varchar(15),
    lcl            varchar(15),
    memo           varchar(255),
    CONSTRAINT pk_tblqcspecdetail PRIMARY KEY (qcspecserialno, spcid)
);

COMMENT ON TABLE "YMES".tblqcspecbasis IS 'QC303 品管項目規格主檔；Oracle 動態因子欄以 JSONB 保存';
COMMENT ON COLUMN "YMES".tblqcspecbasis.factorvalues IS 'FactorNo/value 字典；key 限該 QC Item 的 FactorSpec=Y 清單，並保留 POSION、THINKNESS 等來源代碼拼字';

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcspecbasis TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcspecdetail TO ymes_app;

COMMIT;

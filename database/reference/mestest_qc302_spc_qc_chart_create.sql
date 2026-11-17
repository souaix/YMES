-- PostgreSQL empty-table reference adapted from the Oracle DDL in
-- mestest_qc302_spc_qc_chart_source_ddl.sql. Reference only; compare live YMES
-- tables and deployed migrations before creating or changing anything.
-- Oracle source declares no foreign keys. The only non-PK index is QCItemNo.
-- FactorNo columns are added dynamically in legacy Oracle. YMES should store
-- approved online factor values in factorvalues JSONB and never run ALTER TABLE
-- based on uploaded column names.
-- The business-key UNIQUE constraint below mirrors the Core duplicate check;
-- check existing duplicates before adding this constraint to a populated schema.

CREATE TABLE "YMES".tblspcqcchartbasis (
    factorserial varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    samplesize numeric(5,0) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0,
    keynode varchar(1) DEFAULT 'N',
    sponsor varchar(50),
    charttitle varchar(50),
    isshortrunchart numeric(1,0) NOT NULL DEFAULT 0,
    planno varchar(25),
    chartunit varchar(30),
    mo varchar(61) DEFAULT '*',
    memo varchar(255),
    product varchar(61) DEFAULT '*',
    equipment varchar(61) DEFAULT '*',
    br varchar(61) DEFAULT '*',
    customer varchar(61) DEFAULT '*',
    op varchar(61) DEFAULT '*',
    preopno varchar(61) DEFAULT '*',
    process varchar(61) DEFAULT '*',
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    mtlvendor varchar(61) DEFAULT '*',
    eqptype varchar(61) DEFAULT '*',
    material varchar(61) DEFAULT '*',
    version varchar(61) DEFAULT '*',
    posion varchar(61) DEFAULT '*',
    employee varchar(61) DEFAULT '*',
    thinkness varchar(61) DEFAULT '*',
    thickness varchar(61) DEFAULT '*',
    device varchar(61) DEFAULT '*',
    factorvalues jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_tblspcqcchartbasis PRIMARY KEY (factorserial),
    CONSTRAINT uq_tblspcqcchartbasis_qcitem_factors UNIQUE (qcitemno, factorvalues)
);

CREATE INDEX i_tblspcqcchartbasis_01 ON "YMES".tblspcqcchartbasis (qcitemno);

CREATE TABLE "YMES".tblspcqcchartdetail (
    factorserial varchar(50) NOT NULL,
    spcid numeric(2,0) NOT NULL,
    us varchar(15),
    ls varchar(15),
    ucl varchar(15),
    lcl varchar(15),
    cl varchar(15),
    target varchar(10),
    cpkgoal varchar(10),
    yaxisfrom varchar(10),
    yaxisto varchar(10),
    cs varchar(15),
    CONSTRAINT pk_tblspcqcchartdetail PRIMARY KEY (factorserial, spcid)
);

CREATE TABLE "YMES".tblspcqcchartdetaillog (
    factorserial varchar(50) NOT NULL,
    spcid numeric(2,0) NOT NULL,
    us varchar(15),
    ls varchar(15),
    ucl varchar(15),
    lcl varchar(15),
    cl varchar(15),
    creator varchar(10),
    starttime timestamp without time zone NOT NULL,
    target varchar(10),
    cpkgoal varchar(10),
    yaxisfrom varchar(10),
    yaxisto varchar(10),
    cs varchar(15),
    memo varchar(255),
    CONSTRAINT pk_tblspcqcchartdetaillog PRIMARY KEY (factorserial, spcid, starttime)
);

CREATE TABLE "YMES".tblspcqcchartrule (
    factorserial varchar(50) NOT NULL,
    spcid numeric(2,0) NOT NULL,
    ruleno varchar(30) NOT NULL,
    action numeric(1,0) NOT NULL,
    equipmentstate numeric(2,0),
    erfstate numeric(1,0),
    emailstate numeric(1,0),
    CONSTRAINT pk_tblspcqcchartrule PRIMARY KEY (factorserial, spcid, ruleno)
);

COMMENT ON TABLE "YMES".tblspcqcchartbasis IS 'QC302 管制圖主檔；動態核准要因值以 JSONB 儲存';
COMMENT ON COLUMN "YMES".tblspcqcchartbasis.factorvalues IS 'FactorNo/value 字典；FactorNo 必須來自該 QC Item 已核准的線上管制要因白名單';
COMMENT ON COLUMN "YMES".tblspcqcchartbasis.posion IS '保留 Oracle 來源欄位拼字 POSION';
COMMENT ON COLUMN "YMES".tblspcqcchartbasis.thinkness IS '保留 Oracle 來源欄位拼字 THINKNESS';

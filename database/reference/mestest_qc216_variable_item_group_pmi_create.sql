-- PostgreSQL YMES empty-schema reference for QC216 PMI variable item groups.
-- Compare the home PC live YMES schema and migrations before use.
-- This is not Oracle SQL and must never be run against MES MESTEST.
-- Oracle source DDL and dictionary metadata were read-only queries through
-- PowerShell 5.1 + Oracle.ManagedDataAccess; no MES business rows were queried.
--
-- Direct tables: TBLQCITEMGROUPBASIS_PMI, TBLQCITEMGROUPDETAIL_PMI,
-- TBLQCGROUPDEDUCTIONBASIS_PMI. Legacy hard-delete maps:
-- TBLSPCFQCFormQCItemMap, TBLSPCINCOMINGQCFormQCItemMap,
-- TBLSPCOQCFormQCItemMap. Oracle has only the listed PK unique indexes and
-- no foreign keys or additional indexes for these tables.
--
-- YMES adaptation: Oracle adds factor columns dynamically to the deduction
-- basis table. Store those values in factorvalues JSONB, keyed by the approved
-- MES parameter number (case-insensitive field mapping); preserve '*' as the
-- wildcard value. Do not create dynamic SQL columns from user input.

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemgroupbasis_pmi (
    groupno varchar(25) NOT NULL,
    groupname varchar(50),
    lotisvalid numeric(1,0) DEFAULT 0,
    componentcollect numeric(1,0) DEFAULT 0,
    planno varchar(25),
    formname varchar(50),
    weburl varchar(100),
    unionerf numeric(1,0) DEFAULT 0,
    errorreportname varchar(50),
    noticereportname varchar(50),
    issuestate numeric(1,0) DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    cursormode numeric(1,0),
    qcclass varchar(30),
    getfilepath varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemgroupbasis_pmi PRIMARY KEY (groupno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemgroupdetail_pmi (
    groupno varchar(25) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    showorder numeric(3,0) NOT NULL DEFAULT 0,
    deductionitem numeric(1,0) DEFAULT 0,
    CONSTRAINT pk_tblqcitemgroupdetail_pmi PRIMARY KEY (groupno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcgroupdeductionbasis_pmi (
    deductionserial varchar(50) NOT NULL,
    groupno varchar(25) NOT NULL,
    normaltimes numeric(2,0),
    deductiontimes numeric(2,0),
    curpass numeric(3,0) DEFAULT 0,
    curstatus numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    factorvalues jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_tblqcgroupdeductionbasis_pmi PRIMARY KEY (deductionserial)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcfqcformqcitemmap (
    qcformno varchar(30) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemname varchar(500),
    qcitemtype numeric(1,0) NOT NULL,
    status numeric(1,0) NOT NULL DEFAULT 0,
    result numeric(1,0),
    spcserial varchar(1500),
    CONSTRAINT pk_tblspcfqcformqcitemmap PRIMARY KEY (qcformno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcincomingqcformqcitemmap (
    qcformno varchar(30) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemname varchar(500),
    qcitemtype numeric(1,0) NOT NULL,
    status numeric(1,0) NOT NULL DEFAULT 0,
    result numeric(1,0),
    spcserial varchar(1500),
    CONSTRAINT pk_tblspcincomingqcformqcitemmap PRIMARY KEY (qcformno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcoqcformqcitemmap (
    qcformno varchar(30) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    status numeric(1,0) NOT NULL DEFAULT 0,
    result numeric(1,0),
    qcitemname varchar(500),
    spcserial varchar(1500),
    CONSTRAINT pk_tblspcoqcformqcitemmap PRIMARY KEY (qcformno, qcitemno)
);

COMMENT ON TABLE "YMES".tblqcitemgroupbasis_pmi IS 'QC216 PMI 計量品管項目群組主檔；來源 MESTEST.TBLQCITEMGROUPBASIS_PMI';
COMMENT ON COLUMN "YMES".tblqcitemgroupbasis_pmi.issuestate IS '0 未簽核、1 簽核中、2 已核准、-1 不使用；來源 nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tblqcitemgroupdetail_pmi.deductionitem IS '此明細是否為減項；來源 nullable DEFAULT 0';
COMMENT ON TABLE "YMES".tblqcgroupdeductionbasis_pmi IS 'QC216 群組扣除基準；factorvalues JSONB 適配 Oracle 動態因子欄位';
COMMENT ON COLUMN "YMES".tblqcgroupdeductionbasis_pmi.factorvalues IS '參數編號至值的 JSON object；''*'' 代表未指定條件';
COMMENT ON TABLE "YMES".tblspcfqcformqcitemmap IS 'QC216 硬刪清理關聯；來源 MESTEST.TBLSPCFQCFORMQCITEMMAP';
COMMENT ON TABLE "YMES".tblspcincomingqcformqcitemmap IS 'QC216 硬刪清理關聯；來源 MESTEST.TBLSPCINCOMINGQCFORMQCITEMMAP';
COMMENT ON TABLE "YMES".tblspcoqcformqcitemmap IS 'QC216 硬刪清理關聯；來源 MESTEST.TBLSPCOQCFORMQCITEMMAP';

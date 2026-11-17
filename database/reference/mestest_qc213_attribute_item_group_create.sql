-- PostgreSQL YMES empty-table reference for QC213: Attribute QC Item Group.
-- Compare the home PC live YMES schema and migrations before use.
-- Do not run this file against MES Oracle; it is CREATE-only reference SQL.
-- Oracle source DDL was read from MESTEST with PowerShell 5.1 +
-- Oracle.ManagedDataAccess; no business rows were queried.
--
-- Direct source tables:
--   TBLQCITEMATTGROUPORDERBASIS
--   TBLQCITEMATTGORDERDETAIL
-- Hard-delete dependencies:
--   TBLOPGQCITEMDETAIL, TBLPRDQCITEM, TBLWIPTEMP_QCITEM
-- Oracle defines only each table's primary-key unique index. There are no
-- foreign keys or additional indexes; none are introduced here.

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemattgrouporderbasis (
    groupno varchar(25) NOT NULL,
    groupname varchar(50) NOT NULL,
    lotisvalid numeric(1,0) DEFAULT 0,
    issuestate numeric(1,0) DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemattgrouporderbasis PRIMARY KEY (groupno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemattgorderdetail (
    groupno varchar(25) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    showorder numeric(3,0) DEFAULT 0,
    CONSTRAINT pk_tblqcitemattgorderdetail PRIMARY KEY (groupno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopgqcitemdetail (
    opno varchar(20) NOT NULL,
    groupno varchar(25) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    inline numeric(1,0) DEFAULT 1,
    executeorder numeric(2,0) DEFAULT 0,
    CONSTRAINT pk_tblopgqcitemdetail PRIMARY KEY (opno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdqcitem (
    productno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    executeorder numeric(2,0) NOT NULL DEFAULT 0,
    inline numeric(1,0) NOT NULL DEFAULT 1,
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdqcitem PRIMARY KEY (productno, productversion, opno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwiptemp_qcitem (
    lotno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    executeflag numeric(1,0) NOT NULL,
    executeorder numeric(2,0) DEFAULT 0,
    reinspwith2ndinspplan numeric(1,0) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    executeflag_fai numeric(1,0) DEFAULT 0,
    executeflag_ipqc numeric(1,0) DEFAULT 0
);

CREATE INDEX IF NOT EXISTS i_tblwiptemp_qcitem_01 ON "YMES".tblwiptemp_qcitem (lotno, qcitemno);
CREATE INDEX IF NOT EXISTS i_tblwiptemp_qcitem_02 ON "YMES".tblwiptemp_qcitem (lotno);

COMMENT ON TABLE "YMES".tblqcitemattgrouporderbasis IS 'QC213 計數品管項目群組主檔；來源 MESTEST.TBLQCITEMATTGROUPORDERBASIS';
COMMENT ON COLUMN "YMES".tblqcitemattgrouporderbasis.lotisvalid IS 'Lot Valid Check：0 No、1 Yes；來源 nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tblqcitemattgrouporderbasis.issuestate IS '0 未簽核、1 簽核中、2 已核准、-1 不使用；來源 nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tblqcitemattgrouporderbasis.datastamp IS '來源 nullable DEFAULT 0';
COMMENT ON TABLE "YMES".tblqcitemattgorderdetail IS 'QC213 計數品管項目群組明細；來源 MESTEST.TBLQCITEMATTGORDERDETAIL';
COMMENT ON COLUMN "YMES".tblqcitemattgorderdetail.showorder IS '群組內顯示順序；來源 nullable DEFAULT 0';
COMMENT ON TABLE "YMES".tblopgqcitemdetail IS 'QC213 硬刪除清理關聯；來源 MESTEST.TBLOPGQCITEMDETAIL';
COMMENT ON TABLE "YMES".tblprdqcitem IS 'QC213 硬刪除清理關聯；來源 MESTEST.TBLPRDQCITEM';
COMMENT ON TABLE "YMES".tblwiptemp_qcitem IS 'QC213 硬刪前檢查線上 QC 使用狀態；來源 MESTEST.TBLWIPTEMP_QCITEM；來源無 PK/FK';

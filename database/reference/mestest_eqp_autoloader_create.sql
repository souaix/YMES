-- YMES PostgreSQL empty-table reference translated from Oracle source DDL.
-- Compare the live YMES schema and migrations before applying; do not run blindly.
-- Oracle source: MESTEST.TBLEQPALSCRIPT and its three detail tables.
-- Oracle NUMBER -> numeric; DATE -> timestamp without time zone; CLOB -> text.
-- The Oracle main table has no PK constraint; its source-defined unique index is
-- on (EquipmentNo, FileNameFormat), so preserve it as a unique index here.
-- No Oracle foreign-key constraints exist among these four source tables.

CREATE TABLE "YMES".tbleqpalscript (
    equipmentno varchar(50) NOT NULL,
    formatscript text,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    filenameformat varchar(100) NOT NULL,
    storerawdata numeric(1,0) DEFAULT 0 NOT NULL,
    sendmail numeric(1,0) DEFAULT 0 NOT NULL,
    masterseparatesign varchar(10),
    detailcloumnseparatesign varchar(10),
    multiwafer numeric(1,0) DEFAULT 0,
    rawdataformatscript text,
    batchformat numeric(1,0) DEFAULT 0,
    havecoordinfo numeric(1,0) DEFAULT 0,
    withheader numeric(1,0),
    iscoordinatey numeric(1,0),
    datastamp numeric(11,0) DEFAULT 0
);
CREATE UNIQUE INDEX pk_tbleqpalscript ON "YMES".tbleqpalscript (equipmentno, filenameformat);

CREATE TABLE "YMES".tbleqpalscript_testrecipe (
    equipmentno varchar(50) NOT NULL,
    filenameformat varchar(100) NOT NULL,
    testrecipeno varchar(50) NOT NULL,
    calculation varchar(100),
    formula varchar(50),
    filefield varchar(50),
    CONSTRAINT pk_tbleqpalscript_testrecipe PRIMARY KEY (equipmentno, filenameformat, testrecipeno)
);

CREATE TABLE "YMES".tbleqpalscript_unnumericfield (
    equipmentno varchar(50) NOT NULL,
    filenameformat varchar(100) NOT NULL,
    fieldname varchar(50) NOT NULL,
    datatype numeric(1,0) NOT NULL,
    CONSTRAINT pk_tbleqpalscript_unnumericfield PRIMARY KEY (equipmentno, filenameformat, fieldname)
);

CREATE TABLE "YMES".tbleqpalscript_coordinfo (
    equipmentno varchar(50) NOT NULL,
    filenameformat varchar(100) NOT NULL,
    fieldindex numeric(3,0) NOT NULL,
    coordaxistype numeric(1,0) NOT NULL,
    CONSTRAINT pk_tbleqpalscript_coordinfo PRIMARY KEY (equipmentno, filenameformat, coordaxistype)
);

COMMENT ON TABLE "YMES".tbleqpalscript IS 'MES 設備 AutoLoader 格式主檔；來源 MESTEST.TBLEQPALSCRIPT';
COMMENT ON COLUMN "YMES".tbleqpalscript.equipmentno IS '設備編號；複合唯一鍵之一';
COMMENT ON COLUMN "YMES".tbleqpalscript.formatscript IS 'AutoLoader 欄位解析腳本 CLOB';
COMMENT ON COLUMN "YMES".tbleqpalscript.filenameformat IS '檔名格式；複合唯一鍵之一';
COMMENT ON COLUMN "YMES".tbleqpalscript.storerawdata IS '是否儲存原始資料';
COMMENT ON COLUMN "YMES".tbleqpalscript.sendmail IS '是否發送郵件';
COMMENT ON COLUMN "YMES".tbleqpalscript.masterseparatesign IS '主檔分隔符號';
COMMENT ON COLUMN "YMES".tbleqpalscript.detailcloumnseparatesign IS '明細欄位分隔符號；保留來源欄名拼字';
COMMENT ON COLUMN "YMES".tbleqpalscript.multiwafer IS '多晶圓格式代碼；nullable DEFAULT 0，單批次為 -1';
COMMENT ON COLUMN "YMES".tbleqpalscript.rawdataformatscript IS '原始資料解析腳本 CLOB';
COMMENT ON COLUMN "YMES".tbleqpalscript.batchformat IS '設備生產資料格式旗標；nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tbleqpalscript.havecoordinfo IS '是否包含座標設定；nullable DEFAULT 0';
COMMENT ON COLUMN "YMES".tbleqpalscript.withheader IS '檔案是否含表頭';
COMMENT ON COLUMN "YMES".tbleqpalscript.iscoordinatey IS 'IC Ingot 座標軸判斷旗標';
COMMENT ON COLUMN "YMES".tbleqpalscript.datastamp IS '來源 nullable DEFAULT 0；新 API 以 CAS 更新';
COMMENT ON TABLE "YMES".tbleqpalscript_testrecipe IS '設備 AutoLoader 測試配方明細';
COMMENT ON TABLE "YMES".tbleqpalscript_unnumericfield IS '設備 AutoLoader 非數值欄位明細';
COMMENT ON TABLE "YMES".tbleqpalscript_coordinfo IS '設備 AutoLoader 座標欄位明細';

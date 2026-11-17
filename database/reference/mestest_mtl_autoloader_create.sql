-- PostgreSQL empty-table reference translated from Oracle source DDL.
-- Compare the live YMES schema and migrations before applying; do not run blindly.
-- Oracle TABLESPACE, storage, and BASICFILE LOB clauses are omitted.
-- Oracle NUMBER -> numeric; Oracle DATE -> timestamp without time zone; CLOB -> text.

CREATE TABLE "YMES".tblmtlvendoralscript (
    mtlfolderpath varchar(400) NOT NULL,
    vendorno varchar(20),
    formatscript text,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    storerawdata numeric(1,0) DEFAULT 0 NOT NULL,
    sendmail numeric(1,0) DEFAULT 0 NOT NULL,
    masterseparatesign varchar(10),
    detailcloumnseparatesign varchar(10),
    rawdataformatscript text,
    withheader numeric(1,0),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblmtlvendoralscript PRIMARY KEY (mtlfolderpath)
);

COMMENT ON TABLE "YMES".tblmtlvendoralscript IS 'MES incoming material AutoLoader format setup; Oracle source: MESTEST.TBLMTLVENDORALSCRIPT';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.mtlfolderpath IS 'AutoLoader 監控目錄；主鍵';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.vendorno IS '供應商編號';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.formatscript IS '欄位解析規則 CLOB';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.description IS '格式說明';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.creator IS '建立人';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.createdate IS '建立日期';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.reviser IS '修改人';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.revisedate IS '修改日期';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.storerawdata IS '是否儲存原始資料；WinForms 目前固定傳入 0';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.sendmail IS '是否發送郵件';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.masterseparatesign IS '表頭分隔符號';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.detailcloumnseparatesign IS '明細欄位分隔符號';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.rawdataformatscript IS '原始資料格式描述 CLOB';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.withheader IS '是否有表頭';
COMMENT ON COLUMN "YMES".tblmtlvendoralscript.datastamp IS '資料版本戳記；來源為 nullable DEFAULT 0';

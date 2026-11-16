-- YMES PostgreSQL empty-table reference translated from Oracle source DDL.
-- Compare the live YMES schema and migration history before applying; do not run this blindly.
CREATE TABLE "YMES".tblmtlmaterialgroupbasis (
    groupno varchar(50) NOT NULL,
    materialtype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialgroupbasis PRIMARY KEY (groupno)
);

COMMENT ON TABLE "YMES".tblmtlmaterialgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLMTLMATERIALGROUPBASIS';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.groupno IS '物料群組編號';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.materialtype IS '物料類別';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.description IS '說明';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.creator IS '建立人';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.createdate IS '建立日期';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.datastamp IS '並行版本戳記';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.reviser IS '修改人';
COMMENT ON COLUMN "YMES".tblmtlmaterialgroupbasis.revisedate IS '修改日期';

-- PostgreSQL empty-table reference translated from Oracle source DDL.
-- Compare the live YMES schema and migrations before applying; do not run this blindly.
-- Oracle tablespace/storage clauses are omitted; NUMBER -> numeric; DATE -> timestamp without time zone.

CREATE TABLE "YMES".tblmtllabelmap (
    materialtype varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    labelsource numeric(1,0) NOT NULL,
    labeltype varchar(50) NOT NULL,
    labelno varchar(50) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtllabelmap PRIMARY KEY (materialtype, materialno, labelsource, labeltype)
);

COMMENT ON TABLE "YMES".tblmtllabelmap IS 'MES material label mapping; Oracle source: MESTEST.TBLMTLLABELMAP';
COMMENT ON COLUMN "YMES".tblmtllabelmap.materialtype IS '物料類別';
COMMENT ON COLUMN "YMES".tblmtllabelmap.materialno IS '物料編號';
COMMENT ON COLUMN "YMES".tblmtllabelmap.labelsource IS '標籤來源；物料對應固定為 5';
COMMENT ON COLUMN "YMES".tblmtllabelmap.labeltype IS '標籤類別';
COMMENT ON COLUMN "YMES".tblmtllabelmap.labelno IS '標籤編號';
COMMENT ON COLUMN "YMES".tblmtllabelmap.creator IS '建立人';
COMMENT ON COLUMN "YMES".tblmtllabelmap.createdate IS '建立日期';
COMMENT ON COLUMN "YMES".tblmtllabelmap.datastamp IS '並行版本戳記';
COMMENT ON COLUMN "YMES".tblmtllabelmap.reviser IS '修改人';
COMMENT ON COLUMN "YMES".tblmtllabelmap.revisedate IS '修改日期';

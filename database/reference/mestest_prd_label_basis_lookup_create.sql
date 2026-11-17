-- PostgreSQL empty-table reference for the existing MES label lookup table.
-- Compare the live YMES schema and migrations before applying; do not run this blindly.
-- This file is only needed if YMES does not already contain the label basis table.

CREATE TABLE "YMES".tblprdlabelbasis (
    labeltype varchar(30) NOT NULL,
    labelno varchar(50) NOT NULL,
    formatfilename varchar(100) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    description varchar(255),
    printerno varchar(100),
    labelsource numeric(1,0) NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdlabelbasis PRIMARY KEY (labelno)
);

COMMENT ON TABLE "YMES".tblprdlabelbasis IS 'MES label definitions; Oracle source: MESTEST.TBLPRDLABELBASIS';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.labeltype IS '標籤類別';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.labelno IS '標籤編號';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.formatfilename IS '標籤格式檔名';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.creator IS '建立人';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.createdate IS '建立日期';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.description IS '說明';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.printerno IS '印表機編號';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.labelsource IS '標籤來源';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.datastamp IS '並行版本戳記';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.reviser IS '修改人';
COMMENT ON COLUMN "YMES".tblprdlabelbasis.revisedate IS '修改日期';

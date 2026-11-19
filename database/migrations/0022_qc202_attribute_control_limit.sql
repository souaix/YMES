-- QC202 YMES fixed-schema adaptation of MESTEST.TBLSPCATTLIMIT.
-- Oracle dynamically adds FactorNo columns; YMES stores approved FactorOnline
-- values in JSONB to keep identifiers out of dynamically assembled SQL.
BEGIN;

CREATE TABLE IF NOT EXISTS "YMES".tblspcattlimit (
    qcitemno varchar(25) NOT NULL,
    ucl varchar(15),
    lcl varchar(15),
    cl varchar(15),
    us varchar(15),
    ls varchar(15),
    ruleset varchar(500),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    factorvalues jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT uq_tblspcattlimit_qcitem_factors UNIQUE (qcitemno, factorvalues)
);

CREATE INDEX IF NOT EXISTS i_tblspcattlimit_01 ON "YMES".tblspcattlimit (qcitemno);

COMMENT ON TABLE "YMES".tblspcattlimit IS 'QC202 計數管制內容；來源 MESTEST.TBLSPCATTLIMIT，動態因子值改存 JSONB';
COMMENT ON COLUMN "YMES".tblspcattlimit.datastamp IS '來源 nullable DEFAULT 0；修改以 nullable CAS 比對後遞增';
COMMENT ON COLUMN "YMES".tblspcattlimit.factorvalues IS 'QC201 核准 FactorOnline FactorNo/value 字典；鍵名須由服務端白名單驗證';

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblspcattlimit TO ymes_app;

COMMIT;

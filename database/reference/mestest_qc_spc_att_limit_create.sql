-- YMES PostgreSQL empty-table reference adapted from MESTEST.TBLSPCATTLIMIT.
-- Oracle dynamically adds one column per FactorOnline FactorNo; YMES stores
-- those values in factorvalues JSONB to keep a fixed, safe API schema.
-- This is a reference only. Compare the live YMES schema and migrations first.
-- Source Oracle table has no PK/FK/unique constraint; it has a non-unique
-- index on QCITEMNO. The unique pair below preserves the WinForms duplicate
-- check as a server-side invariant.

CREATE TABLE "YMES".tblspcattlimit (
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

CREATE INDEX i_tblspcattlimit_01 ON "YMES".tblspcattlimit (qcitemno);

COMMENT ON TABLE "YMES".tblspcattlimit IS 'QC202 計數管制內容；來源 MESTEST.TBLSPCATTLIMIT，動態因子值改存 JSONB';
COMMENT ON COLUMN "YMES".tblspcattlimit.datastamp IS '來源 nullable DEFAULT 0；修改時以 nullable CAS 比對並遞增';
COMMENT ON COLUMN "YMES".tblspcattlimit.factorvalues IS 'FactorOnline FactorNo/value 字典；鍵值需由 QC Item 核准因子白名單驗證';

-- PostgreSQL structure reference for QC205's THEIL BU1 Memo setting.
-- Compare with the live YMES schema before creating a migration. This is not an Oracle script.
-- Oracle source: MESTEST.XX_TBLQCITEM_MEMO, captured read-only through DBMS_METADATA on 2026-11-17.
-- Oracle has no PK/index; YMES adds a unique composite key to enforce the replace-set behavior.

CREATE TABLE IF NOT EXISTS "YMES".xx_tblqcitem_memo (
    qcitemno varchar(50) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    memo_item varchar(50) NOT NULL,
    resvitor varchar(50) NOT NULL,
    "datetime" timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblqcitem_memo PRIMARY KEY (qcitemno, qcitemtype, memo_item)
);

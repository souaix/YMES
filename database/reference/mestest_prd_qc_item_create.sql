-- PostgreSQL CREATE-only reference for the current PD102 QC Item table.
-- Compare with YMES live schema and migration history before applying a change.
-- CREATE IF NOT EXISTS cannot add XX_FREQ to an existing table.
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
    revisedate timestamp(0) without time zone,
    xx_freq varchar(300),
    CONSTRAINT pk_tblprdqcitem PRIMARY KEY (productno, productversion, opno, qcitemno)
);

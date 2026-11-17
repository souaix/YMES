-- YMES PostgreSQL schema-only reference translated from MESTEST.TBLQCREASONPRDSOPFILE.
-- Compare the live YMES schema before creating a migration; preserve YMES-only columns.
CREATE TABLE IF NOT EXISTS "YMES".tblqcreasonprdsopfile (
    reasonno varchar(20) NOT NULL,
    productno varchar(50) NOT NULL,
    sopfilename varchar(100),
    sopfilepath varchar(2000),
    CONSTRAINT pk_tblqcreasonprdsopfile PRIMARY KEY (reasonno, productno)
);

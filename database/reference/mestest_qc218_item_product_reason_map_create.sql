-- PostgreSQL CREATE-only reference for QC218.
-- Oracle MESTEST source DDL (including the three shared lookup tables) is in:
--   mestest_qc218_tblqcitemprdreasonmap_source_ddl.sql
--   mestest_qc218_tblqcitemreasonmap_source_ddl.sql
--   mestest_qc218_tblqcitemforattributes_source_ddl.sql
--   mestest_qc218_tblqcreasonbasis_source_ddl.sql
--   mestest_qc218_tblprdproductbasis_source_ddl.sql
-- This is not a migration. Compare YMES live schema first; create/alter only
-- missing structures using a forward migration. Never run this on Oracle.

CREATE TABLE IF NOT EXISTS tblqcitemprdreasonmap (
    qcitemno   varchar(25) NOT NULL,
    productno  varchar(50) NOT NULL,
    reasonno   varchar(20) NOT NULL,
    creator    varchar(10),
    createdate timestamp without time zone,
    reviser    varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemprdreasonmap
        PRIMARY KEY (qcitemno, productno, reasonno)
);

CREATE TABLE IF NOT EXISTS tblqcitemreasonmap (
    qcitemno   varchar(25) NOT NULL,
    reasonno   varchar(20) NOT NULL,
    creator    varchar(10),
    createdate timestamp without time zone,
    reviser    varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemreasonmap
        PRIMARY KEY (qcitemno, reasonno)
);

-- Oracle declares no foreign keys or non-PK indexes on these two mapping tables.
-- QC item, product, and reason masters are shared tables; do not recreate them
-- just because their source DDL references are included for lookup verification.

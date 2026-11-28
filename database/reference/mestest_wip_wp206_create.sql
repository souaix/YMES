-- PostgreSQL CREATE-only reference derived from MESTEST source DDL.
-- TBLOPABNORMAL is already provided by mestest_operation_and_system_create.sql; do not recreate it here.
-- Compare YMES live schema and migration history before creating any of these objects.

CREATE TABLE IF NOT EXISTS "YMES".tblqcabnormalbasis (
    abnormalno varchar(20) CONSTRAINT n_tblqcabnormalbasis_01 NOT NULL,
    abnormalname varchar(100) CONSTRAINT n_tblqcabnormalbasis_02 NOT NULL,
    abnormaltype varchar(20) CONSTRAINT n_tblqcabnormalbasis_03 NOT NULL,
    linkname varchar(20) CONSTRAINT n_tblqcabnormalbasis_04 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblqcabnormalbasis_05 NOT NULL,
    priority numeric(2,0) DEFAULT 0,
    CONSTRAINT pk_tblqcabnormalbasis PRIMARY KEY (abnormalno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcabnormalposition (
    abnormalno varchar(20) CONSTRAINT n_tblqcabnormalposition_01 NOT NULL,
    position varchar(50) CONSTRAINT n_tblqcabnormalposition_02 NOT NULL,
    CONSTRAINT pk_tblqcabnormalposition PRIMARY KEY (abnormalno, position)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwipcont_abnormal (
    lotserial varchar(55),
    opno varchar(20),
    abnormalno varchar(20),
    position varchar(50),
    abnormalqty numeric(6,0) DEFAULT 0 CONSTRAINT n_tblwipcont_abnormal_01 NOT NULL,
    userno varchar(10),
    lotno varchar(50),
    shiftno varchar(20),
    eventtime timestamp without time zone
);

CREATE INDEX IF NOT EXISTS i_tblwipcont_abnormal_01 ON "YMES".tblwipcont_abnormal (lotserial);

-- Oracle has no PK, FK, unique index, or trigger on TBLWIPCONT_ABNORMAL.
-- Keep this source index non-unique; do not invent a primary key for the table.

-- PostgreSQL YMES schema reference for QC212: Variable QC Item Group.
-- Compare with the home PC live YMES schema before writing a migration.
-- Do not run this file against MES Oracle. It contains CREATE statements only.
--
-- Oracle source for TBLQCITEMGROUPDETAIL:
--   database/reference/mestest_qc205_variable_dependencies_source_ddl.sql
--   (also present in mestest_qc_item_attributes_source_ddl.sql)
-- Oracle source for TBLQCCALCULATESCRIPT:
--   database/reference/mestest_qc205_variable_dependencies_source_ddl.sql
-- TBLQCITEMGROUPBASIS already has a PostgreSQL reference in
--   database/reference/mestest_mtl_qc_item_tables_create.sql
-- THEIL memo XX_TBLQCITEM_MEMO already has a PostgreSQL reference in
--   database/reference/mestest_qc205_theil_custom_create.sql
--
-- The two tables below are the QC212 dependencies missing from existing
-- PostgreSQL CREATE references. No foreign keys are added because Oracle
-- has no corresponding FK constraints.

CREATE TABLE IF NOT EXISTS "YMES".tblqcitemgroupdetail (
    groupno varchar(25),
    qcitemno varchar(25),
    showorder numeric(3,0) DEFAULT 0 CONSTRAINT n_tblqcitemgroupdetail_01 NOT NULL,
    CONSTRAINT pk_tblqcitemgroupdetail PRIMARY KEY (groupno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqccalculatescript (
    qcitemno varchar(25) CONSTRAINT n_tblqccalculatescript_01 NOT NULL,
    script varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    groupno varchar(25) DEFAULT 'N/A' CONSTRAINT n_tblqccalculatescript_02 NOT NULL,
    CONSTRAINT pk_tblqccalculatescript PRIMARY KEY (qcitemno, groupno)
);

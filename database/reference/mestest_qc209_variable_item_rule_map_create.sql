-- PostgreSQL schema-only reference for QC209 variable QC item rule assignment.
-- Oracle source DDL was captured read-only from MESTEST and is reused from:
-- database/reference/mestest_qc205_variable_dependencies_source_ddl.sql (TBLQCITEMRULE).
-- Compare the live YMES schema and deployed migrations before creating any migration.
-- Preserve Oracle optional columns. The legacy QC209 UpdQCItemRule replaces rows and inserts only
-- QCItemNo/SPCID/RuleNo/Action, so EquipmentState/ERFState/EmailState become NULL on saved rows.
CREATE TABLE IF NOT EXISTS "YMES".tblqcitemrule (
    qcitemno varchar(25) NOT NULL,
    spcid numeric(2,0) NOT NULL,
    ruleno varchar(20) NOT NULL,
    action numeric(1,0) NOT NULL,
    equipmentstate numeric(2,0),
    erfstate numeric(1,0),
    emailstate numeric(1,0),
    CONSTRAINT pk_tblqcitemrule PRIMARY KEY (qcitemno, spcid, ruleno)
);

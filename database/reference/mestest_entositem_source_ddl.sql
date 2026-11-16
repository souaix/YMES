-- Oracle MESTEST source definition queried read-only on 2026-11-16.
-- Queried with Windows PowerShell 5.1 + Oracle.ManagedDataAccess using the
-- protected local Oracle connection settings. No business rows were queried.
-- Oracle storage and tablespace clauses are omitted from this logical DDL.

CREATE TABLE MESTEST.TBLENTOSITEM (
    OSITEMNO VARCHAR2(2) CONSTRAINT N_TBLENTOSITEM_01 NOT NULL,
    OSITEMNAME VARCHAR2(50) CONSTRAINT N_TBLENTOSITEM_02 NOT NULL,
    PLANDAYS NUMBER(3,0) CONSTRAINT N_TBLENTOSITEM_03 NOT NULL,
    PROCESSNO VARCHAR2(30) CONSTRAINT N_TBLENTOSITEM_04 NOT NULL,
    RETURNTYPE NUMBER(1,0) CONSTRAINT N_TBLENTOSITEM_05 NOT NULL,
    DESCRIPTION VARCHAR2(255),
    CREATOR VARCHAR2(10),
    CREATEDATE DATE,
    DATASTAMP NUMBER(11,0) DEFAULT 0,
    REVISER VARCHAR2(10),
    REVISEDATE DATE,
    CONSTRAINT PK_TBLENTOSITEM PRIMARY KEY (OSITEMNO)
);

-- Oracle dictionary: only PK_TBLENTOSITEM unique index on OSITEMNO;
-- no foreign keys or additional indexes were found.

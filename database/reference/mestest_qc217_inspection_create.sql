-- PostgreSQL CREATE-only reference for QC217 (設備品檢管理設定).
-- Source: Oracle MESTEST.TBLQCINSPECTIONBASIS and TBLQCINSPECTIONDETAIL
-- Oracle source DDL is stored in:
--   mestest_qc217_tblqcinspectionbasis_source_ddl.sql
--   mestest_qc217_tblqcinspectiondetail_source_ddl.sql
-- This file is not a migration. Compare against the live YMES schema first;
-- add only missing columns/indexes through a forward migration. Do not run on Oracle.

CREATE TABLE IF NOT EXISTS tblqcinspectionbasis (
    opno              varchar(20)  NOT NULL,
    equipmentno       varchar(50)  NOT NULL,
    insptype          numeric(2,0) NOT NULL,
    inspstarttime     varchar(20)  NOT NULL DEFAULT 'N/A',
    periodmin         numeric(5,0),
    equipmentstate    numeric(2,0),
    lotcount          numeric(3,0),
    issuestate        numeric(1,0) NOT NULL DEFAULT 0,
    creator           varchar(10),
    createdate        timestamp without time zone,
    inspweek          numeric(2,0) NOT NULL DEFAULT -1,
    mailgroupno       varchar(255),
    ccmailgroupno     varchar(255),
    eqpdatatype       varchar(255) DEFAULT '0',
    datastamp         numeric(11,0) DEFAULT 0,
    reviser           varchar(10),
    revisedate        timestamp without time zone,
    CONSTRAINT pk_tblqcinspectionbasis
        PRIMARY KEY (opno, equipmentno, insptype, inspweek, inspstarttime)
);

CREATE TABLE IF NOT EXISTS tblqcinspectiondetail (
    opno              varchar(20)  NOT NULL,
    equipmentno       varchar(50)  NOT NULL,
    insptype          numeric(2,0) NOT NULL,
    qcitemno          varchar(25)  NOT NULL,
    qcitemtype        numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblqcinspectiondetail
        PRIMARY KEY (opno, equipmentno, insptype, qcitemno)
);

-- Oracle source has no foreign keys or non-PK indexes on either table.
-- Detail rows are intentionally shared by (OPNO, EQUIPMENTNO, INSPTYPE);
-- week/start-time are not part of the detail primary key.

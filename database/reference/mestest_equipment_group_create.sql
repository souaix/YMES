-- Source: Oracle MESTEST DBMS_METADATA DDL in the company MES source workspace.
-- Target: PostgreSQL, schema ymes. Empty schema objects only; no MES business data.
-- Oracle DATE maps to timestamp without time zone; NUMBER maps to numeric.
-- The source has no declared foreign keys for these tables.
-- Check existing YMES tables and compare columns before applying: IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tbleqpgroupbasis (
    equipmentgroup varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpgroupbasis PRIMARY KEY (equipmentgroup)
);
COMMENT ON TABLE ymes.tbleqpgroupbasis IS 'MES Oracle source: MESTEST.TBLEQPGROUPBASIS';

CREATE TABLE IF NOT EXISTS ymes.tbleqpgroupdetail (
    equipmentgroup varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpgroupdetail PRIMARY KEY (equipmentgroup, equipmentno)
);
COMMENT ON TABLE ymes.tbleqpgroupdetail IS 'MES Oracle source: MESTEST.TBLEQPGROUPDETAIL';

-- Add/delete also synchronize SMD area mapping when the group is assigned to a layout:
-- TBLSMDAREARELATION and TBLSMDSMTAREAEQPMAP DDL are not present in this source dump.
-- Retrieve their exact Oracle DDL and verify YMES presence before implementing that integration.
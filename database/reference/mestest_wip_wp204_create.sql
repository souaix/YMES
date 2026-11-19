-- PostgreSQL CREATE-only reference translated from Oracle MESTEST metadata.
-- Retrieved 2026-11-19. Reference only, not a migration.
-- Compare YMES live schema/migrations before creating anything.
-- Oracle storage/tablespace clauses are omitted. Preserve source nullability,
-- defaults and the one non-unique secondary index. No primary keys were found.
CREATE SCHEMA IF NOT EXISTS ymes;

-- Oracle source table: TBLEQPFAVORITE
CREATE TABLE IF NOT EXISTS ymes.tbleqpfavorite (
    hostid varchar(50) NOT NULL,
    pageno varchar(50) NOT NULL,
    equipmentno varchar(50)
);

-- Oracle source table: TBLWIPLOTGPEQUIPMENT
CREATE TABLE IF NOT EXISTS ymes.tblwiplotgpequipment (
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    equipmentno varchar(50) NOT NULL
);
CREATE INDEX IF NOT EXISTS i_tblwiplotgpequipment_01
    ON ymes.tblwiplotgpequipment (lotno, opno);

-- Oracle source table: TBLPRDSTAGESTEPSET
CREATE TABLE IF NOT EXISTS ymes.tblprdstagestepset (
    productno varchar(50) NOT NULL,
    stageno varchar(50),
    opno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL
);

-- Oracle source table: TBLPRDSTAGESTEPSETDETAIL
CREATE TABLE IF NOT EXISTS ymes.tblprdstagestepsetdetail (
    productno varchar(50) NOT NULL,
    stageno varchar(50),
    opno varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50)
);

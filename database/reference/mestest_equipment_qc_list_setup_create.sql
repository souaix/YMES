-- PostgreSQL structure translated from the Oracle DDL capture at
-- D:\iMES\CSiMES\.equipment-settings-ddl.tmp.sql (MESTEST.TBLEQPQCLISTSETUP).
-- Schema only; no MES business rows. No foreign key was declared in the source DDL.

CREATE TABLE IF NOT EXISTS ymes.tbleqpqclistsetup (
    equipmentno varchar(50) NOT NULL,
    qclistno varchar(100) NOT NULL,
    insptype numeric(22,0) NOT NULL,
    inspstarttime varchar(20) DEFAULT 'N/A' NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tbleqpqclistsetup PRIMARY KEY (equipmentno, qclistno, insptype, inspstarttime)
);

COMMENT ON TABLE ymes.tbleqpqclistsetup IS 'MES Oracle source: MESTEST.TBLEQPQCLISTSETUP';

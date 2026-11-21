-- PostgreSQL CREATE-only reference for MESTEST.TBLEMSEQUIPMENTREASONS.
-- Reconstructed from the 2026-11-21 read-only ALL_TAB_COLUMNS / ALL_CONSTRAINTS / ALL_INDEXES / ALL_TRIGGERS results.
-- DBMS_METADATA.GET_DDL returned ORA-03135; see mestest_es204_equipment_reasons_metadata.txt.
-- No primary key, unique index, foreign key, or trigger was visible; none is invented here.
-- This file is for home-side YMES schema comparison only. It was not executed on Oracle or PostgreSQL.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblemsequipmentreasons (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    reasonno varchar(50),
    reasonname varchar(100)
);

COMMENT ON TABLE ymes.tblemsequipmentreasons IS 'MES Oracle source: MESTEST.TBLEMSEQUIPMENTREASONS; dictionary-derived CREATE-only reference, no visible primary key';

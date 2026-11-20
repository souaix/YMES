CREATE SCHEMA IF NOT EXISTS ymes;

-- Source: Oracle MESTEST DDL/data dictionary, queried read-only on 2026-11-20.
-- Target: PostgreSQL schema YMES. CREATE-only reference; creates no MES business rows.
-- TBLSMDAREARELATION is not repeated here; its existing source/CREATE reference is
-- database/reference/mestest_equipment_group_smd_source_ddl.sql and
-- database/reference/mestest_equipment_group_smd_create.sql.
-- Oracle has no foreign keys for these WIP tables. TBLWIPAREAOPERATORLOG has no
-- primary key or indexes in Oracle; do not invent a unique key or index here.

CREATE TABLE IF NOT EXISTS ymes.tblwipareaoperatorstate (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    logincomment varchar(255),
    logintime timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblwipareaoperatorstate PRIMARY KEY (userno)
);
COMMENT ON TABLE ymes.tblwipareaoperatorstate IS 'MES Oracle source: MESTEST.TBLWIPAREAOPERATORSTATE';

CREATE TABLE IF NOT EXISTS ymes.tblwipareaoperatorlog (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    logintime timestamp without time zone NOT NULL,
    logouttime timestamp without time zone NOT NULL,
    exceptiontime numeric(18,0) NOT NULL,
    logincomment varchar(255),
    logoutcomment varchar(255)
);
COMMENT ON TABLE ymes.tblwipareaoperatorlog IS 'MES Oracle source: MESTEST.TBLWIPAREAOPERATORLOG';

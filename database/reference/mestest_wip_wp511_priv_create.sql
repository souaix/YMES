-- CREATE-only PostgreSQL reference for Oracle MESTEST.XX_TBLWAITPRIV.
-- Compare YMES live schema and migration history first. This is not a migration and does not copy data.
-- Source has GROUPNO primary key, no foreign key or trigger. Primary key creates the only index.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.xx_tblwaitpriv (
    groupno varchar(50) NOT NULL,
    go numeric(1,0) NOT NULL DEFAULT 0,
    jump_op numeric(1,0) NOT NULL DEFAULT 0,
    jump_main numeric(1,0) NOT NULL DEFAULT 0,
    jump_process_01 numeric(1,0) NOT NULL DEFAULT 0,
    jump_process_02 numeric(1,0) NOT NULL DEFAULT 0,
    jump_module numeric(1,0) NOT NULL DEFAULT 0,
    process_turning numeric(1,0) NOT NULL DEFAULT 0,
    inventory numeric(1,0) NOT NULL DEFAULT 0,
    scrap_inventory numeric(1,0) NOT NULL DEFAULT 0,
    split numeric(1,0) NOT NULL DEFAULT 0,
    merge numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(50) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblwaitpriv PRIMARY KEY (groupno)
);

COMMENT ON TABLE ymes.xx_tblwaitpriv IS 'MES Oracle source: MESTEST.XX_TBLWAITPRIV; group based wait disposition permissions';

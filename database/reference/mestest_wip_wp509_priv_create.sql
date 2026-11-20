-- CREATE-only PostgreSQL reference for Oracle MESTEST.XX_TBLQCHOLDPRIV.
-- Compare YMES live schema and migration history first; this is not a migration.
-- GROUPNO is the primary key and creates the only source index. No FK or trigger.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.xx_tblqcholdpriv (
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
    "merge" numeric(1,0) NOT NULL DEFAULT 0,
    back_disposition numeric(1,0) NOT NULL DEFAULT 0,
    qty_adjust numeric(1,0) NOT NULL DEFAULT 0,
    scrap numeric(1,0) NOT NULL DEFAULT 0,
    "return" numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(50) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblqcholdpriv PRIMARY KEY (groupno)
);

COMMENT ON TABLE ymes.xx_tblqcholdpriv IS 'MES Oracle source: MESTEST.XX_TBLQCHOLDPRIV; group based ERF lot disposition permissions';

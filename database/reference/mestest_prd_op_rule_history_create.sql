-- PostgreSQL CREATE only. Main table is covered by mestest_prd_parameter_setup_create.sql.

CREATE TABLE IF NOT EXISTS "YMES".xx_tblprdoprulelog_theil (
    productno varchar(50),
    productversion varchar(5),
    opno varchar(20),
    ruleno varchar(55),
    rulename varchar(50),
    rulescript varchar(3000),
    state numeric(1, 0),
    old_ruleno varchar(20),
    old_rulescript varchar(3000),
    creator varchar(20),
    createdate timestamp(0) without time zone
);

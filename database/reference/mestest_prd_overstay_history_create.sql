-- PostgreSQL CREATE only. Main table is covered by mestest_prd_gbom_create.sql.

CREATE TABLE IF NOT EXISTS "YMES".xx_tbloversetuplog_theil_bu1 (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    alarmtime numeric(6, 2),
    holdtime numeric(6, 2),
    alarmgroup varchar(20),
    update_user varchar(10) NOT NULL,
    updatedate timestamp(0) without time zone NOT NULL,
    remark varchar(2000),
    toopno varchar(20) NOT NULL,
    from_br varchar(20) NOT NULL,
    to_br varchar(20) NOT NULL,
    mclassno varchar(30),
    sclassno varchar(30),
    holditemno varchar(50)
);

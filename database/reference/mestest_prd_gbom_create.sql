-- PostgreSQL CREATE-only comparison for GBOM custom tables. Check existing YMES schema first.
CREATE TABLE IF NOT EXISTS "YMES".xx_tblprdprarkey_theil_bu1 (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    para_key varchar(20) NOT NULL,
    para_key_name varchar(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS "YMES".xx_tbloversetup_theil_bu1 (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    alarmtime numeric(6,2),
    holdtime numeric(6,2),
    alarmgroup varchar(20),
    update_user varchar(10),
    updatedate timestamp(0) without time zone,
    remark varchar(2000),
    toopno varchar(20) NOT NULL,
    from_br varchar(20) NOT NULL,
    to_br varchar(20) NOT NULL,
    mclassno varchar(30),
    sclassno varchar(30),
    holditemno varchar(50),
    CONSTRAINT pk_xx_tbloversetup_theil_bu1 PRIMARY KEY (productno, productversion, opno, toopno, from_br, to_br)
);

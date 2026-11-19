-- PostgreSQL CREATE only. Use the established YMES schema.
-- VIEWSAP_TECH remains an external lookup.

CREATE TABLE IF NOT EXISTS "YMES".xx_tblprdruntime_theil_bu1 (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    sequence numeric(4, 0) NOT NULL,
    stdunittime numeric(6, 3),
    stdunitemptime numeric(6, 3),
    stduniteqptime numeric(6, 3),
    updatedate timestamp(0) without time zone,
    update_user varchar(10),
    remark varchar(2000),
    customersname varchar(100),
    sap_ktext varchar(40),
    opname varchar(200),
    CONSTRAINT xx_tblprdruntime_theil_bu1_pk PRIMARY KEY (productno, productversion, opno, sequence)
);

CREATE TABLE IF NOT EXISTS "YMES".xx_tblprdruntime_log_theil_bu1 (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    sequence numeric(4, 0) NOT NULL,
    stdunittime numeric(6, 2),
    stdunitemptime numeric(6, 2),
    stduniteqptime numeric(6, 2),
    updatedate timestamp(0) without time zone,
    update_user varchar(10),
    remark varchar(2000),
    customersname varchar(50),
    sap_ktext varchar(40),
    opname varchar(200)
);

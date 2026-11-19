-- PostgreSQL CREATE-only comparison for PD108. Confirm the existing YMES table first.
CREATE TABLE IF NOT EXISTS "YMES".tblprdlabelmap (
    productno varchar(50) NOT NULL,
    labeltype varchar(50) NOT NULL,
    labelno varchar(50),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    customerno varchar(50) DEFAULT '*' NOT NULL,
    labelsource numeric(1,0) NOT NULL,
    opno varchar(50) NOT NULL,
    lastactionuserno varchar(10),
    lastactiondate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdlabelmap PRIMARY KEY (productno, customerno, labeltype, opno, labelsource)
);

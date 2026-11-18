-- PostgreSQL CREATE-only reference for PD102 Packing Set.
-- Compare against YMES live schema and migration history before applying changes.
-- The product quantity and PackOIPath columns reuse the existing product master.

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingaccessory (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    accessorybarcode varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdpackingaccessory PRIMARY KEY (productno, productversion, accessorybarcode)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingrulemap (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdpackingrulemap PRIMARY KEY (productno, productversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductlabelrulebasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    labeltype varchar(2) NOT NULL,
    printcount numeric(5,0) NOT NULL,
    labelpath varchar(100) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblprdproductlabelrulebasis PRIMARY KEY (productno, productversion, labeltype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductlabelruledetail (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    labeltype varchar(2) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    itemname varchar(50) NOT NULL,
    dataproperty numeric(1,0) NOT NULL,
    userdefine varchar(50),
    sourcetype varchar(50),
    searchstart numeric(2,0),
    searchend numeric(2,0),
    description varchar(255),
    separatesign varchar(1),
    CONSTRAINT pk_prdproductlabelruledetail PRIMARY KEY (productno, productversion, labeltype, itemname)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductlabelruledetail_m (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    labeltype varchar(2) NOT NULL,
    multiitemno numeric(2,0) NOT NULL,
    itemname varchar(50) NOT NULL,
    dataproperty numeric(1,0) NOT NULL,
    userdefine varchar(50),
    sourcetype varchar(50),
    searchstart numeric(2,0),
    searchend numeric(2,0),
    CONSTRAINT pk_productlabelruledetail_m PRIMARY KEY (productno, productversion, labeltype, itemname, multiitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductserialrulebasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    serialtype numeric(2,0) NOT NULL,
    totallength numeric(3,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS pk_prdproductserialrulebasis
    ON "YMES".tblprdproductserialrulebasis (productno, productversion, serialtype);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductserialruledetail (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    serialtype numeric(2,0) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    codestart numeric(2,0) NOT NULL,
    codeend numeric(2,0) NOT NULL,
    sourcetype varchar(50) NOT NULL,
    seriallength numeric(2,0) NOT NULL,
    searchstart numeric(2,0),
    searchend numeric(2,0),
    decimaltype numeric(1,0),
    userdefine varchar(50),
    description varchar(255),
    CONSTRAINT pk_prdproductserialruledetail PRIMARY KEY (productno, productversion, serialtype, itemno)
);

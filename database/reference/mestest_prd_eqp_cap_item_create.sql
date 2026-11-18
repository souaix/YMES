-- PostgreSQL CREATE-only reference. Check YMES live schema and migrations first.
-- TBLEMSEQPCAPBASIS is the shared EMS lookup table.
CREATE TABLE IF NOT EXISTS "YMES".tblemseqpcapbasis (
    capitem varchar(50) NOT NULL,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblemseqpcapbasis PRIMARY KEY (capitem)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdopcapitem (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    capitem varchar(50) NOT NULL,
    capvalue varchar(20) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    description varchar(255),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblprdopcapitem PRIMARY KEY (productno, productversion, opno, capitem)
);

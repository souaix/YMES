-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpsyscombomapbasis (
    customerno varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    comboseqno varchar(30) NOT NULL,
    status varchar(10) NOT NULL,
    shipmapoption varchar(20) NOT NULL,
    shipdetailoption varchar(20) NOT NULL,
    ocheck_mapafterco varchar(1) NOT NULL,
    ocheck_mapmatch varchar(1) NOT NULL,
    ocheck_goodqtymatch varchar(1) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tbl_cpsyscombomapbasis PRIMARY KEY (customerno, productno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpsyscombomapdetail (
    customerno varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    comboseqno varchar(30) NOT NULL,
    combomapsource varchar(15) NOT NULL,
    opno varchar(20) NOT NULL,
    CONSTRAINT pk_tbl_cpsyscombomapdetail PRIMARY KEY (customerno, productno, combomapsource, opno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpsyscombomaplog (
    customerno varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    comboseqno varchar(30) NOT NULL,
    status varchar(10) NOT NULL,
    shipmapoption varchar(20) NOT NULL,
    shipdetailoption varchar(20) NOT NULL,
    ocheck_mapafterco varchar(1) NOT NULL,
    ocheck_mapmatch varchar(1) NOT NULL,
    ocheck_goodqtymatch varchar(1) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tbl_cpsyscombomaplog PRIMARY KEY (customerno, productno, comboseqno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpsyscombomapdetaillog (
    customerno varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    comboseqno varchar(30) NOT NULL,
    combomapsource varchar(15) NOT NULL,
    opno varchar(20) NOT NULL,
    CONSTRAINT pk_tbl_cpsyscombomapdetaillog PRIMARY KEY (customerno, productno, comboseqno, combomapsource, opno)
);

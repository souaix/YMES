-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tblprdecnbasic (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    ecntype varchar(10) NOT NULL,
    ecnstate varchar(10) NOT NULL,
    sentype varchar(20),
    starttime timestamp(0) without time zone,
    endtime timestamp(0) without time zone,
    reviser varchar(10) NOT NULL,
    revisedate timestamp(0) without time zone NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    approver varchar(10),
    approvedate timestamp(0) without time zone,
    opgroupno varchar(20),
    datastamp numeric(11, 0),
    CONSTRAINT pk_tblprdecnbasic PRIMARY KEY (productno, productversion, ecnno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdecnbasicdetail (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    elementno varchar(50) NOT NULL,
    elementvalue varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    allownull numeric(1, 0),
    sequence numeric(2, 0),
    CONSTRAINT pk_tblprdecnbasicdetail PRIMARY KEY (productno, productversion, ecnno, elementno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdecnprocess (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    ecntype varchar(10) NOT NULL,
    ecnstate varchar(10) NOT NULL,
    sentype varchar(20),
    starttime timestamp(0) without time zone,
    endtime timestamp(0) without time zone,
    reviser varchar(10) NOT NULL,
    revisedate timestamp(0) without time zone NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    approver varchar(10),
    approvedate timestamp(0) without time zone,
    datastamp numeric(11, 0),
    CONSTRAINT pk_tblprdecnprocess PRIMARY KEY (productno, productversion, ecnno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdecnop (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    ecntype varchar(10) NOT NULL,
    ecnstate varchar(10) NOT NULL,
    sentype varchar(20),
    starttime timestamp(0) without time zone,
    endtime timestamp(0) without time zone,
    reviser varchar(10) NOT NULL,
    revisedate timestamp(0) without time zone NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    approver varchar(10),
    approvedate timestamp(0) without time zone,
    datastamp numeric(11, 0),
    CONSTRAINT pk_tblprdecnop PRIMARY KEY (productno, productversion, ecnno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdmoprocess (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    processno varchar(30) NOT NULL,
    processversion varchar(3) NOT NULL,
    motypeno varchar(5) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tblprdmoprocess PRIMARY KEY (productno, productversion, ecnno, processno, processversion, motypeno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdsenlotdetail (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    ecnno varchar(20) NOT NULL,
    sentype varchar(20) NOT NULL,
    lotno varchar(20) NOT NULL,
    CONSTRAINT pk_tblprdsenlotdetail PRIMARY KEY (productno, productversion, ecnno, lotno)
);

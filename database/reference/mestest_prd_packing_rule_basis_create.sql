-- PostgreSQL CREATE-only comparison for PD112 packing-rule tables. Check live schema first.

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingruleaccessory (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    accessorybarcode varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_prdpackingruleaccessory PRIMARY KEY (packingruleno, packingruleversion, accessorybarcode)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingrulebasis (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    curversion numeric(1,0) DEFAULT 0 NOT NULL,
    levelmax numeric(1,0) NOT NULL,
    l1_name varchar(50) NOT NULL,
    l1_qty numeric(12,4) NOT NULL,
    l1_soppath varchar(500),
    l2_name varchar(50),
    l2_qty numeric(12,4),
    l2_soppath varchar(500),
    l3_name varchar(50),
    l3_qty numeric(12,4),
    l3_soppath varchar(500),
    l4_name varchar(50),
    l4_qty numeric(12,4),
    l4_soppath varchar(500),
    l5_name varchar(50),
    l5_qty numeric(12,4),
    l5_soppath varchar(500),
    l6_name varchar(50),
    l6_qty numeric(12,4),
    l6_soppath varchar(500),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    l1_unitno varchar(30) NOT NULL,
    l2_unitno varchar(30),
    l3_unitno varchar(30),
    l4_unitno varchar(30),
    l5_unitno varchar(30),
    l6_unitno varchar(30),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    l1_num numeric(3,0),
    l2_num numeric(3,0),
    l3_num numeric(3,0),
    l4_num numeric(3,0),
    l5_num numeric(3,0),
    l6_num numeric(3,0),
    inlinepackingflag numeric(1,0) DEFAULT 1 NOT NULL,
    CONSTRAINT pk_tblprdpackingrulebasis PRIMARY KEY (packingruleno, packingruleversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackinglabelrulebasis (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    labeltype varchar(5) NOT NULL,
    printcount numeric(5,0) NOT NULL,
    labelpath varchar(100) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_prdpackinglabelrulebasis PRIMARY KEY (packingruleno, packingruleversion, labeltype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackinglabelruledetail (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    labeltype varchar(5) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    itemname varchar(50) NOT NULL,
    dataproperty numeric(1,0) NOT NULL,
    userdefine varchar(50),
    sourcetype varchar(50),
    searchstart numeric(2,0),
    searchend numeric(2,0),
    description varchar(255),
    separatesign varchar(1),
    sourcedata varchar(50),
    datascript text,
    CONSTRAINT pk_prdpackinglabelruledetail PRIMARY KEY (packingruleno, packingruleversion, labeltype, itemname)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackinglabelruledetail_m (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    labeltype varchar(5) NOT NULL,
    multiitemno numeric(2,0) NOT NULL,
    itemname varchar(50) NOT NULL,
    dataproperty numeric(1,0) NOT NULL,
    userdefine varchar(50),
    sourcetype varchar(50),
    searchstart numeric(2,0),
    searchend numeric(2,0),
    sourcedata varchar(50),
    datascript text,
    CONSTRAINT pk_packinglabelruledetail_m PRIMARY KEY (packingruleno, packingruleversion, labeltype, multiitemno, itemname)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingserialrulebasis (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
    serialtype numeric(2,0) NOT NULL,
    totallength numeric(3,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    description varchar(255),
    limitlength numeric(1,0) DEFAULT 1 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_prdpackingserialrulebasis PRIMARY KEY (packingruleno, packingruleversion, serialtype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdpackingserialruledetail (
    packingruleno varchar(50) NOT NULL,
    packingruleversion varchar(5) NOT NULL,
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
    serialcodeflag numeric(1,0) DEFAULT 0,
    getdata numeric(1,0) DEFAULT 0,
    functionname varchar(100),
    padleft numeric(1,0),
    CONSTRAINT pk_packingserialruledetail PRIMARY KEY (packingruleno, packingruleversion, serialtype, itemno)
);

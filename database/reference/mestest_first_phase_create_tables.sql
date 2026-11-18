-- PostgreSQL CREATE TABLE starter generated from the existing MES Oracle MESTEST data-dictionary extraction.
-- Scope: first-phase master-data tables, the EP303 WIP carrier-state dependency, and 3 MES system menu/function tables; no business rows are copied.
-- Target schema is YMES to align with the YMES API database. Review against the home-PC target before applying.
-- This remains a starter DDL, not the complete MES schema; other domains and permission/translation tables are not included.

CREATE SCHEMA IF NOT EXISTS "YMES";
SET search_path TO "YMES", public;

-- 基礎組織與外部夥伴
CREATE TABLE IF NOT EXISTS tblsmdfactorybasis (
    factoryno varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    fabcode varchar(10),
    factoryname varchar(50),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdfactorybasis PRIMARY KEY (factoryno)
);
COMMENT ON TABLE tblsmdfactorybasis IS 'MES master data; Oracle source: MESTEST.TBLSMDFACTORYBASIS';

CREATE TABLE IF NOT EXISTS tblsmdareabasis (
    areano varchar(20) NOT NULL,
    areaname varchar(50) NOT NULL,
    areatype numeric(1,0) NOT NULL,
    areaclass numeric(1,0) NOT NULL,
    baseareano varchar(20),
    factoryno varchar(20) NOT NULL,
    departmentno varchar(20),
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    areacompose numeric(1,0) NOT NULL,
    backgroundimage bytea,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareabasis PRIMARY KEY (areano)
);
COMMENT ON TABLE tblsmdareabasis IS 'MES master data; Oracle source: MESTEST.TBLSMDAREABASIS';
COMMENT ON COLUMN tblsmdareabasis.areano IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.areaname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.areatype IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.areaclass IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.baseareano IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.factoryno IS '廠別（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.departmentno IS '部門（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.areacompose IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.backgroundimage IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsmdareabasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsmdcomputerareabasis (
    computerno varchar(20) NOT NULL,
    factoryno varchar(20) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdcomputerareabasis PRIMARY KEY (computerno, factoryno)
);
COMMENT ON TABLE tblsmdcomputerareabasis IS 'MES master data; Oracle source: MESTEST.TBLSMDCOMPUTERAREABASIS';

CREATE TABLE IF NOT EXISTS tblsmdcomputerareadetail (
    computerno varchar(20) NOT NULL,
    factoryno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    CONSTRAINT pk_tblsmdcomputerareadetail PRIMARY KEY (computerno, factoryno, areano)
);
COMMENT ON TABLE tblsmdcomputerareadetail IS 'MES master data; Oracle source: MESTEST.TBLSMDCOMPUTERAREADETAIL';

CREATE TABLE IF NOT EXISTS tblsmdpdlinebasis (
    pdlineno varchar(50) NOT NULL,
    pdlinename varchar(50) NOT NULL,
    pdlinestatus numeric(1,0) DEFAULT 1 NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdpdlinebasis PRIMARY KEY (pdlineno)
);
COMMENT ON TABLE tblsmdpdlinebasis IS 'MES master data; Oracle source: MESTEST.TBLSMDPDLINEBASIS';

CREATE TABLE IF NOT EXISTS tblsmdpdlinepositionbasis (
    pdlineno varchar(50) NOT NULL,
    positionno varchar(50) NOT NULL,
    description varchar(50),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdpdlinepositionbasis PRIMARY KEY (pdlineno, positionno)
);
COMMENT ON TABLE tblsmdpdlinepositionbasis IS 'MES Oracle source: MESTEST.TBLSMDPDLINEPOSITIONBASIS';

CREATE TABLE IF NOT EXISTS tblsmdareashift (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    managerno varchar(10) NOT NULL,
    exceptiontime numeric(4,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareashift PRIMARY KEY (areano, departmentno, shiftno)
);
COMMENT ON TABLE tblsmdareashift IS 'MES Oracle source: MESTEST.TBLSMDAREASHIFT';

CREATE TABLE IF NOT EXISTS tblsmdareaoperator (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsmdareaoperator PRIMARY KEY (areano, departmentno, shiftno, userno)
);
COMMENT ON TABLE tblsmdareaoperator IS 'MES Oracle source: MESTEST.TBLSMDAREAOPERATOR';

CREATE TABLE IF NOT EXISTS tblusrdepartmentbasis (
    departmentno varchar(20) NOT NULL,
    departmentname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL,
    departmentsname varchar(60) NOT NULL,
    costcenter varchar(30),
    erpno varchar(50),
    controldtconect numeric(1,0) NOT NULL,
    dtconectqty numeric(3,0) NOT NULL,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblusrdepartmentbasis PRIMARY KEY (departmentno)
);
COMMENT ON TABLE tblusrdepartmentbasis IS 'MES master data; Oracle source: MESTEST.TBLUSRDEPARTMENTBASIS';
COMMENT ON COLUMN tblusrdepartmentbasis.departmentno IS '部門（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.departmentname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.departmentsname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.costcenter IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.controldtconect IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.dtconectqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrdepartmentbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblusrgroupbasis (
    groupno varchar(20) NOT NULL,
    groupname varchar(50) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    groupleader varchar(10),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblusrgroupbasis PRIMARY KEY (groupno)
);
COMMENT ON TABLE tblusrgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLUSRGROUPBASIS';
COMMENT ON COLUMN tblusrgroupbasis.groupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.groupname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.groupleader IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrgroupbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblusrpdlinebasis (
    pdlineno varchar(50) NOT NULL,
    pdlinename varchar(50),
    state numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL
);
COMMENT ON TABLE tblusrpdlinebasis IS 'MES master data; Oracle source: MESTEST.TBLUSRPDLINEBASIS';
COMMENT ON COLUMN tblusrpdlinebasis.pdlineno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.pdlinename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.state IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblusrpdlinebasis.issuestate IS '狀態（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblusrshiftbasis (
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    shiftname varchar(50) NOT NULL,
    createdate timestamp without time zone,
    creator varchar(10),
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    fromtime timestamp without time zone,
    totime timestamp without time zone,
    erpno varchar(50),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblusrshiftbasis PRIMARY KEY (departmentno, shiftno)
);
COMMENT ON TABLE tblusrshiftbasis IS 'MES master data; Oracle source: MESTEST.TBLUSRSHIFTBASIS';
COMMENT ON COLUMN tblusrshiftbasis.departmentno IS '部門（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.shiftno IS '班別（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.shiftname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.fromtime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.totime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusrshiftbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblusruserbasis (
    userno varchar(10) NOT NULL,
    username varchar(50) NOT NULL,
    userlevel numeric(1,0) NOT NULL,
    password varchar(50) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    usertype numeric(1,0) NOT NULL,
    departmentno varchar(20),
    customerno varchar(20),
    emailaddress varchar(100),
    changedate timestamp without time zone NOT NULL,
    shiftno varchar(20),
    mobileno varchar(50),
    resetpassword numeric(1,0) NOT NULL,
    erpno varchar(50),
    lineid varchar(50),
    wechatid varchar(50),
    watchid varchar(50),
    xx_ext varchar(10),
    CONSTRAINT pk_tblusruserbasis PRIMARY KEY (userno)
);
COMMENT ON TABLE tblusruserbasis IS 'MES master data; Oracle source: MESTEST.TBLUSRUSERBASIS';
COMMENT ON COLUMN tblusruserbasis.userno IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.username IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.userlevel IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.password IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.usertype IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.departmentno IS '部門（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.customerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.emailaddress IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.changedate IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.shiftno IS '班別（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.mobileno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.resetpassword IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.lineid IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.wechatid IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.watchid IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblusruserbasis.xx_ext IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblentcustomerbasis (
    customerno varchar(50) NOT NULL,
    customername varchar(100) NOT NULL,
    customersname varchar(100) NOT NULL,
    telno varchar(25),
    faxno varchar(25),
    www varchar(50),
    taxcode varchar(25),
    address varchar(255),
    director varchar(50),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0,
    address2 varchar(255),
    country varchar(50),
    status varchar(5),
    currency varchar(6),
    invtypeno varchar(1),
    invaddr varchar(255),
    invaddr2 varchar(255),
    invcustno varchar(20),
    invremark varchar(100),
    custtype varchar(1),
    artype varchar(1),
    freightterms varchar(20),
    paymentterms varchar(20),
    acctno varchar(50),
    deliveryterm varchar(50),
    forwarderassigned varchar(50),
    brokerassigned varchar(50),
    insurancecoverage varchar(50),
    reviser varchar(20),
    revisedate timestamp without time zone,
    customerename varchar(50),
    erpno varchar(50),
    datastamp numeric(11,0),
    sap_fiskn varchar(50),
    xx_name1 varchar(35),
    xx_name2 varchar(35),
    xx_rebarcode varchar(1),
    xx_supplydata varchar(1),
    CONSTRAINT pk_tblentcustomerbasis PRIMARY KEY (customerno)
);
COMMENT ON TABLE tblentcustomerbasis IS 'MES master data; Oracle source: MESTEST.TBLENTCUSTOMERBASIS';
COMMENT ON COLUMN tblentcustomerbasis.customerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.customername IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.customersname IS '歸屬客戶代碼_Cust. Number (Belong)';
COMMENT ON COLUMN tblentcustomerbasis.telno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.faxno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.www IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.taxcode IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.address IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.director IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.address2 IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.country IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.status IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.currency IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.invtypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.invaddr IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.invaddr2 IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.invcustno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.invremark IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.custtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.artype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.freightterms IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.paymentterms IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.acctno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.deliveryterm IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.forwarderassigned IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.brokerassigned IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.insurancecoverage IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.customerename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.sap_fiskn IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.xx_name1 IS '名稱 1_Name 1';
COMMENT ON COLUMN tblentcustomerbasis.xx_name2 IS '名稱 2_Name 2';
COMMENT ON COLUMN tblentcustomerbasis.xx_rebarcode IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblentcustomerbasis.xx_supplydata IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblentbrokerbasis (
    brokerno varchar(50) NOT NULL,
    contactorname varchar(50),
    telno varchar(25),
    faxno varchar(25),
    email varchar(255),
    address varchar(500),
    bankno varchar(20),
    issuestate numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentbrokerbasis PRIMARY KEY (brokerno)
);
COMMENT ON TABLE tblentbrokerbasis IS 'MES master data; Oracle source: MESTEST.TBLENTBROKERBASIS';
COMMENT ON COLUMN tblentbrokerbasis.brokerno IS 'Broker No';
COMMENT ON COLUMN tblentbrokerbasis.contactorname IS 'Contact person';
COMMENT ON COLUMN tblentbrokerbasis.telno IS 'Telephone';
COMMENT ON COLUMN tblentbrokerbasis.faxno IS 'Fax';
COMMENT ON COLUMN tblentbrokerbasis.email IS 'Email';
COMMENT ON COLUMN tblentbrokerbasis.address IS 'Address';
COMMENT ON COLUMN tblentbrokerbasis.bankno IS 'Bank No';
COMMENT ON COLUMN tblentbrokerbasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN tblentbrokerbasis.creator IS 'Creator';
COMMENT ON COLUMN tblentbrokerbasis.createdate IS 'Creation time';
COMMENT ON COLUMN tblentbrokerbasis.datastamp IS 'Optimistic concurrency stamp';
COMMENT ON COLUMN tblentbrokerbasis.reviser IS 'Reviser';
COMMENT ON COLUMN tblentbrokerbasis.revisedate IS 'Revision time';

CREATE TABLE IF NOT EXISTS tblentforwarderbasis (
    forwarderno varchar(50) NOT NULL,
    contactorname varchar(50),
    telno varchar(25),
    faxno varchar(25),
    email varchar(255),
    address varchar(500),
    brokerno varchar(50) NOT NULL,
    bankno varchar(20),
    issuestate numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    acno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentforwarderbasis PRIMARY KEY (forwarderno)
);
COMMENT ON TABLE tblentforwarderbasis IS 'MES master data; Oracle source: MESTEST.TBLENTFORWARDERBASIS';
COMMENT ON COLUMN tblentforwarderbasis.forwarderno IS 'Forwarder No';
COMMENT ON COLUMN tblentforwarderbasis.contactorname IS 'Contact person';
COMMENT ON COLUMN tblentforwarderbasis.telno IS 'Telephone';
COMMENT ON COLUMN tblentforwarderbasis.faxno IS 'Fax';
COMMENT ON COLUMN tblentforwarderbasis.email IS 'Email';
COMMENT ON COLUMN tblentforwarderbasis.address IS 'Address';
COMMENT ON COLUMN tblentforwarderbasis.brokerno IS 'Broker No';
COMMENT ON COLUMN tblentforwarderbasis.bankno IS 'Bank No';
COMMENT ON COLUMN tblentforwarderbasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN tblentforwarderbasis.creator IS 'Creator';
COMMENT ON COLUMN tblentforwarderbasis.createdate IS 'Creation time';
COMMENT ON COLUMN tblentforwarderbasis.acno IS 'AC No';
COMMENT ON COLUMN tblentforwarderbasis.datastamp IS 'Optimistic concurrency stamp';
COMMENT ON COLUMN tblentforwarderbasis.reviser IS 'Reviser';
COMMENT ON COLUMN tblentforwarderbasis.revisedate IS 'Revision time';

CREATE TABLE IF NOT EXISTS tblenttakerbasis (
    takerno varchar(50) NOT NULL,
    contactorname varchar(50),
    telno varchar(25),
    faxno varchar(25),
    email varchar(255),
    address varchar(500),
    bankno varchar(20),
    issuestate numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblenttakerbasis PRIMARY KEY (takerno)
);
COMMENT ON TABLE tblenttakerbasis IS 'MES master data; Oracle source: MESTEST.TBLENTTAKERBASIS';
COMMENT ON COLUMN tblenttakerbasis.takerno IS 'Taker No';
COMMENT ON COLUMN tblenttakerbasis.contactorname IS 'Contact person';
COMMENT ON COLUMN tblenttakerbasis.telno IS 'Telephone';
COMMENT ON COLUMN tblenttakerbasis.faxno IS 'Fax';
COMMENT ON COLUMN tblenttakerbasis.email IS 'Email';
COMMENT ON COLUMN tblenttakerbasis.address IS 'Address';
COMMENT ON COLUMN tblenttakerbasis.bankno IS 'Bank No';
COMMENT ON COLUMN tblenttakerbasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN tblenttakerbasis.creator IS 'Creator';
COMMENT ON COLUMN tblenttakerbasis.createdate IS 'Creation time';
COMMENT ON COLUMN tblenttakerbasis.datastamp IS 'Optimistic concurrency stamp';
COMMENT ON COLUMN tblenttakerbasis.reviser IS 'Reviser';
COMMENT ON COLUMN tblenttakerbasis.revisedate IS 'Revision time';

-- 系統共用主檔
CREATE TABLE IF NOT EXISTS tblsyselementbasis (
    elementno varchar(20) NOT NULL,
    elementname varchar(50),
    elementtype numeric(2,0),
    datatype numeric(1,0),
    transfertype numeric(1,0),
    valuetype numeric(1,0),
    validlist varchar(2000),
    defaultvalue varchar(255),
    formatstring varchar(4000),
    elementsequence numeric(2,0),
    unit numeric(1,0),
    description varchar(255),
    issuestate numeric(1,0),
    controlled numeric(1,0),
    inputtype numeric(1,0),
    allownull numeric(1,0),
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsyselementbasis PRIMARY KEY (elementno)
);
COMMENT ON TABLE tblsyselementbasis IS 'MES master data; Oracle source: MESTEST.TBLSYSELEMENTBASIS';
COMMENT ON COLUMN tblsyselementbasis.elementno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.elementname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.elementtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.datatype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.transfertype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.valuetype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.validlist IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.defaultvalue IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.formatstring IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.elementsequence IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.unit IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.controlled IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.inputtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.allownull IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsyselementbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysflowbasis (
    flowno varchar(30) NOT NULL,
    flowname varchar(50) NOT NULL,
    floworder numeric(2,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysflowbasis PRIMARY KEY (flowno)
);
COMMENT ON TABLE tblsysflowbasis IS 'MES master data; Oracle source: MESTEST.TBLSYSFLOWBASIS';
COMMENT ON COLUMN tblsysflowbasis.flowno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.flowname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.floworder IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflowbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysflownodebasis (
    flowno varchar(30) NOT NULL,
    nodeid varchar(50) NOT NULL,
    nodename varchar(50) NOT NULL,
    nodetype numeric(1,0) NOT NULL,
    functionno varchar(50) NOT NULL,
    groupno varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    opno varchar(20),
    CONSTRAINT pk_tblsysflownodebasis PRIMARY KEY (nodeid)
);
COMMENT ON TABLE tblsysflownodebasis IS 'MES master data; Oracle source: MESTEST.TBLSYSFLOWNODEBASIS';
COMMENT ON COLUMN tblsysflownodebasis.flowno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.nodeid IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.nodename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.nodetype IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.functionno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.groupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysflownodebasis.opno IS '作業／操作（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsyslotpropertybasis (
    propertyno varchar(20) NOT NULL,
    propertyname varchar(50),
    datatype numeric(1,0) NOT NULL,
    transfertype numeric(1,0) NOT NULL,
    tabletype numeric(1,0) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    updatetype numeric(1,0),
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsyslotpropertybasis PRIMARY KEY (propertyno)
);
COMMENT ON TABLE tblsyslotpropertybasis IS 'MES master data; Oracle source: MESTEST.TBLSYSLOTPROPERTYBASIS';
COMMENT ON COLUMN tblsyslotpropertybasis.propertyno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.propertyname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.datatype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.transfertype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.tabletype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.tablename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.fieldname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.updatetype IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsyslotpropertybasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysmopropertybasis (
    propertyno varchar(20) NOT NULL,
    propertyname varchar(50),
    datatype numeric(1,0) NOT NULL,
    transfertype numeric(1,0) NOT NULL,
    tabletype numeric(1,0) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    updatetype numeric(1,0),
    controlled numeric(1,0),
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmopropertybasis PRIMARY KEY (propertyno)
);
COMMENT ON TABLE tblsysmopropertybasis IS 'MES master data; Oracle source: MESTEST.TBLSYSMOPROPERTYBASIS';
COMMENT ON COLUMN tblsysmopropertybasis.propertyno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.propertyname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.datatype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.transfertype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.tabletype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.tablename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.fieldname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.updatetype IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.controlled IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmopropertybasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysropropertybasis (
    propertyno varchar(20) NOT NULL,
    propertyname varchar(50),
    datatype numeric(1,0) NOT NULL,
    transfertype numeric(1,0) NOT NULL,
    tabletype numeric(1,0) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    updatetype numeric(1,0),
    controlled numeric(1,0),
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysropropertybasis PRIMARY KEY (propertyno)
);
COMMENT ON TABLE tblsysropropertybasis IS 'MES master data; Oracle source: MESTEST.TBLSYSROPROPERTYBASIS';
COMMENT ON COLUMN tblsysropropertybasis.propertyno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.propertyname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.datatype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.transfertype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.tabletype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.tablename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.fieldname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.updatetype IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.controlled IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysropropertybasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysmotypebasis (
    motypeno numeric(2,0) NOT NULL,
    motypename varchar(20) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    motypecode varchar(10),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmotypebasis PRIMARY KEY (motypeno)
);
COMMENT ON TABLE tblsysmotypebasis IS 'MES master data; Oracle source: MESTEST.TBLSYSMOTYPEBASIS';
COMMENT ON COLUMN tblsysmotypebasis.motypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.motypename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.motypecode IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysmotypebasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysphasebasis (
    phaseno numeric(2,0) NOT NULL,
    phasename varchar(20) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysphasebasis PRIMARY KEY (phaseno)
);
COMMENT ON TABLE tblsysphasebasis IS 'MES master data; Oracle source: MESTEST.TBLSYSPHASEBASIS';
COMMENT ON COLUMN tblsysphasebasis.phaseno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.phasename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysphasebasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysserialrulebasis (
    serialtype varchar(50) NOT NULL,
    enable numeric(1,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    description varchar(255),
    serialtypeno varchar(50) NOT NULL,
    datastamp numeric(11,0),
    CONSTRAINT pk_tblsysserialrulebasis PRIMARY KEY (serialtype, serialtypeno)
);
COMMENT ON TABLE tblsysserialrulebasis IS 'MES master data; Oracle source: MESTEST.TBLSYSSERIALRULEBASIS';
COMMENT ON COLUMN tblsysserialrulebasis.serialtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.enable IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.serialtypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialrulebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysserialruledetail (
    serialtype varchar(50) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    sourcetype varchar(50) NOT NULL,
    seriallength numeric(2,0) NOT NULL,
    searchstart numeric(2,0) NOT NULL,
    decimaltype numeric(1,0),
    userdefine varchar(50),
    serialcodeflag numeric(1,0),
    description varchar(255),
    serialtypeno varchar(50) NOT NULL,
    getdata numeric(1,0),
    functionname varchar(100),
    padleft numeric(1,0),
    CONSTRAINT pk_tblsysserialruledetail PRIMARY KEY (serialtype, serialtypeno, itemno)
);
COMMENT ON TABLE tblsysserialruledetail IS 'MES master data; Oracle source: MESTEST.TBLSYSSERIALRULEDETAIL';
COMMENT ON COLUMN tblsysserialruledetail.serialtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.itemno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.sourcetype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.seriallength IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.searchstart IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.decimaltype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.userdefine IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.serialcodeflag IS '旗標（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.serialtypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.getdata IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.functionname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblsysserialruledetail.padleft IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysunitbasis (
    unitno varchar(30) NOT NULL,
    unittype varchar(30) NOT NULL,
    transferrate numeric(12,4),
    transferfunction varchar(50),
    systemunit varchar(30) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL,
    description varchar(255),
    erpno varchar(50),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysunitbasis PRIMARY KEY (unitno)
);
COMMENT ON TABLE tblsysunitbasis IS 'MES master data; Oracle source: MESTEST.TBLSYSUNITBASIS';
COMMENT ON COLUMN tblsysunitbasis.unitno IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.unittype IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.transferrate IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.transferfunction IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.systemunit IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysunitbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblsysyeardefinitionbasis (
    yearno numeric(4,0) NOT NULL,
    startdate timestamp without time zone NOT NULL,
    enddate timestamp without time zone NOT NULL,
    description varchar(255),
    issuestate numeric(1,0),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysyeardefinitionbasis PRIMARY KEY (yearno)
);
COMMENT ON TABLE tblsysyeardefinitionbasis IS 'MES master data; Oracle source: MESTEST.TBLSYSYEARDEFINITIONBASIS';
COMMENT ON COLUMN tblsysyeardefinitionbasis.yearno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.startdate IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.enddate IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblsysyeardefinitionbasis.revisedate IS '修改時間（推測，請依業務確認）';

-- 設備主檔
CREATE TABLE IF NOT EXISTS tbleqpequipmentbasis (
    equipmentno varchar(50) CONSTRAINT n_tbleqpequipmentbasis_01 NOT NULL,
    equipmenttype varchar(50) CONSTRAINT n_tbleqpequipmentbasis_02 NOT NULL,
    capacity numeric(8,0) DEFAULT -1,
    vendorno varchar(50) CONSTRAINT n_tbleqpequipmentbasis_03 NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tbleqpequipmentbasis_04 NOT NULL,
    engineergroupno varchar(20),
    assetno varchar(50),
    equipmentclass varchar(50),
    loadport numeric(2,0) DEFAULT 0,
    autoflag numeric(1,0),
    eacontroller varchar(200),
    eqprecipe numeric(1,0),
    qclistno varchar(50),
    maxtime numeric(6,2),
    fixeqptime numeric(6,2) DEFAULT 0,
    vareqptime numeric(6,2) DEFAULT 0,
    countopunitqty numeric(6,0) DEFAULT 1,
    counteqpunitqty numeric(6,0) DEFAULT 1,
    counter numeric(5,0),
    equipmentname varchar(255),
    erpno varchar(50),
    queueserver varchar(50),
    requestmsgqueue varchar(100),
    responsemsgqueue varchar(100),
    chamberflag numeric(1,0) DEFAULT 0,
    belongequipmentno varchar(50),
    equipmentclassno numeric(3,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    requestmsgqueue_mes varchar(100),
    responsemsgqueue_eap varchar(100),
    xx_keeperno varchar(50),
    CONSTRAINT pk_tbleqpequipmentbasis PRIMARY KEY (equipmentno)
);
COMMENT ON TABLE tbleqpequipmentbasis IS 'Structure converted from user-supplied Oracle MESTEST.TBLEQPEQUIPMENTBASIS DDL.';

CREATE TABLE IF NOT EXISTS tbleqpgroupbasis (
    equipmentgroup varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpgroupbasis PRIMARY KEY (equipmentgroup)
);
COMMENT ON TABLE tbleqpgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPGROUPBASIS';
COMMENT ON COLUMN tbleqpgroupbasis.equipmentgroup IS '設備群組編號；Oracle source: MESTEST.TBLEQPGROUPBASIS.EQUIPMENTGROUP';
COMMENT ON COLUMN tbleqpgroupbasis.description IS '群組說明；Oracle source: MESTEST.TBLEQPGROUPBASIS.DESCRIPTION';
COMMENT ON COLUMN tbleqpgroupbasis.creator IS '建立人員；Oracle source: MESTEST.TBLEQPGROUPBASIS.CREATOR';
COMMENT ON COLUMN tbleqpgroupbasis.createdate IS '建立日期時間；Oracle source: MESTEST.TBLEQPGROUPBASIS.CREATEDATE';
COMMENT ON COLUMN tbleqpgroupbasis.issuestate IS '簽核狀態；Oracle source: MESTEST.TBLEQPGROUPBASIS.ISSUESTATE';
COMMENT ON COLUMN tbleqpgroupbasis.datastamp IS '併發版本；Oracle source: MESTEST.TBLEQPGROUPBASIS.DATASTAMP';
COMMENT ON COLUMN tbleqpgroupbasis.reviser IS '修改人員；Oracle source: MESTEST.TBLEQPGROUPBASIS.REVISER';
COMMENT ON COLUMN tbleqpgroupbasis.revisedate IS '修改日期時間；Oracle source: MESTEST.TBLEQPGROUPBASIS.REVISEDATE';

CREATE TABLE IF NOT EXISTS tbleqpgroupdetail (
    equipmentgroup varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpgroupdetail PRIMARY KEY (equipmentgroup, equipmentno)
);
COMMENT ON TABLE tbleqpgroupdetail IS 'MES master data; Oracle source: MESTEST.TBLEQPGROUPDETAIL';
COMMENT ON COLUMN tbleqpgroupdetail.equipmentgroup IS '設備群組編號；Oracle source: MESTEST.TBLEQPGROUPDETAIL.EQUIPMENTGROUP';
COMMENT ON COLUMN tbleqpgroupdetail.equipmentno IS '設備編號；Oracle source: MESTEST.TBLEQPGROUPDETAIL.EQUIPMENTNO';

CREATE TABLE IF NOT EXISTS tbleqpengineergroupbasis (
    engineergroupno varchar(20) NOT NULL,
    engineergroupname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpengineergroupbasis PRIMARY KEY (engineergroupno)
);
COMMENT ON TABLE tbleqpengineergroupbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPENGINEERGROUPBASIS';
COMMENT ON COLUMN tbleqpengineergroupbasis.engineergroupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.engineergroupname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpengineergroupbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tbleqpstatebasis (
    equipmentstate numeric(2,0) NOT NULL,
    statetype numeric(2,0) DEFAULT 1 NOT NULL,
    statename varchar(50) NOT NULL,
    statecolor numeric(11,0) NOT NULL,
    description varchar(255),
    engineergroupno varchar(20),
    utilizationtype numeric(1,0) DEFAULT 1 NOT NULL,
    statecolor_hex varchar(15),
    CONSTRAINT pk_tbleqpstatebasis PRIMARY KEY (equipmentstate)
);
COMMENT ON TABLE tbleqpstatebasis IS 'MES master data; Oracle source: MESTEST.TBLEQPSTATEBASIS';
COMMENT ON COLUMN tbleqpstatebasis.equipmentstate IS '設備狀態編號（NUMBER(2,0)）';
COMMENT ON COLUMN tbleqpstatebasis.statetype IS '狀態類型 ValueList 索引（WinForms 標示 System/User Define）';
COMMENT ON COLUMN tbleqpstatebasis.statename IS '設備狀態名稱';
COMMENT ON COLUMN tbleqpstatebasis.statecolor IS '.NET Color.ToArgb() 有號整數';
COMMENT ON COLUMN tbleqpstatebasis.description IS '狀態說明';
COMMENT ON COLUMN tbleqpstatebasis.engineergroupno IS '設備工程群組編號（來源欄位；此表單未編輯）';
COMMENT ON COLUMN tbleqpstatebasis.utilizationtype IS '稼動類型（0=Off、1=On）';
COMMENT ON COLUMN tbleqpstatebasis.statecolor_hex IS 'RGB 十六進位色碼，不含 #；WinForms 確定時寫入';

CREATE TABLE IF NOT EXISTS tbleqpcarrierbasis (
    carrierno varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    carriertype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    factoryno varchar(20),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    getgroupno varchar(50),
    getansno varchar(50),
    maintenancetime timestamp without time zone,
    begroupmainten varchar(50),
    CONSTRAINT pk_tbleqpcarrierbasis PRIMARY KEY (carrierno)
);
COMMENT ON TABLE tbleqpcarrierbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPCARRIERBASIS';

CREATE TABLE IF NOT EXISTS tblwipcarrierstate (
    carrierno varchar(50) NOT NULL,
    carriercategory varchar(50) NOT NULL,
    carriertype varchar(50) NOT NULL,
    carrierstate numeric(1,0),
    description varchar(255),
    lotno varchar(50),
    CONSTRAINT pk_tblwipcarrierstate PRIMARY KEY (carrierno)
);
COMMENT ON TABLE tblwipcarrierstate IS 'MES master data dependency; Oracle source: MESTEST.TBLWIPCARRIERSTATE';

CREATE TABLE IF NOT EXISTS tbleqptoolbasis (
    toolno varchar(20) NOT NULL,
    toolname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqptoolbasis PRIMARY KEY (toolno)
);
COMMENT ON TABLE tbleqptoolbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPTOOLBASIS';
COMMENT ON COLUMN tbleqptoolbasis.toolno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.toolname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqptoolbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tbleqpstockerbasis (
    stockerno varchar(20) NOT NULL,
    factoryno varchar(20),
    vnumber numeric(2,0),
    hnumber numeric(2,0),
    locatortype numeric(1,0),
    contenttype numeric(1,0),
    qtylimit numeric(3,0),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpstockerbasis PRIMARY KEY (stockerno)
);
COMMENT ON TABLE tbleqpstockerbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPSTOCKERBASIS';
COMMENT ON COLUMN tbleqpstockerbasis.stockerno IS '貨架編號';
COMMENT ON COLUMN tbleqpstockerbasis.factoryno IS '工廠編號';
COMMENT ON COLUMN tbleqpstockerbasis.vnumber IS 'V軸';
COMMENT ON COLUMN tbleqpstockerbasis.hnumber IS 'H軸';
COMMENT ON COLUMN tbleqpstockerbasis.locatortype IS '貨架類型(0.大架位, 1. 小架位)';
COMMENT ON COLUMN tbleqpstockerbasis.contenttype IS '內容(0. CST, 1. Lot)';
COMMENT ON COLUMN tbleqpstockerbasis.qtylimit IS 'Locaor限制批量';
COMMENT ON COLUMN tbleqpstockerbasis.creator IS '建檔人';
COMMENT ON COLUMN tbleqpstockerbasis.createdate IS '建檔日期';
COMMENT ON COLUMN tbleqpstockerbasis.issuestate IS '簽核狀態';
COMMENT ON COLUMN tbleqpstockerbasis.reviser IS '維護人員';
COMMENT ON COLUMN tbleqpstockerbasis.revisedate IS '維護日期';

CREATE TABLE IF NOT EXISTS tbleqpaccessorybasis (
    accessoryno varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    vendorno varchar(50) NOT NULL,
    modelno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    assetno varchar(50),
    accessoryversion varchar(3),
    curversion numeric(1,0) DEFAULT 0 NOT NULL,
    accessorycategory varchar(50),
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    xx_para1 varchar(100), xx_para2 varchar(100), xx_para3 varchar(100), xx_para4 varchar(100),
    xx_para5 varchar(100), xx_para6 varchar(100), xx_para7 varchar(100), xx_para8 varchar(100),
    xx_para9 varchar(100), xx_para10 varchar(100), xx_para11 varchar(100), xx_para12 varchar(100),
    xx_para13 varchar(100), xx_para14 varchar(100), xx_para15 varchar(100), xx_para16 varchar(100),
    xx_para17 varchar(100), xx_para18 varchar(100), xx_para19 varchar(100), xx_para20 varchar(100),
    xx_para21 varchar(100), xx_para22 varchar(100), xx_para23 varchar(100), xx_para24 varchar(100),
    getansno varchar(50),
    ansstatus varchar(20),
    acc_file_path1 varchar(500),
    acc_file_path2 varchar(500),
    acc_file_path3 varchar(500),
    accessorystate numeric(2,0) DEFAULT 0,
    getgroupno varchar(20),
    maintenancetime timestamp without time zone,
    begroupmainten varchar(1),
    CONSTRAINT n_tbleqpaccessorybasis_06 CHECK (accessoryversion IS NOT NULL),
    CONSTRAINT pk_tbleqpaccessorybasis PRIMARY KEY (accessoryno, accessoryversion)
);
CREATE INDEX IF NOT EXISTS idx_eqpacc_getgroupno ON tbleqpaccessorybasis (getgroupno);

CREATE TABLE IF NOT EXISTS tbleqpaccstatebasis (
    accessorystate numeric(2,0) NOT NULL,
    statetype numeric(2,0) DEFAULT 1 NOT NULL,
    statename varchar(50) NOT NULL,
    statecolor numeric(11,0) NOT NULL,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpaccstatebasis PRIMARY KEY (accessorystate)
);
COMMENT ON TABLE tbleqpaccstatebasis IS 'MES master data; Oracle source: MESTEST.TBLEQPACCSTATEBASIS';
COMMENT ON COLUMN tbleqpaccstatebasis.accessorystate IS 'Accessory state code';
COMMENT ON COLUMN tbleqpaccstatebasis.statetype IS 'Accessory state type';
COMMENT ON COLUMN tbleqpaccstatebasis.statename IS 'Accessory state name';
COMMENT ON COLUMN tbleqpaccstatebasis.statecolor IS 'Accessory state color';
COMMENT ON COLUMN tbleqpaccstatebasis.description IS 'Description';
COMMENT ON COLUMN tbleqpaccstatebasis.datastamp IS 'Optimistic concurrency stamp';
COMMENT ON COLUMN tbleqpaccstatebasis.creator IS 'Creator user number';
COMMENT ON COLUMN tbleqpaccstatebasis.createdate IS 'Creation timestamp';
COMMENT ON COLUMN tbleqpaccstatebasis.reviser IS 'Last reviser user number';
COMMENT ON COLUMN tbleqpaccstatebasis.revisedate IS 'Last revision timestamp';
CREATE TABLE IF NOT EXISTS tbleqprecipebasis (
    recipegroup varchar(50) CONSTRAINT n_tbleqprecipebasis_01 NOT NULL,
    recipeversion numeric(2,0) DEFAULT 0 CONSTRAINT n_tbleqprecipebasis_02 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0,
    curversion numeric(1,0) DEFAULT 0 CONSTRAINT n_tbleqprecipebasis_03 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqprecipebasis PRIMARY KEY (recipegroup, recipeversion)
);
CREATE INDEX IF NOT EXISTS i_tbleqprecipebasis_01 ON tbleqprecipebasis (recipegroup, curversion);
COMMENT ON TABLE tbleqprecipebasis IS 'MES master data; Oracle source: MESTEST.TBLEQPRECIPEBASIS';
COMMENT ON COLUMN tbleqprecipebasis.recipegroup IS 'Recipe group identifier';
COMMENT ON COLUMN tbleqprecipebasis.recipeversion IS 'Recipe version';
COMMENT ON COLUMN tbleqprecipebasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN tbleqprecipebasis.curversion IS 'Current approved version flag';
COMMENT ON COLUMN tbleqprecipebasis.datastamp IS 'Optimistic concurrency stamp';

CREATE TABLE IF NOT EXISTS tbleqprecipedetail (
    recipegroup varchar(50) CONSTRAINT n_bleqprecipedetail_01 NOT NULL,
    recipeversion numeric(2,0) DEFAULT 0 CONSTRAINT n_bleqprecipedetail_02 NOT NULL,
    recipeno varchar(20) CONSTRAINT n_bleqprecipedetail_03 NOT NULL,
    recipevalue varchar(255) CONSTRAINT n_bleqprecipedetail_04 NOT NULL,
    sequence numeric(2,0) DEFAULT 0,
    description varchar(255),
    type numeric(1,0),
    stdvalue numeric(8,4),
    maximum numeric(8,4),
    minimum numeric(8,4),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqprecipedetail PRIMARY KEY (recipegroup, recipeversion, recipeno)
);
CREATE INDEX IF NOT EXISTS i_tbleqprecipedetail_01 ON tbleqprecipedetail (recipegroup, recipeno);
COMMENT ON TABLE tbleqprecipedetail IS 'MES master data; Oracle source: MESTEST.TBLEQPRECIPEDETAIL';
COMMENT ON COLUMN tbleqprecipedetail.type IS 'Recipe value type: -1 none, 0 value, 1 range';
COMMENT ON COLUMN tbleqprecipedetail.datastamp IS 'Optimistic concurrency stamp';

CREATE TABLE IF NOT EXISTS tbleqpparameterbasis (
    equipmentno varchar(50) CONSTRAINT n_tbleqpparameterbasis_01 NOT NULL,
    seqno numeric(2,0) CONSTRAINT n_tbleqpparameterbasis_02 NOT NULL,
    parameterno varchar(20) CONSTRAINT n_tbleqpparameterbasis_03 NOT NULL,
    spectype numeric(1,0) CONSTRAINT n_tbleqpparameterbasis_04 NOT NULL,
    parametervalue numeric(12,6),
    reviser varchar(10) CONSTRAINT n_tbleqpparameterbasis_05 NOT NULL,
    revisedate timestamp without time zone CONSTRAINT n_tbleqpparameterbasis_06 NOT NULL,
    eqpparamdate timestamp without time zone,
    CONSTRAINT pk_tbleqpparameterbasis PRIMARY KEY (equipmentno, parameterno)
);
COMMENT ON TABLE tbleqpparameterbasis IS 'Structure converted from user-supplied Oracle MESTEST.TBLEQPPARAMETERBASIS DDL.';

CREATE TABLE IF NOT EXISTS tbleqpqclistbasis (
    qclistno varchar(100) NOT NULL,
    qclistname varchar(100) NOT NULL,
    qctype numeric(2,0) NOT NULL,
    unvalidaction numeric(2,0),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    aftereventqc numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpqclistbasis PRIMARY KEY (qclistno)
);
COMMENT ON TABLE tbleqpqclistbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPQCLISTBASIS';
COMMENT ON COLUMN tbleqpqclistbasis.qclistno IS '設備點檢表編號';
COMMENT ON COLUMN tbleqpqclistbasis.qclistname IS '設備點檢表名稱';
COMMENT ON COLUMN tbleqpqclistbasis.qctype IS '點檢時機／類型 ValueList 值';
COMMENT ON COLUMN tbleqpqclistbasis.unvalidaction IS '點檢不合格時套用的設備狀態；-1 代表不動作';
COMMENT ON COLUMN tbleqpqclistbasis.description IS '點檢表說明';
COMMENT ON COLUMN tbleqpqclistbasis.creator IS '建立者使用者編號';
COMMENT ON COLUMN tbleqpqclistbasis.createdate IS '建立時間';
COMMENT ON COLUMN tbleqpqclistbasis.aftereventqc IS '事件後點檢旗標；UI 僅 QCType=0 時可設定';
COMMENT ON COLUMN tbleqpqclistbasis.datastamp IS '樂觀並行版本';
COMMENT ON COLUMN tbleqpqclistbasis.reviser IS '最後修改者使用者編號';
COMMENT ON COLUMN tbleqpqclistbasis.revisedate IS '最後修改時間';

CREATE TABLE IF NOT EXISTS tbleqpqclistdetail (
    qclistno varchar(100) NOT NULL,
    qcorder numeric(2,0) NOT NULL,
    qcitem varchar(1000) NOT NULL,
    qctype numeric(1,0) NOT NULL,
    stdvalue varchar(12),
    maxivalue varchar(12),
    minivalue varchar(12),
    inputdatacount numeric(2,0) DEFAULT 1 NOT NULL,
    inspectionmethod varchar(100),
    inspectionstandards varchar(100),
    CONSTRAINT pk_tbleqpqclistdetail PRIMARY KEY (qclistno, qcorder)
);
COMMENT ON TABLE tbleqpqclistdetail IS 'MES master data; Oracle source: MESTEST.TBLEQPQCLISTDETAIL';
COMMENT ON COLUMN tbleqpqclistdetail.qclistno IS '所屬點檢表編號';
COMMENT ON COLUMN tbleqpqclistdetail.qcorder IS '顯示與執行順序';
COMMENT ON COLUMN tbleqpqclistdetail.qcitem IS '點檢項目內容';
COMMENT ON COLUMN tbleqpqclistdetail.qctype IS '項目型態（0 標準值、1 範圍、2 訊息、3 輸入資料）';
COMMENT ON COLUMN tbleqpqclistdetail.stdvalue IS 'QCType=0 時的標準值';
COMMENT ON COLUMN tbleqpqclistdetail.maxivalue IS '範圍上限；* 代表無上限';
COMMENT ON COLUMN tbleqpqclistdetail.minivalue IS '範圍下限；* 代表無下限';
COMMENT ON COLUMN tbleqpqclistdetail.inputdatacount IS '輸入／取樣數量；WinForms 控制項範圍 1 到 10';
COMMENT ON COLUMN tbleqpqclistdetail.inspectionmethod IS '點檢方法';
COMMENT ON COLUMN tbleqpqclistdetail.inspectionstandards IS '點檢判定標準';

CREATE TABLE IF NOT EXISTS tbleqpmaskgroupbasis (
    maskgroup varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpmaskgroupbasis PRIMARY KEY (maskgroup)
);
COMMENT ON TABLE tbleqpmaskgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLEQPMASKGROUPBASIS';
COMMENT ON COLUMN tbleqpmaskgroupbasis.maskgroup IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tbleqpmaskgroupdetail (
    maskgroup varchar(50) NOT NULL,
    maskid varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpmaskgroupdetail PRIMARY KEY (maskgroup, maskid)
);
COMMENT ON TABLE tbleqpmaskgroupdetail IS 'MES master data; Oracle source: MESTEST.TBLEQPMASKGROUPDETAIL';
COMMENT ON COLUMN tbleqpmaskgroupdetail.maskgroup IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tbleqpmaskgroupdetail.maskid IS '識別碼／代碼（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tbleqplocatordetail (
    locatorno varchar(20) NOT NULL,
    contentno varchar(50) NOT NULL,
    contentqty numeric(12,4),
    opno varchar(20),
    areano varchar(20),
    creator varchar(10),
    createdate timestamp without time zone,
    lotno varchar(50),
    carrierno varchar(50),
    CONSTRAINT pk_tbleqplocatordetail PRIMARY KEY (locatorno, contentno)
);
COMMENT ON TABLE tbleqplocatordetail IS 'MES master data; Oracle source: MESTEST.TBLEQPLOCATORDETAIL';
COMMENT ON COLUMN tbleqplocatordetail.locatorno IS '儲位（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.contentno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.contentqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.areano IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.lotno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tbleqplocatordetail.carrierno IS '識別碼／代碼（推測，請依業務確認）';

-- 物料主檔
CREATE TABLE IF NOT EXISTS tblmtlmaterialbasis (
    materialno varchar(50) NOT NULL,
    materialtype varchar(50) NOT NULL,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    materialname varchar(300),
    materialspec varchar(300),
    putinplace numeric(1,0) DEFAULT 0,
    countway numeric(1,0) DEFAULT 0,
    checklotno numeric(1,0) DEFAULT 0,
    unittype varchar(30) NOT NULL,
    safeqty numeric(16,8) DEFAULT 0,
    unitno varchar(30) NOT NULL,
    ex_mtlbasis1 varchar(20),
    keymaterials numeric(1,0) DEFAULT 0,
    openlimittimeflag numeric(1,0),
    openlimittime numeric(6,2),
    resetopendate numeric(1,0),
    expiredateflag numeric(1,0),
    erpno varchar(50),
    user_defined01 varchar(255),
    user_defined02 varchar(255),
    user_defined03 varchar(255),
    user_defined04 varchar(255),
    user_defined05 varchar(255),
    user_defined06 varchar(255),
    user_defined07 varchar(255),
    user_defined08 varchar(255),
    user_defined09 varchar(255),
    user_defined10 varchar(255),
    user_defined11 numeric(23,8),
    user_defined12 numeric(23,8),
    user_defined13 numeric(23,8),
    user_defined14 numeric(23,8),
    user_defined15 numeric(23,8),
    user_defined16 numeric(23,8),
    user_defined17 numeric(23,8),
    user_defined18 numeric(23,8),
    user_defined19 numeric(23,8),
    user_defined20 numeric(23,8),
    user_defined21 timestamp without time zone,
    user_defined22 timestamp without time zone,
    user_defined23 timestamp without time zone,
    user_defined24 timestamp without time zone,
    user_defined25 timestamp without time zone,
    user_defined26 timestamp without time zone,
    user_defined27 timestamp without time zone,
    user_defined28 timestamp without time zone,
    user_defined29 timestamp without time zone,
    user_defined30 timestamp without time zone,
    user_defined31 varchar(255),
    user_defined32 varchar(255),
    user_defined33 numeric(23,8),
    user_defined34 numeric(23,8),
    graphno varchar(255),
    qccategory varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    sap_plnnr varchar(8),
    sap_numerator numeric,
    sap_werks varchar(4),
    sap_dsn_office varchar(3),
    sap_prod_hier varchar(18),
    sap_zz1_zbonded_plt varchar(1),
    sap_old_mat_no varchar(40),
    sap_matl_type varchar(4),
    sap_matl_desc_m varchar(40),
    sap_matl_desc_e varchar(40),
    sap_base_uom varchar(3),
    sap_shelf_life numeric(4,0),
    sap_zzkunnr varchar(10),
    sap_mfr_no varchar(10),
    sap_matl_grp_2 varchar(3),
    sap_denominatr numeric(5,0),
    sap_extmatlgrp varchar(18),
    sap_stor_conds varchar(2),
    sap_matl_group varchar(9),
    sap_pur_status varchar(2),
    sap_pur_status_c varchar(2),
    sap_item_cat varchar(4),
    sap_page_no varchar(3),
    sap_zztype varchar(50),
    sap_zzqdoc varchar(10),
    sap_zzdesign varchar(50),
    sap_class_m varchar(18),
    sap_class_b varchar(18),
    sap_profit_ctr varchar(10),
    sap_alt_unit varchar(3),
    sap_replentime numeric(3,0),
    sap_proc_type varchar(1),
    sap_spproctype varchar(2),
    sap_comp_scrap numeric(5,2),
    sap_assy_scrap numeric(5,2),
    sap_name1 varchar(35),
    sap_sapimportdatetime timestamp without time zone,
    sap_document varchar(22),
    sap_minlotsize numeric(13,3),
    sap_round_val numeric(13,3),
    sap_plnd_delry numeric(3,0),
    sap_mrp_ctrler varchar(3),
    sap_sapupdatedatetime timestamp without time zone,
    sap_frameqty varchar(30),
    sap_framesize varchar(30),
    sap_grossdie varchar(30),
    sap_thickness varchar(30),
    sap_technology varchar(30),
    sap_resolution varchar(30),
    framsize numeric,
    CONSTRAINT pk_tblmtlmaterialbasis PRIMARY KEY (materialno)
);
COMMENT ON TABLE tblmtlmaterialbasis IS 'MES master data; Oracle source: MESTEST.TBLMTLMATERIALBASIS';
CREATE TABLE IF NOT EXISTS tblmtlmaterialgroupbasis (
    groupno varchar(50) NOT NULL,
    materialtype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialgroupbasis PRIMARY KEY (groupno)
);
COMMENT ON TABLE tblmtlmaterialgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLMTLMATERIALGROUPBASIS';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.groupno IS '物料群組編號';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.materialtype IS '物料類別';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.description IS '說明';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.creator IS '建立人';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.createdate IS '建立日期';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.datastamp IS '並行版本戳記';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.reviser IS '修改人';
COMMENT ON COLUMN tblmtlmaterialgroupbasis.revisedate IS '修改日期';

CREATE TABLE IF NOT EXISTS tblmtlmaterialgroupdetail (
    groupno varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    CONSTRAINT pk_tblmtlmaterialgroupdetail PRIMARY KEY (groupno, materialno)
);
COMMENT ON TABLE tblmtlmaterialgroupdetail IS 'MES master data; Oracle source: MESTEST.TBLMTLMATERIALGROUPDETAIL';
COMMENT ON COLUMN tblmtlmaterialgroupdetail.groupno IS '物料群組編號';
COMMENT ON COLUMN tblmtlmaterialgroupdetail.materialno IS '物料編號';

CREATE TABLE IF NOT EXISTS tblmtldummywaferbasis (
    dummyno varchar(50) NOT NULL,
    dummytype varchar(50) NOT NULL,
    vendorno varchar(50) NOT NULL,
    factoryno varchar(20) NOT NULL,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtldummywaferbasis PRIMARY KEY (dummyno)
);
COMMENT ON TABLE tblmtldummywaferbasis IS 'MES master data; Oracle source: MESTEST.TBLMTLDUMMYWAFERBASIS';
COMMENT ON COLUMN tblmtldummywaferbasis.dummyno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.dummytype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.vendorno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.factoryno IS '廠別（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblmtldummywaferbasis.revisedate IS '修改時間（推測，請依業務確認）';

-- 產品與工程主檔
CREATE TABLE IF NOT EXISTS tblprdproductbasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    productname varchar(300) NOT NULL,
    producttype varchar(50) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(1000),
    curversion numeric(1,0) NOT NULL,
    unitno varchar(30),
    unittype varchar(30),
    specno varchar(120) NOT NULL,
    cartonqty numeric(8,0) NOT NULL,
    palletqty numeric(8,0) NOT NULL,
    packoipath varchar(500),
    boxqty numeric(8,4),
    productcode varchar(10),
    serialtypeno_lot varchar(50),
    serialtypeno_comp varchar(50),
    picturename varchar(50),
    lotstdqty numeric(12,4),
    erpno varchar(50),
    user_defined01 varchar(255),
    user_defined02 varchar(255),
    user_defined03 varchar(255),
    user_defined04 varchar(255),
    user_defined05 varchar(255),
    user_defined06 varchar(255),
    user_defined07 varchar(255),
    user_defined08 varchar(255),
    user_defined09 varchar(255),
    user_defined10 varchar(255),
    user_defined11 numeric(23,8),
    user_defined12 numeric(23,8),
    user_defined13 numeric(23,8),
    user_defined14 numeric(23,8),
    user_defined15 numeric(23,8),
    user_defined16 numeric(23,8),
    user_defined17 numeric(23,8),
    user_defined18 numeric(23,8),
    user_defined19 numeric(23,8),
    user_defined20 numeric(23,8),
    user_defined21 timestamp without time zone,
    user_defined22 timestamp without time zone,
    user_defined23 timestamp without time zone,
    user_defined24 timestamp without time zone,
    user_defined25 timestamp without time zone,
    user_defined26 timestamp without time zone,
    user_defined27 timestamp without time zone,
    user_defined28 timestamp without time zone,
    user_defined29 timestamp without time zone,
    user_defined30 timestamp without time zone,
    user_defined31 varchar(255),
    user_defined32 varchar(255),
    user_defined33 numeric(23,8),
    user_defined34 numeric(23,8),
    graphno varchar(255),
    qccategory varchar(50),
    itemspec varchar(255),
    customerno varchar(50),
    deviceno varchar(50),
    ecncontrol varchar(1),
    i_customerno varchar(50),
    batchuserno varchar(10),
    batchecnlock numeric(1,0),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    sap_plnal varchar(2),
    sap_ktext varchar(40),
    sap_aennr varchar(12),
    sap_datuv varchar(12),
    sap_upload varchar(1),
    sap_uploaddate timestamp without time zone,
    sap_bomaennr varchar(12),
    xx_sopspec varchar(50),
    xx_soprev varchar(40),
    sopspec varchar(50),
    soprev varchar(10),
    sap_werks varchar(10),
    sap_ieupload varchar(20),
    sap_ieuploaddate timestamp without time zone,
    sap_ieuploaduser varchar(10),
    sap_sapupdatedatetime timestamp without time zone,
    CONSTRAINT pk_tblprdproductbasis PRIMARY KEY (productno, productversion)
);
COMMENT ON TABLE tblprdproductbasis IS 'MES master data; Oracle source: MESTEST.TBLPRDPRODUCTBASIS';
COMMENT ON COLUMN tblprdproductbasis.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.productname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.producttype IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.curversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.unitno IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.unittype IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.specno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.cartonqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.palletqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.packoipath IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.boxqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.productcode IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.serialtypeno_lot IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.serialtypeno_comp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.picturename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.lotstdqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined01 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined02 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined03 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined04 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined05 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined06 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined07 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined08 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined09 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined10 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined11 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined12 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined13 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined14 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined15 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined16 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined17 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined18 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined19 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined20 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined21 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined22 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined23 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined24 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined25 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined26 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined27 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined28 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined29 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined30 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined31 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined32 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined33 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.user_defined34 IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.graphno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.qccategory IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.itemspec IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.customerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.deviceno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.ecncontrol IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.i_customerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.batchuserno IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.batchecnlock IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.sap_plnal IS '群組計數器';
COMMENT ON COLUMN tblprdproductbasis.sap_ktext IS '群組計數器說明_Routing Description';
COMMENT ON COLUMN tblprdproductbasis.sap_aennr IS '更改號碼_Change Number';
COMMENT ON COLUMN tblprdproductbasis.sap_datuv IS '生效日期_Valid-From Date';
COMMENT ON COLUMN tblprdproductbasis.sap_upload IS 'SAP途程上傳狀態';
COMMENT ON COLUMN tblprdproductbasis.sap_uploaddate IS '途程上傳日期';
COMMENT ON COLUMN tblprdproductbasis.sap_bomaennr IS 'BOM更改號碼_Change Number';
COMMENT ON COLUMN tblprdproductbasis.xx_sopspec IS 'SOPSPEC';
COMMENT ON COLUMN tblprdproductbasis.xx_soprev IS 'SOPSPEC';
COMMENT ON COLUMN tblprdproductbasis.sopspec IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.soprev IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.sap_werks IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductbasis.sap_ieupload IS 'SAP IE上傳狀態';
COMMENT ON COLUMN tblprdproductbasis.sap_ieuploaddate IS 'SAP IE上傳時間';
COMMENT ON COLUMN tblprdproductbasis.sap_ieuploaduser IS 'SAP IE上傳人員';
COMMENT ON COLUMN tblprdproductbasis.sap_sapupdatedatetime IS '日期／時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdrecipebasis (
    recipegroup varchar(50) NOT NULL,
    recipeversion numeric(2,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0),
    curversion numeric(1,0) NOT NULL,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdrecipebasis PRIMARY KEY (recipegroup, recipeversion)
);
COMMENT ON TABLE tblprdrecipebasis IS 'MES master data; Oracle source: MESTEST.TBLPRDRECIPEBASIS';
COMMENT ON COLUMN tblprdrecipebasis.recipegroup IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.recipeversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.curversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipebasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdrecipedetail (
    recipegroup varchar(50) NOT NULL,
    recipeversion numeric(2,0) NOT NULL,
    opno varchar(20) NOT NULL,
    recipeno varchar(20) NOT NULL,
    recipevalue varchar(255),
    sequence numeric(2,0),
    description varchar(255),
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdrecipedetail PRIMARY KEY (recipegroup, recipeversion, opno, recipeno)
);
COMMENT ON TABLE tblprdrecipedetail IS 'MES master data; Oracle source: MESTEST.TBLPRDRECIPEDETAIL';
COMMENT ON COLUMN tblprdrecipedetail.recipegroup IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.recipeversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.recipeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.recipevalue IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.sequence IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdrecipedetail.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdproductlabelrulebasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    labeltype varchar(2) NOT NULL,
    printcount numeric(5,0) NOT NULL,
    labelpath varchar(100) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    description varchar(255),
    datastamp numeric(11,0),
    CONSTRAINT pk_tblprdproductlabelrulebasis PRIMARY KEY (productno, productversion, labeltype)
);
COMMENT ON TABLE tblprdproductlabelrulebasis IS 'MES master data; Oracle source: MESTEST.TBLPRDPRODUCTLABELRULEBASIS';
COMMENT ON COLUMN tblprdproductlabelrulebasis.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.labeltype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.printcount IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.labelpath IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelrulebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdproductlabelruledetail (
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
    CONSTRAINT pk_tblprdproductlabelruledetail PRIMARY KEY (productno, productversion, labeltype, itemname)
);
COMMENT ON TABLE tblprdproductlabelruledetail IS 'MES master data; Oracle source: MESTEST.TBLPRDPRODUCTLABELRULEDETAIL';
COMMENT ON COLUMN tblprdproductlabelruledetail.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.labeltype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.itemno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.itemname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.dataproperty IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.userdefine IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.sourcetype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.searchstart IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.searchend IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductlabelruledetail.separatesign IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdproductserialrulebasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    serialtype numeric(2,0) NOT NULL,
    totallength numeric(3,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    description varchar(255),
    datastamp numeric(11,0)
);
COMMENT ON TABLE tblprdproductserialrulebasis IS 'MES master data; Oracle source: MESTEST.TBLPRDPRODUCTSERIALRULEBASIS';
COMMENT ON COLUMN tblprdproductserialrulebasis.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.serialtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.totallength IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialrulebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdproductserialruledetail (
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
    CONSTRAINT pk_tblprdproductserialruledetail PRIMARY KEY (productno, productversion, serialtype, itemno)
);
COMMENT ON TABLE tblprdproductserialruledetail IS 'MES master data; Oracle source: MESTEST.TBLPRDPRODUCTSERIALRULEDETAIL';
COMMENT ON COLUMN tblprdproductserialruledetail.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.serialtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.itemno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.codestart IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.codeend IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.sourcetype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.seriallength IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.searchstart IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.searchend IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.decimaltype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.userdefine IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdproductserialruledetail.description IS '描述（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdmodulebasis (
    moduleno varchar(45) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    modulename varchar(100) NOT NULL,
    moduletype varchar(50) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    curversion numeric(1,0) NOT NULL,
    unitno varchar(30),
    unittype varchar(30),
    psno varchar(50) NOT NULL,
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdmodulebasis PRIMARY KEY (moduleno, moduleversion)
);
COMMENT ON TABLE tblprdmodulebasis IS 'MES master data; Oracle source: MESTEST.TBLPRDMODULEBASIS';
COMMENT ON COLUMN tblprdmodulebasis.moduleno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.moduleversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.modulename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.moduletype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.curversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.unitno IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.unittype IS '單位（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.psno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdmodulebasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdprogrambasis (
    programno varchar(50) NOT NULL,
    programtype varchar(10),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    CONSTRAINT pk_tblprdprogrambasis PRIMARY KEY (programno)
);
COMMENT ON TABLE tblprdprogrambasis IS 'MES master data; Oracle source: MESTEST.TBLPRDPROGRAMBASIS';
COMMENT ON COLUMN tblprdprogrambasis.programno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprogrambasis.programtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprogrambasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprogrambasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprogrambasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprogrambasis.description IS '描述（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdengbasis (
    engno varchar(30) NOT NULL,
    engversion varchar(3) NOT NULL,
    engcode varchar(35) NOT NULL,
    psno varchar(50) NOT NULL,
    processno varchar(30),
    processversion varchar(3),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0),
    curversion numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblprdengbasis PRIMARY KEY (engno, engversion)
);
COMMENT ON TABLE tblprdengbasis IS 'MES master data; Oracle source: MESTEST.TBLPRDENGBASIS';
COMMENT ON COLUMN tblprdengbasis.engno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.engversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.engcode IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.psno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengbasis.curversion IS '版本（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdengrecipebasis (
    recipeno varchar(60) NOT NULL,
    recipeversion varchar(3) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    curversion numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    attachfile numeric(1,0),
    attachgraph numeric(1,0),
    CONSTRAINT pk_tblprdengrecipebasis PRIMARY KEY (recipeno, recipeversion)
);
COMMENT ON TABLE tblprdengrecipebasis IS 'MES master data; Oracle source: MESTEST.TBLPRDENGRECIPEBASIS';
COMMENT ON COLUMN tblprdengrecipebasis.recipeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.recipeversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.curversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.attachfile IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdengrecipebasis.attachgraph IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdesopbasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    subopno varchar(50) NOT NULL,
    positionno varchar(50) NOT NULL,
    sopfile varchar(255),
    pageno varchar(20),
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblprdesopbasis PRIMARY KEY (productno, productversion, opno, subopno, positionno)
);
COMMENT ON TABLE tblprdesopbasis IS 'MES master data; Oracle source: MESTEST.TBLPRDESOPBASIS';
COMMENT ON COLUMN tblprdesopbasis.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.subopno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.positionno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.sopfile IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.pageno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdesopbasis.createdate IS '建立時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprddefineruncardbasis (
    runcardno varchar(50) NOT NULL,
    runcardtype numeric(1,0) NOT NULL,
    formatfilename varchar(100) NOT NULL,
    printerno varchar(100),
    mainruncard numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    detailheaderstart numeric(5,0),
    detailheaderend numeric(5,0),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprddefineruncardbasis PRIMARY KEY (runcardno)
);
COMMENT ON TABLE tblprddefineruncardbasis IS 'MES master data; Oracle source: MESTEST.TBLPRDDEFINERUNCARDBASIS';
COMMENT ON COLUMN tblprddefineruncardbasis.runcardno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.runcardtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.formatfilename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.printerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.mainruncard IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.detailheaderstart IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.detailheaderend IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncardbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprddefineruncarddetail (
    runcardno varchar(50) NOT NULL,
    filefield varchar(50) NOT NULL,
    fieldvaluetype numeric(2,0) NOT NULL,
    datasource varchar(10),
    datasourcename varchar(50),
    fielddata varchar(50),
    direction numeric(2,0),
    startindex numeric(2,0),
    datalength numeric(2,0),
    defaultvalue varchar(100),
    datascript text,
    memo varchar(255),
    CONSTRAINT pk_tblprddefineruncarddetail PRIMARY KEY (runcardno, filefield)
);
COMMENT ON TABLE tblprddefineruncarddetail IS 'MES master data; Oracle source: MESTEST.TBLPRDDEFINERUNCARDDETAIL';
COMMENT ON COLUMN tblprddefineruncarddetail.runcardno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.filefield IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.fieldvaluetype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.datasource IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.datasourcename IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.fielddata IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.direction IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.startindex IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.datalength IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.defaultvalue IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.datascript IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprddefineruncarddetail.memo IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

-- 途程與作業站主檔
CREATE TABLE IF NOT EXISTS tblopbasis (
    opno varchar(20) NOT NULL,
    opname varchar(200) NOT NULL,
    optype varchar(20) NOT NULL,
    opclass numeric(1,0) NOT NULL,
    opshortname varchar(50),
    oporder numeric(6,0),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL,
    rulexmlstring text,
    psno varchar(50),
    printoutonruncard numeric(1,0),
    stdunitruntime numeric(6,2) NOT NULL,
    countopunitqty numeric(6,0) NOT NULL,
    stdqueuetime numeric(6,2) NOT NULL,
    partialmode numeric(1,0),
    materialoption numeric(1,0) NOT NULL,
    multioperatormode numeric(1,0),
    erpno varchar(50),
    osoption numeric(1,0) NOT NULL,
    erppdlineno varchar(20),
    spcinspabnormalholdlot numeric(1,0),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopbasis PRIMARY KEY (opno)
);
COMMENT ON TABLE tblopbasis IS 'MES master data; Oracle source: MESTEST.TBLOPBASIS';
COMMENT ON COLUMN tblopbasis.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.opname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.optype IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.opclass IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.opshortname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.oporder IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.rulexmlstring IS '規則（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.psno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.printoutonruncard IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.stdunitruntime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.countopunitqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.stdqueuetime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.partialmode IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.materialoption IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.multioperatormode IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.osoption IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.erppdlineno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.spcinspabnormalholdlot IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblopgroupbasis (
    opgroupno varchar(20) NOT NULL,
    opgroupname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    psno varchar(50),
    opgrouporder numeric(6,0) DEFAULT 0,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopgroupbasis PRIMARY KEY (opgroupno)
);
COMMENT ON TABLE tblopgroupbasis IS 'MES master data; Oracle source: MESTEST.TBLOPGROUPBASIS';
COMMENT ON COLUMN tblopgroupbasis.opgroupno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.opgroupname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.psno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.opgrouporder IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblopgroupdetail (
    opgroupno varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    oproleflag numeric(1,0) DEFAULT 0 NOT NULL,
    weightrate numeric(5,2) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopgroupdetail PRIMARY KEY (opgroupno, opno)
);
COMMENT ON TABLE tblopgroupdetail IS 'MES master data; Oracle source: MESTEST.TBLOPGROUPDETAIL';
COMMENT ON COLUMN tblopgroupdetail.opgroupno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.oproleflag IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.weightrate IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblopgroupdetail.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprsprocessbasis (
    processno varchar(30) NOT NULL,
    processclass numeric(2,0) NOT NULL,
    psno varchar(50) NOT NULL,
    processtype varchar(20) NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    nodexmlstring text,
    tuningprocess numeric(1,0),
    processversion varchar(3) NOT NULL,
    curversion numeric(1,0) NOT NULL,
    partialtype numeric(1,0),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprsprocessbasis PRIMARY KEY (processno, processversion)
);
COMMENT ON TABLE tblprsprocessbasis IS 'MES master data; Oracle source: MESTEST.TBLPRSPROCESSBASIS';
COMMENT ON COLUMN tblprsprocessbasis.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.processclass IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.psno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.processtype IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.nodexmlstring IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.tuningprocess IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.curversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.partialtype IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprsprocessbasis.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprsnodebasis (
    nodeid varchar(50) NOT NULL,
    nodeno varchar(50) NOT NULL,
    nodetype numeric(1,0) NOT NULL,
    processno varchar(30),
    groupno varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(50),
    processversion varchar(3),
    nodeversion varchar(3),
    stageno varchar(50),
    sequence numeric(4,0),
    sap_vornr varchar(4),
    sap_steus varchar(4),
    sap_infnr varchar(10),
    xx_matrix_code varchar(3000),
    xx_sawversion varchar(10),
    CONSTRAINT pk_tblprsnodebasis PRIMARY KEY (nodeid)
);
COMMENT ON TABLE tblprsnodebasis IS 'MES master data; Oracle source: MESTEST.TBLPRSNODEBASIS';
COMMENT ON COLUMN tblprsnodebasis.nodeid IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.nodeno IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.nodetype IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.groupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.nodeversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.stageno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.sequence IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.sap_vornr IS '作業號碼_Operation/Activity Number';
COMMENT ON COLUMN tblprsnodebasis.sap_steus IS '控制碼_Control Key';
COMMENT ON COLUMN tblprsnodebasis.sap_infnr IS '採購資訊記錄_Number of purchasing info record';
COMMENT ON COLUMN tblprsnodebasis.xx_matrix_code IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprsnodebasis.xx_sawversion IS '版本（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprsopapsdetail (
    processno varchar(50) NOT NULL,
    processversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    nodeid varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    stdworktimeqty numeric(8,0) NOT NULL,
    apsfixeqptime numeric(8,0) NOT NULL,
    apsvareqptime numeric(8,0) NOT NULL,
    schedule numeric(1,0) NOT NULL,
    batchop numeric(1,0) NOT NULL,
    batchmaxqty numeric(9,0) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprsopapsdetail PRIMARY KEY (nodeid, equipmentno)
);
COMMENT ON TABLE tblprsopapsdetail IS 'MES master data; Oracle source: MESTEST.TBLPRSOPAPSDETAIL';
COMMENT ON COLUMN tblprsopapsdetail.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.nodeid IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.equipmentno IS '設備（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.stdworktimeqty IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.apsfixeqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.apsvareqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.schedule IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.batchop IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.batchmaxqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprsopapsdetail.revisedate IS '修改時間（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdprsopapsbasedetail (
    processno varchar(50) NOT NULL,
    processversion varchar(5) NOT NULL,
    motypeno numeric(2,0) NOT NULL,
    opno varchar(25) NOT NULL,
    nodeid varchar(50) NOT NULL,
    equipmentgroup varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    apsfixeqptime numeric(8,0) NOT NULL,
    apsvareqptime numeric(8,0) NOT NULL,
    stdworktimeqty numeric(8,0) NOT NULL,
    transferqty numeric(8,0) NOT NULL,
    lotreartime numeric(8,0) NOT NULL,
    schedule numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblprdprsopapsbasedetail PRIMARY KEY (processno, processversion, motypeno, opno, nodeid, equipmentno)
);
COMMENT ON TABLE tblprdprsopapsbasedetail IS 'MES master data; Oracle source: MESTEST.TBLPRDPRSOPAPSBASEDETAIL';
COMMENT ON COLUMN tblprdprsopapsbasedetail.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.motypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.nodeid IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.equipmentgroup IS '設備（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.equipmentno IS '設備（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.apsfixeqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.apsvareqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.stdworktimeqty IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.transferqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.lotreartime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapsbasedetail.schedule IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblprdprsopapssetupdetail (
    productno varchar(30) NOT NULL,
    productversion varchar(5) NOT NULL,
    processno varchar(50) NOT NULL,
    processversion varchar(5) NOT NULL,
    motypeno numeric(2,0) NOT NULL,
    opno varchar(20) NOT NULL,
    nodeid varchar(50) NOT NULL,
    equipmentgroup varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    apsfixeqptime numeric(8,0) NOT NULL,
    apsvareqptime numeric(8,0) NOT NULL,
    stdworktimeqty numeric(8,0) NOT NULL,
    transferqty numeric(8,0) NOT NULL,
    lotreartime numeric(8,0) NOT NULL,
    schedule numeric(1,0) NOT NULL,
    batchop numeric(1,0) NOT NULL,
    batchmaxqty numeric(9,0) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdprsopapssetupdetail PRIMARY KEY (productno, productversion, motypeno, nodeid, equipmentno)
);
COMMENT ON TABLE tblprdprsopapssetupdetail IS 'MES master data; Oracle source: MESTEST.TBLPRDPRSOPAPSSETUPDETAIL';
COMMENT ON COLUMN tblprdprsopapssetupdetail.productno IS '產品（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.productversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.processno IS '途程／製程（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.processversion IS '版本（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.motypeno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.opno IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.nodeid IS '製程節點／作業站（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.equipmentgroup IS '設備（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.equipmentno IS '設備（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.apsfixeqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.apsvareqptime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.stdworktimeqty IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.transferqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.lotreartime IS '日期／時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.schedule IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.batchop IS '作業／操作（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.batchmaxqty IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblprdprsopapssetupdetail.revisedate IS '修改時間（推測，請依業務確認）';

-- 庫存位置主檔
CREATE TABLE IF NOT EXISTS tblinvinventorybasis (
    inventoryno varchar(20) NOT NULL,
    inventoryname varchar(50) NOT NULL,
    inventorytype numeric(2,0) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    engineergroupno varchar(20),
    inventoryclass numeric(1,0),
    factoryno varchar(20) NOT NULL,
    erpno varchar(50),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0),
    CONSTRAINT pk_tblinvinventorybasis PRIMARY KEY (inventoryno)
);
COMMENT ON TABLE tblinvinventorybasis IS 'MES master data; Oracle source: MESTEST.TBLINVINVENTORYBASIS';
COMMENT ON COLUMN tblinvinventorybasis.inventoryno IS '庫存（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.inventoryname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.inventorytype IS '庫存（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.engineergroupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.inventoryclass IS '庫存（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.factoryno IS '廠別（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.erpno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvinventorybasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblinvlocatorbasis (
    inventoryno varchar(20) NOT NULL,
    locatorno varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    capacity numeric(12,4) DEFAULT 0,
    locatorname varchar(50),
    erpno varchar(50),
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblinvlocatorbasis PRIMARY KEY (inventoryno, locatorno)
);
COMMENT ON TABLE tblinvlocatorbasis IS 'MES master data; Oracle source: MESTEST.TBLINVLOCATORBASIS';

CREATE TABLE IF NOT EXISTS tblinvstockerbasis (
    stockerno varchar(20) NOT NULL,
    factoryno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    vnumber numeric(2,0) NOT NULL,
    hnumber numeric(2,0) NOT NULL,
    capacityctrl numeric(1,0) NOT NULL,
    qtylimit numeric(3,0),
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    issuestate numeric(1,0) NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0),
    CONSTRAINT pk_tblinvstockerbasis PRIMARY KEY (stockerno)
);
COMMENT ON TABLE tblinvstockerbasis IS 'MES master data; Oracle source: MESTEST.TBLINVSTOCKERBASIS';
COMMENT ON COLUMN tblinvstockerbasis.stockerno IS '識別碼／代碼（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.factoryno IS '廠別（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.areano IS '區域（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.vnumber IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.hnumber IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.capacityctrl IS '來源欄位；尚未有來源註解（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.qtylimit IS '數量／計數（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.issuestate IS '狀態（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvstockerbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

CREATE TABLE IF NOT EXISTS tblinvengineergroupbasis (
    engineergroupno varchar(20) NOT NULL,
    engineergroupname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0),
    CONSTRAINT pk_tblinvengineergroupbasis PRIMARY KEY (engineergroupno)
);
COMMENT ON TABLE tblinvengineergroupbasis IS 'MES master data; Oracle source: MESTEST.TBLINVENGINEERGROUPBASIS';
COMMENT ON COLUMN tblinvengineergroupbasis.engineergroupno IS '群組（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.engineergroupname IS '名稱（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.description IS '描述（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.creator IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.createdate IS '建立時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.reviser IS '使用者／人員（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.revisedate IS '修改時間（推測，請依業務確認）';
COMMENT ON COLUMN tblinvengineergroupbasis.datastamp IS '來源欄位；尚未有來源註解（推測，請依業務確認）';

-- MES system menu and function catalog
-- Converted from Oracle MESTEST DBMS_METADATA.GET_DDL; column names, defaults, nullability, and primary keys are retained.
CREATE TABLE IF NOT EXISTS tblsysmodule (
    moduleno varchar(10) NOT NULL,
    moduleorder numeric(6,1) NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmodule PRIMARY KEY (moduleno)
);
COMMENT ON TABLE tblsysmodule IS 'MES system module catalog; Oracle source: MESTEST.TBLSYSMODULE';

CREATE TABLE IF NOT EXISTS tblsysmenuitem (
    moduleno varchar(50) NOT NULL,
    menuno varchar(50) NOT NULL,
    menuname varchar(50) NOT NULL,
    menutype numeric(1,0) NOT NULL,
    menuorder numeric(4,0) NOT NULL,
    parentmenuno varchar(50) NOT NULL,
    activemodule varchar(50) NOT NULL,
    CONSTRAINT pk_tblsysmenuitem PRIMARY KEY (moduleno, menuno)
);
COMMENT ON TABLE tblsysmenuitem IS 'MES system menu catalog; Oracle source: MESTEST.TBLSYSMENUITEM';

CREATE TABLE IF NOT EXISTS tblsysfunction (
    functionno varchar(50) NOT NULL,
    functionname varchar(50) NOT NULL,
    functionorder numeric(6,1) NOT NULL,
    moduleno varchar(10) NOT NULL,
    formname varchar(50),
    weburl varchar(100),
    creator varchar(10),
    createdate timestamp without time zone,
    privoption numeric(1,0) DEFAULT 0,
    issueoption numeric(1,0) DEFAULT 0,
    issuetable varchar(50),
    executionfile varchar(50),
    functionkey varchar(10),
    executionmode numeric(1,0) DEFAULT 0 NOT NULL,
    activemodule varchar(50),
    systemversion varchar(10),
    semi_wafer numeric(1,0),
    ic_cp_pkg numeric(1,0),
    ic_ft_pkg numeric(1,0),
    discrete_pkg numeric(1,0),
    led_pkg numeric(1,0),
    asm numeric(1,0),
    ic_ingot numeric(1,0),
    epi_wafer numeric(1,0) DEFAULT 0,
    accstatechangeinterface numeric(1,0) DEFAULT 0,
    ic_smt numeric(1,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysfunction PRIMARY KEY (functionno)
);
COMMENT ON TABLE tblsysfunction IS 'MES system function catalog; Oracle source: MESTEST.TBLSYSFUNCTION';

SET search_path TO public;

-- CREATE-only reference. Compare with the live schema before creating a migration.
-- ERP setting layout is reconstructed from columns and keys. Configure secrets outside this script.

CREATE TABLE "YMES".tblwipfutureoslog
   (	lotno varchar(50) NOT NULL,
	opno varchar(20) NOT NULL,
	description varchar(250),
	creator varchar(10) NOT NULL,
	createdate timestamp without time zone NOT NULL,
	moduleno varchar(50) NOT NULL,
	moduleversion varchar(3) NOT NULL,
	sysosno varchar(20),
	realosno varchar(20) NOT NULL
);

CREATE INDEX i_tblwipfutureoslog_01 ON "YMES".tblwipfutureoslog (lotno);

CREATE TABLE "YMES".tblsyserpsetting (
    erp_name varchar(50) NOT NULL,
    erp_org varchar(20) NOT NULL,
    erp_wsurl varchar(255) NOT NULL,
    erp_user varchar(20) NOT NULL,
    erp_pwd varchar(50),
    erp_lang varchar(10),
    erp_version varchar(10) NOT NULL,
    mesproductname varchar(50),
    mesproductver varchar(10),
    mesip varchar(20),
    mesid varchar(10),
    calledproductname varchar(50),
    calledproductver varchar(10),
    calledip varchar(20),
    calledid varchar(10),
    erp_entid varchar(5),
    calledsrvver varchar(5),
    eai_type numeric(2,0),
    queueserver varchar(50),
    requestmsgqueue varchar(100),
    responsemsgqueue varchar(100),
    eapmode numeric(1,0),
    queueuserid varchar(50),
    queuepassword varchar(50),
    activemq_type numeric(1,0),
    activeerp numeric(1,0) NOT NULL,
    datastamp numeric(11,0),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    requestmsgqueue_mes varchar(100),
    responsemsgqueue_eap varchar(100),
    CONSTRAINT pk_tblsyserpsetting PRIMARY KEY (erp_name)
);

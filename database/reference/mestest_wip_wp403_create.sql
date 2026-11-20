-- CREATE-only reference. Compare with live schema before creating a migration.

CREATE TABLE "YMES".tblwipcont_resource
   (	lotno varchar(50) NOT NULL,
	mono varchar(50) NOT NULL,
	baselotno varchar(50) NOT NULL,
	opno varchar(20) NOT NULL,
	loggroupserial varchar(50) NOT NULL,
	resclass numeric(2,0) NOT NULL,
	restype varchar(50) NOT NULL,
	resitem varchar(50) NOT NULL,
	resvalue numeric(10,2) NOT NULL,
	stdvalue numeric(10,2) NOT NULL,
	inputqty numeric(12,4) NOT NULL,
	userno varchar(10) NOT NULL,
	eventtime timestamp without time zone NOT NULL,
	erpflag numeric(1,0),
	workreportid varchar(30),
	 CONSTRAINT pk_tblwipcont_resource PRIMARY KEY (loggroupserial, resitem, eventtime, userno)
);

CREATE TABLE "YMES".tblwiposdetail_component
   (	osno varchar(20) NOT NULL,
	lotno varchar(50) NOT NULL,
	componentno varchar(50) NOT NULL,
	goodqty numeric(12,4),
	scrapqty numeric(12,4),
	unitno varchar(30),
	status numeric(1,0) DEFAULT 0 NOT NULL,
	 CONSTRAINT pk_tblwiposdetail_component PRIMARY KEY (osno, lotno, componentno)
);

CREATE INDEX i_tblwipcont_resource_01 ON "YMES".tblwipcont_resource (loggroupserial);

CREATE INDEX i_tblwipcont_resource_02 ON "YMES".tblwipcont_resource (lotno);

CREATE INDEX i_tblwipcont_resource_03 ON "YMES".tblwipcont_resource (lotno, resitem);

CREATE INDEX i_tblwipcont_resource_04 ON "YMES".tblwipcont_resource (eventtime);

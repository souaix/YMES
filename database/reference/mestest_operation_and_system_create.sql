-- PostgreSQL reference CREATE SQL converted from Oracle MESTEST source DDL queried on 2026-11-13.

-- Review existing "YMES" tables before applying. IF NOT EXISTS does not reconcile an existing table.

-- This file contains empty table structures only; it does not export business records.

CREATE TABLE IF NOT EXISTS "YMES".tblopbasis (
opno varchar(20) CONSTRAINT n_tblopbasis_01 NOT NULL ,
	opname varchar(200) CONSTRAINT n_tblopbasis_02 NOT NULL ,
	optype varchar(20) CONSTRAINT n_tblopbasis_03 NOT NULL ,
	opclass numeric(1,0) CONSTRAINT n_tblopbasis_04 NOT NULL ,
	opshortname varchar(200),
	oporder numeric(6,0) DEFAULT 0,
	description varchar(255),
	creator varchar(10),
	createdate timestamp without time zone,
	issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblopbasis_05 NOT NULL ,
	rulexmlstring LONG,
	psno varchar(50),
	printoutonruncard numeric(1,0),
	stdunitruntime numeric(6,2) DEFAULT 0 CONSTRAINT n_tblopbasis_06 NOT NULL ,
	countopunitqty numeric(6,0) DEFAULT 1 CONSTRAINT n_tblopbasis_07 NOT NULL ,
	stdqueuetime numeric(6,2) DEFAULT 0 CONSTRAINT n_tblopbasis_08 NOT NULL ,
	partialmode numeric(1,0),
	materialoption numeric(1,0) DEFAULT 0 CONSTRAINT n_tblopbasis_09 NOT NULL ,
	multioperatormode numeric(1,0) DEFAULT 0,
	erpno varchar(50),
	osoption numeric(1,0) DEFAULT '0' NOT NULL ,
	erppdlineno varchar(20),
	spcinspabnormalholdlot numeric(1,0) DEFAULT 1,
	datastamp numeric(11,0) DEFAULT 0,
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblopbasis PRIMARY KEY (opno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopgroupdetail (
opgroupno varchar(20) CONSTRAINT n_tblopgroupdetail_01 NOT NULL ,
	opno varchar(20) CONSTRAINT n_tblopgroupdetail_02 NOT NULL ,
	oproleflag numeric(1,0) DEFAULT 0 CONSTRAINT n_tblopgroupdetail_03 NOT NULL ,
	weightrate numeric(5,2) DEFAULT 0 CONSTRAINT n_tblopgroupdetail_04 NOT NULL ,
	datastamp numeric(11,0) DEFAULT 0,
	creator varchar(10),
	createdate timestamp without time zone,
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblopgroupdetail PRIMARY KEY (opgroupno, opno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopabnormal (
opno varchar(20) CONSTRAINT n_tblopabnormal_01 NOT NULL ,
	abnormalno varchar(20) CONSTRAINT n_tblopabnormal_02 NOT NULL ,
	 CONSTRAINT pk_tblopabnormal PRIMARY KEY (opno, abnormalno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbloperror (
opno varchar(20) CONSTRAINT n_tbloperror_01 NOT NULL ,
	errorno varchar(20) CONSTRAINT n_tbloperror_02 NOT NULL ,
	 CONSTRAINT pk_tbloperror PRIMARY KEY (opno, errorno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopproperty (
opno varchar(20) CONSTRAINT n_tblopproperty_01 NOT NULL ,
	propertyno varchar(20) CONSTRAINT n_tblopproperty_02 NOT NULL ,
	defaultvalue varchar(255),
	propertysequence numeric(2,0),
	description varchar(255),
	issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblopproperty_03 NOT NULL ,
	datastamp numeric(11,0) DEFAULT 0,
	creator varchar(10),
	createdate timestamp without time zone,
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblopproperty PRIMARY KEY (opno, propertyno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopbusinessrulerelation (
opno varchar(20) CONSTRAINT n_tblopbusinessrulerelation_01 NOT NULL ,
	fromnode varchar(20) CONSTRAINT n_tblopbusinessrulerelation_02 NOT NULL ,
	tonode varchar(20) CONSTRAINT n_tblopbusinessrulerelation_03 NOT NULL ,
	linkname varchar(20) CONSTRAINT n_tblopbusinessrulerelation_04 NOT NULL ,
	phaseno numeric(2,0) CONSTRAINT n_tblopbusinessrulerelation_05 NOT NULL ,
	 CONSTRAINT pk_tblopbusinessrulerelation PRIMARY KEY (opno, fromnode, tonode, linkname)
);

CREATE TABLE IF NOT EXISTS "YMES".tbloparea (
opno varchar(20) CONSTRAINT n_tbloparea_01 NOT NULL ,
	areano varchar(20) CONSTRAINT n_tbloparea_02 NOT NULL ,
	defaultarea numeric(1,0) DEFAULT 0,
	description varchar(255),
	 CONSTRAINT pk_tbloparea PRIMARY KEY (opno, areano)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopequipmenttype (
opno varchar(20) CONSTRAINT n_tblopequipmenttype_01 NOT NULL ,
	equipmenttype varchar(50) CONSTRAINT n_tblopequipmenttype_02 NOT NULL ,
	description varchar(255),
	 CONSTRAINT pk_tblopequipmenttype PRIMARY KEY (opno, equipmenttype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopattrib (
opno varchar(20) CONSTRAINT n_tblopattrib_01 NOT NULL ,
	attribtype numeric(1,0) CONSTRAINT n_tblopattrib_02 NOT NULL ,
	attribno varchar(20) CONSTRAINT n_tblopattrib_03 NOT NULL ,
	attribsource numeric(1,0) CONSTRAINT n_tblopattrib_04 NOT NULL ,
	attribphase numeric(2,0) CONSTRAINT n_tblopattrib_05 NOT NULL ,
	attribsequence numeric(2,0) CONSTRAINT n_tblopattrib_06 NOT NULL ,
	attribvalue varchar(255) DEFAULT ' ',
	attribscript varchar(3000) DEFAULT ' ',
	savetolotpropertyno varchar(20),
	qcitemno varchar(25),
	 CONSTRAINT pk_tblopattrib PRIMARY KEY (opno, attribno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbloprule (
opno varchar(20) CONSTRAINT n_tbloprule_01 NOT NULL ,
	ruleno varchar(20) CONSTRAINT n_tbloprule_02 NOT NULL ,
	rulephase numeric(2,0) CONSTRAINT n_tbloprule_03 NOT NULL ,
	rulescript varchar(3000) CONSTRAINT n_tbloprule_04 NOT NULL ,
	rulesequence numeric(2,0) DEFAULT 0,
	ruletype numeric(1,0) DEFAULT 0 CONSTRAINT n_tbloprule_05 NOT NULL ,
	ruleclass numeric(1,0) DEFAULT 0 NOT NULL ,
	rulemode numeric(1,0) DEFAULT 0,
	rulemodeqty numeric(2,0),
	 CONSTRAINT pk_tbloprule PRIMARY KEY (opno, ruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpoprule (
opno varchar(20) CONSTRAINT n_tbl_cpoprule_01 NOT NULL ,
	ruleno varchar(20) CONSTRAINT n_tbl_cpoprule_02 NOT NULL ,
	ruletype numeric(1,0) DEFAULT 0 NOT NULL ,
	definetype numeric(1,0) NOT NULL ,
	rulesource varchar(50),
	rulename varchar(50),
	rulescript varchar(3000),
	description varchar(255),
	holddispgroup varchar(50),
	sendmail numeric(1,0) DEFAULT 0,
	 CONSTRAINT pk_tbl_cpoprule PRIMARY KEY (opno, ruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpoprulecondition (
opno varchar(20) CONSTRAINT n_tbl_cpoprulecondition_01 NOT NULL ,
	ruleno varchar(20) CONSTRAINT n_tbl_cpoprulecondition_02 NOT NULL ,
	defruleid varchar(50) CONSTRAINT n_tbl_cpoprulecondition_03 NOT NULL ,
	itemno numeric(2,0) CONSTRAINT n_tbl_cpoprulecondition_04 NOT NULL ,
	condvalue varchar(55) NOT NULL ,
	 CONSTRAINT pk_tbl_cpoprulecondition PRIMARY KEY (opno, ruleno, defruleid, itemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_ftoprule (
opno varchar(20) CONSTRAINT n_tbl_ftoprule_01 NOT NULL ,
	ruleno varchar(20) CONSTRAINT n_tbl_ftoprule_02 NOT NULL ,
	ruletype numeric(1,0) DEFAULT 0 NOT NULL ,
	definetype numeric(1,0) NOT NULL ,
	rulesource varchar(50),
	rulename varchar(50),
	rulescript varchar(3000),
	description varchar(255),
	holddispgroup varchar(50),
	sendmail numeric(1,0) DEFAULT 0,
	 CONSTRAINT pk_tbl_ftoprule PRIMARY KEY (opno, ruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_ftoprulecondition (
opno varchar(20) CONSTRAINT n_tbl_ftoprulecondition_01 NOT NULL ,
	ruleno varchar(20) CONSTRAINT n_tbl_ftoprulecondition_02 NOT NULL ,
	defruleid varchar(50) CONSTRAINT n_tbl_ftoprulecondition_03 NOT NULL ,
	itemno numeric(2,0) CONSTRAINT n_tbl_ftoprulecondition_04 NOT NULL ,
	condvalue varchar(55) NOT NULL ,
	 CONSTRAINT pk_tbl_ftoprulecondition PRIMARY KEY (opno, ruleno, defruleid, itemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopeqprecipe (
opno varchar(20) CONSTRAINT n_tblopeqprecipe_01 NOT NULL ,
	equipmentno varchar(50) CONSTRAINT n_tblopeqprecipe_02 NOT NULL ,
	recipegroup varchar(50),
	 CONSTRAINT pk_tblopeqprecipe PRIMARY KEY (opno, equipmentno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopqcitem (
opno varchar(20) CONSTRAINT n_tblopqcitem_01 NOT NULL ,
	qcitemno varchar(25) CONSTRAINT n_tblopqcitem_02 NOT NULL ,
	qcitemtype numeric(1,0) CONSTRAINT n_tblopqcitem_03 NOT NULL ,
	inline numeric(1,0) DEFAULT 1 CONSTRAINT n_tblopqcitem_04 NOT NULL ,
	executeorder numeric(2,0) DEFAULT 0 CONSTRAINT n_tblopqcitem_05 NOT NULL ,
	insptype_fai numeric(1,0),
	insptype_ipqc numeric(1,0),
	insptype_pqc numeric(1,0),
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblopqcitem PRIMARY KEY (opno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbloprulexml_clob (
opno varchar(20) CONSTRAINT n_tbloprulexml_clob_01 NOT NULL ,
	rulexmlstring text,
	 CONSTRAINT pk_tbloprulexml_clob PRIMARY KEY (opno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblopsubopbasis (
opno varchar(20) CONSTRAINT n_tblopsubopbasis_01 NOT NULL ,
	subopsequence numeric(4,0) CONSTRAINT n_tblopsubopbasis_02 NOT NULL ,
	subopno varchar(20) CONSTRAINT n_tblopsubopbasis_03 NOT NULL ,
	subopname varchar(100) CONSTRAINT n_tblopsubopbasis_04 NOT NULL ,
	creator varchar(100),
	createdate timestamp without time zone,
	datastamp numeric(11,0) DEFAULT 0,
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblopsubopbasis PRIMARY KEY (opno, subopsequence)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsysmodule (
moduleno varchar(10) CONSTRAINT n_tblsysmodule_01 NOT NULL ,
	moduleorder numeric(6,1) CONSTRAINT n_tblsysmodule_02 NOT NULL ,
	datastamp numeric(11,0) DEFAULT 0,
	creator varchar(10),
	createdate timestamp without time zone,
	reviser varchar(10),
	revisedate timestamp without time zone,
	 CONSTRAINT pk_tblsysmodule PRIMARY KEY (moduleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsysmenuitem (
moduleno varchar(50) CONSTRAINT n_tblsysmenuitem_01 NOT NULL ,
	menuno varchar(50) CONSTRAINT n_tblsysmenuitem_02 NOT NULL ,
	menuname varchar(50) CONSTRAINT n_tblsysmenuitem_03 NOT NULL ,
	menutype numeric(1,0) CONSTRAINT n_tblsysmenuitem_04 NOT NULL ,
	menuorder numeric(4,0) CONSTRAINT n_tblsysmenuitem_05 NOT NULL ,
	parentmenuno varchar(50) CONSTRAINT n_tblsysmenuitem_06 NOT NULL ,
	activemodule varchar(50) CONSTRAINT n_tblsysmenuitem_07 NOT NULL ,
	 CONSTRAINT pk_tblsysmenuitem PRIMARY KEY (moduleno, menuno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsysfunction (
functionno varchar(50) CONSTRAINT n_tblsysfunction_01 NOT NULL ,
	functionname varchar(50) CONSTRAINT n_tblsysfunction_02 NOT NULL ,
	functionorder numeric(6,1) CONSTRAINT n_tblsysfunction_03 NOT NULL ,
	moduleno varchar(10) CONSTRAINT n_tblsysfunction_04 NOT NULL ,
	formname varchar(50),
	weburl varchar(100),
	creator varchar(10),
	createdate timestamp without time zone,
	privoption numeric(1,0) DEFAULT 0,
	issueoption numeric(1,0) DEFAULT 0,
	issuetable varchar(50),
	executionfile varchar(50),
	functionkey varchar(10),
	executionmode numeric(1,0) DEFAULT 0 CONSTRAINT n_tblsysfunction_05 NOT NULL ,
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

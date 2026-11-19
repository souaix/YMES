-- PostgreSQL create-only reference. Compare with live schema before writing a migration.

CREATE TABLE tbloemocompmantainlog

(
	mono varchar(50) NOT NULL,
	componentno varchar(50) NOT NULL,
	goodqty numeric(12,4),
	unitno varchar(30),
	releasestate numeric(1,0) NOT NULL,
	ex_mocomp1 varchar(50),
	ex_mocomp2 varchar(50),
	inventoryno varchar(20),
	materialno varchar(50),
	materiallotno varchar(50),
	slotno varchar(3),
	vendorno varchar(50),
	componentlotno varchar(50),
	datatype numeric(1,0) NOT NULL,
	userno varchar(10) NOT NULL,
	eventtime timestamp NOT NULL,
	 CONSTRAINT pk_tbloemocompmantainlog PRIMARY KEY (mono, componentno, eventtime)
);

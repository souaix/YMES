-- PostgreSQL create-only reference. Compare with live schema before writing a migration.

CREATE TABLE tbloemomtlstate_component

(
	mono varchar(50) NOT NULL,
	materialno varchar(50) NOT NULL,
	materiallotno varchar(50) NOT NULL,
	unitno varchar(30) NOT NULL,
	componentno varchar(50) NOT NULL,
	componentlotno varchar(50),
	lasermark varchar(50),
	cassetteno varchar(50),
	slotno numeric(5,0),
	goodqty numeric(12,4),
	useflag numeric(1,0) DEFAULT 0 NOT NULL,
	compunitno varchar(30),
	scrapqty numeric(12,4) DEFAULT 0,
	bookingflag numeric(1,0) DEFAULT 0,
	 CONSTRAINT pk_tbloemomtlstate_component PRIMARY KEY (mono, materialno, materiallotno, componentno)
);

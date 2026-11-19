-- PostgreSQL create-only reference. Compare with live schema before writing a migration.

CREATE TABLE tblsystestrecipebasis

(
	testrecipetype varchar(20) NOT NULL,
	testrecipeno varchar(50) NOT NULL,
	testrecipename varchar(100) NOT NULL,
	testrecipeclass numeric(1,0) NOT NULL,
	psno varchar(50) NOT NULL,
	description varchar(255),
	defaultvalue varchar(255),
	transfertype numeric(1,0) NOT NULL,
	datatype numeric(1,0) NOT NULL,
	summarytablename varchar(30),
	creator varchar(10) NOT NULL,
	createdate timestamp NOT NULL,
	issuestate numeric(1,0) NOT NULL,
	reviser varchar(10),
	revisedate timestamp,
	conversionfield varchar(50) DEFAULT 'N/A' NOT NULL,
	datastamp numeric(11,0) DEFAULT 0,
	 CONSTRAINT pk_tblsystestrecipebasis PRIMARY KEY (testrecipetype, testrecipeno)
);

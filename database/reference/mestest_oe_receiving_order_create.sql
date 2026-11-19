-- PostgreSQL create-only reference for home PC schema review.
-- No source rows or deployment instructions. Oracle DATE maps to timestamp without time zone.

CREATE TABLE "YMES".tbloerobasis
(
	rono varchar(25) NOT NULL,
	customerno varchar(50) NOT NULL,
	description varchar(255),
	creator varchar(10),
	createdate timestamp,
	issuestate numeric(1,0) NOT NULL,
	lotserial varchar(55),
	rostate numeric(2,0) DEFAULT 0 NOT NULL,
	duedate timestamp,
	reviser varchar(10),
	revisedate timestamp,
	datastamp numeric(11,0) DEFAULT 0,
	 CONSTRAINT pk_tbloerobasis PRIMARY KEY (rono)
);

CREATE TABLE "YMES".tbloerodetail
(
	rono varchar(25) NOT NULL,
	itemno numeric(2,0) NOT NULL,
	productno varchar(50) NOT NULL,
	orderqty numeric(12,4) NOT NULL,
	unreleaseqty numeric(12,4) NOT NULL,
	state numeric(2,0) DEFAULT 0 NOT NULL,
	description varchar(255),
	creator varchar(10),
	createdate timestamp,
	rounitno varchar(30) NOT NULL,
	lotserial varchar(55),
	customerno varchar(50),
	ordertype varchar(10),
	shipqty numeric(12,4) DEFAULT 0 NOT NULL,
	priority numeric(2,0) DEFAULT 9 NOT NULL,
	bookingflag numeric(1,0) DEFAULT 1 NOT NULL,
	shippingflag numeric(1,0) DEFAULT 1 NOT NULL,
	unitprice numeric(8,2) DEFAULT 0,
	unitmeasure varchar(10),
	currency varchar(10),
	orderdate timestamp,
	reviser varchar(10),
	revisedate timestamp,
	customername varchar(100),
	datastamp numeric(11,0) DEFAULT 0,
	 CONSTRAINT pk_tbloerodetail PRIMARY KEY (rono, itemno)
);

CREATE TABLE "YMES".tbloeroproperty
(
	rono varchar(25) NOT NULL,
	propertyno varchar(20) NOT NULL,
	propertyvalue varchar(255),
	lotserial varchar(55),
	 CONSTRAINT pk_tbloeroproperty PRIMARY KEY (rono, propertyno)
);

CREATE TABLE "YMES".tbloeroproperty_recive
(
	rono varchar(25) NOT NULL,
	propertyno varchar(20) NOT NULL,
	propertyvalue varchar(255),
	 CONSTRAINT pk_tbloeroproperty_recive PRIMARY KEY (rono, propertyno)
);

-- PostgreSQL create-only reference. Compare with live schema before writing a migration.

CREATE TABLE "YMES".tbloemosemiout

(
	mono varchar(50) NOT NULL,
	inventoryno varchar(20) NOT NULL,
	productno varchar(50) NOT NULL,
	lotno varchar(50) NOT NULL,
	locatorno varchar(20) NOT NULL,
	unitno varchar(30) NOT NULL,
	qty numeric(12,4),
	state numeric(1,0) DEFAULT 0,
	 CONSTRAINT pk_tbloemosemiout PRIMARY KEY (mono, productno)
);

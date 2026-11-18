-- PostgreSQL CREATE-only reference translated from MESTEST PRD Program tables.
-- Home PC: compare YMES live schema and migrations first; do not execute against existing tables.
-- Oracle has no foreign keys, triggers, or indexes other than the primary-key unique indexes.

CREATE TABLE IF NOT EXISTS "YMES".tblprdprogrambasis (
  programno varchar(50) CONSTRAINT n_tblprdprogrambasis_01 NOT NULL,
  programtype varchar(10),
  issuestate numeric(1,0) DEFAULT 0 CONSTRAINT n_tblprdprogrambasis_02 NOT NULL,
  creator varchar(10),
  createdate timestamp without time zone,
  description varchar(255),
  CONSTRAINT pk_tblprdprogrambasis PRIMARY KEY (programno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdproductprograms (
  productno varchar(50) CONSTRAINT n_tblprdproductprograms_01 NOT NULL,
  productversion varchar(5) CONSTRAINT n_tblprdproductprograms_02 NOT NULL,
  programno varchar(50) CONSTRAINT n_tblprdproductprograms_03 NOT NULL,
  CONSTRAINT pk_tblprdproductprograms PRIMARY KEY (productno, productversion, programno)
);

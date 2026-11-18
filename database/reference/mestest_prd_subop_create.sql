-- PostgreSQL CREATE-only reference translated from MESTEST.TBLPRDOPSUBOP.
-- Home PC: compare YMES live schema and migrations first; do not execute against an existing table.
-- The primary key creates its unique index. Oracle has no other indexes, foreign keys, or triggers.

CREATE TABLE IF NOT EXISTS "YMES".tblprdopsubop (
  productno varchar(50) CONSTRAINT n_tblprdopsubop_01 NOT NULL,
  productversion varchar(5) CONSTRAINT n_tblprdopsubop_02 NOT NULL,
  opno varchar(20) CONSTRAINT n_tblprdopsubop_03 NOT NULL,
  subopsequence numeric(4,0) CONSTRAINT n_tblprdopsubop_04 NOT NULL,
  subopno varchar(20) CONSTRAINT n_tblprdopsubop_05 NOT NULL,
  subopname varchar(100) CONSTRAINT n_tblprdopsubop_06 NOT NULL,
  creator varchar(10),
  createdate timestamp without time zone,
  datastamp numeric(11,0) DEFAULT 0,
  reviser varchar(10),
  revisedate timestamp without time zone,
  CONSTRAINT pk_tblprdopsubop PRIMARY KEY (productno, productversion, opno, subopsequence)
);

-- PostgreSQL CREATE-only reference translated from MESTEST PD102 Default APS tables.
-- Home PC: compare YMES live schema and migration history first. These Oracle tables
-- already exist; do not run this as a deployment script or recreate populated tables.
-- Oracle has no FK, trigger, or index beyond each table's primary-key unique index.

CREATE TABLE IF NOT EXISTS "YMES".tblprdprsopapssetup (
  productno varchar(30) NOT NULL,
  productversion varchar(5) NOT NULL,
  processno varchar(50) NOT NULL,
  processversion varchar(5) NOT NULL,
  motypeno numeric(2,0) NOT NULL,
  opno varchar(20) NOT NULL,
  nodeid varchar(50) NOT NULL,
  equipmentgroup varchar(50) DEFAULT 'N/A' NOT NULL,
  equipmentno varchar(50) DEFAULT 'N/A' NOT NULL,
  apsfixeqptime numeric(8,0) DEFAULT 0 NOT NULL,
  apsvareqptime numeric(8,0) DEFAULT 0 NOT NULL,
  stdworktimeqty numeric(8,0) NOT NULL,
  transferqty numeric(8,0) DEFAULT 0 NOT NULL,
  lotreartime numeric(8,0) DEFAULT 0 NOT NULL,
  schedule numeric(1,0) NOT NULL,
  subcontractorno varchar(30) NOT NULL,
  batchop numeric(1,0) DEFAULT 0 NOT NULL,
  batchmaxqty numeric(9,0) DEFAULT 999999 NOT NULL,
  description varchar(255),
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  reviser varchar(10),
  revisedate timestamp without time zone,
  CONSTRAINT pk_tblprdprsopapssetup PRIMARY KEY (productno, productversion, motypeno, nodeid)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdprsopapssetupdetail (
  productno varchar(30) NOT NULL,
  productversion varchar(5) NOT NULL,
  processno varchar(50) NOT NULL,
  processversion varchar(5) NOT NULL,
  motypeno numeric(2,0) NOT NULL,
  opno varchar(20) NOT NULL,
  nodeid varchar(50) NOT NULL,
  equipmentgroup varchar(50) NOT NULL,
  equipmentno varchar(50) NOT NULL,
  apsfixeqptime numeric(8,0) DEFAULT 0 NOT NULL,
  apsvareqptime numeric(8,0) DEFAULT 0 NOT NULL,
  stdworktimeqty numeric(8,0) NOT NULL,
  transferqty numeric(8,0) DEFAULT 0 NOT NULL,
  lotreartime numeric(8,0) DEFAULT 0 NOT NULL,
  schedule numeric(1,0) NOT NULL,
  batchop numeric(1,0) DEFAULT 0 NOT NULL,
  batchmaxqty numeric(9,0) DEFAULT 999999 NOT NULL,
  description varchar(255),
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  reviser varchar(10),
  revisedate timestamp without time zone,
  CONSTRAINT pk_tblprdprsopapssetupdetail PRIMARY KEY (productno, productversion, motypeno, nodeid, equipmentno)
);

COMMENT ON TABLE "YMES".tblprdprsopapssetup IS 'MES source MESTEST.TBLPRDPRSOPAPSSETUP';
COMMENT ON TABLE "YMES".tblprdprsopapssetupdetail IS 'MES source MESTEST.TBLPRDPRSOPAPSSETUPDETAIL';

-- PostgreSQL CREATE-only reference for WP601; compare YMES live schema/migrations before use.
-- Generated from read-only MESTEST Oracle DDL. Never apply wholesale or recreate existing objects.
-- Oracle object TBLWIPMOLINEPACKL was absent (no table/view/synonym); no guessed CREATE is included.
-- Source has no foreign keys or triggers. PKs are included; relation table has two nonunique indexes.

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl1 (
  l1_packingno varchar(50) NOT NULL,
  l1_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l1_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl2 (
  l2_packingno varchar(50) NOT NULL,
  l2_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l2_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl3 (
  l3_packingno varchar(50) NOT NULL,
  l3_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l3_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl4 (
  l4_packingno varchar(50) NOT NULL,
  l4_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l4_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl5 (
  l5_packingno varchar(50) NOT NULL,
  l5_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l5_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl6 (
  l6_packingno varchar(50) NOT NULL,
  l6_packingname varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  closeflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  labelprintid varchar(50),
  PRIMARY KEY (l6_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl1detail (
  l1_packingno varchar(50) NOT NULL,
  lotno varchar(50) NOT NULL,
  packqty numeric(12,4) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l1_packingno,lotno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl2detail (
  l2_packingno varchar(50) NOT NULL,
  from_packingno varchar(50) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l2_packingno,from_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl3detail (
  l3_packingno varchar(50) NOT NULL,
  from_packingno varchar(50) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l3_packingno,from_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl4detail (
  l4_packingno varchar(50) NOT NULL,
  from_packingno varchar(50) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l4_packingno,from_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl5detail (
  l5_packingno varchar(50) NOT NULL,
  from_packingno varchar(50) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l5_packingno,from_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackl6detail (
  l6_packingno varchar(50) NOT NULL,
  from_packingno varchar(50) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (l6_packingno,from_packingno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackrelation (
  from_packingno varchar(50) NOT NULL,
  from_packinglevel numeric(1,0) NOT NULL,
  to_packingno varchar(50) NOT NULL,
  to_packinglevel numeric(1,0) NOT NULL,
  mono varchar(50)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvpackstate (
  mono varchar(50) NOT NULL,
  lotno varchar(50) NOT NULL,
  l1_packingno varchar(50) NOT NULL,
  l1_name varchar(50),
  l2_packingno varchar(50),
  l2_name varchar(50),
  l3_packingno varchar(50),
  l3_name varchar(50),
  l4_packingno varchar(50),
  l4_name varchar(50),
  l5_packingno varchar(50),
  l5_name varchar(50),
  l6_packingno varchar(50),
  l6_name varchar(50),
  pkgqty numeric(12,4) NOT NULL,
  l1_opno varchar(20) NOT NULL,
  l1_nodeid varchar(50) NOT NULL,
  l1_pkgtype numeric(1,0) NOT NULL,
  l2_opno varchar(20),
  l2_nodeid varchar(50),
  l2_pkgtype numeric(1,0),
  l3_opno varchar(20),
  l3_nodeid varchar(50),
  l3_pkgtype numeric(1,0),
  l4_opno varchar(20),
  l4_nodeid varchar(50),
  l4_pkgtype numeric(1,0),
  l5_opno varchar(20),
  l5_nodeid varchar(50),
  l5_pkgtype numeric(1,0),
  l6_opno varchar(20),
  l6_nodeid varchar(50),
  l6_pkgtype numeric(1,0),
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  reviser varchar(10),
  revisedate timestamp without time zone,
  PRIMARY KEY (lotno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvpacklog (
  serialno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  lotno varchar(50) NOT NULL,
  l1_packingno varchar(50) NOT NULL,
  l1_name varchar(50),
  l2_packingno varchar(50),
  l2_name varchar(50),
  l3_packingno varchar(50),
  l3_name varchar(50),
  l4_packingno varchar(50),
  l4_name varchar(50),
  l5_packingno varchar(50),
  l5_name varchar(50),
  l6_packingno varchar(50),
  l6_name varchar(50),
  pkgqty numeric(12,4) NOT NULL,
  l1_opno varchar(20) NOT NULL,
  l1_nodeid varchar(50),
  l1_pkgtype numeric(1,0) NOT NULL,
  l2_opno varchar(20),
  l2_nodeid varchar(50),
  l2_pkgtype numeric(1,0),
  l3_opno varchar(20),
  l3_nodeid varchar(50),
  l3_pkgtype numeric(1,0),
  l4_opno varchar(20),
  l4_nodeid varchar(50),
  l4_pkgtype numeric(1,0),
  l5_opno varchar(20),
  l5_nodeid varchar(50),
  l5_pkgtype numeric(1,0),
  l6_opno varchar(20),
  l6_nodeid varchar(50),
  l6_pkgtype numeric(1,0),
  eventaction numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (serialno)
);

CREATE TABLE IF NOT EXISTS ymes.tblinvpackmergelog (
  mono varchar(50) NOT NULL,
  frompacklevel numeric(1,0) NOT NULL,
  frompackno varchar(50) NOT NULL,
  frompackname varchar(50) NOT NULL,
  frompackopno varchar(20) NOT NULL,
  frompacktype numeric(1,0) NOT NULL,
  frompackqty numeric(12,4) NOT NULL,
  topacklevel numeric(1,0) NOT NULL,
  topackno varchar(50) NOT NULL,
  topackname varchar(50) NOT NULL,
  topackopno varchar(20) NOT NULL,
  topacktype numeric(1,0) NOT NULL,
  topackqty numeric(12,4) NOT NULL,
  action numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL
);

CREATE TABLE IF NOT EXISTS ymes.tblwipsmstate (
  lotno varchar(50) NOT NULL,
  fromlotno varchar(50) NOT NULL,
  frombaselotno varchar(50) NOT NULL,
  fromlotserial varchar(55) NOT NULL,
  fromlotqty numeric(12,4) NOT NULL,
  fromscrapqty numeric(12,4) NOT NULL,
  fromotherqty numeric(12,4) NOT NULL,
  tolotno varchar(50) NOT NULL,
  tobaselotno varchar(50) NOT NULL,
  tolotserial varchar(55) NOT NULL,
  tolotqty numeric(12,4) NOT NULL,
  toscrapqty numeric(12,4) NOT NULL,
  tootherqty numeric(12,4) NOT NULL,
  reflotno varchar(50) NOT NULL,
  majorlot numeric(1,0) NOT NULL,
  smtype numeric(2,0) NOT NULL,
  eventtime timestamp without time zone NOT NULL,
  erpflag numeric(1,0) NOT NULL,
  closeflag numeric(1,0) DEFAULT 0,
  PRIMARY KEY (lotno,fromlotno,tolotno,eventtime)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepackconfig (
  computername varchar(50) NOT NULL,
  pdlineno varchar(50) NOT NULL,
  mono varchar(50) NOT NULL,
  l1_packflag numeric(1,0) NOT NULL,
  l2_packflag numeric(1,0) NOT NULL,
  l3_packflag numeric(1,0) NOT NULL,
  l4_packflag numeric(1,0) NOT NULL,
  l5_packflag numeric(1,0) NOT NULL,
  l6_packflag numeric(1,0) NOT NULL,
  creator varchar(10) NOT NULL,
  createdate timestamp without time zone NOT NULL,
  PRIMARY KEY (computername)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmolinepacklabelprinter (
  computername varchar(50) NOT NULL,
  packinglevel numeric(1,0) NOT NULL,
  labeltype varchar(5) NOT NULL,
  printername varchar(100) NOT NULL,
  PRIMARY KEY (computername,labeltype)
);

CREATE INDEX IF NOT EXISTS i_tblwipmolinepackrelation_01 ON ymes.tblwipmolinepackrelation (to_packingno);
CREATE INDEX IF NOT EXISTS i_tblwipmolinepackrelation_02 ON ymes.tblwipmolinepackrelation (from_packingno);

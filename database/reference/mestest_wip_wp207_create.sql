-- PostgreSQL CREATE-only references derived from MESTEST source DDL.
-- Compare YMES live schema and migration history before creating any objects.
-- Shared Lot/Product/Process objects reuse WP101/WP201 and existing master references.

CREATE TABLE IF NOT EXISTS "YMES".tblemsdispatchstate (
    equipmentno varchar(50) CONSTRAINT n_tblemsdispatchstate_01 NOT NULL,
    dispatchorder numeric(3,0) CONSTRAINT n_tblemsdispatchstate_02 NOT NULL,
    lotno varchar(50) CONSTRAINT n_tblemsdispatchstate_03 NOT NULL,
    CONSTRAINT pk_tblemsdispatchstate PRIMARY KEY (equipmentno, dispatchorder)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwipautomerge (
    fromlotno varchar(50) CONSTRAINT n_tblwipautomerge_01 NOT NULL,
    tolotno varchar(50) CONSTRAINT n_tblwipautomerge_02 NOT NULL,
    lotserial varchar(55),
    CONSTRAINT pk_tblwipautomerge PRIMARY KEY (fromlotno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwipcont_employee (
    lotserial varchar(55) CONSTRAINT n_tblwipcont_employee_01 NOT NULL,
    lotno varchar(50) CONSTRAINT n_tblwipcont_employee_02 NOT NULL,
    opno varchar(20) CONSTRAINT n_tblwipcont_employee_03 NOT NULL,
    brno varchar(20) CONSTRAINT n_tblwipcont_employee_04 NOT NULL,
    userno varchar(10) CONSTRAINT n_tblwipcont_employee_05 NOT NULL,
    eventtime timestamp without time zone CONSTRAINT n_tblwipcont_employee_06 NOT NULL,
    functionname varchar(100),
    shiftno varchar(20),
    loggroupserial varchar(50),
    linkname varchar(50),
    interfacetime timestamp without time zone,
    phaseno numeric(2,0)
);

CREATE INDEX IF NOT EXISTS i_tblwipcont_employee_01 ON "YMES".tblwipcont_employee (loggroupserial);
CREATE INDEX IF NOT EXISTS i_tblwipcont_employee_02 ON "YMES".tblwipcont_employee (lotserial);
CREATE INDEX IF NOT EXISTS i_tblwipcont_employee_03 ON "YMES".tblwipcont_employee (lotno, functionname, eventtime);

CREATE TABLE IF NOT EXISTS "YMES".tblwiplotopecnrecord (
    lotno varchar(50) CONSTRAINT n_tblwiplotopecnrecord_01 NOT NULL,
    opno varchar(20) CONSTRAINT n_tblwiplotopecnrecord_02 NOT NULL,
    ecnno_op varchar(20),
    creator varchar(10) CONSTRAINT n_tblwiplotopecnrecord_04 NOT NULL,
    createdate timestamp without time zone CONSTRAINT n_tblwiplotopecnrecord_05 NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    ecntype varchar(10),
    areano varchar(500) CONSTRAINT n_tblwiplotopecnrecord_07 NOT NULL,
    CONSTRAINT pk_tblwiplotopecnrecord PRIMARY KEY (lotno, opno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwiposbasis (
    osno varchar(20) CONSTRAINT n_tblwiposbasis_01 NOT NULL,
    opno varchar(20) CONSTRAINT n_tblwiposbasis_02 NOT NULL,
    preparename varchar(10),
    needbydate timestamp without time zone,
    osqty numeric(12,4) CONSTRAINT n_tblwiposbasis_03 NOT NULL,
    status numeric(2,0) CONSTRAINT n_tblwiposbasis_04 NOT NULL,
    shpdate timestamp without time zone,
    closedate timestamp without time zone,
    price numeric(12,4),
    subcontractorno varchar(20),
    nodeid varchar(50),
    ositemno varchar(2),
    returnopno varchar(20),
    returnnodeid varchar(50),
    shpuserno varchar(10),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    reviser varchar(10),
    revisedate timestamp without time zone,
    ostype numeric(1,0),
    erpno_out varchar(20),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblwiposbasis PRIMARY KEY (osno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwiposdetail (
    osno varchar(20) CONSTRAINT n_tblwiposdetail_01 NOT NULL,
    lotno varchar(50) CONSTRAINT n_tblwiposdetail_02 NOT NULL,
    rcvdate timestamp without time zone,
    status numeric(2,0) CONSTRAINT n_tblwiposdetail_03 NOT NULL,
    inputqty numeric(12,4) CONSTRAINT n_tblwiposdetail_04 NOT NULL,
    inputunitno varchar(30),
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    otherqty numeric(12,4),
    description varchar(250),
    unitprice numeric(12,4),
    returnunitno varchar(30),
    rcvuserno varchar(10),
    erpno_rcv varchar(20),
    CONSTRAINT pk_tblwiposdetail PRIMARY KEY (osno, lotno)
);

-- Source metadata: no foreign keys or triggers on these six tables.
-- TBLWIPCONT_EMPLOYEE has no PK/unique index; keep its three source indexes non-unique.

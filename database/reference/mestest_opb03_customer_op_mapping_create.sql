-- PostgreSQL YMES CREATE-only reference translated from the Oracle source DDL.
-- Do not run this against Oracle. First compare YMES live schema and migrations.
-- CREATE TABLE IF NOT EXISTS does not update an already existing table.
-- No foreign key or additional index is created: Oracle metadata has none.

CREATE TABLE IF NOT EXISTS tbl_bopcustopmapbasis (
    customerno varchar(50) NOT NULL,
    customeropno varchar(20) NOT NULL,
    customeropname varchar(50) NOT NULL,
    customeropseq numeric(3,0) NOT NULL,
    psno varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_bopcustopmapbasis PRIMARY KEY (customerno, customeropno)
);

CREATE TABLE IF NOT EXISTS tbl_bopcustopmapdetail (
    customerno varchar(50) NOT NULL,
    customeropno varchar(20) NOT NULL,
    psno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    custopprocesscode varchar(20) NOT NULL,
    custstepname varchar(20) NOT NULL,
    transactionflag numeric(1,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_bopcustopmapdetail PRIMARY KEY (customerno, customeropno, opno)
);

COMMENT ON TABLE tbl_bopcustopmapbasis IS 'MES Oracle source MESTEST.TBL_BOPCUSTOPMAPBASIS; OPB03 customer operation mapping header';
COMMENT ON TABLE tbl_bopcustopmapdetail IS 'MES Oracle source MESTEST.TBL_BOPCUSTOPMAPDETAIL; OPB03 customer operation mapping detail';

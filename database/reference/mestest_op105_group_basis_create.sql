-- PostgreSQL YMES empty-table reference derived from:
-- database/reference/mestest_op105_group_basis_source_ddl.sql
-- CREATE-only reference. Compare YMES live schema and migrations before applying.
-- Do not execute this file in MESTEST Oracle. No sample/business rows are included.

CREATE TABLE IF NOT EXISTS tblopgroupbasis (
    opgroupno varchar(20) NOT NULL,
    opgroupname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    psno varchar(50),
    opgrouporder numeric(6,0) DEFAULT 0,
    erpno varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopgroupbasis PRIMARY KEY (opgroupno)
);

CREATE TABLE IF NOT EXISTS tblopgroupdetail (
    opgroupno varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    oproleflag numeric(1,0) DEFAULT 0 NOT NULL,
    weightrate numeric(5,2) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopgroupdetail PRIMARY KEY (opgroupno, opno)
);

COMMENT ON TABLE tblopgroupbasis IS 'MES source MESTEST.TBLOPGROUPBASIS; OP105作業站群組主檔';
COMMENT ON TABLE tblopgroupdetail IS 'MES source MESTEST.TBLOPGROUPDETAIL; OP105作業站群組明細';

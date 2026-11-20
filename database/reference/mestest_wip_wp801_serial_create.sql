-- Oracle source: MESTEST.TBLSYSSERIALRULEBASIS and TBLSYSSERIALRULEDETAIL.
-- CREATE-only reference for WP801 8DNO_FACTORY; compare live YMES schema/history first.
-- Earlier mestest_first_phase_create_tables.sql has the columns/PK but omits five Oracle defaults.
CREATE TABLE IF NOT EXISTS ymes.tblsysserialrulebasis (
    serialtype varchar(50) NOT NULL,
    enable numeric(1,0) DEFAULT 0 NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    description varchar(255),
    serialtypeno varchar(50) DEFAULT 'DEFAULT' NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsysserialrulebasis PRIMARY KEY (serialtype, serialtypeno)
);

CREATE TABLE IF NOT EXISTS ymes.tblsysserialruledetail (
    serialtype varchar(50) NOT NULL,
    itemno numeric(2,0) NOT NULL,
    sourcetype varchar(50) NOT NULL,
    seriallength numeric(2,0) NOT NULL,
    searchstart numeric(2,0) NOT NULL,
    decimaltype numeric(1,0),
    userdefine varchar(50),
    serialcodeflag numeric(1,0),
    description varchar(255),
    serialtypeno varchar(50) DEFAULT 'DEFAULT' NOT NULL,
    getdata numeric(1,0) DEFAULT 0,
    functionname varchar(100),
    padleft numeric(1,0),
    CONSTRAINT pk_tblsysserialruledetail PRIMARY KEY (serialtype, serialtypeno, itemno)
);

COMMENT ON TABLE ymes.tblsysserialrulebasis IS 'MES Oracle source: MESTEST.TBLSYSSERIALRULEBASIS; WP801 8DNO_FACTORY';
COMMENT ON TABLE ymes.tblsysserialruledetail IS 'MES Oracle source: MESTEST.TBLSYSSERIALRULEDETAIL; WP801 8DNO_FACTORY';

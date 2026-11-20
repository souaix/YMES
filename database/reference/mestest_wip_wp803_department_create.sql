-- WP803 lookup table: exact Oracle MESTEST.TBLUSRDEPARTMENTBASIS structure,
-- translated to PostgreSQL CREATE-only for home-PC schema comparison.
-- Existing first-phase bootstrap contains the same 14 columns and PK but lacks
-- four Oracle defaults. CREATE IF NOT EXISTS does not repair an existing table.
CREATE TABLE IF NOT EXISTS ymes.tblusrdepartmentbasis (
    departmentno varchar(20) NOT NULL,
    departmentname varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    departmentsname varchar(60) NOT NULL,
    costcenter varchar(30),
    erpno varchar(50),
    controldtconect numeric(1,0) DEFAULT 0 NOT NULL,
    dtconectqty numeric(3,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblusrdepartmentbasis PRIMARY KEY (departmentno)
);
COMMENT ON TABLE ymes.tblusrdepartmentbasis IS 'MES Oracle source: MESTEST.TBLUSRDEPARTMENTBASIS; WP803 department lookup';

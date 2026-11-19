-- PostgreSQL CREATE only. Use the established YMES schema.
-- Columns, nullability and primary keys only. Read-only reference.

CREATE TABLE IF NOT EXISTS "YMES".tblprddefineruncardbasis (
    runcardno varchar(50) NOT NULL,
    runcardtype numeric(1, 0) NOT NULL,
    formatfilename varchar(100) NOT NULL,
    printerno varchar(100),
    mainruncard numeric(1, 0) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    detailheaderstart numeric(5, 0),
    detailheaderend numeric(5, 0),
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprddefineruncardbasis PRIMARY KEY (runcardno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprddefineruncarddetail (
    runcardno varchar(50) NOT NULL,
    filefield varchar(50) NOT NULL,
    fieldvaluetype numeric(2, 0) NOT NULL,
    datasource varchar(10),
    datasourcename varchar(50),
    fielddata varchar(50),
    direction numeric(2, 0),
    startindex numeric(2, 0),
    datalength numeric(2, 0),
    defaultvalue varchar(100),
    datascript text,
    memo varchar(255),
    CONSTRAINT pk_tblprddefineruncarddetail PRIMARY KEY (runcardno, filefield)
);

-- PostgreSQL CREATE-only comparison. Verify YMES live schema and version lengths before migrating.
-- varchar limits characters in PostgreSQL. Oracle source limits bytes for these columns.
CREATE TABLE IF NOT EXISTS "YMES".tblsysbin (
    binno varchar(10) NOT NULL,
    binorder numeric(3,0) DEFAULT 99,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblsysbin PRIMARY KEY (binno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdftprogrambin (
    programno varchar(50) NOT NULL,
    programversion varchar(3) NOT NULL,
    binno varchar(10) NOT NULL,
    binvalue numeric(1,0) NOT NULL,
    dgrade numeric(1,0) DEFAULT 0 NOT NULL,
    rank varchar(25),
    description varchar(255),
    osbin numeric(1,0) DEFAULT 0 NOT NULL,
    CONSTRAINT pk_tblprdftprogrambin PRIMARY KEY (programno, programversion, binno)
);

-- PostgreSQL CREATE only. Use the established YMES schema.
-- Hardware Bin is covered by mestest_prd_bin.

CREATE TABLE IF NOT EXISTS "YMES".tblprdftprogrambasis (
    programno varchar(50) NOT NULL,
    programversion varchar(3) NOT NULL,
    curversion numeric(1, 0) NOT NULL,
    issuestate numeric(1, 0) NOT NULL,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    description varchar(255),
    softwarebinmanage numeric(1, 0),
    folderno varchar(100),
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdftprogrambasis PRIMARY KEY (programno, programversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdftprogramswbin (
    programno varchar(50) NOT NULL,
    programversion varchar(3) NOT NULL,
    binno varchar(10) NOT NULL,
    testplan_name varchar(50) NOT NULL,
    swbinno varchar(10) NOT NULL,
    swbinvalue numeric(1, 0) NOT NULL,
    CONSTRAINT pk_tblprdftprogramswbin PRIMARY KEY (programno, programversion, testplan_name, swbinno)
);

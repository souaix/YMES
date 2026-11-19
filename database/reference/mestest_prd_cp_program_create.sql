-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpprdprogrambasis (
    programno varchar(50) NOT NULL,
    programtype varchar(20) NOT NULL,
    programversion varchar(10) NOT NULL,
    issuestate numeric(1, 0) NOT NULL,
    curversion numeric(1, 0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    folderno varchar(100),
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tbl_cpprdprogrambasis PRIMARY KEY (programno, programversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_cpprdprogrambin (
    programno varchar(50) NOT NULL,
    programversion varchar(20) NOT NULL,
    binno varchar(10) NOT NULL,
    binresult numeric(1, 0) NOT NULL,
    osflag numeric(1, 0) NOT NULL,
    description varchar(255),
    bincolor numeric(11, 0),
    bincolor_rgb varchar(15),
    CONSTRAINT pk_tbl_cpprdprogrambin PRIMARY KEY (programno, programversion, binno)
);

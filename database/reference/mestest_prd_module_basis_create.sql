-- PostgreSQL CREATE-only comparison. Reconcile first-phase reference and live schema before migrating.
CREATE TABLE IF NOT EXISTS "YMES".tblprdmodulebasis (
    moduleno varchar(45) NOT NULL,
    moduleversion varchar(3) NOT NULL,
    modulename varchar(100) NOT NULL,
    moduletype varchar(50) NOT NULL,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    description varchar(255),
    curversion numeric(1,0) NOT NULL DEFAULT 0,
    unitno varchar(30),
    unittype varchar(30),
    psno varchar(50) NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdmodulebasis PRIMARY KEY (moduleno, moduleversion)
);

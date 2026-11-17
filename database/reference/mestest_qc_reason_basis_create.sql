-- YMES PostgreSQL schema-only reference translated from MESTEST.TBLQCREASONBASIS.
-- Compare the live YMES schema before creating a migration; preserve YMES-only columns.
CREATE TABLE IF NOT EXISTS "YMES".tblqcreasonbasis (
    reasonno varchar(20) NOT NULL,
    reasonname varchar(100) NOT NULL,
    reasontype numeric(1,0) NOT NULL,
    reasonlevel numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    reasonsubtype varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcreasonbasis PRIMARY KEY (reasonno)
);

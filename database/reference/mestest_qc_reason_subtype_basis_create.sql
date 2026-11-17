-- YMES PostgreSQL schema-only reference translated from MESTEST.TBLQCREASONSUBTYPEBASIS.
-- Compare the live YMES schema before creating a migration; preserve YMES-only columns.
CREATE TABLE IF NOT EXISTS "YMES".tblqcreasonsubtypebasis (
    reasontype numeric(1,0),
    reasonsubtype varchar(50) NOT NULL,
    reasonsubtypename varchar(100),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcreasonsubtypebasis PRIMARY KEY (reasonsubtype)
);

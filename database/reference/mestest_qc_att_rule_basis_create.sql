-- PostgreSQL schema-only reference translated from MESTEST.TBLQCATTRULEBASIS and TBLQCATTRULEDETAIL.
-- Compare YMES live schema before creating migrations; retain YMES-only columns and any existing shared lookup table.
CREATE TABLE IF NOT EXISTS "YMES".tblqcattrulebasis (
    ruleno varchar(20) NOT NULL,
    componentcollect numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcattrulebasis PRIMARY KEY (ruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcattruledetail (
    ruleno varchar(20) NOT NULL,
    action numeric(1,0) NOT NULL,
    reasontype numeric(1,0) NOT NULL DEFAULT 0,
    reasonlevel numeric(1,0) NOT NULL DEFAULT -1,
    rejectnum numeric(14,4),
    ruletype numeric(1,0) DEFAULT 0,
    rate varchar(10),
    changeaql varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcattruledetail PRIMARY KEY (ruleno, action, reasontype, reasonlevel)
);
-- Source tables have no foreign key between basis and detail. Enforce parent-child behavior in the API transaction.

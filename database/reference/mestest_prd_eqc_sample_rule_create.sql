-- PostgreSQL CREATE only. Use the established YMES schema.
-- System Bin catalog is covered by mestest_prd_bin.

CREATE TABLE IF NOT EXISTS "YMES".tblprdeqcsamplerule (
    ruleno varchar(55) NOT NULL,
    ruleoption numeric(2, 0) NOT NULL,
    sampletype numeric(1, 0) NOT NULL,
    sampleqty numeric(6, 0),
    maxbinqty numeric(2, 0),
    assignbinno varchar(10),
    issuestate numeric(1, 0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    samplefunction varchar(50),
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdeqcsamplerule PRIMARY KEY (ruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblprdeqcsamplerulebin (
    ruleno varchar(55) NOT NULL,
    binorder numeric(3, 0) NOT NULL,
    binno varchar(10) NOT NULL,
    CONSTRAINT pk_tblprdeqcsamplerulebin PRIMARY KEY (ruleno, binorder)
);

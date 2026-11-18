BEGIN;

-- Home-PC live audit on 2026-11-18 confirmed this table absent.
-- Based on MESTEST.TBL_FABPRSSTAGEBASIS, not a data import.
CREATE TABLE "YMES".tbl_fabprsstagebasis (
    stageno varchar(50) NOT NULL,
    stagename varchar(50) NOT NULL,
    stagetype varchar(50) NOT NULL,
    sequence numeric(4,0) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_fabprsstagebasis PRIMARY KEY (stageno),
    CONSTRAINT u_tbl_fabprsstagebasis_01 UNIQUE (sequence)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tbl_fabprsstagebasis TO ymes_app;

COMMIT;

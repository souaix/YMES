-- PostgreSQL YMES CREATE-only reference translated from MESTEST.TBL_FABPRSSTAGEBASIS.
-- Do not run against Oracle. Compare the YMES live schema and migrations first.
-- CREATE TABLE IF NOT EXISTS will not change an existing table or its defaults.
-- Oracle metadata has only the StageNo primary key and unique Sequence; no FK or extra index.

CREATE TABLE IF NOT EXISTS ymes.tbl_fabprsstagebasis (
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

COMMENT ON TABLE ymes.tbl_fabprsstagebasis IS 'MES Oracle source MESTEST.TBL_FABPRSSTAGEBASIS; PS102 FAB PRS stage master';

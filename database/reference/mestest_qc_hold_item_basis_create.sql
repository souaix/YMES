-- YMES PostgreSQL reference CREATE for MESTEST.TBLQCHOLDITEMBASIS.
-- Compare the live YMES schema before adding a migration; preserve any YMES extensions.
-- Oracle DATE maps to timestamp without time zone. Oracle NUMBER precision/default/nullability is preserved.
-- The Oracle table has only the HOLDITEMNO primary-key index and no foreign keys.
CREATE TABLE IF NOT EXISTS tblqcholditembasis (
    mclassno     varchar(30) NOT NULL,
    sclassno     varchar(30) NOT NULL,
    holditemno   varchar(50) NOT NULL,
    holditemname varchar(100) NOT NULL,
    description  varchar(255),
    creator      varchar(10),
    createdate   timestamp without time zone,
    issuestate   numeric(1,0) DEFAULT 0 NOT NULL,
    createerf    numeric(1,0) DEFAULT 1,
    holddispgroup varchar(50),
    ocapno       varchar(50),
    ocapversion  varchar(5),
    holdsystem   numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp    numeric(11,0) DEFAULT 0,
    reviser      varchar(10),
    revisedate   timestamp without time zone,
    CONSTRAINT pk_tblqcholditembasis PRIMARY KEY (holditemno)
);

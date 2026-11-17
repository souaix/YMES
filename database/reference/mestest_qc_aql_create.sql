-- PostgreSQL schema-only reference translated from MESTEST.TBLQCAQL.
-- This is a shared lookup dependency; compare YMES live schema before considering a migration.
CREATE TABLE IF NOT EXISTS "YMES".tblqcaql (
    aqltype varchar(25) NOT NULL,
    minnum numeric(8,0) NOT NULL,
    maxnum numeric(8,0) NOT NULL,
    slevel varchar(6) NOT NULL,
    aql varchar(30) NOT NULL,
    samplenum numeric(6,0),
    acceptnum numeric(6,0),
    rejectnum numeric(6,0),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblqcaql PRIMARY KEY (aqltype, minnum, maxnum, slevel, aql)
);

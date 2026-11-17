-- PostgreSQL structure-only lookup reference mapped from the Oracle source DDL.
-- Not a migration. Check whether YMES already has this shared OP scenario table.
-- Oracle DATE maps to timestamp without time zone; NUMBER(p,0) maps to numeric(p,0).
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES".tblopscenariobasis (
    scenariono varchar(50) NOT NULL,
    scenarioname varchar(50) NOT NULL,
    issuestate numeric(1, 0) NOT NULL DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopscenariobasis PRIMARY KEY (scenariono)
);

-- Oracle metadata: one PK unique index; no foreign keys or additional indexes.

BEGIN;
-- Apply only after checking live absence and verifying a backup.
-- Applied by HOME PC on 2026-11-19 after both checks; do not replay CREATE.
-- This forward migration does not execute the full reference.
-- Source: mestest_qc_factor_maintain_source_ddl.sql, TBLSYSMESPARAMETER only.
-- 2026-11-19 PPK BEGIN READ ONLY audit found no such relation in any schema.
-- All 16 columns, including the first PARAMETERNO, were compared with raw DDL.
-- Keep five NOT NULL columns, four defaults, and the source single-column PK.
-- No FC tables, FK, extra index, parameter rows, or provider implementation.
CREATE TABLE "YMES".tblsysmesparameter (
    parameterno varchar(30) NOT NULL,
    parametername varchar(50) NOT NULL,
    tablename varchar(50),
    fieldname varchar(50),
    "condition" varchar(50),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    parametertype numeric(1,0) NOT NULL DEFAULT 0,
    functionname varchar(50),
    parameterclass numeric(1,0) NOT NULL,
    showname varchar(50) DEFAULT 'N/A',
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsysmesparameter PRIMARY KEY (parameterno)
);
GRANT SELECT ON "YMES".tblsysmesparameter TO ymes_app;
-- PostgreSQL row-locking SELECT also requires UPDATE on at least one column.
-- This is not a grant of table-level UPDATE or a parameter maintenance API.
GRANT UPDATE (parameterno) ON "YMES".tblsysmesparameter TO ymes_app;
COMMIT;

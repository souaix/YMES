-- PostgreSQL CREATE-only reference adapted from Oracle MESTEST.TBLQCSPECBASIS
-- and TBLQCSPECDETAIL. This is a schema comparison aid, not a migration.
-- Oracle DDL is in mestest_qc205_variable_dependencies_source_ddl.sql.
-- Compare YMES live schema/migrations before adding a forward migration.
-- Do not execute this against Oracle or rebuild shared QC Item/Factor tables.

CREATE TABLE IF NOT EXISTS "YMES".tblqcspecbasis (
    qcitemno       varchar(25) NOT NULL,
    qcspecserialno varchar(35) NOT NULL,
    issuestate     numeric(1,0) NOT NULL DEFAULT 0,
    datastamp      numeric(11,0) DEFAULT 0,
    description    varchar(255),
    creator        varchar(10),
    createdate     timestamp without time zone,
    reviser        varchar(10),
    revisedate     timestamp without time zone,
    factorvalues   jsonb NOT NULL DEFAULT '{}'::jsonb,
    CONSTRAINT pk_tblqcspecbasis PRIMARY KEY (qcspecserialno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblqcspecdetail (
    spcid          numeric(2,0) NOT NULL DEFAULT 0,
    us             numeric(12,4),
    ls             numeric(12,4),
    cs             numeric(12,4),
    qcspecserialno varchar(35) NOT NULL,
    ucl            varchar(15),
    cl             varchar(15),
    lcl            varchar(15),
    memo           varchar(255),
    CONSTRAINT pk_tblqcspecdetail PRIMARY KEY (qcspecserialno, spcid)
);

-- Oracle has no FK or non-PK indexes on these two tables.
-- Factor JSON keys preserve source parameter identifiers, including POSION
-- and THINKNESS. Validate keys against the selected QC Item FactorSpec list.

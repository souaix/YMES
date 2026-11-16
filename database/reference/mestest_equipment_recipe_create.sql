-- Source: MESTEST Oracle DDL queried read-only with PowerShell 5.1 + Oracle.ManagedDataAccess on 2026-11-16.
-- Target: PostgreSQL schema ymes. Empty tables only; no MES business data.
-- Oracle DATE maps to timestamp without time zone; NUMBER maps to numeric.
-- The Oracle tables declare no foreign keys. Recipe basis deletion must explicitly delete its detail rows in one transaction.
-- Compare the target YMES schema before applying; IF NOT EXISTS does not reconcile existing tables.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tbleqprecipebasis (
    recipegroup varchar(50) CONSTRAINT n_tbleqprecipebasis_01 NOT NULL,
    recipeversion numeric(2,0) DEFAULT 0 CONSTRAINT n_tbleqprecipebasis_02 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0,
    curversion numeric(1,0) DEFAULT 0 CONSTRAINT n_tbleqprecipebasis_03 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqprecipebasis PRIMARY KEY (recipegroup, recipeversion)
);
CREATE INDEX IF NOT EXISTS i_tbleqprecipebasis_01 ON ymes.tbleqprecipebasis (recipegroup, curversion);
COMMENT ON TABLE ymes.tbleqprecipebasis IS 'MES Oracle source: MESTEST.TBLEQPRECIPEBASIS';
COMMENT ON COLUMN ymes.tbleqprecipebasis.recipegroup IS 'Recipe group identifier';
COMMENT ON COLUMN ymes.tbleqprecipebasis.recipeversion IS 'Recipe version';
COMMENT ON COLUMN ymes.tbleqprecipebasis.issuestate IS 'Issue state: -1 unused, 0 unfrozen, 1 pending, 2 active';
COMMENT ON COLUMN ymes.tbleqprecipebasis.curversion IS 'Current approved version flag';
COMMENT ON COLUMN ymes.tbleqprecipebasis.datastamp IS 'Optimistic concurrency stamp';

CREATE TABLE IF NOT EXISTS ymes.tbleqprecipedetail (
    recipegroup varchar(50) CONSTRAINT n_bleqprecipedetail_01 NOT NULL,
    recipeversion numeric(2,0) DEFAULT 0 CONSTRAINT n_bleqprecipedetail_02 NOT NULL,
    recipeno varchar(20) CONSTRAINT n_bleqprecipedetail_03 NOT NULL,
    recipevalue varchar(255) CONSTRAINT n_bleqprecipedetail_04 NOT NULL,
    sequence numeric(2,0) DEFAULT 0,
    description varchar(255),
    type numeric(1,0),
    stdvalue numeric(8,4),
    maximum numeric(8,4),
    minimum numeric(8,4),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqprecipedetail PRIMARY KEY (recipegroup, recipeversion, recipeno)
);
CREATE INDEX IF NOT EXISTS i_tbleqprecipedetail_01 ON ymes.tbleqprecipedetail (recipegroup, recipeno);
COMMENT ON TABLE ymes.tbleqprecipedetail IS 'MES Oracle source: MESTEST.TBLEQPRECIPEDETAIL';
COMMENT ON COLUMN ymes.tbleqprecipedetail.type IS 'Recipe value type: -1 none, 0 value, 1 range';
COMMENT ON COLUMN ymes.tbleqprecipedetail.datastamp IS 'Optimistic concurrency stamp';

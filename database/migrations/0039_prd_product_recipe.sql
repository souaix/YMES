BEGIN;
-- 2026-11-19 live audit: recipe master exists; assignment table missing.
ALTER TABLE "YMES".tblprdrecipebasis
    ALTER COLUMN recipeversion SET DEFAULT 0,
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN curversion SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;
CREATE TABLE "YMES".tblprdrecipe (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    recipegroup varchar(50) NOT NULL,
    CONSTRAINT pk_tblprdrecipe PRIMARY KEY (productno, productversion, recipegroup)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprdrecipe TO ymes_app;
-- PostgreSQL row-locking SELECT requires UPDATE privilege on at least one column.
GRANT SELECT, UPDATE (recipegroup) ON "YMES".tblprdrecipebasis TO ymes_app;
COMMIT;

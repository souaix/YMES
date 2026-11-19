BEGIN;
-- 2026-11-19 live audit: program master exists; association table missing.
-- Restore verified source DEFAULT without changing existing values/nullability.
ALTER TABLE "YMES".tblprdprogrambasis ALTER COLUMN issuestate SET DEFAULT 0;
CREATE TABLE "YMES".tblprdproductprograms (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    programno varchar(50) NOT NULL,
    CONSTRAINT pk_tblprdproductprograms PRIMARY KEY (productno, productversion, programno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprdproductprograms TO ymes_app;
GRANT SELECT, UPDATE (programno) ON "YMES".tblprdprogrambasis TO ymes_app;
COMMIT;

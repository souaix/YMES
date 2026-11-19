BEGIN;
-- Restore only defaults verified against raw Oracle/reference DDL and live metadata.
-- No type/nullability/PK change, data rewrite, FK, or new index.
ALTER TABLE "YMES".tblprdproductbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN curversion SET DEFAULT 0,
    ALTER COLUMN specno SET DEFAULT 'N/A',
    ALTER COLUMN cartonqty SET DEFAULT 0,
    ALTER COLUMN palletqty SET DEFAULT 0,
    ALTER COLUMN boxqty SET DEFAULT 0,
    ALTER COLUMN lotstdqty SET DEFAULT 0,
    ALTER COLUMN customerno SET DEFAULT 'N/A',
    ALTER COLUMN deviceno SET DEFAULT 'N/A',
    ALTER COLUMN batchecnlock SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0,
    ALTER COLUMN sap_upload SET DEFAULT 'N';
COMMIT;

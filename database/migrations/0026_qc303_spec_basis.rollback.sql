BEGIN;

-- Destructive rollback: this removes QC303 data and both created tables.
DROP TABLE "YMES".tblqcspecdetail;
DROP TABLE "YMES".tblqcspecbasis;

COMMIT;

-- Destructive rollback. Only run after confirming that all QC501-QC503 data
-- can be discarded and the dependent API version has been rolled back.
BEGIN;
DROP TABLE "YMES".tblqcinspmachineparameter;
DROP TABLE "YMES".tblqcinspmachinebasis;
DROP TABLE "YMES".tblqcinspdriver;
DROP TABLE "YMES".tblqcinspmachineproperty;
DROP TABLE "YMES".tblqcinspmachineparametertype;
DROP TABLE "YMES".tblqcinspmachinetype;
COMMIT;

BEGIN;
-- Existing QC106 master, reused by QC201 approved-reason mapping.
-- 2026-11-19 PPK READ ONLY audit confirmed SELECT is already granted,
-- reasonno is the real primary key, and no UPDATE column grant exists.
-- FOR SHARE requires UPDATE on at least one column; do not grant full DML.
GRANT UPDATE (reasonno) ON "YMES".tblqcreasonbasis TO ymes_app;
COMMIT;

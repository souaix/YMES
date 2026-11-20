-- PREPARED CANDIDATE ONLY; NOT APPLIED. Root reviews/backs up/applies separately.
-- HOME PC PostgreSQL clone: existing "YMES" Forwarder source15/PK/index.
-- Only the three missing source defaults and Forwarder table CRUD are added.
-- Preserve all values/NULLs, parent Broker, Customer, unrelated grants and
-- the existing0057 brokerno column SELECT. No CREATE/rebuild/backfill/FK/index.
BEGIN;

ALTER TABLE "YMES".tblentforwarderbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN createdate SET DEFAULT LOCALTIMESTAMP,
    ALTER COLUMN datastamp SET DEFAULT 0;

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblentforwarderbasis TO ymes_app;

COMMIT;

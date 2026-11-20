-- PREPARED CANDIDATE ONLY; NOT APPLIED. Root reviews/backs up/applies separately.
-- HOME PC PostgreSQL clone, exact existing "YMES" owner tables.
-- Root readonly 2026-11-20 audit: Broker native13/PK/index already exist;
-- only the three source defaults and necessary API/refguard privileges lack.
-- Preserve every existing value (including NULL), column, constraint and audit.
-- Root's candidate contract marks the reference-delete guard as a YMES addition;
-- this grant only permits reading that real reference key, not Forwarder CRUD.
BEGIN;

ALTER TABLE "YMES".tblentbrokerbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN createdate SET DEFAULT LOCALTIMESTAMP,
    ALTER COLUMN datastamp SET DEFAULT 0;

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblentbrokerbasis TO ymes_app;
GRANT SELECT (brokerno) ON "YMES".tblentforwarderbasis TO ymes_app;

COMMIT;

-- PREPARED ONLY / NOT APPLIED. Migration number remains pending Root allocation.
-- ET105 native Taker13 already exists; only the three missing source defaults
-- and its own application CRUD grants are in scope. No CREATE/backfill/role changes.
-- Root must wrap this exact payload with reviewed guards, full private backup,
-- transactional recheck and independent readback before any real application.
ALTER TABLE "YMES".tblenttakerbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN createdate SET DEFAULT LOCALTIMESTAMP,
    ALTER COLUMN datastamp SET DEFAULT 0;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tblenttakerbasis TO ymes_app;

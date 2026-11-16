BEGIN;

-- TBLSMDPDLINEBASIS was verified against the Oracle source DDL and already
-- exists in YMES. Restore its source defaults without rewriting current rows.
ALTER TABLE "YMES".tblsmdpdlinebasis
    ALTER COLUMN pdlinestatus SET DEFAULT 1,
    ALTER COLUMN createdate SET DEFAULT LOCALTIMESTAMP,
    ALTER COLUMN datastamp SET DEFAULT 0;

-- Grant only the operations required by the authenticated SD107 API.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tblsmdpdlinebasis TO ymes_app;

COMMIT;

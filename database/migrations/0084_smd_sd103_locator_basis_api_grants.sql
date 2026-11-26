BEGIN;

-- SD103 reuses the locator table created from verified MESTEST DDL.
-- Grant only the operations required by the authenticated Locator Basis API.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblinvlocatorbasis
TO ymes_app;

COMMIT;

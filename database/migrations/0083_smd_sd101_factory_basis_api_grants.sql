BEGIN;

-- SD101 reuses the verified factory table from the first-phase schema.
-- Grant only the operations needed by the authenticated factory-basis API.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblsmdfactorybasis
TO ymes_app;

COMMIT;

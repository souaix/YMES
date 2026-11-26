BEGIN;

-- The SD105 table was created from verified MESTEST DDL in migration 0017.
-- Grant only the operations required by the authenticated Area Inventory API.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblsmdareainventory
TO ymes_app;

COMMIT;

BEGIN;

-- Restore the source MES defaults and grant the API account the required CRUD access.
ALTER TABLE "YMES".tbleqpstatebasis
    ALTER COLUMN statetype SET DEFAULT 1,
    ALTER COLUMN utilizationtype SET DEFAULT 1;

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tbleqpstatebasis TO ymes_app;

COMMIT;

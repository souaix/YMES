BEGIN;

REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tbleqpstatebasis FROM ymes_app;

ALTER TABLE "YMES".tbleqpstatebasis
    ALTER COLUMN statetype DROP DEFAULT,
    ALTER COLUMN utilizationtype DROP DEFAULT;

COMMIT;

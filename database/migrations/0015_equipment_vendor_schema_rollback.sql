BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tbleqpvendorcontactor)
       OR EXISTS (SELECT 1 FROM "YMES".tbleqpvendor) THEN
        RAISE EXCEPTION 'Refusing to remove equipment vendor tables containing data.';
    END IF;
END $$;

REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tbleqpvendor,
    "YMES".tbleqpvendorcontactor
FROM ymes_app;

DROP TABLE "YMES".tbleqpvendorcontactor;
DROP TABLE "YMES".tbleqpvendor;

COMMIT;

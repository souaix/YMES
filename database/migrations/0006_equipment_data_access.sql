BEGIN;

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tbleqpequipmentbasis
TO ymes_app;

COMMIT;

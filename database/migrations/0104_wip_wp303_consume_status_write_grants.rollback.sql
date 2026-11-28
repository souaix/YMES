BEGIN;

REVOKE INSERT, UPDATE, DELETE, SELECT ON TABLE "YMES".tblwipeqpconsumestatus FROM ymes_app;
REVOKE INSERT ON TABLE "YMES".tblwipeqpconsumelog FROM ymes_app;

COMMIT;

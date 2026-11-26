BEGIN;

REVOKE INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblsmdareabasis
FROM ymes_app;

REVOKE SELECT ON TABLE
    "YMES".tblusrdepartmentbasis
FROM ymes_app;

COMMIT;

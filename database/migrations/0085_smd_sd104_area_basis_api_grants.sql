BEGIN;

-- SD104 Area Basis API: CRUD on the existing basis table and read-only
-- department lookup. Layout relationship writers are deliberately excluded
-- until their source MES transaction rules are verified.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblsmdareabasis
TO ymes_app;

GRANT SELECT ON TABLE
    "YMES".tblusrdepartmentbasis
TO ymes_app;

COMMIT;

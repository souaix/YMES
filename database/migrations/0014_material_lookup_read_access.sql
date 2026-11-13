BEGIN;

-- Production material forms read lookup definitions from these MES source tables.
-- Keep the application role read-only on shared system reference data.
GRANT SELECT ON TABLE
    "YMES".tblsysunitbasis,
    "YMES".tblsyselementbasis
TO ymes_app;

COMMIT;

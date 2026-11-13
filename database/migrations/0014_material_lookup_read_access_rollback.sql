BEGIN;

REVOKE SELECT ON TABLE
    "YMES".tblsysunitbasis,
    "YMES".tblsyselementbasis
FROM ymes_app;

COMMIT;

BEGIN;

GRANT SELECT ON TABLE
    "YMES".tblsysmodule,
    "YMES".tblsysmenuitem,
    "YMES".tblsysfunction
TO ymes_app;

COMMIT;

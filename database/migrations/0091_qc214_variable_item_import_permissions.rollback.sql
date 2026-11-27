BEGIN;

REVOKE INSERT, DELETE ON "YMES".tblqcitemforvariables FROM ymes_app;

-- These two SELECT grants were absent before migration 0091. The PMI group
-- basis SELECT privilege was already present and is deliberately retained.
REVOKE SELECT ON "YMES".tblprdqcitem, "YMES".tblopgqcitemdetail FROM ymes_app;

COMMIT;

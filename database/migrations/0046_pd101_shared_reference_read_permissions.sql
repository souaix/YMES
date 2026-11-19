BEGIN;
-- 2026-11-19 PPK pg_catalog audit: these six native tables already exist,
-- but ymes_app cannot read them. PD101 checks existing TypeNo/version=-1
-- references before deleting its parent; unreadable is not equivalent to empty.
-- This migration grants read access only, without changing schema or data.
GRANT SELECT ON
    "YMES".tblprdesopbasis,
    "YMES".tblprdproductlabelrulebasis,
    "YMES".tblprdproductlabelruledetail,
    "YMES".tblprdproductserialrulebasis,
    "YMES".tblprdproductserialruledetail,
    "YMES".tblprdprsopapssetupdetail
TO ymes_app;
COMMIT;

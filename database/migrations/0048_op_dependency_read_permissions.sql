BEGIN;
-- PD206 parent lifecycle regression exposed OP hard-delete dependency reads
-- failing on existing native tables. 2026-11-19 pg_catalog audit confirmed all
-- six tables and their opno columns exist; ymes_app lacks SELECT on each.
-- These reads prevent deleting a referenced operation. No schema/data change.
GRANT SELECT ON
    "YMES".tblsysflownodebasis,
    "YMES".tbleqplocatordetail,
    "YMES".tblprdrecipedetail,
    "YMES".tblopgroupdetail,
    "YMES".tblprsopapsdetail,
    "YMES".tblprdprsopapsbasedetail
TO ymes_app;
COMMIT;

-- OP105 source defaults only. Existing rows (including true NULLs), keys and
-- indexes are preserved. Compare the live schema before applying this file.
-- Source: mestest_op105_group_basis_source_ddl.sql (11 + 9 columns).
BEGIN;

ALTER TABLE "YMES".tblopgroupbasis
    ALTER COLUMN opgrouporder SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

ALTER TABLE "YMES".tblopgroupdetail
    ALTER COLUMN oproleflag SET DEFAULT 0,
    ALTER COLUMN weightrate SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

GRANT SELECT, INSERT, UPDATE, DELETE ON
    "YMES".tblopgroupbasis,
    "YMES".tblopgroupdetail
TO ymes_app;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwipareaoperatorstate') IS NULL
       OR to_regclass('"YMES".tblwipareaoperatorlog') IS NULL THEN
        RAISE EXCEPTION 'WP307 source tables are missing; apply migration 0098 before 0099';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'YMES'
          AND c.relname IN ('ix_wp307_operator_state_area_login', 'ix_wp307_operator_log_area_logout')
    ) THEN
        RAISE EXCEPTION 'A WP307 support index name already exists in YMES; review before migration 0099';
    END IF;
END
$$;

-- YMES-only, non-unique query support indexes. MES Oracle has no such indexes;
-- these do not create a source key or change business semantics.
CREATE INDEX ix_wp307_operator_state_area_login
    ON "YMES".tblwipareaoperatorstate (areano, departmentno, logintime DESC, userno COLLATE "C");
COMMENT ON INDEX "YMES".ix_wp307_operator_state_area_login
    IS 'YMES WP307 support index for current state area and login-time queries; not present in Oracle source.';

CREATE INDEX ix_wp307_operator_log_area_logout
    ON "YMES".tblwipareaoperatorlog (areano, departmentno, logouttime DESC, logintime DESC);
COMMENT ON INDEX "YMES".ix_wp307_operator_log_area_logout
    IS 'YMES WP307 support index for history area and logout-time queries; not present in Oracle source.';

COMMIT;

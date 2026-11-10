BEGIN;

ALTER TABLE "YMES".ym_auth_accounts RENAME TO ym_auth_login_state;
ALTER TABLE "YMES".ym_auth_login_state
    RENAME CONSTRAINT ym_auth_accounts_pkey TO ym_auth_login_state_pkey;
ALTER TABLE "YMES".ym_auth_login_state
    RENAME CONSTRAINT fk_ym_auth_accounts_user_master TO fk_ym_auth_login_state_user_master;
ALTER INDEX IF EXISTS "YMES".ux_ym_auth_accounts_user_no_lower
    RENAME TO ux_ym_auth_login_state_user_no_lower;

ALTER TABLE "YMES".ym_auth_login_state
    DROP COLUMN IF EXISTS display_name,
    DROP COLUMN IF EXISTS password_hash,
    DROP COLUMN IF EXISTS role_code,
    DROP COLUMN IF EXISTS is_active,
    DROP COLUMN IF EXISTS must_change_password,
    DROP COLUMN IF EXISTS password_changed_at;

COMMENT ON TABLE "YMES".ym_auth_login_state IS
    'Login lockout and last-login metadata keyed to tblusruserbasis.userno; no account credentials or roles.';
COMMENT ON COLUMN "YMES".tblusruserbasis.password IS
    'ASP.NET Core Identity IdentityV3 versioned PBKDF2-HMAC-SHA512 password hash; never store plaintext.';

COMMIT;

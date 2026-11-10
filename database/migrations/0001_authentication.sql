BEGIN;

CREATE TABLE IF NOT EXISTS "YMES".ym_auth_accounts (
    user_no varchar(10) PRIMARY KEY,
    display_name varchar(50) NOT NULL,
    password_hash text NOT NULL,
    role_code varchar(50) NOT NULL CHECK (role_code IN ('SYSTEM_ADMIN', 'SUPERVISOR', 'OPERATOR')),
    is_active boolean NOT NULL DEFAULT true,
    must_change_password boolean NOT NULL DEFAULT false,
    failed_login_attempts smallint NOT NULL DEFAULT 0 CHECK (failed_login_attempts >= 0),
    locked_until timestamptz,
    last_login_at timestamptz,
    password_changed_at timestamptz NOT NULL DEFAULT now(),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ym_auth_accounts_user_no_lower
    ON "YMES".ym_auth_accounts (lower(user_no));

CREATE TABLE IF NOT EXISTS "YMES".ym_auth_audit_events (
    event_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_no varchar(10),
    event_type varchar(40) NOT NULL,
    succeeded boolean NOT NULL,
    environment_name varchar(30) NOT NULL,
    client_ip varchar(45),
    user_agent varchar(512),
    occurred_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ym_auth_audit_events_occurred_at
    ON "YMES".ym_auth_audit_events (occurred_at DESC);

COMMENT ON TABLE "YMES".ym_auth_accounts IS
    'YMES authentication accounts. Passwords are stored only as salted adaptive hashes.';
COMMENT ON COLUMN "YMES".ym_auth_accounts.password_hash IS
    'Versioned ASP.NET Core Identity PBKDF2-HMAC-SHA256 hash; never store plaintext.';
COMMENT ON TABLE "YMES".ym_auth_audit_events IS
    'Authentication and password-change audit events; credentials are never recorded.';

COMMIT;

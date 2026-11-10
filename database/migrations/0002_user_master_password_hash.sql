BEGIN;

ALTER TABLE "YMES".tblusruserbasis
    ALTER COLUMN password TYPE varchar(255);

COMMENT ON COLUMN "YMES".tblusruserbasis.password IS
    'ASP.NET Core Identity versioned PBKDF2-HMAC-SHA256 password hash; never store plaintext.';

ALTER TABLE "YMES".ym_auth_accounts
    ALTER COLUMN password_hash DROP NOT NULL;

COMMENT ON COLUMN "YMES".ym_auth_accounts.password_hash IS
    'Deprecated and unused. The credential hash is stored in tblusruserbasis.password.';

ALTER TABLE "YMES".ym_auth_accounts
    ADD CONSTRAINT fk_ym_auth_accounts_user_master
    FOREIGN KEY (user_no)
    REFERENCES "YMES".tblusruserbasis (userno)
    ON UPDATE CASCADE
    ON DELETE CASCADE;

COMMIT;

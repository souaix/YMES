BEGIN;

COMMENT ON COLUMN "YMES".tblusruserbasis.password IS
    'ASP.NET Core Identity IdentityV3 versioned PBKDF2-HMAC-SHA512 password hash; never store plaintext.';

COMMENT ON TABLE "YMES".ym_auth_accounts IS
    'Authentication metadata; the credential hash is stored in tblusruserbasis.password.';

COMMIT;

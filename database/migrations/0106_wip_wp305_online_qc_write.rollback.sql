-- Rollback WP305 writer privileges only when the API idempotency ledger is unused.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".ym_wp305_request') IS NULL THEN
        RAISE EXCEPTION 'WP305 idempotency ledger is missing; inspect manually before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".ym_wp305_request LIMIT 1) THEN
        RAISE EXCEPTION 'WP305 request ledger contains committed operations; refusing destructive rollback';
    END IF;
END
$guard$;

REVOKE INSERT ON "YMES".tblwipeqpqclistlog FROM ymes_app;
REVOKE INSERT ON "YMES".tblwipeqpqclistdetail FROM ymes_app;
REVOKE UPDATE (status, qcserialno) ON "YMES".tblwipeqpqctask FROM ymes_app;
REVOKE SELECT, INSERT ON "YMES".ym_wp305_request FROM ymes_app;
DROP TABLE "YMES".ym_wp305_request;

COMMIT;

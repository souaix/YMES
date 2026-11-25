-- Revoke the narrow IV501 lookup permission if the API is removed.
BEGIN;

REVOKE SELECT ON "YMES".tblinvinventorybasis FROM ymes_app;

COMMIT;

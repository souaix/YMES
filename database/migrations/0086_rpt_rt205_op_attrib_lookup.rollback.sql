BEGIN;

REVOKE SELECT ON TABLE "YMES".tblopattrib FROM ymes_app;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblopattrib LIMIT 1) THEN
        RAISE EXCEPTION 'YMES.tblopattrib is not empty; preserve rows and do not roll back migration 0086';
    END IF;
END
$$;

DROP TABLE "YMES".tblopattrib;

COMMIT;

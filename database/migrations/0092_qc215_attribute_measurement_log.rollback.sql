BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblspcattbasislog') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblspcattbasislog is missing; rollback 0092 does not match this database';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblspcattbasislog LIMIT 1) THEN
        RAISE EXCEPTION 'YMES.tblspcattbasislog contains records; rollback 0092 refuses to discard them';
    END IF;
END
$$;

REVOKE SELECT ON "YMES".tblspcattbasislog FROM ymes_app;
DROP TABLE "YMES".tblspcattbasislog;

COMMIT;

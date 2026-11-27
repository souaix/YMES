-- Rollback only if the source consumable-basis table is still empty.
BEGIN;

DO $rollback_guard$
BEGIN
    IF to_regclass('"YMES".tbleqpconsumebasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tbleqpconsumebasis is missing; inspect manually before rollback';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tbleqpconsumebasis LIMIT 1) THEN
        RAISE EXCEPTION 'YMES.tbleqpconsumebasis contains rows; refusing destructive rollback';
    END IF;
END
$rollback_guard$;

DROP INDEX "YMES".ix_wp304_consume_basis_lookup;
DROP TABLE "YMES".tbleqpconsumebasis;

COMMIT;

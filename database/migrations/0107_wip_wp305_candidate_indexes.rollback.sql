-- Drops only the two YMES-only WP305 query indexes; no data or source table is removed.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".ix_wp305_qctask_wait_candidate') IS NULL
       OR to_regclass('"YMES".ix_wp305_qctask_recheck_candidate') IS NULL THEN
        RAISE EXCEPTION 'WP305 candidate index set is incomplete; inspect manually before rollback';
    END IF;
END
$guard$;

DROP INDEX "YMES".ix_wp305_qctask_recheck_candidate;
DROP INDEX "YMES".ix_wp305_qctask_wait_candidate;

COMMIT;

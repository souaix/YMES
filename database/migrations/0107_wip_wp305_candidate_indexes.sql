-- YMES-only candidate access paths for WP305.
-- Oracle source has only the QCTaskNo primary-key index; these indexes support the new API filters.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tblwipeqpqctask') IS NULL THEN
        RAISE EXCEPTION 'YMES.TBLWIPEQPQCTASK is missing; refusing WP305 candidate index migration';
    END IF;
    IF to_regclass('"YMES".ix_wp305_qctask_wait_candidate') IS NOT NULL
       OR to_regclass('"YMES".ix_wp305_qctask_recheck_candidate') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP305 candidate index already exists; inspect its definition before migrating';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE c.relname IN ('ix_wp305_qctask_wait_candidate','ix_wp305_qctask_recheck_candidate')
          AND c.relkind='i' AND n.nspname<>'YMES'
    ) THEN
        RAISE EXCEPTION 'A WP305 candidate index name exists outside YMES; inspect before migrating';
    END IF;
END
$guard$;

CREATE INDEX ix_wp305_qctask_wait_candidate
    ON "YMES".tblwipeqpqctask
       (equipmentno, belongdate, qcno, insptype, inspstarttime, planqctime DESC NULLS LAST, qctaskno)
    WHERE qctaskclass=1 AND status=0;

CREATE INDEX ix_wp305_qctask_recheck_candidate
    ON "YMES".tblwipeqpqctask
       (equipmentno, qcno, insptype, inspstarttime, planqctime DESC NULLS LAST, qctaskno)
    WHERE qctaskclass=1 AND status=2;

COMMENT ON INDEX "YMES".ix_wp305_qctask_wait_candidate IS
    'YMES-only WP305 wait/check-in candidate lookup; not an MES source index';
COMMENT ON INDEX "YMES".ix_wp305_qctask_recheck_candidate IS
    'YMES-only WP305 recheck candidate lookup; not an MES source index';

COMMIT;

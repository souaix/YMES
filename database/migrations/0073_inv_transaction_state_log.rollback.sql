-- Rollback for migration 0073. Refuse to drop transaction records or queue rows.
BEGIN;

DO $guard$
DECLARE
    object_name text;
    has_rows boolean;
BEGIN
    FOREACH object_name IN ARRAY ARRAY['tblinvtransactionstate','tblinvtransactionlog'] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing partial rollback', object_name;
        END IF;
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)', 'YMES', object_name) INTO has_rows;
        IF has_rows THEN
            RAISE EXCEPTION 'YMES.% contains rows; refusing to discard inventory transaction data', object_name;
        END IF;
    END LOOP;
END
$guard$;

DROP TABLE "YMES".tblinvtransactionlog;
DROP TABLE "YMES".tblinvtransactionstate;

COMMIT;

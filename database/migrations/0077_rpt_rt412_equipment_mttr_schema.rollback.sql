-- Rollback is permitted only while all three RT412 structures remain empty.
-- Never discard equipment configuration or live equipment-state records.
BEGIN;

DO $guard$
DECLARE
    object_name text;
    has_rows boolean;
BEGIN
    FOREACH object_name IN ARRAY ARRAY[
        'tbleqpstatecontrol','tblemsequipmentstate','tblemsequipmentstatelog'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing partial RT412 rollback', object_name;
        END IF;
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)', 'YMES', object_name) INTO has_rows;
        IF has_rows THEN
            RAISE EXCEPTION 'YMES.% contains rows; refusing to discard operational data', object_name;
        END IF;
    END LOOP;
END
$guard$;

DROP TABLE "YMES".tblemsequipmentstatelog;
DROP TABLE "YMES".tblemsequipmentstate;
DROP TABLE "YMES".tbleqpstatecontrol;

COMMIT;

-- Rollback for 0076 is allowed only while every created support table is empty.
-- Never discard WIP, MO, inventory or component business rows.
BEGIN;

DO $guard$
DECLARE
    object_name text;
    has_rows boolean;
BEGIN
    FOREACH object_name IN ARRAY ARRAY[
        'tblinvwipinventory_raw','tblinvsemiinventory',
        'tblinvsemiinventorycomponent','tbloemomaterialstate',
        'tbloemomtlstate_component','tbloemocomponent'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing partial 0076 rollback', object_name;
        END IF;
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)', 'YMES', object_name) INTO has_rows;
        IF has_rows THEN
            RAISE EXCEPTION 'YMES.% contains rows; refusing to discard operational data', object_name;
        END IF;
    END LOOP;
END
$guard$;

DROP TABLE "YMES".tbloemocomponent;
DROP TABLE "YMES".tbloemomtlstate_component;
DROP TABLE "YMES".tbloemomaterialstate;
DROP TABLE "YMES".tblinvsemiinventorycomponent;
DROP TABLE "YMES".tblinvsemiinventory;
DROP TABLE "YMES".tblinvwipinventory_raw;

COMMIT;

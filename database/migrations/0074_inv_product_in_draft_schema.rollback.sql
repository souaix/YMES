-- Rollback for 0074 is allowed only while every created table remains empty.
-- Never discard inventory or draft business rows.
BEGIN;

DO $guard$
DECLARE
    object_name text;
    has_rows boolean;
BEGIN
    FOREACH object_name IN ARRAY ARRAY[
        'tblinvfgdinbasis','tblinvfgdindetail','tblinvfgdincomponent',
        'tblinvsemiinbasis','tblinvsemiindetail','tblinvsemiincomponent',
        'tblinvwipinventory_semi','tblinvwipinventory_component',
        'tblinvfgdinventory','tblinvfgdinventorycomponent',
        'tblwipmolinepackrelation','tblprdpackingrulebasis','tblsysbin',
        'tblwipreversehistory','tblwipgoodbinstate'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing partial 0074 rollback', object_name;
        END IF;
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)', 'YMES', object_name) INTO has_rows;
        IF has_rows THEN
            RAISE EXCEPTION 'YMES.% contains rows; refusing to discard inventory data', object_name;
        END IF;
    END LOOP;
END
$guard$;

DROP TABLE "YMES".tblinvwipgoodbinstate;
DROP TABLE "YMES".tblwipreversehistory;
DROP TABLE "YMES".tblsysbin;
DROP TABLE "YMES".tblprdpackingrulebasis;
DROP TABLE "YMES".tblwipmolinepackrelation;
DROP TABLE "YMES".tblinvfgdinventorycomponent;
DROP TABLE "YMES".tblinvfgdinventory;
DROP TABLE "YMES".tblinvwipinventory_component;
DROP TABLE "YMES".tblinvwipinventory_semi;
DROP TABLE "YMES".tblinvsemiincomponent;
DROP TABLE "YMES".tblinvsemiindetail;
DROP TABLE "YMES".tblinvsemiinbasis;
DROP TABLE "YMES".tblinvfgdincomponent;
DROP TABLE "YMES".tblinvfgdindetail;
DROP TABLE "YMES".tblinvfgdinbasis;

COMMIT;

-- Rollback for migration 0072.
-- Refuse to remove any table that contains synchronized MES business rows.
BEGIN;

DO $guard$
DECLARE
    table_name text;
    has_rows boolean;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'tblwiperfholdreason','tblwiperfholdreasondisp','tblwiperfdisposition',
        'tblwiperflotdisposition','tblwiperf_attachfile','tblwiperfocap_attachfile',
        'tblwiperfocapdisposition'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',table_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing partial rollback', table_name;
        END IF;
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I)', 'YMES', table_name) INTO has_rows;
        IF has_rows THEN
            RAISE EXCEPTION 'YMES.% contains data; refusing rollback', table_name;
        END IF;
    END LOOP;
END
$guard$;

DROP TABLE "YMES".tblwiperfocapdisposition;
DROP TABLE "YMES".tblwiperfocap_attachfile;
DROP TABLE "YMES".tblwiperf_attachfile;
DROP TABLE "YMES".tblwiperflotdisposition;
DROP TABLE "YMES".tblwiperfdisposition;
DROP TABLE "YMES".tblwiperfholdreasondisp;
DROP TABLE "YMES".tblwiperfholdreason;
REVOKE SELECT ON TABLE "YMES".tblprsnodebasis FROM ymes_app;

COMMIT;

BEGIN;

DO $$
DECLARE
    table_name text;
    row_count bigint;
BEGIN
    FOREACH table_name IN ARRAY ARRAY[
        'tblqccalculatescript',
        'xx_tblqcitem_memo',
        'tblwiptemp_qcitem',
        'tblqcsamplingplan_wafer'
    ] LOOP
        IF to_regclass(format('%I.%I', 'YMES', table_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; verify migration 0089 state before rollback', table_name;
        END IF;
        EXECUTE format('SELECT count(*) FROM %I.%I', 'YMES', table_name) INTO row_count;
        IF row_count <> 0 THEN
            RAISE EXCEPTION 'YMES.% contains % rows; rollback is blocked to preserve data', table_name, row_count;
        END IF;
    END LOOP;
    IF to_regclass('"YMES".tblqcitemgroupbasis') IS NULL
       OR to_regclass('"YMES".tblqcitemgroupdetail') IS NULL THEN
        RAISE EXCEPTION 'QC212 group tables are missing; verify schema before rollback';
    END IF;
END
$$;

REVOKE INSERT, UPDATE, DELETE ON "YMES".tblqcitemgroupbasis, "YMES".tblqcitemgroupdetail FROM ymes_app;
REVOKE ALL ON "YMES".tblqccalculatescript, "YMES".xx_tblqcitem_memo,
              "YMES".tblwiptemp_qcitem, "YMES".tblqcsamplingplan_wafer FROM ymes_app;

DROP TABLE "YMES".tblqcsamplingplan_wafer;
DROP TABLE "YMES".tblwiptemp_qcitem;
DROP TABLE "YMES".xx_tblqcitem_memo;
DROP TABLE "YMES".tblqccalculatescript;

COMMIT;

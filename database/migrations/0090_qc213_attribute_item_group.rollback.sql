BEGIN;

DO $$
DECLARE
    table_name text;
    row_count bigint;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['tblqcitemattgorderdetail', 'tblopgqcitemdetail', 'tblprdqcitem'] LOOP
        IF to_regclass(format('%I.%I', 'YMES', table_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; verify migration 0090 before rollback', table_name;
        END IF;
        EXECUTE format('SELECT count(*) FROM %I.%I', 'YMES', table_name) INTO row_count;
        IF row_count <> 0 THEN
            RAISE EXCEPTION 'YMES.% contains % rows; rollback is blocked to preserve data', table_name, row_count;
        END IF;
    END LOOP;
    IF to_regclass('"YMES".tblqcitemattgrouporderbasis') IS NULL
       OR to_regclass('"YMES".tblwiptemp_qcitem') IS NULL THEN
        RAISE EXCEPTION 'QC213 existing group basis or WIP guard is missing; verify schema before rollback';
    END IF;
END
$$;

REVOKE INSERT, UPDATE, DELETE ON "YMES".tblqcitemattgrouporderbasis FROM ymes_app;
REVOKE SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemattgorderdetail FROM ymes_app;
REVOKE DELETE ON "YMES".tblopgqcitemdetail, "YMES".tblprdqcitem FROM ymes_app;

DROP TABLE "YMES".tblprdqcitem;
DROP TABLE "YMES".tblopgqcitemdetail;
DROP TABLE "YMES".tblqcitemattgorderdetail;

COMMIT;

BEGIN;

DO $$
DECLARE
    v_column record;
BEGIN
    IF to_regclass('"YMES".tblusrdepartmentbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblusrdepartmentbasis is missing; inspect the live schema before rollback';
    END IF;

    FOR v_column IN
        SELECT expected.column_name,
               COALESCE(pg_get_expr(d.adbin, d.adrelid), '') AS actual_default
        FROM (VALUES ('issuestate'), ('controldtconect'), ('dtconectqty'), ('datastamp'))
            AS expected(column_name)
        JOIN pg_namespace n ON n.nspname = 'YMES'
        JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = 'tblusrdepartmentbasis'
        JOIN pg_attribute a ON a.attrelid = c.oid AND a.attname = expected.column_name
                              AND a.attnum > 0 AND NOT a.attisdropped
        LEFT JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
    LOOP
        IF v_column.actual_default NOT IN ('0', '0::numeric') THEN
            RAISE EXCEPTION 'YMES.tblusrdepartmentbasis.% default is not migration 0087 value 0; preserve current schema',
                v_column.column_name;
        END IF;
    END LOOP;
END
$$;

ALTER TABLE "YMES".tblusrdepartmentbasis
    ALTER COLUMN issuestate DROP DEFAULT,
    ALTER COLUMN controldtconect DROP DEFAULT,
    ALTER COLUMN dtconectqty DROP DEFAULT,
    ALTER COLUMN datastamp DROP DEFAULT;

COMMIT;

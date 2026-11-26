BEGIN;

DO $$
DECLARE
    v_column record;
BEGIN
    IF to_regclass('"YMES".tblusrdepartmentbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblusrdepartmentbasis is missing; inspect the live schema before applying migration 0087';
    END IF;

    FOR v_column IN
        SELECT expected.column_name, expected.expected_type, expected.expected_not_null,
               format_type(a.atttypid, a.atttypmod) AS actual_type,
               a.attnotnull AS actual_not_null,
               COALESCE(pg_get_expr(d.adbin, d.adrelid), '') AS actual_default
        FROM (VALUES
            ('issuestate', 'numeric(1,0)', true),
            ('controldtconect', 'numeric(1,0)', true),
            ('dtconectqty', 'numeric(3,0)', true),
            ('datastamp', 'numeric(11,0)', false)
        ) AS expected(column_name, expected_type, expected_not_null)
        LEFT JOIN pg_namespace n ON n.nspname = 'YMES'
        LEFT JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = 'tblusrdepartmentbasis'
        LEFT JOIN pg_attribute a ON a.attrelid = c.oid AND a.attname = expected.column_name
                                      AND a.attnum > 0 AND NOT a.attisdropped
        LEFT JOIN pg_attrdef d ON d.adrelid = c.oid AND d.adnum = a.attnum
    LOOP
        IF v_column.actual_type IS DISTINCT FROM v_column.expected_type
           OR v_column.actual_not_null IS DISTINCT FROM v_column.expected_not_null THEN
            RAISE EXCEPTION 'YMES.tblusrdepartmentbasis.% definition differs from the verified Oracle source; expected % NOT NULL=%',
                v_column.column_name, v_column.expected_type, v_column.expected_not_null;
        END IF;
        IF v_column.actual_default NOT IN ('', '0', '0::numeric') THEN
            RAISE EXCEPTION 'YMES.tblusrdepartmentbasis.% has unexpected default %; review before migration 0087',
                v_column.column_name, v_column.actual_default;
        END IF;
    END LOOP;
END
$$;

-- Match the verified MESTEST Oracle defaults; existing rows are unchanged.
ALTER TABLE "YMES".tblusrdepartmentbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN controldtconect SET DEFAULT 0,
    ALTER COLUMN dtconectqty SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

COMMIT;

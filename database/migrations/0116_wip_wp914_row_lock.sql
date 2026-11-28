-- WP914 needs row locks on source MES rows without broad UPDATE grants to ymes_app.
-- This bounded SECURITY DEFINER helper locks only the exact selected eligible rows.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP914:0116', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0116 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE')
       OR has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app schema permissions are not in the expected state; refusing 0116';
    END IF;
    IF NOT has_table_privilege('ymes_app', '"YMES".tblwiplotbasis', 'SELECT')
       OR NOT has_table_privilege('ymes_app', '"YMES".tblwiplotstate', 'SELECT')
       OR NOT has_column_privilege('ymes_app', '"YMES".tblwiplotbasis', 'hotlot', 'UPDATE')
       OR NOT has_column_privilege('ymes_app', '"YMES".tblwiplotbasis', 'priority', 'UPDATE') THEN
        RAISE EXCEPTION 'WP914 source or update grants differ from expected; refusing 0116';
    END IF;
    IF to_regprocedure('"YMES".ym_wp914_lock_selected_lots(character varying[],character varying[],character varying[],numeric[],numeric[])') IS NOT NULL
       OR EXISTS (
            SELECT 1 FROM pg_catalog.pg_proc p
            WHERE lower(p.proname) = 'ym_wp914_lock_selected_lots'
              AND p.oid <> COALESCE(to_regprocedure('"YMES".ym_wp914_lock_selected_lots(character varying[],character varying[],character varying[],numeric[],numeric[])')::oid, 0)
       ) THEN
        RAISE EXCEPTION 'A WP914 lock routine already exists; inspect before 0116';
    END IF;
END
$guard$;

CREATE FUNCTION "YMES".ym_wp914_lock_selected_lots(
    p_lotnos character varying[],
    p_baselotnos character varying[],
    p_opnos character varying[],
    p_statuses numeric[],
    p_versions numeric[]
)
RETURNS TABLE(lotno character varying, baselotno character varying,
              opno character varying, status numeric, lotstamp numeric)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = pg_catalog, "YMES"
AS $function$
DECLARE
    selected_count integer;
BEGIN
    IF p_lotnos IS NULL OR p_baselotnos IS NULL OR p_opnos IS NULL
       OR p_statuses IS NULL OR p_versions IS NULL THEN
        RAISE EXCEPTION 'WP914 selected lot arrays are required' USING ERRCODE = '22023';
    END IF;
    selected_count := cardinality(p_lotnos);
    IF selected_count NOT BETWEEN 1 AND 200
       OR cardinality(p_baselotnos) <> selected_count
       OR cardinality(p_opnos) <> selected_count
       OR cardinality(p_statuses) <> selected_count
       OR cardinality(p_versions) <> selected_count THEN
        RAISE EXCEPTION 'WP914 selected lot arrays must have equal lengths from 1 through 200'
            USING ERRCODE = '22023';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM unnest(p_lotnos, p_baselotnos, p_opnos, p_statuses, p_versions)
             AS r(lotno, baselotno, opno, status, version)
        WHERE lotno IS NULL OR btrim(lotno) = ''
           OR baselotno IS NULL OR btrim(baselotno) = ''
           OR opno IS NULL OR btrim(opno) = ''
           OR status IS NULL OR status NOT BETWEEN 0 AND 5
           OR version IS NULL OR version < 0
    ) THEN
        RAISE EXCEPTION 'WP914 selected lot keys or versions are invalid' USING ERRCODE = '22023';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM unnest(p_lotnos, p_baselotnos, p_opnos, p_statuses, p_versions)
             AS r(lotno, baselotno, opno, status, version)
        GROUP BY lotno, opno, status
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'WP914 selected lot keys must be unique' USING ERRCODE = '22023';
    END IF;

    RETURN QUERY
    WITH requested AS (
        SELECT * FROM unnest(p_lotnos, p_baselotnos, p_opnos, p_statuses, p_versions)
            AS r(lotno, baselotno, opno, status, version)
    )
    SELECT s.lotno, s.baselotno, s.opno, s.status, s.lotstamp
    FROM requested r
    JOIN "YMES".tblwiplotstate s
      ON s.lotno = r.lotno AND s.baselotno = r.baselotno
     AND s.opno = r.opno AND s.status = r.status AND s.lotstamp = r.version
    JOIN "YMES".tblwiplotbasis b ON b.baselotno = s.baselotno
    WHERE b.lotstate = 1 AND s.status BETWEEN 0 AND 5
    ORDER BY s.baselotno COLLATE "C", s.lotno COLLATE "C", s.opno COLLATE "C", s.status
    FOR UPDATE OF s, b;
END
$function$;

COMMENT ON FUNCTION "YMES".ym_wp914_lock_selected_lots(
    character varying[], character varying[], character varying[], numeric[], numeric[]
) IS 'YMES-only bounded WP914 helper; locks exact eligible MES Lot rows under the database owner without broad ymes_app UPDATE grants';

REVOKE ALL PRIVILEGES ON FUNCTION "YMES".ym_wp914_lock_selected_lots(
    character varying[], character varying[], character varying[], numeric[], numeric[]
) FROM PUBLIC, ymes_app;
GRANT EXECUTE ON FUNCTION "YMES".ym_wp914_lock_selected_lots(
    character varying[], character varying[], character varying[], numeric[], numeric[]
) TO ymes_app;

COMMIT;

-- Qualify record fields in the WP914 lock function to avoid PL/pgSQL output-name ambiguity.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP914:0117', 0));

DO $guard$
DECLARE
    routine_oid oid;
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0117 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    routine_oid := to_regprocedure('"YMES".ym_wp914_lock_selected_lots(character varying[],character varying[],character varying[],numeric[],numeric[])');
    IF routine_oid IS NULL
       OR (SELECT prosecdef FROM pg_catalog.pg_proc WHERE oid = routine_oid) IS NOT TRUE
       OR (SELECT proowner FROM pg_catalog.pg_proc WHERE oid = routine_oid) <> 'postgres'::regrole
       OR NOT has_function_privilege('ymes_app', routine_oid, 'EXECUTE')
       OR has_table_privilege('ymes_app', '"YMES".tblwiplotstate', 'UPDATE') THEN
        RAISE EXCEPTION 'WP914 lock routine or minimum grants differ from expected; refusing 0117';
    END IF;
END
$guard$;

CREATE OR REPLACE FUNCTION "YMES".ym_wp914_lock_selected_lots(
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
        WHERE r.lotno IS NULL OR btrim(r.lotno) = ''
           OR r.baselotno IS NULL OR btrim(r.baselotno) = ''
           OR r.opno IS NULL OR btrim(r.opno) = ''
           OR r.status IS NULL OR r.status NOT BETWEEN 0 AND 5
           OR r.version IS NULL OR r.version < 0
    ) THEN
        RAISE EXCEPTION 'WP914 selected lot keys or versions are invalid' USING ERRCODE = '22023';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM unnest(p_lotnos, p_baselotnos, p_opnos, p_statuses, p_versions)
             AS r(lotno, baselotno, opno, status, version)
        GROUP BY r.lotno, r.opno, r.status
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

COMMIT;

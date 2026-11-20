-- PREPARED ONLY; NOT EXECUTED. Reusable in the Root DDL transaction, then
-- again in an INDEPENDENT BEGIN READ ONLY transaction after actual commit.
-- Only metadata and WHERE-false permission probes; no business-row count/read.
DO $wp109_0062_native$
DECLARE v62_spec record; v62_field record; v62_attr record; v62_idxattr record; v62_parentattr record;
 v62_rel oid; v62_index oid; v62_app oid; v62_owner oid; v62_collation oid;
 v62_count bigint; v62_actual_acl jsonb; v62_expected_acl jsonb;
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 SELECT r.oid INTO v62_app FROM pg_catalog.pg_roles r WHERE r.rolname='ymes_app';
 SELECT r.oid INTO v62_owner FROM pg_catalog.pg_roles r WHERE r.rolname='postgres';
 SELECT co.oid INTO v62_collation FROM pg_catalog.pg_collation co
  JOIN pg_catalog.pg_namespace ns ON ns.oid=co.collnamespace
  WHERE ns.nspname='pg_catalog' AND co.collname='default' AND co.collisdeterministic;
 IF v62_app IS NULL OR v62_owner IS NULL OR v62_collation IS NULL
  OR NOT has_schema_privilege(v62_app,'YMES','USAGE') THEN
  RAISE EXCEPTION 'Existing owner/app/default-collation/schema USAGE absent'; END IF;
 FOR v62_spec IN SELECT * FROM (VALUES
  ('tblwipswrbasis',15,'pk_tblwipswrbasis',ARRAY[1]::smallint[],1,true),
  ('tblwipswrdetail',2,'pk_tblwipswrdetail',ARRAY[1,2]::smallint[],2,true),
  ('tblsysserialno',2,'pk_tblsysserialno',ARRAY[1]::smallint[],1,false)
 ) AS s(tbl,cols,pkname,keynums,keycount,reader_select) LOOP
  -- All kinds/cases/schemas are counted; only ONE exact ordinary table allowed.
  IF (SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace ns ON ns.oid=c.relnamespace
    WHERE ns.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=v62_spec.tbl)<>1 THEN
   RAISE EXCEPTION 'Ambiguous/missing table name: %',v62_spec.tbl; END IF;
  SELECT c.oid INTO v62_rel FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace ns ON ns.oid=c.relnamespace
   WHERE ns.nspname='YMES' AND c.relname=v62_spec.tbl AND c.relkind='r' AND c.relpersistence='p'
    AND c.relowner=v62_owner AND NOT c.relrowsecurity AND NOT c.relforcerowsecurity
    AND NOT c.relispartition AND NOT c.relhasrules AND c.reloptions IS NULL;
  IF v62_rel IS NULL OR EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=v62_rel OR i.inhparent=v62_rel)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_trigger g WHERE g.tgrelid=v62_rel)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_policy p WHERE p.polrelid=v62_rel)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.contype='f' AND (k.conrelid=v62_rel OR k.confrelid=v62_rel)) THEN
   RAISE EXCEPTION 'Native relation owner/kind/partition/RLS/rule/trigger/FK differs: %',v62_spec.tbl; END IF;
  SELECT count(*) INTO v62_count FROM pg_catalog.pg_attribute a WHERE a.attrelid=v62_rel AND a.attnum>0;
  IF v62_count<>v62_spec.cols OR EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a WHERE a.attrelid=v62_rel
    AND a.attnum>0 AND (a.attisdropped OR a.attidentity<>'' OR a.attgenerated<>'' OR a.atthasdef OR a.attacl IS NOT NULL))
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_attrdef d WHERE d.adrelid=v62_rel) THEN
   RAISE EXCEPTION 'Physical column count/drop/identity/generated/default/columnACL differs: %',v62_spec.tbl; END IF;
  FOR v62_field IN SELECT * FROM (VALUES
   ('tblwipswrbasis',1,'swrno','character varying(20)',true),
   ('tblwipswrbasis',2,'swrtype','numeric(1,0)',true),
   ('tblwipswrbasis',3,'status','numeric(2,0)',true),
   ('tblwipswrbasis',4,'customerno','character varying(50)',true),
   ('tblwipswrbasis',5,'opno','character varying(20)',true),
   ('tblwipswrbasis',6,'deviceno','character varying(50)',false),
   ('tblwipswrbasis',7,'starttime','timestamp without time zone',false),
   ('tblwipswrbasis',8,'endtime','timestamp without time zone',false),
   ('tblwipswrbasis',9,'customerlotno','character varying(50)',false),
   ('tblwipswrbasis',10,'mono','character varying(50)',false),
   ('tblwipswrbasis',11,'attachfile','bytea',false),
   ('tblwipswrbasis',12,'swrdesc','character varying(400)',true),
   ('tblwipswrbasis',13,'creator','character varying(50)',false),
   ('tblwipswrbasis',14,'createdate','timestamp without time zone',false),
   ('tblwipswrbasis',15,'attachfilepath','character varying(200)',false),
   ('tblwipswrdetail',1,'swrno','character varying(20)',true),
   ('tblwipswrdetail',2,'lotno','character varying(50)',true),
   ('tblsysserialno',1,'functionname','character varying(20)',true),
   ('tblsysserialno',2,'serialno','numeric(9,0)',true)
  ) AS e(tbl,ord,col,typ,nn) WHERE e.tbl=v62_spec.tbl LOOP
   SELECT a.attname,format_type(a.atttypid,a.atttypmod) AS typ,a.attnotnull,a.attcollation INTO v62_attr
    FROM pg_catalog.pg_attribute a WHERE a.attrelid=v62_rel AND a.attnum=v62_field.ord;
   IF NOT FOUND OR v62_attr.attname IS DISTINCT FROM v62_field.col OR v62_attr.typ IS DISTINCT FROM v62_field.typ
    OR v62_attr.attnotnull IS DISTINCT FROM v62_field.nn
    OR v62_attr.attcollation IS DISTINCT FROM (CASE WHEN v62_field.typ LIKE 'character varying%' THEN v62_collation ELSE 0::oid END) THEN
    RAISE EXCEPTION 'Native ordinal/name/type/null/collation differs: %.%',v62_spec.tbl,v62_field.col; END IF;
  END LOOP;
  -- Serial has NULL raw relacl by default; acldefault is a NONEMPTY owner ACL.
  -- An explicit empty ACL is passed as NULL to strict aclexplode, never a 0-D array.
  SELECT coalesce(jsonb_agg(jsonb_build_array(x.grantor,x.grantee,x.privilege_type,x.is_grantable)
    ORDER BY x.grantor,x.grantee,x.privilege_type,x.is_grantable),'[]'::jsonb) INTO v62_actual_acl
   FROM pg_catalog.pg_class c CROSS JOIN LATERAL aclexplode(CASE
    WHEN c.relacl IS NULL THEN acldefault('r',c.relowner)
    WHEN cardinality(c.relacl)=0 THEN NULL::aclitem[] ELSE c.relacl END) AS x WHERE c.oid=v62_rel;
  SELECT jsonb_agg(jsonb_build_array(v62_owner,q.grantee,q.privilege,false)
    ORDER BY v62_owner,q.grantee,q.privilege) INTO v62_expected_acl FROM (
    SELECT v62_owner AS grantee,unnest(ARRAY['DELETE','INSERT','REFERENCES','SELECT','TRIGGER','TRUNCATE','UPDATE']) AS privilege
    UNION ALL SELECT v62_app,'SELECT' WHERE v62_spec.reader_select
   ) AS q;
  IF v62_actual_acl IS DISTINCT FROM v62_expected_acl
   OR has_table_privilege(v62_app,v62_rel,'SELECT') IS DISTINCT FROM v62_spec.reader_select
   OR has_any_column_privilege(v62_app,v62_rel,'SELECT') IS DISTINCT FROM v62_spec.reader_select
   OR has_table_privilege(v62_app,v62_rel,'INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER') THEN
   RAISE EXCEPTION 'Only owner ACL plus reader SELECT on Basis/Detail allowed; no app Serial grant: %',v62_spec.tbl; END IF;
  IF (SELECT count(*) FROM pg_catalog.pg_constraint k WHERE k.conrelid=v62_rel)<>1
   OR (SELECT count(*) FROM pg_catalog.pg_index i WHERE i.indrelid=v62_rel)<>1
   OR (SELECT count(*) FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace ns ON ns.oid=c.relnamespace
       WHERE ns.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=v62_spec.pkname)<>1 THEN
   RAISE EXCEPTION 'Exactly one native PK/index, no extra constraints or occupied index name: %',v62_spec.tbl; END IF;
  SELECT k.conindid INTO v62_index FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_index i ON i.indexrelid=k.conindid
   JOIN pg_catalog.pg_class idx ON idx.oid=i.indexrelid JOIN pg_catalog.pg_namespace ns ON ns.oid=idx.relnamespace
   JOIN pg_catalog.pg_am am ON am.oid=idx.relam
   WHERE k.conrelid=v62_rel AND k.contype='p' AND k.conname=v62_spec.pkname AND k.conkey=v62_spec.keynums
    AND k.convalidated AND NOT k.condeferrable AND NOT k.condeferred
    AND i.indisprimary AND i.indisunique AND i.indisvalid AND i.indisready AND i.indislive AND NOT i.indisexclusion
    AND i.indnkeyatts=v62_spec.keycount AND i.indnatts=v62_spec.keycount AND i.indpred IS NULL AND i.indexprs IS NULL
    AND i.indkey::text=array_to_string(v62_spec.keynums,' ')
    AND idx.relname=v62_spec.pkname AND ns.nspname='YMES' AND idx.relkind='i' AND idx.relpersistence='p'
    AND idx.relowner=v62_owner AND idx.relacl IS NULL AND idx.reloptions IS NULL AND am.amname='btree';
  IF v62_index IS NULL THEN RAISE EXCEPTION 'Ordered native PK/index shape differs: %',v62_spec.tbl; END IF;
  IF (SELECT count(*) FROM pg_catalog.pg_attribute a WHERE a.attrelid=v62_index AND a.attnum>0)<>v62_spec.keycount THEN
   RAISE EXCEPTION 'Native PK index physical attribute count differs: %',v62_spec.tbl; END IF;
  FOR v62_idxattr IN SELECT * FROM pg_catalog.pg_attribute a WHERE a.attrelid=v62_index AND a.attnum>0 ORDER BY a.attnum LOOP
   SELECT * INTO v62_parentattr FROM pg_catalog.pg_attribute a
    WHERE a.attrelid=v62_rel AND a.attnum=v62_spec.keynums[v62_idxattr.attnum];
   IF NOT FOUND OR v62_idxattr.attisdropped OR v62_idxattr.attnotnull OR v62_idxattr.attidentity<>'' OR v62_idxattr.attgenerated<>''
    OR v62_idxattr.atthasdef OR v62_idxattr.attacl IS NOT NULL
    OR v62_idxattr.attname IS DISTINCT FROM v62_parentattr.attname
    OR v62_idxattr.atttypid IS DISTINCT FROM v62_parentattr.atttypid OR v62_idxattr.atttypmod IS DISTINCT FROM v62_parentattr.atttypmod
    OR v62_idxattr.attcollation IS DISTINCT FROM v62_parentattr.attcollation THEN
    RAISE EXCEPTION 'Native PK index attribute differs: %',v62_spec.tbl; END IF;
  END LOOP;
  IF (SELECT count(*) FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace ns ON ns.oid=t.typnamespace
      WHERE ns.nspname NOT IN ('pg_catalog','information_schema') AND lower(t.typname)=ANY(ARRAY[v62_spec.tbl,'_'||v62_spec.tbl]))<>2
   OR NOT EXISTS(SELECT 1 FROM pg_catalog.pg_type t JOIN pg_catalog.pg_type ar ON ar.oid=t.typarray
      JOIN pg_catalog.pg_namespace ns ON ns.oid=t.typnamespace JOIN pg_catalog.pg_class c ON c.reltype=t.oid
      WHERE c.oid=v62_rel AND t.typrelid=v62_rel AND t.typname=v62_spec.tbl AND t.typtype='c' AND t.typelem=0
       AND t.typowner=v62_owner AND t.typacl IS NULL AND ns.nspname='YMES'
       AND ar.typname='_'||v62_spec.tbl AND ar.typtype='b' AND ar.typelem=t.oid AND ar.typrelid=0
       AND ar.typnamespace=t.typnamespace AND ar.typowner=v62_owner AND ar.typacl IS NULL) THEN
   RAISE EXCEPTION 'Exact native row/array type pair differs: %',v62_spec.tbl; END IF;
 END LOOP;
END $wp109_0062_native$;

-- App reads precisely the two delivered reader tables, never Serial.
SET LOCAL ROLE ymes_app;
SELECT swrno,swrtype,status,customerno,opno,deviceno,starttime,endtime,customerlotno,mono,
 attachfile,swrdesc,creator,createdate,attachfilepath FROM "YMES".tblwipswrbasis WHERE false;
SELECT swrno,lotno FROM "YMES".tblwipswrdetail WHERE false;
RESET ROLE;
SELECT jsonb_build_object('module','WP109_SCHEMA0062_NATIVE19','status','passed',
 'nativeTables',3,'nativeColumns',19,'pkConstraints',3,'pkIndexes',3,'pkIndexAttributes',4,
 'rowArrayTypes',6,'defaults',0,'appReaderSelectTables',2,'appSerialGrants',0,
 'businessRowsRead',false,'countsAreMetadataOnly',true) AS native19_verification;

-- PREPARED ONLY; NOT EXECUTED. Run AFTER native19 verifier but BEFORE COMMIT
-- in the SAME Root explicit DDL transaction. No broad object-prefix exemption.
DO $wp109_0062_allowlist$
DECLARE v62_before jsonb; v62_actual jsonb; v62_normalized jsonb;
 v62_section text; v62_added text[]; v62_expected text[];
 v62_categories text[] := ARRAY['collations','columns','constraints','database','defaultACL','defaults',
 'eventTriggers','indexes','memberships','policies','relations','roleSettings','roles','schemas','triggers','types'];
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999
  OR current_setting('ymes.wp109_0062_atomic_ready',true) IS DISTINCT FROM txid_current()::text THEN
  RAISE EXCEPTION 'Same reviewed PostgreSQL14 Root atomic transaction required'; END IF;
 v62_before:=nullif(current_setting('ymes.wp109_0062_before',true),'')::jsonb;
 IF v62_before IS NULL THEN RAISE EXCEPTION 'Before snapshot from pre-CREATE guard absent'; END IF;
WITH rels AS (
 SELECT c.*,n.nspname FROM pg_catalog.pg_class c
 JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
 WHERE c.relpersistence<>'t' AND n.nspname NOT IN ('pg_catalog','information_schema')
 AND n.nspname !~ '^pg_(toast|temp)_'
 AND n.nspname<>'pg_toast'
), relation_rows AS (
 SELECT nspname||'.'||relname AS key,
 to_jsonb(c)-ARRAY['nspname','relpages','reltuples','relallvisible','relfrozenxid','relminmxid'] AS value
 FROM rels c
), columns AS (
 SELECT c.nspname||'.'||c.relname||'.'||a.attname AS key,to_jsonb(a) AS value
 FROM rels c JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0
), defaults AS (
 SELECT c.nspname||'.'||c.relname||'.'||a.attname AS key,
 jsonb_build_object('catalog',to_jsonb(d),'expression',pg_get_expr(d.adbin,d.adrelid)) AS value
 FROM rels c JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid
 JOIN pg_catalog.pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
), user_types AS (
 SELECT n.nspname||'.'||t.typname AS key,to_jsonb(t) AS value
 FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
 WHERE n.nspname NOT IN ('pg_catalog','information_schema','pg_toast') AND n.nspname !~ '^pg_(toast|temp)_'
), constraints AS (
 SELECT n.nspname||'.'||coalesce(c.relname,t.typname)||'.'||k.conname AS key,
 jsonb_build_object('catalog',to_jsonb(k),'definition',pg_get_constraintdef(k.oid)) AS value
 FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_namespace n ON n.oid=k.connamespace
 LEFT JOIN pg_catalog.pg_class c ON c.oid=k.conrelid LEFT JOIN pg_catalog.pg_type t ON t.oid=k.contypid
 WHERE n.nspname NOT IN ('pg_catalog','information_schema','pg_toast') AND n.nspname !~ '^pg_(toast|temp)_'
), indexes AS (
 SELECT c.nspname||'.'||c.relname AS key,jsonb_build_object('catalog',to_jsonb(i),'definition',pg_get_indexdef(i.indexrelid)) AS value
 FROM rels c JOIN pg_catalog.pg_index i ON i.indexrelid=c.oid
), triggers AS (
 SELECT c.nspname||'.'||c.relname||'.'||t.tgname AS key,jsonb_build_object('catalog',to_jsonb(t),'definition',pg_get_triggerdef(t.oid)) AS value
 FROM rels c JOIN pg_catalog.pg_trigger t ON t.tgrelid=c.oid
), policies AS (
 SELECT c.nspname||'.'||c.relname||'.'||p.polname AS key,to_jsonb(p) AS value
 FROM rels c JOIN pg_catalog.pg_policy p ON p.polrelid=c.oid
)
SELECT jsonb_build_object(
 'relations',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM relation_rows),
 'columns',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM columns),
 'defaults',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM defaults),
 'types',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM user_types),
 'constraints',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM constraints),
 'indexes',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM indexes),
 'triggers',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM triggers),
 'policies',(SELECT coalesce(jsonb_object_agg(key,value),'{}'::jsonb) FROM policies),
 'schemas',(SELECT jsonb_agg(to_jsonb(n) ORDER BY oid) FROM pg_catalog.pg_namespace n WHERE nspname !~ '^pg_(toast|temp)_'),
 'defaultACL',(SELECT coalesce(jsonb_agg(to_jsonb(d) ORDER BY oid),'[]'::jsonb) FROM pg_catalog.pg_default_acl d),
 'roles',(SELECT jsonb_agg(to_jsonb(r)-'rolpassword' ORDER BY oid) FROM pg_catalog.pg_roles r),
 'memberships',(SELECT coalesce(jsonb_agg(to_jsonb(m) ORDER BY roleid,member,grantor),'[]'::jsonb) FROM pg_catalog.pg_auth_members m),
 'roleSettings',(SELECT coalesce(jsonb_agg(to_jsonb(s) ORDER BY setdatabase,setrole),'[]'::jsonb) FROM pg_catalog.pg_db_role_setting s),
 'eventTriggers',(SELECT coalesce(jsonb_agg(to_jsonb(e) ORDER BY oid),'[]'::jsonb) FROM pg_catalog.pg_event_trigger e),
 'collations',(SELECT jsonb_agg(to_jsonb(c) ORDER BY oid) FROM pg_catalog.pg_collation c),
 'database',(SELECT jsonb_build_object('name',datname,'owner',datdba,'encoding',encoding,'collate',datcollate,'ctype',datctype,'acl',datacl::text) FROM pg_catalog.pg_database WHERE datname=current_database())
) INTO v62_actual;
 IF ARRAY(SELECT z.key FROM jsonb_object_keys(v62_before) AS z(key) ORDER BY z.key) IS DISTINCT FROM v62_categories
  OR ARRAY(SELECT z.key FROM jsonb_object_keys(v62_actual) AS z(key) ORDER BY z.key) IS DISTINCT FROM v62_categories THEN
  RAISE EXCEPTION 'Exact 16-category snapshot shape required'; END IF;
 v62_normalized:=v62_actual;
 FOREACH v62_section IN ARRAY ARRAY['relations','columns','types','constraints','indexes'] LOOP
  SELECT coalesce(array_agg(z.key ORDER BY z.key),ARRAY[]::text[]) INTO v62_added
   FROM jsonb_object_keys(v62_actual->v62_section) AS z(key) WHERE NOT (v62_before->v62_section ? z.key);
  IF v62_section='relations' THEN v62_expected:=ARRAY['YMES.pk_tblsysserialno','YMES.pk_tblwipswrbasis','YMES.pk_tblwipswrdetail','YMES.tblsysserialno','YMES.tblwipswrbasis','YMES.tblwipswrdetail'];
  ELSIF v62_section='columns' THEN v62_expected:=ARRAY['YMES.pk_tblsysserialno.functionname','YMES.pk_tblwipswrbasis.swrno','YMES.pk_tblwipswrdetail.lotno','YMES.pk_tblwipswrdetail.swrno','YMES.tblsysserialno.functionname','YMES.tblsysserialno.serialno','YMES.tblwipswrbasis.attachfile','YMES.tblwipswrbasis.attachfilepath','YMES.tblwipswrbasis.createdate','YMES.tblwipswrbasis.creator','YMES.tblwipswrbasis.customerlotno','YMES.tblwipswrbasis.customerno','YMES.tblwipswrbasis.deviceno','YMES.tblwipswrbasis.endtime','YMES.tblwipswrbasis.mono','YMES.tblwipswrbasis.opno','YMES.tblwipswrbasis.starttime','YMES.tblwipswrbasis.status','YMES.tblwipswrbasis.swrdesc','YMES.tblwipswrbasis.swrno','YMES.tblwipswrbasis.swrtype','YMES.tblwipswrdetail.lotno','YMES.tblwipswrdetail.swrno'];
  ELSIF v62_section='types' THEN v62_expected:=ARRAY['YMES._tblsysserialno','YMES._tblwipswrbasis','YMES._tblwipswrdetail','YMES.tblsysserialno','YMES.tblwipswrbasis','YMES.tblwipswrdetail'];
  ELSIF v62_section='constraints' THEN v62_expected:=ARRAY['YMES.tblsysserialno.pk_tblsysserialno','YMES.tblwipswrbasis.pk_tblwipswrbasis','YMES.tblwipswrdetail.pk_tblwipswrdetail'];
  ELSIF v62_section='indexes' THEN v62_expected:=ARRAY['YMES.pk_tblsysserialno','YMES.pk_tblwipswrbasis','YMES.pk_tblwipswrdetail'];
  ELSE RAISE EXCEPTION 'Unreviewed allowlist section'; END IF;
  SELECT array_agg(q.key ORDER BY q.key) INTO v62_expected FROM unnest(v62_expected) AS q(key);
  IF v62_added IS DISTINCT FROM v62_expected THEN
   RAISE EXCEPTION 'Only exact reviewed SWR additions allowed in %',v62_section; END IF;
  v62_normalized:=jsonb_set(v62_normalized,ARRAY[v62_section],(v62_actual->v62_section)-v62_expected);
 END LOOP;
 -- Defaults/triggers/policies are not removed: even one new item fails.
 -- Existing rows/ACLs/catalog identities, all parents, roles/memberships,
 -- schemas/defaultACL/collations/eventTriggers/database stay byte-value equal.
 IF v62_normalized IS DISTINCT FROM v62_before THEN
  RAISE EXCEPTION 'Unrelated catalog/parentACL/defaultACL/roles changed; abort whole DDL transaction'; END IF;
 PERFORM set_config('ymes.wp109_0062_after',v62_actual::text,true);
END $wp109_0062_allowlist$;
SELECT jsonb_build_object('module','WP109_SCHEMA0062_ALLOWLIST','status','passed',
 'newTables',3,'newPkIndexes',3,'newUserRelations',6,'newNativeColumns',19,'newIndexAttributes',4,
 'newCatalogColumnKeys',23,'newTypes',6,'newPkConstraints',3,'newDefaults',0,
 'preservedCatalogCategories',16,'noBusinessDataChangedByVerifier',true) AS catalog_allowlist;

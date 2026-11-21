-- PREPARED ONLY / NOT EXECUTED. Same-Root-transaction 0063 catalog allow-delta.
-- Root sets the before/atomic GUCs only after fresh catalog/absence checks and
-- verified complete pg_dump/restore-list, then applies exact reviewed0063 SQL.
-- Run native/ACL verifier first. No business rows are read or written here.
DO $et202_0063_allowlist$
DECLARE
 g63_before jsonb;
 g63_actual jsonb;
 g63_normalized jsonb;
 g63_section text;
 g63_added text[];
 g63_expected text[];
 g63_categories text[] := ARRAY['collations','columns','constraints','database','defaultACL','defaults',
  'eventTriggers','indexes','memberships','policies','relations','roleSettings','roles','schemas','triggers','types'];
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
  OR current_setting('ymes.et202_0063_atomic_ready',true) IS DISTINCT FROM txid_current()::text
  OR current_setting('ymes.et202_0063_native_verified',true) IS DISTINCT FROM txid_current()::text THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14 Root same-transaction guards/native verification required'; END IF;
 g63_before := nullif(current_setting('ymes.et202_0063_before',true),'')::jsonb;
 IF g63_before IS NULL THEN RAISE EXCEPTION 'Pre-CREATE whole snapshot absent'; END IF;
 -- Actual audit1c9ac / catalog62d9 has no defaultACL or event triggers. Do not
 -- compensate with REVOKE or blindly allow a later inherited grant/provider.
 IF g63_before->'defaultACL' IS DISTINCT FROM '[]'::jsonb
  OR g63_before->'eventTriggers' IS DISTINCT FROM '[]'::jsonb THEN
  RAISE EXCEPTION 'Reviewed empty defaultACL/eventTrigger baseline changed'; END IF;
-- PREPARED ONLY. A scalar bounded permanent-catalog snapshot; no business rows.
-- TOAST/internal catalogs are managed by PostgreSQL, not new business objects.
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
) INTO g63_actual
;
 IF ARRAY(SELECT entry.key FROM jsonb_object_keys(g63_before) AS entry(key) ORDER BY entry.key) IS DISTINCT FROM g63_categories
  OR ARRAY(SELECT entry.key FROM jsonb_object_keys(g63_actual) AS entry(key) ORDER BY entry.key) IS DISTINCT FROM g63_categories THEN
  RAISE EXCEPTION 'Exact16-category original snapshot required'; END IF;
 g63_normalized := g63_actual;
 FOREACH g63_section IN ARRAY ARRAY['relations','columns','types','constraints','indexes','defaults'] LOOP
  IF jsonb_typeof(g63_before->g63_section) IS DISTINCT FROM 'object'
   OR jsonb_typeof(g63_actual->g63_section) IS DISTINCT FROM 'object' THEN
   RAISE EXCEPTION 'Catalog section shape differs: %',g63_section; END IF;
  SELECT COALESCE(array_agg(entry.key ORDER BY entry.key),ARRAY[]::text[]) INTO g63_added
   FROM jsonb_object_keys(g63_actual->g63_section) AS entry(key)
   WHERE NOT (g63_before->g63_section ? entry.key);
  IF g63_section='relations' THEN g63_expected := ARRAY['YMES.pk_tblentositem','YMES.tblentositem'];
  ELSIF g63_section='columns' THEN g63_expected := ARRAY[
   'YMES.pk_tblentositem.ositemno',
   'YMES.tblentositem.createdate','YMES.tblentositem.creator','YMES.tblentositem.datastamp',
   'YMES.tblentositem.description','YMES.tblentositem.ositemname','YMES.tblentositem.ositemno',
   'YMES.tblentositem.plandays','YMES.tblentositem.processno','YMES.tblentositem.returntype',
   'YMES.tblentositem.revisedate','YMES.tblentositem.reviser'];
  ELSIF g63_section='types' THEN g63_expected := ARRAY['YMES._tblentositem','YMES.tblentositem'];
  ELSIF g63_section='constraints' THEN g63_expected := ARRAY['YMES.tblentositem.pk_tblentositem'];
  ELSIF g63_section='indexes' THEN g63_expected := ARRAY['YMES.pk_tblentositem'];
  ELSIF g63_section='defaults' THEN g63_expected := ARRAY['YMES.tblentositem.datastamp'];
  ELSE RAISE EXCEPTION 'Unreviewed allowlist category'; END IF;
  SELECT array_agg(entry.key ORDER BY entry.key) INTO g63_expected FROM unnest(g63_expected) AS entry(key);
  IF g63_added IS DISTINCT FROM g63_expected THEN
   RAISE EXCEPTION 'Only exact native ET202 additions allowed in %',g63_section; END IF;
  -- Every expected new key must have been absent, even if an attacker inserted
  -- an object before this DDL; existing-row modifications never get removed.
  IF EXISTS(SELECT 1 FROM unnest(g63_expected) AS entry(key) WHERE g63_before->g63_section ? entry.key) THEN
   RAISE EXCEPTION 'Reviewed ET202 key already existed before transaction'; END IF;
  g63_normalized := jsonb_set(g63_normalized,ARRAY[g63_section],(g63_actual->g63_section)-g63_expected);
 END LOOP;
 IF g63_normalized IS DISTINCT FROM g63_before THEN
  RAISE EXCEPTION 'Existing catalog/parentACL/roles/schema/defaultACL changed; abort0063 whole transaction'; END IF;
 PERFORM set_config('ymes.et202_0063_after',g63_actual::text,true);
END;
$et202_0063_allowlist$;
SELECT jsonb_build_object('module','ET202_SCHEMA0063_ALLOWLIST','status','passed',
 'newTables',1,'newPkIndexes',1,'newRelations',2,'newNativeColumns',11,
 'newIndexAttributes',1,'newCatalogColumnKeys',12,'newDerivedTypes',2,
 'newPrimaryKeyConstraints',1,'newDefaults',1,'preservedCatalogCategories',16,
 'targetOnlyExplicitAppCrud4',true,'noBusinessRowsReadOrChangedByVerifier',true) AS catalog_allowlist;

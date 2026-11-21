\set ON_ERROR_STOP on
BEGIN READ ONLY;
DO $wp109_0064_catalog_scope$
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION '0064 catalog review requires PG14 database ymes as postgres'; END IF;
END;
$wp109_0064_catalog_scope$;
SELECT jsonb_build_object('database',current_database(),'role',current_user,'sessionRole',session_user,
 'serverVersion',current_setting('server_version_num'),'port',current_setting('port'),
 'dataDirectory',current_setting('data_directory'),
 'systemIdentifier',(SELECT system_identifier::text FROM pg_control_system())) AS target_identity;
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
) AS reviewed_full16;
ROLLBACK;
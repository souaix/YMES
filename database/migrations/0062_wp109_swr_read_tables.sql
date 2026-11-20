-- PREPARED ONLY; NOT EXECUTED. Transaction fragment, not an autocommit script.
-- Root must use one reviewed BEGIN -> this file -> native verifier -> allowlist
-- -> COMMIT, after full custom backup/restore-list and deployed API172 absent QA.
-- No CREATE SCHEMA, IF NOT EXISTS, defaults, rows, FK, trigger, roles or writer grants.
DO $wp109_0062_before$
DECLARE v62_actual jsonb; v62_reviewed jsonb; v62_names text[] := ARRAY[
 'tblwipswrbasis','tblwipswrdetail','tblsysserialno',
 'pk_tblwipswrbasis','pk_tblwipswrdetail','pk_tblsysserialno'];
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 -- A tx-local marker set in the PREVIOUS statement survives only inside the
 -- same explicit transaction. Do not execute individual payload statements.
 IF current_setting('ymes.wp109_0062_atomic_ready',true) IS DISTINCT FROM txid_current()::text
  OR current_setting('transaction_read_only')<>'off' THEN
  RAISE EXCEPTION 'Root explicit atomic transaction marker required before any CREATE'; END IF;
 IF current_setting('ymes.wp109_0062_reviewed_catalog_sha256',true) IS DISTINCT FROM
  '2349d0f0f8444c675044339ef586e934d6dde091de39560ec263ded0d411590b' THEN
  RAISE EXCEPTION 'Reviewed catalog2349 evidence pin required'; END IF;
 v62_reviewed:=nullif(current_setting('ymes.wp109_0062_reviewed_before',true),'')::jsonb;
 IF v62_reviewed IS NULL OR jsonb_typeof(v62_reviewed)<>'object' THEN
  RAISE EXCEPTION 'Full reviewed 16-category catalog required; digest alone is insufficient'; END IF;
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
 IF v62_actual IS DISTINCT FROM v62_reviewed THEN
  RAISE EXCEPTION 'Fresh whole catalog differs BEFORE CREATE; no SWR DDL permitted'; END IF;
 IF v62_actual->'defaultACL' IS DISTINCT FROM '[]'::jsonb
  OR v62_actual->'eventTriggers' IS DISTINCT FROM '[]'::jsonb THEN
  RAISE EXCEPTION 'Reviewed empty defaultACL/eventTrigger baseline required'; END IF;
 IF NOT EXISTS(SELECT 1 FROM pg_catalog.pg_namespace n JOIN pg_catalog.pg_roles r ON r.oid=n.nspowner
    WHERE n.nspname='YMES' AND r.rolname='postgres')
  OR NOT EXISTS(SELECT 1 FROM pg_catalog.pg_roles r WHERE r.rolname='ymes_app')
  OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
  RAISE EXCEPTION 'Existing quoted YMES schema/owner/app USAGE prerequisite absent'; END IF;
 IF EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=ANY(v62_names))
  OR EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_namespace n ON n.oid=k.connamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(k.conname)=ANY(v62_names))
  OR EXISTS(SELECT 1 FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(t.typname)=ANY(ARRAY[
    'tblwipswrbasis','_tblwipswrbasis','tblwipswrdetail','_tblwipswrdetail','tblsysserialno','_tblsysserialno'])) THEN
  RAISE EXCEPTION 'SWR table/PK/index/row-array type name occupied across schema/kind/case'; END IF;
 PERFORM set_config('ymes.wp109_0062_before',v62_actual::text,true);
END $wp109_0062_before$;

CREATE TABLE "YMES".tblwipswrbasis (
    swrno varchar(20) NOT NULL,
    swrtype numeric(1,0) NOT NULL,
    status numeric(2,0) NOT NULL,
    customerno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    deviceno varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    customerlotno varchar(50),
    mono varchar(50),
    attachfile bytea,
    swrdesc varchar(400) NOT NULL,
    creator varchar(50),
    createdate timestamp without time zone,
    attachfilepath varchar(200),
    CONSTRAINT pk_tblwipswrbasis PRIMARY KEY (swrno)
);

CREATE TABLE "YMES".tblwipswrdetail (
    swrno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    CONSTRAINT pk_tblwipswrdetail PRIMARY KEY (swrno, lotno)
);

CREATE TABLE "YMES".tblsysserialno (
    functionname varchar(20) NOT NULL,
    serialno numeric(9,0) NOT NULL,
    CONSTRAINT pk_tblsysserialno PRIMARY KEY (functionname)
);

GRANT SELECT ON "YMES".tblwipswrbasis, "YMES".tblwipswrdetail TO ymes_app;

-- STOP: Root must run native19 verifier and exact catalog allowlist in THIS
-- transaction before COMMIT. Serial has no app grants until a writer is reviewed.

-- 0064 WP109 writer provider tables and minimum grants.
-- Prepared transaction fragment; never run this file with psql autocommit.
-- Before sourcing this file, Root must BEGIN, take/review a fresh full16 catalog
-- from prepare_0064_wp109_provider_catalog.sql, and set transaction-local
-- ymes.wp109_provider_reviewed_catalog_sha256 and ymes.wp109_provider_reviewed_before
-- to the reviewed SHA-256 and JSON. The before verifier compares that full catalog
-- structurally as jsonb before any CREATE or GRANT. Run the independent verifier
-- after commit. Keep the 0062/SWR native metadata and app-privilege preconditions.
SET LOCAL lock_timeout='15s';
SET LOCAL statement_timeout='60s';
DO $wp109_provider_atomic$
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999
  OR current_setting('transaction_read_only')<>'off' THEN
  RAISE EXCEPTION '0064 requires an explicit writable PG14 ymes/postgres transaction'; END IF;
 PERFORM set_config('ymes.wp109_provider_atomic_ready',txid_current()::text,true);
END;
$wp109_provider_atomic$;
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP109Provider',0));
-- 0064 provider migration step; keep this block inside the same reviewed transaction.
-- Transaction fragment; Root fresh catalog review, backup and independent peer required.
-- Original frozen native0062 reader/Serial-no-app-grant verification copied unchanged below.
-- 0064 verifier; run in the Root DDL transaction and then
-- rerun independently in BEGIN READ ONLY after the migration commits.
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

DO $wp109_provider_before$
DECLARE p109_actual jsonb; p109_reviewed jsonb;
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 IF current_setting('ymes.wp109_provider_atomic_ready',true) IS DISTINCT FROM txid_current()::text
  OR current_setting('transaction_read_only')<>'off' THEN
  RAISE EXCEPTION 'Previous-statement Root atomic transaction marker required'; END IF;
 IF coalesce(current_setting('ymes.wp109_provider_reviewed_catalog_sha256',true),'') !~ '^[a-f0-9]{64}$' THEN
  RAISE EXCEPTION 'Fresh externally hash-verified reviewed input required, not a historical catalog pin'; END IF;
 p109_reviewed:=nullif(current_setting('ymes.wp109_provider_reviewed_before',true),'')::jsonb;
 IF p109_reviewed IS NULL OR jsonb_typeof(p109_reviewed)<>'object' THEN
  RAISE EXCEPTION 'Fresh reviewed full16 JSON required'; END IF;
 IF encode(pg_catalog.sha256(convert_to(p109_reviewed::text,'UTF8')),'hex')
    IS DISTINCT FROM current_setting('ymes.wp109_provider_reviewed_catalog_sha256',true) THEN
  RAISE EXCEPTION 'Reviewed full16 SHA-256 does not match supplied JSON'; END IF;
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
) INTO p109_actual;
 IF ARRAY(SELECT x.key FROM jsonb_object_keys(p109_actual)AS x(key)ORDER BY x.key) IS DISTINCT FROM ARRAY['collations','columns','constraints','database','defaultACL','defaults','eventTriggers','indexes','memberships','policies','relations','roleSettings','roles','schemas','triggers','types']
  OR p109_actual IS DISTINCT FROM p109_reviewed THEN
  RAISE EXCEPTION 'Fresh full16 differs before any CREATE/GRANT'; END IF;
 IF p109_actual->'defaultACL' IS DISTINCT FROM '[]'::jsonb OR p109_actual->'eventTriggers' IS DISTINCT FROM '[]'::jsonb THEN
  RAISE EXCEPTION 'Unexpected default ACL/event trigger: stop and obtain new review'; END IF;
 IF EXISTS(SELECT 1 FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema')AND lower(c.relname)=ANY(ARRAY['tblsyseventlog','tblsysparameter','i_tblsyseventlog_01','pk_tblsysparameter']))
  OR EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_namespace n ON n.oid=k.connamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema')AND lower(k.conname)=ANY(ARRAY['i_tblsyseventlog_01','pk_tblsysparameter','tblsyseventlog','tblsysparameter']))
  OR EXISTS(SELECT 1 FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema')AND lower(t.typname)=ANY(ARRAY['tblsyseventlog','_tblsyseventlog','tblsysparameter','_tblsysparameter'])) THEN
  RAISE EXCEPTION 'Provider table/index/constraint/type occupied across schema/kind/case'; END IF;
 PERFORM set_config('ymes.wp109_provider_before',p109_actual::text,true);
 PERFORM set_config('ymes.wp109_provider_before_ready',txid_current()::text,true);
END $wp109_provider_before$;

-- 0064 provider migration step; keep this block inside the same reviewed transaction.
-- Transaction fragment; Root fresh catalog review, backup and independent peer required.
DO $wp109_provider_create_gate$
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 IF current_setting('ymes.wp109_provider_atomic_ready',true) IS DISTINCT FROM txid_current()::text
  OR current_setting('transaction_read_only')<>'off' THEN
  RAISE EXCEPTION 'Previous-statement Root atomic transaction marker required'; END IF;
 IF current_setting('ymes.wp109_provider_before_ready',true)IS DISTINCT FROM txid_current()::text
  OR nullif(current_setting('ymes.wp109_provider_before',true),'')IS NULL THEN
  RAISE EXCEPTION 'Same-transaction inspected-before-grant full catalog/native19 required'; END IF;
END $wp109_provider_create_gate$;

CREATE TABLE "YMES".tblsyseventlog (
    userid varchar(10) NOT NULL,
    recorddate timestamp(0) without time zone NOT NULL,
    servicename varchar(30) NOT NULL,
    description text,
    keyfield varchar(1000),
    keyvalue varchar(1000),
    computername varchar(50)
);
CREATE INDEX i_tblsyseventlog_01 ON "YMES".tblsyseventlog (recorddate);

CREATE TABLE "YMES".tblsysparameter (
    parametertype varchar(50) NOT NULL,
    parameterno varchar(50) NOT NULL,
    parametervalue varchar(255) NOT NULL,
    parametervisable numeric(1,0) NOT NULL DEFAULT 0,
    parametercategory varchar(50),
    categoryorder numeric(3,0),
    parameterorder numeric(3,0) DEFAULT 0,
    parameterdatatype numeric(1,0),
    parameterinputtype numeric(1,0),
    encryptionflag numeric(1,0) DEFAULT 0,
    validlist varchar(2000),
    activemodule varchar(50),
    ic_ingot numeric(1,0) DEFAULT 0,
    epi_wafer numeric(1,0) DEFAULT 0,
    semi_wafer numeric(1,0) DEFAULT 0,
    ic_cp_pkg numeric(1,0) DEFAULT 0,
    ic_ft_pkg numeric(1,0) DEFAULT 0,
    discrete_pkg numeric(1,0) DEFAULT 0,
    led_pkg numeric(1,0) DEFAULT 0,
    ic_smt numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsysparameter PRIMARY KEY (parameterno)
);

-- Existing Basis/Detail SELECT retained. No REVOKE, column grants, role/default-ACL
-- changes, parameter seeds, Serial seeds, Lot tables, or business DML.
GRANT INSERT,UPDATE,DELETE ON "YMES".tblwipswrbasis TO ymes_app;
GRANT DELETE ON "YMES".tblwipswrdetail TO ymes_app;
GRANT SELECT,INSERT,UPDATE ON "YMES".tblsysserialno TO ymes_app;
GRANT SELECT ON "YMES".tblsysparameter TO ymes_app;
GRANT INSERT ON "YMES".tblsyseventlog TO ymes_app;
-- STOP: writer native19 + provider native32/ACL + full16 allowdelta must pass
-- in THIS transaction before Root may issue COMMIT.

-- 0064 provider migration step; keep this block inside the same reviewed transaction.
-- Transaction fragment; Root fresh catalog review, backup and independent peer required.
-- 0064 verifier; run in the Root DDL transaction and then
-- rerun independently in BEGIN READ ONLY after the migration commits.
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
  ('tblwipswrbasis',15,'pk_tblwipswrbasis',ARRAY[1]::smallint[],1,ARRAY['DELETE','INSERT','SELECT','UPDATE']::text[]),
  ('tblwipswrdetail',2,'pk_tblwipswrdetail',ARRAY[1,2]::smallint[],2,ARRAY['DELETE','SELECT']::text[]),
  ('tblsysserialno',2,'pk_tblsysserialno',ARRAY[1]::smallint[],1,ARRAY['INSERT','SELECT','UPDATE']::text[])
 ) AS s(tbl,cols,pkname,keynums,keycount,app_privileges) LOOP
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
    UNION ALL SELECT v62_app,unnest(v62_spec.app_privileges)
   ) AS q;
  IF v62_actual_acl IS DISTINCT FROM v62_expected_acl
   OR EXISTS(SELECT 1 FROM unnest(ARRAY['DELETE','INSERT','REFERENCES','SELECT','TRIGGER','TRUNCATE','UPDATE']) AS p(privilege)
    WHERE has_table_privilege(v62_app,v62_rel,p.privilege)IS DISTINCT FROM(p.privilege=ANY(v62_spec.app_privileges))) THEN
   RAISE EXCEPTION 'Exact writer-phase app privileges differ: %',v62_spec.tbl; END IF;
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

-- New strict writer phase; all native shape checks retained. Metadata/zero-row reads only.
SET LOCAL ROLE ymes_app;
SELECT swrno,swrtype,status,customerno,opno,deviceno,starttime,endtime,customerlotno,mono,
 attachfile,swrdesc,creator,createdate,attachfilepath FROM "YMES".tblwipswrbasis WHERE false;
SELECT swrno,lotno FROM "YMES".tblwipswrdetail WHERE false;
SELECT functionname,serialno FROM "YMES".tblsysserialno WHERE false;
RESET ROLE;
SELECT jsonb_build_object('module','WP109_WRITER_EXISTING_NATIVE19','status','passed',
 'nativeTables',3,'nativeColumns',19,'pkConstraints',3,'pkIndexes',3,'pkIndexAttributes',4,
 'rowArrayTypes',6,'defaults',0,'appSelectTables',3,'basisPrivileges','S/I/U/D','detailPrivileges','S/D','serialPrivileges','S/I/U',
 'businessRowsRead',false,'countsAreMetadataOnly',true) AS native19_verification;

SELECT set_config('ymes.wp109_provider_existing_native_ready',txid_current()::text,true);

-- 0064 provider migration step; keep this block inside the same reviewed transaction.
-- Transaction fragment; Root fresh catalog review, backup and independent peer required.
-- SOURCE PROPOSAL / NOT EXECUTED. Structure only, no ACL/whole-catalog claim.
-- Root may wrap this in READ ONLY/ROLLBACK or the reviewed CREATE transaction.
DO $wp109_provider_native$
DECLARE
 g109_name text;
 g109_expected jsonb;
 g109_actual jsonb;
 g109_matches bigint;
 g109_relation oid;
 g109_rowtype oid;
 g109_index_columns text[];
BEGIN
 IF current_database()<>'ymes' OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PG14 ymes context required'; END IF;
 FOR g109_name,g109_expected IN SELECT p.name,p.shape FROM (VALUES
 ('tblsyseventlog',$g109_event$[
  [1,"userid","character varying(10)",true,false],
  [2,"recorddate","timestamp(0) without time zone",true,false],
  [3,"servicename","character varying(30)",true,false],
  [4,"description","text",false,false],
  [5,"keyfield","character varying(1000)",false,false],
  [6,"keyvalue","character varying(1000)",false,false],
  [7,"computername","character varying(50)",false,false]
 ]$g109_event$::jsonb),
 ('tblsysparameter',$g109_parameter$[
  [1,"parametertype","character varying(50)",true,false],
  [2,"parameterno","character varying(50)",true,false],
  [3,"parametervalue","character varying(255)",true,false],
  [4,"parametervisable","numeric(1,0)",true,true],
  [5,"parametercategory","character varying(50)",false,false],
  [6,"categoryorder","numeric(3,0)",false,false],
  [7,"parameterorder","numeric(3,0)",false,true],
  [8,"parameterdatatype","numeric(1,0)",false,false],
  [9,"parameterinputtype","numeric(1,0)",false,false],
  [10,"encryptionflag","numeric(1,0)",false,true],
  [11,"validlist","character varying(2000)",false,false],
  [12,"activemodule","character varying(50)",false,false],
  [13,"ic_ingot","numeric(1,0)",false,true],
  [14,"epi_wafer","numeric(1,0)",false,true],
  [15,"semi_wafer","numeric(1,0)",false,true],
  [16,"ic_cp_pkg","numeric(1,0)",false,true],
  [17,"ic_ft_pkg","numeric(1,0)",false,true],
  [18,"discrete_pkg","numeric(1,0)",false,true],
  [19,"led_pkg","numeric(1,0)",false,true],
  [20,"ic_smt","numeric(1,0)",false,true],
  [21,"creator","character varying(10)",false,false],
  [22,"createdate","timestamp without time zone",false,false],
  [23,"reviser","character varying(10)",false,false],
  [24,"revisedate","timestamp without time zone",false,false],
  [25,"datastamp","numeric(11,0)",false,true]
 ]$g109_parameter$::jsonb)) p(name,shape)
 LOOP
  SELECT count(*) INTO g109_matches FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(c.relname)=g109_name;
  IF g109_matches<>1 THEN RAISE EXCEPTION 'Provider relation absent/shadowed: %',g109_name; END IF;
  SELECT c.oid,c.reltype INTO g109_relation,g109_rowtype FROM pg_catalog.pg_class c
   JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname='YMES' AND c.relname=g109_name AND c.relkind='r' AND c.relpersistence='p'
    AND pg_get_userbyid(c.relowner)='postgres' AND NOT c.relrowsecurity AND NOT c.relforcerowsecurity
    AND NOT c.relispartition AND NOT c.relhasrules;
  IF NOT FOUND THEN RAISE EXCEPTION 'Provider owner/kind/security differs: %',g109_name; END IF;
  IF EXISTS(SELECT 1 FROM pg_catalog.pg_inherits i WHERE i.inhrelid=g109_relation OR i.inhparent=g109_relation)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_trigger t WHERE t.tgrelid=g109_relation)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.contype='f' AND (k.conrelid=g109_relation OR k.confrelid=g109_relation)) THEN
   RAISE EXCEPTION 'Provider hidden effects/inheritance unsupported: %',g109_name; END IF;
  SELECT count(*) INTO g109_matches FROM pg_catalog.pg_type t JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace
   WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND lower(t.typname)=ANY(ARRAY[g109_name,'_'||g109_name]);
  IF g109_matches<>2 OR NOT EXISTS(
   SELECT 1 FROM pg_catalog.pg_type r JOIN pg_catalog.pg_type a ON a.oid=r.typarray
    JOIN pg_catalog.pg_namespace n ON n.oid=r.typnamespace WHERE r.oid=g109_rowtype
    AND n.nspname='YMES' AND r.typname=g109_name AND r.typtype='c' AND r.typrelid=g109_relation
    AND r.typelem=0 AND r.typisdefined AND a.typnamespace=n.oid AND a.typname='_'||g109_name
    AND a.typtype='b' AND a.typrelid=0 AND a.typelem=r.oid AND a.typisdefined
    AND pg_get_userbyid(r.typowner)='postgres' AND pg_get_userbyid(a.typowner)='postgres') THEN
   RAISE EXCEPTION 'Provider row/array types differ: %',g109_name; END IF;
  SELECT coalesce(jsonb_agg(jsonb_build_array(a.attnum::int,a.attname,format_type(a.atttypid,a.atttypmod),
   a.attnotnull,a.atthasdef) ORDER BY a.attnum),'[]'::jsonb) INTO g109_actual
   FROM pg_catalog.pg_attribute a WHERE a.attrelid=g109_relation AND a.attnum>0;
  IF g109_actual IS DISTINCT FROM g109_expected OR EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a
   WHERE a.attrelid=g109_relation AND a.attnum>0 AND (a.attisdropped OR a.attidentity<>'' OR a.attgenerated<>'')) THEN
   RAISE EXCEPTION 'Provider physical columns/NULL/default positions differ: %',g109_name; END IF;
  IF EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
   WHERE a.attrelid=g109_relation AND a.attnum>0 AND a.atthasdef
    AND (d.oid IS NULL OR pg_get_expr(d.adbin,d.adrelid) NOT IN ('0','0::numeric','''0''::numeric','(0)::numeric'))) THEN
   RAISE EXCEPTION 'Provider default must be literal0: %',g109_name; END IF;
  IF EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_collation l ON l.oid=a.attcollation
   LEFT JOIN pg_catalog.pg_namespace n ON n.oid=l.collnamespace WHERE a.attrelid=g109_relation AND a.attnum>0 AND a.attcollation<>0
    AND (n.nspname IS DISTINCT FROM 'pg_catalog' OR l.collname IS DISTINCT FROM 'default' OR l.collisdeterministic IS DISTINCT FROM true)) THEN
   RAISE EXCEPTION 'Provider text collation differs: %',g109_name; END IF;
  SELECT count(*) INTO g109_matches FROM pg_catalog.pg_index i WHERE i.indrelid=g109_relation;
  IF g109_matches<>1 THEN RAISE EXCEPTION 'Provider native index count differs: %',g109_name; END IF;
  SELECT ARRAY(SELECT a.attname::text FROM unnest(i.indkey) WITH ORDINALITY k(attnum,ord)
   JOIN pg_catalog.pg_attribute a ON a.attrelid=i.indrelid AND a.attnum=k.attnum ORDER BY k.ord)
   INTO g109_index_columns FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class ix ON ix.oid=i.indexrelid
   JOIN pg_catalog.pg_am m ON m.oid=ix.relam WHERE i.indrelid=g109_relation
    AND i.indisvalid AND i.indisready AND i.indislive AND i.indnkeyatts=1 AND i.indnatts=1
    AND i.indpred IS NULL AND i.indexprs IS NULL AND m.amname='btree' AND i.indoption::text='0';
  IF NOT FOUND THEN RAISE EXCEPTION 'Provider native index shape differs: %',g109_name; END IF;
  SELECT count(*) INTO g109_matches FROM pg_catalog.pg_constraint k WHERE k.conrelid=g109_relation;
  IF g109_name='tblsysparameter' THEN
   IF g109_matches<>1 OR g109_index_columns IS DISTINCT FROM ARRAY['parameterno']::text[] OR NOT EXISTS(
    SELECT 1 FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_index i ON i.indexrelid=k.conindid
    JOIN pg_catalog.pg_attribute a ON a.attrelid=k.conrelid AND a.attname='parameterno' AND a.attnum=ANY(k.conkey)
    WHERE k.conrelid=g109_relation AND k.contype='p' AND k.conname='pk_tblsysparameter' AND cardinality(k.conkey)=1
     AND k.convalidated AND NOT k.condeferrable AND NOT k.condeferred AND i.indisprimary AND i.indisunique) THEN
    RAISE EXCEPTION 'Parameter native PK differs'; END IF;
  ELSE
   IF g109_matches<>0 OR g109_index_columns IS DISTINCT FROM ARRAY['recorddate']::text[] OR NOT EXISTS(
    SELECT 1 FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class x ON x.oid=i.indexrelid
    WHERE i.indrelid=g109_relation AND x.relname='i_tblsyseventlog_01' AND NOT i.indisprimary AND NOT i.indisunique) THEN
    RAISE EXCEPTION 'EventLog nonunique index/noPK differs'; END IF;
  END IF;
 END LOOP;
END;
$wp109_provider_native$;
SELECT jsonb_build_object('module','WP109_WRITER_PROVIDER_NATIVE32','status','passed',
 'nativeBusinessColumns',32,'nativeDefaults',12,'primaryKeys',1,'indexes',2,'derivedTypes',4,
 'permissionsVerified',false,'wholeCatalogPreservationVerified',false,'businessRowsRead',false);

DO $wp109_provider_strict_acl$
DECLARE p109_name text; p109_privileges text[]; p109_rel oid; p109_owner oid; p109_app oid;
 p109_index oid; p109_key smallint; p109_indexname text; p109_collation oid;
 p109_actual jsonb; p109_expected jsonb; p109_col record; p109_indexcol record;
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 IF current_setting('ymes.wp109_provider_existing_native_ready',true)IS DISTINCT FROM txid_current()::text THEN
  RAISE EXCEPTION 'Strict native19 writer-phase verification required in same transaction'; END IF;
 SELECT oid INTO p109_owner FROM pg_catalog.pg_roles WHERE rolname='postgres';
 SELECT oid INTO p109_app FROM pg_catalog.pg_roles WHERE rolname='ymes_app';
 SELECT c.oid INTO p109_collation FROM pg_catalog.pg_collation c JOIN pg_catalog.pg_namespace n ON n.oid=c.collnamespace
  WHERE n.nspname='pg_catalog'AND c.collname='default'AND c.collisdeterministic;
 IF p109_owner IS NULL OR p109_app IS NULL OR p109_collation IS NULL THEN RAISE EXCEPTION 'Reviewed roles/collation absent';END IF;
 FOR p109_name,p109_privileges,p109_indexname,p109_key IN SELECT * FROM(VALUES
  ('tblsyseventlog',ARRAY['INSERT']::text[],'i_tblsyseventlog_01',2::smallint),
  ('tblsysparameter',ARRAY['SELECT']::text[],'pk_tblsysparameter',2::smallint)
 )AS p(name,privileges,indexname,keynum) LOOP
  SELECT c.oid INTO p109_rel FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname='YMES'AND c.relname=p109_name AND c.relkind='r'AND c.relpersistence='p'AND c.relowner=p109_owner
    AND c.reloptions IS NULL AND c.reltablespace=0 AND NOT c.relrowsecurity AND NOT c.relforcerowsecurity
    AND NOT c.relispartition AND NOT c.relhasrules;
  IF p109_rel IS NULL OR EXISTS(SELECT 1 FROM pg_catalog.pg_policy p WHERE p.polrelid=p109_rel)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_attribute a WHERE a.attrelid=p109_rel AND a.attnum>0 AND a.attacl IS NOT NULL)
   OR EXISTS(SELECT 1 FROM pg_catalog.pg_type t WHERE(t.typrelid=p109_rel OR t.typelem=(SELECT reltype FROM pg_catalog.pg_class WHERE oid=p109_rel))AND t.typacl IS NOT NULL)THEN
   RAISE EXCEPTION 'Provider options/columnACL/typeACL/policy differs: %',p109_name;END IF;
  SELECT coalesce(jsonb_agg(jsonb_build_array(x.grantor,x.grantee,x.privilege_type,x.is_grantable)
   ORDER BY x.grantor,x.grantee,x.privilege_type,x.is_grantable),'[]'::jsonb)INTO p109_actual
   FROM pg_catalog.pg_class c CROSS JOIN LATERAL aclexplode(CASE WHEN c.relacl IS NULL THEN acldefault('r',c.relowner)
    WHEN cardinality(c.relacl)=0 THEN NULL::aclitem[]ELSE c.relacl END)AS x WHERE c.oid=p109_rel;
  SELECT jsonb_agg(jsonb_build_array(p109_owner,q.grantee,q.privilege,false)ORDER BY p109_owner,q.grantee,q.privilege)
   INTO p109_expected FROM(SELECT p109_owner AS grantee,unnest(ARRAY['DELETE','INSERT','REFERENCES','SELECT','TRIGGER','TRUNCATE','UPDATE'])AS privilege
    UNION ALL SELECT p109_app,unnest(p109_privileges))AS q;
  IF p109_actual IS DISTINCT FROM p109_expected OR EXISTS(
   SELECT 1 FROM unnest(ARRAY['DELETE','INSERT','REFERENCES','SELECT','TRIGGER','TRUNCATE','UPDATE'])AS x(privilege)
   WHERE has_table_privilege(p109_app,p109_rel,x.privilege)IS DISTINCT FROM(x.privilege=ANY(p109_privileges)))THEN
   RAISE EXCEPTION 'Exact INSERT-only audit/SELECT-only parameter ACL differs: %',p109_name;END IF;
  IF(SELECT count(*)FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
   WHERE n.nspname NOT IN('pg_catalog','information_schema')AND lower(c.relname)=p109_indexname)<>1 THEN
   RAISE EXCEPTION 'Provider index name shadowed: %',p109_indexname;END IF;
  SELECT i.indexrelid INTO p109_index FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class c ON c.oid=i.indexrelid
   JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace JOIN pg_catalog.pg_am m ON m.oid=c.relam
   WHERE i.indrelid=p109_rel AND c.relname=p109_indexname AND n.nspname='YMES'AND c.relkind='i'AND c.relpersistence='p'
    AND c.relowner=p109_owner AND c.relacl IS NULL AND c.reloptions IS NULL AND c.reltablespace=0 AND m.amname='btree'
    AND i.indisvalid AND i.indisready AND i.indislive AND NOT i.indisexclusion AND i.indnkeyatts=1 AND i.indnatts=1
    AND i.indkey::text=p109_key::text AND i.indoption::text='0'AND i.indexprs IS NULL AND i.indpred IS NULL
    AND i.indisprimary=(p109_name='tblsysparameter')AND i.indisunique=(p109_name='tblsysparameter')
    AND i.indcollation::text=(CASE WHEN p109_name='tblsysparameter'THEN p109_collation::text ELSE '0'END)
    AND i.indclass::text=(SELECT oc.oid::text FROM pg_catalog.pg_opclass oc JOIN pg_catalog.pg_namespace ns ON ns.oid=oc.opcnamespace
     WHERE ns.nspname='pg_catalog'AND oc.opcname=CASE WHEN p109_name='tblsysparameter'THEN 'text_ops'ELSE 'timestamp_ops'END AND oc.opcmethod=c.relam);
  IF p109_index IS NULL OR(SELECT count(*)FROM pg_catalog.pg_attribute WHERE attrelid=p109_index AND attnum>0)<>1 THEN
   RAISE EXCEPTION 'Provider exact index properties differ: %',p109_indexname;END IF;
  SELECT * INTO p109_col FROM pg_catalog.pg_attribute WHERE attrelid=p109_rel AND attnum=p109_key;
  SELECT * INTO p109_indexcol FROM pg_catalog.pg_attribute WHERE attrelid=p109_index AND attnum=1;
  IF p109_indexcol.attname IS DISTINCT FROM p109_col.attname OR p109_indexcol.atttypid IS DISTINCT FROM p109_col.atttypid
   OR p109_indexcol.atttypmod IS DISTINCT FROM p109_col.atttypmod OR p109_indexcol.attcollation IS DISTINCT FROM p109_col.attcollation
   OR p109_indexcol.attisdropped OR p109_indexcol.attnotnull OR p109_indexcol.atthasdef OR p109_indexcol.attacl IS NOT NULL
   OR p109_indexcol.attidentity<>''OR p109_indexcol.attgenerated<>''THEN
   RAISE EXCEPTION 'Provider exact index attribute differs: %',p109_indexname;END IF;
  IF p109_name='tblsysparameter'AND NOT EXISTS(SELECT 1 FROM pg_catalog.pg_constraint k WHERE k.conrelid=p109_rel
   AND k.contype='p'AND k.conname='pk_tblsysparameter'AND k.conkey=ARRAY[2]::smallint[]AND k.conindid=p109_index
   AND k.convalidated AND NOT k.condeferrable AND NOT k.condeferred)THEN RAISE EXCEPTION 'Exact parameter PK target differs';END IF;
  IF (SELECT count(*) FROM pg_catalog.pg_attrdef WHERE adrelid=p109_rel) <> (CASE WHEN p109_name='tblsysparameter' THEN 12 ELSE 0 END) THEN
   RAISE EXCEPTION 'Provider exact default count differs';END IF;
 END LOOP;
 PERFORM set_config('ymes.wp109_provider_native_ready',txid_current()::text,true);
END $wp109_provider_strict_acl$;
-- Metadata-only verifier never SELECTs EventLog or attempts an audit INSERT.
SET LOCAL ROLE ymes_app;
SELECT parameterno,parametervalue,parametertype FROM "YMES".tblsysparameter WHERE false;
RESET ROLE;
SELECT jsonb_build_object('module','WP109_WRITER_PROVIDER_NATIVE32_STRICT_ACL','status','passed',
 'nativeBusinessColumns',32,'nativeIndexAttributes',2,'nativeDefaults',12,'primaryKeys',1,'indexes',2,'derivedTypes',4,
 'eventLogAppPrivileges','INSERT only','parameterAppPrivileges','SELECT only','businessRowsRead',false,
 'wholeCatalogPreservationVerified',false,'preparedEventInsertRuntimeLockTested',false);

-- 0064 provider migration step; keep this block inside the same reviewed transaction.
-- Transaction fragment; Root fresh catalog review, backup and independent peer required.
DO $wp109_provider_allowdelta$
DECLARE p109_before jsonb;p109_actual jsonb;p109_normalized jsonb;p109_section text;p109_added text[];p109_expected text[];p109_key text;
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::int NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'Reviewed PostgreSQL14/postgres/ymes context required'; END IF;
 IF current_setting('ymes.wp109_provider_atomic_ready',true) IS DISTINCT FROM txid_current()::text
  OR current_setting('transaction_read_only')<>'off' THEN
  RAISE EXCEPTION 'Previous-statement Root atomic transaction marker required'; END IF;
 IF current_setting('ymes.wp109_provider_before_ready',true)IS DISTINCT FROM txid_current()::text
  OR current_setting('ymes.wp109_provider_native_ready',true)IS DISTINCT FROM txid_current()::text THEN
  RAISE EXCEPTION 'Same-transaction before/native19/native32/strict ACL gates required';END IF;
 p109_before:=nullif(current_setting('ymes.wp109_provider_before',true),'')::jsonb;
 IF p109_before IS NULL THEN RAISE EXCEPTION 'Before full16 JSON absent';END IF;
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
) INTO p109_actual;
 IF ARRAY(SELECT x.key FROM jsonb_object_keys(p109_before)AS x(key)ORDER BY x.key)IS DISTINCT FROM ARRAY['collations','columns','constraints','database','defaultACL','defaults','eventTriggers','indexes','memberships','policies','relations','roleSettings','roles','schemas','triggers','types']
  OR ARRAY(SELECT x.key FROM jsonb_object_keys(p109_actual)AS x(key)ORDER BY x.key)IS DISTINCT FROM ARRAY['collations','columns','constraints','database','defaultACL','defaults','eventTriggers','indexes','memberships','policies','relations','roleSettings','roles','schemas','triggers','types']THEN
  RAISE EXCEPTION 'Exact sixteen catalog categories required';END IF;
 p109_normalized:=p109_actual;
 FOREACH p109_section IN ARRAY ARRAY['relations','columns','types','defaults','constraints','indexes']LOOP
  SELECT coalesce(array_agg(x.key ORDER BY x.key),ARRAY[]::text[])INTO p109_added
   FROM jsonb_object_keys(p109_actual->p109_section)AS x(key)WHERE NOT(p109_before->p109_section ? x.key);
  IF p109_section='relations'THEN p109_expected:=ARRAY['YMES.i_tblsyseventlog_01','YMES.pk_tblsysparameter','YMES.tblsyseventlog','YMES.tblsysparameter']::text[];
  ELSIF p109_section='columns'THEN p109_expected:=ARRAY['YMES.i_tblsyseventlog_01.recorddate','YMES.pk_tblsysparameter.parameterno','YMES.tblsyseventlog.computername','YMES.tblsyseventlog.description','YMES.tblsyseventlog.keyfield','YMES.tblsyseventlog.keyvalue','YMES.tblsyseventlog.recorddate','YMES.tblsyseventlog.servicename','YMES.tblsyseventlog.userid','YMES.tblsysparameter.activemodule','YMES.tblsysparameter.categoryorder','YMES.tblsysparameter.createdate','YMES.tblsysparameter.creator','YMES.tblsysparameter.datastamp','YMES.tblsysparameter.discrete_pkg','YMES.tblsysparameter.encryptionflag','YMES.tblsysparameter.epi_wafer','YMES.tblsysparameter.ic_cp_pkg','YMES.tblsysparameter.ic_ft_pkg','YMES.tblsysparameter.ic_ingot','YMES.tblsysparameter.ic_smt','YMES.tblsysparameter.led_pkg','YMES.tblsysparameter.parametercategory','YMES.tblsysparameter.parameterdatatype','YMES.tblsysparameter.parameterinputtype','YMES.tblsysparameter.parameterno','YMES.tblsysparameter.parameterorder','YMES.tblsysparameter.parametertype','YMES.tblsysparameter.parametervalue','YMES.tblsysparameter.parametervisable','YMES.tblsysparameter.revisedate','YMES.tblsysparameter.reviser','YMES.tblsysparameter.semi_wafer','YMES.tblsysparameter.validlist']::text[];
  ELSIF p109_section='types'THEN p109_expected:=ARRAY['YMES._tblsyseventlog','YMES._tblsysparameter','YMES.tblsyseventlog','YMES.tblsysparameter']::text[];
  ELSIF p109_section='defaults'THEN p109_expected:=ARRAY['YMES.tblsysparameter.datastamp','YMES.tblsysparameter.discrete_pkg','YMES.tblsysparameter.encryptionflag','YMES.tblsysparameter.epi_wafer','YMES.tblsysparameter.ic_cp_pkg','YMES.tblsysparameter.ic_ft_pkg','YMES.tblsysparameter.ic_ingot','YMES.tblsysparameter.ic_smt','YMES.tblsysparameter.led_pkg','YMES.tblsysparameter.parameterorder','YMES.tblsysparameter.parametervisable','YMES.tblsysparameter.semi_wafer']::text[];
  ELSIF p109_section='constraints'THEN p109_expected:=ARRAY['YMES.tblsysparameter.pk_tblsysparameter']::text[];
  ELSIF p109_section='indexes'THEN p109_expected:=ARRAY['YMES.i_tblsyseventlog_01','YMES.pk_tblsysparameter']::text[];
  ELSE RAISE EXCEPTION 'Unreviewed allowdelta category';END IF;
  IF p109_added IS DISTINCT FROM p109_expected THEN RAISE EXCEPTION 'Only exact reviewed provider additions allowed: %',p109_section;END IF;
  p109_normalized:=jsonb_set(p109_normalized,ARRAY[p109_section],(p109_actual->p109_section)-p109_expected);
 END LOOP;
 -- Only relacl may change on these THREE existing relations. The strict
 -- native19 verifier already requires exact per-relation writer ACL sets.
 FOREACH p109_key IN ARRAY ARRAY['YMES.tblwipswrbasis','YMES.tblwipswrdetail','YMES.tblsysserialno']LOOP
  IF NOT(p109_before->'relations' ? p109_key)OR NOT(p109_actual->'relations' ? p109_key)
   OR((p109_actual->'relations'->p109_key)-'relacl')IS DISTINCT FROM((p109_before->'relations'->p109_key)-'relacl')THEN
   RAISE EXCEPTION 'Existing relation changed beyond explicitly reviewed ACL: %',p109_key;END IF;
  p109_normalized:=jsonb_set(p109_normalized,ARRAY['relations',p109_key],p109_before->'relations'->p109_key);
 END LOOP;
 IF p109_normalized IS DISTINCT FROM p109_before THEN
  RAISE EXCEPTION 'Unreviewed catalog/role/schema/defaultACL/parent change: rollback entire proposal';END IF;
 PERFORM set_config('ymes.wp109_provider_after',p109_actual::text,true);
END $wp109_provider_allowdelta$;
SELECT jsonb_build_object('module','WP109_WRITER_PROVIDER_FULL16_ALLOWDELTA','status','passed',
 'newTables',2,'newUserRelations',4,'newBusinessColumns',32,'newIndexAttributes',2,'newColumnKeys',34,
 'newRowArrayTypes',4,'newDefaults',12,'newPkConstraints',1,'newIndexes',2,'existingRelAclChanges',3,
 'preservedCatalogCategories',16,'businessDmlByProposal',false);
SELECT jsonb_build_object('module','WP109_PROVIDER_0064_PRECOMMIT','status','all_native_acl_and_catalog_allowlist_checks_passed','commitPending',true,'businessRowsChanged',false) AS wp109_provider_precommit_gate;

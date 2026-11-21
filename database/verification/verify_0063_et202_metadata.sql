-- PREPARED ONLY / NOT EXECUTED. Same-transaction native verification.
-- Root supplies BEGIN/COMMIT; independent readback wraps BEGIN READ ONLY/ROLLBACK.
-- Native DO block raw text is exact source910d25; original standalone is unchanged.
DO $et202_verify$
DECLARE
    g202_relation oid;
    g202_rowtype oid;
    g202_arraytype oid;
    g202_column_count integer := 0;
    g202_attribute record;
BEGIN
    IF (SELECT count(*) FROM pg_catalog.pg_namespace sn WHERE lower(sn.nspname)='ymes')<>1
       OR NOT EXISTS(SELECT 1 FROM pg_catalog.pg_namespace sn WHERE sn.nspname='YMES') THEN
        RAISE EXCEPTION 'ET202 exact YMES namespace required';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_class rc JOIN pg_catalog.pg_namespace rn ON rn.oid=rc.relnamespace
        WHERE rn.nspname NOT IN ('pg_catalog','information_schema') AND lower(rc.relname)='tblentositem')<>1 THEN
        RAISE EXCEPTION 'ET202 all-schema relation collision or missing target';
    END IF;
    SELECT rc.oid,rc.reltype INTO STRICT g202_relation,g202_rowtype
    FROM pg_catalog.pg_class rc JOIN pg_catalog.pg_namespace rn ON rn.oid=rc.relnamespace
    WHERE rn.nspname='YMES' AND rc.relname='tblentositem' AND rc.relkind='r' AND rc.relpersistence='p'
      AND pg_get_userbyid(rc.relowner)='postgres' AND NOT rc.relrowsecurity AND NOT rc.relforcerowsecurity
      AND NOT rc.relispartition AND NOT rc.relhasrules;
    IF EXISTS(SELECT 1 FROM pg_catalog.pg_inherits ih WHERE ih.inhrelid=g202_relation OR ih.inhparent=g202_relation)
       OR EXISTS(SELECT 1 FROM pg_catalog.pg_trigger tg WHERE tg.tgrelid=g202_relation)
       OR EXISTS(SELECT 1 FROM pg_catalog.pg_policy pl WHERE pl.polrelid=g202_relation)
       OR EXISTS(SELECT 1 FROM pg_catalog.pg_constraint fk WHERE fk.contype='f' AND (fk.conrelid=g202_relation OR fk.confrelid=g202_relation)) THEN
        RAISE EXCEPTION 'ET202 unexpected inheritance/trigger/policy/incoming or outgoing FK';
    END IF;
    FOR g202_attribute IN
        SELECT ca.attnum::integer AS ordinal,ca.attname,format_type(ca.atttypid,ca.atttypmod) AS type,
               ca.attnotnull,ca.attidentity::text,ca.attgenerated::text,ca.attisdropped,ca.attinhcount,
               ca.atthasdef,pg_get_expr(df.adbin,df.adrelid) AS expression,
               cn.nspname AS collation_schema,co.collname,co.collisdeterministic,
               ex.name AS expected_name,ex.type AS expected_type,ex.required AS expected_notnull,
               ex.defaulted AS expected_default
        FROM pg_catalog.pg_attribute ca
        LEFT JOIN (VALUES
            (1,'ositemno','character varying(2)',true,false),
            (2,'ositemname','character varying(50)',true,false),
            (3,'plandays','numeric(3,0)',true,false),
            (4,'processno','character varying(30)',true,false),
            (5,'returntype','numeric(1,0)',true,false),
            (6,'description','character varying(255)',false,false),
            (7,'creator','character varying(10)',false,false),
            (8,'createdate','timestamp without time zone',false,false),
            (9,'datastamp','numeric(11,0)',false,true),
            (10,'reviser','character varying(10)',false,false),
            (11,'revisedate','timestamp without time zone',false,false)
        ) AS ex(ordinal,name,type,required,defaulted) ON ex.ordinal=ca.attnum
        LEFT JOIN pg_catalog.pg_attrdef df ON df.adrelid=ca.attrelid AND df.adnum=ca.attnum
        LEFT JOIN pg_catalog.pg_collation co ON co.oid=ca.attcollation
        LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
        WHERE ca.attrelid=g202_relation AND ca.attnum>0 ORDER BY ca.attnum
    LOOP
        g202_column_count := g202_column_count+1;
        IF g202_attribute.expected_name IS NULL OR g202_attribute.attisdropped
           OR g202_attribute.ordinal<>g202_column_count
           OR g202_attribute.attname<>g202_attribute.expected_name
           OR g202_attribute.type<>g202_attribute.expected_type
           OR g202_attribute.attnotnull IS DISTINCT FROM g202_attribute.expected_notnull
           OR g202_attribute.attidentity<>'' OR g202_attribute.attgenerated<>''
           OR g202_attribute.attinhcount<>0
           OR g202_attribute.atthasdef IS DISTINCT FROM g202_attribute.expected_default THEN
            RAISE EXCEPTION 'ET202 native column mismatch at ordinal %',g202_attribute.ordinal;
        END IF;
        IF g202_attribute.expected_default AND g202_attribute.expression NOT IN ('0','0::numeric','''0''::numeric','(0)::numeric') THEN
            RAISE EXCEPTION 'ET202 DataStamp default must be literal zero';
        END IF;
        IF g202_attribute.type LIKE 'character varying(%' AND
           (g202_attribute.collation_schema IS DISTINCT FROM 'pg_catalog'
            OR g202_attribute.collname IS DISTINCT FROM 'default'
            OR g202_attribute.collisdeterministic IS DISTINCT FROM true) THEN
            RAISE EXCEPTION 'ET202 target varchar collation differs';
        END IF;
    END LOOP;
    IF g202_column_count<>11 THEN RAISE EXCEPTION 'ET202 native column count is not11'; END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_constraint ct WHERE ct.conrelid=g202_relation)<>1
       OR (SELECT count(*) FROM pg_catalog.pg_index ix WHERE ix.indrelid=g202_relation)<>1 THEN
        RAISE EXCEPTION 'ET202 exactly one PK and its one index required';
    END IF;
    IF NOT EXISTS(
        SELECT 1 FROM pg_catalog.pg_constraint pk
        JOIN pg_catalog.pg_index ix ON ix.indexrelid=pk.conindid
        JOIN pg_catalog.pg_class ic ON ic.oid=ix.indexrelid
        JOIN pg_catalog.pg_namespace ins ON ins.oid=ic.relnamespace
        JOIN pg_catalog.pg_am am ON am.oid=ic.relam
        WHERE pk.conrelid=g202_relation AND pk.contype='p' AND pk.conname='pk_tblentositem'
          AND pk.convalidated AND NOT pk.condeferrable AND NOT pk.condeferred
          AND pk.conkey=ARRAY[1]::smallint[]
          AND ix.indisprimary AND ix.indisunique AND ix.indisvalid AND ix.indisready AND ix.indislive
          AND ix.indnkeyatts=1 AND ix.indnatts=1 AND ix.indkey::text='1' AND ix.indoption::text='0'
          AND ix.indexprs IS NULL AND ix.indpred IS NULL
          AND ic.relname='pk_tblentositem' AND ins.nspname='YMES' AND ic.relkind='i'
          AND pg_get_userbyid(ic.relowner)='postgres' AND am.amname='btree'
    ) OR (SELECT count(*) FROM pg_catalog.pg_class pc JOIN pg_catalog.pg_namespace pn ON pn.oid=pc.relnamespace
          WHERE pn.nspname NOT IN ('pg_catalog','information_schema') AND lower(pc.relname)='pk_tblentositem')<>1 THEN
        RAISE EXCEPTION 'ET202 exact PK/index or all-schema index name mismatch';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_type ty JOIN pg_catalog.pg_namespace tn ON tn.oid=ty.typnamespace
        WHERE tn.nspname NOT IN ('pg_catalog','information_schema') AND lower(ty.typname)=ANY(ARRAY['tblentositem','_tblentositem']))<>2 THEN
        RAISE EXCEPTION 'ET202 row/array type name collision';
    END IF;
    SELECT ty.typarray INTO STRICT g202_arraytype FROM pg_catalog.pg_type ty
    JOIN pg_catalog.pg_namespace tn ON tn.oid=ty.typnamespace
    WHERE ty.oid=g202_rowtype AND tn.nspname='YMES' AND ty.typname='tblentositem'
      AND ty.typtype='c' AND ty.typrelid=g202_relation AND pg_get_userbyid(ty.typowner)='postgres';
    IF NOT EXISTS(SELECT 1 FROM pg_catalog.pg_type ta JOIN pg_catalog.pg_namespace an ON an.oid=ta.typnamespace
                  WHERE ta.oid=g202_arraytype AND an.nspname='YMES' AND ta.typname='_tblentositem'
                    AND ta.typtype='b' AND ta.typrelid=0 AND ta.typelem=g202_rowtype
                    AND pg_get_userbyid(ta.typowner)='postgres') THEN
        RAISE EXCEPTION 'ET202 exact linked row-array types required';
    END IF;
END;
$et202_verify$;

DO $et202_0063_acl$
BEGIN
 IF current_database()<>'ymes' OR current_user<>'postgres' OR session_user<>'postgres'
  OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999 THEN
  RAISE EXCEPTION 'ET2020063 requires reviewed PostgreSQL14 postgres context'; END IF;
 IF ARRAY(SELECT privilege.item::text FROM pg_catalog.pg_class target
          CROSS JOIN LATERAL unnest(target.relacl) AS privilege(item)
          WHERE target.oid='"YMES".tblentositem'::regclass ORDER BY privilege.item::text)
     IS DISTINCT FROM ARRAY['postgres=arwdDxt/postgres','ymes_app=arwd/postgres']::text[] THEN
  RAISE EXCEPTION 'ET2020063 exact ownerACL and app CRUD4 only required'; END IF;
 IF EXISTS(SELECT 1 FROM pg_catalog.pg_attribute attr
           WHERE attr.attrelid='"YMES".tblentositem'::regclass AND attr.attnum>0 AND attr.attacl IS NOT NULL)
  OR (SELECT target.relacl FROM pg_catalog.pg_class target WHERE target.oid='"YMES".pk_tblentositem'::regclass) IS NOT NULL
  OR EXISTS(SELECT 1 FROM pg_catalog.pg_type ty JOIN pg_catalog.pg_namespace tn ON tn.oid=ty.typnamespace
            WHERE tn.nspname='YMES' AND ty.typname IN ('tblentositem','_tblentositem') AND ty.typacl IS NOT NULL) THEN
  RAISE EXCEPTION 'ET2020063 no column/index/type extra ACL changes allowed'; END IF;
 IF NOT has_schema_privilege('ymes_app','YMES','USAGE')
  OR NOT has_table_privilege('ymes_app','"YMES".tblentositem','SELECT')
  OR NOT has_table_privilege('ymes_app','"YMES".tblentositem','INSERT')
  OR NOT has_table_privilege('ymes_app','"YMES".tblentositem','UPDATE')
  OR NOT has_table_privilege('ymes_app','"YMES".tblentositem','DELETE') THEN
  RAISE EXCEPTION 'ET2020063 required effective app USAGE/CRUD missing'; END IF;
END;
$et202_0063_acl$;
SET LOCAL ROLE ymes_app;
SELECT ositemno,ositemname,plandays,processno,returntype,description,creator,createdate,datastamp,reviser,revisedate
FROM "YMES".tblentositem WHERE false;
RESET ROLE;
SELECT set_config('ymes.et202_0063_native_verified',txid_current()::text,true);
SELECT jsonb_build_object('module','ET202_SCHEMA0063_NATIVE11','status','passed',
 'nativeColumns',11,'nativeNotNull',5,'nativeDefaults',1,'primaryKeys',1,'pkIndexes',1,
 'indexAttributes',1,'catalogColumnKeys',12,'derivedTypes',2,'appCrud4',true,
 'zeroRowAppSelect',true,'businessRowsRead',false,'dataChangedByVerifier',false) AS native_verification;

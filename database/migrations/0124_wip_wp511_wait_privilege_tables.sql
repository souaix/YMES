-- Create the two source-backed WP511 authorization tables that are missing from YMES.
-- Sources: MESTEST.XX_TBLWAITPRIV and MESTEST.TBLUSRUSERGROUP.
-- This is schema-only. It adds no membership or privilege data and grants no application access.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP511:0124', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0124 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app role/USAGE is missing; refusing 0124';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0124';
    END IF;
    IF to_regclass('"YMES".xxtblwaitpriv') IS NOT NULL
       OR to_regclass('"YMES".tblusrusergroup') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP511 source table already exists in YMES; compare it before 0124';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname) IN ('xxtblwaitpriv','tblusrusergroup')
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'A WP511 source relation exists outside YMES; inspect before 0124';
    END IF;
    IF to_regclass('"YMES".tblwipwaitbasis') IS NULL
       OR to_regclass('"YMES".tblwipwaitlotdisposition') IS NULL
       OR to_regclass('"YMES".tblwiplotstate') IS NULL
       OR to_regclass('"YMES".tblwiplotbasis') IS NULL
       OR to_regclass('"YMES".tblopbasis') IS NULL
       OR to_regclass('"YMES".tblprsprocessbasis') IS NULL THEN
        RAISE EXCEPTION 'WP511 prerequisite source tables are missing; inspect before 0124';
    END IF;
    IF obj_description(to_regclass('"YMES".tblwipwaitbasis'),'pg_class') IS DISTINCT FROM
           'MES Oracle source: MESTEST.TBLWIPWAITBASIS'
       OR obj_description(to_regclass('"YMES".tblwipwaitlotdisposition'),'pg_class') IS DISTINCT FROM
           'MES Oracle source: MESTEST.TBLWIPWAITLOTDISPOSITION' THEN
        RAISE EXCEPTION 'WP511 wait source markers differ; inspect before 0124';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblusrusergroup (
    userno varchar(10) NOT NULL,
    groupno varchar(20) NOT NULL,
    grouptype numeric(1,0) NOT NULL DEFAULT 0,
    agentof varchar(10) NOT NULL,
    agentdate timestamp without time zone,
    agentcreatedate timestamp without time zone,
    CONSTRAINT pk_tblusrusergroup PRIMARY KEY (userno, groupno, grouptype, agentof)
);
COMMENT ON TABLE "YMES".tblusrusergroup
    IS 'MES Oracle source: MESTEST.TBLUSRUSERGROUP';

CREATE TABLE "YMES".xxtblwaitpriv (
    groupno varchar(50) NOT NULL,
    go numeric(1,0) NOT NULL DEFAULT 0,
    jump_op numeric(1,0) NOT NULL DEFAULT 0,
    jump_main numeric(1,0) NOT NULL DEFAULT 0,
    jump_process_01 numeric(1,0) NOT NULL DEFAULT 0,
    jump_process_02 numeric(1,0) NOT NULL DEFAULT 0,
    jump_module numeric(1,0) NOT NULL DEFAULT 0,
    process_turning numeric(1,0) NOT NULL DEFAULT 0,
    inventory numeric(1,0) NOT NULL DEFAULT 0,
    scrap_inventory numeric(1,0) NOT NULL DEFAULT 0,
    split numeric(1,0) NOT NULL DEFAULT 0,
    merge numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(50) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblwaitpriv PRIMARY KEY (groupno)
);
COMMENT ON TABLE "YMES".xxtblwaitpriv
    IS 'MES Oracle source: MESTEST.XX_TBLWAITPRIV';

REVOKE ALL PRIVILEGES ON TABLE "YMES".tblusrusergroup, "YMES".xxtblwaitpriv FROM PUBLIC, ymes_app;

DO $verify$
DECLARE
    user_group_oid oid := to_regclass('"YMES".tblusrusergroup');
    wait_priv_oid oid := to_regclass('"YMES".xxtblwaitpriv');
BEGIN
    IF user_group_oid IS NULL OR wait_priv_oid IS NULL
       OR obj_description(user_group_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.TBLUSRUSERGROUP'
       OR obj_description(wait_priv_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.XX_TBLWAITPRIV' THEN
        RAISE EXCEPTION '0124 source-backed table verification failed';
    END IF;
    IF (SELECT count(*) FROM "YMES".tblusrusergroup) <> 0
       OR (SELECT count(*) FROM "YMES".xxtblwaitpriv) <> 0 THEN
        RAISE EXCEPTION '0124 created tables must remain empty';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=user_group_oid AND attnum > 0 AND NOT attisdropped) <> 6
       OR (SELECT count(*) FROM pg_catalog.pg_attribute
           WHERE attrelid=wait_priv_oid AND attnum > 0 AND NOT attisdropped) <> 14
       OR (SELECT count(*) FROM pg_catalog.pg_constraint
           WHERE conrelid=user_group_oid AND contype='p') <> 1
       OR (SELECT count(*) FROM pg_catalog.pg_constraint
           WHERE conrelid=wait_priv_oid AND contype='p') <> 1
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_constraint
                  WHERE conrelid IN (user_group_oid,wait_priv_oid) AND contype IN ('f','u','c','x'))
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_trigger
                  WHERE tgrelid IN (user_group_oid,wait_priv_oid) AND NOT tgisinternal) THEN
        RAISE EXCEPTION '0124 columns or constraints differ from the Oracle source';
    END IF;
    IF has_table_privilege('ymes_app',user_group_oid,'SELECT')
       OR has_table_privilege('ymes_app',user_group_oid,'INSERT')
       OR has_table_privilege('ymes_app',user_group_oid,'UPDATE')
       OR has_table_privilege('ymes_app',user_group_oid,'DELETE')
       OR has_table_privilege('ymes_app',wait_priv_oid,'SELECT')
       OR has_table_privilege('ymes_app',wait_priv_oid,'INSERT')
       OR has_table_privilege('ymes_app',wait_priv_oid,'UPDATE')
       OR has_table_privilege('ymes_app',wait_priv_oid,'DELETE') THEN
        RAISE EXCEPTION '0124 must not grant application table access';
    END IF;
END
$verify$;

COMMIT;

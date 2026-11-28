-- Create the source-backed WP509 ERF lot-disposition privilege table only.
-- Source: database/reference/mestest_wip_wp509_priv_*.
-- No permissions or Oracle privilege rows are copied; app access remains denied.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP509:0125', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0125 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES')
       OR NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app')
       OR NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'YMES schema or ymes_app role/USAGE is missing; refusing 0125';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0125';
    END IF;
    IF to_regclass('"YMES".xx_tblqcholdpriv') IS NOT NULL THEN
        RAISE EXCEPTION 'XX_TBLQCHOLDPRIV already exists in YMES; compare it before 0125';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='xx_tblqcholdpriv'
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'XX_TBLQCHOLDPRIV exists outside YMES; inspect before 0125';
    END IF;
    IF to_regclass('"YMES".tblusrusergroup') IS NULL
       OR to_regclass('"YMES".tblwiperfbasis') IS NULL
       OR to_regclass('"YMES".tblwiperflotdisposition') IS NULL
       OR to_regclass('"YMES".tblwiperfocapdisposition') IS NULL THEN
        RAISE EXCEPTION 'WP509 source-backed prerequisites are missing; inspect before 0125';
    END IF;
    IF obj_description(to_regclass('"YMES".tblusrusergroup'),'pg_class') IS DISTINCT FROM
           'MES Oracle source: MESTEST.TBLUSRUSERGROUP' THEN
        RAISE EXCEPTION 'TBLUSRUSERGROUP source marker differs; inspect before 0125';
    END IF;
END
$guard$;

CREATE TABLE "YMES".xx_tblqcholdpriv (
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
    "merge" numeric(1,0) NOT NULL DEFAULT 0,
    back_disposition numeric(1,0) NOT NULL DEFAULT 0,
    qty_adjust numeric(1,0) NOT NULL DEFAULT 0,
    scrap numeric(1,0) NOT NULL DEFAULT 0,
    "return" numeric(1,0) NOT NULL DEFAULT 0,
    creator varchar(50) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblqcholdpriv PRIMARY KEY (groupno)
);
COMMENT ON TABLE "YMES".xx_tblqcholdpriv
    IS 'MES Oracle source: MESTEST.XX_TBLQCHOLDPRIV; group based ERF lot disposition permissions';
REVOKE ALL PRIVILEGES ON TABLE "YMES".xx_tblqcholdpriv FROM PUBLIC, ymes_app;

DO $verify$
DECLARE
    privilege_oid oid := to_regclass('"YMES".xx_tblqcholdpriv');
BEGIN
    IF privilege_oid IS NULL
       OR obj_description(privilege_oid,'pg_class') IS DISTINCT FROM
          'MES Oracle source: MESTEST.XX_TBLQCHOLDPRIV; group based ERF lot disposition permissions' THEN
        RAISE EXCEPTION '0125 source table marker verification failed';
    END IF;
    IF (SELECT count(*) FROM pg_catalog.pg_attribute
        WHERE attrelid=privilege_oid AND attnum>0 AND NOT attisdropped) <> 18
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_attribute
                  WHERE attrelid=privilege_oid AND attnum>0 AND NOT attisdropped AND NOT attnotnull)
       OR (SELECT count(*) FROM pg_catalog.pg_attrdef WHERE adrelid=privilege_oid) <> 15
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_attrdef d
                  WHERE d.adrelid=privilege_oid AND pg_catalog.pg_get_expr(d.adbin,d.adrelid) <> '0')
       OR (SELECT count(*) FROM pg_catalog.pg_constraint
           WHERE conrelid=privilege_oid AND contype='p'
             AND pg_catalog.pg_get_constraintdef(oid)='PRIMARY KEY (groupno)') <> 1
       OR (SELECT count(*) FROM pg_catalog.pg_constraint WHERE conrelid=privilege_oid) <> 1
       OR EXISTS (SELECT 1 FROM pg_catalog.pg_trigger
                  WHERE tgrelid=privilege_oid AND NOT tgisinternal) THEN
        RAISE EXCEPTION '0125 columns, defaults, keys, or triggers differ from the Oracle source';
    END IF;
    IF has_table_privilege('ymes_app',privilege_oid,'SELECT')
       OR has_table_privilege('ymes_app',privilege_oid,'INSERT')
       OR has_table_privilege('ymes_app',privilege_oid,'UPDATE')
       OR has_table_privilege('ymes_app',privilege_oid,'DELETE') THEN
        RAISE EXCEPTION '0125 must not grant application table access';
    END IF;
    IF (SELECT count(*) FROM "YMES".xx_tblqcholdpriv) <> 0 THEN
        RAISE EXCEPTION '0125 privilege table must remain empty';
    END IF;
END
$verify$;

COMMIT;

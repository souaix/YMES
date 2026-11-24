-- Empty MES-source table required to reproduce the SC501 group lookup.
-- Oracle source: MESTEST.TBLQCITEMGROUPDETAIL in
-- database/reference/mestest_qc205_variable_dependencies_source_ddl.sql.
-- This migration creates structure only; it does not copy MES business rows.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing SC501 lookup migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing SC501 lookup migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing SC501 lookup migration';
    END IF;
    IF to_regclass('"YMES".tblqcitemgroupdetail') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblqcitemgroupdetail already exists; compare its live structure before migrating';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblqcitemgroupdetail'
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An object named tblqcitemgroupdetail exists outside YMES';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblqcitemforvariables','tblqcitemgroupbasis','tblqcinspmachineparameter'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; refusing SC501 lookup migration', object_name;
        END IF;
        IF NOT has_table_privilege('ymes_app',format('%I.%I','YMES',object_name),'SELECT') THEN
            RAISE EXCEPTION 'ymes_app lacks SELECT on YMES.%', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblqcitemgroupdetail (
    groupno varchar(25),
    qcitemno varchar(25),
    showorder numeric(3,0) DEFAULT 0 CONSTRAINT n_tblqcitemgroupdetail_01 NOT NULL,
    CONSTRAINT pk_tblqcitemgroupdetail PRIMARY KEY (groupno, qcitemno)
);

COMMENT ON TABLE "YMES".tblqcitemgroupdetail IS 'MES Oracle source: MESTEST.TBLQCITEMGROUPDETAIL; SC501 lookup';
GRANT SELECT ON TABLE "YMES".tblqcitemgroupdetail TO ymes_app;

COMMIT;

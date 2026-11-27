-- The source-aligned Equipment Type consumable basis used by WP303/WP304 lookups.
-- Empty source table only; no equipment or consumable business rows are copied.
BEGIN;

DO $guard$
DECLARE object_name text := 'tbleqpconsumebasis';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WP304 lookup migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP304 lookup migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WP304 lookup migration';
    END IF;
    IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)=object_name AND n.nspname<>'YMES'
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_class WHERE lower(relname)='ix_wp304_consume_basis_lookup' AND relkind='i') THEN
        RAISE EXCEPTION 'Index ix_wp304_consume_basis_lookup already exists; inspect before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tbleqpconsumebasis (
    equipmenttype varchar(25) NOT NULL,
    consumeno varchar(25) NOT NULL,
    quantity numeric(8,0) NOT NULL,
    toleranceqty numeric(16,8) NOT NULL,
    period numeric(5,0) NOT NULL,
    toleranceperiod numeric(5,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tbleqpconsumebasis PRIMARY KEY (equipmenttype, consumeno)
);

CREATE INDEX ix_wp304_consume_basis_lookup
    ON "YMES".tbleqpconsumebasis (consumeno COLLATE "C", equipmenttype COLLATE "C");

COMMENT ON TABLE "YMES".tbleqpconsumebasis IS
    'MES Oracle source: MESTEST.TBLEQPCONSUMEBASIS; empty equipment consumable basis';
COMMENT ON INDEX "YMES".ix_wp304_consume_basis_lookup IS
    'YMES-only WP304 lookup support index; not an Oracle source index';

GRANT SELECT ON "YMES".tbleqpconsumebasis TO ymes_app;

COMMIT;

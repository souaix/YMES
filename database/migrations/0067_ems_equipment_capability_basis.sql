-- ES107 / PD102 shared equipment capability basis tables.
-- Source: checked MESTEST DDL in database/reference/mestest_prd_eqp_cap_item_source_ddl.sql.
-- Live catalog audit (2026-11-23) found neither table in any schema.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing ES107 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing ES107 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing ES107 migration';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        WHERE c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND c.relname IN ('tblemseqpcapbasis', 'tblprdopcapitem')
    ) THEN
        RAISE EXCEPTION 'An ES107/PD102 target already exists in a schema; compare it before proceeding';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblemseqpcapbasis (
    capitem varchar(50) NOT NULL,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblemseqpcapbasis PRIMARY KEY (capitem)
);
COMMENT ON TABLE "YMES".tblemseqpcapbasis IS 'MES Oracle source: MESTEST.TBLEMSEQPCAPBASIS';

CREATE TABLE "YMES".tblprdopcapitem (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    capitem varchar(50) NOT NULL,
    capvalue varchar(20) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    description varchar(255),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblprdopcapitem PRIMARY KEY (productno, productversion, opno, capitem)
);
COMMENT ON TABLE "YMES".tblprdopcapitem IS 'MES Oracle source: MESTEST.TBLPRDOPCAPITEM';

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblemseqpcapbasis TO ymes_app;
GRANT SELECT ON "YMES".tblprdopcapitem TO ymes_app;

COMMIT;

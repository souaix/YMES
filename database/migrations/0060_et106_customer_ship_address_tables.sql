-- HOME migration 0060: native ET106 address/declaration tables.
-- Numbered after source review; original proposal comments below are retained history.
-- Execution and independent verification are recorded separately; no seed data.
-- ET106 PREPARED / NOT EXECUTED; no migration number allocated.
-- Native: database/reference/mestest_customer_basis_source_ddl.sql:68-115
-- PG reference: database/reference/mestest_customer_basis_create.sql:69-112
-- Root actual absence/ACL audit:79e553e411b7c341477f9fa1eb8c4f6e48fc241055c483dc6717503e9ba1f682.
-- ORDERING: deploy and test optional-absent/present ET103/104/105 compatibility
-- API BEFORE creating ShipAddr. The old API8db rejects its new reference columns.
-- Root's reviewed wrapper must verify that real release receipt first, then set
-- session GUC ymes.et106_compat_absent_verified=yes for this connection.
-- This GUC is an explicit operator assertion, NOT proof obtained from PostgreSQL.
-- Before execution: private full backup + nonempty restore-list, fresh metadata,
-- and an approved numbered forward migration derived from this prepared file.
-- Wrapper must compare complete pre/post permanent catalog/ACL snapshots inside
-- this transaction: allow ONLY these two new tables, their row/array types,
-- Addr PK/index, 35 native columns/5 defaults, and own-table ymes_app CRUD.
-- Preserve parent metadata/ACL, all roles/schema/defaultACL and unrelated objects.
-- No seed, repair, new ID, Declaration PK/index/unique/FK, parent ALTER or REVOKE.

BEGIN;

DO $et106_before$
DECLARE
    schema_oid oid;
    actor_oid oid;
    app_oid oid;
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres'
       OR session_user <> 'postgres' THEN
        RAISE EXCEPTION 'ET106 requires reviewed postgres connection to database ymes';
    END IF;
    IF current_setting('ymes.et106_compat_absent_verified', true) IS DISTINCT FROM 'yes' THEN
        RAISE EXCEPTION 'ET106 compatibility API absent-branch verification is not confirmed';
    END IF;
    SELECT oid INTO actor_oid FROM pg_catalog.pg_roles WHERE rolname = 'postgres';
    SELECT oid INTO app_oid FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app';
    SELECT oid INTO schema_oid FROM pg_catalog.pg_namespace
      WHERE nspname = 'YMES' AND nspowner = actor_oid;
    IF schema_oid IS NULL OR app_oid IS NULL
       OR NOT has_schema_privilege(app_oid, schema_oid, 'USAGE')
       OR has_schema_privilege(app_oid, schema_oid, 'CREATE') THEN
        RAISE EXCEPTION 'ET106 existing schema/owner/app schema privileges differ from reviewed baseline';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_catalog.pg_default_acl) THEN
        RAISE EXCEPTION 'ET106 default ACL baseline changed; review instead of changing existing grants';
    END IF;
    -- All schemas and all relation kinds, including an existing index/view/type
    -- owner, are checked. No IF NOT EXISTS silently accepts a changed deployment.
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class
        WHERE lower(relname) IN (
            'tblentcustomershipaddr', 'tblentcustomershipdeclaration',
            'pk_tblentcustomershipaddr', 'tblentcustomershipaddr_pkey',
            'pk_tblentcustomershipdeclaration', 'tblentcustomershipdeclaration_pkey')
    ) OR EXISTS (
        SELECT 1 FROM pg_catalog.pg_type
        WHERE lower(typname) IN (
            'tblentcustomershipaddr', '_tblentcustomershipaddr',
            'tblentcustomershipdeclaration', '_tblentcustomershipdeclaration')
    ) THEN
        RAISE EXCEPTION 'ET106 table/index/row-array type name is occupied; preserve existing objects';
    END IF;
END;
$et106_before$;

CREATE TABLE "YMES".tblentcustomershipaddr (
    shipaddr varchar(255) CONSTRAINT n_tblentcustomershipaddr_01 NOT NULL,
    customerno varchar(50) CONSTRAINT n_tblentcustomershipaddr_02 NOT NULL,
    telno varchar(25),
    contactorname varchar(25),
    shipcustomername varchar(30),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    flow varchar(10) DEFAULT 'N/A' CONSTRAINT n_tblentcustomershipaddr_03 NOT NULL,
    declarationflag varchar(1) DEFAULT '0' CONSTRAINT n_tblentcustomershipaddr_04 NOT NULL,
    overseaflag varchar(1),
    faxno varchar(25),
    email varchar(255),
    shipaddr2 varchar(500),
    country varchar(50),
    companyname varchar(150),
    takerno varchar(50),
    forwarderno varchar(50),
    broker varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentcustomershipaddr PRIMARY KEY (customerno, shipaddr)
);

-- Native permits duplicate natural-seven groups: do not add a synthetic locator.
CREATE TABLE "YMES".tblentcustomershipdeclaration (
    customerno varchar(50) CONSTRAINT n_customershipdeclaration_01 NOT NULL,
    shipto varchar(10) CONSTRAINT n_customershipdeclaration_02 NOT NULL,
    flow varchar(10) CONSTRAINT n_customershipdeclaration_03 NOT NULL,
    overseaflag varchar(1) CONSTRAINT n_customershipdeclaration_04 NOT NULL,
    declarationflag varchar(1) CONSTRAINT n_customershipdeclaration_05 NOT NULL,
    bondflag varchar(1) CONSTRAINT n_customershipdeclaration_06 NOT NULL,
    handcarryflag varchar(1) CONSTRAINT n_customershipdeclaration_07 NOT NULL,
    declarationway varchar(50) CONSTRAINT n_customershipdeclaration_08 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone DEFAULT LOCALTIMESTAMP,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone
);

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblentcustomershipaddr,
    "YMES".tblentcustomershipdeclaration
TO ymes_app;

-- Root wrapper inserts reviewed in-transaction full-catalog/ACL comparison here.
-- After COMMIT, perform independent native35/PK/default/ACL readback and verify
-- all three compatibility registry paths before enabling the ET106 writer.
COMMIT;

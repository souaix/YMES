-- SC216 read-only history API and the two missing approved QC Item group lookups.
-- Source DDL: MESTEST.TBLQCITEMATTGROUPORDERBASIS and
-- MESTEST.TBLQCITEMGROUPBASIS_PMI; PostgreSQL CREATE-only references are in
-- mestest_qc213_attribute_item_group_create.sql and
-- mestest_qc216_variable_item_group_pmi_create.sql.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing SC216 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing SC216 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing SC216 migration';
    END IF;
    IF to_regclass('"YMES".tblqcitembindlog') IS NULL THEN
        RAISE EXCEPTION 'TBLQCITEMBINDLOG is missing; compare migration 0019 before SC216 migration';
    END IF;
    IF to_regclass('"YMES".tblqcitemforattributes') IS NULL
       OR to_regclass('"YMES".tblqcitemforvariables') IS NULL
       OR to_regclass('"YMES".tblqcitemgroupbasis') IS NULL THEN
        RAISE EXCEPTION 'An existing QC Item lookup table is missing; refusing SC216 migration';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE n.nspname='YMES'
          AND c.relname IN ('tblqcitemattgrouporderbasis','tblqcitemgroupbasis_pmi')
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An SC216 group lookup object already exists; compare it before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblqcitemattgrouporderbasis (
    groupno varchar(25) NOT NULL,
    groupname varchar(50) NOT NULL,
    lotisvalid numeric(1,0) DEFAULT 0,
    issuestate numeric(1,0) DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemattgrouporderbasis PRIMARY KEY (groupno)
);
COMMENT ON TABLE "YMES".tblqcitemattgrouporderbasis IS 'MES Oracle source: MESTEST.TBLQCITEMATTGROUPORDERBASIS';

CREATE TABLE "YMES".tblqcitemgroupbasis_pmi (
    groupno varchar(25) NOT NULL,
    groupname varchar(50),
    lotisvalid numeric(1,0) DEFAULT 0,
    componentcollect numeric(1,0) DEFAULT 0,
    planno varchar(25),
    formname varchar(50),
    weburl varchar(100),
    unionerf numeric(1,0) DEFAULT 0,
    errorreportname varchar(50),
    noticereportname varchar(50),
    issuestate numeric(1,0) DEFAULT 0,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    cursormode numeric(1,0),
    qcclass varchar(30),
    getfilepath varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcitemgroupbasis_pmi PRIMARY KEY (groupno)
);
COMMENT ON TABLE "YMES".tblqcitemgroupbasis_pmi IS 'MES Oracle source: MESTEST.TBLQCITEMGROUPBASIS_PMI';

GRANT SELECT ON TABLE
    "YMES".tblqcitembindlog,
    "YMES".tblqcitemattgrouporderbasis,
    "YMES".tblqcitemgroupbasis_pmi
TO ymes_app;

COMMIT;

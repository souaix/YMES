BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; stop before WP307 migration 0098';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before WP307 migration 0098';
    END IF;
    IF to_regclass('"YMES".tblusrshiftbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblusrshiftbasis is missing; compare the verified source before WP307 migration 0098';
    END IF;
    IF has_table_privilege('ymes_app', '"YMES".tblusrshiftbasis', 'SELECT') THEN
        RAISE EXCEPTION 'ymes_app already has SELECT on tblusrshiftbasis; review the existing grant before WP307 migration 0098';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE lower(c.relname) IN (
            'tblsmdareashift', 'tblsmdareaoperator',
            'tblwipareaoperatorstate', 'tblwipareaoperatorlog'
        )
          AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
    ) THEN
        RAISE EXCEPTION 'A WP307/SD108 source table exists in some schema; compare it with the verified source before migration 0098';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = 'YMES'
          AND c.relname IN ('pk_tblsmdareashift', 'pk_tblsmdareaoperator', 'pk_tblwipareaoperatorstate')
    ) THEN
        RAISE EXCEPTION 'A WP307/SD108 primary-key index name already exists in YMES; review before migration 0098';
    END IF;
END
$$;

-- Empty PostgreSQL replicas of verified Oracle MESTEST sources. The two SMD
-- assignment tables are required for WP307 read/eligibility checks; no source
-- business rows are copied. The history table has no source key or index.
CREATE TABLE "YMES".tblsmdareashift (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    managerno varchar(10) NOT NULL,
    exceptiontime numeric(4,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareashift PRIMARY KEY (areano, departmentno, shiftno)
);
COMMENT ON TABLE "YMES".tblsmdareashift IS 'MES Oracle source: MESTEST.TBLSMDAREASHIFT';

CREATE TABLE "YMES".tblsmdareaoperator (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblsmdareaoperator PRIMARY KEY (areano, departmentno, shiftno, userno)
);
COMMENT ON TABLE "YMES".tblsmdareaoperator IS 'MES Oracle source: MESTEST.TBLSMDAREAOPERATOR';

CREATE TABLE "YMES".tblwipareaoperatorstate (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    logincomment varchar(255),
    logintime timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblwipareaoperatorstate PRIMARY KEY (userno)
);
COMMENT ON TABLE "YMES".tblwipareaoperatorstate IS 'MES Oracle source: MESTEST.TBLWIPAREAOPERATORSTATE';

CREATE TABLE "YMES".tblwipareaoperatorlog (
    areano varchar(20) NOT NULL,
    departmentno varchar(20) NOT NULL,
    shiftno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    logintime timestamp without time zone NOT NULL,
    logouttime timestamp without time zone NOT NULL,
    exceptiontime numeric(18,0) NOT NULL,
    logincomment varchar(255),
    logoutcomment varchar(255)
);
COMMENT ON TABLE "YMES".tblwipareaoperatorlog IS 'MES Oracle source: MESTEST.TBLWIPAREAOPERATORLOG';

-- Read-only API access. Login/logout write grants will be added only with the
-- source-verified atomic command implementation.
GRANT SELECT ON "YMES".tblsmdareashift,
                "YMES".tblsmdareaoperator,
                "YMES".tblwipareaoperatorstate,
                "YMES".tblwipareaoperatorlog,
                "YMES".tblusrshiftbasis TO ymes_app;

COMMIT;

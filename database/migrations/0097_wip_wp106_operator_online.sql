BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; stop before WP106 migration 0097';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before WP106 migration 0097';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE lower(c.relname) IN ('tblwipoperatorstate', 'tblwipoperatorlog', 'tblwiplotloginstate')
          AND c.relkind IN ('r', 'p')
    ) THEN
        RAISE EXCEPTION 'A WP104/WP106 operator table exists in some schema; compare it with the verified source before migration 0097';
    END IF;
END
$$;

-- Empty PostgreSQL replicas of verified Oracle MESTEST sources used by WP104-WP106.
-- No employee/session rows are copied. The app receives SELECT only until the
-- source-backed sign-in/sign-out transaction is implemented and separately reviewed.
CREATE TABLE "YMES".tblwipoperatorstate (
    userno varchar(10) NOT NULL,
    shiftno varchar(20) NOT NULL,
    logindate timestamp without time zone NOT NULL,
    workdate timestamp without time zone NOT NULL,
    opno varchar(20) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    loginplaceno varchar(50) NOT NULL,
    CONSTRAINT pk_tblwipoperatorstate PRIMARY KEY (userno, multioperatormode, loginplaceno)
);
COMMENT ON TABLE "YMES".tblwipoperatorstate IS 'MES Oracle source: MESTEST.TBLWIPOPERATORSTATE; WP104/WP106';

CREATE TABLE "YMES".tblwipoperatorlog (
    sid varchar(30) NOT NULL,
    userno varchar(10) NOT NULL,
    shiftno varchar(20) NOT NULL,
    logindate timestamp without time zone NOT NULL,
    logoutdate timestamp without time zone NOT NULL,
    exceptiontime numeric(10,2) DEFAULT 0 NOT NULL,
    worktime numeric(10,2) DEFAULT 0 NOT NULL,
    realworktime numeric(10,2) DEFAULT 0 NOT NULL,
    stdworktime numeric(10,2) DEFAULT 0 NOT NULL,
    qty numeric(10,4) DEFAULT 0 NOT NULL,
    workdate timestamp without time zone NOT NULL,
    opno varchar(20) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    loginplaceno varchar(50) NOT NULL,
    eventtime timestamp without time zone,
    CONSTRAINT pk_tblwipoperatorlog PRIMARY KEY (sid)
);
COMMENT ON TABLE "YMES".tblwipoperatorlog IS 'MES Oracle source: MESTEST.TBLWIPOPERATORLOG; WP104/WP106';

CREATE TABLE "YMES".tblwiplotloginstate (
    datatype numeric(1,0) NOT NULL,
    datasource numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    userno varchar(10) NOT NULL,
    username varchar(50) NOT NULL,
    checkintime timestamp without time zone NOT NULL,
    cicreator varchar(10) NOT NULL,
    cicreatedate timestamp without time zone NOT NULL,
    checkouttime timestamp without time zone,
    cocreator varchar(10),
    cocreatedate timestamp without time zone,
    loginplaceno varchar(50) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    CONSTRAINT pk_tblwiplotloginstate PRIMARY KEY (lotno, opno, userno, checkintime, loginplaceno)
);
CREATE INDEX i_tblwiplotloginstate_01 ON "YMES".tblwiplotloginstate (loggroupserial);
COMMENT ON TABLE "YMES".tblwiplotloginstate IS 'MES Oracle source: MESTEST.TBLWIPLOTLOGINSTATE; WP305/WP106';

GRANT SELECT ON "YMES".tblwipoperatorstate,
                "YMES".tblwipoperatorlog,
                "YMES".tblwiplotloginstate TO ymes_app;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; stop before RT605 migration 0096';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before RT605 migration 0096';
    END IF;
    IF to_regclass('"YMES".tblwipcont_resource') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblwipcont_resource already exists; compare it with verified WP403 source before migration 0096';
    END IF;
END
$$;

-- Empty Oracle MESTEST replica. Column definitions, primary key, and all four
-- nonunique indexes come from the WP403 source DDL; no business rows are copied.
CREATE TABLE "YMES".tblwipcont_resource (
    lotno varchar(50) NOT NULL,
    mono varchar(50) NOT NULL,
    baselotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    resclass numeric(2,0) NOT NULL,
    restype varchar(50) NOT NULL,
    resitem varchar(50) NOT NULL,
    resvalue numeric(10,2) NOT NULL,
    stdvalue numeric(10,2) NOT NULL,
    inputqty numeric(12,4) NOT NULL,
    userno varchar(10) NOT NULL,
    eventtime timestamp without time zone NOT NULL,
    erpflag numeric(1,0),
    workreportid varchar(30),
    CONSTRAINT pk_tblwipcont_resource PRIMARY KEY (loggroupserial, resitem, eventtime, userno)
);
CREATE INDEX i_tblwipcont_resource_01 ON "YMES".tblwipcont_resource (loggroupserial);
CREATE INDEX i_tblwipcont_resource_02 ON "YMES".tblwipcont_resource (lotno);
CREATE INDEX i_tblwipcont_resource_03 ON "YMES".tblwipcont_resource (lotno, resitem);
CREATE INDEX i_tblwipcont_resource_04 ON "YMES".tblwipcont_resource (eventtime);
COMMENT ON TABLE "YMES".tblwipcont_resource IS 'MES Oracle source: MESTEST.TBLWIPCONT_RESOURCE (WP403)';

REVOKE ALL ON "YMES".tblwipcont_resource FROM ymes_app;
GRANT SELECT ON "YMES".tblwipcont_resource TO ymes_app;

COMMIT;

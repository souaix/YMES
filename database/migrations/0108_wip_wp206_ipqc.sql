-- WP206 source tables were absent from the YMES clone.
-- Create the four verified MESTEST source structures and one YMES-only idempotency ledger.
-- No Oracle business rows are copied; TBLWIPCONT_ABNORMAL intentionally has no PK.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP206 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WP206 migration';
    END IF;
    IF has_schema_privilege('ymes_app','YMES','CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review database security before WP206 migration';
    END IF;
    IF to_regclass('"YMES".tblopbasis') IS NULL OR to_regclass('"YMES".tblwiplotstate') IS NULL THEN
        RAISE EXCEPTION 'WP206 requires existing YMES.tblopbasis and YMES.tblwiplotstate';
    END IF;
    IF NOT has_table_privilege('ymes_app','"YMES".tblopbasis','SELECT')
       OR NOT has_table_privilege('ymes_app','"YMES".tblwiplotstate','SELECT') THEN
        RAISE EXCEPTION 'ymes_app lacks SELECT on an existing WP206 operation/lot dependency';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblopabnormal','tblqcabnormalbasis','tblqcabnormalposition',
        'tblwipcont_abnormal','ym_wp206_request'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name
              AND n.nspname<>'YMES'
              AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; inspect before migrating', object_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE c.relkind='i'
          AND lower(c.relname) IN (
              'pk_tblopabnormal','pk_tblqcabnormalbasis',
              'pk_tblqcabnormalposition','i_tblwipcont_abnormal_01'
          )
    ) THEN
        RAISE EXCEPTION 'A WP206 source index name already exists; inspect before migrating';
    END IF;
END
$guard$;

-- TBLOPABNORMAL is defined by the existing Operation/System source reference.
-- It is created here once because the live YMES schema did not contain it.
CREATE TABLE "YMES".tblopabnormal (
    opno varchar(20) NOT NULL,
    abnormalno varchar(20) NOT NULL,
    CONSTRAINT pk_tblopabnormal PRIMARY KEY (opno,abnormalno)
);
COMMENT ON TABLE "YMES".tblopabnormal IS 'MES Oracle source: MESTEST.TBLOPABNORMAL';

CREATE TABLE "YMES".tblqcabnormalbasis (
    abnormalno varchar(20) NOT NULL,
    abnormalname varchar(100) NOT NULL,
    abnormaltype varchar(20) NOT NULL,
    linkname varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    priority numeric(2,0) DEFAULT 0,
    CONSTRAINT pk_tblqcabnormalbasis PRIMARY KEY (abnormalno)
);
COMMENT ON TABLE "YMES".tblqcabnormalbasis IS 'MES Oracle source: MESTEST.TBLQCABNORMALBASIS';

CREATE TABLE "YMES".tblqcabnormalposition (
    abnormalno varchar(20) NOT NULL,
    position varchar(50) NOT NULL,
    CONSTRAINT pk_tblqcabnormalposition PRIMARY KEY (abnormalno,position)
);
COMMENT ON TABLE "YMES".tblqcabnormalposition IS 'MES Oracle source: MESTEST.TBLQCABNORMALPOSITION';

CREATE TABLE "YMES".tblwipcont_abnormal (
    lotserial varchar(55),
    opno varchar(20),
    abnormalno varchar(20),
    position varchar(50),
    abnormalqty numeric(6,0) DEFAULT 0 NOT NULL,
    userno varchar(10),
    lotno varchar(50),
    shiftno varchar(20),
    eventtime timestamp without time zone
);
COMMENT ON TABLE "YMES".tblwipcont_abnormal IS 'MES Oracle source: MESTEST.TBLWIPCONT_ABNORMAL; intentionally has no primary key';
CREATE INDEX i_tblwipcont_abnormal_01 ON "YMES".tblwipcont_abnormal (lotserial);

CREATE TABLE "YMES".ym_wp206_request (
    requestid uuid NOT NULL,
    creator varchar(10) NOT NULL,
    payloadsha256 varchar(64) NOT NULL,
    insertedcount integer NOT NULL CHECK (insertedcount BETWEEN 1 AND 100),
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_ym_wp206_request PRIMARY KEY (requestid)
);
COMMENT ON TABLE "YMES".ym_wp206_request IS 'YMES-only WP206 request idempotency ledger; not an Oracle MES table';

REVOKE ALL ON "YMES".tblopabnormal,
    "YMES".tblqcabnormalbasis,
    "YMES".tblqcabnormalposition,
    "YMES".tblwipcont_abnormal,
    "YMES".ym_wp206_request FROM PUBLIC;

GRANT SELECT ON "YMES".tblopabnormal,
    "YMES".tblqcabnormalbasis,
    "YMES".tblqcabnormalposition TO ymes_app;
GRANT INSERT ON "YMES".tblwipcont_abnormal TO ymes_app;
GRANT SELECT, INSERT ON "YMES".ym_wp206_request TO ymes_app;

COMMIT;

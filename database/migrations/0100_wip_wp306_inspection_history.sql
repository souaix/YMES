-- WP306 reads the MESTEST inspection history tables verified by the office PC.
-- Empty source tables only; no inspection or operator rows are inserted.
BEGIN;

DO $guard$
DECLARE object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WP306 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP306 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WP306 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY['tblwipeqpqclistlog','tblwipeqpqclistdetail'] LOOP
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
    END LOOP;

    FOREACH object_name IN ARRAY ARRAY[
        'ix_wp306_qc_log_equipment_serial','ix_wp306_qc_log_created_serial'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            WHERE lower(c.relname)=object_name AND c.relkind='i'
        ) THEN
            RAISE EXCEPTION 'Index name % already exists; inspect before migrating', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblwipeqpqclistlog (
    qclistserial varchar(100) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    qclistno varchar(100) NOT NULL,
    lotserial varchar(55),
    loggroupserial varchar(55),
    qcresult numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    qctaskno varchar(30),
    insptype numeric(2,0),
    shiftno varchar(20),
    lotno varchar(50),
    inspstarttime varchar(20),
    qctaskstatus numeric(2,0),
    belongdate timestamp without time zone,
    planqctime timestamp without time zone,
    CONSTRAINT pk_tblwipeqpqclistlog PRIMARY KEY (qclistserial)
);

CREATE TABLE "YMES".tblwipeqpqclistdetail (
    qclistserial varchar(120) NOT NULL,
    qcitem varchar(1000) NOT NULL,
    qcresult numeric(1,0) NOT NULL,
    inputvalue varchar(50),
    stdvalue varchar(12),
    maxivalue varchar(12),
    minivalue varchar(12),
    inputdatacount numeric(2,0) DEFAULT 1 NOT NULL,
    qcorder numeric(2,0) DEFAULT -99 NOT NULL,
    qctype numeric(1,0),
    d01 varchar(50),
    d02 varchar(50),
    d03 varchar(50),
    d04 varchar(50),
    d05 varchar(50),
    d06 varchar(50),
    d07 varchar(50),
    d08 varchar(50),
    d09 varchar(50),
    d10 varchar(50),
    CONSTRAINT pk_tblwipeqpqclistdetail PRIMARY KEY (qclistserial, qcitem)
);

-- YMES-only access paths for the bounded equipment/date search and stable list order.
CREATE INDEX ix_wp306_qc_log_equipment_serial
    ON "YMES".tblwipeqpqclistlog (equipmentno, qclistserial COLLATE "C");
CREATE INDEX ix_wp306_qc_log_created_serial
    ON "YMES".tblwipeqpqclistlog (createdate, qclistserial COLLATE "C");

COMMENT ON TABLE "YMES".tblwipeqpqclistlog IS
    'MES Oracle source: MESTEST.TBLWIPEQPQCLISTLOG; empty WP306 history source';
COMMENT ON TABLE "YMES".tblwipeqpqclistdetail IS
    'MES Oracle source: MESTEST.TBLWIPEQPQCLISTDETAIL; empty WP306 detail source';
COMMENT ON INDEX "YMES".ix_wp306_qc_log_equipment_serial IS
    'YMES API support index; not an Oracle source index';
COMMENT ON INDEX "YMES".ix_wp306_qc_log_created_serial IS
    'YMES API support index; not an Oracle source index';

GRANT SELECT ON "YMES".tblwipeqpqclistlog,
    "YMES".tblwipeqpqclistdetail TO ymes_app;

COMMIT;

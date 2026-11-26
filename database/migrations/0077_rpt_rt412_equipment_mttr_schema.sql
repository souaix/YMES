-- RT412 requires the MES equipment state-control, current-state, and history
-- structures. Source DDL is verified in the MESTEST references listed below.
-- This migration creates empty YMES structures only; it does not import rows.
-- ymes_app receives SELECT only for the RT412 read-only API.
-- Sources: mestest_equipment_state_references_create.sql and
--          mestest_wip_wp305_create.sql.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing RT412 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing RT412 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing RT412 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tbleqpstatecontrol','tblemsequipmentstate','tblemsequipmentstatelog'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name
              AND n.nspname<>'YMES'
              AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE c.relname IN ('i_equipmentstatelog_01','i_tblemsequipmentstatelog_01')
          AND c.relkind='i'
    ) THEN
        RAISE EXCEPTION 'An RT412 history index name already exists; inspect before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tbleqpstatecontrol (
    controltype varchar(50) NOT NULL,
    controlno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    statename varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpstatecontrol PRIMARY KEY (controltype, controlno)
);

CREATE TABLE "YMES".tblemsequipmentstate (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblemsequipmentstate PRIMARY KEY (equipmentno)
);

CREATE TABLE "YMES".tblemsequipmentstatelog (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    endtime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblemsequipmentstatelog PRIMARY KEY (eqpserialno)
);

CREATE INDEX i_equipmentstatelog_01
    ON "YMES".tblemsequipmentstatelog (starttime);
CREATE INDEX i_tblemsequipmentstatelog_01
    ON "YMES".tblemsequipmentstatelog (equipmentno, starttime, endtime);

COMMENT ON TABLE "YMES".tbleqpstatecontrol IS
    'MES Oracle source: MESTEST.TBLEQPSTATECONTROL';
COMMENT ON TABLE "YMES".tblemsequipmentstate IS
    'MES Oracle source: MESTEST.TBLEMSEQUIPMENTSTATE';
COMMENT ON TABLE "YMES".tblemsequipmentstatelog IS
    'MES Oracle source: MESTEST.TBLEMSEQUIPMENTSTATELOG';

GRANT SELECT ON "YMES".tbleqpstatecontrol,
    "YMES".tblemsequipmentstate,
    "YMES".tblemsequipmentstatelog TO ymes_app;

COMMIT;

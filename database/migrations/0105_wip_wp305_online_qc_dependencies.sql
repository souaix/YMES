-- WP305 schema dependencies missing from the YMES clone.
-- Source-aligned structures only; does not copy MES business rows.
-- TBLWIPEQPQCLISTLOG/DETAIL, equipment status, and Cont Equipment are created
-- by prior YMES migrations; do not recreate them here.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP305 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WP305 migration';
    END IF;
    IF to_regclass('"YMES".tbleqpqclistbasis') IS NULL
       OR to_regclass('"YMES".tbleqpqclistdetail') IS NULL
       OR to_regclass('"YMES".tbleqpequipmentbasis') IS NULL THEN
        RAISE EXCEPTION 'WP305 QC/equipment basis references are missing; inspect live schema before migrating';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblwipeqpqctask', 'tbleqpqclistsetup', 'tblwiptemp_equipment'
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
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE c.relname IN ('pk_tblwipeqpqctask','pk_tbleqpqclistsetup')
          AND c.relkind='i'
    ) THEN
        RAISE EXCEPTION 'A WP305 source primary-key index name already exists; inspect before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipeqpqctask (
    qctaskno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    qctaskclass numeric(2,0) NOT NULL,
    insptype numeric(2,0) NOT NULL,
    shiftno varchar(20),
    lotno varchar(50),
    inspstarttime varchar(20),
    qcno varchar(100),
    memo varchar(255),
    status numeric(2,0) DEFAULT 0 NOT NULL,
    belongdate timestamp without time zone NOT NULL,
    planqctime timestamp without time zone,
    qcserialno varchar(120),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwipeqpqctask PRIMARY KEY (qctaskno)
);
COMMENT ON TABLE "YMES".tblwipeqpqctask IS 'MES Oracle source: MESTEST.TBLWIPEQPQCTASK; WP305 inspection task';
COMMENT ON COLUMN "YMES".tblwipeqpqctask.status IS '0=waiting, 1=completed, 2=recheck';

CREATE TABLE "YMES".tbleqpqclistsetup (
    equipmentno varchar(50) NOT NULL,
    qclistno varchar(100) NOT NULL,
    insptype numeric(22,0) NOT NULL,
    inspstarttime varchar(20) DEFAULT 'N/A' NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tbleqpqclistsetup PRIMARY KEY (equipmentno, qclistno, insptype, inspstarttime)
);
COMMENT ON TABLE "YMES".tbleqpqclistsetup IS 'MES Oracle source: MESTEST.TBLEQPQCLISTSETUP';

CREATE TABLE "YMES".tblwiptemp_equipment (
    lotno varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50),
    recipegroup varchar(50),
    recipeversion numeric(2,0),
    lotserial varchar(55),
    equipmentclass varchar(50),
    loadport numeric(2,0),
    loggroupserial varchar(50),
    inputqty numeric(12,4) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    planfinishtime timestamp without time zone,
    s_batchid varchar(150)
);
COMMENT ON TABLE "YMES".tblwiptemp_equipment IS 'MES Oracle source: MESTEST.TBLWIPTEMP_EQUIPMENT; source declares no key';

-- WP305 needs these source tables only for server-side candidate reads.
-- No DML is granted by this schema migration.
GRANT SELECT ON "YMES".tbleqpqclistbasis,
    "YMES".tbleqpqclistdetail,
    "YMES".tbleqpqclistsetup,
    "YMES".tblwipeqpqctask,
    "YMES".tblwiptemp_equipment TO ymes_app;

COMMIT;

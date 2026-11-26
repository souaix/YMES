-- RT410 daily equipment utilization uses exact source structures for WIP
-- equipment-to-lot links and reported lot operations. Empty tables only.
-- Source PostgreSQL CREATE references: WP305 and WP201; no business rows.
-- ymes_app receives SELECT only on new tables and the existing area relation.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing RT410 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing RT410 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing RT410 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY['tblwipcont_equipment','tblwiplotlog_report'] LOOP
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
        'i_tblwipcont_equipment_01','i_tblwipcont_equipment_02',
        'i_rt410_wiplogreport_loggroup_endtime','i_rt410_area_objectno_type'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            WHERE lower(c.relname)=object_name AND c.relkind='i'
        ) THEN
            RAISE EXCEPTION 'Index name % already exists; inspect before migrating', object_name;
        END IF;
    END LOOP;

    IF to_regclass('"YMES".tblsmdarearelation') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblsmdarearelation is missing; refusing RT410 migration';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipcont_equipment (
    lotserial varchar(50),
    equipmenttype varchar(50),
    equipmentno varchar(50),
    recipegroup varchar(50),
    recipeversion numeric(2,0),
    equipmentclass varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    loadport numeric(2,0),
    loggroupserial varchar(50),
    inputqty numeric(12,4) DEFAULT 0,
    outputqty numeric(12,4) DEFAULT 0,
    eventtime timestamp without time zone,
    fromlotno varchar(50),
    fromloggroupserial varchar(50),
    erpflag numeric(1,0),
    summaryflag numeric(1,0),
    workreportid varchar(30),
    planfinishtime timestamp without time zone,
    chamberflag numeric(1,0) DEFAULT 0,
    s_batchid varchar(150)
);

CREATE TABLE "YMES".tblwiplotlog_report (
    loggroupserial varchar(50),
    baselotno varchar(50),
    lotno varchar(50),
    opno varchar(20),
    status numeric(1,0),
    preopendtime timestamp without time zone,
    arrivetime timestamp without time zone,
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    inputqty numeric(12,4),
    inputunitno varchar(30),
    goodqty numeric(12,4),
    failqty numeric(12,4),
    goodunitno varchar(30),
    engno varchar(30) DEFAULT 'N/A' NOT NULL,
    engversion varchar(3) DEFAULT 'N/A' NOT NULL,
    recipeno varchar(300) DEFAULT 'N/A' NOT NULL,
    recipeversion varchar(15) DEFAULT 'N/A' NOT NULL,
    recipesql varchar(400) DEFAULT 'N/A' NOT NULL,
    nodeid varchar(50) DEFAULT 'N/A' NOT NULL,
    factoryno varchar(20) DEFAULT 'N/A' NOT NULL,
    moduleserial varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleno varchar(50) DEFAULT 'N/A' NOT NULL,
    moduleversion varchar(3) DEFAULT 'N/A' NOT NULL,
    productno varchar(50),
    productversion varchar(5),
    customerlotno varchar(50),
    deviceno varchar(50),
    areano varchar(20) DEFAULT 'N/A' NOT NULL,
    lossqty numeric(12,4),
    completeflag numeric(1,0) DEFAULT 0 NOT NULL,
    checkintime timestamp without time zone,
    checkouttime timestamp without time zone,
    opgroupno varchar(20) DEFAULT 'N/A',
    serialno varchar(50),
    mono varchar(50),
    pdlineno varchar(50) DEFAULT 'N/A',
    erpno varchar(50),
    defectqty numeric(12,4) DEFAULT 0,
    ecntype_basis varchar(10),
    ecnno_basis varchar(20),
    ecntype_process varchar(10),
    ecnno_process varchar(20),
    ecntype_op varchar(10),
    ecnno_op varchar(20),
    i_failbinqty numeric(12,4),
    eqpsetuplistno varchar(20),
    sap_upload varchar(1) DEFAULT 'N',
    sap_rmzhl varchar(8)
);

-- Preserve the verified WP305 source indexes. The additional report index
-- supports RT410's LogGroupSerial join and EndTime range predicate.
CREATE INDEX i_tblwipcont_equipment_01
    ON "YMES".tblwipcont_equipment (loggroupserial, equipmentno);
CREATE INDEX i_tblwipcont_equipment_02
    ON "YMES".tblwipcont_equipment (lotserial);
CREATE INDEX i_rt410_wiplogreport_loggroup_endtime
    ON "YMES".tblwiplotlog_report (loggroupserial, endtime);
CREATE INDEX i_rt410_area_objectno_type
    ON "YMES".tblsmdarearelation (objectno, objecttype);

COMMENT ON TABLE "YMES".tblwipcont_equipment IS
    'MES Oracle source: MESTEST.TBLWIPCONT_EQUIPMENT; source declares no primary key';
COMMENT ON TABLE "YMES".tblwiplotlog_report IS
    'MES Oracle source: MESTEST.TBLWIPLOTLOG_REPORT; source declares no primary key';

GRANT SELECT ON "YMES".tblsmdarearelation,
    "YMES".tblwipcont_equipment,
    "YMES".tblwiplotlog_report TO ymes_app;

COMMIT;

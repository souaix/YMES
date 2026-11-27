-- Source-aligned WP303 consumable tables; WP304 reads the log table.
-- Empty source tables only. Do not copy MES business rows.
BEGIN;

DO $guard$
DECLARE object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing WP303 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing WP303 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing WP303 migration';
    END IF;

    FOREACH object_name IN ARRAY ARRAY['tblwipeqpconsumestatus','tblwipeqpconsumelog'] LOOP
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
        'ix_wp304_consume_log_equipment_created',
        'ix_wp304_consume_log_consume_created',
        'ix_wp304_consume_log_created'
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

CREATE TABLE "YMES".tblwipeqpconsumestatus (
    equipmentno varchar(50) NOT NULL,
    consumeno varchar(25) NOT NULL,
    consumeseq varchar(30) NOT NULL,
    curqty numeric(8,0) NOT NULL,
    curperiod numeric(7,2) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    revisor varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblwipeqpconsumestatus PRIMARY KEY (equipmentno, consumeno)
);

CREATE TABLE "YMES".tblwipeqpconsumelog (
    equipmentno varchar(50) NOT NULL,
    consumeno varchar(25) NOT NULL,
    consumeseq varchar(30) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    accqty numeric(8,0),
    accperiod numeric(7,2),
    CONSTRAINT pk_tblwipeqpconsumelog PRIMARY KEY (equipmentno, consumeno, consumeseq)
);

CREATE INDEX ix_wp304_consume_log_equipment_created
    ON "YMES".tblwipeqpconsumelog
       (equipmentno, createdate DESC NULLS LAST, consumeno COLLATE "C", consumeseq COLLATE "C");
CREATE INDEX ix_wp304_consume_log_consume_created
    ON "YMES".tblwipeqpconsumelog
       (consumeno, createdate DESC NULLS LAST, equipmentno COLLATE "C", consumeseq COLLATE "C");
CREATE INDEX ix_wp304_consume_log_created
    ON "YMES".tblwipeqpconsumelog
       (createdate DESC NULLS LAST, equipmentno COLLATE "C", consumeno COLLATE "C", consumeseq COLLATE "C");

COMMENT ON TABLE "YMES".tblwipeqpconsumestatus IS
    'MES Oracle source: MESTEST.TBLWIPEQPCONSUMESTATUS; empty WP303 status source';
COMMENT ON TABLE "YMES".tblwipeqpconsumelog IS
    'MES Oracle source: MESTEST.TBLWIPEQPCONSUMELOG; empty WP304 history source';
COMMENT ON INDEX "YMES".ix_wp304_consume_log_equipment_created IS
    'YMES-only WP304 query support index; not an Oracle source index';
COMMENT ON INDEX "YMES".ix_wp304_consume_log_consume_created IS
    'YMES-only WP304 query support index; not an Oracle source index';
COMMENT ON INDEX "YMES".ix_wp304_consume_log_created IS
    'YMES-only WP304 query support index; not an Oracle source index';

-- WP304 is read-only. WP303 status read/write grants wait for its transaction API.
GRANT SELECT ON "YMES".tblwipeqpconsumelog TO ymes_app;

COMMIT;

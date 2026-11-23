-- ES408: read-only equipment/accessory history query.
-- Source schema is the checked MESTEST.TBLEMSEQPACCSTATELOG CREATE-only reference.
-- The live YMES schema audit found no object with this name in any schema.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing ES408 migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing ES408 migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing ES408 migration';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        WHERE c.relname = 'tblemseqpaccstatelog'
          AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
    ) THEN
        RAISE EXCEPTION 'tblemseqpaccstatelog already exists in a schema; compare it before proceeding';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblemseqpaccstatelog (
    accserialno varchar(20) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    accessoryno varchar(50) NOT NULL,
    accessoryversion varchar(3) NOT NULL,
    userno varchar(10),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    CONSTRAINT pk_tblemseqpaccstatelog PRIMARY KEY (accserialno)
);
COMMENT ON TABLE "YMES".tblemseqpaccstatelog IS 'Oracle source: MESTEST.TBLEMSEQPACCSTATELOG; schema checked 2026-11-16.';
CREATE INDEX ix_tblemseqpaccstatelog_start_time ON "YMES".tblemseqpaccstatelog (starttime, accserialno);
CREATE INDEX ix_tblemseqpaccstatelog_end_time ON "YMES".tblemseqpaccstatelog (endtime, accserialno);
GRANT SELECT ON "YMES".tblemseqpaccstatelog TO ymes_app;

COMMIT;

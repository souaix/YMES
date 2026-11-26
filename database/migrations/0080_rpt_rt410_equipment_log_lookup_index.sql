-- RT410 finds completed WIP groups by equipment and then joins each group to
-- the reported lot operation. This supporting access path does not change the
-- MES source structure or data.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tblwipcont_equipment') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwipcont_equipment is missing; apply 0079 first';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE lower(c.relname)='i_rt410_wipcont_equipment_equipment_loggroup'
          AND c.relkind='i'
    ) THEN
        RAISE EXCEPTION 'RT410 equipment/log-group index already exists; inspect before migrating';
    END IF;
END
$guard$;

CREATE INDEX i_rt410_wipcont_equipment_equipment_loggroup
    ON "YMES".tblwipcont_equipment (equipmentno, loggroupserial);
COMMENT ON INDEX "YMES".i_rt410_wipcont_equipment_equipment_loggroup IS
    'YMES API RT410 supporting index; source table definition remains unchanged';

COMMIT;

-- Keep the WP304 IssueState=2 equipment picker ordered and bounded by its first page.
-- YMES-only query support; this is not an Oracle source index.
BEGIN;

DO $guard$
BEGIN
    IF to_regclass('"YMES".tbleqpequipmentbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tbleqpequipmentbasis is missing; refusing WP304 lookup index migration';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        WHERE lower(c.relname)='ix_wp304_equipment_lookup' AND c.relkind='i'
    ) THEN
        RAISE EXCEPTION 'Index ix_wp304_equipment_lookup already exists; inspect before migrating';
    END IF;
END
$guard$;

CREATE INDEX ix_wp304_equipment_lookup
    ON "YMES".tbleqpequipmentbasis (equipmenttype COLLATE "C", equipmentno COLLATE "C")
    WHERE issuestate = 2 AND COALESCE(chamberflag, 0) <> 1;

COMMENT ON INDEX "YMES".ix_wp304_equipment_lookup IS
    'YMES-only WP304 IssueState=2 non-chamber lookup support index; not an Oracle source index';

COMMIT;

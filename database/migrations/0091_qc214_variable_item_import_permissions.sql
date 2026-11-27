BEGIN;

DO $$
DECLARE
    relation_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before QC214 permission migration 0091';
    END IF;

    FOREACH relation_name IN ARRAY ARRAY[
        'tblqcitemforvariables', 'tblprdqcitem', 'tblqcitemgroupbasis_pmi', 'tblopgqcitemdetail'
    ] LOOP
        IF to_regclass(format('%I.%I', 'YMES', relation_name)) IS NULL THEN
            RAISE EXCEPTION 'YMES.% is missing; QC214 requires verified existing relations and will not create them', relation_name;
        END IF;
    END LOOP;
END
$$;

-- QC214 replaces an unused variable item by deleting and reinserting it.
GRANT INSERT, DELETE ON "YMES".tblqcitemforvariables TO ymes_app;

-- Read-only guards prevent overwriting referenced products or colliding with
-- an existing PMI group identifier. No writes are granted on either table.
GRANT SELECT ON "YMES".tblprdqcitem, "YMES".tblopgqcitemdetail TO ymes_app;

COMMIT;

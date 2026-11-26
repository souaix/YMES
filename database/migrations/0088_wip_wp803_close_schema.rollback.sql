BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwip8dbasis_factory') IS NULL
       OR to_regclass('"YMES".tblwip8d_release') IS NULL
       OR to_regclass('"YMES".tblwip8d_releaselog') IS NULL
       OR to_regclass('"YMES".tblwip8d_teammember') IS NULL THEN
        RAISE EXCEPTION 'WP803 migration 0088 table set is incomplete; preserve schema and inspect manually';
    END IF;
    IF EXISTS (SELECT 1 FROM "YMES".tblwip8dbasis_factory LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwip8d_release LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwip8d_releaselog LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblwip8d_teammember LIMIT 1) THEN
        RAISE EXCEPTION 'WP803 source tables contain data; preserve records and do not roll back migration 0088';
    END IF;
END
$$;

DROP TABLE "YMES".tblwip8d_teammember;
DROP TABLE "YMES".tblwip8d_releaselog;
DROP TABLE "YMES".tblwip8d_release;
DROP TABLE "YMES".tblwip8dbasis_factory;

COMMIT;

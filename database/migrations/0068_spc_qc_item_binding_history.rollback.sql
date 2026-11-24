-- Rollback SC216 read access and the two lookup tables created by migration 0068.
-- Refuse to remove either group basis after it has received business data.
BEGIN;

DO $guard$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblqcitemattgrouporderbasis LIMIT 1)
       OR EXISTS (SELECT 1 FROM "YMES".tblqcitemgroupbasis_pmi LIMIT 1) THEN
        RAISE EXCEPTION 'SC216 group lookup data exists; refusing destructive rollback';
    END IF;
END
$guard$;

REVOKE SELECT ON TABLE
    "YMES".tblqcitembindlog,
    "YMES".tblqcitemattgrouporderbasis,
    "YMES".tblqcitemgroupbasis_pmi
FROM ymes_app;

DROP TABLE "YMES".tblqcitemattgrouporderbasis;
DROP TABLE "YMES".tblqcitemgroupbasis_pmi;

COMMIT;

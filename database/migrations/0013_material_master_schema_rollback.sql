BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "YMES".tblmtlmaterialtype) OR
       EXISTS (SELECT 1 FROM "YMES".tblmtlmaterialproperty) OR
       EXISTS (SELECT 1 FROM "YMES".tblmtltypeqcitem) OR
       EXISTS (SELECT 1 FROM "YMES".tblmtlqcitem) OR
       EXISTS (SELECT 1 FROM "YMES".tblmtlmaterialvendormap) OR
       EXISTS (SELECT 1 FROM "YMES".tblmtltypeacclimation)
    THEN
        RAISE EXCEPTION '0013 rollback refused: one or more new MTL tables contain data';
    END IF;
END $$;

REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblmtlmaterialtype,
    "YMES".tblmtlmaterialproperty,
    "YMES".tblmtltypeqcitem,
    "YMES".tblmtlqcitem,
    "YMES".tblmtlmaterialvendormap,
    "YMES".tblmtltypeacclimation,
    "YMES".tblmtlmaterialgroupdetail
FROM ymes_app;

DROP TABLE "YMES".tblmtltypeacclimation;
DROP TABLE "YMES".tblmtlmaterialvendormap;
DROP TABLE "YMES".tblmtlqcitem;
DROP TABLE "YMES".tblmtltypeqcitem;
DROP TABLE "YMES".tblmtlmaterialproperty;
DROP TABLE "YMES".tblmtlmaterialtype;

ALTER TABLE "YMES".tblmtlmaterialbasis
    ALTER COLUMN issuestate DROP DEFAULT,
    ALTER COLUMN putinplace DROP DEFAULT,
    ALTER COLUMN countway DROP DEFAULT,
    ALTER COLUMN checklotno DROP DEFAULT,
    ALTER COLUMN safeqty DROP DEFAULT,
    ALTER COLUMN keymaterials DROP DEFAULT,
    ALTER COLUMN datastamp DROP DEFAULT;

COMMIT;

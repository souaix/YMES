-- PREPARED / NOT EXECUTED. Existing Customer41, Root fresh audit0b04346d.
-- Native39 has ONLY IssueState/DataStamp DEFAULT0; CreateDate has no default.
-- Preserve existing SELECT, column ACLs, roles, schema, data and every other object.
ALTER TABLE "YMES".tblentcustomerbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;
GRANT INSERT, UPDATE, DELETE ON TABLE "YMES".tblentcustomerbasis TO ymes_app;

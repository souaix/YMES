BEGIN;
-- 2026-11-19 live pg_catalog/privilege audit: native tblprdmodulebasis exists
-- but ymes_app lacks SELECT. New ProductNo must not collide with ModuleNo.
-- Read only; no module CRUD, table creation, or business data changes.
GRANT SELECT ON "YMES".tblprdmodulebasis TO ymes_app;
COMMIT;

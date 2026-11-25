-- IV501's finished-goods inventory lookup uses the existing inventory basis.
-- Grant only the read access required to select InventoryType=1 warehouses.
BEGIN;

GRANT SELECT ON "YMES".tblinvinventorybasis TO ymes_app;

COMMIT;

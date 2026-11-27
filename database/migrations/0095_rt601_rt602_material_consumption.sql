BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = 'YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; stop before RT601/RT602 migration 0095';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before RT601/RT602 migration 0095';
    END IF;
    IF to_regclass('"YMES".tblwipcont_material') IS NOT NULL
       OR to_regclass('"YMES".tblwipcont_materiallot') IS NOT NULL THEN
        RAISE EXCEPTION 'An RT601/RT602 material-consumption table already exists; compare it with verified Oracle source before migration 0095';
    END IF;
END
$$;

-- Empty replicas of Oracle MESTEST tables required by RT601 and RT602.
-- TBLWIPCONT_MATERIAL has no PK or unique key in the source. Its two source indexes
-- are reported by metadata, but their columns were not delivered; do not guess them.
-- TBLWIPCONT_MATERIALLOT has no PK/FK/unique key; its two verified nonunique indexes are copied.
-- No business rows are imported.
CREATE TABLE "YMES".tblwipcont_material (
    lotserial varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialtype varchar(50) NOT NULL,
    useqty numeric(16,8) NOT NULL,
    unitno varchar(30) NOT NULL,
    description varchar(255),
    loggroupserial varchar(50) NOT NULL,
    undistributeqty numeric(16,8),
    eventtime timestamp without time zone
);
COMMENT ON TABLE "YMES".tblwipcont_material IS 'MES Oracle source: MESTEST.TBLWIPCONT_MATERIAL; no PK/unique key; index columns pending source metadata';

CREATE TABLE "YMES".tblwipcont_materiallot (
    lotserial varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    lotqty numeric(16,8) NOT NULL,
    equipmentno varchar(50),
    loggroupserial varchar(50) NOT NULL,
    undistributeqty numeric(16,8),
    createdate timestamp without time zone NOT NULL DEFAULT LOCALTIMESTAMP,
    ex_materiallot1 varchar(20),
    ex_materiallot2 varchar(20),
    ex_materiallot3 varchar(20),
    eventtime timestamp without time zone,
    sap_charg varchar(10),
    scrap_rate numeric(8,5),
    xx_remark varchar(50),
    xx_dmr_flag varchar(1),
    xx_change_type varchar(40),
    xx_equipment varchar(50)
);
CREATE INDEX i_tblwipcont_materiallot_01
    ON "YMES".tblwipcont_materiallot (loggroupserial, materialno);
CREATE INDEX i_tblwipcont_materiallot_02
    ON "YMES".tblwipcont_materiallot (lotserial);
COMMENT ON TABLE "YMES".tblwipcont_materiallot IS 'MES Oracle source: MESTEST.TBLWIPCONT_MATERIALLOT; no PK/FK/unique key';

REVOKE ALL ON "YMES".tblwipcont_material, "YMES".tblwipcont_materiallot FROM ymes_app;
GRANT SELECT ON "YMES".tblwipcont_material, "YMES".tblwipcont_materiallot TO ymes_app;

COMMIT;

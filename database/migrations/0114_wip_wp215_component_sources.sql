-- WP215 source-backed ComponentBasis and Container Component tables.
-- Both relations were proven absent from all user schemas in live YMES.
-- The component-basis reference is from WP215; the container-component
-- reference is from WP201. No business rows are copied and no app grants are added.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP215:0114', 0));

DO $guard$
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0114 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0114';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0114';
    END IF;
    IF to_regclass('"YMES".tblwipcomponentbasis') IS NOT NULL
       OR to_regclass('"YMES".tblwipcont_component') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP215 source table already exists in YMES; compare its structure before 0114';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname) IN ('tblwipcomponentbasis','tblwipcont_component')
          AND n.nspname NOT IN ('pg_catalog','information_schema','YMES')
    ) THEN
        RAISE EXCEPTION 'A WP215 source relation exists outside YMES; inspect before 0114';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblwipcomponentbasis (
    componentno varchar(50) NOT NULL,
    componentlotno varchar(50),
    slotno varchar(3),
    b_orgcomponentno varchar(50),
    b_materialno varchar(50),
    b_materiallotno varchar(50),
    mono varchar(50),
    s_aln_lotno varchar(50),
    s_cassetteno varchar(50),
    s_color varchar(100),
    s_cot_lotno varchar(50),
    s_cow_lotno varchar(50),
    s_cycleno varchar(100),
    s_cycleposition varchar(500),
    s_epi_lotno varchar(50),
    s_feedbackstate varchar(100),
    s_lasermark varchar(50),
    s_location numeric(4,0),
    s_mocvdeqpno varchar(100),
    s_orgcomponentlotno varchar(50),
    s_pss_lotno varchar(50),
    s_psv_lotno varchar(50),
    s_runno varchar(100),
    s_substrate_id varchar(100),
    s_wafercode varchar(50),
    s_waferfeedbackstatus varchar(2),
    s_wafersize varchar(100),
    p_bumpmergestatus numeric(1,0),
    p_waferid numeric(4,0),
    p_waferlot varchar(50),
    b_cassetteno varchar(50),
    g_lasermark varchar(50),
    CONSTRAINT pk_tblwipcomponentbasis PRIMARY KEY (componentno)
);
COMMENT ON TABLE "YMES".tblwipcomponentbasis IS 'MES Oracle source: MESTEST.TBLWIPCOMPONENTBASIS';
REVOKE ALL PRIVILEGES ON "YMES".tblwipcomponentbasis FROM PUBLIC, ymes_app;

CREATE TABLE "YMES".tblwipcont_component (
    lotserial varchar(55),
    componentno varchar(50),
    cpass numeric(1,0),
    in_goodqty numeric(12,4),
    in_scrapqty numeric(12,4),
    out_goodqty numeric(12,4),
    out_scrapqty numeric(12,4),
    unitno varchar(30),
    loggroupserial varchar(50),
    in_carrierno varchar(50),
    in_slotno varchar(3),
    out_carrierno varchar(50),
    out_slotno varchar(3),
    chamberno varchar(50),
    chamber_starttime timestamp without time zone,
    chamber_endtime timestamp without time zone,
    s_batchid varchar(150),
    s_cycleno varchar(100),
    s_endtime timestamp without time zone,
    s_feedbackstate numeric(1,0),
    s_mocvdeqpno varchar(100),
    s_position varchar(100),
    s_runno varchar(100),
    s_starttime timestamp without time zone,
    s_substrate_id varchar(100),
    s_formbatchseqno numeric(3,0),
    s_batchseqno numeric(3,0)
);
COMMENT ON TABLE "YMES".tblwipcont_component IS 'MES Oracle source: MESTEST.TBLWIPCONT_COMPONENT';
REVOKE ALL PRIVILEGES ON "YMES".tblwipcont_component FROM PUBLIC, ymes_app;

COMMIT;

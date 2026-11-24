-- Read-only SPC statistic dependencies for SC301/SC311/SC312.
-- Table definition and indexes follow the existing PostgreSQL reference for
-- MESTEST.TBLSPCVARFACTORLOG; this migration creates an empty replica table.
BEGIN;

DO $guard$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing SPC factor-log migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing SPC factor-log migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing SPC factor-log migration';
    END IF;
    IF to_regclass('"YMES".tblspcvarfactorlog') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblspcvarfactorlog already exists; compare it before migrating';
    END IF;
    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
        WHERE lower(c.relname)='tblspcvarfactorlog'
          AND c.relkind IN ('r','p','v','m','f')
    ) THEN
        RAISE EXCEPTION 'An SPC factor-log object already exists in another schema';
    END IF;
    IF to_regclass('"YMES".tblspcqcchartbasis') IS NULL
       OR to_regclass('"YMES".tblspcvardetaillog') IS NULL
       OR to_regclass('"YMES".tblqcitemfactor') IS NULL THEN
        RAISE EXCEPTION 'An SC311/SC312 existing dependency is missing; compare its reference before migrating';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblspcvarfactorlog (
    spcserial varchar(1500) NOT NULL,
    factorserial varchar(50),
    qcitemno varchar(25) NOT NULL,
    lotno varchar(50),
    lotserial varchar(55),
    retestlotno varchar(55),
    creator varchar(10),
    createdate timestamp without time zone,
    result numeric(1,0) DEFAULT 0,
    erfno varchar(20),
    excluded numeric(1,0) DEFAULT 0,
    groupno varchar(25),
    gspcserial varchar(50),
    shortrunserial varchar(50),
    loggroupserial varchar(50),
    memo varchar(300),
    memocolor numeric(2,0) DEFAULT 0,
    mo varchar(61) DEFAULT '*',
    product varchar(61) DEFAULT '*',
    equipment varchar(61) DEFAULT '*',
    br varchar(61) DEFAULT '*',
    customer varchar(61) DEFAULT '*',
    op varchar(61) DEFAULT '*',
    preopno varchar(61) DEFAULT '*',
    process varchar(61) DEFAULT '*',
    mtlvendor varchar(61) DEFAULT '*',
    eqptype varchar(61) DEFAULT '*',
    employee varchar(61) DEFAULT '*',
    material varchar(61) DEFAULT '*',
    line varchar(61) DEFAULT '*',
    version varchar(61) DEFAULT '*',
    mono varchar(61) DEFAULT '*',
    posion varchar(61) DEFAULT '*',
    thinkness varchar(61) DEFAULT '*',
    thickness varchar(61) DEFAULT '*',
    al_spec varchar(61) DEFAULT '*',
    "condition" varchar(61) DEFAULT '*',
    f_1_force varchar(61) DEFAULT '*',
    f_1_power varchar(61) DEFAULT '*',
    f_1st_time varchar(61) DEFAULT '*',
    f_2_force varchar(61) DEFAULT '*',
    f_2_power varchar(61) DEFAULT '*',
    f_2st_time varchar(61) DEFAULT '*',
    loop_height varchar(61) DEFAULT '*',
    wire_diameter varchar(61) DEFAULT '*',
    device varchar(61) DEFAULT '*',
    shift varchar(61) DEFAULT '*',
    bath_location varchar(61) DEFAULT '*',
    device_a varchar(61) DEFAULT '*',
    device_b varchar(61) DEFAULT '*',
    fm varchar(61) DEFAULT '*',
    measure_operator varchar(61) DEFAULT '*',
    measuring_machine varchar(61) DEFAULT '*',
    operator varchar(61) DEFAULT '*',
    position varchar(61) DEFAULT '*',
    substrate_material varchar(61) DEFAULT '*',
    state varchar(61) DEFAULT '*',
    CONSTRAINT pk_tblspcvarfactorlog PRIMARY KEY (spcserial)
);

CREATE INDEX ix_qc210_varfactorlog_lotno ON "YMES".tblspcvarfactorlog (lotno);
CREATE INDEX ix_qc210_varfactorlog_groupno ON "YMES".tblspcvarfactorlog (groupno);
CREATE INDEX ix_qc210_varfactorlog_factorserial ON "YMES".tblspcvarfactorlog (factorserial);
CREATE INDEX ix_qc210_varfactorlog_qcitemno ON "YMES".tblspcvarfactorlog (qcitemno);
CREATE INDEX ix_qc210_varfactorlog_lotserial ON "YMES".tblspcvarfactorlog (lotserial);

COMMENT ON TABLE "YMES".tblspcvarfactorlog IS 'MES Oracle source: MESTEST.TBLSPCVARFACTORLOG';
GRANT SELECT ON TABLE "YMES".tblspcvarfactorlog TO ymes_app;

COMMIT;

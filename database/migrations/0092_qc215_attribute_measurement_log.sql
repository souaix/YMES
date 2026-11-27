BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before QC215 migration 0092';
    END IF;
    IF to_regclass('"YMES".tblspcattbasislog') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblspcattbasislog already exists; compare its live structure before migration 0092';
    END IF;
END
$$;

-- Source DDL is preserved in database/reference/mestest_qc_item_attributes_source_ddl.sql.
CREATE TABLE "YMES".tblspcattbasislog (
    spcserial varchar(1500) NOT NULL,
    qcattspserial varchar(50),
    qcitemno varchar(25) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55),
    retestlotno varchar(55),
    sampleqty numeric(12,4),
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    defectqty numeric(12,4),
    result numeric(1,0) DEFAULT 0,
    erfno varchar(20),
    excluded numeric(1,0) DEFAULT 0,
    loggroupserial varchar(50),
    rank varchar(5),
    rankdetail varchar(10),
    mo varchar(61) DEFAULT '*',
    product varchar(50) DEFAULT '*',
    employee varchar(50) DEFAULT '*',
    equipment varchar(50) DEFAULT '*',
    customer varchar(50) DEFAULT '*',
    material varchar(50) DEFAULT '*',
    op varchar(50) DEFAULT '*',
    process varchar(50) DEFAULT '*',
    eqptype varchar(50) DEFAULT '*',
    mtlvendor varchar(50) DEFAULT '*',
    preopno varchar(50) DEFAULT '*',
    shift varchar(50) DEFAULT '*',
    version varchar(50) DEFAULT '*',
    thickness varchar(50) DEFAULT '*',
    CONSTRAINT pk_tblspcattbasislog PRIMARY KEY (spcserial)
);

CREATE INDEX i_tblspcattbasislog_01 ON "YMES".tblspcattbasislog (lotserial);
CREATE INDEX ix_tblspcattbasislog_qcitemno ON "YMES".tblspcattbasislog (qcitemno);
GRANT SELECT ON "YMES".tblspcattbasislog TO ymes_app;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblopattrib') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.tblopattrib already exists; inspect its live definition before applying migration 0086';
    END IF;
    IF to_regclass('"YMES".pk_tblopattrib') IS NOT NULL THEN
        RAISE EXCEPTION 'YMES.pk_tblopattrib already exists; inspect the conflicting object before applying migration 0086';
    END IF;
END
$$;

-- Source: MESTEST.TBLOPATTRIB Oracle DDL and the delivered PostgreSQL
-- CREATE-only reference. The report only needs to read StdYield attributes.
CREATE TABLE "YMES".tblopattrib (
    opno varchar(20) CONSTRAINT n_tblopattrib_01 NOT NULL,
    attribtype numeric(1,0) CONSTRAINT n_tblopattrib_02 NOT NULL,
    attribno varchar(20) CONSTRAINT n_tblopattrib_03 NOT NULL,
    attribsource numeric(1,0) CONSTRAINT n_tblopattrib_04 NOT NULL,
    attribphase numeric(2,0) CONSTRAINT n_tblopattrib_05 NOT NULL,
    attribsequence numeric(2,0) CONSTRAINT n_tblopattrib_06 NOT NULL,
    attribvalue varchar(255) DEFAULT ' ',
    attribscript varchar(3000) DEFAULT ' ',
    savetolotpropertyno varchar(20),
    qcitemno varchar(25),
    CONSTRAINT pk_tblopattrib PRIMARY KEY (opno, attribno)
);

GRANT SELECT ON TABLE "YMES".tblopattrib TO ymes_app;

COMMIT;

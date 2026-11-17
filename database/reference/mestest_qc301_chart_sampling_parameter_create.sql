-- PostgreSQL CREATE-only reference adapted from the Oracle DDL in
-- mestest_qc301_chart_sampling_parameter_source_ddl.sql.
-- Check YMES live schema and deployed migrations before using. No FK was
-- present in Oracle; the unique index is represented by the composite PK.

CREATE TABLE "YMES".tblspcqcchartsamplanparameter (
    factorserial varchar(50) NOT NULL,
    parameterno varchar(20) NOT NULL,
    parametervalue varchar(50),
    CONSTRAINT pk_spcqcchartsamplanparameter PRIMARY KEY (factorserial, parameterno)
);

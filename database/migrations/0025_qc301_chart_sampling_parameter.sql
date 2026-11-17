BEGIN;

-- QC301 chart-specific sampling parameter values. Oracle source has only the
-- (FactorSerial, ParameterNo) primary key and no foreign key/index.
CREATE TABLE "YMES".tblspcqcchartsamplanparameter (
    factorserial varchar(50) NOT NULL,
    parameterno varchar(20) NOT NULL,
    parametervalue varchar(50),
    CONSTRAINT pk_spcqcchartsamplanparameter PRIMARY KEY (factorserial, parameterno)
);

COMMENT ON TABLE "YMES".tblspcqcchartsamplanparameter IS
    'QC301 管制圖抽樣計畫參數；來源 MESTEST.TBLSPCQCCHARTSAMPLANPARAMETER';

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblspcqcchartsamplanparameter TO ymes_app;

COMMIT;

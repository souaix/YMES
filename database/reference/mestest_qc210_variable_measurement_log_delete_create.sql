-- PostgreSQL schema-only reference for QC210 variable QC measurement-log deletion.
-- Oracle source DDL: mestest_qc205_variable_dependencies_source_ddl.sql,
-- mestest_qc205_theil_custom_source_ddl.sql, and mestest_qc_item_attributes_source_ddl.sql.
-- TBLQCSAMPLEPLANCHANGE is already included in mestest_qc_item_attributes_create.sql.
-- Compare YMES live schema/migrations first; CREATE IF NOT EXISTS does not reconcile existing columns.
-- Use a forward migration only for missing structures. No foreign keys are added.

CREATE TABLE IF NOT EXISTS "YMES".tblspcvarstatisticlog (
    "spcserial" varchar(1500) NOT NULL,
    "factorserial" varchar(50),
    "qcitemno" varchar(25),
    "samplesize" numeric(5,0),
    "meanvalue" numeric(38,20),
    "maxvalue" numeric(38,20),
    "minvalue" numeric(38,20),
    "sdvalue" numeric(38,20),
    "sumvalue" numeric(38,20),
    "rangevalue" numeric(38,20),
    "createdate" timestamp without time zone,
    "lotno" varchar(50),
    "lotserial" varchar(50),
    "cp" varchar(30),
    "cpk" varchar(30),
    "loggroupserial" varchar(50),
    "ca" varchar(50),
    "squaresumvalue" varchar(30),
    CONSTRAINT pk_tblspcvarstatisticlog PRIMARY KEY (spcserial)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvarlimitcontent (
    "spcserial" varchar(1500) NOT NULL,
    "spcid" numeric(2,0) NOT NULL,
    "us" varchar(15),
    "ls" varchar(15),
    "ucl" varchar(15),
    "lcl" varchar(15),
    "cl" varchar(15),
    "target" varchar(10),
    "cpkgoal" varchar(10),
    "cs" varchar(15),
    CONSTRAINT pk_tblspcvarlimitcontent PRIMARY KEY (spcserial, spcid)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvarviolationcontent (
    "spcserial" varchar(1500) NOT NULL,
    "spcid" numeric(2,0) NOT NULL,
    "serialno" numeric(5,0) NOT NULL,
    "violationid" varchar(20) NOT NULL,
    CONSTRAINT pk_tblspcvarviolationcontent PRIMARY KEY (spcserial, spcid, serialno, violationid)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvardetaillog (
    "spcserial" varchar(1500) NOT NULL,
    "factorserial" varchar(50),
    "qcitemno" varchar(25) NOT NULL,
    "serialno" numeric(5,0) NOT NULL,
    "testvalue" numeric(38,20) NOT NULL,
    "createdate" timestamp without time zone NOT NULL,
    "lotserial" varchar(55),
    "memo" varchar(255),
    "componentno" varchar(50),
    "loggroupserial" varchar(50),
    "x" varchar(10),
    "y" varchar(10),
    "thickness" varchar(500),
    "posion" varchar(500),
    "shift" varchar(500),
    "line" varchar(500),
    "version" varchar(500),
    "material" varchar(500),
    "eqptype" varchar(500),
    "equipment" varchar(500),
    "process" varchar(500),
    "op" varchar(500),
    "customer" varchar(500),
    "employee" varchar(500),
    "product" varchar(500),
    "fm" varchar(500),
    "al_spec" varchar(500),
    CONSTRAINT pk_tblspcvardetaillog PRIMARY KEY (spcserial, serialno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvardetailonline (
    "spcserial" varchar(1500) NOT NULL,
    "factorserial" varchar(50),
    "qcitemno" varchar(25) NOT NULL,
    "serialno" numeric(5,0) NOT NULL,
    "testvalue" numeric(38,20) NOT NULL,
    "createdate" timestamp without time zone NOT NULL,
    "lotserial" varchar(55),
    "memo" varchar(255),
    "componentno" varchar(50),
    "loggroupserial" varchar(50),
    "x" varchar(10),
    "y" varchar(10),
    CONSTRAINT pk_tblspcvardetailonline PRIMARY KEY (spcserial, serialno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvarfactorlog (
    "spcserial" varchar(1500) NOT NULL,
    "factorserial" varchar(50),
    "qcitemno" varchar(25) NOT NULL,
    "lotno" varchar(50),
    "lotserial" varchar(55),
    "retestlotno" varchar(55),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "result" numeric(1,0) DEFAULT 0,
    "erfno" varchar(20),
    "excluded" numeric(1,0) DEFAULT 0,
    "groupno" varchar(25),
    "gspcserial" varchar(50),
    "shortrunserial" varchar(50),
    "loggroupserial" varchar(50),
    "memo" varchar(300),
    "memocolor" numeric(2,0) DEFAULT 0,
    "mo" varchar(61) DEFAULT '*',
    "product" varchar(61) DEFAULT '*',
    "equipment" varchar(61) DEFAULT '*',
    "br" varchar(61) DEFAULT '*',
    "customer" varchar(61) DEFAULT '*',
    "op" varchar(61) DEFAULT '*',
    "preopno" varchar(61) DEFAULT '*',
    "process" varchar(61) DEFAULT '*',
    "mtlvendor" varchar(61) DEFAULT '*',
    "eqptype" varchar(61) DEFAULT '*',
    "employee" varchar(61) DEFAULT '*',
    "material" varchar(61) DEFAULT '*',
    "line" varchar(61) DEFAULT '*',
    "version" varchar(61) DEFAULT '*',
    "mono" varchar(61) DEFAULT '*',
    "posion" varchar(61) DEFAULT '*',
    "thinkness" varchar(61) DEFAULT '*',
    "thickness" varchar(61) DEFAULT '*',
    "al_spec" varchar(61) DEFAULT '*',
    "condition" varchar(61) DEFAULT '*',
    "f_1_force" varchar(61) DEFAULT '*',
    "f_1_power" varchar(61) DEFAULT '*',
    "f_1st_time" varchar(61) DEFAULT '*',
    "f_2_force" varchar(61) DEFAULT '*',
    "f_2_power" varchar(61) DEFAULT '*',
    "f_2st_time" varchar(61) DEFAULT '*',
    "loop_height" varchar(61) DEFAULT '*',
    "wire_diameter" varchar(61) DEFAULT '*',
    "device" varchar(61) DEFAULT '*',
    "shift" varchar(61) DEFAULT '*',
    "bath_location" varchar(61) DEFAULT '*',
    "device_a" varchar(61) DEFAULT '*',
    "device_b" varchar(61) DEFAULT '*',
    "fm" varchar(61) DEFAULT '*',
    "measure_operator" varchar(61) DEFAULT '*',
    "measuring_machine" varchar(61) DEFAULT '*',
    "operator" varchar(61) DEFAULT '*',
    "position" varchar(61) DEFAULT '*',
    "substrate_material" varchar(61) DEFAULT '*',
    "state" varchar(61) DEFAULT '*',
    CONSTRAINT pk_tblspcvarfactorlog PRIMARY KEY (spcserial)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvarfactoronline (
    "spcserial" varchar(1500) NOT NULL,
    "factorserial" varchar(50),
    "qcitemno" varchar(25) NOT NULL,
    "lotno" varchar(50),
    "lotserial" varchar(55),
    "retestlotno" varchar(55),
    "creator" varchar(10),
    "createdate" timestamp without time zone,
    "result" numeric(1,0) DEFAULT 0,
    "erfno" varchar(20),
    "excluded" numeric(1,0) DEFAULT 0,
    "groupno" varchar(25),
    "gspcserial" varchar(50),
    "loggroupserial" varchar(50),
    "shortrunserial" varchar(50),
    "memo" varchar(300),
    "memocolor" numeric(2,0) DEFAULT 0,
    "mo" varchar(61) DEFAULT '*',
    "product" varchar(61) DEFAULT '*',
    "equipment" varchar(61) DEFAULT '*',
    "br" varchar(61) DEFAULT '*',
    "customer" varchar(61) DEFAULT '*',
    "op" varchar(61) DEFAULT '*',
    "preopno" varchar(61) DEFAULT '*',
    "process" varchar(61) DEFAULT '*',
    "mtlvendor" varchar(61) DEFAULT '*',
    "eqptype" varchar(61) DEFAULT '*',
    "employee" varchar(61) DEFAULT '*',
    "material" varchar(61) DEFAULT '*',
    "line" varchar(61) DEFAULT '*',
    "version" varchar(61) DEFAULT '*',
    "mono" varchar(61) DEFAULT '*',
    "posion" varchar(61) DEFAULT '*',
    "thinkness" varchar(61) DEFAULT '*',
    "thickness" varchar(61) DEFAULT '*',
    "al_spec" varchar(61) DEFAULT '*',
    "condition" varchar(61) DEFAULT '*',
    "f_1_force" varchar(61) DEFAULT '*',
    "f_1_power" varchar(61) DEFAULT '*',
    "f_1st_time" varchar(61) DEFAULT '*',
    "f_2_force" varchar(61) DEFAULT '*',
    "f_2_power" varchar(61) DEFAULT '*',
    "f_2st_time" varchar(61) DEFAULT '*',
    "loop_height" varchar(61) DEFAULT '*',
    "wire_diameter" varchar(61) DEFAULT '*',
    "device" varchar(61) DEFAULT '*',
    "shift" varchar(61) DEFAULT '*',
    "bath_location" varchar(61) DEFAULT '*',
    "device_a" varchar(61) DEFAULT '*',
    "device_b" varchar(61) DEFAULT '*',
    "fm" varchar(61) DEFAULT '*',
    "measure_operator" varchar(61) DEFAULT '*',
    "measuring_machine" varchar(61) DEFAULT '*',
    "operator" varchar(61) DEFAULT '*',
    "position" varchar(61) DEFAULT '*',
    "substrate_material" varchar(61) DEFAULT '*',
    "state" varchar(61) DEFAULT '*',
    CONSTRAINT pk_tblspcvarfactoronline PRIMARY KEY (spcserial)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspcvareditlog (
    "userid" varchar(10) NOT NULL,
    "recorddate" timestamp without time zone NOT NULL,
    "spcserial" varchar(1500) NOT NULL,
    "qcitemno" varchar(25) NOT NULL,
    "lotserial" varchar(55),
    "originalvalue" varchar(1000),
    "newvalue" varchar(1000),
    "description" varchar(255),
    "loggroupserial" varchar(50)
);

CREATE TABLE IF NOT EXISTS "YMES".tblspclogforsplanchange (
    "qcitemno" varchar(25) NOT NULL,
    "cursampleplan" varchar(25),
    "resultlist" varchar(100),
    "qcitemtype" numeric(1,0) DEFAULT 0,
    "mo" varchar(61) DEFAULT '*',
    "product" varchar(61) DEFAULT '*',
    "equipment" varchar(61) DEFAULT '*',
    "br" varchar(61) DEFAULT '*',
    "customer" varchar(61) DEFAULT '*',
    "op" varchar(61) DEFAULT '*',
    "preopno" varchar(61) DEFAULT '*',
    "process" varchar(61) DEFAULT '*',
    "employee" varchar(61) DEFAULT '*',
    "material" varchar(61) DEFAULT '*',
    "eqptype" varchar(61) DEFAULT '*',
    "mtlvendor" varchar(61) DEFAULT '*',
    "shift" varchar(61) DEFAULT '*',
    "line" varchar(61) DEFAULT '*',
    "version" varchar(61) DEFAULT '*',
    "mono" varchar(61) DEFAULT '*',
    "posion" varchar(61) DEFAULT '*',
    "thinkness" varchar(61) DEFAULT '*',
    "thickness" varchar(61) DEFAULT '*',
    "al_spec" varchar(61) DEFAULT '*',
    "condition" varchar(61) DEFAULT '*',
    "f_1_force" varchar(61) DEFAULT '*',
    "f_1_power" varchar(61) DEFAULT '*',
    "f_1st_time" varchar(61) DEFAULT '*',
    "f_2_force" varchar(61) DEFAULT '*',
    "f_2_power" varchar(61) DEFAULT '*',
    "f_2st_time" varchar(61) DEFAULT '*',
    "loop_height" varchar(61) DEFAULT '*',
    "wire_diameter" varchar(61) DEFAULT '*',
    "device" varchar(61) DEFAULT '*',
    "bath_location" varchar(61) DEFAULT '*',
    "device_a" varchar(61) DEFAULT '*',
    "device_b" varchar(61) DEFAULT '*',
    "fm" varchar(61) DEFAULT '*',
    "measure_operator" varchar(61) DEFAULT '*',
    "measuring_machine" varchar(61) DEFAULT '*',
    "operator" varchar(61) DEFAULT '*',
    "position" varchar(61) DEFAULT '*',
    "substrate_material" varchar(61) DEFAULT '*',
    "state" varchar(61) DEFAULT '*'
);

-- Non-PK indexes from the verified Oracle source DDL, renamed for YMES.
CREATE INDEX IF NOT EXISTS ix_qc210_varfactorlog_lotno ON "YMES".tblspcvarfactorlog (lotno);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactorlog_groupno ON "YMES".tblspcvarfactorlog (groupno);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactorlog_factorserial ON "YMES".tblspcvarfactorlog (factorserial);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactorlog_qcitemno ON "YMES".tblspcvarfactorlog (qcitemno);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactorlog_lotserial ON "YMES".tblspcvarfactorlog (lotserial);
CREATE INDEX IF NOT EXISTS ix_qc210_varstatisticlog_lotserial ON "YMES".tblspcvarstatisticlog (lotserial);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactoronline_factorserial ON "YMES".tblspcvarfactoronline (factorserial);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactoronline_qcitemno ON "YMES".tblspcvarfactoronline (qcitemno);
CREATE INDEX IF NOT EXISTS ix_qc210_varfactoronline_lotserial ON "YMES".tblspcvarfactoronline (lotserial);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetaillog_factorserial ON "YMES".tblspcvardetaillog (factorserial);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetaillog_lotserial ON "YMES".tblspcvardetaillog (lotserial);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetaillog_qcitemno ON "YMES".tblspcvardetaillog (qcitemno);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetailonline_lotserial ON "YMES".tblspcvardetailonline (lotserial);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetailonline_factorserial ON "YMES".tblspcvardetailonline (factorserial);
CREATE INDEX IF NOT EXISTS ix_qc210_vardetailonline_qcitemno ON "YMES".tblspcvardetailonline (qcitemno);
CREATE INDEX IF NOT EXISTS ix_qc210_splanchangelog_qcitemno ON "YMES".tblspclogforsplanchange (qcitemno);

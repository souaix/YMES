BEGIN;

DO $$
BEGIN
    IF to_regclass('"YMES".tblwip8dbasis_factory') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_release') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_releaselog') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_teammember') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP803 source table already exists; compare it with the verified Oracle DDL before migration 0088';
    END IF;
END
$$;

-- These are empty replicas of the verified MESTEST Oracle source tables.
-- Source data is not copied into YMES.
CREATE TABLE "YMES".tblwip8dbasis_factory (
    qrsno varchar(30) NOT NULL,
    qrsclass numeric(1,0) NOT NULL,
    qrssummary varchar(500) NOT NULL,
    lotno varchar(50),
    lotqty numeric(12,4),
    sampleqty numeric(6,0),
    errorqty numeric(12,4),
    factoryno varchar(20) NOT NULL,
    areano varchar(20) NOT NULL,
    mono varchar(50),
    customerno varchar(50),
    productno varchar(50),
    productversion varchar(5),
    rono varchar(25),
    opno varchar(20),
    equipmentno varchar(50),
    erfno varchar(25),
    holddescription varchar(4000),
    description varchar(255),
    projectdepartment varchar(20),
    projectmanager varchar(10),
    projectmanagername varchar(50),
    curstage varchar(10) DEFAULT 'N/A' NOT NULL,
    planclosedate timestamp without time zone,
    lastupdatedate timestamp without time zone,
    summarydescription varchar(500),
    disphour numeric(12,1),
    dispcost numeric(12,1),
    summarydepartment varchar(20),
    summaryuserno varchar(10),
    summaryusername varchar(50),
    closetype numeric(2,0),
    closedescription varchar(500),
    closedepartment varchar(20),
    closeuserno varchar(10),
    closeusername varchar(50),
    closedate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblwip8dbasis_factory PRIMARY KEY (qrsno)
);
COMMENT ON TABLE "YMES".tblwip8dbasis_factory IS 'MES Oracle source: MESTEST.TBLWIP8DBASIS_FACTORY; WP508 linked 8D lookup';

CREATE TABLE "YMES".tblwip8d_release (
    qrsno varchar(30) NOT NULL,
    releasememo varchar(500),
    releasedepartment varchar(20),
    releaser varchar(10),
    releasername varchar(50),
    releasedate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_release PRIMARY KEY (qrsno)
);
COMMENT ON TABLE "YMES".tblwip8d_release IS 'MES Oracle source: MESTEST.TBLWIP8D_RELEASE; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_releaselog (
    qrsno varchar(30) NOT NULL,
    releasetype numeric(2,0) NOT NULL,
    releasememo varchar(500) NOT NULL,
    releasedepartment varchar(20) NOT NULL,
    releaser varchar(10) NOT NULL,
    releasername varchar(50) NOT NULL,
    releasedate timestamp without time zone NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_releaselog PRIMARY KEY (qrsno)
);
COMMENT ON TABLE "YMES".tblwip8d_releaselog IS 'MES Oracle source: MESTEST.TBLWIP8D_RELEASELOG; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_teammember (
    qrsno varchar(30) NOT NULL,
    membertype numeric(1,0) NOT NULL,
    departmentno varchar(20),
    customerno varchar(50),
    vendorno varchar(50),
    memberno varchar(10) NOT NULL,
    membername varchar(50) NOT NULL,
    telno varchar(25),
    email varchar(255),
    taskdesc varchar(100) NOT NULL,
    description varchar(255),
    pmflag numeric(1,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone
);
CREATE UNIQUE INDEX pk_tblwip8d_teammember ON "YMES".tblwip8d_teammember (qrsno, membertype, memberno);
COMMENT ON TABLE "YMES".tblwip8d_teammember IS 'MES Oracle source: MESTEST.TBLWIP8D_TEAMMEMBER; WP802 8D Maintain';

GRANT SELECT ON "YMES".tblwip8dbasis_factory,
                "YMES".tblwip8d_release,
                "YMES".tblwip8d_releaselog,
                "YMES".tblwip8d_teammember TO ymes_app;
GRANT UPDATE (releasememo, releasedepartment, releaser, releasername, releasedate, description)
    ON "YMES".tblwip8d_release TO ymes_app;

COMMIT;

BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before WP802 migration 0093';
    END IF;
    IF to_regclass('"YMES".tblwip8d_action') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_actiondetail') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_attachfile') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_disposition') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_prevention') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_preventiontrack') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_qa') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_question') IS NOT NULL
       OR to_regclass('"YMES".tblwip8d_rootcause') IS NOT NULL THEN
        RAISE EXCEPTION 'A WP802 child table already exists; compare it with the verified Oracle DDL before migration 0093';
    END IF;
END
$$;

-- Empty PostgreSQL replicas of the verified Oracle MESTEST WP802 child tables.
-- No source business rows, attachment bodies, triggers, foreign keys, or extra indexes are copied.
CREATE TABLE "YMES".tblwip8d_action (
    qrsno varchar(30) NOT NULL,
    mainsn numeric(2,0) NOT NULL,
    planaction varchar(500) NOT NULL,
    dutydepartment varchar(20) NOT NULL,
    dutymember varchar(10),
    dutymembername varchar(50) NOT NULL,
    planfinishdate timestamp without time zone NOT NULL,
    description varchar(255),
    actualfinishdate timestamp without time zone,
    actionresult varchar(2),
    qauserno varchar(10),
    qausername varchar(50),
    qamemo varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_action PRIMARY KEY (qrsno, mainsn)
);
COMMENT ON TABLE "YMES".tblwip8d_action IS 'MES Oracle source: MESTEST.TBLWIP8D_ACTION; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_actiondetail (
    qrsno varchar(30) NOT NULL,
    mainsn numeric(2,0) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    subaction varchar(500) NOT NULL,
    dutydepartment varchar(20) NOT NULL,
    dutymember varchar(10),
    dutymembername varchar(50) NOT NULL,
    planfinishdate timestamp without time zone NOT NULL,
    description varchar(255),
    actualfinishdate timestamp without time zone,
    actionresult varchar(2),
    qauserno varchar(10),
    qausername varchar(50),
    qamemo varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_actiondetail PRIMARY KEY (qrsno, mainsn, sequenceno)
);
COMMENT ON TABLE "YMES".tblwip8d_actiondetail IS 'MES Oracle source: MESTEST.TBLWIP8D_ACTIONDETAIL; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_attachfile (
    qrsno varchar(30) NOT NULL,
    stageno varchar(10) NOT NULL,
    attachfilename varchar(60) NOT NULL,
    attachbody bytea,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_attachfile PRIMARY KEY (qrsno, stageno, attachfilename)
);
COMMENT ON TABLE "YMES".tblwip8d_attachfile IS 'MES Oracle source: MESTEST.TBLWIP8D_ATTACHFILE; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_disposition (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    reasonno varchar(55) NOT NULL,
    disposition varchar(500) NOT NULL,
    dutydepartment varchar(20) NOT NULL,
    dutymember varchar(10) NOT NULL,
    dutymembername varchar(50) NOT NULL,
    planfinishdate timestamp without time zone NOT NULL,
    description varchar(255),
    actualfinishdate timestamp without time zone,
    qadescription varchar(255),
    qaresult varchar(2),
    qamember varchar(10),
    qamembername varchar(50),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_disposition PRIMARY KEY (qrsno, sequenceno, reasonno)
);
COMMENT ON TABLE "YMES".tblwip8d_disposition IS 'MES Oracle source: MESTEST.TBLWIP8D_DISPOSITION; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_prevention (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    preventionitem varchar(55) NOT NULL,
    description varchar(255),
    maindepartment varchar(20) NOT NULL,
    director varchar(10) NOT NULL,
    directorname varchar(50) NOT NULL,
    planfinishdate timestamp without time zone NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_prevention PRIMARY KEY (qrsno, sequenceno)
);
COMMENT ON TABLE "YMES".tblwip8d_prevention IS 'MES Oracle source: MESTEST.TBLWIP8D_PREVENTION; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_preventiontrack (
    qrsno varchar(30) NOT NULL,
    mainsn numeric(2,0) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    finishdate timestamp without time zone NOT NULL,
    trackdepartment varchar(20) NOT NULL,
    tracker varchar(10) NOT NULL,
    trackername varchar(50) NOT NULL,
    tracedescription varchar(255) NOT NULL,
    traceresult varchar(2) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_preventiontrack PRIMARY KEY (qrsno, mainsn, sequenceno)
);
COMMENT ON TABLE "YMES".tblwip8d_preventiontrack IS 'MES Oracle source: MESTEST.TBLWIP8D_PREVENTIONTRACK; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_qa (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    reasonno varchar(55) NOT NULL,
    dispositionsn numeric(2,0) NOT NULL,
    qadescription varchar(255) NOT NULL,
    qaresult varchar(2) NOT NULL,
    qadepartment varchar(25) NOT NULL,
    verifier varchar(10) NOT NULL,
    verifiername varchar(50) NOT NULL,
    verifydate timestamp without time zone NOT NULL,
    lotno varchar(50),
    ipqcformno varchar(30),
    disphours numeric(12,4),
    consumecost numeric(12,4),
    memo varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_qa PRIMARY KEY (qrsno, reasonno, dispositionsn)
);
COMMENT ON TABLE "YMES".tblwip8d_qa IS 'MES Oracle source: MESTEST.TBLWIP8D_QA; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_question (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    questiondesc varchar(255) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_question PRIMARY KEY (qrsno, sequenceno)
);
COMMENT ON TABLE "YMES".tblwip8d_question IS 'MES Oracle source: MESTEST.TBLWIP8D_QUESTION; WP802 8D Maintain';

CREATE TABLE "YMES".tblwip8d_rootcause (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    reasonno varchar(55) NOT NULL,
    frequency numeric(3,0),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_rootcause PRIMARY KEY (qrsno, reasonno)
);
COMMENT ON TABLE "YMES".tblwip8d_rootcause IS 'MES Oracle source: MESTEST.TBLWIP8D_ROOTCAUSE; WP802 8D Maintain';

-- Metadata-only access for the attachment list. Binary bodies remain inaccessible to ymes_app.
GRANT SELECT ON "YMES".tblwip8d_action,
                "YMES".tblwip8d_actiondetail,
                "YMES".tblwip8d_disposition,
                "YMES".tblwip8d_prevention,
                "YMES".tblwip8d_preventiontrack,
                "YMES".tblwip8d_qa,
                "YMES".tblwip8d_question,
                "YMES".tblwip8d_rootcause TO ymes_app;
GRANT SELECT (qrsno, stageno, attachfilename, creator, createdate)
    ON "YMES".tblwip8d_attachfile TO ymes_app;

COMMIT;

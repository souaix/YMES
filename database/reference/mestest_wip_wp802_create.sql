-- Oracle MESTEST WP802 8D child table structural reference. CREATE only; compare live YMES schema/history before applying.
-- Oracle DATE is mapped to timestamp without time zone; BLOB to bytea. No Oracle business rows were queried.

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_action (
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
COMMENT ON TABLE ymes.tblwip8d_action IS 'MES Oracle source: MESTEST.TBLWIP8D_ACTION; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_actiondetail (
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
COMMENT ON TABLE ymes.tblwip8d_actiondetail IS 'MES Oracle source: MESTEST.TBLWIP8D_ACTIONDETAIL; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_attachfile (
    qrsno varchar(30) NOT NULL,
    stageno varchar(10) NOT NULL,
    attachfilename varchar(60) NOT NULL,
    attachbody bytea,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_attachfile PRIMARY KEY (qrsno, stageno, attachfilename)
);
COMMENT ON TABLE ymes.tblwip8d_attachfile IS 'MES Oracle source: MESTEST.TBLWIP8D_ATTACHFILE; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_disposition (
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
COMMENT ON TABLE ymes.tblwip8d_disposition IS 'MES Oracle source: MESTEST.TBLWIP8D_DISPOSITION; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_prevention (
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
COMMENT ON TABLE ymes.tblwip8d_prevention IS 'MES Oracle source: MESTEST.TBLWIP8D_PREVENTION; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_preventiontrack (
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
COMMENT ON TABLE ymes.tblwip8d_preventiontrack IS 'MES Oracle source: MESTEST.TBLWIP8D_PREVENTIONTRACK; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_qa (
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
COMMENT ON TABLE ymes.tblwip8d_qa IS 'MES Oracle source: MESTEST.TBLWIP8D_QA; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_question (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    questiondesc varchar(255) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_question PRIMARY KEY (qrsno, sequenceno)
);
COMMENT ON TABLE ymes.tblwip8d_question IS 'MES Oracle source: MESTEST.TBLWIP8D_QUESTION; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_release (
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
COMMENT ON TABLE ymes.tblwip8d_release IS 'MES Oracle source: MESTEST.TBLWIP8D_RELEASE; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_releaselog (
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
COMMENT ON TABLE ymes.tblwip8d_releaselog IS 'MES Oracle source: MESTEST.TBLWIP8D_RELEASELOG; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_rootcause (
    qrsno varchar(30) NOT NULL,
    sequenceno numeric(2,0) NOT NULL,
    reasonno varchar(55) NOT NULL,
    frequency numeric(3,0),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwip8d_rootcause PRIMARY KEY (qrsno, reasonno)
);
COMMENT ON TABLE ymes.tblwip8d_rootcause IS 'MES Oracle source: MESTEST.TBLWIP8D_ROOTCAUSE; WP802 8D Maintain';

CREATE TABLE IF NOT EXISTS ymes.tblwip8d_teammember (
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
CREATE UNIQUE INDEX IF NOT EXISTS pk_tblwip8d_teammember ON ymes.tblwip8d_teammember (qrsno, membertype, memberno);
COMMENT ON TABLE ymes.tblwip8d_teammember IS 'MES Oracle source: MESTEST.TBLWIP8D_TEAMMEMBER; WP802 8D Maintain';

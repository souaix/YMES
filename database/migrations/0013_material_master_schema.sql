BEGIN;

-- YMES PostgreSQL was checked over SSH against the MES-derived reference SQL.
-- Preserve the existing material basis table (including its seven YMES fields)
-- and add only the six missing MES source tables.
CREATE TABLE IF NOT EXISTS "YMES".tblmtlmaterialtype (
    materialtype varchar(50) NOT NULL,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    materialtypename varchar(50),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialtype PRIMARY KEY (materialtype)
);

CREATE TABLE IF NOT EXISTS "YMES".tblmtlmaterialproperty (
    material varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    defaultvalue varchar(255),
    propertysequence numeric(2,0),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialproperty PRIMARY KEY (material, propertyno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblmtltypeqcitem (
    materialtype varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtltypeqcitem PRIMARY KEY (materialtype, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblmtlqcitem (
    mtlno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlqcitem PRIMARY KEY (mtlno, qcitemno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblmtlmaterialvendormap (
    materialno varchar(50) NOT NULL,
    vendorno varchar(20) NOT NULL,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtlmaterialvendormap PRIMARY KEY (materialno, vendorno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblmtltypeacclimation (
    materialtype varchar(50) NOT NULL,
    accmin numeric(5,2) NOT NULL,
    accmax numeric(5,2) NOT NULL,
    reforzentimes numeric(3,0) NOT NULL,
    refrozenflag numeric(1,0) DEFAULT 0 NOT NULL,
    frozenmin numeric(5,2),
    reacclimateflag numeric(1,0) DEFAULT 0 NOT NULL,
    reaccmin numeric(5,2),
    reaccmax numeric(5,2),
    materialno varchar(50) DEFAULT '*' NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblmtltypeacclimation PRIMARY KEY (materialtype, materialno)
);

-- Restore the seven defaults from current MES MESTEST CREATE SQL.  The columns
-- were present in the old YMES bootstrap but had no defaults on this server.
ALTER TABLE "YMES".tblmtlmaterialbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN putinplace SET DEFAULT 0,
    ALTER COLUMN countway SET DEFAULT 0,
    ALTER COLUMN checklotno SET DEFAULT 0,
    ALTER COLUMN safeqty SET DEFAULT 0,
    ALTER COLUMN keymaterials SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblmtlmaterialtype,
    "YMES".tblmtlmaterialproperty,
    "YMES".tblmtltypeqcitem,
    "YMES".tblmtlqcitem,
    "YMES".tblmtlmaterialvendormap,
    "YMES".tblmtltypeacclimation,
    "YMES".tblmtlmaterialgroupdetail
TO ymes_app;

COMMIT;

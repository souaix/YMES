BEGIN;

-- The SD106 tables already exist in YMES and match the verified Oracle column
-- definitions and primary keys. Restore the Oracle DEFAULT without rebuilding
-- tables or touching existing rows.
ALTER TABLE "YMES".tblsmdcomputerareabasis
    ALTER COLUMN datastamp SET DEFAULT 0;

-- These Oracle source tables were verified but were not present in YMES.
-- Create empty structures only; do not import MESTEST business records.
CREATE TABLE IF NOT EXISTS "YMES".tblsmdareainventory (
    areano varchar(50) NOT NULL,
    inventoryno varchar(50) NOT NULL,
    baseareano varchar(50) NOT NULL,
    defaultinv numeric(1,0) DEFAULT 0 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblsmdareainventory PRIMARY KEY (areano, inventoryno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsmdareamap (
    baseareano varchar(50) NOT NULL,
    parentareano varchar(50) NOT NULL,
    childareano varchar(50) NOT NULL,
    parentareatype numeric(11,0) NOT NULL,
    childareatype numeric(11,0) NOT NULL,
    toppdlineareano varchar(50),
    CONSTRAINT pk_tblsmdareamap PRIMARY KEY (baseareano, parentareano, childareano)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsmdarearelation (
    baseareano varchar(50) NOT NULL,
    containareano varchar(50) NOT NULL,
    objectno varchar(50) NOT NULL,
    objecttype numeric(1,0) NOT NULL,
    objecttext varchar(50) NOT NULL,
    objectx numeric(11,0) NOT NULL,
    objecty numeric(11,0) NOT NULL,
    objectlength numeric(11,0) NOT NULL,
    objectwidth numeric(11,0) NOT NULL,
    objectcolor numeric(11,0) NOT NULL,
    objectsequence numeric(11,0),
    objectareatype numeric(11,0),
    bordercolor numeric(11,0),
    borderstyle numeric(1,0),
    textcolor numeric(11,0),
    CONSTRAINT pk_tblsmdarearelation PRIMARY KEY (baseareano, objectno)
);

CREATE INDEX IF NOT EXISTS i_tblsmdarearelation_01
    ON "YMES".tblsmdarearelation (containareano, objecttype);

CREATE TABLE IF NOT EXISTS "YMES".tblsmdsmtareaeqpmap (
    baseareano varchar(50) NOT NULL,
    areano varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    eqpsequence numeric(11,0),
    equipmentgroup varchar(50),
    CONSTRAINT pk_tblsmdsmtareaeqpmap PRIMARY KEY (baseareano, areano, equipmentno)
);

-- Restore missing source defaults on existing SMD tables. These changes do not
-- rewrite current rows and retain nullable DataStamp columns.
ALTER TABLE "YMES".tblsmdareabasis
    ALTER COLUMN areatype SET DEFAULT 0,
    ALTER COLUMN areaclass SET DEFAULT 0,
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN areacompose SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

ALTER TABLE "YMES".tblsmdfactorybasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

ALTER TABLE "YMES".tblinvlocatorbasis
    ALTER COLUMN issuestate SET DEFAULT 0,
    ALTER COLUMN capacity SET DEFAULT 0,
    ALTER COLUMN datastamp SET DEFAULT 0;

-- Grant only what the SD106 API needs: CRUD for its two tables and SELECT for
-- the approved Factory/Area lookup sources.
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    "YMES".tblsmdcomputerareabasis,
    "YMES".tblsmdcomputerareadetail
TO ymes_app;

GRANT SELECT ON TABLE
    "YMES".tblsmdfactorybasis,
    "YMES".tblsmdareabasis
TO ymes_app;

COMMIT;

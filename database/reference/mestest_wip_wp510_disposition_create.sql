-- CREATE-only PostgreSQL reference for Oracle MESTEST.TBLWIPWAITLOTDISPOSITION.
-- Compare YMES live schema and migration history first. This is not a migration and does not copy data.
-- Oracle source has no primary key, unique constraint, foreign key, default, or trigger.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwipwaitlotdisposition (
    waitno varchar(20) NOT NULL,
    lotserial varchar(55) NOT NULL,
    lotdisptype numeric(2,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    description varchar(255),
    nodeid varchar(50),
    inventoryno varchar(50),
    returntype numeric(1,0),
    returnnodeid varchar(50),
    moduleno varchar(50),
    moduleversion varchar(3),
    startnodeid varchar(50)
);

CREATE INDEX IF NOT EXISTS i_tblwipwaitlotdisposition_01
    ON ymes.tblwipwaitlotdisposition (waitno);

-- PostgreSQL CREATE-only reference from Oracle MESTEST WP109 tables.
-- Check YMES live schema/history before writing forward migrations; do not run as-is on an existing schema.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwipswrbasis (
    swrno varchar(20) NOT NULL,
    swrtype numeric(1,0) NOT NULL,
    status numeric(2,0) NOT NULL,
    customerno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    deviceno varchar(50),
    starttime timestamp without time zone,
    endtime timestamp without time zone,
    customerlotno varchar(50),
    mono varchar(50),
    attachfile bytea,
    swrdesc varchar(400) NOT NULL,
    creator varchar(50),
    createdate timestamp without time zone,
    attachfilepath varchar(200),
    CONSTRAINT pk_tblwipswrbasis PRIMARY KEY (swrno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipswrdetail (
    swrno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    CONSTRAINT pk_tblwipswrdetail PRIMARY KEY (swrno, lotno)
);

CREATE TABLE IF NOT EXISTS ymes.tblsysserialno (
    functionname varchar(20) NOT NULL,
    serialno numeric(9,0) NOT NULL,
    CONSTRAINT pk_tblsysserialno PRIMARY KEY (functionname)
);

COMMENT ON TABLE ymes.tblwipswrbasis IS 'Oracle MESTEST.TBLWIPSWRBASIS; WP109 special work request basis';
COMMENT ON TABLE ymes.tblwipswrdetail IS 'Oracle MESTEST.TBLWIPSWRDETAIL; one row per SWRNo/LotNo';
COMMENT ON TABLE ymes.tblsysserialno IS 'Oracle MESTEST.TBLSYSSERIALNO; shared serial counter; do not duplicate if already migrated';

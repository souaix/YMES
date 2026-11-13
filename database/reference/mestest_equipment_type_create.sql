-- Source: Oracle MESTEST DBMS_METADATA DDL in the company MES source workspace.
-- Target: PostgreSQL, schema ymes. Empty schema objects only; no MES business data.
-- Oracle DATE maps to timestamp without time zone; NUMBER maps to numeric.
-- Oracle RAW/BLOB maps to bytea. No foreign keys are added because the source DDL has none.
-- Check existing YMES tables and compare columns before applying: IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tbleqpequipmenttype (
    equipmenttype varchar(50) NOT NULL,
    capacity numeric(8,0) DEFAULT -1,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    picturename varchar(50),
    engineergroupno varchar(20),
    equipmentclass varchar(50),
    equipmentpicture bytea,
    stdunitemptime numeric(6,2) DEFAULT 0 NOT NULL,
    stduniteqptime numeric(6,2) DEFAULT 0 NOT NULL,
    counteqpunitqty numeric(6,0) DEFAULT 1 NOT NULL,
    equipmentclassno numeric(3,0),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tbleqpequipmenttype PRIMARY KEY (equipmenttype)
);
COMMENT ON TABLE ymes.tbleqpequipmenttype IS 'MES Oracle source: MESTEST.TBLEQPEQUIPMENTTYPE';

CREATE TABLE IF NOT EXISTS ymes.tbleqpequipmenttypeacctype (
    equipmenttype varchar(50) NOT NULL,
    accessorytype varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpequipmenttypeacctype PRIMARY KEY (equipmenttype, accessorytype)
);
COMMENT ON TABLE ymes.tbleqpequipmenttypeacctype IS 'MES Oracle source: MESTEST.TBLEQPEQUIPMENTTYPEACCTYPE';

CREATE TABLE IF NOT EXISTS ymes.tbleqpequipmenttype_picture (
    picturename varchar(50) NOT NULL,
    picturebody bytea NOT NULL,
    CONSTRAINT pk_tbleqpequipmenttype_picture PRIMARY KEY (picturename)
);
COMMENT ON TABLE ymes.tbleqpequipmenttype_picture IS 'MES Oracle source: MESTEST.TBLEQPEQUIPMENTTYPE_PICTURE';

CREATE TABLE IF NOT EXISTS ymes.tbleqpequipmentproperty (
    equipment varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    defaultvalue varchar(255),
    propertysequence numeric(2,0),
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbleqpequipmentproperty PRIMARY KEY (equipment, propertyno)
);
COMMENT ON TABLE ymes.tbleqpequipmentproperty IS 'MES Oracle source: MESTEST.TBLEQPEQUIPMENTPROPERTY';

CREATE TABLE IF NOT EXISTS ymes.tbleqpconsumebasis (
    equipmenttype varchar(25) NOT NULL,
    consumeno varchar(25) NOT NULL,
    quantity numeric(8,0) NOT NULL,
    toleranceqty numeric(16,8) NOT NULL,
    period numeric(5,0) NOT NULL,
    toleranceperiod numeric(5,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tbleqpconsumebasis PRIMARY KEY (equipmenttype, consumeno)
);
COMMENT ON TABLE ymes.tbleqpconsumebasis IS 'MES Oracle source: MESTEST.TBLEQPCONSUMEBASIS';

-- Not included: the accessory type catalogue table. Its exact Oracle table name and DDL
-- have not yet been verified. Do not invent it; obtain source metadata before API/schema work.
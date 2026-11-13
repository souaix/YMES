-- Source: MES Oracle MESTEST.TBLEQPSTATEBASIS DBMS_METADATA DDL.
-- Target: PostgreSQL schema ymes. This creates an empty master table only.
-- It does not create the reference tables used by the legacy deletion check.
-- Compare an existing YMES table column by column before applying; IF NOT EXISTS does not reconcile schemas.
CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tbleqpstatebasis (
    equipmentstate numeric(2,0) NOT NULL,
    statetype numeric(2,0) DEFAULT 1 NOT NULL,
    statename varchar(50) NOT NULL,
    statecolor numeric(11,0) NOT NULL,
    description varchar(255),
    engineergroupno varchar(20),
    utilizationtype numeric(1,0) DEFAULT 1 NOT NULL,
    statecolor_hex varchar(15),
    CONSTRAINT pk_tbleqpstatebasis PRIMARY KEY (equipmentstate)
);
COMMENT ON TABLE ymes.tbleqpstatebasis IS 'MES Oracle source: MESTEST.TBLEQPSTATEBASIS';
COMMENT ON COLUMN ymes.tbleqpstatebasis.equipmentstate IS 'Equipment state code (0-99 by NUMBER(2,0))';
COMMENT ON COLUMN ymes.tbleqpstatebasis.statetype IS 'State type ValueList index; WinForms labels include System and User Define';
COMMENT ON COLUMN ymes.tbleqpstatebasis.statename IS 'Equipment state display name';
COMMENT ON COLUMN ymes.tbleqpstatebasis.statecolor IS '.NET Color.ToArgb() signed integer value';
COMMENT ON COLUMN ymes.tbleqpstatebasis.description IS 'State description';
COMMENT ON COLUMN ymes.tbleqpstatebasis.engineergroupno IS 'Optional equipment engineer group number; not edited by this form';
COMMENT ON COLUMN ymes.tbleqpstatebasis.utilizationtype IS 'Utilization radio value: 0=Off, 1=On';
COMMENT ON COLUMN ymes.tbleqpstatebasis.statecolor_hex IS 'RGB hexadecimal color without #; written by WinForms Confirm';

-- Deletion references are checked through legacy tables tblEMSEquipmentState,
-- TBLEQPSTATECONTROL, and tblEQPQCListBasis.UNVALIDACTION. Their DDL is not duplicated here.

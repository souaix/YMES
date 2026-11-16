-- PostgreSQL empty-table CREATE SQL converted from the confirmed Oracle source DDL.
-- For YMES only. Do not run in MES Oracle. Contains no operational equipment data.
-- The primary keys create the same unique indexes; Oracle dictionary shows no
-- additional indexes or foreign keys on either source table.

CREATE TABLE IF NOT EXISTS tbleqpstatecontrol (
    controltype varchar(50) NOT NULL,
    controlno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    statename varchar(50) NOT NULL,
    CONSTRAINT pk_tbleqpstatecontrol PRIMARY KEY (controltype, controlno)
);

CREATE TABLE IF NOT EXISTS tblemsequipmentstate (
    eqpserialno varchar(30) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    equipmentstate numeric(2,0) NOT NULL,
    equipmenttype varchar(50) NOT NULL,
    starttime timestamp without time zone NOT NULL,
    userno varchar(10) NOT NULL,
    description varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblemsequipmentstate PRIMARY KEY (equipmentno)
);

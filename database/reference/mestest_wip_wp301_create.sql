-- PostgreSQL CREATE-only reference for WIP WP301.
-- Source: Oracle MESTEST metadata queried read-only on 2026-11-20.
-- This is not a migration. Compare YMES live schema and migration history first.
-- Existing shared WIP, MO, material, inventory and map tables are not repeated here.

CREATE TABLE IF NOT EXISTS "YMES".tblwipeqpmaterialstate (
    equipmentno varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    checklotno numeric(1, 0) NOT NULL DEFAULT 0,
    mono varchar(50) NOT NULL,
    inputmaterialno varchar(50) NOT NULL DEFAULT 'N/A',
    materiallotno varchar(50) NOT NULL,
    seq numeric(6, 0) NOT NULL,
    inputqty numeric(16, 8) NOT NULL,
    qty numeric(16, 8) NOT NULL,
    reviser varchar(20) NOT NULL,
    revisedate timestamp without time zone NOT NULL,
    opendate timestamp without time zone,
    expiredate timestamp without time zone,
    acclimationdate timestamp without time zone,
    putinplaceno varchar(50),
    putinplacetype numeric(1, 0),
    materiallevel numeric(1, 0),
    locatorno varchar(20) DEFAULT 'N/A',
    hascomponent numeric(1, 0),
    deducttype numeric(1, 0),
    CONSTRAINT pk_tblwipeqpmaterialstate PRIMARY KEY
        (equipmentno, materialno, inputmaterialno, materiallotno, mono)
);

CREATE TABLE IF NOT EXISTS "YMES".tblwipeqpmtlstate_comp (
    mono varchar(50) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    materialno varchar(50) NOT NULL,
    inputmaterialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    goodqty numeric(12, 4) NOT NULL,
    scrapqty numeric(12, 4) NOT NULL,
    unitno varchar(30) NOT NULL,
    componentlotno varchar(50),
    s_lasermark varchar(50),
    b_cassetteno varchar(50),
    slotno varchar(3),
    CONSTRAINT pk_tblwipeqpmtlstate_comp PRIMARY KEY
        (mono, equipmentno, materialno, inputmaterialno, materiallotno, componentno)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_binklessmapstate (
    materiallotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    coordinatex numeric(6, 0) NOT NULL,
    coordinatey numeric(6, 0) NOT NULL,
    binno numeric(6, 0),
    result numeric(1, 0) NOT NULL,
    attachequipmentno varchar(50),
    attachlotno varchar(50),
    attachloggroupserial varchar(50),
    substratemateriallotno varchar(50),
    substratecomponentno varchar(50),
    substratecoordinatex numeric(6, 0),
    substratecoordinatey numeric(6, 0),
    substratesubindex numeric(6, 0),
    attachuserno varchar(10),
    attacheventtime timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_binklessmapstate PRIMARY KEY
        (materiallotno, componentno, coordinatex, coordinatey)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_bsubstratemapstate (
    materiallotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    coordinatex numeric(6, 0) NOT NULL,
    coordinatey numeric(6, 0) NOT NULL,
    substratesubindex numeric(6, 0) NOT NULL,
    result numeric(1, 0) NOT NULL,
    attachequipmentno varchar(50),
    attachlotno varchar(50),
    attachloggroupserial varchar(50),
    fromwaferlot varchar(50),
    fromwaferid numeric(3, 0),
    fromwaferidtext varchar(50),
    fromwaferbinno numeric(6, 0),
    fromcoordinatex numeric(6, 0),
    fromcoordinatey numeric(6, 0),
    attachuserno varchar(10),
    attacheventtime timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_bsubstratemapstate PRIMARY KEY
        (materiallotno, componentno, coordinatex, coordinatey, substratesubindex)
);

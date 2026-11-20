-- WP902 CREATE-only reference from Oracle dictionary metadata.
-- Oracle VARCHAR2 uses BYTE limits; validate equivalent byte limits if required.
-- LONG is mapped to storage-only text. Never execute legacy UPDATESQL as SQL.
CREATE TABLE IF NOT EXISTS ymes.tblinvstockerdetaillog (
    stockerno varchar(20) NOT NULL,
    desstockerno varchar(50) NOT NULL,
    srclocatorno varchar(50) NOT NULL,
    deslocatorno varchar(50) NOT NULL,
    contentno varchar(50) NOT NULL,
    contentqty numeric(3,0) NOT NULL,
    areano varchar(20),
    lotno varchar(50),
    status varchar(5),
    carrierno varchar(50),
    creator varchar(10),
    createdate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tblinvstockerdetaillog PRIMARY KEY
        (stockerno, desstockerno, srclocatorno, deslocatorno, contentno, createdate)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmaterialdeductcontent (
    lotserial varchar(55) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    datatypeno numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    lotqty numeric(16,8) NOT NULL,
    deductqty numeric(16,8) NOT NULL,
    putinplaceno varchar(50) NOT NULL,
    putinplacetype numeric(1,0) NOT NULL,
    locatorno varchar(20) NOT NULL,
    stocktablename varchar(30) NOT NULL,
    deductdate timestamp(0) without time zone NOT NULL,
    deductmemo varchar(255) NOT NULL,
    erpflag varchar(1),
    erpno varchar(50),
    mono varchar(50),
    deducttype numeric(1,0),
    deductunitno varchar(10),
    CONSTRAINT pk_tblwipmaterialdeductcontent PRIMARY KEY
        (lotserial, loggroupserial, datatypeno, equipmentno, materiallevel, materialno, materiallotno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipmtldeductcont_comp (
    lotserial varchar(55) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    datatypeno numeric(1,0) NOT NULL,
    deducttype numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    materiallevel numeric(1,0) NOT NULL,
    materialno varchar(50) NOT NULL,
    materiallotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    deductgoodqty numeric(12,4) NOT NULL,
    deductscrapqty numeric(12,4) NOT NULL,
    CONSTRAINT pk_tblwipmtldeductcont_comp PRIMARY KEY
        (lotserial, loggroupserial, datatypeno, equipmentno, materiallevel, materialno, materiallotno, componentno)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipopgroupstate (
    lotno varchar(50) NOT NULL,
    groupno varchar(30) NOT NULL,
    opno varchar(20) NOT NULL,
    nodeid varchar(50) NOT NULL,
    grouptype numeric(1,0) NOT NULL,
    executeflag numeric(1,0) NOT NULL,
    groupnodeid varchar(50) NOT NULL,
    lotserial varchar(55)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipreverseupdatecontent (
    lotserial varchar(55) NOT NULL,
    updatesql text NOT NULL,
    reverseorder numeric(8,0) NOT NULL,
    estimateaffectrows numeric(6,0)
);

CREATE INDEX IF NOT EXISTS i_tblwipopgroupstate_01 ON ymes.tblwipopgroupstate (lotno);
CREATE INDEX IF NOT EXISTS i_wipreverseupdatecontent_01 ON ymes.tblwipreverseupdatecontent (lotserial);

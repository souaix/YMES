BEGIN;

-- QC217 equipment inspection settings. These source tables were absent from
-- the live YMES schema; definitions follow the checked MESTEST PostgreSQL
-- CREATE reference and preserve the shared main/detail key semantics.
CREATE TABLE "YMES".tblqcinspectionbasis (
    opno              varchar(20)  NOT NULL,
    equipmentno       varchar(50)  NOT NULL,
    insptype          numeric(2,0) NOT NULL,
    inspstarttime     varchar(20)  NOT NULL DEFAULT 'N/A',
    periodmin         numeric(5,0),
    equipmentstate    numeric(2,0),
    lotcount          numeric(3,0),
    issuestate        numeric(1,0) NOT NULL DEFAULT 0,
    creator           varchar(10),
    createdate        timestamp without time zone,
    inspweek          numeric(2,0) NOT NULL DEFAULT -1,
    mailgroupno       varchar(255),
    ccmailgroupno     varchar(255),
    eqpdatatype       varchar(255) DEFAULT '0',
    datastamp         numeric(11,0) DEFAULT 0,
    reviser           varchar(10),
    revisedate        timestamp without time zone,
    CONSTRAINT pk_tblqcinspectionbasis
        PRIMARY KEY (opno, equipmentno, insptype, inspweek, inspstarttime)
);

CREATE TABLE "YMES".tblqcinspectiondetail (
    opno              varchar(20)  NOT NULL,
    equipmentno       varchar(50)  NOT NULL,
    insptype          numeric(2,0) NOT NULL,
    qcitemno          varchar(25)  NOT NULL,
    qcitemtype        numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblqcinspectiondetail
        PRIMARY KEY (opno, equipmentno, insptype, qcitemno)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspectionbasis TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcinspectiondetail TO ymes_app;

COMMIT;

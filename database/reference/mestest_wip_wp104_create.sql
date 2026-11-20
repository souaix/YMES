-- CREATE-only reference generated from MESTEST source metadata for WP104.
-- Do not run as a migration without checking YMES PostgreSQL live schema/history.

CREATE TABLE IF NOT EXISTS ymes.tblwipoperatorstate (
    userno varchar(10) NOT NULL,
    shiftno varchar(20) NOT NULL,
    logindate timestamp without time zone NOT NULL,
    workdate timestamp without time zone NOT NULL,
    opno varchar(20) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    loginplaceno varchar(50) NOT NULL,
    CONSTRAINT pk_tblwipoperatorstate PRIMARY KEY (userno, multioperatormode, loginplaceno)
);
COMMENT ON TABLE ymes.tblwipoperatorstate IS 'MES Oracle source: MESTEST.TBLWIPOPERATORSTATE; PK (USERNO, MULTIOPERATORMODE, LOGINPLACENO)';

CREATE TABLE IF NOT EXISTS ymes.tblwipoperatorlog (
    sid varchar(30) NOT NULL,
    userno varchar(10) NOT NULL,
    shiftno varchar(20) NOT NULL,
    logindate timestamp without time zone NOT NULL,
    logoutdate timestamp without time zone NOT NULL,
    exceptiontime numeric(10,2) DEFAULT 0 NOT NULL,
    worktime numeric(10,2) DEFAULT 0 NOT NULL,
    realworktime numeric(10,2) DEFAULT 0 NOT NULL,
    stdworktime numeric(10,2) DEFAULT 0 NOT NULL,
    qty numeric(10,4) DEFAULT 0 NOT NULL,
    workdate timestamp without time zone NOT NULL,
    opno varchar(20) DEFAULT 'N/A' NOT NULL,
    multioperatormode numeric(1,0) DEFAULT 0 NOT NULL,
    loginplaceno varchar(50) NOT NULL,
    eventtime timestamp without time zone,
    CONSTRAINT pk_tblwipoperatorlog PRIMARY KEY (sid)
);
COMMENT ON TABLE ymes.tblwipoperatorlog IS 'MES Oracle source: MESTEST.TBLWIPOPERATORLOG; PK (SID)';

-- Oracle metadata reports no foreign keys, triggers, or indexes other than the PK indexes.

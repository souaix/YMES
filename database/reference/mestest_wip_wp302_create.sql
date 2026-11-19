-- PostgreSQL CREATE-only reference derived from Oracle MESTEST source DDL.
-- Do not run over a live YMES schema without first comparing tables and migrations.

CREATE TABLE IF NOT EXISTS ymes.tblwipcarrierstatedetail (
    carrierno varchar(50) NOT NULL,
    carrierposition numeric(3,0) NOT NULL DEFAULT 1,
    bcip varchar(50) NOT NULL,
    contentid varchar(50) NOT NULL,
    flag numeric(1,0) NOT NULL DEFAULT 0,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    eventtime timestamp without time zone,
    s_bindmode numeric(1,0),
    s_bindopno varchar(20),
    s_bindtype numeric(1,0)
);

CREATE INDEX IF NOT EXISTS i_tblwipcarrierstatedetail_01
    ON ymes.tblwipcarrierstatedetail (componentno);

COMMENT ON TABLE ymes.tblwipcarrierstatedetail IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATEDETAIL';

CREATE TABLE IF NOT EXISTS ymes.tblwipcarrierstatedetaillog (
    lotserial varchar(50) NOT NULL,
    loggroupserial varchar(50) NOT NULL,
    carrierno varchar(50) NOT NULL,
    carrierposition numeric(3,0) NOT NULL,
    bcip varchar(50) NOT NULL,
    contentid varchar(50) NOT NULL,
    flag numeric(1,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    componentno varchar(50) NOT NULL,
    putintime timestamp without time zone,
    takeouttime timestamp without time zone,
    s_bindmode numeric(1,0),
    s_bindopno varchar(20),
    s_bindtype numeric(1,0),
    s_releasebindmode numeric(1,0),
    s_releasebindopno varchar(20),
    s_releasebindtype numeric(1,0),
    s_releaseuserno varchar(10)
);

COMMENT ON TABLE ymes.tblwipcarrierstatedetaillog IS 'MES Oracle source: MESTEST.TBLWIPCARRIERSTATEDETAILLOG';

CREATE TABLE IF NOT EXISTS ymes.tblinvstockerstate (
    stockerno varchar(20) NOT NULL,
    locatorno varchar(20) NOT NULL,
    contentno varchar(50) NOT NULL,
    areano varchar(20) NOT NULL,
    lotno varchar(20),
    carrierno varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblinvstockerstate PRIMARY KEY (stockerno, locatorno, contentno)
);

COMMENT ON TABLE ymes.tblinvstockerstate IS 'MES Oracle source: MESTEST.TBLINVSTOCKERSTATE';

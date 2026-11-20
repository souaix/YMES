-- CREATE-only from MESTEST dictionary. Oracle VARCHAR2 uses BYTE limits.
CREATE TABLE IF NOT EXISTS ymes.tblwipcont_attribadjustlog (
    loggroupserial varchar(50) NOT NULL,
    attribno varchar(20) NOT NULL,
    componentno varchar(50),
    attribvalueorg varchar(50),
    attribvaluenew varchar(50),
    description varchar(255),
    revisor varchar(10) NOT NULL,
    revisedate timestamp(0) without time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS i_wipcont_attribadjustlog_01 ON ymes.tblwipcont_attribadjustlog (loggroupserial);

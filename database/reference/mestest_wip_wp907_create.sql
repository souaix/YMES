-- CREATE-only reference from MESTEST dictionary. Verify BYTE limits and defaults before migration.
CREATE TABLE IF NOT EXISTS ymes.tblwipadjustlog (
    adjustserial varchar(25) NOT NULL,
    lotno varchar(50) NOT NULL,
    lotserial varchar(55) NOT NULL,
    inputqty numeric(12,4),
    bonusqty numeric(12,4),
    deductionqty numeric(12,4),
    description varchar(255),
    userid varchar(10),
    recorddate timestamp(0) without time zone,
    CONSTRAINT pk_tblwipadjustlog PRIMARY KEY (adjustserial)
);
CREATE INDEX IF NOT EXISTS i_tblwipadjustlog_01 ON ymes.tblwipadjustlog (lotserial);

CREATE TABLE IF NOT EXISTS ymes.tblwipadjustdetail (
    adjustserial varchar(25) NOT NULL,
    reasonno varchar(20) NOT NULL,
    CONSTRAINT pk_tblwipadjustdetail PRIMARY KEY (adjustserial, reasonno)
);

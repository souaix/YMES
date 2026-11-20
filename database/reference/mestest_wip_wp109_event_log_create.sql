-- CREATE-only from Oracle dictionary. Compare live schema before adding a shared EventLog provider.
CREATE TABLE IF NOT EXISTS ymes.tblsyseventlog (
    userid varchar(10) NOT NULL,
    recorddate timestamp(0) without time zone NOT NULL,
    servicename varchar(30) NOT NULL,
    description text,
    keyfield varchar(1000),
    keyvalue varchar(1000),
    computername varchar(50)
);
CREATE INDEX IF NOT EXISTS i_tblsyseventlog_01 ON ymes.tblsyseventlog (recorddate);

-- PostgreSQL CREATE-only reference derived from Oracle MESTEST source DDL.
-- Do not run over a live YMES schema without first comparing tables and migrations.

CREATE TABLE IF NOT EXISTS ymes.tblwipeqpconsumestatus (
    equipmentno varchar(50) NOT NULL,
    consumeno varchar(25) NOT NULL,
    consumeseq varchar(30) NOT NULL,
    curqty numeric(8,0) NOT NULL,
    curperiod numeric(7,2) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    revisor varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblwipeqpconsumestatus PRIMARY KEY (equipmentno, consumeno)
);

COMMENT ON TABLE ymes.tblwipeqpconsumestatus IS 'MES Oracle source: MESTEST.TBLWIPEQPCONSUMESTATUS';

CREATE TABLE IF NOT EXISTS ymes.tblwipeqpconsumelog (
    equipmentno varchar(50) NOT NULL,
    consumeno varchar(25) NOT NULL,
    consumeseq varchar(30) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    accqty numeric(8,0),
    accperiod numeric(7,2),
    CONSTRAINT pk_tblwipeqpconsumelog PRIMARY KEY (equipmentno, consumeno, consumeseq)
);

COMMENT ON TABLE ymes.tblwipeqpconsumelog IS 'MES Oracle source: MESTEST.TBLWIPEQPCONSUMELOG';

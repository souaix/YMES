BEGIN;
-- Home-PC live audit 2026-11-18: missing table; no source business data copied.
CREATE TABLE "YMES".tblprsproductionsegment (
    psno varchar(50) NOT NULL,
    psname varchar(50) NOT NULL,
    psorder numeric(2,0) DEFAULT 0 NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    havecomponent numeric(1,0) DEFAULT 0 NOT NULL,
    havelevel numeric(1,0) DEFAULT 0 NOT NULL,
    serialtypeno_lot varchar(50),
    serialtypeno_comp varchar(50),
    ispartial numeric(1,0) DEFAULT 0 NOT NULL,
    isnetflow numeric(1,0) DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprsproductionsegment PRIMARY KEY (psno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprsproductionsegment TO ymes_app;
GRANT SELECT ON "YMES".tblsysserialrulebasis TO ymes_app;
COMMIT;

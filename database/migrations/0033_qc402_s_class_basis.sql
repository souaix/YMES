BEGIN;
-- Live audit 2026-11-18: absent table; approved parent is enforced by API, no source FK.
CREATE TABLE "YMES".tblqcsclassbasis (
    sclassno varchar(30) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    mclassno varchar(30),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcsclassbasis PRIMARY KEY (sclassno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcsclassbasis TO ymes_app;
COMMIT;

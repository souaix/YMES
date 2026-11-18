BEGIN;
-- Home-PC live audit 2026-11-18: missing QC401 source-aligned master only.
CREATE TABLE "YMES".tblqcmclassbasis (
    mclassno varchar(30) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcmclassbasis PRIMARY KEY (mclassno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcmclassbasis TO ymes_app;
COMMIT;

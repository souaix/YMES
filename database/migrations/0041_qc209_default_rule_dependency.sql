BEGIN;
-- 2026-11-19 live audit: missing QC209 table, required by QC301/QC302 defaults.
-- Exact source seven columns and composite PK; no FK/extra index or business data.
CREATE TABLE "YMES".tblqcitemrule (
    qcitemno varchar(25) NOT NULL,
    spcid numeric(2,0) NOT NULL,
    ruleno varchar(20) NOT NULL,
    action numeric(1,0) NOT NULL,
    equipmentstate numeric(2,0),
    erfstate numeric(1,0),
    emailstate numeric(1,0),
    CONSTRAINT pk_tblqcitemrule PRIMARY KEY (qcitemno, spcid, ruleno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemrule TO ymes_app;
-- Existing item master already has SELECT; row-locking SELECT also needs UPDATE.
GRANT UPDATE (qcitemno) ON "YMES".tblqcitemforvariables TO ymes_app;
COMMIT;

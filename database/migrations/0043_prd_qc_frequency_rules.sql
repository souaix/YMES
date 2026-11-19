BEGIN;
-- 2026-11-19 PPK SSH read-only audit: table absent in all schemas.
-- Source: mestest_prd_qc_frequency_source_ddl.sql / *_create.sql.
-- No source DEFAULT, IssueState, FK, additional index, or business rows.
CREATE TABLE "YMES".tblprdqcrulebasis (
    productno varchar(25) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    opno varchar(30) NOT NULL,
    qcmethod numeric(1,0) NOT NULL,
    qcnum numeric(2,0),
    shifta varchar(5),
    shiftb varchar(5),
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    qcitem varchar(25),
    qctype numeric(1,0),
    moduleno varchar(10),
    datastamp numeric(11,0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdqcrulebasis PRIMARY KEY(productno,equipmentno,opno)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprdqcrulebasis TO ymes_app;
COMMIT;

BEGIN;
-- Source: mestest_operation_and_system_source_ddl.sql, TBLOPPROPERTY.
-- Shared by OP Basis and OP Type; no parallel YMES property provider/table.
CREATE TABLE IF NOT EXISTS "YMES".tblopproperty (
    opno varchar(20) NOT NULL,
    propertyno varchar(20) NOT NULL,
    defaultvalue varchar(255),
    propertysequence numeric(2,0),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopproperty PRIMARY KEY (opno,propertyno)
);
-- Only the source-defined OP103 hard-delete cleanup is supplied this release.
GRANT SELECT,DELETE ON "YMES".tblopproperty TO ymes_app;
COMMIT;

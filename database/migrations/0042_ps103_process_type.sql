BEGIN;
-- 2026-11-19 PPK SSH live audit: tblprstype is absent; tblprsprocessbasis exists.
-- Source: database/reference/mestest_prs103_process_type_source_ddl.sql.
-- Create schema only; no Oracle business rows, extra indexes or foreign keys.
CREATE TABLE "YMES".tblprstype (
    processtype varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprstype PRIMARY KEY (processtype)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprstype TO ymes_app;
-- The optional shared tblprsprocessproperty table is absent in this audit.
-- Its owner must verify the complete shared provider before creating it.
COMMIT;

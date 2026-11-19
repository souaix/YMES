-- DRAFT ONLY; NOT APPLIED. Root must verify a readable backup and recheck
-- metadata before applying. No rows, source-state default, FK or extra index.
-- 2026-11-19 PPK BEGIN READ ONLY found both exact names absent in ALL schemas.
-- Compared raw MESTEST DDL with PG reference: 11 + 2 columns, first keys intact.
BEGIN;

CREATE TABLE "YMES".tblopecngroupbasis (
    opgroupno varchar(20) NOT NULL,
    opgroupname varchar(50) NOT NULL,
    psno varchar(50) NOT NULL,
    opgrouporder numeric(6,0),
    description varchar(255),
    issuestate numeric(1,0) NOT NULL,
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblopecngroupbasis PRIMARY KEY (opgroupno)
);

CREATE TABLE "YMES".tblopecngroupdetail (
    opgroupno varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    CONSTRAINT pk_tblopecngroupdetail PRIMARY KEY (opno)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblopecngroupbasis TO ymes_app;
GRANT SELECT, INSERT, DELETE ON "YMES".tblopecngroupdetail TO ymes_app;
-- PostgreSQL row-locking SELECT needs UPDATE on at least one column. There is
-- no detail Edit endpoint or table-level UPDATE permission.
GRANT UPDATE (opno) ON "YMES".tblopecngroupdetail TO ymes_app;

COMMIT;

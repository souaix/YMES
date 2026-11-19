-- PREPARED SOURCE; NOT APPLIED. Root reviews the full private database backup,
-- all-schema/type absence guards and independent readback before execution.
-- Oracle MESTEST source and PostgreSQL reference agree: Basis11/Detail12,
-- PK(CustomerNo,CustomerOPNo) and PK(CustomerNo,CustomerOPNo,OPNo).
-- Keep native numeric(3,0); API validation 0..999 does not widen this column.
-- Both real DataStamp columns remain nullable DEFAULT0. No state/workflow,
-- synthetic child stamp, FK, extra index, seed rows or existing-table rebuild.
BEGIN;

CREATE TABLE "YMES".tbl_bopcustopmapbasis (
    customerno varchar(50) NOT NULL,
    customeropno varchar(20) NOT NULL,
    customeropname varchar(50) NOT NULL,
    customeropseq numeric(3,0) NOT NULL,
    psno varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_bopcustopmapbasis PRIMARY KEY (customerno, customeropno)
);

CREATE TABLE "YMES".tbl_bopcustopmapdetail (
    customerno varchar(50) NOT NULL,
    customeropno varchar(20) NOT NULL,
    psno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    custopprocesscode varchar(20) NOT NULL,
    custstepname varchar(20) NOT NULL,
    transactionflag numeric(1,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbl_bopcustopmapdetail PRIMARY KEY (customerno, customeropno, opno)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tbl_bopcustopmapbasis TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tbl_bopcustopmapdetail TO ymes_app;
-- New/changed approved Customer rows are held FOR SHARE until commit. PostgreSQL
-- requires SELECT plus UPDATE on a column for row locking; no other Customer
-- write permission is granted. Existing PS/OP grants are deliberately untouched.
GRANT SELECT ON "YMES".tblentcustomerbasis TO ymes_app;
GRANT UPDATE (customerno) ON "YMES".tblentcustomerbasis TO ymes_app;

COMMIT;

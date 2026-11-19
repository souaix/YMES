BEGIN;
-- 2026-11-19 PPK SSH live metadata audit: tblprdtype is absent.
-- Source: mestest_prd_product_type_source_ddl.sql, eleven dictionary-verified columns.
-- This owns the shared product-type catalog; no business data is copied.
CREATE TABLE "YMES".tblprdtype (
    producttype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    ex_prdtype1 varchar(20),
    ex_prdtype2 varchar(20),
    producttypecode varchar(10),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdtype PRIMARY KEY (producttype)
);
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblprdtype TO ymes_app;
COMMIT;

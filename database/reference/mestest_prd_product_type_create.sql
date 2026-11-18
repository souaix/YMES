-- PostgreSQL CREATE-only comparison. Verify the existing product type lookup first.
CREATE TABLE IF NOT EXISTS "YMES".tblprdtype (
    producttype varchar(50) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    issuestate numeric(1,0) DEFAULT 0 NOT NULL,
    ex_prdtype1 varchar(20),
    ex_prdtype2 varchar(20),
    producttypecode varchar(10),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdtype PRIMARY KEY (producttype)
);

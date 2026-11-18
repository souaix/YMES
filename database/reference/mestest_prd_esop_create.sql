-- PostgreSQL CREATE-only comparison for existing PD102 eSOP schema.
-- The first-phase reference already defines this table. Verify live schema first.
CREATE TABLE IF NOT EXISTS "YMES".tblprdesopbasis (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    subopno varchar(50) NOT NULL,
    positionno varchar(50) NOT NULL,
    sopfile varchar(255),
    pageno varchar(20),
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tblprdesopbasis PRIMARY KEY (productno, productversion, opno, subopno, positionno)
);

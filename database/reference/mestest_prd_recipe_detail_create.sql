-- PostgreSQL CREATE-only comparison. Reconcile the live schema and prior references first.
CREATE TABLE IF NOT EXISTS "YMES".tblprdrecipedetail (
    recipegroup varchar(50) NOT NULL,
    recipeversion numeric(2,0) DEFAULT 0 NOT NULL,
    opno varchar(20) NOT NULL,
    recipeno varchar(20) NOT NULL,
    recipevalue varchar(255),
    sequence numeric(2,0) DEFAULT 0,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdrecipedetail PRIMARY KEY (recipegroup, recipeversion, opno, recipeno)
);

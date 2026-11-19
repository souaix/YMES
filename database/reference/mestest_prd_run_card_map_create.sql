-- PostgreSQL CREATE only. Use the established YMES schema.
-- Columns, nullability and primary keys only. Read-only reference.

CREATE TABLE IF NOT EXISTS "YMES".tblprdruncardmap (
    productno varchar(50) NOT NULL,
    runcardno varchar(50) NOT NULL,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblprdruncardmap PRIMARY KEY (productno)
);

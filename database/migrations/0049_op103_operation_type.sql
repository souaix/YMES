BEGIN;
-- Source: mestest_op103_optype_source_ddl.sql; live audit found no tbloptype.
CREATE TABLE IF NOT EXISTS "YMES".tbloptype (
    optype varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tbloptype PRIMARY KEY (optype)
);
GRANT SELECT,INSERT,UPDATE,DELETE ON "YMES".tbloptype TO ymes_app;
COMMIT;

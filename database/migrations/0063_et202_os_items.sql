-- 0063 ET202: PREPARED ONLY / NOT EXECUTED.
-- Root allocated 0063 after true read-only audit1c9ac confirmed all names absent.
-- Run only as postgres in Root's reviewed guarded whole transaction, after
-- compatible API176 deployment/absent proof and fresh whole catalog62d9 gate.
-- Existing schema YMES/roles/defaultACL and all parent objects stay unchanged.
-- No IF NOT EXISTS, CREATE SCHEMA, seed, backfill, role changes or extra index.
CREATE TABLE "YMES".tblentositem (
    ositemno varchar(2) NOT NULL,
    ositemname varchar(50) NOT NULL,
    plandays numeric(3,0) NOT NULL,
    processno varchar(30) NOT NULL,
    returntype numeric(1,0) NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentositem PRIMARY KEY (ositemno)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE "YMES".tblentositem TO ymes_app;

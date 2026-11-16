-- Source: Oracle MESTEST.TBLENTOSITEM DDL and dictionary queried read-only on 2026-11-16.
-- Target schema follows the quoted uppercase YMES schema used by the API.
-- Empty table only; no MES business data is included.
-- Oracle DATE maps to timestamp without time zone; NUMBER maps to numeric.
-- Compare the actual YMES schema/migrations before applying. IF NOT EXISTS does
-- not reconcile an already existing table or its columns/constraints.

CREATE SCHEMA IF NOT EXISTS "YMES";
SET search_path TO "YMES", public;

CREATE TABLE IF NOT EXISTS tblentositem (
    ositemno varchar(2) CONSTRAINT n_tblentositem_01 NOT NULL,
    ositemname varchar(50) CONSTRAINT n_tblentositem_02 NOT NULL,
    plandays numeric(3,0) CONSTRAINT n_tblentositem_03 NOT NULL,
    processno varchar(30) CONSTRAINT n_tblentositem_04 NOT NULL,
    returntype numeric(1,0) CONSTRAINT n_tblentositem_05 NOT NULL,
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentositem PRIMARY KEY (ositemno)
);

COMMENT ON TABLE tblentositem IS 'MES Oracle source: MESTEST.TBLENTOSITEM; empty structure only.';

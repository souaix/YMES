-- WP207: create only the six Oracle source tables proven absent from YMES.
-- Derived from database/reference/mestest_wip_wp207_*; no business data copied.
-- TBLWIPCONT_EMPLOYEE intentionally remains without a PK or unique index.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '60s';
SELECT pg_advisory_xact_lock(hashtextextended('YMES:Migration:WP207:0109', 0));

DO $guard$
DECLARE object_name text;
BEGIN
    IF current_database() <> 'ymes' OR current_user <> 'postgres' OR session_user <> 'postgres'
       OR current_setting('server_version_num')::integer NOT BETWEEN 140000 AND 149999
       OR current_setting('transaction_read_only') <> 'off' THEN
        RAISE EXCEPTION '0109 requires a writable PostgreSQL 14 transaction on ymes as postgres';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app')
       OR NOT has_schema_privilege('ymes_app', 'YMES', 'USAGE') THEN
        RAISE EXCEPTION 'ymes_app or YMES schema USAGE is missing; refusing 0109';
    END IF;
    IF has_schema_privilege('ymes_app', 'YMES', 'CREATE') THEN
        RAISE EXCEPTION 'ymes_app has YMES schema CREATE; review permissions before 0109';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblemsdispatchstate', 'tblwipautomerge', 'tblwipcont_employee',
        'tblwiplotopecnrecord', 'tblwiposbasis', 'tblwiposdetail'
    ] LOOP
        IF to_regclass(format('%I.%I', 'YMES', object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare live structure before 0109', object_name;
        END IF;
        IF EXISTS (
            SELECT 1
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE lower(c.relname) = object_name
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; inspect before 0109', object_name;
        END IF;
    END LOOP;

    IF EXISTS (
        SELECT 1
        FROM pg_catalog.pg_class c
        WHERE c.relkind = 'i'
          AND lower(c.relname) = ANY (ARRAY[
              'pk_tblemsdispatchstate', 'pk_tblwipautomerge',
              'i_tblwipcont_employee_01', 'i_tblwipcont_employee_02', 'i_tblwipcont_employee_03',
              'pk_tblwiplotopecnrecord', 'pk_tblwiposbasis', 'pk_tblwiposdetail'
          ])
    ) THEN
        RAISE EXCEPTION 'A WP207 source index name already exists; inspect before 0109';
    END IF;
END
$guard$;

CREATE TABLE "YMES".tblemsdispatchstate (
    equipmentno varchar(50) NOT NULL,
    dispatchorder numeric(3,0) NOT NULL,
    lotno varchar(50) NOT NULL,
    CONSTRAINT pk_tblemsdispatchstate PRIMARY KEY (equipmentno, dispatchorder)
);
COMMENT ON TABLE "YMES".tblemsdispatchstate IS 'MES Oracle source: MESTEST.TBLEMSDISPATCHSTATE';

CREATE TABLE "YMES".tblwipautomerge (
    fromlotno varchar(50) NOT NULL,
    tolotno varchar(50) NOT NULL,
    lotserial varchar(55),
    CONSTRAINT pk_tblwipautomerge PRIMARY KEY (fromlotno)
);
COMMENT ON TABLE "YMES".tblwipautomerge IS 'MES Oracle source: MESTEST.TBLWIPAUTOMERGE';

CREATE TABLE "YMES".tblwipcont_employee (
    lotserial varchar(55) NOT NULL,
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    brno varchar(20) NOT NULL,
    userno varchar(10) NOT NULL,
    eventtime timestamp without time zone NOT NULL,
    functionname varchar(100),
    shiftno varchar(20),
    loggroupserial varchar(50),
    linkname varchar(50),
    interfacetime timestamp without time zone,
    phaseno numeric(2,0)
);
COMMENT ON TABLE "YMES".tblwipcont_employee IS 'MES Oracle source: MESTEST.TBLWIPCONT_EMPLOYEE; no PK or unique index';
CREATE INDEX i_tblwipcont_employee_01 ON "YMES".tblwipcont_employee (loggroupserial);
CREATE INDEX i_tblwipcont_employee_02 ON "YMES".tblwipcont_employee (lotserial);
CREATE INDEX i_tblwipcont_employee_03 ON "YMES".tblwipcont_employee (lotno, functionname, eventtime);

CREATE TABLE "YMES".tblwiplotopecnrecord (
    lotno varchar(50) NOT NULL,
    opno varchar(20) NOT NULL,
    ecnno_op varchar(20),
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    reviser varchar(10),
    revisedate timestamp without time zone,
    ecntype varchar(10),
    areano varchar(500) NOT NULL,
    CONSTRAINT pk_tblwiplotopecnrecord PRIMARY KEY (lotno, opno)
);
COMMENT ON TABLE "YMES".tblwiplotopecnrecord IS 'MES Oracle source: MESTEST.TBLWIPLOTOPECNRECORD';

CREATE TABLE "YMES".tblwiposbasis (
    osno varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    preparename varchar(10),
    needbydate timestamp without time zone,
    osqty numeric(12,4) NOT NULL,
    status numeric(2,0) NOT NULL,
    shpdate timestamp without time zone,
    closedate timestamp without time zone,
    price numeric(12,4),
    subcontractorno varchar(20),
    nodeid varchar(50),
    ositemno varchar(2),
    returnopno varchar(20),
    returnnodeid varchar(50),
    shpuserno varchar(10),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    reviser varchar(10),
    revisedate timestamp without time zone,
    ostype numeric(1,0),
    erpno_out varchar(20),
    datastamp numeric(11,0) DEFAULT 0,
    CONSTRAINT pk_tblwiposbasis PRIMARY KEY (osno)
);
COMMENT ON TABLE "YMES".tblwiposbasis IS 'MES Oracle source: MESTEST.TBLWIPOSBASIS';

CREATE TABLE "YMES".tblwiposdetail (
    osno varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    rcvdate timestamp without time zone,
    status numeric(2,0) NOT NULL,
    inputqty numeric(12,4) NOT NULL,
    inputunitno varchar(30),
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    otherqty numeric(12,4),
    description varchar(250),
    unitprice numeric(12,4),
    returnunitno varchar(30),
    rcvuserno varchar(10),
    erpno_rcv varchar(20),
    CONSTRAINT pk_tblwiposdetail PRIMARY KEY (osno, lotno)
);
COMMENT ON TABLE "YMES".tblwiposdetail IS 'MES Oracle source: MESTEST.TBLWIPOSDETAIL';

REVOKE ALL ON "YMES".tblemsdispatchstate,
    "YMES".tblwipautomerge,
    "YMES".tblwipcont_employee,
    "YMES".tblwiplotopecnrecord,
    "YMES".tblwiposbasis,
    "YMES".tblwiposdetail FROM PUBLIC;

COMMIT;

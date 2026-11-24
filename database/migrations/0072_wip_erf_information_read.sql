-- Empty MES-source tables required by the shared WP508/SC401/SC402 ERF Info read API.
-- CREATE-only inputs: WP501, QC409 ERF disposition/OCAP and WP508Info references.
-- This forward migration creates structure only; it does not import MES business data.
BEGIN;

DO $guard$
DECLARE
    object_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname='YMES') THEN
        RAISE EXCEPTION 'YMES schema is missing; refusing ERF Info migration';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname='ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; refusing ERF Info migration';
    END IF;
    IF NOT has_schema_privilege('ymes_app','YMES','USAGE') THEN
        RAISE EXCEPTION 'ymes_app lacks YMES schema USAGE; refusing ERF Info migration';
    END IF;
    IF to_regclass('"YMES".tblwiperfbasis') IS NULL
       OR NOT has_table_privilege('ymes_app','"YMES".tblwiperfbasis','SELECT') THEN
        RAISE EXCEPTION 'YMES.tblwiperfbasis or its app SELECT grant is missing';
    END IF;
    IF to_regclass('"YMES".tblprsnodebasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblprsnodebasis is missing; compare its live structure before migrating';
    END IF;

    FOREACH object_name IN ARRAY ARRAY[
        'tblwiperfholdreason','tblwiperfholdreasondisp','tblwiperfdisposition',
        'tblwiperflotdisposition','tblwiperf_attachfile','tblwiperfocap_attachfile',
        'tblwiperfocapdisposition'
    ] LOOP
        IF to_regclass(format('%I.%I','YMES',object_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its live structure before migrating', object_name;
        END IF;
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE lower(c.relname)=object_name AND n.nspname<>'YMES'
              AND c.relkind IN ('r','p','v','m','f')
        ) THEN
            RAISE EXCEPTION 'An object named % exists outside YMES; compare it before migrating', object_name;
        END IF;
    END LOOP;
END
$guard$;

CREATE TABLE "YMES".tblwiperfholdreason (
    erfno varchar(20) NOT NULL,
    holditemno varchar(50) NOT NULL,
    qty numeric(12,4) DEFAULT 0,
    lotserial varchar(55),
    componentno varchar(50) DEFAULT 'N/A' NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    description varchar(255),
    areano varchar(10) NOT NULL,
    CONSTRAINT pk_tblwiperfholdreason PRIMARY KEY (erfno, componentno, holditemno, areano)
);
CREATE INDEX i_tblwiperfholdreason_01 ON "YMES".tblwiperfholdreason (lotserial);
COMMENT ON TABLE "YMES".tblwiperfholdreason IS 'MES Oracle source: MESTEST.TBLWIPERFHOLDREASON';

CREATE TABLE "YMES".tblwiperfholdreasondisp (
    erfno varchar(20) NOT NULL,
    holditemno varchar(50) NOT NULL,
    qty numeric(12,4) DEFAULT 0,
    holddisposition varchar(100),
    lotserial varchar(55),
    componentno varchar(50) DEFAULT 'N/A' NOT NULL,
    goodqty numeric(12,4),
    scrapqty numeric(12,4),
    description varchar(255),
    areano varchar(10) NOT NULL,
    CONSTRAINT pk_tblwiperfholdreasondisp PRIMARY KEY (erfno, componentno, holditemno, areano)
);
CREATE INDEX i_tblwiperfholdreasondisp_01 ON "YMES".tblwiperfholdreasondisp (lotserial);
COMMENT ON TABLE "YMES".tblwiperfholdreasondisp IS 'MES Oracle source: MESTEST.TBLWIPERFHOLDREASONDISP';

CREATE TABLE "YMES".tblwiperfdisposition (
    erfno varchar(20) NOT NULL,
    disporder numeric(2,0) NOT NULL,
    disptype numeric(2,0) NOT NULL,
    dispdescription varchar(500) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    nextdisptype numeric(2,0) NOT NULL,
    lotserial varchar(55),
    groupno varchar(20) NOT NULL,
    nextgroupno varchar(20),
    contactorname varchar(50),
    CONSTRAINT pk_tblwiperfdisposition PRIMARY KEY (erfno, disporder)
);
CREATE INDEX i_tblwiperfdisposition_01 ON "YMES".tblwiperfdisposition (lotserial);
COMMENT ON TABLE "YMES".tblwiperfdisposition IS 'MES Oracle source: MESTEST.TBLWIPERFDISPOSITION';

CREATE TABLE "YMES".tblwiperflotdisposition (
    erfno varchar(20) NOT NULL,
    lotdisporder numeric(2,0) NOT NULL,
    lotdisptype numeric(2,0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    nodeid varchar(50),
    inventoryno varchar(50),
    returntype numeric(1,0),
    returnnodeid varchar(50),
    lotserial varchar(55),
    description varchar(255),
    engno varchar(30),
    engversion varchar(3),
    moduleno varchar(50),
    moduleversion varchar(3),
    startnodeid varchar(50),
    CONSTRAINT pk_tblwiperflotdisposition PRIMARY KEY (erfno, lotdisporder)
);
CREATE INDEX i_tblwiperflotdisposition_01 ON "YMES".tblwiperflotdisposition (lotserial);
COMMENT ON TABLE "YMES".tblwiperflotdisposition IS 'MES Oracle source: MESTEST.TBLWIPERFLOTDISPOSITION';

CREATE TABLE "YMES".tblwiperf_attachfile (
    lotno varchar(50) NOT NULL,
    erfno varchar(20) NOT NULL,
    attachtype varchar(10) NOT NULL,
    attachname varchar(50) NOT NULL,
    status numeric(1,0) DEFAULT 0 NOT NULL,
    attachpoint numeric(1,0) DEFAULT 0 NOT NULL,
    attachbody bytea,
    creator varchar(10),
    createdate timestamp without time zone,
    CONSTRAINT pk_tblwiperf_attachfile PRIMARY KEY (lotno, erfno, attachname)
);
COMMENT ON TABLE "YMES".tblwiperf_attachfile IS 'MES Oracle source: MESTEST.TBLWIPERF_ATTACHFILE';

CREATE TABLE "YMES".tblwiperfocap_attachfile (
    lotno varchar(50) NOT NULL,
    erfno varchar(20) NOT NULL,
    attachtype numeric(1,0) NOT NULL,
    attachname varchar(50) NOT NULL,
    status numeric(1,0) DEFAULT 0 NOT NULL,
    attachbody bytea,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp without time zone NOT NULL,
    CONSTRAINT pk_tblwiperfocap_attachfile PRIMARY KEY (lotno, erfno, attachname, ocapno)
);
COMMENT ON TABLE "YMES".tblwiperfocap_attachfile IS 'MES Oracle source: MESTEST.TBLWIPERFOCAP_ATTACHFILE';

CREATE TABLE "YMES".tblwiperfocapdisposition (
    erfno varchar(20) NOT NULL,
    holditemno varchar(200) NOT NULL,
    ocapno varchar(50) NOT NULL,
    ocapversion varchar(5) NOT NULL,
    disporder numeric(5,0) NOT NULL,
    ocapnodeid varchar(50) NOT NULL,
    ocapnodeno varchar(50) NOT NULL,
    dispuserno varchar(10),
    ocapgroupno varchar(30),
    ocapnextgroupno varchar(30),
    dispdate timestamp without time zone,
    ocaplinkname varchar(20),
    ocapnextdisptype varchar(5),
    description varchar(255),
    CONSTRAINT pk_tblwiperfocapdisposition PRIMARY KEY (erfno, holditemno, ocapno, ocapversion, disporder)
);
COMMENT ON TABLE "YMES".tblwiperfocapdisposition IS 'MES Oracle source: MESTEST.TBLWIPERFOCAPDISPOSITION';

GRANT SELECT ON TABLE
    "YMES".tblwiperfholdreason,
    "YMES".tblwiperfholdreasondisp,
    "YMES".tblwiperfdisposition,
    "YMES".tblwiperflotdisposition,
    "YMES".tblwiperf_attachfile,
    "YMES".tblwiperfocap_attachfile,
    "YMES".tblwiperfocapdisposition,
    "YMES".tblprsnodebasis
TO ymes_app;

COMMIT;

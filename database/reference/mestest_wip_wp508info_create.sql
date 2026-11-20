-- Oracle source: MESTEST.TBLWIPERFLOTDISPOSITION (WP508 ERF Info).
-- CREATE-only reference for home PC. Compare live YMES schema/history before migration.
CREATE TABLE IF NOT EXISTS ymes.tblwiperflotdisposition (
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

CREATE INDEX IF NOT EXISTS i_tblwiperflotdisposition_01 ON ymes.tblwiperflotdisposition (lotserial);
COMMENT ON TABLE ymes.tblwiperflotdisposition IS 'MES Oracle source: MESTEST.TBLWIPERFLOTDISPOSITION; WP508 Lot Disposition';

-- Oracle source DDL already stored in mestest_qc409_wip_disposition_dependencies_source_ddl.sql.
-- Required only when the live YMES schema does not already include the OCAP attachment table.
CREATE TABLE IF NOT EXISTS ymes.tblwiperfocap_attachfile (
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

COMMENT ON TABLE ymes.tblwiperfocap_attachfile IS 'MES Oracle source: MESTEST.TBLWIPERFOCAP_ATTACHFILE; WP508 OCAP attachment';

-- WP213 source-structure reference. Compare PostgreSQL live schema/history before migration.
-- Source: MESTEST.TBLWIPMERGECONTENT_BIN; Oracle DBMS_METADATA and ALL_TAB_COLUMNS checked 2026-11-20.
CREATE TABLE IF NOT EXISTS ymes.tblwipmergecontent_bin (
    fromlotserial varchar(55) NOT NULL,
    frombin1 numeric(6,0) NOT NULL DEFAULT 0,
    frombin2 numeric(6,0) NOT NULL DEFAULT 0,
    frombin3 numeric(6,0) NOT NULL DEFAULT 0,
    frombin4 numeric(6,0) NOT NULL DEFAULT 0,
    frombin5 numeric(6,0) NOT NULL DEFAULT 0,
    frombin6 numeric(6,0) NOT NULL DEFAULT 0,
    frombin7 numeric(6,0) NOT NULL DEFAULT 0,
    frombin8 numeric(6,0) NOT NULL DEFAULT 0,
    tobin1 numeric(6,0) NOT NULL DEFAULT 0,
    tobin2 numeric(6,0) NOT NULL DEFAULT 0,
    tobin3 numeric(6,0) NOT NULL DEFAULT 0,
    tobin4 numeric(6,0) NOT NULL DEFAULT 0,
    tobin5 numeric(6,0) NOT NULL DEFAULT 0,
    tobin6 numeric(6,0) NOT NULL DEFAULT 0,
    tobin7 numeric(6,0) NOT NULL DEFAULT 0,
    tobin8 numeric(6,0) NOT NULL DEFAULT 0,
    frompassqty numeric(6,0) NOT NULL DEFAULT 0,
    fromfailqty numeric(6,0) NOT NULL DEFAULT 0,
    topassqty numeric(6,0) NOT NULL DEFAULT 0,
    tofailqty numeric(6,0) NOT NULL DEFAULT 0,
    tolotserial varchar(55) NOT NULL DEFAULT 'N/A',
    CONSTRAINT pk_tblwipmergecontent_bin PRIMARY KEY (fromlotserial)
);
COMMENT ON TABLE ymes.tblwipmergecontent_bin IS 'MES Oracle source: MESTEST.TBLWIPMERGECONTENT_BIN';

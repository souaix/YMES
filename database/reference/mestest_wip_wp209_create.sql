-- WP209 source-structure reference. Compare PostgreSQL live schema/history before migration.
-- Source: MESTEST.TBLWIPSPLITCONTENT_BIN. TBLWIPSORTERBINDATA was not visible; no guessed CREATE.
CREATE TABLE IF NOT EXISTS ymes.tblwipsplitcontent_bin (
    tolotserial varchar(55) NOT NULL,
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
    fromlotserial varchar(55) NOT NULL DEFAULT 'N/A',
    CONSTRAINT pk_tblwipsplitcontent_bin PRIMARY KEY (tolotserial)
);
COMMENT ON TABLE ymes.tblwipsplitcontent_bin IS 'MES Oracle source: MESTEST.TBLWIPSPLITCONTENT_BIN';

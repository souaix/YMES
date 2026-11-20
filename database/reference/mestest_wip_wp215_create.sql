-- PostgreSQL CREATE-only reference derived from Oracle MESTEST structural DDL.
-- Compare live YMES schema and migration history before making a forward migration.
-- TBLWIPCOMPONENTBASIS is used only when that table exists in the source deployment.

CREATE TABLE IF NOT EXISTS ymes.tblwipcomponentbasis (
    componentno varchar(50) NOT NULL,
    componentlotno varchar(50),
    slotno varchar(3),
    b_orgcomponentno varchar(50),
    b_materialno varchar(50),
    b_materiallotno varchar(50),
    mono varchar(50),
    s_aln_lotno varchar(50),
    s_cassetteno varchar(50),
    s_color varchar(100),
    s_cot_lotno varchar(50),
    s_cow_lotno varchar(50),
    s_cycleno varchar(100),
    s_cycleposition varchar(500),
    s_epi_lotno varchar(50),
    s_feedbackstate varchar(100),
    s_lasermark varchar(50),
    s_location numeric(4,0),
    s_mocvdeqpno varchar(100),
    s_orgcomponentlotno varchar(50),
    s_pss_lotno varchar(50),
    s_psv_lotno varchar(50),
    s_runno varchar(100),
    s_substrate_id varchar(100),
    s_wafercode varchar(50),
    s_waferfeedbackstatus varchar(2),
    s_wafersize varchar(100),
    p_bumpmergestatus numeric(1,0),
    p_waferid numeric(4,0),
    p_waferlot varchar(50),
    b_cassetteno varchar(50),
    g_lasermark varchar(50),
    CONSTRAINT pk_tblwipcomponentbasis PRIMARY KEY (componentno)
);

COMMENT ON TABLE ymes.tblwipcomponentbasis IS 'MES Oracle source: MESTEST.TBLWIPCOMPONENTBASIS';

CREATE TABLE IF NOT EXISTS ymes.tblwipreversehistory (
    lotno varchar(50) NOT NULL,
    reverseid numeric(6,0) NOT NULL,
    reversetype numeric(2,0) NOT NULL,
    lotserial varchar(55) NOT NULL,
    opno varchar(20) NOT NULL,
    commitstate numeric(1,0) NOT NULL DEFAULT 0,
    orglotno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    commitdesc varchar(255),
    CONSTRAINT pk_tblwipreversehistory PRIMARY KEY (lotno, reverseid)
);

CREATE INDEX IF NOT EXISTS i_tblwipreversehistory_01
    ON ymes.tblwipreversehistory (lotserial);

COMMENT ON TABLE ymes.tblwipreversehistory IS 'MES Oracle source: MESTEST.TBLWIPREVERSEHISTORY';

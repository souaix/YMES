BEGIN;

DO $$
DECLARE
    name text;
BEGIN
    FOREACH name IN ARRAY ARRAY[
        'tblqccalculatescript',
        'xx_tblqcitem_memo',
        'tblwiptemp_qcitem',
        'tblqcsamplingplan_wafer'
    ] LOOP
        IF EXISTS (
            SELECT 1
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relname = name
              AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
              AND n.nspname <> 'YMES'
        ) THEN
            RAISE EXCEPTION 'QC212 dependency % exists outside YMES; compare the live object before migration 0089', name;
        END IF;
        IF to_regclass(format('%I.%I', 'YMES', name)) IS NOT NULL THEN
            RAISE EXCEPTION 'QC212 dependency YMES.% already exists; compare its columns and grants before migration 0089', name;
        END IF;
    END LOOP;
END
$$;

-- Empty replicas only. Source business rows are not copied into YMES.
-- Source: mestest_qc205_variable_dependencies_source_ddl.sql.
CREATE TABLE "YMES".tblqccalculatescript (
    qcitemno varchar(25) NOT NULL,
    script varchar(255),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    groupno varchar(25) DEFAULT 'N/A' NOT NULL,
    CONSTRAINT pk_tblqccalculatescript PRIMARY KEY (qcitemno, groupno)
);
COMMENT ON TABLE "YMES".tblqccalculatescript IS 'MES Oracle source: MESTEST.TBLQCCALCULATESCRIPT; QC212 group-specific calculation scripts';

-- Oracle source has no PK/index; YMES uses the repository reference composite key
-- to make QCItemType=1 replace-set writes idempotent.
CREATE TABLE "YMES".xx_tblqcitem_memo (
    qcitemno varchar(50) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    memo_item varchar(50) NOT NULL,
    resvitor varchar(50) NOT NULL,
    "datetime" timestamp without time zone NOT NULL,
    CONSTRAINT pk_xx_tblqcitem_memo PRIMARY KEY (qcitemno, qcitemtype, memo_item)
);
COMMENT ON TABLE "YMES".xx_tblqcitem_memo IS 'MES Oracle source: MESTEST.XX_TBLQCITEM_MEMO; YMES key follows QC205/QC212 CREATE-only reference';

-- Oracle source has no PK/FK. Deletion checks every QCItemNo conservatively,
-- independent of legacy QCItemType encoding, before a group can be hard-deleted.
CREATE TABLE "YMES".tblwiptemp_qcitem (
    lotno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    lotserial varchar(55),
    executeflag numeric(1,0) NOT NULL,
    executeorder numeric(2,0) DEFAULT 0,
    reinspwith2ndinspplan numeric(1,0) DEFAULT 0,
    opno varchar(20),
    nodeid varchar(50),
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    executeflag_fai numeric(1,0) DEFAULT 0,
    executeflag_ipqc numeric(1,0) DEFAULT 0
);
CREATE INDEX i_tblwiptemp_qcitem_01 ON "YMES".tblwiptemp_qcitem (lotno, qcitemno);
CREATE INDEX i_tblwiptemp_qcitem_02 ON "YMES".tblwiptemp_qcitem (lotno);
COMMENT ON TABLE "YMES".tblwiptemp_qcitem IS 'MES Oracle source: MESTEST.TBLWIPTEMP_QCITEM; empty replica used by QC212 hard-delete guard';

-- QC212 requires only the Wafer plan header lookup. QC104 child tables are not
-- created here because they are outside this module and must be audited separately.
-- Source: mestest_qc_sampling_plan_wafer_source_ddl.sql and *_create.sql.
CREATE TABLE "YMES".tblqcsamplingplan_wafer (
    planno varchar(25) NOT NULL,
    sampletype numeric(1,0) NOT NULL,
    samplerate numeric(3,0) NOT NULL DEFAULT 0,
    everynum numeric(4,0) NOT NULL DEFAULT 0,
    samplepiecenum numeric(4,0) NOT NULL DEFAULT 0,
    samplepointnum numeric(6,0) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp without time zone,
    issuestate numeric(1,0) NOT NULL DEFAULT 0,
    functionname varchar(50),
    aqltype varchar(25),
    aql varchar(10),
    qclevel varchar(6),
    rejectqty numeric(4,0),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblqcsamplingplan_wafer PRIMARY KEY (planno)
);
COMMENT ON TABLE "YMES".tblqcsamplingplan_wafer IS 'MES Oracle source: MESTEST.TBLQCSAMPLINGPLAN_WAFER; QC212 approved Wafer plan lookup';

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemgroupbasis, "YMES".tblqcitemgroupdetail TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqccalculatescript, "YMES".xx_tblqcitem_memo TO ymes_app;
GRANT SELECT ON "YMES".tblwiptemp_qcitem, "YMES".tblqcsamplingplan_wafer TO ymes_app;

COMMIT;

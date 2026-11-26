BEGIN;

DO $$
DECLARE
    relation_name text;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname = 'ymes_app') THEN
        RAISE EXCEPTION 'ymes_app role is missing; stop before QC213 migration 0090';
    END IF;

    IF to_regclass('"YMES".tblqcitemattgrouporderbasis') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblqcitemattgrouporderbasis is missing; migration 0090 only uses the verified existing source table';
    END IF;
    IF to_regclass('"YMES".tblwiptemp_qcitem') IS NULL THEN
        RAISE EXCEPTION 'YMES.tblwiptemp_qcitem is missing; QC213 hard-delete guard cannot be deployed safely';
    END IF;

    IF (SELECT count(*) FROM information_schema.columns
        WHERE table_schema = 'YMES' AND table_name = 'tblqcitemattgrouporderbasis') <> 10
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='groupno' AND data_type='character varying' AND character_maximum_length=25 AND is_nullable='NO')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='groupname' AND data_type='character varying' AND character_maximum_length=50 AND is_nullable='NO')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='lotisvalid' AND data_type='numeric' AND numeric_precision=1 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='issuestate' AND data_type='numeric' AND numeric_precision=1 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='description' AND data_type='character varying' AND character_maximum_length=255 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='creator' AND data_type='character varying' AND character_maximum_length=10 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='createdate' AND data_type='timestamp without time zone' AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='datastamp' AND data_type='numeric' AND numeric_precision=11 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='reviser' AND data_type='character varying' AND character_maximum_length=10 AND is_nullable='YES')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='YMES' AND table_name='tblqcitemattgrouporderbasis' AND column_name='revisedate' AND data_type='timestamp without time zone' AND is_nullable='YES') THEN
        RAISE EXCEPTION 'YMES.tblqcitemattgrouporderbasis differs from verified QC213 source structure; migration 0090 stopped without changing it';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_catalog.pg_constraint
        WHERE conrelid=to_regclass('"YMES".tblqcitemattgrouporderbasis')
          AND contype='p' AND pg_get_constraintdef(oid)='PRIMARY KEY (groupno)'
    ) THEN
        RAISE EXCEPTION 'YMES.tblqcitemattgrouporderbasis primary key differs from QC213 source; migration 0090 stopped';
    END IF;

    FOREACH relation_name IN ARRAY ARRAY[
        'tblqcitemattgorderdetail', 'tblopgqcitemdetail', 'tblprdqcitem',
        'pk_tblqcitemattgorderdetail', 'pk_tblopgqcitemdetail', 'pk_tblprdqcitem'
    ] LOOP
        IF EXISTS (
            SELECT 1 FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE c.relname=relation_name AND c.relkind IN ('r','p','v','m','f') AND n.nspname <> 'YMES'
        ) THEN
            RAISE EXCEPTION 'Relation % already exists outside YMES; compare it before migration 0090', relation_name;
        END IF;
        IF to_regclass(format('%I.%I', 'YMES', relation_name)) IS NOT NULL THEN
            RAISE EXCEPTION 'YMES.% already exists; compare its columns, constraints, and data before migration 0090', relation_name;
        END IF;
    END LOOP;
END
$$;

-- QC213 direct detail and hard-delete dependencies absent from YMES. The
-- existing Attribute group basis and QCType=3 shared tables are preserved.
CREATE TABLE "YMES".tblqcitemattgorderdetail (
    groupno varchar(25) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    showorder numeric(3,0) DEFAULT 0,
    CONSTRAINT pk_tblqcitemattgorderdetail PRIMARY KEY (groupno, qcitemno)
);
COMMENT ON TABLE "YMES".tblqcitemattgorderdetail IS 'MES Oracle source: TBLQCITEMATTGORDERDETAIL; QC213 Attribute QC Item Group detail';
COMMENT ON COLUMN "YMES".tblqcitemattgorderdetail.showorder IS 'Group detail display order; source nullable DEFAULT 0';

CREATE TABLE "YMES".tblopgqcitemdetail (
    opno varchar(20) NOT NULL,
    groupno varchar(25) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    inline numeric(1,0) DEFAULT 1,
    executeorder numeric(2,0) DEFAULT 0,
    CONSTRAINT pk_tblopgqcitemdetail PRIMARY KEY (opno, qcitemno)
);
COMMENT ON TABLE "YMES".tblopgqcitemdetail IS 'MES Oracle source: TBLOPGQCITEMDETAIL; QC213 hard-delete dependency';

CREATE TABLE "YMES".tblprdqcitem (
    productno varchar(50) NOT NULL,
    qcitemno varchar(25) NOT NULL,
    qcitemtype numeric(1,0) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20) NOT NULL,
    executeorder numeric(2,0) NOT NULL DEFAULT 0,
    inline numeric(1,0) NOT NULL DEFAULT 1,
    insptype_fai numeric(1,0),
    insptype_ipqc numeric(1,0),
    insptype_pqc numeric(1,0),
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblprdqcitem PRIMARY KEY (productno, productversion, opno, qcitemno)
);
COMMENT ON TABLE "YMES".tblprdqcitem IS 'MES Oracle source: TBLPRDQCITEM; QC213 hard-delete dependency';

GRANT INSERT, UPDATE, DELETE ON "YMES".tblqcitemattgrouporderbasis TO ymes_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblqcitemattgorderdetail TO ymes_app;
GRANT DELETE ON "YMES".tblopgqcitemdetail, "YMES".tblprdqcitem TO ymes_app;

COMMIT;

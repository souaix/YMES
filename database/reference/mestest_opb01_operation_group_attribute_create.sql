-- PostgreSQL CREATE-only reference adapted from MESTEST.TBLOPGROUPATTRIB.
-- Not a migration or a production deployment script. Compare YMES live schema first.
-- The shared tblsyselementbasis is already provisioned by first-phase schema; do not recreate it here.
CREATE SCHEMA IF NOT EXISTS "YMES";

CREATE TABLE IF NOT EXISTS "YMES"."tblopgroupattrib" (
    "customerno" varchar(20) NOT NULL,
    "opgroupno" varchar(20) NOT NULL,
    "elementno" varchar(20) NOT NULL,
    "elementname" varchar(50) NOT NULL,
    "datatype" numeric(1, 0) NOT NULL,
    "allownull" numeric(1, 0) NOT NULL,
    "defaultvalue" varchar(255),
    "sequence" numeric(2, 0) NOT NULL,
    "reviser" varchar(10),
    "revisedate" timestamp without time zone,
    "creator" varchar(10) NOT NULL,
    "createdate" timestamp without time zone NOT NULL,
    "bysecondclass" numeric(1, 0) DEFAULT 0,
    "datastamp" numeric(11, 0) DEFAULT 0,
    CONSTRAINT "pk_tblopgroupattrib" PRIMARY KEY ("customerno", "opgroupno", "elementno")
);

COMMENT ON TABLE "YMES"."tblopgroupattrib" IS 'MES master data; Oracle source: MESTEST.TBLOPGROUPATTRIB';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."customerno" IS '客戶編號；* 代表共用設定';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."opgroupno" IS 'ECN 作業站群組編號';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."elementno" IS 'Element 編號';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."elementname" IS 'Element 名稱快照';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."datatype" IS '資料型態代碼';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."allownull" IS '是否允許空值';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."defaultvalue" IS '預設值';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."sequence" IS '顯示次序';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."bysecondclass" IS 'BYSECONDCLASS；WinForms 顯示名 BYEQUIPMENT_TYPE';
COMMENT ON COLUMN "YMES"."tblopgroupattrib"."datastamp" IS '並行更新版本';

-- Preserve Oracle null/default semantics: BYSECONDCLASS and DATASTAMP are nullable DEFAULT 0.
-- Oracle source has only the composite PK; there is no FK or additional index.

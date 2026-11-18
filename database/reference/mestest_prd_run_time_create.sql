-- PostgreSQL CREATE-only reference for PD102 RunTime. No data or migration.
-- Compare with YMES live schema before authoring a forward migration.
-- The Oracle source declares no primary key or indexes for this table.
CREATE TABLE IF NOT EXISTS "YMES".tblprdruntimesetup (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    areano varchar(20) NOT NULL,
    opno varchar(20) NOT NULL,
    equipmentno varchar(50),
    equipmenttype varchar(50),
    stdunitemptime numeric(6,2) NOT NULL DEFAULT 0,
    stduniteqptime numeric(6,2) NOT NULL DEFAULT 0,
    counteqpunitqty numeric(6,0) NOT NULL DEFAULT 1,
    stdunitruntime numeric(6,2) NOT NULL DEFAULT 0,
    countopunitqty numeric(6,0) NOT NULL DEFAULT 1,
    stdqueuetime numeric(6,2) NOT NULL DEFAULT 0,
    creator varchar(10),
    createdate timestamp(0) without time zone,
    fixemptime numeric(6,2) DEFAULT 0,
    varemptime numeric(6,2) DEFAULT 0,
    fixeqptime numeric(6,2) DEFAULT 0,
    vareqptime numeric(6,2) DEFAULT 0,
    workpricetype numeric(1,0) DEFAULT 1,
    workprice numeric(6,2)
);

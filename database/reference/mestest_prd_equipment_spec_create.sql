-- PostgreSQL CREATE-only comparison for PD102 EquipmentSpec.
-- Reuse the existing equipment parameter catalog.
CREATE TABLE IF NOT EXISTS "YMES".tblprdequipmentspec (
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    equipmentno varchar(50) NOT NULL,
    parameterno varchar(20) NOT NULL,
    minvalue numeric(12,6) NOT NULL,
    maxvalue numeric(12,6) NOT NULL,
    guideline_1 numeric(12,6) NOT NULL,
    guideline_2 numeric(12,6) NOT NULL,
    reviser varchar(10) NOT NULL,
    revisedate timestamp(0) without time zone NOT NULL,
    CONSTRAINT pk_tblprdequipmentspec PRIMARY KEY (productno, productversion, equipmentno, parameterno)
);

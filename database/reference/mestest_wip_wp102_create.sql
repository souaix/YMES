-- PostgreSQL CREATE-only reference generated from MESTEST metadata on 2026-11-19.
-- Empty-schema reference only. Compare YMES live schema and migrations before applying.
-- WP102 adds only this staging table; tblwiplotproperty is documented in WP101.
-- Oracle NVARCHAR2 lengths map to PostgreSQL varchar lengths.

CREATE SCHEMA IF NOT EXISTS ymes;

CREATE TABLE IF NOT EXISTS ymes.tblwiplotproperty_temp (
    lotno varchar(50) NOT NULL,
    propertyno varchar(20) NOT NULL,
    propertyvalue varchar(255),
    lotserial varchar(55),
    CONSTRAINT pk_tblwiplotproperty_temp PRIMARY KEY (lotno, propertyno)
);

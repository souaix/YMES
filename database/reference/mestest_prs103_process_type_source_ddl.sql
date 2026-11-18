-- Oracle source DDL captured via direct read-only PowerShell 5.1 + Oracle.ManagedDataAccess.
-- Source object: MESTEST.TBLPRSTYPE. Only dictionary metadata was read; no business rows/DDL/DML.
-- Credentials are not included.

CREATE TABLE "MESTEST"."TBLPRSTYPE"
   (    "PROCESSTYPE" VARCHAR2(20) CONSTRAINT "N_TBLPRSTYPE_01" NOT NULL ENABLE,
        "DESCRIPTION" VARCHAR2(255),
        "CREATOR" VARCHAR2(10),
        "CREATEDATE" DATE,
        "ISSUESTATE" NUMBER(1,0) DEFAULT 0 CONSTRAINT "N_TBLPRSTYPE_02" NOT NULL ENABLE,
        "DATASTAMP" NUMBER(11,0) DEFAULT 0,
        "REVISER" VARCHAR2(10),
        "REVISEDATE" DATE,
         CONSTRAINT "PK_TBLPRSTYPE" PRIMARY KEY ("PROCESSTYPE")
  USING INDEX ENABLE
   ) ;

-- Metadata: PK_TBLPRSTYPE is the only unique index; no foreign keys or other indexes.

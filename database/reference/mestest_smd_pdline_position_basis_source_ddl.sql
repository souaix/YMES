-- Oracle MESTEST structure verified by read-only PowerShell 5.1 + Oracle.ManagedDataAccess metadata query.
-- These are normalized logical CREATE statements. Oracle physical storage/TABLESPACE clauses are omitted.
-- No MES business rows were queried or copied.

CREATE TABLE "MESTEST"."TBLSMDPDLINEPOSITIONBASIS" (
    "PDLINENO" VARCHAR2(50) NOT NULL,
    "POSITIONNO" VARCHAR2(50) NOT NULL,
    "DESCRIPTION" VARCHAR2(50),
    "CREATOR" VARCHAR2(10) NOT NULL,
    "CREATEDATE" DATE DEFAULT SYSDATE NOT NULL,
    "REVISER" VARCHAR2(10),
    "REVISEDATE" DATE,
    "DATASTAMP" NUMBER(11,0) DEFAULT 0,
    CONSTRAINT "PK_TBLSMDPDLINEPOSITIONBASIS" PRIMARY KEY ("PDLINENO", "POSITIONNO")
);

CREATE UNIQUE INDEX "MESTEST"."PK_TBLSMDPDLINEPOSITIONBASIS"
    ON "MESTEST"."TBLSMDPDLINEPOSITIONBASIS" ("PDLINENO", "POSITIONNO");

-- Dictionary metadata showed only the primary-key unique index; no foreign keys or other indexes.

-- PREPARED SOURCE; NOT APPLIED. Root reviews the full private database backup,
-- all-schema/type absence guards and independent readback before execution.
-- Exact native ET102 source: thirteen columns; ContactorName physically first,
-- CustomerNo second; PK(CustomerNo,ContactorName). Title/DeptName are varchar25.
-- Native audit/date/stamp nullability and DataStamp DEFAULT0 remain unchanged.
-- No parent ALTER/GRANT, FK, extra index, state, workflow, seed or table rebuild.
BEGIN;

CREATE TABLE "YMES".tblentcustomercontactor (
    contactorname varchar(50) NOT NULL,
    customerno varchar(50) NOT NULL,
    telno varchar(25),
    faxno varchar(25),
    email varchar(50),
    title varchar(25),
    deptname varchar(25),
    creator varchar(10),
    createdate timestamp without time zone,
    description varchar(255),
    datastamp numeric(11,0) DEFAULT 0,
    reviser varchar(10),
    revisedate timestamp without time zone,
    CONSTRAINT pk_tblentcustomercontactor PRIMARY KEY (customerno, contactorname)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON "YMES".tblentcustomercontactor TO ymes_app;

COMMIT;

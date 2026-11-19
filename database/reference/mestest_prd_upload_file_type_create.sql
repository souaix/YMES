-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tbl_bprdmesfiletype (
    filetype numeric(5, 0) NOT NULL,
    fileservicetype numeric(1, 0) NOT NULL,
    fileservicehost varchar(255) NOT NULL,
    fileserviceusername varchar(50) NOT NULL,
    fileservicepassword varchar(50) NOT NULL,
    fileserviceport numeric(22, 0) NOT NULL,
    filetypepathpatten varchar(255) NOT NULL,
    filetypedescription varchar(255),
    issuestate numeric(1, 0) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tbl_bprdmesfiletype PRIMARY KEY (filetype)
);

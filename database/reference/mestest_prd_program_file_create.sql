-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tbl_bprdmesfilemanagement (
    filetype numeric(5, 0) NOT NULL,
    filename text NOT NULL,
    filecount numeric(3, 0) NOT NULL,
    folderno varchar(100) NOT NULL,
    filepath varchar(255) NOT NULL,
    fileno varchar(70) NOT NULL,
    filenoversion varchar(70) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    memo varchar(255),
    CONSTRAINT pk_tbl_bprdmesfilemanagement PRIMARY KEY (filetype, fileno, filenoversion)
);

CREATE TABLE IF NOT EXISTS "YMES".tbl_bprdmesfilemgt_log (
    filetype numeric(5, 0) NOT NULL,
    filename text NOT NULL,
    filecount numeric(3, 0) NOT NULL,
    folderno varchar(100) NOT NULL,
    filepath varchar(255) NOT NULL,
    fileno varchar(70) NOT NULL,
    filenoversion varchar(70) NOT NULL,
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    memo varchar(255),
    CONSTRAINT pk_tbl_bprdmesfilemgt_log PRIMARY KEY (filetype, fileno, filenoversion, createdate)
);

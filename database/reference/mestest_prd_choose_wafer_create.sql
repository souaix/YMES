-- PostgreSQL CREATE only. Use the established YMES schema.

CREATE TABLE IF NOT EXISTS "YMES".tblprdchoosewaferrules (
    psno varchar(20) NOT NULL,
    choosewaferruleno varchar(50) NOT NULL,
    choosewaferrulename varchar(100) NOT NULL,
    refproducttype varchar(50),
    refproductno varchar(50),
    assinginventoryno varchar(4000) NOT NULL,
    assingvendorno varchar(4000),
    assingscript text,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    issuestate numeric(1, 0) NOT NULL,
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    datastamp numeric(11, 0),
    CONSTRAINT pk_tblprdchoosewaferrules PRIMARY KEY (choosewaferruleno)
);

CREATE TABLE IF NOT EXISTS "YMES".tblsysmochoosewafertable (
    functionno varchar(50) NOT NULL,
    assinginventoryno varchar(4000) NOT NULL,
    assingvendorno varchar(4000),
    assingtablename varchar(4000),
    psno varchar(50) NOT NULL,
    mustchoosewafer numeric(1, 0) NOT NULL,
    moclass varchar(20) NOT NULL,
    description varchar(255),
    creator varchar(10) NOT NULL,
    createdate timestamp(0) without time zone NOT NULL,
    mustsamecassette numeric(1, 0),
    usedoubleunit varchar(1),
    datastamp numeric(11, 0),
    reviser varchar(10),
    revisedate timestamp(0) without time zone,
    CONSTRAINT pk_tblsysmochoosewafertable PRIMARY KEY (functionno)
);

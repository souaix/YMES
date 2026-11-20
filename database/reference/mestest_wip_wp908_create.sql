-- CREATE-only from Oracle dictionary. Confirm source defaults and VARCHAR2 BYTE limits.
-- LABELCONTENT maps CLOB to text and remains mandatory.
CREATE TABLE IF NOT EXISTS ymes.tblwiplabelprintlog (
    labelprintid varchar(50) NOT NULL,
    labelsource numeric(2,0) NOT NULL,
    labeltype varchar(25) NOT NULL,
    labelno varchar(50) NOT NULL,
    mono varchar(50) NOT NULL,
    productno varchar(50) NOT NULL,
    productversion varchar(5) NOT NULL,
    opno varchar(20),
    lotno varchar(50) NOT NULL,
    printkeyfield varchar(50) NOT NULL,
    printkeyvalue varchar(100) NOT NULL,
    printuserno varchar(10) NOT NULL,
    printcomputername varchar(50) NOT NULL,
    printapplication varchar(100) NOT NULL,
    printdate timestamp(0) without time zone NOT NULL,
    printlabelqty numeric(3,0) NOT NULL,
    labelcontent text NOT NULL,
    reprintstatus numeric(1,0) NOT NULL,
    CONSTRAINT pk_tblwiplabelprintlog PRIMARY KEY (labelprintid)
);

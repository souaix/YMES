-- PostgreSQL CREATE-only comparison. Check the live schema and label basis first.
CREATE TABLE IF NOT EXISTS "YMES".tblprdlabeldetail (
    labelno varchar(50) NOT NULL,
    position numeric(2,0) NOT NULL,
    piece numeric(2,0) NOT NULL,
    piecesource varchar(10) NOT NULL,
    piecedata varchar(50) NOT NULL,
    direction numeric(2,0) NOT NULL,
    startindex numeric(2,0),
    piecelength numeric(2,0),
    memo varchar(255),
    itemname varchar(50),
    datascript text,
    CONSTRAINT pk_tblprdlabeldetail PRIMARY KEY (labelno, position, piece)
);

-- WP901 CREATE-only reference derived from MESTEST dictionary metadata.
-- No P/U/R constraints were observed. Confirm defaults and byte/char semantics on live schema.
CREATE TABLE IF NOT EXISTS ymes.tblwipreversebatch (
    batchserial varchar(20) NOT NULL,
    lotno varchar(50) NOT NULL,
    reverseid numeric(6,0) NOT NULL,
    lotserial varchar(55) NOT NULL
);

CREATE TABLE IF NOT EXISTS ymes.tblwipreverselog (
    lotno varchar(50) NOT NULL,
    reverseid numeric(6,0) NOT NULL,
    reversetype numeric(2,0) NOT NULL,
    lotserial varchar(55) NOT NULL,
    opno varchar(20) NOT NULL,
    orglotno varchar(50),
    description varchar(255),
    creator varchar(10),
    createdate timestamp(0) without time zone,
    executor varchar(10) NOT NULL,
    executedate timestamp(0) without time zone NOT NULL,
    executedesc varchar(255),
    reversedescription varchar(255),
    revesedescription varchar(255)
);

CREATE TABLE IF NOT EXISTS ymes.tblwipreverseuncommit (
    lotserial varchar(55) NOT NULL,
    conditionstring varchar(450) NOT NULL
);

CREATE INDEX IF NOT EXISTS i_tblwipreversebatch_01 ON ymes.tblwipreversebatch (batchserial);
CREATE INDEX IF NOT EXISTS i_tblwipreverseuncommit_01 ON ymes.tblwipreverseuncommit (lotserial);
CREATE INDEX IF NOT EXISTS i_tblwipreverseuncommit_02 ON ymes.tblwipreverseuncommit (conditionstring);

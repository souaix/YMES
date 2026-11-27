BEGIN;

REVOKE SELECT ON "YMES".tblwip8d_action,
                 "YMES".tblwip8d_actiondetail,
                 "YMES".tblwip8d_disposition,
                 "YMES".tblwip8d_prevention,
                 "YMES".tblwip8d_preventiontrack,
                 "YMES".tblwip8d_qa,
                 "YMES".tblwip8d_question,
                 "YMES".tblwip8d_rootcause FROM ymes_app;
REVOKE SELECT (qrsno, stageno, attachfilename, creator, createdate)
    ON "YMES".tblwip8d_attachfile FROM ymes_app;

DROP TABLE "YMES".tblwip8d_action;
DROP TABLE "YMES".tblwip8d_actiondetail;
DROP TABLE "YMES".tblwip8d_attachfile;
DROP TABLE "YMES".tblwip8d_disposition;
DROP TABLE "YMES".tblwip8d_prevention;
DROP TABLE "YMES".tblwip8d_preventiontrack;
DROP TABLE "YMES".tblwip8d_qa;
DROP TABLE "YMES".tblwip8d_question;
DROP TABLE "YMES".tblwip8d_rootcause;

COMMIT;

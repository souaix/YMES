BEGIN;

INSERT INTO "YMES".tblsysmodule (moduleno, moduleorder, datastamp, creator, createdate)
VALUES
    ('EQP', 10, 1, 'ymes', now()),
    ('OP', 20, 1, 'ymes', now())
ON CONFLICT (moduleno) DO UPDATE
SET moduleorder = EXCLUDED.moduleorder;

INSERT INTO "YMES".tblsysmenuitem
    (moduleno, menuno, menuname, menutype, menuorder, parentmenuno, activemodule)
VALUES
    ('EQP', 'EQP', '設備管理', 1, 1, '', 'EQP'),
    ('EQP', 'EQP BASIS', '設備主檔', 0, 1, 'EQP', 'EQP'),
    ('OP', 'OP', '作業站管理', 1, 1, '', 'OP'),
    ('OP', 'OP BASIS', '作業站主檔', 0, 1, 'OP', 'OP')
ON CONFLICT (moduleno, menuno) DO UPDATE
SET menuname = EXCLUDED.menuname,
    menutype = EXCLUDED.menutype,
    menuorder = EXCLUDED.menuorder,
    parentmenuno = EXCLUDED.parentmenuno,
    activemodule = EXCLUDED.activemodule;

INSERT INTO "YMES".tblsysfunction
    (functionno, functionname, functionorder, moduleno, weburl, issuetable, activemodule)
VALUES
    ('EQP BASIS', '設備主檔', 1, 'EQP', '/YMES/Equipment', 'tbleqpequipmentbasis', 'EQP'),
    ('OP BASIS', '作業站主檔', 1, 'OP', '/YMES/MasterData/Operations', 'tblopbasis', 'OP')
ON CONFLICT (functionno) DO UPDATE
SET functionname = EXCLUDED.functionname,
    functionorder = EXCLUDED.functionorder,
    moduleno = EXCLUDED.moduleno,
    weburl = EXCLUDED.weburl,
    issuetable = EXCLUDED.issuetable,
    activemodule = EXCLUDED.activemodule;

COMMIT;

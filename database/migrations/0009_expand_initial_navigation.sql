BEGIN;

INSERT INTO "YMES".tblsysmodule (moduleno, moduleorder, datastamp, creator, createdate)
VALUES
    ('MTL', 30, 1, 'ymes', now()),
    ('PRS', 40, 1, 'ymes', now()),
    ('PRD', 50, 1, 'ymes', now())
ON CONFLICT (moduleno) DO UPDATE
SET moduleorder = EXCLUDED.moduleorder;

INSERT INTO "YMES".tblsysmenuitem
    (moduleno, menuno, menuname, menutype, menuorder, parentmenuno, activemodule)
VALUES
    ('MTL', 'MTL', '物料管理', 1, 1, '', 'MTL'),
    ('MTL', 'MTL BASIS', '物料主檔', 0, 1, 'MTL', 'MTL'),
    ('PRS', 'PRS', '流程管理', 1, 1, '', 'PRS'),
    ('PRS', 'PRS BASIS', '流程主檔', 0, 1, 'PRS', 'PRS'),
    ('PRD', 'PRD', '產品管理', 1, 1, '', 'PRD'),
    ('PRD', 'PRD BASIS', '產品主檔', 0, 1, 'PRD', 'PRD')
ON CONFLICT (moduleno, menuno) DO UPDATE
SET menuname = EXCLUDED.menuname,
    menutype = EXCLUDED.menutype,
    menuorder = EXCLUDED.menuorder,
    parentmenuno = EXCLUDED.parentmenuno,
    activemodule = EXCLUDED.activemodule;

INSERT INTO "YMES".tblsysfunction
    (functionno, functionname, functionorder, moduleno, weburl, issuetable, activemodule)
VALUES
    ('MTL BASIS', '物料主檔', 1, 'MTL', '/YMES/MasterData/Materials', 'tblmtlmaterialbasis', 'MTL'),
    ('PRS BASIS', '流程主檔', 1, 'PRS', '/YMES/MasterData/Processes', 'tblprsprocessbasis', 'PRS'),
    ('PRD BASIS', '產品主檔', 1, 'PRD', '/YMES/MasterData/Products', 'tblprdproductbasis', 'PRD')
ON CONFLICT (functionno) DO UPDATE
SET functionname = EXCLUDED.functionname,
    functionorder = EXCLUDED.functionorder,
    moduleno = EXCLUDED.moduleno,
    weburl = EXCLUDED.weburl,
    issuetable = EXCLUDED.issuetable,
    activemodule = EXCLUDED.activemodule;

COMMIT;

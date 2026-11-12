BEGIN;

UPDATE "YMES".tblsysmenuitem
SET menuname = CASE moduleno
    WHEN 'EQP' THEN '設備設定'
    WHEN 'OP' THEN '作業站設定'
    WHEN 'MTL' THEN '物料設定'
    WHEN 'PRS' THEN '流程設定'
    WHEN 'PRD' THEN '產品設定'
END
WHERE menutype = 1
  AND moduleno IN ('EQP', 'OP', 'MTL', 'PRS', 'PRD');

UPDATE "YMES".tblsysmenuitem
SET menuname = CASE menuno
    WHEN 'EQP BASIS' THEN '設備基本資料'
    WHEN 'OP BASIS' THEN '作業站主檔'
    WHEN 'MTL BASIS' THEN '物料基本資料'
    WHEN 'PRS BASIS' THEN '流程主檔'
    WHEN 'PRD BASIS' THEN '產品主檔'
END
WHERE menutype = 0
  AND menuno IN ('EQP BASIS', 'OP BASIS', 'MTL BASIS', 'PRS BASIS', 'PRD BASIS');

UPDATE "YMES".tblsysfunction
SET functionname = CASE functionno
    WHEN 'EQP BASIS' THEN '設備基本資料'
    WHEN 'OP BASIS' THEN '作業站主檔'
    WHEN 'MTL BASIS' THEN '物料基本資料'
    WHEN 'PRS BASIS' THEN '流程主檔'
    WHEN 'PRD BASIS' THEN '產品主檔'
END
WHERE functionno IN ('EQP BASIS', 'OP BASIS', 'MTL BASIS', 'PRS BASIS', 'PRD BASIS');

COMMIT;

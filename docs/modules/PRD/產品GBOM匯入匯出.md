# 產品 GBOM 匯入與匯出（PD102）

## WinForms、Service 與 Core

- `Customer/Production/MESWin/PRD/PD_ProductBasis_THEIL_BU1/frmProductBasis_THEIL_BU1.vb` 內含按鈕 Designer 和 `btnGBOMImport_Click`、`btnGBOMExport_Click`。匯入限 `IssueState=0`，使用單一 `.xls`／`.xlsx`，檔名主體為 `ProductNo-ProductVersion`，比較時忽略大小寫。
- WinForms 用 NPOI 讀第一張工作表。`A2` 和 `B2` 都須等於檔名主體，`版次REV` 須等於產品版本。後續依欄名讀共用資料，將作業、材料、參數和連線組成匯入資料。流程版本指定 `01`。
- `Customer/Production/wsTHEIL/wsPRD/wsPRD.cs` 的 `GBOMImport` 把工作表資料交給 `Customer/Production/MEStc_THEIL/tcPRD/clsProduct.cs`。Core 可建立流程主檔、替換流程節點／連線、指派產品流程、更新產品主檔及 BOM，並依作業與系統參數更新產品相關設定。
- 資料匯入成功後，WinForms 呼叫 `UpdGBOMExcelFile` 備份原始 bytes。Core 以 `GBOMPath` 儲存並替換同名 `.xls`／`.xlsx`。匯出以兩個產品鍵識別備份，讀回原始 bytes，按檔案格式決定副檔名。

## 公司端 DEV

- 匯入有產品版本狀態、檔名、原始檔案標頭檢查。XLSX 另檢查第一張工作表的識別列與 `版次REV`。確認後在本次頁面記憶體保存 `File`，按產品版本隔離，同版重新匯入會替換備份。匯出下載保存的原檔，無備份時顯示空狀態。重新整理或離開產品主檔後重置。
- DEV 顯示備份結果。舊式 `.xls` 的工作表驗證、Excel 公式重算、流程圖與表資料更新待正式匯入流程。

## Oracle 字典與家用端 API/schema

- PowerShell 5.1 + ODP.NET 唯讀確認 `XX_TBLPRDPRARKEY_THEIL_BU1`（4 欄，無主鍵）與 `XX_TBLOVERSETUP_THEIL_BU1`（15 欄，複合主鍵含前後作業和 Hold 條件）。附 [Oracle 邏輯 DDL](../../../database/reference/mestest_prd_gbom_source_ddl.sql) 與 [PostgreSQL CREATE-only 比對](../../../database/reference/mestest_prd_gbom_create.sql)。共用流程與節點使用 [PS104 參考](../../../database/reference/mestest_prs104_process_basis_create.sql)，RunTime 和其他產品子表對照現有規格與 live schema。
- Core 的 `XX_TBLWIPINFORECORD` 是 INVALID view，MESTEST 字典回 `UNDEFINED` 欄型。PP02／PP05 的前後段資訊記錄查詢需先確認有效 view 或正式替代來源，CREATE 參考暫缺。
- 建議 `POST /api/prd/products/{productNo}/versions/{productVersion}/gbom` 收 multipart Excel。伺服器驗證狀態、檔名、檔案格式、工作表、版本與資料列，再用預期的資料戳核對併發。流程、BOM 與衍生設定一起提交。處理檔案儲存失敗及重試，避免資料已更新但匯出仍是舊版本。
- 建議 `GET /api/prd/products/{productNo}/versions/{productVersion}/gbom/file` 從版本鍵取備份，以對應 MIME 與下載檔名回傳原始 bytes。未匯入回 404，產品與檔案皆依權限檢查。

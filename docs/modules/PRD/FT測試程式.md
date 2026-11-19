# PD202 FT 測試程式

- `PRD / FT PROGRAM BASIS`，Web `/YMES/PRD/FTPrograms`。來源 `STD/MESWin/PRD/PD_FTProgramBasis` 的 `frmFTProgramBasis`、`frmFTProgramBasisDef`、`frmFTProgramSWBinImport`、`frmProgramUpload`（含內嵌 Designer）。硬體 Bin 另由 `PD_FTProgramBin` 視窗處理。
- 主檔依測試程式編號、版次和狀態查詢。未核准可編輯、匯入檔案及核准，已核准目前版可開新版，複製可帶入 Bin。刪除傳送狀態與 DataStamp。啟用軟體 Bin 管理時，核准前要有能連到硬體 Bin 的軟體 Bin。
- 軟體 Bin 文字檔第一列為程式資訊，第二列 `Good:` 為逗號分隔的硬體 Bin，第三列指定 `TestPlan_Name Software_Bin Hardware_Bin`，第四列起逐列讀取，於 Bin 編號前加 `BIN`。硬體 Bin 決定 Good 或 Fail，未在明細出現的 Good Bin 補入。輸入明細禁止重複 TestPlan_Name。
- DEV 可 CRUD、複製、開版、核准、預覽並匯入 Bin，啟用軟體 Bin 時維持核准卡控。核准直接切至本機目前版。硬體 Bin 編輯採編號、結果及說明；正式系統 Bin 目錄、OS Bin、Rank、DGrade、舊版文字檔編碼與完整匯入驗證待接續。檔案操作只存檔名，正式程式檔內容、資料夾與上傳授權由檔案服務承接。

## Oracle 與家用端交接

- PowerShell 5.1 + ODP.NET 唯讀核對 `TBLPRDFTPROGRAMBASIS` 12 欄，主鍵 `(PROGRAMNO, PROGRAMVERSION)`；`TBLPRDFTPROGRAMSWBIN` 6 欄，主鍵 `(PROGRAMNO, PROGRAMVERSION, TESTPLAN_NAME, SWBINNO)`。`TBLPRDFTPROGRAMBIN` 8 欄與既有硬體 Bin 交接一致。參閱 [FT Oracle 邏輯 DDL](../../../database/reference/mestest_prd_ft_program_source_ddl.sql)、[FT CREATE-only](../../../database/reference/mestest_prd_ft_program_create.sql) 與 [硬體 Bin CREATE](../../../database/reference/mestest_prd_bin_create.sql)。
- 原標準 `wsPRD.cs` 的 Load／Add／Edit／Copy／Approve／DelFTProgram 對應 Core `STD/MEStc/tcPRD_IC/clsProgram.cs`。Bin 使用 `LoadFTProgramSWBinJoinHWBin` 和 `ImportFTProgramBin`，檔案類型為 51，透過 `LoadFile`、`UpdFileManagement` 和 FolderNo 關聯。
- 建議 `/api/prd/ft-programs` 提供查詢、版次 CRUD、複製、核准、硬體 Bin 和軟體 Bin 預覽／匯入。寫入以程式號／版次和預期 DataStamp 驗證；Bin 取代、複製及核准時目前版切換維持交易一致性。啟用簽核時依正式流程處理簽核中、核准及駁回，另核對刪除相依與檔案服務。

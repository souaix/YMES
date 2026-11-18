# 產品 Bin 定義（PD102）

## WinForms、Service 與 Core

- `Customer/Production/MESWin/PRD/PD_ProductBasis_THEIL_BU1/frmProductBasis_THEIL_BU1.vb` 的 `btnBin_Click` 將選定的 `ProductNo`、`ProductVersion`、`IssueState` 傳給 `PD_FTProgramBin`。產品兩個鍵分別對應後端的 `ProgramNo`、`ProgramVersion`。
- `STD/MESWin/PRD/PD_FTProgramBin/frmFTProgramBin.vb` 含 Form 與 Designer。`funLoadBinDefine` 依 `BinOrder` 排序系統清單，以 `BinNo` 合併已有設定。預設未勾選、結果 Good、OS Bin 為 No。`DGrade` 和 `Rank` 在 Grid 隱藏，但從已有設定載入並帶回儲存。`IssueState=0` 才能確認。
- `STD/MESws_STD/wsSTD/wsPRD/wsPRD.cs` 的 `LoadFTProgramBinDefine` 組合 `clsSYSBasis_IC.LoadBin` 和 `clsProgram.LoadFTProgramBin`。資料分別來自 `TBLSYSBIN` 和 `TBLPRDFTPROGRAMBIN`。`UpdFTProgramBin` 接收選取列，呼叫 `STD/MEStc/tcPRD_IC/clsProgram.cs` 的同名方法，依兩個產品鍵刪除舊列並插入傳入列。空清單清除該版本全部定義。
- 此入口的 load/save 使用硬體 Bin 資料。`TBLPRDFTPROGRAMSWBIN` 出現在另一個 `CopyFTProgramBin` 方法，超出此視窗的儲存範圍。

## 公司端 DEV

- 使用八筆固定的系統 Bin 假資料。Vue 從產品版本的本機假資料補入已儲存設定，顯示選取、BinNo、Good/Fail、說明和 OS Bin。已核准及其他非 0 狀態可查看。
- Good/Fail 對應 `BinValue=0/1`，No/Yes 對應 `OSBin=0/1`。原 Grid 的 `BeforeCellUpdate` 使用舊儲存格值判斷。DEV 依訊息表達的規則限制 Good 搭配 Yes，並在確認時檢查最終組合。
- 確認只儲存勾選列，保留隱藏的 `DGrade`、`Rank`。取消或關閉捨棄暫存編輯。

## 家用端 API 與 schema 交接

- 建議 `GET /api/prd/products/{productNo}/versions/{productVersion}/bins` 回傳 `issueState`、依 `binOrder` 排序的 `catalog`（`binNo`、`binOrder`）和 `assignments`（`binNo`、`binValue`、`dGrade`、`rank`、`osBin`、`description`）。維持 `BinNo` 字串，保留前導零。
- 建議同路徑 `PUT` 接收 `assignments`，驗證產品狀態、權限、重複或未定義的 BinNo 和 Good/OS Bin 規則。以交易替換該版本全部設定，包含空清單；查驗及寫入須一起處理併發，成功後記錄產品鍵與操作者。
- 2026-11-19 的 Oracle 查詢在 Codex 自動審查服務回傳 HTTP 503 時中止，未執行查詢。兩張表的欄位型別、長度、NULL/default、鍵、索引和 trigger 待查。上述欄位來自 Form 與 Core 的讀寫欄位，CREATE SQL 待 Oracle 字典確認後補齊。

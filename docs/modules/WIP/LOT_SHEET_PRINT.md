# LOT SHEET 列印（WP113）

## 功能識別與來源

| 項目 | 來源 |
|---|---|
| Menu Function | `LOT SHEET PRINT`，FunctionKey `WP113`，模組 WIP |
| WinForms | `Customer/Production/MESWin/WIP/WP_BRRunCard_General_THEIL_BU1/frmBRRunCard_General_THEIL_BU1.vb` 及同目錄 Designer |
| Service | `Customer/Production/wsTHEIL/wsWIP/wsWIP_Runcard.cs`：`LoadLotBasisJoinState`、`UpdatePrintCount`、`LoadRunCardData_Empty` |
| Core | `Customer/Production/MEStc_THEIL/tcWIP/clsRuncard.cs`：批次查詢、流程卡資料、THEIL Matrix、處置／使用者資料與列印次數 |
| 報表 | `frmBRRunCard_General_THEIL_BU1/Reports/arRunCard_General_THEIL_BU1`、`arRunCard_General_Empty_THEIL_BU1` |
| DEV 頁 | `/YMES/WIP/LotSheetPrint`，`web/src/components/WipLotSheetPrint.vue` |

WP113 是 THEIL 客製列印頁，不可套用一般 WP103 的查詢欄位、狀態過濾或報表版型。

## WinForms 畫面與查詢

- 上方 `Query` 區有 MO No 與 lookup、`WO Create` 勾選、From／To 日期時間及 Query。`Form_Load` 不執行 Lot 查詢；預設時間區間起點為最近一次早上 07:30，終點為目前時間。
- 查詢至少需 MO No 或啟用 WO Create 日期條件。日期條件需有起訖且起點早於終點；Core 依 MO 建立時間篩選。DEV 假資料頁保留此檢核，沒有登入正式 API。
- `SetUpPrinter` 顯示印表機名稱與 `...` 選擇。WinForms 從使用者端本機設定讀值，載入時以 Windows 預設印表機覆蓋；瀏覽器版只在 DEV 提供假選項，Production 由瀏覽器列印對話框選取印表機。
- `Lot State` Grid 顯示 Lot、Status、Area、PS、OP、MO、CustomerLot、Product、數量與 `Print_Count`。查詢結果排除 `LotState=0`，依 HotLot 降冪、Priority 升冪、LotNo 升冪排列。來源頁的 Status 條件程式被註解，故不可照 WP103 限制狀態 0/1/4；像 Status 11 的列仍可能出現。
- Scope 選項是 `Current Process`（預設）與 `Original Process`。Current Process 依 Lot 當前流程，重工／回復流程會標示額外步驟；Original Process 依主流程。Core 還可展開 Net Flow。Current PS、All Processes、Include Log 等 Designer 控制項目前設為隱藏，不能當成可用查詢條件。
- 右側按鈕為 Preview、Print、Print Empty Version、Close。Preview／Print 需要作用列，可勾選多 Lot；RunCardStyle 0 時選取列必須屬同一張 MO。`MOState=99` 會載入流程卡紀錄；一般查詢中的 Include Log 呼叫目前註解，且控制項隱藏。

## 預覽與列印邏輯

- Form 從系統參數讀 `RunCardStyle`，缺省為 0。正式服務需由伺服器設定決定樣式，不可接受瀏覽器自行指定。
- Style 0 使用 THEIL `arRunCard_General_THEIL_BU1` 報表，涵蓋產品／OP 屬性規格、SOP、備註、流程站、工單材料、Lot 屬性與列印次數、處置資訊及 PMC／品質／產品工程人員；Matrix Code 讀 `XX_TBLLOTMARITX_THEIL_BU1`。WinForms 還會檢查主管使用者是否有效並提示。
- Style 非 0 依產品 RunCardMap 選自訂版型，先以產品精確比對，沒有時才使用 `*` 通用設定。若產品對應執行器或報表無法移植，Production API 必須回明確錯誤，不可偽造成功結果。
- Print 成功後經 `UpdatePrintCount` 更新既有 WIP Lot Property 的 `WIP_PRINT_COUNT`。家用端需將「產生列印內容」和列印次數寫入設計成可重試且一致的 API command，避免重送造成錯誤重複加計；瀏覽器無法證明紙張已實際輸出，該差異需在 API/UI 行為中明確處理。
- Print Empty Version 先選產品，再由 `LoadRunCardData_Empty` 取產品流程、OP 屬性、BOM 與人員資料，使用 `arRunCard_General_Empty_THEIL_BU1`；此流程沒有 Lot，也不增加 Lot Print Count。
- 標準報表依 Lot 逐張產生／預覽；DEV 畫面只提供固定假 Lot、假產品及假流程預覽，Print 僅在本機 DEV mock 記錄假次數，不連正式印表機、不呼叫 API。

## 資料表與家用端 API handoff

- 重用 `TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLWIPLOTPROPERTY`、MO／產品／流程、OP 屬性、BOM、材料、等待／ERF 處置與使用者資料，以及既有 `XX_TBLLOTMARITX_THEIL_BU1`。Matrix 的 Oracle source DDL 與 PG CREATE-only 已在 `database/reference/mestest_wip_wp201_*`；Lot Property 由既有 WIP reference 提供。本功能沒有查到或要求新增專用表，無新 DDL。
- 建議登入保護的查詢 `GET /api/wip/lot-sheet/lots?moNo=...&createFrom=...&createTo=...`，伺服器強制至少一種查詢條件、日期順序、授權範圍、`LotState<>0`、確定排序及分頁。MO lookup 重用既有 MO API。
- 建議 `POST /api/wip/lot-sheet/preview`，body 提供 LotNo 清單、Current／Original scope、ReportStyle 由 server 設定取得。伺服器重新查詢及授權每個 Lot，保證同 MO 限制，載入報表資料及 Log 規則後回傳 PDF／文件；不接受客戶端傳入列印機名稱或檔案路徑。
- 建議 `POST /api/wip/lot-sheet/print` 由 server 產生列印文件並處理 Print Count command；定義冪等鍵，資料更新在交易中原子執行。因瀏覽器列印狀態不可由 server 確認，請與家用端決定次數記錄時點並寫入 API 規格。
- 建議 `POST /api/wip/lot-sheet/empty-version`，body 為 ProductNo／Version，伺服器讀取產品與流程資料及空白版報表，不修改 Lot 狀態或列印次數。
- 家用端先核對 PostgreSQL live schema／migration 與引用 API。這份規格不產生 CREATE-only DDL、不代表 live schema 已驗證；只在實際缺少共用 Matrix／Lot Property 或來源資料表時，再補 Oracle metadata 查證與 forward migration。

## 公司端界線與未完成項

Vue 頁固定假資料，只驗證查詢條件、Current／Original 切換、多 Lot 同 MO 規則、預覽與空白版本互動；假列印次數用 localStorage。未連 YMES PostgreSQL、未執行 Oracle DDL/DML、未執行 build／test／browser。ActiveReports 版面、完整 SQL 欄位及 Production API 仍由家用端接手實作與驗證。

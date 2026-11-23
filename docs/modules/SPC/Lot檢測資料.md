# SC202 Lot 檢測資料

## 範圍與來源

- Function：`SPC DATA BY LOTNO`，FunctionKey `SC202`，Form `frmQueryDataByLotNo_THEIL_BU1`，ExecutionFile `SC_QueryDataByLotNo_THEIL_BU1.dll`。
- WinForms／Designer：`D:\iMES\CSiMES\Customer\Production\MESWin\SPC\SC_QueryDataByLotNo_THEIL_BU1\frmQueryDataByLotNo_THEIL_BU1.vb`（Designer 併在 Form 檔）。
- Form 的主要查詢呼叫是 `THEIL.wsSPC.LoadLotSPCMeasureData`，不是標準 `wsSPC` 同名方法。
- 客製 Service：`D:\iMES\CSiMES\Customer\Production\wsTHEIL\wsSPC\wsSPC.cs`，`LoadLotSPCMeasureData`。
- 客製 Core：`D:\iMES\CSiMES\Customer\Production\MEStc_THEIL\tcSPC\clsSPC.cs`，`LoadLotMeasureData`。

## WinForms 查詢與結果行為

- 提供依 LotNo 或依 QC Item 查詢的條件頁。LotNo 可多選／輸入多筆；快速選取透過 `wsSPC.LoadVarLotNoFromFactorLog` 載入 Lot 候選。From／End 日期可勾選套用。
- 可分別要求計量資料與計數資料。依 QC Item 查詢時，可選一個或多個計量 QC Item、可用的變數 QC Item 群組（群組 IssueState=2，展開成成員項目）、以及多個計數 QC Item；亦可依各 QC Item 的 Collect Factor 指定條件。
- Service 將 LotNo、QC Item、QC Item Type、日期、計量／計數載入旗標、計量／計數 QC Item 清單及兩組 Factor DataTable 交給 THEIL Core。Service 另解析 ERFNo、MO、Product、COA、QC Form Info、ShowName 等選用條件；目前 Form 主要查詢未使用的選用欄位不作 DEV 輸入。
- Core 回傳計量 `LotDataResult`／`LotDataShow`、計數 `AttLotDataResult`、要因清單 `AllFactor`／`AttAllFactor`、計量統計／原始量測值及計數缺陷明細。計量結果依 Lot／建立時間排列；原始值依 SPCSerial、QC Item、SerialNo 對應。計數結果可展開一般錯誤與元件錯誤內容。
- 查詢結果唯讀。Form 無修改／刪除操作；另提供 Factor Select 顯示／隱藏要因欄位。Result 狀態碼為 0 Pass、1 Warning、2 Wait、3 Hold、4 Alert。
- Form 對有效的起迄日期做區間檢查；日期條件及 Lot/QC Item 條件可組合。Core 以 950 筆分段組合部分 SQL `IN` 清單，但這不是整體結果筆數上限。

## Vue DEV 頁

- 路由：`/YMES/SPC/DataByLotNo`，選單鍵 `function:SPC:SPC DATA BY LOTNO`。
- 以頁面記憶體中的 DEV fixtures 模擬 Lot／QC Item 兩種查詢、計量／計數切換、批次 Lot 選取、QC Item／群組選擇、日期與 Collect Factor 條件、Lot 彙整列、計量統計與量測值、計數樣本／缺陷與元件明細、Factor Select、唯讀 SPC 明細視窗。
- 為避免假資料頁觸發無界查詢，DEV 會要求 Lot 或日期條件；依 QC Item 查詢時，所勾選資料類型各自需有對應 QC Item。僅兩個日期皆啟用時檢查先後順序。這些是 DEV 查詢防護，不宣稱等同舊系統所有提示。
- DEV 對選取 QC Item 不含該 Factor 的列不套該 Factor 值限制；實際正式查詢需按每個 QC Item 對應的 Factor 定義組合條件。
- 不呼叫 Service、Oracle、YMES API 或資料庫。結果只存在前端記憶體，離開頁面即重置。

## DDL／metadata 與家用端 handoff

- 計量參考：`database/reference/mestest_qc205_variable_dependencies_source_ddl.sql`、`database/reference/mestest_qc205_theil_custom_source_ddl.sql` 及現有 `database/reference/mestest_qc210_variable_measurement_log_delete_create.sql`。計量 Factor／Detail／Statistic／Limit 等表重用既有 SPC reference，不重複建立 CREATE SQL。
- 計數參考：`database/reference/mestest_qc_item_attributes_source_ddl.sql` 與 `database/reference/mestest_qc_item_attributes_create.sql`；既有 Basis／Detail／Tape 等結構重用該 reference。
- THEIL Core 明確查詢 `TBLSPCAttDetailLogComponent`（與 `tblQCReasonBasis` 關聯）以產生計數元件錯誤明細；repo 沒有此表同版 Oracle source DDL／metadata 或 PostgreSQL CREATE-only。桌機受保護 Oracle profile 不存在，本次未連 Oracle、未猜欄位／key／index。取得可用的唯讀受保護 profile 並確認 metadata 前，不建立該表的 CREATE SQL。
- 家用端先比對既有 YMES tables/migrations，再確定 SC202 使用欄位與元件明細資料來源。正式 API 必須登入及依 FunctionNo 授權、限制查詢範圍／結果筆數並提供分頁或明確上限；Lot 多選及日期條件須可組合，兩類 QC Item 與 Factor 條件分開驗證。SQL 值必須參數化；動態 Factor 欄名須以已核准 QC Item Factor metadata 白名單驗證，不能直接使用請求值拼接識別字。
- 家用端應保留純查詢語意，不新增 SC202 寫入 API；查清動態要因投影、ShowName 轉換、Component error／ReasonType 的來源關聯與查詢效能。補做 Oracle metadata 前，元件錯誤表只可沿用 live schema 已存在的可靠定義或暫不提供該欄，不可依 Core 的 SELECT 欄位猜建 schema。
- 公司端未連 PostgreSQL、未建 migration／schema、未實作或驗收正式 API。

# RT416 整體設備效率 (OEE)

## 清冊與來源

- 選單：RPT「在製品」→「整體設備效率(OEE)」，FunctionNo `EQP OEE SUMMARY`、FunctionKey `RT416`，Form `frmEQPOEESummary`，執行檔 `RT_EQPOEESummary.dll`。
- Form／Designer：`D:\iMES\CSiMES\STD\MESWin\RPT\RT_EQPOEESummary\frmEQPOEESummary.vb` 與同目錄 Designer。THEIL checkout 的 Form SHA256 與 CSiMES 相同。
- Service/Core：`wsRPT.LoadEQPOEESummary` → `kcRPT.clsReport.LoadEQPOEESummary`。
- 指標產生來源：`MESAgent\MESAutoRun\AutoRunLib\clsAutoRunLibrary.cs` 的 `funOEESummary`、`fuOEEAvailRate`、`fuOEEPerforRate`。此 Agent 工作流按日期重算 `tblRPTEQPOEESummary`，不在 WinForms 查詢時即時計算指標。

## WinForms 畫面與查詢

- Form 初始日期為本月 1 日至今天；欄位為 `BelongDate(Start)`、`BelongDate(End)`、AreaNo、EquipmentType、EquipmentNo，三組設備條件皆可多選並用逗號分隔。Area picker 查 `AreaClass=0`；EquipmentType／Equipment picker 查 `IssueState=2`。Equipment picker 的清單只依選定 EquipmentType 篩選，不套 AreaNo。
- `Query` 拒絕未來日期，並要求開始日期早於結束日期。送出起始日 `00:00:00`、結束日 `23:59:59`，三組選填條件以 `<additional>` 條件傳送。未要求至少一個設備條件，沒有最大日期跨度。
- `wsRPT.LoadEQPOEESummary` 解析起訖與 Additional，未由 Form 傳 `reportmode`；`clsReport` 用 `Select * From tblRPTEQPOEESummary`，日期 `BelongDate>=`／`<=`，並以 AreaNo／EquipmentType／EquipmentNo 條件查詢。結果依 EquipmentNo、BelongDate 排序。
- Grid 顯示 13 欄：AreaNo、EquipmentType、EquipmentNo、BelongDate、EQPAvailableTime、AvailRate、EQPRealOutput、EQPSTDOutput、PerforRate、EQPTotalOutput、EQPTotalInput、Yield、OEE。Availability、Performance、Yield、OEE 使用 `0.##%` 格式；`EQPAvailableTime` 於 Agent 計算中為分鐘。Core 不在查詢時重算數值。
- `ultraChart1` 初始隱藏，Query handler 對 `funGetChart()` 的呼叫被註解；font、zoom、tooltip 等 Gantt chart 程式碼不會由一般查詢顯示。DEV 只復刻實際可達的彙總表，不增加 OEE 趨勢圖。

## MESAutoRun 產生邏輯

- AutoRun Parameter01／Parameter02 可提供重算日期起訖；任一未提供時預設重算昨天 00:00 至今天 00:00。系統參數 `OEEWorkTime`（每日工時分鐘）及 `OEEExceptTime`（每日除外時間分鐘）必須存在且通過範圍檢查；每日計畫工時為兩者相減。
- `fuOEEAvailRate` 將設備狀態歷程及目前狀態依 BelongDate 切日；`UtilizationType=0` 計入 Off Time，其他狀態計入 Available Time。狀態未涵蓋的計畫工時差額也歸入 Off Time。設備區域來自 Area／Equipment Group 對應；無區域時寫 `N/A`。設備類型標準產出每小時值由 `CountEQPUnitQty / StdUnitEQPTime * 60` 取得。
- `fuOEEPerforRate` 讀已完成生產歷程、資源與設備紀錄；EQPTotalInput 使用設備投入量，EQPTotalOutput 使用 GoodQty。實際每小時產出 = TotalOutput / 實際運轉小時；PerforRate = 實際產出 / 類型標準產出；Yield = TotalOutput / TotalInput；OEE = AvailRate × PerforRate × Yield。缺標準產能或沒有生產時，相應欄位可為 NULL，內部乘數採 1。
- 每次重算會先依 BelongDate 刪除既有 `tblRPTEQPOEESummary` 與 `tblRPTEQPOEELot`，再批次新增結果。`tblRPTEQPOEELot` 是 Agent 維護的 Lot 對應表，RT416 Form/Core 未查詢它。此 DEV 不模擬 AutoRun 寫入或重算。
- Service/Core 使用字串串接 SQL，Filter 由 Additional SQL 條件組成。家用端正式 API 必須使用參數化條件及授權檢查；Core 未設定最大日期跨度或設備數量。

## Vue DEV 復刻

- 路徑 `/YMES/RPT/EqpOeeSummary`，選單 key `function:RPT:EQP OEE SUMMARY`。
- 合成 DEV fixtures 支援日期範圍、Area／EquipmentType／EquipmentNo 多選與文字逗號條件、來源條件互動、查詢錯誤／空結果／載入狀態，以及 13 欄 OEE 彙總表。DEV rate 直接使用 fixture 值，沒有重算公式。
- Lookup 假資料模擬 AreaClass=0、EquipmentType／Equipment IssueState=2。正式模式顯示尚未接上，沒有呼叫 API 或資料庫。
- 未 build、未測試、未用瀏覽器驗收。

## Oracle metadata 與家用端 handoff

- `TBLRPTEQPOEESUMMARY` 是報表 Core 直接讀取且 MESAutoRun 寫入的主表；repo 沒有可用的 Oracle source DDL／metadata 或對應 CREATE-only SQL。Agent 另維護 `TBLRPTEQPOEELot`，RT416 查詢頁不使用。桌機找不到受保護 Oracle attachment/profile，因此沒有連線或猜欄位。
- 公司端待安全 profile 可用後，唯讀取得兩表（至少主表）的 `DBMS_METADATA.GET_DDL`、owner/synonym、完整欄位型別/precision/scale/null/default、PK／FK／unique／indexes／trigger；只取結構，不讀取或匯出 OEE 業務資料列。查到主表定義後再交 PostgreSQL CREATE-only。
- 家用端先確認 OEE summary 在 YMES 的同步／產生來源與更新頻率，再提供登入授權的分頁 `GET /api/reports/equipment-oee-summary`，查詢條件為 startDate、endDate、areaNos、equipmentTypes、equipmentNos，回傳上述 13 欄。勿假設 YMES 有 OEE summary rows；不要在家用端重跑 MESAutoRun 刪除／重建作業。
- 家用端於 2026-11-27 以 PPK／SSH 唯讀確認 `tblrpteqpoeesummary`、`tblrpteqpoeelot` 在正式 PostgreSQL 所有 schema 均不存在；未讀 OEE 業務列、未建立 guessed schema。資料庫連線紀錄詳見 `docs/DATABASE_CONNECTION.md`。
- 後續 schema 與 API handoff 依上列原生欄位核實後補齊；沒有 OEE summary DDL 前不建立 guessed schema／CREATE-only SQL。

# RTS01 電性測試資料查詢

## 選單與來源

- 清冊：RPT「報表管理」→「行業包」→「電性測試資料查詢」，FunctionNo `B_ELECTRICAL TEST DATA QUERY`、FunctionKey `RTS01`、Form `frmElectricalTestDataQuery`、ExecutionFile `RT_B_ElectricalTestData.dll`。
- WinForms／inline Designer：`D:\iMES\CSiMES\Industry_Base\MESWin_Industry_Base\RPT\RT_B_ElectricalTestData\frmElectricalTestDataQuery.vb`。
- Service：`Industry_Base\MESws_Industry_Base\wsRPT\wsRPT_Base.cs` 的 `LoadTestRecipeType`、`LoadSummaryTable`、`GetPSNoByOPNo`；PS 清單另呼叫標準 `wsPRS.LoadProductionSegment`。
- Core：`Industry_Base\MEStc_Industry_Base\tcRPT\clsRPT_Industry_Base.cs` 的同名方法。查詢只讀；Form 沒有新增／修改／刪除或明確匯出按鈕。

## 畫面及操作

- 預設 `NowData`、`TestRecipeType` 分組。`HistoryLog` 切換到歷程摘要表；另一種分組是 `Component`。
- `PSNo` 必填，可多選、逗號分隔；`...` 使用 Production Segment 清單。選 RecipeType 前要先有 PSNo，再以 `...` 載入該 PS 的可用測試類別並多選。
- `LotNo`、`ComponentNo`、`TestRecipeType`、`TestDate` 以各自 CheckBox 啟用；三個文字條件可輸入逗號分隔值。TestDate 包含 From／To 時間端點。
- Query 先檢查 PSNo，再載入查詢結果。結果是階層 Grid：依 RecipeType 分組時父列為 RecipeType；依 Component 分組時父列含 LotNo／ComponentNo。展開後顯示測試時間、批號、元件、RecipeType、站點、設備、LogGroupSerial 及動態測試欄位。數值格式依欄位 metadata 判斷。
- ObjectMode 可由其他 WinForms 呼叫者帶入 `PSNo` 或 `OPNo`、Lot／Component／RecipeType、Now／History 和分組方式。若只有 OPNo，Core 先由 `tblOPBasis` 取得 PSNo；傳入值會鎖定，載入時自動查詢。獨立選單頁使用 DefaultMode。

## Service／Core 行為

- RecipeType lookup 從 `tblSYSTestRecipeBasis` 取 `SummaryTableName` 不為空、`IssueState=2` 的核准設定；本 Form 的一般路徑限定 `TestRecipeClass=2`。指定 OPNo 時另讀 `tblOPProperty` 的 `TESTRECIPETYPE` 預設值過濾。
- 摘要查詢使用 `PSNo`、`IssueState=2`、`SummaryTableName LIKE 'TBLWIPTESTSUM_%'` 找表。History 模式將表名前綴換為 `TBLWIPTESTSUMLOG_`。實際測試欄由每個核准 Recipe 設定的 `ConversionField` 組成；回傳欄位會加上 RecipeType 前綴以區分同名欄位。
- Core 再從 `tblWipLotLog_Report`／`tblOPBasis` 取得 OPNo／OPName，建立依 TestRecipeType 或 ComponentNo 的父子 Relation。Oracle 分支從 `ALL_TAB_COLUMNS` 讀測試欄型別，MSSQL 分支使用 `INFORMATION_SCHEMA.COLUMNS`。
- 舊 Core 將 PSNo、LotNo、ComponentNo、RecipeType 與表／欄位識別字串接到 SQL；家用端 API 必須參數化值，且只允許由伺服器端核准 Recipe 設定解析動態表名及欄位名。回應應限量／分頁並限制最大 Recipe 數與資料列數。
- 舊 Core 套用 `EndTestDate` 的條件位於 `FromTestDate` 非空判斷內；只輸入 To 時不會套用上界。Vue DEV 依一般日期範圍處理兩端，正式 API handoff 要求明確定義單獨端點語義並採正確範圍。

## Vue DEV 與家用端 handoff

- `/YMES/RPT/ElectricalTestData` 只在 DEV＋使用者選擇開發環境時載入本機假資料；可操作 PS／Recipe 多選、各條件開關、Now／History、兩種分組、Query 及階層明細。假資料有數個不同欄位集合，用來呈現動態欄位形狀；沒有呼叫正式 Service、MES、Oracle、PostgreSQL 或印表機。
- 建議 API：重用既有 PS 清單；`GET /api/reports/electrical-test-data/options/test-recipe-types?psNos=...&opNo=...`；`GET /api/reports/electrical-test-data?psNos=...&dataType=now|history&groupBy=testRecipeType|component&lotNos=...&componentNos=...&testRecipeTypes=...&fromTestDate=...&toTestDate=...`。回傳固定 metadata 欄位及每種 Recipe 的動態 `measurementColumns`／`values`，不要回傳可執行 SQL 或任意資料表名稱。
- 桌機受保護 Oracle profile 不可用，本輪未查 Oracle。Repo 已有 `TBLSYSTESTRECIPEBASIS`（`database/reference/mestest_oe_wafer_test_recipe_source_ddl.sql`）及 `TBLOPBASIS`／`TBLOPPROPERTY`（`database/reference/mestest_operation_and_system_source_ddl.sql`）參考；動態 `TBLWIPTESTSUM_<RecipeType>`／`TBLWIPTESTSUMLOG_<RecipeType>` 的可信 DDL／metadata 未取得，未猜 CREATE SQL。家用端先核同版 MESTEST 與 YMES live schema、來源庫／Owner、索引及各 Recipe 動態欄位，再決定是否需要新增空表。
- 公司端未連 YMES PostgreSQL、未實作正式 API／schema、未 build／test／browser。

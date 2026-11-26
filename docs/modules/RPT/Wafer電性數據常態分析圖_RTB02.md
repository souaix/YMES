# RTB02 Wafer 電性數據常態分析圖

## 功能與來源

- 清冊：RPT「報表管理」→「行業包」→「Wafer電性數據常態分析圖」，FunctionNo `B_WAFER EDC NORMALITY CHART`、FunctionKey `RTB02`、ExecutionFile `RT_B_WaferEDCNormalityChart.dll`。
- WinForms Query／inline Designer：`D:\iMES\CSiMES\Industry_Base\MESWin_Industry_Base\RPT\RT_B_WaferEDCNormalityChart\frmWaferEDCNormalityChartQuery.vb`。
- WinForms 分析／inline Designer：同資料夾 `frmWaferEDCNormalityChartView.vb`。
- Service：`D:\iMES\CSiMES\Industry_Base\MESws_Industry_Base\wsRPT\wsRPT_Base.cs` 的 `LoadEDCNormalityChart`、`ShowWIPRawDataToNormalityChartBlob`、`LoadRPT_EDCNormalityChartCalData`、`LoadRPT_EDCNormalityChartData`；選單資料由 `wsPRS.LoadProductionSegment`、`wsSYS.LoadTestRecipe_RawData`、`wsEQP.LoadEquipmentBasis` 提供。
- Core：`D:\iMES\CSiMES\Industry_Base\MEStc_Industry_Base\tcRPT\clsRPT_Industry_Base.cs` 的同名查詢／分析方法；測試 Recipe lookup 由 `tcSYS\clsSYSMesParameter_Industry_Base.cs` 的 `LoadSYSTESTRECIPEBASIS_RWADATA`／`ShowSYSTESTRECIPEBASIS_RWADATA` 提供。

## WinForms 條件與互動

- PSNo 為單選，從 Production Segment picker 載入。選 PSNo 後，TestRecipe picker 只載入 `IssueState=2`、`TestRecipeClass=2` 的廠內 RawData Recipe，允許多選，畫面以 `TestRecipeType,TestRecipeNo` 配對傳入。
- TestRecipe 必選。EquipmentNo 是可選的單一主設備條件，picker 只顯示 `ChamberFlag=0`；ComponentNo 和 LotNo 是可選文字片段條件。
- StartTestTime、EndTestTime 各自有啟用核取方塊，預設未勾選，日期預設為當日 00:00:00 與 23:59:59。來源允許只設單一端點。
- Query 顯示測試記錄。列包含 `LogGroupSerial`、Component／Product／Lot、OP、Equipment、TestRecipeType／TestRecipe、TestDate／ImportDate、OPName、FileName。
- 選取結果後按 Enable Chart 開啟分析視窗。分析視窗載入該 Recipe 的 BLOB 量測值，呈現直方圖、Frequency groups、Test Data Detail 與統計摘要；支援切換統計欄、調整組數與 sigma、排除量測點後重算、SaveGraph、Close。
- 原 Query 的選取程式以 `TestRecipe` 再篩整份結果集並取第一列，沒有沿用目前列的複合鍵；同 Recipe 多列時可能開到錯誤記錄。Web DEV 版以選取列的記錄 ID 保持精確對應。

## Service／Core 資料流程

1. `wsSYS.LoadTestRecipe_RawData` 解析 PSNo、IssueState、TestRecipeClass 後呼叫 `ShowSYSTESTRECIPEBASIS_RWADATA`。來源 Query 不把 PSNo 傳進原始量測查詢，它用 PSNo 限定可選 Recipe。
2. `wsRPT.LoadEDCNormalityChart` 將 RecipeType／RecipeNo 及 Equipment、Component、Lot、起訖時間交給 Core。Core 對每個 RecipeType 查 EDC `TBLWIPRAWDATA_<TestRecipeType>`，依所選 RecipeNo 欄位非空、Equipment 相等、Component／日期條件篩選，再以 LogGroupSerial 查 MES `tblWIPLotLog_Report` 並連 `tblOPBasis` 取得批次、Product、OPName；整理為 `WIPRawData`。
3. 分析視窗呼叫 `wsRPT.ShowWIPRawDataToNormalityChartBlob`，Core 由 `TBLWIPRAWDATA_<type>` 的 BLOB 欄位讀取所選 Recipe，UTF-8 解碼並拆分成 Measure Data／Sequence。`HAVECOORDINFO=1` 的格式取分號項目中逗號分隔的第三欄；無座標時直接取分號分隔量測值。
4. View 以預設 8 個區間計數繪直方圖，對排除點後的資料更新圖表／統計，並展示 Test Data Detail。原 WinForms 在開啟分析時會呼叫兩條寫入路徑：`tblRPTEDCQCCalData` 儲存 DataPointCount、Mean、SD、Skew、Kurtosis、PValue 與計算時間；`tblRPTNormalityChartData` 儲存 Frequency 區間及次數。這是舊系統副作用，桌機 DEV 不模擬資料庫寫入。

## Vue DEV 交付

- App 入口：`/YMES/RPT/WaferEDCNormalityChart`，RPT「行業包」下新增清冊功能入口。
- DEV 預覽只用內嵌假資料：PS 單選、Recipe 多選、主設備選擇、可選 Component／Lot／日期端點、查詢結果列選取、Enable Chart 分析視窗、直方圖與常態參考曲線、Frequency groups、統計摘要、統計欄位切換、Exclude 即時重算及 SVG Save Graph。
- 生產環境維持 pending 頁。沒有呼叫 Oracle、EDC、MES Service、PostgreSQL 或正式 API；分析結果只在 Vue 狀態計算，不會保存。
- 為避免把估算值冒充來源統計，假資料頁僅顯示樣本數、平均、標準差、最大、最小、範圍、偏態與峰度；不提供未由來源表查明的 US／LS／Target、Cp／Cpk 或 P-Value。

## 資料庫與實作風險

- Repo `database/reference`、`docs` 未找到 `TBLWIPRAWDATA_<type>`、`tblRPTEDCQCCalData`、`tblRPTNormalityChartData` 的可信 DDL／欄位／鍵／索引。RawData 是依 RecipeType 建表、依 RecipeNo 建 BLOB 欄位，不能以單一猜測的靜態 CREATE 取代。
- 桌機未有可用的受保護 Oracle profile；本項未查 Oracle metadata，也未猜 DDL。另需查明 EDC 與 MES 實際 DB Owner、版本、EDC 連線來源及可用的唯讀權限。
- Core 以 RecipeType、RecipeNo 等值組動態資料表／欄位及 SQL 字串；日期與文字條件也有拼接。正式 API 必須從伺服器端核准 Recipe metadata allowlist 取得動態識別字，所有值參數化、限量分頁，不能接受用戶任意 table／column 名稱。
- `fuReverse_CoordinateOn` 的 BLOB 欄位迴圈與資料列迴圈變數分別為 `j`／`i`，但部分欄位讀取使用 `dtRawData.Rows[j]`；這和外層欄位索引不一致，疑似來源列索引錯誤，應以實際樣本核對後再決定是否修正。
- Core detail 連線設定分別使用 EDC 與 MES connection string；移植時不能假設 EDC raw data 已在 YMES PostgreSQL，應先確認資料供應方式。開啟圖表原本會寫兩張報表表；家用端需決定是否保留持久化、改成只讀／本地計算，或另行設計授權的寫入流程。

## 家用端 API／schema handoff

- 先確認可用 EDC RawData 來源、MES Recipe／PS／Equipment／LotLog／OP lookup，以及各資料庫 schema／Owner／欄位型別／索引。取得可信 DDL 或 metadata 前，不提供動態 RawData／統計表 CREATE SQL。
- 建議唯讀 API 分為 Recipe options（依 PSNo）、EDC 查詢記錄（RecipeType／RecipeNo 多選與可選條件）、單筆記錄的量測明細。查詢記錄 DTO 帶穩定複合識別鍵：RecipeType、RecipeNo、LogGroupSerial、ComponentNo、TestDate、ImportDate；支援上限與分頁。
- 對外只接收 PSNo、核准 Recipe 選項、EquipmentNo、Component／Lot 片段和起訖時間。資料庫物件識別字從核准設定解析；查詢用參數化 SQL。量測 BLOB 解析需依來源格式、`HAVECOORDINFO` 及版本處理，並核對目前 Core 的列索引疑點。
- 在確認產品是否需要跨工作階段保存分析結果前，先讓統計由 API 提供 raw measurements 後在 UI 計算。若需保存，另核實兩張舊 RPT 表的用途、鍵與資料生命週期，再決定 YMES 寫入 API 和 schema；不要直接照搬舊 Core 的字串 SQL 或在公司端建立 PostgreSQL 結構。

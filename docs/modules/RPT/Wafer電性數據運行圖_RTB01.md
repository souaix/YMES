# RTB01 Wafer 電性數據運行圖

## 功能與來源

- 清冊：RPT「報表管理」→「行業包」→「Wafer電性數據運行圖」，FunctionNo `B_WAFER EDC RUUNING CHART`（來源清冊拼法）、FunctionKey `RTB01`、ExecutionFile `RT_B_WaferEDCRunningChart.dll`。
- WinForms Query／inline Designer：`D:\iMES\CSiMES\Industry_Base\MESWin_Industry_Base\RPT\RT_B_WaferEDCRunningChart\frmWaferEDCRunChartQuery.vb`。
- WinForms 圖表 View／inline Designer：同資料夾 `frmWaferEDCRunChartView.vb`。
- Service：`D:\iMES\CSiMES\Industry_Base\MESws_Industry_Base\wsRPT\wsRPT_Base.cs` 的 `LoadEDCRunningChart`、`LoadEDCRunningBlobToChart`、`LoadRPT_EDCQCCalData`；選單資料由 `wsPRS.LoadProductionSegment`、`wsSYS.LoadTestRecipe_RawData`、`wsEQP.LoadEquipmentBasis` 提供。
- Core：`D:\iMES\CSiMES\Industry_Base\MEStc_Industry_Base\tcRPT\clsRPT_Industry_Base.cs` 的 `LoadEDCRunningChart`、`LoadEDCRunningBlobToChart`、`LoadRPT_EDCQCCalData`；Recipe lookup 由 `tcSYS\clsSYSMesParameter_Industry_Base.cs` 的 RawData Recipe 方法提供。

## WinForms 條件與互動

- PSNo 單選且必填，picker 只列 `IssueState=2`。TestRecipe 由所選 PSNo 載入 `IssueState=2` 的 RawData Recipe，多選且必填；來源未額外限制 `TestRecipeClass`。
- EquipmentNo 是可選單一主設備，picker 僅顯示 `ChamberFlag=0`；ComponentNo、LotNo 是可選文字片段。StartTestTime、EndTestTime 各自可選，初始核取方塊未勾選，日期預設為當日 00:00:00 至 23:59:59。
- Query 結果有 CheckFlag 勾選欄，可選多筆記錄。欄位包含 `LogGroupSerial`、`ImportDate`、Component／Product／Lot、OPNo、EquipmentNo、TestRecipeType／TestRecipeNo、TestDate。Enable Chart 對每筆勾選記錄載入量測 BLOB，並在一個可捲動視窗中逐筆繪製 running chart；可 Save Graph。
- 每張圖的 X 軸為量測順序 Sequence，Y 軸為 Measure Data。圖下顯示 DataPoint、Mean、SDEV；來源保留以 TestRecipeNo、批次、設備和 TestDate 組成的標題。雙擊圖可再開單筆圖表 View。

## Service／Core 資料流程

1. `wsSYS.LoadTestRecipe_RawData` 以 PSNo 和 IssueState 載入可選 Recipe；Query 將 RecipeType／RecipeNo 以平行逗號清單傳入，Service 解析後呼叫 `clsRPT_Industry_Base.LoadEDCRunningChart`。
2. Core 對每個 RecipeType 查 EDC `TBLWIPRAWDATA_<TestRecipeType>`，以 RecipeNo 對應的動態 BLOB 欄位非空、Equipment 相等、Component 片段及 TestDate 起訖篩選。逐一以 LogGroupSerial 查 MES `tblWIPLotLog_Report`，加上 Lot 條件後合併回傳；結果包含 TestRecipeNo 和同值的 TestRecipe 欄位。
3. View 對每個已勾選結果呼叫 `LoadEDCRunningBlobToChart`。Core 依資料表欄位 metadata 找該 RecipeNo 的 BLOB、UTF-8 解碼並以分號拆點；`HAVECOORDINFO=1` 時每點含 X、Y、Measure Data，否則每點只含 Measure Data。View 按 Sequence 作 X 軸、Measure Data 作 Y 軸，統計 DataPointCount、Mean、SD、Skew、Kurtosis。
4. 開啟 View 後，WinForms 會呼叫 `LoadRPT_EDCQCCalData` 查 `tblRPTEDCQCCalData`；若沒有符合 LogGroupSerial／Component／Recipe／TestDate／ImportDate 的資料，就插入計算統計。公司端 DEV 只使用瀏覽器中的假資料與本機計算，不執行這項資料庫寫入。

## Vue DEV 交付

- App 入口：`/YMES/RPT/WaferEDCRunningChart`，新增 RPT「行業包」清冊入口。
- DEV 預覽提供 PS 單選、Recipe 多選、設備 picker、可選 Component／Lot／日期端點、查詢結果 CheckFlag 多選、全選、Enable Chart、多筆圖表、Measure Data 序列線圖、DataPoint／Mean／SDEV／Min／Max、量測明細展開與單圖 SVG 儲存。
- 使用假的 EDC 匯入記錄及量測序列。正式環境維持 pending 頁；不呼叫 EDC、Oracle、MES Service、PostgreSQL 或正式 API，不寫 `tblRPTEDCQCCalData`。
- 為避免偽造製程界限，DEV 圖只繪原始序列與描述統計；不推測 US／LS／控制線、SPC 規則違反標記或能力指標資料。

## 資料庫與風險

- Repo `database/reference`／`docs` 未找到 EDC `TBLWIPRAWDATA_<type>` 或 `tblRPTEDCQCCalData` 的可信 DDL／欄位／鍵／索引。RawData 為 per-RecipeType 動態表及 per-RecipeNo BLOB 欄位，沒有 Oracle profile，未查 Oracle metadata，也未猜 CREATE。
- Core 同時連 EDC 與 MES。表名、Recipe BLOB 欄名和 SQL 條件透過字串組合；動態識別字需由伺服器端核准 Recipe metadata allowlist 解析，所有值參數化並設分頁上限。
- `LoadEDCRunningBlobToChart` 雖以 EDC connection 查 BLOB，部分 metadata／SQL tuning 判斷使用 MES `strDataBaseType`，而同區塊查 metadata 又看 `strEDCDBType`；移植前需核實 EDC DB dialect 來源。舊 SQL 的 MSSQL 日期格式也需依實際 provider 核對。
- RPT Query 以 LogGroupSerial 逐筆查 LotLog，且依序向 Service 傳每筆。正式 API 可批次 join／集合查詢，但必須保持來源唯一性與 RecipeType／RecipeNo 對應，並限制日期範圍、回傳列數及明細 BLOB 大小。
- 原 Query 的空結果檢查使用 `Rows.Count < 0`，不會成立；後續勾選列檢查會阻擋未選取操作。DEV 入口使用正確的勾選筆數條件。

## 家用端 API／schema handoff

- 核實 EDC RawData 是否可供 YMES 讀取、實際 DB Owner／schema／dialect、動態 BLOB 欄位型別、`HAVECOORDINFO` 編碼與資料大小；另核對 MES Recipe、Equipment、LotLog 的鍵及索引。DDL 未驗證前不要建立猜測性 CREATE SQL。
- 建議唯讀 API 分為 PS／Recipe options、帶條件且限量分頁的 RawData 記錄查詢、勾選記錄的批次量測明細。DTO 使用 RecipeType、RecipeNo、LogGroupSerial、ComponentNo、TestDate、ImportDate 的複合鍵；Server 端從核准 Recipe 清單解析動態表／欄位名。
- 載入量測資料後由 UI 依來源顯示規則繪序列圖和描述統計。舊畫面會新增 `tblRPTEDCQCCalData` 統計快照；是否在 YMES 保留持久化、其鍵／生命週期與寫入權限需另外決定，不能假設此 Read API 能安全重現舊寫入副作用。

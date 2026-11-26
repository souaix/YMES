# RTB03 Wafer Map 電性測試圖表

## 功能與來源

- 清冊：RPT「報表管理」→「行業包」→「Wafer Map電性測試圖表」，FunctionNo `B_WAFER MAP TEST CHART`、FunctionKey `RTB03`、ExecutionFile `RT_B_WaferMAPTestChart.dll`。
- WinForms Query／inline Designer：`D:\iMES\CSiMES\Industry_Base\MESWin_Industry_Base\RPT\RT_B_WaferMAPTestChart\frmWaferMAPTestChart.vb`。
- WinForms 圖表 View／inline Designer：同資料夾 `frmWaferMAPTestChartView.vb`。
- 原圖繪製 helper：`D:\iMES\CSiMES\Industry_Base\MESWin_Industry_Base\COM\COM_WaferMAPTestChartLib\modTestChart.vb`、`TestData.vb`。專案引用 `COM_WaferMAPTestChartLib.dll`，可讀到對應 helper source。
- Service：`D:\iMES\CSiMES\Industry_Base\MESws_Industry_Base\wsRPT\wsRPT_Base.cs` 的 `LoadWIPRawRecordJoinLotLogJoinOPBasis`、`LoadWIPRawRecord`；lookup 使用 `wsPRS.LoadProductionSegment`、`wsSYS.LoadTestRecipe_RawData`、`wsEQP.LoadEquipmentBasis`。
- Core：`D:\iMES\CSiMES\Industry_Base\MEStc_Industry_Base\tcRPT\clsRPT_Industry_Base.cs` 的同名 RawRecord 查詢，以及 `tcSYS\clsSYSMesParameter_Industry_Base.cs` 的 Recipe lookup。

## WinForms 條件與互動

- PSNo 必填單選；Production Segment lookup 傳 `IssueState=2`。Recipe lookup 依 PSNo 傳 `IssueState=2`、`TestRecipeClass=2`、`DataType=1`，將去重後的 TestRecipeType 多選；查詢要求至少一個 type。
- EquipmentNo、ComponentNo、LotNo 為各自可勾選的可選條件；文字欄輸入時自動勾選，清空／取消時停用。Core 用完整相等條件，不是片段搜尋。Equipment lookup 傳 `IssueState=2`。
- TestDate 為一組可選日期區間，初始日期為今日往前六天 00:00 至今日 23:59:59，核取方塊預設未勾。Query 傳 `HaveCoordInfo=1`，只取具有 CoordinateX／Y 的 Wafer map 記錄。
- 結果可勾選多筆，顯示欄位為 CheckFlag、TestRecipeType、LotNo、OPNo、OPName、EquipmentNo、ComponentNo、TestDate、ImportDate、LogGroupSerial。Enable Chart 將所有勾選記錄送入圖表 View。
- 每筆 Wafer 記錄會載入該 TestRecipeType 下所有有效 TestRecipeNo 的量測欄位，依 CoordinateX／Y 畫圓形 Map。原 helper 以原點為圓心、最遠座標距離乘 1.1 推算半徑、翻轉 Y 座標；顯示座標點及數值文字。
- 使用者可任意勾選 Max Value、Min Value、Mean Value、Sigma 組合。Helper 以所有非空量測值計算，Sigma 使用母體標準差（平方差總和除以 N 再開根號），各統計值四捨五入到小數第六位。Legacy View 的 Save Graph 可存 JPEG／BMP／GIF。

## Service／Core 資料流程

1. 查詢 Service 傳入 TestRecipeType 逗號清單、可選 LotNo／ComponentNo／EquipmentNo、可選 From／To TestDate，以及 `HaveCoordInfo=1`。
2. Core 依每個 TestRecipeType 查詢 `TBLWIPRAWRECORD_<TestRecipeType>`，以 `LOGGROUPSERIAL` 非空且 CoordinateX／Y 非空為篩選，彙整 Component／Lot／LogGroupSerial／Equipment／TestDate／ImportDate；接著把結果欄位接入 Lot／OP 資料，輸出 CheckFlag 以外的唯讀結果。
3. 選列後，View 以 TestRecipeType、LogGroupSerial、ComponentNo、EquipmentNo、TestDate、ImportDate 呼叫 `LoadWIPRawRecord`。Core 從 RawData Recipe basis 取得該 type 有效 TestRecipeNo，將這些 RecipeNo 當作動態數值欄位讀取，連同 SequenceNo、CoordinateX、CoordinateY 回傳；圖表依每一欄各畫一張 Map。

## Vue DEV 交付

- App 入口：`/YMES/RPT/WaferMAPTestChart`，新增 RPT「行業包」清冊入口。
- DEV 假資料互動包含 PSNo 單選、TestRecipeType 多選、Equipment picker、精確 Component／Lot、可選 TestDate 區間、查詢結果多選、全選、Enable Chart、每片 Wafer 的多個 TestRecipeNo 圓形 Map、四種統計值任意組合、長圖捲動及整份 SVG 匯出。
- SVG 圖依 helper 的圓心座標縮放與 Y 方向，顯示紅色量測點、數值與座標 tooltip。假資料不連 EDC、Oracle、MES Service、PostgreSQL 或正式 API；production wrapper 保持 pending。
- Legacy Save Graph 格式是 JPEG／BMP／GIF；Web DEV 以 SVG 匯出，保留縮放清晰度。

## 資料庫與來源風險

- Repo `database/reference`／`docs` 找不到 `TBLWIPRAWRECORD_<type>` 的可信 DDL／欄位型別、Owner、索引或 Recipe 動態欄位定義。桌機受保護 Oracle profile 不存在，本次未查 MESTEST metadata，也未產生猜測 CREATE-only SQL。
- 原 Core 有需確認的 SQL 拼接問題：條件片段產生 `A.And ...`／`B.And ...`；`SQLCommandTuning` 套用在 `strSQL_Sub`，實際執行的卻是 `strSQL`。依該方法的字串內容，這些片段疑似讓條件查詢失敗。
- Core 註解明確指出 RawRecord 表位於 EDC 資料庫、LotLog／OP 表位於不同 MES 資料庫，且列出改為程式 Join 的原因；但目前 active 查詢字串仍將 `TBLWIPLOTLOG_REPORT`、`TBLOPBASIS` 與 EDC RawRecord 子查詢寫在同一 SQL 中。原本以 `cnnTemp_MES` 分段查 MES 的程式碼被註解。移植前需確認目前實際連線、資料庫 link 或分庫 join 實作。
- RecipeNo 被作為動態 SQL 欄名；正式 API 必須由 server 端已核准的 `TestRecipeType`／Recipe basis metadata 產生識別字 allowlist，值條件一律參數化。還需核對 LogGroupSerial／日期等查詢鍵的唯一性、索引及分頁／Map 點數限制。

## 家用端 API／schema handoff

- 核實 EDC 讀取來源、Owner／schema／DB dialect、`TBLWIPRAWRECORD_<type>` 的座標／序號／量測欄型別、RecipeNo 欄位產生規則與索引；同時確認 EDC RawRecord 與 MES LotLog／OP lookup 的可行跨庫方式及 join 基數。
- 建議唯讀 API：PS options；依 PSNo 取得 IssueState 2、TestRecipeClass 2、DataType 1 的 RecipeType／RecipeNo metadata；依 RecipeType 和精確可選條件分頁取得 Record；最後以被核准的 record composite keys 批次取得座標及 Recipe measurement points。限制結果列、Recipe 數、每筆點數與日期區間。
- API record key 至少包含 TestRecipeType、LogGroupSerial、ComponentNo、EquipmentNo、TestDate、ImportDate；用 server 端 Recipe metadata 驗證動態欄名，所有 filter 值參數化。家用端先取得同版 DDL／metadata 後再出可信 CREATE-only SQL；公司端不連 PostgreSQL、不實作正式 API/schema。

# BARTENDER Bartender print

## 來源與入口

- 清冊來源為 `BARTENDER`／`frmWP_BartenderPrint_THEIL_BU1`，FunctionKey 空白，隸屬「包裝與庫房」。公司桌機以 THEIL 原始碼 `D:\TP-iMes\iMES_THEIL_TP\Customer\Production\MESWin\WIP\WP_BartenderPrint_THEIL\frmWP_BartenderPrint_THEIL_BU1.vb` 為主核對；Designer 內嵌在主 Form，列印預覽對話框另有 `frmWP_BartenderPrint_Form.vb`／Designer。
- DEV fallback 路由：`/YMES/WIP/BartenderPrint`。頁面只有在 Vite DEV 且 `developmentPreview=true` 時載入互動原型；正式環境顯示未接服務提示。

## WinForms 及可確認的服務呼叫

- Form Load 讀取執行環境參數、載入 `TBLINLABEL_TAG_FILE_MAP` 對照 Grid、讀取本機 Windows 印表機清單；原生頁籤為「工具」、「應用設定」、「PREVIEW」。設計器內 Lot 輸入預設值未沿用到 DEV fixture，以免把來源環境資料當成測試批號。
- 「產生標籤選項」以 `GETLPC` 查 Lot／Customer／CustomerParent／Product，再呼叫 `InLabel/GetAvailableTags`，類型傳「內標籤」、`IsTotal=N`。回傳含 `TAG_NO`、`LABEL_NAME`、標籤型態／尺寸／DPI／名稱／屬性、客戶／產品、預設份數及預設標記。
- 「產生資料」呼叫 `GETXXQCLABEL`，再用所選 `.btw` 檔名（去副檔名並大寫）及 Lot 呼叫 `LabelApi/Query`。查詢回傳 rows 為動態欄位；Grid 前加 `CHK` 核取欄和全選／取消全選標頭。
- 「預覽」要求至少選一筆；呼叫 `GetSamplePreviewInfo` 並以 SFTP 取得範例 PDF，接著對話框啟動外部執行檔的 `PREVIEW` 模式，傳入標籤檔、Lot、主檔 JSON、所選資料 JSON、可選印表機。Print 再確認筆數後啟動 `PRINT` 模式；預覽窗支援頁面上一頁／下一頁。
- 「應用設定」Grid 欄位為 `TAG_NO`、`LABEL_NAME`、`TAG_ATTRIBUTE`；UPDATE 驗證每欄必填，確認後將整份清單送 `InLabel/SaveTagFileMap`，回報新增／更新／略過筆數。
- 「工具」頁可從設定的 SFTP 路徑下載並解壓外部 BarTender 執行檔。本次不下載、不解壓、不執行或安裝任何外部列印二進位檔；Web DEV 以文字／HTML 假標籤及本頁內狀態模擬預覽和送印確認。

## Service／Core 來源缺項

- THEIL WinForms 專案有 `ApiClient.vb`／`ApiExecResult.vb`；在目前可讀的 `D:\TP-iMes` checkout 搜尋，只找到上述消費端，未找到 `InLabel` API Controller、`LabelApi/Query` server handler 或 `GETLPC`／`GETXXQCLABEL` 對應 SQL／Core 來源。這條流程不使用標準 `wsWIP`／`clsWIP` 直連方法；伺服器資料來源、where 條件、列印 audit／重試契約因此待查，沒有從 UI 推定。
- `TBLINLABEL_TAG_FILE_MAP` 的 Oracle DDL／metadata 在 repo references 缺項；桌機沒有可用受保護 Oracle profile，未連 Oracle、未猜 CREATE-only。此工作未連 YMES PostgreSQL。

## Vue DEV 假資料行為

- 固定使用 `DEVLOT001`／`DEVLOT002`、DEV 客戶／產品／標籤對照；未知批號顯示查無／不在 fixture。可載入標籤選項、切換標籤、產生動態欄位假資料、逐筆／全選、選擇 DEV 標籤機、打開分頁預覽、確認模擬列印；頁內列印紀錄在關閉頁面後不保留。
- 設定頁可編輯、新增、移除對照，檢查必填並模擬 UPDATE 的新增／更新數；不呼叫 `SqlExecApi`、`InLabel`、`LabelApi`、SFTP、檔案系統或印表機。BarTender 工具頁顯示 runtime 未連線，下載按鈕停用。
- 標籤 API 的實際動態欄位、複本／標籤規則、PDF 及 `.btw` 渲染、印表機／裝置權限均未由 Service／Core source 或實機確認；DEV fixture 不代表正式格式。

## 家用端 handoff

- 先在家用端唯讀核對 YMES live schema、migration／既有 endpoints 是否涵蓋 `TBLINLABEL_TAG_FILE_MAP`；確認 `GETLPC`、`GETXXQCLABEL`、InLabel 查標籤與存檔、`LabelApi/Query` 各自真正資料來源、條件及登入／ACL。沒有可確認的 DDL 時先取得原始 schema metadata，不可用這份 DEV fixture 產生正式 CREATE。
- 正式 Web 由 server 解析可授權的 Lot／標籤範本，限制可用列印 provider、範本和印表機；不要讓 browser 任選 server path／printer 或直接執行外部 BarTender 檔。設計預覽／送印 job、idempotency、未知結果 reconciliation、操作 audit 及正式 BarTender runtime／授權部署策略。公司端只交付 DEV 假資料畫面及來源缺項，不實作 API／schema 或安裝 vendor runtime。

## 家用端唯讀 repo／schema 核對（2026-11-22）

- Home `src/` 未找到 `GETLPC`／`GETXXQCLABEL`、InLabel／LabelApi server handler；遠端 YMES catalog 未找到 `TBLINLABEL_TAG_FILE_MAP`。未讀業務資料、未執行 DDL／DML，也沒有猜測 CREATE-only。
- 需取得實際 server handler／endpoint 契約、標籤來源 SQL／Core 及該 mapping table 的同版 Oracle DDL／metadata，再決定正式 provider、查詢範圍、權限和列印 audit；不可由 API consumer 或 DEV fixture 推測。

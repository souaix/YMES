# SC102 SPC 量測數據收集_計量連續（SPC ONLIE CONTROL CONTINUE）

## 選單與來源

- 選單識別：Module `SPC`、FunctionNo `SPC ONLIE CONTROL CONTINUE`、FunctionKey `SC102`、Form `frmSPCOnlineControl_test`、ExecutionFile `SC_OnlineControl_Test.dll`，分類 `SPC管制`。
- 主表單及內嵌 Designer：`D:\iMES\CSiMES\STD\MESWin\SPC\SC_OnlineControl_Test\frmSPCOnlineControl_test.vb`。連續計量子表單及內嵌 Designer：`STD\MESWin\SPC\SC_Var_MeasureContinuous\frmVar_MeasureContinuous.vb`。
- 主 Form 的 `btnConfirm` 對計量／計量群組於非 Component 分支動態載入 `frmVar_MeasureContinuous` / `SC_Var_MeasureContinuous.dll`；QCItem 可透過特別 FormName／ExecutionFile 改用客製 DLL。傳入 LotNo、OPNo、LotSerial、LogGroupSerial、GroupNo、LotIsValid、RetestLotNo、SenderName、Factor、QCItem、PopulationQty、LotStamp、UserId、抽樣參數及要因。
- Service：`STD\MESws_STD\wsSTD\wsSPC\wsSPC.cs` 的 `UpdVar_TestValue` 接收新增量測；`UpdVar_LotData` 接收既有量測值修改；圖表／規則相關分支可讀取 `LoadVarQCItemChartData`、`ExecuteQCChartRuleCheck`。主表單另依 OP／QCItem／Lot／抽樣計畫／要因查詢使用 `wsOP`、`wsQC`、`wsWIP`、`wsPRD`、`wsSYS`。
- Core：`STD\MESkc\kcSPC\clsSPC.cs` 的 `UpdVar_TestValue` / `pvtUpdVar_TestValue` 寫入計量量測；`UpdVar_LotData` 處理修改；`InitSPCSerial` 配置量測 Serial。規格限值、公式、管制圖與批次處置尚依實際呼叫設定及整合服務。

## WinForms 流程與操作

- 主 Form 依作業站、QCItem、Lot、抽樣計畫／參數及收集要因建立子表單上下文。WIP、QCL 等入口會帶入既有 Lot／OP 並限制可修改欄位；有效性與權限需回來源服務核驗。
- 連續計量子表單於載入時檢查 `dvQCItem`、`dtFactor`、Lot，取得管制界線，建立要因與量測資料表及 Grid，並預先建立 SampleSize 的逐點輸入位置。每一 QCItem 的最大抽樣位置依設定建立。
- 量測值可手動逐點輸入，也可透過工具列匯入 CSV、TXT、XLS，或依本機 `InspMachineSetup.xml` 設定連線 RS232／檢測機台驅動；自動通訊依 channel 對應將裝置回傳值寫入下一個輸入位置。這些會連接真實裝置或本機檔案，DEV 不實作。
- Save 先檢查所有抽樣值完整且為數字，更新收集要因、計算／檢查 QC Chart Rule、規格界線、公式與 violation，彙整 QCItem 結果後呼叫 `wsSPC.UpdVar_TestValue`；WIP 入口會攜帶 LinkName、LotStamp 等回傳上下文。Core 建立 SPC Serial 並處理量測、因子、統計、限值快照及批次整合。
- Edit 需先選已儲存列並確認修改；WinForms 明示修改後不會重新判定原檢驗批結果。ConfirmEdit 呼叫 `wsSPC.UpdVar_LotData` 並記錄變更前後值；Reverse 還原原量測值。來源含圖表顯示／停用、規則檢查、異常處置、ERF、批次新增及 UI 設定存取。

## Vue DEV 復刻與界線

- fallback 路由 `/YMES/SPC/OnlineControlContinue`，位於品質資料／SPC 管制；正式環境只顯示未開放提示。
- DEV 假資料可選 OP、QCItem、Lot、母體數、Sampling Plan、SampleQty 及班別／設備／操作人員要因；確認後建立固定抽樣位置，可逐點輸入、模擬假設備填下一點、貼上逗號／分號／換行分隔值、顯示固定假規格界線結果、檢查必填數字、儲存本頁歷程，並可載入歷程進入 Edit／ConfirmEdit 或 Reverse。
- 假 OP 權限、Lot、抽樣計畫、Factor、量測值、規格界線與 Serial 都是頁面 fixture；沒有 Service/API、資料庫、設備、RS232、ERP、ERF、檔案系統或呼叫端回寫。頁面規格檢查僅示範逐點提示，不執行 MES 規則／公式／AQL 或管制圖演算法，也不重判修改後結果。
- DEV 目前只呈現標準連續計量路徑。客製 DLL、計量群組、Component/Wafer、Lot Stamp 並發、完整 WIP/QC Form／ERP／ERF 處理、圖表／管制規則、實際 CSV/TXT/XLS 格式與各設備 driver 需依家用端可取得的正式契約逐項核實。

## 資料表、Oracle metadata 與家用端 handoff

- SC102 新增量測重用 `clsSPC.UpdVar_TestValue`，使用的計量 Online／Log／Statistic／Factor／Limit 資料表依 [SC101 SPC 量測數據收集規格](SPC量測數據收集.md)所列 Core 呼叫分支核對；修改路徑另涉及量測編輯記錄。Serial 由共用 SPC Serial 路徑建立。部署時仍需沿實際 Service/Core 分支核完整讀寫表及交易邊界。
- SC101／SC102 共用計量執行表目前沒有同版 MESTEST source DDL／metadata 與 PostgreSQL CREATE-only。桌機受保護 Oracle attachment/profile 檔不存在（已只檢查存在性，未讀取或輸出其他設定）。因此本項沒有執行 Oracle 查詢，未猜欄位、PK、索引或 FK，也未產生 CREATE SQL。
- 家用端先 Pull 最新 main，逐表比對現有 YMES live schema／migrations，取得可靠 Oracle DDL／dictionary metadata 後才補必要 schema。正式 API 必須 server-side 驗證登入與 FunctionNo 授權、OP/QCItem 狀態與關聯、有效 Lot／LotStamp、核准 Factor、抽樣計畫／參數、規格／管制界線；在交易中配置 Serial 並保存量測、Factor、Statistic、Limit snapshot 與編輯 audit。
- 核清新增及修改實際原子範圍；WIP／QC Form／Lot 結果、ERF、ERP 或外部設備不能放入同一交易時，回傳階段結果並定義安全重試／補償。瀏覽器不可提供可直接採信的使用者、結果、Serial、LotStamp、SQL 欄位或公式。
- 公司端只交付 DEV UI 和來源／API handoff，不連 PostgreSQL、不實作或驗收家用端正式 API/schema、不設定 `ConnectionStrings__YMes`。

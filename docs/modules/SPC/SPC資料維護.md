# SC201 SPC 資料維護（SPC DATA）

## 選單與原始碼

- 選單識別：Module `SPC`、FunctionNo `SPC DATA`、FunctionKey `SC201`、Form `frmQueryData_THEIL_BU1`、ExecutionFile `SC_QueryData_THEIL_BU1.dll`，分類 SPC分析。
- 查詢 Form／inline Designer：`D:\iMES\CSiMES\Customer\Production\MESWin\SPC\SC_QueryData_THEIL_BU1\frmQueryData_THEIL_BU1.vb`。計數明細 Form／inline Designer：同目錄 `frmAnsAttRawData_THEIL_BU1.vb`。計量／群組明細子物件及 inline Designer：`D:\iMES\CSiMES\Customer\Production\MESWin\SPC\SC_AnsLineChart_THEIL_BU1\frmAnsVarRawData.vb`，由主 Form 的 `ObjectFileHandler.Execute` 載入。
- Service：`D:\iMES\CSiMES\Customer\Production\MESws_STD\wsSTD\wsSPC\wsSPC.cs`，核對 `LoadSPCMeasureData`、`LoadSPCMeasureData2`、`LoadSPCDataHistory`、`UpdAtt_LotData`、`UpdVar_LotData`、`ChkLotNoERFNoForDelData`、`DeleteAtt_LotData`、`DeleteVar_LotData` 及 `THEIL.wsSPC.LoadUserDeleteGroup`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcSPC\clsSPC.cs`，核對 `LoadSPCMeasureData`／`LoadSPCMeasureData2`、`LoadSPCDataHistory`、`UpdAtt_LotData`／`UpdVar_LotData`、兩種 Delete、`ChkLotNoERFNoForDelData`、`AddSPCAttDataEditLog`／`AddSPCVarDataEditLog`。

## WinForms 查詢與明細行為

- 查詢需選計數、計量或計量群組並指定 QC Item；可加 LotNo、起訖日及依 QC Item 動態載入的 Collect Factor。起訖日各自有啟用勾選。QC Item 清單只帶 `IssueState` 2／-1；有 LotNo 時另呼叫 `LoadLogQCItem`。計量群組的要因為成員 QC Item 的要因聯集。
- 查詢呼叫 `LoadSPCMeasureData`。計數結果開 `frmAnsAttRawData_THEIL_BU1`，提供 Basis、Raw Data、Data Log；計量／群組用 `SC_AnsLineChart_THEIL_BU1`，載入要因、測值、統計與資料歷程。群組模式隱藏 Edit／Save／Delete；計量明細可顯示多個 SPCSerial 或選定批次。
- 計數修改由 `SPCDATAEDITATT` 控制，計量修改由 `SPCDATAEDIT` 控制。修改說明必填。計數可改要因、CreateDate、Normal／Excluded 及各 Error 數量；報廢總數不能大於 SampleQty，GoodQty = SampleQty - ScrapQty，DefectQty 另計。計量可改要因、測值、CreateDate、RetestLotNo 及 Normal／Excluded／Isolated；Core 重算 Mean、Min／Max、樣本標準差、Cp／Cpk 及統計記錄，但不重新判定 Result。若是最新管制圖批次，來源會重繪圖表。
- 計量刪除除了 `SPCDATAEDIT`，還呼叫 `THEIL.wsSPC.LoadUserDeleteGroup` 檢查刪除群組權限。兩種刪除都先呼叫 `ChkLotNoERFNoForDelData`：非最新管制圖批次會警告，已有 ERF 也會警告並連帶刪除 ERF 資料。成功後清除目前明細並重查／重繪。Hold 且沒有 ERF 時，計數與計量明細都可透過 `WP_BRERFCreate` 補開 ERF（`ERFSource=1`、帶 SPCSerial、`RepairERF=True`）。
- Data Log 由 `TBLSPCATTEDITLOG`／`TBLSPCVAREDITLOG` 供應。計數／計量更新及刪除後 Service 寫入對應編輯歷程；本 Vue 頁只在記憶體建立 DEV 歷程。

## Vue DEV 頁與簡化界線

- DEV fallback 選單路由：`/YMES/SPC/DataMaintenance`。提供計數／計量／群組切換、QC Item、LotNo、日期、要因篩選及假查詢結果；點選批次可開 Basis、缺陷或計量統計／Raw Data、Data Log。DEV 可編輯與取消、檢查必填修改說明、驗證計數報廢量、重算計量樣本統計、模擬刪除警告與權限阻擋、補開假 ERF。
- 所有列、權限、管制圖最新批次提示及 ERF 編號都是 fixture。刪除的最新批次比較依相同 QC Item／要因值及時間近似，不代表 Core `FactorSerial` 判定；DEV 編輯不呼叫服務、不存資料庫、不真正重繪管制圖或建立 ERF。計量統計依假 USL／LSL 計算；Result 保留原值。
- Infragistics Grid、動態 `MESParameter` 選擇器、實際使用者授權、Core `FactorSerial`、資料量與多筆群組序號行為未在瀏覽器重現。未執行 build／test／browser。

## 資料表與家用端 handoff

- 來源明確存取計數 `TBLSPCATTBASISLOG`、`TBLSPCATTDETAILLOG`、`TBLSPCATTEDITLOG` 及 Tape／Section 明細；計量 `TBLSPCVARFACTORLOG`／`ONLINE`、`TBLSPCVARDETAILLOG`／`ONLINE`、`TBLSPCVARSTATISTICLOG`、`TBLSPCVARLIMITCONTENT`、`TBLSPCVARVIOLATIONCONTENT`、`TBLSPCVAREDITLOG`。CreateDate 編修及刪除亦碰觸 `TBLWIPERFCREATEDATA`、`TBLWIPCONT_SAMPLECOMPONENT`、`TBLWIPCONT_QCITEM`；計數刪除另外處理 `TBLWIPCONT_DEFECTCODE`。若有 ERF，Core 還會刪 ERF Basis、HoldReason、Disposition、LotDisposition 與 AttachFile 等資料。
- 可沿用 repo 已存的計數 [Oracle source DDL](../../../database/reference/mestest_qc_item_attributes_source_ddl.sql)／[PG CREATE-only](../../../database/reference/mestest_qc_item_attributes_create.sql)，計量 [QC205 Oracle dependencies](../../../database/reference/mestest_qc205_variable_dependencies_source_ddl.sql)／[THEIL custom Oracle DDL](../../../database/reference/mestest_qc205_theil_custom_source_ddl.sql)／[THEIL custom PG CREATE-only](../../../database/reference/mestest_qc205_theil_custom_create.sql)，以及相關 [QC210 variable measurement reference](../../../database/reference/mestest_qc210_variable_measurement_log_delete_create.sql)。需先與家用端 live schema 比對，這些檔案不是本功能完整交易的替代品。
- repo 未找到 `TBLWIPCONT_QCITEM`、`TBLWIPCONT_SAMPLECOMPONENT`、`TBLWIPCONT_DEFECTCODE` 同版 source DDL／metadata；桌機受保護 Oracle profile 不存在，因此本次未查 Oracle、未猜欄位或新增 CREATE SQL。其餘表請家用端按實際呼叫分支補核版次與欄位。
- 正式 API 需登入及分別核驗 `SPCDATAEDITATT`、`SPCDATAEDIT` 和計量刪除群組權限；查詢要限量並支援三種 QC Item、Lot/date/factor 條件及歷程。更新須參數化，並在交易內更新 SPC Online／Log／Statistic／Factor／WIP 與 audit；計數報廢量、計量統計公式與 Result 不重判語意須與來源一致。刪除需 transaction 涵蓋 SPC、WIP、ERF 子資料及 edit log，重查實際 `FactorSerial` 最新批次守門；Create ERF 則經正式 WP_BRERFCreate 契約，不由瀏覽器直接寫入。建議增加伺服器端 concurrency token，來源舊流程未見資料版本 CAS。
- 家用端先比對上述依賴的 live schema／現有 migrations 與 SC101／QC205 references，再決定 schema 工作；公司桌機未連 YMES PostgreSQL、不實作正式 API/schema。

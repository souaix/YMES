# SC101 SPC 量測數據收集（SPC ONLINE CONTROL）

## 選單與來源

- 選單識別：Module `SPC`、FunctionNo `SPC ONLIE CONTROL`（來源清冊原拼字）、FunctionKey `SC101`、Form `frmSPCOnlineControl`、ExecutionFile `SC_OnlineControl_THEIL_BU1.dll`，分類 `SPC CONTROL`。
- 主表單與內嵌 Designer：`D:\iMES\CSiMES\Customer\Production\MESWin\SPC\SC_OnlineControl_THEIL_BU1\frmSPCOnlineControl.vb`。另核對標準版 `STD\MESWin\SPC\SC_OnlineControl\frmSPCOnlineControl.vb`，此清冊項目執行 Customer `THEIL_BU1` 版。
- 收集子表單：計量 `Customer\Production\MESWin\SPC\SC_Var_MeasureData_THEIL_BU1\frmVar_MeasureData_THEIL_BU1.vb`；計數 `Customer\Production\MESWin\SPC\SC_Att_OnlineControlMeasureData_THEIL_BU1\frmAtt_OnlineControlMeasureData_THEIL_BU1.vb`；有元件的計量群組另走 `STD\MESWin\SPC\SC_Var_OnlineControlMeasureDataWaferGroup\frmVar_OnlineControlMeasureDataWaferGroup.vb`。
- Service：Customer `MESws_STD\wsSTD\wsSPC\wsSPC.cs` 的 `UpdVar_TestValue_LinkName`、`UpdAtt_TestValue`、`LoadFactorValue`、`GetSerialNo`；另依主表單使用 `wsOP`、`wsQC`、`wsWIP`、`wsPRD`、`wsSYS` 的作業站／品管項目／Lot／抽樣計畫／參數／要因 lookup。
- Core：`STD\MESkc\kcSPC\clsSPC.cs` 的 `UpdVar_TestValue`、`UpdAtt_TestValue`、`UpdVar_LotData`、`UpdAtt_LotData`、`InitSPCSerial`；Lot／ERP／儀器控制另涉及 `clsLot`、`tcSPC` 與 Customer 分支。

## WinForms 流程

- Form 可由 WIP、QCL、QC/IQC/IPQC/OQC/FQC 或 ERP 呼叫並帶入 `SenderName`、OP、Lot、QCItem、QCItemType、PopulationQty、LotStamp、QC Form 狀態等上下文；不同呼叫端會限制欄位或在完成後回寫／關閉。獨立入口需選有 SPC 權限的 OP，`funCheckUserSPCOPPriv` 由 `wsSPC.ChkUserSPCOPPriv` 檢核。
- 品管類型包括計數（0）、計量（1）、計量群組（2）、群組減項（4），並支援計數群組依序收集、重檢批、批號母體數及有 Component／Wafer 的抽樣。OP、QCItem 依 `IssueState=2` 與 OP 綁定查找；群組／減項需另外讀取成員／扣除資料。
- 收集要因來自 QCItem Factor 與參數設定，控制項依要因種類動態建立；可帶入 lot、產品／ERP、使用者之前輸入的要因及機連條件。批號有效性、LotStamp、群組共用抽樣計畫、依 Factor 切換的抽樣計畫與複檢次數會改變查詢／驗證結果。
- `btnConfirm1` 驗證 QCItem、Lot、要因、抽樣數與抽樣計畫，載入 QC／產品參數、Wafer 欄位及轉換抽樣歷程，準備收集資料。`btnConfirm` 依計數／計量、Component、群組與 QCItem 特別設定反射載入不同 DLL 表單，再把資料集與 Lot／QC Form 上下文傳入。
- 計量子表單包含逐筆量測輸入、限值／管制圖判定、公式與要因計算、RS232／檢測儀器讀取及匯入；計數子表單含 Sample/Good/Scrap/Defect 數、錯誤原因／Area、抽樣規則及部分 IQC 檔案／FTP 流程。計量儲存走 `UpdVar_TestValue_LinkName`（900,000ms timeout），計數走 `UpdAtt_TestValue`；Core 會產生 SPC Serial、因子／量測明細、統計／判定及歷程，後續可能更新 WIP QC Item／Sample Component、Lot 結果、ERF 與檢驗單／ERP。
- 群組可建立多筆關聯 SPC Serial；群組順序、Group Deduction、Union ERF、Plan Change、Wafer/Component、短期管制、儀器與客製 DLL 會改變結果。這些是連動交易，不可把「送出一筆量測」簡化成單一資料表 INSERT。

## Vue DEV 頁與簡化界線

- DEV fallback 路由 `/YMES/SPC/OnlineControl`，選單放在「品質資料 / SPC 管制」。正式環境目前只顯示未開放提示。
- DEV 頁可查找假 OP／QCItem，選計數／計量／計量群組／群組減項，編輯 Lot、母體數、複檢批與要因，選擇抽樣計畫並調整抽樣數；可進入計數缺點或計量值收集視窗，檢核必填值，模擬儀器／檔案填值，顯示規格結果及本頁收集歷程。網址可帶 `OPNo`、`LotNo`、`QCItemNo`、`QCItemType`、`SenderName` 假入口上下文。
- 假資料與 DEV Serial 僅存在頁面記憶體；不呼叫 Service/API，不讀寫 Oracle／PostgreSQL、Lot、QC Form、ERP、ERF 或使用者權限，不連線 RS232／設備，不執行量測公式、管制圖規則、AQL／抽樣引擎、Group Deduction 或外部 DLL。群組以兩個假計量項目示範分頁收集；特殊項目、Component/Wafer、FTP、機連、Union ERF、Lot Stamp／併發與正式資料回寫尚未模擬。
- DEV 的母體／抽樣數整數及抽樣數不得大於母體是頁面示範檢核；WinForms 依系統參數與品管／產品規則計算，不能把這項簡化當正式抽樣政策。

## 資料表與 Oracle metadata

- 已直接核對 Core 寫入／更新的計量表：`TBLSPCVARDETAILONLINE`、`TBLSPCVARDETAILLOG`、`TBLSPCVARSTATISTICLOG`、`TBLSPCVARFACTORONLINE`、`TBLSPCVARFACTORLOG`、`TBLSPCVARLIMITCONTENT`、`TBLSPCUSERFACTORVALUE`。計數核心至少寫 `TBLSPCATTDETAILLOG`、`TBLSPCATTBASISLOG` 與要因值表；`InitSPCSerial` 使用 `TBLSYSSERIALNO` 或 Oracle sequence。
- 批號／檢驗單整合可更新 `TBLWIPCONT_QCITEM`、`TBLWIPCONT_SAMPLECOMPONENT`、`TBLWIPERFCREATEDATA`，並依結果呼叫 Lot／ERF／ERP 路徑。所有讀寫表需由家用端沿實際呼叫分支逐表核對，不能只依 DEV fixture 視為完整依賴清單。
- repo 目前沒有上述 SC101 量測 Online/Log/Statistic/Factor 的同版 MESTEST source DDL／metadata 與 PostgreSQL CREATE-only；現有 QC sampling、QC item、QC factor、rule／limit 的 references 只覆蓋設定表或其他功能，不足以推導 SC101 的執行表鍵、欄位長度、PK、索引與 FK。公司桌機受保護 Oracle profile 不存在，本次未查 Oracle，也未依 Core SQL 猜 CREATE SQL。`TBLSYSSERIALNO` 沿用共用系統序號資料表 references，若家用端需新建 Oracle sequence，須先確認實際部署規則及權限。
- Core 有 SQL 字串組合與多表 CollectionSQL；helper 實作及組合交易邊界沒有由本次可讀來源完整確認。另有 SPC Serial／Factor Serial 與 LotStamp 併發條件，家用端須對照實際 MESTEST metadata 及 service helper，不可直接照搬 SQL 字串或假定全流程原子。

## 家用端 API／schema handoff

- 先 Pull 最新 main，再對每個收集模式追 Service／Core 呼叫與資料表，取得有來源依據的 Oracle DDL／dictionary metadata；確認既有 YMES migration 與 live schema。未取得 DDL 前不新增 SC101 Online/Log/Statistic/Factor tables，也不以 C# SQL 欄名推測 PostgreSQL schema。
- 提供登入與 `SPC ONLIE CONTROL` FunctionNo 授權的 context／lookup API：OP 可見與 SPC 權限、有效 Lot/OP/產品、核准 QCItem 與 OP 關聯、FactorOnline、抽樣計畫／參數、目前管制界線、重檢批／群組成員及 component/wafers。所有可見範圍與 `LotStamp` 必須在伺服器重查。
- 計量與計數可用分型提交 command，但必須驗證抽樣參數、QCItem／Factor 白名單、Lot 與操作人、規格／rule／限值、必需 Component 或 defect reason；SPC Serial／Factor Serial 由交易內序號服務配置，不能信任瀏覽器送入。
- 定義同一 transaction 的完整邊界與結果契約：計量 Online+Log+Statistic+Factor+Limit snapshot；計數 Basis+Detail+Factor；以及條件式 WIP QC Item／Sample Component、Lot 狀態、ERF、ERP／檢驗單更新。若外部 ERP、設備或 Issue 流程不能同交易，須回傳逐階段結果及安全重試／補償，避免重送造成重複量測。
- 儀器讀值、特殊公式／客製 DLL、Wafer／Component、AQL、group deduction、sample-plan transition、Union ERF、進料 FTP／檔案及 ERP 需依可用的正式介面逐項交付。公司端僅完成 DEV 與來源規格，不連 YMES PostgreSQL、不實作或驗收正式 API/schema。

# SC105 PQC 檢驗單（IPQC FORM）

## 選單與來源

- 選單識別：Module `SPC`、FunctionNo `IPQC FORM`、FunctionKey `SC105`、Form `frmIPQCForm`、ExecutionFile `SC_IPQCForm.dll`，分類 `SPC管制`。
- 主 Form／inline Designer：`D:\iMES\CSiMES\STD\MESWin\SPC\SC_IPQCForm\frmIPQCForm.vb`；新增／編輯／複製定義 Form／inline Designer：`frmIPQCFormDef.vb`；品管項目對應 Form：`frmIPQCFormQCItemMap.vb`。
- Service：`STD\MESws_STD\wsSTD\wsSPC\wsSPC.cs` 的 `LoadIPQCForm`、`AddIPQCForm`、`EditIPQCForm`、`DelIPQCForm`、`SetIPQCFormStatus`、`LoadIPQCFormQCItemMap`、`AddIPQCFormQCItemMap`、`DelIPQCFormQCItemMap`；定義畫面另呼叫 `wsPRD.LoadPRDQCItemLotValid`、`wsWIP.LoadLotBasisJoinState`、`wsPRD.LoadPRDOP`、`wsSYS.GetIPQCFormNo`。
- Core：`STD\MESkc\kcSPC\clsSPC.cs` 的 `LoadIPQCForm`、主單 CRUD、QC Item Map CRUD 與 `UpdIPQCFormQCItemMapResult`；`STD\MESkc\kcBasis_UPD\clsBasis_UPD.cs` 的 `SetIPQCFormStatus` 使用 `DataStamp` 更新狀態及 `Reviser`／`ReviseDate`。`LoadIPQCForm` Left Join 產品名稱、客戶名稱及 WIP Lot 資料。

## WinForms 狀態與互動

- 主 Form 依 Status 分六個頁籤：0 未確認、1 待檢、2 檢驗完成、3 允收、4 拒收、5 特採。查詢條件含建立日期區間及 LotNo；清單顯示 QCFormNo、LotNo、產品／Module、名稱、版本、OPNo、Qty、客戶、ModuleType、`S_OffLineHold`、狀態與修改資料。各頁依選取主單顯示 QC Item Map；五個狀態頁都有 Copy。
- 未確認頁有 Add／Edit／Copy／Delete／Confirm 與 Add／Remove QC Item。Def Form 要求產品／Module、Qty>0、OPNo、ProductVersion、LotNo；有效批號 lookup 帶入 Lot、OP、Product 或 Module、版本、客戶及數量。`ModuleType=0` 為 Product、`1` 為 Module Info；依 Product／版本／OP 透過 `LoadPRDQCItemLotValid` 預載品管項目。`S_OffLineHold` 停用時存 0、啟用時存 1；原生畫面只在 `SEMI_WAFER` 模組顯示此欄。
- Confirm 以 DataStamp 將 Status 0 設為 1；WinForms handler 沒有先檢查 Map 數量。待檢頁可對選取 QC Item 開 SPC Online Control。每個 Item 完成後寫 Map Status／Result／SPCSerial；最後一個待檢項目完成時 Core 將主單 Status 設 2 並記 FinishTime。若 Map 全部未開始，待檢頁 Del 可刪除主單及 Map；已開始任何一項時停用 Del。
- QC Item Map Form 可取 `QCItemType` 0 計數屬性、1 計量、2 群組、3 屬性順序群組。Reject 的自動 HOLD／WAIT 規則只檢查 type 0／1 的指定 Result；群組型別不直接套此判定。
- 檢驗完成頁支援 Receive、Reject、Concession，分別將狀態設為 3、4、5。只有 `CallBackType='SFT'` 會啟用數量回報並呼叫 SFT `ipqc` callback。數量守恆為 `Good + Defect + Return + Rework + Unknown - Surplus = Qty`；callback 結果代碼 2／3／4 對應允收／不良／特採。非 SFT 單據不顯示可編輯的 SFT Qty 回報欄。
- Reject 有 `S_OffLineHold=1` 且 Lot 有效時，會查看 Map 結果：計數型 Item (`QCItemType=0`) Result=3 或計量型 (`1`) Result=1 走 HOLD；型別 0／1 Result=2 走 WAIT。接著依 Lot Status／OPStatus 分支呼叫 WIP 異常／等待流程（包含 `WP_BRERFCreate`、立即／未來 Hold 或 Wait）。這些外部副作用會影響批號與異常單，不是單純 PQC Status 更新。

## Vue DEV 頁與簡化界線

- 路由 `/YMES/SPC/InProcessQCForm`，選單名稱「PQC 檢驗單」；正式環境只顯示未開放提示。
- DEV 提供六個 Status 頁籤、關鍵字與建立日期篩選、單據清單及 QC Item 明細。未確認可 Add／Edit／Copy／Delete／Confirm，依 DEV Lot／Product／Module fixture 帶入基本欄位及 QC Item，並可調整 Map；fixture 覆蓋 QCItemType 0／1／2／3，lookup eligibility 仍由正式端依產品／版本／OP 驗證。待檢頁可逐項模擬 SPC 結果；全部完成後進入檢驗完成，未開始檢驗的待檢單可刪除。
- 檢驗完成頁可模擬允收／拒收／特採；SFT fixture 顯示六類數量並驗證來源數量公式。拒收時可展示 `S_OffLineHold` 對 WIP HOLD／WAIT 分支的說明提示；不呼叫 SFT／WIP／ERF，也不更新外部狀態。DEV 一律展示 `S_OffLineHold` 欄位，方便操作；WinForms 只在 SEMI_WAFER module 顯示。
- 單號、產品／Module／Lot／QC Item、CallBackType、DataStamp、SPC Serial、結果、數量與所有狀態都是本頁記憶體假資料。DEV 不呼叫 MES Service/API、SPC 量測設備、Oracle／PostgreSQL、SFT 或 WIP。Keyword 篩選是本機便利操作；WinForms 查詢輸入是 LotNo 與可選建立日期。
- DEV 不實作正式序號 reservation、產品與 Lot eligibility、ModuleInfo 核對、QC Item rule、SPC／儀器、SFT callback、WIP Hold／Wait 交易、ERF 建單或逾時補償；UI 守門不代替 server 驗證。

## 資料表、Oracle metadata 與家用端 handoff

- Core 主表為 `TBLSPCIPQCFORM`，Map 表為 `TBLSPCIPQCFORMQCITEMMAP`。主表包含 QCFormNo、ProductNo、Qty、Status、LotNo、CustomerNo、Creator／CreateDate、OPNo、ProductVersion、FinishTime、CallBackType、`S_OffLineHold`、DataStamp、Reviser／ReviseDate、ModuleType。Load 另 Join `TBLPRDPRODUCTBASIS`、`TBLENTCUSTOMERBASIS` 與 `TBLWIPLOTBASIS`。
- repo 已有主表 MESTEST Oracle source DDL：`database/reference/mestest_wip_wp201_unresolved_source_ddl.sql`，同主表的 PostgreSQL CREATE-only 位於 `database/reference/mestest_wip_wp201_create.sql`。它是既有 WP201 handoff reference，家用端須先比對這次功能同版欄位用途與 live schema，不要重複建表。repo 尚無 `TBLSPCIPQCFORMQCITEMMAP` 的同版 Oracle source DDL／metadata 或 PostgreSQL CREATE-only；桌機受保護 Oracle attachment/profile 不存在，本次未查 Oracle，也未猜 Map 欄位／PK／索引或輸出 CREATE SQL。
- 家用端 Pull 後先核對既有主表 CREATE 與 live migration；取得 Map 的可靠 Oracle dictionary metadata 後才新增必要 CREATE-only。正式 API 須驗登入／FunctionNo 權限、ModuleType/Product／Module、Lot、OP、ProductVersion、QC Item eligibility、序號 reservation 與 DataStamp compare-and-set；所有 SQL 參數化並限制查詢筆數。主單＋Map、新增／編輯／刪除、Confirm、待檢未開始刪除、SPC Map Result 及最後 Item 自動完成需明確交易邊界。
- SFT Qty 分類及數量公式需由 server 重新驗證。SFT 外部 callback 需保存 idempotency key、request／response／狀態、重試與對帳結果；不可在 callback 未成功時先更新本機結果。`S_OffLineHold` 的拒收副作用須確認 WIP Lot Status／OPStatus 與 Hold／Wait 操作順序，記錄 ERF/WIP 部分成功與補償；公司端 DEV 不呼叫任何外部服務。
- 公司桌機只交付 DEV UI 與 API／schema handoff，不連 YMES PostgreSQL、不實作或驗收正式 API／schema、不設定 `ConnectionStrings__YMes`。

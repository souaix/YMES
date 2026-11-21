# SCRAPSTOCKIN_E10ICD 不良品入庫至 E10ICD ERP（WP622）

## 來源與範圍

- 選單清冊 FunctionNo：`SCRAPSTOCKIN_E10ICD`；清冊標示未掛入 `MenuItem`。公司端 fallback DEV 選單另提供入口，並標註 DEV，不表示正式選單或 ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_ScrapStockIN_E10ICD/frmInventory_ScrapStockIN_E10ICD.vb`，含 inline `InitializeComponent`／Designer。
- ERP Service：Customer production `wsERP_E10ICD.Get_ERP_Doc_Type`、`ERP_Stock_IN`、`ERP_Stock_IN_Cancel`、`LoadMOComponent`。
- MES Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `loadWIP_SEMI_MO_TP`、`LoadWIPInventory_SEMIbyMO_TP`、`ChkmesStockIn_Lot`、`MOStockIn_Scrap_TP`、`updateINVBookingFlag`。
- Core：`STD/MESEAI/EAItc/clsWIP.cs` 的不良 MO／Lot／Serial／Error／BookingFlag 查詢和預約；`STD/MESEAI/EAItc/clsINV.cs` 的 `MOStockIn_Scrap_TP`、`MOStockIn_Scrap_TP_Component`、`MOStockIn_Scrap_TP_FailBin`；`STD/MEStc/tcERP/clsERP.cs` 的 `LoadStockInLotInfo_E10ICD_Scrap`、`warehouse_process_e10icd`。

## WinForms 流程

1. 載入時呼叫 `Get_ERP_Doc_Type`，傳 `DocType=2` 取得 ERP 不良品單別；載入可用不良品庫及 MO。MO 候選有 `TBLINVWIP_SCRAP`、`TBLINVWIP_COMPONENTSCRAP` 或 `TBLINVWIP_FAILBIN` 資料，且 MO 在狀態 3／6、`FromERP=1`。`Ref. Lot No` 輸入後按 Enter 會呼叫 `wsWIP.LoadLotBasisJoinState`，以 Lot 找 MO 再重載不良品清單。
2. MO 查詢回傳 Lot／LotSerial／OP／Source／Qty／Unit／Inventory 等父列，並以 Relation 展開不良原因子列；Form 同時載入 Component Scrap、Fail Bin、產品／MO 屬性與 Bin 定義。初始只顯示 `BookingFlag=0`。
3. `Source=0` 是一般 Lot Scrap，Qty 取 ScrapQty；`Source=1` 是 Component Scrap，Qty 為元件數；`Source=2` 是 Fail Bin，Qty 為依 `TBLSYSBIN.BinOrder` 動態欄位加總。`chkScrapStockInOrNot` 未勾時只允許 Source=1／2，勾選才納入 Source=0。
4. 入庫量不可局部輸入，勾選後以整個 LotSerial 入庫。若 BoxNoFlag／DateCodeFlag 啟用則必填；同一 Lot 多個 Serial 的 BoxNo 與有效 DateCode 必須一致。可空白儲位；ERP 倉庫／儲位先依 Inventory／Locator ERPNo 對照。
5. Confirm 先送 `ChkmesStockIn_Lot` 防重查核並預約 BookingFlag，再呼叫 ERP `ERP_Stock_IN`，傳 `RequestType=1`、`auto=N`，建立 `wo.scrap.data.create.e10icd`。與 WP621 良品不同，ERP 回傳 `code=W` 不視為成功；Service 只有 RequestType=0 良品分支接受 W，Scrap 分支要求 code=0。
6. ERP 建單成功後呼叫 MES `MOStockIn_Scrap_TP`。Core 建立 SCRIN basis/detail，累加不良庫數量；依 Source 刪除 Lot Scrap，或將 Component／FailBin 搬入 SCRIN 明細與目的庫；更新反悔歷程 CommitState，符合條件時結批。
7. MES 入庫失敗時 Form 呼叫 `ERP_Stock_IN_Cancel`；Confirm finally 以 `updateINVBookingFlag` 將整列相關 Lot Scrap、Component Scrap、FailBin 的預約重設。Form 對所選列傳 `AllIn=True`；MES 檢查與預約按 LotSerial 查多個不良來源，正式端必須防止跨來源誤鎖／誤解鎖。

## 表與 Oracle metadata

來源明確使用 `TBLINVWIP_SCRAP`、`TBLINVWIP_COMPONENTSCRAP`、`TBLINVWIP_FAILBIN`、`TBLINVSCRINBASIS`、`TBLINVSCRINDETAIL`、`TBLINVSCRINCOMPONENT`、`TBLINVSCRINFAILBIN`、`TBLWIPREVERSEHISTORY`、`TBLWIPCONT_ERROR`、`TBLQCREASONBASIS`、`TBLWIPCONT_RESOURCE`、`TBLWIPLOTLOG_REPORT`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLPRDPRODUCTBASIS`／Property、`TBLSYSBIN`、`TBLOEMOBASIS`、`TBLOPGROUPDETAIL`、Inventory／Locator basis 等。

- 既有 Oracle DDL／CREATE-only 沿用 WP201（Lot Scrap、LotLog Report）、WP603（Component Scrap、FailBin、SCRIN Basis／Detail／Component／FailBin、WIP Error）、WP215（Reverse History）、QC Reason、WP403（Resource）、WP101、OE、PRD、OP、SMD 與 PRD Bin references；本項不重複產生既有表的 CREATE-only。
- Core 還明確寫入／更新以下三張目的表，但 repository 沒有其 Oracle DDL／metadata：`TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN`。公司桌機目前無可用受保護 Oracle profile，筆電 attachment 也不在本機；因此未連 Oracle、未猜欄位、未建立 CREATE-only。取得桌機安全 profile 後，只讀查三表 DDL、欄位、索引、約束及 trigger，再補交接。
- FailBin 表與目的 SCRIN FailBin 的 Bin 欄依 `TBLSYSBIN.BinNo`／`BinOrder` 動態讀寫；不可把當前假 Bin 集當固定 schema。
- `MOStockIn_Scrap_TP` 組 `FinalCollectionSQL` 後呼叫 `ExecuteSQLNoneQuery_UPD`；transaction 範例已註解。是否原子涵蓋 SCRIN、數量累加、來源刪除、Component／Bin 移轉、反悔歷程與結批，需家用端查 helper 實作後確認。

## Vue DEV 頁

- Fallback 路由：`/YMES/WIP/ScrapStockInE10Icd`；FunctionNo `SCRAPSTOCKIN_E10ICD`。原生 Function 未掛 MenuItem，DEV fallback 入口不代表正式授權。
- 提供合成 MO、ERP 單別、不良品庫（InventoryType=3）、可空儲位、Ref. Lot No 快速載入、Source 0／1／2 篩選、LotSerial 勾選、BoxNo／DateCode、Error 原因明細、Component Scrap 與 Fail Bin 明細。
- 模擬 ERP／MES 成功、ERP W 非成功、ERP 建單失敗、MES 失敗後 ERP 取消與預約釋放。沒有部分入庫量欄位。
- 僅 `import.meta.env.DEV && developmentPreview` 開啟互動；Production 無假資料，不呼叫 E10ICD／MES Service、不連資料庫。真實 ERP payload 組裝、ERP 品號／倉庫對照、動態 Bin 定義、MES 列級副作用和取消逾時均待正式端實作。

## 家用端 API／資料 handoff

- 先對照 PostgreSQL live schema、migration 與 YMES 擴充欄，沿用 WP201／WP603、WP215、QC／OE／PRD／OP references。三張目的表 `TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` 尚缺 Oracle metadata；取得後再比對 live 欄位，不能直接套 Oracle DDL或猜 Bin 欄。
- 建議讀取 API 提供符合 MES state／FromERP／可用不良 Lot／Serial、原因、Component、FailBin、產品／MO、ERP Doc Type 與 Inventory／Locator 資料；Create command 接收 MO、Inventory、Locator、ERP Doc Type、所選 Serial、Source、BoxNo／DateCode 及 idempotency key。伺服器需重查 Qty／Source／BookingFlag／LotSerial／MO／庫別型別、ERP 品號與所有動態 Bin，不能信任瀏覽器傳入量或 Bin 數。
- 交易需預約所選 Serial 對應的原來源列，並確保只鎖／解鎖命令實際持有的來源；目前舊 Core 會以 LotSerial 跨查 Source 0／1／2 並傳 `AllIn=True`，應以來源 row version／明確來源鍵修正並記錄相容差異。
- ERP 呼叫要防止重送及 timeout unknown outcome；僅 code=0 可以進 MES，MES 失敗要記錄 ERP 取消結果，取消失敗需保留可恢復人工處理狀態。以交易或可靠補償維持 SCRIN basis/detail、目的 Scrap Inventory、Component／FailBin、原 WIP rows、BookingFlag、ReverseHistory 與 Lot 結批一致。
- 公司桌機只提供 DEV／handoff，未連 PostgreSQL、未實作或驗收正式 API/schema、未 SSH 部署。

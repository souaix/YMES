# FGDSTOCKIN_E10ICD 入庫至 E10ICD ERP（WP621）

## 來源與範圍

- 選單清冊 FunctionNo：`FGDSTOCKIN_E10ICD`；清冊標示未掛入 `MenuItem`。公司端 fallback DEV 選單另提供入口，名稱加註 DEV，不代表正式選單已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_E10ICD/frmInventory_FGDStockIN_E10ICD.vb` 及 `.designer.vb`。主要控制項為 MO、ERP Sheet Type、成品庫、可空白儲位、Lot Grid、Component/Bin 明細、Confirm、Close。
- ERP Service：Customer production `wsERP_E10ICD.Get_ERP_Doc_Type`、`ERP_Stock_IN`、`ERP_Stock_IN_Cancel`、`LoadMOComponent`。
- MES Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `LoadWIPInventory_SEMIbyMO_TP`、`ChkmesStockIn_Lot`、`MOStockIn_E10`、`updateINVBookingFlag`。
- Core：`STD/MESEAI/EAItc/clsWIP.cs` 的工單候選與 BookingFlag 處理；`STD/MESEAI/EAItc/clsINV.cs` 的 `MOStockIn_E10`／`MOStockIn_TP_Component`；`STD/MEStc/tcERP/clsERP.cs` 的 `LoadStockInLotInfo`、`warehouse_process_e10icd_stockin`。

## WinForms 流程與守門

1. Form 載入時呼叫 `Get_ERP_Doc_Type` 取得 E10ICD 入庫單別。使用者選取 MO 後，查詢良品 WIP 庫存及關聯產品屬性、MO 屬性、Component、GoodBin、包裝關係和包裝規則。
2. Core 的 MO 候選以 `TBLINVWIPINVENTORY_SEMI` 中 Qty>0 的批次連結 `TBLWIPLOTBASIS`，再限制 `TBLOEMOBASIS.MOState IN (3,6)` 且 `FromERP=1`。`LoadStockInLotInfo` 另外要求 LotState Status 11 或 99，並帶出產品 ERP 品號、OP、廠別與單位等資訊。
3. Grid 只顯示 `Qty>0` 且 `BookingFlag=0` 的列；使用者勾選 Lot 並輸入 `InInvNum`。入庫量必須大於 0 且不得高於 Qty。若 `BoxNoFlag`／`DateCodeFlag` 啟用，欄位必填；相同 Lot 的多列 BoxNo、有效 DateCode 必須一致。
4. 可選包裝編號必須是最高層包裝；包裝內來源 Lot 必須全部包含於本次勾選。儲位可空白，空值送 ERP 時轉為 `N/A`，實際 ERP 儲位則依 Inventory／Locator ERPNo 對照。
5. Confirm 先以 `ChkmesStockIn_Lot` 重查重複入庫及預約狀態，再依來源設 `auto=N` 呼叫 ERP `ERP_Stock_IN`，`RequestType=0`，回傳 E10ICD 入庫單號。ERP `code=W` 視為成功警告並繼續 MES 入庫。
6. ERP 建單成功後呼叫 `MOStockIn_E10`，以回傳單號寫 MES 入庫單及 ERPNo，再更新成品庫存、扣減 WIP 數量及 LotState CurQty／SysQty、移轉 Component／Bin、符合條件時結批、更新產品操作與下線歷程、清除已入庫批次的包裝資訊。
7. MES 入庫失敗時 Form 呼叫 `ERP_Stock_IN_Cancel` 補償取消 ERP 單；Confirm finally 呼叫 `updateINVBookingFlag` 釋放全批入庫所設的 BookingFlag。來源是跨 ERP／MES 的兩段式流程，並非單一資料庫交易。

## 表與 Oracle metadata

Core 明確引用的主要表包含：`TBLINVFGDINBASIS`、`TBLINVFGDINDETAIL`、`TBLINVFGDINVENTORY`、`TBLINVFGDINCOMPONENT`、`TBLINVFGDINVENTORYCOMPONENT`、`TBLINVWIPINVENTORY_SEMI`、`TBLINVWIPINVENTORY_COMPONENT`、`TBLWIPGOODBINSTATE`、`TBLSYSBIN`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLOEMOBASIS`、`TBLOEMOPROPERTY`、`TBLOPGROUPDETAIL`、`TBLPRDPRODUCTBASIS`、`TBLINVINVENTORYBASIS` 及 `TBLINVLOCATORBASIS`。產品、OP、MO、庫房與 Locator 等沿用既有 reference。

- 入庫單主檔、明細、Component、成品庫 Component 沿用 WP605 的 Oracle source DDL／metadata 與 CREATE-only；成品庫存及 WIP Semi 沿用 WP201；WIP Component 沿用 WP603；GoodBin／SYSBin 沿用既有 WP605、QC409、PRD Bin refs；Lot、MO、產品、OP、Locator 沿用 WP101、OE、PRD、OP／SMD refs。本項不重複產生既有表的 CREATE-only。
- `TBLINVFGDINBIN` 是 `clsINV.MOStockIn_E10` 明確讀寫的 Bin 入庫表，但目前 repository 沒有其 Oracle source DDL／metadata。公司桌機此交接點沒有可用的受保護 Oracle profile，筆電 attachment 也不在本機；因此未連 Oracle，未猜欄位或建立 CREATE-only。取得桌機受保護 Oracle profile 後，只讀查該表 DDL、欄位、索引、約束及 trigger，再補家用端 handoff。
- Core 會依 `TBLSYSBIN.BinNo` 動態增加 `TBLINVFGDINBIN` 欄位；GoodBin 缺列時不一定走同一更新分支。不要把動態 Bin 欄位名當固定欄位清單。
- `MOStockIn_E10` 以 `CollectionSQL` 批次呼叫 `ExecuteSQLNoneQuery_UPD`；同方法內的 transaction／commit 範例已註解。Helper 是否提供等價原子性需由家用端沿執行器確認，不能假定整組 WIP、庫存、Component、Bin、結批與包裝變更必然原子。

## Vue DEV 頁

- Fallback 路由：`/YMES/WIP/FgdStockInE10Icd`；FunctionNo `FGDSTOCKIN_E10ICD`。因原生 Function 未掛 MenuItem，此項僅加入本機 DEV fallback，不宣稱正式 ACL／選單已具備。
- 提供合成 MO／WIP Lot、ERP 單別、成品庫／可空儲位、Lot 勾選、可入庫量編輯、條件式 BoxNo／DateCode、Bin 與 Component 明細。
- Confirm 可操作模擬 ERP+MES 成功、ERP W 警告後續行、ERP 建單失敗，以及 MES 失敗後 ERP 取消與 BookingFlag 釋放；成功情境更新本頁假可入庫量及紀錄。
- 僅 `import.meta.env.DEV && developmentPreview` 開啟互動。Production 不載入假資料，不連 E10ICD、MES Service 或資料庫。
- 為保持 DEV 範圍，單次查詢限制一個 MO；包裝階層完整驗證、ERP 單別即時查詢、ERP 品號／庫房／儲位對照、動態 Bin 欄、實際 WIP／FGD／Component/歷程更新及網路逾時／重送均只以明確狀態或假資料呈現，未代替正式交易。

## 家用端 API／資料 handoff

- 先核對 YMES PostgreSQL live schema、migration 與資料；保留現存 YMES 擴充欄位。重用 WP605／WP201／WP603 及 OE、PRD、OP、SMD 的表，不因本功能另造重複主檔或 Component 表。
- 取得並核對 `TBLINVFGDINBIN` Oracle metadata 後，再決定其 live schema 欄位對照；不得直接套 Oracle DDL，也不能猜動態 Bin 欄。
- 建議讀取 API 分開提供符合來源條件的 MO 候選、WIP Lot、Component／Bin、ERP 單別及 Inventory／Locator lookup。Command 接收 MO、成品庫、可空儲位、ERP 單別及選取 Lot 的數量、BoxNo、DateCode；伺服器必須重查 Qty、狀態、BookingFlag、同 MO／廠區、包裝完整性、庫房類別及 Component／Bin，不能信任瀏覽器提交的狀態或 Bin 數量。
- 寫入端應有 idempotency key 與庫存預約／並發鎖，避免重送 ERP 建單；ERP timeout／unknown outcome 不可直接再建單。保留 ERP `W` 警告可繼續 MES 的來源語義；MES 失敗需要可靠補償或可恢復的 saga 記錄，並在 ERP 取消失敗時保留可人工處理的狀態。
- 在確認 Core SQL batch 的 atomicity 後，讓 MES 庫存、入庫單、Lot Qty／SysQty、Component、Bin、結批、產品操作、下線歷程與包裝資料同步提交／回滾。公司桌機僅提供此 handoff，未連 YMES PostgreSQL、未實作或驗收正式 API/schema。

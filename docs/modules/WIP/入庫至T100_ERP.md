# 入庫至 T100 ERP（WP616）

## 來源與範圍

- 選單清冊 FunctionNo：`FGDSTOCKIN_T100`；目前未掛正式 `MenuItem`。公司端 fallback DEV 入口不表示正式選單／ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_T100/frmInventory_FGDStockIN_T100.vb` 與 `.designer.vb`。
- ERP Service：Customer production `wsERP_T100.Get_ERP_Doc_Type`、`ERP_Stock_IN`、`ERP_Stock_IN_Cancel`。
- MES Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `LoadWIPInventory_SEMIbyMO_TP`、`ChkmesStockIn_Lot`、`updateINVBookingFlag`、`MOStockIn_TP`。
- Core：`STD/MESEAI/EAItc/clsWIP.cs` 的 Lot Reservation；`STD/MESEAI/EAItc/clsINV.cs` 的 `MOStockIn_TP`／`MOStockIn_TP_Component`、`funGetSysQty`；後續 Lot Close／Product Operation／Lot Release Log。

## WinForms／ERP 流程

1. 載入時呼叫 `Get_ERP_Doc_Type(DocType=1)` 取得 T100 入庫 Sheet Type；目的庫以 InventoryType=1 選擇，儲位可空白；來源候選由 `LoadWIPInventory_SEMIbyMO_TP` 取得 InventoryType=6 的線邊庫 Lot。
2. MO 選擇器支援多 MO。Ref. Lot No 按 Enter 先以 `wsWIP.LoadLotBasisJoinState` 找 MO，再重載其線邊庫。兩個 Grid 分別顯示 `BookingFlag=0` 的可用 WIP Inventory 與待入庫 Lot；Select All、Clear、上下移動及 LotNo／OrgLotNo 掃描 Select／UnSelect 均操作整列，沒有部分 Qty 欄位。條碼掃描會以來源 InventoryNo 和 LotNo 定位。
3. Confirm 必填 Sheet Type、MO、目的庫與至少一筆 Lot。先由 `ChkmesStockIn_Lot` 重查每列來源 WIP Inventory／工單且 `BookingFlag=0`；前端將 `AllIn=True`，Core 對被選 Lot 設預約 `BookingFlag=1`。
4. 目前使用中的整合路徑呼叫 `wsERP_T100.ERP_Stock_IN`，傳 `RequestType=0`、ERP Doc Type、目的 Warehouse／Storage、登入員工及 Lot 清單；T100 Service 組 `stockin.data.create`，ERP 回傳單號後填入 `FGDInNo`。Confirm 雖設定區域變數 `auto=N`，但目前 `erpStockIn_T100_Integration` 未使用或序列化該參數；需確認 T100 service default 的確認／過帳行為。合成 ERP 清單欄位包括 Lot、Product、版本、Qty、Unit、來源 Inventory、日期與 BaseLot；`OrgLotNoToERP=1` 時只把送 ERP 的 LotNo 改為 OrgLotNo，MES 仍用實際 LotNo。每列 ERP locator 傳 N/A；選取的目的 Storage 另傳給 ERP。
5. `wsERP_T100.ERP_Stock_IN` 對良品 `code=0` 成功；`code=W` 轉成服務 `Result=success` 並附警告，因此 Form 顯示警告後仍繼續 MES 入庫。其他 ERP code 失敗，不執行 MES、清除暫選並重載 Lot。
6. ERP 成功後呼叫 `wsERP_MES.wsMES.MOStockIn_TP`，傳 ERP FGDInNo、MO、目的庫、全量 Lot 明細及 `ERPName=T100`。MES 失敗會呼叫 `ERP_Stock_IN_Cancel`；Confirm finally 會釋放預約。來源取消結果僅顯示訊息，沒有持久化補償／對帳狀態，ERP timeout 或取消失敗的結果可能不明。
7. `btnGetStockInNo` 在 Form 已隱藏並無功能。現行整合回傳 EAI `datainfo.parameter` 作 `FGDInNo`；不等同 Form 前端手動取得新單號。

## MES Core 更新

`MOStockIn_TP` 建立 `TBLINVFGDINBASIS`（State=2、Source=1、ERPNo、FromInventoryNo）、逐列 `TBLINVFGDINDETAIL`、更新／新增 `TBLINVFGDINVENTORY`、扣減及清除 `TBLINVWIPINVENTORY_SEMI`、更新 Lot `CurQty`／`SysQty`。另由 `MOStockIn_TP_Component` 搬入良品庫元件庫存，檢查剩餘 WIP 後結批，更新 Product／Operation 並記錄 release log action 13。Core 將 SQL 加入 `CollectionSQL` 後呼叫 `ExecuteSQLNoneQuery_UPD`；transaction 範例已註解，原子性待家用端核查。

直接表 DDL／CREATE-only 已有 WP605 的 `TBLINVFGDINBASIS`／Detail／Component／FGD Inventory Component references，與 WP201 的 FGD Inventory／WIP SEMI Inventory／Lot references；本項沒有新增缺表或猜測欄位。此桌機沒有可用受保護 Oracle profile，本項未連 Oracle；也未連 YMES PostgreSQL。

## 舊／目前整合差異

- Form 仍保留未被 Confirm 呼叫的舊 `erpStockIn_T100`。它有 `defERPUnitNoProperty`、MO Property 與其它 legacy XML 組裝；現行 `erpStockIn_T100_Integration` 沒呼叫它。`TPERPProcessCost` 在 Confirm 讀取後亦未被現行方法使用。不要把舊路徑欄位／行為當成目前交易規則。
- Form 會以 T100 ERP 單號當 MES `FGDInNo`。ERP 已成功、MES 失敗時會嘗試取消，但若取消失敗仍在 finally 重設 Reservation；正式 API 必須保留 ERP 建立／取消的獨立狀態，不能只依 BookingFlag 判斷可安全重送。

## Vue DEV 頁

- Fallback 路由：`/YMES/WIP/FgdStockInT100`；FunctionNo `FGDSTOCKIN_T100`。
- 合成 T100 Doc Type、MO、成品庫、儲位、來源庫與 Lot。模擬參考 Lot、完整 Lot 掃描 Select／UnSelect、雙 Grid 選取／全選／清除、BookingFlag、ERP success／W warning／failure、MES failure 後 ERP cancel success／failure。
- DEV 將來源 Form 的多 MO 選擇簡化為單一 MO；不呼叫 T100／MES Service、不連資料庫。Production 不會載入假資料。

## 家用端 API／資料 handoff

- 讀取 API 載入授權可用 T100 Sheet Type、MO、InventoryType=1 目的庫／Locator、InventoryType=6 線邊 Lot／元件與 Lot state。Create command 使用 idempotency key，伺服器重查每列 Lot、實際來源庫、Qty／Unit、MO、BookingFlag、目的庫及版本，不信任前端數量或 OrgLotNo 設定。
- 以所選 WIP Inventory rows 原子預約；ERP 建單需可查詢／冪等重送並保留 `code=W` 警告及 ERP 單號。MES 僅在確定 ERP 單號後執行；MES 失敗先以 ERP 單號呼叫取消，將取消結果與 unknown outcome 寫入可恢復處理記錄，不能只重設 BookingFlag。
- MES 本地交易涵蓋 FGD Basis／Detail／Inventory、WIP SEMI Inventory、Component、Lot Qty、Lot Close／Product Operation／Release Log。先核查 helper 交易原子性並比對 PostgreSQL live schema／migration；公司桌機未實作或驗收正式 API／schema。
- 確認 `FGDSTOCKIN_T100` 的正式 ACL／MenuItem；清冊目前標示未掛入 MenuItem。

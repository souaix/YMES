# IV302 開立退料單（WIP）

## 來源與範圍

- 功能鍵／選單：`IV302`／`WIP MATERIAL RETURN`／「開立退料單(WIP)」；路由 `/YMES/INV/MaterialWipReturn`。
- WinForms：`D:\iMES\CSiMES\STD\MESWin\INV\IV_MaterialWIPMOReturn\frmINVMaterialWIPReturn.vb`（Form 與 Designer inline）、`frmINVMaterialWIPReturnBasisDef.vb`、`frmINVMaterialWIPReturnDetailDef.vb`。
- Service：`D:\iMES\CSiMES\STD\MESws_STD\wsSTD\wsINV\wsINV.cs` 的 `Load/Add/Edit/DelINVMaterialWIPReturnBasis`、`Load/Add/Edit/DelINVMaterialWIPReturnDetail`、`LoadINVMaterialWIPReturnComp`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcINV\clsInventory.cs` 的 IV302 單頭／明細／元件查詢與寫入方法，以及 `clsBasis_UPD` 的 DataStamp 更新。
- 公司端已用 DEV 假資料完成 UI 與可模擬互動；不連 YMES PostgreSQL、不實作正式 API/schema、不部署。

## 主畫面

- Form 開啟時呼叫 `funLoadMaterialWIPReturnBasis`，固定帶 `State=0`，另外接受系統查詢欄位 `MaterialReturnNo`；結果依單號排序。畫面只列 UnConfirm 草稿，沒有單頭確認按鈕。
- 上方 Basis Grid 顯示 `MaterialReturnNo`、`FromPlaceType`、`FromInventoryNo`、`InventoryNo`、`State`、`Description`、`Creator`、`CreateDate`、`Reviser`、`ReviseDate`。`DataStamp` 隱藏供更新使用。
- 下方 Detail Grid 顯示 `MaterialNo`、`MaterialLotNo`、`LocatorNo`、`ToLocatorNo`、`UnitNo`、`Qty`、`Creator`、`CreateDate`、`Reviser`、`ReviseDate`，並以 Relation 顯示元件子列 `ComponentNo`、`ComponentLotNo`、`GoodQty`、`ScrapQty` 等欄位。
- 單頭與明細都提供 Add／Edit／Delete／Copy。Edit、明細新增／修改／刪除均受 State=0 限制；單頭 Delete 可多選，逐張呼叫 Service，遇錯即停止後續列。
- 選取單頭會載入該單明細及元件子列。Basis Copy 配新單號，只複製單頭欄位、不複製明細；Detail Copy 是新明細，元件需重新選擇。

## 單頭與來源類型

`FromPlaceType` 是來源類型，Form 使用五種值：

| 值 | Form 選項 | 來源選擇 | 目標庫房 |
|---:|---|---|---|
| 2 | WIP Inventory | 在製原料庫房 | 原料庫房（`InventoryType=0`） |
| 3 | MO | 工單 | 從工單物料元件來源推得原料庫房 |
| 5 | WIP Semi | 在製半成品庫房 | 半成品庫房（`InventoryType=2`） |
| 6 | MO Semi | 工單 | 從工單來源推得半成品庫房 |
| 7 | MO Material + Semi | 工單 | Form 設為 `N/A` |

- 單頭欄位包含 `MaterialReturnNo`、`InventoryNo`、`FromInventoryNo`（Form 顯示 From Place No）、`FromPlaceType`、`Description` 及稽核欄位。
- Add／Copy 呼叫 `wsSYS.GetMaterialReturnNo_WIP`；流水號規則先查 `SerialType=MATERIALRETURNNO`，新增成功後才 reserve。Core 建立時 State 固定為 0。
- 單頭 Edit 僅允許修改 `Description`，並傳 `DataStamp`、Reviser、ReviseDate；單號、來源類型、來源與目標庫房鎖定。Copy 取得新流水號。
- Add／Copy 必須選來源類型、來源編號及目標庫房。MO 類型 3／6 從工單物料元件來源帶出庫房；類型 7 使用 `N/A`。

## 明細與元件規則

- 明細識別欄位為 `MaterialReturnNo`、`MaterialNo`、`MaterialLotNo`、`LocatorNo`；另有 `ToLocatorNo`、`UnitNo`、`Qty` 及稽核欄位。`FromPlaceNo`、`InventoryNo`、`FromPlaceType` 由 Service 傳入以處理元件來源／預約，不是明細 Grid 欄位。
- 來源批號資料依單頭 FromPlaceType 分流：2 查 WIP Raw；3／6 查 MO Material State；5 查 WIP Semi；7 查 MO 混合來源。來源與退入 Locator 都經庫房 Locator lookup；沒有 Locator 時舊 Form 以 `N/A` 表示。
- 無元件批號的數量需為數值、大於 0，且不得超過來源批號可用 Qty。Detail Edit 以 `DataStamp` 做併發更新。
- 類型 2／3／5／7 若載入到元件資料，必須至少選一個未被預約的元件，且 `CInt(Qty)` 必須等於已選元件數；Select All／UnSelect All／MultiSelect 會同步更新 Qty。類型 3／7 另拒絕已建立且已釋出工單批號的元件。類型 7 沒有元件資料時不能新增明細；類型 6 不走元件選取分支。
- Component Service/Core 分別使用 WIP Raw (`tblINVWIP_Raw_Component`)、WIP Semi (`tblINVWIPInventory_Component`) 與 MO (`tblOEMOMTLState_Component`) 來源，將退料元件寫至 `tblINVMTLWIPReturnComp` 並更新來源 `BookingFlag`。明細新增／修改與 Component／BookingFlag 更新由多個 Core 呼叫依序執行，可能部分成功；家用端正式 API 應在單一交易內重驗來源可用量、State、DataStamp、元件未預約／未釋出條件並同步明細及預約。
- 刪除單頭會清除單頭、明細、IV302 元件連結並依來源類型釋放對應元件 BookingFlag。單頭 Delete 為批次逐單呼叫；不得在 UI 端假設所有選取列一次原子成功。
- IV302 只維護草稿，不直接執行退料／扣減庫存。WIP 退料確認由 IV304「退料單確認(WIP)」處理；Service 的確認狀態流程使用 Source 10。

## DEV 實作

- `web/src/components/InvMaterialWipReturn.vue` 僅在 DEV preview 開啟本頁；正式環境維持 pending placeholder。
- `InvMaterialWipReturnDevPreview.vue` 提供 State=0 查詢、Basis／Detail 雙 Grid、單頭新增／說明修改／複製／多選刪除、明細新增／修改／複製／多選刪除，以及來源類型、批號、來源／目標 Locator、數量和元件假預約互動。
- 五種 `FromPlaceType` 都有固定 DEV 假資料。操作寫入瀏覽器 localStorage，不呼叫 API，也不修改正式庫存；複製單頭不帶明細，刪除會釋放本機元件假預約。
- 為避免 DEV 模擬舊程式數量截斷，元件數量規則使用整數精確相等；其餘 Qty 上限以假來源批號數量判斷。

## Oracle metadata 與家用端 handoff

- 本機受保護 Oracle profile 不存在；沒有使用連線密碼，也沒有查 Oracle。未在 repo 找到可作為同版可信來源的 IV302 DDL；不根據 Core SQL 字串猜 PostgreSQL CREATE。
- Core 引用的主要資料表：`TBLINVMATERIALWIPRETURNBASIS`、`TBLINVMATERIALWIPRETURNDETAIL`、`TBLINVMTLWIPRETURNCOMP`；元件預約來源還包括 `TBLINVWIP_RAW_COMPONENT`、`TBLINVWIPINVENTORY_COMPONENT`、`TBLOEMOMTLSTATE_COMPONENT`。上述表的欄位型別、PK、FK、index、default 與 trigger 仍待家用端比對 live PostgreSQL／可靠 DDL。
- 建議正式 API：登入與 IV302 ACL 保護的單頭分頁查詢（State 預設限制 0）、單頭新增／Description-only 修改／刪除、父單號限定明細查詢與明細新增／修改／刪除；來源物料／Locator／元件 lookup 需用 FromPlaceType、FromInventoryNo／MO、InventoryNo、MaterialNo、LotNo 完整範圍限制。
- Core 明細 Load 在 MaterialReturnNo 空值時不加父單號條件，可回傳全表；新 API 必須拒絕空／無效父單號，使用參數化 SQL、穩定排序及分頁。單頭、明細、元件連結及來源 BookingFlag 必須在交易中一致更新，避免照搬舊 Service 分段寫入造成局部成功。
- 家用端需另核 IV304 的 Source=10 確認流程、庫存／元件狀態更新和冪等策略；IV302 DEV handoff 不等於正式確認 API 已完成。


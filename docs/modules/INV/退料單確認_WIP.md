# IV304 退料單確認（WIP）

## 來源與範圍

- 功能鍵／選單：`IV304`／`WIP RETURN CONFIRM`／「退料單確認(WIP)」；路由 `/YMES/INV/WipReturnConfirm`。
- WinForms：`D:\iMES\CSiMES\STD\MESWin\INV\IV_WIPReturnConfirm\frmINVWIPReturnConfirm.vb`，Form 與 Designer inline。
- Service：`D:\iMES\CSiMES\STD\MESws_STD\wsSTD\wsINV\wsINV.cs` 的 `AddINVTransactionState`、`ExeINVTransactionState`、`LoadINVMaterialWIPReturnBasis/Detail/Comp`；另有獨立舊入口 `ExeMaterialWIPReturn`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcINV\clsInventory.cs` 的 `AddINVTransactionState`、`ExeMaterialWIPReturn_ForMaterialWIPMOReturn`、`SetINVMaterialWIPReturnBasisState`。交易列的新增、處理中狀態、過帳、完成／失敗狀態分多個呼叫處理。
- 公司端只用 DEV 假資料復刻查詢、單選確認與結果回饋；不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不實作正式 API/schema、不部署。

## 查詢與畫面

- Form 開啟時不自動查詢。`funInitial` 後將 CreateDate 起日預設為近 14 天、迄日設為今天；起迄核取方塊預設未勾。State 固定 `0=UnConfirm`、已勾選且停用，清單只顯示待確認單。
- 可查欄位：Return No（`MaterialReturnNo` contains）、目標 `InventoryNo`、來源 WIP `FromInventoryNo` 或來源工單 `FromInventoryNo`、`FromPlaceType`、`CreateDate` 起日與迄日。WIP Inventory 與 MO 來源核取方塊互斥；迄日是含當日 23:59:59 的日期條件。沒有 ReturnDate 篩選。
- Inventory picker 查 `InventoryType=0`；WIP source picker 使用 `InventoryType=6`；MO picker 讀工單主檔。目標庫房可手動輸入，picker 類型不代表每張已建立的退料單都只會退入原料庫。
- Basis Grid 顯示 `MaterialReturnNo`、`FromPlaceType`、`FromInventoryNo`、`InventoryNo`、`State`、`Description`、`Creator`、`CreateDate`、`ConfirmDate`。Form 將 Basis `ReturnDate` 改名為 `ConfirmDate`。Core Basis 查詢未設定排序、分頁或最大列數。
- 啟用 Basis 列時呼叫 `LoadINVMaterialWIPReturnComp`，Detail Grid 顯示 `MaterialNo`、`MaterialLotNo`、`LocatorNo`、`UnitNo`、`Qty`；展開子列顯示 `ComponentNo`、`ComponentGoodQty`、`ComponentScrapQty` 等元件欄。畫面不顯示 `ToLocatorNo`。
- `WIP Return` 按鈕只處理目前 ActiveRow；GroupRow 不處理。按下先顯示 Ok/Cancel 確認視窗，只有目前單據 `State=0` 才送出。送出後重查固定 State=0，因此成功或失敗的單據都會從此查詢清單消失。

## Source=10 確認流程

1. Form 呼叫 `wsINV.AddINVTransactionState`，送入 `Source=10`、`TicketNo=MaterialReturnNo`、`FromInventoryNo`、`Creator`。
2. Service 建立 `TBLINVTRANSACTIONSTATE` 交易列，將 WIP Return Basis 設為 `State=1`（Handling），再同步呼叫 `ExeINVTransactionState`。
3. `ExeINVTransactionState` 驗證 TransactionID 存在，Source 10 呼叫 `ExeMaterialWIPReturn_ForMaterialWIPMOReturn`，成功後將 Basis 設為 `State=2` 並填 Basis `ReturnDate`，再把交易列移到 `TBLINVTRANSACTIONLOG`。失敗時 Basis 設為 `State=3` 並寫錯誤記錄。
4. `AddINVTransactionState` 呼叫內部 `ExeINVTransactionState` 時使用空的 catch；即使 Exe 回傳失敗 XML，Add Service 仍可能回覆成功。WinForms 之後只重查 State=0，使用者可能看不到明確的失敗原因。正式 API 應將過帳結果明確回傳並保留可查錯誤。
5. `wsINV.ExeMaterialWIPReturn` 是另一個舊 WebMethod，直接呼叫舊 `clsInventory.ExeMaterialWIPReturn`；本 Form 的按鈕不使用它。正式 API 應以目前 UI 的交易入口及 `ExeMaterialWIPReturn_ForMaterialWIPMOReturn` 行為核對，不要直接把舊方法當成唯一確認規格。

## Core 過帳行為

| FromPlaceType | 來源量扣減 | 退入庫存 |
|---:|---|---|
| 2 WIP Inventory | `TBLINVWIPINVENTORY_RAW` | `TBLINVRAWINVENTORY` 與 `TBLINVRAWINVENTORYLOC` |
| 3 MO | `TBLOEMOMATERIALSTATE` | `TBLINVRAWINVENTORY` 與 `TBLINVRAWINVENTORYLOC` |
| 5 WIP Semi | `TBLINVWIPINVENTORY_SEMI` | `TBLINVSEMIINVENTORY` |
| 6 MO Semi | `TBLOEMOMATERIALSTATE` | `TBLINVSEMIINVENTORY` |
| 7 MO Material + Semi | 從工單物料狀態扣量；依 IV302 Component link 的退入庫房／類別分流 | 依各 Component 的 `ReturnInventoryNo` 寫入 Raw 或 Semi 庫存及元件庫存；確認前另檢查工單是否已有釋出批號 |

- Core 依 MaterialNo＋MaterialLotNo 彙總明細數量，檢查來源量後扣減；Raw／Semi 目標庫存按 Detail 的 LocatorNo 加量，若庫存列不存在則建立庫存／儲位列。允許同批分多個 Locator。
- Core 查 Detail 時只取 `MaterialNo, MaterialLotNo, LocatorNo, Qty`，完全沒有讀 `ToLocatorNo`；因此目前實際加量使用來源 `LocatorNo`，IV302 建立時選擇的目標 Locator 未用於過帳。正式 API 應釐清是否保留此舊行為或改用 ToLocatorNo，並明確記錄決策。
- Component 路徑依來源類型搬移元件：WIP Raw 讀 `TBLINVWIP_RAW_COMPONENT`；MO 讀 `TBLOEMOMTLSTATE_COMPONENT`／`TBLOEMOCOMPONENT`；WIP Semi 讀 `TBLINVWIPINVENTORY_COMPONENT`。Raw／Semi 元件寫回 `TBLINVRAWINVENTORYCOMPONENT`／`TBLINVSEMIINVENTORYCOMPONENT`，並刪除／解除來源元件紀錄。`ExcludeInventoryCalCompRule` 系統參數會影響 `TBLINVTEMP_COMPONENT` 重算佇列或 `TBLSYSEVENTLOG` 記錄。
- 元件來源與目標混合處理還依賴 `TBLINVMTLWIPRETURNCOMP`；Core 留存這些連結列，不在確認時刪除。類型 7 依 Component 設定的 ReturnInventoryNo／ReturnInventoryType 回庫。
- Core 將一批 SQL 收集後呼叫 `ExecuteSQLNoneQuery_ChkRowCount`；Basis State、交易列及交易 Log 另由 Service/Core 步驟處理。完整交易邊界需家用端在正式實作中設計及驗收，不能只提交一個 queue row 或僅更新 Basis State。

## 已核對的原始碼疑點與 API handoff

- `ExeMaterialWIPReturn_ForMaterialWIPMOReturn` 讀 Basis 的 State 欄位，但沒有檢查 State 值；Core 也不使用 DataStamp 做確認 CAS。正式確認需伺服器端重讀並鎖定 `State=0`、DataStamp、來源量、元件 BookingFlag／MO ReleaseState，防止重複確認與併發超退。
- Type 7 對每筆 Material/Lot 查詢分組後的 ReturnInventoryNo／ReturnInventoryType，卻用外層 Material/Lot 索引 `Rows[i]` 選目標，且依 Material/Lot 合計 Qty 過帳。多目標庫房／多明細的分配需要逐筆核對；家用端應使用明細與元件完整鍵，逐個目標彙總，避免錯配、重複加量或索引越界。
- 元件刪除有多處只按 LotNo／Material／ComponentNo，未一律包含來源 InventoryNo／LocatorNo／TicketNo。正式 API 必須依可信完整鍵限定元件，並在同一交易內移動元件、更新庫存及 BookingFlag。
- Core 讀寫欄位可用來確認業務流程，但不足以推導 CREATE schema；正式 API 須參數化 SQL、有登入／Function ACL、父單號限定、伺服器生成交易 ID、冪等鍵、庫存守恆檢查與可追蹤失敗結果。
- 建議正式 command API：`POST /api/inventory/wip-returns/{materialReturnNo}/confirm`。在同一資料庫交易中鎖定單據與來源量，重驗 State／DataStamp／來源庫存／MO ReleaseState／元件預約，完成來源扣減、Raw／Semi 目標量、Locator、Component、交易列／Log 與狀態更新；重複提交不得再次過帳。查詢 API 應有穩定排序、日期跨度限制及分頁。

## DEV 實作

- `web/src/components/InvWipReturnConfirm.vue` 僅在 DEV preview 開啟此功能；正式環境維持 pending placeholder。
- `InvWipReturnConfirmDevPreview.vue` 提供固定 State=0 查詢、日期／來源條件、單頭清單、選列明細／Component 子列、單筆確認對話框及 localStorage 模擬過帳列。成功確認會將假單據改為 State=2、產生來源／目標庫房、Locator、數量與元件的本機過帳清單，並從待確認清單移除；Reset DEV 可還原固定假資料。
- DEV 的確認不呼叫 API、不改正式庫存／工單資料；頁面只保存本瀏覽器 localStorage。

## Oracle metadata 與 schema handoff

- 桌機受保護 Oracle profile 不存在；本輪未用 PowerShell 5.1／ODP.NET 連 MESTEST，未查業務資料，也未產生猜測的 CREATE SQL。
- IV304 核心退料表 `TBLINVMATERIALWIPRETURNBASIS`、`TBLINVMATERIALWIPRETURNDETAIL`、`TBLINVMTLWIPRETURNCOMP`，以及 `TBLINVRAWINVENTORY`／`TBLINVRAWINVENTORYLOC`／`TBLINVRAWINVENTORYCOMPONENT` 和 `TBLINVTEMP_COMPONENT`，沒有在 repo 找到本輪可確認的同版 DDL／完整 metadata。家用端先核 YMES live schema／migration；缺可信結構時記錄欄位證據與缺項，不可由舊 SQL 推測型別、PK、FK、index 或 default。
- 可沿用 repo 已有參考而不重建：`TBLINVWIPINVENTORY_RAW/SEMI`（WP201）、`TBLINVWIPINVENTORY_COMPONENT`（WP603）、`TBLINVSEMIINVENTORY`（WP201）、`TBLINVSEMIINVENTORYCOMPONENT`（WP604）、`TBLOEMOMTLSTATE_COMPONENT`（OE Component reference）、`TBLINVTRANSACTIONSTATE/LOG`（WP605／migration `0073`）。其他 lookup 表依各自模組 reference 核對。
- 公司端不連 YMES PostgreSQL、不新增 migration/schema；正式 schema、API、權限、交易與部署都由家用電腦負責。

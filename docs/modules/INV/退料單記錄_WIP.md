# IV303 退料單記錄（WIP）

## 來源與範圍

- 功能鍵／選單：`IV303`／`WIP MATERIAL RETURN LOG`／「退料單記錄(WIP)」；路由 `/YMES/INV/MaterialWipReturnLog`。
- WinForms：`D:\iMES\CSiMES\STD\MESWin\INV\IV_MaterialWIPReturnLog\frmINVMaterialWIPReturnLog.vb`（Form 與 Designer inline）。
- Service：`D:\iMES\CSiMES\STD\MESws_STD\wsSTD\wsINV\wsINV.cs` 的 `LoadINVMaterialWIPReturnBasis`、`LoadINVMaterialWIPReturnDetail`、`LoadINVMaterialWIPReturnComp`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcINV\clsInventory.cs` 對應 Basis／Detail／Component 查詢。
- 本模組是唯讀記錄查詢；DEV 用固定假資料模擬條件、結果選取及明細／元件展開，不呼叫正式 API。

## 查詢畫面與條件

- Form 開啟時不自動查詢。CreateDate、MaterialReturnDate 預設起日為近 14 天、迄日為今天，但各條件預設未勾選，所以初始 Query 不帶日期限制。
- 查詢欄位：MaterialReturnNo（Core 使用包含式 `LIKE`）、InventoryNo、FromPlaceType、State、From Return InventoryNo、From Return MO No、CreateDate 起迄、MaterialReturnDate 起迄。來源庫房與 MO 欄位共用 Core 的 `FromInventoryNo`，Form 透過兩個互斥核取條件切換。
- MaterialReturnNo、InventoryNo、FromInventoryNo 的 `funQuery` 只看輸入是否非空，不看各自核取方塊；即使取消核取，保留的值仍會被送出。State／FromPlaceType／日期則依核取方塊送出。
- Inventory lookup 固定篩 `InventoryType=0`；WIP Inventory lookup 篩 `InventoryType=6`。MO No 由文字欄輸入。查詢沒有日期跨度上限，也沒有 Form/Core 的分頁或列數限制。
- State 顯示 0 UnConfirm、1 Handling、2 Confirm、3 Fail。查詢 Combo 與 Basis Grid 都使用 `ValueListProvider` 的 FromPlaceType／State 值；Form 另建立一份硬編碼 `AFromPlaceType` 清單（僅 2／3／5／6），但目前 Grid 綁定的是 provider 清單。IV302 的 BasisDef 明確支援 Type 7「MO Material + Semi」；DEV 顯示其來源語義，不依賴未確認的舊 ValueList 標籤。
- Core 以 `TBLINVMATERIALWIPRETURNBASIS` 查詢；MaterialReturnNo／InventoryNo／FromInventoryNo／State／FromPlaceType 與日期條件直接拼接 SQL，沒有 `ORDER BY`、分頁或上限。`EndCreateDate`、`EndReturnDate` 傳日期值時，Core 將迄日擴為當日 23:59:59。

## 查詢結果與明細

- Basis Grid 顯示 `MaterialReturnNo`、`FromPlaceType`、`FromInventoryNo`、`InventoryNo`、`State`、`Description`、`Creator`、`CreateDate`、`ConfirmDate`。Form 將資料欄 `ReturnDate` 改名為 `ConfirmDate`；這是單頭 ReturnDate，不是 TransactionLog CreateDate。
- 點選 Basis 後，Form 以該 `MaterialReturnNo` 呼叫 `LoadINVMaterialWIPReturnComp` 載入明細及元件子列；沒有編輯或確認操作。
- Detail Grid 只顯示 `MaterialNo`、`MaterialLotNo`、`LocatorNo`、`UnitNo`、`Qty`；`ToLocatorNo`、稽核欄位未顯示。元件 Relation 顯示 `MaterialLotNo`、`ComponentNo`、`ComponentGoodQty`、`ComponentScrapQty`。
- 舊 Core `LoadINVMaterialWIPReturnDetail` 在 `MaterialReturnNo` 為空時省略父單條件，可能回傳全表；新 Detail API 必須先驗證有效單號並要求父單限定。
- `LoadINVMaterialWIPReturnComp` 先讀該單 Detail，再以 `MaterialReturnNo` 取元件資料；迴圈中將 LocatorNo 變數覆寫成各 Detail 的 locator，迴圈結束後元件 SQL 可能只套用最後一筆 Detail 的 LocatorNo，漏掉其他 locator 的元件。它建立 Relation2 時只連 MaterialNo＋MaterialLotNo，沒有 MaterialReturnNo 或 LocatorNo；同單同物料／Lot 跨 locator 時，子列可能錯配或重複。正式 API 應依可信完整鍵（至少單號、物料、Lot、Locator）載入與關聯，不照搬舊 relation。

## DEV 實作

- `web/src/components/InvMaterialWipReturnLog.vue` 只在 DEV preview 開啟本頁；正式環境維持 pending placeholder。
- `InvMaterialWipReturnLogDevPreview.vue` 提供 9 個 Core 條件欄位、日期近 14 天預設、初次不查詢、查詢／清除、Basis 選取載入 Detail、元件子列展開及唯讀 State 標記。FromPlaceType 7 依 IV302 BasisDef 顯示為 MO Material + Semi。
- DEV 用 detail-specific Component fixture 顯示預期的精確父子列；這是正確關聯的 UI 示意，不複製舊 Core 的最後 Locator 過濾與錯誤 Relation2 行為。
- Clear 按鈕是 DEV 便利操作；舊 Form 沒有清除按鈕。所有資料皆為固定假資料，不呼叫 API、不寫資料庫。

## Oracle metadata 與家用端 handoff

- 桌機受保護 Oracle profile 不存在，未連 Oracle，未取得本版 `TBLINVMATERIALWIPRETURNBASIS`／`TBLINVMATERIALWIPRETURNDETAIL`／`TBLINVMTLWIPRETURNCOMP` DDL／metadata；不根據 `SELECT *` 或舊 Core 欄位猜 CREATE SQL。
- 家用端先比對 live PostgreSQL schema／migration，再提供登入及 `WIP MATERIAL RETURN LOG` ACL 保護的唯讀 API。Basis 查詢需參數化、穩定排序、日期跨度上限、分頁與最大列數；保留 FromPlaceType 7 的穩定代碼與標籤，並核對正式端的 ValueList 設定。
- Detail endpoint 必須拒絕空／無效 `MaterialReturnNo`，只回傳該父單明細；元件資料要修正舊 Core 的最後 Locator 過濾與不完整 Relation2 鍵。回傳 Form 可見欄位，保留 ReturnDate 原欄位語義並明確以 ConfirmDate 標籤呈現。


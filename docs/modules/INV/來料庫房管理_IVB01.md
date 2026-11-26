# IVB01 來料庫房管理

更新日期：2026-11-25。公司端交付範圍：Industry Base WinForms／Service／Core 行為核對、Vue 3 DEV 假資料互動及家用端 API／schema handoff；未連 YMES PostgreSQL，未實作正式 API。

## 功能與入口

- MES 選單 FunctionNo `B_RAW WAFER RECEIVING`、功能代碼 IVB01；主畫面 `frmINVWaferReceiving`，執行檔 IV_B_WAFERRECEIVING.DLL。
- Vue DEV 路由 `/YMES/INV/WaferReceiving`。主畫面查詢原料庫存、料批／儲位與晶圓 Component／動態電性摘要；Import 模擬來料檔解析及已確認入庫，Delete 模擬刪除庫存與自動建立的已確認退料單。
- 這是 Industry Base 來料庫房流程，不是已取消的 WAFERBANK vendor DLL 項目；WAFERBANK 未納入本次功能。

## 核對來源

| 層 | 原始來源 | 已核對責任 |
|---|---|---|
| WinForms 主畫面／inline Designer | `Industry_Base/MESWin_Industry_Base/INV/IV_B_WaferReceiving/frmINVWaferReceiving.vb` | 五個勾選式查詢條件、Basis／Locator／Component 三 Grid、Import／Delete 流程及選列關聯 |
| WinForms 匯入 Form／Designer | `Industry_Base/MESWin_Industry_Base/INV/IV_B_WaferReceiving/frmINVWaferReceivingImport.vb`、`frmINVWaferReceivingImport.Designer.vb` | 必填欄位、Vendor AutoLoader 格式、CSV／Excel 欄位解析、模板上傳下載及存檔呼叫 |
| Service | `Industry_Base/MESws_Industry_Base/wsINV/wsINV_Base.cs`：`LoadINVRAWInventoryData`、`DelINVRAWInventoryDetail`、`SaveTestSumData`；同專案 `wsMTL`／`wsSYS` | XML 條件解析、資料集傳遞、事件／錯誤記錄、測試 Recipe／Template／Vendor 格式服務 |
| Core | `Industry_Base/MEStc_Industry_Base/tcINV/clsInventory_Industry_Base.cs`：`LoadINVRAWInventoryData`、`DelINVRAWInventoryDetail`、`SaveTestSumData` | 原料／料批／片號／測試摘要查詢、Booking 守門、刪除與自動退料、匯入入庫與測試值寫入 |

## 舊畫面與行為

### 查詢與三層清單

- Form 啟動時載入 TestRecipeClass=1、IssueState=2 的來料測試 Recipe；不自動載入庫存。使用者按 Query 才查詢。
- 查詢欄位為 InventoryNo、MaterialNo、MaterialLotNo、ComponentNo、VendorNo，各欄都有獨立 CheckBox；只有勾選且文字非空的條件才送 Service。Inventory Lookup 使用 IssueState=2、InventoryType=0 庫房；Material Lookup 使用 IssueState=2 物料。
- Basis Grid 顯示 InventoryNo、MaterialNo、CurQty；選取料號後載入 Locator／Lot Grid，顯示 MaterialLotNo、VendorNo、LocatorNo、LocQty；選取 Locator／Lot 後顯示 Component／測試資料。
- Component Grid 顯示 ComponentNo、ComponentLotNo、ImportDate、S_LaserMark、B_CASSETTENO、SlotNo、S_FixID，並依啟用的測試 Recipe 動態顯示 `TestRecipeType_ConversionField`（若 ConversionField 無效則用 TestRecipeNo）欄位。全部 Grid 僅供檢視。
- Core 以 `TBLINVRAWInventory`、`TBLINVRAWInventoryLOC`、`TBLINVRAWInventoryComponent` 和 `TestRecipeClass=1` 的 SummaryTable 動態 Join。Component 與 Locator 的 Join 未包含 LocatorNo；同一料批分散於不同儲位時，Component 可能重複顯示在各 Locator。
- 測試欄位查詢根據核准 Recipe／轉換欄位與實際 SummaryTable 欄位動態組 SQL。查詢同時依 MaterialNo／MaterialLotNo／ComponentNo Join 測試摘要；Component 查詢也會逐列讀 Material In Basis／Component，以 ConfirmDate（有值時）或 CreateDate 回填 ImportDate。
- 所有查詢條件均可不提供，Core 沒有排序、分頁或列數上限。一般 Raw 來料查詢預設排除 MTL Property 標示為 PackageSubstrate／ProbedWafer 的料號；該 exclusion 會檢查 MaterialNo 與 MaterialType 的核准 Property。

### Import：供應商測試檔轉入庫存

- Import 開啟 `frmINVWaferReceivingImport`。必填 InventoryNo、LocatorNo、VendorNo、PSNo、TestRecipeType、FileName；Importer 預設目前登入者，ImportDate 預設現在。庫房／Locator／Vendor／PS／Recipe 選擇依據不同 Service／Master。
- 確認時讀取匯入檔所在目錄的 Vendor AutoLoader Script，產生／讀取 `FormatFile.txt`；Script 指定表頭固定欄位、明細欄位、資料起始位置及動態測試欄位。按副檔名走 CSV 或 Excel parser，再呼叫 `Industry_Base.wsINV.SaveTestSumData`。
- Core 會檢查核准物料、供應商物料對應、料批與 Slot、測試 Recipe DataType，並以 ComponentNo／MaterialNo／MaterialLotNo 檢查重複元件。指定 Locator 有容量時，舊檢查只比較「本次匯入片數」與整數 Capacity，沒有把目的儲位既有 LocQty 加總。
- 匯入會取新 MaterialInNo，寫 TestSummary 最新表與 Log、`TBLINVMATERIALINCOMPONENT`、`TBLINVRAWINVENTORYCOMPONENT`，並建立 `TBLINVMATERIALINBASIS` State=2、ConfirmDate=現在及按 MaterialNo／MaterialLotNo 彙總的 Detail。同步增加 Raw CurQty／Locator LocQty；非排除庫房會替換對應 `TBLINVTemp_Component`，供 AutoRun 後續挑片計算。這不是 IV202 的草稿建立流程。
- Template Upload／Download 使用系統 Template Service。主 Form 匯入成功後重查庫存並選取新料號。

### Delete：刪除儲位並自動建立退料單

- 使用者在 Locator／Lot Grid 多選並確認刪除；每列呼叫 Industry Base `DelINVRAWInventoryDetail`。Service 逐列處理，遇到失敗會停止迴圈；先前成功列不會被回復。
- Core 先以 InventoryNo／MaterialNo／MaterialLotNo 範圍檢查是否有任一 `TBLINVRAWINVENTORYCOMPONENT.BookingFlag=1`；有預約元件便拒絕刪除。此檢查不含 LocatorNo，因此同料批任一儲位有預約會擋住目前選列。
- 未被擋下時，Core 刪除指定 Locator row、按該列 LocQty 扣 Raw CurQty（為零則刪除 Raw Basis），刪除該料批 Component 及各 TestSummary 最新表對應資料，並清除 `TBLINVTemp_Component`。Component／TestSummary 的刪除範圍只用 InventoryNo／MaterialNo／MaterialLotNo，沒有 LocatorNo，可能影響同料批其他儲位的資料。
- 同一次刪除會建立 State=2、ReturnDate=現在的 MaterialReturn Basis，寫入選定 Locator 的退料 Detail，將查得的料批 Component 複製到 MaterialReturnComponent，並按傳入 S_FixID 更新預挑片 Log 狀態。需家用端核對刪除 Component 查詢未帶 LocatorNo 的影響範圍及 FixID 更新條件；不可只當一般 CRUD Delete。

## Vue DEV 復刻

- 接 App 原物料倉 FunctionKey `function:INV:B_RAW WAFER RECEIVING`、路由 `/YMES/INV/WaferReceiving`。
- Query Panel 保留五個各自勾選的精確查詢欄位與 Lookup；按 Query 後顯示 Basis → Locator／Lot → Wafer／Test Summary 三層唯讀清單。Component 展示 ImportDate、Component／Lot／Slot／Cassette／LaserMark／FixID、BookingFlag 與假動態測試值。
- Import Dialog 提供庫房、儲位、Vendor、PS、TestRecipeType、匯入者／日期、檔名、假預覽及 Template upload/download 操作。按確認驗證必填、模擬 Locator 容量／重複 Component 守門，再於本頁假資料增加已確認進料與 Raw／Locator／Component 數量；未讀取或解析使用者選擇的真實檔案。
- Delete Dialog 模擬 Core BookingFlag 守門、逐列刪除及 State=2 已確認退料單結果。DEV 以完整 `(InventoryNo, MaterialNo, MaterialLotNo, LocatorNo)` 精確刪選取的 Component／測試資料，避免舊 Core 刪掉同料批其他 Locator 子列；頁面明確提示這是安全簡化，不是舊刪除 SQL 的逐字複製。
- 所有新增、刪除、數量變動與自動退料單只存在頁面記憶體；無真實檔案上傳、localStorage、API 或 PostgreSQL 寫入。未執行 build／test／browser。

## 表、DDL 與 handoff

| 資料 | 用途 | DDL／metadata 狀態 |
|---|---|---|
| `TBLINVRAWINVENTORY`／`TBLINVRAWINVENTORYLOC` | 原料庫存與料批／儲位數量 | 沿用 IV201／WP201 references；本輪未新增 DDL |
| `TBLINVRAWINVENTORYCOMPONENT` | 晶圓 Component／Slot／BookingFlag | 可核實的 Industry Base 同版 DDL／metadata 未找到 |
| `TBLINVMaterialInBasis`／Detail／Component | 匯入時產生的已確認進料 | 可核實的同版 DDL／metadata 未找到；沿用 IV202 handoff 要家用端核 live schema |
| `TBLINVMaterialReturnBasis`／Detail／Component | 刪除時建立的已確認退料 | 可核實的同版 DDL／metadata 未找到；沿用 IV206 handoff，仍需核對實際欄位與交易紀錄 |
| `TBLINVTemp_Component` | 非排除庫房的 AutoRun 暫存元件 | 同版 DDL／metadata 未取得 |
| `TBLWIPTESTSUM_<TestRecipeType>`／`...LOG_...` | 動態測試值與 Log | 表名由核准 TestRecipeType 決定；欄位／DDL 未取得，API 不可直接接受任意表名 |
| `tbl_BReciveMTLBasis` 及 Industry Base 專用表 | 晶圓來料／Map 資料的其他 Industry Base flows | 本 Form 的 `btnOK` 未呼叫 sibling Substrate／Inkless Save flow；本輪未推定其 DDL |
| `TBLINVLOCATORBASIS` | 匯入 Locator Capacity | 沿用既有 Locator reference；Core 僅檢查本次匯入數 |

桌機受保護 Oracle profile 不存在，本輪未連 MESTEST。Industry Base Component、MaterialIn／Return、Temp_Component 與動態 TestSummary 表的 DDL／metadata 未能核實；沒有猜欄位或建立 CREATE SQL。

## 家用端 API／schema handoff

- 先核 live PostgreSQL tables／migrations、PK／FK、單位精度、預約欄位、Locator、TransactionLog 及動態 TestSummary 是否仍使用資料表；本規格不提供推測 CREATE SQL。
- 查詢 API 應要求至少一個精確條件、使用參數化條件與 bounded paging；動態 TestRecipeType／欄位必須對照已核准 Recipe allowlist，不得讓用戶輸入內容作為 SQL 表名或欄名。由資料層按完整庫房／料號／料批／Locator 鍵連 Component，避免舊 Join 漏 LocatorNo。
- 匯入改為受限的 upload → validate／preview → commit 流程；只接受 server registry 中已核准 Vendor AutoLoader mapping／PS／Recipe，驗證檔案、欄位型別、Vendor-Material map、Component 去重、Slot、BookingFlag、庫房／單位及真實 Locator 剩餘容量。容量需在同一交易鎖定／重讀並計入既有 LocQty，避免並行匯入超量。
- Commit 需以單一交易建立 TestSummary／Log、MaterialIn Basis／Detail／Component（State=2）、Raw Basis／Locator／Component、Temp_Component 與交易事件；使用 operation ID 防止同檔重送，回報逐筆錯誤與建立的 MaterialInNo。若 TestSummary 由 Recipe 動態建表，使用遷移管理的欄位映射，不可接受任意識別字。
- Delete 是高影響命令，不做通用 DELETE endpoint：必填完整 Locator 主鍵、重讀 State／Qty／BookingFlag，確認來源無預約，以原子交易處理庫存扣減、同 Locator Component／TestSummary 清理、Temp_Component、FixID 狀態與正式 MaterialReturn State／Detail／Component／TransactionLog；對多列命令採全有或全無或回傳逐列結果且明確保留已完成項，不能靜默呈現原子成功。
- 舊 Core SQL 使用字串串接、查詢無分頁、Capacity 只比較本次匯入數、Delete Component／TestSummary 刪除未限制 Locator 並逐筆呼叫可能部分成功。家用端應保留來源差異在 API 測試情境，採參數化查詢、完整複合鍵、版本／冪等保護與原子交易。

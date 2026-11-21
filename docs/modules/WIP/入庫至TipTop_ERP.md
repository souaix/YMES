# 入庫至 TipTop ERP（WP609）

## 來源與範圍

- 選單清冊 FunctionNo：`FGDSTOCKIN_TP`；清冊標記未掛正式 `MenuItem`。公司端 fallback DEV 入口不代表正式選單／ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_TP/frmInventory_FGDStockIN_TP.vb` 與 `.designer.vb`。
- ERP：Form 直接呼叫 TipTop `wsERP_TP`／`wsERP_TP_RPC.TIPTOPServiceGateWay.CreateStockInData`；ERP 版本為 GP 3.X 時使用 RPC reference。此頁沒有經 `wsERP_TP` 本地 ERP Web Service 中轉。
- MES Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `LoadWIPInventory_SEMIbyMO_TP`、`ChkmesStockIn_Lot`、`updateINVBookingFlag`、`MOStockIn_TP`，另有 `loadWIP_SEMI_MO_TP` 工單選擇器。
- Core：`STD/MESEAI/EAItc/clsWIP.cs` 的 Lot 查詢、reservation／重查及 `STD/MESEAI/EAItc/clsINV.cs` 的 `MOStockIn_TP`、`MOStockIn_TP_Component`、`funGetSysQty`；並呼叫 `clsLot.CloseLot`、`ChgProductOperation` 與 release log。

## Form／Designer 操作流程

1. Load 呼叫 `getMFGDocument_TP(ReferenceType="A")` 取 TipTop 入庫 Sheet Type，填入 ComboBox 並預設第一筆。Form 另載入 InventoryBasis；目的庫選 `InventoryType=1`，來源庫選 `InventoryType=6`，Locator 可留空。
2. MO 選擇器使用 `loadWIP_SEMI_MO_TP` 並支援多選。Ref. Lot No 按 Enter 會用 `LoadLotBasisJoinState` 查 Lot（Status filter `0|1|2|3|4|5`），帶出 MO 後再載入該 MO 的待入庫線邊 Lot。
3. 兩個 Grid 顯示可用 WIP Lot 與已選 Lot；可 Select／UnSelect、Select All／Clear、上下移動，亦可掃描 MES LotNo 或 OrgLotNo。可用 Grid 的來源資料為 Qty>0 且 BookingFlag=0。`InInvNum` 可編輯，Confirm 要求每列 `0 < InInvNum <= Qty`，支援部分入庫。
4. 部分入庫時，Form 會查 WIP Component、Scrap Component 與 `TBLWIPGOODBINSTATE`；任一類存在時拒絕部分入庫，因畫面沒有選取 Component／Bin 子明細的能力。整批入庫則繼續處理。Sheet Type、MO、目的庫與至少一筆 Lot 為必要條件。
5. 原 `btnGetStockInNo` handler 將 `getReferenceNo()` 的回傳狀態字串寫入單號文字欄；該函式實際用途是填 Sheet Type 清單，並非建立 TipTop 單號。Confirm 成功後則以 ERP response `sfu01` 作 MES FGDInNo。

## TipTop ERP 請求與補償

- 每次請求 `status=N`，不要求 ERP 自動確認／過帳；主檔 `sfu_file` 使用 `sfu01` 單別及 `sfu02` 日期。明細 `sfv_file` 包含序號、產品、目的 Warehouse、Locator、Lot、Qty 及 MO；沒有 ERPNo 時產品欄回退使用 ProductNo。
- `OrgLotNoToERP=1` 時送 OrgLotNo，否則送 MES LotNo。包裝批（`PackingLevel` 非空）ERP Qty 使用 `SysQty`；其他 Lot 使用手動輸入 `InInvNum`。`TPERPProcessCost != 1` 時才送 EMP／EQP 實際與標準工時欄位。MES 後續仍使用 MES LotNo 及 `InInvNum`，故包裝批 ERP Qty 與 MES detail Qty 來源可能不同。
- ERP 回應經 `chkExecutionStatus_TP` 解析；只有 `code="0"` 轉為成功，其他 code 均中止 MES。ERP 成功後將回應 `sfu01` 傳給 `MOStockIn_TP`。ERP 失敗時不執行 MES。
- MES 失敗時 Form 嘗試 `UndoCreateStockInData`（`status=Y`、`sfu01=FGDInNo`、`sfumodu=tiptop`）。Confirm `finally` 對失敗操作呼叫 `updateINVBookingFlag`。若 ERP undo 失敗，程式仍可能把整批 Lot 的 BookingFlag 重設為 0；ERP 單可能留存，而該批又可被重送。部分入庫在 reservation 階段本來就不設 BookingFlag，並發保護更弱。

## MES 查詢、狀態與 Core 更新

- `loadWIPInventory_SEMIbyMO_TP` 查 `TBLINVWIPINVENTORY_SEMI`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLPRDPRODUCTBASIS` 與報工資源／FailQty，並附 ERP Unit property。SQL 查詢本身選出 BookingFlag；Form 再用 `Qty>0 and BookingFlag=0` 過濾可選列。
- `ChkmesStockIn_Lot` 在進 ERP 前重查每列 MO、來源 Inventory、Lot 與 `BookingFlag=0`。只有 `AllIn=true` 的列會設定 BookingFlag=1 並檢查更新筆數；部分入庫不鎖列。`updateINVBookingFlag` 對完整預約列嘗試將 1 還原 0。
- `MOStockIn_TP` 寫入 `TBLINVFGDINBASIS`（State=2、Source=1、ERPNo、FromInventoryNo）與 `TBLINVFGDINDETAIL`；扣減 `TBLINVWIPINVENTORY_SEMI`、更新 Lot `CurQty`／`SysQty`、新增或累加 `TBLINVFGDINVENTORY`，依剩餘 WIP 狀態處理結批、產品／作業變更與 Lot release log action 13。
- `MOStockIn_TP_Component` 將來源 `TBLINVWIPINVENTORY_COMPONENT` 的良品／不良 Component 明細搬到 `TBLINVFGDINCOMPONENT` 及 `TBLINVFGDINVENTORYCOMPONENT`，刪除已入庫的 WIP Component 與 Component State。
- **Good Bin source gap：**此 Form 明確禁止含 Good Bin 的部分入庫；但本次追到的 active `clsINV.MOStockIn_TP`／`MOStockIn_TP_Component` 方法沒有 `TBLWIPGOODBINSTATE` 或 `TBLINVFGDINBIN` 更新語句。完整入庫遇到 Good Bin 的預期落點／後續取消回復仍須家用端核對舊系統其他 helper 或實際案例，不能按 E10ICD 路徑推測。
- 多表 SQL 加入 `CollectionSQL` 後以 `ExecuteSQLNoneQuery_UPD` 執行；程式內的交易範例已註解。家用端需將本地庫存變更納入單一交易，並持久化 ERP Create／Undo 的請求、response、unknown outcome 與可恢復狀態。

## Oracle reference 與執行界線

- 直接表沿用既有 Oracle／CREATE-only references：WP605 的 `TBLINVFGDINBASIS`、`TBLINVFGDINDETAIL`、`TBLINVFGDINCOMPONENT`、`TBLINVFGDINVENTORYCOMPONENT`；WP201 的 `TBLINVFGDINVENTORY`、`TBLINVWIPINVENTORY_SEMI`、Lot 與 WIP Component references。沒有新增直接表或 DDL 缺項，不重複產 CREATE-only。
- 這次沒有新的 Oracle metadata 查詢，也沒有連 YMES PostgreSQL。公司端只交付 Vue DEV、規格與家用端 API／schema handoff；正式 API、live schema、migration 與驗收由家用端負責。

## Vue DEV 頁

- Fallback 路由：`/YMES/WIP/FgdStockInTp`；合成 Sheet Type、MES／ERP 單號、MO、Ref Lot、FGD 目的庫、可空 Locator、WIP 來源庫及雙 Grid。
- DEV 支援 MO 多選條件、Lot/OrgLot 參考批、掃描 Select／UnSelect、Select All／Clear、可入庫 Qty 與 InInvNum、部分入庫守門、ERP `OrgLotNoToERP`／`TPERPProcessCost` 參數呈現，以及 ERP／MES／ERP Undo 成敗情境。Good Bin 顯示為來源缺口，不模擬未知的 Core Bin 轉移。
- ERP code 非 0 時不執行 MES；MES 失敗則展示 ERP undo 成功／失敗及舊程式最後釋放 BookingFlag 的風險。頁面只使用 DEV 假資料，不呼叫 TipTop SOAP、MES EAI Service 或資料庫。

## 家用端 API／schema handoff

- 實作單一可冪等的 FGD stock-in command：伺服器重查 Sheet Type、使用者權限、MO、Lot／來源庫、Unit、當前 Qty／SysQty、BookingFlag、Component／Bin、目的庫及 Locator；每列用版本／交易鎖保護，不信任瀏覽器輸入。
- 外部 TipTop CreateStockInData 前持久化 intent；只有 code=0 才進 MES 本地交易。timeout／unknown outcome 先查 ERP 或排對帳，不盲目重送。MES 失敗的 ERP Undo 結果要保存並提供恢復作業，Undo 失敗不得僅釋放預約後重新開放 Lot。
- 查明 Good Bin 在 TipTop 整批入庫的有效來源規則；若無已證明的其他 helper，定義 Bin 轉移與取消回復，核對 `TBLINVFGDINBIN` schema，勿以 E10ICD 行為替代。
- 核對包裝批 ERP Qty=`SysQty` 與 MES Qty=`InInvNum` 的單位／換算契約；將 ERP 建單與 MES Lot／FGD／WIP／Component／LotState／release log 寫入本地原子交易並處理 release-log helper 的間接表依賴。公司端未連 PostgreSQL、未實作／驗收正式 API 或 schema。

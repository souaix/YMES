# 取消入庫 WorkFlow ERP（WP619）

## 來源與範圍

- FunctionNo：`FGDSTOCKIN_WF_CANCEL`，選單清冊標示未掛正式 `MenuItem`；DEV fallback 不代表正式選單或 ACL 已啟用。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_WF_Cancel/frmInventory_FGDStockIN_WF_Cancel.vb` 與 `.Designer.vb`。
- Inventory Service：`STD/MESws_STD/wsSTD/wsINV/wsINV.cs` 的 `LoadFGDInNoUnionSCRInNo`、`LoadFGDInDetailUnionAllSCRInDetail`、`UpdINVInfo`；由 `STD/MESkc/kcINV/clsInventory.cs` 提供 SQL 查詢與狀態更新。
- MES Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `MOStockIn_WF_Cancel`；EAI Core：`STD/MESEAI/EAItc/clsINV.cs` 的 `MOStockIn_WF_Cancel`。
- ERP：Form 直接呼叫 `wsERP_WF.WFMESService.XMLAdapter`，另以 transaction log 記錄 `UndoCreateStockInData_WF`。公司 DEV 不會呼叫 Service、ERP 或資料庫。

## Form／Designer／查詢

- Designer 有唯讀 MES `INVInNo`、唯讀 `ERPNo`、`...` 查詢鈕、FGD／SCRIN 共用明細 Grid、「CancelINV」與 Close。
- `LoadFGDInNoUnionSCRInNo` 傳 `State=2`、`IsERP=True`。Core 將 FGD 與 SCRIN basis `UNION`，要求 ERPNo 非空，但沒有 ERP source 欄位或 WorkFlow 單別條件；TipTop、T100、E10ICD 及 WorkFlow 文件都可能出現在同一清單。
- 選單載入後，`LoadFGDInDetailUnionAllSCRInDetail(INVInNo)` 將 FGD／SCRIN detail `UNION ALL`，帶回 INVType、Inventory、MO、Lot、產品／版次、Good／Scrap／Loss Qty、Unit。Form 顯示混合良品與不良品明細，並依實際明細組合傳 `FGDINV|SCRAPINV`。
- 按取消前再重查 State=2 且 ERPNo 非空；檢查 ERPNo 含 `-`，拆成單別／ERP 單號。此檢查仍不確認其 ERP 來源。

## WorkFlow Undo 與 MES 回復

1. Form 先以 `UpdINVInfo(INVInNo, INVType, 2, 1)` 將主檔設為處理中，再送 WorkFlow `XMLAdapter`：`MOCTF.TF001`／`TF002` 取自 ERPNo、`ReferenceType=1`、`ReferenceCase=4`（還原）；`WFERPUseMESUserNo` 決定 XML 身分。`UpdINVInfo` 實際 SQL 不以 `OriginState` 限制 `WHERE`，只覆寫 `OrgState` 與 `State`。
2. ERP 明確失敗時，Form 呼叫 `UpdINVInfo(..., 1, 2)` 還原為已確認；ERP 逾時／例外也可能維持預設 `result=fail`，同樣把 MES 還原為 State=2，未辨識 ERP 是否已接受 Undo，是未知結果風險。
3. ERP Undo 成功後，Form 先設 State=-2，再呼叫 MES `MOStockIn_WF_Cancel(INVInNo)`。EAI Core 對 FGD／SCRIN 明細還原良品及不良品庫存、Lot CurQty／SysQty、Component／FailBin、BookingFlag、結批狀態與 release log；Core 將 basis State 設為 3，Form 隨後再以 `UpdINVInfo(..., -2, -1)` 覆寫成最終取消 State=-1，並寫 CancelUser／CancelDate。
4. 若 MES 回復失敗，Form 留下 State=-2；ERP 已撤銷，MES 庫存可能未完整回復。Core 多表 SQL 的 transaction／rollback 範例已註解，不能視為原子操作。

## 缺口與 Oracle metadata

- 查詢與取消沒有區分 ERP 來源。把其他 ERP 的 ERPNo 送給 WorkFlow `ReferenceCase=4` 會取消錯誤系統中的單據；DEV 會顯示混合來源候選並阻擋非 WorkFlow 文件。正式 API 必須驗證可信 ERP source 並路由或拒絕。
- 單據狀態更新沒有 compare-and-set 條件；先前查詢／重查不能阻止兩個操作者同時取消。需以授權、State 條件更新、冪等 key 與 ERP 對帳記錄重新設計。
- 回復直接使用 FGD／SCRIN basis、detail、inventory、Component、WIP Semi／Scrap／Component Scrap／FailBin、Lot State、release log 等既有表。重用 WP201／WP603／WP605 references；不產生新的 CREATE-only。
- WP619 會刪改 `TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN`；這三張 Oracle DDL／metadata 在 WP622 已列缺項。桌機沒有可用的受保護 Oracle profile，本次未連 Oracle、未猜欄位或 DDL。公司端未連 YMES PostgreSQL，未實作 API/schema。

## Vue DEV 頁

- Fallback：`/YMES/WIP/FgdStockInWorkFlowCancel`。以合成清單呈現 State=2／ERPNo 非空單據、混合 FGD／SCRIN 明細、ERP source、產品／Lot 與數量。
- 可模擬 WorkFlow Undo success、explicit fail、timeout／exception，以及 MES 回復 success／fail。成功路徑依 State 1 → -2 → Core 3 → 最終 -1 呈現；MES fail 留在 -2；ERP fail 還原 State=2；timeout 呈現舊 Form 將未知結果誤當 fail 的風險。
- 非 WorkFlow ERP source 被阻擋；production 無假資料互動，不呼叫 ERP、MES Service 或資料庫。

## 家用端 API handoff

- 伺服器重新確認使用者 ACL、ERP source、ERPNo、FGD／SCRIN 同號明細、期望 State=2、單據版次及庫存前置條件；以 compare-and-set／冪等 command 保留取消 intent。
- WorkFlow Undo timeout 先查 ERP／排 reconciliation，不可回報明確失敗或釋放 State=2。Undo 成功後在單一本地交易還原 FGD／SCRIN／WIP／Component／FailBin／Lot／BookingFlag／release log，MES 失敗須保留可恢復狀態與操作歷程。
- 比對 PostgreSQL live schema／migration；三張 SCRIN 庫存表的 Oracle source metadata 待公司端取得受保護 Oracle profile 後唯讀補查。家用端負責 schema／migration、正式 API、部署與驗收。

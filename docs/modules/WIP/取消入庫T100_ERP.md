# 取消入庫 T100 ERP（WP620）

## 來源與範圍

- 選單 FunctionNo：`FGDSTOCKIN_T100_CANCEL`；清冊標記未掛正式 `MenuItem`。公司端 fallback DEV 入口不表示正式選單／ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_T100_Cancel/frmInventory_FGDStockIN_T100_Cancel.vb` 與 Designer。
- MES Service：`STD/MESws_STD/wsSTD/wsINV/wsINV.cs` 的 `LoadFGDInNoUnionSCRInNo`、`LoadFGDInDetailUnionAllSCRInDetail`、`UpdINVInfo`、`ExeINVFGDINCancel`。
- T100 Service：`Customer/Production/MESws_STD/wsSTD/wsERP/wsERP_T100.cs` 的 `ERP_Stock_IN_Cancel`。
- Core：`STD/MESkc/kcINV/clsInventory.cs` 的對應 union 查詢、狀態更新、`ExeFGDInCancel` 與 `ExeSCRInCancel`。

## WinForms 與 ERP 流程

1. 查詢使用 `LoadFGDInNoUnionSCRInNo(State=2)`，沒有傳 `IsERP=true`；因此清單包含已確認的 FGD／SCRIN 入庫單，也可能包含其他 ERP 來源的資料。Confirm 會重新查詢 State=2，並另外要求 `ERPNo` 非空，但仍沒有確認 ERP source 的檢查。這是 T100 取消流程的來源守門缺口。
2. 選取單據後載入 FGD／SCRIN union detail，顯示 MES 單號、ERP 單號、種類、庫別、Lot、MO、Product／Version、GoodQty／ScrapQty／LossQty 與 Unit。取消以整張單為單位。
3. Form 依每列 `INVType` 對應 RequestType：`FGDINV` → `0`，呼叫 T100 `stockin.data.process`，參數使用 `stock_in_no`；`SCRAPINV` → `1`，呼叫 `wo.scrap.data.process`，參數使用 `doc_no`。T100 Service 只把 response `code=0` 當成功，並記錄 request／response。
4. 確認前先 `UpdINVInfo(2,1)` 鎖定單據。ERP 取消成功後更新 State 到 `-2`，再呼叫 MES `ExeINVFGDINCancel`；Core 將 FGD／SCRIN 分別派到 `ExeFGDInCancel`／`ExeSCRInCancel`。MES 成功後更新 State 到 `-1` 並寫 CancelUser／CancelDate。
5. ERP 取消失敗時 `UpdINVInfo(1,2)` 將狀態還原到可重試的 2，MES 庫存不動。ERP 已成功但 MES 失敗時保留 State=-2；單據不會回到 State=2 查詢清單，需由正式端提供可追蹤的恢復／對帳流程。原 Form 在處理後清空單號及 Grid。

## Core 庫存回復與資料缺項

- FGD 回復會處理來源 WIP／線邊 Inventory、BookingFlag、FGD Inventory、Component／動態 Bin、Lot `CurQty`／`SysQty` 與 Lot 歷程；SCRIN 回復會依 Source 還原 Lot Scrap、Component Scrap 或 Fail Bin，處理相關庫存、預約及歷程。共用取消 Core 行為與 WP623。
- `TBLINVFGDINBIN` Oracle DDL／metadata 尚缺，沿 WP621／WP623 記錄；公司桌機沒有可用的受保護 Oracle profile，所以本項沒有連 Oracle、沒有猜表欄位，也未新增 CREATE-only SQL。其餘資料表沿用 WP201／WP603／WP605／WP215 的既有 references。
- `UpdINVInfo` 每次都以傳入的前一狀態覆寫 `OrgState`；例如成功路徑最後留下 `OrgState=-2`，ERP 失敗路徑留下 `OrgState=1`。正式端應保留穩定的原始狀態及取消操作歷程。
- ERP Web Service 與 MES 本地多表更新不是單一原子交易；Core 使用 `CollectionSQL`／`ExecuteSQLNoneQuery_UPD`。家用端需核對 PostgreSQL live schema 和本地交易範圍，實作冪等及 unknown-outcome 對帳，不能依賴桌機推測。

## Vue DEV 頁與差異

- Fallback 路由：`/YMES/WIP/FgdStockInT100Cancel`；只在 DEV 預覽使用合成資料，不讀資料庫、不呼叫 T100／MES Service。
- 模擬良品／不良品單據、RequestType 0／1、ERP 取消失敗後回復 State=2、ERP 成功但 MES 失敗後留在 State=-2，以及全成功取消。
- 為讓來源缺口可見，DEV 清單保留其他 ERP 樣本，但會阻擋非 T100 單據送入模擬取消；原始 Form 沒有此 ERP source guard。正式端必須依單據 ERP source 路由或拒絕，不可盲送 T100。
- 表格搜尋取代 WinForms 對話框是呈現簡化；正式 API 尚未接入。公司端未連 PostgreSQL、未實作／驗收正式 API 或 schema。

## 家用端 API／schema handoff

- 取消命令需在伺服器重查單據版本、State=2、ERPNo、`FGDINV`／`SCRAPINV` 種類、ERP source 與使用者權限，並以單據鎖與 idempotency key 防止重複取消；不得只照抄舊 Form 的無 source filter 查詢。
- 依已確認的 ERP source 才可呼叫對應取消服務。T100 source 對 FGD 使用 RequestType=0、對 SCRIN 使用 RequestType=1；只有 code=0 才回復 MES。timeout／unknown outcome 先查 ERP 結果或進對帳佇列，不可盲目重送。
- MES 回復與最後狀態更新須在單一本地交易完成，包含來源／目的庫、Component／動態 Bin、BookingFlag、Lot 數量及歷程。ERP 成功而本地失敗時保留可恢復狀態並提供重試／人工對帳；不得重新開放 State=2。
- `TBLINVFGDINBIN` 的 Oracle metadata 沿 WP621／WP623 待具 profile 的公司端唯讀補查；未取得前不產生 CREATE SQL／migration。家用端負責 live schema、migration、正式 API 與驗收。

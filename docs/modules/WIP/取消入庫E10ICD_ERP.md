# 取消入庫 E10ICD ERP（WP623）

## 來源與範圍

- 選單清冊 FunctionNo：`FGDSTOCKIN_E10ICD_CANCEL`；目前未掛正式 `MenuItem`。公司端 fallback DEV 入口不代表正式選單／ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_E10ICD_Cancel/frmInventory_FGDStockIN_E10ICD_Cancel.vb`，含 inline Designer；表格欄位布局在同一 Form。
- MES Service：`STD/MESws_STD/wsSTD/wsINV/wsINV.cs` 的 `LoadFGDInNoUnionSCRInNo`、`LoadINVFGDInDetail`、`LoadINVSCRInDetail`、`UpdINVInfo`、`ExeINVFGDINCancel`。
- ERP Service：Customer production `wsERP_E10ICD.ERP_Stock_IN_Cancel`。
- Core：`STD/MESkc/kcINV/clsInventory.cs` 的 `LoadFGDInNoUnionSCRInNo`、`UpdINVInfo`、`ExeFGDInCancel`、`ExeSCRInCancel`；由 `ExeINVFGDINCancel` 呼叫並掛 Lot release log action 14（INVCANCEL）。

## WinForms 流程

1. 以搜尋對話框挑選入庫單；欄位包含 INVInNo、InventoryType、InventoryNo、MO、Product、Version、InputDate。查詢呼叫 `LoadFGDInNoUnionSCRInNo(State=2, IsERP=true)`，Union FGD／SCRIN Basis＋Detail，只顯示 MES 已確認且 ERPNo 非空的入庫單。
2. 選取後由 InventoryType 對應 `FGDINV` 或 `SCRAPINV`。先試載 `LoadINVFGDInDetail`；有良品明細就顯示良品 Grid，否則載 `LoadINVSCRInDetail` 顯示不良品 Grid。可檢視 Lot／LotSerial、數量、來源庫、Component 與 Bin 明細；不能部分取消。
3. Confirm 重新呼叫上述 MES 清單並確認單據仍存在、State=2 且 ERPNo 非空，避免重複／不允許取消。其後 `UpdINVInfo(2,1)` 將 Basis 鎖到處理中，再呼叫 ERP `ERP_Stock_IN_Cancel`：良品 RequestType `0`／`stockin.data.process`，不良品 RequestType `1`／`wo.scrap.data.process`。ERP 只有 `code=0` 視為成功。
4. ERP 取消成功後先 `UpdINVInfo(1,-2)`，表示 ERP 已取消、MES 正在回復；再呼叫 `ExeINVFGDINCancel`。良品走 `ExeFGDInCancel`，不良品走 `ExeSCRInCancel`。MES 成功後 `UpdINVInfo(-2,-1, CancelUser, CancelDate)`；完成後單據不可再從 State=2 清單挑選。
5. ERP 取消失敗時 Form 以 `UpdINVInfo(1,2)` 還原 State=2，未執行 MES 庫存回復。若 ERP 成功但 MES 回復失敗，Form 不把 State=-2 還原，ERP 與 MES 形成待恢復的不一致狀態。Form 會清 Grid／單號；應由正式端提供可追蹤的對帳／恢復流程。

## Core 庫存回復

- 良品 `ExeFGDInCancel`：依 FGD Basis／Detail 還原來源 WIP Inventory SEMI、BookingFlag、FGD Inventory Qty、Component／動態 Bin 數量、Lot `CurQty`／`SysQty`，並記錄 Lot 轉移歷程。僅 `Source=1` 會回填線邊庫；有些 FGD 單來源是手動開立。
- 不良品 `ExeSCRInCancel`：依每列 Detail `Source` 還原 Lot Scrap、Component Scrap 或 Fail Bin 至其來源庫，減少 SCR Inventory，處理 Component／FailBin 明細、來源 Reservation 與 Lot 歷程。Bin 欄依 `TBLSYSBIN` 動態讀寫。
- FGD `TBLINVFGDINBIN` 是 Core 明確讀取後回加 `TBLWIPGOODBINSTATE`、再刪除的表；目前 repo 沒有其 Oracle DDL／metadata，WP621 已列為缺項。不得推測固定 Bin 欄位。其他已見來源沿用 WP201／WP603／WP605／WP215 references；不新增表。
- `ExeFGDInCancel`／`ExeSCRInCancel` 以 `CollectionSQL`／`ExecuteSQLNoneQuery_UPD` 寫多表，正式端需核查 helper 交易行為。ERP Web Service 與本地 DB 必非單一原子交易，需以冪等命令、外部結果記錄及可恢復狀態協調。

## 狀態欄位注意事項

`UpdINVInfo` 每次都把第一個 `OriginState` 引數寫進 `OrgState`，而不是只保留最初 State：流程依序為 `(2,1)`、成功後 `(1,-2)`、結束 `( -2,-1)`；ERP 失敗則 `(2,1)` 後 `(1,2)`。因此 `OrgState` 最後會是上一個狀態而非原始狀態。家用端應保存穩定的原始狀態／取消操作紀錄，勿照抄此覆寫行為。

## Vue DEV 頁

- Fallback 路由：`/YMES/WIP/FgdStockInE10IcdCancel`；FunctionNo `FGDSTOCKIN_E10ICD_CANCEL`。
- 合成良品與不良品已確認收貨、ERP 單號、整張明細及 Component／動態 Bin；搜尋只列 State=2 且有 ERPNo 的資料。
- 模擬 ERP／MES 取消成功、ERP 取消失敗後回復可取消狀態、ERP 已成功但 MES 回復失敗（State=-2）。呈現 `OrgState` 在原程式被逐步覆寫的差異。
- 僅 DEV 預覽可操作；Production 不讀假資料、不呼叫 ERP／MES、不連資料庫。搜尋表格取代 WinForms 對話框是呈現簡化。

## 家用端 API／資料 handoff

- 使用單一 `CancelInboundReceipt` 命令，明確傳入 `FGDINV`／`SCRAPINV`、MES 入庫單號、ERP 單號、idempotency key 與期望版本；伺服器重查 State=2、ERPNo、種類、單據明細及使用者取消權限，並鎖住該單避免多客戶重複取消。
- 記錄 ERP request／response 與外部操作識別碼。良品 RequestType=0、不良品=1，僅 ERP code=0 進入 MES 回復；timeout／unknown outcome 必須先查 ERP 結果或進待對帳佇列，不能盲目重送或把單據重新開放。
- ERP 成功、MES 回復失敗時保留「ERP 已取消、MES 待恢復」狀態與可重試／人工對帳工作項。完成後同一命令重送須回傳既有結果，不重複改數量。
- MES 取消需在單一本地交易內更新 Basis／Detail、良品／不良庫、Component／動態 Bin、線邊來源庫、BookingFlag、Lot Qty／Log 與最後 State。家用端比對 live schema／migration；桌機未連 PostgreSQL、未實作／驗收 API 或 schema。
- `TBLINVFGDINBIN` Oracle metadata 缺項沿 WP621；需由具安全 MESTEST profile 的公司端用唯讀方式補查，未取得前不可產 CREATE-only 或 migration。

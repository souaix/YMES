# 取消入庫至 TipTop ERP（WP610）

## 來源與範圍

- FunctionNo：`FGDSTOCKIN_TP_CANCEL`，清冊標示未掛正式 `MenuItem`；DEV fallback 不表示正式選單或 ACL 已開放。
- WinForms：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_TP_Cancel/frmInventory_FGDStockIN_TP_Cancel.vb` 與 Designer。
- MES Service：`STD/MESws_STD/wsSTD/wsINV/wsINV.cs` 的 `LoadFGDInNoUnionSCRInNo`、`LoadFGDInDetailUnionAllSCRInDetail`、`UpdINVInfo`、`ExeINVFGDINCancel`；工單狀態另呼叫 `wsOE.LoadMOBasis`。
- ERP：Form 直接呼叫 TipTop SOAP `TIPTOPServiceGateWay.UndoCreateStockInData`。
- Core：`STD/MESkc/kcINV/clsInventory.cs` 的 `LoadFGDInNoUnionSCRInNo`、`LoadFGDInDetailUnionAllSCRInDetail`、`UpdINVInfo`、`ExeFGDInCancel`、`ExeSCRInCancel`。

## WinForms 與狀態流程

1. 查詢清單及 Confirm 重查均用 `LoadFGDInNoUnionSCRInNo(State=2)`，沒有傳 `IsERP=true`，也沒有比對 ERP source。Core union FGD／SCRIN basis；`IsERP` 為 true 時才排除空 ERPNo，但 TipTop Form 未使用此參數。
2. 選單後用 `LoadFGDInDetailUnionAllSCRInDetail` 顯示種類、庫別、MO、Lot、Product／Version、GoodQty／ScrapQty／LossQty、Unit。Confirm 逐筆呼叫 `wsOE.LoadMOBasis`；只要 MOState=99 即禁止整單取消。另要求單據仍在 State=2 清單內且 ERPNo 非空。
3. Form 以 `status=Y`、`sfu_file.sfu01=INVInNo`、`sfumodu=tiptop` 呼叫 `UndoCreateStockInData`；只有回覆 code=`0` 算 ERP 取消成功。`auto=Y`，request／response 和結果寫入 TipTop transaction log。FGD／SCRIN 的 MES 類型由明細 `INVType` 傳給後續 Service；TipTop ERP 取消呼叫本身沒有 T100 的 RequestType 0／1。
4. 取消前以 `UpdINVInfo(OriginState=2, NewStatus=1)` 防止重複操作。ERP 失敗時更新 `(1,2)` 還原 State=2。ERP 成功後更新 `(1,-2)`，再呼叫 `ExeINVFGDINCancel` 並帶入 `ERPName=TipTop`；MES 成功後更新 `(-2,-1)` 並寫入 CancelUser／CancelDate。ERP 已取消、MES 失敗會留在 State=-2，不再出現在 State=2 清單。
5. 舊 `UpdINVInfo` 每次將傳入 OriginState 覆寫到 OrgState；成功路徑最終 OrgState=-2，ERP 失敗還原後 OrgState=1，不保留單一穩定的原始狀態。

## MES Core 回復

- `ExeINVFGDINCancel` 依 `FGDINV`／`SCRAPINV` 去重後派送 `ExeFGDInCancel`／`ExeSCRInCancel`，共用取消 Core 與 WP620／WP623。
- FGD 取消會檢查目的 FGD inventory 足夠，再扣 FGD inventory、回增 WIP SEMI 數量並清除 reservation；回復 `TBLINVFGDINBIN` 對應 Good Bin 數量；依 Component inventory 決定 Lot SysQty 換算，回增 Lot CurQty／SysQty，將 Component inventory 移回 WIP 並補回 Component State，寫入 action 14 Lot release log。Source=1（由 WIP inventory 入庫）才有來源庫回補。
- SCRIN 依來源類型回復 Lot Scrap、Component Scrap 或 Fail Bin，扣減 SCR inventory，並寫入 action 14 歷程；完整行為沿用 WP620／WP623 規格。
- Core 將多筆 SQL 收集後透過 `ExecuteSQLNoneQuery_UPD` 執行；ERP SOAP 與 MES 多表變更不在同一個分散式原子交易中。ERP 成功、MES 失敗需提供正式恢復／對帳流程。

## Oracle metadata 與資料範圍

- 沿用 WP201／WP603／WP605／WP215 已有的庫存、Lot、Component references，沒有新增直接表的 CREATE-only handoff。
- Core 會讀取／更新 `TBLINVFGDINBIN` 和 `TBLWIPGOODBINSTATE`；`TBLINVFGDINBIN` Oracle DDL／metadata 仍缺，沿 WP621／WP623 記錄。公司桌機目前沒有可用的受保護 Oracle profile，未連 Oracle、未猜欄位或密碼。
- 公司端沒有連 YMES PostgreSQL、沒有實作正式 API／schema，也沒有呼叫 TipTop 或 MES Service。

## Vue DEV 頁與差異

- Fallback 路由：`/YMES/WIP/FgdStockInTpCancel`；清冊標記未掛正式 `MenuItem`。頁面只在 DEV 預覽讀取合成資料。
- 模擬 State=2 搜尋、FGD／SCRIN 明細、ERPNo 空白、TipTop／T100／E10ICD 來源、已結案 MO、ERP 取消失敗、MES 回復失敗及成功情境。State=-2 recovery row 會留在歷史但不列入 State=2 查詢。
- 原查詢未限制 ERP source；DEV 仍顯示外來源樣本，但阻止把它送到 TipTop。正式端需依 ERP source 路由或拒絕。原 Form 的 `MOState=99` 守門在 DEV 有模擬。
- DEV 將 FGD／SCRIN 顯示為同一個 TipTop `UndoCreateStockInData` API，不沿用 T100 RequestType；取消成功只模擬 UI 狀態，不呼叫外部服務或資料庫。

## 家用端 API／schema handoff

- 伺服器重新查單據版本、State=2、ERPNo、ERP source、INVType、每筆 MOState、使用者 ACL 與庫存回復前置條件；以單據鎖及 idempotency key 防止重複取消。不可照抄舊查詢將其他 ERP 入庫單送往 TipTop。
- 持久化 TipTop Undo intent／request／response 和 unknown outcome；timeout 先查 ERP 狀態或排對帳。只有確定 ERP 已取消才進 MES 本地交易。
- 在單一本地交易回復 FGD／WIP／Component／Good Bin／Lot Qty／預約與 release log，再完成 State=-1；ERP 已取消但 MES 失敗須留可恢復狀態，不重開 State=2。`OrgState` 保存規則應改為保留原始值並另外記錄逐步狀態事件。
- 對照 YMES live schema／既有 migration 後再補必要 schema；`TBLINVFGDINBIN` 的來源欄位尚待具受保護 Oracle profile 的公司端補查。家用端負責 PostgreSQL migration、正式 API、部署及驗收。

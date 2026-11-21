# 入庫至 WorkFlow ERP（WP614）

## 來源與範圍

- FunctionNo：`FGDSTOCKIN_WF`，選單清冊標示未掛正式 `MenuItem`；本機 DEV fallback 不代表正式選單或 ACL 已啟用。
- WinForms 主頁：`STD/MESWin/WIP/WP_Inventory_FGDStockIN_WF/frmInventory_FGDStockIN_WF.vb` 與 `.Designer.vb`；重拋對話框：`frmFGDStockIN_ReThorwERP.vb` 與 `.Designer.vb`。
- MES EAI Service：`STD/MESEAI/EAIws/wsMES.asmx.cs` 的 `MOStockIn_Check_WF`、`MOStockIn_WF`、`LoadWIPInventory_SEMIbyMO_WF`、`MOStockIn_WF_AddReThrowTicket`、`MOStockIn_WF_Cancel_ForStockIn`、`MOStockIn_WF_ConfirmState`。
- EAI Core：`STD/MESEAI/EAItc/clsWIP.cs` 的 `loadWIPInventory_SEMIbyMO_WF`；`STD/MESEAI/EAItc/clsINV.cs` 的同名入庫／檢查／重拋票券／取消／ConfirmState 方法。
- ERP：WinForms 直接呼叫 `wsERP_WF.WFMESService.XMLAdapter`；入庫與 Undo XML 均記錄 transaction log。非正式 API 的既有服務呼叫在公司 DEV 頁不會執行。

## Form／Designer 與查詢

- 主頁欄位包含 MO、MO Type、Factory、ERP Sheet Type、MES 入庫單號、FGD Inventory、Locator；中央 Grid 為 WIP SEMI／SCRAP 庫存候選，另有 `ReThorwERP` 按鈕。
- `ALLOWINV2ERP=0` 時 Sheet Type 為 `MES`，只做 MES 入庫；`1` 送 ERP 但略過預入庫數量檢查；`2` 送 ERP 並先執行 `MOStockIn_Check_WF`。送 ERP 取得的 Sheet Type 使用 `getMFGDocument_WF(ReferenceType=3, ReferenceCase=1)`。`ShowLossData_FGDStockIN_WF=1` 才把 Loss-only 列納入可見 Grid；永遠只顯示 `BookingFlag=0` 且 Good／Scrap／Loss 至少一項大於 0 的列。
- MO 清單查 `MOState=3|6`。每次查詢一張 MO 的線邊庫存，會把其他已勾選列保留，所以可將多 MO Lot 累積至同一張入庫單。查詢由 `clsWIP.loadWIPInventory_SEMIbyMO_WF` union WIP Semi、WIP Scrap、Component Scrap，依 Lot／MO／產品／版本／Unit／LotSerial 聚合並帶 ERP 品號、ERP Unit、原始 Lot、BookingFlag。
- 成品庫與 Locator 必填；系統對每列加入可編輯的目的 FGD Inventory／Locator。使用者選整列 Lot，不輸入部分 InInvNum；ERP `MOCTG.TG011` 為 GoodQty+ScrapQty，TG012／TG013 分別為 Scrap／Good。`LossQty` 由 MES 明細接收，但未併入這個 ERP 入庫數量欄位。MO 工單類型拆出字串後綴送 `TG033`，工廠送 `MOCTF.TF004`，目的庫／儲位送 `TG010`／`TG034`，OrgLotNo／LotNo 受 `OrgLotNoToERP` 控制，ERP Unit 優先取產品 `ERPUnitNo`、空白時退回 MES Unit。

## Submit、ERP 回應與重拋

1. Confirm 檢查 Sheet Type、MO、成品庫、Locator 及至少一筆勾選列。`ALLOWINV2ERP=2` 對選取 MO 執行 `MOStockIn_Check_WF`：以 MOQty 減既有 FGD 入庫及 SCRIN 數量限制新增 Good+Scrap，並依 MO MaterialList 標準用量比較 ERP 已發料減退料量。舊 Core 對同 MO 多選列逐筆檢查；計算目標量沒有累加本次其他已選列，可能漏掉批次總量超入，正式端需以本次完整 payload 聚合重驗。
2. `wsMES.MOStockIn_WF` 先寫 MES，不良／良品可在同張 MES 入庫號下同時存在。`clsINV.MOStockIn_WF` 寫 FGD／SCRIN Basis（起始 State=0、ToERP=1）、各自 Detail，搬動良品／Component／Scrap 庫存、更新 Lot CurQty／SysQty、BookingFlag、結批與 WorkFlow release log action 13。`ALLOWINV2ERP=1|2` 接著呼叫 ERP `XMLAdapter`；ERP 欄位以 `MOCTF` 表頭、`MOCTG` 明細、`ReferenceType=1`、`ReferenceCase=1` 組成。
3. WorkFlow 成功回傳 ERP 單號後以 `SheetType-ERPNo` 呼叫 `MOStockIn_WF_ConfirmState`，將 FGD／SCRIN State=2、回寫 ERPNo 並刪除重拋票券。`ALLOWINV2ERP=0` 則不呼叫 ERP，ConfirmState 以空 ERPNo 完成 MES 入庫。
4. ERP 明確失敗時呼叫 `MOStockIn_WF_Cancel_ForStockIn`，Core 還原 MES 入庫、將單據設為失敗 State=3，來源庫存重新可用。ERP 呼叫逾時、`result` 空白時不撤回已完成的 MES 入庫：`MOStockIn_WF_AddReThrowTicket` 將 State=1、OrgState=2，並在 `TBLERPRETHROWTICKET` 存 MESTicketNo／Type／SheetType／RequestXML，供使用者從重拋視窗選取後以原 XML 重送。
5. 重拋明確失敗會呼叫 MES 取消入庫；逾時則票券留存；成功則回寫 SheetType 前綴 ERPNo、State=2、OrgState=1 並刪除票券。查詢 `wsINV.LoadERPReThrowTicket` 預設 MESTicketType=0，依 SheetType／MES Ticket No 精確 `Like` 篩選，沒有 State 條件。
6. ERP SOAP 與 MES 多表寫入不是單一原子交易；`clsINV` 的 transaction／commit 範例已註解，最後將 `CollectionSQL` 傳入 `ExecuteSQLNoneQuery_UPD`。主 Form 外層通用 Catch 為空，特定失敗結果與未捕捉錯誤應在新 API 明確回報。

## 舊程式缺口與資料 metadata

- 主 Form 與重拋 Form 都只在 `ALLOWINV2ERP=1` 時從「代碼:名稱」取 Sheet Type 代碼；模式 `2` 可能把完整顯示字串送到 `MOCTF.TF001`。重拋查詢也只在模式 1 切代碼，模式 2 可能查不到實際存為純代碼的票券。DEV 以純代碼執行流程並顯示 source warning；正式端需修正兩處並加回歸情境。
- Loss-only 列受 `ShowLossData_FGDStockIN_WF=1` 可見，但 `clsINV.MOStockIn_WF` 的 FGDInState／SCRInState 分流以 GoodQty／ScrapQty 判斷；只含 Loss 的列沒有可證明的入庫分支。DEV 會顯示該列但阻擋送出，正式端先確認舊資料案例與預期 Core 行為。
- 來源表重用既有 WP201／WP603／WP605、WP101、OE、PRD、SMD references。程式另讀 `TBLOEMOMATERIALINBASIS_ERP`、`TBLINVWIP_SCRAP`，並新增查寫 `TBLERPRETHROWTICKET`；本 repo 沒有這幾張表的完整 MESTEST source DDL／metadata。桌機目前沒有可用的受保護 Oracle profile，所以本次未連 Oracle、未猜 CREATE-only。`TBLERPReThrowTicket.RequestXML` 會保存業務 XML，正式 API 必須限制存取與操作紀錄輸出。
- 公司端未連 YMES PostgreSQL，未建立 schema／migration，未呼叫 WorkFlow SOAP 或 MES EAI Service。

## Vue DEV 頁

- Fallback：`/YMES/WIP/FgdStockInWorkFlow`。以合成資料呈現 MO 3／6 查詢、多 MO 累積選列、成品庫／Locator、WIP Semi／Scrap／Component Scrap、Good／Scrap／Loss、ERP Unit 與 `ALLOWINV2ERP=0/1/2`。
- DEV 模擬 MES-only、ERP success、ERP fail 後 MES 取消（State=3）、ERP timeout 產生 State=1 重拋票，以及 MES fail／Undo 分支。重拋頁有 SheetType／MES Ticket No filter、RequestXML 標記、FGD／SCRIN 模擬明細、逐張多選重拋、成功綁 ERP 單號／刪票、失敗取消與再次逾時保留票券。
- DEV 數量檢查以整個選取 MO payload 聚合 Good+Scrap 與淨發料，避免舊 Core 多列漏算；此外限制一張 ERP 表頭不可混不同 Factory，並阻擋 Loss-only 列。這些是 DEV 安全守門與明確簡化，需由正式端確認對舊 WinForms 的差異。
- 僅 `import.meta.env.DEV && developmentPreview` 啟用假資料互動。production 不讀假資料，不連 ERP、MES 或資料庫。

## 家用端 API／schema handoff

- 實作有 ACL、冪等 key 與 request version 的 MES stock-in command。伺服器重查 MOState、完整 Good／Scrap／Loss 庫存、WIP／Scrap／Component source、BookingFlag、來源庫、Unit、ERP Unit、ERP 品號、MO Qty／既有入庫、ERP material issue、Factory／Inventory／Locator／SheetType；不可信任 browser 的 Lot、Qty、ERP XML 或手動設定 `ALLOWINV2ERP`。
- 入庫主檔／明細／庫存、Lot CurQty／SysQty、Component、BookingFlag、關帳及 release log 變更應在單一本地交易內完成。ERP Create 前持久化 intent；成功後再確認本地單據；明確 ERP failure 撤回 MES、timeout/unknown outcome 則保留待對帳狀態，不盲目重送。
- 將 `TBLERPRETHROWTICKET` 改為明確、可存取控制且具 audit 的 ERP integration intent／reconciliation record；檢查 ERP XML 中是否含敏感欄位，限制讀取及日誌，不提供任意 XML 重放入口。ERP 成功時原子回寫 ERPNo／State，MES 補償失敗時提供可恢復狀態及操作紀錄。
- 修正 ALLOWINV2ERP=2 的 SheetType code parsing；重拋查詢只讀到授權使用者可見且狀態確須重拋的票券。以多 Lot／多 MO／不同 Factory、Good+Scrap+Loss、Loss-only、物料足量不足、ERP fail／timeout／重拋重複與 MES failure 情境核驗。
- `TBLOEMOMATERIALINBASIS_ERP`、`TBLINVWIP_SCRAP`、`TBLERPRETHROWTICKET` 的 source DDL／metadata 待取得受保護 Oracle profile 的公司端唯讀補查；家用端須先比較 PostgreSQL live schema／migrations，再決定必要新增結構。家用端負責 migration、正式 API、部署及驗收。

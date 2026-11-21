# Bump MAP 疊圖作業（WPP07）

## 範圍與已核來源

- 清冊：`P_BUMP MAP MERGE`／Bump MAP疊圖作業，FunctionKey `WPP07`，分類「芯片測試生產管理」；inventory 的 `IssueTable` 為空。
- Form 與 inline Designer：`D:\iMES\CSiMES\IC_CP_PKG\MESWin_IC_CP_PKG\WIP\WP_P_MergeStandardMAP\frmMergeStandardMAP.vb`。MAP 檢視子窗：同目錄 `frmMergeStandardMAP_Viewer.vb` 與 `.Designer.vb`。
- Service：`D:\iMES\CSiMES\IC_CP_PKG\wsIC_CP_PKG\wsWIP\wsWIP.cs`（`LoadBumpMapMergeState`、`AddBumpMAPMergeStateAndLog`）；同專案 `wsPRD\wsPRD.cs`（`LoadMergeMAPRule`／`LoadMergeMAPRuleDetail`）；`wsSYS\wsSYS.cs`（`LoadMAPTemplateDetail`／`LoadMAPTemplateDetailB`）。
- Core：`D:\iMES\CSiMES\IC_CP_PKG\MEStc_IC_CP_PKG\tcWIP\clsLot.cs`（疊圖檔案／狀態／歷程與 component update）；`tcPRD\clsProduct.cs`、`tcSYS\clsSYSEng.cs`（規則與模板查詢）。共同 Lot、ComponentState、ComponentBasis 來源沿用既有 WinForms／Core 與 repository references。

## WinForms 流程

1. 使用者輸入 LotNo 後按 Enter。Form 載入 Lot／MO／產品／CustomerLot／OP／目前及系統數量；先檢查 `Status in (0,1,2,3,4,5)`，再另檢查第一筆 `LotState` 不可為 0。此處來源使用兩個欄位判斷；正式端須依實際 Core 回傳契約核實，不能把兩欄視為同一欄。
2. 查產品／版本對應 Golden MAP、CustomerLot 的 Bump MAP、Lot 的 ComponentState 及 ComponentBasis，依 Component 對應 WaferLot／WaferID，並查目前合併狀態。Bump MAP 檔不存在的元件列灰階、不可勾選；全選只選可用 Bump MAP 的列。
3. 取得 IssueState=2 核准 Merge MAP Rule，按下列優先序比對：Customer／ProductType／Product／Version 完全相符、同前三欄且 Version=`*`、同 Customer／ProductType 且 Product／Version=`*`、同 Customer 且後三欄=`*`、全部為 `*`。VendorNo 與 OPNo 在此 Form 固定查 `N/A`。再載入 Source／Reference MAP Template Detail、DetailB、Merge Rule Detail／Sub。
4. 手動 MergeMAP 要求至少一筆選取、每筆有 Bump MAP、Golden MAP 可用、非已凍結狀態，且兩份 Template 與兩份 Merge Rule Detail 均有資料。檔案流程依序下載 Golden/Bump MAP、解析與比對 WaferLot／WaferID、WaferFlat 方向及 X/Y 尺寸、依模板／規則疊圖產生 UFMAP、上傳並確認檔案存在，再新增現況與歷程、更新 `TBLWIPCOMPONENTBASIS.P_BUMPMERGESTATUS=1`。
5. UndoFrozen 只接受所有選取列均為 `FreezingStatus=1`；Core 將現況更新為 0，重建現況／歷程並把 component 的 `P_BumpMergeStatus` 設為 0。現有 UFMAP 檔案不會因解除凍結而刪除。
6. Grid 可查看已存在的 UFMAP 結果；上方「查看 MAP」預覽 Golden MAP。狀態包括 Bump MAP 檔案存在與否、UFMAP 未疊圖／已疊圖／檔案不存在、Merge／Freezing 狀態與時間。

Form 還有 `pMergeMAPMode=1` 的自動疊圖入口：由上游傳入 Lot、元件、數量及產品資料，載入後自動呼叫 Merge；已凍結資料被當成自動模式成功略過，完成後關閉並回傳產品號。本次 DEV 對應清冊手動選單，未接上游自動呼叫者。

## Vue DEV 原型

- 元件 `web/src/components/WipBumpMapMerge.vue`，fallback menu 分類「芯片測試生產管理」，路徑 `/YMES/WIP/BumpMapMerge`。FunctionNo／FunctionKey 沿用清冊 `P_BUMP MAP MERGE`／`WPP07`。
- DEV 假資料可載入生產批、查看 Lot／MO／產品／CustomerLot／OP／數量、顯示規則與模板、逐列／全部選取可用 Bump MAP、查看 Golden／UFMAP 示意圖、模擬 Merge 進度及解除凍結；選取中的 Queue、缺 Golden、缺 Bump、無規則、已凍結等錯誤會呈現。
- 二進位 MAP 解析／旋轉／座標比對、FTP/檔案服務下載上傳及真實 COM 產檔未在瀏覽器執行。DEV 只在記憶體內產生狀態／歷程示意，不讀寫 FTP、Oracle、YMES PostgreSQL 或真實檔案；不代表正式疊圖結果。

## DDL／metadata 狀態

- 已有 Oracle source DDL 與 PostgreSQL CREATE-only：`TBL_BPRDMERGEMAPRULE`、`TBL_BPRDMERGEMAPRULEDETAIL`、`TBL_BPRDMERGEMAPRULE_SUB`（`database/reference/mestest_prd_cp_merge_map_source_ddl.sql`、`mestest_prd_cp_merge_map_create.sql`）；`TBLWIPCOMPONENTSTATE`（WP101 refs）、`TBLWIPCOMPONENTBASIS`（WP215 refs）。
- 尚缺 Oracle source DDL／metadata 與相應 CREATE-only 的直接相依表：`TBL_PWIPGOLDENMAPFILESTATE`、`TBL_PWIPBUMPMAPFILESTATE`、`TBL_PWIPBUMPMAPMERGESTATE`、`TBL_PWIPBUMPMAPMERGELOG`、`TBL_BSYSMAPTEMPLATEDETAIL`、`TBL_BSYSMAPTEMPLATEDETAILB`。不以 Core SQL 或 PRD Rule 表結構推造這六張表。
- 本桌機沒有可用的受保護 Oracle profile，筆電 attachment 亦不存在於此機；因此沒有執行 Oracle metadata 查詢，也未查任何業務資料。本項不新增猜測 DDL／CREATE-only。補齊前，家用端先沿既有 refs 核對已知表，對上列六表保留待查狀態，不先建 migration。

## 家用端 API／檔案服務 handoff

- 先 pull 最新 `main`，比對 YMES live schema 與現有 migrations；保留既有擴充欄位。六張未核實來源 DDL 的表暫不遷移；取得精確 metadata 後再形成 CREATE-only／migration。
- 提供 WPP07 權限保護的 Lot／component／Bump MAP／Golden MAP／Merge State 查詢，以及 Merge／UndoFrozen commands。伺服器重新驗證 Lot 狀態、Component 對應、檔案版本、已核准規則的 wildcard precedence、模板與凍結狀態；一批涉及檔案和多列 DB 狀態，需可重試／補償的操作記錄與原子資料更新。
- 不把檔案 binary 放在瀏覽器。正式端需設定受保護的 CP 檔案服務，下載後驗證 WaferLot／WaferID／WaferFlat／X/Y 尺寸，呼叫已核准 MAP engine 產出，再上傳、確認檔案存在後更新狀態；actor、檔名、檔案 checksum、批次 id 及前後狀態需可追查。公司桌機不設定 FTP 憑證或連檔案服務。
- `clsLot.LoadBumpMapMergeState` 的 BumpMergeSerial 條件誤以 WaferLot 作比較值；WaferId 條件也拼接 WaferLot。該查詢並以字串串接且無排序。正式查詢應用參數、正確欄位與穩定順序。
- Core 寫入路徑把多條 SQL 收集後交 `ExecuteSQLNoneQuery_UPD`；需在家用端確認實際交易邊界。Service 在 Core 寫入完成後另呼叫 EventLog，不可假設該事件與狀態更新原子。檔案先上傳後 DB 失敗時，正式 API 需有可恢復／清理策略。

# Codex handoff：公司筆電 → 桌電

更新：2026-11-24。用途：在公司端桌電／筆電 Codex 開啟 YMES repository 後直接接續，不必重做已完成調查。

> **最新公司端接手（2026-11-24）**：公司端已依清冊完成 IV502「開立成品／半成品入庫單」DEV 與規格交接；請先讀本檔的 IV502 checkpoint、[公司端待辦](../todolist_officepc.md)、[家用端待辦](../todolist_homepc.md) 及 [模組規格索引](MODULE_REPLICATION_SPEC_INDEX.md)，下一項依 INV 清冊為 IV503「成品入庫單記錄」。WP401 段落是較早的歷史接手點。

## 接手方式

1. 在桌電開啟同一個 GitHub repository `souaix/YMES`，執行 `git pull origin main`。
2. 先讀本檔、`todolist_officepc.md`、`todolist_homepc.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md` 與 [MES 選單清冊](MES_MENU_FUNCTION_INVENTORY.md)，再直接核對下一項 IV503 的 WinForms／Service／Core 原始碼。
3. 本機 Codex 使用 YMES repo；MES WinForms 原始碼在公司筆電 `D:\iMES\CSiMES`，桌電可能是另一個路徑。若桌電有既有 MES checkout，先核對路徑；不可用 UI 截圖代替 Form/Designer/Service/Core 原始碼。原始碼缺少時記錄具體缺檔，先做不依賴它的工作。
4. 若在桌電打開同一個 Codex 對話，thread 對話與 Goal 脈絡仍在；若另開新對話，把本檔及 TODO 當成接手上下文，不要假設新 thread 自動有舊 Goal 狀態。

## Repository 與分工

- Repository：`D:\YMes`（桌電可用不同 clone 路徑）；remote `https://github.com/souaix/YMES.git`，branch `main`。
- Vue 3 UI：`web/`。
- .NET 9 API：`src/YMes.Api/`。
- 共用 DTO：`src/YMes.Contracts/`。
- `src/YMes.Web/` 是既有 ASP.NET MVC 專案，不是目前 Vue UI。
- 公司端以 DEV 假資料完成 Vue 畫面/互動並記錄規格；家用端負責 PostgreSQL schema、API、migration/Create SQL handoff。DEV localStorage 不送入正式 API。

### 桌機與筆電的職責

- 公司桌機和公司筆電屬於同一個「公司端」，工作範圍相同：讀 MES WinForms/Service/Core、只讀查 Oracle MESTEST DDL/metadata、以 DEV 假資料完成全部 Vue UI/互動邏輯、整理規格與 API/SQL handoff。
- 公司端不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不建 PostgreSQL schema/migration、不實作或驗收家用端 API、不 SSH 或遠端部署。這些全部由家用電腦接手，依 `todolist_homepc.md` 執行。
- 公司桌機為 Vue DEV 安裝 Node.js 22.x（22.12+）與 npm，並安裝 `web/` 套件即可。桌機上的 .NET 9 SDK 和 API/PostgreSQL 連線不是公司端 UI 工作的前置條件；不要為了跑 API 在公司電腦設定 PostgreSQL 憑證。
- 公司端允許使用的是 MES 原生 Oracle MESTEST 受保護唯讀連線，僅用於查 DDL/資料字典；不在 Oracle 寫入資料。

## 目前檢查點與下一步

- **2026-11-24 INV IV502 接續點**：共用 `frmINVProductIn`／`frmINVProductInDef` 已完成 `/YMES/INV/ProductIn` DEV 與 [規格](modules/INV/開立成品半成品入庫單_IV502.md)。直接核對 FGD／SEMI Add／Edit／Delete／combined Load Service、`clsInventory`、`clsBasis_UPD`。DEV 支援 FGD 預設與 SEMI 切換、State=0／Source=1 草稿查詢、來源 WIP 半成品 Lot／Component 選取及假預約、完整最高層包裝子 Lot 檢核、DataStamp 編輯、作廢保留明細／解除預約和列印預覽。建立草稿不執行庫存過帳；IV501 Source=30 確認才過帳。
- FGD In 表沿用 WP605、SEMI In 表沿用 WP604；WIP Semi／Component、Lot、ReverseHistory、Package、Locator 使用 WP201／603／101／215／601／PRD112 refs，無需新增 Oracle DDL／CREATE-only。本家用端已以 0074 migration 建立經 live 確認缺少的 15 張來源表；完整備份、欄位／鍵／索引／權限回讀見 [資料庫連線紀錄](../DATABASE_CONNECTION.md)。本機已新增登入保護的 IV502 列表／父單號明細唯讀 API 並通過 .NET 9 build，尚未部署或接正式頁面；建立／編輯／作廢仍待實作。草稿交易仍須重驗 Qty、BookingFlag、Component、包裝完整性、DataStamp；作廢是 State=-1、釋放預約並保留明細。公司端下一項依清冊為 IV503「成品入庫單記錄」。

- **2026-11-24 INV IV304 接續點**：`WIP RETURN CONFIRM` 已依 inline `frmINVWIPReturnConfirm`／Designer、`wsINV.AddINVTransactionState/ExeINVTransactionState`、`clsInventory.ExeMaterialWIPReturn_ForMaterialWIPMOReturn` 完成 `/YMES/INV/WipReturnConfirm` DEV 與[規格](modules/INV/退料單確認_WIP.md)。DEV 模擬 State=0 查詢、Detail／Component 展開與單筆 Source=10 確認。Core 依 FromPlaceType 2／3／5／6／7 扣 WIP／MO source、回補 Raw／Semi 與元件庫存；Form 使用的 Add Service 會吞掉內部 Exe 失敗，Core 忽略 ToLocatorNo，Type 7 多目標分流需逐鍵核實。桌機受保護 Oracle profile 不存在；退料單及 Raw Inventory／Locator／Component DDL／metadata 未取得，不猜 CREATE。沿用 repo WP201／603／604／605 與 OE references。未連 YMES PostgreSQL、未 build/test/browser；下一項依清冊為 IV310「包裝拆並箱作業」。
- **2026-11-24 INV IV303 歷史接續點**：`WIP MATERIAL RETURN LOG` 已依 inline `frmINVMaterialWIPReturnLog`／Designer、`wsINV.LoadINVMaterialWIPReturnBasis/Detail/Comp`、`clsInventory` 完成 `/YMES/INV/MaterialWipReturnLog` DEV 與[規格](modules/INV/退料單記錄_WIP.md)。含 MaterialReturnNo、InventoryNo、FromPlaceType、State、來源 WIP／MO、CreateDate／ReturnDate 篩選及選列明細／Component 子列；ReturnDate 由 Basis.ReturnDate 映射成畫面的 ConfirmDate。表 DDL／metadata 因桌機沒有受保護 Oracle profile 而未取得，不猜 CREATE。Core Detail 空單號可能回全表；Component 查詢以最後一筆明細 LocatorNo 過濾且關聯缺 MaterialReturnNo／LocatorNo，正式 API 必須父單號限定、完整關聯、穩定排序及分頁。當時下一項依清冊為 IV304「退料單確認(WIP)」。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV302 歷史交接**：`WIP MATERIAL RETURN` 已依 `frmINVMaterialWIPReturn`／BasisDef／DetailDef、`wsINV`、`clsInventory`／`clsBasis_UPD` 完成 `/YMES/INV/MaterialWipReturn` DEV 與[規格](modules/INV/開立退料單_WIP.md)。含 State=0 單頭／明細維護、五種 FromPlaceType、WIP／MO 假來源、來源／退入 Locator、元件選取與 BookingFlag 假預約。單頭 Copy 不複製明細；Basis Edit 僅 Description。IV302 草稿不執行實際退料，確認在 IV304 Source=10。桌機受保護 Oracle profile 不存在，`TBLINVMATERIALWIPRETURNBASIS`／`TBLINVMATERIALWIPRETURNDETAIL`／`TBLINVMTLWIPRETURNCOMP` 與元件來源 DDL／metadata 未取得，不猜 CREATE。舊 Core 空 MaterialReturnNo Detail 查詢可回全表；Service 分別更新 Detail、Component 及 BookingFlag，正式 API 必須父單號限定並原子交易更新。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV211 接續點**：`MATERIAL RETURN LOG` 已依 inline `frmINVMaterialReturnLog`、`wsINV.LoadINVMaterialReturnBasis/Detail`、`clsInventory` 完成 `/YMES/INV/MaterialReturnLog` DEV 與[規格](modules/INV/退料單記錄.md)。含 8 組勾選條件、State 0–3、Basis ReturnDate／CreateDate 日期、選列載入 Detail／Component 子列。ReturnDate 是 Basis.ReturnDate，Form 顯示為 ConfirmDate，不用 TransactionLog 替代。桌機受保護 Oracle profile 不存在，Return Basis／Detail／Component DDL／metadata 未取得，不猜 CREATE。舊 Core 空單號 Detail 回全表、Component relation 少 LocatorNo、Basis 無排序／分頁已交家用端。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV206 接續點**：`MATERIAL RETURN` 已依 `frmINVMaterialReturn`／BasisDef／DetailDef、`wsINV`、`clsInventory`／`clsBasis_UPD` 完成 `/YMES/INV/MaterialReturn` DEV 與 [規格](modules/INV/開立退料單.md)。主／明細 State=0 CRUD、假 Vendor／Raw Lot lookup、元件 booking 本機模擬均已交接。桌機指定 Oracle profile 不存在，因此 Return Basis／Detail／Component、Raw Component／Locator 原生 DDL／metadata 未取得，不猜 CREATE。舊 Service 元件寫入分步、Core Edit 可能刪除整單元件、元件候選缺 InventoryNo 的來源差異已交家用端；正式 API 必須完整鍵、庫房範圍及原子交易。IV206 草稿不扣庫存，確認由 IV201 Source=3。公司端下一項依清冊為 IV211「退料單記錄」。未連 YMES PostgreSQL、未 build/test/browser。
- 公司端已接續 OE101、OE102、OE201 至 OE209、OEB01、OEP01 的 DEV 訂單與工單操作。各功能及 Oracle DDL／API 交接見模組索引。WP101、WP102、WP201、WP202、WP204、WP301、WP302、WP601、WP603、WP604、WP605、WP629 已完成公司端 DEV 與家用端 handoff；後續工作依 `todolist_officepc.md` 及 MES menu inventory 接續。
- **2026-11-20 WIP 接續檢查點：WP305–WP307 及 WP203／WP103／WP113／WP104／WP105／WP205–WP207 的公司端 DEV/spec/handoff 已同步至 `D:\YMes` 工作樹。** 同步前逐檔比對、於 `D:\iMES\CSiMES\ymestage\wp207_sync_backup_20261120` 備份七個有差異的既有檔案並核實 SHA256；新增檔與既有 WP305 未提交檔均已保留。WP307 的 State/History ShiftNo／UserNo lookup 與正式 API contract 已列於規格與 `todolist_homepc.md`。
- Oracle 僅以受保護 MESTEST profile 讀取 WP307、WP206–WP209、WP213、WP215 等結構字典，未查業務資料、未執行 DDL/DML。source DDL、metadata、CREATE-only 與兩端 TODO 均在工作樹。公司端仍未連 YMES PostgreSQL、未實作 Production API/migration，也未執行 build/test/browser；WP208–WP215 公司端 DEV／規格已完成，後續依 menu inventory 選下一項。
- **2026-11-20 同步檢查點**：公司端 WP207 以前的工作保留家用端 14 筆遠端提交，經 rebase 以 `b3f0f06` 正常推送 GitHub main。WP208 沿同一版本建立 staging；整合時須再次核對遠端與家用端新異動，逐檔備份／比較，不 force push。WP208 Core 直接引用 27 張表；其中 `TBLSYSPARAMETER` 原先缺 SQL reference，已由公司端 PowerShell 5.1／受保護 Oracle profile 唯讀取 DDL／metadata並補 CREATE-only，其餘 26 表沿用先前 refs。家用端須實作元件守恆的交易 API。
- **2026-11-20 WP208／WP209 接續**：WP208 已以 `dbebd0f` 正常推送 main。WP209 `LOT SPLIT_BIN` 已依 WinForms／Service／Core 完成 Vue DEV、規格與 Oracle 唯讀 DDL／CREATE-only 交接，路由 `/YMES/WIP/LotSplitBin`。`TBLWIPSPLITCONTENT_BIN` 已查得完整結構；`TBLWIPSORTERBINDATA` 在目前 MESTEST `ALL_TABLES`／`ALL_OBJECTS`／`ALL_SYNONYMS` 均不可見，只影響 Core 的 LED_PKG 分支，待正確 DDL，不猜表結構。家用端負責 live PostgreSQL 核對、交易 API 與正式驗收。公司端下個 menu 功能依 inventory 查 WP210；WP209 本輪未執行 build/test/browser。
- **2026-11-20 WP210 接續**：WP210 `LOT SPLIT_CARRIER` 已依主 Form、元件 Slot picker、Service 與有／無元件兩條 Core 完成 Vue DEV 與 API 規格，路由 `/YMES/WIP/LotSplitCarrier`。兩條 Core 直接引用 17 表，逐名核對均已有 CREATE TABLE reference，沒有新缺表需 Oracle DDL／CREATE-only；家用端仍要核 live PostgreSQL、追 helper 間接表並完成載具／元件 Slot 一致交易。原 Change Product 按鈕未傳產品欄的來源差異見模組 MD。下個 menu 功能為 WP211「併批處理」。
- **2026-11-20 WP211 接續**：WP211 `LOT MERGE` 已依 `frmBR_L_Merge_GEN`、`Exe_Manual_LotMerge` 與 `BR_Manual_LotMerge` 完成 Vue DEV、`/YMES/WIP/LotMerge`、規格與家用端 API handoff。八張直接表均有既存 source DDL／CREATE-only；沒有新 SQL。來源中至少兩批的檢查已註解，MO 跨工單由 `AllowDiffMOMerge` 控制，OS、包裝、GoodBin 與 BR 後續轉站分支見模組 MD。下個 menu 功能為 WP212「併批處理_元件」。
- **2026-11-20 WP212 接續**：WP212 `LOT MERGE_COMP` 已依獨立 WinForms、Service、Core 完成 `/YMES/WIP/LotMergeComponent` Vue DEV、右側元件 Grid、至少兩批與載具守門，並交付規格及家用端 API handoff。十張直接表均有 source DDL／CREATE-only，無新 SQL。Form 的載具檢查迴圈實際只查 `txtLotNo`，正式 API 須逐來源批檢查，詳模組 MD。下個 menu 功能 WP213「併批處理_等級」。
- **2026-11-20 WP213 接續**：WP213 `LOT MERGE_BIN` 已依獨立 WinForms／Service／Core 完成 `/YMES/WIP/LotMergeBin` Vue DEV、SysBin 動態欄、GoodBin 參考批、全選、Go／Assign 與家用端規格。Core 八張直接表中七張沿用現有 refs，`TBLWIPMERGECONTENT_BIN` 以受保護 Oracle profile 唯讀取得 22 欄 source DDL／metadata，並提供 CREATE-only。家用端須核 live PG、實作 Bin/GoodBin 交易；下個 menu 功能 WP214「併批處理_載具」。
- **2026-11-20 WP214 接續**：WP214 `LOT MERGE_CARRIER` 已依 `frmBR_CL_Merge_GEN`／`frmCarrierMerge_Comp`、Service／Core 完成 `/YMES/WIP/LotMergeCarrier` Vue DEV，一般載具合併與有元件 Slot 移動都在頁內。11 張 Core 直接表均已有 source DDL／CREATE-only，無新 SQL；原 Form 外包 helper 錯傳 CarrierNo、Core 同 OS AND 條件疑點已交接。家用端核 live PG、實作載具／Slot／元件一致交易。下一個 menu 功能 WP215「元件分片處理」。本輪未執行 build/test/browser。
- **2026-11-20 WP215 接續**：WP215 `LOT COMP SPLIT` 已依 `frmSplitComponent`、`wsWIP.InitRunSplitComponentNo`／`EXE_SplitComponent`、`clsWIP` 核對並完成 `/YMES/WIP/ComponentSplit` Vue DEV、2–26 元件預覽、良／報廢量守恆、載具拒絕與家用端 API 規格。14 張 Core 直接表中 12 張沿用 refs；公司端用受保護 MESTEST 唯讀取得 `TBLWIPCOMPONENTBASIS`（32 欄）與 `TBLWIPREVERSEHISTORY`（11 欄）source DDL／metadata，提供 CREATE-only。家用端核 live PG 再做缺口 migration／原子編號與交易。原 CarrierNo 欄隱藏且 Core 新元件上載具已註解。未執行 build/test/browser。
- **2026-11-20 WP914 接續**：`LOT HOTLOT AND PRIPORITY CHANGE` 已核 WinForms Form／Designer、`wsWIP.EditLotBasis_Confirm`、`clsLot.EditLotBasis`，完成 `/YMES/WIP/HotLotPriority` Vue DEV 查詢／勾選／掃描／HotLot 與 Priority，規格與家用端 API handoff 見模組索引。只更新 `TBLWIPLOTBASIS`，所需源表均有 SQL reference；無新 Oracle 查詢或 CREATE SQL。原 Form 逐列按 BaseLotNo 更新可能部分成功，正式批次應以版本／交易和去重處理。下個待處理群組為 WP501／WP502 等「異常與等待」；WP501 Set 會開 `WP_BRERFCreate` 約 5,000 行共用表單，須一併核對。未執行 build/test/browser。
- **2026-11-20 WP501 接續**：已核 `frmImmediateHoldSet`、共用 `frmBRERFCreate`、`wsWIP.Exe_ERF_Create`、`clsLot.UpdERF_Create`，完成 `/YMES/WIP/ImmediateHoldSet` Vue DEV 查詢、Set、異常單分類／原因／元件 Area／Confirm 本機模擬。受保護 MESTEST 唯讀查得 `TBLWIPERFCREATEDATA`、`TBLWIPERFHOLDREASON`、`BKTBLWIPERFCREATEDATA` 的 source DDL／metadata；另三張 ERF Basis／ReasonDisp／AttachFile 沿用 QC409 source，六表 CREATE-only 在 `mestest_wip_wp501_create.sql`。家用端核 live PG 再做正式原子交易。附件/PDF/通知與設備實際占用交易仍是 DEV 簡化；設備共用/獨佔假分支已在後續補核，詳下方記錄。未執行 build/test/browser。
- **2026-11-20 WP502 接續**：已核 `frmImmediateWaitSet`、共用 `frmWaitDescription`、`wsWIP.Exe_Wait_Set`、`clsLot.SetLotStateToWait`，完成 `/YMES/WIP/ImmediateWaitSet` Vue DEV 七欄查詢、15 欄勾選 Grid、Select All／掃描、彈窗說明與逐批假 Wait。原 Form 將多批 LotNo 逗號連接後送給只接受單值的 `CheckLotPackage`，正式端須逐筆查；原迴圈中途失敗可部分完成。WaitBasis 等七張 Core 直接表沿用現有 refs，無新增 CREATE。下一項可核 WP503 批次立即異常設定。未執行 build/test/browser。
- **2026-11-20 WP503 接續**：已核 `frmImmediateHoldSet_Batch`、專屬 `frmBRERFCreate_Batch`、`wsWIP.Exe_ERF_Create_Batch`、`clsLot.UpdERF_Create_Batch`，完成 `/YMES/WIP/BatchImmediateHold` Vue DEV 十欄＋時間查詢、16 欄多選 Grid、批次異常彈窗和 `ERFBatchHoldGenType` 0/1 假交易。0 為單 ERF 多批、寫 BatchLot；1 為逐批 ERFType2、不寫 BatchLot且可能部分成功。BatchLot Oracle source 沿用 QC409，本批補單表 PG CREATE-only。Core 的 FutureHoldLog/Reason 僅 ERFType3 使用，已有公司端受保護 Oracle 唯讀結構供下一個 Future Hold 規格；不列 WP503 migration。詳見模組規格；未執行 build/test/browser。
- **2026-11-20 WP504 接續**：已核 `frmFutureHoldSet`、`wsWIP.Exe_FutureHold_Set`／`CheckLotPackage`、`clsLot.UpdFutureHold_Set`／`ChkFutureHold`，完成 `/YMES/WIP/FutureHoldSet` Vue DEV 六欄 Query、八欄 Lot Grid、Selected Lot/掃描、PS→OP 多選、Start/End、QC 原因與假 Set。原 Form 多批包裝檢查有逗號單值缺陷；Core 按 Lot×OP 替換 FutureHold/Reason，Set 不立即 Hold。WP503 階段唯讀查到的 Reason/Log DDL/metadata 已交付，PG CREATE-only 保留 Log 無 PK。規格與家用端 API/生命週期 handoff 見索引；未執行 build/test/browser。下一項依 inventory 可處理 WP505 預先等待設定、WP506 預先異常解除。
- **2026-11-20 WP505 接續**：已核 `frmFutureWaitSet`、`wsWIP.Exe_FutureWait_Set`／`CheckLotPackage`、`clsLot.UpdFutureWait_Set`／`ChkFutureWait`，完成 `/YMES/WIP/FutureWaitSet` Vue DEV 六欄 Query、六欄 Lot Grid、Selected Lot/掃描、PS→OP 多選、WaitTime Start/End、可空說明與假 Set。原 Form 多批包裝檢查同樣有逗號單值缺陷；Core 按 Lot×OP×WaitTime 交易替換 FutureWait，不立即 Wait。FutureWait source/CREATE 沿用 WP201，無新 SQL；下一項 WP506/507 Release。未執行 build/test/browser。
- **2026-11-20 WP506 接續**：已核 `frmFutureHoldRelease`、`wsWIP.DelFutureHold`、`clsLot.DelFutureHold`，完成 `/YMES/WIP/FutureHoldRelease` Vue DEV 四種勾選查詢、九欄 Grid、單列 Release/Close，並讀寫 WP504 共用假資料。來源只檢選取列 LockFlag，Core 以 Lot/OP/HoldTime 刪主表與原因，可能刪同鍵其他 Module/Serial/鎖定列；DEV 先檢整個實際刪除範圍。資料 DDL/CREATE 沿用 WP201/WP504，無新 SQL；正式 API 的精確或批量契約交家用端。下一項 WP507 預先等待解除。未執行 build/test/browser。
- **2026-11-20 WP507 接續**：已核 `frmFutureWaitRelease`、`wsWIP.DelFutureWait`、`clsLot.DelFutureWait`，完成 `/YMES/WIP/FutureWaitRelease` Vue DEV Lot/OP/WaitTime 勾選查詢、六欄 Grid、單筆假 Release，與 WP505 共用 localStorage。原 Form 的 Lot 查找、Find 均隱藏；Service 傳完整三鍵，Core 通用方法若缺鍵可擴大 DELETE，正式 API 必須強制三鍵。FutureWait source/PG CREATE 沿用 WP201，無新 SQL；下一項 WP508 ERF Basis。未執行 build/test/browser。
- **2026-11-20 WP508 接續**：已核 `frmERFBasis`／`wsWIP.LoadERFBasisJoinLotBasis`／`clsLot.LoadERFBasisJoinLotBasis`，完成 `/YMES/WIP/ErfBasis` 六條件＋日期、batch member 查詢、25 欄 Grid 與 8D 待接提示。追 `WP_ERFInformation` 後更正「只讀」描述：附件 Grid 有 View/Delete，會保護異常單產生檔；DEV 已補五頁欄位與假附件操作。受保護 MESTEST 唯讀取得 8D Basis 43 欄及 LotDisposition 16 欄/PK/索引 DDL/metadata，交 PG CREATE-only；OCAP 附件源 DDL 沿 QC409，CREATE 補在 WP508info。正式五頁真資料/附件 API 與 WP801 8D 工作流待家用端；公司端接著追 WP801 source。未執行 build/test/browser。
- **2026-11-20 WP801 接續**：已追 `frmWP8DBasisFactory`／Def、Service/Core/流水號；`/YMES/WIP/8DBasisFactory` Vue DEV 有四狀態/QRSNo、40 欄、ADD/EDIT/DEL/Copy/Approve/ERFINFO，與 WP508 無 QRSNo 的假 ERF 建立關聯。主表 DDL 沿 WP508，Oracle 唯讀補兩張 SerialRule source DDL/metadata；原 first_phase PG CREATE 漏五個 Oracle default，新交準確 CREATE-only。公司端未連 PG、未 build/test/browser。家用端負責真序號/ERF 原子交易、Issue Job、API/遷移；公司端下一項 WP802 8D 維護來源。
- **2026-11-20 WP802 接續**：已追 `frmWP8DMaintain`、`frmWP_8DMaintainDefine`、子 Form 入口與 `wsWIP`/`clsWIP`；`/YMES/WIP/8DMaintain` Vue DEV 有 38 欄已簽核/未結案清單、Basis 至 D9 十頁及 D1–D7 明細/Verify/Track、D8 假總結、附件檔名與 WP508 有 QRSNo 直達。公司端用受保護 MESTEST 唯讀取得 12 張 8D 子表 source/index/metadata，另交 PG CREATE-only；TeamMember 是 unique index、無 PK constraint。專屬 Form/FishBone/附件 BLOB 詳情的 DEV 簡化見 `docs/modules/WIP/8D矯正單維護作業.md`。家用端接真 API/PG，下一項公司端 WP803 結案作業；未連 PG、未 build/test/browser。
- **2026-11-20 WP803 接續**：已追 `frmWP8DClose`／Def、`wsWIP`/`clsWIP`；`/YMES/WIP/8DClose` Vue DEV 有四狀態、五條件、九欄、結案編輯、D1 lookup、多選 Approve 及 8DQuery 唯讀。Release/ReleaseLog/TeamMember/Basis 沿 WP802/WP508；另唯讀取得部門表 14 欄 Oracle DDL/metadata，PG CREATE-only 補原 bootstrap 四個 default。WinForms Pending 提前結案、工作流 SQL 改錯表與 ActiveRow 錯列詳見 `docs/modules/WIP/8D矯正單結案作業.md`；家用端須用原子交易/正確 Issue 表接正式 API。公司端下一項 WP804；未連 PG、未 build/test/browser。
- **2026-11-20 WP804 接續**：已追 `frmWP8DQuery`、`wsWIP.Load8DBasisFactory`、`clsWIP.Load8DBasisFactory`；`/YMES/WIP/8DQuery` Vue DEV 十組條件、39 欄跨狀態查詢、ERFNo 外部參數與 WP802 唯讀十頁。來源只勾 WIPERF/Other 會漏 QRSClass 條件，已於 DEV 修正並在 `docs/modules/WIP/8D矯正單查詢.md` 記錄。主表 Oracle source/PG CREATE-only 沿 WP508，無新表；家用端接分頁真 API。未連 PG、未 build/test/browser。
- **2026-11-20 WP501 Release EQP 補核**：來源 Form → wsWIP → clsLot 確認 0 行警告無法阻擋提交，且把無設備與其他 Lot 共用混在一起。公司 DEV 已補 Vendor/Material、數字 EquipmentState 選單、共用／獨佔設備兩種假分支與原 v1 storage 增量升級。`TBLWIPTEMP_EQUIPMENT`、`TBLEMSEQUIPMENTSTATE`、`TBLEQPSTATEBASIS` 都有既有 reference，無新增直接表。詳 `docs/modules/WIP/立即異常設定.md` 和 home TODO；正式 API 必須在交易中驗證設備占用。接續 inventory 其他未完成 WIP 功能。未連 PG、未 build/test/browser。
- **WP203「過帳設備設定」（`EQP FAVORITE SETUP`）公司端 DEV 與 handoff 已完成。** 原始來源為 `STD/MESWin/WIP/WP_EquipmentFavorite/frmEquipmentFavorite.vb`、`frmEquipmentFavoriteDef.vb`、`STD/MESws_STD/wsSTD/wsEQP/wsEQP.cs`、`STD/MESkc/kcEQP/clsEQPBasis.cs`。重用 WP204 的 `TBLEQPFAVORITE` source DDL/CREATE-only；沒有新增 Oracle table。HostId 是 workstation-scoped，Production API 不得信任瀏覽器傳值。
- **WP103「列印流程卡（RUN CARD）」公司端 DEV 與 handoff 已完成**，規格為 `docs/modules/WIP/列印流程卡.md`，FunctionKey `WP103` 路徑 `/YMES/WIP/RunCard`。來源 Form/Service/Core 和 RunCardStyle 0/非 0、Lot 查詢、RunCard Log、Net Flow、Runcard template／Product RunCardMap 分支均已記錄。無新增專用表／DDL；DEV Preview 使用假 lot/流程，Print 不送出實體工作。Production API、PDF renderer、ProductMap/PD_Kit 移植與 PostgreSQL live schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP113「LOT SHEET PRINT」THEIL 專用頁已完成公司端 source trace、DEV 假資料頁與 API handoff**，規格 `docs/modules/WIP/LOT_SHEET_PRINT.md`，路徑 `/YMES/WIP/LotSheetPrint`。來源為 THEIL WinForms/Service/Core 與 ActiveReports；假資料包含 MO/建立日期篩選、Current/Original Process、Lot Sheet/Empty Version 預覽及不連印表機的 Print 模擬。重用既有 Lot Property 與 THEIL Matrix，無新增表／DDL。Production 查詢、報表 renderer 與 print count 原子更新交家用端依 `todolist_homepc.md` 執行。
- **WP104「使用者登入作業（上工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登入作業_上工.md`，路徑 `/YMES/WIP/OperatorClockIn`。已依 WinForms、wsUSR/wsOP/wsWIP_OperatorState、clsOperatorState 實作假員工、班別、OP mode、LoginPlace 與 auto-clockout 確認。Oracle 只讀新查 `TBLWIPOPERATORSTATE` 和 `TBLWIPOPERATORLOG`，DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp104_*`；重用 WP305 的 LotLoginState。Production credential verification、交易及 live PG schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP105「使用者登出作業（下工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登出作業_下工.md`，路徑 `/YMES/WIP/OperatorClockOut`。依 WinForms/Service/Core 模擬員工驗證、現況篩選、預設全選與逐列下工；沿用 WP104 OperatorState／OperatorLog 和 WP305 LotLoginState。沒有新增 Oracle 表或 DDL；Production API 與 PostgreSQL live schema 核對交家用端。
- **Lot Operator Clockin 批次上工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員上工.md`，路徑 `/YMES/WIP/LotOperatorClockIn`。頁面依 LotStatus/ClockStatus/PS/OP 查詢候選 Lot，預設全選，按 mode/location 去重後一次模擬批次上工，並與 WP104/WP105 共用假 OperatorState。沿用 WP104/WP305 Oracle references，無新 DDL。WinForms 來源只暴露 `PrivFunNo`，FunctionKey 未核實；fallback FunctionNo 是標題推導值，上游 menu metadata 待確認。
- **Lot Operator ClockOut 批次下工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員下工.md`，路徑 `/YMES/WIP/LotOperatorClockOut`。頁面依預設 ClockStatus=Clocked In 查詢、可選員工篩選、同步勾選同 UserNo+LoginPlace rows，按來源批次去重模擬下工；共用 WP104/WP105 假狀態／歷程，沿用 WP104/WP305 refs，無新 DDL。FunctionKey 未核實，正式路由待 menu metadata 確認。
- **作業員 OP／Area 登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/作業員登入登出_OPArea.md`，路徑 `/YMES/WIP/OperatorLogin`。來源使用 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`，但公司端 MESTEST 所有可見 Owner/Synonym 查無這兩表；未製作推測 CREATE，家用端應先核 live PG schema，並取得正確 Oracle DDL 再決定 migration。
- **OPERATOR LOGIN_LOT 生產批人員登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/生產批作業員登入登出.md`，路徑 `/YMES/WIP/OperatorLoginLot`。DEV 呈現 CallType=MenuItem，規格涵蓋 BR 呼叫端 1/2/3；共用 WP104 OperatorState/Log 與 WP305 LotLoginState，沒有新表／DDL。正式 FunctionKey 待 menu metadata 核實。
- WP301 設備上料已核對 `frmEQPMaterialLoading`／`wsWIP_Material`／`clsMaterialHandler`，完成物料、元件、半成品模式的 DEV 假資料頁。四張相依表 DDL／CREATE-only 在 `database/reference/mestest_wip_wp301_*`；其餘 13 張沿用既有 reference。未連 YMES PostgreSQL、未實作 Production API／migration。
- WP302 載具現況管理已核對 `frmCarrierState`／`wsEQP`／`wsWIP`／`wsINV`／`clsCarrierBasis`，完成 Vue DEV 放入、取出、清空及隱藏狀態操作。三張新查證表 DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp302_*`；Lot／Component／CarrierType 等沿用既有 references。未連 YMES PostgreSQL、未實作 Production API／migration。

- PD102 DEV 子視窗已分段完成 Property、Parameter、Business Rule、OPDescription、RecipeGroup、Accessory、Resource、SubOP、Program、Default、RunTime、QC Item。各功能的 WinForms 證據、DDL 及 API handoff 在 `docs/modules/PRD/` 與兩份 TODO。
- RunTime 已完成單列／表格編輯與套用。QC Item 已完成依 OP 指派與檢驗類型，QC Import／QC Chart 的 Excel 流程待接續。兩項 DDL 依 MESTEST 字典實查重建，Production API 由家用端接續。
- Packing Set、EQPCapItem、eSOP、EquipmentSpec、APS Integration 和 Bin 已完成 DEV 設定與 Oracle 交接。`GBOMImport`／`GBOMExport` 已完成 DEV 備份操作與客製表交接，工作簿解析和資料匯入待正式接入。

## Default 已核實事項

- WinForms 入口 `btnDefault_Click` → `frmProductLoadDefault` → `wsPRD.UpdPRDOP_BatchDefault` → `clsProduct.UpdPRDOP_BatchDefault`。
- `Product Process OP` 對應 `DefaultOP=2` 且預設選取；`All PS OP` 對應 `DefaultOP=1`。父畫面沒有產品流程時不開表單。
- 複製旗標：Parameter、BusinessRule、Description、OPResource、QCItem、SubOP、APS。產品 IssueState 非 0 時 Checkbox/Apply disabled，Radio 仍可選。APS 受 `ExecuteAPS` 顯示控制但 Designer 預設 checked；QCItem 初始狀態由 `PRDDefault_QCItemAutoChecked` 控制。
- Oracle APS product table 字典是由 Windows PowerShell 5.1 + ODP.NET 直連唯讀取得。`DBMS_METADATA.GET_DDL` 回 ORA-03135；ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_CONS_COLUMNS/ALL_INDEXES 可查。欄位/PK 以 `database/reference/mestest_prd_default_aps_source_ddl.sql` 為準。

## Oracle 連線與安全

- 查 DDL 固定使用 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，由本機受保護 profile 取得連線字串後直接讀 Oracle metadata；不用 DBeaver GUI 抄 DDL。
- 公司筆電本次實際讀取的受保護設定檔是 `C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt`（JSON），Oracle 測試連線欄位為 `DatabaseConfigs.CS.Test.Mes.ConnectionString`，類型欄位為 `DatabaseConfigs.CS.Test.Mes.DbType`。這是筆電上的 Codex attachment 路徑，不保證桌電存在；桌電先檢查此檔是否可用，不能因路徑不同就從文件猜密碼。
- 該 JSON 還包含其他系統設定與機密。不可將檔案複製進 repo、提交 Git 或輸出內容；只在程式中讀取所需 Oracle key。若桌電不存在此受保護檔，請使用者安全地重新設定 Oracle profile。
- 公司桌機已使用目前 Windows 帳號的 DPAPI profile 驗證 MESTEST 登入及 RunTime 資料字典查詢。設定檔位於 Git repo 外，由桌機帳號本機解密。
- IP、帳號、密碼、SID 不得寫入 GitHub Markdown、SQL、source code 或 console output。桌電若沒有受保護 profile，透過安全方式請使用者重新設定；不可猜連線資料或從舊電腦文件搜尋明碼。
- 只讀 metadata，不查 MES 業務資料列、不執行 Oracle DDL/DML。PostgreSQL CREATE-only reference 不是 migration；家用端先查 YMES live schema 和 migration history。

## 使用者目標與工作規則

- 忠實復刻完整 MES 的三層 menu、WinForms 功能頁、欄位、按鈕及操作邏輯。困難處可先簡化，但要明確記錄限制，供使用者決策。
- 公司端：DEV 假資料下完成 UI/互動、模組規格、Oracle source DDL 與家用端 API/schema/Create SQL handoff。家用端：核對 PostgreSQL live schema、建立缺少的 migration/API。使用者已授權此 YMES 專案完成後分階段 commit/push GitHub。
- WinForms Form、Designer、Service/Core 是主要證據；圖片只用於版面核對。找不到原始碼/DDL時，告知具體缺少的路徑或表名，不要假裝已確認。
- 不寫入或輸出資料庫秘密。未收到使用者要求時，不執行 build/test/browser；可做靜態檢閱和 `git diff --check`。
- PD101 至 PD117、PD202、PD204、PD206、PD901、`PRD OVERSTAY` 與 `PRD OP RULE` 已完成本輪 DEV 互動與交接。各模組文件記錄正式簽核、匯入、資料權限和引擎相依。`PD_BATCHMODIFYAREA` 的客製原始碼待補。
- CP 行業設定 PDP01 至 PDP04、挑片規則 PDB01、封測產品 PDB02、產品文件 PDB04 與 PDB05 已完成本輪 DEV 與交接。封測行業設定 PDI01／PDI03 的獨立 Form 與 Designer 待補。

## 2026-11-20 WIP WP308 接續檢查點

- WP308「載具管理_元件」已依 WinForms frmCarrierManagement／frmCarrierIn／frmCarrierChange、Service 與 Core 完成 Vue DEV 主頁與兩個子視窗；與 WP302 共用 DEV 假資料。規格為 docs/modules/WIP/載具管理_元件.md；WP303 設備耗品現況已接續完成，詳見下方檢查點。
- WP308 沒有新表；沿用 WP302、WP101、Carrier Basis/Type 的 Oracle reference。家用端需比對 PostgreSQL live schema，再實作 Carrier In/Out/Change 的登入保護 POST command、lookup 與原子交易。
- 這輪未連 YMES PostgreSQL、未部署 API、未執行 build/test/browser；詳見 todolist_officepc.md 與 todolist_homepc.md。

## 2026-11-20 WIP WP303 接續檢查點

- WP303「設備耗品現況」已依 `frmEQPConsumeStatus`、`frmEQPConsumeStatusDef`、`WP_EQPConsumeReset/frmEQPConsumeReset`、wsWIP/wsEQP 與 clsWIP/clsEQPBasis 完成 DEV 主頁及維護、設備 lookup、ConsumReset 視窗。規格：`docs/modules/WIP/設備耗品現況.md`；頁面 `/YMES/WIP/EquipmentConsumeStatus`。
- 兩張新表 `TBLWIPEQPCONSUMESTATUS`、`TBLWIPEQPCONSUMELOG` 的 Oracle source DDL／metadata／PostgreSQL CREATE-only 已建立於 `database/reference/mestest_wip_wp303_*`。設備耗品基準沿用既有設備類別 reference，設備主檔沿用既有 reference。
- WP304「設備耗品歷程」已接續完成，詳見下方檢查點。公司端仍只做 Vue DEV 假資料、WinForms／Service／Core 核對及 DDL metadata handoff；不做 YMES API／migration、不連 PostgreSQL、不執行 build/test/browser。

## 2026-11-20 WIP WP304 接續檢查點

- WP304 依 `frmEQPConsumeStatusLog`、`wsWIP.LoadEQPConsumeLog`、`wsEQP` lookup 與 `clsWIP.LoadEQPConsumeLog` 完成唯讀查詢頁；設備／耗品核取篩選、兩個 lookup、From/End Date、Query、七欄 Grid 與 Close 均已實作。頁面 `/YMES/WIP/EquipmentConsumeStatusLog`。
- 本功能只讀 WP303 已核對的 `TBLWIPEQPCONSUMELOG`，沒有新 Oracle DDL／CREATE-only；DEV 與 WP303 共用假 log storage。
- WP305「設備線上點檢」已接續完成，接手摘要見下節。公司端不連 YMES PostgreSQL、不實作 API/migration、不部署、不執行 build/test/browser。

## 2026-11-20 WIP WP305 接續檢查點

- WP305 依 `frmEQPCheckQCList`、`wsEQP/wsWIP`、`clsWIP`、`clsLot` 完成 Vue DEV `/YMES/WIP/EquipmentOnlineQC`。規格與 DEV 差異見 `docs/modules/WIP/設備線上點檢.md`。
- 直接唯讀 Oracle MESTEST 字典取得 6 表 DDL、indexes、欄位/constraint/trigger metadata：點檢 Log/Detail/Task、設備狀態 Log、設備上機歷程與 Lot 人員登入狀態。Source DDL/Index DDL/metadata/CREATE-only 在 `database/reference/mestest_wip_wp305_*`。`TBLWIPCONT_EQUIPMENT` 原始無 PK；不可自行補 key。
- Vue DEV 使用 localStorage `ymes.dev.wp305-equipment-online-qc.v1`，測項涵蓋 QCType 0–3、多筆 D01-D10、Wait QC、Re QC、未完成確認及結果存檔。NG + UnValidAction 的設備狀態/生產下機副作用尚未在 DEV 寫假 DB 狀態，已明列於規格。
- 正式 API 需保留 Header/Detail/Task 原子交易、server-side QC 結果計算、check-in 不允許部分存檔，以及失敗設備狀態變更時共用 WP201/WP204 下機 command；家用端先比對 live schema/migrations，只補缺少物件。
- 下一功能為 WP306「設備點檢歷程」。繼續讀該 Form/Designer/Service/Core，沿用 WP305 Log/Detail references；若查到未收錄表才以公司 PowerShell 5.1 + Oracle profile 唯讀查 DDL。公司端不連 YMES PostgreSQL、不改 API/migration、不部署/執行 build/test/browser。

- **2026-11-20 WP510 接續**：已依 `STD/MESWin/WIP/WP_WaitBasis` 與 `WP_WaitInformation`、wsWIP/clsLot 完成 `/YMES/WIP/WaitBasis` Vue DEV 六組勾選查詢、15 欄 Grid、七欄唯讀 Wait Info，並合併 WP502 已建立的本機 Wait。Oracle MESTEST 只讀查得 `TBLWIPWAITLOTDISPOSITION` 13 欄、無 PK/trigger、WAITNO 非唯一 index 的 DDL/metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp510_disposition_*`；主表沿 WP201、NodeBasis 沿 PRS104。家用端接授權分頁 API；公司端下一項 WP509 或 WP511。未連 PG、未執行 build/test/browser。
- **2026-11-20 WP511 接續**：已核 THEIL BU1 等待處置主 Form、七個 Split/Merge 子窗入口、`wsWIP.LoadWaitBasisJoinLotState`、`THEIL.wsWIP_Wait.Exe_Wait_LotRelease`、`tcWIP.clsLot_Wait` 及群組權限 `LoadDispPriv`。Vue DEV `/YMES/WIP/WaitDisposition` 有六欄查詢、17 欄 Grid、10 個可見選項、批次/Partial/路由守門與簡化分併批對話框；WP510 會顯示其假解除結果。Oracle MESTEST 唯讀查得新表 `XX_TBLWAITPRIV` 14 欄／GROUPNO PK source DDL、metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp511_priv_*`；其餘 Wait/USR/PRS 表沿現有 references。規格 `docs/modules/WIP/等待處置.md` 明列 DEV 簡化及真交易差異；家用端核 PG live schema、權限、分併批七種交易並做 API。公司端下一項 WP509；未連 PG、未執行 build/test/browser。
- **2026-11-20 WP509 接續**：已核 THEIL BU1 未解除異常單主/處置 Form、七個分併批子窗入口及 `wsWIP`/THEIL `wsWIP_ERF`/Core。Vue DEV `/YMES/WIP/ErfUnreleased` 提供六條件、23 欄、ERF Disp/Abort/Create/Info、狀態 0–3/11 分頁、14 種 Lot 處置及簡化 OCAP/分併批；詳 `docs/modules/WIP/未解除異常單.md`。公司端以 PowerShell 5.1 + 受保護 MESTEST profile 唯讀查得新權限表 `XX_TBLQCHOLDPRIV` 18 欄／GROUPNO PK 原 DDL/index/metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp509_priv_*`；無業務資料讀寫，未連 PG、未執行 build/test/browser。家用端做正式 API、群組/OCAP/ERF/Lot 原子交易及七子窗；公司端接續下一未完成 WIP 功能。
- **2026-11-20 WP106 接續**：已核 `frmOperatorOnline`/Designer、`wsWIP_OperatorState`/`clsOperatorState`；Vue DEV `/YMES/WIP/OperatorOnline` 共用 WP104/105 假 State/Log，支援 OP/Area/Equipment 多選並集、七欄 Grid、逐列 SignOut/確認及外部唯讀入口。規格 `docs/modules/WIP/使用者登入資訊.md`、家用端 API 待辦已補；Oracle State/Log DDL/PG CREATE-only 沿 WP104，LotLoginState 沿 WP305，無新增表。公司端未連 YMES PostgreSQL、未 build/test/browser；下一項 WP109 SWR BASIS。
- **2026-11-20 WP109 接續**：已核 `frmWIPSWRBasis`／Def、`wsWIP`／`clsWIP`，Vue DEV `/YMES/WIP/SwrBasis` 覆蓋七勾選查詢、四狀態、14 欄 JOIN Grid、右側操作鍵、ADD/EDIT/Copy/Approve/Void/附件檔名示意。公司 PowerShell 5.1 + 受保護 MESTEST profile 唯讀取得 `TBLWIPSWRBASIS` 15 欄、`TBLWIPSWRDETAIL` 2 欄、共用 `TBLSYSSERIALNO` 2 欄 source DDL/index/metadata；PG CREATE-only 在 `database/reference/mestest_wip_wp109_create.sql`。無 FK/trigger；正式 API 要修正原 PENDING 作廢假成功、JOIN 重複核准、附件末 byte、SQL 拼接。家用端核 live PG、實作交易/附件/稽核並驗收；公司端未查業務列、未寫 Oracle、未連 PG、未 build/test/browser。詳 `docs/modules/WIP/特殊生產需求維護.md`。下一項依 WIP 清冊找未完成功能。
- **2026-11-20 WP112 接續**：已核 THEIL 撤派工 Form/Service/Core/SAP 拋轉來源；Vue DEV `/YMES/WIP/UndoDispatch` 兩模式、MO 查找、13 欄勾選 Grid、全選與逐批假 Confirm。公司端用受保護 MESTEST profile + PowerShell 5.1 **唯讀**取得六表 `TBLWIPCONT_MATERIALLOT`、`TBLSYSSERIALCREATEMAX`、`XX_TBLOEMO_OS_BU1`、`ZPPT0025A/B/C1` 原始 DDL/index/metadata 與 PG CREATE-only `database/reference/mestest_wip_wp112_create.sql`；外部 `THSAP.*@SAP_DBLINK` 真契約未核。家用端必須接 SAP/Log/Inventory provider，未具備時正式 POST 不可假成功；規格在 `docs/modules/WIP/工單撤派工.md`。公司端未查業務列、未寫 Oracle、未連 PG、未 build/test/browser；後續依 WIP 清冊選下一項。

## IV501 checkpoint

- 規格：[IV501 成品庫庫房資料](modules/INV/成品庫庫房資料_IV501.md)。DEV route `/YMES/INV/FgdInventory`；庫房查詢顯示 `LocQty>0`，選列先查 Component，無 Component 才依 `TBLSYSBIN.BinOrder` 顯示 Lot Bin；FGD In 使用 Source=30。
- 已直接核對 `frmINVFGDInventory`／`frmINVFGDInConfirm`、相關 Service 與 `clsInventory.ExeFGDIn`。重要風險：`AddINVTransactionState` 會吞內層 `ExeINVTransactionState` 例外，外層仍可能回 success，雖然 Basis 已標 State=3；正式 API 不可沿用。
- DDL／metadata 沿用 WP201／WP605、QC409 WIP Bin、PRD Bin refs；沒有新表或 CREATE SQL。桌機未重連 Oracle、未連 PostgreSQL、未 build/test/browser。
- IV502 已完成；下一項 IV503「成品入庫單記錄」。

## IV310 checkpoint

- 規格：[IV310 包裝拆並箱作業](modules/INV/包裝拆並箱作業.md)。DEV route 為 /YMES/INV/PackingCombineDismantle，拆箱 mode 0／1／2 與並箱掃描／容量檢核／假歷程已交付。
- WinForms Form／Designer、wsWIP／wsINV Service、clsLot／clsInventory Core 均有直接核對；Combine 寫入前 Core stale-state 驗證已註解，Service 先拆目標再併入為兩段 Core 寫入，Lot transfer 是非同步呼叫且吞錯。正式 API 需要重新核對交易與 CloseFlag，不可依賴 browser payload。
- 包裝 DDL／metadata 與 PG CREATE-only 沿用 WP601 的 19 張共用表 references，沒有 IV310 專屬新表；家用端核對 live schema 後實作原子拆並箱 API、冪等 operation ID 與授權 label job。公司端未連 PG、未 build/test/browser。

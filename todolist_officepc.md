# 公司電腦續作清單

> 目的：記錄家用電腦完成資料庫/API交接後，公司電腦接著要做的 WinForms UI、互動邏輯與規格文件。公司電腦先讀本檔；家用電腦先讀 `todolist_homepc.md`。

> **公司電腦工作邊界：** MES 原生 Oracle MESTEST 僅供本端唯讀查詢 DDL、資料字典與舊系統規則；不得在原生 Oracle 建 YMES 表或寫入業務資料。本端只開發 UI、按鈕互動與呼叫 `/YMES/API`；不連 YMES PostgreSQL、不使用家用端 PPK/SSH、不執行 migration、不遠端部署。資料庫或 API 需求寫入規格並交給家用電腦。

## 交接基準

- 專案：`D:\YMes`，GitHub `souaix/YMES`；來源 WinForms：`D:\iMES\CSiMES`。
- 公司電腦以 MESTEST 假資料完成 Vue UI 與前端互動；家用電腦負責 YMES PostgreSQL、migration、API 與部署。
- 開工前先 pull 最新 `main`，確認家用電腦已 push API/schema 變更，再對照本檔與 `todolist_homepc.md`。不要覆蓋另一台電腦尚未同步的修改。
- Oracle 來源 DDL 用 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢；使用本機受保護連線設定，不必請使用者重貼，也不得把連線值寫進文件、SQL 或 Git。
- 目前只查詢過 Oracle 結構與系統選單中繼資料，沒有匯出 MES 業務資料。不要把 DEV 假資料當成正式資料。

## 目前停留點（2026-11-17）

### 2026-11-17 MTL ML101 物料供應商基本資料

- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀查得 `TBLMTLMATERIALVENDOR`、`TBLMTLMATERIALVENDORCONTACTOR` 完整欄位、NULL/default、主鍵、索引及 FK；沒有讀取 MES 業務資料或在 Oracle 執行 DDL。
- [x] 對照 `frmMaterialVendor`、主檔/連絡人 Def、`wsMTL`、`clsMTLBasis`，完成四狀態查詢、雙 Grid、CRUD/複製/多筆刪除/核准、連絡人 Mail、Issue/Job、audit 與 DataStamp 規格；新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL 及獨立規格文件。
- [x] 新增 `web/src/components/MaterialVendorMaster.vue`，依 WinForms 雙 Grid 與 KD 黃色欄位完成 DEV localStorage 互動；新增 MTL fallback 三層選單與 `MTL VENDOR` 路由 `/YMES/MTL/MaterialVendors`。
- [x] 已將 API、schema、migration handoff 寫入 `todolist_homepc.md`；提醒家用端修正 material vendor map 名稱查詢誤 join EQP vendor 的問題。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML102 物料類別資料

- [x] 對照 `frmMaterialType`、`frmMaterialTypeDef`、`frmMaterialGumType`、`frmMaterialProperty`、`wsMTL`、`clsMTLBasis`；整理畫面、狀態、CRUD、Copy GumHour、Property/MTL PROPERTY、固晶膠核准檢查與 GumHour 數值驗證。
- [x] 確認 Oracle DDL/PG CREATE SQL 已存在於 `mestest_mtl_master_*`，包含 MaterialType、Property、TypeQCItem、TypeAcclimation；此功能沒有新增 table/CREATE SQL。
- [x] 新增 `web/src/components/MaterialTypeMaster.vue`，建立 `/YMES/MTL/MaterialTypes`、`MTL TYPE` 動態/fallback 路由；包含狀態查詢、主 Grid、Add/Edit/Copy/Delete/Approve、Property 與 GumHour 視窗，Copy 會一併複製類別時效，核准後 GumHour 唯讀，DEV 假參數不代表正式系統參數值。
- [x] 更新 ML102 規格、索引及家用端 schema/API/Issue handoff。
- [x] 重新確認 origin/main 與公司端基線相同；已把 Copy 的單交易 API 契約與 GumHour POST/PUT 區分補進家用端待辦。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML105 物料類別品管項目設定

- [x] 對照 `frmMaterialTypeQCItem`、`wsMTL`、`wsQC`、`clsMTLBasis`；確認前綴查詢、三種 QC 候選清單、雙向多選、類別範本儲存及同步至所有關聯物料/IQC 項目。
- [x] 確認 `TBLMTLTYPEQCITEM`、`TBLMTLQCITEM` 的 Oracle DDL/PG CREATE 已在 MTL reference 和 0013 migration；無新增 CREATE SQL。
- [x] 新增 ML105 Vue DEV 頁與三層 MTL 路由，並建立 Production API/transaction handoff 規格。
- [ ] 未執行 build、test 或瀏覽器驗收；等待家用端完成 API 後再接 Production endpoint。

- Oracle MESTEST 直接查詢已可用，資料字典可見 1,287 張表。查詢方式及憑證規則見 `docs/DATABASE_CONNECTION.md`。
- 2026-11-16 已用 PowerShell 5.1 + Oracle.ManagedDataAccess 查得 `TBLUSRUSERGROUP` 原始 DDL、6 欄 NULL/default、PK 與索引資訊；來源 DDL、空表 PostgreSQL CREATE SQL、設備工程群組規格及家用端待辦均已更新。
- 2026-11-16 已查得 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 原始 DDL、欄位/PK/索引/FK；SMD 空表 CREATE SQL 與設備群組規格已補齊。
- 設備群組 Vue 已依 WinForms 對齊：編號失焦轉大寫；查詢/狀態切換時清除不可見列選取，DEV 核准的 Issue flow 簡化已記錄。
- 2026-11-16 已從 WinForms/Service 確認配件類別選項表為 TBLEQPACCESSORYTYPE，Oracle 9 欄 DDL/PK 索引已直連核實；來源與空表 PostgreSQL CREATE SQL 已交付家用端。
- WinForms 正式選單目錄已查得 20 個 Module、601 筆 MenuItem、553 筆 Function；繁中第一層名稱取自 `D:\iMES\CSiMES\Customer\Production\MESClient\mesresource.zh-cht.resources`。完整 ModuleOrder、分類、FunctionNo、FormName、ExecutionFile 見 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司電腦開工前已 pull GitHub `origin/main` 最新版本；家用端已部署 20 個模組的三層選單、物料子功能 API、設備供應商 API 與設備狀態 API。每次接續仍先 pull，並對照新 endpoint/options 的 Vue 呼叫契約與來源 WinForms。
- 作業站主表、群組明細及 Service 明確使用的 16 張子表，連同 3 張系統目錄表，共 21 張來源 DDL/空表 CREATE SQL 已交付；物料 8 張來源 DDL/空表 CREATE SQL 也已交付。檔案索引見 `docs/DATABASE_CONNECTION.md`、`docs/modules/OP/作業站主檔.md`、`docs/modules/MTL/物料主檔.md`。
- `TBLOPBASIS` 等 21 張作業站/系統表已確認每表一個 PK unique index，沒有額外獨立索引；本次查的 29 張 OP/系統/MTL 表沒有 Oracle FK constraint。PK/NOT NULL 保留在 CREATE SQL。
- `tblmtlmaterialbasis` 的 YMES bootstrap 已補齊 7 個來源 DEFAULT；多出的 7 個 YMES 欄位仍保留，等家用電腦比對實際 schema，不要自行刪除。
- 物料主檔及其子功能已有 DEV localStorage 互動雛形；物料匯入已補 CSV/`.xlsx` 預覽、9 欄解析、物料編號/名稱分組、結果列、開發範本下載及逐組假資料進度/取消；正式範本下載、`.xls` 預覽與群組 API 規則待家用端。詳細規格見 `docs/modules/MTL/物料資料匯入.md`。作業站子功能也有簡化 DEV 視窗。設備設定已有七個功能頁雛形。設備狀態的兩張刪除引用表 DDL、空表 PostgreSQL CREATE SQL、正式 ValueList 和繁中翻譯已於 2026-11-16 交付家用端；家用端仍需 migration 和 API 刪除檢查部署。其他頁面仍待逐項 WinForms 對照。
- 本次未執行 build 或測試；未完成整套 MES 遷移，也未把尚未完成功能標為正式上線。

## 2026-11-16 SMD SD104 區域基本資料公司端交接

- [x] Pull `origin/main` 最新版本，確認 `82de6f4` 為當前基準。
- [x] 從 `frmAreaBasis`、`frmAreaBasisDef`、`frmAreaLayout`、`frmNodeDef`、`wsSMD`、`clsSMDBasis` 邏輯完成 SD104 主 Grid/表單/狀態/核准/刪除規則整理；AddFlow 版面在 Vue 先做可操作簡化，差異已列規格。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線直查 `TBLSMDAREABASIS`、`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP` DDL、欄位、PK、索引與 FK；建立 source DDL 和 PG 空表 CREATE SQL。關聯配置兩表使用設備群組來源 DDL/CREATE SQL。
- [x] 建立 SMD fallback 三層目錄（8 個 WinForms Function）；SD104 接上 DEV Vue 頁，其他 7 個仍由 FunctionPending 說明。
- [x] 新增 `docs/modules/SMD/區域基本資料.md`，並更新規格索引、資料庫連線紀錄及家用端待辦。
- [x] 修正 SD104 DEV 預設列，使未簽核子區可選已簽核 Base Area；套用新增後轉入 Edit 狀態；核准頁採用 API 回傳實際 IssueState；配置保存不限制 IssueState，符合 WinForms 對應程式。
- [ ] 本段尚未執行 build/test/瀏覽器驗收；家用端 API/schema 完成後需 pull、逐欄對接並檢查正式行為。

## 2026-11-16 SMD SD105 區域庫房基本設定公司端交接

- [x] 對照 `frmAreaInventory`、`frmAreaInventoryDef`、wsSMD 與 clsSMDBasis；記錄 Base Area 選擇、兩種 AreaRelation picker、Default 唯一規則、CRUD/Apply/Copy/Delete 與來源參數不一致處。
- [x] Oracle `TBLSMDAREAINVENTORY` DDL/欄位/PK/index 已由 SD104 PowerShell 5.1 + ODP.NET 直連查得，且已附 `mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`；本功能沿用既有表，沒有重複查詢或另造 SQL。
- [x] 新增 Vue DEV 頁 `/YMES/SMD/AreaInventories`、Area/Inventory 查找彈窗、Default/重複驗證、Apply、稽核、localStorage 假資料及 `AREA INV MAP` 路由。
- [x] 新增 `docs/modules/SMD/區域庫房基本設定.md`，更新 SMD 功能索引、資料庫查詢紀錄與家用端 API/schema handoff。
- [ ] 尚未執行 build、test 或瀏覽器驗收；家用 API 完成後再 pull 對接。
- [x] SD106「過帳電腦之區域」已完成 WinForms Form/Service/Core 對照、Oracle DDL 直查、DEV Vue 頁及 API/CREATE SQL handoff；進度見下方 SD106 區塊。
- [x] SD101「工廠基本資料」已完成，WinForms/Service/Core 對照、Vue DEV 頁與家用端 API/schema handoff 見下方區塊。Oracle DDL 沿用 2026-11-16 EP303 已直連查詢的結果，沒有重查。

## 家用電腦完成後，公司端先做

- [x] 2026-11-16 Pull 家用電腦最新提交 `944b793`，閱讀更新後的 DTO/endpoint/migration 與 `todolist_homepc.md`；設備狀態 Production endpoint 使用 `GET/options/POST/PUT/DELETE`，Vue 呼叫路徑已對上。
- [ ] 用公司端 Development 假資料頁逐頁核對 API 欄位名稱、狀態值、DataStamp、錯誤訊息與選項，不讓 Production 回退 localStorage。
- [ ] 以目前已查到的 20 個模組與資源檔標題整合三層側欄：Module → Menu Group → Function；維持 ModuleOrder、MenuOrder、FunctionOrder。未遷移功能要明確顯示尚未遷移，不能導向空白頁。
- [ ] 按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 逐筆核對 FunctionNo、FormName、ExecutionFile 與 `D:\iMES\CSiMES\STD\MESWin` Form/Designer/Service；一個 Function 建一份或更新一份獨立規格，不以截圖代替程式碼/DDL。
- [ ] 先完成核心五模組既有頁面與子功能的 WinForms 對照：EQP 設備設定七項、OP 作業站子功能、MTL 屬性/QC Item/供應商/GumHour/匯入、PRS/PRD 擴充頁面。每個頁面復刻查詢欄位、資料欄位、按鈕、表單、狀態規則與互動。
- [x] MTL 匯入 DEV 畫面改依 WinForms 呈現：`.xlsx`/CSV 預覽、9 欄欄序、按物料編號/名稱分組、逐組結果、開發範本、進度與取消。規格見 `docs/modules/MTL/物料資料匯入.md`。
- [ ] MTL 匯入仍需家用端提供正式範本下載、`.xls` 預覽、ErpUnitNo 及與 WinForms 一致的群組寫入/結果契約；之後再做 Production UI 對接。
- [x] 設備點檢表設定 FunctionNo/FunctionKey 已核實為 `EQP QCLIST`/`EP107`；透過 PowerShell 5.1 唯讀查 MESTEST ValueList 並核對繁中資源，QCType 合法值為 0「開始生產時點檢」、2「手動點檢」。已修正 DEV 選項及新增預設值；Production options API 交由家用端依規格提供。
- [x] 設備工程群組、設備類別、設備群組 FunctionNo/FunctionKey 已由目錄 inventory 核實：`EQP ENGINEER GROUP`/`EP102`、`EQP TYPE`/`EP103`、`EQP GROUP BASIS`/`EP105`。
- [x] 設備類別匯入 DEV UI 改依 WinForms：第一工作表固定 8 欄、略過標題列、依 EquipmentType/EquipmentClass 顯示結果、產生 .xlsx 開發範本、逐類別進度與取消。假資料不模擬每類別多列或舊 Checker，明確提示且不寫入；.xls/正式 Checker 交家用 API。規格與 API handoff 已更新設備類別文件。
- [ ] 按 ModuleOrder 繼續其餘 15 個模組、476 個 Function。每頁先查 WinForms Form/Designer/Service；需新表時由公司端 PowerShell 查原始 DDL，再提供家用端 CREATE SQL/API 規格。不要猜欄位或流程。
- [ ] DEV 互動使用清楚標示的本機假資料；正式 API 未提供時停用或顯示未接通，不能回報假成功。
- [ ] 每完成一個功能，更新對應 `docs/modules/<ModuleNo>/...md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md`、`todolist_homepc.md`/本檔與 Function 遷移狀態。
- [ ] 如遇 WinForms 隱藏的複雜功能，先做可操作的簡化版並記錄未復刻規則，繼續下一頁；把需要使用者決策的精確差異集中列出。
- [ ] 全部功能完成後核對三層選單、FunctionNo 路由、UI/API 契約、規格文件與資料庫交接；完成後 push GitHub。

## Oracle 來源交付給家用電腦



- [x] **設備工程群組成員表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `MESTEST.TBLUSRUSERGROUP` DDL、欄位 NULL/default、PK、索引/FK 字典。來源 Oracle DDL、PostgreSQL 空表 CREATE SQL 與規格已更新；家用端需核對 YMES schema，缺表才新增 migration 並實作 GroupType=2 成員 API。
- [x] **設備群組 SMD 映射表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` DDL 與欄位/PK/索引/FK。家用端比對 YMES schema，實作新增/移除設備明細時的舊 SMD 同步語意。

- [x] **設備狀態設定來源資料已交付（2026-11-16）**：以 PowerShell 5.1 唯讀查 MESTEST 的 `TBLEQPSTATECONTROL` / `TBLEMSEQUIPMENTSTATE` 物件、欄位、PK/唯一索引、NOT NULL/default 與 DDL；查 `TBLSYSVALUELIST` 的兩組選項，並核對 `mesresource.zh-cht.resources` 繁中翻譯。來源 DDL、PG 空表 CREATE SQL 與規格已更新；家用端仍待比對引用表 schema、migration/SELECT 權限及 DELETE 完整驗收，CRUD/options API 已部署。

- [x] 設備類別配件目錄已交付（2026-11-16）：已核實 MESTEST.TBLEQPACCESSORYTYPE 原始 Oracle DDL、9 欄 nullable/default、PK 唯一索引與無 FK，並建立 PostgreSQL 空表 CREATE SQL。家用端仍需比對 YMES 目標表與完成 API options。

## 2026-11-16 SMD SD106 過帳電腦之區域公司端進度

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，確認沒有新提交。
- [x] 對照 `frmComputerArea`、`frmComputerAreaDef`、wsSMD 與 clsSMDBasis，記錄主 Grid/定義 Form、查詢欄位、按鈕、工廠與區域選擇、複合鍵、Copy 與批次 Delete 行為；指出 Copy 舊 Core 只按 ComputerNo 找來源，以及 ws/Core DataStamp 簽章不一致。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護連線直查 `TBLSMDCOMPUTERAREABASIS`、`TBLSMDCOMPUTERAREADETAIL` DDL、欄位 NULL/default、PK/index/FK；只查結構，未讀業務資料，未在 Oracle 執行 DDL，憑證未輸出。
- [x] 新增 Oracle logical source DDL 與 PostgreSQL 空表 CREATE SQL；校正 first-phase reference 中 ComputerArea DataStamp DEFAULT 及 SD104 AreaBasis DataStamp DEFAULT，移除 ComputerArea 欄位推測註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ComputerAreaMaster.vue`、`/YMES/SMD/ComputerAreas` 路徑及 FunctionNo=`COMPUTER AREA`；實作查詢、多選、Add/Edit/Copy/Delete、approved Factory/Area 選擇、稽核與 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/過帳電腦之區域.md`，更新 SMD 規格索引、Oracle 連線查詢紀錄與家用端 schema/API handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/migration 待家用端完成。
- [x] SD101「工廠基本資料」已接上 Vue DEV 路由；既有 Oracle DDL/PG CREATE SQL 已確認可沿用，完成規格及 API/schema handoff。
- [x] SD103「儲位基本資料」公司端 UI、WinForms 規格與 Oracle/PG CREATE SQL 已完成；詳本文下方 2026-11-16 SD103 進度。

## 2026-11-16 SMD SD101 工廠基本資料公司端進度

- [x] 對照 `frmFactoryBasis`、`frmFactoryBasisDef`、wsSMD 與 `clsSMDBasis`/`clsBasis_UPD`；記錄四種 IssueState、FactoryNo 搜尋、IC_FT_PKG FactoryName 顯示、欄位/按鈕、Copy、批次 Delete/Approve、DataStamp 與 Issue/Job 規則。
- [x] Oracle `TBLSMDFACTORYBASIS` 原始 DDL、欄位 NULL/default、PK/index/FK 已於 2026-11-16 EP303 查詢直連取得；沿用 `mestest_carrier_basis_dependencies_*` 和 first-phase CREATE SQL，未重複查詢/新增 SQL。
- [x] 新增 Vue DEV 頁 `web/src/components/FactoryBasisMaster.vue`，接上 `/YMES/SMD/Factories` 與 FunctionNo=`FACTORY BASIS`；包含查詢/狀態篩選、選列/多選、Add/Edit/Copy/Delete/Approve、KD 黃色定義表單、稽核及 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/工廠基本資料.md`，更新功能索引、Oracle 查詢記錄與家用端 API/schema/簽核 handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端比對現況並補齊，核准需串正式 Issue/Job 流程。

## 結束規則

- 不要把「已建立 Vue 元件」視為「已完整復刻」；需對照舊 Form 的欄位、按鈕、流程與 Service 規則。
- 不要刪除來源 DDL 尚未核實的 YMES 欄位或資料表。對既有表先比對，缺表才建，差異用 forward migration。
- 連線密碼、Token、SSH 私鑰不可放入 `todolist_*.md`、SQL、程式碼或 Git。

## 2026-11-16 EP202 配件類別資料進度與下一項

- [x] GitHub 最新 main 已 pull；新增 web/src/components/AccessoryTypeMaster.vue，接上「設備資料 → 配件 → 配件類別資料」與 /YMES/Equipment/AccessoryTypes。
- [x] 依 WinForms 建立狀態篩選、AccessoryType 查詢、清單、Add/Edit/Copy、批次 Delete/Approve、屬性管理、壽命管理、匯入預覽及 KD 黃色系表單。
- [x] 建立 docs/modules/EQP/配件/配件類別資料.md；查詢 MESTEST 四表 DDL 並附原始 Oracle DDL / PostgreSQL 空表 CREATE SQL，家用 API 工作已寫入 todolist_homepc.md。
- [ ] 待家用 API 合併後，pull 並對接 EP202 Production endpoints；補 PropertyNo 系統選項選擇器、壽命公式編輯器及 Excel .xlsx/.xls 前端互動。
- [ ] 家用端 2026-11-16 執行最新 Web build 時，`AccessoryTypeMaster.vue` 第 52、59 行各有 TS1109 語法錯誤（copy 模式 description ternary 缺少 true 分支的空字串值）。修正並重新執行 `npm run build`，再做頁面瀏覽器驗收；家用端未直接修改 UI。
- [x] 已完成 EP201 配件分類與 EP204 可用狀態的 WinForms/Service 對照、Vue Development 畫面、規格及 Oracle DDL/PG CREATE SQL 交接；詳細完成狀態見本文下方 2026-11-16 更新。
- [ ] 本次未執行 build 或測試；完成後需在後續驗證階段處理，不代表 Production 功能已完成。

## 2026-11-16 EP201 配件分類與 EP204 可用狀態完成公司端交接

- [x] GitHub main 已確認為最新；本次保留先前未提交的 Oracle DDL 並繼續公司端工作。
- [x] 新增 web/src/components/AccessoryCategoryMaster.vue，接入設備資料 → 配件 → 配件分類資料與 /YMES/Equipment/AccessoryCategories；提供狀態/分類查詢、CRUD/複製、核准、狀態依來源刪除/停用、CSV 匯入預覽。
- [x] 新增 web/src/components/AccessoryCategoryStateMaster.vue，接入 EP204 獨立功能路徑，也可由 EP201 以 Vue 彈窗傳入分類；包括狀態選項、初始旗標、作用名稱/介面驗證、CRUD/複製、核准與刪除/停用。
- [x] App.vue 三層選單 fallback、動態 API menu merge、FunctionNo route 與 Vue component 分派已加入 ACC CATEGORY / ACC CATEGORY STATE。
- [x] 依 WinForms Form/Designer、wsEQP、clsAccBasis/clsBasis_UPD 追出按鈕、欄位、狀態、簽核與 DataStamp 規則；規格分別為 docs/modules/EQP/配件/配件分類資料.md、配件分類狀態定義設定.md。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 TBLEQPACCCATEGORYSTATEBASIS 與 TBLEQPACCSTATEBASIS DDL/欄位/PK/index；建立兩份 PostgreSQL 空表 CREATE SQL，修正 first_phase_create_tables.sql 中曾標成推測的 lookup 欄位 default。
- [x] todolist_homepc.md 與 docs/DATABASE_CONNECTION.md 已列出三表比對/migration、API/options、簽核、InitialFlag 交易/唯一性及欄位契約。
- [ ] 家用 API 完成後，公司電腦 pull 並逐欄對接/核對；EP201 Excel .xls/.xlsx 與舊 DataChecker 在 Vue Development 先簡化為 CSV，須依正式 API 再補前端體驗。
- [x] ACC BASIS / EP203「配件基本資料」已完成公司端復刻與 Oracle DDL 交接；詳見本文 EP203 進度及獨立規格。
- [ ] 未執行 build 或測試；整套 MES 其餘 Function 尚未完成，繼續依 ModuleOrder 往下逐項復刻。

## EP203 配件基本資料公司端進度（2026-11-16）

- [x] 已 pull GitHub main（f569a3f，當時與 origin/main 同步）。
- [x] 透過 PowerShell 5.1 + ODP.NET 使用本機受保護 Oracle 設定，唯讀取得 TBLEQPACCESSORYBASIS 與五張關聯/狀態表 DDL；並查得八個 Oracle index DDL，未讀 MES 業務資料。
- [x] 已將六表 Oracle 原始 DDL、PG 空表 CREATE SQL 及 schema 比對/API 工作交給家用端：database/reference/mestest_accessory_basis_source_ddl.sql、mestest_accessory_basis_create.sql、todolist_homepc.md。
- [x] 已依 WinForms frmAccessoryBasis/Def、EP_ACCProperty、AccessoryDataImport 追出狀態、清單顯示欄位、資料表、核准/刪除、複製、屬性及匯入規則；規格為 docs/modules/EQP/配件/配件基本資料.md。
- [x] Vue EP203 已加入設備資料 → 配件第三層選單、/YMES/Equipment/AccessoryBasis 頁面及 localStorage DEV 假資料互動；包括全 Grid 欄位、狀態篩選、多選按鈕、表單、屬性視窗、CSV 預覽與 Production Excel API 契約。
- [ ] 家用端比對六張 YMES 表並實作 Production API 後，公司端 pull 並對接正式 API。DEV 核准/簽核與 CSV 匯入僅作 UI 預覽，Excel 正式解析由 API 完成。
- [x] EP205「配件狀態設定」已完成 WinForms Form/Def/Service 對照、Vue Development 頁與 API/schema handoff；詳見 docs/modules/EQP/配件/配件狀態設定.md。
- [ ] 不執行 build/test，除非使用者提出驗證要求；不要將 Oracle 連線密碼寫進文件或 Git。

## 2026-11-16 EP205 配件狀態設定公司端進度

- [x] 依 frmEQPACCStateBasis、frmEQPACCStateBasisDef、wsEQP、clsAccBasis 核對主 Grid、ADD/EDIT/DEL/Copy/Close、Apply/Confirm、數值/StateType/色彩/稽核/DataStamp 規則。
- [x] 核對 Oracle TBLEQPACCSTATEBASIS DDL、欄位/PK/index；沿用既有來源 DDL 與 PostgreSQL 空表 SQL，不重複建立表。
- [x] 新增 web/src/components/AccessoryStateBasisMaster.vue，接上「設備資料 → 配件 → 配件狀態設定」和 /YMES/Equipment/AccessoryStates；Development localStorage 假資料提供新增、修改、複製、刪除、多選及 Apply/Confirm。
- [x] 建立 docs/modules/EQP/配件/配件狀態設定.md，更新規格索引與家用端 schema/API TODO。
- [ ] 家用端核對 YMES schema 並完成 Production API 後，公司端 pull 對接實際 API。
- [ ] 下一項繼續按設備 FunctionNo 追 WinForms Form/Service；需新增資料表時由 PowerShell + ODP.NET 唯讀查 Oracle DDL。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP101 設備供應商基本資料補正（公司端 2026-11-16）

- [x] 重新核對 frmEQPVendor、frmEQPVendorDef、frmEQPVendorContactorDef：補上新增/複製供應商「...」帶入編號按鈕，以及供應商與聯絡人表單的「確定、關閉、套用」流程。
- [x] Development 假資料改由應用程式環境選擇控制；切到 Development 才讀寫 localStorage，切換環境或 API 位址時重新載入。正式/測試環境使用 API。
- [x] 儲存後更新表單 DataStamp 與稽核欄位；更新 EP101 規格、規格索引及本檔。
- [ ] 下一項按設備功能清單繼續對照 WinForms Form/Designer/Service；只有發現未有規格的資料表時才查 Oracle DDL 並交付家用端。
- [ ] 本次未執行 build 或測試；未變更 API 與資料庫 schema。


## EP102 設備工程群組資料補正（公司端 2026-11-16）

- [x] 直接核對 frmEQPEngineerGroupBasis/Def 與 Designer：主按鈕順序 Add/Edit/Delete/Copy/Close；Find/Print 隱藏；Def 支援 Confirm/Close/Apply；編號失焦轉大寫。
- [x] Vue 表單加入套用並保留表單、儲存後更新 DataStamp/稽核欄位；主按鈕依 WinForms 排序。
- [x] Development 假資料依應用程式選取環境啟用；切換環境/API 位址時重新載入；Test/Production 呼叫 API。
- [x] 更新 EP102 規格、索引與本檔。正式 API及 TBLUSRUSERGROUP schema 工作仍留給家用端既有 TODO。
- [ ] 下一項繼續依設備設定清單對照 WinForms Form/Designer/Service；需要新資料表才查 Oracle DDL 並交付家用端。
- [ ] 未執行 build 或測試；沒有更動 API/schema。


## EP103 設備類別資料公司端 UI 規格完成（2026-11-16）

- [x] 直接對照 frmEquipmentType/Def、ConsumeBasis/Def、EQPProperty/Def、圖片選擇器及工程群組查詢事件；完成主表單、耗材表單、屬性表單的確定/關閉/套用與套用異動狀態。
- [x] EngineerGroupNo、PictureName 改為唯讀欄位與 WinForms「...」選擇視窗；設備圖片上傳與圖片庫選擇保留。
- [x] Development 假資料只在 Development preview 啟用；EP103 頁使用共用 api/equipment/engineer-groups 契約，其他環境走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備類別資料.md、規格索引及家用端 API 待辦；沒有新增資料表或 SQL。
- [ ] 家用電腦比對五張設備類別表與配件目錄表 schema，完成 API/圖片/匯入/簽核流程後，拉回最新版本做 Production 契約核對。
- [x] EP105「設備群組資料」WinForms Form/Designer/Service 已核對並完成公司端 UI 規格補正；EP106 後續進度見本文 EP106 區塊。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP105 設備群組資料公司端 UI 補正（2026-11-16）

- [x] 直接對照 frmEquipmentGroup/Def、Designer、clsEQPBasis 與 wsEQP；保留主從清單、隱藏 Copy 按鈕、未核准群組才能編輯/加入/移除、設備只能加入一次及 Chamber 排除規則。
- [x] 群組 Def 補上 WinForms 確定/關閉/套用、異動後才啟用 Apply、建立/修改稽核欄位、編號失焦轉大寫及儲存後更新 DataStamp/清單。
- [x] Development 假資料改由所選 Development preview 控制；環境/API 位址切換會重新載入。Production 走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備群組資料.md、規格索引與 todolist_homepc.md。主從與 SMD 表 Oracle DDL/CREATE SQL 已先前查得，本次無新表/無需再查 DDL。
- [ ] 家用端按 todolist_homepc.md EP105 區塊比對 schema、完成 CRUD/Approve/SMD API，部署後公司端再對接並驗證。
- [x] EP106 設備狀態設定 UI/互動已完成本輪 WinForms 對照；詳見下方 2026-11-16 進度。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP106 設備狀態設定公司端補正（2026-11-16）

- [x] 開工前 pull GitHub `main`，確認與 `origin/main` 同步於 `894d5a4`；來源 WinForms 位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_EQPStateBasis`。
- [x] 按 Designer 還原表單尺寸與單欄欄位排列，校正主功能按鈕順序及 Def 表單 Confirm/Close/Apply 順序；Set Color、Add/Copy Apply 保留欄位與編號焦點行為已補齊。
- [x] Development 假資料僅在 App 環境選為 Development 且執行 DEV build 時啟用；切換環境/API 位址會重載。Test/Production 使用 API。
- [x] Delete 會把目前列合併到多選項目，逐筆檢查引用與刪除；部分成功後重載並保留錯誤，符合舊表單逐筆處理行為。
- [x] 更新 EP106 規格、索引及家用端 EP106 待辦；本次未新增表/DDL，既有兩張引用表 Oracle DDL/PG CREATE SQL 已交付。
- [ ] 家用端比對兩張引用表 schema/migration、授予最小 SELECT 權限，移除 DELETE fail-closed 503；部署後驗證 409/204 引用檢查。
- [x] EP107「設備點檢表設定」已於後續進度完成 WinForms Form/Designer/Service 對照與 Vue 補正。
- [ ] 未執行 build 或測試；不代表 Production 刪除驗收已完成。


## EP107 設備點檢表設定公司端 UI 補正（2026-11-16）

- [x] 開工前 pull GitHub `main`；最新已推送基準為 `1ab42be`，`git pull --ff-only origin main` 確認已同步。來源 WinForms 為 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_QCListBasis`。
- [x] 對照主 Form、Def、Designer 與 Service：主從 Grid、作用列+多選刪除、複製、明細多選刪除/重排、主檔/明細驗證及 Confirm/Close/Apply 流程已補齊。
- [x] Def 表單加入建立/修改稽核 GroupBox，按鈕順序 Confirm/Close/Apply；明細操作按鈕移至清單右側並依 WinForms 排序。新增/複製 Apply 保持原模式。
- [x] Development 假資料只有 DEV build 且選取 Development preview 時啟用；Production/Test 走 API。修正明細刪除重複宣告，環境/API 切換關閉編輯器並取消舊明細請求。
- [x] 更新 `docs/modules/EQP/設備設定/設備點檢表設定.md`、規格索引及家用端 EP107 API TODO；既有兩表 PostgreSQL CREATE SQL 已存在，本輪沒有新增資料表或 DDL。
- [ ] 家用端逐欄比對兩張 YMES 表、實作主從原子 CRUD/options API；完成後公司端 pull 對接並核對 DataStamp/稽核欄位。
- [x] EP301「載具分類資料」公司端 UI、規格、DDL 與 API handoff 已推送；Production schema/API 仍由家用端待辦追蹤。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP301 載具分類資料公司端進度（2026-11-16）

- [x] EP107 推送後重新 pull，確認 GitHub `main` 已同步於 `f6cb3d9`；來源位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierCategory`。
- [x] 直接讀取 WinForms 主/Def Form、Designer、`wsEQP`、`clsCarrierBasis`；記錄狀態查詢、按鈕、批次作用列、核准工作流、Delete 0/-1 實刪/2 停用/1 拒絕、DataStamp、Audit 與表單尺寸。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 及本機受保護設定唯讀取得 MESTEST.TBLEQPCARRIERCATEGORY DDL、欄位、constraint 與 index；建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL。8 欄，PK 唯一索引，無其他 index/FK。
- [x] 新增 Vue DEV 頁、Development-only 假資料、三層載具分類選單與 EP301 route。Production API 呼叫契約及 Issue/Job 簽核複雜度已記錄，DEV 核准明確簡化為直接 0→2。
- [x] 建立 `docs/modules/EQP/載具/載具分類資料.md`，更新 `docs/DATABASE_CONNECTION.md`、規格索引及 `todolist_homepc.md` API/schema 交接。
- [ ] 家用端確認 YMES schema/migration、實作 Production CRUD/簽核 API；公司端 pull 後對接並核對部分成功、DataStamp 與狀態轉換。
- [x] 已開始 `CARRIER TYPE` / EP302「載具類別資料」；進度見下方 EP302 區塊。
- [ ] 未執行 build 或測試；Production API 未建置。

## 2026-11-16 ENT ET101 客戶基本資料公司端交接

- [x] 開始前 pull GitHub `origin/main`；當時最新提交為 `c6c7895`，工作樹乾淨。
- [x] 追查 `frmCustomerBasis`、`frmCustomerBasisDef`、Designer、ET_ETDataImport、`clsCustomerBasis` 與 `wsENT`，記錄清單欄位、表單欄位/鎖定規則、批次刪除/核准、Customer BASIS 簽核與 Excel 匯入流程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀查詢 `TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR` 的 DDL、NULL/default、PK、索引/FK；沒有查客戶業務資料或執行 Oracle DDL。
- [x] 新增 Vue DEV 頁 `web/src/components/CustomerBasisMaster.vue`、FunctionNo `CUSTOMER BASIS` 路由 `/YMES/Customer/Basis` 及 fallback 三層選單；Production 經 API，Development 使用標明的 localStorage 假資料。
- [x] 新增 `docs/modules/ENT/客戶基本資料.md`、Oracle source logical DDL、PostgreSQL 空表 CREATE SQL、規格索引、DB 連線查詢紀錄及家用端 migration/API/簽核/匯入待辦。
- [ ] 未執行 build/test 或瀏覽器人工驗收；待後續依要求驗證。
- [ ] 下一個公司端功能從 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續依序核對 Form/Designer/Service。設備 AutoLoader Function `B_EQP AUTOLOADER SETUP` 的指定 Form 檔在目前 WinForms source tree 找不到；後續先記錄其 binary-only 來源限制，再做其餘有原始碼的功能。

## EP302 載具類別資料公司端進度（2026-11-16）

- [x] 從 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierType` 讀取 Form/Def/Designer；讀取 `wsEQP.Load/Add/Edit/Delete/ApproveCarrierType` 與 `clsCarrierBasis` CRUD/query 實作。
- [x] 使用 PowerShell 5.1 + ODP.NET、本機受保護設定，只讀查詢 MESTEST.TBLEQPCARRIERTYPE 的 DBMS_METADATA DDL 與欄位/constraint/index 字典。13 欄，PK 唯一索引，無外鍵或額外索引。
- [x] 建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL；UI 規格記錄 UseLayer 0/1、已核准 CarrierCategory 查找、NUMBER(3,0) 數值限制、狀態和 Issue/Job 核准流程。
- [x] 新增 Vue DEV 頁、carrier type route 與主選單 FunctionNo 映射；DEV 核准明確簡化 0→2，分類查找只列已核准類別。
- [ ] 家用端確認 YMES schema/migration、實作 CRUD/分類選項/正式核准 API；公司端 pull 後對接與核對。
- [ ] 下一個公司端功能依既有全部模組待辦順序續做；先以 WinForms Form/Designer/Service 判斷功能完整範圍，結構缺漏時用 PowerShell 5.1 直連 Oracle 查詢。
- [ ] 未執行 build 或測試；Production API 未建置。

## EP303 載具基本資料公司端進度（2026-11-16）

- [x] 先 pull GitHub main，確認最新基準 a1fa7cd；工作樹初始乾淨。
- [x] 依序核對 frmCarrierBasis/Def、frmBasisTemplat、wsEQP、clsCarrierBasis；完成清單欄位、唯讀/選擇器、狀態篩選、Add/Edit/Copy/Delete/Approve、DataStamp、WIP 同步及簽核規則規格。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護設定唯讀查詢 TBLEQPCARRIERBASIS、TBLWIPCarrierState、TBLSMDFactoryBasis 的 DDL/欄位/約束/索引；未查詢業務資料，未在 Oracle 執行 DDL。
- [x] 建立主表與依賴表 Oracle source DDL、PostgreSQL 空表 CREATE SQL；更正 first-phase SQL 的 DEFAULT，移除這三表未證實欄位的猜測註解。
- [x] 新增 Vue DEV 畫面與 /YMES/Equipment/CarrierBasis 路由，三層選單 FunctionNo 為 CARRIER BASIS；加入三個選項視窗、KD 黃色欄位、Apply/Audit 與批次處理。
- [x] 建立 docs/modules/EQP/載具/載具基本資料.md，更新規格索引、資料庫連線記錄及家用端 API/DDL 待辦。

## EP401 設備參數資料公司端進度（2026-11-16）

- [x] 開始前 pull GitHub 最新 main；遠端已是 `4628cea`，無家用電腦新提交。
- [x] 追查 `frmEQPRecipeBasis`、Basis/Detail Define、wsEQP 與 clsEQPBasis/kcBasis_UPD，核對雙 UltraGrid、欄位/按鈕、狀態限制、簽核、刪除與 Copy/Version 明細實際複製欄位。
- [x] Oracle DDL 直接查詢 MESTEST；明確以 Windows PowerShell 5.1 啟動 Oracle.ManagedDataAccess。查詢 `TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL` 的 DDL、NOT NULL/default、PK、索引/FK，並查明 Type=-1/0/1 對應 NONE/TYPEVALUE/RANGE。沒有查 MES 業務資料或在 Oracle 執行 DDL。
- [x] Vue DEV 頁 `web/src/components/EquipmentRecipeBasisMaster.vue`：雙清單、狀態篩選/RecipeGroup 查詢、主檔 Add/Edit/Copy/Delete/Approve/Version、明細 Add/Edit/Delete、ElementType=9 本地選擇器、KD 黃色表單與本機假資料；Production 顯示 API 尚未建置，不使用假資料寫入。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL，並校正 first-phase Recipe Basis/Detail 的欄位 DEFAULT/NOT NULL/額外索引。
- [x] 建立 `docs/modules/EQP/設備設定/設備參數資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 與 `todolist_homepc.md`。
- [ ] 尚未執行 build/test 或瀏覽器人工驗收；Production schema/API/簽核仍由家用電腦依交接清單完成。
- [ ] 尚未以瀏覽器人工驗收；未執行 build 或測試。家用端 API/schema 完成後，pull 最新版並對接正式端點。
- [ ] 後續繼續依 docs/MES_MENU_FUNCTION_INVENTORY.md 逐功能復刻；每個功能先讀 WinForms Form/Designer/Service，必要時以 PowerShell 5.1 直連 Oracle 查 DDL，完成後同步規格與兩台電腦待辦。

## ENT ET102 客戶連絡人資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` pull；遠端已是 `f5dce82`，工作樹乾淨。
- [x] 核對 `frmCustomerContactor`、`frmCustomerContactorDef`、`wsENT.Load/Add/Edit/DelCustomerContactor`、`kcENT.clsCustomerBasis`：客戶查找、欄位、排序、多選刪除、Copy、Apply/Confirm、主鍵唯讀等規則已寫入功能規格。
- [x] 沿用 ET101 直連 Oracle 已取得的 `TBLENTCUSTOMERCONTACTOR` DDL；沒有重複查詢、沒有增加第二份 Create SQL，也沒有讀取 MES 客戶業務資料。
- [x] 新增 Vue DEV 畫面 `web/src/components/CustomerContactorMaster.vue`，路徑 `/YMES/Customer/Contactors`、FunctionNo `CUSTOMER CONTACTOR`，Development localStorage 假資料含客戶選擇、CRUD/Copy/多筆刪除及稽核欄位。
- [x] 新增 `docs/modules/ENT/客戶連絡人資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 及本家用端 API 待辦；沿用 ET101 的既有 `database/reference/mestest_customer_basis_create.sql`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端建立。
- [ ] 下一段依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 往後核對未遷移的 ENT/EQP 功能與其 WinForms 程式；缺少 DDL 時直接以 PowerShell 5.1 + ODP.NET 查詢 Oracle 結構。

## ENT ET106 客戶出貨地址資料公司端進度（2026-11-16）

- [x] 依序核對 `frmCustomerShipAddr`、`frmCustomerShipAddrDef`、`frmCustomerShipDeclarationDef`、ENT WebService/Core，確認主畫面雙 Grid、只選已核准客戶、按客戶載入全部報關列、出貨地址表單與報關子功能規則。
- [x] 出貨地址表 `TBLENTCUSTOMERSHIPADDR` 沿用 ET101 已直連取得的 DDL；缺少的 `TBLENTCUSTOMERSHIPDECLARATION` 已使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線直接唯讀查得。
- [x] 報關表原始 DDL 有 13 欄及 8 個 NOT NULL constraint；資料字典沒有 PK、unique/一般索引或 FK。已將 Oracle logical DDL 與不加 PK 的 PostgreSQL 空表 CREATE SQL 追加到客戶 DDL 檔案。
- [x] 新增 Vue DEV 雙 Grid 頁 `web/src/components/CustomerShipAddressMaster.vue`、路徑 `/YMES/Customer/ShipAddresses`、FunctionNo `CUSTOMER SHIP ADDRESS`；含已核准客戶選擇、地址/報關 CRUD、報關預設值、Taker/Forwarder/Broker DEV 選擇器、稽核欄位與 KD 黃色表單。
- [x] 建立 `docs/modules/ENT/客戶出貨地址資料.md`，更新規格索引、DB 連線記錄及家用端 migration/API 待辦。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 下一功能已依選單開始 ET103 報關行基本資料；最新狀態見下方 ET103 區塊。

## ENT ET103 報關行基本資料公司端進度（2026-11-16）

- [x] 開始 ET103 前已完成 ET106 並推送 `49fbd9c`；其後讀取 `frmBrokerBasis`、`frmBrokerBasisDef`、`wsENT`、`kcBasis_Query`、`kcBasis_UPD`、`kcENT.clsCustomerBasis`。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從先前提供的受保護連線直接唯讀查詢 `MESTEST.TBLENTBROKERBASIS` DDL、欄位 NULL/default、PK、索引/FK；未查業務資料、未執行 Oracle DDL，憑證未輸出/存檔。
- [x] 查得 13 欄、BrokerNo 單欄 PK/唯一索引，無 FK/其他索引。補 Oracle source DDL 與 `ymes.tblentbrokerbasis` PostgreSQL CREATE SQL，並校正 first-phase reference 中此表遺漏的三個 DEFAULT。
- [x] 新增 `web/src/components/BrokerBasisMaster.vue`、`/YMES/Customer/Brokers` 路徑與 `BROKER BASIS` 對應；DEV 假資料支援狀態篩選、搜尋、CRUD、停用/刪除、Apply/Confirm、稽核及直接核准預覽。
- [x] 新增 `docs/modules/ENT/報關行基本資料.md`，記錄 WinForms 版面、欄位限制、狀態刪除規則、`BROKER BASIS` 簽核流程及 YMES API 契約；更新規格索引、DB 連線記錄及家用端待辦。
- [ ] 家用端比對 YMES schema/migrations，依 DDL 只新增缺少的表；實作 Production CRUD、DataStamp、IssueState 軟刪除/硬刪除與既有簽核服務 API。
- [ ] 本次未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 後續已依 ENT FunctionOrder 開始 ET104「貨運行基本資料」；詳細進度與待辦見下方 ET104 區塊。

## ENT ET104 貨運行基本資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，遠端已是 `8d87cd8`；前一段 ET104 DDL 草稿保留。
- [x] 核對 `frmForwarderBasis`、`frmForwarderBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveForwarderBasis` 與 `kcBasis_Query`、`kcBasis_UPD`：狀態篩選、多選操作、欄位/唯讀、Broker 選擇、Apply/Confirm、狀態2舊服務缺陷都記入功能規格。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線唯讀取得 `MESTEST.TBLENTFORWARDERBASIS` DDL、欄位 nullable/default、PK/index/FK；只查結構，未查業務列、未在 Oracle 執行 DDL，憑證未輸出。
- [x] Oracle 原始 logical DDL 與 PostgreSQL `ymes.tblentforwarderbasis` 空表 CREATE SQL 已加入客戶主檔 reference；同步校正 first-phase reference 的 DEFAULT 與欄位註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ForwarderBasisMaster.vue`、`/YMES/Customer/Forwarders` 路徑、`FORWARDER BASIS` 對應；含狀態篩選、動態選列、多選、CRUD、停用/刪除、核准預覽、稽核及只顯示已簽核報關行的選擇器。
- [x] 新增 `docs/modules/ENT/貨運行基本資料.md`，更新功能索引、DB 連線記錄、ET103 報關行 lookup query 規格與家用端 ET104 schema/API/簽核 TODO。
- [ ] 家用端比對 YMES schema/migrations；只新增缺漏表或欄位，實作 ET104 Production CRUD、BrokerNo=已簽核報關行驗證、DataStamp、狀態刪除及 `FORWARDER BASIS` 簽核 API。
- [x] ET103 radio/checkbox KD 黃色 accent 與簽核完成後 audit/DataStamp 規格修正已包含於 ET104 提交 `39b2973`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] ET104 與 ET103 收尾修正已提交並推送 GitHub：`39b2973 feat: add forwarder basis UI`。
- [x] 後續已依 ENT FunctionOrder 開始 ET105「取貨公司基本資料」；最新進度見下方 ET105 區塊。

## ENT ET105 取貨公司基本資料公司端進度（2026-11-16）

- [x] 依 WinForms `frmTakerBasis`、`frmTakerBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveTakerBasisIssuestate` 與 `kcENT.clsCustomerBasis` 核對狀態、欄位、按鈕、稽核與來源版本不一致處。
- [x] Oracle DDL 原先不在 repo。以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線只讀取得 `MESTEST.TBLENTTAKERBASIS` 的 DBMS_METADATA DDL/欄位 NULL-default/constraints/indexes；只查結構，不讀業務列、不執行 Oracle DDL，連線值未輸出。
- [x] Oracle source logical DDL、PostgreSQL 空表 CREATE SQL 已追加 customer reference；first-phase SQL 預設值與猜測性欄位註解已修正。
- [x] 新增 Vue DEV 頁 `web/src/components/TakerBasisMaster.vue`、路徑 `/YMES/Customer/Takers` 與 `TAKER BASIS` 路由；含狀態篩選、即時選列、多選 CRUD、停用/刪除、核准預覽、KD 黃色欄位與稽核。
- [x] 新增 `docs/modules/ENT/取貨公司基本資料.md`，更新功能索引、DB 連線記錄與家用端 API/schema TODO。
- [ ] 家用端先比對 YMES schema/migrations；缺少才新增 Taker table migration。實作 GET/POST/PUT/DELETE/approve、DataStamp compare-and-swap、稽核與 `TAKER BASIS` 共用簽核服務。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 待家用端完成。
- [x] 下一項 ET201 已開始；完成狀態見下方 ET201 區塊。

## ENT ET201 外包商基本資料公司端進度（2026-11-16）

- [x] 本次先 pull `origin/main`，確認 GitHub 最新版已同步且工作樹乾淨。
- [x] 對照 `frmSubcontractor`、`frmSubcontractorDef`、`frmSubcontractorContDef`、wsENT 與 clsCustomerBasis；記錄雙 Grid、按鈕、欄位、主從刪除、複製、套用與 Issue/Job 簽核流程。
- [x] Oracle 兩張表結構已由既有 PowerShell 5.1 + ODP.NET 唯讀查詢取得；新增 source DDL 與 PostgreSQL 空表 CREATE SQL。未讀取業務資料，也未對 MES Oracle 寫入。
- [x] 新增 `web/src/components/SubcontractorBasisMaster.vue`，Development 使用獨立 localStorage 假資料，實作上下雙 Grid、狀態/編號查詢、多選、主檔/連絡人新增修改複製刪除、核准、Mail、KD 黃色欄位與稽核區。
- [x] 加入 `/YMES/Customer/Subcontractors` 路由及 FunctionNo `SUBCONTRACTOR BASIS`；建立外包商規格並更新模組索引、Oracle 連線紀錄及家用端待辦。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/migration/Issue service 仍待家用端完成。
- [x] 後續依 ENT Function inventory 開始 ET202 `ENTOSITEM` 外包項目設定；以下為完成紀錄。

## ENT ET202 外包項目設定公司端進度（2026-11-16）

- [x] 開始前執行 `git pull --ff-only origin main`；GitHub `main` 已是最新版。
- [x] 對照 `frmENTOSItem`、`frmENTOSItemDef`、wsENT、`clsCustomerBasis`，記錄查詢列、Grid 欄位/寬度、Add/Edit/Delete/Copy/Close、表單欄位驗證、Apply/Confirm 與外包流程選取條件。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀查到 `MESTEST.TBLENTOSITEM` DDL、欄位 nullable/default、constraints、PK/index；未讀業務資料、未在 Oracle 執行 DDL、憑證未輸出。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL、ET202 模組規格與規格索引/連線查詢紀錄。
- [x] 新增 Vue DEV 頁 `web/src/components/ENTOSItemMaster.vue`，路徑 `/YMES/Customer/OSItems`、FunctionNo `ENTOSITEM`；含單 Grid、列選取、多選刪除、Add/Edit/Copy、KD 黃色欄位、Audit、Apply/Confirm、外包流程 lookup 與 localStorage 假資料。
- [ ] 家用端比較實際 YMES schema/migrations；缺表時依 `database/reference/mestest_entositem_create.sql` 建 migration，不得以 IF NOT EXISTS 代替欄位差異檢查。
- [ ] 家用端實作 ENTOSITEM GET/POST/PUT/DELETE、DataStamp CAS、伺服器 audit、ProcessClass=3 + IssueState=2 流程驗證及權限檢查；遵守 API/DB 分離，Vue Production 不可使用假資料。
- [ ] 公司端尚未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [ ] ENT 八個 Function 已有 ET101/102/103/104/105/106/201/202 規格與 DEV 頁；下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續盤點下一個尚未完整復刻的 MES 功能，先核對 Form/Service/Core，再查 Oracle DDL。

## 2026-11-16 SMD SD103 儲位基本資料公司端進度

- [x] 從 GitHub main 快轉檢查最新版；本地與 origin/main 均為 3ab417d，沒有新提交。
- [x] 直接讀 WinForms 主畫面、定義窗、wsINV、clsInventory 與更新版 clsBasis_UPD；記錄四狀態篩選、欄位/按鈕、InventoryNo 選取、複合鍵、Copy、批次 Delete/Approve、IssueNo 與 DataStamp 版本差異。
- [x] 用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查 MESTEST.TBLINVLOCATORBASIS DBMS_METADATA 與資料字典；確認複合 PK、沒有 FK/額外索引。新增原始 Oracle DDL 和 PostgreSQL 空表 CREATE SQL，校正 first-phase bootstrap 預設值並刪除未核實欄位註解。
- [x] 新增 Vue DEV 頁 web/src/components/LocatorBasisMaster.vue，路由 /YMES/SMD/Locators、FunctionNo=LOCATOR BASIS；依 WinForms 具備查詢、狀態切換、選列、多選、Add/Edit/Copy/Delete/Approve、已核准庫存挑選器、KD 黃色欄位、稽核及 Apply/Confirm。
- [x] 新增 docs/modules/SMD/儲位基本資料.md；更新資料庫連線查詢紀錄、功能索引、家用端 API/schema handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；家用端需比對已部署 YMES schema 並補 forward migration/API 和正式 Issue/Job 核准流程。
- [x] SD107「生產線別基本資料」已依 Form/Service/Core 完成 DEV 頁、規格、Oracle DDL/PG CREATE 與家用端 API/schema handoff。下一個尚未完成的 SMD FunctionKey 為 SD108「設備狀態監控_區域」。
## 2026-11-16 SMD SD107 生產線別基本資料公司端進度

- [x] 直接對照 frmPDLineBasis / frmPDLineBasisDef、wsSMD、clsSMDBasis 與 clsBasis_UPD；整理欄位、按鈕、ValueList、四種 IssueState、Apply、批次操作、Delete 與 Issue/Job 核准邏輯。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀查得 TBLSMDPDLINEBASIS 完整 DDL、欄位 null/default、constraints、PK/index；未讀業務資料，未執行 Oracle DDL。
- [x] 新增 web/src/components/ProductionLineBasisMaster.vue，路由 /YMES/SMD/ProductionLines，FunctionNo=SMD PDLINE BASIS；Development 使用 localStorage 假資料，Production 只呼叫 API。
- [x] 新增 docs/modules/SMD/生產線別基本資料.md、Oracle source DDL 與 PostgreSQL 空表 CREATE SQL；修正 first-phase bootstrap 的來源 defaults 並移除猜測欄位註解。
- [x] 更新 SMD 規格索引、Oracle 連線查詢紀錄及 todolist_homepc.md API/schema handoff。
- [ ] 本次未執行 build、test 或瀏覽器驗收；家用端部署 API/schema 後公司端再 pull 對接。
- [ ] 後續 SMD 頁面：SD108 設備狀態監控_區域、SD109 工位基本資料；依序讀 WinForms Form/Designer/Service/Core，再查缺少的 Oracle DDL。
- [ ] 請以 Oracle ValueListProvider 同一來源查 `TBLSYSVALUELIST` 中 `TABLENAME='TBLSMDPDLINEBASIS'`、`FIELDNAME='PDLINESTATUS'` 的正式值、排序與顯示文字，並交付繁中資源翻譯；不要以 Vue DEV 標籤或舊註解代替。
- [ ] 供家用端實作 Production Approve：核實 Issue/Job/IssueDetail/IssueJobMap 等簽核資料表的 Oracle DDL、資料欄位與 Service 建立流程；目前 YMES 尚無 Issue/Job 表，不能用 DEV 的直接 0→2 模擬代替。

## SMD SD108、SD109 公司端交接（2026-11-17）

- [x] 開始前 pull GitHub main，遠端新版本為 09dfd65；包含家用端 SD106 過帳電腦區域 migration/API 及 SD104/SD105 schema handoff，已一併納入。
- [x] 依 WinForms frmAreaShow、frmAreaShift、frmAreaShiftDef、frmSMDPDLinePositionBasis、frmSMDPDLinePositionBasisDef 與 clsSMDBasis 核對畫面、事件、班別登入狀態檢查及複合鍵。
- [x] SD108 區域圖、縮放/網格、右鍵狀態/歷史、雙擊班別維護與班別人員增刪已做 Vue Development Preview；SD109 搜尋、複合鍵 CRUD/Copy、多選刪除、作業區與 eSOP 選擇、Apply/Confirm 已做 Vue Development Preview。
- [x] Oracle DDL 由本機受保護連線以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得；新增兩組 source DDL 與 PostgreSQL 空表 CREATE，並修正 first-phase SQL 中 SD109 defaults/猜測性欄位註解。
- [x] 完成 SMD SD108/SD109 規格、功能索引、DB 查詢紀錄與家用端 API/schema handoff。補明 nullable DataStamp 用 `dataStampIsNull=true` 做精確 NULL CAS，並修正 SD109 刪除請求漏傳版本值。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/schema 需家用端按 todolist_homepc.md 完成。
- [ ] 下一步依 MES_MENU_FUNCTION_INVENTORY 的 ModuleOrder/FunctionOrder 繼續找下一個尚未完整復刻的 Function（接續 MTL），先核對 WinForms Form/Designer/Service/Core，再查 Oracle DDL。

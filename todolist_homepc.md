# 家裡電腦接手清單

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

## 公司電腦目前交付

- Vue 主選單已能呈現三層 Module → Menu Group → Function。
- UI 預計使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。
- 正式選單列出五個既有主檔頁：「設備管理」、「作業站管理」、「物料管理」、「流程管理」、「產品管理」；角色權限先不套用。
- 選單中尚未遷移的 FunctionNo 會明確標示，避免使用者誤以為功能已可操作。
- 主檔頁仍由 Vue 呼叫獨立 API；Web 不連資料庫。

## MES 資料庫 CREATE SQL

- 已附實際 MES Oracle `MESTEST` 資料字典轉出的 PostgreSQL 建表 SQL：[`database/reference/mestest_first_phase_create_tables.sql`](database/reference/mestest_first_phase_create_tables.sql)。
- 這份 SQL 包含 75 張第一階段主檔相關表，以及 TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 三張系統選單/功能目錄表；目標 schema 為 YMES，不包含任何 MES 業務資料。
- 三張系統選單/功能目錄表已依目前 MES Oracle MESTEST 資料字典補入 SQL；五個既有主檔選單以 migration 寫入，並非舊 MES 完整選單匯出。整套 MES 其他第一層模組資料表仍需分批盤點。
- [x] 2026-11-12 以家用電腦的 PPK SSH 連線，在遠端 YMES PostgreSQL 以單一交易執行此 SQL。確認原有 75 張主檔表已存在，這次建立 `tblsysmodule`、`tblsysmenuitem`、`tblsysfunction` 三張表；YMES schema 共 80 張表（78 張 MES `tbl*` 表及 2 張登入支援表）。
- 本次沒有匯入 MES 業務資料；`tblusruserbasis.password` 維持 `varchar(255)`，以保留現有 PBKDF2 雜湊，不縮回來源 DDL 的 `varchar(50)`。
- 此 SQL 尚未由自動 migration runner 管理。全新資料庫需先執行本檔，再依序執行 `database/migrations/0001` 至 `0004`。

## 優先一：選單、功能目錄與權限 API

- [x] 已依目前 MES Oracle MESTEST 原生 DDL 完成三張系統表的 PostgreSQL 結構轉換，保留欄位名稱、預設值、NULL 設定與主鍵。
- [x] 建立五個既有主檔 module 和功能項，分別對應 EQP/OP/MTL/PRS/PRD；前兩項由 `0008_initial_navigation_seed.sql` 建立，後三項由 `0009_expand_initial_navigation.sql` 增補並已套用遠端。
- [ ] 從舊系統匯出其他正式選單資料及排序，補入 `tblSYSModule`、`tblSYSMenuItem`、`tblSYSFunction`；目前五項是既有主檔頁清單，不代表舊 MES 完整選單。
- [ ] 用 Oracle 實際資料確認 `MenuType=0` 的 `MenuNo` 與 `FunctionNo` 關聯；目前 DDL 沒有明確外鍵。停用/隱藏欄位及相關來源表也需依實際資料字典補齊。
- [x] `tblSYSModule` 沒有 `ModuleName` 欄位；API 將 EQP/OP/MTL/PRS/PRD 顯示為設備管理、作業站管理、物料管理、流程管理、產品管理。
- [x] 新增可重複執行的 `0008_initial_navigation_seed.sql` 與 `0009_expand_initial_navigation.sql`；正式選單包含五個既有主檔頁，0009 已套用 YMES 遠端資料庫。
- [x] 實作登入 Cookie 保護的 `GET /api/navigation/menu-tree`，讀取三張 YMES 系統表並回傳 `modules → menuGroups → items` 結構；目前依使用者指示先不套用角色/使用者權限篩選。
- [x] 新增並套用 `0005_navigation_read_access.sql`，僅授予 `ymes_app` 對三張選單表的 `SELECT` 權限。
- [x] API 以 MenuNo 對應 FunctionNo；五個既有主檔選單項均以 MenuNo 對應 FunctionNo。
- [ ] 匯入其他正式選單後，確認各 ModuleName、MenuNo、FunctionNo 與原 MES 一致。
- [ ] 確認已完成的五個功能能由 FunctionNo 導向 Vue：`EQP BASIS`、`OP BASIS`、`MTL BASIS`、`PRS BASIS`、`PRD BASIS`；其他功能回傳識別資料，讓 UI 顯示尚未遷移狀態。

## 優先二：五個主檔的正式資料庫與 API

主檔 API 依 provider 選擇來源；正式環境目前已將設備及作業站接至 PostgreSQL，其他三類仍未接上正式資料表。

- [x] 設備主檔 PostgreSQL repository 已部署：設備/Chamber CRUD、核准、停用、DataStamp 衝突控制；欄位依正式 DDL 對齊，移除不存在的「預設伺服器」輸入。
- [x] `0006_equipment_data_access.sql` 已套用，僅授予 `ymes_app` 對設備表的 CRUD 權限。遠端設備表目前沒有業務資料。
- [x] 作業站 CRUD、核准、停用 API 已部署，對應遠端實際存在的 `YMES.tblopbasis`；使用者先前提到 `tblopno`，但遠端 YMES schema 及提供的 CREATE SQL 都沒有此表。SPC 數量門檻欄位不在 `tblopbasis`，已從作業站頁移除，未新增資料庫欄位。
- [x] `0007_operation_data_access.sql` 已授予 `ymes_app` 作業站表 CRUD 權限；遠端作業站表目前沒有業務資料。

- [ ] 設備：`tbleqpequipmentbasis`、Chamber 關係、設備群/工程群組、設備屬性、參數、QC List；支援清單 CRUD、核准/停用、複製、匯入。
- [ ] 作業站延伸資料：群組、屬性、參數、QC、Error/Abnormal、Resource/Area、Business Rule、SubOP；支援匯入。
- [ ] 物料：`tblmtlmaterialbasis`、Vendor Map、Property、QC Item、Gum Type；建立物料時從類別複製預設屬性/QC 的動作需同交易。
- [ ] 流程：`tblprsprocessbasis`、Node、Node Relation/XML、APS；支援新版本複製、切換目前版本、流程圖驗證與匯入。
- [ ] 產品：`tblprdproductbasis`、Product Process、BOM、產品 OP/屬性及相關引用；複製、建版本、核准、停用與 BOM 更新需遵守舊交易規則。
- [ ] 所有新增/修改/核准/刪除端點均驗證 IssueState、DataStamp、引用限制與舊系統業務規則；正式環境不得使用 Fake provider。
- [ ] 把五份主檔及產品流程整合的舊表欄位、PK/UK、型態、索引與正式 DDL 對齊；目前精簡資料字典中的推測欄位不可直接當正式 schema。

## 優先三：其餘第一層 Module 與功能

- [ ] 以正式 `tblSYSModule`/`tblSYSFunction` 選單清單作為盤點基準，為每個 FunctionNo 對照舊 WinForms FormName/ExecutionFile、WebService、核心類別、查詢/寫入資料表及權限。
- [ ] 依第一層 Module 分批新增 PostgreSQL schema/migration、Contracts DTO、API、交易/驗證與 Vue 功能頁；每個功能頁的查詢條件、欄位、按鈕及子視窗需能追溯至對應 WinForms。
- [ ] 每一批完成後更新遷移清單：ModuleNo、FunctionNo、WinForms Form、API 路徑、Table、UI 狀態（未開始/進行中/完成）及驗收結果。
- [ ] 未完成的功能維持明確的「尚未遷移」狀態；沒有 API 支援的功能不得以靜態假資料回報成功。

## 優先四：整合與部署

- [x] Web/API 分開執行於同一台 Server：Web `/YMES`、API `/YMES/API`。同步修正遠端 Nginx 過期設定，主檔頁不再送到舊 18090 MVC 容器，改由目前 Vue Web 18089 提供。
- [ ] 以 PostgreSQL provider 取代 Production 的 Disabled/Fake 主檔來源；Development Fake 僅供本機預覽。
- [ ] 完成資料 migration/匯入、備份還原、錯誤紀錄與部署設定；憑證、連線字串、`.env.local`、私鑰不得提交 Git。
- [ ] 部署前確認 API 權限、Cookie、HTTPS、CORS/同源反向代理與正式環境設定。

## 驗收完成條件

- 登入後可從 API 取得與舊 WinForms 一致的全部第一層 Module 及三層選單名稱/順序/權限。
- 每個 FunctionNo 都有明確的遷移狀態；標記完成的項目有對應 Vue 頁面、API 與正式資料表，不使用假成功流程。
- 生產資料操作經 API 完成；Web UI 不直接連資料庫；Production 不使用記憶體 Fake provider。
- 主要 WinForms 業務規則、狀態轉換、跨表交易及引用限制已由 API 實作並記錄。

## 2026-11-12 遠端瀏覽器檢查

- [x] `https://yakuyama.com/YMES/` 登入正常；正式區主選單預設折疊。
- [x] 從選單進入設備主檔及作業站主檔，兩頁查詢 API 均回應正常；目前資料表為空，顯示 0 筆。
- [x] 部署 0009 並重建 YMES API 後，瀏覽器確認五個主檔模組均出現在正式選單；預設折疊，展開後各有對應主檔項目。
- [x] YMES API readiness 回應 200；物料、流程、產品三類的 Production PostgreSQL API 尚未實作，進入頁面後目前不能查詢或寫入。
- [x] 設備與作業站新增表單空白送出都會擋下必填欄位。
- [ ] 正式區沒有業務資料可供修改、核准或刪除；本次未寫入測試資料，因此上述寫入流程尚未做資料庫端到端驗收。
- [x] API readiness、Web route 和遠端容器 health check 均回應正常；Nginx 已由過期的 18090 MVC 路由改為 Vue Web 18089。

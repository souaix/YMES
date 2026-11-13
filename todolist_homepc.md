# 家裡電腦接手清單

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

## 家用電腦現在接手

公司端已整理一批可直接接手的來源結構與目錄資料。開始前先 pull 這份清單所在的最新 `main`，並閱讀 `todolist_officepc.md`、`docs/DATABASE_CONNECTION.md`、`docs/MES_MENU_FUNCTION_INVENTORY.md`、OP/MTL 規格及下列 SQL。家用端只負責 DB/API/migration；公司端後續接回 Vue/WinForms 畫面。

1. **先核對 YMES 遠端 schema**：不可直接對整份參考 SQL 執行；`IF NOT EXISTS` 不會校正既有表。核對 20 個 Module、601 個 MenuItem、553 個 Function 三張系統表與現有 0008/0009 seed、路由及權限後，把 `database/reference/mestest_navigation_catalog_seed.sql` 整合成可回滾/可重跑的 migration。來源 seed 是系統目錄中繼資料，不含業務紀錄，也不會刪除 YMES 額外列。完整第一層繁中名稱已對照 `Customer/Production/MESClient/mesresource.zh-cht.resources`，見 inventory。
2. **完成物料 API/schema**：逐欄核對 `database/reference/mestest_mtl_master_create.sql` 的 8 張表及現有 YMES 結構。`tblmtlmaterialbasis` 另有 7 個既有 YMES 欄位，先保留；公司 bootstrap 已按 MESTEST 補 7 個 DEFAULT。依 `docs/modules/MTL/物料主檔.md` 實作 Property、QC Item、Vendor Map、GumHour、Import、主檔核准/刪除交易及正式 ValueList。
3. **完成作業站延伸 API/schema**：比對 `database/reference/mestest_operation_and_system_create.sql` 內 18 張 OP 表；此批 18 張 OP 表及 3 張系統表的原始 Oracle DDL 在 `mestest_operation_and_system_source_ddl.sql`。YMES 目前作業站僅主檔 CRUD/核准/停用已部署；按 `docs/modules/OP/作業站主檔.md` 實作子表引用、資源、商業規則、參數、QC、Error、SubOP、Import 及交易。
4. **繼續設備功能 API**：公司端已交付設備供應商、工程群組、類別、群組、狀態、點檢表等規格與來源 CREATE SQL；先核對現有 YMES 表，再按功能規格完成缺少的 lookup、CRUD、成員與匯入契約。
5. **完成全部第一層模組**：按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 的 ModuleOrder 逐個 FunctionNo/FormName/ExecutionFile，補齊 PostgreSQL 結構、API DTO/驗證/交易及部署。任何來源表缺少 DDL 時，讓公司端透過 PowerShell 直接查 MESTEST 並列出確切表名；不可猜 schema。

每完成一項，更新本檔 checkbox、對應 module spec 與 migration/API 狀態，再 commit/push；不要把連線密碼、Token、SSH 私鑰或 Oracle 連線字串放入文件或 Git。

## 公司電腦目前交付

- Vue 主選單以三層 Module → Menu Group → Function 呈現；正式資料 API 不得把設定分類與主檔功能壓成兩層。
- UI 使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。
- YMES 正式 menu-tree 目前只整合 EQP/OP/MTL/PRS/PRD 五個核心主檔模組；公司端已從 MES Oracle 查得完整 20 個 Module、601 筆 MenuItem、553 筆 Function。依使用者要求，需逐批把完整三層選單與功能遷入。完整來源目錄見 docs/MES_MENU_FUNCTION_INVENTORY.md。
- 每個選單項目都有對應圖示；側欄收合時僅顯示圖示。
- 選單中尚未遷移的 FunctionNo 會明確標示，避免使用者誤以為功能已可操作。
- Vue 開發用設備設定選單已依 WinForm 補列七個項目；設備設定的 Oracle MenuNo/FunctionNo 已可由完整來源目錄核對，尚待逐功能整合至正式 API。供應商及工程群組 Vue 頁使用 Development 假資料，功能規格入口：docs/MODULE_REPLICATION_SPEC_INDEX.md。
- 公司電腦先以 Development 本地假資料完成 WinForms UI 與互動，不呼叫假 API；假資料頁會顯示提示並存於瀏覽器 localStorage。Production 僅呼叫正式 API，不能啟用假資料。

## MES 資料庫 CREATE SQL

- 已附實際 MES Oracle `MESTEST` 資料字典轉出的 PostgreSQL 建表 SQL：[`database/reference/mestest_first_phase_create_tables.sql`](database/reference/mestest_first_phase_create_tables.sql)。
- 這份 SQL 包含 75 張第一階段主檔相關表，以及 TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 三張系統選單/功能目錄表；目標 schema 為 YMES，不包含任何 MES 業務資料。
- 三張系統選單/功能目錄表的 MESTEST 原始 DDL 已查；Oracle 目錄清單、ModuleOrder、MenuItem、FunctionNo、FormName、ExecutionFile 見 docs/MES_MENU_FUNCTION_INVENTORY.md。遠端目前仍只 seed 五個模組，完整 20 模組 seed/API 需後續遷入。
- 本次另附作業站與系統選單來源 DDL/空表 CREATE SQL（18 張作業站相關表 + 3 張系統表），以及物料 8 張來源 DDL/空表 CREATE SQL，見 database/reference/mestest_operation_and_system_*、mestest_mtl_master_*。
- 另附即時 Oracle MESTEST DDL 轉換的供應商表 SQL：[設備供應商 CREATE SQL](database/reference/mestest_equipment_vendor_create.sql)，只含 tbleqpvendor、tbleqpvendorcontactor 結構，沒有業務資料。這兩表不在既有 75 表 SQL 內。
- [x] 2026-11-12 以家用電腦的 PPK SSH 連線，在遠端 YMES PostgreSQL 以單一交易執行此 SQL。確認原有 75 張主檔表已存在，這次建立 `tblsysmodule`、`tblsysmenuitem`、`tblsysfunction` 三張表；YMES schema 共 80 張表（78 張 MES `tbl*` 表及 2 張登入支援表）。
- 本次沒有匯入 MES 業務資料；`tblusruserbasis.password` 維持 `varchar(255)`，以保留現有 PBKDF2 雜湊，不縮回來源 DDL 的 `varchar(50)`。
- 此 SQL 尚未由自動 migration runner 管理。全新資料庫需先執行本檔，再依序執行 `database/migrations/0001` 至 `0004`。

## 優先一：選單、功能目錄與權限 API

- [x] 已依目前 MES Oracle MESTEST 原生 DDL 完成三張系統表的 PostgreSQL 結構轉換，保留欄位名稱、預設值、NULL 設定與主鍵。
- [x] 建立五個既有主檔 module 和功能項，分別對應 EQP/OP/MTL/PRS/PRD；前兩項由 `0008_initial_navigation_seed.sql` 建立，後三項由 `0009_expand_initial_navigation.sql` 增補並已套用遠端。
- [x] 公司端已由 PowerShell 5.1 直接查 MESTEST 系統目錄：20 Module、601 MenuItem、553 Function；518/519 個 MenuType=0 項目有相符 FunctionNo。完整清單與排序交付於 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司端另產生 `database/reference/mestest_navigation_catalog_seed.sql`，包含 20 Module、601 MenuItem、553 Function 的來源設定 upsert；不含業務資料、不刪除 YMES 既有列。家用電腦先比對現有表與路由，再整合為正式 migration，不直接手動執行。
- [ ] 先比對家用 YMES 的三張系統目錄表、目前五模組 migrations 與 20 模組 Oracle 來源資料；新增可重複執行的 seed migration 匯入其餘目錄項目，保留現有登入及權限設定。
- [x] 公司端已從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實全部 20 個 ModuleNo + Module 繁中顯示名稱；完整對照見 docs/MES_MENU_FUNCTION_INVENTORY.md。TBLSYSMODULE 本身沒有 ModuleName 欄位。
- [ ] 依來源 FunctionNo 對照完整 FormName/ExecutionFile；逐個遷移後才把對應功能導向 Vue/API，未遷移項目保留「尚未遷移」提示。
- [ ] 確認 MenuType=0 的 MenuNo 與 FunctionNo 關聯和 1 個未匹配項；DDL 沒有明確外鍵，需另外檢查停用/隱藏欄位與使用者權限規則。
- [x] `tblSYSModule` 沒有 `ModuleName` 欄位；API 將 EQP/OP/MTL/PRS/PRD 顯示為設備資料、作業站資料、物料資料、流程資料、產品資料。
- [x] 新增可重複執行的 `0008_initial_navigation_seed.sql` 與 `0009_expand_initial_navigation.sql`；正式選單包含五個既有主檔頁，0009 已套用 YMES 遠端資料庫。
- [x] 實作登入 Cookie 保護的 `GET /api/navigation/menu-tree`，讀取三張 YMES 系統表並回傳 `modules → menuGroups → items` 結構；目前依使用者指示先不套用角色/使用者權限篩選。
- [x] 新增並套用 `0005_navigation_read_access.sql`，僅授予 `ymes_app` 對三張選單表的 `SELECT` 權限。
- [x] API 以 MenuNo 對應 FunctionNo；五個既有主檔選單項均以 MenuNo 對應 FunctionNo。
- [ ] 匯入完整目錄後，確認各 ModuleNo、MenuNo、FunctionNo、順序與原 MES 一致；ModuleName 顯示使用核實後的翻譯表，不自建猜測值。
- [ ] 確認已完成的五個功能能由 FunctionNo 導向 Vue：`EQP BASIS`、`OP BASIS`、`MTL BASIS`、`PRS BASIS`、`PRD BASIS`；其他功能回傳識別資料，讓 UI 顯示尚未遷移狀態。

## 優先二：五個主檔的正式資料庫與 API

正式環境主檔 API 由 PostgreSQL provider 提供，Web 只透過 `/YMES/API` 呼叫。五個主檔 API 已覆蓋目前 Vue 頁面提供的 CRUD、核准/停用與產品流程/BOM 操作；舊系統擴充子表、批次匯入及完整 WinForms 業務規則仍需分批遷移。

- [x] 設備主檔 PostgreSQL repository 已部署：設備/Chamber CRUD、核准、停用、DataStamp 衝突控制；欄位依正式 DDL 對齊，移除不存在的「預設伺服器」輸入。
- [x] `0006_equipment_data_access.sql` 已套用，僅授予 `ymes_app` 對設備表的 CRUD 權限。遠端設備表目前沒有業務資料。
- [x] 作業站 CRUD、核准、停用 API 已部署，對應遠端實際存在的 `YMES.tblopbasis`；使用者先前提到 `tblopno`，但遠端 YMES schema 及提供的 CREATE SQL 都沒有此表。SPC 數量門檻欄位不在 `tblopbasis`，已從作業站頁移除，未新增資料庫欄位。
- [x] `0007_operation_data_access.sql` 已授予 `ymes_app` 作業站表 CRUD 權限；遠端作業站表目前沒有業務資料。

- [ ] 設備供應商 API/schema：依上述 SQL 建立兩張表及 migration；參照 docs/modules/EQP/設備設定/設備供應商基本資料.md 與 docs/DATABASE_CONNECTION.md；提供 GET /api/equipment/vendors、GET /api/equipment/vendors/{vendorNo}/contactors、供應商 POST/PUT/DELETE/approve、聯絡人 POST/PUT/DELETE。DTO 欄位依 Oracle DDL；刪除與核准採 DataStamp 衝突控制，供應商核准只允許 IssueState=0，刪除遵循 WinForm 只允許 0/-1/2，聯絡人不額外限制供應商狀態。
- [ ] 設備：`tbleqpequipmentbasis`、Chamber 關係、設備群/工程群組、設備屬性、參數、QC List；支援清單 CRUD、核准/停用、複製、匯入。
- [ ] 設備工程群組：完成 tbleqpengineergroupbasis API CRUD、DataStamp 衝突檢查、複製與成員清單 API；規格：docs/modules/EQP/設備設定/設備工程群組資料.md。
- [ ] 核實 YMES 是否已有 tblusrusergroup。它不在目前 75 表 bootstrap/migrations；若不存在，先由公司電腦以 MES Oracle MESTEST 唯讀資料字典取得精確原始 DDL，轉為 PostgreSQL CREATE SQL，再加入 migration。不得依欄位使用痕跡猜表結構。
- [ ] 成員 GET 回傳可配置使用者及 selected；PUT 以交易方式取代指定 EngineerGroupNo 的 GroupType=2 集合，只寫入 AgentOf='NA' 的直接成員；刪除群組時與刪除成員關聯同交易。
- [ ] 作業站延伸資料：群組、屬性、參數、QC、Error/Abnormal、Resource/Area、Business Rule、SubOP；支援匯入。
- [x] 物料頁面 API：使用 `tblmtlmaterialbasis`，支援清單、明細、新增、修改、核准、停用/刪除與資料版本衝突控制；引用物料群組或產品 BOM 時禁止物理刪除。
- [x] 流程頁面 API：使用 `tblprsprocessbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除及流程圖讀寫/驗證。因來源 Node 型態代碼尚未釐清，流程圖儲存在 YMES 自有 `ym_prsprocess_node`、`ym_prsprocess_edge`。
- [x] 產品頁面 API：使用 `tblprdproductbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除、產品流程指派及 BOM 讀寫。來源 DDL 沒有這些 UI 關聯表，YMES 自有 `ym_product_process`、`ym_product_bom` 保存新增資料。
- [ ] 物料主檔來源規格及 SQL 已交付：`docs/modules/MTL/物料主檔.md`、`database/reference/mestest_mtl_master_source_ddl.sql`、`database/reference/mestest_mtl_master_create.sql`。比對 YMES 現有 8 張表的欄位、NULL、DEFAULT、PK/索引後，再實作 Vendor Map、Property、QC Item、Gum Type API 與新增時範本複製交易。
- [ ] 遷移流程 APS 等擴充欄位/關聯及匯入功能，並與舊系統 Node 編碼及 XML 規則比對。
- [ ] 遷移產品 OP、屬性及其他舊系統關聯；跨表商業規則仍需對照 WinForms/Service 實作。
- [ ] 所有新增/修改/核准/刪除端點均驗證 IssueState、DataStamp、引用限制與舊系統業務規則；正式環境不得使用 Fake provider。
- [ ] 把五份主檔及產品流程整合的舊表欄位、PK/UK、型態、索引與正式 DDL 對齊；目前精簡資料字典中的推測欄位不可直接當正式 schema。

## 優先三：其餘第一層 Module 與功能

- [ ] 以正式 `tblSYSModule`/`tblSYSFunction` 選單清單作為盤點基準，為每個 FunctionNo 對照舊 WinForms FormName/ExecutionFile、WebService、核心類別、查詢/寫入資料表及權限。
- [ ] 依第一層 Module 分批新增 PostgreSQL schema/migration、Contracts DTO、API、交易/驗證與 Vue 功能頁；公司電腦先以假資料完成每個功能頁的查詢條件、欄位、按鈕、表單與互動；同批交付給家用電腦的 API 契約與依 Oracle 核實的 CREATE SQL 再由家用電腦完成正式 API/schema。
- [ ] 每一批完成後更新遷移清單：ModuleNo、FunctionNo、WinForms Form、API 路徑、Table、UI 狀態（未開始/進行中/完成）及驗收結果。
- [ ] 未完成的功能維持明確的「尚未遷移」狀態；沒有 API 支援的功能不得以靜態假資料回報成功。

## 優先四：整合與部署

- [x] Web/API 分開執行於同一台 Server：Web `/YMES`、API `/YMES/API`。同步修正遠端 Nginx 過期設定，主檔頁不再送到舊 18090 MVC 容器，改由目前 Vue Web 18089 提供。
- [x] Production 已使用 PostgreSQL provider；Development Fake 僅供本機預覽。
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
- [x] 2026-11-12 已套用 migration 0010、0011，重建 YMES API/Web，正式選單資料回傳三層：模組 → 設定 → 主檔功能。
- [x] 瀏覽器確認五個模組與對應主檔頁均能載入；設備、作業站、物料、流程、產品查詢 API 均回應正常，目前各表 0 筆。
- [x] 側欄收合時只顯示選單圖示；側欄展開後仍是三層結構，模組預設收合。
- [x] 首次線上檢查發現設備路由重複映射而回 500；移除重複映射後重建 API，設備頁重查正常。修正已推送至 GitHub `main`，commit `a080a3b`。
- [x] 五個主檔 Production PostgreSQL API 已部署；流程圖與產品流程/BOM 關聯使用 YMES 自有延伸表。
- [x] 設備與作業站新增表單空白送出都會擋下必填欄位。
- [ ] 正式區沒有業務資料可供修改、核准或刪除；本次未寫入測試資料，因此上述寫入流程尚未做資料庫端到端驗收。
- [x] API readiness、Web route 和遠端容器 health check 均回應正常；Nginx 已由過期的 18090 MVC 路由改為 Vue Web 18089。

## 設備設定：設備類別資料（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備類別資料.md、docs/DATABASE_CONNECTION.md 與下方 CREATE SQL，再開始 API/schema。連線憑證只從家用電腦既有安全設定取得，不寫入 Markdown/Git。

- [ ] 先查 YMES ymes schema 是否已存在下列五表；逐欄比對既有結構，未存在才執行 database/reference/mestest_equipment_type_create.sql：tbleqpequipmenttype、tbleqpequipmenttypeacctype、tbleqpequipmenttype_picture、tbleqpequipmentproperty、tbleqpconsumebasis。
- [ ] 本檔是公司電腦從 MES Oracle MESTEST 原始 DDL 轉換的 PostgreSQL 空表 CREATE SQL，沒有匯入業務資料。IF NOT EXISTS 不會修正已存在表的錯誤欄位。
- [ ] 等待公司電腦從 MES Oracle 唯讀資料字典查出配件類別目錄的真實表名、完整 CREATE DDL、欄位/PK/索引並交付 SQL；家用電腦再確認 YMES 是否已有此表並實作。不可依 DEV-ACC-* 假資料猜表或欄位。
- [ ] 核實 tblsyselementbasis 既有 YMES 結構與 MES Oracle DDL；目前 mestest_first_phase_create_tables.sql 中部分欄位有「推測」註記。依 WinForms ValueList 確認屬性選項與 PropertyType=1 查詢規則後，才實作 /api/system/elements?elementType=1。
- [ ] 實作設備類別清單/查詢、新增、修改、複製、刪除及核准 API。修改/核准/刪除使用 DataStamp；EDIT 僅 IssueState=0，核准僅 0，刪除只接受 0/-1/2，並沿用舊 Service 的引用檢查。
- [ ] 核實並實作 wsEQP.ApproveEquipmentType 對簽核 Job/Detail 的流程；無待簽工作可直接到 2，有簽核流程先到 1，再依結果到 2 或退回 0。
- [ ] ACC Type API：可選配件目錄查詢、既有關聯查詢、完整集合取代；在交易內同步 tbleqpequipmenttypeacctype。COPY 僅複製舊 clsEQPBasis.AddEquipmentType 明確複製的 ACC 關聯。
- [ ] Consume API：依規格驗證數量、容許量與週期關係，CRUD 以 (EquipmentType, ConsumeNo) 為鍵。
- [ ] Property API：依舊按鈕把設備類別編號映射至共用表 tbleqpequipmentproperty.equipment；預設查 IssueState=0；更新/刪除檢查 DataStamp。回傳屬性定義名稱與舊 ValueList 一致。
- [ ] 圖片庫 API：列舉、預覽（目前 Vue 契約為 { dataUrl }）、新增/更新 multipart pictureBody、刪除；確認被類別引用時拒絕刪除，限制 MIME/檔案大小並授權呼叫者。
- [ ] 匯入 API：讀取 WinForms .xls/.xlsx、執行 EquipmentTypeDataChecker 等價檢查、逐筆回傳結果；不得只回傳 HTTP 成功但略過失敗資料。
- [ ] 以正式 PostgreSQL migration 管理實際要新增的 schema/索引/權限；授予 ymes_app 最小必要權限，所有多表修改使用交易。Production 禁用 Fake provider。
- [ ] API 完成後由公司電腦連 Development 假資料 UI 做契約對接，再由家用電腦完成 API/資料庫驗證；同步回寫規格與本清單。不要提交連線字串、密碼、Token、私鑰。
## 設備設定：設備群組資料（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備群組資料.md、docs/DATABASE_CONNECTION.md 與 database/reference/mestest_equipment_group_create.sql。此 SQL 建立已由 MES Oracle MESTEST 核實的兩張空表，不含業務資料；執行前先核對 YMES schema 既有結構。

- [ ] 確認 YMES 已有 tbleqpgroupbasis、tbleqpgroupdetail；缺表才執行單表 CREATE SQL。第一階段 bootstrap 也已含兩表，Oracle 核實後的欄位來源註解已校正。
- [ ] 等待公司電腦查 MES Oracle 唯讀字典並交付 TBLSMDAREARELATION、TBLSMDSMTAREAEQPMAP 原始 DDL/PK/索引與 CREATE SQL；家用電腦再確認 YMES 目標表存在及權限。暫不建立推測 SQL。
- [ ] 實作設備群組清單、CRUD、核准/刪除 API：IssueState 0 才可修改；核准只允許 0，依 IssueNo EQP GROUP BASIS 啟動簽核；刪除只接受 0/-1/2，傳 DataStamp。
- [ ] 新增 API 必須等價執行 ChkObjectForSMDLayout(EquipmentGroup,4) 編號檢查；未確認規則前不要只做長度檢查。
- [ ] 實作群組明細與可加入設備端點。可加入清單限定 IssueState=2、ChamberFlag=0、尚未出現在任何 group detail；新增前由 API 再次以交易/鎖定檢查設備跨群組唯一性。
- [ ] 明細新增/刪除只允許父群組 IssueState=0；要同步 TBLSMDSMTAREAEQPMAP 的情況依 TBLSMDAREARELATION ObjectType=4 判斷。
- [ ] 核實舊 DelEQPGroupBasis 刪除關聯明細但未見刪 SMD 映射的既有行為；以公司 MES 現況確認孤兒映射處理方式，再決定新 API 同步清理規則。
- [ ] 授予 ymes_app 對兩個群組表的最小權限；若 SMD 映射 API 需要寫入，再依核實 DDL 補 CREATE SQL、migration 和權限。正式 API 不使用 Fake provider。
- [ ] 公司電腦完成 UI/API 契約對接後，回寫遷移規格及本清單；不要提交連線字串、密碼、Token、私鑰。## 設備設定：設備狀態設定（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備狀態設定.md 與 database/reference/mestest_equipment_state_create.sql。CREATE SQL 依 MES Oracle MESTEST 原始 DDL 轉為 PostgreSQL，只建立 ymes.tbleqpstatebasis 空表，不含 MES 業務資料。

- [ ] 確認 YMES ymes.tbleqpstatebasis 是否存在並逐欄比對；缺表才套用 database/reference/mestest_equipment_state_create.sql。第一階段 bootstrap 已補上 Oracle 確認過的 STATETYPE / UTILIZATIONTYPE DEFAULT 1 及欄位註解。
- [ ] 核對狀態選項 ValueList 的實際值與顯示文字；WinForms 透過 ValueListProvider 讀取 TBLEQPSTATEBASIS.STATETYPE 和 UTILIZATIONTYPE，不能只以程式註解推定完整選項。
- [ ] 實作 GET /api/equipment/states、GET /api/equipment/states/options、POST/PUT/DELETE /api/equipment/states/{equipmentState}；POST/PUT DTO 參照規格。PUT 在 stateColorHex 未提供時保留原值，對應 WinForms Apply 不更新 HEX。
- [ ] 刪除 API 必須實作舊 ChkEQPStateExist 規則：檢查設備現況狀態、TBLEQPSTATECONTROL 與 tblEQPQCListBasis.UNVALIDACTION；任何一處引用即回 409 並說明不可刪除。不得只依賴 FK。
- [ ] 確認 YMES 是否有 tblEMSEquipmentState、tbleqpstatecontrol、tbleqpqclistbasis 及欄位；若沒有，先確認 YMES 的實際對應表或 API 資料來源，不可直接從畫面推測新增替代欄位/表。只有經 MES Oracle 確認且 YMES 確實缺少、API 必需時，再交付精確 CREATE SQL/migration。
- [ ] 保留 WinForms 業務限制：狀態代碼為整數且非負；User Define（ValueList 索引 1）不可使用 0–10；名稱必填；Confirm 要求選 On/Off；Apply 未選時依舊邏輯送 0；ARGB 使用有號整數；Confirm 才帶 StateColor_HEX。
- [ ] 沒有 IssueState、DataStamp、核准流程；Delete 接受逐筆處理，並確認 API 錯誤能表達被引用狀態。
- [ ] 依規格完成 API 後，在公司電腦用 DEV 假資料 UI 對接 Production 契約；同步更新規格與本清單，不提交連線字串、密碼、Token 或私鑰。
## 設備設定：設備點檢表設定（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備點檢表設定.md 與 database/reference/mestest_equipment_qclist_create.sql。SQL 來自 MES Oracle MESTEST 原始 DDL，轉為 PostgreSQL 空表結構，不含 MES 點檢主檔/明細資料。

- [ ] 確認 YMES ymes.tbleqpqclistbasis、ymes.tbleqpqclistdetail 是否已存在；逐欄比對後，缺表才套用參考 SQL。第一階段 bootstrap 已按 Oracle DDL 補上 AFTEREVENTQC DEFAULT 0、DATASTAMP DEFAULT 0、INPUTDATACOUNT DEFAULT 1。
- [ ] 實作 GET /api/equipment/qc-lists（qcListNo 查詢、QCListNo 排序）、GET /api/equipment/qc-lists/{qcListNo}/items（QCOrder 排序）。
- [ ] 實作 GET /api/equipment/qc-lists/options；QCType 值/文字依舊 ValueListProvider，設備狀態選項依實際設備狀態主檔；-1 顯示不動作。
- [ ] POST /api/equipment/qc-lists 新增主檔及完整明細；PUT /api/equipment/qc-lists/{qcListNo} 使用 DataStamp 樂觀並行檢查並完整取代明細；DELETE 同交易刪除明細與主檔。任何多表寫入需由一個 PostgreSQL transaction 保證原子性。
- [ ] 伺服器驗證與 WinForms 一致：編號/名稱必填；編號與名稱長度 100；QCType、UnValidAction 必填；至少一項；QCOrder 依序且不超過 NUMBER(2,0)；CheckItem 不可重複且最多 1000；Type 0 StdValue 數字必填；Type 1 至少一個邊界、各自為數字、雙邊界時最大值大於最小值、空邊界存星號；InputDataCount 1–10；方法/標準最多 100。
- [ ] AfterEventQC 只允許 QCType=0；其他類型強制 0。DataStamp 不符回 409；欄位驗證回 400；不存在回 404。
- [ ] 確認既有 YMES 外鍵/點檢排程使用關係與 legacy delete 的影響；Oracle 原表 DDL 未宣告主從 FK，legacy Service 的 delete 會明確刪主檔和明細。
- [ ] Production API 完成後以公司電腦 DEV UI 對接；同步更新規格與本待辦，不能提交密碼、Token、連線字串或私鑰。

## 設備基本資料 EP104（公司電腦規格交接）

先讀 docs/modules/EQP/設備設定/設備基本資料.md 與 docs/modules/EQP/設備設定/設備參數設定.md。設備主表與參數表原始 DDL 已由使用者提供；單表 PostgreSQL CREATE SQL 已放在 database/reference/。

- [x] 公司端將使用者提供的 TBLEQPEQUIPMENTBASIS 原始 DDL 轉成 database/reference/mestest_equipment_basis_create.sql，並修正第一階段 bootstrap 的 DEFAULT/NOT NULL constraints，移除推測欄位註解。
- [x] 公司端將使用者提供的 TBLEQPPARAMETERBASIS 原始 DDL 轉成 database/reference/mestest_equipment_parameter_basis_create.sql，並修正第一階段 bootstrap 的 NOT NULL constraint names，移除推測欄位註解。
- [ ] 家用電腦比對 "YMES" schema 內 TBLEQPEQUIPMENTBASIS、TBLEQPPARAMETERBASIS 的欄位/NULL/DEFAULT/PK/index；缺表才套用 CREATE SQL，既有表若不符則建立 forward migration。不要在公司電腦連資料庫或執行 migration。
- [ ] 核對設備 CRUD/Approve、Chamber 共表資料、DataStamp、IssueState、刪除轉停用與 Controlled 屬性核准檢查，確保多表更新使用正確交易。
- [ ] 提供 EquipmentClass、設備類別、工程群組、供應商、點檢表及訊息佇列 lookup/options API；options 保留舊 ValueList 值和權限。
- [ ] 修正設備 DTO / write contract：回傳 Creator、Reviser；EqpRecipe 接受 0–3（WinForms ValueList 四種模式），目前 EquipmentWriteRequest 限制 0–1。
- [ ] 屬性、參數、設備點檢關聯、資料匯入分別依 WinForms 子功能規格實作 API，並核實其原始資料表 DDL。
- [ ] 設備屬性表 `TBLEQPEQUIPMENTPROPERTY` 已有 Oracle 來源單表 SQL：`database/reference/mestest_equipment_type_create.sql`；套用前比對 YMES 結構，API 保留 Equipment 可代表設備編號或設備類別編號的舊語意。
- [x] `TBLEQPEQUIPMENTBASIS`、`TBLEQPPARAMETERBASIS` 原始 DDL 已由使用者提供並轉成 PostgreSQL 單表 CREATE SQL；參照上一條核對 YMES schema。
- [x] `TBLEQPQCListSetup` Oracle DDL 已從公司端 `.equipment-settings-ddl.tmp.sql` 轉換至 `database/reference/mestest_equipment_qc_list_setup_create.sql`；套用前比對 YMES 既有表。
- [ ] 依設備參數規格提供全量 GET/PUT 與 options API；單交易取代整份清單，空陣列代表清空，驗證 ParameterName 唯一。
- [ ] 依設備點檢表關聯規格提供 GET/CRUD 及 QCList options；核對舊 Form 傳入 DataStamp 但 Service 編輯方法未接收該參數的併發行為。
- [ ] 依設備匯入規格提供 xls/xlsx 第一工作表解析、欄位/父子 Chamber 驗證及逐組結果 API；明確定義逐筆/整批交易與取消行為。
- [x] 公司端已把參數、設備點檢表關聯及設備匯入 Vue 子畫面接到 EP104；DEV 假資料只存 localStorage，Production 改呼叫 API，不使用 Fake provider。
- [ ] 對照公司電腦 Vue DEV UI 契約實作 API：設備參數 GET/PUT/options；設備點檢表關聯 GET/CRUD/options；設備匯入 POST 建立工作、GET 輪詢、POST 取消、GET xls/xlsx 範本，回傳逐組結果。完成後同步回寫規格與此清單。

## 作業站主檔 OP Basis（公司電腦交接）

先讀 `docs/modules/OP/作業站主檔.md` 與 `docs/DATABASE_CONNECTION.md`。公司端已從 WinForms Form、Designer、`clsOpBasis` 及 MESTEST Oracle 原始 DDL 追出主畫面規則、刪除引用範圍與 18 張作業站相關表結構。

- [x] 公司端已透過 PowerShell 5.1 + ODP.NET 呼叫 `DBMS_METADATA.GET_DDL`，來源及 PostgreSQL 空表 SQL 分別為 `database/reference/mestest_operation_and_system_source_ddl.sql`、`database/reference/mestest_operation_and_system_create.sql`。本批包含 TBLOPBASIS、TBLOPGROUPDETAIL 及服務引用的 16 張子表；同檔另附三張 TBLSYS* 原始 DDL。
- [ ] 家用電腦逐欄比對 YMES schema 的 18 張作業站表與 migration；缺表才套用 CREATE SQL，既有差異以 migration 修正。不要從 `mes_master_schema.sql` 推測欄位。
- [ ] 實作作業站 GET/POST/PUT/DELETE/approve API，驗證 OPNo 不含逗號、不等於 LOTCREATE、不可與 OPType 衝突；StdUnitRunTime/StdQueueTime >= 0、CountOpUnitQty >= 1；DataStamp 樂觀並行；沿用 IssueState=0/1/2/-1 的編輯/刪除/停用規則。
- [ ] Delete 先檢查 `TBLOPGROUPDETAIL` 引用並回 409；交易內照 `clsOpBasis.DelOPBasis` 確認各子表刪除順序與條件，再刪主檔，不能只刪 `tblopbasis`。
- [ ] Approve 實作 CPass 屬性檢查、`LC_CO_MEASURE` 規則限制、`L_SPC_GEN`/`L_SPC_FAI`/`L_SPC_IPQC` 對應 QC Item 提示及舊簽核 Job/Detail 行為；補齊由原始碼/Oracle 確認的測試案例。
- [ ] 依規格為 Resource、Business Rule、Parameter、QC Item、Error、Copy OP Data、SubOP、Import 各建 API/DTO；多表更新使用交易。精確 endpoint 命名可依現有 YMES API 慣例調整並回寫本規格。
- [ ] ValueList 的 MaterialOption、MultiOperatorMode、OSOption 從 MES 原始選項資料確認，不把程式註解中的舊選項清單直接當正式值。
- [ ] Production API 完成後，公司電腦將 OP 子功能 DEV 假資料畫面接到正式 API 契約並回寫規格；Production 不可使用 localStorage Fake provider。

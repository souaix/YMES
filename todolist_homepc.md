# 家裡電腦接手清單

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

> **本次接手身分：家用電腦。** MES 原生來源是公司端唯讀查詢的 Oracle MESTEST；本端要復刻的是 YMES PostgreSQL，透過桌面 PPK/SSH 核對、建表、migration、開發 API 與部署。`database/reference/*_create.sql` 是由原生 MES DDL 轉換的 PostgreSQL 建表結構，只能用於 YMES 復刻資料庫，不在 Oracle 執行，也不含 MES 業務資料。公司電腦只做 UI 並呼叫 `/YMES/API`，不能連 YMES PostgreSQL、SSH 或上版。不要因 API 應用帳號權限不足而把工作角色改成交由公司電腦；由家用端 SSH 使用資料庫管理權限完成。

## 家用電腦現在接手

公司端已整理一批可直接接手的來源結構與目錄資料。開始前先 pull 這份清單所在的最新 `main`，並閱讀 `todolist_officepc.md`、`docs/DATABASE_CONNECTION.md`、`docs/MES_MENU_FUNCTION_INVENTORY.md`、OP/MTL 規格及下列 SQL。家用端只負責 DB/API/migration；公司端後續接回 Vue/WinForms 畫面。

1. **系統目錄 migration 0012 已完成**：以 SSH 比對來源 schema，部署可回滾/可重跑 migration，載入 20 Module、601 MES MenuItem、553 Function 並保留 5 筆 YMES 自訂列。模組繁中名稱已核實、API/Web 已部署，瀏覽器已確認三層選單；逐筆自動比較來源與 API 回傳仍待補。
2. **物料 API/schema 主體已完成**：已逐欄核對 8 張表並套用 0013 migration，保留 `tblmtlmaterialbasis` 的 7 個 YMES 欄位、補回 7 個 DEFAULT，建立缺少的 6 張表。Property、QC Item、Vendor Map、GumHour、JSON Import 及主檔交易 API/Vue Production 已部署；app-browser 找到並修正控制器 DI、`tblsysunitbasis`/`tblsyselementbasis` 唯讀授權及投料點 JSON 型別問題，瀏覽器已驗證主檔新增、屬性、QC、GumHour 讀寫。0014 授予必要唯讀權限。公司 Oracle 端仍需核實正式 ValueList、供應商名稱 Join、GumHour 參數來源；CSV/JSON 提交端到端、正式 Excel 範本、`.xls` 預覽及 WinForms 群組寫入規則仍待完成。
3. **完成作業站延伸 API/schema**：比對 `database/reference/mestest_operation_and_system_create.sql` 內 18 張 OP 表；此批 18 張 OP 表及 3 張系統表的原始 Oracle DDL 在 `mestest_operation_and_system_source_ddl.sql`。YMES 目前作業站僅主檔 CRUD/核准/停用已部署；按 `docs/modules/OP/作業站主檔.md` 實作子表引用、資源、商業規則、參數、QC、Error、SubOP、Import 及交易。
4. **繼續設備功能 API**：設備供應商已完成；設備狀態 GET/options/POST/PUT/DELETE API 與 0016 migration 已部署。公司端已交付兩張來源引用表 DDL、空表 CREATE SQL 與完整繁中 ValueList；家用端需新增 migration、啟用引用檢查並更新 options 標籤。工程群組成員表 TBLUSRUSERGROUP 及設備群組用的兩張 SMD 映射表 Oracle DDL/PG CREATE SQL 已於 2026-11-16 交付；先比對 YMES schema，再完成工程群組與設備群組 API。其他類別、點檢表功能依規格續做。
5. **完成全部第一層模組**：按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 的 ModuleOrder 逐個 FunctionNo/FormName/ExecutionFile，補齊 PostgreSQL 結構、API DTO/驗證/交易及部署。任何來源表缺少 DDL 時，讓公司端透過 PowerShell 直接查 MESTEST 並列出確切表名；不可猜 schema。

每完成一項，更新本檔 checkbox、對應 module spec 與 migration/API 狀態，再 commit/push；不要把連線密碼、Token、SSH 私鑰或 Oracle 連線字串放入文件或 Git。

## SMD SD104 區域基本資料（公司端 2026-11-16 已交接）

- [x] 2026-11-16 家用端比對 YMES：`TBLSMDAREABASIS` 已存在但四項來源 DEFAULT 缺漏；`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP`、`TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 缺表。已透過 `0017_smd_computer_area_api.sql` 補回 DEFAULT 並建立四張空表；未匯入 MES 業務資料。SD104 Production CRUD/layout API 仍待完成。

先讀 `docs/modules/SMD/區域基本資料.md`、`todolist_officepc.md` 的 SD104 區塊及以下 Oracle reference。公司端已有 `/YMES/SMD/Areas` Vue DEV 頁與 SMD 三層 fallback 選單；Production 不使用 localStorage 假資料。

### Schema / migration

- 新查 Oracle 主表 `TBLSMDAREABASIS`、`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP` 的 source DDL/PG 空表 SQL：`database/reference/mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`。
- 區域配置另外使用 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP`；其 Oracle DDL/CREATE SQL 在 `database/reference/mestest_equipment_group_smd_source_ddl.sql`、`mestest_equipment_group_smd_create.sql`。先比對 YMES 現況，保留已存在的 YMES 擴充，不重建重複表。
- `TBLSMDAREABASIS` 16 欄、PK AreaNo；AreaType/Class/IssueState/AreaCompose DEFAULT 0 NOT NULL；BackgroundImage BLOB；DataStamp DEFAULT 0 nullable。`TBLSMDAREAINVENTORY` 9 欄、PK `(AreaNo,InventoryNo)`；DefaultInv DEFAULT 0 NOT NULL、DataStamp DEFAULT 0 nullable。`TBLSMDAREAMAP` 6 欄、PK `(BaseAreaNo,ParentAreaNo,ChildAreaNo)`。三表只有 PK unique index，沒有 FK/其他 index。完整欄位見規格與 reference SQL。
- Oracle DATE → PostgreSQL timestamp without time zone；BLOB → bytea。WinForms 表單要求 DepartmentNo 必填，但 Oracle 欄位允許 NULL；資料庫欄位仍照 Oracle，API 做業務驗證。

### API / Service handoff

1. 建 SMD API 並檢查登入者 SMD 權限：`GET /api/smd/areas?issueState=&areaNo=`、`GET /api/smd/area-basis/options`、`POST /api/smd/areas`、`PUT/DELETE /api/smd/areas/{areaNo}`、`POST /{areaNo}/approve`、`GET/PUT /{areaNo}/layout`、`GET /{areaNo}/layout-options`。JSON camelCase DTO 與 query/body 欄位以功能規格和 `AreaBasisMaster.vue` 為準；layout PUT 回更新後完整 Area DTO/DataStamp。
2. 新增只接受有效的區域類型組合；AreaNo 長度20、AreaName 50、Description 255；編號大寫。AreaNo/AreaName/FactoryNo/Creator/CreateDate 在 DB NOT NULL；DepartmentNo DB nullable 但 UI/API required。一般區域 BaseAreaNo 必填且必須屬於 active Base Area，FactoryNo 從 Base Area 繼承；Base Area 必須選 active Factory。Factory/Department options 不得使用 DEV 假資料。
3. Create 設 IssueState=0、稽核與 DataStamp；Edit 僅 state 0，以 DataStamp compare-and-swap 更新；NULL DataStamp 也要正確 CAS，遞增 `COALESCE(DataStamp,0)+1`。Delete state 0/-1 實體刪除；2 改 -1；1 回 409。實體刪除要 transaction 檢查 `TBLSMDAREARELATION.ContainAreaNo`，若仍有 layout relation 要拒絕。多筆 UI 逐筆請求，錯誤回報完成筆數。
4. Approval 僅接受 state 0，IssueNo=`AREA BASIS`，重用 YMES Issue/Job service：無 Job/Detail 依 WinForms 規則直達 2；有設定轉 1，核准2/退回0。Production 不得照 Vue DEV 直接轉狀態。
5. Layout 必須讀舊 `frmAreaLayout.vb`、`frmNodeDef.vb` 與 `funSaveAreaData`，在 PostgreSQL 同一 transaction 處理 `TBLSMDAREARELATION`、`TBLSMDAREAMAP`、`TBLSMDAREAINVENTORY`、`TBLSMDSMTAREAEQPMAP` 及 `TBLSMDAREABASIS.BackgroundImage`。從節點推導 parent/child area map、top production line、inventory mapping、equipment/equipment group mapping，驗證 ObjectType、唯一鍵、節點尺寸/座標/顏色與 BaseArea 關聯。保存 BaseArea DataStamp 並回傳更新後完整 DTO，前端下一次操作才不會拿舊 stamp。
6. WinForms AddFlow 支援多選/群組、Node Define、完整線型巢狀推導；公司端第一版簡化成可拖曳/數值調整節點與背景圖，尚未聲稱完整 AddFlow 等價。需按來源 Service 完成 API 邏輯並記錄無法逐一等價的部分。`ymes_app` 只授予所需 SELECT/INSERT/UPDATE/DELETE。
7. 建 migration 前逐欄比對 PostgreSQL schema；缺表才新增。完成後部署 API、更新 migration/API 文件，再通知公司端 pull 接正式 API 驗收。公司端 DEV 還未執行 build/test/browser。

## SMD SD105 區域庫房基本設定（公司端 2026-11-16 已交接）

- [x] `TBLSMDAREAINVENTORY` 已由 `0017_smd_computer_area_api.sql` 建立空表，欄位長度/複合 PK 依來源 DDL；SD105 CRUD API 尚待完成。

先讀 `docs/modules/SMD/區域庫房基本設定.md` 及上方 SD104 handoff。公司端已有 `/YMES/SMD/AreaInventories` Vue DEV 頁，FunctionNo=`AREA INV MAP`；沒有 IssueState/核准。

- Oracle `TBLSMDAREAINVENTORY` 已由公司端 SD104 直連查 DDL，來源 logical DDL/PG 空表 CREATE 已包含在 `database/reference/mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`。不要重複建表；先比對 YMES schema/migrations，缺表才新增。
- Table PK 是 `(AreaNo,InventoryNo)`，不是 `(BaseAreaNo,AreaNo,InventoryNo)`；BaseAreaNo 是額外範圍條件。沒有 Oracle FK/其他 index。DefaultINV DEFAULT 0 NOT NULL；DataStamp DEFAULT 0 nullable。完整欄位見來源 SQL。
- 延伸 SMD API：`GET /api/smd/area-inventories?baseAreaNo=`、`POST /api/smd/area-inventories`、`PUT /{areaNo}/{inventoryNo}?dataStamp=`、`DELETE /{areaNo}/{inventoryNo}?baseAreaNo=&dataStamp=`。Base Area 選項沿用 `GET /api/smd/area-basis/options`；Area/Inventory picker 使用 `GET /api/smd/areas/{baseAreaNo}/layout` 中 ObjectType 0/1 nodes。
- Create 必填 BaseAreaNo/AreaNo/InventoryNo；DefaultINV 僅 0/1；伺服器產生 Creator/CreateDate/DataStamp。Edit 不改 key，只改 DefaultINV，更新 Reviser/ReviseDate 並做 DataStamp CAS（支援 NULL）；Delete 交易內比較 DataStamp 後移除。
- Default=Yes 每一 Area 只能一筆。舊 Add 檢查 BaseAreaNo+AreaNo；舊 Edit 的 `GetAreaInventory` 未帶 BaseAreaNo，可能跨 BaseArea 影響相同 AreaNo。規格已記錄來源差異；實作時保留觀察行為，或在 API change log 明確列出核准的修正。
- wsSMD Add/Edit 呼叫傳 Creator/CreateDate/DataStamp/Reviser/ReviseDate，但本 checkout 的 clsSMDBasis 方法簽章沒有這些命名參數。新 API 需明確由伺服器產生稽核/CAS，不要假設舊 Core SQL 已寫入這些欄位。所有 API 檢查登入與 SMD 權限，`ymes_app` 只授最小 CRUD。
- DDL 已從 2026-11-16 直接 metadata query 交付，不必重查 Oracle。schema/API/部署完成後通知公司端 pull 驗收。

## SMD SD106 過帳電腦之區域（公司端 2026-11-16 已交接）

- [x] 2026-11-16 遠端 schema 核對：兩張主明細表已存在且欄位、NULL、型態、PK 符合來源；僅 `DataStamp DEFAULT 0` 漏失。`0017_smd_computer_area_api.sql` 已補 DEFAULT、建立必要 SMD 空表，並授予 `ymes_app` 對 SD106 主明細 CRUD 與 Factory/Area lookup SELECT。備份為 `/opt/ymes/backups/ymes_before_0017_smd_20261116.dump`，mode 600，`pg_restore --list` 驗證可讀。
- [x] SD106 API（查詢、Create、Edit CAS/整組替換明細、Delete、Copy、已簽核工廠與區域 options）已部署至遠端獨立 API 容器；.NET 9 build 成功，`/health/ready` 回 200，未登入 API 路由回 401。登入後 app-browser 讀取與 Production UI 驗收待完成。
- [ ] SMD API 目前僅要求登入；依使用者指示，角色／功能權限暫不實作。PostgreSQL `ymes_app` 的表級權限另按每個端點實際需要最小授權。

先讀 `docs/modules/SMD/過帳電腦之區域.md`。公司端已完成 `/YMES/SMD/ComputerAreas` Vue DEV 頁；Production 僅呼叫 API。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_smd_computer_area_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_smd_computer_area_create.sql`。來源是公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀查詢，不需再問 Oracle 連線資訊，也不要重查。
- `TBLSMDCOMPUTERAREABASIS` 欄位：ComputerNo varchar(20) NOT NULL、FactoryNo varchar(20) NOT NULL、Creator varchar(10)、CreateDate timestamp、Reviser varchar(10)、ReviseDate timestamp、DataStamp numeric(11,0) DEFAULT 0 nullable；PK `(ComputerNo,FactoryNo)`。
- `TBLSMDCOMPUTERAREADETAIL` 欄位 ComputerNo/FactoryNo/AreaNo varchar(20) 均 NOT NULL；PK `(ComputerNo,FactoryNo,AreaNo)`。兩表只有 PK unique index，沒有 Oracle FK/額外 index。不要新增來源不存在的 FK/index。
- 先逐欄比對 YMES schema、migration 及現有 API；表可能已在 first-phase schema，缺表/欄位才新增 forward migration，保留 YMES 擴充欄位。first-phase reference 已按來源補 ComputerArea DataStamp DEFAULT 0；不要把空表 SQL 當作可直接上線 migration。

### API / 業務規則

1. SMD API：`GET /api/smd/computer-areas?computerNo=&factoryNo=` 回傳主檔及 `areas: string[]`；`POST /api/smd/computer-areas` 新增主檔與區域明細；`PUT /api/smd/computer-areas/{computerNo}/{factoryNo}?dataStamp=` 依 CAS 更新稽核並整組替換明細；`DELETE /{computerNo}/{factoryNo}` 同交易先刪明細再刪主檔；`POST /computer-areas/copy` body `{fromComputerNo,fromFactoryNo,toComputerNo}` 以複合鍵複製。
2. 新增/修改至少選一個 Area。Factory 必須為 IssueState=2；Area 必須屬於該 Factory 且 IssueState=2、AreaClass=0。選項可沿用 `GET /api/smd/area-basis/options` 及 `GET /api/smd/areas?factoryNo=&issueState=2&areaClass=0`。其中 `factories` 選項需含 `factoryNo, description`（並保留 SD104 使用的 `factoryName`）；WinForms 查找視窗顯示 FactoryNo/Description。所有端點驗證登入與 SMD 權限及欄位長度。
3. Add/Copy 由 API 設 Creator/CreateDate；Edit 設 Reviser/ReviseDate/DataStamp。DataStamp nullable/default 0，CAS 要處理 NULL 並以 `COALESCE(datastamp,0)+1` 遞增。主檔與全部明細新增/修改/刪除/複製都要 transaction。
4. 舊 Core 的 CopyComputerArea 只按 FromComputerNo 查來源，未指定 FactoryNo；舊資料表是複合 PK。新 API 以 `(fromComputerNo,fromFactoryNo)` 明確鎖定來源，拒絕重複目標鍵。
5. wsSMD Edit 傳入 DataStamp，但本 checkout 的 `clsSMDBasis.EditComputerArea` 簽章沒有此參數，舊 Core SQL 沒有 CAS。新 API 補足 CAS/audit，不照搬舊缺漏。UI Delete 多選逐筆請求，首錯停止並回報已完成筆數。
6. DDL 與 API 規格已交付；家用端完成 schema/migration/API 並部署後通知公司端 pull，對接 Production 驗收。

## SMD SD101 工廠基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/SMD/工廠基本資料.md` 及公司端 SD101 區塊。公司端已完成 `/YMES/SMD/Factories` Vue DEV 頁，FunctionNo=`FACTORY BASIS`；Production 只呼叫 API。

- Oracle `TBLSMDFACTORYBASIS` 已由公司端在 2026-11-16 EP303 DDL 查詢中直連查得。Oracle source logical DDL、PostgreSQL 空表 CREATE SQL 已在 `database/reference/mestest_carrier_basis_dependencies_source_ddl.sql`、`mestest_carrier_basis_dependencies_create.sql`，first-phase bootstrap 已含表；不要重查或建立重複 CREATE SQL。
- 欄位：FactoryNo varchar(20) NOT NULL PK、Description varchar(255)、Creator varchar(10) NOT NULL、CreateDate timestamp NOT NULL、IssueState numeric(1,0) DEFAULT 0 NOT NULL、FabCode varchar(10)、FactoryName varchar(50)、Reviser varchar(10)、ReviseDate timestamp、DataStamp numeric(11,0) DEFAULT 0 nullable。只有 PK unique index，沒有 Oracle FK/額外 index。
- 先比對 YMES schema/migrations。缺表或欄位才建 forward migration；保留 YMES 擴充欄位。DATE 對應 timestamp without time zone。
- API：`GET /api/smd/factory-basis?factoryNo=`（大小寫不敏感包含查詢、空值回全部）、`GET /api/smd/factory-basis/options` 回 `{factoryNameEnabled}`、POST `/api/smd/factory-basis`、PUT `/{factoryNo}?issueState=0&dataStamp=`、DELETE `/{factoryNo}?issueState=&dataStamp=`、POST `/{factoryNo}/approve` body `{issueState:0,dataStamp}`。DTO/回應依規格與 `FactoryBasisMaster.vue`。
- CRUD 權限需登入與 SMD 權限；FactoryNo 必填唯一。只允許 IssueState=0 修改；DataStamp nullable 要 CAS 並以 COALESCE(DataStamp,0)+1 更新。伺服器產生 Creator/CreateDate/Reviser/ReviseDate。Delete 僅狀態 0、2、-1 可實體刪除，狀態 1 回 409。
- Approve IssueNo=`FACTORY BASIS`：沒有 Job/Detail 時依設定直達 2；有工作流程時建立 Issue Master 並轉 1，之後核准 2/退回 0。Production 不得使用 DEV 的直接轉狀態邏輯；家用端需重用正式 Issue/Job API。
- `options.factoryNameEnabled` 依正式 `IC_FT_PKG` 設定；主 Grid 只有啟用時顯示 FactoryName，定義表單仍有欄位。舊 WinForms 清空 FactoryName 不會清除資料，Vue 延續此語意。
- 舊 ws/Core 有不同版本的 DataStamp/audit 方法簽章；實作以規格明確定義的 CAS/登入稽核為準，不沿用舊簽章差異。
- schema/API/Issue 流程完成並部署後，更新本待辦及 API 文件，通知公司端 pull 對接 Production。

## SMD SD103 儲位基本資料（公司端 2026-11-16 已交接）

先讀 docs/modules/SMD/儲位基本資料.md。公司端完成 /YMES/SMD/Locators Vue DEV 頁與 FunctionNo=LOCATOR BASIS。Oracle 原始 DDL 和 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_locator_basis_source_ddl.sql、mestest_smd_locator_basis_create.sql；first-phase bootstrap 已校正。

- Oracle table 為 TBLINVLOCATORBASIS，複合 PK (InventoryNo, LocatorNo)，無 FK/額外索引。欄位、NULL/default 以 CREATE SQL 為準；IssueState DEFAULT 0 NOT NULL，Capacity NUMBER(12,4) DEFAULT 0 nullable，DataStamp NUMBER(11,0) DEFAULT 0 nullable。
- 先查目前 YMES schema/migration 狀態。舊 first-phase SQL 已在家用端執行時，新增 forward migration 修正 IssueState、Capacity、DataStamp 預設值；不刪資料、不重建表、不加 Oracle 沒有的外鍵。
- API：GET /api/smd/locator-basis?inventoryNo= 以 InventoryNo 完全相等比對，回傳四狀態並按複合鍵排序；GET /api/smd/locator-basis/options 回 { approvedInventories: [{ inventoryNo }] }，由既有 TBLINVInventoryBasis 篩 IssueState=2、distinct InventoryNo。POST /api/smd/locator-basis；PUT /api/smd/locator-basis/item?inventoryNo=&locatorNo=&issueState=0&dataStamp=；DELETE 同路徑並帶實際狀態/DataStamp；POST /api/smd/locator-basis/item/approve?inventoryNo=&locatorNo=&issueState=0&dataStamp=。
- POST 設 Creator/CreateDate、IssueState=0。PUT UI 只允許狀態 0，鍵不可改；空白 LocatorName/Capacity/Description 在舊 WinForms 代表不送欄位，故 Edit 不清除舊值。Capacity 必須 >=0，最大 NUMBER(12,4) 範圍。
- DELETE 狀態 0/-1 實體刪除、狀態 2 轉 -1 並更新 Reviser/ReviseDate/DataStamp、狀態 1 回衝突。Approve IssueNo=INV LOCATOR；無 Job/Detail 時直達 2，有流程時先 1，後續核准 2/退回 0。多筆 UI 逐筆請求，第一個失敗即停止。
- 所有 API 驗證登入及 SMD 權限；DataStamp nullable 必須 CAS，以 COALESCE(datastamp,0)+1 遞增。若家用端已擴充 YMES 欄位，保留擴充內容。
- 公司端 Production 頁只呼叫 API；家用端 schema/API 完成並部署後通知公司端 pull 對接。沒有把 DEV mock 當成正式流程。

## 公司電腦目前交付

- Vue 主選單以三層 Module → Menu Group → Function 呈現；正式資料 API 不得把設定分類與主檔功能壓成兩層。
- UI 使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。
- 2026-11-14 已套用 0012 migration，YMES 正式目錄匯入 MES 來源 20 個 Module、601 筆 MenuItem、553 筆 Function，保留 5 筆 YMES 既有分類列；全量模組中文名稱 API/Web 已部署，app-browser 已確認三層選單可載入。完整來源目錄見 docs/MES_MENU_FUNCTION_INVENTORY.md。
- 每個選單項目都有對應圖示；側欄收合時僅顯示圖示。
- 選單中尚未遷移的 FunctionNo 會明確標示，避免使用者誤以為功能已可操作。
- Vue 開發用設備設定選單已依 WinForm 補列七個項目；設備設定的 Oracle MenuNo/FunctionNo 已可由完整來源目錄核對，尚待逐功能整合至正式 API。供應商及工程群組 Vue 頁使用 Development 假資料，功能規格入口：docs/MODULE_REPLICATION_SPEC_INDEX.md。
- 公司電腦先以 Development 本地假資料完成 WinForms UI 與互動，不呼叫假 API；假資料頁會顯示提示並存於瀏覽器 localStorage。Production 僅呼叫正式 API，不能啟用假資料。

## MES 資料庫 CREATE SQL

- 已附實際 MES Oracle `MESTEST` 資料字典轉出的 PostgreSQL 建表 SQL：[`database/reference/mestest_first_phase_create_tables.sql`](database/reference/mestest_first_phase_create_tables.sql)。
- 這份 SQL 包含 75 張第一階段主檔相關表，以及 TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 三張系統選單/功能目錄表；目標 schema 為 YMES，不包含任何 MES 業務資料。
- 三張系統選單/功能目錄表的 MESTEST 原始 DDL 已查；Oracle 目錄清單、ModuleOrder、MenuItem、FunctionNo、FormName、ExecutionFile 見 docs/MES_MENU_FUNCTION_INVENTORY.md。2026-11-14 已透過 0012 migration 載入完整目錄；YMES 另保留 5 筆不屬於 Oracle source seed 的舊選單列。
- 本次另附作業站與系統選單來源 DDL/空表 CREATE SQL（18 張作業站相關表 + 3 張系統表），以及物料 8 張來源 DDL/空表 CREATE SQL，見 database/reference/mestest_operation_and_system_*、mestest_mtl_master_*。
- 另附即時 Oracle MESTEST DDL 轉換的供應商表 SQL：[設備供應商 CREATE SQL](database/reference/mestest_equipment_vendor_create.sql)，只含 tbleqpvendor、tbleqpvendorcontactor 結構，沒有業務資料。這兩表不在既有 75 表 SQL 內。
- [x] 2026-11-12 以家用電腦的 PPK SSH 連線，在遠端 YMES PostgreSQL 以單一交易執行此 SQL。確認原有 75 張主檔表已存在，這次建立 `tblsysmodule`、`tblsysmenuitem`、`tblsysfunction` 三張表；YMES schema 共 80 張表（78 張 MES `tbl*` 表及 2 張登入支援表）。
- 本次沒有匯入 MES 業務資料；`tblusruserbasis.password` 維持 `varchar(255)`，以保留現有 PBKDF2 雜湊，不縮回來源 DDL 的 `varchar(50)`。
- 此 SQL 尚未由自動 migration runner 管理。全新資料庫需先執行本檔，再依序執行 `database/migrations/0001` 至 `0004`。

## 優先一：選單、功能目錄與權限 API

- [x] 已依目前 MES Oracle MESTEST 原生 DDL 完成三張系統表的 PostgreSQL 結構轉換，保留欄位名稱、預設值、NULL 設定與主鍵。
- [x] 建立五個既有主檔 module 和功能項，分別對應 EQP/OP/MTL/PRS/PRD；前兩項由 `0008_initial_navigation_seed.sql` 建立，後三項由 `0009_expand_initial_navigation.sql` 增補並已套用遠端。
- [x] 公司端已由 PowerShell 5.1 直接查 MESTEST 系統目錄：20 Module、601 MenuItem、553 Function；518/519 個 MenuType=0 項目有相符 FunctionNo。完整清單與排序交付於 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司端另產生 `database/reference/mestest_navigation_catalog_seed.sql`，包含 20 Module、601 MenuItem、553 Function 的來源設定 upsert；不含業務資料、不刪除 YMES 既有列。家用端依實際 schema 與五個既有 Vue 路由整合為 0012 migration，另附 rollback。
- [x] 2026-11-14 以 SSH 核對 YMES 遠端實際為 5 Module/10 MenuItem/5 Function；三張表欄位與來源 DDL 相符。migration 前建立並驗證備份 `/opt/ymes/backups/ymes_before_0012_full_navigation_20261114.dump`。
- [x] 0012 migration 可重跑；在交易中鎖定三張目錄表、快照將被覆寫的來源列、記錄由 migration 新增的來源鍵，保留來源外 YMES 列。`0012_full_navigation_catalog_rollback.sql` 可還原 migration 前列並移除 migration 新增列。
- [x] 2026-11-14 以 PostgreSQL 管理者 SSH 套用 0012；目前 `tblsysmodule=20`、`tblsysmenuitem=606`（MES 來源 601 + 保留的 YMES 既有 5）、`tblsysfunction=553`；來源 MenuType=0/1 分別為 519/82，另有 5 筆保留的 YMES 分類列。
- [x] 2026-11-14 以 SSH 核對物料 8 張來源表：現有主表及群組明細相符，缺少其餘 6 張。先備份 `/opt/ymes/backups/ymes_before_0013_mtl_20261114.dump`，再套用 `0013_material_master_schema.sql`。驗證 8 張來源表齊全；主表 7 個預設值均為 0，原 7 個 YMES 擴充欄位保留。
- [x] 公司端已從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實全部 20 個 ModuleNo + Module 繁中顯示名稱；完整對照見 docs/MES_MENU_FUNCTION_INVENTORY.md。TBLSYSMODULE 本身沒有 ModuleName 欄位。
- [ ] 依來源 FunctionNo 對照完整 FormName/ExecutionFile；逐個遷移後才把對應功能導向 Vue/API，未遷移項目保留「尚未遷移」提示。
- [x] 核對 519 個來源 MenuType=0 項目與同 ModuleNo 的 FunctionNo：518 筆相符、1 筆無對應 Function（`PRD / PD_BatchModifyArea`，來源名稱 `XX_PRDBatchModifyArea_THEIL`）。API 保留此來源識別，UI 以尚未遷移狀態呈現；5 筆 YMES 舊分類沒有來源子項，不會出現在 menu-tree。
- [x] 三張 Oracle DDL 沒有 MenuItem→Function 外鍵；目錄保留 `ActiveModule`、`PrivOption`、`IssueOption`、`ExecutionMode` 等來源值。依使用者指示，menu-tree 目前只要求登入、不套用使用者/角色篩選；migration 不修改既有 `weburl`、`privoption`、`issueoption`，不新增/修改角色或登入資料。
- [x] `tblSYSModule` 沒有 `ModuleName` 欄位；API 依 MES 資源檔對應 20 個 ModuleNo 的繁中名稱，完整 mapping 位於 `NavigationRepository`。
- [x] 新增可重複執行的 `0008_initial_navigation_seed.sql` 與 `0009_expand_initial_navigation.sql`；正式選單包含五個既有主檔頁，0009 已套用 YMES 遠端資料庫。
- [x] 實作登入 Cookie 保護的 `GET /api/navigation/menu-tree`，讀取三張 YMES 系統表並回傳 `modules → menuGroups → items` 結構；目前依使用者指示先不套用角色/使用者權限篩選。
- [x] 新增並套用 `0005_navigation_read_access.sql`，僅授予 `ymes_app` 對三張選單表的 `SELECT` 權限。
- [x] API 以 MenuNo 對應 FunctionNo；五個既有主檔選單項均以 MenuNo 對應 FunctionNo。
- [ ] 以 menu-tree API 逐筆核對各 ModuleNo、MenuNo、FunctionNo、順序與原 MES 完全一致；瀏覽器已確認 20 個模組與三層樹可載入，API 目錄筆數待自動比對。ModuleName 顯示使用已核實的完整翻譯表。
- [x] 2026-11-14 以 app-browser 逐一開啟五個既有 Vue 路由並確認頁面/API 無錯誤：`EQP BASIS`、`OP BASIS`、`MTL BASIS`、`PRS BASIS`、`PRD BASIS`；其他功能依舊顯示尚未遷移。

## 優先二：五個主檔的正式資料庫與 API

正式環境主檔 API 由 PostgreSQL provider 提供，Web 只透過 `/YMES/API` 呼叫。五個主檔 API 已覆蓋目前 Vue 頁面提供的 CRUD、核准/停用與產品流程/BOM 操作；舊系統擴充子表、批次匯入及完整 WinForms 業務規則仍需分批遷移。

- [x] 設備主檔 PostgreSQL repository 已部署：設備/Chamber CRUD、核准、停用、DataStamp 衝突控制；欄位依正式 DDL 對齊，移除不存在的「預設伺服器」輸入。
- [x] `0006_equipment_data_access.sql` 已套用，僅授予 `ymes_app` 對設備表的 CRUD 權限。遠端設備表目前沒有業務資料。
- [x] 作業站 CRUD、核准、停用 API 已部署，對應遠端實際存在的 `YMES.tblopbasis`；使用者先前提到 `tblopno`，但遠端 YMES schema 及提供的 CREATE SQL 都沒有此表。SPC 數量門檻欄位不在 `tblopbasis`，已從作業站頁移除，未新增資料庫欄位。
- [x] `0007_operation_data_access.sql` 已授予 `ymes_app` 作業站表 CRUD 權限；遠端作業站表目前沒有業務資料。

- [x] 設備供應商 API/schema：2026-11-14 依 MESTEST DDL 套用 `0015_equipment_vendor_schema.sql`，migration 前備份 `/opt/ymes/backups/ymes_before_0015_equipment_vendor_20261114.dump`；供應商與聯絡人 CRUD、核准、DataStamp、狀態及引用限制 API 已部署。app-browser 驗證新增/修改供應商與聯絡人、重複編號拒絕及 `ES0001` 預設編號；修正新增後清單未更新，並清除測試資料。
- [ ] 設備：`tbleqpequipmentbasis`、Chamber 關係、設備群/工程群組、設備屬性、參數、QC List；支援清單 CRUD、核准/停用、複製、匯入。
- [ ] 設備群組與 SMD：比對 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 及設備群組 YMES 結構；缺表才建 migration。依規格在新增/移除設備明細時交易同步 SMD 映射；群組刪除保留舊 Service 僅刪明細與主檔、不清 SMD 映射的行為。
- [ ] 設備工程群組：完成 tbleqpengineergroupbasis API CRUD、DataStamp 衝突檢查、複製與成員清單 API；先比對 `ymes.tblusrusergroup`，缺表才依來源 CREATE SQL 建 migration。成員 API 僅操作 GroupType=2、AgentOf=NA 的直接成員；規格：docs/modules/EQP/設備設定/設備工程群組資料.md。
- [x] 設備狀態設定：Production API 與 `0016_equipment_state_api.sql` 已部署；核對 8 欄 YMES schema、恢復來源 DEFAULT，授予 app role 對主檔 CRUD；migration 前備份並驗證 `/opt/ymes/backups/ymes_before_0016_equipment_state_20261114_admin.dump`。app-browser 驗證 Production 清單/options、Create、Apply Update 與 HEX 色碼保留，測試列已清除；修正數字欄位驗證轉型錯誤。DELETE 缺少兩張 MES 引用表時回 503 fail-closed，待公司端來源 DDL 後驗證完整刪除流程。Vue 路由對應來源 `FunctionNo=EQP STATE`（FunctionKey `EP106`）。
- [x] 設備狀態設定來源交付（2026-11-16）：公司端以 PowerShell 5.1 直接查 MESTEST，已確認 `TBLEQPSTATECONTROL`、`TBLEMSEQUIPMENTSTATE` 物件、欄位、NOT NULL、PK、唯一索引；兩張表無額外索引/FK。空表 PG CREATE SQL、Oracle 原始 DDL 在 `database/reference/mestest_equipment_state_references_create.sql` 與 `mestest_equipment_state_references_source_ddl.sql`。`TBLSYSVALUELIST` 正式值/繁中翻譯：StateType 0 系統、1 使用者定義；UtilizationType 0 非稼動、1 稼動。詳見 `docs/modules/EQP/設備設定/設備狀態設定.md`。
- [ ] 設備狀態設定：核對 YMES 實際 schema 後新增引用表 migration 及最小 SELECT 權限；部署後 DELETE 需精確檢查 `tblemsequipmentstate.equipmentstate`、`tbleqpstatecontrol.equipmentstate`、`tbleqpqclistbasis.unvalidaction`，引用回 409、未引用才 204；API options 保持已確認數值，Vue 依資源翻譯顯示繁中，並驗證 Production。
- [x] 公司端於 2026-11-16 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接查得 MESTEST.TBLUSRUSERGROUP 原始 DDL；6 欄、PK (UserNo, GroupNo, GroupType, AgentOf)、GroupType DEFAULT 0、AgentOf 由 PK 強制 NOT NULL，僅 PK unique index，無其他 index/FK。Oracle DDL 與 PostgreSQL 空表 CREATE SQL 見 `database/reference/mestest_equipment_engineer_group_member_source_ddl.sql`、`mestest_equipment_engineer_group_member_create.sql`。
- [ ] 成員 GET 回傳可配置使用者及 selected；PUT 以交易方式取代指定 EngineerGroupNo 的 GroupType=2 集合，只寫入 AgentOf='NA' 的直接成員；刪除群組時與刪除成員關聯同交易。
- [ ] 作業站延伸資料：群組、屬性、參數、QC、Error/Abnormal、Resource/Area、Business Rule、SubOP；支援匯入。
- [x] 物料頁面 API：使用 `tblmtlmaterialbasis`，支援清單、明細、新增、修改、核准、停用/刪除與資料版本衝突控制；引用物料群組或產品 BOM 時禁止物理刪除。
- [x] 流程頁面 API：使用 `tblprsprocessbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除及流程圖讀寫/驗證。因來源 Node 型態代碼尚未釐清，流程圖儲存在 YMES 自有 `ym_prsprocess_node`、`ym_prsprocess_edge`。
- [x] 產品頁面 API：使用 `tblprdproductbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除、產品流程指派及 BOM 讀寫。來源 DDL 沒有這些 UI 關聯表，YMES 自有 `ym_product_process`、`ym_product_bom` 保存新增資料。
- [x] 物料表結構已比對及套用；Production API 已部署屬性、QC Item、Vendor Map、GumHour、CSV 匯入、核准檢查、QC 類別範本複製與子資料交易清理。2026-11-14 app-browser 驗證物料清單/新增、屬性新增與讀回、QC Item 儲存、GumHour 儲存；QA 物料及子資料已清除。
- [ ] 待公司 Oracle 端交付正式 `CountWay`/`CheckLotNo` ValueList、供應商名稱 Join、GumHour 參數來源；Production 匯入逐列瀏覽器測試、正式 Excel 範本、`.xls` 解析及 WinForms 群組寫入語意仍未完成。
- [ ] **物料 Excel 匯入 API 接手**：先讀 `docs/modules/MTL/物料資料匯入.md`。公司 Vue 已讀 CSV/`.xlsx` 第一工作表、依 9 欄順序解析並按物料編號/名稱分組；Production 範本與 legacy `.xls` 仍需 API。
- [ ] 提供 `GET /api/materials/import/template`，回傳正式模板位元組、檔名與原始副檔名；先查 YMES 是否有舊模板附件，若沒有再用相同欄序產生標準 `.xlsx`。
- [ ] 提供 `POST /api/materials/import/preview` multipart `file`，用伺服器 NPOI/相容套件解析 `.xls` 第一工作表，回傳正規化 `rows`（MaterialType、MaterialNo、MaterialName、PutInPlace、CountWay、CheckLotNo、UnitType、UnitNo、ERPUnitNo）；限制檔案大小/列數，並回覆欄位錯誤。
- [ ] 核對外部 `DataImporter.MaterialDataImport.MaterialDataChecker` 的多列/重複 MaterialNo 語意與交易邊界。現有 `POST /api/materials/import` 每筆呼叫 CreateMaterial，不能直接視為 WinForms 每個 `(MaterialNo, MaterialName)` 群組呼叫一次 checker 的等價行為；按規格調整群組 payload 或回傳逐列結果。
- [ ] 核實 `ERPUnitNo` 在舊 checker 的驗證/儲存用途；目前 `MaterialImportRowRequest` 未接收此欄位，不能默默丟棄。若正式匯入需要存資料，先從公司端取得欄位 DDL 再補 CREATE SQL/migration；若只用於驗證，記錄其去向。
- [ ] Production 匯入 API 目前同步、無法取消；若要求完整保留 WinForms 背景進度/取消，提供工作狀態/取消 API 並確保取消代表尚未執行列不再寫入。若先簡化，Production UI 應顯示伺服器處理中，不顯示可取消按鈕。
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
- [x] 公司端已從 WinForms/Service 確認配件選項來源為 MESTEST.TBLEQPACCESSORYTYPE，並以 PowerShell 5.1 + ODP.NET 取得原始 DDL、9 欄 nullable/default、PK/索引/FK；來源與空表 PostgreSQL CREATE SQL 已交付。
- [ ] 家用電腦先比對 ymes.tbleqpaccessorytype；缺表才建 migration。ACC Type options 依舊 Service 篩 IssueState=2、AccessoryType 非空提供。
- [ ] 核實 tblsyselementbasis 既有 YMES 結構與 MES Oracle DDL；目前 mestest_first_phase_create_tables.sql 中部分欄位有「推測」註記。依 WinForms ValueList 確認屬性選項與 PropertyType=1 查詢規則後，才實作 /api/system/elements?elementType=1。
- [ ] EP103 Vue 工程群組選擇器依賴共用 `GET /api/equipment/engineer-groups`，回傳 `engineerGroupNo`、`engineerGroupName`；與 EP102 群組清單 API 共用，不另建表。
- [ ] 實作設備類別清單/查詢、新增、修改、複製、刪除及核准 API。修改/核准/刪除使用 DataStamp；EDIT 僅 IssueState=0，核准僅 0，刪除只接受 0/-1/2，並沿用舊 Service 的引用檢查。
- [ ] 核實並實作 wsEQP.ApproveEquipmentType 對簽核 Job/Detail 的流程；無待簽工作可直接到 2，有簽核流程先到 1，再依結果到 2 或退回 0。
- [ ] ACC Type API：可選配件目錄查詢、既有關聯查詢、完整集合取代；在交易內同步 tbleqpequipmenttypeacctype。COPY 僅複製舊 clsEQPBasis.AddEquipmentType 明確複製的 ACC 關聯。
- [ ] Consume API：依規格驗證數量、容許量與週期關係，CRUD 以 (EquipmentType, ConsumeNo) 為鍵。
- [ ] Property API：依舊按鈕把設備類別編號映射至共用表 tbleqpequipmentproperty.equipment；預設查 IssueState=0；更新/刪除檢查 DataStamp。回傳屬性定義名稱與舊 ValueList 一致。
- [ ] 圖片庫 API：列舉、預覽（目前 Vue 契約為 { dataUrl }）、新增/更新 multipart pictureBody、刪除；確認被類別引用時拒絕刪除，限制 MIME/檔案大小並授權呼叫者。
- [ ] 設備類別範本 API：GET /api/equipment/types/import/template 回傳正式 .xlsx 範本；POST /api/equipment/types/import/preview 解析舊 .xls 第一工作表並回傳 distinct EquipmentType/EquipmentClass 結果列。
- [ ] 設備類別匯入工作 API：POST /api/equipment/types/import/jobs 接收 .xls/.xlsx multipart，沿用第一工作表固定 8 欄欄序、略過第 1 列標題、使用登入者 UserNo，依 EquipmentType 分組呼叫/重建 EquipmentTypeDataChecker；回傳 jobId/status/progress/results。
- [ ] 實作 GET /api/equipment/types/import/jobs/{jobId} 供進度與每個 EquipmentType/EquipmentClass 的 Message 輪詢；POST /api/equipment/types/import/jobs/{jobId}/cancel 合作式取消，保留已完成的逐類別結果。需把部分成功、失敗原因與是否已寫入正式表說清楚，不可只回 HTTP 成功。
- [ ] 以正式 PostgreSQL migration 管理實際要新增的 schema/索引/權限；授予 ymes_app 最小必要權限，所有多表修改使用交易。Production 禁用 Fake provider。
- [ ] API 完成後由公司電腦連 Development 假資料 UI 做契約對接，再由家用電腦完成 API/資料庫驗證；同步回寫規格與本清單。不要提交連線字串、密碼、Token、私鑰。
## 設備設定：設備群組資料（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備群組資料.md、docs/DATABASE_CONNECTION.md 與 database/reference/mestest_equipment_group_create.sql。此 SQL 建立已由 MES Oracle MESTEST 核實的兩張空表，不含業務資料；執行前先核對 YMES schema 既有結構。

- [ ] 確認 YMES 已有 tbleqpgroupbasis、tbleqpgroupdetail；缺表才執行單表 CREATE SQL。第一階段 bootstrap 也已含兩表，Oracle 核實後的欄位來源註解已校正。
- [x] 公司端已查得 TBLSMDAREARELATION、TBLSMDSMTAREAEQPMAP 原始 DDL、欄位/NULL/PK/索引/FK，交付 `database/reference/mestest_equipment_group_smd_source_ddl.sql` 與 `mestest_equipment_group_smd_create.sql`。
- [x] 公司端已完成 EP105 設備群組 Vue 頁 WinForms Apply/稽核 UI 與 Development preview 隔離；家用端仍只接手本節 schema/API/SMD migration，不需新增表規格。
- [ ] 實作設備群組清單、CRUD、核准/刪除 API：IssueState 0 才可修改；核准只允許 0，依 IssueNo EQP GROUP BASIS 啟動簽核；刪除只接受 0/-1/2，傳 DataStamp。
- [ ] 新增 API 實作 ChkObjectForSMDLayout(EquipmentGroup,4)：歷史 helper 會拒絕已存在於 `tblSMDAreaBasis.AreaNo`、`tblEQPEquipmentBasis.EquipmentNo` 或 `tblINVInventoryBasis.InventoryNo` 的同值編號。此 helper 本體在 `modKC.vb` 是註解版本；若家用端有目前部署的 active helper，先比對錯誤訊息與例外行為。
- [ ] 實作群組明細與可加入設備端點。可加入清單限定 IssueState=2、ChamberFlag=0、尚未出現在任何 group detail；新增前由 API 再次以交易/鎖定檢查設備跨群組唯一性。
- [ ] 明細新增/刪除只允許父群組 IssueState=0；要同步 TBLSMDSMTAREAEQPMAP 的情況依 TBLSMDAREARELATION ObjectType=4 判斷。
- [x] 公司端已從 clsEQPBasis.DelEQPGroupBasis 確認舊行為：同一批刪 group detail 與 group basis，不刪 TBLSMDSMTAREAEQPMAP；API 保留此相容行為。
- [ ] 核對並授予 ymes_app 對設備群組主從表與 SMD 映射表的最小讀寫權限；缺表時以已交付來源 SQL 建 migration。正式 API 不使用 Fake provider。
- [ ] 公司電腦完成 UI/API 契約對接後，回寫遷移規格及本清單；不要提交連線字串、密碼、Token、私鑰。

## 設備設定：設備狀態設定（公司電腦交接）

先閱讀 `docs/modules/EQP/設備設定/設備狀態設定.md`。主表與 CRUD/options API 已由家用端部署；本次公司端只調整 Vue UI/互動，不新增 schema 或 API。

- [x] 設備狀態主表 `ymes.tbleqpstatebasis` 已存在；migration `0016_equipment_state_api.sql` 恢復 Oracle 確認過的 DEFAULT，且 API GET/options/POST/PUT/DELETE 已部署。
- [x] StateType 0/1、UtilizationType 0/1 與繁中標籤已由公司端唯讀核對 MESTEST ValueList/資源檔；API options 保留上述值。
- [x] API DTO/Apply HEX 保留行為、ARBG signed integer、User Define 0–10 限制已記錄於規格；前端改為只在 Development preview 使用假資料。
- [x] 兩張刪除引用表 Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 已交付：`database/reference/mestest_equipment_state_references_source_ddl.sql`、`database/reference/mestest_equipment_state_references_create.sql`。
- [ ] 在 YMES 實際 schema/migrations 比對 `tbleqpstatecontrol`、`tblemsequipmentstate` 與 `tbleqpqclistbasis.unvalidaction`；對已存在資料表保留額外欄位，只有缺少且 API 必須時新增 forward migration。
- [ ] 給 `ymes_app` 對引用來源最小 SELECT 權限；migration/權限完成後移除 DELETE fail-closed 503，精確檢查三個來源，引用回 409、無引用回 204。
- [ ] 部署後驗證引用狀態阻擋及未引用刪除；將結果回寫規格與本待辦。不得提交連線字串、密碼、Token 或私鑰。
## 設備設定：設備點檢表設定（公司電腦 UI 已交付；家用端待 API）

先閱讀 docs/modules/EQP/設備設定/設備點檢表設定.md 與 database/reference/mestest_equipment_qclist_create.sql。SQL 來自 MES Oracle MESTEST 原始 DDL，轉為 PostgreSQL 空表結構，不含 MES 點檢主檔/明細資料。

- [x] 公司端已依 WinForms Form/Designer/Service 補齊 Vue DEV UI：主從清單、Add/Edit/Copy/Delete、明細新增/修改/多選刪除/排序、Confirm/Close/Apply、稽核欄位與作用列刪除行為；本輪無新表或新 DDL。
- [ ] 確認 YMES ymes.tbleqpqclistbasis、ymes.tbleqpqclistdetail 是否已存在；逐欄比對後，缺表才套用參考 SQL。第一階段 bootstrap 已按 Oracle DDL 補上 AFTEREVENTQC DEFAULT 0、DATASTAMP DEFAULT 0、INPUTDATACOUNT DEFAULT 1。
- [ ] 實作 GET /api/equipment/qc-lists（qcListNo 查詢、QCListNo 排序）、GET /api/equipment/qc-lists/{qcListNo}/items（QCOrder 排序）。
- [ ] 實作 GET /api/equipment/qc-lists/options；QCType 只回 0「開始生產時點檢」和 2「手動點檢」（已由公司端唯讀核對 MESTEST ValueList/繁中資源），設備狀態選項依 YMES 設備狀態主檔；-1 顯示「不動作」。
- [ ] POST /api/equipment/qc-lists 新增主檔及完整明細；PUT /api/equipment/qc-lists/{qcListNo} 使用 DataStamp 樂觀並行檢查並完整取代明細；DELETE 同交易刪除明細與主檔。任何多表寫入需由一個 PostgreSQL transaction 保證原子性。
- [ ] 伺服器驗證與 WinForms 一致：編號/名稱必填；編號與名稱長度 100；QCType、UnValidAction 必填；至少一項；QCOrder 依序且不超過 NUMBER(2,0)；CheckItem 不可重複且最多 1000；Type 0 StdValue 數字必填；Type 1 至少一個邊界、各自為數字、雙邊界時最大值大於最小值、空邊界存星號；InputDataCount 1–10；方法/標準最多 100。
- [ ] AfterEventQC 只允許 QCType=0；其他類型強制 0。DataStamp 不符回 409；欄位驗證回 400；不存在回 404。
- [ ] 確認既有 YMES 外鍵/點檢排程使用關係與 legacy delete 的影響；Oracle 原表 DDL 未宣告主從 FK，legacy Service 的 delete 會明確刪主檔和明細。
- [ ] Production API 完成後以公司電腦 DEV UI 對接；同步更新規格與本待辦，不能提交密碼、Token、連線字串或私鑰。

## 設備資料：載具分類資料 EP301（公司端 UI 與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/載具/載具分類資料.md`、`docs/DATABASE_CONNECTION.md` 及下列 PostgreSQL CREATE SQL。Oracle DDL 由公司電腦以 PowerShell 5.1 + ODP.NET 直接唯讀查得，不含 MES 業務資料。

- [x] 公司端已對照 `frmCarrierCategory` / `frmCarrierCategoryDef`、`wsEQP`、`clsCarrierBasis`；Vue DEV UI、Function route 與逐筆 API 契約已交付。
- [x] 公司端已查得 MESTEST.TBLEQPCARRIERCATEGORY 8 欄、PK、唯一索引、NOT NULL/default；Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_carrier_category_source_ddl.sql`、`database/reference/mestest_carrier_category_create.sql`。IssueState DEFAULT 0 NOT NULL；DataStamp DEFAULT 0 nullable；無 FK/額外索引。
- [ ] 核對 YMES `ymes.tbleqpcarriercategory` 既有欄位/NULL/DEFAULT/PK/index 與 migration；缺表才依 `mestest_carrier_category_create.sql` 新增 migration，保留任何額外 YMES 欄位。
- [ ] 實作 GET `/api/equipment/carrier-categories?issueState=&carrierCategory=`，狀態限 -1/0/1/2、CarrierCategory 部分符合、排序；回傳稽核欄位及 DataStamp。
- [ ] 實作 POST 新增與 PUT 修改；CarrierCategory Trim/ToUpper、必填、最多 50，Description 最多 255；新增設定 IssueState=0/Creator/CreateDate/DataStamp=0；Edit 只允許 IssueState=0 並做 DataStamp 比對。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除，2 改成 -1 並更新 Reviser/ReviseDate/DataStamp，1 拒絕。舊服務雖只對狀態更新檢查 DataStamp，新 API 建議刪除兩種分支都做並行檢查；API 每筆操作保持原子。
- [ ] 實作 POST `/{carrierCategory}/approve`：只接受狀態 0 並檢查 DataStamp。舊 `wsEQP.ApproveCarrierCategory` 依 EQP Issue/Job 設定，無簽核工作可直接轉 2；有 Job 則建立簽核主檔並轉 1、簽核完成轉 2、退回轉 0，並通知第一簽核群組。Production 不可把 DEV 0→2 的簡化結果當成正式簽核成功。
- [ ] 完成後以公司端 Vue 頁對接、核對 Delete 多筆部分成功、Edit/Approve/Delete 的狀態與並行衝突、API 回傳及稽核欄位，更新規格並部署。

## 設備資料：載具類別資料 EP302（公司端 UI 與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/載具/載具類別資料.md`、`docs/DATABASE_CONNECTION.md` 及下列 PostgreSQL CREATE SQL。Oracle DDL 由公司電腦以 PowerShell 5.1 + ODP.NET 直接唯讀查得，不含 MES 業務資料。

- [x] 公司端已對照 `frmCarrierType` / `frmCarrierTypeDef`、`wsEQP`、`clsCarrierBasis`；Vue DEV UI、Function route 與逐筆 API 契約已交付。
- [x] 公司端已查得 MESTEST.TBLEQPCARRIERTYPE 13 欄、PK、唯一索引、NOT NULL/default；Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_carrier_type_source_ddl.sql`、`database/reference/mestest_carrier_type_create.sql`。CarrierCategory 雖是邏輯關聯，原表沒有 FK 或額外索引。
- [ ] 核對 YMES `ymes.tbleqpcarriertype` 現有欄位/NULL/DEFAULT/PK/index；缺表才依 CREATE SQL 建 migration，保留 YMES 額外欄位。
- [ ] 實作 GET `/api/equipment/carrier-types?carrierType=`，CarrierType 部分符合、排序；回傳全部顯示欄位、IssueState、DataStamp 及稽核欄位。可提供可選 `issueState` 過濾，但 Vue 的 WinForms 狀態列會在前端即時切換。
- [ ] 實作 GET `/api/equipment/carrier-categories?issueState=2` 作為 CarrierCategory 查找選項；只提供已核准分類。
- [ ] 實作 POST 新增與 PUT 修改：CarrierType Trim/ToUpper、必填、最多 50；CarrierCategory 必須存在且 IssueState=2；UseLayer 必須 0/1；UseLayer=0 時 CarrierLayer/CarrierCapacity/StartPosition 寫 1，UseLayer=1 時三者須正整數且符合 NUMBER(3,0)；Description 最長 255。新增設定 IssueState=0/Creator/CreateDate/DataStamp=0；Edit 限 IssueState=0 並做 DataStamp 比對。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除，2 改成 -1 並更新 Reviser/ReviseDate/DataStamp，1 拒絕。舊服務未對實體刪除做 DataStamp 條件；新 API 建議兩種分支都檢查並行衝突，且每筆操作原子。
- [ ] 實作 POST `/{carrierType}/approve`：只接受狀態 0 並檢查 DataStamp。舊 `wsEQP.ApproveCarrierType` 依 `CARRIER TYPE` Issue/Job 設定直核轉 2，或建立 Issue 主檔並轉 1；簽核完成轉 2、退回轉 0、通知第一簽核群組。不可把 DEV 0→2 簡化結果當正式簽核成功。
- [ ] 完成後以公司端 Vue 頁對接；核對 UseLayer 條件欄位、分類選項、批次 Delete/Approve 部分成功、DataStamp 與稽核欄位，更新規格並部署。

## 設備基本資料 EP104（公司電腦規格交接）

先讀 docs/modules/EQP/設備設定/設備基本資料.md 與 docs/modules/EQP/設備設定/設備參數設定.md。設備主表與參數表原始 DDL 已由使用者提供；單表 PostgreSQL CREATE SQL 已放在 database/reference/。

- [x] 公司端將使用者提供的 TBLEQPEQUIPMENTBASIS 原始 DDL 轉成 database/reference/mestest_equipment_basis_create.sql，並修正第一階段 bootstrap 的 DEFAULT/NOT NULL constraints，移除推測欄位註解。
- [x] 公司端將使用者提供的 TBLEQPPARAMETERBASIS 原始 DDL 轉成 database/reference/mestest_equipment_parameter_basis_create.sql，並修正第一階段 bootstrap 的 NOT NULL constraint names，移除推測欄位註解。
- [ ] 家用電腦比對 "YMES" schema 內 TBLEQPEQUIPMENTBASIS、TBLEQPPARAMETERBASIS 的欄位/NULL/DEFAULT/PK/index；缺表才套用 CREATE SQL，既有表若不符則建立 forward migration。不要在公司電腦連資料庫或執行 migration。
- [ ] 核對設備 CRUD/Approve、Chamber 共表資料、DataStamp、IssueState、刪除轉停用與 Controlled 屬性核准檢查，確保多表更新使用正確交易。
- [ ] 提供 EquipmentClass、設備類別、配件類別、工程群組、供應商、點檢表及訊息佇列 lookup/options API；配件類別取自 ymes.tbleqpaccessorytype 並保留 IssueState=2 篩選。
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

## EP202 配件類別資料（公司端 2026-11-16 已交接）

公司端已依 docs/modules/EQP/配件/配件類別資料.md 完成 Vue Development 假資料頁，並直連 Oracle 查得下列來源表 DDL。請先 pull，再比對 YMES 實際 schema；只有缺表才新增 forward migration，不直接執行 Oracle 原始 DDL。

來源資料：
- database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql / _create.sql：TBLEQPACCESSORYTYPE。
- database/reference/mestest_accessory_type_dependencies_source_ddl.sql / _create.sql：TBLEQPACCESSORYCATEGORY、TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYLIFE。

家用端工作：
1. 比對四表實際 YMES 欄位、NULL/default、PK 及既有歷史 migration；保留 YMES 額外欄位，不覆蓋既有表。
2. 實作規格中的 accessory type CRUD、category lookup、property（固定 AccessoryVersion=-1）、life API。
3. 刪除須在單一交易依序清理屬性與壽命後刪主檔；Copy API 僅複製來源壽命列，不複製屬性列。
4. 核准端接原有簽核流程並回傳實際 IssueState；DEV UI 直接轉為 2 只是本地簡化。
5. Import API 支援舊第一工作表欄序 AccessoryType、AccessoryCategory、Description、.xlsx/.xls，保留前兩欄必填與逐組 DataChecker 結果/錯誤訊息。公司端目前只有 CSV 本地假匯入。
6. 部署後更新本節、功能規格與 API 狀態，再由公司端 pull 對接實際 API。

API 路徑與 DTO 詳見 docs/modules/EQP/配件/配件類別資料.md；目前 Production API 尚未完成，不要以 Development 假資料結果回報正式成功。

## EP201 配件分類與 EP204 可用狀態（公司端 2026-11-16 交接）

先讀以下規格與程式，再做 YMES schema/API；公司端已用唯讀 Oracle 連線核實 DDL，並完成 Vue Development 假資料頁。不可把假資料視為 MES 業務資料。

### 來源與 SQL

- EP201 規格：docs/modules/EQP/配件/配件分類資料.md
- EP204 規格：docs/modules/EQP/配件/配件分類狀態定義設定.md
- EP201 主表 TBLEQPACCESSORYCATEGORY 的來源 DDL/PG 空表 SQL：database/reference/mestest_accessory_type_dependencies_source_ddl.sql、database/reference/mestest_accessory_type_dependencies_create.sql。
- EP204 狀態定義表 TBLEQPACCCATEGORYSTATEBASIS：database/reference/mestest_accessory_category_state_source_ddl.sql、database/reference/mestest_accessory_category_state_create.sql。
- EP204 lookup 表 TBLEQPACCSTATEBASIS：database/reference/mestest_accessory_state_source_ddl.sql、database/reference/mestest_accessory_state_create.sql。database/reference/mestest_first_phase_create_tables.sql 中的 lookup 表 block 已用 Oracle 原始 DDL 修正。
- CREATE SQL 只建空結構、不含 MES 資料。開始前查 YMES PostgreSQL 實際 schema；既有表保留額外欄位，缺表才新增 forward migration，欄位/default 差異用 migration，不要在來源 Oracle 執行 SQL。

### 家用端待辦

1. 逐欄比對三張表的欄位型別、NULL/default、PK、索引與既有 migration。TBLEQPACCCATEGORYSTATEBASIS 的 PK 為 AccessoryCategory/AccessoryState/OperationName；DATASTAMP default 0 但可 NULL。TBLEQPACCSTATEBASIS 的 STATETYPE default 1 且 NOT NULL；DATASTAMP default 0 但可 NULL。沒有額外 Oracle FK/index。
2. 若缺表，新增可重複/版本化 migration，套用前按既有部署慣例備份；授予 ymes_app 最小必要 CRUD 權限。不要直接改動已部署資料。
3. 建立 EP201 API：GET/POST/PUT accessory-categories、POST approve/delete/import。對齊規格中的 0/1/2/-1 狀態、DataStamp、簽核 Job/Detail 流程；狀態 0/-1 物理刪除、2 改為 -1、1 拒絕。來源刪主分類不會 cascade 狀態子列，API 保留此行為。
4. 建立 EP204 API：GET/POST accessory-category-states、PUT edit、POST approve/delete；GET accessory-states、GET accessory-state-controls?controlType=EMS、GET system/functions?executionMode=0&accessoryStateChangeInterface=true，以及分類 options。
5. EP204 新增/修改需檢查每個 AccessoryCategory 至多一個 InitialFlag=1。Oracle 原表沒有相應 unique index；Production 需以交易/鎖或先確認既有資料後新增安全約束，避免並行同時設定兩個初始狀態。
6. OperationName 不可等於 ControlType=EMS 標準動作的 ControlNo 或翻譯名稱。介面選項只限 EXECUTIONMODE=0 且 ACCStateChangeInterface=1；預設介面 frmACCStateChangeDescription，執行檔 ES_ACCStateChangeDescription.dll。
7. 狀態清單以 AccessoryState join TBLEQPACCSTATEBASIS 取得 StateName/StateColor。EP204 刪除 0/-1 物理刪除、2 改成 -1、1 拒絕；核准走現有 issue workflow。
8. 配合 Vue API DTO 欄位採 camelCase：AccessoryCategory/AccessoryState/OperationName 三欄為 EP204 key，寫入時驗 DataStamp。匯入回傳逐筆 results（accessoryCategory/result/message）；支援舊第一工作表 .xls/.xlsx 欄位 AccessoryCategory/Description 和 DataChecker 分組結果；Vue 以 multipart/form-data 的 file 欄位上傳 Excel，CSV 則傳 JSON。
9. 部署後更新規格與本段待辦，再通知公司端 pull 對接 Production；API 不可讓 Production 回退 localStorage。

Vue 元件與路由已在公司端完成：web/src/components/AccessoryCategoryMaster.vue、AccessoryCategoryStateMaster.vue；路徑為 /YMES/Equipment/AccessoryCategories、/YMES/Equipment/AccessoryCategoryStates。兩頁 Production endpoint 尚未存在。


## EP203 配件基本資料（家用電腦接手）

先讀 docs/modules/EQP/配件/配件基本資料.md 與 docs/DATABASE_CONNECTION.md。公司端以 Windows PowerShell 5.1 + ODP.NET 直接唯讀查 MESTEST DDL；本次沒有查詢或匯出業務資料。

### Oracle 來源檔

- 六張表原始 Oracle DDL、八個 index DDL：database/reference/mestest_accessory_basis_source_ddl.sql
- 六張表 PostgreSQL 空表 CREATE SQL：database/reference/mestest_accessory_basis_create.sql
- EP203 主表區塊已回寫 database/reference/mestest_first_phase_create_tables.sql。
- 新表清單：TBLEQPACCESSORYBASIS、TBLEMSACCESSORYSTATE、TBLEMSACCESSORYSTATELOG、TBLEQPACCSTATECONTROL、TBLEMSEQPACCSTATE、TBLEMSEQPACCSTATELOG。
- 原始 Oracle DDL 沒有 FK。非 PK index 僅主表 GETGROUPNO 與設備配件狀態 EQUIPMENTNO；其他六個 index 為 PK index。
- DDL 僅描述結構；不得在來源 Oracle 執行 CREATE SQL，不得把本機憑證寫入 Git。

### 家用端待辦

1. 逐欄比對 YMES PostgreSQL schema/migrations 的上述六表：欄位型別、長度、NULL、DEFAULT、PK、index；查 TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYTYPE、TBLEQPACCESSORYCATEGORY、TBLEQPACCSTATEBASIS、TBLEQPACCCATEGORYSTATEBASIS、TBLEQPACCESSORYLIFE、TBLSYSELEMENTBASIS、設備狀態/簽核資料是否已有 YMES 對應。缺表才新增 forward migration；既有額外欄位不可刪。
2. 實作 GET api/equipment/accessory-basis?issueState=all&currentOnly=false，回傳 WinForms Grid 所有非隱藏欄位及 IssueState、CurVersion、AccessoryVersion、DataStamp。排序 AccessoryType、AccessoryNo；保留前端查詢 AccessoryNo。
3. 實作 POST 新增、PUT /{accessoryNo}/{version} 修改，伺服器驗證長度/必填/版本複合鍵；新增狀態 0、版本 01。PUT/Approve/Delete 驗 DataStamp，Creator/Reviser 與時間由登入上下文/server clock 取得。
4. Approve 只接受狀態 0，依配件分類初始狀態、TBLEQPACCSTATECONTROL Initial/EMS、配件壽命 RepairCycle 和既有簽核 Job/Detail 實作。同步目前配件狀態、設備配件映射及歷程資料；回傳真實狀態，不可固定把每筆標成 2。多表更新使用單一 PostgreSQL transaction。
5. Delete 接受 0/-1/2：0/-1 依舊 Service 刪配件現況、設備目前配件、屬性（所有版本）及主檔；2 改為 -1 並更新 DataStamp/Reviser/ReviseDate；1 回 409。多選回逐筆結果，避免部分失敗無法追蹤。
6. Copy API 新增主檔及複製來源已核准的配件屬性至新版本，目標屬性 IssueState=0；必須同交易完成。公司 UI 目前使用新增後呼叫 copy-properties，建議收斂成單一 POST copy endpoint，或讓 API 提供相同原子契約。
7. 子屬性依配件/版本提供 GET/POST/PUT/DELETE/Approve；使用既有 TBLEQPACCESSORYPROPERTY，不得另建重複表。核對 DataStamp、PropertyNo 定義 (PropertyType=1)、元素值 lookup (TBLSYSELEMENTBASIS) 與狀態規則。
8. 提供已核准配件類別及供應商 lookup；舊配件類別清單須取 IssueState=2，AccessoryCategory 由所選類別帶入。API 從 auth context 取操作者，不信任前端 Creator/Reviser。
9. 匯入需支援舊 Excel 第一工作表七欄：AccessoryNo、AccessoryType、AccessoryCategory、VendorNo、ModelNo、AssetNo、Description。前四項必填；重查已存在編號、已核准類別/供應商及分類關係；每組 (AccessoryNo, AccessoryType) 唯一；插入版本 01/狀態 0。實作 multipart /import/preview 回傳 token+逐列結果及 /import/commit 重新驗證後交易寫入；提供 ACC BASIS 範本下載。
10. 完成後更新 API 規格/migration 記錄及本待辦，通知公司端 pull 對接。Production 不得回退 localStorage 假資料。

## EP205 配件狀態設定（公司端 2026-11-16 已交接）

先讀 docs/modules/EQP/配件/配件狀態設定.md。公司端已照 WinForms frmEQPACCStateBasis / Def 建立 Vue Development 假資料頁，使用既有 Oracle 狀態主表，不新增重複 DDL。

- Oracle DDL：database/reference/mestest_accessory_state_source_ddl.sql；PostgreSQL 空表 SQL：database/reference/mestest_accessory_state_create.sql。Oracle 欄位/PK/index 已透過 PowerShell + ODP.NET 唯讀查證。
- 元件與路由：web/src/components/AccessoryStateBasisMaster.vue、/YMES/Equipment/AccessoryStates；FunctionNo EQP ACC STATE / EP205。
- 先比較 YMES tbleqpaccstatebasis 實際 schema 與 migration；若表已存在則保留 YMES 欄位，只針對缺漏差異新增 migration，不重建、不將 Oracle 原始 SQL 套到來源資料庫。
- 實作 GET/POST/PUT/DELETE /api/equipment/accessory-states，與 EP204 共用狀態 lookup DTO；回傳 signed ARGB stateColor，保留可為 NULL 的 DataStamp。
- 新增/修改驗證 AccessoryState -99..99、StateType 0/1、User Define 不可用 0..10、StateName 必填、Description 最長 255；StateName 最長 50。稽核欄位取 auth context/伺服器時間；修改用 DataStamp 並行檢查。
- DELETE 不 cascade；若 TBLEQPACCCATEGORYSTATEBASIS 引用該狀態，回 409，避免舊表無 FK 時留下孤兒狀態定義。舊 WinForms 直接刪除的差異記在 API 文件。
- 確認 ymes_app 最小權限、API DTO/錯誤回應與 EP204 消費端一致；部署後更新規格/本待辦並通知公司端接 API。

## 設備資料：載具基本資料 EP303（公司端 UI、規格與 Oracle DDL 已交付）

先讀 docs/modules/EQP/載具/載具基本資料.md、docs/DATABASE_CONNECTION.md 及下列 SQL。公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查詢 MESTEST；沒有讀取業務資料，也沒有在 Oracle 執行 DDL。

### 已交付

- [x] Vue DEV 元件 web/src/components/EquipmentCarrierBasisMaster.vue；第三層 FunctionNo CARRIER BASIS，路徑 /YMES/Equipment/CarrierBasis。
- [x] Oracle 原始 DDL：database/reference/mestest_carrier_basis_source_ddl.sql、database/reference/mestest_carrier_basis_dependencies_source_ddl.sql。
- [x] PostgreSQL 空表 CREATE SQL：database/reference/mestest_carrier_basis_create.sql、database/reference/mestest_carrier_basis_dependencies_create.sql。
- [x] 主表 TBLEQPCARRIERBASIS 15 欄，PK CarrierNo；依賴 TBLWIPCarrierState 6 欄、TBLSMDFactoryBasis 10 欄。三表各僅 PK unique index，無 FK；CarrierBasis IssueState DEFAULT 0 NOT NULL、DataStamp DEFAULT 0 nullable。
- [x] first-phase SQL 已補載具在製狀態表，修正 CarrierBasis/FactoryBasis defaults，移除來源未證實的推測欄位註解。

### 家用端待辦

1. 先核對 YMES schema/migrations 中 ymes.tbleqpcarrierbasis、ymes.tblwipcarrierstate、ymes.tblsmdfactorybasis 的欄位型別/長度/NULL/DEFAULT/PK/index；FactoryBasis 已在第一階段 SQL，先確認 YMES 實際表。缺表才新增 forward migration，保留 YMES 額外欄位，不直接在 Oracle 執行 CREATE SQL。
2. 實作 GET /api/equipment/carrier-basis?carrierNo=，回傳全部 IssueState，CarrierNo 部分符合；包含所有非隱藏 grid 欄位、DataStamp、稽核欄位。
3. 實作 POST 新增主表與 WIP CarrierState=0 列，單一交易；CarrierNo 大寫/必填/最多 50，Category/Type 必填最多 50、FactoryNo 必填最多 20、Description 最多 255；Category/Type/Factory 必須是 IssueState=2 且 Type 屬於指定 Category。Creator/CreateDate 取 auth context/server clock，DataStamp 初值 0。
4. 實作 PUT /api/equipment/carrier-basis/{carrierNo}，只允許 IssueState=0、DataStamp compare-and-swap；更新主表 Category/Type/FactoryNo/Description、Reviser/ReviseDate/DataStamp，並在同一交易同步 WIP CarrierCategory/CarrierType。
5. 實作 DELETE /{carrierNo}?issueState=&dataStamp=：0/-1 在同一交易實體刪除主表與 WIP 列；2 僅將主表改為 -1 並更新稽核/DataStamp；1 回 409。舊服務物理刪除未帶 DataStamp 條件，新 API 兩種分支都要比對版本。
6. 實作 POST /{carrierNo}/approve：IssueNo CARRIER BASIS；先確認 Issue/Job/Detail 配置。未配置簽核工作時可直轉 2；有簽核工作時建立工作並轉 1、通知第一簽核群組，核准轉 2、退回轉 0。使用既有 YMES 簽核服務，不把 DEV 直核流程當 Production。
7. 提供 GET /api/master-data/factories?issueState=2，從 ymes.tblsmdfactorybasis 回傳 FactoryNo/FactoryName/Description；檢查 ymes_app 唯讀權限。
8. EP302 的 GET /api/equipment/carrier-types 增加可選 carrierCategory 篩選；EP303 Type picker 傳 issueState=2 和 carrierCategory。現有狀態列仍由 Vue 前端即時切換。
9. 三張表比對/migration、options、CRUD/簽核完成後更新規格/API 文件、本待辦與資料庫紀錄，再部署並通知公司端 pull 對接。公司端尚未做瀏覽器人工驗收。

## 設備參數資料 EP401（公司端 UI、規格與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/設備設定/設備參數資料.md`、`docs/DATABASE_CONNECTION.md` 與兩份 DDL。此功能使用 `TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL`，Oracle DDL 由公司電腦 PowerShell 5.1 + ODP.NET 直連唯讀取得；未匯出 MES 業務資料。

### Oracle/PG schema

- Oracle 原始 DDL：`database/reference/mestest_equipment_recipe_source_ddl.sql`。
- PostgreSQL 空表 CREATE SQL：`database/reference/mestest_equipment_recipe_create.sql`；`database/reference/mestest_first_phase_create_tables.sql` 已修正相關 bootstrap block。
- 主檔 PK `(recipegroup,recipeversion)`、非唯一索引 `(recipegroup,curversion)`；明細 PK `(recipegroup,recipeversion,recipeno)`、非唯一索引 `(recipegroup,recipeno)`。來源無 FK，主檔刪除須 API 交易刪除明細再刪主檔。
- DEFAULT/NULL、完整欄位與 Type 值說明見功能規格及 CREATE SQL。執行前逐欄比較 YMES 實際 schema/migrations；缺表才新增 forward migration，保留既有 YMES 額外欄位。

### API/schema 待辦

1. 實作 GET/POST/PUT/DELETE `/api/equipment/recipes`、簽核與 copy/version endpoint；欄位驗證、DataStamp compare-and-swap、稽核欄位由登入上下文/伺服器時間取得。
2. 實作每個 `(RecipeGroup,RecipeVersion)` 的 Detail GET/POST/PUT/DELETE。RecipeNo 唯一；只有父主檔 IssueState=0 可維護明細；主檔刪除與明細刪除同交易。
3. 核准 IssueNo=`RECIPE BASIS`：核對 YMES 簽核工作/明細，支援直接核准與 Pending/核准/退回。核准時同一 RecipeGroup 只能有一個 CurVersion=1；Pending 時保持舊版 current，核准 SQL 完成才切換。
4. COPY/VERSION 沿用 wsEQP.CopyRecipe：只複製 RecipeNo、RecipeValue、Sequence、Description；Type、StdValue、Maximum、Minimum、Creator、CreateDate 不複製（NULL）。Version 使用來源選取版本+1，不以「最大版本+1」註解推測。
5. Recipe No picker 使用既有 `TBLSYSELEMENTBASIS`，提供 `GET /api/system/elements?elementType=9` 回傳 ElementNo/Description/DefaultValue/ElementSequence；不要建立重複元素表。
6. Type options 經 MESTEST TBLSYSVALUELIST 查證：-1 NONE、0 TYPEVALUE、1 RANGE；Production DTO/畫面使用明確標籤。
7. 授予 `ymes_app` 最小必要權限，完成 migration/API/簽核/併發驗證後更新規格並部署，再通知公司端 pull 對接 Production。

## ENT ET101 客戶基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶基本資料.md`、`docs/DATABASE_CONNECTION.md` 和以下來源 SQL。公司端已用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得三表結構；沒有讀取客戶業務資料，沒有在來源 Oracle 執行 DDL。

### DDL / YMES schema

- Oracle source logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`。
- PostgreSQL 空表 CREATE SQL：`database/reference/mestest_customer_basis_create.sql`。
- 三表：`TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR`；各自只有 PK unique index，Oracle 無 FK/其他索引。
- 先逐欄比較 YMES PostgreSQL schema/migrations 的型別、長度、NULL、DEFAULT、PK/index。缺表才新增 forward migration；保留 YMES 已有額外欄位/索引，不要以 `IF NOT EXISTS` 假設既有表已符合。
- 來源列 CustomerSName VARCHAR2(100 CHAR)，PG 使用 varchar(100)；Oracle DATE 使用 timestamp without time zone；NUMBER(1,0)/NUMBER(11,0) 保留 numeric 型別/DEFAULT/NULL。

### API / Service 待辦

1. 實作 `GET /api/ent/customer-basis`，回傳 WinForms 33 個可見欄位、IssueState、DataStamp、稽核欄位及來源保留欄位；支援 CustomerNo 查詢。IssueState 列表篩選由 Vue 即時處理。
2. 實作 POST 新增及 PUT `/{customerNo}` 修改。CustomerNo、CustomerName、CustomerSName 必填；CustomerNo 大寫，編號唯一；空白選項保存為 NULL/依既有 YMES API 契約處理。以登入上下文/伺服器時間寫 Creator/CreateDate/Reviser/ReviseDate，不信任用戶端稽核資料；修改以 DataStamp compare-and-swap。
3. 對非 IssueState=0 的修改重現 WinForms 可編輯範圍：基本身份/聯絡欄位唯讀；允許交易/發票欄位更新。API 必須伺服器端以欄位 allow-list 強制此限制。來源的欄位清空 sentinel 不可靠，新 API 要能明確保存空值。
4. 實作 DELETE `/{customerNo}?issueState=&dataStamp=`。狀態 0/-1 在一筆 PostgreSQL transaction 內依序刪 `tblentcustomershipaddr`、`tblentcustomercontactor`、`tblentcustomerbasis`；狀態 2 改為 -1 並更新 Reviser/ReviseDate/DataStamp；狀態 1 回 409。所有分支都先比對 DataStamp。不要新增未核實的 FK cascade。
5. 實作 POST `/{customerNo}/approve?dataStamp=`，只接受 IssueState=0；沿用 YMES 簽核服務與 IssueNo `CUSTOMER BASIS`。未設定 Job/Detail 可直接到 2；有簽核 Detail 時轉 1、簽核通過到 2、退回到 0。關聯簽核與稽核更新需交易一致。
6. 實作 multipart POST `/api/ent/customer-basis/import/preview`、POST `/import/commit` 及 GET `/import/template`。讀 Excel 第一工作表前三欄 CustomerNo/CustomerName/CustomerSName，逐列必填，預覽回傳依 CustomerNo 分組的結果列與短期 preview token；token 綁定登入者並於 commit 重新驗證。Commit 使用舊 DataImporter CustomerDataChecker 的等價檢查並回傳逐組 Message/進度。範本需沿用 FunctionNo `ENT BASIS` 的匯入範本格式；DEV CSV 不是正式範本。
7. 設定 `ymes_app` 最小表權限；完成 schema/API、交易、簽核與匯入規格後更新 migration/API 文件和本待辦，部署並通知公司端 pull。公司端 Vue 頁在家用 API 完成後再對接 Production。

## ENT ET102 客戶連絡人資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶連絡人資料.md`、ET101 規格與 `todolist_officepc.md` ET102 區塊。公司端已核對 WinForms 並完成 Development Vue 假資料頁。

### Schema

- Oracle DDL 與 PostgreSQL 空表 CREATE SQL 已隨 ET101 交付：`database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。`TBLENTCUSTOMERCONTACTOR` 欄位為 `CUSTOMERNO varchar(50) NOT NULL`、`CONTACTORNAME varchar(50) NOT NULL`、`TELNO varchar(25)`、`FAXNO varchar(25)`、`EMAIL varchar(50)`、`TITLE varchar(25)`、`DEPTNAME varchar(25)`、`CREATOR varchar(10)`、`CREATEDATE timestamp`、`DESCRIPTION varchar(255)`、`DATASTAMP numeric(11,0) DEFAULT 0` nullable、`REVISER varchar(10)`、`REVISEDATE timestamp`。
- 複合 PK 順序 `(CUSTOMERNO, CONTACTORNAME)`；無 Oracle FK 或額外索引。不要再建立重複 reference Create SQL。
- 核對 YMES migration 是否已建此表；若沒有，沿用上述 PostgreSQL 定義新增 forward migration。保留現存 YMES 擴充欄位/索引，別用 `IF NOT EXISTS` 代替 schema 比對。`ymes_app` 只授予需要的表權限。

### API 待辦

1. 以現有 ENT API controller/repository 的結構新增巢狀端點：`GET /api/ent/customer-basis/{customerNo}/contactors`；按 `ContactorName` 排序。客戶選擇器使用 ET101 的 `GET /api/ent/customer-basis`，需能搜尋全部 IssueState。
2. 新增 `POST /api/ent/customer-basis/{customerNo}/contactors`、`PUT /api/ent/customer-basis/{customerNo}/contactors/{contactorName}`、`DELETE /api/ent/customer-basis/{customerNo}/contactors/{contactorName}?dataStamp=`。Body 對應 `contactorName/telNo/faxNo/email/title/deptName/description/dataStamp`；Update 的 composite key 由 route 指定，ContactorName 不可更名。
3. 寫入時驗證 CustomerNo 存在（來源表沒有 FK）、必填 ContactorName 與唯一複合鍵；長度嚴格依 Oracle DDL，特別是 WinForms Title MaxLength=50、實際資料庫僅 25，回傳 400 欄位錯誤、不截斷。
4. Creator/CreateDate、Reviser/ReviseDate 從登入身分與伺服器時間產生；新資料 DataStamp 設 0，更新/刪除以 DataStamp 條件保護並在更新時遞增。刪除依舊 WinForms 支援多筆逐筆處理；API 單筆刪除回傳 404/409 供 Vue 中止、重載清單。
5. 原始 `wsENT.EditCustomerContactor` 呼叫帶有命名參數 DataStamp/Reviser/ReviseDate，但目前檢視的 `kcENT.clsCustomerBasis.EditCustomerContactor` 宣告未含此參數；請依現代 API 明確實作伺服器稽核/併發邏輯，不照搬此來源不一致處。
6. 更新 API 文件/migration 記錄、部署，通知公司端 pull 並在 Production 環境以正式 API 對接。公司端目前的 `/YMES/Customer/Contactors` 頁 Production 模式不使用假資料。

## ENT ET106 客戶出貨地址資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶出貨地址資料.md`、ET101/ET102 規格、`database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql` 及 `todolist_officepc.md` ET106 區塊。公司端已完成 Development Vue 雙 Grid 畫面。ET101 客戶基本資料 API 尚未完成時，先合併 ET101/ET102/ET106 的 ENT controller/repository，避免重複路由與 schema 工作。

### Schema / migration

- `TBLENTCUSTOMERSHIPADDR` 已有 Oracle source DDL/PG CREATE SQL（跟 ET101 一起交付）；ET106 新查 `TBLENTCUSTOMERSHIPDECLARATION` 原始 DDL/PG CREATE SQL 已追加在同兩個 reference 檔。
- Schema 比對後只新增缺少的表；報關表必須照來源無 PK、無 unique/index/FK 的結構建立，不可依表名猜 PK。欄位、NOT NULL、DEFAULT 與索引資料字典以 ET106 規格為準；`CREATEDATE DEFAULT SYSDATE`、`DATASTAMP DEFAULT 0` 都 nullable。
- 出貨地址表 PK `(CUSTOMERNO, SHIPADDR)`；報關表八欄 NOT NULL。來源兩表都沒有 Oracle FK。保留 YMES 現存擴充欄/索引。

### API / Service 待辦

1. `GET /api/ent/customer-basis` 支援 ET106 客戶查找；Vue 僅使用 `IssueState=2`。所有 API 都需檢查登入者 ENT 權限。
2. 地址端點：`GET/POST /api/ent/customer-basis/{customerNo}/ship-addresses`、`PUT /{customerNo}/ship-addresses/{oldShipAddr}`、`DELETE /{customerNo}/ship-addresses/{shipAddr}?flow=&shipCustomerName=&declarationFlag=&dataStamp=`。PUT 以舊 ShipAddr 定位，可在 Body 修改 ShipAddr；驗證 CustomerNo/ShipAddr NOT NULL、複合 PK 唯一及 Oracle 欄位長度。新/修改稽核由 server 產生，DataStamp compare-and-swap。
3. 報關端點：`GET/POST /api/ent/customer-basis/{customerNo}/ship-declarations`；`PUT/DELETE /{customerNo}/ship-declarations?shipTo=&flow=&overseaFlag=&declarationFlag=&bondFlag=&handCarryFlag=&dataStamp=`。原始表沒有主鍵且允許重複；自然定位值為七欄 CustomerNo、ShipTo、Flow、OverseaFlag、DeclarationFlag、BondFlag、HandCarryFlag。沿用舊 Core 對完全相同鍵列的多列更新/刪除效果，API 不得虛構 1:1 主鍵。DataStamp 新 API 可作併發檢查，但須處理來源 NULL。
4. 出貨地址刪除/修改在舊 `clsCustomerBasis` 未同步 cascade/update `TBLENTCUSTOMERSHIPDECLARATION`；兩表無 FK，故會有舊 ShipTo 孤兒值。先維持來源可觀察行為並在規格留下記錄，migration 不加 FK/cascade。
5. 表單的 Taker/Forwarder/Broker Lookup 分別依 WinForms `wsENT.LoadTakerBasis`、`LoadForwarderBasis`、`LoadBrokerBasis`，都只顯示 IssueState=2。統一使用 `GET /api/ent/taker-basis?issueState=2`、`/api/ent/forwarder-basis?issueState=2`、`/api/ent/broker-basis?issueState=2`；DTO 名稱欄位為 ContactorName。不要把 DEV picker 假資料回傳 Production。
6. API 驗證與日期/稽核欄位依規格實作，授予 `ymes_app` 最小 SELECT/INSERT/UPDATE/DELETE 權限，部署並通知公司端 pull 對接 Production。WinForms/Core 目前的 Edit 方法宣告不含 wsENT 傳入的 DataStamp/Reviser/ReviseDate 命名參數，依 Modern API 明確實作，不照搬來源版本差異。

## ENT ET103 報關行基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/報關行基本資料.md`、`todolist_officepc.md` ET103 區塊，以及 `database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。公司端已核對 WinForms、直連取得 Oracle DDL，並完成 Development Vue 頁 `/YMES/Customer/Brokers`。ET103 與 ET101/102/106 共用 ENT 範圍；若 controller/repository 尚未建立，請合併在同一 ENT API 結構中。

### Schema / migration

- Oracle `MESTEST.TBLENTBROKERBASIS` 已直接查得；Oracle source logical DDL 在 `database/reference/mestest_customer_basis_source_ddl.sql`，PostgreSQL 空表 CREATE SQL 在 `database/reference/mestest_customer_basis_create.sql`。
- 欄位：`BROKERNO varchar(50) NOT NULL PK`、`CONTACTORNAME varchar(50)`、`TELNO varchar(25)`、`FAXNO varchar(25)`、`EMAIL varchar(255)`、`ADDRESS varchar(500)`、`BANKNO varchar(20)`、`ISSUESTATE numeric(1,0) DEFAULT 0` nullable、`CREATOR varchar(10)`、`CREATEDATE timestamp DEFAULT LOCALTIMESTAMP` nullable、`DATASTAMP numeric(11,0) DEFAULT 0` nullable、`REVISER varchar(10)`、`REVISEDATE timestamp`。
- 唯一索引是 PK `BROKERNO`；無 FK/其他索引。`mestest_first_phase_create_tables.sql` 已校正這張表原先漏掉的 DEFAULT。先比對現存 YMES table/migrations，只補 schema 缺項，不覆蓋或刪除現有擴充。

### API /簽核待辦

1. 建立/延伸 ENT controller：`GET /api/ent/broker-basis?brokerNo=&issueState=` 回傳欄位對應前端 `Broker` DTO 的清單。IssueState 選填：主畫面 Vue 在前端篩選，ET104 的 Broker picker 傳 `issueState=2` 僅取已核准報關行；回傳資料須包含 IssueState/DataStamp。WinForms 舊查詢沒有 ORDER BY；API 可以 BrokerNo 穩定排序，作為明確顯示約定。
2. 新增 `POST /api/ent/broker-basis`，BrokerNo 必填且大寫正規化；IssueState=0，Creator/CreateDate/DataStamp 由 server 設定。欄位長度照 Oracle DDL；其餘聯絡資料可空白，空值採明確 NULL 語意。
3. 新增 `PUT /api/ent/broker-basis/{brokerNo}?dataStamp=`，只可修改 ContactorName/TelNo/FaxNo/Email/Address/BankNo，伺服器必須確認 IssueState=0；更新 Reviser/ReviseDate 並遞增 DataStamp。失敗時區分 404 與 409。
4. 新增 `DELETE /api/ent/broker-basis/{brokerNo}?issueState=&dataStamp=`：只接受目前狀態 0、-1、2，需同時驗證資料庫實際狀態。0/-1 在交易中實體刪除；2 改為 -1，並更新 Reviser/ReviseDate/DataStamp；狀態 1 回 409。舊 WinForms 對 0/-1 實體刪除雖傳 DataStamp，但舊 WebMethod 沒有把 stamp 傳入 DELETE SQL；新 API 請仍做 compare-and-swap 避免過期刪除。來源 DataStamp 可為 NULL，要能以 `IS NULL` 比對，更新成 `COALESCE(datastamp,0)+1`。
5. 新增 `POST /api/ent/broker-basis/{brokerNo}/approve?dataStamp=`，只接受狀態 0，需使用 YMES 現有 Issue/Job 簽核服務及 IssueNo `BROKER BASIS`。沒有 JobMap/Detail 時按舊規則直接轉 2；有 Detail 時建立 Issue Master、狀態轉 1、簽核通過到 2、退回到 0，寄送第一簽核群組通知。主檔狀態、簽核單、稽核與通知需使用既有交易/簽核服務，不可將 DEV 直接核准複製到 Production。
6. 所有端點檢查登入者 ENT 權限；設定 `ymes_app` 最小 CRUD 權限。更新 migration/API 文件後部署並通知公司端 pull。公司端 Production Vue 不使用假資料。

## ENT ET104 貨運行基本資料（公司端已完成 DEV UI/Oracle DDL，待家用端接手）

先讀 `docs/modules/ENT/貨運行基本資料.md`、ET103 報關行規格及 `todolist_officepc.md` ET104 進度。公司端已依 WinForms 原始碼完成 `/YMES/Customer/Forwarders` Vue DEV 頁，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得正式 DDL。ET104 與 ET101/102/106/103 共用 ENT API、schema/migration；不要另建重複 Controller。

### Schema / migration

- Oracle 原始 logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_customer_basis_create.sql`；first-phase reference 已同步修正 DEFAULT/欄位註解。
- `ymes.tblentforwarderbasis` 欄位與長度：ForwarderNo varchar(50) NOT NULL PK、ContactorName varchar(50)、TelNo varchar(25)、FaxNo varchar(25)、Email varchar(255)、Address varchar(500)、BrokerNo varchar(50) NOT NULL、BankNo varchar(20)、IssueState numeric(1,0) DEFAULT 0 nullable、Creator varchar(10)、CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable、ACNo varchar(50)、DataStamp numeric(11,0) DEFAULT 0 nullable、Reviser varchar(10)、ReviseDate timestamp。只有 ForwarderNo PK unique index；沒有 FK/其他 index。
- 先逐欄比對 YMES PostgreSQL schema 和 migration。缺表才新增 forward migration，已存在就保留擴充欄位，只補缺失 schema 差異；不可只依 `IF NOT EXISTS` 判斷已符合。執行 migration 前核對 DDL 不加 FK/index。

### API / 簽核待辦

1. 延伸 ENT API：`GET /api/ent/forwarder-basis?forwarderNo=&issueState=` 清單含完整欄位、IssueState/DataStamp；IssueState 選填供 ET106 lookup 篩選已簽核列；`POST /api/ent/forwarder-basis` 新增；`PUT /{forwarderNo}?dataStamp=` 修改 ContactorName/TelNo/FaxNo/Email/Address/BankNo/ACNo，ForwarderNo/BrokerNo 鎖定。
2. BrokerNo 必填，新增時查 `tblentbrokerbasis` 確認 BrokerNo 存在且 IssueState=2。ET103 `GET /api/ent/broker-basis` 必須支援選填 `issueState` query，讓本頁 picker 呼叫 `?issueState=2`；該 lookup 不可回 DEV 假資料到 Production。Oracle 無 FK，驗證必須在 API。
3. WinForms UI 允許 IssueState 0、2 開啟編輯，但舊 `wsENT.EditForwarderBasis` 對狀態 2 可能只回成功而不更新 Core。新 API 按 UI 意圖支援 0/2 的明細修改，鍵值不可改，1/-1 拒絕；記錄並避免重製此舊服務缺陷。
4. `DELETE /{forwarderNo}?issueState=&dataStamp=`：0/-1 交易中實體刪除；2 改 -1 並更新稽核/DataStamp；1 回 409。DataStamp 原值 NULL 時以 `IS NULL` compare-and-swap，遞增使用 `COALESCE(DataStamp,0)+1`。每筆端點單筆操作，前端多選逐筆送出、首錯停止並重載。
5. `POST /{forwarderNo}/approve?dataStamp=` 僅狀態 0，使用既有 Issue/Job 簽核服務及 IssueNo `FORWARDER BASIS`；無 Job/Detail 直接轉 2，有簽核 Detail 依流程轉 1、核准到 2、退回到 0。Production 不可照抄 DEV 直接核准。
6. 所有 API 驗證 ENT 權限、欄位長度、伺服器身分/時間 audit、DataStamp 併發。更新 migration/API 文件、授予 `ymes_app` 最小權限，完成 API 與既有簽核流程後部署，再通知公司端切 Production 驗收。

## ENT ET105 取貨公司基本資料（公司端已完成 DEV UI/Oracle DDL，待家用端接手）

先讀 `docs/modules/ENT/取貨公司基本資料.md`、`todolist_officepc.md` ET105 區塊與 ET101/ET103/ET104 ENT API 待辦。Vue DEV 頁 `/YMES/Customer/Takers` 已完成；Oracle DDL 是公司端透過 Windows PowerShell 5.1 + ODP.NET 直接唯讀取得。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`；PostgreSQL CREATE SQL：`database/reference/mestest_customer_basis_create.sql`；`mestest_first_phase_create_tables.sql` 已補 IssueState/CreateDate/DataStamp defaults 並移除推測註解。
- 欄位：TakerNo varchar(50) NOT NULL PK、ContactorName varchar(50)、TelNo varchar(25)、FaxNo varchar(25)、Email varchar(255)、Address varchar(500)、BankNo varchar(20)、IssueState numeric(1,0) DEFAULT 0 nullable、Creator varchar(10)、CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable、DataStamp numeric(11,0) DEFAULT 0 nullable、Reviser varchar(10)、ReviseDate timestamp。
- 只有 TakerNo PK unique index，無 FK/其他 index。先逐欄比較 YMES schema/migration，缺表/欄位才建立 forward migration，保留已存在的 YMES 擴充，不加入來源沒有的 FK/index。

### API / 簽核待辦

1. 延伸共用 ENT API：`GET /api/ent/taker-basis?takerNo=&issueState=`、`POST /api/ent/taker-basis`、`PUT /api/ent/taker-basis/{takerNo}?dataStamp=`。IssueState 選填供 ET106 lookup 篩選已簽核列。TakerNo 必填、大寫、唯一；只有 IssueState=0 可修改 TakerNo 以外的聯絡資料；所有長度依 Oracle DDL。
2. `DELETE /{takerNo}?issueState=&dataStamp=`：0/-1 交易中實體刪除，2 改 -1，1 回 409。驗證 DB 實際狀態與 DataStamp；來源 DataStamp nullable，以 `IS NULL` 比對並以 `COALESCE(DataStamp,0)+1` 遞增。舊 WebMethod 0/-1 delete 沒有把 DataStamp 傳進 Delete SQL，新 API 必須補上併發檢查。
3. `POST /{takerNo}/approve?dataStamp=` 只接受狀態 0，IssueNo=`TAKER BASIS`，沿用 YMES Issue/Job 簽核服務；無 Job/Detail 時直達 2，有簽核 Detail 時建立簽核、狀態 1、通過 2、退回 0。
4. WinForms/WebService 與本 checkout 的 Core 參數不一致：wsENT Edit/SetIssueState 傳 DataStamp/Reviser/ReviseDate，但 `kcENT.clsCustomerBasis` 宣告沒有這些參數；Core Edit 不寫 audit/DataStamp，IssueState helper 也只更新狀態。Production 新 API 依規格完成稽核/併發，不複製舊缺漏。
5. 所有端點檢查登入者 ENT 權限；server 產生 audit 欄位與時間，使用 PostgreSQL transaction 和 compare-and-swap。更新 API/migration 文件、授予 `ymes_app` 最小權限，部署後通知公司端對接 Production。

## ENT ET201 外包商主檔與連絡人（公司端已完成 DEV UI/Oracle DDL，待家用端接手）

先讀 `docs/modules/ENT/外包商基本資料.md`。Vue DEV 頁為 `/YMES/Customer/Subcontractors`、FunctionNo `SUBCONTRACTOR BASIS`。UI/規格根據 `frmSubcontractor`、兩個 Define Form、wsENT/Core 及 Oracle 來源 DDL。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_subcontractor_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_subcontractor_create.sql`。只供逐欄比對 YMES 現況，不表示 YMES 尚無表。
- `ymes.tblentsubcontractor`: SubcontractorNo varchar(20) NOT NULL PK; SubcontractorName varchar(220) NOT NULL; Description varchar(255); Creator varchar(10); CreateDate timestamp; IssueState numeric(1,0) DEFAULT 0 NOT NULL; ERPNo varchar(50); DataStamp numeric(11,0) DEFAULT 0 nullable; Reviser varchar(10); ReviseDate timestamp。
- `ymes.tblentsubcontractorcont`: SubcontractorNo varchar(20) NOT NULL; ContactorName varchar(50) NOT NULL; TelNo/FaxNo varchar(20); Title varchar(20); Address/Email varchar(50); Description varchar(255); DataStamp numeric(11,0) DEFAULT 0 nullable; Creator varchar(10); CreateDate timestamp; Reviser varchar(10); ReviseDate timestamp; composite PK `(SubcontractorNo, ContactorName)`。
- 兩張 Oracle 表都僅 PK unique index，無 FK/其他 index。先比對實際 YMES schema/migrations；保留 YMES 額外欄位/索引，不自行加 FK。舊 Core 的實體刪除會先刪連絡人，再刪主檔，新的 API 需用同一 PostgreSQL transaction。

### API / 業務規則

1. 新增 ENT API：`GET /api/ent/subcontractor-basis?subcontractorNo=`、`POST /api/ent/subcontractor-basis`、`PUT /{subcontractorNo}?dataStamp=`、`DELETE /{subcontractorNo}?issueState=&dataStamp=`、`POST /{subcontractorNo}/approve?dataStamp=`。
2. 連絡人端點：`GET/POST /{subcontractorNo}/contacts`、`PUT/DELETE /{subcontractorNo}/contacts/{contactorName}?dataStamp=`。回傳完整 DTO 與 DataStamp；主鍵姓名只能在新增/複製時指定，修改時不可更換。
3. 主檔必填 SubcontractorNo/Name；新增狀態 0；只有狀態 0 可修改；編號與連絡人姓名大寫。長度依 Oracle DDL。連絡人必填 ContactorName，另外欄位皆可空。
4. 主檔刪除：狀態 0/-1 在一個交易內比對 DataStamp 後刪除連絡人及主檔；狀態 2 改 -1、更新 audit/DataStamp 且保留明細；狀態 1 回 409。批次是前端逐筆請求，遇錯停止並回報已完成筆數。
5. 核准只接受 IssueState=0，IssueNo=`SUBCONTRACTOR BASIS`。重用 YMES Issue/Job 簽核服務：無 Job/Detail 直達 2；有簽核明細則狀態 1，通過 2、退回 0。Production 不得照 DEV 直接核准。
6. DataStamp nullable/default 0；CAS 同時支援原值 NULL（`IS NULL`）及數值、遞增使用 `COALESCE(DataStamp,0)+1`。所有 API 檢查登入者 ENT 權限、主檔存在、欄位長度/唯一鍵與伺服器 audit。Core/wsENT 此 checkout 的命名參數存在版本差異，依本文件的新 API 契約統一補足 audit/CAS，不照搬未寫 audit 的舊 SQL。
7. 公司端尚未 build/test/瀏覽器驗收。API/schema 完成並部署後，通知公司端 pull 並對接 Production 驗收。

## ENT ET202 外包項目設定（公司端 DEV 完成，家用端接手 API/schema）

先讀 `docs/modules/ENT/外包項目設定.md`、`todolist_officepc.md` 的 ET202 區塊，以及 Oracle 來源 `database/reference/mestest_entositem_source_ddl.sql`、PostgreSQL 空表 `database/reference/mestest_entositem_create.sql`。公司端按 WinForms `frmENTOSItem`/`frmENTOSItemDef` 完成 Vue DEV 頁 `/YMES/Customer/OSItems`。Oracle DDL 是公司端以 PowerShell 5.1 + ODP.NET 直連受保護連線唯讀取得。

### Schema / Migration

- Oracle 主表 `MESTEST.TBLENTOSITEM` 11 欄：OSItemNo varchar(2) NOT NULL PK；OSItemName varchar(50) NOT NULL；PlanDays numeric(3,0) NOT NULL；ProcessNo varchar(30) NOT NULL；ReturnType numeric(1,0) NOT NULL；Description varchar(255)；Creator varchar(10)；CreateDate timestamp；DataStamp numeric(11,0) DEFAULT 0 nullable；Reviser varchar(10)；ReviseDate timestamp。
- Oracle 只有 OSItemNo PK unique index，沒有 FK 或額外索引。Create SQL 是空表定義；先查 YMES 現有 schema、migration 和 API 使用的 schema 大小寫，再只建立缺漏項目，不要盲目執行 IF NOT EXISTS。
- Oracle 沒有 IssueState。PlanDays、ReturnType 不可另加來源不存在的欄位/default；Oracle DATE 對應 PostgreSQL timestamp without time zone。

### API / 業務規則

1. ENT API：`GET /api/ent/os-items?osItemNo=`、`POST /api/ent/os-items`、`PUT /api/ent/os-items/{osItemNo}?dataStamp=`、`DELETE /api/ent/os-items/{osItemNo}?dataStamp=`。GET 空條件列全體，有值依編號查詢。JSON 欄位為 `osItemNo, osItemName, planDays, processNo, returnType, description`；POST/PUT 回傳完整 DTO。
2. 必填與長度依 Oracle DDL；WinForms 輸入上限 OSItemNo 2、OSItemName 50、PlanDays 0–999 整數、ProcessNo 25（DB 上限 30）、Description 255。OSItemNo 儲存前大寫；ReturnType 只接受 0/1，新增預設 0。
3. ProcessNo 必須存在於已簽核 `IssueState=2` 且 `ProcessClass=3` 的流程。查找用既有 `GET /api/processes?issueState=2`，以 DTO ProcessClass 篩選；舊 WinForms LoadProcess 未限制 CurVersion=1，不要額外只留目前版本。
4. DataStamp nullable/default 0；Update/Delete 需 transaction/CAS，支援 NULL 原值和數值，更新 `COALESCE(datastamp,0)+1`。新增/更新的 Creator/CreateDate/Reviser/ReviseDate 由 API 設定，不接受用戶端 audit。
5. 舊 ws Edit 有送 DataStamp，但目前 Core `EditENTOSItem` 簽章沒有 CAS/audit；舊 Core Delete 只按 OSItemNo 實體刪除。新 API 補完整 CAS 與伺服器 audit。多選刪除由前端逐筆呼叫，首個錯誤即停止並回報已完成筆數。
6. 本功能沒有簽核/IssueState；所有端點檢查登入者 ENT 權限、資料庫欄位長度、必填、ReturnType/PlanDays 範圍與流程參照。
7. 公司端 Development 使用 localStorage 假資料；Production 只呼叫 API。API/schema 完成、部署並提供可用環境後，通知公司端 pull 最新版對接驗收。
## SMD SD107 生產線別基本資料（公司端 DEV 已完成，家用端接手 API/schema）

- [x] 2026-11-17 遠端 schema 核對與 migration：`TBLSMDPDLINEBASIS` 已存在，欄位型態、長度、NULL 與 PDLineNo PK 符合來源；只補 PDLineStatus、CreateDate、DataStamp 來源 DEFAULT 與本功能 CRUD 權限，未重建表。Migration 前備份 `/opt/ymes/backups/ymes_before_0018_smd_pdline_20261117.dump`，`pg_restore --list` 驗證通過。
- [x] 完成並部署 GET/POST/PUT/DELETE `/api/smd/pd-line-basis`，全狀態查詢、名稱/說明驗證、伺服器 audit、CAS、狀態 2 停用及舊規則狀態 0/1/-1 實體刪除。
- [ ] 正式 PDLineStatus ValueList 與共用 Issue/Job 核准結構尚未交付；目前 `/options` 與 `/approve` 分別回 503 fail closed。收到公司端核實項目前，不推定正式標籤、不以 0→2 假核准。

先讀 docs/modules/SMD/生產線別基本資料.md。Vue 頁為 /YMES/SMD/ProductionLines，FunctionNo=SMD PDLINE BASIS；公司端以本機假資料完成操作，不代表 Production API 已存在。

### Schema / migration

- Oracle source DDL：database/reference/mestest_smd_pdline_basis_source_ddl.sql；PostgreSQL 空表 CREATE SQL：database/reference/mestest_smd_pdline_basis_create.sql。DDL 是公司端 2026-11-16 以 Windows PowerShell 5.1 + ODP.NET 直連受保護連線唯讀取得；不要重問連線資訊，也不要在 MES Oracle 建表。
- Oracle 表 MESTEST.TBLSMDPDLINEBASIS 欄位：PDLineNo varchar(50) NOT NULL PK；PDLineName varchar(50) NOT NULL；PDLineStatus numeric(1,0) DEFAULT 1 NOT NULL；IssueState numeric(1,0) NOT NULL 且無 default；Description varchar(255)；Creator varchar(10)；CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable；Reviser varchar(10)；ReviseDate timestamp；DataStamp numeric(11,0) DEFAULT 0 nullable。
- Oracle 僅一個 PDLineNo PK unique index，沒有 FK 或其他索引。DATE 對應 timestamp without time zone。此表已出現在 first-phase reference；先查 YMES 實際 schema/migration。缺漏才新增 forward migration，保留既有資料和 YMES 擴充欄位；如 first-phase 曾被套用且 defaults 不符，採 forward migration 修正，不重建表。
- PostgreSQL CREATE 檔是空表定義，不是部署 migration；不要無條件執行 IF NOT EXISTS 取代欄位差異檢查。

### API / 行為

1. 新增 SMD API：GET /api/smd/pd-line-basis?pdLineNo=（回傳所有四種 IssueState，PDLineNo 大小寫不敏感部分查詢，依 PDLineNo 排序）；GET /api/smd/pd-line-basis/options 回傳 { pdLineStatuses: [{ value, label }] }。狀態選項需沿用正式 ValueListProvider，不能把 DEV 標籤當正式文案。
2. POST /api/smd/pd-line-basis 新增 IssueState=0；PDLineNo、PDLineName 必填，長度各 50；PDLineNo 大寫，PDLineStatus 只接受 0/1/2，WinForms 新增預設 1；說明上限 255。Creator/CreateDate/DataStamp 由伺服器建立。
3. PUT /api/smd/pd-line-basis/{pdLineNo}?issueState=0&dataStamp= 只允許 IssueState=0，主鍵不可改；更新名稱/狀態/說明並由伺服器寫 Reviser/ReviseDate/DataStamp。舊 Core 空白 Description 不更新也不清除；需維持相容。
4. DELETE /api/smd/pd-line-basis/{pdLineNo}?issueState=&dataStamp= 按舊 wsSMD 規則：0、1、-1 實體刪除，2 轉為 -1 並寫修改稽核/DataStamp。注意此功能舊 Service 明確接受 IssueState=1 實體刪除；若要改成阻擋簽核中刪除，需在 API 文件標示行為修正。交易/CAS 需檢查傳入與實際狀態。
5. POST /api/smd/pd-line-basis/{pdLineNo}/approve?issueState=0&dataStamp= 僅接受 0，IssueNo=SMD PDLINE BASIS。使用 YMES 共用 Issue/Job 簽核服務：無 Job/Detail 依設定直達 2；有流程時先 1，核准 2、退回 0，通知第一簽核群組。不可照搬 DEV 直接切成 2。
6. 所有端點檢查登入與 SMD 功能權限、唯一鍵/欄位範圍及資料狀態。DataStamp nullable/default 0；NULL 原值需以 IS NULL CAS，更新遞增 COALESCE(datastamp,0)+1。批次 UI 逐筆呼叫，第一個錯誤停止並保留已成功筆數。
7. API/schema/migration 完成並部署後，通知公司端 pull 最新 main 對接 Production；不要把假資料回退用於 Production。

## SMD SD108 設備狀態監控_區域（公司端 UI handoff）

- 公司端已新增 Development Vue 頁與規格：docs/modules/SMD/設備狀態監控_區域.md、web/src/components/AreaShowMaster.vue。Production 不使用 localStorage。
- Oracle 表 DDL/PG 空表 SQL：database/reference/mestest_smd_area_shift_source_ddl.sql、mestest_smd_area_shift_create.sql，表為 TBLSMDAREASHIFT、TBLSMDAREAOPERATOR。先比對 YMES schema；只對缺漏新增 migration。
- 實作班別/人員 API、DataStamp CAS、以 transaction 同步 Operator 明細、複製班別、刪除主從資料、同部門有效人員驗證及舊 ChkAreaShiftLoginState 規則（tblWIPAreaOperatorState 有登入即拒絕修改）。當來源 DataStamp 為 NULL，API 接收 `dataStamp=0&dataStampIsNull=true` 並以 `IS NULL` CAS；有值時則比較該值。
- SD104 layout API 仍未完成；SD108 首頁依既有 GET /api/smd/areas/{areaNo}/layout 讀配置。另需 BaseArea lookup 與 shift-context/shift-options API。完整端點與 DTO 見 SD108 規格。
- WIP 即時狀態與歷史由既有 WP_AreaOperatorState/WP_AreaOperatorLog 提供；補查 YMES WIP API 後沿用或實作 GET /api/wip/areas/{areaNo}/operator-state 與 /operator-log。不要將 DEV 假資料當正式狀態。

## SMD SD109 工位基本資料（公司端 UI handoff）

- 公司端已新增 Development Vue 頁與規格：docs/modules/SMD/工位基本資料.md、web/src/components/PositionBasisMaster.vue。Production 不使用 localStorage。
- Oracle TBLSMDPDLINEPOSITIONBASIS 的來源 DDL/PG 空表 SQL 已加入 reference；先比對實際 YMES schema，缺漏/預設差異才新增 migration。
- 實作複合鍵 (PDLineNo,PositionNo) GET/POST/PUT/DELETE、DataStamp CAS、audit，並驗證 PDLineNo 為已核准一般作業區（AreaClass=0、AreaType=0、IssueState=2）。DataStamp 為 NULL 時按 `dataStamp=0&dataStampIsNull=true` 以 `IS NULL` CAS；API 路徑/DTO 見 SD109 規格。
- eSOP 選項由 Execute_eSOP 系統參數控制；瀏覽器不可直接呼叫 eSOP，請由 API 使用現有 ERP/EAI 設定代理 station.get。
- first-phase CREATE SQL 已修正 CreateDate DEFAULT LOCALTIMESTAMP NOT NULL、DataStamp DEFAULT 0；家用端實際 schema 仍須用 migration 比對與修正，不要重跑 bootstrap。

家用端完成後更新本檔與兩份模組規格，部署 API/schema，再通知公司端 pull 進行 Production 對接。

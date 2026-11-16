# YMES / MES 資料庫連線與憑證處理

## 連線用途與邊界

| 資料來源 | 用途 | 應用程式連線方式 |
| --- | --- | --- |
| MES Oracle，來源 schema MESTEST | 由使用者授權後查詢資料字典、原始表結構及必要的來源邏輯；例如 ALL_TABLES、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、DBMS_METADATA.GET_DDL | Vue Web 與 YMES API 不連 MES Oracle。公司電腦的來源查詢使用者提供的既有唯讀連線資訊 |
| YMES PostgreSQL，schema YMES | YMES API 的正式資料庫 | 由 .NET API 透過 Npgsql 連線；資料庫/migration/API 由家用電腦維護 |
| YMES Web | 顯示畫面並呼叫 API | 不保存、不讀取 Oracle 或 PostgreSQL 連線字串 |

MES Oracle MESTEST 是原生 MES 來源；YMES PostgreSQL 是家用端建置的復刻資料庫。兩者是不同資料庫、不同用途：公司端唯讀查 Oracle 的資料字典、原始 DDL 與舊系統規則；家用端透過 PPK SSH 管理 YMES PostgreSQL，將核實過的原生 MES schema 轉成 PostgreSQL 結構並重建 API 行為。`database/reference/*_create.sql` 是這種 PostgreSQL 建表交接，不得在原生 Oracle 執行。這些 CREATE SQL 只包含結構，不會複製 MES 業務資料；任何業務資料匯入都需另外規劃與授權。

## 已確認的 YMES API 設定名稱

.NET API 從設定提供者取得連線字串，支援以下名稱：

- ConnectionStrings__YMes（對應 .NET ConnectionStrings:YMes）
- YMES_DATABASE_URL

實際值只放在部署 Secret Store、受保護的本機環境變數或未追蹤的本機設定檔；不得寫進 Git 追蹤的 appsettings、Markdown、SQL 註解或終端輸出。

## MES Oracle 來源連線

- 使用者先前提供的 Oracle 設定已從本機受保護附件安全讀取，無須再次索取。公司電腦以 Windows PowerShell 5.1 和 Oracle.ManagedDataAccess 直接連線查詢；不可改用 DBeaver 螢幕操作作為主要 DDL 擷取方式。
- 實際 Oracle 主機、帳號及密碼不放在這份受 Git 追蹤的文件中。這是為遵守 docs/DEVELOPMENT_STYLE.md 的憑證規則，並避免 push 到 GitHub。
- Windows PowerShell 5.1 已成功連到 MESTEST；Oracle.ManagedDataAccess 驅動位於舊 MES 工作目錄。查詢只讀取資料字典、DDL 與選單中繼資料，不匯出 MES 業務資料。
- 來源查詢只用於確認結構/規則及產生 CREATE SQL；不得在 MES Oracle 執行 DDL 或寫入業務資料。
- 連線字串只由本機受保護設定提供，不複製到工作目錄、終端輸出或 Git。家用電腦如需直接查來源，先檢查其受保護設定；不得從 Git 文件還原密碼。

### 本次公司電腦盤點狀態（2026-11-13）

- `D:\iMES\CSiMES\.equipment-settings-ddl.tmp.sql` 內含 19 張表的 Oracle DDL，但不含 `TBLEQPEQUIPMENTBASIS` 與 `TBLEQPPARAMETERBASIS`。
- 使用者於 2026-11-13 提供上述兩表的原始 Oracle CREATE TABLE 與主鍵唯一索引 DDL；公司端已轉成 `database/reference/mestest_equipment_basis_create.sql` 及 `database/reference/mestest_equipment_parameter_basis_create.sql`，並校正 bootstrap。這兩張表不再列為待取得 DDL。
- PowerShell 5.1 已以唯讀方式成功查詢 MESTEST 的 1,287 張表；`DBMS_METADATA.GET_DDL` 擷取及轉換成果依模組放在 `database/reference/`。本次已保存 8 張物料表原始 DDL/PG CREATE，以及 18 張作業站相關表和 3 張系統選單表原始 DDL/PG CREATE。
- Oracle `TBLSYSMODULE`、`TBLSYSMENUITEM`、`TBLSYSFUNCTION` 只查系統目錄中繼資料：20 個 Module、601 筆 MenuItem、553 筆 Function；盤點摘要與完整功能列見 `docs/MES_MENU_FUNCTION_INVENTORY.md`。第一層標題已從 `Customer/Production/MESClient/mesresource.zh-cht.resources` 依 `{ModuleNo}Module` 翻譯鍵核實；`TBLSYSMODULE` 沒有 ModuleName 欄位。
- 對應目錄資料的冪等 upsert SQL 為 `database/reference/mestest_navigation_catalog_seed.sql`；僅含 Module/Menu/Function 設定中繼資料，不刪除既有目錄列、不含 MES 業務資料。家用電腦確認 YMES schema 與現有自訂路由後，再整合為 migration。
- 作業站 `TBLOPBASIS`、`TBLOPGROUPDETAIL` 及服務明確使用的 16 張作業站子表原始 Oracle DDL 已擷取並附於 `database/reference/mestest_operation_and_system_source_ddl.sql`；對應 PG 空表 SQL 為 `database/reference/mestest_operation_and_system_create.sql`。
- 已再查這 21 張作業站/系統表的 Oracle index/constraint 字典：每表只有一個 PK unique index，沒有額外獨立索引；29 張作業站、系統、物料表各有一個 PK，沒有 FK (`R`) constraint。NOT NULL constraints 保留在來源 DDL與 PG CREATE SQL。
- 物料主檔 8 張來源表的原始 Oracle DDL 與 PG 空表 SQL 分別為 `database/reference/mestest_mtl_master_source_ddl.sql`、`database/reference/mestest_mtl_master_create.sql`。兩份 CREATE SQL 均只含結構，不含 MES 業務資料；套用前由家用電腦比對實際 YMES schema。



### 更新 2026-11-16：設備類別配件目錄

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得 MESTEST.TBLEQPACCESSORYTYPE 的 DBMS_METADATA DDL、9 欄 nullable/default、PK、index 與 FK 字典。
- 舊 frmEQPTypeACCType 呼叫 LoadAccessoryType(IssueState=2)；clsAccBasis.ShowAccessoryType 的查詢還排除 AccessoryType IS NULL，沒有指定 AccessoryCategory。
- PK 為 ACCESSORYTYPE，唯一索引僅 PK index，無額外索引/FK。Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql、mestest_equipment_type_accessory_catalog_create.sql。家用端仍需先核對 YMES schema；本次只讀結構，未讀業務資料。

### 更新 2026-11-16：設備群組 SMD 區域映射

- 以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀取得 `MESTEST.TBLSMDAREARELATION`、`MESTEST.TBLSMDSMTAREAEQPMAP` 的 DBMS_METADATA DDL，並核對欄位、NULL、PK、索引及 FK。
- `TBLSMDAREARELATION` 有 PK `(BASEAREANO, OBJECTNO)` 及非唯一索引 `(CONTAINAREANO, OBJECTTYPE)`；`TBLSMDSMTAREAEQPMAP` 的 PK 為 `(BASEAREANO, AREANO, EQUIPMENTNO)`。兩表沒有 FK。
- Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_equipment_group_smd_source_ddl.sql`、`database/reference/mestest_equipment_group_smd_create.sql`。家用端仍須比對實際 YMES schema；本次只讀結構，未讀取業務資料。

### 更新 2026-11-16：SMD SD104 區域基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線直接唯讀查詢 MESTEST.TBLSMDAREABASIS、TBLSMDAREAINVENTORY、TBLSMDAREAMAP 的 DBMS_METADATA DDL，並核對欄位 NULL/default、PK、索引與 FK 字典。
- 三張表只有 PK unique index，沒有外鍵或其他索引。`TBLSMDAREABASIS.BACKGROUNDIMAGE` 是 BLOB；`TBLSMDAREABASIS.DATASTAMP`、`TBLSMDAREAINVENTORY.DATASTAMP` DEFAULT 0 但 nullable。
- 原始 logical Oracle DDL 與 PostgreSQL 空表 CREATE SQL 已保存於 `database/reference/mestest_smd_area_basis_source_ddl.sql`、`database/reference/mestest_smd_area_basis_create.sql`。`TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 的 DDL/CREATE 沿用設備群組來源檔。只查資料字典，沒有讀 MES 業務資料，也沒有在 Oracle 執行 DDL。
- 主表 Form/Service 與配置交易/API 待辦見 `docs/modules/SMD/區域基本資料.md`。SD105「區域庫房基本設定」沿用此處已查的 `TBLSMDAREAINVENTORY` DDL/CREATE SQL，沒有重複查詢或新增 table SQL；規格見 `docs/modules/SMD/區域庫房基本設定.md`。連線值僅使用本機受保護設定，未輸出到文件或 Git。

### 更新 2026-11-16：SMD SD106 過帳電腦之區域

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀取得 `MESTEST.TBLSMDCOMPUTERAREABASIS`、`TBLSMDCOMPUTERAREADETAIL` DBMS_METADATA DDL，並查欄位 NULL/default、PK、索引與 FK 字典。
- 主檔 PK `(ComputerNo,FactoryNo)`；明細 PK `(ComputerNo,FactoryNo,AreaNo)`；兩表僅有 PK unique index，沒有 FK 或額外索引。DataStamp 是 NUMBER(11,0) DEFAULT 0 nullable。
- Oracle logical source DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_smd_computer_area_source_ddl.sql`、`mestest_smd_computer_area_create.sql`。只讀資料字典，沒有讀取 MES 業務資料或在 Oracle 執行 DDL；連線值沒有輸出或存檔。
- WinForms、Service/Core、Vue 畫面與 API handoff 規格見 `docs/modules/SMD/過帳電腦之區域.md`。家用端先比對 YMES 現有 schema/migrations，再補缺項；不要把 DEV 假資料寫進正式 DB。
### 更新 2026-11-16：設備工程群組成員表

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查得 `MESTEST.TBLUSRUSERGROUP` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES` 與 `ALL_IND_COLUMNS`。
- 結構為 6 欄；PK `(USERNO, GROUPNO, GROUPTYPE, AGENTOF)`；`GROUPTYPE NUMBER(1,0) DEFAULT 0`；`AGENTOF` 雖沒有 inline NOT NULL 宣告，仍由 PK 強制 NOT NULL。唯一索引只有 PK index，沒有額外 index/FK。
- 原始 Oracle DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_equipment_engineer_group_member_source_ddl.sql`、`database/reference/mestest_equipment_engineer_group_member_create.sql`。目標 YMES schema 尚待家用電腦核對；此查詢只讀結構，未讀取業務資料。

## PostgreSQL / API 操作

- 公司電腦只做 Vue UI、選單、表單及 API 呼叫；不要在 Web 專案加入資料庫驅動或連線字串。
- 家用電腦負責 YMES PostgreSQL migration、API repository、DTO 及部署；先讀本文件與 todolist_homepc.md。
- 家用電腦可經 SSH 使用資料庫管理權限維護 YMES PostgreSQL。API 的 `ymes_app` 是執行帳號，權限不足以執行 DDL 時，應由家用端使用 SSH 管理入口操作；不可因此把資料庫工作交給公司端。
- DATABASE_URL、ConnectionStrings__YMes、SSH 私鑰、Oracle 帳密及其他憑證不得 commit、push、寫入 Markdown 或貼在聊天輸出。
- DDL handoff 只包含 schema 結構；套用到 YMES 前由家用電腦檢查 migration 順序、現有表、權限與備份。

## 相關設定來源

- 開發分工與禁止提交憑證規則：[docs/DEVELOPMENT_STYLE.md](DEVELOPMENT_STYLE.md)
- API 啟動連線設定：src/YMes.Api/Program.cs
- 部署環境範本：deploy/compose.yaml（檢視設定變數名稱時不得輸出 secret 值）
- Oracle CREATE SQL handoff：database/reference/

### 更新 2026-11-16：EP202 配件類別相關表

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢 MESTEST 的 TBLEQPACCESSORYTYPE、TBLEQPACCESSORYCATEGORY、TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYLIFE DDL、欄位 NULL/default、PK、索引與 FK 字典。
- Oracle 原始 DDL/PG 空表 CREATE SQL：database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql、mestest_equipment_type_accessory_catalog_create.sql、mestest_accessory_type_dependencies_source_ddl.sql、mestest_accessory_type_dependencies_create.sql。
- 四表均無額外索引及 FK；只有 PK 唯一索引。此批查詢只讀結構，未讀 MES 業務資料。家用端執行前需比對實際 YMES schema，缺表才經 migration 建立。

### 更新 2026-11-16：EP201 配件分類與 EP204 可用狀態

- 公司端用 PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀呼叫 DBMS_METADATA.GET_DDL，核實 MESTEST.TBLEQPACCCATEGORYSTATEBASIS、TBLEQPACCSTATEBASIS；另核對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- EP204 原始 DDL與 PostgreSQL 空表 CREATE SQL：mestest_accessory_category_state_source_ddl.sql、mestest_accessory_category_state_create.sql；lookup DDL/CREATE：mestest_accessory_state_source_ddl.sql、mestest_accessory_state_create.sql。
- TBLEQPACCCATEGORYSTATEBASIS PK 為 (ACCESSORYCATEGORY, ACCESSORYSTATE, OPERATIONNAME)，只有 PK 唯一索引，沒有 FK/額外索引。OPERATIONNAME 因 PK 而 NOT NULL；DATASTAMP DEFAULT 0 但 nullable。
- TBLEQPACCSTATEBASIS PK 為 ACCESSORYSTATE，STATETYPE DEFAULT 1 NOT NULL，DATASTAMP DEFAULT 0 但 nullable；僅 PK 唯一索引，沒有額外 FK/index。
- 已修正 mestest_first_phase_create_tables.sql 中 lookup 表曾以推測註解呈現的欄位/default，並有單表 CREATE SQL 供家用端比對。套用前仍須檢查 YMES 現有 schema；缺表才 migration。本次只查結構，未讀取或匯出 MES 業務資料。
- EP201 主表 TBLEQPACCESSORYCATEGORY 的 DDL/CREATE SQL 已在 EP202 共用依賴檔案中；家用端規格見 todolist_homepc.md 的 EP201/EP204 交接段落。

### 更新 2026-11-16：EP203 配件基本資料 Oracle DDL

- 公司端使用 Windows PowerShell 5.1 與本機已安裝的 Oracle.ManagedDataAccess.dll，從本機受保護設定讀取測試連線，唯讀呼叫 DBMS_METADATA.GET_DDL；以 ALL_INDEXES 取得 6 張表的 8 個 index 名稱，再逐個取 DDL。
- 本次涉及 TBLEQPACCESSORYBASIS、TBLEMSACCESSORYSTATE、TBLEMSACCESSORYSTATELOG、TBLEQPACCSTATECONTROL、TBLEMSEQPACCSTATE、TBLEMSEQPACCSTATELOG；原始 DDL 及 PG 空表 SQL 已交付 database/reference/。
- 僅查結構，沒有查詢 MES 業務資料，也沒有在 Oracle 執行 DDL。連線字串、IP、帳號、密碼及 SID 僅留在本機受保護設定，不寫入 Markdown/Git。

### 更新 2026-11-16：EP301 載具分類資料

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接查詢 MESTEST.TBLEQPCARRIERCATEGORY 的 DBMS_METADATA.GET_DDL，並查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得 8 欄：CarrierCategory VARCHAR2(50) NOT NULL；Description VARCHAR2(255)；Creator/Reviser VARCHAR2(10)；CreateDate/ReviseDate DATE；IssueState NUMBER(1,0) DEFAULT 0 NOT NULL；DataStamp NUMBER(11,0) DEFAULT 0 nullable。PK 為 CarrierCategory；唯一索引只有 PK index，沒有其他索引或 FK。
- Oracle 原始 DDL/PK index 與 PostgreSQL 空表 CREATE SQL 分別為 `database/reference/mestest_carrier_category_source_ddl.sql`、`database/reference/mestest_carrier_category_create.sql`。只查結構，未讀取業務資料或執行 Oracle DDL；家用端套用前先核對 YMES schema。
- PowerShell 執行時應明確呼叫 Windows PowerShell 5.1；目前互動終端預設 PowerShell 7，直接載入既有 Oracle.ManagedDataAccess.dll 會因 BinaryFormatter runtime 相容性而無法初始化。連線設定仍由本機受保護附件讀取，不輸出或記錄連線值。

### 更新 2026-11-16：EP302 載具類別資料

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接唯讀查詢 MESTEST.TBLEQPCARRIERTYPE 的 DBMS_METADATA.GET_DDL，並核對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得 13 欄；PK 為 CARRIERTYPE，唯一索引只有 PK index，沒有其他索引或 FK。Oracle 原始 DDL/PG 空表 CREATE SQL 為 `database/reference/mestest_carrier_type_source_ddl.sql`、`database/reference/mestest_carrier_type_create.sql`。
- 只查結構，未讀取 MES 業務資料或執行 Oracle DDL；連線資訊仍只由本機受保護設定讀取，不輸出、不保存到 Git。

### 更新 2026-11-16：EP303 載具基本資料與選項依賴

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定唯讀查詢 MESTEST.TBLEQPCARRIERBASIS、TBLWIPCarrierState、TBLSMDFactoryBasis 的 DBMS_METADATA DDL 及欄位、NOT NULL/default、PK、index 字典。
- 載具主表 DataStamp 為 nullable 且 DEFAULT 0；IssueState DEFAULT 0 NOT NULL。WIPCarrierState 主鍵為 CarrierNo；FactoryBasis IssueState DEFAULT 0 NOT NULL，DataStamp DEFAULT 0 nullable。三表均只有 PK unique index，沒有 FK。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 已保存於 database/reference/mestest_carrier_basis_source_ddl.sql、mestest_carrier_basis_create.sql、mestest_carrier_basis_dependencies_source_ddl.sql、mestest_carrier_basis_dependencies_create.sql。同步修正 first-phase CREATE SQL 對這些欄位的預設值，移除未經來源確認的欄位推測註解。
- 本次僅查結構及系統設定，不讀取 MES 業務資料，不在 Oracle 執行 DDL；IP、帳號、密碼與 SID 仍只留在本機受保護設定。

### 更新 2026-11-16：EP401 設備參數資料

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀查得 `MESTEST.TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL` 的 DBMS_METADATA DDL、欄位、NOT NULL/default、PK、索引與 FK 字典；另查 TBLSYSVALUELIST 的 Type 系統選項。沒有查 MES 業務資料或在 Oracle 執行 DDL。
- 兩張表各有一個 PK unique index 與一個額外 non-unique index，沒有 Oracle FK。Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_equipment_recipe_source_ddl.sql`、`mestest_equipment_recipe_create.sql`；first-phase bootstrap 已依正式 DDL 校正。
- 查詢時明確啟動 `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`；Codex 終端預設 PowerShell 7/.NET 10 不相容於本機舊版 ODP.NET 使用的 BinaryFormatter。連線值仍由受保護附件提供，不在終端、Markdown 或 Git 記錄。

### 更新 2026-11-16：ENT 客戶基本資料

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有受保護連線唯讀查得 `TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR` 的 DBMS_METADATA DDL、欄位 NULL/default、PK、索引與 FK 字典。
- 三表僅 PK unique index，沒有外鍵或其他索引。舊 CustomerBasis 刪除程序仍會明確刪除兩個子表；這是 service 行為，不是 Oracle FK cascade。
- Oracle 原始邏輯 DDL 與 PostgreSQL 空表 CREATE SQL 分別存於 `database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。本次只查結構，沒有查詢或匯出客戶業務資料，也沒有在 Oracle 執行 CREATE SQL。
- ET102 客戶連絡人資料直接沿用同一批已查 DDL 中的 `TBLENTCUSTOMERCONTACTOR`；不需要再次查詢或另建 Create SQL。其欄位、畫面限制差異與巢狀 API 契約記錄在 `docs/modules/ENT/客戶連絡人資料.md`。

### 更新 2026-11-16：ENT ET106 客戶出貨地址

- 出貨地址表 `TBLENTCUSTOMERSHIPADDR` 已在 ET101 查詢；報關表 `TBLENTCUSTOMERSHIPDECLARATION` 本次由 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護連線直接唯讀查詢 DBMS_METADATA/ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES。欄位 DDL 可得，資料字典沒有 PK、unique index、一般 index 或 FK。
- PostgreSQL 空表 CREATE SQL 已追加到 `database/reference/mestest_customer_basis_create.sql`，不加來源未定義的 PK/索引/FK。沒有讀業務列、沒有在來源 Oracle 執行 CREATE。
- DDL 查詢程式仍用 `%TEMP%\ymes-ep302-readonly-ddl.ps1` 中的本機受保護連線資料；本次只替換 helper 的表名為 `TBLENTCUSTOMERSHIPDECLARATION`。憑證未輸出或寫入文件/Git。
- 詳細的雙 Grid、表單限制、無主鍵自然鍵/API 規格見 `docs/modules/ENT/客戶出貨地址資料.md`。

### 更新 2026-11-16：ENT ET103 報關行基本資料

- 使用 Windows PowerShell 5.1 啟動 Oracle.ManagedDataAccess，從先前提供的受保護連線設定直接唯讀查詢 `MESTEST.TBLENTBROKERBASIS` DBMS_METADATA DDL 及 ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES/ALL_IND_COLUMNS；沒有讀業務資料或在 Oracle 執行 DDL。
- 查得 BrokerNo 單欄 PK/唯一索引；無 FK/其他索引。Oracle DDL 的 IssueState、CreateDate、DataStamp DEFAULT 與 NULL 性質已逐欄記入 ET103 規格及來源 DDL。既有第一階段 PostgreSQL reference 的該表預設值也已修正。
- PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_customer_basis_create.sql`。連線憑證只使用本機受保護設定，未輸出、未寫入文件或 Git。
- WinForms 查詢、狀態、刪除/停用與 IssueNo=`BROKER BASIS` 簽核邏輯見 `docs/modules/ENT/報關行基本資料.md`。

### 更新 2026-11-16：ENT ET104 貨運行基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線直接唯讀查詢 `MESTEST.TBLENTFORWARDERBASIS` 的 DBMS_METADATA DDL、欄位 NULL/default、constraint、index 與 FK 字典；沒有查業務資料或在 Oracle 執行 DDL。
- 表有 ForwarderNo 單欄 PK/唯一索引，無 FK/其他索引；PostgreSQL 空表 CREATE SQL 已加到 `database/reference/mestest_customer_basis_create.sql`，first-phase reference 也同步補 DEFAULT 與欄位註解。
- DDL 直連方式、欄位與 ET104 邏輯見 `docs/modules/ENT/貨運行基本資料.md`。連線資訊仍僅由本機受保護設定提供，未輸出或寫入 Git。

### 更新 2026-11-16：ENT ET105 取貨公司基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀取得 `MESTEST.TBLENTTAKERBASIS` DBMS_METADATA DDL、欄位 NULL/default、constraints 與 indexes；只查資料字典，未讀取業務列，未在 Oracle 執行 DDL。
- 表只有 TakerNo PK unique index，無 FK/其他 index。Oracle source DDL/PG 空表 CREATE SQL 已追加至 customer reference；first-phase table default 與欄位註解已按查得 DDL 校正。
- 完整 DDL、WinForms 欄位/狀態/簽核規則見 `docs/modules/ENT/取貨公司基本資料.md`；Oracle 連線設定仍只留本機受保護設定。

### 更新 2026-11-16：ENT ET201 外包商主檔與連絡人

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過既有本機受保護連線唯讀查詢 `MESTEST.TBLENTSUBCONTRACTOR`、`TBLENTSUBCONTRACTORCONT` 的 DBMS_METADATA DDL、欄位 NULL/default、PK、索引與 FK 字典；沒有查業務資料或在 Oracle 執行 DDL。
- 主檔只有 SubcontractorNo PK unique index；連絡人只有 `(SubcontractorNo, ContactorName)` 複合 PK unique index。Oracle 資料字典兩表均無 FK、無額外索引；舊 Core 仍在刪除主檔時明確刪除連絡人。
- Oracle logical DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_subcontractor_source_ddl.sql`、`database/reference/mestest_subcontractor_create.sql`。包含 IssueState NOT NULL DEFAULT 0、兩表 DataStamp nullable DEFAULT 0。
- WinForms 雙 Grid、表單欄位、狀態刪除/簽核與 API 規格見 `docs/modules/ENT/外包商基本資料.md`。連線憑證只由本機受保護設定提供，未輸出或提交。

### 更新 2026-11-16：ENT ET202 外包項目設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查詢 `MESTEST.TBLENTOSITEM` 的 DBMS_METADATA DDL、欄位 NULL/default、constraints、indexes 與 index columns；未查業務資料，未在 Oracle 執行 DDL。
- 查得 11 欄、OSITEMNO 單欄 PK；DataStamp nullable DEFAULT 0。Oracle 字典僅有 PK unique index，無 FK、無額外索引。source logical DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_entositem_source_ddl.sql`、`mestest_entositem_create.sql`。
- 查詢 helper 從 `%TEMP%\ymes-ep302-readonly-ddl.ps1` 讀取受保護連線值，僅將表名改為 TBLENTOSITEM；連線資訊未輸出到文件或提交。本次只查結構，不讀取 MES 業務列，也沒有在來源資料庫執行 DDL。
- WinForms、Service/Core、流程查找、版本差異及 API 契約見 `docs/modules/ENT/外包項目設定.md`。

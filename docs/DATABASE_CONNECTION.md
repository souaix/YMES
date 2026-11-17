# YMES / MES 資料庫連線與憑證處理

### 更新 2026-11-18：QC408 OCAP 流程三表

- 使用本機受保護的既有連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 `MESTEST.TBLQCOCAPBASIS`、`TBLQCOCAPNODEBASIS`、`TBLQCOCAPNODERELATION` 的 `DBMS_METADATA.GET_DDL` 與 PK/index/FK metadata。未讀 MES 業務列，未在 Oracle 執行 DDL，沒有輸出/提交連線秘密。
- Oracle DDL 保存於 `database/reference/mestest_qc408_ocap_source_ddl.sql`；YMES PostgreSQL CREATE-only 對照為 `database/reference/mestest_qc408_ocap_create.sql`。三表只有 PK unique index，沒有 FK/額外索引。Relation 的 `OCAPLINKNAME` 由複合 PK 實際要求 NOT NULL；OCAP Basis 的 DataStamp nullable 且 DEFAULT 0。
- `OCAPXMLSTRING` 是舊 AddFlow serializer CLOB；Flow Form 呼叫 `iMESXMLFlow.dll` 的 `FlowToXML/XMLToFlow`，來源 repository 未含該 serializer 原始碼。家用端需在 API 儲存前確認相容格式，不能直接把 Vue 結構化 payload 當成舊 XML。
- WinForms/UI、刪除/簽核、三表交易及 API handoff 見 `docs/modules/QC/OCAP基本資料.md`。

### 更新 2026-11-18：QC501–QC503 檢測儀器共用資料表

- 使用已保存的本機受保護連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 MESTEST 六張表的 CREATE TABLE/PK unique index DDL：`TBLQCINSPMACHINEPARAMETERTYPE`、`TBLQCINSPMACHINETYPE`、`TBLQCINSPMACHINEBASIS`、`TBLQCINSPMACHINEPARAMETER`、`TBLQCINSPMACHINEPROPERTY`、`TBLQCINSPDRIVER`。
- 原始 Oracle DDL：`database/reference/mestest_qc501_inspection_machine_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc501_inspection_machine_create.sql`。來源只有 PK unique index，未查到 FK 或額外獨立索引。CREATE 是 YMES 對照參考，不是 migration；家用端必須查 live schema 後只補缺表。
- 只讀資料字典/DDL，未讀業務資料列，未在 Oracle 執行 DDL；連線秘密未輸出/提交。PS7 載入此舊版 Oracle provider 時遇 BinaryFormatter 平台例外；改用 Windows PowerShell 5.1 後查詢成功。
- QC501 表主鍵實際是 InspMachineType 單欄；來源 `IssueState`/`DataStamp` 可 NULL 且有 DEFAULT，CREATE reference 保留 nullable；`ChNo` 欄位長度為 3，但 WinForms TextBox MaxLength=10，API 需回報超長值錯誤，不得靜默截斷。
- QC502「檢測儀器基本資料」沿用同一批 Oracle source DDL 與 PostgreSQL CREATE 對照；其主要寫入表 `TBLQCINSPMACHINEPARAMETER` 在 QC501 六表清單中已直接查得。本次未重查 Oracle，沒有讀業務列或執行 Oracle DDL。`TBLQCINSPMACHINEBASIS` 不在 QC502 Form/Core 的 Add/Edit/Delete 呼叫內，不要當成 QC502 主表。
- QC503「檢測設備連線程式」主表 `TBLQCINSPDRIVER` 沿用上述已取得的 Oracle source DDL 與 PostgreSQL CREATE 參考；本功能不需另查 Oracle 或新增 CREATE SQL。此回合依 WinForms/wsQC/Core/Driver Function 原始碼核對行為；沒有讀取 MES 業務資料列、連 YMES PostgreSQL、執行 Oracle DDL 或輸出連線秘密。

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

### 更新 2026-11-16：SMD SD101 工廠基本資料

- `MESTEST.TBLSMDFACTORYBASIS` 的 Oracle DDL、欄位 NULL/default、PK/index/FK 已由 PowerShell 5.1 + Oracle.ManagedDataAccess 於 EP303 來源查詢中直接取得；SD101 沿用該次查證，不需重查。
- Oracle logical source DDL、PostgreSQL 空表 CREATE SQL 已在 `database/reference/mestest_carrier_basis_dependencies_source_ddl.sql`、`mestest_carrier_basis_dependencies_create.sql`；first-phase bootstrap 也已包含該表。家用端比對 YMES schema 後，缺少才新增 migration。
- WinForms 狀態、核准/刪除、欄位、DataStamp 與 Vue/API handoff 見 `docs/modules/SMD/工廠基本資料.md`。只使用既有受保護 Oracle 設定查結構，連線值未輸出或寫入 Git。
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

### 更新 2026-11-16：SMD SD103 儲位基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接唯讀查 MESTEST.TBLINVLOCATORBASIS 的 DBMS_METADATA.GET_DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得複合 PK (INVENTORYNO, LOCATORNO)，僅 PK unique index；沒有 Oracle FK 或其他索引。IssueState DEFAULT 0 NOT NULL、Capacity NUMBER(12,4) DEFAULT 0 nullable、DataStamp NUMBER(11,0) DEFAULT 0 nullable。
- Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_locator_basis_source_ddl.sql、mestest_smd_locator_basis_create.sql。first-phase bootstrap 已同步校正預設值，移除尚未有 Oracle column comment 證據的猜測註解。
- 只查資料字典，未讀業務資料、未在 Oracle 執行 DDL；IP、帳號、密碼、SID 僅由受保護設定讀取，未輸出或保存到 Git。庫存挑選沿用既有 TBLINVInventoryBasis，不涉及新增來源 DDL。
### 更新 2026-11-16：SMD SD107 生產線別基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查 MESTEST.TBLSMDPDLINEBASIS 的 DBMS_METADATA DDL、欄位、constraint、PK/index 與 index columns。
- 核實 PDLineStatus NUMBER(1,0) DEFAULT 1 NOT NULL；IssueState NUMBER(1,0) NOT NULL 無 DEFAULT；CreateDate DATE DEFAULT SYSDATE 可 NULL；DataStamp NUMBER(11,0) DEFAULT 0 可 NULL。PDLineNo 是唯一 PK index；無 FK 或其他索引。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_pdline_basis_source_ddl.sql、mestest_smd_pdline_basis_create.sql。first-phase bootstrap 已校正來源 defaults 並移除無來源依據的欄位猜測註解；家用端仍須比對實際 schema/migration，缺漏才新增 forward migration。
- 只查結構，未讀業務資料或在 Oracle 執行 DDL；受保護連線資料未輸出或保存至 Git。

### 2026-11-16：SMD SD108、SD109

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 及本機受保護連線，直接唯讀查詢 MESTEST.TBLSMDAREASHIFT、TBLSMDAREAOPERATOR、TBLSMDPDLINEPOSITIONBASIS 的 DBMS_METADATA 與資料字典。
- 新增 Oracle 邏輯來源 DDL 與 PostgreSQL 空表 CREATE SQL：mestest_smd_area_shift_source_ddl.sql / mestest_smd_area_shift_create.sql；mestest_smd_pdline_position_basis_source_ddl.sql / mestest_smd_pdline_position_basis_create.sql。
- 僅查表結構、欄位 null/default、PK/index/FK；未讀業務資料、未在 Oracle 執行 DDL。連線值未輸出或寫入 Git。

### 更新 2026-11-17：MTL ML101 物料供應商

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取本機受保護連線，唯讀呼叫 DBMS_METADATA.GET_DDL 查 MESTEST.TBLMTLMATERIALVENDOR、TBLMTLMATERIALVENDORCONTACTOR。
- 再查 ALL_INDEXES 與 ALL_CONSTRAINTS：主檔 PK VendorNo；連絡人 PK (VendorNo, ContactorName)；兩表都只有 PK unique index，沒有 FK 或額外索引。
- Oracle 邏輯 DDL與 PostgreSQL 空表 CREATE SQL：database/reference/mestest_mtl_material_vendor_source_ddl.sql、database/reference/mestest_mtl_material_vendor_create.sql。僅查結構，未讀業務資料、未在 Oracle 執行 DDL，連線值未輸出或提交。
### 更新 2026-11-17：MTL ML106 物料品管項目設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護設定唯讀查詢 MESTEST.TBLQCITEMFORATTRIBUTES、TBLQCITEMFORVARIABLES、TBLQCITEMGROUPBASIS、TBLQCITEMBINDLOG 的 DBMS_METADATA DDL、欄位、PK 與 index；未查詢業務資料，未在 Oracle 執行 DDL。
- 原始 Oracle DDL 與 PostgreSQL 空表 CREATE SQL 分別為 database/reference/mestest_mtl_qc_item_tables_source_ddl.sql、database/reference/mestest_mtl_qc_item_tables_create.sql。TBLMTLQCITEM 與 TBLOPQCITEM 沿用既有 MTL/OP reference。
- 來源連線仍由本機受保護設定提供；IP、帳號、密碼不寫入 repo、終端或規格文件。家用端先核對 YMES schema/migrations，只新增缺漏結構。

### 更新 2026-11-17：MTL ML104 物料群組資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線直接唯讀呼叫 DBMS_METADATA.GET_DDL，查得 `MESTEST.TBLMTLMATERIALGROUPBASIS` DDL；另查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。僅查結構，未讀 MES 業務列、未在 Oracle 執行 DDL。
- GROUPNO 與 MATERIALTYPE 的資料字典 column Nullable 顯示 Y，但各有 ENABLED CHECK constraint 強制非 NULL；DATASTAMP NUMBER(11,0) DEFAULT 0 可 NULL。唯一索引為 GroupNo PK index，無其他索引/FK。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 分別在 `database/reference/mestest_mtl_material_group_source_ddl.sql`、`database/reference/mestest_mtl_material_group_create.sql`；群組明細沿用 `mestest_mtl_master_source_ddl.sql`、`mestest_mtl_master_create.sql`。
- 連線秘密仍只在本機受保護設定中；IP、帳號、密碼、SID 不寫入 repo 或待辦，也不需要重新向使用者索取。

### 更新 2026-11-17：MTL ML107 物料標籤對應

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線唯讀呼叫 DBMS_METADATA.GET_DDL，查得 `MESTEST.TBLMTLLABELMAP` 與 lookup table `MESTEST.TBLPRDLABELBASIS`；並查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- TBLMTLLABELMAP 主鍵為 (MaterialType, MaterialNo, LabelSource, LabelType)，LabelSource=5 是物料來源；DataStamp nullable DEFAULT 0。TBLPRDLABELBASIS 主鍵為 LabelNo，兩表都只有 PK unique index，沒有 FK/額外索引。
- Source DDL / PostgreSQL 空表參考見 `database/reference/mestest_mtl_label_map_source_ddl.sql`、`mestest_mtl_label_map_create.sql`、`mestest_prd_label_basis_lookup_create.sql`。本次只查資料字典結構，未讀業務列、未在 Oracle 執行 DDL。
- IP、帳號、密碼、SID 仍由本機受保護設定提供，未輸出或寫入文件/Git；下次公司端查 DDL 沿用這個既有 PowerShell/ODP.NET 方式。

### 更新 2026-11-17：MTL MLB01 來料 AutoLoader 格式檔設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取本機既有受保護連線，唯讀取得 `MESTEST.TBLMTLVENDORALSCRIPT` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。
- 主鍵為 `MTLFOLDERPATH`；只有 PK unique index，無外鍵/額外索引。`FORMATScript`、`RAWDATAFORMATSCRIPT` 是 CLOB；`DATASTAMP NUMBER(11,0) DEFAULT 0` 可 NULL；`STORERAWDATA`、`SENDMAIL` 為 NOT NULL DEFAULT 0。
- Oracle 來源 DDL 與 PostgreSQL 空表參考分別在 `database/reference/mestest_mtl_autoloader_source_ddl.sql`、`database/reference/mestest_mtl_autoloader_create.sql`。僅查資料字典結構，未讀 MES 業務列，未在 Oracle 執行 DDL。
- 連線秘密仍只在本機受保護設定中；IP、帳號、密碼、SID 未輸出或寫入文件/Git。家用端建立 migration 前須比對 YMES PostgreSQL live schema 與既有 migrations。

### 更新 2026-11-17：EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader

- 公司端以 PowerShell 5.1 + Oracle.ManagedDataAccess 從本機既有受保護連線唯讀取得四張表 DDL/資料字典：`TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`。沒有讀取 MES 業務列，也沒有在 Oracle 執行 DDL。
- Oracle source DDL 與 YMES 空表參考為 `database/reference/mestest_eqp_autoloader_source_ddl.sql`、`database/reference/mestest_eqp_autoloader_create.sql`。主表沒有 PK constraint，存在唯一索引 `(EquipmentNo,FileNameFormat)`；三個子表各有來源複合 PK unique index。四表間沒有 FK。
- `FormatScript`、`RawDataFormatScript` 為 CLOB；`DataStamp NUMBER(11,0) DEFAULT 0` nullable。子表與主表需由 API transaction 一致維護。DDL/schema 與功能規格見 `docs/modules/EQP/設備AutoLoader格式檔設定.md`。
- IP、帳號、密碼、SID 沒有輸出或寫入 Git；家用端只需核對 YMES PostgreSQL 結構並決定 forward migration，不要連 Oracle 建表。

### 更新 2026-11-17：QC111 8D 原因基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護 Oracle 連線直接唯讀查詢 MESTEST.TBLQCREASONBASIS_8D、TBLQCREASONSUBTYPE_8D 的 DBMS_METADATA.GET_DDL 及 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_8d_reason_source_ddl.sql、database/reference/mestest_qc_8d_reason_create.sql。兩張表各只有 PK unique index，沒有外鍵或其他索引；DataStamp 是 nullable DEFAULT 0。Subtype ReasonType 是 NUMBER(1,0)，Reason 明細 ReasonType 是 VARCHAR2(50)。
- 本次只讀取資料字典/DDL，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。
- 同以 Oracle 唯讀查詢 MESTEST.TBLSYSVALUELIST 的 TBLQCREASONBASIS_8D.REASONTYPE 選項，並核對 Customer/Production/MESClient/mesresource.zh-cht.resources：1 Men、人員；2 Machine_8D、設備；3 Material、物料；4 Method、方法；5 Environment、環境。此為系統字典與語系資源，不是業務資料。
- YMES 家用端先比對 live schema/migrations，缺少時才建立 forward migration；WinForms、簽核流程、API DTO/端點及子類別刪除留下無 FK 明細的行為見 docs/modules/QC/8D原因基本資料.md。

### 更新 2026-11-17：QC406 第一處理群組來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護 Oracle 連線直接唯讀查詢 MESTEST.TBLQCFIRSTGROUP 的 DBMS_METADATA.GET_DDL 與 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_first_group_source_ddl.sql、mestest_qc_first_group_create.sql。主表 PK 為 `(MCLASSNO, OPNO)`，只有 PK unique index，沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- 查找表 `TBLQCMCLASSBASIS` 與 `TBLQCHOLDDISPGROUPBASIS` 的來源 DDL / PostgreSQL CREATE 參考為 database/reference/mestest_qc_first_group_lookups_source_ddl.sql、mestest_qc_first_group_lookups_create.sql。作業站 `TBLOPBASIS` 已有 `mestest_operation_and_system_source_ddl.sql` / `mestest_operation_and_system_create.sql`。
- 查找來源為 TBLQCMCLASSBASIS、TBLOPBASIS、TBLQCHOLDDISPGROUPBASIS；Form 僅載入 IssueState=2。三表 key 與必要欄位已由 Oracle 資料字典直連核對，本功能不新增查找表。
- 本次只讀取 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。
- WinForms 多作業站新增/複製、`OPNo='*'` 通用設定、Issue Job `FIRST GROUP`、狀態刪除規則與 API 契約見 docs/modules/QC/第一處理群組.md。

### 更新 2026-11-17：QC407 最後處理群組來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護連線直連唯讀查詢 MESTEST.TBLQCFINALGROUP 的 DBMS_METADATA.GET_DDL 與 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_final_group_source_ddl.sql`、`mestest_qc_final_group_create.sql`。PK 為 `(MCLASSNO, OPNO)`，只有 PK unique index、沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- 主分類、作業站、異常處理群組查找表與 QC406 共用，僅查詢 IssueState=2；不新增重複 lookup table。
- 本次只讀取 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。UI、狀態與 Issue Job `Final GROUP` 規則見 `docs/modules/QC/最後處理群組.md`。

### 更新 2026-11-17：QC404 異常說明來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護連線直連唯讀查詢 MESTEST.TBLQCHOLDDESCRIPTION 的 DBMS_METADATA.GET_DDL、`I_TBLQCHOLDDESCRIPTION_01` index DDL，以及 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_hold_description_source_ddl.sql`、`mestest_qc_hold_description_create.sql`。PK 是 `HoldDescriptionID`，另有 `MClassNo` 非唯一索引，沒有 FK；`HoldDescription` VARCHAR2(4000) NOT NULL；DataStamp nullable DEFAULT 0。
- 主分類查找重用 `TBLQCMCLASSBASIS`，WinForms LoadMClassBasis 預設 IssueState=2；不新增 lookup table。Core 以 `Guid.NewGuid().ToString()` 產生 HoldDescriptionID。
- 本次只讀 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。Form、狀態、Issue Job `HOLD DESCRIPTION` 規則見 `docs/modules/QC/異常說明.md`。

### 更新 2026-11-17：QC405 異常處理群組

- `TBLQCHOLDDISPGROUPBASIS` Oracle DDL/欄位/PK/index 已由公司端直連唯讀查得並放在 QC406 共用 lookup source DDL；PostgreSQL 空表 CREATE 也已存在。只有 GroupNo PK index，無其他 index/FK。
- `TBLUSRUSERGROUP` Oracle DDL/欄位/複合 PK/索引已於 2026-11-16 由同一受保護連線直接查得；來源與 PostgreSQL 空表 CREATE 參照放在設備工程群組成員 DDL 檔。其 AgentOf 在 Oracle 欄位屬 nullable，但因納入 PK，實際不得為 NULL。QC405 使用 GroupType=5/6。
- 本次未讀業務資料，未執行 Oracle DDL，沒有輸出或保存連線秘密；沒有新 CREATE SQL 要執行。功能及 API handoff 見 `docs/modules/QC/異常處理群組.md`。

### 更新 2026-11-17：QC403 異常原因基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 及本機既有受保護連線，唯讀取得 MESTEST.TBLQCHOLDITEMBASIS 的 DBMS_METADATA DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES/ALL_IND_COLUMNS。
- Oracle source DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`、`database/reference/mestest_qc_hold_item_basis_create.sql`。PK 為 HoldItemNo，只有 PK unique index，沒有 FK/其他 index；CreateERF nullable DEFAULT 1，HoldSystem NOT NULL DEFAULT 0，DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- WinForms `HoldSystem` ValueList 對照由 `STD/MEStc/DataImporter/HoldItemDataImporter.cs` enum 核實：0=MES、1=EAP_Exception、2=EDA_Exception。查找來源是已簽核 MClass/SClass/異常處理群組，及已簽核且目前版本 OCAP；未建立重複查找表。
- 本次僅查 DDL/資料字典，沒有讀 MES 業務資料，沒有在 Oracle 執行 DDL；連線字串/憑證未輸出或寫入 Git。畫面、CRUD、匯入與 Issue Job 規則見 `docs/modules/QC/異常原因基本資料.md`。

### 更新 2026-11-17：QC401 主分類基本資料

- TBLQCMCLASSBASIS Oracle DBMS_METADATA DDL 及 PostgreSQL 空表 CREATE SQL 已由先前 PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機既有受保護連線唯讀查得，納入 QC406 共用查找表 reference：`database/reference/mestest_qc_first_group_lookups_source_ddl.sql`、`mestest_qc_first_group_lookups_create.sql`。本次沿用既有結果，未重複連線查詢。
- 主鍵為 MClassNo，只有 PK unique index，沒有 FK 或其他 index；本功能不新增 DDL/CREATE 檔。資料表欄位與 UI/API 規則見 `docs/modules/QC/主分類基本資料.md`。
- 另由公司端原始碼確認匯入器位於 `STD/MESWin/QC/QC_MClassDataImport` 及 `STD/MEStc/DataImporter/MClassDataImporter.cs`；未找到 template binary。Excel 只讀程式與匯入檢查來源，不使用 Oracle 查業務列。
- 不讀取 MES 業務列、不在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或保存於 Git。家用端只核對 YMES PostgreSQL live schema/migrations。

### 更新 2026-11-17：QC402 次分類基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線唯讀取得 `MESTEST.TBLQCSCLASSBASIS` 的 `DBMS_METADATA.GET_DDL`，並單獨查詢 `PK_TBLQCSCLASSBASIS` unique index DDL。
- DDL 檔案：`database/reference/mestest_qc_s_class_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_s_class_basis_create.sql`。
- Primary Key 是 SClassNo；只有此 PK unique index，沒有 FK 或額外 index。MClassNo 在 Oracle DDL 可為 NULL、沒有 FK，但 WinForms 表單必填且僅允許選 IssueState=2 的主分類；由 QC402 API 驗證，不在 PostgreSQL DDL 自行加 FK。
- 本次只有查 Oracle metadata，沒有讀取 MES 業務列或執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。
### 更新 2026-11-17：QC101 抽樣表格來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線唯讀取得 MESTEST.TBLQCSAMPLINGTABLE 的 DBMS_METADATA table/index DDL，並查 ALL_CONSTRAINTS。
- 主鍵為 (SAMPLINGTABLE, MINNUM, MAXNUM)；只有 PK unique index；四個 NOT NULL 欄位約束；沒有 FK、其他 unique index 或額外索引。Oracle source DDL 與 PostgreSQL 空表 CREATE 參考為 database/reference/mestest_qc_sampling_table_source_ddl.sql、mestest_qc_sampling_table_create.sql。
- 未讀取 MES 業務列、未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations，缺少時才建 migration。
### 更新 2026-11-17：QC102 抽樣計劃設定_1916 來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLING1916` 的 `DBMS_METADATA.GET_DDL` table/index DDL，並查詢 `ALL_CONSTRAINTS`；沒有讀取業務資料。
- DDL 檔案：`database/reference/mestest_qc_sampling_1916_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_1916_create.sql`。
- 唯一索引 `(SAMPLINGTYPE, MINNUM, MAXNUM, SLEVEL)` 是獨立 unique index，不是 PK constraint；四個 NOT NULL constraint，沒有 FK/其他 index。欄位 nullable/default 依 Oracle DDL 保留。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC103 抽樣計劃來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER` 的 DBMS_METADATA table/index DDL，並查詢 ALL_CONSTRAINTS；未讀取業務資料。
- Oracle source DDL：`database/reference/mestest_qc_sampling_plan_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_create.sql`。
- 主表以 PlanNo 為 PK；參數表以 (PlanNo,ParameterNo) 為複合 PK。兩表只有 PK unique index，沒有 FK/其他 index；欄位 NULL/default 依 Oracle DDL 保留。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC104 Wafer 抽樣計劃來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN_WAFER`、`TBLQCSAMPLINGRANGE`、`TBLQCSAMPLINGAREA_WAFER` 的 DBMS_METADATA table/index DDL，並查詢 ALL_CONSTRAINTS；未讀取業務列。
- Oracle source DDL：`database/reference/mestest_qc_sampling_plan_wafer_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_wafer_create.sql`。
- 主檔 PK 為 PlanNo；Area PK 為 (PlanNo,Area)；Range 無 PK/index/FK。三表沒有 FK。`TBLQCSAMPLINGRANGE` 由 WinForms QC103/QC104 共用名稱，API/schema 變更前需核對 YMES 現有使用。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC105 品質原因子類別來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取公司端既有受保護連線設定，直接唯讀取得 `MESTEST.TBLQCREASONSUBTYPEBASIS` 的 DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢業務列。
- Oracle source DDL：`database/reference/mestest_qc_reason_subtype_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_reason_subtype_basis_create.sql`。
- 主鍵為 ReasonSubType；唯一索引只有 PK index，無 FK/其他 index。欄位 NULL/default 依 DDL 記錄於 `docs/modules/QC/品質原因子類別資料.md`。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC106 品質原因基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCREASONBASIS` DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢業務列。
- Oracle source DDL：`database/reference/mestest_qc_reason_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_reason_basis_create.sql`。
- 主鍵為 ReasonNo；唯一索引只有 PK index，無 FK/額外 index。欄位 NULL/default 與簽核/API 規則記於 `docs/modules/QC/品質原因基本資料.md`。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC107 計數判別法則來源表

- 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCATTRULEBASIS`、`TBLQCATTRULEDETAIL`、`TBLQCAQL` table/index DBMS_METADATA DDL 與 ALL_CONSTRAINTS；未查詢 MES 業務列。
- Oracle source DDL：`database/reference/mestest_qc_att_rule_basis_source_ddl.sql`、`database/reference/mestest_qc_aql_source_ddl.sql`；PG CREATE 參考為對應 `_create.sql`。
- 主表 PK RuleNo；detail PK (RuleNo,Action,ReasonType,ReasonLevel)；AQL PK (AQLType,MinNum,MaxNum,SLevel,AQL)。三表皆無外鍵/額外索引；主與明細皆無 DataStamp。欄位 nullable/default 詳見 `docs/modules/QC/計數判別法則基本資料.md`。
- 本次僅查 metadata、沒有讀取業務列或執行 Oracle DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations，現有 TBLQCAQL 必須優先重用。

### 更新 2026-11-17：QC108 計量品管法則來源表

- 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCRULEBASIS` 的 DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢 MES 業務列。
- Oracle source DDL：`database/reference/mestest_qc_variable_rule_basis_source_ddl.sql`；PostgreSQL CREATE 參考：`database/reference/mestest_qc_variable_rule_basis_create.sql`。
- PK 為 RuleSerial；唯一索引只有 PK index，沒有外鍵或額外索引。RuleNo 沒有 DB unique constraint，舊服務以大寫 RuleNo 檢查重複；DataStamp nullable DEFAULT 0。
- 僅查詢 metadata，沒有在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC109 要因資料維護來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀查 `MESTEST.TBLSYSMESPARAMETER` 與 `ALL_TABLES` 中符合舊程式 `TableName LIKE 'FC_%'` 的表，再用 DBMS_METADATA、ALL_INDEXES、ALL_CONSTRAINTS 取得結構。本次查得 `FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` 三表。
- Oracle source DDL：`database/reference/mestest_qc_factor_maintain_source_ddl.sql`；PostgreSQL CREATE 參考：`database/reference/mestest_qc_factor_maintain_create.sql`。四表各只有 PK unique index，沒有 FK/額外索引。
- 僅查詢表名與 DDL/資料字典 metadata，沒有讀取 MES 業務列，沒有在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端核對 YMES PostgreSQL 中既有參數目錄與 FC 表，必要時才建立 forward migration。

### 更新 2026-11-17：QC110 特殊主檔維護

- QC110 使用相同的 `TBLSYSMESPARAMETER` 目錄與 `FC_%` 動態表機制，但篩選 `ParameterClass=0`；QC109 篩選 `ParameterClass=1`。請家用端以獨立 FunctionKey/API 權限處理兩個功能。
- QC110 所用 `TBLSYSMESPARAMETER`、`FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` Oracle metadata DDL 及 PostgreSQL CREATE 參考已在 QC109 同一批次取得並交付，檔案位於 `database/reference/mestest_qc_factor_maintain_{source_ddl,create}.sql`；本次未重複連 Oracle 或查詢業務列，不需另一份相同 DDL。
- 公司端只新增 Vue DEV 頁與 API handoff；YMES PostgreSQL 是否已有相同結構由家用端比對 live schema/migrations 決定，僅對缺少項新增 forward migration。不得在 Oracle 執行 DDL、不得從 MES 複製業務資料。

### 更新 2026-11-17：QC201 計數品管項目

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端本機既有受保護連線設定直接唯讀連接 MESTEST，取得 18 個 QC201 主表/子表/引用物件的 DBMS_METADATA DDL、欄位、NULL/default、constraints、PK/index metadata。連線方式沿用本檔既有公司端 Oracle 設定；不透過 DBeaver 操作畫面，不讀 MES 業務列。
- 原始結構記錄：`database/reference/mestest_qc_item_attributes_source_ddl.sql`；四張直接維護子表的 PostgreSQL CREATE 參考：`database/reference/mestest_qc_item_attributes_create.sql`；主表 CREATE 已存在於 MTL 共用 reference SQL。只有 YMES schema 核對確認缺表/欄位後才新增 migration。
- IP、帳號、密碼、SID 與 protected config 檔案路徑不寫入文件/SQL/Git；沒有在 Oracle 執行 DDL。

### 更新 2026-11-17：QC112 品質原因產品指導書

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定直接唯讀取得 `MESTEST.TBLQCREASONPRDSOPFILE` 的 `DBMS_METADATA.GET_DDL`，並查詢欄位、PK、索引及外鍵字典。結構含四欄，PK `(REASONNO, PRODUCTNO)`；只有 PK unique index，無 FK、DataStamp 或其他索引。
- Oracle source DDL 與 PostgreSQL 空表 CREATE 參考分別為 `database/reference/mestest_qc_reason_prd_sop_file_source_ddl.sql`、`database/reference/mestest_qc_reason_prd_sop_file_create.sql`。家用端已核對 YMES live schema；`0020_qc112_reason_product_sop_file.sql` 只補缺少的 `tblqcreasonbasis` 與 `tblqcreasonprdsopfile`，並授予 `ymes_app` 必要 table privileges。
- 僅查詢 Oracle 資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL。連線值及檔案服務參數值均未輸出、保存或提交。YMES API 的 QC112 附件存於獨立 named volume，不沿用未存在的 MES file-service 設定。

### 更新 2026-11-17：QC202 計數管制內容

- MESTEST.TBLSPCATTLIMIT Oracle DDL 沿用 QC201 當次由 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接連線取得的來源字典結果；本次未透過 DBeaver UI 查詢。來源 reference：database/reference/mestest_qc_spc_att_limit_source_ddl.sql，欄位及索引資訊也見 mestest_qc_item_attributes_source_ddl.sql 的 TBLSPCATTLIMIT 區塊。
- QC202 不需要新增 Oracle 連線設定。既有連線方法、憑證保護原則和 driver 相容性見本文件前段；連線值只從本機受保護設定讀取，不輸出、不寫入 Markdown/Git。本次只引用既有結構查詢，沒有讀取 MES 業務列或在 Oracle 執行 DDL。
- YMES PostgreSQL 空表參考 database/reference/mestest_qc_spc_att_limit_create.sql 以 JSONB 承接舊 Oracle 動態因子欄位，屬 YMES 固定 schema 適配；家用端需先核對 live schema/migrations，不得把參考檔直接當 migration。

### 更新 2026-11-17：QC203 計數品管項目判別法則對應

- 公司端使用 Windows PowerShell 5.1、既有受保護的 MESTEST 連線設定與 Oracle.ManagedDataAccess，唯讀查詢 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER 的 DBMS_METADATA DDL、ALL_TABLES 與獨立 index metadata；未查業務列，也未在 Oracle 執行 DDL。過程採純 metadata SELECT；一次 session 被 Oracle 中斷後以精簡查詢重試並成功。
- 原始 Oracle DDL reference：database/reference/mestest_qc_attribute_rule_map_source_ddl.sql；PostgreSQL CREATE reference：database/reference/mestest_qc_attribute_rule_map_create.sql。後者將來源動態因子欄適配為 JSONB，並提議 sequence 供 YMES serial 使用；必須先核對 live schema、migration 與共用 serial service，不可直接當 migration 執行。
- 連線設定只在本機受保護附件內讀取，未把 IP、帳號、密碼、SID、連線字串或附件路徑輸出、保存到專案或提交 Git。

### 更新 2026-11-17：QC204 刪除計數型品管項目量測記錄

- QC204 所用的 `TBLQCITEMFORATTRIBUTES`、`TBLSPCATTBASISLOG`、`TBLSPCATTDETAILLOG`、`TBLSPCATTEDITLOG`、`TBLSPCATTDETAILLOG_TAPE` DDL 已存在 `database/reference/mestest_qc_item_attributes_source_ddl.sql`，本次直接閱讀現有 reference，未重新連 Oracle，也未查業務資料。
- QC204 不新增 Oracle/YMES 資料表。家用端需先比較 PostgreSQL migration/schema；只有缺少來源日誌表時才按既有 Oracle reference 設計 YMES migration/Create SQL，保留已存在 schema 與資料。

### 更新 2026-11-17：QC205 計量品管項目 THEIL BU1

- 公司端沿用既有受保護本機連線，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST，唯讀查詢 ALL_TABLES、DBMS_METADATA.GET_DDL 及索引字典。
- 取得 XX_TBLQCITEM_MEMO、XX_TBLSPC_TEMPDATA_VAR、TBLSPCVARDETAILLOG，以及 QC205 刪除/複製/引用檢查所需的 23 個候選物件；其中 TBLSPCQCHARTDETAIL、TBLSPCQCHARTDETAILLOG、TBLSPCQCHARTRULE、TBLSPCQCHARTBASIS 在 MESTEST 不存在。未讀業務資料、未在 Oracle 執行 DDL。
- Oracle source DDL：database/reference/mestest_qc205_theil_custom_source_ddl.sql、mestest_qc205_variable_dependencies_source_ddl.sql；Memo PostgreSQL Create 參考：mestest_qc205_theil_custom_create.sql。家用端先核對 YMES live schema/migrations，再決定必要 migration。
- 連線秘密、主機值、帳號、密碼、SID 與受保護附件路徑未輸出或寫入 Git。
### 更新 2026-11-17：QC209 計量品管項目管制法則對應

- QC209 `TBLQCITEMRULE` Oracle DDL/PK 沿用 QC205 前次以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀擷取結果：`database/reference/mestest_qc205_variable_dependencies_source_ddl.sql`。`TBLQCRULEBASIS` 沿用 QC108 直接查得來源 reference。本次未重複連線，也未查 MES 業務列或執行 Oracle DDL。
- QC209 新增 YMES schema-only Create 參考：`database/reference/mestest_qc209_variable_item_rule_map_create.sql`；需由家用端比對 live PostgreSQL schema/migrations 後再決定是否建立 migration。

### 更新 2026-11-17：QC211 二次檢驗抽樣計畫

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經公司端既有受保護設定唯讀連接 MESTEST；以 DBMS_METADATA.GET_DDL 與 ALL_* 字典取得 TBLQCSAMPLINGFOR2NDINSP 欄位、constraint、index metadata。Oracle source DDL 為 database/reference/mestest_qc211_second_inspection_sampling_source_ddl.sql。
- 主表有五個 NOT NULL 欄位、QCITEMTYPE/ DATASTAMP 預設值；無 PK/FK constraint，唯一索引 U_TBLQCSAMPLINGFOR2NDINSP_01 的欄位順序為 QCITEMNO、QCITEMTYPE、OPNO、OLDSAMPLEPLAN。PostgreSQL schema-only Create 參考為 database/reference/mestest_qc211_second_inspection_sampling_create.sql。
- 只查結構 metadata，未讀業務列、未在 Oracle 執行 DDL。IP、帳號、密碼、SID 與受保護連線設定未寫入 Git。

### 更新 2026-11-17：QC212 計量品管項目群組

- 本次直接閱讀已保存的 Oracle source DDL reference，沒有再次連線 Oracle：`TBLQCITEMGROUPBASIS` 在 `mestest_mtl_qc_item_tables_source_ddl.sql`；`TBLQCITEMGROUPDETAIL` 與 `TBLQCCALCULATESCRIPT` 在 `mestest_qc205_variable_dependencies_source_ddl.sql`；`XX_TBLQCITEM_MEMO` 在 `mestest_qc205_theil_custom_source_ddl.sql`；共用 `TBLQCITEMPARAMETER` / `TBLOPQCITEM` 也各有既有 DDL reference。
- 新增 `database/reference/mestest_qc212_item_group_create.sql`，僅補 PostgreSQL YMES 的 `tblqcitemgroupdetail`、`tblqccalculatescript` schema-only CREATE 參考。群組主表與 THEIL Memo 已各有先前 CREATE 參考；家用端要先核對 live PostgreSQL 和 migration，再補缺表 migration。
- 本次未讀 Oracle 業務列、未在 Oracle 執行 DDL；未使用 DBeaver 螢幕查詢，連線秘密未輸出、未寫入專案。

### 更新 2026-11-17：QC210 刪除計量型品管項目量測記錄

- QC210 依賴的變數量測 Oracle DDL 沿用先前以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得的 reference：database/reference/mestest_qc205_variable_dependencies_source_ddl.sql、mestest_qc205_theil_custom_source_ddl.sql、mestest_qc_item_attributes_source_ddl.sql。本次重新核對已保存的 CREATE TABLE/index 定義，未再次連 Oracle，未讀業務資料。
- PostgreSQL 空表 CREATE 參考為 database/reference/mestest_qc210_variable_measurement_log_delete_create.sql；包含 9 張 SPC 日誌表的來源欄位/PK/索引。TBLQCSAMPLEPLANCHANGE 沿用 QC201 的 mestest_qc_item_attributes_create.sql。
- 公司端未連 YMES PostgreSQL。家用端須先比對 live schema 與已部署 migration，再建立缺少的 forward migration；不能把參考檔當成已套用 migration，也不能在 Oracle 執行 PostgreSQL DDL。
- Oracle IP、帳號、密碼、SID 與受保護連線附件路徑仍只存在本機受保護設定，不記錄於 Git。

### 更新 2026-11-17：QC216 計量品管項目群組_減項

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經本機既有受保護設定直接唯讀查 MESTEST 的 PMI 群組主檔、明細、扣除基準及三張 SPC 表單 map。取得 DBMS_METADATA DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES；未讀 MES 業務列，未在 Oracle 執行 DDL。
- Oracle source DDL 位於 `database/reference/mestest_qc216_*_source_ddl.sql`；PostgreSQL 空表 CREATE 參考 `database/reference/mestest_qc216_variable_item_group_pmi_create.sql`。六表均只有 Oracle PK unique index，沒有 FK 或額外索引。`TBLQCGROUPDEDUCTIONBASIS_PMI` 的 Factor 欄位會由舊 Core 動態新增，YMES 參考改用 `factorvalues JSONB`，不是 Oracle 欄位的原樣複製。
- 查詢僅限資料字典/DDL。連線 IP、帳號、密碼、SID 和受保護設定位置未輸出或寫入 Git；公司端不連 YMES PostgreSQL。家用端必須先查 live schema/migrations，只為缺漏結構建立 forward migration。

### 更新 2026-11-17：QC213 計數品管項目群組

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機既有受保護連線唯讀連至 MESTEST，取得兩張 QC213 主/明細表及兩張 Core 硬刪依賴表的 DBMS_METADATA.GET_DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES metadata。WIP 檢查表的來源 DDL 已在既有 QC205 dependency reference。
- Oracle source DDL：`mestest_qc213_attribute_item_group_basis_source_ddl.sql`、`mestest_qc213_attribute_item_group_detail_source_ddl.sql`、`mestest_qc213_attribute_item_group_op_detail_source_ddl.sql`、`mestest_qc213_attribute_item_group_prd_item_source_ddl.sql`；WIP source 為 `mestest_qc205_variable_dependencies_source_ddl.sql`。PostgreSQL YMES 空表 CREATE 參考：`mestest_qc213_attribute_item_group_create.sql`，列出 QC213 主/明細表、OP Group/PRD 清理關聯、WIP 使用檢查表與來源索引。
- 主檔 PK 為 GROUPNO；明細 PK 為 (GROUPNO, QCITEMNO)。兩表僅有 PK unique index，沒有 FK 或額外 index。預設值欄位在 Oracle 可為 NULL，CREATE 參考保留這項語意。額外查得 TBLOPGQCITEMDETAIL PK=(OPNO,QCITEMNO)，TBLPRDQCITEM PK=(PRODUCTNO,PRODUCTVERSION,OPNO,QCITEMNO)，兩表無 FK/額外索引；TBLWIPTEMP_QCITEM 無 PK，只有 (LOTNO,QCITEMNO) 與 (LOTNO) 索引。
- Legacy Core 硬刪流程先確認 TBLWIPTEMP_QCITEM 沒有該 QC 群組的在線記錄，後清主表/明細以及 TBLQCITEMPARAMETER、TBLOPQCITEM、TBLOPGQCITEMDETAIL、TBLPRDQCITEM 對應資料。家用端須核對 YMES 實際關聯欄位並以 transaction 維持一致；共用 parameter/OP source DDL 沿用既有 QC reference。
- 只查結構 metadata，未查 MES 業務列、未在 Oracle 執行 DDL。IP、帳號、密碼、SID 與受保護連線設定未輸出或寫入 Git。家用端只查 YMES PostgreSQL live schema/migrations，再對缺表建立 migration。

### 更新 2026-11-17：QC218 計數品管項目產品原因對應設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，沿用本機受保護 MESTEST 唯讀連線，查詢兩張 mapping 表與三張共用 lookup 表的 DBMS_METADATA table/index DDL、欄位 nullable/default、PK/index/FK metadata；未查 MES 業務資料列，未在 Oracle 執行 DDL。
- Oracle source DDL：`mestest_qc218_tblqcitemprdreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemforattributes_source_ddl.sql`、`mestest_qc218_tblqcreasonbasis_source_ddl.sql`、`mestest_qc218_tblprdproductbasis_source_ddl.sql`。PostgreSQL CREATE-only 參考只建立兩張 mapping 表：`mestest_qc218_item_product_reason_map_create.sql`。Mapping tables 都只有 PK unique index，沒有 FK 或其他索引。
- 僅查 Oracle metadata；連線 IP、帳號、密碼、SID 和受保護設定位置未輸出或寫入 Git。公司端未連 YMES PostgreSQL。家用端先核對 PostgreSQL live schema/migrations，只為缺少的 mapping 結構建立 forward migration；三張共用 lookup tables 不可重建。

### 更新 2026-11-17：QC214 計量品管項目匯入

- QC214 沿用先前以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得的 QC205/QC201 Oracle 結構 reference：`mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_qc205_variable_dependencies_source_ddl.sql`、`mestest_qc_item_attributes_source_ddl.sql`；QC210 PostgreSQL CREATE 參考為 `mestest_qc210_variable_measurement_log_delete_create.sql`。本次直接閱讀 WinForms Form、wsQC 與 clsQCBasis，沒有再次連 Oracle、沒有讀業務列、沒有執行 DDL。
- QC214 是匯入流程，不需要新 Oracle 表或獨立 PostgreSQL CREATE SQL。`TBLQCITEMFORVARIABLES` 空表 CREATE 參考在 `mestest_mtl_qc_item_tables_create.sql`；子表和引用表依 QC205/QC210/QC201 的現有 CREATE/reference 核對。
- Core 對 SPC Chart Basis 的引用拼字與現有 QC205 dependency DDL 清單的缺表拼字不一致。家用端依 YMES live schema、migration 及 API 實際 SQL 確認正式物件名；不要為了讓舊拼字通過而臆造一張新表。
- 本次沒有新查詢或資料連線。IP、帳號、密碼、SID 與受保護設定都未輸出或寫進 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC215 計數品管項目匯入

- QC215 沿用先前已保存的 QC201/QC205/QC213 Oracle 結構參照：`mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_qc_item_attributes_source_ddl.sql`、`mestest_qc213_attribute_item_group_detail_source_ddl.sql` 及 QC205 dependency DDL；PostgreSQL CREATE 參考沿用對應 QC201/QC205/QC213 reference。本次直接閱讀 WinForms Form、wsQC 與 clsQCBasis，沒有再次連 Oracle、沒有讀業務列、沒有執行 Oracle DDL。
- QC215 是匯入既有 `TBLQCITEMFORATTRIBUTES` 計數品管資料的功能，沒有 QC215 專用資料表，不新增 CREATE SQL。範圍依賴 QC Item Parameter/Factor/Reason Map、抽樣計畫、作業站、群組及 SPC 既有資料表；不得重建共用表。家用端只比對 YMES PostgreSQL live schema/migrations，缺漏時建立 forward migration。
- 連線 IP、帳號、密碼、SID 與受保護設定都未輸出或寫進 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC302 匯入管制圖內容

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護的 MESTEST 唯讀設定，查詢 `TBLSPCQCCHARTBASIS`、`TBLSPCQCCHARTDETAIL`、`TBLSPCQCCHARTDETAILLOG`、`TBLSPCQCCHARTRULE` 的 DBMS_METADATA table/index DDL。沒有讀 MES 業務資料列，沒有在 Oracle 執行 DDL。
- Oracle source DDL `mestest_qc302_spc_qc_chart_source_ddl.sql` 與 PostgreSQL 空表 CREATE-only 參考 `mestest_qc302_spc_qc_chart_create.sql` 已保存於 `database/reference/`。QC302 匯入不會新增/更新 `TBLSPCQCCHARTSAMPLANPARAMETER`，因此未納入此功能的 CREATE handoff。
- Oracle 欄位中的 FactorNo 動態欄位在 YMES CREATE 參考採 `factorvalues JSONB`；家用端須核對 live schema/migrations 並檢查重複 QCItem/Factor 組合，才建立 forward migration。連線值未輸出或寫入 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC301 管制圖內容

- QC301 共用 QC302 已查得的 `TBLSPCQCCHARTBASIS`、`TBLSPCQCCHARTDETAIL`、`TBLSPCQCCHARTDETAILLOG`、`TBLSPCQCCHARTRULE` Oracle DDL；本次另以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護 MESTEST 唯讀設定，直接查 `TBLSPCQCCHARTSAMPLANPARAMETER` 的 DBMS_METADATA table/index DDL。
- 新增 Oracle DDL reference `mestest_qc301_chart_sampling_parameter_source_ddl.sql` 和 PostgreSQL 空表 CREATE-only 對照 `mestest_qc301_chart_sampling_parameter_create.sql`。參數表 PK=(FactorSerial,ParameterNo)，未查到 FK 或額外索引。
- 僅查 Oracle 結構 metadata，未讀業務資料列、未在 Oracle 執行 DDL。公司端未連 YMES PostgreSQL；家用端須先核對 live schema/migrations，缺少時才新增 forward migration。IP、帳號、密碼、SID 與受保護設定未輸出或寫入 Git。

### 更新 2026-11-17：QC303 品管項目規格

- QC303 沿用已保存的 Oracle DDL：`database/reference/mestest_qc205_variable_dependencies_source_ddl.sql` 中 `TBLQCSPECDETAIL` 與 `TBLQCSPECBASIS` 區塊。該 DDL 是先前由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得，本次沒有改用 DBeaver 螢幕操作，也沒有重新讀業務列或執行 Oracle DDL。
- 本次建立 PostgreSQL 適配的 CREATE-only 對照 `database/reference/mestest_qc303_spec_basis_create.sql`；因子欄收納為 JSONB。家用端先比對 YMES live schema/migrations，確認缺漏後再新增 forward migration。公司端不連 YMES PostgreSQL。
- 本次未輸出或寫入 Oracle IP、帳號、密碼、SID 或本機受保護連線設定。

### 更新 2026-11-17：QC304 物料規格設定

- QC304 的 Chart Basis/Detail/DetailLog Oracle DDL 沿用 QC302 既有 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀查詢 reference `mestest_qc302_spc_qc_chart_source_ddl.sql`；MTL/QC Item/Group DDL 沿用已保存 MTL、QC201、QC205、QC212 references。本次直接閱讀 WinForms Form、wsQC、clsQCBasis 原碼，未重新查 Oracle。
- QC304 沒有新專屬資料表，重用 QC302 已建立的 chart schema/migration 0024 與既有物料/QC Item 關聯表；未新增 CREATE SQL。家用端先查 YMES live schema/migrations，只有確認相依物件缺漏才新增 forward migration。
- 本次未查 MES 業務資料列，未在 Oracle 執行 DDL；未輸出或寫入 IP、帳號、密碼、SID 或本機受保護連線設定。

### 更新 2026-11-17：QC305 產品規格設定

- QC305 的 Chart Basis/Detail/DetailLog Oracle DDL 沿用 QC302 reference `mestest_qc302_spc_qc_chart_source_ddl.sql`；產品主檔 DDL 沿用 `mestest_qc218_tblprdproductbasis_source_ddl.sql`，PRD QC Item/Variable/Factor 依賴沿用 QC205 reference，QC Item Group CREATE 沿用 QC212 reference。本回合直接讀 `frmQCProductSpec.vb`、`clsQCBasis.cs` 與既有 DDL 檔，沒有重新連 Oracle 或讀取業務列。
- QC305 不新增表；重用 YMES Chart Basis/Detail/DetailLog、產品、PRDQCITEM、QC Item/Factor/Group schema。`TBLPRDTYPE` 是舊產品類別 lookup 依賴，但 repo 沒有獨立 Oracle DDL reference；家用端先核對 live schema/API，若確認 YMES 缺少對應物件，再由公司端透過 PowerShell + Oracle.ManagedDataAccess 取得原始 DDL。
- 產品 Basis 的動態 PRODUCT 因子在 YMES 使用 `factorvalues JSONB`；家用端需按 QC302 實際資料存取方式核對舊 physical `product` 欄的相容性。QC305 Core 以 ProductNo 而非 ProductVersion/OPNo 讀寫 chart basis，規格已記錄此舊行為。
- 本回合未在 Oracle 執行 DDL、未查 YMES PostgreSQL、未輸出或寫入 IP、帳號、密碼、SID 或受保護連線設定。

### 更新 2026-11-18：QC409 OCAP 異常單處置調整

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取既有受保護 DatabaseConfigs.CS.Test.Mes.ConnectionString，直連 MESTEST 查 DBMS_METADATA.GET_DDL 及 index metadata。查詢只讀結構，不讀 WIP 業務列；未在 Oracle 執行 DDL/DML，連線值未輸出或寫入 Git。
- 專用表 DDL：mestest_qc409_wip_ocap_source_ddl.sql，涵蓋 TBLWIPERFOCAPDISPOSITION、TBLWIPERFOCAPSTATE、TBLWIPERFOCAPGROUPSTATE。
- 核心 ERF/WIP/通知依賴 DDL：mestest_qc409_wip_disposition_dependencies_source_ddl.sql，涵蓋 TBLWIPERFBASIS、TBLWIPERFBATCHLOT、TBLWIPERFDISPOSITION、TBLWIPERFHOLDREASONDISP、TBLWIPLOTSTATE、TBLWIPLOTLOG、TBLWIPGOODBINSTATE、TBLWIPERF_ATTACHFILE、TBLWIPERFOCAP_ATTACHFILE、TBLUSRUSERBASIS、TBLENTCUSTOMERCONTACTOR，以及來源索引。
- mestest_qc409_wip_ocap_create.sql 僅為三張 OCAP 專用表提供 YMES PostgreSQL schema-only Create 對照；其他表為共用 WIP/ENT/USR schema，家用端需查 live schema/migrations 再適配，勿重建或盲目套用 Oracle DDL。
### 更新 2026-11-18：OP103 作業站類別 TBLOPTYPE

- 以公司端既有受保護 Oracle 設定，透過 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 MESTEST.TBLOPTYPE 的 DBMS_METADATA.GET_DDL 與索引資訊；DDL 保存為 database/reference/mestest_op103_optype_source_ddl.sql。只讀 metadata，沒有讀 MES 業務資料列或執行 Oracle DDL；連線秘密沒有寫入 Git。
- 確認主鍵只有 OPTYPE；無 FK/額外 index。DATASTAMP nullable + DEFAULT 0；ISSUESTATE NOT NULL + DEFAULT 0。PostgreSQL CREATE-only 對照為 database/reference/mestest_op103_optype_create.sql，不等於 migration。
- WinForms 舊 Core 新增作業站類別前，會檢查 TBLOPBASIS.OPNO 是否已存在同名 OP；刪除會先清除 TBLOPPROPERTY 中對應 OPNo。家用端須先比 live schema 與 YMES OP Property API，再實作交易及 namespace 保護，詳見 docs/modules/OP/作業站類別資料.md。

### 更新 2026-11-18：OP101 作業站法則設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀取得 `MESTEST.TBLOPCONTROLRULE` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_CONSTRAINTS`/`ALL_INDEXES`；確認僅有 RuleNo 主鍵唯一索引，無 FK 或其他索引。
- Oracle source DDL 與 PostgreSQL CREATE-only 參考：`database/reference/mestest_op101_control_rule_source_ddl.sql`、`mestest_op101_control_rule_create.sql`。未讀 MES 業務列，沒有在 Oracle 執行 DDL；憑證未輸出或保存到 Git。

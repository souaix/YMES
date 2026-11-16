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



### 更新 2026-11-16：設備群組 SMD 區域映射

- 以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀取得 `MESTEST.TBLSMDAREARELATION`、`MESTEST.TBLSMDSMTAREAEQPMAP` 的 DBMS_METADATA DDL，並核對欄位、NULL、PK、索引及 FK。
- `TBLSMDAREARELATION` 有 PK `(BASEAREANO, OBJECTNO)` 及非唯一索引 `(CONTAINAREANO, OBJECTTYPE)`；`TBLSMDSMTAREAEQPMAP` 的 PK 為 `(BASEAREANO, AREANO, EQUIPMENTNO)`。兩表沒有 FK。
- Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_equipment_group_smd_source_ddl.sql`、`database/reference/mestest_equipment_group_smd_create.sql`。家用端仍須比對實際 YMES schema；本次只讀結構，未讀取業務資料。
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

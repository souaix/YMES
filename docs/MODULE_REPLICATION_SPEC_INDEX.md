# MES WinForms 復刻規格索引

本索引追蹤從舊 MES WinForms 與 Oracle schema 還原出的畫面、操作規則、資料表及 YMES API 規格。每個功能以獨立 Markdown 規格記錄；未從程式碼或資料庫確認的內容標示「待查」，不以畫面推測成正式規則。

## 規格產出原則

- 以 WinForms Form、Service/Core 類別及 MES Oracle 資料字典為依據；截圖用於核對版面，不取代程式碼與資料表證據。
- 每個功能頁記錄選單階層、Form 名稱、欄位、查詢條件、按鈕、狀態轉換、驗證、交易與資料表。
- 同時記錄 Vue UI 狀態、API 契約及資料庫待辦。公司電腦 Development 可用明確標示的本地假資料完成互動；Production API 尚未完成時，必須如實標示待建置。
- 共用文件：[功能規格範本](MODULE_SPEC_TEMPLATE.md)、[資料庫連線與憑證處理](DATABASE_CONNECTION.md)、[開發分工與 UI 準則](DEVELOPMENT_STYLE.md)。

## 2026-11-17 新增完成規格

- EQP `B_EQP AUTOLOADER SETUP`（EPB01）設備 AutoLoader 格式檔設定：[`modules/EQP/設備AutoLoader格式檔設定.md`](modules/EQP/設備AutoLoader格式檔設定.md)。Oracle source DDL 與 YMES 空表 CREATE 參考：`database/reference/mestest_eqp_autoloader_source_ddl.sql`、`database/reference/mestest_eqp_autoloader_create.sql`。公司端 Vue DEV 頁掛載 `/YMES/EQP/EquipmentAutoLoaderSetup`；API、schema migration、選項與解析驗證待家用端交接。
- Oracle 來源為 `TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`；主表唯一索引是 `(EquipmentNo,FileNameFormat)`，來源無 FK。沒有在 Oracle 執行 DDL 或讀取 MES 業務列。

## 2026-11-17 新增功能規格

- QC104「抽樣計劃_元件」：[功能規格](modules/QC/Wafer抽樣計劃.md)。公司端依 `frmSamplingPlan_Wafer`/Def、wsQC 與 Core 完成 `/YMES/QC/SamplingPlanWafer` DEV 頁和 QC 基本資料選單。Oracle source DDL / PostgreSQL CREATE 參考包含 Wafer 主檔、共用 Range、Area 三表；Range 無 PK/index/FK，家用端需先協調和 QC103 共用表的 schema/API 行為，再補 QC104 transaction CRUD、DataStamp CAS 及 `WAFER SAMPLING PLAN` Issue Job。

- QC103「抽樣計劃」：[功能規格](modules/QC/抽樣計劃.md)。公司端已對照 `frmSamplingPlan`/`frmSamplingPlanDef`、wsQC 與 Core，完成 `/YMES/QC/SamplingPlan` DEV 頁及 QC 基本資料選單。兩張 Oracle source DDL / PostgreSQL CREATE 參考已建立；來源各有 PK、沒有 FK/額外索引。家用端需實作交易式主檔/參數 API、`SAMPLING PLAN` Issue Job、DataStamp CAS、PlanType/參數選項及 Simulation。


- QC102「抽樣計劃設定_1916」：[功能規格](modules/QC/抽樣計劃設定_1916.md)。公司端已對照 WinForms Form/Def、wsQC、`clsQCBasis`/`clsBasis_UPD`，完成 `/YMES/QC/Sampling1916` DEV 頁與 QC 基本資料選單。Oracle source DDL/PostgreSQL 空表 CREATE 參考已建立；唯一索引為 SamplingType/MinNum/MaxNum/SLevel，沒有宣告 PK constraint、FK 或簽核。家用端需核對 YMES schema 並實作 QC102 CRUD、DataStamp CAS 及抽樣解析規則。


- QC101「抽樣表格」：modules/QC/抽樣表格.md。公司端已完成 Vue DEV 頁 /YMES/QC/SamplingTable、三欄查詢、區間 CRUD、單列/整表複製及 QC 基本資料選單；PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 TBLQCSAMPLINGTABLE DDL/index/constraints，建立來源 DDL 與 PostgreSQL 空表 CREATE 參考。家用端需先比對 YMES schema/migrations，再實作 QC101 CRUD、DataStamp CAS 與 CopyTable 部分成功 API；沒有簽核或 lookup。


- QC401「主分類基本資料」：[功能規格](modules/QC/主分類基本資料.md)。公司端已完成 `/YMES/QC/MClassBasis` DEV 頁；重用 QC406 已查得的 `TBLQCMCLASSBASIS` DDL/CREATE。匯入器原碼已找到並記錄第一工作表/前兩欄/逐編號部分成功規則；官方 template binary 未找到，需由家用端接 YMES template storage。
- QC402「次分類基本資料」：[功能規格](modules/QC/次分類基本資料.md)。公司端已完成 `/YMES/QC/SClassBasis` DEV 頁、QC402 路由/選單、已簽核主分類 lookup 及匯入器 UI。Oracle source DDL 和 PostgreSQL 空表 CREATE 參考已由公司端受保護 Oracle 連線直接查得；主鍵僅 SClassNo，沒有 FK。家用端需比對 live schema/migrations，實作 QC402 CRUD、`SCLASS BASIS` Issue Job 簽核、MClass lookup 與 `.xls/.xlsx` template/import API。

- QC403「異常原因基本資料」：[功能規格](modules/QC/異常原因基本資料.md)。公司端已完成 `/YMES/QC/HoldItemBasis` DEV 頁；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。`TBLQCHOLDITEMBASIS` 以 HoldItemNo 為 PK，只有 PK unique index，無 FK/額外索引；家用端需比對 schema，實作 QC403 CRUD、HoldSystem/分類/群組/OCAP 查找、`HOLDITEM BASIS` Issue Job 與 Excel 匯入 API。

- QC404「異常說明」：[功能規格](modules/QC/異常說明.md)。公司端已依 WinForms 建立 DEV 頁 `/YMES/QC/HoldDescription`；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。來源主鍵是 HoldDescriptionID，另有 MClassNo 非唯一索引、沒有 FK；家用端需比對 YMES schema 並實作 `HOLD DESCRIPTION` Issue Job 核准流程。
- QC405「異常處理群組」：[功能規格](modules/QC/異常處理群組.md)。公司端 Vue DEV 頁 `/YMES/QC/HoldDispGroup` 包含群組主檔、GroupType 5 主處理人員與 GroupType 6 CC 人員；沿用已查的 `TBLQCHOLDDISPGROUPBASIS`、`TBLUSRUSERGROUP` DDL/CREATE 參考，無新增資料表；家用端需實作群組/成員原子更新及 `HOLD DISP GROUP` Issue Job。
- QC406「第一處理群組」：[功能規格](modules/QC/第一處理群組.md)。公司端已依 WinForms 建立 DEV 頁 `/YMES/QC/FirstGroup`；主表與查找表 Oracle source DDL、PostgreSQL 空表 CREATE 參考位於 [`database/reference`](../database/reference/) 下的 `mestest_qc_first_group*_*.sql`。來源主表是 `(MClassNo, OPNo)` 複合 PK，沒有 FK；家用端需比對 YMES schema 並實作 Issue Job 核准與查找端點。
- QC407「最後處理群組」：[功能規格](modules/QC/最後處理群組.md)。公司端已依 `frmFinalGroup`/Service/Core 建立 DEV 頁 `/YMES/QC/FinalGroup`；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。來源主表也是 `(MClassNo, OPNo)` 複合 PK，沒有 FK；Issue Job Map 的來源識別字串為 `Final GROUP`。
- QC111「8D 原因基本資料」：[功能規格](modules/QC/8D原因基本資料.md)。公司端已接 WinForms 雙 Grid DEV 頁 `/YMES/QC/8DReasonBasis`；[Oracle source DDL](../database/reference/mestest_qc_8d_reason_source_ddl.sql) 與 [PostgreSQL 空表 CREATE 參考](../database/reference/mestest_qc_8d_reason_create.sql) 已建立。兩張來源表只有 PK unique index，沒有 FK；家用端需比對 YMES schema 並補 API/Issue Job 核准流程。

## 已確認的第一層模組

以下清單已由公司端 PowerShell 5.1 唯讀查詢 MESTEST.TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 取得。第一層名稱依 WinForms 的 ModuleNo + Module 翻譯鍵，從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實。詳細功能編號、分類、FormName 及 ExecutionFile 見 [MES Oracle 選單與功能目錄](MES_MENU_FUNCTION_INVENTORY.md)。目錄資料也不等同使用者實際可見權限，WinForms 另以使用者權限篩選。

| 順序 | ModuleNo | 第一層名稱（已核實者） | MenuItem | Function |
| ---: | --- | --- | ---: | ---: |
| 1 | ENT | 客戶資料 | 10 | 8 |
| 2 | EQP | 設備資料 | 22 | 18 |
| 3 | SMD | 區域資料 | 9 | 8 |
| 4 | MTL | 物料資料 | 10 | 8 |
| 5 | QC | 品質資料 | 49 | 44 |
| 6 | OP | 作業站資料 | 12 | 10 |
| 7 | PRS | 流程資料 | 5 | 4 |
| 8 | PRD | 產品資料 | 41 | 37 |
| 9 | OE | 工單管理 | 16 | 13 |
| 10 | WIP | 在製品管理 | 98 | 112 |
| 11 | EMS | 設備管理 | 28 | 26 |
| 12 | SPC | 品質管理 | 37 | 34 |
| 13 | INV | 庫存管理 | 44 | 38 |
| 14 | PM | 維修保養管理 | 47 | 42 |
| 15 | RPT | 報表管理 | 83 | 74 |
| 16 | IEW | 預警管理 | 6 | 5 |
| 17 | USR | 使用者管理 | 17 | 14 |
| 18 | SYS | 系統管理 | 52 | 47 |
| 19 | ISU | 簽核管理 | 9 | 7 |
| 20 | AUT | 自動執行 | 6 | 4 |

### 標準 WinForms 原始碼模組盤點（候選清單）

以下是 `STD/MESWin` 第一層原始碼目錄及 `frm*.vb` 數量，只用來安排後續搜尋。這些目錄不等於正式 `TBLSYSMODULE` 菜單列，也不能據此推斷第一層顯示名稱、排序或使用者可見權限。

| 原始碼目錄候選 | Form 數 | 原始碼目錄候選 | Form 數 |
| --- | ---: | --- | ---: |
| AUT | 8 | BR | 85 |
| DT | 7 | EMS | 59 |
| ENT | 20 | EQP | 70 |
| FMA | 19 | IEW | 20 |
| INV | 81 | ISU | 18 |
| MSA | 10 | MTL | 25 |
| OE | 32 | OP | 36 |
| PM | 68 | PRD | 144 |
| PRS | 21 | QC | 153 |
| RPT | 143 | SMD | 25 |
| SPC | 135 | SYS | 125 |
| USR | 35 | WIP | 355 |

Oracle 目錄共有 20 個 Module、601 筆 MenuItem、553 筆 Function；MenuType=1 分類 82 筆，MenuType=0 功能項 519 筆，其中 518 筆能對應 TBLSYSFUNCTION。完整來源目錄與 ModuleOrder 已保存至 MES_MENU_FUNCTION_INVENTORY.md。STD/MESWin Form 數仍是輔助盤點值，下一步逐筆依 FunctionNo/FormName/ExecutionFile 對照原始碼和 WinForms 畫面，並用實際權限確認可見性。

## 現有核心主檔遷移狀態

| ModuleNo | 已有畫面 | 規格與來源 DDL | 家用端 Production API |
| --- | --- | --- | --- |
| ENT | 客戶基本資料 ET101 DEV 頁面 | [客戶基本資料規格](modules/ENT/客戶基本資料.md)；3 張來源表 DDL/空表 CREATE SQL 已交付 | YMES schema/API/匯入尚待家用端 |
| ENT | 客戶連絡人資料 ET102 DEV 頁面 | [客戶連絡人規格](modules/ENT/客戶連絡人資料.md)；沿用 ET101 已查的 `TBLENTCUSTOMERCONTACTOR` DDL/CREATE SQL | YMES API 待家用端；不新增重複資料表 |
| ENT | 客戶出貨地址資料 ET106 DEV 頁面 | [客戶出貨地址規格](modules/ENT/客戶出貨地址資料.md)；出貨地址沿用 ET101 DDL，報關表已另查 DDL/CREATE SQL | 地址與報關 API、查找選項待家用端 |
| ENT | 報關行基本資料 ET103 DEV 頁面 | [報關行基本資料規格](modules/ENT/報關行基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、DataStamp 與簽核 API 待家用端 |
| ENT | 貨運行基本資料 ET104 DEV 頁面 | [貨運行基本資料規格](modules/ENT/貨運行基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、已簽核報關行查找與簽核 API 待家用端 |
| ENT | 取貨公司基本資料 ET105 DEV 頁面 | [取貨公司基本資料規格](modules/ENT/取貨公司基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、DataStamp 與簽核 API 待家用端 |
| ENT | 外包商基本資料 ET201 DEV 雙 Grid 頁面 | [外包商基本資料規格](modules/ENT/外包商基本資料.md)；兩張 Oracle 來源 DDL/PG 空表 CREATE SQL 已建立 | YMES schema 比對、雙表 CRUD/交易與 Issue/Job 核准 API 待家用端 |
| ENT | 外包項目設定 ET202 DEV 單 Grid 頁面 | [外包項目設定規格](modules/ENT/外包項目設定.md)；Oracle 來源 DDL/PG 空表 CREATE SQL 已建立 | YMES schema 比對、CRUD/DataStamp 與已核准外包流程查找 API 待家用端 |
| EQP | 設備設定七項 DEV 頁面 | 設備設定功能規格已分頁；多張設備表已查 DDL | 待逐功能完成 |
| SMD | SD101 工廠、SD103 儲位、SD104 區域、SD105 區域庫房、SD106 過帳電腦之區域、SD107 生產線別、SD108 區域設備狀態監控、SD109 工位基本資料 DEV 頁面；SMD 第一層三層選單列出 8 個功能 | [SD101](modules/SMD/工廠基本資料.md)、[SD103](modules/SMD/儲位基本資料.md)、[SD104](modules/SMD/區域基本資料.md)、[SD105](modules/SMD/區域庫房基本設定.md)、[SD106](modules/SMD/過帳電腦之區域.md)、[SD107](modules/SMD/生產線別基本資料.md)、[SD108](modules/SMD/設備狀態監控_區域.md)、[SD109](modules/SMD/工位基本資料.md)；來源 Oracle DDL 與 PostgreSQL 空表 CREATE 已交付 | SD101、SD103–SD109 Production API/schema 待家用端 |
| OP | 作業站主檔及簡化 DEV 子功能 | [作業站主檔規格](modules/OP/作業站主檔.md)；18 張作業站相關表來源 DDL/CREATE SQL 已交付 | 待補子功能 API |
| MTL | 物料主檔與 DEV 子功能；ML101、ML102、ML104、ML105、ML106、ML107、MLB01 畫面/規格已建立 | [物料主檔](modules/MTL/物料主檔.md)、[ML101](modules/MTL/物料供應商基本資料.md)、[ML102](modules/MTL/物料類別資料.md)、[ML104](modules/MTL/物料群組資料.md)、[ML105](modules/MTL/物料類別品管項目設定.md)、[ML106](modules/MTL/物料品管項目設定.md)、[ML107](modules/MTL/物料標籤對應設定.md)、[MLB01](modules/MTL/來料AutoLoader格式檔設定.md)；MLB01 Oracle DDL/PG CREATE 已交付 | MTL API 部分部署；ML101、102、104、105、106、107、MLB01 Production API/schema 依 `todolist_homepc.md` 待家用端完成 |
| PRS | 流程主檔 | 既有主檔規格與程式待逐項核對 | 部分主檔 API 已有，擴充待補 |
| PRD | 產品主檔 | 既有主檔規格與程式待逐項核對 | 部分主檔 API 已有，擴充待補 |

其餘 15 個 Module 的 476 個 Function 尚未逐一遷移。先以 MES_MENU_FUNCTION_INVENTORY.md 為工作目錄，再按 ModuleOrder、MenuNo、FunctionNo 追 WinForms Form/Service、相關表、API、Create SQL 及 DEV 畫面。

## 設備資料 → 設備設定

以下順序依使用者提供的 WinForms 選單畫面：

| 功能 | WinForms / 資料來源狀態 | Web 規格狀態 |
| --- | --- | --- |
| 設備供應商基本資料 | Form、Designer 與 Service 已對照；Oracle DDL 已查 | [規格](modules/EQP/設備設定/設備供應商基本資料.md)與 Vue 已依 WinForms 對齊；供應商/聯絡人 API 已部署 |
| 設備工程群組資料 | Form、Designer、Service 與 GroupType=2 成員規則已對照；兩張來源表 DDL 已查 | [規格](modules/EQP/設備設定/設備工程群組資料.md)及 Vue UI 已依 WinForms 補正；YMES schema 比對及 API 待家用電腦 |
| 設備類別資料 | WinForms/Service、5 張設備類別相關表及配件目錄表 Oracle DDL、FunctionNo/FunctionKey 已核實 | [規格](modules/EQP/設備設定/設備類別資料.md)與 Vue Development UI 已依 WinForms 補齊主/子表單套用、稽核與工程群組/圖片選擇器；家用端待比對 schema 並實作 API |
| 配件類別資料 | ACC TYPE / EP202；frmAccessoryType、定義、壽命、屬性及匯入原始碼已核對；4 張來源表 DDL/PK 已由 PowerShell 直連 Oracle 查得 | [規格](modules/EQP/配件/配件類別資料.md)與 Vue DEV 頁已建立；Excel/舊 Checker 簡化已記錄，Production API/schema 比對待家用端 |
| 配件分類資料 | ACC CATEGORY / EP201；frmAccessoryCategory/Def/Import 與 Service 狀態規則已核對；主表 DDL 沿用 EP202 依賴檔 | [規格](modules/EQP/配件/配件分類資料.md)與 Vue DEV 頁已建立；CSV 暫代 Excel，API/schema 待家用端 |
| 配件分類狀態定義設定 | ACC CATEGORY STATE / EP204；Form/Def、狀態/介面選項、InitialFlag 唯一性與簽核刪除規則已核對；兩張 Oracle DDL 已直連查得 | [規格](modules/EQP/配件/配件分類狀態定義設定.md)、PG 空表 CREATE SQL 與 Vue DEV 彈窗/獨立頁已建立；API/schema 待家用端 |
| 配件狀態設定 | EQP ACC STATE / EP205；Form/Def、CRUD/Copy/Apply、欄位驗證與既有狀態表 DDL 已核對 | [規格](modules/EQP/配件/配件狀態設定.md)、Vue DEV 頁已建立；既有表 schema 比對及 API 待家用端 |
| 配件基本資料 | ACC BASIS / EP203；主/Def、配件屬性、匯入及 Service 原碼已核對；六張 Oracle DDL 與八個 index 已由 PowerShell 直連查得 | [規格](modules/EQP/配件/配件基本資料.md)、Oracle 原始 DDL/PG CREATE SQL 與 Vue DEV 頁已建立；YMES schema/API 待家用電腦 |
| 設備基本資料 | WinForms 主/編輯/Chamber Form、屬性、參數、QC 關聯、匯入 Form 與 Service 均已追 | [設備基本資料](modules/EQP/設備設定/設備基本資料.md)、[設備屬性](modules/EQP/設備設定/設備屬性設定.md)、[設備參數](modules/EQP/設備設定/設備參數設定.md)、[設備點檢表關聯](modules/EQP/設備設定/設備點檢表關聯.md)、[設備匯入](modules/EQP/設備設定/設備資料匯入.md)規格已建立；設備子功能 DEV Vue 已接線；設備主表與參數表原始 DDL 已由使用者提供並轉成單表 CREATE SQL；其他未匯出關聯表仍待查 |
| 設備群組資料 | WinForms/Service、主從表與兩張 SMD 區域映射表 Oracle DDL/索引已核對 | [規格](modules/EQP/設備設定/設備群組資料.md)與 PostgreSQL 空表 CREATE SQL 已建立；Vue UI 已補齊環境限定假資料、Apply 與稽核；YMES schema 比對及 Production API 待家用電腦 |
| 設備狀態設定 | WinForms / Service、主表及兩張刪除引用表 Oracle DDL 已追；完整 ValueList/繁中翻譯已核實 | [規格](modules/EQP/設備設定/設備狀態設定.md)與 Vue UI 已對齊；Production CRUD/options API 已部署，家用端待建立引用表 migration/SELECT 權限並啟用精確刪除檢查 |
| 設備點檢表設定 | WinForms 主從 Form / Designer / Service、主從 Oracle DDL、FunctionNo/FunctionKey、QCType ValueList 已核實 | [規格](modules/EQP/設備設定/設備點檢表設定.md)與 Vue DEV 畫面/互動已按來源補齊；Production API 待家用電腦 |
| 載具分類資料 | EP301；WinForms Form/Def/Designer、wsEQP/clsCarrierBasis、Oracle DDL 與 IssueState/簽核規則已核實 | [規格](modules/EQP/載具/載具分類資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 載具類別資料 | EP302；WinForms Form/Def/Designer、wsEQP/clsCarrierBasis、Oracle DDL 與 UseLayer/簽核規則已核實 | [規格](modules/EQP/載具/載具類別資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 載具基本資料 | EP303；frmCarrierBasis/Def、wsEQP/clsCarrierBasis、主表/WIP 狀態/廠區 DDL 與交易/簽核規則已核實 | [規格](modules/EQP/載具/載具基本資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 設備參數資料 | EP401；frmEQPRecipeBasis/Def、雙清單與 Recipe Detail/Copy/Version/簽核 Service 規則已核對；兩張 Oracle DDL、索引及 Type ValueList 已直連查證 | [規格](modules/EQP/設備設定/設備參數資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL、bootstrap 修正與 Vue DEV UI 已建立；Production schema/API/簽核待家用電腦 |

## 物料資料 → 物料設定

| 功能 | WinForms / 資料來源狀態 | Web 規格狀態 |
| --- | --- | --- |
| ML101 物料供應商基本資料 | Form/Service/Core、主從表 DDL 已核對 | [規格](modules/MTL/物料供應商基本資料.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML102 物料類別資料 | Form/Service/Core 與 MTL 主表/屬性/QC/GumHour 結構已核對 | [規格](modules/MTL/物料類別資料.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML104 物料群組資料 | 主從 Form/Service/Core、主表 Oracle DDL 已核對 | [規格](modules/MTL/物料群組資料.md)、Oracle DDL/PG CREATE 與 Vue DEV 頁已建立；Production API 待家用端 |
| ML105 物料類別品管項目設定 | Form/Service/Core、資料同步與 IQC 副作用已核對 | [規格](modules/MTL/物料類別品管項目設定.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML106 物料品管項目設定 | Form/Service/Core、catalog 與 bind log 結構已核對 | [規格](modules/MTL/物料品管項目設定.md)、Oracle DDL/PG CREATE 與 Vue DEV 頁已建立；Production API 待家用端 |
| ML107 物料標籤對應設定 | Form/Service/Core；TBLMTLLABELMAP 與 TBLPRDLABELBASIS Oracle DDL 已由 PowerShell 直連核對 | [規格](modules/MTL/物料標籤對應設定.md)、兩張表 CREATE 參考與 Vue DEV 頁已建立；Production API 待家用端 |

## 全 MES 盤點狀態

- Oracle 第一層 Module 清單、排序及功能目錄筆數已查；ModuleNo 翻譯標題與實際使用者權限仍待核實，553 個 Function 的 WinForms/API 遷移狀態尚需逐項追蹤。
- 每發現一個 Function，新增或更新對應 Markdown，並在本索引登記 Form、主要資料表、API 狀態及待確認事項。
- 本索引及功能規格需與 UI/API 變更同批更新，供公司與家用電腦共同開發。

# 家裡電腦接手清單

## 家用端接手摘要（2026-11-18）

- [x] QC501/QC502 已部署並完成本輪正式 app-browser 操作驗收，測試資料清空；細節見本檔 QC501/QC502 完成紀錄與各模組文件。
- [x] 已整合公司端 `28bc913` 及其前 23 筆新增提交，保留全部 DDL、規格與 UI；處理雙方文件衝突，補正 PRD/PRS 的 TypeScript/初始化/換版格式錯誤。完整 .NET Release 與 Web Vite build 成功。
- [x] 合併新增頁在本機 Development 完成相關修正的基本操作檢查；PRD/PRS 正式 API 與完整 CRUD 驗收仍依下方交接，未標成正式可用。正式站本輪發行範圍為 QC501/QC502。
- [x] GitHub main 已同步整合提交 `340db5f`，包含公司端 `7125143`，未使用 force push。下一個家用端功能先接 QC503「檢測設備連線程式」；沿用 migration 0027 的 Driver 表及 QC501/QC502 共用查找。

## 最新公司端交接：PS102 製造層別設定（2026-11-18）

- [ ] Pull 最新 GitHub `main`，閱讀 `docs/modules/PRS/製造層別設定.md`、`docs/DATABASE_CONNECTION.md` PS102 段落，以及 `database/reference/mestest_ps102_stage_basis_source_ddl.sql` / `mestest_ps102_stage_basis_create.sql`。
- [ ] 先比對 YMES PostgreSQL live schema/migrations 與資料。Oracle source 是 `MESTEST.TBL_FABPRSSTAGEBASIS`；PS102 owner 建議用 `YMES.tbl_fabprsstagebasis`。只有 StageNo PK 與 Sequence unique constraint；不可新增 FK/index 或重建有資料的表。
- [ ] 欄位/nullable/default 照 Oracle：StageNo/StageName/StageType varchar(50) NOT NULL；Sequence numeric(4,0) NOT NULL；IssueState numeric(1,0) NULL DEFAULT 0；Creator/Reviser varchar(10) NULL；CreateDate timestamp NULL DEFAULT current timestamp；DataStamp numeric(11,0) NULL DEFAULT 0；ReviseDate timestamp NULL。`Sequence` 對全表唯一，包含所有 IssueState。
- [ ] CREATE 檔只是參考，先查 live schema/migrations；缺表才新增 forward migration，既有表只針對核實缺少的 default 做 ALTER，不覆蓋資料。
- [ ] 建登入保護 API，FunctionNo=`STAGE BASIS`、FunctionKey=`PS102`：GET `/api/fab-prs-stages?stageNo=`、POST、PUT/DELETE `/{stageNo}`、POST `/{stageNo}/approve`。DTO 和 DEV Vue 契約依模組規格。
- [ ] StageNo/StageName/StageType 必填且最多 50；StageNo 正規化大寫。Sequence 為 1–9999 整數且全表唯一。WinForms Confirm 的 duplicate check 誤把 Edit 自身列算重複；Web UI 排除自身，但 DB unique constraint 仍是最後防線。新增 Creator/CreateDate/IssueState 由 server 設；Edit 只允許狀態 0；DataStamp nullable-safe CAS，衝突回 409。
- [ ] Delete 依 DB 最新 IssueState：0/-1 實體刪除；2 CAS 軟停用為 -1；1 拒絕。舊 Core 沒子表刪除且 Oracle 無 FK，勿自行 cascade。Approve 僅狀態 0，重用 IssueManagement IssueNo=`STAGE BASIS`，無 Job/Detail 才直接核准；有 Detail 建簽核、設 1、通知第一關，由 callback 設 2/0。
- [ ] PS104 `tblprsnodebasis.StageNo` 應重用此 API 的已核准層別查找，不建立重複表。完成 live schema/API/migration 後更新規格/TODO，commit/push `main`。
- [ ] 公司端 PS102 已做 Vue DEV localStorage 假資料和 Oracle DDL 參考，未連 YMES PostgreSQL、未做正式 API/migration；未執行 build/test/browser。

## 最新公司端交接：PS101 生產區段資料（2026-11-18）

- [ ] Pull GitHub `main`，閱讀 `docs/modules/PRS/生產區段資料.md`、`docs/DATABASE_CONNECTION.md` 的 PS101 段落，以及 `database/reference/mestest_prs101_production_segment_source_ddl.sql` / `mestest_prs101_production_segment_create.sql`。
- [ ] 查 YMES PostgreSQL live schema/migrations 的 `YMES.tblprsproductionsegment`。PS101 是此表 owner；先核對欄位、PK、nullable/default 及現存資料。缺表才新增 forward migration；若已存在，保留資料，只補 live schema 明確缺少的 default，禁止重建。
- [ ] 原始 Oracle DDL 由公司端以 PowerShell 5.1 + Oracle.ManagedDataAccess 直接連 MESTEST 唯讀取得。欄位及預設照 Oracle source：PSNo/PSName varchar(50) NOT NULL；PSOrder numeric(2,0) DEFAULT 0 NOT NULL；Description varchar(255) nullable；IssueState numeric(1,0) DEFAULT 0 NOT NULL；HaveComponent/HaveLevel numeric(1,0) DEFAULT 0 NOT NULL；SerialTypeNo_Lot/Comp varchar(50) nullable；IsPartial numeric(1,0) DEFAULT 0 NOT NULL；IsNetFlow numeric(1,0) DEFAULT 0 nullable；DataStamp numeric(11,0) DEFAULT 0 nullable；Creator/Reviser varchar(10) nullable；日期 timestamp nullable。只有 PSNo PK unique index；無 FK/其他 index。
- [ ] `CREATE` 參考不是 migration。先比較 live PostgreSQL table 與 migrations，只新增缺表/必要 default forward migration，不覆蓋現有資料；若要使用 reference SQL，先確認 schema/table 名稱並做 migration review。
- [ ] 實作登入保護 FunctionNo=`PS BASIS`、FunctionKey=`PS101` API：GET `/api/production-segments?psNo=`、POST、PUT/DELETE `/{psNo}`、POST `/{psNo}/approve`。請依 `docs/modules/PRS/生產區段資料.md` 保持 Vue 契約及 DTO。
- [ ] 新增/編輯驗證：PSNo 必填、轉大寫、最多 50；PSName 必填最多 50；PSOrder 1–99；Description 最多 255；HaveComponent/HaveLevel 僅 0/1 且不可同時為 1。新增由 server 設 Creator/CreateDate、IssueState=0；DB 預設 IsPartial/IsNetFlow/DataStamp。Edit 僅 IssueState=0；DataStamp null-safe CAS、成功遞增，衝突回 409；Reviser/ReviseDate 由 server 設。
- [ ] Delete 以資料庫目前狀態分流：0/-1 實體刪主表，2 CAS 改 -1，1 拒絕。舊 Core 沒有 cascade/reference cleanup，Oracle 無 FK；不要自行新增 cascade 或假定無引用。多選逐筆回報結果。
- [ ] Approve 僅狀態 0，重用共用 IssueManagement IssueNo=`PS BASIS`：沒有 Job/Detail 才直接改 2；有簽核明細先建立簽核流程、改 1、通知第一關，callback 核准 2/退回 0。
- [ ] 重用/建立共用 GET `/api/system/serial-rule-basis?serialType=LOTNO`，查既有 `TBLSYSSERIALRULEBASIS`。WinForms 沒傳 Enable 條件，因此 lookup 不要只傳啟用列。不要新建另一張序號類別表。Component 序號欄位在此畫面隱藏，編輯/Copy 要保存既有值。
- [ ] WAHHONG 企業設定會強制 HaveComponent/HaveLevel=No 並停用 Radio；必須依可信任的登入企業資訊套用。PS104 流程主檔若需要 Production Segment lookup，重用 PS101 table/API，不要新增同義 table/controller。
- [ ] 完成 schema/API、更新規格/TODO 後 commit/push `main`；公司端再 pull 對接。公司端 PS101 使用 DEV localStorage；未連 YMES PostgreSQL、未實作 Production API/migration、未執行 build/test/browser。

## 最新公司端交接：PS103 流程類別資料（2026-11-18）

- [ ] Pull 最新 GitHub `main`，讀 `docs/modules/PRS/流程類別資料.md`、`docs/DATABASE_CONNECTION.md` 的 PS103 段落，以及 `database/reference/mestest_prs103_process_type_source_ddl.sql` / `mestest_prs103_process_type_create.sql`。
- [ ] 查 YMES PostgreSQL live schema/migrations 的 `YMES.tblprstype`。PS103 是此表的 owner；先核對欄位、PK、nullable/default 和現存資料。缺表才新增 forward migration；若已存在，保留資料，只補 live schema 確認缺少的 default，禁止重建。
- [ ] Oracle 結構：`processtype varchar(20) NOT NULL` PK；`description varchar(255) NULL`；creator/reviser varchar(10) nullable；create/revise date nullable；`issuestate numeric(1,0) DEFAULT 0 NOT NULL`；`datastamp numeric(11,0) DEFAULT 0 NULL`。來源僅 PK unique index，無 FK/額外 index。PostgreSQL CREATE 檔是參考，不是直接部署 migration。
- [ ] 實作登入保護 FunctionNo=`PRS TYPE`、FunctionKey=`PS103` API：GET `/api/process-types?issueState=&processType=`、POST、PUT `/{processType}`、DELETE `/{processType}`、POST `/{processType}/approve`。Vue DEV 頁 `/YMES/PRS/ProcessType` 已固定此契約。
- [ ] Add/Copy 共用新增流程：ProcessType 必填、正規化大寫、最多 20；Description 最多 255；server 設 Creator/CreateDate。除 PK unique 外，新增時還要以不分大小寫檢查 `TBLPRSProcessBasis.ProcessNo` 衝突，來源 clsProcess 明確禁止兩種識別碼相同。
- [ ] Edit 僅允許 IssueState=0、ProcessType PK 不可改，只更新 Description、Reviser/ReviseDate，DataStamp null-safe CAS 並遞增，競爭回 409。IssueState 0/-1 刪除時同一 transaction 先清 `tblprsprocessproperty` 中 `processno=ProcessType` 的列再刪 `tblprstype`；狀態 2 CAS 改 -1，狀態 1 禁止。多選 UI 是逐筆處理，回報每筆成功/失敗。
- [ ] Approve 只允許狀態 0，重用共用 IssueManagement IssueNo=`PRS TYPE`。無 Job/Detail 直升 2；有簽核設定先轉 1、發第一關通知，callback 核准 2/退回 0；API 不得略過簽核流程。
- [ ] Property 按鈕重用 shared `TBLPRSProcessProperty` API。legacy 以 ProcessNo=ProcessType 開啟且 ProcessVersion 未指定，列表查詢省略版本以載入該 ProcessNo 的所有版本；Property 編輯畫面空白 ProcessVersion 對應資料庫鍵值 -1，新增、修改、刪除均須以 -1 傳送而 UI 顯示空白；default value 需重用 Element 驗證，新增檢查 ProcessNo/version，核准 IssueNo=`PRS PROPERTY`。勿將屬性列複製到新 ProcessType。協調 PS104，避免複製兩套 property controller。
- [ ] PS104 `GET /api/process-basis/lookups/process-types?issueState=2` 應查同一張 `tblprstype`，重用 PS103 schema/lookup，不另建 lookup 表。完成 schema/API 後更新規格和 TODO、commit/push `main`；公司端再 pull 對接。公司端 PS103 使用 DEV localStorage，未連 YMES PostgreSQL，也未執行 build/test/browser。

## 最新公司端交接：PS104 流程基本資料（2026-11-18）

- [ ] Pull GitHub `main`，先讀 `docs/modules/PRS/流程基本資料.md`、`docs/DATABASE_CONNECTION.md` 的 PS104 段落，以及 `database/reference/mestest_prs104_process_basis_source_ddl.sql` / `mestest_prs104_process_basis_create.sql`。
- [ ] 比對 YMES PostgreSQL live schema、migration 和欄位 defaults。Create SQL 是對照，不可直接整份執行、不可重建既有表。First-phase 有 `tblprsprocessbasis`、`tblprsnodebasis`、`tblprsopapsdetail` 三表；確認 live objects 後，只為缺失建立 migration。
- [ ] Oracle DDL 中三個 `VARCHAR2(n CHAR)` 長度與 PostgreSQL varchar(n) 相同；`ALL_TAB_COLUMNS.DATA_LENGTH` 是 bytes，不是來源字元數，不要用 byte 數誤改欄寬。
- [ ] Source defaults 與 first-phase Create 目前可見差異：ProcessBasis 的 IssueState/TuningProcess/CurVersion/PartialType/DataStamp = 0；NodeBasis 的 NodeType=0、NodeVersion='N/A'；OPAPSDetail 的 ApsFixEqpTime/ApsVarEqpTime/BatchOP=0、BatchMaxQty=999999。先查 live `pg_attrdef`，只對 live 缺少的 default 加 forward `ALTER COLUMN SET DEFAULT`；保留 nullable/原資料。
- [ ] 首次新增 PS104 專屬的四張缺表：`tblprsnoderelation`、`tblprsnodexml_clob`、`tblprsprocessproperty`、`tblprsopapsbase`。`tblprstype` 由 PS103 建立、`tblprsproductionsegment` 由 PS101 建立；PS104 重用兩張共用表及其 API/lookup，不另建重複表。欄位、PK、NULL/default 依 Oracle DDL/CREATE 參照，Oracle source 僅 PK unique index，無 FK/額外 index。不要自行新增索引/FK。
- [ ] 完成登入保護 PS104 API，路徑、DTO 和 DEV Vue 實際呼叫見功能規格：process basis list/Add/Edit/Delete/Approve/Copy/Version、Property CRUD/Issue、Exclude OP、已核准 PS/ProcessType lookup 及 Excel import。
- [ ] IssueState 語意：0 未核准、1 簽核中、2 已核准、-1 不使用。Edit/Draw 只允許 0；Version 只允許 Active/current；刪除 0/-1 實體清理子表，2 用 DataStamp CAS 標成 -1，1 禁止。多表 delete/copy 使用單一 transaction。Creator/Reviser/時間由 API principal/server 設定；DataStamp null-safe CAS，衝突回 409。
- [ ] 核准不能由 API 直接改狀態：核對非空流程圖、Exclude OP；無 Issue Job/Detail 才可 direct Active，有設定則走共用 IssueManagement IssueNo=`PRS BASIS`，維持 Pending、簽核 callback、第一關通知與 CurVersion 前版切換。
- [ ] Flow Form 依節點/連線表和 `NodeXmlString`/CLOB snapshot。舊 XML 由外部 iMESXMLFlow.dll 序列化；未證明相容前不要把 Vue JSON 寫入 XML 欄位，也不可對外宣稱 Web 流程圖可和舊 WinForms 互通。決定 serializer 方案後才開放 Production graph write。
- [ ] 流程匯入舊程式只讀 xls/xlsx 第一個工作表、逐 ProcessNo/Version 呼叫外部 `DataImporter.DataImport.ProcessDataChecker`；程式碼不在來源樹。檢查部署是否可取得該 DLL/規則，否則以 API 實作核准作業站、同一區段、類別、重複主鍵、逐筆結果及取消/背景作業，不可回傳假成功。
- [ ] 完成 schema/API 後更新規格與 TODO、commit/push `main`，公司端再 pull 對接。公司端 PS104 Vue DEV 使用 localStorage，未連 YMES PostgreSQL；未執行 build/test/browser。

## 最新公司端交接：OPB01 作業站參數群組設定（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/OP/作業站參數群組設定.md`、`docs/DATABASE_CONNECTION.md` OPB01 段落，以及 Oracle source DDL / PostgreSQL CREATE-only 對照。
- [ ] 先比對 YMES live schema/migrations 的 `YMES.tblopgroupattrib`。Oracle DDL 在 `database/reference/mestest_opb01_operation_group_attribute_source_ddl.sql`；空表 CREATE 參考在 `database/reference/mestest_opb01_operation_group_attribute_create.sql`。只缺表才新增 forward migration；既有表只做必要 ALTER，不重建、不覆蓋資料。
- [ ] `TBLSYSELEMENTBASIS` 已由 first-phase schema 提供，不要建立重複 Element 表；重用客戶基本資料與 OPB02 ECN 群組 lookup。
- [ ] 實作登入保護的 OPB01 API，FunctionNo=`B_OP GROUP ATTRIB`、FunctionKey=`OPB01`。Vue `OperationGroupAttribMaster.vue` 已固定 `GET /api/operation-group-attributes`、`GET .../editor`、`POST .../batch-save`、`DELETE ...`、三種 lookup、Element values/validation 及 `GET /api/system/parameters/InputAbortLog` 路徑；DTO 見模組規格。
- [ ] 主表複合 PK `(CUSTOMERNO, OPGROUPNO, ELEMENTNO)`；來源 NOT NULL、欄長、nullable/default 照 Oracle DDL。`BYSECONDCLASS`、`DATASTAMP` nullable DEFAULT 0。Oracle 只有 PK unique index，無 FK/其他 index；不要自行加 FK/index。
- [ ] 查找僅用已核准客戶、`TBLOPECNGROUPBASIS.ISSUESTATE=2` ECN 群組、`TBLSYSELEMENTBASIS.ISSUESTATE=2 AND ELEMENTTYPE=5` 產品 Element。CustomerNo=`*` 表共用。Select List values 重用 `wsSYS.LoadElementValue`；Key In 驗證重用 `wsSYS.ChkElementValueValid`/SYS Core，不能只檢查前端清單。
- [ ] Batch Save 使用單一 transaction，依新增/修改/刪除旗標處理所有列；任一筆失敗整批 rollback。修改用 DataStamp null-safe CAS，舊值 NULL 以 `IS NULL` 比對，成功遞增；未命中回 HTTP 409。Audit 僅由 API principal/server 設定。
- [ ] 複製改變 CustomerNo 或 OPGroupNo 時，對目的複合鍵新增存活明細，原群組不變；目的 PK 已存在則沿用舊 Copy Core 的略過語義。DataFlag=2 的刪除列不可複製。
- [ ] 多選主 Grid 刪除依舊 WinForms 逐筆呼叫；每筆處理 optional `InputAbortLog`，不得把 UI request 當成批次原子交易。Element editor 的多列 Apply/Confirm 則必須是原子 batch transaction。
- [ ] 檢查 YMES 權限、既有客戶/ECN/System Element API，並更新 API/schema 規格與 TODOLIST。完成後 commit/push `main`，公司端再 pull 對接 Production API。DEV localStorage 假資料不可寫正式資料庫。

## 最新公司端交接：OPB02 ECN 作業站群組設定（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/OP/ECN作業站群組設定.md`、`docs/DATABASE_CONNECTION.md` OPB02 段落，以及 Oracle source DDL / PostgreSQL CREATE 對照。
- [ ] 比對 YMES PostgreSQL live schema/migrations：`YMES.tblopecngroupbasis`、`YMES.tblopecngroupdetail`。Oracle source DDL 在 `database/reference/mestest_opb02_ecn_operation_group_source_ddl.sql`；CREATE-only 參考在 `database/reference/mestest_opb02_ecn_operation_group_create.sql`。只補缺表的 forward migration；既有表只做必要 alter migration，不重建、不覆蓋資料。
- [ ] 實作 FunctionNo=`B_ECN OP GROUP BASIS`、FunctionKey=`OPB02` 的登入/API 權限及規格所列 endpoints。Vue 固定使用 `/api/ecn-operation-groups`、`/{opGroupNo}/operations`、`/lookups/production-segments`、`/lookups/operations`、POST/PUT/DELETE、`/{opGroupNo}/approve`。
- [ ] 主表 `OPGROUPNO VARCHAR2(20)` PK；GroupName 50、PSNo 50、Description 255；OPGroupOrder NUMBER(6,0) nullable；IssueState NUMBER(1,0) NOT NULL 且 Oracle 無 default；DataStamp NUMBER(11,0) nullable DEFAULT 0。保留來源 NULL/default，DataStamp 用 null-safe CAS，衝突回 409；audit 僅由 API principal/server 設定。
- [ ] Detail PK 是 `OPNO` 單欄全表唯一（不是 `(OPGROUPNO, OPNO)`），OPGroupNo/OPNo 都必填 20 字元。新增只能選 `TBLOPBASIS.IssueState=2`、`PSNo` 與所屬群組完全相等，且目前未出現在任何 `TBLOPECNGROUPDETAIL` 的 OP；這是全域排除。不要把 `PSNo='*'` 當萬用值。
- [ ] 主檔修改僅狀態 0；刪除時 DB 狀態 0/-1 在單一 transaction 先刪 detail 再刪 basis，狀態 2 採 DataStamp CAS 改 -1 並保留 details，狀態 1 禁止。不可相信 request body 的 IssueState 作為授權依據。來源沒有 FK 或 PK 以外的 index，不要自行新增。
- [ ] 核准使用 IssueNo=`OP ECN GROUP BASIS`：重用共用 IssueManagement。無 Job 或 Job 無 Detail 時直接 0→2；否則建立 Job/Detail、設定 1，核准 SQL 更新 2、退回 SQL 還原 0，通知第一關。不要在 Vue/API 繞過既有簽核服務或遺漏通知。
- [ ] 明細新增/刪除為逐筆操作、沒有明細 Edit；明細可在任何群組狀態操作（WinForms 沒有狀態限制）。明細新增需重驗群組、OP 核准狀態、PS 相同及全表 OPNo 唯一，刪除實體資料。
- [ ] API/schema 完成後更新模組規格與 TODO、提交並 push `main`，供公司端 pull。DEV localStorage 假資料不得寫入正式資料庫。

## 最新公司端交接：OPB03 客戶站點對應設定（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/客戶站點對應設定.md、OP101–OP105 規格，以及 database/reference/mestest_opb03_customer_op_mapping_source_ddl.sql 和 mestest_opb03_customer_op_mapping_create.sql。
- [ ] 先核對 YMES PostgreSQL live schema/migrations 的 tbl_bopcustopmapbasis、tbl_bopcustopmapdetail。Oracle DDL 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連查證；source 只有兩表 PK、無 FK/額外索引。CREATE-only 檔不是直接部署指令；僅缺表才新增 forward migration，既有表先比欄位與資料。
- [ ] 實作登入保護 FunctionNo=B_CUSTOMER OP MAPPING API。Vue CustomerOPMappingMaster.vue 頁面 /YMES/OP/CustomerOPMapping 已固定主/明細 CRUD 路徑及 lookup 路徑，細節見規格。
- [ ] lookup 重用既有 Customer Basis、Production Segment、OP Basis API/資料；客戶與區段、作業站都只提供 IssueState=2。作業站需依主檔 PSNo 過濾，不新增相同主檔 lookup tables。
- [ ] Basis PK (CustomerNo, CustomerOPNo)；Detail PK (CustomerNo, CustomerOPNo, OPNo)。必填及字數依 Oracle source DDL；TransactionFlag 僅 0/1。來源 CustomerOPSeq NUMBER(3,0)，但 WinForms NumericUpDown 範圍 0–999999；UI 保留原範圍，API 回清楚 DB 欄寬驗證錯誤，除非查 live schema/現存資料後另有遷移決定。
- [ ] Edit 主檔 PK 不可變；已有明細時 PSNo 不可改。明細可修改 OPNo，路由用舊 OPNo 對應、body 帶新 OPNo，需驗複合 PK 唯一。
- [ ] DataStamp nullable + DEFAULT 0；Basis/Detail edit 都要 null-safe CAS，衝突回 409；Creator/CreateDate/Reviser/ReviseDate 只由 API principal/server 產生，忽略 request audit 欄位。
- [ ] 主檔刪除在單一 DB transaction 先刪全部 Detail 再刪 Basis；Oracle 無 FK，不要假定 DB cascade。Detail delete 為實體刪除。
- [ ] Production API/schema 完成後更新規格與交接、提交並 push main，供公司端 pull 整合。公司端尚未接 YMES PostgreSQL；未執行 build/test/browser。
## 最新公司端交接：OP105 作業站群組資料（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/作業站群組資料.md、docs/DATABASE_CONNECTION.md 的 OP105 段落、OP101–OP104 規格，以及 database/reference/mestest_op105_group_basis_source_ddl.sql 與 mestest_op105_group_basis_create.sql。
- [ ] 先核對 YMES live schema/migrations 的 tblopgroupbasis、tblopgroupdetail。Oracle 原始欄位、PK/NULL/default 已直接查證。database/reference/mestest_first_phase_create_tables.sql 已校正新庫預設值；CREATE TABLE IF NOT EXISTS 不會更新既有表。若 live schema 缺 DEFAULT 0，依現有 migration 慣例只補必要 forward migration，不重建表、不覆蓋資料。
- [ ] 實作 FunctionNo=OP GROUP BASIS 登入/API 權限。Vue OperationGroupMaster.vue Production 路徑：GET /api/operation-groups?opGroupNo=&psNo=、GET /{opGroupNo}/members、POST/PUT/DELETE 群組、POST/PUT/DELETE 群組成員；路徑與 DTO 見功能規格。
- [ ] 實作 GET /api/operation-groups/lookups/production-segments，重用既有 PS lookup；另實作/映射 GET /api/operation-groups/lookups/operations?psNo=&issueState=2，只回 IssueState=2 的已核准 OP。PSNo='*' 時不限制區段。不要建重複作業站或生產區段表。
- [ ] 主檔驗證：OPGroupNo 必填、大寫、最多 20；OPGroupName 必填、最多 50；PSNo 必填、最多 50；OPGroupOrder 0–999999 整數；Description 最多 255。編輯以 DataStamp CAS，衝突回 409；Oracle DataStamp nullable + DEFAULT 0，舊值為 NULL 時須用 null-safe CAS 條件，不能把 NULL 靜默當成 0。Creator/CreateDate/Reviser/ReviseDate 由 API principal/server 寫入。
- [ ] 成員驗證：複合 PK (OPGroupNo, OPNo)；OPRoleFlag 僅 0/1/2；WeightRate 必填且 0–100、最多兩位小數；新成員必須是此群組區段中的已核准 OP；修改只能變更角色/權重並採 DataStamp CAS；DataStamp nullable + DEFAULT 0，舊值為 NULL 時須用 null-safe CAS。audit 不信任 body。
- [ ] 主檔刪除必須在單一 DB transaction 內先刪 TBLOPGROUPDETAIL 再刪 TBLOPGROUPBASIS。成員刪除為實體刪除。不得加 Oracle 未定義的 FK/index；依 YMES 的 schema extension 習慣處理參照完整性。
- [ ] Check 是 WinForms 清單端即時檢核，不需 API 寫入：同一群組 Entry 最多 1、Exit 最多 1、WeightRate 合計 100；不要求 Entry/Exit 各至少一筆。不要讓合計規則阻止使用者逐筆編輯中間狀態的明細。
- [ ] OP Basis 刪除會以 TBLOPGROUPDETAIL 引用阻擋；確認 OP104 API 保持此保護。
- [ ] 完成 API/schema 後更新規格、migration/API handoff，提交並 push GitHub main。公司端尚未連 YMES PostgreSQL，未執行 build/test/browser 驗收；API/schema 完成後通知公司端 pull 整合。

## 最新公司端交接：OP103 作業站類別資料（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/作業站類別資料.md、docs/DATABASE_CONNECTION.md OP103 段落與 WinForms frmOPType/Def、wsOP、clsOpBasis 規則摘要。
- [ ] 先查 YMES PostgreSQL live schema/migrations 的 YMES.tbloptype、tblopbasis 與 Property 表。原生 MES Oracle DDL 在 database/reference/mestest_op103_optype_source_ddl.sql；空表 CREATE 對照在 database/reference/mestest_op103_optype_create.sql。CREATE 只是參考；只對實際缺表建立 forward migration，保留既有 YMES 資料/擴充欄，不重建 tblopbasis、Property 或 Issue/Job 共用表。
- [ ] API：登入保護 GET /api/operation-types?issueState=-1&opType=、POST、PUT /{opType}、DELETE /{opType}、POST /{opType}/approve。Vue OperationTypeMaster.vue 已固定以上路徑/body；如需改路由，前後端與此 spec 一起更新。
- [ ] 新增驗證 Oracle 欄位長度（OPType 20、Description 255）、Trim/大寫 OPType、PK 唯一；另檢查 TBLOPBASIS.OPNO 大小寫不敏感衝突，因舊 Core 把 OPType/OPNo 視為同一 Property namespace。
- [ ] 修改只允許 IssueState=0，僅更新 Description；DATASTAMP 在 Oracle 可 NULL、預設 0，確認現有 YMES 型別/live NULL 語義後採 CAS 並回 409；Creator/日期、Reviser/日期與狀態不信任前端，由 API 管理。
- [ ] Delete 0/-1 硬刪、2 改 -1、1 禁止。舊 clsOpBasis.DelOPType 先刪 TBLOPPROPERTY 中相同 OPNo 屬性再刪類別；確認 YMES 的共享屬性資料讀寫策略、OPType/OPBasis 碰撞與實際 FK/引用，使用單一 transaction，不要在 schema reference 臆造 FK。
- [ ] Approve 只允許 0，IssueNo=OP TYPE；共用 Issue Job/Detail 不存在才 direct Active(2)，有流程則 Pending(1)、通過 2、退回 0，第一關通知由共用簽核服務處理。不要在 API/前端自行猜狀態或重建 Issue 表。
- [ ] Property WinForms 呼叫 OP_OPProperty 並以 OPType 當 OPNo。先核對 OP BASIS 共用 Property API/表，不要為 OP103 重複建 Property table。完成 API/schema 後提交並 push；公司端 DEV mock 不會寫入正式 DB。
- [ ] OP103 本回合僅新增 Oracle source DDL、PG CREATE-only 參考、Vue DEV UI、規格及 handoff；未連 YMES PG、未實作 API/migration，未執行 build/test/browser 驗收。
## 最新公司端 handoff：QC409 OCAP異常單處置調整（2026-11-18）

- [ ] Pull 最新 main，閱讀 docs/modules/QC/OCAP異常單處置調整.md、QC408 OCAP流程規格與本檔下方 QC408 handoff、docs/DATABASE_CONNECTION.md QC409 段落。
- [ ] Schema 核對：三張專用表 DDL 在 database/reference/mestest_qc409_wip_ocap_source_ddl.sql，PG CREATE-only 參考在 database/reference/mestest_qc409_wip_ocap_create.sql。先比對 YMES live schema/migrations；只補缺的 forward migration。不要重建或覆蓋共用 WIP 表。
- [ ] 11 張 WIP/ERF/通知依賴的 MESTEST source DDL/index 在 database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql。逐張比對 YMES 現有 WIP/ENT/USR schema 和 API；表已存在時重用既有 mapping，不另建同義表。若有必要 mapping 缺表，先由來源 DDL 設計適配 migration，保留舊 schema/資料。
- [ ] 完成 /api/qc/ocap-dispositions 查詢及 route-options、POST .../{erfNo}/{holdItemNo}/adjust；授權 FunctionNo=OCAP DISPOSITION ADJUST / FunctionKey=QC409。
- [ ] 這不是只寫三張 OCAP history/state 表的 CRUD。依規格復刻 wsWIP.Exe_ERF_Disposition → clsLot.UpdOCAPAndERF_Disposition：ERF history/status、Lot Hold/LotStamp、WIP LotLog/GoodBin、OCAP current/group state、DispOrder、批次 ERF 分支、使用者定義 hook 及通知。
- [ ] Finish 必須在 transaction 中重新計算同 ERF 的剩餘 Hold Item：最後一項採 DispType=2/Final；仍有其他項目則 DispType=0 但本筆 OCAPStatus=11。避免同時操作導致錯誤結案；系統 ERF 必須驗證新鮮 LotStamp，衝突回 409。
- [ ] Change Group 只接受 server 從 OCAP graph/version/relation 驗證的可達節點；不可接受任意 GroupNo/NodeId/LinkName。附件和 mail 使用 YMES 現有 extension/outbox；確認不重複呼叫舊 side effect。
- [ ] 完成後更新 migration/API 規格與 TODO，push GitHub main。公司端 pull 後以 Vue DEV UI 整合驗收；公司端尚未做 build、測試或瀏覽器驗收。

## 最新公司端 handoff：QC408 OCAP基本資料（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/QC/OCAP基本資料.md`、`docs/DATABASE_CONNECTION.md` QC408 段落，以及 Oracle source DDL / PostgreSQL CREATE 對照。
- [ ] 比對 YMES live schema 與 migrations：`tblqcocapbasis`、`tblqcocapnodebasis`、`tblqcocapnoderelation`。只為缺表補 forward migration，維持來源 PK、NULL/default 和 `OCAPLINKNAME` PK 隱含 NOT NULL；不新增 Oracle 未定義的 FK/index、不重建既有資料。
- [ ] 原始 Oracle DDL 在 `database/reference/mestest_qc408_ocap_source_ddl.sql`；PG 空表 CREATE 參考在 `database/reference/mestest_qc408_ocap_create.sql`。公司端已用 PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取 DDL；不要在 Oracle 執行 CREATE。
- [ ] 先確認 `OCAPXMLSTRING` 相容策略：WinForms 呼叫二進位 `iMESXMLFlow.dll` 的 `FlowToXML/XMLToFlow`，目前舊源碼庫沒有 serializer source。決定反編譯/相容轉換，或明確採 YMES 自有格式；不可把 Vue nodes/links JSON 直接寫成舊 AddFlow XML 並宣稱可相容 MES。
- [ ] 實作登入/`OCAP BASIS`/QC408 權限保護 API：GET 清單及已簽核 HoldDispGroup lookup、POST 新增、PUT 說明、POST copy、PUT flow、POST approve、DELETE。細節以功能規格的 API 表為準，SQL 參數化，audit 和時間由 server 管理。
- [ ] Flow 儲存/複製需同交易更新主 CLOB、NodeBasis、NodeRelation；保持 NodeID 和 relation 端點一致。實作 DataStamp CAS；舊 DataStamp NULL 要定義明確處理。流程狀態及刪除：未簽核 0 與停用 -1 硬刪三表；已核准 2 軟停用為 -1 並清 CurVersion；簽核中 1 禁刪；核准 0→2 並同 OCAPNo 只留一個 CurVersion=1。
- [ ] 補 `FunctionNo=OCAP BASIS`、`FunctionKey=QC408` 授權；驗證 OCAP No/Version 組合唯一、版本長度 5、No 長度 50、Description 長度 255、節點和 relation 主鍵、GroupNo 已簽核、DataStamp 衝突回 409。
- [ ] 完成 API/schema 後推到 GitHub，通知公司端 pull。不要將 DEV localStorage 假資料灌入資料庫。

## 最新公司端 handoff：QC503 檢測設備連線程式（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/QC/檢測設備連線程式.md`、QC501/QC502 規格與 `docs/DATABASE_CONNECTION.md`。
- [ ] 主表沿用 `TBLQCINSPDRIVER`，比較 YMES live schema/migrations 與 `database/reference/mestest_qc501_inspection_machine_create.sql`；確認缺表才建 migration，不重建既有資料。QC503 不需要新增 Oracle DDL 或重複 CREATE SQL。
- [ ] 實作登入保護 API：GET `/api/qc/inspection-drivers?issueState&inspDriverNo`、GET `/api/qc/inspection-drivers/functions`、POST/PUT/DELETE/approve，授權 FunctionNo=`INSPMACHINE DRIVER`/FunctionKey=`QC503`。DriverNo PK 上限25、FunctionName 50、Description 255；DataStamp nullable+default、IssueState/DataStamp CAS、server audit、參數化 SQL。遇舊 NULL DataStamp 要按 live data/legacy 語義處理，不能跳過版本檢查。
- [ ] Function options 必須由允許清單提供；本分支 WinForms reflection 只列出 `funDrv_Thick`、`funDrv_Channel`、`funDrFunctionC`，目前三個 legacy method 都回傳 default string。不要把任意用戶端字串反射執行。
- [ ] 核准使用 IssueNo=`INSPMACHINE DRIVER`：依 Issue Job/Detail 決定直接 Active(2)或 Pending(1)，Issue Process 退回時回 0；前端不自行決定結果狀態。Edit 依共用 Issue Control，Pending 不可修改。
- [ ] 刪除狀態 0/-1 硬刪、狀態2 改 -1 並遞增 DataStamp；1 禁止。舊 MES 沒明確檢查 QC502 儀器的 Driver 引用且 Oracle 無 FK；先查 YMES live schema/既有規則，再決定參照中 Driver 的防刪策略，記錄差異。
- [ ] Driver GET 在不帶狀態條件時仍需回傳所有狀態，因 QC502 共用 Driver lookup 未限制 IssueState。逐筆動作遇首個失敗就停止的 WinForms 行為需保留。API/schema 完成後推送，供公司端 pull 整合。

## 家用端進度：QC501/QC502 檢測儀器（2026-11-18）

- [x] QC501–QC503 六張共用表 live schema audit 確認缺表；先備份並驗證 `/opt/ymes/backups/ymes_before_0027_qc501_20261118.dump`，再套用 migration `0027_qc501_qc502_inspection_machines.sql`，授予 `ymes_app` 表級 CRUD。
- [x] 實作並部署 QC501/QC502 登入保護 API、參數化 SQL、DataStamp/IssueState CAS、audit、欄位檢查、類別/Driver lookup、刪除引用檢查；Release build 0 warning/0 error，API readiness=200，未登入 API=401。
- [x] Production app-browser 已驗證 QC501 新增/套用/修改、複製 UserDefine/Driver lookup、核准取消不改資料後確定轉 2，以及被儀器引用的類別刪除回 409 且保留資料。
- [x] QC502 新增/套用/修改、類別 A RS232→類別 B UserDefine 更換、無類別複製/Driver lookup、核准轉 2 與刪除儀器 B 均經 app-browser 驗證；舊 DataStamp 在 DB 改成 NULL 後保存回 409，重查 NULL 後保存成功。
- [x] 精確 nullable DataStamp CAS、交易內鎖定權威類別/Driver、Apply 使用 server DTO 與新增轉修改模式已實作。.NET 9 Release build 0 warning/0 error；頁內確認框已取代 `window.confirm`。最終部署後 app-browser 確認 ProblemDetails 顯示清楚中文 detail，不顯示 JSON。
- [x] 本輪與先前測試儀器/類別均已清理；暫存 Driver 以交易清理，遠端精確 fixture 計數為 0。
- [x] 收尾驗證通過：QC501 狀態 1 編輯欄位/保存停用、-1 與 2 類別刪除成功；QC502 狀態 0 刪除先取消保留再確定刪除，狀態 2 刪除成功。兩頁四狀態篩選、Close 回空白首頁及登出/Demo 登入均通過；API/Web healthy、readiness=200。
- [x] 本次家用端變更已隨 `340db5f` 同步 GitHub main，保留公司端 `7125143` 與 QC503 UI 提交 `b572f3b`；已讀新版待辦，下一步為 QC503 家用端 API。

## 最新公司端交接：QC501 檢測儀器類別（2026-11-18；家用端已接手）

- [x] 家用端已完成 live schema audit、備份、0027 migration 與 QC501 API；細節見 `docs/modules/QC/檢測儀器類別.md`。主鍵 InspMachineType 單欄，IssueState/DataStamp nullable，ChNo 長度 3。
- [x] app-browser 已驗證新增/套用/修改、複製/Driver lookup、核准取消/確定、引用刪除防護、狀態 1 唯讀、-1/2 成功刪除、四狀態/Close 與最終錯誤文字。共用 Issue/Job 表不存在時核准轉 ACTIVE，若表出現則 API fail closed；Issue Control 尚未接入。
- [ ] 公司端 pull 家用端同步後，確認 Vue DTO/API contract 與正式 route；不要把 DEV mock 資料送至正式 DB。
- [ ] 公司端已提交 QC503 UI（`b572f3b`）；家用端 merge 後讀規格並接手 API，沿用 0027 六表與 CREATE/source DDL。

## 前次公司端交接：QC502 檢測儀器基本資料（2026-11-18）

- [x] 家用端已建立 QC502 GET/POST/PUT/DELETE/approve API，沿用 0027 六表 migration 與 QC501 class/Driver lookup；規則見 `docs/modules/QC/檢測儀器基本資料.md`。
- [x] app-browser 已驗證新增/套用/修改、跨通訊型別類別帶入、複製/Driver lookup、核准、取消/確定刪除、nullable DataStamp 409 與重查後保存、四狀態/Close、最終錯誤文字及 Demo 登入；本輪 fixture 已清空。
- [ ] 公司端 pull 家用端同步並確認正式 API/DTO；QC503 UI 已提交，下一項由家用端接手 API，沿用 QC501 已查 DDL。

## 最新公司端 handoff：QC305 產品規格設定（2026-11-17）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/QC/產品規格設定.md`、`docs/DATABASE_CONNECTION.md`。FunctionNo=`PRDQCITEM`、FunctionKey=`QC305`；Vue DEV 頁 `/YMES/QC/ProductSpec` 位於品質資料 → 計量規格設定 → 產品規格設定。
- [ ] 核對 YMES PostgreSQL live schema/migrations 與 QC302 API。Chart Basis/Detail/DetailLog 使用 migration 0024；產品 Basis Oracle DDL 參照 `mestest_qc218_tblprdproductbasis_source_ddl.sql`，PRDQCITEM/Variable/Factor 參照 QC205，Group 參照 QC212。QC305 沒有專屬資料表，不要新增重複 CREATE；`TBLPRDTYPE` 先查 live schema，只有確認 YMES lookup 依賴缺漏才請公司端取得 Oracle DDL。
- [ ] 建立登入保護 QC305 API：`GET /api/qc/product-spec/product-types`、`GET /api/qc/product-spec/products?productType&productNoPrefix`、`GET /{productNo}/qc-items`、`GET /{productNo}/chart-details`、`POST /api/qc/product-spec/batch-update`。依既有授權方式檢查 PRDQCITEM/QC305；若已存在等效 API，沿用並更新 Vue/spec。
- [ ] 查詢忠實採舊 `LoadProductBasisJoinPRDQC`：產品 `IssueState=2`，ProductType 精確、ProductNo 前綴，舊 `QC_Count=1`；產品 QC Item/Chart API 需讀 `TBLPRDQCITEM`、`TBLQCITEMFORVARIABLES`、`TBLQCITEMFACTOR` 及 Group 主/明細。只有恰好一個 FactorSpec 且 FactorNo=`PRODUCT` 的變數 QC Item 建 Detail；保留 GroupNo。ChartType/SPCID、X-Rm SPCID 0/5 映射、規格必填/排序和 MEMO 規則見功能規格。
- [ ] `batch-update` 接受 `{productNos,details:[{qcItemNo,groupNo,spcId,us,cs,ls,ucl,cl,lcl}],memo}`，逐產品/QCItem transaction 重查 ACTIVE 與直接/Group 綁定、FactorSpec 和 SampleSize。Basis 依 `(QCItemNo,Product)` 定位；新建才產生 serial，SampleSize 取 QC Item、Product 因子寫 `factorvalues JSONB`（並按 QC302 核對 physical `product` 欄相容性）、IssueState=0；既有 Basis 沿用 serial/state。交易式替換 Detail、追加 DetailLog 界限快照；MEMO 進 audit/log。回傳逐項 updated/skipped/failed 結果，失敗需 rollback。
- [ ] 舊 Core 的產品綁定和 chart 查詢只用 ProductNo，沒有 ProductVersion/OPNo 條件；API 應在規格中保留此相容行為及版本/多 OP 歧義，不要自行切換 key。對同一產品/QCItem 去重，避免不同 GroupNo 對同一 Basis 重複覆寫。
- [ ] 公司端本次未執行 build、測試或瀏覽器驗收；QC305 沒有新增表，沒有額外 CREATE SQL。本 handoff 之外，QC303、QC304 API/schema 工作仍依各自段落追蹤。

## 已完成家用端 handoff：QC303 品管項目規格（2026-11-18）

- [x] YMES live schema 原先缺 Basis/Detail；新增並套用 `0026_qc303_spec_basis.sql`。套用前備份 `/opt/ymes/backups/ymes_before_0026_qc303_20261118.dump` 已通過 `pg_restore --list`；只建 QC303 兩表及 `ymes_app` CRUD 權限，不重建共用 QC Item/Factor/Parameter/Issue/Job/QCSN。
- [x] 完成登入保護 API（QC Item/Factor、List/Detail/Options、CRUD、Approve/Disapprove、Copy2）、FactorSpec 白名單、JSONB/參數化 SQL、交易、重複檢查、DataStamp 與服務端 audit。依使用者指示暫不做角色／FunctionKey ACL。
- [x] .NET 9 Release build 0 warnings/0 errors；正式 API/Web 已部署。正式 menu 路由更新後只重建 `ymes-web`；容器 healthy，API readiness=200、未登入 QC303 API=401、Web route=200。App-browser 驗證三層選單、正式頁、四個狀態篩選與 Close。
- [ ] Live 計量 QC Item 清單為空，新增/修改/刪除/Copy/簽核/Detail 等依賴業務列的流程尚無對象可做端到端驗收。`tblsysmesparameter`、Issue/Job、共用 QCSN 序號服務亦不存在；現階段 serial 使用服務端 35 字元 GUID fallback。待共用資料、參數與簽核資源可用後補驗；詳見 `docs/modules/QC/品管項目規格.md`。

## 下一個公司端 handoff：QC304 物料規格設定（2026-11-17）

- [ ] 讀 `docs/modules/QC/物料規格設定.md`。FunctionNo=`MTLQCITEM`、FunctionKey=`QC304`；Vue DEV 頁 `/YMES/QC/MaterialSpec` 位於品質資料 → 計量規格設定 → 物料規格設定。
- [ ] QC304 沒有專屬新表；先核對 YMES live schema/migrations。Chart Basis/Detail/DetailLog 已在 QC302 migration 0024；物料/QC Item/QC Group 使用既有 tables。Oracle/PG CREATE 參照沿用 `mestest_qc302_spc_qc_chart_source_ddl.sql`、`mestest_qc302_spc_qc_chart_create.sql`、MTL references、`mestest_qc_item_attributes_create.sql`、`mestest_qc212_item_group_create.sql`。不新增重複 schema；若 live schema 真的缺 group detail 或其他依賴，先以其既有 reference 做必要 forward migration。
- [ ] 重用現有 `GET /api/materials/lookups/options` 供 MaterialType lookup。新增/沿用登入保護 QC304 API：`GET /api/qc/material-spec/materials?materialType&materialNoPrefix`、`GET /{materialNo}/qc-items`、`GET /{materialNo}/chart-details`、`POST /api/qc/material-spec/batch-update`；要求 QC304 FunctionKey，路由有變更時同步 Vue/spec。
- [ ] 查詢限 Material IssueState=2，MaterialNo prefix；維持原 `QC_Count=1` 結果條件。物料的計量 QC Item 可由 direct binding/group 展開，但 Detail 只由 FactorSpec 恰為一個且 FactorNo=MATERIAL 的 QC Item 產生；QCItem ChartType=-1 不產 Detail。舊 Save 還會重查 direct `TBLMTLQCITEM`，未 direct 綁定的 group child 會被略過，API 必須明確回報每筆 Material/QCItem 的更新/略過結果。
- [ ] SPCID：ChartType=0→0/1/2，=1→0/1/3，=2→1/4/5；X Chart SPCID=5 在既有 Chart Basis/Detail 存為 0，讀回時轉回 5。CheckBox 僅篩 Grid 顯示，Save 仍送全部明細。US/CS/LS/UCL/CL/LCL 驗數值與 US≥UCL≥CL≥LCL≥LS；SPCID0 按 SpecLineType 驗 US/LS 必填。CS/Tolerance Edit 算 CS±Tolerance；貼上只處理明確使用者資料。
- [ ] `batch-update` 對每個 Material/QCItem transaction 中重查物料、直接綁定、FactorSpec 與現有 Chart Basis。Basis 不存在時以 Chart serial service 新增，SampleSize 取 QC Item、IssueState=0、Material 因子寫入 `factorvalues JSONB`（依現有 schema 同步 physical material 欄若需要）；存在時沿用 serial/IssueState，不重建 Basis。刪除該 serial 的舊 Detail、寫入完整新 Detail 並追加 DetailLog 界限快照。MEMO 必填；舊 Core 對既有 Basis 未保存 Memo/Revision，YMES 應將原因記入操作 audit，不要自動改其 IssueState。
- [ ] 回傳 `{updatedCount,skippedCount,failedCount,results:[{materialNo,qcItemNo,status,message}]}` 給 Vue；逐項顯示直接綁定缺失的略過原因。真正資料庫失敗須在 transaction rollback，不能留下部分 Detail/DetailLog 並回報整批成功。
- [ ] 保持跨多物料結果可見：來源一個更新 helper 接收整個 Material 清單與 Detail 集合；transaction 失敗不可假報整批成功。QC304 沒有新 Oracle DDL 或 CREATE SQL；公司端沿用先前唯讀取得的 reference，本回合未讀業務列、未執行 Oracle DDL，連線資訊未寫入 Git。

## 最新公司端 handoff：QC301 管制圖內容（2026-11-17）

- [x] `git fetch origin` 後 `main` 與 `origin/main` 同為 `d763983`；已讀 `docs/modules/QC/管制圖內容.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`QC CHART BASIS`、FunctionKey=`QC301`；Vue DEV 頁 `/YMES/QC/ChartBasis` 位於品質資料 → 計量規格設定 → 管制圖內容。
- [x] 以家用端 PPK/SSH 查 YMES PostgreSQL live schema/migrations：QC Chart Basis/Detail/DetailLog/Rule 四表已由 0024 建立；QC301 Sampling Parameter 表缺少，已建立 forward migration `0025_qc301_chart_sampling_parameter.sql` 並套用。套用前完整備份 `/opt/ymes/backups/ymes_before_0025_qc301_20261117.dump` 通過 `pg_restore --list`，`ymes_app` 具 SELECT/INSERT/UPDATE/DELETE。QC Item/Factor live table 已存在；`TBLQCITEMRULE`、`TBLQCRULEBASIS`、`TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER`、量測日誌及 Issue/Job 共用表尚未建立，不在本 migration 重建。
- [x] 實作登入保護的 QC301 API 路由與 DTO：`GET /api/qc/chart-bases/qc-items`、清單/Detail/Rule 查詢、basis 新增修改刪除、Approve/Disapprove、Rule replacement、Copy2 及 Sampling Plan lookups。Dynamic FactorNo 使用 FactorOnline 白名單與 JSONB；共用表未建立時回 503，不略過預設規則、抽樣參數或量測紀錄檢查。API 全域要求登入；FunctionKey/角色 ACL 依使用者先前指示暫緩。Rule 編輯回傳新 DataStamp 並更新前端快取。
- [x] DTO/transaction/CAS、FactorOnline 白名單、JSONB duplicate、數值/SpecLineType/SampleSize/ChartType 驗證及伺服器稽核欄位已實作。新增需要 `TBLQCITEMRULE` 與已核准 Rule 表；PlanNo 功能需要抽樣計畫主/參數表。相依表尚未建立時寫入端點會回 503，未寫半套資料。
- [ ] Issue/Job 共用流程尚未有 YMES table/API reference。現況只有確認 Issue/Job table 均不存在時才允許直接 ACTIVE 並記 DetailLog；若部署後出現任一 Issue/Job table，Approve/Disapprove 會 503 停止，須先接上共用簽核服務。尚未建立 durable QC 操作 audit table，現階段 API 記錄至服務 log。
- [x] Delete 狀態規則、DataStamp、同交易刪 chart 子表已實作；硬刪需 `TBLSPCVARDETAILLOG` 查量測使用。該共用表目前缺少，硬刪會 503 停止，不會把「表不存在」當成「沒有使用資料」。
- [x] CopyType 3/0/2、FactorSpec/ChartType 一致性、UNFROZEN 副本及 Basis/Detail/Rule/Parameter 複製已實作。Rule replacement 限 FactorSerial/SPCID、核准 RuleNo、Action 0–3，並更新 Reviser/DataStamp。
- [x] 2026-11-17 .NET 9 Release build 0 warning/0 error；遠端 `ymes-api`、`ymes-web` Docker build 成功並已部署，兩容器 healthy，`/YMES/API/health/ready`=200、QC301 Web route=200、未登入 QC301 API=401。app-browser 已登入檢視 `/YMES/QC/ChartBasis`，頁面載入正常，UNFROZEN/PENDING/ACTIVE/UNUSED 篩選可切換；live QC Item 清單為空，新增/修改/核准等按鈕無可用資料，CRUD 操作無法端到端驗證。API/前端已提交於 `3fd0a03`，merge 公司端 QC303 提交 `e6c5a29` 後以 `eb9a73a` 推送 GitHub `main`；QC303 為下一個家用端 handoff。

## 最新公司端 handoff：QC302 匯入管制圖內容（2026-11-17）

- [x] 以家用端 PPK/SSH 核對 YMES schema；QC302 四張圖表表原先都不存在，QC Item Variables/Factor 已存在。Rule、Sampling Plan 共用資料表也尚未複製，沒有在 QC302 migration 中重建共用表。
- [x] 新增並套用 `database/migrations/0024_qc302_chart_data_import.sql`；套用前建立並以 `pg_restore --list` 驗證 `/opt/ymes/backups/ymes_before_0024_qc302_20261117.dump`。核對四張表共 68 欄及 `ymes_app` CRUD 權限；migration 只建結構，未新增業務資料。
- [x] 完成登入保護 API：`POST /api/qc/chart-data-import/preview`（`.xls/.xlsx` 第一工作表、第 1 列標頭、第 2 列跳過、第 3 列起、空白列終止）、`POST /import`（全列匯入、無 Check）及 `GET /template`。因角色/功能權限依使用者決定暫緩，API 目前要求登入，不做 FunctionKey ACL。
- [x] Preview 回 `{columns,rows}`；Import 回列級 `rowNo/qcItemNo/succeeded/execute/factorSerial`，每列獨立交易。已實作核准 QC Item/線上 Factor 白名單、JSONB、SpecLineType/TUS/TLS、數值/排序/Y 軸、RuleNo/Action 核准檢查、重複組合、basis、五種 detail、detail history、rule 複製/替換；Creator/CreateDate/FactorSerial 由伺服器管理，所有資料欄參數化。
- [ ] 可匯入 QC302 的基本規則仍受 YMES 尚缺共用 `TBLQCITEMRULE`、`TBLQCRULEBASIS` 阻擋；新管制圖必須複製預設規則，API 會對該列回 503，不會略過。若輸入 PlanNo，因 `TBLQCSAMPLINGPLAN` 尚未複製也會回 503。待相關共用 QC schema 任務另行建立後再驗證匯入。
- [ ] `GET /template` 目前以 503 明確回報尚未移轉 `FunctionNo=IMPORT QC CHART DATA` 的正式範本；未用 DEV 假檔冒充。待取得／移轉 MES 範本儲存區與 `QCChart_Template.xls/.xlsx` 後接上下載。
- [x] 本機 `dotnet build src/YMes.Api/YMes.Api.csproj --no-restore` 成功（0 warning/0 error）；遠端 Docker API image build 成功，`ymes-api` healthy、`/YMES/API/health/ready`=200；未登入呼叫 QC302 端點回 401。app-browser 的認證狀態已失效並被導回登入頁，本回合未操作登入畫面。
- [x] 已 `git fetch origin`；`origin/main` 最新為 `0b0f434`，已包含於家用端，沒有更新 commit。將只提交本 QC302 API/schema/docs 與 QC302 待辦段落，然後以一般 fast-forward push 同步 `main`，不 force push。Oracle source DDL 由公司端唯讀取得，沒有讀業務列或執行 Oracle DDL，連線 IP/帳密/SID 未輸出。

## 前一項公司端 handoff：QC215 計數品管項目匯入（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計數品管項目匯入.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`IMPORT QCITEM ATTRIBUTES`、FunctionKey=`QC215`；Vue DEV 頁 `/YMES/QC/AttributeItemImport` 已掛在品質資料 → 品管設定。
- [ ] QC215 匯入既有計數品管資料，沒有 QC215 專用表，也沒有新的 CREATE SQL。先核對 YMES PostgreSQL live schema/migrations；主檔沿用 `mestest_mtl_qc_item_tables_source_ddl.sql` / `mestest_mtl_qc_item_tables_create.sql`，Parameter/Factor/ReasonMap 等子表參考 `mestest_qc_item_attributes_source_ddl.sql` / `mestest_qc_item_attributes_create.sql`，群組與依賴表參考 QC205/QC213 既有 source/CREATE。不得複製重建共用表；只為 live schema 真正缺少的 YMES 結構新增 forward migration。
- [ ] 實作登入保護並要求 FunctionKey QC215 的 API：`POST /api/qc/attribute-item-import/preview`（multipart `.xls/.xlsx` 第一工作表；保留 WinForms 第 1 列欄名、第 2 列跳過、第 3 列起讀取）、`POST /check`（唯讀驗證）、`POST /import`（Save）及 `GET /template`。Production Vue 目前依此路徑呼叫；如配合既有 controller 改路徑，請同步改 Vue/spec。
- [ ] Preview 回 `{rows:[{recognizedColumns...}]}`；Check/Import 每列回 `{rows:[{rowNo,qcItemNo,succeeded,result,execute}]}`。只處理前端勾選列，逐列獨立交易並回列級結果；缺少任何列結果要視為失敗，不能因 HTTP 200 或空訊息標成功。
- [ ] 依 QC215 規格驗證 QCItemNo/Name、量測記錄引用、LotIsVaild、PlanNo/ComponentCollect、核准 RuleNo、正整數 LimitLotNum、MClass、FactorNo/FactorOnline、核准 OP/Inline/Offline、檢驗旗標與核准 Reason 清單；修正 legacy 重複 Reason 檢查失效問題。FactorNo 只能是核准 Parameter 白名單，絕不可由上傳值構造 SQL 欄名或觸發任意 `ALTER TABLE`。
- [ ] Save 覆寫前在該列交易中重查 WIP、SPC Attribute Log、Material/MaterialType/OP/Product、`TBLQCITEMATTGORDERDETAIL` 及一般 QC Item Group 綁定。Legacy 會刪除既有主/子資料且未先保護一般 Group Detail；YMES 必須在群組仍有引用時拒絕覆寫，不可靜默解除綁定。完整清除/重建範圍及欄位預設值依 QC215 規格實作。
- [ ] Creator/CreateDate 必須由登入身份與伺服器時間產生，不信任檔案內容；限制上傳檔案大小/列數/欄數，不執行公式或使用者提供的路徑。範本沿用 FunctionNo=`IMPORT QCITEM ATTRIBUTES` 的既有 template storage。
- [ ] 公司端沿用已保存 Oracle DDL reference，本次沒有再查 Oracle、沒有讀業務列、不需要索取 IP/帳密/SID，也沒有新 CREATE SQL。家用端完成 schema/API/權限/template 後 commit/push；公司端再 pull 對接。公司端本段未執行 build、測試或瀏覽器驗收。

## 前一項公司端 handoff：QC214 計量品管項目匯入（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計量品管項目匯入.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`IMPORT QCITEM VARIABLES`、FunctionKey=`QC214`；Vue DEV 頁 `/YMES/QC/VariableItemImport` 已掛在品質資料 → 品管設定。
- [ ] QC214 是匯入既有計量品管資料的功能，沒有專屬新資料表、沒有 QC214 專用 CREATE SQL。先查 YMES PostgreSQL live schema/migrations；主表沿用 `database/reference/mestest_mtl_qc_item_tables_source_ddl.sql` / `mestest_mtl_qc_item_tables_create.sql`，依賴表參考 `mestest_qc205_variable_dependencies_source_ddl.sql`、`mestest_qc210_variable_measurement_log_delete_create.sql`、`mestest_qc_item_attributes_create.sql` 及 QC205/QC201 的共用 CREATE SQL。只為實際缺少結構新增 forward migration，不複製重建 QC205 共用表。
- [ ] 實作登入保護並要求 FunctionKey QC214 的 API：`POST /api/qc/variable-item-import/preview`（multipart `.xls/.xlsx` 第一工作表；相容 WinForms 第 1 列欄名、第 2 列範例跳過、第 3 列起資料）、`POST /check`（OnlyCheck、不寫入）、`POST /import`（Save）及 `GET /template`。Production Vue 現在依此契約呼叫；如採既有 controller 路徑，需同步修改 Vue/spec。
- [ ] `/preview` 回 `{rows:[{recognizedColumns...}]}`；Check/Import 每列回 `{rows:[{rowNo,qcItemNo,succeeded,result,execute}]}`。前端只送所勾選且未鎖定資料列；結果需能依 `rowNo` 對回。以每列獨立交易、逐列回報，允許部分成功；不要只看 legacy Service 的整體 success。
- [ ] 依 QC214 規格實作驗證：QCItemNo/Name、LotIsVaild、PlanNo、SampleSize、SpecLineType、ComponentCollect、CalType、ChartType/X-Rm 關係、Min/Max、LimitLotNum、MClass、Roundoff_Num、已核准 Class 1 Factor 集合及 FactorOnline/FactorSpec 子集合、核准 OP/Inline/Offline 與 FAI/IPQC/PQC。舊 Core 的 ComponentCollect/LotIsVaild 限制已註解；不要自行阻擋。
- [ ] Save 覆寫既有 QCItem 前交易內重查：無 Spec/Chart/量測資料、無 WIP 使用，無 Material/MaterialType/OP/Product/一般 Group/PMI Group 綁定。舊 Core 會清除主檔與多張子表再重建；列出的清除範圍要用 YMES 正規化關聯表和參數化 SQL 重現。Factor 欄不准由客戶端輸入觸發 `ALTER TABLE`；只有白名單核准 Factor 可套用，動態量測資料沿用 YMES 結構化儲存設計。確認 transaction 可回滾所有主/子資料寫入。
- [ ] Creator/CreateDate 雖是舊匯入欄位，API 必須忽略檔案提供的稽核資料，改用登入身份與伺服器時間。限制工作簿大小/列數/欄數，不執行公式、不採信用戶端檔案路徑；範本沿用 FunctionNo=`IMPORT QCITEM VARIABLES` 的既有 template storage。
- [ ] Oracle DDL references 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得，本次沿用 QC205/QC210/QC201 結構參照，不需要再問公司端索取 IP/帳密/SID，也沒有新增 Oracle 查詢或業務列讀取。後續 QC302 直接查得 MESTEST chart 主表為 `TBLSPCQCCHARTBASIS`；比對 YMES live schema 時依正式 PostgreSQL/API 物件名稱處理，不可因 Oracle 有表就推定 YMES 已建立。
- [ ] 家用端完成 schema 比對、API、權限與 template 後 commit/push；公司端 pull 對接。公司端本段未執行 build、測試或瀏覽器驗收。

## 前一項公司端 handoff：QC218 計數品管項目產品原因對應設定（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計數品管項目產品原因對應設定.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`QCITEM PRD REASON MAP`、FunctionKey=`QC218`；Vue DEV 頁 `/YMES/QC/ItemProductReasonMap` 已完成並掛到品質資料 → 品管設定。
- [ ] 用 YMES PostgreSQL 管理連線查 live schema / migrations。Oracle 原始 DDL：`database/reference/mestest_qc218_tblqcitemprdreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemreasonmap_source_ddl.sql`；兩張 mapping 表的空結構參考：`database/reference/mestest_qc218_item_product_reason_map_create.sql`。只比對/建立缺少的 mapping 結構；`TBLQCITEMFORATTRIBUTES`、`TBLQCREASONBASIS`、`TBLPRDPRODUCTBASIS` 是共用表，另外三份 Oracle DDL 只供欄位核對，禁止重建或覆寫。
- [ ] 建立登入保護並要求 QC218 FunctionKey 的 API：`GET /api/qc/item-product-reasons/qc-items?issueState=2&qcItemNo=`、`GET /api/qc/item-product-reasons?qcItemNo=&allData=true`、`GET /{qcItemNo}/products`（distinct 已對應 ProductNo）、`GET /reasons?issueState=2`、`GET /products?issueState=2&currentVersion=1&search=`、`GET /{qcItemNo}/{productNo}/reasons`、`PUT /{qcItemNo}/{productNo}`、`POST /import/preview`、`POST /import`、`GET /template`；細節依 QC218 規格，路徑若需配合既有 API 慣例請同步更新文件和 Vue。
- [ ] Main QC Item 只讀已核准計數項目，QCItemNo `%keyword%` 模糊查詢；全量映射需 Join `TBLQCREASONBASIS` 回 ReasonName/Type/Level，再按 ProductNo 分組。ProductNo 查詢空白=全部、`*`=只看共用、其他值=模糊篩選；產品 `...` 查該 QC Item 已有映射的 distinct ProductNo。
- [ ] ReasonType 0=Scrap、1=Defect；候選原因只取 IssueState=2，並依 ReasonType/ReasonLevel 篩選。Product 必須 IssueState=2 且 CurVersion=1，只有字面值 `*` 免查產品並代表全域映射。來源 WinForms 編輯器似乎把 `*` 當產品代碼驗證而可能無法儲存；API/UI 必須修正此 defect，將 `*` 寫 `TBLQCITEMREASONMAP`，其他產品寫 `TBLQCITEMPRDREASONMAP`。
- [ ] `PUT` 以 `{ reasonNos: string[] }` 原子整組取代目標 QCItemNo/ProductNo；空陣列清空。先重驗核准 QC Item/Reason/產品目前版本及授權；不得跨產品寫入。對原有關聯保留 Creator/CreateDate，新增關聯以登入身份/伺服器時間建立，Reviser/ReviseDate 由 API 產生。全程參數化並正確處理 unique key race。
- [ ] Import `.xlsx/.xls` 只讀第一工作表，欄位順序 QCItemNo、ProductNo、ReasonNo，需驗證每列必填；必須包含最後一列（舊程式有 off-by-one bug，勿照抄）。Preview 後逐列獨立驗證/交易/回報結果，允許部分成功與取消。拒絕重複三欄鍵、已存在映射、未核准 QC Item/Reason、非核准目前版本產品；`*` 不查產品。不要接受上傳檔或前端的 Creator。
- [ ] 範本沿用 FunctionNo=`QCITEM PRD REASON MAP` 的既有 template storage。新增 migration/API/權限/範本後 commit/push；公司端 pull 再接 Production API。公司端尚未執行 build、測試或瀏覽器驗收。

## 最新公司端 handoff：QC217 設備品檢管理設定（2026-11-17）

- [x] Pull 最新 `main` 至 `19b8e11`，讀完 `docs/modules/QC/設備品檢管理設定.md`、`docs/DATABASE_CONNECTION.md` 與本節；FunctionNo=`QC INSPECTION`、FunctionKey=`QC217`。公司端 Vue 頁已掛 `/YMES/QC/Inspection`，EQP `/YMES/Equipment/Inspection` 維持獨立。
- [x] 以家用端 PPK/SSH 查 YMES live schema：兩張 QC217 表原先都不存在；設備、作業站、設備狀態、計數/計量項目與計量群組主檔已存在。原始 DDL 與 CREATE 參考已比對，沒有覆蓋既有表。
- [x] 新增及套用 `database/migrations/0023_qc217_equipment_inspection.sql`。套用前的 `/opt/ymes/backups/ymes_before_0023_qc217_20261117.dump` 已通過 `pg_restore --list`；套用後核對兩張表欄位、複合 PK 與 `ymes_app` CRUD grants。
- [x] 建立登入保護 API：`GET /api/qc/inspections`、`GET /options`、`GET /{opNo}/{equipmentNo}/{inspType}/details`、POST、PUT、DELETE 及 `/approval`。主檔/共用明細原子交易、查找重驗、參數化 SQL、DataStamp CAS 及 audit 已實作；API 使用目前產品決策的登入保護，FunctionKey ACL 尚未建立。
- [x] API 已部署至 Production `/YMES/API`，容器 healthy，`/health/ready` 回 200；Web 已重新建置並部署至 `/YMES`。app-browser 經正式三層選單可開啟 QC217，Options 載入、空清單查詢和新增表單開關正常。現場 options 缺 MES ValueList、通知群組及 QCItemType=3 群組目錄，API 以 `missingReferences` 回報、不使用假資料；目前資料庫也沒有 QC217 主檔/可用 QC Item 資料。
- [x] Options 回傳已核准 OP、設備與設備類型、設備狀態及目前三種已存在 QCItemType 的可選項目/ LotIsValid；`missingReferences` 列出尚未複製的 ValueList、通知群組與 QCItemType=3 主檔。OP `*` 表示全部作業站。Server 重新驗證已存在的設備/OP/狀態及 QC Item，禁止未核准或 LotIsValid=0 的項目；所有 SQL 參數化。
- [x] Add/Edit/Delete/Approval API 已實作並部署：主檔/明細原子交易、狀態限制、共用明細鍵、DataStamp CAS、登入身份與伺服器稽核；API 以正確完整複合鍵更新，不沿用舊 callback 的 InspWeek 條件錯誤。未在正式資料庫新增測試業務資料；寫入路徑尚未透過瀏覽器實際送出。
- [x] Delete 規則依來源實作：狀態 0/-1 硬刪、2 改 -1、1 拒絕；硬刪依 legacy 共用鍵清除明細，UI 操作需留意同 OP/設備/檢查類型其他排程共用的細項。
- [x] 核准 API 已依 live YMES 結構處理：沒有 Issue Job/Detail 表時沿用 legacy 直接 0→2；若簽核結構存在但共用簽核尚未接入，回 503 且不改狀態。
- [ ] commit/push 本次 QC217 API、migration、Web 路由修正與必要 Vue build 修正後，公司端 pull 並以 Production API 對接。公司端 UI 實際 CRUD 瀏覽器驗收待資料來源補齊。

## 最新公司端 handoff：QC216 計量品管項目群組_減項（2026-11-17）

- [ ] 先讀 `docs/modules/QC/計量品管項目群組_減項.md`、`docs/DATABASE_CONNECTION.md` 與本節；FunctionNo=`QCITEM_G_PMI`、FunctionKey=`QC216`。公司端 Vue 頁已掛在 `/YMES/QC/PmiItemGroup`，Production API 尚未建立。
- [ ] 用家用端 PostgreSQL 管理連線比對 YMES live schema、migration 與現有 QC API。 Oracle source DDL 在 `database/reference/mestest_qc216_*_source_ddl.sql`，空結構 CREATE 參考在 `database/reference/mestest_qc216_variable_item_group_pmi_create.sql`。只對實際缺漏新增 forward migration；三張 SPC map、TBLWIPTEMP_QCITEM 與共享 QC/OP/MTL/PRD 表先確認已存在及欄位/索引，不可重建或覆寫。
- [ ] 新增 `QC216` 登入授權 API：`GET/POST /api/qc/pmi-item-groups`、`GET/PUT/DELETE /{groupNo}`、`POST /{groupNo}/approval`、`GET/PUT /{groupNo}/details`、`GET/POST /{groupNo}/deductions`、`PUT/DELETE /{groupNo}/deductions/{serial}`、`GET /options/factors`。DTO 和資料限制依 QC216 規格；Audit 由伺服器登入身份產生。
- [ ] 編輯需重現共用 `ExeIssueControl`：IssueState=0 欄位可編輯；1 唯讀；2/-1 依 `wsSYS.LoadIssueControlForm` 針對 `frmPMI_QCItemGroupDef` 的設定逐欄授權，缺少設定時全唯讀。Vue 讀 `GET /{groupNo}/edit-policy`，PUT 端仍在伺服器重驗欄位。
- [ ] 群組新增後 IssueState=0。Add/Edit 檢查 GroupNo 與 Attribute/Variable QC Item、一般 QC 群組及 PMI 群組互斥（來源大小寫無關）；Define UI 的 ComponentCollect、FormName、WebURL、Error/NoticeReportName、QCClass、CursorMode 等隱藏欄位在修改時必須保留。PlanNo 只可選同 ComponentCollect 且 IssueState=2 的計畫。
- [ ] 明細完整替換必須交易處理：只接受已核准計量 QC Item、ComponentCollect 相同、QCItemNo 不重複；保存 ShowOrder、DeductionItem，至少保留一筆非減項。核准前每個目標群組均須至少有一筆 detail。簽核 Issue Job=`QCITEM_G_PMI`；無 Job 直接到 2，有 Job 先到 1，核准 2、退回 0。
- [ ] 刪除依舊系統規則：IssueState=0/-1 硬刪、2 改 -1、1 拒絕；先查 TBLWIPTEMP_QCITEM 在線引用，再同 transaction 清 PMI 主/明細及 Core 實際清理的共用 QC Item Parameter、OP/MTL/PRD/SPC 關聯；只有任何一般/PMI 群組都不再引用時才清孤兒 Calculate Script。WinForms Core 沒有刪 `TBLQCGROUPDEDUCTIONBASIS_PMI`；為維持相容，API 預設保留這些 orphan rows，規格記為 legacy 缺陷。核對各關聯表實際 QCType 與鍵後實作，逐筆回報批次結果。
- [ ] `TBLQCGROUPDEDUCTIONBASIS_PMI` 的 Oracle 動態要因欄位在 PostgreSQL 用 `factorvalues JSONB`；只允許 API 依啟用的 FactorOnline 白名單收鍵，不可把用戶輸入串成 SQL 欄位。保留 `*` 萬用條件，factor value 依 Oracle VARCHAR2(61) 限制；NormalTimes/DeductionTimes 1–99、CurPass 0–999、CurStatus 0/1/2、重複因子組合檢查與 DataStamp CAS 均由 API 執行。序號沿用 YMES serial 服務。
- [ ] 共用子功能 Parameter 使用 QCItemType=4，PlanNo 有值時傳遞已核准 PlanType；Op 使用 QCType=4，GET 已核准作業站候選清單及該群組目前的關聯/FAI-IPQC-PQC 設定，PUT 以 transaction 完整替換。只允許已核准群組修改；LotIsValid=0 禁止 InLine；每筆 InLine 至少要有一種 FAI/IPQC/PQC。共享 API 仍需要求 QC216 授權。
- [ ] migration、API、選項及 API 文件完成後 commit/push；公司端再 pull 並對接。公司端沒有連 YMES PostgreSQL，也沒有執行 build、test 或瀏覽器驗收。

## 最新公司端 handoff：QC213 計數品管項目群組（2026-11-17）

- [ ] QC212 是本功能前置工作；先完成下方 QC212 handoff 的 live schema 比對與 API，再接 QC213，避免 QC 主檔 API 路徑重複或漏掉共用資源。
- [ ] 先讀 docs/modules/QC/計數品管項目群組.md、docs/DATABASE_CONNECTION.md、docs/MES_MENU_FUNCTION_INVENTORY.md；FunctionNo=QCITEM_G_A_ORDER，FunctionKey=QC213。
- [ ] Oracle source DDL 已由公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得，無須重查：QC213 主/明細及 OP/PRD 硬刪依賴表各有 reference；WIP 檢查表在 QC205 dependency reference。PostgreSQL CREATE-only 參考 `database/reference/mestest_qc213_attribute_item_group_create.sql` 列出這些表及必要索引。
- [ ] 查 YMES PostgreSQL live schema、migrations、API 現況。僅在缺表時新增 forward migration；主檔與明細 PK 分別是 tblqcitemattgrouporderbasis(groupno)、tblqcitemattgorderdetail(groupno,qcitemno)。逐一比較 CREATE 參考中的硬刪依賴表及既有 QC 共用表欄位/PK/index，不可覆寫現有資料，也不可把 PostgreSQL SQL 放到 MES Oracle 執行。
- [ ] 實作登入保護及 QC213 FunctionKey 授權 API：GET/POST/PUT/DELETE /api/qc/attribute-item-groups、GET/PUT /{groupNo}/details、POST /{groupNo}/approval。詳細 DTO、欄位限制、交易及錯誤處理依 QC213 功能規格。
- [ ] 主檔欄位限制 GroupNo 25、GroupName 50、Description 255；LotIsValid 僅 0/1。Add/Edit 應避免 GroupNo 與 Attribute QC Item、Variable QC Item、計量群組 TBLQCITEMGROUPBASIS、PMI 群組 TBLQCITEMGROUPBASIS_PMI 重複（來源 Core 以大小寫無關比對）。
- [ ] 伺服器依實際 IssueState/DataStamp 決定操作：新增狀態 0；編輯僅 0；刪除 0/-1 硬刪、2 改 -1、1 拒絕。硬刪先檢查 TBLWIPTEMP_QCITEM 是否在線使用；來源 Core 另清除 QC Item Parameter、OPQCItem、OPGQCItemDetail、PRDQCItem 關聯。單一 transaction 執行，並核對 YMES 現有表的 QCType 條件與刪除順序。
- [ ] 核准使用 Issue Job ATTQCITEM_G_ORDER；有 Job 時設 1 並啟動簽核，核准後 2、退回回 0；沒有 Job 直接設 2。API 每筆都確認群組至少有一筆有效 Attribute 明細，批次逐筆回報。
- [ ] 明細 API lookup 沿用已核准 Attribute QC Item：GET /api/qc/attribute-qc-items?issueState=2。Add/Remove/reorder 寫入以單一 transaction 完整替換 details，檢查群組未簽核、QC item IssueState=2、Attribute 類型、唯一性；ShowOrder 連續 0..N-1。沿用共用作業站資源 QCType=3，LotIsValid=0 不可指定 Inline，InLine 至少勾選 FAI/IPQC/PQC 一種；不要建 QC213 專屬 operation table。
- [ ] 更新 migration/API 文件與 todolist_homepc.md，commit/push 後通知公司端 pull。公司端尚未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收。

## QC212 前置 handoff：計量品管項目群組（2026-11-17）

- [ ] 讀取 `docs/modules/QC/計量品管項目群組.md`、`docs/DATABASE_CONNECTION.md` 與下方 QC212 handoff；選單 FunctionNo=`QCITEM_G`、API 權限 FunctionKey=`QC212`。
- [ ] 使用家用端可用的 PostgreSQL 管理連線先查 live schema、migration 及 API 現況。核對 `tblqcitemgroupbasis`、`tblqcitemgroupdetail`、`tblqccalculatescript`、`xx_tblqcitem_memo`、`tblqcitemparameter`、`tblopqcitem`、QC Variable item、sampling plan、MES parameter。Oracle source DDL 已在 reference，不需要公司端重查 Oracle。
- [ ] 若 live YMES 缺少群組明細或腳本表，參考 `database/reference/mestest_qc212_item_group_create.sql` 設計 forward migration；群組主檔既有參考為 `mestest_mtl_qc_item_tables_create.sql`，Memo 為 `mestest_qc205_theil_custom_create.sql`。先比對既有 schema，不可覆蓋/重建既有資料，不可把 PostgreSQL SQL 放到 Oracle 執行。
- [ ] 實作登入保護且使用 QC212 FunctionKey 的主檔、Detail、Copy、Approve、Delete API。規格詳 `docs/modules/QC/計量品管項目群組.md`；包含完整替換明細/ShowOrder、ScriptIsValid、計算腳本、共用 Parameter/OP、Memo Type=1、抽樣計畫/WinSPC/QCClass 查找。
- [ ] 狀態規則：刪除 0/-1 硬刪、2 轉 -1、1 禁刪；硬刪檢查 WIP 引用及 legacy 關聯清理。核准使用 QCITEM_G Issue Job，無 Job 直接至 2、有 Job 至 1。Detail Add/Remove 僅狀態 0；OP 編輯拒絕狀態 0/1。API 以真實 DB 狀態與 DataStamp 驗證，使用參數化 SQL/transaction，批次作業逐筆回報。
- [ ] 明確決定群組硬刪是否一併清除 `XX_TBLQCITEM_MEMO`（Legacy Core 未刪該表），並記錄相容性決策；確認 Copy 是否只複製 detail（Legacy Core 沒有複製群組 Script 的程式）。
- [ ] 完成 migration/API 文件、更新 `todolist_homepc.md`，commit/push 後通知公司端 pull Production API 驗收。公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。

## 前一公司端 handoff：QC211 二次檢驗抽樣計畫（2026-11-17）

- [ ] 讀取 docs/modules/QC/二次檢驗抽樣計畫.md、docs/DATABASE_CONNECTION.md 與 QC211 公司端紀錄；FunctionNo 為 SAMPLINGFOR2NDINSP，FunctionKey 為 QC211。
- [ ] 比對 YMES live PostgreSQL schema 與 migrations。主表來源為 TBLQCSAMPLINGFOR2NDINSP；Oracle source DDL 與 PostgreSQL schema-only Create 參考分別在 database/reference/mestest_qc211_second_inspection_sampling_source_ddl.sql、mestest_qc211_second_inspection_sampling_create.sql。Oracle 沒有 PK constraint，只有唯一索引 U_TBLQCSAMPLINGFOR2NDINSP_01，鍵序為 QCITEMNO、QCITEMTYPE、OPNO、OLDSAMPLEPLAN；沒有 FK。只為確認缺少的結構新增 forward migration，不覆寫 YMES 資料。
- [ ] 實作登入保護且使用 QC211 FunctionKey 的 list、Add、Edit、Delete API。Update/Delete 的資源鍵為 (qcItemType, qcItemNo, opNo, oldSamplePlan)；新增時檢查唯一鍵及 OP/QC Item/抽樣計畫關聯。Update 只允許變更 NewSamplePlan，使用交易與 DataStamp concurrency，並正確更新 Reviser/ReviseDate。
- [ ] 提供作業站、IssueState=2 的計量型 QC Item（依 OP 過濾並回傳 ComponentCollect）、一般抽樣計畫、Wafer 抽樣計畫 lookup；沿用 YMES 既有 OP/QC/Plan 資源，不建立重複 lookup table。
- [ ] 核對舊來源不一致：wsQC Edit 呼叫帶 DataStamp/Reviser/ReviseDate，但 clsQCBasis Edit 簽章只接受五個業務欄位且只更新 NewSamplePlan。API 需採明確的 YMES 稽核/並行更新規則並回填規格。
- [ ] 完成 schema/API 文件與 migration 後 commit/push；公司端再 pull 並對接驗收。公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

> **本次接手身分：家用電腦。** MES 原生來源是公司端唯讀查詢的 Oracle MESTEST；本端要復刻的是 YMES PostgreSQL，透過桌面 PPK/SSH 核對、建表、migration、開發 API 與部署。`database/reference/*_create.sql` 是由原生 MES DDL 轉換的 PostgreSQL 建表結構，只能用於 YMES 復刻資料庫，不在 Oracle 執行，也不含 MES 業務資料。公司電腦只做 UI 並呼叫 `/YMES/API`，不能連 YMES PostgreSQL、SSH 或上版。不要因 API 應用帳號權限不足而把工作角色改成交由公司電腦；由家用端 SSH 使用資料庫管理權限完成。

## 家用電腦現在接手

### QC201 計數品管項目（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/計數品管項目.md`、公司端 QC201 區段及 `docs/DATABASE_CONNECTION.md`；確認 Vue route/menu 使用 FunctionNo `QCITEM_A`、API 權限使用 FunctionKey `QC201`。
- [ ] 比對 YMES live PostgreSQL schema 與 migrations：主表 `tblqcitemforattributes` 已列於 `mestest_mtl_qc_item_tables_create.sql`；四張直接子表 `tblqcitemparameter`、`tblqcitemfactor`、`tblqcitemreasonmap`、`tblqcsampleplanchange` 參考 `mestest_qc_item_attributes_create.sql`。只對實際缺少的表/欄位/constraint 建立 forward migration；保留 YMES 擴充欄位與資料，禁止 drop/recreate 或從 Oracle 複製 MES 業務資料。
- [ ] `mestest_qc_item_attributes_source_ddl.sql` 含 18 張 Oracle 結構物件，包含舊刪除流程參照表；先把主表與 4 張直接子表以外的物件逐一對照 YMES 既有功能/表，不要將 raw DDL 全部批次建立。這四張子表各自只有列出的 PK，沒有 Oracle FK/非 PK index；SamplePlanChange 的 `LOTRANGE`、`AQLCHKFORSPLANCHG` 以 CHECK NOT NULL 實作，請保留語義。
- [ ] 實作 FunctionKey QC201 保護的 API，契約詳見規格：主清單 GET、主檔 POST/PUT/DELETE、Copy POST、Approve POST；options、已核准 Reason lookup、ReasonType ValueList、Parameter/Factor/Reason/Operations/SamplePlanChange GET/PUT、Operation choices GET 及 Simulation POST。DTO 欄位大小寫/日期/nullable 請和 Vue `QcAttributeQcItemMaster.vue` 對齊。
- [ ] API 必須重做 WinForms/Core 驗證：Add/Edit/Copy 的欄位限制、Issue Control、DataStamp concurrency、複製關聯交易、Approve 僅未簽核且至少有 Factor/Reason、Delete 的 0/-1 實刪/2→-1/1 拒絕及 WIP/MTL/MTL Type/OP/PRD/QC Group/SPC 引用檢查。刪除與子表清理必須交易化，按多選逐筆處理時保留前端首錯停止的結果語義。
- [ ] Options 從 YMES 真實抽樣計劃、計數判別法則、主分類與介面目錄取得；`showQcClass` 對 ZKT/KYT 開啟，`showLotValid`/`showComponentCollect` 依 SPK 模式設定。ReasonType 由 MES ValueList 的實際有效值及顯示字串提供，不能照搬 DEV 假資料。
- [ ] `QC_ItemOp` Vue 左右雙清單已完成；接 API `GET /api/qc/attribute-qc-items/{qcItemNo}/operations`、`GET /api/qc/attribute-qc-items/options/operations?qcType=0&lotIsValid=...`、`PUT /api/qc/attribute-qc-items/{qcItemNo}/operations`。保存採完整替換，保留 OP/Inline/ExecuteOrder/檢驗時機/Reviser/ReviseDate；僅 IssueState=2 可編輯、LotIsValid=0 不可加 InLine、每筆 InLine 必須至少勾一個 FAI/IPQC/PQC。沿用 YMES 已有的作業站及 `TBLOPQCITEM` schema，不另建重複表。
- [ ] 完成後更新 migration/API 文件及本待辦，提交並 push；公司端 pull 後對照 QC201 Production 頁面及回應契約。

### QC112 品質原因產品指導書（公司端 2026-11-17 交接）

- [x] 2026-11-17 家用端 live audit：QC112 的 `tblsysfunction.functionkey` 與品質資料選單項目已存在；`tblprdproductbasis` 已存在，但來源 `tblqcreasonbasis` 與關聯 `tblqcreasonprdsopfile` 缺表。目錄中的五個 `FILESERVICE*`/`REASONPRDSOPFILEPATH` 參數均不存在於 YMES `tblsyselementbasis`；API 沒有 file-service code，容器只有 Data Protection volume，主機也未掛載 NFS/CIFS。使用者功能授權表未在目前 YMES schema 內盤點到。
- [x] 閱讀 QC112 規格、公司端 handoff 與資料庫連線紀錄；核對 `/YMES/QC/ReasonProductSOPFile` route、選單及 API 契約。遠端已存在 QC112 Function/Menu 項目，不需另建。
- [x] 檢查 YMES 使用者功能授權資料表；目前 schema 沒有 user-function 對照。本專案依使用者決策「角色/權限先不管」，使用既有登入保護，不新增角色模型，也不宣稱有 QC112 專屬 ACL。
- [x] 套用 `0020_qc112_reason_product_sop_file.sql`：僅建立缺少的 `tblqcreasonbasis`、`tblqcreasonprdsopfile`，沒有匯入 Oracle 業務資料、FK 或額外索引；`ymes_app` 僅獲原因 SELECT 與關聯表 SELECT/INSERT/DELETE。套用前已建立並驗證遠端備份 `ymes_before_0020_qc112_20261117.dump`。
- [x] 完成登入保護的 QC112 API：原因僅回 IssueState=2；產品僅回 IssueState=2/CurVersion=1，ProductNo 包含式搜尋、`*` 查全部；新增驗證原因/產品狀態與複合鍵；移除時檢查共用檔案引用並以 pending move 在 DB rollback 時補償。
- [x] 新增 YMES API 專用 Docker named volume `ymes-qc112-files`，容器掛載 `/data/qc112`；資料庫只存 `QC112/yyyy/MM/{guid}.{ext}` 邏輯路徑。上限 1 GiB、單檔 25 MiB，允許 JPG/JPEG/JPE/JFIF/PNG/BMP/PDF，並驗證內容簽章。未找到既有檔案服務或 NFS/CIFS，這是 YMES 新增的 API server-side 儲存方式。
- [x] 匯入支援第一張 XLS/XLSX/CSV 工作表、最多 1000 筆；產生四欄 XLSX 範本。`FilePath` 僅作顯示文字，API 不讀取該路徑；附件由使用者另外選取後依 FileName 上傳。
- [ ] 公司來源樹缺少舊 `QCItemRPDReasonSOPFileImporter`，因此無法確認其中未記錄的額外 business checker 條件；已實作可核實的核准狀態、產品版本、重複鍵及共用檔案刪除保護。若客戶驗收發現額外舊規則，再由公司端提供具體規則補齊。
- [x] `5a0109b` 已 push 到共用 `main` 並部署 YMES API；遠端 `/health/ready` 回 ready、未登入 QC112 endpoint 回 401，專用 volume `ymes-qc112-files` 已建立。只重建 `ymes-api`，`ymes-web` 與其他容器未重啟。
- [ ] 公司端 pull 最新 main 後驗收 Production route 與匯入/附件操作；舊 checker 額外規則仍待公司端提供具體依據。

### QC110 特殊主檔維護（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/特殊主檔維護.md`、公司端 QC110 區段與 `docs/DATABASE_CONNECTION.md`，pull 後核對 Vue 頁面、選單及 API 契約。
- [ ] 核對 YMES 的 `TBLSYSMODULE`、`TBLSYSMENUITEM`、`TBLSYSFUNCTION` 及使用者功能授權是否已有 `SPECIALBASISMAINTAIN`（QC110）與「品質資料 → 基本資料」階層；僅補缺少的 YMES menu/function mapping，讓正式 API 菜單能顯示此功能。
- [ ] QC110 沿用 QC109 已交付的 `database/reference/mestest_qc_factor_maintain_source_ddl.sql` 與 `database/reference/mestest_qc_factor_maintain_create.sql`；先比對 YMES live schema/migrations 的 `tblsysmesparameter` 與 `fc_*` 表，只對實際缺少的 schema 增加 forward migration。不要重複建表、清除既有資料、匯入 Oracle 業務列或在 Oracle 執行 CREATE。
- [ ] 實作 FunctionKey `QC110` 保護的獨立 `GET /api/qc/special-basis-maintain/parameters`，只回傳 `IssueState=2`、`ParameterType=0`、`ParameterClass=0` 且 `TableName LIKE 'FC_%'` 的參數目錄；勿與 QC109 的 ParameterClass=1 混用。以 ParameterNo 查回伺服器設定，不接受 request 任意指定 table/column/condition。
- [ ] 實作 `GET /api/qc/special-basis-maintain/parameters/{parameterNo}/data`，依已授權 ParameterNo 查 YMES metadata，回傳動態欄位（name/type/length/nullable）與 Condition 範圍的資料列；TableName、FieldName、Condition 都須由服務端參數目錄解析並用 schema/table/column allowlist 驗證。
- [ ] 實作 `PUT /api/qc/special-basis-maintain/parameters/{parameterNo}/data`：伺服器端驗證 `^[A-Za-z][A-Za-z0-9_]*$` 欄名、metadata 欄位集合、型別、長度與 NOT NULL；單一 transaction 內刪除舊 Condition 範圍並插入完整 rows，空 rows 表示清除此範圍，失敗 rollback。沒有 DataStamp；避免併發整表覆寫。
- [ ] 實作 `POST /api/qc/special-basis-maintain/parameters/{parameterNo}/import-preview` 解析 WinForms 接受的 Excel 檔並只回 preview rows，不寫資料庫；CSV/Paste 由 Vue 先編輯 Grid，最後才按 Save。
- [ ] 舊 Condition 不可直接當任意 SQL 執行。若不能安全轉成固定 predicate，建立經審查的 server-side 對照並記錄 YMES 環境差異。所有 endpoint（含清單、資料、preview、更新）都需套登入與 QC110 功能授權。
- [ ] 完成 YMES schema/API/migration 文件後 push GitHub 並通知公司端 pull，以驗收 Production API。

### QC109 要因資料維護（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/要因資料維護.md`、公司端 QC109 區段、`docs/DATABASE_CONNECTION.md`，並先 pull 取得 Vue DEV 頁與 API 規格。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_factor_maintain_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_factor_maintain_create.sql`，核對 YMES live schema/migrations 的 `tblsysmesparameter`、`fc_posionlist`、`fc_qcformcallbackitemmap`、`fc_thickness`。只對缺少的表/欄位新增 forward migration，保留 YMES 擴充與資料，不 drop/recreate、不匯入 MES 業務列、不在 Oracle 執行 CREATE。
- [ ] 來源 NOT NULL/default：`tblsysmesparameter`: ParameterNo varchar(30) PK、ParameterName varchar(50)、IssueState numeric(1,0) DEFAULT 0、ParameterType numeric(1,0) DEFAULT 0、ParameterClass numeric(1,0)；TableName/FieldName varchar(50)、Condition varchar(50)、Description varchar(255)、FunctionName varchar(50)、ShowName varchar(50) DEFAULT 'N/A'、DataStamp numeric(11,0) DEFAULT 0、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp。`fc_posionlist.posion_no varchar(50)` PK；`fc_qcformcallbackitemmap.fieldname varchar(30)` PK、QCItemNo varchar(50) NOT NULL、Description varchar(500) NULL；`fc_thickness.spec varchar(50)` PK。來源僅 PK unique index，沒有 FK/額外 index。資料庫欄位 NULL/default 依來源保留。
- [ ] 實作 FunctionKey `QC109` 保護的 `GET /api/qc/factor-maintain/parameters`，只列出已核准、ParameterType=0、ParameterClass=1、TableName LIKE `FC_%` 的 Table 參數，回傳 ParameterNo/Name/TableName；以 ParameterNo 伺服器端查回設定，不採用 request 直接指定 TableName/FieldName/Condition。
- [ ] 實作 `GET /api/qc/factor-maintain/parameters/{parameterNo}/data`：服務端驗證參數及允許的 YMES schema/table，依欄位 metadata 回傳 columns(name/type/length/nullable) 與資料 rows；套用目錄設定 Condition 並依 FieldName 排序。限制 table/column identifier 到 metadata allowlist，不可將使用者字串當 SQL identifier 或任意 where clause。
- [ ] 實作 `PUT /api/qc/factor-maintain/parameters/{parameterNo}/data`：接收整份 Grid rows；驗欄位集合、型別、長度與 NOT NULL，在單一 transaction 內刪除 Condition 範圍資料，再 parameterize insert 所有 rows；插入失敗 rollback；空 rows 表示清空該 Condition 範圍。確保不刪除條件以外的記錄。沒有 DataStamp；完整替換需避免併發覆寫，至少以資料庫 transaction/適當鎖處理。
- [ ] 實作 `POST /api/qc/factor-maintain/parameters/{parameterNo}/import-preview` 解析 WinForms `.xls` 範本（第一列欄名），只回 preview rows，不寫 DB；CSV 可由 Vue 本地解析。功能頁的 Import/Paste 都只修改前端 Grid，最後仍須按 Save。
- [ ] FunctionKey/登入權限套用於目錄、讀取、preview 與更新 endpoints。完成後更新 API/schema/migration 文件、本待辦並 push GitHub，通知公司端 pull。若舊 Condition 字串無法安全轉換，設計可稽核的伺服器端固定 predicate 對照並記錄環境差異；不得任意執行目錄中的 SQL。

### QC104 Wafer 抽樣計劃（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/Wafer抽樣計劃.md`、本檔 QC104 區段、公司端 QC104 區段與 `docs/DATABASE_CONNECTION.md`。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_sampling_plan_wafer_source_ddl.sql`、PostgreSQL CREATE 參考 `database/reference/mestest_qc_sampling_plan_wafer_create.sql`，核對 YMES live schema/migrations。特別先查 `tblqcsamplingrange`：WinForms QC103/QC104 都以相同 PlanNo 表名讀寫，Oracle 沒有 FK/PK/index；核對 QC103 已建表/程式/API及其他既有使用，決定兼容的共用儲存策略，再新增必要 forward migration。不可 drop/recreate、清空既有資料或在 MES Oracle 執行 CREATE。
- [ ] 保留 `tblqcsamplingplan_wafer` nullable/default：PlanNo varchar(25) NOT NULL PK；SampleType numeric(1,0) NOT NULL；SampleRate numeric(3,0)、EveryNum/SamplePieceNum numeric(4,0)、SamplePointNum numeric(6,0) NOT NULL DEFAULT 0；Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp nullable；IssueState numeric(1,0) NOT NULL DEFAULT 0；FunctionName varchar(50)、AQLType varchar(25)、AQL varchar(10)、QCLevel varchar(6)、RejectQty numeric(4,0) nullable；DataStamp numeric(11,0) nullable DEFAULT 0。`tblqcsamplingarea_wafer` 以 (PlanNo,Area) PK，AreaType numeric(18,0) NOT NULL DEFAULT 0、AreaNo varchar(50) nullable DEFAULT '0'。來源三表都無 FK；range 表本身無 PK/index。
- [ ] 實作 FunctionKey `QC104` 保護的 GET/POST/PUT/DELETE `/api/qc/sampling-plans/wafer`，明細 GET `/{planNo}/details`、選項 GET `/aql-options`。List 回傳 audit/DataStamp；主檔、Range、Area aggregate 寫入同一 transaction。AQLType/QCLevel/AQL 選項依 `wsSYS.LoadMesParameterRef` 的 AQLType/QCLevel/AQLData 參數和 105E/GB2828 前綴篩選。
- [ ] 實作狀態規則：編輯限 0；0/-1 刪除主檔與明細，2 用 nullable DataStamp CAS 轉 -1，1 拒絕。Approve IssueNo `WAFER SAMPLING PLAN`：無 Job/Detail 時 0→2，有簽核流程時建立 Issue、轉 1、通知首簽核群組，核准 2/退回 0；狀態與 Reviser/ReviseDate/DataStamp 一併維護。
- [ ] 參數化 SQL；依規格更新 API/schema 文件與本待辦，完成後 push GitHub 並通知公司端 pull。UI 的 DEV AQL 選項是假資料；不得將 MES 業務資料匯入 YMES。


### QC105 品質原因子類別資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/品質原因子類別資料.md`、公司端 QC105 區段與 `docs/DATABASE_CONNECTION.md`。
- [ ] 對照 `database/reference/mestest_qc_reason_subtype_basis_source_ddl.sql` 和 `database/reference/mestest_qc_reason_subtype_basis_create.sql`，核對 YMES live schema/migrations；保留 nullable/default。只有確認缺表或結構差異後建立 forward migration，不得 drop/recreate 或清除既有資料。
- [ ] 欄位依來源保留：`reasontype numeric(1,0) NULL`、`reasonsubtype varchar(50) NOT NULL PK`、`reasonsubtypename varchar(100) NULL`、`description varchar(255) NULL`、`creator/reviser varchar(10) NULL`、`createdate/revisedate timestamp NULL`、`issuestate numeric(1,0) NOT NULL DEFAULT 0`、`datastamp numeric(11,0) NULL DEFAULT 0`。只需 PK unique index；來源沒有 FK 或額外索引。WinForms 雖要求 ReasonSubTypeName 必填，Oracle 欄位本身允許 NULL。
- [ ] 實作 FunctionKey `QC105` 保護的 `GET /api/qc/reason-subtypes?searchFieldName=ReasonSubType&searchFieldValue=`，回傳所有狀態、ReasonType 顯示文字、audit/DataStamp 與依 Issue Control 設定計算的 `editableFields`；搜尋欄只接受 ReasonSubType。
- [ ] 實作 `GET /api/qc/reason-subtypes/reason-types`，依 MES ValueList 等價設定回傳 `{ value, label }`。來源 VB 註解中的代碼不完整且不同年代內容互相矛盾，不可當成正式選項；如 YMES 尚無此 lookup，先記錄設定來源與缺口再決定相容方式。
- [ ] 實作 POST/PUT/DELETE/approve：新增驗必填/長度/ValueList/主鍵唯一並建立狀態 0 audit；修改主鍵唯讀，狀態 1 拒絕，狀態 2/-1 僅允許 Issue Control 的 EditFlag 欄位；DataStamp nullable CAS，衝突回 409。刪除 0/-1 實體刪除、2→-1、1 拒絕。Approve Issue Job Map `ReasonSubType BASIS`，支援直接 0→2 或 Issue 流程 0→1→2/退回 0。
- [ ] 多筆刪除／核准逐筆執行、第一筆錯誤停止，保留已成功項目；使用參數化 SQL、登入者 audit、交易與權限檢查。更新 API/schema 文件及 migration 索引並 push GitHub；不得連 MES Oracle 或匯入 MES 業務資料。


### QC106 品質原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/品質原因基本資料.md`、公司端 QC106 區段及 `docs/DATABASE_CONNECTION.md`；先完成 QC104/QC105 前置交接，尤其 QC105 子類別 options API。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_reason_basis_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_reason_basis_create.sql`，核對 YMES live schema/migrations；只有確認缺表/欄位時建立 forward migration，保留既有 YMES 擴充與資料，不可 drop/recreate，也不可在 MES Oracle 執行 CREATE。
- [ ] 保留來源欄位：ReasonNo varchar(20) NOT NULL PK；ReasonName varchar(100) NOT NULL；ReasonType/ReasonLevel numeric(1,0) NOT NULL；Description varchar(255) NULL；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；IssueState numeric(1,0) NOT NULL DEFAULT 0；ReasonSubType varchar(50) NULL；DataStamp numeric(11,0) NULL DEFAULT 0。只有 PK unique index；Oracle 沒有 FK/額外索引。
- [ ] 實作登入及 FunctionKey QC106 授權的 `GET /api/qc/reason-basis?searchFieldName=ReasonNo&searchFieldValue=`，回傳四種 IssueState、DataStamp 及依 Issue Control 算出的 `editableFields`。只允許 ReasonNo LIKE 搜尋，支援 `%`/`_` 萬用字元，狀態由 UI 篩選。
- [ ] ReasonType options 由 MES `GetValueList(TBLQCREASONBASIS, REASONTYPE)` 等價服務提供；不可把 WinForms 舊註解中的數字/文字當正式清單。子類別 options 僅回傳 QC105 `IssueState=2` 且 ReasonType 相符的項目；`GET /api/qc/reason-basis/subtype-options?reasonType=` 需先授權並驗證參數。Oracle 沒 FK，新增/更新由 API 驗證引用。
- [ ] 實作 `POST /api/qc/reason-basis`、`PUT /api/qc/reason-basis/{reasonNo}`：必填、長度、ReasonLevel 0–9、逗號禁用與 ReasonNo 大寫、ValueList/子類別狀態及類別檢查、Issue Control 欄位白名單。登入者與伺服器時間負責 audit；ReasonNo 修改唯讀。
- [ ] DataStamp nullable DEFAULT 0；PUT/DELETE/approve compare-and-swap 必須區分 NULL 與 0，成功更新版本，衝突回 409。狀態 0 全欄位可編輯、1 唯讀、2/-1 僅允許 Issue Control EditFlag 欄位；缺少控制設定時唯讀。
- [ ] 實作 DELETE：狀態 0/-1 實體刪除，狀態 2 CAS 後更新為 -1 並記錄 Reviser/ReviseDate/DataStamp，狀態 1 拒絕。多選 UI 逐筆呼叫，第一筆失敗停止並回報先前成功筆數。
- [ ] 實作 `POST /api/qc/reason-basis/{reasonNo}/approve`：依 Issue Job Map `REASON BASIS` 無 Job/Detail 時直接 0→2；有 Detail 建 Issue、0→1、通知首簽核群組；後續核准 2、退回 0。回傳真實狀態及 DataStamp，不能假設一律直接 Active。
- [ ] 全部 SQL 參數化；API/schema/migration 變更後更新規格及本清單並 push GitHub，通知公司端 pull。公司端 DEV 頁 ReasonType 標 DEMO，Production 不使用 localStorage 假資料。

### QC107 計數判別法則基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/計數判別法則基本資料.md`、公司端 QC107 區段與 `docs/DATABASE_CONNECTION.md`；依序完成 QC104–QC106 既有交接，然後核對 QC107 API/schema。
- [ ] 對照 Oracle DDL `database/reference/mestest_qc_att_rule_basis_source_ddl.sql` 與 PG CREATE 參考 `database/reference/mestest_qc_att_rule_basis_create.sql`，檢查 YMES live schema/migrations。主檔 `tblqcattrulebasis` PK RuleNo；明細 `tblqcattruledetail` 複合 PK (RuleNo,Action,ReasonType,ReasonLevel)；兩表均無 FK、無 DataStamp。只建立必要 forward migration，不 drop/recreate、不清除資料。
- [ ] 對照共用 AQL DDL `database/reference/mestest_qc_aql_source_ddl.sql`、PG CREATE 參考 `database/reference/mestest_qc_aql_create.sql`，檢查是否已經有 YMES `tblqcaql`。若已存在直接重用；只有確定缺少才新增 migration，保留現有共用表資料/擴充欄位。來源 PK (AQLType,MinNum,MaxNum,SLevel,AQL)，無 FK/額外索引。
- [ ] `tblqcattrulebasis` 欄位：RuleNo varchar(20) NOT NULL PK；ComponentCollect numeric(1,0) NOT NULL DEFAULT 0；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；IssueState numeric(1,0) DEFAULT 0 NULL。`tblqcattruledetail`：RuleNo varchar(20)、Action numeric(1,0) NOT NULL、ReasonType numeric(1,0) NOT NULL DEFAULT 0、ReasonLevel numeric(1,0) NOT NULL DEFAULT -1、RejectNum numeric(14,4)、RuleType numeric(1,0) DEFAULT 0、Rate varchar(10)、ChangeAQL varchar(30)、Creator/Reviser varchar(10)、日期 timestamp；按來源保留 nullable/default。
- [ ] 實作 FunctionKey QC107 登入權限保護的 `GET /api/qc/attribute-rule-bases?searchFieldName=RuleNo&searchFieldValue=` 與 `GET /{ruleNo}/details`，回主檔/明細及 Issue Control `editableFields`。RuleNo LIKE 支援 `%`/`_`；前端按四狀態過濾。主/明細讀取須授權同一 FunctionKey。
- [ ] 建立 Action/ReasonType ValueList options API；Action 等價於 `TBLQCATTRULEDETAIL.ACTION`，ReasonType 等價於 `GetValueList(TBLQCATTRULEBASIS,REASONTYPE)`。舊程式註解只列部分值，不可作正式選項。自訂 AQL 選項先讀 SysInfo/UserDefineAQL，啟用時由 `tblqcaql` 回 distinct AQL、AQLType 非空；關閉時 API 回 enabled=false，不查選項資料列。
- [ ] 實作 aggregate `POST/PUT /api/qc/attribute-rule-bases`：主檔與整組明細單一交易保存。驗證 RuleNo 大寫/長度/唯一、ComponentCollect 0/1、Action/ReasonType 合法、ReasonLevel -1..9、組合 PK 唯一；Number RejectNum 必填且 >=1 整數、上限 9,999,999,999（符合 NUMBER(14,4)）、Rate 必填正數且 <=10 字元、AQL 型態檢查 UserDefineAQL。非啟用模式的空 Rate/ChangeAQL 對 Oracle 等價 NULL；RejectNum 在非 Number 模式依舊 API 的空字串轉換為 0。
- [ ] 舊 WinForms Edit 會刪除再重建主/明細，沒有 DataStamp；API 使用 transaction 原子化，保留舊狀態及 Creator/CreateDate。Def 雖設定 Reviser/ReviseDate，SOAP 未傳入，舊 Core 更新後沒有寫回修訂 audit；YMES 若補 audit 必須記錄為新 API 差異。
- [ ] 實作 DELETE：狀態 0/-1/2 實體刪除明細與主檔，同交易；狀態 1 拒絕。Approve 僅 state0 且每個 RuleNo 至少一筆明細；依 Issue Job Map `QCITEM_A RULE DATA` 無流程時 0→2，有簽核則 0→1、通知首簽群組，核准2/退回0；回傳實際狀態。
- [ ] 多選刪除/核准依 WinForms 逐筆執行，首錯停止並保留已完成筆數；全部 SQL 參數化。比對 YMES schema、遷移及 API 後更新文件並 push，通知公司端 pull。不得連 MES Oracle 或匯入業務列。

### QC108 計量品管法則基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/計量品管法則基本資料.md`、公司端 QC108 區段與 `docs/DATABASE_CONNECTION.md`；完成既有 QC104–QC107 API/schema 交接後再核對 QC108。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_variable_rule_basis_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_variable_rule_basis_create.sql`，檢查 YMES live schema/migrations。只在確認缺表/欄位時新增 forward migration；保留 YMES 擴充與資料，不 drop/recreate，不在 Oracle 執行 CREATE。
- [ ] 欄位 nullable/default 依來源保留：RuleSerial varchar(30) NOT NULL PK；RuleNo varchar(30) NOT NULL；RuleName varchar(50) NOT NULL；FunctionName varchar(50) NULL；RuleType/IssueState numeric(1,0) NULL DEFAULT 0；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；Description varchar(255) NULL；RuleOption numeric(1,0) NULL；TestOutof/TestCount numeric(2,0) NULL；CompareValue1/2/Relation varchar(10) NULL；WEType numeric(1,0) NULL DEFAULT 0；DataStamp numeric(11,0) NULL DEFAULT 0。唯一索引僅 PK(RuleSerial)，沒有 FK/額外索引；RuleNo 無 DB 唯一限制。
- [ ] 實作 FunctionKey QC108 保護的 `GET /api/qc/rule-bases`，回傳四狀態資料、audit、DataStamp、editableFields；UI 篩選 IssueState、`RuleType<>2`、`WEType=0` 並依 RuleSerial/RuleNo 排序。新增 rule-options options API，從現存 ValueList 供應原始儲存值/標籤；依規格的 UI/Oracle RuleOption mapping。另實作 functions options API，等價於舊 `LoadQRFunctionName` 對 `udQCRule` 私有 FUNQR* 函式目錄，不載入 DLL 反射給 Web。
- [ ] 實作 POST/PUT/DELETE/approve：新增 RuleSerial=RuleNo、state0、audit；RuleNo 大小寫不敏感重複檢查在 API transaction 內執行。更新以 DataStamp nullable CAS，維護 Reviser/ReviseDate/DataStamp；`editableFields` 缺失時唯讀。Delete 0/-1 實體刪除、2→-1、1 拒絕；Approve state0，依 Issue Job Map `QC RULE BASIS` bypass 0→2 或建立 Issue 後 0→1，核准2/退回0，使用共用 Issue/通知流程。
- [ ] 依 WinForms sparse update：只更新有傳入的欄位；System Rule 欄位與 User Define FunctionName 的切換不應無意清除其他既有欄位。驗證 RuleName、RuleNo 長度/必填、RuleType、FunctionName、七種 RuleOption、TestOutof/TestCount 1–99、TestOutof≥TestCount、Value Range 上下限、SpecLimit relation 等。RuleOption=9 舊資料顯示 Other；API 保留既有 9，新建 Other 用 5。多選操作逐筆、第一個錯誤停止並回報已完成筆數；全部 SQL 參數化。
- [ ] 更新 API/schema/migration 文件及本清單後 push GitHub 並通知公司端 pull。不得連 MES Oracle 或匯入 MES 業務資料。

### QC103 抽樣計劃（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/抽樣計劃.md`、本檔 QC103 區段、公司端 QC103 區段及 `docs/DATABASE_CONNECTION.md`。
- [ ] 比對 YMES PostgreSQL live schema/migrations。Oracle source DDL：`database/reference/mestest_qc_sampling_plan_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_create.sql`。只在確認缺漏時新增 forward migration，保留 YMES 擴充欄位；不可在 MES Oracle 執行 CREATE。
- [ ] 保留 `tblqcsamplingplan` 欄位與 nullable/default：PlanNo varchar(25) NOT NULL PK；PlanName varchar(50) NOT NULL；PlanType numeric(1,0) NOT NULL；FunctionName varchar(50)、Description varchar(255)、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp nullable；IssueState numeric(1,0) NOT NULL DEFAULT 0；SQtyCanBBiggerThenPQty numeric(1,0) NOT NULL DEFAULT 0；DataStamp numeric(11,0) nullable DEFAULT 0。`tblqcsamplingparameter` 欄位 PlanNo varchar(25)、ParameterNo varchar(20) NOT NULL 複合 PK，ParameterValue varchar(255) nullable。來源沒有 FK/額外索引。
- [ ] 實作 FunctionKey `QC103` 保護的 GET/POST/PUT/DELETE `/api/qc/sampling-plans`；主檔和參數 Aggregate 寫入需單一交易，Edit/核准/狀態刪除使用 nullable DataStamp CAS。狀態 0/-1 實體刪除兩表、2 改 -1、1 拒絕。
- [ ] 實作 `/plan-types`、`/function-options`、`/{planNo}/parameters`、`/parameter-options/{parameterNo}`：PlanType 取自現有 ValueList；FunctionName 依 `udSamplingFunction` 名稱篩選 `FUNSA*`；ParameterValue 選項依 `wsSYS.LoadElementValue` 對應邏輯；ShowFlag 依 PlanType 推導，不需新增資料庫欄位。
- [ ] 實作核准 `POST /{planNo}/approve`：先驗證參數，再依 Issue Job Map `SAMPLING PLAN` 無 Job 時 0→2；有 Job/Detail 時建立 Issue、狀態 1、通知第一簽核群組，核准轉 2、退回回 0。所有狀態轉換維護 Reviser/ReviseDate/DataStamp。
- [ ] 實作 `POST /{planNo}/simulate`：依 0–7 PlanType 呼叫 WinForms 原 Core 的 `ShowParameter_*` 對應規則；不能沿用 DEV 固定抽樣近似值作正式計算。
- [ ] 使用參數化 SQL；完成後更新 API/schema 文件與本待辦並 push GitHub，通知公司端 pull。來源無 MES 業務資料，Oracle 只查 DDL。


### QC102 抽樣計劃設定_1916（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/抽樣計劃設定_1916.md`、本檔 QC102 區段、公司端 QC102 區段及 `docs/DATABASE_CONNECTION.md`。
- [ ] 比對 YMES PostgreSQL live schema/migrations。Oracle source DDL：`database/reference/mestest_qc_sampling_1916_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_1916_create.sql`。只在確認缺漏時新增 forward migration，保留 YMES 擴充欄位；不可在 MES Oracle 執行 CREATE。
- [ ] 保留欄位 nullable/default：SamplingType varchar(25) NOT NULL；MinNum/MaxNum numeric(8,0) NOT NULL；SLevel varchar(6) NOT NULL；SampleNum/RejectNum numeric(6,0) nullable；KValue/FValue numeric(10,3) nullable；Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp nullable；DataStamp numeric(11,0) nullable DEFAULT 0。索引為獨立 unique index `(samplingtype,minnum,maxnum,slevel)`；來源沒有 PK constraint、FK 或其他 index。
- [ ] 實作登入與 FunctionKey `QC102` 保護的 `GET /api/qc/sampling-1916`，回傳全表與 audit/DataStamp。UI 依 SamplingType 在前端切換，不帶搜尋條件。
- [ ] 實作 `POST /api/qc/sampling-1916` body `{ samplingType, minNum, maxNum, sLevel, sampleNum, rejectNum, kValue, fValue }`；驗證 NOT NULL、區間、拒收數不得大於抽樣數、Variables 必填 K/F、複合鍵唯一。Creator/CreateDate 由登入者及伺服器時間維護。
- [ ] 實作複合鍵 `PUT /api/qc/sampling-1916/{samplingType}/{minNum}/{maxNum}/{sLevel}?dataStampIsNull=&dataStamp=`，僅更新 SampleNum/RejectNum/KValue/FValue；Reviser/ReviseDate/DataStamp 由伺服器更新，nullable DataStamp 需以 CAS 區分 NULL 與 0，衝突回 409。實作相同複合鍵 DELETE；UI 多選時逐筆呼叫，首錯停止，保留已完成刪除。
- [ ] 若此 API 要服務檢驗流程，實作並記錄 `ShowParameter_Sampling1916` 等價抽樣解析：Population=1 特例、SamplingType/SLevel、含端點區間查找、`SQtyCanBBiggerThenPQty` 上限，以及 Variables 的 K/F 回傳；找不到資料需有明確錯誤。
- [ ] 使用參數化 SQL；完成後更新 API/schema 文件與本待辦、確認遷移/API 後 push GitHub，通知公司端 pull。此主檔沒有簽核/Issue Job。


### QC101 抽樣表格（公司端 2026-11-17 交接）

- [ ] 閱讀 docs/modules/QC/抽樣表格.md、todolist_officepc.md 的 QC101 區段及資料庫連線紀錄。
- [ ] 比對 YMES PostgreSQL live schema/migrations。Oracle 來源 DDL：database/reference/mestest_qc_sampling_table_source_ddl.sql；PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_sampling_table_create.sql。只有確認缺漏才建立 migration，不要在 Oracle 執行 CREATE，也不要刪除 YMES 擴充欄位。
- [ ] 保留欄位與 NULL/default：SamplingTable varchar(25)、MinNum/MaxNum/SampleNum numeric(12,4) 必填、RejectNum numeric(12,4) 可空、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp、DataStamp numeric(11,0) nullable DEFAULT 0。唯一索引只有複合 PK (SamplingTable, MinNum, MaxNum)，沒有 FK/其他索引。
- [ ] 實作登入與 FunctionKey QC101 保護的 GET /api/qc/sampling-tables?searchFieldName=&searchFieldValue=，限定 SamplingTable/MinNum/MaxNum 搜尋、回傳全部區間與 audit/DataStamp；先核對舊系統共用 AdditionalXml helper 的實際比對方式，再與 DEV 搜尋及正式 API 對齊。
- [ ] 實作 POST 與複合鍵 PUT/DELETE；沿用 UI required/numeric/integer/RejectNum 上限/MinNum≤MaxNum 驗證。Add 設 Creator/CreateDate，Edit 設 Reviser/ReviseDate 並 DataStamp + 1；PUT/DELETE 對 DataStamp nullable 做 CAS。此功能沒有 IssueState、簽核或 Issue Job API。
- [ ] 實作 POST /api/qc/sampling-tables/copy：只複製 sourceSamplingTable 中目的表不存在的 MinNum/MaxNum 區間；逐筆插入，首錯停止但保留之前成功筆，回傳 insertedCount、skippedCount 與部分失敗資訊。前端已呼叫此端點。
- [ ] 使用參數化 SQL、登入使用者 audit；先查現有 migration/API 是否已建置。完成後更新 API/schema 交接文件，推送 GitHub 並通知公司端 pull。


### QC402 次分類基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/次分類基本資料.md`、`todolist_officepc.md` 的 QC402 區段及 `docs/DATABASE_CONNECTION.md` 的 QC402 查詢紀錄。
- [ ] 比對 YMES PostgreSQL live schema/migrations。Oracle source DDL：`database/reference/mestest_qc_s_class_basis_source_ddl.sql`；空表 PostgreSQL CREATE 參考：`database/reference/mestest_qc_s_class_basis_create.sql`。僅在缺漏時補 forward migration，保留 YMES 擴充欄位；不要在 MES Oracle 執行 CREATE。
- [ ] 保留 `tblqcsclassbasis` 欄位與來源預設：`sclassno varchar(30) NOT NULL PK`、`description varchar(255)`、`creator/reviser varchar(10)`、`createdate/revisedate timestamp without time zone`、`issuestate numeric(1,0) NOT NULL DEFAULT 0`、`mclassno varchar(30) NULL`、`datastamp numeric(11,0) NULL DEFAULT 0`。只建立 PK unique index，不加 FK 或其他索引；UI/API 以規則檢查已簽核 MClass。
- [ ] 實作 `GET /api/qc/s-class-basis?searchFieldName=SClassNo&searchFieldValue=`，只接受 SClassNo 搜尋，回傳所有狀態與 audit/DataStamp；登入與 FunctionKey QC402 權限必須檢查。
- [ ] 實作 `GET /api/qc/s-class-basis/m-class-options`，依 QC402 權限只回傳 `TBLQCMCLASSBASIS.IssueState=2` 的 MClassNo/Description；靜態 lookup route 必須先於 `/{sClassNo}` route，避免要求使用者另有 QC401 權限。
- [ ] 實作 POST body `{ sClassNo, mClassNo, description }`：兩編號必填且最多 30 字、Description 最多 255 字、SClassNo 大小寫不敏感唯一、MClassNo 必須存在且已簽核。伺服器設定 Creator/CreateDate、IssueState=0。
- [ ] 實作 PUT `/api/qc/s-class-basis/{sClassNo}?issueState=&dataStamp=` body `{ mClassNo, description }`；SClassNo 唯讀，僅 IssueState 0/2 更新，MClassNo 僅能選已簽核資料，Reviser/ReviseDate/DataStamp 由伺服器更新並做 nullable DataStamp CAS，衝突回 409。
- [ ] DELETE：狀態 0/-1 實體刪除；2 改為 -1 並更新 audit/DataStamp；1 拒絕。Approve 僅接受 0，Issue Job Map `SCLASS BASIS`；沒有 Job/Detail 時轉 2，有流程則建立 Issue、轉 1、通知第一簽核群組，核准轉 2、退回回 0。
- [ ] 實作 `/import/preview`、`/import` multipart `.xls/.xlsx`：NPOI 第一工作表、略過首列，前三欄固定為 SClassNo/MClassNo/Description，不驗標題；兩編號必填、Description 可空；preview distinct `(SClassNo,MClassNo)`，匯入按 SClassNo 分組，同組超過一列整組重複失敗；逐組交易、部分成功、回傳每個預覽 pair 結果並可取消後續列。驗 MClass 已簽核、SClassNo case-insensitive unique；成功時兩編號轉大寫，IssueState=0、Creator=登入者。
- [ ] 實作 `/import/template`，透過 YMES template storage 供應 FunctionNo `SCLASS BASIS` 的正式範本及副檔名；來源樹找不到範本 binary。DEV CSV 僅示範欄序，不可冒充正式範本。
- [ ] 全部 API 使用參數化 SQL、QC402 登入/功能權限和版本檢查；更新 API 文件/migration 索引及本 TODO。不得連 MES Oracle。

### QC401 主分類基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 docs/modules/QC/主分類基本資料.md、todolist_officepc.md 的 QC401 區段，以及 docs/DATABASE_CONNECTION.md 的 QC401 查詢紀錄。
- [ ] 比對 YMES PostgreSQL live schema 與 migrations。TBLQCMCLASSBASIS Oracle source DDL / PostgreSQL CREATE 已存在於 database/reference/mestest_qc_first_group_lookups_source_ddl.sql 和 _create.sql；只在 YMES 缺表或欄位時補 forward migration，勿在 MES Oracle 建表或重建既有 YMES 擴充欄位。
- [ ] schema 欄位需保留 MClassNo varchar(30) NOT NULL PK、Description varchar(255)、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp without time zone、IssueState numeric(1,0) NOT NULL DEFAULT 0、DataStamp numeric(11,0) NULL DEFAULT 0。只有 PK unique index，沒有 FK/其他 index。
- [ ] 實作 GET /api/qc/m-class-basis?searchFieldName=MClassNo&searchFieldValue=，回傳所有 IssueState 與 audit/DataStamp；只接受 MClassNo 搜尋，QC401 FunctionKey 權限及登入必須檢查。
- [ ] 實作 POST 建立，body 為 mClassNo、description；驗證 MClassNo 必填/長度 30/唯一及 Description 長度 255；伺服器設定 Creator/CreateDate、IssueState=0，DataStamp 使用 default。
- [ ] 實作 PUT /api/qc/m-class-basis/{mClassNo}?issueState=&dataStamp=，僅更新 Description；只允許 IssueState 0/2；MClassNo 唯讀，Reviser/ReviseDate 由伺服器更新。使用 nullable DataStamp CAS，衝突回 409。
- [ ] 實作 DELETE：狀態 0/-1 實體刪除；狀態 2 以 CAS 改 IssueState=-1 並更新 Reviser/ReviseDate/DataStamp；狀態 1 拒絕。
- [ ] 實作 POST /api/qc/m-class-basis/{mClassNo}/approve：限 IssueState=0，Issue Job Map key 為 MCLASS BASIS。找不到 Job 或 Job Detail 時直接轉 2；有流程時建立 Issue、轉 1、通知第一簽核群組；核准轉 2，退回回 0。更新共用 Issue Job/API 規格。
- [ ] WinForms 匯入器已找到：STD/MESWin/QC/QC_MClassDataImport/frmMClassDataImport.vb、STD/MEStc/DataImporter/MClassDataImporter.cs。實作 POST /api/qc/m-class-basis/import/preview 與 /import：NPOI 讀 .xls/.xlsx 第一工作表；跳過首列，固定用第 1 欄 MClassNo（必填）、第 2 欄 Description（可空），不驗標題；先 distinct (MClassNo, Description)，按 MClassNo 分組逐組檢查/插入；大小寫不敏感拒絕已存在鍵、編號轉大寫；同編號不同說明失敗；每組獨立 transaction，部分成功並回傳結果。
- [ ] 實作 GET /api/qc/m-class-basis/import/template，透過 YMES template storage 提供舊 wsSYS.GetTemplateFile 的 MCLASS BASIS 範本與副檔名。來源樹找到 downloader/extension API 呼叫，但未找到範本 binary；部署前須接原始 template storage 或取得正式範本。DEV CSV 只演示相同欄序，不能冒充官方 Excel 範本。
- [ ] API 使用參數化 SQL、登入與 QC401 功能權限；更新 API 文件、migration 索引與本 TODO。不得連 MES Oracle。

### QC403 異常原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常原因基本資料.md`、`todolist_officepc.md` QC403 區段及 `docs/DATABASE_CONNECTION.md` 的 QC403 查詢紀錄。
- [ ] Oracle source DDL：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`；PostgreSQL 空表 CREATE：`database/reference/mestest_qc_hold_item_basis_create.sql`。先比對 YMES live schema/migrations；只有 `tblqcholditembasis` 缺少才新增 forward migration，保留 YMES 擴充欄位。不可在 MES Oracle 執行 CREATE。
- [ ] 主表 PK `holditemno`；僅 PK unique index，無 FK/額外索引。保留來源 nullable/default：`createerf` numeric(1,0) DEFAULT 1 NULL、`datastamp` numeric(11,0) DEFAULT 0 NULL；`holdsystem` numeric(1,0) DEFAULT 0 NOT NULL；IssueState numeric(1,0) DEFAULT 0 NOT NULL。Oracle DATE 對應 timestamp without time zone。
- [ ] 實作 `GET /api/qc/hold-item-basis?searchFieldName=&searchFieldValue=`，回傳查詢條件內所有 IssueState 與 audit/DataStamp；搜尋欄 whitelist 僅 HoldItemNo/MClassNo/SClassNo；FunctionKey `QC403`。
- [ ] 實作 `GET /api/qc/hold-item-basis/lookups`：已簽核 MClass、依 MClass 過濾的已簽核 SClass、已簽核 HoldDispGroup、IssueState=2 且 CURVERSION=1 的 OCAP、HoldSystem 0/1/2（MES/EAP_Exception/EDA_Exception）。優先重用已有 QC lookup service，禁止新增重複 lookup 表。
- [ ] 實作 POST 建立與 PUT 修改。伺服器設定 Creator/CreateDate、IssueState=0、Reviser/ReviseDate/DataStamp；HoldItemNo PK。修改只允許 HoldItemName、CreateERF、HoldSystem、HoldDispGroup、OCAPNo/Version、Description；HoldItemNo/MClassNo/SClassNo 唯讀；待簽核禁止修改，Active/Unused 按舊 IssueControl 設定的欄位權限限制。以 HoldItemNo + IssueState + nullable DataStamp CAS。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 改為 -1；1 拒絕。核准只允許 0，FunctionNo/Issue Job Map key 為 `HOLDITEM BASIS`；沒有 Job/Detail 時直接轉 2，有簽核流程時建立 Issue、轉 1、通知第一群組，保留共用退回/核准流程。
- [ ] 實作 `POST /api/qc/hold-item-basis/import` 支援 `.xls/.xlsx` 第一工作表，欄序為 HOLDITEMNO、MCLASSNO、SCLASSNO、HOLDITEMNAME、CREATEERF、HoldSystem、OCAPNo、HOLDDISPGROUP、DESCRIPTION；逐列檢查分類已簽核、群組存在、OCAP 已簽核、主鍵不重複。每列獨立 transaction，回傳成功/失敗與 message；維持舊系統部分成功行為。DEV 頁目前只解析 CSV。
- [ ] 所有 endpoints 要登入並檢查 QC403 功能權限；參數化 SQL、DTO 驗證、檔案大小/格式限制，不讀/寫 MES Oracle。更新 API 文件、migration 索引及本 TODO。

### QC405 異常處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常處理群組.md`；公司端已完成 `/YMES/QC/HoldDispGroup` Vue DEV 頁，包含群組主檔及主處理/CC 使用者三 Grid。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations。Oracle source DDL/PG 空表 CREATE 已存在：`TBLQCHOLDDISPGROUPBASIS` 參照 `mestest_qc_first_group_lookups_source_ddl.sql` / `_create.sql`；`TBLUSRUSERGROUP` 參照 `mestest_equipment_engineer_group_member_source_ddl.sql` / `_create.sql`。兩表缺漏才新增 forward migration；本功能沒有新增表，不要在 Oracle 建表。
- [ ] 實作 `GET /api/qc/hold-disposition-groups?searchFieldName=GroupNo&searchFieldValue=`：回傳四狀態群組及 audit/DataStamp，搜尋只允許 GroupNo。FunctionKey 權限為 QC405。
- [ ] 實作 `GET /api/qc/hold-disposition-groups/options/users?issueState=2`，等價 WinForms `wsUSR.LoadUserSecurity(IssueState=2)`，只回已核准使用者的 DepartmentNo/UserNo/UserName。若 YMES 已有安全的使用者查找 API，優先重用。
- [ ] 實作 `GET /api/qc/hold-disposition-groups/{groupNo}/members`，回 `{primary: string[], cc: string[]}`。直接 UI 成員對應 `TBLUSRUSERGROUP.GroupType=5/6 AND AgentOf='NA'`；顯示可沿用已核准使用者資料，缺失/停用的既存關聯仍需可見並可移除。
- [ ] 實作 POST `/api/qc/hold-disposition-groups` body `{groupNo, groupName, description}`：GroupNo 必填/長度20/大寫、GroupName 必填/長度50、Description 長度255；伺服器寫 Creator/CreateDate、IssueState=0、DataStamp 初值。
- [ ] 實作 PUT `/api/qc/hold-disposition-groups/{groupNo}?dataStamp=`，僅更新 GroupName/Description，只接受 IssueState 0/2；由伺服器更新 Reviser/ReviseDate/DataStamp，以 GroupNo + nullable DataStamp CAS。
- [ ] 實作 POST `/api/qc/hold-disposition-groups/copy`，body `{sourceGroupNo, groupNo, groupName, description}`；transaction 同時新增主檔並複製舊 Core `ShowHoldDispGroupDetail(CCGroup=true)` 回傳的 5/6 類成員。舊 `Show...` 不限 AgentOf，但 `AddHoldDispGroupDetail` 將複製關聯寫成 AgentOf='NA'；保持這個相容行為，或在 API 規格清楚標註產品決策後的修正。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除主檔與該 GroupNo 的 5/6 類成員；IssueState 2 改 -1 並保留成員；IssueState 1 拒絕。用主檔 DataStamp CAS 並在 transaction 中處理。
- [ ] 實作 PUT `/api/qc/hold-disposition-groups/{groupNo}/members?dataStamp=`，body `{primaryUserNos, ccUserNos}`。單一 transaction 先以 GroupNo + DataStamp CAS 更新主檔 Reviser/ReviseDate/DataStamp，再刪除該群組所有 GroupType 5/6 關聯，重建選取的直接成員 (AgentOf='NA')；兩個清單不得重複同一 UserNo，空陣列表示清除。每個 GroupType 5 成員需沿用 USR `AddUserAgentToGroup(UserNo, GroupNo, 5)` 代理人同步副作用。
- [ ] 實作 `POST /api/qc/hold-disposition-groups/{groupNo}/approve?issueState=0&dataStamp=`：Issue Job Map 精確 key `HOLD DISP GROUP`；沒有 Job/Detail 直接轉 2，有流程建立 Issue、狀態轉 1、通知第一簽核群組，核准轉 2、退回轉 0。
- [ ] 所有端點驗證登入、QC405 功能權限、SQL 參數化及版本衝突；先比對 live schema，不重建已存在表。部署 API/schema 後更新 handoff、push 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC404 異常說明（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常說明.md`、`docs/DATABASE_CONNECTION.md`；公司端已對照 `frmHoldDescription`、`frmHoldDescriptionDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`，完成 QC → 異常設定 → 異常說明 Vue DEV 頁與 `/YMES/QC/HoldDescription` 路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations；來源 CREATE 參考 `database/reference/mestest_qc_hold_description_create.sql`，只在缺少時做 forward migration，保留 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_hold_description_source_ddl.sql`。`tblqcholddescription` PK 為 `holddescriptionid`；`mclassno` 有非唯一索引、無 FK；`holddescription varchar(4000) NOT NULL`、`issuestate NOT NULL DEFAULT 0`、`datastamp nullable DEFAULT 0`。不對 `(mclassno, holddescription)` 加唯一限制。
- [ ] 實作 `GET /api/qc/hold-descriptions?searchFieldName=MClassNo&searchFieldValue=`，回全部狀態及 audit/DataStamp；搜尋欄白名單只允許 MClassNo，保持 WinForms query condition 語意。
- [ ] 實作主分類 options endpoint，只回 `TBLQCMCLASSBASIS.IssueState=2`；沿用 QC406 lookup table/API 時仍要驗證 QC404 FunctionKey。
- [ ] 實作 POST `/api/qc/hold-descriptions` body `{mClassNo, holdDescription, description}`；檢查主分類核准、必填與長度，伺服器建立 GUID HoldDescriptionID、Creator/CreateDate、IssueState=0、DataStamp 初值，回傳完整新列。
- [ ] 實作 PUT `/api/qc/hold-descriptions/{holdDescriptionId}?dataStamp=`，僅允許修改 Description；MClassNo/HoldDescription 在 WinForms 修改視窗唯讀。狀態 0/2 可更新，伺服器寫 Reviser/ReviseDate 並遞增 DataStamp，nullable DataStamp 以 `IS NULL` CAS。
- [ ] 實作 DELETE：0/-1 實體刪除，2 軟停用為 -1，1 拒絕；所有寫入/刪除用 ID + DataStamp CAS。多選 UI 逐筆呼叫，首筆失敗停止並回報部分成功。
- [ ] 實作核准端點，Issue Job Map key 為 `HOLD DESCRIPTION`；沒有 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、處理附件/通知第一簽核群組，核准轉 2、退回 0。
- [ ] 所有 API 驗證登入與 FunctionKey `QC404`、SQL 參數化。先核對 YMES live table/index，勿盲目執行 CREATE；部署 API/schema 後更新 handoff 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC407 最後處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/最後處理群組.md`、`docs/DATABASE_CONNECTION.md`；公司端已對照 `frmFinalGroup`、`frmFinalGroupDef`、`wsQC`、`clsQCBasis`、`clsBasis_UPD`，完成 QC → 異常設定 → 最後處理群組 Vue DEV 頁與 `/YMES/QC/FinalGroup` 路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes、migrations；來源 CREATE 參考 `database/reference/mestest_qc_final_group_create.sql`，只在缺少時做 forward migration並保留 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_final_group_source_ddl.sql`。`tblqcfinalgroup` 複合 PK `(mclassno, opno)`、只有 PK unique index、無 FK；`issuestate NOT NULL DEFAULT 0`、`datastamp nullable DEFAULT 0`。保留 `opno='*'` 通用作業站鍵。
- [ ] 實作 `GET /api/qc/final-groups?searchFieldName=&searchFieldValue=`，回全部狀態/audit/DataStamp；搜尋欄白名單限 MClassNo、OPNo，四狀態由 UI 篩選。
- [ ] 主分類、作業站、異常處理群組 options 只回 IssueState=2；可重用 QC406 查找實作，但檢查 QC407 FunctionKey 權限。先核對 YMES live schema，不重建已存在的 lookup 表。
- [ ] 實作 POST `/api/qc/final-groups`，body `{mClassNo, operationNos, groupNo, description}`；逐個所選 OP 建列，驗證必填/長度/核准選項/複合鍵唯一；`*` 僅作明確允許的通用 OP。批次以單一交易寫入，Audit/DataStamp 由伺服器設定。
- [ ] 實作 PUT `/api/qc/final-groups/{mClassNo}/{opNo}?dataStamp=`，僅更新 GroupNo/Description；狀態 0/2 可更新，修改者/日期與 DataStamp 由伺服器更新。nullable DataStamp 以 `IS NULL` CAS。
- [ ] 實作 DELETE：0/-1 實體刪除、2 軟停用成 -1、1 拒絕；全部以 DataStamp CAS。UI 多選逐筆、首筆失敗停止並回報部分成功。
- [ ] 實作核准端點，Issue Job Map 原始 key 必須使用 **`Final GROUP`**（注意它和 FunctionNo `FINAL GROUP` 不同）；沒有 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、附件處理/通知第一簽核群組，核准 2、退回 0。
- [ ] 所有 API 驗證登入與 FunctionKey `QC407`、SQL 參數化；部署 API/schema 後更新規格與 handoff，push 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC406 第一處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/第一處理群組.md`、`docs/DATABASE_CONNECTION.md` 和本節。公司端已對照 WinForms/Form/Service/Core，完成 Vue DEV 畫面及 QC → 異常設定 → 第一處理群組路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations。來源 CREATE 參考 `database/reference/mestest_qc_first_group_create.sql`；缺表/欄位才建 forward migration，不要盲目執行或刪除 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_first_group_source_ddl.sql`。表 `tblqcfirstgroup` 的 PK 為 `(mclassno, opno)`，只有 PK unique index、沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。保留 `OPNo='*'` 作為通用作業站 key。
- [ ] 實作 `GET /api/qc/first-groups?searchFieldName=&searchFieldValue=`，回全部狀態欄位與 audit/DataStamp，搜尋欄位白名單限 MClassNo、OPNo；UI 按四狀態本機篩選。
- [ ] 查找器沿用 YMES 已有 endpoint；若不存在，提供只回已核准資料的 options API：主分類 `TBLQCMCLASSBASIS` (MClassNo)、作業站 `TBLOPBASIS` (OPNo/OPName)、異常處理群組 `TBLQCHOLDDISPGROUPBASIS` (GroupNo/GroupName/Description)，均對應 WinForms `IssueState=2`。先比對 live schema；主分類/異常群組缺表時參考 `mestest_qc_first_group_lookups_create.sql`，作業站缺表時參考現有 `mestest_operation_and_system_create.sql`。不要覆蓋既有 YMES 擴充欄位。
- [ ] 實作 `POST /api/qc/first-groups` body `{mClassNo, operationNos, groupNo, description}`；一次對每個已選 OP 建列，驗證主分類/群組已核准、普通 OP 已核准、`*` 僅作為特殊通用 key 放行，檢查長度/必填/複合鍵唯一；API 設 Creator/CreateDate/IssueState=0/DataStamp 初值，多列寫入交易。
- [ ] 實作複合鍵 `PUT /api/qc/first-groups/{mClassNo}/{opNo}?dataStamp=`，只改 GroupNo/Description；依舊 Service 僅接受狀態 0/2。更新 Reviser/ReviseDate 並遞增 DataStamp，nullable DataStamp 用 `dataStamp=0&dataStampIsNull=true` 以 `IS NULL` CAS。
- [ ] 實作複合鍵 DELETE：狀態 0/-1 實體刪除，2 改 -1，1 拒絕；以 DataStamp CAS。UI 多選仍逐筆，第一筆失敗停止並回報部分成功。
- [ ] 實作核准 endpoint，Issue Job Map key 為 `FIRST GROUP`；無 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、附件處理/通知第一簽核群組，Issue 核准轉 2、退回轉 0。所有狀態變更需以複合鍵 + DataStamp CAS。
- [ ] 所有 API 驗證登入與 FunctionKey `QC406` 權限，SQL 參數化；不新增 Oracle FK/cascade。部署 schema/API 後更新規格、push，通知公司端 pull Production API。公司端未執行 build/test/瀏覽器驗收。

### QC111 8D 原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 docs/modules/QC/8D原因基本資料.md、docs/DATABASE_CONNECTION.md 和本節。公司端已對照 frmReasonBasis_8D、兩個 Def Form、wsQC/clsQCBasis，完成 Vue DEV 雙 Grid 及 QC → 基本資料 → 8D 原因基本資料路由。
- [ ] 先以家用端管理連線比對 YMES live schema、constraints、indexes 及 migrations。來源 PostgreSQL 空表 CREATE 參考 database/reference/mestest_qc_8d_reason_create.sql；缺表/欄位才新增 forward migration，不要盲目執行或移除 YMES 擴充欄位。
- [ ] Oracle 原始 DDL 在 database/reference/mestest_qc_8d_reason_source_ddl.sql。兩表為 tblqcreasonbasis_8d、tblqcreasonsubtype_8d；各只有單一 PK unique index、沒有 FK。Subtype 的 ReasonType 為 numeric(1,0)，detail 的 ReasonType 為 varchar(50)；DataStamp nullable DEFAULT 0。維持來源型別，不要把兩表欄位型別強制統一。
- [ ] 實作 GET /api/qc/8d-reason-subtypes?search=，回傳 ReasonSubType 搜尋結果及四狀態資料；GET /api/qc/8d-reasons?reasonSubType= 必須按子類別精確查詢。ReasonType 值已由 Oracle TBLSYSVALUELIST 核實為 1 Men、2 Machine_8D、3 Material、4 Method、5 Environment；API/options 使用同代碼並從 YMES 字典提供繁中顯示名（人員、設備、物料、方法、環境）。
- [ ] 實作 POST/PUT/DELETE 子類別 API：新增 IssueState=0、DataStamp=0 並由伺服器設定 audit；PUT 僅允許 IssueState=0；DELETE 狀態 0/-1 實體刪除、狀態 2 改 -1、狀態 1 拒絕。PUT/DELETE/核准都需以 nullable DataStamp 做 CAS，版本衝突回 409。
- [ ] 實作 POST /api/qc/8d-reason-subtypes/{reasonSubType}/approve：查 Issue Job Map 的 ReasonSubType BASIS；沒有 Job 或 Job 無 Detail 時直接轉 2，有簽核明細則建立 Issue 流程並轉 1、通知第一簽核群組；Issue 核准轉 2、退回轉 0。回傳實際 IssueState/DataStamp；不可假設所有核准都立即 Active。
- [ ] 實作原因明細 POST/PUT/DELETE。只允許父子類別 IssueState=0；ReasonType 從父列取得；ReasonNo/ReasonName 必填，ReasonLevel 0-9；更新需 DataStamp CAS 與伺服器 audit。來源沒有 FK，刪子類別不 cascade 明細；WinForms/Core 實際留下孤兒明細，保持可見相容性並在 API/日誌標示。
- [ ] 驗證登入、QC111 權限、欄位長度、唯一鍵、ReasonType 有效選項、父列狀態與 DataStamp。主/明細批次刪除按舊 WinForms 逐筆執行，第一筆失敗停止並回報已完成筆數；不可宣稱整批交易回滾。
- [ ] API/schema 部署後更新本節與規格狀態、push，通知公司端 pull 接 Production API。公司端尚未執行 build/test/瀏覽器驗收。

### EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader 格式檔設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/EQP/設備AutoLoader格式檔設定.md`、`docs/DATABASE_CONNECTION.md` 及本節；WinForms Form/Designer/Service/Core 已查，Oracle DDL 由公司端既有受保護 PowerShell 5.1 + ODP.NET 連線唯讀取得，Vue DEV 頁與三層選單已完成。
- [ ] 先以家用端管理連線比對 YMES PostgreSQL 的 4 張表、索引、現有 migration 及擴充欄位。缺表/欄位才做 forward migration；reference CREATE 檔是空表定義，不能盲目套用或重建資料。
- [ ] Oracle 來源表為 `TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`。主表沒有 PK constraint，唯一索引 `(EquipmentNo,FileNameFormat)`；三個子表的複合 PK 如規格；四表沒有 FK。CLOB 轉 PostgreSQL text；`DataStamp` nullable DEFAULT 0。
- [ ] 若 YMES 缺少來源表，依 `database/reference/mestest_eqp_autoloader_create.sql` 建 migration；來源 Oracle DDL `database/reference/mestest_eqp_autoloader_source_ddl.sql` 僅供比對。不要在 MES Oracle 執行 CREATE。
- [ ] 實作 `GET /api/eqp/auto-loader-scripts?equipmentNo=&equipmentType=`。條件可空，equipmentNo/type 採 WinForms exact match；回主畫面所需設備欄位、格式腳本、稽核/DataStamp 及三類子明細。
- [ ] 實作設備 lookup `GET /api/eqp/auto-loader-scripts/options/equipment?prefix=&equipmentType=&fileNameFormat=`。lookup 沿用設備主檔、依登入者權限過濾；有 fileNameFormat 時回 `hasMatchingFormat` 供預設套用排除已存在設備。
- [ ] 從 WinForms `frmEquipmentAutoLoaderSetupDef`、測試配方 lookup Forms 與 `wsEQP` 追出 TestRecipe/Parameter 候選真正來源，再提供 `/options/test-recipes` 或沿用現有 YMES API。不要把 Vue DEV 選項當正式 ValueList。
- [ ] 實作 POST/PUT/DELETE `/api/eqp/auto-loader-scripts`，所有 main/detail 在同交易寫入；更新/刪除採 DataStamp CAS（舊 NULL 需 `IS NULL` 比較，修改後 `COALESCE(DataStamp,0)+1`），POST/PUT request 不接受 audit/DataStamp，伺服器建立 audit。
- [ ] 主列更新時區分 detail array 未提交與明確空陣列；刪除要在交易中先刪三張 detail，再刪 main。舊 Oracle 沒有 FK，不能依賴 cascade。
- [ ] `POST /api/eqp/auto-loader-scripts/{equipmentNo}/{fileNameFormat}/default?dataStamp=` body 接受多個目標設備，排除已有相同檔名格式的目標、不覆寫，主列/三種明細完整複製，Creator/CreateDate 重設；來源 DataStamp CAS。回 `{ createdEquipmentNos, existingEquipmentNos, invalidEquipmentNos }`，UI 顯示新增/略過筆數。
- [ ] API 必須承接 `ChkAutoLoaderSetup` 與 `ChkAutoLoaderSetup_Raw` 等價邏輯：Header 欄位、座標/IC Ingot 例外、動態參數/核准 Test Recipe、公式與 RawData 對照、NonNumeric DataType；不能信任前端生成的簡化 FormatScript。
- [ ] 實作功能授權、輸入長度/唯一鍵/設備參照驗證及 409 CAS 回應；部署 API/schema/migration 後通知公司端 pull 做 Production 對接。公司端頁未執行 build/test/瀏覽器驗收。

### MTL MLB01 來料 AutoLoader 格式檔設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/來料AutoLoader格式檔設定.md`、`docs/DATABASE_CONNECTION.md` 與本節；公司端已直接查 Oracle DDL、建立 Vue DEV 頁與 API handoff。
- [ ] 先從家用端 SSH/管理連線比對 YMES PostgreSQL live schema、constraints、index 和 migration。新表來源是 `MESTEST.TBLMTLVENDORALSCRIPT`；若缺表，參考 `database/reference/mestest_mtl_autoloader_create.sql` 建 forward migration；不要直接執行 reference SQL 或覆蓋 YMES 擴充欄位。
- [ ] Oracle 結構：PK `MTLFOLDERPATH varchar(400)`；`FORMATSCRIPT`/`RAWDATAFORMATSCRIPT` 是 CLOB；只有 PK index、無 FK。StoreRawData、SendMail NOT NULL DEFAULT 0；DataStamp nullable DEFAULT 0。來源 DDL：`database/reference/mestest_mtl_autoloader_source_ddl.sql`。
- [ ] 實作 `GET /api/material-vendor-autoloader-scripts?mtlFolderPath=&vendorNo=`；兩條件皆精確相等、空白不套條件、依 Folder Path 排序，回傳全部欄位及稽核值。主畫面啟動不自動查詢。
- [ ] 實作 POST/PUT/DELETE `/api/material-vendor-autoloader-scripts`：新增/修改稽核由伺服器設定；StoreRawData 固定為 0；新增/複製的 VendorNo 保持 NULL；Edit 不提供 VendorNo 修改；FormatScript/RawDataFormatScript 使用文字/CLOB 對應型別。
- [ ] PUT/DELETE 加入主鍵及 DataStamp optimistic concurrency；支援舊 nullable `DataStamp`（NULL 與 0 不可混為必然相同），成功修改遞增版本。WinForms Service 傳 DataStamp，但目前檢視的 Core method signature/UPDATE SQL 未看到版本檢查，請核對家用端實際引用的 Industry_Base 版本；規格記有差異。
- [ ] 實作受控 `GET /api/material-vendor-autoloader-scripts/directories` 只回傳伺服器允許目錄，不可讓瀏覽器任意瀏覽 MES Server 檔案系統。供應商選擇沿用 `GET /api/material-vendors`。
- [ ] 實作 Test Item Setup `GET /api/test-recipes?recipeType=MTL_AUTOLOADER`，及 `POST /api/material-vendor-autoloader-scripts/validate`；validate 由 API 承接舊 `Industry_Base.wsSYS.ChkAutoLoaderSetup`。UI DEV 目前僅 CSV/TXT 預覽與簡化檢查；Excel/NPOI、完整欄位轉換/parser 要由 API/後續設定介面補齊。
- [ ] 驗證登入者 `B_MTL AUTOLOADER SETUP` 權限、Folder Path 400、VendorNo 20、Description 255、separators 10、欄位序號、唯一鍵、DataStamp 衝突；逐筆 Delete 應在第一個失敗時停止。此功能無 IssueState/簽核、無 FK，不新增 audit table。
- [ ] 比對並部署 schema/migration/API 後更新規格與本清單，再 push；通知公司端 pull Production API 契約。

### MTL ML104 物料群組資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料群組資料.md`、公司端 ML104 區塊及 `docs/DATABASE_CONNECTION.md`。WinForms Form/Service/Core、DEV UI 與兩張表來源已核對。
- [ ] 先比對 PostgreSQL live schema、欄位/constraints/indexes 與 migrations。`tblmtlmaterialgroupbasis`、`tblmtlmaterialgroupdetail` 已出現在既有物料 0013/first-phase reference；若 YMES schema 已有相符結構就沿用。缺主檔才用 `database/reference/mestest_mtl_material_group_create.sql`，缺明細才依 `database/reference/mestest_mtl_master_create.sql` 的 detail 定義建 forward migration；不得盲跑 `IF NOT EXISTS` 或覆蓋 YMES 擴充欄位。來源確認 `materialtype NOT NULL`、`datastamp DEFAULT 0` nullable；既有 schema 差異需用 forward migration 修正。
- [ ] 實作 `GET /api/material-groups?groupNo=`、`POST /api/material-groups`、`PUT /api/material-groups/{groupNo}?dataStamp=...`、`DELETE /api/material-groups/{groupNo}?dataStamp=...`。清單不使用 IssueState；編輯只更新 Description；新增/修改稽核由伺服器設定；DataStamp nullable 必須支援 `dataStampIsNull=true` CAS。
- [ ] 實作 `GET /api/material-groups/{groupNo}/materials` 與 `PUT /api/material-groups/{groupNo}/materials`，body `{materialNos:[...]}` 是整組取代，空陣列清空。以單一交易刪除舊明細、寫入新明細、事件記錄；刪主檔亦須同交易刪明細後刪 header。驗證新增成員存在、MaterialType 相符且 IssueState=2；既有歷史成員若後來不再核准，保持原狀時允許保存，移除後不可重新加入。
- [ ] MaterialType picker 使用 `GET /api/material-types`（不帶 IssueState，對應 WinForms `LoadMaterialType` 全清單）；左側候選物料使用 `GET /api/materials?issueState=2&materialType=` 精確條件。檢查既有 endpoint 的篩選和資料權限。
- [ ] 驗證登入者的 `MTL GROUP` 權限、GroupNo/MaterialType/Description 長度與必填、唯一鍵、DataStamp 衝突；此功能無核准 endpoint。沿用現有伺服器 event log，不建新表。
- [ ] API/schema/migration 部署後更新規格及本清單並 push；通知公司端 pull 最新版接 Production API。公司端 `/YMES/MTL/MaterialGroups` 已完成 DEV 頁。

### MTL ML107 物料標籤對應設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料標籤對應設定.md`、公司端 ML107 進度及本節。WinForms Form/Designer、Service/Core、Oracle 結構、Vue DEV 頁與 API 契約已整理。
- [ ] 以家用端 PPK/SSH 比對 YMES live schema 和 migrations。主表 `tblmtllabelmap` 若缺少，依 `database/reference/mestest_mtl_label_map_create.sql` 新增 forward migration；標籤 lookup `tblprdlabelbasis` 若缺少，才依 `database/reference/mestest_prd_label_basis_lookup_create.sql` 建立。若已存在，核對欄位、NULL/default、PK/index 並保留 YMES 擴充欄位；不可盲跑 reference SQL。
- [ ] 實作 `GET /api/material-label-maps?materialType=&materialNo=&labelNo=`，三條件以參數化 contains/LIKE 篩選，主查詢沿用舊 Core 的 `MaterialNo IS NOT NULL`（不加 LabelSource 條件），回傳物料/類別名稱、LabelSource、DataStamp、稽核欄，依 MaterialType/MaterialNo/LabelType 排序。
- [ ] 實作 `GET /api/product-labels?labelSource=5` 讀標籤目錄；LabelSource 必須精確等於 5，回傳 LabelType、LabelNo、FormatFileName、Description、PrinterNo。另沿用 `GET /api/material-types?issueState=2` 及 `GET /api/materials?issueState=2&materialType={exact}` 選擇器；物料 lookup 支援虛擬 `MaterialNo='*'`，不可要求該值存在於物料表。
- [ ] 實作 `POST /api/material-label-maps`：必填 MaterialType/MaterialNo/LabelType/LabelNo、來源固定為 5、長度符合 DDL、API 端設定 Creator/CreateDate、DataStamp 初值 0；複合 PK 衝突回 409。舊 WinForms 可手動輸入 LabelNo，沒有 label-map FK，因此不要要求手打的 LabelNo 必須存在於 label template 表。
- [ ] 實作 `PUT /api/material-label-maps/{materialType}/{materialNo}/{labelType}?dataStamp=`：只更新 LabelNo；WHERE 限定 LabelSource=5 與舊 DataStamp，成功更新 Reviser/ReviseDate 並遞增 DataStamp；nullable DataStamp 需支援 `dataStamp=0&dataStampIsNull=true` 並用 `IS NULL` CAS。版本衝突/不存在回 409/404。實體刪除依複合 UI key 加 LabelSource=5；主 Grid 多筆刪除仍逐筆執行、遇錯停止並回報部分成功。
- [ ] 所有端點檢查登入與 FunctionNo `MTL LABEL MAP BASIS` 權限；不加入 IssueState/核准流程、不新增 Oracle FK 或事件資料表。完成 schema/API 部署後更新規格與 migration index，push 並通知公司端 pull Production API。
- [ ] 公司端頁面 `/YMES/MTL/MaterialLabelMaps` 已建立 DEV UI；Production API 未部署前不得以 localStorage 充當正式資料。

### MTL ML101 物料供應商基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料供應商基本資料.md`、`docs/DATABASE_CONNECTION.md` 新增段落及本檔公司端進度；WinForms Form/Service、API 契約、Issue 狀態與刪除交易均已記錄。
- [ ] 依 `database/reference/mestest_mtl_material_vendor_source_ddl.sql` 與 `mestest_mtl_material_vendor_create.sql`，透過家用端 PPK/SSH 先比對 YMES schema/migrations，再建立 forward migration；不得在 Oracle 執行 CREATE SQL，也不得移除 YMES 擴充欄位。
- [ ] 實作 `/api/material-vendors` 及巢狀 `/contactors` API：搜尋/四狀態、CRUD、DataStamp CAS（NULL 版本用 `dataStampIsNull=true`）、批次逐筆刪除、已核准軟刪除、核准 Issue/Job 流程及 contactor 交易；登入及 MTL ML101 權限必須在 API 驗證。
- [ ] 主檔實體刪除須同交易刪除 `TBLMTLMATERIALVENDORCONTACTOR`、`TBLMTLMATERIALVENDORMAP` 對應資料，再刪主檔；供應商名稱查詢需依 WinForms 舊碼使用 MTL vendor 表。核對並修正 `src/YMes.Api/MasterData/Data/PostgresMasterDataRepository.MaterialFeatures.cs` 目前把 material vendor map join 到 `tbleqpvendor` 的來源錯誤。
- [ ] 對照規格確認 PostgreSQL 欄位長度、NULL/default、主鍵、API DTO 與 Vue 路徑 `/YMES/MTL/MaterialVendors`；完成後更新規格與 migration/API 部署狀態並 push，通知公司端 pull Production endpoint。

### MTL ML102 物料類別資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料類別資料.md`、WinForms Form/Service 規則及現有 MTL API。此功能的 `TBLMTLMATERIALTYPE`、`TBLMTLMATERIALPROPERTY`、`TBLMTLTYPEQCITEM`、`TBLMTLTYPEACCLIMATION` Oracle DDL/空表 CREATE SQL 已在 `mestest_mtl_master_*` reference，並由 0013 建置；先以 PPK/SSH 核對當前 YMES schema/migration，缺表才補 forward migration，不要重建或覆蓋擴充欄位。
- [ ] 實作 `/api/material-types` CRUD/搜尋/四狀態/核准與 DataStamp CAS；新增檢查 `MaterialType` 不可與 `tblmtlmaterialbasis.materialno` 重複；實體刪除在單一交易移除類別屬性、`tblmtltypeqcitem`、`tblmtltypeacclimation` 與主檔，已核准軟刪除為 -1，簽核中拒絕。
- [ ] Property 子功能以 Material=`MaterialType` 操作共用 `tblmtlmaterialproperty`，補 CRUD/MTL PROPERTY Issue 核准及 CAS；不要把 MaterialType 屬性和一般 MaterialNo 記錄混在同一查詢結果。
- [ ] GumHour 以 `tblmtltypeacclimation` 的 `(MaterialType, MaterialNo='*')` 讀寫類別級設定；套用 ACCMin/Max、回冰/ReForzenTimes、再次回溫欄位驗證與 CAS。核准前從系統參數取得 `GumMaterialType`、`MATERIALMULTITIMECTRL`，若屬固晶膠但未設定 GumHour 則拒絕核准；重用 `MTL TYPE` Issue/Job 流程及第一簽核群組通知。
- [ ] 新增 `POST /api/material-types/{sourceMaterialType}/copy`，以單一交易複製類別資料及來源的類別級 GumHour（若存在），重設 IssueState/audit/DataStamp；不複製 Property/QC 明細。GumHour 端點須支援 GET/POST/PUT，新增時 INSERT、更新時 CAS。
- [ ] 完成並部署後更新規格、migration/API 狀態與本待辦，push 後通知公司端 pull。公司端 `/YMES/MTL/MaterialTypes` DEV 頁已建立。

### MTL ML105 物料類別品管項目設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料類別品管項目設定.md`，核對 `TBLMTLTYPEQCITEM`、`TBLMTLQCITEM` 已在 MTL 0013/schema；Oracle DDL/PG CREATE 已在 `mestest_mtl_master_*` reference，沒有新表需建立。
- [ ] 實作 `GET /api/material-types?materialType=`（不帶 IssueState 時回傳全部狀態，MaterialType 前綴搜尋）及 `GET /api/material-types/{materialType}/qc-items`，回傳三種 QC 候選目錄與已指派項目；候選需沿用 QC Attributes、Variables、Group 舊目錄服務。
- [ ] 實作 `PUT /api/material-types/{materialType}/qc-items`，以單次 transaction 取代 Type QC 範本、同步該類別所有物料的 `TBLMTLQCITEM`，依舊程式補齊 `TBLOPQCITEM` 的 `OPNo='IQC'` 項目及異動紀錄；驗證項目存在/唯一性/型別及 MTL ML105 權限。
- [ ] 完成 API 後更新規格與家用待辦、push 並通知公司端接 Production endpoint；公司端 `/YMES/MTL/MaterialTypeQCItems` DEV 頁已建立。

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


## MTL ML106 物料品管項目設定（公司端 2026-11-17 交接）

- [x] 2026-11-17 遠端比對：`TBLMTLQCITEM` 欄位/複合 PK 符合 reference；三個 QC catalog、`TBLQCITEMBINDLOG`、`TBLOPQCITEM` 缺表。備份 `/opt/ymes/backups/ymes_before_0019_mtl_qc_20261117.dump` 已由 `pg_restore --list` 驗證；0019 migration 已套用，`ymes_app` 取得目錄 SELECT、IQC mapping INSERT、bind log INSERT 權限。
- [x] 擴充 `GET /api/materials` 的 `materialNoPrefix`，新增 ML106 專用 `GET /api/materials/{no}/qc-item-library`，保留既有 `/qc-items` 類別範本回應。
- [x] PUT 在單一交易驗證已核准 QC 目錄、替換直接綁定、補 `IQC` 虛擬站項目、寫 BindType=2 新增/移除事件及 server audit；依來源 WinForms 行為不限制物料 IssueState。
- [x] .NET 9 API Release build 0 warning/0 error；只重建 `ymes-api`，遠端 readiness 200 且容器 healthy，`ymes-web` 與其他容器未重啟。
- [ ] app-browser 直接開 `/YMES/MTL/MaterialQCItems` 後顯示主選單；尚未確認是否是 Production Web 尚未更新或功能授權/路由狀態，留給公司端核對 UI route。

公司端頁面 `/YMES/MTL/MaterialQCItems` 已完成 Vue DEV UI；WinForms Form/Designer、wsMTL、clsMTLBasis、clsQCBasis 已對照。Production schema/API 已由家用端完成並部署；Production UI route 待公司端查明。

### Schema / DDL

- Oracle DDL 由公司端 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過既有受保護連線唯讀查詢 MESTEST，DDL 檔：database/reference/mestest_mtl_qc_item_tables_source_ddl.sql。連線秘密仍存既有保護設定，不複製到 repo；不要再問使用者 IP/帳密，也不要在 Oracle 執行 CREATE。
- PostgreSQL 空表參考檔：database/reference/mestest_mtl_qc_item_tables_create.sql，含 tblqcitemforattributes、tblqcitemforvariables、tblqcitemgroupbasis、tblqcitembindlog。先查 YMES PostgreSQL 實際 schema/table/columns/indexes 和 migration；若符合就沿用，缺表才加 migration，缺欄位/約束才 forward migration。不要盲目套用 IF NOT EXISTS。
- TBLMTLQCITEM 在 mestest_mtl_master_create.sql；TBLOPQCITEM 在 mestest_operation_and_system_create.sql。這兩張先比對現有 schema，不要重建。
- 四張新查 DDL 表各只有 Oracle DDL 列出的 PK，沒有 FK 或其他 index。Oracle DATE 用 timestamp without time zone，NUMBER 精度及 nullable/default 按 source DDL 保留。
- 授予 ymes_app 本功能必要 SELECT/INSERT/UPDATE/DELETE 權限；避免為查目錄或寫 log 開放多餘權限。migration 前照既有家用端流程備份並留下部署紀錄。

### API / Service

1. 擴充物料清單 GET /api/materials 支援選填 materialType 精確條件及 materialNoPrefix 前綴條件。ML106 的查詢不限制 IssueState；materialType/materialNoPrefix 皆空時 API 需回傳完整清單以供 WinForms 等價 MaterialNo 查找視窗使用，Vue 主畫面 Query 按鈕自行要求至少一個條件。保留既有 keyword、issueState 呼叫行為。
2. 新增 GET /api/materials/{no}/qc-item-library，回傳 { available, assigned }：available 以 IssueState=2 分別讀三個 QC catalog，輸出 qcItemNo/qcItemName/qcItemType（0 Attributes、1 Variables、2 Group）；assigned 讀 tblmtlqcitem 並依型別左 Join 正確 catalog 取得名稱，保留無法 Join 的舊綁定供移除，輸出 reviser/reviseDate。
3. 保持既有 GET /api/materials/{no}/qc-items 的 type-template 行為，因 MaterialFeatureDialog.vue 依賴它；不可把其 available 換成 catalog 全表。前端新增的 library GET 路徑已獨立。
4. 既有 PUT /api/materials/{no}/qc-items 接受 {items:[{qcItemNo,qcItemType}]} 並寫同一張 tblmtlqcitem。新增來源目錄驗證、完整交易及舊 WinForms 副作用：替換物料現有綁定；對各新 QC Item 用 INSERT ... SELECT/等價原子方式確保 tblopqcitem 的 opno='IQC'、inline=0、executeorder=0 存在且不重複；寫 tblqcitembindlog，BindType=2，刪除事件 ExecuteStatus=2、新增事件 ExecuteStatus=0，記錄 MTLNo、QCItemType/No、ExecuteDate、Executor。QCItemLogSerial 由伺服器產生唯一值，不建 Oracle sequence。
5. Audit 由登入者與伺服器時間設定；同一 transaction 更新 assignment、IQC 關聯與 bind log。先核對目前 API 的 IssueState=0 寫入限制是否符合舊 WinForms；若為既有 MaterialFeatureDialog 必需，保持既有契約且明確處理兩種 UI 的合法狀態，不可意外禁止舊資料修正。
6. TBLQCITEMBINDLOG 在 YMES 若缺少才使用 reference SQL 建表；如現有 schema 有擴充，保留它並用 forward migration 對齊必要欄位。其複合 PK 順序為 (QCITEMNO, BINDTYPE, QCITEMLOGSERIAL)，不能自行改成其他順序。
7. 完成 migration/API/權限後部署，更新 API 文件、migration index、本待辦；通知公司端 pull。公司端會對照 ML106 頁面核對 response、錯誤、全狀態搜尋與儲存副作用。

## 2026-11-17 ML103 物料基本資料 API 選項交接

- ML103 主資料、屬性、QC、供應商、GumHour 等 Oracle DDL/PG CREATE 已在 database/reference/mestest_mtl_master_* 建立並由 0013 migration 比對/部署；本次公司端沒有發現新表，不新增 SQL 或 migration。
- 公司端 ML103 多選核准/刪除會對每筆資料依序呼叫既有 POST /api/materials/{no}/approve 與 DELETE /api/materials/{no}?issueState=...&dataStamp=...；不需要額外批次 API。請確認現有單筆 API 的簽核檢查、引用檢查、DataStamp、稽核與狀態轉換和 WinForms 相同。已檢視 PostgresMasterDataRepository.ApproveMaterial：目前有 Controlled=1、DataStamp、IssueState 檢查，但缺少 WinForms 的條件式 GumHour 設定檢查；需在服務端補上。
- GET /api/materials/lookups/options 現況由 MaterialFeaturesController.Options() 回傳 countWay=[]、checkLotNo=[]，且沒有 MATERIALMULTITIMECTRL / GumMaterialType。請從公司端 WinForms ValueListProvider.GetValueList("TBLMTLMATERIALBASIS", ...) 與 wsSYS.LoadParameter 核實真實值，不要以 DEV 樣本猜測；將 CountWay、CheckLotNo 顯示選項與 GumHour 參數設定加入 DTO/API response。若參數已有 YMES 系統設定來源，沿用既有結構，不新建設定表。
- 完成後更新 MaterialOptionsDto、API 文件與本待辦。公司端 UI 已能讀取選項；多次回溫參數未提供時 Production 會隱藏 GumHour，避免對非適用物料開啟設定。


## QC203 計數品管項目判別法則對應（公司端 UI / API handoff，2026-11-17）

先讀 docs/modules/QC/計數品管項目判別法則對應.md、docs/DATABASE_CONNECTION.md 與本節。公司端已完成 Vue DEV UI /YMES/QC/AttributeRuleMap；Production API、YMES migration、schema 比對及部署尚未完成。FunctionNo=QCITEM_A RULE MAP，FunctionKey=QC203，IssueNo=QCITEM_A RULE MAP。

### Oracle DDL / PostgreSQL schema

- 公司端以 PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護的 MES Oracle 設定直連 MESTEST，只查 DBMS_METADATA/ALL_TABLES/index metadata，沒有查業務列或執行 Oracle DDL。Oracle source DDL：database/reference/mestest_qc_attribute_rule_map_source_ddl.sql；同一批表亦在 mestest_qc_item_attributes_source_ddl.sql。
- 三張來源表為 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER。來源 PK 分別為 QCAttSPSerial、(QCAttSPSerial,RetestLot)、(QCAttSPSerial,RetestLot,ParameterNo)；主表另有非唯一 QCItemNo index；三表沒有 Oracle FK。
- PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_attribute_rule_map_create.sql。先比對 YMES live schema、已部署 migration、已存在的共享 ID/sequence service，再決定 forward migration；不可直接執行 reference 取代已有表。
- Oracle 主表使用動態 FactorNo 欄位；CREATE 參考以 factorvalues JSONB 適配。API 只能接受該 QCItem 已核准 FactorOnline=Y 的 FactorNo 白名單，不能將傳入名稱拼成 SQL 欄位。精確重複組合由 (qcitemno,factorvalues) 防重；來源其他非該 QCItem 因子欄語義為 *。
- Oracle 用 TBLSYSSERIALNO 的 QCAttSPSerial 產生 SP 加 8 位數 serial。CREATE reference 內的 qcattspserial_seq 是建議 ID 來源，先確認 live YMES 的共享 serial service 與既有 serial 最大值；禁止不檢查就從 1 使用。來源沒有 DataStamp、Reviser、ReviseDate；不要冒稱為 Oracle 欄位。若 YMES 新增版本欄位，必須另做 schema 適配說明。

### API / transaction

1. 全部端點驗證已登入、QC203 權限、資料狀態、欄位長度、已核准 lookup 來源及 factor whitelist；使用參數化 SQL。因舊服務以多句 SQL 全量替換 Detail/Parameter，新增、修改、刪除必須在同一交易原子完成。
2. GET /api/qc/attribute-rule-maps?qcItemNo=&issueState= 回傳主 Grid rows：qcAttSpSerial、qcItemNo、issueState、creator、createDate、factorValues、canEdit。canEdit 依來源 ExeIssueControl/TBLSYSISSUECONTROL 規則計算，供 Vue 表單決定唯讀狀態。
3. GET /api/qc/attribute-rule-maps/{qcAttSpSerial} 回傳 { basis, details, parameters }；Detail 依 RetestLot 排序，Parameter 按 RetestLot/ParameterNo 排序。
4. GET /api/qc/attribute-rule-maps/options?qcItemNo= 回傳 qcItemRuleNo、componentCollect、已核准屬性法則、一般與元件抽樣計劃、FactorOnline 因子選項與 MES Parameter Reference 值、planParameters 對應表。一般 PlanNo 取已核准 TBLQCSAMPLINGPLAN；ComponentCollect 非 0 取已核准 TBLQCSAMPLINGPLAN_WAFER。法則取已核准 TBLQCATTRULEBASIS，依 ComponentCollect 0/1 分組。每種值的正式來源先核 WinForms/既有 YMES API，不要沿用 DEV 樣本。
5. POST /api/qc/attribute-rule-maps body { qcItemNo, factorValues, details:[{retestLot,planNo,lotRuleNo,comRuleNo}], parameters:[{retestLot,parameterNo,parameterValue}] }。伺服器生成 SP########、IssueState=0；QC Item 必須已核准且 RuleNo 不可空，FactorOnline 至少一項，所有要因值必填；Detail 至少一筆；每筆 Detail 必填 PlanNo 且至少一筆 Parameter。ComponentCollect=0 且 PlanType=2 的 AQL 可以不填 LotRuleNo；其他一般 Plan 必填 LotRuleNo；元件模式至少填 LotRuleNo 或 ComRuleNo。驗證 RetestLot unique、同 RetestLot ParameterNo unique。對全部既有狀態做來源等價重複檢查；資料列/所有因子內容必須有相同語義才算精確 duplicate。序號需安全並行生成。
6. PUT /api/qc/attribute-rule-maps/{qcAttSpSerial} 的 Vue request 僅送 details/parameters；API 不允許改 qcItemNo/factorValues/serial。依 ExeIssueControl：IssueState=0 可編輯；IssueState=1 唯讀；IssueState=2/-1 依 TBLSYSISSUECONTROL，無設定視為唯讀。交易內先驗證狀態，再替換所有 detail/parameter，保留 IssueState。來源 Core 會保留原 Creator 並將 CreateDate 更新為伺服器目前時間；若採正規 Reviser audit 或 CAS 是 YMES 行為改善，須在 API 文件記為適配，不得宣稱來源有這些欄位。
7. DELETE /api/qc/attribute-rule-maps/{qcAttSpSerial}?issueState= 只接受來源允許刪除的 0、-1、2，Pending=1 拒絕。舊 Core 刪 Basis 和 Detail 卻漏刪 Parameter；新 API 應在一個交易刪三表，並以實際資料狀態/CAS 防止競態。Vue 多選逐筆呼叫，首錯停止並回報已完成筆數。
8. POST /api/qc/attribute-rule-maps/{qcAttSpSerial}/approve?issueState=：接共用 Issue/Job，IssueNo=QCITEM_A RULE MAP；沒有 Job/Detail 時直接 2，有流程先 1，核准 2、退回 0，依既有 service 寫事件並通知第一簽核群組。WinForms UI 對 0/-1 都呼叫服務，但舊 wsQC 僅在傳入 0 時執行；-1 實際是成功 no-op。API 應拒絕 -1 或回報未變更，不能表示已核准。
9. UI options/lookup 必須回正式資料：GET /api/qc/attribute-qc-items?issueState=2、GET /api/qc/attribute-qc-items/{qcItemNo}/factors?factorOnline=Y（QC201 已有），以及 QC203 options 提供 MES parameter reference、已核准 plans/rules/plan parameters。任何 API 不可將用戶端檔案路徑或因子名稱當 SQL。
10. 先完成 live schema 比對與有資料備份，再採 forward migration；保留現有擴充欄位和資料。部署 API/schema 後更新 API/migration 文件、本 TODO 並 push，通知公司端 pull 以 Production 驗收主/Detail/Parameter、Issue Job 與錯誤狀態。


## QC202 計數管制內容（公司端 UI handoff，2026-11-17）

- 公司端已依 WinForms frmSPCAttLimit / frmSPCAttLimitDef、wsQC、clsQCBasis / clsBasis_UPD 建立 Vue DEV 頁 /YMES/QC/AttributeControlLimit，三層選單為「品質資料 → 品管設定 → 計數管制內容」。主 Grid 固定載入 IssueState=2 QC Item；管制內容可動態列出 FactorOnline 欄位，支援新增/修改/刪除/複製、規則集、界限、Apply/Confirm、多選刪除。
- Oracle TBLSPCATTLIMIT DDL 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直接查 MESTEST 後，納入 QC201 DDL 擷取；單表 reference 為 database/reference/mestest_qc_spc_att_limit_source_ddl.sql。PG schema 參考 database/reference/mestest_qc_spc_att_limit_create.sql 以 factorvalues JSONB 取代動態欄位，先核對 YMES live schema/migrations 再決定 migration，不可照 Oracle 動態欄位實作任意 ALTER。
- API：GET api/qc/attribute-control-limits?qcItemNo=、GET .../options?qcItemNo=、POST collection、PUT /{qcItemNo} body 帶 originalFactorValues/dataStamp、DELETE /{qcItemNo} body 帶 factorValues/dataStamp。完整 DTO、FactorOnline 白名單、RuleSet 格式、nullable DataStamp CAS 見 docs/modules/QC/計數管制內容.md。
- 因子沿用既有 QC201 Factor API，查找值沿用 MES parameter reference；法則從 TBLQCRULEBASIS 讀 IssueState=2。API 所有端點需檢查登入與 FunctionKey QC202，SQL 參數化；新增以 QCItemNo + 正規化 factorValues 防重，修改/刪除以 composite key + DataStamp CAS，NULL DataStamp 使用 IS NULL。勿把 FactorNo 直接拼成 PostgreSQL 欄位名。
- TBLSPCATTLIMIT Oracle source 無 PK/FK/唯一鍵，只有 QCITEMNO index；WinForms 客戶端新增時先查重。PG CREATE reference 加入 composite unique constraint 作 server-side 防重，屬 YMES 適配，請在實作前確認與既有架構相容。


## QC204 刪除計數型品管項目量測記錄（公司端 UI handoff，2026-11-17）

先讀 `docs/modules/QC/刪除計數型品管項目量測記錄.md`。公司端已完成 Vue DEV 頁 `/YMES/QC/AttributeMeasurementDelete`。FunctionNo=`QCITEM_ADELETE`、FunctionKey=`QC204`；此為刪除量測日誌操作，不是刪除 QC Item 主檔。

### YMES schema / DDL

- QC204 不新增資料表。Oracle reference `database/reference/mestest_qc_item_attributes_source_ddl.sql` 已包含 `TBLQCITEMFORATTRIBUTES`、`TBLSPCATTBASISLOG`、`TBLSPCATTDETAILLOG`、`TBLSPCATTEDITLOG`、`TBLSPCATTDETAILLOG_TAPE` 的來源 DDL/索引。
- 先查 YMES PostgreSQL 現有 schema 和 migration，確認 QC201 日誌 API 所用表名/欄位。保留已部署 schema 與資料；只有確定缺表時才依既有 Oracle source DDL 做 YMES forward migration/Create SQL 並記錄適配，勿在公司端臆造或重複建表。

### API

1. Vue 清單依序讀取 `GET /api/qc/attribute-qc-items?issueState=-1`、`GET /api/qc/attribute-qc-items?issueState=0`，依 WinForms 順序合併；QC201 list DTO 必須包含畫面欄位、`FormName`、`IssueState`。目前 API 專案若尚未實作 QC201 list，需一併完成此依賴。
2. 新增 `POST /api/qc/attribute-qc-items/{qcItemNo}/measurement-log-deletions`，body `{ issueState }`；成功回傳 QCItemNo、刪除表/筆數。端點檢查登入、FunctionKey QC204 權限、QC Item 存在、真實 IssueState 與請求一致且限 0/-1；不得只依賴前端資料。
3. 以資料庫讀到的 FormName 判斷是否為 `frmAtt_OnlineControlMeasureDataForTape`。不得接受用戶端的 FormName/table name/SQL。
4. 先確認 `TBLSPCATTBASISLOG` 有該 QCItemNo 資料；無資料回 404/409、使用者可讀錯誤。存在時，在單一 transaction 刪除 BasisLog、DetailLog、EditLog；Tape Form 另刪 Tape DetailLog。所有條件參數化、整筆 operation rollback on error。
5. 相容舊 Core：`Log=Log` 模式不刪 QC Item/Parameter/Factor/Reason/Group 或其他設定，並略過一般 OP/WIP 使用檢查。業務目標是刪除量測日誌；不得擴大刪除範圍。
6. 舊 Core 在成功刪除後丟 `0000-000001`，舊 WebService 包成失敗。本 API 應回 2xx 成功，不複製此錯誤包裝；無資料回錯誤。多選由 Vue 逐筆依清單順序呼叫，首錯停止並回報已完成筆數。
7. 成功/失敗均記錄目前登入者、QCItemNo、時間、電腦/請求識別與實際刪除表筆數。避免日誌內容或連線設定被寫入應用 log。

### 完成後

- 更新 QC204 API、schema/migration 文件及本 TODO；若需要新增 migration，列出已比對的 live table，保留既有欄位/索引。
- Push 到 GitHub main，通知公司端 pull 後以 DEV UI 接 Production API 驗收：0/-1 狀態、一般日誌、Tape 特例、無記錄、transaction 失敗回復、多選中途錯誤與權限拒絕。
- 公司端已完成 QC205 Vue DEV 頁並交付家用端 API/schema 工作，詳細清單見本檔「QC205 計量品管項目 API/schema handoff」。
## QC205 計量品管項目 API/schema handoff（公司端 2026-11-17）

公司端已完成 `/YMES/QC/VariableQcItem` Vue DEV 畫面、三層選單、WinForms/Service/Core 規格與 Oracle metadata 參考。先 pull 最新 main，再閱讀 `docs/modules/QC/計量品管項目.md`、`docs/DATABASE_CONNECTION.md`、本段與 Create SQL；此段不是部署授權，也不代表 live schema 已確認。

### Schema / Create SQL

- Oracle source DDL 僅供參照：`database/reference/mestest_qc205_theil_custom_source_ddl.sql`、`mestest_qc205_variable_dependencies_source_ddl.sql`。MESTEST 沒有 `TBLSPCQCHARTDETAIL`、`TBLSPCQCHARTDETAILLOG`、`TBLSPCQCHARTRULE`、`TBLSPCQCHARTBASIS` 四表；追查 YMES 等價表/實際程式路徑，勿直接建立四張替代表。
- QC205 主表 `TBLQCITEMFORVARIABLES` 的 Oracle/PG 結構沿用 `mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_mtl_qc_item_tables_create.sql`；Parameter、Factor、SamplePlanChange 沿用 QC201 `mestest_qc_item_attributes_source_ddl.sql`、`mestest_qc_item_attributes_create.sql`。先檢查 live PostgreSQL schema、已部署 migration 與 API DTO，禁止重複建表或覆蓋既有資料。
- THEIL Memo 的 Oracle 原表是 `XX_TBLQCITEM_MEMO`；PG CREATE 參考在 `database/reference/mestest_qc205_theil_custom_create.sql`，含 YMES composite PK 適配。若 live schema 尚無等價 memo mapping，再新增 forward migration 和 rollback；保留 `qcitemtype=0` 的來源行為。
- `XX_TBLSPC_TEMPDATA_VAR` 與 `TBLSPCVARDETAILLOG` Oracle 欄位含動態 ParameterNo 欄名；不要在 PostgreSQL 動態 ALTER 欄位。依既有量測 log API/schema 以 JSONB 或正規化子表承接；先查目前 schema 再決定需要的遷移。
- 記錄 live schema 對照結果與 migration 使用的既有表/欄位。Create reference 不能未比對就直接當 production migration 執行；備份、部署遵循既有家用端流程。

### API 工作

1. 新增登入保護的 QC205 API controller/service/DTO，路徑與 payload 依 `docs/modules/QC/計量品管項目.md`。Vue 使用的端點包含列表/options、主檔 CRUD/Copy/Delete/Approve、Parameter/Factor/OP/SamplePlanChange/Memo 子資源及 Simulation。
2. 實作 `POST /api/qc/variable-qc-items/copy-preview`，回傳 QCItem Parameter 與目標 Sampling Plan Parameter；若兩者不同，前端以 `parameterSource` 明確選擇，取消不建立資料。Copy 主表與已勾選子資料須同一 transaction；未複製 Parameter 時以新 Plan 初始化參數。
3. 狀態更新須用資料庫真實狀態及 DataStamp/CAS。QCItemNo 跨計數/計量/群組衝突檢查；Validate min/max 同時限制上下界。Approve 先檢查 Factor，IssueNo=`QCITEM_V` 接共用 Issue/Job 流程，依有無 Job/Detail 決定 1 或 2；不得直接把所有核准狀態寫成 2。
4. DELETE：IssueState=2 改成 -1；0/-1 僅在無量測、WIP、材料/OP/產品/群組使用時交易式刪除主檔及相關子資料，另清理 Memo mapping。先追查既有 YMES 正規關聯，不可照搬四張 MESTEST 缺表名稱。
5. Memo GET 需回傳已核准 ParameterClass=1 清單加目前勾選；PUT 接 `{qcItemType:0,parameterNos}`，驗證 Parameter 清單並交易式全量替換 mapping。ParameterNo 僅為資料值，不得拼接成 SQL 欄名或執行動態 ALTER。
6. Simulation 只回預覽結果、不寫量測日誌；所有 SQL parameterized，audit 使用登入者；端點遵循專案目前登入/權限約定。先在 API 文件列出 YMES 與舊 WinForms 行為差異，再實作。
7. 更新 API/schema/migration 文件與本 TODO；commit/push 後通知公司端 pull，以 QC205 Production API 驗收。公司端此頁尚未執行 build、測試或瀏覽器驗收。
## QC209 計量品管項目管制法則對應 API/schema handoff（公司端 2026-11-17）

公司端已完成 Vue DEV 頁與 WinForms 規格。請 pull 最新 main，先讀 `docs/modules/QC/計量品管項目管制法則對應.md`、`docs/DATABASE_CONNECTION.md` 與本段。公司端沒有連 YMES PostgreSQL；本段提供 Create 參考，不表示 live schema 已比對或 migration 已套用。

### Schema / Create SQL

- Oracle `TBLQCITEMRULE` source DDL 已在 `database/reference/mestest_qc205_variable_dependencies_source_ddl.sql`，包含主鍵 `(QCITEMNO,SPCID,RULENO)` 及 `EQUIPMENTSTATE`、`ERFSTATE`、`EMAILSTATE` 三個 nullable 欄位；由公司端前次 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得。Oracle 無 FK。
- `TBLQCRULEBASIS` Oracle DDL 與 YMES Create 參考已在 QC108 files：`mestest_qc_variable_rule_basis_source_ddl.sql`、`mestest_qc_variable_rule_basis_create.sql`。QC Item variables DDL/CREATE 沿用 QC205。
- `database/reference/mestest_qc209_variable_item_rule_map_create.sql` 是本次新增 PostgreSQL CREATE 參考。先查 live schema/migrations：若 `tblqcitemrule` 已存在，勿重建或覆寫資料；只有確認缺表才設計 forward migration/rollback。保留三個 nullable source 欄位及複合鍵，不新增 Oracle 沒有的 FK。
- 舊 Core `UpdQCItemRule` 以 QCItemNo+SPCID 刪除整組後重插四欄；未寫的 EquipmentState/ERFState/EmailState 會變 NULL。記錄這個 legacy 副作用；migration 不得漏掉來源欄位。

### API 工作

1. `GET /api/qc/variable-qc-items?issueState=2`：沿用 QC205 API 為 lookup 提供 QCItemNo/Name/ChartType/IssueState。
2. `GET /api/qc/rule-bases`：沿用 QC108 法則 API；候選加入清單只允許 IssueState=2。GET mapping 仍須回傳已指派但目前非核准/已刪除的規則，避免使用者儲存其他規則時誤清除既有對應。
3. `GET /api/qc/variable-item-rules/options` 回傳 `{waitEnabled}`；伺服器依企業設定，SPK=false。
4. `GET /api/qc/variable-item-rules/{qcItemNo}` 回傳所有 SPCID 對應及 RuleName。SPCID 顯示 0 Trend、1 Xbar、2 R、3 S、4 Rm；圖表可選組合依 QC Item ChartType，詳功能規格。
5. `PUT /api/qc/variable-item-rules/{qcItemNo}/{spcId}` body `{rules:[{ruleNo,action}]}` 完整替換指定 QCItemNo+SPCID。transaction 內先查 QC Item 為已核准、SPCID 符合 ChartType，再新增所選規則；action 僅 0 Warning、1 Wait、2 Hold、3 Alert，RuleNo 在同組唯一；RULENO 最長 20 字元。SPK 禁止新增/移動 Wait，但保存其他動作時保留既有 Wait 規則，除非明確移出。
6. API 必須以單一 DB transaction 做 set replacement，參數化 SQL；遵循專案現有登入/錯誤格式。部署後更新 API/schema 文件與本 TODO，commit/push 後通知公司端 pull 驗收。

## QC210 刪除計量型品管項目量測記錄 API/schema handoff（公司端 2026-11-17）

公司端已完成 QC210 WinForms 程式核對、Vue DEV 頁、功能規格與 PostgreSQL CREATE 參考。請 pull 最新 main，先讀 docs/modules/QC/刪除計量型品管項目量測記錄.md、docs/modules/QC/計量品管項目.md、docs/DATABASE_CONNECTION.md，再核對本段。公司端未連 YMES PostgreSQL，以下 Create 檔不表示已比對 live schema 或套用 migration。

### Schema / Create SQL

- 本功能查找表沿用 QC205 的 TBLQCITEMFORVARIABLES，沒有新增主檔表。
- QC210 執行會使用 TBLSPCVARSTATISTICLOG、TBLSPCVARLIMITCONTENT、TBLSPCVARVIOLATIONCONTENT、TBLSPCVARDETAILLOG、TBLSPCVARDETAILONLINE、TBLSPCVARFACTORLOG、TBLSPCVARFACTORONLINE、TBLSPCVAREDITLOG、TBLQCSAMPLEPLANCHANGE、TBLSPCLOGFORSPLANCHANGE。
- Oracle DDL 已保存在 database/reference/mestest_qc205_variable_dependencies_source_ddl.sql、mestest_qc205_theil_custom_source_ddl.sql、mestest_qc_item_attributes_source_ddl.sql。PostgreSQL CREATE 參考 database/reference/mestest_qc210_variable_measurement_log_delete_create.sql 覆蓋 9 張變數量測日誌表；TBLQCSAMPLEPLANCHANGE 已在 mestest_qc_item_attributes_create.sql。
- 請先查 YMES live schema、部署 migration 及 QC205 對應資料表。若表已存在，不要重建或覆寫資料；若只有部分欄位/索引缺漏，再做 forward migration。確認缺表才使用 CREATE 參考設計 migration。CREATE IF NOT EXISTS 不會補齊既有表的欄位。
- 不加 Oracle 沒有的 FK。不要在 MES Oracle 執行這些 PostgreSQL CREATE SQL。只查結構，不讀或匯入 MES 量測業務資料。

### API 工作

1. 沿用 QC205 GET /api/qc/variable-qc-items?issueState=-1 與 ?issueState=0。清單 DTO 要包含 QC210 顯示欄位：QCItemNo、QCItemName、ChartType、SpecLineType、ComponentCollect、LotIsValid、PlanNo、ChartControl、CollectType、LimitLotNum、SampleSize、Unit、QCDataType、Creator、CreateDate、Reviser、ReviseDate，以及隱藏使用的 IssueState。
2. 新增登入保護的 POST /api/qc/variable-qc-items/{qcItemNo}/measurement-log-deletions，body 僅接受 { issueState }；FunctionKey 為 QC210。API 從資料庫讀真實 QC Item 和 IssueState，只允許 0、-1。Log 模式由伺服器固定，不接受用戶端傳 table name、SQL 或任意條件。
3. 每個 QCItemNo 在單一 DB transaction 中先確認 TBLSPCVARFACTORLOG 有資料；沒有資料回傳可辨識的 404/409，不刪任何表。有資料時依規格刪統計、管制界限、違規內容、量測明細/即時明細、量測主檔/即時主檔、修改紀錄、SamplePlanChange 與 SPCLogForSPlanChange。
4. 管制界限及違規內容的 SPCSerial 需從尚未刪除的 TBLSPCVARDETAILLOG 取出。先刪兩類 content，再刪 detail log。整個單一項目刪除應可 rollback；勿照抄 Core 在刪除成功後拋出成功例外，應回傳正常成功 JSON，含 QCItemNo 與各表刪除筆數。
5. Vue 多選時逐筆呼叫上述 API；一項失敗不阻止其他項目，結果需回報成功/失敗 QCItemNo，並寫入 YMES 操作稽核。遵循專案既有登入、錯誤、權限與參數化 SQL 規約。
6. 加上缺少的 forward migration 與 rollback、更新 schema/API 規格與 TODO；commit/push 後通知公司端 pull，等待 QC210 Production API 驗收。公司端尚未執行 build、測試或瀏覽器驗收。

## OP101 作業站法則設定（公司端 UI / API handoff，2026-11-18）

請 pull 最新 `main` 後先閱讀 `docs/modules/OP/作業站法則設定.md`、本段及 `docs/DATABASE_CONNECTION.md`。公司端完成 Vue DEV 頁 `/YMES/OP/ControlRules`；Production API、YMES live schema 比對與正式部署尚未完成。資料結構參考：`database/reference/mestest_op101_control_rule_source_ddl.sql`（Oracle 原始 DDL）及 `database/reference/mestest_op101_control_rule_create.sql`（PostgreSQL CREATE-only，禁止未比對就直接當 migration 套用）。

- [ ] 先比對 YMES live schema/既有 migration 的 `tblopcontrolrule`；若已存在就沿用，若缺表才新增 migration，欄位必須遵循 CREATE 參考中的 NULL/default/主鍵。不要在 MES Oracle 執行 CREATE。
- [ ] 實作登入保護、檢查 OP101 FunctionKey/權限的 API：`GET /api/operation-control-rules?ruleClass=0&ruleNo=`、`POST /api/operation-control-rules`、`PUT /api/operation-control-rules/{ruleNo}`、`DELETE /api/operation-control-rules/{ruleNo}`。
- [ ] GET 僅回標準版 RuleClass=0；包含 UI Grid 欄及 DataStamp。保持 `RuleMode`、`RuleModeQty` 和未暴露欄位；RuleClass 1/2 不要出現在 OP101 清單。
- [ ] POST 大小寫正規化 RuleNo、驗證 RuleNo/RuleName/RuleScript 必填與長度（20/50/3000）、RuleType 0 Lot/1 Component；server 設 Creator/CreateDate、RuleClass=0、DataStamp 初值，處理 RuleNo PK 重複。
- [ ] PUT 僅修改允許的定義欄位，使用舊 DataStamp CAS，成功遞增 DataStamp 並由 server 設 Reviser/ReviseDate；不可重設 RuleMode/RuleModeQty。DataStamp 可為 NULL，實作 null-safe version check。
- [ ] 相容 WinForms 儲存 payload：Apply 不帶 SendMail/HoldDispGroup，缺少欄位代表「保持現值」（新增採 DB/legacy 預設）；Confirm 才帶兩欄。不得把省略欄位轉成 null 清空。Legacy 另有新增 Apply 後仍留在新增模式的行為，詳規格。
- [ ] DELETE 沿用 RuleNo 實體刪除；前端多選逐筆 DELETE，部分成功時回報成功/失敗項目。不連帶刪除 `TBLOPRULE`、`TBL_CPOPRULE` 等其他流程法則。
- [ ] Hold Disp Group 選擇器重用 QC405 handoff 的 `GET /api/qc/hold-disposition-groups?searchFieldName=GroupNo&searchFieldValue=`，資料來源 `TBLQCHOLDDISPGROUPBASIS`，相容 WinForms 未傳 IssueState、載入全狀態的行為；不要另建 lookup table/API。SendMail=Yes 必須有群組。
- [ ] 核對 `RuleScript` 僅當字串儲存，不在 API 執行腳本。原生 `OP_Script` 動態編輯器尚未移植，先用公司端純文字編輯器。
- [ ] 先完成 YMES schema/API 實作，補 API 層驗證及 concurrency 驗收後再提交並 push；公司端 DEV localStorage 假資料不能作為正式資料寫入。

## OP102 企業邏輯設定（公司端 Vue / API handoff，2026-11-18）

請 pull 最新 main，先讀 docs/modules/OP/企業邏輯設定.md、docs/DATABASE_CONNECTION.md 與本段。公司端已完成 /YMES/OP/BusinessRules Vue DEV 頁；Production API/schema 尚未實作。Oracle source DDL 與 PostgreSQL CREATE-only 參考：
- database/reference/mestest_op102_business_rule_basis_source_ddl.sql
- database/reference/mestest_op102_business_rule_basis_create.sql
- database/reference/mestest_op102_scenario_basis_lookup_source_ddl.sql
- database/reference/mestest_op102_scenario_basis_lookup_create.sql

資料庫工作：
- [ ] 先查 YMES live schema/migrations 的 tblopbusinessrulebasis 和 tblopscenariobasis。已存在的表不得重建；只在確認缺表後新增 forward migration，對照 Oracle 欄位 nullability/default/PK。CREATE-only reference 不是 migration，也不要在 MES Oracle 執行 PostgreSQL SQL。
- [ ] 情境表是 OP102 DT 下拉的共享 lookup。若 YMES 已有則重用；若沒有，評估新增其 schema dependency；OP102 handoff 不要求另外建立情境 CRUD。
- [ ] 企業邏輯主表 Oracle 只有 BRNO PK unique index，無 FK/額外 index；情境表只有 SCENARIONO PK unique index，無 FK/額外 index。

API 工作：
- [ ] 實作登入保護及 OP102 FunctionKey/權限驗證。路由需求：GET /api/operation-business-rules?issueState=all&brNo=、GET /api/operation-business-rules/options、POST /api/operation-business-rules、PUT /api/operation-business-rules/{brNo}、DELETE /api/operation-business-rules/{brNo}、POST /api/operation-business-rules/{brNo}/approve。
- [ ] List 回傳 UI 14 個 Grid 欄位及隱藏 IssueState/DataStamp。Options 回傳已允許的 plugin function/ExecutionFile、FRMBR interface/ExecutionFile、核准 ScenarioNo/Name、BRProdType 與 AllowTimeLimitControl ValueList。前端傳入的 DLL/function 名稱不得被服務端任意載入或執行。
- [ ] POST/PUT 只接受 OP102 畫面欄位，Creator/CreateDate/Reviser/ReviseDate/IssueState/DataStamp 與 Issue Job 權限由 server 控制。修改時保留 KernelModule、KernelFunctionName、ActiveModule 與所有產品線旗標欄位，不得以空 DTO 清空。
- [ ] PUT、已簽核停用、Approve 使用 BRNO + 舊 DataStamp compare-and-swap，成功才遞增 DataStamp；Oracle DataStamp nullable/default 0，實作需有 null-safe CAS 並檢查受影響列數，零列回傳 concurrency conflict。
- [ ] 刪除規則：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 改成 -1 並寫 audit；1 拒絕。多筆刪除是逐筆操作，需回報成功/失敗 BRNo。
- [ ] 核准只接受 IssueState=0，沿用 IssueNo BR BASIS 與共用 Issue Job/Issue Control：沒有簽核設定時直接到 2，有設定則先到 1，callback 核准到 2、退回到 0。多筆需回報逐筆結果與 server EventLog。
- [ ] BRProdType ValueList 來源為 TBLOPBUSINESSRULEBASIS/BRPRODTYPE；AllowTimeLimitControl 清單來源鍵是 TBLOPBUSINESSRULEBASIS/TIMELIMITCONTROL。定義窗會停用兩欄；更新時保留已載入值，沒有提交 null。
- [ ] Manual RuleMethod 可用 FunctionName，或 InterfaceName/InterfaceURL；FunctionName 不可與 Interface/URL 同填。DT 的 ScenarioNo 持久化於 InterfaceName；API options 僅回核准情境。
- [ ] Add/Edit 的 Apply 與 Confirm 使用相同業務欄位；Apply 保持視窗開啟，Confirm 關閉。服務端 audit 一律由登入身分建立。
- [ ] 完成 API、schema、權限與簽核驗收後 commit/push，通知公司端 pull 並接正式 API。公司端本段尚未 build/test/browser 驗收。

# 2026-11-18 公司電腦交接：PRD PD102 產品主檔

先 git pull。詳細來源與欄位規則：docs/modules/PRD/產品主檔.md。Oracle 原始 DDL/字典證據：database/reference/mestest_prd_product_basis_source_ddl.sql；PostgreSQL CREATE-only 結構參照：database/reference/mestest_prd_product_basis_create.sql。ProductProperty DDL 因 DBMS_METADATA 連線 ORA-03135，依 Oracle 字典重建並已標示。Create 檔不是 migration。

本 repo 已有 ProductDto、ProductWriteRequest、MasterDataController、PostgresMasterDataRepository.Products.cs 與 migration 0010 的 ym_product_process/ym_product_bom。這些是 YMES 自有簡化關聯；Oracle DDL 顯示 MES 原生 TBLPRDPRODUCTPROCESS、TBLPRDBOM、TBLPRDBOMMATERIALGROUP、TBLPRDEQPCONSTRAINT、TBLPRDPRODUCTPROPERTY、TBLPRDPRODUCT_PICTURE 也存在。先比對 live PostgreSQL schema/migrations，決定映射或擴充；不要再次建立已存在表，也不可照 CREATE-only 檔直接部署。

待辦：
1. 比對 Product DTO/Request 與 Oracle TBLPRDPRODUCTBASIS 欄位長度、SAP/SOP/ERP、序號、圖片欄位；維持 DataStamp CAS。
2. 保留現有產品列表、CRUD、流程及 BOM API；列表 query keyword 與 ProductDto currentVersion/createdAt 命名需和 Vue 相容。
3. 調整原生產品流程 mapping，核實 MOTypeNo、PSNo/PSOrder、ProcessVersion selector、DefaultProcess、HaveComponent/HaveLevel 與 * 目前版本語意。
4. 實作 Oracle BOM 節點樹、材料群組、指定料、損耗率、遞迴最多五層、投入站及產品/材料節點；比對現有扁平 ym_product_bom，不可直接宣稱等價。
5. 為 TBLPRDPRODUCTPROPERTY、TBLPRDEQPCONSTRAINT、TBLPRDPRODUCT_PICTURE 與圖片 BLOB 補必要 schema/API；先查是否已有 YMES 對應。
6. 依 WinForms 實作 Controlled=1/2 屬性、RulePhase/AttribPhase 的 CPASS 檢查、共用 Issue/Job callback、IssueState/DataStamp；目前 API 的預設流程檢查不等價。
7. 對照 Add/Edit/Copy/Version/Apply；WinForms 讓使用者輸入 ProductVersion，YMES 目前自動加一版，需協調 API 契約與子資料複製。補 ProductType/Unit/serial/SAP_PLNAL lookup 與 ProductMaterialcoexist 檢查。
8. 比對後才新增 migration；執行 migration 前確認 home DB schema/data。正式 API 檢查 PRD BASIS/PD102 權限。
9. API/schema 完成後回報辦公室端，串 integration profile；localStorage 假資料僅供 DEV preview。

產品專用 Vue 頁已從 App.vue 的 product-master 分支載入。DEV 模式有四狀態假資料和主要表單。流程/BOM、Property、Parameter、Business Rule、OPDescription、RecipeGroup 與 Accessory DEV 子視窗已完成；OPDescription/RecipeGroup/Accessory 的 Production API/schema 依本檔 handoff 由家用端處理。Resource、SubOP、Program、Default、RunTime、QC、Packing、eSOP、APS、Bin、GBOM、Import 仍待公司端逐項遷移。

## PRD PD102 產品流程／BOM 子表單 API handoff（2026-11-18 公司端更新）

公司端已依 WinForms 原始碼補上 `ProductProcessEditor.vue` 與 `ProductBomEditor.vue`，並接入產品專用頁。DEV 預覽支援產品流程 ADD/DEL/Apply/Confirm、重複與 Default Process 檢查、Net Flow 檢查；BOM 支援右鍵樹選單、已核准產品查找、假材料查找、節點 Add/Edit/View/Del、Specified、投入點、用量、損耗率、LOTCREATE 限制、替代料群組與五層遞迴樹。所有 DEV 寫入只更新產品專用 localStorage。正式 API 模式兩個子視窗唯讀。

詳細 WinForms 欄位、事件、規則與來源路徑：`docs/modules/PRD/產品主檔子表單.md`。表結構參照仍以 `database/reference/mestest_prd_product_basis_source_ddl.sql` 為準；先核對 YMES migrations/live schema，不可重複建表或直接執行 CREATE-only 參照 SQL。

### 家用端接手

- [ ] 對照 Oracle `TBLPRDPRODUCTPROCESS`，補產品流程查詢/批次儲存契約：MOTypeNo、PSNo、PSOrder、ProcessNo、ProcessVersion (`*` current)、DefaultProcess、HaveComponent、HaveLevel；服務端驗證複合鍵、每一 MOType/PS 唯一 Default 與 Net Flow 單一區段。
- [ ] 對照 `TBLPRDBOM` 及 `TBLPRDBOMMATERIALGROUP`，提供產品/材料/投入 OP lookup、節點新增修改刪除、指定料、投料點 2/3/4、數量、損耗率、材料群組/替代料與最多五層遞迴查詢。
- [ ] 服務端重驗已核准節點、LOTCREATE、企業 TS 批號規則及 BOM 複合鍵；不能只依賴 Vue 欄位檢查。
- [ ] 比較現有 `ym_product_process`/`ym_product_bom` 與 Oracle 原生語意後才決定 mapping / migration；測試完成後回報公司端接正式 API。

公司端此段尚未執行 build、test 或 browser 驗收；DEV 元件採靜態檢閱。Business Rule DEV 已完成，Production API/XML serializer 見本檔後續 handoff；OPDescription DEV 已接入但仍待靜態複核，API/schema 見本檔 OPDescription handoff。RecipeGroup/Accessory/Resource/SubOP/Program/Default/RunTime/QC/Packing/eSOP/APS/Bin/GBOM/Import 仍須依 WinForms 原始碼逐項遷移。Parameter handoff 見本檔前段。

## PRD PD102 產品屬性子表單 API/schema handoff（2026-11-18）

公司端已依 `frmProductProperty.vb`、`frmProductPropertyDef.vb`、`wsPRD`、`clsProduct` 完成 `ProductPropertyMaster.vue` DEV 假資料視窗；正式 API 模式唯讀。詳細欄位、狀態轉移、來源路徑與 API 規格：`docs/modules/PRD/產品主檔產品屬性.md`。既有 Oracle 字典證據在 `database/reference/mestest_prd_product_basis_source_ddl.sql` 的 `TBLPRDPRODUCTPROPERTY`；此表已存在，ProductProperty DDL 為 Oracle 字典重建，勿重建同一表或把 CREATE-only 參照 SQL 當 migration。

### 家用端待辦

- [ ] git pull 後先比對 YMES live PostgreSQL schema/migrations，確認產品屬性表是否已存在及其欄位/PK，避免重複建表。尚缺欄位才新增 migration；Create SQL 僅供 review。
- [ ] 提供 `GET/POST/PUT/DELETE /api/products/{productNo}/versions/{version}/properties`、`POST /api/products/{productNo}/versions/{version}/properties/{propertyNo}/approve`；List 回傳 PK、DefaultValue、PropertySequence、Description、IssueState、DataStamp、Creator/CreateDate/Reviser/ReviseDate。
- [ ] Add/Edit 只接收 ProductNo、Version、PropertyNo、DefaultValue、PropertySequence、Description。Creator/日期/IssueState/DataStamp 由 server 控制；Edit 僅狀態 0，PropertyNo 不可改；Update 用複合鍵+舊 DataStamp CAS。Sequence 限 0..99。
- [ ] SYS Property Basis options 對應 `LoadProductPropertyDefine(PropertyType=5, IssueState=2)`；欄位至少 ElementNo、名稱、DefaultValue、ElementSequence、Controlled。PropertyValue options 對應 `wsSYS.LoadElementValue(ElementNo)`。API 端重用/建立適當 SYS lookup API，不能讓前端提供 Controlled 或有效值清單作為信任資料。
- [ ] Add 與 DefaultValue 更新需服務端驗證 PropertyNo 已核准且值符合 SYS Element/MES Parameter (`ChkElementValueValid`)。
- [ ] Delete：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 設 -1；1 拒絕。多筆逐筆處理，回傳各筆結果，碰到第一筆錯誤即停止後續。
- [ ] Approve 僅接受狀態 0。IssueNo=`PRD PROPERTY` 沒有 Job/Detail 時直接設 2；有簽核 Job 時建 Issue Master、設 1、送第一關通知，callback 再核准/退回。保留 audit/EventLog 並使用 DataStamp。
- [ ] 產品主檔核准由 server 重查 SYS Controlled=1 必填、Controlled=2 空值確認及流程 RulePhase/AttribPhase/CPASS 規則，不接受 Vue 送入的 Controlled 作安全依據。
- [ ] 明確對應 ProductType/無版次屬性的 PRODUCTVERSION sentinel；Oracle 空字串與空白語意需以現有資料/應用層規則定義，避免將 `-1`、`''`、`' '` 混為一談。

公司端未執行 build/test/browser 驗收；Property 的選項只有 DEV 假資料，不連 Oracle/YMES API。Parameter API/schema handoff 見本檔新段落。

## PRD PD102 產品 Parameter Setup API/schema handoff（公司端 2026-11-18）

公司端已依 WinForms `PD_ParameterSetup` Form/Designer、`wsPRD`、`clsProduct` 完成 DEV 樹狀子視窗。請先 pull 最新 `main`。詳細畫面、ContextMenu 權限、欄位、驗證、來源服務與資料表見 `docs/modules/PRD/產品參數設定.md`。Oracle source 結構與 PostgreSQL CREATE-only 參考：

- `database/reference/mestest_prd_parameter_setup_source_ddl.sql`
- `database/reference/mestest_prd_parameter_setup_create.sql`

### 家用端待辦

- [ ] 先核對 YMES PostgreSQL live schema、migration、repository/entity 是否已包含產品 OP/Attrib/Rule/Equipment Recipe；上述 CREATE-only 是結構參照，不是部署 migration。只對確實缺少的資料新增獨立 migration，勿照檔案一口氣建立 8 張 Oracle 原名表。
- [ ] 主要 MES 結構為 `TBLPRDOP` (PK SerialNo)、`TBLPRDOPATTRIB` (PK SerialNo/AttribNo/ECNNo/EquipmentType)、`TBLPRDOPRULE` (PK SerialNo/RuleNo)、`TBLPRDOPEQPRECIPE` (無 PK/unique，僅 SerialNo/EquipmentNo/RecipeGroup 非唯一 index)。四表均無 DataStamp、IssueState、FK、Trigger。Oracle 設備關聯表允許資料重複，YMES API 應明確做 duplicate check/交易控制。
- [ ] `TBLPRDOPRULEXML_CLOB`、`TBLPRDOPBUSINESSRULE`、`TBLPRDOPBUSINESSRULERELATION` 已由公司端依實際 service 查明分別供 Rule XML fallback 與 PD102 Business Rule 使用；請沿用該功能 handoff 並先比對 YMES schema。`TBLPRDOPAREA` 是相鄰功能，需追實際 service 後再決定是否納入；不可因參照 SQL 存在就一併建表。
- [ ] 建議讀取產品版本的 Parameter tree；至少回傳 PS/OP、OP SerialNo/NodeID/OPScript、Attrib、Rule、EquipmentNo/RecipeGroup、Recipe Detail。實作 Add OP、Add Condition OP、Attrib/Rule/Equipment 關聯 CRUD；SerialNo 由 server 產生。產品 IssueState!=0 全部唯讀；Server 必須重查產品、PS/OP、權限及 Element/Phase/Rule/Equipment lookup。
- [ ] Attrib API 必須保留 `AttribType` 0 Lot / 1 Component、`AttribSource` 0 KeyIn / 1 Calculate / 2 System / 3 Setup、AttribPhase/Sequence/Value/Script/SaveToLotPropertyNo/QCItemNo 等語意；重做 Element Value、Phase 99、Calculate Script、Setup Default、SaveToProperty/QC Item 驗證。EquipmentType/ECNNo 也是 Oracle Attrib NOT NULL 複合 PK 欄位。
- [ ] **先釐清 Oracle/WinForms 欄位差異再實作 Attrib insert：** MESTEST 字典顯示 TBLPRDOPATTRIB.ECNNO、EQUIPMENTTYPE NOT NULL 且無 Trigger；可讀到的 `wsPRD.AddPRDOPAttrib`/`clsProduct.AddPRDOPAttrib` XML/SQL 路徑未明確帶這兩欄。檢查本機客製 wsPRD、執行時 AdditionalXml、資料庫是否有版本差異；不要塞空字串或假值。
- [ ] 一般 Rule 的 RuleNo/Phase/Script/Type required；CP/FT 專用 Rule 使用不同 service/資料結構，須另核對 RuleClass/CP/FT package API 後再承諾支援。設備候選由產品/OP 可用設備 lookup，Recipe Detail 僅由 RecipeGroup 主檔讀取，產品 OP 只存 RecipeGroup。
- [ ] `DBMS_METADATA.GET_DDL` 本次從 Oracle 連線回 SocketException，但 Windows PowerShell 5.1/ODP.NET 對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_CONS_COLUMNS、ALL_INDEXES、ALL_IND_COLUMNS 的唯讀查詢成功；source DDL 因此是 Oracle 字典重建邏輯 CREATE，不含 tablespace/storage。未讀業務資料、未在 Oracle 執行 DDL/DML。連線方式/秘密處理見 `docs/DATABASE_CONNECTION.md`，密碼只留本機受保護設定，不寫入 Git。
- [ ] 實作 API/schema 並 push 後回報公司端；公司端接 API 時保留 `ProductParameterSetup.vue` 的 DEV mode 與 API read-only 行為界線。

公司端未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，也未部署上述 SQL。

## PRD PD102 Business Rule API/schema handoff（2026-11-18）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品商業規則.md`、`docs/modules/PRD/產品參數設定.md` 及 `docs/DATABASE_CONNECTION.md` 的 PD102 metadata 紀錄。
- [ ] 先比對 YMES live schema/migrations。重用 `database/reference/mestest_prd_parameter_setup_source_ddl.sql`、`mestest_prd_parameter_setup_create.sql` 中的 `tblprdopbusinessrule`、`tblprdopbusinessrulerelation`、`tblprdoprulexml_clob`；三張 Oracle source table 已由公司端直接查 MESTEST 字典，不能因 CREATE-only 檔再建第二份表或原樣執行 SQL。
- [ ] 實作產品/版次/OP 範圍的 Business Rule 查詢與 ADD/EDIT/DEL/Default API。主表 RuleXmlString 是 Oracle LONG；relation 以 `(ProductNo,ProductVersion,OPNo,FromNode,ToNode,LinkName)` 為 PK；LONG provider fallback 使用既有 CLOB 表。XML 與 relations 同一交易更新/刪除。
- [ ] 保留 `iMESXMLFlow.Serial.FlowToXML/XMLToFlow` 的舊版 XML round-trip，並把 Vue graph DTO 轉譯至 RuleXmlString；不能把 DEV localStorage JSON 直接當正式資料。實作 BR/Phase/Template/Process/OP lookup，NetFlow lookup 需帶 `IsNetFlow`。
- [ ] Server 依登入者的 Function/PS 權限與 DB 當前產品 IssueState 驗證；不可接受 client 傳 `PSPrivilege`。依規格實作 Start/End、PASS、輸入/輸出連線與 Phase 驗證，Default 複製 `TBLOPBASIS` XML 及 `TBLOPBUSINESSRULERELATION`，回報錯誤不可吞例外。
- [ ] 此功能不需要建立新的 Oracle 表；migration 僅在 YMES live schema 比對後確有缺漏才新增。完成 API/schema 後通知公司端 pull，接正式 API 與 serializer。

## PRD PD102 Business Rule DEV UI（2026-11-18）

- [x] 核對真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、WinForms Designer、`wsPRD`、`clsProduct`、Service graph validation；排除名稱相近但不同的 `PD_PRDOPRule_THEIL_BU1`。
- [x] 完成產品 OP 清單/圖形預覽及 Default/Close 版面、Add/Edit/多選 Del、圖形節點/連線/Phase 編輯、NetFlow readonly 的 DEV UI，接至 ProductBasisMaster 並只寫專用 localStorage。
- [x] 新增 `docs/modules/PRD/產品商業規則.md`；重用 Parameter 階段已查到的三張 Oracle 表與既有 DDL/PG CREATE 參照，沒有重複 Oracle query、沒有新增表或 CREATE SQL。
- [ ] Production API、OP/Phase/Business Rule/Template lookup、Lassalle XML serializer 尚待家用端完成；Default、樣板及 catalog 現在仍為 DEV 假資料。
- [ ] 公司端未執行 build/test/browser。OPDescription、RecipeGroup、Accessory DEV/DDL handoff 已分別記於本檔後續段落；辦公室端接續 Resource。


## PRD PD102 OPDescription API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品作業站說明.md`、`docs/modules/PRD/產品主檔.md` 及 `docs/DATABASE_CONNECTION.md` 的 OPDescription 查詢紀錄。
- [ ] 比對 YMES PostgreSQL live schema、migration、entity/repository；公司端搜尋目前未找到 OPDescription 專用 API/schema。Oracle source DDL 與 PG CREATE-only 參照為 `database/reference/mestest_prd_op_description_source_ddl.sql`、`mestest_prd_op_description_create.sql`。只在 live schema 確認缺少時建立 forward migration，不照 CREATE-only 直接部署。
- [ ] 表欄位：ProductNo varchar(50)、ProductVersion varchar(5)、OPNo varchar(20) 必填複合 PK；Description varchar(255) nullable；DataStamp numeric(11,0) nullable default 0；Creator/Reviser varchar(10)、CreateDate/ReviseDate nullable。無 FK、trigger、其他 index。
- [ ] 實作產品/版本範圍 OPDescription GET/POST/PUT/DELETE。GET 依登入者 PS 權限過濾並 join OP lookup；POST 驗證產品 IssueState=0、OP 在產品已設定且有權限的流程範圍、同產品版本 OPNo 唯一；PUT 以複合鍵 + DataStamp CAS 原子更新 Description、Reviser/ReviseDate，衝突回 409；DELETE 要求明確 OPNo 並實體刪除。
- [ ] 提供同等 WinForms 查找：產品流程 lookup、指定流程的 Expand Process OP lookup；server 依 session 決定 PS 權限，排除已存在於目前產品/版本的 OPNo，OP 說明初值取 `TBLOPBASIS.Description`。不可信任 client 傳入 PSPrivilege、IssueState 或 audit 欄位。
- [ ] UI 多選 DEL 是逐筆呼叫、遇錯停止且保留前面成功刪除；API 若提供批次 endpoint，需定義並回傳部分成功結果。產品 Copy/Version 需保留 Core `CopyPRDOPDescription` 行為；Product Load Default 的 Description 是另一個功能，不要塞進此子視窗 CRUD。
- [ ] 完成後同步 API contract/schema migration 與文件、push main，通知公司端接正式 API。DEV localStorage 不能寫入正式 DB。

## PRD PD102 RecipeGroup API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品RecipeGroup.md`、`docs/modules/PRD/產品主檔.md` 與 `docs/DATABASE_CONNECTION.md` metadata provenance。
- [ ] Oracle source DDL 與 PostgreSQL CREATE-only 參照為 `database/reference/mestest_prd_recipe_group_source_ddl.sql`、`mestest_prd_recipe_group_create.sql`。先查 YMES live schema、migration、entity/repository；僅當 assignment relation 或必要 lookup 確實不存在時新增 forward migration。`TBLPRDRECIPEBASIS` 是共用 Recipe 主檔，不能重複建表。
- [ ] Oracle `TBLPRDRECIPE` 為 `(ProductNo varchar(50), ProductVersion varchar(5), RecipeGroup varchar(50))` 必填複合 PK，無 audit 欄、無 FK/index/trigger（PK unique index 除外）。`TBLPRDRECIPEBASIS` 為 `(RecipeGroup varchar(50), RecipeVersion numeric(2,0))` 複合 PK；RecipeVersion default 0 NOT NULL、IssueState default 0 nullable、CurVersion default 0 NOT NULL、DataStamp default 0 nullable，其餘 Description/Audit nullable；無 FK/trigger/額外 index。
- [ ] 提供產品版本 RecipeGroup GET、已核准目前版 RecipeGroup lookup、單筆 POST 指派與要求明確 RecipeGroup 的 DELETE；不提供 Edit（WinForms EDIT 隱藏，service 也沒有實際更新）。Lookup 對應 `TBLPRDRECIPEBASIS` 的 `CurVersion=1 AND IssueState=2`，新增時 server 重新檢查，不信任 client。
- [ ] 入口傳入 IssueState，但 `frmProductRecipe` 不用狀態限制 ADD/DEL；新 API 先保留這項 legacy 行為並檢查登入者對產品的權限。若要加狀態限制，需另有業務規則依據。
- [ ] WinForms 多列刪除逐筆呼叫、遇錯停止且保留先前成功結果。API 若做批次需定義 partial result；單筆 DELETE 不可省略 RecipeGroup，避免 Core 預設刪除產品版本全部關聯。
- [ ] 產品 Copy/Version 需保留 `CopyPRDRecipe`：先清除目標版本 RecipeGroup 關聯，再複製來源關聯。Product Recipe Apply/Confirm/Close 綁定原生指派語意；完成 schema/API/文件後 push main，通知公司端接正式 API。DEV localStorage 不寫正式 DB。

## PRD PD102 Accessory API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品配件設定.md`、產品主檔規格與 DB connection 的 Oracle metadata provenance。
- [ ] Oracle source DDL / PostgreSQL CREATE-only 參考：`database/reference/mestest_prd_accessory_source_ddl.sql`、`mestest_prd_accessory_create.sql`。先查 YMES `tblprdaccessory` live schema、migrations、entity/repository；僅在確認缺表後新增 forward migration。
- [ ] Source `TBLPRDACCESSORY` 欄位：ProductNo varchar(50)、AccessoryType varchar(50)、AccessoryNo varchar(50)、ProductVersion varchar(5)、OPNo varchar(20) DEFAULT `'*'`、OPName varchar(200)；五欄 `(ProductNo,ProductVersion,OPNo,AccessoryType,AccessoryNo)` 複合 PK；OPName nullable；無 FK、trigger、其他 index（PK unique index 除外）。
- [ ] 重用配件功能既有 `tbleqpaccessorytype`、`tbleqpaccessorybasis` lookup/API，不要新增同義表。提供產品/版本配件 GET、PS 權限下的產品流程 OP lookup、AccessoryType 與 AccessoryNo lookup。
- [ ] 實作以單一 ProductNo/ProductVersion/OPNo 更新設定的 API，支援指定多個 AccessoryNo 與 `'*'` 全部模式；定義空 multivalue 是否清空該類別、操作是否以 transaction 原子替換。WinForms client 只證明 Update 帶單一 OPNo/OPName 與 multivalue、DEL 逐筆帶 OPNo/OPName/AccessoryType；`wsPRD`/`clsProduct` 實作不在 client source，需查服務/Core 確認替換範圍及 Copy/Version 行為。
- [ ] Delete 必須明確限定 ProductNo、ProductVersion、OPNo、AccessoryType。UI 多選逐筆處理，遇錯停止並保留先前成功結果；批次 endpoint 若實作需回傳逐筆結果。
- [ ] Server 依登入身分驗證產品、流程/PS 權限及配件 lookup，不接受 client 自傳 PSPrivilege、OPName/audit 身分或 IssueState。完成 API/schema/文件後 push main 通知公司端；DEV localStorage 資料不可送進正式 API。

公司端此段未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Resource API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 docs/modules/PRD/產品作業區域設定.md、docs/modules/PRD/產品主檔.md 和本檔 handoff。
- [ ] 不新增表或 CREATE SQL：Oracle source/PG CREATE 參照已存在。TBLPRDOPAREA：database/reference/mestest_prd_parameter_setup_source_ddl.sql / mestest_prd_parameter_setup_create.sql；TBLOPAREA：mestest_operation_and_system_source_ddl.sql / mestest_operation_and_system_create.sql；TBLSMDAREABASIS：mestest_smd_area_basis_source_ddl.sql / mestest_smd_area_basis_create.sql。仍先核對 YMES live schema/migrations，不可因參考 CREATE 檔重建有資料的表。
- [ ] 核對 TBLPRDOPAREA 五欄與 PK (ProductNo,ProductVersion,OPNo,AreaNo)：前四欄 NOT NULL，DefaultArea numeric(1,0) nullable，無 audit/DataStamp/FK/trigger/額外 index。AreaName、FactoryNo 是 SMD Area lookup 顯示欄，不可存入此表。
- [ ] 實作登入保護 API：GET 產品/版次 OP Area 清單及指定 OP 明細；PUT 以 (ProductNo,ProductVersion,OPNo) 全量取代 areas:[{areaNo,defaultArea}]；DELETE 必須帶完整產品、版次、OP 範圍並刪該 OP 全部區域。WinForms 新增/修改都呼叫同一 UpdEditPRDOPArea，來源註解要求全刪全增；家用端查 Core/Service 確認 transaction/空集合語意，更新需原子執行。
- [ ] 提供產品版次流程 lookup、指定 Process/Version 的 OP NodeType=0 lookup、OP 的 TBLOPAREA 查找、Area Basis lookup。重用 PRS、OP、SMD 的既有 API/table，不新增同義表。新增模式初始區域由 OP Area Map 載入；修改模式載入目前產品 OP Area，兩者選項範圍按 WinForms 原 service 行為查證。
- [ ] Server 從登入者權限決定 PSPrivilege；不接受 client 的 PSPrivilege、IssueState 或 audit identity。對產品當前 IssueState、OP/PS 權限、Area 存在及關聯範圍重新驗證。PUT 要拒絕重複 AreaNo，並要求恰一筆 DefaultArea=1；UI Delete 一次確認後逐 OP 呼叫，遇錯停止並保留先前已成功項目。
- [ ] 查 LoadPRDOPArea 的 DefaultArea 篩選、LoadOPResourceDefine lookup 範圍、DelPRDOPArea 精確刪除條件及 CopyPRDOPArea 產品 Copy/Version 行為。規格記錄來源 client 無法證明之處；核對後更新模組規格。
- [ ] 完成 YMES schema/API/DTO 與文件後 commit/push main，通知公司端接 Production API。公司端 Resource DEV 已完成；未執行 build/test/browser，未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 SubOP API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品子作業設定.md`、`docs/modules/PRD/產品主檔.md` 與本檔 handoff。
- [ ] Oracle source DDL：`database/reference/mestest_prd_subop_source_ddl.sql`；PostgreSQL CREATE-only 參照：`database/reference/mestest_prd_subop_create.sql`。先查 YMES live schema、migration、entity/repository；只在確認缺表或缺欄後新增 forward migration，不能直接執行參照 CREATE SQL。
- [ ] 專用關聯表 `tblprdopsubop` 欄位：`productno varchar(50)`, `productversion varchar(5)`, `opno varchar(20)`, `subopsequence numeric(4,0)`, `subopno varchar(20)`, `subopname varchar(100)` 全為 NOT NULL；`creator varchar(10)`, `createdate timestamp`, `datastamp numeric(11,0) DEFAULT 0`, `reviser varchar(10)`, `revisedate timestamp` nullable。PK `(productno,productversion,opno,subopsequence)`；Oracle 無 FK、trigger 或非 PK index。不要與共用 OP 主檔 `tblopsubopbasis` 混用，也不要重建已存在的 OP 主檔表。
- [ ] 提供產品/版次 SubOP 清單、尾端新增、指定序號插入、修改、多選/單列刪除與序號整理 API。Insert 的後續序號位移與新增需在同一 transaction；Delete 精確限定產品、版次、OP、Sequence。WinForms 多選是逐筆呼叫、遇錯停止且保留此前成功刪除；若改批次 API，要表達部分成功結果。
- [ ] OP lookup 重用產品流程 OP 查詢及 `PSPrivilege=True` 範圍；由登入者權限在 server 計算 PSPrivilege，不信任 client 傳入的權限、Creator/Reviser、IssueState、OPNo 或 DataStamp 更新值。此子表單未按產品 IssueState 限制，資料表無 IssueState/Approval 欄，維持來源行為。
- [ ] Edit 依舊以 DataStamp 做 optimistic concurrency；欄位 nullable/default 需保留，確認舊資料 DataStamp NULL 的更新語意。Sort 對各 OP 重新編成 1..N，只更新序號。
- [ ] 完成 live schema/API/DTO/migration 與文件後 push `main`，通知公司端接正式 API；公司端目前只有 DEV 假資料，未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Program API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品程式指派.md`、`docs/modules/PRD/產品主檔.md` 與本檔 handoff。
- [ ] Oracle source DDL：`database/reference/mestest_prd_program_source_ddl.sql`；PostgreSQL CREATE-only：`database/reference/mestest_prd_program_create.sql`。先檢查 YMES live schema、migration、entity/repository；`tblprdprogrambasis` 已在 first-phase bootstrap，先核對欄位並修正既有 schema mapping，不要重複建立。只在確認 `tblprdproductprograms` 不存在時新增 forward migration。
- [ ] `tblprdprogrambasis` 欄位：ProgramNo varchar(50) NOT NULL PK；ProgramType varchar(10) nullable；IssueState numeric(1,0) DEFAULT 0 NOT NULL；Creator varchar(10)、CreateDate timestamp、Description varchar(255) nullable。`tblprdproductprograms` 為 ProductNo varchar(50)、ProductVersion varchar(5)、ProgramNo varchar(50) 三欄 NOT NULL，複合 PK `(productno,productversion,programno)`；無 FK、trigger 或 PK 以外 index。
- [ ] 提供登入保護的 Program option GET 與產品版次 assignments GET。WinForms `LoadProgram()` 不傳 IssueState 條件，option lookup 不過濾 Program IssueState；UI 欄位為 ProgramNo/ProgramType/Description。
- [ ] 提供完整替換 API `PUT /api/products/{productNo}/versions/{version}/programs`；同一 transaction 內刪除該版次現有關聯並插入勾選清單。空清單代表清除全部。驗證 ProgramNo 存在、去重、登入者有產品版次設定權限。
- [ ] 只有產品 IssueState=0 時允許儲存。舊畫面在其他狀態仍允許勾選，但 Confirm disabled；API 必須 server-side 再檢查狀態與權限，不信任 client 的 IssueState/identity。
- [ ] 完成 schema/API/DTO/文件後 push main，通知公司端接正式 API。公司端目前只完成 DEV localStorage，Production API 未接線；未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Default API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品預設設定.md`、`docs/modules/PRD/產品主檔.md` 與本段 handoff。
- [ ] Oracle APS product source DDL：`database/reference/mestest_prd_default_aps_source_ddl.sql`；PG CREATE-only：`database/reference/mestest_prd_default_aps_create.sql`。先查 YMES live schema、migrations、first-phase bootstrap；APS bootstrap 有欄位曾標示為推測，不能直接執行 CREATE-only 或重建既有表。
- [ ] `tblprdprsopapssetup` PK `(productno,productversion,motypeno,nodeid)`；`tblprdprsopapssetupdetail` PK `(productno,productversion,motypeno,nodeid,equipmentno)`。兩張 Oracle 表都存在；欄位、nullable/default 與字典證據見功能規格及 SQL reference。流程 APS `tblprsopapsbase/detail` 已由 PS104 提供，先比對 YMES schema，不建立重複表。
- [ ] 實作登入保護的批次 Default API。請求攜帶 ProductNo、ProductVersion、DefaultOP (1=All PS OP、2=Product Process OP)、Parameter/BusinessRule/Description/OPResource/QCItem/SubOP/APS 旗標；API 端從 DB 展開 OP，重新驗證 IssueState=0、產品流程/PS 權限、來源預設已核准。禁止信任 client 的 OP 清單、IssueState 及 audit identity。
- [ ] 對照 `clsProduct.UpdPRDOP_BatchDefault`：此功能可能重建 `TBLPRDOP`、Parameter/Attrib/Rule、BusinessRule/Relation/XML CLOB、Description、OPArea、QCItem、SubOP 及 APS setup/detail。查清空來源時的清除行為、未勾項目的保留範圍、Parameter ECN 例外及實際交易邊界後，才決定 API transaction。
- [ ] 注意 WinForms APS checkbox 在 `ExecuteAPS` 未啟用時隱藏但 Designer 預設 checked；QCItem 預設由 `PRDDefault_QCItemAutoChecked` 決定。核對舊 client 送旗標行為並寫入規格，避免 UI/API 默默改變資料。
- [ ] 只有 live schema 確認缺表/缺欄才新增 migration；完成 API/DTO/錯誤處理後更新規格及 TODO，commit/push `main`。公司端 Default 目前只有 DEV localStorage；Production API 未接線，未部署 SQL。

## PRD PD102 RunTime 家用端 handoff（2026-11-18）

- [ ] 先比對 live schema、first-phase bootstrap 與 migration history。以 [RunTime 規格](docs/modules/PRD/產品標準工時.md)、[Oracle 邏輯 DDL](database/reference/mestest_prd_run_time_source_ddl.sql) 及 [PostgreSQL CREATE-only](database/reference/mestest_prd_run_time_create.sql) 核對 `tblprdruntimesetup` 與六張既有 lookup 表。來源 RunTime 表沒有 PK、unique、FK、索引或 trigger。
- [ ] 實作登入保護的產品版次 RunTime GET／PUT。GET 依產品 OP 區域、設備／群組與設備類型預設展開，再套用已存設定。PUT 拒絕空清單，核對伺服器計算的列身分、精度、權限與同版次並發，全量替換必須在同一 transaction。核對 Fix/Var 工時及 WorkPrice 欄位是否須為其他流程保留。
- [ ] 驗收無設備、設備直掛／群組重複、首次儲存、整批替換、錯誤回滾、並發、Copy／刪除、稽核及權限。公司端 RunTime 已提供 DEV localStorage 操作，正式模式待 API 接線。

## PRD PD102 QC Item 家用端 handoff（2026-11-18）

- [ ] 依 [QC Item 規格](docs/modules/PRD/產品品管項目設定.md) 先比對 live schema 和 migration history。`TBLPRDQCITEM.XX_FREQ` 為目前 MESTEST 已確認的 nullable 欄位。沿用既有 OP、QC 目錄及 BindLog，評估需要的 forward migration。
- [ ] 實作產品版次／OP 範圍 GET/PUT。按伺服器可用目錄驗證項目、PS 權限、LotIsValid、InLine 檢驗旗標和主鍵，以完整清單替換該 OP，支援清空，保留其他 OP，妥善處理 BindLog 與並發。
- [ ] QC Import、QC Chart 檔案解析及檢查／寫入仍待接續，見規格中的 WinForms 與 Service 路徑。公司端目前已完成 DEV 指派表單。

## PRD PD102 Packing Set 家用端 handoff（2026-11-18）

- [ ] 依 [Packing Set 規格](docs/modules/PRD/產品包裝設定.md) 核對 live schema、first-phase、既有 migrations 及七表的 [Oracle 邏輯 DDL](database/reference/mestest_prd_packing_set_source_ddl.sql)／[PostgreSQL CREATE-only](database/reference/mestest_prd_packing_set_create.sql)。產品包裝數量沿用產品主檔，保留序號主表索引與 PK 的差異紀錄。
- [ ] 接三階 Pack Setting、配件 CRUD／整批 Copy 及多階 Rule Map API。以伺服器權限、產品 IssueState、DataStamp、Current／Specified 規則版次和使用中批次作為寫入條件。
- [ ] 公司端已完成 Serial Rule、Label Rule／Multi View 和 DEV 欄位預覽；家用端待核對序號長度 100 與 Oracle 欄位精度、2D 分隔符、標籤引擎、檔案路徑權限與批次寫入。

## PRD PD102 EQPCapItem 家用端 handoff（2026-11-18）

- [ ] 依 [設備能力項目規格](docs/modules/PRD/產品設備能力項目.md) 核對 `tblprdopcapitem`、共用 `tblemseqpcapbasis`、`tblopbasis` 與 live schema／migration history。只補缺漏的表或欄位。
- [ ] 實作產品版次設備能力 CRUD、OP／CapItem lookup、同版次 Copy、新舊 DataStamp 衝突、稽核和多選刪除語意。伺服器驗證 IssueState、權限、主鍵及 CapValue 資料型別與長度。

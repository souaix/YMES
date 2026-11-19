# ECN 作業站群組設定（OPB02）

> **本批完成（2026-11-20）：已正常同步GitHub main `487e9d1`。** 正式程式版本（release code commit）為 `487e9d1d15489be5d864c54abc832b81724f579d`；root已核對1份migration／139份API／206份Web與已驗收來源一致，正常推送395a→487，沒有force push。API1d09／Webcfa、0054、48個驗收檢查點（含SQL讀回1項）及19scope全0的實證不變。下方Git待推文字保留為同步前歷史；後續純文件狀態提交另記，不取代本release code或重建產物。未提供的Job／通知provider仍待來源。

## 功能識別

| 欄位 | 值 |
| --- | --- |
| FunctionNo / FunctionKey | `B_ECN OP GROUP BASIS` / `OPB02` |
| 三層選單 | 作業站資料 → 行業包 → ECN 作業站群組設定 |
| WinForms 主畫面 / 編輯畫面 | `frmOP_B_ECNOPGroupBasis` / `frmOP_B_ECNOPGroupBasisDef` |
| WinForms DLL | `OP_B_ECNOPGroupBasis.dll` |
| Vue 頁面 / URL | `web/src/components/EcnOperationGroupMaster.vue` / `/YMES/OP/ECNOperationGroups` |
| Oracle 主表 / 明細表 | `MESTEST.TBLOPECNGROUPBASIS` / `MESTEST.TBLOPECNGROUPDETAIL` |
| 主鍵 | Basis: `OPGROUPNO`；Detail: `OPNO`（單欄、全表唯一） |
| 公司端／家用端狀態 | 公司端Vue DEV假資料頁與原生來源已交付；家用端API／schema／Web已部署、正式GUI已驗，已正常同步main487e9d1，詳見本檔本輪驗收段 |

## WinForms 來源與 Oracle 結構

本節Oracle metadata查證由**公司電腦**完成，下文「本機」指公司端原生MES環境。**家用電腦**只依這些來源核對／建立復刻的YMES PostgreSQL與API，不在家用端連Oracle，也不把兩種資料庫當成同一實體。

直接閱讀 WinForms Forms/Designer、`Industry_Base.wsOP`、`clsOpBasis`、`clsB_Basis_UPD`，並用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀取得 Oracle DDL 與資料字典。只查 metadata，未查業務列、未執行 Oracle DDL/DML，連線秘密未輸出或提交。

- `TBLOPECNGROUPBASIS`：`OPGROUPNO VARCHAR2(20)` PK；`OPGROUPNAME VARCHAR2(50)`、`PSNO VARCHAR2(50)`、`ISSUESTATE NUMBER(1,0)` 為 NOT NULL；`OPGROUPORDER NUMBER(6,0)`、`DESCRIPTION VARCHAR2(255)`、`CREATOR/REVISER VARCHAR2(10)`、`CREATEDATE/REVISEDATE DATE`；`DATASTAMP NUMBER(11,0) DEFAULT 0` nullable。
- `TBLOPECNGROUPDETAIL`：`OPGROUPNO VARCHAR2(20)`、`OPNO VARCHAR2(20)` 都 NOT NULL；PK 只有 `OPNO`，因此一個作業站全表只能屬於一個 ECN 群組。
- Oracle constraints 只有 PK 與欄位 NOT NULL checks；唯一 index 只有兩張表各自的 PK index。沒有 FK 或額外 index。
- 原始 Oracle DDL：[mestest_opb02_ecn_operation_group_source_ddl.sql](../../../database/reference/mestest_opb02_ecn_operation_group_source_ddl.sql)。PostgreSQL 空表 CREATE 對照：[mestest_opb02_ecn_operation_group_create.sql](../../../database/reference/mestest_opb02_ecn_operation_group_create.sql)。CREATE 僅供家用端核對 live schema，不是 migration。

## 主畫面與欄位

- WinForms 標題 `ECN OP Group Define`，ClientSize 1008×591，啟動最大化。主檔框 `(12,53)` / `984×247`；明細框 `(12,306)` / `984×235`；兩個 Grid 左側、80×32 操作按鈕在右側，底部 Close。
- 預設狀態為未簽核 `0`；狀態列有未簽核 `0`、簽核中 `1`、已簽核 `2`、不使用 `-1`。查詢欄位為 `OPGROUPNO`。舊程式載入群組後在畫面端依狀態篩選，排序 `OPGroupOrder, OPGroupNo`。
- 主 Grid 顯示 `OPGroupNo, OPGroupName, PSNo, OPGroupOrder, Description, Creator, CreateDate, Reviser, ReviseDate`。`IssueState` 與 `DataStamp` 是程式欄位；Grid 隱藏。日期格式 `yyyy/MM/dd HH:mm:ss`。
- 主檔按鈕：ADD、EDIT、DEL、Copy、Approve；底部 Close。明細按鈕只有 ADD、DEL；沒有明細 Edit 或 Copy。
- 明細 Grid 隱藏 `OPGroupNo`，只顯示作業站 `OPNo, OPName`。選取主檔列時即時載入該群組明細。
- Vue 保留上下雙 Grid、右側縱向按鈕、狀態篩選、查詢、稽核欄與 KD 淡黃色輸入欄；選列以 Vue 狀態更新，不導頁重載。

## 主檔新增、修改、複製

定義視窗 `ECN OP Group Basis Define` 有群組編號、群組名稱、生產區段、群組順序、說明及建立/修改稽核資訊；按鈕為確定、關閉、套用。

- `OPGroupNo` 必填、離開欄位轉大寫、Oracle 最大 20 字元；新增/複製可輸入，修改時唯讀。
- `OPGroupName` 必填，最多 50 字元；`PSNo` 必填，由生產區段選擇視窗選取，最多 50 字元。
- `OPGroupOrder` 為 0–999999 整數；`Description` 可空，最多 255 字元。
- Add/Copy 新增 `IssueState=0`，由服務建立 Creator/CreateDate；Edit 僅限狀態 0，修改名稱、區段、順序、說明，帶 `DataStamp`，服務記錄 Reviser/ReviseDate 並遞增 DataStamp。
- Copy 只建立一筆新的群組主檔，不複製來源明細；新增後明細為空。
- Apply 與 Confirm 使用同一 Add/Edit 服務；Apply 保持視窗開啟，Confirm 成功後關閉。瀏覽器不得提交可信任的 audit 欄位。

## 明細加入與刪除

- Add 明細先按目前群組的 `PSNo` 查詢作業站；舊 Core 條件為 `TBLOPBASIS.IssueState=2`、`PSNo` 完全相等，且 `OPNo NOT IN (SELECT OPNo FROM TBLOPECNGROUPDETAIL)`。這是全表排除，不能將作業站重複加入不同 ECN 群組；`PSNo='*'` 也照舊 SQL 做字面相等，不解讀成不限區段。
- WinForms 多選清單可搜尋 `OPNo, OPNAME, PSNO, OPTYPE, OPCLASS`；`OPCLASS` 對照 0 一般作業站、1 品質管理、2 其他作業站。Grid 明細僅顯示 `OPNo/OPName`。
- 加入時逐筆呼叫 `AddECNOPGroupDetail`；刪除支援多選、逐筆實體刪除。資料庫明細沒有 audit、IssueState 或 DataStamp 欄位。
- 明細操作沒有在 Form 端依群組 IssueState 禁用；Production API 應依核對後的來源規則處理，不要自行假設明細只能在未簽核群組變更。
- 群組主檔實體刪除會在同一交易先刪其明細再刪主檔。已核准群組停用時保留明細。

## 刪除與簽核狀態

| 當前狀態 | 修改 | 刪除 | 核准 |
| ---: | --- | --- | --- |
| 0 未簽核 | 可修改 | 實體刪除（先刪明細） | 可送核准 |
| 1 簽核中 | 不可修改 | 不允許 | 不允許 |
| 2 已簽核 | 不可修改 | 改為 -1，保留明細 | 不允許 |
| -1 不使用 | 不可修改 | 實體刪除（先刪明細） | 不允許 |

Approve 使用 IssueNo=`OP ECN GROUP BASIS`，並以當前 DataStamp 更新 Reviser/ReviseDate：

1. IssueManagement 沒有此 IssueNo 對應 Job，或該 Job 沒有 Detail 時，直接把狀態由 0 改為 2。
2. 若有簽核 Job 與 Detail，建立 Issue Master/Job，核准 SQL 將狀態改 2、退回 SQL 還原 0，送出後設為 1，並呼叫共用 Issue 流程通知第一關負責群組。
3. WinForms 多選核准逐筆處理；遇到非 0 狀態或第一筆失敗即停止，之前成功列不回滾。Production API 呼叫共用簽核服務，不可在頁面自行模擬 production approval。

`DataStamp` 在 Oracle nullable、DEFAULT 0。Production API 使用 null-safe compare-and-set；不得把舊 NULL 靜默當成已讀取的 0。主檔硬刪與明細刪除應各用交易；Oracle 無 FK，不得新增來源不存在的 FK/index。

## 家用端 API 契約

所有端點要求登入；FunctionNo=`B_ECN OP GROUP BASIS` / FunctionKey=`OPB02`。功能 ACL 依目前全專案決策暫緩，不另造帳號／角色表。API audit user/time 取自伺服器 principal；主檔修改、刪除停用、核准及每筆明細操作皆檢查父群組 DataStamp，衝突回 409。下列為家用端 query-key 草稿；舊公司端 segment 路徑為歷史 handoff，未曾部署為正式 OPB02 API。

| 方法 | 路徑 | 用途 |
| --- | --- | --- |
| GET | `/api/ecn-operation-groups?issueState=all&opGroupNo=` | 依權限查群組；支援狀態及編號查詢。必要時套用使用者可見 PS 範圍。 |
| GET | `/api/ecn-operation-groups/item?opGroupNo=` | 完整 Group DTO，含原始 nullable order／audit／DataStamp。 |
| GET | `/api/ecn-operation-groups/operations?opGroupNo=` | `{group,items}`；同一交易持有父 SHARE，讀出父版本、明細及目前 nullable OPName。 |
| GET | `/api/ecn-operation-groups/lookups/production-segments` | 真 PS lookup；新／變更採 state2 保守規則，見下節。沒有合成的全區段 `*` 選項。 |
| GET | `/api/ecn-operation-groups/lookups/operations?psNo=&issueState=2&notInAnyGroup=true` | 僅回相同 PS、已核准且不在任一 ECN 群組的作業站；帶 OPClass 顯示值所需欄位。 |
| POST | `/api/ecn-operation-groups` | 新增主檔，伺服器設定狀態 0 與 audit。 |
| PUT | `/api/ecn-operation-groups/item?opGroupNo=` | 修改狀態0主檔，主欄位加必須出現的 nullable `dataStamp`；回完整 Group DTO。 |
| DELETE | `/api/ecn-operation-groups/item?opGroupNo=` | body `{dataStamp}`；按 DB 真狀態：0/-1 同交易刪子／父、2 改 -1 保留明細、1／未知狀態409。均回200 `{deleted,item}`，硬刪 `deleted=true,item=null`，停用 `deleted=false,item=新Group`。不相信 body 的 IssueState 作授權。 |
| POST | `/api/ecn-operation-groups/approve?opGroupNo=` | body `{dataStamp}`；採下節明載本地無流程策略，回保存 Group DTO；發現任何已知本地流程來源時503零修改。 |
| POST | `/api/ecn-operation-groups/operations?opGroupNo=` | body `{opNo,dataStamp}`，版本為父群組；新增單一核准、PS字面相等、全域未分組 OP，回 `{group,item}`，父版本遞增。 |
| DELETE | `/api/ecn-operation-groups/operations?opGroupNo=&opNo=` | body `{dataStamp}`，版本為父群組；驗真正 owner 後精準實刪，回 `{group}`，父版本遞增。 |

主檔 POST／PUT 的 `psNo`、`opGroupOrder` 必須出現；POST 必填有效值。PUT `dataStamp`、DELETE／Approve／每筆明細 `dataStamp` 必須出現，允許明確 NULL；省略400、舊版本409、NULL不等於0。成功遞增以 `COALESCE(datastamp,0)+1`，匹配NULL後為1；負數來源整數照真值比對及加1，不重置成0。非整數或超出 numeric(11,0) 為400；需遞增時達99999999999回409。0/-1主檔硬刪不需遞增，匹配最大版本仍可同交易刪除；2→-1及明細操作因需父遞增而受上限保護。

query-key 保留 `/`、`|`、`*`、`%` 為文字識別值；查詢使用字面 contains，沒有把 `%`／`_` 當 SQL LIKE 萬用字元。明細沒有 PUT／Edit，也沒有 child DataStamp、audit 或新增欄位。每筆明細的父 CAS／bump 是明載 YMES 延伸，避免舊主檔版本刪掉後來加入的成員；多選仍逐筆呼叫，前一筆回傳的新父版本必須接續到下一筆。

## Vue DEV 資料與實作界線

公司端交付的 `EcnOperationGroupMaster.vue` 僅在 Vite DEV 且 `developmentPreview=true` 使用 `DEV-` 假資料與瀏覽器 localStorage。核准假資料走「無簽核 Job、直接狀態 0→2」分支。當時未實作 Production API/migration，也未執行 build、test 或 browser 驗收；家用端後續草稿及正式策略以下節為準。

## 家用端 API／schema 草稿（2026-11-19，尚未部署）

### 獨立只讀稽核與最小建表

- PPK SSH 僅 `BEGIN READ ONLY` 查 `pg_catalog`、ACL／角色；跨所有 schema、不分大小寫查兩表精確名稱，皆不存在。沒有讀業務列，也沒有使用 Oracle。這是缺表，不是空表／缺預設值的判定。
- 原生 DDL 與既有 PG CREATE-only 逐欄核對為11＋2欄、次序／logical type／NULL／default／PK差異0，首欄 OPGroupNo 皆在；Oracle DATE 沿專案映為 `timestamp without time zone`，不宣稱儲存格式／小數秒精度相同。
- 新草稿 `database/migrations/0054_opb02_ecn_operation_groups.sql` 只建這兩張真表及各自 PK，不重建 schema／PS／OP／Issue 表，不造 FK、額外 index、業務列或 child stamp。IssueState NOT NULL 但沒有 default；Create API 明確 INSERT 0。Order nullable、沒有 default；DataStamp nullable DEFAULT 0。
- Basis 需 app CRUD；Detail 只有 SELECT／INSERT／DELETE，另欄級 `UPDATE(opno)` 供 PostgreSQL row-locking SELECT 使用，沒有表級 UPDATE，也沒有明細 Edit。現有 `tblopbasis`、`tblprsproductionsegment` 的 app 查找／SHARE 鎖權限已足夠，不在0054另擴權。
- 本檔仍為未套用草稿。由主流程重新核對 metadata、備份並驗證備份可讀後決定套用；不得把文件或 CREATE 檔當作已建表／API已部署。

### 來源規則與明載的 YMES 完整性延伸

- Group identity 及全域 OP identity 用 PostgreSQL `lower(btrim(...))` 判斷歧義及取得識別碼鎖；不改原始 PK／index，也不清洗來源列。舊重複 group、child owner raw別名、全表 OPNo 大小寫／空白歧義、被查找 PS／OP master 歧義均409，保留原列。
- 成員 PS 關係使用實際值字面相等（SQL `IS NOT DISTINCT FROM`／lookup `psno=@ps`），沒有大小寫折疊／trim後比值或 `*` 放寬分支。`*` 只有實際真 PS master及 OP.PSNo 也都是 `*` 時才可使用；不從 OP105 複製全區段規則。
- 新／變更 PS 必須真實唯一並為 state2；來源沒有交付完整 LoadPSDefine 的 state／可見範圍條件，state2 是 YMES 保守策略。未變的 raw、停用、目錄缺失值可保留，不強迫重選。PS 在來源 schema 為 NOT NULL；程式對舊真NULL防禦性保存，不為測試放寬來源 NOT NULL。Order既有NULL也可保留，不當成0寫回。
- 改主檔 PS 前，按 PostgreSQL 固定順序鎖住所有既有成員真 OP，確認其實際 PS 都與新 PS 字面相等；缺 master或不符409、整交易不改。這是維護既有關聯的 YMES 延伸，不宣稱原 Core 有此額外檢查。
- 寫入順序為 group namespace → group UPDATE → 全域 ECN OP namespace（逐筆Add/Delete）→ 新增時真 OP SHARE → detail UPDATE。變更 PS 依固定 OP key順序持有 SHARE。所有 true查找及父／子寫入維持到 commit；主檔硬刪只精準刪 own details與父列，不反向鎖其他群組／OP。
- 明細可在任何群組狀態逐筆操作；新增仍重驗真OP state2、PS字面相等及全表未分組。刪除依實際 detail owner判斷，不因 OP master停用／缺失而禁止，也不能只按全域 OPNo 刪掉其他群組的列。新增／刪除在同交易更新父 audit與版本，保持群組 state。
- 新增時間與修改時間均為 server UTC `Z`；Creator／Reviser 取登入者，不接受 body audit。保存結果在同交易讀出，再 commit返回完整DTO，沒有 commit後再GET造成「已存卻回失敗」的額外查詢。
- 其他共用 OP實刪的 ECN引用檢查由主流程另行窄整合；此 repository本身不修改舊OP程式。只有row SHARE不能宣稱 commit後永遠阻止外部writer刪OP；未採同一協定的直接SQL不受此完整性協定保護。

### 簽核的目前界線

只讀 catalog 目前未找到已知四類 Job表或 `tblisu%`／Issue-Job候選 relation，repo也沒有可重用的完整 IssueManagement provider。現階段明採專案既定的「本地無流程」策略：Approve在父鎖／CAS下確認本地所有非系統 schema 都沒有已知四類 Job表或上述候選 relation，才直接0→2；任何 table／partition／view／materialized view／foreign table候選存在便503零修改，不讀取／臆測其業務設定。

這不是原 MES IssueNo=`OP ECN GROUP BASIS` 的 Job map、Job Detail、Issue Master、簽核回呼或第一關通知實作，也不能以PG沒有表宣稱已查到原生設定無Job。原流程仍待精確實體DDL／關聯、Core/provider呼叫、核准／退回callback與通知契約；不造Job表、不假通知成功、不送出假pending。未來來源交付後改由真正共用provider判斷「無Job／無Detail／有流程」。

### 驗證與前端交接

目前新增 `OpB02Controller.cs`、`OpB02Models.cs`、`OpB02Repository.cs`，僅本機語法核對與source peer階段。主流程持有 Program DI、舊OP ECN引用分支、正式備份／套用／build／API gate；本草稿未操作遠端、未執行HTTP／DML、未做GUI驗收或Git同步。

本頁Vue已改用新 query-key 契約，完成純 `vue-tsc --noEmit` 型別核對；沒有執行 Vite 發行build、正式HTTP／DML或GUI驗收。它在 API 尚未部署時不會啟用 DEV mock冒充成功。

- 所有 JSON body 明設 Content-Type；PS／OP lookup 使用真服務，沒有合成 `*` 區段或把未知欄位當0。
- Group DTO 嚴格讀取原始 nullable order／audit／stamp；表單將既有NULL order顯示空白，未改時傳回NULL。新增／Copy 的有效順序仍由使用者明確輸入，不從NULL默造0。
- POST／PUT先採用完整保存DTO、新父CAS與 audit，再做可能失敗的清單／明細GET；Apply後轉Edit。後續刷新失敗顯示「已儲存」並保留已知保存版本，不重做新增。
- 每筆明細新增／刪除接續上一筆回傳的父版本，同交易結果即時更新清單；任一錯誤立即停止，保留先前成功列並回報完成筆數。新增結果不明時鎖住再次提交，先重新載入真實明細及全域候選再操作；不盲目重送write。
- 明細載入具 sequence及 activeGroupNo雙重驗證，切換時清除舊列；晚回的A群組資料不能覆蓋B群組。明細按鈕依已讀取的父快照啟用，不把state0當明細維護的必要條件。
- 使用共用站內確認窗，保護未存表單／未加入選取項目；writing、lookup、確認期間主畫面及適用的dialog inert，Tab留在最上層dialog，保存期間防止焦點掉到框架。取消及保存後恢復有效焦點，卸載移除listener。
- 採KD深色primary `#3B3930`、淡黃色輸入與 `--yellow`焦點、`#F7F5EE`底色，維持同一套自適應上下雙Grid；僅Vite DEV且明確 developmentPreview=true 時使用獨立DEV資料。

## 原始碼索引

- WinForms：`Industry_Base/MESWin_Industry_Base/OP/OP_B_ECNOPGroupBasis/frmOP_B_ECNOPGroupBasis.vb`、`frmOP_B_ECNOPGroupBasisDef.vb`（Designer 與事件同檔）。
- Service：`Industry_Base/MESws_Industry_Base/wsOP/wsOP.cs` 的 Load/Add/Edit/Delete/Approve/Detail endpoints。
- Core：`Industry_Base/MEStc_Industry_Base/tcOP/clsOpBasis.cs`；DataStamp update：`Industry_Base/MEStc_B_Basis_UPD/clsB_Basis_UPD.cs`。

## 0054 正式資料庫套用實證（2026-11-20，API／GUI待驗）

本節接續上方保存的來源及草稿歷史；0054現已由root正式套用，不能再將原DRAFT註解當作live缺表的依據。原migration bytes未改，SHA-256仍為 `be53744a0177aaa4b422ddea8f45e93c6a33348d1e24f1d016490d33f470c6cd`。本節只記資料庫完成，不把schema當作API或GUI通過。

- root先以既有PPK讀回所有schema兩表ABSENT及名稱衝突0，再完成完整 `ymes` custom備份 `/opt/ymes/backups/ymes_before_0054_opb02_20261119T181813Z_kaHec4fs.dump`（624893 bytes；目錄700／檔600）；`pg_restore --list` 成功且1780個物件條目後，才以原交易再次guard並CREATE／GRANT／COMMIT。
- 獨立schema／ACL讀回：兩真表、共13欄、兩PK、兩PK index、user trigger0、欄位差異0、ACL差異0。Basis仍11欄，PSNo與IssueState NOT NULL且無default；Order真nullable無default，DataStamp nullable DEFAULT0。Detail只兩欄、全表OPNo PK，沒有child版本／audit、額外index或FK。
- app的Basis CRUD及Detail SID已就位；Detail只有opno欄UPDATE供row lock，沒有表UPDATE或opgroupno UPDATE。`SET LOCAL ROLE ymes_app` 的兩表 `WHERE false` SELECT probe通過；未以READ ONLY執行FOR UPDATE，也未讀業務列來冒充metadata驗證。
- 沒有seed、DROP、建立Job表或合成全域`*` PS目錄；原先workflow候選不存在的唯讀結果不等於完整IssueManagement provider已交付。

套用時正式API／Web仍是 `16a825af0bea`／`722bc259cc81`，未部署OPB02；新API版本、登入／路由與DI、OP實刪引用鎖、八組受gate回歸、Vue正式接線及app-browser逐項操作仍待下一步驗證。只有另行完成部署與真實驗證後才更新這些狀態，不借其他模組的已驗結果。

本機正式記錄為 `.tools/opb02-migration-applied-20261120.json`；備份／metadata完整維護紀錄亦見 [資料庫連線與憑證處理](../../DATABASE_CONNECTION.md)。原生來源、舊草稿heading與未提供Job／通知／ACL的限制全部保留。

## 上版前修正與Web a2建置（2026-11-20，API／GUI仍待驗）

本節記錄本輪來源修正及root建置結果，不代表正式端已可使用OPB02。

- 群組勾選與active列同步：取消最後一筆會清除active及明細；取消active但仍有其他勾選時，改用仍選取的群組。即時篩選移除父列時一併移除其勾選、active及明細；明細顯示與刪除另驗可見父列及實際owner，避免操作已隱藏群組的舊明細。
- 修改主檔時，與原始PS完全相同的舊空字串／空白值可保留，不因trim驗證強迫重選；送出仍保留原始值。新增、Copy或變更為空白PS仍拒絕。真NULL的防禦性處理不代表放寬來源PS NOT NULL，也不以SQL改schema製造此案例。
- 群組順序number input改用明確的字串value／input處理。Vue number input的v-model會自動轉數字，原先後續呼叫trim可能拋TypeError；目前先保留輸入字串，驗證後才轉為數值或既有NULL，仍依0至999999整數及新增必填規則處理。

root的Web a2完整 `npm.cmd run build` 已實際exit0（含vue-tsc／Vite），206來源檔build前後hash一致；canonical為 `b00c7f40020ff23a12fff2f18e3c71d6d92e3102c76a0a72a368dfff8dca6528`，記錄於 `.tools/opb02-web-build-passed-a2.json` 及其prebuild receipt。a1成功後尚有上述順序修正，a1紀錄保留為歷史但不能用於本輪封裝；a1未封裝／部署。

API a1僅建置階段的非root執行使用者讀取檢查曾因compat候選映像的 `appsettings.json` 為0600而中止，未切換正式容器。Dockerfile已補映像內 `/app` 發佈目錄0755、非機密發佈檔0644的修正；新版映像的實際非root讀取／啟動及新image ID仍由root另行確認，不以Docker build或來源修正冒充runtime通過。來源快照、機密、備份及持久資料不因此放寬權限。

本節記錄時live仍為API `16a825af0bea`／Web `722bc259cc81`。Web a2只準備新輸出路徑的封裝設定，尚未capture／pack，等待新API actual ID；八組API回歸、app-browser真實逐項操作、測試清理及本批Git同步皆未宣稱完成。

## API a2正式回歸實證（2026-11-20，八組分兩輪；Web／GUI待驗）

root已將OPB02 API a2正式啟動為 `1d09e4febf482433d398a986a3d9458eca693324763a45cbcd468e0d791e2755`，ready200；相容回復映像為 `232cc6e27e34f08c1bb06ea35dc111ee8aa3917252e13e6e6bdbe0bb98c63df3`，仍含ECN引用防護。兩映像均由root通過實際nonroot使用者五個發佈檔讀取gate後才啟動main，沒有放寬主機來源／備份權限。API來源139檔、相容來源136檔；0054仍維持Basis11＋Detail2欄／兩PK／兩index／無user trigger，Detail全表OPNo PK及欄級UPDATE(opno)的最小權限沒有被測試改動。

### 分輪結果與失敗紀錄

| 執行 | 實際取得證據 | 收尾 |
| --- | --- | --- |
| `2FCE461D77A744BCBA78AD38E611EBF7`，scope `OB022FCE461D` | G1–6通過；G7並行請求發生HTTPS傳輸失敗而中止，沒有將其記為通過。原log維持整輪 `result=FAIL`，記錄148 HTTP／461斷言，含G7中止前已完成的部分檢查。 | 15個精確資料scope皆0、holder0、logout204。先核對既有提交及清理，再改用新scope補測，不盲重送原write。 |
| `4A1AE89BFC594FE58866353B07ED4665`，scope `OB024A1AE89B` | 只補G7、G8，兩組通過，31 HTTP／95斷言；沒有重跑G1–6或修改API程式。 | 相同15個精確scope皆0、holder0、原metadata保持、logout204。 |

完成組的聯集為G1–8，兩份執行紀錄合計179 HTTP／556斷言。此數字包含首輪G7中止前的檢查，不是單次八組一次全通，也不是去重案例數。傳輸失敗保留在原log，不當作API已回傳成功或悄悄刪除失敗證據。原始記錄為 `.tools/opb02-a2-regression-20261120.log` 及 `.tools/opb02-a2-regression-7-8-20261120.log`；root實際執行，本文依紀錄與root回報整理。

### 已驗API範圍

| 組 | 已通過的主要行為 |
| --- | --- |
| G1 | 未登入401；真PS／核准且PS相等的OP查找；主檔新增保存DTO／UTC actor、重複409、header-only Copy、query-key文字識別值及字面contains查詢。 |
| G2 | 必填nullable CAS；遺漏400、NULL≠0／stale409、真NULL更新到1；NULL日期／order保留、值域及max版本、無需bump的硬刪。 |
| G3 | Detail全表OPNo唯一、跨群組競爭；真OP核准條件、PS大小寫／前後空白及`*`字面相等；錯owner與歧義拒絕且不誤增父版本。 |
| G4 | 父狀態0／1／2／-1皆可逐筆Add／Delete明細，使用必填父CAS並連續採用回傳新版本；舊OP停用／消失仍可刪自己的成員，父max拒絕且零修改。 |
| G5 | 變更PS時核對全體成員，mismatch／缺master整交易拒絕；未改的舊raw／失效PS保存，新建或真正改PS只准真唯一state2。 |
| G6 | 實際catalog無流程候選時本地0→2；已核准soft-delete保明細、0／-1 hard-delete同交易；pending／未知狀態、raw alias／owner／orphan等拒絕並保留來源列。 |
| G7 | 同主檔雙新增及跨主檔同OP競爭；父先刪後Add無ghost、子先提交後舊父CAS刪除409，按真鎖等待確認順序。 |
| G8 | 新成員對真OP的SHARE保持到commit；OP停用後既有引用保留，實刪409保全列；移除自有ECN成員後OP可實刪，驗證整合的ECN引用防護。 |

兩輪cleanup的15個計數為Group、Member-by-group、GlobalMember-by-OP、OP、Type、PS、Property引用、OP105明細引用，以及FlowNode／EquipmentLocator／RecipeDetail／ESOP／ProcessAPS／ProductAPS Basis Detail／ProductAPS Setup Detail七個引用範圍；每項皆0，holder另為0。這是精確scope數，不稱為15張不同真表；同一Detail同時檢查父與全域OP兩方向。只使用預檢為空的自有短UUID鍵與actor／marker，不借原生業務列，不改schema／grant或清除其他scope。

### 未提供與尚未驗證

- 真catalog目前沒有已知Issue／Job候選，因此只取得本地無流程直接核准的API證據；「候選存在便503零修改」分支本輪未實際觸發，沒有為通過測試建立假Job表。完整Job map／Detail、pending callback／通知及ACL provider仍未提供。
- `*`案例只把自有legacy group／OP關係值設成`*`驗字面關係，沒有建立全域`*` PS master，不能宣稱已驗新建時的真`*`目錄。PS來源NOT NULL保持，未放寬schema硬造NULL。
- 正式Web仍是 `722bc259cc81`；OPB02 Web a2已封裝，206來源逐檔一致、公開6檔／2目錄及資產SHA／0755／0644、非root與exact版本guard的部署script已完成靜態／獨立peer檢查，但尚未切換。Web建置時API為舊16a，封裝及未來切換綁定新1d09，兩者分開記錄。
- root須先dryvalidate／套用API-only checkpoint（保留舊Web a3／Git395歷史），再親自部署Web、記錄實際新image、逐操作完成app-browser與精確GUI清理，之後正常合併／推送GitHub。本節不將API或封裝結果當作GUI／本批Git已完成，整體仍 `releaseComplete=false`。

## 正式GUI驗收與清理（2026-11-20，本批Git待推）

本節為上方建置／API待驗歷史之後的最新結果。root使用正式app-browser（IAB）自行Demo登入，在API `1d09e4febf482433d398a986a3d9458eca693324763a45cbcd468e0d791e2755`／Web `cfa5cf288359f1f3bbfdd5dfc9f2dbb5457f4ef0e6537d0fc38c698edb902fd7` 完成已實作操作。證據 `.tools/gui-proof-20261120/opb02-current.json` 有48個通過的驗收檢查點，**其中1項是SQL提交後清理讀回**；不是48次純GUI點擊，也不代表每個控件與所有狀態的笛卡兒組合都已測試。

### 真畫面操作

| 範圍 | 實際結果 |
| --- | --- |
| 主檔與Copy | A經GUI新增／套用，真鍵入Order10後再改11並連續套用，轉Edit及編號唯讀、建立／修改者與日期正常；重複Copy被拒、B與C經GUI header-only Copy且沒有複製明細。N／S經GUI新增，P經GUI Copy並補齊必填。六個群組均由GUI建立，沒有SQL預建父列。 |
| 真PS／OP查找與明細 | 真查找顯示同PS、已核准且全表未分組的O1／O2／O3／O5，排除不同PS的O4及未核准O6；實際多選加入三筆、選列取代勾選集合、刪除取消／確認、重新開lookup的全域排除及刪除後重新可選均通過。其他群組也不能選到已分組的OP。 |
| 選取與查詢 | 取消active但仍有勾選時回到有效選取群組；取消最後一筆會清active及異動目標。篩選隱藏父列即清明細／選取並停用子異動；精確查B及查無資料通過。成員仍屬舊PS時變更父PS被拒，原父及明細保持。 |
| 核准與刪除 | 取消核准確認不異動；實按核准令A由0→2，修改停用且三筆明細保留。取消刪除不異動，確認刪除使2→-1並保留三子；再確認硬刪清父子並釋放全域OP候選。一般批次刪除取消亦通過。這是本地無Job直接核准策略的真GUI操作。 |
| 版本衝突與部分成功 | 固定B／C畫面快照後，以自有SQL只增加C版本；GUI批次刪除呈現已完成1/2、stale拒絕並保留C，重新查詢後GUI成功刪C。N的畫面舊版本遇自有SQL版本增加後，GUI保存被拒且未蓋掉既有值。 |
| 舊NULL／空白PS | N先GUI新增，再以SQL51將自有stamp／order／日期設真NULL；GUI顯示空白日期與順序，Apply實際保存成功而未將NULL order／建立日期默寫0或現在時間。SQL53只將自有N的PS改為三空格，GUI修改其他欄仍保留raw PS；由此Copy時必須另選有效PS並填新順序，不能以舊值保留例外繞過新增必填。 |
| state1明細 | P先GUI Copy建立，SQL50只設其state1；畫面主檔修改停用、刪除／核准操作被拒，但真明細Add／Delete可用且保留state1。沒有透過Job流程進入pending，不能將此fixture當成原簽核流程通過。 |
| 字面`*` | S先GUI建立；SQL52只改自有S及OP5的舊關係值為`*`，沒有建立全域PS `*` master。GUI真lookup只出符合該字面關係的OP5、加入明細、修改主檔及刪明細通過；不將`*`視為所有PS，也不宣稱新建時的真PS `*`目錄已驗。 |
| 對話框與框架 | 主檔dialog正／反Tab留在dialog，dirty取消回到有效輸入、放棄後重開正確。Close回空白首頁、由選單重進與正式深路由reload皆通過。 |

以上NULL、raw PS、state1、字面`*`及stale版本是精確自有SQL設定；lookup的Type1／PS2／OP6共9列亦僅為SQL前置資料，不當作GUI核准。真正由GUI做的新增／Copy／套用／明細異動／本地核准／soft及hard刪除與可見錯誤回應，依各檢查點分開記錄。

### GUI先刪父子，再精確清理lookup

測試範圍固定 `GB02-1120-R8`，A／B／C／N／P／S六個父列全部經GUI刪除。root在執行SQL cleanup前，先獨立跑 `qa-opb02-gui-92-readback.sql`／`91-counts.sql`，確認Group、Detail-by-group、Detail-by-OP均已0，僅剩Type1／PS2／OP6共9列lookup；因此不是靠SQL清掉父子後冒稱GUI刪除成功。

其後root實跑 `qa-opb02-gui-90-cleanup.sql` 精確清理自有lookup，再獨立跑 `91-counts.sql`，**19項scope計數全0**；不稱為19張不同真表。前述兩輪API holder已各為0，本GUI沒有以他人scope補清理。關鍵畫面留存 `opb02-approved-with-members.jpg`、`opb02-empty-after-gui.jpg`，證據維持ignored本機檔案，不加入來源Git。

### 發行紀錄與保留界線

- root已先完成API-only checkpoint dryvalidate／apply，再部署Web並通過Docker、公開0755／0644、候選與運行容器nonroot及正常HTTPS JS／CSS MIME、位元組與SHA檢查；API保持1d09、ready200。之後Web-only updater亦已dryvalidate／apply通過，206份私有來源安裝於 `/opt/ymes/checkpoints/web-opb02-20261120-a2`，原API／migration0054及舊Web／Git歷史保留，沒有重啟服務或改DB。
- `browserLogs=[]` 只代表最後一次全新深路由reload範圍；先前互動logs未跨reload保留，不能宣稱整輪歷史console全部無錯。先前操作依實際可見結果與讀回驗證。
- 已驗主從操作及既有值相容情境，未測所有狀態／控件交叉組合。完整IssueJob／callback／通知與功能ACL provider仍未提供；實際catalog無候選，因此候選存在503分支仍 `NOT_EXERCISED`，沒有造Job表或假通知成功。
- 本批GUI已驗、精確清理完成；GitHub同步仍待root正常提交／推送。root本次fetch確認origin仍 `395a41a`，沒有公司新提交；這是交接時點，不是新的已推版本。整體仍 `releaseComplete=false`，待完成Git同步及最終紀錄再由主流程更新。

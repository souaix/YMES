# ECN 作業站群組設定（OPB02）

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
| 公司端狀態 | Vue DEV 假資料頁完成；Production API/schema 由家用端接手 |

## WinForms 來源與 Oracle 結構

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

所有端點須要求登入，並檢查 FunctionNo=`B_ECN OP GROUP BASIS` / FunctionKey=`OPB02`。API audit user/time 取自伺服器 principal；主檔修改、刪除停用及核准皆檢查 DataStamp，衝突回 409。

| 方法 | 路徑 | 用途 |
| --- | --- | --- |
| GET | `/api/ecn-operation-groups?issueState=all&opGroupNo=` | 依權限查群組；支援狀態及編號查詢。必要時套用使用者可見 PS 範圍。 |
| GET | `/api/ecn-operation-groups/{opGroupNo}/operations` | 明細及目前 OPName；明細按 OPNo PK。 |
| GET | `/api/ecn-operation-groups/lookups/production-segments` | 重用既有可見 PS lookup，不建立重複區段表。 |
| GET | `/api/ecn-operation-groups/lookups/operations?psNo=&issueState=2&notInAnyGroup=true` | 僅回相同 PS、已核准且不在任一 ECN 群組的作業站；帶 OPClass 顯示值所需欄位。 |
| POST | `/api/ecn-operation-groups` | 新增主檔，伺服器設定狀態 0 與 audit。 |
| PUT | `/api/ecn-operation-groups/{opGroupNo}` | 修改狀態 0 的名稱、PSNo、順序、說明；帶 DataStamp CAS。 |
| DELETE | `/api/ecn-operation-groups/{opGroupNo}` | 依 DB 目前狀態及 DataStamp：0/-1 刪明細及主檔；2 改 -1 並保留明細；1 回 409。不可將 body 的 IssueState 當授權依據。 |
| POST | `/api/ecn-operation-groups/{opGroupNo}/approve` | DataStamp CAS，透過 IssueNo `OP ECN GROUP BASIS` 的共用 Issue Job/簽核服務。 |
| POST | `/api/ecn-operation-groups/{opGroupNo}/operations` | 加入單一已核准、同 PS、全域尚未分組的 OP；UI 多選時逐筆呼叫。 |
| DELETE | `/api/ecn-operation-groups/{opGroupNo}/operations/{opNo}` | 實體刪除一筆群組明細。 |

## Vue DEV 資料與實作界線

`EcnOperationGroupMaster.vue` 僅在 Vite DEV 且 `developmentPreview=true` 使用 `DEV-` 假資料與瀏覽器 localStorage。核准假資料走「無簽核 Job、直接狀態 0→2」分支；正式端由 API 決定直接核准或進入 Issue Job。未連 YMES PostgreSQL、未實作 Production API/migration，也未執行 build、test 或 browser 驗收。

## 原始碼索引

- WinForms：`Industry_Base/MESWin_Industry_Base/OP/OP_B_ECNOPGroupBasis/frmOP_B_ECNOPGroupBasis.vb`、`frmOP_B_ECNOPGroupBasisDef.vb`（Designer 與事件同檔）。
- Service：`Industry_Base/MESws_Industry_Base/wsOP/wsOP.cs` 的 Load/Add/Edit/Delete/Approve/Detail endpoints。
- Core：`Industry_Base/MEStc_Industry_Base/tcOP/clsOpBasis.cs`；DataStamp update：`Industry_Base/MEStc_B_Basis_UPD/clsB_Basis_UPD.cs`。

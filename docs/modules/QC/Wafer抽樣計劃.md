# QC104 Wafer 抽樣計劃（抽樣計劃_元件）

## 功能與來源

- 選單：品質資料 → 基本資料 → 抽樣計劃_元件；FunctionNo `WAFER SAMPLING PLAN`、FunctionKey `QC104`。
- WinForms：`STD/MESWin/QC/QC_SamplingPlan_Wafer/frmSamplingPlan_Wafer.vb`、`frmSamplingPlanDef_Wafer.vb`。
- Service/Core：`STD/MESws_STD/wsSTD/wsQC/wsQC.cs` 的 Load/Add/Edit/Del/Approve；`STD/MESkc/kcQC/clsQCBasis.cs`、`STD/MESkc/kcBasis_UPD/clsBasis_UPD.cs`。
- Oracle source DDL：`database/reference/mestest_qc_sampling_plan_wafer_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_wafer_create.sql`。CREATE 檔只是參考，不可直接在 MES Oracle 執行。

## WinForms 清單畫面

- Form 標題 `SAMPLING PLAN WAFER`，ClientSize 1008×591。狀態篩選 0 未簽核、1 簽核中、2 已簽核、-1 不使用；預設未簽核。條件查詢只提供 PlanNo。舊表單先全載入，再於前端套狀態篩選並按 PlanNo 排序。
- 按鈕：ADD、EDIT、DEL、Copy、Approve、View、Close。EDIT 只允許 IssueState=0；Copy 可從選取記錄開啟副本定義；View 為唯讀。
- 清單顯示 PlanNo、SampleType、SampleRate、EveryNum、SamplePieceNum、SamplePointNum、Creator/CreateDate、Reviser/ReviseDate。IssueState、DataStamp、FunctionName、AQL 欄位等其餘資料不顯示。日期格式 `yyyy/MM/dd HH:mm:ss`。
- 可多選刪除或核准；逐筆處理，第一筆失敗即停止，之前已成功項目不回復。

## 定義畫面與輸入

- Form 標題 `Wafer Sample Plan Define`，ClientSize 632×690。區塊包含 Sampling Plan、Sample Draw Out、Sample Area、Basis Data；底部 Confirm、Close、Apply。
- PlanNo 最長 25 字元；離開欄位時轉大寫。Edit 時唯讀；Copy 時允許輸入新 PlanNo。Creator/CreateDate、Reviser/ReviseDate 顯示稽核資訊。
- 可見抽樣類型及代碼：0 by Rate；1 by Wafer Count；2 by Fixed Piece；3 by Range；5 by 105E AQL；6 by GB2828 AQL。舊 Core 還保留 SampleType=4 User Define 寫入分支，但這個定義窗沒有對應 radio option；規格與 DEV 頁不把它當成可建立的新類型，讀到既有 type 4 時保留其值並提示舊型別。
- by Rate：SampleRate 必填整數，1–100，單位百分比。
- by Wafer Count：EveryNum、SamplePieceNum 均為正整數；SamplePieceNum 不可大於 EveryNum。
- by Fixed Piece：SamplePieceNum 為正整數。
- by Range：至少一筆範圍。LowerLimit、UpperLimit、SampleNum 必須為正 Int32 整數；LowerLimit ≤ UpperLimit；SampleNum ≤ UpperLimit。舊碼沒有啟用「SampleNum 必須落在 LowerLimit 與 UpperLimit 間」的註解驗證，也沒有檢查範圍重疊，Web 不額外加此規則。
- 105E/GB2828 AQL：AQLType、QCLevel、AQL 均必填。AQLType、QCLevel、AQL 選項由 `wsSYS.LoadMesParameterRef` 取得，ParameterNo 分別為 `AQLType`、`QCLevel`、`AQLData`、ParameterType=0；依類型只顯示 `105E%` 或 `GB2828%` 開頭的 AQLType。
- Sample Draw Out 使用 Fixed Point Per Piece。SamplePointNum 必須為正整數；RejectQty 選填，舊表單只在輸入為數字時傳送。
- Sample Area：Area 必填、最多 20 字元且不可重複；AreaNo 空白時存 `0`，最多 50 字元。Enhance 未勾選時 AreaType=0 General，勾選時 AreaType=1 Enhance。可以新增與勾選刪除多列。
- Apply 儲存後留在視窗，Confirm 儲存並關閉。Edit 時 PlanNo 不可改，資料戳使用讀取時舊值比對；Copy 建立新主檔、狀態回到 0。

## 狀態、簽核與刪除

- 新增 IssueState=0。Approve 只接受未簽核資料；Issue Job Map 的識別字串為 `WAFER SAMPLING PLAN`。沒有 Job/Detail 時直接 0→2；有簽核流程則建立 Issue、狀態轉 1，寄信通知第一簽核群組；流程核准轉 2，退回回 0。
- IssueState=0 或 -1 刪除時，單一 Core 呼叫依序刪除 `TBLQCSAMPLINGRANGE`、`TBLQCSAMPLINGAREA_WAFER`、`TBLQCSAMPLINGPLAN_WAFER`。IssueState=2 不實體刪除，以 DataStamp CAS 改成 -1；IssueState=1 拒絕刪除。
- 核准、已簽核停用及編輯會以 DataStamp 舊值更新，並維護 Reviser/ReviseDate。來源 DataStamp 是 nullable `NUMBER(11,0) DEFAULT 0`，API CAS 要分辨 NULL 與 0。
- `TBLQCSAMPLINGRANGE` 是 QC103/QC104 共用名稱的範圍表；來源沒有 PK、索引或 FK。舊 Core 只用 PlanNo 讀寫/刪除範圍，沒有 FunctionNo 維度。家用 API/schema 必須先核對共用表既有使用及 YMES 擴充，避免 QC103 範圍資料互相影響；不要自行新增 Oracle 關聯。

## Oracle 結構（唯讀查詢）

### `TBLQCSAMPLINGPLAN_WAFER`

| 欄位 | 型別 | Nullable / 預設 |
| --- | --- | --- |
| PlanNo | varchar2(25) | NOT NULL，PK |
| SampleType | number(1,0) | NOT NULL |
| SampleRate | number(3,0) | NOT NULL DEFAULT 0 |
| EveryNum | number(4,0) | NOT NULL DEFAULT 0 |
| SamplePieceNum | number(4,0) | NOT NULL DEFAULT 0 |
| SamplePointNum | number(6,0) | NOT NULL DEFAULT 0 |
| Creator / Reviser | varchar2(10) | NULL |
| CreateDate / ReviseDate | DATE | NULL |
| IssueState | number(1,0) | NOT NULL DEFAULT 0 |
| FunctionName | varchar2(50) | NULL |
| AQLType | varchar2(25) | NULL |
| AQL | varchar2(10) | NULL |
| QCLevel | varchar2(6) | NULL |
| RejectQty | number(4,0) | NULL |
| DataStamp | number(11,0) | NULL DEFAULT 0 |

僅 PlanNo PK unique index；沒有 FK 或其他 index。

### `TBLQCSAMPLINGRANGE`

| 欄位 | 型別 | Nullable / 預設 |
| --- | --- | --- |
| PlanNo | varchar2(25) | NOT NULL |
| LowerLimit / UpperLimit / SampleNum | number(12,0) | NOT NULL DEFAULT 0 |

沒有 PK constraint、索引、FK；資料以 PlanNo 定位。

### `TBLQCSAMPLINGAREA_WAFER`

| 欄位 | 型別 | Nullable / 預設 |
| --- | --- | --- |
| PlanNo | varchar2(25) | NOT NULL |
| Area | varchar2(20) | NOT NULL |
| AreaType | number(18,0) | NOT NULL DEFAULT 0 |
| AreaNo | varchar2(50) | NULL DEFAULT '0' |

複合 PK `(PlanNo, Area)`，只有該 PK unique index；沒有 FK/其他 index。

## QC104 API/schema handoff

- FunctionKey `QC104` 必須由登入使用者權限保護。
- `GET /api/qc/sampling-plans/wafer?searchFieldName=PlanNo&searchFieldValue=` 回傳主檔欄位與 audit/DataStamp；UI 依 IssueState 本地篩選，和舊表單相同。
- `GET /api/qc/sampling-plans/wafer/{planNo}/details` 回傳 `{ ranges, areas }`；AQL options 由 `GET /api/qc/sampling-plans/wafer/aql-options` 提供 `{ aqlTypes, qcLevels, aqlValues }`，值從對應 MES parameter ref 來源取得。
- `POST /api/qc/sampling-plans/wafer` 建立主檔與 range/area 明細；`PUT /api/qc/sampling-plans/wafer/{planNo}?issueState=&dataStampIsNull=&dataStamp=` 編輯整份 aggregate。主檔及兩種明細需同一 DB transaction，避免部份保存。
- `DELETE /api/qc/sampling-plans/wafer/{planNo}?issueState=&dataStampIsNull=&dataStamp=` 依狀態執行實體刪除或 CAS 停用；`POST /api/qc/sampling-plans/wafer/{planNo}/approve?dataStampIsNull=&dataStamp=` 套用 Issue Job 流程。
- SQL 必須參數化。PostgreSQL 空表 CREATE 是參考；家用端先查 YMES schema/migrations。尤其 `tblqcsamplingrange` 可能已供 QC103 使用，若存在不可 drop/recreate 或丟失原有資料/schema；先決定共用儲存/API 的兼容方式，再新增 forward migration。

## Oracle DDL 查詢紀錄

- 2026-11-17 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機既有受保護設定直接唯讀取回三張表的 DBMS_METADATA table/index DDL、ALL_CONSTRAINTS；沒有讀 MES 業務列，沒有在 Oracle 執行 CREATE/UPDATE/DELETE。
- 主檔及 Area 表各有 PK unique index；Range 表沒有索引或主鍵。三表都沒有 FK。連線資訊不輸出、不寫入 Git。

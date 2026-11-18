# QC408：OCAP基本資料

## 功能識別

| 項目 | 值 |
| --- | --- |
| MES 功能碼 | QC408 |
| FunctionNo | `OCAP BASIS` |
| 選單 | 品管資料 → 異常設定 → OCAP基本資料 |
| 主 Form | `frmOCAPBasis_THEIL_BU1` |
| 編輯 Form | `frmOCAPBasisDef_THEIL_BU1` |
| 流程圖 Form | `frmOCAPProcessDraw_THEIL_BU1` |
| 節點群組選擇 Form | `frmNodeOCAPDef_THEIL_BU1` |
| MES service | `wsQC.LoadOCAPBasis`、`AddOCAPBasis`、`EditOCAP`、`CopyOCAP`、`DelOCAP`、`ApproveOCAP`、`UpdOCAPNode` |
| Core | `clsQCBasis`、`clsBasis_UPD` |
| Vue 頁面 | `web/src/components/QcOcapBasisMaster.vue` |
| Vue 路徑 | `/YMES/QC/OCAPBasis` |
| YMES API | 待家用電腦實作，契約見本文下方 |

本規格以 `D:\iMES\CSiMES\Customer\Production\MESWin\QC\QC_OCAPBasis_THEIL_BU1` 中四個 WinForms Form/Designer、`STD\MESws_STD\wsSTD\wsQC\wsQC.cs`、`STD\MESkc\kcQC\clsQCBasis.cs` 及 `STD\MESkc\kcBasis_UPD\clsBasis_UPD.cs` 為依據。圖形序列化使用編譯後的 `iMESXMLFlow.dll`，目前舊 MES 原始碼庫沒有該元件的 serializer 原始碼。

## 畫面與操作

### 主清單

- WinForms 主視窗 ClientSize 為 1008×591；標題為 `OCAP Basis`。上方用四個狀態選項切換未簽核(0)、簽核中(1)、已簽核(2)、不使用(-1)，另有 OCAP 編號查詢欄。
- Grid 為單一資料列區，不顯示 GroupBy 區。資料按 `OCAPNo, OCAPVersion` 排序。欄位依序為 OCAP No、OCAP Version、Cur. Version、Description、Creator、CreateDate、Reviser、ReviseDate；時間顯示到秒。`IssueState`、`DataStamp`、`OCAPXmlString` 隱藏。
- 選取資料列會以 `OCAPXmlString` 還原 AddFlow 圖形作右側流程預覽；無 XML 時不顯示流程。流程 XML 不是 Grid 可見欄位。
- 主畫面操作：新增、修改、刪除、核准、複製、Draw、關閉。修改及 Draw 僅允許狀態 0；核准僅允許狀態 0 且已有流程 XML；刪除支援多選。複製可從選取版本建立另一個編號/版本。

### 新增/修改/複製

- 編輯視窗為 625×335，欄位為 OCAP No、OCAP Version、Description，並有建立/修改人與時間稽核欄。
- OCAP No 與 Version 必填；離開欄位時轉成大寫。資料表長度分別為 50、5、255；Description 可空白。
- 新增：使用者輸入 No、Version、Description；Creator/CreateDate 由 MES 設定；新增狀態為 0、CurVersion 為 0。版本不由畫面自動推算，需人工輸入。
- 修改：只有狀態 0 可進入；OCAP No、Version 鎖定，只修改 Description。更新帶 DataStamp，Reviser/ReviseDate 由 server 更新。
- 複製：來源 No/Version/Description 預填，目的 No/Version 可編輯；`wsQC.CopyOCAP` 複製來源流程 XML、節點與連線，並重建目標 NodeID；新資料為未簽核版本。目的鍵不得與既有 `(OCAPNo, OCAPVersion)` 重複。
- 確定儲存後關閉；套用儲存後保留編輯視窗。建立與複製表單會允許繼續輸入下一筆。

### 核准與刪除

- 核准可多選，但每筆都必須是 IssueState=0 且 `OCAPXmlString` 非 NULL。WinForms 呼叫 `wsQC.ApproveOCAP`，由 Core 直接核准，不走 Issue Job。核准某版本時，先將同一 OCAPNo 的所有版本 `CurVersion=0`，再將該筆設成 `IssueState=2, CurVersion=1`，並更新稽核與 DataStamp。
- 刪除支援多選，逐筆處理；首筆失敗即停止。狀態 0 或 -1 會硬刪主表、節點表及連線表資料。狀態 2 不硬刪，改為 `IssueState=-1, CurVersion=0` 並更新 Reviser、ReviseDate、DataStamp。狀態 1 不允許刪除。
- 刪除與核准前會提示確認。API 應以交易涵蓋所有相關變更，且不可讓主表/子表留下半套資料。

### 流程繪製與節點

- `Draw` 只允許狀態 0。舊工具列提供開始/結束、OCAP 節點、一般/垂直水平/水平垂直/多段折線連線、AND/OR 群組、群組/解除群組及自動排列等操作。
- 節點類型數值：OCAP=0、Process=1、AND 群組=6、OR 群組=7、START=8、END=9。節點表還記錄 NodeNo、NodeID、OCAPNo/Version、OCAPGroupNo。
- OCAP 節點的群組選擇 Form 透過 `wsQC.LoadHoldDispGroupBasis` 查詢異常處理群組，要求 GroupNo 存在並帶回 GroupName；Production API 應使用已簽核群組選項，不接受任意不存在的 GroupNo。
- 儲存圖形時，舊程式把 AddFlow 物件序列化為 `OCAPXmlString` CLOB，並同步產生 NodeBasis/NodeRelation 明細。已有 NodeID 盡量保留，新節點採 `{OCAPNo}-{OCAPVersion}-{四位流水序號}`；群組 ID 使用 `GROUP{索引}`。一般連線的端點 NodeID、NodeNo、LinkName 寫入 relation 表；群組內部的 rigid link 不列入一般關聯。
- 舊程式會檢查群組節點的連線限制，以及主流程中 Process 節點 Stage 順序。完整規則與 AddFlow XML 格式尚需以 `iMESXMLFlow.dll`/既有樣本確認；不可由 Vue 的視覺位置推斷流程順序。
- 節點圖和三張表必須在同一個資料庫交易內更新。繪圖視窗傳入原 DataStamp，並呼叫 `wsQC.UpdOCAPNode`。

## Oracle 資料表與來源 DDL

本功能直接使用下列表，Oracle DDL 由 2026-11-18 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過既有受保護連線唯讀取得：

| Oracle 表 | 用途 | 主鍵 | 主要欄位/注意事項 |
| --- | --- | --- | --- |
| `MESTEST.TBLQCOCAPBASIS` | OCAP 版本主檔與完整 AddFlow XML | `(OCAPNO, OCAPVERSION)` | `OCAPXMLSTRING CLOB`；`ISSUESTATE NUMBER(1,0) DEFAULT 0 NOT NULL`；`CURVERSION NUMBER(1,0) DEFAULT 0 NOT NULL`；`DATASTAMP NUMBER(11,0) DEFAULT 0` 可 NULL；建立日期預設 SYSDATE。 |
| `MESTEST.TBLQCOCAPNODEBASIS` | 流程節點 | `(OCAPNODEID)` | NodeNo、NodeType、OCAPNo/Version、可空 OCAPGroupNo。 |
| `MESTEST.TBLQCOCAPNODERELATION` | 流程連線 | `(FROMNODEID, TONODEID, OCAPLINKNAME)` | 端點 No、OCAPNo/Version 可空；OCAPLinkName 雖無欄位層 NOT NULL，因屬 PK 實際不可 NULL。 |

- Oracle 原始 DDL：`database/reference/mestest_qc408_ocap_source_ddl.sql`。
- PostgreSQL 空表 CREATE 對照：`database/reference/mestest_qc408_ocap_create.sql`。
- 三表只有 PK unique index；DDL 未發現 FK 或額外索引。資料模型仍以 OCAPNo/Version 關聯明細，但不要自行新增來源沒有的 FK。
- Oracle CLOB 在 YMES PostgreSQL 參考中使用 `text`；Oracle DATE 使用 `timestamp without time zone`。DataStamp DEFAULT 0 但 nullable，請保留來源 NULL 語義並在 API CAS 處理舊 NULL 值。
- Oracle 查詢僅讀取資料字典/DDL，未讀業務列，也未在 Oracle 執行 DDL。連線秘密不寫入 Git。

## Vue DEV 頁目前行為

`QcOcapBasisMaster.vue` 掛載於 QC → 異常設定 → OCAP基本資料，使用 KD 黃色輸入欄、標準灰色 Grid 表頭、橘色主要按鈕。DEV 模式以標明 DEV 的假資料及 localStorage 運作，不連 MES Oracle 或 YMES PostgreSQL。

- 已提供四狀態篩選、編號查詢、排序、列選取/多選、資料筆數、稽核欄、建立/修改/複製、套用/確定、核准、刪除、流程預覽與流程編輯器。
- 簡化的網頁編輯器可新增 START/END、OCAP、AND/OR 節點；編輯節點編號/GroupNo、增加/移除 pass/fail/是/否連線。流程繪圖是節點卡片及直線連線，不含 WinForms AddFlow 的拖曳路徑編輯、自動排列、巢狀群組成員操作、完整群組連線限制、Process Stage 順序驗證及原生 XML 相容轉換。
- 群組編號目前是文字輸入，DEV 不呼叫 HoldDispGroup API picker。Production 接 API 前需改為已簽核 GroupNo 選擇器並實作 server 驗證。
- 目前 Vue flow payload 是結構化 nodes/links；資料表仍須存舊 `OCAPXMLSTRING`。`iMESXMLFlow.dll` 目前只有編譯檔，API 開發前需反編譯/建立格式相容 serializer，或由產品決定 YMES 僅使用自有 JSON 並記錄與 MES XML 不相容。不可把 Vue payload JSON 直接冒充舊 AddFlow XML。

## YMES API 契約交接

API 必須使用登入使用者身分與 FunctionNo=`OCAP BASIS`/FunctionKey=`QC408` 做授權。所有 SQL 參數化；寫入 audit 欄位使用 server 使用者與 server 時間。DataStamp 是並行控制值，發生競爭回 409 並要求重新載入。

| Method / route | 行為 |
| --- | --- |
| `GET /api/qc/ocap-basis?issueState=&ocapNo=` | 查詢所選狀態與 OCAP No 條件；以 OCAPNo、OCAPVersion 排序；回傳稽核欄、IssueState、CurVersion、DataStamp 及可供預覽的解析後 flow。 |
| `GET /api/qc/ocap-basis/lookups/hold-groups` | 回傳已簽核 HoldDispGroup 的 GroupNo/GroupName，供節點群組選擇。 |
| `POST /api/qc/ocap-basis` | 新增 OCAP No/Version/Description；Creator/CreateDate、狀態及 DataStamp 由 server 設定。 |
| `PUT /api/qc/ocap-basis/{ocapNo}/{version}` | 僅修改 Description；只允許 IssueState=0；要求 DataStamp。 |
| `POST /api/qc/ocap-basis/{ocapNo}/{version}/copy` | body 提供目標 OCAP No/Version；在單一交易複製主檔描述、XML、節點及連線並重編 NodeID，目的狀態 0、CurVersion 0。 |
| `PUT /api/qc/ocap-basis/{ocapNo}/{version}/flow` | body 帶 DataStamp、nodes、links；驗證狀態 0、群組、節點/連線及流程順序，產生相容 XML，交易更新 CLOB 和兩張明細表。 |
| `POST /api/qc/ocap-basis/{ocapNo}/{version}/approve` | 僅允許狀態 0 且存在有效流程；交易清除同 No 的 CurVersion，再核准指定版本並設為目前版本。 |
| `DELETE /api/qc/ocap-basis/{ocapNo}/{version}?issueState=&dataStamp=` | 0/-1 硬刪主/節點/連線；2 軟停用為 -1 並清 CurVersion、更新 audit；1 拒絕。狀態/DataStamp 必須與目前資料吻合。 |

前端 API URL 與 method 寫在 Vue component；Production 未接通前仍只能使用 DEV localStorage 假資料。API 回傳錯誤不得吞掉；Unauthorized 按現有登入流程處理。

## 家用端建置順序

1. 先讀 `docs/DATABASE_CONNECTION.md` 及 `todolist_homepc.md`，核對 YMES live schema/migrations。只在缺表時建立 forward migration，禁止對 MES Oracle 執行 CREATE。
2. 比對三張表的既有 migration、schema、索引與 NULL/default；新 migration 欄位/PK 依 `mestest_qc408_ocap_create.sql`，不要補來源沒有的 FK/index。
3. 查清楚 AddFlow XML 與節點、relation 的一致性要求，再實作 flow serializer/parser 和原子交易；先確認是否要求和舊 MES 共用 XML。
4. 實作授權、狀態/DataStamp 條件、三表 CRUD/copy/approval、已簽核 HoldDispGroup lookup、錯誤回應，再由公司端 pull 驗收 UI/API 整合。

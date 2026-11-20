# 作業員登入／登出（OP／Area）

## 功能身分與來源

| 項目 | 來源 |
|---|---|
| WinForms | `STD/MESWin/WIP/WP_OperatorLogin/frmOperatorLogin.vb`（表單與事件在同一檔） |
| Service | `wsUSR.LoadUserBasisJoinLoginState`、`wsUSR.UpdOperatorLogin`、`wsUSR.UpdOperatorLogout`、`wsOP.LoadOPBasis`、`wsSMD.LoadAreaBasis` |
| Core | `STD/MESkc/kcUSR/clsWorkTime.cs`：`LoadUserBasisJoinLoginState`、`Add/DelOperatorLoginState`、`Add/EditOperatorLoginLog`、`GetOperatorLoginLog` |
| DEV 頁 | `/YMES/WIP/OperatorLogin`，`web/src/components/WipOperatorLogin.vue` |

WinForms 透過外部注入 `PrivFunNo`，Form source 未包含正式數值 FunctionKey/FunctionNo。DEV fallback `OPERATOR LOGIN` 依程式／視窗標題推導，正式選單權限需由 menu metadata 確認。這是使用者在 MES 現場指定作業登入狀態，與系統網站登入、WP104/WP105 生產上工下工是不同流程。

## 畫面

- 464×325 WinForms Form，預設 title `Operator Login`，輸入已登入員工後切換 `Operator Logout`；初始 group title `Operator Login/Logout`。
- 上方 Group：Employee No、唯讀 Employee Name、Shift No、CurrentTime。
- 下方 Group：`Login OP`／`Login Area` radio（二選一，預設 OP）、地點欄位及 `.....` 搜尋、Work Day 日期、唯讀 Login Date。選 OP 時 Area 欄與 lookup disabled；選 Area 時 OP 欄與 lookup disabled。
- 底部 `Confirm`、`Close`。Form_Load 預載有效 OP Basis、Area Basis、清空表單、套用語系與 Function 群組權限；沒有密碼欄。
- DEV 假資料：`DEV-EMP-01` 預設未登入，`DEV-EMP-02` 預設已登入。localStorage 儲存假登入狀態／歷程，不呼叫正式 API。

## 查詢與操作邏輯

1. Employee No 按 Enter 後轉大寫，Service 以 UserNo 載入 User、Shift、FromTime/ToTime 及可選的 `USROperatorLoginState`。Core 限 `UserType=0`、User/Shift `IssueState=2`，以 DepartmentNo 對應 Shift，並帶出 LoginSerial、LoginType、LoginArea、WorkDay、LoginDate。不存在員工／班別、ShiftNo 空值或班別時間空值時拒絕載入。
2. 有 LoginSerial 表示已登入：標題改 `Operator Logout`，還原登入類型、地點、WorkDay、LoginDate，鎖定 Location/WorkDay。無現況表示登入：標題為 `Operator Login`，預設 Login OP，顯示目前時間；班別同日預設今天，跨日班別沿用 WinForms 的 FromTime/ToTime 判斷預設 WorkDay。
3. 登入前必填 OP 或 Area。OP 必須存在於 Form 載入的 OP Basis；Area 必須存在於 Area Basis（`AreaClass=0`）。LoginType 0 表 OP、1 表 Area。WorkDay 只能在預設班別日的前後一天。
4. `UpdOperatorLogin` 由 Core 產生 LoginSerial，以 AP Server `DateTime.Now` 新增 `tblUSROperatorLoginState` 與 `tblUSROperatorLoginLog`，再執行同一組 SQL。Request 傳 UserNo、LoginType、LoginArea、ShiftNo、WorkDay；不接受 browser 自帶 LoginSerial／LoginDate。
5. 登出時以目前 LoginSerial 查 LoginLog 的 LoginDate，AP Server 計算 WorkingHour；刪除該 UserNo、LoginArea 的 LoginState，並更新 LoginLog 的 LoginState、LogoutDate、WorkingHour。登出身份和目標員工是兩個概念，Production audit actor 必須從 API 已驗證的 session 取得。
6. 成功後清空員工欄位和條件。來源登出成功提示文字仍使用 LOGIN 字樣，是原 Form 顯示瑕疵；DEV 結果訊息依實際操作顯示登入或登出。

## Oracle DDL 狀態與 API handoff

- Core 寫入的表為 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`。公司端使用受保護 Oracle profile 直連 MESTEST，以 `ALL_TABLES`、`ALL_SYNONYMS` 查詢；兩表在 MESTEST 及目前連線可見 schema/synonym 均未找到。`DBMS_METADATA.GET_DDL` 因物件不存在而沒有可用 source DDL；不得從程式 INSERT/UPDATE 欄位推測並產生正式 PostgreSQL CREATE。
- 查詢紀錄及 `TABLE NOT FOUND` 輸出：`database/reference/mestest_usr_operator_login_metadata.txt`、`mestest_usr_operator_login_source_ddl.sql`。查詢 script：`fetch_usr_operator_login_ddl.ps1`、`lookup_usr_operator_login_owners.ps1`。只查 Oracle 結構字典，沒有讀業務資料列或執行 DDL/DML。
- 家用端先核對 YMES live PostgreSQL schema/migrations 是否已有對應 LoginState/LoginLog；若已有，明確映射並重用。若沒有，需先取得正確 Oracle 環境的正式 DDL/metadata，再建立 PostgreSQL migration/Create SQL。此項缺表來源是目前真實阻塞，不能用猜測結構代替。
- API 建議：登入授權查詢 User/Shift/current login state；POST login 只接受 target UserNo、LoginType、validated LoginArea、允許範圍內 WorkDay。Server 重查 User/Shift/Basis、location existence、WorkDay ±1 與登入現況，產生 serial/time，原子新增 State/Log。POST logout 只接受 LoginSerial；server 以鎖定現況驗證 UserNo/Location，計算工時並原子更新/刪除。保留「刷入員工」的現場工作模式，但另驗證 terminal/operator 權限，避免僅憑 browser 任意 UserNo 替人登入登出。
- 公司端僅完成 Vue DEV 與 source/spec；Oracle 新表 DDL 未找到、未連 YMES PostgreSQL、未建立正式 API/migration，也未執行 build/test/browser。

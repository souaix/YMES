# Codex handoff：公司筆電 → 桌電

更新：2026-11-19。用途：在公司端桌電／筆電 Codex 開啟 YMES repository 後直接接續，不必重做已完成調查。

## 接手方式

1. 在桌電開啟同一個 GitHub repository `souaix/YMES`，執行 `git pull origin main`。
2. 先讀本檔、`todolist_officepc.md`、`todolist_homepc.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md` 及目前的 [WP601 包裝作業規格](modules/WIP/包裝作業.md)。
3. 本機 Codex 使用 YMES repo；MES WinForms 原始碼在公司筆電 `D:\iMES\CSiMES`，桌電可能是另一個路徑。若桌電有既有 MES checkout，先核對路徑；不可用 UI 截圖代替 Form/Designer/Service/Core 原始碼。原始碼缺少時記錄具體缺檔，先做不依賴它的工作。
4. 若在桌電打開同一個 Codex 對話，thread 對話與 Goal 脈絡仍在；若另開新對話，把本檔及 TODO 當成接手上下文，不要假設新 thread 自動有舊 Goal 狀態。

## Repository 與分工

- Repository：`D:\YMes`（桌電可用不同 clone 路徑）；remote `https://github.com/souaix/YMES.git`，branch `main`。
- Vue 3 UI：`web/`。
- .NET 9 API：`src/YMes.Api/`。
- 共用 DTO：`src/YMes.Contracts/`。
- `src/YMes.Web/` 是既有 ASP.NET MVC 專案，不是目前 Vue UI。
- 公司端以 DEV 假資料完成 Vue 畫面/互動並記錄規格；家用端負責 PostgreSQL schema、API、migration/Create SQL handoff。DEV localStorage 不送入正式 API。

### 桌機與筆電的職責

- 公司桌機和公司筆電屬於同一個「公司端」，工作範圍相同：讀 MES WinForms/Service/Core、只讀查 Oracle MESTEST DDL/metadata、以 DEV 假資料完成全部 Vue UI/互動邏輯、整理規格與 API/SQL handoff。
- 公司端不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不建 PostgreSQL schema/migration、不實作或驗收家用端 API、不 SSH 或遠端部署。這些全部由家用電腦接手，依 `todolist_homepc.md` 執行。
- 公司桌機為 Vue DEV 安裝 Node.js 22.x（22.12+）與 npm，並安裝 `web/` 套件即可。桌機上的 .NET 9 SDK 和 API/PostgreSQL 連線不是公司端 UI 工作的前置條件；不要為了跑 API 在公司電腦設定 PostgreSQL 憑證。
- 公司端允許使用的是 MES 原生 Oracle MESTEST 受保護唯讀連線，僅用於查 DDL/資料字典；不在 Oracle 寫入資料。

## 目前檢查點與下一步

- 公司端已接續 OE101、OE102、OE201 至 OE209、OEB01、OEP01 的 DEV 訂單與工單操作。各功能及 Oracle DDL／API 交接見模組索引。WP101、WP102、WP201、WP202、WP204、WP601、WP603、WP604、WP605、WP629 已完成公司端 DEV 與家用端 handoff；後續工作依 `todolist_officepc.md` 及 MES menu inventory 接續。

- PD102 DEV 子視窗已分段完成 Property、Parameter、Business Rule、OPDescription、RecipeGroup、Accessory、Resource、SubOP、Program、Default、RunTime、QC Item。各功能的 WinForms 證據、DDL 及 API handoff 在 `docs/modules/PRD/` 與兩份 TODO。
- RunTime 已完成單列／表格編輯與套用。QC Item 已完成依 OP 指派與檢驗類型，QC Import／QC Chart 的 Excel 流程待接續。兩項 DDL 依 MESTEST 字典實查重建，Production API 由家用端接續。
- Packing Set、EQPCapItem、eSOP、EquipmentSpec、APS Integration 和 Bin 已完成 DEV 設定與 Oracle 交接。`GBOMImport`／`GBOMExport` 已完成 DEV 備份操作與客製表交接，工作簿解析和資料匯入待正式接入。

## Default 已核實事項

- WinForms 入口 `btnDefault_Click` → `frmProductLoadDefault` → `wsPRD.UpdPRDOP_BatchDefault` → `clsProduct.UpdPRDOP_BatchDefault`。
- `Product Process OP` 對應 `DefaultOP=2` 且預設選取；`All PS OP` 對應 `DefaultOP=1`。父畫面沒有產品流程時不開表單。
- 複製旗標：Parameter、BusinessRule、Description、OPResource、QCItem、SubOP、APS。產品 IssueState 非 0 時 Checkbox/Apply disabled，Radio 仍可選。APS 受 `ExecuteAPS` 顯示控制但 Designer 預設 checked；QCItem 初始狀態由 `PRDDefault_QCItemAutoChecked` 控制。
- Oracle APS product table 字典是由 Windows PowerShell 5.1 + ODP.NET 直連唯讀取得。`DBMS_METADATA.GET_DDL` 回 ORA-03135；ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_CONS_COLUMNS/ALL_INDEXES 可查。欄位/PK 以 `database/reference/mestest_prd_default_aps_source_ddl.sql` 為準。

## Oracle 連線與安全

- 查 DDL 固定使用 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，由本機受保護 profile 取得連線字串後直接讀 Oracle metadata；不用 DBeaver GUI 抄 DDL。
- 公司筆電本次實際讀取的受保護設定檔是 `C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt`（JSON），Oracle 測試連線欄位為 `DatabaseConfigs.CS.Test.Mes.ConnectionString`，類型欄位為 `DatabaseConfigs.CS.Test.Mes.DbType`。這是筆電上的 Codex attachment 路徑，不保證桌電存在；桌電先檢查此檔是否可用，不能因路徑不同就從文件猜密碼。
- 該 JSON 還包含其他系統設定與機密。不可將檔案複製進 repo、提交 Git 或輸出內容；只在程式中讀取所需 Oracle key。若桌電不存在此受保護檔，請使用者安全地重新設定 Oracle profile。
- 公司桌機已使用目前 Windows 帳號的 DPAPI profile 驗證 MESTEST 登入及 RunTime 資料字典查詢。設定檔位於 Git repo 外，由桌機帳號本機解密。
- IP、帳號、密碼、SID 不得寫入 GitHub Markdown、SQL、source code 或 console output。桌電若沒有受保護 profile，透過安全方式請使用者重新設定；不可猜連線資料或從舊電腦文件搜尋明碼。
- 只讀 metadata，不查 MES 業務資料列、不執行 Oracle DDL/DML。PostgreSQL CREATE-only reference 不是 migration；家用端先查 YMES live schema 和 migration history。

## 使用者目標與工作規則

- 忠實復刻完整 MES 的三層 menu、WinForms 功能頁、欄位、按鈕及操作邏輯。困難處可先簡化，但要明確記錄限制，供使用者決策。
- 公司端：DEV 假資料下完成 UI/互動、模組規格、Oracle source DDL 與家用端 API/schema/Create SQL handoff。家用端：核對 PostgreSQL live schema、建立缺少的 migration/API。使用者已授權此 YMES 專案完成後分階段 commit/push GitHub。
- WinForms Form、Designer、Service/Core 是主要證據；圖片只用於版面核對。找不到原始碼/DDL時，告知具體缺少的路徑或表名，不要假裝已確認。
- 不寫入或輸出資料庫秘密。未收到使用者要求時，不執行 build/test/browser；可做靜態檢閱和 `git diff --check`。
- PD101 至 PD117、PD202、PD204、PD206、PD901、`PRD OVERSTAY` 與 `PRD OP RULE` 已完成本輪 DEV 互動與交接。各模組文件記錄正式簽核、匯入、資料權限和引擎相依。`PD_BATCHMODIFYAREA` 的客製原始碼待補。
- CP 行業設定 PDP01 至 PDP04、挑片規則 PDB01、封測產品 PDB02、產品文件 PDB04 與 PDB05 已完成本輪 DEV 與交接。封測行業設定 PDI01／PDI03 的獨立 Form 與 Designer 待補。

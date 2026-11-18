# Codex handoff：公司筆電 → 桌電

更新：2026-11-18。用途：在桌電 Codex 開啟 YMES repository 後直接接續，不必重做已完成調查。

## 接手方式

1. 在桌電開啟同一個 GitHub repository `souaix/YMES`，執行 `git pull origin main`。
2. 先讀本檔、`todolist_officepc.md`、`todolist_homepc.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md` 及 `docs/modules/PRD/產品預設設定.md`。
3. 本機 Codex 使用 YMES repo；MES WinForms 原始碼在公司筆電 `D:\iMES\CSiMES`，桌電可能是另一個路徑。若桌電有既有 MES checkout，先核對路徑；不可用 UI 截圖代替 Form/Designer/Service/Core 原始碼。原始碼缺少時記錄具體缺檔，先做不依賴它的工作。
4. 若在桌電打開同一個 Codex 對話，thread 對話與 Goal 脈絡仍在；若另開新對話，把本檔及 TODO 當成接手上下文，不要假設新 thread 自動有舊 Goal 狀態。

## Repository 與分工

- Repository：`D:\YMes`（桌電可用不同 clone 路徑）；remote `https://github.com/souaix/YMES.git`，branch `main`。
- Vue 3 UI：`web/`。
- .NET 9 API：`src/YMes.Api/`。
- 共用 DTO：`src/YMes.Contracts/`。
- `src/YMes.Web/` 是既有 ASP.NET MVC 專案，不是目前 Vue UI。
- 公司端以 DEV 假資料完成 Vue 畫面/互動並記錄規格；家用端負責 PostgreSQL schema、API、migration/Create SQL handoff。DEV localStorage 不送入正式 API。

## 目前檢查點與下一步

- 前一個已推送檢查點為 `45cddce`（PD102 Program）。此次 Default 檢查點以 GitHub `main` 最新狀態為準；先檢查 `git status`、`git log -1`，再決定是否尚需 commit/push，不要重做已提交的工作。
- PD102 DEV 子視窗已分段完成 Property、Parameter、Business Rule、OPDescription、RecipeGroup、Accessory、Resource、SubOP、Program。各功能的 WinForms 證據、DDL 及 API handoff 在 `docs/modules/PRD/` 與兩份 TODO。
- 本 checkpoint 的 PD102 Default 內容包括 `web/src/components/ProductDefaultSetup.vue`、`ProductBasisMaster.vue` 接線、DEV localStorage 假樣板、APS DDL 參照、`docs/modules/PRD/產品預設設定.md` 與兩端 handoff。若這些變更已在最新 main，直接 review，不重做。
- Default Production API 未完成。完成 checkpoint 後依 WinForms toolbar 次序接續 PD102 `RunTime`，再依 `MES_MENU_FUNCTION_INVENTORY.md` 逐功能完成全 MES；目前整體目標遠未完成。

## Default 已核實事項

- WinForms 入口 `btnDefault_Click` → `frmProductLoadDefault` → `wsPRD.UpdPRDOP_BatchDefault` → `clsProduct.UpdPRDOP_BatchDefault`。
- `Product Process OP` 對應 `DefaultOP=2` 且預設選取；`All PS OP` 對應 `DefaultOP=1`。父畫面沒有產品流程時不開表單。
- 複製旗標：Parameter、BusinessRule、Description、OPResource、QCItem、SubOP、APS。產品 IssueState 非 0 時 Checkbox/Apply disabled，Radio 仍可選。APS 受 `ExecuteAPS` 顯示控制但 Designer 預設 checked；QCItem 初始狀態由 `PRDDefault_QCItemAutoChecked` 控制。
- Oracle APS product table 字典是由 Windows PowerShell 5.1 + ODP.NET 直連唯讀取得。`DBMS_METADATA.GET_DDL` 回 ORA-03135；ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_CONS_COLUMNS/ALL_INDEXES 可查。欄位/PK 以 `database/reference/mestest_prd_default_aps_source_ddl.sql` 為準。

## Oracle 連線與安全

- 查 DDL 固定使用 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，由本機受保護 profile 取得連線字串後直接讀 Oracle metadata；不用 DBeaver GUI 抄 DDL。
- IP、帳號、密碼、SID 不得寫入 GitHub Markdown、SQL、source code 或 console output。桌電若沒有受保護 profile，透過安全方式請使用者重新設定；不可猜連線資料或從舊電腦文件搜尋明碼。
- 只讀 metadata，不查 MES 業務資料列、不執行 Oracle DDL/DML。PostgreSQL CREATE-only reference 不是 migration；家用端先查 YMES live schema 和 migration history。

## 使用者目標與工作規則

- 忠實復刻完整 MES 的三層 menu、WinForms 功能頁、欄位、按鈕及操作邏輯。困難處可先簡化，但要明確記錄限制，供使用者決策。
- 公司端：DEV 假資料下完成 UI/互動、模組規格、Oracle source DDL 與家用端 API/schema/Create SQL handoff。家用端：核對 PostgreSQL live schema、建立缺少的 migration/API。使用者已授權此 YMES 專案完成後分階段 commit/push GitHub。
- WinForms Form、Designer、Service/Core 是主要證據；圖片只用於版面核對。找不到原始碼/DDL時，告知具體缺少的路徑或表名，不要假裝已確認。
- 不寫入或輸出資料庫秘密。未收到使用者要求時，不執行 build/test/browser；可做靜態檢閱和 `git diff --check`。
- 開始接續時：pull `main` → 核對 Default checkpoint 狀態 → 靜態檢閱並 commit/push 尚未提交內容 → 開始 PD102 RunTime。

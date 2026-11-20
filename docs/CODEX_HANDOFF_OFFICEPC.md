# Codex handoff：公司筆電 → 桌電

更新：2026-11-20。用途：在公司端桌電／筆電 Codex 開啟 YMES repository 後直接接續，不必重做已完成調查。

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

- 公司端已接續 OE101、OE102、OE201 至 OE209、OEB01、OEP01 的 DEV 訂單與工單操作。各功能及 Oracle DDL／API 交接見模組索引。WP101、WP102、WP201、WP202、WP204、WP301、WP302、WP601、WP603、WP604、WP605、WP629 已完成公司端 DEV 與家用端 handoff；後續工作依 `todolist_officepc.md` 及 MES menu inventory 接續。
- **2026-11-20 WIP 接續檢查點：WP305–WP307 及 WP203／WP103／WP113／WP104／WP105／WP205–WP207 的公司端 DEV/spec/handoff 已同步至 `D:\YMes` 工作樹。** 同步前逐檔比對、於 `D:\iMES\CSiMES\ymestage\wp207_sync_backup_20261120` 備份七個有差異的既有檔案並核實 SHA256；新增檔與既有 WP305 未提交檔均已保留。WP307 的 State/History ShiftNo／UserNo lookup 與正式 API contract 已列於規格與 `todolist_homepc.md`。
- Oracle 僅以受保護 MESTEST profile 讀取 WP307、WP206–WP209 等結構字典，未查業務資料、未執行 DDL/DML。source DDL、metadata、CREATE-only 與兩端 TODO 均在工作樹。公司端仍未連 YMES PostgreSQL、未實作 Production API/migration，也未執行 build/test/browser；WP208–WP211 公司端 DEV／規格已完成，下一步 WP212「併批處理_元件」。
- **2026-11-20 同步檢查點**：公司端 WP207 以前的工作保留家用端 14 筆遠端提交，經 rebase 以 `b3f0f06` 正常推送 GitHub main。WP208 沿同一版本建立 staging；整合時須再次核對遠端與家用端新異動，逐檔備份／比較，不 force push。WP208 Core 直接引用 27 張表；其中 `TBLSYSPARAMETER` 原先缺 SQL reference，已由公司端 PowerShell 5.1／受保護 Oracle profile 唯讀取 DDL／metadata並補 CREATE-only，其餘 26 表沿用先前 refs。家用端須實作元件守恆的交易 API。
- **2026-11-20 WP208／WP209 接續**：WP208 已以 `dbebd0f` 正常推送 main。WP209 `LOT SPLIT_BIN` 已依 WinForms／Service／Core 完成 Vue DEV、規格與 Oracle 唯讀 DDL／CREATE-only 交接，路由 `/YMES/WIP/LotSplitBin`。`TBLWIPSPLITCONTENT_BIN` 已查得完整結構；`TBLWIPSORTERBINDATA` 在目前 MESTEST `ALL_TABLES`／`ALL_OBJECTS`／`ALL_SYNONYMS` 均不可見，只影響 Core 的 LED_PKG 分支，待正確 DDL，不猜表結構。家用端負責 live PostgreSQL 核對、交易 API 與正式驗收。公司端下個 menu 功能依 inventory 查 WP210；WP209 本輪未執行 build/test/browser。
- **2026-11-20 WP210 接續**：WP210 `LOT SPLIT_CARRIER` 已依主 Form、元件 Slot picker、Service 與有／無元件兩條 Core 完成 Vue DEV 與 API 規格，路由 `/YMES/WIP/LotSplitCarrier`。兩條 Core 直接引用 17 表，逐名核對均已有 CREATE TABLE reference，沒有新缺表需 Oracle DDL／CREATE-only；家用端仍要核 live PostgreSQL、追 helper 間接表並完成載具／元件 Slot 一致交易。原 Change Product 按鈕未傳產品欄的來源差異見模組 MD。下個 menu 功能為 WP211「併批處理」。
- **2026-11-20 WP211 接續**：WP211 `LOT MERGE` 已依 `frmBR_L_Merge_GEN`、`Exe_Manual_LotMerge` 與 `BR_Manual_LotMerge` 完成 Vue DEV、`/YMES/WIP/LotMerge`、規格與家用端 API handoff。八張直接表均有既存 source DDL／CREATE-only；沒有新 SQL。來源中至少兩批的檢查已註解，MO 跨工單由 `AllowDiffMOMerge` 控制，OS、包裝、GoodBin 與 BR 後續轉站分支見模組 MD。下個 menu 功能為 WP212「併批處理_元件」。
- **WP203「過帳設備設定」（`EQP FAVORITE SETUP`）公司端 DEV 與 handoff 已完成。** 原始來源為 `STD/MESWin/WIP/WP_EquipmentFavorite/frmEquipmentFavorite.vb`、`frmEquipmentFavoriteDef.vb`、`STD/MESws_STD/wsSTD/wsEQP/wsEQP.cs`、`STD/MESkc/kcEQP/clsEQPBasis.cs`。重用 WP204 的 `TBLEQPFAVORITE` source DDL/CREATE-only；沒有新增 Oracle table。HostId 是 workstation-scoped，Production API 不得信任瀏覽器傳值。
- **WP103「列印流程卡（RUN CARD）」公司端 DEV 與 handoff 已完成**，規格為 `docs/modules/WIP/列印流程卡.md`，FunctionKey `WP103` 路徑 `/YMES/WIP/RunCard`。來源 Form/Service/Core 和 RunCardStyle 0/非 0、Lot 查詢、RunCard Log、Net Flow、Runcard template／Product RunCardMap 分支均已記錄。無新增專用表／DDL；DEV Preview 使用假 lot/流程，Print 不送出實體工作。Production API、PDF renderer、ProductMap/PD_Kit 移植與 PostgreSQL live schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP113「LOT SHEET PRINT」THEIL 專用頁已完成公司端 source trace、DEV 假資料頁與 API handoff**，規格 `docs/modules/WIP/LOT_SHEET_PRINT.md`，路徑 `/YMES/WIP/LotSheetPrint`。來源為 THEIL WinForms/Service/Core 與 ActiveReports；假資料包含 MO/建立日期篩選、Current/Original Process、Lot Sheet/Empty Version 預覽及不連印表機的 Print 模擬。重用既有 Lot Property 與 THEIL Matrix，無新增表／DDL。Production 查詢、報表 renderer 與 print count 原子更新交家用端依 `todolist_homepc.md` 執行。
- **WP104「使用者登入作業（上工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登入作業_上工.md`，路徑 `/YMES/WIP/OperatorClockIn`。已依 WinForms、wsUSR/wsOP/wsWIP_OperatorState、clsOperatorState 實作假員工、班別、OP mode、LoginPlace 與 auto-clockout 確認。Oracle 只讀新查 `TBLWIPOPERATORSTATE` 和 `TBLWIPOPERATORLOG`，DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp104_*`；重用 WP305 的 LotLoginState。Production credential verification、交易及 live PG schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP105「使用者登出作業（下工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登出作業_下工.md`，路徑 `/YMES/WIP/OperatorClockOut`。依 WinForms/Service/Core 模擬員工驗證、現況篩選、預設全選與逐列下工；沿用 WP104 OperatorState／OperatorLog 和 WP305 LotLoginState。沒有新增 Oracle 表或 DDL；Production API 與 PostgreSQL live schema 核對交家用端。
- **Lot Operator Clockin 批次上工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員上工.md`，路徑 `/YMES/WIP/LotOperatorClockIn`。頁面依 LotStatus/ClockStatus/PS/OP 查詢候選 Lot，預設全選，按 mode/location 去重後一次模擬批次上工，並與 WP104/WP105 共用假 OperatorState。沿用 WP104/WP305 Oracle references，無新 DDL。WinForms 來源只暴露 `PrivFunNo`，FunctionKey 未核實；fallback FunctionNo 是標題推導值，上游 menu metadata 待確認。
- **Lot Operator ClockOut 批次下工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員下工.md`，路徑 `/YMES/WIP/LotOperatorClockOut`。頁面依預設 ClockStatus=Clocked In 查詢、可選員工篩選、同步勾選同 UserNo+LoginPlace rows，按來源批次去重模擬下工；共用 WP104/WP105 假狀態／歷程，沿用 WP104/WP305 refs，無新 DDL。FunctionKey 未核實，正式路由待 menu metadata 確認。
- **作業員 OP／Area 登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/作業員登入登出_OPArea.md`，路徑 `/YMES/WIP/OperatorLogin`。來源使用 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`，但公司端 MESTEST 所有可見 Owner/Synonym 查無這兩表；未製作推測 CREATE，家用端應先核 live PG schema，並取得正確 Oracle DDL 再決定 migration。
- **OPERATOR LOGIN_LOT 生產批人員登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/生產批作業員登入登出.md`，路徑 `/YMES/WIP/OperatorLoginLot`。DEV 呈現 CallType=MenuItem，規格涵蓋 BR 呼叫端 1/2/3；共用 WP104 OperatorState/Log 與 WP305 LotLoginState，沒有新表／DDL。正式 FunctionKey 待 menu metadata 核實。
- WP301 設備上料已核對 `frmEQPMaterialLoading`／`wsWIP_Material`／`clsMaterialHandler`，完成物料、元件、半成品模式的 DEV 假資料頁。四張相依表 DDL／CREATE-only 在 `database/reference/mestest_wip_wp301_*`；其餘 13 張沿用既有 reference。未連 YMES PostgreSQL、未實作 Production API／migration。
- WP302 載具現況管理已核對 `frmCarrierState`／`wsEQP`／`wsWIP`／`wsINV`／`clsCarrierBasis`，完成 Vue DEV 放入、取出、清空及隱藏狀態操作。三張新查證表 DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp302_*`；Lot／Component／CarrierType 等沿用既有 references。未連 YMES PostgreSQL、未實作 Production API／migration。

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

## 2026-11-20 WIP WP308 接續檢查點

- WP308「載具管理_元件」已依 WinForms frmCarrierManagement／frmCarrierIn／frmCarrierChange、Service 與 Core 完成 Vue DEV 主頁與兩個子視窗；與 WP302 共用 DEV 假資料。規格為 docs/modules/WIP/載具管理_元件.md；WP303 設備耗品現況已接續完成，詳見下方檢查點。
- WP308 沒有新表；沿用 WP302、WP101、Carrier Basis/Type 的 Oracle reference。家用端需比對 PostgreSQL live schema，再實作 Carrier In/Out/Change 的登入保護 POST command、lookup 與原子交易。
- 這輪未連 YMES PostgreSQL、未部署 API、未執行 build/test/browser；詳見 todolist_officepc.md 與 todolist_homepc.md。

## 2026-11-20 WIP WP303 接續檢查點

- WP303「設備耗品現況」已依 `frmEQPConsumeStatus`、`frmEQPConsumeStatusDef`、`WP_EQPConsumeReset/frmEQPConsumeReset`、wsWIP/wsEQP 與 clsWIP/clsEQPBasis 完成 DEV 主頁及維護、設備 lookup、ConsumReset 視窗。規格：`docs/modules/WIP/設備耗品現況.md`；頁面 `/YMES/WIP/EquipmentConsumeStatus`。
- 兩張新表 `TBLWIPEQPCONSUMESTATUS`、`TBLWIPEQPCONSUMELOG` 的 Oracle source DDL／metadata／PostgreSQL CREATE-only 已建立於 `database/reference/mestest_wip_wp303_*`。設備耗品基準沿用既有設備類別 reference，設備主檔沿用既有 reference。
- WP304「設備耗品歷程」已接續完成，詳見下方檢查點。公司端仍只做 Vue DEV 假資料、WinForms／Service／Core 核對及 DDL metadata handoff；不做 YMES API／migration、不連 PostgreSQL、不執行 build/test/browser。

## 2026-11-20 WIP WP304 接續檢查點

- WP304 依 `frmEQPConsumeStatusLog`、`wsWIP.LoadEQPConsumeLog`、`wsEQP` lookup 與 `clsWIP.LoadEQPConsumeLog` 完成唯讀查詢頁；設備／耗品核取篩選、兩個 lookup、From/End Date、Query、七欄 Grid 與 Close 均已實作。頁面 `/YMES/WIP/EquipmentConsumeStatusLog`。
- 本功能只讀 WP303 已核對的 `TBLWIPEQPCONSUMELOG`，沒有新 Oracle DDL／CREATE-only；DEV 與 WP303 共用假 log storage。
- WP305「設備線上點檢」已接續完成，接手摘要見下節。公司端不連 YMES PostgreSQL、不實作 API/migration、不部署、不執行 build/test/browser。

## 2026-11-20 WIP WP305 接續檢查點

- WP305 依 `frmEQPCheckQCList`、`wsEQP/wsWIP`、`clsWIP`、`clsLot` 完成 Vue DEV `/YMES/WIP/EquipmentOnlineQC`。規格與 DEV 差異見 `docs/modules/WIP/設備線上點檢.md`。
- 直接唯讀 Oracle MESTEST 字典取得 6 表 DDL、indexes、欄位/constraint/trigger metadata：點檢 Log/Detail/Task、設備狀態 Log、設備上機歷程與 Lot 人員登入狀態。Source DDL/Index DDL/metadata/CREATE-only 在 `database/reference/mestest_wip_wp305_*`。`TBLWIPCONT_EQUIPMENT` 原始無 PK；不可自行補 key。
- Vue DEV 使用 localStorage `ymes.dev.wp305-equipment-online-qc.v1`，測項涵蓋 QCType 0–3、多筆 D01-D10、Wait QC、Re QC、未完成確認及結果存檔。NG + UnValidAction 的設備狀態/生產下機副作用尚未在 DEV 寫假 DB 狀態，已明列於規格。
- 正式 API 需保留 Header/Detail/Task 原子交易、server-side QC 結果計算、check-in 不允許部分存檔，以及失敗設備狀態變更時共用 WP201/WP204 下機 command；家用端先比對 live schema/migrations，只補缺少物件。
- 下一功能為 WP306「設備點檢歷程」。繼續讀該 Form/Designer/Service/Core，沿用 WP305 Log/Detail references；若查到未收錄表才以公司 PowerShell 5.1 + Oracle profile 唯讀查 DDL。公司端不連 YMES PostgreSQL、不改 API/migration、不部署/執行 build/test/browser。

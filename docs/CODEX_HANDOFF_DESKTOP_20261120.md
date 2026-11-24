# 2026-11-20 公司桌機 Codex 接手點

## 後續最新進度（2026-11-24）

本檔下方的 WP401 起始指示是 2026-11-20 當時的歷史接手點。公司端後續已完成 SPC SC501 及 INV IV101、IV102、IV103、IV201、IV202、IV207、IV203、IV208；最新狀態和家用端具體工作請看 [公司端待辦](../todolist_officepc.md) 與 [家用端待辦](../todolist_homepc.md)。下一項依選單清冊列序為 IV205「開立預訂單」。本輪 `main` 僅交付公司端 DEV／規格，不包含 PostgreSQL/API/schema 實作。

## 立即接手

這是公司筆電接回桌機任務後，完成 WP509、WP106、WP109、WP112 的最新狀態。使用者要**完整復刻 MES 全部第一層選單及其功能**：公司端以 WinForms／Service／Core 原碼為準，用 Vue 3 DEV 假資料完成畫面與可模擬的互動，缺表時唯讀查 Oracle MESTEST DDL/metadata，提供 PostgreSQL CREATE-only 與正式 API 規格給家用端。可先簡化真外部交易，但要在模組規格中明確記錄。每個段落正常 commit/push 到 `souaix/YMES` 的 `main`，不要 force push。先前誤發的 vendor DLL 問題已由使用者取消，**不要接該任務**。

桌機可依此順序直接開始：

1. 在桌機 YMES clone 執行 `git fetch origin main`、檢查 `git status`，乾淨時 `git pull --ff-only origin main`。本交接整理前，筆電本地與 GitHub `main` 已核對為 `ba2012194debba17f8b124ed3972d86ded3ab247`，工作樹乾淨；本交接文件自身另有後續提交，**以桌機 pull 後的最新 main 為準**。若桌機有未推工作，先比對，不要覆蓋。
2. 讀本檔、[原公司端交接](CODEX_HANDOFF_OFFICEPC.md)、[公司待辦](../todolist_officepc.md)、[家用端待辦](../todolist_homepc.md)、[選單清冊](MES_MENU_FUNCTION_INVENTORY.md)及[規格索引](MODULE_REPLICATION_SPEC_INDEX.md)。原交接中較早的「下一功能」是歷史記錄，以本檔和最新待辦置頂段落為準。
3. 下一個尚無 Vue 路由的 WIP 選單功能是 **WP401 `OS CREATE`「外包單開立」**。原 Form 在筆電 `D:\iMES\CSiMES\STD\MESWin\WIP\WP_OS\frmWIPOS.vb`，另有 `frmWIPOSBasisDef.vb`；桌機先找自己的 MES source checkout，追 Form/Service/Core。清冊所列 WIP 其餘未有路由的主要功能為 WP402/403/404 外包處理、WP901–909 及 WP911–913/915/916 例外處理；逐一對照清冊、現有 `web/src/App.vue` 和模組規格，不重做已完成頁。
4. 對每項完成 Vue DEV、`docs/modules/...` 規格、`database/reference/...` 的原 Oracle DDL/metadata與需要的 PG CREATE-only、兩端 TODO，再核差異與 GitHub 遠端，正常推送。家用端新增正式 API/PG 提交可能同時到來；每次同步前重查 remote，不覆蓋它們。

## 筆電接回後完成並推送的四項

| 功能 | Git commit | 公司端交付與限制 |
|---|---|---|
| WP509 未解除異常單 | `673a3fb` | `/YMES/WIP/ErfUnreleased`、[規格](modules/WIP/未解除異常單.md)、Oracle `XX_TBLQCHOLDPRIV` 原 DDL/metadata 與 PG CREATE-only。DEV 分併批/OCAP 子窗是簡化；正式群組權限與 ERF/Lot 原子交易交家用端。 |
| WP106 使用者登入（上工）資訊 | `d96b80d` | `/YMES/WIP/OperatorOnline`、[規格](modules/WIP/使用者登入資訊.md)，共用 WP104/105 DEV State/Log，三種 place 查詢與 SignOut。無新直接表。 |
| WP109 特殊生產需求維護 | `8f4b96d` | `/YMES/WIP/SwrBasis`、[規格](modules/WIP/特殊生產需求維護.md)、`database/reference/mestest_wip_wp109_*`：SWR Basis/Detail/共用 Serial 三表原 DDL/metadata與 CREATE-only。DEV 附件只存檔名；正式 API 須處理 PENDING 作廢假成功、JOIN 重複核准、序號競爭與附件完整讀寫。 |
| WP112 工單撤派工 | `ba20121` | `/YMES/WIP/UndoDispatch`、[規格](modules/WIP/工單撤派工.md)、`database/reference/mestest_wip_wp112_*`：MaterialLot、SYSSerialCreateMax、MO OS、ZPPT0025A/B/C1 六表原 DDL/metadata與 CREATE-only。DEV 只示意逐批 Lot 狀態，沒有真 SAP/Log/Inventory；原 Core 實寫 `THSAP.*@SAP_DBLINK`，外部表契約未驗。正式端 provider 未具備時不得回交易成功。 |

WP510、WP511 以及更早的 WIP/OE/主檔進度在原交接、規格索引及 Git 歷史。以上四項均只做公司端 DEV/規格/DDL handoff，**沒有**宣稱正式 API、PostgreSQL migration 或部署完成。WP109/WP112 由筆電透過 PowerShell 5.1 + 受保護 Oracle profile 直連，只讀 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`/`ALL_CONSTRAINTS`/`ALL_INDEXES`/`ALL_TRIGGERS`，沒查業務列或執行 Oracle DDL/DML。`git diff --cached --check` 通過；**未執行 build/test/browser**，桌機若未被使用者要求驗證，照既有公司端交接先以靜態核對為主。

## 環境與分工

- 公司筆電 YMES clone：`D:\YMes`，MES 原碼：`D:\iMES\CSiMES`；桌機先核本機 clone/source 路徑，不假設相同。`web/` 是 Vue 3，API 在 `src/YMes.Api/`（.NET 9），Web/API 分離。左側三層選單、右側功能頁，KD 黃色系；來源以 Form/Designer/Service/Core 為準，不只看截圖。
- 公司桌機與筆電工作**相同**：讀原始碼、MESTEST 唯讀 DDL/metadata、Vue DEV 假 UI/邏輯、MD 規格和 home TODO。家用端工作：YMES PostgreSQL live schema/migration、正式 API、外部 provider、build/測試、部署與驗收；公司端不要連家用 PostgreSQL、設 `ConnectionStrings__YMes` 或 SSH 部署。
- Oracle 驅動在 MES checkout 的 `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`。筆電使用受保護 JSON 的 `DatabaseConfigs.CS.Test.Mes.ConnectionString`，實際筆電 attachment 路徑及桌機 DPAPI profile 資訊見[原交接的 Oracle 連線與安全](CODEX_HANDOFF_OFFICEPC.md)；原紀錄指出桌機已以自身 Windows 帳號的 DPAPI profile 驗證 MESTEST 字典查詢。**不要把 IP、帳號、密碼、SID 或 profile 原文輸出／提交**。若桌機受保護設定不可用，告訴使用者缺的是哪個設定；不以 DBeaver 畫面抄 DDL，也不猜表結構。
- 筆電臨時 `D:\iMES\CSiMES\ymestage\wp109`、`wp112` 中的查詢 helper/工作複本**未提交**；交付的 source DDL/metadata/CREATE-only 和規格都已在 YMES repo。桌機只需 pull 最新 main 即可接續；查新表可依原交接方式自寫唯讀 PowerShell。
- 家用端最新既有正式進度在 `todolist_homepc.md` 的 ET103 段，先前 PostgreSQL/API 的驗收不代表 WP509/WP106/WP109/WP112 已正式完成。CREATE-only 是結構參照，家用端先核 live schema/history，才決定是否做 forward migration。

## 給桌機 Codex 的接續指令

> 這台是公司桌機。請 pull `souaix/YMES` 最新 `main`，先讀 `docs/CODEX_HANDOFF_DESKTOP_20261120.md`、`docs/CODEX_HANDOFF_OFFICEPC.md`、`todolist_officepc.md`、`todolist_homepc.md`。接著做 WP401 `OS CREATE` 外包單開立：核對 MES WinForms／Service／Core，以 DEV 假資料完成 Vue 3 畫面與邏輯；缺直接表就用桌機受保護 Oracle MESTEST 設定做唯讀 DDL/metadata，交 PostgreSQL CREATE-only 和 API 規格給家用端。持續依清冊完成尚缺功能，分段正常推送 GitHub；公司端不接 PostgreSQL/API/部署。WP112 已在 `main`，不要重做；先前 vendor DLL 任務已取消。

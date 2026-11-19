# YMES 開發與 UI 風格準則

這份文件是家用電腦與公司電腦共用的開發方向。兩邊都以 GitHub `souaix/YMES` 為唯一程式來源；架構、API 契約與畫面規範有變更時，先更新這份文件，再同步程式碼。

## 電腦身分與工作邊界

- 每次接手先依使用者指出的電腦，以及 `todolist_homepc.md` 或 `todolist_officepc.md` 確認工作身分；工作資料夾、瀏覽器目前開啟頁面或某個工具能否啟動，都不會改變這台電腦的角色。
- 家用電腦負責 YMES PostgreSQL 復刻資料庫、schema/migration、API 與遠端部署；家用端使用本機已授權的 PPK SSH 連線管理 YMES PostgreSQL。若 API 執行帳號只有應用程式權限，應由家用端經 SSH 使用資料庫管理權限處理 migration，不能因此改交公司電腦或把資料庫工作改成 UI 工作。
- 公司電腦連 MES 原生 Oracle（MESTEST）作來源查詢，唯讀查資料字典、原始 DDL 與舊系統規則；不在 Oracle 寫入或建 YMES 表。公司端負責 Vue/WinForms UI、按鈕互動與呼叫 `/YMES/API`，不連 YMES PostgreSQL、不 SSH、不遠端部署。
- PPK、資料庫管理憑證與連線字串只留在家用端受保護位置，不複製到公司電腦、文件或 Git。

## 原生 MES 來源與 YMES 復刻資料庫

- MES 原生資料庫是公司端可唯讀查詢的 Oracle MESTEST；YMES PostgreSQL 是家用端建置的復刻目標，用來重建 MES schema、API 與業務行為。兩者是來源與復刻目標，不是同一資料庫，也不互換連線責任。
- `database/reference/*_create.sql` 是從 MES Oracle 原生 DDL/資料字典擷取後轉成 PostgreSQL 的建表 SQL，用來在 YMES PostgreSQL 建立復刻結構；不得拿去 Oracle 執行，也不可把未核實的欄位或表名自行補進去。
- 來源 CREATE SQL 只建立結構，不代表已複製 MES 業務資料。MES 業務資料要匯入 YMES，須另有明確授權、欄位映射與匯入規劃；未授權時只搬系統目錄等必要中繼資料。
- 家用端可用 SSH 對 YMES PostgreSQL 做核對、建表、migration 及資料維護；Vue/Web 仍只透過獨立 API 存取 YMES，不直接連 PostgreSQL 或 Oracle。

## 開發分工

| 工作環境 | 主要工作 | 不直接處理 |
| --- | --- | --- |
| 家用電腦 | API、資料庫、schema/migration、整合與遠端部署 | — |
| 公司電腦 | Vue UI、選單、表單、按鈕互動與呼叫 API | 資料庫連線、SSH、遠端上版 |

- GitHub `souaix/YMES` 是兩台電腦共用的程式來源。開始修改前先同步最新程式，完成後提交回同一個 repo。
- 每個版本完成後，家用端使用 app-browser 逐項操作本次模組的查詢、新增、修改、複製、套用、lookup、核准、刪除及關閉；驗收結果與未完成原因寫入模組 Markdown。需要登入時可自行按既有 Demo 登入，使用者已授權，毋須等待使用者回到電腦前。
- 驗證完成才同步 GitHub：先 fetch 並檢查公司端新增提交，再正常 merge，衝突按雙方功能整合；禁止以 force push 或檔案整批覆蓋取代合併。合併有程式差異時重做相關 build/操作驗收，push 後再檢查 `todolist_homepc.md` 的下一交接。
- 單一模組完成後要繼續處理下一項可執行的家用端待辦；只有已無待開發項目，或存在必須由使用者補充資料且無其他可推進工作的阻礙時才停止。不得將分階段提交或同步 GitHub 當作停止工作的理由。
- 每個完成模組必須有 `docs/modules/<模組>/<功能>.md`，包含來源規則、資料表/migration、API 契約、按鈕邏輯、驗證紀錄與維護限制；先前缺漏的模組文件也要補齊。公司端後續工作寫 `todolist_officepc.md`，家用端資料庫/API/部署工作寫 `todolist_homepc.md`。
- 公司電腦先以明確標示的 Development 假資料完成所有畫面與本地互動邏輯，不連資料庫；同時把正式 API 契約和 Oracle 對照的 CREATE SQL 寫入交接文件，交由家用電腦完成 API/schema。Development 假資料不得冒充正式資料，Production 必須使用正式 API，且不得啟用假資料。
- 資料庫連線字串、`.env.local`、私鑰與其他憑證不得提交到 GitHub。資料庫結構與 migration 由家用電腦維護，並透過 API 提供給 UI。
- Web 與 API 是分開建置、設定及執行的服務，可以部署在同一台 Server。Web 路徑固定為 `/YMES`，API 路徑固定為 `/YMES/API`；路徑大小寫固定，不自行改名或增加反向代理前綴。
- 目前專案目標版本為 .NET 9。Vue 3、TypeScript、Vite 用於登入、主選單及所有主檔清單、表單和關聯設定；前端一律經由獨立 API 存取資料，不直接連資料庫。清單列選取應在 Vue 狀態內更新，不以整頁導覽選取資料。
- Web 與 API 共用資料契約放在 `src/YMes.Contracts/`。修改契約時，需同步確認 API 與 Web 使用相同版本。

## KD 視覺基準

KD Logo 是 YMES 的品牌與配色依據。新增或調整畫面時，沿用以下共用色票；CSS token 集中維護於 Vue `web/src/style.css`，不另造一組互相衝突的品牌色。

| 用途 | 色彩 | CSS token / 說明 |
| --- | --- | --- |
| 頁面底色 | `#F7F5EE` | 暖白背景 |
| 卡片與表格 | `#FFFFFF` | 主要內容表面 |
| 主要文字 | `#26251F` | `--ink` |
| 次要文字 | `#777366` | `--muted` |
| 分隔線與欄位邊框 | `#E5E1D5` | `--line` |
| KD 黃金色 | `#E7C600` | `--yellow`；用於選取、重點與互動狀態 |
| 主要按鈕底色 | `#3B3930` | 深色按鈕搭配白字 |

- 字型使用 `Segoe UI`、`Microsoft JhengHei` 與系統 sans-serif fallback。
- 畫面以白色內容卡片、淺暖色背景、細邊框和清楚留白為主。互動焦點使用 KD 黃色系；錯誤與成功訊息只用必要的語意色，不擴大成裝飾色塊。
- GitHub 主檔原型的欄位、資料表、按鈕邏輯和業務資訊優先保留；套用 KD 色票時，不改變其資料欄位意義、狀態語意或 API 行為。
- 避免使用與 KD 主色競爭的亮粉、亮藍作為全站品牌色。既有主檔原型中的狀態提示只保留必要的語意區分，後續樣式整理時應轉為共用色票。

## 登入、主選單與首頁

- 登入頁保持精簡，只放作業環境、帳號、密碼、Demo 登入和登入按鈕；錯誤訊息貼近欄位或表單顯示。不要另加大型頁首、宣傳文字、狀態看板或非必要說明區塊。
- 作業環境選項為正式區、測試區、開發區，預設正式區。既有部署提供的 Demo 登入依使用者授權供驗收使用；Development 假資料預覽仍與正式 API 資料分開。
- 登入後使用 KD Logo 與固定側欄/主內容區。主內容首頁先留白，不自行增加儀表板或統計卡片。
- 選單預設全部收合，依舊系統 Module → Menu Group → Function 的三層結構呈現；不得把第三層主檔功能誤列為第一層。
- 每一層選單項目都顯示語意相符的圖示；側欄收合時只顯示圖示，隱藏文字與展開箭頭。
- 側欄收合時取消所有層級的水平縮排，圖示完整置中於窄欄；不得被子層邊框或捲動條裁切。每個選單按鈕以完整階層名稱提供 `title` 與 `aria-label`，收合後仍能辨識、點選並導向原功能。
- 主選單項目由 API 提供，Vue 不自行猜測舊系統的 Module、MenuNo、FunctionNo 或翻譯名稱。API 尚未提供完整選單時，Development 可以清楚標示並使用五個已開發主檔的簡化導覽；Production 不以簡化導覽假裝完整。
- 已有 Web 畫面的功能導向既有 Vue 頁面；公司電腦遷移期間，尚無 API 的頁面可使用 Development 本地假資料完成畫面與互動，必須在頁面清楚標示「開發假資料」；Production 對未完成 API 的功能顯示未遷移狀態，不可回傳假成功。

### 主選單 API 契約

Vue 使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。回應保留舊系統識別碼及排序：

```json
{
  "modules": [
    {
      "moduleNo": "EQP",
      "moduleName": "設備資料",
      "moduleOrder": 10,
      "menuGroups": [
        {
          "menuNo": "EQP_SETTING",
          "menuName": "設備設定",
          "menuOrder": 10,
          "items": [
            {
              "menuNo": "EQP_BASIS_MENU",
              "menuName": "設備基本資料",
              "functionNo": "EQP BASIS",
              "functionName": "設備基本資料",
              "functionOrder": 10
            }
          ]
        }
      ]
    }
  ]
}
```

API 從 YMES 復刻資料庫的 `tblSYSModule`、`tblSYSMenuItem`、`tblSYSFunction` 讀取來源目錄，保留舊識別碼與排序。依目前使用者指示，主選單 API 要求登入但暫不依角色或使用者權限篩選；原生 MES 的 privilege/issue flags 保留於資料庫，不在此次選單 migration 中修改。前端只負責階層呈現和導向，不連資料庫。

## 自適應與主檔頁

- 只維護一套自適應 Web，不另做獨立手機版網站、手機路由或手機專用功能。桌面、平板與手機共用相同 API 與功能，版面依可用寬度調整。
- 窄螢幕下側欄可以收合，表格可水平捲動或切換成較易讀的欄位排列；不得因固定寬度造成主要操作被裁切。
- 主檔頁的資料欄位、表單結構、查詢條件與按鈕行為以 GitHub `main` 原型為基準；Vue 外殼、登入、選單及設備清單依本文件的 KD 樣式。
- 編輯視窗開啟時停用背景操作，Tab／Shift+Tab 留在目前視窗。取消放棄修改或完成套用後，先解除忙碌／確認狀態，再於 DOM 更新後恢復焦點；共用確認視窗開啟時，由確認視窗管理焦點，避免父視窗攔截。
- 每個資料頁要清楚呈現載入中、無資料、驗證錯誤、API 錯誤及完成結果；只顯示使用者當下需要做決策或操作的內容。
- 設備、作業站、物料、流程與產品資料都由 `/YMES/API` 提供。尚未有正式 API/repository 支援的功能，應如實顯示未提供或 API 錯誤，不回退成看似正式的假資料。

## 共用檢查項目

每次新增功能或調整畫面前後，確認：

1. 兩台電腦都在同一個 `souaix/YMES` repo，使用相同的 API 契約與 .NET 9 目標。
2. Vue Web 呼叫 `/YMES/API`；UI 專案不設定資料庫連線。
3. Production 不啟用 Fake repository；驗收可依使用者授權使用部署已提供的 Demo 登入，登入後仍呼叫正式 API。
4. 新增樣式遵循 KD 色票、收合式三層選單與自適應版面。
5. 修改部署路徑時，同步檢查 `deploy/compose.yaml` 和 `deploy/nginx-ymes.locations.conf`。

相關文件：[GitHub 原型整合](GITHUB_INTEGRATION.md)、[共用系統架構](../ARCHITECTURE_BASE.md)、[部署設定](../DEPLOYMENT_PROFILES.md)。

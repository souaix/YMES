# 部署設定檔

## 1. 共通原則

同一套 Web/API 程式碼可部署至不同主機；執行產物、反向代理、機密注入、服務管理和腳本依目標環境準備。部署差異留在設定和 Infrastructure，不進入 Domain/Application 業務規則。

Web 靜態資產與 API 分別建置和部署。PostgreSQL 可由 Compose 部署，或連客戶管理的資料庫；位置依備份、容量、權限、可用性及客戶政策決定。

## 2. Ubuntu + Docker Compose

- Nginx 或核准的反向代理提供 Web 靜態資產和 TLS 入口。
- ASP.NET Core API 以 Linux Container 執行；Docker Compose 管理服務、網路、健康檢查和設定。
- YMES API 目標為 .NET 9；以 API Dockerfile 的 .NET 9 SDK/runtime 映像建置與執行。Vue Web 是獨立的靜態資產服務，不安裝或替換主機共用的 .NET runtime，避免影響同機其他專案。
- PostgreSQL 可作為獨立 Compose 服務或使用外部資料庫；資料目錄必須使用持久化儲存。
- 機密由部署環境注入，不嵌入映像、不提交版本庫、不輸出到 log。
- 資料庫端口不公開至不必要網路，服務帳號和網路權限採最小權限。
- 單一 Compose 主機仍是故障單點；高可用需求要另設備援主機/資料庫及演練流程。

YMES Vue Web 與 ASP.NET Core API 使用不同容器，部署在同一台主機。反向代理將 `/YMES/` 下的 UI 路徑送到 Vue 靜態服務並提供 SPA fallback，以 `/YMES/API/` 轉送 API；主檔表單和關聯設定都由 Vue 呼叫 API，不再轉送 MVC。小寫 `/ymes` 路徑回應 404，避免未匹配路徑落到同機其他網站。API 與 Web 容器只透過主機 loopback 的專用連接埠提供服務。主檔 API 使用 repository 介面；Production 預設停用主檔端點，直到 PostgreSQL provider 完成，Fake provider 只用於 Development。部署時將 YMES 路由以獨立 Nginx include 加入既有 HTTPS server，保留其他專案的路由，並在重新載入前執行 `nginx -t`、保留設定備份。API 使用獨立服務名稱、資料保護金鑰持久卷及機密設定，不替換主機共用的系統 runtime。

## 3. Windows Server + IIS

- IIS 提供 Vue 3 Web 靜態資產並配置 SPA 路由 fallback。
- ASP.NET Core API 透過 ASP.NET Core Module 託管；主機安裝符合專案版本的 .NET Hosting Bundle。
- Web/API 使用獨立網站或應用程式邊界，設定應用程式集區身分和檔案權限。
- PostgreSQL 可位於同一台或獨立資料庫主機；連線和防火牆依客戶政策設定。
- 機密不得進 Web 靜態資產、原始碼或一般日誌；憑證、IIS 代理/重寫設定和權限列入部署檢查。
- Windows/IIS 使用 Windows 目標產物；API 契約、資料結構和業務行為與 Ubuntu profile 保持一致。

## 4. 遠端測試和發行

### 遠端記憶體有限時的 Web 發行

先在家用端 `web/` 執行 `npm.cmd run build`（包含 vue-tsc），將產出的 `dist/` 與 `web/nginx.conf` 放入同一發行目錄。以 `deploy/Dockerfile.web.prebuilt` 建立 `ymes-web:current`，再對既有 Compose 執行 `up -d --no-build --no-deps ymes-web`。此方式只搬已驗證的公開靜態產物，不包含 `.env`、node_modules、登入憑證或資料庫設定。API 獨立建置與部署；不調大共用主機記憶體或修改其他服務。

2026-11-18 完整 UI 擴充後，遠端 Node 建置約 1 GB heap 上限耗盡；本機同版 Web build 已成功，因此 QC503/PS102 發行改採此方式。發行時保留前版 image tag 與靜態產物，回復使用前版 image，不在運作中的容器直接覆蓋檔案。

建置必須等待程序實際以 exit code 0 結束才可打包；工具回傳 session ID 代表仍在執行，不能當成成功。發行快照只包含本批已驗證內容，避免混入下一批草稿。打包前確認 `dist/index.html` 引用的 JS/CSS 存在且非空，封裝後再查 archive 大小與內容；不可將 Vite 建置期間已清空的 dist 部署上線。切換後檢查入口及同名 JS/CSS 的 HTTP 200 與實際下載大小，再做 app-browser 冒煙確認；失敗先切回保留的完整 image。

- Ubuntu 遠端測試可透過 SSH 操作；IIS 依客戶核准的 Windows Server 管理/部署方式執行。
- 測試、驗收和正式環境分開；未授權不得以正式資料做破壞性測試或 Migration。
- 每次部署保留版本、設定差異、Migration 記錄、健康檢查和回復步驟。
- 部署後驗證 Web/API 健康檢查、認證授權、資料庫連線和關鍵流程。
- 回滾程式版本不代表可回滾 DB；Migration 設計備份前置條件和向前修正方案。
- SSH 私鑰、Windows 管理憑證、正式帳號和環境檔放在受控憑證系統；文件只記取得和輪替流程，不記機密值。

### 2026-11-19 家用端可重建的來源快照

當工作目錄同時包含下一批草稿時，發行從固定 commit 加明列且已核對的檔案建立獨立快照。保留檔案清單、內容比對結果、建置輸出、API/Web image ID 與靜態資產名稱；不能直接把整個工作目錄當發行來源。

目前遠端另保存 root 專用的 `/opt/ymes/checkpoints/fb22a87`，是本機提交 `fb22a87` 的完整來源封存；不含 `.env` 或尚未驗證的 QC201/QC202 草稿。此提交只作本機及遠端維護 checkpoint，尚未推送 GitHub，也尚未完成本批所有瀏覽器驗收。來源封存不代表版本驗收完成。

`/opt/ymes/current-checkpoint.json` 記錄 checkpoint、公司端來源基底、映像及驗收狀態。OP103快照為API `260dfd2ff2bb`、Web `760d6055e519`，入口引用 `index-DI-geD67.js`；`browserQa` 與 `githubSync` 仍為 `pending`、`releaseComplete=false`。之後切換版本時更新此記錄，不把這些狀態提前改為完成。

OP103映像建置來源是 `/opt/ymes/releases/op103-20261119/`；共用 Compose 仍位於 `/opt/ymes/source/deploy/compose.yaml`，環境機密留在原受保護檔案。`/opt/ymes/source` 其他原始碼尚未同步至本 checkpoint，不能直接在該舊目錄執行預設重建而將線上程式退回舊版。重建須使用已記錄的獨立快照，確認映像內容與來源一致後，以指定映像對 `ymes-api`、`ymes-web` 執行 `--no-build --no-deps` 切換；保留其他專案及既有孤立服務。

OP103回復標籤為 `ymes-api:before-op103-20261119` 與 `ymes-web:before-op103-20261119`。回復前先核對實際標籤及 image ID，依本批 migration 的相容性判斷，只回復程式不會還原資料庫。備份使用 `umask 077`，`/opt/ymes/backups` 維持 700，dump 維持 600；由 root 讀取或透過標準輸入交由資料庫維護工具驗證，不放寬目錄權限。

QC201／QC202批API為 `95a153cef1d7`，由同一 `fb22a87` 基底加13份明列覆檔建立；完整API建置來源在 `/opt/ymes/releases/qc201202-20261119-a1/api`，同層 `api-source-manifest.json` 保存SHA與建置／部署狀態。本機及遠端 .NET9 Release均成功，本機0警告／0錯誤，啟動ready200；QC201七組分兩輪224HTTP／771斷言、QC202十組103HTTP／444斷言通過，各自exact fixtures／holder均0。品質Web最終a4已部署1c9b82c00343並通過完整資產HTTP核對，app-browser仍待；此批API回復標籤為 `ymes-api:before-qc201202-20261119`。

接續OP105批目前運行API `16a825af0bea`／Web `1f2dff75f080`；API來源 `/opt/ymes/releases/op105-20261119-a1/api`，基於品質API加6份SHA覆檔，Web來源 `/opt/ymes/releases/op105-20261119-a2/web`，沿品質a4僅加OP105。兩者同層manifest與 `/opt/ymes/current-checkpoint.json` 記錄實際映像、來源、回歸與pending GUI／GitHub狀態。OP105 API8組145HTTP／501斷言及Web9項路由／完整資產SHA通過，不宣稱GUI已驗收。回復標籤 `ymes-api:before-op105-20261119`／`ymes-web:before-op105-20261119`。保留每批base與delta來源，不把舊 `fb22a87` 誤當已包含後續批次；未驗收的OPB02等草稿不得混入重建。

每批完成 app-browser 操作驗證並清理測試資料後，再正常 fetch/merge GitHub 新提交，解決衝突、重跑受影響檢查後 push；禁止 force push。若公司端同時有新工作，保留雙方提交與變更，合併不等於已部署或已驗收。

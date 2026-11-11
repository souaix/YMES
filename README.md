# YMES MES

商用 MES 的共用架構基底與第一階段主檔入口。登入、選單、設備與作業站／物料／流程／產品主檔清單及表單採 Vue 3 + TypeScript + Vite；後端 API 與 PostgreSQL 分離。

## 專案內容

- web/：Vue 3 登入入口、選單與全部主檔頁面；提供正式／測試／開發環境選擇，正式區預設，並支援首次登入改密碼。
- src/YMes.Api/：登入 API 與主檔 API。登入帳號連 PostgreSQL；主檔 API 透過 repository 介面切換資料來源，目前只有開發用 Fake 實作。
- src/YMes.Web/：遷移前保留的 MVC 原型，不納入目前 UI 部署；主檔 UI 由 web/ 提供並只透過 API 存取資料。
- src/YMes.Contracts/：Web 與 API 共用的主檔資料契約。
- database/migrations/：YMES 登入帳號與稽核資料表。
- reference/gmail-md-2026-11-07/：使用者提供的主檔資料字典與初始 DDL 參考。
- ARCHITECTURE_BASE.md、SECURITY_BASELINE.md、DEPLOYMENT_PROFILES.md、RELIABILITY_AND_RECOVERY.md：共用架構、安全與部署原則。
- docs/DEVELOPMENT_STYLE.md：兩台電腦共用的開發分工、API 路徑與 KD UI 風格規範。
- docs/AUTHENTICATION.md：登入帳號表、密碼政策與部署設定說明。

## 本機啟動

使用 .NET 9 SDK、Node.js 22 與 PostgreSQL。前端依賴安裝後，在 web/ 執行 npm run dev；所有 UI 頁面由 Vue/Vite 提供，API 在 src/YMes.Api/ 執行 dotnet run。設定 ConnectionStrings__YMes 後，登入 API 才能連到資料庫；資料庫連線密碼只能由環境變數或 Secret Manager 注入。開發用 Fake repository 只替代主檔資料來源，登入仍使用資料庫帳號；本機畫面預覽可另外明確啟用 `YMES_LOCAL_DEMO_AUTH=true`，僅供 Development 使用。

公司電腦可以只跑 UI，透過 Vite 代理連到可達的 YMES API，不需設定資料庫或 SSH。複製 `web/.env.example` 為 `web/.env.local` 並設定 `VITE_API_PROXY_TARGET` 為 API 主機來源。本機 `.env.local` 不提交到 GitHub。共用 API 必須已提供該功能的端點；目前正式環境的主檔 API 在 PostgreSQL repository 完成前預設停用。

正式 Web 位址為 `/YMES`，API 基底路徑為 `/YMES/API`；正式區預設選中。測試區 API 尚未部署時保持空白，避免把測試登入送到正式資料庫。開發區使用本機 API `http://localhost:5080`。

## 第一階段主選單

所有主檔清單、資料表單及關聯設定畫面由 Vue 3 提供；瀏覽器只呼叫獨立 API，正式 API 路徑為 /YMES/API/api/。開發環境預設使用記憶體 Fake repository，API 重啟後會重設；正式環境預設停用主檔 API，直到 PostgreSQL repository 完成。Fake 不是另一個資料庫，也不會寫入 YMES 主檔表。來源版本、資料表和公司端連線方式記錄在 docs/GITHUB_INTEGRATION.md。

- 登入後使用同一套固定側欄與主內容區，版面隨螢幕寬度自適應；首頁主內容先留白。
- 登入頁提供 Demo 登入捷徑，使用 `ymes` 示範帳號。
- 選單以可展開樹狀結構呈現，最多三層；選單項目預設收合。
- 設備管理 → 設備主檔 → `"YMES".tbleqpequipmentbasis`
- 作業站管理 → 作業站主檔 → `"YMES".tblopbasis`（主鍵 `opno`）
- 選單先不處理帳號角色權限；主檔 UI 已整合 GitHub 原型，正式 PostgreSQL repository 仍待接入。

## 安全狀態

登入帳號以 `"YMES".tblusruserbasis` 的 userno、username、password 為唯一來源；密碼欄位已擴充為 varchar(255)，供 ASP.NET Core Identity IdentityV3 格式的 salted、versioned PBKDF2-HMAC-SHA512 雜湊使用。`ym_auth_login_state` 只保存登入鎖定與最近登入時間，不建立另一套角色或帳號。角色/權限值域未在來源字典中定義，API 不自行指定角色。詳見 docs/AUTHENTICATION.md。

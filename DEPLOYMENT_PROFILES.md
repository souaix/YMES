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

- Ubuntu 遠端測試可透過 SSH 操作；IIS 依客戶核准的 Windows Server 管理/部署方式執行。
- 測試、驗收和正式環境分開；未授權不得以正式資料做破壞性測試或 Migration。
- 每次部署保留版本、設定差異、Migration 記錄、健康檢查和回復步驟。
- 部署後驗證 Web/API 健康檢查、認證授權、資料庫連線和關鍵流程。
- 回滾程式版本不代表可回滾 DB；Migration 設計備份前置條件和向前修正方案。
- SSH 私鑰、Windows 管理憑證、正式帳號和環境檔放在受控憑證系統；文件只記取得和輪替流程，不記機密值。

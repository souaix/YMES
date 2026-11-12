# GitHub 主檔原型整合

## 來源

- Repo：souaix/YMES
- 分支：main
- 匯入版本：6fc2e261790e50c80ddce1b06674b210ec9a6636（2026-11-10）
- 來源功能：設備、作業站、物料、流程、產品主檔頁面與 API 契約。

## 整合方式

- web/ 提供 Vue 登入頁、KD 配色、收合式主選單，以及設備、作業站、物料、流程和產品的清單與表單；資料列選取在 Vue 狀態中更新，不重新載入整頁。
- src/YMes.Web/ 保留遷移前的 MVC 原型，不納入目前 UI 部署；src/YMes.Api/ 提供登入 API 與主檔 API。
- 使用者登入後由 API cookie 驗證；Vue 以同源 credential 呼叫 API，不直接連資料庫。
- 公開 Web 路徑維持 /YMES/；API 維持 /YMES/API/。YMES UI 路徑由 Vue 靜態服務提供 SPA fallback；`/YMES/API/*` 轉送到獨立 API 服務。
- 選單依舊系統 Module → Menu Group → Function 呈現三層，分類預設收合；完整 Module/Function 清單由 API 提供，不能只保留設備、作業站、物料、流程、產品五個主檔入口。
- 家用電腦負責 API、資料庫、migration 與遠端部署；公司電腦負責 UI、表單及按鈕流程，透過共用 API 開發，不直接連資料庫或 SSH。
- 兩台電腦都以本 repo 為程式來源，UI 與 API 共用 `src/YMes.Contracts/` 契約；專案目標為 .NET 9。
- KD 色票、自適應版面、精簡登入和主選單規則統一記錄於 [DEVELOPMENT_STYLE.md](DEVELOPMENT_STYLE.md)。

## 資料狀態

一般登入使用 "YMES".tblusruserbasis 與既有登入狀態表。主檔控制器依 `IEquipmentRepository` 和 `IMasterDataRepository` 介面工作。Development 可使用記憶體 Fake provider；Production 使用 PostgreSQL provider，支援設備、作業站、物料、流程、產品主檔頁目前提供的查詢與寫入 API。API 的 launch profile 明確開啟 `YMES_LOCAL_DEMO_AUTH`，讓本機預覽可用 `ymes`／`1234` 登入而不連資料庫；此模式只允許 ASP.NET Development 且不允許 production YMES 環境。

公司電腦不需要直接連資料庫，也不需要 SSH 或部署權限：本機 UI 將 `/YMES/API` 代理到可連線的共用 API。`VITE_API_PROXY_TARGET` 可在未提交的 `web/.env.local` 設定；部署後 Vue 的執行環境設定指向 API 根位址。帳號、API DTO 和按鈕呼叫流程維持相同，資料仍由 API 主機端處理。

Fake repository 只用於獨立的 Development API 驗證表單與按鈕流程，不複製 PostgreSQL。公司 UI 日常呼叫共用 API；目前五個主檔頁的 PostgreSQL API 已實作並部署。

Production PostgreSQL provider 將舊 MES 主檔保留在原表，並以 YMES 自有關聯表支援來源 DDL 尚未提供的流程圖與產品關聯：

- `ym_prsprocess_node`、`ym_prsprocess_edge` 保存 YMES 流程圖，避免猜測 `tblprsnodebasis.nodetype` 的舊代碼語意。
- `ym_product_process`、`ym_product_bom` 保存產品頁的流程指派及 BOM；舊表定義未包含對應關聯。

Migration `0010_postgres_master_data_api.sql` 建立上述延伸表並授予 `ymes_app` 所需權限；`0011_three_level_master_navigation.sql` 將現有五個模組的分類與功能名稱修正為三層。兩份 migration 已套用遠端，YMES Web/API 已部署；瀏覽器已確認三層選單、圖示、收合側欄及五個主檔清單 API。擴充子表、批次匯入及舊 WinForms 的全部交易規則仍列為後續遷移範圍。

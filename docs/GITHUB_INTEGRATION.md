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

一般登入使用 "YMES".tblusruserbasis 與既有登入狀態表。主檔控制器依 `IEquipmentRepository` 和 `IMasterDataRepository` 介面工作；目前只有 `FakeEquipmentRepository` 和 `FakeMasterDataRepository`，在記憶體保存示範資料，API 重啟後會重設。`YMES_MASTER_DATA_PROVIDER` 在 Development 預設為 `fake`，Production 預設為 `disabled`；Fake provider 不允許在 `YMES_ENVIRONMENT=production` 啟用。API 的 launch profile 明確開啟 `YMES_LOCAL_DEMO_AUTH`，讓本機預覽可用 `ymes`／`1234` 登入而不連資料庫；此模式只允許 ASP.NET Development 且不允許 production YMES 環境。

公司電腦不需要直接連資料庫，也不需要 SSH 或部署權限：本機 UI 將 `/YMES/API` 代理到可連線的共用 API。`VITE_API_PROXY_TARGET` 可在未提交的 `web/.env.local` 設定；部署後 Vue 的執行環境設定指向 API 根位址。帳號、API DTO 和按鈕呼叫流程維持相同，資料仍由 API 主機端處理。

Fake repository 適合在獨立的 Development API 驗證表單與按鈕流程，不能代替公司端日常呼叫共用 API，也不會複製 PostgreSQL。若共用 API 尚未提供主檔端點，需在有權維護 API 的主機部署非正式 Fake provider，或完成 PostgreSQL repository；公司 UI 本身不能只靠 repository mock 連到尚未存在的端點。

目前主檔新增、修改、核准及停用僅供 Fake provider 畫面／按鈕流程示範，尚未連線到下列 PostgreSQL 主檔：

- 設備："YMES".tbleqpequipmentbasis（Chamber 子資料也要依實際來源表確認）
- 作業站："YMES".tblopbasis
- 物料："YMES".tblmtlmaterialbasis
- 流程："YMES".tblprsprocessbasis、"YMES".tblprsnodebasis 與連線資料格式
- 產品："YMES".tblprdproductbasis，以及產品途程／BOM 關聯表

下一步接資料庫時，需實作上述 repository 介面的 Npgsql provider，並逐一確認欄位代碼、狀態轉換、DataStamp 與交易規則；Production 在切換到該 provider 前維持停用主檔 API，不會把示範資料寫入客戶資料庫。

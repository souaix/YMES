# MES Web + API（主檔原型）

此原型將 UI 與 API 分成不同 ASP.NET Core 專案、不同服務網址，方便之後部署到不同 Server。

```text
MES.Web (http://localhost:5202)
  └─ MVC UI，左側主選單、右側功能頁
       └─ HTTP/JSON → MES.Api (http://localhost:5201)
            ├─ Equipment API
            └─ Operation / Material / Process / Product APIs
```

三個專案目前都以 `.NET 9` (`net9.0`) 為目標。啟動需安裝 .NET 9 SDK/runtime：

1. 在一個終端機執行 `dotnet run --project MES.Api`。
2. 在另一個終端機執行 `dotnet run --project MES.Web`。
3. 開啟 `http://localhost:5202`。

API 預設網址可在 `MES.Web/appsettings.json` 的 `ApiBaseUrl` 修改；部署時可使用環境設定覆寫，不需把 UI 與 API 放在同一台 Server。兩個服務在正式環境要設定各自可被部署平台存取的綁定位址。

## 主檔功能

- 設備：搜尋/狀態篩選、明細、建立、編輯、核准、刪除/停用與 Chamber 檢視；表單涵蓋設備識別、組織、容量/工時、Auto/EA、QC 與訊息佇列。
- 作業站（Operation Definition）：基本作業、製程段、標準工時、品質旗標及狀態操作。
- 物料：物料識別、規格/單位、安全庫存、批號與效期/開封時限設定。
- 流程：流程版本、核准/目前版本切換及節點/連線 JSON 編輯器；核准前檢查 START/END、節點引用及連線。
- 產品：產品版本、產品流程指派與 BOM 項目；核准前要求至少一條預設流程。
- 以上採舊 MES IssueState：`0=UNFROZEN`、`1=PENDING`、`2=ACTIVE`、`-1=UNUSED`。示範核准由 0 直接轉 2，尚未接正式 Issue Workflow。
- API 使用 `DataStamp` 做並行更新檢查，衝突回 HTTP 409；主檔新增/編輯/停用與引用完整性由 API 執行。

## 資料來源

API 使用記憶體內的示範資料，重啟後會還原，不會連接或修改舊 MES 資料庫。正式化時應將兩個 repository 換成持久化 repository/資料庫，並補上登入授權、正式 Issue Workflow、各欄位代碼查詢、設備 Property/Parameter、完整 Chamber CRUD、流程圖可視化編輯及產品 BOM/流程指派的更多舊 MES 規則。開發環境使用專案目錄中的 Data Protection 金鑰；若 UI 部署多個 instance，正式環境需設定共用且持久的金鑰儲存位置。


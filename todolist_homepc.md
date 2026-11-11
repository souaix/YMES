# 家裡電腦接手清單

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

## 公司電腦目前交付

- Vue 主選單已能呈現三層 Module → Menu Group → Function。
- UI 預計使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。
- Development 在 API 尚未提供選單時，保留五個已開發主檔的簡化導覽；Production 不以簡化選單假裝整套 MES 已完成。
- 選單中尚未遷移的 FunctionNo 會明確標示，避免使用者誤以為功能已可操作。
- 主檔頁仍由 Vue 呼叫獨立 API；Web 不連資料庫。

## 優先一：選單、功能目錄與權限 API

- [ ] 從舊系統取得正式選單資料及排序，來源至少包含 `tblSYSModule`、`tblSYSMenuItem`、`tblSYSFunction`；保留 `MenuType`、`ParentMenuNo`、`ModuleNo`、`MenuNo`、`FunctionNo` 與停用/隱藏狀態。
- [ ] 以舊系統語系資料解析 Module、Menu、Function 的顯示名稱。WinForms 的三層結構為 Module → `MenuType=1` 分類 → `MenuType=0` 功能項。
- [ ] 在 YMES PostgreSQL 建立對應 migration/schema 與可重複匯入的選單 seed/import；來源是舊系統資料，不要手工猜第一層名稱或用程式資料夾名稱代替正式選單。
- [ ] 實作 `GET /api/navigation/menu-tree`，使用現有登入 Cookie，依使用者在舊系統的有效權限回傳可見功能；不要新增第二套帳號或權限規則。
- [ ] 回應符合 `docs/DEVELOPMENT_STYLE.md` 的 `modules → menuGroups → items` 契約，依 ModuleOrder、MenuOrder、FunctionOrder 排序，並保留 ModuleNo、MenuNo、FunctionNo。
- [ ] 確認已完成的五個功能能由 FunctionNo 導向 Vue：`EQP BASIS`、`OP BASIS`、`MTL BASIS`、`PRS BASIS`、`PRD BASIS`；其他功能回傳識別資料，讓 UI 顯示尚未遷移狀態。

## 優先二：五個主檔的正式資料庫與 API

目前 `src/YMes.Api` 的主檔 repository 是記憶體 Fake provider；重啟會重設資料，Production 預設停用主檔 API。請以舊 WinForms、WebService、核心類別和資料字典為依據，逐項新增 PostgreSQL migration/schema、Npgsql repository、DTO/API 與交易規則。

- [ ] 設備：`tbleqpequipmentbasis`、Chamber 關係、設備群/工程群組、設備屬性、參數、QC List；支援清單 CRUD、核准/停用、複製、匯入。
- [ ] 作業站：`tblopbasis`、群組、屬性、參數、QC、Error/Abnormal、Resource/Area、Business Rule、SubOP；支援核准、複製與匯入。
- [ ] 物料：`tblmtlmaterialbasis`、Vendor Map、Property、QC Item、Gum Type；建立物料時從類別複製預設屬性/QC 的動作需同交易。
- [ ] 流程：`tblprsprocessbasis`、Node、Node Relation/XML、APS；支援新版本複製、切換目前版本、流程圖驗證與匯入。
- [ ] 產品：`tblprdproductbasis`、Product Process、BOM、產品 OP/屬性及相關引用；複製、建版本、核准、停用與 BOM 更新需遵守舊交易規則。
- [ ] 所有新增/修改/核准/刪除端點均驗證 IssueState、DataStamp、引用限制與舊系統業務規則；正式環境不得使用 Fake provider。
- [ ] 把五份主檔及產品流程整合的舊表欄位、PK/UK、型態、索引與正式 DDL 對齊；目前精簡資料字典中的推測欄位不可直接當正式 schema。

## 優先三：其餘第一層 Module 與功能

- [ ] 以正式 `tblSYSModule`/`tblSYSFunction` 選單清單作為盤點基準，為每個 FunctionNo 對照舊 WinForms FormName/ExecutionFile、WebService、核心類別、查詢/寫入資料表及權限。
- [ ] 依第一層 Module 分批新增 PostgreSQL schema/migration、Contracts DTO、API、交易/驗證與 Vue 功能頁；每個功能頁的查詢條件、欄位、按鈕及子視窗需能追溯至對應 WinForms。
- [ ] 每一批完成後更新遷移清單：ModuleNo、FunctionNo、WinForms Form、API 路徑、Table、UI 狀態（未開始/進行中/完成）及驗收結果。
- [ ] 未完成的功能維持明確的「尚未遷移」狀態；沒有 API 支援的功能不得以靜態假資料回報成功。

## 優先四：整合與部署

- [ ] 確認 Web/API 分開建置與執行，Web 固定 `/YMES`、API 固定 `/YMES/API`，同一台 Server 可分服務部署。
- [ ] 以 PostgreSQL provider 取代 Production 的 Disabled/Fake 主檔來源；Development Fake 僅供本機預覽。
- [ ] 完成資料 migration/匯入、備份還原、錯誤紀錄與部署設定；憑證、連線字串、`.env.local`、私鑰不得提交 Git。
- [ ] 部署前確認 API 權限、Cookie、HTTPS、CORS/同源反向代理與正式環境設定。

## 驗收完成條件

- 登入後可從 API 取得與舊 WinForms 一致的全部第一層 Module 及三層選單名稱/順序/權限。
- 每個 FunctionNo 都有明確的遷移狀態；標記完成的項目有對應 Vue 頁面、API 與正式資料表，不使用假成功流程。
- 生產資料操作經 API 完成；Web UI 不直接連資料庫；Production 不使用記憶體 Fake provider。
- 主要 WinForms 業務規則、狀態轉換、跨表交易及引用限制已由 API 實作並記錄。

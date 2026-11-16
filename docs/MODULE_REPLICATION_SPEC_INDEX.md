# MES WinForms 復刻規格索引

本索引追蹤從舊 MES WinForms 與 Oracle schema 還原出的畫面、操作規則、資料表及 YMES API 規格。每個功能以獨立 Markdown 規格記錄；未從程式碼或資料庫確認的內容標示「待查」，不以畫面推測成正式規則。

## 規格產出原則

- 以 WinForms Form、Service/Core 類別及 MES Oracle 資料字典為依據；截圖用於核對版面，不取代程式碼與資料表證據。
- 每個功能頁記錄選單階層、Form 名稱、欄位、查詢條件、按鈕、狀態轉換、驗證、交易與資料表。
- 同時記錄 Vue UI 狀態、API 契約及資料庫待辦。公司電腦 Development 可用明確標示的本地假資料完成互動；Production API 尚未完成時，必須如實標示待建置。
- 共用文件：[功能規格範本](MODULE_SPEC_TEMPLATE.md)、[資料庫連線與憑證處理](DATABASE_CONNECTION.md)、[開發分工與 UI 準則](DEVELOPMENT_STYLE.md)。

## 已確認的第一層模組

以下清單已由公司端 PowerShell 5.1 唯讀查詢 MESTEST.TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 取得。第一層名稱依 WinForms 的 ModuleNo + Module 翻譯鍵，從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實。詳細功能編號、分類、FormName 及 ExecutionFile 見 [MES Oracle 選單與功能目錄](MES_MENU_FUNCTION_INVENTORY.md)。目錄資料也不等同使用者實際可見權限，WinForms 另以使用者權限篩選。

| 順序 | ModuleNo | 第一層名稱（已核實者） | MenuItem | Function |
| ---: | --- | --- | ---: | ---: |
| 1 | ENT | 客戶資料 | 10 | 8 |
| 2 | EQP | 設備資料 | 22 | 18 |
| 3 | SMD | 區域資料 | 9 | 8 |
| 4 | MTL | 物料資料 | 10 | 8 |
| 5 | QC | 品質資料 | 49 | 44 |
| 6 | OP | 作業站資料 | 12 | 10 |
| 7 | PRS | 流程資料 | 5 | 4 |
| 8 | PRD | 產品資料 | 41 | 37 |
| 9 | OE | 工單管理 | 16 | 13 |
| 10 | WIP | 在製品管理 | 98 | 112 |
| 11 | EMS | 設備管理 | 28 | 26 |
| 12 | SPC | 品質管理 | 37 | 34 |
| 13 | INV | 庫存管理 | 44 | 38 |
| 14 | PM | 維修保養管理 | 47 | 42 |
| 15 | RPT | 報表管理 | 83 | 74 |
| 16 | IEW | 預警管理 | 6 | 5 |
| 17 | USR | 使用者管理 | 17 | 14 |
| 18 | SYS | 系統管理 | 52 | 47 |
| 19 | ISU | 簽核管理 | 9 | 7 |
| 20 | AUT | 自動執行 | 6 | 4 |

### 標準 WinForms 原始碼模組盤點（候選清單）

以下是 `STD/MESWin` 第一層原始碼目錄及 `frm*.vb` 數量，只用來安排後續搜尋。這些目錄不等於正式 `TBLSYSMODULE` 菜單列，也不能據此推斷第一層顯示名稱、排序或使用者可見權限。

| 原始碼目錄候選 | Form 數 | 原始碼目錄候選 | Form 數 |
| --- | ---: | --- | ---: |
| AUT | 8 | BR | 85 |
| DT | 7 | EMS | 59 |
| ENT | 20 | EQP | 70 |
| FMA | 19 | IEW | 20 |
| INV | 81 | ISU | 18 |
| MSA | 10 | MTL | 25 |
| OE | 32 | OP | 36 |
| PM | 68 | PRD | 144 |
| PRS | 21 | QC | 153 |
| RPT | 143 | SMD | 25 |
| SPC | 135 | SYS | 125 |
| USR | 35 | WIP | 355 |

Oracle 目錄共有 20 個 Module、601 筆 MenuItem、553 筆 Function；MenuType=1 分類 82 筆，MenuType=0 功能項 519 筆，其中 518 筆能對應 TBLSYSFUNCTION。完整來源目錄與 ModuleOrder 已保存至 MES_MENU_FUNCTION_INVENTORY.md。STD/MESWin Form 數仍是輔助盤點值，下一步逐筆依 FunctionNo/FormName/ExecutionFile 對照原始碼和 WinForms 畫面，並用實際權限確認可見性。

## 現有核心主檔遷移狀態

| ModuleNo | 已有畫面 | 規格與來源 DDL | 家用端 Production API |
| --- | --- | --- | --- |
| EQP | 設備設定七項 DEV 頁面 | 設備設定功能規格已分頁；多張設備表已查 DDL | 待逐功能完成 |
| OP | 作業站主檔及簡化 DEV 子功能 | [作業站主檔規格](modules/OP/作業站主檔.md)；18 張作業站相關表來源 DDL/CREATE SQL 已交付 | 待補子功能 API |
| MTL | 物料主檔及簡化 DEV 子功能 | [物料主檔規格](modules/MTL/物料主檔.md)、[物料資料匯入](modules/MTL/物料資料匯入.md)；8 張來源表 DDL/CREATE SQL 已交付 | 主檔/API 部分完成；功能仍待逐項復刻 |
| PRS | 流程主檔 | 既有主檔規格與程式待逐項核對 | 部分主檔 API 已有，擴充待補 |
| PRD | 產品主檔 | 既有主檔規格與程式待逐項核對 | 部分主檔 API 已有，擴充待補 |

其餘 15 個 Module 的 476 個 Function 尚未逐一遷移。先以 MES_MENU_FUNCTION_INVENTORY.md 為工作目錄，再按 ModuleOrder、MenuNo、FunctionNo 追 WinForms Form/Service、相關表、API、Create SQL 及 DEV 畫面。

## 設備資料 → 設備設定

以下順序依使用者提供的 WinForms 選單畫面：

| 功能 | WinForms / 資料來源狀態 | Web 規格狀態 |
| --- | --- | --- |
| 設備供應商基本資料 | Form 與 Service 已對照；Oracle DDL 已查 | [規格](modules/EQP/設備設定/設備供應商基本資料.md)已建立；Vue 已建立，API 待家用電腦 |
| 設備工程群組資料 | Form、Service、主表及 `TBLUSRUSERGROUP` 成員表 Oracle DDL 已追；成員表 PK/欄位/索引已核對 | 規格與 PostgreSQL 空表 CREATE SQL 已建立；Vue Development 假資料頁已建立；YMES schema 比對及 CRUD/成員 API 待家用電腦 |
| 設備類別資料 | WinForms/Service、5 張 Oracle 表 DDL、FunctionNo/FunctionKey 已核實；配件目錄表待查 | Vue DEV 頁已有類別 CRUD/子視窗；匯入改依 WinForms 第一工作表固定欄序、結果列與取消流程；API/配件目錄 DDL 待家用電腦 |
| 設備基本資料 | WinForms 主/編輯/Chamber Form、屬性、參數、QC 關聯、匯入 Form 與 Service 均已追 | [設備基本資料](modules/EQP/設備設定/設備基本資料.md)、[設備屬性](modules/EQP/設備設定/設備屬性設定.md)、[設備參數](modules/EQP/設備設定/設備參數設定.md)、[設備點檢表關聯](modules/EQP/設備設定/設備點檢表關聯.md)、[設備匯入](modules/EQP/設備設定/設備資料匯入.md)規格已建立；設備子功能 DEV Vue 已接線；設備主表與參數表原始 DDL 已由使用者提供並轉成單表 CREATE SQL；其他未匯出關聯表仍待查 |
| 設備群組資料 | WinForms/Service 與兩表 Oracle DDL 已對照；SMD 區域映射依賴待查 | 規格已建立；Vue Development 假資料頁已建立；Production API/SMD DDL 待家用電腦 |
| 設備狀態設定 | WinForms / Service、主表及兩張刪除引用表 Oracle DDL 已追；完整 ValueList/繁中翻譯已核實 | [規格](modules/EQP/設備設定/設備狀態設定.md)已建立；Vue 頁面已依 ValueList 本地化，Production API 已有，家用端待建立引用表 migration 並啟用精確刪除檢查 |
| 設備點檢表設定 | WinForms 主從 Form / Service、主從 Oracle DDL、FunctionNo/FunctionKey、QCType ValueList 已核實 | [規格](modules/EQP/設備設定/設備點檢表設定.md)已建立；Vue DEV 頁已修正正式 QCType 選項/預設，Production API 待家用電腦 |

## 全 MES 盤點狀態

- Oracle 第一層 Module 清單、排序及功能目錄筆數已查；ModuleNo 翻譯標題與實際使用者權限仍待核實，553 個 Function 的 WinForms/API 遷移狀態尚需逐項追蹤。
- 每發現一個 Function，新增或更新對應 Markdown，並在本索引登記 Form、主要資料表、API 狀態及待確認事項。
- 本索引及功能規格需與 UI/API 變更同批更新，供公司與家用電腦共同開發。

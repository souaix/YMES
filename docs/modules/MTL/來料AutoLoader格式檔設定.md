# 來料 AutoLoader 格式檔設定（MLB01）

## 功能識別

| 項目 | 內容 |
| --- | --- |
| MES FunctionNo | `B_MTL AUTOLOADER SETUP` |
| MES FunctionKey | `MLB01` |
| 名稱 | 來料AutoLoader格式檔設定 |
| WinForms 主畫面 | `frmMaterialVendorAutoLoaderSetup` |
| WinForms 定義畫面 | `frmMaterialVendorAutoLoaderSetupDef` |
| DLL | `ML_B_MTLAUTOLOADERSETUP.DLL` |
| 主資料表 | `MESTEST.TBLMTLVENDORALSCRIPT` |
| YMES route | `/YMES/MTL/MaterialVendorAutoLoaderSetup` |
| Vue DEV 頁 | `web/src/components/MaterialVendorAutoLoaderSetupMaster.vue` |

來源程式：

- `Industry_Base/MESWin_Industry_Base/MTL/ML_B_MTLAutoLoaderSetup/frmMaterialVendorAutoLoaderSetup.vb`
- `Industry_Base/MESWin_Industry_Base/MTL/ML_B_MTLAutoLoaderSetup/frmMaterialVendorAutoLoaderSetupDef.vb`
- `Industry_Base/MESWin_Industry_Base/MTL/ML_B_MTLAutoLoaderSetup/frmDyanmicParameter.vb`
- `Industry_Base/MESWin_Industry_Base/MTL/ML_B_MTLAutoLoaderSetup/frmViewTestFileItem.vb`
- `Industry_Base/MESws_Industry_Base/wsMTL/wsMTL_Base.cs`
- `Industry_Base/MEStc_Industry_Base/tcMTL/clsMTLBasis_Industry_Base.cs`

## WinForms 主畫面與操作

主畫面初始化不自動查詢。上方條件為 `MTL Folder Path` 和 `Vendor No`，按 Query 才呼叫 `Industry_Base.wsMTL.LoadMTLVendorALScript`。Core 以 `MTLFolderPath = ...`、`VendorNo = ...` 精確相等篩選；兩欄皆可空白，結果按 MTLFolderPath 排序。Folder Path 與 Vendor No 離開欄位時轉大寫。Vendor No 的 `...` 開啟物料供應商選擇器；定義窗的資料夾選擇按鈕是 Windows FolderBrowserDialog。

Grid 顯示 `MTLFolderPath`、`Description`、`Creator`、`CreateDate`、`Reviser`、`ReviseDate`、`MasterSeparateSign`、`DetailCloumnSeparateSign`、`WITHHEADER`。CreateDate/ReviseDate 格式為 `yyyy/MM/dd HH:mm:ss`。VendorNo、StoreRawData、SendMail、FormatScript 等欄位不顯示於 Grid。選取資料列時，下方唯讀 RichTextBox 顯示該筆 FormatScript。

主畫面按鈕為 Query、Add、Edit、Delete、Copy、Close。Edit 和 Copy 以 MTLFolderPath 取得整筆資料；Delete 支援多選，逐筆刪除，遇第一筆失敗即停止，若已有刪除成功則重新查詢。Add/Edit/Copy 成功後重新查詢並定位至該筆。此功能沒有 IssueState 或簽核流程。

## 定義畫面欄位與規則

定義窗包含以下區塊與按鈕：

- **Format File Basic Data Setting**：MTL Folder Path、Description、Head Exists、Master Separate Sign、DetailCloumn Separate Sign、Allow Send Mail。
- **Header**：Columns Order；VendorNo Order、MaterialNo Order、MaterialLotNo Order。
- **Body**：Data Order；MaterialsNo Row Order、MaterialLotNo Row Order、CarrierBox Order、Location Row Order、ComponentNo Row Order、Laser Row Order。
- **Test File Content**：測試檔選擇、View、欄位/資料預覽。
- **Dynamic Test Parameter**：測試參數編號和欄位序號。
- **Test Item Setup**：由 `Industry_Base.wsSYS.LoadTestRecipe` 載入測試配方/參數。
- **Columns Transform Setup**：進階欄位轉換設定。
- **格式檢查**：呼叫 `Industry_Base.wsSYS.ChkAutoLoaderSetup`，並可搭配測試檔檢查腳本。
- 底部操作：Apply、Confirm、Close。

來源預設 `MasterSeparateSign=':'`、`DetailCloumnSeparateSign=','`、Head Exists 勾選；Allow Send Mail 預設 No。StoreRawData 在目前定義窗的儲存程式中固定傳 0，RawDataFormatScript 的編輯與檢查程式碼已註解，編輯時不應清掉資料庫既有 RawDataFormatScript。

基本檢查：MTL Folder Path 必填且最多 400 字元；Description 最多 255 字元；兩個分隔符號最多 10 字元。勾選 Head Exists 時 Master Separate Sign 必填；DetailCloumn Separate Sign 必填。Columns Order、Data Order 必須大於 0，Data Order 必須大於 Columns Order。其餘固定欄位序號必須有值，0 表示不使用該位置。資料表 PK 阻擋重複 MTLFolderPath。

FormatScript 是由欄位序號產生的多行文字，格式為 `Key=>>Sequence`：

```text
VendorNo=>>...
MaterialNo=>>...
MaterialLotNo=>>...
ColumnsStart=>>...
DataStart=>>...
D_MaterialNo=>>...
D_MaterialLotNo=>>...
D_CassetteNo=>>...
D_SlotNo=>>...
D_ComponentNo=>>...
D_LaserMark=>>...
<TestParameterNo>=>>...
```

`D_SlotNo` 對應 WinForms 的 Location Row Order；此拼字為原始腳本格式，API 不要自行改名。動態測試參數以 `TestParameterNo=>>Sequence` 逐行追加。

## Add、Edit、Copy、Apply

- **Add**：Folder Path 可輸入/選取；Creator/CreateDate 由登入者和伺服器時間建立。StoreRawData=0；儲存生成的 FormatScript、分隔符、WithHeader、SendMail 和 Description。
- **Edit**：Folder Path 鎖定；載入腳本後回填欄位序號和動態參數；更新 Reviser/ReviseDate、分隔符、WithHeader、SendMail、Description、FormatScript。舊 UI 沒有提供 VendorNo 編輯欄，修改時保留資料庫 VendorNo。
- **Copy**：使用選取資料複製欄位設定，但 Folder Path 清空，建立新的主鍵；Creator/CreateDate 使用目前登入者/時間。
- **Apply**：儲存但保持定義窗開啟；Confirm 儲存後關閉；Close 不保存尚未套用的變更。
- **刪除**：以主鍵 MTLFolderPath 實體刪除，無相關表 FK。

### DataStamp 原始碼差異待核

WinForms Edit 會送出 DataStamp，Service 也解析此值；但目前檢視的 `clsMTLBasis_Industry_Base.EditMTLVendorALScript` 宣告及 UPDATE SQL 未包含 DataStamp 條件/更新，Service 對 Core 的呼叫卻使用 `DataStamp`、`ReviseDate` named arguments。請家用端以其實際使用的 Industry_Base 版本再次核對此呼叫鏈。YMES API 應以 `dataStamp`（包含來源 nullable DEFAULT 0）做條件更新並遞增版本，回傳衝突時要求重查；不可把 nullable 版本值直接當成一定非 NULL。

## Oracle 結構

Oracle DDL 是公司端以 Windows PowerShell 5.1、Oracle.ManagedDataAccess，從本機既有受保護連線唯讀呼叫 `DBMS_METADATA.GET_DDL` 並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS` 取得。只查結構，未讀 MES 業務資料、未在 Oracle 執行 DDL。

| 欄位 | Oracle 型別 | NULL / DEFAULT |
| --- | --- | --- |
| MTLFOLDERPATH | VARCHAR2(400) | NOT NULL |
| VENDORNO | VARCHAR2(20) | NULL |
| FORMATSCRIPT | CLOB | NULL |
| DESCRIPTION | VARCHAR2(255) | NULL |
| CREATOR | VARCHAR2(10) | NOT NULL |
| CREATEDATE | DATE | NOT NULL |
| REVISER | VARCHAR2(10) | NULL |
| REVISEDATE | DATE | NULL |
| STORERAWDATA | NUMBER(1,0) | NOT NULL DEFAULT 0 |
| SENDMAIL | NUMBER(1,0) | NOT NULL DEFAULT 0 |
| MASTERSEPARATESIGN | VARCHAR2(10) | NULL |
| DETAILCLOUMNSEPARATESIGN | VARCHAR2(10) | NULL |
| RAWDATAFORMATSCRIPT | CLOB | NULL |
| WITHHEADER | NUMBER(1,0) | NULL |
| DATASTAMP | NUMBER(11,0) | NULL DEFAULT 0 |

PK 為 `MTLFOLDERPATH`。目前只有 PK 唯一索引；無外鍵或其他索引。Oracle 完整來源 DDL：`database/reference/mestest_mtl_autoloader_source_ddl.sql`。YMES PostgreSQL 空表參考：`database/reference/mestest_mtl_autoloader_create.sql`。建立 migration 前必須先比對 YMES live schema 和既有 migrations，保留 YMES 擴充欄位，不可直接盲跑參考 SQL。

## YMES Vue 與 API 契約

此 Vue 頁只在 Development Preview 使用 DEV 假資料與 localStorage。正式環境不使用假資料；初始 Grid 保持空白，使用者按 Query 才讀 API。表單採 KD 淡黃色輸入底色、一般灰色框線；右側直排 Add/Edit/Delete/Copy/Close 對應 WinForms 按鈕。

### 既有資料操作

- `GET /api/material-vendor-autoloader-scripts?mtlFolderPath=&vendorNo=`：精確相等條件查詢；回傳依 Folder Path 排序的完整欄位及稽核值。
- `POST /api/material-vendor-autoloader-scripts`：新增。伺服器設定 Creator/CreateDate；檢查 Folder Path 長度、唯一性及必填欄位；StoreRawData 固定 0；CLOB 接收完整 FormatScript。VendorNo 由舊 UI 不可編輯，Add/Copy 保持 NULL。
- `PUT /api/material-vendor-autoloader-scripts/{mtlFolderPath}`：更新非鍵欄位，伺服器設定 Reviser/ReviseDate；執行 DataStamp 條件更新及遞增並回傳新版本。
- `DELETE /api/material-vendor-autoloader-scripts/{mtlFolderPath}`：以主鍵刪除。UI 多筆刪除逐筆呼叫；每筆成功後立即可見，失敗停止後續筆數。
- `GET /api/material-vendor-autoloader-scripts/directories`：回傳伺服器設定的允許目錄清單。瀏覽器不能直接選取 MES Server 的任意目錄，也不可暴露伺服器檔案系統。
- Vendor picker 沿用 `GET /api/material-vendors`。
- Test Item Setup 使用 `GET /api/test-recipes?recipeType=MTL_AUTOLOADER`，對應舊 `LoadTestRecipe`。
- `POST /api/material-vendor-autoloader-scripts/validate`：接收 FormatScript、分隔符、WithHeader 和選取的測試檔內容/解析資料；伺服器承接舊 `ChkAutoLoaderSetup` 檢查並回傳 `{ valid, message }`。

Production API 應採參數化 SQL；FormatScript/RawDataFormatScript 以文字型別保存。新增/修改稽核欄由伺服器登入身分和伺服器時間設定，不信任瀏覽器傳入值。此功能不新增 audit table 或外鍵。

## 明確簡化項目

- DEV 只直接預覽 CSV/TXT 的純文字行；舊 WinForms 的 Excel（NPOI）解析、動態轉換公式、完整雙 Grid 欄位映射/測試配方互動改由正式 API／後續轉換介面補齊。
- DEV 格式檢查只驗證必要欄位和 FormatScript 生成；舊 `ChkAutoLoaderSetup` 的完整業務 parser 必須由 API 實作，不能把 DEV 檢查當成正式檢核。
- Columns Transform Setup 保留入口和限制說明，完整轉換運算未復刻。


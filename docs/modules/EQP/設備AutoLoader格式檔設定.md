# 設備 AutoLoader 格式檔設定（B_EQP AUTOLOADER SETUP / EPB01）

## 範圍與來源

- WinForms Function：`B_EQP AUTOLOADER SETUP`；FunctionKey `EPB01`；DLL `EP_B_EQPAUTOLOADERSETUP.DLL`。
- WinForms 主畫面：`Industry_Base/MESWin_Industry_Base/EQP/EP_B_EQPAutoLoaderSetup/frmEquipmentAutoLoaderSetup.vb` 及 `.resx`。
- 編輯表單：`frmEquipmentAutoLoaderSetupDef.vb`；批次預設：`frmEquipmentAutoLoaderSetupDefault.vb`。
- 子設定視窗：`frmDyanmicParameter.vb`、`frmRawDataSet.vb`、`frmNonNumericFieldSet.vb`、`frmCoordOnfoSetup.vb`、`frmTestRecipeSet_Single.vb`、`frmTestRecipeSet_Multi.vb`、`frmViewTestFileItem.vb` 等。
- Service：`Industry_Base/MESws_Industry_Base/wsEQP/wsEQP.cs`；Core：`Industry_Base/MEStc_Industry_Base/tcEQP/clsEQP.cs`；驗證：`wsSYS_Base.cs` 的 `ChkAutoLoaderSetup` 與 `ChkAutoLoaderSetup_Raw`。
- Oracle 原始結構：`database/reference/mestest_eqp_autoloader_source_ddl.sql`；YMES 空表 SQL 參考：`database/reference/mestest_eqp_autoloader_create.sql`。

此功能在三層選單 `設備資料 → 行業包 → 設備AutoLoader格式檔設定`，路徑 `/YMES/EQP/EquipmentAutoLoaderSetup`。WinForms 頁面不是設備基本資料表單；資料鍵由設備編號與檔名格式組成。

## 主畫面

1. 初次開啟不自動查詢。上方提供設備類別、設備編號及查詢按鈕；兩個條件可空白，WinForms Core 以 `TBLEQPEQUIPMENTBASIS` 左連接補設備名稱/類別資料，有條件時分別以 `EquipmentNo`、`EquipmentType` 精確相等查詢。輸入框離開時轉大寫。
2. 表格列出設備編號、檔名格式、設備類別、設備名稱、管理群組、設備分類、供應商編號、設備型號、財產編號、批量限制、批數限制、批量單位、自動設備、EAP Controller、設備配方、最大時間、固定機時、變動機時、機時單位批量、點檢表編號、說明、建立/修改人與日期、主檔/明細分隔符、儲存原始值、多晶圓、批次格式、座標資訊、表頭及檔案類型。`FILETYPE` 是 WinForms 依 `MultiWafer`/`BatchFormat` 組合顯示的衍生值，不是資料表欄位。
3. 選取資料列會同步顯示 `FormatScript` 與 `FormatScript_Raw` 兩個唯讀解析腳本區。複合鍵 `(EquipmentNo, FileNameFormat)` 定位記錄。
4. 操作按鈕：新增(A)、修改(E)、刪除(D)、複製(C)、預設、關閉。刪除可多選，舊畫面逐筆執行；若中途錯誤，保留已完成筆數並顯示失敗。沒有 IssueState/簽核狀態篩選或核准按鈕。

## 新增、修改與複製表單

主表欄位：EquipmentNo、FileNameFormat、Description、Allow Send Mail、Store Raw Data、MasterSeparateSign、DetailCloumnSeparateSign、MultiWafer、BatchFormat、HaveCoordInfo、WithHeader、IsCoordinateY、FormatScript、RawDataFormatScript。EquipmentNo/FileNameFormat 為必要欄位，修改時設備編號不可變；檔名格式長度 100，說明長度 255。建立/修改人與時間由伺服器記錄。

四種檔案型別是來源欄位組合，不是獨立欄位：

| UI 類型 | MultiWafer | BatchFormat |
| --- | ---: | ---: |
| 單片 | 0 | 0 |
| 多片 | 1 | 0 |
| 設備生產資料 | 0 | 1 |
| 單批次 | -1 | 0 |

表頭設定有 Head Exists、EquipmentNo/ProgramNo/ComponentNo/TestDate 欄位順序、ColumnsOrder、DataOrder。生產資料另設明細 LotNo / ComponentNo 位置。`HaveCoordInfo=1` 時維護 X/Y 軸欄位索引；IC Ingot 特殊路徑可用 `IsCoordinateY` 控制 Y 軸判斷方式。測試檔選取後，WinForms 讀取檔案並在 Header/Body 檢視窗顯示解析預覽。

表單頁籤/子功能：

- **Summary EQC / Dynamic Parameter**：指定測試參數與檔案欄位或公式。參數需符合可用的核准測試配方；格式依單片/多片/批次使用不同轉換規則。
- **Original Value / Raw Data**：指定原始欄位回存順序；啟用 Store Raw Data 時，需有有效原始欄位對照。
- **TestFile UnNumeric**：設定非數值欄位及 `DataType`，允許值應使用原系統 `ValueListProvider`，不能由 UI 猜代碼。
- **Test Recipe Define**：維護 `(EquipmentNo, FileNameFormat, TestRecipeNo)`、Calculation、Formula、FileField 對照。載入候選項應依設備及現有配方服務查詢。
- **座標資訊**：座標子表每筆為軸類別及欄位索引；X/Y 軸別在資料表以 `CoordAxisType` 表示。

### 格式腳本

WinForms 會由位置對照產生解析內容：

```text
WaferId=>>[ComponentNo 欄位位置]
TestDate=>>[TestDate 欄位位置]
EquipmentNo=>>[EquipmentNo 欄位位置]
ProgramNo=>>[ProgramNo 欄位位置]
ColumnsStart=>>[ColumnsOrder]
DataStart=>>[DataOrder]
[TestParameterNo]=>>[欄位位置]
```

多片格式再產生 `D_ComponentNo`；設備生產資料產生 `D_ComponentNo=>>0` 與 `D_LotNo`。Summary EQC 參數可能生成以資料庫暫存表公式或測試檔欄名為來源的轉換語法；Raw Data 對照另外寫入 `RawDataFormatScript`。Vue 頁目前提供主要欄位與可編輯子列，但使用簡化 generator 預覽；API 端需以 WinForms 規則及 `ChkAutoLoaderSetup`/`ChkAutoLoaderSetup_Raw` 等價驗證腳本，不能信任用戶端生成結果。

### 驗證與儲存

- 必填 EquipmentNo、FileNameFormat；檔名格式上限 100、說明 255；EquipmentNo 上限 50。
- Head Exists 啟用後檢查必要 Header 欄位順序；座標啟用時檢查 X/Y 定義，但 IC Ingot 對 Y 軸有例外規則。
- Store Raw Data 啟用時檢查 Raw Value 對照；Summary EQC / Dynamic Parameter 要存在於適用 Test Recipe；NonNumeric datatype 要符合來源選項。
- Add/Edit 把主列與提交的 TestRecipe、UnNumericField、CoordInfo 子列置於同一交易，或採明確可復原方案；來源表沒有 FK，資料庫不會自動 cascade。API 必須區分「沒有提交子集合」與「提交空集合」，避免更新時誤刪。
- Delete 需在單一交易明確刪除四張表中相應設備/檔名複合鍵的子列與主列。多筆 UI 刪除維持逐筆執行與部分完成回報。
- Oracle 主表 `DataStamp` 為 nullable `DEFAULT 0`，舊 Core 修改 SQL不做樂觀鎖。新 API 使用 `DataStamp` CAS：NULL 原值以 `IS NULL` 比較；更新後 `COALESCE(DataStamp,0)+1`。新增時 audit/DataStamp 由伺服器建立，前端不得指定 audit。
- Copy 以完整子設定複製成新 `(EquipmentNo,FileNameFormat)`，新資料重設建立稽核，不覆蓋現有鍵。
- 預設套用來源設備/檔名格式至其他設備，排除來源設備及已有相同檔名格式的設備；每個目標都是新增資料，需複製主列及全部子設定、重設 Creator/CreateDate，不覆寫現有資料。

## 來源資料表及索引

公司端透過既有受保護 Oracle 連線，以 Windows PowerShell 5.1 + `Oracle.ManagedDataAccess` 唯讀取得 `MESTEST` 字典/DDL。不要用 DBeaver 畫面擷取，也不要把 IP、帳號、密碼或連線字串寫進 repo。沒有讀取 MES 業務資料，沒有在 Oracle 執行 DDL。

| Oracle 表 | 用途 | 主鍵/唯一索引 | 外鍵 |
| --- | --- | --- | --- |
| `TBLEQPALSCRIPT` | 主設定與兩段 CLOB 腳本 | 無 PK constraint；唯一索引 `PK_TBLEQPALSCRIPT(EquipmentNo,FileNameFormat)` | 無 |
| `TBLEQPALSCRIPT_TESTRECIPE` | 測試配方明細 | PK `(EquipmentNo,FileNameFormat,TestRecipeNo)` | 無 |
| `TBLEQPALSCRIPT_UNNUMERICFIELD` | 非數值欄位 | PK `(EquipmentNo,FileNameFormat,FieldName)` | 無 |
| `TBLEQPALSCRIPT_COORDINFO` | 座標欄位位置 | PK `(EquipmentNo,FileNameFormat,CoordAxisType)` | 無 |

三個明細表只有來源 PK unique index；沒有其他 index 或 FK。Oracle `DATE` 對應 PostgreSQL `timestamp without time zone`，CLOB 對應 `text`，NUMBER 精度、nullable 與 default 依空表 SQL 保留。`DETAILCLOUMNSEPARATESIGN` 是來源拼字，不能自行改欄名。來源 DDL 與 CREATE SQL 只供 YMES schema 比對；不可直接於 MES Oracle 執行或略過差異檢查。

## Vue 畫面與簡化項目

- Vue 開發頁 `/YMES/EQP/EquipmentAutoLoaderSetup`，掛在設備資料下的行業包分類。DEV 使用獨立 localStorage 假資料；Production 僅呼叫 API，不回退 localStorage。顏色沿用 KD 黃色輸入欄、淺灰框線與橘色主要按鈕。
- 已包含初次空白、設備類別/編號查詢、設備查找、多選、雙腳本檢視、新增/修改/複製/多筆刪除/預設套用、稽核欄、5 個設定頁籤、基本欄位檢查及本機文字測試檔預覽。
- 暫時簡化：WinForms 全部查找視窗、豐富檔案解析器與 Excel/多格式預覽、Summary EQC 公式/暫存表編輯器、Raw Data 完整欄位解析、NonNumeric 原始 ValueList、測試配方規則與 IC Ingot 設備類別判斷。API 端必須完整實作並驗證；DEV 示例不得當正式 ValueList。
- 原主 Grid 有大量設備屬性欄，Vue 以橫向捲動資料表呈現；沒有重製 WinForms UltraGrid 原生外觀，但保留欄位與操作。

## YMES API handoff

以下為家用端待實作契約，落地時遵循現有 `.NET 9/10` API、權限服務及 DTO 慣例。先檢查 YMES PostgreSQL schema/migrations，禁止重建已存在資料表。

1. `GET /api/eqp/auto-loader-scripts?equipmentNo=&equipmentType=`：兩條件可空；依來源 exact match 查設備編號/類別，回主 Grid 欄位、兩段腳本、DataStamp 及三張子表的 nested detail DTO（或另設同鍵 details endpoint）。EquipmentType/Name 等欄位 join YMES 設備主檔。
2. `GET /api/eqp/auto-loader-scripts/options/equipment?prefix=&equipmentType=`：回設備編號、類別、名稱供編輯查找；查 Default 候選時另傳 `fileNameFormat`，回 `hasMatchingFormat`，只列授權可見設備。
3. `GET /api/eqp/auto-loader-scripts/options/test-recipes?equipmentNo=&fileNameFormat=`：回候選配方/參數與核准狀態。先從 WinForms lookup service 查明實際來源，再連既有 YMES API/表；不能以 DEV 樣本當候選。
4. `POST /api/eqp/auto-loader-scripts`：建立主列與提供的 children，單一交易；設備與配方參照、必填/長度、格式旗標、子表鍵由 server 驗證。Request 不接受 Creator/CreateDate/Reviser/ReviseDate/DataStamp；這些由 server 設定。
5. `PUT /api/eqp/auto-loader-scripts/{equipmentNo}/{fileNameFormat}?dataStamp=`：修改主欄位與提交的子集合，採 CAS；路徑值 URL encode，回完整資料。DataStamp 只放在 CAS query，不放 request body；回傳 audit 由 server 設定。
6. `DELETE /api/eqp/auto-loader-scripts/{equipmentNo}/{fileNameFormat}?dataStamp=`：交易中刪除子列及主列；409 回報資料已變更或不存在。UI 多選逐筆呼叫並顯示部分成功數。
7. `POST /api/eqp/auto-loader-scripts/{equipmentNo}/{fileNameFormat}/default?dataStamp=` body `{ "equipmentNos": ["..."] }`：一次交易複製給多台尚未有同檔名格式的設備，不覆寫；以 query DataStamp 檢查來源。回傳 `{ createdEquipmentNos, existingEquipmentNos, invalidEquipmentNos }`，主列和所有子設定一併複製。Audit 由 server 重設。
8. 寫入端點檢查登入及 `B_EQP AUTOLOADER SETUP` 功能權限，audit 由伺服器產生；腳本驗證及 `ChkAutoLoaderSetup` / `_Raw` 等價邏輯不可以只在瀏覽器執行。

主 DTO 建議包含 `equipmentNo,fileNameFormat,description,creator,createDate,reviser,reviseDate,storeRawData,sendMail,masterSeparateSign,detailCloumnSeparateSign,multiWafer,rawDataFormatScript,batchFormat,haveCoordInfo,withHeader,isCoordinateY,dataStamp,formatScript,testRecipes,unNumericFields,coordInfo`。子欄位遵循 Oracle DDL，不另外假造資料庫欄位。

## 家用端交接

- 讀 root `todolist_homepc.md` 的 `EQP B_EQP AUTOLOADER SETUP` 區塊；先比較 YMES 現有 4 張表與既有 migrations，再決定是否新增 forward migration。
- 完成 API 交易、CAS、測試配方來源、設備 lookup、複製和 Default 套用。部署後更新 API/migration/DDL 索引及本規格，通知公司端 pull，再逐欄對接 Production。
- 公司端未執行 build、test 或瀏覽器驗收；不可把 DEV localStorage 假資料視為 Production 完成。

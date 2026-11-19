# PD102 產品 RecipeGroup 指派

## 功能入口

- PD102 `frmProductBasis_THEIL_BU1.btnRecipeGroup_Click` 從目前產品列取 `ProductNo`、`ProductVersion`、`IssueState`，經 `ObjectFileHandler("PD_ProductRecipe")` 開啟 `STD/MESWin/PRD/PD_ProductRecipe/frmProductRecipe.vb`。
- 此頁維護產品/版本與產品 Recipe Group 的指派關係，不是 Recipe Group 主檔。Recipe Group 主檔查找由 `frmProductRecipeDef` 載入。
- `IssueState` 雖由入口傳入，`frmProductRecipe` 沒有依它停用按鈕；不得把它誤記成只可編輯未核准產品。

## WinForms 畫面

| 視窗 | 尺寸 | 控制項與行為 |
|---|---:|---|
| Product Recipe | 684×486 | UltraGrid 在左側，欄位只顯示 `RecipeGroup`；`ProductNo`、`ProductVersion` 隱藏。ADD、DEL 在右上，Close 在右下；EDIT 與 Find 在 Designer 設 `Visible=False`。 |
| Product Recipe Define | 392×150 | GroupBox `Product Recipe`；`Recipe Group` 黃底唯讀欄位與 `...` 查找；Confirm、Close、Apply。Apply 初始停用，欄位值變更後啟用。 |

清單查詢由 `LoadPRDRecipe` 載入目前產品/版本的 `TBLPRDRECIPE` 關聯。多列 Delete 使用 UltraGrid 多選，先確認，再逐筆刪除；任一筆失敗就停止，已成功的刪除不回滾，至少一筆成功後重新載入清單。

## 新增與查找

1. ADD 開啟 `frmProductRecipeDef` 的 Add 模式，傳入 ProductNo/ProductVersion。
2. 表單呼叫 `LoadPRDRecipeBasis`，服務預設 `CurVersion=1`、`IssueState=2`；Core 再依 RecipeGroup、RecipeVersion 排序。查找視窗由此資料集開啟，欄位輸入唯讀，只能按 `...` 選擇。
3. 候選代表已核准且目前版本的產品 Recipe Group。選取後可按 Apply 新增並留在對話框，或 Confirm 新增並關閉。Close 放棄尚未新增的選擇。
4. 每次新增只寫 `TBLPRDRECIPE(ProductNo, ProductVersion, RecipeGroup)`；該表沒有 RecipeVersion。資料庫複合 PK 阻止同產品版本重複指派。
5. WinForms 會把已指派群組保留在 lookup 候選；再次新增會由資料庫 PK 拒絕。DEV UI 也檢查重複並提示。Apply 已成功後再按 Confirm 時，DEV 直接關閉，不會重送相同新增。

EDIT 雖在程式中有 handler 和 Define Edit 分支，但主畫面的 EDIT 按鈕永遠隱藏；`EditPRDRecipe` service 沒有執行 Core 更新，只回成功。因此此頁不提供 Edit API。

## Service/Core 邏輯與資料表

- `wsPRD.LoadPRDRecipe` → `clsProduct.LoadPRDRecipe`：以 ProductNo/ProductVersion 篩選 `TBLPRDRECIPE`。
- `wsPRD.LoadPRDRecipeBasis` → `clsProduct.LoadPRDRecipeBasis`：查目前版本、已核准的 `TBLPRDRECIPEBASIS`，提供新增 lookup。
- `wsPRD.AddPRDRecipe` → `clsProduct.AddPRDRecipe`：新增一筆關聯。
- `wsPRD.DelPRDRecipe` → `clsProduct.DelPRDRecipe`：有 RecipeGroup 時刪該關聯；Core 若 RecipeGroup 省略則刪掉整個產品版本的所有關聯。新 API 必須要求明確 RecipeGroup。
- PD102 產品 Copy 服務會呼叫 `clsProduct.CopyPRDRecipe`：先清除目標產品版本關聯，再複製來源 RecipeGroup。產品 Copy/Version API 必須保留這項行為。
- Delete 在 UI 逐筆送出；新 API 若提供批次操作，應回傳逐筆結果並保留既有部分成功語意。
- Oracle 字典未定義 FK 或 Trigger；產品/版本與 RecipeGroup 關聯只有複合 PK unique index。應用 API 仍需核對登入者可操作該產品，並在新增時重查 RecipeGroup 是否已核准且為目前版本。

Oracle DDL 由公司電腦 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護的 MESTEST 連線取得 `DBMS_METADATA.GET_DDL` 與字典。只查 `TBLPRDRECIPE`、`TBLPRDRECIPEBASIS` metadata，未讀業務列、未執行 Oracle DDL/DML。欄位/PK/index/FK/trigger 摘要及 SQL 參照：

- Oracle source DDL：[`mestest_prd_recipe_group_source_ddl.sql`](../../../database/reference/mestest_prd_recipe_group_source_ddl.sql)
- PostgreSQL CREATE-only：[`mestest_prd_recipe_group_create.sql`](../../../database/reference/mestest_prd_recipe_group_create.sql)

PostgreSQL CREATE-only 僅供 YMES 家用端比對。先查 live schema、migration、entity/repository；只在 assignment 表或 lookup 主檔確實缺少時建立 forward migration。`TBLPRDRECIPEBASIS` 是其他 Recipe 主檔功能共用的 lookup，不可因本頁交接就重複建表。

## DEV 畫面與家用端 handoff

- `ProductRecipeGroupSetup.vue` 以 684×486 清單、392×150 Define 對齊 WinForms；DEV 有 approved/current 假 lookup、多列 Delete、Add/Apply/Confirm/Close，僅寫產品關聯 localStorage。公司端初始 handoff 的 Production 模式為唯讀；家用端正式接線狀況見下節。
- 建議 API 語意：GET 產品版本 RecipeGroup 指派；GET lookup 僅回已核准且目前版次；POST 新增一個 RecipeGroup；DELETE 必須帶單一 RecipeGroup。沒有 Edit endpoint。
- Legacy `IssueState` 不控制此頁寫入；是否要在新 API 增加簽核狀態限制需由業務另定，不可把 legacy 行為說成已有限制。

## 家用端實作維護（2026-11-19，正式驗收見末節）

### 實際檔案與端點

- API：`src/YMes.Api/Products/PrdRecipeGroupController.cs`、`PrdRecipeGroupModels.cs`、`PrdRecipeGroupRepository.cs`；Vue 沿用 `web/src/components/ProductRecipeGroupSetup.vue`，`ProductBasisMaster.vue` 只增加該子視窗的 `apiBaseUrl` 傳入。Controller 自行以既有 `NpgsqlDataSource` 建立 repository，沒有 Program/DI 修改或平行產品主檔。
- 下表路徑前綴為 `/api/products/{no}/versions/{version}/recipe-groups`。所有端點要求登入；角色/FunctionKey ACL 依目前使用者決策暫緩。產品版本必須實際存在，API 不自行增加產品 IssueState=0 的限制。

| Method | 路徑 | 請求與實際回傳 |
|---|---|---|
| GET | 前綴本身 | 原始 assignment 陣列，每筆只有 ProductNo、ProductVersion、RecipeGroup |
| GET | `/options` | 原始 lookup 陣列：RecipeGroup、RecipeVersion、Description、IssueState、CurVersion；只列 IssueState=2 且 CurVersion=1 |
| POST | 前綴本身 | body `{ "recipeGroup": "…" }`；回實際 INSERT RETURNING 的三欄 DTO |
| DELETE | `/{recipeGroup}` | 必須明確指定一個 RecipeGroup，回該筆已刪除的三欄 DTO；不提供清空全部或 Edit endpoint |

- ProductNo/Version 的可信來源共用既有 `PostgresMasterDataRepository.ProductContext.cs` 的 `ReadProductVersionContext`，與產品 Program 指派共用。以產品來源表的 canonical 鍵操作；大小寫比對若命中多個產品版本則 409，不任選一筆。讀取用父列 `FOR SHARE`；新增/刪除用 `FOR UPDATE`，在同一交易內完成子列操作。
- 新增時必須重新查核 RecipeGroup 已核准且目前版，並持有 lookup 父列 `FOR SHARE` 到提交；不信任剛才查找的前端資料。多個已核准目前版或僅大小寫不同而無法唯一判別的候選回 409。重複 assignment 回 409；刪除不存在回 404，大小寫重複的 assignment 不批次刪掉，回 409 要求整理。
- `tblprdrecipe` 沒有 DataStamp、IssueState 或 audit 欄，不新增虛構版本欄位、核准流程或 CAS token。此頁以明確三欄複合鍵、來源父列鎖與交易處理會員關係的新增/移除；可信 principal 只記入伺服器操作紀錄，不寫入來源沒有的欄位。RecipeGroup/ProductNo/Version 長度依來源 50/50/5 驗證。
- 缺表回 503；存在但無權讀寫的表也回 503，不把權限不足誤判為「沒有資料」。optional module 的 metadata 使用 `pg_catalog` 檢查，不用依角色可見性過濾的 `information_schema.tables` 來決定略過。

### Apply、刪除與錯誤畫面

- Production 模式直接讀寫 API，不使用 props 假關聯或 DEV lookup 回退；只有明確 DEV 預覽才維持原 localStorage/emits 路徑。所有產品狀態的 ADD/DEL 維持來源規則，不以主頁可否 Edit 推導子視窗權限。
- Apply 使用真實 server DTO 對齊清單、目前作用列與已套用的 RecipeGroup；留在 Define 供下一筆查找。Apply 成功後未再選新值而按 Confirm，只關閉，不重送同一 POST。重新查找新值才啟用 Apply；Confirm 新增成功則關閉。
- 查找仍保留已指派群組；重複新增明確提示，不替使用者隱藏來源候選。選項載入失敗顯示錯誤與空候選，不顯示 DEV 資料。
- 多選 Delete 先以共用頁內確認視窗確認，再逐筆 DELETE；遇錯停止，前面已成功的筆數與結果保留。第一筆即失敗也重新載入清單；清楚顯示成功筆數及原始錯誤。
- 未新增的查找選擇在 Close/關閉整頁時使用共用放棄確認；請求進行中鎖住按鈕。產品主頁此子窗的外層遮罩不再用背景點擊直接卸載元件，避免繞過 busy/放棄確認。API 錯誤取 ProblemDetails detail/message/title，非 JSON 保留原文字；401 走既有登入頁。

### 產品版本複製與實體刪除協調

- 已存在的 `CreateProductVersion` 同交易加入本模組與 Program 指派複製：先以共用 reader 鎖來源 canonical 產品，建立並鎖新產品版本，再依固定 RecipeGroup → Program 順序呼叫兩個 helper，最後才 commit。失敗會連同新版本、既有流程/BOM 複製一起回復。
- Recipe helper 先保留來源完整群組集合，再依 RecipeGroup/RecipeVersion 排序鎖查找父列；先清目標關聯再複製來源。此為 CopyPRDRecipe 語義，不用「目前已核准」篩選來漏掉既有群組；若舊 assignment 的 RecipeGroup 主檔已不存在，回 422，整筆版本交易拒絕。assignment optional 表完全不存在時代表沒有已儲存關聯可複製，明確略過該尚未部署模組；已存在的表若權限/schema/複製失敗則 503，不吞錯。
- 產品實體刪除先以同一可信 parent `FOR UPDATE` 鎖保護，只有狀態 0/-1 分支檢查 RecipeGroup 與 Program 現存指派。仍有指派回 409，不自行 cascade 刪除來源未證實的子項；狀態 2 軟停用不增加這個限制。這是 YMES 明確的資料完整性規則：可移除的指派先移除，再實刪 parent。Program 的非 0 產品指派依其來源唯讀，若舊資料有此引用，需後續正式維護/生命週期流程，不能臨時放寬來繞過保護。optional 指派表不存在不阻擋既有產品刪除；存在但無權查詢則 503。
- 本輪只補現有 new-version 流程的關聯 hook，未完成一般產品 Copy/Detail Source 的通用 serializer，也沒有把使用者指定新版號接入現有自動加號 API。這兩項仍是既有產品主檔 gap，不能聲稱整個 Copy/Version 已符合原型。

### Schema 與驗證界線

- 依公司來源 reference 核對 `tblprdrecipe`、共用 `tblprdrecipebasis`；由家用端 root 查 live schema 決定 migration/必要授權。此批未新增 migration、未執行 DDL/DML，未另建 Recipe master；實際依賴即本文件上列來源兩表與既有產品主檔。
- 此段為準備階段契約；正式部署、四種產品狀態、approved/current lookup、重複/失效候選、Apply→Confirm、Version複製/rollback及parent引用保護的實際API／browser範圍以下節為準，未實測分支不推論已通過。

## 正式 API／瀏覽器實際驗收（2026-11-19）

- 已部署真正Recipe主表的已核准目前版候選、產品版次關聯GET/POST/DELETE；三欄複合鍵沿用來源，沒有捏造IssueState／DataStamp或Edit API。
- 正式API/DB全套10群組回歸包含：目前版／核准候選、保存時重驗、canonical／重複檢查、各產品狀態可維護、Version真實複製Recipe和Program及較後Program檢核失敗時整筆rollback、父列刪除guard與並發協調。11項regression精確範圍清理皆0。
- app-browser：選真實active/current Recipe `CODUI-PRD-1119-1413E0-R`，Apply成功再Confirm只留一筆，未重複POST；刪除確認Cancel保留一筆。將自有測試父列切state2後，正式UI DELETE仍可成功，重查顯示空集合，符合此子功能不限制產品IssueState的來源。
- Recipe子窗當時的父列state2由受控SQL fixture設定，用來驗子資源規則；另於後續父產品GUI，以真 `CODUI1119-PRDPS` default assignment完成Approve→2、EDIT disabled、DEL2→-1→硬刪，不把前者的SQL fixture冒充簽核UI。此仍僅是目前YMES核准路徑，完整MES Controlled/CPASS/IssueJob引擎未完成。
- 證據 `.tools/prd-recipe-browser.jpg`、`.tools/prd-recipe-active-delete.jpg`；父頁另有 `.tools/prd-multiselect-stale-browser.jpg`、`.tools/prd-approval-browser.jpg`、`.tools/prd-parent-clean-browser.jpg`。最新Web `29139f507e77`／artifact `CKNGXUoB`，保留原Recipe操作實證，不將父頁新測項擴稱Recipe全部失敗分支已驗。
- root已完成 `CODUI-PRD-1119-1413E0` A/B/C全版與自有catalog/PS最終清理，實際PPK十項計數products、recipes、programs、processAssignments、bom、programMasters、recipeMaster、processMaster、unit、type全0；原為補驗保留的UI組已不再待清理。不是全表業務資料計數。
- API後續通用錯誤處理小修為 `269434c2aaf0`、readiness200，Web不變；只新增MasterDataController的Npgsql乾淨503／三list包裝，未改RecipeGroup來源契約。新image已通過11項正常HTTPS唯讀/匿名401/小寫404/登出確認，不重述為全套Recipe回歸重跑。

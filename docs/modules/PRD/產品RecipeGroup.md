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

- `ProductRecipeGroupSetup.vue` 以 684×486 清單、392×150 Define 對齊 WinForms；DEV 有 approved/current 假 lookup、多列 Delete、Add/Apply/Confirm/Close，僅寫產品關聯 localStorage。Production API 未接線時唯讀。
- 建議 API 語意：GET 產品版本 RecipeGroup 指派；GET lookup 僅回已核准且目前版次；POST 新增一個 RecipeGroup；DELETE 必須帶單一 RecipeGroup。沒有 Edit endpoint。
- Legacy `IssueState` 不控制此頁寫入；是否要在新 API 增加簽核狀態限制需由業務另定，不可把 legacy 行為說成已有限制。
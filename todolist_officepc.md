## 家用端交付及公司端待補來源（2026-11-19）

- [x] QC101/QC102/QC401/QC402/PS101正式API及主要頁面操作已部署驗證，模組MD已補實作、版本與限制；公司端pull保留nullable版本條件與共用確認窗。QC101/QC102 Apply保持連續區間新增；QC401/QC402/PS101 Apply轉Edit，依各自來源／YMES明確決策保持一致。
- [ ] 公司端補交 MCLASS BASIS／SCLASS BASIS 原始Excel範本binary和SYS template metadata，勿以DEV CSV代替。家用端正式匯入API已通，瀏覽器選檔驗收仍列家用端待辦。
- [ ] 公司端補交 QC107 真實Action／ReasonType ValueList儲存值與標籤、對應來源schema/provider及SysInfo UserDefineAQL實際值；不要照舊註解硬編碼。QC108補交真實FUNQR函式catalog。來源空缺目前回明確503，不填入DEV函式名。
- [ ] 公司端補交 QC103 PlanType6/4參數輸入輸出ShowFlag、sampling核心/Population flags、FUNSA catalog與AQL/SYS ValueList metadata。已知獨立CRUD由家用端接續，不要求公司端連YMES PostgreSQL或SSH。
- [ ] 公司端確認QC101 legacy AdditionalXml helper數值/識別字查詢規則，以便比較目前已明確記錄的YMES literal contains搜尋契約。

## 家用端交付：QC503／PS102（2026-11-18）

- [x] 家用端已部署並完成正式 app-browser 驗收；QC503 共用 Driver API 與 PS102 製造層別 API 可供 UI 呼叫。模組文件已補實作、migration、按鈕、版本與驗證紀錄。
- [x] 核准／刪除共用站內確認窗；新增/複製 Apply 成功轉修改模式並使用 server DataStamp，避免重複新增。這是明確記錄的 YMES 互動改善。
- [ ] 公司端 pull 後保留以上 API/互動契約，繼續依本檔未完成 UI/Oracle DDL 交接；資料庫、API 與正式驗收仍由家用端負責。

## 2026-11-18 家用端整合補正：PRD Vue 型別

- [x] 修正 `ProductBasisMaster` 與 Parameter/Business Rule 子元件的結構型別；BOM lookup 使用共用 key/label/row；OPDescription 正規化 OPName/audit；Parameter draft 保存明確保留識別鍵並排除 recipeDetails；Property 在有效值驗證前初始化 draft。沒有新增 broad any cast，亦未新增正式 API/schema。
- [x] 本輪 `npx.cmd vue-tsc --noEmit` 與完整 `npm.cmd run build` 均通過；維護細節已補至 PRD 對應五份模組文件。
- [x] 本機 DEV app-browser PRD 冒煙檢查通過：產品主檔、Property 清單/新增視窗、Parameter 樹、Business Rule 圖、OPDescription 稽核欄位可載入；BOM 右鍵 Add→Product→lookup→選取 `DEV-PRD-1002` 及 Cancel/Close 正常。所有相關視窗可關閉，未出現 TypeError/ReferenceError/Unhandled 紀錄。
- [ ] 本輪不是完整 CRUD/正式 API 驗收；公司端 pull 後沿用既有正式唯讀/DEV 假資料邊界，後續正式 API/schema 與完整 CRUD 仍依模組文件 handoff 接續。

## 2026-11-18 公司端進度：PS102 製造層別設定

- [x] 閱讀 `frmFABPRSStageBasis`/Def Form 與 Designer、`wsPRS.Load/Add/Edit/Delete/ApproveFABPRSStageBasis`、`kcBasis_Query`、`kcPRS.clsProcess`、`kcBasis_UPD`；整理欄位、四狀態、Sequence 唯一、Apply/Confirm、刪除與 IssueManagement 流程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 Oracle 設定唯讀直連 MESTEST，查 `TBL_FABPRSSTAGEBASIS` 的 DBMS_METADATA DDL、欄位/default、constraints/indexes 和獨立 PK/Sequence index DDL。兩個唯一索引由 StageNo PK 與 Sequence unique constraint 建立；沒有 FK/額外 index。未讀業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。
- [x] 完成 `StageBasisMaster.vue`、PS102 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、PRS 三層選單路由與家用端 schema/API handoff。DEV 假資料限 `ymes.dev.ps102-stage-basis.v1`。
- [ ] 尚未連 YMES PostgreSQL，未實作 API/migration；未執行 build、test 或 browser 驗收。完成本次 PS102 commit/push 後，依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續下一個尚未完成 FunctionNo。

## 2026-11-18 公司端進度：PS101 生產區段資料

- [x] 直接閱讀 `frmProductionSegment`/Def Form 與 Designer、`wsPRS.Load/Add/Edit/Del/ApproveProductionSegment`、`clsProcess`、`clsBasis_UPD.SetProductionSegmentIssueState`、`wsSYS.LoadSerialRuleBasis`/Core；核對列表、四狀態、Copy/Apply/Confirm、刪除/簽核、Lot serial lookup 與隱藏欄位。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線直連 MESTEST，唯讀取得 `TBLPRSPRODUCTIONSEGMENT` 的 `DBMS_METADATA.GET_DDL`、欄位/default、constraint/index metadata，並另外取得 PK index DDL；未讀業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。僅 PK unique index，無 FK/其他 index。
- [x] 完成 `ProductionSegmentMaster.vue`、PS101 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、PRS 三層選單路由及家用端 API/schema handoff。PS104 改為重用 PS101 的 `tblprsproductionsegment`，避免重複建立。
- [x] DEV 假資料在專用 localStorage；有四狀態、假資料直接核准/送簽兩種分流、定義窗及 LOTNO 選擇視窗。無 MES/ YMES 寫入。
- [ ] 尚未連 YMES PostgreSQL，未實作 Production API/migration；未執行 build、test 或 browser 驗收。PS102 公司端 DEV 頁與 DDL/API handoff 已在本輪完成；接續依 inventory 排定下一個未完成 FunctionNo。

## 2026-11-18 公司端進度：PS103 流程類別資料

- [x] 直接閱讀 `frmProcessType`/Def、Designer、`wsPRS.Load/Add/Edit/Del/ApproveProcessType`、`clsProcess`、`clsBasis_UPD`，以及共用 `PS_ProcessProperty` 子功能；確認列表欄位、四狀態、Apply/Confirm、ProcessNo 衝突、子資料刪除、簽核 IssueNo 和 DataStamp。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護 Oracle 設定唯讀取得 `MESTEST.TBLPRSTYPE` DBMS_METADATA DDL、欄位/default、constraint/index；未讀業務資料、未執行 Oracle DDL/DML、未輸出連線秘密。來源僅有 PK unique index。
- [x] 新增 `ProcessTypeMaster.vue`、PS103 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、三層選單路由與家用端 schema/API handoff；Property 共用 API 和 PS104 ProcessType lookup 相依已註明。
- [ ] 尚未連 YMES PostgreSQL，未實作 Production API/migration；依規則未執行 build、test 或 browser 驗收。推送完成後依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續下一個未遷移功能。

## 2026-11-18 公司端進度：PS104 流程基本資料

- [x] 直接閱讀主/Def/Draw/Exclude OP WinForms、Property 子功能、Data Import Form、wsPRS 和 clsProcess，整理欄位、狀態、核准、版本、複製、刪除、流程圖及匯入行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線直連 MESTEST，只讀查九張表的 DBMS_METADATA DDL、欄位、constraints/indexes；未查業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照、PS104 規格和家用端 handoff。九張表只有 PK unique index，沒有 FK/額外索引。first-phase 三表 default 差異及新增表責任已依 PS103/PS101/PS104 分拆寫入交接。
- [x] 新增 Vue DEV 流程基本資料頁：狀態查詢、欄位 Grid、Add/Edit/Copy/Approve/Delete/Property/Version/Exclude OP、簡化節點/連線編輯及 Import 視窗。假資料存在獨立 localStorage。
- [ ] 流程圖 XML serializer、群組/分支拖曳編輯與舊版格式相容待確認；Excel checker 外部 DLL 未找到原始碼，DEV 只保留選檔視窗，不宣稱匯入完成。
- [ ] 未連 YMES PostgreSQL、未實作 API/migration；未執行 build/test/browser。
- [ ] 家用端完成 API/schema 並 push 後，公司端 pull、對接 Production API。PS103 與 PS101 的公司端 DEV 頁及交接已完成；PS102 公司端 DEV 頁與 DDL/API handoff 已於本輪完成；接續依 inventory 排定下一個未完成 FunctionNo。

## 2026-11-18 公司端進度：OPB02 ECN 作業站群組設定

- [x] 直接閱讀 `frmOP_B_ECNOPGroupBasis`/Def、Designer、`wsOP.Load/Add/Edit/Del/ApproveOPECNGroupBasis`、`clsOpBasis` 與 DataStamp update；確認主/明細雙 Grid、狀態篩選、Copy 僅複製主檔、OP lookup 限制、逐筆多選與簽核 Job 分流。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護 MESTEST 連線唯讀取得 `TBLOPECNGROUPBASIS`、`TBLOPECNGROUPDETAIL` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。主鍵分別是 `OPGROUPNO` 與單欄 `OPNO`；只有 PK unique index，沒有 FK/額外索引；未讀業務列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照、`docs/modules/OP/ECN作業站群組設定.md`；更新規格索引、資料庫連線紀錄及家用端 API/schema handoff。
- [x] 完成作業站資料 → 行業包 → ECN 作業站群組設定 Vue DEV 頁 `/YMES/OP/ECNOperationGroups`：四狀態篩選、主/明細 Grid、CRUD/Copy/核准/刪除、PS/OP 選擇、稽核欄、DataStamp 欄位及 KD 黃色輸入欄；假資料僅 DEV localStorage。
- [ ] 家用端尚未比對 YMES live schema、實作 Production API/migration；公司端未連 YMES PostgreSQL，未執行 build、test、browser。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 的下一個尚未完成 FunctionNo，不要重做已交接功能。

## 2026-11-18 公司端進度：OPB03 客戶站點對應設定

- [x] 直接閱讀 frmB_OPCustomerOPMapping、主檔/明細 Def Forms 與 Designer、wsOP、clsOpBasis、clsB_Basis_UPD；確認雙 Grid、lookup 狀態、Add/Edit/Copy/刪除、DataStamp、Audit 與批次刪除行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 TBL_BOPCUSTOPMAPBASIS、TBL_BOPCUSTOPMAPDETAIL DDL，並查 ALL_CONSTRAINTS/ALL_INDEXES；兩表只有 PK、沒有 FK/額外索引。未讀 MES 業務資料、未執行 Oracle DDL、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照與 docs/modules/OP/客戶站點對應設定.md，更新規格索引、Oracle 查詢紀錄與家用端 API/schema handoff。
- [x] 完成「作業站資料 → 行業包 → 客戶站點對應設定」Vue DEV 頁 /YMES/OP/CustomerOPMapping：上下雙 Grid、查詢、多選刪除、主檔 ADD/EDIT/DEL/Copy、明細 ADD/EDIT/DEL、Customer/PS/OP lookup、Audit 顯示與 KD 黃色欄位；假資料限 DEV localStorage。
- [ ] Oracle CustomerOPSeq NUMBER(3,0) 與舊 NumericUpDown 最大 999999 不一致，已於規格和家用端 handoff 標明；Production API 依 live schema 驗證，沒有擴欄或截斷。
- [ ] 未連 YMES PostgreSQL、未實作 Production API/migration，未執行 build、test、browser。OPB03 已交家用端 API/schema；公司端接續功能依 docs/MES_MENU_FUNCTION_INVENTORY.md，下一項 OPB02 ECN 作業站群組設定。
# 公司電腦續作清單

## 2026-11-18 公司端進度：OPB01 作業站參數群組設定

- [x] 直接閱讀 `frmOPGroupAttrib`/Designer、`frmOPGroupAttribDef`/Designer、`wsOP.Load/JoinElement/Save/DelOPGroupAttrib`、`clsOpBasis`、`clsB_Basis_UPD.SaveOPGroupAttrib` 與 `wsSYS.ChkElementValueValid`；確認三欄精確查詢、整組多列編輯、DataFlag、Copy、InputType、Sequence、BYSECONDCLASS 對應與 DataStamp。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 MESTEST 連線唯讀取得 `TBLOPGROUPATTRIB`、`TBLSYSELEMENTBASIS` 的 `DBMS_METADATA.GET_DDL`、ALL_TAB_COLUMNS/CONSTRAINTS/INDEXES/IND_COLUMNS；OP 主表僅複合 PK，無 FK/額外索引。未讀業務列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 OPB01 Oracle source DDL、PG CREATE-only 參考、功能規格及本段家用端 API/schema handoff。`TBLSYSELEMENTBASIS` 是既有共用表，本功能不重建。
- [x] 完成「作業站資料 → 行業包 → 作業站參數群組設定」Vue DEV 頁 `/YMES/OP/OperationGroupAttributes`：三條勾選精確查詢、Grid 多選刪除、Add/Edit/Copy，整組 Element 批次編輯、多選新增、InputType 下拉/Key In、AllowNull、Sequence、BYEQUIPMENT_TYPE、Apply/Confirm/Close、客戶/ECN 群組/Element lookup；localStorage 假資料限 DEV。
- [ ] 家用端依 `todolist_homepc.md` 核對 YMES live schema、接登入/權限及 lookup，實作 batch transaction、DataStamp CAS、InputAbortLog 和 SYS Element value validation API；完成後公司端 pull 對接。
- [ ] 未連 YMES PostgreSQL，未實作 Production API/migration，未執行 build、test、browser 或 CRUD 驗收。
- [ ] OPB01 完成此 UI/spec handoff 後，繼續從 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個未完成 Function；整套 MES 遷移尚未完成。

## 2026-11-18 公司端進度：QC408 OCAP基本資料

- [x] 直接閱讀 QC408 主/Def/流程繪製/節點 Form 與 Designer、`wsQC`、`clsQCBasis`，整理畫面、CRUD、核准、刪除、Copy、DataStamp、NodeType、群組 lookup、XML/節點/連線儲存規則。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀取得 `TBLQCOCAPBASIS`、`TBLQCOCAPNODEBASIS`、`TBLQCOCAPNODERELATION` 原始 DDL；確認 PK、欄位空值/預設值，只有 PK index、未見 FK 或額外 index。未讀 MES 業務資料、未執行 Oracle DDL、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 對照，並新增 `docs/modules/QC/OCAP基本資料.md`、索引、資料庫連線紀錄與家用端 API/schema handoff。
- [x] 完成 QC → 異常設定 → OCAP基本資料 Vue DEV 頁：四狀態清單、查詢、多選 CRUD/Copy/Approve/Delete、稽核列、流程預覽與簡化節點/連線繪製器，KD 黃色輸入欄；路徑 `/YMES/QC/OCAPBasis`。
- [ ] 流程編輯器是 AddFlow web 簡化版；舊 `iMESXMLFlow.dll` serializer 只有 binary，未完成 XML 格式相容與 nested group/Stage validation。Production API 前需由家用端決定相容策略。
- [ ] 未連 YMES PostgreSQL，未實作 API/migration，未執行 build、測試或瀏覽器驗收。家用端完成 API/schema 並 push 後，公司端 pull 再整合驗收。

## 2026-11-18 公司端進度：QC501 檢測儀器類別

- [x] Pull `origin/main`（Already up to date），直接閱讀 QC501 WinForms 主/Def Forms、Designer、`clsInspMachine` Load/Add/Edit/Delete/Driver 來源與共用 `ExeIssueControl`；不是依截圖臆測。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀取得 QC501–QC503 共用六表 Oracle DDL/PK index，產生 Oracle source DDL 與 PostgreSQL 空表 CREATE 參考；無業務列、未執行 Oracle DDL、無連線秘密輸出。
- [x] 建立 `/YMES/QC/InspectionMachineParameterType` Vue DEV 假資料頁、品質資料→檢測儀器→檢測儀器類別路由/選單、查詢狀態、WinForms 欄位與按鈕、RS232/UserDefine 編輯、Driver picker、複製、多選刪除/送簽、Audit 區及假資料操作。
- [x] 新增 `docs/modules/QC/檢測儀器類別.md`，更新模組索引、資料庫連線紀錄與 `todolist_homepc.md` schema/API handoff。
- [x] 家用端已部署共用六表 0027 migration 與 QC501 API；app-browser 已驗證新增/套用/修改、複製/Driver lookup、核准取消/確定、引用刪除 409、狀態 1 唯讀、-1/2 成功刪除、四狀態/Close、錯誤文字與 Demo 登入；fixture 已清空。公司端待家用端 GitHub 同步後 pull，以 server DTO/nullable DataStamp 對接；共用 Issue/Job、Issue Control 仍待整合。QC503 UI 已於公司端 `b572f3b` 提交，下一步為家用端 API handoff。

## 2026-11-18 公司端進度：QC502 檢測儀器基本資料

- [x] 直接閱讀 `frmInspMachineParameter`/Def、Designer、`wsQC`、`clsInspMachine` 的 Load/Add/Edit/Delete/Approve，追蹤類別 checkbox/lookup、類別參數複製、Driver lookup、RS232 必填與 IssueManagement Job 分流；非依截圖推測。
- [x] 確認 QC502 直接主表為 QC501 已取得 DDL 的 `TBLQCINSPMACHINEPARAMETER`；QC501 類別與 Driver 是 lookup。`TBLQCINSPMACHINEBASIS` 不在此 Form/Core 的 CRUD 寫入路徑；Property 按鈕 Designer 預設隱藏。本次無需重查 Oracle 或另建 CREATE SQL。
- [x] 新增 `/YMES/QC/InspectionMachine` Vue DEV 頁與品質資料→檢測儀器→檢測儀器基本資料路由；包含 IssueState/識別碼查詢、主 Grid、Add/Edit/Copy/Approve/Delete、類別帶入/本機 RS232/UserDefine Driver 表單、lookup modal、audit 與獨立假資料。
- [x] 新增 `docs/modules/QC/檢測儀器基本資料.md`，更新 module index、資料庫紀錄與家用端共用 API/schema handoff。
- [x] 家用端已部署 QC502 API，app-browser 已驗證新增/套用/修改、RS232→UserDefine 類別更換、複製/Driver lookup、核准、取消/確定刪除、nullable DataStamp 衝突與重查保存、四狀態/Close、錯誤文字及 Demo 登入；fixture 已清空。類別參數由 API 鎖定重讀，Apply 使用 server DTO。公司端待家用端 GitHub 同步後 pull 並確認 DTO/API 對接；QC503「檢測設備連線程式」UI 已於 `b572f3b` 提交，家用端接續 API。

## 2026-11-18 公司端進度：QC503 檢測設備連線程式

- [x] 直接閱讀 `frmInspDriver`/Def、Designer、`wsQC.Load/Add/Edit/Del/ApproveInspDriver`、`clsInspMachine`、`clsBasis_UPD` 與 `udDriverFunction.clsDriverFunction`；確認 Driver CRUD 的狀態/DataStamp、Issue Job 簽核、核准/刪除行為及三個可選函式名稱。
- [x] 新增 `/YMES/QC/InspectionDrivers` Vue DEV 頁及品質資料→檢測儀器→檢測設備連線程式路由。完成四狀態/DriverNo 查詢、Grid、Add/Edit/Copy/Delete/Approve、FunctionName 下拉、Description、Audit、多選與 localStorage 假資料。
- [x] 新增 `docs/modules/QC/檢測設備連線程式.md`、索引與資料庫紀錄，並更新 `todolist_homepc.md` API/schema handoff。主表 `TBLQCINSPDRIVER` 的 Oracle DDL/PG CREATE 已含在 QC501 共用六表 reference，本功能不需再查 Oracle或另建 SQL。
- [ ] 公司端未連 YMES PostgreSQL、未實作 API/migration，未執行 build、測試或瀏覽器驗收；家用端完成 API/schema 後再 pull 整合。接續功能依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個尚未完成項目。

> 目的：記錄家用電腦完成資料庫/API交接後，公司電腦接著要做的 WinForms UI、互動邏輯與規格文件。公司電腦先讀本檔；家用電腦先讀 `todolist_homepc.md`。

> **公司電腦工作邊界：** MES 原生 Oracle MESTEST 僅供本端唯讀查詢 DDL、資料字典與舊系統規則；不得在原生 Oracle 建 YMES 表或寫入業務資料。本端只開發 UI、按鈕互動與呼叫 `/YMES/API`；不連 YMES PostgreSQL、不使用家用端 PPK/SSH、不執行 migration、不遠端部署。資料庫或 API 需求寫入規格並交給家用電腦。

## 交接基準

- 專案：`D:\YMes`，GitHub `souaix/YMES`；來源 WinForms：`D:\iMES\CSiMES`。
- 公司電腦以 MESTEST 假資料完成 Vue UI 與前端互動；家用電腦負責 YMES PostgreSQL、migration、API 與部署。
- 開工前先 pull 最新 `main`，確認家用電腦已 push API/schema 變更，再對照本檔與 `todolist_homepc.md`。不要覆蓋另一台電腦尚未同步的修改。
- Oracle 來源 DDL 用 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢；使用本機受保護連線設定，不必請使用者重貼，也不得把連線值寫進文件、SQL 或 Git。
- 目前只查詢過 Oracle 結構與系統選單中繼資料，沒有匯出 MES 業務資料。不要把 DEV 假資料當成正式資料。

## 最新公司端工作：QC305 產品規格設定（2026-11-17）

- [x] 直接對照 `frmQCProductSpec` Designer/事件、`wsPRD.LoadProductType`、`wsQC.LoadProductBasisJoinPRDQC`、`LoadPRDQCItemJoinForVariables`、`LoadPRDQCItemJoinQCChart`、`UpdPRDQCChartData` 與 `clsQCBasis.cs`；記錄 ProductType/ProductNo 查詢、`QC_Count=1`、SPEC 載入、GroupNo、FactorSpec=PRODUCT、SPCID、MEMO、批次套用及舊 ProductNo-only 行為。
- [x] 新增 `/YMES/QC/ProductSpec` Vue DEV 頁及 `PRDQCITEM/QC305` 選單/路由；支援假資料查詢、SPEC 編輯、群組關聯識別、Chart filter、CS±Tolerance、剪貼簿貼上、規格驗證及批次假資料套用。
- [x] 建立 `docs/modules/QC/產品規格設定.md`，更新規格索引、資料庫連線/DDL provenance、家用端 API handoff。此功能沿用 QC302 Chart Basis/Detail/DetailLog 與既有產品/QC Item 表，沒有專屬 CREATE SQL；TBLPRDTYPE DDL 尚未保存，僅當家用端確認 YMES lookup 依賴缺漏才需由本機 Oracle 直連查詢。
- [ ] 本回合未執行 build、test 或瀏覽器驗收。家用端完成 API/schema 後 pull，對照有/無既有 Basis、產品群組綁定、批次部分略過及 ProductVersion/OPNo 歧義。
- [ ] 完成後依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續下一個尚未遷移的 WinForms 功能；維持 office fake UI/spec 與 home API/schema handoff 分工。

## 最新公司端工作：QC304 物料規格設定（2026-11-17）

- [x] 直接閱讀 `frmQCMaterialSpec` Designer/事件、wsQC、clsQCBasis；記錄 MaterialType/MaterialNo 查詢、Active 物料清單、SelectAll/UnSelectAll、SPEC 載入、QCItem FactorSpec 條件、ChartType/SPCID、CS/Tolerance、Paste、MEMO、完整數值/界限驗證與多物料套用。
- [x] 確認此功能維護 QC302 的 Chart Basis/Detail/DetailLog 及既有 Material/QC Item/Group tables，沒有 QC304 專屬新表。Oracle/PG DDL 沿用 QC302、MTL、QC201/QC205/QC212 references，未重查 Oracle 業務資料，也沒有新增 CREATE SQL。
- [x] 新增 `/YMES/QC/MaterialSpec` Vue DEV 頁與 QC304 三層選單/路由；完成查詢/選取假資料、點 SPEC 載入、圖表 filter、X-Rm mapping、CS±Tolerance、剪貼簿/文字框貼上、數值與界限驗證、MEMO、批次假資料保存。
- [x] 建立 QC304 功能規格、更新規格索引與 Oracle DDL 查詢 provenance，並加入家用端 API/schema handoff。
- [ ] 公司端未執行 build、test 或瀏覽器驗收。家用端完成 schema/API 後，pull 並對照不同 Material/QCItem 的 Chart Basis/Detail/DetailLog 與 group item 跳過結果。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接對照下一項尚未遷移的 WinForms Form/Service/Core。

## 前一項公司端工作：QC303 品管項目規格（2026-11-17）

- [x] 直接對照 `frmQCSpecBasis`、`frmQCSpecBasisDef`、`frmQCSpecCopy`、wsQC 與 clsQCBasis；記錄 QC Item、FactorSpec、四狀態、Detail SPCID、規格界限、Above helper、MEMO、簽核、刪除與跨 QC Item 複製行為。
- [x] 沿用先前已保存的 Oracle 直連唯讀 DDL `mestest_qc205_variable_dependencies_source_ddl.sql` 中 `TBLQCSPECBASIS`/`TBLQCSPECDETAIL`；本次沒有讀業務資料列，也沒有在 Oracle 執行 DDL。
- [x] 新增 `/YMES/QC/SpecBasis` Vue DEV 頁、品質資料→計量規格設定→品管項目規格選單/路由、QC303 規格文件與 PostgreSQL CREATE-only JSONB 對照。
- [x] 更新規格索引、Oracle DDL provenance 記錄、家用端 schema/API/Issue Job handoff。
- [ ] 公司端尚未執行 build、test 或瀏覽器驗收；禁止因此宣稱 Production API 已完成。家用端完成 schema/API 並 push 後，公司端 pull 對接、依 API 核對 QC Item/Factor lookup 及簽核狀態。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接看下一項尚未遷移 WinForms Form/Service/Core；不以截圖替代原始碼調查。

## 前一項公司端工作：QC301 管制圖內容（2026-11-17）

- [x] 直接閱讀 `frmQCChartBasis`、`frmQCChartBasisDef`、`frmQCChartCopy`、Rule Define、wsQC 與 clsQCBasis；整理 QC Item、狀態篩選、動態 Factor 欄、Detail/Rule、簽核、刪除、跨 QC Item 複製與 sampling parameter 流程。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀取得 `TBLSPCQCCHARTSAMPLANPARAMETER` DDL/index。Chart Basis/Detail/DetailLog/Rule DDL 沿用 QC302 reference；沒有讀業務資料列或在 Oracle 執行 DDL。
- [x] 建立 `/YMES/QC/ChartBasis` Vue DEV 頁，對齊三層選單、QC Item/Chart Grid、四種狀態、Detail/Rule tabs、Define/Cross Copy/Rule modal、表單驗證、假資料 CRUD/簽核/刪除與稽核。
- [x] 新增 `docs/modules/QC/管制圖內容.md`、Oracle/PG sampling parameter reference，更新資料庫查詢紀錄、規格索引與家用 handoff。
- [ ] 家用端依 `todolist_homepc.md` 先查 PostgreSQL live schema/migrations，再完成 QC301 API/schema/Issue Job；Production API 完成後公司端 pull 對接與後續 UI 調整。
- [ ] 公司端本段未執行 build、test 或瀏覽器驗收；接續沿 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接查看下一項 WinForms Form/Service/Core。

## 目前停留點（2026-11-17）

### QC302 匯入管制圖內容（公司端 2026-11-17）

- [x] 閱讀 `frmImport_QCChartBasis` Form/Designer、`wsQC.ImportQCChartBasis`、`clsQCBasis.ImportQCChartBasis`、Get/Add/Edit ChartBasis 與 Upd Chart Rule；整理格式化選項、第一工作表/第 2 列跳過、全列 Save、SPCID 0–4 圖表界限、每列 Execute、FactorSerial、重複圖表更新與明細歷程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 和本機受保護設定唯讀查四張實際寫入的 SPC QC Chart Oracle table/index DDL；沒有讀業務列或在 Oracle 執行 DDL。建立 `mestest_qc302_spc_qc_chart_source_ddl.sql` 及 PostgreSQL schema-only CREATE 參考 `mestest_qc302_spc_qc_chart_create.sql`；Factor 動態欄位以 JSONB handoff。
- [x] 新增 `/YMES/QC/ChartDataImport` Vue DEV 頁、品質資料→計量規格設定三層選單、格式化欄位、動態 Grid 編輯/刪列、假資料逐列 Save/結果回報。
- [x] 新增 `docs/modules/QC/匯入管制圖內容.md`，記錄程式來源、欄位與 Core 驗證/更新行為、API 契約及 Oracle/PG DDL；更新規格索引、DB 查詢紀錄與家用 TODO。並將 QC214 先前的 Oracle Chart table 拼字疑義修正為已確認名稱，仍要求家用端核對 YMES live schema。
- [x] 家用端已部署 QC302 migration `0024_qc302_chart_data_import.sql` 與登入保護 API：Preview `.xls/.xlsx`、全列 Import、JSONB 因子、逐列交易/結果、圖表 basis/五種 detail/history/rule 寫入；API health=200。角色/FunctionKey ACL 依使用者決定暫緩。
- [ ] 公司端待家用端提交/推送後 pull 最新 `main`，再用正式頁面確認 API Preview/Import 回應與資料欄位映射。匯入目前會因 YMES 尚缺共用 `TBLQCITEMRULE`、`TBLQCRULEBASIS` 而拒絕新圖，傳入 PlanNo 且缺 `TBLQCSAMPLINGPLAN` 亦拒絕；不可把缺表視為成功。
- [ ] 正式範本 binary/storage 尚未移轉，`GET /template` 暫回 503；待取得 MES `QCChart_Template.xls/.xlsx` 與模板儲存規則後再驗收下載。app-browser 本回合導回登入頁，需在登入狀態下完成 QC302 操作驗收。
- [ ] API 部署後公司端可接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接讀取下一個未遷移功能的 WinForms Form/Service/Core。

### 前一項：QC215 計數品管項目匯入（公司端 2026-11-17）

- [x] 直接閱讀 `frmImport_QCItem_Attributes` Form/Designer、`wsQC.ImportQCItem_Attributes`、`clsQCBasis.ImportQCItem_Attributes` 與 `DelQCItemForAttributes`；整理 1008×591 Grid/按鈕、Excel 欄位解析、第 2 列跳過、勾選列 Check/Save、逐列錯誤、計數 QC Item 子資料重建、抽樣計畫/Rule/Factor/OP/Reason 驗證與外部引用規則。
- [x] 確認 QC215 沒有專用資料表，不新增 CREATE SQL。沿用已保存 QC201/QC205/QC213 Oracle DDL 與 PostgreSQL CREATE 參考；本次未重連 Oracle、未讀業務列、未執行 Oracle DDL。
- [x] 新增 `/YMES/QC/AttributeItemImport` Vue 3 DEV 頁與 QC215 三層選單；保留 WinForms File/Check Data/Save/Template DownLoad/Select All/UnSelect All/MultiSelect/Close、勾選列處理、逐列結果樣式與假資料操作。API 漏回列級結果時前端顯示失敗。
- [x] 新增 `docs/modules/QC/計數品管項目匯入.md`，記錄來源程式、欄位、驗證、覆寫副作用、API 契約與資料表參照；更新規格索引、資料庫查詢紀錄及 `todolist_homepc.md`。
- [ ] QC215 Production API/schema 由家用端依 `todolist_homepc.md` 實作；無新表/CREATE SQL。API 需在每列交易內重驗引用、逐列回報、要求 QC215 FunctionKey、由伺服器寫 audit，並禁止 Factor 動態 DDL。
- [ ] 本段未執行 build、test 或瀏覽器驗收。接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接讀未遷移功能的 WinForms Form/Service/Core，完成 UI、規格與 API/schema handoff。

### 前一項：QC214 計量品管項目匯入（公司端 2026-11-17）

- [x] 直接閱讀 `frmImport_QCItem_Variables` Form/Designer、`wsQC.ImportQCItem_Variables`、`clsQCBasis.ImportQCItem_Variables` 與 `DelQCItemForVariables`；整理 1008×591 Grid/按鈕、Excel 欄位解析、第 2 列跳過、勾選列 Check/Save、逐列錯誤、QC205 子資料重建、抽樣計畫/Factor/OP 驗證和外部引用阻擋。
- [x] 確認 QC214 無專用資料表，不新增 CREATE SQL。沿用已查好的 QC205 主檔與 QC205/QC210/QC201 dependency Oracle DDL/PG CREATE 參考；本次未重連 Oracle、未讀業務列、未執行 Oracle DDL。
- [x] 新增 `/YMES/QC/VariableItemImport` Vue 3 DEV 畫面和 QC214 三層選單；重現 File/Check Data/Save/Template DownLoad/Select All/UnSelect All/MultiSelect/Close、唯讀欄位、勾選匯入、逐列粉紅失敗/灰色成功。DEV sample 儲存在獨立 localStorage，成功操作僅改假資料。
- [x] 新增 `docs/modules/QC/計量品管項目匯入.md`，記錄來源程式、解析欄位、驗證條件、覆寫副作用、API 契約和引用表；更新功能索引、DB 查詢紀錄及 `todolist_homepc.md`。
- [ ] QC214 production API/schema 待家用端依 `todolist_homepc.md` 實作；沒有 QC214 新表，沿用既有 QC205 tables。API 需逐列交易/結果、FunctionKey QC214、服務端 audit、參數化 SQL，並禁止 Factor 動態 DDL。
- [ ] 本段未執行 build、test 或瀏覽器驗收；接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 和未完成頁面直接讀 WinForms Form/Service/Core，再完成 UI/spec/API handoff。

### QC218 計數品管項目產品原因對應設定（公司端 2026-11-17）

- [x] 閱讀 `frmItemPRDReasonMap`、`frmItemPRDReasonDef`、Import Form/Designer、`QCItemRPDReasonSOPFileImporter`、`wsQC`、`clsQCBasis`；記錄 QCItemNo 核准查詢、ProductNo 主從群組、ReasonType/Level 雙清單、整組替換、全域 `*`、範本與分列部分成功匯入。
- [x] PowerShell 5.1 + Oracle.ManagedDataAccess 使用本機受保護唯讀連線取得兩張 mapping 表與三張共用 lookup 表 DBMS_METADATA DDL；兩張 mapping 只有 PK unique index、沒有 FK/其他 index，未查業務資料列、未在 Oracle 執行 DDL。
- [x] 新增 Oracle source DDL 五份與只建立兩張 mapping 空結構的 PostgreSQL CREATE-only 參考 `database/reference/mestest_qc218_item_product_reason_map_create.sql`；三張共用 QC Item/Reason/Product 表不可重建。
- [x] 新增 `web/src/components/QcItemProductReasonMap.vue`、FunctionNo=`QCITEM PRD REASON MAP` / QC218 fallback 三層選單與 `/YMES/QC/ItemProductReasonMap`。DEV localStorage 提供假 QC Item/Reason/Product、ProductNo 分組、產品篩選、`*` 全域、ReasonMap 左右移動/整組儲存及 CSV 部分成功匯入。
- [x] 新增 `docs/modules/QC/計數品管項目產品原因對應設定.md`、規格索引及家用端 API/schema handoff。規格記錄 legacy 全域 `*` 編輯驗證疑點、Importer 重複查詢錯誤處理及略過最後 Excel row 的瑕疵，並要求 API 修正。
- [ ] 未執行 build/test/瀏覽器驗收。DEV 預覽只接受 CSV；Production 需由 API 支援 WinForms `.xlsx/.xls` 第一工作表，家用端 schema/API 待依 `todolist_homepc.md` 實作。
- [ ] 此功能完成；公司端接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個未遷移 Function，直接讀 Form/Designer、Service/Core 與表結構，再建立 UI/spec/API handoff。

### QC217 設備品檢管理設定（公司端 2026-11-17）

- [x] 直接對照 `frmQCInspection`/Designer、`frmQCInspectionDef`/Designer、`wsQC` 和 `clsQCBasis`，整理兩個 Grid、查詢條件、Add/Edit/Delete/Copy/Approve、九種檢查週期、範圍選取、四類 QCItem、Mail Group、Issue Job 和 DataStamp 行為。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線唯讀取得 `TBLQCINSPECTIONBASIS`、`TBLQCINSPECTIONDETAIL` Oracle table/index DDL；確認兩張表只有 PK unique index、無 FK/其他索引，未查業務列或執行 Oracle DDL。
- [x] 新增 `/YMES/QC/Inspection` Vue DEV 頁、QC 三層選單及獨立 localStorage 假資料；保留 EQP「設備點檢表設定」獨立路由。新增 QC217 規格、Oracle DDL、PG CREATE-only 參考及家用端 API/SQL handoff。
- [x] 記錄舊程式 Delete 會依 OP/設備/檢查類型清除共用明細；Issue Job callback SQL 將 InspWeek 誤寫 InspType，家用端 API 必須用完整正確複合鍵修正。
- [ ] 家用端已完成 QC217 schema/API 與 Production 部署；正式 `/YMES/QC/Inspection` 的選單路由、Options/空清單查詢及新增表單已由家用端驗證。公司端尚未執行 build/test/CRUD 驗收；家用端 commit push 後請先 pull 並對接 Production API，待共用資料來源補齊後再驗證寫入流程。QC218 UI/spec/DDL handoff 已於上節完成；公司端接續按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選擇下一個尚未遷移功能。

### QC216 計量品管項目群組_減項（公司端 2026-11-17）

- [x] 直接閱讀 `frmPMI_QCItemGroup`、Define/Include/Deduction Define Forms、wsQC、clsQCBasis，整理三區 WinForms 版面、四狀態、主檔 CRUD、減項明細排序、扣除基準、Parameter/Op、簽核與硬刪引用清理。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查六張 MESTEST 表的 DBMS_METADATA、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES；群組主/明細/扣除基準及三張 SPC map 均只有來源 PK、無 FK/額外索引。未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 新增六張 Oracle source DDL、PMI 主/明細/扣除基準及三張 hard-delete 關聯表的 PostgreSQL schema-only CREATE 參考。Oracle 動態 Factor 欄位以 `factorvalues JSONB` 作為 YMES 建議，保留 `*` wildcard；家用端需白名單化已啟用 FactorOnline key。
- [x] 新增 `/YMES/QC/PmiItemGroup` Vue 3 DEV 頁與 QC 三層選單/路由，含主檔 CRUD/Copy/Delete/Approve、Detail 項目/排序/DeductionItem、扣除基準 CRUD/Copy、參數及作業站入口；使用獨立 localStorage 假資料。
- [x] 新增 QC216 功能規格、索引列、Oracle metadata 查詢紀錄及家用端 schema/API TODO。
- [x] 對照共用 `QC_ItemOp` Form/Service/Core，補上可選/已選雙清單、InLine/OffLine 批次加入、移除及 FAI/IPQC/PQC 驗證；非 ParameterType=2 的動態因子欄位改為來源選項清單。
- [ ] 尚未執行 build、test 或瀏覽器驗收；Production API/schema 由家用端依 `todolist_homepc.md` 接手，完成後公司端 pull 並對接。
- [ ] 公司端繼續按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 逐項讀 WinForms Form/Service/Core，完成 UI 和規格後再交 API/CREATE SQL，不以圖片替代程式碼調查。

### QC211 二次檢驗抽樣計畫（公司端 2026-11-17）

- [x] 直接對照 frmSamplingFor2ndInsp / Define 的 WinForms、Designer、wsQC、clsQCBasis；記錄主 Grid、查找條件、Add/Edit/Delete/Copy、一般/Wafer plan lookup 與必填規則。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查 MESTEST.TBLQCSAMPLINGFOR2NDINSP 的欄位、constraints 與 indexes；取得五個 NOT NULL、唯一索引鍵序，確認沒有 PK/FK；未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立 /YMES/QC/SecondInspectionSampling Vue 3 DEV 頁、QC 三層選單/route、功能規格、Oracle source DDL、PostgreSQL Create SQL 參考與家用 API/schema handoff。
- [x] DEV 頁支援篩選、選取、多筆刪除、Add/Edit/Copy、operation/QC item/plan picker、Apply/Confirm、唯一鍵檢查及獨立 localStorage 假資料；色系使用 KD 黃橘色。
- [x] 更新模組索引、DB metadata 查詢紀錄與兩台電腦 TODO。來源的 Edit Service/Core DataStamp/稽核參數不一致，已列為家用端 API 決策項。
- [ ] 未執行 build、測試或瀏覽器驗收；家用端需先核對 live schema/migrations 並完成 QC211 API，之後公司端 pull 驗收。
- [x] 下一個功能 QC212 計量品管項目群組已完成公司端 UI/spec handoff；接續依 MES_MENU_FUNCTION_INVENTORY.md 處理 QC213 計數品管項目群組。

### QC201 計數品管項目（公司端 2026-11-17）

- [x] 直接閱讀 `frmQCItemForAttributes`、`frmQCItemForAttributesDef`、`frmQCItemReason` 及共用 `QC_ItemParameter`、`QC_ItemFactor`、`QC_ItemOp`、`QC_SamplePlanChange`、`QC_SamplingParameterSimulation` WinForms 原始碼，核對主畫面、雙清單、狀態/簽核/刪除限制、欄位與按鈕事件；資料表行為依 wsQC/clsQCBasis 追查。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線直連 MESTEST 唯讀查主表、四張直接子表及刪除流程關聯物件的 DDL、欄位 nullable/default、PK/index/constraint；沒有讀 MES 業務列、沒有在 Oracle 執行 DDL，連線秘密未輸出。
- [x] 建立 Oracle source DDL `database/reference/mestest_qc_item_attributes_source_ddl.sql` 與四張直接維護子表的 PostgreSQL CREATE 參考 `mestest_qc_item_attributes_create.sql`；主表 CREATE 沿用 MTL 共用 SQL，其他刪除關聯表不整批重建。
- [x] 新增 `/YMES/QC/AttributeQcItem` Vue DEV 頁、QC 品質資料三層選單與 `QCITEM_A` 路由；主 Grid、四狀態、Add/Edit/Copy/Apply/Confirm、刪除/核准、Parameter/Factor/Reason/SamplePlanChange 及 Simulation 畫面可用 localStorage 假資料互動，欄位採 KD 淡黃色系。
- [x] 續讀共用 `QC_ItemOp` Form/Service 行為並補 Vue 左右雙清單、InLine/OffLine、ExecuteOrder、FAI/IPQC/PQC；僅已簽核可修改，LotIsValid=0 禁止新增 InLine，InLine 確認前檢查至少一種檢驗時機。
- [x] 新增 QC201 規格、規格索引列、Oracle 查詢紀錄及家用端 schema/API handoff；已將 SPK/ZKT/KYT 顯示差異與正式 ValueList 來源列入 API options 契約。
- [ ] Plan/Rule/MClass 查找鈕仍是簡化選取器；Production API 選項由家用端提供。未執行 build、測試或瀏覽器驗收。
- [ ] 未執行 build、測試或瀏覽器驗收。Production schema/API、權限、Issue Job、引用檢查由家用端依 `todolist_homepc.md` 接手；完成後公司端 pull 並對接驗收。
- [ ] QC201 UI 完成後，依功能目錄選下一個尚未完成 Function；不可把功能佔位頁計為完成。

### QC112 品質原因產品指導書（公司端 2026-11-17）

- [x] 對照 `frmReasonPrdSOPFile`、`frmReasonPrdSOPFileMaintain`、`frmReasonPrdSOPFileImport` 及 Designer；記錄已核准原因載入、選列即載入關聯、產品查找條件、左右移動、檔案上傳/移除、範本下載及逐列匯入進度。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護連線唯讀取得 `TBLQCREASONPRDSOPFILE` DDL/欄位/PK/index/FK 字典；僅 PK `(REASONNO,PRODUCTNO)`，無 FK/DataStamp/額外索引；未讀業務列或在 Oracle 執行 DDL。
- [x] 建立 `/YMES/QC/ReasonProductSOPFile` Vue DEV 頁與 QC 三層選單/路由，支援原因/關聯雙 Grid、Maintain、Import、localStorage 假資料及 API 呼叫契約；新增 QC112 規格、Oracle source DDL、PG CREATE 參考及本段 handoff。
- [x] 舊 `DataImporter.QCItemRPDReasonSOPFileImporter` 實作不在來源樹，已把 checker 及共用檔案刪除語義列為家用端查證項。Web 匯入改為選取工作簿與附件，再按 FileName 配對，不允許用戶端路徑被 API 當伺服器路徑讀取。
- [ ] 家用端已建立 QC112 PostgreSQL schema/API 並部署；請 pull 最新 main，以已登入正式環境開啟 `/YMES/QC/ReasonProductSOPFile`，核對原因/產品清單、附件新增/移除、XLS/XLSX/CSV 匯入、範本下載及錯誤顯示。Production API 使用登入保護；目前不含 FunctionKey 專屬 ACL（專案角色權限暫不處理）。
- [ ] 舊 `QCItemRPDReasonSOPFileImporter` 不在來源樹；若從 MES 原生程式確認到目前 API 未涵蓋的 checker/business 規則，請提供具體規則給家用端補齊。API 不會把工作簿 FilePath 當伺服器路徑使用。
- [ ] 下一個功能依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 排序，對照 WinForms Form/Designer/Service/Core 後繼續。

### QC110 特殊主檔維護（公司端 2026-11-17）

- [x] 對照 `frmSpecialBasisMaintain`/Designer、`wsSYS.LoadMesParameter/LoadMesParameterRef/LoadTablesConstraints`、`wsQC.UpdFactorMaintain` 與共用 Core；確認 ParameterClass=0、選列自動載入、動態 Grid、80px 按鈕欄、欄位名稱驗證、大小寫、必填黃色格、Import/Paste、整表 Save 與放棄變更提示。
- [x] 新增 `/YMES/QC/SpecialBasisMaintain` Vue DEV 頁、QC 基本資料三層選單、QC110 規格與獨立 `special-basis-maintain` API handoff。DEV 假資料與 QC109 隔離；正式資料仍由 API metadata 驅動。
- [x] QC110 與 QC109 共用既有 `TBLSYSMESPARAMETER`/`FC_%` 結構，沿用已查 Oracle source DDL 與 PostgreSQL CREATE 參考；本次不需新 DDL，也未查詢 MES 業務列。
- [ ] 未執行 build/test/瀏覽器驗收。家用端需比對 YMES live schema 並實作 FunctionKey QC110 的 class=0 參數清單、資料載入、欄位驗證、Excel preview 與交易式整表保存；完成後公司端 pull 並驗收 Production API。
- [ ] 下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 核對尚未完整遷移的下一個 Function，直接對照 WinForms Form/Service/Core 再繼續。

### QC109 要因資料維護（公司端 2026-11-17）

- [x] 對照 `frmQCFactorMaintain`、`wsSYS.LoadMesParameter/LoadMesParameterRef/LoadTablesConstraints`、`wsQC.UpdFactorMaintain`、`clsSYSMesParameter`、`clsQCBasis.UpdFactorMaintain_ArrayBinding`，確認參數篩選、動態 Grid、nullable 欄位底色、新增/編輯/刪除暫存、整表 Save、CSV/XLS Import、Clipboard Paste、離開提示與狀態列。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess、本機受保護連線唯讀取得 `TBLSYSMESPARAMETER` 與本次 `TableName LIKE 'FC_%'` 的三張來源表 `FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` 的 DBMS_METADATA table/index DDL、ALL_INDEXES、ALL_CONSTRAINTS；未讀業務列，未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/FactorMaintain` Vue DEV 頁、QC 基本資料選單/路由、QC109 規格及家用端 API/schema handoff。DEV 僅使用三張表的 DEMO schema/假資料，正式環境由 API metadata 驅動。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端先核對 YMES `TBLSYSMESPARAMETER`/`FC_*` 表，再依 QC109 handoff 建立必要 migration 與 FunctionKey QC109 的安全動態資料 API。QC110 已完成公司端 UI/規格，待家用端完成 API/schema 後再對接。

### QC108 計量品管法則基本資料（公司端 2026-11-17）

- [x] 依 `frmQCRuleBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照主 Grid、四狀態、RuleType、七種 RuleOption、RuleNo/RuleSerial、DataStamp、刪除/核准與 Issue Job。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀取得 `MESTEST.TBLQCRULEBASIS` table/index DDL 與 ALL_CONSTRAINTS；RuleSerial 為唯一 PK，僅 PK unique index，無 FK/額外索引；未讀業務列、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/VariableRuleBasis` Vue DEV 頁、選單/路由、QC108 規格及家用端 API/schema handoff。RuleOption UI/Oracle 代碼映射、舊 Other=9、FunctionName 目錄來源均已記錄。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端核對 YMES schema/migration 並實作 API/ValueList/FunctionName options 與 Issue Job。QC110 公司端頁面與規格已完成。

### QC107 計數判別法則基本資料（公司端 2026-11-17）

- [x] 依 `frmQCAttRuleBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照主/明細雙 Grid、RuleNo LIKE、四狀態、ComponentCollect、Action/ReasonType/ReasonLevel、三種 RuleType、複製、刪除及 Issue Job 簽核行為。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀查 `TBLQCATTRULEBASIS`、`TBLQCATTRULEDETAIL`、共用查找 `TBLQCAQL` 的 table/index DDL 及 ALL_CONSTRAINTS；已確認複合 PK、沒有外鍵/其他索引、沒有 DataStamp；未讀業務列或在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PG CREATE 參考、`/YMES/QC/AttributeRuleBasis` Vue 主從 DEV 頁、選單/路由、QC107 規格及家用端 API/schema handoff。自訂 AQL 依 `SysInfo/UserDefineAQL`，選項來自 TBLQCAQL。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端核對 YMES 共用 TBLQCAQL/既有 QC104–QC106 API 與 schema，依待辦開發；QC108 已完成，公司端下一項接續 QC110「特殊主檔維護」。

### QC106 品質原因基本資料（公司端 2026-11-17）

- [x] 依 WinForms `frmReasonBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照清單、CRUD、複製、ReasonType ValueList、QC105 子類別查找、Issue Control、Issue Job 簽核與 DataStamp 規則。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線唯讀取得 `MESTEST.TBLQCREASONBASIS` table/index DDL 與 ALL_CONSTRAINTS；ReasonNo 為 PK，僅 PK unique index，無 FK/額外索引；未讀業務列、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/ReasonBasis` Vue DEV 頁、QC 基本資料選單/路由、QC106 規格及家用端 API/schema handoff。ReasonType 正式選項須由 MES ValueList API 提供；子類別查找只顯示同類別且 IssueState=2。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端依待辦核對 YMES live schema 並實作 API。QC107 已完成；公司端下一項接續 QC110「特殊主檔維護」。
### QC105 品質原因子類別資料（公司端 2026-11-17）

- [x] 依 WinForms `frmReasonSubTypeBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照查詢、欄位、ValueList、CRUD、Copy、Issue Control、刪除及 Issue Job 簽核規則。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查 `MESTEST.TBLQCREASONSUBTYPEBASIS` table/index DDL 與 ALL_CONSTRAINTS；ReasonSubType 為 PK，只有 PK unique index，沒有 FK/額外索引；未讀業務資料、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL 與 PostgreSQL CREATE 參考；完成 `/YMES/QC/ReasonSubTypeBasis` Vue DEV 頁、QC 基本資料選單/路由、QC105 規格、DB 查詢來源及家用端 API/schema handoff。ReasonType 的 DEV 選項有標示 DEMO，正式環境需由 ValueList API 供應。
- [ ] 未執行 build/test/瀏覽器驗收；家用端比對 live schema 並完成 API；QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。

### QC104 Wafer 抽樣計劃（公司端 2026-11-17）

- [x] 對照 `frmSamplingPlan_Wafer`/Def、wsQC、`clsQCBasis`/`clsBasis_UPD`：四狀態、PlanNo 查詢、六種可見 SampleType、區間/區域明細、CRUD/Copy/Approve/View、DataStamp、Issue Job 與首錯停止流程。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護設定唯讀查 `TBLQCSAMPLINGPLAN_WAFER`、`TBLQCSAMPLINGRANGE`、`TBLQCSAMPLINGAREA_WAFER` table/index DDL、ALL_CONSTRAINTS；主檔 PK PlanNo、Area PK (PlanNo,Area)、Range 無 PK/index、沒有 FK；未讀業務列，未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/SamplingPlanWafer` Vue DEV 頁、QC → 基本資料選單/路由及 QC104 規格/家用端 API/schema handoff。AQL DEV options 為假資料；正式 API 對照 `wsSYS.LoadMesParameterRef`。
- [ ] 未執行 build/test/瀏覽器驗收；家用端先核對 `TBLQCSAMPLINGRANGE` 與 QC103 共用表的既有 schema/API，再依待辦實作 QC104；QC105–QC107 已完成；公司端下一項為 QC110「特殊主檔維護」。


### QC103 抽樣計劃（公司端 2026-11-17）

- [x] 對照 `frmSamplingPlan`/`frmSamplingPlanDef`、wsQC、`clsQCBasis`/`clsBasis_UPD`：四狀態與 PlanNo 查詢、PlanType 動態參數、ADD/EDIT/DEL/Copy/Approve、DataStamp、Issue Job、Simulation 入口及多筆首錯停止規則。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經既有受保護連線唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER` table/index DDL 與 ALL_CONSTRAINTS；兩表各有 PK index，沒有 FK/額外索引。未讀業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立兩表 Oracle source DDL 與 PostgreSQL CREATE 參考；新增 `/YMES/QC/SamplingPlan` DEV 頁、QC → 基本資料選單/路由、QC103 規格、家用 API/schema handoff。
- [x] DEV UI 有主檔/參數編輯、狀態篩選、查詢、複製、簽核、刪除、套用及 Simulation 操作；本地 Simulation 只示範 Fixed Sample Size，其餘由正式 API 使用 Core 規則計算。
- [ ] 未執行 build/test/瀏覽器驗收；QC104、QC105 已完成，QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。


### QC102 抽樣計劃設定_1916（公司端 2026-11-17）

- [x] 對照 `frmSampling1916`/`frmSampling1916Def`、wsQC、`clsQCBasis` 與 `clsBasis_UPD`：全表載入、Attributes/Variables 前端切換、欄位顯示、複合鍵、驗證、ADD/EDIT/DEL/Copy/Apply 與多筆刪除首錯停止行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 使用既有受保護連線，直連唯讀取得 `MESTEST.TBLQCSAMPLING1916` table/index DDL 與 ALL_CONSTRAINTS；確認四個 NOT NULL、唯一 index `(SamplingType,MinNum,MaxNum,SLevel)`，沒有 P constraint、FK 或其他 index。未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立來源 DDL 與 PostgreSQL 空表 CREATE 參考；新增 `/YMES/QC/Sampling1916` DEV 頁、QC → 基本資料選單/路由、QC102 規格及家用 API/schema handoff。
- [x] 更新規格索引、資料庫查詢來源紀錄及兩台電腦待辦。DEV 假資料限開發預覽 localStorage。
- [ ] 未執行 build/test/瀏覽器驗收；QC103–QC105 已完成，QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。


### QC101 抽樣表格（公司端 2026-11-17）

- [x] 對照 frmSamplingTable、frmSamplingTableDef、frmSamplingTableCopy、wsQC 與 clsQCBasis/clsBasis_UPD，整理查詢、複合鍵、範圍驗證、Apply、單列 Copy、整表 Copy 的部分成功行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護設定唯讀查得 MESTEST.TBLQCSAMPLINGTABLE table/index DDL 及 ALL_CONSTRAINTS；確認複合 PK (SAMPLINGTABLE, MINNUM, MAXNUM)、唯一 PK index、四個 NOT NULL、無 FK/其他索引。未讀業務資料，未在 Oracle 執行 DDL，未輸出憑證。
- [x] 新增 PostgreSQL 空表 CREATE 參考、/YMES/QC/SamplingTable Vue DEV 畫面、QC → 基本資料 → 抽樣表格 fallback 選單與 API/schema handoff 規格。
- [x] 更新 QC 規格索引、資料庫來源紀錄與兩台電腦待辦；未執行 build/test/瀏覽器驗收。
- [x] QC102–QC107 已完成；公司端接續 QC110「特殊主檔維護」，依 MES_MENU_FUNCTION_INVENTORY.md 檢查 WinForms Form/Service、資料表及邏輯。
### QC111 8D 原因基本資料（公司端 2026-11-17）

- [x] 從 8D 原因基本資料 WinForms Form、兩個 Def Form、wsQC 與 clsQCBasis 整理雙 Grid、欄位、查詢、狀態、CRUD、複製、刪除、核准與 Issue/Job 流程。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 的既有受保護連線，直接唯讀查 MESTEST.TBLQCREASONBASIS_8D、TBLQCREASONSUBTYPE_8D 的 DBMS_METADATA DDL、欄位 NULL/default、PK、unique index 與 FK；確認僅 PK index、沒有 FK。未讀 MES 業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立 Oracle source DDL 與 PostgreSQL 空表 CREATE 參考，檔案位於 database/reference/mestest_qc_8d_reason_source_ddl.sql、mestest_qc_8d_reason_create.sql。
- [x] 新增 Vue 雙 Grid DEV 頁 web/src/components/Qc8DReasonBasisMaster.vue，建立 QC → 基本資料 → 8D 原因基本資料 fallback 選單及 /YMES/QC/8DReasonBasis；包含四狀態、查詢、主/明細 CRUD/Copy/Approve、KD 黃色輸入欄及 localStorage 假資料。
- [x] 新增 docs/modules/QC/8D原因基本資料.md，記錄 Oracle 欄位、WinForms/Form/Service/Core 邏輯、Issue Job 簽核、刪除孤兒明細行為及 API/schema handoff；更新規格索引、資料庫連線紀錄及兩台電腦 TODO。
- [ ] 未執行 build/test/瀏覽器驗收。ReasonType 代碼與繁中資源已直接查證；DEV approval 只模擬無簽核 Job 的直接核准分支，Production 必須實作 Issue/Job。
- [x] 原先排定的下一項為 QC406；確認目錄後完成 QC406，再按功能目錄接續完成 QC407「最後處理群組（FINAL GROUP）」。

### QC406 第一處理群組（公司端 2026-11-17）

- [x] 修正 QC 選單功能識別：FunctionNo `FIRST GROUP` / FunctionKey `QC406`；`FINAL GROUP` 才是 QC407。
- [x] 對照 `frmFirstGroup`、`frmFirstGroupDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`，整理狀態篩選、複合鍵、查找來源、多 OP 批次新增/複製、`OPNo='*'`、刪除、DataStamp 與 `FIRST GROUP` Issue Job 簽核。
- [x] 用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀查得 `TBLQCFIRSTGROUP` DDL、欄位 nullable/default、複合 PK、unique index 與 FK；並查核三張已存在查找表的 key/必要欄位。未讀業務資料，未執行 Oracle DDL，未輸出憑證。
- [x] 建立來源 DDL、PostgreSQL 空表 CREATE、Vue DEV 畫面 `/YMES/QC/FirstGroup`、規格文件及家用端 API/schema handoff；更新 DB 連線紀錄與索引。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准僅模擬無簽核流程直接轉 Active。
- [x] 下一個功能 QC407「最後處理群組（FINAL GROUP）」已於本次完成，詳見下節。

### QC407 最後處理群組（公司端 2026-11-17）

- [x] 對照 `frmFinalGroup`、`frmFinalGroupDef`、`wsQC.LoadFinalGroup*`/`*FinalGroup`、`clsQCBasis` 與 `clsBasis_UPD`；記錄四狀態篩選、複合鍵、查找、多 OP 新增/複製、`OPNo='*'`、刪除、DataStamp、Issue Job 與附件/通知流程。
- [x] 使用既有受保護連線，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 `TBLQCFINALGROUP` DDL、欄位/null/default、PK/index/FK；確認無 FK、只有複合 PK unique index。未讀業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立 Oracle source DDL、PostgreSQL 空表 CREATE、Vue DEV 頁 `/YMES/QC/FinalGroup`、QC → 異常設定 → 最後處理群組路由、規格文件及家用 API/schema handoff；共用 QC406 lookup table 定義。
- [x] 保留核准 Job Map key 的原始字串 `Final GROUP`，與 FunctionNo `FINAL GROUP` 明確區分。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准僅模擬沒有 Job/Detail 時直接轉 Active。
- [x] 下一個功能 QC404「異常說明（HOLD DESCRIPTION）」已於本次完成，詳見下節。

### QC404 異常說明（公司端 2026-11-17）

- [x] 對照 `frmHoldDescription`、`frmHoldDescriptionDef`、`wsQC`、`clsHoldBasis` 與 `clsBasis_UPD`；整理單 Grid、四狀態、MClassNo 查詢、GUID 主鍵、唯讀欄位、複製/套用、刪除、DataStamp 與 `HOLD DESCRIPTION` Issue Job/附件通知規則。
- [x] 用既有受保護連線，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀查得 `TBLQCHOLDDESCRIPTION` table/index DDL、欄位/null/default、PK/index/FK。確認 PK 為 HoldDescriptionID、MClassNo 有非唯一索引、沒有 FK；未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立 Oracle source DDL、PostgreSQL 空表 CREATE、Vue DEV 頁 `/YMES/QC/HoldDescription`、QC → 異常設定 → 異常說明三層選單、功能規格及家用端 schema/API handoff。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准只模擬無 Job/Detail 時直接轉 Active。
- [x] QC405「異常處理群組（HOLD DISP GROUP）」已完成，詳見下節。

### QC405 異常處理群組（公司端 2026-11-17）

- [x] 對照 `frmHoldDispGroupBasis_THEIL_BU1`（來源 Form 位於 QC_HoldDispGroupBasis 專案）、`frmHoldDispGroupBasisDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`；完成上方群組 Grid 與下方三 Grid 人員分配版面、四狀態/GroupNo 查詢、CRUD/Copy/Delete/Approve、主處理/CC 切換、代理成員同步及 DataStamp/Issue Job 規格。
- [x] 直接沿用已由 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得的兩張 Oracle DDL：`TBLQCHOLDDISPGROUPBASIS` 在 QC406 lookup source DDL，`TBLUSRUSERGROUP` 在設備工程群組成員 source DDL；其 PostgreSQL 空表 CREATE 參考已存在。沒有新表或新 CREATE SQL 要交付。
- [x] 新增 Vue DEV 頁 `web/src/components/QcHoldDispGroupMaster.vue`，建立 QC → 異常設定 → 異常處理群組與 `/YMES/QC/HoldDispGroup`；加入四狀態、群組 CRUD/Copy、組員多選移動、Type 5 主處理/Type 6 CC、套用式儲存及 localStorage DEV 假資料。
- [x] 新增 `docs/modules/QC/異常處理群組.md`，記錄 Form/Service/Core、兩張表與 DDL 參照、精確 `HOLD DISP GROUP` Issue key 及 Production API/schema handoff；同步索引、DB 查詢紀錄與家用端 TODO。
- [ ] 未執行 build、test 或瀏覽器驗收。DEV 核准只演示 0→2；Production 必須依 Issue/Job 處理。家用端比對 live schema/migrations 後實作 API；不可在 MES Oracle 執行 CREATE。
- [x] QC403「異常原因基本資料（HOLDITEM BASIS）」已完成公司端 DEV UI、WinForms 規格與 Oracle/PG CREATE 參照，詳見下節。

### QC403 異常原因基本資料（公司端 2026-11-17）

- [x] 對照主清單/Def/匯入 Form、HoldItemDataImporter、wsQC、clsHoldBasis；完成四狀態與三欄查詢、欄位/lookup、CRUD/Copy/Delete/Approve、DataStamp、HoldSystem、OCAP/通知群組及 Excel 匯入規則。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 既有受保護唯讀連線，查得 MESTEST.TBLQCHOLDITEMBASIS DBMS_METADATA DDL、欄位 NULL/default、PK/index/FK。只有 HoldItemNo PK unique index，沒有其他 index/FK；未讀業務列，未在 Oracle 執行 DDL，未輸出憑證。
- [x] 新增 Oracle source DDL 與 PostgreSQL 空表 CREATE：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`、`mestest_qc_hold_item_basis_create.sql`。
- [x] 新增 Vue DEV 頁 `web/src/components/QcHoldItemBasisMaster.vue`，路由 `/YMES/QC/HoldItemBasis`；含查詢、四狀態 Grid、CRUD/複製、欄位 picker、核准/刪除及 CSV 假資料匯入。
- [x] 新增 `docs/modules/QC/異常原因基本資料.md`，更新規格索引、DB 連線查詢紀錄及 home/office TODO；正式 API、Issue Job 與 `.xls/.xlsx` 匯入由家用端接手。
- [ ] 未執行 build、test 或瀏覽器驗收。DEV 核准僅模擬無 Job/Detail 的 0→2；已簽核/不使用欄位權限仍需家用端對接 IssueControl 規則。
- [x] QC401「主分類基本資料」已完成公司端 DEV UI 與 WinForms/Service/Core 規格，詳見下節。
- [x] QC402「次分類基本資料」已完成公司端 DEV UI、WinForms/Service/Core/Importer 規格與 Oracle DDL/PostgreSQL CREATE 交接，詳見本檔 QC402 區段。
- [ ] 下一項依功能目錄與未遷移頁面，接續尚未完成的 MES Function。

### QC401 主分類基本資料（公司端 2026-11-17）

- [x] 讀取 frmMClassBasis、frmMClassBasisDef、wsQC 與 clsHoldBasis；確認四狀態、MClassNo 查詢、Grid 欄位、Def 表單、Copy/Apply、批次 Delete/Approve、DataStamp 和 MCLASS BASIS Issue Job 規則。
- [x] TBLQCMCLASSBASIS Oracle source DDL 與 PostgreSQL CREATE SQL 已由先前 PowerShell 5.1 + Oracle.ManagedDataAccess 直連查得並與 QC406 共用，不新增重複 DDL/CREATE 檔；MClassNo 為 PK，僅 PK unique index，無 FK/額外索引。
- [x] 新增 Vue DEV 頁 web/src/components/QcMClassBasisMaster.vue；加入三層 fallback menu、FunctionNo MCLASS BASIS / FunctionKey QC401 和 /YMES/QC/MClassBasis 路由。表格標頭與欄位使用 KD 黃色系；同步修正 QC403 藍色 Grid 表頭。DEV localStorage 假資料含四種狀態。
- [x] 新增 docs/modules/QC/主分類基本資料.md，更新公司/家用 TODO、兩份功能索引及 Oracle 連線紀錄；家用端僅需核對 live schema，缺表才建立 migration。
- [x] 找到並對照獨立 frmMClassDataImport 與 DataImporter.MClassDataImport。確認 NPOI 第一工作表、首列跳過、前兩欄、distinct pair、MClassNo 大小寫不敏感去重、每編號獨立部分成功及結果 Grid；更新 QC401 規格與 Home API 契約。正式範本二進位檔未在來源樹找到，Production 需沿用系統 template storage。
- [ ] 未執行 build、test、瀏覽器驗收或 YMES PostgreSQL 查詢。家用端 API、schema 比對與正式 Issue Job 仍待完成。

### EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader 格式檔設定（2026-11-17）

- [x] 已確認舊 WinForms source tree 有完整原始碼：`Industry_Base/MESWin_Industry_Base/EQP/EP_B_EQPAutoLoaderSetup/`，並對照主畫面/編輯器/Default、相關子表單、`wsEQP`、`clsEQP` 及 `wsSYS` AutoLoader 檢查。先前 TODO 寫「找不到 Form」是錯誤，本次已修正認知。
- [x] 使用既有 Oracle 受保護設定，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 4 張表的 source DDL、欄位、NULL/default、PK/index/FK；未讀 MES 業務資料，也未在 Oracle 執行 DDL。
- [x] 加入 Vue DEV 頁 `web/src/components/EquipmentAutoLoaderSetupMaster.vue`，涵蓋三層選單、查詢/選取、完整可見欄位、格式腳本、CRUD/Copy/Default、5 個設定頁籤及 KD 黃色欄位。
- [x] 加入功能規格、Oracle source DDL、PostgreSQL 空表 CREATE 參考、規格索引與家用端 API/schema handoff。詳細差異與簡化項目見 `docs/modules/EQP/設備AutoLoader格式檔設定.md`。
- [ ] 未執行 build/test/瀏覽器驗收。家用端先完成 schema/API/migration 並部署後，公司端 pull 最新 main，逐欄對接 Production，再補完整 TestFile parser、公式/ValueList 與設備查找 UI。

### 2026-11-17 MTL ML101 物料供應商基本資料

- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀查得 `TBLMTLMATERIALVENDOR`、`TBLMTLMATERIALVENDORCONTACTOR` 完整欄位、NULL/default、主鍵、索引及 FK；沒有讀取 MES 業務資料或在 Oracle 執行 DDL。
- [x] 對照 `frmMaterialVendor`、主檔/連絡人 Def、`wsMTL`、`clsMTLBasis`，完成四狀態查詢、雙 Grid、CRUD/複製/多筆刪除/核准、連絡人 Mail、Issue/Job、audit 與 DataStamp 規格；新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL 及獨立規格文件。
- [x] 新增 `web/src/components/MaterialVendorMaster.vue`，依 WinForms 雙 Grid 與 KD 黃色欄位完成 DEV localStorage 互動；新增 MTL fallback 三層選單與 `MTL VENDOR` 路由 `/YMES/MTL/MaterialVendors`。
- [x] 已將 API、schema、migration handoff 寫入 `todolist_homepc.md`；提醒家用端修正 material vendor map 名稱查詢誤 join EQP vendor 的問題。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML102 物料類別資料

- [x] 對照 `frmMaterialType`、`frmMaterialTypeDef`、`frmMaterialGumType`、`frmMaterialProperty`、`wsMTL`、`clsMTLBasis`；整理畫面、狀態、CRUD、Copy GumHour、Property/MTL PROPERTY、固晶膠核准檢查與 GumHour 數值驗證。
- [x] 確認 Oracle DDL/PG CREATE SQL 已存在於 `mestest_mtl_master_*`，包含 MaterialType、Property、TypeQCItem、TypeAcclimation；此功能沒有新增 table/CREATE SQL。
- [x] 新增 `web/src/components/MaterialTypeMaster.vue`，建立 `/YMES/MTL/MaterialTypes`、`MTL TYPE` 動態/fallback 路由；包含狀態查詢、主 Grid、Add/Edit/Copy/Delete/Approve、Property 與 GumHour 視窗，Copy 會一併複製類別時效，核准後 GumHour 唯讀，DEV 假參數不代表正式系統參數值。
- [x] 更新 ML102 規格、索引及家用端 schema/API/Issue handoff。
- [x] 重新確認 origin/main 與公司端基線相同；已把 Copy 的單交易 API 契約與 GumHour POST/PUT 區分補進家用端待辦。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML105 物料類別品管項目設定

- [x] 對照 `frmMaterialTypeQCItem`、`wsMTL`、`wsQC`、`clsMTLBasis`；確認前綴查詢、三種 QC 候選清單、雙向多選、類別範本儲存及同步至所有關聯物料/IQC 項目。
- [x] 確認 `TBLMTLTYPEQCITEM`、`TBLMTLQCITEM` 的 Oracle DDL/PG CREATE 已在 MTL reference 和 0013 migration；無新增 CREATE SQL。
- [x] 新增 ML105 Vue DEV 頁與三層 MTL 路由，並建立 Production API/transaction handoff 規格。
- [ ] 未執行 build、test 或瀏覽器驗收；等待家用端完成 API 後再接 Production endpoint。

- Oracle MESTEST 直接查詢已可用，資料字典可見 1,287 張表。查詢方式及憑證規則見 `docs/DATABASE_CONNECTION.md`。
- 2026-11-16 已用 PowerShell 5.1 + Oracle.ManagedDataAccess 查得 `TBLUSRUSERGROUP` 原始 DDL、6 欄 NULL/default、PK 與索引資訊；來源 DDL、空表 PostgreSQL CREATE SQL、設備工程群組規格及家用端待辦均已更新。
- 2026-11-16 已查得 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 原始 DDL、欄位/PK/索引/FK；SMD 空表 CREATE SQL 與設備群組規格已補齊。
- 設備群組 Vue 已依 WinForms 對齊：編號失焦轉大寫；查詢/狀態切換時清除不可見列選取，DEV 核准的 Issue flow 簡化已記錄。
- 2026-11-16 已從 WinForms/Service 確認配件類別選項表為 TBLEQPACCESSORYTYPE，Oracle 9 欄 DDL/PK 索引已直連核實；來源與空表 PostgreSQL CREATE SQL 已交付家用端。
- WinForms 正式選單目錄已查得 20 個 Module、601 筆 MenuItem、553 筆 Function；繁中第一層名稱取自 `D:\iMES\CSiMES\Customer\Production\MESClient\mesresource.zh-cht.resources`。完整 ModuleOrder、分類、FunctionNo、FormName、ExecutionFile 見 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司電腦開工前已 pull GitHub `origin/main` 最新版本；家用端已部署 20 個模組的三層選單、物料子功能 API、設備供應商 API 與設備狀態 API。每次接續仍先 pull，並對照新 endpoint/options 的 Vue 呼叫契約與來源 WinForms。
- 作業站主表、群組明細及 Service 明確使用的 16 張子表，連同 3 張系統目錄表，共 21 張來源 DDL/空表 CREATE SQL 已交付；物料 8 張來源 DDL/空表 CREATE SQL 也已交付。檔案索引見 `docs/DATABASE_CONNECTION.md`、`docs/modules/OP/作業站主檔.md`、`docs/modules/MTL/物料主檔.md`。
- `TBLOPBASIS` 等 21 張作業站/系統表已確認每表一個 PK unique index，沒有額外獨立索引；本次查的 29 張 OP/系統/MTL 表沒有 Oracle FK constraint。PK/NOT NULL 保留在 CREATE SQL。
- `tblmtlmaterialbasis` 的 YMES bootstrap 已補齊 7 個來源 DEFAULT；多出的 7 個 YMES 欄位仍保留，等家用電腦比對實際 schema，不要自行刪除。
- 物料主檔及其子功能已有 DEV localStorage 互動雛形；物料匯入已補 CSV/`.xlsx` 預覽、9 欄解析、物料編號/名稱分組、結果列、開發範本下載及逐組假資料進度/取消；正式範本下載、`.xls` 預覽與群組 API 規則待家用端。詳細規格見 `docs/modules/MTL/物料資料匯入.md`。作業站子功能也有簡化 DEV 視窗。設備設定已有七個功能頁雛形。設備狀態的兩張刪除引用表 DDL、空表 PostgreSQL CREATE SQL、正式 ValueList 和繁中翻譯已於 2026-11-16 交付家用端；家用端仍需 migration 和 API 刪除檢查部署。其他頁面仍待逐項 WinForms 對照。
- 本次未執行 build 或測試；未完成整套 MES 遷移，也未把尚未完成功能標為正式上線。

## 2026-11-16 SMD SD104 區域基本資料公司端交接

- [x] Pull `origin/main` 最新版本，確認 `82de6f4` 為當前基準。
- [x] 從 `frmAreaBasis`、`frmAreaBasisDef`、`frmAreaLayout`、`frmNodeDef`、`wsSMD`、`clsSMDBasis` 邏輯完成 SD104 主 Grid/表單/狀態/核准/刪除規則整理；AddFlow 版面在 Vue 先做可操作簡化，差異已列規格。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線直查 `TBLSMDAREABASIS`、`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP` DDL、欄位、PK、索引與 FK；建立 source DDL 和 PG 空表 CREATE SQL。關聯配置兩表使用設備群組來源 DDL/CREATE SQL。
- [x] 建立 SMD fallback 三層目錄（8 個 WinForms Function）；SD104 接上 DEV Vue 頁，其他 7 個仍由 FunctionPending 說明。
- [x] 新增 `docs/modules/SMD/區域基本資料.md`，並更新規格索引、資料庫連線紀錄及家用端待辦。
- [x] 修正 SD104 DEV 預設列，使未簽核子區可選已簽核 Base Area；套用新增後轉入 Edit 狀態；核准頁採用 API 回傳實際 IssueState；配置保存不限制 IssueState，符合 WinForms 對應程式。
- [ ] 本段尚未執行 build/test/瀏覽器驗收；家用端 API/schema 完成後需 pull、逐欄對接並檢查正式行為。

## 2026-11-16 SMD SD105 區域庫房基本設定公司端交接

- [x] 對照 `frmAreaInventory`、`frmAreaInventoryDef`、wsSMD 與 clsSMDBasis；記錄 Base Area 選擇、兩種 AreaRelation picker、Default 唯一規則、CRUD/Apply/Copy/Delete 與來源參數不一致處。
- [x] Oracle `TBLSMDAREAINVENTORY` DDL/欄位/PK/index 已由 SD104 PowerShell 5.1 + ODP.NET 直連查得，且已附 `mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`；本功能沿用既有表，沒有重複查詢或另造 SQL。
- [x] 新增 Vue DEV 頁 `/YMES/SMD/AreaInventories`、Area/Inventory 查找彈窗、Default/重複驗證、Apply、稽核、localStorage 假資料及 `AREA INV MAP` 路由。
- [x] 新增 `docs/modules/SMD/區域庫房基本設定.md`，更新 SMD 功能索引、資料庫查詢紀錄與家用端 API/schema handoff。
- [ ] 尚未執行 build、test 或瀏覽器驗收；家用 API 完成後再 pull 對接。
- [x] SD106「過帳電腦之區域」已完成 WinForms Form/Service/Core 對照、Oracle DDL 直查、DEV Vue 頁及 API/CREATE SQL handoff；進度見下方 SD106 區塊。
- [x] SD101「工廠基本資料」已完成，WinForms/Service/Core 對照、Vue DEV 頁與家用端 API/schema handoff 見下方區塊。Oracle DDL 沿用 2026-11-16 EP303 已直連查詢的結果，沒有重查。

## 家用電腦完成後，公司端先做

- [x] 2026-11-16 Pull 家用電腦最新提交 `944b793`，閱讀更新後的 DTO/endpoint/migration 與 `todolist_homepc.md`；設備狀態 Production endpoint 使用 `GET/options/POST/PUT/DELETE`，Vue 呼叫路徑已對上。
- [ ] 用公司端 Development 假資料頁逐頁核對 API 欄位名稱、狀態值、DataStamp、錯誤訊息與選項，不讓 Production 回退 localStorage。
- [ ] 以目前已查到的 20 個模組與資源檔標題整合三層側欄：Module → Menu Group → Function；維持 ModuleOrder、MenuOrder、FunctionOrder。未遷移功能要明確顯示尚未遷移，不能導向空白頁。
- [ ] 按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 逐筆核對 FunctionNo、FormName、ExecutionFile 與 `D:\iMES\CSiMES\STD\MESWin` Form/Designer/Service；一個 Function 建一份或更新一份獨立規格，不以截圖代替程式碼/DDL。
- [ ] 先完成核心五模組既有頁面與子功能的 WinForms 對照：EQP 設備設定七項、OP 作業站子功能、MTL 屬性/QC Item/供應商/GumHour/匯入、PRS/PRD 擴充頁面。每個頁面復刻查詢欄位、資料欄位、按鈕、表單、狀態規則與互動。
- [x] MTL 匯入 DEV 畫面改依 WinForms 呈現：`.xlsx`/CSV 預覽、9 欄欄序、按物料編號/名稱分組、逐組結果、開發範本、進度與取消。規格見 `docs/modules/MTL/物料資料匯入.md`。
- [ ] MTL 匯入仍需家用端提供正式範本下載、`.xls` 預覽、ErpUnitNo 及與 WinForms 一致的群組寫入/結果契約；之後再做 Production UI 對接。
- [x] 設備點檢表設定 FunctionNo/FunctionKey 已核實為 `EQP QCLIST`/`EP107`；透過 PowerShell 5.1 唯讀查 MESTEST ValueList 並核對繁中資源，QCType 合法值為 0「開始生產時點檢」、2「手動點檢」。已修正 DEV 選項及新增預設值；Production options API 交由家用端依規格提供。
- [x] 設備工程群組、設備類別、設備群組 FunctionNo/FunctionKey 已由目錄 inventory 核實：`EQP ENGINEER GROUP`/`EP102`、`EQP TYPE`/`EP103`、`EQP GROUP BASIS`/`EP105`。
- [x] 設備類別匯入 DEV UI 改依 WinForms：第一工作表固定 8 欄、略過標題列、依 EquipmentType/EquipmentClass 顯示結果、產生 .xlsx 開發範本、逐類別進度與取消。假資料不模擬每類別多列或舊 Checker，明確提示且不寫入；.xls/正式 Checker 交家用 API。規格與 API handoff 已更新設備類別文件。
- [ ] 按 ModuleOrder 繼續其餘 15 個模組、476 個 Function。每頁先查 WinForms Form/Designer/Service；需新表時由公司端 PowerShell 查原始 DDL，再提供家用端 CREATE SQL/API 規格。不要猜欄位或流程。
- [ ] DEV 互動使用清楚標示的本機假資料；正式 API 未提供時停用或顯示未接通，不能回報假成功。
- [ ] 每完成一個功能，更新對應 `docs/modules/<ModuleNo>/...md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md`、`todolist_homepc.md`/本檔與 Function 遷移狀態。
- [ ] 如遇 WinForms 隱藏的複雜功能，先做可操作的簡化版並記錄未復刻規則，繼續下一頁；把需要使用者決策的精確差異集中列出。
- [ ] 全部功能完成後核對三層選單、FunctionNo 路由、UI/API 契約、規格文件與資料庫交接；完成後 push GitHub。

## Oracle 來源交付給家用電腦



- [x] **設備工程群組成員表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `MESTEST.TBLUSRUSERGROUP` DDL、欄位 NULL/default、PK、索引/FK 字典。來源 Oracle DDL、PostgreSQL 空表 CREATE SQL 與規格已更新；家用端需核對 YMES schema，缺表才新增 migration 並實作 GroupType=2 成員 API。
- [x] **設備群組 SMD 映射表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` DDL 與欄位/PK/索引/FK。家用端比對 YMES schema，實作新增/移除設備明細時的舊 SMD 同步語意。

- [x] **設備狀態設定來源資料已交付（2026-11-16）**：以 PowerShell 5.1 唯讀查 MESTEST 的 `TBLEQPSTATECONTROL` / `TBLEMSEQUIPMENTSTATE` 物件、欄位、PK/唯一索引、NOT NULL/default 與 DDL；查 `TBLSYSVALUELIST` 的兩組選項，並核對 `mesresource.zh-cht.resources` 繁中翻譯。來源 DDL、PG 空表 CREATE SQL 與規格已更新；家用端仍待比對引用表 schema、migration/SELECT 權限及 DELETE 完整驗收，CRUD/options API 已部署。

- [x] 設備類別配件目錄已交付（2026-11-16）：已核實 MESTEST.TBLEQPACCESSORYTYPE 原始 Oracle DDL、9 欄 nullable/default、PK 唯一索引與無 FK，並建立 PostgreSQL 空表 CREATE SQL。家用端仍需比對 YMES 目標表與完成 API options。

## 2026-11-16 SMD SD106 過帳電腦之區域公司端進度

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，確認沒有新提交。
- [x] 對照 `frmComputerArea`、`frmComputerAreaDef`、wsSMD 與 clsSMDBasis，記錄主 Grid/定義 Form、查詢欄位、按鈕、工廠與區域選擇、複合鍵、Copy 與批次 Delete 行為；指出 Copy 舊 Core 只按 ComputerNo 找來源，以及 ws/Core DataStamp 簽章不一致。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護連線直查 `TBLSMDCOMPUTERAREABASIS`、`TBLSMDCOMPUTERAREADETAIL` DDL、欄位 NULL/default、PK/index/FK；只查結構，未讀業務資料，未在 Oracle 執行 DDL，憑證未輸出。
- [x] 新增 Oracle logical source DDL 與 PostgreSQL 空表 CREATE SQL；校正 first-phase reference 中 ComputerArea DataStamp DEFAULT 及 SD104 AreaBasis DataStamp DEFAULT，移除 ComputerArea 欄位推測註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ComputerAreaMaster.vue`、`/YMES/SMD/ComputerAreas` 路徑及 FunctionNo=`COMPUTER AREA`；實作查詢、多選、Add/Edit/Copy/Delete、approved Factory/Area 選擇、稽核與 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/過帳電腦之區域.md`，更新 SMD 規格索引、Oracle 連線查詢紀錄與家用端 schema/API handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/migration 待家用端完成。
- [x] SD101「工廠基本資料」已接上 Vue DEV 路由；既有 Oracle DDL/PG CREATE SQL 已確認可沿用，完成規格及 API/schema handoff。
- [x] SD103「儲位基本資料」公司端 UI、WinForms 規格與 Oracle/PG CREATE SQL 已完成；詳本文下方 2026-11-16 SD103 進度。

## 2026-11-16 SMD SD101 工廠基本資料公司端進度

- [x] 對照 `frmFactoryBasis`、`frmFactoryBasisDef`、wsSMD 與 `clsSMDBasis`/`clsBasis_UPD`；記錄四種 IssueState、FactoryNo 搜尋、IC_FT_PKG FactoryName 顯示、欄位/按鈕、Copy、批次 Delete/Approve、DataStamp 與 Issue/Job 規則。
- [x] Oracle `TBLSMDFACTORYBASIS` 原始 DDL、欄位 NULL/default、PK/index/FK 已於 2026-11-16 EP303 查詢直連取得；沿用 `mestest_carrier_basis_dependencies_*` 和 first-phase CREATE SQL，未重複查詢/新增 SQL。
- [x] 新增 Vue DEV 頁 `web/src/components/FactoryBasisMaster.vue`，接上 `/YMES/SMD/Factories` 與 FunctionNo=`FACTORY BASIS`；包含查詢/狀態篩選、選列/多選、Add/Edit/Copy/Delete/Approve、KD 黃色定義表單、稽核及 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/工廠基本資料.md`，更新功能索引、Oracle 查詢記錄與家用端 API/schema/簽核 handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端比對現況並補齊，核准需串正式 Issue/Job 流程。

## 結束規則

- 不要把「已建立 Vue 元件」視為「已完整復刻」；需對照舊 Form 的欄位、按鈕、流程與 Service 規則。
- 不要刪除來源 DDL 尚未核實的 YMES 欄位或資料表。對既有表先比對，缺表才建，差異用 forward migration。
- 連線密碼、Token、SSH 私鑰不可放入 `todolist_*.md`、SQL、程式碼或 Git。

## 2026-11-16 EP202 配件類別資料進度與下一項

- [x] GitHub 最新 main 已 pull；新增 web/src/components/AccessoryTypeMaster.vue，接上「設備資料 → 配件 → 配件類別資料」與 /YMES/Equipment/AccessoryTypes。
- [x] 依 WinForms 建立狀態篩選、AccessoryType 查詢、清單、Add/Edit/Copy、批次 Delete/Approve、屬性管理、壽命管理、匯入預覽及 KD 黃色系表單。
- [x] 建立 docs/modules/EQP/配件/配件類別資料.md；查詢 MESTEST 四表 DDL 並附原始 Oracle DDL / PostgreSQL 空表 CREATE SQL，家用 API 工作已寫入 todolist_homepc.md。
- [ ] 待家用 API 合併後，pull 並對接 EP202 Production endpoints；補 PropertyNo 系統選項選擇器、壽命公式編輯器及 Excel .xlsx/.xls 前端互動。
- [ ] 家用端 2026-11-16 執行最新 Web build 時，`AccessoryTypeMaster.vue` 第 52、59 行各有 TS1109 語法錯誤（copy 模式 description ternary 缺少 true 分支的空字串值）。修正並重新執行 `npm run build`，再做頁面瀏覽器驗收；家用端未直接修改 UI。
- [x] 已完成 EP201 配件分類與 EP204 可用狀態的 WinForms/Service 對照、Vue Development 畫面、規格及 Oracle DDL/PG CREATE SQL 交接；詳細完成狀態見本文下方 2026-11-16 更新。
- [ ] 本次未執行 build 或測試；完成後需在後續驗證階段處理，不代表 Production 功能已完成。

## 2026-11-16 EP201 配件分類與 EP204 可用狀態完成公司端交接

- [x] GitHub main 已確認為最新；本次保留先前未提交的 Oracle DDL 並繼續公司端工作。
- [x] 新增 web/src/components/AccessoryCategoryMaster.vue，接入設備資料 → 配件 → 配件分類資料與 /YMES/Equipment/AccessoryCategories；提供狀態/分類查詢、CRUD/複製、核准、狀態依來源刪除/停用、CSV 匯入預覽。
- [x] 新增 web/src/components/AccessoryCategoryStateMaster.vue，接入 EP204 獨立功能路徑，也可由 EP201 以 Vue 彈窗傳入分類；包括狀態選項、初始旗標、作用名稱/介面驗證、CRUD/複製、核准與刪除/停用。
- [x] App.vue 三層選單 fallback、動態 API menu merge、FunctionNo route 與 Vue component 分派已加入 ACC CATEGORY / ACC CATEGORY STATE。
- [x] 依 WinForms Form/Designer、wsEQP、clsAccBasis/clsBasis_UPD 追出按鈕、欄位、狀態、簽核與 DataStamp 規則；規格分別為 docs/modules/EQP/配件/配件分類資料.md、配件分類狀態定義設定.md。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 TBLEQPACCCATEGORYSTATEBASIS 與 TBLEQPACCSTATEBASIS DDL/欄位/PK/index；建立兩份 PostgreSQL 空表 CREATE SQL，修正 first_phase_create_tables.sql 中曾標成推測的 lookup 欄位 default。
- [x] todolist_homepc.md 與 docs/DATABASE_CONNECTION.md 已列出三表比對/migration、API/options、簽核、InitialFlag 交易/唯一性及欄位契約。
- [ ] 家用 API 完成後，公司電腦 pull 並逐欄對接/核對；EP201 Excel .xls/.xlsx 與舊 DataChecker 在 Vue Development 先簡化為 CSV，須依正式 API 再補前端體驗。
- [x] ACC BASIS / EP203「配件基本資料」已完成公司端復刻與 Oracle DDL 交接；詳見本文 EP203 進度及獨立規格。
- [ ] 未執行 build 或測試；整套 MES 其餘 Function 尚未完成，繼續依 ModuleOrder 往下逐項復刻。

## EP203 配件基本資料公司端進度（2026-11-16）

- [x] 已 pull GitHub main（f569a3f，當時與 origin/main 同步）。
- [x] 透過 PowerShell 5.1 + ODP.NET 使用本機受保護 Oracle 設定，唯讀取得 TBLEQPACCESSORYBASIS 與五張關聯/狀態表 DDL；並查得八個 Oracle index DDL，未讀 MES 業務資料。
- [x] 已將六表 Oracle 原始 DDL、PG 空表 CREATE SQL 及 schema 比對/API 工作交給家用端：database/reference/mestest_accessory_basis_source_ddl.sql、mestest_accessory_basis_create.sql、todolist_homepc.md。
- [x] 已依 WinForms frmAccessoryBasis/Def、EP_ACCProperty、AccessoryDataImport 追出狀態、清單顯示欄位、資料表、核准/刪除、複製、屬性及匯入規則；規格為 docs/modules/EQP/配件/配件基本資料.md。
- [x] Vue EP203 已加入設備資料 → 配件第三層選單、/YMES/Equipment/AccessoryBasis 頁面及 localStorage DEV 假資料互動；包括全 Grid 欄位、狀態篩選、多選按鈕、表單、屬性視窗、CSV 預覽與 Production Excel API 契約。
- [ ] 家用端比對六張 YMES 表並實作 Production API 後，公司端 pull 並對接正式 API。DEV 核准/簽核與 CSV 匯入僅作 UI 預覽，Excel 正式解析由 API 完成。
- [x] EP205「配件狀態設定」已完成 WinForms Form/Def/Service 對照、Vue Development 頁與 API/schema handoff；詳見 docs/modules/EQP/配件/配件狀態設定.md。
- [ ] 不執行 build/test，除非使用者提出驗證要求；不要將 Oracle 連線密碼寫進文件或 Git。

## 2026-11-16 EP205 配件狀態設定公司端進度

- [x] 依 frmEQPACCStateBasis、frmEQPACCStateBasisDef、wsEQP、clsAccBasis 核對主 Grid、ADD/EDIT/DEL/Copy/Close、Apply/Confirm、數值/StateType/色彩/稽核/DataStamp 規則。
- [x] 核對 Oracle TBLEQPACCSTATEBASIS DDL、欄位/PK/index；沿用既有來源 DDL 與 PostgreSQL 空表 SQL，不重複建立表。
- [x] 新增 web/src/components/AccessoryStateBasisMaster.vue，接上「設備資料 → 配件 → 配件狀態設定」和 /YMES/Equipment/AccessoryStates；Development localStorage 假資料提供新增、修改、複製、刪除、多選及 Apply/Confirm。
- [x] 建立 docs/modules/EQP/配件/配件狀態設定.md，更新規格索引與家用端 schema/API TODO。
- [ ] 家用端核對 YMES schema 並完成 Production API 後，公司端 pull 對接實際 API。
- [ ] 下一項繼續按設備 FunctionNo 追 WinForms Form/Service；需新增資料表時由 PowerShell + ODP.NET 唯讀查 Oracle DDL。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP101 設備供應商基本資料補正（公司端 2026-11-16）

- [x] 重新核對 frmEQPVendor、frmEQPVendorDef、frmEQPVendorContactorDef：補上新增/複製供應商「...」帶入編號按鈕，以及供應商與聯絡人表單的「確定、關閉、套用」流程。
- [x] Development 假資料改由應用程式環境選擇控制；切到 Development 才讀寫 localStorage，切換環境或 API 位址時重新載入。正式/測試環境使用 API。
- [x] 儲存後更新表單 DataStamp 與稽核欄位；更新 EP101 規格、規格索引及本檔。
- [ ] 下一項按設備功能清單繼續對照 WinForms Form/Designer/Service；只有發現未有規格的資料表時才查 Oracle DDL 並交付家用端。
- [ ] 本次未執行 build 或測試；未變更 API 與資料庫 schema。


## EP102 設備工程群組資料補正（公司端 2026-11-16）

- [x] 直接核對 frmEQPEngineerGroupBasis/Def 與 Designer：主按鈕順序 Add/Edit/Delete/Copy/Close；Find/Print 隱藏；Def 支援 Confirm/Close/Apply；編號失焦轉大寫。
- [x] Vue 表單加入套用並保留表單、儲存後更新 DataStamp/稽核欄位；主按鈕依 WinForms 排序。
- [x] Development 假資料依應用程式選取環境啟用；切換環境/API 位址時重新載入；Test/Production 呼叫 API。
- [x] 更新 EP102 規格、索引與本檔。正式 API及 TBLUSRUSERGROUP schema 工作仍留給家用端既有 TODO。
- [ ] 下一項繼續依設備設定清單對照 WinForms Form/Designer/Service；需要新資料表才查 Oracle DDL 並交付家用端。
- [ ] 未執行 build 或測試；沒有更動 API/schema。


## EP103 設備類別資料公司端 UI 規格完成（2026-11-16）

- [x] 直接對照 frmEquipmentType/Def、ConsumeBasis/Def、EQPProperty/Def、圖片選擇器及工程群組查詢事件；完成主表單、耗材表單、屬性表單的確定/關閉/套用與套用異動狀態。
- [x] EngineerGroupNo、PictureName 改為唯讀欄位與 WinForms「...」選擇視窗；設備圖片上傳與圖片庫選擇保留。
- [x] Development 假資料只在 Development preview 啟用；EP103 頁使用共用 api/equipment/engineer-groups 契約，其他環境走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備類別資料.md、規格索引及家用端 API 待辦；沒有新增資料表或 SQL。
- [ ] 家用電腦比對五張設備類別表與配件目錄表 schema，完成 API/圖片/匯入/簽核流程後，拉回最新版本做 Production 契約核對。
- [x] EP105「設備群組資料」WinForms Form/Designer/Service 已核對並完成公司端 UI 規格補正；EP106 後續進度見本文 EP106 區塊。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP105 設備群組資料公司端 UI 補正（2026-11-16）

- [x] 直接對照 frmEquipmentGroup/Def、Designer、clsEQPBasis 與 wsEQP；保留主從清單、隱藏 Copy 按鈕、未核准群組才能編輯/加入/移除、設備只能加入一次及 Chamber 排除規則。
- [x] 群組 Def 補上 WinForms 確定/關閉/套用、異動後才啟用 Apply、建立/修改稽核欄位、編號失焦轉大寫及儲存後更新 DataStamp/清單。
- [x] Development 假資料改由所選 Development preview 控制；環境/API 位址切換會重新載入。Production 走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備群組資料.md、規格索引與 todolist_homepc.md。主從與 SMD 表 Oracle DDL/CREATE SQL 已先前查得，本次無新表/無需再查 DDL。
- [ ] 家用端按 todolist_homepc.md EP105 區塊比對 schema、完成 CRUD/Approve/SMD API，部署後公司端再對接並驗證。
- [x] EP106 設備狀態設定 UI/互動已完成本輪 WinForms 對照；詳見下方 2026-11-16 進度。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP106 設備狀態設定公司端補正（2026-11-16）

- [x] 開工前 pull GitHub `main`，確認與 `origin/main` 同步於 `894d5a4`；來源 WinForms 位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_EQPStateBasis`。
- [x] 按 Designer 還原表單尺寸與單欄欄位排列，校正主功能按鈕順序及 Def 表單 Confirm/Close/Apply 順序；Set Color、Add/Copy Apply 保留欄位與編號焦點行為已補齊。
- [x] Development 假資料僅在 App 環境選為 Development 且執行 DEV build 時啟用；切換環境/API 位址會重載。Test/Production 使用 API。
- [x] Delete 會把目前列合併到多選項目，逐筆檢查引用與刪除；部分成功後重載並保留錯誤，符合舊表單逐筆處理行為。
- [x] 更新 EP106 規格、索引及家用端 EP106 待辦；本次未新增表/DDL，既有兩張引用表 Oracle DDL/PG CREATE SQL 已交付。
- [ ] 家用端比對兩張引用表 schema/migration、授予最小 SELECT 權限，移除 DELETE fail-closed 503；部署後驗證 409/204 引用檢查。
- [x] EP107「設備點檢表設定」已於後續進度完成 WinForms Form/Designer/Service 對照與 Vue 補正。
- [ ] 未執行 build 或測試；不代表 Production 刪除驗收已完成。


## EP107 設備點檢表設定公司端 UI 補正（2026-11-16）

- [x] 開工前 pull GitHub `main`；最新已推送基準為 `1ab42be`，`git pull --ff-only origin main` 確認已同步。來源 WinForms 為 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_QCListBasis`。
- [x] 對照主 Form、Def、Designer 與 Service：主從 Grid、作用列+多選刪除、複製、明細多選刪除/重排、主檔/明細驗證及 Confirm/Close/Apply 流程已補齊。
- [x] Def 表單加入建立/修改稽核 GroupBox，按鈕順序 Confirm/Close/Apply；明細操作按鈕移至清單右側並依 WinForms 排序。新增/複製 Apply 保持原模式。
- [x] Development 假資料只有 DEV build 且選取 Development preview 時啟用；Production/Test 走 API。修正明細刪除重複宣告，環境/API 切換關閉編輯器並取消舊明細請求。
- [x] 更新 `docs/modules/EQP/設備設定/設備點檢表設定.md`、規格索引及家用端 EP107 API TODO；既有兩表 PostgreSQL CREATE SQL 已存在，本輪沒有新增資料表或 DDL。
- [ ] 家用端逐欄比對兩張 YMES 表、實作主從原子 CRUD/options API；完成後公司端 pull 對接並核對 DataStamp/稽核欄位。
- [x] EP301「載具分類資料」公司端 UI、規格、DDL 與 API handoff 已推送；Production schema/API 仍由家用端待辦追蹤。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP301 載具分類資料公司端進度（2026-11-16）

- [x] EP107 推送後重新 pull，確認 GitHub `main` 已同步於 `f6cb3d9`；來源位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierCategory`。
- [x] 直接讀取 WinForms 主/Def Form、Designer、`wsEQP`、`clsCarrierBasis`；記錄狀態查詢、按鈕、批次作用列、核准工作流、Delete 0/-1 實刪/2 停用/1 拒絕、DataStamp、Audit 與表單尺寸。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 及本機受保護設定唯讀取得 MESTEST.TBLEQPCARRIERCATEGORY DDL、欄位、constraint 與 index；建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL。8 欄，PK 唯一索引，無其他 index/FK。
- [x] 新增 Vue DEV 頁、Development-only 假資料、三層載具分類選單與 EP301 route。Production API 呼叫契約及 Issue/Job 簽核複雜度已記錄，DEV 核准明確簡化為直接 0→2。
- [x] 建立 `docs/modules/EQP/載具/載具分類資料.md`，更新 `docs/DATABASE_CONNECTION.md`、規格索引及 `todolist_homepc.md` API/schema 交接。
- [ ] 家用端確認 YMES schema/migration、實作 Production CRUD/簽核 API；公司端 pull 後對接並核對部分成功、DataStamp 與狀態轉換。
- [x] 已開始 `CARRIER TYPE` / EP302「載具類別資料」；進度見下方 EP302 區塊。
- [ ] 未執行 build 或測試；Production API 未建置。

## 2026-11-16 ENT ET101 客戶基本資料公司端交接

- [x] 開始前 pull GitHub `origin/main`；當時最新提交為 `c6c7895`，工作樹乾淨。
- [x] 追查 `frmCustomerBasis`、`frmCustomerBasisDef`、Designer、ET_ETDataImport、`clsCustomerBasis` 與 `wsENT`，記錄清單欄位、表單欄位/鎖定規則、批次刪除/核准、Customer BASIS 簽核與 Excel 匯入流程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀查詢 `TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR` 的 DDL、NULL/default、PK、索引/FK；沒有查客戶業務資料或執行 Oracle DDL。
- [x] 新增 Vue DEV 頁 `web/src/components/CustomerBasisMaster.vue`、FunctionNo `CUSTOMER BASIS` 路由 `/YMES/Customer/Basis` 及 fallback 三層選單；Production 經 API，Development 使用標明的 localStorage 假資料。
- [x] 新增 `docs/modules/ENT/客戶基本資料.md`、Oracle source logical DDL、PostgreSQL 空表 CREATE SQL、規格索引、DB 連線查詢紀錄及家用端 migration/API/簽核/匯入待辦。
- [ ] 未執行 build/test 或瀏覽器人工驗收；待後續依要求驗證。
- [ ] 依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續下一個未遷移 Function；EQP AutoLoader 原始碼已確認並完成 DEV 頁，避免再標成 binary-only。

## EP302 載具類別資料公司端進度（2026-11-16）

- [x] 從 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierType` 讀取 Form/Def/Designer；讀取 `wsEQP.Load/Add/Edit/Delete/ApproveCarrierType` 與 `clsCarrierBasis` CRUD/query 實作。
- [x] 使用 PowerShell 5.1 + ODP.NET、本機受保護設定，只讀查詢 MESTEST.TBLEQPCARRIERTYPE 的 DBMS_METADATA DDL 與欄位/constraint/index 字典。13 欄，PK 唯一索引，無外鍵或額外索引。
- [x] 建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL；UI 規格記錄 UseLayer 0/1、已核准 CarrierCategory 查找、NUMBER(3,0) 數值限制、狀態和 Issue/Job 核准流程。
- [x] 新增 Vue DEV 頁、carrier type route 與主選單 FunctionNo 映射；DEV 核准明確簡化 0→2，分類查找只列已核准類別。
- [ ] 家用端確認 YMES schema/migration、實作 CRUD/分類選項/正式核准 API；公司端 pull 後對接與核對。
- [ ] 下一個公司端功能依既有全部模組待辦順序續做；先以 WinForms Form/Designer/Service 判斷功能完整範圍，結構缺漏時用 PowerShell 5.1 直連 Oracle 查詢。
- [ ] 未執行 build 或測試；Production API 未建置。

## EP303 載具基本資料公司端進度（2026-11-16）

- [x] 先 pull GitHub main，確認最新基準 a1fa7cd；工作樹初始乾淨。
- [x] 依序核對 frmCarrierBasis/Def、frmBasisTemplat、wsEQP、clsCarrierBasis；完成清單欄位、唯讀/選擇器、狀態篩選、Add/Edit/Copy/Delete/Approve、DataStamp、WIP 同步及簽核規則規格。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護設定唯讀查詢 TBLEQPCARRIERBASIS、TBLWIPCarrierState、TBLSMDFactoryBasis 的 DDL/欄位/約束/索引；未查詢業務資料，未在 Oracle 執行 DDL。
- [x] 建立主表與依賴表 Oracle source DDL、PostgreSQL 空表 CREATE SQL；更正 first-phase SQL 的 DEFAULT，移除這三表未證實欄位的猜測註解。
- [x] 新增 Vue DEV 畫面與 /YMES/Equipment/CarrierBasis 路由，三層選單 FunctionNo 為 CARRIER BASIS；加入三個選項視窗、KD 黃色欄位、Apply/Audit 與批次處理。
- [x] 建立 docs/modules/EQP/載具/載具基本資料.md，更新規格索引、資料庫連線記錄及家用端 API/DDL 待辦。

## EP401 設備參數資料公司端進度（2026-11-16）

- [x] 開始前 pull GitHub 最新 main；遠端已是 `4628cea`，無家用電腦新提交。
- [x] 追查 `frmEQPRecipeBasis`、Basis/Detail Define、wsEQP 與 clsEQPBasis/kcBasis_UPD，核對雙 UltraGrid、欄位/按鈕、狀態限制、簽核、刪除與 Copy/Version 明細實際複製欄位。
- [x] Oracle DDL 直接查詢 MESTEST；明確以 Windows PowerShell 5.1 啟動 Oracle.ManagedDataAccess。查詢 `TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL` 的 DDL、NOT NULL/default、PK、索引/FK，並查明 Type=-1/0/1 對應 NONE/TYPEVALUE/RANGE。沒有查 MES 業務資料或在 Oracle 執行 DDL。
- [x] Vue DEV 頁 `web/src/components/EquipmentRecipeBasisMaster.vue`：雙清單、狀態篩選/RecipeGroup 查詢、主檔 Add/Edit/Copy/Delete/Approve/Version、明細 Add/Edit/Delete、ElementType=9 本地選擇器、KD 黃色表單與本機假資料；Production 顯示 API 尚未建置，不使用假資料寫入。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL，並校正 first-phase Recipe Basis/Detail 的欄位 DEFAULT/NOT NULL/額外索引。
- [x] 建立 `docs/modules/EQP/設備設定/設備參數資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 與 `todolist_homepc.md`。
- [ ] 尚未執行 build/test 或瀏覽器人工驗收；Production schema/API/簽核仍由家用電腦依交接清單完成。
- [ ] 尚未以瀏覽器人工驗收；未執行 build 或測試。家用端 API/schema 完成後，pull 最新版並對接正式端點。
- [ ] 後續繼續依 docs/MES_MENU_FUNCTION_INVENTORY.md 逐功能復刻；每個功能先讀 WinForms Form/Designer/Service，必要時以 PowerShell 5.1 直連 Oracle 查 DDL，完成後同步規格與兩台電腦待辦。

## ENT ET102 客戶連絡人資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` pull；遠端已是 `f5dce82`，工作樹乾淨。
- [x] 核對 `frmCustomerContactor`、`frmCustomerContactorDef`、`wsENT.Load/Add/Edit/DelCustomerContactor`、`kcENT.clsCustomerBasis`：客戶查找、欄位、排序、多選刪除、Copy、Apply/Confirm、主鍵唯讀等規則已寫入功能規格。
- [x] 沿用 ET101 直連 Oracle 已取得的 `TBLENTCUSTOMERCONTACTOR` DDL；沒有重複查詢、沒有增加第二份 Create SQL，也沒有讀取 MES 客戶業務資料。
- [x] 新增 Vue DEV 畫面 `web/src/components/CustomerContactorMaster.vue`，路徑 `/YMES/Customer/Contactors`、FunctionNo `CUSTOMER CONTACTOR`，Development localStorage 假資料含客戶選擇、CRUD/Copy/多筆刪除及稽核欄位。
- [x] 新增 `docs/modules/ENT/客戶連絡人資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 及本家用端 API 待辦；沿用 ET101 的既有 `database/reference/mestest_customer_basis_create.sql`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端建立。
- [ ] 下一段依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 往後核對未遷移的 ENT/EQP 功能與其 WinForms 程式；缺少 DDL 時直接以 PowerShell 5.1 + ODP.NET 查詢 Oracle 結構。

## ENT ET106 客戶出貨地址資料公司端進度（2026-11-16）

- [x] 依序核對 `frmCustomerShipAddr`、`frmCustomerShipAddrDef`、`frmCustomerShipDeclarationDef`、ENT WebService/Core，確認主畫面雙 Grid、只選已核准客戶、按客戶載入全部報關列、出貨地址表單與報關子功能規則。
- [x] 出貨地址表 `TBLENTCUSTOMERSHIPADDR` 沿用 ET101 已直連取得的 DDL；缺少的 `TBLENTCUSTOMERSHIPDECLARATION` 已使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線直接唯讀查得。
- [x] 報關表原始 DDL 有 13 欄及 8 個 NOT NULL constraint；資料字典沒有 PK、unique/一般索引或 FK。已將 Oracle logical DDL 與不加 PK 的 PostgreSQL 空表 CREATE SQL 追加到客戶 DDL 檔案。
- [x] 新增 Vue DEV 雙 Grid 頁 `web/src/components/CustomerShipAddressMaster.vue`、路徑 `/YMES/Customer/ShipAddresses`、FunctionNo `CUSTOMER SHIP ADDRESS`；含已核准客戶選擇、地址/報關 CRUD、報關預設值、Taker/Forwarder/Broker DEV 選擇器、稽核欄位與 KD 黃色表單。
- [x] 建立 `docs/modules/ENT/客戶出貨地址資料.md`，更新規格索引、DB 連線記錄及家用端 migration/API 待辦。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 下一功能已依選單開始 ET103 報關行基本資料；最新狀態見下方 ET103 區塊。

## ENT ET103 報關行基本資料公司端進度（2026-11-16）

- [x] 開始 ET103 前已完成 ET106 並推送 `49fbd9c`；其後讀取 `frmBrokerBasis`、`frmBrokerBasisDef`、`wsENT`、`kcBasis_Query`、`kcBasis_UPD`、`kcENT.clsCustomerBasis`。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從先前提供的受保護連線直接唯讀查詢 `MESTEST.TBLENTBROKERBASIS` DDL、欄位 NULL/default、PK、索引/FK；未查業務資料、未執行 Oracle DDL，憑證未輸出/存檔。
- [x] 查得 13 欄、BrokerNo 單欄 PK/唯一索引，無 FK/其他索引。補 Oracle source DDL 與 `ymes.tblentbrokerbasis` PostgreSQL CREATE SQL，並校正 first-phase reference 中此表遺漏的三個 DEFAULT。
- [x] 新增 `web/src/components/BrokerBasisMaster.vue`、`/YMES/Customer/Brokers` 路徑與 `BROKER BASIS` 對應；DEV 假資料支援狀態篩選、搜尋、CRUD、停用/刪除、Apply/Confirm、稽核及直接核准預覽。
- [x] 新增 `docs/modules/ENT/報關行基本資料.md`，記錄 WinForms 版面、欄位限制、狀態刪除規則、`BROKER BASIS` 簽核流程及 YMES API 契約；更新規格索引、DB 連線記錄及家用端待辦。
- [ ] 家用端比對 YMES schema/migrations，依 DDL 只新增缺少的表；實作 Production CRUD、DataStamp、IssueState 軟刪除/硬刪除與既有簽核服務 API。
- [ ] 本次未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 後續已依 ENT FunctionOrder 開始 ET104「貨運行基本資料」；詳細進度與待辦見下方 ET104 區塊。

## ENT ET104 貨運行基本資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，遠端已是 `8d87cd8`；前一段 ET104 DDL 草稿保留。
- [x] 核對 `frmForwarderBasis`、`frmForwarderBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveForwarderBasis` 與 `kcBasis_Query`、`kcBasis_UPD`：狀態篩選、多選操作、欄位/唯讀、Broker 選擇、Apply/Confirm、狀態2舊服務缺陷都記入功能規格。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線唯讀取得 `MESTEST.TBLENTFORWARDERBASIS` DDL、欄位 nullable/default、PK/index/FK；只查結構，未查業務列、未在 Oracle 執行 DDL，憑證未輸出。
- [x] Oracle 原始 logical DDL 與 PostgreSQL `ymes.tblentforwarderbasis` 空表 CREATE SQL 已加入客戶主檔 reference；同步校正 first-phase reference 的 DEFAULT 與欄位註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ForwarderBasisMaster.vue`、`/YMES/Customer/Forwarders` 路徑、`FORWARDER BASIS` 對應；含狀態篩選、動態選列、多選、CRUD、停用/刪除、核准預覽、稽核及只顯示已簽核報關行的選擇器。
- [x] 新增 `docs/modules/ENT/貨運行基本資料.md`，更新功能索引、DB 連線記錄、ET103 報關行 lookup query 規格與家用端 ET104 schema/API/簽核 TODO。
- [ ] 家用端比對 YMES schema/migrations；只新增缺漏表或欄位，實作 ET104 Production CRUD、BrokerNo=已簽核報關行驗證、DataStamp、狀態刪除及 `FORWARDER BASIS` 簽核 API。
- [x] ET103 radio/checkbox KD 黃色 accent 與簽核完成後 audit/DataStamp 規格修正已包含於 ET104 提交 `39b2973`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] ET104 與 ET103 收尾修正已提交並推送 GitHub：`39b2973 feat: add forwarder basis UI`。
- [x] 後續已依 ENT FunctionOrder 開始 ET105「取貨公司基本資料」；最新進度見下方 ET105 區塊。

## ENT ET105 取貨公司基本資料公司端進度（2026-11-16）

- [x] 依 WinForms `frmTakerBasis`、`frmTakerBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveTakerBasisIssuestate` 與 `kcENT.clsCustomerBasis` 核對狀態、欄位、按鈕、稽核與來源版本不一致處。
- [x] Oracle DDL 原先不在 repo。以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線只讀取得 `MESTEST.TBLENTTAKERBASIS` 的 DBMS_METADATA DDL/欄位 NULL-default/constraints/indexes；只查結構，不讀業務列、不執行 Oracle DDL，連線值未輸出。
- [x] Oracle source logical DDL、PostgreSQL 空表 CREATE SQL 已追加 customer reference；first-phase SQL 預設值與猜測性欄位註解已修正。
- [x] 新增 Vue DEV 頁 `web/src/components/TakerBasisMaster.vue`、路徑 `/YMES/Customer/Takers` 與 `TAKER BASIS` 路由；含狀態篩選、即時選列、多選 CRUD、停用/刪除、核准預覽、KD 黃色欄位與稽核。
- [x] 新增 `docs/modules/ENT/取貨公司基本資料.md`，更新功能索引、DB 連線記錄與家用端 API/schema TODO。
- [ ] 家用端先比對 YMES schema/migrations；缺少才新增 Taker table migration。實作 GET/POST/PUT/DELETE/approve、DataStamp compare-and-swap、稽核與 `TAKER BASIS` 共用簽核服務。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 待家用端完成。
- [x] 下一項 ET201 已開始；完成狀態見下方 ET201 區塊。

## ENT ET201 外包商基本資料公司端進度（2026-11-16）

- [x] 本次先 pull `origin/main`，確認 GitHub 最新版已同步且工作樹乾淨。
- [x] 對照 `frmSubcontractor`、`frmSubcontractorDef`、`frmSubcontractorContDef`、wsENT 與 clsCustomerBasis；記錄雙 Grid、按鈕、欄位、主從刪除、複製、套用與 Issue/Job 簽核流程。
- [x] Oracle 兩張表結構已由既有 PowerShell 5.1 + ODP.NET 唯讀查詢取得；新增 source DDL 與 PostgreSQL 空表 CREATE SQL。未讀取業務資料，也未對 MES Oracle 寫入。
- [x] 新增 `web/src/components/SubcontractorBasisMaster.vue`，Development 使用獨立 localStorage 假資料，實作上下雙 Grid、狀態/編號查詢、多選、主檔/連絡人新增修改複製刪除、核准、Mail、KD 黃色欄位與稽核區。
- [x] 加入 `/YMES/Customer/Subcontractors` 路由及 FunctionNo `SUBCONTRACTOR BASIS`；建立外包商規格並更新模組索引、Oracle 連線紀錄及家用端待辦。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/migration/Issue service 仍待家用端完成。
- [x] 後續依 ENT Function inventory 開始 ET202 `ENTOSITEM` 外包項目設定；以下為完成紀錄。

## ENT ET202 外包項目設定公司端進度（2026-11-16）

- [x] 開始前執行 `git pull --ff-only origin main`；GitHub `main` 已是最新版。
- [x] 對照 `frmENTOSItem`、`frmENTOSItemDef`、wsENT、`clsCustomerBasis`，記錄查詢列、Grid 欄位/寬度、Add/Edit/Delete/Copy/Close、表單欄位驗證、Apply/Confirm 與外包流程選取條件。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀查到 `MESTEST.TBLENTOSITEM` DDL、欄位 nullable/default、constraints、PK/index；未讀業務資料、未在 Oracle 執行 DDL、憑證未輸出。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL、ET202 模組規格與規格索引/連線查詢紀錄。
- [x] 新增 Vue DEV 頁 `web/src/components/ENTOSItemMaster.vue`，路徑 `/YMES/Customer/OSItems`、FunctionNo `ENTOSITEM`；含單 Grid、列選取、多選刪除、Add/Edit/Copy、KD 黃色欄位、Audit、Apply/Confirm、外包流程 lookup 與 localStorage 假資料。
- [ ] 家用端比較實際 YMES schema/migrations；缺表時依 `database/reference/mestest_entositem_create.sql` 建 migration，不得以 IF NOT EXISTS 代替欄位差異檢查。
- [ ] 家用端實作 ENTOSITEM GET/POST/PUT/DELETE、DataStamp CAS、伺服器 audit、ProcessClass=3 + IssueState=2 流程驗證及權限檢查；遵守 API/DB 分離，Vue Production 不可使用假資料。
- [ ] 公司端尚未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [ ] ENT 八個 Function 已有 ET101/102/103/104/105/106/201/202 規格與 DEV 頁；下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續盤點下一個尚未完整復刻的 MES 功能，先核對 Form/Service/Core，再查 Oracle DDL。

## 2026-11-16 SMD SD103 儲位基本資料公司端進度

- [x] 從 GitHub main 快轉檢查最新版；本地與 origin/main 均為 3ab417d，沒有新提交。
- [x] 直接讀 WinForms 主畫面、定義窗、wsINV、clsInventory 與更新版 clsBasis_UPD；記錄四狀態篩選、欄位/按鈕、InventoryNo 選取、複合鍵、Copy、批次 Delete/Approve、IssueNo 與 DataStamp 版本差異。
- [x] 用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查 MESTEST.TBLINVLOCATORBASIS DBMS_METADATA 與資料字典；確認複合 PK、沒有 FK/額外索引。新增原始 Oracle DDL 和 PostgreSQL 空表 CREATE SQL，校正 first-phase bootstrap 預設值並刪除未核實欄位註解。
- [x] 新增 Vue DEV 頁 web/src/components/LocatorBasisMaster.vue，路由 /YMES/SMD/Locators、FunctionNo=LOCATOR BASIS；依 WinForms 具備查詢、狀態切換、選列、多選、Add/Edit/Copy/Delete/Approve、已核准庫存挑選器、KD 黃色欄位、稽核及 Apply/Confirm。
- [x] 新增 docs/modules/SMD/儲位基本資料.md；更新資料庫連線查詢紀錄、功能索引、家用端 API/schema handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；家用端需比對已部署 YMES schema 並補 forward migration/API 和正式 Issue/Job 核准流程。
- [x] SD107「生產線別基本資料」已依 Form/Service/Core 完成 DEV 頁、規格、Oracle DDL/PG CREATE 與家用端 API/schema handoff。下一個尚未完成的 SMD FunctionKey 為 SD108「設備狀態監控_區域」。
## 2026-11-16 SMD SD107 生產線別基本資料公司端進度

- [x] 直接對照 frmPDLineBasis / frmPDLineBasisDef、wsSMD、clsSMDBasis 與 clsBasis_UPD；整理欄位、按鈕、ValueList、四種 IssueState、Apply、批次操作、Delete 與 Issue/Job 核准邏輯。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀查得 TBLSMDPDLINEBASIS 完整 DDL、欄位 null/default、constraints、PK/index；未讀業務資料，未執行 Oracle DDL。
- [x] 新增 web/src/components/ProductionLineBasisMaster.vue，路由 /YMES/SMD/ProductionLines，FunctionNo=SMD PDLINE BASIS；Development 使用 localStorage 假資料，Production 只呼叫 API。
- [x] 新增 docs/modules/SMD/生產線別基本資料.md、Oracle source DDL 與 PostgreSQL 空表 CREATE SQL；修正 first-phase bootstrap 的來源 defaults 並移除猜測欄位註解。
- [x] 更新 SMD 規格索引、Oracle 連線查詢紀錄及 todolist_homepc.md API/schema handoff。
- [ ] 家用端 2026-11-17 app-browser 驗收：主 menu 可登入；SMD → 廠區設定 → 生產線別基本資料仍開到「尚未遷移」占位畫面。Production Web 尚未佈署公司端的 Vue 主檔頁。
- [ ] 修復 `web/src/components/AccessoryTypeMaster.vue` 第 52、59 行的 TypeScript 語法錯誤（`row.description||:` 缺少 `||` 後的運算元），再執行 Web build；目前 `npm.cmd run build` 因 TS1109 失敗，阻擋 Vue 主檔部署。
- [ ] `ProductionLineBasisMaster.vue` 已在 main；待 Web build 通過後通知家用端以 SSH 只更新 `ymes-web`，再驗收 Production 頁與 API 串接。家用端 API 健康檢查 200；登入後 GET `/api/smd/pd-line-basis` 回 `[]`；正式 `/options` 回 503，等 ValueList handoff；Approve 也等 Issue/Job handoff。
- [ ] 後續 SMD 頁面：SD108 設備狀態監控_區域、SD109 工位基本資料；依序讀 WinForms Form/Designer/Service/Core，再查缺少的 Oracle DDL。
- [ ] 請以 Oracle ValueListProvider 同一來源查 `TBLSYSVALUELIST` 中 `TABLENAME='TBLSMDPDLINEBASIS'`、`FIELDNAME='PDLINESTATUS'` 的正式值、排序與顯示文字，並交付繁中資源翻譯；不要以 Vue DEV 標籤或舊註解代替。
- [ ] 供家用端實作 Production Approve：核實 Issue/Job/IssueDetail/IssueJobMap 等簽核資料表的 Oracle DDL、資料欄位與 Service 建立流程；目前 YMES 尚無 Issue/Job 表，不能用 DEV 的直接 0→2 模擬代替。

## SMD SD108、SD109 公司端交接（2026-11-17）

- [x] 開始前 pull GitHub main，遠端新版本為 09dfd65；包含家用端 SD106 過帳電腦區域 migration/API 及 SD104/SD105 schema handoff，已一併納入。
- [x] 依 WinForms frmAreaShow、frmAreaShift、frmAreaShiftDef、frmSMDPDLinePositionBasis、frmSMDPDLinePositionBasisDef 與 clsSMDBasis 核對畫面、事件、班別登入狀態檢查及複合鍵。
- [x] SD108 區域圖、縮放/網格、右鍵狀態/歷史、雙擊班別維護與班別人員增刪已做 Vue Development Preview；SD109 搜尋、複合鍵 CRUD/Copy、多選刪除、作業區與 eSOP 選擇、Apply/Confirm 已做 Vue Development Preview。
- [x] Oracle DDL 由本機受保護連線以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得；新增兩組 source DDL 與 PostgreSQL 空表 CREATE，並修正 first-phase SQL 中 SD109 defaults/猜測性欄位註解。
- [x] 完成 SMD SD108/SD109 規格、功能索引、DB 查詢紀錄與家用端 API/schema handoff。補明 nullable DataStamp 用 `dataStampIsNull=true` 做精確 NULL CAS，並修正 SD109 刪除請求漏傳版本值。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/schema 需家用端按 todolist_homepc.md 完成。
- [ ] 下一步依 MES_MENU_FUNCTION_INVENTORY 的 ModuleOrder/FunctionOrder 繼續找下一個尚未完整復刻的 Function（接續 MTL），先核對 WinForms Form/Designer/Service/Core，再查 Oracle DDL。


## 2026-11-17 MTL ML106 物料品管項目設定公司端交接

- [x] Pull GitHub main，確認遠端包含 ML102、ML105 最新公司端提交；之後開始 ML106。
- [x] 依 frmMaterialQCItemSet.vb/Designer、wsMTL.LoadMTLQCItem/UpdMTLQCItem、clsMTLBasis、clsQCBasis 核對 Form/Service/Core：前綴查詢與全 IssueState、三種啟用 QC catalog、雙清單多選/雙擊、按 Confirm 才保存、IQC 虛擬站及 QC bind log 副作用。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 既有保護連線，直接唯讀查 MESTEST 三個 QC catalog 與 TBLQCITEMBINDLOG DDL；新增 source DDL 與 PostgreSQL 空表 CREATE SQL。現有 TBLMTLQCITEM、TBLOPQCITEM 沿用已交付 reference SQL。
- [x] 新增 Vue DEV 頁 /YMES/MTL/MaterialQCItems，FunctionNo=MTL QCITEM；保持舊 GET /qc-items 類別範本契約，ML106 改走獨立 GET /qc-item-library；避免載入失敗以空集合覆寫。
- [x] 新增 ML106 規格、索引列、家用端 schema/API handoff。
- [ ] 沒有執行 build、test 或瀏覽器驗收。Production API/schema 尚待家用端，完成後公司端 pull 並逐項對接。
- [ ] 下一步按 MES_MENU_FUNCTION_INVENTORY.md MTL FunctionOrder 繼續下一個未完整遷移功能；不得把簡化 DEV 佔位頁算成完成。


## 2026-11-17 MTL ML103 物料基本資料公司端進度

- [x] Pull GitHub main，確認目前最新為 0e50278。
- [x] 依 frmMaterialBasis / frmMaterialBasisDef、WinForms Designer、wsMTL、clsMTLBasis 核對狀態篩選、MaterialNo 查詢、ValueList 欄位、供應商子 Grid、Add/Edit/Copy、多選 Delete/Approve、Property/QC/Import/GumHour 功能。
- [x] Vue 主 Grid 加入多選核取方塊；核准與刪除逐筆呼叫現有單筆 API，DEV 假資料執行相同狀態轉換及關聯假資料清理。已簽核 Delete 設為不使用，未簽核/不使用 Delete 實體移除。
- [x] Vue 已接上 options API 的 CountWay/CheckLotNo 選項與 GumHour 顯示參數契約；Production API 目前回傳空 CountWay/CheckLotNo，且尚未回傳 MATERIALMULTITIMECTRL/GumMaterialType。另發現現有 Approve API 尚未執行條件式 GumHour 檢查，詳見 todolist_homepc.md。
- [x] 更新 docs/modules/MTL/物料主檔.md 與規格索引；沿用已查 8 張物料表 Oracle DDL/PG CREATE，ML103 無新增資料表。
- [ ] 未執行 build、test 或瀏覽器驗收。Production options API 待家用端補齊來源 ValueList/參數後 pull 回公司端再對照。
- [x] ML104 物料群組資料及後續 FunctionOrder 中的 ML107「物料標籤對應設定」均已完成公司端 DEV 頁與規格；Production API/schema 交家用端。ML107 規格見本檔較後方區塊。

## 2026-11-17 MTL ML104 物料群組資料公司端進度

- [x] 對照 `frmMaterialGroup`、Designer、`frmMaterialGroupBasisDef`、`wsMTL`、`clsMTLBasis`：GroupNo 查詢、無 IssueState、主 Grid 欄位/排序、Add/Edit/Copy/Delete、DataStamp、Apply/Confirm 及下方雙清單替換式成員編輯已記錄。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀取得 `TBLMTLMATERIALGROUPBASIS` DBMS_METADATA DDL、欄位 Nullable/default、constraints、PK/index；確認兩個欄位以 CHECK constraint 強制 NOT NULL、DataStamp DEFAULT 0 nullable。未讀業務資料、未執行 Oracle DDL，連線秘密未輸出。
- [x] 新增 `database/reference/mestest_mtl_material_group_source_ddl.sql` 及 PostgreSQL 空表參考 `mestest_mtl_material_group_create.sql`；校正 first-phase 物料群組主檔 DEFAULT/NULL 與欄位註解，明細沿用已查來源 SQL。
- [x] 新增 `web/src/components/MaterialGroupMaster.vue`、三層選單 `MTL GROUP` 與 `/YMES/MTL/MaterialGroups` 路徑；DEV 假資料頁包含主 Grid、多選刪除、編輯/複製/套用、稽核欄、類別 lookup、同類已核准物料雙清單與整組儲存。
- [x] 新增 `docs/modules/MTL/物料群組資料.md`，記錄 Form/Service/Core、Oracle DDL、API 交易與 Vue 規則；更新索引、Oracle 連線查詢紀錄及家用端 API/schema handoff。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API/schema 仍待家用端完成。
- [x] ML107「物料標籤對應設定」已完成公司端 WinForms/Service/Core 對照、Oracle DDL 直查、Vue DEV 頁、功能規格及家用端 API/schema handoff，詳見本檔 ML107 區塊。

## 2026-11-17 MTL ML107 物料標籤對應設定公司端進度

- [x] 開始前 pull GitHub main，確認 YMES 已是 `71fef1e`，工作樹乾淨。
- [x] 對照 `frmMTLLabelBasis`、Designer、`frmMTLLabelBasisDef`、`wsMTL`、`wsPRD`、`clsMTLBasis`、`clsBasis_UPD`、`clsProduct`：主畫面 Filter/Like 條件、初始不查詢、Grid 欄位、四個資料操作按鈕、Add/Edit/Copy/Delete、LabelSource=5、MaterialNo=`*`、DataStamp/Audit、Apply/Confirm 與 lookup 限制。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線直接唯讀呼叫 DBMS_METADATA 並查 ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES/ALL_IND_COLUMNS；取得 `TBLMTLLABELMAP` 與 label lookup `TBLPRDLABELBASIS` DDL。只查結構，未讀 MES 業務資料、未執行 Oracle DDL；連線值未輸出。
- [x] 新增兩張表 Oracle source DDL、PostgreSQL 空表 CREATE SQL、`docs/modules/MTL/物料標籤對應設定.md`，並更新功能規格索引、Oracle 連線查詢紀錄及家用端 API/schema handoff。
- [x] 新增 `web/src/components/MaterialLabelMapMaster.vue`，加入 FunctionNo `MTL LABEL MAP BASIS` 三層選單及 `/YMES/MTL/MaterialLabelMaps` route；DEV 頁包含初始空 Grid、三組查詢/lookup、多選刪除、Add/Edit/Copy、LabelSource/物料 lookup、稽核資訊及 KD 黃色輸入樣式。
- [ ] 未執行 build、測試或瀏覽器驗收；Production schema/API 待家用端依 `todolist_homepc.md` 比對及完成。
- [ ] 下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 確認其餘未完成 FunctionNo，再讀 WinForms Form/Designer/Service/Core，查明缺少的 Oracle DDL 並續做 DEV UI。

## 2026-11-17 MTL MLB01 來料 AutoLoader 格式檔設定公司端進度

- [x] Pull GitHub main，從 `99c9fef` 開始；家用端先前已推送的 MTL ML107 內容已同步。
- [x] 直接對照 `frmMaterialVendorAutoLoaderSetup`、`frmMaterialVendorAutoLoaderSetupDef`、相關 Dynamic Parameter/Test File 表單、`wsMTL_Base`、`clsMTLBasis_Industry_Base`；記錄初始不查詢、精確 Folder/Vendor 條件、Grid 可見欄、Add/Edit/Delete/Copy/Query/Close、格式生成、基本檢查、Apply/Confirm、CLOB 與 DataStamp 原始碼差異。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直連 MESTEST 唯讀取得 `TBLMTLVENDORALSCRIPT` DBMS_METADATA DDL 與欄位/NULL/default/PK/index；無業務列查詢、無 Oracle 寫入，連線值未輸出。
- [x] 新增 Oracle source DDL、PostgreSQL 空表參考、MLB01 功能規格、Vue DEV 頁與三層 `物料資料 → 行業包 → 來料AutoLoader格式檔設定` 選單，路徑 `/YMES/MTL/MaterialVendorAutoLoaderSetup`。
- [x] Vue DEV 頁具有初始空 Grid、精確查詢、右側 WinForms 操作按鈕、多選逐筆刪除、唯讀 FormatScript 預覽、Add/Edit/Copy 定義窗、KD 淡黃輸入欄位、格式生成/檢查、CSV/TXT 預覽、動態測試參數、供應商/允許目錄/測試配方選擇器與 localStorage 假資料；Production 呼叫 API，不用假資料。
- [x] 更新 `todolist_homepc.md` 的 schema/API/parser handoff、`docs/DATABASE_CONNECTION.md` 及功能規格索引。
- [ ] DEV 測試檢查、build 與瀏覽器驗收未執行；使用者未要求測試。
- [ ] 家用端完成 YMES live schema 比對、migration/API 後，公司端 pull 並核對 Production 契約。
- [x] 2026-11-17 已完成 EQP `B_EQP AUTOLOADER SETUP` WinForms Form/Service/Core/DDL 對照、Vue DEV 頁及家用端 API/schema handoff；後續項目改從功能目錄中下一個尚未完成的 Function 選取。

## 2026-11-17 QC402 次分類基本資料公司端進度

- [x] 開工前 pull `origin/main`，確認 `0d16dac` 最新且工作樹乾淨。
- [x] 對照 `frmSClassBasis`/`frmSClassBasisDef`、`wsQC`、`clsHoldBasis`：主 Grid、MClassNo 已簽核 lookup、CRUD/Copy/Apply、四種狀態、DataStamp、`SCLASS BASIS` Issue Job 簽核及 `QC_SClassDataImport`/`SClassDataImporter` 規則。
- [x] Windows PowerShell 7 不相容舊版 ODP.NET 初始化；改用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定直連唯讀查 `MESTEST.TBLQCSCLASSBASIS` table DDL 及 `PK_TBLQCSCLASSBASIS` unique index DDL。只讀結構，沒有查業務列/執行 Oracle DDL，連線值未輸出。
- [x] 已建立 Oracle source DDL 與 PostgreSQL 空表 CREATE SQL；只保留 SClassNo PK，來源沒有 FK。MClassNo approved rule 由 QC402 API 查核，沒有在 SQL 中加 FK。
- [x] 已加入 Vue `/YMES/QC/SClassBasis` DEV 假資料頁、三層 QC 選單、`QC402` route、MClass 已簽核 lookup、主 Grid/表單、四狀態、多選刪除/核准及 SClassImport 預覽/逐組結果/取消 UI。
- [x] 已新增 `docs/modules/QC/次分類基本資料.md`，並更新規格索引、Oracle 查詢紀錄與 `todolist_homepc.md` API/schema handoff。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API/schema 依家用端交接完成後再 pull 核對。沒有新增 API implementation。
- [ ] 下一段依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續 QC 尚未完成 Function；每頁先讀 WinForms Form/Designer/Service/Core，再查缺少的 Oracle DDL 與實際 CREATE SQL/API handoff。


## QC202 計數管制內容（公司端 2026-11-17）

- [x] 已 pull 最新 main；公司端完成 QC202 Vue DEV 頁、三層選單與 WinForms / Service / Core 規格。
- [x] Oracle TBLSPCATTLIMIT 沿用 QC201 透過 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查 MESTEST 取得的 DBMS_METADATA/欄位/索引結果；新增 QC202 單表 source DDL reference，未讀業務資料、未在 Oracle 執行 DDL，未將連線秘密寫入 Git。
- [x] 建立 PostgreSQL JSONB 動態因子空表 CREATE reference；明確標示這是 YMES schema 適配，供家用端比對，不是可直接執行的 migration。
- [x] 後續接續 QC203 計數品管項目判別法則對應：已先讀 Form/Service/Core，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 從 MESTEST 唯讀取得三張專用表 DDL；完成 Vue DEV 畫面與 API/CREATE SQL handoff。

## 2026-11-17 QC203 計數品管項目判別法則對應公司端進度

- [x] 直接閱讀 QC_AttRuleMap 的四個 WinForms Form/Designer 程式、wsQC、clsQCBasis；追查狀態查詢、FactorOnline、RuleNo/ComponentCollect、RetestLot、Plan/Rule 選擇、抽樣參數、CRUD、Issue Job 與歷史刪除副作用。
- [x] 使用本機既有受保護連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接連至 MESTEST，唯讀取得 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER 的 DDL 與獨立索引；無業務列查詢、無 Oracle DDL 執行、連線秘密未輸出。
- [x] 新增 Oracle source DDL 與 PostgreSQL JSONB 因子值 CREATE 參考；標註三張 Oracle 表沒有 FK、serial sequence 是 YMES 建議，家用端需先比較 live schema 與共用 ID service。
- [x] 新增 /YMES/QC/AttributeRuleMap Vue DEV 頁與品質資料三層選單；完成已核准 QC Item 查找、四狀態、動態因子欄、主/Detail/Parameter 清單、Add/Edit/Copy/Delete/Approve、RetestLot/Plan/Rule/Parameter 編輯及獨立假資料。
- [x] 新增 QC203 功能規格並更新規格索引、Oracle 查詢紀錄、本待辦與家用端 API/schema handoff。舊 Core 刪除未刪 Parameter 的 orphan 行為和 YMES 完整交易清理差異已記錄。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/schema 待家用端完成後，公司端 pull 最新 main 再對接驗收。
- [x] 已於 2026-11-17 pull 最新 main，完成 QC204「刪除計數型品管項目量測記錄」Vue DEV 畫面、WinForms/Core 規格與 API handoff，使用既有 QC201 Oracle DDL reference，未新增表。
- [x] QC205「計量品管項目」已完成公司端 Vue DEV 頁與 API/schema 規格交接；進度與家用端待辦見本檔末段及 `todolist_homepc.md`。

## 2026-11-17 QC204 刪除計數型品管項目量測記錄公司端進度

- [x] 閱讀 frmQCItemDeleteForAttributes Form/Designer、wsQC Load/Delete methods、clsQCBasis Load/Delete Core；確認狀態範圍、Log 旗標、四種日誌刪除表、Tape Form 特例及歷史成功訊息被包裝成失敗的行為。
- [x] 沿用 `database/reference/mestest_qc_item_attributes_source_ddl.sql` 已有 QC Item master/BasisLog/DetailLog/EditLog/Tape DetailLog Oracle DDL；確認本功能不新增表，因此不另造 CREATE SQL，也未再次連 Oracle。
- [x] 新增 `/YMES/QC/AttributeMeasurementDelete` Vue DEV 頁、QC204 品質資料三層選單路由、操作確認、多選逐筆刪除與假資料回饋。
- [x] 新增 QC204 功能規格、模組索引、資料庫紀錄，以及家用端 log schema/API handoff。
- [ ] 尚未執行 build、測試或瀏覽器驗收；等待家用端 API/schema 完成後回公司端 pull 驗收。
- [x] QC209 的 WinForms Form/Designer/Service/Core 已核對，UI 與 API/schema handoff 已完成；詳本檔末段。
## QC205 計量品管項目公司端進度（2026-11-17）

- [x] 已 pull 最新 `origin/main`（本次 Already up to date），基於目前 THEIL BU1 WinForms 客製版直接讀主表、Define、Memo Forms、Designer、wsQC/Core 與共用 Parameter/Factor/OP/Simulation/SamplePlanChange 實作；非僅依截圖。
- [x] 已新增 `/YMES/QC/VariableQcItem` Vue 3 DEV 頁，含主 Grid、四 IssueState、CRUD/Copy/Delete/Approve、欄位驗證、Factor/Parameter/OP/Simulation/SamplePlanChange/Memo 視窗、假資料；複製會比對 QCItem/目標 Plan 參數並提供 WinForms 來源選擇器。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護設定直連 MESTEST，只讀取得 Memo/變數量測/引用檢查所需 Oracle DDL；不讀業務列、不執行 Oracle DDL、不輸出或提交連線秘密。四個 Oracle 舊刪除目標在 MESTEST 不存在，已記錄。
- [x] 已新增 Oracle source DDL、Memo PostgreSQL Create 參考、`docs/modules/QC/計量品管項目.md`、索引及 DB 連線查詢紀錄，並在 `todolist_homepc.md` 寫明 API/schema handoff。
- [ ] 尚未執行 build、測試或瀏覽器驗收；家用端完成 PostgreSQL schema/API 後，公司端 pull 最新 main 並做 Production API 對接驗收。
- [x] QC209「計量品管項目管制法則對應」公司端 Vue DEV 頁、規格與 Create/API handoff 已完成，見本檔末段。

## QC209 計量品管項目管制法則對應公司端進度（2026-11-17）

- [x] 已直接閱讀 `STD/MESWin/QC/QC_ItemRule/frmQCItemRule.vb`、`frmQCItemRuleDef.vb`、wsQC 與 `clsQCBasis`：確認 QC Item lookup 預設已核准、SPCID 依 ChartType、候選規則只載已核准、四個 Action 分類、SPK 停用 Wait、Confirm/Apply 完整替換整組映射。
- [x] Oracle `TBLQCITEMRULE` DDL/PK 沿用 QC205 之前由 PowerShell 5.1 + Oracle.ManagedDataAccess 直接查得的 `mestest_qc205_variable_dependencies_source_ddl.sql`；`TBLQCRULEBASIS` 沿用 QC108 source DDL。已找到，不需重新查 Oracle，也未使用 DBeaver UI。
- [x] 建立 `/YMES/QC/VariableItemRuleMap` Vue 3 DEV 頁、QC209 route/三層 fallback menu、獨立 mock 資料、QC Item 查詢、四分類多選移動、ChartType/SPCID 與 Apply/Confirm。
- [x] 新增 `mestest_qc209_variable_item_rule_map_create.sql`、QC209 功能規格、模組索引、Oracle 查詢紀錄與 `todolist_homepc.md` API/schema handoff。
- [ ] Production API/schema 待家用端完成；本頁未執行 build、測試或瀏覽器驗收。家用端完成後公司端 pull 最新 main，以 QC209 Production API 驗收。
- [x] QC210「刪除計量型品管項目量測記錄」公司端 Vue DEV 頁、WinForms 規格、Oracle DDL 來源引用與 PostgreSQL Create/API handoff 已完成，詳本檔末段。
- [ ] QC 頁面 Production API/schema 待家用端完成。公司端未執行 build、測試或瀏覽器驗收；家用端完成後 pull 最新 main 驗收，再按 MES_MENU_FUNCTION_INVENTORY.md 繼續尚未遷移功能。

## QC210 刪除計量型品管項目量測記錄公司端進度（2026-11-17）

- [x] 已直接讀取 QC_ItemDeleteForVariables Form/Designer、wsQC Load/Delete Service、clsQCBasis Load/Delete Core；確認 IssueState -1、0 清單、Visible Grid columns、Log=Log 分支和逐筆多選操作。
- [x] 已核對保存的 MESTEST Oracle DDL：計量量測 log 來源沿用 QC205/201 的直接唯讀 DDL reference；本次未重連 Oracle，也未查看業務列或使用 DBeaver。
- [x] 已新增 /YMES/QC/VariableMeasurementDelete Vue 3 DEV 頁，含 QC210 三層選單、-1/0 清單合併、WinForms 欄位與 ValueList、點列/多選、確認刪除、逐筆結果及獨立 DEV mock。表頭與選取色使用黃橘色系。
- [x] 已新增 QC210 功能規格及 PostgreSQL Create SQL 參考，並更新 MODULE_REPLICATION_SPEC_INDEX.md、docs/DATABASE_CONNECTION.md 與 todolist_homepc.md。
- [ ] 家用端尚未完成 QC205/209/210 Production API/schema；公司端未做 build、測試或瀏覽器驗收。

## QC212 計量品管項目群組公司端交接（2026-11-17）

- [x] pull `origin/main`（Already up to date），依 Customer/Production THEIL BU1 原始碼閱讀群組主 Form、Define Form、Memo Form/Designer、共用 Include/Parameter/OP/Calculate Script 子視窗，以及 `wsQC`、`clsQCBasis`；依據不是截圖。
- [x] 完成 `/YMES/QC/VariableItemGroup` Vue 3 DEV 畫面與三層選單，包含四狀態/GroupNo 查詢、上下主/明細 Grid、Add/Edit/Delete/Copy/Approve、雙清單 Add QC/順序、Remove QC、Parameter、Op、Calculate Script、Memo；使用獨立 localStorage 假資料及 KD 黃橘配色。
- [x] 主/明細/核准/刪除/Copy/Memo 的實際限制與 legacy 差異已寫入 `docs/modules/QC/計量品管項目群組.md`。已整理 API handoff，並記錄 QCITEM_G Issue Job、WIP 刪除檢查、DataStamp 與 Memo Type=1。
- [x] Oracle DDL 沿用既有 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀 metadata 擷取：主表、明細、計算腳本、Memo 及共用表都已有 Oracle source reference；本次未再次連 Oracle、未讀業務資料、未在 Oracle 執行 DDL。
- [x] 新增 `database/reference/mestest_qc212_item_group_create.sql`，供家用端核對並補缺的 `tblqcitemgroupdetail`、`tblqccalculatescript` PostgreSQL 建表參考；主表與 Memo 既有 Create reference 已在規格中引用。
- [x] 更新 `todolist_homepc.md` 最新 QC212 schema/API handoff、`docs/DATABASE_CONNECTION.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md`。
- [ ] 公司端未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收；家用端完成 migration/API 後公司端 pull 再驗收。下一項依目錄為 QC213 計數品管項目群組。

## QC213 計數品管項目群組公司端交接（2026-11-17）

- [x] 依 STD WinForms frmQCItemAttOrderGroup/Define、共用 QC_ItemGroupInclude（IsAttributes=True）、QC_ItemOp（QCType=3）、wsQC 與 clsQCBasis 核對欄位、按鈕、狀態、刪除/簽核規則；沒有用截圖代替原始碼。
- [x] 新增 /YMES/QC/AttributeItemGroup Vue 3 DEV 頁並掛到品質資料 > 品管設定 > 計數品管項目_群組。包含四狀態及 GroupNo 查詢、上下 Grid、Add/Edit/Delete/Copy/Approve、雙清單 Add QC、移除/排序及共用 OP 對應欄位；使用獨立 localStorage 假資料與 KD 黃橘色系。
- [x] 依 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀取得 QC213 主/明細及硬刪 OP/PRD 依賴 DDL/欄位/PK/index；WIP 檢查表沿用既有來源 DDL。沒有查詢 MES 業務列、沒有在 Oracle 執行 DDL，連線資料未輸出。
- [x] 新增 Oracle source DDL reference、含 WIP/OP/PRD 關聯表的 PostgreSQL schema-only CREATE 參考、QC213 功能規格、DATABASE_CONNECTION.md 與 MODULE_REPLICATION_SPEC_INDEX.md 更新。
- [x] 更新 todolist_homepc.md：先完成既有 QC212 API/schema，再依 QC213 handoff 比對所有主/明細/硬刪依賴及 YMES live schema，僅補缺 migration，實作 QC213 API/Issue Job/共用 QCType=3 operation。
- [ ] 公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。家用端完成 migration/API 後 pull 再做整合驗收；之後依 MES_MENU_FUNCTION_INVENTORY.md 接下一個未遷移功能。

## 2026-11-18 QC409 OCAP異常單處置調整公司端進度

- [x] 已直接閱讀 STD/MESWin/QC/QC_OCAPDispositionAdjust/frmOCAPDispositionAdjust.vb、Designer、frmOCAPProcessDraw.vb、wsQC.LoadERFJoinOCAPDisposition、wsWIP.Exe_ERF_Disposition 與 clsLot.UpdOCAPAndERF_Disposition/OCAP helper。已記錄兩種動作、查詢條件、單筆作用列、同 ERF 多 Hold Item 的最終結案差異、LotStamp、WIP/OCAP side effects。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 使用既有受保護 MESTEST 設定，唯讀取得三張 OCAP 專用表 DDL 與 11 張共用 ERF/WIP/通知相依表 DDL/索引；未讀業務資料、未在 Oracle 執行 DDL，未輸出或提交連線秘密。
- [x] 已建立 /YMES/QC/OCAPDispositionAdjust Vue DEV 頁、QC409 menu/route/元件掛載、功能規格、Oracle DDL reference、三張專用表 PG CREATE-only handoff，更新 module index、DATABASE_CONNECTION.md 及家用端 API/schema TODO。
- [ ] Production API/schema 尚未實作；公司端未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收。家用端完成 QC409 API/schema 後 pull，對照真 API/錯誤與交易行為整合驗收。
- [ ] 接續依 docs/MES_MENU_FUNCTION_INVENTORY.md 遷移下一個尚未完成的 Function；各功能先追 WinForms Form/Service/Core，再查缺的 Oracle DDL，完成 DEV UI/規格/API handoff 並 push。

## 2026-11-18 公司端進度：OP103 作業站類別資料

- [x] 直接對照 frmOPType/Designer、frmOPTypeDef/Designer、wsOP.Load/Add/Edit/Delete/ApproveOPType 與 clsOpBasis；記錄四狀態查詢、Grid 隱藏狀態/DataStamp、定義窗欄位、OP namespace 衝突、Property cascade、IssueNo 與狀態轉移。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護連線唯讀取得 MESTEST.TBLOPTYPE 原始 DDL/index；沒有讀業務資料、沒有在 Oracle 執行 DDL、未輸出連線秘密。完成 Oracle source DDL 和 PostgreSQL CREATE-only 參考。
- [x] 新增 /YMES/MasterData/OperationTypes Vue DEV mock UI 與 OP → 作業站設定 → 作業站類別資料路由、四狀態/查詢/清單/新增/修改/複製/刪除/核准/Property，KD 黃色欄位與 WinForms 版型。
- [x] 新增 OP103 規格、模組索引、資料庫連線 provenance、家用端 live schema/API handoff，並在 OP Basis 規格互連。
- [ ] Production API/schema 由家用端按 todolist_homepc.md 實作；未連 YMES PostgreSQL，未執行 build、test 或 browser 驗收。公司端持續依 docs/MES_MENU_FUNCTION_INVENTORY.md 下一個未遷移 WinForms 功能直接閱讀程式碼。

## OP101 作業站法則設定（公司端 2026-11-18）

- [x] 直接閱讀 `frmOPControlRule`/Def、`wsOP.Load/Add/Edit/DeleteOPControlRule`、`clsOpBasis` 與 `clsBasis_UPD.EditOPControlRule`；確認查詢 RuleNo、只顯示 RuleClass=0、無 IssueState/Approve、實體刪除及 DataStamp CAS。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護設定，只讀取得 `MESTEST.TBLOPCONTROLRULE` Oracle DDL、constraint/index metadata；僅 RuleNo PK unique index，無 FK/額外 index。存 Oracle source DDL 與 PostgreSQL CREATE-only 對照。
- [x] 新增 `/YMES/OP/ControlRules` Vue DEV 頁和 OP 三層選單。清單欄位、Add/Edit/Copy/Delete、定義欄位、Send Mail 群組 lookup、DataStamp/audit、legacy Apply payload 差異均已整理。
- [x] 新增 `docs/modules/OP/作業站法則設定.md`、模組索引、Oracle 連線查詢紀錄，以及本段家用端 API/schema handoff。
- [ ] 本段依工作規則未執行 build、test 或瀏覽器驗收；推送後下一項接續 OP102「企業邏輯設定」，先從 WinForms Form/Service/Core 原始碼開始。

## OP102 企業邏輯設定（公司端 2026-11-18）

- [x] 直接閱讀 frmBusinessRuleBasis/Def、wsOP、clsOpBasis、clsBasis_UPD；記錄三種 RuleMethod、plugin/interface/scenario options、四狀態 CRUD/簽核/刪除、DataStamp 與 Apply/Confirm。
- [x] Windows PowerShell 5.1 + Oracle.ManagedDataAccess 受保護連線唯讀取得 MESTEST.TBLOPBUSINESSRULEBASIS 與 TBLOPSCENARIOBASIS DDL/index/constraint metadata；各表只有 PK index、無 FK/額外索引；未讀業務列、未執行 Oracle DDL。
- [x] 建立 /YMES/OP/BusinessRules Vue DEV 假資料頁與 OP 設定三層選單、路由、狀態/查詢/多選/CRUD/核准、欄位條件、稽核展示。
- [x] 建立 OP102 詳細規格、兩張 Oracle source DDL 與 PostgreSQL CREATE-only 參考、索引與資料庫連線 provenance、家用端 API/schema handoff；補上作業站主檔規格交互連結。
- [ ] 按工作規則未執行 build、test 或 browser 驗收；Production API/schema 待家用端依 todolist_homepc.md 完成。家用端 push 後公司端 pull 對接。
- [ ] 接續依 docs/MES_MENU_FUNCTION_INVENTORY.md，直接閱讀下一項尚未遷移 WinForms Form/Service/Core，查缺漏 Oracle DDL，完成 UI/spec/handoff 並 push。

## 2026-11-18 公司端進度：OP105 作業站群組資料

- [x] 直接讀取 frmOPGroupBasis / frmOPGroupBasisDef / frmOPGroupDetailDef、wsOP、clsOpBasis、clsBasis_UPD；整理主檔/明細欄位、雙 Grid、ADD/EDIT/DEL/Copy/Check、DataStamp、已核准作業站與區段 lookup。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 MESTEST.TBLOPGROUPBASIS、TBLOPGROUPDETAIL DDL 與 ALL_TABLES/ALL_INDEXES/ALL_CONSTRAINTS；兩表僅 PK unique index，無 FK/額外 index。未查業務列、未在 Oracle 執行 DDL/DML。
- [x] 新增 /YMES/OP/Groups Vue DEV 假資料雙 Grid/表單/lookup/檢核並接上作業站資料→作業站設定→作業站群組資料選單。
- [x] 新增 OP105 規格、Oracle source DDL、PG CREATE-only SQL、校正 first-phase 新庫 DEFAULT 0，更新 module index、資料庫 provenance 與 todolist_homepc.md。
- [ ] 尚未連 YMES PostgreSQL、未實作 API/schema；未執行 build、test 或 browser 驗收。已列明 API 路徑與 live schema/defaults 核對項目給家用端。
- [ ] 推送 OP105 後依 docs/MES_MENU_FUNCTION_INVENTORY.md 接續下一個尚未完成 WinForms 功能；家用端 API/schema push 後 pull 並整合。

# 2026-11-18 辦公室端續作：產品主檔 PD102

公司端依 frmProductBasis_THEIL_BU1 建立產品專用 Vue DEV 頁並接入 product-master 路由；整理主表單、IssueState、Add/Edit/Copy/Version/Approve/Delete、Process/BOM/EQPConstraint、WinForms 服務邏輯與七張 Oracle 主/關聯表 DDL/字典。規格為 docs/modules/PRD/產品主檔.md，Oracle/PG 參照為 database/reference/mestest_prd_product_basis_*。Oracle ProductProperty 的 DBMS_METADATA 發生 ORA-03135，欄位/約束依字典重建並已標註。家用端 API/schema 未完成。

下一個公司端工作：
1. PD_ProductProcess、frmProductBOM_THEIL_BU1 的流程與 BOM DEV 子表單已完成；產品屬性 DEV 子表單也已完成，分別見 `產品主檔子表單.md`、`產品主檔產品屬性.md`。
2. Business Rule、OPDescription、RecipeGroup、Accessory DEV 已完成並有家用端 API/schema handoff；接續 Resource、SubOP、Program、Default、RunTime、QC Item、Packing Set、EQPCapItem、eSOP、EquipmentSpec、APS、Bin、GBOM Import/Export、Product Import。每項直接閱讀 WinForms Form/Designer/Core/Service，記錄欄位、狀態規則、資料表和服務；DDL 缺漏時用受保護 PowerShell Oracle 連線查字典，不可猜。
3. Product API/關聯 schema 由家用端接手後，回來核對 API contract 與 UI，避免公司端重複改 API。
4. 依 MES 全功能目錄繼續下一個待遷 Function；每一段維護規格索引/TODO，假資料限定 DEV。

頁面底部 Set ... 與 Expand All 帶入選取流程列上下文；BOM Set 開啟該產品 BOM 遷移入口。WinForms Designer 設 Visible=False 的 QTY Conversion 與 DisApprove 沒顯示。window.print() 只是產品包裝 BOM 報表的 DEV 簡化。正式模式目前能載現有產品/流程/BOM，其餘欄位與子功能仍待家用端補 API 契約。

## PRD PD102 產品流程／BOM 子表單續作（2026-11-18）

- [x] 直接對照 `STD/MESWin/PRD/PD_ProductProcess/frmProductProcess.vb`：544×462 視窗、MOType/PS/Process 查找、Current/Specified、Default 唯一、暫存 Add/Del、Apply/Confirm/Close 與 Net Flow 檢查。
- [x] 直接對照客製 `Customer/Production/MESWin/PRD/PD_ProductBasis_THEIL_BU1/frmProductBOM_THEIL_BU1.vb`：719×516 Tree/Node Define、右鍵節點權限、產品/材料版本、投入點、數量、損耗率、Specified、LOTCREATE、材料群組/替代料與遞迴樹。
- [x] 新增 `ProductProcessEditor.vue`、`ProductBomEditor.vue` 並接入 `ProductBasisMaster.vue`。DEV 假資料的流程與 BOM 可新增/刪除/修改/檢視並保存至產品專用 localStorage；正式 API 模式唯讀。LOOKUP 只用 DEV 假資料，不能當正式選項。
- [x] 新增 `docs/modules/PRD/產品主檔子表單.md`，更新家用端 API/schema 待辦。
- [ ] 產品主檔其他側邊子功能仍待閱讀各自 WinForms Form/Core/Service 後實作；不可用本次子表單完成狀態代表 PD102 所有側功能已完成。
- [ ] 未執行 build/test/browser 驗收。完成同步 YMES 後 commit/push，再依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 排定下一個未完成 FunctionNo。

## PRD PD102 產品屬性子表單續作（2026-11-18）

- [x] 直接閱讀 `frmProductProperty`/Designer、`frmProductPropertyDef`/Designer、`wsPRD` 及 `clsProduct`：四狀態、清單欄位與隱藏 DataStamp、ADD/EDIT/DEL/Copy/Approve、PropertyType=5 已核准 SYS Element 查找、MES Parameter 值選取、Apply/Confirm/Close、`PRD PROPERTY` 簽核 Job 分流。
- [x] 新增 `ProductPropertyMaster.vue`：684×486 清單、496×276 定義窗、MES/KD 黃色輸入、狀態篩選、多選/逐筆刪除、ACTIVE 標 UNUSED、Apply/Confirm、Property 與 Parameter DEV lookup、DEV localStorage。
- [x] 正式 API 模式唯讀；DEV 假核准採未配置簽核 Job 的直接 ACTIVE 情境。Controlled 僅為 SYS Element Basis 資訊，產品表不存 Controlled。
- [x] 新增 `docs/modules/PRD/產品主檔產品屬性.md`，更新 PRD 主規格/索引、`todolist_homepc.md` API/schema handoff。
- [ ] 未執行 build/test/browser；按工作規則僅做靜態檢閱。現有 TBLPRDPRODUCTPROPERTY Oracle DDL 由字典重建且已存在，家用端先查 YMES schema/migration，再決定 API mapping/migration。
- [x] PD102 `Parameter` 已依 WinForms Form/Designer、wsPRD、clsProduct 完成 DEV 子視窗及 Oracle 字典/SQL handoff，詳見本檔新增段落。

## PRD PD102 產品 Parameter Setup（公司端 2026-11-18）

- [x] 直接閱讀 `PD_ParameterSetup/frmParameterSetup.vb`、Designer、`wsPRD`、`clsProduct`。依實際右鍵旗標復刻 Product Process OP/All PS OP、PS/OP/Attrib/Rule/Equipment/Recipe 階層，Condition OP，右側 OP/Attrib/Rule/Equipment/Condition OP tabs；QCItem Tab 在 WinForms Load 時移除。
- [x] 新增 `web/src/components/ProductParameterSetup.vue` 並接到 PD102 Parameter；DEV 支援 OP 多選、Condition OP、Lot/Component Attrib、一般 Rule、設備多選、RecipeGroup 及唯讀 Recipe Detail；DEV localStorage；正式 API 模式唯讀。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 對 MESTEST 直接查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`、`ALL_TRIGGERS`。8 張表結構 metadata 已取得；沒有 FK/Trigger。`DBMS_METADATA.GET_DDL` 回 SocketException，故 Oracle source DDL 依 Oracle 字典欄位/約束/索引邏輯重建，不含實體 tablespace/storage。
- [x] 新增 Oracle source DDL 與 PostgreSQL CREATE-only reference、`docs/modules/PRD/產品參數設定.md`，更新 PRD 主規格、索引、家用端 handoff 及 DB query provenance。
- [ ] 未執行 build/test/browser；未連 YMES PostgreSQL，未部署 SQL，正式 API 模式仍唯讀。
- [x] PD102 Business Rule 已依真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、wsPRD/Core/Service 完成 WinForms 規格、DEV UI 與 homepc API/schema handoff；注意真正入口不是名稱相近的 `frmPRDOPRule_THEIL_BU1`。
- [x] PD102 OPDescription DEV/Oracle DDL 已由 commit `dde7973` checkpoint，完成靜態 code review；此段未執行 build/test/browser，Production API/schema 由家用端依 handoff 完成。


## PRD PD102 Business Rule DEV UI（2026-11-18）

- [x] 核對真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、WinForms Designer、`wsPRD`、`clsProduct`、Service graph validation；排除名稱相近但不同的 `PD_PRDOPRule_THEIL_BU1`。
- [x] 完成產品 OP 清單/圖形預覽及 Default/Close 版面、Add/Edit/多選 Del、圖形節點/連線/Phase 編輯、NetFlow readonly 的 DEV UI，接至 ProductBasisMaster 並只寫專用 localStorage。
- [x] 新增 `docs/modules/PRD/產品商業規則.md`；重用 Parameter 階段已查到的三張 Oracle 表與既有 DDL/PG CREATE 參照，沒有重複 Oracle query、沒有新增表或 CREATE SQL。
- [ ] Production API、OP/Phase/Business Rule/Template lookup、Lassalle XML serializer 尚待家用端完成；Default、樣板及 catalog 現在仍為 DEV 假資料。
- [x] OPDescription DEV UI/DDL/規格 checkpoint 已提交為 `dde7973`；靜態檢閱已完成，未執行 build/test/browser。Production API/schema handoff 見本檔末尾。

## PRD PD102 OPDescription 公司端檢查點（2026-11-18）

- [x] 依 PD102 真實 `btnDescription_Click` 入口，閱讀 `frmProductOPDescription`/Def/Designer、`wsPRD.Load/Add/Edit/DelPRDOPDescription`、`clsProduct`/`clsBasis_UPD`，確認 Add/Apply/Confirm/多選 Delete、PS lookup、IssueState、DataStamp 與產品 Default/Copy 的邊界。
- [x] 以 Windows PowerShell 5.1 + 受保護 Oracle 設定直連 MESTEST，唯讀取得 `TBLPRDOPDESCRIPTION` DBMS_METADATA DDL、欄位、PK/index、FK/trigger metadata；無業務資料列，未執行 Oracle DDL/DML。
- [x] 直接讀取 WinForms、wsPRD、Core，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 取得 `TBLPRDOPDESCRIPTION` DDL/字典；只讀 metadata，未讀業務列或執行 Oracle DDL/DML。
- [x] 已在公司端工作區建立 `ProductOpDescriptionSetup.vue` 並接至 PD102；DEV 支援清單、流程/OP 假查找、Add/Edit/多選 Delete、audit/DataStamp 與狀態唯讀，只寫 localStorage。Oracle source DDL、PostgreSQL CREATE-only 與規格文件已建立。
- [x] 已靜態複核元件、WinForms 操作流程與資料欄位；修正輸入欄位字型宣告。未執行 build/test/browser，未連 YMES PostgreSQL；Production API/schema/lookup 尚待家用端。
- [x] OPDescription 已完成靜態複核並建立 homepc handoff；RecipeGroup WinForms/Service/Core/Oracle metadata、DEV UI 與 handoff 已於本次段落完成。

## PRD PD102 RecipeGroup 指派公司端檢查點（2026-11-18）

- [x] 依 `btnRecipeGroup_Click` 追至 `PD_ProductRecipe`、`frmProductRecipe`/Def、`wsPRD.Load/Add/Edit/DelPRDRecipe`、`clsProduct`；確認 ProductNo/Version 關聯、hidden Edit/Find、Apply/Confirm/Delete、IssueState 未被子視窗使用及刪除省略 RecipeGroup 會刪整個產品版本的邊界。
- [x] Windows PowerShell 5.1 + 受保護 Oracle profile 直連 MESTEST，只讀取得 `TBLPRDRECIPE`、`TBLPRDRECIPEBASIS` DDL/字典；無業務資料列，未執行 Oracle DDL/DML。
- [x] 新增 `ProductRecipeGroupSetup.vue` 並接到 PD102，DEV 依產品版本保存關聯 localStorage，含多選刪除、已核准目前版查找、Add/Apply/Confirm/Close；Production API 未接線時唯讀。
- [x] 新增 `docs/modules/PRD/產品RecipeGroup.md`、Oracle source DDL、PostgreSQL CREATE-only 參照，更新主檔規格、DB connection provenance、兩個規格索引及 homepc API/schema handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。RecipeGroup DEV/DDL handoff 已完成；Accessory checkpoint 接續如下。

## PRD PD102 Accessory 公司端檢查點（2026-11-18）

- [x] 閱讀 THEIL BU1 PD102 `btnAccessory_Click`、`frmPRDAccessory_THEIL_BU1` Form/Designer、`frmPRDAccessoryDef_THEIL_BU1`，核對主/明細雙 Grid、ADD/EDIT/DEL、產品流程 OP 查找、配件類別與配件編號查找、All `*` / Specified、Apply/Confirm 和多選逐筆 Delete。
- [x] 追出 WinForms client 呼叫 `Load/Upd/DelPRDAccessory`、`LoadAccessoryType/Basis`、`LoadProductProcess(PSPrivilege=True)`、`LoadNodeBasis(NodeType=0)`、`LoadOPBasis` 及 `PD_ExpandProcess`。wsPRD/clsProduct server/Core 實作未包含於目前 client source，規格明列待家用端核對的替換/空清單/交易行為。
- [x] Windows PowerShell 5.1 + 受保護 Oracle profile 唯讀直連 MESTEST，取得 `TBLPRDACCESSORY` DDL、欄位/constraint/index/trigger 字典；未讀業務資料、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 `ProductAccessorySetup.vue` 接至 PD102；DEV 假資料含兩個作業站、兩種配件類別、指定配件與 `*` 全部模式；只寫產品關聯 localStorage，Production API 未接線時唯讀。
- [x] 新增 `docs/modules/PRD/產品配件設定.md`、Oracle source DDL、PostgreSQL CREATE-only，更新主檔規格、DB metadata provenance、兩個索引與 homepc API/schema handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。下一個 PD102 側邊功能為 `Resource`。

## PRD PD102 Resource 公司端檢查點（2026-11-18）

- [x] 直接對照 PD102 btnOPArea_Click (Handles btnResource.Click)、frmProductOPArea/Def Form 與 Designer；記錄兩視窗尺寸、PSPrivilege 查找、OP/Area 雙 Grid、Add/Edit/多選 Delete、Confirm 僅能一個 Default Area、全量更新呼叫及錯誤停止行為。
- [x] 重用既有 TBLPRDOPAREA、TBLOPAREA、TBLSMDAREABASIS Oracle DDL / PostgreSQL CREATE 參照；未重查 Oracle、未新增重複 CREATE SQL。
- [x] 完成 ProductResourceSetup.vue，接至 PD102 Resource；DEV 有產品流程/OP/Area 假查找、Area 左右多選移動、雙擊移動、Enter 精確搜尋、Default Area 驗證及 OP 多選刪除；假資料存 ymes.dev.prd-product-basis-relations.v1 的 opAreas。
- [x] 新增 docs/modules/PRD/產品作業區域設定.md，更新 PRD 主規格、規格索引、DB metadata provenance 及家用端 handoff。
- [ ] wsPRD/wsOP Core/Service 的 transaction、空 multivalue、Delete 與 Copy/Version 邊界待家用端查核；Production API 尚未接線。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL或部署 SQL。接續 PD102 SubOP，再依側邊功能順序遷移。

## PRD PD102 SubOP 公司端檢查點（2026-11-18）

- [x] 讀取 PD102 `btnSubOP_Click`、`frmPRDSubOP` Form/Designer、`wsPRD`、`clsProduct` 與 `clsBasis_UPD`；核對視窗尺寸、IssueState 行為、OP PS lookup、ADD/INSERT/EDIT/多選 DEL、DataStamp、局部成功及排序。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 MESTEST profile 唯讀取得 `TBLPRDOPSUBOP` DDL 與欄位/constraint/index metadata；未讀業務資料列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 `ProductSubOpSetup.vue` 並接至 PD102；DEV 假資料支援 OP lookup、新增/插入/修改/多選刪除與序號重排，寫入產品關聯 localStorage；正式模式唯讀。
- [x] 建立 `docs/modules/PRD/產品子作業設定.md`、Oracle source DDL、PostgreSQL CREATE-only 參照，並更新產品主規格、模組索引及 DB metadata provenance。
- [ ] 家用端仍須核對 live schema/migration 並實作 Production API/schema；詳見 `todolist_homepc.md` 的 SubOP handoff。
- [ ] 未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，未部署 CREATE SQL。下一個 PD102 側邊功能依 WinForms 順序為 Program。

## PRD PD102 Program 公司端檢查點（2026-11-18）

- [x] 閱讀 PD102 `btnProgram_Click`、`frmProductProgram` Form/Designer、`wsPRD.LoadProductProgramsDefine/UpdProductProgram` 及 `tcPRD_IC.clsProgram`；確認 checkbox Grid、產品 IssueState=0 才能 Confirm、全量替換、空選取清除指派。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護 MESTEST profile 唯讀取得 `TBLPRDPROGRAMBASIS`、`TBLPRDPRODUCTPROGRAMS` DDL/字典；無業務列、未執行 Oracle DDL/DML、連線秘密未輸出。
- [x] 新增 `ProductProgramSetup.vue` 並接至 PD102；DEV 可勾選程式並保存至關聯 localStorage，非未核准狀態保留可勾選但 Confirm disabled 的舊行為；Production 模式唯讀。
- [x] 建立 Program Oracle source DDL、PostgreSQL CREATE-only 參照、功能規格及 Home handoff，更新主檔規格、模組索引、資料庫 metadata provenance。
- [ ] 家用端須核對 `tblprdprogrambasis` first-phase bootstrap/live schema，僅在關聯表缺漏時新增 migration/API；詳見 `todolist_homepc.md`。
- [ ] 未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，未部署 SQL。Default 檢查點接續見下節；完成交接後依 PD102 側功能順序做 RunTime。

## PRD PD102 Default 公司端交接檢查點（2026-11-18）

- [x] 依 PD102 `btnDefault_Click`、`frmProductLoadDefault`/Designer、`wsPRD.UpdPRDOP_BatchDefault`、`clsProduct.UpdPRDOP_BatchDefault` 整理入口、兩種 OP 範圍、七類資料複製、IssueState、確認與錯誤關閉行為。
- [x] 新增 `ProductDefaultSetup.vue` 接至 PD102 Default，DEV 假樣板只寫 localStorage；正式 API 未接線時唯讀。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 直連 MESTEST 查 APS 產品表資料字典：DBMS_METADATA.GET_DDL 回 ORA-03135，改查 ALL_TAB_COLUMNS/CONSTRAINTS/CONS_COLUMNS/INDEXES 重建邏輯 DDL；沒有讀業務資料列或執行 DDL/DML。
- [x] 新增 `docs/modules/PRD/產品預設設定.md`、APS Oracle source DDL、PostgreSQL CREATE-only 參照，更新產品主規格、規格索引、DB metadata provenance 及家用端 API handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。桌電 pull 此檢查點後先看 `git status` 和最新 commit，靜態檢閱 Default 變更，再做 PD102 `RunTime`。Vue UI 在 `web/`、API 在 `src/YMes.Api/`、共用 DTO 在 `src/YMes.Contracts/`。
- [ ] 遇到 DBMS_METADATA ORA-03135 時，使用 Windows PowerShell 5.1 + ODP.NET 查 ALL_* dictionary；不要用 DBeaver GUI 抄 DDL。連線秘密不進 Git；桌電若沒有受保護 profile，要向使用者安全索取，不要猜連線資訊。

## 公司桌機與筆電共同分工

- 公司桌機和公司筆電都是公司端：用 DEV 假資料完成全部 Vue UI/互動邏輯，核對 WinForms 原始碼，唯讀查 Oracle MESTEST metadata，並提交模組規格及家用端 API/SQL handoff。
- 公司端不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不建立 PostgreSQL schema/migration、不實作或驗收家用端 API，也不 SSH/部署。這些工作只由家用電腦依 `todolist_homepc.md` 接手。
- 桌機只需為 Vue DEV 配置 Node.js 22.x（22.12+）與 npm 並安裝 `web/` dependencies；.NET 9 SDK/API/PostgreSQL 連線不屬於公司端前置設定。
- Oracle MESTEST 只可使用本機受保護設定唯讀查詢 DDL/metadata；如果桌機沒有該設定，安全重新設定後再查，不在 Git 或聊天輸出帳密。

## PRD PD102 RunTime 公司端檢查點（2026-11-18）

- [x] 核對 PD102 `btnRunTime_Click`、`frmProductRunTime` 和 `frmProductRunTimeDef`（含 Designer）、`wsPRD` 及 `clsProduct`。確認預設／已保存合併、六個欄位、無設備時的 `N/A` 列及全量儲存。
- [x] 以桌機受保護 Oracle profile 經 Windows PowerShell 5.1 + ODP.NET 驗證 MESTEST 登入，唯讀核對 `TBLPRDRUNTIMESETUP` 及六張 lookup 表的資料字典。`GET_DDL` 回 `ORA-03135`，已依字典重建 RunTime DDL。查詢限 metadata。
- [x] 完成產品主檔 RunTime 的 DEV 表格與單列編輯、Apply、Confirm、未儲存關閉提示，使用產品版次 localStorage。補齊模組規格、Oracle DDL／PostgreSQL CREATE-only 參照及家用端 API／schema handoff。
- [ ] 家用端接續正式 API、schema 及瀏覽器驗收。

## PRD PD102 QC Item 公司端檢查點（2026-11-18）

- [x] 依 `btnQCItem_Click`、`frmProductQCItem`／Designer、`wsOP`、`wsPRD`、`clsOpBasis`、`clsProduct` 與 `clsQCBasis`，完成四類 DEV 品管項目、InLine／OffLine、多選移除、檢驗旗標、Apply／Confirm 及 localStorage。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 唯讀核對 MESTEST 相關資料字典。以 13 欄版本重建 `TBLPRDQCITEM` 邏輯 DDL，包含舊參照缺少的 `XX_FREQ`。
- [ ] QC Import／QC Chart 目前提供選檔視窗，完整 Excel 解析、檢查、選取及儲存待接續。正式 API、schema 和瀏覽器驗收由家用端執行。

## PRD PD102 Packing Set 公司端檢查點（2026-11-18）

- [x] 核對 `btnPackingSet_Click`、`PackOperationMode`、傳統及多階 Form／Designer、`wsPRD`／`wsWIP`、`clsProduct`／`clsBasis_UPD`。完成 DEV 三階數量與配件、Copy、多階規則對應和使用中阻擋示例。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 唯讀核對七張 Oracle 表，收錄邏輯 DDL／CREATE-only 及家用端交接。序號主檔來源為獨立唯一索引。
- [x] 完成傳統 Packing Set 的 Serial Rule、Label Rule、Multi View 與 DEV 欄位預覽。序號／標籤主明細寫入 DEV localStorage；正式標籤引擎及瀏覽器驗收待接續。

## PRD PD102 EQPCapItem 公司端檢查點（2026-11-18）

- [x] 核對 `btnEQPCapItem_Click`、`frmPRDOPCapItem`、Def／Designer、`wsPRD`／`wsEMS`／`wsOP` 及 Core。完成 DEV 清單、選擇 OP／CapItem、Add／Edit／Copy／Delete、Apply／Confirm 與 IssueState 限制。
- [x] 使用 PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDOPCAPITEM` 與 `TBLEMSEQPCAPBASIS`。依字典建立 Oracle／PostgreSQL CREATE-only 參照，已補家用端交接。
- [ ] 瀏覽器與正式 API 待家用端驗收。

## PRD PD102 eSOP 公司端檢查點（2026-11-18）

- [x] 核對主／Def Form、Designer、`wsPRD`、Core 與 eSOP EAI 呼叫。完成 DEV Position／Sub OP、文件選擇、Add／Edit／Copy、Delete 及 Apply／Confirm。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對 `TBLPRDESOPBASIS`，依資料字典重建 DDL 參照並交接家用端。
- [ ] 正式 eSOP、文件存取、API 與瀏覽器驗收待接續。

## PRD PD102 EquipmentSpec 公司端檢查點（2026-11-18）

- [x] 核對 EquipmentSpec 入口、Form／Designer、Service、Core，完成 DEV 流程／設備查找、規格明細、四欄順序／精度檢查和 Confirm。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對 `TBLPRDEQUIPMENTSPEC`，補 Oracle DDL、CREATE-only 和家用端 API/schema handoff。
- [ ] 正式流程展開、設備權限、API 與瀏覽器驗收待接續。

## PRD PD102 APS Integration 公司端檢查點（2026-11-18）

- [x] 核對 APS 主窗／EQPTIME 的 Form／Designer、Service、Core。完成 DEV Node、主／設備明細、Update／Delete、明細重建與數值驗證。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對四張 APS 表欄數與鍵，沿用 Default／PS104 的 DDL 參照並補 API 交接。
- [ ] 正式流程、群組展開、API 與瀏覽器驗收待接續。

## PRD PD102 Bin 公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service 和 Core。完成 DEV Bin 勾選、Good/Fail、說明、OS Bin、版本狀態和本機儲存。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀查兩張表，補 Oracle 邏輯 DDL 及 PostgreSQL CREATE-only 交接。`GET_DDL` 失敗碼已記錄。

## PRD PD102 GBOM 公司端檢查點（2026-11-19）

- [x] 核對 GBOM WinForms／Designer、Service、Core，完成 DEV 依版次匯入檔案備份、檔名與標頭檢查、原檔下載。
- [x] 用 PowerShell 5.1 + ODP.NET 確認兩張客製表及一個 INVALID view，補 Oracle 邏輯 DDL、CREATE-only 及 API 交接。
- [ ] 完整工作簿解析、複合資料異動與正式備份下載待接續。DEV XLSX 已檢查識別列與 `版次REV`。

## PRD PD102 產品 Import 公司端檢查點（2026-11-19）

- [x] 核對 `PD_ProductDataImport_PRS`、Designer、Service／Core，完成 DEV XLSX 兩組預覽、主檔逐列驗證與新增。
- [x] Oracle 唯讀核對產品主檔、類別、流程與節點鍵，補類別 DDL／CREATE-only 及匯入 API 交接。
- [ ] 舊式 XLS、流程／子表寫入與範本服務待接續。

## PRD PD102 Expand All 公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、一般與網狀 Service／Core。以 DEV 流程、區段和作業假資料完成唯讀結果表格。
- [x] Oracle 唯讀核對共用表，沿用產品、區段、流程與作業主檔的 CREATE-only 參考。
- [ ] 正式流程解析與背景作業 lookup 待接續。

## PRD PD101 產品類別資料公司端檢查點（2026-11-19）

- [x] 核對主畫面、定義 Form／Designer、Service／Core；完成 DEV 查詢、ADD／EDIT／Copy、刪除／停用／核准及三個子視窗。
- [x] 唯讀核對 Oracle 主表和共用子表，整理 API/DDL 交接並將 PD102 DEV 類別查找接到已核准類別。
- [ ] 正式完整副本與資料權限待接續。

## PRD PD103 製程模組資料公司端檢查點（2026-11-19）

- [x] 完成 DEV 主檔、版次、查詢、流程、共用子頁及 XLSX 主檔匯入；核對主 Form／Designer／Service／Core。
- [x] Oracle 唯讀確認 `TBLPRDMODULEBASIS`，補來源邏輯 DDL、CREATE-only 比對與家用端 API 交接。
- [ ] 正式權限、簽核、完整匯入與共用資料解析待接續。

## PRD PD104 產品內容查詢公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 三類查找、條件交集及唯讀結果。重用 PD102 假資料。
- [x] Oracle 唯讀確認四張既有表，沿用 PD102 DDL，記錄家用端即時 API 與分頁交接。
- [ ] 正式查詢、資料權限和完整主檔報表欄位待接入。

## PRD PD105 產品參數群組公司端檢查點（2026-11-19）

- [x] 完成群組版次、明細、複製、狀態與核准目錄，供 PD102／PD103 DEV 指派使用。
- [x] Oracle 唯讀核對群組與明細，補明細 Oracle DDL 和 CREATE-only。
- [ ] 正式簽核、完整權限與候選查詢待接續。

## PRD PD106 生產時間限制公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core；完成 DEV Max/Min、參照、起迄站與 BR 檢查、Hold 原因、CRUD 與狀態操作。
- [x] Oracle 唯讀確認主檔、歷程與 Hold 原因結構；補來源邏輯 DDL、CREATE-only 與 API 交接。
- [ ] 正式流程展開、簽核、歷程、設備檢核待接續。

## PRD PD107 標籤設定公司端檢查點（2026-11-19）

- [x] 完成 DEV 主檔與明細 CRUD、Copy、順序和欄位值 Test／Preview。核對 Form、Designer、Service 與 Core。
- [x] Oracle 唯讀核對主／明細，補明細邏輯 DDL、CREATE-only 及 API 交接。
- [ ] 正式標籤引擎、腳本產值、印表機和格式檔待接續。

## PRD PD108 產品標籤對應設定公司端檢查點（2026-11-19）

- [x] 核對主窗、編輯與批次窗、Service／Core，完成 DEV 對應 CRUD、Copy、來源查找及過帳批次預設。
- [x] Oracle 唯讀確認對應表；補來源 DDL、CREATE-only 及家用端 API 交接。
- [ ] 正式跨頁 PrintLabel 參數與權限待接續。

## PRD PD109 產品作業站預設公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core。完成 DEV 前綴查詢、作業範圍、批次 Default／Amend、逐筆進度與取消。
- [x] Oracle 唯讀核對共用表，沿用既有 DDL 參考。
- [ ] 正式候選、權限、批次 API、動態作業預設及 APS 待接續。

## PRD PD110 產品作業站修正公司端檢查點（2026-11-19）

- [x] 核對獨立 Form／Designer 與 PD109 共用 Service／Core。完成 DEV 路徑、固定六項 Amend、進度／取消與 handoff。
- [ ] 正式動態預設、權限與 API 待接續。

## PRD PD111 產品作業站批次複製公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 來源／目標版次、可選複製項目、目標參數序號、進度與取消。
- [x] 沿用 PD109 Oracle 字典及共用表 CREATE-only，整理家用端複製 API 交接。
- [ ] 正式權限、ECN 開關、APS 與 API 待接入。

## PRD PD112 包裝規則設定公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 包裝版次、1–6 層定義、序號／標籤／配件及 PD102 候選串接。
- [x] Oracle 唯讀核對七張規則表，補來源邏輯 DDL、CREATE-only 與 API 交接。
- [ ] 正式用量檢查、站點規則及標籤引擎待接續。

## PRD PD113 產品包裝規則對應公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成獨立 DEV 路徑與 PD102 對應資料、PD112 候選共用。
- [x] Oracle 唯讀核對對應主鍵與 WIP／包裝狀態依賴，沿用既有對應 DDL。
- [ ] 正式使用中卡控與 API 待家用端接續。

## PRD PD114 自定義流程卡公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core 和 MESTEST 字典。
- [x] 完成主檔、欄位明細、複製、刪除和 DEV 批號欄位預覽，整理 [規格與交接](docs/modules/PRD/自定義流程卡.md)。
- [ ] 正式 WIP 產值及報表引擎待家用端接續。

## PRD PD115 產品流程卡對應公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core、MESTEST 欄位和主鍵。
- [x] 完成 CRUD、複製、核准產品與主流程卡候選，整理 [規格與交接](docs/modules/PRD/產品流程卡對應設定.md)。
- [ ] 正式候選及 API 待家用端接續。下一項 PD116 產品品管項目規格匯入。

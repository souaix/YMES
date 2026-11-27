# Codex handoff：公司端接續資料

更新：2026-11-27。用途：在公司端桌電／筆電 Codex 開啟 YMES repository 後直接接續，不必重做已完成調查。

## 全案優先順序更新（2026-11-27）

- [x] OE 工單管理的公司端 Vue DEV／來源規格共 13 項已完成；正式 API／schema 由家用端依原 handoff 接手。使用者最新指示：WIP「在製品管理」優先於其他尚未開始的模組。公司端先做可核對來源的 WIP WinForms／DEV／規格；家用端先做待辦中可執行的 WIP schema／API／migration／整合／驗收工作，再處理非 WIP 項目。WP803 正式 UI 接線、WIPINV_CS DEV／規格 handoff 已完成，WP804 已完成；WPI05 是目前唯一待來源的 WIP 選單功能，原始 Form／Service／Core 到位後應優先接續。WPI05 缺來源期間，記錄缺項並按既定清冊繼續可做工作；發現其他可做的 WIP 即插隊。RT501 與 RT410 正式頁已接家用端唯讀 API／lookups，runtime 驗收待資料與操作時機；RT410 legacy 主／子設備時間邊界差異已核源並記錄。RT416／RT417／RT502／RT503／RT504／RT608 均已完成。OEE summary Oracle DDL 因桌機受保護 profile 不存在尚未取得；公司端不連 PostgreSQL、不實作正式 API/schema。

## 公司端目前交接：RPT RT601 生產批使用物料查詢（2026-11-27）

- [x] 已核對 `frmCont_Material_byLot`、`wsWIP.LoadCont_Material_JoinLog`、`clsLot.LoadCont_Material_JoinLog`；完成 `/YMES/RPT/ContMaterialByLot` DEV 16 欄唯讀查詢。[RT601 規格與家用端 API handoff](modules/RPT/生產批使用物料查詢_RT601.md)
- [ ] 家用端先核 YMES live schema／同步，再實作必填 LotNo、登入保護、參數化、bounded paging 的唯讀 API；需釐清 MaterialLot join fan-out。公司端未連 PostgreSQL、未 build／test／browser。

## 前項公司端交付：RPT RT504 生產批執行法則查詢（2026-11-27）

- [x] 已核對三份相同 `frmRT_RuleLogQuery`（Form／Designer 同檔）、`wsWIP.LoadWIPRuleCheckLog`／`clsLot.LoadWIPRuleCheckLog`，以及 Rule No lookup 的 `wsPRD.LoadPRDOPRule`／`clsProduct.LoadPRDOPRule`；完成 `/YMES/RPT/RuleLogQuery` DEV 查詢頁。[RT504 規格與家用端 handoff](modules/RPT/生產批執行法則查詢_RT504.md)
- [ ] `TBLWIPRULECHECKLOG` Oracle source DDL／metadata 未在 repo；桌機無受保護 Oracle profile，未查 Oracle／猜 CREATE-only。`TBLPRDOPRULE` lookup source DDL 已在 `database/reference/mestest_prd_parameter_setup_source_ddl.sql`；家用端先核 live schema 與資料同步。

## 前項公司端交付：RPT RT503 等待生產批查詢（2026-11-27）

- [x] 已核對三份相同 `frmLotState_Wait`（Form／Designer 同檔）、`wsWIP.LoadLotBasisJoinState` 與 `clsLot.LoadLotBasisJoinState`；完成 `/YMES/RPT/WaitLot` DEV 八條件查詢與四種可見 lookup。[RT503 規格及家用端 handoff](modules/RPT/等待生產批查詢_RT503.md)
- [ ] 家用端先核 YMES live schema、資料同步及 OP lookup 欄位，再評估登入授權保護與參數化、穩定排序、分頁限量的唯讀 API；固定 `Status=2`、不加 LotState 條件。`TBLOPBASIS` 原生 DDL／metadata 未在 repo；桌機無受保護 Oracle profile，未查 Oracle／猜 CREATE-only。

## 前項公司端交付：RPT RT502 異常生產批查詢（2026-11-27）

- [x] 已核對三份一致的 `frmLotState_Hold`、`wsWIP.LoadLotBasisJoinState` 與 `clsLot.LoadLotBasisJoinState`；完成 `/YMES/RPT/HoldLot` DEV 八條件查詢及四種可見 lookup。[RT502 規格與家用端 handoff](modules/RPT/異常生產批查詢_RT502.md)
- [ ] `TBLOPBASIS` 原生 DDL／metadata 未在 repo；桌機無受保護 Oracle profile，未查 Oracle／猜 CREATE-only。家用端先核 live schema／lookup，再處理登入授權與分頁唯讀 API。

## 公司端已完成交接：RPT RT608 生產批資源檢視(分攤)（2026-11-27）

- [x] 已核對 `frmRPTLotResource_Share`（CSiMES／THEIL／DEV_SILVA 相同）、`wsRPT.LoadCont_Resource_Share` 與 `clsReport.LoadCont_Resource_Share`；完成 `/YMES/RPT/LotResourceShare` DEV 假資料及八類查詢條件。[RT608 規格與家用端 handoff](modules/RPT/生產批資源檢視分攤_RT608.md)
- [ ] `TBLWIPCONT_RESOURCE_SHARE` Oracle DDL／metadata 不在 repo，桌機受保護 Oracle profile 不存在；未查 Oracle／猜 CREATE-only。家用端先核對 YMES PostgreSQL schema／同步來源，再接正式登入授權唯讀 API。

## 公司端目前交接：RPT RT417 生產批下線後歷程總表（2026-11-27）

- [x] 已核對 `frmRPTLotReleaseLog`（CSiMES／THEIL／DEV_SILVA 相同）、`wsRPT.LoadReleaseLog_Report`／`clsReport.LoadReleaseLog_Report`，以及 `wsWIP.LoadReleaseLog` Component／BIN 明細與 Split／Merge 查詢。
- [x] 完成 `/YMES/RPT/LotReleaseLog` Vue DEV 假資料、日期及批號／工單／產品／OP／User／UserActiveID 條件、Show ERF／Wait 分支和選列 Lot 明細。[RT417 規格與家用端 handoff](modules/RPT/生產批下線後歷程總表_RT417.md)
- [ ] `TBLWIPRELEASELOG` 及 Component／BIN、Split／Merge 表的 Oracle DDL／metadata 不在 repo；桌機找不到受保護 Oracle profile，未查 Oracle／猜 CREATE-only。收到安全 profile 後再唯讀查結構。
- [ ] 家用端先核對 live PostgreSQL schema 與正式同步來源，再決定登入保護、RT417 ACL、參數化／分頁 API；公司端不連 PostgreSQL、不做正式 API/schema、不部署。

## 公司端最新交付：RPT RT413 設備 MTTR 統計表_以月份匯總（2026-11-27）

- [x] 核對主／Chamber `frmEQP_MTTRSummary_byMonth`／Designer、`wsRPT.LoadEQPMTTRSummary_ByMonth`、`clsReport.LoadEQPMTTRSummary_byMonth` 與趨勢圖 Form；完成 `/YMES/RPT/EqpMttrSummaryByMonth` DEV 月報表、Chamber 子報表、多條件查詢及 MTTR／MTBF／Availability 趨勢圖。[RT413 規格](modules/RPT/設備MTTR統計表_以月份匯總_RT413.md)
- [x] 記錄 Core 逐月重算 RT412 單月統計、每台設備輸出三個指標列、動態 `yyyy/MM` 欄位；來源無月份上限。DDL refs 沿用 RT412；未新查 Oracle、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／索引與月份跨度，設計受授權、參數化、有跨度／設備筆數上限的月度唯讀 API。
- [x] OE 工單管理優先覆核與跨畫面總覽完成：OE101–OE209、OEB01、OEP01 共 13 項皆有原始 Form、獨立規格、Vue route／page mode；共用 Service／Core 引用對得上可讀來源，未發現公司端缺頁。見 [OE 工單管理總覽](modules/OE/工單管理模組總覽.md)。正式 API／schema 仍由家用端負責。接續依清冊回到 RT414。

## 公司端最新交付：RPT RT414 設備狀態期間統計（2026-11-27）

- [x] RT414 清冊項 `frmEQPStateStatistics`／`RT_EQPStateStatistics.dll` 與 EMS ES409 共用 WinForms、Service／Core；新增獨立 RPT menu key、route `/YMES/RPT/EquipmentStateStatistics`，重用已完成 ES409 DEV 畫面。詳 [RT414 規格](modules/RPT/設備狀態期間統計_RT414.md) 及 [ES409 共用來源規格](modules/EMS/設備狀態期間統計查詢.md)。
- [x] Oracle DDL refs 沿用 ES409；未新查 Oracle、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。家用端可依兩個 FunctionKey／權限需求評估共用唯讀 API。RT415、RT416 已接續完成；目前排程依文件開頭 WIP 優先序。

## 公司端最新交付：RPT RT415 異常單柏拉圖分析（2026-11-27）

- [x] 核對查詢 Form／Designer、結果圖與魚骨 Form、`wsRPT.LoadERFParetoHoldReason` Service、`clsReport.LoadERFParetoHoldReason` Core；完成 `/YMES/RPT/ErfPareto` DEV 篩選、多選條件、Hold Qty Pareto／累積率、Top-N、原因明細與五要因魚骨摘要。[RT415 規格與 API handoff](modules/RPT/異常單柏拉圖分析_RT415.md)
- [x] 來源表 Oracle DDL／PG CREATE-only refs 沿用 QC409、WP501、WP503、WP305、WP101 與 QC HoldItem references；未新查 Oracle、未讀業務資料、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RT416 已完成，現接續 RT417。

## 前項公司端交付：RPT RT412 設備 MTTR 統計表（2026-11-26）

- [x] 核對 `frmEQP_MTTRSummary`／Designer、`wsRPT.LoadEQPMTTRSummary` Service、`clsReport.LoadEQPMTTRSummary`／`ShowEQPMTTRSummary` Core；完成 `/YMES/RPT/EqpMttrSummary` Vue DEV 日期／多選條件、Chamber 顯示、故障次數／MTTR／MTBF／Availability 表格及公式說明。[RT412 規格](modules/RPT/設備MTTR統計表_RT412.md)
- [x] 記錄 `TBLEQPSTATECONTROL` 故障狀態、狀態歷程區間截短、非故障時數統計及舊 Core 的父設備／Chamber 篩選限制。所需 MESTEST DDL refs 已在 repo；未新查 Oracle、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RT413 已接續完成。

## 前項公司端交付：RPT RT411 每日設備稼動長條圖（2026-11-26）

- [x] 核對 `frmEQPDailyUtilizationChart`／Designer、Chamber 子 Form、`wsRPT.LoadEQPRealTimeReport`／`LoadEQPRealTimeReport_Chamber` Service、`clsReport` Core；完成 `/YMES/RPT/EqpDailyUtilizationChart` Vue DEV Gantt、多條件 lookup、Utilization Rate、區段提示／明細、圖表控制與 ChamberManage 子設備切換。[RT411 規格](modules/RPT/每日設備稼動長條圖_RT411.md)
- [x] 狀態區間以 overlap 條件截到查詢區間；Rate 以 `UtilizationType=0` 狀態分鐘計算。Core SQL 字串串接、無分頁；Form 限查詢 24 小時。MESTEST DDL refs 已在 repo，本桌機未新查 Oracle、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RT412、RT413 已完成；OE 工單管理 13 項來源覆核完成，下一項為 RT414。

## 前項公司端交付：RPT RT410 設備稼動查詢（2026-11-26）

- [x] 核對 `frmEQPDailyReport`、RPT `LoadEQPDailyReport`／`LoadData_SonEQPData` Service／`clsReport` Core；完成 `/YMES/RPT/EqpDailyReport` DEV 假資料頁，並接上正式設備稼動 API／Area／Equipment／State lookup，保留狀態百分比、Moves、圓餅明細、ChamberManage 子設備展開。已記錄 legacy 主設備與 Chamber 狀態時間條件差異；runtime 操作與正向業務資料待驗收。[RT410 規格](modules/RPT/設備稼動查詢_RT410.md)
- [x] 已核實主設備狀態 SQL 的 EndTime 條件與 Chamber overlap 條件不同；正式 API 目前兩者都採半開區間重疊，口徑差異已記入 RT410 模組規格，是否維持或修正待家用端評估。Oracle DDL refs 已在 repo；本桌機沒有可用受保護 profile，未新查 Oracle。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 正式設備／狀態／WIP source rows 尚無正向樣本；待同步到位後驗收登入、lookup、報表列與狀態明細，並確認時間邊界預期。正式唯讀 API 已部署、公司端正式 UI 已接上；RT412、RT413 已完成，後續排程依本檔 WIP 優先順序及公司端待辦處理。

## 前項公司端交付：RPT RT501 生產批狀態查詢（2026-11-26）

- [x] 核對 `frmLotState`、WIP `LoadLotBasisJoinState` Service／Core；完成 `/YMES/RPT/ConfirmLot` DEV 八條件查詢、lookup、狀態與庫存條件、排序和屬性明細假視窗。[RT501 規格](modules/RPT/生產批狀態查詢_RT501.md)
- [x] 記錄 Status=6 狀態註解與 AdditionalXml 查詢集合不一致。所需 DDL refs 已在 repo；未使用桌機 Oracle profile、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／索引，設計受授權、參數化、固定狀態條件與分頁上限的唯讀查詢 API。

## 前項公司端交付：RPT RT604 物料用於元件查詢（2026-11-26）

- [x] 核對 `frmCont_CompMaterial_byMaterial`、物料／產品選取 Service、WIP `LoadCont_MaterialLotComp_JoinLog` Service/Core；完成 `/YMES/RPT/ContMaterialByMaterial` Vue DEV 雙模式查詢、選取窗及 MaterialLot No 篩選。[RT604 規格與 API/schema handoff](modules/RPT/物料用於元件查詢_RT604.md)
- [x] Material No／Product No 對應 MaterialLevel 0／1；Product No 仍填進舊 Core 的 MaterialNo 參數。RT603／RT604 共用三張 WIP 表；repo 無可信 DDL、桌機 Oracle profile 不存在，未查 Oracle／猜 CREATE。
- [ ] 家用端核對三張 WIP 表 DDL／metadata 與 live schema／索引，實作受授權、參數化、非空編號、層級驗證及有上限的唯讀 API。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RT501、RT410–RT413 已完成；OE 工單管理 13 項來源覆核完成，下一項為 RT414。

## 前項公司端交付：RPT RT603 元件使用物料查詢（2026-11-26）

- [x] 核對 `frmCont_CompMaterial_byComp`、`wsWIP.LoadCont_MaterialLotComp_JoinLog`、`tcWIP_IC.clsLot.LoadCont_MaterialLotComp_JoinLog`；完成 `/YMES/RPT/ContMaterialByComponent` Vue DEV 假資料畫面與查詢互動。[RT603 規格與 API/schema handoff](modules/RPT/元件使用物料查詢_RT603.md)
- [x] Core 以 Report Mode 查 `tblWIPLotBasis`、`tblWIPLotLog`、`tblWIPCont_MaterialLotComp`；ComponentNo 空白時不追加篩選。Repo 無三表可信 DDL／metadata，桌機 Oracle profile 不存在，未查 Oracle／猜 CREATE。
- [ ] 家用端核三表 MESTEST DDL／metadata 與 YMES live schema／索引；提供受授權、參數化、要求非空 ComponentNo、有筆數上限且穩定排序的唯讀報表 API。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RT501、RT410–RT413 已完成；OE 工單管理 13 項來源覆核完成，下一項為 RT414。

## 公司端最新交付：RPT RT204 產出看板時段主檔（2026-11-26）

- [x] 核對 frmBillBoardTimeStepBasis／Designer、frmBillBoardTimeStepBasisDef／Designer、RPT TimeStep CRUD／Approve Service 與 clsReport Core；完成 /YMES/RPT/TimeStepBasis Vue DEV 四狀態主檔、跨午夜時段、Add／Edit／Copy／Apply／Confirm、Approve、刪除／停用假流程。[RT204 規格與家用端 API/schema handoff](modules/RPT/產出看板時段主檔_RT204.md)
- [x] IssueState 0／1／2／-1、TimeStepNo 01–15、Description 編輯限制與 Core 時段重疊邏輯已核對；記錄舊 Core 重疊判斷可能漏掉完整包圍、Approve 不重查重疊。
- [x] Repo 無 tblRPTTimeStepBasis 可信 DDL；桌機 Oracle attachment/profile 不存在，未查 Oracle／猜 CREATE。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端取得可信 MESTEST DDL／metadata，確認日期型別與鍵索引後設計受授權參數化 CRUD API；核准流程需交易內重新檢查重疊。RT603、RT604 已完成，下一項依 RPT 清冊為 RT501。

## 前項公司端交付：RPT RT203 作業站現況看板_產品（2026-11-26）

- [x] 核對 `frmBillBoard_OPState_ProductNo`／Designer、共用 SetupType 3、RPT `LoadBillBoardOPStateConfig`／`LoadBillBoard_OPState_ProductNo` Service／`clsReport` Core 與 WIP `LoadLotBasisJoinState`；完成 `/YMES/RPT/OpStateProductNoBoard` DEV ProductNo 系列堆疊 Queue／Run／Hold／Wait、跨 OP 共用數量軸、設定／刷新、雙擊放大、右鍵 Lot Detail 與 Copy toast 模擬。[RT203 規格與家用端 API/schema handoff](modules/RPT/作業站現況看板_產品_RT203.md)
- [x] LotState／LotBasis／ProductBasis 的 MESTEST DDL refs 可用；共用看板 config／content DDL 缺。桌機受保護 Oracle attachment 不存在，未新查 Oracle／猜 CREATE。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端依 RT203 handoff 核對 live schema／索引，以受授權、參數化 API 支援 ProductNo 彙總及分頁 Lot Detail；確認 SetupType 3 user/device 設定範圍。RT603、RT604 已完成，下一項依 RPT 清冊為 RT501。

## 前項公司端交付：RPT RT202 作業站現況看板_產品類別（2026-11-26）

- [x] 核對 `frmBillBoard_OPState_PRDType`／Designer、共用 SetupType 2、RPT `LoadBillBoardOPStateConfig`／`LoadBillBoard_OPState_PRDType` Service／`clsReport` Core 與 WIP Lot Detail；完成 `/YMES/RPT/OpStateProductTypeBoard` DEV ProductType 系列堆疊 Queue／Run／Hold／Wait、跨 OP 共用數量軸、設定／刷新、雙擊放大、ProductNo 鑽取、Lot Detail 與 Copy 模擬。[RT202 規格與家用端 API/schema handoff](modules/RPT/作業站現況看板_產品類別_RT202.md)
- [x] `tblWIPLotState`／LotBasis／ProductBasis DDL references 可用；共用看板 config／content DDL 缺。桌機受保護 Oracle attachment 不存在，未新查 Oracle／猜 CREATE。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端沿 RT201 handoff 落實受授權、參數化的 ProductType 狀態彙總 API；核 live schema／索引與 SetupType 2 user/device 設定範圍。RT203 公司端 handoff 已完成，RT603、RT604 已完成，下一項依 RPT 清冊為 RT501。

## 前項公司端交付：RPT RT201 作業站現況看板（2026-11-26）

- [x] 核對 `frmBillBoard_OPState`／Designer、共用 SetupType 1 與 Child Form、RPT config／狀態／產品鑽取 Service／`clsReport` Core，以及 WIP `LoadLotBasisJoinState`／`clsLot`；完成 `/YMES/RPT/OpStateBoard` DEV Queue／Run／Hold／Wait 柱與圓餅、產品類別／編號鑽取、設定／刷新、雙擊放大及右鍵 Lot Detail。[RT201 規格與家用端 API/schema handoff](modules/RPT/作業站現況看板_RT201.md)
- [x] WIP Lot、OP、Product、Process、Customer 的 MESTEST DDL reference 可用；`tblRPTBillBoardOPStateConfig`／Content DDL 未找到。桌機可確認的 Oracle attachment 不存在，沒有另查 Oracle 或猜 CREATE。公司端未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 `/api/reports/op-state`、breakdown、lots 的 live schema／索引及受授權範圍；確認看板設定的 user/device scope，再實作正式 API／CREATE-only SQL。RT202、RT203 已完成，RT603、RT604 已完成，下一項 RT501。

## 前項公司端交付：RPT RT207 作業站時段產出_趨勢圖（2026-11-26）

- [x] 核對 TrendChart Form／Designer、共用 SetupType 5、RPT `LoadBillBoardOPStateConfig`／`UpdBillBoardOPStateConfig`／`LoadBillBoardOPOutputSummary`／`LoadBillBoardOPOutputLotDetail` Service 與 `clsReport` Core；完成 `/YMES/RPT/OpOutputTrendChart` DEV 假資料複合圖、四欄 OP 圖表、共用數量軸、Yield 折線、刷新設定、雙擊放大及右鍵 Lot Detail。[RT207 規格與家用端 API/schema handoff](modules/RPT/作業站時段產出趨勢圖_RT207.md)
- [x] 已開始時段顯示 Good／Scrap／Defect 群組柱及 Yield；未開始時段 Legacy Form 將空資料點轉成零值。DEV 右鍵 copy 以 toast 模擬、Lot Detail 用假資料；不觸碰系統剪貼簿或後端。LotLog MESTEST DDL reference 可用；time-step／設定表 DDL 未找到，未查 Oracle／猜 CREATE。未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端沿 RT205 唯讀摘要 API 核時段、Yield、區間和 LotLog schema；確認 user/device 設定 scope 後設計 SetupType 5 設定 API。RT201–RT203 已完成，RT603、RT604 已完成，下一項 RT501。

## 前項公司端交付：RPT RT206 作業站時段產出_良率儀表板（2026-11-26）

- [x] 核對 `frmBillBoard_OPOutputSummary_Gauge`／Designer、`RT_BillBoard` 共用 Setup、`LoadBillBoardOPStateConfig(SetupType=4)` 與 RT205 共用的 `LoadBillBoardOPOutputSummary` Service／Core；完成 `/YMES/RPT/OpOutputYieldGauge` DEV 圓形 Gauge、OP／時段排序、OP／字級／更新設定與自動刷新。[RT206 規格與家用端 API/schema handoff](modules/RPT/作業站時段產出良率儀表板_RT206.md)
- [x] 顯示每個已開始時段 × OP 的 Yield、Good／Scrap／Defect／累計 Good。固定色帶為 0–60／60–80／80–100%；StdYield 雖載入但自訂門檻程式已註解。`YieldContainDefect != 0` 隱藏 DefectQty。
- [x] RT205 使用的 LotLog MESTEST DDL reference 存於 `database/reference/mestest_wip_wp201_dependency_source_ddl.sql`；RT206 `tblRPTBillBoardOPStateConfig`／Content 與 `tblRPTTimeStepBasis` DDL 缺。桌機 Oracle profile metadata 未驗證、未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [x] RT206 後已完成 RT207、RT201–RT203；RT603、RT604 已完成，下一項依 RPT 清冊為 RT501。

## 前項公司端交付：RPT RT205 作業站時段產出看板（2026-11-26）

- [x] 核對 `frmBillBoard_OPOutputSummary`／Setup／History Query Form／Designer、RPT `LoadBillBoardOPOutputConfig`／`UpdBillBoardOPOutputConfig`／`LoadBillBoardOPOutputSummary` Service 與 `clsReport` Core；完成 `/YMES/RPT/OpOutputSummary` DEV 假資料、定時刷新、設定與指定日期歷史查詢。[RT205 規格與家用端 API/schema handoff](modules/RPT/作業站時段產出看板_RT205.md)
- [x] 確認報表查 `tblWIPLotLog_Report` + `tblOPBasis`，時段取 `tblRPTTimeStepBasis`，依 `YieldContainDefect` 算 Yield、依 `StdYield` 標色；記錄 `tbl_NBWIPOPOutputSummary` 屬另一寫入路徑、Core 時段閉區間可能重複及設定 LEFT JOIN 收窄疑點。repo 有 2026-11-19 擷取的 LotLog MESTEST DDL；time-step／設定 DDL 與桌機 Oracle profile metadata 未驗證，未猜 CREATE；未連 PostgreSQL、未實作正式 API/schema、未 build／test／browser。
- [ ] 家用端依 RT205 規格確認同版 MESTEST DDL／鍵索引、時段與時區、系統參數來源；核對舊 Core 時段邊界及 Yield 結果，再提供可信 PostgreSQL CREATE-only SQL 與唯讀報表 API。設定持久化的使用者／裝置範圍待定。
- [x] RT205 後接續完成 RT206 作業站時段產出_良率儀表板、RT207 趨勢圖、RT201–RT203 作業站現況看板；RT603、RT604 已完成，下一項依清冊為 RT501。

## 前項公司端交付：RPT RTB03 Wafer Map 電性測試圖表（2026-11-26）

- [x] 核對 Industry_Base inline Query／View、`COM_WaferMAPTestChartLib`、RPT／SYS Service 與 `clsRPT_Industry_Base`／`clsSYSMesParameter_Industry_Base` Core；完成 RPT「行業包」入口、DEV PS／TestRecipeType 多選、Equipment／Component／Lot／TestDate 條件、CheckFlag 多選、依 X/Y 繪多個 Recipe wafer maps、Max／Min／Mean／Sigma 選項與 SVG 報表下載。[RTB03 規格](modules/RPT/WaferMap電性測試圖表_RTB03.md)
- [x] 記錄動態 `TBLWIPRAWRECORD_<type>` 座標與 RecipeNo 欄、MES LotLog／OP join、統計計算方式及 Core 疑點：條件拼成 `A.And`／`B.And`、tuning 套錯 SQL 字串、EDC/MES 分庫註解與 active 跨表查詢不一致。桌機 Oracle profile／RawRecord 可信 DDL metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端確認 EDC RawRecord owner／欄型別、Recipe 動態欄規則、跨 EDC／MES join 實際連線與基數、索引及分頁限制，再依可信 DDL 建 schema／參數化唯讀 API。
- [x] RTB03 後接續完成 RT205 作業站時段產出看板；現在接 RT206。

## 前項公司端交付：RPT RTB01 Wafer 電性數據運行圖（2026-11-26）

- [x] 核對 inline Query／View、RPT／SYS Service／Core；完成 DEV EDC 查詢、CheckFlag 多選及量測序列線圖。[RTB01 規格](modules/RPT/Wafer電性數據運行圖_RTB01.md)
- [x] 記錄動態 BLOB、MES LotLog join、統計快照副作用及 Core SQL／dialect 疑點；DDL／Oracle metadata 缺失，未猜 CREATE、未連 PostgreSQL、未 build／test／browser。RTB03 已接續完成。

## 前項公司端交付：RPT RTB02 Wafer 電性數據常態分析圖（2026-11-26）

- [x] 核對 inline Query／analysis View、RPT／SYS Service／Core；完成 DEV PS／Recipe 查詢、選列直方圖、量測明細和排除重算。[RTB02 規格](modules/RPT/Wafer電性數據常態分析圖_RTB02.md)
- [x] 記錄 per-Recipe BLOB／MES LotLog／OP lookup、統計表寫入副作用及 BLOB row index 疑點；DDL 缺失、未猜 CREATE、未連 PostgreSQL、未 build／test／browser。RTB01 已接續完成。

## 前項公司端交付：RPT RTS01 電性測試資料查詢（2026-11-26）

- [x] 核對 Industry_Base inline `frmElectricalTestDataQuery`、RPT Service／Core；完成 DEV PS／Recipe 多選、條件、Now／History、分組與動態欄位階層 Grid。[RTS01 規格](modules/RPT/電性測試資料查詢_RTS01.md)
- [x] 記錄動態 summary tables DDL 缺失、日期上界判斷與 SQL 拼接風險；未猜 CREATE、未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。RTB02 已接續完成。

## 前項公司端交付：PM507 故障維修登錄查詢（2026-11-26）

- [x] 核對 inline `frmBreakDown_RepairQuery`、`wsPM.LoadBreakDown_RepairQuery`、`clsPMMaintanceLog.LoadBreakDown_RepairQuery` 及設備／ChamberManage 載入；完成 `function:PM:BREAKDOWN_REPAIRQUERY` Vue DEV Equipment 多選、三組日期時間、狀態條件與 13 欄唯讀 Grid。規格：[PM507 故障維修登錄查詢](modules/PM/故障維修登錄查詢_PM507.md)。
- [x] Core UNION `tblPMEqpBreakDownState` 未結案列與 RepairLog／BreakDownLog 結案列；未選狀態代表全部。記錄 Form 的 RepairStart To 誤傳 RepairEnd To。桌機 Oracle profile／可信 DDL metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata、三表欄位／鍵／索引及一對多維修列，實作參數化唯讀分頁 API 與可信 CREATE-only SQL。
- [x] 公司端後續接續 RPT RTS01、RTB02、RTB01、RTB03，均已完成；繼續盤點下一個未復刻 FunctionKey。

## 前項公司端交付：PM506 設備保養效率查詢（2026-11-26）

- [x] 核對 inline `frmPMPerformance`、`wsPM.LoadPMPerformance`／`LoadSingleTableDistinceData`、`clsPMPlanLog.LoadPMPerformance`；完成 `function:PM:PM PERFORMANCE QUERY` Vue DEV 四組多選清單、兩組可選日期、All／PM／Required 類型及 12 欄唯讀效率 Grid。規格：[PM506 設備保養效率查詢](modules/PM/設備保養效率查詢_PM506.md)。
- [x] Core 合併已完工 PM Log + current/history detail 與 Required history，Act Done 依 Release／Finished 差換算小時。記錄 WO Finished end-date 分支誤讀 `EstPMDateEnd` 的舊程式疑點。公司桌機 Oracle profile／可信 DDL metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata、三類資料分支欄位／鍵、WOTYPE 值、ChamberManage 規則及日期邊界；實作參數化唯讀分頁 API 與可信 CREATE-only SQL。
- [x] 當時依清冊接續的 PM507 已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM505 故障原因統計（2026-11-26）

- [x] 核對 `frmBreakDownQuery`／inline Designer、`wsPM.LoadSumOfEQPBreakDown`、Equipment Type／Basis lookup 與 `clsPMMaintanceLog`；完成 `function:PM:PMBRAEKDOWN QUERY` Vue DEV 雙清單、故障／修復時間條件、彙總 Grid、五種圖表與 Print。規格：[PM505 故障原因統計](modules/PM/故障原因統計_PM505.md)。
- [x] Core 結果按 BreakDownNo／BreakDownCause Count，內連故障主檔／明細／RepairLog；Repair Time 實際條件用 RepairLog.RepairEndTime。桌機 Oracle profile 與可信 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata、四張統計來源表鍵與 RepairLog 關聯基數，提供參數化安全統計 API 及可信 CREATE-only SQL。
- [x] PM507 已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM504 保養工單歷史資料查詢（2026-11-26）

- [x] 核對 `frmPMWoHistory`／inline Designer、`wsPM.LoadPMWoBasis_His`／`LoadPMWoDetail_His`、`wsEQP.LoadEquipmentBasis`、`clsPMBasis`；完成 `function:PM:PMWO HISTORY` Vue DEV Equipment、兩組日期、All／PM／REQ 篩選、主 Grid 與點選載入明細。規格：[PM504 保養工單歷史資料查詢](modules/PM/保養工單歷史資料查詢_PM504.md)。
- [x] Core 以 Equipment／Estimated PM／Finished Date 條件查明細 `WONo` 子查詢限制主歷史表；第二次明細查詢只依 `WONo`，ChamberManage 在 UI 過濾。桌機 Oracle profile 與可信 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata、兩張工單歷史表鍵、欄位型別及索引，提供參數化分頁主查詢與延遲明細 API、可信 CREATE-only SQL。
- [x] PM505–PM507 均已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM503 保養計劃歷史資料查詢（2026-11-26）

- [x] 核對 `frmEQPMaintenancePlanHistory`／inline Designer、`wsPM.LoadMaintenancePlanLog` 與四組 distinct lookup、`clsPMPlanLog`；完成 `function:PM:PM PLAN HISTORY` Vue DEV 四組雙清單、可選日期、五種狀態及 19 欄唯讀歷史 Grid。規格：[PM503 保養計劃歷史資料查詢](modules/PM/保養計劃歷史資料查詢_PM503.md)。
- [x] Core 查詢歷史表並內連頻率／SOP 主檔；畫面依 CreateDate 升冪排序。舊 Core 把條件直接串入 SQL，正式 API 應參數化並分頁。桌機 Oracle profile 與可信 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata、鍵與欄位型別，提供 PM503 唯讀分頁 API 及可信 CREATE-only SQL；另核 PM501／PM502 共用端點。
- [x] PM504–PM507 均已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM502 保養日曆設定（2026-11-26）

- [x] 核對 inline `frmPMPlanCalendar`／Designer、`frmCustomAppointmentDialog`、`wsPM.LoadMaintenancePlan`／`LoadEQPPMPlanByEQPType`、`clsPMBasis`；完成 `function:PM:PM CALENDAR` Vue DEV 設備／類別／頻率篩選、Day／Week／Month、雙月導覽、事件唯讀明細及列印預覽。PM501 會傳入原始查詢結果。規格：[PM502 保養日曆設定](modules/PM/保養日曆設定_PM502.md)。
- [x] Calendar 只從保養計畫產生本機 all-day 事件，舊程式沒有計畫新增／修改 Service；查詢 SQL 字串拼接風險已交接。桌機 Oracle profile 與可信 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／同版 MESTEST metadata 與鍵，完成 PM501／PM502 共用分頁參數化唯讀查詢 API 及可信 CREATE-only SQL。
- [x] PM503–PM507 均已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM501 設備保養計畫查詢（2026-11-26）

- [x] 核對 inline Form／Designer、`wsPM.LoadMaintenancePlan`／distinct lookup、`clsPMBasis.LoadMaintenancePlan`；完成 `function:PM:PM MAINTENANCE QUERY` Vue DEV 三組篩選、日期範圍、排序及 PM502 日／週／月日曆資料交接。規格：[PM501 設備保養計畫查詢](modules/PM/設備保養計畫查詢_PM501.md)。
- [x] 查詢內連設備保養計畫、頻率與 SOP 表；舊 Core 把查詢值直接串入 SQL。Oracle profile 及可信 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端先核 live schema／metadata 並提供分頁、參數化唯讀 API 與可信 CREATE-only SQL。

## 前項公司端交付：PM405 設備修復登錄（2026-11-26）

- [x] 核對 THEIL Form／Def、客製 `UpdEQPRepairRegister`、標準待修查詢與歷史 Service／Core；完成 `function:PM:EQP REPAIR REGISTER` Vue DEV State=1 清單、原因明細、維修表單、有效 Repair Item 與假狀態連動。規格：[PM405 設備修復登錄](modules/PM/設備修復登錄_PM405.md)。
- [x] 儲存依序建立故障／修復歷史、更新 EMS、移除待修單、寄工程群組郵件；舊流程非原子。`RepairState=default(int)` 實為 0，與 -999 註解不一致；UserNo 過濾已註解。DEV 不呼叫 EMS／寄信。
- [x] 桌機 Oracle profile 與可信四張歷史表 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema 與 MESTEST metadata、外鍵、EMS／Chamber 交易邊界後，實作參數化、冪等與可重試 API／CREATE-only DDL。
- [x] PM501–PM507 保養查詢均已完成；接著從功能目錄盤點下一項未復刻 FunctionKey。

## 前項公司端交付：PM404 設備故障登錄（2026-11-26）

- [x] 核對客製 `frmEQPBreakDownRegister_THEIL_BU1`／Def、THEIL Add／Edit／Confirm Service、標準 Load／Delete／Detail Service 與 Core；完成 Vue DEV State=0 清單、原因明細及 Add／Edit／Copy／Delete／Confirm 假互動。規格：[PM404 設備故障登錄](modules/PM/設備故障登錄_PM404.md)。
- [x] 記錄新登錄查重不限制 State、設備狀態 `ES102` 預設、NOCHANGED 不改 EMS、Confirm 改 State=1 並寄工程群組郵件。DEV 不連 EMS／寄信。Edit 的 DataStamp 未傳到 Core；父子資料、EMS 更新、登錄確認及郵件不是一個原子流程。
- [x] 桌機 Oracle profile 與可信父／子表 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端先核 live schema、同版 MESTEST metadata、序號產生器與 EMS／Chamber 關聯，再實作版本安全、交易式明細及可重試確認 API／CREATE-only DDL。
- [x] 當時下一項 PM405「設備修復登錄」已完成；公司端接續 PM501「設備保養計畫查詢」。

## 前項公司端交付：PM403 設備與故障原因對應（2026-11-26）

- [x] 核對 `frmEQPTypeBreakDownMap`／Def、`wsPM.Load/Add/Edit/DelEQPTypeBreakDownMap`、`clsPMBasis`／`clsBasis_UPD`；完成 `function:PM:EQPTYPE BREAKDOWN MAP` DEV 主從查詢、CRUD／Copy。規格：[PM403 設備與故障原因對應](modules/PM/設備與故障原因對應_PM403.md)。
- [x] 故障原因選擇器只載入 IssueState=2；PM404 會按設備類型載入映射供故障登錄勾選。`PM_ACCPLAN` 重複 FunctionKey 已以功能名稱／FormName 區分。
- [x] Edit 使用 DataStamp；舊 Delete 沒有版本條件且 Service 允許只帶單一鍵而批次刪除。桌機 Oracle attachment 與可信映射表 DDL／metadata 缺失，未猜 CREATE；未連 PostgreSQL、未實作正式 API/schema、未 build／test／browser。
- [ ] 家用端核 PostgreSQL live schema／Oracle metadata、FK 與 PM404 引用後實作參數化、expected DataStamp API 及可信 CREATE-only DDL。
- [x] 當時下一項 PM404「設備故障登錄」現已完成；公司端接續 PM405「設備修復登錄」。

## 前項公司端交付：PM402 維修項目基本資料（2026-11-26）

- [x] 核對 `frmRepairBasis`／Def、`wsPM.Load/Add/Edit/Del/ApproveRepairBasis`、`clsPMBasis`／`clsBasis_UPD`；完成 `function:PM:REPAIR BASIS` DEV 四狀態 CRUD／Copy／核准、停用與硬刪。規格：[PM402 維修項目基本資料](modules/PM/維修項目基本資料_PM402.md)。
- [x] 核對客製 PM405 設備修復登錄以預設 IssueState=2 載入候選，顯示 RepairNo／RepairName；舊硬刪未傳 DataStamp、簽核結束 SQL 不更新版本或稽核欄。
- [x] 桌機受保護 Oracle profile／attachment 不存在，repo 無可信 `TBLPMRepairBasis` DDL／metadata，未猜 CREATE；未連 PostgreSQL、未實作正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema 與 PM405 登錄／明細 FK，再做參數化、版本安全的查詢／維護／簽核 API。
- [x] 當時下一項 PM403「設備與故障原因對應」已完成，並依功能名稱／FormName 與 PM_ACCPLAN 區分。

## 前項公司端交付：PM401 故障原因基本資料（2026-11-26）

- [x] 核對 `frmBreakDownBasis`／Def、`wsPM.Load/Add/Edit/Del/ApproveBreakDownBasis`、`clsPMBasis`／`clsBasis_UPD`；完成 `function:PM:BREAKDOWN BASIS` DEV 四狀態 CRUD／Copy／核准、停用與硬刪。規格：[PM401 故障原因基本資料](modules/PM/故障原因基本資料_PM401.md)。
- [x] PM403 設備與故障原因對應及 PM404 設備故障登錄會讀取此主檔；舊硬刪未傳 DataStamp，簽核完成／退回沒有更新版本或稽核欄。
- [x] 桌機受保護 Oracle profile 不存在，repo 無可信 `TBLPMBreakDownBasis` DDL／metadata，未猜 CREATE。未連 PostgreSQL、未實作正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／Oracle metadata 與 PM403／PM404 引用限制，再實作參數化、併發安全、原子簽核／停用 API。
- [x] 當時下一項 PM402 已完成；公司端接續 PM403「設備與故障原因對應」。

## 前項公司端交付：PM303 保養工單完成與回報（2026-11-26）

- [x] 核對 `frmPMWoFinish`／Def、`wsPM.RepayForWoDetail`、`clsPMBasis.RepayForWoDetail` 與整單完工歷史搬移；完成 `/YMES/PM/WorkOrderFinish` DEV 查詢、主從明細、SOP Detail、逐項／整張回報。
- [x] 新增 [PM303 規格與家用端 API/schema handoff](modules/PM/保養工單完成與回報_PM303.md)，更新模組索引、選單進度與兩端 TODO。
- [x] DEV 共用 PM301／PM302 工單與 PlanLog 假資料；模擬 WOStatus=3、PlanLog ExecuteState 3→4、整單完成搬移本機歷史與 EQP 狀態。
- [x] 舊 Form 對早於 ReleaseDate 的完成時間只提示仍送出；家用端 API handoff 已要求伺服器拒絕。桌機受保護 Oracle profile 不存在，直接表 DDL／metadata 未取得，未猜 CREATE。
- [ ] 家用端核 live schema／Oracle metadata，交易式實作 PM303 回報、全單完成歷史搬移與可重試 EQP／Chamber 狀態。
- [x] 當時下一項 PM401、PM402、PM403 均已完成；現接續 PM404「設備故障登錄」。

## 前項公司端交付：PM302 保養工單下線（2026-11-26）

- [x] 核對 `frmPMWORelease`、`wsPM.ReleaseWoDetail`、`clsPMBasis.ReleaseWoDetail`；完成 `/YMES/PM/WorkOrderRelease` DEV 主從查詢、五種狀態篩選、SOP 檢視、Release／Release All。
- [x] 新增 [PM302 規格與家用端 API/schema handoff](modules/PM/保養工單下線_PM302.md)，更新索引、清冊進度及兩端 TODO。
- [x] DEV 共用 PM301 工單與 PlanLog 假資料；模擬 WOStatus=2、PM ExecuteState=3、PM 設備累計歸零與可選 EQP 狀態更新。
- [x] 桌機受保護 Oracle profile 不存在；repo 沒有直接表可核實 DDL／metadata，本次未連 MESTEST、未猜 CREATE。公司端未連 YMES PostgreSQL、未做正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema／Oracle metadata，實作交易式冪等下線 API，並處理 EQP 狀態更新部分完成風險。
- [ ] 當時下一項 PM303「保養工單完成與回報」現已完成；公司端接續 PM401「故障原因基本資料」。

## 前項公司端交付：PM301 開立保養工單（2026-11-26）

- [x] 核對 `frmPMWoBasis`／Create／Detail／SOP Detail、`wsPM`、`clsPMBasis`／`clsBasis_UPD` 與 `clsIssue`；完成 `/YMES/PM/WorkOrderBasis` DEV 主從查詢、PM 計畫選取開單、計畫移入 PlanLog、狀態簽核／刪除、未凍結明細編修與 SOP／工具／備品檢視。
- [x] 新增 [PM301 規格與家用端 API/schema handoff](docs/modules/PM/開立保養工單_PM301.md)。已記錄可見 PM Work Order、隱藏 Required Work Order 分頁、IssueState／WOStatus、EWyyMM#### 編號及計畫消耗交易要求。
- [x] 桌機沒有受保護 Oracle profile，repo 無可核實 PM Work Order／Plan Log DDL；未查 Oracle、未猜 CREATE。DEV 使用 localStorage 假資料，未連 YMES PostgreSQL、未實作正式 API/schema、未 build／test／browser。
- [ ] 家用端核 live schema 與 Oracle metadata，交易式建立工單、明細、PlanLog 並消耗活動計畫；實作核准／下線及 DataStamp API。當時下一項 PM302，現已完成，後續公司端接 PM303。
- [x] PM301 與當時累積的公司端待同步工作已正常 commit/push，提交 `21cca14`。

## 前項公司端交付：PM204 設備定次定時保養設定（2026-11-26）

- [x] 核對 `frmFixedTimesSetUp`／Def／Def_EQP、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/FixedTimesSetup` DEV 類型設定、設備列及 SET 批次互動。
- [x] [PM204 規格與家用端 API/schema handoff](modules/PM/設備定次定時保養設定_PM204.md) 記錄 TypeCode 0／1、QtyType 0/1/2、DataStamp、整類刪除與 SET 重建。舊 Service 先刪後逐筆新增、非原子；關閉 AutoCreateWo 時 Core 可能留下舊 PMSOPNo。
- [x] 桌機受保護 Oracle profile 不存在，repo 無可核實 `TBLPMFIXEDTIMESPMBASIS` DDL／索引；未連 Oracle、未猜 CREATE／輸出 credentials。
- [ ] 家用端核 live PostgreSQL schema／migration 與 Oracle metadata；以交易實作類型預設、設備覆寫、QtyType、SET 清單、DataStamp 更新及整類刪除。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未 build／test／browser。當時下一項 PM301，現已完成；PM302／PM303 也已完成，當前接續 PM401。

## 前項公司端交付：PM203 設備定量保養設定（2026-11-26）

- [x] 核對 `frmFixedQuantitySetUp`／Def／Def_EQP、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/FixedQuantitySetup` DEV 類型設定、設備列與 SET 批次互動。
- [x] [PM203 規格與家用端 API/schema handoff](modules/PM/設備定量保養設定_PM203.md) 記錄 TypeCode 0／1、DataStamp、整類刪除及 SET 重建。舊 Service 先刪後逐筆新增、非原子；關閉 AutoCreateWo 時 Core 可能留下舊 PMSOPNo。
- [x] 桌機受保護 Oracle profile 不存在，repo 無可核實 `TBLPMFIXEDQTYPMBASIS` DDL／索引；未連 Oracle、未猜 CREATE／輸出 credentials。
- [ ] 家用端核 live PostgreSQL schema／migration 與 Oracle metadata；以交易實作類型預設、設備覆寫、SET 清單、DataStamp 更新及整類刪除。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未 build／test／browser。當時下一項為 PM204，現已完成。

## 前項公司端交付：PM202 保養計劃變更（2026-11-26）

- [x] 核對 `frmEQPMaintenancePlanAdjust`／Def、`wsPM.Load/Edit/MoveEQPMaintenancePlan`、`clsPMBasis`；完成 `/YMES/PM/MaintenancePlanAdjust` DEV 月曆查詢、改期、取消轉歷史互動。
- [x] [PM202 規格與家用端 API/schema handoff](modules/PM/保養計劃變更_PM202.md) 記錄計畫查詢、改期與取消流程。舊 Core 取消先新增 PlanLog，隨後又刪除相同 SerialNo 的 PlanLog；家用端應交易式保留取消歷史，不應複製此缺陷。
- [x] 桌機受保護 Oracle profile 不存在，repo 無可核實的計畫／歷史 DDL；未連 Oracle、未猜 CREATE／輸出 credentials。
- [ ] 家用端核 live PostgreSQL schema、Oracle metadata、計畫及 PlanLog 主鍵／索引／日期精度，再實作有界查詢、版本化改期及原子取消 API。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未 build／test／browser。當時下一項為 PM203，現已完成。

## 前項公司端交付：PM201 設備保養標準程序（2026-11-26）

- [x] 核對 `frmEQPTypeSOPBasis`／Def、`PM_EQPTypeSOPBatchCopy`、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/EquipmentTypeSopBasis` DEV 查詢、CRUD／Copy、參照 lookup、計畫轉歷史刪除與批次複製互動。
- [x] [PM201 規格與家用端 API/schema handoff](modules/PM/設備保養標準程序_PM201.md) 已記錄主檔欄位、歷史計畫刪除、批次生成來源及簡化處。PM201 沒有 IssueState／Approve；Plan Data／Plan Set 是獨立視窗，本次提供 DEV 唯讀樣例／摘要。
- [x] 桌機受保護 Oracle profile 不存在，repo 沒有可核實的 `tblPMEQPTypeSOPBasis` DDL；未連 Oracle、未猜 CREATE／輸出 credentials。DEV 批次排程依 localStorage 假頻率估算。
- [ ] 家用端先核 live PostgreSQL schema／Oracle metadata、年度與設備／頻率／SOP／State 參照、Plan／PlanLog 關係，再作交易式 CRUD、計畫轉歷史及排程批次 API。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未 build／test／browser。當時下一項依 FunctionKey 為 PM202「保養計劃變更」，現已完成。

## 前一項公司端交付：PM110 工具基本資料（2026-11-26）

- [x] 核對 `frmEQPToolBasis`／Def、`wsEQP`、`clsToolBasis`／`clsBasis_UPD`；完成 `/YMES/PM/ToolBasis` DEV 四狀態 CRUD／Copy／Approve／Delete，並接入選單。
- [x] 規格與家用端 API/schema handoff：[PM110](modules/PM/工具基本資料_PM110.md)。IssueNo `TOOL BASIS` 控制 Active／Pending；Core 硬刪未檢查 PM108 `ToolUsedDetail` 引用；ToolName XML Type 誤標 Integer、Service 實讀字串。
- [x] PM108 DEV 工具 lookup 改讀 PM110 localStorage key。Repo 已有 `tbleqptoolbasis` provisional CREATE 草稿，但註解標示欄位推測；公司桌機 Oracle profile 不存在，未查 Oracle／未修改 CREATE。
- [ ] 家用端核 live PostgreSQL schema／Oracle metadata、PK/FK／索引／DataStamp，處理 PM108 工具引用並建立正式 API／Issue workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [x] PM201 完成時下一項依 FunctionKey 為 PM202；目前最新進度與待辦見本檔開頭（PM203 已完成，下一項 PM204）。

## 前一項公司端交付：PM109 標準作業程序（2026-11-26）

- [x] 核對 `frmSOPBasis`／Def、SOP Detail／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/SopBasis` DEV 四狀態主檔與步驟明細 CRUD／Copy。
- [x] 規格與家用端 API/schema handoff：[PM109](modules/PM/標準作業程序_PM109.md)。SOP File Path 只存用戶端路徑字串；主檔 Copy 和 steps Copy 分開呼叫；`SOP BASIS` IssueNo 控制 Active／Pending。
- [x] 步驟 Add Service 序號先 floor 至一位小數再轉 int、Edit 保留一位小數；舊 Core 的自動重排已註解；DEV 依序號顯示。
- [x] 兩張業務表 DDL／metadata 未取得，公司桌機受保護 Oracle profile 不存在，未猜 CREATE SQL。
- [ ] 家用端核 live PostgreSQL schema、PK/FK／序號精度與 DataStamp 條件；交易式處理主從 Copy／Delete、狀態保護、bounded 參數化 API 及正式 Issue workflow；決定 SOP 文件路徑／文件儲存策略。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [x] PM110、PM201 已接續完成；此段為當時進度快照，PM202、PM203 已完成，現依清冊續做 PM204。

## 前一項公司端交付：PM108 預防保養項目資料（2026-11-26）

- [x] 核對 `frmMaintenanceItem`／Def、兩種明細 Form／Def、`wsPM`／`wsEQP`／`wsSYS`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/MaintenanceItem` DEV 主檔及 Tool／Substitution 子明細維護。
- [x] 規格與家用端 handoff：[PM108](modules/PM/預防保養項目資料_PM108.md)。主檔硬刪會移除兩種子表；Copy 子資料分開呼叫且 Service 空 catch 會吞錯；`MAINTENANCE ITEM` IssueNo 控制 Active／Pending。
- [x] 三張業務表 DDL／metadata 未取得，桌機受保護 Oracle profile 不存在，未猜 CREATE。
- [ ] 家用端核 live schema、複合鍵及 lookup 引用；在交易內實作主從 Copy／Delete、DataStamp／audit、父狀態與正式 Issue workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [x] PM109、PM110、PM201 均已接續完成；此段為當時進度快照，PM202、PM203 已完成，現依清冊續做 PM204。

## 前一項公司端交付：PM107 預防保養維護方式設定（2026-11-25）

- [x] 核對 `frmMaintenanceType`／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/MaintenanceType` DEV 四狀態 CRUD／Copy／Delete／Approve 並接入 App。
- [x] 規格與家用端 handoff：[PM107](modules/PM/預防保養維護方式設定_PM107.md)。`MAINTENANCE TYPE` IssueNo 控制 Active／Pending；PM108 MaintenanceItem 會 Join MaintainNo，舊硬刪未檢查引用。
- [x] 業務 DDL／metadata 未取得，桌機受保護 Oracle profile 不存在，未猜 CREATE。
- [ ] 家用端核 live schema 與 MaintainNo 引用，交易式實作版本化修改／狀態轉換、參數化查詢及正式 Issue workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [x] PM108、PM109、PM110、PM201 均已接續完成；此段為當時進度快照，PM202、PM203 已完成，現依清冊續做 PM204。

## 前一項公司端交付：PM106 工作形式設定（2026-11-25）

- [x] 核對 `frmJobType`／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/JobType` DEV 四狀態 CRUD／Copy／Delete／Approve 並接入 App。
- [x] 規格與家用端 handoff：[PM106](modules/PM/工作形式設定_PM106.md)。IssueNo `JOB TYPE` 控制直達 Active／Pending；批次逐筆可部分成功，workflow 結束 SQL 未更新 DataStamp／稽核欄位。
- [x] 選單快照僅確認 `tblPMJobTypeBasis` 表名；業務 DDL／metadata 未取得，桌機受保護 Oracle profile 不存在，未猜 CREATE。
- [ ] 家用端核 live PostgreSQL schema 與 PMJobNo 引用，交易式實作版本化修改／狀態轉換、參數化查詢及正式 Issue workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [ ] 下一項依 FunctionKey 為 PM108「預防保養項目資料」。

## 前一項公司端交付：PM105 備品群組資料（2026-11-25）

- [x] 核對 inline Designer `frmSubstitutionGroup`／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/SubstitutionGroup` DEV 群組與成員維護，接入 App 選單／路由。
- [x] 規格與家用端 handoff：[PM105](modules/PM/備品群組資料_PM105.md)。修改群組類別會清空明細；同一備品不可屬於多個群組；多選儲存重用 ActiveRow 成員清單；Copy Service 會吞 Core 複製例外。
- [x] 選單 IssueTable 為 NULL；群組 Basis／Detail DDL／metadata 未找到，桌機受保護 Oracle profile 不存在，未猜 CREATE。
- [ ] 家用端核 PostgreSQL schema／migration，交易式處理群組唯一性、成員類別／狀態／跨群組驗證、DataStamp、Copy 與 Delete。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [ ] 下一項依 FunctionKey 為 PM107「預防保養維護方式設定」。

## 前一項公司端交付：PM104 備品基本資料（2026-11-25）

- [x] 核對 inline Designer `frmSubstitutionBasis`／Def／VendorMapDef、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/SubstitutionBasis` DEV 主從頁與 App 選單／路由。
- [x] 規格及家用端 handoff：[PM104](modules/PM/備品基本資料_PM104.md)。硬刪包含 VendorMap、SubstitutionGroupDetail；Copy 分段呼叫且 Service 內層 catch 會吞掉映射複製例外。PM103／PM104 跨表代碼碰撞檢查已註解。
- [x] 導覽 seed 只確認 PM104 FunctionKey 與 Basis 表名；Basis／VendorMap／GroupDetail DDL／metadata 未取得，桌機 Oracle profile 不存在，未猜 CREATE。
- [ ] 家用端核 PostgreSQL live schema／引用，交易式處理 Copy／delete／共用 Property 唯一性，接入 DataStamp 與 Issue workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。
- [ ] 下一項依 FunctionKey 為 PM105「備品群組資料」。

## 前一項公司端交付：PM103 備品類別資料（2026-11-25）

- [x] 核對 `frmSubstitutionType`／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/SubstitutionType` DEV CRUD／複製／核准／刪除假互動並接入 App 選單／路由。
- [x] 新增 [PM103 規格及家用端 API/schema handoff](modules/PM/備品類別資料_PM103.md)，更新索引、清冊進度與兩端 TODO。Core 的 SubstitutionBasis 共用 Property 代碼衝突檢查已註解，需由正式 API／schema 加強驗證。
- [x] 選單 seed 僅確認 `TBLPMSUBSTITUTIONTYPE` 表名；桌機受保護 Oracle profile 不存在，實際業務 DDL／metadata 未取得，未猜 CREATE。
- [ ] 家用端先核 live schema 與類別引用，再實作授權、bounded paging、交易式 DataStamp／Issue workflow、引用保護與必要的 CREATE-only migration。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。

## 前一項公司端交付：PM102 備品供應商基本資料（2026-11-25）

- [x] DEV／來源規格見 [PM102](modules/PM/備品供應商基本資料_PM102.md)；Vendor／Contactor／VendorMap 的可信同版 DDL／metadata 未取得，未猜 CREATE。

## 公司端最新交付：PM102 備品供應商基本資料（2026-11-25）

- [x] 核對 `frmSubstitutionVendor`／Def／ContactorDef、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/SubstitutionVendor` DEV Vendor／Contactor CRUD、複製、簽核／刪除假互動與 Email 預覽，接入 App 選單／路由。
- [x] 新增 [PM102 規格及家用端 API/schema handoff](modules/PM/備品供應商基本資料_PM102.md)，更新索引、清冊進度與兩端 TODO。舊刪除會移除聯絡人／備品映射，整家複製是分段服務呼叫，API 需改為交易式處理。
- [x] 桌機受保護 Oracle profile 不存在；選單快照只確認 Vendor 主表名，Vendor／Contactor／Map 的實際 DDL／metadata 均未取得，未猜 CREATE。
- [ ] 家用端先核 live PostgreSQL schema／migration、複合鍵及映射引用，再依規格實作登入授權 API、bounded paging、DataStamp 與原子 copy/delete/workflow。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。

## 前一項公司端交付：PM101 頻率基本資料（2026-11-25）

- [x] 核對來源並完成 `/YMES/PM/FrequencyBasis` DEV；規格為 [PM101](modules/PM/頻率基本資料_PM101.md)。選單 snapshot 可確認表名 `TBLPMFREQUENCYBASIS`，但無業務表 DDL／metadata，未猜 CREATE。
- [x] 家用端 API／schema handoff、索引與 TODO 已更新；公司端未連 PostgreSQL，未做 build／test／browser。

## 公司端最新交付：PM101 頻率基本資料（2026-11-25）

- [x] 核對 `frmFrequencyBasis`／Def、`wsPM`、`clsPMBasis`／`clsBasis_UPD`；完成 `/YMES/PM/FrequencyBasis` DEV 搜尋、狀態頁籤、FixedType 0–3、CRUD／複製／刪除／簽核假互動並接入 App 選單／路由。
- [x] 新增 [PM101 規格及家用端 API/schema handoff](modules/PM/頻率基本資料_PM101.md)，更新 Module Index、選單進度與兩端 TODO。
- [x] 選單 seed 能確認 `TBLPMFREQUENCYBASIS` 名稱，但桌機受保護 Oracle profile 不存在，實際業務表 DDL／metadata 未取得；沒有產生臆測 CREATE SQL。
- [ ] 家用端先核 live PostgreSQL schema／migration，再依模組規格實作正式授權 API、版本化寫入與 workflow；CREATE-only 僅能在缺表且 DDL 已確認後新增。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署、未執行 build／test／browser。

## 公司端最新交付：IV602 不良品入庫（2026-11-25）

- [x] 核對 frmINVSCRIn／frmINVSCRInDef inline Form／Designer、wsINV／wsSYS、clsInventory；IV602 草稿維護和 IV601 Source=40 確認／過帳入口分離。
- [x] 完成 /YMES/INV/ScrIn DEV：State=0 查詢、三種 TurnType 來源勾選、取號、單頭新增／修改、BookingFlag 模擬、State=-1 作廢與明細；新增 modules/INV/不良品入庫_IV602.md，接入 App 選單／路由及索引。
- [x] 四張 SCR In 單據表 DDL／metadata 沿 WP603 既有唯讀 MESTEST references；IV601 的三張 SCR Inventory 目標表 DDL 仍缺。記錄舊作廢的部分狀態風險、ComponentNo-only 編輯回選錯列風險。
- [ ] 家用端核 PostgreSQL live schema 後實作父單授權／bounded paging、完整來源複合鍵及原子預約；Source=40 worker／ExeSCRIn 過帳獨立處理。
- [ ] 公司端未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署，未執行 build／test／browser。下一步掃描 INV 清冊尚未復刻且有可讀來源的功能。

## 前一項公司端交付：IV601 報廢倉庫房資料（2026-11-25）

- [x] 核對 inline frmINVSCRInventory／Designer、同頁 frmINVSCRInConfirm、wsSMD.LoadInventoryBasis、wsSYS.LoadBin、wsINV 查詢／刪除 methods 與 clsInventory。
- [x] 完成 /YMES/INV/ScrInventory DEV InventoryType=3 查詢、SCR Inventory Grid、Component 優先／FailBin fallback、BinOrder 動態欄、批次刪除確認與 SCR In Source=40 交易排程互動；新增 IV601 規格並接入 App 路由／選單。
- [x] 舊 Core 查詢無排序／分頁且 SQL 字串串接；刪除用 InventoryNo＋LotSerial 清 Component、FailBin、主列，沒有狀態／預約檢查。多列刪除由 Form 逐筆呼叫，可能部分完成。
- [x] SCR In Confirm 的 Service 只排 Source=40 交易並逐筆設定 Lot reverse lock；正式庫存異動由下游執行。三張 SCR Inventory Oracle DDL／metadata 缺失，桌機 profile 不可用，未猜 CREATE。
- [ ] 未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署，未執行 build／test／browser。IV602 已完成；下一步掃描 INV 清冊尚未復刻且有可讀來源的功能。

> **最新公司端接手（2026-11-26）**：PM301、PM302、PM303 已完成公司端 DEV 與家用端 handoff；PM301／PM302 已正常推上 GitHub，PM303 依本次工作正常提交／推送後接續 PM401「故障原因基本資料」。先看本檔、兩端 TODO、[模組規格索引](MODULE_REPLICATION_SPEC_INDEX.md)。PM303 直接表 DDL／metadata 未取得，桌機沒有受保護 Oracle profile，不猜 CREATE。公司端只做 Vue DEV 與來源／handoff，不連 YMES PostgreSQL、不做正式 API 或部署。

## 公司端最新交付：IV406 半成品出庫單記錄（2026-11-25）

- [x] 核對 `frmINVSEMIOutLog` inline Designer、`wsINV.LoadINVSEMIOutBasis/LoadINVSEMIOutDetailAndComponent`、`clsInventory` 及 IV403／IV401 邊界。
- [x] 完成 `/YMES/INV/SemiOutLog` DEV 唯讀查詢，含 SEMIOutNo／Inventory／State／CreateDate／INVDate、Basis／Detail／Component；詳 [IV406 規格](modules/INV/半成品出庫單記錄_IV406.md)。
- [x] 記錄 INVDate 透過 `TBLINVTransactionLog.Source=21` 的 CreateDate 篩 TicketNo，與 Basis.ConfirmDate 分開；舊 Core 沒排序／分頁且無條件可讀大量資料。
- [x] 出庫三表沿 IV403；沒有可用受保護 Oracle profile／可信同版 DDL，未新增臆測 schema。IV406 唯讀，不需新表。
- [x] IV405 `SEMI PRODUCT IN LOG` 已加入半成品庫選單並指向 IV503 共用 `/YMES/INV/ProductInLog` 頁；規格沿 [IV503](modules/INV/成品入庫單記錄_IV503.md)。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署，未執行 build／test／browser。IV103 已完成；IV602 已完成；下一步掃描 INV 清冊尚未復刻且有可讀來源的功能。

## 公司端最新交付：IV407 半成品進料單記錄（2026-11-25）

- [x] 核對 `frmINVSEMIInLog` inline Designer、`wsINV.LoadINVSEMIInBasis/LoadINVSEMIInDetailAndComponent` 與 `clsInventory`，確認固定 Source=0、狀態及兩組日期篩選。
- [x] 完成 `/YMES/INV/SemiInLog` DEV 唯讀查詢，包含 SEMIInNo／Inventory／State／CreateDate／InputDate、選列載入 Detail 與 Component；規格見 [IV407](modules/INV/半成品進料單記錄_IV407.md)。
- [x] 記錄 InputDate 在舊 UI 顯示為 ConfirmDate、-1 對應 Scrap、Detail relation 僅 `(SEMIInNo, LotNo)`；舊 Core 沒排序／分頁，無條件可讀大量資料。
- [x] 使用 IV404 已知三表；沒有可用受保護 Oracle profile 或可信同版 DDL，未新增臆測 schema。IV407 無新資料表需求。
- [x] 公司端未連 PostgreSQL、未建立 schema／migration、未實作正式 API、未部署，未執行 build／test／browser。下一項 IV406。

## 公司端最新交付：IV404 開立半成品進料單（2026-11-25）

- [x] 核對 `frmINVSEMIIn`／BasisDef／DetailDef inline Designer、`wsINV` 單頭與明細 Load／Add／Edit／Delete、`clsInventory`／`clsBasis_UPD`、IV401 確認入口。
- [x] 完成 `/YMES/INV/SemiIn` DEV 外購 Source=0／State=0 草稿、單頭／明細 CRUD、手動 Lot／Component、假 Unit Transfer、刪除確認；規格見 [IV404](modules/INV/開立半成品進料單_IV404.md)。
- [x] 記錄 SEMIInNo 手動輸入、InventoryType=2、CustomerLotNo→LotNo／BaseLotNo、目前 ProductVersion、Component GoodQty 換算及 VB `CInt(Qty)` 對 Component 筆數限制。確認／庫存入帳只由 IV401 `ExeSEMIIn` 執行。
- [x] `TBLINVSEMIInBasis/Detail/Component` 未找到可信同版 Oracle DDL／metadata；桌機受保護 profile 不可用，未猜 CREATE SQL。家用端須先核 live PostgreSQL schema，再決定 CREATE-only migration。
- [x] 公司端未連 PostgreSQL、未建 schema／migration、未實作正式 API、未部署，未執行 build／test／browser。IV407 已接續完成，下一項 IV406。

## 公司端最新交付：IV403 開立半成品出庫單（2026-11-25）

- [x] 核對 `frmINVSEMIOut`、BasisDef／DetailDef inline Designer、`wsINV` 單頭／明細 Load／Add／Edit／Del 與 `clsInventory`／`clsBasis_UPD`。
- [x] 完成 `/YMES/INV/SemiOut` DEV State=0 單頭查詢、單頭／明細 CRUD、來源 Lot／Locator 假 Lookup、Component 整批帶入顯示及 DEV 列印預覽。
- [x] 新增 [IV403 規格與家用端 API／schema handoff](modules/INV/開立半成品出庫單_IV403.md)，接入 App 選單／路由並更新 Module Index、MES menu progress 與兩端 TODO。
- [x] 記錄舊 Core SQL 字串串接、單頭／明細 State=0、明細 DataStamp、MO Material List／替代料、HaveLevel／HaveComponent 數量鎖定，以及 Component 以完整複合鍵整批預約。
- [x] 出庫單 Basis／Detail／Component 沒有找到可信同版 Oracle DDL／metadata；桌機受保護 Oracle profile 不存在，未猜 CREATE SQL。共用 SEMI Inventory／Component 與 Lot State 需家用端核 WP201／WP604／WP101 live schema。
- [ ] 家用端先核 live PostgreSQL 是否已有出庫三表；若缺少再產 CREATE-only migration，原子處理 Inventory／Lot／Locator／Component booking、DataStamp 與 MO 驗證。出庫確認／扣庫存仍由獨立 IV401 確認流程負責。
- [ ] Git sync pending：恢復 `.git` 寫入及 auto-review 後，fetch／比對 remote 並保留家用端變更，再正常提交／push所有待同步模組，包含 IV403；不可 force push。
- [x] 公司端未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署、未執行 build/test/browser。接著 IV404。

## 公司端最新交付：IV401 半成品庫庫房資料（2026-11-25）

- [x] 核對 `frmINVSEMIInventory` inline Form／Designer、Inventory Basis／SEMI Inventory／Component／GoodBin／Bin loaders 與 Core 查詢；InventoryType=2／IssueState=2，主列 LocQty>0。
- [x] 完成 `/YMES/INV/SemiInventory` DEV InventoryNo Lookup／Enter 查詢、庫存主表、選列 Component 優先／GoodBin fallback、按 BinOrder 動態 Bin 欄。
- [x] 新增 [IV401 規格與家用端 API／schema handoff](modules/INV/半成品庫庫房資料_IV401.md)，接入 App 選單／路由並更新 Module Index、MES menu progress 及兩端待辦。
- [x] IV402 的 `SEMI PRODUCT IN` FunctionKey 另接至共用 `/YMES/INV/SemiProductIn` DEV 入庫頁，引用 [IV502／IV402 規格](modules/INV/開立成品半成品入庫單_IV502.md)；Form 仍依來源預設 FGD radio，使用者可切 SEMI。
- [x] SEMI Inventory／Component／GoodBin 沿 WP201／WP604／QC409；`TBLSYSBIN` 沿 PD102 dictionary logical reference；`TBLINVINVENTORYBASIS` 同版 Oracle DDL／metadata 缺失，桌機 profile 不存在，未猜 CREATE。
- [x] 記錄舊 Core 字串串接、GoodBin fallback 只帶 LotNo、ProductName 僅 current／approved Product；SEMI In／Out 按鈕呼叫 IV404／IV403 確認 Form，非本頁寫入。
- [ ] 家用端核 live PostgreSQL／WP201／WP604 references，實作 Inventory ACL、bounded main query 與完整 Component key；確認 Basis／SYS Bin 表版本，IV403／IV404 API 由各自規格處理。
- [ ] Git sync pending：恢復 `.git` 寫入及 auto-review 後，fetch origin/main、比對並保留家用端變更，再正常提交／push IV503／IV212／IV204／IV209／IVB01／IV301／IV305／IV306／IV307／IV401／IV402；不可 force push。
- [x] 公司端未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署、未執行 build/test/browser。接著掃描 INV 清冊尚未復刻功能。

## 公司端最新交付：IV307 在製貨架歷程查詢（2026-11-25）

- [x] 核對 `frmIV_StockerHistory` inline Form／Designer、`wsINV.LoadINVStockerHistory/LoadStockerNo`、`clsInventory` 查詢及 StockOn／StockOff 歷程語意。
- [x] 完成 `/YMES/INV/StockerHistory` DEV，含 Stocker／Lot／Carrier exact、可選 TimeInterval、當班 07:30 起始與新到舊歷程 Grid。
- [x] 新增 [IV307 規格與家用端 API/schema handoff](modules/INV/在製貨架歷程查詢_IV307.md)，接入 App 選單／路由並更新 Module Index、MES menu progress、兩端待辦。
- [x] DetailLog 沿 WP902 DDL／metadata、Lot State 沿 WP101；Stocker Basis DDL／metadata 缺同版可信來源，profile 不存在，未猜 CREATE。
- [x] 記錄空條件／日期範圍可查全表、Core 字串串接、沒有 paging、Lot Join 只用 LotNo、LogGroupSerial 過濾未實作；DEV 加上反向日期防呆。
- [ ] 家用端核 live PostgreSQL／WP902 references，實作 ACL 保護、參數化查詢、分頁／上限、穩定排序與 Lot Join 唯一鍵；不要支援任意 XML additional filter。
- [ ] Git sync pending：恢復 `.git` 寫入及 auto-review 後，fetch origin/main、比對並保留家用端變更，再正常提交／push IV503／IV212／IV204／IV209／IVB01／IV301／IV305／IV306／IV307；不可 force push。
- [x] 公司端未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署、未執行 build/test/browser。接著掃描 INV 清冊其餘尚未復刻功能。

## 前項公司端交付：IV306 在製貨架查詢作業（2026-11-25）

- [x] 核對 inline Form／Designer、`wsINV.LoadINVstockerStateJoinQtyLimit`／`LoadINVStockerDetail`、`clsInventory` query；無自動查詢，四條件可選且可全部不帶，LotNo contains，其餘 exact。
- [x] 完成 `/YMES/INV/StockerState` DEV 結果 Grid、空／滿／選取格位圖與 Stocker In/Out 導 IV305；從 IV306 會傳 StockerNo 給 IV305 預選。
- [x] 新增 [IV306 規格及 API／schema handoff](modules/INV/在製貨架查詢作業_IV306.md)，更新 Module Index、公司接手檔、MES menu inventory 與兩端待辦。
- [x] Stocker State 沿用 WP302；Stocker Basis／Detail 缺可信同版 Oracle DDL／metadata，桌機 profile 不存在，未猜 CREATE SQL。
- [x] 已記錄舊 Core 無條件查詢可無上限全表、字串串接，Stocker／Area Lookup 由現況 distinct 而漏空貨架；正式 API 需參數化、權限範圍、穩定排序／分頁，格位圖以 Detail + State 組合。
- [ ] 家用端核 PostgreSQL live schema／WP302／IV103 references，實作 ACL 保護、bounded query／lookup／Detail API；IV305 操作需重查目前 Stocker 狀態。
- [ ] Git sync pending：恢復 `.git` 寫入及 auto-review 後，fetch origin/main、比對並保留家用端變更，再正常提交／push IV503／IV212／IV204／IV209／IVB01／IV301／IV305／IV306；不可 force push。
- [x] 公司端未連 PostgreSQL、未新增 schema／migration、未實作正式 API、未部署、未執行 build/test/browser。IV307 已接續完成，詳上方 checkpoint。

## 接手方式

1. 在桌電開啟同一個 GitHub repository `souaix/YMES`，執行 `git pull origin main`。
2. 先讀本檔、`todolist_officepc.md`、`todolist_homepc.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md` 與 [MES 選單清冊](MES_MENU_FUNCTION_INVENTORY.md)，再直接核對尚未復刻功能的 WinForms／Service／Core 原始碼。
3. 本機 Codex 使用 YMES repo；MES WinForms 原始碼在公司筆電 `D:\iMES\CSiMES`，桌電可能是另一個路徑。若桌電有既有 MES checkout，先核對路徑；不可用 UI 截圖代替 Form/Designer/Service/Core 原始碼。原始碼缺少時記錄具體缺檔，先做不依賴它的工作。
4. 若在桌電打開同一個 Codex 對話，thread 對話與 Goal 脈絡仍在；若另開新對話，把本檔及 TODO 當成接手上下文，不要假設新 thread 自動有舊 Goal 狀態。

## Repository 與分工

- Repository：`D:\YMes`（桌電可用不同 clone 路徑）；remote `https://github.com/souaix/YMES.git`，branch `main`。
- Vue 3 UI：`web/`。
- .NET 9 API：`src/YMes.Api/`。
- 共用 DTO：`src/YMes.Contracts/`。
- `src/YMes.Web/` 是既有 ASP.NET MVC 專案，不是目前 Vue UI。
- 公司端以 DEV 假資料完成 Vue 畫面/互動並記錄規格；家用端負責 PostgreSQL schema、API、migration/Create SQL handoff。DEV localStorage 不送入正式 API。

### 桌機與筆電的職責

- 公司桌機和公司筆電屬於同一個「公司端」，工作範圍相同：讀 MES WinForms/Service/Core、只讀查 Oracle MESTEST DDL/metadata、以 DEV 假資料完成全部 Vue UI/互動邏輯、整理規格與 API/SQL handoff。
- 公司端不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不建 PostgreSQL schema/migration、不實作或驗收家用端 API、不 SSH 或遠端部署。這些全部由家用電腦接手，依 `todolist_homepc.md` 執行。
- 公司桌機為 Vue DEV 安裝 Node.js 22.x（22.12+）與 npm，並安裝 `web/` 套件即可。桌機上的 .NET 9 SDK 和 API/PostgreSQL 連線不是公司端 UI 工作的前置條件；不要為了跑 API 在公司電腦設定 PostgreSQL 憑證。
- 公司端允許使用的是 MES 原生 Oracle MESTEST 受保護唯讀連線，僅用於查 DDL/資料字典；不在 Oracle 寫入資料。

## 目前檢查點與下一步

- **2026-11-25 INV IV305 接續點**：`frmStockManagement`／Designer、`wsINV.LoadStockerBasis/LoadINVStockerDetail/LoadStockerState/StockOn/StockOff`、`clsInventory` 已核對；完成 `/YMES/INV/StockerManager` DEV 格位圖、Lot／Carrier 上下架假操作與[規格](modules/INV/在製貨架上下架作業_IV305.md)。舊 Form 依 CapacityCtrl／QtyLimit 前端擋滿格，但 Core 無併發容量重驗；Core 限 Lot Status 1／11／12／99，StockOn Carrier Basis 須 IssueState=2、StockOff 未對稱檢查；現況＋Detail Log 之後才由 Service 寫 EventLog。StockerState／DetailLog／Lot／Carrier 沿 WP302／WP902／WP101／Carrier references，Stocker Basis／Detail 的可信 DDL／metadata 缺失；受保護 Oracle profile 不存在，未猜 CREATE。未連 PostgreSQL、未 build/test/browser。接著掃描其餘尚未復刻且可查證來源的 INV 功能。
- **2026-11-25 INV IV301 接續點**：inline `frmINVWIPInventory`／Designer、`wsINV.LoadWIPInventory_All`、`clsInventory.LoadWIPInventory_All`、Component／GoodBin 的 `wsWIP`／`clsLot` loaders 及 Inventory／SYS Bin Lookup 已核對；完成 `/YMES/INV/WipInventory` DEV RAW、SEMI Lot／Component／Bin 四分頁及選列明細，[規格](modules/INV/在製品庫房資料_IV301.md) 已記錄。半成品查詢有三分組條件、日期需起訖同傳；舊 Detail 的 GoodBin 只以 LotNo 查、Component 無 Locator 條件，且舊 SQL 字串串接。RAW／SEMI 等依賴沿用 WP201、WP101、QC409、MTL／PRD references；`TBLINVINVENTORYBASIS`／`TBLSYSBIN` 同版 DDL／metadata 未找到，桌機受保護 Oracle profile 不存在，未猜 CREATE SQL。公司端未連 PostgreSQL、未 build/test/browser。接著掃描其他尚未復刻且可查證來源的庫存功能。
- **2026-11-25 INV IV301 接續點**：inline `frmINVWIPInventory`／Designer、`wsINV.LoadWIPInventory_All`、`clsInventory.LoadWIPInventory_All`、Component／GoodBin 的 `wsWIP`／`clsLot` loaders 及 Inventory／SYS Bin Lookup 已核對；完成 `/YMES/INV/WipInventory` DEV RAW、SEMI Lot／Component／Bin 四分頁及選列明細，[規格](modules/INV/在製品庫庫房資料_IV301.md) 已記錄。半成品查詢有三分組條件、日期需起訖同傳；舊 Detail 的 GoodBin 只以 LotNo 查、Component 無 Locator 條件，且舊 SQL 字串串接。RAW／SEMI 等依賴沿用 WP201、WP101、QC409、MTL／PRD references；`TBLINVINVENTORYBASIS`／`TBLSYSBIN` 同版 DDL／metadata 未找到，桌機受保護 Oracle profile 不存在，未猜 CREATE SQL。公司端未連 PostgreSQL、未 build/test/browser。接著掃描其他尚未復刻且可查證來源的庫存功能。
- **2026-11-25 INV IVB01 接續點**：Industry Base `frmINVWaferReceiving`／`frmINVWaferReceivingImport` inline Form／Designer、`wsINV_Base.LoadINVRAWInventoryData/DelINVRAWInventoryDetail/SaveTestSumData`、`clsInventory_Industry_Base` 已核對；完成 `/YMES/INV/WaferReceiving` DEV 三層 Raw／Locator／Wafer Test Summary、五條件查詢、假匯入／刪除互動與[規格](modules/INV/來料庫房管理_IVB01.md)。舊匯入依 Vendor AutoLoader Script 解析 CSV／Excel，直接建立 State=2 MaterialIn，更新 Raw／Locator／Component／TestSummary／Log；刪除會扣 Raw 並產生 State=2 MaterialReturn。Core Join／Component 刪除漏 LocatorNo、Capacity 只比本次匯入數，正式端須按完整 Locator 鍵重驗，upload→preview→commit 原子寫入。Industry Base Component／MaterialIn／Return／Temp／動態 TestSummary DDL／metadata 未能核實；受保護 Oracle profile 不存在，未猜 CREATE SQL。WAFERBANK vendor DLL 未處理。公司端未連 PostgreSQL，未 build/test/browser。接著掃描其他尚未復刻且有來源的 INV 功能。
- **2026-11-25 INV IV209 歷史接續點**：inline `frmINVMaterialAdjustLog`／Designer、`wsINV.LoadINVMaterialAdjustBasis/Detail`、`clsInventory` 已核對；完成 `/YMES/INV/MaterialAdjustLog` DEV 唯讀查詢與[規格](modules/INV/庫存調整單記錄_IV209.md)。查詢支援單號／原料庫房／State／CreateDate／INV Date；後者以 `TBLINVTransactionLog.Source=3` TicketNo／CreateDate 篩選，不等於 Basis.ConfirmDate。選取 Basis 載入 Detail／Component。舊 Core 空父單號 Detail 可回整表、Component DataRelation 僅用 MaterialNo，家用端須父單號必填並依 PostgreSQL live 複合鍵建立關聯。調整 Basis／Detail／Component Oracle DDL／metadata 缺失，桌機受保護 Oracle profile 不存在，未猜 CREATE SQL；TransactionLog 沿用 WP605 reference。未連 PostgreSQL、未 build/test/browser。接著依清冊掃描尚未復刻且來源可用的 INV 功能。

- **2026-11-25 INV IV204 歷史接續點**：`frmINVMaterialAdjust`／BasisDef／DetailDef inline Form／Designer、`wsINV`、`clsInventory` 與 IV201 的 `frmINVMaterialAdjustConfirm`／`ExeMaterialAdjust` 已核對；完成 `/YMES/INV/MaterialAdjust` DEV 主／明細 State=0 CRUD、數量調帳／儲位調撥、Raw Lot／Locator 假 Lookup 及家用端 handoff，詳[規格](modules/INV/開立庫存調整單_IV204.md)。IV204 僅維護草稿，真實過帳由 IV201 Source=3 負責。數量調帳更新 Raw CurQty／LocQty；Locator 調撥會重查未確認領料、目的容量，再以 OrgQty 移轉。舊容量檢核只看首筆且將容量轉整數，Component DataRelation／刪除不完整；正式 API 須核 live PostgreSQL schema、參數化並在原子交易中重驗狀態／庫存／容量及同步 Component。調整 Basis／Detail／Component、Raw Inventory／Raw Component、MaterialOut DDL／metadata 未能從 repo 或桌機受保護 Oracle profile 取得，未猜 CREATE SQL；LocatorBasis 沿用既有 reference。公司端未連 PostgreSQL，未執行 build/test/browser。IV209 已接續完成，詳上項。

- **2026-11-25 INV IV212 歷史接續點**：inline `frmINVRaw_Acclimation`、`wsINV.LoadWIPInventory_Raw/Edit_IVN_Raw_Split/SetInventoryRawFrozen`、`wsMTL`／`wsWIP_Material`、`clsInventory`／`clsMTLBasis`／`clsMaterialHandler` 已核對；完成 `/YMES/INV/RawAcclimation` DEV 與[規格](modules/INV/回溫回冰作業_IV212.md)。查詢使用 GumMaterialType、未過期且 Qty>0 的原料批；回溫檢查核准類別、FrozenDate+FROZENMIN；回冰檢查設備 QTY、REFROZENFLAG／次數限制。DEV 假設 `MATERIALMULTITIMECTRL=1`，資料只在本頁記憶體更新。Raw 沿用 WP201、TBLMTLTYPEACCLIMATION 沿用 MTL master、設備上料狀態沿用 WP301；`TBLINVWIPINVENTORY_RAWLOG` 原生 DDL／metadata 缺失，筆電的受保護 attachment 路徑在桌機不存在，未猜 CREATE SQL。舊 Core 的 UPDATE 沒有 LocatorNo（Raw PK 含 LocatorNo），設定查詢也未明定物料專屬／`*` 優先序；家用端需先核 live schema，採完整鍵、狀態版本、參數化與 Raw＋RawLog 原子交易。公司端未連 PostgreSQL，未執行 build/test/browser。IV204 已接續完成；下一項依清冊 IV209「庫存調整單記錄」。

- **2026-11-24 INV IV503 歷史接續點**：`frmINVProductInLog` inline Form／Designer、`wsINV.LoadINVSEMIInAndFGDIn`／FGD／SEMI Detail+Component loaders 與 `clsInventory` 已核對；完成 `/YMES/INV/ProductInLog` 唯讀 DEV 與[規格](modules/INV/成品入庫單記錄_IV503.md)。查詢合併 FGD／SEMI Basis，支援單號、目的／來源庫房、State、CreateDate／InputDate 與選列 Lot／Component 子列；InputDate 在舊 Form 顯示為 ConfirmDate，VOID 的 SelectedIndex bug 由 DEV 使用實際 State=-1 修正。FGD／SEMI 表沿用 WP605／WP604，其他 Lookup 沿用 Inventory／Product／ComponentProperty refs，無新 DDL。家用端需核 live schema 與 FGD Detail／Component 複合鍵；舊 Core 空父單號會回全表，正式 API 必須父單號限定、參數化、穩定排序及分頁。未連 Oracle／PostgreSQL，未執行 build/test/browser。後續 IV212 已完成；目前下一項 IV204「開立庫存調整單」。
- **2026-11-24 INV IV503 接續點**：`frmINVProductInLog` inline Form／Designer、`wsINV.LoadINVSEMIInAndFGDIn`／FGD／SEMI Detail+Component loaders 與 `clsInventory` 已核對；完成 `/YMES/INV/ProductInLog` 唯讀 DEV 與[規格](modules/INV/成品入庫單記錄_IV503.md)。查詢合併 FGD／SEMI Basis，支援單號、目的／來源庫房、State、CreateDate／InputDate 與選列 Lot／Component 子列；InputDate 在舊 Form 顯示為 ConfirmDate，VOID 的 SelectedIndex bug 由 DEV 使用實際 State=-1 修正。FGD／SEMI 表沿用 WP605／WP604，其他 Lookup 沿用 Inventory／Product／ComponentProperty refs，無新 DDL。家用端需核 live schema 與 FGD Detail／Component 複合鍵；舊 Core 空父單號會回全表，正式 API 必須父單號限定、參數化、穩定排序及分頁。未連 Oracle／PostgreSQL，未執行 build/test/browser。IV212 已完成；目前下一項 IV204「開立庫存調整單」。

- **2026-11-24 INV IV502 接續點**：共用 `frmINVProductIn`／`frmINVProductInDef` 已完成 `/YMES/INV/ProductIn` DEV 與 [規格](modules/INV/開立成品半成品入庫單_IV502.md)。直接核對 FGD／SEMI Add／Edit／Delete／combined Load Service、`clsInventory`、`clsBasis_UPD`。DEV 支援 FGD 預設與 SEMI 切換、State=0／Source=1 草稿查詢、來源 WIP 半成品 Lot／Component 選取及假預約、完整最高層包裝子 Lot 檢核、DataStamp 編輯、作廢保留明細／解除預約和列印預覽。建立草稿不執行庫存過帳；IV501 Source=30 確認才過帳。
- FGD In 表沿用 WP605、SEMI In 表沿用 WP604；WIP Semi／Component、Lot、ReverseHistory、Package、Locator 使用 WP201／603／101／215／601／PRD112 refs，無需新增 Oracle DDL／CREATE-only。本家用端已以 0074 migration 建立經 live 確認缺少的 15 張來源表；完整備份、欄位／鍵／索引／權限回讀見 [資料庫連線紀錄](../DATABASE_CONNECTION.md)。本機已新增登入保護的 IV502 列表／父單號明細／來源 Lot Component lookup 唯讀 API 並通過 .NET 9 build，尚未部署或接正式頁面；建立／編輯／作廢仍待實作。草稿交易仍須重驗 Qty、BookingFlag、Component、包裝完整性、DataStamp；作廢是 State=-1、釋放預約並保留明細。公司端下一項依清冊為 IV503「成品入庫單記錄」。

- **2026-11-24 INV IV304 接續點**：`WIP RETURN CONFIRM` 已依 inline `frmINVWIPReturnConfirm`／Designer、`wsINV.AddINVTransactionState/ExeINVTransactionState`、`clsInventory.ExeMaterialWIPReturn_ForMaterialWIPMOReturn` 完成 `/YMES/INV/WipReturnConfirm` DEV 與[規格](modules/INV/退料單確認_WIP.md)。DEV 模擬 State=0 查詢、Detail／Component 展開與單筆 Source=10 確認。Core 依 FromPlaceType 2／3／5／6／7 扣 WIP／MO source、回補 Raw／Semi 與元件庫存；Form 使用的 Add Service 會吞掉內部 Exe 失敗，Core 忽略 ToLocatorNo，Type 7 多目標分流需逐鍵核實。桌機受保護 Oracle profile 不存在；退料單及 Raw Inventory／Locator／Component DDL／metadata 未取得，不猜 CREATE。沿用 repo WP201／603／604／605 與 OE references。未連 YMES PostgreSQL、未 build/test/browser；下一項依清冊為 IV310「包裝拆並箱作業」。
- **2026-11-24 INV IV303 歷史接續點**：`WIP MATERIAL RETURN LOG` 已依 inline `frmINVMaterialWIPReturnLog`／Designer、`wsINV.LoadINVMaterialWIPReturnBasis/Detail/Comp`、`clsInventory` 完成 `/YMES/INV/MaterialWipReturnLog` DEV 與[規格](modules/INV/退料單記錄_WIP.md)。含 MaterialReturnNo、InventoryNo、FromPlaceType、State、來源 WIP／MO、CreateDate／ReturnDate 篩選及選列明細／Component 子列；ReturnDate 由 Basis.ReturnDate 映射成畫面的 ConfirmDate。表 DDL／metadata 因桌機沒有受保護 Oracle profile 而未取得，不猜 CREATE。Core Detail 空單號可能回全表；Component 查詢以最後一筆明細 LocatorNo 過濾且關聯缺 MaterialReturnNo／LocatorNo，正式 API 必須父單號限定、完整關聯、穩定排序及分頁。當時下一項依清冊為 IV304「退料單確認(WIP)」。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV302 歷史交接**：`WIP MATERIAL RETURN` 已依 `frmINVMaterialWIPReturn`／BasisDef／DetailDef、`wsINV`、`clsInventory`／`clsBasis_UPD` 完成 `/YMES/INV/MaterialWipReturn` DEV 與[規格](modules/INV/開立退料單_WIP.md)。含 State=0 單頭／明細維護、五種 FromPlaceType、WIP／MO 假來源、來源／退入 Locator、元件選取與 BookingFlag 假預約。單頭 Copy 不複製明細；Basis Edit 僅 Description。IV302 草稿不執行實際退料，確認在 IV304 Source=10。桌機受保護 Oracle profile 不存在，`TBLINVMATERIALWIPRETURNBASIS`／`TBLINVMATERIALWIPRETURNDETAIL`／`TBLINVMTLWIPRETURNCOMP` 與元件來源 DDL／metadata 未取得，不猜 CREATE。舊 Core 空 MaterialReturnNo Detail 查詢可回全表；Service 分別更新 Detail、Component 及 BookingFlag，正式 API 必須父單號限定並原子交易更新。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV211 接續點**：`MATERIAL RETURN LOG` 已依 inline `frmINVMaterialReturnLog`、`wsINV.LoadINVMaterialReturnBasis/Detail`、`clsInventory` 完成 `/YMES/INV/MaterialReturnLog` DEV 與[規格](modules/INV/退料單記錄.md)。含 8 組勾選條件、State 0–3、Basis ReturnDate／CreateDate 日期、選列載入 Detail／Component 子列。ReturnDate 是 Basis.ReturnDate，Form 顯示為 ConfirmDate，不用 TransactionLog 替代。桌機受保護 Oracle profile 不存在，Return Basis／Detail／Component DDL／metadata 未取得，不猜 CREATE。舊 Core 空單號 Detail 回全表、Component relation 少 LocatorNo、Basis 無排序／分頁已交家用端。未連 YMES PostgreSQL、未 build/test/browser。
- **2026-11-24 INV IV206 接續點**：`MATERIAL RETURN` 已依 `frmINVMaterialReturn`／BasisDef／DetailDef、`wsINV`、`clsInventory`／`clsBasis_UPD` 完成 `/YMES/INV/MaterialReturn` DEV 與 [規格](modules/INV/開立退料單.md)。主／明細 State=0 CRUD、假 Vendor／Raw Lot lookup、元件 booking 本機模擬均已交接。桌機指定 Oracle profile 不存在，因此 Return Basis／Detail／Component、Raw Component／Locator 原生 DDL／metadata 未取得，不猜 CREATE。舊 Service 元件寫入分步、Core Edit 可能刪除整單元件、元件候選缺 InventoryNo 的來源差異已交家用端；正式 API 必須完整鍵、庫房範圍及原子交易。IV206 草稿不扣庫存，確認由 IV201 Source=3。公司端下一項依清冊為 IV211「退料單記錄」。未連 YMES PostgreSQL、未 build/test/browser。
- 公司端已接續 OE101、OE102、OE201 至 OE209、OEB01、OEP01 的 DEV 訂單與工單操作。各功能及 Oracle DDL／API 交接見模組索引。WP101、WP102、WP201、WP202、WP204、WP301、WP302、WP601、WP603、WP604、WP605、WP629 已完成公司端 DEV 與家用端 handoff；後續工作依 `todolist_officepc.md` 及 MES menu inventory 接續。
- **2026-11-20 WIP 接續檢查點：WP305–WP307 及 WP203／WP103／WP113／WP104／WP105／WP205–WP207 的公司端 DEV/spec/handoff 已同步至 `D:\YMes` 工作樹。** 同步前逐檔比對、於 `D:\iMES\CSiMES\ymestage\wp207_sync_backup_20261120` 備份七個有差異的既有檔案並核實 SHA256；新增檔與既有 WP305 未提交檔均已保留。WP307 的 State/History ShiftNo／UserNo lookup 與正式 API contract 已列於規格與 `todolist_homepc.md`。
- Oracle 僅以受保護 MESTEST profile 讀取 WP307、WP206–WP209、WP213、WP215 等結構字典，未查業務資料、未執行 DDL/DML。source DDL、metadata、CREATE-only 與兩端 TODO 均在工作樹。公司端仍未連 YMES PostgreSQL、未實作 Production API/migration，也未執行 build/test/browser；WP208–WP215 公司端 DEV／規格已完成，後續依 menu inventory 選下一項。
- **2026-11-20 同步檢查點**：公司端 WP207 以前的工作保留家用端 14 筆遠端提交，經 rebase 以 `b3f0f06` 正常推送 GitHub main。WP208 沿同一版本建立 staging；整合時須再次核對遠端與家用端新異動，逐檔備份／比較，不 force push。WP208 Core 直接引用 27 張表；其中 `TBLSYSPARAMETER` 原先缺 SQL reference，已由公司端 PowerShell 5.1／受保護 Oracle profile 唯讀取 DDL／metadata並補 CREATE-only，其餘 26 表沿用先前 refs。家用端須實作元件守恆的交易 API。
- **2026-11-20 WP208／WP209 接續**：WP208 已以 `dbebd0f` 正常推送 main。WP209 `LOT SPLIT_BIN` 已依 WinForms／Service／Core 完成 Vue DEV、規格與 Oracle 唯讀 DDL／CREATE-only 交接，路由 `/YMES/WIP/LotSplitBin`。`TBLWIPSPLITCONTENT_BIN` 已查得完整結構；`TBLWIPSORTERBINDATA` 在目前 MESTEST `ALL_TABLES`／`ALL_OBJECTS`／`ALL_SYNONYMS` 均不可見，只影響 Core 的 LED_PKG 分支，待正確 DDL，不猜表結構。家用端負責 live PostgreSQL 核對、交易 API 與正式驗收。公司端下個 menu 功能依 inventory 查 WP210；WP209 本輪未執行 build/test/browser。
- **2026-11-20 WP210 接續**：WP210 `LOT SPLIT_CARRIER` 已依主 Form、元件 Slot picker、Service 與有／無元件兩條 Core 完成 Vue DEV 與 API 規格，路由 `/YMES/WIP/LotSplitCarrier`。兩條 Core 直接引用 17 表，逐名核對均已有 CREATE TABLE reference，沒有新缺表需 Oracle DDL／CREATE-only；家用端仍要核 live PostgreSQL、追 helper 間接表並完成載具／元件 Slot 一致交易。原 Change Product 按鈕未傳產品欄的來源差異見模組 MD。下個 menu 功能為 WP211「併批處理」。
- **2026-11-20 WP211 接續**：WP211 `LOT MERGE` 已依 `frmBR_L_Merge_GEN`、`Exe_Manual_LotMerge` 與 `BR_Manual_LotMerge` 完成 Vue DEV、`/YMES/WIP/LotMerge`、規格與家用端 API handoff。八張直接表均有既存 source DDL／CREATE-only；沒有新 SQL。來源中至少兩批的檢查已註解，MO 跨工單由 `AllowDiffMOMerge` 控制，OS、包裝、GoodBin 與 BR 後續轉站分支見模組 MD。下個 menu 功能為 WP212「併批處理_元件」。
- **2026-11-20 WP212 接續**：WP212 `LOT MERGE_COMP` 已依獨立 WinForms、Service、Core 完成 `/YMES/WIP/LotMergeComponent` Vue DEV、右側元件 Grid、至少兩批與載具守門，並交付規格及家用端 API handoff。十張直接表均有 source DDL／CREATE-only，無新 SQL。Form 的載具檢查迴圈實際只查 `txtLotNo`，正式 API 須逐來源批檢查，詳模組 MD。下個 menu 功能 WP213「併批處理_等級」。
- **2026-11-20 WP213 接續**：WP213 `LOT MERGE_BIN` 已依獨立 WinForms／Service／Core 完成 `/YMES/WIP/LotMergeBin` Vue DEV、SysBin 動態欄、GoodBin 參考批、全選、Go／Assign 與家用端規格。Core 八張直接表中七張沿用現有 refs，`TBLWIPMERGECONTENT_BIN` 以受保護 Oracle profile 唯讀取得 22 欄 source DDL／metadata，並提供 CREATE-only。家用端須核 live PG、實作 Bin/GoodBin 交易；下個 menu 功能 WP214「併批處理_載具」。
- **2026-11-20 WP214 接續**：WP214 `LOT MERGE_CARRIER` 已依 `frmBR_CL_Merge_GEN`／`frmCarrierMerge_Comp`、Service／Core 完成 `/YMES/WIP/LotMergeCarrier` Vue DEV，一般載具合併與有元件 Slot 移動都在頁內。11 張 Core 直接表均已有 source DDL／CREATE-only，無新 SQL；原 Form 外包 helper 錯傳 CarrierNo、Core 同 OS AND 條件疑點已交接。家用端核 live PG、實作載具／Slot／元件一致交易。下一個 menu 功能 WP215「元件分片處理」。本輪未執行 build/test/browser。
- **2026-11-20 WP215 接續**：WP215 `LOT COMP SPLIT` 已依 `frmSplitComponent`、`wsWIP.InitRunSplitComponentNo`／`EXE_SplitComponent`、`clsWIP` 核對並完成 `/YMES/WIP/ComponentSplit` Vue DEV、2–26 元件預覽、良／報廢量守恆、載具拒絕與家用端 API 規格。14 張 Core 直接表中 12 張沿用 refs；公司端用受保護 MESTEST 唯讀取得 `TBLWIPCOMPONENTBASIS`（32 欄）與 `TBLWIPREVERSEHISTORY`（11 欄）source DDL／metadata，提供 CREATE-only。家用端核 live PG 再做缺口 migration／原子編號與交易。原 CarrierNo 欄隱藏且 Core 新元件上載具已註解。未執行 build/test/browser。
- **2026-11-20 WP914 接續**：`LOT HOTLOT AND PRIPORITY CHANGE` 已核 WinForms Form／Designer、`wsWIP.EditLotBasis_Confirm`、`clsLot.EditLotBasis`，完成 `/YMES/WIP/HotLotPriority` Vue DEV 查詢／勾選／掃描／HotLot 與 Priority，規格與家用端 API handoff 見模組索引。只更新 `TBLWIPLOTBASIS`，所需源表均有 SQL reference；無新 Oracle 查詢或 CREATE SQL。原 Form 逐列按 BaseLotNo 更新可能部分成功，正式批次應以版本／交易和去重處理。下個待處理群組為 WP501／WP502 等「異常與等待」；WP501 Set 會開 `WP_BRERFCreate` 約 5,000 行共用表單，須一併核對。未執行 build/test/browser。
- **2026-11-20 WP501 接續**：已核 `frmImmediateHoldSet`、共用 `frmBRERFCreate`、`wsWIP.Exe_ERF_Create`、`clsLot.UpdERF_Create`，完成 `/YMES/WIP/ImmediateHoldSet` Vue DEV 查詢、Set、異常單分類／原因／元件 Area／Confirm 本機模擬。受保護 MESTEST 唯讀查得 `TBLWIPERFCREATEDATA`、`TBLWIPERFHOLDREASON`、`BKTBLWIPERFCREATEDATA` 的 source DDL／metadata；另三張 ERF Basis／ReasonDisp／AttachFile 沿用 QC409 source，六表 CREATE-only 在 `mestest_wip_wp501_create.sql`。家用端核 live PG 再做正式原子交易。附件/PDF/通知與設備實際占用交易仍是 DEV 簡化；設備共用/獨佔假分支已在後續補核，詳下方記錄。未執行 build/test/browser。
- **2026-11-20 WP502 接續**：已核 `frmImmediateWaitSet`、共用 `frmWaitDescription`、`wsWIP.Exe_Wait_Set`、`clsLot.SetLotStateToWait`，完成 `/YMES/WIP/ImmediateWaitSet` Vue DEV 七欄查詢、15 欄勾選 Grid、Select All／掃描、彈窗說明與逐批假 Wait。原 Form 將多批 LotNo 逗號連接後送給只接受單值的 `CheckLotPackage`，正式端須逐筆查；原迴圈中途失敗可部分完成。WaitBasis 等七張 Core 直接表沿用現有 refs，無新增 CREATE。下一項可核 WP503 批次立即異常設定。未執行 build/test/browser。
- **2026-11-20 WP503 接續**：已核 `frmImmediateHoldSet_Batch`、專屬 `frmBRERFCreate_Batch`、`wsWIP.Exe_ERF_Create_Batch`、`clsLot.UpdERF_Create_Batch`，完成 `/YMES/WIP/BatchImmediateHold` Vue DEV 十欄＋時間查詢、16 欄多選 Grid、批次異常彈窗和 `ERFBatchHoldGenType` 0/1 假交易。0 為單 ERF 多批、寫 BatchLot；1 為逐批 ERFType2、不寫 BatchLot且可能部分成功。BatchLot Oracle source 沿用 QC409，本批補單表 PG CREATE-only。Core 的 FutureHoldLog/Reason 僅 ERFType3 使用，已有公司端受保護 Oracle 唯讀結構供下一個 Future Hold 規格；不列 WP503 migration。詳見模組規格；未執行 build/test/browser。
- **2026-11-20 WP504 接續**：已核 `frmFutureHoldSet`、`wsWIP.Exe_FutureHold_Set`／`CheckLotPackage`、`clsLot.UpdFutureHold_Set`／`ChkFutureHold`，完成 `/YMES/WIP/FutureHoldSet` Vue DEV 六欄 Query、八欄 Lot Grid、Selected Lot/掃描、PS→OP 多選、Start/End、QC 原因與假 Set。原 Form 多批包裝檢查有逗號單值缺陷；Core 按 Lot×OP 替換 FutureHold/Reason，Set 不立即 Hold。WP503 階段唯讀查到的 Reason/Log DDL/metadata 已交付，PG CREATE-only 保留 Log 無 PK。規格與家用端 API/生命週期 handoff 見索引；未執行 build/test/browser。下一項依 inventory 可處理 WP505 預先等待設定、WP506 預先異常解除。
- **2026-11-20 WP505 接續**：已核 `frmFutureWaitSet`、`wsWIP.Exe_FutureWait_Set`／`CheckLotPackage`、`clsLot.UpdFutureWait_Set`／`ChkFutureWait`，完成 `/YMES/WIP/FutureWaitSet` Vue DEV 六欄 Query、六欄 Lot Grid、Selected Lot/掃描、PS→OP 多選、WaitTime Start/End、可空說明與假 Set。原 Form 多批包裝檢查同樣有逗號單值缺陷；Core 按 Lot×OP×WaitTime 交易替換 FutureWait，不立即 Wait。FutureWait source/CREATE 沿用 WP201，無新 SQL；下一項 WP506/507 Release。未執行 build/test/browser。
- **2026-11-20 WP506 接續**：已核 `frmFutureHoldRelease`、`wsWIP.DelFutureHold`、`clsLot.DelFutureHold`，完成 `/YMES/WIP/FutureHoldRelease` Vue DEV 四種勾選查詢、九欄 Grid、單列 Release/Close，並讀寫 WP504 共用假資料。來源只檢選取列 LockFlag，Core 以 Lot/OP/HoldTime 刪主表與原因，可能刪同鍵其他 Module/Serial/鎖定列；DEV 先檢整個實際刪除範圍。資料 DDL/CREATE 沿用 WP201/WP504，無新 SQL；正式 API 的精確或批量契約交家用端。下一項 WP507 預先等待解除。未執行 build/test/browser。
- **2026-11-20 WP507 接續**：已核 `frmFutureWaitRelease`、`wsWIP.DelFutureWait`、`clsLot.DelFutureWait`，完成 `/YMES/WIP/FutureWaitRelease` Vue DEV Lot/OP/WaitTime 勾選查詢、六欄 Grid、單筆假 Release，與 WP505 共用 localStorage。原 Form 的 Lot 查找、Find 均隱藏；Service 傳完整三鍵，Core 通用方法若缺鍵可擴大 DELETE，正式 API 必須強制三鍵。FutureWait source/PG CREATE 沿用 WP201，無新 SQL；下一項 WP508 ERF Basis。未執行 build/test/browser。
- **2026-11-20 WP508 接續**：已核 `frmERFBasis`／`wsWIP.LoadERFBasisJoinLotBasis`／`clsLot.LoadERFBasisJoinLotBasis`，完成 `/YMES/WIP/ErfBasis` 六條件＋日期、batch member 查詢、25 欄 Grid 與 8D 待接提示。追 `WP_ERFInformation` 後更正「只讀」描述：附件 Grid 有 View/Delete，會保護異常單產生檔；DEV 已補五頁欄位與假附件操作。受保護 MESTEST 唯讀取得 8D Basis 43 欄及 LotDisposition 16 欄/PK/索引 DDL/metadata，交 PG CREATE-only；OCAP 附件源 DDL 沿 QC409，CREATE 補在 WP508info。正式五頁真資料/附件 API 與 WP801 8D 工作流待家用端；公司端接著追 WP801 source。未執行 build/test/browser。
- **2026-11-20 WP801 接續**：已追 `frmWP8DBasisFactory`／Def、Service/Core/流水號；`/YMES/WIP/8DBasisFactory` Vue DEV 有四狀態/QRSNo、40 欄、ADD/EDIT/DEL/Copy/Approve/ERFINFO，與 WP508 無 QRSNo 的假 ERF 建立關聯。主表 DDL 沿 WP508，Oracle 唯讀補兩張 SerialRule source DDL/metadata；原 first_phase PG CREATE 漏五個 Oracle default，新交準確 CREATE-only。公司端未連 PG、未 build/test/browser。家用端負責真序號/ERF 原子交易、Issue Job、API/遷移；公司端下一項 WP802 8D 維護來源。
- **2026-11-20 WP802 接續**：已追 `frmWP8DMaintain`、`frmWP_8DMaintainDefine`、子 Form 入口與 `wsWIP`/`clsWIP`；`/YMES/WIP/8DMaintain` Vue DEV 有 38 欄已簽核/未結案清單、Basis 至 D9 十頁及 D1–D7 明細/Verify/Track、D8 假總結、附件檔名與 WP508 有 QRSNo 直達。公司端用受保護 MESTEST 唯讀取得 12 張 8D 子表 source/index/metadata，另交 PG CREATE-only；TeamMember 是 unique index、無 PK constraint。專屬 Form/FishBone/附件 BLOB 詳情的 DEV 簡化見 `docs/modules/WIP/8D矯正單維護作業.md`。家用端接真 API/PG，下一項公司端 WP803 結案作業；未連 PG、未 build/test/browser。
- **2026-11-20 WP803 接續**：已追 `frmWP8DClose`／Def、`wsWIP`/`clsWIP`；`/YMES/WIP/8DClose` Vue DEV 有四狀態、五條件、九欄、結案編輯、D1 lookup、多選 Approve 及 8DQuery 唯讀。Release/ReleaseLog/TeamMember/Basis 沿 WP802/WP508；另唯讀取得部門表 14 欄 Oracle DDL/metadata，PG CREATE-only 補原 bootstrap 四個 default。WinForms Pending 提前結案、工作流 SQL 改錯表與 ActiveRow 錯列詳見 `docs/modules/WIP/8D矯正單結案作業.md`；家用端須用原子交易/正確 Issue 表接正式 API。當時下一項 WP804 已完成；未連 PG、未 build/test/browser。
- **2026-11-20 WP804 接續**：已追 `frmWP8DQuery`、`wsWIP.Load8DBasisFactory`、`clsWIP.Load8DBasisFactory`；`/YMES/WIP/8DQuery` Vue DEV 十組條件、39 欄跨狀態查詢、ERFNo 外部參數與 WP802 唯讀十頁。來源只勾 WIPERF/Other 會漏 QRSClass 條件，已於 DEV 修正並在 `docs/modules/WIP/8D矯正單查詢.md` 記錄。主表 Oracle source/PG CREATE-only 沿 WP508，無新表；家用端接分頁真 API。未連 PG、未 build/test/browser。
- **2026-11-20 WP501 Release EQP 補核**：來源 Form → wsWIP → clsLot 確認 0 行警告無法阻擋提交，且把無設備與其他 Lot 共用混在一起。公司 DEV 已補 Vendor/Material、數字 EquipmentState 選單、共用／獨佔設備兩種假分支與原 v1 storage 增量升級。`TBLWIPTEMP_EQUIPMENT`、`TBLEMSEQUIPMENTSTATE`、`TBLEQPSTATEBASIS` 都有既有 reference，無新增直接表。詳 `docs/modules/WIP/立即異常設定.md` 和 home TODO；正式 API 必須在交易中驗證設備占用。接續 inventory 其他未完成 WIP 功能。未連 PG、未 build/test/browser。
- **WP203「過帳設備設定」（`EQP FAVORITE SETUP`）公司端 DEV 與 handoff 已完成。** 原始來源為 `STD/MESWin/WIP/WP_EquipmentFavorite/frmEquipmentFavorite.vb`、`frmEquipmentFavoriteDef.vb`、`STD/MESws_STD/wsSTD/wsEQP/wsEQP.cs`、`STD/MESkc/kcEQP/clsEQPBasis.cs`。重用 WP204 的 `TBLEQPFAVORITE` source DDL/CREATE-only；沒有新增 Oracle table。HostId 是 workstation-scoped，Production API 不得信任瀏覽器傳值。
- **WP103「列印流程卡（RUN CARD）」公司端 DEV 與 handoff 已完成**，規格為 `docs/modules/WIP/列印流程卡.md`，FunctionKey `WP103` 路徑 `/YMES/WIP/RunCard`。來源 Form/Service/Core 和 RunCardStyle 0/非 0、Lot 查詢、RunCard Log、Net Flow、Runcard template／Product RunCardMap 分支均已記錄。無新增專用表／DDL；DEV Preview 使用假 lot/流程，Print 不送出實體工作。Production API、PDF renderer、ProductMap/PD_Kit 移植與 PostgreSQL live schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP113「LOT SHEET PRINT」THEIL 專用頁已完成公司端 source trace、DEV 假資料頁與 API handoff**，規格 `docs/modules/WIP/LOT_SHEET_PRINT.md`，路徑 `/YMES/WIP/LotSheetPrint`。來源為 THEIL WinForms/Service/Core 與 ActiveReports；假資料包含 MO/建立日期篩選、Current/Original Process、Lot Sheet/Empty Version 預覽及不連印表機的 Print 模擬。重用既有 Lot Property 與 THEIL Matrix，無新增表／DDL。Production 查詢、報表 renderer 與 print count 原子更新交家用端依 `todolist_homepc.md` 執行。
- **WP104「使用者登入作業（上工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登入作業_上工.md`，路徑 `/YMES/WIP/OperatorClockIn`。已依 WinForms、wsUSR/wsOP/wsWIP_OperatorState、clsOperatorState 實作假員工、班別、OP mode、LoginPlace 與 auto-clockout 確認。Oracle 只讀新查 `TBLWIPOPERATORSTATE` 和 `TBLWIPOPERATORLOG`，DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp104_*`；重用 WP305 的 LotLoginState。Production credential verification、交易及 live PG schema 核對交家用端依 `todolist_homepc.md` 執行。
- **WP105「使用者登出作業（下工）」公司端 DEV 與 source/API handoff 已完成**，規格 `docs/modules/WIP/使用者登出作業_下工.md`，路徑 `/YMES/WIP/OperatorClockOut`。依 WinForms/Service/Core 模擬員工驗證、現況篩選、預設全選與逐列下工；沿用 WP104 OperatorState／OperatorLog 和 WP305 LotLoginState。沒有新增 Oracle 表或 DDL；Production API 與 PostgreSQL live schema 核對交家用端。
- **Lot Operator Clockin 批次上工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員上工.md`，路徑 `/YMES/WIP/LotOperatorClockIn`。頁面依 LotStatus/ClockStatus/PS/OP 查詢候選 Lot，預設全選，按 mode/location 去重後一次模擬批次上工，並與 WP104/WP105 共用假 OperatorState。沿用 WP104/WP305 Oracle references，無新 DDL。WinForms 來源只暴露 `PrivFunNo`，FunctionKey 未核實；fallback FunctionNo 是標題推導值，上游 menu metadata 待確認。
- **Lot Operator ClockOut 批次下工 DEV 與規格已完成**，見 `docs/modules/WIP/批次作業員下工.md`，路徑 `/YMES/WIP/LotOperatorClockOut`。頁面依預設 ClockStatus=Clocked In 查詢、可選員工篩選、同步勾選同 UserNo+LoginPlace rows，按來源批次去重模擬下工；共用 WP104/WP105 假狀態／歷程，沿用 WP104/WP305 refs，無新 DDL。FunctionKey 未核實，正式路由待 menu metadata 確認。
- **作業員 OP／Area 登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/作業員登入登出_OPArea.md`，路徑 `/YMES/WIP/OperatorLogin`。來源使用 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`，但公司端 MESTEST 所有可見 Owner/Synonym 查無這兩表；未製作推測 CREATE，家用端應先核 live PG schema，並取得正確 Oracle DDL 再決定 migration。
- **OPERATOR LOGIN_LOT 生產批人員登入登出 DEV 與規格已完成**，見 `docs/modules/WIP/生產批作業員登入登出.md`，路徑 `/YMES/WIP/OperatorLoginLot`。DEV 呈現 CallType=MenuItem，規格涵蓋 BR 呼叫端 1/2/3；共用 WP104 OperatorState/Log 與 WP305 LotLoginState，沒有新表／DDL。正式 FunctionKey 待 menu metadata 核實。
- WP301 設備上料已核對 `frmEQPMaterialLoading`／`wsWIP_Material`／`clsMaterialHandler`，完成物料、元件、半成品模式的 DEV 假資料頁。四張相依表 DDL／CREATE-only 在 `database/reference/mestest_wip_wp301_*`；其餘 13 張沿用既有 reference。未連 YMES PostgreSQL、未實作 Production API／migration。
- WP302 載具現況管理已核對 `frmCarrierState`／`wsEQP`／`wsWIP`／`wsINV`／`clsCarrierBasis`，完成 Vue DEV 放入、取出、清空及隱藏狀態操作。三張新查證表 DDL／metadata／CREATE-only 在 `database/reference/mestest_wip_wp302_*`；Lot／Component／CarrierType 等沿用既有 references。未連 YMES PostgreSQL、未實作 Production API／migration。

- PD102 DEV 子視窗已分段完成 Property、Parameter、Business Rule、OPDescription、RecipeGroup、Accessory、Resource、SubOP、Program、Default、RunTime、QC Item。各功能的 WinForms 證據、DDL 及 API handoff 在 `docs/modules/PRD/` 與兩份 TODO。
- RunTime 已完成單列／表格編輯與套用。QC Item 已完成依 OP 指派與檢驗類型，QC Import／QC Chart 的 Excel 流程待接續。兩項 DDL 依 MESTEST 字典實查重建，Production API 由家用端接續。
- Packing Set、EQPCapItem、eSOP、EquipmentSpec、APS Integration 和 Bin 已完成 DEV 設定與 Oracle 交接。`GBOMImport`／`GBOMExport` 已完成 DEV 備份操作與客製表交接，工作簿解析和資料匯入待正式接入。

## Default 已核實事項

- WinForms 入口 `btnDefault_Click` → `frmProductLoadDefault` → `wsPRD.UpdPRDOP_BatchDefault` → `clsProduct.UpdPRDOP_BatchDefault`。
- `Product Process OP` 對應 `DefaultOP=2` 且預設選取；`All PS OP` 對應 `DefaultOP=1`。父畫面沒有產品流程時不開表單。
- 複製旗標：Parameter、BusinessRule、Description、OPResource、QCItem、SubOP、APS。產品 IssueState 非 0 時 Checkbox/Apply disabled，Radio 仍可選。APS 受 `ExecuteAPS` 顯示控制但 Designer 預設 checked；QCItem 初始狀態由 `PRDDefault_QCItemAutoChecked` 控制。
- Oracle APS product table 字典是由 Windows PowerShell 5.1 + ODP.NET 直連唯讀取得。`DBMS_METADATA.GET_DDL` 回 ORA-03135；ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_CONS_COLUMNS/ALL_INDEXES 可查。欄位/PK 以 `database/reference/mestest_prd_default_aps_source_ddl.sql` 為準。

## Oracle 連線與安全

- 查 DDL 固定使用 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，由本機受保護 profile 取得連線字串後直接讀 Oracle metadata；不用 DBeaver GUI 抄 DDL。
- 公司筆電本次實際讀取的受保護設定檔是 `C:\Users\11019\.codex\attachments\14ce649b-edc6-4e1a-bfab-e971ae8a0cca\pasted-text.txt`（JSON），Oracle 測試連線欄位為 `DatabaseConfigs.CS.Test.Mes.ConnectionString`，類型欄位為 `DatabaseConfigs.CS.Test.Mes.DbType`。這是筆電上的 Codex attachment 路徑，不保證桌電存在；桌電先檢查此檔是否可用，不能因路徑不同就從文件猜密碼。
- 該 JSON 還包含其他系統設定與機密。不可將檔案複製進 repo、提交 Git 或輸出內容；只在程式中讀取所需 Oracle key。若桌電不存在此受保護檔，請使用者安全地重新設定 Oracle profile。
- 公司桌機已使用目前 Windows 帳號的 DPAPI profile 驗證 MESTEST 登入及 RunTime 資料字典查詢。設定檔位於 Git repo 外，由桌機帳號本機解密。
- IP、帳號、密碼、SID 不得寫入 GitHub Markdown、SQL、source code 或 console output。桌電若沒有受保護 profile，透過安全方式請使用者重新設定；不可猜連線資料或從舊電腦文件搜尋明碼。
- 只讀 metadata，不查 MES 業務資料列、不執行 Oracle DDL/DML。PostgreSQL CREATE-only reference 不是 migration；家用端先查 YMES live schema 和 migration history。

## 使用者目標與工作規則

- 忠實復刻完整 MES 的三層 menu、WinForms 功能頁、欄位、按鈕及操作邏輯。困難處可先簡化，但要明確記錄限制，供使用者決策。
- 公司端：DEV 假資料下完成 UI/互動、模組規格、Oracle source DDL 與家用端 API/schema/Create SQL handoff。家用端：核對 PostgreSQL live schema、建立缺少的 migration/API。使用者已授權此 YMES 專案完成後分階段 commit/push GitHub。
- WinForms Form、Designer、Service/Core 是主要證據；圖片只用於版面核對。找不到原始碼/DDL時，告知具體缺少的路徑或表名，不要假裝已確認。
- 不寫入或輸出資料庫秘密。未收到使用者要求時，不執行 build/test/browser；可做靜態檢閱和 `git diff --check`。
- PD101 至 PD117、PD202、PD204、PD206、PD901、`PRD OVERSTAY` 與 `PRD OP RULE` 已完成本輪 DEV 互動與交接。各模組文件記錄正式簽核、匯入、資料權限和引擎相依。`PD_BATCHMODIFYAREA` 的客製原始碼待補。
- CP 行業設定 PDP01 至 PDP04、挑片規則 PDB01、封測產品 PDB02、產品文件 PDB04 與 PDB05 已完成本輪 DEV 與交接。封測行業設定 PDI01／PDI03 的獨立 Form 與 Designer 待補。

## 2026-11-20 WIP WP308 接續檢查點

- WP308「載具管理_元件」已依 WinForms frmCarrierManagement／frmCarrierIn／frmCarrierChange、Service 與 Core 完成 Vue DEV 主頁與兩個子視窗；與 WP302 共用 DEV 假資料。規格為 docs/modules/WIP/載具管理_元件.md；WP303 設備耗品現況已接續完成，詳見下方檢查點。
- WP308 沒有新表；沿用 WP302、WP101、Carrier Basis/Type 的 Oracle reference。家用端需比對 PostgreSQL live schema，再實作 Carrier In/Out/Change 的登入保護 POST command、lookup 與原子交易。
- 這輪未連 YMES PostgreSQL、未部署 API、未執行 build/test/browser；詳見 todolist_officepc.md 與 todolist_homepc.md。

## 2026-11-20 WIP WP303 接續檢查點

- WP303「設備耗品現況」已依 `frmEQPConsumeStatus`、`frmEQPConsumeStatusDef`、`WP_EQPConsumeReset/frmEQPConsumeReset`、wsWIP/wsEQP 與 clsWIP/clsEQPBasis 完成 DEV 主頁及維護、設備 lookup、ConsumReset 視窗。規格：`docs/modules/WIP/設備耗品現況.md`；頁面 `/YMES/WIP/EquipmentConsumeStatus`。
- 兩張新表 `TBLWIPEQPCONSUMESTATUS`、`TBLWIPEQPCONSUMELOG` 的 Oracle source DDL／metadata／PostgreSQL CREATE-only 已建立於 `database/reference/mestest_wip_wp303_*`。設備耗品基準沿用既有設備類別 reference，設備主檔沿用既有 reference。
- WP304「設備耗品歷程」已接續完成，詳見下方檢查點。公司端仍只做 Vue DEV 假資料、WinForms／Service／Core 核對及 DDL metadata handoff；不做 YMES API／migration、不連 PostgreSQL、不執行 build/test/browser。

## 2026-11-20 WIP WP304 接續檢查點

- WP304 依 `frmEQPConsumeStatusLog`、`wsWIP.LoadEQPConsumeLog`、`wsEQP` lookup 與 `clsWIP.LoadEQPConsumeLog` 完成唯讀查詢頁；設備／耗品核取篩選、兩個 lookup、From/End Date、Query、七欄 Grid 與 Close 均已實作。頁面 `/YMES/WIP/EquipmentConsumeStatusLog`。
- 本功能只讀 WP303 已核對的 `TBLWIPEQPCONSUMELOG`，沒有新 Oracle DDL／CREATE-only；DEV 與 WP303 共用假 log storage。
- WP305「設備線上點檢」已接續完成，接手摘要見下節。公司端不連 YMES PostgreSQL、不實作 API/migration、不部署、不執行 build/test/browser。

## 2026-11-20 WIP WP305 接續檢查點

- WP305 依 `frmEQPCheckQCList`、`wsEQP/wsWIP`、`clsWIP`、`clsLot` 完成 Vue DEV `/YMES/WIP/EquipmentOnlineQC`。規格與 DEV 差異見 `docs/modules/WIP/設備線上點檢.md`。
- 直接唯讀 Oracle MESTEST 字典取得 6 表 DDL、indexes、欄位/constraint/trigger metadata：點檢 Log/Detail/Task、設備狀態 Log、設備上機歷程與 Lot 人員登入狀態。Source DDL/Index DDL/metadata/CREATE-only 在 `database/reference/mestest_wip_wp305_*`。`TBLWIPCONT_EQUIPMENT` 原始無 PK；不可自行補 key。
- Vue DEV 使用 localStorage `ymes.dev.wp305-equipment-online-qc.v1`，測項涵蓋 QCType 0–3、多筆 D01-D10、Wait QC、Re QC、未完成確認及結果存檔。NG + UnValidAction 的設備狀態/生產下機副作用尚未在 DEV 寫假 DB 狀態，已明列於規格。
- 正式 API 需保留 Header/Detail/Task 原子交易、server-side QC 結果計算、check-in 不允許部分存檔，以及失敗設備狀態變更時共用 WP201/WP204 下機 command；家用端先比對 live schema/migrations，只補缺少物件。
- 下一功能為 WP306「設備點檢歷程」。繼續讀該 Form/Designer/Service/Core，沿用 WP305 Log/Detail references；若查到未收錄表才以公司 PowerShell 5.1 + Oracle profile 唯讀查 DDL。公司端不連 YMES PostgreSQL、不改 API/migration、不部署/執行 build/test/browser。

- **2026-11-20 WP510 接續**：已依 `STD/MESWin/WIP/WP_WaitBasis` 與 `WP_WaitInformation`、wsWIP/clsLot 完成 `/YMES/WIP/WaitBasis` Vue DEV 六組勾選查詢、15 欄 Grid、七欄唯讀 Wait Info，並合併 WP502 已建立的本機 Wait。Oracle MESTEST 只讀查得 `TBLWIPWAITLOTDISPOSITION` 13 欄、無 PK/trigger、WAITNO 非唯一 index 的 DDL/metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp510_disposition_*`；主表沿 WP201、NodeBasis 沿 PRS104。家用端接授權分頁 API；公司端下一項 WP509 或 WP511。未連 PG、未執行 build/test/browser。
- **2026-11-20 WP511 接續**：已核 THEIL BU1 等待處置主 Form、七個 Split/Merge 子窗入口、`wsWIP.LoadWaitBasisJoinLotState`、`THEIL.wsWIP_Wait.Exe_Wait_LotRelease`、`tcWIP.clsLot_Wait` 及群組權限 `LoadDispPriv`。Vue DEV `/YMES/WIP/WaitDisposition` 有六欄查詢、17 欄 Grid、10 個可見選項、批次/Partial/路由守門與簡化分併批對話框；WP510 會顯示其假解除結果。Oracle MESTEST 唯讀查得新表 `XX_TBLWAITPRIV` 14 欄／GROUPNO PK source DDL、metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp511_priv_*`；其餘 Wait/USR/PRS 表沿現有 references。規格 `docs/modules/WIP/等待處置.md` 明列 DEV 簡化及真交易差異；家用端核 PG live schema、權限、分併批七種交易並做 API。公司端下一項 WP509；未連 PG、未執行 build/test/browser。
- **2026-11-20 WP509 接續**：已核 THEIL BU1 未解除異常單主/處置 Form、七個分併批子窗入口及 `wsWIP`/THEIL `wsWIP_ERF`/Core。Vue DEV `/YMES/WIP/ErfUnreleased` 提供六條件、23 欄、ERF Disp/Abort/Create/Info、狀態 0–3/11 分頁、14 種 Lot 處置及簡化 OCAP/分併批；詳 `docs/modules/WIP/未解除異常單.md`。公司端以 PowerShell 5.1 + 受保護 MESTEST profile 唯讀查得新權限表 `XX_TBLQCHOLDPRIV` 18 欄／GROUPNO PK 原 DDL/index/metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp509_priv_*`；無業務資料讀寫，未連 PG、未執行 build/test/browser。家用端做正式 API、群組/OCAP/ERF/Lot 原子交易及七子窗；公司端接續下一未完成 WIP 功能。
- **2026-11-20 WP106 接續**：已核 `frmOperatorOnline`/Designer、`wsWIP_OperatorState`/`clsOperatorState`；Vue DEV `/YMES/WIP/OperatorOnline` 共用 WP104/105 假 State/Log，支援 OP/Area/Equipment 多選並集、七欄 Grid、逐列 SignOut/確認及外部唯讀入口。規格 `docs/modules/WIP/使用者登入資訊.md`、家用端 API 待辦已補；Oracle State/Log DDL/PG CREATE-only 沿 WP104，LotLoginState 沿 WP305，無新增表。公司端未連 YMES PostgreSQL、未 build/test/browser；下一項 WP109 SWR BASIS。
- **2026-11-20 WP109 接續**：已核 `frmWIPSWRBasis`／Def、`wsWIP`／`clsWIP`，Vue DEV `/YMES/WIP/SwrBasis` 覆蓋七勾選查詢、四狀態、14 欄 JOIN Grid、右側操作鍵、ADD/EDIT/Copy/Approve/Void/附件檔名示意。公司 PowerShell 5.1 + 受保護 MESTEST profile 唯讀取得 `TBLWIPSWRBASIS` 15 欄、`TBLWIPSWRDETAIL` 2 欄、共用 `TBLSYSSERIALNO` 2 欄 source DDL/index/metadata；PG CREATE-only 在 `database/reference/mestest_wip_wp109_create.sql`。無 FK/trigger；正式 API 要修正原 PENDING 作廢假成功、JOIN 重複核准、附件末 byte、SQL 拼接。家用端核 live PG、實作交易/附件/稽核並驗收；公司端未查業務列、未寫 Oracle、未連 PG、未 build/test/browser。詳 `docs/modules/WIP/特殊生產需求維護.md`。下一項依 WIP 清冊找未完成功能。
- **2026-11-20 WP112 接續**：已核 THEIL 撤派工 Form/Service/Core/SAP 拋轉來源；Vue DEV `/YMES/WIP/UndoDispatch` 兩模式、MO 查找、13 欄勾選 Grid、全選與逐批假 Confirm。公司端用受保護 MESTEST profile + PowerShell 5.1 **唯讀**取得六表 `TBLWIPCONT_MATERIALLOT`、`TBLSYSSERIALCREATEMAX`、`XX_TBLOEMO_OS_BU1`、`ZPPT0025A/B/C1` 原始 DDL/index/metadata 與 PG CREATE-only `database/reference/mestest_wip_wp112_create.sql`；外部 `THSAP.*@SAP_DBLINK` 真契約未核。家用端必須接 SAP/Log/Inventory provider，未具備時正式 POST 不可假成功；規格在 `docs/modules/WIP/工單撤派工.md`。公司端未查業務列、未寫 Oracle、未連 PG、未 build/test/browser；後續依 WIP 清冊選下一項。

## IV501 checkpoint

- 規格：[IV501 成品庫庫房資料](modules/INV/成品庫庫房資料_IV501.md)。DEV route `/YMES/INV/FgdInventory`；庫房查詢顯示 `LocQty>0`，選列先查 Component，無 Component 才依 `TBLSYSBIN.BinOrder` 顯示 Lot Bin；FGD In 使用 Source=30。
- 已直接核對 `frmINVFGDInventory`／`frmINVFGDInConfirm`、相關 Service 與 `clsInventory.ExeFGDIn`。重要風險：`AddINVTransactionState` 會吞內層 `ExeINVTransactionState` 例外，外層仍可能回 success，雖然 Basis 已標 State=3；正式 API 不可沿用。
- DDL／metadata 沿用 WP201／WP605、QC409 WIP Bin、PRD Bin refs；沒有新表或 CREATE SQL。桌機未重連 Oracle、未連 PostgreSQL、未 build/test/browser。
- IV502、IV503、IV212 均已完成；下一項 IV204「開立庫存調整單」。

## IV310 checkpoint

- 規格：[IV310 包裝拆並箱作業](modules/INV/包裝拆並箱作業.md)。DEV route 為 /YMES/INV/PackingCombineDismantle，拆箱 mode 0／1／2 與並箱掃描／容量檢核／假歷程已交付。
- WinForms Form／Designer、wsWIP／wsINV Service、clsLot／clsInventory Core 均有直接核對；Combine 寫入前 Core stale-state 驗證已註解，Service 先拆目標再併入為兩段 Core 寫入，Lot transfer 是非同步呼叫且吞錯。正式 API 需要重新核對交易與 CloseFlag，不可依賴 browser payload。
- 包裝 DDL／metadata 與 PG CREATE-only 沿用 WP601 的 19 張共用表 references，沒有 IV310 專屬新表；家用端核對 live schema 後實作原子拆並箱 API、冪等 operation ID 與授權 label job。公司端未連 PG、未 build/test/browser。

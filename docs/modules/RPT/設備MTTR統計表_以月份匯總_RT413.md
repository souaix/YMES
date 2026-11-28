# RT413 設備 MTTR 統計表_以月份匯總

## 功能與入口

- 選單清冊：`EQP MTTR SUMMARY BYMONTH`／「設備MTTR統計表_以月份匯總」，RPT／在製品，Function No `RT413`。
- Vue DEV 入口：`/YMES/RPT/EqpMttrSummaryByMonth`。頁面以有限合成設備與月統計資料模擬主報表、Chamber 報表與趨勢圖，不呼叫正式 API、Oracle 或 PostgreSQL。
- 月報不是讀取月結快照表。Core 對選定月份逐月執行 RT412 的設備狀態統計，再組成寬表。

## WinForms、Service、Core

- 主 Form：`D:\iMES\CSiMES\STD\MESWin\RPT\RT_EQP_MTTRSummary_byMonth\frmEQP_MTTRSummary_byMonth.vb`，Designer 為同資料夾 `frmEQP_MTTRSummary_byMonth.designer.vb`。開始月／結束月格式為 `yyyy/MM`，載入時均設為本月。條件為可勾選的 Area No、Equipment Type、Equipment No；選取窗可多選，Equipment No 也可輸入逗號分隔清單。只有勾選且有值的條件會送 Service。
- Lookup：`wsSMD.LoadAreaBasis(AreaClass=0)`、`wsEQP.LoadEquipmentType(IssueState=2)`、`wsEQP.LoadEquipmentBasis(IssueState=2)`。主報表 Equipment No 選取窗只顯示 `ChamberFlag=0` 的設備。Area／Type／Equipment 選取後由 `frmCOMSearch_Multi` 回填。
- 主 Form 以 `wsSYS.LoadParameter(ParameterNo=ChamberManage)` 讀功能設定；`ParameterValue=1` 時顯示 Chamber 按鈕並可打開另一個子設備 Form，否則按鈕隱藏。子 Form 為 `frmEQP_MTTRSummary_Chamber_byMonth.vb`／Designer，有 Area、Type、Main Equipment No、Chamber Equipment No 四種可勾選條件；父設備選取清單限 `ChamberFlag=0`，Chamber 設備清單限 `ChamberFlag=1`。
- 兩個 Form 都檢查開始月、結束月不可晚於本月且開始月不得晚於結束月。主查詢將 StartTime 設為開始月第一日，EndTime 設為結束月之後一個月第一日，傳 Service 的 EndTime 為 exclusive。Form 未設最大月份跨度；家用端正式 API 應加查詢月份及回傳量限制。本月現況狀態由 Core 以資料庫目前時間結束，因此只統計到現在。
- Service：`D:\iMES\CSiMES\STD\MESws_STD\wsSTD\wsRPT\wsRPT.cs` 的 `LoadEQPMTTRSummary_ByMonth`。解析 StartTime、EndTime、AreaNo、EquipmentType、EquipmentNo、MainEquipmentNo、AdditionalXml，並傳入 `kcRPT.clsReport.LoadEQPMTTRSummary_byMonth`。子 Form 也呼叫同一 Service；父設備條件會傳 `mainequipmentno`，Chamber 條件傳 `equipmentno`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcRPT\clsReport.cs` 的 `LoadEQPMTTRSummary_byMonth`。先從 `tblEQPStateControl` 取得 `ControlNo='BreakDown'` 的故障狀態，然後按月呼叫 `ShowEQPMTTRSummary`，傳入每月月初與下月月初。單月統計的狀態歷程、設備有效狀態、區域關聯與 MTTR／MTBF／Availability 公式沿用 [RT412 規格](設備MTTR統計表_RT412.md)。
- 回傳資料表 `EQPMTTRSummary_byMonth` 固定欄位為 `EquipmentNo`、`AreaNo`、`EquipmentType`、`ChamberFlag`、`MainEquipmentNo`、`Type`，其後每個月份增加一個 `yyyy/MM` 欄。每台設備按次序產生 `MTTR`、`MTBF`、`Availability` 三列，月數值寫在對應欄位；當月沒有資料時欄位留空。核心 month 日期欄是動態寬表欄位。
- 主 Form 綁定 `ChamberFlag=0` 的列；主表顯示 Equipment No、Area、Equipment Type、Type 及月份欄，`ChamberFlag`／`MainEquipmentNo` 為隱藏欄。Chamber Form 綁定 `ChamberFlag=1` 的列，顯示 Chamber Equipment No、Area、Equipment Type、Main Equipment No、Type 及月份欄。兩個 Form 都在非空結果後啟用 Trend Chart；條件或月份變更時清除舊結果並停用按鈕。
- `frmEQP_MTTRSummary_TrendChart.vb` 依每台設備三列資料建立一組面板，面板並排顯示 MTTR（紅線、hr）、MTBF（綠線、hr）、Availability（藍線、%）三張折線圖。Availability 縱軸固定 0–110，空值不繪製。視窗尺寸變更會重排各面板；圖表內容選單可複製目前圖表至作業系統剪貼簿。

## Vue DEV 預覽

- 新增 `web/src/components/EqpMttrSummaryByMonth.vue`、`EqpMttrSummaryByMonthDevPreview.vue`，加入 RPT／在製品選單與 `/YMES/RPT/EqpMttrSummaryByMonth` 路由。正式環境仍顯示尚未開放提示。
- 主／Chamber 月報在同頁切換，以 DEV 假設定模擬 `ChamberManage=1`；兩種模式各自保存月份與條件。提供開始／結束月、Area／Type／Equipment 多選、主設備條件、查詢、表格、公式說明及返回主設備。
- 查詢前不自動載入資料。結果用每台設備三列呈現，Availability 格式化為百分比。趨勢圖按設備顯示三張 SVG 折線圖，月份空值保留斷線；複製圖表按鈕只顯示 toast，不碰 OS clipboard。
- 為避免 DEV 動態寬表過大，預覽限制單次最多 120 個月；來源 WinForms/Core 本身沒有此限制，正式 API 需訂定合理窗口。DEV 的設備名稱、指標及數值都是合成資料。

## MESTEST 參照與家用端 API handoff

- 逐月統計用表與 RT412 相同，既有 Oracle MESTEST 參照為：`database/reference/mestest_wip_wp305_source_ddl.sql`（`TBLEMSEQUIPMENTSTATELOG`）；`database/reference/mestest_equipment_state_references_source_ddl.sql`（`TBLEQPSTATECONTROL`、`TBLEMSEQUIPMENTSTATE`）；`database/reference/mestest_equipment_state_create.sql`（`TBLEQPSTATEBASIS`）；`database/reference/mestest_equipment_basis_create.sql`（`TBLEQPEQUIPMENTBASIS`）；`database/reference/mestest_equipment_group_smd_source_ddl.sql`（`TBLSMDAREARELATION`）。桌機不需新建表或 migration；本次未新查 Oracle，未猜 DDL。
- 家用端比對以上 Oracle 參照與 YMES live schema／索引。建議唯讀 API：`GET /api/reports/equipment-mttr-summary/monthly?startMonth=YYYY-MM&endMonth=YYYY-MM&areaNos=...&equipmentTypes=...&equipmentNos=...&mainEquipmentNos=...&scope=main|chamber`。使用授權範圍、固定狀態碼來源及參數化清單，限制跨度與設備筆數；不要把前端 AdditionalXml 傳入 SQL。回傳 normalized `equipment[]`，每設備含 Area、Type、Main／Chamber 編號及 `months[]`（`yearMonth`、`mttrHours`、`mtbfHours`、`availabilityRatio`），前端再組成表格與趨勢圖。
- 延用 RT412 註記：Core 以未限定 ControlType 的 `ControlNo='BreakDown'` 找故障狀態；家用端應核實 ControlType／唯一性。RT413 為唯讀報表，本頁沒有月結儲存功能。
- 公司端未連 YMES PostgreSQL，未建 CREATE／migration，未做正式 API/schema，未執行 build、test 或 browser。

## 家用端月報 API

- API：登入保護唯讀 `GET /api/reports/equipment-mttr-summary/monthly`；必填 `startMonth`／`endMonth`（嚴格 `yyyy-MM`），`scope=main|chamber`；可選逗號清單 `areaNos`、`equipmentTypes`、`equipmentNos`，Chamber 範圍另可用 `mainEquipmentNos`。查詢只接受已核准設備，所有值以參數化等值條件處理，不接收 `AdditionalXml`／SQL。
- 限制：EndMonth 不得晚於台北當月，範圍最多 60 個月；每種清單最多 100 個值、單值最多 50 字元、符合設備最多 2,000 台。Main 範圍排除 Chamber；Chamber 範圍只取 Chamber，並可按父設備篩選。跨範圍使用者權限依 RT412 現行登入保護範圍。
- 回傳正規化 `items[]`，每設備含 `equipmentNo`、`equipmentType`、`areaNo`、`mainEquipmentNo`、`isChamber` 與 `months[]`；每月含 `yearMonth`、`breakdownTimes`、`mttrHours`、`mtbfHours`、`availabilityRatio`。該設備完全沒有來源狀態的月份回傳空值指標；整段區間都沒有可計算狀態的設備不列入結果。Availability 為 0–1 比例，UI 顯示百分比。
- API 對月初到下月月初採左閉右開區間，現月截止至台北目前時間；在單一 `RepeatableRead`、唯讀交易中按月分組，MTTR／MTBF／Availability 公式與 RT412 一致。故障狀態仍須存在唯一 `EQP Break Down / BreakDown` 設定，否則安全回 503。
- 2026-11-28 以正式復刻 DB 的 `ymes_app` 權限，在 `READ ONLY` 交易中對候選設備與月分組 SQL 執行 `EXPLAIN`，兩段皆成功且命中設備、區域關聯與設備狀態索引；未執行 `ANALYZE`、未讀取業務列、未寫入資料。
- 正式庫的故障狀態設定及有效狀態歷程尚未提供，沒有正式正向結果；本次只新增 API 程式，沒有 migration、正式資料寫入或部署。家用端需待完整 Web build 與正式主機可用磁碟空間後部署；公司端依 [待辦](../../../todolist_officepc.md) 接正式頁，DEV 假資料預覽維持原狀。

## 2026-11-29 正式 API-only 部署與瀏覽器驗證

- 2026-11-28 建置的 API-only 發行版 rt413-api-only 已更新正式 ymes-api 容器；部署 image ID 為 sha256:0e1094c566837147d55335fe0aeffdca2123ef772ece0d757f86b4bf79f92f32。保留部署前映像 ymes-api:rollback-before-rt413-20261128-a1，未動 Web／master Web 容器、共用 Docker cache 或其他專案。
- 2026-11-29 回讀正式容器健康狀態為 healthy，app-browser 開啟 /YMES/API/health/ready 回傳 ready。
- app-browser 對月報端點以有效月份查詢時回 HTTP 503，明確指出尚未設定唯一設備故障狀態 EQP Break Down / BreakDown；以錯誤月份查詢回 HTTP 400（要求 yyyy-MM）。沒有新增或修改正式業務資料。
- 正式 UI 路徑 /YMES/RPT/EqpMttrSummaryByMonth 目前顯示「正式環境尚未開放此功能」。此發行版只交付 API；公司端需在保留 DEV 預覽的前提下接上 Production API，見 todolist_officepc.md。
- 有效查詢尚不能回傳報表，直到正式復刻庫同步／設定可信且唯一的故障狀態資料；不以測試列或猜測狀態碼繞過此條件。

# RT412 設備 MTTR 統計表

## 功能與入口

- 選單清冊：`EQP MTTR SUMMARY`／「設備MTTR統計表」，RPT／在製品，Function No `RT412`。
- Vue DEV 入口：`/YMES/RPT/EqpMttrSummary`。目前頁面只用固定合成設備／故障資料，不呼叫正式 API、Oracle 或 PostgreSQL。
- 報表以整日為查詢單位，依設備狀態期間計算故障次數、MTTR、MTBF 與 Availability。非故障狀態時數沒有再依 `UtilizationType` 篩選。

## WinForms、Service、Core

- Form：`D:\iMES\CSiMES\STD\MESWin\RPT\RT_EQP_MTTRSummary\frmEQP_MTTRSummary.vb`；Designer：同資料夾 `frmEQP_MTTRSummary.Designer.vb`。起訖日期載入時均設為今天。Area No、Equipment Type、Equipment No 是可勾選的多選條件；取消勾選會清空內容。Area／設備類型選項透過 `frmCOMSearch_Multi` 選取，Equipment No 可輸入逗號分隔清單或從選取窗挑選。
- 篩選 lookup：`wsSMD.LoadAreaBasis(AreaClass=0)`、`wsEQP.LoadEquipmentType(IssueState=2)`、`wsEQP.LoadEquipmentBasis(IssueState=2, ChamberFlag=0)`。EquipmentNo 選取清單因此是有效主設備；查詢本身仍只在勾選該篩選時套用。
- Query 檢查 Start／End 不可晚於今天、StartDate 不得晚於 EndDate；送至 Service 的時間為 StartDate 00:00 至 EndDate + 1 日 00:00，Core 以這個左閉右開區間切分狀態期間。今天的現況列以資料庫目前時間作為狀態結束時間。
- `wsSYS.LoadParameter(ParameterNo=ChamberManage)` 決定 `chkShow_Chamber` 是否顯示；只有 `ParameterValue=1` 才提供該選項。Form 未勾選時傳 `ChamberFlag=0`，勾選時不傳 ChamberFlag，結果可含主／子設備。回傳的 Chamber 列由 Form 將子設備 EquipmentNo 移到 ChamberNo，將 BelongEquipmentNo 顯示在主 Equipment No 欄，並另外顯示 Chamber Type。
- Service：`D:\iMES\CSiMES\STD\MESws_STD\wsSTD\wsRPT\wsRPT.cs` 的 `LoadEQPMTTRSummary`，解析 AreaNo、EquipmentType、EquipmentNo、StartTime、EndTime、AdditionalXml、ChamberFlag，並呼叫 `kcRPT.clsReport.LoadEQPMTTRSummary`。缺少日期時 Service 回錯誤。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcRPT\clsReport.cs` 的 `LoadEQPMTTRSummary` 先讀 `tblEQPStateControl` 中 `ControlNo='BreakDown'` 的 EquipmentState 作為故障狀態，然後呼叫 `ShowEQPMTTRSummary`。若找不到故障狀態設定，報表失敗。
- `ShowEQPMTTRSummary` 從 `tblEMSEquipmentStateLog UNION ALL tblEMSEquipmentState` 取歷程／現況，依 StartTime／EndTime 與查詢區間重疊條件截短區間，join `tblEQPStateBasis` 取得 StateName；設備需為 `tblEQPequipmentbasis.IssueState=2`。Area 以 `tblSMDAreaRelation.ObjectType=2` 關聯設備；Chamber 的 Area 由其父設備的區域關聯帶入。結果依設備及狀態分組。
- Core 對各設備加總故障狀態的時數與歷程筆數，其他所有狀態時數則加到 MTBF 分子。公式：`MTTR = 故障時數 ÷ 故障次數`；有故障時 `MTBF = 非故障時數 ÷ 故障次數`、`Availability = MTBF ÷ (MTBF + MTTR)`。無故障時 Breakdown Times 為 0、MTTR 為 0、MTBF 為非故障時數、Availability 為非故障時數／總狀態時數。表格顯示 Area、主設備類型／編號、Breakdown Times、MTTR、MTBF、Availability；顯示 Chamber 時再增加 Chamber Type／No。
- `btnSave` 的來源 Designer 設為 `Visible=False`，但 Form 有 `UpdMTTRMonthSummary` 儲存 handler：要求非空查詢結果、StartDate 為月初、EndDate 為月底、同年月且不晚於今天，再把逐設備結果送 Service。此動作目前不可見，本 DEV 頁不提供寫入按鈕，也不模擬正式儲存。

## 已知來源行為與注意點

- Core SQL 直接串接日期、清單條件與 AdditionalXml；家用端不可接受或轉送前端 SQL 片段，應使用固定欄位與參數化清單，加入授權、日期跨度及回傳筆數上限。
- Service 沒有解析／傳送 `MainEquipmentNo`。雖然底層 `ShowEQPMTTRSummary` 有同名參數，但現行 Service 呼叫只傳 EquipmentNo，因此主設備編號條件不會限制其 Chamber 子列。若正式 API 要提供父設備查詢，需由家用端按授權範圍明確設計，不要誤以為舊 Form 已提供。
- `ChamberFlag` 參數只用來決定是否加上 `T2.ChamberFlag=0`，沒有比較傳入值；Form 目前傳 0 或省略，符合主設備／全部的二態用途。
- Core 只以 `ControlNo='BreakDown'` 查控制設定，而 Oracle DDL 的主鍵包含 ControlType、ControlNo。若多個 ControlType 有相同 ControlNo，取用狀態可能不唯一；家用端應確認有效 ControlType 與資料唯一性。
- Availability 的來源格式為百分比，底層欄位值是 0–1 比率；API 回傳建議保持 decimal ratio，前端再格式化百分比。

## Vue DEV 預覽

- 新增 `web/src/components/EqpMttrSummary.vue`、`EqpMttrSummaryDevPreview.vue`，並加入 RPT／在製品選單、路由 `/YMES/RPT/EqpMttrSummary`。正式環境元件仍顯示尚未開放提示。
- 預覽提供日期範圍、Area／Equipment Type／Equipment No 多選與文字輸入、Chamber 顯示、Query、MTTR 公式說明、合成資料結果表、空結果／日期錯誤提示。結果初始為空；查詢後依固定故障事件比例計算統計值。沒有資料寫入或呼叫 API。
- 固定的設備編號、故障次數與故障區間全為示範假資料；ChamberManage 也用啟用狀態假設定，不能作為現場設備／狀態資料。
- 原始資料變更時，表單會清除舊結果；DEV 預覽也清除結果以避免條件與舊統計混淆。

## MESTEST 參照與家用端 handoff

- 查詢表的既有參照：`database/reference/mestest_wip_wp305_source_ddl.sql`（`TBLEMSEQUIPMENTSTATELOG`）；`database/reference/mestest_equipment_state_references_source_ddl.sql`（`TBLEQPSTATECONTROL`、`TBLEMSEQUIPMENTSTATE`）；`database/reference/mestest_equipment_state_create.sql`（`TBLEQPSTATEBASIS`）；`database/reference/mestest_equipment_basis_create.sql`（`TBLEQPEQUIPMENTBASIS`）；`database/reference/mestest_equipment_group_smd_source_ddl.sql`（`TBLSMDAREARELATION`）。這些參照足以支援本次查詢 handoff，本次沒有重查 Oracle，也未新增或猜測 DDL。
- 桌機未連 YMES PostgreSQL，未建立 CREATE／migration，未實作或驗收正式 API；未執行 build、test 或 browser。
- 建議唯讀 endpoint：`GET /api/reports/equipment-mttr-summary?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD&areaNos=...&equipmentTypes=...&equipmentNos=...&includeChambers=false`。服務端依使用者／設備授權套用區域範圍，以 `TBLEQPSTATECONTROL` 的明確 ControlType／ControlNo 取得故障狀態，參數化查詢歷程與現況、狀態主檔、設備及區域關聯；固定整日區間、限制日期跨度與最大設備筆數，並以 EquipmentNo 穩定排序。
- 建議回傳每台設備的 `areaNo`、`equipmentType`、`equipmentNo`、`belongEquipmentNo`、`chamberFlag`、`breakdownTimes`、`mttrHours`、`mtbfHours`、`availabilityRatio`。確認 live schema 欄位型別／索引、有效 Breakdown ControlType、Chamber 需求與父設備篩選語意後，家用端再決定正式 API 及可能的月結保存設計。

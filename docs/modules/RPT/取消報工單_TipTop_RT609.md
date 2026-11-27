# RT609 取消報工單_TipTop

## 功能與入口

- RPT Menu FunctionNo ERPWORKREPORT CANCEL、FunctionKey RT609、Form frmERPWorkReport_Cancel。Vue DEV 入口：/YMES/RPT/ErpWorkReportCancel。
- 查詢條件為 MO No 與報工 DataStartTime 起訖，至少需勾選一項；選列後逐層查看 Summary、Equipment Summary、Detail。
- DEV 使用固定虛構資料，在本機記憶體模擬查詢、TipTop ERP 取消失敗／成功、MES Flag 更新與重拋排程狀態。DEV 不呼叫 ERP、MES API、Oracle 或 PostgreSQL。T100 與 E10ICD 是另兩個 FunctionKey RT610／RT611，不屬於本規格。

## WinForms、Service、Core

- Form／Designer 同檔：D:\iMES\CSiMES\STD\MESWin\RPT\RT_ERPWorkReport_Cancel\frmERPWorkReport_Cancel.vb。Form 載入時設定 DataStartTime 起訖預設值為昨日及今日，日期區間預設未勾選。MONo 有文字時自動勾選，離開欄位轉大寫；Enter 是 Tab。日期條件勾選時起訖都必填，結束必須晚於開始（精確到秒）。
- Form Query 至少要求一項條件。MO 條件與日期條件同時存在時以 AND 查詢；MONo 精確比對。日期條件按 DataStartTime 篩選。
- Summary 呼叫 wsRPT.LoadERPWorkRPT_Summary，Core clsReport.LoadERPWorkRPT_Summary。Service 可解析 MONo、DataStartTime_From／To、ERPFlag、Additional；Form 僅送 MONo 及可選時間範圍。Core 以 report connection 讀 TBLERPWorkRPT_SUMMARY，日期邊界是 DataStartTime >= from 且 <= to，MO 則以 WorkReportID 對 TBLERPWorkRPT_DETAIL.MONo 精確篩選。SQL 以字串串接，沒有排序或分頁；Form 依回傳順序加 ItemNo。Core 本身沒有至少一項條件守門，空條件會查整張 summary。
- Summary grid 僅顯示 ItemNo、DataStartTime、DataEndTime、ERPFlag，其餘欄隱藏。選取 Summary 後呼叫 wsRPT.LoadERPWorkRPT_EQPSummary／clsReport.LoadERPWorkRPT_EQPSummary，按 WorkReportID 取設備／ERP 單號／資源量與 ERPFlag。選取設備後呼叫 wsRPT.LoadERPWorkRPT_Detail／clsReport.LoadERPWorkRPT_Detail，按 WorkReportID、EquipmentNo 取 ItemNo、EquipmentNo、MONo、EQP_RESValue、EMP_RESValue。
- Cancel WorkRPT 僅允許選中 ERPFlag=1。Form 使用 gERPWSURL 直接呼叫 TipTop UndoCreateWOWorkReportData，並依系統參數 TPERPUseMESUserNo 決定 ERP access 身分；auto=N。ERP 單筆失敗時顯示需人工處理及 ERPNo，但仍繼續其他設備，之後才呼叫 wsRPT.ExeUndoERPWorkReport。該 Service 呼叫 clsReport.ExeUndoERPWorkReport，以一批 SQL 清除 TBLWIPCONT_RESOURCE、TBLWIPCONT_RESOURCE_SHARE、TBLWIPCONT_EQUIPMENT、TBLWIPCONT_EMPLOYEE_MULTIUSER 的 ERPFlag／WorkReportID（Equipment／Employee 另清 SummaryFlag），再把 TBLERPWORKRPT_SUMMARY 與 TBLERPWORKRPT_EQPSUMMARY 的 ERPFlag 設為 -1。Form 重新查詢並恢復原選列；外層 Catch 會吞掉錯誤。ERP 已成功而 MES 更新失敗，或 ERP 撤銷失敗但 MES 仍設為 -1，都會造成兩邊狀態不一致。
- ReSum ReTurn 僅允許 ERPFlag=-1；Form 傳 WorkReportID、DataStartTime、DataEndTime 給 wsRPT.ExeReRunERPWorkReport／clsReport.ExeReRunERPWorkReport。Core 查 TBLMESAutoRunJob 中 JobName=funCreateWOWorkReportData_TP；找到 Job 時插入一筆 Standby 到 TBLMESAutoRunQueue，然後把 Summary／Equipment Summary ERPFlag 設為 2。找不到 Job 時不插入佇列，但仍會更新兩張 summary flag。DataEndTime 雖由 Form／Service 傳入，Core 未用於佇列資料；Parameter03 僅填 DataStartTime 日期。Form 再查詢並恢復選列。
- Cancel 的 ERPFlag=1 與 ReSum ReTurn 的 ERPFlag=-1 是來源中明確的操作門檻；畫面狀態字串來自 ValueListProvider，未在 Form 內定義。DEV 只顯示原始數值。

## DDL 與家用端 API／schema handoff

- MESTEST source DDL／metadata 缺少：TBLERPWORKRPT_SUMMARY、TBLERPWORKRPT_EQPSUMMARY、TBLERPWORKRPT_DETAIL、TBLWIPCONT_RESOURCE_SHARE、TBLWIPCONT_EMPLOYEE_MULTIUSER、TBLMESAutoRunJob、TBLMESAutoRunQueue。桌機已確認使用者的受保護 Oracle attachment 不存在，未連 Oracle／查 metadata，也未猜 CREATE-only SQL。
- 可沿用的 repo references：TBLWIPCONT_RESOURCE（database/reference/mestest_wip_wp403_source_ddl.sql）、TBLWIPCONT_EQUIPMENT（mestest_wip_wp305_source_ddl.sql）。只涵蓋 Core 更新的部分表，不足以建立整個 RT609 schema。
- 2026-11-27 家用端以 PPK／SSH 唯讀查正式 `ymes` PostgreSQL catalog：上述七張目標表在所有 schema 均不存在，無欄位／鍵／索引或資料可供比較；未執行 migration，也未讀寫業務資料。連線紀錄記於 [DATABASE_CONNECTION.md](../../DATABASE_CONNECTION.md)。
- 家用端先核 YMES live schema、同步來源、欄位／鍵／索引及正式資料。若有唯讀資料，建議查詢 API 至少要求 MO 或 DataStartTime 範圍，參數化、設定日期跨度／筆數上限與穩定排序，回傳 Summary rows；設備與明細再以 WorkReportID、EquipmentNo 取得並授權檢查父子關係。不可接受前端 SQL／AdditionalXml。
- 取消與重拋不是單純 CRUD。正式整合需設計可追蹤的 ERP/MES 狀態協調、每設備單號冪等撤銷、失敗補償與人工對帳；未有可靠 ERP connector／冪等及 reconciliation 前，不要把舊 WinForms 外部 ERP 呼叫或 Core SQL 直接公開為寫入 API。公司端只交 DEV 邏輯與 handoff，不連 PostgreSQL、不實作正式 API／schema。

## 公司端交付範圍

- 完成 DEV 假資料查詢條件驗證、Summary → Equipment Summary → Detail 選列、ERPFlag 操作門檻、逐設備 ERP 撤銷失敗仍繼續 MES 撤銷的模擬，以及重拋佇列有／無 Job 兩種情境。
- 未執行 build、tests 或 browser 驗收；未呼叫任何 ERP、MES API、Oracle 或 PostgreSQL。

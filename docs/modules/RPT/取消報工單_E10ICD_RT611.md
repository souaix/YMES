# RT611 取消報工單_E10ICD

## 功能與入口

- RPT FunctionNo ERPWORKREPORT CANCEL_E10ICD、FunctionKey RT611、Form frmERPWorkReport_Cancel_E10ICD。Vue DEV 入口：/YMES/RPT/ErpWorkReportCancelE10ICD。
- 查詢條件為 MO No 與報工 DataStartTime 起訖，至少勾選一項；選 Summary 後直接顯示 E10ICD Detail，沒有啟用 Equipment Summary Grid。
- DEV 共用 RT610 的本機 EAI 假資料互動元件，以 E10ICD 假單號及 JobName 顯示；只模擬查詢、取消失敗／成功與重拋，不呼叫 E10ICD、MES Service、Oracle 或 PostgreSQL。

## WinForms、Service、Core

- Form／Designer 同檔：D:/iMES/CSiMES/STD/MESWin/RPT/RT_ERPWorkReport_Cancel_E10ICD/frmERPWorkReport_Cancel_E10ICD.vb。與 RT610 表單互相比對，查詢條件、Summary／Detail Grid、操作門檻及取消／重拋流程相同；主要差異為 E10ICD Function／Service／Core 與資料表名稱。日期預設昨日及今日；MO 輸入會自動勾選並在離開時轉大寫；日期需完整且結束晚於開始。來源錯誤訊息仍沿用「tiptop」字樣。
- Summary 呼叫 wsRPT.LoadERPWorkRPT_Summary_E10ICD／clsReport.LoadERPWorkRPT_Summary_E10ICD，讀 TBLERPWORKRPT_SUMMARY，以 DataStartTime 含界值篩選；MO 由 TBLERPWORKRPT_DETAIL_E10ICD 的 WorkReportID 子查詢等號比對。Core SQL 串接字串、無排序／分頁且本身允許空條件；Form 不送 Service/Core 支援的 ERPFlag。
- Summary 顯示 ItemNo、DataStartTime、DataEndTime、ERPFlag。選列後呼叫 wsRPT.LoadERPWorkRPT_Detail_E10ICDJoinERPDocumentLog／clsReport.LoadERPWorkRPT_Detail_E10ICDJoinERPDocumentLog，以 WorkReportID 並可帶目前 MONo 查 TBLERPWORKRPT_DETAIL_E10ICD。Detail 顯示 ItemNo、EquipmentNo、MONo、EQP_RESValue、EMP_RESValue、ERPNo；沒有 Equipment Summary 中間層。
- Cancel 僅允許 ERPFlag=1。Form 對已載入的 Detail 每一列逐筆呼叫 wsERP_E10ICD.ERPWorkReport_Cancel_E10ICD，不先去重 ERPNo。ERP Service 讀 ERP service 設定的 URL，以 EAI wo.work.report.data.process／report_no 發送取消，回應 code 非 0 視為失敗並記交易 Log。Form 繼續其餘列、彙整失敗 ERPNo，仍會執行 MES Undo。
- Undo Service wsRPT.ExeUndoERPWorkReport_E10ICD 呼叫 clsReport.ExeUndoERPWorkReport_E10ICD。Core 清除 TBLWIPCONT_EQUIPMENT、TBLWIPCONT_EMPLOYEE_MULTIUSER 的 ERPFlag／SummaryFlag／WorkReportID，並將 TBLERPWORKRPT_SUMMARY.ERPFlag 設為 -1；Resource／ResourceShare 更新已註解。Core 從 TBLERPWORKRPT_DETAIL_E10ICD 依 ERPNo 分組，但只讀第一列，再清 TBLWIPLOTLOG_REPORT 中該 ERPNo 且 EndTime 非空的 ERPNo。GROUP BY 沒指定排序；WorkReportID 有多個 ERPNo 時其他回報列可能留存。即使 ERP 取消失敗仍做 MES Undo，會有兩端狀態不一致風險。
- ReSum ReTurn 僅允許 ERPFlag=-1。Core 尋找 JobName=funCreateWOWorkReportData_E10ICD；找到就插入 Standby queue，Parameter03 僅使用 DataStartTime 日期。DataEndTime 未使用。不論 Job 是否存在都將 Summary ERPFlag 設為 2，Detail E10ICD Flag 不更新。狀態字串來自 ValueListProvider，DEV 顯示來源數值。

## DDL 與家用端 API／schema handoff

- 尚缺可信 MESTEST source DDL／metadata：TBLERPWORKRPT_SUMMARY、TBLERPWORKRPT_DETAIL_E10ICD、TBLWIPCONT_EMPLOYEE_MULTIUSER、TBLMESAutoRunJob、TBLMESAutoRunQueue。桌機受保護 Oracle attachment 不存在，未連 Oracle、未猜 CREATE-only SQL。
- 可沿用 repo refs：TBLWIPCONT_EQUIPMENT（WP305）、TBLWIPLOTLOG_REPORT（WP201 dependency source DDL）。不代表上述缺表已同步到 YMES PostgreSQL。
- 2026-11-27 家用端以 PPK／SSH 唯讀查正式 `ymes` PostgreSQL catalog：上述五張目標表在所有 schema 均不存在，無欄位／鍵／索引或資料可供比較；未執行 migration，也未讀寫業務資料。連線紀錄記於 [DATABASE_CONNECTION.md](../../DATABASE_CONNECTION.md)。
- 家用端先核對 live schema、同步來源、ERPNo／WorkReportID 關聯與 E10ICD 正向資料。唯讀 API 需登入及 RT611 ACL、參數化、至少 MO 或 DataStartTime 條件、日期跨度／筆數上限與穩定排序；Detail 依已授權 WorkReportID 驗證父子關係，不接受 SQL／AdditionalXml。
- 寫入需設計逐 ERPNo 冪等、跨 E10ICD／MES 部分成功補償與對帳、多 ERPNo LotLog 清除規則；重拋須確認 E10ICD AutoRun Job，避免 job 不存在仍標示 ERPFlag=2。公司端不實作正式 API／schema。

## 公司端交付範圍

- 完成 Summary／Detail 假資料 UI、條件驗證、選列、ERPFlag 操作門檻及取消／重拋兩種結果的本機模擬；共用 RT610 EAI 預覽元件。
- 未執行 build、tests 或 browser；未呼叫 E10ICD、MES API、Oracle 或 PostgreSQL。

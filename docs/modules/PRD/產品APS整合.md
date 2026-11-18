# PD102 產品 APS Integration

## WinForms、Service 與 Core

- `PD_ProductBasis_THEIL_BU1/frmProductBasis_THEIL_BU1.vb` 的 `btnAPSIntegration_Click` 呼叫 `funSetupAPSIntegration`。入口要求目前產品及流程列，傳 ProductNo、ProductVersion、ProcessNo、ProcessVersion、MOTypeNo 和 PSNo。WinForms 主畫面由 ExecuteAPS 決定按鈕顯示。
- 實際主窗及 EQPTIME 定義窗是同專案的 `frmPRSAPSIntegration_THEIL_BU1.vb` 與 `frmPRSAPSIntegrationDef_THEIL_BU1.vb`，Designer 位於各自的 `InitializeComponent`。主窗從流程展開帶入 OP／Node；EQPTIME 由目前 APS 主設定帶入 NodeID 及設備群組。相同 OP 可以在不同 Node 建立規則。
- 主窗有 EquipmentNo、EquipmentGroup、Subcontractor、Schedule、APS Fix／Var EQP Time、STD Work Time Qty、Transfer Qty、Lot Rear Time、Batch OP／Max Qty、Description。明細需要 EquipmentNo，兩層都要求 STD Work Time Qty 大於 0，其餘工時及數量不可為負。排程及批量選項送 0 或 1。Update 儲存留窗，Delete 後重載。
- Service／Core 由 `wsPRD.Load/Edit/DelPRDPRSOPAPSSetup` 和 `Load/Edit/DelPRDPRSOPAPSSetupDetail` 對應 `clsProduct`。主設定更新時，Core 依群組／區域／系統參數重新展開並重建該節點設備明細。明細可獨立更新或刪除。主設定刪除會一併刪除該鍵下的設備明細。正式設備及委外供應商 lookup 涉及 `wsPRD`／`wsPRS.LoadEQPListOfOPAPSIntegrationBASE`、`wsEQP.LoadEQPGroupBasis`、`wsENT.LoadSubcontractor`。

## Oracle metadata 及既有 SQL

- 使用 Windows PowerShell 5.1 + ODP.NET 唯讀核對 MESTEST 的兩張產品表及兩張流程來源表。產品主／明細為 23／22 欄，流程主／明細為 19／16 欄。四表只查到各自 PK 唯一索引，未見 FK 或 trigger。
- 產品 APS 主表 PK 為 `(PRODUCTNO, PRODUCTVERSION, MOTYPENO, NODEID)`，設備明細加 `EQUIPMENTNO`。流程 APS 主表 PK 為 `NODEID`，明細加 `EQUIPMENTNO`。主明細與 PD102 Default 共用，沿用原 Oracle 與 PostgreSQL CREATE-only 參照。
- [產品 Oracle DDL](../../../database/reference/mestest_prd_default_aps_source_ddl.sql) · [產品 PostgreSQL CREATE-only](../../../database/reference/mestest_prd_default_aps_create.sql) · [流程 Oracle DDL](../../../database/reference/mestest_prs104_process_basis_source_ddl.sql) · [流程 PostgreSQL CREATE-only](../../../database/reference/mestest_prs104_process_basis_create.sql)

## Vue DEV 與家用端 handoff

- `ProductApsIntegration.vue` 提供流程節點選擇、主設定與 EQPTIME、Update／Delete，並沿用 Default 的 DEV `apsSetups`、`apsSetupDetails`。主設定更新用 DEV 節點的固定設備集合重建明細，主設定刪除移除同鍵明細。DEV 以同窗切換 EQPTIME，使用固定供應商與設備樣本，未模擬網狀流程、群組權限、完整參數分流或 APS 上線排程。
- PROD API 仍待接線。家用端先比對 live schema、first-phase、migrations 及 Default handoff。正式服務從所選流程識別 Node、區域、設備群組及使用者權限，以產品版次／MOType／Node 為主鍵範圍實作 GET／PUT／DELETE，明細再限定 EquipmentNo。主列與明細的重建、刪除及並發控制應保持一致。需驗證重複 OP 不同 Node、設備覆蓋、上限、整批回滾、Default 共用表及 ExecuteAPS 顯示。

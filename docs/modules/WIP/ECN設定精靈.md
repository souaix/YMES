# WP906 ECN 設定精靈

## HOME整合：正式防護範圍實測通過（2026-11-20；provider待接）

目前入口 `/YMES/WIP/EcnWizard`，元件 `web/src/components/WipEcnWizard.vue`。HOME已將Office e5f來源整合至既有KD框架，保留原三層選單及本機DEV操作。App只對本輪九個新元件掛 `office-wip-module`，統一外框單一主標題；不改其他模組CSS。

DEV資料、localStorage、假查找、查詢／命令及彈窗都必須同時滿足 `import.meta.env.DEV && developmentPreview === true`。正式包一律空資料、顯示「正式環境尚未開放此功能。」並停用查詢／異動／查找等未交付操作，保留關閉回首頁。不得以假流水號、假SAP／ERP／簽核／Lot變動回成功。fieldset內控制項驗收以有效 `:disabled` 狀態為準。

Root已在正式Web256 `ade411cc…`／API167 `3f0bdc7c…`逐頁實測 `WP906`／`/YMES/WIP/EcnWizard` 的限定防護通過：原生入口、正式空資料與未開放提示、KD單一主標題、無假lookup／DEV列或業務彈窗；本頁direct重進／reload後保持隔離，Close回空首頁。逐頁觀察檔 `.tools/gui-proof-20261120/et101-office-e5f-wip-observations.json` 以本頁code定位：menu#14、direct-reenter#37、reload-confirm#38（#為1起算array序位）；主GUI journal本頁direct／reload／Close事件為112/113/114。

以上只記已實做的正式防護／導航範圍，**不是 `WP906` 正式provider或交易完成**；所需PG migration、權限、原子交易及外部整合仍由HOME按下方原生規格完成。Office持續原生Oracle唯讀來源、UI及規格，不連HOME PostgreSQL或SSH。本輪整體GUI尚未封存，最終數字、Git／main／新checkpoint仍pending，封存後另見[本輪QA](../../qa/20261120-et101-office-e5f.md)。


- FunctionNo 為 `ECN WIZARD`。依 `STD/MESWin/WIP/WP_ECNWizard/frmECNWizard.vb` 及 `uctStep1.vb` 至 `uctStep6.vb` 的 Form／內嵌 Designer，依序歡迎、ECN 資料、特性、條件、選批、完成。
- 步驟二輸入 ECNNo（最長 50）、選擇依作業站或產品取特性，兩種都要 OPNo，產品來源另需產品與版本。生效／失效時間可空，生效不得早於現在，失效須晚於已填的生效時間。
- 步驟三以 AttribSource=3，呼叫 `wsOP.LoadOPAttrib` 或 `wsPRD.LoadPRDOPAttrib_Default` 取 Setup 特性。須勾選至少一項。步驟四至少勾一個客戶、工單或產品／版本因子，每個勾選條件要有值。
- 步驟五透過 `wsWIP.LoadLotBasisJoinState` 查受影響批、預設全選，可逐批取消。確認送 ECNNo、OPNo、有效期、屬性編號／值／類型、因子／值及所選 lotNo。舊 Form 使用 BeginInvokeSrv，正式頁面應等服務明確成功才進完成步驟。
- STD `wsWIP.UpdECN` 按屬性類別走 `kcSYS.clsSYSEng.ChkElementValueValid` 並記 EventLog。 `STD/MESkc/kcWIP/clsECN.cs::UpdECN` 將 ECNNo 大寫，日期結束日午夜值正規化為 23:59:59。交易寫 ECNBasis（IssueState=0）、ECNAttrib 與所選 Lot × Attrib 的 LotECNAttrib（IssueState=0）。

## DEV 與家用端

- `/YMES/WIP/EcnWizard` 提供六步、回上一步、勾選與驗證、受影響批、確認及重做。來源、條件及特性使用 fixtures；未模擬桌面歡迎頁永久偏好、正式 Element 型態轉換、稽核、核准與到期歸檔。DEV 記錄及防重只存在本頁記憶體。
- 建議 WP906 ACL 的 `GET /api/wip/ecn/setup-attributes`、`POST /api/wip/ecn/preview`、`POST /api/wip/ecn`。建立前重查授權、有效期間、因素限制、可修改屬性及預覽批版本。因素只容許 CustomerNo、MoNo、ProductNo 與 ProductVersion 固定欄位，參數化值。用 ECN 主鍵與冪等鍵防重，三表同一交易完成，伺服器回傳筆數與 IssueState。
- 三張直接表沿用 [WP201 CREATE-only](../../../database/reference/mestest_wip_wp201_create.sql)。Setup lookups 沿用 [OP](../../../database/reference/mestest_operation_and_system_create.sql)、[PRD](../../../database/reference/mestest_prd_parameter_setup_create.sql) 及客戶、工單主檔；本項無新直接表。

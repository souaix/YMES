# WP906 ECN 設定精靈

- FunctionNo 為 `ECN WIZARD`。依 `STD/MESWin/WIP/WP_ECNWizard/frmECNWizard.vb` 及 `uctStep1.vb` 至 `uctStep6.vb` 的 Form／內嵌 Designer，依序歡迎、ECN 資料、特性、條件、選批、完成。
- 步驟二輸入 ECNNo（最長 50）、選擇依作業站或產品取特性，兩種都要 OPNo，產品來源另需產品與版本。生效／失效時間可空，生效不得早於現在，失效須晚於已填的生效時間。
- 步驟三以 AttribSource=3，呼叫 `wsOP.LoadOPAttrib` 或 `wsPRD.LoadPRDOPAttrib_Default` 取 Setup 特性。須勾選至少一項。步驟四至少勾一個客戶、工單或產品／版本因子，每個勾選條件要有值。
- 步驟五透過 `wsWIP.LoadLotBasisJoinState` 查受影響批、預設全選，可逐批取消。確認送 ECNNo、OPNo、有效期、屬性編號／值／類型、因子／值及所選 lotNo。舊 Form 使用 BeginInvokeSrv，正式頁面應等服務明確成功才進完成步驟。
- STD `wsWIP.UpdECN` 按屬性類別走 `kcSYS.clsSYSEng.ChkElementValueValid` 並記 EventLog。 `STD/MESkc/kcWIP/clsECN.cs::UpdECN` 將 ECNNo 大寫，日期結束日午夜值正規化為 23:59:59。交易寫 ECNBasis（IssueState=0）、ECNAttrib 與所選 Lot × Attrib 的 LotECNAttrib（IssueState=0）。

## DEV 與家用端

- `/YMES/WIP/EcnWizard` 提供六步、回上一步、勾選與驗證、受影響批、確認及重做。來源、條件及特性使用 fixtures；未模擬桌面歡迎頁永久偏好、正式 Element 型態轉換、稽核、核准與到期歸檔。DEV 記錄及防重只存在本頁記憶體。
- 建議 WP906 ACL 的 `GET /api/wip/ecn/setup-attributes`、`POST /api/wip/ecn/preview`、`POST /api/wip/ecn`。建立前重查授權、有效期間、因素限制、可修改屬性及預覽批版本。因素只容許 CustomerNo、MoNo、ProductNo 與 ProductVersion 固定欄位，參數化值。用 ECN 主鍵與冪等鍵防重，三表同一交易完成，伺服器回傳筆數與 IssueState。
- 三張直接表沿用 [WP201 CREATE-only](../../../database/reference/mestest_wip_wp201_create.sql)。Setup lookups 沿用 [OP](../../../database/reference/mestest_operation_and_system_create.sql)、[PRD](../../../database/reference/mestest_prd_parameter_setup_create.sql) 及客戶、工單主檔；本項無新直接表。

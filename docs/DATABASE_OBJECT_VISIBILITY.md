# 資料表存在性與應用權限

2026-11-19 家用端修正，適用 QC101/QC102/QC103/QC107/QC108/QC301、QC401/QC402/QC501/QC502/QC503、PS101/PS102 的相關 metadata 判斷。

`information_schema.tables` 會依目前角色的表權限過濾。物件存在但 `ymes_app` 尚未取得權限，不能被當成資料表不存在，尤其不得因此略過引用檢查或走「未建立 Issue/Job」的直接核准分支。

- 物件存在性查 `pg_catalog.pg_class` 與 `pg_namespace`，固定大寫 schema `YMES`、參數化物件名稱；欄位存在性以 `pg_attribute` 查核。不得以可見業務列或查詢失敗推導不存在。
- 只有物件確實不存在才採各模組已核實的缺表分支。有簽核表而共用簽核尚未接入時回503；引用表存在但未授權，實際讀取失敗亦回503，不略過引用防護。
- 不因 metadata 可見就代表可讀寫。新物件仍透過已審閱 forward migration 明確授權；API 不自行建立資料表或擴權。
- 外部回應維持既有 ProblemDetails，不能暴露 SQL、連線字串或憑證。此修正不改各模組的狀態、欄位、nullable CAS、來源資料或簽核業務規則。

## 實際唯讀驗證

以 PPK SSH 連家用端 PostgreSQL，交易中 `SET LOCAL ROLE ymes_app`。尚未授權的既有 `YMES.tblprdprogrambasis` 回傳：catalog existence=true、information_schema visibility=false、SELECT privilege=false。全程 `BEGIN READ ONLY`／`ROLLBACK`，未讀業務列、未變更權限、未新增測試簽核表。此結果驗證兩種查詢的差異；不是完整 Issue/Job 工作流程測試。

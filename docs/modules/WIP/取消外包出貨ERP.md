# WP404 取消外包出貨 ERP

- 選單 FunctionNo 為 `OS CONFIRM_ERP_CANCEL`，路由為 `/YMES/WIP/OutsourceErpCancel`。核對 `STD/MESWin/WIP/WP_OSConfirm_ERP_Cancel/frmWIPOSConfirm_ERP_Cancel.vb` 及內嵌 Designer、`wsWIP.ChkReverseForOSCancel`／`Exe_ERP_OutSource_Cancel_Reverse`、`clsLot.funCheckReverseForOSCancel`／`funExcuteReverseForOSCancel`。
- 主查詢固定 Status=1，查詢條件與 [WP403](外包回貨.md) 相同。選一張外包單及明細，確認窗顯示 ERPNo_Out。舊 Form 逐批檢查最新 ReverseHistory 存在、外包站相符、CommitState=1，並重查單據仍是已出貨且 ERPNo_Out 有值。
- 先依 active ERP 呼叫 E10ICD 或 TIPTOPICD 取消。MES 回復前先把合格歷程的 CommitState 設成 0，失敗時補回 1。固定外包 OSType=1 的主檔改 -1，原 Form 傳全部歷程。臨時外包 OSType=2 的主檔回 0 並清 ERPNo_Out，原 Form 傳第一個明細批的歷程，後續回復範圍須依 `ExecuteReverse` 核實。

## 家用端交接

- OS Basis／Detail、ERP settings 及 ReverseHistory 使用既有 [WP401](外包單開立.md)、[WP402](外包單確認.md) 與 [WP215 CREATE-only](../../../database/reference/mestest_wip_wp215_create.sql)。本項無新直接表。
- 建議登入和 WP404 ACL 的 `POST /api/wip/outsource-orders/{osNo}/cancel-shipment`。重查狀態、nullable DataStamp、ERP 單號與逐批最新可逆步驟，明確處理固定及臨時外包兩路。外部呼叫使用冪等 request key 並保留回覆、對帳、補償與重試狀態，避免 ERP 已取消而 MES 尚未回復時重複發送。

## DEV

- WP402 可勾選產生 DEV ERP 單號。WP404 以共用資料展示出貨、未回貨、ERP 單號及回復記號守門，確認後把臨時外包單轉回未確認並清本機出貨資訊，可在 WP401 查到。沒有真 ERP、固定外包範例或批流程回復。已部分回貨的 Status=10 不進候選。

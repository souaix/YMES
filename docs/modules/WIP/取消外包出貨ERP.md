# WP404 取消外包出貨 ERP

## HOME整合：正式防護範圍實測通過（2026-11-20；provider待接）

目前入口 `/YMES/WIP/OutsourceErpCancel`，元件 `web/src/components/WipOutsourceCreate.vue`，共用元件模式 `cancel`。HOME已將Office e5f來源整合至既有KD框架，保留原三層選單及本機DEV操作。App只對本輪九個新元件掛 `office-wip-module`，統一外框單一主標題；不改其他模組CSS。

DEV資料、localStorage、假查找、查詢／命令及彈窗都必須同時滿足 `import.meta.env.DEV && developmentPreview === true`。正式包一律空資料、顯示「正式環境尚未開放此功能。」並停用查詢／異動／查找等未交付操作，保留關閉回首頁。不得以假流水號、假SAP／ERP／簽核／Lot變動回成功。fieldset內控制項驗收以有效 `:disabled` 狀態為準。

Root已在正式Web256 `ade411cc…`／API167 `3f0bdc7c…`逐頁實測 `WP404`／`/YMES/WIP/OutsourceErpCancel` 的限定防護通過：原生入口、正式空資料與未開放提示、KD單一主標題、無假lookup／DEV列或業務彈窗；本頁direct重進／reload後保持隔離，Close回空首頁。逐頁觀察檔 `.tools/gui-proof-20261120/et101-office-e5f-wip-observations.json` 以本頁code定位：menu#7、deep-refresh#8、direct-reenter#25、reload-confirm#26（#為1起算array序位）；主GUI journal本頁direct／reload／Close事件為94/95/96。

以上只記已實做的正式防護／導航範圍，**不是 `WP404` 正式provider或交易完成**；所需PG migration、權限、原子交易及外部整合仍由HOME按下方原生規格完成。Office持續原生Oracle唯讀來源、UI及規格，不連HOME PostgreSQL或SSH。本輪整體GUI尚未封存，最終數字、Git／main／新checkpoint仍pending，封存後另見[本輪QA](../../qa/20261120-et101-office-e5f.md)。


- 選單 FunctionNo 為 `OS CONFIRM_ERP_CANCEL`，路由為 `/YMES/WIP/OutsourceErpCancel`。核對 `STD/MESWin/WIP/WP_OSConfirm_ERP_Cancel/frmWIPOSConfirm_ERP_Cancel.vb` 及內嵌 Designer、`wsWIP.ChkReverseForOSCancel`／`Exe_ERP_OutSource_Cancel_Reverse`、`clsLot.funCheckReverseForOSCancel`／`funExcuteReverseForOSCancel`。
- 主查詢固定 Status=1，查詢條件與 [WP403](外包回貨.md) 相同。選一張外包單及明細，確認窗顯示 ERPNo_Out。舊 Form 逐批檢查最新 ReverseHistory 存在、外包站相符、CommitState=1，並重查單據仍是已出貨且 ERPNo_Out 有值。
- 先依 active ERP 呼叫 E10ICD 或 TIPTOPICD 取消。MES 回復前先把合格歷程的 CommitState 設成 0，失敗時補回 1。固定外包 OSType=1 的主檔改 -1，原 Form 傳全部歷程。臨時外包 OSType=2 的主檔回 0 並清 ERPNo_Out，原 Form 傳第一個明細批的歷程，後續回復範圍須依 `ExecuteReverse` 核實。

## 家用端交接

- OS Basis／Detail、ERP settings 及 ReverseHistory 使用既有 [WP401](外包單開立.md)、[WP402](外包單確認.md) 與 [WP215 CREATE-only](../../../database/reference/mestest_wip_wp215_create.sql)。本項無新直接表。
- 建議登入和 WP404 ACL 的 `POST /api/wip/outsource-orders/{osNo}/cancel-shipment`。重查狀態、nullable DataStamp、ERP 單號與逐批最新可逆步驟，明確處理固定及臨時外包兩路。外部呼叫使用冪等 request key 並保留回覆、對帳、補償與重試狀態，避免 ERP 已取消而 MES 尚未回復時重複發送。

## DEV

- WP402 可勾選產生 DEV ERP 單號。WP404 以共用資料展示出貨、未回貨、ERP 單號及回復記號守門，確認後把臨時外包單轉回未確認並清本機出貨資訊，可在 WP401 查到。沒有真 ERP、固定外包範例或批流程回復。已部分回貨的 Status=10 不進候選。

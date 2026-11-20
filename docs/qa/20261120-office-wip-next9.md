# HOME D1 九個 WIP 入口正式驗收（2026-11-20）

本輪正式部署與限定驗收已完成；公司端 Oracle 原生來源與 DEV 原型保留，家用端整合正式防護、KD 配色、框架選單與 Close。正式 WIP 查詢、庫存／批次／Hold／Wait 交易及外部 provider 仍未交付，不能用本報告宣稱所有模組功能完成。以下只是本次封存範圍。

## 正式來源與版本

- Office 來源 `095b3d267bc102687c32b25c9a3f593cf1ebfda9`；code `eff89213ccfbebadb024374487ec4e3410b2456c` 已正常推送 `refs/heads/codex/homepc-office-wip-next9-20261120` 並直接讀回一致，未 force。code binding `.tools/office-wip-next9-binding-eff89213ccfbebadb024374487ec4e3410b2456c.json` SHA `e8b6c225b31fd882261c61de14118a76415038e17dedd838a18c14bf558fef2b`，SQL1／API154／Web233＝388，未允許差異0。
- Web `423552b638fe578066905ef72d98b58c50e0645519dc7cfc64ae2ce95a715e75`；233 份來源 canonical `6a8b5567ca0d633c93c7a0d4d2f92f9d4ceb8bf0e57e9d0e74ded1f013e29590`；完整 Web 型別檢查／建置與封裝通過。公開資產 JS `index-C0Xm7ApK.js` 4192454 bytes，SHA `f1a91330f5da2c28c3b503c113eac1d38ad9b4100c0b2fa7922120cf5757a50a`；CSS `index-Banka2lH.css` 1105804 bytes，SHA `07c11938fa648f750e67a7280f2f2254a971b4adaf12a1e9246db48bf3d18c19`。
- API `8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e`／154 份來源與 DB `0059` 不變；本版只切換 `ymes-web`，source 私有700／600，public755／644，候選及 live nonroot 資產讀取通過。沒有 SQL fixture、cleanup、DDL／grant 或資料回填。
- 已完成 ET105 checkpoint SHA `e13f7023a76aca28a2cb7ab2c1d7c94f58c5f5de4f336a47f90d1a511ec1bfc4` 是本版基底；其 API／regression／105 檢查歷史保留。D1 metadata 收尾與文件 status commit 尚待 Root，不預填；mainSyncPending=true。QA 後新增 Office `f43fac5…` 的 WP505–508 四頁未納本版 merge／build／部署／GUI。

## 真實 app-browser 操作

Root 自行 Demo 登入，九個原生三層入口逐一進入、確認正式服務防護／空資料／KD 與單一標題、所有可見按鈕狀態、Close 回空白首頁，再重新進入及 Close。Query／lookup／交易在 provider 尚缺時不可假成功。各頁事件如下；合計數字不可當作單頁或純點擊次數。

| 功能 | 正式選單標籤 | 路由 | 原始事件 |
|---|---|---|---|
| WP212 | 併批處理_元件 | `/YMES/WIP/LotMergeComponent` | 3–6 |
| WP213 | 併批處理_等級 | `/YMES/WIP/LotMergeBin` | 7–11、43–44 |
| WP214 | 併批處理_載具 | `/YMES/WIP/LotMergeCarrier` | 12–15 |
| WP215 | 元件分片處理 | `/YMES/WIP/ComponentSplit` | 16–19 |
| WP914 | 生產批緊急狀態/優先權調整 | `/YMES/WIP/HotLotPriority` | 20–23 |
| WP501 | 立即異常設定 | `/YMES/WIP/ImmediateHoldSet` | 24–27 |
| WP502 | 立即等待設定 | `/YMES/WIP/ImmediateWaitSet` | 28–31 |
| WP503 | 批次立即異常設定 | `/YMES/WIP/BatchImmediateHold` | 32–35 |
| WP504 | 預先異常設定 | `/YMES/WIP/FutureHoldSet` | 36–42 |

WP914 live menu 確認是「生產批緊急狀態/優先權調整」；先期 audit 的「生產批緊急批/優先權調整」保留為歷史 caption，現在以 live 為準。WP213 另驗正式無假 Bin 欄或選項與 fresh 直接深路由；WP504 另驗 PS／OP、主次分類、異常項／描述無 DEV 假 metadata 與 fresh 直接深路由。

既有回歸包括 WP103／104／105／107／108／111／113、WP205／206／207／203／208／209／210／211、WP305／306／307 及 direct OperatorLogin。WP307 真實 100／150／75／50% 縮放與 Grid30×40 Apply 後重新開啟保存、設定窗／頁面 Close 通過；沒有宣稱 populated 生產區配置已驗或 WIP map provider 已完成。報關行與貨運行既有 API 讀取／新增入口可用；ET105 Taker 真 GUI NewApply→EditApply→clean Confirm、核准、取消停用、停用→硬刪、四狀態重查皆空完成。

末次 fresh Home 為空白首頁／21 項根選單預設折疊、側欄收合只留 icon 並保留名稱提示、再展開仍空白首頁；scrollbar `overflow-y:auto`，thumb alpha0.4／透明 track。`browserLogs=[]` 只涵蓋該 fresh Home／側欄收展後的末次讀取範圍，不追溯先前整段紀錄。

封存 `.tools/gui-proof-20261120/office-wip-next9-d1.json` 349640 bytes／SHA `5fc69cd06c58d7761e7f04d7e376b6b19572e255640dd1cd04e350b056ce26c3`：93 raw events，91 有效 PASS＝90 GUI＋1 獨立 SQL 讀回。兩筆工具 observation 的原 false 保留：75 的 TEL 欄位被遮蔽，76 以可見列表／audit 真復驗；81 在 batch busy 尚未完成前切 radio 逾時，82 等完成後真復驗。沒有產品變更或刪除失敗紀錄。四張 Root 已實看截圖 descriptor／bytes／SHA 保留在 proof，本報告不將圖片加入 Git。globalBrowserQaComplete=false、globalMesReleaseComplete=false。

## 獨立公開與清理讀回

- `.tools/office-wip-next9-public-runtime-d1-20261120.json` 5268 bytes，SHA `2f98318c2d7978f623a291c58a61927a9f639b7aedd05a6a69186b3ae5a86861`：系統正常 TLS 驗證，root／FutureHoldSet 深路由200與兩個真資產引用、JS／CSS MIME／bytes／SHA、`/ymes` 和 `/ymes/`404、公開 ready200，共7／7。live/current API8db／Web423 相符，UID1654／101、API五檔與Web四檔真可讀，internal ready200，前後 checkpoint／images 不變。這些另列，不加算 GUI91。
- `.tools/office-wip-next9-gui-cleanup-d1-20261120.json` 2040 bytes，SHA `6e2707f228a3595278109b86a4dee1b41bd8b20f6157e65823e8c58a750ebb24`：`GD1T-1120A` exact0、PG lower(btrim) logical0、creator=`ymes` 且 ContactorName=`D1_UI_20261120` marker0。唯一測試 Taker 全由 GUI 建立／刪除，SQL fixture0、SQL cleanup0；只讀三個 count，不讀業務列值。
- 維護 SQL `.tools/office-wip-next9-gui-cleanup-counts-d1.sql` SHA `6e026bd04d3204ad001f6c4c687b6a2e2a6f9a83acc833c56675be7bff5d6617`，BEGIN READ ONLY／timeouts。讀回前後 API8db／Web423、current tags／ready200／checkpoint e13f 均一致；不涉及 SQL 寫入、cookies／登入、服務或 Git 改動。

## 後續事項

保留各模組原生 Form／Service／Core、DDL／CREATE-only、DEV 行為與家用 handoff 全文。HOME 仍須依真 live schema 與完整來源完成受保護 lookup／WIP 交易 API、migration 差異與交易／外部副作用驗證；公司端沒有 PG 或 SSH 發行責任。公司新四頁另行正常整合並驗證後才能同步 main，本輪不宣稱其已部署。

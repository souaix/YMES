# 2026-11-20 公司WIP載具拆批／併批Web224 C1整合（限定GUI完成；main待同步）

本輪正常合併已驗Web222 release `3953bbad60df3ce2f94d7420135f4286e1366493` 與Office最新 `df10bd08af8ad44403ea82f94bef469afb7e401c` 至 `3f99c172787e43a8868cf435e4ab2f57a01f6d7d`。新WP210載具拆批／WP211併批只補正式資料隔離、次要命令／modal guard及既有KD框架；WIP providers未交付。**完整建置exit0、封裝、部署與公開HTTP七項，以及限定GUI73檢查點／73events／0observations已通過並封存。376綁定、release commit與正常main同步仍pending，mainSyncPending=true。** 224另建本版證據，不改已驗222的proof。

## 固定來源與待驗範圍

| 項目 | 實際進度 |
| --- | --- |
| C1來源 | 224檔，canonical `f9bb95660175064e2b0c989df9f09dbe52adf18f85c3a0650bf5753960c072e7`，manifest `.tools/office-wip-carrier-merge-web-source-manifest-c1.json` |
| 建置／封裝 | 完整型別／Vite實際exit0，封裝通過，未用舊版receipt替代 |
| 新Web image／部署／HTTP | 已部署 `e2df5e3df9884894f3805f5f454053c8e748c6256314ddc41f0ee4cf27988375`，公開HTTP七項通過 |
| API／資料庫 | API `6d2501aad008b6b5ca0c11abae0700d1f7a00b74933645c83d3a4c183dd0c171`、151檔canonical434bd27f…及DB `ymes`／schema `YMES`0058保持；本輪未重建API、未套DDL／grant／backfill |
| 待來源綁定 | SQL1＋API151＋Web224＝376；真建置／部署與commit存在後才執行，尚無本輪Git SHA |
| 正式GUI | 73通過檢查點／73events／0observations；18native＋1direct，限定正式guard／顯示與ET104 smoke |

Root自行Demo登入，實際操作新WP210／WP211入口、正式無假metadata／不可用提示與停用命令、Close／重進／direct fresh及19入口KD單一主標題；18native總數包含舊16，direct OP另計1。WP307 Grid30×40／四zoom、SD108空lookup界線及ET104新Web真CRUD smoke均通過。SD108沒有核准Area，不宣稱populated layout。DEV拆批分配、LotStamp、載具／庫存／派工與BR後續交易均不因正式防護而宣稱交付。

封存proof `.tools/gui-proof-20261120/office-wip-carrier-merge-current.json` 1111408 bytes，SHA256 `f2fa8af9974067dd6d37f343553277e5873429f235c51b05cdf4a2abfadbd462`；Root收據 `.tools/office-wip-carrier-merge-integration-gui-receipt-20261120.json` SHA256 `87802ec43a2b144b5c96c4cae80f55072ead0ab1174f324085c79096d8f337bd`。73檢查點不是73個純GUI click，0observations只指本輪正式封存scope，不抹除歷史工具問題。

GWC自有Broker／Forwarder兩筆全由GUI建立並硬刪，SQL fixture writes=0；獨立精準兩鍵broker=0／forwarder=0，清理收據 `.tools/office-wip-carrier-merge-smoke-cleanup-20261120.json` SHA256 `3a5c9a4a387a8a8ae44d6a5c7d18aea36877502956eb61da1c2c3c8aac46e36d`。最後fresh reload空首頁、21收合選單及本次fresh reload logs[]；不延伸為所有歷史操作均無log。獨立peer結果另依實際收據，不預填尚未提供結論。

本次家用端程式範圍為App／style／Carrier／Merge；Bin只沿合併基底保留Office rawRecords三行alias，原有正式guard不移除。AreaShow、Favorite、Forwarder與Component須保持已驗222 release原Git blob；13個原始Office Vue保持b3f原blob。原生來源與兩新頁作者的DEV／provider限制段落保留。

## 歷史與主分支界線

Web220 release `dc34ee6fc02f5949f4ffdfbbd4673e4232a9957a`／372綁定及正常featurebranch推送已完成，保留原86項proof與私有e51a6450…紀錄。Web222 release3953bbad／374綁定（收據SHA256 `c0eed78c80760b1aa7381a5dcc6f2a2c4c97d4f671649a0edbb7fff6ae3b8c32`）／featurebranch正常推送及遠端讀回已完成；69項proof346d017b…／70events／1已解timing觀察不改。其小型本機分支紀錄SHA256 `71fa671a2aecbc720014be331734ce0c3dfedfdd78f90f299f540d3c9a6741d4`，私有上傳另待實證。

Root先前擬推合併main時，automatic review因兩新頁尚未驗證而拒絕，該次main push沒有執行。Root改完成C1全建置／部署／正式GUI後再正常同步，沒有繞過拒絕；main目前仍為Office df10，沒有宣稱家用新merge已到main。

ET104 A1完整checkpoint9cbe／原API169HTTP621assert／GUI87保持原史實，220與222 metadata未套用。後續新metadata必須保留完整歷史，不能將舊proof改配224。ET105僅隔離候選與純讀peer，尚未build、migration或正式部署；WIP providers與完整MES持續待辦。

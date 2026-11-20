# HOME E1 四個 WIP 入口正式限定驗收（2026-11-20）

E1已正式部署，四頁防護與下述回歸完成；保留一次真HTTP520未知結果及三筆已復驗工具觀察。正式 WIP Set／Release、查詢、ERF五頁明細、附件／OCAP／BLOB與WP801／8D provider仍未交付，本報告不宣稱全功能或全MES驗收完成。原生Forms／Service／Core及CREATE-only來源、DEV行為與HOME後續契約均保留於各模組MD。

## 版本與部署證據

- Office來源 `f43fac5a0a600a1674e05664ad8156f2107d12b9` 與D1／狀態a074已在本機正常合併，HEAD `47c8e549c9f2b8b75635fd1d5406b2c2b67afcea`。392來源（SQL1／API154／Web237）binding `.tools/office-wip-next4-binding-47c8e549c9f2b8b75635fd1d5406b2c2b67afcea.json` SHA `4c478e6005a4a9af4a6aa74b3fe798af3e45fb296d8a0ed2136c583617c7b49e` PASS；code已正常推送 refs/heads/codex/homepc-office-wip-next4-20261120 並直接讀回47c8e549c9f2b8b75635fd1d5406b2c2b67afcea；文件狀態提交與metadata收尾仍待完成。QA後fetch到Office 6870eaf的WP801／802新頁，另輪接手，main pending。
- Web `5ff068fc556b6449485dc40ea5b399ff2f773609c59ee19ee529aab1fbd9faae`／source237 canonical `25bd965ebc6e71dc41468b9a11102d53a4444a82aadc48ac12985dfdd76dde8a`，Root完成full build/package、私有來源700／600安裝、公開檔755／644、候選nonroot資產讀取與正式Web-only切換。`.tools/office-wip-next4-deployed-e1-20261120.log`是實際部署；manifest `deployed=false`是較早封裝歷史快照，原件未改。
- API仍 `8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e`／154份來源、DB0059；本輪無DDL、grant、資料回填或SQL清理。D1完整checkpoint `8918a1748840b791ac63a618222fc60b7f71465864bf8980a4fdb0439faf4037`及其原API snapshot／regression／ET105105保留；ET106155是另行build-only候選，未取代此正式API紀錄。E1 metadata收尾待Root。

## 真實 app-browser 範圍

Root自行Demo登入。四頁均由「在製品管理 / 異常與等待」原生三層選單進入，實驗KD／框架單標題、正式無fixture或假選項、所有可見命令／handler防護、Close回空白首頁、重進與Close；四個直接深路由fresh也已重驗防護及Close。

| Key | 正式選單 | 路由 | 原始事件 |
|---|---|---|---|
| WP505 | 預先等待設定 | `/YMES/WIP/FutureWaitSet` | 3–7、21–22 |
| WP506 | 預先異常解除 | `/YMES/WIP/FutureHoldRelease` | 8–11、23–24 |
| WP507 | 預先等待解除 | `/YMES/WIP/FutureWaitRelease` | 12–15、25–26 |
| WP508 | 異常單資料 | `/YMES/WIP/ErfBasis` | 16–20、27–28 |

WP505正式PS／OP不含示範選項；WP506／507查詢／解除與選取仍待真provider；WP508 Info／8D停用、無DEV附件／處置列，不將View訊息當授權下載、Delete當正式刪附件或8D入口當交易完成。

既有回歸包含WP205／206／207／203／208／209／210／211／212／213／214／215，WP103／104／105／107／108／111／113，WP305／306／501／502／503／504／914與WP307、direct OperatorLogin。WP307實驗100／150／75／50%縮放與Grid30×40套用／重開保存、設定及頁面Close；沒有真populated生產圖或WIP交易provider驗收聲明。Broker與Forwarder正式API讀取正常；Taker GUI測試另述。

封存 `.tools/gui-proof-20261120/office-wip-next4-e1.json` SHA `281154eef1fa4d498fb5273849b4c21da90eef5232d9a4ed6b0823414f1c4ac2`：89raw／85有效＝84GUI＋1獨立SQL讀回，四項observations原件均保留。事件67記錄真HTTP520 unknown-save與介面阻擋重送，原`passed=true`是觀察成立，`expectedOperationPassed=false`，**排除New操作通過**，不能按passed欄直接計86。另三筆工具觀察：72等待訊息文字不符→73真核准；74空白比對未匹配DOM cell→75真已簽核列；84reload登入初始化時機→85既有session恢復後fresh Home。不能把四項全部叫harness，也不能把67洗成成功。

Root關閉原未知結果視窗，68重新查詢當下未見測試鍵後，69 GUI NewApply成功轉Edit、70 EditApply採真server audit、71 clean Confirm不重送，73核准、75已簽核篩選、76已核准刪除轉停用、77不使用篩選、78取消刪除保選取、79停用列硬刪／清單0、80–83四狀態精準查詢均0。`GE1T-1120A`全由GUI建立／刪除，SQL fixture0／cleanup0；沒有補SQL讓頁面假成功。第一次520根因與提交時點仍未確定，後續成功與最終清零只證明其各自時點的結果。

事件85真fresh Home空白／21項預設折疊／alpha.4透明按需scrollbar；86收合寬72／21icons和名稱提示，87展開，88最後fresh Home範圍console error／warn空。log範圍只限這次fresh後讀取，不追溯先前整段，也不重算歷史D1／ET105檢查數。

## 獨立讀回與異常界線

- 最終cleanup `.tools/office-wip-next4-gui-cleanup-e1-20261120.json` 2078 bytes／SHA `e2047126fd390fa270490df5ac931feb91ebd09873a1631212b005176f6a773a`：exact GE1T-1120A、PG lower(btrim) logical、creator ymes＋兩marker E1_UI_20261120／E1_UI_20261120_EDIT三範圍全0；source SQL `.tools/office-wip-next4-gui-cleanup-counts-e1.sql` SHA `2a7143e6b4538f331c25a67efe00a0d36282d86ec2e8e717fa1839af13c4988b`，READ ONLY／timeouts，只讀COUNT無業務列值。這是事件89的1筆非GUI檢查，不是第85次click。
- 最終public/runtime `.tools/office-wip-next4-public-runtime-e1-final-20261120.json` 5258 bytes／SHA `2dc935b8b97fcdaca5ac5e46834c0545f71f475d9a8c2b9e50d2b04b85074c0e`：release正確e1，正常TLS共7／7，root／ErfBasis deep200且真assets引用、JS `index-BXyy8TRa.js`4239302 bytes／SHA `e115c4dbf5e9f61e60d695bb894c6b0c86a9c0219a11e10e995b20121fa06305`、CSS `index-CUHRxDdT.css`1122660 bytes／SHA `ece4555894c27f2ce3e4c9ebb64b4909df7999210210ebfa1c47c3ed325394be`／MIME正確、兩小寫/ymes路由404及publicready200；live/current images相同、UID1654／101、API5檔／Web4檔可讀、internalready200，前後CP8918與images不變。七項另列，不加入GUI85。
- 原public receipt `6684fedf…`之release literal是legacy d1標籤，image／asset與七測真值是E1；原件保留，沒有覆寫。新final receipt是Root授權的後續真實再次讀回，不只是改label。
- unknown-save診斷 `.tools/office-wip-next4-unknown-save-readback-e1-20261120.json` SHA `b6fe9617fe70f5ce0b5d7ed7f31bb3872c03d47644826ac8f960a8df62165980` 在其時間讀到state0/stamp1一筆；不可倒推520當時已提交或用它反駁更早GUI重查。API／Web／CP穩定、internalready200、限定10分鐘logs無相關exception；無write retry／SQL清理。
- 公開edge分類 `.tools/office-wip-next4-edge-classification-e1-20261120.json` SHA `92e19338e1ab828174889e03f33572398c8d447bac25a0cdb2668d8952e3e66e`：當時Python-default UA收到Cloudflare403、沿原獨立UA與normal curl都200。這說明該次403的client差異，**不能確證原520根因**。所有原始proof／收據／非成功事件保留。

## 後續維護

HOME負責PG復刻live schema/history及必要差異migration、正式lookup／查詢、權限、transaction／CAS／冪等與actor、附件授權串流／刪除及WP801整合；Office只負責原生MES來源／Oracle唯讀查證與UI原型。WP505／506／507及WP508 providers仍待完成，CREATE-only來源不等於已套live schema。本次程式與來源固定於已驗source237；後續Git與metadata必須另以Root真實結果更新，不以本文件預填成功。

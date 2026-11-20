# YMES / MES 資料庫連線與憑證處理

## D1 Web233 已正式部署與限定驗收（2026-11-20；DB 維持0059）

Root 已完成 Web233 正式部署 `423552b638fe578066905ef72d98b58c50e0645519dc7cfc64ae2ce95a715e75`，source canonical `6a8b5567ca0d633c93c7a0d4d2f92f9d4ceb8bf0e57e9d0e74ded1f013e29590`；API154 `8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e` 與 PostgreSQL `ymes`／schema `YMES`／migration `0059` 保持。HOME 負責 PostgreSQL 復刻、PPK SSH、API／資料庫與發行；Office 維持原生 MES Oracle 唯讀來源及 UI，沒有 PG／SSH 工作轉派。

ET105 完整 checkpoint 已由 Root 實際套用並下載獨立讀回，SHA `e13f7023a76aca28a2cb7ab2c1d7c94f58c5f5de4f336a47f90d1a511ec1bfc4`；下方「最後套用待 Root」是先期文件合併快照，保留為歷史，不是目前狀態。D1 正式 GUI 封存 93 raw／91 有效 PASS＝90 GUI＋1 SQL，兩工具 observation 75→76、81→82 保留原 false 且復驗完成。九個新入口及重進／Close、兩個 fresh 深路由、既有 19 項含區域圖與 direct OperatorLogin、ET105 真 GUI 建核准停用硬刪回歸完成；末次 fresh Home 21 項預設折疊、收合 icon／提示、透明度 0.4 scrollbar 與該 reload 範圍 logs[] 通過。不是整套 MES 或 WIP 交易 provider 已交付。

獨立正常 TLS 七項與 runtime live/current image、UID1654／101、必要檔案及 ready200 均通過。GUI 唯一 Taker `GD1T-1120A` 全由 GUI 建立／刪除；SQL fixture0、SQL cleanup0，獨立 exact／logical／actor+marker 三範圍各0。詳見 [D1 QA](qa/20261120-office-wip-next9.md)。

code `eff89213ccfbebadb024374487ec4e3410b2456c` 已正常推送 `refs/heads/codex/homepc-office-wip-next9-20261120` 並直接讀回同值；SQL1＋API154＋Web233＝388 份來源綁定 PASS，receipt SHA `e8b6c225b31fd882261c61de14118a76415038e17dedd838a18c14bf558fef2b`。文件 status 提交／D1 metadata 收尾待 Root 實際執行，不預填 commit。main 仍待整合：QA 後 origin 有 `f43fac5…` 的 WP505／506／507／508 四個新 UI handoff，尚未納入本版 merge、build、部署或 GUI；本版不宣稱 main 同步。保留以下原生來源、CREATE-only、歷史紀錄及未完成事項；本輪未套任何九頁 CREATE 或新增資料庫權限。


## 本輪九個 WIP 入口整合中（2026-11-20；尚未建置／部署／驗收）

本節是這次文件合併時的最新狀態：家用基底為已正常同步 feature 的 ET105 code `ab0f8269e9778d88811947512e6753e51437703d`／status `a921b44e9b46aba20f6a8f66f4bd8944d79a18fb`；API `8db4b840…`、Web `0c2852e1…`、DB `0059` 已有前述實證，ET105 完整 checkpoint metadata 的最後套用仍待 Root 親自執行與讀回，不能從本次文件解衝突推斷已完成。

公司端 `095b3d267bc102687c32b25c9a3f593cf1ebfda9` 的九個新 UI、模組規格、來源 DDL／CREATE-only 與家用 handoff 在本工作區整合中；尚未完成這九頁的家用建置、正式部署與 app-browser 驗收，也未因本輪合併套用任何九頁 CREATE／migration 或完成 WIP 交易 provider。既有 reference 不代表 PostgreSQL live 表已建，家用端須先核實 live schema／history 再決定必要差異。

本機維持 HOME PC：PostgreSQL 復刻庫、PPK／SSH、API／資料庫與發行由家用端負責。公司端只讀原生 MES Oracle 來源並復刻 UI；以下公司 handoff 內的「本機」Oracle profile 指公司電腦，不是家用 PostgreSQL。兩側原有內容與歷史均保留，後續狀態以實際 Root 收據為準。

## ET105取貨公司：0059已套用，API與正式頁面已驗（2026-11-20）

本機是HOME PC，維護資料庫`ymes`、schema `"YMES"`的PostgreSQL復刻庫。Root以既有PPK SSH核對原生13欄表已存在，完成備份後，0059只補三個default與ymes_app本表CRUD；沒有重建表、回填舊資料或新增ET106表。custom備份634856 bytes／restore-list1806，SHA`c3200aaf96648827d980f6725ba9a11152d39a649d924754a268a876505af51d`；原子allowlist與額外獨立讀回通過，完整證據見[ET105 QA](qa/20261120-et105.md)。

目前正式API為`8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e`（154份來源），Web為`0c2852e15d14fe2c1e4a6ac396f7d815feef42695e0f88857cfd1fb7ca5ba815`（224份來源）；正常HTTPS七項、實際UID1654／101、必要檔案讀取與ready200均通過。API八組184HTTP／1853檢查、精準兩scope與Holder清零；GUI105有效檢查、七筆全GUI建刪與獨立七鍵清零完成，SQL僅一筆自有F版本負例，沒有SQL清理。

code `ab0f8269e9778d88811947512e6753e51437703d`已正常推送`refs/heads/codex/homepc-et105-20261120`並直接讀回相同；SQL1／API154／Web224共379份來源綁定通過，receipt SHA `2ac94056c8c82c76eedf698e64fcaa242f388a66f74a0777db1522c9c3690bae`，未允許差異0。後續純文件狀態提交不改本release code或重建產物。 mainSyncPending=true；公司新增九頁UI／來源CREATE尚未合併本次224。公司端維持原生Oracle唯讀來源與UI，沒有PostgreSQL或SSH部署責任。以下C1／舊版本是各次歷史快照；C1完整checkpoint71933b09…已保留，舊0058與API151歷史不改寫成0059證據。

## 公司WIP載具拆批／併批Web224 C1（2026-11-20；無資料庫異動）

Office df10bd08與已驗Web222 release3953bbad正常合併至3f99c172，Web224 canonical `f9bb95660175064e2b0c989df9f09dbe52adf18f85c3a0650bf5753960c072e7` 已完整建置exit0、封裝並部署Web `e2df5e3df9884894f3805f5f454053c8e748c6256314ddc41f0ee4cf27988375`，公開HTTP七項通過。Root限定GUI73檢查點／73events／0observations封存；ET104 smoke兩筆全GUI建刪，SQL fixture writes0、獨立精準Broker／Forwarder皆0，清理SHA256 `3a5c9a4a387a8a8ae44d6a5c7d18aea36877502956eb61da1c2c3c8aac46e36d`；fresh reload空首頁21收合選單／logs[]。C1 code bacf0ee5的376綁定399f648b…與featurebranch正常推送／遠端讀回已完成，main／feature-only checkpoint仍待後續實證。本版API仍6d2501…／151份，DB `ymes`／schema `YMES`仍0058；未新增DDL、grant、資料回填或WIP交易provider，不能把公司新reference當成已套schema。

Office新兩頁0b342616／d5a8bd13未合併／部署本次224；main正常push雖已獲automatic review允許，Git遠端nonFF拒絕後未force，mainSyncPending=true。保持完整A1及220／222歷史，先封存C1分支版本並接ET105 API，不以未驗新頁改寫376來源或資料庫紀錄。

220 dc34／372及222 3953／374正常featurebranch推送與原proof86／69保留；完整A1 checkpoint仍9cbe，220／222 metadata未套用。WIP providers待逐項接手，ET105僅隔離候選與純讀peer，尚未build／migration。C1實證另見[Web224 QA](qa/20261120-office-wip-carrier-merge-integration.md)，mainSyncPending=true。

## 公司WIP拆批Web222（2026-11-20；資料庫仍0058）

Office12c6a2e與已完成分支同步的Web220 dc34ee6正常合併至32d422b5，新Web來源222檔canonical `ee360b1d9fd47f130df900de539402bda2a2e73a1cd5e6acdcdbca1c9f5df0d5` 已完整建置並部署 `b330b9b009670ea092218b8ecffdc6f1c389e9595a572ebf5ab471afd3e377e8`，公開HTTP七項通過。API仍6d2501…／151份，DB `ymes`／schema `YMES`仍0058；本輪未新增DDL、grant、backfill或資料庫交易provider。

Root已封存69通過檢查點／70events／1已解menu timing harness觀察，涵蓋16個native入口＋1個direct、WP307 Grid30×40／四縮放、共用SD108空lookup／Grid／zoom及ET104新Web完整smoke。2筆資料全GUI建刪、SQL fixture writes=0；獨立精準broker／forwarder皆0，cleanup SHA256 `d8075801657e31dfed1aa792ca63a7affc1bb2323738ef0f46b5110d8f619c46`；fresh reload空首頁／21收合選單／logs[]。WIP正式providers均未交付，SD108未驗有資料的layout；ET105僅隔離候選與純讀peer，尚未build／migration。mainSyncPending=true，374來源綁定及main同步待實證，詳見[Web222 QA](qa/20261120-office-wip-split-integration.md)。

Web220 dc34ee6的372綁定／正常featurebranch推送／私有7份證據已完成；其86項proof、原ET104 A1完整checkpoint9cbe與schema0058歷史保持，未套用220 metadata，不改舊證據配新來源。

## 公司WIP Web220整合（2026-11-20；資料庫維持0058）

本輪正常合併公司b3f0f06與家用A1至基底7e71e38，已建置並部署Web `823d0571d6dc82436ae5eeace7a68b8c22e8baf15dcbf0b478260d7a15224194`／220檔canonical `8071ec80f982071775cdcbd40be6d4b0509cf725e2662468bad17b19df049c46`。API仍6d2501…／151份、DB `ymes`／schema `YMES`仍migration0058，這次Web部署沒有新增DDL、grant或backfill；公司新增reference CREATE SQL僅是來源，不代表已在復刻庫套用，GUI若需自有fixture另依實證記錄。

WP203設備收藏與WP307區域WIP等正式provider仍待家用端接手，頁面先明確隔離DEV資料並停用未提供業務操作，不以空畫面宣稱資料庫／API已完成。ET105僅來源及metadata唯讀稽核，尚無API或migration。公司端仍為Oracle唯讀來源／UI角色，不新增公司端PG／SSH任務。

原ET104 A1的0058備份／權限／API169／621／GUI87及featurebranch X65f17a0／Yb015e51、完整9cbe checkpoint保留。新Web限定GUI已封存86通過檢查點／87events／1已解harness觀察，smoke兩筆全GUI建刪、無SQLfixture寫入，獨立精準broker／forwarder皆0；不是重跑A1整套或交付WIP providers。372來源綁定與本版featurebranch推送仍待實際完成，mainSyncPending=true。

Office後續dbebd0f／12c6a2e尚未納本次220部署／驗收，後續另做Web222整合；維持API6d／schema0058及220原始proof，不能為同步新版重寫既有證據或繞過真正main gate。詳見[整合QA](qa/20261120-office-wip-integration.md)。

## ET104 A1 正式驗收：0058既有貨運行預設值與權限（2026-11-20）

ET104 A1 release code `65f17a0c347acd4b5df2a1be1cad712138ececd6` 已正常推送 `refs/heads/codex/homepc-et104-20261120`，直接 `ls-remote` 讀回相同。358份來源綁定通過（SQL1／API151／Web206，未允許差異0；舊API131份僅representation、Web188份僅Git EOL差異），無force push。後續純文件狀態提交不改此release code；actual status SHA與遠端讀回另封存於checkpoint。mainSyncPending=true：公司b3f0f06的14個新WIP Vue／App／AreaShow將正常合併、另建新Web與驗收後再同步main，不能把A1的87項證據套用新產物。

HOME PC以PPK SSH核對資料庫 `ymes`／`"YMES".tblentforwarderbasis`，既有15欄、PK1／index1及NULL型別皆符合來源。只有IssueState DEFAULT0／CreateDate DEFAULT LOCALTIMESTAMP／DataStamp DEFAULT0及本表CRUD缺少，依[0058](../database/migrations/0058_et104_forwarder_defaults_permissions.sql)補齊；原SQL SHA256 `6ac46dfb0c09241c606665556de01a25e12349fd574993487b2ee900e28d38cb`不改。無CREATE／業務回填／FK／額外index／新簽核表。

完整custom備份 `/opt/ymes/backups/ymes_before_0058_et104_20261120T003221Z_TNcGiFLs.dump` 634316 bytes、restore-list1805 entries，root目錄700／檔600。交易內差異只允三DEFAULT與Forwarder CRUD；0057既有brokerno column SELECT、Broker／Customer完整metadata與ACL、schema／defaultACL／roles／membership／原索引約束保持。提交後獨立讀回及零列app probe通過；證據 `/opt/ymes/backups/et104_0058_reviewed_VbtntSyG`，本輪單次成功，不重跑0058。

API151／Web206已正式部署，API169／621通過，11自有scope＋Holder全0。正式app-browser87項＝77GUI＋10SQL，12Forwarder均GUI建刪；9筆Broker查找與一筆Customer引用只作自有SQLfixture，最後11scope獨立全0、三陣列[]，來源15欄／DEFAULT／原欄ACL再次讀回相符。原NULL／raw／signed版本保存及New-parent／既有parent邊界見[模組](modules/ENT/貨運行基本資料.md)與[QA](qa/20261120-et104.md)。

A1來源358份的Git分支同步待actual X/Y讀回；公司新WIP提交另正常整合並驗新Web版本，不能將本節206份證據套到220份來源。

## ENT ET103 既有報關行default與最小權限：0057（2026-11-20）

- Root以PPK READ ONLY核對三schema及全本地catalog，`"YMES".tblentbrokerbasis` 已有原生十三欄、單欄PK及唯一PK index，無user trigger／RLS，僅三來源default與Broker CRUD缺少；不是缺表，不CREATE／重建。真reference為Customer.brokerassigned與Forwarder.brokerno，前者已有SELECT、後者缺column SELECT。
- 成功套用前完整custom-format備份 `/opt/ymes/backups/ymes_before_0057_et103_20261119T231212Z_7Gf2l4zo.dump`，633770 bytes、目錄700／檔600，`pg_restore --list` 成功1803 entries。之後才以原SHA256 `bfd3f93aa70a91e64fed515547c8b84a1b62aa4970938008723704ac22835aa4` 套 [0057 migration](../database/migrations/0057_et103_broker_defaults_permissions.sql)。只補IssueState0／CreateDate LOCALTIMESTAMP／DataStamp0、Broker CRUD及Forwarder `SELECT(brokerno)`，不回填既有NULL、不加FK／index或改Customer權限。
- 原transaction內前後完整metadata allowlist僅允許三default及指定ACL增量，Customer catalog／ACL、schema／defaultACL／roles／membership、原indexes／constraints與無關grants保持；提交後獨立source13／三default／PK1／index1、column differences0及snapshot全等。app role零列SELECT probe通過；Forwarder table SELECT=false、column SELECT=true，無business讀列／DML。
- 前兩次失敗原log保留：PowerShell尾CR令input path不合；direct流程在ALTER前snapshot的零維空ACL array失敗。Root另READ ONLY確認三default與權限仍原baseline，再只修helper為strict `aclexplode(a.attacl)`、重新完整備份後成功，沒有憑ack0盲重跑。私有證據 `/opt/ymes/backups/et103_0057_reviewed_mXDgnSeb`；本機 `.tools/et103-migration-applied-20261120.json`、`et103-migration-apply-fixed-20261120.log`。
- **Schema已0057，ET103 API／Web已正式部署**：API `12f26b18e94a85e16b4192d815f07ea2698b3ec5ddbfc21c8e589fc98a413d10` ready200／nonroot runtime讀取通過；Web `b3949d57fd39db048cfebe566287d4bbc11d47c4c3007e079960893f728ec871`，206私有source canonical070fad5c／700與600、公開755與644／HTTPS資產MIME／bytes／SHA通過，API不變。API單輪八組126HTTP／464斷言通過，11自有資料scope＋holder0、metadata保持／logout204，獨立11項再讀0。
- Root正式GUI封存59項＝56GUI＋3SQL、0 observations（僅最終此範圍）；10筆Broker均GUI建刪，兩個自有Customer／Forwarder引用只由SQL setup／精準清除且Broker R不改。N raw／11NULL及signed stamp前置由SQL調整，真GUI續存與獨立讀回另記；不是完整簽核或其他ENT writer已交付。最終三array `[]`、GUI精確11項獨立計數全0，fresh reload logs `[]` 僅該次範圍。**355份來源綁定通過，release code `0c65a655fae538280244289e281733f612a0eb4f` 已正常推送main並fetch確認相同，沒有force push**；原0057兩FAIL與部署前PK index query修正歷史保留。完整範圍及證據見 [報關行模組](modules/ENT/報關行基本資料.md)、[ET103 QA](qa/20261120-et103.md)。

## ENT ET102 客戶連絡人結構與權限：0056（2026-11-20）

- HOME PC root先以PPK SSH唯讀核對，Contact表／PK index／同名type在所有schema缺少；既有Customer真表41欄、無RLS，0055提供的SELECT及 `UPDATE(customerno)` 實際可用。本批不重建Customer、不擴充其權限。
- 完整 `ymes` custom-format備份 `/opt/ymes/backups/ymes_before_0056_et102_20261119T215151Z_YfRMIaQl.dump`，631972 bytes；目錄700、檔600，`pg_restore --list` 成功、有1799個非空條目，之後才套 [0056 migration](../database/migrations/0056_et102_customer_contactor.sql)。備份檔名採UTC，本節日期採家用端本地日期。
- 原SQL SHA256 `735e8942311c7ab897f9206071229f3e691ee22f14638be0cfdb5e17fdb6356b`；私有凍結副本在備份後先preflight，原BEGIN內再檢不存在後CREATE／GRANT／COMMIT。保留原source與NOT APPLIED歷史註解bytes；實際已套用狀態以本節和root receipt為準，不能直接重跑。
- 提交後獨立READ ONLY讀回 `"YMES".tblentcustomercontactor` 十三欄型別／順序／NULL／default差異0，物理首欄ContactorName、次欄CustomerNo；唯一PK順序 `(customerno,contactorname)`，一個PK index、user triggers0、RLS policies0、ACL差異0。DataStamp原生nullable DEFAULT0，沒有IssueState、FK或額外index。只有新Contact表授app CRUD；Customer完整catalog／ACL、schema／defaultACL／roles／membership前後保持，未seed或匯入業務資料。
- app實際role對Contact十三欄及Customer四個reader欄作 `WHERE false` SELECT probe通過；SHARE所需Customer key UPDATE只讀metadata確認，不在READ ONLY做row-lock。原始證據位於 `/opt/ymes/backups/et102_0056_reviewed_fxGCeyi4`，本機收據 `.tools/et102-migration-applied-20261120.json`、`et102-migration-apply-20261120.log`。
- 隨後ET102 API實際部署為 `0339807d5b7e13be6efeec1ed72cdbaef71619af3f7c3fe46cd88ce9f0d87c2a`，切換前五個runtime檔案通過真nonroot讀取gate，readiness200；切API當時Web仍fc83。之後不可變API145／Web206來源建置與打包均通過，Web實際換為 `6a4f390f37b80c1579b79579dba625a702e753f11f52e71638dbbb8cdb3b3f67`。API八組單輪98HTTP／406斷言通過，七個自有資料scope＋holder0、metadata保持、logout204，獨立七項再讀0。
- Root正式app-browser完成53個通過檢查點（52GUI＋1SQL清理讀回），另兩次過早讀表／空提示tr計數觀察保留，不算產品修正或55次全部通過。六Customer只作SQL lookup，11筆Contact均GUI建立／刪除；A／C stamp與N raw／NULL為自有SQL測試條件，Customer不改。GUI刪完92為 `[]`、91只剩六Customer，90-cleanup後再獨立91七項全0；fresh reload logs `[]` 僅該範圍。**352份來源綁定通過，release code `7b6771e4718a56a950f4a4de5c61564ea8ed4137` 已正常推至main，fetch確認相同，沒有force push**。完整操作、SQL條件及未GUI分支見 [ET102模組](modules/ENT/客戶連絡人資料.md)及 [ET102 QA](qa/20261120-et102.md)，不擴稱完整MES驗收。

## OPB03 客戶站點對應結構與最小查找權限：0055（2026-11-20）

- HOME PC root 以 PPK SSH 唯讀核對所有 schema：`tbl_bopcustopmapbasis`、`tbl_bopcustopmapdetail`、兩個 PK index 名稱均不存在，type 衝突0；真 Customer／PS／OP 已存在。Customer 原無 SELECT／row-lock UPDATE，PS／OP 必要權限原已可用，未另建 lookup 表。
- 完整 `ymes` custom-format 備份 `/opt/ymes/backups/ymes_before_0055_opb03_20261119T201650Z_ztd3uMlC.dump`，627945 bytes；目錄700、檔案600，`pg_restore --list` 成功且有1789個非空條目。檔名採UTC，本節日期採家用端本地日期。
- 備份驗證後套用 [0055 migration](../database/migrations/0055_opb03_customer_op_mapping.sql)，原始 SHA256 `734eca05b5a335b5eafb5e6b4090452791959b761bd4372300a9de65caa54e8d`；同 transaction 內再查物件不存在後 CREATE／GRANT／COMMIT。保留原source SQL bytes與歷史註解，沒有 DROP、搬業務資料或 seed。
- 提交後獨立 READ ONLY readback：Basis11欄、Detail12欄，共23欄的順序／型別／NULL／default差異0；兩個 PK分別2／3欄、兩個 PK index、user triggers0、RLS policies0、ACL差異0。兩個原生 DataStamp 均 nullable DEFAULT0，無 IssueState、FK或額外index；明細 OPNo 可以在不同客戶站點對應重用。
- `ymes_app` 兩個owner表 CRUD；共用 Customer 僅 SELECT及 `UPDATE(customerno)` 供 `FOR SHARE` 鎖，沒有表級 UPDATE或其他欄 UPDATE。PS／OP lookup權限、角色及lookup目錄保持不變。`SET LOCAL ROLE ymes_app` 零列 SELECT probe通過；本次metadata檢查沒有讀取業務內容、沒有在 READ ONLY內做row-lock或DML。
- 收據：`.tools/opb03-migration-apply-20261120.log`、`.tools/opb03-migration-applied-20261120.json`；migration時 API尚未發行。之後 root已正式啟動 OPB03 API `2a2291cc11630c3ff151f0821fe5ef4a789e63cf1933c319069da379765b1b2f`，ready200；相容回退候選 `aab09e841b7b8dd47ed60152aa16edd14fd77ac15631111c25149e34428b4398`。兩候選均通過實際非root讀取gate；切換時 Web仍為既有 `cfa5cf288359`。
- Main142／compatible139份不可變API來源各自完成 .NET9 Release隔離建置，0 warnings／0 errors，詳見 `.tools/opb03-api-manifest-a1.json`。**正式API regression、OPB03 Web發行／app-browser、測試資料最終清理與GitHub同步仍待實證**；不能將 schema與readiness成功寫成主從操作驗收通過。維護/API界線見 [客戶站點對應設定](modules/OP/客戶站點對應設定.md)。

> **本批完成（2026-11-20）：已正常同步GitHub main `487e9d1`。** release code commit為 `487e9d1d15489be5d864c54abc832b81724f579d`；0054、API1d09／Webcfa及實際API／GUI／精確清理證據已與1份migration／139份API／206份Web核對一致後正常推送。下方Git待推保留為同步前歷史；本次純文件狀態補充不改schema、產物或release code，完整Job provider仍未提供。

## OPB02 ECN 作業站群組結構與權限：0054（2026-11-20，schema／本批API及GUI已驗並同步main487e9d1）

- root 先以 PPK 唯讀查所有 schema，兩個 ECN 精確表名及已知 workflow candidate 均不存在；原 OP／PS 查找及 row-lock 權限可用。套用前再次確認表／PK index 名稱無衝突，不接管既有物件。
- 完整 `ymes` 資料庫 custom-format 備份 `/opt/ymes/backups/ymes_before_0054_opb02_20261119T181813Z_kaHec4fs.dump`，**624893 bytes**；目錄700、檔600，`pg_restore --list` 成功且有 **1780** 個非空物件條目，之後才套用0054。備份檔名為UTC時間，本節日期為家用端本地日期。
- 正式套用的原始 [0054 migration](../database/migrations/0054_opb02_ecn_operation_groups.sql) SHA-256 為 `be53744a0177aaa4b422ddea8f45e93c6a33348d1e24f1d016490d33f470c6cd`；保留原LF bytes及開頭DRAFT歷史註解，正式狀態以本節及 `.tools/opb02-migration-applied-20261120.json` 為準。交易內重查ABSENT後原子CREATE／GRANT／COMMIT成功，沒有DROP、seed或重建共用表。
- 獨立READ ONLY讀回 `tblopecngroupbasis` 11欄／`tblopecngroupdetail` 2欄，型別／順序／NULL／default差異0；PK各1、PK index各1、user trigger0。Detail主鍵是全表 **OPNo**，不是群組＋OP；不造child stamp／audit或FK。
- `ymes_app`：Basis CRUD；Detail SELECT／INSERT／DELETE，只有 `UPDATE(opno)` 供row-locking SELECT，沒有表級UPDATE或opgroupno UPDATE。ACL差異0；另 `SET LOCAL ROLE ymes_app` 對兩表 `WHERE false` 真SELECT probe通過，不讀取業務列，也不在READ ONLY做FOR UPDATE。
- 套用當時正式API仍 `16a825af0bea`、Web仍 `722bc259cc81`，**OPB02 API／Web尚未部署、回歸及app-browser待驗**；本次只完成資料庫基礎，未建立示範Job／PS目錄。來源規則與待驗界線見 [ECN作業站群組設定](modules/OP/ECN作業站群組設定.md)。
- 後續已正式部署API `1d09e4febf48`／Web `cfa5cf288359`；API八組分兩輪完成，兩輪各15個精確scope及holder全0。root正式IAB再驗48個檢查點（含1項SQL清理讀回）：六個父列全GUI建立／刪除；cleanup前獨立讀回父與兩方向明細已0，只剩9個自有lookup，精確清理後19項計數全0。NULL／state1／字面`*`／stale等是自有SQL前置值，真GUI保存／異動另有記錄，沒有複製Oracle業務列或造全域PS `*`／Job表。
- API-only及Web-only checkpoint均由root dryvalidate／apply通過：正式metadata的migration已0054；API主來源139檔／相容來源136檔及Web私有來源206檔均保留，Web來源在 `/opt/ymes/checkpoints/web-opb02-20261120-a2`。更新metadata不重啟服務、不改DB；原migration SHA與DRAFT歷史bytes不改。GUI已驗、本批Git待推，完整IssueJob／通知等provider仍未提供，候選存在503分支未實際觸發，不以本節聲稱已驗全部原MES流程。

## OP105來源預設值與權限：0053（2026-11-19）

- PPK READ ONLY獨立比對 `tblopgroupbasis` 11欄與 `tblopgroupdetail` 9欄，順序／型別／NULL／PK差異0；只缺五個來源DEFAULT0。兩表僅各PK index、無其他constraint／user trigger；app當時Basis無CRUD、Detail只有SELECT。
- `/opt/ymes/backups/ymes_before_0053_op105_20261119.dump`（625213 bytes、600，目錄700）通過 `pg_restore --list` 後，套用0053成功。只ALTER五個default並授予兩owner表CRUD，沒有回填真NULL、重建或搬入業務資料。
- 獨立套用後20欄／五default／PK／index／trigger／CRUD權限回讀差異0；證據 `.tools/op105-root-before-audit-20261119.json`、`op105-root-after-audit-20261119.json`。API／Web仍為未部署草稿，不能把schema完成當作功能或GUI通過，見 [作業站群組資料](modules/OP/作業站群組資料.md)。

## QC 共用參數目錄與原因參照鎖：0051–0052（2026-11-19）

- PPK只讀確認所有schema缺少 `tblsysmesparameter`，原生／PG參照／0051的16欄順序、型別、NULL、DEFAULT、PK離線比對差異0；保留ParameterNo首欄、5個NOT NULL、4個DEFAULT，只建PK index。
- 備份 `/opt/ymes/backups/ymes_before_0051_0052_qc_parameters_20261119.dump`（622742 bytes、600，目錄700）通過 `pg_restore --list` 後，依序套用0051、0052成功。只補參數表及SELECT／UPDATE(parameterno)，既有原因表只補UPDATE(reasonno)供FOR SHARE，不授予全表UPDATE。
- 獨立PPK READ ONLY回讀16欄差異0；PK／PK index各1且無其他constraint/index/user trigger。app SELECT=true、表INSERT／UPDATE／DELETE=false，只有parameterno欄UPDATE=true其餘15欄false；新catalog COUNT=0。沒有插入目錄業務資料、猜測ValueList／Table／Function provider、建立FC表或改其他專案。建表不等於QC201／QC202 API或GUI驗收完成，維護界線見 [因子參數目錄相依](modules/QC/因子參數目錄相依.md)。

## OP103 原生結構：0049–0050（2026-11-19）

- PPK再次唯讀查所有schema，確認 `tbloptype`／`tblopproperty` 仍缺少。備份 `/opt/ymes/backups/ymes_before_0049_0050_op103_20261119.dump` 通過 pg_restore --list 後，按0049→0050套用；未連Oracle或複製業務資料。
- 0049類別8欄、PK(optype)；0050共用Property11欄、PK(opno,propertyno)，逐欄型別、順序、NOT NULL與DEFAULT已獨立讀回和來源一致。兩表各僅PK index、沒有其他constraint或user trigger；初始各0筆。
- `ymes_app` 對類別CRUD全true；Property僅SELECT／DELETE=true、INSERT／UPDATE=false，僅供已核實的類別實刪清理。沒有交付Property維護provider，不把建表當完整服務。
- OP103隔離API／Razor Release皆0警告／0錯誤，Web vue-tsc／Vite成功。已部署API `260dfd2ff2bb`／Web `760d6055e519`，正式十組112HTTP／234斷言通過，Type／OP／Property／自有PS與holder皆0；GUI另待。完整版本與測試界線見 [作業站類別資料](modules/OP/作業站類別資料.md)。
- 已核對並將YMES備份目錄 `/opt/ymes/backups` 設為root專用700、本次dump600，再驗證pg_restore清單可讀。後續備份先用 `umask 077`；維護時由root讀檔或以stdin pipe給postgres，不為讀取備份而放寬目錄。應用容器不需存取備份目錄。

## 作業站引用檢查讀取權限：0048（2026-11-19）

- PD206正式父生命週期回歸發現OP硬刪500；家用PPK唯讀catalog查核確認既有 `PostgresOperationRepository.HasDependents` 的八張來源表都存在且有opno，其中六表缺 `ymes_app` SELECT。這是既存相依讀權，不是缺CREATE SQL，也不因無讀權就省略引用防護。
- 主流程完成套用前備份並通過 `pg_restore --list`，再套用 [0048_op_dependency_read_permissions.sql](../database/migrations/0048_op_dependency_read_permissions.sql)，只對 `tblsysflownodebasis`、`tbleqplocatordetail`、`tblprdrecipedetail`、`tblopgroupdetail`、`tblprsopapsdetail`、`tblprdprsopapsbasedetail` 補SELECT。未改schema、業務列、FK/index，未授予這些表的應用DML。
- 獨立readback六表皆SELECT=true、INSERT/UPDATE/DELETE=false；原已在0046補SELECT的 `tblprdesopbasis`／`tblprdprsopapssetupdetail` 不重建。後續PD206完整105項HTTP回歸含真OP硬刪204、規則保留及自身Delete，全scope四表清0，見 [產品抽樣頻率規則](modules/PRD/產品抽樣頻率規則.md)。
- API小修 `269434c2aaf0` 已部署、readiness200，為MasterDataController補通用Npgsql→乾淨503及流程／物料／OP三list包裝，避免未處理DB例外裸500；Web維持 `29139f507e77`／`CKNGXUoB`。新API已做11項無fixture/SQL DML的HTTPS冒煙：四主檔清單匿名各401、登入後各200、登入200、小寫 `/ymes` 404及登出204。未在正式站撤權故障注入，也沒有把正常路徑冒煙寫成所有模組完成。

## 產品類別引用讀取權限：0046–0047（2026-11-19）

- PPK live catalog／grants 確認七張原生表存在但應用角色無 SELECT。先驗證 `/opt/ymes/backups/ymes_before_0046_pd101_grants_20261119.dump`、`ymes_before_0047_prd_module_read_20261119.dump` 可由 pg_restore --list 讀取，再分別套用兩份 forward migration。
- 0046 對 `tblprdesopbasis`、`tblprdproductlabelrulebasis`、`tblprdproductlabelruledetail`、`tblprdproductserialrulebasis`、`tblprdproductserialruledetail`、`tblprdprsopapssetupdetail` 補 SELECT，供 PD101 硬刪前辨識既有 TypeNo／版本 -1 設定。
- 0047 對 `tblprdmodulebasis` 補 SELECT，供新產品與模組編號衝突檢查。獨立讀回七表皆 SELECT=true，INSERT／UPDATE／DELETE=false；沒有改表、業務列、FK 或 index。API 不把無讀取權限當成不存在或無引用。
- 本批日期保存／顯示採 [API 稽核日期契約](API_AUDIT_TIMESTAMPS.md)，僅調整已核實模組的 API 邊界，不變更資料庫時區或回填歷史日期。

## 後續獨立 schema 基礎：0042–0045（2026-11-19）

- 家用 PPK/SSH live metadata確認 `tblprstype` 與 `tblprdtype` 缺少後，分別驗證備份 `ymes_before_0042_ps103_20261119.dump`、`ymes_before_0044_pd101_20261119.dump` 可由 pg_restore --list讀取，再套用0042與0044。備份位於 `/opt/ymes/backups/`。
- 0042 PS103建立原生八欄／單欄PK／來源default／nullable stamp；0044 PD101建立來源dictionary核對的11欄／單欄PK／來源default。兩表皆已獨立讀回欄位、PK、`ymes_app` CRUD權限，沒有加FK／額外索引或複製業務列。
- 後續先驗證 `ymes_before_0043_pd206_20261119.dump` 與 `ymes_before_0045_qc301_20261119.dump` 的 pg_restore --list，再套用0043與0045。實際獨立套用順序為0042→0044→0043→0045，0044不依賴0043，不以檔號推定部署狀態。
- 0043 `tblprdqcrulebasis` 已獨立讀回16欄、4個NOT NULL、0個DEFAULT、三欄PK且只有PK index、應用CRUD全true，初始count0。PD206 API尚未部署；完整父主檔刪除生命週期仍待整合，不以建表當成完成API。
- 0045 `tblspcvardetaillog` 依原生DDL建立27欄、5個NOT NULL、0個DEFAULT、雙欄PK與三個真實來源索引；保留POSION原拼字。已核對app權限SELECT=true、INSERT/UPDATE/DELETE=false，僅供QC301引用檢查，未交付測量writer或其他QC210表。0045後16項硬刪／真量測引用回歸及精確清理通過，見 [管制圖內容](modules/QC/管制圖內容.md)。
- PD101完整類別CRUD／共享設定Copy仍在接續，目前正式產品查找使用其已核准類別目錄。PS103實際API／瀏覽器驗收見 [流程類別資料](modules/PRS/流程類別資料.md)。

## 家用端部署 checkpoint：0034–0041（2026-11-19）

本節記錄家用端主流程已透過 PPK/plink SSH 套用到 `ymes` 資料庫／`YMES` schema 的 forward migrations。公司端 Oracle MESTEST 仍是唯讀原生來源；此次沒有在 Oracle 執行 DDL、沒有複製 MES 業務資料。以下是實際套用範圍，不將「repo 有 SQL」視為已部署，也不將 schema 建立視為對應 API 已驗收。

| Migration | 已套用物件／變更 | 權限與交付界線 |
| --- | --- | --- |
| [0034_qc103_sampling_plan.sql](../database/migrations/0034_qc103_sampling_plan.sql) | 新建 `tblqcsamplingplan`、`tblqcsamplingparameter`。 | `ymes_app` 兩表 CRUD；QC103 API／Web 已部署，已知四類管理實測見模組 MD。 |
| [0035_qc107_attribute_rule_basis.sql](../database/migrations/0035_qc107_attribute_rule_basis.sql) | 新建 `tblqcattrulebasis`、`tblqcattruledetail`。 | 兩表 CRUD；保留來源 nullable IssueState，沒有新增 DataStamp。API／Web 已部署，空 aggregate 與保留既有明細的實測不等於新增明細／核准成功。 |
| [0036_qc108_variable_rule_basis.sql](../database/migrations/0036_qc108_variable_rule_basis.sql) | 新建 `tblqcrulebasis`。 | 表級 CRUD；保留 RuleSerial PK、RuleNo 另欄及 nullable DataStamp，未增 RuleNo unique index。System 七類已實測；FUNQR 目錄未交付。 |
| [0037_qc_aql_lookup.sql](../database/migrations/0037_qc_aql_lookup.sql) | 新建共用空表 `tblqcaql`。 | 僅 SELECT；沒有填入 AQL 業務列，也沒有補成未知的 SYS ValueList／UserDefineAQL 設定。 |
| [0038_prd_product_programs.sql](../database/migrations/0038_prd_product_programs.sql) | 新建 `tblprdproductprograms`；恢復既有 `tblprdprogrambasis.issuestate DEFAULT 0`。 | 指派表 CRUD，Program master SELECT 及 `UPDATE(programno)` 供 row-locking SELECT；本批只有 PRD schema groundwork，Program 指派 API 尚未部署。 |
| [0039_prd_product_recipe.sql](../database/migrations/0039_prd_product_recipe.sql) | 新建 `tblprdrecipe`；恢復既有 Recipe basis 的 recipeversion／issuestate／curversion／datastamp 四個 DEFAULT 0。 | 指派表 CRUD，Recipe master SELECT 及 `UPDATE(recipegroup)` 供 row-locking SELECT；RecipeGroup 指派 API 尚未部署。 |
| [0040_prd_product_source_defaults.sql](../database/migrations/0040_prd_product_source_defaults.sql) | 恢復既有 `tblprdproductbasis` 已核實的 12 個來源 DEFAULT。 | 只改預設，不改既有值、NULL、型別或 PK；不代表 PRD 程式部署完成。 |
| [0041_qc209_default_rule_dependency.sql](../database/migrations/0041_qc209_default_rule_dependency.sql) | 新建七欄的共用 `tblqcitemrule`，PK `(qcitemno,spcid,ruleno)`。 | 表級 CRUD；補 `tblqcitemforvariables` 的 `UPDATE(qcitemno)` 供父列鎖。解開 QC301／QC302 預設 Rule 相依；不因此宣稱 QC209 獨立頁 API 已完成。 |

- 0034–0037 於 live audit 確認六張來源表缺少後才建立；保留核實的欄位、PK／nullable／default，沒有加入來源沒有的 FK 或額外索引。0038–0040 沿用現有 PRD master，只補缺指派表與明確 DEFAULT，不重建或改寫 master 業務列。
- 三份套用前備份均已由主流程執行 `pg_restore --list` 驗證成功：`/opt/ymes/backups/ymes_before_0034_0037_20261119.dump`、`/opt/ymes/backups/ymes_before_0038_0040_20261119.dump`、`/opt/ymes/backups/ymes_before_0041_20261119.dump`。
- QC103、QC107、QC108 的正式 API／app-browser 測試範圍分別記於 [抽樣計劃](modules/QC/抽樣計劃.md)、[計數判別法則](modules/QC/計數判別法則基本資料.md)、[計量品管法則](modules/QC/計量品管法則基本資料.md)。QC103 的分類／Core／FUNSA、QC107 ValueList／SysInfo、QC108 FUNQR 與共用 Issue/Job 缺項仍保留公司端來源交付，沒有用假資料通過。
- 本輪三個 QC 模組的專屬 fixtures 已由 UI／API 逐一刪除，並以精確本輪識別鍵 SQL 總檢：QC103 主檔／參數、QC107 主檔／明細、QC108 法則皆0，包含 QC103 三個合法 route 邊界鍵及 QC101 共用抽樣表測試鍵。這不是宣稱全表無業務資料。QC301／QC302 的另一組 fixtures 與精確清理證據見各自模組文件。

### 更新 2026-11-18：PRS PS103 流程類別資料

- 使用本機受保護 Oracle 設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查 `MESTEST.TBLPRSTYPE` 的 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`。只有 `PROCESSTYPE` PK unique index，沒有 FK/其他 index。
- Oracle source DDL：`database/reference/mestest_prs103_process_type_source_ddl.sql`；YMES PostgreSQL CREATE-only 對照：`database/reference/mestest_prs103_process_type_create.sql`。只查結構 metadata，未讀業務列、未在 Oracle 執行 DDL/DML、未輸出/提交連線秘密。
- `ISSUESTATE NUMBER(1,0) DEFAULT 0 NOT NULL`；`DATASTAMP NUMBER(11,0) DEFAULT 0` nullable。PS103 是 `TBLPRSTYPE` owner，供 PS104 流程類別 lookup 重用；若 YMES 已有表不重建，先比較 live defaults。
- ProcessType 新增另有程式檢查：不可與 `TBLPRSProcessBasis.PROCESSNO` 大小寫不敏感重複。刪除 0/-1 要在同一交易清 `TBLPRSProcessProperty` 的同 ProcessNo 資料，再刪主檔；Oracle 無 FK。
- Property 由 `PS_ProcessProperty` 共用模組管理，傳入 ProcessNo=ProcessType、ProcessVersion 未指定；核准 IssueNo=`PRS PROPERTY`。Oracle 連線秘密仍只保留本機受保護設定，不寫入 Git 文件或腳本。

### 更新 2026-11-18：PRS PS104 流程基本資料九表

- 使用本機受保護既有 Oracle 設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接連 MESTEST 唯讀取得 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`：`TBLPRSPROCESSBASIS`、`TBLPRSNODEBASIS`、`TBLPRSNODERELATION`、`TBLPRSNODEXML_CLOB`、`TBLPRSPROCESSPROPERTY`、`TBLPRSOPAPSBASE`、`TBLPRSOPAPSDETAIL`、`TBLPRSPRODUCTIONSEGMENT`、`TBLPRSTYPE`。
- Oracle 原始 DDL 在 `database/reference/mestest_prs104_process_basis_source_ddl.sql`；PostgreSQL CREATE-only 對照在 `database/reference/mestest_prs104_process_basis_create.sql`。每表僅 PK unique index，無 FK 或其他 index。
- 只查資料字典與 DDL，未讀 MES 業務列、未在 Oracle 執行 DDL/DML、未輸出/提交連線秘密。Oracle 來源連線值仍只保留在本機受保護設定，不寫進 Git 文件或腳本。
- 第一階段已包含 ProcessBasis、NodeBasis、OPAPSDetail 三表；CREATE IF NOT EXISTS 不會更動已存在表。家用端需先比對 live defaults，再對實際缺 default 的欄位做 forward migration；新增 PS104 專屬 NodeRelation、NodeXML CLOB、ProcessProperty、OPAPSBase 四表；ProductionSegment 由 PS101 建立，ProcessType 由 PS103 建立，PS104 重用。
- NodeBasis SAP/客製欄位為 `VARCHAR2(4 CHAR/10 CHAR/3000 CHAR/10 CHAR)`；資料字典的 DATA_LENGTH 以 bytes 呈現（例如 4 CHAR 對應 16 bytes），不能當字元欄寬並據此擴欄。
- 舊流程圖序列化依賴外部 `iMESXMLFlow.dll`；Excel checker 依賴外部 DataImporter DLL。相容性/部署方式見 `docs/modules/PRS/流程基本資料.md`。

### 更新 2026-11-18：QC408 OCAP 流程三表

- 使用本機受保護的既有連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 `MESTEST.TBLQCOCAPBASIS`、`TBLQCOCAPNODEBASIS`、`TBLQCOCAPNODERELATION` 的 `DBMS_METADATA.GET_DDL` 與 PK/index/FK metadata。未讀 MES 業務列，未在 Oracle 執行 DDL，沒有輸出/提交連線秘密。
- Oracle DDL 保存於 `database/reference/mestest_qc408_ocap_source_ddl.sql`；YMES PostgreSQL CREATE-only 對照為 `database/reference/mestest_qc408_ocap_create.sql`。三表只有 PK unique index，沒有 FK/額外索引。Relation 的 `OCAPLINKNAME` 由複合 PK 實際要求 NOT NULL；OCAP Basis 的 DataStamp nullable 且 DEFAULT 0。
- `OCAPXMLSTRING` 是舊 AddFlow serializer CLOB；Flow Form 呼叫 `iMESXMLFlow.dll` 的 `FlowToXML/XMLToFlow`，來源 repository 未含該 serializer 原始碼。家用端需在 API 儲存前確認相容格式，不能直接把 Vue 結構化 payload 當成舊 XML。
- WinForms/UI、刪除/簽核、三表交易及 API handoff 見 `docs/modules/QC/OCAP基本資料.md`。

### 更新 2026-11-18：QC501–QC503 檢測儀器共用資料表

- 使用已保存的本機受保護連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 MESTEST 六張表的 CREATE TABLE/PK unique index DDL：`TBLQCINSPMACHINEPARAMETERTYPE`、`TBLQCINSPMACHINETYPE`、`TBLQCINSPMACHINEBASIS`、`TBLQCINSPMACHINEPARAMETER`、`TBLQCINSPMACHINEPROPERTY`、`TBLQCINSPDRIVER`。
- 原始 Oracle DDL：`database/reference/mestest_qc501_inspection_machine_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc501_inspection_machine_create.sql`。來源只有 PK unique index，未查到 FK 或額外獨立索引。CREATE 是 YMES 對照參考，不是 migration；家用端必須查 live schema 後只補缺表。
- 只讀資料字典/DDL，未讀業務資料列，未在 Oracle 執行 DDL；連線秘密未輸出/提交。PS7 載入此舊版 Oracle provider 時遇 BinaryFormatter 平台例外；改用 Windows PowerShell 5.1 後查詢成功。
- QC501 表主鍵實際是 InspMachineType 單欄；來源 `IssueState`/`DataStamp` 可 NULL 且有 DEFAULT，CREATE reference 保留 nullable；`ChNo` 欄位長度為 3，但 WinForms TextBox MaxLength=10，API 需回報超長值錯誤，不得靜默截斷。
- QC502「檢測儀器基本資料」沿用同一批 Oracle source DDL 與 PostgreSQL CREATE 對照；其主要寫入表 `TBLQCINSPMACHINEPARAMETER` 在 QC501 六表清單中已直接查得。本次未重查 Oracle，沒有讀業務列或執行 Oracle DDL。`TBLQCINSPMACHINEBASIS` 不在 QC502 Form/Core 的 Add/Edit/Delete 呼叫內，不要當成 QC502 主表。
- 家用端 2026-11-18 live schema audit 確認六張 `tblqcinsp*` 表皆缺少；先以遠端 `postgres` 維護角色備份 YMES，`/opt/ymes/backups/ymes_before_0027_qc501_20261118.dump` 通過 `pg_restore --list`，再套用 `database/migrations/0027_qc501_qc502_inspection_machines.sql`。六張表已建立並授予 `ymes_app` 表級 CRUD；API 角色本身無 schema CREATE 權限。此 migration 沒有建立額外 FK/index、沒有複製來源業務資料。
- QC503「檢測設備連線程式」主表 `TBLQCINSPDRIVER` 沿用上述已取得的 Oracle source DDL 與 PostgreSQL CREATE 參考；本功能不需另查 Oracle 或新增 CREATE SQL。此回合依 WinForms/wsQC/Core/Driver Function 原始碼核對行為；沒有讀取 MES 業務資料列、連 YMES PostgreSQL、執行 Oracle DDL 或輸出連線秘密。

## 連線用途與邊界

| 資料來源 | 用途 | 應用程式連線方式 |
| --- | --- | --- |
| MES Oracle，來源 schema MESTEST | 由使用者授權後查詢資料字典、原始表結構及必要的來源邏輯；例如 ALL_TABLES、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、DBMS_METADATA.GET_DDL | Vue Web 與 YMES API 不連 MES Oracle。公司電腦的來源查詢使用者提供的既有唯讀連線資訊 |
| YMES PostgreSQL，schema YMES | YMES API 的正式資料庫 | 由 .NET API 透過 Npgsql 連線；資料庫/migration/API 由家用電腦維護 |
| YMES Web | 顯示畫面並呼叫 API | 不保存、不讀取 Oracle 或 PostgreSQL 連線字串 |

MES Oracle MESTEST 是原生 MES 來源；YMES PostgreSQL 是家用端建置的復刻資料庫。兩者是不同資料庫、不同用途：公司端唯讀查 Oracle 的資料字典、原始 DDL 與舊系統規則；家用端透過 PPK SSH 管理 YMES PostgreSQL，將核實過的原生 MES schema 轉成 PostgreSQL 結構並重建 API 行為。`database/reference/*_create.sql` 是這種 PostgreSQL 建表交接，不得在原生 Oracle 執行。這些 CREATE SQL 只包含結構，不會複製 MES 業務資料；任何業務資料匯入都需另外規劃與授權。

## 已確認的 YMES API 設定名稱

.NET API 從設定提供者取得連線字串，支援以下名稱：

- ConnectionStrings__YMes（對應 .NET ConnectionStrings:YMes）
- YMES_DATABASE_URL

實際值只放在部署 Secret Store、受保護的本機環境變數或未追蹤的本機設定檔；不得寫進 Git 追蹤的 appsettings、Markdown、SQL 註解或終端輸出。

## MES Oracle 來源連線

- 使用者先前提供的 Oracle 設定已從本機受保護附件安全讀取，無須再次索取。公司電腦以 Windows PowerShell 5.1 和 Oracle.ManagedDataAccess 直接連線查詢；不可改用 DBeaver 螢幕操作作為主要 DDL 擷取方式。
- 實際 Oracle 主機、帳號及密碼不放在這份受 Git 追蹤的文件中。這是為遵守 docs/DEVELOPMENT_STYLE.md 的憑證規則，並避免 push 到 GitHub。
- Windows PowerShell 5.1 已成功連到 MESTEST；Oracle.ManagedDataAccess 驅動位於舊 MES 工作目錄。查詢只讀取資料字典、DDL 與選單中繼資料，不匯出 MES 業務資料。
- 來源查詢只用於確認結構/規則及產生 CREATE SQL；不得在 MES Oracle 執行 DDL 或寫入業務資料。
- 連線字串只由本機受保護設定提供，不複製到工作目錄、終端輸出或 Git。家用電腦如需直接查來源，先檢查其受保護設定；不得從 Git 文件還原密碼。

### 本次公司電腦盤點狀態（2026-11-13）

- `D:\iMES\CSiMES\.equipment-settings-ddl.tmp.sql` 內含 19 張表的 Oracle DDL，但不含 `TBLEQPEQUIPMENTBASIS` 與 `TBLEQPPARAMETERBASIS`。
- 使用者於 2026-11-13 提供上述兩表的原始 Oracle CREATE TABLE 與主鍵唯一索引 DDL；公司端已轉成 `database/reference/mestest_equipment_basis_create.sql` 及 `database/reference/mestest_equipment_parameter_basis_create.sql`，並校正 bootstrap。這兩張表不再列為待取得 DDL。
- PowerShell 5.1 已以唯讀方式成功查詢 MESTEST 的 1,287 張表；`DBMS_METADATA.GET_DDL` 擷取及轉換成果依模組放在 `database/reference/`。本次已保存 8 張物料表原始 DDL/PG CREATE，以及 18 張作業站相關表和 3 張系統選單表原始 DDL/PG CREATE。
- Oracle `TBLSYSMODULE`、`TBLSYSMENUITEM`、`TBLSYSFUNCTION` 只查系統目錄中繼資料：20 個 Module、601 筆 MenuItem、553 筆 Function；盤點摘要與完整功能列見 `docs/MES_MENU_FUNCTION_INVENTORY.md`。第一層標題已從 `Customer/Production/MESClient/mesresource.zh-cht.resources` 依 `{ModuleNo}Module` 翻譯鍵核實；`TBLSYSMODULE` 沒有 ModuleName 欄位。
- 對應目錄資料的冪等 upsert SQL 為 `database/reference/mestest_navigation_catalog_seed.sql`；僅含 Module/Menu/Function 設定中繼資料，不刪除既有目錄列、不含 MES 業務資料。家用電腦確認 YMES schema 與現有自訂路由後，再整合為 migration。
- 作業站 `TBLOPBASIS`、`TBLOPGROUPDETAIL` 及服務明確使用的 16 張作業站子表原始 Oracle DDL 已擷取並附於 `database/reference/mestest_operation_and_system_source_ddl.sql`；對應 PG 空表 SQL 為 `database/reference/mestest_operation_and_system_create.sql`。
- 已再查這 21 張作業站/系統表的 Oracle index/constraint 字典：每表只有一個 PK unique index，沒有額外獨立索引；29 張作業站、系統、物料表各有一個 PK，沒有 FK (`R`) constraint。NOT NULL constraints 保留在來源 DDL與 PG CREATE SQL。
- 物料主檔 8 張來源表的原始 Oracle DDL 與 PG 空表 SQL 分別為 `database/reference/mestest_mtl_master_source_ddl.sql`、`database/reference/mestest_mtl_master_create.sql`。兩份 CREATE SQL 均只含結構，不含 MES 業務資料；套用前由家用電腦比對實際 YMES schema。



### 更新 2026-11-16：設備類別配件目錄

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得 MESTEST.TBLEQPACCESSORYTYPE 的 DBMS_METADATA DDL、9 欄 nullable/default、PK、index 與 FK 字典。
- 舊 frmEQPTypeACCType 呼叫 LoadAccessoryType(IssueState=2)；clsAccBasis.ShowAccessoryType 的查詢還排除 AccessoryType IS NULL，沒有指定 AccessoryCategory。
- PK 為 ACCESSORYTYPE，唯一索引僅 PK index，無額外索引/FK。Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql、mestest_equipment_type_accessory_catalog_create.sql。家用端仍需先核對 YMES schema；本次只讀結構，未讀業務資料。

### 更新 2026-11-16：設備群組 SMD 區域映射

- 以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀取得 `MESTEST.TBLSMDAREARELATION`、`MESTEST.TBLSMDSMTAREAEQPMAP` 的 DBMS_METADATA DDL，並核對欄位、NULL、PK、索引及 FK。
- `TBLSMDAREARELATION` 有 PK `(BASEAREANO, OBJECTNO)` 及非唯一索引 `(CONTAINAREANO, OBJECTTYPE)`；`TBLSMDSMTAREAEQPMAP` 的 PK 為 `(BASEAREANO, AREANO, EQUIPMENTNO)`。兩表沒有 FK。
- Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_equipment_group_smd_source_ddl.sql`、`database/reference/mestest_equipment_group_smd_create.sql`。家用端仍須比對實際 YMES schema；本次只讀結構，未讀取業務資料。

### 更新 2026-11-16：SMD SD104 區域基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線直接唯讀查詢 MESTEST.TBLSMDAREABASIS、TBLSMDAREAINVENTORY、TBLSMDAREAMAP 的 DBMS_METADATA DDL，並核對欄位 NULL/default、PK、索引與 FK 字典。
- 三張表只有 PK unique index，沒有外鍵或其他索引。`TBLSMDAREABASIS.BACKGROUNDIMAGE` 是 BLOB；`TBLSMDAREABASIS.DATASTAMP`、`TBLSMDAREAINVENTORY.DATASTAMP` DEFAULT 0 但 nullable。
- 原始 logical Oracle DDL 與 PostgreSQL 空表 CREATE SQL 已保存於 `database/reference/mestest_smd_area_basis_source_ddl.sql`、`database/reference/mestest_smd_area_basis_create.sql`。`TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 的 DDL/CREATE 沿用設備群組來源檔。只查資料字典，沒有讀 MES 業務資料，也沒有在 Oracle 執行 DDL。
- 主表 Form/Service 與配置交易/API 待辦見 `docs/modules/SMD/區域基本資料.md`。SD105「區域庫房基本設定」沿用此處已查的 `TBLSMDAREAINVENTORY` DDL/CREATE SQL，沒有重複查詢或新增 table SQL；規格見 `docs/modules/SMD/區域庫房基本設定.md`。連線值僅使用本機受保護設定，未輸出到文件或 Git。

### 更新 2026-11-16：SMD SD106 過帳電腦之區域

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀取得 `MESTEST.TBLSMDCOMPUTERAREABASIS`、`TBLSMDCOMPUTERAREADETAIL` DBMS_METADATA DDL，並查欄位 NULL/default、PK、索引與 FK 字典。
- 主檔 PK `(ComputerNo,FactoryNo)`；明細 PK `(ComputerNo,FactoryNo,AreaNo)`；兩表僅有 PK unique index，沒有 FK 或額外索引。DataStamp 是 NUMBER(11,0) DEFAULT 0 nullable。
- Oracle logical source DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_smd_computer_area_source_ddl.sql`、`mestest_smd_computer_area_create.sql`。只讀資料字典，沒有讀取 MES 業務資料或在 Oracle 執行 DDL；連線值沒有輸出或存檔。
- WinForms、Service/Core、Vue 畫面與 API handoff 規格見 `docs/modules/SMD/過帳電腦之區域.md`。家用端先比對 YMES 現有 schema/migrations，再補缺項；不要把 DEV 假資料寫進正式 DB。

### 更新 2026-11-16：SMD SD101 工廠基本資料

- `MESTEST.TBLSMDFACTORYBASIS` 的 Oracle DDL、欄位 NULL/default、PK/index/FK 已由 PowerShell 5.1 + Oracle.ManagedDataAccess 於 EP303 來源查詢中直接取得；SD101 沿用該次查證，不需重查。
- Oracle logical source DDL、PostgreSQL 空表 CREATE SQL 已在 `database/reference/mestest_carrier_basis_dependencies_source_ddl.sql`、`mestest_carrier_basis_dependencies_create.sql`；first-phase bootstrap 也已包含該表。家用端比對 YMES schema 後，缺少才新增 migration。
- WinForms 狀態、核准/刪除、欄位、DataStamp 與 Vue/API handoff 見 `docs/modules/SMD/工廠基本資料.md`。只使用既有受保護 Oracle 設定查結構，連線值未輸出或寫入 Git。
### 更新 2026-11-16：設備工程群組成員表

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查得 `MESTEST.TBLUSRUSERGROUP` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES` 與 `ALL_IND_COLUMNS`。
- 結構為 6 欄；PK `(USERNO, GROUPNO, GROUPTYPE, AGENTOF)`；`GROUPTYPE NUMBER(1,0) DEFAULT 0`；`AGENTOF` 雖沒有 inline NOT NULL 宣告，仍由 PK 強制 NOT NULL。唯一索引只有 PK index，沒有額外 index/FK。
- 原始 Oracle DDL 與 PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_equipment_engineer_group_member_source_ddl.sql`、`database/reference/mestest_equipment_engineer_group_member_create.sql`。目標 YMES schema 尚待家用電腦核對；此查詢只讀結構，未讀取業務資料。

## PostgreSQL / API 操作

- 公司電腦只做 Vue UI、選單、表單及 API 呼叫；不要在 Web 專案加入資料庫驅動或連線字串。
- 家用電腦負責 YMES PostgreSQL migration、API repository、DTO 及部署；先讀本文件與 todolist_homepc.md。
- 家用電腦可經 SSH 使用資料庫管理權限維護 YMES PostgreSQL。API 的 `ymes_app` 是執行帳號，權限不足以執行 DDL 時，應由家用端使用 SSH 管理入口操作；不可因此把資料庫工作交給公司端。
- DATABASE_URL、ConnectionStrings__YMes、SSH 私鑰、Oracle 帳密及其他憑證不得 commit、push、寫入 Markdown 或貼在聊天輸出。
- DDL handoff 只包含 schema 結構；套用到 YMES 前由家用電腦檢查 migration 順序、現有表、權限與備份。

## 相關設定來源

- 開發分工與禁止提交憑證規則：[docs/DEVELOPMENT_STYLE.md](DEVELOPMENT_STYLE.md)
- API 啟動連線設定：src/YMes.Api/Program.cs
- 部署環境範本：deploy/compose.yaml（檢視設定變數名稱時不得輸出 secret 值）
- Oracle CREATE SQL handoff：database/reference/

### 更新 2026-11-16：EP202 配件類別相關表

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢 MESTEST 的 TBLEQPACCESSORYTYPE、TBLEQPACCESSORYCATEGORY、TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYLIFE DDL、欄位 NULL/default、PK、索引與 FK 字典。
- Oracle 原始 DDL/PG 空表 CREATE SQL：database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql、mestest_equipment_type_accessory_catalog_create.sql、mestest_accessory_type_dependencies_source_ddl.sql、mestest_accessory_type_dependencies_create.sql。
- 四表均無額外索引及 FK；只有 PK 唯一索引。此批查詢只讀結構，未讀 MES 業務資料。家用端執行前需比對實際 YMES schema，缺表才經 migration 建立。

### 更新 2026-11-16：EP201 配件分類與 EP204 可用狀態

- 公司端用 PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀呼叫 DBMS_METADATA.GET_DDL，核實 MESTEST.TBLEQPACCCATEGORYSTATEBASIS、TBLEQPACCSTATEBASIS；另核對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- EP204 原始 DDL與 PostgreSQL 空表 CREATE SQL：mestest_accessory_category_state_source_ddl.sql、mestest_accessory_category_state_create.sql；lookup DDL/CREATE：mestest_accessory_state_source_ddl.sql、mestest_accessory_state_create.sql。
- TBLEQPACCCATEGORYSTATEBASIS PK 為 (ACCESSORYCATEGORY, ACCESSORYSTATE, OPERATIONNAME)，只有 PK 唯一索引，沒有 FK/額外索引。OPERATIONNAME 因 PK 而 NOT NULL；DATASTAMP DEFAULT 0 但 nullable。
- TBLEQPACCSTATEBASIS PK 為 ACCESSORYSTATE，STATETYPE DEFAULT 1 NOT NULL，DATASTAMP DEFAULT 0 但 nullable；僅 PK 唯一索引，沒有額外 FK/index。
- 已修正 mestest_first_phase_create_tables.sql 中 lookup 表曾以推測註解呈現的欄位/default，並有單表 CREATE SQL 供家用端比對。套用前仍須檢查 YMES 現有 schema；缺表才 migration。本次只查結構，未讀取或匯出 MES 業務資料。
- EP201 主表 TBLEQPACCESSORYCATEGORY 的 DDL/CREATE SQL 已在 EP202 共用依賴檔案中；家用端規格見 todolist_homepc.md 的 EP201/EP204 交接段落。

### 更新 2026-11-16：EP203 配件基本資料 Oracle DDL

- 公司端使用 Windows PowerShell 5.1 與本機已安裝的 Oracle.ManagedDataAccess.dll，從本機受保護設定讀取測試連線，唯讀呼叫 DBMS_METADATA.GET_DDL；以 ALL_INDEXES 取得 6 張表的 8 個 index 名稱，再逐個取 DDL。
- 本次涉及 TBLEQPACCESSORYBASIS、TBLEMSACCESSORYSTATE、TBLEMSACCESSORYSTATELOG、TBLEQPACCSTATECONTROL、TBLEMSEQPACCSTATE、TBLEMSEQPACCSTATELOG；原始 DDL 及 PG 空表 SQL 已交付 database/reference/。
- 僅查結構，沒有查詢 MES 業務資料，也沒有在 Oracle 執行 DDL。連線字串、IP、帳號、密碼及 SID 僅留在本機受保護設定，不寫入 Markdown/Git。

### 更新 2026-11-16：EP301 載具分類資料

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接查詢 MESTEST.TBLEQPCARRIERCATEGORY 的 DBMS_METADATA.GET_DDL，並查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得 8 欄：CarrierCategory VARCHAR2(50) NOT NULL；Description VARCHAR2(255)；Creator/Reviser VARCHAR2(10)；CreateDate/ReviseDate DATE；IssueState NUMBER(1,0) DEFAULT 0 NOT NULL；DataStamp NUMBER(11,0) DEFAULT 0 nullable。PK 為 CarrierCategory；唯一索引只有 PK index，沒有其他索引或 FK。
- Oracle 原始 DDL/PK index 與 PostgreSQL 空表 CREATE SQL 分別為 `database/reference/mestest_carrier_category_source_ddl.sql`、`database/reference/mestest_carrier_category_create.sql`。只查結構，未讀取業務資料或執行 Oracle DDL；家用端套用前先核對 YMES schema。
- PowerShell 執行時應明確呼叫 Windows PowerShell 5.1；目前互動終端預設 PowerShell 7，直接載入既有 Oracle.ManagedDataAccess.dll 會因 BinaryFormatter runtime 相容性而無法初始化。連線設定仍由本機受保護附件讀取，不輸出或記錄連線值。

### 更新 2026-11-16：EP302 載具類別資料

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接唯讀查詢 MESTEST.TBLEQPCARRIERTYPE 的 DBMS_METADATA.GET_DDL，並核對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得 13 欄；PK 為 CARRIERTYPE，唯一索引只有 PK index，沒有其他索引或 FK。Oracle 原始 DDL/PG 空表 CREATE SQL 為 `database/reference/mestest_carrier_type_source_ddl.sql`、`database/reference/mestest_carrier_type_create.sql`。
- 只查結構，未讀取 MES 業務資料或執行 Oracle DDL；連線資訊仍只由本機受保護設定讀取，不輸出、不保存到 Git。

### 更新 2026-11-16：EP303 載具基本資料與選項依賴

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定唯讀查詢 MESTEST.TBLEQPCARRIERBASIS、TBLWIPCarrierState、TBLSMDFactoryBasis 的 DBMS_METADATA DDL 及欄位、NOT NULL/default、PK、index 字典。
- 載具主表 DataStamp 為 nullable 且 DEFAULT 0；IssueState DEFAULT 0 NOT NULL。WIPCarrierState 主鍵為 CarrierNo；FactoryBasis IssueState DEFAULT 0 NOT NULL，DataStamp DEFAULT 0 nullable。三表均只有 PK unique index，沒有 FK。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 已保存於 database/reference/mestest_carrier_basis_source_ddl.sql、mestest_carrier_basis_create.sql、mestest_carrier_basis_dependencies_source_ddl.sql、mestest_carrier_basis_dependencies_create.sql。同步修正 first-phase CREATE SQL 對這些欄位的預設值，移除未經來源確認的欄位推測註解。
- 本次僅查結構及系統設定，不讀取 MES 業務資料，不在 Oracle 執行 DDL；IP、帳號、密碼與 SID 仍只留在本機受保護設定。

### 更新 2026-11-16：EP401 設備參數資料

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀查得 `MESTEST.TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL` 的 DBMS_METADATA DDL、欄位、NOT NULL/default、PK、索引與 FK 字典；另查 TBLSYSVALUELIST 的 Type 系統選項。沒有查 MES 業務資料或在 Oracle 執行 DDL。
- 兩張表各有一個 PK unique index 與一個額外 non-unique index，沒有 Oracle FK。Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_equipment_recipe_source_ddl.sql`、`mestest_equipment_recipe_create.sql`；first-phase bootstrap 已依正式 DDL 校正。
- 查詢時明確啟動 `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`；Codex 終端預設 PowerShell 7/.NET 10 不相容於本機舊版 ODP.NET 使用的 BinaryFormatter。連線值仍由受保護附件提供，不在終端、Markdown 或 Git 記錄。

### 更新 2026-11-16：ENT 客戶基本資料

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有受保護連線唯讀查得 `TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR` 的 DBMS_METADATA DDL、欄位 NULL/default、PK、索引與 FK 字典。
- 三表僅 PK unique index，沒有外鍵或其他索引。舊 CustomerBasis 刪除程序仍會明確刪除兩個子表；這是 service 行為，不是 Oracle FK cascade。
- Oracle 原始邏輯 DDL 與 PostgreSQL 空表 CREATE SQL 分別存於 `database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。本次只查結構，沒有查詢或匯出客戶業務資料，也沒有在 Oracle 執行 CREATE SQL。
- ET102 客戶連絡人資料直接沿用同一批已查 DDL 中的 `TBLENTCUSTOMERCONTACTOR`；不需要再次查詢或另建 Create SQL。其欄位、畫面限制差異與巢狀 API 契約記錄在 `docs/modules/ENT/客戶連絡人資料.md`。

### 更新 2026-11-16：ENT ET106 客戶出貨地址

- 出貨地址表 `TBLENTCUSTOMERSHIPADDR` 已在 ET101 查詢；報關表 `TBLENTCUSTOMERSHIPDECLARATION` 本次由 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護連線直接唯讀查詢 DBMS_METADATA/ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES。欄位 DDL 可得，資料字典沒有 PK、unique index、一般 index 或 FK。
- PostgreSQL 空表 CREATE SQL 已追加到 `database/reference/mestest_customer_basis_create.sql`，不加來源未定義的 PK/索引/FK。沒有讀業務列、沒有在來源 Oracle 執行 CREATE。
- DDL 查詢程式仍用 `%TEMP%\ymes-ep302-readonly-ddl.ps1` 中的本機受保護連線資料；本次只替換 helper 的表名為 `TBLENTCUSTOMERSHIPDECLARATION`。憑證未輸出或寫入文件/Git。
- 詳細的雙 Grid、表單限制、無主鍵自然鍵/API 規格見 `docs/modules/ENT/客戶出貨地址資料.md`。

### 更新 2026-11-16：ENT ET103 報關行基本資料

- 使用 Windows PowerShell 5.1 啟動 Oracle.ManagedDataAccess，從先前提供的受保護連線設定直接唯讀查詢 `MESTEST.TBLENTBROKERBASIS` DBMS_METADATA DDL 及 ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES/ALL_IND_COLUMNS；沒有讀業務資料或在 Oracle 執行 DDL。
- 查得 BrokerNo 單欄 PK/唯一索引；無 FK/其他索引。Oracle DDL 的 IssueState、CreateDate、DataStamp DEFAULT 與 NULL 性質已逐欄記入 ET103 規格及來源 DDL。既有第一階段 PostgreSQL reference 的該表預設值也已修正。
- PostgreSQL 空表 CREATE SQL 位於 `database/reference/mestest_customer_basis_create.sql`。連線憑證只使用本機受保護設定，未輸出、未寫入文件或 Git。
- WinForms 查詢、狀態、刪除/停用與 IssueNo=`BROKER BASIS` 簽核邏輯見 `docs/modules/ENT/報關行基本資料.md`。

### 更新 2026-11-16：ENT ET104 貨運行基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線直接唯讀查詢 `MESTEST.TBLENTFORWARDERBASIS` 的 DBMS_METADATA DDL、欄位 NULL/default、constraint、index 與 FK 字典；沒有查業務資料或在 Oracle 執行 DDL。
- 表有 ForwarderNo 單欄 PK/唯一索引，無 FK/其他索引；PostgreSQL 空表 CREATE SQL 已加到 `database/reference/mestest_customer_basis_create.sql`，first-phase reference 也同步補 DEFAULT 與欄位註解。
- DDL 直連方式、欄位與 ET104 邏輯見 `docs/modules/ENT/貨運行基本資料.md`。連線資訊仍僅由本機受保護設定提供，未輸出或寫入 Git。

### 更新 2026-11-16：ENT ET105 取貨公司基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀取得 `MESTEST.TBLENTTAKERBASIS` DBMS_METADATA DDL、欄位 NULL/default、constraints 與 indexes；只查資料字典，未讀取業務列，未在 Oracle 執行 DDL。
- 表只有 TakerNo PK unique index，無 FK/其他 index。Oracle source DDL/PG 空表 CREATE SQL 已追加至 customer reference；first-phase table default 與欄位註解已按查得 DDL 校正。
- 完整 DDL、WinForms 欄位/狀態/簽核規則見 `docs/modules/ENT/取貨公司基本資料.md`；Oracle 連線設定仍只留本機受保護設定。

### 更新 2026-11-16：ENT ET201 外包商主檔與連絡人

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過既有本機受保護連線唯讀查詢 `MESTEST.TBLENTSUBCONTRACTOR`、`TBLENTSUBCONTRACTORCONT` 的 DBMS_METADATA DDL、欄位 NULL/default、PK、索引與 FK 字典；沒有查業務資料或在 Oracle 執行 DDL。
- 主檔只有 SubcontractorNo PK unique index；連絡人只有 `(SubcontractorNo, ContactorName)` 複合 PK unique index。Oracle 資料字典兩表均無 FK、無額外索引；舊 Core 仍在刪除主檔時明確刪除連絡人。
- Oracle logical DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_subcontractor_source_ddl.sql`、`database/reference/mestest_subcontractor_create.sql`。包含 IssueState NOT NULL DEFAULT 0、兩表 DataStamp nullable DEFAULT 0。
- WinForms 雙 Grid、表單欄位、狀態刪除/簽核與 API 規格見 `docs/modules/ENT/外包商基本資料.md`。連線憑證只由本機受保護設定提供，未輸出或提交。

### 更新 2026-11-16：ENT ET202 外包項目設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查詢 `MESTEST.TBLENTOSITEM` 的 DBMS_METADATA DDL、欄位 NULL/default、constraints、indexes 與 index columns；未查業務資料，未在 Oracle 執行 DDL。
- 查得 11 欄、OSITEMNO 單欄 PK；DataStamp nullable DEFAULT 0。Oracle 字典僅有 PK unique index，無 FK、無額外索引。source logical DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_entositem_source_ddl.sql`、`mestest_entositem_create.sql`。
- 查詢 helper 從 `%TEMP%\ymes-ep302-readonly-ddl.ps1` 讀取受保護連線值，僅將表名改為 TBLENTOSITEM；連線資訊未輸出到文件或提交。本次只查結構，不讀取 MES 業務列，也沒有在來源資料庫執行 DDL。
- WinForms、Service/Core、流程查找、版本差異及 API 契約見 `docs/modules/ENT/外包項目設定.md`。

### 更新 2026-11-16：SMD SD103 儲位基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直接唯讀查 MESTEST.TBLINVLOCATORBASIS 的 DBMS_METADATA.GET_DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- 查得複合 PK (INVENTORYNO, LOCATORNO)，僅 PK unique index；沒有 Oracle FK 或其他索引。IssueState DEFAULT 0 NOT NULL、Capacity NUMBER(12,4) DEFAULT 0 nullable、DataStamp NUMBER(11,0) DEFAULT 0 nullable。
- Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_locator_basis_source_ddl.sql、mestest_smd_locator_basis_create.sql。first-phase bootstrap 已同步校正預設值，移除尚未有 Oracle column comment 證據的猜測註解。
- 只查資料字典，未讀業務資料、未在 Oracle 執行 DDL；IP、帳號、密碼、SID 僅由受保護設定讀取，未輸出或保存到 Git。庫存挑選沿用既有 TBLINVInventoryBasis，不涉及新增來源 DDL。
### 更新 2026-11-16：SMD SD107 生產線別基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查 MESTEST.TBLSMDPDLINEBASIS 的 DBMS_METADATA DDL、欄位、constraint、PK/index 與 index columns。
- 核實 PDLineStatus NUMBER(1,0) DEFAULT 1 NOT NULL；IssueState NUMBER(1,0) NOT NULL 無 DEFAULT；CreateDate DATE DEFAULT SYSDATE 可 NULL；DataStamp NUMBER(11,0) DEFAULT 0 可 NULL。PDLineNo 是唯一 PK index；無 FK 或其他索引。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_pdline_basis_source_ddl.sql、mestest_smd_pdline_basis_create.sql。first-phase bootstrap 已校正來源 defaults 並移除無來源依據的欄位猜測註解；家用端仍須比對實際 schema/migration，缺漏才新增 forward migration。
- 只查結構，未讀業務資料或在 Oracle 執行 DDL；受保護連線資料未輸出或保存至 Git。

### 2026-11-16：SMD SD108、SD109

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 及本機受保護連線，直接唯讀查詢 MESTEST.TBLSMDAREASHIFT、TBLSMDAREAOPERATOR、TBLSMDPDLINEPOSITIONBASIS 的 DBMS_METADATA 與資料字典。
- 新增 Oracle 邏輯來源 DDL 與 PostgreSQL 空表 CREATE SQL：mestest_smd_area_shift_source_ddl.sql / mestest_smd_area_shift_create.sql；mestest_smd_pdline_position_basis_source_ddl.sql / mestest_smd_pdline_position_basis_create.sql。
- 僅查表結構、欄位 null/default、PK/index/FK；未讀業務資料、未在 Oracle 執行 DDL。連線值未輸出或寫入 Git。

### 更新 2026-11-17：MTL ML101 物料供應商

- 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取本機受保護連線，唯讀呼叫 DBMS_METADATA.GET_DDL 查 MESTEST.TBLMTLMATERIALVENDOR、TBLMTLMATERIALVENDORCONTACTOR。
- 再查 ALL_INDEXES 與 ALL_CONSTRAINTS：主檔 PK VendorNo；連絡人 PK (VendorNo, ContactorName)；兩表都只有 PK unique index，沒有 FK 或額外索引。
- Oracle 邏輯 DDL與 PostgreSQL 空表 CREATE SQL：database/reference/mestest_mtl_material_vendor_source_ddl.sql、database/reference/mestest_mtl_material_vendor_create.sql。僅查結構，未讀業務資料、未在 Oracle 執行 DDL，連線值未輸出或提交。
### 更新 2026-11-17：MTL ML106 物料品管項目設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護設定唯讀查詢 MESTEST.TBLQCITEMFORATTRIBUTES、TBLQCITEMFORVARIABLES、TBLQCITEMGROUPBASIS、TBLQCITEMBINDLOG 的 DBMS_METADATA DDL、欄位、PK 與 index；未查詢業務資料，未在 Oracle 執行 DDL。
- 原始 Oracle DDL 與 PostgreSQL 空表 CREATE SQL 分別為 database/reference/mestest_mtl_qc_item_tables_source_ddl.sql、database/reference/mestest_mtl_qc_item_tables_create.sql。TBLMTLQCITEM 與 TBLOPQCITEM 沿用既有 MTL/OP reference。
- 來源連線仍由本機受保護設定提供；IP、帳號、密碼不寫入 repo、終端或規格文件。家用端先核對 YMES schema/migrations，只新增缺漏結構。

### 更新 2026-11-17：MTL ML104 物料群組資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線直接唯讀呼叫 DBMS_METADATA.GET_DDL，查得 `MESTEST.TBLMTLMATERIALGROUPBASIS` DDL；另查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。僅查結構，未讀 MES 業務列、未在 Oracle 執行 DDL。
- GROUPNO 與 MATERIALTYPE 的資料字典 column Nullable 顯示 Y，但各有 ENABLED CHECK constraint 強制非 NULL；DATASTAMP NUMBER(11,0) DEFAULT 0 可 NULL。唯一索引為 GroupNo PK index，無其他索引/FK。
- Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 分別在 `database/reference/mestest_mtl_material_group_source_ddl.sql`、`database/reference/mestest_mtl_material_group_create.sql`；群組明細沿用 `mestest_mtl_master_source_ddl.sql`、`mestest_mtl_master_create.sql`。
- 連線秘密仍只在本機受保護設定中；IP、帳號、密碼、SID 不寫入 repo 或待辦，也不需要重新向使用者索取。

### 更新 2026-11-17：MTL ML107 物料標籤對應

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線唯讀呼叫 DBMS_METADATA.GET_DDL，查得 `MESTEST.TBLMTLLABELMAP` 與 lookup table `MESTEST.TBLPRDLABELBASIS`；並查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES、ALL_IND_COLUMNS。
- TBLMTLLABELMAP 主鍵為 (MaterialType, MaterialNo, LabelSource, LabelType)，LabelSource=5 是物料來源；DataStamp nullable DEFAULT 0。TBLPRDLABELBASIS 主鍵為 LabelNo，兩表都只有 PK unique index，沒有 FK/額外索引。
- Source DDL / PostgreSQL 空表參考見 `database/reference/mestest_mtl_label_map_source_ddl.sql`、`mestest_mtl_label_map_create.sql`、`mestest_prd_label_basis_lookup_create.sql`。本次只查資料字典結構，未讀業務列、未在 Oracle 執行 DDL。
- IP、帳號、密碼、SID 仍由本機受保護設定提供，未輸出或寫入文件/Git；下次公司端查 DDL 沿用這個既有 PowerShell/ODP.NET 方式。

### 更新 2026-11-17：MTL MLB01 來料 AutoLoader 格式檔設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取本機既有受保護連線，唯讀取得 `MESTEST.TBLMTLVENDORALSCRIPT` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。
- 主鍵為 `MTLFOLDERPATH`；只有 PK unique index，無外鍵/額外索引。`FORMATScript`、`RAWDATAFORMATSCRIPT` 是 CLOB；`DATASTAMP NUMBER(11,0) DEFAULT 0` 可 NULL；`STORERAWDATA`、`SENDMAIL` 為 NOT NULL DEFAULT 0。
- Oracle 來源 DDL 與 PostgreSQL 空表參考分別在 `database/reference/mestest_mtl_autoloader_source_ddl.sql`、`database/reference/mestest_mtl_autoloader_create.sql`。僅查資料字典結構，未讀 MES 業務列，未在 Oracle 執行 DDL。
- 連線秘密仍只在本機受保護設定中；IP、帳號、密碼、SID 未輸出或寫入文件/Git。家用端建立 migration 前須比對 YMES PostgreSQL live schema 與既有 migrations。

### 更新 2026-11-17：EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader

- 公司端以 PowerShell 5.1 + Oracle.ManagedDataAccess 從本機既有受保護連線唯讀取得四張表 DDL/資料字典：`TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`。沒有讀取 MES 業務列，也沒有在 Oracle 執行 DDL。
- Oracle source DDL 與 YMES 空表參考為 `database/reference/mestest_eqp_autoloader_source_ddl.sql`、`database/reference/mestest_eqp_autoloader_create.sql`。主表沒有 PK constraint，存在唯一索引 `(EquipmentNo,FileNameFormat)`；三個子表各有來源複合 PK unique index。四表間沒有 FK。
- `FormatScript`、`RawDataFormatScript` 為 CLOB；`DataStamp NUMBER(11,0) DEFAULT 0` nullable。子表與主表需由 API transaction 一致維護。DDL/schema 與功能規格見 `docs/modules/EQP/設備AutoLoader格式檔設定.md`。
- IP、帳號、密碼、SID 沒有輸出或寫入 Git；家用端只需核對 YMES PostgreSQL 結構並決定 forward migration，不要連 Oracle 建表。

### 更新 2026-11-17：QC111 8D 原因基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護 Oracle 連線直接唯讀查詢 MESTEST.TBLQCREASONBASIS_8D、TBLQCREASONSUBTYPE_8D 的 DBMS_METADATA.GET_DDL 及 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_8d_reason_source_ddl.sql、database/reference/mestest_qc_8d_reason_create.sql。兩張表各只有 PK unique index，沒有外鍵或其他索引；DataStamp 是 nullable DEFAULT 0。Subtype ReasonType 是 NUMBER(1,0)，Reason 明細 ReasonType 是 VARCHAR2(50)。
- 本次只讀取資料字典/DDL，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。
- 同以 Oracle 唯讀查詢 MESTEST.TBLSYSVALUELIST 的 TBLQCREASONBASIS_8D.REASONTYPE 選項，並核對 Customer/Production/MESClient/mesresource.zh-cht.resources：1 Men、人員；2 Machine_8D、設備；3 Material、物料；4 Method、方法；5 Environment、環境。此為系統字典與語系資源，不是業務資料。
- YMES 家用端先比對 live schema/migrations，缺少時才建立 forward migration；WinForms、簽核流程、API DTO/端點及子類別刪除留下無 FK 明細的行為見 docs/modules/QC/8D原因基本資料.md。

### 更新 2026-11-17：QC406 第一處理群組來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護 Oracle 連線直接唯讀查詢 MESTEST.TBLQCFIRSTGROUP 的 DBMS_METADATA.GET_DDL 與 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_first_group_source_ddl.sql、mestest_qc_first_group_create.sql。主表 PK 為 `(MCLASSNO, OPNO)`，只有 PK unique index，沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- 查找表 `TBLQCMCLASSBASIS` 與 `TBLQCHOLDDISPGROUPBASIS` 的來源 DDL / PostgreSQL CREATE 參考為 database/reference/mestest_qc_first_group_lookups_source_ddl.sql、mestest_qc_first_group_lookups_create.sql。作業站 `TBLOPBASIS` 已有 `mestest_operation_and_system_source_ddl.sql` / `mestest_operation_and_system_create.sql`。
- 查找來源為 TBLQCMCLASSBASIS、TBLOPBASIS、TBLQCHOLDDISPGROUPBASIS；Form 僅載入 IssueState=2。三表 key 與必要欄位已由 Oracle 資料字典直連核對，本功能不新增查找表。
- 本次只讀取 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。
- WinForms 多作業站新增/複製、`OPNo='*'` 通用設定、Issue Job `FIRST GROUP`、狀態刪除規則與 API 契約見 docs/modules/QC/第一處理群組.md。

### 更新 2026-11-17：QC407 最後處理群組來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護連線直連唯讀查詢 MESTEST.TBLQCFINALGROUP 的 DBMS_METADATA.GET_DDL 與 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_final_group_source_ddl.sql`、`mestest_qc_final_group_create.sql`。PK 為 `(MCLASSNO, OPNO)`，只有 PK unique index、沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- 主分類、作業站、異常處理群組查找表與 QC406 共用，僅查詢 IssueState=2；不新增重複 lookup table。
- 本次只讀取 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。UI、狀態與 Issue Job `Final GROUP` 規則見 `docs/modules/QC/最後處理群組.md`。

### 更新 2026-11-17：QC404 異常說明來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從既有本機受保護連線直連唯讀查詢 MESTEST.TBLQCHOLDDESCRIPTION 的 DBMS_METADATA.GET_DDL、`I_TBLQCHOLDDESCRIPTION_01` index DDL，以及 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES。
- 來源 DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_hold_description_source_ddl.sql`、`mestest_qc_hold_description_create.sql`。PK 是 `HoldDescriptionID`，另有 `MClassNo` 非唯一索引，沒有 FK；`HoldDescription` VARCHAR2(4000) NOT NULL；DataStamp nullable DEFAULT 0。
- 主分類查找重用 `TBLQCMCLASSBASIS`，WinForms LoadMClassBasis 預設 IssueState=2；不新增 lookup table。Core 以 `Guid.NewGuid().ToString()` 產生 HoldDescriptionID。
- 本次只讀 DDL/資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL；連線字串及憑證未輸出或寫入 Git。Form、狀態、Issue Job `HOLD DESCRIPTION` 規則見 `docs/modules/QC/異常說明.md`。

### 更新 2026-11-17：QC405 異常處理群組

- `TBLQCHOLDDISPGROUPBASIS` Oracle DDL/欄位/PK/index 已由公司端直連唯讀查得並放在 QC406 共用 lookup source DDL；PostgreSQL 空表 CREATE 也已存在。只有 GroupNo PK index，無其他 index/FK。
- `TBLUSRUSERGROUP` Oracle DDL/欄位/複合 PK/索引已於 2026-11-16 由同一受保護連線直接查得；來源與 PostgreSQL 空表 CREATE 參照放在設備工程群組成員 DDL 檔。其 AgentOf 在 Oracle 欄位屬 nullable，但因納入 PK，實際不得為 NULL。QC405 使用 GroupType=5/6。
- 本次未讀業務資料，未執行 Oracle DDL，沒有輸出或保存連線秘密；沒有新 CREATE SQL 要執行。功能及 API handoff 見 `docs/modules/QC/異常處理群組.md`。

### 更新 2026-11-17：QC403 異常原因基本資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 及本機既有受保護連線，唯讀取得 MESTEST.TBLQCHOLDITEMBASIS 的 DBMS_METADATA DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES/ALL_IND_COLUMNS。
- Oracle source DDL / PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`、`database/reference/mestest_qc_hold_item_basis_create.sql`。PK 為 HoldItemNo，只有 PK unique index，沒有 FK/其他 index；CreateERF nullable DEFAULT 1，HoldSystem NOT NULL DEFAULT 0，DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。
- WinForms `HoldSystem` ValueList 對照由 `STD/MEStc/DataImporter/HoldItemDataImporter.cs` enum 核實：0=MES、1=EAP_Exception、2=EDA_Exception。查找來源是已簽核 MClass/SClass/異常處理群組，及已簽核且目前版本 OCAP；未建立重複查找表。
- 本次僅查 DDL/資料字典，沒有讀 MES 業務資料，沒有在 Oracle 執行 DDL；連線字串/憑證未輸出或寫入 Git。畫面、CRUD、匯入與 Issue Job 規則見 `docs/modules/QC/異常原因基本資料.md`。

### 更新 2026-11-17：QC401 主分類基本資料

- TBLQCMCLASSBASIS Oracle DBMS_METADATA DDL 及 PostgreSQL 空表 CREATE SQL 已由先前 PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機既有受保護連線唯讀查得，納入 QC406 共用查找表 reference：`database/reference/mestest_qc_first_group_lookups_source_ddl.sql`、`mestest_qc_first_group_lookups_create.sql`。本次沿用既有結果，未重複連線查詢。
- 主鍵為 MClassNo，只有 PK unique index，沒有 FK 或其他 index；本功能不新增 DDL/CREATE 檔。資料表欄位與 UI/API 規則見 `docs/modules/QC/主分類基本資料.md`。
- 另由公司端原始碼確認匯入器位於 `STD/MESWin/QC/QC_MClassDataImport` 及 `STD/MEStc/DataImporter/MClassDataImporter.cs`；未找到 template binary。Excel 只讀程式與匯入檢查來源，不使用 Oracle 查業務列。
- 不讀取 MES 業務列、不在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或保存於 Git。家用端只核對 YMES PostgreSQL live schema/migrations。

### 更新 2026-11-17：QC402 次分類基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線唯讀取得 `MESTEST.TBLQCSCLASSBASIS` 的 `DBMS_METADATA.GET_DDL`，並單獨查詢 `PK_TBLQCSCLASSBASIS` unique index DDL。
- DDL 檔案：`database/reference/mestest_qc_s_class_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_s_class_basis_create.sql`。
- Primary Key 是 SClassNo；只有此 PK unique index，沒有 FK 或額外 index。MClassNo 在 Oracle DDL 可為 NULL、沒有 FK，但 WinForms 表單必填且僅允許選 IssueState=2 的主分類；由 QC402 API 驗證，不在 PostgreSQL DDL 自行加 FK。
- 本次只有查 Oracle metadata，沒有讀取 MES 業務列或執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。
### 更新 2026-11-17：QC101 抽樣表格來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線唯讀取得 MESTEST.TBLQCSAMPLINGTABLE 的 DBMS_METADATA table/index DDL，並查 ALL_CONSTRAINTS。
- 主鍵為 (SAMPLINGTABLE, MINNUM, MAXNUM)；只有 PK unique index；四個 NOT NULL 欄位約束；沒有 FK、其他 unique index 或額外索引。Oracle source DDL 與 PostgreSQL 空表 CREATE 參考為 database/reference/mestest_qc_sampling_table_source_ddl.sql、mestest_qc_sampling_table_create.sql。
- 未讀取 MES 業務列、未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations，缺少時才建 migration。
### 更新 2026-11-17：QC102 抽樣計劃設定_1916 來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLING1916` 的 `DBMS_METADATA.GET_DDL` table/index DDL，並查詢 `ALL_CONSTRAINTS`；沒有讀取業務資料。
- DDL 檔案：`database/reference/mestest_qc_sampling_1916_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_1916_create.sql`。
- 唯一索引 `(SAMPLINGTYPE, MINNUM, MAXNUM, SLEVEL)` 是獨立 unique index，不是 PK constraint；四個 NOT NULL constraint，沒有 FK/其他 index。欄位 nullable/default 依 Oracle DDL 保留。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC103 抽樣計劃來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER` 的 DBMS_METADATA table/index DDL，並查詢 ALL_CONSTRAINTS；未讀取業務資料。
- Oracle source DDL：`database/reference/mestest_qc_sampling_plan_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_create.sql`。
- 主表以 PlanNo 為 PK；參數表以 (PlanNo,ParameterNo) 為複合 PK。兩表只有 PK unique index，沒有 FK/其他 index；欄位 NULL/default 依 Oracle DDL 保留。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC104 Wafer 抽樣計劃來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護連線直接唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN_WAFER`、`TBLQCSAMPLINGRANGE`、`TBLQCSAMPLINGAREA_WAFER` 的 DBMS_METADATA table/index DDL，並查詢 ALL_CONSTRAINTS；未讀取業務列。
- Oracle source DDL：`database/reference/mestest_qc_sampling_plan_wafer_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_wafer_create.sql`。
- 主檔 PK 為 PlanNo；Area PK 為 (PlanNo,Area)；Range 無 PK/index/FK。三表沒有 FK。`TBLQCSAMPLINGRANGE` 由 WinForms QC103/QC104 共用名稱，API/schema 變更前需核對 YMES 現有使用。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC105 品質原因子類別來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取公司端既有受保護連線設定，直接唯讀取得 `MESTEST.TBLQCREASONSUBTYPEBASIS` 的 DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢業務列。
- Oracle source DDL：`database/reference/mestest_qc_reason_subtype_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_reason_subtype_basis_create.sql`。
- 主鍵為 ReasonSubType；唯一索引只有 PK index，無 FK/其他 index。欄位 NULL/default 依 DDL 記錄於 `docs/modules/QC/品質原因子類別資料.md`。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC106 品質原因基本資料來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCREASONBASIS` DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢業務列。
- Oracle source DDL：`database/reference/mestest_qc_reason_basis_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_reason_basis_create.sql`。
- 主鍵為 ReasonNo；唯一索引只有 PK index，無 FK/額外 index。欄位 NULL/default 與簽核/API 規則記於 `docs/modules/QC/品質原因基本資料.md`。
- 未在 Oracle 執行 DDL；連線字串、IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC107 計數判別法則來源表

- 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCATTRULEBASIS`、`TBLQCATTRULEDETAIL`、`TBLQCAQL` table/index DBMS_METADATA DDL 與 ALL_CONSTRAINTS；未查詢 MES 業務列。
- Oracle source DDL：`database/reference/mestest_qc_att_rule_basis_source_ddl.sql`、`database/reference/mestest_qc_aql_source_ddl.sql`；PG CREATE 參考為對應 `_create.sql`。
- 主表 PK RuleNo；detail PK (RuleNo,Action,ReasonType,ReasonLevel)；AQL PK (AQLType,MinNum,MaxNum,SLevel,AQL)。三表皆無外鍵/額外索引；主與明細皆無 DataStamp。欄位 nullable/default 詳見 `docs/modules/QC/計數判別法則基本資料.md`。
- 本次僅查 metadata、沒有讀取業務列或執行 Oracle DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations，現有 TBLQCAQL 必須優先重用。

### 更新 2026-11-17：QC108 計量品管法則來源表

- 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀取得 `MESTEST.TBLQCRULEBASIS` 的 DBMS_METADATA table/index DDL 與 ALL_CONSTRAINTS；未查詢 MES 業務列。
- Oracle source DDL：`database/reference/mestest_qc_variable_rule_basis_source_ddl.sql`；PostgreSQL CREATE 參考：`database/reference/mestest_qc_variable_rule_basis_create.sql`。
- PK 為 RuleSerial；唯一索引只有 PK index，沒有外鍵或額外索引。RuleNo 沒有 DB unique constraint，舊服務以大寫 RuleNo 檢查重複；DataStamp nullable DEFAULT 0。
- 僅查詢 metadata，沒有在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端只核對 YMES PostgreSQL schema/migrations。

### 更新 2026-11-17：QC109 要因資料維護來源表

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定唯讀查 `MESTEST.TBLSYSMESPARAMETER` 與 `ALL_TABLES` 中符合舊程式 `TableName LIKE 'FC_%'` 的表，再用 DBMS_METADATA、ALL_INDEXES、ALL_CONSTRAINTS 取得結構。本次查得 `FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` 三表。
- Oracle source DDL：`database/reference/mestest_qc_factor_maintain_source_ddl.sql`；PostgreSQL CREATE 參考：`database/reference/mestest_qc_factor_maintain_create.sql`。四表各只有 PK unique index，沒有 FK/額外索引。
- 僅查詢表名與 DDL/資料字典 metadata，沒有讀取 MES 業務列，沒有在 Oracle 執行 DDL；IP、帳號、密碼、SID 未輸出或寫入 Git。家用端核對 YMES PostgreSQL 中既有參數目錄與 FC 表，必要時才建立 forward migration。

### 更新 2026-11-17：QC110 特殊主檔維護

- QC110 使用相同的 `TBLSYSMESPARAMETER` 目錄與 `FC_%` 動態表機制，但篩選 `ParameterClass=0`；QC109 篩選 `ParameterClass=1`。請家用端以獨立 FunctionKey/API 權限處理兩個功能。
- QC110 所用 `TBLSYSMESPARAMETER`、`FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` Oracle metadata DDL 及 PostgreSQL CREATE 參考已在 QC109 同一批次取得並交付，檔案位於 `database/reference/mestest_qc_factor_maintain_{source_ddl,create}.sql`；本次未重複連 Oracle 或查詢業務列，不需另一份相同 DDL。
- 公司端只新增 Vue DEV 頁與 API handoff；YMES PostgreSQL 是否已有相同結構由家用端比對 live schema/migrations 決定，僅對缺少項新增 forward migration。不得在 Oracle 執行 DDL、不得從 MES 複製業務資料。

### 更新 2026-11-17：QC201 計數品管項目

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端本機既有受保護連線設定直接唯讀連接 MESTEST，取得 18 個 QC201 主表/子表/引用物件的 DBMS_METADATA DDL、欄位、NULL/default、constraints、PK/index metadata。連線方式沿用本檔既有公司端 Oracle 設定；不透過 DBeaver 操作畫面，不讀 MES 業務列。
- 原始結構記錄：`database/reference/mestest_qc_item_attributes_source_ddl.sql`；四張直接維護子表的 PostgreSQL CREATE 參考：`database/reference/mestest_qc_item_attributes_create.sql`；主表 CREATE 已存在於 MTL 共用 reference SQL。只有 YMES schema 核對確認缺表/欄位後才新增 migration。
- IP、帳號、密碼、SID 與 protected config 檔案路徑不寫入文件/SQL/Git；沒有在 Oracle 執行 DDL。

### 更新 2026-11-17：QC112 品質原因產品指導書

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定直接唯讀取得 `MESTEST.TBLQCREASONPRDSOPFILE` 的 `DBMS_METADATA.GET_DDL`，並查詢欄位、PK、索引及外鍵字典。結構含四欄，PK `(REASONNO, PRODUCTNO)`；只有 PK unique index，無 FK、DataStamp 或其他索引。
- Oracle source DDL 與 PostgreSQL 空表 CREATE 參考分別為 `database/reference/mestest_qc_reason_prd_sop_file_source_ddl.sql`、`database/reference/mestest_qc_reason_prd_sop_file_create.sql`。家用端已核對 YMES live schema；`0020_qc112_reason_product_sop_file.sql` 只補缺少的 `tblqcreasonbasis` 與 `tblqcreasonprdsopfile`，並授予 `ymes_app` 必要 table privileges。
- 僅查詢 Oracle 資料字典，沒有讀取 MES 業務列、沒有在 Oracle 執行 DDL。連線值及檔案服務參數值均未輸出、保存或提交。YMES API 的 QC112 附件存於獨立 named volume，不沿用未存在的 MES file-service 設定。

### 更新 2026-11-17：QC202 計數管制內容

- MESTEST.TBLSPCATTLIMIT Oracle DDL 沿用 QC201 當次由 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接連線取得的來源字典結果；本次未透過 DBeaver UI 查詢。來源 reference：database/reference/mestest_qc_spc_att_limit_source_ddl.sql，欄位及索引資訊也見 mestest_qc_item_attributes_source_ddl.sql 的 TBLSPCATTLIMIT 區塊。
- QC202 不需要新增 Oracle 連線設定。既有連線方法、憑證保護原則和 driver 相容性見本文件前段；連線值只從本機受保護設定讀取，不輸出、不寫入 Markdown/Git。本次只引用既有結構查詢，沒有讀取 MES 業務列或在 Oracle 執行 DDL。
- YMES PostgreSQL 空表參考 database/reference/mestest_qc_spc_att_limit_create.sql 以 JSONB 承接舊 Oracle 動態因子欄位，屬 YMES 固定 schema 適配；家用端需先核對 live schema/migrations，不得把參考檔直接當 migration。

### 更新 2026-11-17：QC203 計數品管項目判別法則對應

- 公司端使用 Windows PowerShell 5.1、既有受保護的 MESTEST 連線設定與 Oracle.ManagedDataAccess，唯讀查詢 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER 的 DBMS_METADATA DDL、ALL_TABLES 與獨立 index metadata；未查業務列，也未在 Oracle 執行 DDL。過程採純 metadata SELECT；一次 session 被 Oracle 中斷後以精簡查詢重試並成功。
- 原始 Oracle DDL reference：database/reference/mestest_qc_attribute_rule_map_source_ddl.sql；PostgreSQL CREATE reference：database/reference/mestest_qc_attribute_rule_map_create.sql。後者將來源動態因子欄適配為 JSONB，並提議 sequence 供 YMES serial 使用；必須先核對 live schema、migration 與共用 serial service，不可直接當 migration 執行。
- 連線設定只在本機受保護附件內讀取，未把 IP、帳號、密碼、SID、連線字串或附件路徑輸出、保存到專案或提交 Git。

### 更新 2026-11-17：QC204 刪除計數型品管項目量測記錄

- QC204 所用的 `TBLQCITEMFORATTRIBUTES`、`TBLSPCATTBASISLOG`、`TBLSPCATTDETAILLOG`、`TBLSPCATTEDITLOG`、`TBLSPCATTDETAILLOG_TAPE` DDL 已存在 `database/reference/mestest_qc_item_attributes_source_ddl.sql`，本次直接閱讀現有 reference，未重新連 Oracle，也未查業務資料。
- QC204 不新增 Oracle/YMES 資料表。家用端需先比較 PostgreSQL migration/schema；只有缺少來源日誌表時才按既有 Oracle reference 設計 YMES migration/Create SQL，保留已存在 schema 與資料。

### 更新 2026-11-17：QC205 計量品管項目 THEIL BU1

- 公司端沿用既有受保護本機連線，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST，唯讀查詢 ALL_TABLES、DBMS_METADATA.GET_DDL 及索引字典。
- 取得 XX_TBLQCITEM_MEMO、XX_TBLSPC_TEMPDATA_VAR、TBLSPCVARDETAILLOG，以及 QC205 刪除/複製/引用檢查所需的 23 個候選物件；其中 TBLSPCQCHARTDETAIL、TBLSPCQCHARTDETAILLOG、TBLSPCQCHARTRULE、TBLSPCQCHARTBASIS 在 MESTEST 不存在。未讀業務資料、未在 Oracle 執行 DDL。
- Oracle source DDL：database/reference/mestest_qc205_theil_custom_source_ddl.sql、mestest_qc205_variable_dependencies_source_ddl.sql；Memo PostgreSQL Create 參考：mestest_qc205_theil_custom_create.sql。家用端先核對 YMES live schema/migrations，再決定必要 migration。
- 連線秘密、主機值、帳號、密碼、SID 與受保護附件路徑未輸出或寫入 Git。
### 更新 2026-11-17：QC209 計量品管項目管制法則對應

- QC209 `TBLQCITEMRULE` Oracle DDL/PK 沿用 QC205 前次以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀擷取結果：`database/reference/mestest_qc205_variable_dependencies_source_ddl.sql`。`TBLQCRULEBASIS` 沿用 QC108 直接查得來源 reference。本次未重複連線，也未查 MES 業務列或執行 Oracle DDL。
- QC209 新增 YMES schema-only Create 參考：`database/reference/mestest_qc209_variable_item_rule_map_create.sql`；需由家用端比對 live PostgreSQL schema/migrations 後再決定是否建立 migration。

### 更新 2026-11-17：QC211 二次檢驗抽樣計畫

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經公司端既有受保護設定唯讀連接 MESTEST；以 DBMS_METADATA.GET_DDL 與 ALL_* 字典取得 TBLQCSAMPLINGFOR2NDINSP 欄位、constraint、index metadata。Oracle source DDL 為 database/reference/mestest_qc211_second_inspection_sampling_source_ddl.sql。
- 主表有五個 NOT NULL 欄位、QCITEMTYPE/ DATASTAMP 預設值；無 PK/FK constraint，唯一索引 U_TBLQCSAMPLINGFOR2NDINSP_01 的欄位順序為 QCITEMNO、QCITEMTYPE、OPNO、OLDSAMPLEPLAN。PostgreSQL schema-only Create 參考為 database/reference/mestest_qc211_second_inspection_sampling_create.sql。
- 只查結構 metadata，未讀業務列、未在 Oracle 執行 DDL。IP、帳號、密碼、SID 與受保護連線設定未寫入 Git。

### 更新 2026-11-17：QC212 計量品管項目群組

- 本次直接閱讀已保存的 Oracle source DDL reference，沒有再次連線 Oracle：`TBLQCITEMGROUPBASIS` 在 `mestest_mtl_qc_item_tables_source_ddl.sql`；`TBLQCITEMGROUPDETAIL` 與 `TBLQCCALCULATESCRIPT` 在 `mestest_qc205_variable_dependencies_source_ddl.sql`；`XX_TBLQCITEM_MEMO` 在 `mestest_qc205_theil_custom_source_ddl.sql`；共用 `TBLQCITEMPARAMETER` / `TBLOPQCITEM` 也各有既有 DDL reference。
- 新增 `database/reference/mestest_qc212_item_group_create.sql`，僅補 PostgreSQL YMES 的 `tblqcitemgroupdetail`、`tblqccalculatescript` schema-only CREATE 參考。群組主表與 THEIL Memo 已各有先前 CREATE 參考；家用端要先核對 live PostgreSQL 和 migration，再補缺表 migration。
- 本次未讀 Oracle 業務列、未在 Oracle 執行 DDL；未使用 DBeaver 螢幕查詢，連線秘密未輸出、未寫入專案。

### 更新 2026-11-17：QC210 刪除計量型品管項目量測記錄

- QC210 依賴的變數量測 Oracle DDL 沿用先前以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得的 reference：database/reference/mestest_qc205_variable_dependencies_source_ddl.sql、mestest_qc205_theil_custom_source_ddl.sql、mestest_qc_item_attributes_source_ddl.sql。本次重新核對已保存的 CREATE TABLE/index 定義，未再次連 Oracle，未讀業務資料。
- PostgreSQL 空表 CREATE 參考為 database/reference/mestest_qc210_variable_measurement_log_delete_create.sql；包含 9 張 SPC 日誌表的來源欄位/PK/索引。TBLQCSAMPLEPLANCHANGE 沿用 QC201 的 mestest_qc_item_attributes_create.sql。
- 公司端未連 YMES PostgreSQL。家用端須先比對 live schema 與已部署 migration，再建立缺少的 forward migration；不能把參考檔當成已套用 migration，也不能在 Oracle 執行 PostgreSQL DDL。
- Oracle IP、帳號、密碼、SID 與受保護連線附件路徑仍只存在本機受保護設定，不記錄於 Git。

### 更新 2026-11-17：QC216 計量品管項目群組_減項

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經本機既有受保護設定直接唯讀查 MESTEST 的 PMI 群組主檔、明細、扣除基準及三張 SPC 表單 map。取得 DBMS_METADATA DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES；未讀 MES 業務列，未在 Oracle 執行 DDL。
- Oracle source DDL 位於 `database/reference/mestest_qc216_*_source_ddl.sql`；PostgreSQL 空表 CREATE 參考 `database/reference/mestest_qc216_variable_item_group_pmi_create.sql`。六表均只有 Oracle PK unique index，沒有 FK 或額外索引。`TBLQCGROUPDEDUCTIONBASIS_PMI` 的 Factor 欄位會由舊 Core 動態新增，YMES 參考改用 `factorvalues JSONB`，不是 Oracle 欄位的原樣複製。
- 查詢僅限資料字典/DDL。連線 IP、帳號、密碼、SID 和受保護設定位置未輸出或寫入 Git；公司端不連 YMES PostgreSQL。家用端必須先查 live schema/migrations，只為缺漏結構建立 forward migration。

### 更新 2026-11-17：QC213 計數品管項目群組

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機既有受保護連線唯讀連至 MESTEST，取得兩張 QC213 主/明細表及兩張 Core 硬刪依賴表的 DBMS_METADATA.GET_DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES metadata。WIP 檢查表的來源 DDL 已在既有 QC205 dependency reference。
- Oracle source DDL：`mestest_qc213_attribute_item_group_basis_source_ddl.sql`、`mestest_qc213_attribute_item_group_detail_source_ddl.sql`、`mestest_qc213_attribute_item_group_op_detail_source_ddl.sql`、`mestest_qc213_attribute_item_group_prd_item_source_ddl.sql`；WIP source 為 `mestest_qc205_variable_dependencies_source_ddl.sql`。PostgreSQL YMES 空表 CREATE 參考：`mestest_qc213_attribute_item_group_create.sql`，列出 QC213 主/明細表、OP Group/PRD 清理關聯、WIP 使用檢查表與來源索引。
- 主檔 PK 為 GROUPNO；明細 PK 為 (GROUPNO, QCITEMNO)。兩表僅有 PK unique index，沒有 FK 或額外 index。預設值欄位在 Oracle 可為 NULL，CREATE 參考保留這項語意。額外查得 TBLOPGQCITEMDETAIL PK=(OPNO,QCITEMNO)，TBLPRDQCITEM PK=(PRODUCTNO,PRODUCTVERSION,OPNO,QCITEMNO)，兩表無 FK/額外索引；TBLWIPTEMP_QCITEM 無 PK，只有 (LOTNO,QCITEMNO) 與 (LOTNO) 索引。
- Legacy Core 硬刪流程先確認 TBLWIPTEMP_QCITEM 沒有該 QC 群組的在線記錄，後清主表/明細以及 TBLQCITEMPARAMETER、TBLOPQCITEM、TBLOPGQCITEMDETAIL、TBLPRDQCITEM 對應資料。家用端須核對 YMES 實際關聯欄位並以 transaction 維持一致；共用 parameter/OP source DDL 沿用既有 QC reference。
- 只查結構 metadata，未查 MES 業務列、未在 Oracle 執行 DDL。IP、帳號、密碼、SID 與受保護連線設定未輸出或寫入 Git。家用端只查 YMES PostgreSQL live schema/migrations，再對缺表建立 migration。

### 更新 2026-11-17：QC218 計數品管項目產品原因對應設定

- 公司端使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，沿用本機受保護 MESTEST 唯讀連線，查詢兩張 mapping 表與三張共用 lookup 表的 DBMS_METADATA table/index DDL、欄位 nullable/default、PK/index/FK metadata；未查 MES 業務資料列，未在 Oracle 執行 DDL。
- Oracle source DDL：`mestest_qc218_tblqcitemprdreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemforattributes_source_ddl.sql`、`mestest_qc218_tblqcreasonbasis_source_ddl.sql`、`mestest_qc218_tblprdproductbasis_source_ddl.sql`。PostgreSQL CREATE-only 參考只建立兩張 mapping 表：`mestest_qc218_item_product_reason_map_create.sql`。Mapping tables 都只有 PK unique index，沒有 FK 或其他索引。
- 僅查 Oracle metadata；連線 IP、帳號、密碼、SID 和受保護設定位置未輸出或寫入 Git。公司端未連 YMES PostgreSQL。家用端先核對 PostgreSQL live schema/migrations，只為缺少的 mapping 結構建立 forward migration；三張共用 lookup tables 不可重建。

### 更新 2026-11-17：QC214 計量品管項目匯入

- QC214 沿用先前以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得的 QC205/QC201 Oracle 結構 reference：`mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_qc205_variable_dependencies_source_ddl.sql`、`mestest_qc_item_attributes_source_ddl.sql`；QC210 PostgreSQL CREATE 參考為 `mestest_qc210_variable_measurement_log_delete_create.sql`。本次直接閱讀 WinForms Form、wsQC 與 clsQCBasis，沒有再次連 Oracle、沒有讀業務列、沒有執行 DDL。
- QC214 是匯入流程，不需要新 Oracle 表或獨立 PostgreSQL CREATE SQL。`TBLQCITEMFORVARIABLES` 空表 CREATE 參考在 `mestest_mtl_qc_item_tables_create.sql`；子表和引用表依 QC205/QC210/QC201 的現有 CREATE/reference 核對。
- Core 對 SPC Chart Basis 的引用拼字與現有 QC205 dependency DDL 清單的缺表拼字不一致。家用端依 YMES live schema、migration 及 API 實際 SQL 確認正式物件名；不要為了讓舊拼字通過而臆造一張新表。
- 本次沒有新查詢或資料連線。IP、帳號、密碼、SID 與受保護設定都未輸出或寫進 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC215 計數品管項目匯入

- QC215 沿用先前已保存的 QC201/QC205/QC213 Oracle 結構參照：`mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_qc_item_attributes_source_ddl.sql`、`mestest_qc213_attribute_item_group_detail_source_ddl.sql` 及 QC205 dependency DDL；PostgreSQL CREATE 參考沿用對應 QC201/QC205/QC213 reference。本次直接閱讀 WinForms Form、wsQC 與 clsQCBasis，沒有再次連 Oracle、沒有讀業務列、沒有執行 Oracle DDL。
- QC215 是匯入既有 `TBLQCITEMFORATTRIBUTES` 計數品管資料的功能，沒有 QC215 專用資料表，不新增 CREATE SQL。範圍依賴 QC Item Parameter/Factor/Reason Map、抽樣計畫、作業站、群組及 SPC 既有資料表；不得重建共用表。家用端只比對 YMES PostgreSQL live schema/migrations，缺漏時建立 forward migration。
- 連線 IP、帳號、密碼、SID 與受保護設定都未輸出或寫進 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC302 匯入管制圖內容

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護的 MESTEST 唯讀設定，查詢 `TBLSPCQCCHARTBASIS`、`TBLSPCQCCHARTDETAIL`、`TBLSPCQCCHARTDETAILLOG`、`TBLSPCQCCHARTRULE` 的 DBMS_METADATA table/index DDL。沒有讀 MES 業務資料列，沒有在 Oracle 執行 DDL。
- Oracle source DDL `mestest_qc302_spc_qc_chart_source_ddl.sql` 與 PostgreSQL 空表 CREATE-only 參考 `mestest_qc302_spc_qc_chart_create.sql` 已保存於 `database/reference/`。QC302 匯入不會新增/更新 `TBLSPCQCCHARTSAMPLANPARAMETER`，因此未納入此功能的 CREATE handoff。
- Oracle 欄位中的 FactorNo 動態欄位在 YMES CREATE 參考採 `factorvalues JSONB`；家用端須核對 live schema/migrations 並檢查重複 QCItem/Factor 組合，才建立 forward migration。連線值未輸出或寫入 Git；公司端不連 YMES PostgreSQL。

### 更新 2026-11-17：QC301 管制圖內容

- QC301 共用 QC302 已查得的 `TBLSPCQCCHARTBASIS`、`TBLSPCQCCHARTDETAIL`、`TBLSPCQCCHARTDETAILLOG`、`TBLSPCQCCHARTRULE` Oracle DDL；本次另以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護 MESTEST 唯讀設定，直接查 `TBLSPCQCCHARTSAMPLANPARAMETER` 的 DBMS_METADATA table/index DDL。
- 新增 Oracle DDL reference `mestest_qc301_chart_sampling_parameter_source_ddl.sql` 和 PostgreSQL 空表 CREATE-only 對照 `mestest_qc301_chart_sampling_parameter_create.sql`。參數表 PK=(FactorSerial,ParameterNo)，未查到 FK 或額外索引。
- 僅查 Oracle 結構 metadata，未讀業務資料列、未在 Oracle 執行 DDL。公司端未連 YMES PostgreSQL；家用端須先核對 live schema/migrations，缺少時才新增 forward migration。IP、帳號、密碼、SID 與受保護設定未輸出或寫入 Git。

### 更新 2026-11-18：QC303 品管項目規格

- QC303 沿用已保存的 Oracle DDL：`database/reference/mestest_qc205_variable_dependencies_source_ddl.sql` 中 `TBLQCSPECDETAIL` 與 `TBLQCSPECBASIS` 區塊。該 DDL 是先前由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得，本次沒有改用 DBeaver 螢幕操作，也沒有重新讀業務列或執行 Oracle DDL。
- PostgreSQL 適配的 CREATE-only 對照 `database/reference/mestest_qc303_spec_basis_create.sql` 將因子欄收納為 JSONB。家用端查 live schema 後確認 QC303 Basis/Detail 缺少，套用 migration `database/migrations/0026_qc303_spec_basis.sql`；套用前備份 `/opt/ymes/backups/ymes_before_0026_qc303_20261118.dump` 通過 `pg_restore --list`。Migration 只建 QC303 結構與 `ymes_app` 權限；共用 QC Item/Factor/Parameter/Issue/Job/QCSN 資源未建立。
- 遠端 `ymes-web` 已更新 QC303 App.vue 路由並單獨重建，healthy；API readiness=200、未登入 QC303 API=401、Web QC303 route=200。app-browser 已登入核對正式選單、頁面、四種狀態切換及 Close。正式 QC Item 清單為空，業務 CRUD 暫無資料可端到端驗收。詳細 API 限制與下一步記於 `docs/modules/QC/品管項目規格.md`。
- 本次未輸出或寫入 Oracle IP、帳號、密碼、SID 或本機受保護連線設定。

### 更新 2026-11-17：QC304 物料規格設定

- QC304 的 Chart Basis/Detail/DetailLog Oracle DDL 沿用 QC302 既有 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀查詢 reference `mestest_qc302_spc_qc_chart_source_ddl.sql`；MTL/QC Item/Group DDL 沿用已保存 MTL、QC201、QC205、QC212 references。本次直接閱讀 WinForms Form、wsQC、clsQCBasis 原碼，未重新查 Oracle。
- QC304 沒有新專屬資料表，重用 QC302 已建立的 chart schema/migration 0024 與既有物料/QC Item 關聯表；未新增 CREATE SQL。家用端先查 YMES live schema/migrations，只有確認相依物件缺漏才新增 forward migration。
- 本次未查 MES 業務資料列，未在 Oracle 執行 DDL；未輸出或寫入 IP、帳號、密碼、SID 或本機受保護連線設定。

### 更新 2026-11-17：QC305 產品規格設定

- QC305 的 Chart Basis/Detail/DetailLog Oracle DDL 沿用 QC302 reference `mestest_qc302_spc_qc_chart_source_ddl.sql`；產品主檔 DDL 沿用 `mestest_qc218_tblprdproductbasis_source_ddl.sql`，PRD QC Item/Variable/Factor 依賴沿用 QC205 reference，QC Item Group CREATE 沿用 QC212 reference。本回合直接讀 `frmQCProductSpec.vb`、`clsQCBasis.cs` 與既有 DDL 檔，沒有重新連 Oracle 或讀取業務列。
- QC305 不新增表；重用 YMES Chart Basis/Detail/DetailLog、產品、PRDQCITEM、QC Item/Factor/Group schema。`TBLPRDTYPE` 是舊產品類別 lookup 依賴，但 repo 沒有獨立 Oracle DDL reference；家用端先核對 live schema/API，若確認 YMES 缺少對應物件，再由公司端透過 PowerShell + Oracle.ManagedDataAccess 取得原始 DDL。
- 產品 Basis 的動態 PRODUCT 因子在 YMES 使用 `factorvalues JSONB`；家用端需按 QC302 實際資料存取方式核對舊 physical `product` 欄的相容性。QC305 Core 以 ProductNo 而非 ProductVersion/OPNo 讀寫 chart basis，規格已記錄此舊行為。
- 本回合未在 Oracle 執行 DDL、未查 YMES PostgreSQL、未輸出或寫入 IP、帳號、密碼、SID 或受保護連線設定。

### 更新 2026-11-18：QC409 OCAP 異常單處置調整

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，讀取既有受保護 DatabaseConfigs.CS.Test.Mes.ConnectionString，直連 MESTEST 查 DBMS_METADATA.GET_DDL 及 index metadata。查詢只讀結構，不讀 WIP 業務列；未在 Oracle 執行 DDL/DML，連線值未輸出或寫入 Git。
- 專用表 DDL：mestest_qc409_wip_ocap_source_ddl.sql，涵蓋 TBLWIPERFOCAPDISPOSITION、TBLWIPERFOCAPSTATE、TBLWIPERFOCAPGROUPSTATE。
- 核心 ERF/WIP/通知依賴 DDL：mestest_qc409_wip_disposition_dependencies_source_ddl.sql，涵蓋 TBLWIPERFBASIS、TBLWIPERFBATCHLOT、TBLWIPERFDISPOSITION、TBLWIPERFHOLDREASONDISP、TBLWIPLOTSTATE、TBLWIPLOTLOG、TBLWIPGOODBINSTATE、TBLWIPERF_ATTACHFILE、TBLWIPERFOCAP_ATTACHFILE、TBLUSRUSERBASIS、TBLENTCUSTOMERCONTACTOR，以及來源索引。
- mestest_qc409_wip_ocap_create.sql 僅為三張 OCAP 專用表提供 YMES PostgreSQL schema-only Create 對照；其他表為共用 WIP/ENT/USR schema，家用端需查 live schema/migrations 再適配，勿重建或盲目套用 Oracle DDL。
### 更新 2026-11-18：OP103 作業站類別 TBLOPTYPE

- 以公司端既有受保護 Oracle 設定，透過 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 MESTEST.TBLOPTYPE 的 DBMS_METADATA.GET_DDL 與索引資訊；DDL 保存為 database/reference/mestest_op103_optype_source_ddl.sql。只讀 metadata，沒有讀 MES 業務資料列或執行 Oracle DDL；連線秘密沒有寫入 Git。
- 確認主鍵只有 OPTYPE；無 FK/額外 index。DATASTAMP nullable + DEFAULT 0；ISSUESTATE NOT NULL + DEFAULT 0。PostgreSQL CREATE-only 對照為 database/reference/mestest_op103_optype_create.sql，不等於 migration。
- WinForms 舊 Core 新增作業站類別前，會檢查 TBLOPBASIS.OPNO 是否已存在同名 OP；刪除會先清除 TBLOPPROPERTY 中對應 OPNo。家用端須先比 live schema 與 YMES OP Property API，再實作交易及 namespace 保護，詳見 docs/modules/OP/作業站類別資料.md。

### 更新 2026-11-18：OP101 作業站法則設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀取得 `MESTEST.TBLOPCONTROLRULE` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_CONSTRAINTS`/`ALL_INDEXES`；確認僅有 RuleNo 主鍵唯一索引，無 FK 或其他索引。
- Oracle source DDL 與 PostgreSQL CREATE-only 參考：`database/reference/mestest_op101_control_rule_source_ddl.sql`、`mestest_op101_control_rule_create.sql`。未讀 MES 業務列，沒有在 Oracle 執行 DDL；憑證未輸出或保存到 Git。

### 更新 2026-11-18：OP102 企業邏輯設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 DatabaseConfigs.CS.Test.Mes.ConnectionString 直連 MESTEST，唯讀取得 TBLOPBUSINESSRULEBASIS、TBLOPSCENARIOBASIS 的 DBMS_METADATA.GET_DDL，並查 ALL_INDEXES/ALL_CONSTRAINTS。
- 兩表各只有主鍵 unique index、沒有外鍵或額外索引。Oracle source DDL 與 PostgreSQL CREATE-only 對照分別保存於 OP102 reference 檔案；情境表僅為 DT 選項 lookup。
- 僅查結構 metadata，沒有查詢 MES 業務資料列、沒有在 Oracle 執行 DDL；連線值未輸出或放入 Git。

### 更新 2026-11-18：OP105 作業站群組資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護的 MESTEST 設定直連唯讀取得 TBLOPGROUPBASIS、TBLOPGROUPDETAIL 的 DBMS_METADATA.GET_DDL，並查 ALL_TABLES、ALL_INDEXES、ALL_CONSTRAINTS。
- 兩表只有主鍵唯一索引，沒有 FK/額外 index。來源 DDL 與 PostgreSQL CREATE-only 對照分別保存於 database/reference/mestest_op105_group_basis_source_ddl.sql、mestest_op105_group_basis_create.sql；first-phase bootstrap 也已同步 Oracle DEFAULT 0。
- 只讀結構 metadata，未讀 MES 業務資料列、未在 Oracle 執行 DDL/DML；連線秘密未輸出或提交。家用端需先查 YMES live schema/migrations，已存在的表只補必要 defaults migration。

### 更新 2026-11-18：OPB03 客戶站點對應設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從受保護的 MESTEST 連線唯讀執行 DBMS_METADATA.GET_DDL，並查 ALL_CONSTRAINTS、ALL_INDEXES。
- 取得 TBL_BOPCUSTOPMAPBASIS、TBL_BOPCUSTOPMAPDETAIL DDL；兩表只有欄位 NOT NULL checks 與 PK，只有 PK unique index，沒有 FK/額外索引。
- Oracle DDL 與 PostgreSQL CREATE-only 參照分別保存於 database/reference/mestest_opb03_customer_op_mapping_source_ddl.sql、database/reference/mestest_opb03_customer_op_mapping_create.sql。
- 只讀結構 metadata，未查 MES 業務資料列、未執行 Oracle DDL/DML；連線秘密未輸出或寫入 Git。家用端先查 live schema/migrations，確定缺表才加 forward migration。

### 更新 2026-11-18：OPB02 ECN 作業站群組設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護的 `DatabaseConfigs.CS.Test.Mes` MESTEST 設定唯讀取得 `TBLOPECNGROUPBASIS`、`TBLOPECNGROUPDETAIL` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。
- Basis PK 為 `OPGROUPNO`；Detail PK 為單欄 `OPNO`。兩張表僅有 PK unique index 與 NOT NULL check，沒有 FK 或其他獨立 index。Basis `ISSUESTATE` 必填但無 Oracle default；`DATASTAMP` nullable DEFAULT 0。
- Oracle source DDL 與 PostgreSQL CREATE-only 參照為 `database/reference/mestest_opb02_ecn_operation_group_source_ddl.sql`、`database/reference/mestest_opb02_ecn_operation_group_create.sql`。只查 metadata，未讀業務列、未執行 Oracle DDL/DML，連線秘密未輸出或提交；家用端先核對 live schema 再補 forward migration。

### 更新 2026-11-18：OPB01 作業站參數群組設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護的 `DatabaseConfigs.CS.Test.Mes` 設定唯讀取得 `TBLOPGROUPATTRIB`、共用查找表 `TBLSYSELEMENTBASIS` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。
- `TBLOPGROUPATTRIB` PK 為 `(CUSTOMERNO, OPGROUPNO, ELEMENTNO)`；只有 PK unique index 與 NOT NULL checks，沒有 FK/額外 index。`BYSECONDCLASS`、`DATASTAMP` nullable DEFAULT 0。`TBLSYSELEMENTBASIS` 是 first-phase 已存在共用表，本功能不重建。
- Oracle source DDL 與 PostgreSQL CREATE-only 參考為 `database/reference/mestest_opb01_operation_group_attribute_source_ddl.sql`、`database/reference/mestest_opb01_operation_group_attribute_create.sql`。metadata-only；未讀業務列、未執行 Oracle DDL/DML，連線秘密未輸出或提交。家用端先核對 live schema 後，只對實際缺表新增 forward migration。

### 更新 2026-11-18：PRS PS101 生產區段資料

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線直連 MESTEST 唯讀取得 `TBLPRSPRODUCTIONSEGMENT` 的 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`，並取得 PK index DDL。未讀業務列、未在 Oracle 執行 DDL/DML、未輸出或提交連線秘密。
- Oracle source DDL：`database/reference/mestest_prs101_production_segment_source_ddl.sql`；PostgreSQL CREATE-only：`database/reference/mestest_prs101_production_segment_create.sql`。欄位 nullable/default 依 Oracle metadata；只有 `PSNO` 主鍵 unique index，沒有 FK 或其他 index。
- `TBLPRSPRODUCTIONSEGMENT` 由 PS101 擁有；PS104 只重用此表，不另建同義表。MES Oracle 連線設定仍只留在本機受保護設定，不寫入文件或 Git。

### 更新 2026-11-18：PRS PS102 製造層別設定

- 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護既有 `DatabaseConfigs.CS.Test.Mes` 連線設定直接唯讀查 MESTEST `TBL_FABPRSSTAGEBASIS` 的 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`，另外取得兩個 index DDL。
- 表含 StageNo PK、Sequence UNIQUE；沒有 FK 或其他 index。`ISSUESTATE` nullable DEFAULT 0、`CREATEDATE` nullable DEFAULT SYSDATE、`DATASTAMP` nullable DEFAULT 0。
- Oracle source DDL：`database/reference/mestest_ps102_stage_basis_source_ddl.sql`；YMES PostgreSQL CREATE-only：`database/reference/mestest_ps102_stage_basis_create.sql`。未讀業務列、未執行 Oracle DDL/DML、未輸出或提交連線秘密。Oracle 設定只保留在本機受保護檔案。

### 更新 2026-11-18：PRD PD102 Parameter Setup Oracle 字典查詢

- 公司電腦使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，讀取本機受保護的既有 `DatabaseConfigs.CS.Test.Mes` Oracle profile，schema 為 MESTEST。Profile 內存 IP/帳號/密碼/SID；這些值不能放入 repo、SQL、Markdown 或 shell 輸出。
- 直接唯讀查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`、`ALL_TRIGGERS`，只讀 schema metadata，沒有查業務資料列，也沒有在 Oracle 執行 DDL/DML。
- 對 `TBLPRDOP`、`TBLPRDOPATTRIB`、`TBLPRDOPRULE`、`TBLPRDOPEQPRECIPE`、`TBLPRDOPRULEXML_CLOB`、`TBLPRDOPBUSINESSRULE`、`TBLPRDOPBUSINESSRULERELATION`、`TBLPRDOPAREA` 呼叫 `DBMS_METADATA.GET_DDL` 遇到 SocketException；字典查詢仍成功。故 `mestest_prd_parameter_setup_source_ddl.sql` 由 Oracle 字典還原邏輯欄位、NULL、PK 與索引，不含 tablespace/storage。
- PostgreSQL CREATE-only 對照是 `database/reference/mestest_prd_parameter_setup_create.sql`。先查 YMES live schema/migrations，只把實際缺少內容新增為 migration。來源 profile 留在本機受保護位置；此檔刻意不複製連線密碼或內網位址。


### 更新 2026-11-18：PRD PD102 Business Rule 表沿用查證

- Business Rule 使用的 `TBLPRDOPBUSINESSRULE`、`TBLPRDOPBUSINESSRULERELATION`、`TBLPRDOPRULEXML_CLOB` 已包含在上方 Parameter Setup 對 MESTEST 的唯讀 Oracle dictionary query；不重複連 Oracle，也不新增 SQL 檔或同義表。
- Oracle source DDL 與 PostgreSQL CREATE-only 參照仍為 `database/reference/mestest_prd_parameter_setup_source_ddl.sql`、`database/reference/mestest_prd_parameter_setup_create.sql`。欄位與 PK 取自受保護連線查出的 `ALL_TAB_COLUMNS`、constraints/index dictionary；`DBMS_METADATA.GET_DDL` 曾回 SocketException。未查業務列、未執行 Oracle DDL/DML，profile 內 IP/帳密/SID 未輸出或提交。
- 家用端實作前仍需比對 YMES PostgreSQL live schema/migrations，只補現況確實缺漏的 migration；CREATE-only 檔不可直接在環境執行。

### 更新 2026-11-18：PRD PD102 OPDescription Oracle 字典查詢

- 公司電腦以 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 讀取本機受保護的既有 Oracle profile，唯讀直連 MESTEST。直接取得 `DBMS_METADATA.GET_DDL('TABLE','TBLPRDOPDESCRIPTION','MESTEST')`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`、`ALL_TRIGGERS`。
- 結構為 9 欄，PK `(PRODUCTNO,PRODUCTVERSION,OPNO)`；只有對應 unique PK index，沒有 FK、Trigger 或其他 index。Oracle source DDL 與 PostgreSQL CREATE-only 參照分別為 `database/reference/mestest_prd_op_description_source_ddl.sql`、`mestest_prd_op_description_create.sql`。
- 只查資料字典與 DDL，沒有讀業務資料列、沒有在 Oracle 執行 DDL/DML。連線 profile 的 IP/帳號/密碼/SID 未輸出或提交；家用端仍須先核對 YMES live schema/migrations，CREATE-only 參照不可直接部署。

### 更新 2026-11-18：PD102 RecipeGroup 指派 Oracle metadata

- 公司端以 Windows PowerShell 5.1、Oracle.ManagedDataAccess 與本機受保護 MESTEST profile 唯讀查 `TBLPRDRECIPE`、`TBLPRDRECIPEBASIS` 的 `DBMS_METADATA.GET_DDL`、欄位、約束、index、trigger dictionary。沒有讀 MES 業務列，沒有執行 Oracle DDL/DML。
- `TBLPRDRECIPE` 複合 PK `(PRODUCTNO,PRODUCTVERSION,RECIPEGROUP)`；`TBLPRDRECIPEBASIS` 複合 PK `(RECIPEGROUP,RECIPEVERSION)`。兩表只有 PK unique index，沒有 FK、trigger 或其他 index。DDL/CREATE-only 路徑見 `database/reference/mestest_prd_recipe_group_source_ddl.sql`、`mestest_prd_recipe_group_create.sql`。
- 連線 IP/帳號/密碼/SID 未輸出或提交。家用端先比對 YMES live schema/migrations；lookup basis 是其他 Recipe 主檔共用表，不要重複建立。

### 更新 2026-11-18：PD102 Product Accessory Oracle metadata

- 公司電腦以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護 MESTEST profile 唯讀取得 `TBLPRDACCESSORY` 的 `DBMS_METADATA.GET_DDL` 與欄位、constraint、index、trigger dictionary。
- `TBLPRDACCESSORY` 六欄，複合 PK `(PRODUCTNO,PRODUCTVERSION,OPNO,ACCESSORYTYPE,ACCESSORYNO)`；`OPNO` default `'*'`；只有 PK unique index，無 FK、trigger 或其他 index。Oracle source DDL 與 PostgreSQL CREATE-only 參照見 `database/reference/mestest_prd_accessory_source_ddl.sql`、`mestest_prd_accessory_create.sql`。
- 只查資料字典/DDL，沒有讀業務資料、沒有執行 Oracle DDL/DML。連線 IP/帳號/密碼/SID 未輸出或提交。家用端需先比對 YMES live schema/migrations，確認缺表才建立 migration。

### 更新 2026-11-18：PD102 Product OP Area 沿用既有 Oracle metadata

- Resource 子功能重用先前已取得的 TBLPRDOPAREA、TBLOPAREA、TBLSMDAREABASIS 字典與 DDL；本次未重新連 Oracle、未查業務資料列、未執行 DDL/DML。
- TBLPRDOPAREA 的來源 DDL/PG CREATE 參照在 database/reference/mestest_prd_parameter_setup_source_ddl.sql、mestest_prd_parameter_setup_create.sql；TBLOPAREA 在 mestest_operation_and_system_source_ddl.sql、mestest_operation_and_system_create.sql；Area Basis 在 mestest_smd_area_basis_source_ddl.sql、mestest_smd_area_basis_create.sql。
- 本功能不新增 Oracle 表或 CREATE SQL。Oracle profile 的 IP、帳號、密碼、SID 仍只存本機受保護設定，不寫入規格或 Git。

### 更新 2026-11-18：PRD PD102 Product SubOP Oracle metadata

- 公司電腦使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，讀取本機受保護的既有 Oracle profile，唯讀直連 MESTEST；取得 `TBLPRDOPSUBOP` 的 `DBMS_METADATA.GET_DDL` 與欄位、constraints、indexes dictionary。
- 表 PK 為 `(PRODUCTNO, PRODUCTVERSION, OPNO, SUBOPSEQUENCE)`；6 個欄位 NOT NULL；只有 PK unique index，沒有 FK、trigger 或其他 index。Oracle source DDL 與 PostgreSQL CREATE-only 參照分別在 `database/reference/mestest_prd_subop_source_ddl.sql`、`mestest_prd_subop_create.sql`。
- 僅讀結構 metadata，未讀 MES 業務資料列、未在 Oracle 執行 DDL/DML；Oracle IP、帳號、密碼、SID 未輸出或提交。家用端必須先核對 YMES live schema/migrations，不能直接部署 CREATE-only 參照。

### 更新 2026-11-18：PRD PD102 Product Program Oracle metadata

- 公司電腦使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，讀取本機受保護既有 Oracle profile，唯讀直連 MESTEST；取得 `TBLPRDPROGRAMBASIS`、`TBLPRDPRODUCTPROGRAMS` 的 `DBMS_METADATA.GET_DDL` 與欄位、constraint、index、trigger dictionary。
- Program Basis PK 為 `PROGRAMNO`；Product assignment PK 為 `(PRODUCTNO, PRODUCTVERSION, PROGRAMNO)`。各表只有 PK unique index，沒有 FK、trigger 或其他 index。Oracle source DDL 與 PostgreSQL CREATE-only 參照分別為 `database/reference/mestest_prd_program_source_ddl.sql`、`mestest_prd_program_create.sql`。
- 僅讀結構 metadata，未查 MES 業務資料列、未在 Oracle 執行 DDL/DML；Oracle IP、帳號、密碼、SID 未輸出或提交。家用端先核對 live schema/migrations，尤其 Program Basis 已出現在 first-phase bootstrap，不可重複建立。

### 更新 2026-11-18：PRD PD102 Default APS Product Oracle metadata

- 公司端使用 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，由本機受保護設定的 `DatabaseConfigs.CS.Test.Mes.ConnectionString` 直連 MESTEST；`DBMS_METADATA.GET_DDL` 回 ORA-03135，改以 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES` 唯讀重建 `TBLPRDPRSOPAPSSETUP`、`TBLPRDPRSOPAPSSETUPDETAIL` 的邏輯 DDL。
- 兩表只有 PK unique index，無 FK、trigger、額外 index；完整欄位與 DDL 來源見 `database/reference/mestest_prd_default_aps_source_ddl.sql`，PostgreSQL CREATE-only 參照見 `mestest_prd_default_aps_create.sql`。流程端 `TBLPRSOPAPSBASE/DETAIL` 沿用 PS104 已查來源 DDL。
- 未查 MES 業務資料列，也未執行 Oracle DDL/DML。IP、帳號、密碼、SID 不寫入 Git；桌電若沒有本機受保護連線 profile，需透過安全方式重新提供，不可改用 DBeaver 螢幕抄 DDL。


## 家用端持續建置紀錄（2026-11-18）

- QC503 核對既有 migration0027 Driver 欄位、PK/default/nullable，無須另建表。正式 UI 的測試 Driver A/B 及 QC501 引用類別均已刪除，精確 SQL 計數為 0。
- PS102 live audit 確認缺表；備份 /opt/ymes/backups/ymes_before_0028_ps102_20261118.dump 通過 pg_restore --list 後，套用 migration0028，保留原 PK 與 Sequence 全表唯一、nullable/default，授予 ymes_app 表級 CRUD。未新增來源以外 FK/index 或搬移業務資料。正式 UI 測試 Stage A/B 已清空，grant 查核為 true。

### 更新 2026-11-18：PRD PD102 RunTime Oracle metadata

- 公司桌機以 Windows PowerShell 5.1 + ODP.NET 經本機 DPAPI profile 驗證 MESTEST 登入。唯讀核對 `TBLPRDRUNTIMESETUP` 及六張共用 lookup 表的欄位、約束及索引。`GET_DDL` 回 `ORA-03135`，RunTime DDL 依資料字典重建。
- `TBLPRDRUNTIMESETUP` 有 20 欄、10 個具名 `NOT NULL` 條件，無 PK、FK、unique、索引或 trigger。DDL、欄位來源及缺口見 [產品標準工時](modules/PRD/產品標準工時.md)。查詢限 metadata。

### 更新 2026-11-18：PRD PD102 QC Item Oracle metadata

- Windows PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDQCITEM`、OP／QC 共用目錄及 BindLog 字典。新查來源有 13 欄，包含舊參照未列的 `XX_FREQ NVARCHAR2(300)`。`GET_DDL` 回 `ORA-03135`，取字典重建邏輯 DDL。詳見 [產品品管項目](modules/PRD/產品品管項目設定.md)。

### 更新 2026-11-18：PRD PD102 Packing Set Oracle metadata

- Windows PowerShell 5.1 + ODP.NET 唯讀核對七張 Packing Set 設定表，Map 的 `GET_DDL` 回 `ORA-03135`。依字典重建六個 PK 及一個獨立唯一索引，未見 FK、額外索引或 trigger。詳見 [產品包裝設定](modules/PRD/產品包裝設定.md)。

### 更新 2026-11-18：PRD PD102 EQPCapItem Oracle metadata

- Windows PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDOPCAPITEM` 和 `TBLEMSEQPCAPBASIS`。主表 `GET_DDL` 回 `ORA-03135`，兩表結構依字典重建。各表只有 PK 唯一索引。詳見 [設備能力項目規格](modules/PRD/產品設備能力項目.md)。

### 更新 2026-11-18：PRD PD102 eSOP Oracle metadata

- Windows PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDESOPBASIS` 與 SubOP lookup。`GET_DDL` 回 `ORA-03135`，主表 10 欄、五欄 PK，僅有 PK 唯一索引。[eSOP 規格](modules/PRD/產品電子作業指導書.md)


### 家用端2026-11-19：QC101/QC102/QC401/QC402/PS101

- 比對來源reference及live缺表後，已套用forward migrations0029–0033，明確授予ymes_app CRUD（PS101另SYS序號SELECT）；來源唯一鍵/nullable/default保留，無新增FK或額外index。
- 備份 `/opt/ymes/backups/ymes_before_0029_0030_20261118.dump` 及 `ymes_before_0031_0033_20261118.dump` 已由pg_restore --list驗證；正式API/Web與精確測試鍵清空SQL核對完成。當時下一批QC103/QC107/QC108六表於2026-11-19 PPK SSH只讀audit全部缺少；後續已套用0034–0037，最新狀態以上方0034–0041 checkpoint為準。

### 更新 2026-11-18：PRD PD102 EquipmentSpec Oracle metadata

- Windows PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDEQUIPMENTSPEC` 與設備參數目錄。主表 `GET_DDL` 回 `ORA-03135`，依字典重建 10 欄、四欄 PK，四個數值欄均為 `NUMBER(12,6)`。[EquipmentSpec 規格](modules/PRD/產品設備參數規格.md)


### 更新 2026-11-19：WIP WP102 生產批開立_批次

- 公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 profile 讀取 DatabaseConfigs.CS.Test.Mes.ConnectionString，唯讀直連 MESTEST，取得 TBLWIPLOTPROPERTY_TEMP、TBLWIPLOTPROPERTY 的 DBMS_METADATA.GET_DDL，並查 ALL_CONSTRAINTS、ALL_INDEXES、ALL_TRIGGERS。未查業務列、未執行 Oracle DDL/DML，未輸出連線秘密。
- TBLWIPLOTPROPERTY_TEMP 以 (LOTNO, PROPERTYNO) 為 PK，只有 PK unique index，無 FK、trigger、其他 index。欄位為 LOTNO NVARCHAR2(50)、PROPERTYNO NVARCHAR2(20)、PROPERTYVALUE NVARCHAR2(255)、LOTSERIAL NVARCHAR2(55)；前兩欄 NOT NULL。
- TBLWIPLOTPROPERTY 亦以 (LOTNO, PROPERTYNO) 為 PK，無 FK/trigger/額外 index；其 Oracle DDL 已由 WP101 保存，WP102 不重複建立。
- Oracle source DDL：database/reference/mestest_wip_wp102_source_ddl.sql；PostgreSQL CREATE-only 僅含 TEMP：database/reference/mestest_wip_wp102_create.sql。家用端先核對 live schema/migrations，缺表才新增 migration。

### 更新 2026-11-19：WIP WP201 生產批執行

- 依 FunctionNo EXECUTE LOT 的 THEIL Form、標準 Service/Core、THEIL 分支追查執行／TriggerOP／TransferLot 依賴；Windows PowerShell 5.1 + ODP.NET 使用本機受保護 Oracle 設定唯讀取得 39 張 MESTEST 表 DDL 與 ALL_TAB_COLUMNS 欄位 metadata。欄位數逐表與 PostgreSQL CREATE-only 參考吻合。
- TBLWIPECNLOTBASIS、VIEW_TBLWIPPACK_LOT 在 MESTEST/PUBLIC 的 ALL_OBJECTS/ALL_SYNONYMS 均不存在；不生成猜測 DDL。完整來源與限制見 database/reference/mestest_wip_wp201_*.sql 及 docs/modules/WIP/生產批執行.md。
- 只查資料字典與 DDL，未讀業務列、未執行 Oracle DDL/DML、未輸出或提交連線秘密。未連 YMES PostgreSQL。

### 更新 2026-11-19：WIP WP202 生產批執行_元件

- WP202 的 Component JOIN 查詢使用 `TBLWIPLOTSTATE`、`TBLWIPCOMPONENTSTATE`、`TBLWIPLOTBASIS`；三表 Oracle DDL／欄位／索引與 PostgreSQL CREATE-only 已由 WP101 唯讀查證並保存於 `database/reference/mestest_wip_wp101_source_ddl.sql`、`mestest_wip_wp101_create.sql`。
- 本次對照 WinForms／Service／Core 後確認沒有 WP202 專屬表，故不重複直連或產出 CREATE SQL。未讀業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，連線秘密未輸出或提交。API/schema handoff 見 `docs/modules/WIP/生產批執行_元件.md` 與 `todolist_homepc.md`。

### 更新 2026-11-19：WIP WP204 生產批執行_設備

- 使用既有受保護 Oracle profile，以 Windows PowerShell 5.1 + STD/MES_S_DLL/Oracle.ManagedDataAccess.dll 直連 MESTEST，唯讀取得 TBLEQPFAVORITE、TBLWIPLOTGPEQUIPMENT、TBLPRDSTAGESTEPSET、TBLPRDSTAGESTEPSETDETAIL 的 DBMS_METADATA.GET_DDL 與該批表的索引 DDL。
- 僅查 metadata／DDL，未讀業務資料列、未執行 Oracle DDL/DML，未輸出連線字串或憑證。來源與 CREATE-only 參考為 database/reference/mestest_wip_wp204_source_ddl.sql、mestest_wip_wp204_create.sql；家用端須先核 PostgreSQL live schema／migrations。

### 更新 2026-11-20：WIP WP203 過帳設備設定

- 依 `frmEquipmentFavorite`/`frmEquipmentFavoriteDef`、`wsEQP.LoadEQPFavorite*`、`AddEQPFavorite`/`DelEQPFavorite` 與 `clsEQPBasis` 查證，本功能只讀寫 `TBLEQPFAVORITE`，設備候選沿用已查證 Equipment Type/Basis 表。沒有新 Oracle table，未另行連 Oracle 或重複產生 CREATE SQL。
- `TBLEQPFAVORITE` Oracle DDL、metadata 與 PostgreSQL CREATE-only 已由 WP204 於 2026-11-19 查得，保存在 `database/reference/mestest_wip_wp204_source_ddl.sql`、`mestest_wip_wp204_create.sql`。來源沒有 PK/index/FK/trigger；不得建立猜測唯一約束。公司端未連 YMES PostgreSQL，家用端需先核 live schema/migration。
- WinForms HostId 取自客戶端 ComputerName。Web API 必須以可信登入 session/伺服器端 workstation mapping 解析 HostId，不得信任瀏覽器 query/body；完整交易與 DTO 約束見 `docs/modules/WIP/過帳設備設定.md`。

### 更新 2026-11-19：WIP WP605 成品庫帳務維護

- 公司端以 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，使用本機受保護的 `DatabaseConfigs.CS.Test.Mes.ConnectionString` 直連 MESTEST，只讀取得 `TBLINVFGDINVENTORYCOMPONENT`、`TBLINVFGDINBASIS`、`TBLINVFGDINDETAIL`、`TBLINVFGDINCOMPONENT`、`TBLINVTRANSACTIONSTATE`、`TBLINVTRANSACTIONLOG` 的 DBMS_METADATA DDL、ALL_INDEXES、ALL_CONSTRAINTS、ALL_TRIGGERS。
- 六張表均只有各自 PK unique index；沒有 FK、額外 index 或 trigger。Oracle 查無 `TBLINVFGDINVENTORYBIN`；WinForms Bin 明細使用 `TBLWIPGOODBINSTATE` 加 `TBLSYSBIN.BINORDER` 動態建欄。
- Oracle source DDL 與 PostgreSQL CREATE-only 參考為 `database/reference/mestest_wip_wp605_source_ddl.sql`、`mestest_wip_wp605_create.sql`。僅查資料字典與 DDL，未查 MES 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，也未輸出或提交 Oracle 憑證。
- FGD In Source=30 的服務先建立 TransactionState 並把 Basis 設為 1，同步執行 ExeFGDIn；成功設為 2 並搬移至 TransactionLog，失敗設為 3。此流程依 WinForms／Service／Core 原始碼查證，不由 Oracle metadata 推測。完整來源與 API 限制見 [WP605 規格](modules/WIP/成品庫帳務維護.md)。

### 更新 2026-11-19：WIP WP603 在製品庫帳務維護

- 公司端以 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，從本機受保護 profile 直連 MESTEST，只讀取得 WP603 九張功能表的 DDL、索引、constraints 與 triggers；未查業務資料列、未執行 Oracle DDL/DML，也未連 YMES PostgreSQL。
- 九表為 `TBLINVWIPINVENTORY_COMPONENT`、`TBLINVWIP_COMPONENTSCRAP`、`TBLINVWIP_FAILBIN`、`TBLINVWIP_INCOMING`、`TBLWIPCONT_ERROR`、`TBLINVSCRINBASIS`、`TBLINVSCRINDETAIL`、`TBLINVSCRINCOMPONENT`、`TBLINVSCRINFAILBIN`。來源欄位、PK 及五個額外索引見 `database/reference/mestest_wip_wp603_source_ddl.sql`；空表 PostgreSQL CREATE-only 參考見 `database/reference/mestest_wip_wp603_create.sql`。
- 六張表只有 PK 索引；`TBLWIPCONT_ERROR` 無 PK、另有兩個非唯一索引；Component WIP Inventory 有獨立 unique index；Component Scrap 與 Fail Bin 各有 BaseLotNo 非唯一索引。九表無 FK／trigger。獨立索引 `PK_TBLWIPINV_COMPONENT` 的 GET_DDL 回 ORA-31603，以 ALL_INDEXES／ALL_IND_COLUMNS 重建欄位序。
- WP603 共用 Form、Designer、Service/Core 有讀取；THEIL 專用 Form 原始碼缺於目前 checkout，故客製差異未宣稱已核實。CREATE-only 不是 migration；家用端需先核對 live schema，沿用 WP101／WP201 共用 Lot/WIP 表。

### 更新 2026-11-19：WIP WP604 半成品庫帳務維護

- 公司端以 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，從本機受保護設定直連 MESTEST，只讀取得 `TBLINVSEMIINBASIS`、`TBLINVSEMIINDETAIL`、`TBLINVSEMIINCOMPONENT`、`TBLINVSEMIINVENTORYCOMPONENT` 的 DBMS_METADATA DDL 與 ALL_INDEXES／ALL_CONSTRAINTS／ALL_TRIGGERS metadata。
- 四表均只有各自 PK unique index，沒有 FK、額外 index 或 trigger。`TBLINVSEMIINVENTORYCOMPONENT` 未包含於既有 WP201 additional reference，本次補查；`TBLINVSEMIINVENTORY` 與 `TBLINVTRANSACTIONSTATE/LOG` 沿用既有 WP201／WP605 reference，不重複建立。
- Oracle source DDL、PostgreSQL CREATE-only 參考為 `database/reference/mestest_wip_wp604_source_ddl.sql`、`mestest_wip_wp604_create.sql`。CREATE-only 不是 migration；家用端須先核對 YMES live schema 與 migrations。
- 僅查 metadata／DDL，未讀 MES 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未輸出或提交連線秘密。Basis Source `0=手動外購、1=From WIPInventory_SEMI` 與交易引擎 Source `20=SEMI In` 由 Core／Service 原始碼確認，不是從 DDL 推斷。

### 更新 2026-11-19：WIP WP601 包裝作業 Oracle metadata

- 公司端以 Windows PowerShell 5.1 + `STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`，讀取本機受保護 `DatabaseConfigs.CS.Test.Mes.ConnectionString` 後唯讀直連 MESTEST，取得 WP601 19 張功能表的 DBMS_METADATA DDL 與 ALL_TAB_COLUMNS／ALL_CONSTRAINTS／ALL_INDEXES／ALL_TRIGGERS metadata。19 張 CREATE 欄位數均與 MESTEST dictionary 相符；未讀業務資料列、未執行 Oracle DDL/DML。
- `TBLWIPMOLINEPACKL` 通用前綴不存在於 MESTEST ALL_OBJECTS／ALL_SYNONYMS；實體為 L1–L6 與各層 Detail。`TBLWIPMOLINEPACKRELATION` 只有兩個非唯一索引、無 PK；`TBLINVPACKMERGELOG` 無 PK／索引。來源 DDL、CREATE-only 與 metadata 分別在 `database/reference/mestest_wip_wp601_source_ddl.sql`、`mestest_wip_wp601_create.sql`、`mestest_wip_wp601_metadata.txt` 及 supplemental metadata。
- CREATE-only 只供家用端比對 YMES live schema/migrations，不能整批執行；沿用已共用的 WIP／MO／Product／Packing Rule/Label Rule 表。Oracle IP、帳號、密碼及 SID 未輸出或提交；公司端未連 YMES PostgreSQL。

### 更新 2026-11-19：WIP WP629 在製品庫包裝作業來源表重用

- WP629 原始碼使用 `TBLWIPLOTSTATE`、WP601 已核對的包裝層級／Detail／Relation／PackState／PackLog，以及 `TBLINVWIPINVENTORY_SEMI`。本功能沒有新增專屬 Oracle 表，因此本輪沒有重複查 DDL，也沒有另產一份重複 CREATE-only。
- `TBLINVWIPINVENTORY_SEMI` 的 Oracle source DDL 與 CREATE-only 已由 WP201 來源依賴查證保存於 `database/reference/mestest_wip_wp201_dependency_source_ddl.sql` 及 `mestest_wip_wp201_create.sql`；包裝表保存於 WP601 reference。WP629 規格記載 Status=11、InventoryNo 與 `InlinePackingFlag=1` 行為。
- 本次只重用既有 Oracle metadata／DDL 與 WinForms、Service、Core 原始碼；未重查業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未輸出或提交連線秘密。家用端仍須核對 live PostgreSQL schema/migrations，不得因 WP629 而建立重複資料表。

### 更新 2026-11-20：WIP WP301 設備上料

- 公司端使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 及本機受保護 Oracle profile 直連 MESTEST，唯讀取得四張原先未有 reference 的表 DDL 與 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`、`ALL_TRIGGERS` metadata：`TBLWIPEQPMATERIALSTATE`、`TBLWIPEQPMTLSTATE_COMP`、`TBL_BINKLESSMAPSTATE`、`TBL_BSUBSTRATEMAPSTATE`。
- 四表皆只有複合 PK unique index，沒有 FK、額外 index 或 trigger。欄位、Oracle source DDL 與 metadata 分別見 `database/reference/mestest_wip_wp301_source_ddl.sql`、`mestest_wip_wp301_metadata.txt`；家用端 PostgreSQL CREATE-only 見 `database/reference/mestest_wip_wp301_create.sql`。其他 13 張功能相依表沿用 WP101/WP201、OE、MTL、OP/SMD 既有 reference，不在此重複產生 CREATE。
- 僅查結構 metadata／DDL，未讀 MES 業務資料列、未在 Oracle 執行 DDL/DML、未連 YMES PostgreSQL，未輸出或提交連線秘密。CREATE-only 不代表 migration；家用端須先比較 YMES PostgreSQL live schema／migration history，再只為缺表新增 forward migration。

### 更新 2026-11-20：WIP WP302 載具現況管理

- 公司端使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 Oracle profile 直連 MESTEST，只讀取得 `TBLWIPCARRIERSTATEDETAIL`、`TBLWIPCARRIERSTATEDETAILLOG`、`TBLINVSTOCKERSTATE` 的 DBMS_METADATA DDL、欄位／constraint／index／trigger metadata。未查業務資料列，未執行 Oracle DDL/DML，未連 YMES PostgreSQL。
- Detail 有 11 欄、7 個 NOT NULL constraint、1 個 ComponentNo 非唯一 index，沒有 PK/FK/Trigger；DetailLog 有 18 欄、9 個 NOT NULL constraint，沒有 PK/index/FK/Trigger；StockerState 有 8 欄、三欄複合 PK unique index，沒有 FK/Trigger。Oracle source DDL、metadata 與 PostgreSQL CREATE-only 見 `database/reference/mestest_wip_wp302_*`。既有 CarrierState／CarrierType／Lot/Component references 重用，不重複 CREATE。
- 受保護 profile／PowerShell 只在公司本機使用；Oracle IP、帳號、密碼、SID 未輸出或提交。CREATE-only 不是 migration；家用端需先查 YMES PostgreSQL live schema／migration history，僅為實際缺少的物件新增 forward migration。

### 更新 2026-11-20：WIP WP308 載具管理_元件

- WP308 依 frmCarrierManagement、CarrierIn/CarrierChange、wsEQP/wsWIP/wsINV 與 clsLot 使用載具、載具狀態／明細／歷程、Lot／Component、Stocker 現況；所有資料表均已有 Carrier Basis/Type、WP302 或 WP101 Oracle source DDL、metadata 與 CREATE-only reference。
- 本功能沒有新增 Oracle 專屬資料表，故沒有重複產生 source DDL 或 CREATE-only。公司端未重查業務資料列、未執行 Oracle DDL/DML，未連 YMES PostgreSQL。家用端先對照 live schema/migration history，僅為確實缺失表新增 migration；正式放入、卸載、更換都應是登入保護的 POST command，並在交易內重驗 Carrier/Lot/Component/Stocker 與容量。

### 更新 2026-11-20：WIP WP303 設備耗品現況

- 公司端以 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 及本機受保護 Oracle profile 直連 MESTEST，只讀取得 `TBLWIPEQPCONSUMESTATUS`、`TBLWIPEQPCONSUMELOG` 的 DBMS_METADATA DDL 與 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`／`ALL_CONS_COLUMNS`、`ALL_INDEXES`／`ALL_IND_COLUMNS`、`ALL_TRIGGERS` metadata。未查業務資料列，未執行 Oracle DDL/DML。
- Status 有 9 欄、5 個 NOT NULL 欄位約束及 EquipmentNo/ConsumeNo 複合 PK；Log 有 7 欄、3 個 NOT NULL 欄位約束及 EquipmentNo/ConsumeNo/ConsumeSeq 複合 PK。兩表均無 FK、額外索引或 trigger。Oracle source DDL、metadata、PostgreSQL CREATE-only 位於 `database/reference/mestest_wip_wp303_*`。
- `TBLEQPCONSUMEBASIS` 已由設備類別資料查證並收錄於既有 `database/reference/mestest_equipment_type_*`；`TBLEQPEQUIPMENTBASIS` 沿用設備主檔 reference，不重複產生 CREATE-only。
- 僅讀結構 metadata／DDL，未輸出或提交受保護 profile、連線字串或秘密，未連 YMES PostgreSQL。CREATE-only 不是 migration；家用端需先比對 live schema／migration history，只為實際缺少的表新增 forward migration。

### 更新 2026-11-20：WIP WP304 設備耗品歷程

- WP304 僅以設備、耗品及建立時間讀取 `TBLWIPEQPCONSUMELOG`；此表已由 WP303 直連 MESTEST Oracle dictionary 取得 DDL／metadata，source、metadata 與 PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp303_*`。
- 本功能沒有新表，不重複 query Oracle metadata 或產生 CREATE-only；設備及耗品 lookup 沿用既有 WP303／設備主檔 references。本頁未查 Oracle 業務資料列、未執行 Oracle DDL/DML，亦未連 YMES PostgreSQL。
- WinForms 允許不帶條件查整張歷程；家用端 API 必須登入保護、參數化及分頁，限制單次回傳量，並核對使用者可查詢範圍。

### 更新 2026-11-20：WIP WP305 設備線上點檢

- 公司端使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 Oracle profile 直連 MESTEST，只讀取得六張表的 DBMS_METADATA DDL、ALL_TAB_COLUMNS、ALL_CONSTRAINTS/ALL_CONS_COLUMNS、ALL_INDEXES/ALL_IND_COLUMNS 及 ALL_TRIGGERS：`TBLWIPEQPQCLISTLOG`、`TBLWIPEQPQCLISTDETAIL`、`TBLWIPEQPQCTASK`、`TBLEMSEQUIPMENTSTATELOG`、`TBLWIPCONT_EQUIPMENT`、`TBLWIPLOTLOGINSTATE`。
- 六表均無外鍵或 trigger。前三表各只有 PK index；設備狀態 Log 有兩個 non-unique index；`TBLWIPCONT_EQUIPMENT` 無 PK/unique index且有兩個 non-unique index；LotLoginState 有一個額外 non-unique index。詳細欄位、constraint、index 及原始查詢輸出見 `database/reference/mestest_wip_wp305_metadata.txt`、`mestest_wip_wp305_source_ddl.sql`、`mestest_wip_wp305_source_indexes.sql`。
- PostgreSQL 空表與索引參考為 `database/reference/mestest_wip_wp305_create.sql`，包括三張 WP305 Log/Task 表與三張目前 YMES reference 未涵蓋的設備失敗／下機副作用表。設備狀態 Basis 沿用既有 equipment-state reference，`TBLWIPTEMP_EQUIPMENT` 沿用 WP201 reference。
- 原始 DDL、metadata、index 查詢均只讀結構資料，未讀 MES 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未輸出或提交受保護連線秘密。CREATE-only 不是 migration；家用端須先查 live schema／migration history，尤其不可為 `TBLWIPCONT_EQUIPMENT` 自行發明 PK。

### 更新 2026-11-20：WIP WP307 設備狀態監控_區域

- 公司端使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 Oracle profile 直連 MESTEST，只讀取得 `TBLSMDAREARELATION`、`TBLWIPAREAOPERATORSTATE`、`TBLWIPAREAOPERATORLOG` 的 DBMS_METADATA DDL 與欄位、constraint、index、trigger metadata。未查業務資料列，未執行 Oracle DDL/DML。
- `TBLSMDAREARELATION` 已有共用的 Oracle source／PostgreSQL CREATE reference `mestest_equipment_group_smd_*`，WP307 不重複產生 table CREATE。新查兩張 WIP 表：OperatorState 以 UserNo 為 PK，OperatorLog 沒有 PK／index/FK/trigger；PostgreSQL CREATE-only 只新增兩張表，不杜撰 Log key。完整 source DDL、索引、metadata、CREATE-only 位於 `database/reference/mestest_wip_wp307_*`。
- Oracle profile 僅在公司本機讀取，IP、帳號、密碼、SID 及連線字串未輸出或提交。CREATE-only 不是 migration；家用端先核對 YMES live schema/migration history，只為確實缺少的資料表建立 migration。

### 更新 2026-11-20：WIP WP103 列印流程卡

- WP103 查詢及報表讀取既有 WIP Lot Basis/State、Process Node/Relation、OP、Product、Customer、RunCard Log、`TBLPRDRUNCARDMAP`、User Defined Report 與 System Parameter。功能沒有新增專用 Oracle table；本輪沿用已存在的 schema/reference，不重複查 DDL，也未產生重複 CREATE-only。
- WinForms Source/Service/Core 證據記錄於 `docs/modules/WIP/列印流程卡.md`。家用端需先核對上述依賴在 YMES PostgreSQL live schema/API 中的實際可用性；只為實際缺少的表查來源 DDL並新增 forward migration，不能直接套用推測的 CREATE。
- 公司端僅閱讀程式碼並使用 DEV 假資料，未讀 MES 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未執行 build/test/browser；未輸出或提交連線秘密。

### 更新 2026-11-20：WIP WP113 THEIL LOT SHEET PRINT

- 公司端已找到 THEIL 專用 WinForms／Service／Core。功能重用 Lot Basis/State/Property、MO／產品／流程／OP 屬性／BOM／材料／處置／使用者資料及 `XX_TBLLOTMARITX_THEIL_BU1`。Matrix 的 Oracle source DDL、metadata、PG CREATE-only 已在 `database/reference/mestest_wip_wp201_*`，其他共用結構沿用既有 references；沒有新專用表，不重複查 DDL或建立 CREATE-only。
- 家用端先核對 YMES PostgreSQL live schema/migration history，只有缺少已由 Oracle metadata 證實的共用物件才新增 forward migration。正式 API 與 ActiveReports/PDF renderer 尚未完成，依 `docs/modules/WIP/LOT_SHEET_PRINT.md` 及 `todolist_homepc.md` 實作。
- 公司端僅使用固定假資料，不查 Oracle 業務列、不執行 Oracle DDL/DML、不連 YMES PostgreSQL；未輸出或提交受保護連線資訊。

### 更新 2026-11-20：WIP WP104 使用者登入作業（上工）

- 公司端使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護的 MESTEST 設定，唯讀呼叫 `DBMS_METADATA.GET_DDL` 並查 ALL_TAB_COLUMNS、ALL_CONSTRAINTS/ALL_CONS_COLUMNS、ALL_INDEXES/ALL_IND_COLUMNS、ALL_TRIGGERS：`TBLWIPOPERATORSTATE`、`TBLWIPOPERATORLOG`。未查 MES 業務資料列，未執行 Oracle DDL/DML。
- OperatorState 7 欄，PK `(USERNO,MULTIOPERATORMODE,LOGINPLACENO)`；OperatorLog 15 欄，PK `(SID)`。只有 PK index，無外鍵、trigger 或其他 index。Source、metadata、index 與 PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp104_*`。
- `TBLWIPLOTLOGINSTATE` 沿用 WP305 的 Oracle source DDL/metadata/CREATE；其餘 User/Shift/OP/Area/Equipment/System Parameter 等共用 references 重用。本功能未查得其他新專用資料表。
- Profile 的 IP、帳號、密碼、SID 與連線字串留在公司本機受保護設定，未輸出或提交。家用端先查 YMES PostgreSQL live schema/migration history；只為真正缺少的 OperatorState／OperatorLog 建 migration，CREATE-only 不可直接部署。

### 更新 2026-11-20：WIP WP105 使用者登出作業（下工）

- WP105 不涉及新的 Oracle table；其 `TBLWIPOPERATORSTATE`／`TBLWIPOPERATORLOG` 欄位、PK、索引及 CREATE-only 取自 WP104 references，`TBLWIPLOTLOGINSTATE` 取自 WP305 references。未為 WP105 重複查 DDL 或產生 CREATE-only。
- 本輪只核對 WinForms、Service、Core 原始碼與 DEV 假頁，沒有讀 Oracle 業務資料列、執行 Oracle DDL/DML 或連 YMES PostgreSQL。家用端先檢查 live schema/migration，再重用既有 migration/API schema；WP105 的逐筆下工需在交易內更新 OperatorLog、OperatorState 及必要 LotLoginState。

### 更新 2026-11-20：WIP Lot Operator Clockin 批次作業員上工

- 本功能沒有新資料表，不重複查 DDL或產生 CREATE-only。OperatorState／OperatorLog 沿用 WP104 的 Oracle source/metadata/CREATE-only；LotLoginState 沿用 WP305 references。LotState、OP、Area、Equipment、Equipment Group、Temporary Equipment、User/Shift、Group/Privilege、System Parameter 都是既有共用結構。
- 公司端只核對 WinForms、Service、Core 與上述已知 schema metadata，未查業務資料列、執行 Oracle DDL/DML 或連 YMES PostgreSQL。特別確認 WP104 OperatorState DDL 沒有 POSITIONNO；標準 Lot Operator Clockin Form 未傳工位，Core 走不含此欄的 insert。
- 家用端先比對 YMES live PostgreSQL schema/migrations，再重用共用 OperatorState、OperatorLog、LotLoginState 和既有 lookup。Form 未提供數值 FunctionKey；應由可靠 menu/function metadata 核實，不能把目前 fallback 推導值當成正式資料庫值。

### 更新 2026-11-20：WIP Lot Operator ClockOut 批次作業員下工

- 本功能沒有新表，OperatorState／OperatorLog 與 LotLoginState 沿用 WP104/WP305 Oracle references；查詢所需 LotState、OP、Area、Equipment、Group、Temporary Equipment 等沿用共用 references，未重複查 DDL。
- 公司端只核對 WinForms、Service、Core 原始碼與既有 schema references，未讀 Oracle 業務資料列、執行 Oracle DDL/DML 或連 YMES PostgreSQL。家用端先核對 live schema，再實作登入授權 query 與批次下工 API。

### 更新 2026-11-20：WIP 作業員登入登出（OP／Area）

- Core 寫入 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`。受保護 Oracle profile 直連 MESTEST 後，`ALL_TABLES`、`ALL_SYNONYMS` 對兩個名稱均未回傳列；DBMS_METADATA 無法取得不存在物件的 DDL。精確查詢結果是 `TABLE NOT FOUND`，不是尚未執行查詢。
- 查詢 script 與結果在 `fetch_usr_operator_login_ddl.ps1`、`lookup_usr_operator_login_owners.ps1` 及 `database/reference/mestest_usr_operator_login_*`。未查 Oracle business rows、未執行 DDL/DML；未產生 CREATE-only。需要正確有該表的 Oracle schema/環境提供 source DDL，或由家用端證實 YMES PostgreSQL 已有可重用對應結構，再決定 migration。
- IP、帳號、密碼、SID、連線字串未輸出或提交。公司端未連 YMES PostgreSQL，也未建立 Production API。

### 更新 2026-11-20：WIP 生產批作業員登入登出（OPERATOR LOGIN_LOT）

- 本功能沿用 `TBLWIPOPERATORSTATE`／`TBLWIPOPERATORLOG`（WP104）及 `TBLWIPLOTLOGINSTATE`（WP305）Oracle DDL/metadata/CREATE-only，不新增專用表或重複查 DDL。其他 Lot/OP/User 共用表沿用既有 references。
- 只核對 WinForms／Service／Core source 與已取得的結構 references；沒有讀 Oracle business rows、執行 Oracle DDL/DML、連 YMES PostgreSQL或建立 API/migration。

### 更新 2026-11-20：WIP WP205 生產批選用生產設備

- WP205 使用 Lot／Temp LotState、Temp Equipment、Equipment/EMS 狀態、Area Equipment Relation、Equipment Group 及產品 OP Capability 等既有共用表；Lot State 由 WP101／WP201 references 管理，Equipment 與 Temp Equipment references 由 WP204 管理。本功能沒有專用新表，不重複查 Oracle DDL，也不產生重複 PostgreSQL CREATE-only。
- 公司端只核對 WinForms `frmLotChoiceEqp`、`wsWIP.LoadAvailableEquipment_All/LoadTemp_Equipment/Exe_LotChoiceEQP`、`clsLot.ShowAvailableEquipment_All/Exe_LotChoiceEQP` 及 `clsOperatorState.SHR_CI_EMP`。未查業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL。
- 家用端先對照 PostgreSQL live schema／migration history，再把 LotStamp、設備可用性／容量與 Port 占用守門移入交易式 API。公司端 DEV localStorage 假指派不是正式資料；沒有更新或輸出連線秘密。

### 更新 2026-11-20：WIP WP206 計數不良原因收集

- 使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 Oracle profile 直連 MESTEST，唯讀查 `TBLOPABNORMAL`、`TBLQCABNORMALBASIS`、`TBLQCABNORMALPOSITION`、`TBLWIPCONT_ABNORMAL` 的 DBMS_METADATA DDL 與 columns/constraints/indexes/triggers metadata；未讀 business rows、未執行 Oracle DDL/DML。
- `TBLOPABNORMAL`、`TBLQCABNORMALBASIS`、`TBLQCABNORMALPOSITION` 各有來源 PK；`TBLWIPCONT_ABNORMAL` 沒有 PK/FK/Trigger，只含 `LOTSERIAL` 非唯一 index。Source DDL/index/metadata、CREATE-only 在 `database/reference/mestest_wip_wp206_*`。`TBLOPABNORMAL` CREATE-only 已在 Operation/System reference，不另重複產生。
- Oracle IP、帳號、密碼、SID 與連線字串留在公司本機受保護設定；本輪沒有輸出或提交秘密，也未連 YMES PostgreSQL。

### 更新 2026-11-20：WIP WP207 分批處理

- 使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 MESTEST profile，唯讀取得 `TBLEMSDISPATCHSTATE`、`TBLWIPAUTOMERGE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTOPECNRECORD`、`TBLWIPOSBASIS`、`TBLWIPOSDETAIL` 的 `DBMS_METADATA.GET_DDL` 與 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_TRIGGERS` metadata；沒有讀取業務資料列或執行 Oracle DDL/DML。
- 六張表都在 MESTEST，均無 FK/trigger。除 `TBLWIPCONT_EMPLOYEE` 外都有來源 PK；Employee 表無 PK/unique index，只有三個 non-unique index。原始 source/index DDL、metadata 及 PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp207_*`；完整欄位與限制見 `docs/modules/WIP/分批處理.md`。
- 其餘 Lot、Product、Process、Temp、Inventory、Matrix 等共用結構沿用 WP101／WP201／主檔 reference，不重複建表。CREATE-only 不是 migration；家用端先核對 YMES PostgreSQL live schema/history，只為缺失表新增 forward migration，並實作標準拆批與 THEIL 客製更新的交易 API。
- 本輪未連 YMES PostgreSQL、未輸出或提交 IP／帳號／密碼／SID／連線字串。`fetch_wp207_ddl.ps1` 只有受保護 profile 的本機路徑與讀取程序，不含憑證值。

### 更新 2026-11-20：WIP WP208 分批處理_元件

- 對照標準 WinForms `frmBR_LC_Split_GEN`、`WP_LotSplit_ComponentSelect`、Service `Exe_Manual_LotSplit_Component` 與 Core `BR_Manual_LotSplit_Component`。Core 方法直接出現 27 張表；其中 26 張在 WP101／WP201／WP305／WP603／WP207 及主檔／Operation/System source refs，詳 `docs/modules/WIP/分批處理_元件.md`。
- `TBLSYSPARAMETER` 原先缺 SQL reference，本輪使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll`、本機受保護 MESTEST profile 唯讀取得 `DBMS_METADATA.GET_DDL` 與 columns／constraints／indexes／triggers metadata。MESTEST 有 25 欄，PK `(PARAMETERNO)`，無 FK／trigger／非 PK index。source/index DDL、metadata、PG CREATE-only 在 `database/reference/mestest_wip_wp208_*`；未輸出或提交連線秘密。
- 元件專用的 `TBLINVWIPINVENTORY_COMPONENT` 在 `mestest_wip_wp603_*`，`TBLWIPCOMPONENTSTATE` 在 `mestest_wip_wp101_*`，ComponentProperty／Cont／Temp ComponentAttrib／Temp Equipment 在 `mestest_wip_wp201_*`，`TBLWIPCONT_EQUIPMENT` 在 `mestest_wip_wp305_*`；WP207 六張共用表已有 DDL／metadata／CREATE-only。這些不另複製 CREATE SQL。
- 未讀 Oracle 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL。家用端自行核對 live schema/history，僅為真正缺少的表建立 forward migration；不可把 source CREATE-only 當 migration 重播。

### 更新 2026-11-20：WIP WP209 分批處理_等級

- 對照標準 WinForms `frmBR_LB_Split_FT`、Bin Assign 對話框、Service `Exe_Manual_LotSplit_Bin` 與 Core `BR_Manual_LotSplit_Bin`。Core 直接引用 25 張表，23 張已有 WIP／主檔 reference。缺 reference 的兩張名稱是 `TBLWIPSPLITCONTENT_BIN` 與 `TBLWIPSORTERBINDATA`。
- 使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 及本機受保護 MESTEST profile 唯讀查 `DBMS_METADATA.GET_DDL` 和 `ALL_TAB_COLUMNS`／`ALL_CONSTRAINTS`／`ALL_INDEXES`／`ALL_TRIGGERS`。`TBLWIPSPLITCONTENT_BIN` 存在，22 欄，單欄 PK `TOLOTSERIAL`，無 FK／trigger／其他 index；source/index DDL、metadata、PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp209_*`。CREATE-only 是結構參考，家用端先核對 live schema/history 再決定是否建 forward migration。
- `TBLWIPSORTERBINDATA` 在目前連線的 `ALL_TABLES`／`ALL_SYNONYMS`／`ALL_OBJECTS` 均不可見；Core 只在 `LED_PKG` 分支使用。未猜測此表 CREATE SQL；取得正確 schema／環境或 vendor DDL 前，LED 分支不可視為正式 API 已完成。未查 Oracle 業務列、未執行 DDL/DML、未連 YMES PostgreSQL，且未輸出／提交連線秘密。

### 更新 2026-11-20：WIP WP210 分批處理_載具

- 對照 `frmBR_CL_Split_GEN`、`frmCarrierSplit_Comp`、Service `Exe_Manual_LotSplit_Carrier` 及 Core `BR_Manual_LotSplit_Carrier`／`BR_Manual_LotSplit_Carrier_Component`。兩條 Core 字面直接引用共 17 張表；逐名核對 `database/reference/` 中的來源 CREATE TABLE 結構，17 張均已有 reference。Carrier Basis／Type lookup 亦屬既有共用結構；本功能沒有新缺失的直接表，所以不重複取得 Oracle DDL 或產生 CREATE SQL。完整表名及來源行為見 `docs/modules/WIP/分批處理_載具.md`。
- 公司端只讀 WinForms／Service／Core 和既有 source references；未連 YMES PostgreSQL、未讀 Oracle 業務資料列、未執行 Oracle DDL/DML。家用端仍須核對 PostgreSQL live schema／migration history，追查 Core helper 的其他間接表，並以真實載具容量、位置與元件現況建交易式 API；不可從已有 reference 推斷 live schema 必然完整。

### 更新 2026-11-20：WIP WP211 併批處理

- 對照 `frmBR_L_Merge_GEN`、Service `Exe_Manual_LotMerge` 與 Core `BR_Manual_LotMerge`。Core 字面直接引用八張表：`TBLEMSDISPATCHSTATE`、`TBLINVWIPINVENTORY_SEMI`、`TBLWIPAUTOMERGE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLWIPOSBASIS`、`TBLWIPOSDETAIL`。逐名核對 `database/reference/` 均已有 Oracle source DDL 與 PostgreSQL CREATE-only reference；本功能不新增重複 CREATE SQL。詳 `docs/modules/WIP/併批處理.md`。
- 公司端未查 Oracle 業務資料、未執行 Oracle DDL/DML、未連 YMES PostgreSQL；家用端先核 live schema／migration history，再追 Core helper 的間接表、實作具交易性與冪等守門的正式 API。已有 CREATE-only reference 不能當作 live schema 已完成或可直接重播 migration。

## ET105程式碼同步紀錄（2026-11-20；main仍待整合）

code `ab0f8269e9778d88811947512e6753e51437703d`已正常推送`refs/heads/codex/homepc-et105-20261120`並直接讀回相同；SQL1／API154／Web224共379份來源綁定通過，receipt SHA `2ac94056c8c82c76eedf698e64fcaa242f388a66f74a0777db1522c9c3690bae`，未允許差異0。後續純文件狀態提交不改本release code或重建產物。

QA完成後已fetch公司端`095b3d267bc102687c32b25c9a3f593cf1ebfda9`，九個新WIP頁及CREATE交接另行整合／建置／驗收。本次224來源保持，mainSyncPending=true；公司端與家用端歷史均保留，沒有force push。完整checkpoint以Root後續實際套用收據為準。

### 更新 2026-11-20：WIP WP212 併批處理_元件

- 對照 `frmBR_LC_Merge_GEN`、Service `Exe_Manual_LotMerge_Component`、Core `BR_Manual_LotMerge_Component`。Core 直接引用十張表：`TBLEMSDISPATCHSTATE`、`TBLINVWIPINVENTORY_COMPONENT`、`TBLINVWIPINVENTORY_SEMI`、`TBLWIPAUTOMERGE`、`TBLWIPCARRIERSTATE`、`TBLWIPCARRIERSTATEDETAIL`、`TBLWIPCOMPONENTSTATE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`。逐名核對 `database/reference/`，每張已有 Oracle source DDL 和 PostgreSQL CREATE-only；不重複取 DDL 或產生 CREATE SQL。完整來源與載具檢查缺陷見 `docs/modules/WIP/併批處理_元件.md`。
- 公司端未查 Oracle 業務列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL。家用端須核 live schema/migration history，追 helper/User Define hook 間接表，並將所有來源 Lot／元件／載具／Inventory 放在同一交易守門；reference 不能當成已建表證據。

### 更新 2026-11-20：WIP WP213 併批處理_等級

- 對照 `frmBR_LB_Merge_FT`、Service `Exe_Manual_LotMerge_Bin`、Core `BR_Manual_LotMerge_Bin`。Core 字面直接引用八表：`TBLEMSDISPATCHSTATE`、`TBLINVWIPINVENTORY_SEMI`、`TBLSYSBIN`、`TBLWIPAUTOMERGE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLWIPMERGECONTENT_BIN`。前七張沿用 WP101／WP201／WP207／PRD Bin refs；最後一張原缺 SQL reference。
- 使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 MESTEST profile 唯讀 `DBMS_METADATA.GET_DDL` 及 `ALL_TAB_COLUMNS`／`ALL_CONSTRAINTS`／`ALL_INDEXES`／`ALL_TRIGGERS`。`TBLWIPMERGECONTENT_BIN` 有 22 欄、單欄 PK `FROMLOTSERIAL`，無 FK／trigger／其他 index。Oracle source/index DDL、metadata、PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp213_*`；CREATE 欄位與 Oracle metadata 已核 22/22。未查業務列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL或輸出憑證。家用端先核 live schema/history 再決定 forward migration，不直接重播 CREATE-only。

### 更新 2026-11-20：WIP WP214 併批處理_載具

- 對照 WinForms `frmBR_CL_Merge_GEN`／`frmCarrierMerge_Comp`、Service `Exe_Manual_LotMerge_Carrier`、Core `BR_Manual_LotMerge_Carrier`。Core 字面直接引用 11 張表：`TBLEMSDISPATCHSTATE`、`TBLWIPAUTOMERGE`、`TBLWIPCARRIERSTATE`、`TBLWIPCARRIERSTATEDETAIL`、`TBLWIPCARRIERSTATEDETAILLOG`、`TBLWIPCOMPONENTSTATE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTBASIS`、`TBLWIPLOTSTATE`、`TBLWIPOSBASIS`、`TBLWIPOSDETAIL`；逐名核對 `database/reference/` 均有 Oracle source DDL 及 PostgreSQL CREATE-only，分別沿用 WP101、WP302、WP207 與 Carrier Basis refs。`tblCarrierDetailComp` 是記憶體 DataTable，不是資料庫表。本功能沒有新缺失的直接表，故不重複取得 Oracle DDL 或建立 CREATE SQL。
- 公司端只讀原碼及現有來源 SQL，未讀 Oracle 業務資料列、未執行 DDL/DML、未連 YMES PostgreSQL、未輸出憑證。家用端仍須核 PostgreSQL live schema/history，追 Core helper 間接表，再依實際缺口建立 forward migration；既有 CREATE-only 不代表 live 表已建立。詳 `docs/modules/WIP/併批處理_載具.md`。

### 更新 2026-11-20：WIP WP215 元件分片處理

- 對照 `frmSplitComponent`、`wsWIP.InitRunSplitComponentNo`／`EXE_SplitComponent`、`clsWIP.InitRunSplitComponentNo`／`EXE_SplitComponent`。兩個 Core 方法字面直接引用 14 張實體表；12 張已有 Oracle source DDL／PostgreSQL CREATE-only，缺 `TBLWIPCOMPONENTBASIS` 與 `TBLWIPREVERSEHISTORY` reference。
- 使用 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 與本機受保護 MESTEST profile，唯讀查 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`／`ALL_CONSTRAINTS`／`ALL_INDEXES`／`ALL_TRIGGERS`。兩表均位於 MESTEST：ComponentBasis 32 欄、PK `COMPONENTNO`；ReverseHistory 11 欄、PK `(LOTNO,REVERSEID)`，另有非唯一 `LOTSERIAL` index；兩表無 FK／trigger。source DDL、index DDL、metadata、PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp215_*`。ComponentBasis 在 Core 是表存在才繼承的可選分支，家用端先核 live PG schema/history 再決定 forward migration；不要直接重播 CREATE-only。
- 未讀 Oracle 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未輸出或提交連線秘密。完整來源與 API 守門見 `docs/modules/WIP/元件分片處理.md`。

### 更新 2026-11-20：WIP WP914 生產批緊急狀態／優先權調整

- 原生 `frmWIPLot_HotLotAndPriorityChange` 的 Set 呼叫 `wsWIP.EditLotBasis_Confirm`，Core `clsLot.EditLotBasis` 只直接 UPDATE `TBLWIPLOTBASIS.HotLot/Priority`，以 `BaseLotNo` 定位。表的 Oracle source DDL 和 PostgreSQL CREATE-only 已在 `database/reference/mestest_wip_wp101_*`。查詢／lookup 所需 LotState、`TBLOEROBASIS`、`TBLSMDAREABASIS`、`TBLOPBASIS`、`TBLPRDPRODUCTBASIS` 也已有 source／CREATE 參考。本功能沒有新直接表、無新 CREATE SQL；家用端先核 live PG schema/history，追 helper 間接表與權限／版本再做正式 API。
- 公司端只讀 WinForms／Service／Core 與已交付 SQL reference，未連 Oracle 查業務資料、未執行 Oracle DDL/DML、未連 YMES PostgreSQL。原生逐列更新可能部分成功，正式整批交易／回應見 `docs/modules/WIP/生產批緊急狀態與優先權調整.md`。

### 更新 2026-11-20：WIP WP501 立即異常設定

- 公司端以 Windows PowerShell 5.1、`STD/MES_S_DLL/Oracle.ManagedDataAccess.dll` 和受保護 MESTEST profile，唯讀 `DBMS_METADATA.GET_DDL`、`ALL_TAB_COLUMNS`／`ALL_CONSTRAINTS`／`ALL_INDEXES`／`ALL_TRIGGERS`。新增查證 `TBLWIPERFCREATEDATA`（15 欄、PK RepairNo）、`TBLWIPERFHOLDREASON`（9 欄、複合 PK ERFNo+ComponentNo+HoldItemNo+AreaNo、LotSerial 非唯一 index）、`BKTBLWIPERFCREATEDATA`（15 欄、無 PK、LotNo 非唯一 index）。三表均無 trigger；Backup.HoldUser 長 50，原表長 10。Oracle source DDL／index DDL／metadata 在 `database/reference/mestest_wip_wp501_*`。
- `TBLWIPERFBASIS`、`TBLWIPERFHOLDREASONDISP`、`TBLWIPERF_ATTACHFILE` 的 Oracle source DDL 已在 `database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql`，本次補六表的 PostgreSQL CREATE-only 參考 `database/reference/mestest_wip_wp501_create.sql`。其他 Lot／QC／OCAP 共用表沿用既存 refs；正式 API 仍須追 Reverse/ReleaseLog/User Define hook 間接表。
- CREATE-only 僅供家用端比對 YMES PostgreSQL live schema/history；若已存在不得重建或覆蓋資料，缺項才做 forward migration。公司端未查 Oracle 業務列、未執行 DDL/DML、未連 PostgreSQL，連線秘密未寫入 repo。來源與交易細節見 `docs/modules/WIP/立即異常設定.md`。

### 更新 2026-11-20：WIP WP502 立即等待設定

- `frmImmediateWaitSet`／`wsWIP.Exe_Wait_Set`／`clsLot.SetLotStateToWait` 直接使用 LotBasis、LotState、LotLog、Temp_LotState、WaitBasis、USRUserBasis、ComponentState。WaitBasis Oracle source DDL 已在 `database/reference/mestest_wip_wp201_additional_source_ddl.sql`，PG CREATE-only 已在 `mestest_wip_wp201_create.sql`；其餘直接表由 WP101、WP201、QC409/首批 refs 涵蓋。沒有新直接表需 Oracle DDL 或 CREATE SQL。家用端仍先核 YMES live schema/history、資料及 helper/adapter 間接表，再做 API/必要 forward migration。
- 公司端只讀 WinForms/Service/Core 與既有 DDL，未查 Oracle 業務列、未寫 Oracle、未連 YMES PostgreSQL。原 Form 的批次包裝預檢以逗號串 LotNo，但 `CheckLotPackage` 將它當單一值；正式端要逐筆重查。完整規格見 `docs/modules/WIP/立即等待設定.md`。

### 更新 2026-11-20：WIP WP503 批次立即異常設定

- `frmImmediateHoldSet_Batch`／`frmBRERFCreate_Batch`／`wsWIP.Exe_ERF_Create_Batch`／`clsLot.UpdERF_Create_Batch` 的 `ERFBatchHoldGenType=0` 寫 `TBLWIPERFBATCHLOT`，`1` 改 `ERFType=2` 且不寫此表。BatchLot 的六欄 Oracle source DDL 已在 `database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql`；本批 `database/reference/mestest_wip_wp503_create.sql` 提供 PostgreSQL CREATE-only。其他 Lot/ERF/QC/設備查詢直接表沿用 WP101/WP201/WP305/WP501/QC refs，家用端須先核 live schema/history。
- 同一 Core 的 `ERFType=3` Future Hold 分支使用 `TBLWIPFUTUREHOLDLOG`／`TBLWIPFUTUREHOLDREASON`。公司端以既有受保護 Oracle profile、PowerShell 5.1/ODP.NET **唯讀**取得這兩表的 DDL、欄位及索引供後續 Future Hold 規格；WP503 傳入 `6` 或轉 `2`，不執行該分支，兩表不可錯列為 WP503 migration。未查業務列、未寫 Oracle、未連 YMES PostgreSQL，也未保存連線字串或密碼。完整規格見 `docs/modules/WIP/批次立即異常設定.md`。

### 更新 2026-11-20：WIP WP504 預先異常設定

- `frmFutureHoldSet`／`wsWIP.Exe_FutureHold_Set`／`clsLot.UpdFutureHold_Set` 的 Set 直接替換 `TBLWIPFUTUREHOLD`、`TBLWIPFUTUREHOLDREASON`；後續 `ERFType=3` 生效才使用 `TBLWIPFUTUREHOLDLOG`。主表 source/CREATE 已在 WP201；Reason/Log 由受保護 MESTEST 唯讀 `DBMS_METADATA`、ALL_TAB_COLUMNS/CONSTRAINTS/INDEXES/TRIGGERS 取得，原始檔 `database/reference/mestest_wip_wp504_source_ddl.sql`、`mestest_wip_wp504_source_indexes.sql`、`mestest_wip_wp504_metadata.txt`。PostgreSQL **CREATE-only** 在 `mestest_wip_wp504_create.sql`：Reason 八欄七鍵 PK；Log 十欄、無 PK、LotNo 非唯一 index。未查業務列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL；憑證仍只在受保護本機設定，不提交。家用端先核 live schema/history，缺項才 forward migration。
- 原 Form 多批包裝預檢傳逗號字串而 Service 當一個 LotNo，正式端須逐批重查。`UpdFutureHold_Set` 刪除鍵未含 FutureHoldSerial，會覆蓋同鍵其他 Serial；此語義與後續 ERFType3 Log 的 NOT NULL 欄位需在正式 API 交易設計確認。詳見 `docs/modules/WIP/預先異常設定.md`。

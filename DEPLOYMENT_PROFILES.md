# 部署設定檔

## 家用端 WP507 預先等待唯讀查詢（2026-11-28；API/Web）

- `.NET 9` API Release build／publish 與 Vue type-check／production build 成功。API 發行封裝 `/opt/ymes/releases/wp507-future-wait-20261128-a1/api.tar.gz` SHA-256 `CE6BC972461AE420EA9E7FFD5A882B04B0D3916839136F32D4FF8FDEF99CBE1B`；Web 封裝 `/opt/ymes/releases/wp507-future-wait-20261128-a1/web.tar.gz` SHA-256 `A2E19A1BF54C7B44E4D49D50B4955D163FA4DC3E938E52119918415CA3F6ED7E`。封裝不含 `.env`／DB 機密。
- API `ymes-api:current`／`ymes-api:wp507-future-wait-20261128-a1` image ID `sha256:9535786826d531a562d1ec01c2e906f48490cc62fa3e2abe1e6ba0dde4c3232f`；rollback `ymes-api:before-wp507-future-wait-20261128-a1` 保留 WP510 image `sha256:2fddd982909de247ea2203a2bcf7a081d1e5785020a778e3e91a1eb3763e4a65`。Web `ymes-web:current`／`ymes-web:wp507-future-wait-20261128-a1` image ID `sha256:be1e4469078dddf23ffde2616320c53cec6099799cdf4c5e97d356f22182301e`；rollback `ymes-web:before-wp507-future-wait-20261128-a1` 保留 WP510 image `sha256:74aa89c1d172f6716e2b5f2571cc299c2f8bca0d0495889173709736c2e1fb47`。
- 正式 API `/YMES/API/health/ready`、Web `/YMES/`、`environments.json` 回 200，匿名 WP507 API 回 401；API 與 Web 容器 healthy。app-browser Demo session 驗證空查詢、Lot/OP sentinel、Wait Time Start/End、OP lookup、零筆時分頁按鈕停用、Release disabled、Close 及重進。沒有正式業務列被新增／刪除。`ymes-master-web` 與 `yakuyama-web` 容器未重建或停止。
- 根分割區部署後仍約 1.2 GiB 可用（98% 使用）；未 prune 或清除其他專案資料。DB migration 0126 與套用前備份詳見 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)；功能契約與待辦見 [WP507 規格](docs/modules/WIP/預先等待解除.md)。

## 家用端 WP510 等待單資料 API/Web（2026-11-28）

- `.NET 9` API Release build 與 Vue production build 成功；API 出版 context SHA-256 `45831B7BC20B96AD1EC0A9F717DDE234FC4D39889DA4D93E959FEA94A2483383`。NuGet vulnerability metadata feed 無法連線時出現 `NU1900` warning，沒有編譯錯誤。
- API `ymes-api:current`／`wp510-wait-read-20261128-a1` image ID `sha256:2fddd982909de247ea2203a2bcf7a081d1e5785020a778e3e91a1eb3763e4a65`，來源 `/opt/ymes/releases/wp510-wait-read-20261128-a1/`；rollback `ymes-api:before-wp510-wait-read-20261128-a1` 保留前版 `sha256:e3d978837553ad3ddb6342fd79e19507a6e2d703ebb8d1545e92a60a1ae8a289`。
- Web a2 `ymes-web:current`／`wp510-wait-read-20261128-a2` image ID `sha256:74aa89c1d172f6716e2b5f2571cc299c2f8bca0d0495889173709736c2e1fb47`；rollback `ymes-web:before-wp510-wait-read-20261128-a2` 保留 a1 `sha256:8fc788684dd11e3433bef500bba7994b38313d66ef8e29143d2f66cc71391e4d`。a2 補回 OP No 查詢欄位；更早的 a1 回復 tag仍保留。
- API readiness=200、容器 healthy、匿名 WP510 endpoint=401；正式 `/YMES/` 及 `environments.json` 為 200，API base 為 `/YMES/API`。公開 JS `index-DdaDurbn.js`（5,113,480 bytes，SHA-256 `DF3832CA80913683A4D797F9001CBA4388C6369DBF3323B06A3C93E180C58019`）與 CSS `index-DOlIF8kx.css`（1,381,481 bytes，SHA-256 `158554EB83B1C0471A223E67A152799B7A6FE5E094EFBB7E2D701943E973952C`）MIME／大小／雜湊一致；非 root 使用者可讀。
- Web 發行封裝位於 `/opt/ymes/releases/wp510-wait-read-20261128-a2/`，SHA-256 `6D97473FC37A5A172782C5D547F5750F97170184EE6989C9735D6553652C51C4`。僅更新 YMES API／Web 容器，未動 `ymes-master-web` 或其他專案；部署時根目錄約剩 1.3 GiB、使用率約 98%，未 prune 映像或清理其他容器。正式功能／DB 回讀見 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)。

## 家用端 WP914 生產批緊急狀態／優先權 API a3（2026-11-28；API-only）

- .NET 9 Release build／publish 成功。發行封裝 SHA-256 `79AC5421B80E7340153B2FA5355E731F01D631D2D3CB5642C5943E94783FFD09`；`YMes.Api.dll` SHA-256 `417E8462F6A0F11D97EEB56B7333D3BA1D983CA15A43BE82732D87BD06AA0C24`。root-only source path `/opt/ymes/releases/wp914-hotlot-priority-api-20261128-a3/`。
- Image `ymes-api:wp914-hotlot-priority-20261128-a3`／`ymes-api:current` ID `sha256:e3d978837553ad3ddb6342fd79e19507a6e2d703ebb8d1545e92a60a1ae8a289`。Rollback `ymes-api:before-wp914-hotlot-priority-20261128-a3` 指向 a2 `sha256:14af6d35ab6a01e47607c3074ed88b1a7c3eef28a816b870a03fc6e30f4f241c`。僅替換 `ymes-api`，Web／master Web 未重建，資料庫 schema／migration 未變。
- a3 修正 LotNo／BaseLotNo／OPNo key 的大小寫保留、只拒絕重複 `(LotNo, OPNo, Status)` 組合，並拒絕 NUL 字元。Candidate readiness=200、匿名 WP914 endpoint=401 後切換；正式容器 healthy／readiness=200／匿名 endpoint=401。app-browser Demo session 驗證正式頁 guard、Lot sentinel 查詢 `[]`、RO lookup sentinel `[]`、缺少查詢條件 400。四種 lookup、三層選單入口與 Close 已於 a2 驗證；本次沒有正式 Lot 或冪等交易資料，因此未做成功 Set／正式業務寫入。

### WP914 前版 API a2（已由 a3 取代）

- 封裝 SHA-256 `566648362532D19F2DD300FD429925AE981D87EBFF902EAC6FBE5A8E874696FB`；`YMes.Api.dll` SHA-256 `A6ED176E15D19635C3992C8D6E9BA45498002FAB3C0B75D2C892E2B3CDDE4766`。Image ID `sha256:14af6d35ab6a01e47607c3074ed88b1a7c3eef28a816b870a03fc6e30f4f241c`；由 a3 rollback tag保留。
- Migration 0115 建立空冪等表及最小兩欄 UPDATE；0116 security-definer lock helper 首次 sentinel 執行發現 PL/pgSQL record 欄位衝突，0117 已修正並在 API 部署前以 ymes_app sentinel 重驗成功。正式 DB 備份／checksum／rollback 順序見 `docs/DATABASE_CONNECTION.md`。

## 家用端 WP206 計數不良原因收集 API a2（2026-11-28）

- .NET 9 Release build／publish 成功。正式 API image `ymes-api:wp206-ipqc-20261128-a2`／`ymes-api:current` ID `sha256:36d14ea0ef2d1a23e351ff6f260e543be640cc6ae5fc7117f8e1aed7b366a0e0`；發佈封裝 SHA-256 `9E729BF7BC88C0CD532A52282FB30455787D4F9B2DAF2FA51DC89AA37295E8E3`、`YMes.Api.dll` SHA-256 `381360FD3B5633F3F88CAECD33CAAC8BD833AB2306902F006839774A0968F681`。來源位於 root-only release path `/opt/ymes/releases/wp206-api-20261128-a2/`。
- 回復 tag `ymes-api:before-wp206-ipqc-20261128-a2` 指向上一個已知健康的 WP305 image `sha256:0be7a2d2fc54b22eee470143f575e9b82629b8df261df4af56476fa7cefca6dd`。a1 diagnostic image 保留為 `ymes-api:wp206-ipqc-20261128-a1`，不作回復用途。只更新 `ymes-api`；Web `sha256:40fdfefad6d13b74601e67e03727b29d39e350bdd1300daf7cb6ab852b7c4438` 與 master Web `sha256:84356fe73f73db0ab47d95dc6fdd1229ddee0cccdd43e2880b2ed1452bfac7b8` 未變。
- App-browser Demo session：readiness 200、匿名 API 401、OP lookup 回 `[]`、非法分頁 400、不存在 OP abnormal lookup 404；正式 IPQC 頁可載入並 Close 返回主選單。a1 的 READ ONLY transaction `FOR SHARE` 造成 lookup 503，a2 移除唯讀 lookup 列鎖後重驗正常。五張新資料表目前全空，未送生產 POST 或建立正式假資料，無可用列完成正向新增驗收。詳 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)。

## 家用端 WP305 設備線上點檢 API a2（2026-11-27；2026-11-28 核對）

- `.NET 9` API Release build 成功；遠端來源 `/opt/ymes/releases/wp305-api-20261127-a2/`。發佈 tar SHA-256 `D0783E1F6B837F3A09F05FD13E58DF36D9439977B7D6F18A0C23E844F75DF2A2`；`YMes.Api.dll` SHA-256 `BF09EC41E0B35BF22E58908E71FA6E3C995979869C77D754D03540986C0528AE`。
- 正式 image `ymes-api:wp305-online-qc-20261127-a2`／`ymes-api:current` ID `sha256:0be7a2d2fc54b22eee470143f575e9b82629b8df261df4af56476fa7cefca6dd`；回復 tag `ymes-api:before-wp305-online-qc-20261127-a2` 保留 a1，ID `sha256:a3b73066160c5cf0bfb73c6e9a2fb68d52a906a689dfec3a4aca182b292e6ef7`。只替換 `ymes-api`；`ymes-web` ID `sha256:40fdfefad6d13b74601e67e03727b29d39e350bdd1300daf7cb6ab852b7c4438`、`ymes-master-web` ID `sha256:84356fe73f73db0ab47d95dc6fdd1229ddee0cccdd43e2880b2ed1452bfac7b8` 保持不變。
- Readiness 200、匿名 lookup 401、API 容器 running；app-browser Demo session 驗證 equipment types／equipment 回空陣列、Wait／Recheck 不存在設備 404、invalid mode／page／pageSize 400、Check-in 缺少 Lot／LogGroup context 400、正式點檢頁唯讀且 Close 返回主選單。正式沒有有效設備／任務資料，未建立正式假資料或送出新增 POST；正向新增仍待安全測試資料／來源到齊。
- 0107 database index migration 於 2026-11-28 套用前備份 `/opt/ymes/backups/ymes-before-wp305-0107-20261128.dump`（884,831 bytes、SHA-256 `4c1c36c9a870d5d130a5d0f728a16cc2a6ab5cc3e1a4388e38b3585ad9c9aaee`；`pg_restore --list` 2,441 entries）。根分割區約剩 1.5 GiB（98% 使用）；未 prune 映像或清理其他專案。詳見 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)。

## 家用端 WP303／WP304 API-only release a1（2026-11-28）

- 來源 commit `147f83a2177319d1adc69835850d50e777ce90f8`；release 封裝 SHA-256 `98EC090D5CA961319B8C44E47CC857AE3A734175E3285F2FF45C0B7C24A84C50`，`YMes.Api.dll` SHA-256 `ACEE4F2B3B44C06B93B7E4591387437F78F3F0F31A273B6075594B996F601A7C`。
- 正式 `ymes-api:current` image `sha256:60212b445a6d0043d20b9a51a65d3990290067a413322f7676ad769b4abb9305`；回復 tag `ymes-api:before-wp303-wp304-api-20261128-a1` 指向 `sha256:ef6c55545894a2ae7d9c96adf5e16e3ba61f5bca39246da2bdcba35975b1cc51`。此次僅替換 `ymes-api`，未重建 Web、master Web 或其他專案容器。
- Readiness 200、匿名 API 401、正式容器 healthy。app-browser 驗證 WP303 status／equipment lookup、WP304 日期查詢、WP306 日期查詢均回 `[]`；WP307 合成不存在 AreaNo 的 filter-options 回 404。沒有正式測試資料，因此未宣稱有資料 CRUD／點檢／人員操作已驗收。WP303／WP304／WP306／WP307 Production UI 由公司端待辦接線。
- 部署前正式 DB 備份、0104 migration checksum、最小 ACL、root 磁碟 journal 空間處理與完整影像雜湊見 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)。

## 家用端 WP803 結案 API a2 發布（2026-11-27；API-only）

- 來源基底 `b64c95f`，加入 WP803 結案 Controller／Repository、0088 schema migrations 及 D1 成員驗證；本機 `.NET 9` Release build 成功。NuGet 弱點資料來源連線警告 `NU1900` 不影響編譯。
- 私有封裝 `.codex-build/ymes-api-wp803-api-20261127-a2.tar.gz` SHA-256 `4F7DC316F50C8B964059DDCC991BBF989CE7CA8D4053AD373EF206F1F6D033AA`；封裝內 `YMes.Api.dll` SHA-256 `43750D84B27DA90F97739A343D1BD213B59AFE9CFBC7BC4D9949E563098B608F`。不含 `.env`／DB 機密；遠端來源為 `/opt/ymes/releases/wp803-api-20261127-a2/`。
- 正式映像 `ymes-api:wp803-close-20261127-a2`，image ID `sha256:d6d40bab48737f28d76c40a634ef61ec836caddf6d0a24d803134c014e73b4fc`；回復標籤 `ymes-api:before-wp803-close-20261127-a2` 指向 a1 `sha256:2ad40652f14b8848c339b0399101531ee7a3dfc302d0ed333e3f045c18622357`。更新 Compose 僅替換 `ymes-api`；`ymes-web`、`ymes-master-web` 及其他專案不變。
- 部署 readiness 200、匿名 API 401。app-browser Demo session 確認正式清單 API 回 0 筆、不存在 QRS 明細及 D1 團隊查找回 404。四張 WP803 表均為空，沒有正式測試列，故不宣稱編輯正向操作已驗；WP803 正式 GUI 維持 F1 停用。Approve API 固定 503 且無資料寫入，直到公司端提供 Issue Job Map／callback 來源。
- 前置 migration 0088 只建立四張空表並授予最小讀取／Release 六欄更新權限，完整備份及 checksum 記於 [資料庫連線紀錄](docs/DATABASE_CONNECTION.md)。保留遠端部署 a1 與資料庫備份以供分別回復；程式回復不會回復資料庫。

## 家用端 SC215 正式發行（2026-11-23；空資料環境操作驗收）

來源基底 `b36f99b`；API 映像 `sha256:04eb130a6ade95c31d519a8711a55fbb3e2db4a5a6d460b37c8d8b9b6f046af0`，Web 映像 `sha256:23e3fa00665cee0dde3697ec60447e2d42afa64514900a2b04fafe6938a16b42`。使用獨立 .NET 9 API/Web 預建映像部署；API、Web 分容器維持 `/YMES/API` 與 `/YMES/` 路由，沒有資料庫 migration／DDL。

部署前回復標籤：`ymes-api:before-sc215-20261123-a1`（`sha256:80a35f68286527282c92efbd966542973bb06365b4902f189e415d6ecf5b7bc8`）、`ymes-web:before-sc215-20261123-a1`（`sha256:d262d8d1f97fd9cfe378ed35815d0757d55293fc9bff296dcfe255cd57aad76f`）。發行封裝 `/opt/ymes/releases/sc215-20261123-a1/sc215-release.tar.gz` SHA256 `37ADD6948CCFB9B308AAEEAACE79C85A75C8FD3B746FF1C19AAC39FAEAD8F917`；沒有打包 `.env` 或資料庫機密。

正式站由原 Compose 專案 `ymes` 更新且只重建 `ymes-api`、`ymes-web`；兩者 healthy，API readiness `ready`、Web／公開 `/YMES/SPC/ChartLineEditLog` 回 200。`ymes-master-web` 未變更且持續運行。app-browser 以 Demo 登入，從「品質管理 → SPC分析 → 管制線修改紀錄」驗證查詢、無效日期、366 天上限、關閉與三層選單重開；console 無錯誤。正式 `TBLSPCQCCHARTBASIS` 為空表，故只驗證零結果與輸入錯誤，尚未驗證有資料結果、日期端點或分頁；沒有建立測試資料。

如需回復，先核對以上 `before-sc215` 標籤，再於 `/opt/ymes/source/deploy` 使用 Compose 專案 `ymes` 只切回 `ymes-api`／`ymes-web`；不要回退或重建資料庫，也不要影響 `ymes-master-web` 或其他專案。回復程式版本不會回復資料庫。

## 家用端 API/Web 正式站部署（2026-11-21；GUI驗收待補）

以 GitHub `main` 的公司端版本 `262613e13069843c9c44d05f870a46d2a234508c` 為基底，先修正 `deploy/Dockerfile.web.prebuilt` 對 root-only 封裝檔案的讀取權限，部署來源 commit 為 `09bd776a259bafeeb2fec28d393f8af5149392e8`。API `sha256:80a35f68286527282c92efbd966542973bb06365b4902f189e415d6ecf5b7bc8`、Web `sha256:d262d8d1f97fd9cfe378ed35815d0757d55293fc9bff296dcfe255cd57aad76f` 分開建置及切換；保留 `ymes-api:rollback-before-09bd776-a2` 與 `ymes-web:rollback-before-09bd776-a2`。

部署期間公司端另提交 `bc61853`（WP621 E10ICD 入庫 DEV UI／規格）；此較新頁面未包含在本次 runtime image，已正常合併至家用工作分支並列入接手待辦。

候選 API readiness 200、候選 Web 七個靜態檔案雜湊／大小驗證通過；正式 API/Web 容器 healthy，`/YMES/`、`/YMES/API/health/ready` 為 200，公開 JS/CSS 的 MIME、大小及 SHA 與產物相符，`environments.json` 正式 API base 為 `/YMES/API`，小寫 `/ymes/` 為 404。正式 PostgreSQL 0063/0064 結構只讀核實，未在本次部署執行 SQL。其他專案容器不變。

Chrome 已顯示正式登入頁並讀到環境選項及 Demo 按鈕；Windows UI 擷取逾時，按鈕操作回報 `coordinate input geometry is unavailable`，因此本次沒有完成 Demo 登入或模組操作。此部署不能算 GUI 全驗收；後續以 `todolist_homepc.md` 補驗。

## 公司WIP載具拆批／併批Web224 C1（2026-11-20；限定GUI完成，main待同步）

Office最新df10bd08已與已驗Web222 release3953bbad正常合併至3f99c172。224檔來源canonical `f9bb95660175064e2b0c989df9f09dbe52adf18f85c3a0650bf5753960c072e7` 已完成完整建置exit0、封裝與實際部署Web `e2df5e3df9884894f3805f5f454053c8e748c6256314ddc41f0ee4cf27988375`，公開HTTP七項通過。Root封存限定GUI73通過檢查點／73events／0observations：自行Demo登入、18native＋1direct、新兩頁guard／19入口KD／無假metadata／單一主標題／Close／重進／direct fresh、WP307 Grid30×40／四zoom、SD108空lookup及ET104新Web真CRUD均通過。2筆全GUI建刪、SQL fixture writes0、獨立Broker／Forwarder精準兩鍵皆0；fresh reload空首頁21收合選單／logs[]。release code `bacf0ee58288d1a4f1bec41a5a6bde00053d02bf` 已完成376綁定（399f648b…）與featurebranch正常推送／遠端讀回，main仍待同步。

兩新頁驗收後automatic review已允許normal main push，但Git遠端因Office最新d5a8bd13拒絕non-fast-forward；沒有force或改寫main。C1固定已驗224來源，mainSyncPending=true；Office新增元件／等級併批未合併、建置或部署本版，列後續接手，先續ET105 API。C1 feature-only checkpoint及本次狀態文件Git仍待實際紀錄，不把分支同步當main同步。

本版預定只切Web，API6d2501…／151份及PostgreSQL0058不變。新增WP210載具拆批／WP211併批入口與19入口KD樣式，正式provider仍未交付。先前main push因新兩頁未驗遭automatic review拒絕且未執行；Root依完整C1驗證再同步，不繞過。220 dc34／372及222 3953／374正常featurebranch推送已完成，保留各版86／69項proof及main待同步歷史；C1進度見[Web224 QA](docs/qa/20261120-office-wip-carrier-merge-integration.md)。

## 公司WIP拆批整合Web222（2026-11-20；限定GUI完成，main待同步）

已將Office最新12c6a2e與已正常推送的Web220 release dc34ee6合併至32d422b5。固定222檔來源canonical `ee360b1d9fd47f130df900de539402bda2a2e73a1cd5e6acdcdbca1c9f5df0d5`，完整型別／Vite建置exit0，已實際部署Web `b330b9b009670ea092218b8ecffdc6f1c389e9595a572ebf5ab471afd3e377e8`；公開七項HTTP檢查通過。API仍6d2501…／151份，資料庫仍0058，本版未重建API、未套DDL或權限。

Root已封存69個通過檢查點／70原始events，保留一次menu初始化hydration造成locator detach的harness觀察及穩定原生三層選單復驗，未改產品來源。實測新WP208／WP209與舊14個native入口的正式防護／Close、新兩頁重進與直接重新載入、無假Bin、KD單一主標題、WP307的30×40 Grid與四種縮放、direct OP入口、共用SD108空lookup／Grid／zoom及ET104新Web完整smoke。2筆資料全GUI建刪、獨立精準broker／forwarder皆0、SQL fixture writes=0；fresh reload回空首頁、21個收合選單及logs[]。SD108沒有核准Area，未驗populated layout／班別。

本版mainSyncPending=true，新Git／374綁定尚待實證；[Web222 QA](docs/qa/20261120-office-wip-split-integration.md)另記。Web220的dc34ee6／372綁定／正常featurebranch推送與私有證據已完成，保留其86項proof及當時main待同步歷史。WIP正式providers仍未提供；ET105只有隔離候選與純讀peer，未build或migration。

## 公司WIP整合Web220（2026-11-20；限定GUI完成，main待同步）

已正常合併公司b3f0f06與ET104 A1的Yb015e51，整合基底7e71e38；完整Web型別／Vite建置exit0後已部署 `823d0571d6dc82436ae5eeace7a68b8c22e8baf15dcbf0b478260d7a15224194`。固定220檔來源canonical `8071ec80f982071775cdcbd40be6d4b0509cf725e2662468bad17b19df049c46`，私有來源 `/opt/ymes/checkpoints/web-office-wip-20261120-a1` 保持700／600；公開755／644、實際nonroot讀取與正常TLS的七項HTTP檢查已通過。

本輪只切換Web。API仍 `6d2501aad008b6b5ca0c11abae0700d1f7a00b74933645c83d3a4c183dd0c171`／151份來源，資料庫仍0058，沒有API重建、資料庫結構／權限異動或其他專案服務變更。JS `index-CDsIJQNj.js`／CSS `index-DFtv264V.css` 的實際MIME、大小與SHA見[本輪QA](docs/qa/20261120-office-wip-integration.md)。

Root正式app-browser已封存86個通過檢查點／87原始events，保留1個WP104 readonly判斷的已解harness誤判；14原生＋1direct的防護／Close／重進、WP307 Grid／顏色／四種縮放及ET104新Web smoke通過。共用SD108因沒有核准Base Area，只驗picker／grid／zoom／Close，未驗populated layout。2筆smoke資料全由GUI建刪、獨立broker／forwarder皆0，freshreload logs[]；詳見本輪QA，不把不可用入口當成WIP provider已交付。

本版尚待372來源綁定及正常featurebranch推送，**main仍未同步**。Root後續另見Office dbebd0f／12c6a2e，兩版未納本次220部署／驗收；保持220來源與proof81245a0a…不動，先正常同步本版featurebranch，再正常merge公司新版並另做Web222。mainSyncPending=true，不以未推送commit或未驗新版補齊發行紀錄。

下方ET104 A1及更早段落保留各版歷史：A1已完成9cbe checkpoint及featurebranch X65f17a0／Yb015e51，不將其API169／621或GUI87重新標成本輪新Web驗收。ET105目前僅唯讀稽核，無新API／migration。

## ET104 A1 正式驗收版本（2026-11-20；分支同步後續記錄）

ET104 A1 release code `65f17a0c347acd4b5df2a1be1cad712138ececd6` 已正常推送 `refs/heads/codex/homepc-et104-20261120`，直接 `ls-remote` 讀回相同。358份來源綁定通過（SQL1／API151／Web206，未允許差異0；舊API131份僅representation、Web188份僅Git EOL差異），無force push。後續純文件狀態提交不改此release code；actual status SHA與遠端讀回另封存於checkpoint。mainSyncPending=true：公司b3f0f06的14個新WIP Vue／App／AreaShow將正常合併、另建新Web與驗收後再同步main，不能把A1的87項證據套用新產物。

API `6d2501aad008b6b5ca0c11abae0700d1f7a00b74933645c83d3a4c183dd0c171`；Web `56c31765e2af045df561c288feee07d3258501f815577fcabdf188017ff052fa`；migration0058。Web `/YMES/`、API `/YMES/API/`同server分開容器，只更新YMES自己的API／Web，保留其他專案及ymes-master-web。

固定API來源 `/opt/ymes/releases/et104-api-20261120-a1/main`151檔、canonical `434bd27f24545e452768022505cf92d21bbc7eab237bfab6816009a08fee1486`；Web私有來源 `/opt/ymes/checkpoints/web-et104-20261120-a1`206檔、canonical `aacf93de45c49d6d85b531f680bf578c9a54ecfa90c8436eee88383489df3311`。私有700／600、公開755／644，實際nonroot讀取通過；共用 `/opt/ymes/source`不作重建基底。

完整.NET9 Release及Vue型別／Vite建置通過。公開 `index-CpGBiYYp.js`3734245 bytes／`index-DUdqkPfB.css`965419 bytes，正常TLS／MIME／bytes／SHA及七項HTTP檢查通過，小寫兩路徑404。API八組169／621；GUI87項＝77GUI＋10SQL、90events及3個已解harness觀察，12筆GUI建刪、11自有scope全零；fresh reload logs空，完整範圍見[QA](docs/qa/20261120-et104.md)。

API-only回復保留已驗ET103 `12f26b18e94a85e16b4192d815f07ea2698b3ec5ddbfc21c8e589fc98a413d10`及148份原來源／archive e341220d…，保留0058及業務資料；前Web b3949d57…亦保留。A1先正常同步featurebranch `codex/homepc-et104-20261120`，actual X／Y與358 binding後補，mainSyncPending=true。已fetch公司b3f0f06新WIP碼；接續正常merge／新Web220建置與入口驗證後才同步main，不重寫A1封存來源或測試結果。

## 目前正式版本：ET103（2026-11-20，已驗收並同步 main 0c65a65）

API `12f26b18e94a85e16b4192d815f07ea2698b3ec5ddbfc21c8e589fc98a413d10`；Web `b3949d57fd39db048cfebe566287d4bbc11d47c4c3007e079960893f728ec871`；migration **0057**。Web `/YMES/`、API `/YMES/API/` 分開運行，只切換 YMES 自己的容器。下方 ET102 與更早資訊保留歷史。

固定 API來源 `/opt/ymes/releases/et103-api-20261120-a1/main` 共148檔，canonical `941fe45f980a8385f7b2ccd43ac2c8db7782fc7e3ecf4850202947afdbbe62db`；Web私有來源 `/opt/ymes/checkpoints/web-et103-20261120-a1` 共206檔，canonical `070fad5c72ed7d393280b7fbd9a686da2e344ab2f634d8ba763d732d9524123a`。來源0700／0600，公開網站0755／0644，實際nonroot可讀。

公開資產 `index-DrJWnfIC.js`（3724214 bytes）／`index-CQ_CXw4B.css`（966269 bytes）已核MIME、bytes、SHA與正常TLS。API八組126／464、GUI59項（56GUI＋3SQL）、11scope獨立全零通過；詳見 [ET103 QA](docs/qa/20261120-et103.md)。355來源綁定通過後已正常推送release code `0c65a655fae538280244289e281733f612a0eb4f`，fetch確認origin相同。後續純文件狀態提交不改產物；完成checkpoint仍需根據實際status commit與封存證據dryvalidate／apply，不由推送成功推定已寫入。

已驗證回復基底為 API `0339807d5b7e13be6efeec1ed72cdbaef71619af3f7c3fe46cd88ce9f0d87c2a`，來源 `/opt/ymes/releases/et102-api-20261120-a1/main`145檔，保留B2/B3/ET102且無Broker父writer；API回復不移除0057或業務資料，前Web `6a4f390f37b80c1579b79579dba625a702e753f11f52e71638dbbb8cdb3b3f67`亦保留。共用 `/opt/ymes/source` 不作重建來源。

## 目前正式版本：ET102（2026-11-20，已驗收並同步 main 7b6771e）

API `0339807d5b7e13be6efeec1ed72cdbaef71619af3f7c3fe46cd88ce9f0d87c2a`；Web `6a4f390f37b80c1579b79579dba625a702e753f11f52e71638dbbb8cdb3b3f67`；migration **0056**。Web `/YMES/` 與 API `/YMES/API/` 分開運行在同一 server，只更新 YMES 容器。下方 OPB03 與更早資訊是歷史，不作現在的重建來源。

固定 API 來源 `/opt/ymes/releases/et102-api-20261120-a1/main` 共 145 檔，canonical SHA256 `727efc3ce2e381e2279c0e782560cf05c1d06edc12269dded5e85dff3edc0c82`；Web `/opt/ymes/checkpoints/web-et102-20261120-a1` 共 206 檔，canonical `77bcd69d082a61dd0422efd5dc8763954b8314852bd35e687fc59b7a72cb8d26`。私有來源目錄 0700／檔案 0600，公開 Web 0755／0644，API 發布檔實際 nonroot 可讀。

本批 Web JS `index-BituN-HM.js`／CSS `index-Mgc9yX5L.css` 已通過正常 HTTPS MIME／大小／SHA 與正式 app-browser。API 八組 98 HTTP／406 斷言；GUI 53 項（52 GUI、1 SQL）及七個精確清理範圍全零。詳見 [ET102 QA](docs/qa/20261120-et102.md)。352 份來源綁定通過後已正常推送 release code `7b6771e4718a56a950f4a4de5c61564ea8ed4137`，origin/main 再 fetch 相同；後續純文件狀態提交不改產物。完成 checkpoint 仍須依實際狀態提交與證據 dryvalidate／apply，保留整份前版 metadata 與備份，不能由推送成功推定已寫入。

已驗證回復映像是前一版 OPB03 API `2a2291cc11630c3ff151f0821fe5ef4a789e63cf1933c319069da379765b1b2f`，來源 `/opt/ymes/releases/opb03-api-20261120-a1/main`（142 檔），保留 OPB02／OPB03 引用防護且不提供 Customer 父 writer；回復 API 時保留 Contact 表與業務資料。前 Web `fc83e8bc548411500ba11d32da94d20fd0d8b1921d0a7f5cd912d38f7e75f9c9` 亦保留。共用 `/opt/ymes/source` 不作重建基底。

## 目前正式版本：OPB03（2026-11-20，已驗收並同步 main caae71d）

API `2a2291cc11630c3ff151f0821fe5ef4a789e63cf1933c319069da379765b1b2f`；Web `fc83e8bc548411500ba11d32da94d20fd0d8b1921d0a7f5cd912d38f7e75f9c9`；migration **0055**。Web `/YMES/`、API `/YMES/API/` 仍分開運行；`/ymes` 與 `/ymes/` 實際回 404。只更新 YMES API／Web，沒有清理其他專案或 orphan。

可重建 API 來源在 `/opt/ymes/releases/opb03-api-20261120-a1/main`（142 檔）；相容回復來源在同目錄的 `compat`（139 檔），映像 `aab09e841b7b8dd47ed60152aa16edd14fd77ac15631111c25149e34428b4398`，保留兩種 OP 引用防護。不要在有 OPB03 資料時直接退回缺少此防護的舊 API。可重建 Web 來源在 `/opt/ymes/checkpoints/web-opb03-20261120-a2`（206 檔），canonical SHA256 `619f05129728123873253a4e25aacf4234a46cb6b72e42f81498f0473f0faf6e`。

來源與備份保持目錄 700／檔案 600；只有公開 Web 產物與映像內 `/app` 的非機密發行檔採 755／644。兩份 API 實際 nonroot 可讀、Web nonroot、正常 HTTPS 資產 MIME／大小／SHA 均通過。API 八組單輪 162／578、最終 app-browser 67 項（66 GUI＋1 SQL）及 GUI 清理 23 範圍全零，見 [本批 QA](docs/qa/20261120-opb03.md)。正式程式提交 `caae71d25ed3b88274aeddc5095806e286b5816e` 已正常快轉同步 main，349 檔來源綁定通過、未允許差異零（文字換行／BOM 表示差異依既定規則核對）；後續純文件狀態提交另記，不取代 release code。下方 OPB02 與舊 pending 保留歷史。

> **本批完成（2026-11-20）：OPB02已正常同步GitHub main `487e9d1`。** 正式程式版本（release code commit）為 `487e9d1d15489be5d864c54abc832b81724f579d`；root核對1份migration／139份API／206份Web與已部署、已驗收來源一致，正常推送395a→487，沒有force push。API1d09／Webcfa／0054、GUI48項（含SQL讀回1項）及19scope全0狀態不變。下方pending為發行歷史；後續純文件狀態提交另記，不取代release code或重建產物，未提供的Job等provider繼續保留限制。

## 1. 共通原則

同一套 Web/API 程式碼可部署至不同主機；執行產物、反向代理、機密注入、服務管理和腳本依目標環境準備。部署差異留在設定和 Infrastructure，不進入 Domain/Application 業務規則。

Web 靜態資產與 API 分別建置和部署。PostgreSQL 可由 Compose 部署，或連客戶管理的資料庫；位置依備份、容量、權限、可用性及客戶政策決定。

## 2. Ubuntu + Docker Compose

- Nginx 或核准的反向代理提供 Web 靜態資產和 TLS 入口。
- ASP.NET Core API 以 Linux Container 執行；Docker Compose 管理服務、網路、健康檢查和設定。
- YMES API 目標為 .NET 9；以 API Dockerfile 的 .NET 9 SDK/runtime 映像建置與執行。Vue Web 是獨立的靜態資產服務，不安裝或替換主機共用的 .NET runtime，避免影響同機其他專案。
- PostgreSQL 可作為獨立 Compose 服務或使用外部資料庫；資料目錄必須使用持久化儲存。
- 機密由部署環境注入，不嵌入映像、不提交版本庫、不輸出到 log。
- 資料庫端口不公開至不必要網路，服務帳號和網路權限採最小權限。
- 單一 Compose 主機仍是故障單點；高可用需求要另設備援主機/資料庫及演練流程。

YMES Vue Web 與 ASP.NET Core API 使用不同容器，部署在同一台主機。反向代理將 `/YMES/` 下的 UI 路徑送到 Vue 靜態服務並提供 SPA fallback，以 `/YMES/API/` 轉送 API；主檔表單和關聯設定都由 Vue 呼叫 API，不再轉送 MVC。小寫 `/ymes` 路徑回應 404，避免未匹配路徑落到同機其他網站。API 與 Web 容器只透過主機 loopback 的專用連接埠提供服務。主檔 API 使用 repository 介面；Production 預設停用主檔端點，直到 PostgreSQL provider 完成，Fake provider 只用於 Development。部署時將 YMES 路由以獨立 Nginx include 加入既有 HTTPS server，保留其他專案的路由，並在重新載入前執行 `nginx -t`、保留設定備份。API 使用獨立服務名稱、資料保護金鑰持久卷及機密設定，不替換主機共用的系統 runtime。

## 3. Windows Server + IIS

- IIS 提供 Vue 3 Web 靜態資產並配置 SPA 路由 fallback。
- ASP.NET Core API 透過 ASP.NET Core Module 託管；主機安裝符合專案版本的 .NET Hosting Bundle。
- Web/API 使用獨立網站或應用程式邊界，設定應用程式集區身分和檔案權限。
- PostgreSQL 可位於同一台或獨立資料庫主機；連線和防火牆依客戶政策設定。
- 機密不得進 Web 靜態資產、原始碼或一般日誌；憑證、IIS 代理/重寫設定和權限列入部署檢查。
- Windows/IIS 使用 Windows 目標產物；API 契約、資料結構和業務行為與 Ubuntu profile 保持一致。

## 4. 遠端測試和發行

### 遠端記憶體有限時的 Web 發行

先在家用端 `web/` 執行 `npm.cmd run build`（包含 vue-tsc），將產出的 `dist/` 與 `web/nginx.conf` 放入同一發行目錄。以 `deploy/Dockerfile.web.prebuilt` 建立 `ymes-web:current`，再對既有 Compose 執行 `up -d --no-build --no-deps ymes-web`。此方式只搬已驗證的公開靜態產物，不包含 `.env`、node_modules、登入憑證或資料庫設定。API 獨立建置與部署；不調大共用主機記憶體或修改其他服務。

2026-11-18 完整 UI 擴充後，遠端 Node 建置約 1 GB heap 上限耗盡；本機同版 Web build 已成功，因此 QC503/PS102 發行改採此方式。發行時保留前版 image tag 與靜態產物，回復使用前版 image，不在運作中的容器直接覆蓋檔案。

建置必須等待程序實際以 exit code 0 結束才可打包；工具回傳 session ID 代表仍在執行，不能當成成功。發行快照只包含本批已驗證內容，避免混入下一批草稿。打包前確認 `dist/index.html` 引用的 JS/CSS 存在且非空，封裝後再查 archive 大小與內容；不可將 Vite 建置期間已清空的 dist 部署上線。切換後檢查入口及同名 JS/CSS 的 HTTP 200 與實際下載大小，再做 app-browser 冒煙確認；失敗先切回保留的完整 image。

### 公開靜態資產權限與四項發行檢查

公開的 `dist/` 各層目錄使用 `0755`，其中檔案與發行用 `nginx.conf` 使用 `0644`。tar 必須明列 `dist/`、`dist/assets/` 及其他父目錄項目，並指定上述模式；不可只放檔案，依賴解壓時隱式建立目錄。發布流程仍採 `umask 077`，只有公開產物套用這些權限；機密 `.env`、備份及來源快照的目錄維持 `0700`、私密檔案維持 `0600`，不得對整個發行目錄遞迴放寬。

每次切換 Web 必須完成以下四項檢查，結果記入本批 manifest：

1. **封裝與權限**：確認 tar 明列所有父目錄，目錄為 `0755`、一般檔案為 `0644`；解壓後、Docker build 前再次核對公開產物權限，並確認內容與本機建置的檔案清單、大小及 SHA256 相同。
2. **容器可讀**：`COPY` 會保留來源權限，須以容器實際的非 root nginx 使用者確認公開目錄可進入、JS/CSS 與設定檔可讀；以 root 讀取成功不能取代此項檢查。
3. **外部資產**：從正式 HTTPS 入口下載 `index.html` 引用的 JS/CSS，核對 HTTP 200、正確 MIME、完整位元組數及 SHA256 均與本批 manifest 一致。API ready 200 或入口 HTML 200 不能取代資產檢查。
4. **全新瀏覽器載入**：使用 app-browser 重新載入正式入口，確認新版本資產實際執行、畫面正常且無資產載入錯誤，再逐項驗證本批操作；不可沿用舊分頁記憶體中的畫面判定上版成功。入口正常也不等於各模組操作已全部驗收。

2026-11-20 OP103／PS103 焦點修補 a1 發布曾出現空白頁：tar 只有檔案而未明列目錄，`umask 077` 使隱式建立的 `assets/` 成為 `0700`；Docker `COPY` 保留該權限，非 root nginx 無法讀取。已僅將公開產物調整為上述模式並重建 a2；目前 Web `06d3b149f7cb`、API `16a825af0bea`，JS `index-CaOqfmnb.js`／CSS `index-BPgDSiAI.css` 的外部 MIME、大小及 SHA256 已通過核對。保留 a1 的 immutable source `/opt/ymes/checkpoints/web-op103ps103-focus-20261120-a1` 及原始 provenance，以獨立的 runtime 修復紀錄區分舊映像 `1d9162efc1cf` 與 a2，不覆寫來源快照。此為發布修復紀錄，整體 GUI 仍待驗，不能據此宣稱 OP103／PS103 全部操作已驗收；`releaseComplete=false`。

### API映像內發佈檔權限與切換前讀取檢查

API亦須以映像設定的實際非root使用者驗證，不能由root可讀或Docker build成功推定可啟動。Dockerfile只在映像內將 `/app` 的發佈目錄設0755、非機密發佈檔設0644，再以 `$APP_UID` 執行；至少確認父目錄可進入、`YMes.Api.dll`、deps／runtimeconfig、appsettings及本批必要相容設定檔皆可讀。接著用既有受保護設定啟動候選版本並確認ready，再依本批image ID及前版guard切換。讀取或啟動檢查失敗即停止，不切換正式服務。

這是映像內發佈內容的權限處理，不對主機來源快照、機密環境檔、憑證、資料庫備份或掛載的持久資料遞迴chmod；它們仍依原受保護權限及對應runtime擁有者管理。來源／備份目錄0700與檔案0600不因容器COPY會保留模式而直接放寬；建置映像中的非機密發佈檔需在Dockerfile明確處理。

2026-11-20 OPB02 API a1在切換前因compat候選映像的 `appsettings.json` 為0600而被實際非root讀取檢查攔下，正式API仍保留 `16a825af0bea`。已補上述Dockerfile處理，新的a2映像須重新通過讀取、啟動及版本檢查；此處保留當時修正紀錄，後續a2實際部署及驗收結果見下節OPB02發行紀錄。

- Ubuntu 遠端測試可透過 SSH 操作；IIS 依客戶核准的 Windows Server 管理/部署方式執行。
- 測試、驗收和正式環境分開；未授權不得以正式資料做破壞性測試或 Migration。
- 每次部署保留版本、設定差異、Migration 記錄、健康檢查和回復步驟。
- 部署後驗證 Web/API 健康檢查、認證授權、資料庫連線和關鍵流程。
- 回滾程式版本不代表可回滾 DB；Migration 設計備份前置條件和向前修正方案。
- SSH 私鑰、Windows 管理憑證、正式帳號和環境檔放在受控憑證系統；文件只記取得和輪替流程，不記機密值。

### 2026-11-19 家用端可重建的來源快照

當工作目錄同時包含下一批草稿時，發行從固定 commit 加明列且已核對的檔案建立獨立快照。保留檔案清單、內容比對結果、建置輸出、API/Web image ID 與靜態資產名稱；不能直接把整個工作目錄當發行來源。

目前遠端另保存 root 專用的 `/opt/ymes/checkpoints/fb22a87`，是本機提交 `fb22a87` 的完整來源封存；不含 `.env` 或尚未驗證的 QC201/QC202 草稿。此提交只作本機及遠端維護 checkpoint，尚未推送 GitHub，也尚未完成本批所有瀏覽器驗收。來源封存不代表版本驗收完成。

`/opt/ymes/current-checkpoint.json` 記錄 checkpoint、公司端來源基底、映像及驗收狀態。OP103快照為API `260dfd2ff2bb`、Web `760d6055e519`，入口引用 `index-DI-geD67.js`；`browserQa` 與 `githubSync` 仍為 `pending`、`releaseComplete=false`。之後切換版本時更新此記錄，不把這些狀態提前改為完成。

OP103映像建置來源是 `/opt/ymes/releases/op103-20261119/`；共用 Compose 仍位於 `/opt/ymes/source/deploy/compose.yaml`，環境機密留在原受保護檔案。`/opt/ymes/source` 其他原始碼尚未同步至本 checkpoint，不能直接在該舊目錄執行預設重建而將線上程式退回舊版。重建須使用已記錄的獨立快照，確認映像內容與來源一致後，以指定映像對 `ymes-api`、`ymes-web` 執行 `--no-build --no-deps` 切換；保留其他專案及既有孤立服務。

OP103回復標籤為 `ymes-api:before-op103-20261119` 與 `ymes-web:before-op103-20261119`。回復前先核對實際標籤及 image ID，依本批 migration 的相容性判斷，只回復程式不會還原資料庫。備份使用 `umask 077`，`/opt/ymes/backups` 維持 700，dump 維持 600；由 root 讀取或透過標準輸入交由資料庫維護工具驗證，不放寬目錄權限。

QC201／QC202批API為 `95a153cef1d7`，由同一 `fb22a87` 基底加13份明列覆檔建立；完整API建置來源在 `/opt/ymes/releases/qc201202-20261119-a1/api`，同層 `api-source-manifest.json` 保存SHA與建置／部署狀態。本機及遠端 .NET9 Release均成功，本機0警告／0錯誤，啟動ready200；QC201七組分兩輪224HTTP／771斷言、QC202十組103HTTP／444斷言通過，各自exact fixtures／holder均0。品質Web最終a4已部署1c9b82c00343並通過完整資產HTTP核對，app-browser仍待；此批API回復標籤為 `ymes-api:before-qc201202-20261119`。

接續OP105批初版為API `16a825af0bea`／Web `1f2dff75f080`；API來源 `/opt/ymes/releases/op105-20261119-a1/api`，基於品質API加6份SHA覆檔，Web來源 `/opt/ymes/releases/op105-20261119-a2/web`，沿品質a4僅加OP105。兩者同層manifest與 `/opt/ymes/current-checkpoint.json` 記錄實際映像、來源、回歸與pending GUI／GitHub狀態。OP105 API8組145HTTP／501斷言及Web9項路由／完整資產SHA通過，不宣稱GUI已驗收。回復標籤 `ymes-api:before-op105-20261119`／`ymes-web:before-op105-20261119`。保留每批base與delta來源，不把舊 `fb22a87` 誤當已包含後續批次；未驗收的OPB02等草稿不得混入重建。

每批完成 app-browser 操作驗證並清理測試資料後，再正常 fetch/merge GitHub 新提交，解決衝突、重跑受影響檢查後 push；禁止 force push。若公司端同時有新工作，保留雙方提交與變更，合併不等於已部署或已驗收。

### 2026-11-20 OPB02正式發行：API／GUI已驗，本批Git待推

目前API為 `1d09e4febf482433d398a986a3d9458eca693324763a45cbcd468e0d791e2755`，Web為 `cfa5cf288359f1f3bbfdd5dfc9f2dbb5457f4ef0e6537d0fc38c698edb902fd7`，資料庫已到0054。API a2主／相容候選均通過實際nonroot五個發佈檔讀取後才切換main，ready200；本節更新的是後續實況，上方16a／722與a1阻擋紀錄保留為歷史。

- API八組分兩run通過，記錄合計179 HTTP／556斷言，含首輪G7傳輸中止前的部分檢查；原首輪FAIL保留，不稱單輪全通或去重數。兩輪各15scope及holder0。root先完成API-only checkpoint dryvalidate／apply，原Web a3／Git395歷史保留、新API與migration0054更新後才部署Web。
- Web a2由root完整本機build exit0、206來源前後hash全同後封裝；僅既有 `EcnOperationGroupMaster.vue` 異於office a3。公開封裝SHA `3e5ecf202f0323f19d66fd1913e6ffda1a28553b00195e172ed39aae47114283`，來源封裝SHA `87fca1dda7eae7433408f45f75e392206b3f234dab48c91f09948bece3dc4d95`。root實際部署已通公開兩父目錄0755／六檔0644、候選及運行容器nonroot、正常TLS外部JS／CSS MIME／完整大小／SHA；資產為 `index-fsF33iMR.js`、`index-Cd8G82qp.css`，API在Web切換前後仍為1d09且ready200。
- API主／相容來源分別在 `/opt/ymes/releases/opb02-api-20261120-a2/main`（139檔）與 `/opt/ymes/releases/opb02-api-20261120-a2/compat`（136檔）；Web映像建置context在 `/opt/ymes/releases/opb02-web-20261120-a2/web`。Web-only updater已dryvalidate／apply，私有206來源安裝於 `/opt/ymes/checkpoints/web-opb02-20261120-a2`，原JSON備份為 `/opt/ymes/backups/current-checkpoint-before-web-opb02-20261120-a2.json`；API／migration與歷史欄位保持，沒有服務、DB或Git異動。來源目錄0700／檔0600，既有archive／source-manifest不因runtime確認而重寫。
- Web建置時live API仍16a，封裝／切換時綁定新1d09，兩時點已在manifest分開記錄。正式source及checkpoint不能由舊 `/opt/ymes/source` 預設重建取代；重建使用上述已pin來源與本批manifest，Web及API仍獨立處理。
- 0054之後若需回復API，使用保留ECN引用防護的 `ymes-api:opb02-compat-20261120-a2`（`232cc6e27e34f08c1bb06ea35dc111ee8aa3917252e13e6e6bdbe0bb98c63df3`），不能將舊16a當成相容回復而失去已存在ECN資料的OP引用檢查。Web前版保留標籤 `ymes-web:before-opb02-20261120-a2` 指向722；程式回復不等於DB還原，不DROP0054或清除業務列。

root已在正式IAB自行Demo登入並完成OPB02主檔／明細、Copy、查找／選取、真本地核准、soft／hard刪除、nullable／stale／部分成功、dialog／Close／重進／深路由驗證；48個驗收檢查點含1項SQL提交後清理讀回，不能稱48次純GUI點擊。cleanup前父子已全由GUI刪到0，SQL精確清理9個lookup後19scope全0。最後 `browserLogs=[]` 只限最後reload，未保留整輪跨reload的console歷史。完整IssueJob／通知provider與候選存在503仍未驗，完整範圍及證據見 [ECN模組](docs/modules/OP/ECN作業站群組設定.md)。

本節時點GUI已驗、Git待root正常提交／推送；root最後fetch確認origin仍395a、無公司新碼。保留所有其他模組待辦及來源原文，整體 `releaseComplete=false`，不提前將本批記為GitHub已同步。

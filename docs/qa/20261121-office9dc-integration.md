# Office9dc ES204入口整合與限定驗收（2026-11-21）

正式網站已更新為278份來源／映像 `4b8a399b2ad402e387db163d45b0404f3b0898ab78c2250c38bcc14816caf38f`。ES204本輪交付範圍是保留Office DEV原件、正式未開放提示及Close入口；真正設備轉態、原因歷程、Lot／人員／物料副作用API仍待來源與實作，不宣稱交易功能已交付。API176／497、DB0063／catalog2add維持；checkpoint33仍為歷史502／03e／0062，71份驗證檔封存遭自動審查擋下且待明確授權，未繞道上傳或改寫checkpoint。

## 合併與完整建置

Root正常合併已驗證ET202 status `859cc048c59d5979e37010db0f534b06ebaef6e1` 與Office `9dc5bd53c3028ecdf012da1726a0c42a33f3d3d1`，雙親合併提交 `b628c7c1627593db569b3b9e153282b510dbfc21`。App衝突保留Auth A2的boundedRequest／epoch／abort／catch，只在DEV選單轉換外加Ems補項；兩台TODO雙方原文保留。獨立source peer `75da44b2a1c35a6de4c440d8fb91bfb99d4e511855bf0ef53e260aaea35acb11`，Root採用 `0f9c7b7e9187ff979766e5d2e39e12626c6aec4471abe59e2c57810656567746`。

原Web276的275份raw不變，只替App（8c181a4f…）、新增兩個Office Git原始blob：wrapper968ccc8f…／DEV bf597de7…。20份原checkout及兩份新檔僅精確LF→CRLF，發布仍取原凍結bytes；不裁切內容。278 canonical `b80e38e8174254a104655700e8318cb06baea3a8a36964ef6092907bc23a40da`，source manifest `63bffb0513299d0f872ec8e2e77fa0f16a542baa619fa05d2954eb6d2ec49650`。完整vue-tsc與Vite成功，回條 `b577b89975714864c191cc1802156c405815ba19f4f78411d3379e7cf40ea197`；來源與1295-file／203-dir既有依賴前後保持，無安裝或更新。

原pending package `7132b6605583d786a2c78fa17d9b8e10f73bd2a72d46eda5939b9ce9b5198b51`、source META `8193110c9877c72a3309dae84e9728274d1d0a00ea920179eace99038ebd4022` 不回填deployed；public完整8件含原ET202兩lazy。新JS index-CBAVyQf5.js／CSS index-BwlNi2sz.css。

## 真網站部署及獨立讀回

網站專用5輸入為兩份網站archive與baseline／installer／builder，未包含被拒絕的71份QA封存包。Root實際安裝log `e87b6eb2c006f3f0d4c9e1432fb6a6b7fc3481df2e1a3a65b56c7a53d07cd569`；candidate build log `c975b5735920b35320bd035cb4beb484459ddea1971474b13a5a093a226646e4`、下載candidate `b5e13c08a51b561256514a31deda6d306d94bd31fb89b965038cbf1eb04092cc` 保留candidate-only原狀態。

實際切換log `453bf91aa7645910a88499ce34900d7cf670044cc5e6c2248867c6890b4658e9`，只操作ymes-web，API／DB／CP不改，無remove-orphans。Web-only已知回退4f，保API497與schema63，不回有焦點缺陷的76a或不相容172 API。

另一連線獨立collector `5c88d329319f16b3841458b6710e7b4de471ffe6f717f25235016d9997a03dc6` 的真回條 `395a1f7ccf8036ae010476c1702b144329a57357e14076b9b7563a681e0c1051`：176＋278私源、runtime11／public8、配置與實際UID1654／101、current/live IDs、正常TLS11項含EMS深鏈／新JS-CSS／lower404兩項／logo／env／lazy2／ready、native11／19／35／61與full16 catalog2add前後均通過。HTML只核MIME及正確新引用，raw HTML hash是觀察值。

## 真app-browser限定操作

Root新建自有tab7、自行登出及Demo，封存 `.tools/gui-proof-20261121/office9dc-web278-final-root.json`，73901 bytes，SHA `99f3a3d7f08e392995bcb7b28a26f6b93b9b0836ca530e0dabae488810bf0a9c`：15原始事件／15成功／0false。

- 真還原session與完整21項原生選單、正式預設、Demo登入與KD logo；預設收合。
- 原生「設備管理→設備_執行→改變設備狀態」開啟，正式僅提示未開放及Close，沒有DEV假轉態／假歷程。Close、原生重入及觀察到的實際URL deep reload均通過。
- 實際1280×720與836×891截圖已Root視覺檢查；沒有document橫向溢出。收合側欄72px、21項各1 SVG、自動overflow、半透明0.4 scrollbar真實讀回。
- 既有ET202真查詢／新增初值／流程查找Query焦點及Escape／乾淨關閉、WP109現有Query讀取回歸；最後fresh Home及完整原生選單，實讀warn/error為空陣列。

此GUI沒有業務寫入或fixture；登入session／audit另屬認證操作。ET202前版完整Add／Edit／Copy／Delete／CAS／1of2部分刪除仍見5bc4封存，本次正式元件raw72df不變，未冒稱重跑全部MES CRUD。ES204正式command、其他WIP provider、未知網路故障UI與global MES QA均未因此完成。

## 下一接手

驗收後再次fetch，Office main新增ES102／ES409／ES106三提交（最新8e1fdfb）；先保存本版feature，再正常合併並完整建置／操作驗證，保留雙方成果，無force覆蓋。WP109空Lot writer後端184候選已完整SDK9 build／publish零警告錯誤，但尚未採用／PG／HTTP／GUI或部署；其兩表與最小grant提案另行審核，不將candidate編譯當交易成功。


## 程式來源與Git保存

實際提交b628c7c1627593db569b3b9e153282b510dbfc21已通過SQL9＋API176＋Web278＝463份來源綁定（3132f02a77e581f55a54b6fd360cd060a6cb3140764566c84eeeeb78e6d7603b）；正常推送codex/homepc-office9dc-20261121後直接讀回同ref（f9e87ae7809d717d3e184bc76fc61a9dae7eaf8caae015bfb368200a0eecbd4f）。沒有force或推送main；新Office提交先合併及驗證，再同步main。

# 2026-11-20 公司WIP拆批Web222整合驗證（限定GUI完成；main待同步）

本輪將已完成Web220 featurebranch同步的release `dc34ee6fc02f5949f4ffdfbbd4673e4232a9957a`，與Office最新 `12c6a2e8b98be035a82993fcaf3354a555ce31fa` 正常合併至 `32d422b5fe98cfd06338af9a0a6319f53473bfbb`。保留雙方工作，新增WP208元件拆批及WP209等級拆批的入口／KD框架整合，正式未交付provider保持明確不可用。**本版限定GUI已通過；release `3953bbad60df3ce2f94d7420135f4286e1366493` 的374來源綁定與featurebranch正常推送／遠端讀回已完成，mainSyncPending=true。** 後續Office新兩頁另做[Web224驗證](20261120-office-wip-carrier-merge-integration.md)，不重寫本版證據。

## 固定來源與實際部署

| 範圍 | 實際證據 |
| --- | --- |
| Web來源 | 222檔，canonical `ee360b1d9fd47f130df900de539402bda2a2e73a1cd5e6acdcdbca1c9f5df0d5` |
| 完整建置 | 型別檢查／Vite實際exit0後才封裝與部署 |
| 正式Web image | `b330b9b009670ea092218b8ecffdc6f1c389e9595a572ebf5ab471afd3e377e8` |
| 公開驗證 | 七項正常TLS HTTP／資產MIME／bytes／SHA及實際nonroot讀取通過 |
| 未變API | `6d2501aad008b6b5ca0c11abae0700d1f7a00b74933645c83d3a4c183dd0c171`，151檔canonical `434bd27f24545e452768022505cf92d21bbc7eab237bfab6816009a08fee1486` |
| 未變資料庫 | DB `ymes`／schema `YMES`，migration0058；本版沒有API重建、DDL、grant或backfill |

來源manifest為 `.tools/office-wip-split-web-source-manifest-b1.json`。家用端程式差異限App、style與Bin正式隔離；Component保持Office12c6原Git blob，AreaShow／Favorite／Forwarder保持Web220 dc34的Git blob，既有13個Office Vue保持原b3f來源。17入口依既有KD色票套框架，不改拆批DEV演算法或提供假正式交易。

## 正式app-browser封存結果

Root封存proof `.tools/gui-proof-20261120/office-wip-split-current.json`，1052974 bytes，SHA256 `346d017b6f980aa0d414737c36265683726465ae3957133297fcd424da1989f1`；**69個通過檢查點／70個原始events／1個已解harness observation**。這些為入口、防護、顯示與既有ET104 smoke的限定驗證，不把檢查點數當作純GUI click數或WIP API交易數。

- 新WP208／WP209及舊14個native入口均實際進入，確認正式不可用防護、Close與框架回空首頁；新兩頁另實測重進／直接fresh、KD只有一個主標題，WP209正式沒有DEV Bin假欄位。
- direct OP身分待交付入口另驗1個，與16個原生選單入口分開計數；沒有臆造新native功能。
- WP307實測30×40 Grid與四種縮放；共用SD108的核准Area lookup為空，只驗lookup／Grid／zoom／Close及穩定三層原生選單復驗，未驗有資料的layout、人員或班別交易。
- 在新Web實際GUI建立Broker／Forwarder，完成lookup、Apply／Edit、核准、停用與硬刪等ET104完整smoke；兩筆均由GUI建立並硬刪，SQL fixture writes=0。
- 最後fresh reload回空首頁，21個收合選單與本次fresh reload logs[]。此空log結論限本次捕捉範圍，不延伸為所有歷史操作都無log。

唯一observation `navigation-after-direct-load-observation` 為工具在非同步menu還原尚未穩定時繼續定位，造成locator detach／時機誤判；Root穩定後以 `SD108-stable-native-menu-recovery` 通過原生三層選單復驗。原false event及classification／recovery均保留，未修改產品來源，不宣稱整個準備流程沒有工具問題。

## 精準清理與收據

獨立SQL只讀兩個自有smoke精確鍵，broker=0／forwarder=0。`.tools/office-wip-split-smoke-cleanup-20261120.json` SHA256 `d8075801657e31dfed1aa792ca63a7affc1bb2323738ef0f46b5110d8f619c46` 記錄GUI created2／hardDeleted2／SQL fixture writes0；沒有廣刪業務資料。

Root收據 `.tools/office-wip-split-integration-gui-receipt-20261120.json` SHA256 `fbb8441053bb20ff95f5d6d68b0304f3b1dfb081825a24776d0576ba415cbd82`；獨立peer核對同一封存proof通過，收據 `.tools/office-wip-split-gui-peer-20261120.json` SHA256 `3638d1bae6ef97c54678a478612f02ab9319eb6cdbe22379cb31403b132cb4e4`，未見確定blocker。

## 既有版本與尚待完成

Web220 release dc34ee6已完成372來源綁定（收據SHA256 `9c186dc0efe913bedc2862f2a9bc37ee824524894e4e001b3ba85bb6013384b7`），正常推送featurebranch並遠端讀回相同；私有7份證據雜湊通過，小型分支發行紀錄SHA256 `e51a645061b313aa05ecb1d15f859701b398d8d15c3ab30e504fdb0a9e239261`。其86項proof／當時main待同步的歷史保留，未將220證據改成222。

ET104 A1完整checkpoint仍為 `9cbe89c5eef1ad06b72f5782a6ba6b42a8665e1b514f99768db5e4b3548d8fd5`；API169HTTP／621assert與GUI87為原A1已完成證據，本版沒有重跑或重新命名。Web220 metadata未套用，本輪後續新metadata必須保留完整A1歷史。

本版374＝SQL1＋API151＋Web222綁定已通過，收據SHA256 `c0eed78c80760b1aa7381a5dcc6f2a2c4c97d4f671649a0edbb7fff6ae3b8c32`；release `3953bbad60df3ce2f94d7420135f4286e1366493` 已正常推送 `refs/heads/codex/homepc-office-wip-integration-20261120` 並遠端讀回相同。小型本機分支紀錄 `.tools/office-wip-split-feature-release-20261120.json` SHA256 `71fa671a2aecbc720014be331734ce0c3dfedfdd78f90f299f540d3c9a6741d4`，私有上傳另待實證；222 metadata未套用，mainSyncPending=true。

Root後續正常合併Office carrier／merge新頁至3f99c172；automatic review因新兩頁尚未驗證而拒絕main push，沒有執行該次push。改以Web224完整建置／部署／GUI驗證後再正常同步，不把尚未建置的224當成已驗222。WIP providers／HostId、人員與拆批原生交易仍待逐項實作；ET105僅隔離候選與純讀peer，尚未build、migration或正式部署。完整MES及未列分支不在本次完成範圍。

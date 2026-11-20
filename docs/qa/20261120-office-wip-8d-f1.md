# F1 Web241 四8D入口整合驗收（2026-11-20）

本輪為HOME家用端 PostgreSQL復刻／API／PPK發行流程；Office提供MES原生Oracle唯讀來源與UI。F1只發行Web，沒有執行WIP建表或商業列SQL寫入；schema0060是先前ET106地址／報關原生兩表的已完成發行，不代表8D資料庫／交易provider已交付。

## 實際版本與來源

- Web `bd6414710b2bcbb60e577903ee306d79f5a682f13253472561de9ba9fd3f3150`；241份來源 canonical `4bbbf534440c55617f8ed504842feb8af30f05408ac1a91cdc667a77153d8041`。
- API仍為155份compat `466ff88469c70ce08cfd13b6210f986796aa848b8f24d62ee3470da939402487`，原snapshot／regression未重建或改寫；PostgreSQL schema0060。
- 已正常合併Office7df四頁來源與schema statusY521，F1 code70722含兩祖先；App／ERF／樣式的有限變更與原DEV語意經來源peer，Root完整vue-tsc／Vite及封裝通過。遠端由Root建置與切換，並通過非root執行資產讀取；source241 dirs700/files600，public dirs755/files644。
- 當前完成checkpoint SHA `7f4c5ae33a72c9dd3b9599840523d5e89f600ca61998990b408c399ddf350e47` 是已完成schema60／API466／前一E1 Web5ff紀錄。schema codeX800564及statusY521已394 binding、正常feature推送讀回完成。F1 runtime現為bd64，F1 metadata尚待Root收尾，不改原7f4c歷史。

## 正式 app-browser 實測

Root自行登出／Demo登入。封存proof `office-wip-8d-f1.json` SHA `54581f7e57a8700dc7a03b440ecce15d3abcb19bdbc4f9caf94323cd3b476e82`，**50 raw／46有效PASS＝43 GUI＋3 nonGUI，4個已解工具觀察**。3 nonGUI分別為event28前置SQL讀回、47最終SQL讀回、48正常TLS/runtime讀回，不稱作46純GUI操作。

- WP801 `8D BASIS_FACTORY`／`/YMES/WIP/8DBasisFactory`、WP802 `8D MAINTAIN`／`/YMES/WIP/8DMaintain`、WP803 `8D CLOSE`／`/YMES/WIP/8DClose`、WP804 `8D QUERY`／`/YMES/WIP/8DQuery`：各正式控制disabled、無DEV假列／假lookup／modal，Close回Home、原生選單重進與四個真deep URL fresh reload均完成。WP802包含所有class／日期禁用；WP803無模擬結案簽核，WP804保39欄但無假query結果。
- ERF原正式條件、attachments／Info／8D禁用與Close保持；DEV附檔／十頁維護／唯讀返回只是來源保留，不是本輪正式功能完成。
- B/F/T三主檔真query、設備／作業站原主檔真query完成。自有取貨公司 `GF1T-1120A` 完全由GUI新增與硬刪：NewApply採Edit與伺服器建立稽核；EditApply保建立資訊、採修改稽核；clean Confirm只關窗；Approve取消再確認；2→-1停用再硬刪，四態查詢皆無該列。沒有SQLseed／SQLcleanup。
- KD單框架標題；側欄收合72px、所有SVG圖示與可辨識標籤保持、可見文字隱藏、overflow:auto與40%透明thumb／透明底。最後fresh Home為空、21個根節點預設折疊；末次限定fresh範圍logs[]。不宣稱整輪歷史console全空。

原工具觀察完整保留：1以framework aggregate文字／不存在selector誤判Home→3；18把CSS隱藏textContent算可見文字→19以可見innerText復驗；21等待已移除loading cell→22確認實際query完成；23固定3秒先於回應→24等待完成。四者原passed=false皆保留，不計有效PASS，也未因這些觀察改產品source。

## 獨立讀回及清理

`office-wip-8d-public-runtime-f1-20261120.json` 5264B，SHA `da9b635ef8fb853cce2f279dff76a5e7169ecbd4259897a7d11fba9b5fff94be`：正常TLS7/7包括root／原生8DBasisFactory deep／兩個真JS/CSS MIME、bytes、SHA／小寫兩路404／ready200；UID1654/101、必要5/4檔可讀、actual/current tags與前後CP7f4c不變。

Root在GUI硬刪後另執行READ ONLY counts（event47），`office-wip-8d-f1-gui-counts-after-root-20261120.json`：exactKey、logicalKey、actorMarkers三scope全0，actor ymes、兩marker `F1_UI_20261120`／`F1_UI_20261120_EDIT`，SQL SHA `d34705ca347495ef92ac0e3e13af003e838572dcfa059cc58211b08a7429c2ff`。event28是初始0，與event47的執行時點分開；counts內容相同不把preflight借作cleanup。SQL fixture0／cleanup0，沒有讀出其他商業列內容。

## Git與界線

CodeX `70722f89330555de98bc2d01030c58206eea5236` 的398＝SQL2＋API155＋Web241來源綁定PASS，receipt SHA `576076bce7498ee2ab510c36a15d91958bb318e102c4bee322c0a1796f3a591a`。Root已正常推送 `refs/heads/codex/homepc-office-wip-8d-20261120` 並直接讀回同值，無force。純文件statusY與其398綁定／正常推送讀回、F1 metadata更新仍待Root實做，不能預填完成；main仍pending，QA後fetch發現Office `d96b80d` 的5個新提交另輪整合，未混入本版。

本輪不宣稱舊28個WIP入口全數重驗，也沒有另驗WP307／SD108本批全操作或DEV十頁實際交易；其既有歷史proof／source保留。正式8D／WIP provider、簽核／附件／動態lookup仍待實作。ET106真正writer API161／前端242候選只是隔離prepared，尚未部署、未跑真writer回歸／GUI，不計本輪API155／Web241成果；下一次合併Office來源須重新freeze／build／驗收。

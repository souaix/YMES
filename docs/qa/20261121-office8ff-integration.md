# Office 8ff 整合與正式入口驗證（2026-11-21）

本輪已完成結案與main同步。current-checkpoint SHA `33bd353dd129962d2e2cf196aa5b628cdc4b176be1495e58972621f69dd78f58`，另一連線獨立raw下載／64個保護欄位／歷史與服務核對通過，回條SHA `49925286533ee240e7a1b8a5f58513059c69b8646dd185aec8cfb4a51edb1a3d`。原7d0完整raw備份、API與DB所有段落、WP109讀取版結案及history前綴保持。發行對應code X `7c5ba15af622fe4776fecfb9bddc96cabd250ee5`、status Y `23559909602900349e5bf6018a0e9027bf9d64c2`，均真452來源綁定／正常推送／直接ref核回；main已到Y，`mainSyncConfirmed=true/mainSyncPending=false`。本次後補文件提交不改此發行對應。八個WIP provider、global MES QA與偶發登入載入根因仍待辦。

下文包含結案前各步驟的原時點，完整保留。

HOME負責PostgreSQL復刻、API、PPK SSH與發行；Office負責MES原生Oracle唯讀來源與UI。本輪正常合併Office `8ff405290a7f55bfd1e2261f8d85dec97f363409` 九提交及HOME `6e9ee9127a19d47a926db6b5a871684ad4adeaf6`，合併提交 `7c5ba15af622fe4776fecfb9bddc96cabd250ee5`。唯一WP109模組文件衝突完整保留雙方內容，沒有force或覆寫另一端工作。

## 交付範圍

八個入口為WP907、908、909、911、912、913、915、916。Office元件原bytes保留為各自DevPreview；wrapper只在DEV且preview嚴格為true時延遲載入。正式站僅顯示「正式環境尚未開放此功能。」與Close，不實例化原型假資料、查找、異動或彈窗。這次完成入口整合及隔離，八個交易provider仍依模組文件與待辦接續開發。

沿用KD配色、三層選單、預設折疊、72px圖示收合與空白首頁。原257份Web來源保持，包含WP109正式讀取頁及共享樣式；App只新增六組共48行入口對應。Web274 raw canonical為 `9f5d13f8fac0d93ffa3f91be3da2059338854a778ee2332738b49f21432e1bb9`。

## 建置與正式部署

完整vue-tsc與Vite建置均exit0，既有依賴未升級；本機建置回條SHA `799f0df2eb6ac5d88de848580f4406ab2f3085f415bb49054c74d884f8a20a15`。真Web映像 `03e961d0485b6750e4d16d1a705fdd769cad5f41a3daa15ca49e5bdf6a77ea58` 已切換；活化log SHA `ea068c2cae8d4e63739c9632bec9501d489e299c53731605c846405a52d3748b`。

API172映像 `502f88704b928ffcfd0b5dedf6e3c469707e301ac7d04e961be59ce19ca8c614`、DB0062與業務資料未改。獨立唯讀回條SHA `71f57c04cb551f94bf992ace34fee9181f52bff7f1e0e987977d37ad885235ea`，核對172＋274完整來源、API runtime11／Web公開6檔、live/current、1654／101非root、native19／35／61及完整16類catalog `62d9f825dd249d679b08f5a4cde21386158aef6c5737d0d86d75efc417e7a6ae` 前後一致。七項正常TLS檢查通過，HTML核MIME及新asset引用，JS／CSS核完整bytes與SHA；小寫 `/ymes` 與 `/ymes/` 均404。

舊checkpoint `7d0bc62f032e2e24f243fe5b016fa629ec781ed603202d5a082770db4492a4b3` 是此前WP109讀取版結案紀錄，記錄Webc828；它在本次切換中保持原bytes，不能冒稱已記錄03e。新metadata結案另行保留歷史完成。

## app-browser 真操作

本輪獨立proof：`.tools/gui-proof-20261121/office8ff-web274-root.json`，186694 bytes，SHA `437b05caaeaa2d94daf16f9994c760a38bee276dfe0877ee5fe4159bf2cdcb36`。48筆原始事件、45筆成功操作，三筆原false及後續實際復驗完整保留；沒有fixture、業務寫入或使用者代登入。

- 八頁逐一從原生三層選單進入、Close回首頁；另逐一deep link／reload及Close。WP916曾自行Demo恢復，見下段限制。
- 自行登出／Demo；KD標誌、72px圖示選單、21個原生根群組、重載後預設折疊及空白首頁。836×891檢查無文件溢出；選單overflow auto，scrollHeight963／clientHeight690，半透明thumb alpha0.4、track透明。
- WP109原生入口、真Query空結果、真Customer查找空結果、關閉查找及返回首頁回歸；讀取頁原來源未變。沒有以本輪空表smoke代替先前完整WP109含資料驗收。
- 最後fresh console warn/error為空。截圖`office8ff-web274-guard.jpg`與`office8ff-web274-home.jpg`保存在同一ignored證據目錄，guard已人工檢視。

原false #21是session恢復尚未完成時過早取DOM，#22／23核對真正頁面及Close；#42是測試錯誤預期「查無資料」，原DOM已顯示正確「查得0列／0張SWR。」，#43按實際文案核對。#36在快速goto／reload後顯示可操作登入頁，Root自行Demo後#37／38／39確認深連結重載與Close；另一次15秒原生選單等待逾時，後續fresh DOM恢復完整21群組。這兩次載入情況的確切原因尚未確認，列入HOME待辦，不將本輪描述為全程無中斷或根因已修復。

## 同步與後續

合併提交已通過452來源綁定（SQL6＋API172＋Web274，SHA `fab6ac1036584814bcf4b56521fddf9dcd25d2e8f01169d42111d0ae042f3b73`）；這證明來源對應，不代表Git已推送。GUI完成後才正常同步整合分支與main，推送前再讀Office最新ref，保留兩端提交。Git讀回及新metadata完成狀態另補實際回條，不預填。

下一個來源完整項目為ET202真API／PG／UI；WP109 writer可先做原生空Lot分支，非空Lot的最新LotBasis39欄與舊38欄DDL差異已交Office補精確來源。八個WIP交易、global MES QA及以上載入根因均未因本輪入口驗證宣稱完成。

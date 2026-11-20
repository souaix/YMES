# 2026-11-20 公司 WIP Web220整合驗證（限定GUI完成；main待同步）

本輪正常合併公司端 `b3f0f062e22b1df3437868a2c29ec51e80ebbb92` 與家用端 ET104 A1 status `b015e51191da6b6042edbd0f7ba3a55253e04bc9`，整合基底為 `7e71e38454308b2536ace65f4bb64a16f092520a`。保留雙方開發，不使用 force push，也不把舊 Web206 的驗收結果套成新 Web220 已驗。

**本版進度：Web220已部署、公開HTTP七項與限定範圍GUI已通過；Root封存86個通過檢查點／87個原始events，保留1個已解harness誤判。release code `dc34ee6fc02f5949f4ffdfbbd4673e4232a9957a` 已完成372份來源綁定並正常推送featurebranch、遠端讀回相同；本版當時mainSyncPending=true。** Office後續dbebd0f／12c6a2e不在本版220產物，另於[Web222整合](20261120-office-wip-split-integration.md)記錄，沒有改寫本版proof。

## 已完成建置與部署

| 範圍 | 實際結果 |
| --- | --- |
| Web來源 | 220檔，canonical `8071ec80f982071775cdcbd40be6d4b0509cf725e2662468bad17b19df049c46` |
| 完整建置 | vue-tsc／Vite程序實際exit0後才封裝；前次AreaShow推斷型別失敗的快照保留歷史，沒有改標成功 |
| 正式Web | `823d0571d6dc82436ae5eeace7a68b8c22e8baf15dcbf0b478260d7a15224194` |
| API | 沿用 `6d2501aad008b6b5ca0c11abae0700d1f7a00b74933645c83d3a4c183dd0c171`，151份來源canonical434bd27f…，沒有重建／切換API |
| PostgreSQL | 維持migration0058；這次Web部署沒有DDL、grant或backfill，後續GUI的自有測試準備另依實證記錄 |
| 私有來源 | `/opt/ymes/checkpoints/web-office-wip-20261120-a1`，220檔逐檔讀回；目錄700／檔600 |
| 公開產物 | 目錄755／檔644、正常TLS／MIME／bytes／SHA、實際nonroot可讀與七項HTTP檢查通過 |

JS `index-CDsIJQNj.js` 3960542 bytes／SHA256 `5dbbba9ffd3f6cc6612e31eb36e71e5dbe254797dd96c9ad250bc1198ad72718`；CSS `index-DFtv264V.css` 1043102 bytes／SHA256 `4a7cdb51118d816b65ddb8f3ec7f6f4dab2e5ba8945fb149764bf7aa74f94cee`。以 `.tools/office-wip-web-source-manifest-a1.json`、實際部署與HTTP紀錄為準；建置及靜態資產通過不等於全部畫面操作已驗收。

## 整合範圍及保留界線

- 公司端新增14個WIP Vue與原生模組規格／reference；其中13個新增Vue的原始Git blob與公司b3f逐byte保持，只有設備收藏頁新增正式模式防護。既有App補六個原生選單映射，AreaShow只對新增work-area mode停用未提供操作。
- App／共用CSS對15個新入口套KD基底，保留功能提示與按鈕，處理重複內層標題；既有SD108不套此新增WIP範圍。不是重新設計業務規則。
- WP203設備收藏、WP307區域WIP作業的正式provider仍未交付，頁面清楚顯示未開放，正式資料載入、查找與業務寫入停用且有handler guard，不讀DEV localStorage、不假造資料或成功。WP307的Grid／縮放僅改本頁顯示，Close保持可用。
- 其他公司端新增畫面保留DEV互動與來源規格；本輪驗的是正式入口、可用性提示、空資料隔離及Close，不是14個WIP API／原生交易全部交付。

## 正式GUI驗證：86通過檢查點／87原始events

Root以正式站app-browser實際操作並封存 `.tools/gui-proof-20261120/office-wip-current.json`：1590836 bytes／SHA256 `81245a0a217abe24ba90c139b02ada33729e943292f81cfe5a87dc0fcde623d9`，completed=true、freshReload=true、freshReloadLogs=[]。共86個通過檢查點、87個原始events，1個已解harness誤判的false body保留；不改寫成87個全通操作，也不套用A1的87項。

| 原生入口 | 實際頁面／路徑 |
| --- | --- |
| WP103 | 列印流程卡 `/YMES/WIP/RunCard` |
| WP113 | LOT SHEET列印 `/YMES/WIP/LotSheetPrint` |
| WP104 | 使用者登入作業(上工) `/YMES/WIP/OperatorClockIn` |
| WP105 | 使用者登出作業(下工) `/YMES/WIP/OperatorClockOut` |
| WP107 | 使用者批次上工作業 `/YMES/WIP/LotOperatorClockIn` |
| WP108 | 使用者批次下工作業 `/YMES/WIP/LotOperatorClockOut` |
| WP111 | 使用者多人加工登錄_生產批 `/YMES/WIP/OperatorLoginLot` |
| WP205 | 生產批選用生產設備 `/YMES/WIP/LotChoiceEquipment` |
| WP206 | 計數不良原因收集 `/YMES/WIP/IPQC` |
| WP207 | 分批處理 `/YMES/WIP/LotSplit` |
| WP203 | 過帳設備設定 `/YMES/WIP/EquipmentFavorites` |
| WP305 | 設備線上點檢 `/YMES/WIP/EquipmentOnlineQC` |
| WP306 | 設備點檢歷程 `/YMES/WIP/EquipmentQcListLog` |
| WP307 | 設備狀態監控_區域 `/YMES/WIP/AreaShow` |

14個原生入口完成正式空資料隔離、未提供操作guard、Close及重進；另直接驗 `/YMES/WIP/OperatorLogin` 的identity待對齊入口，未冒稱新增第15個原生選單。WP307真操作Grid非法值驗證、套用、Cancel、顏色及150／75／50／100四種縮放，屬頁面顯示設定，不是WIP業務提交。

共用SD108真picker可開，當時沒有核准Base Area，因此只驗picker、grid／zoom、Close與未被新guard攔截；**未驗載入有資料的layout或班別操作**。末次重載為空白主選單、21個nav按鈕且全部收合；beforeFinalReloadLogs與freshReloadLogs皆[]僅指本次明列採集範圍。

唯一harness observation為 `WP104-native-entry`：初始工具只檢查disabled，漏算真實readonly欄位而判false。Root真DOM重查 `WP104-native-entry-readonly-confirmed`通過；原false event、detail／snapshot、classification及recovery對照保留，沒有產品修改。

## ET104新Web smoke與獨立清理

Root在新Web實際GUI建立Broker並核准、由Forwarder真picker選該Broker，New Apply、Edit0、核准後Edit2、停用Cancel／確認、Forwarder與Broker的停用及硬刪均實測。共2筆由GUI建立並硬刪；SQL fixture writes=0，獨立精準兩鍵讀回broker=0／forwarder=0，見 `.tools/office-wip-smoke-cleanup-20261120.json` SHA256 `6b2a95e92e034700976e681c8a72646b2b1796433e3c59e6f870d2f0a2d456e1`。這不是重跑A1 API169／621或GUI87，亦不代表WIP provider交易已交付。

Root收據 `.tools/office-wip-integration-gui-receipt-20261120.json` SHA256 `72ee6f0e0036204489a39d122832837e1707c335d4bba285548b52555d81971e`；獨立peer已核對同一封存proof通過，收據 `.tools/office-wip-gui-peer-20261120.json` SHA256 `d55c78777fc48217b056e0c4064de2e4cb3f15778df94f1667b603c12074f46a`。完整MES、WIP providers、SD108 populated layout與未列分支均不宣稱完成。

## A1歷史及後續Git

ET104 A1已完成的API單輪169HTTP／621assert、GUI87項（77GUI＋10SQL）、12筆GUI建刪及11scope全零維持原紀錄。A1 code `65f17a0c347acd4b5df2a1be1cad712138ececd6`、status `b015e51191da6b6042edbd0f7ba3a55253e04bc9` 已正常同步featurebranch並完成358綁定；完整checkpoint `9cbe89c5eef1ad06b72f5782a6ba6b42a8665e1b514f99768db5e4b3548d8fd5` 保留。這些不是新Web220的完整GUI驗收。

本輪SQL1＋未變API151＋新Web220＝372份真來源綁定已通過，binding收據SHA256 `9c186dc0efe913bedc2862f2a9bc37ee824524894e4e001b3ba85bb6013384b7`。release code `dc34ee6fc02f5949f4ffdfbbd4673e4232a9957a` 已正常推送 `refs/heads/codex/homepc-office-wip-integration-20261120` 並讀回相同；私有7份證據上傳／雜湊核對通過，小型分支發行紀錄SHA256 `e51a645061b313aa05ecb1d15f859701b398d8d15c3ab30e504fdb0a9e239261`，完整A1 checkpoint仍為9cbe，未套用220 metadata。此時main同步仍待後續整合，`mainSyncPending=true`。

Office `dbebd0f336e6a1c5862a121e46ed11b23e8ee75b`／`12c6a2e8b98be035a82993fcaf3354a555ce31fa` 已在後續正常merge至32d422b5，另做Web222建置與驗收；不把220的86項proof改配222。WIP providers及全MES仍未完成。ET105最新僅隔離候選與純讀peer，尚未build、migration或正式發行。

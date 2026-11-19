# 2026-11-20 公司版本整合入口驗證

公司端 `3192964` 之後至 `4e0bae1` 的二十筆 OE／WIP 提交正常整合後，由家用端使用正式 app-browser 實際驗證受影響的 **23 個唯一入口，23/23 通過入口防護**。清單包含新增 OE 6 個、WIP 12 個，以及共用元件受影響的既有 OE 5 個。

## 版本及驗證界線

- API 保持 `16a825af0bea`；本輪沒有宣稱新增這些功能的正式 API、schema 或生產交易。
- 前 22 個入口於 Web `5959083456ce` 完成；a2 僅修改 `SemiInventory.vue`，部署 Web `61166de65bf6` 後補驗第 23 個 WP604。這是兩個相容快照的合併證據，沒有把未重跑的 22 個寫成 a2 全數重跑。
- 各入口確認正確路由、服務未提供提示、未呈現 DEV 業務資料或假查詢結果；對尚未接入的查找及交易停用／顯示不可用，離開後同入口重進通過。
- 18 個入口實按頁內 Close／關閉返回空白首頁；WP101、WP102、WP201、WP202、WP204 的正式 fallback 沒有頁內 Close，實際由框架首頁離開後重進，不計為頁內 Close 測試。
- 本頁通過範圍是正式入口、假資料隔離與不可用防護；庫存扣帳、工單、包裝、載具、設備上料等正式查詢、交易與外部服務仍依各模組 MD／home TODO 待實作。沒有以空清單或 disabled 按鈕替代業務驗收。

## 23 個入口

下表每列均已確認路由、不可用狀態、無 DEV 資料及離開後重進；路徑區分大小寫。

| 功能 | 選單功能名稱 | 正式路徑 | 離開方式 |
| --- | --- | --- | --- |
| OE201 | 工單基本資料 | `/YMES/OE/ManufacturingOrders` | 頁內關閉 |
| OE202 | 工單刪除 | `/YMES/OE/ManufacturingOrderDelete` | 頁內關閉 |
| OE203 | 工單查詢 | `/YMES/OE/ManufacturingOrderQuery` | 頁內關閉 |
| OE204 | 工單達交率(準時開工) | `/YMES/OE/ManufacturingOrderStartRate` | 頁內關閉 |
| OE205 | 工單達交率(準時完工) | `/YMES/OE/ManufacturingOrderFinishRate` | 頁內關閉 |
| OE206 | 工單用料清單 | `/YMES/OE/ManufacturingOrderMaterials` | 頁內關閉 |
| OE207 | 工單存料現況 | `/YMES/OE/ManufacturingOrderMaterialStock` | 頁內關閉 |
| OE208 | 工單結案 | `/YMES/OE/ManufacturingOrderClose` | 頁內關閉 |
| OE209 | 工單元件資料維護作業 | `/YMES/OE/ManufacturingOrderComponents` | 頁內關閉 |
| OEB01 | 工單挑片資料查詢 | `/YMES/OE/ManufacturingOrderWaferQuery` | 頁內關閉 |
| OEP01 | 工單開立_晶片測試 | `/YMES/OE/ChipTestManufacturingOrders` | 頁內關閉 |
| WP101 | 生產批開立 | `/YMES/WIP/LotBasisCreate` | 框架首頁 |
| WP102 | 生產批開立_批次 | `/YMES/WIP/LotBasisBatch` | 框架首頁 |
| WP201 | 生產批執行 | `/YMES/WIP/LotExecution` | 框架首頁 |
| WP202 | 生產批執行_元件 | `/YMES/WIP/LotExecutionByComponent` | 框架首頁 |
| WP204 | 生產批執行_設備 | `/YMES/WIP/EquipmentLotState` | 框架首頁 |
| WP301 | 設備上料 | `/YMES/WIP/EquipmentMaterialLoading` | 頁內關閉 |
| WP302 | 載具現況管理 | `/YMES/WIP/CarrierState` | 頁內關閉 |
| WP601 | 包裝作業 | `/YMES/WIP/PackingMOLine` | 頁內關閉 |
| WP629 | 在製品庫包裝作業 | `/YMES/WIP/PackingMOLineInv` | 頁內關閉 |
| WP603 | 在製品庫帳務維護 | `/YMES/WIP/WipInventory` | 頁內關閉 |
| WP604 | 半成品庫帳務維護 | `/YMES/WIP/SemiInventory` | 頁內關閉（a2 補驗） |
| WP605 | 成品庫帳務維護 | `/YMES/WIP/FgdInventory` | 頁內關閉 |

## 重點補驗與維護紀錄

- WP301／WP302／WP603／WP605：正式空 snapshot、不讀 DEV localStorage／seed、不以示例資料作查詢 fallback；查找與交易入口有 disabled 及 handler 防護。修正及來源界線見各模組 MD。
- WP604：a2 實測 Inventory No、選擇半成品庫、Query、SEMI In、Turn Inv 均 disabled，清單 0 列並明示「半成品庫存服務尚未提供，暫無法查詢或操作。」Close 返回空白首頁，同路由重進通過。
- WP601／WP629：實際往返兩入口確認模式分離，保留 `wp601`／`wp629` 元件 key；正式無 currentLot 時不以 `OP-PACK`／`PCS` 補值。兩頁 `Close (&X)` 及 WP302 `關閉 (Close)` 另有實按紀錄。
- WP201／WP202：`Object.assign(defaults, overrides)` 僅解決 TypeScript 重複宣告；原預設與覆寫順序、DEV 流程及正式服務未提供狀態不變。
- 家用端完整原始紀錄：`.tools/gui-proof-20261120/office-merge-final-a2.json`（本機 ignored QA artifact）；其中 23 個 ID／路徑皆唯一，`devValues` 全空、`reentryPassed` 全為 true，並保留前 22 個版本與 WP604 新版本資訊。

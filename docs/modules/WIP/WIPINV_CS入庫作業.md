# WIPINV_CS 入庫作業

## 清冊與來源

- 選單清冊：WIP「在製品管理」→「包裝與庫房」→「入庫作業」，FunctionNo `WIPINV_CS`、FunctionKey 空白；Form `frmWP_INV_THEIL_BU1`、組件 `WP_INV_THEIL_BU1.dll`。
- 主來源：`D:\TP-iMes\iMES_THEIL_TP\Customer\Production\MESWin\WIP\WP_INV_THEIL\frmWP_INV_THEIL_BU1.vb`、同目錄 Designer 及 `WEBAPI.vb`。另在 `DEV_SILVA` 找到同名副本。
- Form／Designer 可核對輸入、事件、欄位轉換及 HTTP client 合約；可讀來源中沒有該 API 的 server controller／Service／Core 實作，也沒有可確認實際 SQL 或 ERP 入庫交易的程式。

## 畫面與操作

Designer 的主視窗標題為 `WIP INV`。`INV ITEM` 下拉為 `Normal`、`Lite-On`；`TH PN` 與 `LOTNO` 文字框使用大寫；按鈕為 `Search`、`Inbound`。上方 Lot Grid 顯示選取框、WONO、TH PN、LOTNO、Good QTY、CUSTOMERNO、WIP_M_LOTNO、REMARK、pack_good、pack_bad、remainder；下方分別顯示 `GOOD`、`BAD` 兩個明細 Grid。

1. Form 載入時讀取 `SAP_WERKS`、`SAP_MANDT`、`SAP_DBLINK`、`API_BASE_URL` 系統參數，預選 `Normal`。設定值不應放進瀏覽器、repo 或交接文件。
2. Search 將 INV ITEM、TH PN、LOTNO 送到 `POST api/WipInventory/GetLotList`。成功時把結果轉為 Grid，將回傳的 `lotno`、`mono`、`PRODUCTNO`、`curqty` 欄名改成 `LOTNO`、`WONO`、`TH PN`、`Good QTY`，另加 `chk` 欄；清除舊 GOOD／BAD 明細，若有結果則選取第一列並載入其明細。
3. 選取 Lot 列會呼叫 `POST api/WipInventory/GetGoodDetail` 及 `POST api/WipInventory/GetBadDetail`，以該列 LOTNO 查回傳資料並綁定兩個明細 Grid。API client 會動態顯示回傳欄位；原始碼未包含其固定欄位定義。
4. Inbound 逐筆處理已勾選列。一般 `Normal` 送出 `GoodQty = Good QTY`、`BadQty = "0"`；`Lite-On` 送 `GoodQty = pack_good`、`BadQty = pack_bad`，空白數量轉為 `"0"`。兩種模式都帶 LotNo、Mono/WONO、ProductNo/TH PN、Remainder、IsLiteOn 及三個 SAP runtime 參數。`WEBAPI.vb` 另定義 `GetSapPayload`，但此 Form 沒有呼叫它。
5. 某筆失敗即停止後續列並顯示該 Lot 錯誤；此前成功列不會回復，也不會自動重新查詢。所有列成功後才重新 Search。來源沒有顯示部分成功後的冪等／重送保護。

## Vue DEV 復刻

- Vue route：`/YMES/WIP/InvInbound`，選單 id `function:WIP:WIPINV_CS`。
- DEV 頁以合成資料模擬 Normal／Lite-On 查詢、列勾選、Good／Bad 明細、數量 payload 選擇、逐筆入庫及中途失敗；失敗情境可呈現部分成功且停止後續列。假資料不連 MES、SAP、Oracle 或 YMES PostgreSQL。
- Good／Bad DEV 明細以 key/value 方式展示，欄位內容僅為合成 fixture；原服務回傳欄位未知。正式環境目前顯示未接入狀態，不會送交易。
- 未 build、未測試、未使用瀏覽器驗收。

## 家用端 API／資料交接

舊 WinForms client 使用 `ApiExecResult`（Success、Message、FunctionName、RowCount、Data）及 dictionary row list，採 POST 呼叫下列相對路徑：

| 操作 | 舊 client 路徑 | Request 欄位 |
| --- | --- | --- |
| Lot 查詢 | `api/WipInventory/GetLotList` | IsLiteOn、ProductNo、LotNo、SAP_WERKS、SAP_MANDT、SAP_DBLINK |
| GOOD 明細 | `api/WipInventory/GetGoodDetail` | LotNo、SAP_WERKS、SAP_MANDT、SAP_DBLINK |
| BAD 明細 | `api/WipInventory/GetBadDetail` | LotNo、SAP_WERKS、SAP_MANDT、SAP_DBLINK |
| 入庫 | `api/WipInventory/Inbound` | LotNo、Mono、ProductNo、GoodQty、BadQty、Remainder、IsLiteOn 及 SAP runtime 欄位 |

- 家用端先取得上述 endpoint 的 server source／正式契約，核對 lookup SQL、SAP 寫入交易、授權、重試、冪等、逾時與部分成功對帳，再決定 YMES 登入保護 API 的範圍及 service config。舊 endpoint 位址和 SAP runtime 值不得放入前端。
- WinForms／client 沒有指出直接 Oracle 表名；目前沒有可信的 source table DDL，故不提供猜測欄位或 CREATE-only SQL。待 server/Core 證實所用 MES 表後，再由公司端以受保護 MESTEST profile 唯讀取 DDL／metadata，家用端再依交接設計 schema/API。

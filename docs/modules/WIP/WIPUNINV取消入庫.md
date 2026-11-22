# WIPUNINV 取消入庫作業

## 來源與入口

- 清冊項目 `WIPUNINV`「取消入庫作業」，隸屬包裝與庫房，FunctionKey 空白，Form 為 `frmWP_UNINV_THEIL_BU1`。
- 主要來源：`D:\TP-iMes\iMES_THEIL_TP\Customer\Production\MESWin\WIP\WP_UNINV_THEIL\frmWP_UNINV_THEIL_BU1.vb`、同目錄 `frmWP_UNINV_THEIL_BU1.Designer.vb` 及 `WEBAPI.vb`；另有 DEV_SILVA mirror。
- DEV fallback：`/YMES/WIP/UnInventory`。純 Vue fixture，不呼叫 SAP、Web API、MES Service、資料庫或 ERP。

## WinForms 畫面與操作

- Form 顯示時先載入 `SAP_WERKS`、`SAP_MANDT`、`SAP_DBLINK`、`API_BASE_URL` runtime parameters，再自動執行 Search。畫面有 Search、outbound 和單選 Grid；Designer 雖有 Close handler，但沒有事件連結。
- Search 呼叫 `api/WipUnInventory/GetPendingList`。成功時將 `Data` 轉成 Grid，並改欄名：`ZZMES_WO`→`WONO`、`ZZHSDAT`→`HSDATE`、`ZZGOOD_FAIL`→`GOOD/BAD`、`BDMNG`→`QTY`、`MATNR`→`PRODUCTNO`、`ZZSUBLOT`→`LOTNO`、`GETDAT`→`DUEDATE`。錯誤以 Link Error 顯示，保留既有 Grid。
- 顯示欄位為 `WONO`、`PRODUCTNO`、`LOTNO`、`QTY`、`GOOD/BAD`、`HSDATE`、`DUEDATE`、`IDBSNO`、`WERKS`、`GRSNO`、`AUFNR`、`MANDT`；單選整列。
- outbound 直接使用目前 ActiveRow，沒有確認對話框。未選列提示 `Please select one record.`；`LOTNO` 或 `IDBSNO` 空白時拒絕送出。Request 為 `LotNo`、`Aufnr`、`Matnr`（取 `PRODUCTNO`）、`Idbsno`、`Grsno`，空白 `GRSNO` 預設 `0001`；`SAP_WERKS`、`SAP_MANDT`、`SAP_DBLINK` 一併附加。呼叫 `api/WipUnInventory/Outbound`，成功後重新 Search；回傳失敗或例外以 Outbound Error 顯示。
- API consumer response 型別包含 `Success`、`Message`、`FunctionName`、`RowCount`、`Data`。Form 不含服務端交易實作。

## DEV 頁面

- 假資料清單支援自動初次載入、Search、單筆選取、欄位呈現及空 LOTNO／IDBSNO 阻擋；outbound 直接模擬送出。空 `GRSNO` 使用 `0001`，成功後依來源流程重查清單。
- 可切換 Search Link Error、outbound Success／失敗／逾時結果未知。未知結果時先停用重送，要求 Search 對帳；此為 Web handoff 的防重保護，WinForms 原始碼沒有相同保護。操作紀錄僅存於頁面狀態。
- DEV fixture 的工單、批號、工廠、Client 及日期皆為虛構值。正式環境不提供未接服務的假資料。

## 服務、資料表及家用端 handoff

- 可讀 THEIL／DEV_SILVA 程式庫只找到 Form 與 `WEBAPI.vb` API consumer；未找到 `WipUnInventory/GetPendingList`、`Outbound` 的 controller、Service 或 Core 實作。無法確認 pending list 的 SAP 查詢條件、Outbound 業務交易、重送規則及回應語義。
- Consumer 未直接指出 Oracle MES 表或 SQL；實際資料在 SAP／外部 API 的來源和儲存表無法由目前來源確認，因此沒有可作為 Oracle DDL／metadata 查詢對象的表名。未連 Oracle／YMES PostgreSQL，也沒有推測或建立 SQL。
- 家用端先核對部署中的 endpoint、授權與 SAP runtime 設定來源，確認 `WERKS`／`MANDT`／DB link 不可由未授權用戶任意指定；查詢需限制使用者可見工廠與 pending records。Outbound 要定義唯一請求識別、SAP 交易結果、冪等、防重及逾時後的 reconciliation；資料列從 pending 消失須由服務端結果驗證，不能以瀏覽器狀態作準。無法確認時先保留正式 outbound 未接入。

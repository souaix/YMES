# SC402 SPC 異常單查詢

## 來源與定位

- 選單：SPC 異常單 → SPC 異常單查詢；`FunctionNo=SPC ERF QUERY`、`FunctionKey=SC402`。Vue DEV 路徑 `/YMES/SPC/ErfBasis`。
- 標準 Form／Designer 同檔：`D:\iMES\CSiMES\STD\MESWin\SPC\SC_ERFBasis\frmSPC_ERFBasis.vb`。
- 主查詢呼叫 `wsWIP.LoadERFBasisJoinLotBasis` → `kcWIP.clsLot.LoadERFBasisJoinLotBasis`。欄位 lookup 呼叫 `wsSPC.LoadERFFieldValue`／`LoadERFLotNo` 與 `wsOP.LoadOPBasis`；MClass、Hold Disposition Group 由 Form 各自載入。
- ERF Info 使用 ObjectFileHandler 開啟共用 `WP_ERFInformation`。8D 按鈕呼叫 `wsWIP.Load8DBasisFactory` 查既有 QRS；新增／維護畫面分別是 `WP_8DBasis_Factory`／`WP_8DMaintainDef`，沿用 WP801／WP802。

## 查詢與 Result Grid

畫面有六個 checkbox 條件：Lot No、Customer No、OP No、MClass No、Cur Group、ERF No。至少勾選並輸入一項有效條件才呼叫查詢；不接受無條件全表查詢。沒有狀態、日期、Material、Vendor 或 Product 篩選。Form 固定傳 `ERFSource=1|2`（SPC 與收料異常單），不限制 Status，因此已處置、作廢、放行等資料也可能出現在結果中。

Customer、OP、MClass、Cur Group 有 lookup。Lot No 的 `btnGet5_Click` 在 Form 沒有 `Handles` 關聯，畫面上的該 picker 不會由此 handler 執行；ERF No 直接輸入。未勾選條件會清空輸入值。欄位離開時，Cur Group 與 ERF No 轉大寫。

Core 以 `TBLWIPERFBASIS` 為主，left join Lot Basis／Lot State、Product、OP、Material、Vendor、Customer，另用 `TBLWIPERFBATCHLOT` 支援批次異常。Lot No 以 contains 比對普通 Lot 或批次成員；OP、Customer、MClass、Cur Group、ERF No 為 exact match。Form 只傳上述六個可見條件及 ERFSource，雖 Service/Core 還有其他參數，SC402 未使用。

Core 將查詢字串直接串接 SQL，沒有分頁或筆數上限，也沒有明確 `ORDER BY`。批次查詢由有 BatchLot 與無 BatchLot 的兩段 SQL `UNION ALL` 組成。Oracle Product lookup 使用 `CURVERSION=1`，非 Oracle 分支依 Lot Basis 的 `PRODUCTVERSION` 聯結。`CalHoldTime` 對 Status 12/21 使用 CreateDate 至 ReleaseDate，其餘以 CreateDate 至查詢當下計算。

原 Grid 可見欄位依序為 ERF No、Lot No、Customer Lot No、Product No、Status、OPNo、CustomerNo、Vendor No、Material No、MClassNo、HoldDescription、CurGroupNo、DispDescription、RuleNo、CreateQty、ScrapQty、Release Qty、Release UserNo、Release Date、Cal Hold Time、Priority、QRSNo。`ProductName`、CustomerName 及其他 Service 欄位隱藏。Status 0/1/2/11/12/21 對應 ERF Undisposition、ERF Wait Disposition、Notice Customer and Wait Disposition、Wait Lot Disposition、Abort、Release；Form 沒有為其他 Status 顯示文字。ERFNo 設 ascending sort indicator，ERFNo 與 LotNo 兩欄固定在 Grid 左側。

## 按鈕與互動

- **ERF Info**：必須先選結果列，傳該列 ERFBasis DataRow 到共用 `WP_ERFInformation`。DEV 重用 WP508 異常單資料的五頁假資料視窗與假附件。
- **8D**：必須先選列。若有 QRSNo，Form 用 `wsWIP.Load8DBasisFactory(QRSNo, IssueState=default)` 重新查詢；無結果或 `IssueState=-1` 時不開啟。其他狀態開 `WP_8DMaintainDef`，傳 QRS DataRow 及 `QRSClose=1`。若沒有 QRSNo，開 `WP_8DBasis_Factory` 並傳入 ERFBasis；對話窗返回後重新查 ERF 清單並選回原列。DEV 沿用 WP801 建單關聯、WP802 維護假資料和既有 8D 假資料連結。
- **Close**：關閉功能頁。

## DEV 範圍

`web/src/components/WipErfBasis.vue` 增加 `function-variant="spc"`，共用查詢框架、ERF Info 與 8D 導航；WIP WP508 維持 `ERFSource=0`、日期條件、欄位和既有 DEV 查詢。SC402 DEV 只顯示 SPC／收料假資料，隱藏 Create Date／Release Date，保留來源六條件與原 Grid 順序，並模擬 Status 0/1/2/11/12/21、QRSNo、批次 Lot 命中及明細／附件。選列後可進入 ERF Info，無 QRSNo 時可帶 ERFBasis 開 WP801 DEV；含作廢 QRSNo 的資料會阻止開啟，已建立的 DEV 8D 關聯可開 WP802。查詢、明細與附件使用 DEV fixtures／頁面記憶體，跨頁 8D 關聯沿用 DEV localStorage；沒有讀寫正式資料。

## Schema 與家用端 API handoff

SC402 沒有新增業務表。`TBLWIPERFBASIS`、`TBLWIPERFBATCHLOT` 的 Oracle DDL 已在 `database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql`；Lot／Product／OP／Material／Vendor／Customer 沿用 WP101、PRD、OP、MTL、Customer references，8D 表沿用 WP801／WP802。這些結構均不在 SC402 重複建立；本輪未查 Oracle 業務資料，也未連 YMES PostgreSQL。

建議正式端提供：

- 登入及 `SPC ERF QUERY` Function ACL 保護的 ERF 查詢 API。固定限制 `ERFSource IN (1,2)`，要求六個條件至少一項有效；精確保留五個代碼條件、LotNo contains 與批次任一成員命中語意。使用參數化 SQL、明確排序、最大日期／範圍外不開放、有限頁大小及分頁；不要沿用 Core 無界 `DataSet` 查詢。
- 回傳 SC402 Grid 必要欄位與既有 `QRSNo`／Status，採 YMES PostgreSQL 正式 ProductVersion 鍵；明確定義 `CalHoldTime` 是否沿舊式依 Status 12/21 到 ReleaseDate、其他到查詢時間計算。
- ERF Info 重用 WP508 shared read API；8D 的 Basis factory／Maintain 使用 WP801／WP802 已定義的 Function ACL 與 API，新增 8D 時以單一交易或冪等關聯保護 `ERFBasis.QRSNo` 和 QRS Basis，避免孤立或重複建立。

本輪只讀 WinForms／Service／Core 與 repo 既有 DDL references；沒有 Oracle 新查詢需求、沒有建 SQL、沒有正式 API／schema 工作，未執行 build/test/browser。

## 家用端正式唯讀查詢與上線紀錄（2026-11-24）

- PPK SSH 唯讀核對 YMES PostgreSQL 的 `YMES.tblwiperfbasis`、`tblwiperfbatchlot`、`tblwiplotbasis` 欄位；migration 0070 已提供所需欄位，沒有新增 migration、表或授權，也沒有查閱業務資料。
- 正式查詢端點為 `GET /api/spc/erfs/query`（正式站 API base `/YMES/API`），使用登入 Cookie 保護、參數化查詢及 Repeatable Read／Read Only transaction。固定 `ERFSource IN (1,2)`，不加 Status 條件；六條件至少一項有效，五個代碼欄位 exact match，Lot No case-insensitive contains 並包含任一 BatchLot 成員。依 ERFNo 使用 `C` collation 升冪排序。
- 條件文字欄位長度依 Lot／Customer 50、OP 20、MClass 30、Cur Group 100、ERF 20 限制並拒絕控制字元；頁碼 1–100,000、每頁 1–100（預設 100）。`CalHoldTimeMinutes` 對 Status 12／21 且有 ReleaseDate 時使用 CreateDate 至 ReleaseDate，其他狀態使用 CreateDate 至查詢時間，結果不低於 0。
- Web 保留來源六條件與 22 欄 Grid；正式頁 ERF Info／8D 停用。頁面排版使用自適應查詢欄位與表格內水平捲動。正式查詢只回傳 Grid 欄位與後續畫面所需的 ERF Type／Source／Creator／CreateDate，沒有查詢隱藏的 ProductName。
- 正式站版本 API `sc402-query-20261124-b3`、Web `sc402-query-20261124-b4`。Web b4 只替換 `ymes-web`；API b3 保持運作，`ymes-master-web` 未重啟。API readiness 200、未登入端點 401、Web 根目錄與新 JS 資產 200。
- app-browser Demo 登入後驗證 SC402 正式路由、空條件防護、單條件與六條件查詢；目前來源表無業務列，兩次有效查詢均回 0 筆，分頁停用。Close 返回主選單，`/ymes` 是 404；SC401、SC216 查詢回 0 並可切換四個歷程頁籤。新版 SC402 console 無 error／warning。沒有寫入測試業務資料。
- 目前整體 API 框架只有登入保護，未實作 Function ACL；正式 Function ACL、ERF Info／附件與 WP801／WP802 8D API 仍為待辦。因正式表目前無業務列，無法驗證正向 Grid、跨頁結果、選列或分頁資料。

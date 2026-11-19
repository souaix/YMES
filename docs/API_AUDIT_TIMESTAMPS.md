# API 稽核日期契約

## 適用範圍與來源

本批先統一 PS103 流程類別、PD101 產品類別、PD102 產品父主檔、PD206 抽樣頻率規則與 OP103 作業站類別。OP103 僅指類別主檔，既有 OP BASIS 稽核日期不由本批一併推定。原生相容的 CreateDate／ReviseDate 保留 PostgreSQL `timestamp without time zone`，不修改欄位型別、資料庫或角色時區。2026-11-19 PPK 查核資料庫與 API 連線時區為 UTC；這些 YMES API 寫入的稽核值採 UTC，沒有匯入 Oracle 業務日期。

- API 寫入明確使用 `now() AT TIME ZONE 'UTC'`，不依賴連線時區的隱式 cast。
- 讀取上述模組的稽核值時明確標示 UTC，JSON 回傳 ISO 8601 `Z`；Vue 轉為使用者裝置的當地時間，保留秒數。
- NULL 原樣回傳，畫面留空或顯示「—」。PD102 移除 `COALESCE(createdate,now())`，避免每次查詢產生假的建立時間；CreatedAt 改為 nullable，新增真實 Creator、Reviser、RevisedAt，前端使用伺服器回傳值。
- 不回填既有日期、不全表加八小時、不更動其他模組或生效日／班別等業務日期，也不使用全域 JSON converter 猜測日期時區。

未來若匯入 Oracle 原生歷史資料，須先確認來源日期代表的時區，在匯入邊界轉換後保存；不可將未確認的原生牆鐘值直接視為 UTC。其他模組須逐一核實 writer 與來源，不能由此文件推論全系統已完成轉換。

## 驗證

本批須核對 API 新增／修改時間有 `Z`、與資料庫原值及請求時間相符、來源 NULL 不變，並以正式 app-browser 檢查使用者當地時間。建置或 API 測試通過不代替 GUI 驗證，實際結果記錄於各模組 MD。

Npgsql 的 timestamp／DateTime 對應見 [官方日期文件](https://www.npgsql.org/doc/types/datetime.html)。

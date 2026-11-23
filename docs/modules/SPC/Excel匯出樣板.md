# SC204 Excel 匯出樣板

## 範圍與來源

- Function：`XLS SOURCE FILE CONTROL`，FunctionKey `SC204`，Form `frmXlsSourceFileControl`，ExecutionFile `SC_XlsSourceFileControl.dll`。
- WinForms／inline Designer：`D:\iMES\CSiMES\STD\MESWin\SPC\SC_XlsSourceFileControl\frmXlsSourceFileControl.vb`。
- Service：`D:\iMES\CSiMES\Customer\Production\MESws_STD\wsSTD\wsSPC\wsSPC.cs`，`LoadXlsFile`、`AddXLSFile`、`UpdXLSFile`、`DelxlsFile`、`GetXlsFile`。
- Core：`D:\iMES\CSiMES\STD\MESkc\kcSPC\clsSPC.cs`，同名 `tblSPCXLSFILE` data access methods。
- 樣板二進位檔在 STD／Customer MESWin `Resources` 目錄可找到；DEV 只用其檔名作為假資料，未將 binary 複製到 YMES。

## WinForms 類別與互動

- `strXlsType`：0 Control Chart、1 AutoRPT Variable、2 AutoRPT Attribute、3 COA。Form 開啟預設 Control Chart。切換類別時若該類別清單為空，Form 會匯入內建樣板：Control Chart `MESSERIES1/2`，COA `MESSERIES1/2`，AutoRPT Variable `AutoRPTVarMonthReport`／`AutoRPTMeasureData`，AutoRPT Attribute `AutoRPTPareto`。
- Variable／Attribute 型別選單的 ComboBox 在 Designer 設為 `Visible = False`，沒有程式碼重新顯示；AutoRPT radio 的 CheckedChanged 仍讀該 ComboBox 並在空值時提示。因此舊畫面實際無法正常選 AutoRPT 子類別。ReportDefine 按鈕也設為隱藏，其 handler 全為註解。
- Grid 列出 XlsNo、XlsName、XlsFileName、Description、Creator/CreateDate、Revisor/ReviseDate；`xlsType` 隱藏。選列帶入右側表單。XlsNo 上限 25 字並於 Leave 轉大寫；XlsName 上限 25 字。Confirm 僅明確要求 XlsNo 與 XlsFileName。
- Add 選 `.xls`／`.xlsx`／`.xlsm` 檔案後上傳 bytes；Edit 可只改名稱／描述，若另選新檔則替換檔案。Copy 先抓來源 BLOB 到記憶體，再新增同類別資料；Form 保留原 XlsNo，操作者需自行改為新編號。Cancel 放棄表單狀態。
- Delete 可選多列，但逐列呼叫刪除 Service，某列失敗不回復已刪的列。Download 讀回 BLOB，以 SaveFileDialog 寫到本機並啟動檔案關聯程式。

## Service／Core 與資料

- 列表 `LoadXlsFile` 依 XlsType 查詢，不取 BLOB，回傳 XlsNo、XlsType、XlsName、XlsFileName、Description、Creator、CreateDate、Revisor、ReviseDate。`GetXlsFile` 依 XlsNo／XlsType 讀 XlsBody。
- Add／Update／Delete 依 XlsNo + XlsType 操作；Add／Update 將二進位參數綁成 Oracle BLOB 或 SQL Server varbinary。成功時 Service 加 SYS EventLog。Update 不帶 bytes 時只更新 metadata，保留原 BLOB。
- Core 的 Add／Update 檔案操作明確只支援 Oracle／MSSQL，沒有 PostgreSQL provider 分支；正式 YMES API 需要改寫存取層。其餘 metadata 條件由 SQL 字串串接。
- Form 用 `FileStream.Read(buffer, 0, Length - 1)` 讀取 Add／Update 檔案，可能漏掉最後一個 byte；Core Add 收到 null body 時直接返回而不寫入，Service 未檢查該返回值仍會回成功並記 EventLog。家用端須修正 byte count、檔案空值回應與交易結果。

## Vue DEV 頁

- 路由：`/YMES/SPC/XlsSourceFileControl`，選單鍵 `function:SPC:XLS SOURCE FILE CONTROL`。
- 頁面用 DEV fixtures 模擬四個 type、內建樣板命名、空類別時載入範例、Grid 選列、多選刪除、Add／Edit／Copy／Cancel／Confirm、檔名／副檔名檢查及下載提示。DEV 為了讓 AutoRPT 可操作而明示 Variable／Attribute 子類別；舊 Form 的 ComboBox 目前隱藏。
- 檔案挑選僅記錄瀏覽器 File 的名稱與大小，不讀取內容或上傳 bytes。下載只顯示模擬訊息，不產生／開啟本機檔案。所有變更只存在頁面記憶體，離頁重置。
- DEV 在同一 XlsType 下阻擋重複 XlsNo，作為 XlsNo + XlsType 預期 key 的防護；此唯一限制仍待 DDL／metadata 確認。

## DDL／metadata 與家用端 handoff

- Core 明確使用 `TBLSPCXLSFILE`，但 repo 沒有此表同版 Oracle source DDL／metadata 或 PostgreSQL CREATE-only。桌機受保護 Oracle profile 不存在，本次未查 Oracle；不根據 Core SQL 推測 BLOB 型別、欄位長度、PK、index 或 constraint，也不生成 CREATE SQL。
- 家用端先比對 YMES PostgreSQL live schema／migration；若表不存在，待取得可靠 Oracle DDL／metadata 後再建立 CREATE-only reference 與 forward migration。需確認 BLOB 映射、XlsNo/XlsType 唯一性、欄位 nullability、大小限制、權限及原有檔案相容性。
- 正式 API 應登入並檢查 FunctionNo，XlsType 僅接受 0–3；列表可分頁，限制上傳 bytes、依允許副檔名／內容格式驗證、檔名正規化、參數化查詢，不接受路徑。CRUD 用一筆一交易、expected version／row count 衝突處理；批次刪除需明定全成或逐筆結果，並保留 SYS EventLog 語意。
- 需修正檔案讀取少一 byte 與 null body 假成功。AutoRPT hidden selector、ReportDefine handler 及四類內建樣板 binary 是否可部署 YMES，先核需求再交付；公司端未連 PostgreSQL、未建立 migration／schema、未實作正式 API。

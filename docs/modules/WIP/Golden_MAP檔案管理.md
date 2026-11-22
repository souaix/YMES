# WPP05 Golden MAP 檔案管理

## 來源與入口

- FunctionKey WPP05，清冊功能 P_GOLDENMAP FILE MANAGEMENT，Form frmGoldenMAPFileManage，分類「芯片測試生產管理」。
- 主 Form／內嵌 Designer：D:\TP-iMes\iMES_THEIL_TP\IC_CP_PKG\MESWin_IC_CP_PKG\WIP\WP_P_GoldenMAPFileManage\frmGoldenMAPFileManage.vb。更新視窗／內嵌 Designer：同目錄 frmFilerSelect_GoldenMAP.vb；沒有獨立 Designer 檔。
- Service：wsIC_CP_PKG\wsWIP\wsWIP.cs 的 LoadGoldenMAPFileState、LoadGoldenMAPFileLog、UpdGoldenMAPFileState、AddGoldenMAPFileLog、ScrapGoldenMAPFile、UpdGoldenMAPFileLog；更新產品另用 wsPRD.LoadProduct，Form Load 以 wsSYS.LoadParameter 讀檔案服務參數。
- Core：MEStc_IC_CP_PKG\tcWIP\clsLot.cs 的同名 Golden MAP 方法。
- Vue DEV fallback：/YMES/WIP/GoldenMapFileManage。DEV 使用固定 fixtures 與 localStorage，不讀取本機檔案、不連 File Service／FTP 或 legacy Web Service。

## 查詢與清單

- Form Load 取得 File Service type／host／user／password／port、Golden MAP State path、backup path。DEV 不讀取或保留這些系統參數。
- 主畫面有 DeviceNo、ProductNo、UploadTime 三組查詢欄位；有兩個 Grid：GoldenMAP File Data 目前檔案與 GoldenMAP File History Data 歷程。Query 先載入 State，成功後才載入 Log。
- Core 從兩表選出 ProductNo、ProductVersion、FileName、UploadUserNo、UploadDate、FilePath、OrgFileName、FileLength。若 Service 收到 DeviceNo，Core 會先查 tblPRDProductBasis 找 distinct ProductNo；DeviceNo、ProductNo 以 LIKE 篩選。日期只在 UploadTime 勾選時送入，日期上限只含日期則擴至當日 23:59:59。
- 主／歷程 Grid 可多選；可見欄為 Select、ProductNo、ProductVersion、FileName、UploadUserNo、UploadDate；FilePath、OrgFileName、FileLength 隱藏。來源把「至少選一個查詢條件」的檢查程式註解掉，且兩個 checkbox 沒有連動控制。更關鍵的是 Form 先把 DeviceNo XML 放入 strParameter，下一行又用 ProductNo 覆蓋；實際 Service 呼叫收不到 DeviceNo，DeviceNo 查詢無效。DEV 依畫面勾選實作 DeviceNo／ProductNo 預期查詢；正式 API 應加查詢範圍守門並保留產品 DeviceNo 映射。

## 更新檔案

- Update File 開啟 frmFilerSelect_GoldenMAP。Org File Update 預設，可多選；預設只列 .dat，勾 All File 才允許其他副檔名。由 ProductNo-ProductVersion.ext 檔名最後一個連字號切出產品及版本。
- Assign Product Info 先用 wsPRD.LoadProduct 選產品／版本，再單選一個來源檔；可勾自動重命名為 ProductNo-ProductVersion.ext，不勾則保留原檔名。來源可用文字欄位讀取產品及版本；DEV 改用固定產品 fixture。
- Confirm 逐檔處理，不是批次原子交易：若 State 路徑已存在同名檔，來源先下載暫存、搬入 backup；backup 尚無同名檔時沿用原檔名，已有同名檔時才續編 _NNNN（來源將編號檔副檔名固定為 .dat），並更新既有 Log 的檔名／路徑，再刪除 State 路徑舊檔並上傳新檔。隨後以 ProductNo+ProductVersion 刪除並重建 State，追加 Log。若 File Service 與資料庫步驟中途失敗，兩端可能暫時不一致；Form 對每個檔案各自記成功／錯誤及更新進度。

## 作廢與下載

- Scrap File 對勾選的 State 列逐筆處理：下載至暫存、移至 backup（重名加 _NNNN）、更新匹配的既有 Log、刪除 State 路徑檔案，再由 Core 依 ProductNo+ProductVersion 刪除 State。來源未顯示作廢確認視窗；各步驟可能部分成功。
- 兩個 Download File 分別下載勾選的 State 或 History 列。先檢查遠端檔案存在，再由使用者選本機資料夾；同名目的檔會詢問覆寫。DEV 只記錄目的資料夾、檔數與覆寫選項，不建立或讀取本機檔案。

## Service／Core 表與資料交接

- Core 對 tbl_PWIPGoldenMAPFileState 直接查詢；UpdGoldenMAPFileState 對相同 ProductNo+ProductVersion 先刪再插入。對 tbl_PWIPGoldenMAPFileLog 查詢／追加歷程；UpdGoldenMAPFileLog 依產品、版本、檔名、舊路徑更新 FileName／FilePath；ScrapGoldenMAPFile 刪除 State。查詢 DeviceNo 使用共享 tblPRDProductBasis，其參考 DDL／metadata 已存在於 database/reference。
- File Service 是受保護伺服器設定所指的外部副作用，不應將 credential、host、state path 或 backup path 傳給瀏覽器，也不可允許 browser 自選遠端路徑。
- WPP05 專屬 TBL_PWIPGoldenMAPFileState／TBL_PWIPGoldenMAPFileLog 缺同版 Oracle source DDL／metadata。本桌機受保護 Oracle profile／attachment 不存在；未連 Oracle／YMES PostgreSQL，未猜欄位型別、constraint、index 或 CREATE-only SQL。這兩張表需家用端先核對 PostgreSQL live schema／migration，再補同版 Oracle metadata 後才產生 PostgreSQL CREATE-only handoff。
- 家用端 API 建議分離 State／History 查詢、產品 lookup、逐檔 upload、scrap、state/history download。以登入者 audit、參數化條件及產品／版本唯一性處理；服務端驗證副檔名、檔名與產品版本、大小及儲存路徑。每一檔應有 idempotency key、明確狀態進度、file provider 與 DB 狀態核對、timeout reconciliation 及人工修復軌跡；不可假設 File Service 與 PostgreSQL 可共用一筆交易。

## DEV 頁

- fallback 頁提供 DeviceNo／ProductNo／UploadTime 查詢、State／History 多選、Org File Update 多選與 Assign Product Info 單選、假產品 lookup、All File、副檔名／檔名驗證、更新覆蓋、backup 序號、遠端檔不存在時的 Scrap／download 失敗與兩種下載模擬。DEV 按 checkbox 實作可用的查詢意圖；來源 Form 的 DeviceNo 參數覆蓋缺陷已明列。
- 僅修改本頁固定 DEV rows 及 ymes.dev.wpp05-golden-map-file-manage.v1 localStorage。沒有 fetch／XHR、MES Service、Oracle／PostgreSQL、FTP(S)、File Service 或本機檔案操作。
- 未執行 build、test 或 browser。

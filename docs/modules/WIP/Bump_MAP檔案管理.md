# WPP06 Bump MAP 檔案管理

## 來源與入口

- FunctionKey `WPP06`，清冊功能 `P_BUMPMAP FILE MANAGEMENT`，Form `frmBumpMAPFileManage`，分類「芯片測試生產管理」。
- 主 Form／內嵌 Designer：`D:\TP-iMes\iMES_THEIL_TP\IC_CP_PKG\MESWin_IC_CP_PKG\WIP\WP_P_BumpMAPFileManage\frmBumpMAPFileManage.vb`。更新檔視窗及內嵌 Designer：同目錄 `frmFilerSelect_BumpMAP.vb`。專案只編譯這兩個 Form，沒有獨立 `.Designer.vb`。
- 檔案解析 Service library：`iMES_Service\iMESWaferMAPLib\iMESWaferMAPLib\clsMergeMAPHandler.vb`。Web Service：`wsIC_CP_PKG\wsWIP\wsWIP.cs` 的 `LoadBumpMAPFileState`／`LoadBumpMAPFileLog`／`UpdBumpMAPFileState`／`UpdBumpMAPFileLog`／`ScrapBumpMAPFile`；模板另用 `wsSYS.LoadMAPTemplate`／`LoadMAPTemplateDetail`。Core：`MEStc_IC_CP_PKG\tcWIP\clsLot.cs` 與 `tcSYS\clsSYSEng.cs`。
- Vue DEV fallback：`/YMES/WIP/BumpMapFileManage`。DEV 只用本地 fixtures，不讀取本機檔案、不連檔案服務或 legacy Web Service。

## 主畫面與查詢

- Form Load 讀取 File Service／Bump MAP 路徑系統參數並檢查必要設定，再初始化兩個 Grid 與日期。來源未在 Form Load 自動 Query。
- Query 必須勾選 `WaferLot` 或 `UploadTime` 至少一項；可同時勾選。勾 Lot 時以輸入字串作 prefix；勾時間時查 `UploadDate` 起訖，結束日期若只含日期則 Core 擴為當日 `23:59:59`。`LoadBumpMAPFileState` 成功後才呼叫 `LoadBumpMAPFileLog`；第一個失敗即停止第二個查詢。
- Lot checkbox 勾選但輸入為空時，Form 仍讓 Query 通過，只是不送 `WaferLot` 參數；單勾空 Lot 可能形成無 Lot 限制查詢。DEV 保留此行為並提示範圍；正式 API 應拒絕空 prefix，避免無界讀取。
- 目前檔案及歷程都以可編輯 `Select` checkbox 多選。兩個 Grid 可見欄位相同：`WaferLot`、`WaferIDCP`、`FileName`、`UploadUserNo`、`UploadDate`；`BumpCode`、`OrgFileName`、`FilePath`、`FileLength` 等其餘欄位隱藏。
- Core 的 State 與 Log 查詢欄位為 `WaferLot`、`WaferId`、`BumpCode`、`FileName`、`UploadUserNo`、`UploadDate`、`FilePath`、`OrgFileName`、`FileLength`。Form 將 `WaferId` 改名 `WaferIDCP`，並在兩個結果集加 `Select`。

## Update File

- 主畫面的 `Update File` 開啟 `frmFilerSelect_BumpMAP`。按 `Get` 以 MAPType=1 查 `LoadMAPTemplate`，選取後另查該 `MAPTemplateNo` 的 detail；沒有 template 或 detail 時不接受檔案。
- OpenFileDialog 限 `.txt`、允許多選。來源按 template 的 ASCII／Unicode／UTF32／UTF7／UTF8 encoding 讀檔，`clsMergeMAPHandler.RenderTextWaferMAP` 依 `MAPPrefixText`、header split symbol、Lot／Wafer／Notch／X/Y 欄名、die text length 及符號設定解析 header 與 map rows；缺必要 header、WaferID 非數字或 X/Y 尺寸不合會失敗。
- 檔名依 `[WaferLot][BumpCode]-[WaferId].txt` 類格式拆解，部分欄位取自 template 解析的 header；產生 `WaferLot`、`WaferIDCP`、`BumpCode`、原始／正規化檔名、file date、length 及來源路徑。待上傳 Grid 可見 Lot、WaferID、FileName、FileDate，其餘欄位隱藏；同一 Lot／Wafer／檔名會跳過重複列。
- Confirm 逐筆以 File Service 上傳。若目前 state 檔存在，先備份舊檔到 backup，依同名備份的最大 `_NNNN` 續號，移動既有歷程記錄的 FileName／FilePath；再替換 state 檔、以 Lot+Wafer 更新 State，並新增一筆 History。各檔各自 catch error、更新 progress 與結果文字，並非整批原子操作。

## Scrap 與 Download

- `Scrap File` 對勾選的目前檔案逐筆處理：下載至暫存、備份至 backup（同名則遞增 `_NNNN`），更新歷程 FileName／FilePath，刪除 state path 遠端檔案，再刪除 State row。來源沒有 Scrap 確認視窗。
- `Download File`／`Download History` 只處理各自 Grid 已勾選的列，先選本機資料夾、確認遠端檔案存在；若目的地有同名檔，詢問是否覆寫。沒有勾選列或檔案不存在時停止／顯示錯誤。
- 上傳、Scrap、歷程記錄更新、State insert/delete 和 FTP 動作由 UI 依序執行，沒有跨 File Service／資料庫的分散式交易。某步失敗可能留下檔案與資料列不同步；家用端 API 需要有逐檔結果、冪等／對帳及人工復原路徑。

## Service／Core 表與缺項

- `clsLot.LoadBumpMAPFileState`／`LoadBumpMAPFileLog` 直接讀 `tbl_PWIPBumpMAPFileState`／`tbl_PWIPBumpMAPFileLog`；`UpdBumpMAPFileState` 以 Lot+Wafer 刪除舊 State 再新增，`AddBumpMAPFileLog` 追加歷程，`UpdBumpMAPFileLog` 依 Lot+Wafer+FileName+舊 FilePath 更新檔名及路徑，`ScrapBumpMAPFile` 刪除 State。查詢／更新 SQL 為 Core 拼接字串，家用端不可照搬未參數化做法。
- Template Core 讀 `tbl_BSYSMAPTemplateBasis`（MAPType=1）及 `tbl_BSYSMAPTemplateDetail`。File Service 連線設定、正式路徑與 backup path 由既有系統參數供給；不得傳給瀏覽器，也不得由使用者輸入任意 server path。
- 本桌機可用的受保護 Oracle attachment/profile 不存在；未連 Oracle／YMES PostgreSQL。`TBL_PWIPBumpMAPFileState`、`TBL_PWIPBumpMAPFileLog` 欠 Oracle DDL／metadata；template tables 的 DDL／metadata 亦未取得（既有 WPP07 handoff 已記其 template detail metadata 缺項）。沒有猜欄位型別、constraint、index 或產生 CREATE-only SQL。
- 家用端比對四張表的 live schema／migration 及既有查找服務後再規劃 API。服務端從受保護設定取得 file root／憑證，驗證 template 與每份檔案的 MAP 結構、路徑、大小、命名和授權；不可讓 browser 任選遠端路徑。明確規範逐檔成功／失敗、檔案搬移／DB State／Log 原子性、重送防重及 timeout reconciliation。

## DEV 頁面

- fallback `/YMES/WIP/BumpMapFileManage` 提供 Lot／UploadTime 查詢條件、state/history checkbox Grid、MAPType 1 假 template 選取、假 `.txt` file chooser、解析成功／錯誤、逐檔上傳結果、舊檔備份續號、Scrap、state/history 下載目的地及本頁操作紀錄。
- 假 fixture 皆為 `DEV-` 資料。所有 Query、template lookup、Upload、Scrap、Download 僅改 Vue 頁面記憶體狀態；不呼叫 Oracle、legacy `wsWIP`／`wsSYS`、FTP/SFTP、磁碟檔案、MES Core、正式 API 或 PostgreSQL。逾時／檔案與 state 不一致場景只作 DEV 手動對帳示意。
- 未執行 build、test 或 browser。

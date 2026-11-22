# 家裡電腦接手清單

## 公司端交接：WPP08 生產批自主產生元件（2026-11-23）

公司端依 THEIL `frmLotComponentGen`／`frmNewComponent`、CP `wsWIP.UpdComponent`、`clsLot.UpdComponent` 和相關查詢 Service 完成 [WPP08 DEV 頁](docs/modules/WIP/生產批自主產生元件.md)，fallback `/YMES/WIP/LotComponentGen`。DEV 模擬 MOState 3／6、LotState=0、批次查詢、Component State/Basis 重複阻擋、HaveComponent／Enterprise gate、SerialConfig auto/manual、Wafer ID 範圍、Unit conversion、GoodQty 編輯及 SumQty 總量守恆；Confirm 只更新虛構 Component State 記憶體，不呼叫服務或預約正式序號。

- [ ] 三張新增表已存在 repo 先前 Oracle／CREATE-only references：`TBLOEMOCOMPONENT`（OE201）、`TBLWIPCOMPONENTBASIS`（WP215）、`TBLWIPCOMPONENTSTATE`（WP101）。公司端受保護 Oracle profile 不可用，未查新的 MESTEST metadata；家用端先唯讀比對 live schema／migration，不重建共用表。
- [ ] 建立授權保護的 MO／Lot／流程／Component Basis／State／Unit／conversion 查詢與 component-generation command。server 端重新檢查 MO／Lot State、流程 HaveComponent／Enterprise、既有 Component State/Basis、每筆 WaferID／GoodQty、Unit conversion、SumQty、ComponentNo 唯一性。
- [ ] 將 `TBLOEMOCOMPONENT`、`TBLWIPCOMPONENTBASIS`、`TBLWIPCOMPONENTSTATE` 三表寫入納入一筆可恢復交易；使用安全序號預約、idempotency 與併發守門，記錄操作者及結果。Source Core SQL 字串串接，auto serial 呼叫 `ReserveSerialNo=false`，不可照搬為 browser 指定號碼。
- [ ] WPP02／WPP04／WPP08 公司端 DEV／規格／handoff 均已完成。FunctionOrder 原始值未保存在可讀清冊，日後安全 Oracle profile 可用時再核實精確排序。

## 公司端交接：WPP04 晶圓針測紀錄查詢（2026-11-23）

公司端依 THEIL `frmProbingLog`、`IC_CP_PKG.wsWIP.LoadProbingLog_Ext` 與 `clsLot.LoadProbingLog_Ext` 完成 [WPP04 DEV 頁](docs/modules/WIP/晶圓針測紀錄查詢.md)，fallback `/YMES/WIP/ProbingLog`。DEV 用固定虛構紀錄模擬 Customer／Lot／MO 至少一項、OP 多選、Component、EndTestTime、Final Result（CurVersion=1）／History（所有版次）、彙總欄及 BIN／BIN101+ 動態欄；只讀，不寫檔、不呼叫 API／Service。

- [ ] `tbl_CPWIPWAFERBINSUMMARY`、`tbl_CPSYSBin`、條件式 `tbl_PWIPWaferBin_Ext` 同版 Oracle DDL／metadata 缺項；桌機 profile 不存在，未猜 CREATE-only。先對 YMES live schema／migration 做唯讀比對，沿用 Lot／Component／Customer／SYS 共用資料表。
- [ ] 正式查詢須以登入者授權限制 Customer／Lot／MO 範圍，使用參數化查詢與結果上限。修正來源 Core ComponentNo filter 重複累加先前 `strCondition` 的缺陷；確認 From/To 邊界、OP 多值、Final／History CurVersion、extension BIN 展開與零值欄移除。
- [ ] WPP02、WPP04、WPP08 已交付；原始 FunctionOrder 未保存在可讀清冊；桌機安全 Oracle profile 可用時再核實精確排序。

## 公司端交接：WPP02 晶圓針測資料匯出（2026-11-23）

公司端依 THEIL `frmWIPWaferExport`、`IC_CP_PKG.wsWIP`／`wsSYS`、`clsLot`／`clsSYSEng` 與 Wafer MAP export handler 完成 [WPP02 DEV 頁](docs/modules/WIP/晶圓針測資料匯出.md)，fallback `/YMES/WIP/WaferMapExport`。DEV 使用固定 Lot／Wafer／設定 fixtures 模擬非空 Lot 查詢、MAP／Bin Summary、Basis wildcard 選擇、Manual／Automatic、Wafer 多選、格式明細、檔名 manifest、重複 SlotNo+OPNo 與缺 Bin Define；不寫本機檔案或呼叫 FTP／File Service／legacy API。

- [ ] `tblSYSWaferExportBasis`／`tblSYSWaferExportDetail`／`tblSYSWaferExportHeader` 及 CP Wafer MAP／Bin Summary／Bin Define 同版 Oracle DDL／metadata 缺項；桌機受保護 profile 不存在，未猜 CREATE-only。先對 YMES live schema／migrations 做唯讀比對，再決定是否新增 forward migration；不重建 Lot／MO／Product／Inventory／WIP 共用表。
- [ ] 正式 API 依登入身分限制 Lot，參數化載入配置、Current MAP、Combo MAP OP／ShipMap、Bin 定義／彙總、MO／Product 屬性、Equipment／Attribute；保留 exact／wildcard precedence 與 FunctionFlag 0／1 語意。
- [ ] 匯出格式需 server allowlist 與內容相容策略，server-side 限制檔案目的地；FTP 設定不得來自 browser。明確處理 MAP／文字／Excel、Header mapping、Bin symbol、檔名 token、ZIP、同名檔與逐輸出錯誤，提供可下載結果清單及對帳紀錄。
- [ ] WPP02、WPP04、WPP08 均已交付。原始 FunctionOrder 未保存在可讀清冊，桌機受保護 Oracle profile 不存在；有安全 profile 時再核實實際排序。

## 公司端交接：WPP05 Golden MAP 檔案管理（2026-11-23）

公司端依 THEIL 主／Update 子 Form、IC_CP_PKG.wsWIP、clsLot 完成 [WPP05 DEV 頁](docs/modules/WIP/Golden_MAP檔案管理.md)，fallback /YMES/WIP/GoldenMapFileManage。WinForms 請求組字串會以 ProductNo 覆蓋 DeviceNo；DEV 依勾選條件呈現預期 DeviceNo／ProductNo／UploadTime 查詢、State／History、多檔 Org File Update、指定產品單檔 Assign Product Info、檔名驗證、備份、Scrap 及下載；不讀寫本機檔案，不呼叫 API／Service／File Service，不連 Oracle／YMES PostgreSQL。

- [ ] WPP05 專屬 TBL_PWIPGoldenMAPFileState／TBL_PWIPGoldenMAPFileLog 同版 Oracle DDL／metadata 缺項，桌機受保護 profile 不存在，未猜 CREATE-only。共享 TBLPRDPRODUCTBASIS 參考沿既有 DDL／metadata。
- [ ] 家用端比對 Golden MAP State／History live schema／migration，再設計登入保護與參數化查詢、產品 lookup、逐檔更新／作廢／下載 API；File Service 路徑由 server 設定解析，定義部分成功、冪等與檔案／資料列 reconciliation。
- [ ] CP 清冊 WPP02、WPP04、WPP08 已交付，原始 FunctionOrder 待有安全 Oracle profile 後核實。

## 公司端交接：WPP03 設備設置單維護_晶片測試（2026-11-22）

公司端依 THEIL CP 主／ADD／EDIT／CLOSE Form、VIEW Form、`IC_CP_PKG.wsWIP` 與 `clsLot` 完成 [WPP03 DEV 頁](docs/modules/WIP/設備設置單維護_晶片測試.md)，fallback `/YMES/WIP/EqpSetupBasisCp`。DEV 以獨立 localStorage fixtures 模擬設備設置單查詢、SFN 單號、PROBER／TESTER、Accessory Release、配件數量與 Site 實物確認，以及 ADD／EDIT／VIEW／CLOSE 和設備使用中守門；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫設備關聯、serial 或 EMS 配件狀態。

- [x] 以 PPK SSH 隧道只讀核對 WPP03 物件：專屬 Setup／Accessory／Equipment Link 表、EMS accessory state/control、`TBLWIPTEMP_EQUIPMENT` 與 `TBLPRDACCESSORYGROUP_SUB`／`TBLEMSACCVIOLATESTATUSCHANGE` 均不存在；共用 `TBLEQPEQUIPMENTBASIS`、`TBLEQPACCESSORYBASIS`、`TBLEQPACCSTATEBASIS`、`TBLSYSSERIALNO` 存在。未查業務資料、未執行 DDL/DML。
- [ ] 公司端取得可用受保護 Oracle profile 後，提供 WPP03 專屬表與缺少 lookup／EMS 表的同版 source DDL／columns／PK／index／default／constraint metadata；家用端收到精確來源後才做 migration/API，不從 WinForms INSERT 推測結構。
- [x] 合併後前端建置發現 `WipEqpSetupBasisCpDevPreview.vue` 重複宣告 `status`；移除未使用宣告後，`vue-tsc` 與完整 Vite build 通過（813 modules）。
- [ ] Chrome GUI 操作尚未驗收：視窗可列出，但本輪兩次擷取均 `window capture timed out`；未登入／操作或部署，工具恢復後補查。
- [ ] 正式 ADD／EDIT/CLOSE API 需以登入者身分、參數化條件、狀態比較／鎖與冪等保護 Setup serial、主／附屬設備、Accessory Apply、現場 Site 確認；結單須將設備使用守門、舊／新設備 Link State／Log、Setup 狀態及 EMS 配件狀態納入一筆可恢復交易或 saga。
- [x] 公司端 WPP05、WPP02、WPP04、WPP08 已接續完成，原始 FunctionOrder 尚待安全 profile 核實。

## 公司端交接：WPP01 生產批開立_晶片測試（2026-11-22）

公司端依 CP `frmLotBasis_CP`／`frmLotBasisDef_CP`／FT 子窗、`IC_CP_PKG.wsWIP`、共用 `wsWIP`、CP／共用 `clsLot` 完成 [WPP01 DEV 頁](docs/modules/WIP/生產批開立_晶片測試.md)，fallback `/YMES/WIP/LotBasisCreateCp`。DEV 以獨立 localStorage fixtures 模擬 CP MO 查詢、Lot Basis、ECN、MO 流程唯讀、元件／屬性／載具、Add／Edit／Copy／逐批 Delete／Release；不呼叫 API／Service，不連 Oracle／YMES PostgreSQL，不執行序號、扣料、StartLot、CloseLot、載具交易或自動報工。

- [x] 以 PPK SSH 隧道只讀核對 YMES live schema：WP101 的 Lot Basis／State／Process／Property／ComponentState、WP215 ComponentBasis、WP207 OP ECN、WP302 Carrier Detail 與 OE MO Basis／Component 共 12 張表都不存在；`TBLSYSSERIALNO` 存在。未讀業務資料、未執行 DDL/DML。
- [ ] 注意 `TBLWIPLOTBASIS` 參考版本未一致：舊 WP101 source DDL 38 欄、DESCRIPTION 255，8ff lookup 為 39 欄、DESCRIPTION 400 並含 `XX_MARKING`。公司桌機未能使用受保護 Oracle profile，本次未補取同版 Oracle DDL；以 YMES 目標版本決定 schema。
- [ ] 建立登入保護的 CP MO／Lot／ECN／Component／Carrier／Lot Property lookups 與 Add/Edit/Delete/Release API。新增同步 Lot Basis、LotState、Process、ComponentState／Basis、MO Component BookingFlag、Property receive/value、OP ECN 及可選 Carrier In；使用 server-side serial、metadata allowlist 與登入者 audit。
- [ ] 每批次各自鎖定／重查 MO UnReleaseLotQty、LotState、元件預約與 ECN 後交易；Delete 保留 CloseLot event 200 語意；Release 包含物料扣帳、StartLot、MO 數量與 Component 狀態，提交後自動 BR／報工需可追蹤。舊 WinForms 多選逐筆處理，首筆錯誤停止但保留之前成功列，正式 API 必須回傳逐列結果。
- [x] 公司端後續已完成 WPP03「P_EQP SETUP BASIS／設備設置單維護_晶片測試」；現在接續 WPP05。

## 公司端交接：WPP06 Bump MAP 檔案管理（2026-11-22）

公司端核對 THEIL `frmBumpMAPFileManage`／Update File 子 Form、`wsWIP`／`wsSYS`、`clsLot`／`clsSYSEng` 及 `clsMergeMAPHandler`，完成 [WPP06 DEV 頁](docs/modules/WIP/Bump_MAP檔案管理.md)，fallback `/YMES/WIP/BumpMapFileManage`。DEV 僅操作記憶體中的假 state/history、template 和 `.txt` fixture；不碰本機檔案、不呼叫 FTP(S)、legacy Service、Oracle、正式 API 或 PostgreSQL。Core 涉及 `TBL_PWIPBumpMAPFileState`、`TBL_PWIPBumpMAPFileLog`、`TBL_BSYSMAPTEMPLATEBASIS`、`TBL_BSYSMAPTEMPLATEDETAIL`；桌機受保護 Oracle attachment/profile 不可用，未取得 source DDL／metadata，未產生猜測 SQL。

- [x] 家用端只讀核對 YMES catalog：`TBL_PWIPBumpMAPFileState`、`TBL_PWIPBumpMAPFileLog`、`TBL_BSYSMAPTEMPLATEBASIS`、`TBL_BSYSMAPTEMPLATEDETAIL` 均不存在；repo 未找到此模組的資料表 DDL 或正式 File Service API。未讀業務資料、未執行 DDL/DML。
- [ ] 等公司端提供四表同版 Oracle source DDL／metadata，並確認正式 File Service provider 後，才設計 migration 與登入保護 API；未核 Oracle 型別、PK／index/default 前不照 WinForms INSERT 欄位自造結構。
- [ ] 建立登入保護的 State／History 查詢、template lookup、逐檔 upload／archive／Scrap／download API。File root 與 FTP credential 只由 server 受保護設定解析，不接受 browser 傳任意 server path 或 credential；Lot／日期條件參數化，空 Lot prefix 不可退化成無界查詢。
- [ ] 保持 MAP encoding、template detail parser、header／X/Y row-size 驗證相容；明確規範多檔部分成功、State／Log 和 file provider 一致性、冪等／重送與逾時 reconciliation，並提供人工修復紀錄。
- [x] 後續清冊 WPP01「P_CREATE LOT_CP／生產批開立_晶片測試」已由公司端交付；公司端現在接續 WPP03。

## 公司端交接：WIPUNINV 取消入庫作業（2026-11-22）

公司端依 THEIL `frmWP_UNINV_THEIL_BU1.vb`／Designer 與 `WEBAPI.vb` 完成 [WIPUNINV DEV 頁](docs/modules/WIP/WIPUNINV取消入庫.md)，fallback `/YMES/WIP/UnInventory`。DEV 僅用虛構 pending rows 模擬 Search、單列選取、Outbound request、空 `LOTNO`／`IDBSNO` 守門、空 `GRSNO` 預設 `0001`、Success／Error／Timeout 與 Search 對帳；不呼叫 SAP、API、資料庫或 ERP。來源只有 client，未找到 controller／Service／Core／SAP transaction，也沒有 Oracle source table 或 SQL 可供查 DDL／metadata。

- [ ] 核對實際 `WipUnInventory/GetPendingList`、`Outbound` server endpoints、授權和 SAP runtime 參數解析；pending 查詢依登入者權限限制工廠／清單，不信任 browser 自帶 `WERKS`／`MANDT`／DB link。
- [ ] Outbound 實作 idempotency／防重、SAP 結果與 MES 記錄一致性及 timeout reconciliation；以服務端重查確認 pending 狀態。DEV 的未知結果停送並要求 Search 對帳只是 UI safeguard，未驗證任何正式交易。
- [x] 後續清冊項目 WPP06「P_BUMPMAP FILE MANAGEMENT／Bump MAP 檔案管理」已交付；下一項 WPP01 由公司端接續。

## 公司端交接：BARTENDER Bartender print（2026-11-22）

公司端依 THEIL `frmWP_BartenderPrint_THEIL_BU1.vb`（Designer 內嵌）、預覽 Form／Designer 與 `ApiClient` 完成 [BARTENDER DEV 頁](docs/modules/WIP/Bartender列印.md)，fallback `/YMES/WIP/BartenderPrint`。DEV 使用虛構 LOT／標籤規則，模擬 `GETLPC`、`GETXXQCLABEL`／LabelApi rows、標籤選擇、列印資料選取、假標籤預覽、列印確認與 `TBLINLABEL_TAG_FILE_MAP` 編輯；不呼叫任何 endpoint、資料庫、SFTP、外部執行檔或印表機。

- [x] Home repo 靜態查核未找到 `GETLPC`／`GETXXQCLABEL`、InLabel／LabelApi server handler；遠端 YMES catalog 也未找到 `TBLINLABEL_TAG_FILE_MAP`。來源只查程式／目錄 metadata，未讀業務資料或執行 DDL/DML。
- [ ] 等公司端提供實際 server handler／endpoint 契約、標籤來源 SQL/Core 和 `TBLINLABEL_TAG_FILE_MAP` 同版 Oracle DDL／metadata；再決定可重用正式列印 provider，不從 API consumer 猜資料來源或 schema。
- [ ] 正式 Web 以登入者可見的 LOT／標籤規則為界，API server 驗證可用範本與 output；確認範本來源、列印授權、部署 BarTender provider、preview／print job、idempotency、timeout reconciliation 與 audit。Browser 不可任選 server path／printer，不可直接執行 BarTender 或其他 vendor binary。
- [x] 後續清冊項目 WIPUNINV「取消入庫作業」已交付；下一項 WPP06 handoff 待公司端接續。

## 公司端交接：INV COUNT STATE 盤點關／開帳（WP606，2026-11-22）

公司端依本機 `D:\iMES\CSiMES\STD\MESWin\WIP\WP_InvCountState\frmInvCountState.vb`（Designer 內嵌）、`wsWIP`、`clsINVCount`／`clsSYSParameter` 完成關帳／開帳 DEV 假資料頁及[WP606 規格](docs/modules/WIP/盤點關開帳.md)。Fallback 路由 `/YMES/WIP/InvCountState`。DEV 模擬 State=0 關帳、六類 detail snapshot、State=1 開帳、跨 detail AdjustState=1 守門、`AllowExecuteLot` 及兩處失敗情境，不呼叫正式 Service／資料庫。

- [ ] 先核對 YMES PostgreSQL live schema／migration history 及共享 `AllowExecuteLot` 系統參數，再比對 WP608 的四張盤點 state/detail 表；桌機沒有受保護 Oracle profile，缺 Oracle DDL／metadata，未猜 CREATE-only。
- [ ] 正式 API 從登入身分取操作者；以跨請求鎖或 CAS 保護唯一 State=0 cycle 與 `yyMMddNNN` 序號。關帳至少要求一種 Close Option，將 `AllowExecuteLot=0`、State 主檔與六類快照原子化；開帳先守住所有 detail 無 AdjustState=1，再原子更新 State=1／OpenUserNo／OpenDate 與 `AllowExecuteLot=1`，並提供失敗後可恢復的 audit／reconciliation。

## 公司端交接：INV COUNT PRINT 列印盤點清冊（WP607，2026-11-22）

公司端依本機 `D:\iMES\CSiMES\STD\MESWin\WIP\WP_InvCountStatePrint\frmInvCountStatePrint.vb`（Designer 內嵌）、`wsWIP.LoadInvCountStateWithDetail`、`clsINVCount` 及 General／MTL／PRD ActiveReports 完成盤點清冊 DEV 假資料頁及[WP607 規格](docs/modules/WIP/盤點清冊列印.md)。Fallback 路由 `/YMES/WIP/InvCountStatePrint`；原 Function 未掛正式 `MenuItem`。DEV 模擬六種 DataType 的 State=0 全部已關帳明細查詢、Grid 排序、Barcode／NoBarcode report class 選擇及預覽，不產生 PDF／條碼、不呼叫正式 Service／資料庫／印表機。

- [x] 與 WP608 共用讀取資料；家用端以 PPK SSH 隧道做過只讀 catalog audit，已確認四張盤點 state/detail 表都不存在，且沒有找到可辨識的 migration-history table。未查業務資料、未執行 DDL/DML。
- [ ] 等公司端提供四表原生 Oracle DDL／metadata；資料表結構取得後，與 WP608 共用正式盤點讀取 API。
- [ ] 正式 Web 報表須選定可部署方案並確認掃碼規格、版面／分頁／頁首頁尾；重用登入保護的盤點 query。不可假設公司端 ActiveReports runtime 或桌機 DLL 能部署到正式 API/Web。

## 家用端驗收服務固定啟動流程（2026-11-22）

- [x] 本機 Vite `127.0.0.1:18749` 與 API `127.0.0.1:5080` 都仍在監聽，但 SSH DB 隧道 `127.0.0.1:15432` 曾中斷，造成 API readiness 與 Vite API proxy 回 503。2026-11-22 再次由 SSH 核對遠端 `ymes-api`／`ymes-web` 均 running／healthy，正式 `/YMES/` 與 `/YMES/API/health/ready` 都回 HTTP 200。
- [x] 啟動 `plink` PPK 隧道到家用 PostgreSQL clone 後，API `/health/ready` 與 Vite `/YMES/API/health/ready` 立即恢復 200。沒有資料庫寫入或正式站異動。
- [x] 建立並實際驗證 [`scripts/Start-HomePcQa.ps1`](scripts/Start-HomePcQa.ps1)：停止本輪自有測試隧道後，API 先回 503；執行腳本自動重開隧道，API、Web 與代理均回 200。腳本重用健康服務、只在埠空閒時啟動，遇未知程序占埠會停止並報明，不會殺掉該程序；API 連線使用經認證 SSH 隧道與 `SslMode=Disable`。PPK 保持在桌面，連線密碼只由腳本記憶體讀取、不輸出。
- [ ] Chrome/app-browser GUI 驗收仍待補；Chrome 視窗已重新定位，但兩次畫面擷取都回 `window capture timed out`，沒有進行登入或頁面操作。這是 GUI 擷取失敗；本機與正式站健康檢查均正常，瀏覽器工具恢復後再逐項驗收。

## 公司端交接：INV COUNT ADJUST 盤點調帳（WP608，2026-11-22）

公司端依 `frmInvCountStateAdjust`、`frmInvCountAdjust`／`_MTL`／`_PRD`、`wsWIP`、`clsINVCount` 完成六種 DataType 的 DEV 假資料畫面及[WP608 規格](docs/modules/WIP/盤點調帳.md)。Fallback 路由 `/YMES/WIP/InvCountStateAdjust`；原 Function 未掛正式 `MenuItem`。DEV 模擬 State=0 查詢、NewCurQty／AdjustQty 暫存、AdjustState 1→2 Confirm、逐筆更新部分失敗及報表預覽，不呼叫正式 Service、資料庫或印表機。

- [x] 以 PPK SSH 隧道對遠端 `ymes`／YMES 做只讀 catalog audit；四張盤點表均不存在，未找到可辨識的 migration-history table。Repository migrations 有到 `0064_wp109_writer_provider.sql`，但檔案存在不能證明已套用。未查業務資料、未執行 DDL/DML。
- [ ] 待公司端提供四張盤點表的原生 Oracle DDL／metadata 與 `clsINVCount` 對 RAW／MTLWIP 目標表的 Core SQL 映射；來源未確認前不猜 schema、不建立 migration。取得來源後才核 PK/index/constraint 並實作正式 API。
- [ ] 依實際 Core SQL 映射 WIP、SEMIWIP、FGD、SEMI、RAW、MTLWIP 目標表與單位換算服務。實作登入／ACL 保護 Query／調帳／Confirm／報表 API；明確限定 CountSerial+DataType、鎖定並重讀 State=0／AdjustState=1、庫存量與版本，以同一本地交易更新庫存、detail state 及 audit，任何失敗整批 rollback。
- [ ] 修正舊 MTL request 將 `MaterialLotNo` 傳成 `LocatorNo` 及初始化比較錯欄位；Confirm 需防同一 DataType 多個 CountSerial 混批。正式單位換算不可沿用 DEV 的 2:1 fixture。

## 公司端交接：FGDSTOCKIN_WF_CANCEL 取消入庫 WorkFlow ERP（WP619，2026-11-22）

公司端依取消 Form／Designer、`wsINV`／`kcINV.clsInventory`、EAI `wsMES`／`clsINV` 完成 DEV 假資料取消頁及[WP619 規格](docs/modules/WIP/取消入庫WorkFlow_ERP.md)。Fallback 路由 `/YMES/WIP/FgdStockInWorkFlowCancel`；原 Function 未掛正式 `MenuItem`。DEV 模擬 State=2／ERPNo、混合 FGD／SCRIN、WorkFlow Undo 成功／明確失敗／timeout、MES 回復成功／失敗及 1→-2→3→-1；其他 ERP 來源會顯示但被阻擋。不呼叫 ERP、MES Service 或資料庫。

- [ ] 正式 API 重新確認 ACL、ERP source、ERPNo、FGD／SCRIN 同號明細及 State=2，使用 CAS／冪等 intent；`LoadFGDInNoUnionSCRInNo` 舊查詢只有 `ERPNo IS NOT NULL`，把其他 ERP 單據也列入 WorkFlow 取消清單。
- [ ] WorkFlow `ReferenceCase=4` Undo timeout／例外不可當明確失敗後直接復原 State=2；先查 ERP 或排 reconciliation。MES 回復失敗需保留 State=-2 並提供可恢復交易及 audit；舊 Core 多表更新未包有效 transaction，Form `UpdINVInfo` 亦不比對 OriginState。
- [ ] 先對照 PostgreSQL live schema／migrations。`TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` Oracle source DDL／metadata 沿 WP622 缺項，待公司端取得受保護 Oracle profile 後唯讀補查。家用端負責正式 API、migration、部署與驗收；公司端未連 Oracle／PostgreSQL或部署。

## 公司端交接：FGDSTOCKIN_WF 入庫至 WorkFlow ERP（WP614，2026-11-22）

公司端依主 Form／Designer、`frmFGDStockIN_ReThorwERP` 重拋 Form／Designer、EAI `wsMES`、`clsWIP`／`clsINV` 完成 DEV 假資料入庫與重拋頁及[WP614 規格](docs/modules/WIP/入庫至WorkFlow_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInWorkFlow`；原 Function 未掛正式 `MenuItem`。DEV 模擬 ALLOWINV2ERP 0／1／2、多 MO Lot、MES-first、ERP success／fail rollback／timeout State=1 retry ticket、重拋 success／fail／timeout；不呼叫 WorkFlow SOAP、MES EAI Service 或資料庫。

- [ ] Formal provider 應從伺服器配置讀 `ALLOWINV2ERP`、`ShowLossData_FGDStockIN_WF`、ERP SheetType、`WFERPUseMESUserNo`、`OrgLotNoToERP`，不可接受 browser 自選值。模式 2 需修正舊 Form SheetType 只在模式 1 去名稱的缺陷。
- [ ] MO／Lot 完整重查、鎖定、單位／ERP 品號／Factory／Inventory／Locator、工單累計 Good+Scrap 限制、ERP 淨發料及 booking flag 必須伺服器驗證；批次量檢查需 aggregate 全部勾選列，不沿用舊 Core 每列重用相同既有量的漏算。
- [ ] MES 庫存交易完成前建立 ERP intent／冪等 key；明確 ERP fail 可安全補償 MES，timeout／unknown outcome 保留可恢復 State，不盲目 replay。`TBLERPRETHROWTICKET` 原始 RequestXML 需改受 ACL 保護的 reconciliation workflow／audit，不提供任意 XML 重放；ERP/MES 結果、MES 失敗後 WorkFlow Undo helper 順序需家用端實測核對。
- [ ] 先比對 live schema／migrations。`TBLINVWIP_SCRAP`、`TBLOEMOMATERIALINBASIS_ERP`、`TBLERPRETHROWTICKET` Oracle source DDL／metadata 缺項，需桌機有受保護 Oracle profile 後唯讀補查；公司端本次沒有 Oracle／PostgreSQL/API/schema／部署操作。家用端負責 migration、正式 API、部署及驗收。

## 公司端交接：FGDSTOCKIN_TP_CANCEL 取消入庫至 TipTop ERP（WP610，2026-11-22）

公司端依 TipTop Cancel Form／Designer、`wsINV`、TipTop SOAP `UndoCreateStockInData`、`clsInventory` 完成 DEV 假資料取消流程及[WP610 規格](docs/modules/WIP/取消入庫至TipTop_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInTpCancel`；原 Function 未掛正式 `MenuItem`。DEV 模擬 State=2／ERPNo、MOState=99、FGD／SCRIN、ERP 來源、ERP 失敗回復 State=2、MES 失敗保留 State=-2 及成功取消；原查詢未限制 ERP source，DEV 阻擋非 TipTop 來源。不呼叫 TipTop、MES Service 或資料庫。

- [ ] 正式取消命令需重新查 State=2、ERPNo、ERP source、INVType、每筆 MOState、ACL 與庫存前置條件；依 ERP source 路由或拒絕，不可把其他 ERP 入庫單送往 TipTop。
- [ ] 持久化 `UndoCreateStockInData` intent／response；ERP timeout／unknown outcome 先查 TipTop 或排對帳。ERP 確認成功後，在單一本地交易回復 FGD／WIP／Component／Good Bin／Lot Qty／reservation／release log 並寫最終狀態。
- [ ] ERP 已取消但 MES 回復失敗時保留可恢復狀態與人工／自動對帳；補建 State=-2 恢復流程與冪等 key。比較舊 `UpdINVInfo` 對 `OrgState` 的逐步覆寫與既有 schema。
- [ ] `TBLINVFGDINBIN` Oracle DDL／metadata 沿 WP621／WP623 缺項；需桌機有受保護 Oracle profile 後唯讀補查。本次公司端未連 Oracle/PostgreSQL，未做 API/schema 或部署；家用端比對 live schema／migration 後實作。

## 公司端交接：FGDSTOCKIN_TP 入庫至 TipTop ERP（WP609，2026-11-21）

公司端依 TipTop Stock In Form／Designer、TipTop SOAP `CreateStockInData`、EAI `wsMES`、`clsWIP`／`clsINV` 完成 DEV 假資料流程及[WP609 規格](docs/modules/WIP/入庫至TipTop_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInTp`；原 Function 未掛正式 `MenuItem`。DEV 有 MO／Ref Lot、來源 WIP／目的 FGD／Locator、雙 Grid、InInvNum 部分入庫、ERP 建單／MES 入庫／ERP Undo 情境；不呼叫 TipTop、MES EAI 或資料庫。

- [ ] 將 CreateStockInData 前驗證、Lot row version／鎖、權限、來源／目的庫、Component／Good Bin 與冪等鍵放在伺服器；舊程式部分入庫沒有 BookingFlag reservation。
- [ ] 持久化 ERP Create intent、response 及 Undo 結果。ERP 建單成功、MES 失敗時若 Undo 失敗，不能只釋放 BookingFlag 並允許同 Lot 重送；timeout／unknown outcome 先查 ERP 或排對帳。
- [ ] 核實 Good Bin 在 TipTop 整批入庫的來源規則：Form 對部分入庫有 Good Bin 守門，但目前 active `MOStockIn_TP`／`MOStockIn_TP_Component` 沒找到 Bin 更新；確認是否另有 helper，再決定 Bin 轉移及取消回復。
- [ ] 比較包裝批 ERP Qty=`SysQty` 與 MES Qty=`InInvNum` 的單位換算；以本地交易原子更新 FGD Basis／Detail／Inventory、WIP SEMI、Lot Qty、Component、CloseLot／ProductOperation 與 release log。
- [ ] 沿用 WP605／WP201 references 後仍須比對 PostgreSQL live schema／migration；本功能無新 Oracle table DDL，桌機沒有連 PostgreSQL、未實作或驗收正式 API／schema。

## 公司端交接：FGDSTOCKIN_T100_CANCEL 取消入庫 T100 ERP（WP620，2026-11-21）

公司端依 T100 Cancel Form／Designer、`wsINV`、`wsERP_T100`、`clsInventory` 完成 FGD／SCRIN 整張入庫取消的 DEV 假資料流程及[WP620 規格](docs/modules/WIP/取消入庫T100_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInT100Cancel`；原 Function 未掛正式 `MenuItem`。DEV 模擬 RequestType 0／1、ERP 失敗還原 State=2、ERP 成功但 MES 回復失敗保留 State=-2；原查詢缺 ERP source 篩選，DEV 阻擋非 T100 來源以展示正式端必要的防護。不連 T100／MES Service 或資料庫。

- [ ] 實作時重新查核 ERP source、State=2、ERPNo、單據版本／明細與 ACL；避免把 E10ICD 等其他 ERP 單送往 T100。採單據鎖及 idempotency key。
- [ ] T100 FGD 使用 RequestType=0，SCRIN 使用 RequestType=1；僅 code=0 後回復 MES。timeout／unknown outcome 先查 ERP 或進對帳佇列，不盲目重送。
- [ ] 將 ERP 結果及 MES 庫存／Lot／Component／動態 Bin／BookingFlag 回復協調成可恢復流程。ERP 成功、MES 失敗時保留待恢復狀態；比對 live schema 後在本地交易完成回復與最終狀態。
- [ ] `TBLINVFGDINBIN` Oracle metadata 沿 WP621／WP623 待公司端使用安全 profile 唯讀補查；缺項確認前不猜 CREATE-only SQL／migration。

## 公司端交接：FGDSTOCKIN_T100 入庫至 T100 ERP（WP616，2026-11-21）

公司端依 T100 Stock In Form／Designer、`wsERP_T100`、`wsMES`、`clsWIP`／`clsINV` 完成良品整列入庫的 DEV 假資料流程及[WP616 規格](docs/modules/WIP/入庫至T100_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInT100`；原 Function 未掛正式 `MenuItem`。DEV 模擬 T100 Sheet Type、MO／來源 WIP Lot、Select／UnSelect、ERP code W 警告續行、ERP fail 與 MES fail 後 ERP 取消；不連 T100／MES Service 或資料庫。

- [ ] 比對 PostgreSQL live schema／migration 與 WP201／WP605 references；本項使用的 Oracle source DDL／CREATE 已在 repository，桌機未連 Oracle，沒有新增 CREATE-only。
- [ ] 核實 T100 實際 ERP Web Service `stockin.data.create` 在現行 `ERP_Stock_IN` 呼叫未傳 `auto` 時的確認／過帳預設；Form 區域變數 `auto=N` 沒有被 serialise。
- [ ] 以 idempotency key／來源 Lot row version 預約所有 WIP rows，重查工單、來源庫、Qty、BookingFlag、目的庫與權限；ERP W 按既有合同保留警告並允許 MES。
- [ ] ERP timeout／unknown outcome 先查 ERP 單；MES 失敗後 ERP cancel 必須持久化其成功／失敗，取消失敗不能只釋放 BookingFlag 後允許重送。確認 MES Basis／Detail／FGD Inventory／WIP SEMI／Component／Lot Qty／CloseLot／ProductOperation／Log 在本地交易內原子更新。
- [ ] 核對 T100 多 MO 及 `OrgLotNoToERP` 設定；正式 ACL／MenuItem 待確認。公司端未連 PostgreSQL、未實作或驗收正式 API／schema、未 SSH 部署。

## 公司端交接：FGDSTOCKIN_E10ICD_CANCEL 取消入庫 E10ICD ERP（WP623，2026-11-21）

公司端依取消 Form／inline Designer、`wsINV`、`wsERP_E10ICD`、`clsInventory` 完成良品 FGD 與不良品 SCRIN 整張取消的 DEV 假資料流程及[WP623 規格](docs/modules/WIP/取消入庫E10ICD_ERP.md)。Fallback 路由為 `/YMES/WIP/FgdStockInE10IcdCancel`；原 Function 未掛正式 `MenuItem`。DEV 模擬 State=2 且 ERPNo 非空守門、ERP RequestType 0／1、ERP 失敗還原、ERP code=0 後 MES 回復與 State=-2 失敗待恢復；不呼叫 ERP／MES 或資料庫。

- [ ] 比對 PostgreSQL live schema／migration 及 WP201／WP603／WP605／WP215 已有 refs；`TBLINVFGDINBIN` Oracle metadata 沿 WP621 缺項，需安全公司端 profile 唯讀補查後再確認 FGD 動態 Bin 回復欄位，不猜 CREATE-only。
- [ ] 設計帶 idempotency key／期望版本的單據級取消命令，重查權限、State=2、ERPNo、單據種類與明細並鎖單；ERP 良品 RequestType=0、不良品=1，僅 code=0 才執行 MES 回復。
- [ ] ERP timeout／unknown outcome 必須先對帳，不能盲重送或重新開放；ERP 成功而 MES 失敗時保留可恢復狀態，補償良品／不良品庫存、Component／動態 Bin、線邊來源庫、BookingFlag、Lot Qty／Log 與最終 State。
- [ ] 修正舊 `UpdINVInfo` 每步覆寫 `OrgState` 的問題；家用端保存原始狀態與取消操作歷程。公司端未連 PostgreSQL、未實作正式 API／schema、未 SSH 部署。

## 家用正式站部署：API/Web（2026-11-21）

- [x] 先同步公司端最新 `main`（`262613e`），再以合併後來源建置；Web 預建 Dockerfile 補上 Nginx 非 root 可讀權限。
- [x] 部署後公司端又提交 `bc61853`（WP621 E10ICD 入庫 UI／規格）；已以正常 Git merge 保留。該頁未包含在本次正式映像，完成下方 WP621 API/schema work 及驗收前不部署。
- [x] 正式站 API 與 Web 分開切換。API image `80a35f682865…`、Web image `d262d8d1f97f…`；保留回復標籤 `rollback-before-09bd776-a2`。未執行資料庫 DDL，未碰其他專案容器。
- [x] 正式 API readiness、Web/API 路由均為 HTTP 200；公開 JS/CSS 與建置雜湊、大小及 MIME 相符；正式環境 API base 是 `/YMES/API`，小寫 `/ymes/` 回 404。正式資料庫既有 0063/0064 結構已唯讀核實。
- [ ] GUI 操作驗收未完成：Chrome 已載入正式登入頁，輔助樹顯示正式區、測試區、開發區及 Demo 登入；畫面擷取回報 `window capture timed out`，按 Demo 回報 `coordinate input geometry is unavailable`，所以沒有完成 Demo 登入或主選單操作。瀏覽器工具恢復後先補登入、Menu、WIP 頁操作，再標記 GUI 完成。

## 公司端交接：SCRAPSTOCKIN_E10ICD 不良品入庫至 E10ICD ERP（WP622，2026-11-21）

公司端依 Scrap Stock In Form／inline Designer、`wsERP_E10ICD`、`wsMES`、`clsERP`／`clsWIP`／`clsINV` 完成 Source 0 Lot Scrap、Source 1 Component Scrap、Source 2 Fail Bin 的 DEV 假資料流程及[WP622 規格](docs/modules/WIP/不良品入庫至E10ICD_ERP.md)。本機 fallback DEV 路由為 `/YMES/WIP/ScrapStockInE10Icd`；原 Function `SCRAPSTOCKIN_E10ICD` 未掛正式 `MenuItem`。DEV 模擬 ERP→MES、ERP W 非成功及 MES 失敗 ERP 取消，不連 E10ICD、MES Service 或資料庫。

- [ ] 比對 YMES PostgreSQL live schema、migration 與 YMES 擴充欄；重用 WP201／WP603／WP215、QC／PRD／OE／OP references。
- [ ] `TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` 為 Core 明確寫入的目的表，但 repository 尚無 Oracle DDL／metadata。桌機沒有可用受保護 Oracle profile；待安全 profile 後由公司端只讀查 DDL／欄位／索引／約束／trigger，查前不猜欄、不產 CREATE-only、不做 migration。
- [ ] 確認 `ExecuteSQLNoneQuery_UPD` 的原子性；正式交易需同步 SCRIN Basis／Detail、目的 Scrap Inventory、Component／FailBin 搬移、來源刪除、ReverseHistory、BookingFlag 與 Lot 結批。
- [ ] 實作授權保護的 MO／LotSerial／Error／Component／FailBin／Inventory／Locator／ERP Doc Type 查詢與 idempotent Create／Cancel 命令。服務端重查 Source、Qty、狀態、MO ERP eligibility、BookingFlag、庫別型別、BoxNo／DateCode 及動態 Bin；來源可依 Source 0／1／2 不同，保留明確來源鍵及 row version。
- [ ] 以來源列預約鎖防止重複入庫。舊 Core 會跨 Source 0／1／2 用 LotSerial 查核並對多種表設 BookingFlag，且前端傳 `AllIn=True`；測清楚來源副作用後再設計相容或修正。ERP 必須 code=0 才進 MES；MES 失敗須可追蹤 ERP 取消是否成功與 timeout unknown outcome。
- [ ] 確認 `SCRAPSTOCKIN_E10ICD` 正式 ACL／選單是否開放；fallback DEV 可見不代表正式授權。

## 公司端交接：FGDSTOCKIN_E10ICD 入庫至 E10ICD ERP（WP621，2026-11-21）

公司端依 `frmInventory_FGDStockIN_E10ICD`／Designer、E10ICD ERP Service、`wsMES`、`clsERP`／`clsWIP`／`clsINV` 完成 DEV 假資料流程與[WP621 規格](docs/modules/WIP/入庫至E10ICD_ERP.md)。本機 fallback DEV 頁為 `/YMES/WIP/FgdStockInE10Icd`；原 Function `FGDSTOCKIN_E10ICD` 未掛正式 `MenuItem`。DEV 只模擬 ERP 建單→MES 入庫、ERP W 警告續行及失敗補償，不連 E10ICD、MES Service 或資料庫。

- [ ] 比對 YMES PostgreSQL live schema、migration 與現有擴充欄，重用 WP605 的 `TBLINVFGDINBASIS`／Detail／Component、WP201 的 FGD/WIP Semi Inventory、WP603 Component 及既有 Lot／MO／Product／OP／Locator references。
- [ ] `TBLINVFGDINBIN` 為 Core 明確讀寫的 Bin 入庫表，但 repository 尚無 Oracle DDL／metadata；公司桌機目前沒有可用受保護 Oracle profile。待取得安全 profile 後由公司端只讀補查 DDL／欄位／索引／約束／trigger；未查前不猜欄、不產 CREATE-only、不做 migration。
- [ ] 確認 `ExecuteSQLNoneQuery_UPD` 的原子性；`clsINV.MOStockIn_E10` 的 transaction 範例已註解。正式命令須同時維持 ERP 建單、MES 入庫單、FGD／WIP Qty、LotState CurQty／SysQty、Component／Bin、結批、操作／下線歷程及包裝清除的一致性。
- [ ] 提供授權保護 MO／Lot／Inventory／Locator／ERP 單別查詢與具 idempotency／預約鎖的建立命令。伺服器重查 Qty、Lot status、BookingFlag、MO ERP/state／廠區、庫房類別、BoxNo／DateCode、包裝成員及 Bin／Component，不信任瀏覽器數量或狀態；保留 ERP `W` 可續行，MES 失敗時可靠取消 ERP 單，處理取消失敗與 timeout unknown outcome。
- [ ] 確認 `FGDSTOCKIN_E10ICD` 的正式 ACL／選單是否開放；未確認前不要將 fallback DEV 可見視為正式授權。

## 公司端交接：P_BUMP MAP MERGE Bump MAP 疊圖作業（WPP07，2026-11-21）

公司端依 IC/CP `frmMergeStandardMAP`／MAP Viewer、`wsWIP`／`wsPRD`／`wsSYS` 及 `clsLot`／`clsProduct`／`clsSYSEng` 完成 DEV 假資料互動與[WPP07 規格](docs/modules/WIP/Bump_MAP疊圖作業.md)。Form 涵蓋 Lot/Component/Wafer、Golden/Bump MAP、核准規則與模板、Merge、UndoFrozen、UFMAP 檢視。DEV 不處理真實 MAP binary／FTP、不呼叫 API。

- [ ] Pull 最新 `main` 並比對 live schema；既有 `TBL_BPRDMERGEMAPRULE`／Detail／Sub、`TBLWIPCOMPONENTSTATE`、`TBLWIPCOMPONENTBASIS` 依現有 references 核實與保留 YMES 擴充欄位。
- [ ] 公司端尚缺 Oracle metadata 的六張表是 `TBL_PWIPGOLDENMAPFILESTATE`、`TBL_PWIPBUMPMAPFILESTATE`、`TBL_PWIPBUMPMAPMERGESTATE`、`TBL_PWIPBUMPMAPMERGELOG`、`TBL_BSYSMAPTEMPLATEDETAIL`、`TBL_BSYSMAPTEMPLATEDETAILB`；收到唯讀 Oracle metadata／CREATE-only handoff 前，不替這六表建 migration 或推測欄位。
- [ ] 核實 CP 檔案服務與正式 MAP engine 的部署／呼叫介面，再做授權保護的 Lot/component/file/rule/template 查詢及 Merge／UndoFrozen commands。Merge 需涵蓋檔案下載、WaferLot／ID／方向／座標驗證、輸出 checksum／上傳確認、原子 current/log/component 更新與可恢復失敗；不可把公司端 DEV 假流程當正式疊圖 API。
- [ ] `clsLot.LoadBumpMapMergeState` 有 BumpMergeSerial／WaferId 條件拼接錯誤且使用字串 SQL；新 API 用參數化條件及穩定排序。Service 的 EventLog 在 Core 狀態寫入後另呼叫；確認 `ExecuteSQLNoneQuery_UPD` 交易行為並避免假設跨檔案服務與 DB 原子。

## 公司端交接：Skip Inspection Setting 免檢批設定（2026-11-21）

公司端依 THEIL 主 Form、新增／狀態視窗、`wsWIP`、`clsLot` 與 WP201 執行流程完成 DEV 假資料頁及[免檢批設定規格](docs/modules/WIP/免檢批設定.md)。清冊 FunctionKey 空白。直接表 `XX_TBLSPCWAIVE` 的 Oracle DDL 與 CREATE-only 已存在於 repository reference；本項沒有新增 schema，也沒有查 Oracle 業務資料或連 YMES PostgreSQL。

- [ ] Pull 最新 `main` 後比對 `ymes.xx_tblspcwaive` live schema；表已存在且欄位／`(lotno, opno)` 複合 PK 相符時不新增 migration，保留 YMES 擴充欄位。
- [ ] 確認實際 Function ACL 對應。實作授權保護的篩選、Lot／OP 候選、Lot×OP 批次建立、單列狀態變更及 WP201 Status=0 免檢原因查詢；所有查詢／寫入參數化，服務端重驗候選 Lot、OP 與輸入長度。
- [ ] 建立交易需回報複合 PK 衝突並保持整批原子；狀態更新帶預期舊狀態，建立／狀態原因、authenticated actor、時間和事件記錄採一致交易或可靠 outbox。UI 的 `9=全部` 僅為查詢選項，不是可存狀態。
- [ ] WP201 在當前 LotNo+OPNo 查到 Status=0 時顯示免檢原因，依規格維持執行流程語義；先核實正式 UI 契約再整合，不把免檢主檔查詢與報工命令混為一個未驗證的寫入。

## 公司端交接：WORKORDER PRINT 工令單列印（2026-11-21）

公司端依 THEIL `frmWP_WorkorderPrint_THEIL_BU1`／ActiveReport、`wsWIP.LoadWorkorder`／`LoadMOTypeDesc`／`LoadLotDetail`、`clsLot` 完成 DEV 假資料查詢及工令單預覽；規格見 [工令單列印](docs/modules/WIP/工令單列印.md)。Function inventory 的 FunctionKey 為空白。資料來源皆沿用既有 Oracle references，本次沒有新增表或資料庫 DDL；公司端未連 YMES PostgreSQL。

- [ ] Pull 最新 `main` 並先比對 YMES live schema；重用既有 `TBLWIPLOTSTATE`、`TBLWIPLOTBASIS`、`TBLOEMOBASIS`、產品／OP／MO Type、Lot Property 結構，不新增專屬 migration。
- [ ] 先確認 `WORKORDER PRINT` 的正式 Function ACL 對應。提供授權保護的唯讀 MO／建立日期批號查詢與報表預覽 API；服務端重查 MO 與所選批號，拒絕跨 MO，參數化 LotNo 清單／日期／MO，回傳來源欄位及 SAP_PCSQTY、SAP_ARGQTY、SAP_PRDQTY 合計。
- [ ] 核實來源 Core「只抓已下線」註解與 SQL 未限制 `STATUS` 的落差；日期查詢時 Form 可能漏傳 MOTypeNo、報表 `PrePN` 欄未填的行為。若改正需保留來源相容規則並明確記錄，不要把猜測欄位當正式資料。
- [ ] 報表輸出可由正式端的報表元件或列印版頁面產生；此功能只讀，不需資料寫入 API 或資料表 migration。

## HOME最新：WP109 API186與0064套用驗證完成並同步GitHub；GUI／部署待完成（2026-11-21）

API185 暖連線鎖修正後的 API186 離線 build／publish 為 0 warning、0 error；隔離 PostgreSQL 14 HTTP 驗證 26 項通過。API185 原先 multipart 上傳 503，已確認是 MVC 表單 value provider 預讀 request body，API186 停用 Create／Edit 的三種預設 form value provider 後，73,729-byte 附件上傳、下載、替換、保留與清除都通過。結果 SHA-256 `6c5de6392d2727f784ba2baa56ca5b368d09c6748101b9516eeede5947fea3cd`；`YMes.Api.dll` SHA-256 `bcc8529c84ed657cb100c45854ad6b01f8c212ab758565315a0f13d92cce6b85`。

- [x] API186 隔離 HTTP：登入／session、讀取與版本、建立／刪除、附件上傳／替換／保留／清除、舊零長度 BLOB、過期版本衝突、核准、作廢與 EventLog 共 26 項通過。
- [x] QA 合成資料已在 run `c75d52550387b135b57d5cadf993c6cb` 的獨立 PG14 clone 內，以 run ID、system identifier、資料目錄和精確資料條件守衛後清除；另一唯讀連線確認十個相關表及三個 SWR 測試編號皆為 0。清理後驗證 SQL SHA-256 `AB311705F08011EF05D77DA61B7B38E478982303985304B71FFF94C087D4368D`。API186 QA 容器已停止；隔離 PostgreSQL clone 保留。
- [x] Provider migration `0064_wp109_writer_provider.sql` 已先在隔離 QA clone 回滾測試，再在家用 PostgreSQL clone 的 `ymes` main instance（PG14／port 5432／system ID `7483802566828922547`）備份後套用並提交；這是鼎華 MES Oracle 原生資料庫的 PostgreSQL 復刻，不是公司 Oracle。0062 SWR native19、full16 allowlist／32 欄／12 defaults／最小 ACL 與獨立唯讀 post verifier 全通過。家用 clone full backup 645,040 bytes，SHA-256 `9747846d56708a00a684545a00a5f9933a388f973a926a5a3839ed5df21880e7`，保留於遠端 `/var/tmp/ymes-main-before-0064-a1.dump`；restore-list 1,834 TOC 項目，SHA-256 `46e6cb7bc3e3235a50ea623a509784619fac6096fd44ca886a6b7818099a3c88`。備份、restore-list、catalog snapshot 及執行 SQL 均已設遠端權限 600。fresh pre-provider full16 canonical SHA-256 `67ccdae32b8607fc626e373cbbc0e641659d1d4e87f3ee84225163860b567b86`；migration 結果 SHA-256 `dbf448887e89772dfed8db62dc80a862e4f5fa70a32f214c337969b10ffb42ea`；post verifier SHA-256 `ce8b034b0e592edc4a22b0eadaff4fcda220af9b99706e23a0382d338fe3d9ea`；post full16 SHA-256 `929ecf57a82c9d51528d9d643f4f9ec57cbe1cb9d449e125a9e4496620801d1e`。兩張新表 7／25 欄，沒有業務資料寫入。
- [x] 程式與 migration 以 feature branch `codex/homepc-wp109-writer-20261121`（`9eab9ebe7420450e6ca08ba919d95a52081ba85a`）正常合併至 GitHub `main`（merge commit `662c5cf27fdc10b8110a3fcde4d872df063373f1`）；handoff 修訂亦以正常 fast-forward 提交，沒有 force push。
- [ ] app-browser GUI 操作驗證：使用者畫面確認先前的 `ERR_CONNECTION_REFUSED` 是本機服務未啟動，不是瀏覽器故障。原 `/api186-http-harness.html` 不在 repo 且回 404；已啟動 Vite `127.0.0.1:18749`，`/YMES/` 與 `src/main.ts` 回 200，並啟動 Development API `127.0.0.1:5080`（local demo auth／fake provider，不連資料庫）；Demo login 與 session API 回 200。已將 app-browser 開啟目標改為 `/YMES/`。Computer-use 截圖通道仍逾時，尚待逐項視覺操作驗收。
- [x] 家用 PostgreSQL clone 的 main `ymes` 已完成 0064；公司 Oracle 原生 MES 未連線、未改動。
- [ ] 正式 API／Web 尚未部署 API186 與對應 writer 頁面；本次只啟動本機 Development fake provider，沒有連接 PostgreSQL／Oracle 或變更正式站。完成 app-browser 視覺操作驗收後再部署，並重新讀 Office／Home 待辦接手。

## 前一輪待辦紀錄（保留歷史）

## HOME最新：Office8e1正式站驗收與GitHub同步完成，接續WP109真QA（2026-11-21）

正式Web284（d670221e…）已切換並完成app-browser；API176（49765314…）與DB0063維持不變。Office ES102／ES409／ES106與Home提交以6cadf4335384a205418812dd82664dd52b7ee04a正常雙親merge，驗收文件與三份EMS模組文件在1c7e88f74577cf5b827ca880eacfb104dd6d8333；GitHub main 已讀回此版本，沒有 force。469來源綁定通過（receipt SHA-256 `f68c5238a2d691715dd31bec93abbd3a4ee9da5400caa3c408cdca8ab25673df`）。WP109 API185 暖連線修正已獨立隔離驗證，仍是本機候選，未合入 main／未部署。

正式頁面的ES102／ES409／ES106仍是「正式環境尚未開放」提示和Close，不表示三個正式API已完成。42個GUI觀察、141個條件通過；7個初次false保留並由後續事件復驗，最後warn/error為空、無業務資料寫入。完整紀錄：`docs/qa/20261121-office8e1-web284.md`；本機proof SHA-256 `754ad31fadfde4295362cabe5f5fc1a382ca54a8a80d8ea7f1cf7582adff752b`。

- [x] Office8e1正常合併、Web284完整建置與正式切換、獨立讀回及app-browser三層選單／ET202查詢與查找／WP109唯讀Query／自適應與空白首頁驗收完成。
- [x] 三份EMS模組.md補上正式端範圍；驗收QA已同步GitHub main，Office端近期沒有更新於92317e6之後的新提交。
- [ ] WP109 writer：API185離線 build／publish 0 warning／0 error；隔離PG14/Npgsql的冷／暖Prepare、EventLog同交易rollback、Detail S/D鎖共6項通過。獨立Post catalog canonical SHA `65725bfca5faa4f7d5815a7358013b79f0f0d09d120acacd4ebe90a7f2018e1b` 與基準一致，EventLog/SystemParameter各0列。完整API HTTP查詢／建立／編輯／複製／核准／作廢／附件、衝突與回滾仍待QA；正式UI頁目前維持寫入停用，尚未以API185驗收或部署。
- [ ] WP109 provider migration：原始 `IF (...) <> CASE ... END` 語法錯誤只在隔離QA副本以括號修正並通過；修正正式候選 SQL、重審source hash，再做fresh live schema、完整備份／restore-list、精確migration與獨立post。未對正式DB執行migration或grant。
- [ ] 71份舊驗證封存上傳被自動核准審查拒絕，指定內部部署／驗證資料與目的地的外傳授權不足；沒有上傳或繞道，保留既有CP33狀態，不重試同一傳輸。
- [ ] Office補ES204 CHAR_USED／CHAR_LENGTH、互斥key、InitEQPSerialNo、Reverse／結算／簽退與Adapter完整來源；另保留Lot39精確DDL與helper補證。HOME無原Oracle連線，不把此來源查核交給PG猜測。

- [x] Office8e1正常合併、Web284完整建置與正式切換、獨立讀回及app-browser三層選單／ET202查詢與查找／WP109唯讀Query／自適應與空白首頁驗收完成。
- [x] 三份EMS模組.md補上正式端範圍；驗收QA已寫入GitHub，main與家用feature均已同步。
- [ ] WP109 writer：完成隔離PG14 QA冷／暖實際Prepare guard、EventLog同交易、Detail S/D鎖與附件／端到端操作。API184離線build已通過，WP109 UI/API仍在本機隔離工作分支，尚未合入正式分支；目前尚未建立QA cluster、未驗證warm guard、未部署或寫入正式資料。warm guard若失敗須修產品實作並重build，不可加權限或略過鎖來造通過。
- [ ] WP109 provider 0064：核對Web284／API版本的fresh前置、備份／restore list、精確migration與獨立post；目前僅有提案，未上傳、未執行；資料庫migration與grant尚未執行。
- [ ] 71份舊驗證封存上傳被自動核准審查拒絕，指定內部部署／驗證資料與目的地的外傳授權不足；沒有上傳或繞道，先跳過並保留CP33歷史狀態。

## 下方保留完整先前進度作歷史資料；本節為目前狀態。
## 先前進度原文（保留歷史）

## 目前狀態：ET202操作驗收完成，Git／checkpoint同步中（2026-11-21）

正式API176／497、Web276修正版4f、DB0063。Present API127 HTTP／600檢查及獨立44zero；新app-browser90raw／83成功、7測試觀察false原件及實際復驗；首輪Web76a的真focus／Escape缺陷在新Web實操作確認修復。五個項目全GUI建刪、三父API清理，最後獨立39範圍全0。正常auth／原生三層選單／兩尺寸／四個舊頁read smoke通過。詳[本輪QA](docs/qa/20261121-et202-os-items.md)與[ET202維護](docs/modules/ENT/外包項目設定.md)。下方早期pending與Web76a描述保留作歷史，本段優先。

- [x] HOME完成ET202原生11欄建立、最小權限、API與正式UI及限定app-browser逐操作驗收；API／GUI／非GUI fixture與精確清理分列。
- [x] HOME完成Auth A2正常還原／Demo／重載／原生menu與ET202焦點修正；未宣稱故障注入或舊偶發延遲唯一根因。
- [ ] HOME完成正常fetch／merge、461來源綁定、feature／main同步及新checkpoint，保留Office同時開發成果；完成後再讀兩台TODO接下一項。
- [ ] HOME接續WP109空Lot writer：原子取號、附件、Edit／Copy／Approve／Void；8份source候選已備妥，尚未整合／真PG／API／GUI驗收或部署。新權限需新native verifier／精確catalog allowlist，不沿用舊Serial app ACL0收據。
- [ ] Office補最新Lot39與舊CREATE38的精確差異及原生來源；Oracle唯讀／UI契約由Office負責，HOME不猜建Lot表。其他已提供CREATE與原生規則不重複列缺件。
- [ ] HOME後續補未自然重現的unknown-write／Auth故障UI、legacy duplicate raw alias與大量列縱捲；有明確工具限制才列待辦，繼續可做工作，不等人員登入。

## 先前接手紀錄（保留原文）

## HOME目前進行中：ET202（2026-11-21）

API176／497653已上線，缺表完整測試40 HTTP／128檢查及獨立26zero通過；0063已完整備份、同交易建表／CRUD4 grant／native11／allow16及獨立post核對。現行schema catalog `2add5224b4e8b090191a8ef1b634ad99a58d7655a20004851507413bd3281c59`，Web276／76a已切換且獨立11項讀回通過，current-checkpoint33仍記錄前版0062／API502，尚未本版結案。首輪GUI有32成功／2真焦點缺陷／1恢復；三父已清、39範圍全0，正修picker查詢後焦點及Escape。詳[ET202 QA](docs/qa/20261121-et202-os-items.md)及[模組維護](docs/modules/ENT/外包項目設定.md)。下方原「0063未建」等描述為先前時點。

- [x] API176相容部署、Process退用／硬刪原生流程真測；完整備份後建立tblentositem原生11欄及最小CRUD4權限，既有結構／服務保持。
- [x] ET202 present API127 HTTP／600檢查、四次明列stamp-only fixture與八子／三父API清理，獨立44zero；Web276／public8真切換及獨立11項讀回。
- [ ] 修正首輪GUI picker查詢後focusBody／Escape無效（真事件33／34保留），新Web加上已獨立source審查的Auth A2載入順序保護；新scope完整app-browser，精確清理後補MD、正常fetch／merge／push，再看Office新接手事項。
- [ ] HOME修正登入還原／選單非同步狀態：source audit c98665a9…已確認auth/me尚待回應時Login／Demo會提前可按，以及loadMenuTree缺session／environment世代隔離、舊response可能覆寫新menu。先加明確還原中／失敗可重試與過期回應保護，下一版完整build／app-browser重載復驗；尚未證實為先前偶發事件唯一根因。工具做不到的個別操作列具體TODO後繼續，不等待使用者。
- [ ] 下一WP109 writer空Lot分支、原子取號／附件／核准／作廢；非空Lot依原生來源與真provider接續。Lot39／舊38差異由Office補證，HOME可先做空Lot與API／DB。

使用者離線期間已授權自行操作Demo與確認；只將工具確實無法完成之事項列待辦，不以無人登入停止。

## 公司端交接：ES106 設備狀態變更原因資料（2026-11-21）

公司端依 `frmEquipmentReasonBasis`／Def、`wsQC.Load/Add/Edit/Del/ApproveReasonBasis` 與 `clsQCBasis` 完成 DEV 假資料頁及 [ES106 規格](docs/modules/EMS/設備狀態變更原因資料.md)。功能涵蓋 ReasonType=6、四種 IssueState、子類別選擇、複製、Apply／Confirm、依狀態刪除與直接核准／送簽模擬。Oracle 原因主檔與子類別 DDL/metadata 已在 repository；未查 Oracle 業務列、未連 PostgreSQL。

- [ ] Pull 最新 `main` 後比對 `TBLQCREASONBASIS`／`TBLQCREASONSUBTYPEBASIS` live schema；僅缺表或欄位時規劃 forward migration，保留 YMES 擴充欄位。
- [ ] 核對本機 legacy wsQC 傳入 `DataStamp`／`Reviser`／`ReviseDate` named arguments，而 `clsQCBasis.cs` 宣告未包含這些參數且更新狀態 SQL 未寫稽核欄位的來源差異。正式 PUT／狀態 API 應在交易內以舊 DataStamp 條件更新並遞增，寫入修訂人／時間。
- [ ] 完成登入與 ES106 權限保護的原因／Active EQP subtype 查詢、ReasonType 固定為 6 的新增與未凍結編輯；ReasonNo/name/description/subtype 長度分別遵守 20/100/255/50，ReasonLevel 僅 0–9。查詢及更新全部參數化。
- [ ] 實作狀態 API：未凍結／停用資料刪除前檢查參照；已核准資料轉 Unused；簽核中拒絕刪除。核准需依 IssueJobMap 直接 Active 或建立 Pending Issue Master、approve/reject 狀態 SQL、事件／通知，確保 DB 狀態原子及通知可靠；記錄操作者與前後值。

## 公司端交接：ES409 設備狀態期間統計查詢（2026-11-21）

公司端已依標準 Form、`wsRPT.LoadEQPRealTimeReport`／`LoadEQPRealTimeReport_Chamber` 與 Core `clsReport` 完成 ES409 DEV 假資料查詢頁和 [來源規格](docs/modules/EMS/設備狀態期間統計查詢.md)。DEV 模擬狀態／設備多選、區間裁切、分鐘向上取整，以及由 Chamber 子設備投影母設備欄位；不連家用 PostgreSQL、不呼叫正式 API。所需來源結構都已有 repository Oracle DDL/metadata 參考，沒有查 Oracle 使用者資料，沒有新增表。

- [ ] Pull 最新 `main` 後比對 YMES live schema；依 ES409 規格提供登入及 FunctionKey 權限保護的唯讀查詢 API，參數化日期區間、設備／狀態清單與 Chamber 模式，限制最大查詢區間及設備範圍。
- [ ] 查詢時合併 `TBLEMSEQUIPMENTSTATELOG` 與 `TBLEMSEQUIPMENTSTATE`，依起訖 overlap 篩選並裁切邊界；套用狀態名稱、區域映射及 Core `CEIL` 分鐘規則，穩定依區域／設備類型／設備／開始時間排序。
- [ ] Chamber 模式核對 `TBLSMDAREARELATION` 與 `TBLEQPEQUIPMENTBASIS` 的母子關係及 `ChamberFlag`；保留母設備和 ChamberNo 顯示欄位。此功能只讀現有資料，無需專屬 migration 或寫入 API；不可照抄 Core 的 SQL 字串拼接。

## 公司端交接：ES102 設備狀態控制設定（2026-11-21）

公司端已依標準 Form／Designer、`wsEQP.LoadStateControlType`／`UpdStateControlType`、`clsEQPStateControl` 完成三分頁、七個固定 ControlType／ControlNo、DEV 狀態選擇及 Apply／Confirm 模擬；沒有連 YMES PostgreSQL 或做正式 API。詳細 [ES102 規格](docs/modules/EMS/設備狀態控制設定.md)。既有 `database/reference/mestest_equipment_state_references_source_ddl.sql` 與 `mestest_equipment_state_references_create.sql` 已包含 `TBLEQPSTATECONTROL`，本項沒有新增 Oracle DDL。

- [ ] Pull 最新 `main` 後先比對 PostgreSQL live schema/migration；已存在表不重建，只在確認缺項後新增 forward migration。`TBLEQPSTATECONTROL` key 是 `(CONTROLTYPE, CONTROLNO)`，沒有來源 FK 或 unique EquipmentState 約束。
- [ ] 實作登入與 ES102 權限保護的 GET：回傳全部七個設定及狀態 lookup；只接受三種固定 ControlType、七個固定 ControlNo，狀態需存在於 `TBLEQPSTATEBASIS` 且名稱與主檔一致。
- [ ] 實作批次 PUT：只收已修改群組的完整控制項清單，交易內比對目前值／鎖定後使用參數化 upsert；任一控制項失敗整個群組回滾。不要複製 WinForms Service 的逐列 Delete+Add 部分寫入，也不要把翻譯後分頁標題當 key。
- [ ] 記錄 actor、request id 及各控制鍵前後值；核對 `STATENAME` Oracle BYTE／CHAR 長度語意。來源 Form `ContinueToProduction` 名稱檢查有引用錯欄位，正式驗證須檢查該列本身。

## 公司端交接：ES204 改變設備狀態（2026-11-21）

公司端已依 THEIL Form／STD wsEMS／clsEMSEquipment 完成 DEV 假資料畫面與 [原始碼規格](docs/modules/EMS/改變設備狀態.md)，未連家用 PostgreSQL，也未實作正式 API。原因歷程 `TBLEMSEQUIPMENTREASONS` 的 Oracle 原始 `GET_DDL` 回 `ORA-03135`；可用唯讀字典 metadata 與 CREATE-only 參考位於 `database/reference/mestest_es204_equipment_reasons_metadata.txt`、`database/reference/mestest_es204_equipment_reasons_create.sql`。

- [ ] 比對 PostgreSQL live schema/migration，僅在缺表時依 CREATE-only 設計 forward migration；核對表欄位及 Oracle BYTE/CHAR 長度語意。
- [ ] 依 ES204 規格完成登入保護的設備／狀態／互斥／原因／保養工單／Lot lookup 與狀態變更 API。交易中重讀並鎖定設備目前序號，防止並發 serial／狀態覆寫；整合設備歷程、原因、Lot／設備關聯、用料結算及操作員簽退，失敗完整回滾。
- [ ] 驗證 CheckInConfirm、ChangePRODEQPStateMode、PMWoReleased、`pModule != KMI` 的來源規則與 production side effects；不要直接套 Oracle 重建 DDL或把 DEV 狀態當正式資料。

## Office 8ff整合結案及main同步完成（2026-11-21）

本輪已完成結案與main同步。current-checkpoint SHA `33bd353dd129962d2e2cf196aa5b628cdc4b176be1495e58972621f69dd78f58`，另一連線獨立raw下載／64個保護欄位／歷史與服務核對通過，回條SHA `49925286533ee240e7a1b8a5f58513059c69b8646dd185aec8cfb4a51edb1a3d`。原7d0完整raw備份、API與DB所有段落、WP109讀取版結案及history前綴保持。發行對應code X `7c5ba15af622fe4776fecfb9bddc96cabd250ee5`、status Y `23559909602900349e5bf6018a0e9027bf9d64c2`，均真452來源綁定／正常推送／直接ref核回；main已到Y，`mainSyncConfirmed=true/mainSyncPending=false`。本次後補文件提交不改此發行對應。八個WIP provider、global MES QA與偶發登入載入根因仍待辦。



正式Web274／03e已切換，API172／502與DB0062保持。完整typecheck／Vite建置通過，獨立公開七項與完整來源／runtime／catalog核對通過（回條71f57c04…）。Root自行Demo完成八個原生入口及deep reload／Close，並核KD、三層折疊／72px圖示、透明auto scrollbar、WP109 Query與客戶查找回歸。proof437b05ca…為48raw／45成功，三筆false及復驗保留；沒有業務寫入。詳[本輪QA](docs/qa/20261121-office8ff-integration.md)。八個provider仍待真API，不能把DEV隔離當交易完成。

- [x] 完成本輪正常feature／main同步與metadata，原Office九提交保留；結案後再次fetch確認main仍為Y，無新Office提交。接續ET202實作，不等待人員登入。
- [ ] HOME追查快速goto／reload時偶發回登入頁及原生menu載入超過15秒：本輪自行Demo／fresh reload已恢復，最後console空；根因未明，不冒稱已修。取前端與API／代理時間對照後修復，再以app-browser重載復驗；不等待使用者操作。
- [ ] ET202：採用已獨立準備的176後端A2／原生11欄，freshmetadata後決定migration與權限，完成真API／PG／UI與獨立清理。Process只允0／−1硬刪的窄修需一併真驗。
- [ ] WP109 writer先做原生允許空Lot分支；非空Lot缺真provider明確503。Office待補最新Lot39與舊DDL38差異，HOME不猜表。EventLog／Parameter已有來源，先核fresh現況再施作。

使用者睡眠期間已授權自行操作／Demo；工具確實無法完成的步驟記具體TODO並繼續，不以等登入中止開發。以下保留前時點完整歷史。


## 正在接手：Office 8ff整合（2026-11-21）

已正常合併HOME6e9與Office8ff九提交，保留八個新WIP原型與WP109來源補證；唯一模組文件衝突完整保留雙方。Web274候選已採用原型lazy DEV隔離及App六組新增，原257來源／WP109讀取功能保持。完整建置、部署、本輪app-browser與main同步仍待完成，詳[本輪QA](docs/qa/20261121-office8ff-integration.md)。現行正式仍API502／Webc828／DB0062／checkpoint7d0。

- [ ] 完成八入口及深連結／reload／Close、KD／選單、自行Demo與WP109讀取回歸，驗證後正常同步main，再檢查新交接。
- [ ] ET202外包項目來源完整，先補Process硬刪引用與協作鎖，完成真API／PG／UI；候選正獨立準備，尚未採用／建表／部署。
- [ ] WP109 writer可先完成原生允許的空Lot分支、原子取號／附件／核准／作廢；非空Lot缺真provider明確503。先核EventLog7／Parameter25現況與原生SQL，不猜Lot39與舊DDL38的差異。Office只補精確來源，HOME自行完成API／DB。

使用者已授權自行Demo與所有開發、驗收及正常發行；工具確實無法操作時寫具體待辦並繼續其他工作，不等使用者登入。

本輪讀取版已完成metadata結案及獨立下載核對。新current-checkpoint SHA `7d0bc62f032e2e24f243fe5b016fa629ec781ed603202d5a082770db4492a4b3`，獨立回條SHA `5da236c29ef699108996d31a5946155b67345d47e07e9f762cbfc114098d607e`；完整保留原496 raw備份、50項既有保護資料與歷史前綴。發行對應code X `50cf3a6dfb3cd6b310c35c4e98310b59c30eab0d`、status Y `f0d26dfb1d414786b483faabae545de1d075ce0f` 均通過436來源綁定、正常feature推送與直接ref讀回。此後純文件補記不改本次X／Y發行對應。main仍含Office新8ff工作，`mainSyncPending=true`；下一輪正常整合與新頁驗收，不以force覆寫。下方496／未封存／收尾待辦等段落保留為前一時點；本段是目前狀態。SWR writer與global MES QA未因此完成。

已驗證程式提交 X `50cf3a6dfb3cd6b310c35c4e98310b59c30eab0d` 完成436份來源綁定（SQL6／API172／Web258，回條SHA `2c04f82d3ba5de2e6c2f1f57c14921643160efdbbd8b100e6e59ccd315ed0759`），已正常推送 `codex/homepc-wp109-read-20261120` 並直接讀回GitHub ref一致，未force。驗收後fetch發現Office main已到 `8ff405290a7f55bfd1e2261f8d85dec97f363409`，新增9提交／8個WIP DEV入口與WP109來源；這些尚未混入本輪正式c828。接著正常合併並驗新入口，保留兩端成果；本輪status／metadata收尾與main整合分開記錄。

## WP109 正式讀取版驗收及清理完成（2026-11-21；Git／metadata收尾中）

HOME負責PostgreSQL復刻／API／PPK SSH／發行；Office負責MES原生Oracle唯讀來源、UI及原始流程契約。新工作在 `codex/homepc-wp109-read-20261120`，基於已同步main `101b86e`；未推送新版本。

- [x] 核對三張原生DDL／metadata與原先缺席狀態；完成0062三表19欄、原生驗證及精確catalog差異檢查，原SQL已保存migration／verification供維護。
- [x] 採用SWR引用相容A2與查詢／單張／附件串流API A2，完成離線API172建置，零警告／零錯誤。保留原167／168來源與收據；新172來源canonical `454fdebb005e9906fa21e307917a98c57c591d92c1a0c8e6dd936aecdd151ceb`。
- [x] 遠端私有封包核對及candidate-only build後，實際API映像 `502f88704b928ffcfd0b5dedf6e3c469707e301ac7d04e961be59ce19ca8c614` 已切換，nonroot／完整來源172與runtime11／ready通過；API切換當時WebADE／歷史checkpoint496保持，現行Web版本見下方A4部署紀錄。
- [x] 採用正式讀取UI A2，保留公司DEV元件原bytes；修正ASCII空白判定及OP查找原始陣列保留，獨立source/mock peer通過。本機完整Web258建置通過，原EPERM失敗及成功retry紀錄保留；此項是A2準備時點，下方A4已完成正式GUI。
- [x] API172先切換並驗證absent分支、既有Customer／OP增刪與no-Job流程：A3真39HTTP／51檢核，獨立五範圍全0。新完整備份後單交易套0062，另連線驗證native19／全catalog差異及最小權限，真回條SHA `1959a9d1792e4b3f21c1167f8abc7509e16b9764c59a237d65a2a730c8e68522`、catalog62d9；另11次present空表API通過。
- [x] 最終A4 Web258 `c8284e0ed4482a35ebdebc6de53c5ce5ad063ce517d62fa03604c5a40f677871` 已真正切換；source canonical `8eb5c2611459ff9992ba77dfd4ea591f74332751a1de7316a4732fd2738ba76e`，完整建置回條21bf5894…，活化log SHA `4533e0f636cf461f055c59e5c8d76f26898543f8dab91e15507c4ed1d7aae71e`。API502／DB0062不變。
- [x] 獨立公開／runtime七項讀回通過，11968 bytes／SHA `c577f57474c15c6357f14a767ea6851b9a515b08105ce58e65c18c5b2897f55b`；live/current／nonroot、172＋258來源、runtime11／公開6檔及native19／35／61／full16 catalog62d9前後保持。
- [x] Root已完成並封存本輪正式A4操作驗證：46筆原始事件、45筆有效GUI，原false #8是測試期望誤用Lot插入順序，#9／16已按原生C排序復驗；原紀錄保留。最終proof SHA `0982190be36e9feec7db9485a9bdc590666132b18ff76e66ea292d4faf6b3011`。自行Demo、七條件／四狀態、真查找與焦點／Escape、完整單張／limit1、多選與取消、PDF／binary原生下載交付、Close／重進／deep reload、兩個引用拒刪、設備／站點對應／ECN群組讀取回歸及KD／三層選單／72px圖示均已實作操作；836×891及原缺陷1280×720高度通過。OP原生確認造成工具中斷，使用者確認一次後Root核回409提示與資料保留，不把它寫成全程無人協助；後續不重播刪除。最後fresh logs為空。精確清理真14HTTP／29檢核通過（SHA `f31cec32698ab3ef29f24698f3d010163e38ed308d68f1c58b9070b147c30752`），另一唯讀連線26範圍全0（SHA `40abac063f1ec6ec22891195953a960c02707527096704ca2e1965e2189749d2`）；非GUI SQL主6／明細3清理與API父4筆不混算GUI。436來源對真提交的綁定、Git同步及新checkpoint仍待本輪收尾。
- [ ] 補本輪QA／模組MD與兩台TODO，驗收完成後才fetch最新Office、正常merge及同步GitHub，保留雙方工作並檢查新接手事項。
- [ ] Add/Edit/Copy/Approve/Void/upload不在本次讀取版冒充完成；待公司端補原生取號、Lot／EventLog／系統參數契約後接真正writer。SWR表建立後禁止回不相容API167或DROP表繞過引用。

- [ ] 讀取版收尾後整合Office最新 `8ff4052`（9提交／8個新WIP DEV入口及WP109原生契約）；保留雙方變更，正式入口須檢查DEV隔離、完整build／app-browser後再部署。WP109新增取號／lookup／audit來源先核，不繼續把已提供的DDL列為Office缺件。
- [ ] 後續維護：作業站原生window.confirm在app-browser有工具互卡風險，可沿Customer既有自訂確認框統一；本輪已由使用者確認一次並真核拒刪，這項是改善工具相容性，不是將本輪引用守門寫成未通過。若工具確實無法操作，按使用者2026-11-21授權記待辦並繼續其他工作，不等待人員登入。

前版ET101已完成的限定驗收、提交與checkpoint歷史如下，維持原紀錄。

## 最新完成：ET101＋Office e5f限定結案與main同步（2026-11-20）

Root已完成69份私有證據上傳、真正Ready計畫、A2 dry／apply及另一連線獨立下載核對；目前正式checkpoint為 `496ae4bdd3c1cd8d4ecea8d8c16bd9ac35bd843118f97ab965e03651737fd7e9`，獨立讀回收據SHA `3e98979bea11b013ed9e760cdc643fb1f0e4ea50331fd23d4b9c55e5382b534c`，本輪ET101限定結案已完成。API167／3f0bdc7c…、Web256／ade411cc…及DB0061穩定；catalog2349、native35、全部167＋256私有來源、46項原有欄位及history prefixes保持，原2c23 checkpoint的完整raw備份保留。

Code X `f82f183fcd3dfc78d5a690d3c43a15ec64f820b3` 與status Y `faf14b2fd2875464bf280294b681056433b19019` 均完成426來源綁定、正常feature／main推送及直接ref讀回；本批發行對應main提交為Y，`mainSyncConfirmed=true/mainSyncPending=false`。本次結案後文件補記不改本批X／Y紀錄，後續純文件提交不變更此發行對應。

本輪只完成已記錄ET101操作、12個Office WIP入口防護及指定smoke；GUI132raw／124驗證＝117GUI＋5獨立SQL journal項＋2背景API，另3解釋／復驗筆記及五原false保留；六份實際唯讀收據與獨立GUI21範圍全0分開記。global QA／WIP正式providers仍未完成。下一WP109僅reference-compat A2 prepared候選，未執DDL或部署，不代表SWR交易已交付。

以下原文中的收尾／pending為先前時點，完整保留為歷史；本節是目前結案狀態。


## 結案前接手紀錄：ET101＋Office e5f發行中（2026-11-20；原文保留）

- [x] ET106 writer完成正式API121／961、A1＋修正A2指定GUI與清理、codeXebc／statusY24ee409來源綁定及正常feature推送／直接ref讀回。完整checkpoint已真更新並獨立下載：`2c23af30ac36103f5393bc0ab23012845ad00c4258889ca24b3e206ef932513f`；F1／schema60／compat／E1／D1／ET105與舊GUI歷史保留，main待本輪Office整合。
- [x] 正常合併Office e5f的15提交／12新WIP入口，保留雙方source／TODO。九新元件正式DEV資料／查找／storage／命令隔離、KD／單標題與ET101真owner API／UI已Root採用；兩個獨立source peers通過，未以pure測試冒GUI。
- [x] 167 API／256 Web完整離線build／publish／封包通過；其餘160 API／243 Web沿已發原bytes。API3f0b與Web256 `ade411cc93a7a13c363bcb5b9c5b67169b93e5471d161bdb43432e99846ca323`均已真切換，獨立d1b29b02…核正常TLS七項／live-current／nonroot／167+256完整來源；API／DB／CP未因Web切換改變。
- [x] 0061 fullbackup／restore-list後真COMMIT，僅Customer2原生DEFAULT0與本表IUD，獨立READ ONLY／整體catalog／舊native35保持；沒有改業務資料或重建表。
- [x] 真0061＋候選來源核對後完成API-only切換3f0b／相容976 rollback備援；A1實產漏61守衛在執行前被Root發現並改新A2，原件保留。
- [x] A4真API全新UUID八組228 HTTP／1568 assertions通過（f01774be…），unknown=false／pending空；獨立READ ONLY d3db117e…核22範圍全0。User引用與完整workflow未交付／未測界線見QA；A2 HTTP0、A3 unknown及精準恢復原件保留，不回填成PASS，不停用任何FK。
- [x] Root已sealed正式Web256限定GUI（6f2499b5…）：132raw／127true＝124驗證（117GUI＋5唯讀SQL＋2背景API CAS）＋3resolution notes，五原false及復驗保留。Customer3／B/F各1全GUI建刪，Address1／Contact1全GUI新增再native cascade各1；GUI SQLfixture／cleanup0，獨立b990e2e4…核21範圍全0。十二WIP逐頁native／direct重進／reload／Close防護、四舊主檔read smoke與fresh Home已記，providers仍pending，未重跑全部舊CRUD／AreaMap／directOperatorLogin。
- [x] 獨立GUI peer90df59c8…／119項一致性及Root acceptance c61d5d87…完成；五筆獨立SQL journal事件＋事件49退休佐證，共六份唯讀收據。code X `f82f183fcd3dfc78d5a690d3c43a15ec64f820b3`真426綁定b4599bb9…，正常推送feature／main並直接讀回。公司main e5f正常快轉，未force；原package pending／失敗與舊checkpoint歷史保持。
- [ ] 本輪模組／QA／兩台TODO隨純文件status提交，綁定相同426來源並正常同步；再完成combined metadata。WIP／ERP／SAP／Issue／import未交付能力仍依各MD接手，不以入口隔離當交易完成。

HOME負責PG復刻／API／PPK SSH／發行；Office維持MES原生Oracle唯讀來源與UI。詳[ET101](docs/modules/ENT/客戶基本資料.md)。下方舊pending皆保留為當時歷史。


## 最新進度：ET106 writer A2指定驗收與清理通過（2026-11-20）

Root已完成API161／976單輪121 HTTP／961 assertions、獨立API13全0；Web246 A2／eb1已正式部署，GUI36 raw／32有效＝30GUI＋2SQL、四工具觀察已復驗，兩A1產品缺陷在A2真復驗。GUI地址／報關／BFT全GUI建刪，只有Customer SQL前置／清理各1；獨立13範圍全0，native35及完整catalog保持。A1 proof／缺陷、A2 proof／工具觀察及失敗log全部保留，不把API或pure測試當GUI。

Code X `ebc03a106ebd5d47a1a47bf6dcc172fc3bdb8ef1` 已409來源綁定PASS、正常feature推送與直接ref讀回同值。Post-GUI Office main已到e5f4466（d96後15提交／12新入口）；獨立361199be…合併目前source-only，未建置／防護／部署／GUI，不混本批246證據。

- [ ] Root完成文件Y／409綁定與feature同步、combined checkpoint及後續main整合；目前不預填Y／main／metadata完成。
- [ ] 下一ET101客戶owner writer：來源／backend A3及UI候選已本機準備，尚未正式整合、migration／API回歸／GUI／部署；UI原生驗證窄修及no-Job核准邊界依Root最新決策另存候選，先完成本批ET106。
- [ ] 未交付WIP／Issue／import等provider繼續依各模組來源接手，入口防護通過不等於真交易完成。

完整範圍見[ET106 writer QA](docs/qa/20261120-et106-writer.md)；下方所有既有歷史標題／待辦保留。



## 桌機交付 WP401–WP404、WP901–WP906（2026-11-20）

- [ ] 讀 [WP401 規格](docs/modules/WIP/外包單開立.md)，比對既有 OS Basis、OS Detail、Future OS references 與 live schema。完成正式 WP401 權限、參考批 lookup、取號及跨表原子增修／刪除／作廢。
- [ ] 讀 [WP402 規格](docs/modules/WIP/外包單確認.md)，比對 FutureOSLog 和 ERP settings 結構，實作出貨確認、Lot/流程/Reverse 原子更新及可復原的 ERP 請購與回寫。
- [ ] 讀 [WP403 規格](docs/modules/WIP/外包回貨.md)，比對回貨元件及成本資源 DDL，完成批/元件量、庫房、ERP 回貨與原子交易。
- [ ] 讀 [WP404 規格](docs/modules/WIP/取消外包出貨ERP.md)，使用既有 ReverseHistory、外包與 ERP references，實作跨 ERP 和 MES 可對帳的取消出貨。
- [ ] 讀 [WP901 規格](docs/modules/WIP/回復處理.md)，比對三張補表與既有 LotState／ReverseHistory，實作權限、LotStamp／CreateDate、防重及 SAP 對帳。
- [ ] 讀 [WP902 規格](docs/modules/WIP/刪除已下線生產批.md)，比對五張補表，交付統一包裝守門、先回復後刪除、工單、物料／庫位及載具交易。
- [ ] 讀 [WP903 規格](docs/modules/WIP/批次刪除已下線生產批.md)，重用 WP902 單筆交易並提供批次預檢、逐筆版本重查和部分成功回應。
- [ ] 讀 [WP904 規格](docs/modules/WIP/生產批作業區變更.md)，比對現有 batch override／流程／區域 schema，做有身分複核與版本保護的整批變更，排隊時按 Core 規則更新現況區及廠別。
- [ ] 讀 [WP905 規格](docs/modules/WIP/產品版本變更.md)，將目前版本、基礎批、包裝守門與回復歷程提交做成可稽核交易。
- [ ] 讀 [WP906 規格](docs/modules/WIP/ECN設定精靈.md)，完成 Setup 特性／批次預覽及 ECN 三表交易，驗證有效期、條件與 IssueState。
- [ ] 讀 [WP907 規格](docs/modules/WIP/生產批數量調整.md)，核對 AdjustLog／AdjustDetail schema，完成三種數量調整和庫存、原因、稽核與回復交易。
- [ ] 讀 [WP908 規格](docs/modules/WIP/標籤列印.md)，比對列印歷程 CLOB、受控樣板、印表機整合與列印回執。
- [ ] 讀 [WP909 規格](docs/modules/WIP/換廠作業.md)，實作目的區對應、批與靜電箱版本鎖定和同一交易。
- [ ] 讀 [WP911 規格](docs/modules/WIP/生產批作業站用料記錄調整.md)，實作用料與料批聚合交易、舊量／日期並發檢查和逐筆刪除回應。
- [ ] 讀 [WP912 規格](docs/modules/WIP/生產批作業站製程特性值調整.md)，核對特性值、並發版本與調整稽核資料，實作兩種原子交易。
- [ ] 讀 [WP913 規格](docs/modules/WIP/生產批屬性調整.md)，比對既有屬性表，完成批／元件 CRUD、舊值鎖定與理由稽核。
- [ ] 讀 [WP915 規格](docs/modules/WIP/生產批產品變更作業.md)，依來源模式、產品流程／ECN、庫存與回復規則完成正式逐批交易。
- [ ] 讀 [WP916 規格](docs/modules/WIP/生產批強制結批作業.md)，依 Service／Core 完成排隊批 EndOP、301、工單／物料／載具／儲位及回復版本同一交易。
- [ ] 讀 [WP109 原生來源補證](docs/modules/WIP/特殊生產需求維護.md)，接 next counter 原子配置、Lot 所有權、Device、參數與作廢稽核。底層附件 helper rollback 及伺服器時區仍待確認。

## 筆電交付：WP112 工單撤派工（2026-11-20）

公司端完成 `/YMES/WIP/UndoDispatch` Vue DEV 示意與 [WP112 原始碼規格](docs/modules/WIP/工單撤派工.md)；未連 YMES PostgreSQL 或 SAP、未執行正式 API/部署。這個功能有外部 SAP DB link 與 Lot/Inventory 副作用，**不能只做狀態 UPDATE 就標示完成**。

- [ ] Pull 最新 `main`，先核 YMES PostgreSQL live schema/history/權限。六張 MESTEST 直接表 `TBLWIPCONT_MATERIALLOT`、`TBLSYSSERIALCREATEMAX`、`XX_TBLOEMO_OS_BU1`、`ZPPT0025A/B/C1` 的原始 DDL/index/metadata 在 `database/reference/mestest_wip_wp112_*`，PG CREATE-only 在 `mestest_wip_wp112_create.sql`。MaterialLot 無 PK、只有兩個非唯一 index；`VARCHAR2(n CHAR)` 按 source DDL 字數，勿用 DATA_LENGTH 位元組數。已存在表不重建，缺項才 forward migration；本地 MESTEST ZPPT 不等於外部 THSAP 契約。
- [ ] 查詢 API：授權 MOState 3/6 的 MO lookup；DataType1 只取 LotState.Status3，DataType2 只取 Status99+Ex_LotState1 Y 且拒已關結 MO。LotBasis/MO/Product/OP/ERF 的 JOIN 可能重複 Lot，前後端以 LotNo 去重；參數化查詢、分頁與可見範圍。重用 WP101/WP201/WP501/Area/SYS/主檔已有表，不重複 CREATE。
- [ ] Confirm API：ProgramType3/4 逐 Lot 重查鎖定狀態、MO 關結與 `SysInfo/LastEndTime`；讀 `SAP_UPLOAD='Y'` Report、MO SAP_WERKS、OS VORNR、物料批 SAP_CHARG。按 Core 產生 `THSAP.ZPPT0025A/B/C1`、TransactionLog/EventLog，ProgramType4 分配 `SAP_RMZHL` 新計數器，3 沿用舊值；接著 3→LotState99/ExY，4→LotState12/ExNULL 並依 AreaInventory 插入 INVWIP_Scrap。若零 Report 列，須明定是否允許，不能無聲宣稱 SAP 完成。
- [ ] 外部 THSAP schema／DB link／重試補償與交易 Log provider 須驗證。若 provider 未具備，正式 POST 明確回未支援，不能只寫 PG 並回成功。逐批結果含已成功/失敗/未處理、跨系統識別、冪等鍵和並發防重；家用端完成 API/PG/正式 UI/建置/驗收/部署。

## 最新公司端交付：WP109 特殊生產需求維護（2026-11-20）

公司端新增 `/YMES/WIP/SwrBasis` Vue DEV 假資料頁；原碼核對及限制見 [WP109 規格](docs/modules/WIP/特殊生產需求維護.md)。公司端沒有連 YMES PostgreSQL、部署或建立正式 API。

- [ ] Pull 最新 `main`，先查 PostgreSQL live schema/migration history/資料/權限。Oracle MESTEST **唯讀原始 DDL**：`database/reference/mestest_wip_wp109_{basis,detail,serial}_source_ddl.sql`、`_source_indexes.sql`、`_metadata.txt`；PostgreSQL 三表空表 CREATE-only：`database/reference/mestest_wip_wp109_create.sql`。主表 15 欄/SWRNO PK，明細 2 欄/(SWRNO,LOTNO) PK，共用流水號 2 欄/FUNCTIONNAME PK；均無 FK/trigger。已存在表只核差異，確實缺項才做 forward migration；不可直接認定 CREATE-only 已套用，也不要重建共用流水號表。
- [ ] 實作登入/`WP109` ACL、分頁/上限 `LoadSWRBasis` 等效 GET（七個勾選條件，LotNo/OPNo 逗號多值改成驗證後陣列，狀態 0/1/2/99）、SWR 明細、Customer/OP/Device/MO/Lot 安全 lookup。JOIN 導致同單多列，讀取/批次命令以 SWRNo 去重；不接受原 Core SQL 字串。
- [ ] Add/Copy 在交易與 row lock 下配置 `SWRyyMMdd###`，一起寫流水號、主表及 Lot 明細；Edit 鎖主表、限制 status0，替換 Lot 明細並更新所有欄/附件。每個 BaseLotNo 重查屬於 CustomerNo；Start/End 同填且 End>Start，依 Oracle 原長度驗證。Client 預覽號不得當唯一性保證；每日 999 後的策略需明確。
- [ ] Approve 只准 0→2、重驗時間；Void 對 2→99、0/99→刪主明細，狀態 1 回明確衝突（原 Service 誤回 success）；依 `InputAbortLog` 要求原因並寫 SYS EventLog。逐筆批次要回每張成功/失敗/未處理，保留原本中途停止語義但防 JOIN 重複；狀態、ACL、actor、版本全部在 server 端重驗。
- [ ] 附件 API 從 `ATTACHFILE BLOB` 完整上傳/下載，檢查大小與檔名、不要露 `ATTACHFILEPATH` 舊客戶端路徑。WinForms `fs.Read(...Length-1)` 少讀一 byte，正式版須修正；`HaveAttachFile` 目前用路徑算 Y/N，要兼容 BLOB/路徑不一致舊列。正式 UI 接 API，DEV localStorage 只供 preview；API/PG/build/browser/部署和驗收由家用端完成。

## 最新：F1 Web241四8D正式隔離與限定驗收完成（2026-11-20；status／metadata待Root）

HOME仍負責PG復刻／API／PPK／部署，Office負責原生Oracle唯讀source與UI。Root已完成schema0060 metadata SHA `7f4c5ae33a72c9dd3b9599840523d5e89f600ca61998990b408c399ddf350e47`，schema statusY521／394來源綁定與正常feature同步已完成；下方舊pending均為歷史快照。兩表已present，禁止再absent-only或回不相容8db。

- [x] F1實Web241 `bd641471…`／來源canonical `4bbbf534…` 已完整build／package與正式部署，API155 compat466／DB0060不變。新WP801–804與ERF正式guard、四原生deep reload、Close／重進／KD/sidebar及限定既有主檔smoke已真app-browser驗收。
- [x] 本批50 raw／46有效＝43GUI＋3nonGUI（初始SQL／最終SQL／runtime），4工具false保留並復驗；Taker `GF1T-1120A` 全GUI建刪、獨立3scope0、SQLfixture/cleanup0，正常TLS7項／runtimePASS。沒有重新宣稱舊28頁全數驗收，正式8D/WIP provider仍pending。
- [x] CodeX `70722f89330555de98bc2d01030c58206eea5236` 真398＝SQL2＋API155＋Web241 binding PASS（`576076bc…`），正常推送feature並直接讀回同值。
- [ ] Root提交本輪純文件statusY、再驗398／正常feature推送與直接讀回；最後按真sealed GUI＋readback原子收尾F1 metadata。當前7f4c保前一Web5ff歷史，不借runtimebd64改原proof。
- [ ] 繼續ET106真正地址／報關writer；API161／Web242隔離候選仍prepared未發行。QA後Office main新增至d96b80d共5提交另輪整合，不能直接塞入已freeze F1或冒稱main同步。

本輪實證與限制：[F1 QA](docs/qa/20261120-office-wip-8d-f1.md)。原生CREATE、公司交接、既有API／Web／schema／GUI全部歷史原文保留。


## 最新：ET106 原生兩表0060與present相容已驗（2026-11-20；writer／Git收尾待辦）

HOME PC維持PostgreSQL復刻／API／PPK SSH／發行；Office只讀原生Oracle與開發UI。Root已在資料庫 `ymes`／`"YMES"` 真COMMIT0060，Address22＋Declaration13、source PK1／index1／defaults5／row-array types4與本表CRUD，父catalog／ACL／roles／schema保持；API155 actual466、Web237 actual5ff不變。下面舊0059／absent／prepared描述是當時歷史，不是現在DB狀態。

- [x] 依既有CREATE核號0060，完整backup635278B／restore-list1807，Root A3 whole transaction與獨立native35／fullcatalog after比對PASS。原A2真PG14 CASE語法FAIL與private證據保留，先獨立確認全catalog回原before，再新備份／新交易修補成功，不盲重跑。
- [x] 表存在之真相容單輪run `34CA74C7333D4B248CA239A1C8F4451F`：55HTTP／299assert PASS，三個主檔全API建刪，明載SQL地址fixture3／cleanup3；真normalized refs、state0／-1硬刪409／2退休與B僅地址ref檢核完成，獨立16scope全0。
- [x] 建表後app-browser新15raw／15有效＝14GUI＋1SQL、0obs；三主檔真read與Taker `G60T-1120A` NewApply／EditApply／clean Confirm／GUI硬刪／四態0／fresh Home21／末次logs[]。GUI建1刪1、SQLfixture／cleanup0，Root獨立三scope全0。
- [x] code X `800564271c7d92fba7472c28783a6bd4b8d03d49` 已真394＝SQL59/60兩份＋API155＋Web237 binding PASS，receipt SHA `16039d4ad8ac02390a04c726bf21d639eda9b456b0359b15ddd701481fc80a7c`；正常推送 `refs/heads/codex/homepc-et106-schema-20261120` 並直接讀回同值，無force。main仍待公司UI整合。
- [ ] 純文件status Y提交、394 binding與正常feature推送／直接讀回；未填尚未取得的Y。
- [ ] 原子完成schema60 checkpoint metadata；目前CP `7a9191309d3bdb86fb8f496711e24ed87cc70e0f9175ec5b7791b835db4dc324`仍是先前compat59紀錄，DB已60。保留舊rawbackup與完整API／Web／GUI歷史，不就地篡改舊證據。
- [ ] 接ET106真正地址／報關writer：raw可重鍵地址、父／lookup資格、nullable signed CAS／UTC audit、無PK Declaration完整13欄multiset版本／七欄整組實際影響列數；部署後另API／app-browser全操作驗收，不造PK／假provider。已存在兩表，不重跑0060，也不回不相容8db／absent-only suite。
- [ ] F1 Web241四8D整合已由Root本機完整typecheck／Vite與封裝，尚未部署／GUI驗收；其後另依實際版本做source binding／正常Git整合。WIP交易provider未因頁面guard而交付。

實際收據、A2FAIL與A3復原界線、API／GUI自有scope分別見[ET106 schema60 QA](docs/qa/20261120-et106-schema60.md)及[模組](docs/modules/ENT/客戶出貨地址資料.md)。


## ET106前置相容已正式驗證（2026-11-20；最新實況）

Root已正式啟用API155 `466ff88469c70ce08cfd13b6210f986796aa848b8f24d62ee3470da939402487`，canonical `7e795d7013a804b6f8ab848f4d0a5454ec613744ca2c68b6010e6a2273368202`；Web維持E1 Web237 `5ff068fc556b6449485dc40ea5b399ff2f773609c59ee19ee529aab1fbd9faae`。本批只改三個既有主檔的未來出貨地址引用相容性，schema仍0059、ET106兩表／types仍absent，地址／報關writer未交，a2 schema wrapper只是prepared，沒有新增migration／seed或WIP provider。

新466單輪API run `1C17A2F07E944C96AEB072DC13BE25F6`：45HTTP／248assertions PASS，B/F/T三筆全API建刪，獨立九範圍0（receipt `96b8830f…`）。正式app-browser封存43raw／40有效＝39GUI＋1SQL，原3／7／38三個harness false保留並由4／8／39復驗，未修改產品source。三主檔真NewApply／EditApply／Approve／Retire／HardDelete、Forwarder真Broker lookup與引用硬刪409後正確次序刪除、各四態query0均完成；GUI三自有鍵全GUI建刪、SQL fixture／cleanup0、獨立九範圍0（receipt `8535eb0b…`）。fresh穩定Home21、收合72px/icons、展開與該fresh logs[]通過。正常TLS七項、runtime／currenttag／nonroot必要讀檔／ready200與metadata-only缺表讀回PASS（receipt `44a757c1…`）。這不是重用ET105184／1853或E1原proof。

E1完整checkpoint `11bcf89f001a050bd170231bea851e8428eb596d6c2fd863080716f0b193123e`已完成並保留當時API8db／Web5ff歷史；現在live/current API是466，新compat metadata收尾仍pending，不能把舊checkpoint內容改稱已記466。393份來源（SQL1／API155／Web237）綁定已實際PASS，receipt SHA `7bf25a2e1f0b973baeb0bd3f100eba4c97d92e9ee55db65b32c9ef10342a4bab`；code X `817794f07d14e64a4ad1022a5c289aeac00fa9f2` 已正常推送 `refs/heads/codex/homepc-et106-compat-live-20261120` 並直接讀回同值。文件狀態提交與compat metadata收尾仍待完成，main仍待整合公司新提交。Office最新7df39ce的WP803／804另輪接手；WP801／802僅獨立guard來源freeze、未fullbuild／commit／部署，不能混入本批155/237證據。以下E1／D1／ET105及原生source完整保留為當時歷史。

完整實證與限制見 [ET106相容QA](docs/qa/20261120-et106-compat.md)。HOME維持PostgreSQL復刻／PPK SSH／API與DB發行；Office維持MES原生Oracle唯讀來源與UI，不派Office連PG／SSH。

下一步（HOME）：

- [x] 精確整合API155四份C#、啟用466，完成新缺表API與三主檔app-browser實測／精準清理。
- [x] code X393來源綁定、正常feature推送及直接讀回已完成。
- [ ] 文件狀態提交與compat metadata收尾；main仍待公司8D新頁整合及驗證，不覆公司提交。
- [ ] 以真source CREATE／freshmetadata／備份及已review allowlist，在相容API之後另版建立ET106兩表；本批未套用。
- [ ] 接完整ET106地址／報關API：保留raw NULL／audit／source鍵，Declaration無PK與重複列用核實multiset並發契約，不造ID／unique/FK／假provider；完成後再做獨立API／GUI驗收。
- [ ] Office WP801／802／803／804另輪正常整合、build與guard驗證，正式序號／Issue／8D交易provider仍待HOME按source落實。


## E1 四個 WIP 入口已部署與限定驗收（2026-11-20；最新實況）

Root 已實際部署 Web `5ff068fc556b6449485dc40ea5b399ff2f773609c59ee19ee529aab1fbd9faae`，237份來源 canonical `25bd965ebc6e71dc41468b9a11102d53a4444a82aadc48ac12985dfdd76dde8a`，API154 `8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e`／PostgreSQL `ymes` schema `YMES` migration `0059` 保持；本輪只改 Web，不套 WIP CREATE-only、不加權限或完成交易 provider。D1 metadata 已實際套用與獨立下載，checkpoint SHA `8918a1748840b791ac63a618222fc60b7f71465864bf8980a4fdb0439faf4037`，保留原 API154 snapshot／regression／ET105105及所有歷史；下方舊 pending 文字都是當時快照。

四個原生入口 WP505／506／507／508 的正式隔離、空 metadata、KD／單標題、重進／fresh直接路由／Close，以及28項既有頁含區域圖與 direct OperatorLogin 回歸已實測。封存89raw／85有效＝84GUI＋1SQL／四observations：67是真HTTP520傳輸結果不明，雖原passed=true仍有expectedOperationPassed=false，不算New成功；72訊息文案／74空白比對／84reload登入初始化是三個工具觀察。原事件保留，68關閉重查後69新建／70修改／71clean確定／73核准／75篩選／76停用／77篩選／78取消／79硬刪／80–83四狀態空／89獨立SQL零均完成；520根因及第一次提交時點仍未確定。這是帶有已復驗傳輸限制的限定驗收，不是所有流程無異常。

獨立最終正常TLS七項與runtime讀回全通過，正確E1 release receipt SHA `2dc935b8b97fcdaca5ac5e46834c0545f71f475d9a8c2b9e50d2b04b85074c0e`；`GE1T-1120A` exact／logical／actor+兩marker三範圍均0，cleanup receipt SHA `e2047126fd390fa270490df5ac931feb91ebd09873a1631212b005176f6a773a`，全由GUI建刪，SQL fixture0／cleanup0。fresh Home21預設折疊／scrollbar alpha.4、收合寬72／21icons／展開及最後fresh scope logs[]完成。詳見 [E1 QA](docs/qa/20261120-office-wip-next4-e1.md)。

本機已正常合併 D1／a074與Office `f43fac5a0a600a1674e05664ad8156f2107d12b9`，HEAD `47c8e549c9f2b8b75635fd1d5406b2c2b67afcea`，SQL1＋API154＋Web237＝392來源綁定PASS，receipt SHA `4c478e6005a4a9af4a6aa74b3fe798af3e45fb296d8a0ed2136c583617c7b49e`。code 47c8e549c9f2b8b75635fd1d5406b2c2b67afcea 已正常推送 refs/heads/codex/homepc-office-wip-next4-20261120 並直接讀回同值；文件狀態提交與E1 metadata收尾待完成。QA後正常fetch發現Office 6870eaf 的WP801／802兩個8D頁，另輪接手，mainSyncPending=true，未覆蓋公司工作。ET106相容API155另僅候選建置，沒有放進本輪已部署API154實況。HOME維持PG復刻／PPK SSH／API／資料庫／發行；Office維持MES原生Oracle唯讀與UI，原生來源／CREATE-only／所有歷史全文保留。


## D1 九個 WIP 入口正式已驗（2026-11-20；最新權威狀態）

Root 已完成 Web233 正式部署 `423552b638fe578066905ef72d98b58c50e0645519dc7cfc64ae2ce95a715e75`，source canonical `6a8b5567ca0d633c93c7a0d4d2f92f9d4ceb8bf0e57e9d0e74ded1f013e29590`；API154 `8db4b84024af86a82cac448f38492bfba0122a24b3e0a3369fa90b649af9d04e` 與 PostgreSQL `ymes`／schema `YMES`／migration `0059` 保持。HOME 負責 PostgreSQL 復刻、PPK SSH、API／資料庫與發行；Office 維持原生 MES Oracle 唯讀來源及 UI，沒有 PG／SSH 工作轉派。

ET105 完整 checkpoint 已由 Root 實際套用並下載獨立讀回，SHA `e13f7023a76aca28a2cb7ab2c1d7c94f58c5f5de4f336a47f90d1a511ec1bfc4`；下方「最後套用待 Root」是先期文件合併快照，保留為歷史，不是目前狀態。D1 正式 GUI 封存 93 raw／91 有效 PASS＝90 GUI＋1 SQL，兩工具 observation 75→76、81→82 保留原 false 且復驗完成。九個新入口及重進／Close、兩個 fresh 深路由、既有 19 項含區域圖與 direct OperatorLogin、ET105 真 GUI 建核准停用硬刪回歸完成；末次 fresh Home 21 項預設折疊、收合 icon／提示、透明度 0.4 scrollbar 與該 reload 範圍 logs[] 通過。不是整套 MES 或 WIP 交易 provider 已交付。

獨立正常 TLS 七項與 runtime live/current image、UID1654／101、必要檔案及 ready200 均通過。GUI 唯一 Taker `GD1T-1120A` 全由 GUI 建立／刪除；SQL fixture0、SQL cleanup0，獨立 exact／logical／actor+marker 三範圍各0。詳見 [D1 QA](docs/qa/20261120-office-wip-next9.md)。

code `eff89213ccfbebadb024374487ec4e3410b2456c` 已正常推送 `refs/heads/codex/homepc-office-wip-next9-20261120` 並直接讀回同值；SQL1＋API154＋Web233＝388 份來源綁定 PASS，receipt SHA `e8b6c225b31fd882261c61de14118a76415038e17dedd838a18c14bf558fef2b`。文件 status 提交／D1 metadata 收尾待 Root 實際執行，不預填 commit。main 仍待整合：QA 後 origin 有 `f43fac5…` 的 WP505／506／507／508 四個新 UI handoff，尚未納入本版 merge、build、部署或 GUI；本版不宣稱 main 同步。保留以下原生來源、CREATE-only、歷史紀錄及未完成事項；本輪未套任何九頁 CREATE 或新增資料庫權限。


## 本輪九個 WIP 入口整合中（2026-11-20；尚未建置／部署／驗收）

本節是這次文件合併時的最新狀態：家用基底為已正常同步 feature 的 ET105 code `ab0f8269e9778d88811947512e6753e51437703d`／status `a921b44e9b46aba20f6a8f66f4bd8944d79a18fb`；API `8db4b840…`、Web `0c2852e1…`、DB `0059` 已有前述實證，ET105 完整 checkpoint metadata 的最後套用仍待 Root 親自執行與讀回，不能從本次文件解衝突推斷已完成。

公司端 `095b3d267bc102687c32b25c9a3f593cf1ebfda9` 的九個新 UI、模組規格、來源 DDL／CREATE-only 與家用 handoff 在本工作區整合中；尚未完成這九頁的家用建置、正式部署與 app-browser 驗收，也未因本輪合併套用任何九頁 CREATE／migration 或完成 WIP 交易 provider。既有 reference 不代表 PostgreSQL live 表已建，家用端須先核實 live schema／history 再決定必要差異。

本機維持 HOME PC：PostgreSQL 復刻庫、PPK／SSH、API／資料庫與發行由家用端負責。公司端只讀原生 MES Oracle 來源並復刻 UI；以下公司 handoff 內的「本機」Oracle profile 指公司電腦，不是家用 PostgreSQL。兩側原有內容與歷史均保留，後續狀態以實際 Root 收據為準。

## 最新接手：ET105取貨公司基本資料（DB/API/本次A2GUI已驗並同步feature，main待整合）

本機為HOME PC（PostgreSQL復刻／API／PPK SSH／發行）；公司端維持Oracle唯讀來源與UI。以下待辦只列家用工作，不轉派公司端PG／SSH／部署，也不覆寫任何既有歷史heading。

- [x] 依原生13欄完成ET105 query-key API、nullable／signed CAS、state0 Edit、直接無Job核准／停用／硬刪、server UTC actor及同Taker namespace鎖序；Vue正式13DTO、Apply採真DTO、多選partial／dirty／焦點／DEV隔離與CloseHome程式已整合。這是實作紀錄，不是正式驗收結論。
- [x] Root0059實際套用／backup634856B＋restore1806＋c3200aaf…／native13三defaults本表CRUD與額外獨立readback；receipt a5de922d…、Root額外readback1116a7e5…已補維護／QA文件。沒有建表、回填NULL或擴parent/schema/role權限。
- [x] API154 source56f2568b…、SDK9正式build/publish0warnings/errors、actual8db4b840…ready/nonroot通過；Web A2 actual0c2852e1…／source224fcbb3577…、HTTPS資產與public755/644 gate通過；host source/backup700/600保持。
- [x] fresh API C3BFA7DF16CB48579DCDB00DB44598C0八組單輪184HTTP1853assert全PASS，兩exact scopes＋Holder0；Root獨立counts810536b5…再次全0。原055CE…43/355 FAIL原樣保留，日期helper窄修71262765…；recovery3874…只API retire/delete精準清理原inventory1，沒有重跑或洗掉原FAIL。
- [x] Root已封存A2 app-browser本次scope：109raw／105有效pass＝102GUI＋3非GUI，四工具obs2/43/82/95全resolved且原false保留；A1 opaque真產品finding封存，A2 event5修復。真GUI NewApply/連存/clean Confirm、dirty/Tab/selection/duplicate/literal/approve/retire/harddelete/partial1/2完成，不把API-onlyNULL/signed/max等分支偽稱GUI實測。
- [x] 7GUIcreated/harddeleted7，SQLfixture1只F stale stamp/revision、SQLcleanup0；Root final SQL7exact/logical/actor counts皆0，proof event74／rawlogc4db5ae4…。freshHome21收合106、icons107／展開108／末次logs[]109完成；105不是全純click。18nativeWIP＋1direct正式guard/Close及兩ENT read smoke通過，不代表WIP交易provider或populatedmap完成。
- [x] GUI後Root再做正常TLS public7/7（root/deep/JS/CSS/ready200及兩小寫404）與runtime8db/0c285／UID1654/101／nonroot files／ready200純讀核對；不加到API或GUI通過數。
- [x] 模組／QA、共用風格及資料庫維護文件已整合；GUI驗收6734a15e…／cleanup37525ba0…／獨立peer03862512…已核對。
- [x] code `ab0f8269e9778d88811947512e6753e51437703d`已正常推送`refs/heads/codex/homepc-et105-20261120`並直接讀回相同；SQL1／API154／Web224共379份來源綁定通過，receipt SHA `2ac94056c8c82c76eedf698e64fcaa242f388a66f74a0777db1522c9c3690bae`，未允許差異0。後續純文件狀態提交不改本release code或重建產物。
- [ ] 純文件狀態提交及完整checkpoint最後核對；mainSyncPending=true，接續正常整合公司九頁後驗收。

### 下一項：ET106客戶出貨地址資料，先來源與schema稽核

- [x] ET106只讀來源預盤已備`.tools/et106-source-audit.md`及metadata-only`.tools/et106-liveaudit-before.sql`，獨立readonlypeer通過、未DBrun。核正FunctionNo=`CUSTOMER SHIP ADDRESS`／FunctionKeyET106、雙grid、來源Apply/Confirm/rekey。repo有公司WinForms/Service/Core查證規格但未找到完整原方法檔，不冒稱已逐行讀CoreSQL。
- [x] CREATE早已完整交付，不再重問公司同DDL；已逐欄靜態對照Addr22/NN4/default3（createdate無default）與Declaration13/NN8/default2/noPK/index/FK。
- [ ] Root凍結報關自然七欄重複組的snapshot/CAS/affectedRows、地址raw rekey與parent/new/unchanged legacy資格等現代政策，不任選first row／造PK／ctid永久ID；metadata未知保留pending。
- [ ] HOME透過已授權方式只讀核PG現存表／型別／欄位／grants與migration history；只有來源證實且真缺的表／預設／權限才提出forward migration，不能重建共用表或把Oracle CREATE直接套為現況。
- [ ] 核真Customer／Taker／Forwarder／Broker lookup的來源資格與共用引用協作，特別啟用ET105目前尚未交付的ShipAddr真Taker引用registry／SELECT與writer鎖序；先形成可信API契約，且先準備允許ShipAddr optional-absent的Broker/Forwarder/Taker相容registry/API，再引入表及writer，避免新引用使既有三master全部503；未知provider不造假。需要公司來源證據時再列精準類別／方法／DDL欄位缺件，不把家用實作工作塞入officeTODO。

### 後續公司新UI整合（不混本次ET105或ET106）

- [ ] QA後fetch看到公司origin/main d5a8bd1→095b3d2，WP212/213/214/215/914/501/502/503/504共9新WIP UI＋CREATE交接尚未merge／未部署。保本輪ET105固定224，不混入canonical；下一integration scope正常merge／freeze/fullbuild／正式guard與影響GUI驗收後再同步main，禁止force push。

同步後接續公司九頁整合：已fetch至`095b3d2`，新增WP212／213／214／215／914／501／502／503／504及其CREATE交接。這些尚未合併、建置、部署或驗收，不混入本輪Web224／379來源。先保留正常feature同步，再逐頁整合與驗收後同步main；不使用force push。ET106來源／metadata稽核準備沿下方待辦繼續。

## 最新接手：公司WIP載具拆批／併批Web224 C1（2026-11-20；限定GUI完成／main待同步）

- [x] 保留已驗Web220 dc34／372及Web222 3953／374的正常featurebranch推送／遠端讀回；各版proof86／69保持原樣，222小型本機紀錄71fa671a…的私有上傳尚待實證。
- [x] 正常合併Office最新df10bd08至3f99c172，凍結Web224 canonical f9bb9566…；新WP210／WP211只補正式隔離與既有KD框架，Bin只保留Office rawRecords alias，正式providers未交付。
- [x] C1完整建置exit0、封裝／部署Web e2df5e3d…與公開HTTP七項通過，224來源f9bb9566…保持；Root已Demo登入操作新兩頁guard／19入口KD／無假metadata／單一主標題／Close／重進／direct fresh、舊16native／direct OP、WP30730×40／四zoom與SD108空lookup。
- [x] 封存限定GUI73通過檢查點／73events／0observations：18native＋1direct、新兩頁重進／direct fresh、19KD／無假metadata／單一主標題、WP307／SD108空lookup界線與ET104真CRUD均通過；2筆全GUI建刪、SQL fixture writes0、獨立精準B/F皆0，fresh reload21收合選單／空首頁／logs[]。proof f2fa8af9…／receipt87802ec4…／cleanup3a5c9a4a…保持原檔。
- [x] C1 code `bacf0ee58288d1a4f1bec41a5a6bde00053d02bf` 完成376綁定（receipt399f648b…）並正常推送 `refs/heads/codex/homepc-office-wip-integration-20261120`，遠端讀回相同；73項proof保持不動，peer bba6f0d6…通過。
- [ ] 完成C1 feature-only checkpoint與本次狀態文件提交／綁定／分支同步；mainSyncPending=true。兩頁驗收後automatic review已允許normal main push，Git遠端因Office d5a8bd13拒絕nonFF，未force、未改main。
- [ ] Office 0b342616／d5a8bd13新增元件／等級併批兩頁另列待接手，尚未合併本輪worktree／建置／部署／驗收；C1先維持已驗224與feature同步，接續ET105 API工作，不將未驗新頁混入376來源。
- [ ] API6d／151份及DB0058保持，不重建API或套新schema；WIP providers持續由HOME接手。ET105僅隔離候選與純讀peer，尚未build／migration／正式部署。

本機仍為HOME（PostgreSQL clone／API／SSH），公司端為Oracle唯讀來源與UI；[Web224 QA](docs/qa/20261120-office-wip-carrier-merge-integration.md)記本版證據，mainSyncPending=true。下方220／222最新段為當次歷史，不將其proof改配224。

## 最新接手：公司WIP拆批Web222（2026-11-20；限定GUI完成／main待同步）

本機為HOME PC，負責PostgreSQL復刻、API、SSH與發行；公司端維持Oracle唯讀來源與UI。Office12c6a2e已與Web220 release dc34ee6正常合併至32d422b5，兩端工作保留。

- [x] Web220 dc34ee6已完成372來源綁定（收據9c186dc0…）、正常featurebranch推送／遠端讀回及私有7份證據；保留86項proof與當時mainSyncPending=true，不改配222。
- [x] 新Web222來源canonical ee360b1d…完整型別／Vite建置exit0並部署B330b9…，七項HTTP通過；API6d／151份及資料庫0058維持原版。
- [x] Root已封存Web222限定GUI69通過檢查點／70events／1個已解menu hydration harness觀察：WP208／209＋舊14native防護／Close、新兩頁重進／直接fresh／無假Bin／KD單一主標題、WP307 Grid30×40／四zoom、direct OP及SD108空lookup／Grid／zoom、ET104新Web完整smoke均實測；SD108未驗populated layout。
- [x] 2筆smoke全GUI建立並硬刪，獨立精準broker／forwarder皆0、SQL fixture writes=0；fresh reload空首頁／21個收合選單／logs[]。proof346d017b…、receipt fbb84410…及清理d8075801…封存，原harness觀察與穩定三層復驗保留，無產品改碼。
- [ ] 執行374份來源綁定並正常同步main；目前mainSyncPending=true，不預填新commit或push結果。
- [ ] WIP正式provider與原生交易持續由家用端接手；入口防護或DEV原型不算API交付。ET105僅隔離候選與純讀peer，尚未build／migration／正式部署。

本版證據與界線見[Web222 QA](docs/qa/20261120-office-wip-split-integration.md)；舊版歷史及既有待辦保留。

## 最新接手：公司WIP整合Web220（2026-11-20；限定GUI完成／main待同步）

本機是HOME PC，負責PostgreSQL復刻、API、PPK SSH與發行；公司端維護Oracle唯讀來源及UI。公司b3f0f06已與家用A1正常合併至基底7e71e38，兩端原始工作保留。

- [x] 整合公司14個新增WIP Vue；其中13個維持公司原Git blob，收藏頁與WP307新增正式未提供防護，補六個原生route並限定15個新入口套KD共用風格。
- [x] Web220完整型別／Vite建置exit0，已實際部署Web `823d0571d6dc`／canonical8071ec80…；七項HTTP與資產MIME／bytes／SHA／nonroot檢查通過。
- [x] API仍6d2501…／151份、PostgreSQL仍0058；本輪不重建API、不套新表或權限。A1完整checkpoint9cbe、featurebranch X65f17a0／Yb015e51與原API169／621、GUI87證據保留。
- [x] Root封存限定GUI86通過檢查點／87原始events／1個readonly harness誤判保留並真復驗；14原生＋1direct、Close／重進、WP307 Grid／顏色／四縮放及ET104新Web smoke通過。SD108無核准Area，只驗picker／grid／zoom／Close、不宣稱populated layout；2筆smoke全GUI建刪，獨立B/F皆0、freshreload logs[]，見本輪QA與proof81245a0a…。
- [ ] 核對372份來源（SQL1＋API151＋Web220），先正常同步本版featurebranch；actual commit／binding／push尚待實證，不假填main同步。
- [ ] Root另fetch到Office dbebd0f／最新12c6a2e；兩版未在本次220部署／驗收。接續正常merge新版、另做Web222及新驗收後才同步main，保持mainSyncPending=true及220來源／proof不動。
- [ ] WIP正式providers／HostId與人員交易等仍由家用端依來源逐項接手，不把DEV UI或不可用防護當成API完成；ET105僅唯讀audit done，API／migration未實作。

維護紀錄：[整合QA](docs/qa/20261120-office-wip-integration.md)。既有所有模組待辦及A1完成歷史繼續保留。

## ET104 A1 正式驗收（2026-11-20；分支已同步65f17a0）

本機是 HOME PC：PostgreSQL復刻庫／API／PPK SSH／部署；公司端是原生Oracle唯讀來源／UI。正式 API `6d2501aad008`、Web `56c31765e2af`、0058；路由 `/YMES/` 與 `/YMES/API/` 維持分離。

- [x] 既有Forwarder十五欄／PK1／index1核對後，只補三來源DEFAULT及CRUD。完整備份634316 bytes／1805 restore entries、交易差異白名單與獨立metadata通過，保留0057欄權限及父檔，無重建／回填。
- [x] API151／Web206固定來源完整建置與正式部署；API單輪八組169HTTP／621斷言，11自有範圍及Holder全0，獨立11項再讀0。
- [x] 自行按Demo登入正式app-browser，87項（77GUI＋10SQL讀回）通過；90原始events與3項已解harness誤判保留，沒有因此修改產品。12筆Forwarder全由GUI建刪，SQL未新增／刪除Forwarder；最後11scope全0／三個原始資料陣列空，重載錯誤紀錄空。
- [x] 補齊模組維護、QA、資料庫與部署紀錄；既有兩鍵raw／nullable／signed CAS、Edit0與2真更新、Apply乾淨停用、New才驗Broker2、舊parent缺失／退休、引用guard與workflow界線已記錄。
- [x] ET104 A1 release code `65f17a0c347acd4b5df2a1be1cad712138ececd6` 已正常推送 `refs/heads/codex/homepc-et104-20261120`，直接 `ls-remote` 讀回相同。358份來源綁定通過（SQL1／API151／Web206，未允許差異0；舊API131份僅representation、Web188份僅Git EOL差異），無force push。後續純文件狀態提交不改此release code；actual status SHA與遠端讀回另封存於checkpoint。mainSyncPending=true：公司b3f0f06的14個新WIP Vue／App／AreaShow將正常合併、另建新Web與驗收後再同步main，不能把A1的87項證據套用新產物。
- [ ] GUI完成後fetch已有公司提交 `b3f0f062e22b1df3437868a2c29ec51e80ebbb92`；A1紀錄完成後正常merge，保留公司14個新WIP Vue與App／AreaShow修改，另建Web220／驗正式入口及ET104回歸，再正常同步main。不能拿A1的206份來源／87項證據冒充新版本。
- [ ] 同步與整合後再fetch檢查下一交接；接續ET105等來源完整項目。完整Job／通知provider及其他WIP流程保持各自待辦。

維護與驗證：[貨運行模組](docs/modules/ENT/貨運行基本資料.md)、[ET104 QA](docs/qa/20261120-et104.md)。下方ET103等為歷史。

## 最新接手：ET103 報關行已驗收並同步 main 0c65a65（2026-11-20）

本機為 HOME PC，處理 PostgreSQL 復刻庫、API、PPK SSH、部署與驗收；公司端維持原生 Oracle 唯讀來源與 UI。最新 API `12f26b18e94a`／Web `b3949d57fd39`，migration **0057**；下方 ET102 及更早內容保留歷史。

- [x] 既有 Broker 13 欄與 PK/index 核對後，只補三個原生 defaults、Broker CRUD 及 Forwarder `SELECT(brokerno)`；完整備份／restore-list、交易內變更白名單及獨立讀回通過，沒有重建表或回填舊資料。前置工具失敗與修正證據保留於 QA。
- [x] 固定 API148／Web206 來源完成 .NET9 Release 與完整 Vue 型別／Vite 建置、正式部署、nonroot 與正常 HTTPS 資產核對。回復 API 保留 ET102 `0339807d5b7e` 的145份來源與既有防護，保留表與資料。
- [x] API 單輪八組126 HTTP／464斷言通過；11個精確資料範圍及holder全零，獨立再讀11項亦0，metadata不變、登出204。
- [x] 正式 app-browser 自行 Demo 登入專用分頁，59個檢查點（56GUI＋3SQL）通過；新增／重複／反覆套用、必填／原生長度／字面特殊鍵、dirty／雙向Tab、選取、批次核准及刪除部分成功、四態、raw／NULL／signed版本、引用防護、空白首頁／21收合圖示／重進與深路由重載皆有實證。最終59項無失敗觀察，不代表前置工具沒有失敗。
- [x] 十筆 Broker 全由 GUI 建立與刪除。SQL 僅設定自有衝突／舊資料條件及兩筆有標記的引用；引用精準清理後由 GUI 刪 R，最終三個自有資料陣列皆空、11範圍獨立全0。重載後 console 無紀錄，限該次範圍。
- [x] 正常TLS公開HTTP再核對：深路由200、小寫兩路徑404、API ready200、未登入Broker API401。明確QA請求識別通過，不修改邊緣服務規則。
- [x] SQL1／API148／Web206共355份來源綁定通過，未允許差異零；release code `0c65a655fae538280244289e281733f612a0eb4f` 已正常推送main。fetch確認origin相同，從012f257快轉保留兩端歷史，沒有force push或公司新增提交。後續純文件狀態提交不改此release code或重建產物。
- [ ] 同步後再 fetch 檢查新交接，接續 ET104 貨運行等來源完整的家用端工作。完整 Job/通知 provider 與其他模組不因本批驗收而改為完成。

維護契約、原生與 YMES 新增防孤兒行為、已測與未測分支，見 [ET103 模組](docs/modules/ENT/報關行基本資料.md)及 [ET103 QA](docs/qa/20261120-et103.md)。

## 最新接手：ET102 客戶連絡人已驗收並同步 main 7b6771e（2026-11-20）

本機為 HOME PC，負責 PostgreSQL 復刻資料庫、API、PPK SSH、部署與正式驗收；公司端負責原生 Oracle 唯讀來源與 UI。最新正式 API `0339807d5b7e`／Web `6a4f390f37b8`，migration **0056**；下方 OPB03 及更早狀態保留歷史。

- [x] 已備份並驗證 restore-list，僅新增來源核對的 Contact 13 欄／複合 PK／預設值與本表 CRUD 權限；沒有 seed、FK、額外 index 或父表權限擴張。
- [x] 145 API／206 Web 固定來源完整建置、正式部署、nonroot 與公開資產驗證通過；保留前一版 OPB03 API 作相容回復，不移除已建的 Contact 表與資料。
- [x] API 單輪八組 98 HTTP／406 斷言通過，七個精確範圍及 holder 全零、metadata 不變、登出 204。
- [x] 正式 app-browser 自行 Demo 登入，53 個檢查點通過（52 GUI、1 SQL）。涵蓋查找／全客戶狀態、新增／修改／複製、連續套用／確定、必填／重複、dirty／雙向 Tab、50 字元與字面特殊鍵、多選／部分刪除 1/2、stale、舊 raw 鍵及 NULL、關閉空首頁、21 個收合圖示、重進與深路由重載。兩次驗證器觀測錯誤與後續成功重驗完整保留，沒有改寫原始事件。
- [x] 11 筆 Contact 全由 GUI 建立及刪除；獨立 SQL 確認 Contact 為空後才清理六筆 lookup，七個範圍獨立讀回全零。最終重載 console 無紀錄，僅適用該次採集範圍。
- [x] 145 API／206 Web／1 SQL 共 352 份來源綁定通過，未允許差異零；已正常推送 release code `7b6771e4718a56a950f4a4de5c61564ea8ed4137`。再次 fetch 確認 origin/main 相同，從 c2c4930 正常快轉保留全部歷史，沒有 force push 或公司端新提交。後續純文件狀態提交不改本 release code 或重建產物。
- [ ] 同步後再 fetch 檢查下一項來源完整的家用端交接，持續接手；未提供的交易 provider 與其他模組保持待辦。

完整契約、限制與驗證證據見 [ET102 模組](docs/modules/ENT/客戶連絡人資料.md) 與 [ET102 QA](docs/qa/20261120-et102.md)。本批沒有交付 ET101 父客戶 writer 或角色引擎。

## 最新接手：OPB03 客戶站點對應已驗收並同步 main caae71d（2026-11-20）

本機是 **HOME PC**，操作 YMES PostgreSQL 復刻資料庫、API、PPK SSH 與部署；公司端的 Oracle 是 MES 原生來源，用來提供唯讀結構、原型邏輯及 UI。以下 OPB02 與更早的版本狀態保留為歷史，現在正式版本以本段為準。

目前 API `2a2291cc1163`／Web `fc83e8bc5484`，migration 已到 **0055**。OPB03 主、明細 CRUD、複製、真實查找及版本衝突處理已完成；API 八組單輪 162 HTTP／578 斷言通過，正式 app-browser 67 個檢查點（66 GUI、1 SQL 讀回）通過，23 個 GUI 測試範圍全零。完整操作、原始取證修復及未提供的原生細節見 [OPB03 QA](docs/qa/20261120-opb03.md) 與 [模組維護文件](docs/modules/OP/客戶站點對應設定.md)。

- [x] 全庫 custom 備份與 restore-list 驗證後套用 0055；兩表 23 欄、兩個複合 PK、兩個 index、NULL/default/ACL 核對通過；沒有重建既有主檔或新增 FK。
- [x] API 主來源 142 檔及保留 OPB02／OPB03 引用防護的相容回復來源 139 檔，均完成隔離 .NET 9 Release 建置與遠端 nonroot gate；正常 HTTPS 與 API 保護通過。
- [x] Web 最終 a2 僅修正順序欄位名稱；206 檔固定來源、完整型別／Vite 建置、公開目錄 755／檔案 644、資產 MIME／大小／SHA、nonroot 與正式 app-browser 操作通過。A1 暖身 14 項未灌入最終 67 項。
- [x] 真 GUI 驗證 Header Apply／Confirm 關窗、Detail Apply 轉 Edit／新 OP 主鍵與父子版本續存、選取／篩選、批次部分成功、NULL／stale／舊 raw PS／旗標 9／字面星號、主從刪除、空白首頁、21 個收合圖示、選單重進／深路由重載。最後重載與重載前採集的 console 紀錄均空；範圍詳見 QA。
- [x] 所有主檔由 GUI Add／Copy 並由 GUI 刪除，獨立 SQL 已證主、明細為空，再精確清理 12 筆 lookup，23 個範圍獨立讀回全零。原始 86 筆取證事件完整保留，修復容器版本標記後逐筆比對最終 67 項，並另保留四次觀測修正。
- [x] 正式程式提交 `caae71d25ed3b88274aeddc5095806e286b5816e` 已正常推到 main；fetch 確認當時公司端沒有新增提交，以 29b3c04→caae71d 快轉保留完整歷史，沒有 force push。1 SQL／142 API／206 Web 共 349 份來源綁定通過，未允許差異零（文字換行／BOM 表示差異依既定規則核對）；後續純文件狀態提交另記，不取代此 release code 或重建產物。
- [ ] 同步後檢查公司端新交接。下一個來源完整的候選為 ET102 客戶連絡人資料；目前只有 ignored 隔離候選，未整合、未建置、未判定 live schema，不能列為已完成。

OPB03 只完成本模組已提供的操作，不把其他 MES 交易 provider 或角色引擎列為完成；公司端僅需補兩項原生事件／查詢來源，見其專用待辦。

> **本批完成（2026-11-20）：已正常同步GitHub main `487e9d1`。** release code commit固定為 `487e9d1d15489be5d864c54abc832b81724f579d`；root核對1份migration／139份API／206份Web與已驗收來源一致後，正常推送395a→487，沒有force push或公司端新增提交。後續純文件狀態提交另記，不改本release code、不重建產物；歷史pending與其他未完成provider／模組待辦保留。

## 最新接手：OPB02 API／正式GUI已驗並同步main487e9d1（2026-11-20）

目前正式API **`1d09e4febf48`**／Web **`cfa5cf288359`**；資料庫migration已到 **0054**。OPB02 API八組分兩輪、正式IAB主從操作與精確清理均已取得通過證據，本批程式已正常同步main `487e9d1`。以下原「家用端最新進度」保留0053整合版發行當時的歷史快照，最新狀態以本段及模組文件為準；完整Job／通知等未提供能力仍不列為完成。

- [x] root完成全DB custom備份與restore-list驗證，0054原子套用並獨立核對13欄／兩PK／兩index／trigger0／欄與ACL差異0、Detail全表OPNo PK及app-role讀取probe；沒有seed。正式記錄見 `.tools/opb02-migration-applied-20261120.json`、[ECN模組](docs/modules/OP/ECN作業站群組設定.md) 與 [資料庫紀錄](docs/DATABASE_CONNECTION.md)。
- [x] root完成OPB02 API a2獨立建置／實際nonroot讀取與啟動gate，正式API為 `1d09e4febf482433d398a986a3d9458eca693324763a45cbcd468e0d791e2755`；相容回復映像保留ECN引用防護。API程式在兩輪回歸間未改。
- [x] 八組API聯集通過：首run `2FCE461D77A744BCBA78AD38E611EBF7` 的G1–6通過，G7傳輸中止，保留原FAIL紀錄並清理15個精確scope／holder全0；fresh run `4A1AE89BFC594FE58866353B07ED4665` 只補G7–8通過（31 HTTP／95斷言），同樣精確資料／holder0、metadata保持、logout204。兩份紀錄合計179 HTTP／556斷言，含首輪G7中止前檢查，不能當作單次八組全通或去重案例數。詳見 [ECN模組](docs/modules/OP/ECN作業站群組設定.md)。
- [x] root API-only checkpoint dryvalidate／apply完成，再部署Web a2 `cfa5cf288359f1f3bbfdd5dfc9f2dbb5457f4ef0e6537d0fc38c698edb902fd7`；nonroot、公開0755／0644與正常HTTPS資產MIME／大小／SHA全通。Web-only checkpoint亦dry／apply完成，206來源安裝於 `/opt/ymes/checkpoints/web-opb02-20261120-a2`；API1d09及migration0054原值、舊Web a3／Git395歷史保留。
- [x] root正式IAB驗收48個檢查點全部通過，其中1項是SQL cleanup讀回，不稱48次純GUI點擊。六個Group A／B／C／N／P／S全由GUI Add／Copy並GUI刪除；NULL／order／日期／raw空白PS、Copy必填、stale及批次1/2、0→2→-1→hard、SQL state1後真明細Add／Delete、SQL字面`*`後真lookup／CRUD、Tab／dirty及Close／重進／深路由reload皆有實證。SQL設定與真GUI核准分開；最後logs空陣列只限該reload。
- [x] GUI cleanup前獨立92／91已證Group與兩方向Detail全0、只剩9個lookup；精確90清理後獨立91的19項scope全0。模組MD已補全部實證與未提供／未測界線；Job候選存在503仍未實際觸發，不造假provider。
- [x] root已正常提交／推送本批程式至main `487e9d1`，origin確認相同，保留兩端歷史、沒有force push或公司新碼；同步前395a／pending記錄保留為歷史。後續純文件狀態提交另記，不取代release code。OPB01共享Element來源查證只派公司端程式／資源閱讀，見 [公司端待辦](todolist_officepc.md)；原家用schema／API與其餘模組待辦全部保留。

## 家用端最新進度（2026-11-20，正式 GUI 驗收完成，已正常同步 main）

此段為目前狀態；下方保留交接當時的待辦原文，歷史未勾選項不表示本段已完成內容又變回缺表。家用端管理 YMES PostgreSQL／API／PPK SSH；公司端持續提供原生 Oracle 的唯讀結構、來源邏輯與 UI。

目前運行 API `16a825af0bea`／Web `722bc259cc81`，migration 已到 0053。本輪 PD101／PD206／OP103／OP105／PS103／QC201／QC202／QC301／QC302 的已開放主要操作已用正式 app-browser 實際驗證；自行 Demo 登入、預設正式區、空白首頁、三層選單與收合圖示亦已重驗。修正四頁彈窗焦點、選單圖示裁切與公開資產權限；各模組 MD 保留未實作 provider 和未測分支，不把 API 回歸或 SQL fixture 當成 GUI 通過。所有專用測試資料已精確清理。公司端至 `4e0bae1` 的二十筆 OE／WIP 提交已正常合併並完成原 23 個入口防護；接續至 `f68ecea` 的 WP308／WP303／WP304 三筆亦已正常合併、建置、部署並以正式 app-browser 補驗 3/3，累計 26 個唯一路由的不可用提示、無 DEV 資料及離開／重進防護通過，詳見 [公司整合 QA](docs/qa/20261120-office-integration.md)。這 26/26 不是 API 或生產交易完成；原有 API／schema 待辦仍由家用端持續實作，GitHub main 已正常推送至 `c64d0ee`；兩端提交歷史均保留，沒有 force push。下一批 OPB02 草稿與 0054 尚未發行，須獨立保存，不混入本批。

- [x] 已正常合併公司端至 `3192964`，保留新 OE 頁、DDL 與 QC／PD206 來源。PD206 文件衝突同時保留雙方內容；沒有 force push 或捨棄任一端開發。家用端本批仍在正式 GUI 驗收，尚未以完成版本推到 main。
- [x] 0042–0050 已逐份備份、驗證 pg_restore --list 後套用並獨立讀回。0046／0047／0048 僅補既存引用表 SELECT；0049／0050建立來源核對的OP Type／共用Property空表，無FK或多餘index，詳見 docs/DATABASE_CONNECTION.md。沒有搬移Oracle業務資料。
- [x] PS103、PRD Program／Recipe 指派與產品 nullable CAS 已部署。PS103 原58項 API及實際GUI流程已通；PRD完整10群組與SAP專項已通。新版產品多選、取消／確定、部分成功1/2與stale409、重載後2/2、核准必要條件／成功／停用／實刪已由正式 app-browser 驗證；產品與目錄10項SQL計數皆0，見各模組MD。
- [x] PD101 正式 API 九組功能分兩輪共184項HTTP斷言通過；nullable CAS／UTC及NULL audit／引用鎖／雙向同名競爭／模組碰撞均驗證。兩輪15表與鎖holder精確殘留0。完整子設定Copy／IssueJob仍依模組規格，不冒稱全部原Core已完成。
- [x] PD206 最後完整一輪104項檢查加登出共105項通過，包含產品多版本與最後版、設備及作業站真刪除後保留抽樣規則、資格失效時拒絕保存、literal `*`、UTC／NULL audit、CAS與鎖等待。四表自有資料精確殘留0；不等同WIP抽樣執行引擎。
- [x] 捕捉PD206回歸中的OP刪除500，補齊六張真引用表SELECT（0048）；另部署通用主檔Npgsql錯誤503回應。當時API `269434c2aaf0`／Web `29139f507e77` 的四主檔登入讀取／匿名保護／小寫404共11項冒煙通過；PS103新UTC日期另5項HTTP與DB epoch/時間窗通過，自有Type/Process0。
- [x] 一般抽樣來源／ShowFlag子集18項HTTP通過；QC301／302的chart、Plan引用鎖、Create/Update/Approve與Import、反向Copy及alias十組分三輪共88項預期HTTP斷言通過。新因子目錄歧義／重複輸入／NULL stamp與Rule舊snapshot四組分兩輪共39項取得成功證據；每輪11表及本機／DB holder0。細節及各輪helper錯誤保留於QC301／302 MD，不冒稱單次全通或GUI通過。
- [x] QC一般source與因子防護已合入目前發行來源，OP103隔離API／Razor建置0警告／0錯誤、Web vue-tsc／Vite通過。21:19已啟動API `260dfd2ff2bb`／Web `760d6055e519`（`index-DI-geD67.js`），ready200；僅更新YMES容器，未清除其他專案或orphan。
- [x] OP103正式API十組回歸通過：run54BC802、112次HTTP、234項斷言，包含四態、nullable CAS、精準Property清理、namespace競爭及Type引用鎖雙方向。最後Type／OP／Property／自有PS／holder皆0。兩表Source19欄／PK／NULL/default/grants已核對；GUI與完整Property provider仍待，不猜路由或回空成功。
- [x] QC301／QC302 已完成本輪實際 Rule 保存／重複／清空、Add／Copy／Apply、核准／取消核准／停用／硬刪、一般 Plan、Copy2、NULL stamp Rule／stale，以及真 Excel 逐列匯入。原 `CODUI301N-1119-*` 與新測試範圍一併清理，含九個 GUI 產生識別碼的十三項提交後計數全零；詳見兩模組 MD。
- [x] PD101／PD206／OP103／PS103 的主要 CRUD、套用、查找、狀態與日期顯示已補實際 GUI；四頁的取消／套用焦點與正反 Tab 修補已發布重驗。使用 app-browser 自行按 Demo，無須再次等待使用者登入。缺少來源的 Copy／Property／WIP／Simulation 等分支保持明確未開放，不回假成功。
- [x] 驗證後正常 fetch，最新公司端為 `4e0bae1`（本機基底 `3192964` 之後二十筆 OE／WIP 提交）。接續正常 merge，保留兩端來源；合併完成前不以舊版覆蓋公司端。
- [x] 公司整合正式入口防護 23/23：OE 11 個、WIP 12 個。前 22 個於 Web `5959083456ce` 實測，a2 僅修改 SemiInventory.vue 並以 Web `61166de65bf6` 補驗 WP604。18 個有頁內 Close 的入口實按關閉，5 個基本 WIP 正式 fallback 由框架首頁離開，所有入口均重進通過；WP601／WP629 模式往返及無 currentLot 時 OPNo／Unit 不套 DEV 預設值亦已確認。這是未提供服務時的正確防護，不勾選下方正式 API／交易待辦。
- [x] 接續正常合併公司端 `f68ecea` 三筆 WP308／WP303／WP304。修正三頁正式 seed 洩漏、補 empty／disabled／handler guard／DEV-only modal，保留 DEV 原資料與流程；WP308 型別交換窄修後建置通過。Web `722bc259cc81` 的正式 app-browser 三頁均 0 DEV 業務列、不可用提示正確、查找／交易 disabled、頁內 Close→空首頁→同路由重進通過，console 無紀錄；原 23 加 3 共 26 個唯一入口。僅完成入口防護，未將新三頁正式 API／交易待辦勾為完成。
- [x] 已驗收來源與維護 MD 以 `84108ab`、`c64d0ee` 正常合併並推送 main；Web 206 檔與已部署快照逐檔核對一致。OPB02 未驗草稿另存，未混入本批，沒有 force push。後續公司端新提交仍須正常合併及重驗。

## 家用端接續：QC201／QC202（2026-11-19）

- [x] QC共用參數目錄0051、Reason參照鎖權限0052已備份並驗證pg_restore清單後套用；原生／PG參照／migration及live16欄比對無差異，PK/index/grants已獨立回讀。真實catalog保持空表，不seed假資料，見 `docs/modules/QC/因子參數目錄相依.md`。
- [x] 保存既有本機草稿後重新稽核：QC201／QC202原有NULL→0、子資料無版本及Factor metadata fallback不算正式可用；已接手修正。QC202 Vue已對齊單一saved DTO、Apply轉Edit、raw nullable stamp、JSON身分與部分刪除結果，Web型別／Vite建置成功；尚未部署或GUI驗收。
- [ ] 完成QC201狀態／nullable CAS、共用identifier／參照鎖、子資源snapshot版本、Copy預覽選擇、真實lookup與Vue對接。未交付的WIP刪除檢查、IssueJob、Simulation、ValueList／Table／Function provider不以空資料或成功替代；來源不足交公司端補證據。
- [x] QC201／QC202固定API快照13檔SHA一致，.NET9 Release 0警告／0錯誤，遠端API `95a153cef1d7` 已啟動ready200。QC202十組一次通過103項HTTP／444斷言、6表／holder0；QC201前六組175HTTP／544斷言、12scope／holder0，Op第7組另49HTTP／227斷言、14scope／holder0，合計七組分兩輪224HTTP／771斷言。每輪schema metadata保持、登出204。不是GUI驗收；Web仍 `760d6055e519`，新畫面尚未部署。
- [x] 兩頁最終Web a4固定快照vue-tsc／Vite exit0；只含QC201／QC202覆檔、不含OP105草稿。已部署Web `1c9b82c00343`（`index-M6-uxH7Y.js`／`index-GktQBV6c.css`），封裝907350 bytes；遠端8項HTTP確認入口／兩頁SPA／JS3331109與CSS875532 bytes及SHA／小寫404／API ready200全過。API當時為已驗證95a153cef1d7，沒有更動其他專案。
- [x] QC201／QC202 的本輪已開放主要功能已在 2026-11-20 用正式 app-browser 補驗，包含四種明細、參數來源 Copy、實際核准／Op、NULL／stale、管制內容 CRUD／規則／失效 Options。十六個精確資料範圍與 holder 全零；未提供及未 GUI 測的分支列於模組 MD。
- [x] OP105：兩表20欄比對及0053備份／套用／readback完成，僅補5defaults與owner CRUD；API隔離.NET9 Release 0警告／0錯誤、16a825af0bea已啟動ready200。正式8組145HTTP／501斷言一次通過，5owner表／group ref及其他7引用表精準鍵／holder0，metadata保持、登出204。包含真OP引用鎖與padded舊member防實刪。最終Web a2已修7項peer邊界、build exit0，23:14部署1f2dff75f080（DY_b-9Fr.js／BCM0CnE5.css），9項靜態HTTP及完整資產SHA通過；GUI仍待。
- [x] 可重建來源／發行界線已補 `DEPLOYMENT_PROFILES.md`：遠端 `/opt/ymes/checkpoints/fb22a87` 與本機checkpoint保留已部署來源；共用 `/opt/ymes/source` 尚有舊碼，不可直接預設重建退回舊版。

## 新交接公司端作業員 OP／Area 登入登出（2026-11-20）

- [ ] 閱讀 [OP／Area 登入登出規格](docs/modules/WIP/作業員登入登出_OPArea.md) 與 `docs/DATABASE_CONNECTION.md` 對應查詢結果。先核 YMES PostgreSQL live schema/migration 是否已含 Operator Login State/Log。
- [ ] 公司端使用受保護 Oracle profile 對 MESTEST `ALL_TABLES`、`ALL_SYNONYMS` 查 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG` 均查無；source DDL/Create-only 不存在。若 PG 也缺少，先取得有該兩表的正確 Oracle 環境 DDL/metadata，再建立 CREATE SQL/migration，禁止用舊 Core INSERT/UPDATE 欄位推測表結構。
- [ ] DDL 確認後實作登入授權 User/Shift/current-state API、POST login、POST logout。Server 重查 target employee/shift/OP/Area、WorkDay ±1 與 LoginSerial 狀態，產生 serial/time，登入 State+Log 原子新增；登出按 LoginSerial 鎖定、計算 WorkingHour、原子刪 State/更新 Log。
- [ ] 保留現場員工刷入模式，同時限制授權操作者/terminal 可代登入的範圍。Audit actor、LoginDate、LogoutDate、WorkingHour 必須由 server 產生；不可信任 browser 送入的 EmployeeName、ShiftNo、serial、時間或 audit identity。完成後補 CREATE SQL、migration/API 規格及驗收紀錄。

## 新交接公司端 OPERATOR LOGIN_LOT 生產批作業員登入登出（2026-11-20）

- [ ] 閱讀 [生產批作業員登入登出規格](docs/modules/WIP/生產批作業員登入登出.md)、WP104/WP105 與 WP305 references；此頁公司端完成 CallType=MenuItem DEV，BR 呼叫端差異亦已記錄。
- [ ] 核對 YMES PostgreSQL live schema/migrations，重用 `TBLWIPOPERATORSTATE`、`TBLWIPOPERATORLOG`、`TBLWIPLOTLOGINSTATE` 對應資料；無新 Oracle table，不要新增重複 CREATE/migration。
- [ ] 實作登入保護的 Lot mode=5 資料查詢、簽入／簽出 commands。僅允許 Lot Status 0/1；server 重查 OP MultiOperatorMode=5、使用者有效性／OP 權限及 PreOPEndTime/WIPDate、既有區間與暫存狀態；不接受 browser 指定 LoginPlace、Mode、Creator 或 server time。
- [ ] 交易中同步 OperatorState/OperatorLog/LotLoginState 及 `funSHR_CI_EMP_ClockIn`／`funSHR_CO_EMP_ClockOut` 等價副作用。保留 Queue 或 MultiOperator 關閉時刪除 mode 5 現況的特殊下工分支，以及 BR CallType=1/2/3 的預登、批次暫存與權限行為。
- [ ] Company DEV 僅使用假資料，沒有 PostgreSQL/API，也未執行 build/test/browser；正式 FunctionKey 尚待可靠 menu metadata 核實。

## 新交接公司端 Lot Operator ClockOut 批次作業員下工（2026-11-20）

- [ ] 閱讀 [批次下工規格](docs/modules/WIP/批次作業員下工.md)、WP104/WP105 與 Lot Operator Clockin 規格。本功能公司端完成 DEV 假資料，不含正式 API。
- [ ] 核對 YMES PostgreSQL live schema/migrations；重用 OperatorState/OperatorLog 與 LotLoginState，不新增重複表。候選列查詢需沿用已存在 Lot/OP/Area/Equipment/Group/TempEquipment schema 與 APIs。
- [ ] 實作登入授權 `GET /api/wip/lot-operator-clock-outs/lots`，固定 ClockStatus=1（LoginPlaceNo 非空），LotStatus 預設 Running=1，可接受有權範圍內 UserNo/PSNo/OPNo filter；安全分頁與穩定排序。
- [ ] `POST /api/wip/lot-operator-clock-outs` 僅接選取 LotNo/key。Server 重查登入現況、OP mode/place、group/privilege；operator user 由資料列限定，審計操作者由登入 session 取得，LogoutDate 用 server time。交易內新增 OperatorLog、刪 OperatorState、同步 LotLoginState checkout side effect，加入鎖／冪等／競爭錯誤處理。
- [ ] WinForms 將 CheckFlag 同步套用到同 UserNo+LoginPlaceNo 的所有列，批次組資料再呼叫一次 Service；檢查 mode/place 衝突或來源去重造成的差異。沒有新 Oracle table/DDL；公司端未連 PG、未執行 build/test/browser。

## 新交接公司端 Lot Operator Clockin 批次作業員上工（2026-11-20）

- [ ] 閱讀 [批次上工規格](docs/modules/WIP/批次作業員上工.md)、WP104/WP105 規格與本檔 WP104 交接。公司端完成的是 Vue DEV 假資料頁，不含正式 API。
- [ ] 先核對 YMES PostgreSQL live schema/migrations，重用 OperatorState/OperatorLog、LotLoginState、LotState、OP、Area/Equipment、Temporary Equipment 與 User/Shift/Privilege；本功能沒有新專用表，不新增重複 migration。
- [ ] 實作登入授權、分頁 `GET /api/wip/lot-operator-clock-ins/lots`。必須按 WinForms/Core 邏輯展開 `MultiOperatorMode`／`PartialMode` 與設備、Area、Group、TempEquipment mapping；LotStatus 預設 Running=1、ClockStatus 預設 All=0、1/2 分別判斷 LoginPlaceNo 非空/空。保留 PSNo/OPNo/LotNo 排序並限制查詢範圍。
- [ ] 重用 WP104 員工 challenge，實作 `POST /api/wip/lot-operator-clock-ins`。request 只帶 challenge、WorkDay、選取 LotNo；server 重查所選 LotState/OP mode/place，彙整不同 location 後做 OP privilege、Shift、UserLevel、系統參數與現況檢核。
- [ ] 以一個冪等 command/交易處理 `CLOCKIN_AUTOCLOCKOUT`、OperatorLog/OperatorState、LotLoginState 與 `funSHR_CI_EMP_ClockIn` 同等副作用；AP Server 時間由 server 產生。不可由 Vue 傳任意 OPNo、LoginPlace、MultiOperatorMode、LoginDate、UserLevel、ShiftNo 或 user identity。
- [ ] 正式 menu 的 FunctionNo/FunctionKey 尚未核實；取得可靠 menu metadata 後再調整路由／Function ACL，不採信公司端標題推導的 fallback 值。公司端沒有執行 build/test/browser，也未連 YMES PostgreSQL。

## 新交接公司端 WP105 使用者登出作業（下工）（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP105 規格](docs/modules/WIP/使用者登出作業_下工.md)、WP104 規格、`docs/DATABASE_CONNECTION.md` WP104/WP105 記錄及 Oracle references。
- [ ] WP105 沒有新專用表。先查 YMES PostgreSQL live schema/migration history，重用 OperatorState／OperatorLog 與 LotLoginState；不可重複 CREATE。User/Shift/OP/Area/Equipment/System Parameter 沿用現有共用 schema/API。
- [ ] 重用 WP104 員工密碼驗證 challenge。登入授權讀取只限 challenge 員工的 OperatorState；不可開放全廠現況。LogoutDate 由 server 產生，不接受 browser 時間。
- [ ] 實作逐列 `POST /api/wip/operator-clock-outs/close`：request 只帶 challenge 與伺服器查得現況的 `(multiOperatorMode, loginPlaceNo)` key；server 重新查並鎖定目前狀態，原子新增 OperatorLog、刪除 OperatorState、同步 LotLoginState side effects。Vue 依來源逐列呼叫；失敗停止，回報先前成功及未處理列，保留來源部分成功語意。
- [ ] 加冪等／並行防重、權限與稽核；處理狀態已被其他操作關閉、同使用者相同 place/mode PK 等錯誤。檢查 API 不接受任意 UserNo、時間或工作分鐘。
- [ ] 公司端僅完成 DEV 假資料頁，未連 YMES PostgreSQL、未建立 migration/API，未執行 build/test/browser；家用端完成 schema/API 後更新驗收紀錄。

## 新交接公司端 WP103 列印流程卡（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP103 規格](docs/modules/WIP/列印流程卡.md)、本段與 `docs/DATABASE_CONNECTION.md` 的 WP103 記錄。
- [ ] 先核對 YMES PostgreSQL live schema／migration history 與既有 WIP Lot Basis/State、Process Node/Relation、OP、Product、Customer、RunCard Log、`tblprdRuncardMap`、User Defined Report、System Parameter 及現有 lookup API。WP103 沒有新專用表；不得重建或重複建立 CREATE-only 已涵蓋的共用表，確實缺表才補 migration。
- [ ] 實作登入授權 `GET /api/wip/run-cards/lots`，固定預設 Status 0/1/4，服務端排除 `LotState=0`，保留 HotLot DESC、Priority、LotNo 排序；查詢欄位與參數化／分頁規則依 spec。lookup 重用 Area、Production Segment、OP、MO API。
- [ ] 實作 `POST /api/wip/run-cards/preview`：重驗所選 Lot 的登入者可見範圍，`processScope` 對應 Core `IsAllProcess=2/1/0`，Include Log 控制歷程載入；保留一般流程與 Net Flow 的報表步驟來源、使用者定義 `Runcard` 範本與多 Lot 合併預覽。
- [ ] RunCardStyle 由 server `wsSYS.LoadParameter` 同等設定解析，不接受 browser 自選。Style 0 產生 ActiveReports 相容 PDF；Style 非 0 先載入 `tblprdRuncardMap`，ProductNo 精確配對失敗才 fallback `*`，找不到設定需明確錯誤。確認/移植 `PD_Kit.clsPRDKit.funRunCardExecute` 與 Excel/custom map renderer；未完成時不可回傳假成功。
- [ ] 列印文件回傳 PDF，由使用者在瀏覽器選擇本機印表機；不要讓瀏覽器傳入 server printer/path。相容 WinForms：Style 0 的 Print 僅作用列，Preview 可多選；Style 非 0 的 Print 逐筆處理選取 Lot。定義 PDF 產生錯誤、缺 Template/Map、空流程及大量 Lot 的回應。
- [ ] Production API 完成後更新 schema/API、整合與部署證據。公司端 WP103 Vue 頁只用 DEV 固定假資料，未連 PostgreSQL、未實作 API/migration、未執行 build/test/browser。

## 新交接公司端 WP113 THEIL LOT SHEET PRINT（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP113 規格](docs/modules/WIP/LOT_SHEET_PRINT.md)、本段與 `docs/DATABASE_CONNECTION.md` 的 WP113 記錄。此功能與 WP103 是獨立 WinForms Form／Service／Core／ActiveReports。
- [ ] 先核對 YMES PostgreSQL live schema/migration 與 WP101/WP201 已有 Lot Basis/State/Property、MO、產品、流程、OP 屬性、BOM、材料、Wait/ERF disposition、使用者資料，以及 THEIL Matrix `XX_TBLLOTMARITX_THEIL_BU1`。公司端已有 Oracle source DDL／PG CREATE reference；無新專用表，不要重複 CREATE。真正缺 schema 時才查 live Oracle metadata 和做 forward migration。
- [ ] 實作登入保護 `GET /api/wip/lot-sheet/lots`：至少需要 MO No 或 WO 建立日期區間，Start < End；伺服器依 Core 的 `LotState<>0`、HotLot DESC／Priority／LotNo 排序並分頁，不要加 WP103 的 Status 0/1/4 限制。
- [ ] 實作 `POST /api/wip/lot-sheet/preview`：server 讀 RunCardStyle，重新授權與讀取 Lot；維持 Current/Original Process、重工步驟、Net Flow、MOState 99 RunCard Log、Style 0 同 MO 限制及 THEIL Matrix／產品屬性／BOM／處置／人員等報表資料，回傳 ActiveReports 相容 PDF。
- [ ] 移植 THEIL Style 非 0 RunCardMap 精確配對及 `*` fallback；不可套 WP103 或讓 client 選 report style。無可用 renderer 時要明確回錯，不回空白成功 PDF。
- [ ] 實作 Print 產檔及 `WIP_PRINT_COUNT` 更新：重用 `TBLWIPLOTPROPERTY`，依規格使用 command/idempotency key 與 transaction，處理瀏覽器無法確認實體印出的差異。不可再造 print count table。
- [ ] 實作 `POST /api/wip/lot-sheet/empty-version`：ProductNo/Version，讀產品流程、OP Attribute、BOM、人員資料並使用 THEIL Empty Version 報表；不改 Lot、不增 Print Count。
- [ ] 公司 DEV `/YMES/WIP/LotSheetPrint` 目前是假 Lot／假報表、localStorage 假次數，未連 API/PG，未執行 build/test/browser；家用端完成 API/schema 後更新驗收與部署紀錄。

## 新交接公司端 WP104 使用者登入作業（上工）（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP104 規格](docs/modules/WIP/使用者登入作業_上工.md)、`docs/DATABASE_CONNECTION.md` 的 WP104 段與三個 Oracle reference 檔。公司端只做 Vue DEV fake UI，沒有正式 API。
- [ ] 對照 YMES PostgreSQL live schema／migration history。Oracle source metadata 已證實 OperatorState PK `(userno,multioperatormode,loginplaceno)`、OperatorLog PK `(sid)`；CREATE-only `database/reference/mestest_wip_wp104_create.sql` 只在 live schema 缺表時改為 forward migration。LotLoginState 重用 WP305 表／reference，不重建；Employee/User/Shift/OP/OPArea/Equipment/System Parameter 沿用共用 schema/API。
- [ ] 做登入保護 `POST /api/wip/operator-clock-ins/verify`，以 server 驗證 EmployeeNo/password，節流並遮蔽 request body log；不可將員工密碼存 browser 或寫一般 log。回傳短效 challenge 綁定員工與目前 session。`WIPChkPassword=0` 仍需 server 檢查員工/班別狀態。
- [ ] Context/lookup API 回伺服器時間、`WIPChkPassword`、`CLOCKIN_AUTOCLOCKOUT`、Shift、有效 OP、Area/Equipment；從 Server 主檔查 mode 與使用者 OP privilege，不信任 Vue 傳入的 UserLevel/ShiftNo/登入身份。
- [ ] `POST /api/wip/operator-clock-ins` 用 challenge、OPNo、LoginPlaceNo、WorkDay 及明確 auto-clockout confirmation。Server 重新驗證 user/shift、班別時間、UserLevel!=3、OPPRIV、LoginPlace、工作日與伺服器時間不可未來；多工 mode 0/1/2 規則見 spec，mode 5 回導 WP111，不當一般上工。
- [ ] 一個交易內依 `CLOCKIN_AUTOCLOCKOUT` 決策，必要時結束舊 `TBLWIPOPERATORSTATE` 並新增 `TBLWIPOPERATORLOG`，新增本次 State；多人加工功能啟用時同步生成 `TBLWIPLOTLOGINSTATE`，執行 reverse/update side effects。定義鎖、冪等鍵、PK 衝突、工作日自動下工與 rollback。
- [ ] 公司 DEV `/YMES/WIP/OperatorClockIn` 有固定假員工、密碼、班別、OP／Area／Equipment lookup 與 localStorage 假上工/下工紀錄；未呼叫 API/PG、未執行 build/test/browser。家用端整合後更新 API/schema/驗收/部署紀錄。

## 新交接公司端 WP302 載具現況管理（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP302 規格](docs/modules/WIP/載具現況管理.md)、本段及三張 Oracle source DDL／metadata、CREATE-only reference。
- [ ] 先核對 YMES PostgreSQL live schema／migration history。WP302 新查證表為 `tblwipcarrierstatedetail`、`tblwipcarrierstatedetaillog`、`tblinvstockerstate`；對 live schema 確實缺少的表才新增 forward migration。不要因 CREATE-only reference 對既有 schema 重跑 CREATE。
- [ ] `tblwipcarrierstatedetail` 在 Oracle 沒有 PK，僅有 ComponentNo 非唯一 index；`tblwipcarrierstatedetaillog` 沒有 PK／索引。CREATE-only 不可自行補唯一鍵、FK 或額外欄位。`tblinvstockerstate` 的 PK 是 `(StockerNo, LocatorNo, ContentNo)`。`tblwipcarrierstate`、CarrierType、LotBasis/State、ComponentState 沿用既有 reference/schema。
- [ ] 實作登入授權 GET `/api/wip/carrier-state` 及 `/api/wip/carrier-state/{carrierNo}/details`；回傳欄位依 WinForms：CarrierNo、CarrierCategory、CarrierType、CarrierState、Description；Detail 可見 CarrierPosition、ContentID、BCIP、ComponentNo。完整資料排序需穩定，不能假設 Detail 有主鍵。
- [ ] 實作登入授權 POST `/api/wip/carrier-state/{carrierNo}/put-in`，body 只接 LotNo、起始 Position。Server 自查載具/type 與 `CarrierLayer*CarrierCapacity`、lot `LotState=1` 且 Status 0–5、Stocker 占用、ComponentState、全球既有 ContentID/ComponentNo、CarrierPosition 容量／重複。Components 由 server 查出；無 Component 時使用 `N/A`。UserNo/BCIP 取登入者。
- [ ] WinForms 對多 Component 按位置逐筆 Check→Add，後一筆失敗時先前新增不回滾；若維持相容，API 回傳 `insertedCount` 與停止原因。每次單列檢查加寫入需以交易／鎖原子執行：鎖住 CarrierState 與對應 lot state，再查重複與 position，避免不同使用者同時占用相同位置或同一元件掛到多個載具。
- [ ] 實作 POST `/api/wip/carrier-state/{carrierNo}/take-out`（指定 ContentID，依來源刪掉同 CarrierNo + ContentID 的全部元件列）及 `/clear`（清全部內容）。刪前驗證每個 Lot 都仍符合原 WinForms lot-state 條件；同交易寫 `tblwipcarrierstatedetaillog`、刪 Detail、清除 lot CarrierNo。每個 command 要有冪等性、錯誤碼及並行鎖。
- [ ] 保留來源權限：WinForms `Carrier Change`、`Lot Change` 按鈕預設隱藏且受 `ExeGroupControlPriv` 控制。為完成同等功能，新增受獨立權限保護的 PUT `/api/wip/carrier-state/{carrierNo}/state`（`carrierState` 僅 0/1）與 PUT `/api/wip/carrier-state/{carrierNo}/details/flag`（`contentId`、`componentNo`、`flag` 僅 0/1）；後端記錄登入者稽核，不信任 browser user number。
- [ ] 記錄來源相容性問題：Core 在刪除前查舊 Detail 數量，只有舊數量 <=1 才把 CarrierState 設為 0；Clear 多列後可能留在狀態 1。DEV 已重現。API 若修正為依刪除後內容更新狀態，列為明確行為修正並保留回歸案例。
- [ ] Log DDL 雖有 PutInTime/TakeOutTime，`AddCarrierStateDetailLog` 此流程未寫這兩欄，metadata 沒有 trigger；未經其他來源證據不要自行填入。公司端未連 PostgreSQL、不做 migration；由家用端完成 API/schema/deployment。

## 新交接公司端 WP601 包裝作業家用端接手（2026-11-19）

- [ ] Pull 最新 `main`，閱讀 [WP601 規格](docs/modules/WIP/包裝作業.md)、`database/reference/mestest_wip_wp601_source_ddl.sql`、`mestest_wip_wp601_create.sql`、metadata 與 `docs/DATABASE_CONNECTION.md` 的 WP601 查證紀錄。
- [ ] 先核對 YMES PostgreSQL live schema、migrations、資料、PK／nullable/default；WP601 的 19 張 Oracle 功能表逐張比對。CREATE-only 只是來源結構參考，不可整批執行；只替 live schema 確認缺少的表／索引新增 forward migration。不可建立不存在的通用 `TBLWIPMOLINEPACKL`。
- [ ] 共用 `TBLWIPLOTSTATE`、MO／Product／Packing Rule／Label Rule／Lot Component 與 `TBLINVWIPINVENTORY_SEMI` 等沿用既有 YMES schema/API；不要重複建表。`TBLWIPMOLINEPACKRELATION` Oracle 沒有 PK、只有兩個非唯一索引；`TBLINVPACKMERGELOG` 沒有 PK／index。保留 Oracle key/nullable/precision，不能自行猜 PK/FK。
- [ ] 建登入者授權的設定／查詢 API：ComputerName 的 PackConfig、LabelPrinter；MO／產線／產品 packing rule、L1–L6 level、滿箱箱數及數量、配件、LabelType/printer；查詢每層未封箱箱與 detail、relation 展開的 Lot／packing 明細。參數化、排序及分頁；服務端自行取得電腦與登入者資訊，不信任前端規則。
- [ ] POST command 實作 Pack Lot、Pack closed package、Pack Close、Delete、Combine、Unboxing；伺服器重驗 MO/PDLine/ProductVersion、LotStamp/狀態/OP/BR、重複關係、Status=11 庫存分流、Component、可用數量、上下層 CloseFlag 與各層容量。保留 `CloseFlag=1` 自動封箱／`2` 強制封箱、Lot split、L1–L6 主/明細、Relation、PackState/Log、WIP Lot State、FGD In 關聯及 merge/dismantle log side effects。
- [ ] 所有相關表更新必須在明確交易與適當鎖定/CAS 下原子提交；定義重送 idempotency、防止重複掃碼扣量與並發超容量。不能用 CRUD 或只寫包裝明細取代 `clsLot.ExePacking*` domain 行為。失敗需明確回滾或留下可重試交易狀態。
- [ ] `ObjectFileHandler("INVMOPackingCombineDismantle")`、CodeSoft／BarTender 本機 COM 與標籤範本是外部相依；設計授權的 server-side label job／列印代理與補印契約。不可由 browser 任選 server path/printer，也不能假設 WP602（未掛選單的補印 Form）已完成。
- [ ] 完成後更新 WP601 API/schema/migration、交易回歸／部署證據、規格索引與 TODO。公司端只做 Vue DEV 假資料與 Oracle 唯讀；Production PostgreSQL／API／migration／部署由家用端完成。

## 新交接公司端 WP301 設備上料（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP301 規格](docs/modules/WIP/設備上料.md)、`docs/DATABASE_CONNECTION.md` WP301 段落與四張 Oracle source DDL／metadata。
- [ ] 先核對 YMES PostgreSQL live schema／migrations。WP301 新交接四張表為 `tblwipeqpmaterialstate`、`tblwipeqpmtlstate_comp`、`tbl_binklessmapstate`、`tbl_bsubstratemapstate`；對缺表才新增 forward migration。其餘 13 張相依表沿用 WP101／WP201、OE、MTL、OP/SMD 既有 reference，不重複建表。
- [ ] 建登入授權 GET 查詢：已核准且非 VIRTUAL 設備、狀態 0/1/2/3/6 工單、設備上料階層與元件明細。保持 WinForms NewType=1 語意：指定 MO 時現況 Grid 仍載入該設備全部已上料；指定 MO 影響 mount 投料點與 BOM 主物料。
- [ ] 建 POST `mount`、`unmount`、`sequence-change` commands；不得以 GET 修改。Server 重查設備 IssueState/Class、MO state/BOM、物料／產品有效狀態、CheckLotNo、庫存可用量、投料點、MaterialLevel、半成品批號、元件／基板／晶圓映射、效期／解凍／上料順序。不可信任 client 傳入 CheckLotNo、PutInPlaceType、MaterialLevel、庫存量或 Seq 結果。
- [ ] `mount` 交易需協調投料點扣帳、`tblwipeqpmaterialstate` 新增／累加、`tblwipeqpmtlstate_comp` 與 map states、OpenDate／ExpireDate／AcclimationDate，以及必要 Inventory Raw Log。`unmount` 需按全部物料或單一 Component 語意還原庫存、維護現況／map states、處理 ResetOpenDate 與 Raw Log。`sequence-change` 以設備現況完整鍵原子交換兩列 Seq。
- [ ] 唯一鍵請依 Oracle source：設備現況 `(EquipmentNo, MaterialNo, InputMaterialNo, MaterialLotNo, MONo)`；元件現況 `(MONo, EquipmentNo, MaterialNo, InputMaterialNo, MaterialLotNo, ComponentNo)`；兩個 map table 依規格中各自的完整複合 PK。定義重複請求、跨表交易、鎖／CAS 與明確錯誤碼。
- [ ] Core 有些庫存／半成品查詢僅依 LotNo 並讀第一列；同 Lot 跨 Inventory/Locator 有多列時，API 必須依 InventoryNo／LocatorNo／MO 完整限定或回傳歧義，不可依 DB row order 任選。
- [ ] Vue DEV 使用 `ymes.dev.wp301-eqp-material-loading.v1` localStorage 假資料，沒有接正式 API。公司端不連 PostgreSQL、不做 migration；家用端完成正式 API、schema 比對與部署驗收。

## 新交接公司端 WP629 在製品庫包裝作業（2026-11-19）

- [ ] Pull 最新 `main`，閱讀 [WP629 規格](docs/modules/WIP/在製品庫包裝作業.md)、WP601 包裝規格及 `docs/DATABASE_CONNECTION.md` WP629/WP201/WP601 Oracle reference 記錄。
- [ ] WP629 沒有專屬新表：先核對 YMES PostgreSQL live schema/migrations，重用 WP601 的 L1–L6 包裝表、Detail、Relation、`tblinvpackstate`／`tblinvpacklog`，以及既有 `tblinvwipinventory_semi`、`tblwiplotstate`、MO/Product/Packing Rule/Label Rule。若 live schema 缺少共用物件，只依已核實 Oracle DDL 建缺少的 forward migration；不建立另一套 WP629 表，不重建既有表。
- [ ] 在共用 packing domain service 加入由授權 FunctionKey/command 決定的 inventory-inline 執行模式。不要信任瀏覽器提交 InlinePackingFlag、Status 或 InventoryNo；伺服器重查 Lot `Status=11`，並由 `tblinvwipinventory_semi` 取得 WIP InventoryNo。
- [ ] Pack Lot／package command 需重驗 MO、Product/Version、packing rule/層級、Lot/包裝現況、Component、PackQty、CloseFlag、關係和重複掃碼。展開包裝內的 Lot，確保全部仍是 Status=11、同 InventoryNo，並與目前未封箱內容一致；維持 WP601 的容量、下層先封、配件、分批與 package serial 規則。
- [ ] `TBLINVWIPINVENTORY_SEMI` PK 含 LocatorNo，同一 Lot 可有多個庫位列；舊 Core 只依 LotNo 查、無 `ORDER BY`，WinForms 使用 Rows(0)，無法證明其選庫位為確定規則。API 應偵測多筆有效庫位並明確回報歧義／要求明確庫位規則，不可悄悄依 PostgreSQL row order 任選。
- [ ] 保留 WinForms 掃 Lot／PackingNo 時的 FGD In 關聯檢查；已建立 FGD In 的來源 Lot／包裝禁止重複包裝與拆併。
- [ ] `TBLINVWIPINVENTORY_SEMI` PK 含 LocatorNo，同一 Lot 可有多個庫位列；舊 Core 只依 LotNo 查、無 `ORDER BY`，WinForms 使用 Rows(0)，無法證明其選庫位為確定規則。API 應偵測多筆有效庫位並明確回報歧義／要求明確庫位規則，不可悄悄依 PostgreSQL row order 任選。
- [ ] 依來源把 InventoryNo 作包裝現況 OPNo、NodeID=`N/A`、PkgType=`1`；以交易維護共用包裝 Detail/Relation、PackState/Log。WP629 Pack Close 即使到最高層也不得將 Status=11 改成 Status=4；不要額外改動或扣減 WIP Inventory，除非另有經核實的來源副作用。
- [ ] Delete 維持 Core 的層級 CloseFlag、Detail、Relation、PackState/Log 副作用；Combine／Unboxing 的來源是 `ObjectFileHandler("INVMOPackingCombineDismantle")`，未取得外掛契約時明確保留不支援，不能以 CRUD 回成功。標籤資料與列印須沿用正式 label job/授權代理，不呼叫 server WinForms COM。
- [ ] 對每一跨表 command 定義 transaction、鎖／CAS、冪等鍵及可辨識業務錯誤；先比對現有 WP601 API/migration 是否已提供共用服務，再擴充，不要重複建 endpoint 或 schema。部署與 PostgreSQL 驗收留在家用端；公司端 WP629 畫面只有獨立 DEV localStorage 假資料。



## WIP WP603 在製品庫帳務維護家用端 handoff（2026-11-19）

- [ ] Pull 最新 `main`，閱讀 [WP603 規格](docs/modules/WIP/在製品庫帳務維護.md)、Oracle source DDL `database/reference/mestest_wip_wp603_source_ddl.sql`、CREATE-only `database/reference/mestest_wip_wp603_create.sql` 與資料庫查證紀錄。
- [ ] 先核對 YMES PostgreSQL live schema、migrations、WP101／WP201 的 Lot/WIP 狀態及已共用的 Raw、SEMI、Scrap、Good Bin、Locator、Inventory、Material、Product、MO、OP、Customer、QC Reason 表；CREATE-only 只參考缺欄／型別／PK／索引，不得整份重建既有表。只對真正缺少的物件建立 forward migration。
- [ ] 新增登入授權、分頁及參數化 InventoryNo／Lot／INVDate 查詢 API，回傳 8 個 WinForms 可見分頁及 Component、Good Bin、Scrap Error 明細。Bin 欄位必須以既有 BinOrder metadata 動態排序；前端不得傳自由 SQL、AdditionalXML 或受信任庫存結果。
- [ ] Raw Add/Edit/Delete/Scrap、Scrap／Component Scrap／Fail Bin Delete／ReRun、SEMI Lot／Component／Bin Add/Delete/Merge/Split/Turn Inv 必須依規格接入正式 domain service。重新驗證使用者、Inventory、Locator、Lot／Component／Bin 數量與現況；所有跨表狀態與庫存異動要有明確交易／CAS／鎖定、冪等鍵、操作結果及既有 Log side effects。不能以 CRUD 或 Vue 假操作替代 Core。
- [ ] `TBLWIPCONT_ERROR` Oracle 無 PK；不得自行猜唯一鍵／刪除範圍。九張功能表及五個 standalone index 詳見兩份 DDL；先與 live schema 對照再新增缺少的 migration。
- [ ] Label Print 原本依賴客戶端外部程式、Label binary／資料與印表機；定義伺服器端安全產生資料／下載方案。`SEMI_WAFER` 特殊轉庫及 `WP_Inventory_Raw_Split` 外掛為客製依賴；來源／部署契約不足的分支須明確保留不可用，不猜測交易。
- [ ] 本 Form Designer 預設隱藏 Return／Entrust／Wait Incoming 三頁；只有取得 THEIL Form／部署證據確認啟用後再納入正式 API，勿因 handler 存在就開放功能。
- [ ] 公司端沒有連 YMES PostgreSQL；migration/API、正式 DB 驗證與部署由家用端執行。API 完成後更新文件、資料庫 migration/deployment 記錄與回歸證據。

## 新交接公司端 WP604 半成品庫帳務維護（2026-11-19）

- [ ] Pull 最新 `main`，閱讀 [WP604 規格](docs/modules/WIP/半成品庫帳務維護.md)、`database/reference/mestest_wip_wp604_source_ddl.sql`、`mestest_wip_wp604_create.sql` 及 WP604 資料庫查證紀錄。
- [ ] 逐表核對 YMES PostgreSQL live schema、migrations、欄位型別、nullable/default、PK、索引與現有資料；CREATE-only 僅供比對，缺表才新增 forward migration。Oracle 查到的功能表為 `tblinvsemiinbasis`、`tblinvsemiindetail`、`tblinvsemiincomponent`、`tblinvsemiinventorycomponent`；`tblinvsemiinventory` 和 `tblinvtransactionstate/log` 沿用 WP201／WP605，不重複建表。
- [ ] 實作登入保護的 InventoryType=2 lookup、SEMI Inventory 查詢、Component 優先／Good Bin fallback 明細 API；保留 LocQty>0、排序及 TBLSYSBIN.BINORDER 動態欄位規則。
- [ ] SEMI In 單據僅查 State=0，Confirm 需以服務端固定交易種類 Source=20 執行。注意 `TBLINVSEMIINBASIS.Source` 是另一欄：0=手動外購、1=From WIPInventory_SEMI；Source=1 需同交易扣來源庫存。重用 `TBLINVTRANSACTIONSTATE/LOG`，處理 Basis 0→1→2/3、防重、並發鎖、庫存/Component/Lot State、容量與 release log。
- [ ] Turn API 需檢查來源數量與狀態、目的庫別/核准 Locator、容量及並發；WIP/FGD/SEMI 三向搬動的 Inventory、Component、Lot/Component State、reverse state 與 release log 同交易保存。檢查 `SEMI_WAFER`、`TBLWIPCOMPRULEMAP_*`、`TBLINVTemp_Component` 及外部 release-log handler 相依，缺乏來源時標示不可用，不略過副作用。
- [ ] 公司端 Vue 頁 `/YMES/WIP/SemiInventory` 目前僅用 localStorage DEV 假資料，沒有 Production API/migration；家用端完成後更新規格與共用交易契約。公司電腦不連 YMES PostgreSQL。

## 新交接公司端 WP605 成品庫帳務維護（2026-11-19）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/WIP/成品庫帳務維護.md`、`database/reference/mestest_wip_wp605_source_ddl.sql`、`mestest_wip_wp605_create.sql` 及公司端 Oracle 查證紀錄。
- [ ] 先比對 YMES PostgreSQL live schema／migrations，再決定 forward migration。Oracle MESTEST 本次確認存在且需要評估的六張表：`tblinvfgdinventorycomponent`、`tblinvfgdinbasis`、`tblinvfgdindetail`、`tblinvfgdincomponent`、`tblinvtransactionstate`、`tblinvtransactionlog`。逐表檢查，不可直接執行 CREATE-only 覆蓋既有表。
- [ ] 共用表沿用 WP201／WP101、SMD、PRD、OE references；重用 `tblinvfgdinventory`、`tblinvlocatorbasis`、WIP Lot／Component、`tblwipgoodbinstate`、`tblsysbin`、`tblinvwipinventory_component`、`tblinvwipinventory_semi` 與 `tbloemobasis`，禁止重複建表。Oracle 未找到 `TBLINVFGDINVENTORYBIN`，UI Bin 來源是 `TBLWIPGOODBINSTATE` 的動態 Bin 欄位。
- [ ] 實作登入保護 WIP FGD Inventory API：庫別／儲位 lookup、庫存查詢、component/bin 明細、Add upsert、逐筆 Del、容量／並發檢查及 FGD→FGD／SEMI／WIP 轉庫。服務端重查 IssueState、Lot 狀態、容量和庫存；依 spec 保留各 Core 路徑的庫存／Lot／Component／release log side effects，釐清外部 `iMESWIPReleaseLogHandler.dll` 契約。
- [ ] FGD In Source=30 必須一次涵蓋 TransactionID 產生、TransactionState、Basis 0→1→2/3、ExeFGDIn 過帳、State→Log 搬移及防重送；Source=1 還需扣來源 SEMI/WIP inventory。不可僅新增 queue row 或只改 Basis 狀態。`TBLINVTRANSACTIONLOG` 是成功交易必要表，Oracle 查證只有 PK unique index、無 FK／trigger／額外 index。
- [ ] DEV localStorage 並不實際過帳庫存；Production API／schema 完成後依模組規格更新索引、handoff、測試／部署紀錄，公司端再 pull 核對 WinForms 契約。公司端不連 PostgreSQL。

## 前置家用端交接（歷史待辦）

## 新交接公司端 WP102 生產批開立_批次（2026-11-19）

- [ ] Pull 最新 main，讀 docs/modules/WIP/生產批開立_批次.md、database/reference/mestest_wip_wp102_source_ddl.sql 與 mestest_wip_wp102_create.sql。
- [ ] 比對 YMES PostgreSQL live schema/migrations：WP102 新增需求只有 ymes.tblwiplotproperty_temp；tblwiplotproperty 已列於 WP101 schema/reference。工單、WIP LotBasis/State/Process 與 BOM/Process/Serial lookup 都是共用表，禁止重建或覆蓋。
- [ ] 若 TEMP table 不存在，先驗證備份再新增 forward migration；欄位為 LOTNO varchar(50) NOT NULL、PROPERTYNO varchar(20) NOT NULL、PROPERTYVALUE varchar(255) NULL、LOTSERIAL varchar(55) NULL，PK (LOTNO, PROPERTYNO)，無 FK/額外索引。Create-only SQL 僅是空表參考。
- [ ] 接登入保護 WP102 批次 API：重查 MOState=3、UnReleaseLotQty、Product LotBase、MO Process、BOM 及 SYS serial mode；不可信任 UI 顯示欄位，也不能使用 DEV 批號。自動序號由 server 驗證並原子保留，沿用 WP101 LotBasis/State/Process/Property domain logic，不重複實作 CRUD。
- [ ] 一次批次請求逐筆回報結果；每筆交易需鎖定／條件扣減 MO 餘量，執行建批、StartLot、LotCreate material deduction、狀態 0→1、Property/SAP data。保留舊 Core 前批已提交、後批失敗可能部分成功的語意；不可錯誤宣稱全回滾。
- [ ] 處理 Service 的 TEMP staging 生命週期：若使用 tblwiplotproperty_temp，依 LOTNO/PROPERTYNO 隔離並於失敗時清理；亦可採 transaction 內 staging，但要保留逐批結果。檢查 WIP_BIGQTY/WIP_SORTQTY、WIP_CIMLOTNO/WIP_INSP_NO/WIP_M_LOTNO property 對應及 SAP_PCSQTY/SAP_ARGQTY/SAP_PRDQTY 欄位。
- [ ] API 完成後更新 WP102 規格、TODO、forward migration、交易回歸及部署紀錄；公司端 pull 後再接 Vue contract。公司端不連 PostgreSQL；Oracle CREATE-only 不可直接執行到 YMES。

## 新交接公司端 WP101 生產批開立（2026-11-19）

- [ ] 先 pull 最新 main，再讀 docs/modules/WIP/生產批開立.md 與 database/reference/mestest_wip_wp101_source_ddl.sql / mestest_wip_wp101_create.sql。
- [ ] 對 YMES live schema、migration 與現有資料逐一比對七張來源表；表已存在就沿用，不重建、不覆蓋資料。只在確認缺物件且完成備份後新增 forward migration。工單／產品／流程／元件／Lot Property metadata 都是共用資料，禁止重複建表。
- [ ] 實作工單候選與批次查詢、Add/Edit/Copy/Delete/Release API。新增／修改須交易性保存 lot basis、流程、起始站、元件、略過 OP、Property 與 MO component booking。
- [ ] Delete 必須做 CloseLot 等價 WIP 狀態與關聯清理，不可直接刪主表。Release 需交易內鎖定並重查 MO 未下線量、LotState、元件 ReleaseState，再處理扣料、StartLot、lot state/WIPDate、MO 扣量與 property transfer。
- [ ] 動態 Lot Property 寫入須使用 metadata table/field 白名單；未下線量歸零時委外庫存、ECN 及自動 BR/OP/TransferLot 的後續語意依規格補齊。批次 API 每列獨立結果、第一筆失敗後先前成功不可謊稱回滾。
- [ ] 防止並發超量開批：以 DB row lock 或條件式扣量／影響筆數 CAS 保護，不能只做無鎖讀取後比較。補 API／schema 變更、交易回歸與部署紀錄。
- [ ] CREATE-only SQL 僅作型別／約束參考，不可整份直接執行取代 live schema；不得把 Oracle 連線秘密寫入 repo。

## 公司端新交接（2026-11-19，OE／QC／PRD）

- [ ] [OE101／OE102](docs/MODULE_REPLICATION_SPEC_INDEX.md#工單管理--訂單) 訂單主檔、明細、屬性與結案。已提供四張表的 Oracle DDL 及 CREATE-only 參考。先核對 live schema，再實作正式 API、資料驗證及狀態互斥。
- [ ] [OE201–OE205](docs/MODULE_REPLICATION_SPEC_INDEX.md#工單管理--工單) 工單基本資料、刪除、查詢及兩種達交率。先核對五張工單表和後續用料、ERP、挑片、簽核等相依表的現況。保留兩個報表對缺日期分母與 LeadTime 的各自規則。
- [ ] [OE206](docs/modules/OE/工單用料清單.md) 工單用料與存料 DDL 已交付。正式 API 須跟工單刪除、發放、庫房扣料及 OE207 存料現況共用鎖與狀態。
- [ ] [OE207](docs/modules/OE/工單存料現況.md) 衍生存料查詢及元件唯讀查詢。元件表 Oracle DDL 和 CREATE-only 已備妥。
- [ ] [OE208](docs/modules/OE/工單結案.md) 工單結案共用 OE201 DDL。正式 API 需以 WIP／庫房與 ERP 資料檢查結案，保留舊狀態與版本條件。
- [ ] [OE209](docs/modules/OE/工單元件資料維護作業.md) 工單元件維護共用 OE201 元件 DDL，新增異動紀錄表參考。正式 API 須處理序號、同秒異動、單位轉換及同交易保存稽核。
- [ ] [OEB01](docs/modules/OE/工單挑片資料查詢.md) 正式唯讀挑片資料與安全的動態測項欄位。已提供測試 Recipe 表及既有挑片配置 DDL 連結。
- [ ] [OEP01](docs/modules/OE/工單開立_晶片測試.md) 共用工單表，補半成品關聯表的 Oracle DDL 及 CREATE-only。正式 PKG 交易需覆蓋挑片、元件分派、發料及版本驗證。
- [ ] [QC103](docs/modules/QC/抽樣計劃.md#公司端-formcore-與函式目錄查證2026-11-19) 已有 FUNSA、Type 4／6 旗標及 QC301 一般／Wafer 選表來源。QC107 實際字典值與部署開關仍待交付，QC108 標準 FUNQR 方法名單均對應空實作。
- [ ] [PD206](docs/modules/PRD/產品抽樣頻率規則.md#父主檔生命週期來源查證) 已補產品、設備與站點刪除／停用及 WIP 萬用鍵順序。父資料暫不存在及競態處理仍須由 YMES 明定。

## 家用端本輪交付與接續（2026-11-19，QC103／QC107／QC108）

- [x] QC103 抽樣計劃、QC107 計數判別法則、QC108 計量法則的獨立主檔 API/Web 已部署；正式 app-browser 實測範圍、來源依賴限制與清理證據見各模組 MD。缺少真實算法、ValueList 或共用簽核的分支仍明確回應不可用，沒有使用 DEV 資料替代。
- [x] migration 0034–0037、0041 已核對來源並套用，包含 QC301/QC302 的真實預設法則依賴；0038–0040 已完成 PRD schema 基礎，PRD API/Web 尚待下一批部署與驗收。三份備份已用 pg_restore --list 驗證，見 docs/DATABASE_CONNECTION.md。
- [x] 修正既有八個 repository 的資料庫物件存在檢查，使用 pg_catalog 區分缺表與權限不足，避免未取得權限時被當成缺表而略過必要驗證。相關 API 讀取回歸通過，維護規範見 docs/DATABASE_OBJECT_VISIBILITY.md。
- [x] QC301/QC302 的預設法則、引用鎖、逐列交易與故障回滾已實測；QC302 壞 XLSX／空資料列錯誤處理、乾淨錯誤訊息與站內匯入確認窗已驗證。QC301 改用真實核准法則目錄，抽樣選項不可用時只限制相關編輯功能。
- [x] QC401/QC402 真實瀏覽器選檔、預覽、重複列與部分成功已通過。新增 YMES 標準無業務資料 XLSX 範本（非舊 MES binary）；範本 API 200，格式以 ExcelDataReader 與獨立讀取器驗證。
- [x] QC401/QC402 取消後結果不確定狀態已修正並通過正式瀏覽器重驗；兩者均提示可能部分保存並重新查詢，不誤報成功或整批回滾。實測自建資料已精確清理。
- [ ] 下一批接續已準備的 PRD Program／Recipe Group 與產品 nullable CAS、PS103 schema/API；之後修復 QC301 全部寫入契約的 nullable state/stamp 及 Rule 開窗 snapshot。尚未驗收的工作不標成完成。
- [ ] 保留既有 QC201/QC202 本機草稿，待共用依賴補齊後完成正式驗收。先同步經驗證版本、正常合併公司端新提交，再繼續所有可執行家用端待辦，不 force push。

## 家用端交付與下一批（2026-11-19）

- [x] QC101、QC102、QC401、QC402、PS101 API/Web 已部署；各頁 CRUD、適用的核准/停用/多選及版本操作已由正式 app-browser 驗證，精確測試鍵SQL計數皆0。完整範圍與限制見各模組MD。
- [x] forward migrations 0029–0033 已套用，來源欄位／PK／nullable/default及ymes_app grants已核對；備份 `ymes_before_0029_0030_20261118.dump`、`ymes_before_0031_0033_20261118.dump` 已驗證可讀。
- [ ] QC401/QC402 瀏覽器選檔工具停滯；真實 multipart預覽/匯入API及頁面重查已驗證，但完整選檔端到端、取消與匯入故障注入仍待處理。官方範本及共用Issue/Job待公司來源／共用服務，不冒充已完成。
- [ ] 完成本輪同步後接 QC103、QC107、QC108：程式已準備並經交叉檢查，六張來源表live audit均缺少，下一輪備份後新增forward migration及正式驗收。QC301/QC302新增跨模組鎖定也須回歸驗證。
- [ ] 單一模組完成不是停止點；同步公司端更新後繼續其餘可獨立主檔與共用依賴。

## 家用端持續開發進度（2026-11-18）

- [x] QC503 Driver 與 PS102 製造層別 API/Web 已部署，正式 app-browser 操作驗證完成；測試資料 SQL 計數為 0，API/Web healthy、readiness=200。
- [x] 本輪 Web 以本機 vue-tsc/Vite 驗證後產物建立 runtime image，解決遠端 Node heap OOM；作法見 `DEPLOYMENT_PROFILES.md`。
- [x] QC401/QC102/PS101/QC101/QC402 本輪主要功能已部署驗證；詳細限制與下一批見2026-11-19交付段落。


## 家用端接手摘要（2026-11-18）

- [x] QC501/QC502 已部署並完成本輪正式 app-browser 操作驗收，測試資料清空；細節見本檔 QC501/QC502 完成紀錄與各模組文件。
- [x] 已整合公司端 `28bc913` 及其前 23 筆新增提交，保留全部 DDL、規格與 UI；處理雙方文件衝突，補正 PRD/PRS 的 TypeScript/初始化/換版格式錯誤。完整 .NET Release 與 Web Vite build 成功。
- [x] 合併新增頁在本機 Development 完成相關修正的基本操作檢查；PRD/PRS 正式 API 與完整 CRUD 驗收仍依下方交接，未標成正式可用。正式站本輪發行範圍為 QC501/QC502。
- [x] GitHub main 已同步整合提交 `340db5f`，包含公司端 `7125143`，未使用 force push。下一個家用端功能先接 QC503「檢測設備連線程式」；沿用 migration 0027 的 Driver 表及 QC501/QC502 共用查找。

## 已完成家用端：PS102 製造層別設定（2026-11-18）

- [x] Live schema 核對確認缺表，驗證備份 `/opt/ymes/backups/ymes_before_0028_ps102_20261118.dump` 後套用 migration `0028_ps102_fab_prs_stage_basis.sql`；來源 PK、Sequence 全表唯一、nullable/default 及 `ymes_app` CRUD grants 均保留。
- [x] 已部署登入保護清單/CRUD/核准、Sequence 1–9999/全表唯一、nullable DataStamp CAS、server audit、0/-1 實刪與 2→-1 停用；有 Issue/Job 表但共用流程未接入時回 503，不改資料。
- [x] 正式 app-browser 驗證新增/套用後修改、相同 Sequence 編輯、複製防重、四狀態/查詢、多選核准取消/確定、多選停用/刪除與 Close；測試 A/B 已清空。規格及維護紀錄見 `docs/modules/PRS/製造層別設定.md`。
- [ ] 共用簽核服務日後建立時接入完整 IssueManagement；正式 API 的功能角色 ACL 依使用者目前暫緩決策維持登入保護。

## 已完成家用端：PS101 生產區段資料（2026-11-19）

- [x] migration0030、API/共用LOTNO查找、版本及正式頁面主要操作已部署驗證；細節及未測範圍見 `docs/modules/PRS/生產區段資料.md`。
- [ ] 共用完整簽核與後續相關PS模組仍繼續接手。

## 最新公司端交接：PS103 流程類別資料（2026-11-18）

- [ ] Pull 最新 GitHub `main`，讀 `docs/modules/PRS/流程類別資料.md`、`docs/DATABASE_CONNECTION.md` 的 PS103 段落，以及 `database/reference/mestest_prs103_process_type_source_ddl.sql` / `mestest_prs103_process_type_create.sql`。
- [ ] 查 YMES PostgreSQL live schema/migrations 的 `YMES.tblprstype`。PS103 是此表的 owner；先核對欄位、PK、nullable/default 和現存資料。缺表才新增 forward migration；若已存在，保留資料，只補 live schema 確認缺少的 default，禁止重建。
- [ ] Oracle 結構：`processtype varchar(20) NOT NULL` PK；`description varchar(255) NULL`；creator/reviser varchar(10) nullable；create/revise date nullable；`issuestate numeric(1,0) DEFAULT 0 NOT NULL`；`datastamp numeric(11,0) DEFAULT 0 NULL`。來源僅 PK unique index，無 FK/額外 index。PostgreSQL CREATE 檔是參考，不是直接部署 migration。
- [ ] 實作登入保護 FunctionNo=`PRS TYPE`、FunctionKey=`PS103` API：GET `/api/process-types?issueState=&processType=`、POST、PUT `/{processType}`、DELETE `/{processType}`、POST `/{processType}/approve`。Vue DEV 頁 `/YMES/PRS/ProcessType` 已固定此契約。
- [ ] Add/Copy 共用新增流程：ProcessType 必填、正規化大寫、最多 20；Description 最多 255；server 設 Creator/CreateDate。除 PK unique 外，新增時還要以不分大小寫檢查 `TBLPRSProcessBasis.ProcessNo` 衝突，來源 clsProcess 明確禁止兩種識別碼相同。
- [ ] Edit 僅允許 IssueState=0、ProcessType PK 不可改，只更新 Description、Reviser/ReviseDate，DataStamp null-safe CAS 並遞增，競爭回 409。IssueState 0/-1 刪除時同一 transaction 先清 `tblprsprocessproperty` 中 `processno=ProcessType` 的列再刪 `tblprstype`；狀態 2 CAS 改 -1，狀態 1 禁止。多選 UI 是逐筆處理，回報每筆成功/失敗。
- [ ] Approve 只允許狀態 0，重用共用 IssueManagement IssueNo=`PRS TYPE`。無 Job/Detail 直升 2；有簽核設定先轉 1、發第一關通知，callback 核准 2/退回 0；API 不得略過簽核流程。
- [ ] Property 按鈕重用 shared `TBLPRSProcessProperty` API。legacy 以 ProcessNo=ProcessType 開啟且 ProcessVersion 未指定，列表查詢省略版本以載入該 ProcessNo 的所有版本；Property 編輯畫面空白 ProcessVersion 對應資料庫鍵值 -1，新增、修改、刪除均須以 -1 傳送而 UI 顯示空白；default value 需重用 Element 驗證，新增檢查 ProcessNo/version，核准 IssueNo=`PRS PROPERTY`。勿將屬性列複製到新 ProcessType。協調 PS104，避免複製兩套 property controller。
- [ ] PS104 `GET /api/process-basis/lookups/process-types?issueState=2` 應查同一張 `tblprstype`，重用 PS103 schema/lookup，不另建 lookup 表。完成 schema/API 後更新規格和 TODO、commit/push `main`；公司端再 pull 對接。公司端 PS103 使用 DEV localStorage，未連 YMES PostgreSQL，也未執行 build/test/browser。

## 最新公司端交接：PS104 流程基本資料（2026-11-18）

- [ ] Pull GitHub `main`，先讀 `docs/modules/PRS/流程基本資料.md`、`docs/DATABASE_CONNECTION.md` 的 PS104 段落，以及 `database/reference/mestest_prs104_process_basis_source_ddl.sql` / `mestest_prs104_process_basis_create.sql`。
- [ ] 比對 YMES PostgreSQL live schema、migration 和欄位 defaults。Create SQL 是對照，不可直接整份執行、不可重建既有表。First-phase 有 `tblprsprocessbasis`、`tblprsnodebasis`、`tblprsopapsdetail` 三表；確認 live objects 後，只為缺失建立 migration。
- [ ] Oracle DDL 中三個 `VARCHAR2(n CHAR)` 長度與 PostgreSQL varchar(n) 相同；`ALL_TAB_COLUMNS.DATA_LENGTH` 是 bytes，不是來源字元數，不要用 byte 數誤改欄寬。
- [ ] Source defaults 與 first-phase Create 目前可見差異：ProcessBasis 的 IssueState/TuningProcess/CurVersion/PartialType/DataStamp = 0；NodeBasis 的 NodeType=0、NodeVersion='N/A'；OPAPSDetail 的 ApsFixEqpTime/ApsVarEqpTime/BatchOP=0、BatchMaxQty=999999。先查 live `pg_attrdef`，只對 live 缺少的 default 加 forward `ALTER COLUMN SET DEFAULT`；保留 nullable/原資料。
- [ ] 首次新增 PS104 專屬的四張缺表：`tblprsnoderelation`、`tblprsnodexml_clob`、`tblprsprocessproperty`、`tblprsopapsbase`。`tblprstype` 由 PS103 建立、`tblprsproductionsegment` 由 PS101 建立；PS104 重用兩張共用表及其 API/lookup，不另建重複表。欄位、PK、NULL/default 依 Oracle DDL/CREATE 參照，Oracle source 僅 PK unique index，無 FK/額外 index。不要自行新增索引/FK。
- [ ] 完成登入保護 PS104 API，路徑、DTO 和 DEV Vue 實際呼叫見功能規格：process basis list/Add/Edit/Delete/Approve/Copy/Version、Property CRUD/Issue、Exclude OP、已核准 PS/ProcessType lookup 及 Excel import。
- [ ] IssueState 語意：0 未核准、1 簽核中、2 已核准、-1 不使用。Edit/Draw 只允許 0；Version 只允許 Active/current；刪除 0/-1 實體清理子表，2 用 DataStamp CAS 標成 -1，1 禁止。多表 delete/copy 使用單一 transaction。Creator/Reviser/時間由 API principal/server 設定；DataStamp null-safe CAS，衝突回 409。
- [ ] 核准不能由 API 直接改狀態：核對非空流程圖、Exclude OP；無 Issue Job/Detail 才可 direct Active，有設定則走共用 IssueManagement IssueNo=`PRS BASIS`，維持 Pending、簽核 callback、第一關通知與 CurVersion 前版切換。
- [ ] Flow Form 依節點/連線表和 `NodeXmlString`/CLOB snapshot。舊 XML 由外部 iMESXMLFlow.dll 序列化；未證明相容前不要把 Vue JSON 寫入 XML 欄位，也不可對外宣稱 Web 流程圖可和舊 WinForms 互通。決定 serializer 方案後才開放 Production graph write。
- [ ] 流程匯入舊程式只讀 xls/xlsx 第一個工作表、逐 ProcessNo/Version 呼叫外部 `DataImporter.DataImport.ProcessDataChecker`；程式碼不在來源樹。檢查部署是否可取得該 DLL/規則，否則以 API 實作核准作業站、同一區段、類別、重複主鍵、逐筆結果及取消/背景作業，不可回傳假成功。
- [ ] 完成 schema/API 後更新規格與 TODO、commit/push `main`，公司端再 pull 對接。公司端 PS104 Vue DEV 使用 localStorage，未連 YMES PostgreSQL；未執行 build/test/browser。

## 最新公司端交接：OPB01 作業站參數群組設定（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/OP/作業站參數群組設定.md`、`docs/DATABASE_CONNECTION.md` OPB01 段落，以及 Oracle source DDL / PostgreSQL CREATE-only 對照。
- [ ] 先比對 YMES live schema/migrations 的 `YMES.tblopgroupattrib`。Oracle DDL 在 `database/reference/mestest_opb01_operation_group_attribute_source_ddl.sql`；空表 CREATE 參考在 `database/reference/mestest_opb01_operation_group_attribute_create.sql`。只缺表才新增 forward migration；既有表只做必要 ALTER，不重建、不覆蓋資料。
- [ ] `TBLSYSELEMENTBASIS` 已由 first-phase schema 提供，不要建立重複 Element 表；重用客戶基本資料與 OPB02 ECN 群組 lookup。
- [ ] 實作登入保護的 OPB01 API，FunctionNo=`B_OP GROUP ATTRIB`、FunctionKey=`OPB01`。Vue `OperationGroupAttribMaster.vue` 已固定 `GET /api/operation-group-attributes`、`GET .../editor`、`POST .../batch-save`、`DELETE ...`、三種 lookup、Element values/validation 及 `GET /api/system/parameters/InputAbortLog` 路徑；DTO 見模組規格。
- [ ] 主表複合 PK `(CUSTOMERNO, OPGROUPNO, ELEMENTNO)`；來源 NOT NULL、欄長、nullable/default 照 Oracle DDL。`BYSECONDCLASS`、`DATASTAMP` nullable DEFAULT 0。Oracle 只有 PK unique index，無 FK/其他 index；不要自行加 FK/index。
- [ ] 查找僅用已核准客戶、`TBLOPECNGROUPBASIS.ISSUESTATE=2` ECN 群組、`TBLSYSELEMENTBASIS.ISSUESTATE=2 AND ELEMENTTYPE=5` 產品 Element。CustomerNo=`*` 表共用。Select List values 重用 `wsSYS.LoadElementValue`；Key In 驗證重用 `wsSYS.ChkElementValueValid`/SYS Core，不能只檢查前端清單。
- [ ] Batch Save 使用單一 transaction，依新增/修改/刪除旗標處理所有列；任一筆失敗整批 rollback。修改用 DataStamp null-safe CAS，舊值 NULL 以 `IS NULL` 比對，成功遞增；未命中回 HTTP 409。Audit 僅由 API principal/server 設定。
- [ ] 複製改變 CustomerNo 或 OPGroupNo 時，對目的複合鍵新增存活明細，原群組不變；目的 PK 已存在則沿用舊 Copy Core 的略過語義。DataFlag=2 的刪除列不可複製。
- [ ] 多選主 Grid 刪除依舊 WinForms 逐筆呼叫；每筆處理 optional `InputAbortLog`，不得把 UI request 當成批次原子交易。Element editor 的多列 Apply/Confirm 則必須是原子 batch transaction。
- [ ] 檢查 YMES 權限、既有客戶/ECN/System Element API，並更新 API/schema 規格與 TODOLIST。完成後 commit/push `main`，公司端再 pull 對接 Production API。DEV localStorage 假資料不可寫正式資料庫。

## 最新公司端交接：OPB02 ECN 作業站群組設定（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/OP/ECN作業站群組設定.md`、`docs/DATABASE_CONNECTION.md` OPB02 段落，以及 Oracle source DDL / PostgreSQL CREATE 對照。
- [ ] 比對 YMES PostgreSQL live schema/migrations：`YMES.tblopecngroupbasis`、`YMES.tblopecngroupdetail`。Oracle source DDL 在 `database/reference/mestest_opb02_ecn_operation_group_source_ddl.sql`；CREATE-only 參考在 `database/reference/mestest_opb02_ecn_operation_group_create.sql`。只補缺表的 forward migration；既有表只做必要 alter migration，不重建、不覆蓋資料。
- [ ] 實作 FunctionNo=`B_ECN OP GROUP BASIS`、FunctionKey=`OPB02` 的登入/API 權限及規格所列 endpoints。Vue 固定使用 `/api/ecn-operation-groups`、`/{opGroupNo}/operations`、`/lookups/production-segments`、`/lookups/operations`、POST/PUT/DELETE、`/{opGroupNo}/approve`。
- [ ] 主表 `OPGROUPNO VARCHAR2(20)` PK；GroupName 50、PSNo 50、Description 255；OPGroupOrder NUMBER(6,0) nullable；IssueState NUMBER(1,0) NOT NULL 且 Oracle 無 default；DataStamp NUMBER(11,0) nullable DEFAULT 0。保留來源 NULL/default，DataStamp 用 null-safe CAS，衝突回 409；audit 僅由 API principal/server 設定。
- [ ] Detail PK 是 `OPNO` 單欄全表唯一（不是 `(OPGROUPNO, OPNO)`），OPGroupNo/OPNo 都必填 20 字元。新增只能選 `TBLOPBASIS.IssueState=2`、`PSNo` 與所屬群組完全相等，且目前未出現在任何 `TBLOPECNGROUPDETAIL` 的 OP；這是全域排除。不要把 `PSNo='*'` 當萬用值。
- [ ] 主檔修改僅狀態 0；刪除時 DB 狀態 0/-1 在單一 transaction 先刪 detail 再刪 basis，狀態 2 採 DataStamp CAS 改 -1 並保留 details，狀態 1 禁止。不可相信 request body 的 IssueState 作為授權依據。來源沒有 FK 或 PK 以外的 index，不要自行新增。
- [ ] 核准使用 IssueNo=`OP ECN GROUP BASIS`：重用共用 IssueManagement。無 Job 或 Job 無 Detail 時直接 0→2；否則建立 Job/Detail、設定 1，核准 SQL 更新 2、退回 SQL 還原 0，通知第一關。不要在 Vue/API 繞過既有簽核服務或遺漏通知。
- [ ] 明細新增/刪除為逐筆操作、沒有明細 Edit；明細可在任何群組狀態操作（WinForms 沒有狀態限制）。明細新增需重驗群組、OP 核准狀態、PS 相同及全表 OPNo 唯一，刪除實體資料。
- [ ] API/schema 完成後更新模組規格與 TODO、提交並 push `main`，供公司端 pull。DEV localStorage 假資料不得寫入正式資料庫。

## 最新公司端交接：OPB03 客戶站點對應設定（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/客戶站點對應設定.md、OP101–OP105 規格，以及 database/reference/mestest_opb03_customer_op_mapping_source_ddl.sql 和 mestest_opb03_customer_op_mapping_create.sql。
- [ ] 先核對 YMES PostgreSQL live schema/migrations 的 tbl_bopcustopmapbasis、tbl_bopcustopmapdetail。Oracle DDL 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連查證；source 只有兩表 PK、無 FK/額外索引。CREATE-only 檔不是直接部署指令；僅缺表才新增 forward migration，既有表先比欄位與資料。
- [ ] 實作登入保護 FunctionNo=B_CUSTOMER OP MAPPING API。Vue CustomerOPMappingMaster.vue 頁面 /YMES/OP/CustomerOPMapping 已固定主/明細 CRUD 路徑及 lookup 路徑，細節見規格。
- [ ] lookup 重用既有 Customer Basis、Production Segment、OP Basis API/資料；客戶與區段、作業站都只提供 IssueState=2。作業站需依主檔 PSNo 過濾，不新增相同主檔 lookup tables。
- [ ] Basis PK (CustomerNo, CustomerOPNo)；Detail PK (CustomerNo, CustomerOPNo, OPNo)。必填及字數依 Oracle source DDL；TransactionFlag 僅 0/1。來源 CustomerOPSeq NUMBER(3,0)，但 WinForms NumericUpDown 範圍 0–999999；UI 保留原範圍，API 回清楚 DB 欄寬驗證錯誤，除非查 live schema/現存資料後另有遷移決定。
- [ ] Edit 主檔 PK 不可變；已有明細時 PSNo 不可改。明細可修改 OPNo，路由用舊 OPNo 對應、body 帶新 OPNo，需驗複合 PK 唯一。
- [ ] DataStamp nullable + DEFAULT 0；Basis/Detail edit 都要 null-safe CAS，衝突回 409；Creator/CreateDate/Reviser/ReviseDate 只由 API principal/server 產生，忽略 request audit 欄位。
- [ ] 主檔刪除在單一 DB transaction 先刪全部 Detail 再刪 Basis；Oracle 無 FK，不要假定 DB cascade。Detail delete 為實體刪除。
- [ ] Production API/schema 完成後更新規格與交接、提交並 push main，供公司端 pull 整合。公司端尚未接 YMES PostgreSQL；未執行 build/test/browser。
## 最新公司端交接：OP105 作業站群組資料（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/作業站群組資料.md、docs/DATABASE_CONNECTION.md 的 OP105 段落、OP101–OP104 規格，以及 database/reference/mestest_op105_group_basis_source_ddl.sql 與 mestest_op105_group_basis_create.sql。
- [ ] 先核對 YMES live schema/migrations 的 tblopgroupbasis、tblopgroupdetail。Oracle 原始欄位、PK/NULL/default 已直接查證。database/reference/mestest_first_phase_create_tables.sql 已校正新庫預設值；CREATE TABLE IF NOT EXISTS 不會更新既有表。若 live schema 缺 DEFAULT 0，依現有 migration 慣例只補必要 forward migration，不重建表、不覆蓋資料。
- [ ] 實作 FunctionNo=OP GROUP BASIS 登入/API 權限。Vue OperationGroupMaster.vue Production 路徑：GET /api/operation-groups?opGroupNo=&psNo=、GET /{opGroupNo}/members、POST/PUT/DELETE 群組、POST/PUT/DELETE 群組成員；路徑與 DTO 見功能規格。
- [ ] 實作 GET /api/operation-groups/lookups/production-segments，重用既有 PS lookup；另實作/映射 GET /api/operation-groups/lookups/operations?psNo=&issueState=2，只回 IssueState=2 的已核准 OP。PSNo='*' 時不限制區段。不要建重複作業站或生產區段表。
- [ ] 主檔驗證：OPGroupNo 必填、大寫、最多 20；OPGroupName 必填、最多 50；PSNo 必填、最多 50；OPGroupOrder 0–999999 整數；Description 最多 255。編輯以 DataStamp CAS，衝突回 409；Oracle DataStamp nullable + DEFAULT 0，舊值為 NULL 時須用 null-safe CAS 條件，不能把 NULL 靜默當成 0。Creator/CreateDate/Reviser/ReviseDate 由 API principal/server 寫入。
- [ ] 成員驗證：複合 PK (OPGroupNo, OPNo)；OPRoleFlag 僅 0/1/2；WeightRate 必填且 0–100、最多兩位小數；新成員必須是此群組區段中的已核准 OP；修改只能變更角色/權重並採 DataStamp CAS；DataStamp nullable + DEFAULT 0，舊值為 NULL 時須用 null-safe CAS。audit 不信任 body。
- [ ] 主檔刪除必須在單一 DB transaction 內先刪 TBLOPGROUPDETAIL 再刪 TBLOPGROUPBASIS。成員刪除為實體刪除。不得加 Oracle 未定義的 FK/index；依 YMES 的 schema extension 習慣處理參照完整性。
- [ ] Check 是 WinForms 清單端即時檢核，不需 API 寫入：同一群組 Entry 最多 1、Exit 最多 1、WeightRate 合計 100；不要求 Entry/Exit 各至少一筆。不要讓合計規則阻止使用者逐筆編輯中間狀態的明細。
- [ ] OP Basis 刪除會以 TBLOPGROUPDETAIL 引用阻擋；確認 OP104 API 保持此保護。
- [ ] 完成 API/schema 後更新規格、migration/API handoff，提交並 push GitHub main。公司端尚未連 YMES PostgreSQL，未執行 build/test/browser 驗收；API/schema 完成後通知公司端 pull 整合。

## 最新公司端交接：OP103 作業站類別資料（2026-11-18）

- [ ] Pull 最新 main；閱讀 docs/modules/OP/作業站類別資料.md、docs/DATABASE_CONNECTION.md OP103 段落與 WinForms frmOPType/Def、wsOP、clsOpBasis 規則摘要。
- [ ] 先查 YMES PostgreSQL live schema/migrations 的 YMES.tbloptype、tblopbasis 與 Property 表。原生 MES Oracle DDL 在 database/reference/mestest_op103_optype_source_ddl.sql；空表 CREATE 對照在 database/reference/mestest_op103_optype_create.sql。CREATE 只是參考；只對實際缺表建立 forward migration，保留既有 YMES 資料/擴充欄，不重建 tblopbasis、Property 或 Issue/Job 共用表。
- [ ] API：登入保護 GET /api/operation-types?issueState=-1&opType=、POST、PUT /{opType}、DELETE /{opType}、POST /{opType}/approve。Vue OperationTypeMaster.vue 已固定以上路徑/body；如需改路由，前後端與此 spec 一起更新。
- [ ] 新增驗證 Oracle 欄位長度（OPType 20、Description 255）、Trim/大寫 OPType、PK 唯一；另檢查 TBLOPBASIS.OPNO 大小寫不敏感衝突，因舊 Core 把 OPType/OPNo 視為同一 Property namespace。
- [ ] 修改只允許 IssueState=0，僅更新 Description；DATASTAMP 在 Oracle 可 NULL、預設 0，確認現有 YMES 型別/live NULL 語義後採 CAS 並回 409；Creator/日期、Reviser/日期與狀態不信任前端，由 API 管理。
- [ ] Delete 0/-1 硬刪、2 改 -1、1 禁止。舊 clsOpBasis.DelOPType 先刪 TBLOPPROPERTY 中相同 OPNo 屬性再刪類別；確認 YMES 的共享屬性資料讀寫策略、OPType/OPBasis 碰撞與實際 FK/引用，使用單一 transaction，不要在 schema reference 臆造 FK。
- [ ] Approve 只允許 0，IssueNo=OP TYPE；共用 Issue Job/Detail 不存在才 direct Active(2)，有流程則 Pending(1)、通過 2、退回 0，第一關通知由共用簽核服務處理。不要在 API/前端自行猜狀態或重建 Issue 表。
- [ ] Property WinForms 呼叫 OP_OPProperty 並以 OPType 當 OPNo。先核對 OP BASIS 共用 Property API/表，不要為 OP103 重複建 Property table。完成 API/schema 後提交並 push；公司端 DEV mock 不會寫入正式 DB。
- [ ] OP103 本回合僅新增 Oracle source DDL、PG CREATE-only 參考、Vue DEV UI、規格及 handoff；未連 YMES PG、未實作 API/migration，未執行 build/test/browser 驗收。
## 最新公司端 handoff：QC409 OCAP異常單處置調整（2026-11-18）

- [ ] Pull 最新 main，閱讀 docs/modules/QC/OCAP異常單處置調整.md、QC408 OCAP流程規格與本檔下方 QC408 handoff、docs/DATABASE_CONNECTION.md QC409 段落。
- [ ] Schema 核對：三張專用表 DDL 在 database/reference/mestest_qc409_wip_ocap_source_ddl.sql，PG CREATE-only 參考在 database/reference/mestest_qc409_wip_ocap_create.sql。先比對 YMES live schema/migrations；只補缺的 forward migration。不要重建或覆蓋共用 WIP 表。
- [ ] 11 張 WIP/ERF/通知依賴的 MESTEST source DDL/index 在 database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql。逐張比對 YMES 現有 WIP/ENT/USR schema 和 API；表已存在時重用既有 mapping，不另建同義表。若有必要 mapping 缺表，先由來源 DDL 設計適配 migration，保留舊 schema/資料。
- [ ] 完成 /api/qc/ocap-dispositions 查詢及 route-options、POST .../{erfNo}/{holdItemNo}/adjust；授權 FunctionNo=OCAP DISPOSITION ADJUST / FunctionKey=QC409。
- [ ] 這不是只寫三張 OCAP history/state 表的 CRUD。依規格復刻 wsWIP.Exe_ERF_Disposition → clsLot.UpdOCAPAndERF_Disposition：ERF history/status、Lot Hold/LotStamp、WIP LotLog/GoodBin、OCAP current/group state、DispOrder、批次 ERF 分支、使用者定義 hook 及通知。
- [ ] Finish 必須在 transaction 中重新計算同 ERF 的剩餘 Hold Item：最後一項採 DispType=2/Final；仍有其他項目則 DispType=0 但本筆 OCAPStatus=11。避免同時操作導致錯誤結案；系統 ERF 必須驗證新鮮 LotStamp，衝突回 409。
- [ ] Change Group 只接受 server 從 OCAP graph/version/relation 驗證的可達節點；不可接受任意 GroupNo/NodeId/LinkName。附件和 mail 使用 YMES 現有 extension/outbox；確認不重複呼叫舊 side effect。
- [ ] 完成後更新 migration/API 規格與 TODO，push GitHub main。公司端 pull 後以 Vue DEV UI 整合驗收；公司端尚未做 build、測試或瀏覽器驗收。

## 最新公司端 handoff：QC408 OCAP基本資料（2026-11-18）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/QC/OCAP基本資料.md`、`docs/DATABASE_CONNECTION.md` QC408 段落，以及 Oracle source DDL / PostgreSQL CREATE 對照。
- [ ] 比對 YMES live schema 與 migrations：`tblqcocapbasis`、`tblqcocapnodebasis`、`tblqcocapnoderelation`。只為缺表補 forward migration，維持來源 PK、NULL/default 和 `OCAPLINKNAME` PK 隱含 NOT NULL；不新增 Oracle 未定義的 FK/index、不重建既有資料。
- [ ] 原始 Oracle DDL 在 `database/reference/mestest_qc408_ocap_source_ddl.sql`；PG 空表 CREATE 參考在 `database/reference/mestest_qc408_ocap_create.sql`。公司端已用 PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取 DDL；不要在 Oracle 執行 CREATE。
- [ ] 先確認 `OCAPXMLSTRING` 相容策略：WinForms 呼叫二進位 `iMESXMLFlow.dll` 的 `FlowToXML/XMLToFlow`，目前舊源碼庫沒有 serializer source。決定反編譯/相容轉換，或明確採 YMES 自有格式；不可把 Vue nodes/links JSON 直接寫成舊 AddFlow XML 並宣稱可相容 MES。
- [ ] 實作登入/`OCAP BASIS`/QC408 權限保護 API：GET 清單及已簽核 HoldDispGroup lookup、POST 新增、PUT 說明、POST copy、PUT flow、POST approve、DELETE。細節以功能規格的 API 表為準，SQL 參數化，audit 和時間由 server 管理。
- [ ] Flow 儲存/複製需同交易更新主 CLOB、NodeBasis、NodeRelation；保持 NodeID 和 relation 端點一致。實作 DataStamp CAS；舊 DataStamp NULL 要定義明確處理。流程狀態及刪除：未簽核 0 與停用 -1 硬刪三表；已核准 2 軟停用為 -1 並清 CurVersion；簽核中 1 禁刪；核准 0→2 並同 OCAPNo 只留一個 CurVersion=1。
- [ ] 補 `FunctionNo=OCAP BASIS`、`FunctionKey=QC408` 授權；驗證 OCAP No/Version 組合唯一、版本長度 5、No 長度 50、Description 長度 255、節點和 relation 主鍵、GroupNo 已簽核、DataStamp 衝突回 409。
- [ ] 完成 API/schema 後推到 GitHub，通知公司端 pull。不要將 DEV localStorage 假資料灌入資料庫。

## 已完成家用端：QC503 檢測設備連線程式（2026-11-18）

- [x] 核對已由 migration 0027 建立的 Driver 表，未重建資料表。登入保護 API 已部署；共用無狀態查找保留全狀態，functions 僅回來源三個允許名稱。
- [x] CRUD/核准採精確 nullable DataStamp CAS、明確版本必要欄位、server audit、狀態 0 編輯、2→-1 停用、0/-1 硬刪與引用保護；共用流程限制見模組文件。
- [x] 正式 app-browser 已驗證新增/套用/修改/複製、查找相容、核准取消/確定、已核准唯讀、停用再硬刪、引用刪除 409、NULL 版本寫入/舊版本衝突、四狀態查詢與 Close。測試 Driver/類別均清空。
- [x] 邏輯/API/部署與驗證詳見 `docs/modules/QC/檢測設備連線程式.md`。Legacy 三個函式目前無有效解析實作，此功能管理設定名稱，不宣稱可驅動實體儀器。

## 家用端進度：QC501/QC502 檢測儀器（2026-11-18）

- [x] QC501–QC503 六張共用表 live schema audit 確認缺表；先備份並驗證 `/opt/ymes/backups/ymes_before_0027_qc501_20261118.dump`，再套用 migration `0027_qc501_qc502_inspection_machines.sql`，授予 `ymes_app` 表級 CRUD。
- [x] 實作並部署 QC501/QC502 登入保護 API、參數化 SQL、DataStamp/IssueState CAS、audit、欄位檢查、類別/Driver lookup、刪除引用檢查；Release build 0 warning/0 error，API readiness=200，未登入 API=401。
- [x] Production app-browser 已驗證 QC501 新增/套用/修改、複製 UserDefine/Driver lookup、核准取消不改資料後確定轉 2，以及被儀器引用的類別刪除回 409 且保留資料。
- [x] QC502 新增/套用/修改、類別 A RS232→類別 B UserDefine 更換、無類別複製/Driver lookup、核准轉 2 與刪除儀器 B 均經 app-browser 驗證；舊 DataStamp 在 DB 改成 NULL 後保存回 409，重查 NULL 後保存成功。
- [x] 精確 nullable DataStamp CAS、交易內鎖定權威類別/Driver、Apply 使用 server DTO 與新增轉修改模式已實作。.NET 9 Release build 0 warning/0 error；頁內確認框已取代 `window.confirm`。最終部署後 app-browser 確認 ProblemDetails 顯示清楚中文 detail，不顯示 JSON。
- [x] 本輪與先前測試儀器/類別均已清理；暫存 Driver 以交易清理，遠端精確 fixture 計數為 0。
- [x] 收尾驗證通過：QC501 狀態 1 編輯欄位/保存停用、-1 與 2 類別刪除成功；QC502 狀態 0 刪除先取消保留再確定刪除，狀態 2 刪除成功。兩頁四狀態篩選、Close 回空白首頁及登出/Demo 登入均通過；API/Web healthy、readiness=200。
- [x] 本次家用端變更已隨 `340db5f` 同步 GitHub main，保留公司端 `7125143` 與 QC503 UI 提交 `b572f3b`；已讀新版待辦，下一步為 QC503 家用端 API。

## 最新公司端交接：QC501 檢測儀器類別（2026-11-18；家用端已接手）

- [x] 家用端已完成 live schema audit、備份、0027 migration 與 QC501 API；細節見 `docs/modules/QC/檢測儀器類別.md`。主鍵 InspMachineType 單欄，IssueState/DataStamp nullable，ChNo 長度 3。
- [x] app-browser 已驗證新增/套用/修改、複製/Driver lookup、核准取消/確定、引用刪除防護、狀態 1 唯讀、-1/2 成功刪除、四狀態/Close 與最終錯誤文字。共用 Issue/Job 表不存在時核准轉 ACTIVE，若表出現則 API fail closed；Issue Control 尚未接入。
- [ ] 公司端 pull 家用端同步後，確認 Vue DTO/API contract 與正式 route；不要把 DEV mock 資料送至正式 DB。
- [ ] 公司端已提交 QC503 UI（`b572f3b`）；家用端 merge 後讀規格並接手 API，沿用 0027 六表與 CREATE/source DDL。

## 前次公司端交接：QC502 檢測儀器基本資料（2026-11-18）

- [x] 家用端已建立 QC502 GET/POST/PUT/DELETE/approve API，沿用 0027 六表 migration 與 QC501 class/Driver lookup；規則見 `docs/modules/QC/檢測儀器基本資料.md`。
- [x] app-browser 已驗證新增/套用/修改、跨通訊型別類別帶入、複製/Driver lookup、核准、取消/確定刪除、nullable DataStamp 409 與重查後保存、四狀態/Close、最終錯誤文字及 Demo 登入；本輪 fixture 已清空。
- [ ] 公司端 pull 家用端同步並確認正式 API/DTO；QC503 UI 已提交，下一項由家用端接手 API，沿用 QC501 已查 DDL。

## 最新公司端 handoff：QC305 產品規格設定（2026-11-17）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/QC/產品規格設定.md`、`docs/DATABASE_CONNECTION.md`。FunctionNo=`PRDQCITEM`、FunctionKey=`QC305`；Vue DEV 頁 `/YMES/QC/ProductSpec` 位於品質資料 → 計量規格設定 → 產品規格設定。
- [ ] 核對 YMES PostgreSQL live schema/migrations 與 QC302 API。Chart Basis/Detail/DetailLog 使用 migration 0024；產品 Basis Oracle DDL 參照 `mestest_qc218_tblprdproductbasis_source_ddl.sql`，PRDQCITEM/Variable/Factor 參照 QC205，Group 參照 QC212。QC305 沒有專屬資料表，不要新增重複 CREATE；`TBLPRDTYPE` 先查 live schema，只有確認 YMES lookup 依賴缺漏才請公司端取得 Oracle DDL。
- [ ] 建立登入保護 QC305 API：`GET /api/qc/product-spec/product-types`、`GET /api/qc/product-spec/products?productType&productNoPrefix`、`GET /{productNo}/qc-items`、`GET /{productNo}/chart-details`、`POST /api/qc/product-spec/batch-update`。依既有授權方式檢查 PRDQCITEM/QC305；若已存在等效 API，沿用並更新 Vue/spec。
- [ ] 查詢忠實採舊 `LoadProductBasisJoinPRDQC`：產品 `IssueState=2`，ProductType 精確、ProductNo 前綴，舊 `QC_Count=1`；產品 QC Item/Chart API 需讀 `TBLPRDQCITEM`、`TBLQCITEMFORVARIABLES`、`TBLQCITEMFACTOR` 及 Group 主/明細。只有恰好一個 FactorSpec 且 FactorNo=`PRODUCT` 的變數 QC Item 建 Detail；保留 GroupNo。ChartType/SPCID、X-Rm SPCID 0/5 映射、規格必填/排序和 MEMO 規則見功能規格。
- [ ] `batch-update` 接受 `{productNos,details:[{qcItemNo,groupNo,spcId,us,cs,ls,ucl,cl,lcl}],memo}`，逐產品/QCItem transaction 重查 ACTIVE 與直接/Group 綁定、FactorSpec 和 SampleSize。Basis 依 `(QCItemNo,Product)` 定位；新建才產生 serial，SampleSize 取 QC Item、Product 因子寫 `factorvalues JSONB`（並按 QC302 核對 physical `product` 欄相容性）、IssueState=0；既有 Basis 沿用 serial/state。交易式替換 Detail、追加 DetailLog 界限快照；MEMO 進 audit/log。回傳逐項 updated/skipped/failed 結果，失敗需 rollback。
- [ ] 舊 Core 的產品綁定和 chart 查詢只用 ProductNo，沒有 ProductVersion/OPNo 條件；API 應在規格中保留此相容行為及版本/多 OP 歧義，不要自行切換 key。對同一產品/QCItem 去重，避免不同 GroupNo 對同一 Basis 重複覆寫。
- [ ] 公司端本次未執行 build、測試或瀏覽器驗收；QC305 沒有新增表，沒有額外 CREATE SQL。本 handoff 之外，QC303、QC304 API/schema 工作仍依各自段落追蹤。

## 已完成家用端 handoff：QC303 品管項目規格（2026-11-18）

- [x] YMES live schema 原先缺 Basis/Detail；新增並套用 `0026_qc303_spec_basis.sql`。套用前備份 `/opt/ymes/backups/ymes_before_0026_qc303_20261118.dump` 已通過 `pg_restore --list`；只建 QC303 兩表及 `ymes_app` CRUD 權限，不重建共用 QC Item/Factor/Parameter/Issue/Job/QCSN。
- [x] 完成登入保護 API（QC Item/Factor、List/Detail/Options、CRUD、Approve/Disapprove、Copy2）、FactorSpec 白名單、JSONB/參數化 SQL、交易、重複檢查、DataStamp 與服務端 audit。依使用者指示暫不做角色／FunctionKey ACL。
- [x] .NET 9 Release build 0 warnings/0 errors；正式 API/Web 已部署。正式 menu 路由更新後只重建 `ymes-web`；容器 healthy，API readiness=200、未登入 QC303 API=401、Web route=200。App-browser 驗證三層選單、正式頁、四個狀態篩選與 Close。
- [ ] Live 計量 QC Item 清單為空，新增/修改/刪除/Copy/簽核/Detail 等依賴業務列的流程尚無對象可做端到端驗收。`tblsysmesparameter`、Issue/Job、共用 QCSN 序號服務亦不存在；現階段 serial 使用服務端 35 字元 GUID fallback。待共用資料、參數與簽核資源可用後補驗；詳見 `docs/modules/QC/品管項目規格.md`。

## 下一個公司端 handoff：QC304 物料規格設定（2026-11-17）

- [ ] 讀 `docs/modules/QC/物料規格設定.md`。FunctionNo=`MTLQCITEM`、FunctionKey=`QC304`；Vue DEV 頁 `/YMES/QC/MaterialSpec` 位於品質資料 → 計量規格設定 → 物料規格設定。
- [ ] QC304 沒有專屬新表；先核對 YMES live schema/migrations。Chart Basis/Detail/DetailLog 已在 QC302 migration 0024；物料/QC Item/QC Group 使用既有 tables。Oracle/PG CREATE 參照沿用 `mestest_qc302_spc_qc_chart_source_ddl.sql`、`mestest_qc302_spc_qc_chart_create.sql`、MTL references、`mestest_qc_item_attributes_create.sql`、`mestest_qc212_item_group_create.sql`。不新增重複 schema；若 live schema 真的缺 group detail 或其他依賴，先以其既有 reference 做必要 forward migration。
- [ ] 重用現有 `GET /api/materials/lookups/options` 供 MaterialType lookup。新增/沿用登入保護 QC304 API：`GET /api/qc/material-spec/materials?materialType&materialNoPrefix`、`GET /{materialNo}/qc-items`、`GET /{materialNo}/chart-details`、`POST /api/qc/material-spec/batch-update`；要求 QC304 FunctionKey，路由有變更時同步 Vue/spec。
- [ ] 查詢限 Material IssueState=2，MaterialNo prefix；維持原 `QC_Count=1` 結果條件。物料的計量 QC Item 可由 direct binding/group 展開，但 Detail 只由 FactorSpec 恰為一個且 FactorNo=MATERIAL 的 QC Item 產生；QCItem ChartType=-1 不產 Detail。舊 Save 還會重查 direct `TBLMTLQCITEM`，未 direct 綁定的 group child 會被略過，API 必須明確回報每筆 Material/QCItem 的更新/略過結果。
- [ ] SPCID：ChartType=0→0/1/2，=1→0/1/3，=2→1/4/5；X Chart SPCID=5 在既有 Chart Basis/Detail 存為 0，讀回時轉回 5。CheckBox 僅篩 Grid 顯示，Save 仍送全部明細。US/CS/LS/UCL/CL/LCL 驗數值與 US≥UCL≥CL≥LCL≥LS；SPCID0 按 SpecLineType 驗 US/LS 必填。CS/Tolerance Edit 算 CS±Tolerance；貼上只處理明確使用者資料。
- [ ] `batch-update` 對每個 Material/QCItem transaction 中重查物料、直接綁定、FactorSpec 與現有 Chart Basis。Basis 不存在時以 Chart serial service 新增，SampleSize 取 QC Item、IssueState=0、Material 因子寫入 `factorvalues JSONB`（依現有 schema 同步 physical material 欄若需要）；存在時沿用 serial/IssueState，不重建 Basis。刪除該 serial 的舊 Detail、寫入完整新 Detail 並追加 DetailLog 界限快照。MEMO 必填；舊 Core 對既有 Basis 未保存 Memo/Revision，YMES 應將原因記入操作 audit，不要自動改其 IssueState。
- [ ] 回傳 `{updatedCount,skippedCount,failedCount,results:[{materialNo,qcItemNo,status,message}]}` 給 Vue；逐項顯示直接綁定缺失的略過原因。真正資料庫失敗須在 transaction rollback，不能留下部分 Detail/DetailLog 並回報整批成功。
- [ ] 保持跨多物料結果可見：來源一個更新 helper 接收整個 Material 清單與 Detail 集合；transaction 失敗不可假報整批成功。QC304 沒有新 Oracle DDL 或 CREATE SQL；公司端沿用先前唯讀取得的 reference，本回合未讀業務列、未執行 Oracle DDL，連線資訊未寫入 Git。

## 最新公司端 handoff：QC301 管制圖內容（2026-11-17）

- [x] `git fetch origin` 後 `main` 與 `origin/main` 同為 `d763983`；已讀 `docs/modules/QC/管制圖內容.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`QC CHART BASIS`、FunctionKey=`QC301`；Vue DEV 頁 `/YMES/QC/ChartBasis` 位於品質資料 → 計量規格設定 → 管制圖內容。
- [x] 以家用端 PPK/SSH 查 YMES PostgreSQL live schema/migrations：QC Chart Basis/Detail/DetailLog/Rule 四表已由 0024 建立；QC301 Sampling Parameter 表缺少，已建立 forward migration `0025_qc301_chart_sampling_parameter.sql` 並套用。套用前完整備份 `/opt/ymes/backups/ymes_before_0025_qc301_20261117.dump` 通過 `pg_restore --list`，`ymes_app` 具 SELECT/INSERT/UPDATE/DELETE。QC Item/Factor live table 已存在；`TBLQCITEMRULE`、`TBLQCRULEBASIS`、`TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER`、量測日誌及 Issue/Job 共用表尚未建立，不在本 migration 重建。
- [x] 實作登入保護的 QC301 API 路由與 DTO：`GET /api/qc/chart-bases/qc-items`、清單/Detail/Rule 查詢、basis 新增修改刪除、Approve/Disapprove、Rule replacement、Copy2 及 Sampling Plan lookups。Dynamic FactorNo 使用 FactorOnline 白名單與 JSONB；共用表未建立時回 503，不略過預設規則、抽樣參數或量測紀錄檢查。API 全域要求登入；FunctionKey/角色 ACL 依使用者先前指示暫緩。Rule 編輯回傳新 DataStamp 並更新前端快取。
- [x] DTO/transaction/CAS、FactorOnline 白名單、JSONB duplicate、數值/SpecLineType/SampleSize/ChartType 驗證及伺服器稽核欄位已實作。新增需要 `TBLQCITEMRULE` 與已核准 Rule 表；PlanNo 功能需要抽樣計畫主/參數表。相依表尚未建立時寫入端點會回 503，未寫半套資料。
- [ ] Issue/Job 共用流程尚未有 YMES table/API reference。現況只有確認 Issue/Job table 均不存在時才允許直接 ACTIVE 並記 DetailLog；若部署後出現任一 Issue/Job table，Approve/Disapprove 會 503 停止，須先接上共用簽核服務。尚未建立 durable QC 操作 audit table，現階段 API 記錄至服務 log。
- [x] Delete 狀態規則、DataStamp、同交易刪 chart 子表已實作；硬刪需 `TBLSPCVARDETAILLOG` 查量測使用。該共用表目前缺少，硬刪會 503 停止，不會把「表不存在」當成「沒有使用資料」。
- [x] CopyType 3/0/2、FactorSpec/ChartType 一致性、UNFROZEN 副本及 Basis/Detail/Rule/Parameter 複製已實作。Rule replacement 限 FactorSerial/SPCID、核准 RuleNo、Action 0–3，並更新 Reviser/DataStamp。
- [x] 2026-11-17 .NET 9 Release build 0 warning/0 error；遠端 `ymes-api`、`ymes-web` Docker build 成功並已部署，兩容器 healthy，`/YMES/API/health/ready`=200、QC301 Web route=200、未登入 QC301 API=401。app-browser 已登入檢視 `/YMES/QC/ChartBasis`，頁面載入正常，UNFROZEN/PENDING/ACTIVE/UNUSED 篩選可切換；live QC Item 清單為空，新增/修改/核准等按鈕無可用資料，CRUD 操作無法端到端驗證。API/前端已提交於 `3fd0a03`，merge 公司端 QC303 提交 `e6c5a29` 後以 `eb9a73a` 推送 GitHub `main`；QC303 為下一個家用端 handoff。

## 最新公司端 handoff：QC302 匯入管制圖內容（2026-11-17）

- [x] 以家用端 PPK/SSH 核對 YMES schema；QC302 四張圖表表原先都不存在，QC Item Variables/Factor 已存在。Rule、Sampling Plan 共用資料表也尚未複製，沒有在 QC302 migration 中重建共用表。
- [x] 新增並套用 `database/migrations/0024_qc302_chart_data_import.sql`；套用前建立並以 `pg_restore --list` 驗證 `/opt/ymes/backups/ymes_before_0024_qc302_20261117.dump`。核對四張表共 68 欄及 `ymes_app` CRUD 權限；migration 只建結構，未新增業務資料。
- [x] 完成登入保護 API：`POST /api/qc/chart-data-import/preview`（`.xls/.xlsx` 第一工作表、第 1 列標頭、第 2 列跳過、第 3 列起、空白列終止）、`POST /import`（全列匯入、無 Check）及 `GET /template`。因角色/功能權限依使用者決定暫緩，API 目前要求登入，不做 FunctionKey ACL。
- [x] Preview 回 `{columns,rows}`；Import 回列級 `rowNo/qcItemNo/succeeded/execute/factorSerial`，每列獨立交易。已實作核准 QC Item/線上 Factor 白名單、JSONB、SpecLineType/TUS/TLS、數值/排序/Y 軸、RuleNo/Action 核准檢查、重複組合、basis、五種 detail、detail history、rule 複製/替換；Creator/CreateDate/FactorSerial 由伺服器管理，所有資料欄參數化。
- [ ] 可匯入 QC302 的基本規則仍受 YMES 尚缺共用 `TBLQCITEMRULE`、`TBLQCRULEBASIS` 阻擋；新管制圖必須複製預設規則，API 會對該列回 503，不會略過。若輸入 PlanNo，因 `TBLQCSAMPLINGPLAN` 尚未複製也會回 503。待相關共用 QC schema 任務另行建立後再驗證匯入。
- [ ] `GET /template` 目前以 503 明確回報尚未移轉 `FunctionNo=IMPORT QC CHART DATA` 的正式範本；未用 DEV 假檔冒充。待取得／移轉 MES 範本儲存區與 `QCChart_Template.xls/.xlsx` 後接上下載。
- [x] 本機 `dotnet build src/YMes.Api/YMes.Api.csproj --no-restore` 成功（0 warning/0 error）；遠端 Docker API image build 成功，`ymes-api` healthy、`/YMES/API/health/ready`=200；未登入呼叫 QC302 端點回 401。app-browser 的認證狀態已失效並被導回登入頁，本回合未操作登入畫面。
- [x] 已 `git fetch origin`；`origin/main` 最新為 `0b0f434`，已包含於家用端，沒有更新 commit。將只提交本 QC302 API/schema/docs 與 QC302 待辦段落，然後以一般 fast-forward push 同步 `main`，不 force push。Oracle source DDL 由公司端唯讀取得，沒有讀業務列或執行 Oracle DDL，連線 IP/帳密/SID 未輸出。

## 前一項公司端 handoff：QC215 計數品管項目匯入（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計數品管項目匯入.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`IMPORT QCITEM ATTRIBUTES`、FunctionKey=`QC215`；Vue DEV 頁 `/YMES/QC/AttributeItemImport` 已掛在品質資料 → 品管設定。
- [ ] QC215 匯入既有計數品管資料，沒有 QC215 專用表，也沒有新的 CREATE SQL。先核對 YMES PostgreSQL live schema/migrations；主檔沿用 `mestest_mtl_qc_item_tables_source_ddl.sql` / `mestest_mtl_qc_item_tables_create.sql`，Parameter/Factor/ReasonMap 等子表參考 `mestest_qc_item_attributes_source_ddl.sql` / `mestest_qc_item_attributes_create.sql`，群組與依賴表參考 QC205/QC213 既有 source/CREATE。不得複製重建共用表；只為 live schema 真正缺少的 YMES 結構新增 forward migration。
- [ ] 實作登入保護並要求 FunctionKey QC215 的 API：`POST /api/qc/attribute-item-import/preview`（multipart `.xls/.xlsx` 第一工作表；保留 WinForms 第 1 列欄名、第 2 列跳過、第 3 列起讀取）、`POST /check`（唯讀驗證）、`POST /import`（Save）及 `GET /template`。Production Vue 目前依此路徑呼叫；如配合既有 controller 改路徑，請同步改 Vue/spec。
- [ ] Preview 回 `{rows:[{recognizedColumns...}]}`；Check/Import 每列回 `{rows:[{rowNo,qcItemNo,succeeded,result,execute}]}`。只處理前端勾選列，逐列獨立交易並回列級結果；缺少任何列結果要視為失敗，不能因 HTTP 200 或空訊息標成功。
- [ ] 依 QC215 規格驗證 QCItemNo/Name、量測記錄引用、LotIsVaild、PlanNo/ComponentCollect、核准 RuleNo、正整數 LimitLotNum、MClass、FactorNo/FactorOnline、核准 OP/Inline/Offline、檢驗旗標與核准 Reason 清單；修正 legacy 重複 Reason 檢查失效問題。FactorNo 只能是核准 Parameter 白名單，絕不可由上傳值構造 SQL 欄名或觸發任意 `ALTER TABLE`。
- [ ] Save 覆寫前在該列交易中重查 WIP、SPC Attribute Log、Material/MaterialType/OP/Product、`TBLQCITEMATTGORDERDETAIL` 及一般 QC Item Group 綁定。Legacy 會刪除既有主/子資料且未先保護一般 Group Detail；YMES 必須在群組仍有引用時拒絕覆寫，不可靜默解除綁定。完整清除/重建範圍及欄位預設值依 QC215 規格實作。
- [ ] Creator/CreateDate 必須由登入身份與伺服器時間產生，不信任檔案內容；限制上傳檔案大小/列數/欄數，不執行公式或使用者提供的路徑。範本沿用 FunctionNo=`IMPORT QCITEM ATTRIBUTES` 的既有 template storage。
- [ ] 公司端沿用已保存 Oracle DDL reference，本次沒有再查 Oracle、沒有讀業務列、不需要索取 IP/帳密/SID，也沒有新 CREATE SQL。家用端完成 schema/API/權限/template 後 commit/push；公司端再 pull 對接。公司端本段未執行 build、測試或瀏覽器驗收。

## 前一項公司端 handoff：QC214 計量品管項目匯入（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計量品管項目匯入.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`IMPORT QCITEM VARIABLES`、FunctionKey=`QC214`；Vue DEV 頁 `/YMES/QC/VariableItemImport` 已掛在品質資料 → 品管設定。
- [ ] QC214 是匯入既有計量品管資料的功能，沒有專屬新資料表、沒有 QC214 專用 CREATE SQL。先查 YMES PostgreSQL live schema/migrations；主表沿用 `database/reference/mestest_mtl_qc_item_tables_source_ddl.sql` / `mestest_mtl_qc_item_tables_create.sql`，依賴表參考 `mestest_qc205_variable_dependencies_source_ddl.sql`、`mestest_qc210_variable_measurement_log_delete_create.sql`、`mestest_qc_item_attributes_create.sql` 及 QC205/QC201 的共用 CREATE SQL。只為實際缺少結構新增 forward migration，不複製重建 QC205 共用表。
- [ ] 實作登入保護並要求 FunctionKey QC214 的 API：`POST /api/qc/variable-item-import/preview`（multipart `.xls/.xlsx` 第一工作表；相容 WinForms 第 1 列欄名、第 2 列範例跳過、第 3 列起資料）、`POST /check`（OnlyCheck、不寫入）、`POST /import`（Save）及 `GET /template`。Production Vue 現在依此契約呼叫；如採既有 controller 路徑，需同步修改 Vue/spec。
- [ ] `/preview` 回 `{rows:[{recognizedColumns...}]}`；Check/Import 每列回 `{rows:[{rowNo,qcItemNo,succeeded,result,execute}]}`。前端只送所勾選且未鎖定資料列；結果需能依 `rowNo` 對回。以每列獨立交易、逐列回報，允許部分成功；不要只看 legacy Service 的整體 success。
- [ ] 依 QC214 規格實作驗證：QCItemNo/Name、LotIsVaild、PlanNo、SampleSize、SpecLineType、ComponentCollect、CalType、ChartType/X-Rm 關係、Min/Max、LimitLotNum、MClass、Roundoff_Num、已核准 Class 1 Factor 集合及 FactorOnline/FactorSpec 子集合、核准 OP/Inline/Offline 與 FAI/IPQC/PQC。舊 Core 的 ComponentCollect/LotIsVaild 限制已註解；不要自行阻擋。
- [ ] Save 覆寫既有 QCItem 前交易內重查：無 Spec/Chart/量測資料、無 WIP 使用，無 Material/MaterialType/OP/Product/一般 Group/PMI Group 綁定。舊 Core 會清除主檔與多張子表再重建；列出的清除範圍要用 YMES 正規化關聯表和參數化 SQL 重現。Factor 欄不准由客戶端輸入觸發 `ALTER TABLE`；只有白名單核准 Factor 可套用，動態量測資料沿用 YMES 結構化儲存設計。確認 transaction 可回滾所有主/子資料寫入。
- [ ] Creator/CreateDate 雖是舊匯入欄位，API 必須忽略檔案提供的稽核資料，改用登入身份與伺服器時間。限制工作簿大小/列數/欄數，不執行公式、不採信用戶端檔案路徑；範本沿用 FunctionNo=`IMPORT QCITEM VARIABLES` 的既有 template storage。
- [ ] Oracle DDL references 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得，本次沿用 QC205/QC210/QC201 結構參照，不需要再問公司端索取 IP/帳密/SID，也沒有新增 Oracle 查詢或業務列讀取。後續 QC302 直接查得 MESTEST chart 主表為 `TBLSPCQCCHARTBASIS`；比對 YMES live schema 時依正式 PostgreSQL/API 物件名稱處理，不可因 Oracle 有表就推定 YMES 已建立。
- [ ] 家用端完成 schema 比對、API、權限與 template 後 commit/push；公司端 pull 對接。公司端本段未執行 build、測試或瀏覽器驗收。

## 前一項公司端 handoff：QC218 計數品管項目產品原因對應設定（2026-11-17）

- [ ] Pull 最新 `main`，先讀 `docs/modules/QC/計數品管項目產品原因對應設定.md`、`docs/DATABASE_CONNECTION.md` 與本節。FunctionNo=`QCITEM PRD REASON MAP`、FunctionKey=`QC218`；Vue DEV 頁 `/YMES/QC/ItemProductReasonMap` 已完成並掛到品質資料 → 品管設定。
- [ ] 用 YMES PostgreSQL 管理連線查 live schema / migrations。Oracle 原始 DDL：`database/reference/mestest_qc218_tblqcitemprdreasonmap_source_ddl.sql`、`mestest_qc218_tblqcitemreasonmap_source_ddl.sql`；兩張 mapping 表的空結構參考：`database/reference/mestest_qc218_item_product_reason_map_create.sql`。只比對/建立缺少的 mapping 結構；`TBLQCITEMFORATTRIBUTES`、`TBLQCREASONBASIS`、`TBLPRDPRODUCTBASIS` 是共用表，另外三份 Oracle DDL 只供欄位核對，禁止重建或覆寫。
- [ ] 建立登入保護並要求 QC218 FunctionKey 的 API：`GET /api/qc/item-product-reasons/qc-items?issueState=2&qcItemNo=`、`GET /api/qc/item-product-reasons?qcItemNo=&allData=true`、`GET /{qcItemNo}/products`（distinct 已對應 ProductNo）、`GET /reasons?issueState=2`、`GET /products?issueState=2&currentVersion=1&search=`、`GET /{qcItemNo}/{productNo}/reasons`、`PUT /{qcItemNo}/{productNo}`、`POST /import/preview`、`POST /import`、`GET /template`；細節依 QC218 規格，路徑若需配合既有 API 慣例請同步更新文件和 Vue。
- [ ] Main QC Item 只讀已核准計數項目，QCItemNo `%keyword%` 模糊查詢；全量映射需 Join `TBLQCREASONBASIS` 回 ReasonName/Type/Level，再按 ProductNo 分組。ProductNo 查詢空白=全部、`*`=只看共用、其他值=模糊篩選；產品 `...` 查該 QC Item 已有映射的 distinct ProductNo。
- [ ] ReasonType 0=Scrap、1=Defect；候選原因只取 IssueState=2，並依 ReasonType/ReasonLevel 篩選。Product 必須 IssueState=2 且 CurVersion=1，只有字面值 `*` 免查產品並代表全域映射。來源 WinForms 編輯器似乎把 `*` 當產品代碼驗證而可能無法儲存；API/UI 必須修正此 defect，將 `*` 寫 `TBLQCITEMREASONMAP`，其他產品寫 `TBLQCITEMPRDREASONMAP`。
- [ ] `PUT` 以 `{ reasonNos: string[] }` 原子整組取代目標 QCItemNo/ProductNo；空陣列清空。先重驗核准 QC Item/Reason/產品目前版本及授權；不得跨產品寫入。對原有關聯保留 Creator/CreateDate，新增關聯以登入身份/伺服器時間建立，Reviser/ReviseDate 由 API 產生。全程參數化並正確處理 unique key race。
- [ ] Import `.xlsx/.xls` 只讀第一工作表，欄位順序 QCItemNo、ProductNo、ReasonNo，需驗證每列必填；必須包含最後一列（舊程式有 off-by-one bug，勿照抄）。Preview 後逐列獨立驗證/交易/回報結果，允許部分成功與取消。拒絕重複三欄鍵、已存在映射、未核准 QC Item/Reason、非核准目前版本產品；`*` 不查產品。不要接受上傳檔或前端的 Creator。
- [ ] 範本沿用 FunctionNo=`QCITEM PRD REASON MAP` 的既有 template storage。新增 migration/API/權限/範本後 commit/push；公司端 pull 再接 Production API。公司端尚未執行 build、測試或瀏覽器驗收。

## 最新公司端 handoff：QC217 設備品檢管理設定（2026-11-17）

- [x] Pull 並 merge 公司端最新 `main` 至 `0b0f434`，讀完 `docs/modules/QC/設備品檢管理設定.md`、`docs/DATABASE_CONNECTION.md` 與本節；FunctionNo=`QC INSPECTION`、FunctionKey=`QC217`。公司端 Vue 頁已掛 `/YMES/QC/Inspection`，EQP `/YMES/Equipment/Inspection` 維持獨立。
- [x] 以家用端 PPK/SSH 查 YMES live schema：兩張 QC217 表原先都不存在；設備、作業站、設備狀態、計數/計量項目與計量群組主檔已存在。原始 DDL 與 CREATE 參考已比對，沒有覆蓋既有表。
- [x] 新增及套用 `database/migrations/0023_qc217_equipment_inspection.sql`。套用前的 `/opt/ymes/backups/ymes_before_0023_qc217_20261117.dump` 已通過 `pg_restore --list`；套用後核對兩張表欄位、複合 PK 與 `ymes_app` CRUD grants。
- [x] 建立登入保護 API：`GET /api/qc/inspections`、`GET /options`、`GET /{opNo}/{equipmentNo}/{inspType}/details`、POST、PUT、DELETE 及 `/approval`。主檔/共用明細原子交易、查找重驗、參數化 SQL、DataStamp CAS 及 audit 已實作；API 使用目前產品決策的登入保護，FunctionKey ACL 尚未建立。
- [x] API 已部署至 Production `/YMES/API`，容器 healthy，`/health/ready` 回 200；Web 已重新建置並部署至 `/YMES`。app-browser 經正式三層選單可開啟 QC217，Options 載入、空清單查詢和新增表單開關正常。現場 options 缺 MES ValueList、通知群組及 QCItemType=3 群組目錄，API 以 `missingReferences` 回報、不使用假資料；目前資料庫也沒有 QC217 主檔/可用 QC Item 資料。
- [x] Options 回傳已核准 OP、設備與設備類型、設備狀態及目前三種已存在 QCItemType 的可選項目/ LotIsValid；`missingReferences` 列出尚未複製的 ValueList、通知群組與 QCItemType=3 主檔。OP `*` 表示全部作業站。Server 重新驗證已存在的設備/OP/狀態及 QC Item，禁止未核准或 LotIsValid=0 的項目；所有 SQL 參數化。
- [x] Add/Edit/Delete/Approval API 已實作並部署：主檔/明細原子交易、狀態限制、共用明細鍵、DataStamp CAS、登入身份與伺服器稽核；API 以正確完整複合鍵更新，不沿用舊 callback 的 InspWeek 條件錯誤。未在正式資料庫新增測試業務資料；寫入路徑尚未透過瀏覽器實際送出。
- [x] Delete 規則依來源實作：狀態 0/-1 硬刪、2 改 -1、1 拒絕；硬刪依 legacy 共用鍵清除明細，UI 操作需留意同 OP/設備/檢查類型其他排程共用的細項。
- [x] 核准 API 已依 live YMES 結構處理：沒有 Issue Job/Detail 表時沿用 legacy 直接 0→2；若簽核結構存在但共用簽核尚未接入，回 503 且不改狀態。
- [x] QC217 API/migration、路由與必要 Vue build 修正已於 `2730c7f` commit，與公司端 `43079d6` merge 成 `0b0f434` 並 push `main`；公司端可 pull 後以 Production API 對接。公司端 UI 實際 CRUD 瀏覽器驗收待資料來源補齊。

## 最新公司端 handoff：QC216 計量品管項目群組_減項（2026-11-17）

- [ ] 先讀 `docs/modules/QC/計量品管項目群組_減項.md`、`docs/DATABASE_CONNECTION.md` 與本節；FunctionNo=`QCITEM_G_PMI`、FunctionKey=`QC216`。公司端 Vue 頁已掛在 `/YMES/QC/PmiItemGroup`，Production API 尚未建立。
- [ ] 用家用端 PostgreSQL 管理連線比對 YMES live schema、migration 與現有 QC API。 Oracle source DDL 在 `database/reference/mestest_qc216_*_source_ddl.sql`，空結構 CREATE 參考在 `database/reference/mestest_qc216_variable_item_group_pmi_create.sql`。只對實際缺漏新增 forward migration；三張 SPC map、TBLWIPTEMP_QCITEM 與共享 QC/OP/MTL/PRD 表先確認已存在及欄位/索引，不可重建或覆寫。
- [ ] 新增 `QC216` 登入授權 API：`GET/POST /api/qc/pmi-item-groups`、`GET/PUT/DELETE /{groupNo}`、`POST /{groupNo}/approval`、`GET/PUT /{groupNo}/details`、`GET/POST /{groupNo}/deductions`、`PUT/DELETE /{groupNo}/deductions/{serial}`、`GET /options/factors`。DTO 和資料限制依 QC216 規格；Audit 由伺服器登入身份產生。
- [ ] 編輯需重現共用 `ExeIssueControl`：IssueState=0 欄位可編輯；1 唯讀；2/-1 依 `wsSYS.LoadIssueControlForm` 針對 `frmPMI_QCItemGroupDef` 的設定逐欄授權，缺少設定時全唯讀。Vue 讀 `GET /{groupNo}/edit-policy`，PUT 端仍在伺服器重驗欄位。
- [ ] 群組新增後 IssueState=0。Add/Edit 檢查 GroupNo 與 Attribute/Variable QC Item、一般 QC 群組及 PMI 群組互斥（來源大小寫無關）；Define UI 的 ComponentCollect、FormName、WebURL、Error/NoticeReportName、QCClass、CursorMode 等隱藏欄位在修改時必須保留。PlanNo 只可選同 ComponentCollect 且 IssueState=2 的計畫。
- [ ] 明細完整替換必須交易處理：只接受已核准計量 QC Item、ComponentCollect 相同、QCItemNo 不重複；保存 ShowOrder、DeductionItem，至少保留一筆非減項。核准前每個目標群組均須至少有一筆 detail。簽核 Issue Job=`QCITEM_G_PMI`；無 Job 直接到 2，有 Job 先到 1，核准 2、退回 0。
- [ ] 刪除依舊系統規則：IssueState=0/-1 硬刪、2 改 -1、1 拒絕；先查 TBLWIPTEMP_QCITEM 在線引用，再同 transaction 清 PMI 主/明細及 Core 實際清理的共用 QC Item Parameter、OP/MTL/PRD/SPC 關聯；只有任何一般/PMI 群組都不再引用時才清孤兒 Calculate Script。WinForms Core 沒有刪 `TBLQCGROUPDEDUCTIONBASIS_PMI`；為維持相容，API 預設保留這些 orphan rows，規格記為 legacy 缺陷。核對各關聯表實際 QCType 與鍵後實作，逐筆回報批次結果。
- [ ] `TBLQCGROUPDEDUCTIONBASIS_PMI` 的 Oracle 動態要因欄位在 PostgreSQL 用 `factorvalues JSONB`；只允許 API 依啟用的 FactorOnline 白名單收鍵，不可把用戶輸入串成 SQL 欄位。保留 `*` 萬用條件，factor value 依 Oracle VARCHAR2(61) 限制；NormalTimes/DeductionTimes 1–99、CurPass 0–999、CurStatus 0/1/2、重複因子組合檢查與 DataStamp CAS 均由 API 執行。序號沿用 YMES serial 服務。
- [ ] 共用子功能 Parameter 使用 QCItemType=4，PlanNo 有值時傳遞已核准 PlanType；Op 使用 QCType=4，GET 已核准作業站候選清單及該群組目前的關聯/FAI-IPQC-PQC 設定，PUT 以 transaction 完整替換。只允許已核准群組修改；LotIsValid=0 禁止 InLine；每筆 InLine 至少要有一種 FAI/IPQC/PQC。共享 API 仍需要求 QC216 授權。
- [ ] migration、API、選項及 API 文件完成後 commit/push；公司端再 pull 並對接。公司端沒有連 YMES PostgreSQL，也沒有執行 build、test 或瀏覽器驗收。

## 最新公司端 handoff：QC213 計數品管項目群組（2026-11-17）

- [ ] QC212 是本功能前置工作；先完成下方 QC212 handoff 的 live schema 比對與 API，再接 QC213，避免 QC 主檔 API 路徑重複或漏掉共用資源。
- [ ] 先讀 docs/modules/QC/計數品管項目群組.md、docs/DATABASE_CONNECTION.md、docs/MES_MENU_FUNCTION_INVENTORY.md；FunctionNo=QCITEM_G_A_ORDER，FunctionKey=QC213。
- [ ] Oracle source DDL 已由公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得，無須重查：QC213 主/明細及 OP/PRD 硬刪依賴表各有 reference；WIP 檢查表在 QC205 dependency reference。PostgreSQL CREATE-only 參考 `database/reference/mestest_qc213_attribute_item_group_create.sql` 列出這些表及必要索引。
- [ ] 查 YMES PostgreSQL live schema、migrations、API 現況。僅在缺表時新增 forward migration；主檔與明細 PK 分別是 tblqcitemattgrouporderbasis(groupno)、tblqcitemattgorderdetail(groupno,qcitemno)。逐一比較 CREATE 參考中的硬刪依賴表及既有 QC 共用表欄位/PK/index，不可覆寫現有資料，也不可把 PostgreSQL SQL 放到 MES Oracle 執行。
- [ ] 實作登入保護及 QC213 FunctionKey 授權 API：GET/POST/PUT/DELETE /api/qc/attribute-item-groups、GET/PUT /{groupNo}/details、POST /{groupNo}/approval。詳細 DTO、欄位限制、交易及錯誤處理依 QC213 功能規格。
- [ ] 主檔欄位限制 GroupNo 25、GroupName 50、Description 255；LotIsValid 僅 0/1。Add/Edit 應避免 GroupNo 與 Attribute QC Item、Variable QC Item、計量群組 TBLQCITEMGROUPBASIS、PMI 群組 TBLQCITEMGROUPBASIS_PMI 重複（來源 Core 以大小寫無關比對）。
- [ ] 伺服器依實際 IssueState/DataStamp 決定操作：新增狀態 0；編輯僅 0；刪除 0/-1 硬刪、2 改 -1、1 拒絕。硬刪先檢查 TBLWIPTEMP_QCITEM 是否在線使用；來源 Core 另清除 QC Item Parameter、OPQCItem、OPGQCItemDetail、PRDQCItem 關聯。單一 transaction 執行，並核對 YMES 現有表的 QCType 條件與刪除順序。
- [ ] 核准使用 Issue Job ATTQCITEM_G_ORDER；有 Job 時設 1 並啟動簽核，核准後 2、退回回 0；沒有 Job 直接設 2。API 每筆都確認群組至少有一筆有效 Attribute 明細，批次逐筆回報。
- [ ] 明細 API lookup 沿用已核准 Attribute QC Item：GET /api/qc/attribute-qc-items?issueState=2。Add/Remove/reorder 寫入以單一 transaction 完整替換 details，檢查群組未簽核、QC item IssueState=2、Attribute 類型、唯一性；ShowOrder 連續 0..N-1。沿用共用作業站資源 QCType=3，LotIsValid=0 不可指定 Inline，InLine 至少勾選 FAI/IPQC/PQC 一種；不要建 QC213 專屬 operation table。
- [ ] 更新 migration/API 文件與 todolist_homepc.md，commit/push 後通知公司端 pull。公司端尚未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收。

## QC212 前置 handoff：計量品管項目群組（2026-11-17）

- [ ] 讀取 `docs/modules/QC/計量品管項目群組.md`、`docs/DATABASE_CONNECTION.md` 與下方 QC212 handoff；選單 FunctionNo=`QCITEM_G`、API 權限 FunctionKey=`QC212`。
- [ ] 使用家用端可用的 PostgreSQL 管理連線先查 live schema、migration 及 API 現況。核對 `tblqcitemgroupbasis`、`tblqcitemgroupdetail`、`tblqccalculatescript`、`xx_tblqcitem_memo`、`tblqcitemparameter`、`tblopqcitem`、QC Variable item、sampling plan、MES parameter。Oracle source DDL 已在 reference，不需要公司端重查 Oracle。
- [ ] 若 live YMES 缺少群組明細或腳本表，參考 `database/reference/mestest_qc212_item_group_create.sql` 設計 forward migration；群組主檔既有參考為 `mestest_mtl_qc_item_tables_create.sql`，Memo 為 `mestest_qc205_theil_custom_create.sql`。先比對既有 schema，不可覆蓋/重建既有資料，不可把 PostgreSQL SQL 放到 Oracle 執行。
- [ ] 實作登入保護且使用 QC212 FunctionKey 的主檔、Detail、Copy、Approve、Delete API。規格詳 `docs/modules/QC/計量品管項目群組.md`；包含完整替換明細/ShowOrder、ScriptIsValid、計算腳本、共用 Parameter/OP、Memo Type=1、抽樣計畫/WinSPC/QCClass 查找。
- [ ] 狀態規則：刪除 0/-1 硬刪、2 轉 -1、1 禁刪；硬刪檢查 WIP 引用及 legacy 關聯清理。核准使用 QCITEM_G Issue Job，無 Job 直接至 2、有 Job 至 1。Detail Add/Remove 僅狀態 0；OP 編輯拒絕狀態 0/1。API 以真實 DB 狀態與 DataStamp 驗證，使用參數化 SQL/transaction，批次作業逐筆回報。
- [ ] 明確決定群組硬刪是否一併清除 `XX_TBLQCITEM_MEMO`（Legacy Core 未刪該表），並記錄相容性決策；確認 Copy 是否只複製 detail（Legacy Core 沒有複製群組 Script 的程式）。
- [ ] 完成 migration/API 文件、更新 `todolist_homepc.md`，commit/push 後通知公司端 pull Production API 驗收。公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。

## 前一公司端 handoff：QC211 二次檢驗抽樣計畫（2026-11-17）

- [ ] 讀取 docs/modules/QC/二次檢驗抽樣計畫.md、docs/DATABASE_CONNECTION.md 與 QC211 公司端紀錄；FunctionNo 為 SAMPLINGFOR2NDINSP，FunctionKey 為 QC211。
- [ ] 比對 YMES live PostgreSQL schema 與 migrations。主表來源為 TBLQCSAMPLINGFOR2NDINSP；Oracle source DDL 與 PostgreSQL schema-only Create 參考分別在 database/reference/mestest_qc211_second_inspection_sampling_source_ddl.sql、mestest_qc211_second_inspection_sampling_create.sql。Oracle 沒有 PK constraint，只有唯一索引 U_TBLQCSAMPLINGFOR2NDINSP_01，鍵序為 QCITEMNO、QCITEMTYPE、OPNO、OLDSAMPLEPLAN；沒有 FK。只為確認缺少的結構新增 forward migration，不覆寫 YMES 資料。
- [ ] 實作登入保護且使用 QC211 FunctionKey 的 list、Add、Edit、Delete API。Update/Delete 的資源鍵為 (qcItemType, qcItemNo, opNo, oldSamplePlan)；新增時檢查唯一鍵及 OP/QC Item/抽樣計畫關聯。Update 只允許變更 NewSamplePlan，使用交易與 DataStamp concurrency，並正確更新 Reviser/ReviseDate。
- [ ] 提供作業站、IssueState=2 的計量型 QC Item（依 OP 過濾並回傳 ComponentCollect）、一般抽樣計畫、Wafer 抽樣計畫 lookup；沿用 YMES 既有 OP/QC/Plan 資源，不建立重複 lookup table。
- [ ] 核對舊來源不一致：wsQC Edit 呼叫帶 DataStamp/Reviser/ReviseDate，但 clsQCBasis Edit 簽章只接受五個業務欄位且只更新 NewSamplePlan。API 需採明確的 YMES 稽核/並行更新規則並回填規格。
- [ ] 完成 schema/API 文件與 migration 後 commit/push；公司端再 pull 並對接驗收。公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。

> 目的：家裡電腦負責 YMES 的資料庫、migration、API 與遠端部署；公司電腦負責 Vue UI。兩邊共用 GitHub `souaix/YMES`，開工前先 pull，完成後提交並 push。

> **本次接手身分：家用電腦。** MES 原生來源是公司端唯讀查詢的 Oracle MESTEST；本端要復刻的是 YMES PostgreSQL，透過桌面 PPK/SSH 核對、建表、migration、開發 API 與部署。`database/reference/*_create.sql` 是由原生 MES DDL 轉換的 PostgreSQL 建表結構，只能用於 YMES 復刻資料庫，不在 Oracle 執行，也不含 MES 業務資料。公司電腦只做 UI 並呼叫 `/YMES/API`，不能連 YMES PostgreSQL、SSH 或上版。不要因 API 應用帳號權限不足而把工作角色改成交由公司電腦；由家用端 SSH 使用資料庫管理權限完成。

## 家用電腦現在接手

### QC201 計數品管項目（公司端 2026-11-17 交接）

- [x] 閱讀 `docs/modules/QC/計數品管項目.md`、公司端 QC201 區段及 `docs/DATABASE_CONNECTION.md`；確認 Vue route/menu 使用 FunctionNo `QCITEM_A`、API 權限使用 FunctionKey `QC201`。本專案目前沒有 user-function ACL 對照，依使用者「角色/權限先不管」決策，端點沿用登入保護。
- [x] 比對 YMES live PostgreSQL schema 與 migrations：主表 `tblqcitemforattributes` 已存在且有 YMES 擴充欄位；四張直接子表 `tblqcitemparameter`、`tblqcitemfactor`、`tblqcitemreasonmap`、`tblqcsampleplanchange` 原本缺少。2026-11-17 建立並套用 `0021_qc201_attribute_qc_item.sql`，套用前備份已驗證；只建四張子表並補本 API 所需 CRUD 權限，未 drop/recreate 或複製 Oracle 業務資料。
- [x] 已逐項閱讀 `mestest_qc_item_attributes_source_ddl.sql`；其餘刪除依賴表不批次建立，對既有 YMES 關聯只用固定表名做引用檢查，直接子表清理放在交易內。
- [ ] 實作 FunctionKey QC201 保護的 API，契約詳見規格：主清單 GET、主檔 POST/PUT/DELETE、Copy POST、Approve POST；options、已核准 Reason lookup、ReasonType ValueList、Parameter/Factor/Reason/Operations/SamplePlanChange GET/PUT、Operation choices GET 及 Simulation POST。DTO 欄位大小寫/日期/nullable 請和 Vue `QcAttributeQcItemMaster.vue` 對齊。
- [ ] API 必須重做 WinForms/Core 驗證：Add/Edit/Copy 的欄位限制、Issue Control、DataStamp concurrency、複製關聯交易、Approve 僅未簽核且至少有 Factor/Reason、Delete 的 0/-1 實刪/2→-1/1 拒絕及 WIP/MTL/MTL Type/OP/PRD/QC Group/SPC 引用檢查。刪除與子表清理必須交易化，按多選逐筆處理時保留前端首錯停止的結果語義。
- [ ] Options 從 YMES 真實抽樣計劃、計數判別法則、主分類與介面目錄取得；`showQcClass` 對 ZKT/KYT 開啟，`showLotValid`/`showComponentCollect` 依 SPK 模式設定。ReasonType 由 MES ValueList 的實際有效值及顯示字串提供，不能照搬 DEV 假資料。
- [x] `QC_ItemOp` API 已接上 Vue 路徑：GET assigned/choices、PUT full replacement；保留 OP/Inline/ExecuteOrder/檢驗時機/Reviser/ReviseDate，只允許 IssueState=2、LotIsValid=0 不可加 InLine、每筆 InLine 必須至少勾一個 FAI/IPQC/PQC。沿用 YMES 已有的 `tblopbasis`、`tblopqcitem`，不另建重複表。
- [ ] 2026-11-17 家用端本機已新增 QC201 Controller、主檔 CRUD/Copy/Approve、Parameter/Factor/Reason/Operation/SamplePlanChange GET/PUT；`dotnet build src/YMes.Api/YMes.Api.csproj -c Release --no-restore` 成功（0 warning/0 error）。尚未部署或 push。正式 options 仍缺 `tblqcsamplingplan`（QC103）、`tblqcattrulebasis`（QC107）、`tblqcmclassbasis`（QC401）資料來源；`TBLSYSVALUELIST` 在 YMES 不存在，ReasonType endpoint 先回 503；介面目錄未核實；Simulation 尚無可核實算法，先回 503，不回 DEV 假計算。
- [ ] 補齊 QC103/QC107/QC401 正式 lookup 資料/API、MES ValueList 對應來源與 Simulation 原生算法；補完後做 QC201 端到端驗證再部署。
- [ ] 完成後更新 migration/API 文件及本待辦，提交並 push；公司端 pull 後對照 QC201 Production 頁面及回應契約。

### QC112 品質原因產品指導書（公司端 2026-11-17 交接）

- [x] 2026-11-17 家用端 live audit：QC112 的 `tblsysfunction.functionkey` 與品質資料選單項目已存在；`tblprdproductbasis` 已存在，但來源 `tblqcreasonbasis` 與關聯 `tblqcreasonprdsopfile` 缺表。目錄中的五個 `FILESERVICE*`/`REASONPRDSOPFILEPATH` 參數均不存在於 YMES `tblsyselementbasis`；API 沒有 file-service code，容器只有 Data Protection volume，主機也未掛載 NFS/CIFS。使用者功能授權表未在目前 YMES schema 內盤點到。
- [x] 閱讀 QC112 規格、公司端 handoff 與資料庫連線紀錄；核對 `/YMES/QC/ReasonProductSOPFile` route、選單及 API 契約。遠端已存在 QC112 Function/Menu 項目，不需另建。
- [x] 檢查 YMES 使用者功能授權資料表；目前 schema 沒有 user-function 對照。本專案依使用者決策「角色/權限先不管」，使用既有登入保護，不新增角色模型，也不宣稱有 QC112 專屬 ACL。
- [x] 套用 `0020_qc112_reason_product_sop_file.sql`：僅建立缺少的 `tblqcreasonbasis`、`tblqcreasonprdsopfile`，沒有匯入 Oracle 業務資料、FK 或額外索引；`ymes_app` 僅獲原因 SELECT 與關聯表 SELECT/INSERT/DELETE。套用前已建立並驗證遠端備份 `ymes_before_0020_qc112_20261117.dump`。
- [x] 完成登入保護的 QC112 API：原因僅回 IssueState=2；產品僅回 IssueState=2/CurVersion=1，ProductNo 包含式搜尋、`*` 查全部；新增驗證原因/產品狀態與複合鍵；移除時檢查共用檔案引用並以 pending move 在 DB rollback 時補償。
- [x] 新增 YMES API 專用 Docker named volume `ymes-qc112-files`，容器掛載 `/data/qc112`；資料庫只存 `QC112/yyyy/MM/{guid}.{ext}` 邏輯路徑。上限 1 GiB、單檔 25 MiB，允許 JPG/JPEG/JPE/JFIF/PNG/BMP/PDF，並驗證內容簽章。未找到既有檔案服務或 NFS/CIFS，這是 YMES 新增的 API server-side 儲存方式。
- [x] 匯入支援第一張 XLS/XLSX/CSV 工作表、最多 1000 筆；產生四欄 XLSX 範本。`FilePath` 僅作顯示文字，API 不讀取該路徑；附件由使用者另外選取後依 FileName 上傳。
- [ ] 公司來源樹缺少舊 `QCItemRPDReasonSOPFileImporter`，因此無法確認其中未記錄的額外 business checker 條件；已實作可核實的核准狀態、產品版本、重複鍵及共用檔案刪除保護。若客戶驗收發現額外舊規則，再由公司端提供具體規則補齊。
- [x] `5a0109b` 已 push 到共用 `main` 並部署 YMES API；遠端 `/health/ready` 回 ready、未登入 QC112 endpoint 回 401，專用 volume `ymes-qc112-files` 已建立。只重建 `ymes-api`，`ymes-web` 與其他容器未重啟。
- [ ] 公司端 pull 最新 main 後驗收 Production route 與匯入/附件操作；舊 checker 額外規則仍待公司端提供具體依據。

### QC110 特殊主檔維護（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/特殊主檔維護.md`、公司端 QC110 區段與 `docs/DATABASE_CONNECTION.md`，pull 後核對 Vue 頁面、選單及 API 契約。
- [ ] 核對 YMES 的 `TBLSYSMODULE`、`TBLSYSMENUITEM`、`TBLSYSFUNCTION` 及使用者功能授權是否已有 `SPECIALBASISMAINTAIN`（QC110）與「品質資料 → 基本資料」階層；僅補缺少的 YMES menu/function mapping，讓正式 API 菜單能顯示此功能。
- [ ] QC110 沿用 QC109 已交付的 `database/reference/mestest_qc_factor_maintain_source_ddl.sql` 與 `database/reference/mestest_qc_factor_maintain_create.sql`；先比對 YMES live schema/migrations 的 `tblsysmesparameter` 與 `fc_*` 表，只對實際缺少的 schema 增加 forward migration。不要重複建表、清除既有資料、匯入 Oracle 業務列或在 Oracle 執行 CREATE。
- [ ] 實作 FunctionKey `QC110` 保護的獨立 `GET /api/qc/special-basis-maintain/parameters`，只回傳 `IssueState=2`、`ParameterType=0`、`ParameterClass=0` 且 `TableName LIKE 'FC_%'` 的參數目錄；勿與 QC109 的 ParameterClass=1 混用。以 ParameterNo 查回伺服器設定，不接受 request 任意指定 table/column/condition。
- [ ] 實作 `GET /api/qc/special-basis-maintain/parameters/{parameterNo}/data`，依已授權 ParameterNo 查 YMES metadata，回傳動態欄位（name/type/length/nullable）與 Condition 範圍的資料列；TableName、FieldName、Condition 都須由服務端參數目錄解析並用 schema/table/column allowlist 驗證。
- [ ] 實作 `PUT /api/qc/special-basis-maintain/parameters/{parameterNo}/data`：伺服器端驗證 `^[A-Za-z][A-Za-z0-9_]*$` 欄名、metadata 欄位集合、型別、長度與 NOT NULL；單一 transaction 內刪除舊 Condition 範圍並插入完整 rows，空 rows 表示清除此範圍，失敗 rollback。沒有 DataStamp；避免併發整表覆寫。
- [ ] 實作 `POST /api/qc/special-basis-maintain/parameters/{parameterNo}/import-preview` 解析 WinForms 接受的 Excel 檔並只回 preview rows，不寫資料庫；CSV/Paste 由 Vue 先編輯 Grid，最後才按 Save。
- [ ] 舊 Condition 不可直接當任意 SQL 執行。若不能安全轉成固定 predicate，建立經審查的 server-side 對照並記錄 YMES 環境差異。所有 endpoint（含清單、資料、preview、更新）都需套登入與 QC110 功能授權。
- [ ] 完成 YMES schema/API/migration 文件後 push GitHub 並通知公司端 pull，以驗收 Production API。

### QC109 要因資料維護（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/要因資料維護.md`、公司端 QC109 區段、`docs/DATABASE_CONNECTION.md`，並先 pull 取得 Vue DEV 頁與 API 規格。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_factor_maintain_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_factor_maintain_create.sql`，核對 YMES live schema/migrations 的 `tblsysmesparameter`、`fc_posionlist`、`fc_qcformcallbackitemmap`、`fc_thickness`。只對缺少的表/欄位新增 forward migration，保留 YMES 擴充與資料，不 drop/recreate、不匯入 MES 業務列、不在 Oracle 執行 CREATE。
- [ ] 來源 NOT NULL/default：`tblsysmesparameter`: ParameterNo varchar(30) PK、ParameterName varchar(50)、IssueState numeric(1,0) DEFAULT 0、ParameterType numeric(1,0) DEFAULT 0、ParameterClass numeric(1,0)；TableName/FieldName varchar(50)、Condition varchar(50)、Description varchar(255)、FunctionName varchar(50)、ShowName varchar(50) DEFAULT 'N/A'、DataStamp numeric(11,0) DEFAULT 0、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp。`fc_posionlist.posion_no varchar(50)` PK；`fc_qcformcallbackitemmap.fieldname varchar(30)` PK、QCItemNo varchar(50) NOT NULL、Description varchar(500) NULL；`fc_thickness.spec varchar(50)` PK。來源僅 PK unique index，沒有 FK/額外 index。資料庫欄位 NULL/default 依來源保留。
- [ ] 實作 FunctionKey `QC109` 保護的 `GET /api/qc/factor-maintain/parameters`，只列出已核准、ParameterType=0、ParameterClass=1、TableName LIKE `FC_%` 的 Table 參數，回傳 ParameterNo/Name/TableName；以 ParameterNo 伺服器端查回設定，不採用 request 直接指定 TableName/FieldName/Condition。
- [ ] 實作 `GET /api/qc/factor-maintain/parameters/{parameterNo}/data`：服務端驗證參數及允許的 YMES schema/table，依欄位 metadata 回傳 columns(name/type/length/nullable) 與資料 rows；套用目錄設定 Condition 並依 FieldName 排序。限制 table/column identifier 到 metadata allowlist，不可將使用者字串當 SQL identifier 或任意 where clause。
- [ ] 實作 `PUT /api/qc/factor-maintain/parameters/{parameterNo}/data`：接收整份 Grid rows；驗欄位集合、型別、長度與 NOT NULL，在單一 transaction 內刪除 Condition 範圍資料，再 parameterize insert 所有 rows；插入失敗 rollback；空 rows 表示清空該 Condition 範圍。確保不刪除條件以外的記錄。沒有 DataStamp；完整替換需避免併發覆寫，至少以資料庫 transaction/適當鎖處理。
- [ ] 實作 `POST /api/qc/factor-maintain/parameters/{parameterNo}/import-preview` 解析 WinForms `.xls` 範本（第一列欄名），只回 preview rows，不寫 DB；CSV 可由 Vue 本地解析。功能頁的 Import/Paste 都只修改前端 Grid，最後仍須按 Save。
- [ ] FunctionKey/登入權限套用於目錄、讀取、preview 與更新 endpoints。完成後更新 API/schema/migration 文件、本待辦並 push GitHub，通知公司端 pull。若舊 Condition 字串無法安全轉換，設計可稽核的伺服器端固定 predicate 對照並記錄環境差異；不得任意執行目錄中的 SQL。

### QC104 Wafer 抽樣計劃（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/Wafer抽樣計劃.md`、本檔 QC104 區段、公司端 QC104 區段與 `docs/DATABASE_CONNECTION.md`。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_sampling_plan_wafer_source_ddl.sql`、PostgreSQL CREATE 參考 `database/reference/mestest_qc_sampling_plan_wafer_create.sql`，核對 YMES live schema/migrations。特別先查 `tblqcsamplingrange`：WinForms QC103/QC104 都以相同 PlanNo 表名讀寫，Oracle 沒有 FK/PK/index；核對 QC103 已建表/程式/API及其他既有使用，決定兼容的共用儲存策略，再新增必要 forward migration。不可 drop/recreate、清空既有資料或在 MES Oracle 執行 CREATE。
- [ ] 保留 `tblqcsamplingplan_wafer` nullable/default：PlanNo varchar(25) NOT NULL PK；SampleType numeric(1,0) NOT NULL；SampleRate numeric(3,0)、EveryNum/SamplePieceNum numeric(4,0)、SamplePointNum numeric(6,0) NOT NULL DEFAULT 0；Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp nullable；IssueState numeric(1,0) NOT NULL DEFAULT 0；FunctionName varchar(50)、AQLType varchar(25)、AQL varchar(10)、QCLevel varchar(6)、RejectQty numeric(4,0) nullable；DataStamp numeric(11,0) nullable DEFAULT 0。`tblqcsamplingarea_wafer` 以 (PlanNo,Area) PK，AreaType numeric(18,0) NOT NULL DEFAULT 0、AreaNo varchar(50) nullable DEFAULT '0'。來源三表都無 FK；range 表本身無 PK/index。
- [ ] 實作 FunctionKey `QC104` 保護的 GET/POST/PUT/DELETE `/api/qc/sampling-plans/wafer`，明細 GET `/{planNo}/details`、選項 GET `/aql-options`。List 回傳 audit/DataStamp；主檔、Range、Area aggregate 寫入同一 transaction。AQLType/QCLevel/AQL 選項依 `wsSYS.LoadMesParameterRef` 的 AQLType/QCLevel/AQLData 參數和 105E/GB2828 前綴篩選。
- [ ] 實作狀態規則：編輯限 0；0/-1 刪除主檔與明細，2 用 nullable DataStamp CAS 轉 -1，1 拒絕。Approve IssueNo `WAFER SAMPLING PLAN`：無 Job/Detail 時 0→2，有簽核流程時建立 Issue、轉 1、通知首簽核群組，核准 2/退回 0；狀態與 Reviser/ReviseDate/DataStamp 一併維護。
- [ ] 參數化 SQL；依規格更新 API/schema 文件與本待辦，完成後 push GitHub 並通知公司端 pull。UI 的 DEV AQL 選項是假資料；不得將 MES 業務資料匯入 YMES。


### QC105 品質原因子類別資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/品質原因子類別資料.md`、公司端 QC105 區段與 `docs/DATABASE_CONNECTION.md`。
- [ ] 對照 `database/reference/mestest_qc_reason_subtype_basis_source_ddl.sql` 和 `database/reference/mestest_qc_reason_subtype_basis_create.sql`，核對 YMES live schema/migrations；保留 nullable/default。只有確認缺表或結構差異後建立 forward migration，不得 drop/recreate 或清除既有資料。
- [ ] 欄位依來源保留：`reasontype numeric(1,0) NULL`、`reasonsubtype varchar(50) NOT NULL PK`、`reasonsubtypename varchar(100) NULL`、`description varchar(255) NULL`、`creator/reviser varchar(10) NULL`、`createdate/revisedate timestamp NULL`、`issuestate numeric(1,0) NOT NULL DEFAULT 0`、`datastamp numeric(11,0) NULL DEFAULT 0`。只需 PK unique index；來源沒有 FK 或額外索引。WinForms 雖要求 ReasonSubTypeName 必填，Oracle 欄位本身允許 NULL。
- [ ] 實作 FunctionKey `QC105` 保護的 `GET /api/qc/reason-subtypes?searchFieldName=ReasonSubType&searchFieldValue=`，回傳所有狀態、ReasonType 顯示文字、audit/DataStamp 與依 Issue Control 設定計算的 `editableFields`；搜尋欄只接受 ReasonSubType。
- [ ] 實作 `GET /api/qc/reason-subtypes/reason-types`，依 MES ValueList 等價設定回傳 `{ value, label }`。來源 VB 註解中的代碼不完整且不同年代內容互相矛盾，不可當成正式選項；如 YMES 尚無此 lookup，先記錄設定來源與缺口再決定相容方式。
- [ ] 實作 POST/PUT/DELETE/approve：新增驗必填/長度/ValueList/主鍵唯一並建立狀態 0 audit；修改主鍵唯讀，狀態 1 拒絕，狀態 2/-1 僅允許 Issue Control 的 EditFlag 欄位；DataStamp nullable CAS，衝突回 409。刪除 0/-1 實體刪除、2→-1、1 拒絕。Approve Issue Job Map `ReasonSubType BASIS`，支援直接 0→2 或 Issue 流程 0→1→2/退回 0。
- [ ] 多筆刪除／核准逐筆執行、第一筆錯誤停止，保留已成功項目；使用參數化 SQL、登入者 audit、交易與權限檢查。更新 API/schema 文件及 migration 索引並 push GitHub；不得連 MES Oracle 或匯入 MES 業務資料。


### QC106 品質原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/品質原因基本資料.md`、公司端 QC106 區段及 `docs/DATABASE_CONNECTION.md`；先完成 QC104/QC105 前置交接，尤其 QC105 子類別 options API。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_reason_basis_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_reason_basis_create.sql`，核對 YMES live schema/migrations；只有確認缺表/欄位時建立 forward migration，保留既有 YMES 擴充與資料，不可 drop/recreate，也不可在 MES Oracle 執行 CREATE。
- [ ] 保留來源欄位：ReasonNo varchar(20) NOT NULL PK；ReasonName varchar(100) NOT NULL；ReasonType/ReasonLevel numeric(1,0) NOT NULL；Description varchar(255) NULL；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；IssueState numeric(1,0) NOT NULL DEFAULT 0；ReasonSubType varchar(50) NULL；DataStamp numeric(11,0) NULL DEFAULT 0。只有 PK unique index；Oracle 沒有 FK/額外索引。
- [ ] 實作登入及 FunctionKey QC106 授權的 `GET /api/qc/reason-basis?searchFieldName=ReasonNo&searchFieldValue=`，回傳四種 IssueState、DataStamp 及依 Issue Control 算出的 `editableFields`。只允許 ReasonNo LIKE 搜尋，支援 `%`/`_` 萬用字元，狀態由 UI 篩選。
- [ ] ReasonType options 由 MES `GetValueList(TBLQCREASONBASIS, REASONTYPE)` 等價服務提供；不可把 WinForms 舊註解中的數字/文字當正式清單。子類別 options 僅回傳 QC105 `IssueState=2` 且 ReasonType 相符的項目；`GET /api/qc/reason-basis/subtype-options?reasonType=` 需先授權並驗證參數。Oracle 沒 FK，新增/更新由 API 驗證引用。
- [ ] 實作 `POST /api/qc/reason-basis`、`PUT /api/qc/reason-basis/{reasonNo}`：必填、長度、ReasonLevel 0–9、逗號禁用與 ReasonNo 大寫、ValueList/子類別狀態及類別檢查、Issue Control 欄位白名單。登入者與伺服器時間負責 audit；ReasonNo 修改唯讀。
- [ ] DataStamp nullable DEFAULT 0；PUT/DELETE/approve compare-and-swap 必須區分 NULL 與 0，成功更新版本，衝突回 409。狀態 0 全欄位可編輯、1 唯讀、2/-1 僅允許 Issue Control EditFlag 欄位；缺少控制設定時唯讀。
- [ ] 實作 DELETE：狀態 0/-1 實體刪除，狀態 2 CAS 後更新為 -1 並記錄 Reviser/ReviseDate/DataStamp，狀態 1 拒絕。多選 UI 逐筆呼叫，第一筆失敗停止並回報先前成功筆數。
- [ ] 實作 `POST /api/qc/reason-basis/{reasonNo}/approve`：依 Issue Job Map `REASON BASIS` 無 Job/Detail 時直接 0→2；有 Detail 建 Issue、0→1、通知首簽核群組；後續核准 2、退回 0。回傳真實狀態及 DataStamp，不能假設一律直接 Active。
- [ ] 全部 SQL 參數化；API/schema/migration 變更後更新規格及本清單並 push GitHub，通知公司端 pull。公司端 DEV 頁 ReasonType 標 DEMO，Production 不使用 localStorage 假資料。

### QC107 計數判別法則基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/計數判別法則基本資料.md`、公司端 QC107 區段與 `docs/DATABASE_CONNECTION.md`；依序完成 QC104–QC106 既有交接，然後核對 QC107 API/schema。
- [ ] 對照 Oracle DDL `database/reference/mestest_qc_att_rule_basis_source_ddl.sql` 與 PG CREATE 參考 `database/reference/mestest_qc_att_rule_basis_create.sql`，檢查 YMES live schema/migrations。主檔 `tblqcattrulebasis` PK RuleNo；明細 `tblqcattruledetail` 複合 PK (RuleNo,Action,ReasonType,ReasonLevel)；兩表均無 FK、無 DataStamp。只建立必要 forward migration，不 drop/recreate、不清除資料。
- [ ] 對照共用 AQL DDL `database/reference/mestest_qc_aql_source_ddl.sql`、PG CREATE 參考 `database/reference/mestest_qc_aql_create.sql`，檢查是否已經有 YMES `tblqcaql`。若已存在直接重用；只有確定缺少才新增 migration，保留現有共用表資料/擴充欄位。來源 PK (AQLType,MinNum,MaxNum,SLevel,AQL)，無 FK/額外索引。
- [ ] `tblqcattrulebasis` 欄位：RuleNo varchar(20) NOT NULL PK；ComponentCollect numeric(1,0) NOT NULL DEFAULT 0；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；IssueState numeric(1,0) DEFAULT 0 NULL。`tblqcattruledetail`：RuleNo varchar(20)、Action numeric(1,0) NOT NULL、ReasonType numeric(1,0) NOT NULL DEFAULT 0、ReasonLevel numeric(1,0) NOT NULL DEFAULT -1、RejectNum numeric(14,4)、RuleType numeric(1,0) DEFAULT 0、Rate varchar(10)、ChangeAQL varchar(30)、Creator/Reviser varchar(10)、日期 timestamp；按來源保留 nullable/default。
- [ ] 實作 FunctionKey QC107 登入權限保護的 `GET /api/qc/attribute-rule-bases?searchFieldName=RuleNo&searchFieldValue=` 與 `GET /{ruleNo}/details`，回主檔/明細及 Issue Control `editableFields`。RuleNo LIKE 支援 `%`/`_`；前端按四狀態過濾。主/明細讀取須授權同一 FunctionKey。
- [ ] 建立 Action/ReasonType ValueList options API；Action 等價於 `TBLQCATTRULEDETAIL.ACTION`，ReasonType 等價於 `GetValueList(TBLQCATTRULEBASIS,REASONTYPE)`。舊程式註解只列部分值，不可作正式選項。自訂 AQL 選項先讀 SysInfo/UserDefineAQL，啟用時由 `tblqcaql` 回 distinct AQL、AQLType 非空；關閉時 API 回 enabled=false，不查選項資料列。
- [ ] 實作 aggregate `POST/PUT /api/qc/attribute-rule-bases`：主檔與整組明細單一交易保存。驗證 RuleNo 大寫/長度/唯一、ComponentCollect 0/1、Action/ReasonType 合法、ReasonLevel -1..9、組合 PK 唯一；Number RejectNum 必填且 >=1 整數、上限 9,999,999,999（符合 NUMBER(14,4)）、Rate 必填正數且 <=10 字元、AQL 型態檢查 UserDefineAQL。非啟用模式的空 Rate/ChangeAQL 對 Oracle 等價 NULL；RejectNum 在非 Number 模式依舊 API 的空字串轉換為 0。
- [ ] 舊 WinForms Edit 會刪除再重建主/明細，沒有 DataStamp；API 使用 transaction 原子化，保留舊狀態及 Creator/CreateDate。Def 雖設定 Reviser/ReviseDate，SOAP 未傳入，舊 Core 更新後沒有寫回修訂 audit；YMES 若補 audit 必須記錄為新 API 差異。
- [ ] 實作 DELETE：狀態 0/-1/2 實體刪除明細與主檔，同交易；狀態 1 拒絕。Approve 僅 state0 且每個 RuleNo 至少一筆明細；依 Issue Job Map `QCITEM_A RULE DATA` 無流程時 0→2，有簽核則 0→1、通知首簽群組，核准2/退回0；回傳實際狀態。
- [ ] 多選刪除/核准依 WinForms 逐筆執行，首錯停止並保留已完成筆數；全部 SQL 參數化。比對 YMES schema、遷移及 API 後更新文件並 push，通知公司端 pull。不得連 MES Oracle 或匯入業務列。

### QC108 計量品管法則基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/計量品管法則基本資料.md`、公司端 QC108 區段與 `docs/DATABASE_CONNECTION.md`；完成既有 QC104–QC107 API/schema 交接後再核對 QC108。
- [ ] 對照 Oracle source DDL `database/reference/mestest_qc_variable_rule_basis_source_ddl.sql` 與 PostgreSQL CREATE 參考 `database/reference/mestest_qc_variable_rule_basis_create.sql`，檢查 YMES live schema/migrations。只在確認缺表/欄位時新增 forward migration；保留 YMES 擴充與資料，不 drop/recreate，不在 Oracle 執行 CREATE。
- [ ] 欄位 nullable/default 依來源保留：RuleSerial varchar(30) NOT NULL PK；RuleNo varchar(30) NOT NULL；RuleName varchar(50) NOT NULL；FunctionName varchar(50) NULL；RuleType/IssueState numeric(1,0) NULL DEFAULT 0；Creator/Reviser varchar(10) NULL；CreateDate/ReviseDate timestamp NULL；Description varchar(255) NULL；RuleOption numeric(1,0) NULL；TestOutof/TestCount numeric(2,0) NULL；CompareValue1/2/Relation varchar(10) NULL；WEType numeric(1,0) NULL DEFAULT 0；DataStamp numeric(11,0) NULL DEFAULT 0。唯一索引僅 PK(RuleSerial)，沒有 FK/額外索引；RuleNo 無 DB 唯一限制。
- [ ] 實作 FunctionKey QC108 保護的 `GET /api/qc/rule-bases`，回傳四狀態資料、audit、DataStamp、editableFields；UI 篩選 IssueState、`RuleType<>2`、`WEType=0` 並依 RuleSerial/RuleNo 排序。新增 rule-options options API，從現存 ValueList 供應原始儲存值/標籤；依規格的 UI/Oracle RuleOption mapping。另實作 functions options API，等價於舊 `LoadQRFunctionName` 對 `udQCRule` 私有 FUNQR* 函式目錄，不載入 DLL 反射給 Web。
- [ ] 實作 POST/PUT/DELETE/approve：新增 RuleSerial=RuleNo、state0、audit；RuleNo 大小寫不敏感重複檢查在 API transaction 內執行。更新以 DataStamp nullable CAS，維護 Reviser/ReviseDate/DataStamp；`editableFields` 缺失時唯讀。Delete 0/-1 實體刪除、2→-1、1 拒絕；Approve state0，依 Issue Job Map `QC RULE BASIS` bypass 0→2 或建立 Issue 後 0→1，核准2/退回0，使用共用 Issue/通知流程。
- [ ] 依 WinForms sparse update：只更新有傳入的欄位；System Rule 欄位與 User Define FunctionName 的切換不應無意清除其他既有欄位。驗證 RuleName、RuleNo 長度/必填、RuleType、FunctionName、七種 RuleOption、TestOutof/TestCount 1–99、TestOutof≥TestCount、Value Range 上下限、SpecLimit relation 等。RuleOption=9 舊資料顯示 Other；API 保留既有 9，新建 Other 用 5。多選操作逐筆、第一個錯誤停止並回報已完成筆數；全部 SQL 參數化。
- [ ] 更新 API/schema/migration 文件及本清單後 push GitHub 並通知公司端 pull。不得連 MES Oracle 或匯入 MES 業務資料。

### QC103 抽樣計劃（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/抽樣計劃.md`、本檔 QC103 區段、公司端 QC103 區段及 `docs/DATABASE_CONNECTION.md`。
- [ ] 比對 YMES PostgreSQL live schema/migrations。Oracle source DDL：`database/reference/mestest_qc_sampling_plan_source_ddl.sql`；PostgreSQL 空表 CREATE 參考：`database/reference/mestest_qc_sampling_plan_create.sql`。只在確認缺漏時新增 forward migration，保留 YMES 擴充欄位；不可在 MES Oracle 執行 CREATE。
- [ ] 保留 `tblqcsamplingplan` 欄位與 nullable/default：PlanNo varchar(25) NOT NULL PK；PlanName varchar(50) NOT NULL；PlanType numeric(1,0) NOT NULL；FunctionName varchar(50)、Description varchar(255)、Creator/Reviser varchar(10)、CreateDate/ReviseDate timestamp nullable；IssueState numeric(1,0) NOT NULL DEFAULT 0；SQtyCanBBiggerThenPQty numeric(1,0) NOT NULL DEFAULT 0；DataStamp numeric(11,0) nullable DEFAULT 0。`tblqcsamplingparameter` 欄位 PlanNo varchar(25)、ParameterNo varchar(20) NOT NULL 複合 PK，ParameterValue varchar(255) nullable。來源沒有 FK/額外索引。
- [ ] 實作 FunctionKey `QC103` 保護的 GET/POST/PUT/DELETE `/api/qc/sampling-plans`；主檔和參數 Aggregate 寫入需單一交易，Edit/核准/狀態刪除使用 nullable DataStamp CAS。狀態 0/-1 實體刪除兩表、2 改 -1、1 拒絕。
- [ ] 實作 `/plan-types`、`/function-options`、`/{planNo}/parameters`、`/parameter-options/{parameterNo}`：PlanType 取自現有 ValueList；FunctionName 依 `udSamplingFunction` 名稱篩選 `FUNSA*`；ParameterValue 選項依 `wsSYS.LoadElementValue` 對應邏輯；ShowFlag 依 PlanType 推導，不需新增資料庫欄位。
- [ ] 實作核准 `POST /{planNo}/approve`：先驗證參數，再依 Issue Job Map `SAMPLING PLAN` 無 Job 時 0→2；有 Job/Detail 時建立 Issue、狀態 1、通知第一簽核群組，核准轉 2、退回回 0。所有狀態轉換維護 Reviser/ReviseDate/DataStamp。
- [ ] 實作 `POST /{planNo}/simulate`：依 0–7 PlanType 呼叫 WinForms 原 Core 的 `ShowParameter_*` 對應規則；不能沿用 DEV 固定抽樣近似值作正式計算。
- [ ] 使用參數化 SQL；完成後更新 API/schema 文件與本待辦並 push GitHub，通知公司端 pull。來源無 MES 業務資料，Oracle 只查 DDL。


### QC102 家用端交付（2026-11-19）

- [x] schema/API與正式頁面主要操作已完成，驗證範圍見 `docs/modules/QC/抽樣計劃設定_1916.md` 最新紀錄。
- [ ] 該文件明列的共用來源／引擎／範本／未測情境繼續追蹤；角色ACL依使用者決策暫緩。

### QC101 家用端交付（2026-11-19）

- [x] schema/API與正式頁面主要操作已完成，驗證範圍見 `docs/modules/QC/抽樣表格.md` 最新紀錄。
- [ ] 該文件明列的共用來源／引擎／範本／未測情境繼續追蹤；角色ACL依使用者決策暫緩。

### QC402 家用端交付（2026-11-19）

- [x] schema/API與正式頁面主要操作已完成，驗證範圍見 `docs/modules/QC/次分類基本資料.md` 最新紀錄。
- [ ] 該文件明列的共用來源／引擎／範本／未測情境繼續追蹤；角色ACL依使用者決策暫緩。

### QC401 家用端交付（2026-11-19）

- [x] schema/API與正式頁面主要操作已完成，驗證範圍見 `docs/modules/QC/主分類基本資料.md` 最新紀錄。
- [ ] 該文件明列的共用來源／引擎／範本／未測情境繼續追蹤；角色ACL依使用者決策暫緩。

### QC403 異常原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常原因基本資料.md`、`todolist_officepc.md` QC403 區段及 `docs/DATABASE_CONNECTION.md` 的 QC403 查詢紀錄。
- [ ] Oracle source DDL：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`；PostgreSQL 空表 CREATE：`database/reference/mestest_qc_hold_item_basis_create.sql`。先比對 YMES live schema/migrations；只有 `tblqcholditembasis` 缺少才新增 forward migration，保留 YMES 擴充欄位。不可在 MES Oracle 執行 CREATE。
- [ ] 主表 PK `holditemno`；僅 PK unique index，無 FK/額外索引。保留來源 nullable/default：`createerf` numeric(1,0) DEFAULT 1 NULL、`datastamp` numeric(11,0) DEFAULT 0 NULL；`holdsystem` numeric(1,0) DEFAULT 0 NOT NULL；IssueState numeric(1,0) DEFAULT 0 NOT NULL。Oracle DATE 對應 timestamp without time zone。
- [ ] 實作 `GET /api/qc/hold-item-basis?searchFieldName=&searchFieldValue=`，回傳查詢條件內所有 IssueState 與 audit/DataStamp；搜尋欄 whitelist 僅 HoldItemNo/MClassNo/SClassNo；FunctionKey `QC403`。
- [ ] 實作 `GET /api/qc/hold-item-basis/lookups`：已簽核 MClass、依 MClass 過濾的已簽核 SClass、已簽核 HoldDispGroup、IssueState=2 且 CURVERSION=1 的 OCAP、HoldSystem 0/1/2（MES/EAP_Exception/EDA_Exception）。優先重用已有 QC lookup service，禁止新增重複 lookup 表。
- [ ] 實作 POST 建立與 PUT 修改。伺服器設定 Creator/CreateDate、IssueState=0、Reviser/ReviseDate/DataStamp；HoldItemNo PK。修改只允許 HoldItemName、CreateERF、HoldSystem、HoldDispGroup、OCAPNo/Version、Description；HoldItemNo/MClassNo/SClassNo 唯讀；待簽核禁止修改，Active/Unused 按舊 IssueControl 設定的欄位權限限制。以 HoldItemNo + IssueState + nullable DataStamp CAS。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 改為 -1；1 拒絕。核准只允許 0，FunctionNo/Issue Job Map key 為 `HOLDITEM BASIS`；沒有 Job/Detail 時直接轉 2，有簽核流程時建立 Issue、轉 1、通知第一群組，保留共用退回/核准流程。
- [ ] 實作 `POST /api/qc/hold-item-basis/import` 支援 `.xls/.xlsx` 第一工作表，欄序為 HOLDITEMNO、MCLASSNO、SCLASSNO、HOLDITEMNAME、CREATEERF、HoldSystem、OCAPNo、HOLDDISPGROUP、DESCRIPTION；逐列檢查分類已簽核、群組存在、OCAP 已簽核、主鍵不重複。每列獨立 transaction，回傳成功/失敗與 message；維持舊系統部分成功行為。DEV 頁目前只解析 CSV。
- [ ] 所有 endpoints 要登入並檢查 QC403 功能權限；參數化 SQL、DTO 驗證、檔案大小/格式限制，不讀/寫 MES Oracle。更新 API 文件、migration 索引及本 TODO。

### QC405 異常處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常處理群組.md`；公司端已完成 `/YMES/QC/HoldDispGroup` Vue DEV 頁，包含群組主檔及主處理/CC 使用者三 Grid。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations。Oracle source DDL/PG 空表 CREATE 已存在：`TBLQCHOLDDISPGROUPBASIS` 參照 `mestest_qc_first_group_lookups_source_ddl.sql` / `_create.sql`；`TBLUSRUSERGROUP` 參照 `mestest_equipment_engineer_group_member_source_ddl.sql` / `_create.sql`。兩表缺漏才新增 forward migration；本功能沒有新增表，不要在 Oracle 建表。
- [ ] 實作 `GET /api/qc/hold-disposition-groups?searchFieldName=GroupNo&searchFieldValue=`：回傳四狀態群組及 audit/DataStamp，搜尋只允許 GroupNo。FunctionKey 權限為 QC405。
- [ ] 實作 `GET /api/qc/hold-disposition-groups/options/users?issueState=2`，等價 WinForms `wsUSR.LoadUserSecurity(IssueState=2)`，只回已核准使用者的 DepartmentNo/UserNo/UserName。若 YMES 已有安全的使用者查找 API，優先重用。
- [ ] 實作 `GET /api/qc/hold-disposition-groups/{groupNo}/members`，回 `{primary: string[], cc: string[]}`。直接 UI 成員對應 `TBLUSRUSERGROUP.GroupType=5/6 AND AgentOf='NA'`；顯示可沿用已核准使用者資料，缺失/停用的既存關聯仍需可見並可移除。
- [ ] 實作 POST `/api/qc/hold-disposition-groups` body `{groupNo, groupName, description}`：GroupNo 必填/長度20/大寫、GroupName 必填/長度50、Description 長度255；伺服器寫 Creator/CreateDate、IssueState=0、DataStamp 初值。
- [ ] 實作 PUT `/api/qc/hold-disposition-groups/{groupNo}?dataStamp=`，僅更新 GroupName/Description，只接受 IssueState 0/2；由伺服器更新 Reviser/ReviseDate/DataStamp，以 GroupNo + nullable DataStamp CAS。
- [ ] 實作 POST `/api/qc/hold-disposition-groups/copy`，body `{sourceGroupNo, groupNo, groupName, description}`；transaction 同時新增主檔並複製舊 Core `ShowHoldDispGroupDetail(CCGroup=true)` 回傳的 5/6 類成員。舊 `Show...` 不限 AgentOf，但 `AddHoldDispGroupDetail` 將複製關聯寫成 AgentOf='NA'；保持這個相容行為，或在 API 規格清楚標註產品決策後的修正。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除主檔與該 GroupNo 的 5/6 類成員；IssueState 2 改 -1 並保留成員；IssueState 1 拒絕。用主檔 DataStamp CAS 並在 transaction 中處理。
- [ ] 實作 PUT `/api/qc/hold-disposition-groups/{groupNo}/members?dataStamp=`，body `{primaryUserNos, ccUserNos}`。單一 transaction 先以 GroupNo + DataStamp CAS 更新主檔 Reviser/ReviseDate/DataStamp，再刪除該群組所有 GroupType 5/6 關聯，重建選取的直接成員 (AgentOf='NA')；兩個清單不得重複同一 UserNo，空陣列表示清除。每個 GroupType 5 成員需沿用 USR `AddUserAgentToGroup(UserNo, GroupNo, 5)` 代理人同步副作用。
- [ ] 實作 `POST /api/qc/hold-disposition-groups/{groupNo}/approve?issueState=0&dataStamp=`：Issue Job Map 精確 key `HOLD DISP GROUP`；沒有 Job/Detail 直接轉 2，有流程建立 Issue、狀態轉 1、通知第一簽核群組，核准轉 2、退回轉 0。
- [ ] 所有端點驗證登入、QC405 功能權限、SQL 參數化及版本衝突；先比對 live schema，不重建已存在表。部署 API/schema 後更新 handoff、push 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC404 異常說明（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/異常說明.md`、`docs/DATABASE_CONNECTION.md`；公司端已對照 `frmHoldDescription`、`frmHoldDescriptionDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`，完成 QC → 異常設定 → 異常說明 Vue DEV 頁與 `/YMES/QC/HoldDescription` 路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations；來源 CREATE 參考 `database/reference/mestest_qc_hold_description_create.sql`，只在缺少時做 forward migration，保留 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_hold_description_source_ddl.sql`。`tblqcholddescription` PK 為 `holddescriptionid`；`mclassno` 有非唯一索引、無 FK；`holddescription varchar(4000) NOT NULL`、`issuestate NOT NULL DEFAULT 0`、`datastamp nullable DEFAULT 0`。不對 `(mclassno, holddescription)` 加唯一限制。
- [ ] 實作 `GET /api/qc/hold-descriptions?searchFieldName=MClassNo&searchFieldValue=`，回全部狀態及 audit/DataStamp；搜尋欄白名單只允許 MClassNo，保持 WinForms query condition 語意。
- [ ] 實作主分類 options endpoint，只回 `TBLQCMCLASSBASIS.IssueState=2`；沿用 QC406 lookup table/API 時仍要驗證 QC404 FunctionKey。
- [ ] 實作 POST `/api/qc/hold-descriptions` body `{mClassNo, holdDescription, description}`；檢查主分類核准、必填與長度，伺服器建立 GUID HoldDescriptionID、Creator/CreateDate、IssueState=0、DataStamp 初值，回傳完整新列。
- [ ] 實作 PUT `/api/qc/hold-descriptions/{holdDescriptionId}?dataStamp=`，僅允許修改 Description；MClassNo/HoldDescription 在 WinForms 修改視窗唯讀。狀態 0/2 可更新，伺服器寫 Reviser/ReviseDate 並遞增 DataStamp，nullable DataStamp 以 `IS NULL` CAS。
- [ ] 實作 DELETE：0/-1 實體刪除，2 軟停用為 -1，1 拒絕；所有寫入/刪除用 ID + DataStamp CAS。多選 UI 逐筆呼叫，首筆失敗停止並回報部分成功。
- [ ] 實作核准端點，Issue Job Map key 為 `HOLD DESCRIPTION`；沒有 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、處理附件/通知第一簽核群組，核准轉 2、退回 0。
- [ ] 所有 API 驗證登入與 FunctionKey `QC404`、SQL 參數化。先核對 YMES live table/index，勿盲目執行 CREATE；部署 API/schema 後更新 handoff 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC407 最後處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/最後處理群組.md`、`docs/DATABASE_CONNECTION.md`；公司端已對照 `frmFinalGroup`、`frmFinalGroupDef`、`wsQC`、`clsQCBasis`、`clsBasis_UPD`，完成 QC → 異常設定 → 最後處理群組 Vue DEV 頁與 `/YMES/QC/FinalGroup` 路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes、migrations；來源 CREATE 參考 `database/reference/mestest_qc_final_group_create.sql`，只在缺少時做 forward migration並保留 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_final_group_source_ddl.sql`。`tblqcfinalgroup` 複合 PK `(mclassno, opno)`、只有 PK unique index、無 FK；`issuestate NOT NULL DEFAULT 0`、`datastamp nullable DEFAULT 0`。保留 `opno='*'` 通用作業站鍵。
- [ ] 實作 `GET /api/qc/final-groups?searchFieldName=&searchFieldValue=`，回全部狀態/audit/DataStamp；搜尋欄白名單限 MClassNo、OPNo，四狀態由 UI 篩選。
- [ ] 主分類、作業站、異常處理群組 options 只回 IssueState=2；可重用 QC406 查找實作，但檢查 QC407 FunctionKey 權限。先核對 YMES live schema，不重建已存在的 lookup 表。
- [ ] 實作 POST `/api/qc/final-groups`，body `{mClassNo, operationNos, groupNo, description}`；逐個所選 OP 建列，驗證必填/長度/核准選項/複合鍵唯一；`*` 僅作明確允許的通用 OP。批次以單一交易寫入，Audit/DataStamp 由伺服器設定。
- [ ] 實作 PUT `/api/qc/final-groups/{mClassNo}/{opNo}?dataStamp=`，僅更新 GroupNo/Description；狀態 0/2 可更新，修改者/日期與 DataStamp 由伺服器更新。nullable DataStamp 以 `IS NULL` CAS。
- [ ] 實作 DELETE：0/-1 實體刪除、2 軟停用成 -1、1 拒絕；全部以 DataStamp CAS。UI 多選逐筆、首筆失敗停止並回報部分成功。
- [ ] 實作核准端點，Issue Job Map 原始 key 必須使用 **`Final GROUP`**（注意它和 FunctionNo `FINAL GROUP` 不同）；沒有 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、附件處理/通知第一簽核群組，核准 2、退回 0。
- [ ] 所有 API 驗證登入與 FunctionKey `QC407`、SQL 參數化；部署 API/schema 後更新規格與 handoff，push 並通知公司端 pull。公司端未執行 build/test/瀏覽器驗收。

### QC406 第一處理群組（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/QC/第一處理群組.md`、`docs/DATABASE_CONNECTION.md` 和本節。公司端已對照 WinForms/Form/Service/Core，完成 Vue DEV 畫面及 QC → 異常設定 → 第一處理群組路由。
- [ ] 先比對 YMES PostgreSQL live schema、constraints、indexes 與 migrations。來源 CREATE 參考 `database/reference/mestest_qc_first_group_create.sql`；缺表/欄位才建 forward migration，不要盲目執行或刪除 YMES 擴充欄位。
- [ ] Oracle 原始 DDL：`database/reference/mestest_qc_first_group_source_ddl.sql`。表 `tblqcfirstgroup` 的 PK 為 `(mclassno, opno)`，只有 PK unique index、沒有 FK；DataStamp nullable DEFAULT 0，IssueState NOT NULL DEFAULT 0。保留 `OPNo='*'` 作為通用作業站 key。
- [ ] 實作 `GET /api/qc/first-groups?searchFieldName=&searchFieldValue=`，回全部狀態欄位與 audit/DataStamp，搜尋欄位白名單限 MClassNo、OPNo；UI 按四狀態本機篩選。
- [ ] 查找器沿用 YMES 已有 endpoint；若不存在，提供只回已核准資料的 options API：主分類 `TBLQCMCLASSBASIS` (MClassNo)、作業站 `TBLOPBASIS` (OPNo/OPName)、異常處理群組 `TBLQCHOLDDISPGROUPBASIS` (GroupNo/GroupName/Description)，均對應 WinForms `IssueState=2`。先比對 live schema；主分類/異常群組缺表時參考 `mestest_qc_first_group_lookups_create.sql`，作業站缺表時參考現有 `mestest_operation_and_system_create.sql`。不要覆蓋既有 YMES 擴充欄位。
- [ ] 實作 `POST /api/qc/first-groups` body `{mClassNo, operationNos, groupNo, description}`；一次對每個已選 OP 建列，驗證主分類/群組已核准、普通 OP 已核准、`*` 僅作為特殊通用 key 放行，檢查長度/必填/複合鍵唯一；API 設 Creator/CreateDate/IssueState=0/DataStamp 初值，多列寫入交易。
- [ ] 實作複合鍵 `PUT /api/qc/first-groups/{mClassNo}/{opNo}?dataStamp=`，只改 GroupNo/Description；依舊 Service 僅接受狀態 0/2。更新 Reviser/ReviseDate 並遞增 DataStamp，nullable DataStamp 用 `dataStamp=0&dataStampIsNull=true` 以 `IS NULL` CAS。
- [ ] 實作複合鍵 DELETE：狀態 0/-1 實體刪除，2 改 -1，1 拒絕；以 DataStamp CAS。UI 多選仍逐筆，第一筆失敗停止並回報部分成功。
- [ ] 實作核准 endpoint，Issue Job Map key 為 `FIRST GROUP`；無 Job/Detail 時直接轉 2，有流程時建立 Issue、轉 1、附件處理/通知第一簽核群組，Issue 核准轉 2、退回轉 0。所有狀態變更需以複合鍵 + DataStamp CAS。
- [ ] 所有 API 驗證登入與 FunctionKey `QC406` 權限，SQL 參數化；不新增 Oracle FK/cascade。部署 schema/API 後更新規格、push，通知公司端 pull Production API。公司端未執行 build/test/瀏覽器驗收。

### QC111 8D 原因基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 docs/modules/QC/8D原因基本資料.md、docs/DATABASE_CONNECTION.md 和本節。公司端已對照 frmReasonBasis_8D、兩個 Def Form、wsQC/clsQCBasis，完成 Vue DEV 雙 Grid 及 QC → 基本資料 → 8D 原因基本資料路由。
- [ ] 先以家用端管理連線比對 YMES live schema、constraints、indexes 及 migrations。來源 PostgreSQL 空表 CREATE 參考 database/reference/mestest_qc_8d_reason_create.sql；缺表/欄位才新增 forward migration，不要盲目執行或移除 YMES 擴充欄位。
- [ ] Oracle 原始 DDL 在 database/reference/mestest_qc_8d_reason_source_ddl.sql。兩表為 tblqcreasonbasis_8d、tblqcreasonsubtype_8d；各只有單一 PK unique index、沒有 FK。Subtype 的 ReasonType 為 numeric(1,0)，detail 的 ReasonType 為 varchar(50)；DataStamp nullable DEFAULT 0。維持來源型別，不要把兩表欄位型別強制統一。
- [ ] 實作 GET /api/qc/8d-reason-subtypes?search=，回傳 ReasonSubType 搜尋結果及四狀態資料；GET /api/qc/8d-reasons?reasonSubType= 必須按子類別精確查詢。ReasonType 值已由 Oracle TBLSYSVALUELIST 核實為 1 Men、2 Machine_8D、3 Material、4 Method、5 Environment；API/options 使用同代碼並從 YMES 字典提供繁中顯示名（人員、設備、物料、方法、環境）。
- [ ] 實作 POST/PUT/DELETE 子類別 API：新增 IssueState=0、DataStamp=0 並由伺服器設定 audit；PUT 僅允許 IssueState=0；DELETE 狀態 0/-1 實體刪除、狀態 2 改 -1、狀態 1 拒絕。PUT/DELETE/核准都需以 nullable DataStamp 做 CAS，版本衝突回 409。
- [ ] 實作 POST /api/qc/8d-reason-subtypes/{reasonSubType}/approve：查 Issue Job Map 的 ReasonSubType BASIS；沒有 Job 或 Job 無 Detail 時直接轉 2，有簽核明細則建立 Issue 流程並轉 1、通知第一簽核群組；Issue 核准轉 2、退回轉 0。回傳實際 IssueState/DataStamp；不可假設所有核准都立即 Active。
- [ ] 實作原因明細 POST/PUT/DELETE。只允許父子類別 IssueState=0；ReasonType 從父列取得；ReasonNo/ReasonName 必填，ReasonLevel 0-9；更新需 DataStamp CAS 與伺服器 audit。來源沒有 FK，刪子類別不 cascade 明細；WinForms/Core 實際留下孤兒明細，保持可見相容性並在 API/日誌標示。
- [ ] 驗證登入、QC111 權限、欄位長度、唯一鍵、ReasonType 有效選項、父列狀態與 DataStamp。主/明細批次刪除按舊 WinForms 逐筆執行，第一筆失敗停止並回報已完成筆數；不可宣稱整批交易回滾。
- [ ] API/schema 部署後更新本節與規格狀態、push，通知公司端 pull 接 Production API。公司端尚未執行 build/test/瀏覽器驗收。

### EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader 格式檔設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/EQP/設備AutoLoader格式檔設定.md`、`docs/DATABASE_CONNECTION.md` 及本節；WinForms Form/Designer/Service/Core 已查，Oracle DDL 由公司端既有受保護 PowerShell 5.1 + ODP.NET 連線唯讀取得，Vue DEV 頁與三層選單已完成。
- [ ] 先以家用端管理連線比對 YMES PostgreSQL 的 4 張表、索引、現有 migration 及擴充欄位。缺表/欄位才做 forward migration；reference CREATE 檔是空表定義，不能盲目套用或重建資料。
- [ ] Oracle 來源表為 `TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`。主表沒有 PK constraint，唯一索引 `(EquipmentNo,FileNameFormat)`；三個子表的複合 PK 如規格；四表沒有 FK。CLOB 轉 PostgreSQL text；`DataStamp` nullable DEFAULT 0。
- [ ] 若 YMES 缺少來源表，依 `database/reference/mestest_eqp_autoloader_create.sql` 建 migration；來源 Oracle DDL `database/reference/mestest_eqp_autoloader_source_ddl.sql` 僅供比對。不要在 MES Oracle 執行 CREATE。
- [ ] 實作 `GET /api/eqp/auto-loader-scripts?equipmentNo=&equipmentType=`。條件可空，equipmentNo/type 採 WinForms exact match；回主畫面所需設備欄位、格式腳本、稽核/DataStamp 及三類子明細。
- [ ] 實作設備 lookup `GET /api/eqp/auto-loader-scripts/options/equipment?prefix=&equipmentType=&fileNameFormat=`。lookup 沿用設備主檔、依登入者權限過濾；有 fileNameFormat 時回 `hasMatchingFormat` 供預設套用排除已存在設備。
- [ ] 從 WinForms `frmEquipmentAutoLoaderSetupDef`、測試配方 lookup Forms 與 `wsEQP` 追出 TestRecipe/Parameter 候選真正來源，再提供 `/options/test-recipes` 或沿用現有 YMES API。不要把 Vue DEV 選項當正式 ValueList。
- [ ] 實作 POST/PUT/DELETE `/api/eqp/auto-loader-scripts`，所有 main/detail 在同交易寫入；更新/刪除採 DataStamp CAS（舊 NULL 需 `IS NULL` 比較，修改後 `COALESCE(DataStamp,0)+1`），POST/PUT request 不接受 audit/DataStamp，伺服器建立 audit。
- [ ] 主列更新時區分 detail array 未提交與明確空陣列；刪除要在交易中先刪三張 detail，再刪 main。舊 Oracle 沒有 FK，不能依賴 cascade。
- [ ] `POST /api/eqp/auto-loader-scripts/{equipmentNo}/{fileNameFormat}/default?dataStamp=` body 接受多個目標設備，排除已有相同檔名格式的目標、不覆寫，主列/三種明細完整複製，Creator/CreateDate 重設；來源 DataStamp CAS。回 `{ createdEquipmentNos, existingEquipmentNos, invalidEquipmentNos }`，UI 顯示新增/略過筆數。
- [ ] API 必須承接 `ChkAutoLoaderSetup` 與 `ChkAutoLoaderSetup_Raw` 等價邏輯：Header 欄位、座標/IC Ingot 例外、動態參數/核准 Test Recipe、公式與 RawData 對照、NonNumeric DataType；不能信任前端生成的簡化 FormatScript。
- [ ] 實作功能授權、輸入長度/唯一鍵/設備參照驗證及 409 CAS 回應；部署 API/schema/migration 後通知公司端 pull 做 Production 對接。公司端頁未執行 build/test/瀏覽器驗收。

### MTL MLB01 來料 AutoLoader 格式檔設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/來料AutoLoader格式檔設定.md`、`docs/DATABASE_CONNECTION.md` 與本節；公司端已直接查 Oracle DDL、建立 Vue DEV 頁與 API handoff。
- [ ] 先從家用端 SSH/管理連線比對 YMES PostgreSQL live schema、constraints、index 和 migration。新表來源是 `MESTEST.TBLMTLVENDORALSCRIPT`；若缺表，參考 `database/reference/mestest_mtl_autoloader_create.sql` 建 forward migration；不要直接執行 reference SQL 或覆蓋 YMES 擴充欄位。
- [ ] Oracle 結構：PK `MTLFOLDERPATH varchar(400)`；`FORMATSCRIPT`/`RAWDATAFORMATSCRIPT` 是 CLOB；只有 PK index、無 FK。StoreRawData、SendMail NOT NULL DEFAULT 0；DataStamp nullable DEFAULT 0。來源 DDL：`database/reference/mestest_mtl_autoloader_source_ddl.sql`。
- [ ] 實作 `GET /api/material-vendor-autoloader-scripts?mtlFolderPath=&vendorNo=`；兩條件皆精確相等、空白不套條件、依 Folder Path 排序，回傳全部欄位及稽核值。主畫面啟動不自動查詢。
- [ ] 實作 POST/PUT/DELETE `/api/material-vendor-autoloader-scripts`：新增/修改稽核由伺服器設定；StoreRawData 固定為 0；新增/複製的 VendorNo 保持 NULL；Edit 不提供 VendorNo 修改；FormatScript/RawDataFormatScript 使用文字/CLOB 對應型別。
- [ ] PUT/DELETE 加入主鍵及 DataStamp optimistic concurrency；支援舊 nullable `DataStamp`（NULL 與 0 不可混為必然相同），成功修改遞增版本。WinForms Service 傳 DataStamp，但目前檢視的 Core method signature/UPDATE SQL 未看到版本檢查，請核對家用端實際引用的 Industry_Base 版本；規格記有差異。
- [ ] 實作受控 `GET /api/material-vendor-autoloader-scripts/directories` 只回傳伺服器允許目錄，不可讓瀏覽器任意瀏覽 MES Server 檔案系統。供應商選擇沿用 `GET /api/material-vendors`。
- [ ] 實作 Test Item Setup `GET /api/test-recipes?recipeType=MTL_AUTOLOADER`，及 `POST /api/material-vendor-autoloader-scripts/validate`；validate 由 API 承接舊 `Industry_Base.wsSYS.ChkAutoLoaderSetup`。UI DEV 目前僅 CSV/TXT 預覽與簡化檢查；Excel/NPOI、完整欄位轉換/parser 要由 API/後續設定介面補齊。
- [ ] 驗證登入者 `B_MTL AUTOLOADER SETUP` 權限、Folder Path 400、VendorNo 20、Description 255、separators 10、欄位序號、唯一鍵、DataStamp 衝突；逐筆 Delete 應在第一個失敗時停止。此功能無 IssueState/簽核、無 FK，不新增 audit table。
- [ ] 比對並部署 schema/migration/API 後更新規格與本清單，再 push；通知公司端 pull Production API 契約。

### MTL ML104 物料群組資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料群組資料.md`、公司端 ML104 區塊及 `docs/DATABASE_CONNECTION.md`。WinForms Form/Service/Core、DEV UI 與兩張表來源已核對。
- [ ] 先比對 PostgreSQL live schema、欄位/constraints/indexes 與 migrations。`tblmtlmaterialgroupbasis`、`tblmtlmaterialgroupdetail` 已出現在既有物料 0013/first-phase reference；若 YMES schema 已有相符結構就沿用。缺主檔才用 `database/reference/mestest_mtl_material_group_create.sql`，缺明細才依 `database/reference/mestest_mtl_master_create.sql` 的 detail 定義建 forward migration；不得盲跑 `IF NOT EXISTS` 或覆蓋 YMES 擴充欄位。來源確認 `materialtype NOT NULL`、`datastamp DEFAULT 0` nullable；既有 schema 差異需用 forward migration 修正。
- [ ] 實作 `GET /api/material-groups?groupNo=`、`POST /api/material-groups`、`PUT /api/material-groups/{groupNo}?dataStamp=...`、`DELETE /api/material-groups/{groupNo}?dataStamp=...`。清單不使用 IssueState；編輯只更新 Description；新增/修改稽核由伺服器設定；DataStamp nullable 必須支援 `dataStampIsNull=true` CAS。
- [ ] 實作 `GET /api/material-groups/{groupNo}/materials` 與 `PUT /api/material-groups/{groupNo}/materials`，body `{materialNos:[...]}` 是整組取代，空陣列清空。以單一交易刪除舊明細、寫入新明細、事件記錄；刪主檔亦須同交易刪明細後刪 header。驗證新增成員存在、MaterialType 相符且 IssueState=2；既有歷史成員若後來不再核准，保持原狀時允許保存，移除後不可重新加入。
- [ ] MaterialType picker 使用 `GET /api/material-types`（不帶 IssueState，對應 WinForms `LoadMaterialType` 全清單）；左側候選物料使用 `GET /api/materials?issueState=2&materialType=` 精確條件。檢查既有 endpoint 的篩選和資料權限。
- [ ] 驗證登入者的 `MTL GROUP` 權限、GroupNo/MaterialType/Description 長度與必填、唯一鍵、DataStamp 衝突；此功能無核准 endpoint。沿用現有伺服器 event log，不建新表。
- [ ] API/schema/migration 部署後更新規格及本清單並 push；通知公司端 pull 最新版接 Production API。公司端 `/YMES/MTL/MaterialGroups` 已完成 DEV 頁。

### MTL ML107 物料標籤對應設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料標籤對應設定.md`、公司端 ML107 進度及本節。WinForms Form/Designer、Service/Core、Oracle 結構、Vue DEV 頁與 API 契約已整理。
- [ ] 以家用端 PPK/SSH 比對 YMES live schema 和 migrations。主表 `tblmtllabelmap` 若缺少，依 `database/reference/mestest_mtl_label_map_create.sql` 新增 forward migration；標籤 lookup `tblprdlabelbasis` 若缺少，才依 `database/reference/mestest_prd_label_basis_lookup_create.sql` 建立。若已存在，核對欄位、NULL/default、PK/index 並保留 YMES 擴充欄位；不可盲跑 reference SQL。
- [ ] 實作 `GET /api/material-label-maps?materialType=&materialNo=&labelNo=`，三條件以參數化 contains/LIKE 篩選，主查詢沿用舊 Core 的 `MaterialNo IS NOT NULL`（不加 LabelSource 條件），回傳物料/類別名稱、LabelSource、DataStamp、稽核欄，依 MaterialType/MaterialNo/LabelType 排序。
- [ ] 實作 `GET /api/product-labels?labelSource=5` 讀標籤目錄；LabelSource 必須精確等於 5，回傳 LabelType、LabelNo、FormatFileName、Description、PrinterNo。另沿用 `GET /api/material-types?issueState=2` 及 `GET /api/materials?issueState=2&materialType={exact}` 選擇器；物料 lookup 支援虛擬 `MaterialNo='*'`，不可要求該值存在於物料表。
- [ ] 實作 `POST /api/material-label-maps`：必填 MaterialType/MaterialNo/LabelType/LabelNo、來源固定為 5、長度符合 DDL、API 端設定 Creator/CreateDate、DataStamp 初值 0；複合 PK 衝突回 409。舊 WinForms 可手動輸入 LabelNo，沒有 label-map FK，因此不要要求手打的 LabelNo 必須存在於 label template 表。
- [ ] 實作 `PUT /api/material-label-maps/{materialType}/{materialNo}/{labelType}?dataStamp=`：只更新 LabelNo；WHERE 限定 LabelSource=5 與舊 DataStamp，成功更新 Reviser/ReviseDate 並遞增 DataStamp；nullable DataStamp 需支援 `dataStamp=0&dataStampIsNull=true` 並用 `IS NULL` CAS。版本衝突/不存在回 409/404。實體刪除依複合 UI key 加 LabelSource=5；主 Grid 多筆刪除仍逐筆執行、遇錯停止並回報部分成功。
- [ ] 所有端點檢查登入與 FunctionNo `MTL LABEL MAP BASIS` 權限；不加入 IssueState/核准流程、不新增 Oracle FK 或事件資料表。完成 schema/API 部署後更新規格與 migration index，push 並通知公司端 pull Production API。
- [ ] 公司端頁面 `/YMES/MTL/MaterialLabelMaps` 已建立 DEV UI；Production API 未部署前不得以 localStorage 充當正式資料。

### MTL ML101 物料供應商基本資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料供應商基本資料.md`、`docs/DATABASE_CONNECTION.md` 新增段落及本檔公司端進度；WinForms Form/Service、API 契約、Issue 狀態與刪除交易均已記錄。
- [ ] 依 `database/reference/mestest_mtl_material_vendor_source_ddl.sql` 與 `mestest_mtl_material_vendor_create.sql`，透過家用端 PPK/SSH 先比對 YMES schema/migrations，再建立 forward migration；不得在 Oracle 執行 CREATE SQL，也不得移除 YMES 擴充欄位。
- [ ] 實作 `/api/material-vendors` 及巢狀 `/contactors` API：搜尋/四狀態、CRUD、DataStamp CAS（NULL 版本用 `dataStampIsNull=true`）、批次逐筆刪除、已核准軟刪除、核准 Issue/Job 流程及 contactor 交易；登入及 MTL ML101 權限必須在 API 驗證。
- [ ] 主檔實體刪除須同交易刪除 `TBLMTLMATERIALVENDORCONTACTOR`、`TBLMTLMATERIALVENDORMAP` 對應資料，再刪主檔；供應商名稱查詢需依 WinForms 舊碼使用 MTL vendor 表。核對並修正 `src/YMes.Api/MasterData/Data/PostgresMasterDataRepository.MaterialFeatures.cs` 目前把 material vendor map join 到 `tbleqpvendor` 的來源錯誤。
- [ ] 對照規格確認 PostgreSQL 欄位長度、NULL/default、主鍵、API DTO 與 Vue 路徑 `/YMES/MTL/MaterialVendors`；完成後更新規格與 migration/API 部署狀態並 push，通知公司端 pull Production endpoint。

### MTL ML102 物料類別資料（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料類別資料.md`、WinForms Form/Service 規則及現有 MTL API。此功能的 `TBLMTLMATERIALTYPE`、`TBLMTLMATERIALPROPERTY`、`TBLMTLTYPEQCITEM`、`TBLMTLTYPEACCLIMATION` Oracle DDL/空表 CREATE SQL 已在 `mestest_mtl_master_*` reference，並由 0013 建置；先以 PPK/SSH 核對當前 YMES schema/migration，缺表才補 forward migration，不要重建或覆蓋擴充欄位。
- [ ] 實作 `/api/material-types` CRUD/搜尋/四狀態/核准與 DataStamp CAS；新增檢查 `MaterialType` 不可與 `tblmtlmaterialbasis.materialno` 重複；實體刪除在單一交易移除類別屬性、`tblmtltypeqcitem`、`tblmtltypeacclimation` 與主檔，已核准軟刪除為 -1，簽核中拒絕。
- [ ] Property 子功能以 Material=`MaterialType` 操作共用 `tblmtlmaterialproperty`，補 CRUD/MTL PROPERTY Issue 核准及 CAS；不要把 MaterialType 屬性和一般 MaterialNo 記錄混在同一查詢結果。
- [ ] GumHour 以 `tblmtltypeacclimation` 的 `(MaterialType, MaterialNo='*')` 讀寫類別級設定；套用 ACCMin/Max、回冰/ReForzenTimes、再次回溫欄位驗證與 CAS。核准前從系統參數取得 `GumMaterialType`、`MATERIALMULTITIMECTRL`，若屬固晶膠但未設定 GumHour 則拒絕核准；重用 `MTL TYPE` Issue/Job 流程及第一簽核群組通知。
- [ ] 新增 `POST /api/material-types/{sourceMaterialType}/copy`，以單一交易複製類別資料及來源的類別級 GumHour（若存在），重設 IssueState/audit/DataStamp；不複製 Property/QC 明細。GumHour 端點須支援 GET/POST/PUT，新增時 INSERT、更新時 CAS。
- [ ] 完成並部署後更新規格、migration/API 狀態與本待辦，push 後通知公司端 pull。公司端 `/YMES/MTL/MaterialTypes` DEV 頁已建立。

### MTL ML105 物料類別品管項目設定（公司端 2026-11-17 交接）

- [ ] 閱讀 `docs/modules/MTL/物料類別品管項目設定.md`，核對 `TBLMTLTYPEQCITEM`、`TBLMTLQCITEM` 已在 MTL 0013/schema；Oracle DDL/PG CREATE 已在 `mestest_mtl_master_*` reference，沒有新表需建立。
- [ ] 實作 `GET /api/material-types?materialType=`（不帶 IssueState 時回傳全部狀態，MaterialType 前綴搜尋）及 `GET /api/material-types/{materialType}/qc-items`，回傳三種 QC 候選目錄與已指派項目；候選需沿用 QC Attributes、Variables、Group 舊目錄服務。
- [ ] 實作 `PUT /api/material-types/{materialType}/qc-items`，以單次 transaction 取代 Type QC 範本、同步該類別所有物料的 `TBLMTLQCITEM`，依舊程式補齊 `TBLOPQCITEM` 的 `OPNo='IQC'` 項目及異動紀錄；驗證項目存在/唯一性/型別及 MTL ML105 權限。
- [ ] 完成 API 後更新規格與家用待辦、push 並通知公司端接 Production endpoint；公司端 `/YMES/MTL/MaterialTypeQCItems` DEV 頁已建立。

公司端已整理一批可直接接手的來源結構與目錄資料。開始前先 pull 這份清單所在的最新 `main`，並閱讀 `todolist_officepc.md`、`docs/DATABASE_CONNECTION.md`、`docs/MES_MENU_FUNCTION_INVENTORY.md`、OP/MTL 規格及下列 SQL。家用端只負責 DB/API/migration；公司端後續接回 Vue/WinForms 畫面。

1. **系統目錄 migration 0012 已完成**：以 SSH 比對來源 schema，部署可回滾/可重跑 migration，載入 20 Module、601 MES MenuItem、553 Function 並保留 5 筆 YMES 自訂列。模組繁中名稱已核實、API/Web 已部署，瀏覽器已確認三層選單；逐筆自動比較來源與 API 回傳仍待補。
2. **物料 API/schema 主體已完成**：已逐欄核對 8 張表並套用 0013 migration，保留 `tblmtlmaterialbasis` 的 7 個 YMES 欄位、補回 7 個 DEFAULT，建立缺少的 6 張表。Property、QC Item、Vendor Map、GumHour、JSON Import 及主檔交易 API/Vue Production 已部署；app-browser 找到並修正控制器 DI、`tblsysunitbasis`/`tblsyselementbasis` 唯讀授權及投料點 JSON 型別問題，瀏覽器已驗證主檔新增、屬性、QC、GumHour 讀寫。0014 授予必要唯讀權限。公司 Oracle 端仍需核實正式 ValueList、供應商名稱 Join、GumHour 參數來源；CSV/JSON 提交端到端、正式 Excel 範本、`.xls` 預覽及 WinForms 群組寫入規則仍待完成。
3. **完成作業站延伸 API/schema**：比對 `database/reference/mestest_operation_and_system_create.sql` 內 18 張 OP 表；此批 18 張 OP 表及 3 張系統表的原始 Oracle DDL 在 `mestest_operation_and_system_source_ddl.sql`。YMES 目前作業站僅主檔 CRUD/核准/停用已部署；按 `docs/modules/OP/作業站主檔.md` 實作子表引用、資源、商業規則、參數、QC、Error、SubOP、Import 及交易。
4. **繼續設備功能 API**：設備供應商已完成；設備狀態 GET/options/POST/PUT/DELETE API 與 0016 migration 已部署。公司端已交付兩張來源引用表 DDL、空表 CREATE SQL 與完整繁中 ValueList；家用端需新增 migration、啟用引用檢查並更新 options 標籤。工程群組成員表 TBLUSRUSERGROUP 及設備群組用的兩張 SMD 映射表 Oracle DDL/PG CREATE SQL 已於 2026-11-16 交付；先比對 YMES schema，再完成工程群組與設備群組 API。其他類別、點檢表功能依規格續做。
5. **完成全部第一層模組**：按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 的 ModuleOrder 逐個 FunctionNo/FormName/ExecutionFile，補齊 PostgreSQL 結構、API DTO/驗證/交易及部署。任何來源表缺少 DDL 時，讓公司端透過 PowerShell 直接查 MESTEST 並列出確切表名；不可猜 schema。

每完成一項，更新本檔 checkbox、對應 module spec 與 migration/API 狀態，再 commit/push；不要把連線密碼、Token、SSH 私鑰或 Oracle 連線字串放入文件或 Git。

## SMD SD104 區域基本資料（公司端 2026-11-16 已交接）

- [x] 2026-11-16 家用端比對 YMES：`TBLSMDAREABASIS` 已存在但四項來源 DEFAULT 缺漏；`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP`、`TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 缺表。已透過 `0017_smd_computer_area_api.sql` 補回 DEFAULT 並建立四張空表；未匯入 MES 業務資料。SD104 Production CRUD/layout API 仍待完成。

先讀 `docs/modules/SMD/區域基本資料.md`、`todolist_officepc.md` 的 SD104 區塊及以下 Oracle reference。公司端已有 `/YMES/SMD/Areas` Vue DEV 頁與 SMD 三層 fallback 選單；Production 不使用 localStorage 假資料。

### Schema / migration

- 新查 Oracle 主表 `TBLSMDAREABASIS`、`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP` 的 source DDL/PG 空表 SQL：`database/reference/mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`。
- 區域配置另外使用 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP`；其 Oracle DDL/CREATE SQL 在 `database/reference/mestest_equipment_group_smd_source_ddl.sql`、`mestest_equipment_group_smd_create.sql`。先比對 YMES 現況，保留已存在的 YMES 擴充，不重建重複表。
- `TBLSMDAREABASIS` 16 欄、PK AreaNo；AreaType/Class/IssueState/AreaCompose DEFAULT 0 NOT NULL；BackgroundImage BLOB；DataStamp DEFAULT 0 nullable。`TBLSMDAREAINVENTORY` 9 欄、PK `(AreaNo,InventoryNo)`；DefaultInv DEFAULT 0 NOT NULL、DataStamp DEFAULT 0 nullable。`TBLSMDAREAMAP` 6 欄、PK `(BaseAreaNo,ParentAreaNo,ChildAreaNo)`。三表只有 PK unique index，沒有 FK/其他 index。完整欄位見規格與 reference SQL。
- Oracle DATE → PostgreSQL timestamp without time zone；BLOB → bytea。WinForms 表單要求 DepartmentNo 必填，但 Oracle 欄位允許 NULL；資料庫欄位仍照 Oracle，API 做業務驗證。

### API / Service handoff

1. 建 SMD API 並檢查登入者 SMD 權限：`GET /api/smd/areas?issueState=&areaNo=`、`GET /api/smd/area-basis/options`、`POST /api/smd/areas`、`PUT/DELETE /api/smd/areas/{areaNo}`、`POST /{areaNo}/approve`、`GET/PUT /{areaNo}/layout`、`GET /{areaNo}/layout-options`。JSON camelCase DTO 與 query/body 欄位以功能規格和 `AreaBasisMaster.vue` 為準；layout PUT 回更新後完整 Area DTO/DataStamp。
2. 新增只接受有效的區域類型組合；AreaNo 長度20、AreaName 50、Description 255；編號大寫。AreaNo/AreaName/FactoryNo/Creator/CreateDate 在 DB NOT NULL；DepartmentNo DB nullable 但 UI/API required。一般區域 BaseAreaNo 必填且必須屬於 active Base Area，FactoryNo 從 Base Area 繼承；Base Area 必須選 active Factory。Factory/Department options 不得使用 DEV 假資料。
3. Create 設 IssueState=0、稽核與 DataStamp；Edit 僅 state 0，以 DataStamp compare-and-swap 更新；NULL DataStamp 也要正確 CAS，遞增 `COALESCE(DataStamp,0)+1`。Delete state 0/-1 實體刪除；2 改 -1；1 回 409。實體刪除要 transaction 檢查 `TBLSMDAREARELATION.ContainAreaNo`，若仍有 layout relation 要拒絕。多筆 UI 逐筆請求，錯誤回報完成筆數。
4. Approval 僅接受 state 0，IssueNo=`AREA BASIS`，重用 YMES Issue/Job service：無 Job/Detail 依 WinForms 規則直達 2；有設定轉 1，核准2/退回0。Production 不得照 Vue DEV 直接轉狀態。
5. Layout 必須讀舊 `frmAreaLayout.vb`、`frmNodeDef.vb` 與 `funSaveAreaData`，在 PostgreSQL 同一 transaction 處理 `TBLSMDAREARELATION`、`TBLSMDAREAMAP`、`TBLSMDAREAINVENTORY`、`TBLSMDSMTAREAEQPMAP` 及 `TBLSMDAREABASIS.BackgroundImage`。從節點推導 parent/child area map、top production line、inventory mapping、equipment/equipment group mapping，驗證 ObjectType、唯一鍵、節點尺寸/座標/顏色與 BaseArea 關聯。保存 BaseArea DataStamp 並回傳更新後完整 DTO，前端下一次操作才不會拿舊 stamp。
6. WinForms AddFlow 支援多選/群組、Node Define、完整線型巢狀推導；公司端第一版簡化成可拖曳/數值調整節點與背景圖，尚未聲稱完整 AddFlow 等價。需按來源 Service 完成 API 邏輯並記錄無法逐一等價的部分。`ymes_app` 只授予所需 SELECT/INSERT/UPDATE/DELETE。
7. 建 migration 前逐欄比對 PostgreSQL schema；缺表才新增。完成後部署 API、更新 migration/API 文件，再通知公司端 pull 接正式 API 驗收。公司端 DEV 還未執行 build/test/browser。

## SMD SD105 區域庫房基本設定（公司端 2026-11-16 已交接）

- [x] `TBLSMDAREAINVENTORY` 已由 `0017_smd_computer_area_api.sql` 建立空表，欄位長度/複合 PK 依來源 DDL；SD105 CRUD API 尚待完成。

先讀 `docs/modules/SMD/區域庫房基本設定.md` 及上方 SD104 handoff。公司端已有 `/YMES/SMD/AreaInventories` Vue DEV 頁，FunctionNo=`AREA INV MAP`；沒有 IssueState/核准。

- Oracle `TBLSMDAREAINVENTORY` 已由公司端 SD104 直連查 DDL，來源 logical DDL/PG 空表 CREATE 已包含在 `database/reference/mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`。不要重複建表；先比對 YMES schema/migrations，缺表才新增。
- Table PK 是 `(AreaNo,InventoryNo)`，不是 `(BaseAreaNo,AreaNo,InventoryNo)`；BaseAreaNo 是額外範圍條件。沒有 Oracle FK/其他 index。DefaultINV DEFAULT 0 NOT NULL；DataStamp DEFAULT 0 nullable。完整欄位見來源 SQL。
- 延伸 SMD API：`GET /api/smd/area-inventories?baseAreaNo=`、`POST /api/smd/area-inventories`、`PUT /{areaNo}/{inventoryNo}?dataStamp=`、`DELETE /{areaNo}/{inventoryNo}?baseAreaNo=&dataStamp=`。Base Area 選項沿用 `GET /api/smd/area-basis/options`；Area/Inventory picker 使用 `GET /api/smd/areas/{baseAreaNo}/layout` 中 ObjectType 0/1 nodes。
- Create 必填 BaseAreaNo/AreaNo/InventoryNo；DefaultINV 僅 0/1；伺服器產生 Creator/CreateDate/DataStamp。Edit 不改 key，只改 DefaultINV，更新 Reviser/ReviseDate 並做 DataStamp CAS（支援 NULL）；Delete 交易內比較 DataStamp 後移除。
- Default=Yes 每一 Area 只能一筆。舊 Add 檢查 BaseAreaNo+AreaNo；舊 Edit 的 `GetAreaInventory` 未帶 BaseAreaNo，可能跨 BaseArea 影響相同 AreaNo。規格已記錄來源差異；實作時保留觀察行為，或在 API change log 明確列出核准的修正。
- wsSMD Add/Edit 呼叫傳 Creator/CreateDate/DataStamp/Reviser/ReviseDate，但本 checkout 的 clsSMDBasis 方法簽章沒有這些命名參數。新 API 需明確由伺服器產生稽核/CAS，不要假設舊 Core SQL 已寫入這些欄位。所有 API 檢查登入與 SMD 權限，`ymes_app` 只授最小 CRUD。
- DDL 已從 2026-11-16 直接 metadata query 交付，不必重查 Oracle。schema/API/部署完成後通知公司端 pull 驗收。

## SMD SD106 過帳電腦之區域（公司端 2026-11-16 已交接）

- [x] 2026-11-16 遠端 schema 核對：兩張主明細表已存在且欄位、NULL、型態、PK 符合來源；僅 `DataStamp DEFAULT 0` 漏失。`0017_smd_computer_area_api.sql` 已補 DEFAULT、建立必要 SMD 空表，並授予 `ymes_app` 對 SD106 主明細 CRUD 與 Factory/Area lookup SELECT。備份為 `/opt/ymes/backups/ymes_before_0017_smd_20261116.dump`，mode 600，`pg_restore --list` 驗證可讀。
- [x] SD106 API（查詢、Create、Edit CAS/整組替換明細、Delete、Copy、已簽核工廠與區域 options）已部署至遠端獨立 API 容器；.NET 9 build 成功，`/health/ready` 回 200，未登入 API 路由回 401。登入後 app-browser 讀取與 Production UI 驗收待完成。
- [ ] SMD API 目前僅要求登入；依使用者指示，角色／功能權限暫不實作。PostgreSQL `ymes_app` 的表級權限另按每個端點實際需要最小授權。

先讀 `docs/modules/SMD/過帳電腦之區域.md`。公司端已完成 `/YMES/SMD/ComputerAreas` Vue DEV 頁；Production 僅呼叫 API。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_smd_computer_area_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_smd_computer_area_create.sql`。來源是公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀查詢，不需再問 Oracle 連線資訊，也不要重查。
- `TBLSMDCOMPUTERAREABASIS` 欄位：ComputerNo varchar(20) NOT NULL、FactoryNo varchar(20) NOT NULL、Creator varchar(10)、CreateDate timestamp、Reviser varchar(10)、ReviseDate timestamp、DataStamp numeric(11,0) DEFAULT 0 nullable；PK `(ComputerNo,FactoryNo)`。
- `TBLSMDCOMPUTERAREADETAIL` 欄位 ComputerNo/FactoryNo/AreaNo varchar(20) 均 NOT NULL；PK `(ComputerNo,FactoryNo,AreaNo)`。兩表只有 PK unique index，沒有 Oracle FK/額外 index。不要新增來源不存在的 FK/index。
- 先逐欄比對 YMES schema、migration 及現有 API；表可能已在 first-phase schema，缺表/欄位才新增 forward migration，保留 YMES 擴充欄位。first-phase reference 已按來源補 ComputerArea DataStamp DEFAULT 0；不要把空表 SQL 當作可直接上線 migration。

### API / 業務規則

1. SMD API：`GET /api/smd/computer-areas?computerNo=&factoryNo=` 回傳主檔及 `areas: string[]`；`POST /api/smd/computer-areas` 新增主檔與區域明細；`PUT /api/smd/computer-areas/{computerNo}/{factoryNo}?dataStamp=` 依 CAS 更新稽核並整組替換明細；`DELETE /{computerNo}/{factoryNo}` 同交易先刪明細再刪主檔；`POST /computer-areas/copy` body `{fromComputerNo,fromFactoryNo,toComputerNo}` 以複合鍵複製。
2. 新增/修改至少選一個 Area。Factory 必須為 IssueState=2；Area 必須屬於該 Factory 且 IssueState=2、AreaClass=0。選項可沿用 `GET /api/smd/area-basis/options` 及 `GET /api/smd/areas?factoryNo=&issueState=2&areaClass=0`。其中 `factories` 選項需含 `factoryNo, description`（並保留 SD104 使用的 `factoryName`）；WinForms 查找視窗顯示 FactoryNo/Description。所有端點驗證登入與 SMD 權限及欄位長度。
3. Add/Copy 由 API 設 Creator/CreateDate；Edit 設 Reviser/ReviseDate/DataStamp。DataStamp nullable/default 0，CAS 要處理 NULL 並以 `COALESCE(datastamp,0)+1` 遞增。主檔與全部明細新增/修改/刪除/複製都要 transaction。
4. 舊 Core 的 CopyComputerArea 只按 FromComputerNo 查來源，未指定 FactoryNo；舊資料表是複合 PK。新 API 以 `(fromComputerNo,fromFactoryNo)` 明確鎖定來源，拒絕重複目標鍵。
5. wsSMD Edit 傳入 DataStamp，但本 checkout 的 `clsSMDBasis.EditComputerArea` 簽章沒有此參數，舊 Core SQL 沒有 CAS。新 API 補足 CAS/audit，不照搬舊缺漏。UI Delete 多選逐筆請求，首錯停止並回報已完成筆數。
6. DDL 與 API 規格已交付；家用端完成 schema/migration/API 並部署後通知公司端 pull，對接 Production 驗收。

## SMD SD101 工廠基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/SMD/工廠基本資料.md` 及公司端 SD101 區塊。公司端已完成 `/YMES/SMD/Factories` Vue DEV 頁，FunctionNo=`FACTORY BASIS`；Production 只呼叫 API。

- Oracle `TBLSMDFACTORYBASIS` 已由公司端在 2026-11-16 EP303 DDL 查詢中直連查得。Oracle source logical DDL、PostgreSQL 空表 CREATE SQL 已在 `database/reference/mestest_carrier_basis_dependencies_source_ddl.sql`、`mestest_carrier_basis_dependencies_create.sql`，first-phase bootstrap 已含表；不要重查或建立重複 CREATE SQL。
- 欄位：FactoryNo varchar(20) NOT NULL PK、Description varchar(255)、Creator varchar(10) NOT NULL、CreateDate timestamp NOT NULL、IssueState numeric(1,0) DEFAULT 0 NOT NULL、FabCode varchar(10)、FactoryName varchar(50)、Reviser varchar(10)、ReviseDate timestamp、DataStamp numeric(11,0) DEFAULT 0 nullable。只有 PK unique index，沒有 Oracle FK/額外 index。
- 先比對 YMES schema/migrations。缺表或欄位才建 forward migration；保留 YMES 擴充欄位。DATE 對應 timestamp without time zone。
- API：`GET /api/smd/factory-basis?factoryNo=`（大小寫不敏感包含查詢、空值回全部）、`GET /api/smd/factory-basis/options` 回 `{factoryNameEnabled}`、POST `/api/smd/factory-basis`、PUT `/{factoryNo}?issueState=0&dataStamp=`、DELETE `/{factoryNo}?issueState=&dataStamp=`、POST `/{factoryNo}/approve` body `{issueState:0,dataStamp}`。DTO/回應依規格與 `FactoryBasisMaster.vue`。
- CRUD 權限需登入與 SMD 權限；FactoryNo 必填唯一。只允許 IssueState=0 修改；DataStamp nullable 要 CAS 並以 COALESCE(DataStamp,0)+1 更新。伺服器產生 Creator/CreateDate/Reviser/ReviseDate。Delete 僅狀態 0、2、-1 可實體刪除，狀態 1 回 409。
- Approve IssueNo=`FACTORY BASIS`：沒有 Job/Detail 時依設定直達 2；有工作流程時建立 Issue Master 並轉 1，之後核准 2/退回 0。Production 不得使用 DEV 的直接轉狀態邏輯；家用端需重用正式 Issue/Job API。
- `options.factoryNameEnabled` 依正式 `IC_FT_PKG` 設定；主 Grid 只有啟用時顯示 FactoryName，定義表單仍有欄位。舊 WinForms 清空 FactoryName 不會清除資料，Vue 延續此語意。
- 舊 ws/Core 有不同版本的 DataStamp/audit 方法簽章；實作以規格明確定義的 CAS/登入稽核為準，不沿用舊簽章差異。
- schema/API/Issue 流程完成並部署後，更新本待辦及 API 文件，通知公司端 pull 對接 Production。

## SMD SD103 儲位基本資料（公司端 2026-11-16 已交接）

先讀 docs/modules/SMD/儲位基本資料.md。公司端完成 /YMES/SMD/Locators Vue DEV 頁與 FunctionNo=LOCATOR BASIS。Oracle 原始 DDL 和 PostgreSQL 空表 CREATE SQL 為 database/reference/mestest_smd_locator_basis_source_ddl.sql、mestest_smd_locator_basis_create.sql；first-phase bootstrap 已校正。

- Oracle table 為 TBLINVLOCATORBASIS，複合 PK (InventoryNo, LocatorNo)，無 FK/額外索引。欄位、NULL/default 以 CREATE SQL 為準；IssueState DEFAULT 0 NOT NULL，Capacity NUMBER(12,4) DEFAULT 0 nullable，DataStamp NUMBER(11,0) DEFAULT 0 nullable。
- 先查目前 YMES schema/migration 狀態。舊 first-phase SQL 已在家用端執行時，新增 forward migration 修正 IssueState、Capacity、DataStamp 預設值；不刪資料、不重建表、不加 Oracle 沒有的外鍵。
- API：GET /api/smd/locator-basis?inventoryNo= 以 InventoryNo 完全相等比對，回傳四狀態並按複合鍵排序；GET /api/smd/locator-basis/options 回 { approvedInventories: [{ inventoryNo }] }，由既有 TBLINVInventoryBasis 篩 IssueState=2、distinct InventoryNo。POST /api/smd/locator-basis；PUT /api/smd/locator-basis/item?inventoryNo=&locatorNo=&issueState=0&dataStamp=；DELETE 同路徑並帶實際狀態/DataStamp；POST /api/smd/locator-basis/item/approve?inventoryNo=&locatorNo=&issueState=0&dataStamp=。
- POST 設 Creator/CreateDate、IssueState=0。PUT UI 只允許狀態 0，鍵不可改；空白 LocatorName/Capacity/Description 在舊 WinForms 代表不送欄位，故 Edit 不清除舊值。Capacity 必須 >=0，最大 NUMBER(12,4) 範圍。
- DELETE 狀態 0/-1 實體刪除、狀態 2 轉 -1 並更新 Reviser/ReviseDate/DataStamp、狀態 1 回衝突。Approve IssueNo=INV LOCATOR；無 Job/Detail 時直達 2，有流程時先 1，後續核准 2/退回 0。多筆 UI 逐筆請求，第一個失敗即停止。
- 所有 API 驗證登入及 SMD 權限；DataStamp nullable 必須 CAS，以 COALESCE(datastamp,0)+1 遞增。若家用端已擴充 YMES 欄位，保留擴充內容。
- 公司端 Production 頁只呼叫 API；家用端 schema/API 完成並部署後通知公司端 pull 對接。沒有把 DEV mock 當成正式流程。

## 公司電腦目前交付

- Vue 主選單以三層 Module → Menu Group → Function 呈現；正式資料 API 不得把設定分類與主檔功能壓成兩層。
- UI 使用登入 Cookie 呼叫 `GET /YMES/API/api/navigation/menu-tree`。
- 2026-11-14 已套用 0012 migration，YMES 正式目錄匯入 MES 來源 20 個 Module、601 筆 MenuItem、553 筆 Function，保留 5 筆 YMES 既有分類列；全量模組中文名稱 API/Web 已部署，app-browser 已確認三層選單可載入。完整來源目錄見 docs/MES_MENU_FUNCTION_INVENTORY.md。
- 每個選單項目都有對應圖示；側欄收合時僅顯示圖示。
- 選單中尚未遷移的 FunctionNo 會明確標示，避免使用者誤以為功能已可操作。
- Vue 開發用設備設定選單已依 WinForm 補列七個項目；設備設定的 Oracle MenuNo/FunctionNo 已可由完整來源目錄核對，尚待逐功能整合至正式 API。供應商及工程群組 Vue 頁使用 Development 假資料，功能規格入口：docs/MODULE_REPLICATION_SPEC_INDEX.md。
- 公司電腦先以 Development 本地假資料完成 WinForms UI 與互動，不呼叫假 API；假資料頁會顯示提示並存於瀏覽器 localStorage。Production 僅呼叫正式 API，不能啟用假資料。

## MES 資料庫 CREATE SQL

- 已附實際 MES Oracle `MESTEST` 資料字典轉出的 PostgreSQL 建表 SQL：[`database/reference/mestest_first_phase_create_tables.sql`](database/reference/mestest_first_phase_create_tables.sql)。
- 這份 SQL 包含 75 張第一階段主檔相關表，以及 TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 三張系統選單/功能目錄表；目標 schema 為 YMES，不包含任何 MES 業務資料。
- 三張系統選單/功能目錄表的 MESTEST 原始 DDL 已查；Oracle 目錄清單、ModuleOrder、MenuItem、FunctionNo、FormName、ExecutionFile 見 docs/MES_MENU_FUNCTION_INVENTORY.md。2026-11-14 已透過 0012 migration 載入完整目錄；YMES 另保留 5 筆不屬於 Oracle source seed 的舊選單列。
- 本次另附作業站與系統選單來源 DDL/空表 CREATE SQL（18 張作業站相關表 + 3 張系統表），以及物料 8 張來源 DDL/空表 CREATE SQL，見 database/reference/mestest_operation_and_system_*、mestest_mtl_master_*。
- 另附即時 Oracle MESTEST DDL 轉換的供應商表 SQL：[設備供應商 CREATE SQL](database/reference/mestest_equipment_vendor_create.sql)，只含 tbleqpvendor、tbleqpvendorcontactor 結構，沒有業務資料。這兩表不在既有 75 表 SQL 內。
- [x] 2026-11-12 以家用電腦的 PPK SSH 連線，在遠端 YMES PostgreSQL 以單一交易執行此 SQL。確認原有 75 張主檔表已存在，這次建立 `tblsysmodule`、`tblsysmenuitem`、`tblsysfunction` 三張表；YMES schema 共 80 張表（78 張 MES `tbl*` 表及 2 張登入支援表）。
- 本次沒有匯入 MES 業務資料；`tblusruserbasis.password` 維持 `varchar(255)`，以保留現有 PBKDF2 雜湊，不縮回來源 DDL 的 `varchar(50)`。
- 此 SQL 尚未由自動 migration runner 管理。全新資料庫需先執行本檔，再依序執行 `database/migrations/0001` 至 `0004`。

## 優先一：選單、功能目錄與權限 API

- [x] 已依目前 MES Oracle MESTEST 原生 DDL 完成三張系統表的 PostgreSQL 結構轉換，保留欄位名稱、預設值、NULL 設定與主鍵。
- [x] 建立五個既有主檔 module 和功能項，分別對應 EQP/OP/MTL/PRS/PRD；前兩項由 `0008_initial_navigation_seed.sql` 建立，後三項由 `0009_expand_initial_navigation.sql` 增補並已套用遠端。
- [x] 公司端已由 PowerShell 5.1 直接查 MESTEST 系統目錄：20 Module、601 MenuItem、553 Function；518/519 個 MenuType=0 項目有相符 FunctionNo。完整清單與排序交付於 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司端另產生 `database/reference/mestest_navigation_catalog_seed.sql`，包含 20 Module、601 MenuItem、553 Function 的來源設定 upsert；不含業務資料、不刪除 YMES 既有列。家用端依實際 schema 與五個既有 Vue 路由整合為 0012 migration，另附 rollback。
- [x] 2026-11-14 以 SSH 核對 YMES 遠端實際為 5 Module/10 MenuItem/5 Function；三張表欄位與來源 DDL 相符。migration 前建立並驗證備份 `/opt/ymes/backups/ymes_before_0012_full_navigation_20261114.dump`。
- [x] 0012 migration 可重跑；在交易中鎖定三張目錄表、快照將被覆寫的來源列、記錄由 migration 新增的來源鍵，保留來源外 YMES 列。`0012_full_navigation_catalog_rollback.sql` 可還原 migration 前列並移除 migration 新增列。
- [x] 2026-11-14 以 PostgreSQL 管理者 SSH 套用 0012；目前 `tblsysmodule=20`、`tblsysmenuitem=606`（MES 來源 601 + 保留的 YMES 既有 5）、`tblsysfunction=553`；來源 MenuType=0/1 分別為 519/82，另有 5 筆保留的 YMES 分類列。
- [x] 2026-11-14 以 SSH 核對物料 8 張來源表：現有主表及群組明細相符，缺少其餘 6 張。先備份 `/opt/ymes/backups/ymes_before_0013_mtl_20261114.dump`，再套用 `0013_material_master_schema.sql`。驗證 8 張來源表齊全；主表 7 個預設值均為 0，原 7 個 YMES 擴充欄位保留。
- [x] 公司端已從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實全部 20 個 ModuleNo + Module 繁中顯示名稱；完整對照見 docs/MES_MENU_FUNCTION_INVENTORY.md。TBLSYSMODULE 本身沒有 ModuleName 欄位。
- [ ] 依來源 FunctionNo 對照完整 FormName/ExecutionFile；逐個遷移後才把對應功能導向 Vue/API，未遷移項目保留「尚未遷移」提示。
- [x] 核對 519 個來源 MenuType=0 項目與同 ModuleNo 的 FunctionNo：518 筆相符、1 筆無對應 Function（`PRD / PD_BatchModifyArea`，來源名稱 `XX_PRDBatchModifyArea_THEIL`）。API 保留此來源識別，UI 以尚未遷移狀態呈現；5 筆 YMES 舊分類沒有來源子項，不會出現在 menu-tree。
- [x] 三張 Oracle DDL 沒有 MenuItem→Function 外鍵；目錄保留 `ActiveModule`、`PrivOption`、`IssueOption`、`ExecutionMode` 等來源值。依使用者指示，menu-tree 目前只要求登入、不套用使用者/角色篩選；migration 不修改既有 `weburl`、`privoption`、`issueoption`，不新增/修改角色或登入資料。
- [x] `tblSYSModule` 沒有 `ModuleName` 欄位；API 依 MES 資源檔對應 20 個 ModuleNo 的繁中名稱，完整 mapping 位於 `NavigationRepository`。
- [x] 新增可重複執行的 `0008_initial_navigation_seed.sql` 與 `0009_expand_initial_navigation.sql`；正式選單包含五個既有主檔頁，0009 已套用 YMES 遠端資料庫。
- [x] 實作登入 Cookie 保護的 `GET /api/navigation/menu-tree`，讀取三張 YMES 系統表並回傳 `modules → menuGroups → items` 結構；目前依使用者指示先不套用角色/使用者權限篩選。
- [x] 新增並套用 `0005_navigation_read_access.sql`，僅授予 `ymes_app` 對三張選單表的 `SELECT` 權限。
- [x] API 以 MenuNo 對應 FunctionNo；五個既有主檔選單項均以 MenuNo 對應 FunctionNo。
- [ ] 以 menu-tree API 逐筆核對各 ModuleNo、MenuNo、FunctionNo、順序與原 MES 完全一致；瀏覽器已確認 20 個模組與三層樹可載入，API 目錄筆數待自動比對。ModuleName 顯示使用已核實的完整翻譯表。
- [x] 2026-11-14 以 app-browser 逐一開啟五個既有 Vue 路由並確認頁面/API 無錯誤：`EQP BASIS`、`OP BASIS`、`MTL BASIS`、`PRS BASIS`、`PRD BASIS`；其他功能依舊顯示尚未遷移。

## 優先二：五個主檔的正式資料庫與 API

正式環境主檔 API 由 PostgreSQL provider 提供，Web 只透過 `/YMES/API` 呼叫。五個主檔 API 已覆蓋目前 Vue 頁面提供的 CRUD、核准/停用與產品流程/BOM 操作；舊系統擴充子表、批次匯入及完整 WinForms 業務規則仍需分批遷移。

- [x] 設備主檔 PostgreSQL repository 已部署：設備/Chamber CRUD、核准、停用、DataStamp 衝突控制；欄位依正式 DDL 對齊，移除不存在的「預設伺服器」輸入。
- [x] `0006_equipment_data_access.sql` 已套用，僅授予 `ymes_app` 對設備表的 CRUD 權限。遠端設備表目前沒有業務資料。
- [x] 作業站 CRUD、核准、停用 API 已部署，對應遠端實際存在的 `YMES.tblopbasis`；使用者先前提到 `tblopno`，但遠端 YMES schema 及提供的 CREATE SQL 都沒有此表。SPC 數量門檻欄位不在 `tblopbasis`，已從作業站頁移除，未新增資料庫欄位。
- [x] `0007_operation_data_access.sql` 已授予 `ymes_app` 作業站表 CRUD 權限；遠端作業站表目前沒有業務資料。

- [x] 設備供應商 API/schema：2026-11-14 依 MESTEST DDL 套用 `0015_equipment_vendor_schema.sql`，migration 前備份 `/opt/ymes/backups/ymes_before_0015_equipment_vendor_20261114.dump`；供應商與聯絡人 CRUD、核准、DataStamp、狀態及引用限制 API 已部署。app-browser 驗證新增/修改供應商與聯絡人、重複編號拒絕及 `ES0001` 預設編號；修正新增後清單未更新，並清除測試資料。
- [ ] 設備：`tbleqpequipmentbasis`、Chamber 關係、設備群/工程群組、設備屬性、參數、QC List；支援清單 CRUD、核准/停用、複製、匯入。
- [ ] 設備群組與 SMD：比對 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 及設備群組 YMES 結構；缺表才建 migration。依規格在新增/移除設備明細時交易同步 SMD 映射；群組刪除保留舊 Service 僅刪明細與主檔、不清 SMD 映射的行為。
- [ ] 設備工程群組：完成 tbleqpengineergroupbasis API CRUD、DataStamp 衝突檢查、複製與成員清單 API；先比對 `ymes.tblusrusergroup`，缺表才依來源 CREATE SQL 建 migration。成員 API 僅操作 GroupType=2、AgentOf=NA 的直接成員；規格：docs/modules/EQP/設備設定/設備工程群組資料.md。
- [x] 設備狀態設定：Production API 與 `0016_equipment_state_api.sql` 已部署；核對 8 欄 YMES schema、恢復來源 DEFAULT，授予 app role 對主檔 CRUD；migration 前備份並驗證 `/opt/ymes/backups/ymes_before_0016_equipment_state_20261114_admin.dump`。app-browser 驗證 Production 清單/options、Create、Apply Update 與 HEX 色碼保留，測試列已清除；修正數字欄位驗證轉型錯誤。DELETE 缺少兩張 MES 引用表時回 503 fail-closed，待公司端來源 DDL 後驗證完整刪除流程。Vue 路由對應來源 `FunctionNo=EQP STATE`（FunctionKey `EP106`）。
- [x] 設備狀態設定來源交付（2026-11-16）：公司端以 PowerShell 5.1 直接查 MESTEST，已確認 `TBLEQPSTATECONTROL`、`TBLEMSEQUIPMENTSTATE` 物件、欄位、NOT NULL、PK、唯一索引；兩張表無額外索引/FK。空表 PG CREATE SQL、Oracle 原始 DDL 在 `database/reference/mestest_equipment_state_references_create.sql` 與 `mestest_equipment_state_references_source_ddl.sql`。`TBLSYSVALUELIST` 正式值/繁中翻譯：StateType 0 系統、1 使用者定義；UtilizationType 0 非稼動、1 稼動。詳見 `docs/modules/EQP/設備設定/設備狀態設定.md`。
- [ ] 設備狀態設定：核對 YMES 實際 schema 後新增引用表 migration 及最小 SELECT 權限；部署後 DELETE 需精確檢查 `tblemsequipmentstate.equipmentstate`、`tbleqpstatecontrol.equipmentstate`、`tbleqpqclistbasis.unvalidaction`，引用回 409、未引用才 204；API options 保持已確認數值，Vue 依資源翻譯顯示繁中，並驗證 Production。
- [x] 公司端於 2026-11-16 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接查得 MESTEST.TBLUSRUSERGROUP 原始 DDL；6 欄、PK (UserNo, GroupNo, GroupType, AgentOf)、GroupType DEFAULT 0、AgentOf 由 PK 強制 NOT NULL，僅 PK unique index，無其他 index/FK。Oracle DDL 與 PostgreSQL 空表 CREATE SQL 見 `database/reference/mestest_equipment_engineer_group_member_source_ddl.sql`、`mestest_equipment_engineer_group_member_create.sql`。
- [ ] 成員 GET 回傳可配置使用者及 selected；PUT 以交易方式取代指定 EngineerGroupNo 的 GroupType=2 集合，只寫入 AgentOf='NA' 的直接成員；刪除群組時與刪除成員關聯同交易。
- [ ] 作業站延伸資料：群組、屬性、參數、QC、Error/Abnormal、Resource/Area、Business Rule、SubOP；支援匯入。
- [x] 物料頁面 API：使用 `tblmtlmaterialbasis`，支援清單、明細、新增、修改、核准、停用/刪除與資料版本衝突控制；引用物料群組或產品 BOM 時禁止物理刪除。
- [x] 流程頁面 API：使用 `tblprsprocessbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除及流程圖讀寫/驗證。因來源 Node 型態代碼尚未釐清，流程圖儲存在 YMES 自有 `ym_prsprocess_node`、`ym_prsprocess_edge`。
- [x] 產品頁面 API：使用 `tblprdproductbasis`，支援清單、明細、新增、修改、核准、版本複製、目前版本切換、停用/刪除、產品流程指派及 BOM 讀寫。來源 DDL 沒有這些 UI 關聯表，YMES 自有 `ym_product_process`、`ym_product_bom` 保存新增資料。
- [x] 物料表結構已比對及套用；Production API 已部署屬性、QC Item、Vendor Map、GumHour、CSV 匯入、核准檢查、QC 類別範本複製與子資料交易清理。2026-11-14 app-browser 驗證物料清單/新增、屬性新增與讀回、QC Item 儲存、GumHour 儲存；QA 物料及子資料已清除。
- [ ] 待公司 Oracle 端交付正式 `CountWay`/`CheckLotNo` ValueList、供應商名稱 Join、GumHour 參數來源；Production 匯入逐列瀏覽器測試、正式 Excel 範本、`.xls` 解析及 WinForms 群組寫入語意仍未完成。
- [ ] **物料 Excel 匯入 API 接手**：先讀 `docs/modules/MTL/物料資料匯入.md`。公司 Vue 已讀 CSV/`.xlsx` 第一工作表、依 9 欄順序解析並按物料編號/名稱分組；Production 範本與 legacy `.xls` 仍需 API。
- [ ] 提供 `GET /api/materials/import/template`，回傳正式模板位元組、檔名與原始副檔名；先查 YMES 是否有舊模板附件，若沒有再用相同欄序產生標準 `.xlsx`。
- [ ] 提供 `POST /api/materials/import/preview` multipart `file`，用伺服器 NPOI/相容套件解析 `.xls` 第一工作表，回傳正規化 `rows`（MaterialType、MaterialNo、MaterialName、PutInPlace、CountWay、CheckLotNo、UnitType、UnitNo、ERPUnitNo）；限制檔案大小/列數，並回覆欄位錯誤。
- [ ] 核對外部 `DataImporter.MaterialDataImport.MaterialDataChecker` 的多列/重複 MaterialNo 語意與交易邊界。現有 `POST /api/materials/import` 每筆呼叫 CreateMaterial，不能直接視為 WinForms 每個 `(MaterialNo, MaterialName)` 群組呼叫一次 checker 的等價行為；按規格調整群組 payload 或回傳逐列結果。
- [ ] 核實 `ERPUnitNo` 在舊 checker 的驗證/儲存用途；目前 `MaterialImportRowRequest` 未接收此欄位，不能默默丟棄。若正式匯入需要存資料，先從公司端取得欄位 DDL 再補 CREATE SQL/migration；若只用於驗證，記錄其去向。
- [ ] Production 匯入 API 目前同步、無法取消；若要求完整保留 WinForms 背景進度/取消，提供工作狀態/取消 API 並確保取消代表尚未執行列不再寫入。若先簡化，Production UI 應顯示伺服器處理中，不顯示可取消按鈕。
- [ ] 遷移流程 APS 等擴充欄位/關聯及匯入功能，並與舊系統 Node 編碼及 XML 規則比對。
- [ ] 遷移產品 OP、屬性及其他舊系統關聯；跨表商業規則仍需對照 WinForms/Service 實作。
- [ ] 所有新增/修改/核准/刪除端點均驗證 IssueState、DataStamp、引用限制與舊系統業務規則；正式環境不得使用 Fake provider。
- [ ] 把五份主檔及產品流程整合的舊表欄位、PK/UK、型態、索引與正式 DDL 對齊；目前精簡資料字典中的推測欄位不可直接當正式 schema。

## 優先三：其餘第一層 Module 與功能

- [ ] 以正式 `tblSYSModule`/`tblSYSFunction` 選單清單作為盤點基準，為每個 FunctionNo 對照舊 WinForms FormName/ExecutionFile、WebService、核心類別、查詢/寫入資料表及權限。
- [ ] 依第一層 Module 分批新增 PostgreSQL schema/migration、Contracts DTO、API、交易/驗證與 Vue 功能頁；公司電腦先以假資料完成每個功能頁的查詢條件、欄位、按鈕、表單與互動；同批交付給家用電腦的 API 契約與依 Oracle 核實的 CREATE SQL 再由家用電腦完成正式 API/schema。
- [ ] 每一批完成後更新遷移清單：ModuleNo、FunctionNo、WinForms Form、API 路徑、Table、UI 狀態（未開始/進行中/完成）及驗收結果。
- [ ] 未完成的功能維持明確的「尚未遷移」狀態；沒有 API 支援的功能不得以靜態假資料回報成功。

## 優先四：整合與部署

- [x] Web/API 分開執行於同一台 Server：Web `/YMES`、API `/YMES/API`。同步修正遠端 Nginx 過期設定，主檔頁不再送到舊 18090 MVC 容器，改由目前 Vue Web 18089 提供。
- [x] Production 已使用 PostgreSQL provider；Development Fake 僅供本機預覽。
- [ ] 完成資料 migration/匯入、備份還原、錯誤紀錄與部署設定；憑證、連線字串、`.env.local`、私鑰不得提交 Git。
- [ ] 部署前確認 API 權限、Cookie、HTTPS、CORS/同源反向代理與正式環境設定。

## 驗收完成條件

- 登入後可從 API 取得與舊 WinForms 一致的全部第一層 Module 及三層選單名稱/順序/權限。
- 每個 FunctionNo 都有明確的遷移狀態；標記完成的項目有對應 Vue 頁面、API 與正式資料表，不使用假成功流程。
- 生產資料操作經 API 完成；Web UI 不直接連資料庫；Production 不使用記憶體 Fake provider。
- 主要 WinForms 業務規則、狀態轉換、跨表交易及引用限制已由 API 實作並記錄。

## 2026-11-12 遠端瀏覽器檢查

- [x] `https://yakuyama.com/YMES/` 登入正常；正式區主選單預設折疊。
- [x] 從選單進入設備主檔及作業站主檔，兩頁查詢 API 均回應正常；目前資料表為空，顯示 0 筆。
- [x] 2026-11-12 已套用 migration 0010、0011，重建 YMES API/Web，正式選單資料回傳三層：模組 → 設定 → 主檔功能。
- [x] 瀏覽器確認五個模組與對應主檔頁均能載入；設備、作業站、物料、流程、產品查詢 API 均回應正常，目前各表 0 筆。
- [x] 側欄收合時只顯示選單圖示；側欄展開後仍是三層結構，模組預設收合。
- [x] 首次線上檢查發現設備路由重複映射而回 500；移除重複映射後重建 API，設備頁重查正常。修正已推送至 GitHub `main`，commit `a080a3b`。
- [x] 五個主檔 Production PostgreSQL API 已部署；流程圖與產品流程/BOM 關聯使用 YMES 自有延伸表。
- [x] 設備與作業站新增表單空白送出都會擋下必填欄位。
- [ ] 正式區沒有業務資料可供修改、核准或刪除；本次未寫入測試資料，因此上述寫入流程尚未做資料庫端到端驗收。
- [x] API readiness、Web route 和遠端容器 health check 均回應正常；Nginx 已由過期的 18090 MVC 路由改為 Vue Web 18089。

## 設備設定：設備類別資料（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備類別資料.md、docs/DATABASE_CONNECTION.md 與下方 CREATE SQL，再開始 API/schema。連線憑證只從家用電腦既有安全設定取得，不寫入 Markdown/Git。

- [ ] 先查 YMES ymes schema 是否已存在下列五表；逐欄比對既有結構，未存在才執行 database/reference/mestest_equipment_type_create.sql：tbleqpequipmenttype、tbleqpequipmenttypeacctype、tbleqpequipmenttype_picture、tbleqpequipmentproperty、tbleqpconsumebasis。
- [ ] 本檔是公司電腦從 MES Oracle MESTEST 原始 DDL 轉換的 PostgreSQL 空表 CREATE SQL，沒有匯入業務資料。IF NOT EXISTS 不會修正已存在表的錯誤欄位。
- [x] 公司端已從 WinForms/Service 確認配件選項來源為 MESTEST.TBLEQPACCESSORYTYPE，並以 PowerShell 5.1 + ODP.NET 取得原始 DDL、9 欄 nullable/default、PK/索引/FK；來源與空表 PostgreSQL CREATE SQL 已交付。
- [ ] 家用電腦先比對 ymes.tbleqpaccessorytype；缺表才建 migration。ACC Type options 依舊 Service 篩 IssueState=2、AccessoryType 非空提供。
- [ ] 核實 tblsyselementbasis 既有 YMES 結構與 MES Oracle DDL；目前 mestest_first_phase_create_tables.sql 中部分欄位有「推測」註記。依 WinForms ValueList 確認屬性選項與 PropertyType=1 查詢規則後，才實作 /api/system/elements?elementType=1。
- [ ] EP103 Vue 工程群組選擇器依賴共用 `GET /api/equipment/engineer-groups`，回傳 `engineerGroupNo`、`engineerGroupName`；與 EP102 群組清單 API 共用，不另建表。
- [ ] 實作設備類別清單/查詢、新增、修改、複製、刪除及核准 API。修改/核准/刪除使用 DataStamp；EDIT 僅 IssueState=0，核准僅 0，刪除只接受 0/-1/2，並沿用舊 Service 的引用檢查。
- [ ] 核實並實作 wsEQP.ApproveEquipmentType 對簽核 Job/Detail 的流程；無待簽工作可直接到 2，有簽核流程先到 1，再依結果到 2 或退回 0。
- [ ] ACC Type API：可選配件目錄查詢、既有關聯查詢、完整集合取代；在交易內同步 tbleqpequipmenttypeacctype。COPY 僅複製舊 clsEQPBasis.AddEquipmentType 明確複製的 ACC 關聯。
- [ ] Consume API：依規格驗證數量、容許量與週期關係，CRUD 以 (EquipmentType, ConsumeNo) 為鍵。
- [ ] Property API：依舊按鈕把設備類別編號映射至共用表 tbleqpequipmentproperty.equipment；預設查 IssueState=0；更新/刪除檢查 DataStamp。回傳屬性定義名稱與舊 ValueList 一致。
- [ ] 圖片庫 API：列舉、預覽（目前 Vue 契約為 { dataUrl }）、新增/更新 multipart pictureBody、刪除；確認被類別引用時拒絕刪除，限制 MIME/檔案大小並授權呼叫者。
- [ ] 設備類別範本 API：GET /api/equipment/types/import/template 回傳正式 .xlsx 範本；POST /api/equipment/types/import/preview 解析舊 .xls 第一工作表並回傳 distinct EquipmentType/EquipmentClass 結果列。
- [ ] 設備類別匯入工作 API：POST /api/equipment/types/import/jobs 接收 .xls/.xlsx multipart，沿用第一工作表固定 8 欄欄序、略過第 1 列標題、使用登入者 UserNo，依 EquipmentType 分組呼叫/重建 EquipmentTypeDataChecker；回傳 jobId/status/progress/results。
- [ ] 實作 GET /api/equipment/types/import/jobs/{jobId} 供進度與每個 EquipmentType/EquipmentClass 的 Message 輪詢；POST /api/equipment/types/import/jobs/{jobId}/cancel 合作式取消，保留已完成的逐類別結果。需把部分成功、失敗原因與是否已寫入正式表說清楚，不可只回 HTTP 成功。
- [ ] 以正式 PostgreSQL migration 管理實際要新增的 schema/索引/權限；授予 ymes_app 最小必要權限，所有多表修改使用交易。Production 禁用 Fake provider。
- [ ] API 完成後由公司電腦連 Development 假資料 UI 做契約對接，再由家用電腦完成 API/資料庫驗證；同步回寫規格與本清單。不要提交連線字串、密碼、Token、私鑰。
## 設備設定：設備群組資料（公司電腦交接）

先閱讀 docs/modules/EQP/設備設定/設備群組資料.md、docs/DATABASE_CONNECTION.md 與 database/reference/mestest_equipment_group_create.sql。此 SQL 建立已由 MES Oracle MESTEST 核實的兩張空表，不含業務資料；執行前先核對 YMES schema 既有結構。

- [ ] 確認 YMES 已有 tbleqpgroupbasis、tbleqpgroupdetail；缺表才執行單表 CREATE SQL。第一階段 bootstrap 也已含兩表，Oracle 核實後的欄位來源註解已校正。
- [x] 公司端已查得 TBLSMDAREARELATION、TBLSMDSMTAREAEQPMAP 原始 DDL、欄位/NULL/PK/索引/FK，交付 `database/reference/mestest_equipment_group_smd_source_ddl.sql` 與 `mestest_equipment_group_smd_create.sql`。
- [x] 公司端已完成 EP105 設備群組 Vue 頁 WinForms Apply/稽核 UI 與 Development preview 隔離；家用端仍只接手本節 schema/API/SMD migration，不需新增表規格。
- [ ] 實作設備群組清單、CRUD、核准/刪除 API：IssueState 0 才可修改；核准只允許 0，依 IssueNo EQP GROUP BASIS 啟動簽核；刪除只接受 0/-1/2，傳 DataStamp。
- [ ] 新增 API 實作 ChkObjectForSMDLayout(EquipmentGroup,4)：歷史 helper 會拒絕已存在於 `tblSMDAreaBasis.AreaNo`、`tblEQPEquipmentBasis.EquipmentNo` 或 `tblINVInventoryBasis.InventoryNo` 的同值編號。此 helper 本體在 `modKC.vb` 是註解版本；若家用端有目前部署的 active helper，先比對錯誤訊息與例外行為。
- [ ] 實作群組明細與可加入設備端點。可加入清單限定 IssueState=2、ChamberFlag=0、尚未出現在任何 group detail；新增前由 API 再次以交易/鎖定檢查設備跨群組唯一性。
- [ ] 明細新增/刪除只允許父群組 IssueState=0；要同步 TBLSMDSMTAREAEQPMAP 的情況依 TBLSMDAREARELATION ObjectType=4 判斷。
- [x] 公司端已從 clsEQPBasis.DelEQPGroupBasis 確認舊行為：同一批刪 group detail 與 group basis，不刪 TBLSMDSMTAREAEQPMAP；API 保留此相容行為。
- [ ] 核對並授予 ymes_app 對設備群組主從表與 SMD 映射表的最小讀寫權限；缺表時以已交付來源 SQL 建 migration。正式 API 不使用 Fake provider。
- [ ] 公司電腦完成 UI/API 契約對接後，回寫遷移規格及本清單；不要提交連線字串、密碼、Token、私鑰。

## 設備設定：設備狀態設定（公司電腦交接）

先閱讀 `docs/modules/EQP/設備設定/設備狀態設定.md`。主表與 CRUD/options API 已由家用端部署；本次公司端只調整 Vue UI/互動，不新增 schema 或 API。

- [x] 設備狀態主表 `ymes.tbleqpstatebasis` 已存在；migration `0016_equipment_state_api.sql` 恢復 Oracle 確認過的 DEFAULT，且 API GET/options/POST/PUT/DELETE 已部署。
- [x] StateType 0/1、UtilizationType 0/1 與繁中標籤已由公司端唯讀核對 MESTEST ValueList/資源檔；API options 保留上述值。
- [x] API DTO/Apply HEX 保留行為、ARBG signed integer、User Define 0–10 限制已記錄於規格；前端改為只在 Development preview 使用假資料。
- [x] 兩張刪除引用表 Oracle 原始 DDL 與 PostgreSQL 空表 CREATE SQL 已交付：`database/reference/mestest_equipment_state_references_source_ddl.sql`、`database/reference/mestest_equipment_state_references_create.sql`。
- [ ] 在 YMES 實際 schema/migrations 比對 `tbleqpstatecontrol`、`tblemsequipmentstate` 與 `tbleqpqclistbasis.unvalidaction`；對已存在資料表保留額外欄位，只有缺少且 API 必須時新增 forward migration。
- [ ] 給 `ymes_app` 對引用來源最小 SELECT 權限；migration/權限完成後移除 DELETE fail-closed 503，精確檢查三個來源，引用回 409、無引用回 204。
- [ ] 部署後驗證引用狀態阻擋及未引用刪除；將結果回寫規格與本待辦。不得提交連線字串、密碼、Token 或私鑰。
## 設備設定：設備點檢表設定（公司電腦 UI 已交付；家用端待 API）

先閱讀 docs/modules/EQP/設備設定/設備點檢表設定.md 與 database/reference/mestest_equipment_qclist_create.sql。SQL 來自 MES Oracle MESTEST 原始 DDL，轉為 PostgreSQL 空表結構，不含 MES 點檢主檔/明細資料。

- [x] 公司端已依 WinForms Form/Designer/Service 補齊 Vue DEV UI：主從清單、Add/Edit/Copy/Delete、明細新增/修改/多選刪除/排序、Confirm/Close/Apply、稽核欄位與作用列刪除行為；本輪無新表或新 DDL。
- [ ] 確認 YMES ymes.tbleqpqclistbasis、ymes.tbleqpqclistdetail 是否已存在；逐欄比對後，缺表才套用參考 SQL。第一階段 bootstrap 已按 Oracle DDL 補上 AFTEREVENTQC DEFAULT 0、DATASTAMP DEFAULT 0、INPUTDATACOUNT DEFAULT 1。
- [ ] 實作 GET /api/equipment/qc-lists（qcListNo 查詢、QCListNo 排序）、GET /api/equipment/qc-lists/{qcListNo}/items（QCOrder 排序）。
- [ ] 實作 GET /api/equipment/qc-lists/options；QCType 只回 0「開始生產時點檢」和 2「手動點檢」（已由公司端唯讀核對 MESTEST ValueList/繁中資源），設備狀態選項依 YMES 設備狀態主檔；-1 顯示「不動作」。
- [ ] POST /api/equipment/qc-lists 新增主檔及完整明細；PUT /api/equipment/qc-lists/{qcListNo} 使用 DataStamp 樂觀並行檢查並完整取代明細；DELETE 同交易刪除明細與主檔。任何多表寫入需由一個 PostgreSQL transaction 保證原子性。
- [ ] 伺服器驗證與 WinForms 一致：編號/名稱必填；編號與名稱長度 100；QCType、UnValidAction 必填；至少一項；QCOrder 依序且不超過 NUMBER(2,0)；CheckItem 不可重複且最多 1000；Type 0 StdValue 數字必填；Type 1 至少一個邊界、各自為數字、雙邊界時最大值大於最小值、空邊界存星號；InputDataCount 1–10；方法/標準最多 100。
- [ ] AfterEventQC 只允許 QCType=0；其他類型強制 0。DataStamp 不符回 409；欄位驗證回 400；不存在回 404。
- [ ] 確認既有 YMES 外鍵/點檢排程使用關係與 legacy delete 的影響；Oracle 原表 DDL 未宣告主從 FK，legacy Service 的 delete 會明確刪主檔和明細。
- [ ] Production API 完成後以公司電腦 DEV UI 對接；同步更新規格與本待辦，不能提交密碼、Token、連線字串或私鑰。

## 設備資料：載具分類資料 EP301（公司端 UI 與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/載具/載具分類資料.md`、`docs/DATABASE_CONNECTION.md` 及下列 PostgreSQL CREATE SQL。Oracle DDL 由公司電腦以 PowerShell 5.1 + ODP.NET 直接唯讀查得，不含 MES 業務資料。

- [x] 公司端已對照 `frmCarrierCategory` / `frmCarrierCategoryDef`、`wsEQP`、`clsCarrierBasis`；Vue DEV UI、Function route 與逐筆 API 契約已交付。
- [x] 公司端已查得 MESTEST.TBLEQPCARRIERCATEGORY 8 欄、PK、唯一索引、NOT NULL/default；Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_carrier_category_source_ddl.sql`、`database/reference/mestest_carrier_category_create.sql`。IssueState DEFAULT 0 NOT NULL；DataStamp DEFAULT 0 nullable；無 FK/額外索引。
- [ ] 核對 YMES `ymes.tbleqpcarriercategory` 既有欄位/NULL/DEFAULT/PK/index 與 migration；缺表才依 `mestest_carrier_category_create.sql` 新增 migration，保留任何額外 YMES 欄位。
- [ ] 實作 GET `/api/equipment/carrier-categories?issueState=&carrierCategory=`，狀態限 -1/0/1/2、CarrierCategory 部分符合、排序；回傳稽核欄位及 DataStamp。
- [ ] 實作 POST 新增與 PUT 修改；CarrierCategory Trim/ToUpper、必填、最多 50，Description 最多 255；新增設定 IssueState=0/Creator/CreateDate/DataStamp=0；Edit 只允許 IssueState=0 並做 DataStamp 比對。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除，2 改成 -1 並更新 Reviser/ReviseDate/DataStamp，1 拒絕。舊服務雖只對狀態更新檢查 DataStamp，新 API 建議刪除兩種分支都做並行檢查；API 每筆操作保持原子。
- [ ] 實作 POST `/{carrierCategory}/approve`：只接受狀態 0 並檢查 DataStamp。舊 `wsEQP.ApproveCarrierCategory` 依 EQP Issue/Job 設定，無簽核工作可直接轉 2；有 Job 則建立簽核主檔並轉 1、簽核完成轉 2、退回轉 0，並通知第一簽核群組。Production 不可把 DEV 0→2 的簡化結果當成正式簽核成功。
- [ ] 完成後以公司端 Vue 頁對接、核對 Delete 多筆部分成功、Edit/Approve/Delete 的狀態與並行衝突、API 回傳及稽核欄位，更新規格並部署。

## 設備資料：載具類別資料 EP302（公司端 UI 與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/載具/載具類別資料.md`、`docs/DATABASE_CONNECTION.md` 及下列 PostgreSQL CREATE SQL。Oracle DDL 由公司電腦以 PowerShell 5.1 + ODP.NET 直接唯讀查得，不含 MES 業務資料。

- [x] 公司端已對照 `frmCarrierType` / `frmCarrierTypeDef`、`wsEQP`、`clsCarrierBasis`；Vue DEV UI、Function route 與逐筆 API 契約已交付。
- [x] 公司端已查得 MESTEST.TBLEQPCARRIERTYPE 13 欄、PK、唯一索引、NOT NULL/default；Oracle source DDL 與 PostgreSQL 空表 CREATE SQL 為 `database/reference/mestest_carrier_type_source_ddl.sql`、`database/reference/mestest_carrier_type_create.sql`。CarrierCategory 雖是邏輯關聯，原表沒有 FK 或額外索引。
- [ ] 核對 YMES `ymes.tbleqpcarriertype` 現有欄位/NULL/DEFAULT/PK/index；缺表才依 CREATE SQL 建 migration，保留 YMES 額外欄位。
- [ ] 實作 GET `/api/equipment/carrier-types?carrierType=`，CarrierType 部分符合、排序；回傳全部顯示欄位、IssueState、DataStamp 及稽核欄位。可提供可選 `issueState` 過濾，但 Vue 的 WinForms 狀態列會在前端即時切換。
- [ ] 實作 GET `/api/equipment/carrier-categories?issueState=2` 作為 CarrierCategory 查找選項；只提供已核准分類。
- [ ] 實作 POST 新增與 PUT 修改：CarrierType Trim/ToUpper、必填、最多 50；CarrierCategory 必須存在且 IssueState=2；UseLayer 必須 0/1；UseLayer=0 時 CarrierLayer/CarrierCapacity/StartPosition 寫 1，UseLayer=1 時三者須正整數且符合 NUMBER(3,0)；Description 最長 255。新增設定 IssueState=0/Creator/CreateDate/DataStamp=0；Edit 限 IssueState=0 並做 DataStamp 比對。
- [ ] 實作 DELETE：IssueState 0/-1 實體刪除，2 改成 -1 並更新 Reviser/ReviseDate/DataStamp，1 拒絕。舊服務未對實體刪除做 DataStamp 條件；新 API 建議兩種分支都檢查並行衝突，且每筆操作原子。
- [ ] 實作 POST `/{carrierType}/approve`：只接受狀態 0 並檢查 DataStamp。舊 `wsEQP.ApproveCarrierType` 依 `CARRIER TYPE` Issue/Job 設定直核轉 2，或建立 Issue 主檔並轉 1；簽核完成轉 2、退回轉 0、通知第一簽核群組。不可把 DEV 0→2 簡化結果當正式簽核成功。
- [ ] 完成後以公司端 Vue 頁對接；核對 UseLayer 條件欄位、分類選項、批次 Delete/Approve 部分成功、DataStamp 與稽核欄位，更新規格並部署。

## 設備基本資料 EP104（公司電腦規格交接）

先讀 docs/modules/EQP/設備設定/設備基本資料.md 與 docs/modules/EQP/設備設定/設備參數設定.md。設備主表與參數表原始 DDL 已由使用者提供；單表 PostgreSQL CREATE SQL 已放在 database/reference/。

- [x] 公司端將使用者提供的 TBLEQPEQUIPMENTBASIS 原始 DDL 轉成 database/reference/mestest_equipment_basis_create.sql，並修正第一階段 bootstrap 的 DEFAULT/NOT NULL constraints，移除推測欄位註解。
- [x] 公司端將使用者提供的 TBLEQPPARAMETERBASIS 原始 DDL 轉成 database/reference/mestest_equipment_parameter_basis_create.sql，並修正第一階段 bootstrap 的 NOT NULL constraint names，移除推測欄位註解。
- [ ] 家用電腦比對 "YMES" schema 內 TBLEQPEQUIPMENTBASIS、TBLEQPPARAMETERBASIS 的欄位/NULL/DEFAULT/PK/index；缺表才套用 CREATE SQL，既有表若不符則建立 forward migration。不要在公司電腦連資料庫或執行 migration。
- [ ] 核對設備 CRUD/Approve、Chamber 共表資料、DataStamp、IssueState、刪除轉停用與 Controlled 屬性核准檢查，確保多表更新使用正確交易。
- [ ] 提供 EquipmentClass、設備類別、配件類別、工程群組、供應商、點檢表及訊息佇列 lookup/options API；配件類別取自 ymes.tbleqpaccessorytype 並保留 IssueState=2 篩選。
- [ ] 修正設備 DTO / write contract：回傳 Creator、Reviser；EqpRecipe 接受 0–3（WinForms ValueList 四種模式），目前 EquipmentWriteRequest 限制 0–1。
- [ ] 屬性、參數、設備點檢關聯、資料匯入分別依 WinForms 子功能規格實作 API，並核實其原始資料表 DDL。
- [ ] 設備屬性表 `TBLEQPEQUIPMENTPROPERTY` 已有 Oracle 來源單表 SQL：`database/reference/mestest_equipment_type_create.sql`；套用前比對 YMES 結構，API 保留 Equipment 可代表設備編號或設備類別編號的舊語意。
- [x] `TBLEQPEQUIPMENTBASIS`、`TBLEQPPARAMETERBASIS` 原始 DDL 已由使用者提供並轉成 PostgreSQL 單表 CREATE SQL；參照上一條核對 YMES schema。
- [x] `TBLEQPQCListSetup` Oracle DDL 已從公司端 `.equipment-settings-ddl.tmp.sql` 轉換至 `database/reference/mestest_equipment_qc_list_setup_create.sql`；套用前比對 YMES 既有表。
- [ ] 依設備參數規格提供全量 GET/PUT 與 options API；單交易取代整份清單，空陣列代表清空，驗證 ParameterName 唯一。
- [ ] 依設備點檢表關聯規格提供 GET/CRUD 及 QCList options；核對舊 Form 傳入 DataStamp 但 Service 編輯方法未接收該參數的併發行為。
- [ ] 依設備匯入規格提供 xls/xlsx 第一工作表解析、欄位/父子 Chamber 驗證及逐組結果 API；明確定義逐筆/整批交易與取消行為。
- [x] 公司端已把參數、設備點檢表關聯及設備匯入 Vue 子畫面接到 EP104；DEV 假資料只存 localStorage，Production 改呼叫 API，不使用 Fake provider。
- [ ] 對照公司電腦 Vue DEV UI 契約實作 API：設備參數 GET/PUT/options；設備點檢表關聯 GET/CRUD/options；設備匯入 POST 建立工作、GET 輪詢、POST 取消、GET xls/xlsx 範本，回傳逐組結果。完成後同步回寫規格與此清單。

## 作業站主檔 OP Basis（公司電腦交接）

先讀 `docs/modules/OP/作業站主檔.md` 與 `docs/DATABASE_CONNECTION.md`。公司端已從 WinForms Form、Designer、`clsOpBasis` 及 MESTEST Oracle 原始 DDL 追出主畫面規則、刪除引用範圍與 18 張作業站相關表結構。

- [x] 公司端已透過 PowerShell 5.1 + ODP.NET 呼叫 `DBMS_METADATA.GET_DDL`，來源及 PostgreSQL 空表 SQL 分別為 `database/reference/mestest_operation_and_system_source_ddl.sql`、`database/reference/mestest_operation_and_system_create.sql`。本批包含 TBLOPBASIS、TBLOPGROUPDETAIL 及服務引用的 16 張子表；同檔另附三張 TBLSYS* 原始 DDL。
- [ ] 家用電腦逐欄比對 YMES schema 的 18 張作業站表與 migration；缺表才套用 CREATE SQL，既有差異以 migration 修正。不要從 `mes_master_schema.sql` 推測欄位。
- [ ] 實作作業站 GET/POST/PUT/DELETE/approve API，驗證 OPNo 不含逗號、不等於 LOTCREATE、不可與 OPType 衝突；StdUnitRunTime/StdQueueTime >= 0、CountOpUnitQty >= 1；DataStamp 樂觀並行；沿用 IssueState=0/1/2/-1 的編輯/刪除/停用規則。
- [ ] Delete 先檢查 `TBLOPGROUPDETAIL` 引用並回 409；交易內照 `clsOpBasis.DelOPBasis` 確認各子表刪除順序與條件，再刪主檔，不能只刪 `tblopbasis`。
- [ ] Approve 實作 CPass 屬性檢查、`LC_CO_MEASURE` 規則限制、`L_SPC_GEN`/`L_SPC_FAI`/`L_SPC_IPQC` 對應 QC Item 提示及舊簽核 Job/Detail 行為；補齊由原始碼/Oracle 確認的測試案例。
- [ ] 依規格為 Resource、Business Rule、Parameter、QC Item、Error、Copy OP Data、SubOP、Import 各建 API/DTO；多表更新使用交易。精確 endpoint 命名可依現有 YMES API 慣例調整並回寫本規格。
- [ ] ValueList 的 MaterialOption、MultiOperatorMode、OSOption 從 MES 原始選項資料確認，不把程式註解中的舊選項清單直接當正式值。
- [ ] Production API 完成後，公司電腦將 OP 子功能 DEV 假資料畫面接到正式 API 契約並回寫規格；Production 不可使用 localStorage Fake provider。

## EP202 配件類別資料（公司端 2026-11-16 已交接）

公司端已依 docs/modules/EQP/配件/配件類別資料.md 完成 Vue Development 假資料頁，並直連 Oracle 查得下列來源表 DDL。請先 pull，再比對 YMES 實際 schema；只有缺表才新增 forward migration，不直接執行 Oracle 原始 DDL。

來源資料：
- database/reference/mestest_equipment_type_accessory_catalog_source_ddl.sql / _create.sql：TBLEQPACCESSORYTYPE。
- database/reference/mestest_accessory_type_dependencies_source_ddl.sql / _create.sql：TBLEQPACCESSORYCATEGORY、TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYLIFE。

家用端工作：
1. 比對四表實際 YMES 欄位、NULL/default、PK 及既有歷史 migration；保留 YMES 額外欄位，不覆蓋既有表。
2. 實作規格中的 accessory type CRUD、category lookup、property（固定 AccessoryVersion=-1）、life API。
3. 刪除須在單一交易依序清理屬性與壽命後刪主檔；Copy API 僅複製來源壽命列，不複製屬性列。
4. 核准端接原有簽核流程並回傳實際 IssueState；DEV UI 直接轉為 2 只是本地簡化。
5. Import API 支援舊第一工作表欄序 AccessoryType、AccessoryCategory、Description、.xlsx/.xls，保留前兩欄必填與逐組 DataChecker 結果/錯誤訊息。公司端目前只有 CSV 本地假匯入。
6. 部署後更新本節、功能規格與 API 狀態，再由公司端 pull 對接實際 API。

API 路徑與 DTO 詳見 docs/modules/EQP/配件/配件類別資料.md；目前 Production API 尚未完成，不要以 Development 假資料結果回報正式成功。

## EP201 配件分類與 EP204 可用狀態（公司端 2026-11-16 交接）

先讀以下規格與程式，再做 YMES schema/API；公司端已用唯讀 Oracle 連線核實 DDL，並完成 Vue Development 假資料頁。不可把假資料視為 MES 業務資料。

### 來源與 SQL

- EP201 規格：docs/modules/EQP/配件/配件分類資料.md
- EP204 規格：docs/modules/EQP/配件/配件分類狀態定義設定.md
- EP201 主表 TBLEQPACCESSORYCATEGORY 的來源 DDL/PG 空表 SQL：database/reference/mestest_accessory_type_dependencies_source_ddl.sql、database/reference/mestest_accessory_type_dependencies_create.sql。
- EP204 狀態定義表 TBLEQPACCCATEGORYSTATEBASIS：database/reference/mestest_accessory_category_state_source_ddl.sql、database/reference/mestest_accessory_category_state_create.sql。
- EP204 lookup 表 TBLEQPACCSTATEBASIS：database/reference/mestest_accessory_state_source_ddl.sql、database/reference/mestest_accessory_state_create.sql。database/reference/mestest_first_phase_create_tables.sql 中的 lookup 表 block 已用 Oracle 原始 DDL 修正。
- CREATE SQL 只建空結構、不含 MES 資料。開始前查 YMES PostgreSQL 實際 schema；既有表保留額外欄位，缺表才新增 forward migration，欄位/default 差異用 migration，不要在來源 Oracle 執行 SQL。

### 家用端待辦

1. 逐欄比對三張表的欄位型別、NULL/default、PK、索引與既有 migration。TBLEQPACCCATEGORYSTATEBASIS 的 PK 為 AccessoryCategory/AccessoryState/OperationName；DATASTAMP default 0 但可 NULL。TBLEQPACCSTATEBASIS 的 STATETYPE default 1 且 NOT NULL；DATASTAMP default 0 但可 NULL。沒有額外 Oracle FK/index。
2. 若缺表，新增可重複/版本化 migration，套用前按既有部署慣例備份；授予 ymes_app 最小必要 CRUD 權限。不要直接改動已部署資料。
3. 建立 EP201 API：GET/POST/PUT accessory-categories、POST approve/delete/import。對齊規格中的 0/1/2/-1 狀態、DataStamp、簽核 Job/Detail 流程；狀態 0/-1 物理刪除、2 改為 -1、1 拒絕。來源刪主分類不會 cascade 狀態子列，API 保留此行為。
4. 建立 EP204 API：GET/POST accessory-category-states、PUT edit、POST approve/delete；GET accessory-states、GET accessory-state-controls?controlType=EMS、GET system/functions?executionMode=0&accessoryStateChangeInterface=true，以及分類 options。
5. EP204 新增/修改需檢查每個 AccessoryCategory 至多一個 InitialFlag=1。Oracle 原表沒有相應 unique index；Production 需以交易/鎖或先確認既有資料後新增安全約束，避免並行同時設定兩個初始狀態。
6. OperationName 不可等於 ControlType=EMS 標準動作的 ControlNo 或翻譯名稱。介面選項只限 EXECUTIONMODE=0 且 ACCStateChangeInterface=1；預設介面 frmACCStateChangeDescription，執行檔 ES_ACCStateChangeDescription.dll。
7. 狀態清單以 AccessoryState join TBLEQPACCSTATEBASIS 取得 StateName/StateColor。EP204 刪除 0/-1 物理刪除、2 改成 -1、1 拒絕；核准走現有 issue workflow。
8. 配合 Vue API DTO 欄位採 camelCase：AccessoryCategory/AccessoryState/OperationName 三欄為 EP204 key，寫入時驗 DataStamp。匯入回傳逐筆 results（accessoryCategory/result/message）；支援舊第一工作表 .xls/.xlsx 欄位 AccessoryCategory/Description 和 DataChecker 分組結果；Vue 以 multipart/form-data 的 file 欄位上傳 Excel，CSV 則傳 JSON。
9. 部署後更新規格與本段待辦，再通知公司端 pull 對接 Production；API 不可讓 Production 回退 localStorage。

Vue 元件與路由已在公司端完成：web/src/components/AccessoryCategoryMaster.vue、AccessoryCategoryStateMaster.vue；路徑為 /YMES/Equipment/AccessoryCategories、/YMES/Equipment/AccessoryCategoryStates。兩頁 Production endpoint 尚未存在。


## EP203 配件基本資料（家用電腦接手）

先讀 docs/modules/EQP/配件/配件基本資料.md 與 docs/DATABASE_CONNECTION.md。公司端以 Windows PowerShell 5.1 + ODP.NET 直接唯讀查 MESTEST DDL；本次沒有查詢或匯出業務資料。

### Oracle 來源檔

- 六張表原始 Oracle DDL、八個 index DDL：database/reference/mestest_accessory_basis_source_ddl.sql
- 六張表 PostgreSQL 空表 CREATE SQL：database/reference/mestest_accessory_basis_create.sql
- EP203 主表區塊已回寫 database/reference/mestest_first_phase_create_tables.sql。
- 新表清單：TBLEQPACCESSORYBASIS、TBLEMSACCESSORYSTATE、TBLEMSACCESSORYSTATELOG、TBLEQPACCSTATECONTROL、TBLEMSEQPACCSTATE、TBLEMSEQPACCSTATELOG。
- 原始 Oracle DDL 沒有 FK。非 PK index 僅主表 GETGROUPNO 與設備配件狀態 EQUIPMENTNO；其他六個 index 為 PK index。
- DDL 僅描述結構；不得在來源 Oracle 執行 CREATE SQL，不得把本機憑證寫入 Git。

### 家用端待辦

1. 逐欄比對 YMES PostgreSQL schema/migrations 的上述六表：欄位型別、長度、NULL、DEFAULT、PK、index；查 TBLEQPACCESSORYPROPERTY、TBLEQPACCESSORYTYPE、TBLEQPACCESSORYCATEGORY、TBLEQPACCSTATEBASIS、TBLEQPACCCATEGORYSTATEBASIS、TBLEQPACCESSORYLIFE、TBLSYSELEMENTBASIS、設備狀態/簽核資料是否已有 YMES 對應。缺表才新增 forward migration；既有額外欄位不可刪。
2. 實作 GET api/equipment/accessory-basis?issueState=all&currentOnly=false，回傳 WinForms Grid 所有非隱藏欄位及 IssueState、CurVersion、AccessoryVersion、DataStamp。排序 AccessoryType、AccessoryNo；保留前端查詢 AccessoryNo。
3. 實作 POST 新增、PUT /{accessoryNo}/{version} 修改，伺服器驗證長度/必填/版本複合鍵；新增狀態 0、版本 01。PUT/Approve/Delete 驗 DataStamp，Creator/Reviser 與時間由登入上下文/server clock 取得。
4. Approve 只接受狀態 0，依配件分類初始狀態、TBLEQPACCSTATECONTROL Initial/EMS、配件壽命 RepairCycle 和既有簽核 Job/Detail 實作。同步目前配件狀態、設備配件映射及歷程資料；回傳真實狀態，不可固定把每筆標成 2。多表更新使用單一 PostgreSQL transaction。
5. Delete 接受 0/-1/2：0/-1 依舊 Service 刪配件現況、設備目前配件、屬性（所有版本）及主檔；2 改為 -1 並更新 DataStamp/Reviser/ReviseDate；1 回 409。多選回逐筆結果，避免部分失敗無法追蹤。
6. Copy API 新增主檔及複製來源已核准的配件屬性至新版本，目標屬性 IssueState=0；必須同交易完成。公司 UI 目前使用新增後呼叫 copy-properties，建議收斂成單一 POST copy endpoint，或讓 API 提供相同原子契約。
7. 子屬性依配件/版本提供 GET/POST/PUT/DELETE/Approve；使用既有 TBLEQPACCESSORYPROPERTY，不得另建重複表。核對 DataStamp、PropertyNo 定義 (PropertyType=1)、元素值 lookup (TBLSYSELEMENTBASIS) 與狀態規則。
8. 提供已核准配件類別及供應商 lookup；舊配件類別清單須取 IssueState=2，AccessoryCategory 由所選類別帶入。API 從 auth context 取操作者，不信任前端 Creator/Reviser。
9. 匯入需支援舊 Excel 第一工作表七欄：AccessoryNo、AccessoryType、AccessoryCategory、VendorNo、ModelNo、AssetNo、Description。前四項必填；重查已存在編號、已核准類別/供應商及分類關係；每組 (AccessoryNo, AccessoryType) 唯一；插入版本 01/狀態 0。實作 multipart /import/preview 回傳 token+逐列結果及 /import/commit 重新驗證後交易寫入；提供 ACC BASIS 範本下載。
10. 完成後更新 API 規格/migration 記錄及本待辦，通知公司端 pull 對接。Production 不得回退 localStorage 假資料。

## EP205 配件狀態設定（公司端 2026-11-16 已交接）

先讀 docs/modules/EQP/配件/配件狀態設定.md。公司端已照 WinForms frmEQPACCStateBasis / Def 建立 Vue Development 假資料頁，使用既有 Oracle 狀態主表，不新增重複 DDL。

- Oracle DDL：database/reference/mestest_accessory_state_source_ddl.sql；PostgreSQL 空表 SQL：database/reference/mestest_accessory_state_create.sql。Oracle 欄位/PK/index 已透過 PowerShell + ODP.NET 唯讀查證。
- 元件與路由：web/src/components/AccessoryStateBasisMaster.vue、/YMES/Equipment/AccessoryStates；FunctionNo EQP ACC STATE / EP205。
- 先比較 YMES tbleqpaccstatebasis 實際 schema 與 migration；若表已存在則保留 YMES 欄位，只針對缺漏差異新增 migration，不重建、不將 Oracle 原始 SQL 套到來源資料庫。
- 實作 GET/POST/PUT/DELETE /api/equipment/accessory-states，與 EP204 共用狀態 lookup DTO；回傳 signed ARGB stateColor，保留可為 NULL 的 DataStamp。
- 新增/修改驗證 AccessoryState -99..99、StateType 0/1、User Define 不可用 0..10、StateName 必填、Description 最長 255；StateName 最長 50。稽核欄位取 auth context/伺服器時間；修改用 DataStamp 並行檢查。
- DELETE 不 cascade；若 TBLEQPACCCATEGORYSTATEBASIS 引用該狀態，回 409，避免舊表無 FK 時留下孤兒狀態定義。舊 WinForms 直接刪除的差異記在 API 文件。
- 確認 ymes_app 最小權限、API DTO/錯誤回應與 EP204 消費端一致；部署後更新規格/本待辦並通知公司端接 API。

## 設備資料：載具基本資料 EP303（公司端 UI、規格與 Oracle DDL 已交付）

先讀 docs/modules/EQP/載具/載具基本資料.md、docs/DATABASE_CONNECTION.md 及下列 SQL。公司端以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查詢 MESTEST；沒有讀取業務資料，也沒有在 Oracle 執行 DDL。

### 已交付

- [x] Vue DEV 元件 web/src/components/EquipmentCarrierBasisMaster.vue；第三層 FunctionNo CARRIER BASIS，路徑 /YMES/Equipment/CarrierBasis。
- [x] Oracle 原始 DDL：database/reference/mestest_carrier_basis_source_ddl.sql、database/reference/mestest_carrier_basis_dependencies_source_ddl.sql。
- [x] PostgreSQL 空表 CREATE SQL：database/reference/mestest_carrier_basis_create.sql、database/reference/mestest_carrier_basis_dependencies_create.sql。
- [x] 主表 TBLEQPCARRIERBASIS 15 欄，PK CarrierNo；依賴 TBLWIPCarrierState 6 欄、TBLSMDFactoryBasis 10 欄。三表各僅 PK unique index，無 FK；CarrierBasis IssueState DEFAULT 0 NOT NULL、DataStamp DEFAULT 0 nullable。
- [x] first-phase SQL 已補載具在製狀態表，修正 CarrierBasis/FactoryBasis defaults，移除來源未證實的推測欄位註解。

### 家用端待辦

1. 先核對 YMES schema/migrations 中 ymes.tbleqpcarrierbasis、ymes.tblwipcarrierstate、ymes.tblsmdfactorybasis 的欄位型別/長度/NULL/DEFAULT/PK/index；FactoryBasis 已在第一階段 SQL，先確認 YMES 實際表。缺表才新增 forward migration，保留 YMES 額外欄位，不直接在 Oracle 執行 CREATE SQL。
2. 實作 GET /api/equipment/carrier-basis?carrierNo=，回傳全部 IssueState，CarrierNo 部分符合；包含所有非隱藏 grid 欄位、DataStamp、稽核欄位。
3. 實作 POST 新增主表與 WIP CarrierState=0 列，單一交易；CarrierNo 大寫/必填/最多 50，Category/Type 必填最多 50、FactoryNo 必填最多 20、Description 最多 255；Category/Type/Factory 必須是 IssueState=2 且 Type 屬於指定 Category。Creator/CreateDate 取 auth context/server clock，DataStamp 初值 0。
4. 實作 PUT /api/equipment/carrier-basis/{carrierNo}，只允許 IssueState=0、DataStamp compare-and-swap；更新主表 Category/Type/FactoryNo/Description、Reviser/ReviseDate/DataStamp，並在同一交易同步 WIP CarrierCategory/CarrierType。
5. 實作 DELETE /{carrierNo}?issueState=&dataStamp=：0/-1 在同一交易實體刪除主表與 WIP 列；2 僅將主表改為 -1 並更新稽核/DataStamp；1 回 409。舊服務物理刪除未帶 DataStamp 條件，新 API 兩種分支都要比對版本。
6. 實作 POST /{carrierNo}/approve：IssueNo CARRIER BASIS；先確認 Issue/Job/Detail 配置。未配置簽核工作時可直轉 2；有簽核工作時建立工作並轉 1、通知第一簽核群組，核准轉 2、退回轉 0。使用既有 YMES 簽核服務，不把 DEV 直核流程當 Production。
7. 提供 GET /api/master-data/factories?issueState=2，從 ymes.tblsmdfactorybasis 回傳 FactoryNo/FactoryName/Description；檢查 ymes_app 唯讀權限。
8. EP302 的 GET /api/equipment/carrier-types 增加可選 carrierCategory 篩選；EP303 Type picker 傳 issueState=2 和 carrierCategory。現有狀態列仍由 Vue 前端即時切換。
9. 三張表比對/migration、options、CRUD/簽核完成後更新規格/API 文件、本待辦與資料庫紀錄，再部署並通知公司端 pull 對接。公司端尚未做瀏覽器人工驗收。

## 設備參數資料 EP401（公司端 UI、規格與 Oracle DDL 已交付）

先讀 `docs/modules/EQP/設備設定/設備參數資料.md`、`docs/DATABASE_CONNECTION.md` 與兩份 DDL。此功能使用 `TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL`，Oracle DDL 由公司電腦 PowerShell 5.1 + ODP.NET 直連唯讀取得；未匯出 MES 業務資料。

### Oracle/PG schema

- Oracle 原始 DDL：`database/reference/mestest_equipment_recipe_source_ddl.sql`。
- PostgreSQL 空表 CREATE SQL：`database/reference/mestest_equipment_recipe_create.sql`；`database/reference/mestest_first_phase_create_tables.sql` 已修正相關 bootstrap block。
- 主檔 PK `(recipegroup,recipeversion)`、非唯一索引 `(recipegroup,curversion)`；明細 PK `(recipegroup,recipeversion,recipeno)`、非唯一索引 `(recipegroup,recipeno)`。來源無 FK，主檔刪除須 API 交易刪除明細再刪主檔。
- DEFAULT/NULL、完整欄位與 Type 值說明見功能規格及 CREATE SQL。執行前逐欄比較 YMES 實際 schema/migrations；缺表才新增 forward migration，保留既有 YMES 額外欄位。

### API/schema 待辦

1. 實作 GET/POST/PUT/DELETE `/api/equipment/recipes`、簽核與 copy/version endpoint；欄位驗證、DataStamp compare-and-swap、稽核欄位由登入上下文/伺服器時間取得。
2. 實作每個 `(RecipeGroup,RecipeVersion)` 的 Detail GET/POST/PUT/DELETE。RecipeNo 唯一；只有父主檔 IssueState=0 可維護明細；主檔刪除與明細刪除同交易。
3. 核准 IssueNo=`RECIPE BASIS`：核對 YMES 簽核工作/明細，支援直接核准與 Pending/核准/退回。核准時同一 RecipeGroup 只能有一個 CurVersion=1；Pending 時保持舊版 current，核准 SQL 完成才切換。
4. COPY/VERSION 沿用 wsEQP.CopyRecipe：只複製 RecipeNo、RecipeValue、Sequence、Description；Type、StdValue、Maximum、Minimum、Creator、CreateDate 不複製（NULL）。Version 使用來源選取版本+1，不以「最大版本+1」註解推測。
5. Recipe No picker 使用既有 `TBLSYSELEMENTBASIS`，提供 `GET /api/system/elements?elementType=9` 回傳 ElementNo/Description/DefaultValue/ElementSequence；不要建立重複元素表。
6. Type options 經 MESTEST TBLSYSVALUELIST 查證：-1 NONE、0 TYPEVALUE、1 RANGE；Production DTO/畫面使用明確標籤。
7. 授予 `ymes_app` 最小必要權限，完成 migration/API/簽核/併發驗證後更新規格並部署，再通知公司端 pull 對接 Production。

## ENT ET101 客戶基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶基本資料.md`、`docs/DATABASE_CONNECTION.md` 和以下來源 SQL。公司端已用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得三表結構；沒有讀取客戶業務資料，沒有在來源 Oracle 執行 DDL。

### DDL / YMES schema

- Oracle source logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`。
- PostgreSQL 空表 CREATE SQL：`database/reference/mestest_customer_basis_create.sql`。
- 三表：`TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR`；各自只有 PK unique index，Oracle 無 FK/其他索引。
- 先逐欄比較 YMES PostgreSQL schema/migrations 的型別、長度、NULL、DEFAULT、PK/index。缺表才新增 forward migration；保留 YMES 已有額外欄位/索引，不要以 `IF NOT EXISTS` 假設既有表已符合。
- 來源列 CustomerSName VARCHAR2(100 CHAR)，PG 使用 varchar(100)；Oracle DATE 使用 timestamp without time zone；NUMBER(1,0)/NUMBER(11,0) 保留 numeric 型別/DEFAULT/NULL。

### API / Service 待辦

1. 實作 `GET /api/ent/customer-basis`，回傳 WinForms 33 個可見欄位、IssueState、DataStamp、稽核欄位及來源保留欄位；支援 CustomerNo 查詢。IssueState 列表篩選由 Vue 即時處理。
2. 實作 POST 新增及 PUT `/{customerNo}` 修改。CustomerNo、CustomerName、CustomerSName 必填；CustomerNo 大寫，編號唯一；空白選項保存為 NULL/依既有 YMES API 契約處理。以登入上下文/伺服器時間寫 Creator/CreateDate/Reviser/ReviseDate，不信任用戶端稽核資料；修改以 DataStamp compare-and-swap。
3. 對非 IssueState=0 的修改重現 WinForms 可編輯範圍：基本身份/聯絡欄位唯讀；允許交易/發票欄位更新。API 必須伺服器端以欄位 allow-list 強制此限制。來源的欄位清空 sentinel 不可靠，新 API 要能明確保存空值。
4. 實作 DELETE `/{customerNo}?issueState=&dataStamp=`。狀態 0/-1 在一筆 PostgreSQL transaction 內依序刪 `tblentcustomershipaddr`、`tblentcustomercontactor`、`tblentcustomerbasis`；狀態 2 改為 -1 並更新 Reviser/ReviseDate/DataStamp；狀態 1 回 409。所有分支都先比對 DataStamp。不要新增未核實的 FK cascade。
5. 實作 POST `/{customerNo}/approve?dataStamp=`，只接受 IssueState=0；沿用 YMES 簽核服務與 IssueNo `CUSTOMER BASIS`。未設定 Job/Detail 可直接到 2；有簽核 Detail 時轉 1、簽核通過到 2、退回到 0。關聯簽核與稽核更新需交易一致。
6. 實作 multipart POST `/api/ent/customer-basis/import/preview`、POST `/import/commit` 及 GET `/import/template`。讀 Excel 第一工作表前三欄 CustomerNo/CustomerName/CustomerSName，逐列必填，預覽回傳依 CustomerNo 分組的結果列與短期 preview token；token 綁定登入者並於 commit 重新驗證。Commit 使用舊 DataImporter CustomerDataChecker 的等價檢查並回傳逐組 Message/進度。範本需沿用 FunctionNo `ENT BASIS` 的匯入範本格式；DEV CSV 不是正式範本。
7. 設定 `ymes_app` 最小表權限；完成 schema/API、交易、簽核與匯入規格後更新 migration/API 文件和本待辦，部署並通知公司端 pull。公司端 Vue 頁在家用 API 完成後再對接 Production。

## ENT ET102 客戶連絡人資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶連絡人資料.md`、ET101 規格與 `todolist_officepc.md` ET102 區塊。公司端已核對 WinForms 並完成 Development Vue 假資料頁。

### Schema

- Oracle DDL 與 PostgreSQL 空表 CREATE SQL 已隨 ET101 交付：`database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。`TBLENTCUSTOMERCONTACTOR` 欄位為 `CUSTOMERNO varchar(50) NOT NULL`、`CONTACTORNAME varchar(50) NOT NULL`、`TELNO varchar(25)`、`FAXNO varchar(25)`、`EMAIL varchar(50)`、`TITLE varchar(25)`、`DEPTNAME varchar(25)`、`CREATOR varchar(10)`、`CREATEDATE timestamp`、`DESCRIPTION varchar(255)`、`DATASTAMP numeric(11,0) DEFAULT 0` nullable、`REVISER varchar(10)`、`REVISEDATE timestamp`。
- 複合 PK 順序 `(CUSTOMERNO, CONTACTORNAME)`；無 Oracle FK 或額外索引。不要再建立重複 reference Create SQL。
- 核對 YMES migration 是否已建此表；若沒有，沿用上述 PostgreSQL 定義新增 forward migration。保留現存 YMES 擴充欄位/索引，別用 `IF NOT EXISTS` 代替 schema 比對。`ymes_app` 只授予需要的表權限。

### API 待辦

1. 以現有 ENT API controller/repository 的結構新增巢狀端點：`GET /api/ent/customer-basis/{customerNo}/contactors`；按 `ContactorName` 排序。客戶選擇器使用 ET101 的 `GET /api/ent/customer-basis`，需能搜尋全部 IssueState。
2. 新增 `POST /api/ent/customer-basis/{customerNo}/contactors`、`PUT /api/ent/customer-basis/{customerNo}/contactors/{contactorName}`、`DELETE /api/ent/customer-basis/{customerNo}/contactors/{contactorName}?dataStamp=`。Body 對應 `contactorName/telNo/faxNo/email/title/deptName/description/dataStamp`；Update 的 composite key 由 route 指定，ContactorName 不可更名。
3. 寫入時驗證 CustomerNo 存在（來源表沒有 FK）、必填 ContactorName 與唯一複合鍵；長度嚴格依 Oracle DDL，特別是 WinForms Title MaxLength=50、實際資料庫僅 25，回傳 400 欄位錯誤、不截斷。
4. Creator/CreateDate、Reviser/ReviseDate 從登入身分與伺服器時間產生；新資料 DataStamp 設 0，更新/刪除以 DataStamp 條件保護並在更新時遞增。刪除依舊 WinForms 支援多筆逐筆處理；API 單筆刪除回傳 404/409 供 Vue 中止、重載清單。
5. 原始 `wsENT.EditCustomerContactor` 呼叫帶有命名參數 DataStamp/Reviser/ReviseDate，但目前檢視的 `kcENT.clsCustomerBasis.EditCustomerContactor` 宣告未含此參數；請依現代 API 明確實作伺服器稽核/併發邏輯，不照搬此來源不一致處。
6. 更新 API 文件/migration 記錄、部署，通知公司端 pull 並在 Production 環境以正式 API 對接。公司端目前的 `/YMES/Customer/Contactors` 頁 Production 模式不使用假資料。

## ENT ET106 客戶出貨地址資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/客戶出貨地址資料.md`、ET101/ET102 規格、`database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql` 及 `todolist_officepc.md` ET106 區塊。公司端已完成 Development Vue 雙 Grid 畫面。ET101 客戶基本資料 API 尚未完成時，先合併 ET101/ET102/ET106 的 ENT controller/repository，避免重複路由與 schema 工作。

### Schema / migration

- `TBLENTCUSTOMERSHIPADDR` 已有 Oracle source DDL/PG CREATE SQL（跟 ET101 一起交付）；ET106 新查 `TBLENTCUSTOMERSHIPDECLARATION` 原始 DDL/PG CREATE SQL 已追加在同兩個 reference 檔。
- Schema 比對後只新增缺少的表；報關表必須照來源無 PK、無 unique/index/FK 的結構建立，不可依表名猜 PK。欄位、NOT NULL、DEFAULT 與索引資料字典以 ET106 規格為準；`CREATEDATE DEFAULT SYSDATE`、`DATASTAMP DEFAULT 0` 都 nullable。
- 出貨地址表 PK `(CUSTOMERNO, SHIPADDR)`；報關表八欄 NOT NULL。來源兩表都沒有 Oracle FK。保留 YMES 現存擴充欄/索引。

### API / Service 待辦

1. `GET /api/ent/customer-basis` 支援 ET106 客戶查找；Vue 僅使用 `IssueState=2`。所有 API 都需檢查登入者 ENT 權限。
2. 地址端點：`GET/POST /api/ent/customer-basis/{customerNo}/ship-addresses`、`PUT /{customerNo}/ship-addresses/{oldShipAddr}`、`DELETE /{customerNo}/ship-addresses/{shipAddr}?flow=&shipCustomerName=&declarationFlag=&dataStamp=`。PUT 以舊 ShipAddr 定位，可在 Body 修改 ShipAddr；驗證 CustomerNo/ShipAddr NOT NULL、複合 PK 唯一及 Oracle 欄位長度。新/修改稽核由 server 產生，DataStamp compare-and-swap。
3. 報關端點：`GET/POST /api/ent/customer-basis/{customerNo}/ship-declarations`；`PUT/DELETE /{customerNo}/ship-declarations?shipTo=&flow=&overseaFlag=&declarationFlag=&bondFlag=&handCarryFlag=&dataStamp=`。原始表沒有主鍵且允許重複；自然定位值為七欄 CustomerNo、ShipTo、Flow、OverseaFlag、DeclarationFlag、BondFlag、HandCarryFlag。沿用舊 Core 對完全相同鍵列的多列更新/刪除效果，API 不得虛構 1:1 主鍵。DataStamp 新 API 可作併發檢查，但須處理來源 NULL。
4. 出貨地址刪除/修改在舊 `clsCustomerBasis` 未同步 cascade/update `TBLENTCUSTOMERSHIPDECLARATION`；兩表無 FK，故會有舊 ShipTo 孤兒值。先維持來源可觀察行為並在規格留下記錄，migration 不加 FK/cascade。
5. 表單的 Taker/Forwarder/Broker Lookup 分別依 WinForms `wsENT.LoadTakerBasis`、`LoadForwarderBasis`、`LoadBrokerBasis`，都只顯示 IssueState=2。統一使用 `GET /api/ent/taker-basis?issueState=2`、`/api/ent/forwarder-basis?issueState=2`、`/api/ent/broker-basis?issueState=2`；DTO 名稱欄位為 ContactorName。不要把 DEV picker 假資料回傳 Production。
6. API 驗證與日期/稽核欄位依規格實作，授予 `ymes_app` 最小 SELECT/INSERT/UPDATE/DELETE 權限，部署並通知公司端 pull 對接 Production。WinForms/Core 目前的 Edit 方法宣告不含 wsENT 傳入的 DataStamp/Reviser/ReviseDate 命名參數，依 Modern API 明確實作，不照搬來源版本差異。

## ENT ET103 報關行基本資料（公司端 2026-11-16 已交接）

先讀 `docs/modules/ENT/報關行基本資料.md`、`todolist_officepc.md` ET103 區塊，以及 `database/reference/mestest_customer_basis_source_ddl.sql`、`database/reference/mestest_customer_basis_create.sql`。公司端已核對 WinForms、直連取得 Oracle DDL，並完成 Development Vue 頁 `/YMES/Customer/Brokers`。ET103 與 ET101/102/106 共用 ENT 範圍；若 controller/repository 尚未建立，請合併在同一 ENT API 結構中。

### Schema / migration

- Oracle `MESTEST.TBLENTBROKERBASIS` 已直接查得；Oracle source logical DDL 在 `database/reference/mestest_customer_basis_source_ddl.sql`，PostgreSQL 空表 CREATE SQL 在 `database/reference/mestest_customer_basis_create.sql`。
- 欄位：`BROKERNO varchar(50) NOT NULL PK`、`CONTACTORNAME varchar(50)`、`TELNO varchar(25)`、`FAXNO varchar(25)`、`EMAIL varchar(255)`、`ADDRESS varchar(500)`、`BANKNO varchar(20)`、`ISSUESTATE numeric(1,0) DEFAULT 0` nullable、`CREATOR varchar(10)`、`CREATEDATE timestamp DEFAULT LOCALTIMESTAMP` nullable、`DATASTAMP numeric(11,0) DEFAULT 0` nullable、`REVISER varchar(10)`、`REVISEDATE timestamp`。
- 唯一索引是 PK `BROKERNO`；無 FK/其他索引。`mestest_first_phase_create_tables.sql` 已校正這張表原先漏掉的 DEFAULT。先比對現存 YMES table/migrations，只補 schema 缺項，不覆蓋或刪除現有擴充。

### API /簽核待辦

1. 建立/延伸 ENT controller：`GET /api/ent/broker-basis?brokerNo=&issueState=` 回傳欄位對應前端 `Broker` DTO 的清單。IssueState 選填：主畫面 Vue 在前端篩選，ET104 的 Broker picker 傳 `issueState=2` 僅取已核准報關行；回傳資料須包含 IssueState/DataStamp。WinForms 舊查詢沒有 ORDER BY；API 可以 BrokerNo 穩定排序，作為明確顯示約定。
2. 新增 `POST /api/ent/broker-basis`，BrokerNo 必填且大寫正規化；IssueState=0，Creator/CreateDate/DataStamp 由 server 設定。欄位長度照 Oracle DDL；其餘聯絡資料可空白，空值採明確 NULL 語意。
3. 新增 `PUT /api/ent/broker-basis/{brokerNo}?dataStamp=`，只可修改 ContactorName/TelNo/FaxNo/Email/Address/BankNo，伺服器必須確認 IssueState=0；更新 Reviser/ReviseDate 並遞增 DataStamp。失敗時區分 404 與 409。
4. 新增 `DELETE /api/ent/broker-basis/{brokerNo}?issueState=&dataStamp=`：只接受目前狀態 0、-1、2，需同時驗證資料庫實際狀態。0/-1 在交易中實體刪除；2 改為 -1，並更新 Reviser/ReviseDate/DataStamp；狀態 1 回 409。舊 WinForms 對 0/-1 實體刪除雖傳 DataStamp，但舊 WebMethod 沒有把 stamp 傳入 DELETE SQL；新 API 請仍做 compare-and-swap 避免過期刪除。來源 DataStamp 可為 NULL，要能以 `IS NULL` 比對，更新成 `COALESCE(datastamp,0)+1`。
5. 新增 `POST /api/ent/broker-basis/{brokerNo}/approve?dataStamp=`，只接受狀態 0，需使用 YMES 現有 Issue/Job 簽核服務及 IssueNo `BROKER BASIS`。沒有 JobMap/Detail 時按舊規則直接轉 2；有 Detail 時建立 Issue Master、狀態轉 1、簽核通過到 2、退回到 0，寄送第一簽核群組通知。主檔狀態、簽核單、稽核與通知需使用既有交易/簽核服務，不可將 DEV 直接核准複製到 Production。
6. 所有端點檢查登入者 ENT 權限；設定 `ymes_app` 最小 CRUD 權限。更新 migration/API 文件後部署並通知公司端 pull。公司端 Production Vue 不使用假資料。

## ENT ET104 貨運行基本資料（家用端A1已實作驗收；以下保留原交接來源）

正式query-key／JSON CAS與已測界線見本檔最上方ET104及[模組維護](docs/modules/ENT/貨運行基本資料.md)；下列path-parameter與完整Job建議屬原交接歷史，不能當成正式端點或已交付Job。

先讀 `docs/modules/ENT/貨運行基本資料.md`、ET103 報關行規格及 `todolist_officepc.md` ET104 進度。公司端已依 WinForms 原始碼完成 `/YMES/Customer/Forwarders` Vue DEV 頁，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得正式 DDL。ET104 與 ET101/102/106/103 共用 ENT API、schema/migration；不要另建重複 Controller。

### Schema / migration

- Oracle 原始 logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_customer_basis_create.sql`；first-phase reference 已同步修正 DEFAULT/欄位註解。
- `ymes.tblentforwarderbasis` 欄位與長度：ForwarderNo varchar(50) NOT NULL PK、ContactorName varchar(50)、TelNo varchar(25)、FaxNo varchar(25)、Email varchar(255)、Address varchar(500)、BrokerNo varchar(50) NOT NULL、BankNo varchar(20)、IssueState numeric(1,0) DEFAULT 0 nullable、Creator varchar(10)、CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable、ACNo varchar(50)、DataStamp numeric(11,0) DEFAULT 0 nullable、Reviser varchar(10)、ReviseDate timestamp。只有 ForwarderNo PK unique index；沒有 FK/其他 index。
- 先逐欄比對 YMES PostgreSQL schema 和 migration。缺表才新增 forward migration，已存在就保留擴充欄位，只補缺失 schema 差異；不可只依 `IF NOT EXISTS` 判斷已符合。執行 migration 前核對 DDL 不加 FK/index。

### API / 簽核待辦

1. 延伸 ENT API：`GET /api/ent/forwarder-basis?forwarderNo=&issueState=` 清單含完整欄位、IssueState/DataStamp；IssueState 選填供 ET106 lookup 篩選已簽核列；`POST /api/ent/forwarder-basis` 新增；`PUT /{forwarderNo}?dataStamp=` 修改 ContactorName/TelNo/FaxNo/Email/Address/BankNo/ACNo，ForwarderNo/BrokerNo 鎖定。
2. BrokerNo 必填，新增時查 `tblentbrokerbasis` 確認 BrokerNo 存在且 IssueState=2。ET103 `GET /api/ent/broker-basis` 必須支援選填 `issueState` query，讓本頁 picker 呼叫 `?issueState=2`；該 lookup 不可回 DEV 假資料到 Production。Oracle 無 FK，驗證必須在 API。
3. WinForms UI 允許 IssueState 0、2 開啟編輯，但舊 `wsENT.EditForwarderBasis` 對狀態 2 可能只回成功而不更新 Core。新 API 按 UI 意圖支援 0/2 的明細修改，鍵值不可改，1/-1 拒絕；記錄並避免重製此舊服務缺陷。
4. `DELETE /{forwarderNo}?issueState=&dataStamp=`：0/-1 交易中實體刪除；2 改 -1 並更新稽核/DataStamp；1 回 409。DataStamp 原值 NULL 時以 `IS NULL` compare-and-swap，遞增使用 `COALESCE(DataStamp,0)+1`。每筆端點單筆操作，前端多選逐筆送出、首錯停止並重載。
5. `POST /{forwarderNo}/approve?dataStamp=` 僅狀態 0，使用既有 Issue/Job 簽核服務及 IssueNo `FORWARDER BASIS`；無 Job/Detail 直接轉 2，有簽核 Detail 依流程轉 1、核准到 2、退回到 0。Production 不可照抄 DEV 直接核准。
6. 所有 API 驗證 ENT 權限、欄位長度、伺服器身分/時間 audit、DataStamp 併發。更新 migration/API 文件、授予 `ymes_app` 最小權限，完成 API 與既有簽核流程後部署，再通知公司端切 Production 驗收。

## ENT ET105 取貨公司基本資料（公司端已完成 DEV UI/Oracle DDL，待家用端接手）

先讀 `docs/modules/ENT/取貨公司基本資料.md`、`todolist_officepc.md` ET105 區塊與 ET101/ET103/ET104 ENT API 待辦。Vue DEV 頁 `/YMES/Customer/Takers` 已完成；Oracle DDL 是公司端透過 Windows PowerShell 5.1 + ODP.NET 直接唯讀取得。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_customer_basis_source_ddl.sql`；PostgreSQL CREATE SQL：`database/reference/mestest_customer_basis_create.sql`；`mestest_first_phase_create_tables.sql` 已補 IssueState/CreateDate/DataStamp defaults 並移除推測註解。
- 欄位：TakerNo varchar(50) NOT NULL PK、ContactorName varchar(50)、TelNo varchar(25)、FaxNo varchar(25)、Email varchar(255)、Address varchar(500)、BankNo varchar(20)、IssueState numeric(1,0) DEFAULT 0 nullable、Creator varchar(10)、CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable、DataStamp numeric(11,0) DEFAULT 0 nullable、Reviser varchar(10)、ReviseDate timestamp。
- 只有 TakerNo PK unique index，無 FK/其他 index。先逐欄比較 YMES schema/migration，缺表/欄位才建立 forward migration，保留已存在的 YMES 擴充，不加入來源沒有的 FK/index。

### API / 簽核待辦

1. 延伸共用 ENT API：`GET /api/ent/taker-basis?takerNo=&issueState=`、`POST /api/ent/taker-basis`、`PUT /api/ent/taker-basis/{takerNo}?dataStamp=`。IssueState 選填供 ET106 lookup 篩選已簽核列。TakerNo 必填、大寫、唯一；只有 IssueState=0 可修改 TakerNo 以外的聯絡資料；所有長度依 Oracle DDL。
2. `DELETE /{takerNo}?issueState=&dataStamp=`：0/-1 交易中實體刪除，2 改 -1，1 回 409。驗證 DB 實際狀態與 DataStamp；來源 DataStamp nullable，以 `IS NULL` 比對並以 `COALESCE(DataStamp,0)+1` 遞增。舊 WebMethod 0/-1 delete 沒有把 DataStamp 傳進 Delete SQL，新 API 必須補上併發檢查。
3. `POST /{takerNo}/approve?dataStamp=` 只接受狀態 0，IssueNo=`TAKER BASIS`，沿用 YMES Issue/Job 簽核服務；無 Job/Detail 時直達 2，有簽核 Detail 時建立簽核、狀態 1、通過 2、退回 0。
4. WinForms/WebService 與本 checkout 的 Core 參數不一致：wsENT Edit/SetIssueState 傳 DataStamp/Reviser/ReviseDate，但 `kcENT.clsCustomerBasis` 宣告沒有這些參數；Core Edit 不寫 audit/DataStamp，IssueState helper 也只更新狀態。Production 新 API 依規格完成稽核/併發，不複製舊缺漏。
5. 所有端點檢查登入者 ENT 權限；server 產生 audit 欄位與時間，使用 PostgreSQL transaction 和 compare-and-swap。更新 API/migration 文件、授予 `ymes_app` 最小權限，部署後通知公司端對接 Production。

## ENT ET201 外包商主檔與連絡人（公司端已完成 DEV UI/Oracle DDL，待家用端接手）

先讀 `docs/modules/ENT/外包商基本資料.md`。Vue DEV 頁為 `/YMES/Customer/Subcontractors`、FunctionNo `SUBCONTRACTOR BASIS`。UI/規格根據 `frmSubcontractor`、兩個 Define Form、wsENT/Core 及 Oracle 來源 DDL。

### Schema / migration

- Oracle source logical DDL：`database/reference/mestest_subcontractor_source_ddl.sql`；PostgreSQL 空表 CREATE SQL：`database/reference/mestest_subcontractor_create.sql`。只供逐欄比對 YMES 現況，不表示 YMES 尚無表。
- `ymes.tblentsubcontractor`: SubcontractorNo varchar(20) NOT NULL PK; SubcontractorName varchar(220) NOT NULL; Description varchar(255); Creator varchar(10); CreateDate timestamp; IssueState numeric(1,0) DEFAULT 0 NOT NULL; ERPNo varchar(50); DataStamp numeric(11,0) DEFAULT 0 nullable; Reviser varchar(10); ReviseDate timestamp。
- `ymes.tblentsubcontractorcont`: SubcontractorNo varchar(20) NOT NULL; ContactorName varchar(50) NOT NULL; TelNo/FaxNo varchar(20); Title varchar(20); Address/Email varchar(50); Description varchar(255); DataStamp numeric(11,0) DEFAULT 0 nullable; Creator varchar(10); CreateDate timestamp; Reviser varchar(10); ReviseDate timestamp; composite PK `(SubcontractorNo, ContactorName)`。
- 兩張 Oracle 表都僅 PK unique index，無 FK/其他 index。先比對實際 YMES schema/migrations；保留 YMES 額外欄位/索引，不自行加 FK。舊 Core 的實體刪除會先刪連絡人，再刪主檔，新的 API 需用同一 PostgreSQL transaction。

### API / 業務規則

1. 新增 ENT API：`GET /api/ent/subcontractor-basis?subcontractorNo=`、`POST /api/ent/subcontractor-basis`、`PUT /{subcontractorNo}?dataStamp=`、`DELETE /{subcontractorNo}?issueState=&dataStamp=`、`POST /{subcontractorNo}/approve?dataStamp=`。
2. 連絡人端點：`GET/POST /{subcontractorNo}/contacts`、`PUT/DELETE /{subcontractorNo}/contacts/{contactorName}?dataStamp=`。回傳完整 DTO 與 DataStamp；主鍵姓名只能在新增/複製時指定，修改時不可更換。
3. 主檔必填 SubcontractorNo/Name；新增狀態 0；只有狀態 0 可修改；編號與連絡人姓名大寫。長度依 Oracle DDL。連絡人必填 ContactorName，另外欄位皆可空。
4. 主檔刪除：狀態 0/-1 在一個交易內比對 DataStamp 後刪除連絡人及主檔；狀態 2 改 -1、更新 audit/DataStamp 且保留明細；狀態 1 回 409。批次是前端逐筆請求，遇錯停止並回報已完成筆數。
5. 核准只接受 IssueState=0，IssueNo=`SUBCONTRACTOR BASIS`。重用 YMES Issue/Job 簽核服務：無 Job/Detail 直達 2；有簽核明細則狀態 1，通過 2、退回 0。Production 不得照 DEV 直接核准。
6. DataStamp nullable/default 0；CAS 同時支援原值 NULL（`IS NULL`）及數值、遞增使用 `COALESCE(DataStamp,0)+1`。所有 API 檢查登入者 ENT 權限、主檔存在、欄位長度/唯一鍵與伺服器 audit。Core/wsENT 此 checkout 的命名參數存在版本差異，依本文件的新 API 契約統一補足 audit/CAS，不照搬未寫 audit 的舊 SQL。
7. 公司端尚未 build/test/瀏覽器驗收。API/schema 完成並部署後，通知公司端 pull 並對接 Production 驗收。

## ENT ET202 外包項目設定（公司端 DEV 完成，家用端接手 API/schema）

先讀 `docs/modules/ENT/外包項目設定.md`、`todolist_officepc.md` 的 ET202 區塊，以及 Oracle 來源 `database/reference/mestest_entositem_source_ddl.sql`、PostgreSQL 空表 `database/reference/mestest_entositem_create.sql`。公司端按 WinForms `frmENTOSItem`/`frmENTOSItemDef` 完成 Vue DEV 頁 `/YMES/Customer/OSItems`。Oracle DDL 是公司端以 PowerShell 5.1 + ODP.NET 直連受保護連線唯讀取得。

### Schema / Migration

- Oracle 主表 `MESTEST.TBLENTOSITEM` 11 欄：OSItemNo varchar(2) NOT NULL PK；OSItemName varchar(50) NOT NULL；PlanDays numeric(3,0) NOT NULL；ProcessNo varchar(30) NOT NULL；ReturnType numeric(1,0) NOT NULL；Description varchar(255)；Creator varchar(10)；CreateDate timestamp；DataStamp numeric(11,0) DEFAULT 0 nullable；Reviser varchar(10)；ReviseDate timestamp。
- Oracle 只有 OSItemNo PK unique index，沒有 FK 或額外索引。Create SQL 是空表定義；先查 YMES 現有 schema、migration 和 API 使用的 schema 大小寫，再只建立缺漏項目，不要盲目執行 IF NOT EXISTS。
- Oracle 沒有 IssueState。PlanDays、ReturnType 不可另加來源不存在的欄位/default；Oracle DATE 對應 PostgreSQL timestamp without time zone。

### API / 業務規則

1. ENT API：`GET /api/ent/os-items?osItemNo=`、`POST /api/ent/os-items`、`PUT /api/ent/os-items/{osItemNo}?dataStamp=`、`DELETE /api/ent/os-items/{osItemNo}?dataStamp=`。GET 空條件列全體，有值依編號查詢。JSON 欄位為 `osItemNo, osItemName, planDays, processNo, returnType, description`；POST/PUT 回傳完整 DTO。
2. 必填與長度依 Oracle DDL；WinForms 輸入上限 OSItemNo 2、OSItemName 50、PlanDays 0–999 整數、ProcessNo 25（DB 上限 30）、Description 255。OSItemNo 儲存前大寫；ReturnType 只接受 0/1，新增預設 0。
3. ProcessNo 必須存在於已簽核 `IssueState=2` 且 `ProcessClass=3` 的流程。查找用既有 `GET /api/processes?issueState=2`，以 DTO ProcessClass 篩選；舊 WinForms LoadProcess 未限制 CurVersion=1，不要額外只留目前版本。
4. DataStamp nullable/default 0；Update/Delete 需 transaction/CAS，支援 NULL 原值和數值，更新 `COALESCE(datastamp,0)+1`。新增/更新的 Creator/CreateDate/Reviser/ReviseDate 由 API 設定，不接受用戶端 audit。
5. 舊 ws Edit 有送 DataStamp，但目前 Core `EditENTOSItem` 簽章沒有 CAS/audit；舊 Core Delete 只按 OSItemNo 實體刪除。新 API 補完整 CAS 與伺服器 audit。多選刪除由前端逐筆呼叫，首個錯誤即停止並回報已完成筆數。
6. 本功能沒有簽核/IssueState；所有端點檢查登入者 ENT 權限、資料庫欄位長度、必填、ReturnType/PlanDays 範圍與流程參照。
7. 公司端 Development 使用 localStorage 假資料；Production 只呼叫 API。API/schema 完成、部署並提供可用環境後，通知公司端 pull 最新版對接驗收。
## SMD SD107 生產線別基本資料（公司端 DEV 已完成，家用端接手 API/schema）

- [x] 2026-11-17 遠端 schema 核對與 migration：`TBLSMDPDLINEBASIS` 已存在，欄位型態、長度、NULL 與 PDLineNo PK 符合來源；只補 PDLineStatus、CreateDate、DataStamp 來源 DEFAULT 與本功能 CRUD 權限，未重建表。Migration 前備份 `/opt/ymes/backups/ymes_before_0018_smd_pdline_20261117.dump`，`pg_restore --list` 驗證通過。
- [x] 完成並部署 GET/POST/PUT/DELETE `/api/smd/pd-line-basis`，全狀態查詢、名稱/說明驗證、伺服器 audit、CAS、狀態 2 停用及舊規則狀態 0/1/-1 實體刪除。
- [ ] 正式 PDLineStatus ValueList 與共用 Issue/Job 核准結構尚未交付；目前 `/options` 與 `/approve` 分別回 503 fail closed。收到公司端核實項目前，不推定正式標籤、不以 0→2 假核准。

先讀 docs/modules/SMD/生產線別基本資料.md。Vue 頁為 /YMES/SMD/ProductionLines，FunctionNo=SMD PDLINE BASIS；公司端以本機假資料完成操作，不代表 Production API 已存在。

### Schema / migration

- Oracle source DDL：database/reference/mestest_smd_pdline_basis_source_ddl.sql；PostgreSQL 空表 CREATE SQL：database/reference/mestest_smd_pdline_basis_create.sql。DDL 是公司端 2026-11-16 以 Windows PowerShell 5.1 + ODP.NET 直連受保護連線唯讀取得；不要重問連線資訊，也不要在 MES Oracle 建表。
- Oracle 表 MESTEST.TBLSMDPDLINEBASIS 欄位：PDLineNo varchar(50) NOT NULL PK；PDLineName varchar(50) NOT NULL；PDLineStatus numeric(1,0) DEFAULT 1 NOT NULL；IssueState numeric(1,0) NOT NULL 且無 default；Description varchar(255)；Creator varchar(10)；CreateDate timestamp DEFAULT LOCALTIMESTAMP nullable；Reviser varchar(10)；ReviseDate timestamp；DataStamp numeric(11,0) DEFAULT 0 nullable。
- Oracle 僅一個 PDLineNo PK unique index，沒有 FK 或其他索引。DATE 對應 timestamp without time zone。此表已出現在 first-phase reference；先查 YMES 實際 schema/migration。缺漏才新增 forward migration，保留既有資料和 YMES 擴充欄位；如 first-phase 曾被套用且 defaults 不符，採 forward migration 修正，不重建表。
- PostgreSQL CREATE 檔是空表定義，不是部署 migration；不要無條件執行 IF NOT EXISTS 取代欄位差異檢查。

### API / 行為

1. 新增 SMD API：GET /api/smd/pd-line-basis?pdLineNo=（回傳所有四種 IssueState，PDLineNo 大小寫不敏感部分查詢，依 PDLineNo 排序）；GET /api/smd/pd-line-basis/options 回傳 { pdLineStatuses: [{ value, label }] }。狀態選項需沿用正式 ValueListProvider，不能把 DEV 標籤當正式文案。
2. POST /api/smd/pd-line-basis 新增 IssueState=0；PDLineNo、PDLineName 必填，長度各 50；PDLineNo 大寫，PDLineStatus 只接受 0/1/2，WinForms 新增預設 1；說明上限 255。Creator/CreateDate/DataStamp 由伺服器建立。
3. PUT /api/smd/pd-line-basis/{pdLineNo}?issueState=0&dataStamp= 只允許 IssueState=0，主鍵不可改；更新名稱/狀態/說明並由伺服器寫 Reviser/ReviseDate/DataStamp。舊 Core 空白 Description 不更新也不清除；需維持相容。
4. DELETE /api/smd/pd-line-basis/{pdLineNo}?issueState=&dataStamp= 按舊 wsSMD 規則：0、1、-1 實體刪除，2 轉為 -1 並寫修改稽核/DataStamp。注意此功能舊 Service 明確接受 IssueState=1 實體刪除；若要改成阻擋簽核中刪除，需在 API 文件標示行為修正。交易/CAS 需檢查傳入與實際狀態。
5. POST /api/smd/pd-line-basis/{pdLineNo}/approve?issueState=0&dataStamp= 僅接受 0，IssueNo=SMD PDLINE BASIS。使用 YMES 共用 Issue/Job 簽核服務：無 Job/Detail 依設定直達 2；有流程時先 1，核准 2、退回 0，通知第一簽核群組。不可照搬 DEV 直接切成 2。
6. 所有端點檢查登入與 SMD 功能權限、唯一鍵/欄位範圍及資料狀態。DataStamp nullable/default 0；NULL 原值需以 IS NULL CAS，更新遞增 COALESCE(datastamp,0)+1。批次 UI 逐筆呼叫，第一個錯誤停止並保留已成功筆數。
7. API/schema/migration 完成並部署後，通知公司端 pull 最新 main 對接 Production；不要把假資料回退用於 Production。

## SMD SD108 設備狀態監控_區域（公司端 UI handoff）

- 公司端已新增 Development Vue 頁與規格：docs/modules/SMD/設備狀態監控_區域.md、web/src/components/AreaShowMaster.vue。Production 不使用 localStorage。
- Oracle 表 DDL/PG 空表 SQL：database/reference/mestest_smd_area_shift_source_ddl.sql、mestest_smd_area_shift_create.sql，表為 TBLSMDAREASHIFT、TBLSMDAREAOPERATOR。先比對 YMES schema；只對缺漏新增 migration。
- 實作班別/人員 API、DataStamp CAS、以 transaction 同步 Operator 明細、複製班別、刪除主從資料、同部門有效人員驗證及舊 ChkAreaShiftLoginState 規則（tblWIPAreaOperatorState 有登入即拒絕修改）。當來源 DataStamp 為 NULL，API 接收 `dataStamp=0&dataStampIsNull=true` 並以 `IS NULL` CAS；有值時則比較該值。
- SD104 layout API 仍未完成；SD108 首頁依既有 GET /api/smd/areas/{areaNo}/layout 讀配置。另需 BaseArea lookup 與 shift-context/shift-options API。完整端點與 DTO 見 SD108 規格。
- WIP 即時狀態與歷史由既有 WP_AreaOperatorState/WP_AreaOperatorLog 提供；補查 YMES WIP API 後沿用或實作 GET /api/wip/areas/{areaNo}/operator-state 與 /operator-log。不要將 DEV 假資料當正式狀態。

## SMD SD109 工位基本資料（公司端 UI handoff）

- 公司端已新增 Development Vue 頁與規格：docs/modules/SMD/工位基本資料.md、web/src/components/PositionBasisMaster.vue。Production 不使用 localStorage。
- Oracle TBLSMDPDLINEPOSITIONBASIS 的來源 DDL/PG 空表 SQL 已加入 reference；先比對實際 YMES schema，缺漏/預設差異才新增 migration。
- 實作複合鍵 (PDLineNo,PositionNo) GET/POST/PUT/DELETE、DataStamp CAS、audit，並驗證 PDLineNo 為已核准一般作業區（AreaClass=0、AreaType=0、IssueState=2）。DataStamp 為 NULL 時按 `dataStamp=0&dataStampIsNull=true` 以 `IS NULL` CAS；API 路徑/DTO 見 SD109 規格。
- eSOP 選項由 Execute_eSOP 系統參數控制；瀏覽器不可直接呼叫 eSOP，請由 API 使用現有 ERP/EAI 設定代理 station.get。
- first-phase CREATE SQL 已修正 CreateDate DEFAULT LOCALTIMESTAMP NOT NULL、DataStamp DEFAULT 0；家用端實際 schema 仍須用 migration 比對與修正，不要重跑 bootstrap。

家用端完成後更新本檔與兩份模組規格，部署 API/schema，再通知公司端 pull 進行 Production 對接。


## MTL ML106 物料品管項目設定（公司端 2026-11-17 交接）

- [x] 2026-11-17 遠端比對：`TBLMTLQCITEM` 欄位/複合 PK 符合 reference；三個 QC catalog、`TBLQCITEMBINDLOG`、`TBLOPQCITEM` 缺表。備份 `/opt/ymes/backups/ymes_before_0019_mtl_qc_20261117.dump` 已由 `pg_restore --list` 驗證；0019 migration 已套用，`ymes_app` 取得目錄 SELECT、IQC mapping INSERT、bind log INSERT 權限。
- [x] 擴充 `GET /api/materials` 的 `materialNoPrefix`，新增 ML106 專用 `GET /api/materials/{no}/qc-item-library`，保留既有 `/qc-items` 類別範本回應。
- [x] PUT 在單一交易驗證已核准 QC 目錄、替換直接綁定、補 `IQC` 虛擬站項目、寫 BindType=2 新增/移除事件及 server audit；依來源 WinForms 行為不限制物料 IssueState。
- [x] .NET 9 API Release build 0 warning/0 error；只重建 `ymes-api`，遠端 readiness 200 且容器 healthy，`ymes-web` 與其他容器未重啟。
- [ ] app-browser 直接開 `/YMES/MTL/MaterialQCItems` 後顯示主選單；尚未確認是否是 Production Web 尚未更新或功能授權/路由狀態，留給公司端核對 UI route。

公司端頁面 `/YMES/MTL/MaterialQCItems` 已完成 Vue DEV UI；WinForms Form/Designer、wsMTL、clsMTLBasis、clsQCBasis 已對照。Production schema/API 已由家用端完成並部署；Production UI route 待公司端查明。

### Schema / DDL

- Oracle DDL 由公司端 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過既有受保護連線唯讀查詢 MESTEST，DDL 檔：database/reference/mestest_mtl_qc_item_tables_source_ddl.sql。連線秘密仍存既有保護設定，不複製到 repo；不要再問使用者 IP/帳密，也不要在 Oracle 執行 CREATE。
- PostgreSQL 空表參考檔：database/reference/mestest_mtl_qc_item_tables_create.sql，含 tblqcitemforattributes、tblqcitemforvariables、tblqcitemgroupbasis、tblqcitembindlog。先查 YMES PostgreSQL 實際 schema/table/columns/indexes 和 migration；若符合就沿用，缺表才加 migration，缺欄位/約束才 forward migration。不要盲目套用 IF NOT EXISTS。
- TBLMTLQCITEM 在 mestest_mtl_master_create.sql；TBLOPQCITEM 在 mestest_operation_and_system_create.sql。這兩張先比對現有 schema，不要重建。
- 四張新查 DDL 表各只有 Oracle DDL 列出的 PK，沒有 FK 或其他 index。Oracle DATE 用 timestamp without time zone，NUMBER 精度及 nullable/default 按 source DDL 保留。
- 授予 ymes_app 本功能必要 SELECT/INSERT/UPDATE/DELETE 權限；避免為查目錄或寫 log 開放多餘權限。migration 前照既有家用端流程備份並留下部署紀錄。

### API / Service

1. 擴充物料清單 GET /api/materials 支援選填 materialType 精確條件及 materialNoPrefix 前綴條件。ML106 的查詢不限制 IssueState；materialType/materialNoPrefix 皆空時 API 需回傳完整清單以供 WinForms 等價 MaterialNo 查找視窗使用，Vue 主畫面 Query 按鈕自行要求至少一個條件。保留既有 keyword、issueState 呼叫行為。
2. 新增 GET /api/materials/{no}/qc-item-library，回傳 { available, assigned }：available 以 IssueState=2 分別讀三個 QC catalog，輸出 qcItemNo/qcItemName/qcItemType（0 Attributes、1 Variables、2 Group）；assigned 讀 tblmtlqcitem 並依型別左 Join 正確 catalog 取得名稱，保留無法 Join 的舊綁定供移除，輸出 reviser/reviseDate。
3. 保持既有 GET /api/materials/{no}/qc-items 的 type-template 行為，因 MaterialFeatureDialog.vue 依賴它；不可把其 available 換成 catalog 全表。前端新增的 library GET 路徑已獨立。
4. 既有 PUT /api/materials/{no}/qc-items 接受 {items:[{qcItemNo,qcItemType}]} 並寫同一張 tblmtlqcitem。新增來源目錄驗證、完整交易及舊 WinForms 副作用：替換物料現有綁定；對各新 QC Item 用 INSERT ... SELECT/等價原子方式確保 tblopqcitem 的 opno='IQC'、inline=0、executeorder=0 存在且不重複；寫 tblqcitembindlog，BindType=2，刪除事件 ExecuteStatus=2、新增事件 ExecuteStatus=0，記錄 MTLNo、QCItemType/No、ExecuteDate、Executor。QCItemLogSerial 由伺服器產生唯一值，不建 Oracle sequence。
5. Audit 由登入者與伺服器時間設定；同一 transaction 更新 assignment、IQC 關聯與 bind log。先核對目前 API 的 IssueState=0 寫入限制是否符合舊 WinForms；若為既有 MaterialFeatureDialog 必需，保持既有契約且明確處理兩種 UI 的合法狀態，不可意外禁止舊資料修正。
6. TBLQCITEMBINDLOG 在 YMES 若缺少才使用 reference SQL 建表；如現有 schema 有擴充，保留它並用 forward migration 對齊必要欄位。其複合 PK 順序為 (QCITEMNO, BINDTYPE, QCITEMLOGSERIAL)，不能自行改成其他順序。
7. 完成 migration/API/權限後部署，更新 API 文件、migration index、本待辦；通知公司端 pull。公司端會對照 ML106 頁面核對 response、錯誤、全狀態搜尋與儲存副作用。

## 2026-11-17 ML103 物料基本資料 API 選項交接

- ML103 主資料、屬性、QC、供應商、GumHour 等 Oracle DDL/PG CREATE 已在 database/reference/mestest_mtl_master_* 建立並由 0013 migration 比對/部署；本次公司端沒有發現新表，不新增 SQL 或 migration。
- 公司端 ML103 多選核准/刪除會對每筆資料依序呼叫既有 POST /api/materials/{no}/approve 與 DELETE /api/materials/{no}?issueState=...&dataStamp=...；不需要額外批次 API。請確認現有單筆 API 的簽核檢查、引用檢查、DataStamp、稽核與狀態轉換和 WinForms 相同。已檢視 PostgresMasterDataRepository.ApproveMaterial：目前有 Controlled=1、DataStamp、IssueState 檢查，但缺少 WinForms 的條件式 GumHour 設定檢查；需在服務端補上。
- GET /api/materials/lookups/options 現況由 MaterialFeaturesController.Options() 回傳 countWay=[]、checkLotNo=[]，且沒有 MATERIALMULTITIMECTRL / GumMaterialType。請從公司端 WinForms ValueListProvider.GetValueList("TBLMTLMATERIALBASIS", ...) 與 wsSYS.LoadParameter 核實真實值，不要以 DEV 樣本猜測；將 CountWay、CheckLotNo 顯示選項與 GumHour 參數設定加入 DTO/API response。若參數已有 YMES 系統設定來源，沿用既有結構，不新建設定表。
- 完成後更新 MaterialOptionsDto、API 文件與本待辦。公司端 UI 已能讀取選項；多次回溫參數未提供時 Production 會隱藏 GumHour，避免對非適用物料開啟設定。


## QC203 計數品管項目判別法則對應（公司端 UI / API handoff，2026-11-17）

先讀 docs/modules/QC/計數品管項目判別法則對應.md、docs/DATABASE_CONNECTION.md 與本節。公司端已完成 Vue DEV UI /YMES/QC/AttributeRuleMap；Production API、YMES migration、schema 比對及部署尚未完成。FunctionNo=QCITEM_A RULE MAP，FunctionKey=QC203，IssueNo=QCITEM_A RULE MAP。

### Oracle DDL / PostgreSQL schema

- 公司端以 PowerShell 5.1 + Oracle.ManagedDataAccess，使用本機受保護的 MES Oracle 設定直連 MESTEST，只查 DBMS_METADATA/ALL_TABLES/index metadata，沒有查業務列或執行 Oracle DDL。Oracle source DDL：database/reference/mestest_qc_attribute_rule_map_source_ddl.sql；同一批表亦在 mestest_qc_item_attributes_source_ddl.sql。
- 三張來源表為 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER。來源 PK 分別為 QCAttSPSerial、(QCAttSPSerial,RetestLot)、(QCAttSPSerial,RetestLot,ParameterNo)；主表另有非唯一 QCItemNo index；三表沒有 Oracle FK。
- PostgreSQL 空表 CREATE 參考：database/reference/mestest_qc_attribute_rule_map_create.sql。先比對 YMES live schema、已部署 migration、已存在的共享 ID/sequence service，再決定 forward migration；不可直接執行 reference 取代已有表。
- Oracle 主表使用動態 FactorNo 欄位；CREATE 參考以 factorvalues JSONB 適配。API 只能接受該 QCItem 已核准 FactorOnline=Y 的 FactorNo 白名單，不能將傳入名稱拼成 SQL 欄位。精確重複組合由 (qcitemno,factorvalues) 防重；來源其他非該 QCItem 因子欄語義為 *。
- Oracle 用 TBLSYSSERIALNO 的 QCAttSPSerial 產生 SP 加 8 位數 serial。CREATE reference 內的 qcattspserial_seq 是建議 ID 來源，先確認 live YMES 的共享 serial service 與既有 serial 最大值；禁止不檢查就從 1 使用。來源沒有 DataStamp、Reviser、ReviseDate；不要冒稱為 Oracle 欄位。若 YMES 新增版本欄位，必須另做 schema 適配說明。

### API / transaction

1. 全部端點驗證已登入、QC203 權限、資料狀態、欄位長度、已核准 lookup 來源及 factor whitelist；使用參數化 SQL。因舊服務以多句 SQL 全量替換 Detail/Parameter，新增、修改、刪除必須在同一交易原子完成。
2. GET /api/qc/attribute-rule-maps?qcItemNo=&issueState= 回傳主 Grid rows：qcAttSpSerial、qcItemNo、issueState、creator、createDate、factorValues、canEdit。canEdit 依來源 ExeIssueControl/TBLSYSISSUECONTROL 規則計算，供 Vue 表單決定唯讀狀態。
3. GET /api/qc/attribute-rule-maps/{qcAttSpSerial} 回傳 { basis, details, parameters }；Detail 依 RetestLot 排序，Parameter 按 RetestLot/ParameterNo 排序。
4. GET /api/qc/attribute-rule-maps/options?qcItemNo= 回傳 qcItemRuleNo、componentCollect、已核准屬性法則、一般與元件抽樣計劃、FactorOnline 因子選項與 MES Parameter Reference 值、planParameters 對應表。一般 PlanNo 取已核准 TBLQCSAMPLINGPLAN；ComponentCollect 非 0 取已核准 TBLQCSAMPLINGPLAN_WAFER。法則取已核准 TBLQCATTRULEBASIS，依 ComponentCollect 0/1 分組。每種值的正式來源先核 WinForms/既有 YMES API，不要沿用 DEV 樣本。
5. POST /api/qc/attribute-rule-maps body { qcItemNo, factorValues, details:[{retestLot,planNo,lotRuleNo,comRuleNo}], parameters:[{retestLot,parameterNo,parameterValue}] }。伺服器生成 SP########、IssueState=0；QC Item 必須已核准且 RuleNo 不可空，FactorOnline 至少一項，所有要因值必填；Detail 至少一筆；每筆 Detail 必填 PlanNo 且至少一筆 Parameter。ComponentCollect=0 且 PlanType=2 的 AQL 可以不填 LotRuleNo；其他一般 Plan 必填 LotRuleNo；元件模式至少填 LotRuleNo 或 ComRuleNo。驗證 RetestLot unique、同 RetestLot ParameterNo unique。對全部既有狀態做來源等價重複檢查；資料列/所有因子內容必須有相同語義才算精確 duplicate。序號需安全並行生成。
6. PUT /api/qc/attribute-rule-maps/{qcAttSpSerial} 的 Vue request 僅送 details/parameters；API 不允許改 qcItemNo/factorValues/serial。依 ExeIssueControl：IssueState=0 可編輯；IssueState=1 唯讀；IssueState=2/-1 依 TBLSYSISSUECONTROL，無設定視為唯讀。交易內先驗證狀態，再替換所有 detail/parameter，保留 IssueState。來源 Core 會保留原 Creator 並將 CreateDate 更新為伺服器目前時間；若採正規 Reviser audit 或 CAS 是 YMES 行為改善，須在 API 文件記為適配，不得宣稱來源有這些欄位。
7. DELETE /api/qc/attribute-rule-maps/{qcAttSpSerial}?issueState= 只接受來源允許刪除的 0、-1、2，Pending=1 拒絕。舊 Core 刪 Basis 和 Detail 卻漏刪 Parameter；新 API 應在一個交易刪三表，並以實際資料狀態/CAS 防止競態。Vue 多選逐筆呼叫，首錯停止並回報已完成筆數。
8. POST /api/qc/attribute-rule-maps/{qcAttSpSerial}/approve?issueState=：接共用 Issue/Job，IssueNo=QCITEM_A RULE MAP；沒有 Job/Detail 時直接 2，有流程先 1，核准 2、退回 0，依既有 service 寫事件並通知第一簽核群組。WinForms UI 對 0/-1 都呼叫服務，但舊 wsQC 僅在傳入 0 時執行；-1 實際是成功 no-op。API 應拒絕 -1 或回報未變更，不能表示已核准。
9. UI options/lookup 必須回正式資料：GET /api/qc/attribute-qc-items?issueState=2、GET /api/qc/attribute-qc-items/{qcItemNo}/factors?factorOnline=Y（QC201 已有），以及 QC203 options 提供 MES parameter reference、已核准 plans/rules/plan parameters。任何 API 不可將用戶端檔案路徑或因子名稱當 SQL。
10. 先完成 live schema 比對與有資料備份，再採 forward migration；保留現有擴充欄位和資料。部署 API/schema 後更新 API/migration 文件、本 TODO 並 push，通知公司端 pull 以 Production 驗收主/Detail/Parameter、Issue Job 與錯誤狀態。


## QC202 計數管制內容（公司端 UI handoff，2026-11-17）

- 公司端已依 WinForms frmSPCAttLimit / frmSPCAttLimitDef、wsQC、clsQCBasis / clsBasis_UPD 建立 Vue DEV 頁 /YMES/QC/AttributeControlLimit，三層選單為「品質資料 → 品管設定 → 計數管制內容」。主 Grid 固定載入 IssueState=2 QC Item；管制內容可動態列出 FactorOnline 欄位，支援新增/修改/刪除/複製、規則集、界限、Apply/Confirm、多選刪除。
- Oracle TBLSPCATTLIMIT DDL 已由公司端 PowerShell 5.1 + Oracle.ManagedDataAccess 直接查 MESTEST 後，納入 QC201 DDL 擷取；單表 reference 為 database/reference/mestest_qc_spc_att_limit_source_ddl.sql。PG schema 參考 database/reference/mestest_qc_spc_att_limit_create.sql 以 factorvalues JSONB 取代動態欄位，先核對 YMES live schema/migrations 再決定 migration，不可照 Oracle 動態欄位實作任意 ALTER。
- API：GET api/qc/attribute-control-limits?qcItemNo=、GET .../options?qcItemNo=、POST collection、PUT /{qcItemNo} body 帶 originalFactorValues/dataStamp、DELETE /{qcItemNo} body 帶 factorValues/dataStamp。完整 DTO、FactorOnline 白名單、RuleSet 格式、nullable DataStamp CAS 見 docs/modules/QC/計數管制內容.md。
- 因子沿用既有 QC201 Factor API，查找值沿用 MES parameter reference；法則從 TBLQCRULEBASIS 讀 IssueState=2。API 所有端點需檢查登入與 FunctionKey QC202，SQL 參數化；新增以 QCItemNo + 正規化 factorValues 防重，修改/刪除以 composite key + DataStamp CAS，NULL DataStamp 使用 IS NULL。勿把 FactorNo 直接拼成 PostgreSQL 欄位名。
- TBLSPCATTLIMIT Oracle source 無 PK/FK/唯一鍵，只有 QCITEMNO index；WinForms 客戶端新增時先查重。PG CREATE reference 加入 composite unique constraint 作 server-side 防重，屬 YMES 適配，請在實作前確認與既有架構相容。
- [x] 讀取 QC202 規格、source DDL、PG JSONB 參考及公司端 handoff；家用端 SSH live audit 確認 `tblspcattlimit` 缺表，`tblqcitemforattributes`/`tblqcitemfactor` 已存在，`tblqcrulebasis`/`tblsysmesparameter` 未建立。
- [x] 建立並套用 `database/migrations/0022_qc202_attribute_control_limit.sql`；套用前建立並驗證 `/opt/ymes/backups/ymes_before_0022_qc202_20261117.dump`。只新增 `tblspcattlimit`、JSONB 因子字典、唯一鍵、QCItemNo 索引及 `ymes_app` CRUD 權限；遠端欄位、索引、grant 已核對，未新增 Oracle 動態欄位或匯入業務資料。
- [ ] 本機已新增 QC202 API routes/repository：approved QC Item 限值查詢、FactorOnline 欄位白名單、JSONB 新增/修改/刪除、界限驗證、RuleSet 格式/狀態驗證及 nullable DataStamp CAS；Release build 成功，尚未部署。因 `tblsysmesparameter` 缺少，FactorName/ParameterType/參照選項尚不能完整供應；目前非 ParameterType=2 因子只允許 `*`。因 `tblqcrulebasis` 缺少，法則 options 為空、非空 RuleSet 寫入回 503。
- [ ] 取得/建立正式 MES Parameter reference 與 QC108 Rule lookup 後補齊 options，再部署並用 Production 頁逐項核對新增、修改、複製、多選刪除與 nullable DataStamp 衝突行為。


## QC204 刪除計數型品管項目量測記錄（公司端 UI handoff，2026-11-17）

先讀 `docs/modules/QC/刪除計數型品管項目量測記錄.md`。公司端已完成 Vue DEV 頁 `/YMES/QC/AttributeMeasurementDelete`。FunctionNo=`QCITEM_ADELETE`、FunctionKey=`QC204`；此為刪除量測日誌操作，不是刪除 QC Item 主檔。

### YMES schema / DDL

- QC204 不新增資料表。Oracle reference `database/reference/mestest_qc_item_attributes_source_ddl.sql` 已包含 `TBLQCITEMFORATTRIBUTES`、`TBLSPCATTBASISLOG`、`TBLSPCATTDETAILLOG`、`TBLSPCATTEDITLOG`、`TBLSPCATTDETAILLOG_TAPE` 的來源 DDL/索引。
- 先查 YMES PostgreSQL 現有 schema 和 migration，確認 QC201 日誌 API 所用表名/欄位。保留已部署 schema 與資料；只有確定缺表時才依既有 Oracle source DDL 做 YMES forward migration/Create SQL 並記錄適配，勿在公司端臆造或重複建表。

### API

1. Vue 清單依序讀取 `GET /api/qc/attribute-qc-items?issueState=-1`、`GET /api/qc/attribute-qc-items?issueState=0`，依 WinForms 順序合併；QC201 list DTO 必須包含畫面欄位、`FormName`、`IssueState`。目前 API 專案若尚未實作 QC201 list，需一併完成此依賴。
2. 新增 `POST /api/qc/attribute-qc-items/{qcItemNo}/measurement-log-deletions`，body `{ issueState }`；成功回傳 QCItemNo、刪除表/筆數。端點檢查登入、FunctionKey QC204 權限、QC Item 存在、真實 IssueState 與請求一致且限 0/-1；不得只依賴前端資料。
3. 以資料庫讀到的 FormName 判斷是否為 `frmAtt_OnlineControlMeasureDataForTape`。不得接受用戶端的 FormName/table name/SQL。
4. 先確認 `TBLSPCATTBASISLOG` 有該 QCItemNo 資料；無資料回 404/409、使用者可讀錯誤。存在時，在單一 transaction 刪除 BasisLog、DetailLog、EditLog；Tape Form 另刪 Tape DetailLog。所有條件參數化、整筆 operation rollback on error。
5. 相容舊 Core：`Log=Log` 模式不刪 QC Item/Parameter/Factor/Reason/Group 或其他設定，並略過一般 OP/WIP 使用檢查。業務目標是刪除量測日誌；不得擴大刪除範圍。
6. 舊 Core 在成功刪除後丟 `0000-000001`，舊 WebService 包成失敗。本 API 應回 2xx 成功，不複製此錯誤包裝；無資料回錯誤。多選由 Vue 逐筆依清單順序呼叫，首錯停止並回報已完成筆數。
7. 成功/失敗均記錄目前登入者、QCItemNo、時間、電腦/請求識別與實際刪除表筆數。避免日誌內容或連線設定被寫入應用 log。

### 完成後

- 更新 QC204 API、schema/migration 文件及本 TODO；若需要新增 migration，列出已比對的 live table，保留既有欄位/索引。
- Push 到 GitHub main，通知公司端 pull 後以 DEV UI 接 Production API 驗收：0/-1 狀態、一般日誌、Tape 特例、無記錄、transaction 失敗回復、多選中途錯誤與權限拒絕。
- 公司端已完成 QC205 Vue DEV 頁並交付家用端 API/schema 工作，詳細清單見本檔「QC205 計量品管項目 API/schema handoff」。
## QC205 計量品管項目 API/schema handoff（公司端 2026-11-17）

公司端已完成 `/YMES/QC/VariableQcItem` Vue DEV 畫面、三層選單、WinForms/Service/Core 規格與 Oracle metadata 參考。先 pull 最新 main，再閱讀 `docs/modules/QC/計量品管項目.md`、`docs/DATABASE_CONNECTION.md`、本段與 Create SQL；此段不是部署授權，也不代表 live schema 已確認。

### Schema / Create SQL

- Oracle source DDL 僅供參照：`database/reference/mestest_qc205_theil_custom_source_ddl.sql`、`mestest_qc205_variable_dependencies_source_ddl.sql`。MESTEST 沒有 `TBLSPCQCHARTDETAIL`、`TBLSPCQCHARTDETAILLOG`、`TBLSPCQCHARTRULE`、`TBLSPCQCHARTBASIS` 四表；追查 YMES 等價表/實際程式路徑，勿直接建立四張替代表。
- QC205 主表 `TBLQCITEMFORVARIABLES` 的 Oracle/PG 結構沿用 `mestest_mtl_qc_item_tables_source_ddl.sql`、`mestest_mtl_qc_item_tables_create.sql`；Parameter、Factor、SamplePlanChange 沿用 QC201 `mestest_qc_item_attributes_source_ddl.sql`、`mestest_qc_item_attributes_create.sql`。先檢查 live PostgreSQL schema、已部署 migration 與 API DTO，禁止重複建表或覆蓋既有資料。
- THEIL Memo 的 Oracle 原表是 `XX_TBLQCITEM_MEMO`；PG CREATE 參考在 `database/reference/mestest_qc205_theil_custom_create.sql`，含 YMES composite PK 適配。若 live schema 尚無等價 memo mapping，再新增 forward migration 和 rollback；保留 `qcitemtype=0` 的來源行為。
- `XX_TBLSPC_TEMPDATA_VAR` 與 `TBLSPCVARDETAILLOG` Oracle 欄位含動態 ParameterNo 欄名；不要在 PostgreSQL 動態 ALTER 欄位。依既有量測 log API/schema 以 JSONB 或正規化子表承接；先查目前 schema 再決定需要的遷移。
- 記錄 live schema 對照結果與 migration 使用的既有表/欄位。Create reference 不能未比對就直接當 production migration 執行；備份、部署遵循既有家用端流程。

### API 工作

1. 新增登入保護的 QC205 API controller/service/DTO，路徑與 payload 依 `docs/modules/QC/計量品管項目.md`。Vue 使用的端點包含列表/options、主檔 CRUD/Copy/Delete/Approve、Parameter/Factor/OP/SamplePlanChange/Memo 子資源及 Simulation。
2. 實作 `POST /api/qc/variable-qc-items/copy-preview`，回傳 QCItem Parameter 與目標 Sampling Plan Parameter；若兩者不同，前端以 `parameterSource` 明確選擇，取消不建立資料。Copy 主表與已勾選子資料須同一 transaction；未複製 Parameter 時以新 Plan 初始化參數。
3. 狀態更新須用資料庫真實狀態及 DataStamp/CAS。QCItemNo 跨計數/計量/群組衝突檢查；Validate min/max 同時限制上下界。Approve 先檢查 Factor，IssueNo=`QCITEM_V` 接共用 Issue/Job 流程，依有無 Job/Detail 決定 1 或 2；不得直接把所有核准狀態寫成 2。
4. DELETE：IssueState=2 改成 -1；0/-1 僅在無量測、WIP、材料/OP/產品/群組使用時交易式刪除主檔及相關子資料，另清理 Memo mapping。先追查既有 YMES 正規關聯，不可照搬四張 MESTEST 缺表名稱。
5. Memo GET 需回傳已核准 ParameterClass=1 清單加目前勾選；PUT 接 `{qcItemType:0,parameterNos}`，驗證 Parameter 清單並交易式全量替換 mapping。ParameterNo 僅為資料值，不得拼接成 SQL 欄名或執行動態 ALTER。
6. Simulation 只回預覽結果、不寫量測日誌；所有 SQL parameterized，audit 使用登入者；端點遵循專案目前登入/權限約定。先在 API 文件列出 YMES 與舊 WinForms 行為差異，再實作。
7. 更新 API/schema/migration 文件與本 TODO；commit/push 後通知公司端 pull，以 QC205 Production API 驗收。公司端此頁尚未執行 build、測試或瀏覽器驗收。
## QC209 計量品管項目管制法則對應 API/schema handoff（公司端 2026-11-17）

公司端已完成 Vue DEV 頁與 WinForms 規格。請 pull 最新 main，先讀 `docs/modules/QC/計量品管項目管制法則對應.md`、`docs/DATABASE_CONNECTION.md` 與本段。公司端沒有連 YMES PostgreSQL；本段提供 Create 參考，不表示 live schema 已比對或 migration 已套用。

### Schema / Create SQL

- Oracle `TBLQCITEMRULE` source DDL 已在 `database/reference/mestest_qc205_variable_dependencies_source_ddl.sql`，包含主鍵 `(QCITEMNO,SPCID,RULENO)` 及 `EQUIPMENTSTATE`、`ERFSTATE`、`EMAILSTATE` 三個 nullable 欄位；由公司端前次 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得。Oracle 無 FK。
- `TBLQCRULEBASIS` Oracle DDL 與 YMES Create 參考已在 QC108 files：`mestest_qc_variable_rule_basis_source_ddl.sql`、`mestest_qc_variable_rule_basis_create.sql`。QC Item variables DDL/CREATE 沿用 QC205。
- `database/reference/mestest_qc209_variable_item_rule_map_create.sql` 是本次新增 PostgreSQL CREATE 參考。先查 live schema/migrations：若 `tblqcitemrule` 已存在，勿重建或覆寫資料；只有確認缺表才設計 forward migration/rollback。保留三個 nullable source 欄位及複合鍵，不新增 Oracle 沒有的 FK。
- 舊 Core `UpdQCItemRule` 以 QCItemNo+SPCID 刪除整組後重插四欄；未寫的 EquipmentState/ERFState/EmailState 會變 NULL。記錄這個 legacy 副作用；migration 不得漏掉來源欄位。

### API 工作

1. `GET /api/qc/variable-qc-items?issueState=2`：沿用 QC205 API 為 lookup 提供 QCItemNo/Name/ChartType/IssueState。
2. `GET /api/qc/rule-bases`：沿用 QC108 法則 API；候選加入清單只允許 IssueState=2。GET mapping 仍須回傳已指派但目前非核准/已刪除的規則，避免使用者儲存其他規則時誤清除既有對應。
3. `GET /api/qc/variable-item-rules/options` 回傳 `{waitEnabled}`；伺服器依企業設定，SPK=false。
4. `GET /api/qc/variable-item-rules/{qcItemNo}` 回傳所有 SPCID 對應及 RuleName。SPCID 顯示 0 Trend、1 Xbar、2 R、3 S、4 Rm；圖表可選組合依 QC Item ChartType，詳功能規格。
5. `PUT /api/qc/variable-item-rules/{qcItemNo}/{spcId}` body `{rules:[{ruleNo,action}]}` 完整替換指定 QCItemNo+SPCID。transaction 內先查 QC Item 為已核准、SPCID 符合 ChartType，再新增所選規則；action 僅 0 Warning、1 Wait、2 Hold、3 Alert，RuleNo 在同組唯一；RULENO 最長 20 字元。SPK 禁止新增/移動 Wait，但保存其他動作時保留既有 Wait 規則，除非明確移出。
6. API 必須以單一 DB transaction 做 set replacement，參數化 SQL；遵循專案現有登入/錯誤格式。部署後更新 API/schema 文件與本 TODO，commit/push 後通知公司端 pull 驗收。

## QC210 刪除計量型品管項目量測記錄 API/schema handoff（公司端 2026-11-17）

公司端已完成 QC210 WinForms 程式核對、Vue DEV 頁、功能規格與 PostgreSQL CREATE 參考。請 pull 最新 main，先讀 docs/modules/QC/刪除計量型品管項目量測記錄.md、docs/modules/QC/計量品管項目.md、docs/DATABASE_CONNECTION.md，再核對本段。公司端未連 YMES PostgreSQL，以下 Create 檔不表示已比對 live schema 或套用 migration。

### Schema / Create SQL

- 本功能查找表沿用 QC205 的 TBLQCITEMFORVARIABLES，沒有新增主檔表。
- QC210 執行會使用 TBLSPCVARSTATISTICLOG、TBLSPCVARLIMITCONTENT、TBLSPCVARVIOLATIONCONTENT、TBLSPCVARDETAILLOG、TBLSPCVARDETAILONLINE、TBLSPCVARFACTORLOG、TBLSPCVARFACTORONLINE、TBLSPCVAREDITLOG、TBLQCSAMPLEPLANCHANGE、TBLSPCLOGFORSPLANCHANGE。
- Oracle DDL 已保存在 database/reference/mestest_qc205_variable_dependencies_source_ddl.sql、mestest_qc205_theil_custom_source_ddl.sql、mestest_qc_item_attributes_source_ddl.sql。PostgreSQL CREATE 參考 database/reference/mestest_qc210_variable_measurement_log_delete_create.sql 覆蓋 9 張變數量測日誌表；TBLQCSAMPLEPLANCHANGE 已在 mestest_qc_item_attributes_create.sql。
- 請先查 YMES live schema、部署 migration 及 QC205 對應資料表。若表已存在，不要重建或覆寫資料；若只有部分欄位/索引缺漏，再做 forward migration。確認缺表才使用 CREATE 參考設計 migration。CREATE IF NOT EXISTS 不會補齊既有表的欄位。
- 不加 Oracle 沒有的 FK。不要在 MES Oracle 執行這些 PostgreSQL CREATE SQL。只查結構，不讀或匯入 MES 量測業務資料。

### API 工作

1. 沿用 QC205 GET /api/qc/variable-qc-items?issueState=-1 與 ?issueState=0。清單 DTO 要包含 QC210 顯示欄位：QCItemNo、QCItemName、ChartType、SpecLineType、ComponentCollect、LotIsValid、PlanNo、ChartControl、CollectType、LimitLotNum、SampleSize、Unit、QCDataType、Creator、CreateDate、Reviser、ReviseDate，以及隱藏使用的 IssueState。
2. 新增登入保護的 POST /api/qc/variable-qc-items/{qcItemNo}/measurement-log-deletions，body 僅接受 { issueState }；FunctionKey 為 QC210。API 從資料庫讀真實 QC Item 和 IssueState，只允許 0、-1。Log 模式由伺服器固定，不接受用戶端傳 table name、SQL 或任意條件。
3. 每個 QCItemNo 在單一 DB transaction 中先確認 TBLSPCVARFACTORLOG 有資料；沒有資料回傳可辨識的 404/409，不刪任何表。有資料時依規格刪統計、管制界限、違規內容、量測明細/即時明細、量測主檔/即時主檔、修改紀錄、SamplePlanChange 與 SPCLogForSPlanChange。
4. 管制界限及違規內容的 SPCSerial 需從尚未刪除的 TBLSPCVARDETAILLOG 取出。先刪兩類 content，再刪 detail log。整個單一項目刪除應可 rollback；勿照抄 Core 在刪除成功後拋出成功例外，應回傳正常成功 JSON，含 QCItemNo 與各表刪除筆數。
5. Vue 多選時逐筆呼叫上述 API；一項失敗不阻止其他項目，結果需回報成功/失敗 QCItemNo，並寫入 YMES 操作稽核。遵循專案既有登入、錯誤、權限與參數化 SQL 規約。
6. 加上缺少的 forward migration 與 rollback、更新 schema/API 規格與 TODO；commit/push 後通知公司端 pull，等待 QC210 Production API 驗收。公司端尚未執行 build、測試或瀏覽器驗收。

## OP101 作業站法則設定（公司端 UI / API handoff，2026-11-18）

請 pull 最新 `main` 後先閱讀 `docs/modules/OP/作業站法則設定.md`、本段及 `docs/DATABASE_CONNECTION.md`。公司端完成 Vue DEV 頁 `/YMES/OP/ControlRules`；Production API、YMES live schema 比對與正式部署尚未完成。資料結構參考：`database/reference/mestest_op101_control_rule_source_ddl.sql`（Oracle 原始 DDL）及 `database/reference/mestest_op101_control_rule_create.sql`（PostgreSQL CREATE-only，禁止未比對就直接當 migration 套用）。

- [ ] 先比對 YMES live schema/既有 migration 的 `tblopcontrolrule`；若已存在就沿用，若缺表才新增 migration，欄位必須遵循 CREATE 參考中的 NULL/default/主鍵。不要在 MES Oracle 執行 CREATE。
- [ ] 實作登入保護、檢查 OP101 FunctionKey/權限的 API：`GET /api/operation-control-rules?ruleClass=0&ruleNo=`、`POST /api/operation-control-rules`、`PUT /api/operation-control-rules/{ruleNo}`、`DELETE /api/operation-control-rules/{ruleNo}`。
- [ ] GET 僅回標準版 RuleClass=0；包含 UI Grid 欄及 DataStamp。保持 `RuleMode`、`RuleModeQty` 和未暴露欄位；RuleClass 1/2 不要出現在 OP101 清單。
- [ ] POST 大小寫正規化 RuleNo、驗證 RuleNo/RuleName/RuleScript 必填與長度（20/50/3000）、RuleType 0 Lot/1 Component；server 設 Creator/CreateDate、RuleClass=0、DataStamp 初值，處理 RuleNo PK 重複。
- [ ] PUT 僅修改允許的定義欄位，使用舊 DataStamp CAS，成功遞增 DataStamp 並由 server 設 Reviser/ReviseDate；不可重設 RuleMode/RuleModeQty。DataStamp 可為 NULL，實作 null-safe version check。
- [ ] 相容 WinForms 儲存 payload：Apply 不帶 SendMail/HoldDispGroup，缺少欄位代表「保持現值」（新增採 DB/legacy 預設）；Confirm 才帶兩欄。不得把省略欄位轉成 null 清空。Legacy 另有新增 Apply 後仍留在新增模式的行為，詳規格。
- [ ] DELETE 沿用 RuleNo 實體刪除；前端多選逐筆 DELETE，部分成功時回報成功/失敗項目。不連帶刪除 `TBLOPRULE`、`TBL_CPOPRULE` 等其他流程法則。
- [ ] Hold Disp Group 選擇器重用 QC405 handoff 的 `GET /api/qc/hold-disposition-groups?searchFieldName=GroupNo&searchFieldValue=`，資料來源 `TBLQCHOLDDISPGROUPBASIS`，相容 WinForms 未傳 IssueState、載入全狀態的行為；不要另建 lookup table/API。SendMail=Yes 必須有群組。
- [ ] 核對 `RuleScript` 僅當字串儲存，不在 API 執行腳本。原生 `OP_Script` 動態編輯器尚未移植，先用公司端純文字編輯器。
- [ ] 先完成 YMES schema/API 實作，補 API 層驗證及 concurrency 驗收後再提交並 push；公司端 DEV localStorage 假資料不能作為正式資料寫入。

## OP102 企業邏輯設定（公司端 Vue / API handoff，2026-11-18）

請 pull 最新 main，先讀 docs/modules/OP/企業邏輯設定.md、docs/DATABASE_CONNECTION.md 與本段。公司端已完成 /YMES/OP/BusinessRules Vue DEV 頁；Production API/schema 尚未實作。Oracle source DDL 與 PostgreSQL CREATE-only 參考：
- database/reference/mestest_op102_business_rule_basis_source_ddl.sql
- database/reference/mestest_op102_business_rule_basis_create.sql
- database/reference/mestest_op102_scenario_basis_lookup_source_ddl.sql
- database/reference/mestest_op102_scenario_basis_lookup_create.sql

資料庫工作：
- [ ] 先查 YMES live schema/migrations 的 tblopbusinessrulebasis 和 tblopscenariobasis。已存在的表不得重建；只在確認缺表後新增 forward migration，對照 Oracle 欄位 nullability/default/PK。CREATE-only reference 不是 migration，也不要在 MES Oracle 執行 PostgreSQL SQL。
- [ ] 情境表是 OP102 DT 下拉的共享 lookup。若 YMES 已有則重用；若沒有，評估新增其 schema dependency；OP102 handoff 不要求另外建立情境 CRUD。
- [ ] 企業邏輯主表 Oracle 只有 BRNO PK unique index，無 FK/額外 index；情境表只有 SCENARIONO PK unique index，無 FK/額外 index。

API 工作：
- [ ] 實作登入保護及 OP102 FunctionKey/權限驗證。路由需求：GET /api/operation-business-rules?issueState=all&brNo=、GET /api/operation-business-rules/options、POST /api/operation-business-rules、PUT /api/operation-business-rules/{brNo}、DELETE /api/operation-business-rules/{brNo}、POST /api/operation-business-rules/{brNo}/approve。
- [ ] List 回傳 UI 14 個 Grid 欄位及隱藏 IssueState/DataStamp。Options 回傳已允許的 plugin function/ExecutionFile、FRMBR interface/ExecutionFile、核准 ScenarioNo/Name、BRProdType 與 AllowTimeLimitControl ValueList。前端傳入的 DLL/function 名稱不得被服務端任意載入或執行。
- [ ] POST/PUT 只接受 OP102 畫面欄位，Creator/CreateDate/Reviser/ReviseDate/IssueState/DataStamp 與 Issue Job 權限由 server 控制。修改時保留 KernelModule、KernelFunctionName、ActiveModule 與所有產品線旗標欄位，不得以空 DTO 清空。
- [ ] PUT、已簽核停用、Approve 使用 BRNO + 舊 DataStamp compare-and-swap，成功才遞增 DataStamp；Oracle DataStamp nullable/default 0，實作需有 null-safe CAS 並檢查受影響列數，零列回傳 concurrency conflict。
- [ ] 刪除規則：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 改成 -1 並寫 audit；1 拒絕。多筆刪除是逐筆操作，需回報成功/失敗 BRNo。
- [ ] 核准只接受 IssueState=0，沿用 IssueNo BR BASIS 與共用 Issue Job/Issue Control：沒有簽核設定時直接到 2，有設定則先到 1，callback 核准到 2、退回到 0。多筆需回報逐筆結果與 server EventLog。
- [ ] BRProdType ValueList 來源為 TBLOPBUSINESSRULEBASIS/BRPRODTYPE；AllowTimeLimitControl 清單來源鍵是 TBLOPBUSINESSRULEBASIS/TIMELIMITCONTROL。定義窗會停用兩欄；更新時保留已載入值，沒有提交 null。
- [ ] Manual RuleMethod 可用 FunctionName，或 InterfaceName/InterfaceURL；FunctionName 不可與 Interface/URL 同填。DT 的 ScenarioNo 持久化於 InterfaceName；API options 僅回核准情境。
- [ ] Add/Edit 的 Apply 與 Confirm 使用相同業務欄位；Apply 保持視窗開啟，Confirm 關閉。服務端 audit 一律由登入身分建立。
- [ ] 完成 API、schema、權限與簽核驗收後 commit/push，通知公司端 pull 並接正式 API。公司端本段尚未 build/test/browser 驗收。

# 2026-11-18 公司電腦交接：PRD PD102 產品主檔

先 git pull。詳細來源與欄位規則：docs/modules/PRD/產品主檔.md。Oracle 原始 DDL/字典證據：database/reference/mestest_prd_product_basis_source_ddl.sql；PostgreSQL CREATE-only 結構參照：database/reference/mestest_prd_product_basis_create.sql。ProductProperty DDL 因 DBMS_METADATA 連線 ORA-03135，依 Oracle 字典重建並已標示。Create 檔不是 migration。

本 repo 已有 ProductDto、ProductWriteRequest、MasterDataController、PostgresMasterDataRepository.Products.cs 與 migration 0010 的 ym_product_process/ym_product_bom。這些是 YMES 自有簡化關聯；Oracle DDL 顯示 MES 原生 TBLPRDPRODUCTPROCESS、TBLPRDBOM、TBLPRDBOMMATERIALGROUP、TBLPRDEQPCONSTRAINT、TBLPRDPRODUCTPROPERTY、TBLPRDPRODUCT_PICTURE 也存在。先比對 live PostgreSQL schema/migrations，決定映射或擴充；不要再次建立已存在表，也不可照 CREATE-only 檔直接部署。

待辦：
1. 比對 Product DTO/Request 與 Oracle TBLPRDPRODUCTBASIS 欄位長度、SAP/SOP/ERP、序號、圖片欄位；維持 DataStamp CAS。
2. 保留現有產品列表、CRUD、流程及 BOM API；列表 query keyword 與 ProductDto currentVersion/createdAt 命名需和 Vue 相容。
3. 調整原生產品流程 mapping，核實 MOTypeNo、PSNo/PSOrder、ProcessVersion selector、DefaultProcess、HaveComponent/HaveLevel 與 * 目前版本語意。
4. 實作 Oracle BOM 節點樹、材料群組、指定料、損耗率、遞迴最多五層、投入站及產品/材料節點；比對現有扁平 ym_product_bom，不可直接宣稱等價。
5. 為 TBLPRDPRODUCTPROPERTY、TBLPRDEQPCONSTRAINT、TBLPRDPRODUCT_PICTURE 與圖片 BLOB 補必要 schema/API；先查是否已有 YMES 對應。
6. 依 WinForms 實作 Controlled=1/2 屬性、RulePhase/AttribPhase 的 CPASS 檢查、共用 Issue/Job callback、IssueState/DataStamp；目前 API 的預設流程檢查不等價。
7. 對照 Add/Edit/Copy/Version/Apply；WinForms 讓使用者輸入 ProductVersion，YMES 目前自動加一版，需協調 API 契約與子資料複製。補 ProductType/Unit/serial/SAP_PLNAL lookup 與 ProductMaterialcoexist 檢查。
8. 比對後才新增 migration；執行 migration 前確認 home DB schema/data。正式 API 檢查 PRD BASIS/PD102 權限。
9. API/schema 完成後回報辦公室端，串 integration profile；localStorage 假資料僅供 DEV preview。

產品專用 Vue 頁已從 App.vue 的 product-master 分支載入。DEV 模式有四狀態假資料和主要表單。流程/BOM、Property、Parameter、Business Rule、OPDescription、RecipeGroup 與 Accessory DEV 子視窗已完成；OPDescription/RecipeGroup/Accessory 的 Production API/schema 依本檔 handoff 由家用端處理。Resource、SubOP、Program、Default、RunTime、QC、Packing、eSOP、APS、Bin、GBOM、Import 仍待公司端逐項遷移。

## PRD PD102 產品流程／BOM 子表單 API handoff（2026-11-18 公司端更新）

公司端已依 WinForms 原始碼補上 `ProductProcessEditor.vue` 與 `ProductBomEditor.vue`，並接入產品專用頁。DEV 預覽支援產品流程 ADD/DEL/Apply/Confirm、重複與 Default Process 檢查、Net Flow 檢查；BOM 支援右鍵樹選單、已核准產品查找、假材料查找、節點 Add/Edit/View/Del、Specified、投入點、用量、損耗率、LOTCREATE 限制、替代料群組與五層遞迴樹。所有 DEV 寫入只更新產品專用 localStorage。正式 API 模式兩個子視窗唯讀。

詳細 WinForms 欄位、事件、規則與來源路徑：`docs/modules/PRD/產品主檔子表單.md`。表結構參照仍以 `database/reference/mestest_prd_product_basis_source_ddl.sql` 為準；先核對 YMES migrations/live schema，不可重複建表或直接執行 CREATE-only 參照 SQL。

### 家用端接手

- [ ] 對照 Oracle `TBLPRDPRODUCTPROCESS`，補產品流程查詢/批次儲存契約：MOTypeNo、PSNo、PSOrder、ProcessNo、ProcessVersion (`*` current)、DefaultProcess、HaveComponent、HaveLevel；服務端驗證複合鍵、每一 MOType/PS 唯一 Default 與 Net Flow 單一區段。
- [ ] 對照 `TBLPRDBOM` 及 `TBLPRDBOMMATERIALGROUP`，提供產品/材料/投入 OP lookup、節點新增修改刪除、指定料、投料點 2/3/4、數量、損耗率、材料群組/替代料與最多五層遞迴查詢。
- [ ] 服務端重驗已核准節點、LOTCREATE、企業 TS 批號規則及 BOM 複合鍵；不能只依賴 Vue 欄位檢查。
- [ ] 比較現有 `ym_product_process`/`ym_product_bom` 與 Oracle 原生語意後才決定 mapping / migration；測試完成後回報公司端接正式 API。

公司端此段尚未執行 build、test 或 browser 驗收；DEV 元件採靜態檢閱。Business Rule DEV 已完成，Production API/XML serializer 見本檔後續 handoff；OPDescription DEV 已接入但仍待靜態複核，API/schema 見本檔 OPDescription handoff。RecipeGroup/Accessory/Resource/SubOP/Program/Default/RunTime/QC/Packing/eSOP/APS/Bin/GBOM/Import 仍須依 WinForms 原始碼逐項遷移。Parameter handoff 見本檔前段。

## PRD PD102 產品屬性子表單 API/schema handoff（2026-11-18）

公司端已依 `frmProductProperty.vb`、`frmProductPropertyDef.vb`、`wsPRD`、`clsProduct` 完成 `ProductPropertyMaster.vue` DEV 假資料視窗；正式 API 模式唯讀。詳細欄位、狀態轉移、來源路徑與 API 規格：`docs/modules/PRD/產品主檔產品屬性.md`。既有 Oracle 字典證據在 `database/reference/mestest_prd_product_basis_source_ddl.sql` 的 `TBLPRDPRODUCTPROPERTY`；此表已存在，ProductProperty DDL 為 Oracle 字典重建，勿重建同一表或把 CREATE-only 參照 SQL 當 migration。

### 家用端待辦

- [ ] git pull 後先比對 YMES live PostgreSQL schema/migrations，確認產品屬性表是否已存在及其欄位/PK，避免重複建表。尚缺欄位才新增 migration；Create SQL 僅供 review。
- [ ] 提供 `GET/POST/PUT/DELETE /api/products/{productNo}/versions/{version}/properties`、`POST /api/products/{productNo}/versions/{version}/properties/{propertyNo}/approve`；List 回傳 PK、DefaultValue、PropertySequence、Description、IssueState、DataStamp、Creator/CreateDate/Reviser/ReviseDate。
- [ ] Add/Edit 只接收 ProductNo、Version、PropertyNo、DefaultValue、PropertySequence、Description。Creator/日期/IssueState/DataStamp 由 server 控制；Edit 僅狀態 0，PropertyNo 不可改；Update 用複合鍵+舊 DataStamp CAS。Sequence 限 0..99。
- [ ] SYS Property Basis options 對應 `LoadProductPropertyDefine(PropertyType=5, IssueState=2)`；欄位至少 ElementNo、名稱、DefaultValue、ElementSequence、Controlled。PropertyValue options 對應 `wsSYS.LoadElementValue(ElementNo)`。API 端重用/建立適當 SYS lookup API，不能讓前端提供 Controlled 或有效值清單作為信任資料。
- [ ] Add 與 DefaultValue 更新需服務端驗證 PropertyNo 已核准且值符合 SYS Element/MES Parameter (`ChkElementValueValid`)。
- [ ] Delete：IssueState 0/-1 實體刪除；2 以 DataStamp CAS 設 -1；1 拒絕。多筆逐筆處理，回傳各筆結果，碰到第一筆錯誤即停止後續。
- [ ] Approve 僅接受狀態 0。IssueNo=`PRD PROPERTY` 沒有 Job/Detail 時直接設 2；有簽核 Job 時建 Issue Master、設 1、送第一關通知，callback 再核准/退回。保留 audit/EventLog 並使用 DataStamp。
- [ ] 產品主檔核准由 server 重查 SYS Controlled=1 必填、Controlled=2 空值確認及流程 RulePhase/AttribPhase/CPASS 規則，不接受 Vue 送入的 Controlled 作安全依據。
- [ ] 明確對應 ProductType/無版次屬性的 PRODUCTVERSION sentinel；Oracle 空字串與空白語意需以現有資料/應用層規則定義，避免將 `-1`、`''`、`' '` 混為一談。

公司端未執行 build/test/browser 驗收；Property 的選項只有 DEV 假資料，不連 Oracle/YMES API。Parameter API/schema handoff 見本檔新段落。

## PRD PD102 產品 Parameter Setup API/schema handoff（公司端 2026-11-18）

公司端已依 WinForms `PD_ParameterSetup` Form/Designer、`wsPRD`、`clsProduct` 完成 DEV 樹狀子視窗。請先 pull 最新 `main`。詳細畫面、ContextMenu 權限、欄位、驗證、來源服務與資料表見 `docs/modules/PRD/產品參數設定.md`。Oracle source 結構與 PostgreSQL CREATE-only 參考：

- `database/reference/mestest_prd_parameter_setup_source_ddl.sql`
- `database/reference/mestest_prd_parameter_setup_create.sql`

### 家用端待辦

- [ ] 先核對 YMES PostgreSQL live schema、migration、repository/entity 是否已包含產品 OP/Attrib/Rule/Equipment Recipe；上述 CREATE-only 是結構參照，不是部署 migration。只對確實缺少的資料新增獨立 migration，勿照檔案一口氣建立 8 張 Oracle 原名表。
- [ ] 主要 MES 結構為 `TBLPRDOP` (PK SerialNo)、`TBLPRDOPATTRIB` (PK SerialNo/AttribNo/ECNNo/EquipmentType)、`TBLPRDOPRULE` (PK SerialNo/RuleNo)、`TBLPRDOPEQPRECIPE` (無 PK/unique，僅 SerialNo/EquipmentNo/RecipeGroup 非唯一 index)。四表均無 DataStamp、IssueState、FK、Trigger。Oracle 設備關聯表允許資料重複，YMES API 應明確做 duplicate check/交易控制。
- [ ] `TBLPRDOPRULEXML_CLOB`、`TBLPRDOPBUSINESSRULE`、`TBLPRDOPBUSINESSRULERELATION` 已由公司端依實際 service 查明分別供 Rule XML fallback 與 PD102 Business Rule 使用；請沿用該功能 handoff 並先比對 YMES schema。`TBLPRDOPAREA` 是相鄰功能，需追實際 service 後再決定是否納入；不可因參照 SQL 存在就一併建表。
- [ ] 建議讀取產品版本的 Parameter tree；至少回傳 PS/OP、OP SerialNo/NodeID/OPScript、Attrib、Rule、EquipmentNo/RecipeGroup、Recipe Detail。實作 Add OP、Add Condition OP、Attrib/Rule/Equipment 關聯 CRUD；SerialNo 由 server 產生。產品 IssueState!=0 全部唯讀；Server 必須重查產品、PS/OP、權限及 Element/Phase/Rule/Equipment lookup。
- [ ] Attrib API 必須保留 `AttribType` 0 Lot / 1 Component、`AttribSource` 0 KeyIn / 1 Calculate / 2 System / 3 Setup、AttribPhase/Sequence/Value/Script/SaveToLotPropertyNo/QCItemNo 等語意；重做 Element Value、Phase 99、Calculate Script、Setup Default、SaveToProperty/QC Item 驗證。EquipmentType/ECNNo 也是 Oracle Attrib NOT NULL 複合 PK 欄位。
- [ ] **先釐清 Oracle/WinForms 欄位差異再實作 Attrib insert：** MESTEST 字典顯示 TBLPRDOPATTRIB.ECNNO、EQUIPMENTTYPE NOT NULL 且無 Trigger；可讀到的 `wsPRD.AddPRDOPAttrib`/`clsProduct.AddPRDOPAttrib` XML/SQL 路徑未明確帶這兩欄。檢查本機客製 wsPRD、執行時 AdditionalXml、資料庫是否有版本差異；不要塞空字串或假值。
- [ ] 一般 Rule 的 RuleNo/Phase/Script/Type required；CP/FT 專用 Rule 使用不同 service/資料結構，須另核對 RuleClass/CP/FT package API 後再承諾支援。設備候選由產品/OP 可用設備 lookup，Recipe Detail 僅由 RecipeGroup 主檔讀取，產品 OP 只存 RecipeGroup。
- [ ] `DBMS_METADATA.GET_DDL` 本次從 Oracle 連線回 SocketException，但 Windows PowerShell 5.1/ODP.NET 對 ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_CONS_COLUMNS、ALL_INDEXES、ALL_IND_COLUMNS 的唯讀查詢成功；source DDL 因此是 Oracle 字典重建邏輯 CREATE，不含 tablespace/storage。未讀業務資料、未在 Oracle 執行 DDL/DML。連線方式/秘密處理見 `docs/DATABASE_CONNECTION.md`，密碼只留本機受保護設定，不寫入 Git。
- [ ] 實作 API/schema 並 push 後回報公司端；公司端接 API 時保留 `ProductParameterSetup.vue` 的 DEV mode 與 API read-only 行為界線。

公司端未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，也未部署上述 SQL。

## PRD PD102 Business Rule API/schema handoff（2026-11-18）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品商業規則.md`、`docs/modules/PRD/產品參數設定.md` 及 `docs/DATABASE_CONNECTION.md` 的 PD102 metadata 紀錄。
- [ ] 先比對 YMES live schema/migrations。重用 `database/reference/mestest_prd_parameter_setup_source_ddl.sql`、`mestest_prd_parameter_setup_create.sql` 中的 `tblprdopbusinessrule`、`tblprdopbusinessrulerelation`、`tblprdoprulexml_clob`；三張 Oracle source table 已由公司端直接查 MESTEST 字典，不能因 CREATE-only 檔再建第二份表或原樣執行 SQL。
- [ ] 實作產品/版次/OP 範圍的 Business Rule 查詢與 ADD/EDIT/DEL/Default API。主表 RuleXmlString 是 Oracle LONG；relation 以 `(ProductNo,ProductVersion,OPNo,FromNode,ToNode,LinkName)` 為 PK；LONG provider fallback 使用既有 CLOB 表。XML 與 relations 同一交易更新/刪除。
- [ ] 保留 `iMESXMLFlow.Serial.FlowToXML/XMLToFlow` 的舊版 XML round-trip，並把 Vue graph DTO 轉譯至 RuleXmlString；不能把 DEV localStorage JSON 直接當正式資料。實作 BR/Phase/Template/Process/OP lookup，NetFlow lookup 需帶 `IsNetFlow`。
- [ ] Server 依登入者的 Function/PS 權限與 DB 當前產品 IssueState 驗證；不可接受 client 傳 `PSPrivilege`。依規格實作 Start/End、PASS、輸入/輸出連線與 Phase 驗證，Default 複製 `TBLOPBASIS` XML 及 `TBLOPBUSINESSRULERELATION`，回報錯誤不可吞例外。
- [ ] 此功能不需要建立新的 Oracle 表；migration 僅在 YMES live schema 比對後確有缺漏才新增。完成 API/schema 後通知公司端 pull，接正式 API 與 serializer。

## PRD PD102 Business Rule DEV UI（2026-11-18）

- [x] 核對真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、WinForms Designer、`wsPRD`、`clsProduct`、Service graph validation；排除名稱相近但不同的 `PD_PRDOPRule_THEIL_BU1`。
- [x] 完成產品 OP 清單/圖形預覽及 Default/Close 版面、Add/Edit/多選 Del、圖形節點/連線/Phase 編輯、NetFlow readonly 的 DEV UI，接至 ProductBasisMaster 並只寫專用 localStorage。
- [x] 新增 `docs/modules/PRD/產品商業規則.md`；重用 Parameter 階段已查到的三張 Oracle 表與既有 DDL/PG CREATE 參照，沒有重複 Oracle query、沒有新增表或 CREATE SQL。
- [ ] Production API、OP/Phase/Business Rule/Template lookup、Lassalle XML serializer 尚待家用端完成；Default、樣板及 catalog 現在仍為 DEV 假資料。
- [ ] 公司端未執行 build/test/browser。OPDescription、RecipeGroup、Accessory DEV/DDL handoff 已分別記於本檔後續段落；辦公室端接續 Resource。


## PRD PD102 OPDescription API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品作業站說明.md`、`docs/modules/PRD/產品主檔.md` 及 `docs/DATABASE_CONNECTION.md` 的 OPDescription 查詢紀錄。
- [ ] 比對 YMES PostgreSQL live schema、migration、entity/repository；公司端搜尋目前未找到 OPDescription 專用 API/schema。Oracle source DDL 與 PG CREATE-only 參照為 `database/reference/mestest_prd_op_description_source_ddl.sql`、`mestest_prd_op_description_create.sql`。只在 live schema 確認缺少時建立 forward migration，不照 CREATE-only 直接部署。
- [ ] 表欄位：ProductNo varchar(50)、ProductVersion varchar(5)、OPNo varchar(20) 必填複合 PK；Description varchar(255) nullable；DataStamp numeric(11,0) nullable default 0；Creator/Reviser varchar(10)、CreateDate/ReviseDate nullable。無 FK、trigger、其他 index。
- [ ] 實作產品/版本範圍 OPDescription GET/POST/PUT/DELETE。GET 依登入者 PS 權限過濾並 join OP lookup；POST 驗證產品 IssueState=0、OP 在產品已設定且有權限的流程範圍、同產品版本 OPNo 唯一；PUT 以複合鍵 + DataStamp CAS 原子更新 Description、Reviser/ReviseDate，衝突回 409；DELETE 要求明確 OPNo 並實體刪除。
- [ ] 提供同等 WinForms 查找：產品流程 lookup、指定流程的 Expand Process OP lookup；server 依 session 決定 PS 權限，排除已存在於目前產品/版本的 OPNo，OP 說明初值取 `TBLOPBASIS.Description`。不可信任 client 傳入 PSPrivilege、IssueState 或 audit 欄位。
- [ ] UI 多選 DEL 是逐筆呼叫、遇錯停止且保留前面成功刪除；API 若提供批次 endpoint，需定義並回傳部分成功結果。產品 Copy/Version 需保留 Core `CopyPRDOPDescription` 行為；Product Load Default 的 Description 是另一個功能，不要塞進此子視窗 CRUD。
- [ ] 完成後同步 API contract/schema migration 與文件、push main，通知公司端接正式 API。DEV localStorage 不能寫入正式 DB。

## PRD PD102 RecipeGroup API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 `docs/modules/PRD/產品RecipeGroup.md`、`docs/modules/PRD/產品主檔.md` 與 `docs/DATABASE_CONNECTION.md` metadata provenance。
- [ ] Oracle source DDL 與 PostgreSQL CREATE-only 參照為 `database/reference/mestest_prd_recipe_group_source_ddl.sql`、`mestest_prd_recipe_group_create.sql`。先查 YMES live schema、migration、entity/repository；僅當 assignment relation 或必要 lookup 確實不存在時新增 forward migration。`TBLPRDRECIPEBASIS` 是共用 Recipe 主檔，不能重複建表。
- [ ] Oracle `TBLPRDRECIPE` 為 `(ProductNo varchar(50), ProductVersion varchar(5), RecipeGroup varchar(50))` 必填複合 PK，無 audit 欄、無 FK/index/trigger（PK unique index 除外）。`TBLPRDRECIPEBASIS` 為 `(RecipeGroup varchar(50), RecipeVersion numeric(2,0))` 複合 PK；RecipeVersion default 0 NOT NULL、IssueState default 0 nullable、CurVersion default 0 NOT NULL、DataStamp default 0 nullable，其餘 Description/Audit nullable；無 FK/trigger/額外 index。
- [ ] 提供產品版本 RecipeGroup GET、已核准目前版 RecipeGroup lookup、單筆 POST 指派與要求明確 RecipeGroup 的 DELETE；不提供 Edit（WinForms EDIT 隱藏，service 也沒有實際更新）。Lookup 對應 `TBLPRDRECIPEBASIS` 的 `CurVersion=1 AND IssueState=2`，新增時 server 重新檢查，不信任 client。
- [ ] 入口傳入 IssueState，但 `frmProductRecipe` 不用狀態限制 ADD/DEL；新 API 先保留這項 legacy 行為並檢查登入者對產品的權限。若要加狀態限制，需另有業務規則依據。
- [ ] WinForms 多列刪除逐筆呼叫、遇錯停止且保留先前成功結果。API 若做批次需定義 partial result；單筆 DELETE 不可省略 RecipeGroup，避免 Core 預設刪除產品版本全部關聯。
- [ ] 產品 Copy/Version 需保留 `CopyPRDRecipe`：先清除目標版本 RecipeGroup 關聯，再複製來源關聯。Product Recipe Apply/Confirm/Close 綁定原生指派語意；完成 schema/API/文件後 push main，通知公司端接正式 API。DEV localStorage 不寫正式 DB。

## PRD PD102 Accessory API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品配件設定.md`、產品主檔規格與 DB connection 的 Oracle metadata provenance。
- [ ] Oracle source DDL / PostgreSQL CREATE-only 參考：`database/reference/mestest_prd_accessory_source_ddl.sql`、`mestest_prd_accessory_create.sql`。先查 YMES `tblprdaccessory` live schema、migrations、entity/repository；僅在確認缺表後新增 forward migration。
- [ ] Source `TBLPRDACCESSORY` 欄位：ProductNo varchar(50)、AccessoryType varchar(50)、AccessoryNo varchar(50)、ProductVersion varchar(5)、OPNo varchar(20) DEFAULT `'*'`、OPName varchar(200)；五欄 `(ProductNo,ProductVersion,OPNo,AccessoryType,AccessoryNo)` 複合 PK；OPName nullable；無 FK、trigger、其他 index（PK unique index 除外）。
- [ ] 重用配件功能既有 `tbleqpaccessorytype`、`tbleqpaccessorybasis` lookup/API，不要新增同義表。提供產品/版本配件 GET、PS 權限下的產品流程 OP lookup、AccessoryType 與 AccessoryNo lookup。
- [ ] 實作以單一 ProductNo/ProductVersion/OPNo 更新設定的 API，支援指定多個 AccessoryNo 與 `'*'` 全部模式；定義空 multivalue 是否清空該類別、操作是否以 transaction 原子替換。WinForms client 只證明 Update 帶單一 OPNo/OPName 與 multivalue、DEL 逐筆帶 OPNo/OPName/AccessoryType；`wsPRD`/`clsProduct` 實作不在 client source，需查服務/Core 確認替換範圍及 Copy/Version 行為。
- [ ] Delete 必須明確限定 ProductNo、ProductVersion、OPNo、AccessoryType。UI 多選逐筆處理，遇錯停止並保留先前成功結果；批次 endpoint 若實作需回傳逐筆結果。
- [ ] Server 依登入身分驗證產品、流程/PS 權限及配件 lookup，不接受 client 自傳 PSPrivilege、OPName/audit 身分或 IssueState。完成 API/schema/文件後 push main 通知公司端；DEV localStorage 資料不可送進正式 API。

公司端此段未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Resource API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 main，閱讀 docs/modules/PRD/產品作業區域設定.md、docs/modules/PRD/產品主檔.md 和本檔 handoff。
- [ ] 不新增表或 CREATE SQL：Oracle source/PG CREATE 參照已存在。TBLPRDOPAREA：database/reference/mestest_prd_parameter_setup_source_ddl.sql / mestest_prd_parameter_setup_create.sql；TBLOPAREA：mestest_operation_and_system_source_ddl.sql / mestest_operation_and_system_create.sql；TBLSMDAREABASIS：mestest_smd_area_basis_source_ddl.sql / mestest_smd_area_basis_create.sql。仍先核對 YMES live schema/migrations，不可因參考 CREATE 檔重建有資料的表。
- [ ] 核對 TBLPRDOPAREA 五欄與 PK (ProductNo,ProductVersion,OPNo,AreaNo)：前四欄 NOT NULL，DefaultArea numeric(1,0) nullable，無 audit/DataStamp/FK/trigger/額外 index。AreaName、FactoryNo 是 SMD Area lookup 顯示欄，不可存入此表。
- [ ] 實作登入保護 API：GET 產品/版次 OP Area 清單及指定 OP 明細；PUT 以 (ProductNo,ProductVersion,OPNo) 全量取代 areas:[{areaNo,defaultArea}]；DELETE 必須帶完整產品、版次、OP 範圍並刪該 OP 全部區域。WinForms 新增/修改都呼叫同一 UpdEditPRDOPArea，來源註解要求全刪全增；家用端查 Core/Service 確認 transaction/空集合語意，更新需原子執行。
- [ ] 提供產品版次流程 lookup、指定 Process/Version 的 OP NodeType=0 lookup、OP 的 TBLOPAREA 查找、Area Basis lookup。重用 PRS、OP、SMD 的既有 API/table，不新增同義表。新增模式初始區域由 OP Area Map 載入；修改模式載入目前產品 OP Area，兩者選項範圍按 WinForms 原 service 行為查證。
- [ ] Server 從登入者權限決定 PSPrivilege；不接受 client 的 PSPrivilege、IssueState 或 audit identity。對產品當前 IssueState、OP/PS 權限、Area 存在及關聯範圍重新驗證。PUT 要拒絕重複 AreaNo，並要求恰一筆 DefaultArea=1；UI Delete 一次確認後逐 OP 呼叫，遇錯停止並保留先前已成功項目。
- [ ] 查 LoadPRDOPArea 的 DefaultArea 篩選、LoadOPResourceDefine lookup 範圍、DelPRDOPArea 精確刪除條件及 CopyPRDOPArea 產品 Copy/Version 行為。規格記錄來源 client 無法證明之處；核對後更新模組規格。
- [ ] 完成 YMES schema/API/DTO 與文件後 commit/push main，通知公司端接 Production API。公司端 Resource DEV 已完成；未執行 build/test/browser，未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 SubOP API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品子作業設定.md`、`docs/modules/PRD/產品主檔.md` 與本檔 handoff。
- [ ] Oracle source DDL：`database/reference/mestest_prd_subop_source_ddl.sql`；PostgreSQL CREATE-only 參照：`database/reference/mestest_prd_subop_create.sql`。先查 YMES live schema、migration、entity/repository；只在確認缺表或缺欄後新增 forward migration，不能直接執行參照 CREATE SQL。
- [ ] 專用關聯表 `tblprdopsubop` 欄位：`productno varchar(50)`, `productversion varchar(5)`, `opno varchar(20)`, `subopsequence numeric(4,0)`, `subopno varchar(20)`, `subopname varchar(100)` 全為 NOT NULL；`creator varchar(10)`, `createdate timestamp`, `datastamp numeric(11,0) DEFAULT 0`, `reviser varchar(10)`, `revisedate timestamp` nullable。PK `(productno,productversion,opno,subopsequence)`；Oracle 無 FK、trigger 或非 PK index。不要與共用 OP 主檔 `tblopsubopbasis` 混用，也不要重建已存在的 OP 主檔表。
- [ ] 提供產品/版次 SubOP 清單、尾端新增、指定序號插入、修改、多選/單列刪除與序號整理 API。Insert 的後續序號位移與新增需在同一 transaction；Delete 精確限定產品、版次、OP、Sequence。WinForms 多選是逐筆呼叫、遇錯停止且保留此前成功刪除；若改批次 API，要表達部分成功結果。
- [ ] OP lookup 重用產品流程 OP 查詢及 `PSPrivilege=True` 範圍；由登入者權限在 server 計算 PSPrivilege，不信任 client 傳入的權限、Creator/Reviser、IssueState、OPNo 或 DataStamp 更新值。此子表單未按產品 IssueState 限制，資料表無 IssueState/Approval 欄，維持來源行為。
- [ ] Edit 依舊以 DataStamp 做 optimistic concurrency；欄位 nullable/default 需保留，確認舊資料 DataStamp NULL 的更新語意。Sort 對各 OP 重新編成 1..N，只更新序號。
- [ ] 完成 live schema/API/DTO/migration 與文件後 push `main`，通知公司端接正式 API；公司端目前只有 DEV 假資料，未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Program API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品程式指派.md`、`docs/modules/PRD/產品主檔.md` 與本檔 handoff。
- [ ] Oracle source DDL：`database/reference/mestest_prd_program_source_ddl.sql`；PostgreSQL CREATE-only：`database/reference/mestest_prd_program_create.sql`。先檢查 YMES live schema、migration、entity/repository；`tblprdprogrambasis` 已在 first-phase bootstrap，先核對欄位並修正既有 schema mapping，不要重複建立。只在確認 `tblprdproductprograms` 不存在時新增 forward migration。
- [ ] `tblprdprogrambasis` 欄位：ProgramNo varchar(50) NOT NULL PK；ProgramType varchar(10) nullable；IssueState numeric(1,0) DEFAULT 0 NOT NULL；Creator varchar(10)、CreateDate timestamp、Description varchar(255) nullable。`tblprdproductprograms` 為 ProductNo varchar(50)、ProductVersion varchar(5)、ProgramNo varchar(50) 三欄 NOT NULL，複合 PK `(productno,productversion,programno)`；無 FK、trigger 或 PK 以外 index。
- [ ] 提供登入保護的 Program option GET 與產品版次 assignments GET。WinForms `LoadProgram()` 不傳 IssueState 條件，option lookup 不過濾 Program IssueState；UI 欄位為 ProgramNo/ProgramType/Description。
- [ ] 提供完整替換 API `PUT /api/products/{productNo}/versions/{version}/programs`；同一 transaction 內刪除該版次現有關聯並插入勾選清單。空清單代表清除全部。驗證 ProgramNo 存在、去重、登入者有產品版次設定權限。
- [ ] 只有產品 IssueState=0 時允許儲存。舊畫面在其他狀態仍允許勾選，但 Confirm disabled；API 必須 server-side 再檢查狀態與權限，不信任 client 的 IssueState/identity。
- [ ] 完成 schema/API/DTO/文件後 push main，通知公司端接正式 API。公司端目前只完成 DEV localStorage，Production API 未接線；未執行 build/test/browser、未連 YMES PostgreSQL、未部署 SQL。

## PRD PD102 Default API/schema handoff（2026-11-18 公司端更新）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/PRD/產品預設設定.md`、`docs/modules/PRD/產品主檔.md` 與本段 handoff。
- [ ] Oracle APS product source DDL：`database/reference/mestest_prd_default_aps_source_ddl.sql`；PG CREATE-only：`database/reference/mestest_prd_default_aps_create.sql`。先查 YMES live schema、migrations、first-phase bootstrap；APS bootstrap 有欄位曾標示為推測，不能直接執行 CREATE-only 或重建既有表。
- [ ] `tblprdprsopapssetup` PK `(productno,productversion,motypeno,nodeid)`；`tblprdprsopapssetupdetail` PK `(productno,productversion,motypeno,nodeid,equipmentno)`。兩張 Oracle 表都存在；欄位、nullable/default 與字典證據見功能規格及 SQL reference。流程 APS `tblprsopapsbase/detail` 已由 PS104 提供，先比對 YMES schema，不建立重複表。
- [ ] 實作登入保護的批次 Default API。請求攜帶 ProductNo、ProductVersion、DefaultOP (1=All PS OP、2=Product Process OP)、Parameter/BusinessRule/Description/OPResource/QCItem/SubOP/APS 旗標；API 端從 DB 展開 OP，重新驗證 IssueState=0、產品流程/PS 權限、來源預設已核准。禁止信任 client 的 OP 清單、IssueState 及 audit identity。
- [ ] 對照 `clsProduct.UpdPRDOP_BatchDefault`：此功能可能重建 `TBLPRDOP`、Parameter/Attrib/Rule、BusinessRule/Relation/XML CLOB、Description、OPArea、QCItem、SubOP 及 APS setup/detail。查清空來源時的清除行為、未勾項目的保留範圍、Parameter ECN 例外及實際交易邊界後，才決定 API transaction。
- [ ] 注意 WinForms APS checkbox 在 `ExecuteAPS` 未啟用時隱藏但 Designer 預設 checked；QCItem 預設由 `PRDDefault_QCItemAutoChecked` 決定。核對舊 client 送旗標行為並寫入規格，避免 UI/API 默默改變資料。
- [ ] 只有 live schema 確認缺表/缺欄才新增 migration；完成 API/DTO/錯誤處理後更新規格及 TODO，commit/push `main`。公司端 Default 目前只有 DEV localStorage；Production API 未接線，未部署 SQL。

## PRD PD102 RunTime 家用端 handoff（2026-11-18）

- [ ] 先比對 live schema、first-phase bootstrap 與 migration history。以 [RunTime 規格](docs/modules/PRD/產品標準工時.md)、[Oracle 邏輯 DDL](database/reference/mestest_prd_run_time_source_ddl.sql) 及 [PostgreSQL CREATE-only](database/reference/mestest_prd_run_time_create.sql) 核對 `tblprdruntimesetup` 與六張既有 lookup 表。來源 RunTime 表沒有 PK、unique、FK、索引或 trigger。
- [ ] 實作登入保護的產品版次 RunTime GET／PUT。GET 依產品 OP 區域、設備／群組與設備類型預設展開，再套用已存設定。PUT 拒絕空清單，核對伺服器計算的列身分、精度、權限與同版次並發，全量替換必須在同一 transaction。核對 Fix/Var 工時及 WorkPrice 欄位是否須為其他流程保留。
- [ ] 驗收無設備、設備直掛／群組重複、首次儲存、整批替換、錯誤回滾、並發、Copy／刪除、稽核及權限。公司端 RunTime 已提供 DEV localStorage 操作，正式模式待 API 接線。

## PRD PD102 QC Item 家用端 handoff（2026-11-18）

- [ ] 依 [QC Item 規格](docs/modules/PRD/產品品管項目設定.md) 先比對 live schema 和 migration history。`TBLPRDQCITEM.XX_FREQ` 為目前 MESTEST 已確認的 nullable 欄位。沿用既有 OP、QC 目錄及 BindLog，評估需要的 forward migration。
- [ ] 實作產品版次／OP 範圍 GET/PUT。按伺服器可用目錄驗證項目、PS 權限、LotIsValid、InLine 檢驗旗標和主鍵，以完整清單替換該 OP，支援清空，保留其他 OP，妥善處理 BindLog 與並發。
- [ ] QC Import、QC Chart 檔案解析及檢查／寫入仍待接續，見規格中的 WinForms 與 Service 路徑。公司端目前已完成 DEV 指派表單。

## PRD PD102 Packing Set 家用端 handoff（2026-11-18）

- [ ] 依 [Packing Set 規格](docs/modules/PRD/產品包裝設定.md) 核對 live schema、first-phase、既有 migrations 及七表的 [Oracle 邏輯 DDL](database/reference/mestest_prd_packing_set_source_ddl.sql)／[PostgreSQL CREATE-only](database/reference/mestest_prd_packing_set_create.sql)。產品包裝數量沿用產品主檔，保留序號主表索引與 PK 的差異紀錄。
- [ ] 接三階 Pack Setting、配件 CRUD／整批 Copy 及多階 Rule Map API。以伺服器權限、產品 IssueState、DataStamp、Current／Specified 規則版次和使用中批次作為寫入條件。
- [ ] 公司端已完成 Serial Rule、Label Rule／Multi View 和 DEV 欄位預覽；家用端待核對序號長度 100 與 Oracle 欄位精度、2D 分隔符、標籤引擎、檔案路徑權限與批次寫入。

## PRD PD102 EQPCapItem 家用端 handoff（2026-11-18）

- [ ] 依 [設備能力項目規格](docs/modules/PRD/產品設備能力項目.md) 核對 `tblprdopcapitem`、共用 `tblemseqpcapbasis`、`tblopbasis` 與 live schema／migration history。只補缺漏的表或欄位。
- [ ] 實作產品版次設備能力 CRUD、OP／CapItem lookup、同版次 Copy、新舊 DataStamp 衝突、稽核和多選刪除語意。伺服器驗證 IssueState、權限、主鍵及 CapValue 資料型別與長度。

## PRD PD102 eSOP 家用端 handoff（2026-11-18）

- [ ] 依 [產品電子作業指導書規格](docs/modules/PRD/產品電子作業指導書.md) 核對 `tblprdesopbasis`、first-phase 和 live schema。沿用 OP／SubOP 及參數，依需求補缺漏的 migration。
- [ ] 實作產品版次的 eSOP CRUD、Position／SubOP／文件 lookup 與授權存取。正式 Position／文件模型對照 EAI 的 `station.get`、`sop.get`。DEV 目前只用假資料或檔名，不讀檔案內容。

## PRD PD102 EquipmentSpec 家用端 handoff（2026-11-18）

- [ ] 依 [產品設備參數規格](docs/modules/PRD/產品設備參數規格.md) 核對 `tblprdequipmentspec`、`tbleqpparameterbasis`、live schema 和既有 migrations。
- [ ] 實作依流程與 PS 權限找設備、設備參數目錄與產品規格合併查詢、整批 PUT。驗證四欄順序、精度、可用參數與稽核來源，固化整批交易及並發語意。

## PRD PD102 APS Integration 家用端 handoff（2026-11-18）

- [ ] 依 [產品 APS 規格](docs/modules/PRD/產品APS整合.md)，對照 Default／PS104 的四張既有 APS 表 DDL 與 live schema，沿用共用結構。
- [ ] 實作產品版次／MOType／Node 主設定與設備明細 API。伺服器重新驗證流程、設備、群組與權限，主設定 Update／Delete 及設備明細重建保持同一交易。

## PRD PD102 Bin 家用端 handoff（2026-11-19）

- [ ] 依 [產品 Bin 規格](docs/modules/PRD/產品Bin定義.md) 及兩張表的 [CREATE-only 參考](database/reference/mestest_prd_bin_create.sql) 核對 YMES live schema，確認是否需要 migration。
- [ ] 實作依產品版本載入系統 Bin 目錄與整組定義，並提供交易式整批替換。確認 `IssueState`、空清單清除、Good／OS Bin 檢查與操作者稽核。

## PRD PD102 GBOM 家用端 handoff（2026-11-19）

- [ ] 依 [GBOM 規格](docs/modules/PRD/產品GBOM匯入匯出.md) 核對共用流程表、客製表 CREATE-only 參照與 PP02／PP05 資訊記錄來源。MESTEST 的 `XX_TBLWIPINFORECORD` 為 INVALID。
- [ ] 實作 Excel 驗證、產品流程與 BOM 的交易、檔案備份與原始檔下載，定義備份失敗後的恢復流程。

## PRD PD102 產品 Import 家用端 handoff（2026-11-19）

- [ ] 依 [產品資料匯入規格](docs/modules/PRD/產品資料匯入.md) 核對產品／類別／流程及子表的 live schema，沿用產品和 PS104 參考並檢查 `tblprdtype`。
- [ ] 實作五張工作表預檢、依流程與產品群組匯入、各組結果、取消與 SYS 範本權限。Vue DEV 目前可預覽流程並新增主檔假資料。

## PRD PD102 Expand All 家用端 handoff（2026-11-19）

- [ ] 依 [產品流程展開規格](docs/modules/PRD/產品流程展開.md)，核對共用流程與作業表；按 `IsNetFlow`、產品流程版次和工單類型實作唯讀 API。

## PRD PD101 產品類別資料家用端 handoff（2026-11-19）

- [ ] 依 [產品類別規格](docs/modules/PRD/產品類別資料.md) 核對 `tblprdtype` 和 `ProductVersion='-1'` 共用表，沿用既有 CREATE-only。
- [ ] 實作類別 API、狀態及 DataStamp、區段權限、交易式完整 Copy 和類別範圍的刪除。

## PRD PD103 製程模組資料家用端 handoff（2026-11-19）

- [ ] 依 [製程模組規格](docs/modules/PRD/製程模組資料.md) 核對 live schema、`tblprdmodulebasis` 與共用子表鍵。
- [ ] 實作模組版次、簽核與目前版切換、區段權限、完整複製與匯入，確保共用子表可區別模組及產品。

## PRD PD104 產品內容查詢家用端 handoff（2026-11-19）

- [ ] 依 [產品內容查詢規格](docs/modules/PRD/產品內容查詢.md) 對照既有 PD102 schema，實作候選查找、版次內條件交集、分頁、資料權限及欄位設定。

## PRD PD105 產品參數群組家用端 handoff（2026-11-19）

- [ ] 依 [產品參數群組規格](docs/modules/PRD/產品參數群組.md) 核對群組／明細 schema，實作版次、明細、交易式複製、簽核與目前版切換。

## PRD PD106 生產時間限制家用端 handoff（2026-11-19）

- [ ] 依 [生產時間限制規格](docs/modules/PRD/生產時間限制.md) 核對三張表、Hold 原因的版次長度差異和既有 YMES schema。
- [ ] 實作唯讀候選、複合鍵更新、核准與刪除；交易中保存歷程、限制與 Hold 原因。

## PRD PD107 標籤設定家用端 handoff（2026-11-19）

- [ ] 依 [標籤設定規格](docs/modules/PRD/標籤設定.md) 核對主檔與明細 schema，實作交易式內容更新、權限、具名欄位預覽與正式引擎接入。

## PRD PD108 產品標籤對應設定家用端 handoff（2026-11-19）

- [ ] 依 [產品標籤對應規格](docs/modules/PRD/產品標籤對應設定.md) 核對對應表與作業參數，實作對應 CRUD、候選及批次設定 PrintLabel，處理明確對應優先和逐列交易。

## PRD PD109 產品作業站預設家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站預設規格](docs/modules/PRD/產品作業站預設.md) 沿用 PD102 共用表，實作候選及依作業範圍執行的 Default／Amend、每版交易、進度與取消。

## PRD PD110 產品作業站修正家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站修正規格](docs/modules/PRD/產品作業站修正.md) 沿用 PD109 Amend API、schema 與權限，帶入 PD110 固定六項。

## PRD PD111 產品作業站批次複製家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站批次複製規格](docs/modules/PRD/產品作業站批次複製.md) 沿用共用表，實作版次／ECN 檢查、每版交易、SerialNo 關聯複製與取消。

## PRD PD112 包裝規則設定家用端 handoff（2026-11-19）

- [ ] 依 [包裝規則規格](docs/modules/PRD/包裝規則設定.md) 核對七張表、產品與規則版次鍵及現有遷移，實作主檔、子表、正式用量與標籤引擎。

## PRD PD113 產品包裝規則對應家用端 handoff（2026-11-19）

- [ ] 依 [包裝規則對應規格](docs/modules/PRD/產品包裝規則對應設定.md) 沿用 PD102 對應及 PD112 候選 API，實作 `*` 及 WIP／庫存使用中卡控。

## PRD PD114 自定義流程卡家用端 handoff（2026-11-19）

- [ ] 依 [自定義流程卡](docs/modules/PRD/自定義流程卡.md) 核對主檔與明細 CREATE-only、DataStamp 及 WIP／報表引擎依賴。

## PRD PD115 產品流程卡對應家用端 handoff（2026-11-19）

- [ ] 依 [產品流程卡對應設定](docs/modules/PRD/產品流程卡對應設定.md) 核對 CREATE-only、候選 API、DataStamp、`*` 及正式列印呼叫端。

## PRD PD116 產品品管項目規格匯入家用端 handoff（2026-11-19）

- [ ] 依 [產品品管項目規格匯入](docs/modules/PRD/產品品管項目規格匯入.md) 沿用 PD102／QC302 CREATE，實作業務預檢、整批交易、規格歷史與範本匯出。

## PRD PD117 GBOM 批次匯入家用端 handoff（2026-11-19）

- [ ] 依 [GBOM 批次匯入](docs/modules/PRD/GBOM批次匯入.md) 沿用現有產品、流程與客製表，實作逐檔預檢、每產品版次交易、原檔備份、逐檔結果與 SAP 相依。

## PRD PD202 FT 測試程式家用端 handoff（2026-11-19）

- [ ] 依 [FT 測試程式](docs/modules/PRD/FT測試程式.md) 核對主檔與軟體 Bin CREATE、硬體 Bin、簽核、DataStamp 及檔案管理。

## PRD PD204 電性抽樣規則家用端 handoff（2026-11-19）

- [ ] 依 [電性抽樣規則](docs/modules/PRD/電性抽樣規則.md) 核對主從 CREATE、系統 Bin、DataStamp、簽核和執行函式。

## PRD PD206 產品抽樣頻率規則家用端 handoff（2026-11-19）

- [ ] 依 [產品抽樣頻率規則](docs/modules/PRD/產品抽樣頻率規則.md) 核對 CREATE、萬用範圍鍵、各頻率欄位與 WIP 執行。

## PRD PD901 產品作業站標工維護家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站標工維護](docs/modules/PRD/產品作業站標工維護.md) 核對主表、歷史、SAP_KTEXT 與內部版次、查詢、匯入交易及轉 SAP。

## PRD OVERSTAY 產品作業站 Overstay 家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站 Overstay 維護](docs/modules/PRD/產品作業站Overstay維護.md) 核對主表與歷史、舊匯入三欄範圍和實際六欄主鍵、異常群組及交易。

## PRD OP RULE 產品作業站法則家用端 handoff（2026-11-19）

- [ ] 依 [產品作業站法則維護](docs/modules/PRD/產品作業站法則維護.md) 核對已存在的作業站 DDL、新增的歷史 DDL、CP／FT 條件及批量替換／匯入交易。

## PRD PDP01 晶圓良率與出貨報告家用端 handoff（2026-11-19）

- [ ] 依 [晶圓良率與出貨報告維護](docs/modules/PRD/晶圓良率與出貨報告維護.md) 核對四表 CREATE、候選、ComboSeqNo、停用及報告檢查。

## PRD PDP02 晶圓測試程式定義家用端 handoff（2026-11-19）

- [ ] 依 [晶圓測試程式定義](docs/modules/PRD/晶圓測試程式定義.md) 核對 CP 程式／Bin CREATE、匯入、簽核、DataStamp 與檔案管理。

## PRD PDP03 產品晶圓法則管制家用端 handoff（2026-11-19）

- [ ] 依 [產品晶圓法則管制管理](docs/modules/PRD/產品晶圓法則管制管理.md) 核對摘要／明細 CREATE、合併格、ImportType、原始列欄及正式腳本。

## PRD PDP04 產品晶圓圖疊圖規則家用端 handoff（2026-11-19）

- [ ] 依 [產品晶圓圖疊圖規則設置](docs/modules/PRD/產品晶圓圖疊圖規則設置.md) 核對三表、核准、批次複製、範本及正式疊圖引擎。

## PRD PDB01 挑片規則家用端 handoff（2026-11-19）

- [ ] 依 [挑片規則設定](docs/modules/PRD/挑片規則設定.md) 核對兩張 CREATE、區段欄位、同類倉庫、DataStamp 及條件執行。

## PRD PDB02 產品設定_封測家用端 handoff（2026-11-19）

- [ ] 依 [產品設定_封測](docs/modules/PRD/產品設定封測.md) 核對既有產品主檔、ECN 六表、正式 WIP 清線、SEN 批號及封測 API。

## PRD PDB04 檔案上傳基本資料家用端 handoff（2026-11-19）

- [ ] 依 [檔案上傳基本資料](docs/modules/PRD/檔案上傳基本資料.md) 核對檔案類型 CREATE、DataStamp、正式連線與伺服器端憑證。

## PRD PDB05 測試程式檔案管理家用端 handoff（2026-11-19）

- [ ] 依 [測試程式檔案管理](docs/modules/PRD/測試程式檔案管理.md) 核對檔案／歷程 CREATE、程式版本鍵、核准類型、傳輸與交易一致性。

## WIP WP201 生產批執行家用端 handoff（2026-11-19）

- [ ] 先 pull 最新 main，閱讀 WP201 規格、Oracle source DDL 三檔與 database/reference/mestest_wip_wp201_create.sql。此 CREATE-only 有 39 張來源表，不代表要新增 39 張 schema。
- [ ] 對照 YMES PostgreSQL live schema/migrations，沿用 WP101/WP102、OE、PRS、EQP、QC/SPC、INV 共用物件；只替缺表補 forward migration。Create reference 不含 FK、Check、secondary index 與 Oracle storage/index tablespace clauses。
- [ ] 規劃登入保護的 Lot 查詢、lookup 與 POST ExecuteLot command；不得 GET 修改在製狀態。後端重新驗證 Employee、LotStamp、批次狀態、NetFlow、Wait/Hold、Complete/OS、QTime/LMS、OP Group、Business Rule、設備／元件／SPC/QC 門檻。
- [ ] 交易性接續 wsWIP.ExecuteLot → Core TriggerOP／TransferLot。明確定義 ERF、Future Hold/Wait/OS、StopFlag、Complete+HOLD、狀態 11/12/31/99、Component RunBatchCO、動態 Manual BR DLL 的 YMES 對應與錯誤結果；不要假裝用單純 lot status CRUD 替代。
- [ ] TBLWIPECNLOTBASIS 與 VIEW_TBLWIPPACK_LOT 在公司端 MESTEST/PUBLIC 無表、view 或 synonym；確認 YMES 現況／替代客製物件後再決定是否實作該可選分支，不要建立猜測的 CREATE SQL。
- [ ] 依規格保留 API 錯誤碼、幂等鍵、CAS/LotStamp、operation log 與權限邊界；正式驗收需針對 status、併發、服務中斷／部分成功及自動／手動 BR 做回歸，成功後更新 migration/deploy record。

## WIP WP202 生產批執行_元件家用端 handoff（2026-11-19）

- [ ] Pull 最新 main，閱讀 [WP202 規格](docs/modules/WIP/生產批執行_元件.md)、[WP201 共用執行規格](docs/modules/WIP/生產批執行.md)，以及 `docs/DATABASE_CONNECTION.md` WP202 段落。
- [ ] Component JOIN 查詢使用 WP101 已交付的 `TBLWIPLOTSTATE`、`TBLWIPCOMPONENTSTATE`、`TBLWIPLOTBASIS`；先核對 YMES PostgreSQL live schema 與 migrations，不新增重複表。Oracle source／CREATE-only 參考為 `database/reference/mestest_wip_wp101_source_ddl.sql`、`mestest_wip_wp101_create.sql`。
- [ ] 建立登入保護的 Area／PS／OP Component JOIN 查詢，狀態限定 0／1／4／5，回傳 ComponentNo、OPNo、BRNo、StatusShow、BaseLotNo、LotNo、MoNo，參數化、分頁並依 ComponentNo 排序。OP lookup 可依 PS 限縮。
- [ ] ComponentNo 可對應多個 ComponentState 列／LotNo 時，確認實際唯一性；有歧義時回候選，不可無排序取第一筆。Execute 使用 POST，若 LotNo 有值維持 LotNo 優先，否則伺服器解析 ComponentNo，再接 WP201 共用 ExecuteLot command；重驗登入身份、權限、批次狀態與 LotStamp/CAS。
- [ ] API 必須沿用 WP201 的 TriggerOP、動態 BR、TransferLot、Future Hold／Wait／OS、StopFlag、ERF、幂等鍵及 operation log 決策；WP202 只增加元件選批入口，不建立簡化的獨立 Lot 狀態更新流程。完成後更新 API/schema、部署及驗收紀錄。

## WIP WP204 生產批執行_設備家用端 handoff（2026-11-19）

- [ ] Pull 最新 main，閱讀 docs/modules/WIP/生產批執行_設備.md、WP201 共用執行規格及 docs/DATABASE_CONNECTION.md WP204 段落。
- [ ] Oracle source DDL／CREATE-only：database/reference/mestest_wip_wp204_source_ddl.sql、mestest_wip_wp204_create.sql。先核對 PostgreSQL live schema/migrations；設備、狀態、WIP 與產品流程表沿用既有 WP101/WP201／主檔資源，只對缺表做 forward migration，不得照單全建四張。
- [ ] 建登入者授權的設備收藏／頁面查詢、設備狀態及顏色、LoadPort／在機批次、Lot Info、候選批次／Recommand、EQP／Lot log、QC List API；工作站與設備收藏需定義權限範圍及分頁。
- [ ] Check In／Out、Carrier Check In／Out、設備 Hold、全域 Hold、Eqp Change State 使用 POST command。不可 GET 寫入，也不可接受前端傳入的可派批次結果作為資格；伺服器重驗登入者、Equipment、LoadPort、Lot、LotStamp／狀態及關聯。
- [ ] CI／CO 接 WP201 的共用 ExecuteLot、TriggerOP、Business Rule、TransferLot 交易；保留 QTime/LMS、ERF、SPC/QC、設備／Carrier 與 operation log side effects。定義 CAS/鎖定、冪等、部分失敗與錯誤碼，不用簡化的 LotStatus 更新替代。
- [ ] 四張 CREATE SQL 為原生 Oracle 結構參考；來源中 TBLEQPFAVORITE 及 TBLPRDSTAGESTEPSET、DETAIL 沒有 PK，TBLWIPLOTGPEQUIPMENT 只有 LOTNO/OPNO 非唯一索引。不可自行補猜測 PK/FK。

## WIP WP308 載具管理_元件家用端 handoff（2026-11-20）

- [ ] Pull 最新 main，閱讀 docs/modules/WIP/載具管理_元件.md、docs/modules/WIP/載具現況管理.md 與 docs/DATABASE_CONNECTION.md 的 WP302/WP308 段落。
- [ ] WP308 沒有新增 Oracle 表或 CREATE-only。先核對 YMES PostgreSQL live schema／migration history；重用 Carrier Basis/Type、CarrierState、Detail/DetailLog、LotBasis/State、ComponentState、StockerState。只有實際缺表才新增 forward migration，不要重建 reference 已覆蓋的表。
- [ ] 建登入保護的載具查詢、明細、CarrierType、可用載具、Lot/Component/Stocker 驗證 lookup；遵守查詢權限範圍、參數化、分頁。
- [ ] Carrier In、Carrier Out、Change Carrier 全用 POST command。交易內重新檢查 Carrier IssueState/UseLayer/容量、LotState/status/CarrierNo、Component SlotNo、StockerState 與 Carrier Detail；為兩個載具及 Lot/Component 關聯定義鎖定/CAS，確保歷程、明細、Lot carrier no、Component SlotNo、CarrierState 一致回滾。
- [ ] 保留來源差異：Carrier Out 目前不清 ComponentState.SlotNo；Change Carrier 會清舊值並設新值；Carrier Change 的候選以 IssueState=2、無 Detail 與 UseLayer 過濾，不以 CarrierState 單獨判空；Carrier Change Lot 掃描沒有 Stocker 檢核。若正式端改行為，需明確記為功能修正。
- [ ] 共享 WP302 假資料的 DEV localStorage 不是正式 DTO/schema；正式 API 需從資料庫重讀 serial、component 與 slot，再寫 Detail/DetailLog。完成 API、migration（若確有缺表）、錯誤碼與交易回歸後更新 API/schema/deploy records。

## WIP WP303 設備耗品現況家用端 handoff（2026-11-20）

- [ ] Pull 最新 main，閱讀 `docs/modules/WIP/設備耗品現況.md`、本段 `docs/DATABASE_CONNECTION.md` 與 `database/reference/mestest_wip_wp303_*`。
- [ ] 兩張 Oracle 專用表已交付 PostgreSQL CREATE-only：`ymes.tblwipeqpconsumestatus`、`ymes.tblwipeqpconsumelog`。先查 PostgreSQL live schema／migration history；只為缺表建立 forward migration，不可把 CREATE-only 直接套用到既有環境。`ymes.tbleqpconsumebasis` 與設備主檔沿用既有 reference。
- [ ] 實作登入保護、權限範圍明確、分頁排序的設備耗品現況列表；Equipment lookup 限 IssueState=2，ConsumeNo lookup 依 EquipmentType 查 ConsumeBasis。
- [ ] 實作新增／編輯／刪除：Status PK 為 EquipmentNo+ConsumeNo；新增 Creator/CreateDate；編輯依 WinForms 只改 ConsumeSeq/CurQty/CurPeriod；刪除支援多選。來源多選刪除逐列執行，須明確選擇相容逐列部分成功或採原子 bulk-delete。
- [ ] 實作 Reset POST command：以資料庫最新 Status/Basis 重算，於同一交易 INSERT 舊 ConsumeSeq 至 Log（Creator、CreateDate、AccQty、AccPeriod），再 CAS 更新 Status 的新 ConsumeSeq、CurQty=0、CurPeriod=0、Revisor、ReviseDate。任何步驟失敗都回滾；禁止以 GET 執行重置。
- [ ] ConsumeSeq 重複檢查在來源以 ConsumeNo 為範圍查 Log，跨設備共用序號時也拒絕；此規則未由 PK 保證，需解決併發重複（資料庫鎖／交易級一致性檢查），但不可擅自新增 Oracle 不存在的唯一約束。編輯不更新 Revisor/ReviseDate 是 Core 來源行為。
- [ ] 保留並評估來源的重置邊界瑕疵：Period=-1 代表不限；RemainPeriod 不足顯示時用 ToleranceQty（疑似應為 TolerancePeriod）；達用量／週期上限時 Reset 視窗會把 AccQty／AccPeriod 輸入清為 0。若修正，記為產品規則變更並取得辦公室確認。
- [ ] CREATE-only 與 WinForms 假資料不構成正式 schema/API。完成 migration（若缺）、API／DTO、授權、錯誤碼、交易回歸與部署紀錄後更新此交接；WP304 設備耗品歷程交接列於下一節。

## WIP WP304 設備耗品歷程家用端 handoff（2026-11-20）

- [ ] Pull 最新 main，閱讀 `docs/modules/WIP/設備耗品歷程.md`、WP303 `docs/modules/WIP/設備耗品現況.md`，以及 `docs/DATABASE_CONNECTION.md` WP303/WP304 段落。
- [ ] WP304 沒有新增 Oracle table／CREATE-only。只讀 WP303 已交付的 `ymes.tblwipeqpconsumelog`；先核對 PostgreSQL live schema／migration history，不要新增重複 migration。來源 DDL、metadata、CREATE-only 在 `database/reference/mestest_wip_wp303_*`。
- [ ] 建立登入保護的唯讀 GET 查詢，支援 EquipmentNo、ConsumeNo、StartTime、EndTime；StartTime 使用 `CreateDate >=`，EndTime 使用 `CreateDate <=`，均含界值。WinForms 頁沒有 ConsumeSeq 欄位，即使底層 Service 支援，也不擴大此頁 UI。
- [ ] 來源頁不帶條件也會查完整 log。正式 API 必須以使用者權限限制可見資料、參數化查詢、穩定排序與伺服器端分頁／最大筆數；不可讓 UI 一次下載全表。可要求日期區間或提供安全的預設範圍，但需將其記錄為相對 WinForms 的明確效能／安全調整。
- [ ] Equipment lookup 限 IssueState=2；ConsumeNo lookup 讀全部 Equipment Type Consume Basis，不隨設備類別縮小。DTO 至少提供 EquipmentNo、ConsumeNo、ConsumeSeq、AccQty、AccPeriod、Creator、CreateDate。
- [ ] WP304 Vue DEV 會在開頁時讀取 WP303 共用假 log localStorage；正式端改為 GET API，勿把 localStorage 假資料當成伺服器資料或授權依據。
- [ ] 完成 API、查詢權限、分頁、空條件保護及部署紀錄後更新 TODO；接續公司端 WP305 設備線上點檢。

## WIP WP305 設備線上點檢家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/WIP/設備線上點檢.md`、`docs/DATABASE_CONNECTION.md` WP305 段落，以及 `database/reference/mestest_wip_wp305_metadata.txt`。
- [ ] WP305 Oracle source DDL、index DDL、metadata 與 PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp305_source_ddl.sql`、`mestest_wip_wp305_source_indexes.sql`、`mestest_wip_wp305_metadata.txt`、`mestest_wip_wp305_create.sql`。CREATE-only 包含點檢 Log/Detail/Task 及設備狀態 Log、`TBLWIPCONT_EQUIPMENT`、`TBLWIPLOTLOGINSTATE` 三張失敗處置副作用表。先比對 YMES live schema／migration history，缺少才新增 migration；equipment-state basis、QCList Basis/Detail/Setup、Equipment、`TBLWIPTEMP_EQUIPMENT` 沿用既有 reference。
- [ ] `TBLWIPCONT_EQUIPMENT` 在 Oracle 沒有 PK/unique index；不可在 YMES 猜測主鍵或宣告不實唯一性。三張功能 Log/Detail/Task 的 PK 依 Oracle source metadata；非唯一索引照 DDL reference 核實後再建。
- [ ] 建登入保護的 Equipment/EquipmentType、QC List Basis/Detail、Wait QC/Re QC candidate API。Wait QC 必須遵守 QCTaskClass=1、Status=0、設備設定及報工模式 BelongDate/最近 PlanQCTime；ReQC 僅 Status=2。多候選不得無排序自動取第一筆。
- [ ] 新增點檢使用 `POST`，修改既有 Log 使用 `PUT`；禁止 GET 寫入。Server 從 QCList Detail 重新計算 QCType 0 exact / 1 inclusive range（`*` 無界）/ 2 OK-NG / 3 text 的 item 結果與整體 QCResult，不接受瀏覽器提交的 QCResult 作權威值。保留 InputDataCount 及 D01-D10。
- [ ] Header + Detail + 若有 QCTask 的 Status=1、QCSerialNo/任務欄位回填須同一 transaction；以唯一 Serial、task row lock/CAS、idempotency 防重複提交與補驗競態。獨立模式部分存檔可留 QCResult=-1；Check-in 呼叫必須拒絕未完成項。
- [ ] 若 QCResult=NG 且 QCList UnValidAction 有設定，Core 會改設備狀態並寫 EquipmentStateLog。若設備原為 WIP CheckInConfirm，還會呼叫 SHR_COEquipment 結算設備上的批次、更新設備歷程、刪除 Temp Equipment、關閉 Cont Equipment、更新多人上工 LotLoginState 簽退。正式 API 必須接 WP201/WP204 共用下機交易或等價一致 transaction；不可只建立 QC Log 後遺漏設備/WIP/人員副作用，也不可允許半套提交。
- [ ] WP305 DEV localStorage `ymes.dev.wp305-equipment-online-qc.v1` 只供假畫面及 WP306 DEV 假歷程使用；不得當作正式 API 資料、授權或 task 狀態依據。完成權限、transaction、錯誤碼與 API/migration/deploy records 後更新交接。

## WIP WP306 設備點檢歷程家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 `docs/modules/WIP/設備點檢歷程.md`、WP305 規格與 `docs/DATABASE_CONNECTION.md` 的 WP305 共用表 DDL 段落。
- [ ] WP306 沿用 WP305 已建的 `TBLWIPEQPQCLISTLOG`／`TBLWIPEQPQCLISTDETAIL`；先查 YMES PostgreSQL live schema／migration，不要重複建表。沒有新增 Oracle DDL／CREATE-only。
- [ ] 建登入保護的唯讀查詢 API，支援 WinForms 篩選欄位與穩定分頁排序；同一 QCSerialNo 回傳點檢 Header 與所有 Detail，避免把不同檢驗序號的 detail 混在一起。
- [ ] 限制可查詢設備範圍及最大日期/頁數；權限依使用者與 WIP 功能設定。WP306 是唯讀歷程，不透過此 API 修改 Log/Detail。

## WIP WP307 設備狀態監控_區域家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP307 規格](docs/modules/WIP/設備狀態監控_區域.md)、SMD SD108/SD104 規格及 `docs/DATABASE_CONNECTION.md` WP307 段落。
- [ ] Oracle source DDL、index DDL、metadata 與 PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp307_source_ddl.sql`、`mestest_wip_wp307_source_indexes.sql`、`mestest_wip_wp307_metadata.txt`、`mestest_wip_wp307_create.sql`。`TBLSMDAREARELATION` 已在 `mestest_equipment_group_smd_*`，不要重複 migration；先核對 YMES live schema／migration history，僅建立缺少的兩張 WIP 表。
- [ ] `TBLWIPAREAOPERATORSTATE` 的 PK 是 UserNo，表示跨全區域每位使用者最多一筆 Current 狀態；以此唯一性防止同一使用者同時登入多區。`TBLWIPAREAOPERATORLOG` 在 Oracle 無 PK/index，不能為方便而加入虛構的 source unique key/index。
- [ ] 實作登入保護、WIP307/IMESTECH 功能授權與參數化 API：eligible 檢查目前班別、AreaOperator、UserShift 時段及既有登入，回傳全部目前合格 ShiftNo 並保持來源順序；伺服器要處理跨午夜班別，並自行產生 LoginTime。
- [ ] 實作 GET `/api/wip/areas/{areaNo}/operators/filter-options?departmentNo=`，回傳該部門 IssueState=2 班別（ShiftNo、ShiftName、FromTime、ToTime）與使用者（UserNo、UserName），供 State/History 的 ShiftNo/UserNo `...` 查找；使用者清單不可縮限為 AreaOperator 排班名單。
- [ ] Login POST 在交易內重新驗證當下排班後插入 Current。Logout POST 鎖定 UserNo Current row、核對 area，從 AreaShift 取得 ExceptionTime，原子刪除 Current 並新增 History，LogoutTime 由伺服器產生；使用者不得傳入任意 ExceptionTime。
- [ ] 提供 State/History 篩選 GET，欄位、可選核取條件與日期分頁見規格。WinForms Core 的 State 日期界值為嚴格 `>`/`<`；若改成含界值，需明確記錄相對來源行為的 API 決策。結果採穩定排序、權限範圍及安全分頁。
- [ ] DEV localStorage `ymes-dev-mock:wip-area-operator-state:v1`、`ymes-dev-mock:wip-area-operator-log:v1` 只用於公司端畫面互動，正式 API 不得讀取或信任瀏覽器假資料。Migration/API transaction、權限與部署紀錄完成後，更新 office/home TODO。

## WIP WP203 過帳設備設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP203 規格](docs/modules/WIP/過帳設備設定.md)、`docs/DATABASE_CONNECTION.md` 的 WP203/WP204 段落及 `todolist_officepc.md` WP203 段落。
- [ ] WP203 沒有新 Oracle 表。沿用 `TBLEQPFAVORITE` 的 WP204 source DDL/CREATE-only，以及已存在的 EquipmentBasis/EquipmentType references；先核對 YMES live schema/migration history，不重複建表。
- [ ] **先解決 HostId Web 身份對應。** WinForms HostId 是 `gComputerName` 去掉最後一個 `/` 元素所得的 workstation id，不是 UserNo。API 從可信登入 session 或伺服器端 workstation binding 解析；不得接受瀏覽器任意 HostId。若目前登入流程沒有可信 workstation mapping，先記錄需要的架構決策，不能暗中改成 user-scoped favorite。
- [ ] 實作登入保護 API：GET `/api/wip/equipment-favorites` 列該 Host 的 PageNo；GET `/{pageNo}` 讀明細；GET `/options?issueState=2` 同時回已核准 EquipmentType 與 EquipmentBasis。Server 強制 `IssueState=2`，且可選設備必須 `ChamberFlag=0`。
- [ ] POST 根路徑新增/複製 `{pageNo,equipmentNos,replaceExisting}`；UI 取代確認後才傳 true。PUT `/{pageNo}` 編輯並保持 PageNo 不可修改；DELETE `/{pageNo}` 刪單頁。所有寫入參數化並在 server 重驗設備核准狀態、EquipmentType、主設備資格及 1–15 個唯一 EquipmentNo。
- [ ] Favorite source table 無 PK/unique index。不得新增 source 不存在的 unique constraint；用 PostgreSQL transaction-scoped advisory lock（固定 hash(HostId,PageNo)）或同等跨多個 API instance 的序列化，將舊頁刪除與整批設備重建放在同一交易，防止同頁併發 duplicates。
- [ ] Main form 多選 Delete 是逐頁執行，可能部分成功。正式 API可逐頁呼叫並回報已刪/失敗頁；不可假稱來源是整批原子刪除。
- [ ] Vue DEV localStorage `ymes-dev-mock:wip-eqp-favorite:pages:v1`、`ymes-dev-mock:wip-eqp-favorite:equipment:v1` 只供假 UI，Production 不得使用；API、權限、交易、部署完成後更新交接。

## WIP WP205 生產批選用生產設備家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP205 規格](docs/modules/WIP/生產批選用生產設備.md)、WP101／WP201／WP204 規格與 `docs/DATABASE_CONNECTION.md` WP205 段落。
- [ ] WP205 無新增 Oracle table／CREATE-only；先核對 YMES live schema/migration，重用 LotState／TempLotState、TempEquipment、Equipment/EMS、Area Relation/Group、PRD OP Capability。不可為此頁重複建共用表。
- [ ] 提供登入保護查詢：LotNo + Status=1 的 LotState；以 `LoadAvailableEquipment_All` 同等伺服器規則回傳 Area 全部設備、AllowExecute、Capacity、EQPLoadPort、Description 及原因；設備 LoadPort 查詢需回傳占用 Lot 與空 Port。
- [ ] 提供登入保護的選機 POST command，接受 LotNo、LotStamp、唯一 EquipmentNo 清單與可選 LoadPort。交易內重讀 Lot/Temp LotState，要求 OPStatus=1 且 Lot 尚未上機；重算設備核准／開關／狀態、產品 OP Capability、Capacity（-1 不限）、LoadPort 占用與設備數量，再執行 `SHR_CIEquipment` 與 OperatorState Mode=1/MultiOperatorMode=5 的副作用；透過 LotStamp CAS 防止舊資料，任一步失敗 rollback。
- [ ] 多設備選擇時拒絕 LoadPort；單設備 EQPLoadPort>0 可指定但非必填。不能信任瀏覽器傳回的 AllowExecute、Capacity 或 Port 空位，正式端不得讀取 DEV localStorage `ymes-dev-mock:wip-lot-choice-equipment:assignments:v1`。
- [ ] DEV-only FunctionNo `LOT CHOICE EQP`／FunctionKey `WP205` 的 localStorage 樣本不是正式授權資料。完成 live schema 比對、API、交易、WP205 功能授權與部署紀錄後更新 handoff。

## WIP WP206 計數不良原因收集家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP206 規格](docs/modules/WIP/計數不良原因收集.md)、`docs/DATABASE_CONNECTION.md` WP206 段落及 DDL／metadata／CREATE-only references。
- [ ] `TBLOPABNORMAL` 已在 `database/reference/mestest_operation_and_system_*`，不可重複 migration。新增表 reference 是 `TBLQCABNORMALBASIS`、`TBLQCABNORMALPOSITION`、`TBLWIPCONT_ABNORMAL`；先查 YMES live PostgreSQL schema 與 migration history，缺少才建 forward migration。
- [ ] Oracle source DDL、index DDL、metadata、PG CREATE-only 在 `database/reference/mestest_wip_wp206_source_ddl.sql`、`mestest_wip_wp206_source_indexes.sql`、`mestest_wip_wp206_metadata.txt`、`mestest_wip_wp206_create.sql`。`TBLWIPCONT_ABNORMAL` 無 PK/FK/Trigger，只有 `LOTSERIAL` 非唯一 index；不得補造 PK／unique constraint。
- [ ] 建登入保護的 OPClass=1／IssueState=2 作業站 lookup、OP 關聯異常 lookup、異常位置 lookup；非同步以一次有限查詢取得 LotNo，不延續 WinForms Enter 時無條件載入全 LotState 的程式。
- [ ] 建登入保護的 IPQC POST command，伺服器驗證 Lot、OP、異常關聯及 Position 關聯，在單一交易新增選取資料。舊 Core 將 LotNo 寫到 LotSerial、AbnormalQty 留 DB default 0，且忽略 LinkName/Priority/UserNo/ShiftNo/EventTime；正式 API 若補填需標示為擴充。防止多次提交重複資料，但不新增 Oracle 沒有的 source key；使用 request idempotency 或明確冪等策略。
- [ ] DEV localStorage `ymes-dev-mock:wip-ipqc:abnormal-records:v1` 只供公司端假互動，正式端不得讀取。API/schema/交易/授權/部署完成後更新交接。

## WIP WP207 分批處理家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP207 規格](docs/modules/WIP/分批處理.md) 與 `docs/DATABASE_CONNECTION.md` WP207 段落。公司端 UI 在 `web/src/components/WipLotSplit.vue`，僅 DEV 假資料，不代表正式過帳。
- [ ] WP207 六張缺失 reference 的 Oracle source DDL、index DDL、metadata 與 PostgreSQL CREATE-only 分別在 `database/reference/mestest_wip_wp207_source_ddl.sql`、`mestest_wip_wp207_source_indexes.sql`、`mestest_wip_wp207_metadata.txt`、`mestest_wip_wp207_create.sql`。先核對 YMES live PostgreSQL schema/migration history，只為實際缺少的表新增 forward migration；Lot/Process/Product/Temp/Inventory/Matrix 共用結構沿用 WP101／WP201 與主檔 references，不重複建表。
- [ ] 六張表為 `TBLEMSDISPATCHSTATE`、`TBLWIPAUTOMERGE`、`TBLWIPCONT_EMPLOYEE`、`TBLWIPLOTOPECNRECORD`、`TBLWIPOSBASIS`、`TBLWIPOSDETAIL`。Employee 表沒有來源 PK/unique index，僅三個 non-unique index；不可加虛構的來源唯一鍵。其他表 PK 以 metadata 為準。
- [ ] 實作登入保護的指定 Lot 查詢，要求原 WinForms Status `0|1|4|5`、HaveComponent=0、LotState=1、非 Partial/Stocker、無 GoodBinState、未包裝及使用者可見範圍；查產品／流程節點／單位換算／是否可顯示 Electricbox 的 API。路由和產品候選應由 server 依批次、權限與系統參數提供，不能沿用 Vue 固定選項。
- [ ] 實作分批 POST command，使用 LotNo、LotStamp、IncludeMother、各列 LotQty／SysQty／SAP_PRDQTY／SAP_PCSQTY／Action／NodeID／Product／Version／UnitNo。Server 在交易內重驗正數、四組合計、ReturnType、包裝／GoodBin／委外守門、LotStamp、產品與流程有效性；按來源 `GetSplitLotNo_Multiple` 語意原子保留子批號，拒絕舊預覽號與重複提交。BR／WIP Inventory 宿主模式須由可信內部上下文區分，不接受瀏覽器任意指定。
- [ ] 以 `BR_Manual_LotSplit` 的 Lot/Temp、報工、屬性、流程、庫存、歷程、委外、派工等多表變更為規格，追查 Core helper 的額外共享表。標準拆批成功後 Form 另呼叫 THEIL `UPD_SplitLot` 寫 `TBLWIPLOTBASIS.SAP_PRDQTY/SAP_PCSQTY`、`TBLWIPLOTPROPERTY.WIP_BIGQTY`、`XX_TBLLOTMARITX_THEIL_BU1`；正式端要同一原子交易，或明確可恢復且冪等的二階段流程。失敗回應需區分標準拆批未提交與客製更新失敗，避免重複拆批。
- [ ] 客製 Core 的 `UPDATE ... SAP_PCSQTY = <value>WHERE ...` 字串缺空白，依原始碼字面可能 SQL 無效；請以參數化 SQL 修正，確認 BaseLotNo/OrgLotNo 映射與 Matrix 複製。不複製字串拼接或瀏覽器 localStorage `ymes-dev-mock:wip-lot-split:*`。完成 migration/API/授權/交易驗證與部署紀錄後更新 TODO。

## WIP WP208 分批處理_元件家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP208 規格](docs/modules/WIP/分批處理_元件.md)、[WP207 共用拆批規格](docs/modules/WIP/分批處理.md) 與 `docs/DATABASE_CONNECTION.md` WP208 段落。公司端 UI 是 `web/src/components/WipLotSplitComponent.vue`，只有 DEV 假資料。
- [ ] WP208 的 `TBLSYSPARAMETER` 原先缺 SQL reference；公司端已從 MESTEST 唯讀取得 `database/reference/mestest_wip_wp208_source_ddl.sql`、`mestest_wip_wp208_source_indexes.sql`、`mestest_wip_wp208_metadata.txt`，並提供 `mestest_wip_wp208_create.sql`。Oracle 原表 25 欄，PK `PARAMETERNO`，無 FK／trigger／其他 index。先核對 YMES live schema/migration history；這是共用參數表，已有就不重建，只為真正缺口做 forward migration，不用 CREATE-only 重播。
- [ ] 其餘 26 張直接依賴表沿用已交付 reference：`TBLINVWIPINVENTORY_COMPONENT` 用 WP603；`TBLWIPCOMPONENTSTATE` 用 WP101；ComponentProperty／Cont／Temp ComponentAttrib／Temp Equipment 用 WP201；`TBLWIPCONT_EQUIPMENT` 用 WP305；派工／AutoMerge／Cont Employee／Lot OP ECN／OS 六表用 WP207；其他 Lot／Product／Process／System 用既有主檔與 WP201。不要重複建表或自創無來源 PK。
- [ ] 建登入保護的指定 Lot 與 Component 查詢，限制 Status、HaveComponent、LotState、非 Partial／Stocker、載具／庫存範圍與使用者權限；產品和路由 lookup 依真實 `AllowLotProductChange`、ECN／流程設定回傳。WIP Inventory／BR 宿主模式只由可信 server 上下文設定。
- [ ] 實作拆批 POST command：在交易內重讀／鎖定 LotStamp、ComponentState、Inventory 和序號，驗證每列數量、元件完整／唯一分配、未被其他子批／庫房占用、Action／ReturnType／NodeID、產品／路由；產生並保留批號，執行 `BR_Manual_LotSplit_Component` 對應的 Lot、Component、Temp、報工、庫存、派工及歷程副作用。不要信任 DEV 預覽號、localStorage 或瀏覽器提交的 SplitType=1。
- [ ] 逐層追查 Core helper 對其他表的讀寫，並處理 BR 模式成功後 TransferLot、SEMI_Wafer `S_BatchID`／`LC_CI` 的後續轉站。若不能與拆批同一交易，提供可觀察且冪等的第二階段，避免已拆成功後重按 Confirm。WP208 不呼叫 WP207 的 THEIL `UPD_SplitLot`，不可把兩者誤併成同一輸入／SAP 規格。

## WIP WP209 分批處理_等級家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP209 規格](docs/modules/WIP/分批處理_等級.md) 與 `docs/DATABASE_CONNECTION.md` WP209 段落。公司端 Vue `web/src/components/WipLotSplitBin.vue` 只供 DEV 假資料；正式版需保留原 WinForms 的母批／子批／Bin 三區、Count／Qty、Include Mother、路由／產品與靜電箱行為，不能讀 localStorage 當 MES 資料。
- [ ] `TBLWIPSPLITCONTENT_BIN` 已由公司端 Oracle MESTEST 唯讀取得 source DDL、index DDL、metadata，檔案為 `database/reference/mestest_wip_wp209_source_ddl.sql`、`mestest_wip_wp209_source_indexes.sql`、`mestest_wip_wp209_metadata.txt`；PG CREATE-only 為 `mestest_wip_wp209_create.sql`。來源 22 欄、單欄 PK `TOLOTSERIAL`，無 FK／trigger／其他 index。先核對 YMES PostgreSQL live schema／migration history，僅在確實缺表時新增 forward migration；其他 23 張已知直接依賴表沿用既有 refs，不重建。
- [ ] `TBLWIPSORTERBINDATA` 目前在公司 MESTEST `ALL_TABLES`／`ALL_SYNONYMS`／`ALL_OBJECTS` 查無。Core 的 LED_PKG 分支使用此表；取得正確 Oracle schema／環境或 vendor DDL 後才補 source／PG CREATE，不能由 Core 使用欄位臆造 migration。正式 LED 分支在 DDL 與副作用確認前保持未交付，並明確回應不可用。
- [ ] 建登入保護的指定 Lot、GoodBinState、SysBin、Product／Routing 查詢。重現 WinForms Status `0|1|4`、LotState=1、HaveLevel=1、非 Partial／Stocker 與有 GoodBinState 守門。Bin 目錄依 `BinOrder, BinNo` 動態返回，不能固定 DEV 三 Bin。路由、產品及 `IC_FT_PKG`／`AllowLotProductChange` 由可信 server 讀取。
- [ ] 拆批 POST 在交易內鎖定並重讀 LotStamp、GoodBinState、庫存、序號及每個 Bin 餘量。驗證每子批 Qty 等於 Bin 分配、全子批等於 CurQty、每 Bin 不超量且總分配與來源相等、非負及精度、Action／ReturnType／NodeID、產品／流程權限。序號以 server 原子保留，拒絕舊預覽號／重複提交；BR／Inventory／Electricbox 模式只接受可信 server 上下文。
- [ ] 移植 `BR_Manual_LotSplit_Bin` 的 Lot、GoodBin、Temp、庫存、報工、派工、載具／靜電箱與歷程副作用，逐層追查 Core helper 的額外共用表。BR 宿主成功後的 TransferLot、`BindByMLotNo` 複製載具明細須有同交易或可恢復且冪等的階段結果。正式 PostgreSQL/API/部署與驗收仍由家用端負責；公司端未連 PostgreSQL、未建立 migration/API、未執行 build/test/browser。

## WIP WP210 分批處理_載具家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP210 規格](docs/modules/WIP/分批處理_載具.md)。公司端 Vue `web/src/components/WipLotSplitCarrier.vue` 只有 DEV 假資料；正式 Web 不得讀 `ymes-dev-mock:wip-lot-split-carrier:state:v1` 或將 Slot 畫面假結果當正式過帳。
- [ ] 核對 YMES PostgreSQL live schema／migration history。兩條 Core 直接引用的 17 張表已在 `database/reference/` 有來源 CREATE TABLE，Carrier Basis／Type 亦有既存共用 refs；按 live 差異補必要 forward migration，不重建已有表。逐層追查 `SHR_Split_GetReference`、`UpdLotAttrib`、Reverse／LotLog 等 helper 的其他間接表。
- [ ] 建登入保護的指定 CarrierNo 查詢：先讀 CarrierState／Detail，必須有**唯一** Lot；LotState=1、HaveLevel=0、非 Partial，拒絕 Wait／Hold／Complete，Status=5 時 Confirm 查委外未回件。由 server 決定 HaveComponent，並提供空載具候選與 CarrierLayer×CarrierCapacity 的位置／類別，不能信任瀏覽器回傳的來源 Lot、原 CarrierNo、Status 或 HaveComponent。
- [ ] 無元件拆批支援 Count／Qty、母批 Yes／No、每列 Qty／Carrier／Action／Route／ReturnType。POST 交易內重新鎖定 LotStamp、來源與目標載具、批號序號；驗證 Qty 正數及總量、不同且可用 Carrier、母批只能 Go、Go／Jump OP ReturnType=0、跳站 NodeID 存在。預覽子批號不得由瀏覽器保留或當可信識別。
- [ ] 有元件拆批固定兩批，讀真實 ComponentState、CarrierStateDetail、載具容量與原位置；由 server 核對每顆元件完整、唯一、屬於原批，兩邊均非空，Slot 目標位置未占用、各 Carrier 容量與位置唯一。原元件／新元件、Lot、CarrierState／Detail／DetailLog、Temp／Cont、報工／逆轉／歷程須一致寫入；防重與失敗回復需覆蓋並行載具占用。
- [ ] 原主 Form 的 Change Product 按鈕雖修改暫存列，Confirm XML **未送 ProductNo／Version**，不應直接當成正式已有的產品變更。請決定相容模式停用此按鈕或取得業務確認再擴充 API；不要從 DEV localStorage 推斷來源生效。BR SplitType=1 之後另做 TransferLot，若非同交易，須回可觀察、可重試、冪等的階段結果。公司端未連 PostgreSQL、未建正式 API/migration、未執行 build/test/browser。

## WIP WP211 併批處理家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP211 規格](docs/modules/WIP/併批處理.md) 與 `docs/DATABASE_CONNECTION.md` WP211 段落。Vue `web/src/components/WipLotMerge.vue` 只供 DEV 假資料；正式路徑須禁用 `ymes-dev-mock:wip-lot-merge:records:v1`，不能把本機模擬結果當 MES 過帳。
- [ ] 核對 YMES PostgreSQL live schema／migration history；Core 直接引用的八張表已在 `database/reference/` 有 Oracle source DDL 和 PostgreSQL CREATE-only，不要重建現存表。逐層追 `BR_Manual_LotMerge` helper 的間接表、索引、更新與資料一致性，確有缺口才建 forward migration。
- [ ] 提供有權限的 Reference Lot／候選 Lot、MO 多選、Routing、未回貨 OS、GoodBin／包裝狀態查詢。原生參考條件 `Status=0|1|4|5`、`HaveComponent=0`、`NotContainGoobinState=True`、`LotState=1`、非 Partial／Stocker；一般候選同 OP／Product／BR／Status／Unit；`SysInfo/AllowDiffMOMerge=0` 才限同工單。Status=5 走同一未回貨外包單，Confirm 再重查。
- [ ] 實作原子併批 POST，server 重讀並鎖定所有選取批及 LotStamp、CurQty、包裝／GoodBin／OS 現況與輸出批號；驗證 Action、Assign 路由／NodeID、數量與版本，完成 LotBasis／State、Reverse／Log、Dispatch／AutoMerge／OS／Inventory／員工副作用。原 Form 的至少兩筆檢查已註解，不能當來源硬規則；若產品需要禁止單批併批，應另外列為 YMES 新規則。相同 OP 不同 NodeID 要明確要求確認。
- [ ] `MergeType`、WIPINV、UserNo、ShiftNo、時間與部署參數從可信 server 上下文取得，不能由瀏覽器任填。BR 宿主 Go 後另做 `BRTransferLot`；若不能同交易，提供可查詢且可冪等重試的部分完成狀態。電箱綁定須串接真權限／服務，不採 DEV 假確認。正式 API、PostgreSQL、部署與驗收均由家用端完成；公司端未連 PostgreSQL、未建 migration／Production API、未執行 build/test/browser。

## ET105程式碼同步紀錄（2026-11-20；main仍待整合）

code `ab0f8269e9778d88811947512e6753e51437703d`已正常推送`refs/heads/codex/homepc-et105-20261120`並直接讀回相同；SQL1／API154／Web224共379份來源綁定通過，receipt SHA `2ac94056c8c82c76eedf698e64fcaa242f388a66f74a0777db1522c9c3690bae`，未允許差異0。後續純文件狀態提交不改本release code或重建產物。

QA完成後已fetch公司端`095b3d267bc102687c32b25c9a3f593cf1ebfda9`，九個新WIP頁及CREATE交接另行整合／建置／驗收。本次224來源保持，mainSyncPending=true；公司端與家用端歷史均保留，沒有force push。完整checkpoint以Root後續實際套用收據為準。

## WIP WP212 併批處理_元件家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP212 規格](docs/modules/WIP/併批處理_元件.md) 與 `docs/DATABASE_CONNECTION.md` WP212 段落。Vue `web/src/components/WipLotMergeComponent.vue` 只供 DEV 假資料；正式環境禁用 `ymes-dev-mock:wip-lot-merge-component:records:v1` 與本機模擬確認。
- [ ] 核對 YMES PostgreSQL live schema／migration history。Core 直接引用十張表已在 `database/reference/` 有 Oracle source DDL 和 PostgreSQL CREATE-only，不重建既有表；追 `SHR_Merge_Inherit`、Reverse、LotLog、`udWIP.clsWIP.BR_Manual_LotMerge_Component` 等 helper/hook 間接表及部署差異，再決定 forward migration。
- [ ] 提供有權限的 Reference Lot／候選／MO／元件明細／Routing 查詢。一般模式參考 `Status=0|1|4`、`HaveComponent=1`、`LotState=1`、非 Partial／Stocker；候選同 OP／Product／BR／Status／CurUnit，`HaveLevel=0`，且 `AllowDiffMOMerge=0` 才限同工單。參考批的 HaveLevel 條件已註解，候選仍要求 0，須清楚處理兩者不一致；WIP Inventory 模式另限同 InventoryNo、BookingFlag=0 並核跨庫房元件。
- [ ] 原 Form 每筆來源的載具檢查迴圈**誤用輸出 `txtLotNo`**。正式 POST 必須對每個選取來源 Lot 分別重查 CarrierState／CarrierStateDetail，任何已在載具的批次都拒絕並引導至 WP214，不能沿用原 bug。至少選兩批且保留參考批；server 鎖並重查 LotStamp、CurQty、ComponentState 全體元件、元件歸屬／數量／唯一性、輸出批號、Action／Assign NodeID、User／Shift／模式與系統參數。
- [ ] 在同一交易完成來源 Lot 關閉、合併 Lot 建立、Reverse／LotLog、ComponentState、派工／AutoMerge／Cont Employee、WIP Inventory Semi／Component 與跨庫房回補；失敗回滾且防重。BR 宿主 Go 的 Form 另呼叫 TransferLot，Core 註解又寫一律手動 Transfer，需按實際宿主模式提供可查、可冪等重試的部分完成結果，避免重按 Confirm。Production API／PostgreSQL／部署驗收由家用端負責；公司端未連 PostgreSQL、未建正式 API/migration、未執行 build/test/browser。

## WIP WP213 併批處理_等級家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP213 規格](docs/modules/WIP/併批處理_等級.md) 和 `docs/DATABASE_CONNECTION.md` WP213 段落。Vue `web/src/components/WipLotMergeBin.vue` 僅是 DEV 假資料；正式環境不得讀 `ymes-dev-mock:wip-lot-merge-bin:records:v1` 或使用固定三 Bin fixture。
- [ ] `TBLWIPMERGECONTENT_BIN` 已由公司端 Oracle MESTEST 唯讀取得 source DDL、index DDL、metadata：`database/reference/mestest_wip_wp213_source_ddl.sql`、`mestest_wip_wp213_source_indexes.sql`、`mestest_wip_wp213_metadata.txt`；PG CREATE-only 為 `mestest_wip_wp213_create.sql`。22 欄、PK `FROMLOTSERIAL`、無 FK／trigger／額外 index。先核對 YMES PostgreSQL live schema/migration history，確實缺表才建 forward migration；其餘七張直接表沿既有 refs，helper 間接表另查。
- [ ] 提供有權限的 Reference Lot／候選、SysBin 動態目錄、GoodBinState、Routing、`IC_FT_PKG` 查詢。參考批 `Status=0|1|4`、`HaveComponent=0`、`HaveContainGoobinState=True`、`LotState=1`、非 Partial／Stocker；候選同 OP／Product／BR／Status／CurUnit、`HaveLevel=1`，`AllowDiffMOMerge=0` 才限同工單。WIP Inventory 宿主另有 `_INV` 查詢與隱藏 Go／Assign，正式規則需依 source/部署核對。
- [ ] 併批 POST 在同一交易鎖定來源 Lot、LotStamp、GoodBinState、SysBin／Bin 餘量、輸出批號、庫房半成品與派工狀態；至少兩批含參考批、CurQty 非零、Action/Assign NodeID 合法。同 OP 不同 NodeID 要明確確認。對每個動態 Bin 的數量、精度與歸屬做 server 重查，合併後 Bin／Pass／Fail 快照寫入 MergeContent；不信任瀏覽器傳來的 Bin、LotSerial、actor、模式或 `AllowDiffMOMerge`。原 Form 未明定每筆 Bin 總量必等 CurQty，應先追 Core helper/真資料再訂正式硬規則。
- [ ] 把 LotBasis／State、Reverse／LotLog、GoodBin、`TBLWIPMERGECONTENT_BIN`、Dispatch／AutoMerge／Cont Employee 與 Inventory 副作用放在一致交易；失敗回滾且防重。BR 宿主 Go 之後另做 TransferLot，若不能同交易，須可查、可重試、冪等；Eletricbox 入口須串真服務及權限。Production API／PostgreSQL／部署驗收由家用端負責；公司端未連 PG、未建正式 API/migration、未執行 build/test/browser。

## WIP WP214 併批處理_載具家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP214 規格](docs/modules/WIP/併批處理_載具.md) 與 `docs/DATABASE_CONNECTION.md` WP214 段落。Vue `web/src/components/WipLotMergeCarrier.vue` 只供 DEV 假資料；正式環境禁用 `ymes-dev-mock:wip-lot-merge-carrier:records:v1`、固定載具／元件 fixture 與本機模擬確認。
- [ ] 核對 YMES PostgreSQL live schema／migration history。Core 直接引用的 11 表均已有 `database/reference/` Oracle source DDL／PostgreSQL CREATE-only；不要重建已存在的表。追 `NewLot_Merge`、`SHR_Merge_Inherit`、Reverse／LotLog／User Define hooks 的間接表與載具類別查找，缺口再做 forward migration。
- [ ] 實作有權限的 Carrier→Lot／Detail／CarrierType 容量、可用空 Carrier、候選 Lot、MO 多選、路由、同 OS 未回貨及 `AllowDiffMOMerge` 讀取。參考 Carrier 有多筆 Lot 且有元件時拒絕；候選同 OP／Product／BR／Status／CurUnit、同 HaveComponent、HaveLevel=0、非 Stocker；`AllowDiffMOMerge=0` 才限同 MONo。Status=5 應按參考 **LotNo** 判定未回貨 OS 且強制 Go。原 Form 的 `funChkBeforeConfirm` 把 `txtRefCarrierNo` 誤傳給 LotNo helper，不能沿用。
- [ ] 交易式 POST 重讀並鎖所有來源 LotStamp／CurQty／CarrierState／CarrierStateDetail／ComponentState、目標 CarrierState／CarrierType、OS／Route／部署參數；確認至少兩批、參考批固定、來源 CarrierNo 真歸屬、目標空載具可用、各 Slot 在容量內且唯一、每顆元件完整且唯一、每批元件數與 CurQty 一致。原 Core 同 OS 守門使用 `!beSameOSNo && outsorucingLotCount != mergeLotCount`，只有同時成立才拒絕；正式 API 要分別拒絕不同 OS 和未全部屬於同一有效 OS。拒絕瀏覽器自報的 HaveComponent、原 Slot、MergeType、UserNo、ShiftNo、時間與總量。
- [ ] 同交易維護 Lot／Reverse／MergeContent／CarrierState／CarrierStateDetail／DetailLog／ComponentState.SlotNo／OSDetail／Dispatch／AutoMerge／Cont Employee／History 等，失敗回滾並防重。BR 宿主 Go 後另做 TransferLot，須回可查、可冪等重試的部分完成結果；Core 結尾另有手動 Transfer 註解，實際宿主分支請核對。正式 API、PostgreSQL migration、部署與驗收由家用端負責；公司端未連 PG、未建正式 API/migration、未執行 build/test/browser。

## WIP WP215 元件分片處理家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP215 規格](docs/modules/WIP/元件分片處理.md) 與 `docs/DATABASE_CONNECTION.md` WP215 段落。Vue `web/src/components/WipComponentSplit.vue` 僅供 DEV 假資料；正式環境不得讀 `ymes-dev-mock:wip-component-split:state:v1` 或以 A–Z fixture 編號正式過帳。
- [ ] 核 YMES PostgreSQL live schema／migration history。公司端 Oracle MESTEST 唯讀取得 `TBLWIPCOMPONENTBASIS` 32 欄、PK ComponentNo 與 `TBLWIPREVERSEHISTORY` 11 欄、PK LotNo+ReverseId 及 LotSerial 非唯一 index；source DDL、index DDL、metadata、PG CREATE-only 在 `database/reference/mestest_wip_wp215_*`。若 live 表不存在才建 forward migration；ComponentBasis 在 Core 為存在才繼承的選用分支。另 12 張直接表已有 refs，helper 的間接表與客製欄位另查。
- [ ] 提供登入保護的可執行 Lot／ComponentState、GoodQty／ScrapQty、LotStamp、CarrierState／Detail 與編號預覽查詢。原 Form Lot 候選 Status=0/1/4/5、HaveComponent=1、LotState=1；有載具拒絕。`CarrierNo` 雖在 Form DataTable/Service，Grid 隱藏且 Core 新元件上載具 SQL 已註解，正式先維持無載具分片。
- [ ] 交易式 POST 鎖定 Lot／來源元件／編號命名空間，重讀 LotStamp、元件歸屬、GoodQty／ScrapQty、載具現況；server 產生或原子保留 2–26 個唯一新元件號，逐列驗證非負／非全零、精度／長度，兩種數量合計各自完全等於來源。原 `EXE_SplitComponent` Core 沒有重新算數量守恆，Service 也只有三個欄位同時缺時才報缺欄；不可只靠 Form。分片成功後來源消失，新元件及 Lot CurQty／LotStamp 更新，重試須冪等。
- [ ] 同交易繼承 ComponentState／Property／選用 Basis、站中 Temp/Cont ComponentAttrib 與 Cont Component、SEMI_WAFER／LED_PKG TestSummary、LotState／Temp_LotState、Carrier DetailLog（若部署允許）及 ReverseHistory CommitState；失敗整體回滾。正式 API、PostgreSQL migration、部署及驗收由家用端執行；公司端未連 PG、未建正式 API/migration、未執行 build/test/browser。

## WIP WP914 生產批緊急狀態／優先權調整家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP914 規格](docs/modules/WIP/生產批緊急狀態與優先權調整.md)。公司頁 `web/src/components/WipHotLotPriority.vue` 只有 DEV 假資料，正式 API 不得讀 `ymes-dev-mock:wip-hotlot-priority:state:v1`。原 FunctionNo 的 `PRIPORITY` 拼字保留用於 menu mapping。
- [ ] 核 YMES PostgreSQL live schema／migration history。Core 直接更新 `TBLWIPLOTBASIS`，WP101 已有 Oracle source DDL／PostgreSQL CREATE-only；查詢 LotState 和 RO／Area／OP／Product lookup 表也有既有 source refs。沒有本功能新表，不重建現存表；若 helper 另有間接表再核對。
- [ ] 做登入授權的六欄候選 Lot 查詢與四類 lookup。固定 Status=0–5、LotState=1，至少一篩選條件；回傳 BaseLotNo、HotLot、Priority、LotNo、Status、18 欄顯示資料及可供衝突檢查的版本。排序 HotLot 降、Priority 升、LotNo 升；限制查詢量／分頁。掃描選取只對目前可見結果生效。
- [ ] 做原子批次 Set API：必須至少一個有效選取、HotLot 0／1、Priority 整數 0–99；server 重讀／鎖定每個 BaseLotNo 及對應 Lot 歸屬、狀態、版本／權限，對同一 BaseLotNo 去重，參數化更新 `TBLWIPLOTBASIS.HotLot/Priority`，明確回報每個基礎批及影響 Lot、審計／衝突／重試結果。原 Form 逐列呼叫 `EditLotBasis_Confirm`，中途失敗可能部分完成；不要讓 browser 或 localStorage 決定已過帳狀態。正式部署與驗收由家用端負責。

## WIP WP501 立即異常設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP501 規格](docs/modules/WIP/立即異常設定.md) 與 `docs/DATABASE_CONNECTION.md` WP501 段。公司端 `/YMES/WIP/ImmediateHoldSet` 是 DEV 假交易，正式環境不可讀 `ymes-dev-mock:wip-immediate-hold:state:v1`、使用本機 ERFNo 或把附件檔名當上傳成功。
- [ ] 核 YMES PostgreSQL live schema、migration history、資料與權限。新增 Oracle source DDL/metadata 的 `TBLWIPERFCREATEDATA`、`TBLWIPERFHOLDREASON`、`BKTBLWIPERFCREATEDATA`，另三張 `TBLWIPERFBASIS`、`TBLWIPERFHOLDREASONDISP`、`TBLWIPERF_ATTACHFILE` 的 source DDL 已在 QC409 reference；六表 CREATE-only 是 `database/reference/mestest_wip_wp501_create.sql`。只對 live 確認缺少的結構做 forward migration，不直接執行整份 CREATE-only；Backup 表無 PK，不要自行補。Lot、QC、OCAP 及 System 既有表先重用，追 Reverse/ReleaseLog/User Define hook 間接依賴。
- [ ] 提供有登入/功能權限的六欄 Lot 候選查詢、前五類 lookup、ERF 主次分類/HoldItem/元件/Area/OCAP/EquipmentState 查詢，限制查詢量或分頁。至少一個 filter；按 `ERFMultipleHold` 選 Status 0/1/4 或 0/1/3/4，固定 CurQty>0、LotState<>0；Partial 非 Queue 排除。Set 前與 POST 時重查包裝、批與 OP/Status/LotStamp，拒絕已 Final 處置及未處理 Dummy 衝突。
- [ ] 做單一原子 Immediate Hold POST：server 保留 ERFNo、取登入使用者，驗證 MClass、HoldDescription、原因非空、Qty 四位精度、原因複合鍵唯一、OCAP 全有/全空、Release EQP 狀態及其他 Lot 使用；按原窗有元件時分派 Component+T1–T5，無元件用 N/A。寫 ERFBasis、兩張 HoldReason、LotState/Log/LoginState、Reverse/ReleaseLog、OCAP 及必要附件關聯，依 CreateERF/既有狀態與系統參數處理多重 Hold。重送/版本衝突應有明確回應；參數化 SQL，不能沿用舊 Core 拼字串。
- [ ] WP501 Release EQP 補充：Form `frmBRERFCreate.vb:1300–1314,4784–4832` 呼叫 `wsWIP.LoadWIPTemp_Equipment`；Core `clsLot.cs:183479–183531` 的 SQL **只回獨佔設備**，0 行也可能是沒有在機設備。Form 0 行顯示「其他 Lot」警告卻**沒有中止 Confirm**，不能沿用為安全守門。正式 POST 要在交易內鎖 `TBLWIPTEMP_EQUIPMENT`／設備狀態，確定本 Lot 有在機設備且無其他 Lot 佔用，才可下機；狀態選單來自 `TBLEQPSTATEBASIS` 的整數 EquipmentState/StateName，值要 server 重查。Core 會呼叫 `SHR_COEquipment`。設備關聯／現況兩表的 Oracle DDL／PG CREATE-only 已在 WP201／設備狀態 refs，狀態主檔另有既存 PG CREATE-only，不要重複建表；詳 WP501 規格。公司 DEV 有共用設備拒絕與獨佔設備成功兩種假資料。
- [ ] 附件實際上傳/BLOB、PDF、郵件/訊息與交易內外的失敗補償或 outbox 一起設計。`TBLWIPERFCREATEDATA` 有資料時，原 Core 會複製到 BK 表再刪除；是否需要此流程由 live 狀態與 API 契約確認。完成 migration/API 後才接正式 Vue、做部署與驗收；公司端未連 PostgreSQL、未執行 build/test/browser。

## WIP WP502 立即等待設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP502 規格](docs/modules/WIP/立即等待設定.md)。公司 `/YMES/WIP/ImmediateWaitSet` 只用 DEV 假資料，正式環境不得讀 `ymes-dev-mock:wip-immediate-wait:state:v1`、以本機 `DEV-WAIT-*` 作 WaitNo 或回報假成功。
- [ ] 核 YMES PostgreSQL live schema/migration history、資料與權限，重用 WP101/WP201 已查的 LotBasis/State/Log/Temp_LotState/WaitBasis/ComponentState 與 USRUserBasis；WaitBasis source DDL 在 `mestest_wip_wp201_additional_source_ddl.sql`，CREATE-only 在 `mestest_wip_wp201_create.sql`。本功能無新直接表，不重建既有表。追包裝、Reverse/ReleaseLog、Adapter 等間接表，確實缺少才做 forward migration。
- [ ] 做授權的七欄候選查詢與六類 lookup：至少一個 filter、Status 0/1/4、CurQty>0、LotState<>0；排除 PartialMode=1 且 PartialType 0/1 且 Status>1，以及 Complete+LinkName WAIT/HOLD。回傳 LotNo+OPNo、LotStamp、顯示欄與選取所需資料，限制結果量/分頁。
- [ ] 批次 Set API 對**每筆**選取 Lot 重查包裝、Status、PartialType、LinkName、LotStamp、CurQty、權限和歸屬；原 Form 把多筆 LotNo 用逗號連接，但 Service 只接單一字面值，不能沿用。server 產各筆 WaitNo、共用第一筆 BatchWaitNo、WaitType=2/WaitSource=0/LotIsValid=1；Description 可空。維護 WaitBasis、LotState/Log、Temp_LotState、Reverse/ReleaseLog 及部署插件。先明定全批原子或相容逐批部分成功的契約、冪等/衝突/逐筆結果，不能把前幾筆成功後一筆失敗顯示成全成功。公司端未連 PG、未執行 build/test/browser；API 完成後才做正式 Vue 整合與驗收。

## WIP WP503 批次立即異常設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP503 規格](docs/modules/WIP/批次立即異常設定.md)。公司 `/YMES/WIP/BatchImmediateHold` 只有 DEV 假資料，正式環境不可讀 `ymes-dev-mock:wip-batch-immediate-hold:state:v1`、使用 `DEV-BERF-*` 或回報假交易成功。
- [ ] 核對 YMES PostgreSQL live schema/migration history、資料與權限；`TBLWIPERFBATCHLOT` Oracle source 已在 `mestest_qc409_wip_disposition_dependencies_source_ddl.sql`，CREATE-only 參考在 `mestest_wip_wp503_create.sql`，六欄、PK `(ERFNO,LOTNO,OPNO)`。僅實際缺項做 forward migration，不重建 WP101/WP201/WP305/WP501/QC 既有 Lot/ERF/設備查詢表。同 Core 的 FutureHoldLog/Reason 僅 `ERFType=3`，**不是 WP503 直接 migration**。
- [ ] 建授權且分頁的十欄＋CheckInTime 候選查詢、七類 lookup、元件／主次分類／HoldItem／EquipmentState 資料。至少一個 filter；依 `ERFMultipleHold` 選 Status `0/1/4` 或 `0/1/3/4`、LotState=1、CurQty>0，排除非 Queue 的 Partial。設備查詢須重建目前 TempEquipment 與 CheckInTime 期間 LotLog_Report/ContEquipment 的 OR 語義，不接受 browser SQL Additional。
- [ ] Batch Hold POST 從 server 讀 `ERFBatchHoldGenType`：`0` 一單多批、`ERFType=6` 並逐批寫 BatchLot；`1` 每批一 ERF、Core 語意 `ERFType=2`，不寫 BatchLot，且 Component 原因依該批元件過濾。選批須同 PartialMode，首批依 LotNo 排序作參考；重查每批 LotNo/OPNo/LotStamp/CurQty/狀態、原因、元件、權限、ERFNo、上機與設備釋放條件。OCAPNo 有值拒絕；附件、PDF、通知、Reverse/ReleaseLog 與部署 adapter 的真副作用要納入失敗設計。
- [ ] 模式 0 整張 ERF 原子提交；模式 1 明定全批原子或相容原 Form 逐批部分成功的契約、冪等鍵、逐筆結果及重試。不得把部分成功當全成功。UserNo、時間、ERFNo、Mode 及狀態由 server 決定，SQL 參數化；正式 API 完成後才接正式 Vue／部署與驗收。公司端未連 PG、未執行 build/test/browser。

## WIP WP504 預先異常設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，讀 [WP504 規格](docs/modules/WIP/預先異常設定.md)。公司 `/YMES/WIP/FutureHoldSet` 只有 DEV 假資料，正式環境不可讀 `ymes-dev-mock:wip-future-hold-set:state:v1` 或回報本機假 Set 成功。
- [ ] 先核 YMES PostgreSQL live schema、migration history、資料和權限。`TBLWIPFUTUREHOLD` Oracle source/CREATE 已有 WP201 refs；新增 `TBLWIPFUTUREHOLDREASON`（Set 直接表）及 `TBLWIPFUTUREHOLDLOG`（後續 ERFType3 才寫）Oracle DDL/metadata/index 在 `mestest_wip_wp504_*`，PG CREATE-only 在 `mestest_wip_wp504_create.sql`。Reason 複合七鍵；Log 無 PK、只有 LotNo 非唯一索引，不能擅自加 unique。確實缺少才 forward migration，不重建 WP201 主表。
- [ ] 建登入/Function ACL 的六欄候選查詢、五種 lookup、PS→有效 OP、QC M/S Class/Hold Description/HoldItem/FirstGroup 的正式讀取 API，限制結果量；Status `0|1|4`、ShowPartialLot=false、至少一個 filter。Selected Lot 是已勾列過濾，Lot/HoldItem 掃描在當前查詢或分類結果勾選。SClass 是 UI 必填與候選依據，原 Service 只收 MClass，不把 SClass 誤當主表欄。
- [ ] Set POST 對每**一筆**所選 Lot 重查包裝、狀態、Partial、版本與權限；原 Form 把多筆 LotNo 逗號串送給單值 `CheckLotPackage`，有漏檢風險。確認選 PS/OP、MClass/SClass/HoldItem 合法、HoldTime 只能 0/1、說明非空。FirstGroup 缺對應 OP 而無 `*` 時提供可取消確認，不能把它當硬拒或忽略警示。
- [ ] 按 Lot×OP 與 `(LotNo,OPNo,HoldTime,ModuleNo,ModuleVersion)` 在同一交易替換 FutureHold/Reason；此入口 ModuleNo/Version=`N/A`，`FutureHoldSerial` default=`N/A`，原刪除鍵**不含 Serial**，須確認不同 Serial 舊資料的覆蓋政策。Set 不立刻改 Lot/開 ERF；後續 Start/End 命中、ERFType3、FutureHoldLog、Reverse/ReleaseLog 與 WP506 Release 要設計一致。交易使用參數化 SQL、冪等鍵、server Creator/時間與並發衝突處理；API 完成後才接正式 Vue／部署/驗收。公司端未連 PG、未執行 build/test/browser。

## WIP WP505 預先等待設定家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，讀 [WP505 規格](docs/modules/WIP/預先等待設定.md)。公司 `/YMES/WIP/FutureWaitSet` 只用 DEV 假資料，正式環境不可讀 `ymes-dev-mock:wip-future-wait-set:state:v1` 或回報假 Set 成功。
- [ ] 核 YMES PostgreSQL live schema/history、資料與權限。`TBLWIPFUTUREWAIT` 七欄、PK `(LOTNO,OPNO,WAITTIME)`，Oracle source `mestest_wip_wp201_dependency_source_ddl.sql`、PG CREATE-only `mestest_wip_wp201_create.sql` 已存在；無新直接表，缺項才 forward migration，別重建 WP201 既有結構。
- [ ] 建有登入/Function ACL 的六欄候選查詢、五類 lookup、PS→有效 OP；至少一個 filter、Status `0|1|4`、ShowPartialLot=false、結果分頁/上限。Selected Lot 是檢視過濾，掃描只勾當前查詢列。
- [ ] Set POST 逐批重查包裝、Status、Partial、版本、Lot/OP 和權限；原 Form 的多筆 LotNo 逗號串被 `CheckLotPackage` 當單值，不能沿用。WaitTime 只允許 Start=0/End=1，OP 至少一個，Description **可空**且最多 3000 字元。以參數化 SQL 交易按 Lot×OP×WaitTime 刪舊再插新 FutureWait，server 決定 Creator/日期、明確處理併發/冪等；Set 不直接改 LotState/WaitBasis。後續 `ChkFutureWait` 命中、WP502 等待交易及 WP507 Release 需生命週期一致。完成 API 才接正式 Vue/部署驗收；公司端未連 PG、未執行 build/test/browser。

## WIP WP506 預先異常解除家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，讀 [WP506 規格](docs/modules/WIP/預先異常解除.md)。公司 `/YMES/WIP/FutureHoldRelease` 為 DEV 假資料，與 WP504 共用 `ymes-dev-mock:wip-future-hold-set:state:v1`；正式環境不可讀此 key 或回報本機假 Release 成功。
- [ ] 核 YMES PostgreSQL live schema/history、資料與權限。主表 `TBLWIPFUTUREHOLD` Oracle source/PG CREATE-only 在 WP201 refs；原因表 `TBLWIPFUTUREHOLDREASON` 在 WP504 refs。本功能無新直接表；確有缺項才 forward migration，不重建既有結構。
- [ ] 提供有登入與 `WP506` Function ACL 的四種可選條件查詢：LotNo、OPNo、HoldTime 0/1、MClassNo；OP/MClass 查找，九欄結果。來源 Form 可無條件查全部，正式端必須限制筆數／分頁、做可見範圍與參數化 SQL，不在瀏覽器一次載入全表。
- [ ] 先決定 Release 的業務範圍：建議以主表六鍵 `(LotNo,ModuleNo,ModuleVersion,OPNo,HoldTime,FutureHoldSerial)` 精確刪單筆及對應原因；若相容來源三鍵 `(LotNo,OPNo,HoldTime)` 批量刪，須明示影響筆數並鎖定/重查**每筆** LockFlag，任何鎖定均拒絕。來源只檢選取列 LockFlag 而 Core 刪同鍵全部，可能誤刪其他鎖定列；不能照搬。
- [ ] 在交易內刪主/原因、處理並發版本/冪等與審計事件，server 取得 actor/時間；已進入 ERF 處置的設定及 FutureHoldLog 必須遵守完整生命週期，不能把此刪除當成即時解除 ERF。不可相信 browser 的 LockFlag 或拼 SQL；API 完成後接正式 Vue/部署驗收。公司端未連 PG、未執行 build/test/browser。

## WIP WP507 預先等待解除家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，讀 [WP507 規格](docs/modules/WIP/預先等待解除.md)。公司 `/YMES/WIP/FutureWaitRelease` 與 WP505 共用 `ymes-dev-mock:wip-future-wait-set:state:v1`，只有 DEV 假資料；正式環境不得讀此 key 或回報假 Release 成功。
- [ ] 核 YMES PostgreSQL live schema/history、資料與權限。`TBLWIPFUTUREWAIT` 七欄、PK `(LOTNO,OPNO,WAITTIME)` 的 Oracle source 在 `mestest_wip_wp201_dependency_source_ddl.sql`、PG CREATE-only 在 `mestest_wip_wp201_create.sql`；無新直接表。只對實際缺項 forward migration，不重建既有表。
- [ ] 提供登入/`WP507` Function ACL 的 LotNo、OPNo、WaitTime 可選查詢和 OP lookup，顯示六欄；原 Form 可無條件查全表，正式 API 要分頁／結果上限、可見範圍和參數化 SQL。
- [ ] Release POST 必須完整提供 LotNo/OPNo/WaitTime，WaitTime 僅 0/1；server 鎖定並重查該 PK、版本與權限，以參數化 SQL 交易刪單筆並記 actor/事件，提供不存在／衝突／冪等回應。舊 Core 允許缺鍵造成廣域刪除，絕不可照搬。Release 只刪未來設定，不解除已生效 WaitBasis/Lot Wait；須與 WP505 Set、`ChkFutureWait`、WP502 真等待交易分清生命週期。API 完成後接正式 Vue/部署驗收；公司端未連 PG、未執行 build/test/browser。

## WIP WP508 異常單資料家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP508 規格](docs/modules/WIP/異常單資料.md)。公司 `/YMES/WIP/ErfBasis` 的三筆 ERF、ERF Info 五頁及 WP801 連動都是 DEV 假資料，正式環境不得使用 fixture/localStorage 或把假 8D 建立當正式成功。
- [ ] 核 YMES PostgreSQL live schema/history、資料、權限。主查詢 ERFBasis/BatchLot 與 Lot/Product/OP/Material/Vendor/Customer 直接表已有 Oracle/PG refs；8D Basis 的 Oracle source/PG CREATE-only 在 `mestest_wip_wp508_*`。ERF Info 新補 `mestest_wip_wp508info_source_ddl.sql`、`_source_indexes.sql`、`_metadata.txt` 和 PG CREATE-only `mestest_wip_wp508info_create.sql`：LotDisposition 16 欄、PK `(ERFNO, LOTDISPORDER)`、`LOTSERIAL` 索引；OCAP 附件 source 沿 QC409，CREATE 在本批，PK `(LOTNO, ERFNO, ATTACHNAME, OCAPNO)`。一般 ERF 附件 CREATE 沿 WP501；NodeBasis 沿 PRS104。確有缺項才 forward migration；WP801 SerialRule 與 WP802 子表見下段。
- [ ] 提供登入/`WP508` Function ACL 的 ERF 查詢：六個可選文字欄、Create/Release 日期區，至少一有效條件，固定 `ERFSource=0`，分頁/上限，返回 25 欄及 batch member Lot/OP/Customer 比對語義。LotNo 是包含查詢；batch ERF 應能因任一成員命中而返回一張主 ERF。StatusShow、ERFRecordQty=ScrapQty、CalHoldTime 等派生欄由 server 計算；不得直接移植 Core 拼字串 SQL。
- [ ] `ERF Info` 依 ERFNo 精確、授權讀主表、HoldReason、HoldReasonDisp、ERFDisposition、LotDisposition、ERF/OCAP 附件，做五頁完整查詢。LotDisposition 要以 NodeID 左聯 NodeBasis 取 OPNo/ProcessNo/ProcessVersion；HoldReason 的 `RECORDQTY=Qty`，有 OCAPNO 時 ERFDisposition 使用 OCAP 欄位分支。公司 DEV 已補假 grid/附件操作，正式端要按 `WP_ERFInformation/frmERFInformation.vb` 接真資料。
- [ ] 附件提供分流的授權串流 View/Download 與 Delete API。Delete 必須確認、檢查獨立刪除權限、從 server 重查 ERFNo/LotNo/來源/OCAPNO 與完整 PK，並禁止刪除檔名不分大小寫包含 `CreateERFNo + "."` 的產生檔；執行前後都檢查。**舊 `wsWIP.DelERFOCAP_AttachFile`／Core 沒傳 OCAPNO，會刪同名多個 OCAP 列；新 API 必須用四鍵精確刪除。** 記 actor/稽核，交易處理競態、已刪/衝突；回應後重載兩種列表。下載輸出安全的 Content-Disposition，不能照搬 WinForms 把附件名稱與本機路徑相接並執行檔案。公司 DEV 的 View 只顯示假訊息、Delete 僅移除記憶體假列，不能當成 API 完成。
- [ ] `8D` 有 QRSNo 時查 8D Basis，無結果或 IssueState=-1 不開維護；沒有 QRSNo 時走 WP801 建立並回寫 ERFBasis.QRSNo。WP801 主單 source/API 契約在 [WP801 規格](docs/modules/WIP/廠內8D矯正單.md)，已有 QRSNo 的 WP802 詳見 [WP802 規格](docs/modules/WIP/8D矯正單維護作業.md)；直接入口不受 WP802 主窗 Active 查詢篩選限制。不要從 browser 接受任意 QRSNo、IssueState；正式 API 後接 Vue/部署驗收。

## WIP WP801 廠內 8D 矯正單家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP801 規格](docs/modules/WIP/廠內8D矯正單.md)。公司 `/YMES/WIP/8DBasisFactory` 與 WP508→WP801 連動使用 `ymes-dev-mock:wip-8d-basis-factory:state:v1`、`ymes-dev-mock:wip-8d-erf-links:v1`；正式環境禁止讀這些 key，DEV Issue Job 下拉僅測兩個畫面分支，正式由 server 的 Issue Job Map 決定。
- [ ] 先核 YMES PostgreSQL live schema/history。8D 主表 `TBLWIP8DBASIS_FACTORY` Oracle source/PG CREATE-only 沿 WP508；ERF Basis 沿 QC409/WP501。新補 `database/reference/mestest_wip_wp801_serial_source_ddl.sql`、`_source_indexes.sql`、`_metadata.txt` 與 `mestest_wip_wp801_serial_create.sql`。既有 `mestest_first_phase_create_tables.sql` 已建 SerialRuleBasis/Detail 欄位及 PK，但少五個 Oracle default：Basis ENABLE=0、SERIALTYPENO='DEFAULT'、DATASTAMP=0，Detail SERIALTYPENO='DEFAULT'、GETDATA=0。只對 live 缺項做 forward migration；`CREATE IF NOT EXISTS` 不會修現有 default。
- [ ] 提供登入/`WP801` Function ACL 的四狀態＋QRSNo 查詢，分頁/上限與 40 欄清單；單筆詳情、查找（Lot/產品/工序/設備/工廠/區域/部門/經理/ERF）由 server 授權，禁止把 Core 的可拼接 SQL 條件直接帶入。
- [ ] ADD/Copy API 在 transaction 內依 `8DNO_FACTORY` 規則保留真 QRSNo、驗必填/欄長/權限與唯一性；來源 ERF 建立時由 server 查 ERFBasis/狀態/相關 log/Lot/設備/工單/原因，鎖定 ERFNo 與未關聯 QRSNo，原子插入 8D Basis 並回寫 ERFBasis.QRSNo。不可接受 browser 假序號或任意 ERF 派生值；失敗需回滾兩表。Copy 要明確定義是否保留 ERFNo（舊 Form 會帶欄，但未必更新 ERFBasis），避免重複關聯。
- [ ] EDIT 只許 IssueState=0，QRSNo 唯讀，依 DataStamp CAS/可編輯欄位更新、記登入 actor/時間；DELETE 0/-1 實體刪並清 ERFBasis.QRSNo，2 需 CurStage=N/A 才轉 -1，1 禁止。每筆重查版本及狀態；多選可設整批原子，回應明確列出成功/失敗，避免 WinForms 逐筆部分成功的困惑。
- [ ] APPROVE 只許 0，ProjectDepartment/ProjectManager/PlanCloseDate 有值且 CurStage=N/A，DataStamp CAS。Issue Job Map **`8D BASIS FACTORY`** 無 Job 或無 Detail 直接轉 2；有 Job/Detail 建 Issue、轉 1，核准 2、退回 0，第一群組通知採 outbox/補償與冪等。實作後接正式 Vue，驗收 ERF/8D 競態、序號重複、刪除與簽核版本衝突。WP802 維護 source/schema 見下段，WP803 結案流程再追；公司端未連 PG、未 build/test/browser。

## WIP WP802 8D 矯正單維護作業家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP802 規格](docs/modules/WIP/8D矯正單維護作業.md)。公司 `/YMES/WIP/8DMaintain` 的主查詢、D1–D9、附件及 WP508 直達使用 `ymes-dev-mock:wip-8d-maintain:state:v1` 與 WP801 假主單 key；正式環境不得讀 localStorage 假資料或回傳假成功。
- [ ] 核 YMES PostgreSQL live schema/history、權限，再對照 `database/reference/mestest_wip_wp802_source_ddl.sql`、`_source_indexes.sql`、`_metadata.txt`、`mestest_wip_wp802_create.sql`。12 張新 8D 子表全部 MESTEST 唯讀查得，無 trigger；只有 TeamMember **unique index、沒有 PK constraint**，PK 名稱不可照名字臆判。ReleaseLog 原 PK 僅 QRSNO。確有缺表/欄/index/default 才 forward migration，CREATE-only 不會修改已存在表。主單沿 WP508。
- [ ] 提供登入/`WP802` Function ACL 的 38 欄主查詢：五個可勾文字欄、四個 QRSClass、兩組日期，固定 IssueState=2/CloseDate null，分頁/上限；明細直接入口另以 QRSNo 重查主單，只有不存在/IssueState=-1 拒絕，不套主窗 Active 篩選。依實際群組/資料範圍驗 read/write ACL。
- [ ] D1–D7 分別提供 TeamMember、Question、Action/ActionDetail/Verify、RootCause/FishBone、Disposition、QA、Prevention/Track 的真 API；表單欄長、非空與複合鍵照 Oracle，序號於主單鎖下配置。D1 團隊存在與登入 actor 是否團隊成員需 server 重查；D5 連 D4、D6 連 D5，Action 父刪連動 Detail，其他引用刪除需明確守門。每筆寫入同一交易推進 CurStage、LastUpdateDate，防止舊 Core 多次獨立命令部分成功；所有 SQL 參數化、稽核 actor，必要時 DataStamp CAS。
- [ ] Basis Confirm 只改專案部門/經理/預計結案日。D8 必須 D1 有筆、D3 有筆且每筆已 Verify；D2 原檢核已註解。`SysInfo.8DReportAutoClose` 由 server 讀取：Y 時總結+ReleaseLog+CloseType=1+CloseDate+D9 原子提交；N 時建 Release IssueState=0、保持 D8，交 WP803 簽核結案。需處理重送冪等、併發及 ReleaseLog 的單鍵限制，不以 DEV checkbox 當真設定。
- [ ] 8D 附件以 `(QRSNO,STAGENO,ATTACHFILENAME)` 完整鍵建授權 Upload/View/Delete API，安全下載、內容掃描與尺寸限制；D9/已結案不可增刪。公司 DEV 僅保存檔名、FishBone 僅概覽、單列刪除；真 BLOB/魚骨圖分類及多列結果由家用端補完。WP803/804 另按後續來源實作；公司端未連 PG、未 build/test/browser。

## WIP WP803 8D 矯正單結案作業家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP803 規格](docs/modules/WIP/8D矯正單結案作業.md)。公司 `/YMES/WIP/8DClose` 與 WP802 8DQuery 唯讀路由皆為 DEV 假資料；要先於 WP802 D8 選非自動結案才有 Release 可查。正式環境禁止讀 localStorage 假資料或把 DEV 模擬簽核當真。
- [ ] 核 YMES PostgreSQL live schema/history。`TBLWIP8D_RELEASE`、`RELEASELOG`、`TEAMMEMBER` 用 `mestest_wip_wp802_*`，Basis 用 `mestest_wip_wp508_*`。`TBLUSRDEPARTMENTBASIS` 新增 MESTEST 唯讀來源 `database/reference/mestest_wip_wp803_department_source_ddl.sql`、`_metadata.txt` 與 PG CREATE-only `mestest_wip_wp803_department_create.sql`。原 first_phase 表已含 14 欄/DepartmentNo PK，但少 **IssueState、ControlDtConect、DtConectQty、DataStamp 的 DEFAULT 0**；只對 live 缺項做 forward migration。ReleaseLog 主鍵只有 QRSNo，TeamMember 只有 unique index、無 PK constraint。
- [ ] 實作登入/`WP803` ACL 的分頁查詢：Release inner join Basis、四狀態、QRSNo 包含搜尋、FactoryNo/ProjectDepartment 精確、Release CreateDate/ReleaseDate 起迄；畫面「CloseDate」實際用 ReleaseDate。GET 詳細與本 QRS D1 TeamMember + `TBLUSRDEPARTMENTBASIS` 部門名 lookup。編輯 PUT 限 Release.IssueState=0 且主單未結案，驗必填、欄長、同部門成員、授權與並發，只更新 Release；不接受 client 提供的狀態/Creator。
- [ ] `8D CLOSE` Issue Job Map 在 server 決定。無 Job/Detail：Release=2、ReleaseLog(ReleaseType=2)、Basis CloseType=2/CloseDate/部門/人員/說明、CurStage=D9 **同一交易**；有 Job/Detail：先 Pending=1，**最終核准後**才原子結案，退回回 0。來源在 Pending 時立即結案、ApproveSQL/RejectSQL 誤更新 Basis.IssueState、WinForms 多選用 ActiveRow 寫 log 且 Core 多命令非原子，均不得直接照搬。處理重送/ReleaseLog 單鍵衝突、簽核與通知冪等、主單 DataStamp CAS、批量結果與稽核。實際結案時刻由 server 產生，勿混同編輯窗 ReleaseDate。
- [ ] 8DQuery 應由 API 按 QRSNo 重查 WP802 十頁資料並唯讀；權限不可因前端 `readOnly` prop 而放行寫入。驗收 D8→申請→直接核准、工作流 Pending/通過/退回、重送、多選錯列、已結案重複核准及同時編輯。公司端未連 PG、未執行 build/test/browser。

## WIP WP804 8D 矯正單查詢家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP804 規格](docs/modules/WIP/8D矯正單查詢.md)。公司 `/YMES/WIP/8DQuery` 為 WP801 本機 8D 假資料與 WP802 唯讀十頁，正式模式要接真 API；現有 WP508 `8D` 建立/維護入口不改成 WP804，來源另有 ERF/SPC 查詢可傳 ERFNo。
- [ ] 主表僅 `TBLWIP8DBASIS_FACTORY`，Oracle source/metadata/PG CREATE-only 沿 `database/reference/mestest_wip_wp508_*`，先核 YMES PostgreSQL live schema/history，無新 WP804 table。提供登入/`WP804` ACL 的分頁 GET：QRSClass 任意 1/2/3/4 集合、QRSNo/QRSSummary 包含、FactoryNo/ProjectDepartment/ProjectManager 精確、Basis CreateDate/PlanCloseDate/CloseDate 區間、CloseState closed/open/all、可選 ERFNo；不固定 IssueState，只讀所有狀態。要求至少一個有效條件與 pageSize 上限，不接受僅「Closed+NotClose」當篩選。來源 QRSType 顯示 0 但未送出查詢；WIPERF/Other 單勾來源漏送條件，正式 API 應照意圖正確過濾。
- [ ] 8DQuery 詳細頁重查 QRSNo 與 WP802 十頁子表/附件，強制 server 端讀取權限，與 WP802 寫入 ACL 分離；驗收封存/已結案/未簽核/作廢查詢、QRSClass 每一單獨勾選、日期尾日、ERFNo 外部入口、無條件拒絕及大結果分頁。公司端未連 PG、未 build/test/browser。

## WIP WP510 等待單資料家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP510 規格](docs/modules/WIP/等待單資料.md)。公司 Vue `/YMES/WIP/WaitBasis` 僅 DEV 唯讀假資料，會合併 WP502 本機 Wait；正式環境要用真 API，不讀 localStorage。
- [ ] 核 YMES PostgreSQL live schema/migration history、資料與權限。主表 `TBLWIPWAITBASIS` 沿 WP201 source/CREATE；Wait Info 左連 `TBLPRSNODEBASIS` 沿 PRS104。新增 `TBLWIPWAITLOTDISPOSITION` 的 Oracle source/index DDL、13 欄 metadata 與 PG CREATE-only 在 `database/reference/mestest_wip_wp510_disposition_*`。源表無 PK/FK/unique/trigger，只有 WAITNO 非唯一 index；不能臆造主鍵。只對確定缺項做 forward migration，不直接執行 CREATE-only。
- [ ] 提供登入/`WP510` ACL 的分頁 waits GET：四文字條件各以勾選且非空才生效，CreateDate/ReleaseDate 可選起訖、至少一有效條件、後端固定 WaitSource=0；LotNo 包含，其餘精確；回傳 WinForms 15 欄及 Status 0/1/2/3/11/12/21/31 顯示。日期與時區/含端點契約、NULL ReleaseDate、分頁上限、參數化 SQL 需明確處理。OP/Customer lookup 要限權/限量。
- [ ] 提供 waits/{waitNo}/dispositions 唯讀 GET：按 WaitNo 讀 `TBLWIPWAITLOTDISPOSITION` 左連 `TBLPRSNODEBASIS`，回傳 LotDispType/Description/Creator/CreateDate/OPNo/ProcessNo/ProcessVersion，CreateDate DESC。核查 WaitNo 可見範圍；源明細無唯一鍵，不可假定單一 WaitNo 僅一列。正式 API 接線、部署與驗收由家用端負責；公司端未執行 build/test/browser。

## WIP WP511 等待處置家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP511 規格](docs/modules/WIP/等待處置.md)。公司 `/YMES/WIP/WaitDisposition` 僅 Vue DEV 假資料，分併批子窗只是單批數量／批號簡化示意，正式不可把 localStorage 處置視為交易成功。先核 YMES PostgreSQL live schema/history；新表 `XX_TBLWAITPRIV` Oracle source/index DDL、14 欄 metadata 與 PG CREATE-only 在 `database/reference/mestest_wip_wp511_priv_*`，僅缺表/欄/default/PK 才 forward migration。WaitBasis 沿 WP201、WaitLotDisposition 沿 WP510、USRUserGroup 沿設備工程群組、LotOPECNRecord 沿 WP207；不要重建已有表。
- [ ] 提供登入/`WP511` ACL 的固定 Status=0、WaitSource=0 分頁查詢與六欄／獨立 CreateDate 起訖篩選；依 source Join WaitBasis/LotState/LotBasis/OPBasis/ProcessBasis，回傳 17 欄 Grid、Wait Description、LotStamp、Partial/ECN/Module/Stocker 守門資料。全表查詢需要安全上限。Customer/OP/Product/MO、目標 PRS Node/Process/Module/Inventory lookup 需依登入權限與選中 Lot 範圍限制，僅回傳可選項。
- [ ] 解除 command 每筆重新從 server 驗 `TBLUSRUSERGROUP` → `XX_TBLWAITPRIV` 的選項權限（Jump Process 01/02 要分開）、Function ACL、Wait Status/Source、LotStamp、PartialMode、ECN、ProcessNo/PSNo、Module/Stocker、目標 route/NodeId、Inventory。不可相信 Vue option/文字/NodeId；不可讓隱藏的 Change ENGNo 或無權限選項從 API 直呼。正式使用參數化 SQL、明確版本衝突與 idempotency key。
- [ ] 移植 `THEIL.wsWIP_Wait.Exe_Wait_LotRelease`／`clsLot_Wait.UpdWait_LotRelease` 的 0/1/2/3/4/6/7/8 分支：WaitLotDisposition insert、WaitBasis Status=21/ReleaseQty/ReleaseDate/Releaser、Lot/Reverse/Log/Inventory/route 一致交易。非 Partial Go 後續 Transfer Lot 或 Product Change；Partial 用 NetFlow 分支，可能產 ReworkLotNo。批次需定逐筆結果或整批原子契約；來源逐筆呼叫可部分成功，不能回單一「全部成功」。驗收權限、多批不同模式/流程、舊 LotStamp、重送、路由/庫存衝突、GO 後續副作用及 rollback。
- [ ] 七個分併批子 Form 按 Bin、Component、Carrier、一般各自 `wsWIP.Exe_Wait_LotSplit_*`／`Exe_Wait_LotMerge_*` 與 Core 逐一移植；只可單 Lot，先驗 Stocker、GoodBin、HaveComponent、CarrierNo，再驗分量、元件/容器、MO/產品/流程與序號分配。公司 DEV 只做簡化對話框，未做這些正式交易；完成後與既有 WP208–WP215 共用可重用的 WIP 拆併批服務。API 接線/PG migration/部署/實測由家用端完成。

## WIP WP509 未解除異常單家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP509 規格](docs/modules/WIP/未解除異常單.md)。公司 `/YMES/WIP/ErfUnreleased` 只有 Vue DEV 假資料，`ymes-dev-mock:wip-erf-unreleased:state:v1` 的動作不能當真交易；WP508 Info 亦尚未同步 WP509 本地假處置。
- [ ] 核 YMES PostgreSQL live schema/history。新增 `XX_TBLQCHOLDPRIV` 的 MESTEST 原表/索引 DDL、18 欄 metadata、PG CREATE-only 位於 `database/reference/mestest_wip_wp509_priv_*`；15 旗標 `DEFAULT 0`、GROUPNO PK/unique、無 trigger。僅在 live 真缺表/欄/default/PK 時寫 forward migration。ERFBasis/BatchLot/Lot、OCAP 與附件沿 QC409/WP501/WP508 references，ERF LotDisposition 沿 WP508 Info，UserGroup 沿既有設備工程群組 reference，先核對再補，不重建。
- [ ] 實作登入/`WP509` Function ACL 的有上限分頁查詢：固定 `ERFSource=0`、`Status IN (0,1,2,3,11)`，六個可勾條件；LotNo contains，Customer/OP/MClass/CurGroup/ERF exact。Lot/OP/Customer 命中任一 `TBLWIPERFBATCHLOT` 成員也返回主 ERF。Join LotState Status 3/31、LotBasis、OP/Product/Customer，回 23 欄及版本/群組/OCAP/路由守門資料；使用參數化 SQL，不接受 client 指定任意 status/source 或拼 SQL。
- [ ] 處置 context API 每次按 server actor 查 `TBLUSRUSERGROUP` → `XX_TBLQCHOLDPRIV`，多群組任一旗標 1 才開選項；Jump Process 分 `_01`/`_02`。同時核 `LotDispTypeLimit`、狀態 0–3/11、PartialMode、OCAP、Stocker、GoodBin/Component、Module/主流程/版本、`CreateERF`，安全地提供 OP/Process/Module/Inventory/Group lookup。原 Abort/ReCreate 只認 CurGroupNo 精確單值，Disp 支援逗號；正式 policy 要決定多群組一致行為並做權限驗收。所有 command 都重驗，不信 UI disabled 或 client 提供的 NodeId。
- [ ] ERF Disposition command 支援 Next Group、Notice Customer、Final Disposition，驗必填 DispDescription/群組/聯絡人、Hold Reason Disp、附件/OCAP state/節點與最後節點。用 `THEIL.wsWIP_ERF.Exe_ERF_Disposition`、`clsLot_ERF` 源碼核寫入，ERF/OCAP/通知以同一 DB transaction 或可靠 outbox 處理；回傳新的狀態/群組/版本及操作紀錄。Waive 原畫面隱藏，勿開放 API 直呼。
- [ ] Lot Disposition command 僅 Status 11，逐一移植 0/1/2/3/4/6/7/8/98/99 與 Batch ERFType6 的不同 Service/Core。Back=11、Split=12、Merge=13、QtyAdjust=14 走獨立分支；非 `ReleaseType` 一律禁止錯送通用解除。以 row lock/DataStamp 或 LotStamp、idempotency key、權限/庫存/路由/OCAP/ERFType 重驗做 ERF Basis/Batch、LotState、ERFLotDisposition、Reverse/Log/Inventory/Transfer/ProductChange 的原子交易。處理外部 Transfer 失敗與重送，回應明確狀態。
- [ ] ERF Abort：OCAP 禁止；ERFType6 用 Batch，否則單筆加 LotStamp，確認後 server 更新 ERF/Lot 關聯並處理 Complete Lot 的 BRTransferLot；不得只改 Status=12。ERF Create 固定 ERFType5，由 server 走 `WP_BRERFCreate` 規則、流水號、Hold/附件/日誌，不能由 browser 複製一列。ERF Info 重用 WP508 正式五頁查詢及附件安全下載。
- [ ] 七種 Split/Merge 子窗依 GoodBinState、Component、Carrier 分支逐一核 WinForms/Service/Core：分量、料件、Bin、Carrier 容量、Lot/ERF 關聯、序號與庫存均需 transaction；QtyAdjust 的 Bin/Comp/standard 三分支與 `Exe_ERF_QtyAdjustLog` 也要保留。公司 DEV 僅批號與數量簡化示意；完成 API 後與 WP508 Info 的 read model 一起驗收正常/Batch/OCAP/Partial、無權限、重送、舊版本和部分成功。家用端負責 PG migration、API、部署及 build/test/browser。

## WIP WP106 使用者登入（上工）資訊家用端 handoff（2026-11-20）

- [ ] Pull 最新 `main`，閱讀 [WP106 規格](docs/modules/WIP/使用者登入資訊.md)。公司 `/YMES/WIP/OperatorOnline` 只用 WP104/WP105 共用 DEV localStorage `ymes-dev-mock:wip-operator-clock-in:state:v1`/`...:log:v1`；正式環境不可從這些 key 讀在線員工或把假 SignOut 當交易成功。
- [ ] 核 YMES PostgreSQL live schema/history。WP106 沒有新直接表，`TBLWIPOPERATORSTATE`/`TBLWIPOPERATORLOG` Oracle DDL/metadata/PG CREATE-only 沿 `database/reference/mestest_wip_wp104_*`，`TBLWIPLOTLOGINSTATE` 沿 WP305，UserBasis/OP/Area/Equipment 沿既有 refs。只補真缺項，無需新增 WP106 CREATE SQL。
- [ ] 提供登入/`WP106` Function ACL 的在線人員分頁/有上限 GET。OP/Area/Equipment/Lot 多值條件是 **LoginPlaceNo 的聯集**，以值陣列參數化，不接受 Form 原始 `AdditionalXml` SQL 字串。無條件查詢須有授權範圍與 pageSize 上限；回七欄、WorkDate/OPNo/版本鍵。OP/Area/Equipment lookup 依權限、現行狀態限量查。
- [ ] SignOut command 檢查員工與地點可見/可操作權限，用 server LogoutDate、UserNo+MultiOperatorMode+LoginPlaceNo 重新查/鎖 State；單筆交易插 OperatorLog、刪 State、更新相關 LotLoginState，保留多人加工 side effect。需冪等、併發保護，逐筆結果表明成功/失敗/未處理；來源 Form 逐筆呼叫可能部分成功。`EnableSignOut=False` 僅是 UI 唯讀提示，不能當 API 權限。家用端接正式 Vue、PG migration/部署並驗收。


本版限定GUI的獨立證據peer `c21d7e7e220a09f64d320cc9eff0309e9be8df0a21088cceece0eecbbcc08d52` 無阻擋；Root接受回條 `69b38e831ac1a469bea728665bee2e97c05414a48b13527ab0fcc4f587cfc172`，保留90raw／83通過／7原false及復驗，非全系統驗收。程式提交 X `aa3be9528880d03f74370b22ac979dff4227570d` 已通過461來源綁定 `032ea499e39664af8558b6d148ed25fe74face9b462a9f7af7e8a77f5d88b555`，正常推送功能分支並直接讀回同ref（`30e5dd7e1b5832e3f3c4fbb08cb5956bcab15a26730af6fc164cc35890dca2cb`）。接續本份純文件status提交與main正常快轉；原checkpoint33保持至新metadata真結案，不預填已完成。


### 遠端驗證檔封存待授權（2026-11-21）

- [ ] HOME：71份版本驗證檔整包上傳在程序建立前遭自動核准審查拒絕；原因是缺這批內部程式／部署／驗證資料與指定私有目的地的明確外傳授權。未連線、未上傳、未改checkpoint；已另送精確授權問題，不等待使用者而繼續其他開發。
- [ ] 原CP33及Ready=false計畫保持，9224／f2c1工具均未遠端執行。若後續網站或API版本已前進，禁止直接執行綁舊497／4f runtime的73e結案器；須以新live版本與既有ET202歷史實證重新審查整合結案，不倒退服務或改寫舊證據。


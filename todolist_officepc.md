# 公司端待辦

## 公司端最新交付：IV302 開立退料單(WIP)（2026-11-24）

- [x] 直接核對 `frmINVMaterialWIPReturn` inline Form／Designer、BasisDef／DetailDef、`wsINV` Basis／Detail／Component Service、`clsInventory`／`clsBasis_UPD`。
- [x] 完成 `/YMES/INV/MaterialWipReturn` DEV 假資料：State=0 Basis／Detail 雙 Grid、查詢、ADD／EDIT／多選 DEL／COPY、五種 FromPlaceType、WIP／MO 來源、來源與退入 Locator、元件展開／選取及 BookingFlag 本機模擬。
- [x] 記錄單頭 Copy 不複製明細、Basis Edit 僅 Description／DataStamp、類型 2／3／5／7 的元件數量守門、類型 3／7 已釋出元件禁止退回，以及 Type 7 必須有元件。IV302 草稿不扣庫存；確認由 IV304 Source=10 負責。
- [x] 新增 [IV302 規格](docs/modules/INV/開立退料單_WIP.md)，接入 Vue DEV 選單／路由，更新 Module Index、MES menu inventory、兩端待辦及公司端交接檔。桌機受保護 Oracle profile 不存在，WIP Return Basis／Detail／Component 與來源元件 DDL／metadata 缺失，未猜 CREATE SQL。
- [ ] 家用端 pull 後比對 `TBLINVMATERIALWIPRETURNBASIS`／`TBLINVMATERIALWIPRETURNDETAIL`／`TBLINVMTLWIPRETURNCOMP`、Raw／Semi／MO Component 的 live schema／migration；沒有可信 DDL 前不建推測 schema。
- [ ] 提供 IV302 ACL 保護且父單號限定的 Basis／Detail API、來源批號／Locator／Component lookup；明細空單號不得回全表，列表穩定排序並分頁。以單一交易重驗 State=0、DataStamp、來源可用 Qty、BookingFlag、MO ReleaseState，原子更新 Detail／元件連結／BookingFlag。
- [ ] 核對 IV304 Source=10 正式退料確認與庫存／元件更新、冪等和交易邊界；IV302 草稿 API 不代替確認。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。下一項依清冊列序為 IV303「退料單記錄(WIP)」。

## 公司端前項交付：IV211 退料單記錄（2026-11-24）

- [x] 直接核對 `frmINVMaterialReturnLog` inline Form／Designer、`wsINV.LoadINVMaterialReturnBasis/Detail` 與 `clsInventory` 對應 Core。
- [x] 完成 `/YMES/INV/MaterialReturnLog` DEV 唯讀頁：8 組勾選條件、庫房／Vendor 假 lookup、State 0–3、CreateDate／ReturnDate 起迄、Query、結果選列載入單號明細及 Component 展開。
- [x] 記錄 Form 開啟不自動查詢、日期值預設近 14 天但核取條件未勾、主 Grid 顯示欄、Core 沒有排序／分頁／上限及 ReturnDate→ConfirmDate 映射。退料日來自 Basis.ReturnDate，不是 TransactionLog CreateDate。
- [x] 發現 Detail Core 的空 MaterialReturnNo 會回傳全表、Component relation 僅以 MaterialNo＋MaterialLotNo 關聯，且只有 `ActiveModule='BASE'` 才載入元件；規格要求正式 API 強制有效單號並限量回應。
- [x] 新增 [IV211 規格](docs/modules/INV/退料單記錄.md)，接入 Vue DEV 選單／路由，更新 Module Index、MES menu inventory、兩端待辦與公司端交接檔。桌機受保護 Oracle profile 不存在；Basis／Detail／Component DDL／metadata 缺失，未猜 CREATE SQL。
- [ ] 家用端 pull 後核 live PostgreSQL schema／migration；提供登入及 IV211 ACL 保護的有上限分頁唯讀 API，參數化 Basis filters，日期迄日含當日末端時間。ReturnDate 直接取 Basis.ReturnDate，不用 TransactionLog 代替。
- [ ] Detail API 必須要求有效 MaterialReturnNo，空單號不得回全表；回傳 Form 可見欄並確認產品是否要提供 BASE Component 子列。核實真實 component key 後再定關聯，勿照搬舊 relation 只用 MaterialNo＋LotNo。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。下一項依清冊列序為 IV302「開立退料單(WIP)」。

## 公司端最新交付：IV206 開立退料單（2026-11-24）

- [x] 直接核對 `frmINVMaterialReturn`／BasisDef／DetailDef inline Form／Designer、`wsINV` Basis／Detail／Component Service、`clsInventory`／`clsBasis_UPD` 對應 Core。
- [x] 完成 `/YMES/INV/MaterialReturn` DEV 假資料：單號查詢、State=0 雙 Grid、Basis／Detail ADD／EDIT／多選 DEL／COPY、Vendor／Inventory／Raw Lot／Locator lookup、元件選取及 BookingFlag 本機模擬。
- [x] 記錄 Basis Edit 僅 Description、Copy 不複製 Detail、`MATERIALRETURNNO` serial、Detail Qty>0／元件數量守門、元件候選只允許未預約等來源規則。IV206 草稿不扣庫存；正式退料確認在 IV201 Source=3。
- [x] 明列 Service 分開寫 Detail／Component／BookingFlag 的部分成功風險、Core 元件 Edit 以 MaterialReturnNo 刪整單資料及 Raw Component lookup 漏 InventoryNo 的範圍問題；正式 API handoff 要用完整明細鍵、補庫房範圍並以交易原子更新。
- [x] 新增 [IV206 規格](docs/modules/INV/開立退料單.md)，接入 Vue DEV 選單／路由，更新 Module Index、MES menu inventory、兩端待辦與公司端交接檔。桌機受保護 Oracle profile 不存在；Return Basis／Detail／Component、Raw Component／Locator DDL／metadata 缺失，未猜 CREATE SQL。
- [ ] 家用端 pull 後核對 PostgreSQL live schema／migration；在可信來源結構到位前不猜 Return 表欄位／PK／index。提供 IV206 ACL 保護的單頭／明細／元件 API，空單號不回傳全表，父單 State、DataStamp、元件 BookingFlag 與單一交易需在伺服器重驗。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。下一項依清冊列序為 IV211「退料單記錄」。

## 公司端最新交付：IV210 預訂單記錄（2026-11-24）

- [x] 核對 `frmINVPreOrderLog` inline Form／Designer、`wsINV.LoadINVPreOrderBasis/Detail` 與 `clsInventory` 對應 Core。
- [x] 完成 `/YMES/INV/PreOrderLog` DEV 唯讀查詢：PreOrderNo／InventoryNo／State／CreateDate／Source=4 INV Date、查找視窗、查詢結果選列載入明細。
- [x] 記錄 Basis／Detail 顯示欄、State 0–4、日期界限按日含迄日、INV Date 依 TransactionLog `Source=4` TicketNo／CreateDate 而非 ConfirmDate，以及 Core 無穩定排序／分頁。
- [x] 記錄舊 Form 無 Basis 結果／群組列時會用空 PreOrderNo 載入 Detail，Core 空單號可能回傳全表；DEV 安全清空，正式 API 必須要求有效 PreOrderNo。
- [x] 新增 [IV210 規格](docs/modules/INV/預訂單記錄.md)，接入 DEV fallback 選單與路由，更新 Module Index、MES menu inventory、兩端待辦與交接 checkpoint。Basis／Detail DDL／metadata 缺失；TransactionLog 沿用 WP605 reference，未猜 CREATE SQL。
- [ ] 家用端核 live schema 並提供 IV210 ACL 保護唯讀 API；參數化查詢、Source=4 日期篩選、穩定排序／分頁及單號限定 Detail endpoint。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。下一項依清冊列序為 IV206「開立退料單」。

## 前項交付：IV205 開立預訂單（2026-11-24）

- [x] 核對 `frmINVPreOrder`／BasisDef／DetailDef inline Form／Designer、`wsINV` 與 `clsInventory`、`clsBasis_UPD`。
- [x] 完成 `/YMES/INV/PreOrder` DEV State=0 主／明細 CRUD、PreOrderNo 搜尋、PREORDERNO 假取號、原物料庫房及 CurQty>0 原料 lookup、DataStamp 本機更新；單頭 COPY 不複製明細。
- [x] 記錄主／明細欄位、State=0 寫入界線、Description-only basis edit、物料 UnitNo lookup、Qty>0、DataStamp／Reviser／ReviseDate、單頭刪除 cascade 及 IV201 預訂確認邊界。草稿建立不扣庫存或改 `AllocateQty`。
- [x] 新增 [IV205 規格](docs/modules/INV/開立預訂單.md)，接入 DEV fallback 選單與路由，更新 Module Index、MES menu inventory、兩端待辦與交接 checkpoint。PreOrder Basis／Detail DDL／metadata 缺失；TransactionLog 沿用 WP605 reference，未猜 CREATE SQL。
- [ ] 請公司端以受保護 MESTEST Oracle profile 唯讀補交 `TBLINVPREORDERBASIS`／`TBLINVPREORDERDETAIL` 與 IV203／IV208 共用的 `TBLINVMATERIALOUTBASIS`／`TBLINVMATERIALOUTDETAIL`／`TBLINVMATERIALOUTCOMPONENT`、`TBLINVRAWINVENTORY`／`TBLINVRAWINVENTORYLOC` 原生 DDL 和欄位／PK／index／FK／default／trigger metadata，並提供對應 PostgreSQL CREATE-only reference。家用端 live audit 已確認多張表缺失；沒有可信結構前無法安全建立正式 API／migration。只查 schema metadata，不讀業務列、不執行 Oracle DDL/DML。
- [ ] 家用端先核 live schema，實作 IV205 ACL 保護的 State=0 主／明細 API、伺服器 serial reservation、DataStamp CAS、父單狀態守門及單頭／明細交易。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。IV210 已交付，目前下一項依清冊為 IV206「開立退料單」。

## 前項交付：IV208 領料單記錄（2026-11-24）

- [x] 核對 `frmINVMaterialOutLog.vb` inline Form／Designer、`wsINV.LoadINVMaterialOutBasis/Detail` 與 `clsInventory` 對應 Core。
- [x] 完成 `/YMES/INV/MaterialOutLog` DEV 唯讀查詢：MaterialOutNo／InventoryNo／PreOrderNo／State、建立日與 Source=1 出庫交易日範圍、主檔查找、主檔選列載入明細與元件子列。
- [x] 規格記錄 Main／Detail／Component 顯示欄位、日期條件、State 0–3、Core 無排序／分頁，以及 MaterialOut Date 依 TransactionLog `Source=1` TicketNo／CreateDate 而非 Basis ConfirmDate。
- [x] 記錄舊 Form 在查無主檔時以空單號呼叫 Detail；Core 空單號不加限制可能讀回全表。DEV 不載全表，正式 Detail API 必須要求有效 MaterialOutNo。
- [x] 新增 [IV208 規格](docs/modules/INV/領料單記錄.md)，接入 DEV fallback 選單與路由，更新 Module Index、MES menu inventory、兩端待辦及交接 checkpoint。MaterialOut Basis／Detail／Component DDL／metadata 缺失；TransactionLog 沿用 WP605 reference，未猜 CREATE SQL。
- [ ] 家用端先核對 live schema；提供 IV208 ACL 保護的參數化、穩定排序／分頁唯讀查詢，Source=1 出庫日期條件和必填 MaterialOutNo 明細 API。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。IV205、IV210 已交付，目前下一項依清冊為 IV206。

## 前項交付：IV203 開立領料單（2026-11-24）

- [x] 核對 `frmINVMaterialOut`／BasisDef／DetailDef inline Form／Designer、`wsINV` Basis／Detail／Component Service 與 `clsInventory` 對應 Core。
- [x] 完成 `/YMES/INV/MaterialOut` DEV 假資料：State=0 單頭／明細 ADD／EDIT／多選 DEL／COPY、MaterialOutNo 查詢、核准預訂單、庫房批號、五種領料去向、MO 物料對應、元件勾選和 `arMaterialOut` 預覽。
- [x] 記錄 `MATERIALOUTNO` serial rule、header copy 不複製明細、DataStamp、Detail Qty>0／目標驗證、Component 數量與明細 Qty 相等及 `BookingFlag` 假預約。
- [x] 明確區分 IV203 草稿 CRUD 與 IV201 Source=1 確認扣帳；建立明細不檢查當下可領量，正式確認才跑 `ChkMaterialOutQty` 與單位／預訂量校驗。
- [x] 新增 [IV203 規格](docs/modules/INV/開立領料單.md)，更新 Module Index、MES menu inventory、兩端待辦及交接 checkpoint。MaterialOut Basis／Detail／Component、RAW Component、PreOrder Basis／Detail Oracle DDL／metadata 缺失；共用 Transaction State／Log 沿用 WP605 refs，未猜 CREATE SQL。
- [ ] 家用端先核 live schema／migration，再實作 IV203 ACL 保護的草稿與明細 API、伺服器 serial、DataStamp、預訂與 component booking 交易；缺可信 DDL 前不猜 CREATE。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。IV208、IV205、IV210 均已交付，目前下一項依清冊為 IV206。

## 前項交付：IV207 進料單記錄（2026-11-24）

- [x] 直接核對 `frmINVMaterialInLog.vb` inline Form／Designer、`wsINV.LoadINVMaterialInBasis`／`LoadINVMaterialInDetail` 與 `clsInventory` 對應 Core。
- [x] 完成 `/YMES/INV/MaterialInLog` 唯讀 DEV 頁：單號／庫房／Vendor／State／建立日／入庫交易日條件、查找對話框、按 Query 顯示主檔、選取主檔載入明細。
- [x] 記錄主 Grid 和 Detail Grid 顯示欄、State 0–3、INV Date 依 `TBLINVTRANSACTIONLOG.Source=0` 的 TicketNo/CreateDate 篩選、date-only 迄日延伸至日終，以及無分頁／排序的 Core 行為。
- [x] 發現並記錄舊 Form 查無主檔／群組列時 `LoadDetail("")` 可能使 Core 回傳全表明細；正式 API 與 DEV 均限制單號。
- [x] 新增 [IV207 規格](docs/modules/INV/進料單記錄.md)，更新 Module Index、MES menu inventory、兩端待辦與交接 checkpoint。`TBLINVMATERIALINBASIS`／`TBLINVMATERIALINDETAIL` Oracle DDL／metadata 缺失；`TBLINVTRANSACTIONLOG` 沿用 WP605 source DDL／CREATE-only。
- [ ] 家用端先比對 live schema；主／明細沒有可信 DDL 前不猜 CREATE。實作唯讀主檔篩選 API 和必填 MaterialInNo 的明細 API，加入 IV207 ACL、參數化日期／State 條件、穩定排序與分頁上限。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。IV208、IV205、IV210 均已交付，目前下一項依清冊為 IV206。

## 前項交付：IV202 開立進料單（2026-11-24）

- [x] 核對 `frmINVMaterialIn`／BasisDef／DetailDef inline Designer、`wsINV` Load/Add/Edit/Del 與 `clsInventory` Basis／Detail Core。
- [x] 完成 `/YMES/INV/MaterialIn` DEV 雙 Grid：單頭與明細 ADD／EDIT／COPY／DEL、單號篩選、State=0 假資料、供應商物料／Locator 假選項、QC 假連動、列印預覽與驗證。
- [x] 記錄主畫面只載入 State=0、單號與明細排序、Description-only header edit、單頭 Copy 不複製明細、`MATERIALINNO` 取號、CheckLotNo／ExpireDateFlag／日期／Locator Capacity、`MaterialInGenerateQCForm` 及待檢 QC 狀態守門。
- [x] 新增 [IV202 規格](docs/modules/INV/開立進料單.md)，更新 Module Index、MES menu inventory、兩端待辦與交接 checkpoint。
- [ ] 家用端先比 live schema/migrations：`TBLINVMATERIALINBASIS`、`TBLINVMATERIALINDETAIL`、`TBLSPCINCOMINGQCFORM` 缺同版 Oracle DDL／metadata；Oracle profile 不存在，未猜 CREATE SQL。QC Item Map 沿用 QC216 reference。確認 Core Basis delete SQL 的 QC 清理次序，正式 API 應在交易中先保存關聯再按依賴刪除。
- [x] 公司端未連 YMES PostgreSQL、未設定 `ConnectionStrings__YMes`、未建 schema/migration、未實作正式 API、未部署、未執行 build/test/browser。後續依選單清冊接續 IV207，已於上方交付。

## 前項交付：IV201 原物料倉庫房資料（2026-11-24）

## IV201 原物料倉庫房資料（2026-11-24）

- [x] 直接核對 `frmINVRawInventory` inline Designer、六個確認視窗、`wsINV.LoadRawInventory`／`LoadRawInventoryLoc`／`AddINVTransactionState`／`ExeINVTransactionState` 與 `clsInventory` 查詢／交易方法。
- [x] 完成 `/YMES/INV/RawInventory` DEV：有效原料庫房選擇、庫存主 Grid、選列連動 Locator/Lot Grid、數量摘要、Refresh／Close、Material In/Out/Adjust、PreOrder、Material Return、WIP Return 單據確認入口與 localStorage 假交易。
- [x] 記錄六個 Source code 0/1/3/4/2/10、`CurQty > 0` 主查詢、Locator 顯示欄及 `PreOrder` 只增加 `AllocateQty`；Material Return 扣庫存，WIP Return 回補原料庫；多明細、單位換算、投入點／WIP 副作用及產業包分支明確列為 DEV 簡化。
- [x] 新增 [IV201 規格](docs/modules/INV/原物料倉庫房資料.md)，更新 Module Index、MES menu inventory 和家用端 handoff。
- [ ] 家用端回報：YMES PostgreSQL `YMES` schema 缺 `tblinvrawinventory`、`tblinvrawinventoryloc`、`tblinvtransactionstate`、`tblinvtransactionlog`；已存在 `tblmtlmaterialbasis`（110 欄）、`tblinvinventorybasis`（14 欄）、`tblinvlocatorbasis`（12 欄）。請用公司端受保護 MESTEST profile 唯讀匯出 `TBLINVRAWINVENTORY`、`TBLINVRAWINVENTORYLOC` 的 Oracle DDL 與欄位／PK／index／FK／trigger/default metadata，並逐一整理六種 IV201 Confirm Core 的直接相依表；對 repo 缺少可信來源的表補 source DDL、metadata 與 PostgreSQL CREATE-only。禁止查業務列、執行 Oracle DDL/DML 或猜測欄位。共用 Transaction State／Log 已有 WP605 refs，不需重複查 Oracle。
- [x] 公司端未連 YMES PostgreSQL、未實作／驗收正式 API/schema、未設定 `ConnectionStrings__YMes`、未部署、未執行 build/test/browser。IV207、IV203、IV208、IV205、IV210 均已交付，目前下一項為 IV206。

## 公司端最新交付：SC501 品管項目檢測儀器對應（2026-11-24）

- [x] 核對 `frmInspMachineParameterSetup`／`frmInspMachineParameterTest`、inline Designer、`wsQC.LoadQCItemForVariables`／`LoadInspMachineParameter`／`LoadQCItemGroupDetail` 與 `clsQCBasis`／`clsInspMachine`。
- [x] 完成 `/YMES/SPC/InspMachineParameterSetup` DEV、SPC 檢測設備選單：本機設定 Grid 分組、QC Item／Group／儀器 picker、多列新增、Edit／Copy／多選刪除、RS232 參數帶入、Channel 守門、Enable 排他、UnitConversion、XML Import／Export、Load／Confirm／Close 與模擬測試。
- [x] 確認 SC501 的 Confirm 寫 `%LocalAppData%\iMESTech\InspMachineSetup.xml`，沒有儀器對應表或 Add/Edit/Delete Service；不可將本機設定誤當 QC502 儀器主檔 CRUD。瀏覽器 DEV 用 localStorage／XML 模擬，無真實 COM Port。
- [x] Lookup 來源是計量 QC Item (`CollectType=1`, 預設 `IssueState=2`)、儀器主檔 (預設 `IssueState=2`) 與 ShowOrder 排序的 QC Item Group Detail；沿用 repo QC205／QC501 references，未新增表／CREATE SQL，也未連 Oracle 或 YMES PostgreSQL。
- [x] 新增 [SC501 規格](docs/modules/SPC/品管項目檢測儀器對應.md)，更新 Module Index、Inventory 與家用端 handoff。SC 清冊目前無其他未完成 FunctionKey；依全清冊下一模組移至 INV 盤點。

### 家用端 handoff

- [ ] Pull 後閱讀 SC501 規格；正式頁沿用受登入／Function ACL 保護的 QC205 Variable QC Item、QC502 儀器主檔與 QC Item Group Detail 唯讀 lookup，保留其狀態／排序語意。
- [ ] 決定 Web 頁是否維持瀏覽器下載／匯入的操作站設定檔。舊功能只寫本機 XML；如需跨使用者同步，先另定設備／工作站範圍、版本及本機設備代理方案，再設計 server schema/API；不可直接寫入 QC502 儀器主檔。
- [ ] 真實 Serial Port I/O 不在一般瀏覽器 DEV 範圍；若產品需 Web 實際讀值，另設受控本機 agent／設備服務及錯誤／重送契約。

## 公司端最新交付：SC402 SPC 異常單查詢（2026-11-24）

- [x] 核對標準 `frmSPC_ERFBasis` inline Designer、`wsWIP.LoadERFBasisJoinLotBasis`／`kcWIP.clsLot.LoadERFBasisJoinLotBasis`、SPC lookup 與 8D `Load8DBasisFactory` 呼叫。
- [x] 完成 `/YMES/SPC/ErfBasis` DEV、SPC 選單及 SC402 路由；6 個條件至少一項、ERFSource 1/2、原始 22 欄 Grid、ERF Info、8D 新增／維護入口及作廢 8D 守門。
- [x] 記錄 SC402 與 SC401 的差異：SC402 不限制 Status、無日期條件，需至少一個有效篩選；8D QRSNo 使用 WP801／WP802，LotNo 批次成員 contains 查詢；Core SQL 無分頁／上限且字串串接。
- [x] 查詢表與 8D 已沿用 QC409、WP101、PRD、OP、MTL、Customer、WP801／WP802 references，無新增表或 CREATE SQL；公司端未連 Oracle 業務資料或 YMES PostgreSQL。
- [ ] 家用端比對 live schema／migration；建立 `SPC ERF QUERY` ACL 保護的限量分頁 API，固定 ERFSource 1/2，要求六條件至少一項、參數化精確代碼／Lot contains 與批次成員語意；ERF Info 沿用 WP508 API，8D 寫入重用 WP801／WP802 原子關聯。
- [x] 新增 [SC402 規格](docs/modules/SPC/SPC異常單查詢.md)，更新 Module Index、Inventory 和家用端 handoff。下一個未完成 SPC FunctionKey 為 SC501；公司端不連 PostgreSQL、不實作正式 API/schema。

## 公司端最新交付：SC401 未解除異常單（SPC）（2026-11-24）

- [x] 核對標準 `frmSPC_ERFUnReleased`／inline Designer、`frmSPC_ERFDisposition`、`wsWIP.LoadERFBasisJoinLotBasis`／處置服務及 `kcWIP.clsLot`／`kcSPC` lookup。
- [x] 完成 `/YMES/SPC/ErfUnreleased` DEV，SPC 選單、獨立 localStorage、SPC／收料 ERFSource 1/2、九組查詢、原生 Grid 欄位、附件入口、ERF Disp／Info；批次處置假資料與 WP509 分開。
- [x] 記錄 SC401 與 WP509 的不同：標準 Lot Disposition 只有 Back／Go／Go Scrap Inventory；ERF Abort 按鈕隱藏且 handler 的 Service 呼叫註解；Form 送 Material/Vendor，但 Service 漏解析。DEV 依畫面意圖模擬 Material/Vendor 篩選。
- [x] 查詢表沿用 QC409、WP101、PRD、OP、MTL、Customer references，無新增主查詢表。為共用 `TBLWIPERFDISPOSITION` 增加 PostgreSQL CREATE-only 參考；其 Oracle source DDL 已存在。桌機未查 Oracle 業務資料或連 YMES PostgreSQL。
- [ ] 家用端比對 live schema／migration；缺表時才參照 `database/reference/mestest_wip_erf_disposition_create.sql` 建 forward migration。實作 SC401 Function ACL、限量分頁、ERFSource 1/2 查詢、Material/Vendor 參數化篩選、OCAP/group guard、附件及單一交易 ERF/Lot disposition API，沿用 WP508／WP509 handoff。
- [x] 新增 [SC401 規格](docs/modules/SPC/未解除異常單.md)，更新 Module Index、Inventory 與家用端 handoff；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作正式 API/schema。

## 公司端最新交付：SC312 管制圖月報表_以要因查詢（2026-11-24）

- [x] 核對 `frmQueryMnthVarChartRPTByFactor`／inline Designer、`frmRPTChartMnth`、`wsSPC.LoadSPCChartMonthReportByFactor` 與 `clsSPC` 同名 Core。
- [x] 完成 `/YMES/SPC/MonthChartReportByFactor` DEV：全部 Variable 的 Factor lookup、多值選取／自由輸入、日期、Key_Node、Get Last Month、Clear／Query、月報結果 Grid。
- [x] 記錄 QC Item、Report Code、QCDataType 與 Combine/Main 群組隱藏；一般查詢不傳 QCItemNo／QCDataType；記錄三期區間、SPCID=0 limit、Run3 空列剔除及 R1/R2 警示。
- [x] 沿用 QC302／QC205／QC210 references，無新增表；Factor SQL identifier/value 字串拼接、查詢無上限且 Service timeout 6,000,000ms。桌機 Oracle profile 不存在，本輪未連 Oracle／YMES PostgreSQL。
- [ ] 家用端核對 live schema／migrations，建立登入及 SC312 權限保護的 Factor 查詢 API；Factor allowlist、參數化條件、限制日期／Factor／RawData／計算時間，明確決定全體 IssueState=2 控制圖的授權範圍。
- [ ] 保留或記錄舊三期、GetLastMonth、`Excluded=0` 與 R1/R2 警示語意；隱藏 Report Code／Combine 流程若要重開，先與 SC302 核對 schema/API 與 SampleSize。
- [x] 新增 [SC312 規格](docs/modules/SPC/管制圖月報表_以要因查詢.md)，更新 Module Index、Inventory 與家用端 handoff；下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC311 製程能力報表_以要因查詢（2026-11-24）

- [x] 核對 `frmQueryChartAllCapabilityByFactor`／inline Designer、ALL／YMW result forms、`wsSPC.LoadChartCapabilityDataByFactor`／`LoadChartYMWCapabilityData` 與 `kcSPC` 同名 Core。
- [x] 完成 `/YMES/SPC/ChartAllCapabilityByFactor` DEV：隱藏 QC Item picker 的全部 Variable 要因清單、MES Parameter／自由輸入、ALL／週／月／年、日期、Lot State／Result、CalRule、八項能力指標、Clear／Query、動態結果 Grid 與選列 Chart。
- [x] 記錄至少一個 FactorValue 才可查詢；舊 YMW caller 未傳 `blnByFactor`，Factor rows 也未填 QCItemNo，可能影響服務查詢及結果 Factor 顯示。
- [x] 沿用 QC302／QC205／QC210 references，無新增業務表；ALL／YMW RawData 均以 `Excluded=0` 起始，Factor identifier/value 字串拼接且無上限，Service timeout 6,000,000ms。桌機 Oracle profile 不存在，本輪未連 Oracle／YMES PostgreSQL。
- [ ] 家用端核對 live schema／migrations；設計不需 QCItemNo 的 SC311 查詢 API、伺服器端 Factor allowlist、參數化條件、日期／Factor／列數／計算時間限制，並統一摘要與 RawData 的 LotState／Result 篩選。
- [x] 新增 [SC311 規格](docs/modules/SPC/製程能力報表以要因查詢.md)，更新 Module Index、Inventory 及家用端 handoff；下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC303 管制圖月報表（2026-11-23）

- [x] 核對 `frmQueryMnthVarChartRPT`／`frmRPTChartMnth` inline Designer、`wsSPC.LoadSPCChartMonthReport/ChkSampleSizeforMonthReport`、`clsSPC` 同名 Core 及 QC／SYS／SC302 lookup。
- [x] 完成 `/YMES/SPC/MonthChartReport` DEV：Report Code 與 QC Item picker、Combine／Main、From／End、Key Node、Factor 多值、Get Last Month、三期月報 Grid；新增 [SC303 規格](docs/modules/SPC/管制圖月報表.md)，更新 Index、Inventory 和家用端 handoff。
- [x] 記錄 Code picker 只帶入 QC Item／Main／Factor；一般 Service 不收 ReportNo。Combine 先檢 SampleSize，Main QC Item 決定 Chart Basis／SampleSize，Core 再彙整多 QCI 的 RawData；Get Last Month 預設 true，關閉後只算本期。
- [x] 直接表沿用 QC302 Chart Basis／Detail Log、QC205 Variable Factor／Factor Log、QC210 Variable Detail Log references；無新增表。SC302 `TBLSPCMONTHRPTCODE` 缺 DDL／metadata 已另列 handoff；桌機 Oracle profile 不可用，本輪未連 Oracle／YMES PostgreSQL。
- [x] 標記 Core 的 Excluded=0、SPCID=0 limit lookup、Run3 無資料時剔除、dynamic Factor SQL／無列數限制／6,000,000ms timeout，以及 R1/R2 欄位未填等行為。
- [ ] 家用端 pull 後核對 live migrations，與 SC302 合併確認 Code API；正式查詢須登入／SC303 權限、參數化條件、Factor metadata allowlist、限制日期／資料列／計算時間，明確保留或記錄舊統計差異。
- [x] SC303 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC302 管制圖月報表編碼（2026-11-23）

- [x] 核對 `frmMonthRPTCodeBasis`／`frmMonthRPTCodeBasisDef` inline Designer、`wsSPC.LoadMonthRPTCode/AddMonthRPTCode/DelMonthRPTCodeBasis`、`clsSPC` 同名 Core，以及 QC Item／Factor／MES Parameter lookups。
- [x] 完成 `/YMES/SPC/MonthRPTCodeBasis` DEV：ReportNo 清單、ADD／EDIT／Copy／多選整組刪除、QC Item 多選與 Main、Factor 值設定及明細；新增 [SC302 規格](docs/modules/SPC/管制圖月報表編碼.md)，更新 Module Index、Function Inventory 與家用端 handoff。
- [x] 記錄 Core 以 ReportNo 建 QC Item／Factor DataRelation；Factor 子表沒有 QCItemNo；新增為 QC Item × Factor 列寫入，空 Factor 寫 `NA`；編輯 Service 未實作，舊 Form 刪除再新增且非原子。
- [x] `TBLSPCMONTHRPTCODE` 在 repo references 找不到 Oracle DDL／metadata／PG CREATE-only；桌機受保護 Oracle profile 不可用，本次未連 Oracle／YMES PostgreSQL，沒有猜 schema。
- [ ] 家用端 pull 後核對 live schema／migrations；依可信 Oracle metadata 補缺 schema，參數化並以交易實作 aggregate 新增／替換／刪除，驗證 SC303 消費的多 QC Item、Main 與 Factor mapping。
- [x] SC302 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC301 製程能力報表（2026-11-23）

- [x] 核對主 Form／inline Designer、ALL 詳細報表、YMW 報表與 Chart、`wsSPC` 兩個 Service／Core methods、QC Item／Factor／MES Parameter lookup。
- [x] 完成 `/YMES/SPC/ChartAllCapability` DEV：多選 Issue Variable QC Item、動態 Factor 條件、週／月／年／ALL、Lot State／Result、CalRule、CP／CPK／PP／PPK／CA／Mean／EstSd／Sd、動態週期 Grid 及選列趨勢視窗；新增 [SC301 規格](docs/modules/SPC/製程能力報表.md)，更新 Index、Inventory 與兩端 TODO。
- [x] 記錄 ALL 與週／月／年查詢分支、預設 MONTH／兩個月日期值、批次／結果／因子篩選、period boundary 和點選 Chart 流程。DEV 指標使用固定假資料，不冒充 SPC 統計重算。
- [x] QC Item／Factor、QC302 Chart Basis／Detail、Variable Factor／Detail log 使用 repo 既有 DDL／CREATE-only references，SC301 無新增表。桌機 Oracle profile 不存在，未直連 Oracle／YMES PostgreSQL、未猜 DDL。
- [x] 舊 Core RawData 查詢固定 `Excluded=0`，與 Lot State 的 Excluded／Isolated 選項可能不一致；動態 Factor SQL 字串拼接且無上限，Service timeout 6,000,000ms，已列入 handoff。
- [ ] 家用端核對 live schema／migration、Factor metadata 及完整統計公式；所有值參數化並限制 QC Item／Factor／日期／週期／輸出列數，FactorNo 使用授權 allowlist。
- [ ] 家用端統一 Lot State／Result 對計數與指標的定義，核對 CalRule≥25、週界和部分月份／年度區間；若修正 `Excluded=0` 差異，記錄相對舊程式的行為差異。下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC216 品管項目綁定紀錄表（2026-11-23）

- [x] 核對 `frmQcItemBindingHisLog`／Designer、`wsSPC.LoadQCItemLogData`、`kcSPC.LoadQCItemLogData` 與 `clsQCBasis.UpdQCItemBindLog`／`AddQCItemBindLog` 寫入流程。
- [x] 完成 `/YMES/SPC/QcItemBindingHisLog` DEV：五種 QC Item Type 與 Issue lookup、可選 Type／ExecuteStatus、預設最近 7 天 inclusive 日期、Product／OP／Material Type／MaterialNo 四個父子歷程頁籤；新增 [SC216 規格](docs/modules/SPC/品管項目綁定紀錄表.md)，更新 Index、Inventory 與兩端 TODO。
- [x] 寫入來源以 ExecuteStatus 0／1／2 區分新增綁定／更新綁定／解除綁定；查詢以 DataRelation 回傳父子表，明細由 Form 排序。Core relation keys 未包含 ProductNo／ProductVersion，需避免多產品時重複掛接。
- [x] `TBLQCITEMBINDLOG` Oracle source DDL／metadata 與 PostgreSQL CREATE-only 已在 MTL QC Item table references；無新增表。桌機受保護 Oracle profile 不存在，本次使用 repo reference，未直連 Oracle／YMES PostgreSQL、未猜 DDL。
- [ ] 家用端核對 live schema／migration，將日期、Type、Status、QC Item 參數化並限制日期跨度與父子結果數；父子資料使用包含 Product／Version 的明確綁定鍵和一致性 snapshot，穩定排序分頁。
- [ ] 家用端核對部署版 ValueList 的 QCItemType／ExecuteStatus／Inline 翻譯對照；修正舊 DataRelation 遺漏 ProductNo／ProductVersion 的行為需記錄差異。下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC215 管制線修改紀錄（2026-11-23）

- [x] 核對 `frmChartLineEditLog`、inline Designer、`wsSPC.ShowControlChartLineData`、`kcSPC.ShowControlChartLineData` 與 `Load_Check_StartTime`。確認 WinForms 實際只開放 Variable；Attribute radio 隱藏。
- [x] 完成 `/YMES/SPC/ChartLineEditLog` DEV：QC Item／Product／Customer／Material／Vendor 假 lookup、未勾選的預設日期、Query、由 snapshot 重建新增／規格修改／管制線修改唯讀 Grid。新增 [SC215 規格](docs/modules/SPC/管制線修改紀錄.md)，更新 Index、Inventory 與兩端 TODO。
- [x] 記錄來源差異：Form 不傳日期給 Service；日期在歷程重建後用嚴格 `>`／`<` 過濾。Core 查詢無上限且串接 SQL；`CustomerNo` 欄位檢查與 QC302 `CUSTOMER` DDL 不符。舊 `Old_ROW` 未按 SPCID 比較，並可能在多圖別歷程誤比。
- [x] Basis／DetailLog Oracle source DDL 與 PostgreSQL CREATE-only 已有 QC302 references，無新增表。桌機受保護 Oracle profile 不存在，未直連 Oracle／YMES PostgreSQL、未猜 DDL。
- [ ] 家用端核對 YMES live schema／migrations；參數化所有條件、限日期跨度／FactorSerial／結果數並穩定排序分頁。先重建完整 timeline 再套日期，否則區間首筆可能被誤標新增。核對 Customer 欄位是否應對應 `CUSTOMER`。
- [ ] 家用端決定是否修正 SPCID 比較與排序，若按 FactorSerial＋SPCID＋StartTime 配對，記錄相對舊 `Old_ROW` 的行為差異；Factor 值採授權 metadata allowlist 和固定 JSON 欄位，不以動態識別字組 SQL。下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC214 管制圖分析_以要因查詢（2026-11-23）

- [x] 直接核對 `frmQueryBarControlChartByFactor`、`wsSPC.LoadControlChartDataByFactor`、`kcSPC.LoadControlChartDataByFactor` 與共用 `SC_AnsLineBarChart`：Factor 多選、KeyNode、FactorSerial summary、資料／圖表／Histogram drilldown。
- [x] 完成 `/YMES/SPC/ControlChartAnalysisByFactor` DEV：MES Parameter 類型／lookup 假條件、Factor AND／同因子多值 OR、群組摘要、選取群組明細圖表／Raw Data／Histogram，以及 Refresh／Save／Print 假互動；更新 [SC214 規格](docs/modules/SPC/管制圖分析以要因查詢.md)、Index、Inventory、兩端 TODO。
- [x] 查詢行為差異已記錄：Service 未傳 QCItemNo，Core summary 不套用日期；Short-run tab 在 Form Designer 隱藏。Factor 欄位識別字和值被舊 Core 拼入 SQL，不沿用。
- [x] 群組／量測所需 Chart Basis、Variable FactorLog DDL 與 CREATE-only 已有 QC302／QC205 references；無新增資料表。桌機受保護 Oracle profile 不存在，本輪未連 Oracle／YMES PostgreSQL，未猜 DDL。
- [ ] 家用端先核對 live schema／migrations；API 決定是否限定 QCItemNo、是否修正日期 summary 的無效條件，並記錄行為差異。Factor identifier 只接受授權 metadata allowlist；Factor 值全部參數化，設日期／條件數／回傳筆數上限與穩定分頁。
- [ ] Drilldown 重用 SC206 chart analysis contract，限制 SC214 ACL、所選 FactorSerial／QCItemNo、日期或 LatestLots，回傳受限量測、limit／rule 與點選明細。下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC213 出貨報告（SPC COA）（2026-11-23）

- [x] 依 `frmSPC_COA`、標準／THEIL `wsSPC.LoadLotSPCMeasureData` 與 `kcSPC.LoadLotMeasureData` 核對 Variable／Attribute 查詢、COA 最新資料、Factor 欄位、模板匯出和來源缺陷；完成 `/YMES/SPC/CoaReport` DEV 與 [SC213 規格](docs/modules/SPC/出貨報告.md)。
- [x] DEV 提供 QC Item／計量群組選擇、可選日期及 Lot／MO／Product／OP 條件、兩類結果、Factor 顯示選擇、COA token 預覽、模板選擇與本機輸出模擬。OPNo 依舊程式實際行為不套用，頁面明示。
- [x] 計量／計數 SPC 表沿用 repo references；`TBLSPCATTDETAILLOGCOMPONENT` 與 `TBLSPCXLSFILE` 缺可信 DDL／metadata／PG CREATE-only。公司端 Oracle profile 不存在，未連 Oracle／PostgreSQL、未猜 schema。
- [ ] 家用端核對 SPC 主／明細表、`TBLSPCATTDETAILLOGCOMPONENT`、`TBLSPCXLSFILE` 與 live schema／migrations。沒有可信 metadata 前不要推測建表；使用 COA 模板 bytes 時需完整串流、檔案大小／型別限制與權限控制。
- [ ] 家用端決定 OPNo 是否在正式 API 實際篩選；舊 Service/Core 雖接收畫面 XML，未解析或傳遞 OPNo。維持相容時須明示無效果，若修正需記為行為差異。查詢先取跨全期每組 QCItemNo＋LotNo 最新資料再套日期；API 加日期跨度、QC Item／Lot 數及結果列上限，Factor 識別字使用 metadata allowlist，值一律參數化。
- [ ] COA API 限縮 Attribute error/component/tape 明細至已查詢 SPCSerial；模板／匯出避免伺服器端 Excel COM。公司端未實作正式 API/schema；下一個未完成 SPC FunctionKey 為 SC501。

## 公司端最新交付：SC212 計數型管制圖（2026-11-23）

- [x] 直接核對 `frmQueryAttChart`／`frmAnsAttChart`／`frmAnsAttChartSetLimit`、標準 `wsSPC`、`kcSPC` 及 QC Item／Factor lookup；確認可選日期、Normal／Excluded 的 0／1／全選語意、Error detail、Lot 狀態更新與 chart limit 讀寫。
- [x] 完成 `/YMES/SPC/AttributeChart` DEV：假 QC Item／Factor、四種 P／np／C／U 圖、篩選、選點與原因明細、界線編輯／重算、Lot 狀態模擬；新增 [SC212 規格](docs/modules/SPC/計數型管制圖.md)，更新 index、inventory 和家用端 handoff。
- [x] `TBLSPCATTBASISLOG`／`TBLSPCATTDETAILLOG`、`TBLQCREASONBASIS`、`TBLSPCATTLIMIT` 沿用 repo references。`TBLSPCATTCHARTLIMIT` 缺 Oracle DDL／metadata／PG CREATE-only；舊 Core 會在查詢中動態 `ALTER TABLE`，不可移植。公司桌機受保護 Oracle profile 不存在，未連 Oracle、未猜 DDL，也未連 YMES PostgreSQL。
- [ ] 家用端核對 Attribute／Reason／Limit live schema 與 `TBLSPCATTCHARTLIMIT` 是否存在；沒有可信 metadata 前不要建猜測 schema，也不要複製動態 Factor 欄位 DDL。以固定 schema 保存 Factor 條件與管制界線。
- [ ] 家用端提供登入及 SC212 Function ACL 保護的 Attribute QC Item／Factor lookup、限量圖表資料與選取 SPCSerial 的原因明細 API。FactorNo 使用 metadata allowlist，所有 Factor values 參數化；限制日期、批次、因素及明細數。
- [x] SC212 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC211 拒收率／百萬不良數管制圖（2026-11-23）

- [x] 核對 `frmQueryLRRandPPMChart`／`frmAnsLRRandPPMChart`／`frmSetLimitLine`、Customer 與標準 `wsSPC.LoadSPCLRRandPPMAttLogData`、標準 `kcSPC.LoadSPCLRRandPPMAttLogData`；確認週／月／年期間、VISERA calendar 分支、狀態篩選、Result=0 才接受及 LRR／雙系列 PPM 公式。
- [x] 完成 `/YMES/SPC/LrrPpmChart` DEV：QC Item／Factor 假資料選取、日期與期間條件、LRR／PPM、彙總圖表／明細、手動目標線與超標提示、Save／Export／Print 無副作用提示。新增 [SC211 規格](docs/modules/SPC/拒收率百萬不良數管制圖.md)，更新 Index、Function Inventory 與家用端 handoff。
- [x] `TBLSPCATTBASISLOG` 沿用 Attribute Oracle source DDL／PostgreSQL CREATE-only；VISERA 專用 `TBLSYSCALENDAR` 的 DDL／metadata／CREATE-only 在 repo 找不到。公司桌機受保護 Oracle profile 不存在，本輪未連 Oracle、未猜建 calendar，也未連 YMES PostgreSQL。
- [ ] 家用端核對 live `TBLSPCATTBASISLOG`／既有 migrations 與 `TBLSYSCALENDAR`。只有取得可信 metadata 才建立日曆 CREATE-only；正式 API 限制日期跨度／回傳期間，FactorNo 由授權 QC Item metadata allowlist 控制，所有值參數化。
- [x] SC211 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC210 柏拉圖分析（2026-11-23）

- [x] 核對 `frmQueryPareto`／`frmAnsPareto` 與 Detail／Rank／Section／Fishbone／Last4Week forms、Customer／標準 `wsSPC.LoadSPCAttLogData`、`kcSPC.LoadSPCAttLogData`／`LoadSPCAttDetailLogSection`。查詢載入 QC Item／日期／Factor 後，狀態、重測、ReasonType 與統計在結果視窗處理。
- [x] 完成 `/YMES/SPC/ParetoDiagram` DEV：多 QC Item、Factor 聯集、多值條件、日期、狀態／重測／Scrap／Defect、五種排序指標、Top N 柏拉圖與 Reason detail／Fishbone／Rank／Section／Pie／Last 4 Weeks 模擬；Export／Save／Print 僅提示。新增 [SC210 規格](docs/modules/SPC/柏拉圖分析.md)，更新 Index、Function Inventory 與家用端 handoff。
- [x] `TBLSPCATTBASISLOG`／`TBLSPCATTDETAILLOG`、`TBLQCREASONBASIS` 沿用既有 references。Section Detail 使用的 `TBLSPCATTDETAILLOGSECTION` 缺 Oracle DDL／metadata／PG CREATE-only；受保護 Oracle profile 不存在，本輪未連 Oracle、未猜 CREATE，也未連 YMES PostgreSQL。
- [ ] 家用端 pull 後核對 `TBLSPCATTDETAILLOGSECTION` live schema／既有 migration；無可信 DDL 前不猜建表。正式 query 限制日期跨度、批次與 detail 筆數；FactorNo metadata allowlist、值參數化；核對 Product PCS 的 `wsPRD.LoadProductProperty` lookup。
- [x] SC210 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC209 不良率管制圖看板（2026-11-23）

- [x] 核對 `frmQueryPChartSignBoard`／`frmPChartSignBoard`、Customer `wsQC`／`wsSPC` 與標準 `kcSPC`：Attribute QC Item、Factor lookup、多值條件、6 分鐘預設刷新、最近 `LimitLotNum`、Scrap／Sample Chart、limit fallback、ErrorType 0 明細及 Violation／Rule。
- [x] 完成 `/YMES/SPC/PChartSignboard` Vue DEV 頁；點選圖表批次同步明細，Refresh／Save Graph 以本機假資料模擬；更新 [SC209 規格](docs/modules/SPC/不良率管制圖看板.md)、Index、Function Inventory 與家用端 handoff。
- [x] Attribute basis/detail、reason basis、`TBLSPCATTLIMIT` 沿用現有 references。`TBLSPCATTVIOLATIONCONTENT` 缺 Oracle DDL／metadata／PG CREATE-only；公司桌機受保護 Oracle profile 不存在，本輪未連 Oracle、未猜 DDL，未連 YMES PostgreSQL。
- [ ] 家用端 pull 後核對 `TBLSPCATTVIOLATIONCONTENT` live schema／migration；無可信 DDL 前不要猜建表。正式 query 限制批次、Error detail／Violation 筆數，FactorNo 用 QC metadata allowlist，所有值參數化。
- [x] SC209 公司端 DEV／來源規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC208 散佈圖分析（2026-11-23）

- [x] 核對 `frmQueryScatterChart`／inline Designer、`frmAnsScatterChart`、`ScatterChartLayer`、Customer `wsSPC.LoadScatterChartData` 與標準 `kcSPC.LoadScatterChartData`。
- [x] 完成 `/YMES/SPC/ScatterDiagram` DEV：兩個不同 QC Item、共同 FactorNo、可選日期、Lot Report State／Result、LotNo + SerialNo 配對、散佈圖、關聯統計明細；Save Graph／Export 為無副作用提示。
- [x] 新增 [SC208 規格](docs/modules/SPC/散佈圖分析.md)，更新 Module index、Function Inventory 與家用端 handoff。Factor lookup 及相關統計為假資料／近似算法，已記錄未實作的 Product Factor 相依篩選與檔案輸出。
- [x] 計量／Factor log 沿用 QC205 source DDL 與 QC210 CREATE-only reference，沒有新增表。桌機受保護 Oracle profile 不存在，本次未連 Oracle；未連 YMES PostgreSQL、未執行 build／test／browser。
- [ ] 家用端比對 `TBLSPCVARFACTORLOG`／`TBLSPCVARDETAILLOG` live schema／migrations；確認 LotNo + SerialNo 重複配對規則、LotState／Result 及 ScatterChartLayer 舊統計公式。正式查詢限制日期／筆數；Factor 欄位採 metadata allowlist、值參數化。
- [x] SC208 公司端 DEV／規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC207 彙整分析圖（2026-11-23）

- [x] 直接核對 `frmQueryLineBarChart`／inline Designer、共用 `frmAnsLineBarChart`、Customer `wsSPC`、標準 `kcQC`／`kcSPC`；確認 QC Item／動態要因、多值查找、日期、短程轉換公差，以及資料／Line Chart／Histogram／FactorAnalysis／MaxMinMean 流程。
- [x] 完成 `/YMES/SPC/LineChartAnalysis` DEV：QC Item picker、ParameterType 0／1 多選及 2 自由輸入要因、日期條件、Short-Run relative／absolute 選項、三種查詢按鈕與五種分析檢視。
- [x] 新增 [SC207 規格](docs/modules/SPC/彙整分析圖.md)，更新 Module index、Function Inventory 與家用端 API/schema handoff；圖表與統計僅為簡化假資料。
- [x] 沿用 QC302／QC205／QC210 references，未新增業務表。`TBLSPCLIMIT` fallback 缺同版 Oracle DDL／metadata／CREATE-only；桌機受保護 Oracle profile 不存在，沒有連 Oracle 或猜 SQL；未連 YMES PostgreSQL，未執行 build／test／browser。
- [ ] 家用端比對 PostgreSQL live schema／migration，核實 `TBLSPCLIMIT` fallback；正式 API 限制日期跨度及資料量，參數化值、動態 Factor 欄位採 metadata allowlist，重用 Core limit／Rule／Violation／Statistic 規則。
- [x] SC207 公司端 DEV／規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC206 管制圖分析（2026-11-23）

- [x] 核對 `frmQueryBarControlChart`／inline Designer、`frmAnsLineBarChart`、Customer `wsQC`／`wsSPC`、標準 `kcQC`／`kcSPC`；記錄 QC Item／FactorSerial／Factor、一般與短程圖、日期／最近批數、ChartType、ERF／Hold／Rule、Histogram、Save／Print 流程。
- [x] 完成 `/YMES/SPC/ControlChartAnalysis` DEV：QC Item picker、一般／短程群組、KeyNode／期間／最近批數條件、X-R／X-S／X-Rm 假圖、資料點明細、Histogram、ERF／Hold 標示及刷新／儲存／列印模擬。
- [x] 新增 [SC206 規格](docs/modules/SPC/管制圖分析.md)，更新 Module index、Function Inventory 及家用端 API/schema handoff；圖表、統計與限值皆為展示用簡化假資料，不是 Core 正式結果。
- [x] 沿用 QC302 Chart Basis／Detail／Rule 與 QC205／QC210 variable references；`TBLSPCLIMIT` fallback 缺同版 DDL／metadata／CREATE-only。桌機受保護 Oracle profile 不存在，沒有連線或猜 SQL；未新增業務資料表，未連 YMES PostgreSQL，未執行 build／test／browser。
- [ ] 家用端比對 live schema／migrations，核實 SC206 分析呼叫所需資料與 `TBLSPCLIMIT` fallback；正式 API 限制資料筆數／日期範圍，參數化值並以 QC Item Factor metadata allowlist 控制動態欄位。Save Graph 另核授權與檔案上限。
- [x] SC206 公司端 DEV／規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC205 管制圖看板（2026-11-23）

- [x] 直接核對 `frmQueryChartSignBoard`／inline Designer、`frmVar_ChartSignBoard`、Customer `wsQC`／`wsSPC`、標準 `kcQC`／`kcSPC`；確認 QC Item 篩選、在線 FactorSerial 群組、X-R／X-S／X-Rm、違規點、刷新及 Save Graph 呼叫。
- [x] 完成 `/YMES/SPC/ChartSignboard` DEV：QC Item／Data Type 選擇、群組清單、在線要因、假管制圖／能力指標／違規點明細、計時與手動刷新、儲圖提示。
- [x] 新增 [SC205 規格](docs/modules/SPC/管制圖看板.md)，更新 Module index、Function Inventory 及家用端 handoff；使用 repo 既有 QC302／QC205 DDL references，沒有新增表或猜測 DDL。未連 Oracle／PostgreSQL，未執行 build／test／browser。
- [x] DEV 僅使用本機假資料；圖表界線、Cp／Cpk 和違規標示是簡化示意。定時刷新不重新查詢資料，Save Graph 不產生／傳送檔案。
- [ ] 家用端比對 QC／SYS lookup、Chart Basis／Detail Log、Variable Factor／Detail Online／Violation 等 live schema／migrations；正式算法需採來源 Core 規則。新增限量、參數化、Function 權限 API；Save Graph 限制格式／bytes／檔名或依需求不開放。
- [x] SC205 公司端 DEV／規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC204 Excel 匯出樣板（2026-11-23）

- [x] 依 `frmXlsSourceFileControl`、Customer `wsSPC`／標準 `kcSPC` 核對 Control Chart／AutoRPT／COA 類別、樣板 CRUD、BLOB 上下載與 auto-seed。
- [x] 完成 `/YMES/SPC/XlsSourceFileControl` DEV：四種 type、樣板清單、Add／Edit／Copy／多選 Delete／Confirm／Cancel／Download 假互動；檔案只記檔名／大小，不讀取或上傳 bytes。
- [x] 新增 [SC204 規格](docs/modules/SPC/Excel匯出樣板.md)、更新 Module index、Function Inventory 與家用端 handoff；未連 YMES PostgreSQL，未執行 build／test／browser。
- [x] 已確認 Control Chart／COA／AutoRPT 樣板 binary 檔名和本機來源檔存在；只引用 DEV fixtures，沒有複製到 YMES repo。
- [ ] `TBLSPCXLSFILE` 缺同版 Oracle source DDL／metadata／PG CREATE-only；桌機受保護 Oracle profile 不存在，未查 Oracle或猜 BLOB／欄位／key／index。取得唯讀 metadata 後補 source reference 與 CREATE-only handoff。
- [ ] 家用端先比對 YMES live schema／migration；正式 API 需 FunctionNo 授權、檔案大小／副檔名／內容驗證、參數化 metadata、BLOB 上下傳、版本衝突處理與明確 batch delete 語意。修正 legacy 長度少一 byte、空 body 假成功；確認 Core Oracle／MSSQL-only 需換 PostgreSQL data access。
- [x] SC204 公司端 DEV／規格交接完成；下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC203 資料修改記錄（2026-11-23）

- [x] 依 `frmQueryDataEditLog`、`wsSPC.LoadSPCDataHistory`、`kcSPC.LoadSPCDataHistory` 核對計量／計數修改歷程、QC Item／日期／Editor 條件與唯讀 Grid；確認 SC201 使用的 AddSPC*DataEditLog 寫入欄位。
- [x] 完成 `/YMES/SPC/DataEditLog` DEV：Attribute／Variable 切換、Issue／Stop QC Item、Editor、可選日期、修改前後值明細；資料全在頁面記憶體。
- [x] 新增 [SC203 規格](docs/modules/SPC/資料修改記錄.md)、更新 Module index、Function Inventory 與家用端 handoff；未連 YMES PostgreSQL，未執行 build／test／browser。
- [x] Variable `TBLSPCVAREDITLOG` 沿用 QC205 Oracle reference 與 QC210 CREATE-only；Attribute `TBLSPCATTEDITLOG` 沿用既有 Oracle DDL，新增 `database/reference/mestest_qc_attribute_data_edit_log_create.sql` CREATE-only handoff，未套用。
- [ ] 家用端比對 YMES live schema／migrations；正式查詢需 FunctionNo 授權、QC Type allowlist、參數化日期／Editor／QC Item、分頁與筆數上限。VISERA IQC／IPQC／OQC 舊缺陷碼特殊分支需另核企業設定及 WIP Cont metadata。
- [x] SC203 公司端 DEV／規格交接完成；SC204 已依序交接，下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC202 Lot 檢測資料（2026-11-23）

- [x] 依 THEIL_BU1 `frmQueryDataByLotNo_THEIL_BU1`、`THEIL.wsSPC.LoadLotSPCMeasureData`、`MEStc_THEIL.clsSPC.LoadLotMeasureData` 核對查詢條件、兩類 SPC 結果、要因及元件缺陷明細。
- [x] 完成 `/YMES/SPC/DataByLotNo` DEV：Lot／QC Item 模式、計量／計數切換、日期／QC Item 群組／Factor 條件、Lot 分組結果與唯讀明細；資料全在頁面記憶體。
- [x] 新增 [SC202 規格](docs/modules/SPC/Lot檢測資料.md)、更新 Module index、Function Inventory 與家用端 handoff；未連 YMES PostgreSQL，未執行 build／test／browser。
- [x] 計量／計數 SPC 表沿用 repo QC205／QC210／attribute references。`TBLSPCAttDetailLogComponent` 缺同版 Oracle source DDL／metadata／PG CREATE-only；桌機受保護 Oracle profile 不存在，未查 Oracle、未猜 schema。
- [ ] 家用端比對 YMES live schema／migrations，核實元件缺陷明細來源；正式查詢需登入／FunctionNo 授權、限量／分頁、參數化條件與動態 Factor 欄名 allowlist。
- [x] SC202 公司端 DEV／規格交接完成；SC203、SC204 已依序交接，下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC201 SPC 資料維護（2026-11-23）

- [x] 依 `frmQueryData_THEIL_BU1`、`frmAnsAttRawData_THEIL_BU1`、`frmAnsVarRawData`、`wsSPC`、`clsSPC` 核對計數／計量／群組查詢、明細、授權、修改、刪除、歷程及補開 ERF。
- [x] 完成 `/YMES/SPC/DataMaintenance` DEV：動態 QC Item／要因與 Lot／日期查詢、Basis／Raw Data／統計／歷程、編修／取消、計數量守門、計量統計重算、刪除警告及權限模擬、補開假 ERF；資料全在頁面記憶體。
- [x] 新增 [SC201 規格](docs/modules/SPC/SPC資料維護.md)、Module index、Function Inventory 進度與家用端 handoff；未連 YMES PostgreSQL，未執行 build／test／browser。
- [x] 計數與計量 SPC DDL 沿用 repo attribute／QC205／QC210 references。`TBLWIPCONT_QCITEM`、`TBLWIPCONT_SAMPLECOMPONENT`、`TBLWIPCONT_DEFECTCODE` 缺同版 Oracle source DDL／metadata／CREATE-only；桌機受保護 Oracle profile 不存在，未查 Oracle、未猜 schema。
- [ ] 家用端比對 live schema／migration，核對 SPC／WIP／ERF／edit log 關聯完整性與刪除交易；正式 API 必須分別檢查 `SPCDATAEDITATT`、`SPCDATAEDIT`、計量刪除群組權限，並限制查詢、參數化更新及新增 concurrency guard。
- [ ] 家用端重驗計數 GoodQty = SampleQty - ScrapQty、計量統計重算但不重判 Result、FactorSerial 最新批次警告、ERF/WIP 子資料連帶刪除順序；補開 ERF 透過 `WP_BRERFCreate` 正式契約。
- [x] SC201 公司端 DEV／規格交接完成；SC202–SC214 已依序交接，下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC108 進料檢驗_元件（2026-11-23）

- [x] 依 `frmIncomingQCFormComponent`／Def／QC Item Map、`wsSPC`、`clsSPC`、`clsBasis_UPD` 核對七狀態、單據 CRUD／Copy、QC Item Map、元件明細、狀態轉換、ERP callback、複判及 `WP_BRERFCreate` 分支。
- [x] 完成 `/YMES/SPC/IncomingQCFormComponent` DEV：Header CRUD／Confirm、元件 CSV／XLSX／貼上與 Qty 守門、Map、SPC 假結果、Receive／Reject／Concession callback 成敗及 Revise；不呼叫資料庫、SPC、ERP／sQMS、ERF。
- [x] 新增 [SC108 規格](docs/modules/SPC/進料檢驗_元件.md)、Module index、Function Inventory 進度及家用端 handoff；未連 PostgreSQL，未執行 build／test／browser。
- [x] `TBLSPCIQCFORMCOMPONENT`、`TBLSPCIQCFORMCOMPQCITEMMAP`、`TBLSPCIQCCOMPONENT` 缺同版 Oracle source DDL／metadata／PG CREATE-only；桌機受保護 profile 不存在，未查 Oracle、未猜 schema。另記錄刪 Header 未見清理元件明細及 Core Map result collection 重複執行風險。
- [ ] 家用端先核對 YMES live schema／migration 並取得可靠三表結構，再決定必要 CREATE／forward migration；實作 Header、Map、元件清單 replace、Confirm、SPC 結果及狀態 API，處理 DataStamp CAS、component orphan、重複 SQL、callback idempotency／部分成功補償與 `WP_BRERFCreate`。
- [x] SC108 公司端交接完成；SC201–SC214 已依序完成，下一個未完成 SPC FunctionKey 為 SC501。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端交接：SC107 匯入計量檢測數據（2026-11-23）

- [x] 核對 `frmImport_VarMeasureData`／inline Designer、`frmFileTypeSelect`／Designer、`wsSPC.AutoBRSPCContorl_Var`／`LoadSPCImportData`、`clsMDS.funAutoBRSPCContorl_Var` 與 `clsSPC.UpdVar_TestValue`／待檢清單／限值／違規路徑。
- [x] 完成 `/YMES/SPC/VariableMeasurementImport` DEV：一般／元件欄位範本、XLSX／CSV 載入、開發範例、逐列勾選、Check／Save、失敗訊息及已儲存鎖定狀態。DEV 僅在頁面記憶體，不呼叫服務／資料庫。
- [x] 新增 [SC107 規格](docs/modules/SPC/匯入計量檢測數據.md)、Module index、Function Inventory 進度及家用端 handoff；未連 PostgreSQL，未執行 build／test／browser。
- [x] `LoadSPCImportData` Core 直接查 `TBLSPCImportData`，但 SC107 主 Form 搜尋未見呼叫；repo 缺該表 Oracle source DDL／metadata／PG CREATE-only。共用計量寫入表需依 SC101 handoff 與 QC205 references 對照，桌機受保護 Oracle profile 不存在，本次未查 Oracle、未猜 schema。
- [ ] 家用端確認 `LoadSPCImportData` 的真實呼叫端，再決定 `TBLSPCImportData` 是否需正式 API/schema；若需要，先取得 MESTEST DDL／metadata 並比對 live schema。
- [ ] 正式端逐表核實 `funAutoBRSPCContorl_Var` 的 SPC Online／Log／Statistic／Factor／Limit／Violation／SPC Lot QC List 寫入與交易 helper；檢查多列部分失敗、serial 併發、Check 結果不可被信任、SPC 待檢以及條件式 Lot／ERF／通知副作用與補償，提供參數化預覽／檢查／儲存和逐列結果。
- [x] SC107 公司端 DEV／規格完成；SC108「進料檢驗_元件」已接續交付。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC106 出貨檢驗單（2026-11-23）

- [x] 核對 `frmOQCForm`／inline Designer、`frmOQCFormDef`／inline Designer、QC Item Map Form、`wsSPC`、`clsSPC`、`clsBasis_UPD` 及產品／OP／WIP lookup。
- [x] 完成 `/YMES/SPC/OutgoingQCForm` DEV：五狀態、單據 CRUD／Copy／Confirm、Map、SPC 假結果、ERP callback 成敗模擬、ERP 來源待檢退回未確認。
- [x] 新增 [SC106 規格](docs/modules/SPC/出貨檢驗單.md)、Module index 及家用端 API／schema handoff；未連 PostgreSQL，未執行 build／test／browser。
- [x] `TBLSPCOQCFORMQCITEMMAP` Oracle source DDL／PG CREATE reuse QC216 reference；`TBLSPCOQCFORM` Header DDL／metadata／PG CREATE-only 缺失。桌機受保護 Oracle attachment/profile 不存在，本次未查 Oracle、未猜 Header schema。
- [ ] 家用端比對 Header live schema／migration，取得可靠 MESTEST Header DDL／metadata 後才補 CREATE-only；核對 OQC Map 是否與共用 reference 同版。
- [ ] 正式 API 驗證 OQC Form／ERP source ownership、參數化限量查詢、DataStamp CAS、Header＋Map 交易及 QC Map 全量取代；核對 `UpdOQCFormQCItemMapResult` 重複執行 collection 的實際副作用。
- [ ] TIPTOP／T100 ERP callback 保存 idempotency、request／response、錯誤／重試／對帳，外部成功後才更新 OQC Status；確認 WFGP skip 行為與 ERP owner 契約，定義 callback timeout／本機 CAS 衝突補償。OP `SPCInspAbnormalHoldLot` 的 `WP_BRERFCreate` 保持獨立交易並處理部分成功。
- [x] SC106 公司端 DEV／規格完成；清冊下一項 SC107「匯入計量檢測數據」，已完成並交接。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC105 PQC 檢驗單（2026-11-23）

- [x] 核對 `frmIPQCForm`／inline Designer、`frmIPQCFormDef`／inline Designer、QC Item Map Form、`wsSPC`／`wsPRD`／`wsWIP`／`wsSYS`、`clsSPC` 與 `clsBasis_UPD`。
- [x] 完成 `/YMES/SPC/InProcessQCForm` DEV 頁：6 狀態、Product／Module／Lot fixture、CRUD／Copy／Confirm、QC Item Map、SPC 假結果、SFT 六類 Qty 守恆、允收／拒收／特採及 WIP Hold／Wait 提示。
- [x] 新增 [SC105 規格](docs/modules/SPC/PQC檢驗單.md)、Module index 與家用端 handoff；未連 PostgreSQL，未執行 build／test／browser。
- [x] `TBLSPCIPQCFORM` Oracle DDL／PG CREATE 沿用 repo WP201 reference；`TBLSPCIPQCFORMQCITEMMAP` 缺同版 Oracle DDL／metadata／PG CREATE-only。桌機受保護 attachment/profile 不存在，本次未查 Oracle、未猜 Map schema。
- [ ] 家用端 Pull 後比對 WP201 reference 的主表與 live schema／migration；取得 Map table 可靠 Oracle metadata 後才補 CREATE-only。正式 API 重驗 Product／Module／Lot／OP／QC Item／ModuleType、DataStamp 與 `IPQCFORMNO` 序號。
- [ ] 正式 API 設計 SFT Qty 守恆及 callback idempotency／重試／對帳；驗證拒收 + `S_OffLineHold` 時 WIP／ERF 的 Hold／Wait 寫入順序、部分成功與補償。公司端 DEV 不呼叫外部服務。
- [x] SC105 公司端 DEV／規格完成；清冊下一項 SC106「出貨檢驗單」。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC104 終檢檢驗單（2026-11-23）

- [x] 核對 `frmFQCForm`／inline Designer、`frmFQCFormDef`／inline Designer、`frmFQCFormQCItemMap`、`wsSPC`／`wsPRD`／`wsWIP`／`wsSYS`、`clsSPC` 與 `clsBasis_UPD`。
- [x] 完成 `/YMES/SPC/FinalQCForm` DEV 頁：5 個狀態、單據 CRUD／Copy／Confirm、待檢頁未開始單據刪除、Lot／產品／工序 fixture、QC Item Map、SPC 假結果、允收／拒收及狀態稽核欄位。
- [x] 新增 [SC104 規格](docs/modules/SPC/終檢檢驗單.md)、Module index 與家用端 handoff；未連 PostgreSQL，未執行 build／test／browser。
- [x] QC Item Map 沿用 QC216 source DDL／PG CREATE reference；`TBLSPCFQCFORM` 缺同版 Oracle DDL／metadata 與 PG CREATE-only。桌機受保護 attachment/profile 不存在，本次未連 Oracle、未猜主表 schema。
- [ ] 公司端請從 MES 唯讀 Oracle 字典補交 `TBLSPCFQCFORM` 原生 `CREATE TABLE`、欄位 metadata（型別／長度／NULL／DEFAULT）、PK／unique、index、FK、trigger DDL，以及依相同欄位產生的 PostgreSQL CREATE-only SQL。QC Item Map 請沿用 QC216 reference，不需重交；只交結構，不提供終檢單、Lot、產品、客戶或員工等業務資料列。
- [ ] 家用端 Pull 後核對 FQC 主單／Map live schema、migrations、DataStamp compare-and-set 及 `FQCFORMNO` 序號規則；取得可靠主表 DDL 後再補必要 schema。正式 API 驗證產品／工序／版本／Lot／QC Item eligibility，處理主單／Map／量測結果交易與狀態。
- [ ] 家用端整合 Workflow／TIPTOP／WFGP／T100 `funQC_Update` callback，驗證 ERP source／seq／產品與允收或拒收全數 Qty，設計 idempotency、逾時對帳及補償；公司端 DEV 不呼叫外部服務。
- [x] SC104 公司端 DEV／規格完成；清冊下一項 SC105「PQC 檢驗單」。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC103 進料檢驗單（2026-11-23）

- [x] 核對主 Form／inline Designer、`frmIncomingQCFormDef`／inline Designer、QC Item Map Form、`wsSPC`／`wsMTL`／`wsSYS`／`wsOP`、`clsSPC` 及 `clsBasis_UPD`。
- [x] 完成 `/YMES/SPC/IncomingQCForm` DEV 頁：7 狀態頁籤、搜尋與日期、單據 Add/Edit/Copy/Delete/Confirm、物料／供應商 fixture、QC Item map 維護、逐項 SPC 假收集、Receive/Reject/Concession 數量檢核與 Revise。
- [x] 新增 [SC103 規格](docs/modules/SPC/進料檢驗單.md)、Module index 與家用端 handoff；未連 PostgreSQL、未執行 build／test／browser。
- [x] `TBLSPCINCOMINGQCFORMQCITEMMAP` 沿用 QC216 Oracle source DDL／PG CREATE reference；`TBLSPCINCOMINGQCFORM` 缺同版 Oracle DDL／metadata 與 PG CREATE-only。桌機受保護 attachment/profile 不存在，本次未連 Oracle、未猜主表 schema。
- [ ] 公司端請從 MES 唯讀 Oracle 字典補交 `TBLSPCINCOMINGQCFORM` 原生 `CREATE TABLE`、欄位 metadata（型別／長度／NULL／DEFAULT）、PK／unique、index、FK、trigger DDL，以及依相同欄位產生的 PostgreSQL CREATE-only SQL。Map 表請沿用 QC216 已有 reference，不必重交；只交結構，不提供檢驗單、物料、供應商或員工等業務資料列。
- [ ] 家用端 Pull 後核對主單／Map live schema、migrations、DataStamp compare-and-set 及序號規則；取得可靠主表 DDL 後再補必要 schema。正式 API 需參數化、限量查詢，重驗使用者、物料／供應商、ERP-owned、表單狀態、QCItem eligibility 與 Qty，定義主單／Map／量測結果的交易邊界。
- [ ] ERP／sQMS callback、ERP QC Form／WIP／ERF、超時重試／冪等鍵與部分成功補償由家用端整合驗收；公司端 DEV 不呼叫外部服務。
- [x] SC103 公司端 DEV／規格完成；清冊下一項 SC104「終檢檢驗單」。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC102 SPC 量測數據收集_計量連續（2026-11-23）

- [x] 核對標準 `frmSPCOnlineControl_test`／內嵌 Designer、動態連續計量 `frmVar_MeasureContinuous`／Designer、`wsSPC.UpdVar_TestValue/UpdVar_LotData`、`LoadVarQCItemChartData`／`ExecuteQCChartRuleCheck` 與 `clsSPC`。
- [x] 完成 `/YMES/SPC/OnlineControlContinue` DEV 頁：OP／QCItem／Lot／要因／抽樣參數、逐點量測、假設備填值、貼上值匯入、規格提示、Save、Edit／ConfirmEdit／Reverse 與記憶體歷程；正式環境維持未開放提示。
- [x] 新增 [SC102 規格](docs/modules/SPC/SPC量測數據收集_計量連續.md)、Module index 與家用端 handoff；SC102 計量表重用 SC101 Core 執行路徑。
- [x] SC101／SC102 共用計量 Online/Log/Statistic/Factor/Limit 執行表缺同版 Oracle source DDL／metadata 與 PG CREATE-only；受保護 attachment 檔不存在，本次只檢查存在性，未讀取設定、未連 Oracle、未猜 CREATE SQL。
- [ ] 家用端 Pull 後逐表核對 SC102 新增與 `UpdVar_LotData` 修改所用 live schema／migration，取得可靠 DDL 後再補必要 schema；正式 API 重驗登入／SPC 權限、OP/QCItem/Lot/LotStamp、Factor、計畫／參數、限值／規則，交易保存量測及修改 audit，釐清 WIP／QC Form／Lot／ERF／ERP 外部階段與補償。
- [ ] RS232／儀器 driver、量測公式、管制圖／rule、客製 DLL、Component/Wafer 及實際檔案格式需家用端依正式服務契約整合與驗收；公司端只交付 DEV handoff。
- [x] SC102 公司端 DEV／規格完成；清冊下一項 SC103「進料檢驗單」。公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：SC101 SPC 量測數據收集（2026-11-23）

- [x] 核對 Customer `SC_OnlineControl_THEIL_BU1` 主 Form／Designer、計量／計數／Wafer Group 收集 DLL、`wsSPC`／`wsOP`／`wsQC`／`wsWIP`／`wsPRD`／`wsSYS` 與 `clsSPC`。
- [x] 完成 `/YMES/SPC/OnlineControl` DEV 頁：OP／QCItem 查找、4 種 QC 類型、Lot／母體／複檢批、Factor、抽樣計畫與參數、計數缺點／計量讀值、假匯入與記憶體歷程；正式環境維持未開放提示。
- [x] 新增 [SC101 規格](docs/modules/SPC/SPC量測數據收集.md)、Module index 與家用端 handoff。完成前靜態檢閱；未連 PostgreSQL、未執行 build／test／browser。
- [x] 計量 Online/Log/Statistic/Factor 及計數 Basis/Detail 執行表缺同版 Oracle source DDL／metadata 與 PostgreSQL CREATE-only；桌機受保護 Oracle profile 不存在。本次未查 Oracle、不猜 CREATE SQL。
- [ ] 公司端請從公司 MES 的唯讀 Oracle 字典補交以下 9 張執行表的原生 Oracle `CREATE TABLE`／constraint／index DDL 與欄位 metadata（型別、長度、NULL、DEFAULT、PK／unique、index、FK、trigger）：`TBLSPCVARDETAILONLINE`、`TBLSPCVARDETAILLOG`、`TBLSPCVARSTATISTICLOG`、`TBLSPCVARFACTORONLINE`、`TBLSPCVARFACTORLOG`、`TBLSPCVARLIMITCONTENT`、`TBLSPCUSERFACTORVALUE`、`TBLSPCATTDETAILLOG`、`TBLSPCATTBASISLOG`。另提供依原生欄位整理的 PostgreSQL CREATE-only 參考檔。僅查 MESTEST 字典結構，不提供或匯出量測、Lot、員工等業務資料列；未取得可靠來源前，家用端不建立這些表。
- [ ] 家用端逐一核對 SC101 量測寫入分支所用表及 YMES live schema/migrations；可靠 DDL 到手前不建 SC101 執行表。登入授權後由 server 重驗 OP/QCItem/Factor/LotStamp、抽樣／管制規則與 Component，交易配置 SPC Serial 並保存計數／計量主從紀錄、統計／Factor snapshot；釐清 WIP／ERF／ERP 各階段及群組部分成功契約。
- [x] SC101 公司端 DEV／規格完成，依選單清冊接續尚未交付功能；公司端不連 PostgreSQL、不實作或驗收正式 API/schema。

## 公司端最新交付：ES301 配件上機（2026-11-23）

- [x] 核對 `frmApplyEQPAccessory`／內嵌 Designer、`wsEMS.LoadAvailAccStatebyEquipmentType/LoadEQPACCState/UpdEQPACC/ReturnEQPACC`、`wsEQP` 設備 lookup 與 `clsEMSEquipment`／`clsAccState`。
- [x] 完成 `/YMES/EMS/ApplyEquipmentAccessory` DEV 頁：設備／類別 lookup、掃描配件 Find、預設 Available Only、多選批次 Confirm、既有設備關聯的逐件先下機／略過、互斥阻擋、壽命提醒及本頁操作記錄；新增 [ES301 規格](docs/modules/EMS/配件上機作業.md)、索引與家用端 handoff。
- [x] 掛載與配件狀態／歷程表重用 repo Oracle DDL／PostgreSQL CREATE-only；`TBLEMSACCVIOLATESTATUSCHANGE`、`TBLWIPCONT_ACCESSORY` 缺同版 DDL／metadata 與 CREATE-only，桌機受保護 Oracle profile 不存在，本次未查 Oracle、未猜欄位或建 SQL。未連 PostgreSQL、未執行 build／test／browser。
- [ ] 家用端 Pull 後核對既有 references 與 live schema；補查缺少的 Oracle source DDL／metadata，可靠來源到手後再建立必要 CREATE-only。正式 API 需重驗使用者／設備／配件相容性、IssueState、APPLY 控制值、互斥及 expected state，參數化交易寫掛載／狀態／歷程，並明確定義先下機再上機的部分成功／補償。
- [x] ES301 公司端 DEV／來源規格完成；依清冊接續下一個尚缺功能。

## 公司端最新交付：ES108 設備能力設定（2026-11-23）

- [x] 核對 `frmEQPCapMapping`／Def 與內嵌 Designer、`wsEMS.LoadEMSEQPCapMapping/LoadEMSEQPCapMapping_EQP/UpdEMSEQPCapMapping/SetEMSEQPCapMapping_OpenStatus/DelEMSEQPCapMapping`、`clsEMSEquipment`；記錄查詢雙 Grid、整組替換、逐條件開／關及逐台整組刪除。
- [x] 完成 `/YMES/EMS/EquipmentCapabilityMapping` DEV 假資料頁：Equipment Type／No／CapItem 查詢、Add／Edit／Copy、複合條件檢查、開／關、多設備刪除；新增 [ES108 規格](docs/modules/EMS/設備能力設定.md)、索引及家用端 handoff。
- [x] `TBLEMSEQPCAPMAPPING` 的 Oracle source DDL／metadata 與 PostgreSQL CREATE-only 不在 repo；桌機受保護 Oracle profile 檔不存在，本次未查 Oracle，未猜欄位／鍵／DDL。沿用 ES107 已知能力項目主檔參照；未連 PostgreSQL，未執行 build／test／browser。
- [ ] 家用端 Pull 後比對 mapping table live schema／migration。若缺表或無法核實欄位／鍵，先取得允許的 Oracle metadata，再補 CREATE-only；正式 API 以交易原子整組替換、expected-set／status 保護、參數化查詢，並定義多設備刪除與多列狀態更新的批次結果語意。
- [x] ES108 公司端 DEV／來源規格完成；依清冊接續下一個尚缺功能。

## 公司端最新交付：ES107 設備能力項目設定（2026-11-23）

- [x] 核對 `frmEQPCapBasis`／`frmEQPCapBasisDef` 與內嵌 Designer、`wsEMS.Load/Add/Edit/Del/ApproveEMSEQPCapBasis`、`clsEMSEquipment`、`clsBasis_UPD`；記錄四狀態、CapItem 搜尋、CRUD／Copy、逐筆刪除及 `EMS_EQP_CAPBASIS` 核准分支。
- [x] 完成 `/YMES/EMS/EquipmentCapabilityBasis` DEV 假資料頁：四種狀態、搜尋、六欄 Grid、Add/Edit/Copy/Apply/Confirm、多選刪除與直接核准／送簽假操作；新增 [ES107 規格](docs/modules/EMS/設備能力項目設定.md)、索引及家用端 handoff。
- [x] 重用 PD102 已保存的 `TBLEMSEQPCAPBASIS` Oracle dictionary DDL 與 PostgreSQL CREATE-only reference；前次 GET_DDL 回 ORA-03135，未猜 schema，未重新連 Oracle或查業務資料。本次未連 PostgreSQL、未跑 build/test/browser。
- [ ] 家用端比對 live schema／migration，實作授權、參數化及限量分頁 CRUD、DataStamp 衝突處理、引用檢查與依 server `EMS_EQP_CAPBASIS` 設定執行核准流程。
- [x] ES107 公司端 DEV／來源規格完成；清冊下一項 ES108「設備能力設定」。

### ES107 簽核流程交接補件（供家用端接 API）

- [ ] 請提供 `ApproveEMSEQPCapBasis` 對應 `EMS_EQP_CAPBASIS` 的完整來源判斷：實際讀取的設定／Job／Detail／issue process／通知資料表或 view 名稱、join/key 條件、無 Job、無 Detail、有簽核群組三種分支，以及每個分支更新 `IssueState`／`DataStamp` 的順序。
- [ ] 若 YMES PostgreSQL 尚無上述簽核資料表，請交付每張必要表的 Oracle 原生 `CREATE TABLE`／constraint／index DDL 與對應 PostgreSQL CREATE-only DDL；並列出本功能要讀寫的欄位。請勿提供正式簽核紀錄、員工資料、通知收件人或其他業務資料列。
- [ ] 說明第一簽核群組通知如何由 server 呼叫、其失敗時來源系統如何處理；附呼叫介面／程式路徑即可，不需真實通知內容或收件名單。
- [ ] 如果來源系統的共用簽核 API 已集中處理以上邏輯，請交付可用的 API／service 名稱、必要參數及成功／失敗結果語意，不要只交 UI mock。
- [ ] 家用端目前 `POST /api/ems/equipment-capability-bases/approve` 在資料不完整時固定回 409，不會改主檔；收到並核對以上交接後再實作與驗收。

## 公司端最新交付：ES407 設備配件現況查詢（2026-11-23）

- [x] 核對標準 `frmEquipmentAccState`／內嵌 Designer、`wsEMS.LoadEquipmentAccState`、`clsAccState.LoadEquipmentAccState` 及配件監控帶入 AccessoryNo 的開窗流程。
- [x] 完成 `/YMES/EMS/EquipmentAccStateQuery` DEV 查詢頁：設備／配件類別與編號、使用者、Start Time 起訖、假 lookup picker、AND 條件查詢及現況 Grid；新增 [ES407 規格](docs/modules/EMS/設備配件現況查詢.md)、索引與家用端 handoff。
- [x] DEV 只用虛構掛載資料；未呼叫 API／Service、未連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。Legacy AccessoryType 會覆蓋先前 XML 條件，DEV 改用預期 AND 語意。
- [x] 重用 `TBLEMSEQPACCSTATE` 既有 Oracle DDL／metadata 與 PostgreSQL CREATE-only references，無新增 DDL；Grid 保留來源可見 `AccApplySerialNo` 並隱藏 `AccessoryVersion`。
- [ ] 家用端比對 live schema，實作 FunctionNo `EQP ACC STATE QUERY` 權限保護、參數化唯讀查詢、穩定分頁及最大筆數／期間守門；User lookup 不回 Password／ResetPassword。
- [x] ES407 公司端完成；依清冊接續下一功能。

## 公司端最新交付：ES408 設備配件歷史資料查詢（2026-11-23）

- [x] 核對標準 `frmEquipmentAccStateLog`／內嵌 Designer、`wsEMS.LoadEquipmentAccStateLog`、`clsAccState.LoadEquipmentAccStateLog` 與配件監控帶入 AccessoryNo 的開窗流程。
- [x] 完成 `/YMES/EMS/EquipmentAccStateLogQuery` DEV 查詢頁：設備類別／編號、配件類別／編號、使用者與 Start／End 四個獨立時間界限、假 lookup picker、精確篩選和 7 欄歷程 Grid；新增 [ES408 規格](docs/modules/EMS/設備配件歷史資料查詢.md)、索引與家用端 handoff。
- [x] DEV 只用虛構歷程，未呼叫 API／Service、未連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。DEV 以 AND 組合篩選；Legacy Form 在 Accessory Type 條件會覆蓋之前的設備／使用者條件，此差異已記錄。
- [x] `TBLEMSEQPACCSTATELOG` 及 lookup tables 沿用既有 Oracle DDL／metadata 與 CREATE-only references，無新增 DDL；空結果以正常空狀態處理，沒有實際 Oracle 連線。
- [ ] 家用端核對 live schema 後，實作 FunctionNo `EQP ACC STATE LOG QUERY` 權限保護、參數化唯讀 API、時間範圍／列數限制、穩定分頁；User lookup 僅提供安全欄位，空結果回傳空陣列。
- [x] ES408 公司端完成；依選單清冊接續下一項。

## 公司端最新交付：ES101 設備控管設定（2026-11-23）

- [x] 核對標準 `frmEquipmentControl`／內嵌 Designer、`wsEMS.LoadEquipmentStateDefine/UpdEquipmentState` 與 `clsEQPBasis`／`clsEQPStateControl`／`clsEMSEquipment` Core。
- [x] 完成 `/YMES/EMS/EquipmentControl` DEV 頁：設備類別篩選、未管制／已管制設備多選、左右搬移、父設備自動帶 Chamber、Apply 假套用、Confirm 假套用並關閉、Close 丟棄未套用差異；新增 [ES101 規格](docs/modules/EMS/設備控管設定.md)、索引與家用端 handoff。
- [x] DEV 設備、類別、狀態及 Chamber 皆為虛構，只有頁面記憶體變更；未呼叫 Service／API、未連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 沿用既有 refs，無新增 DDL；`TBLEQPEQUIPMENTBASIS`、`TBLEQPSTATECONTROL`、`TBLEMSEQUIPMENTSTATE`、`TBLEMSEQUIPMENTSTATELOG` 有 Oracle DDL／CREATE-only。`TBLEQPEQUIPMENTTYPE` 目前只有 PostgreSQL CREATE-only，缺 Oracle source DDL／metadata，詳下方待公司端補件項目。
- [ ] 家用端核對既有表 live schema，實作登入與 `EMS CONTROL` 權限保護的原子批次 API；伺服器端重驗核准父設備並展開 Chamber、查 Idle state、expected-state 衝突檢查，整批更新 current/log 並回滾失敗。
- [ ] 公司端補交 `TBLEQPEQUIPMENTTYPE` 的 Oracle source DDL、欄位／NULL／DEFAULT／PK／index／trigger 唯讀 metadata 及對應 PostgreSQL CREATE-only；目前 repository 只有 `database/reference/mestest_equipment_type_create.sql` CREATE-only，缺 Oracle source DDL，家用端 live audit 也確認該表不存在。只查 MESTEST metadata，不查業務資料、不連家用 PostgreSQL。
- [x] ES101 公司端完成；依選單清冊接續下一個尚缺的 WinForms 功能。

## 公司端最新交付：ES303 配件壽命維護（2026-11-23）

- [x] 核對本機 STD `frmAccessoryRepair`／Designer、`wsEMS.LoadAccessoryStateJoinLife/UpdEMSAccesssoryRepair`、`wsEQP.LoadAccessoryBasis/LoadACCStateControlJoinStateBasis`、`wsSYS.GetAccessoryRepairNo` 與 `clsAccState`／`clsSYSEng`。
- [x] 完成 `/YMES/EMS/AccessoryRepair` DEV 頁：配件多選／掃描、多版本現況、累積量及週期起算時間編輯、單號假預留、調帳確認、Return Zero、維修次數／備用量／總使用量及報廢規則假套用；新增 [ES303 規格](docs/modules/EMS/配件壽命維護.md)、索引與家用端 handoff。
- [x] 所有配件、版本、數量、狀態與單號均為虛構值，僅修改頁面記憶體；未呼叫 Service／API、未連 Oracle／YMES PostgreSQL、未寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 沿用現有配件主檔／狀態／生命設定／狀態互斥 DDL references；`TBLEMSACCESSORYREPAIR` 缺 Oracle source DDL／metadata 及 PG CREATE-only。桌機受保護 Oracle profile 不可用，未連線或猜 schema；序號規則須家用端確認。
- [ ] 家用端比對 repair table live schema／migration 與 Serial Rule；取得核准 metadata 後補 CREATE-only。正式 API 須以單一交易寫維修明細、更新數量並視情況更新報廢狀態／歷程，使用 expected 原值做衝突檢查。
- [x] ES303 已完成；後續尚缺功能依選單清冊與本機來源持續接續。
- [ ] 公司端以受保護 MESTEST Oracle profile 唯讀取得 `TBLEMSACCESSORYREPAIR` 的 DBMS_METADATA table/index DDL、欄位／NULL／DEFAULT／PK／index／trigger metadata，並提供對應 PostgreSQL CREATE-only；另核實 `GetAccessoryRepairNo` 使用的 Serial Rule／設定證據。不可查業務資料、不可連家用 PostgreSQL，也不自行猜欄位或取號規則。

## 公司端最新交付：ES105 配件狀態互斥設定（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 相同的 `frmEQPACCStateControl` Form／Designer、`wsEQP.LoadAccessoryCategory/LoadEQPACCStateBasis`、`wsEMS.LoadViolateStateAndShowNowName/UpdACCViolatStateChangeData` 及 `clsAccBasis`／`clsEMSEquipment` Core。
- [x] 完成 `/YMES/EMS/AccessoryStateViolativeSetting` DEV 頁：配件類別選擇、配件狀態色矩陣、雙擊切換禁止 pair、Confirm 假套用及 Close；新增 [ES105 規格](docs/modules/EMS/配件狀態互斥設定.md)、索引與家用端 handoff。
- [x] DEV 狀態／類別／互斥組合全為虛構；Confirm 僅更新本頁記憶體，不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 重用作用中矩陣的 `TBLEQPACCSTATEBASIS` 已有 Oracle source DDL 與 CREATE-only，無新增 DDL。repo 雖有 `TBLEQPACCSTATECONTROL` DDL，但 Form 目前流程不使用該舊 helper。`TBLEMSACCVIOLATESTATUSCHANGE`／`TBLEMSACCVIOLATESTATUSLOG` 缺 Oracle source DDL／metadata 及 CREATE-only；桌機受保護 Oracle profile 不可用，未連線或猜 schema。
- [ ] 家用端比對兩張互斥設定／歷程表 live schema／migration，取得核准 DDL 後補 CREATE-only；登入授權、參數化，於單一交易原子替換完整矩陣與歷程，並由 server 查狀態名稱。Legacy 逐狀態 Delete+Add 沒有涵蓋全矩陣的交易，失敗可能部分更新。
- [x] ES105 完成；後續 ES303 已於同日完成並交接。

## 公司端最新交付：ACC MONITOR_BU1 配件狀態監控_管理（2026-11-23）

- [x] 核對 DEV_SILVA Customer `frmAccessoryStateMonitor`、共用 `wsEQP`／`wsEMS.ChangeAccessoryState` Service 與 `clsAccState.ChangeACCState` Core；Customer／STD Service source hash 一致。標準 Form 是已完成的 ES304；此客戶版 Form 僅在 DEV_SILVA Customer 找到。
- [x] 完成 `/YMES/EMS/AccessoryStateMonitorBu1` DEV 頁：類別／類型假選擇器、STATES／LOG、列勾選、0–5 多選狀態模擬、逐筆成功／略過／失敗摘要及 Refresh LOG；新增 [規格](docs/modules/EMS/配件狀態監控管理.md)、索引與家用端 handoff。
- [x] 所有 DEV 配件、狀態、顏色、歷程皆為虛構並限本頁記憶體；未呼叫 API／Service、未連 Oracle／YMES PostgreSQL、未寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 重用配件現況／歷程／基礎／狀態既有 Oracle DDL 與 CREATE-only，無新增 DDL。客戶版直接呼叫的 `GETACC_STATE`、`GET_ACCLOG`、`GETACCSTATE_FIELD` 及 `AccessoryApi.ChangeAccessoryState` REST server source 未在本機來源樹找到；未猜 SQL、response 欄位或授權行為。
- [ ] 家用端依規格核對相關表 live schema／migration，取得缺少 REST server 契約；新 API 需登入授權、伺服器端推導 user／SAP 範圍、限制目前版本、參數化及分頁，狀態更新重驗 expected state 並原子記錄 log／current／新序號。多選結果要明確支援部分成功。
- [x] `ACC MONITOR_BU1`、ES105 已完成；ES303「配件壽命維護」已完成；後續依清冊接續。

## 公司端最新交付：ES405 配件狀態查詢（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 相同的 `frmAccessoryState`，THEIL `wsEMS.LoadAccessoryState`／`LoadAccessoryReasons`、`wsEQP` 選擇器及 `clsEMSEquipment`／`clsAccState` Core。
- [x] 完成 `/YMES/EMS/AccessoryStateQuery` DEV 頁：配件類型／編號條件、假選擇器、Query、12 欄配件現況、壽命剩餘量及 ACCSERIALNO 理由子列；新增 [ES405 規格](docs/modules/EMS/配件狀態查詢.md)、索引與家用端 handoff。
- [x] 配件、版本、使用者、數量、狀態及原因均為虛構，只存在本頁記憶體；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 重用 `TBLEMSACCESSORYSTATE`、`TBLEQPACCSTATEBASIS`、`TBLEQPACCESSORYLIFE` 既有 Oracle DDL／PostgreSQL CREATE-only。`TBLEMSACCESSORYREASONS` 缺 Oracle source DDL／metadata 與 CREATE-only；桌機受保護 Oracle profile 不可用，本次記錄缺項且未猜 schema。
- [ ] 家用端比對三張已知表 live schema／migration；取得核准 Oracle 唯讀 metadata 後補 `TBLEMSACCESSORYREASONS` CREATE-only。正式 API 需授權、參數化及穩定分頁；逐列載入理由，並決定各 AccessoryVersion 的顯示語意／欄位及 StateName 是否為現況名稱。
- [x] ES405、`ACC MONITOR_BU1`、ES105 均已完成；ES303「配件壽命維護」已完成；後續依清冊接續。

## 公司端最新交付：ES406 配件歷史資料查詢（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 三份一致的 `frmAccessoryStateLog`，THEIL `wsEMS.LoadAccessoryStateLog`／`LoadAccessoryReasons`、`wsEQP`／`wsUSR` 查詢選擇器及 `clsAccState` Core。
- [x] 完成 `/YMES/EMS/AccessoryStateLogQuery` DEV 頁：配件類型／編號／使用者／起始時間篩選、假選擇器、Query、配件歷程主 Grid、選取列載入原因明細；新增 [ES406 規格](docs/modules/EMS/配件歷史資料查詢.md)、索引及家用端 handoff。
- [x] 歷程、使用者、配件與原因均為虛構，只存在本頁記憶體；不呼叫正式 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 重用 `TBLEMSACCESSORYSTATELOG`、`TBLEQPACCSTATEBASIS` 既有 Oracle DDL／PostgreSQL CREATE-only。`TBLEMSACCESSORYREASONS` 缺 Oracle source DDL／metadata 與 CREATE-only；桌機受保護 Oracle profile 不可用，本次記錄缺項且未猜 schema。
- [ ] 家用端先比對兩張已知表 live schema／migration，唯讀取得 `TBLEMSACCESSORYREASONS` DDL／PK／index 後補 CREATE-only；正式查詢需登入授權、參數化、穩定分頁、避免無範圍全表掃描，User lookup 僅回傳安全的 UserNo／名稱。
- [x] ES405、`ACC MONITOR_BU1` 與 ES105 均已完成；ES303「配件壽命維護」已完成；後續依清冊接續。

## 公司端最新交付：ES103 配件狀態控制設定（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 一致的 `frmEQPACCStateControlSet` Form／內嵌 Designer，THEIL `wsEQP.LoadEQPACCStateBasis`／`LoadACCStateControl`／`UpdACCStateControl`，以及 `clsAccBasis`、`clsEQPStateControl`、`clsAccState` Core。
- [x] 完成 `/YMES/EMS/AccessoryStateControlSet` DEV 畫面：7 個配件控制狀態、模擬 `IC_FT_PKG` 時出現 `I_SetupClose`、狀態選擇視窗、Apply 假套用、Confirm 假儲存並關閉、Close 丟棄未套用變更；新增 [ES103 規格](docs/modules/EMS/配件狀態控制設定.md)、索引及家用端 handoff。
- [x] 所有 DEV 狀態資料均為虛構且只存在頁面記憶體；不呼叫正式 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 沿用 `TBLEQPACCSTATECONTROL`／`TBLEQPACCSTATEBASIS` 既有 Oracle DDL 與 PostgreSQL CREATE-only references，無新增 DDL。受保護 Oracle profile 不可用，但本項直接表 DDL 已在 repo，無需補猜或另查。
- [ ] 家用端比對 live schema／migration；批次設定交易需同步處理舊設定狀態的配件歷程、現況及新序號。Form 每次送全組，Legacy Core 對未變更控制值也會重寫舊狀態配件、寫 log 並換發序號；逐列再 Delete+Insert 控制設定，沒有涵蓋整批的交易，失敗可能部分生效。正式端只處理實際變更 ControlNo，參數化並回滾完整交易、核實 `IC_FT_PKG` 資格及回傳影響筆數。
- [x] 清冊下一未完成項目為 ES405「配件狀態查詢」。

## 公司端最新交付：ES305 配件狀態監控_設備（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 三份相同的 `frmAccessoryStateMonitorByEQP`／內嵌 Designer，及 `wsEQP` 設備 lookup、`wsEMS.LoadAccessoryStatebyEquipment`／`ChangeAccessoryState`、`clsAccState.LoadAccessoryNoByEquipment`／`ChangeACCState` 與 `clsEMSEquipment` 配件狀態 Core。
- [x] 完成 `/YMES/EMS/AccessoryStateMonitorByEquipment` DEV 頁：設備類別／編號篩選、每頁 20 筆掛載配件、狀態色卡片、分類互斥停用、記憶體狀態變更、右鍵／雙擊假視窗；新增 [ES305 規格](docs/modules/EMS/配件狀態監控設備.md)、索引及家用端 handoff。
- [x] 假設備／掛載／配件／狀態資料皆為虛構，變更只留本頁記憶體；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 沿用配件掛載／現況／狀態／控制與設備基礎 references。`TBLEQPEQUIPMENTTYPE` 缺同版 Oracle source DDL，`TBLEMSACCVIOLATESTATUSCHANGE` 缺 Oracle source／CREATE-only；桌機受保護 Oracle profile 不可用，已記錄未猜 schema。
- [ ] 家用端比對缺 DDL 表與 live schema／migration；監控 GET 要要求設備類別或編號、授權並分頁。Core 查詢按 AccessoryNo join 多版本表，正式 API 必須使用目前掛載的 AccessoryNo+Version；狀態更新另需在交易內重驗分類權限、互斥與 expected state。
- [x] ES103 已完成；清冊下一未完成項目為 ES405「配件狀態查詢」。

## 公司端最新交付：ES304 配件狀態監控（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 相同的 `frmAccessoryStateMonitor`／內嵌 Designer，並追 `wsEQP`／`wsEMS` lookup、`LoadAccessoryStatebyCategory`／`ChangeAccessoryState` 與 `clsEMSEquipment`／`clsAccState` Core。
- [x] 完成 `/YMES/EMS/AccessoryStateMonitor` DEV 頁：分類／類型篩選、每頁 20 筆、狀態色配件卡、互斥停用、記憶體狀態變更、右鍵及雙擊假視窗；新增 [ES304 規格](docs/modules/EMS/配件狀態監控.md)、索引及家用端 handoff。
- [x] 配件、狀態、色碼及互斥 pair 均為虛構資料，沒有呼叫 API／Service、連 Oracle／YMES PostgreSQL 或寫入正式資料；未執行 build、test、browser 或 WinForms runtime。
- [x] 重用現有配件類型／基礎／狀態／現況／歷程 source DDL 及 CREATE-only references；`TBLEMSACCVIOLATESTATUSCHANGE`、`TBLEMSACCESSORYREASONS` 缺同版 Oracle source DDL／CREATE-only，桌機受保護 Oracle profile 不可用，已記錄且未猜 schema。
- [ ] 家用端先比對缺 DDL 表與 live schema／migration，再接授權、參數化、分頁監控 API；一般狀態轉換須原子寫狀態歷程／現況並重驗互斥、類別狀態權限、版本與序號。Apply／Return 應走 ES301／ES302 專用流程。
- [x] ES304／ES305 均完成；其後 ES103 已完成，清冊下一未完成項目為 ES405「配件狀態查詢」。

## 公司端最新交付：ES104 設備狀態互斥設定（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 三份相同的 `frmViolateStateChangeSetMatrix`／內嵌 Designer、`wsEQP.LoadEQPStateBasis`、`wsEMS.LoadViolateStateChange`／`UpdViolatStateChangeData` 與 `clsEQPBasis`／`clsEMSEquipment` Core。
- [x] 完成 `/YMES/EMS/ViolateStateChangeSetMatrix` DEV 頁：狀態色矩陣、雙擊切換 forbidden pair、dirty 格數、Confirm 假儲存及 Close；新增 [ES104 規格](docs/modules/EMS/設備狀態互斥設定.md)、索引及家用端 handoff。
- [x] DEV 只操作虛構狀態與記憶體 pair；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料。Form 有 `Activation.Disabled`／`AllowUpdate=False` 與雙擊 handler 衝突，DEV 依 handler 預期互動；未執行 build、test、browser 或 WinForms runtime。
- [x] `TBLEMSVIOLATESTATUSCHANGE`、`TBLEMSVIOLATESTATUSLOG` 在 repo 缺 Oracle source DDL／PostgreSQL CREATE-only；桌機受保護 profile 不可用，已記錄缺項，沒有猜建表。
- [ ] 家用端取得並唯讀核對上述兩張直接寫入表的 DDL、主鍵／索引和 log 欄位；再做登入授權、參數化互斥矩陣 API，以交易原子替換各來源狀態的完整 forbidden pair 並寫 history。
- [x] ES304、ES305、ES103 均已完成；目前接續 ES406「配件歷史資料查詢」。

## 公司端最新交付：ES202 設備狀態監控_設備類別（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 三份相同選擇器 Form／內嵌 Designer，並追共用 `frmEquipmentStateMonitor_THEIL_BU1`、`wsEQP.LoadEquipmentType/LoadEquipmentBasis/LoadEQPStateBasis`、`wsEMS.LoadEquipmentState/EditEquipmentState` 與 `clsEQPBasis`／`clsEMSEquipment` Core。
- [x] 完成 `/YMES/EMS/EquipmentStateByType` DEV 頁：核准類別選擇、Confirm 開啟共用監控預覽、每頁 10 台、狀態色／翻頁／Refresh、DEV Chamber、狀態按鈕和右鍵假視窗。新增 [ES202 規格](docs/modules/EMS/設備狀態監控設備類別.md)、索引及家用端 handoff；設備圖片、版本／權限／PM 工單／互斥／批號／腔體交易規則列為簡化。
- [x] 狀態變更僅在本頁虛構資料記憶體生效；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料。未執行 build、test 或 browser。
- [x] 沿用設備類別、設備基礎、狀態與互斥 references，沒有新增 CREATE SQL。`TBLEQPEQUIPMENTTYPE` repo 只有 PostgreSQL CREATE-only，缺原始 Oracle source DDL；桌機受保護 profile 不可用，已記錄未查 metadata。
- [ ] 家用端比對 `TBLEQPEQUIPMENTTYPE`、`TBLEQPEQUIPMENTBASIS`、`TBLEMSEQUIPMENTSTATE`、`TBLEQPSTATEBASIS` live schema／migrations，設計授權類別及監控查詢；狀態寫入沿用 ES204 的版本／互斥／PM／Lot／Chamber 規則與交易。
- [x] 清冊下一未完成項目 ES104「設備狀態互斥設定」已完成；目前接續 ES304。

## 公司端最新交付：ES203 設備狀態監控_區域（2026-11-23）

- [x] 核對 CSiMES／THEIL／DEV_SILVA 三份相同的主 Form 與 Designer；追 `wsSMD.LoadBaseArea`／`LoadAreaRelationDefine`、`wsEMS.LoadEquipmentStateBySMD` 及 `clsSMDBasis`／`clsEMSEquipment` Core。
- [x] 完成 `/YMES/EMS/EquipmentStateByArea` DEV 頁：Area Color／Equipment State Color、Base Area 選擇、五種節點、縮放、Grid、Refresh、節點選取／右鍵／雙擊預覽。新增 [ES203 規格](docs/modules/EMS/設備狀態監控區域.md)、索引及家用端 handoff；AddFlow 碰撞群選、Grid 詳細設定、底圖 BLOB 與外部視窗明列為簡化。
- [x] DEV 僅使用固定虛構節點、設備狀態與 IMESTECH 右鍵假企業別，不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫正式資料；未執行 build、test 或 browser。
- [x] 沿用 SMD Area、設備狀態與 equipment-group references，無新增 DDL。`TBLEQPGROUPDETAIL` repo 只有 CREATE-only，缺同版 Oracle source DDL；本機受保護 profile 不可用，已記錄缺項。
- [ ] 家用端先唯讀比對 `TBLSMDAREABASIS`、`TBLSMDAREARELATION`、`TBLEMSEQUIPMENTSTATE`、`TBLEQPSTATEBASIS`、`TBLEQPGROUPDETAIL` live schema／migrations，再實作登入授權、參數化 BaseArea／設備範圍、群組展開、現況狀態色與刷新 API；直接 area layout 可共用 SMD SD108 handoff。
- [x] 清冊下一列 ES202「設備狀態監控_設備類別」與 ES104「設備狀態互斥設定」均已完成；未完成清冊下一項為 ES304。

## 公司端最新交付：ES403 設備歷史資料查詢_狀態（2026-11-23）

- [x] 核對 THEIL `frmEquipmentStateLogByState`／資料表與趨勢圖子 Form、`wsEMS.LoadEquipmentStateLogByState`、`clsEMSEquipment.LoadEquipmentStateLogByState`；確認 EquipmentState 必選、可選設備／人員條件、Designer 可見的 StartTime 範圍，以及依 StartTime 日彙總。EndTime 控制項隱藏，雖有 Service/Core 分支但不可從表單操作。
- [x] 完成 `/YMES/EMS/EquipmentStateLogByState` DEV 頁：假 lookup、狀態必選、可見 StartTime 含端點日期條件、每日結果表與數值標籤趨勢長條圖。新增 [ES403 規格](docs/modules/EMS/設備歷史資料查詢狀態.md)、索引及家用端 API/schema handoff；圖表單頁切換，舊隱藏圖表按鈕未呈現。
- [x] 直接表 `TBLEMSEQUIPMENTSTATELOG` 與 state basis 沿用 WP305／equipment-state references，沒有新增 DDL／CREATE-only；本次未連 Oracle、YMES PostgreSQL，未讀業務資料；未執行 build、test 或 browser。
- [ ] 家用端比對 `TBLEMSEQUIPMENTSTATELOG`／`TBLEQPSTATEBASIS` live schema／migrations，實作登入與設備範圍授權、參數化唯讀 API、日期／結果限制及一致的小時計算與捨入規則。
- [x] ES203「設備狀態監控_區域」、ES202「設備狀態監控_設備類別」及 ES104「設備狀態互斥設定」均已完成公司端交付；清冊下一未完成項目為 ES304。

## 公司端最新交付：ES404 設備歷史資料查詢（2026-11-23）

- [x] 依 THEIL `frmEquipmentStateLogQuery`／Top Ten Grid 與 Pie／Bar／Label Bar 子 Form、`wsEMS.LoadEquipmentStateLogTopTen`、`clsEMSEquipment.LoadEquipmentStateLogTopTen` 核對查詢欄位、兩種時間模式、狀態時數聚合與 Others。
- [x] 完成 `/YMES/EMS/EquipmentStateLogQuery` DEV 頁：Equipment Type／No／User／State 篩選與假 lookup、Start／End 日期模式、獨立 From／To checkbox、Top Ten 表格、Pie、Bar、Bar(Label)；新增 [ES404 規格](docs/modules/EMS/設備歷史資料查詢.md)、索引與家用端 handoff。
- [x] DEV 僅使用虛構狀態歷程、Equipment／User／State 資料及頁面內計算，不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫資料；未執行 build、test 或 browser。圖表於單頁切換，簡化 WinForms 分開開窗。
- [x] 直接表沿用 WP305 狀態歷程與 equipment-state basis references；無新增 DDL／CREATE-only。桌機受保護 Oracle profile 不可用，本次沒有連 MESTEST 或查業務資料。
- [ ] 家用端比對兩張直接表 live schema／migrations，實作登入授權、參數化、設備可見範圍、日期跨度／全歷史掃描限制及單一小時計算／捨入契約；Oracle／非 Oracle Core 時數精度不同。
- [x] ES403「設備歷史資料查詢_狀態」、ES203「設備狀態監控_區域」、ES202「設備狀態監控_設備類別」與 ES104「設備狀態互斥設定」均已完成公司端交付；目前清冊下一未完成項目為 ES304。

## 公司端最新交付：ES302 配件下機（2026-11-23）

- [x] 核對 THEIL、DEV_SILVA、CSiMES 三份 `frmReturnEQPAccessory.vb`（SHA-256 相同）、`wsEMS.LoadDistinctEQPACCStateACCType`／`LoadEQPACCState`／`ReturnEQPACC`、`clsEMSEquipment.UpdEQPACCState_Return`，以及 `clsAccState` 權限／壽命提示／流水號 helper。
- [x] 完成 `/YMES/EMS/ReturnEquipmentAccessory` DEV 假資料頁：配件類別與已掛載配件 lookup、版本／設備／Lot 摘要、互斥阻擋、壽命警示、Confirm 後模擬移除掛載與頁面內下機紀錄；新增 [ES302 規格](docs/modules/EMS/配件下機作業.md)、索引及家用端 handoff。
- [x] DEV 僅變更虛構資料的頁面記憶體，不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫入正式資料；未執行 build、test 或 browser。
- [x] 沿用配件狀態／設備掛載及 WP201 references；`TBLWIPCONT_ACCESSORY`、`TBLEMSACCVIOLATESTATUSCHANGE` 找不到同版 Oracle source DDL／PG CREATE-only，受保護 Oracle profile 不可用，沒有推測建表或連 Oracle。
- [ ] 家用端比對上述兩張缺少參考的 live schema／migration，並確認配件狀態、掛載、WIP 使用時間及狀態互斥的版本與主鍵；依規格實作授權、參數化及原子下機 API。
- [x] ES302 後接續 ES404、ES403、ES203、ES202、ES104 均已完成；目前清冊下一未完成項目 ES304。

## 公司端最新交付：ES402 設備狀態記錄查詢（2026-11-23）

- [x] 依 THEIL `frmEquipmentStateLog`（Designer 內嵌）／Gantt 子窗、`wsEMS.LoadEquipmentStateLog`、Core `clsEMSEquipment.LoadEquipmentStateLogJoinStateBasis` 核對日期與設備／狀態／使用者條件、Lot／OP 隱藏欄、狀態色及查詢排序。
- [x] 完成 `/YMES/EMS/EquipmentStateLog` DEV 假資料頁：勾選式 lookup、四個可選日期邊界、查詢結果表、設備狀態 Gantt；支援 `strEquipmentNo` query string 深連結。新增 [ES402 規格](docs/modules/EMS/設備狀態記錄查詢.md)、索引與家用端 handoff。
- [x] DEV 僅用虛構資料與頁面記憶體，不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫入；未執行 build、test 或 browser。
- [x] Oracle 結構引用沿用 WP305／設備狀態 basis references；為既有 Oracle source `TBLWIPLOTLOG` 新增 PG CREATE-only 參考，未執行 migration。受保護 Oracle profile 不可用，本次未連 Oracle、未查業務資料或 YMES PostgreSQL。
- [ ] 家用端先比對四張來源表 live schema／migrations，尤其 `TBLWIPCONT_EQUIPMENT` 無 PK；以登入者 ES402 權限做唯讀 API，參數化所有 filters、限制日期跨度與結果數、採完整 EquipmentNo＋StartTime＋EndTime 關聯並分頁。Oracle／非 Oracle legacy join 有 EndTime 條件差異；不要照搬 Core 字串 SQL。
- [x] ES402 完成後接續 ES302、ES404、ES403、ES203、ES202、ES104；目前均已完成公司端 DEV／規格，清冊下一未完成項目為 ES304。

## 清冊交接：WPI05 原始來源缺失，跳過後接續 EMS 清冊（2026-11-23）

- [x] 查驗 `D:\iMES\CSiMES`、`D:\TP-iMes\iMES_THEIL_TP`、`D:\TP-iMes\DEV_SILVA`，未找到 WPI05 `frmEQPAccessoryBasis`／其專案或直接 Service/Core；EP203 配件主檔、ES 配件上機與 CP Setup 配件查詢不是同一功能。詳見[來源狀態](docs/modules/WIP/配件領用單維護作業.md)。
- [ ] 取得缺少的 WPI05 Form／Designer、Service、Core 後，再按來源復刻；公司端 Oracle 僅在受保護 profile 可用時唯讀查 metadata，不猜欄位／DDL。
- [x] 跳過無法由現有來源核實的 WPI05；ES402、ES302、ES404、ES403、ES203、ES202、ES104 均已完成公司端 DEV／規格，現在接續清冊下一未完成項目 ES304。

## 公司端最新交付：WPP08 P_LOT COMP GEN／生產批自主產生元件（2026-11-23）

- [x] 依 THEIL `frmLotComponentGen`／`frmNewComponent` 與 Designer、CP `wsWIP.LoadComponentBasis`／`LoadComponentState`／`UpdComponent`、共用 `wsWIP`／`wsSYS`／`wsPRD` 及 Core `clsLot.UpdComponent` 核對查詢、重複阻擋、流程資格、序號、Unit transfer、Wafer ID、GoodQty 總量及三表新增契約。
- [x] 完成 fallback `/YMES/WIP/LotComponentGen` DEV 頁：MOState 3／6、LotState=0、Lot 查詢／排序、Component State、ComponentBasis 重複阻擋、HaveComponent／Enterprise gate、兩種假序號模式、Unit conversion、Wafer ID range、逐列 GoodQty 與 SumQty 守恆；新增 [WPP08 規格](docs/modules/WIP/生產批自主產生元件.md)、索引及家用端 handoff。
- [x] DEV 只操作固定虛構 fixtures 與頁面記憶體；不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫檔或預約正式序號。未執行 build、test 或 browser。
- [ ] 三張新增表已有先前 Oracle／CREATE-only references：`TBLOEMOCOMPONENT`（OE201）、`TBLWIPCOMPONENTBASIS`（WP215）、`TBLWIPCOMPONENTSTATE`（WP101）；本機受保護 Oracle profile 不可用，本次沒有查 MESTEST 新 metadata。家用端先比對 live schema／migration，不重建共用表。
- [ ] 正式 API 需重新檢查 MO／Lot／流程、既有 Component State／Basis、單位換算、逐筆 Wafer ID／GoodQty、總量守恆、序號唯一性與登入者授權；三表新增需原子交易、冪等與併發守門。Source Core SQL 為字串串接，ComponentNo auto source 呼叫 `ReserveSerialNo=false`，需設計安全正式序號預約。
- [ ] WPP02、WPP04、WPP08 均已完成公司端 DEV／規格／handoff。原始 FunctionOrder 未保存在可讀清冊；安全 Oracle profile 可用時再核實精確排序。

## 公司端最新交付：WPP04 P_PROBING LOG／晶圓針測紀錄查詢（2026-11-23）

- [x] 依 THEIL `frmProbingLog`（Designer 內嵌）、`IC_CP_PKG.wsWIP.LoadProbingLog_Ext`、`clsLot.LoadProbingLog_Ext` 核對 Customer／Lot／MO 至少一項、OP 多選、Component、EndTestTime、Final／History、動態 BIN 欄與 WIP 子視窗自動查詢。
- [x] 完成 fallback `/YMES/WIP/ProbingLog` DEV 唯讀查詢頁：篩選勾選／Customer lookup／OP 多選、CurVersion 1 Final／all History、EndTestTime、動態 BIN 與 BIN101+、Core Component filter 缺陷註記；新增 [WPP04 規格](docs/modules/WIP/晶圓針測紀錄查詢.md)、索引及家用端 handoff。
- [x] DEV 只呈現固定假資料與本機記憶體查詢紀錄，不呼叫 API／Service、不連 Oracle／YMES PostgreSQL、不寫資料或匯出檔。未執行 build、test 或 browser。
- [ ] `tbl_CPWIPWAFERBINSUMMARY`／`tbl_CPSYSBin`／`tbl_PWIPWaferBin_Ext` 同版 Oracle DDL／metadata 缺項；桌機受保護 Oracle profile 不存在，未查 MESTEST／未猜 CREATE-only。家用端先比對 YMES live schema／migration。
- [ ] 正式 API 需限制登入者 Customer／Lot／MO 可見範圍、參數化條件、修正 ComponentNo 重複字串 bug；保留 Final=CurVersion 1、History 不限版、OP 多選、EndTestTime 日期上限、SYS parameter 決定 BIN extension。
- [ ] WPP02、WPP04、WPP08 均已交付；原始 FunctionOrder 未保存在可讀清冊，桌機受保護 profile 不存在；後續可用安全 profile 時再核實精確順序。

## 公司端最新交付：WPP02 P_WAFERMAP EXPORT／晶圓針測資料匯出（2026-11-23）

- [x] 依 THEIL `frmWIPWaferExport`（Designer 內嵌）、`IC_CP_PKG.wsWIP`／`wsSYS`、`clsLot`／`clsSYSEng`、`clsWaferExportHanlder` 核對 Lot／MAP／Bin 查詢、Basis wildcard 選擇、FunctionFlag 0／1、Wafer 多選、Detail／Header、檔名 token、MAP／文字／Excel／ZIP 輸出與 FTP 路徑分支。
- [x] 完成 fallback `/YMES/WIP/WaferMapExport` DEV 假頁：Lot 查詢、設定匹配、Manual／Automatic、Wafer 選取、格式明細、Set Folder、重複 SlotNo+OPNo／缺 Bin Define 情境與輸出 manifest；新增 [WPP02 規格](docs/modules/WIP/晶圓針測資料匯出.md)、索引並更新家用端 handoff。
- [x] DEV 只操作虛構 fixtures 與本機操作紀錄；不建立本機檔案／Excel／ZIP、不呼叫 legacy Service／API／FTP／File Service、不連 Oracle／YMES PostgreSQL。未執行 build、test 或 browser。
- [ ] `tblSYSWaferExportBasis`／`tblSYSWaferExportDetail`／`tblSYSWaferExportHeader` 與 CP `TBL_CPWIPWAFERMAP`／Bin Summary／Bin Define 同版 Oracle DDL／metadata 缺項。桌機受保護 Oracle profile 不存在，未猜欄位或 CREATE-only；家用端先核 live schema／migration 再定 schema/API。
- [ ] 家用端核實 Lot 權限、設定 wildcard precedence、FunctionFlag、Ship MAP／Combo MAP OP、Bin mapping／Header、輸出格式 allowlist 與 server-side 檔案生成；FTP credential、遠端目錄由受保護 server 設定管理，不接受 browser 任意路徑。
- [ ] WPP02、WPP04、WPP08 均已交付。原始 FunctionOrder 未保存在可讀清冊，桌機 Oracle profile 不可用；有安全 profile 時再核實實際排序。

## 家用端補件：WPP05 Golden MAP 原生資料與 File Service 契約（2026-11-23）

- [ ] 以受保護的 Oracle profile 唯讀確認權威 MES owner／schema 與 synonym，並查 `TBL_PWIPGoldenMAPFileState`、`TBL_PWIPGoldenMAPFileLog` 是否存在；如不存在，記錄查詢範圍與結果，不從 INSERT／SELECT 欄位推測表結構。
- [ ] 對上述兩表提供同一 MESTEST／MES 版本的 Oracle `DBMS_METADATA` CREATE DDL，以及欄位 metadata（型別、BYTE／CHAR 長度、nullable、default、comment）、PK／UK／FK、index、trigger 與 sequence（若有）。需確認 State 的 `(ProductNo, ProductVersion)` 唯一性、Log 的識別鍵與 `UpdGoldenMAPFileLog` 更新條件；不得帶回業務資料列或把 secrets 寫進 repo。
- [ ] 補 `wsWIP.LoadGoldenMAPFileState`／`LoadGoldenMAPFileLog`／`UpdGoldenMAPFileState`／`AddGoldenMAPFileLog`／`ScrapGoldenMAPFile`／`UpdGoldenMAPFileLog` 的實際 SQL／交易邊界與各步驟回傳語意；特別說明 State 刪後重建、Log 追加／更新及失敗時是否 rollback。
- [ ] 補 `wsSYS.LoadParameter` 的 File Service 設定參數編號／名稱、解析型別及缺值／錯誤行為（File Service type、host、port、user、password、Golden MAP state path、backup path）。只交參數定義與讀取流程，不交實際 credential／secret 值。
- [ ] 找出可供正式 API 使用的受支援 File Service/provider 介面：上傳、下載、存在檢查、刪除、移動／備份、overwrite 與錯誤／timeout 回應格式；補服務端實際部署位置／相依元件及設定來源，不提供明文連線密碼。若沒有可重用 provider，明確註明需新建的服務端元件與安全設定位置。
- [ ] 確認並記錄來源 Form 將 DeviceNo XML 參數下一行覆蓋成 ProductNo 的缺陷；由家用 API 按規格實作 DeviceNo→ProductNo 查詢，不需公司端改原型或重交假資料。

## 公司端最新交付：WPP05 P_GOLDENMAP FILE MANAGEMENT／Golden MAP檔案管理（2026-11-23）

- [x] 依 THEIL 主／Update 子 Form、IC_CP_PKG.wsWIP、clsLot 核對 DeviceNo 參數被 ProductNo 覆蓋的來源缺陷、Product／UploadTime 現況與歷程查詢、Org File Update／Assign Product Info、逐檔上傳、backup／Scrap／下載及其檔案服務參數。
- [x] 完成 DEV fallback /YMES/WIP/GoldenMapFileManage：假資料 State／History、多選、產品 fixture 選擇、DeviceNo／ProductNo checkbox 預期查詢、檔名／副檔名驗證、更新替換／歷程、Scrap、下載模擬；新增 Golden_MAP檔案管理.md 並更新索引。
- [x] 僅操作固定 DEV fixtures 與 localStorage；不讀／寫本機檔案、不呼叫 legacy Service、Oracle／YMES PostgreSQL、FTP 或正式 File Service。未執行 build、test 或 browser。
- [ ] WPP05 專屬 TBL_PWIPGoldenMAPFileState／TBL_PWIPGoldenMAPFileLog 同版 Oracle DDL／metadata 缺項；桌機受保護 Oracle profile 不存在，未猜欄位或 CREATE-only。共享 TBLPRDPRODUCTBASIS 參考 DDL／metadata 沿既有 references。
- [ ] 家用端先核對兩張 Golden MAP 表的 PostgreSQL live schema／migration，再設計登入保護、參數化的 State／History／Product lookup 與逐檔 upload／scrap／download API。File Service 路徑／憑證由 server 受保護設定管理，逐檔補冪等、部分成功回報及 file／DB 對帳。
- [ ] CP 清冊 WPP02、WPP04、WPP08 已交付，原始 FunctionOrder 尚待有安全 Oracle profile 後核實。

## 公司端最新交付：WPP03 P_EQP SETUP BASIS／設備設置單維護_晶片測試（2026-11-22）

- [x] 依 THEIL CP 主 Form（內嵌 Designer）、ADD／EDIT／CLOSE Def Form、VIEW Form、`IC_CP_PKG.wsWIP` 與 `clsLot` 核對狀態 0／1／2／99、`SFN` 序號、PROBER／TESTER、Accessory Release、配件實物確認、設備使用守門及 Link State／Log 更新。
- [x] 完成 fallback `/YMES/WIP/EqpSetupBasisCp` DEV 頁：SetupNo／設備／RunCard／OP／日期／狀態查詢，九欄 Grid，ADD／EDIT／VIEW／CLOSE、配件數量與 Site 驗證、設備使用中阻擋及前一單歸檔模擬；新增 [WPP03 規格](docs/modules/WIP/設備設置單維護_晶片測試.md)、索引及家用端 handoff。
- [x] DEV 僅本機 `localStorage` fixtures；不呼叫 MES Service／API，不連 Oracle／YMES PostgreSQL，不寫序號、Setup、設備關聯或 EMS 配件狀態。未執行 build、test 或 browser。
- [x] 家用端修正合併後 build 揭露的重複 `status` 宣告，完整 Vite build 通過；Chrome 畫面擷取逾時，尚未完成操作驗收，未部署。
- [ ] WPP03 專屬 Setup、Accessory、Equipment Link 表及 `TBLPRDACCESSORYGROUP_SUB`／`TBLEMSACCVIOLATESTATUSCHANGE` lookup source DDL／metadata 缺項；serial、WIPTemp equipment、EMS accessory state/control 沿用 WP109／WP201／accessory references。桌機受保護 Oracle profile 不存在，未猜 CREATE-only SQL。家用端先比對 live schema／migration 與同版 Oracle metadata，再設計正式 API／schema。
- [ ] 家用端正式 CLOSE API 須原子處理設備 Lot 使用守門、原關聯結束／歸檔、新關聯歷程、Setup Status=2／CloseDate／CloseUser 與 EMS accessory state；ADD／EDIT 須同步保護 serial、Status=0 及外部配件發料競爭。
- [x] WPP05、WPP02、WPP04、WPP08 已接續完成，原始 FunctionOrder 尚待安全 profile 核實。

## 公司端交付：WPP01 P_CREATE LOT_CP／生產批開立_晶片測試（2026-11-22）

- [x] 直接核對 CP 主／定義 Form（含 Designer）、`IC_CP_PKG.wsWIP`／共用查詢與 `MEStc_IC_CP_PKG.clsLot`；補充 `IC_FT_PKG` Delete／非強制挑片 FT 子窗分支、序號、元件、ECN、Property、Carrier 與自動下線條件。
- [x] 完成獨立 Vue DEV 頁 `/YMES/WIP/LotBasisCreateCp`：MO／Lot 查詢、19 欄清單、Add／Edit／Copy／多選 Delete／Release、ECN／流程唯讀／元件／Property／Carrier 互動及部分失敗情境；新增 [WPP01 規格](docs/modules/WIP/生產批開立_晶片測試.md)並更新索引與家用端 handoff。
- [x] DEV 僅 localStorage 假資料；不連 Oracle／YMES PostgreSQL、不呼叫 Service／API、不執行序號、扣料、StartLot、CloseLot、載具或自動報工；未執行 build、test 或 browser。
- [ ] 未有本機受保護 Oracle profile，本次未查 WPP01 相關 metadata、未讀業務資料、未猜 CREATE-only。沿用 WP101／WP207／WP215／WP302／OE references；`TBLWIPLOTBASIS` 舊 WP101 DDL 與 8ff lookup 有已知 38／39 欄、DESCRIPTION 長度及 `XX_MARKING` 差異，家用端須先核目標 schema。
- [x] 家用端只讀核對 YMES catalog：上述 WP101／WP207／WP215／WP302／OE 共用 Lot／Component／ECN／Carrier／MO 表目前都不存在，`TBLSYSSERIALNO` 存在；沒有讀業務資料或執行 DDL/DML。
- [ ] 家用端先唯讀比對 live schema／migration，再規劃 CP MO／Lot／ECN／Component／Carrier／Property 查詢與新增、編輯、逐批刪除／下線 API；鎖定 MO 未下線量與元件預約，明確逐筆交易及部分成功結果。
- [x] 後續依清冊已完成 WPP03「P_EQP SETUP BASIS／設備設置單維護_晶片測試」；現在接續 WPP05。

## 公司端最新交付：WPP06 Bump MAP 檔案管理（2026-11-22）

- [x] 核 THEIL 主 Form／內嵌 Designer、Update File 子 Form、`wsWIP`、`wsSYS`、`clsLot`、`clsSYSEng` 及 `clsMergeMAPHandler`；記錄目前／歷程查詢、MAPType=1 template、encoding／header／尺寸檢查、逐檔上傳備份、Scrap、下載與事件紀錄流程。
- [x] 完成 DEV 假資料頁：Lot／UploadTime 查詢、State／History 多選、假 template／`.txt` file chooser、解析提示、逐檔 upload／Scrap、備份續號、下載目的地與部分失敗情境；新增 [WPP06 規格](docs/modules/WIP/Bump_MAP檔案管理.md)、索引及家用端 handoff。
- [x] fallback `/YMES/WIP/BumpMapFileManage` 僅 Vue 記憶體 fixtures；不連 Oracle／YMES PostgreSQL／legacy Service／FTP(S)、不讀寫任何本機檔案。未執行 build、test 或 browser。
- [ ] 直接表為 `TBL_PWIPBumpMAPFileState`／`TBL_PWIPBumpMAPFileLog`；template 依賴 `TBL_BSYSMAPTEMPLATEBASIS`／`TBL_BSYSMAPTEMPLATEDETAIL`。本機受保護 Oracle attachment/profile 不可用，未查 DDL／metadata，未猜 CREATE-only。家用端比對 live schema 並設計參數化 API、server-side file root、冪等與跨檔案／DB reconciliation。
- [x] 家用端已以只讀 catalog 核實以上四表均不存在；repo 沒有相應 DDL／正式 File Service API。未讀業務資料或執行 DDL/DML，需先取得同版 Oracle metadata 與 File Service 介面。
- [x] 後續清冊項目 WPP01「P_CREATE LOT_CP／生產批開立_晶片測試」已完成；現在接續 WPP03。

## 公司端最新交付：WIPUNINV 取消入庫作業（2026-11-22）

- [x] 依 THEIL `frmWP_UNINV_THEIL_BU1.vb`／Designer、`WEBAPI.vb` 核對自動 Search、12 欄單選 Grid、欄位改名、Outbound request、`GRSNO` 預設 `0001` 與成功後重查。
- [x] 完成純 DEV 假資料頁及 Search／Outbound 成功、失敗、逾時對帳互動；新增 [WIPUNINV 規格](docs/modules/WIP/WIPUNINV取消入庫.md)、索引及家用端 handoff。Outbound 依來源直接送出，未加 WinForms 沒有的確認視窗。
- [x] fallback `/YMES/WIP/UnInventory` 僅本地 fixture；不呼叫 SAP／Web API／MES Service／資料庫／ERP。未連 Oracle／YMES PostgreSQL，未執行 build、test 或 browser。
- [ ] 可讀 THEIL／DEV_SILVA checkout 只有 Form／API consumer，未找到 GetPendingList／Outbound controller、Service、Core、SAP 查詢與交易；API 未提供直接 Oracle 表名或 SQL，沒有可查的 Oracle DDL／metadata 對象。家用端確認部署 endpoint、授權、冪等與逾時結果對帳。
- [x] 後續清冊項目 WPP06「P_BUMPMAP FILE MANAGEMENT／Bump MAP 檔案管理」已完成；最新接續項目見本檔最上方。

## 公司端最新交付：BARTENDER Bartender print（2026-11-22）

- [x] 依 THEIL `frmWP_BartenderPrint_THEIL_BU1.vb`（主 Form／Designer 內嵌）、`frmWP_BartenderPrint_Form.vb`／Designer、`ApiClient`／`ApiExecResult` 核對 Lot、標籤選項、動態列印資料、預覽／送印、TAG_FILE_MAP 設定及工具頁流程。
- [x] 完成 DEV 假資料標籤查詢、規則選擇、逐筆／全選、動態 Grid、對照設定編輯與驗證、假標籤分頁預覽、列印確認及本頁狀態紀錄；新增 [BARTENDER 規格](docs/modules/WIP/Bartender列印.md)、索引與家用端 handoff。
- [x] DEV fallback `/YMES/WIP/BartenderPrint`。不呼叫 SqlExecApi／InLabel／LabelApi、資料庫、SFTP、BarTender、檔案系統或印表機；不下載、解壓、安裝或執行 vendor 二進位檔。未執行 build、test 或 browser。
- [ ] 可讀 THEIL checkout 只有 API consumer；未找到 InLabel／LabelApi server handler 或 `GETLPC`／`GETXXQCLABEL` SQL／Core 實作。`TBLINLABEL_TAG_FILE_MAP` Oracle DDL／metadata 缺項，桌機沒有受保護 Oracle profile，未連 Oracle／YMES PostgreSQL，也未猜 CREATE-only。家用端先核 live schema／API 身分與權限，再決定正式列印 provider。
- [x] 家用端確認 repo `src/` 沒有 InLabel／LabelApi server handler，YMES live catalog 沒有 `TBLINLABEL_TAG_FILE_MAP`；未查業務資料或執行 DDL/DML。
- [x] 後續清冊項目 WIPUNINV「取消入庫作業」已完成；最新接續項目見本檔最上方。

## 公司端最新交付：INV COUNT STATE 盤點關／開帳（WP606，2026-11-22）

- [x] 依本機 `D:\iMES\CSiMES\STD\MESWin\WIP\WP_InvCountState\frmInvCountState.vb`（Designer 內嵌）、`wsWIP.LoadInvCountState`／`ExeInvCountClose`／`ExeInvCountOpen`／`ChkInvCountAdjust`、`clsINVCount`／`clsSYSParameter` 核對 State=0／1、六種 Close Option 快照與批次過站鎖。
- [x] 完成 Vue DEV 假資料關帳／開帳、六類快照選擇、未 Confirm 調帳阻擋、失敗復原情境；新增 [WP606 規格](docs/modules/WIP/盤點關開帳.md)、索引與家用端 handoff。記錄 AllowExecuteLot 參數與 State 寫入先後造成的失敗恢復風險，以及伺服器未檢查至少一項 Close Option 的缺口。
- [x] 路由 `/YMES/WIP/InvCountState` 僅 DEV fallback，不呼叫資料庫或 MES Service。沿用 WP608 四張盤點 state/detail 表，Oracle DDL／metadata 缺項且桌機無受保護 Oracle profile，未猜 CREATE-only。未執行 build、test 或 browser。
- [x] 下一個尚未復刻功能 BARTENDER「Bartender print」已完成；接續項目見本檔最上方及功能清冊。

## 公司端最新交付：INV COUNT PRINT 列印盤點清冊（WP607，2026-11-22）

- [x] 依本機 `D:\iMES\CSiMES\STD\MESWin\WIP\WP_InvCountStatePrint\frmInvCountStatePrint.vb`（Designer 內嵌）、`wsWIP.LoadInvCountStateWithDetail`、`clsINVCount` 及 General／MTL／PRD ActiveReports，核對 State=0 六類查詢、欄位／排序、BarCode 預設值與六種報表選擇。
- [x] 完成 Vue DEV 假資料查詢、WIP／SEMIWIP／FGD／SEMI／RAW／MTLWIP Grid、條碼／無條碼報表切換及列印預覽；新增 [WP607 規格](docs/modules/WIP/盤點清冊列印.md)、索引及家用端 handoff。
- [x] 路由 `/YMES/WIP/InvCountStatePrint` 僅供 DEV fallback，不呼叫 ActiveReports、資料庫或印表機。未增加資料表；WP608 四張盤點 state/detail 的 Oracle DDL／metadata 仍缺、桌機無受保護 Oracle profile。沒有執行 build、test 或 browser。
- [x] 後續 WP606「盤點關／開帳」已完成；之後跳過已完成清冊項目，接續下一個未復刻功能。

## 公司端最新交付：INV COUNT ADJUST 盤點調帳（WP608，2026-11-22）

- [x] 依 `frmInvCountStateAdjust`、三個數量調整 Dialog、`wsWIP.LoadInvCountStateWithDetail`／`ExeInvCountAdjust`／`ExeInvCountConfirm`、`clsINVCount` 完成六種 WIP／SEMIWIP／FGD／SEMI／RAW／MTLWIP 流程核對。
- [x] 完成 Vue DEV 假資料查詢、New CurQty／AdjustQty 暫存、逐筆 Confirm、部分完成失敗模擬及 General／MTL／PRD 報表預覽；新增 [WP608 規格](docs/modules/WIP/盤點調帳.md)、索引與家用端 handoff。修正 MTL request 欄位名稱缺陷的 DEV 行為，記錄舊 Form 的兩處 MaterialLotNo typo。
- [x] WIP 路由 `/YMES/WIP/InvCountStateAdjust` 僅在 DEV fallback 顯示，不呼叫 Service／Oracle／PostgreSQL／印表機。四張盤點 state/detail 表無 Oracle DDL／metadata reference；無受保護 Oracle profile，未猜 CREATE-only。沒有執行 build、test 或 browser。
- [x] WP606「盤點關／開帳」與 WP607「列印盤點清冊」均已完成；接續項目見本檔最新交付及功能清冊。
- [x] 家用端已以只讀 catalog 核實 YMES PostgreSQL 缺少四張盤點 state/detail 表；未查業務資料列，也未執行 DDL/DML。
- [ ] 待桌機取得受保護 Oracle profile 後唯讀取四表 `DBMS_METADATA` table/index DDL 與 columns／constraints／indexes／triggers metadata，並補 `clsINVCount` 中 RAW／MTLWIP 目標表的精確 SQL 映射；來源未確認前不可猜 CREATE-only。

## 公司端最新交付：FGDSTOCKIN_WF_CANCEL 取消入庫 WorkFlow ERP（WP619，2026-11-22）

- [x] 核對取消 Form／Designer、`wsINV`／`kcINV.clsInventory`、EAI `wsMES`／`clsINV`；確認查詢 State=2／ERPNo 非空，送 WorkFlow `ReferenceCase=4` Undo，MES Core 回復 FGD／SCRIN 庫存與狀態 1→-2→3→-1。
- [x] 完成 Vue DEV 假資料候選清單、FGD／SCRIN 明細、WorkFlow Undo success／fail／timeout 與 MES 回復 success／fail；列表保留混合 ERP 來源並阻擋非 WorkFlow。記錄舊查詢無 source filter、timeout 被當明確失敗回 State=2、MES failure 停 State=-2、`UpdINVInfo` 無狀態條件。
- [x] 新增 [WP619 規格](docs/modules/WIP/取消入庫WorkFlow_ERP.md)、規格索引、清冊進度及家用端 handoff。沿用 WP201／WP603／WP605；`TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` metadata 缺項沿 WP622。桌機沒有可用受保護 Oracle profile，未連 Oracle、未猜 CREATE-only；未連 YMES PostgreSQL。
- [x] 原 Function 未掛正式 MenuItem；DEV fallback `/YMES/WIP/FgdStockInWorkFlowCancel`，不呼叫 ERP、MES Service 或資料庫。未執行 build、test 或 browser。
- [x] 後續 WP608「盤點調帳」已完成，下一項依清冊為 WP607「列印盤點清冊」；進度見本檔最上方及功能清冊。

## 公司端最新交付：FGDSTOCKIN_WF 入庫至 WorkFlow ERP（WP614，2026-11-22）

- [x] 依主 Form／Designer、`frmFGDStockIN_ReThorwERP` 重拋 Form／Designer、EAI `wsMES`、`clsWIP`／`clsINV` 核對 ALLOWINV2ERP 0／1／2、MO／多 Lot、多 MO、Good／Scrap／Loss、ERP XMLAdapter、MES rollback、timeout ticket 及 retry flow。
- [x] 完成 Vue DEV 假資料雙 Grid、MO 3／6 查詢、保留跨 MO 勾選、FGD／Locator、成品／Scrap 列、ERP／MES success／fail／timeout 與重拋成功／失敗／再逾時情境。Loss-only 會提示來源缺口並阻擋，模式 2 顯示 SheetType 舊程式解析缺陷。
- [x] 新增 [WP614 規格](docs/modules/WIP/入庫至WorkFlow_ERP.md)、索引、清冊進度及家用端 handoff。沿用 WP201／WP603／WP605／OE／PRD refs；`TBLINVWIP_SCRAP`、`TBLOEMOMATERIALINBASIS_ERP`、`TBLERPRETHROWTICKET` metadata 缺項。此桌機無可用受保護 Oracle profile，未連 Oracle、未猜 CREATE-only；未連 YMES PostgreSQL。
- [x] 原 Function 未掛正式 MenuItem；DEV fallback 為 `/YMES/WIP/FgdStockInWorkFlow`，不呼叫 WorkFlow SOAP、MES EAI Service 或資料庫。未執行 build、test 或 browser。
- [x] 後續 FGDSTOCKIN_WF_CANCEL（WP619）已完成；後續狀態見最上方最新交付。

## 公司端最新交付：FGDSTOCKIN_TP_CANCEL 取消入庫至 TipTop ERP（WP610，2026-11-22）

- [x] 依 Cancel Form／Designer、`wsINV`、TipTop `UndoCreateStockInData`、`clsInventory` 核對 State=2／ERPNo 守門、工單 MOState=99 檢查、FGD／SCRIN MES 庫存回復、Good Bin、Component、Lot Qty 與狀態轉移。
- [x] 完成 Vue DEV 假資料取消頁及 FGD／SCRIN、MO 已結案、ERPNo 空白、TipTop／T100／E10ICD 來源、ERP 失敗、MES 失敗及成功情境。原 Form 查詢未篩 ERP source，DEV 保留異來源列並阻擋跨 ERP 取消。
- [x] 新增 [WP610 規格](docs/modules/WIP/取消入庫至TipTop_ERP.md)、規格索引、清冊進度及家用端冪等／source guard／State=-2 handoff。沿用 WP201／WP603／WP605／WP215 references；`TBLINVFGDINBIN` Oracle DDL／metadata 缺項沿 WP621／WP623。桌機無可用受保護 Oracle profile，未連 Oracle、未猜 CREATE-only；未連 YMES PostgreSQL。
- [x] 原 Form 未掛正式 MenuItem；DEV fallback 為 `/YMES/WIP/FgdStockInTpCancel`，不呼叫 TipTop SOAP、MES Service 或資料庫。
- [x] 後續 FGDSTOCKIN_WF（WP614）已完成；依清冊接續 FGDSTOCKIN_WF_CANCEL（WP619）。

## 公司端最新交付：FGDSTOCKIN_TP 入庫至 TipTop ERP（WP609，2026-11-21）

- [x] 依 Form／Designer、TipTop `CreateStockInData`、EAI `wsMES`、`clsWIP`／`clsINV` 核對 Sheet Type、MO／Ref Lot、來源／目的庫、可部分入庫與 Core 更新。確認來源程式只對整批 Lot 設 BookingFlag；部分 Lot 並無 reservation。
- [x] 完成 Vue DEV 假資料雙 Grid、MO／Ref Lot、掃描 Select／UnSelect、InInvNum 驗證、Component／Good Bin 部分入庫守門、ERP code／MES／ERP Undo 成敗情境。TipTop pack Lot ERP qty=SysQty，MES qty=InInvNum。
- [x] 新增 [WP609 規格](docs/modules/WIP/入庫至TipTop_ERP.md)、索引、清冊進度與家用端交接。Good Bin active `MOStockIn_TP` Core 未找到更新語句，列為來源待確認；ERP Undo 失敗後仍可能釋放 BookingFlag。
- [x] 沿用 WP605／WP201 已有 Oracle DDL／CREATE refs，沒有新增缺表；未做重複 CREATE-only，未連 Oracle 或 YMES PostgreSQL。
- [x] 後續 FGDSTOCKIN_TP_CANCEL（WP610）及 FGDSTOCKIN_WF（WP614）已完成；後續狀態見最上方最新交付。

## 公司端最新交付：FGDSTOCKIN_T100_CANCEL 取消入庫 T100 ERP（WP620，2026-11-21）

- [x] 依 Form／Designer、`wsINV`、`wsERP_T100`、`clsInventory` 核對取消查詢、ERP 呼叫、Core 庫存回復及狀態轉移。查詢只篩 State=2，未篩 ERP source；Confirm 有 State／ERPNo 守門但仍可能將非 T100 單送往 T100，已在規格及 DEV 明確呈現並阻擋跨 ERP 模擬取消。
- [x] 新增 [WP620 規格](docs/modules/WIP/取消入庫T100_ERP.md)、索引、清冊進度及家用端 source guard／冪等／unknown outcome handoff。沿用 WP201／WP603／WP605／WP215 references；`TBLINVFGDINBIN` metadata 缺項沿 WP621／WP623，桌機無可用受保護 Oracle profile，未連 Oracle 或 YMES PostgreSQL、未猜 CREATE-only。
- [x] DEV 頁模擬 FGD／SCRIN RequestType 0／1、ERP code=0 成功、ERP 失敗還原 State=2、MES 失敗保留 State=-2 及成功取消；Function 未掛正式 MenuItem，fallback 路由為 `/YMES/WIP/FgdStockInT100Cancel`。
- [x] 後續 FGDSTOCKIN_TP（WP609）與 FGDSTOCKIN_TP_CANCEL（WP610）已完成；後續狀態見最上方最新交付。

## 公司端交付：FGDSTOCKIN_T100 入庫至 T100 ERP（WP616，2026-11-21）

- [x] 依 Form／Designer、`wsERP_T100`、`wsMES`、`clsWIP`／`clsINV` 核對 DocType=1、InventoryType 1／6、MO 多選、Lot Select／UnSelect、Reservation、ERP `RequestType=0` 與 T100／MES 流程。
- [x] 完成 Vue DEV 假資料雙 Grid、掃描選／取消、全選／清除、完整 Lot Qty、ERP W warning 續行、ERP fail、MES fail 後 ERP cancel success／fail 情境。
- [x] 重用 WP201／WP605 已有 Oracle DDL／CREATE references；沒有新增缺表。此桌機無可用受保護 Oracle profile，未連 Oracle、未做 CREATE-only，也未連 YMES PostgreSQL。
- [x] 新增 [WP616 規格](docs/modules/WIP/入庫至T100_ERP.md)、索引與家用端 ERP／schema handoff。記錄現行路徑未序列化區域變數 `auto=N`，以及 ERP 取消失敗仍釋放 BookingFlag 的補償風險；Function 原生未掛 MenuItem，DEV fallback 有標示。
- [x] 後續 FGDSTOCKIN_T100_CANCEL（WP620）、FGDSTOCKIN_TP（WP609）及 FGDSTOCKIN_TP_CANCEL（WP610）已完成；後續狀態見最上方最新交付。

## 公司端最新交付：FGDSTOCKIN_E10ICD_CANCEL 取消入庫 E10ICD ERP（WP623，2026-11-21）

- [x] 依取消 Form／inline Designer、`wsINV`、`wsERP_E10ICD`、`clsInventory` 核對 State=2+ERPNo 守門、良品／不良品 ERP RequestType、ERP code=0、MES 整單庫存回復及狀態流程。
- [x] 完成 Vue DEV 假資料互動頁：查詢可取消單據、良品／不良品明細、Component／動態 Bin，ERP 取消失敗還原、ERP 成功後 MES 回復成功／失敗情境。
- [x] 記錄舊流程 `OrgState` 會被 `(2,1) → (1,-2) → (-2,-1)` 覆寫；ERP 已取消但 MES 失敗時留在 State=-2，正式端須提供恢復／對帳。
- [x] 重用 WP201／WP603／WP605／WP215 references；`TBLINVFGDINBIN` metadata 沿 WP621 缺項。無可用桌機受保護 Oracle profile，未連 Oracle、未猜 CREATE-only，也未連 YMES PostgreSQL。
- [x] 新增 [WP623 規格](docs/modules/WIP/取消入庫E10ICD_ERP.md)、索引與家用端取消冪等／補償 handoff。Function 原生未掛 MenuItem；DEV fallback 有標示。
- [x] FGDSTOCKIN_T100 WP616 已於本輪完成；後續狀態以公司端待辦最上方交付記錄為準。

## 公司端最新交付：SCRAPSTOCKIN_E10ICD 不良品入庫至 E10ICD ERP（WP622，2026-11-21）

- [x] 依 Scrap Stock In Form／inline Designer、`wsERP_E10ICD`、`wsMES`、`clsERP`、`clsWIP`、`clsINV` 核對 Source 0 Lot Scrap、1 Component Scrap、2 Fail Bin、原因明細、LotSerial 全量入庫、ERP→MES 及失敗取消／BookingFlag 回復。
- [x] 完成 Vue DEV 假資料互動頁：Source 類別篩選、LotSerial 勾選、BoxNo／DateCode、Error／Component／動態 Bin 明細，及 ERP/MES 成功、ERP W 非成功、ERP 失敗、MES 失敗補償情境。
- [x] 重用 WP201／WP603／WP215、QC／PRD／OE／OP／SMD references；缺 `TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` DDL／metadata 已記錄。無可用桌機 Oracle profile，未連 Oracle、未猜 CREATE-only，也未連 YMES PostgreSQL。
- [x] 新增 [WP622 規格](docs/modules/WIP/不良品入庫至E10ICD_ERP.md)、索引與家用端 schema／API handoff。Function 原生未掛 MenuItem；DEV fallback 有標示。
- [x] FGDSTOCKIN_E10ICD_CANCEL WP623 已於本輪完成；後續狀態以公司端待辦最上方交付記錄為準。

## 公司端最新交付：FGDSTOCKIN_E10ICD 入庫至 E10ICD ERP（WP621，2026-11-21）

- [x] 依 `frmInventory_FGDStockIN_E10ICD`／Designer、`wsERP_E10ICD`、`wsMES`、`clsERP`、`clsWIP`、`clsINV` 核對 MO/Lot 查詢、ERP 建單、MES 入庫、W 警告續行、ERP 補償取消與 BookingFlag 回復。
- [x] 完成 Vue DEV 假資料頁：MO／成品庫／儲位、批次勾選、InInvNum、條件式 BoxNo／DateCode、Bin／Component 明細及 ERP/MES 成功、W 警告、ERP 失敗、MES 失敗補償情境。
- [x] 重用 WP605／WP201／WP603 與 MO、Lot、產品、OP、Locator 的既有 Oracle／CREATE references；無可用桌機 Oracle profile，缺 `TBLINVFGDINBIN` DDL／metadata 已記錄，沒有猜 CREATE-only，未連 YMES PostgreSQL。
- [x] 新增 [WP621 規格](docs/modules/WIP/入庫至E10ICD_ERP.md)、規格索引及家用端 schema／API handoff。Function 原生未掛 MenuItem；DEV fallback 入口有標示。
- [x] SCRAPSTOCKIN_E10ICD WP622 已於本輪完成；後續狀態以本檔最上方交付記錄為準。

## 公司端最新交付：P_BUMP MAP MERGE Bump MAP 疊圖作業（2026-11-21）

- [x] 依 IC/CP Form、MAP Viewer、`wsWIP`／`wsPRD`／`wsSYS`、`clsLot`／`clsProduct`／`clsSYSEng` 核對 Lot、Component／Wafer、Golden／Bump MAP 狀態、IssueState=2 規則、Template Detail、Merge、UndoFrozen 及檔案流程；完成 Vue DEV 假資料互動頁。
- [x] 重用 `mestest_prd_cp_merge_map_*`、WP101 ComponentState、WP215 ComponentBasis 的既有 references；本項未連 Oracle、未查業務資料、未新增猜測 CREATE-only，也未連 YMES PostgreSQL。
- [x] 新增 [WPP07 規格](docs/modules/WIP/Bump_MAP疊圖作業.md)、索引與家用端 handoff；明列六張缺 Oracle DDL／metadata 的 CP MAP table、目前受保護 Oracle profile 不可用、Core 查詢參數串錯及真正二進位／FTP 合併由 DEV 示意簡化。
- [x] FGDSTOCKIN_E10ICD WP621 已於本輪完成；後續狀態以本檔最上方交付記錄為準。

## 前一項公司端交付：Skip Inspection Setting 免檢批設定（2026-11-21）

- [x] 依 THEIL 主 Form、新增／狀態視窗、`wsWIP`、`clsLot` 與 WP201 執行流程核對查詢、Lot×OP 新增、狀態往返及免檢原因提示；完成 DEV 假資料互動頁。
- [x] 重用 `XX_TBLSPCWAIVE` 的既有 Oracle source DDL／metadata 及 PostgreSQL CREATE-only reference；沒有查業務資料或新增 DDL，也未連 YMES PostgreSQL。
- [x] 新增 [免檢批設定規格](docs/modules/WIP/免檢批設定.md)及 WP201 查詢 handoff；FunctionKey 空白。

## 前一項公司端交付：WORKORDER PRINT 工令單列印（2026-11-21）

- [x] 依 THEIL Form／inline Designer、`wsWIP.LoadWorkorder`／`LoadMOTypeDesc`／`LoadLotDetail`、`clsLot` 與 ActiveReport 核對 MO／日期篩選、批次選取、同 MO 守門、報表欄位及數量彙總；完成 Vue DEV 假資料查詢與工令單預覽。
- [x] 重用 WP101、OE、PRD、OP／系統的既有 Oracle source DDL／metadata references；沒有查 Oracle 業務列或增加 CREATE-only，也未連 YMES PostgreSQL。
- [x] 新增 [WORKORDER PRINT 規格](docs/modules/WIP/工令單列印.md)及家用端唯讀 API handoff；記錄來源差異與 FunctionKey 缺漏。

## OFFICE最新：ES102／ES409／ES106 DEV交付已正常合併；接續來源原型工作（2026-11-21）

三個新EMS功能的WinForms來源整理、DEV原型與模組.md已整合至Home分支；Home正式Web284目前只呈現未開放guard，沒有宣稱正式API或資料庫已完成。office8e1的原生表單來源與原有三層選單仍由公司端繼續核對。

- [x] ES102設備狀態控制設定、ES409設備狀態期間統計查詢、ES106設備狀態變更原因資料DEV原型及各自維護.md已在正常merge 6cadf433…；正式站驗收範圍見 docs/qa/20261121-office8e1-web284.md。
- [x] 已依新的選單游標改從 WP916 後續接做；WP301、WP629、WP112 已完成並略過，當前最新狀態見本檔最上方。
- [ ] 補齊ES204原生來源細節：CHAR_USED／CHAR_LENGTH、互斥key、InitEQPSerialNo、Reverse／結算／簽退及Adapter行為。
- [ ] 補Lot39精確DDL與舊CREATE38差異的原生來源證據；不要以Home PostgreSQL推測Oracle結構。
- [ ] 公司電腦維持DEV UI／按鈕邏輯與原生來源核對角色；不連Home PostgreSQL、不做SSH部署。

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

## HOME最新執行中回報：ET202（2026-11-21）

HOME已完成API176／497653缺表相容測試及獨立26zero，完整備份後真建立0063 tblentositem原生11欄與CRUD4 grant，原結構保持；正接present API／Web276與app-browser。尚未本版Git／metadata結案，不把舊checkpoint33／schema0062當新live schema。詳[ET202 QA](docs/qa/20261121-et202-os-items.md)。Office繼續原生Oracle唯讀來源／UI；ET202 API／PG／SSH／發行由HOME自行完成，無須重做。最新LotBasis39欄精確DDL／metadata與原生流程來源補證仍請依下列待辦接續。

## 公司端最新交付：ES106 設備狀態變更原因資料（2026-11-21）

- [x] 依 `frmEquipmentReasonBasis`／`frmEquipmentReasonBasisDef`、`wsQC` 與 `clsQCBasis` 核對狀態篩選、EQP ReasonType=6、子類別查找、新增／編輯／複製／刪除／核准；完成 DEV 假資料互動頁。
- [x] 沿用既有 `TBLQCREASONBASIS`、`TBLQCREASONSUBTYPEBASIS` Oracle DDL/metadata 及 CREATE-only 參考；本項未查 Oracle 業務列，未連 YMES PostgreSQL，沒有新建表。
- [x] 新增 [ES106 規格](docs/modules/EMS/設備狀態變更原因資料.md)、索引和家用端狀態／簽核 API handoff；記錄 wsQC 傳 DataStamp／修訂欄但本機 Core method 未宣告／更新的來源差異。
- [x] 接續清冊完成 ES402 前先回到 WP916 後未完成清冊項目；最新順序見本檔最上方。

## 公司端最新交付：ES409 設備狀態期間統計查詢（2026-11-21）

- [x] 核對標準 Form/Designer、`wsRPT.LoadEQPRealTimeReport`／`LoadEQPRealTimeReport_Chamber` 與 `clsReport`；完成 DEV 假資料篩選、區間裁切、分鐘向上取整、結果表格和 Chamber 母子設備投影。
- [x] 重用設備狀態歷程／目前狀態、狀態 basis、設備 basis、區域映射的既有唯讀 Oracle DDL/metadata 參考；未查 Oracle 使用者資料，未連 YMES PostgreSQL，沒有新增 CREATE-only。
- [x] 新增 [ES409 規格](docs/modules/EMS/設備狀態期間統計查詢.md)、索引和登入保護／參數化唯讀 API handoff。
- [x] 接續清冊並完成 ES106「設備狀態變更原因資料」；下一個未完成項目 ES402「設備狀態記錄查詢」。

## 公司端最新交付：ES102 設備狀態控制設定（2026-11-21）

- [x] 依 `frmEQPStateControlSet.vb`（Form/Designer 同檔）、`wsEQP.LoadStateControlType`／`UpdStateControlType` 及 `clsEQPStateControl` 核對三分頁、七控制鍵、狀態 lookup、Apply／Confirm 與原批次 Delete+Add 寫入方式；完成 Vue DEV 假資料頁及 EMS設定選單路由。
- [x] 沿用 2026-11-16 已唯讀核實的 `TBLEQPSTATECONTROL` Oracle DDL、複合 PK `(CONTROLTYPE, CONTROLNO)` 及 PostgreSQL CREATE-only；ES102 沒有新增查詢或匯出 Oracle 結構，也未連 YMES PostgreSQL。
- [x] 新增 [ES102 規格](docs/modules/EMS/設備狀態控制設定.md)、索引及家用端原子批次更新 handoff。來源原批次逐列 Delete+Add 未證明交易，正式 API 應完整回滾。
- [x] 續接清冊並完成 ES409、ES106；下一個未完成項目 ES402「設備狀態記錄查詢」。

## 公司端交付：ES204 改變設備狀態（2026-11-21）

- [x] 依 THEIL Form（控制項配置內嵌主 VB）、STD wsEMS／clsEMSEquipment 核對設備查詢、互斥、原因、PM 工單守門、生產批 CheckInConfirm、狀態歷程與設備／批／人員副作用；完成 Vue DEV 假資料頁及路由。
- [x] PowerShell 5.1 + ODP.NET 唯讀取得 `TBLEMSEQUIPMENTREASONS` 欄位與 constraint/index/trigger metadata；`GET_DDL` 回 `ORA-03135`，沒有 Oracle 原始 DDL，CREATE-only 按字典重建且不猜 PK/FK。資料見 `database/reference/mestest_es204_equipment_reasons_metadata.txt`。
- [x] 規格與家用端原子 API／序號並發 handoff 已寫入 [ES204 規格](docs/modules/EMS/改變設備狀態.md)；公司端未連 YMES PostgreSQL、未做正式 API/schema。
- [x] 續接 ES102 後已完成 ES409、ES106；目前下一項為 ES402「設備狀態記錄查詢」。

## HOME本輪已結案並同步main（2026-11-21）

本輪已完成結案與main同步。current-checkpoint SHA `33bd353dd129962d2e2cf196aa5b628cdc4b176be1495e58972621f69dd78f58`，另一連線獨立raw下載／64個保護欄位／歷史與服務核對通過，回條SHA `49925286533ee240e7a1b8a5f58513059c69b8646dd185aec8cfb4a51edb1a3d`。原7d0完整raw備份、API與DB所有段落、WP109讀取版結案及history前綴保持。發行對應code X `7c5ba15af622fe4776fecfb9bddc96cabd250ee5`、status Y `23559909602900349e5bf6018a0e9027bf9d64c2`，均真452來源綁定／正常推送／直接ref核回；main已到Y，`mainSyncConfirmed=true/mainSyncPending=false`。本次後補文件提交不改此發行對應。八個WIP provider、global MES QA與偶發登入載入根因仍待辦。


## HOME新整合驗證回報（2026-11-21）

8ff九提交已正常合併HOME6e9，八個原型原bytes保留DEV，正式wrapper隔離已上Web274／03e，八個原生／deep reload／Close已驗。API502／PG0062未改。詳細[本輪QA](docs/qa/20261121-office8ff-integration.md)；交易provider仍由HOME依真來源接續，不把入口防護標成完成。

請繼續下方最新LotBasis39欄與舊DDL38欄精確來源補證及各原生流程來源任務；公司端不負責PostgreSQL、SSH或遠端發行。快速重載偶發登入／menu延遲由HOME追查，無須Office代辦部署。


## HOME新查證：WP109 LotBasis來源版本差（2026-11-21）

已收到8ff的取號、Lot／Device／MONo、參數與稽核補證；HOME三張SWR表0062及引用相容已上線，不重索這三張DDL。下一個精確來源需求是：

- [ ] 補同一來源版本MESTEST.TBLWIPLOTBASIS完整DBMS_METADATA.GET_DDL、COLUMN_ID順序的型別／長度／NULL／DATA_DEFAULT、constraint與index欄序、trigger清單。8ff lookup metadata為39欄：DESCRIPTION400且有XX_MARKING1024；舊WP101 sourceDDL為38欄、DESCRIPTION255且沒有XX_MARKING。標示來源環境／擷取時間並解釋版本差，不查業務列、不執行DDL。
- [ ] 若可核，補原應用伺服器DateTime.Now實際時區與ExecuteSQLNoneQuery_AttachFile本體。HOME會自行定義新服務的業務時區、每日上限與明確原子交易，不把未知原helper當已證實rollback。

HOME正正常整合八個新WIP頁，保留原DEV、正式隔離未交付provider；完整建置／正式驗收後才同步main。Office維持Oracle原生唯讀／UI，不需連HOME PostgreSQL、SSH或部署。

本輪讀取版已完成metadata結案及獨立下載核對。新current-checkpoint SHA `7d0bc62f032e2e24f243fe5b016fa629ec781ed603202d5a082770db4492a4b3`，獨立回條SHA `5da236c29ef699108996d31a5946155b67345d47e07e9f762cbfc114098d607e`；完整保留原496 raw備份、50項既有保護資料與歷史前綴。發行對應code X `50cf3a6dfb3cd6b310c35c4e98310b59c30eab0d`、status Y `f0d26dfb1d414786b483faabae545de1d075ce0f` 均通過436來源綁定、正常feature推送與直接ref讀回。此後純文件補記不改本次X／Y發行對應。main仍含Office新8ff工作，`mainSyncPending=true`；下一輪正常整合與新頁驗收，不以force覆寫。下方496／未封存／收尾待辦等段落保留為前一時點；本段是目前狀態。SWR writer與global MES QA未因此完成。

已驗證程式提交 X `50cf3a6dfb3cd6b310c35c4e98310b59c30eab0d` 完成436份來源綁定（SQL6／API172／Web258，回條SHA `2c04f82d3ba5de2e6c2f1f57c14921643160efdbbd8b100e6e59ccd315ed0759`），已正常推送 `codex/homepc-wp109-read-20261120` 並直接讀回GitHub ref一致，未force。驗收後fetch發現Office main已到 `8ff405290a7f55bfd1e2261f8d85dec97f363409`，新增9提交／8個WIP DEV入口與WP109來源；這些尚未混入本輪正式c828。接著正常合併並驗新入口，保留兩端成果；本輪status／metadata收尾與main整合分開記錄。

## HOME最新回饋：WP109正式讀取版驗收及清理完成（Git／metadata收尾中）

HOME實際API172 `502f88704b928ffcfd0b5dedf6e3c469707e301ac7d04e961be59ce19ca8c614`、DB0062與修正後Web258 `c8284e0ed4482a35ebdebc6de53c5ce5ad063ce517d62fa03604c5a40f677871` 已上線；A4活化log SHA `4533e0f636cf461f055c59e5c8d76f26898543f8dab91e15507c4ed1d7aae71e`。獨立公開／runtime七項通過，11968 bytes／SHA `c577f57474c15c6357f14a767ea6851b9a515b08105ce58e65c18c5b2897f55b`，原生結構、完整來源／私有模式及前後實際版本已核。

Root已完成並封存本輪正式A4操作驗證：46筆原始事件、45筆有效GUI，原false #8是測試期望誤用Lot插入順序，#9／16已按原生C排序復驗；原紀錄保留。最終proof SHA `0982190be36e9feec7db9485a9bdc590666132b18ff76e66ea292d4faf6b3011`。自行Demo、七條件／四狀態、真查找與焦點／Escape、完整單張／limit1、多選與取消、PDF／binary原生下載交付、Close／重進／deep reload、兩個引用拒刪、設備／站點對應／ECN群組讀取回歸及KD／三層選單／72px圖示均已實作操作；836×891及原缺陷1280×720高度通過。OP原生確認造成工具中斷，使用者確認一次後Root核回409提示與資料保留，不把它寫成全程無人協助；後續不重播刪除。最後fresh logs為空。精確清理真14HTTP／29檢核通過（SHA `f31cec32698ab3ef29f24698f3d010163e38ed308d68f1c58b9070b147c30752`），另一唯讀連線26範圍全0（SHA `40abac063f1ec6ec22891195953a960c02707527096704ca2e1965e2189749d2`）；非GUI SQL主6／明細3清理與API父4筆不混算GUI。436來源對真提交的綁定、Git同步及新checkpoint仍待本輪收尾。

本版提供真查詢、完整單張與附件串流；新增、編輯、複製、核准、作廢及上傳仍停用，未把讀取版或DEV示意當作完整SWR交易。三張Basis15／Detail2／Serial2的原生DDL、index與metadata已交，HOME已套0062，不重複向Office索取；Customer／OP的SWR引用相容、防孤立接管及硬刪保護已由API172交付，下方舊「建表前尚待相容」是當時歷史。

驗證後fetch已收到Office `8ff4052` 的新增WP109原生契約，HOME下一輪核對並接writer；下列四類是先前來源缺口，未核對前不自行預填完成：`InitSWRNo/AddSWRBasis`及Core完整取號／交易body；LotBasis DDL與BASELOTNO→CustomerNo歸屬及Device／MO真lookup；Abort／Del SWR的SYS EventLog實作與DDL；FileSizeLimit／InputAbortLog參數來源、型別、單位、default及失敗分支。下方四項原文與所有其他待辦保留。Office維持Oracle原生唯讀來源／UI，HOME負責PG／PPK／API／發行；不新增Office資料庫、SSH或部署任務。checkpoint496仍是前版ET101歷史，本輪新checkpoint／Git未完成；global QA與其他WIP providers仍未完成。

## HOME最新回饋：ET101＋e5f限定結案已完成（2026-11-20）

Root已完成69份私有證據上傳、真正Ready計畫、A2 dry／apply及另一連線獨立下載核對；目前正式checkpoint為 `496ae4bdd3c1cd8d4ecea8d8c16bd9ac35bd843118f97ab965e03651737fd7e9`，獨立讀回收據SHA `3e98979bea11b013ed9e760cdc643fb1f0e4ea50331fd23d4b9c55e5382b534c`，本輪ET101限定結案已完成。API167／3f0bdc7c…、Web256／ade411cc…及DB0061穩定；catalog2349、native35、全部167＋256私有來源、46項原有欄位及history prefixes保持，原2c23 checkpoint的完整raw備份保留。

Code X `f82f183fcd3dfc78d5a690d3c43a15ec64f820b3` 與status Y `faf14b2fd2875464bf280294b681056433b19019` 均完成426來源綁定、正常feature／main推送及直接ref讀回；本批發行對應main提交為Y，`mainSyncConfirmed=true/mainSyncPending=false`。本次結案後文件補記不改本批X／Y紀錄，後續純文件提交不變更此發行對應。

本輪只完成已記錄ET101操作、12個Office WIP入口防護及指定smoke；GUI132raw／124驗證＝117GUI＋5獨立SQL journal項＋2背景API，另3解釋／復驗筆記及五原false保留；六份實際唯讀收據與獨立GUI21範圍全0分開記。global QA／WIP正式providers仍未完成。下一WP109僅reference-compat A2 prepared候選，未執DDL或部署，不代表SWR交易已交付。

以下原文中的收尾／pending為先前時點，完整保留為歷史；本節是目前結案狀態。

Office繼續處理MES原生Oracle唯讀來源／UI／規格；HOME自行處理PG／PPK／API／發行。原有WP109精確來源待辦及其他未交付功能保持，不要求Office執行HOME資料庫或上版工作。


## HOME結案前回饋：ET101與e5f整合階段（2026-11-20；原文保留）

HOME已正常整合Office e5f與ET106完成歷史，API167／3f0b與Web256／ade411cc已正式切換；0061完成真備份／COMMIT與獨立核對。真API A4單輪八組228 HTTP／1568 assertions通過，另連線22自有範圍全0；公開TLS七項／live-current／nonroot／完整167+256來源讀回通過，API／DB未因Web切換改變。正式GUI已sealed／passed_scoped：132raw／127true＝124驗證（117GUI＋5唯讀SQL＋2背景API CAS）＋3resolution notes，五false與復驗完整保留；五個獨立SQL journal事件另有事件49退休佐證，實際六份GUI範圍唯讀收據；Customer3／B/F各1與Address1／Contact1真GUI建刪（子列由Customer native cascade），SQLfixture／cleanup0，獨立21範圍全0。十二WIP native／direct重進／reload／Close正式防護及四舊主檔read smoke已記，**不代表WIP provider交付或全部舊CRUD重驗**。獨立peer90df59c8…及Root acceptance c61d5d87…已完成，code Xf82f183真426綁定後已正常推送feature／main並直接讀回。純文件status與新checkpoint仍待收尾；User引用／完整workflow不擴稱已測，A2／A3歷史不改。各頁觀察映射與十二模組連結見[本輪QA](docs/qa/20261120-et101-office-e5f.md)。

- [ ] Office後續新WIP DEV頁統一使用 `import.meta.env.DEV && developmentPreview === true` 隔離初始資料、storage、查找／Query、命令、選取與所有彈窗；正式尚無provider時空資料及明確未開放，不能只停用一顆按鈕仍露假lookup或假成功。HOME已對e5f九個新元件／12入口套同契約並逐模組補MD。
- [ ] 保持KD共用外框與原三層選單；同頁只一個主標題，不用全域CSS改所有既有頁。每模組MD寫原來源、欄位／NULL／狀態／並發及跨系統副作用；完整MES交易與DEV示意分開交代。
- [ ] 後續改ET101請對齊真query-key路由、完整41欄讀／29可寫、nullable signed CAS、New正規化但Edit raw key不變、六個hidden只讀、changed-only合法舊值保留、savedDTO先採用、逐筆部分成功與unknown-write停送。Apply保持Edit，clean Confirm不多寫。No-Job才支援真核准，遇流程候選503；Import未交付。

Office維持MES原生Oracle唯讀來源／UI／規格；HOME維持復刻PostgreSQL／PPK SSH／API／部署／正式驗收。下方原生查證及尚未交付provider待辦完整保留，不要求Office操作HOME資料庫或上版。

### WP109 精確原生來源補證

已補 [WP109 取號、Lot／Device／工單、參數與作廢稽核](docs/modules/WIP/特殊生產需求維護.md)。附件 SQL helper 本體及實際時區未見可核來源，已在規格註明。

三張SWR Basis15／Detail2／共用Serial2的Oracle DDL、index與metadata已交，不重複要求；HOME本機只讀readiness確認captured catalog缺這三表及LotBasis，沒有建表或接provider。Office請僅補下列原碼摘錄／必要Oracle唯讀metadata，不操作HOME PG、SSH、部署或業務列：

- [x] 補 next counter、初始化、建立交接及超 999 行為。
- [x] 補 LotBasis DDL、客戶同表比對、Device 來源及 MONo 自由輸入。
- [x] 補作廢及單獨刪除差異、EventLog metadata 與失敗交易。
- [x] 補 FileSizeLimit／InputAbortLog 文字參數、單位與 fallback。

HOME在任何SWR建表前須先補ET101 CustomerNo registry absent/present相容與防orphan；開放SWR寫入前補OP引用／鎖協作。這是HOME下一批責任，尚未完成；本輪ET101／Web256驗收不代表WP109正式交易交付。原生`issueoption=0/issuetable=NULL`，Approve為直接0→2，不新增無據SAP／generic IssueJob需求。


## 家用驗收回饋：ET106 writer A1／A2（2026-11-20）

HOME已依已交原生22／13欄及核定契約完成本批真API／指定GUI／清理：API121／961，A2 GUI32有效（30GUI＋2SQL），原A1兩產品缺陷已在新Web246 A2真復驗；原false／工具觀察保留。Address／Declaration／BFT由GUI建立清除，Customer僅SQL前置／清理各1，獨立13範圍全0。這是本批範圍，不擴稱全MES／WIP／簽核provider完成。

Code X ebc03a1已409來源綁定PASS、正常feature推送／ref讀回；文件Y及combined checkpoint由HOME Root依實際完成後另報，這裡不預填main完成。公司後續e5f4466新增15提交／12WIP入口已另行source-only合併，尚未部署／驗收，不混本輪證據。公司既有來源待辦與歷史保留；本回饋沒有新增Office PostgreSQL／SSH／部署或業務資料查詢工作。詳[ET106 writer QA](docs/qa/20261120-et106-writer.md)。



## 桌機進度（2026-11-20）

- [x] 桌機乾淨工作樹 fast-forward 至筆電交接 `d87c731`。依 Form／Designer、Service、Core 完成 Vue DEV 主明細、參考批／刷入批號、回貨站、增修、刪除與作廢。
- [x] [WP401 規格](docs/modules/WIP/外包單開立.md) 交付交易和 API 契約；三張直接表均有 Oracle DDL／CREATE-only。
- [x] [WP402 外包單確認](docs/modules/WIP/外包單確認.md) 已依 Form／Service／Core 做 Vue DEV 單筆確認及共用單據，交付 FutureOSLog 與 ERP 設定表結構及正式 ERP API 規格。
- [x] [WP403 外包回貨](docs/modules/WIP/外包回貨.md) 已依兩份 Form、Service 與 Core 做 DEV 分批回貨，補兩張 Oracle 直接表及 CREATE-only。
- [x] [WP404 取消外包出貨 ERP](docs/modules/WIP/取消外包出貨ERP.md) 已完成公司端 DEV 守門與臨時外包模擬回復，正式 ERP／Reverse 交家用端。
- [x] [WP901 回復處理](docs/modules/WIP/回復處理.md) 完成 Vue DEV 歷程、回復守門和 API/CREATE-only 交接。Oracle 原始 DDL 回 ORA-03135，三張補表以唯讀 metadata 核對。
- [x] [WP902 刪除已下線生產批](docs/modules/WIP/刪除已下線生產批.md) 完成雙 Grid、刪除確認、DEV 守門與五張表的 metadata／CREATE-only。
- [x] [WP903 批次刪除已下線生產批](docs/modules/WIP/批次刪除已下線生產批.md) 完成 DEV 過濾、全選、包裝預檢與逐筆結果。表結構和交易沿用 WP902。
- [x] [WP904 生產批作業區變更](docs/modules/WIP/生產批作業區變更.md) 完成 DEV 選批、主子流程、變更與清除。沿用現有 DDL／CREATE-only 參考並交接整批替換交易。
- [x] [WP905 產品版本變更](docs/modules/WIP/產品版本變更.md) 完成 DEV 產品與舊版查詢、基礎批選取、包裝守門與版本變更。正式交易沿用既有表。
- [x] [WP906 ECN 設定精靈](docs/modules/WIP/ECN設定精靈.md) 完成 Vue DEV 六步、條件及受影響批。三張正式表沿用 WP201。
- [x] [WP907 生產批數量調整](docs/modules/WIP/生產批數量調整.md) 完成整批、Bin、元件三種 Vue DEV 與 Oracle 調整歷程兩表的 metadata／CREATE-only。
- [x] [WP908 標籤列印](docs/modules/WIP/標籤列印.md) 完成 DEV 多批對應、預覽與模擬列印，補列印歷程 metadata。
- [x] [WP909 換廠作業](docs/modules/WIP/換廠作業.md) 完成 DEV 查詢、靜電箱整組檢查、目的區驗證及換廠。選單清冊未列 WP910。
- [x] [WP911 生產批作業站用料記錄調整](docs/modules/WIP/生產批作業站用料記錄調整.md) 完成 DEV 報工／用料主明細、加改刪與合計，沿用現有表。
- [x] [WP912 生產批作業站製程特性值調整](docs/modules/WIP/生產批作業站製程特性值調整.md) 完成 DEV 批／元件特性值編輯、唯讀守門與稽核表 metadata。
- [x] [WP913 生產批屬性調整](docs/modules/WIP/生產批屬性調整.md) 完成 DEV 批／元件屬性查詢及增刪改。WP914 已有公司端交付。
- [x] [WP915 生產批產品變更作業](docs/modules/WIP/生產批產品變更作業.md) 完成 DEV 篩選、聯產品、包裝預檢、逐批結果。正式 ERP 和流程改掛已交接。
- [x] [WP916 生產批強制結批作業](docs/modules/WIP/生產批強制結批作業.md) 完成 DEV 單批結批、包裝守門與共同 CloseLot 依賴的唯讀結構交接。
- [x] WP901–WP916 公司端選單已齊；另已補本頁 HOME 指明的 WP109 精確原生來源。

## 2026-11-20 桌機再接手：從 WP401 開始

- [x] 筆電接回桌機原工作後，WP509、WP106、WP109、WP112 的 Vue DEV、規格、缺表 Oracle 唯讀 DDL/metadata、PG CREATE-only 與家用端 TODO 已分段推 GitHub；WP112 程式提交 `ba20121` 已向 remote `main` 核對，交接詳 [桌機接手點](docs/CODEX_HANDOFF_DESKTOP_20261120.md)。本交接文件另有後續提交，桌機以 pull 後最新 main 為準。
- [x] 桌機檢查乾淨工作樹並 fast-forward 至筆電交接點，讀交接與兩端 TODO。WP401 起依清冊核對 Form、Designer、Service、Core，進度見本頁最上方。
- [ ] 保留公司／家用分工：桌機不連 YMES PostgreSQL、不做正式 API/migration/部署。WP112 真 SAP/Log/Inventory provider 未交付，家用端需按 [WP112 規格](docs/modules/WIP/工單撤派工.md) 接正式交易；公司端不把 DEV 假狀態視為正式完成。先前 vendor DLL 問題已取消，不接該項。

## 筆電交接紀錄：WP112 UNDO DISPATCH（2026-11-20）

- [x] 靜態核對 THEIL `frmUndoDispatch_THEIL_BU1`／`wsWIP.LoadUndoDispatch`／`exeUndoDispatch`／`clsLot.LoadUndoDispatch`／`clsSAP.GetSAP_ZPPT0025x_Data`。Vue DEV `/YMES/WIP/UndoDispatch` 有兩模式、MO lookup、13 欄 Grid、全選/取消全選、逐批假 Confirm；規格 [工單撤派工](docs/modules/WIP/工單撤派工.md) 明列 SAP/Inventory/Log 不在 DEV 實作。
- [x] PowerShell 5.1 + 受保護 MESTEST profile 唯讀取得六張缺表的原始 DDL/index/metadata：MaterialLot、SAP 計數器、MO OS、ZPPT0025A/B/C1；PG CREATE-only 在 `database/reference/mestest_wip_wp112_create.sql`。MESTEST 本地 ZPPT 不能代替實際 `THSAP.*@SAP_DBLINK` 的契約，家用端另核。未查業務列、未寫 Oracle、未連 YMES PostgreSQL、未 build/test/browser。
- [ ] 照 [MES 選單清冊](docs/MES_MENU_FUNCTION_INVENTORY.md) 接續未完成 WIP 功能；公司端只做原碼／Oracle 唯讀／Vue DEV／規格/家用交接，避免覆蓋家用端正式變更。

## 公司端最新進度：WP109 SWR BASIS（2026-11-20）

- [x] 已依 `frmWIPSWRBasis`／Def、`wsWIP`、`clsWIP` 靜態核對七查詢條件、四狀態、14 欄 JOIN Grid、ADD/EDIT/Copy、Approve/Void、附件與流水號；Vue DEV 路徑 `/YMES/WIP/SwrBasis`，規格 [特殊生產需求維護](docs/modules/WIP/特殊生產需求維護.md)。
- [x] 公司 PowerShell 5.1 + 受保護 Oracle profile 唯讀取得 `TBLWIPSWRBASIS`、`TBLWIPSWRDETAIL`、`TBLSYSSERIALNO` 原始 DDL/index/metadata；交 PostgreSQL CREATE-only `database/reference/mestest_wip_wp109_create.sql`。未查業務列、未寫 Oracle、未連 YMES PostgreSQL、未執行 build/test/browser；正式 API/PG/驗收交家用端。
- [ ] 下一項依 [MES 選單清冊](docs/MES_MENU_FUNCTION_INVENTORY.md) 查未完成 WIP 功能；先看 Form/Service/Core，缺直接表才用公司受保護 Oracle 連線唯讀查 DDL。持續補 DEV UI、規格與 `todolist_homepc.md`，正常推送 GitHub main，勿覆蓋家用端變更。

## 家用端最新整合進度：Web224 C1（2026-11-20；限定GUI完成／main待同步）

Office最新df10bd08的載具拆批／併批已正常合併至3f99c172，保留雙方來源。Web224來源f9bb9566…完整建置exit0、封裝／部署e2df5e3d…與公開HTTP七項已通過；Root封存限定GUI73通過檢查點／73events／0observations，含自行Demo登入、18native＋1direct、新兩頁guard／19KD／無假metadata／單一主標題／Close／重進／direct fresh、WP307／SD108空lookup界線及ET104真CRUD。2筆全GUI建刪、SQL fixture writes0、獨立精準B/F皆0，fresh reload21收合選單／空首頁／logs[]。C1 code bacf0ee5已完成376綁定399f648b…及featurebranch正常推送／遠端讀回，main仍pending。API6d／151份與DB0058未動，所有WIP正式providers仍由家用端接手。

兩頁驗收解決automatic review先前限制，後續main正常push被Git遠端新增Office0b342616／d5a8bd13以nonFF拒絕，沒有force或改寫main。Office新元件／等級併批兩頁未合併本次worktree、未建置／部署／驗收，另列後續；家用先完成C1 feature-only checkpoint並接ET105 API。mainSyncPending=true，狀態文件提交尚待實證，不要求公司端做PG／SSH或停下UI開發。

220 dc34／372及222 3953／374已正常同步featurebranch並讀回相同，各版86／69項proof不改。先前新兩頁未驗的main push遭automatic review拒絕且未執行，Root會完成C1驗證後再正常同步；mainSyncPending=true。ET105僅隔離候選與純讀peer，尚未build或migration；本節不新增公司端PG／SSH／部署工作，既有Oracle唯讀來源與UI待辦保留。見[Web224 QA](docs/qa/20261120-office-wip-carrier-merge-integration.md)。

## 家用端最新整合進度：Web222（2026-11-20；限定GUI完成／main待同步）

公司12c6a2e的WP208／WP209已與家用Web220 dc34ee6正常合併至32d422b5，保留雙方來源。Web222／B330b9…（來源222檔canonical ee360b1d…）完整建置與七項HTTP已通過；API6d／151份、PostgreSQL0058保持不變。17入口採既有KD框架，WIP正式providers仍待家用端實作。

Root已封存限定GUI69通過檢查點／70events／1個已解menu hydration harness觀察：新兩頁＋舊14native的正式防護／Close、新兩頁重進及direct fresh／無假Bin／KD單一主標題、WP307 Grid30×40／四縮放、direct OP、SD108空lookup／Grid／zoom與ET104新Web完整smoke均通過。2筆資料全GUI建刪、獨立精準broker／forwarder皆0、SQL fixture writes=0，fresh reload空首頁／21收合選單／logs[]。原locator detach觀察與穩定三層選單復驗保留，未修改產品來源；SD108未驗populated layout。mainSyncPending=true，374綁定及新commit／main推送仍待實證，見[Web222 QA](docs/qa/20261120-office-wip-split-integration.md)。

前版Web220的dc34ee6／372綁定／featurebranch正常推送與私有證據已完成，86項proof及當時main待同步狀態保留。ET105最新為隔離候選與純讀peer，尚未build／migration。公司端仍只處理原生Oracle唯讀來源、UI及規格，不新增公司端PG、SSH或部署任務。

## 家用端整合進度：公司WIP Web220（2026-11-20；限定GUI完成／main待同步）

已正常合併公司b3f0f06的14個新WIP Vue／App／AreaShow及原生來源文件，保留雙方工作，未force push。家用端新Web220已建置並部署 `823d0571d6dc`（canonical8071ec80…），公開七項HTTP與資產檢查通過；API6d2501…／151份與PostgreSQL0058維持原版。

本輪補六個原生menu route、WP203／WP307正式provider未交付防護與15入口限定KD風格；13個其他新增Vue維持公司原Git blob。Root已封存限定GUI86通過檢查點／87events／1個已解readonly harness觀察，14原生＋1direct／Close重進、WP307 Grid／顏色／四縮放及ET104 smoke通過；SD108無核准Area，只驗picker／grid／zoom／Close，未驗populated layout。2筆smoke全GUI建刪、獨立B/F0、freshreload logs[]。**目前尚未完成本輪main同步**，實證與界線見[整合QA](docs/qa/20261120-office-wip-integration.md)。

ET104 A1的featurebranch X65f17a0／Yb015e51、checkpoint9cbe與API169／621／GUI87是已完成歷史，不套用成Web220新驗收。新增UI、DEV互動或正式不可用提示都不代表WIP API已交付；家用端會繼續依原生來源實作provider。ET105目前僅readonly audit，無API／migration。

公司端持續處理原生Oracle唯讀來源、UI與模組規格；本節不新增公司端PostgreSQL、建表、SSH或部署任務，既有來源查證待辦保留。Root另取得Office dbebd0f／12c6a2e，兩版未在本次220部署／驗收；先完成220的372綁定及featurebranch正常推送，再正常merge新版、另做Web222驗收後才同步main。mainSyncPending=true，保留兩端工作，不重寫220的封存來源與證據。

## ET104 A1 正式驗收回饋（2026-11-20；main整合另記）

ET104 A1 release code `65f17a0c347acd4b5df2a1be1cad712138ececd6` 已正常推送 `refs/heads/codex/homepc-et104-20261120`，直接 `ls-remote` 讀回相同。358份來源綁定通過（SQL1／API151／Web206，未允許差異0；舊API131份僅representation、Web188份僅Git EOL差異），無force push。後續純文件狀態提交不改此release code；actual status SHA與遠端讀回另封存於checkpoint。mainSyncPending=true：公司b3f0f06的14個新WIP Vue／App／AreaShow將正常合併、另建新Web與驗收後再同步main，不能把A1的87項證據套用新產物。

HOME PC已完成0058、正式API `6d2501aad008`／Web `56c31765e2af`；API169HTTP／621斷言與app-browser87項（77GUI＋10SQL讀回）通過，12筆Forwarder由GUI建刪，11項資料範圍獨立全零。三個harness誤判原始紀錄與復驗保留；見[模組](docs/modules/ENT/貨運行基本資料.md)及[QA](docs/qa/20261120-et104.md)。

公司端請對齊：列表 `{items}`、query-key item／approve、完整nullable十五欄，原ForwarderNo／BrokerNo在Edit均唯讀且不得trim／轉大寫。必帶nullable dataStamp，Delete204為硬刪、200為停用savedDTO；Apply先採新DTO/token轉Edit，乾淨停用。New才查真Broker2；既有0／2聯絡維護不重驗舊parent資格。未來Customer指定Forwarder時，家用API writer應合作Forwarder namespace／SHARE；不派公司端連PG或SSH。

A1的151 API／206 Web來源先在 `codex/homepc-et104-20261120` 正常同步；實際X/Y及遠端讀回待記，尚不能稱main已整合。已取得公司新提交 `b3f0f062e22b1df3437868a2c29ec51e80ebbb92`；接著在同branch正常合併、以新Web220及新增WIP正式入口／ET104回歸另驗，再同步main。保留全部公司來源，不force push。原生來源查證待辦繼續保留，不新增公司端資料庫／部署工作。

## 家用端最新回饋：ET103 已驗收並同步 main 0c65a65（2026-11-20）

HOME PC 已完成0057、API `12f26b18e94a`／Web `b3949d57fd39` 的報關行功能。API單輪八組126HTTP／464斷言、正式 app-browser59項（56GUI＋3SQL）通過；十筆Broker均由GUI建立／刪除，兩筆SQL引用fixture已精準清理，十一項資料範圍獨立讀回全零。最新來源與限制見 [ET103模組](docs/modules/ENT/報關行基本資料.md)及 [QA](docs/qa/20261120-et103.md)。355份來源綁定通過後，release code `0c65a655fae538280244289e281733f612a0eb4f` 已從012f257正常推至main，再fetch確認相同，沒有force push或公司新提交；後續純文件狀態提交不取代本release code。

公司端後續使用ET103的 `{items}`清單、query-key item與nullable原始DTO；主鍵Edit保持raw，不轉大寫或把NULL token變0。hard Delete回204、退用回200完整savedDTO。未來ET104新引用需協作 `YMES:ENT:Broker:` namespace及父列SHARE，確認state2並持鎖到提交；ET101指定Broker也須協作。這是家用端API實作責任，不派公司端連PG或SSH。原生無法證明的引用刪除限制已明載為YMES防孤兒新增行為；Job候選存在但provider未交付時保持503，不用DEV直接核准冒充完整簽核引擎。公司端原來源查證待辦保留。

## 家用端最新回饋：ET102 已驗收並同步 main 7b6771e（2026-11-20）

HOME PC 已完成 ET102 客戶連絡人 PostgreSQL 0056、正式 API `0339807d5b7e`／Web `6a4f390f37b8`；API 單輪八組 98 HTTP／406 斷言、正式 app-browser 53 個檢查點（52 GUI、1 SQL）通過。11 筆 Contact 全由 GUI 建立／刪除，清理 lookup 後七個精確範圍獨立讀回全零。完整限制見 [模組](docs/modules/ENT/客戶連絡人資料.md) 與 [QA](docs/qa/20261120-et102.md)。352 份來源核對通過後，release code `7b6771e4718a56a950f4a4de5c61564ea8ed4137` 已從 c2c4930 正常推到 main，再次 fetch 確認相同；保留兩端歷史，沒有 force push。後續純文件狀態提交不取代本 release code。

公司端同步時保留真 Customer 全狀態 lookup、唯讀原始 owner／name、nullable token／audit、Apply 轉 Edit 與續存，以及正式區不使用 DEV seed 的隔離。每次異動須重查父客戶存在；父缺時回 422 並保留資料。這些與 OPB03 的父表規則不同，不可直接套用 OPB03 限制。公司端不需連 PostgreSQL、建表或 SSH；本批不新增公司端資料庫任務，原有原生來源查證待辦繼續保留。

## 家用端最新回饋：OPB03 已驗收並同步 main caae71d（2026-11-20）

HOME PC 已依原生 DDL 建立 YMES PostgreSQL 的 OPB03 兩表、完成 API／Web 與正式 app-browser 驗收。現在 API `2a2291cc1163`／Web `fc83e8bc5484`／0055；API 八組單輪 162／578，GUI 67 項含 SQL 讀回一項，測試資料精確清零。公司端無需操作家用 PostgreSQL、SSH 或部署；原生 Oracle 與 Vue 來源工作仍由公司端進行。正式程式提交 `caae71d25ed3b88274aeddc5095806e286b5816e` 已正常快轉推到 main，349 檔來源核對通過；當時公司端沒有新提交。後續純文件狀態提交另記，保留兩端歷史、不 force push。

OPB03 已知 CRUD 不受以下兩個細節阻擋，目前採明載的 YMES 行為；請公司端僅做原程式唯讀查證並更新 [模組文件](docs/modules/OP/客戶站點對應設定.md)：

- [ ] 補 `frmB_OPCustomerOPMappingDetailDef` 的 Apply／Confirm 成功事件精確片段，確認原生明細 Apply 是否關窗。目前 YMES 的 Detail Apply 留在 Edit 並採用真實新主鍵／版本；Header Apply 與 Confirm 皆關窗已有來源。
- [ ] 補 `LoadCustomerOPMappingBasis` 的 CustomerNo／CustomerOPNo 查詢條件片段，確認前綴／包含、大小寫與 `%`／`_`／`*` 的原生解讀。目前 YMES 明載採字面 contains，不把輸入當 SQL wildcard。

其他未交付的共用 Element／Job 等來源依原待辦持續查證；此段不新增公司端資料庫或上版任務。下方 OPB02 與較早的完成／pending 記錄保留歷史。

> **本批完成（2026-11-20）：已正常同步GitHub main `487e9d1`。** release code commit為 `487e9d1d15489be5d864c54abc832b81724f579d`；root核對1份migration／139份API／206份Web後正常推送395a→487，保留兩端歷史、沒有force push或公司新增提交。公司端可依既有安全合併流程同步；後續純文件狀態提交另記，不取代此release code。下方推送前pending保留歷史，未提供的共享Element／Job規則仍依原待辦查證。

## 家用端最新回饋：OPB02已實測並正常同步main487e9d1（2026-11-20）

- 家用端已完成OPB02原生11＋2欄核對、0054／最小權限、正式API `1d09e4febf48` 與Web `cfa5cf288359`。API八組分兩輪及正式IAB 48驗收檢查點通過（含1項SQL清理讀回，不是48純點擊），六群組全GUI建立／刪除，GUI後19項精確計數全0。完整來源、SQL fixture界線、Job候選503未觸發與未提供provider詳見 [ECN模組](docs/modules/OP/ECN作業站群組設定.md)。本批程式已正常推送main `487e9d1`，root確認origin相同；此前395a及Git待推為同步前歷史。
- 公司端後續同步保留三層選單、KD配色、正式DEV隔離、ECN raw NULL／parent CAS／字面PS／全域OP唯一與選取owner防護，不以舊DEV流程覆蓋。PostgreSQL核對／migration、API／交易與部署持續由家用端處理；以下只新增原生程式／資源查證，不派公司端連PG或SSH，也不請公司端建表。

### OPB01共享Element：精確來源查證待辦

OPB01為「作業站參數群組設定」，不是OPB02簽核流程。Binding14欄及共用Element21欄原生DDL已完整，**不需重造Element表或重取這兩表DDL**；live欄位／九個參考default差異／權限是否需forward修正由家用端另查，不能以reference差異當作live已缺欄。

- [ ] 從 `STD/MESws_STD/wsSTD/wsSYS/wsSYS.cs` 的 `LoadElementValue`、`ChkElementValueValid` 追到真正SYS Core完整方法，交付呼叫鏈及程式路徑／方法定位。整理DataType／ValueType／InputType／Controlled／AllowNull分支、ValidList與FormatString真正grammar、Min／Max與空值規則、MES Parameter關聯／讀取限制及錯誤回應；實際Core類別由原碼確認，不猜名稱或把DEV validValues當provider。
- [ ] 從 `frmOPGroupAttrib` 及 `DelOPGroupAttrib` Service／Core查清InputAbortLog實際設定讀取來源、key／default／失敗分支，以及abortLog傳到哪個記錄器、實體及欄位。只交已讀到的程式證據；缺檔／provider要精確標示，不能預設0、只收原因不保存或宣稱刪除流程完整。此項不要求公司端對資料庫執行查改或建表。
- [ ] 查明 `TBLSYSELEMENTBASIS` 的DataType／TransferType／ValueType／Controlled／InputType／AllowNull等選項在原生程式的真ValueList查找key／provider與繁中resource／常數。可讀源碼／資源直接附出處；若只能依賴DB設定，標出真依賴交家用端判定，不用Vue硬編碼label／fallback補成正式目錄，也不新增公司端DB操作待辦。

家用端可先接既有主表盤點、真Customer／ECN／Element raw metadata reader及交易骨架；未交付的值驗證或作廢記錄分支不得假成功。公司端交付上述來源後更新 [OPB01模組](docs/modules/OP/作業站參數群組設定.md) 及本檔交回；下方其他來源與UI待辦全部保留。

**前次家用端回饋：公司整合歷史（2026-11-20）**

- 已正常合併公司端至 `f68ecea`，保留兩端來源。原 OE／WIP 23 個入口加上 WP303、WP304、WP308 三個，累計 26 個唯一入口已完成正式 app-browser 的路由、不可用提示、無 DEV 業務資料及離開／重進防護；三個新入口實按頁內 Close 返回空白首頁，console 無紀錄。分批版本與精確範圍見 [公司整合 QA](docs/qa/20261120-office-integration.md)。這不是 26 個功能的正式 API 或交易完成。
- 家用端已修正正式 non-DEV 不可回傳 seed／fixture、查詢／查找及異動入口的 disabled 與 handler guard，並保留公司端 DEV seed、共用 storage 與來源邏輯；WP308 額外修正四個位置欄位交換的 TypeScript 型別錯誤，未改值語意。後續公司端正常同步時請保留這些正式隔離及維護 MD。
- 正式 PostgreSQL schema 核對／migration、API、並發交易、部署與驗收仍由家用電腦依 [家用端待辦](todolist_homepc.md) 持續接手；公司端繼續原生 Oracle 唯讀來源與 UI 開發，無須連家用 PostgreSQL 或 SSH。下方歷史交接與尚未交付的 provider／API 待辦保留，GitHub main 已由家用端正常推送至 `c64d0ee`，公司端可依既有安全合併流程同步；兩端歷史均保留。

## 公司桌機交回原公司筆電（2026-11-19）

### 先同步

1. 在公司筆電的 YMES 工作目錄執行 `git status --short --branch`，保留筆電既有改動，再以 `git pull --ff-only origin main` 同步。功能交接以包含 `a1f382f` 的版本為準。該提交包含先前的 `09349eb`（OEB01）及 `c9de376`（OEP01）。桌機曾遇到 GitHub HTTPS 連線重置，請用 `git log -1 --oneline` 和 `git merge-base --is-ancestor a1f382f HEAD` 確認已取得最新一批桌機功能。
2. 以本節為起點，配合 [公司端交接](docs/CODEX_HANDOFF_OFFICEPC.md)、[原 MES 選單清冊](docs/MES_MENU_FUNCTION_INVENTORY.md)、[模組規格索引](docs/MODULE_REPLICATION_SPEC_INDEX.md) 及 [家用端待辦](todolist_homepc.md)。切換電腦後從 Git 和本文件接續即可。

### 已完成與驗證

- [x] PRD 已從 PD102 RunTime 接續至各子功能及產品設定、封測功能。PDI01、PDI03、PRDGBOMEXPORT／OLD、PD_BATCHMODIFYAREA、PDCUSTOMERDEVICE 缺可讀的獨立客製來源，具體缺件記於本檔後段，可平行等資料。
- [x] PD206 已交付父主檔刪除／停用和八種萬用鍵優先順序。QC103 已核對 FUNSA、Type 4／6 ShowFlag、QC301 一般／Wafer 分流，並更新 DEV 定義窗。QC107 已核對 provider，實際選項與 UserDefineAQL 部署值待補。QC108 四個標準 FUNQR 回傳 null，正式可執行版本仍需客製來源。
- [x] OE 完成 13 項公司端 DEV 功能，OE101／OE102、OE201–OE209、OEB01、OEP01。Vue 入口集中在 `web/src/App.vue`，各項規格在 `docs/modules/OE/`，Oracle DDL 與 PostgreSQL CREATE-only 參考在 `database/reference/mestest_oe_*.sql`。製程、發料、挑片、ERP 與正式簽核的簡化已分項記錄。
- [x] 截至 `c9de376`，Vue `vue-tsc --noEmit` 通過。各批提交均通過 `git diff --check`，13 份 OE 模組文件的本機連結已檢查。公司端本輪採靜態驗證，瀏覽器操作與正式 API 驗收由各自後續流程處理。

### WP101 生產批開立已交付（2026-11-19）

- [x] 直接核對主／定義 WinForms、wsWIP 與 clsLot；查明工單條件、LotState=0 清單、CRUD、CloseLot 刪除、批次明細與下線交易。
- [x] 使用本機受保護 Oracle MESTEST 連線唯讀取得七張 WIP 表 DBMS_METADATA DDL 與資料字典；未查業務列、未執行 Oracle DDL/DML。來源 DDL 和 PostgreSQL CREATE-only 參考在 database/reference/mestest_wip_wp101_*.sql。
- [x] Vue DEV 假資料頁已加入 web/src/components/WipLotBasisCreate.vue，選單路由為 /YMES/WIP/LotBasisCreate；含工單查找、狀態／批號查詢、新增／修改／複製／刪除／下線、基本欄位與流程／元件／略過 OP／Lot Property 頁籤。DEV 列使用 localStorage，不接正式 API。
- [x] 規格與 API/schema handoff：docs/modules/WIP/生產批開立.md；索引與家用端待辦已更新。明確記錄物料扣帳、StartLot、CloseLot、屬性動態目標、ECN、自動 BR／OP 等正式限制。
- [ ] 本輪未執行 build、test 或 browser；後續需要驗收時再依使用者指示執行。

### WP102 生產批開立_批次已交付（2026-11-19）

- [x] 直接對照 Form／Designer、工單／流程／BOM／LotBase／序號載入、Add/View/Tuning、Expand、Confirm 及 THEIL Service/Core；規格記錄兩種分批、序號重查／保留、批次逐筆寫入與部分成功語意。
- [x] PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 TBLWIPLOTPROPERTY_TEMP 與 TBLWIPLOTPROPERTY DDL/constraints/indexes/triggers。WP102 新增需求僅 TEMP；正式屬性表與其他 WIP 表沿用 WP101，不重複建 SQL。未查業務列、未執行 Oracle DDL/DML。
- [x] 完成 Vue DEV 頁 /YMES/WIP/LotBasisBatch、三層選單、by Count/by Qty、批次 Grid、BOM/Process、Add/View/Tuning 假視窗、確認驗證及專屬 localStorage。
- [x] 新增 WP102 模組規格、Oracle source DDL、PostgreSQL CREATE-only（僅 TEMP）及家用端 API/schema handoff；未連 YMES PostgreSQL，未實作 API/migration。
- [x] WP102 靜態檢查並推送 GitHub；其後已接續完成 WP201、WP202。

### WP201 生產批執行已交付（2026-11-19）

- [x] 對照 THEIL WinForm／Designer、Service/Core；完成 Execute 與五個查詢條件、lookup、20 欄 Grid、狀態、掃描優先序、LMS/QTime、Group OP、BR／TransferLot 的規格。
- [x] 以本機受保護 Oracle profile + PowerShell 5.1/ODP.NET 唯讀取得 39 張 MESTEST 表的 Oracle DDL／欄位 metadata；39 張 CREATE 欄位數逐表符合 ALL_TAB_COLUMNS。TBLWIPECNLOTBASIS、VIEW_TBLWIPPACK_LOT 在 MESTEST/PUBLIC 均未找到，未虛構 DDL。
- [x] 完成 /YMES/WIP/LotExecution Vue DEV 假資料畫面、WP201 模組規格、39 表 PostgreSQL CREATE-only 參考與家用端 API/schema handoff；未連 YMES PostgreSQL、未建立正式 API/migration。
- [x] 依本輪規則未執行 build、test、browser；完成靜態檢查與 GitHub push 後繼續下一個功能。

### WP202 生產批執行_元件已交付（2026-11-19）

- [x] 依 `frmWIPLotSelectbyComp.vb`、標準 `wsWIP`、`tcWIP_IC.clsLot` 與 `clsLot.LoadComponentState` 核實三欄輸入、Component JOIN LotState 查詢、元件解析、七欄 Grid、Find 與共用 ExecuteLot 流程。
- [x] 查詢只讀 TBLWIPLOTSTATE、TBLWIPCOMPONENTSTATE、TBLWIPLOTBASIS；Oracle DDL／PostgreSQL CREATE-only 已由 WP101 查證，沒有新表，未重複產生 CREATE SQL。
- [x] 完成 Vue DEV 頁 `/YMES/WIP/LotExecutionByComponent`、FunctionNo EXECUTE LOT_COMP 三層選單路由、[WP202 規格](docs/modules/WIP/生產批執行_元件.md)與家用端 API/schema handoff。
- [x] 公司端未連 YMES PostgreSQL、未實作 API／migration；未執行 build、test、browser。靜態檢查後推送 GitHub。

### WP204 生產批執行_設備已交付（2026-11-19）

- [x] 對照 frmEquipmentLotState Form／Designer、CI／CO／設備狀態子視窗、wsEQP／wsEMS／wsWIP 與 Core；完成設備收藏頁、5×3 看板、LoadPort、推薦及底部操作流程規格。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護 profile 唯讀取得 TBLEQPFAVORITE、TBLWIPLOTGPEQUIPMENT、TBLPRDSTAGESTEPSET、TBLPRDSTAGESTEPSETDETAIL DDL/index；未讀業務列、未執行 Oracle DDL/DML。
- [x] 完成 /YMES/WIP/EquipmentLotState DEV 假資料畫面、FunctionNo EXECUTE LOT_EQP fallback 三層選單／路由、WP204 模組規格、Oracle source DDL 與 PostgreSQL CREATE-only handoff。
- [x] DEV 僅寫專屬 localStorage，不連 YMES PostgreSQL、不實作正式 API／migration；未執行 build、test、browser。

### WP605 成品庫帳務維護已交付（FunctionNo FGD INV，2026-11-19）

- [x] 對照 frmInventory_FGD／Def／Turn／FGDIn WinForms、Designer、wsINV／wsWIP／wsSYS 及 clsInventory／clsLot；核實庫存查詢、Component/Bin 切換、人工調整、逐筆刪除、三種轉庫與 FGD In 交易狀態。
- [x] Oracle MESTEST 只讀核實六張功能表 DDL、index、constraints、triggers：FGD Inventory Component、FGD In Basis/Detail/Component、Transaction State/Log；六表各只有 PK unique index，無 FK、額外 index 或 trigger。TBLINVFGDINVENTORYBIN 不存在，不列為本頁依賴。
- [x] 完成 Vue DEV `/YMES/WIP/FgdInventory`、三層選單與動態 Bin 欄位、Add／逐筆 Del、FGD／SEMI／WIP 轉庫和 FGD In Confirm 假資料互動。
- [x] 完成 [WP605 規格](docs/modules/WIP/成品庫帳務維護.md)、Oracle source DDL、PostgreSQL CREATE-only、模組索引、資料庫連線查證紀錄與家用端 API/schema handoff；Production API／migration 由家用端接手。
- [ ] 本輪未執行 build、test 或 browser；完成靜態檢查、更新交接並 push 後接續下一個 WIP 功能。

### WP603 在製品庫帳務維護已交付（FunctionNo WIP INV，2026-11-19）

- [x] 依共用 WinForms／Designer、Service／Core 對照 Query、8 個可見分頁、Grid 欄位、列選取、按鈕及跨表交易；THEIL 專用 Form 原始碼缺於目前 checkout，已在規格標明此證據限制。
- [x] PowerShell 5.1 + Oracle.ManagedDataAccess 受保護唯讀連線查得九張 MESTEST 功能表與索引／constraints／triggers metadata；未讀業務列或執行 Oracle DDL/DML。
- [x] 完成 Vue DEV `/YMES/WIP/WipInventory`、三層選單及 8 個可見分頁的假資料互動，並建立 [WP603 規格](docs/modules/WIP/在製品庫帳務維護.md)、Oracle source DDL、PostgreSQL CREATE-only、模組索引與 API/schema handoff。
- [x] 公司端未連 PostgreSQL，未執行 build、test 或 browser。WP629 在製品庫包裝作業 DEV 與家用端 handoff 已完成。

### WP604 半成品庫帳務維護已交付（FunctionNo SEMI INV，2026-11-19）

- [x] 對照 `frmInventory_SEMI`、`frmInventory_SEMITurn`、`frmINVSEMIInConfirm` 的 Form／Designer、wsINV／wsWIP／wsSYS 與 clsInventory／clsLot；核對查詢欄位、Component/Bin 切換、SEMI In 交易種類、Basis Source 與 WIP/FGD/SEMI 轉庫副作用。
- [x] PowerShell 5.1 + Oracle.ManagedDataAccess 受保護唯讀連線取得四張 SEMI 專用表 DDL、索引／constraints／triggers metadata；四表只有 PK unique index，無 FK／額外 index／trigger。未讀業務列、未執行 Oracle DDL/DML；SEMI Inventory 主表與交易 State/Log 沿用先前 reference。
- [x] 完成 Vue DEV `/YMES/WIP/SemiInventory`，含庫房 lookup、庫存查詢、選列即時 Component/Bin 明細、未確認 SEMI In 假確認、多 Lot/Component 轉庫、目的庫與容量驗證；建立 [WP604 規格](docs/modules/WIP/半成品庫帳務維護.md)、Oracle source DDL、PostgreSQL CREATE-only、模組索引及家用端 API/schema handoff。
- [x] DEV 假資料存於專屬 localStorage，不接 API／資料庫；正式庫存／Lot transaction 與特殊 `SEMI_WAFER` 分支仍由家用端依規格接手。公司端不連 PostgreSQL，未執行 build、test 或 browser。

### WP601 包裝作業已交付（FunctionNo PACKING_MOLINE，2026-11-19）

- [x] 對照 `frmBR_L_CO_Packing_MOLine`／PackConfig WinForms 與 Designer、`wsWIP`、`clsLot.ExePacking*`；整理畫面欄位、設定、Lot/包裝掃碼、滿箱/強制封箱、拆批、刪除、標籤、Combine/Unboxing 與跨表副作用。
- [x] 使用受保護 Oracle profile + PowerShell 5.1/ODP.NET 直連 MESTEST 唯讀取得 19 張 WP601 功能表 DDL 與 constraints/indexes/triggers metadata；逐表欄位數對上 ALL_TAB_COLUMNS。`TBLWIPMOLINEPACKL` 通用前綴物件不存在；Relation 與 PackMergeLog 沒有 PK，未猜鍵。
- [x] 完成 Vue DEV `/YMES/WIP/PackingMOLine` 與 WIP → 包裝與庫房 → 包裝作業三層選單、PackConfig、階層/明細/標籤/配件、Pack Close、Combine、Unboxing、Delete 假互動；建立 [WP601 規格](docs/modules/WIP/包裝作業.md)、Oracle source DDL、PostgreSQL CREATE-only 與 metadata。
- [x] 家用端 API/schema handoff 已寫入 `todolist_homepc.md`。公司端未連 PostgreSQL，未執行 build、test 或 browser；標籤 COM 與 Combine/Unboxing 外掛正式整合留待家用端依規格處理。

### WP629 在製品庫包裝作業已交付（FunctionNo PACKING_MOLINE_INV，2026-11-19）

- [x] 已對照 `frmBR_L_CO_Packing_MOLine_INV`、`frmMOLinePackConfig`、`wsWIP.ExePacking`／`ExePacking_Close`／`ExePacking_Delete`、`clsLot` InlinePackingFlag 分支及選單 FunctionKey WP629。
- [x] Vue DEV 已加入 WIP → 包裝與庫房 → 在製品庫包裝作業，重用 WP601 版面，獨立 localStorage 假資料；Status=11、InventoryNo 同庫驗證、已包裝批與封箱後狀態保留均按來源規則模擬。
- [x] 完成 [WP629 規格](docs/modules/WIP/在製品庫包裝作業.md)、模組索引、Oracle/reference 重用紀錄及 `todolist_homepc.md` API/schema handoff。WP629 無新專屬表；使用 WP601 包裝 DDL 與 WP201 `TBLINVWIPINVENTORY_SEMI` reference，不重複 CREATE SQL。
- [x] 公司端未連 PostgreSQL，未執行 build、test 或 browser；未在 Oracle 執行 DDL/DML。其後已接續完成 WP301 設備上料。

### WP301 設備上料已交付（FunctionNo EQP MOUNT MATERIAL，2026-11-20）

- [x] 對照 `frmEQPMaterialLoading` Form／Designer、`wsWIP_Material` 與 `clsMaterialHandler`；核實設備與 MO 查找條件、三種上料模式、階層現況、CheckMTL、Mount／Unmount、Component map 及 Seq 交換。
- [x] 受保護 Oracle profile + PowerShell 5.1／ODP.NET 唯讀取得四張未有既有 reference 的 MESTEST 表 DDL／metadata：`TBLWIPEQPMATERIALSTATE`、`TBLWIPEQPMTLSTATE_COMP`、`TBL_BINKLESSMAPSTATE`、`TBL_BSUBSTRATEMAPSTATE`。四表各只有 PK unique index，無 FK／額外 index／trigger；未讀業務列、未執行 Oracle DDL/DML。
- [x] 完成 Vue DEV `/YMES/WIP/EquipmentMaterialLoading`、三層選單與 [WP301 規格](docs/modules/WIP/設備上料.md)。Oracle source DDL／metadata／PostgreSQL CREATE-only 在 `database/reference/mestest_wip_wp301_*`；另 13 張相依表沿用已存在 references。
- [x] DEV 支援物料、物料(元件)、半成品輸入、設備／MO 查找、上下移序與下料確認，使用獨立 localStorage 假資料。家用端待核對 PostgreSQL live schema，實作庫存扣還、設備現況、元件映射及序號交易 API。
- [x] 公司端未連 PostgreSQL，未執行 build、test 或 browser；本輪 Oracle 僅讀 metadata／DDL。

### WP302 載具現況管理已交付（FunctionNo CARRIER STATE，2026-11-20）

- [x] 直接對照 `frmCarrierState` inline Designer／事件、`wsEQP`、`wsWIP`、`wsINV`、`clsCarrierBasis`、`clsLot`、`clsInventory`；核實上下層 Grid、Put In／Takes Out／Clear Carrier 與兩個預設隱藏且受權限控管的狀態按鈕。
- [x] 使用受保護 Oracle profile，以 Windows PowerShell 5.1／ODP.NET 直連唯讀取得 `TBLWIPCARRIERSTATEDETAIL`、`TBLWIPCARRIERSTATEDETAILLOG`、`TBLINVSTOCKERSTATE` DDL／metadata。確認 Detail 無 PK、僅 ComponentNo 非唯一索引；DetailLog 無 PK/index；StockerState 有三欄複合 PK；未讀業務資料、未執行 Oracle DDL/DML。
- [x] 完成 Vue DEV `/YMES/WIP/CarrierState`、設備與載具三層選單、假 Lot／Component／Stocker 資料、放入／取出／清空確認、容量／位置／批次狀態驗證、權限控制區及獨立 localStorage；完成 [WP302 規格](docs/modules/WIP/載具現況管理.md)、Oracle source DDL、metadata、PostgreSQL CREATE-only 與 home TODO。
- [x] 按來源保留多元件逐筆放入的部分成功語意、取出整個 ContentID、Clear 前 lot-state 驗證、隱藏狀態按鈕；將 Clear 多列可能留 CarrierState=1 的 Core 行為記為 legacy quirk，沒有暗中修正。
- [x] 公司端未連 PostgreSQL，未執行 build、test 或 browser；資料表比較、API、migration 與部署由家用端依 `todolist_homepc.md` 接手。

### 公司端工作範圍

- WinForms／Service／Core 原始碼、Vue DEV 假資料、模組規格與家用端 API／SQL 交接。Vue 環境為 Node.js 22.12+、npm 與 `web/` 套件，筆電沿用自己的既有工具，不依賴桌機的本機安裝路徑。
- Oracle MESTEST 使用筆電自己的本機受保護 profile，以 Windows PowerShell 5.1 + ODP.NET 唯讀查 DDL／資料字典。桌機 DPAPI 設定繫結桌機帳號，筆電應使用原有安全設定。沒有 profile 時再安全設定，勿將連線資料寫進 Git 或交接檔。
- 正式 PostgreSQL、`ConnectionStrings__YMes`、.NET API、migration、SSH 與部署由家用端處理。每完成一項，做靜態檢查、更新待辦並單獨 commit，GitHub 可連時再推送。

## 家用端交付及公司端待補來源（2026-11-19）

### OP103／OP BASIS 公司端來源補件（家用端追加）

- [ ] 讀原 `frmOPBasisDef` 的 OPType 選擇與服務呼叫，確認是否有狀態／ValueList／其他篩選，提供具體 Form→Service→Core 與條件。現有證據未證明只允許已核准，家用端只做真類別存在性／唯一性與引用鎖，不猜 active-only。
- [ ] 補 `OP_OPProperty` 的 Form／Designer／Service／Core、Element 候選、Controlled／值驗證、簽核與共享OPNo owner規則。8欄Type及11欄共用Property的原生DDL已有，不需再造表或讀業務資料；家用端已按來源建立空表，僅實作Type硬刪所需精準Property清理，維護provider仍未交付。
- [ ] UI 後續接線保留真正 nullable Type 與 DataStamp，不將 NULL 版本改為0；保留伺服器回傳的 canonical key／新版本、頁內確認及逐筆部分成功。公司端繼續使用 API／DEV 假資料，不需要 PostgreSQL 或 SSH。

### QC201／QC202 公司端來源補交（2026-11-19，家用端追加）

此節只列仍缺少的原生程式／系統 metadata 證據。QC201／QC202 API 已部署與實測範圍見各模組 MD，不代表 GUI 或尚缺 provider 已驗收完成。公司端唯讀追查 WinForms → Service → Core、必要的 Oracle 結構及系統選項 metadata，補到既有模組 MD 並附檔案、方法、行號／來源版本；不讀取或匯出 MES 業務列，不在 Oracle 執行 DDL/DML，也不連 YMES PostgreSQL、使用 PPK/SSH 或部署。家用端繼續負責 API、migration、權限與驗收，不把這些工作轉交公司端。

QC201 主表／四張直接子表、QC202 `TBLSPCATTLIMIT`、QC103/QC107/QC108 及 `TBLSYSMESPARAMETER` 16 欄的真實 DDL／參照均已在 repo；不必重查或重交 CREATE SQL。以下 provider 缺口不等同缺表，也不能用空表、DEV 假選項或另一個表單的規則代替。

- [ ] **QC201 刪除與停用的真實 WIP 檢查：** 從 `wsQC`／`clsQCBasis.DelQCItemForAttributes` 追完整引用檢查呼叫鏈，交付實際 WIP table/view、查詢 SQL／參數、join 與 QCItemNo 對應欄位、`QCType`／`QCItemType` 的計數範圍，以及目前／歷史／已失效記錄的判斷。確認此檢查在 0/-1 實刪及 2→-1 停用各分支的執行順序，並列原生其餘物料／作業站／產品／群組／SPC 引用檢查與清理順序。不要虛構 `tblwipqcitem`，也不要把查無已知表解讀成「沒有引用」。僅當實際追到 repo 尚無結構的真物件，才唯讀補該物件的 DDL／欄位／約束；已有 DDL 不重交。交付至 `docs/modules/QC/計數品管項目.md`。
- [ ] **QC201 已核准主檔的 IssueControl 欄位：** 補 `frmQCItemForAttributesDef`、共用 `ExeIssueControl`／服務端 Edit 路徑的完整控制來源，列出 state2 每個可改／唯讀欄位、判斷用 FunctionNo／IssueNo／控制設定鍵及缺設定時的行為，確認 Service/Core 是否重驗。另核實 Parameter／Factor／Reason／SamplePlanChange 手動維護各狀態的允許規則；不要把 QC215 匯入的 ParameterClass=1 條件當成手動 Factor 規則。家用端目前採明載的 state0 保守寫入及已證實 state2 Op 規則；取得來源後由家用端調整。交付至 QC201 既有模組 MD。
- [ ] **QC201 CopyParameters=false 的初始化分支：** 追 Copy Form → Service/Core 在不勾複製 Parameter、且目標 Sampling Plan 有參數時，究竟建立 Plan 預設值、保留空集合、要求選來源或採其他分支；交付實際方法、參數來源及交易順序。並核實項目／Plan 參數不一致時的選擇、取消、缺值與 NULL 行為，說明四個 copy checkbox 各自如何影響初始化。不要因未勾選就假設成功建立空參數設定。交付至 QC201 既有模組 MD。
- [ ] **QC201／QC202 共用 MES Parameter Reference 契約：** 沿 `wsSYS.LoadMesParameterRef`／`LoadMESParameterRef` → `clsSYSMesParameter` 核實 `ParameterType` 完整 enum（已知 0=Table、2=自由文字；補 1／其他／未知值的真實處理），以及 TableName／FieldName／Condition／FunctionName／ShowName 的實際語意、參數、回傳欄位與顯示值、大小寫／NULL／空值／`*` 規則。提供原生允許的 table/column/function 清單及查找篩選，說明核准狀態與 ParameterClass 條件在哪個 caller 生效，讓家用端依可信 allowlist 實作；不要要求 API 任意執行 metadata SQL 或 FunctionName。`TBLSYSMESPARAMETER`／三張 FC 表結構已有，無需重交 DDL 或把 provider 實作推給公司端；補到 `docs/modules/QC/要因資料維護.md`、`特殊主檔維護.md`，QC201／QC202 MD 引用同一份契約。
- [ ] **QC201 ReasonType 真正 ValueList：** 核實 `frmQCItemReason` 的 ValueList 呼叫鍵／owner、語系、排序、代碼／標籤與缺值行為，以系統 metadata 交付；若與 QC105/QC106/QC107 共用，明確列出同一來源及各 caller 的篩選，避免重做字典或借用其他表單的硬編碼選項。本檔既有 QC107 Action／ReasonType 待辦仍保留其自身範圍。`TBLQCITEMREASONMAP` 只存 ReasonNo，ReasonType 是原因主檔／篩選資料；缺字典不應阻止真實已核准 ReasonNo 對應。交付至 QC201 MD，並由相關模組引用。
- [ ] **QC201 SpecialForm 目錄：** 補主定義窗 FormName／ExecutionFile 的真實選擇器、目錄 provider／介面篩選、欄位對應、有效性驗證與缺項時的原生行為，確認既有值保留和新增／變更的差別；不以掃描任意檔案或 DEV 文字清單替代正式來源。交付至 QC201 MD。
- [ ] **QC201 Simulation 原生計算來源：** 補 `QC_SamplingParameterSimulation` 對 `PlanOrQCItem=1` 的方法簽章與完整 Core／外部 DLL 來源、版本與相依，核實 QCType、ComponentCollect 一般／Wafer 分支、Plan／QC Item 參數的優先順序及輸出／錯誤契約。沿用本檔既有 QC103 Simulation 待辦提供共用算法的合成輸入、來源可核對的期望輸出及邊界案例，不另做重複算法清單；QC201 只補其 caller 差異。若只有 binary 或來源不存在，明確標示，不用近似公式或 DEV 假結果冒充。交付至 QC201／QC103 既有模組 MD。

## 家用端最新交付及公司端接續（2026-11-19，QC103／QC107／QC108）

- [x] QC103/QC107/QC108 正式獨立主檔已部署並完成模組 MD 所列 app-browser 操作；請保留 nullable 版本、原子 aggregate、來源狀態規則和共用確認窗。完整共用簽核／算法／下列來源依賴仍未完成。
- [x] QC301 使用 QC108 真實核准目錄；QC301/QC302 預設法則及引用鎖已有正式 API/DB 回歸。QC401/QC402 真實 XLSX 選檔預覽與部分成功已驗證，YMES 標準空白範本已可下載；不再以缺少舊範本 binary 阻擋一般匯入。
- [x] QC107 已核對 ValueList provider、語系、排序、Oracle 主鍵與 UserDefineAQL 的缺值行為。見 [QC107 來源查證](docs/modules/QC/計數判別法則基本資料.md#公司端-valuelist-與開關來源查證2026-11-19)。
- [x] QC108 核對標準 FUNQR 原始碼與公司端 DLL 靜態識別字。四個方法在標準 Core 均回傳 null，客製可執行版本仍待交付。見 [QC108 來源查證](docs/modules/QC/計量品管法則基本資料.md#公司端-funqr-來源查證2026-11-19)。
- [x] QC103 已核實 FUNSA 名單、Type 4/6 ShowFlag、Type 6 Core 與母體上限旗標、QC301 一般／Wafer 分表條件。見 [QC103 來源查證](docs/modules/QC/抽樣計劃.md#公司端-formcore-與函式目錄查證2026-11-19)。
- [x] QC103 DEV 定義窗已套用核實的預設旗標及 FUNSA 名單，保留舊 DEV 參數值，`vue-tsc --noEmit` 通過。
- [ ] 若須與原 MES 範本完全相容，再提供 MCLASS/SCLASS 原始 binary 與 SYS template metadata；目前 YMES 標準 .xlsx 範本是已記錄的產品決策，非原 MES 匯出檔。
- [ ] 持續按照本檔完成 Oracle 來源／UI handoff。家用電腦負責 YMES PostgreSQL、API、SSH 部署與正式瀏覽器驗收；公司電腦不需取得 PostgreSQL 或 SSH 連線。

- [x] QC101/QC102/QC401/QC402/PS101正式API及主要頁面操作已部署驗證，模組MD已補實作、版本與限制；公司端pull保留nullable版本條件與共用確認窗。QC101/QC102 Apply保持連續區間新增；QC401/QC402/PS101 Apply轉Edit，依各自來源／YMES明確決策保持一致。
- [ ] 公司端補交 MCLASS BASIS／SCLASS BASIS 原始Excel範本binary和SYS template metadata，勿以DEV CSV代替。家用端正式匯入API已通，瀏覽器選檔驗收仍列家用端待辦。
- [ ] QC107 尚待授權來源提供 Action／ReasonType 實際字典值與對應標籤，以及 UserDefineAQL 部署值；現行 API 在來源空缺時回 503。
- [ ] QC103 尚待 SYS ValueList／ElementValue 實際可用選項，以及各型別正式 Simulation 回歸的樣本與期望結果。
- [ ] 公司端確認QC101 legacy AdditionalXml helper數值/識別字查詢規則，以便比較目前已明確記錄的YMES literal contains搜尋契約。

## 家用端交付：QC503／PS102（2026-11-18）

- [x] 家用端已部署並完成正式 app-browser 驗收；QC503 共用 Driver API 與 PS102 製造層別 API 可供 UI 呼叫。模組文件已補實作、migration、按鈕、版本與驗證紀錄。
- [x] 核准／刪除共用站內確認窗；新增/複製 Apply 成功轉修改模式並使用 server DataStamp，避免重複新增。這是明確記錄的 YMES 互動改善。
- [ ] 公司端 pull 後保留以上 API/互動契約，繼續依本檔未完成 UI/Oracle DDL 交接；資料庫、API 與正式驗收仍由家用端負責。

## 2026-11-18 家用端整合補正：PRD Vue 型別

- [x] 修正 `ProductBasisMaster` 與 Parameter/Business Rule 子元件的結構型別；BOM lookup 使用共用 key/label/row；OPDescription 正規化 OPName/audit；Parameter draft 保存明確保留識別鍵並排除 recipeDetails；Property 在有效值驗證前初始化 draft。沒有新增 broad any cast，亦未新增正式 API/schema。
- [x] 本輪 `npx.cmd vue-tsc --noEmit` 與完整 `npm.cmd run build` 均通過；維護細節已補至 PRD 對應五份模組文件。
- [x] 本機 DEV app-browser PRD 冒煙檢查通過：產品主檔、Property 清單/新增視窗、Parameter 樹、Business Rule 圖、OPDescription 稽核欄位可載入；BOM 右鍵 Add→Product→lookup→選取 `DEV-PRD-1002` 及 Cancel/Close 正常。所有相關視窗可關閉，未出現 TypeError/ReferenceError/Unhandled 紀錄。
- [ ] 本輪不是完整 CRUD/正式 API 驗收；公司端 pull 後沿用既有正式唯讀/DEV 假資料邊界，後續正式 API/schema 與完整 CRUD 仍依模組文件 handoff 接續。

## 2026-11-18 公司端進度：PS102 製造層別設定

- [x] 閱讀 `frmFABPRSStageBasis`/Def Form 與 Designer、`wsPRS.Load/Add/Edit/Delete/ApproveFABPRSStageBasis`、`kcBasis_Query`、`kcPRS.clsProcess`、`kcBasis_UPD`；整理欄位、四狀態、Sequence 唯一、Apply/Confirm、刪除與 IssueManagement 流程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 Oracle 設定唯讀直連 MESTEST，查 `TBL_FABPRSSTAGEBASIS` 的 DBMS_METADATA DDL、欄位/default、constraints/indexes 和獨立 PK/Sequence index DDL。兩個唯一索引由 StageNo PK 與 Sequence unique constraint 建立；沒有 FK/額外 index。未讀業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。
- [x] 完成 `StageBasisMaster.vue`、PS102 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、PRS 三層選單路由與家用端 schema/API handoff。DEV 假資料限 `ymes.dev.ps102-stage-basis.v1`。
- [ ] 尚未連 YMES PostgreSQL，未實作 API/migration；未執行 build、test 或 browser 驗收。完成本次 PS102 commit/push 後，依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續下一個尚未完成 FunctionNo。

## 2026-11-18 公司端進度：PS101 生產區段資料

- [x] 直接閱讀 `frmProductionSegment`/Def Form 與 Designer、`wsPRS.Load/Add/Edit/Del/ApproveProductionSegment`、`clsProcess`、`clsBasis_UPD.SetProductionSegmentIssueState`、`wsSYS.LoadSerialRuleBasis`/Core；核對列表、四狀態、Copy/Apply/Confirm、刪除/簽核、Lot serial lookup 與隱藏欄位。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線直連 MESTEST，唯讀取得 `TBLPRSPRODUCTIONSEGMENT` 的 `DBMS_METADATA.GET_DDL`、欄位/default、constraint/index metadata，並另外取得 PK index DDL；未讀業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。僅 PK unique index，無 FK/其他 index。
- [x] 完成 `ProductionSegmentMaster.vue`、PS101 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、PRS 三層選單路由及家用端 API/schema handoff。PS104 改為重用 PS101 的 `tblprsproductionsegment`，避免重複建立。
- [x] DEV 假資料在專用 localStorage；有四狀態、假資料直接核准/送簽兩種分流、定義窗及 LOTNO 選擇視窗。無 MES/ YMES 寫入。
- [ ] 尚未連 YMES PostgreSQL，未實作 Production API/migration；未執行 build、test 或 browser 驗收。PS102 公司端 DEV 頁與 DDL/API handoff 已在本輪完成；接續依 inventory 排定下一個未完成 FunctionNo。

## 2026-11-18 公司端進度：PS103 流程類別資料

- [x] 直接閱讀 `frmProcessType`/Def、Designer、`wsPRS.Load/Add/Edit/Del/ApproveProcessType`、`clsProcess`、`clsBasis_UPD`，以及共用 `PS_ProcessProperty` 子功能；確認列表欄位、四狀態、Apply/Confirm、ProcessNo 衝突、子資料刪除、簽核 IssueNo 和 DataStamp。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護 Oracle 設定唯讀取得 `MESTEST.TBLPRSTYPE` DBMS_METADATA DDL、欄位/default、constraint/index；未讀業務資料、未執行 Oracle DDL/DML、未輸出連線秘密。來源僅有 PK unique index。
- [x] 新增 `ProcessTypeMaster.vue`、PS103 規格、Oracle source DDL、PostgreSQL CREATE-only 參考、三層選單路由與家用端 schema/API handoff；Property 共用 API 和 PS104 ProcessType lookup 相依已註明。
- [ ] 尚未連 YMES PostgreSQL，未實作 Production API/migration；依規則未執行 build、test 或 browser 驗收。推送完成後依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續下一個未遷移功能。

## 2026-11-18 公司端進度：PS104 流程基本資料

- [x] 直接閱讀主/Def/Draw/Exclude OP WinForms、Property 子功能、Data Import Form、wsPRS 和 clsProcess，整理欄位、狀態、核准、版本、複製、刪除、流程圖及匯入行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線直連 MESTEST，只讀查九張表的 DBMS_METADATA DDL、欄位、constraints/indexes；未查業務列、未在 Oracle 執行 DDL/DML、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照、PS104 規格和家用端 handoff。九張表只有 PK unique index，沒有 FK/額外索引。first-phase 三表 default 差異及新增表責任已依 PS103/PS101/PS104 分拆寫入交接。
- [x] 新增 Vue DEV 流程基本資料頁：狀態查詢、欄位 Grid、Add/Edit/Copy/Approve/Delete/Property/Version/Exclude OP、簡化節點/連線編輯及 Import 視窗。假資料存在獨立 localStorage。
- [ ] 流程圖 XML serializer、群組/分支拖曳編輯與舊版格式相容待確認；Excel checker 外部 DLL 未找到原始碼，DEV 只保留選檔視窗，不宣稱匯入完成。
- [ ] 未連 YMES PostgreSQL、未實作 API/migration；未執行 build/test/browser。
- [ ] 家用端完成 API/schema 並 push 後，公司端 pull、對接 Production API。PS103 與 PS101 的公司端 DEV 頁及交接已完成；PS102 公司端 DEV 頁與 DDL/API handoff 已於本輪完成；接續依 inventory 排定下一個未完成 FunctionNo。

## 2026-11-18 公司端進度：OPB02 ECN 作業站群組設定

- [x] 直接閱讀 `frmOP_B_ECNOPGroupBasis`/Def、Designer、`wsOP.Load/Add/Edit/Del/ApproveOPECNGroupBasis`、`clsOpBasis` 與 DataStamp update；確認主/明細雙 Grid、狀態篩選、Copy 僅複製主檔、OP lookup 限制、逐筆多選與簽核 Job 分流。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護 MESTEST 連線唯讀取得 `TBLOPECNGROUPBASIS`、`TBLOPECNGROUPDETAIL` 的 `DBMS_METADATA.GET_DDL`，並查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`。主鍵分別是 `OPGROUPNO` 與單欄 `OPNO`；只有 PK unique index，沒有 FK/額外索引；未讀業務列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照、`docs/modules/OP/ECN作業站群組設定.md`；更新規格索引、資料庫連線紀錄及家用端 API/schema handoff。
- [x] 完成作業站資料 → 行業包 → ECN 作業站群組設定 Vue DEV 頁 `/YMES/OP/ECNOperationGroups`：四狀態篩選、主/明細 Grid、CRUD/Copy/核准/刪除、PS/OP 選擇、稽核欄、DataStamp 欄位及 KD 黃色輸入欄；假資料僅 DEV localStorage。
- [ ] 家用端尚未比對 YMES live schema、實作 Production API/migration；公司端未連 YMES PostgreSQL，未執行 build、test、browser。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 的下一個尚未完成 FunctionNo，不要重做已交接功能。

## 2026-11-18 公司端進度：OPB03 客戶站點對應設定

- [x] 直接閱讀 frmB_OPCustomerOPMapping、主檔/明細 Def Forms 與 Designer、wsOP、clsOpBasis、clsB_Basis_UPD；確認雙 Grid、lookup 狀態、Add/Edit/Copy/刪除、DataStamp、Audit 與批次刪除行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 TBL_BOPCUSTOPMAPBASIS、TBL_BOPCUSTOPMAPDETAIL DDL，並查 ALL_CONSTRAINTS/ALL_INDEXES；兩表只有 PK、沒有 FK/額外索引。未讀 MES 業務資料、未執行 Oracle DDL、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 參照與 docs/modules/OP/客戶站點對應設定.md，更新規格索引、Oracle 查詢紀錄與家用端 API/schema handoff。
- [x] 完成「作業站資料 → 行業包 → 客戶站點對應設定」Vue DEV 頁 /YMES/OP/CustomerOPMapping：上下雙 Grid、查詢、多選刪除、主檔 ADD/EDIT/DEL/Copy、明細 ADD/EDIT/DEL、Customer/PS/OP lookup、Audit 顯示與 KD 黃色欄位；假資料限 DEV localStorage。
- [ ] Oracle CustomerOPSeq NUMBER(3,0) 與舊 NumericUpDown 最大 999999 不一致，已於規格和家用端 handoff 標明；Production API 依 live schema 驗證，沒有擴欄或截斷。
- [ ] 未連 YMES PostgreSQL、未實作 Production API/migration，未執行 build、test、browser。OPB03 已交家用端 API/schema；公司端接續功能依 docs/MES_MENU_FUNCTION_INVENTORY.md，下一項 OPB02 ECN 作業站群組設定。
# 公司電腦續作清單

## 2026-11-18 公司端進度：OPB01 作業站參數群組設定

- [x] 直接閱讀 `frmOPGroupAttrib`/Designer、`frmOPGroupAttribDef`/Designer、`wsOP.Load/JoinElement/Save/DelOPGroupAttrib`、`clsOpBasis`、`clsB_Basis_UPD.SaveOPGroupAttrib` 與 `wsSYS.ChkElementValueValid`；確認三欄精確查詢、整組多列編輯、DataFlag、Copy、InputType、Sequence、BYSECONDCLASS 對應與 DataStamp。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 MESTEST 連線唯讀取得 `TBLOPGROUPATTRIB`、`TBLSYSELEMENTBASIS` 的 `DBMS_METADATA.GET_DDL`、ALL_TAB_COLUMNS/CONSTRAINTS/INDEXES/IND_COLUMNS；OP 主表僅複合 PK，無 FK/額外索引。未讀業務列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 OPB01 Oracle source DDL、PG CREATE-only 參考、功能規格及本段家用端 API/schema handoff。`TBLSYSELEMENTBASIS` 是既有共用表，本功能不重建。
- [x] 完成「作業站資料 → 行業包 → 作業站參數群組設定」Vue DEV 頁 `/YMES/OP/OperationGroupAttributes`：三條勾選精確查詢、Grid 多選刪除、Add/Edit/Copy，整組 Element 批次編輯、多選新增、InputType 下拉/Key In、AllowNull、Sequence、BYEQUIPMENT_TYPE、Apply/Confirm/Close、客戶/ECN 群組/Element lookup；localStorage 假資料限 DEV。
- [ ] 家用端依 `todolist_homepc.md` 核對 YMES live schema、接登入/權限及 lookup，實作 batch transaction、DataStamp CAS、InputAbortLog 和 SYS Element value validation API；完成後公司端 pull 對接。
- [ ] 未連 YMES PostgreSQL，未實作 Production API/migration，未執行 build、test、browser 或 CRUD 驗收。
- [ ] OPB01 完成此 UI/spec handoff 後，繼續從 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個未完成 Function；整套 MES 遷移尚未完成。

## 2026-11-18 公司端進度：QC408 OCAP基本資料

- [x] 直接閱讀 QC408 主/Def/流程繪製/節點 Form 與 Designer、`wsQC`、`clsQCBasis`，整理畫面、CRUD、核准、刪除、Copy、DataStamp、NodeType、群組 lookup、XML/節點/連線儲存規則。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀取得 `TBLQCOCAPBASIS`、`TBLQCOCAPNODEBASIS`、`TBLQCOCAPNODERELATION` 原始 DDL；確認 PK、欄位空值/預設值，只有 PK index、未見 FK 或額外 index。未讀 MES 業務資料、未執行 Oracle DDL、未輸出連線秘密。
- [x] 新增 Oracle source DDL、PostgreSQL CREATE-only 對照，並新增 `docs/modules/QC/OCAP基本資料.md`、索引、資料庫連線紀錄與家用端 API/schema handoff。
- [x] 完成 QC → 異常設定 → OCAP基本資料 Vue DEV 頁：四狀態清單、查詢、多選 CRUD/Copy/Approve/Delete、稽核列、流程預覽與簡化節點/連線繪製器，KD 黃色輸入欄；路徑 `/YMES/QC/OCAPBasis`。
- [ ] 流程編輯器是 AddFlow web 簡化版；舊 `iMESXMLFlow.dll` serializer 只有 binary，未完成 XML 格式相容與 nested group/Stage validation。Production API 前需由家用端決定相容策略。
- [ ] 未連 YMES PostgreSQL，未實作 API/migration，未執行 build、測試或瀏覽器驗收。家用端完成 API/schema 並 push 後，公司端 pull 再整合驗收。

## 2026-11-18 公司端進度：QC501 檢測儀器類別

- [x] Pull `origin/main`（Already up to date），直接閱讀 QC501 WinForms 主/Def Forms、Designer、`clsInspMachine` Load/Add/Edit/Delete/Driver 來源與共用 `ExeIssueControl`；不是依截圖臆測。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀取得 QC501–QC503 共用六表 Oracle DDL/PK index，產生 Oracle source DDL 與 PostgreSQL 空表 CREATE 參考；無業務列、未執行 Oracle DDL、無連線秘密輸出。
- [x] 建立 `/YMES/QC/InspectionMachineParameterType` Vue DEV 假資料頁、品質資料→檢測儀器→檢測儀器類別路由/選單、查詢狀態、WinForms 欄位與按鈕、RS232/UserDefine 編輯、Driver picker、複製、多選刪除/送簽、Audit 區及假資料操作。
- [x] 新增 `docs/modules/QC/檢測儀器類別.md`，更新模組索引、資料庫連線紀錄與 `todolist_homepc.md` schema/API handoff。
- [x] 家用端已部署共用六表 0027 migration 與 QC501 API；app-browser 已驗證新增/套用/修改、複製/Driver lookup、核准取消/確定、引用刪除 409、狀態 1 唯讀、-1/2 成功刪除、四狀態/Close、錯誤文字與 Demo 登入；fixture 已清空。公司端待家用端 GitHub 同步後 pull，以 server DTO/nullable DataStamp 對接；共用 Issue/Job、Issue Control 仍待整合。QC503 UI 已於公司端 `b572f3b` 提交，下一步為家用端 API handoff。

## 2026-11-18 公司端進度：QC502 檢測儀器基本資料

- [x] 直接閱讀 `frmInspMachineParameter`/Def、Designer、`wsQC`、`clsInspMachine` 的 Load/Add/Edit/Delete/Approve，追蹤類別 checkbox/lookup、類別參數複製、Driver lookup、RS232 必填與 IssueManagement Job 分流；非依截圖推測。
- [x] 確認 QC502 直接主表為 QC501 已取得 DDL 的 `TBLQCINSPMACHINEPARAMETER`；QC501 類別與 Driver 是 lookup。`TBLQCINSPMACHINEBASIS` 不在此 Form/Core 的 CRUD 寫入路徑；Property 按鈕 Designer 預設隱藏。本次無需重查 Oracle 或另建 CREATE SQL。
- [x] 新增 `/YMES/QC/InspectionMachine` Vue DEV 頁與品質資料→檢測儀器→檢測儀器基本資料路由；包含 IssueState/識別碼查詢、主 Grid、Add/Edit/Copy/Approve/Delete、類別帶入/本機 RS232/UserDefine Driver 表單、lookup modal、audit 與獨立假資料。
- [x] 新增 `docs/modules/QC/檢測儀器基本資料.md`，更新 module index、資料庫紀錄與家用端共用 API/schema handoff。
- [x] 家用端已部署 QC502 API，app-browser 已驗證新增/套用/修改、RS232→UserDefine 類別更換、複製/Driver lookup、核准、取消/確定刪除、nullable DataStamp 衝突與重查保存、四狀態/Close、錯誤文字及 Demo 登入；fixture 已清空。類別參數由 API 鎖定重讀，Apply 使用 server DTO。公司端待家用端 GitHub 同步後 pull 並確認 DTO/API 對接；QC503「檢測設備連線程式」UI 已於 `b572f3b` 提交，家用端接續 API。

## 2026-11-18 公司端進度：QC503 檢測設備連線程式

- [x] 直接閱讀 `frmInspDriver`/Def、Designer、`wsQC.Load/Add/Edit/Del/ApproveInspDriver`、`clsInspMachine`、`clsBasis_UPD` 與 `udDriverFunction.clsDriverFunction`；確認 Driver CRUD 的狀態/DataStamp、Issue Job 簽核、核准/刪除行為及三個可選函式名稱。
- [x] 新增 `/YMES/QC/InspectionDrivers` Vue DEV 頁及品質資料→檢測儀器→檢測設備連線程式路由。完成四狀態/DriverNo 查詢、Grid、Add/Edit/Copy/Delete/Approve、FunctionName 下拉、Description、Audit、多選與 localStorage 假資料。
- [x] 新增 `docs/modules/QC/檢測設備連線程式.md`、索引與資料庫紀錄，並更新 `todolist_homepc.md` API/schema handoff。主表 `TBLQCINSPDRIVER` 的 Oracle DDL/PG CREATE 已含在 QC501 共用六表 reference，本功能不需再查 Oracle或另建 SQL。
- [ ] 公司端未連 YMES PostgreSQL、未實作 API/migration，未執行 build、測試或瀏覽器驗收；家用端完成 API/schema 後再 pull 整合。接續功能依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個尚未完成項目。

> 目的：記錄家用電腦完成資料庫/API交接後，公司電腦接著要做的 WinForms UI、互動邏輯與規格文件。公司電腦先讀本檔；家用電腦先讀 `todolist_homepc.md`。

> **公司電腦工作邊界：** MES 原生 Oracle MESTEST 僅供本端唯讀查詢 DDL、資料字典與舊系統規則；不得在原生 Oracle 建 YMES 表或寫入業務資料。本端只開發 UI、按鈕互動與呼叫 `/YMES/API`；不連 YMES PostgreSQL、不使用家用端 PPK/SSH、不執行 migration、不遠端部署。資料庫或 API 需求寫入規格並交給家用電腦。

## 交接基準

- 專案：`D:\YMes`，GitHub `souaix/YMES`；來源 WinForms：`D:\iMES\CSiMES`。
- 公司電腦以 MESTEST 假資料完成 Vue UI 與前端互動；家用電腦負責 YMES PostgreSQL、migration、API 與部署。
- 開工前先 pull 最新 `main`，確認家用電腦已 push API/schema 變更，再對照本檔與 `todolist_homepc.md`。不要覆蓋另一台電腦尚未同步的修改。
- Oracle 來源 DDL 用 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢；使用本機受保護連線設定，不必請使用者重貼，也不得把連線值寫進文件、SQL 或 Git。
- 目前只查詢過 Oracle 結構與系統選單中繼資料，沒有匯出 MES 業務資料。不要把 DEV 假資料當成正式資料。

## 最新公司端工作：QC305 產品規格設定（2026-11-17）

- [x] 直接對照 `frmQCProductSpec` Designer/事件、`wsPRD.LoadProductType`、`wsQC.LoadProductBasisJoinPRDQC`、`LoadPRDQCItemJoinForVariables`、`LoadPRDQCItemJoinQCChart`、`UpdPRDQCChartData` 與 `clsQCBasis.cs`；記錄 ProductType/ProductNo 查詢、`QC_Count=1`、SPEC 載入、GroupNo、FactorSpec=PRODUCT、SPCID、MEMO、批次套用及舊 ProductNo-only 行為。
- [x] 新增 `/YMES/QC/ProductSpec` Vue DEV 頁及 `PRDQCITEM/QC305` 選單/路由；支援假資料查詢、SPEC 編輯、群組關聯識別、Chart filter、CS±Tolerance、剪貼簿貼上、規格驗證及批次假資料套用。
- [x] 建立 `docs/modules/QC/產品規格設定.md`，更新規格索引、資料庫連線/DDL provenance、家用端 API handoff。此功能沿用 QC302 Chart Basis/Detail/DetailLog 與既有產品/QC Item 表，沒有專屬 CREATE SQL；TBLPRDTYPE DDL 尚未保存，僅當家用端確認 YMES lookup 依賴缺漏才需由本機 Oracle 直連查詢。
- [ ] 本回合未執行 build、test 或瀏覽器驗收。家用端完成 API/schema 後 pull，對照有/無既有 Basis、產品群組綁定、批次部分略過及 ProductVersion/OPNo 歧義。
- [ ] 完成後依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續下一個尚未遷移的 WinForms 功能；維持 office fake UI/spec 與 home API/schema handoff 分工。

## 最新公司端工作：QC304 物料規格設定（2026-11-17）

- [x] 直接閱讀 `frmQCMaterialSpec` Designer/事件、wsQC、clsQCBasis；記錄 MaterialType/MaterialNo 查詢、Active 物料清單、SelectAll/UnSelectAll、SPEC 載入、QCItem FactorSpec 條件、ChartType/SPCID、CS/Tolerance、Paste、MEMO、完整數值/界限驗證與多物料套用。
- [x] 確認此功能維護 QC302 的 Chart Basis/Detail/DetailLog 及既有 Material/QC Item/Group tables，沒有 QC304 專屬新表。Oracle/PG DDL 沿用 QC302、MTL、QC201/QC205/QC212 references，未重查 Oracle 業務資料，也沒有新增 CREATE SQL。
- [x] 新增 `/YMES/QC/MaterialSpec` Vue DEV 頁與 QC304 三層選單/路由；完成查詢/選取假資料、點 SPEC 載入、圖表 filter、X-Rm mapping、CS±Tolerance、剪貼簿/文字框貼上、數值與界限驗證、MEMO、批次假資料保存。
- [x] 建立 QC304 功能規格、更新規格索引與 Oracle DDL 查詢 provenance，並加入家用端 API/schema handoff。
- [ ] 公司端未執行 build、test 或瀏覽器驗收。家用端完成 schema/API 後，pull 並對照不同 Material/QCItem 的 Chart Basis/Detail/DetailLog 與 group item 跳過結果。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接對照下一項尚未遷移的 WinForms Form/Service/Core。

## 前一項公司端工作：QC303 品管項目規格（2026-11-17）

- [x] 直接對照 `frmQCSpecBasis`、`frmQCSpecBasisDef`、`frmQCSpecCopy`、wsQC 與 clsQCBasis；記錄 QC Item、FactorSpec、四狀態、Detail SPCID、規格界限、Above helper、MEMO、簽核、刪除與跨 QC Item 複製行為。
- [x] 沿用先前已保存的 Oracle 直連唯讀 DDL `mestest_qc205_variable_dependencies_source_ddl.sql` 中 `TBLQCSPECBASIS`/`TBLQCSPECDETAIL`；本次沒有讀業務資料列，也沒有在 Oracle 執行 DDL。
- [x] 新增 `/YMES/QC/SpecBasis` Vue DEV 頁、品質資料→計量規格設定→品管項目規格選單/路由、QC303 規格文件與 PostgreSQL CREATE-only JSONB 對照。
- [x] 更新規格索引、Oracle DDL provenance 記錄、家用端 schema/API/Issue Job handoff。
- [ ] 公司端尚未執行 build、test 或瀏覽器驗收；禁止因此宣稱 Production API 已完成。家用端完成 schema/API 並 push 後，公司端 pull 對接、依 API 核對 QC Item/Factor lookup 及簽核狀態。
- [ ] 接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接看下一項尚未遷移 WinForms Form/Service/Core；不以截圖替代原始碼調查。

## 前一項公司端工作：QC301 管制圖內容（2026-11-17）

- [x] 直接閱讀 `frmQCChartBasis`、`frmQCChartBasisDef`、`frmQCChartCopy`、Rule Define、wsQC 與 clsQCBasis；整理 QC Item、狀態篩選、動態 Factor 欄、Detail/Rule、簽核、刪除、跨 QC Item 複製與 sampling parameter 流程。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀取得 `TBLSPCQCCHARTSAMPLANPARAMETER` DDL/index。Chart Basis/Detail/DetailLog/Rule DDL 沿用 QC302 reference；沒有讀業務資料列或在 Oracle 執行 DDL。
- [x] 建立 `/YMES/QC/ChartBasis` Vue DEV 頁，對齊三層選單、QC Item/Chart Grid、四種狀態、Detail/Rule tabs、Define/Cross Copy/Rule modal、表單驗證、假資料 CRUD/簽核/刪除與稽核。
- [x] 新增 `docs/modules/QC/管制圖內容.md`、Oracle/PG sampling parameter reference，更新資料庫查詢紀錄、規格索引與家用 handoff。
- [ ] 家用端依 `todolist_homepc.md` 先查 PostgreSQL live schema/migrations，再完成 QC301 API/schema/Issue Job；Production API 完成後公司端 pull 對接與後續 UI 調整。
- [ ] 公司端本段未執行 build、test 或瀏覽器驗收；接續沿 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接查看下一項 WinForms Form/Service/Core。

## 目前停留點（2026-11-17）

### QC302 匯入管制圖內容（公司端 2026-11-17）

- [x] 閱讀 `frmImport_QCChartBasis` Form/Designer、`wsQC.ImportQCChartBasis`、`clsQCBasis.ImportQCChartBasis`、Get/Add/Edit ChartBasis 與 Upd Chart Rule；整理格式化選項、第一工作表/第 2 列跳過、全列 Save、SPCID 0–4 圖表界限、每列 Execute、FactorSerial、重複圖表更新與明細歷程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 和本機受保護設定唯讀查四張實際寫入的 SPC QC Chart Oracle table/index DDL；沒有讀業務列或在 Oracle 執行 DDL。建立 `mestest_qc302_spc_qc_chart_source_ddl.sql` 及 PostgreSQL schema-only CREATE 參考 `mestest_qc302_spc_qc_chart_create.sql`；Factor 動態欄位以 JSONB handoff。
- [x] 新增 `/YMES/QC/ChartDataImport` Vue DEV 頁、品質資料→計量規格設定三層選單、格式化欄位、動態 Grid 編輯/刪列、假資料逐列 Save/結果回報。
- [x] 新增 `docs/modules/QC/匯入管制圖內容.md`，記錄程式來源、欄位與 Core 驗證/更新行為、API 契約及 Oracle/PG DDL；更新規格索引、DB 查詢紀錄與家用 TODO。並將 QC214 先前的 Oracle Chart table 拼字疑義修正為已確認名稱，仍要求家用端核對 YMES live schema。
- [x] 家用端已部署 QC302 migration `0024_qc302_chart_data_import.sql` 與登入保護 API：Preview `.xls/.xlsx`、全列 Import、JSONB 因子、逐列交易/結果、圖表 basis/五種 detail/history/rule 寫入；API health=200。角色/FunctionKey ACL 依使用者決定暫緩。
- [ ] 公司端待家用端提交/推送後 pull 最新 `main`，再用正式頁面確認 API Preview/Import 回應與資料欄位映射。匯入目前會因 YMES 尚缺共用 `TBLQCITEMRULE`、`TBLQCRULEBASIS` 而拒絕新圖，傳入 PlanNo 且缺 `TBLQCSAMPLINGPLAN` 亦拒絕；不可把缺表視為成功。
- [ ] 正式範本 binary/storage 尚未移轉，`GET /template` 暫回 503；待取得 MES `QCChart_Template.xls/.xlsx` 與模板儲存規則後再驗收下載。app-browser 本回合導回登入頁，需在登入狀態下完成 QC302 操作驗收。
- [ ] API 部署後公司端可接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接讀取下一個未遷移功能的 WinForms Form/Service/Core。

### 前一項：QC215 計數品管項目匯入（公司端 2026-11-17）

- [x] 直接閱讀 `frmImport_QCItem_Attributes` Form/Designer、`wsQC.ImportQCItem_Attributes`、`clsQCBasis.ImportQCItem_Attributes` 與 `DelQCItemForAttributes`；整理 1008×591 Grid/按鈕、Excel 欄位解析、第 2 列跳過、勾選列 Check/Save、逐列錯誤、計數 QC Item 子資料重建、抽樣計畫/Rule/Factor/OP/Reason 驗證與外部引用規則。
- [x] 確認 QC215 沒有專用資料表，不新增 CREATE SQL。沿用已保存 QC201/QC205/QC213 Oracle DDL 與 PostgreSQL CREATE 參考；本次未重連 Oracle、未讀業務列、未執行 Oracle DDL。
- [x] 新增 `/YMES/QC/AttributeItemImport` Vue 3 DEV 頁與 QC215 三層選單；保留 WinForms File/Check Data/Save/Template DownLoad/Select All/UnSelect All/MultiSelect/Close、勾選列處理、逐列結果樣式與假資料操作。API 漏回列級結果時前端顯示失敗。
- [x] 新增 `docs/modules/QC/計數品管項目匯入.md`，記錄來源程式、欄位、驗證、覆寫副作用、API 契約與資料表參照；更新規格索引、資料庫查詢紀錄及 `todolist_homepc.md`。
- [ ] QC215 Production API/schema 由家用端依 `todolist_homepc.md` 實作；無新表/CREATE SQL。API 需在每列交易內重驗引用、逐列回報、要求 QC215 FunctionKey、由伺服器寫 audit，並禁止 Factor 動態 DDL。
- [ ] 本段未執行 build、test 或瀏覽器驗收。接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 直接讀未遷移功能的 WinForms Form/Service/Core，完成 UI、規格與 API/schema handoff。

### 前一項：QC214 計量品管項目匯入（公司端 2026-11-17）

- [x] 直接閱讀 `frmImport_QCItem_Variables` Form/Designer、`wsQC.ImportQCItem_Variables`、`clsQCBasis.ImportQCItem_Variables` 與 `DelQCItemForVariables`；整理 1008×591 Grid/按鈕、Excel 欄位解析、第 2 列跳過、勾選列 Check/Save、逐列錯誤、QC205 子資料重建、抽樣計畫/Factor/OP 驗證和外部引用阻擋。
- [x] 確認 QC214 無專用資料表，不新增 CREATE SQL。沿用已查好的 QC205 主檔與 QC205/QC210/QC201 dependency Oracle DDL/PG CREATE 參考；本次未重連 Oracle、未讀業務列、未執行 Oracle DDL。
- [x] 新增 `/YMES/QC/VariableItemImport` Vue 3 DEV 畫面和 QC214 三層選單；重現 File/Check Data/Save/Template DownLoad/Select All/UnSelect All/MultiSelect/Close、唯讀欄位、勾選匯入、逐列粉紅失敗/灰色成功。DEV sample 儲存在獨立 localStorage，成功操作僅改假資料。
- [x] 新增 `docs/modules/QC/計量品管項目匯入.md`，記錄來源程式、解析欄位、驗證條件、覆寫副作用、API 契約和引用表；更新功能索引、DB 查詢紀錄及 `todolist_homepc.md`。
- [ ] QC214 production API/schema 待家用端依 `todolist_homepc.md` 實作；沒有 QC214 新表，沿用既有 QC205 tables。API 需逐列交易/結果、FunctionKey QC214、服務端 audit、參數化 SQL，並禁止 Factor 動態 DDL。
- [ ] 本段未執行 build、test 或瀏覽器驗收；接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 和未完成頁面直接讀 WinForms Form/Service/Core，再完成 UI/spec/API handoff。

### QC218 計數品管項目產品原因對應設定（公司端 2026-11-17）

- [x] 閱讀 `frmItemPRDReasonMap`、`frmItemPRDReasonDef`、Import Form/Designer、`QCItemRPDReasonSOPFileImporter`、`wsQC`、`clsQCBasis`；記錄 QCItemNo 核准查詢、ProductNo 主從群組、ReasonType/Level 雙清單、整組替換、全域 `*`、範本與分列部分成功匯入。
- [x] PowerShell 5.1 + Oracle.ManagedDataAccess 使用本機受保護唯讀連線取得兩張 mapping 表與三張共用 lookup 表 DBMS_METADATA DDL；兩張 mapping 只有 PK unique index、沒有 FK/其他 index，未查業務資料列、未在 Oracle 執行 DDL。
- [x] 新增 Oracle source DDL 五份與只建立兩張 mapping 空結構的 PostgreSQL CREATE-only 參考 `database/reference/mestest_qc218_item_product_reason_map_create.sql`；三張共用 QC Item/Reason/Product 表不可重建。
- [x] 新增 `web/src/components/QcItemProductReasonMap.vue`、FunctionNo=`QCITEM PRD REASON MAP` / QC218 fallback 三層選單與 `/YMES/QC/ItemProductReasonMap`。DEV localStorage 提供假 QC Item/Reason/Product、ProductNo 分組、產品篩選、`*` 全域、ReasonMap 左右移動/整組儲存及 CSV 部分成功匯入。
- [x] 新增 `docs/modules/QC/計數品管項目產品原因對應設定.md`、規格索引及家用端 API/schema handoff。規格記錄 legacy 全域 `*` 編輯驗證疑點、Importer 重複查詢錯誤處理及略過最後 Excel row 的瑕疵，並要求 API 修正。
- [ ] 未執行 build/test/瀏覽器驗收。DEV 預覽只接受 CSV；Production 需由 API 支援 WinForms `.xlsx/.xls` 第一工作表，家用端 schema/API 待依 `todolist_homepc.md` 實作。
- [ ] 此功能完成；公司端接續依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選下一個未遷移 Function，直接讀 Form/Designer、Service/Core 與表結構，再建立 UI/spec/API handoff。

### QC217 設備品檢管理設定（公司端 2026-11-17）

- [x] 直接對照 `frmQCInspection`/Designer、`frmQCInspectionDef`/Designer、`wsQC` 和 `clsQCBasis`，整理兩個 Grid、查詢條件、Add/Edit/Delete/Copy/Approve、九種檢查週期、範圍選取、四類 QCItem、Mail Group、Issue Job 和 DataStamp 行為。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線唯讀取得 `TBLQCINSPECTIONBASIS`、`TBLQCINSPECTIONDETAIL` Oracle table/index DDL；確認兩張表只有 PK unique index、無 FK/其他索引，未查業務列或執行 Oracle DDL。
- [x] 新增 `/YMES/QC/Inspection` Vue DEV 頁、QC 三層選單及獨立 localStorage 假資料；保留 EQP「設備點檢表設定」獨立路由。新增 QC217 規格、Oracle DDL、PG CREATE-only 參考及家用端 API/SQL handoff。
- [x] 記錄舊程式 Delete 會依 OP/設備/檢查類型清除共用明細；Issue Job callback SQL 將 InspWeek 誤寫 InspType，家用端 API 必須用完整正確複合鍵修正。
- [ ] 家用端 QC217 commit `2730c7f` 與公司端 `43079d6` 已 merge 為 `0b0f434` 並 push；Production `/YMES/QC/Inspection` 選單路由、Options/空清單查詢及新增表單已由家用端驗證。公司端請 pull `main`，尚待 build/CRUD 驗收；待共用資料來源補齊後驗證寫入流程。QC218 UI/spec/DDL handoff 已於上節完成；公司端接續按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 選擇下一個尚未遷移功能。

### QC216 計量品管項目群組_減項（公司端 2026-11-17）

- [x] 直接閱讀 `frmPMI_QCItemGroup`、Define/Include/Deduction Define Forms、wsQC、clsQCBasis，整理三區 WinForms 版面、四狀態、主檔 CRUD、減項明細排序、扣除基準、Parameter/Op、簽核與硬刪引用清理。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查六張 MESTEST 表的 DBMS_METADATA、ALL_TAB_COLUMNS、ALL_CONSTRAINTS、ALL_INDEXES；群組主/明細/扣除基準及三張 SPC map 均只有來源 PK、無 FK/額外索引。未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 新增六張 Oracle source DDL、PMI 主/明細/扣除基準及三張 hard-delete 關聯表的 PostgreSQL schema-only CREATE 參考。Oracle 動態 Factor 欄位以 `factorvalues JSONB` 作為 YMES 建議，保留 `*` wildcard；家用端需白名單化已啟用 FactorOnline key。
- [x] 新增 `/YMES/QC/PmiItemGroup` Vue 3 DEV 頁與 QC 三層選單/路由，含主檔 CRUD/Copy/Delete/Approve、Detail 項目/排序/DeductionItem、扣除基準 CRUD/Copy、參數及作業站入口；使用獨立 localStorage 假資料。
- [x] 新增 QC216 功能規格、索引列、Oracle metadata 查詢紀錄及家用端 schema/API TODO。
- [x] 對照共用 `QC_ItemOp` Form/Service/Core，補上可選/已選雙清單、InLine/OffLine 批次加入、移除及 FAI/IPQC/PQC 驗證；非 ParameterType=2 的動態因子欄位改為來源選項清單。
- [ ] 尚未執行 build、test 或瀏覽器驗收；Production API/schema 由家用端依 `todolist_homepc.md` 接手，完成後公司端 pull 並對接。
- [ ] 公司端繼續按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 逐項讀 WinForms Form/Service/Core，完成 UI 和規格後再交 API/CREATE SQL，不以圖片替代程式碼調查。

### QC211 二次檢驗抽樣計畫（公司端 2026-11-17）

- [x] 直接對照 frmSamplingFor2ndInsp / Define 的 WinForms、Designer、wsQC、clsQCBasis；記錄主 Grid、查找條件、Add/Edit/Delete/Copy、一般/Wafer plan lookup 與必填規則。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查 MESTEST.TBLQCSAMPLINGFOR2NDINSP 的欄位、constraints 與 indexes；取得五個 NOT NULL、唯一索引鍵序，確認沒有 PK/FK；未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立 /YMES/QC/SecondInspectionSampling Vue 3 DEV 頁、QC 三層選單/route、功能規格、Oracle source DDL、PostgreSQL Create SQL 參考與家用 API/schema handoff。
- [x] DEV 頁支援篩選、選取、多筆刪除、Add/Edit/Copy、operation/QC item/plan picker、Apply/Confirm、唯一鍵檢查及獨立 localStorage 假資料；色系使用 KD 黃橘色。
- [x] 更新模組索引、DB metadata 查詢紀錄與兩台電腦 TODO。來源的 Edit Service/Core DataStamp/稽核參數不一致，已列為家用端 API 決策項。
- [ ] 未執行 build、測試或瀏覽器驗收；家用端需先核對 live schema/migrations 並完成 QC211 API，之後公司端 pull 驗收。
- [x] 下一個功能 QC212 計量品管項目群組已完成公司端 UI/spec handoff；接續依 MES_MENU_FUNCTION_INVENTORY.md 處理 QC213 計數品管項目群組。

### QC201 計數品管項目（公司端 2026-11-17）

- [x] 直接閱讀 `frmQCItemForAttributes`、`frmQCItemForAttributesDef`、`frmQCItemReason` 及共用 `QC_ItemParameter`、`QC_ItemFactor`、`QC_ItemOp`、`QC_SamplePlanChange`、`QC_SamplingParameterSimulation` WinForms 原始碼，核對主畫面、雙清單、狀態/簽核/刪除限制、欄位與按鈕事件；資料表行為依 wsQC/clsQCBasis 追查。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線直連 MESTEST 唯讀查主表、四張直接子表及刪除流程關聯物件的 DDL、欄位 nullable/default、PK/index/constraint；沒有讀 MES 業務列、沒有在 Oracle 執行 DDL，連線秘密未輸出。
- [x] 建立 Oracle source DDL `database/reference/mestest_qc_item_attributes_source_ddl.sql` 與四張直接維護子表的 PostgreSQL CREATE 參考 `mestest_qc_item_attributes_create.sql`；主表 CREATE 沿用 MTL 共用 SQL，其他刪除關聯表不整批重建。
- [x] 新增 `/YMES/QC/AttributeQcItem` Vue DEV 頁、QC 品質資料三層選單與 `QCITEM_A` 路由；主 Grid、四狀態、Add/Edit/Copy/Apply/Confirm、刪除/核准、Parameter/Factor/Reason/SamplePlanChange 及 Simulation 畫面可用 localStorage 假資料互動，欄位採 KD 淡黃色系。
- [x] 續讀共用 `QC_ItemOp` Form/Service 行為並補 Vue 左右雙清單、InLine/OffLine、ExecuteOrder、FAI/IPQC/PQC；僅已簽核可修改，LotIsValid=0 禁止新增 InLine，InLine 確認前檢查至少一種檢驗時機。
- [x] 新增 QC201 規格、規格索引列、Oracle 查詢紀錄及家用端 schema/API handoff；已將 SPK/ZKT/KYT 顯示差異與正式 ValueList 來源列入 API options 契約。
- [ ] Plan/Rule/MClass 查找鈕仍是簡化選取器；Production API 選項由家用端提供。未執行 build、測試或瀏覽器驗收。
- [ ] 未執行 build、測試或瀏覽器驗收。Production schema/API、權限、Issue Job、引用檢查由家用端依 `todolist_homepc.md` 接手；完成後公司端 pull 並對接驗收。
- [ ] 家用端接手後發現的原生 provider／分支證據缺口，集中依本檔頂部「QC201／QC202 公司端來源補交」處理；已有主表／子表 DDL 不重交，不將家用端 API／部署事項列為公司端來源工作。
- [ ] QC201 UI 完成後，依功能目錄選下一個尚未完成 Function；不可把功能佔位頁計為完成。

### QC112 品質原因產品指導書（公司端 2026-11-17）

- [x] 對照 `frmReasonPrdSOPFile`、`frmReasonPrdSOPFileMaintain`、`frmReasonPrdSOPFileImport` 及 Designer；記錄已核准原因載入、選列即載入關聯、產品查找條件、左右移動、檔案上傳/移除、範本下載及逐列匯入進度。
- [x] 以 PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護連線唯讀取得 `TBLQCREASONPRDSOPFILE` DDL/欄位/PK/index/FK 字典；僅 PK `(REASONNO,PRODUCTNO)`，無 FK/DataStamp/額外索引；未讀業務列或在 Oracle 執行 DDL。
- [x] 建立 `/YMES/QC/ReasonProductSOPFile` Vue DEV 頁與 QC 三層選單/路由，支援原因/關聯雙 Grid、Maintain、Import、localStorage 假資料及 API 呼叫契約；新增 QC112 規格、Oracle source DDL、PG CREATE 參考及本段 handoff。
- [x] 舊 `DataImporter.QCItemRPDReasonSOPFileImporter` 實作不在來源樹，已把 checker 及共用檔案刪除語義列為家用端查證項。Web 匯入改為選取工作簿與附件，再按 FileName 配對，不允許用戶端路徑被 API 當伺服器路徑讀取。
- [ ] 家用端已建立 QC112 PostgreSQL schema/API 並部署；請 pull 最新 main，以已登入正式環境開啟 `/YMES/QC/ReasonProductSOPFile`，核對原因/產品清單、附件新增/移除、XLS/XLSX/CSV 匯入、範本下載及錯誤顯示。Production API 使用登入保護；目前不含 FunctionKey 專屬 ACL（專案角色權限暫不處理）。
- [ ] 舊 `QCItemRPDReasonSOPFileImporter` 不在來源樹；若從 MES 原生程式確認到目前 API 未涵蓋的 checker/business 規則，請提供具體規則給家用端補齊。API 不會把工作簿 FilePath 當伺服器路徑使用。
- [ ] 下一個功能依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 排序，對照 WinForms Form/Designer/Service/Core 後繼續。

### QC110 特殊主檔維護（公司端 2026-11-17）

- [x] 對照 `frmSpecialBasisMaintain`/Designer、`wsSYS.LoadMesParameter/LoadMesParameterRef/LoadTablesConstraints`、`wsQC.UpdFactorMaintain` 與共用 Core；確認 ParameterClass=0、選列自動載入、動態 Grid、80px 按鈕欄、欄位名稱驗證、大小寫、必填黃色格、Import/Paste、整表 Save 與放棄變更提示。
- [x] 新增 `/YMES/QC/SpecialBasisMaintain` Vue DEV 頁、QC 基本資料三層選單、QC110 規格與獨立 `special-basis-maintain` API handoff。DEV 假資料與 QC109 隔離；正式資料仍由 API metadata 驅動。
- [x] QC110 與 QC109 共用既有 `TBLSYSMESPARAMETER`/`FC_%` 結構，沿用已查 Oracle source DDL 與 PostgreSQL CREATE 參考；本次不需新 DDL，也未查詢 MES 業務列。
- [ ] 未執行 build/test/瀏覽器驗收。家用端需比對 YMES live schema 並實作 FunctionKey QC110 的 class=0 參數清單、資料載入、欄位驗證、Excel preview 與交易式整表保存；完成後公司端 pull 並驗收 Production API。
- [ ] 下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 核對尚未完整遷移的下一個 Function，直接對照 WinForms Form/Service/Core 再繼續。

### QC109 要因資料維護（公司端 2026-11-17）

- [x] 對照 `frmQCFactorMaintain`、`wsSYS.LoadMesParameter/LoadMesParameterRef/LoadTablesConstraints`、`wsQC.UpdFactorMaintain`、`clsSYSMesParameter`、`clsQCBasis.UpdFactorMaintain_ArrayBinding`，確認參數篩選、動態 Grid、nullable 欄位底色、新增/編輯/刪除暫存、整表 Save、CSV/XLS Import、Clipboard Paste、離開提示與狀態列。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess、本機受保護連線唯讀取得 `TBLSYSMESPARAMETER` 與本次 `TableName LIKE 'FC_%'` 的三張來源表 `FC_POSIONLIST`、`FC_QCFORMCALLBACKITEMMAP`、`FC_THICKNESS` 的 DBMS_METADATA table/index DDL、ALL_INDEXES、ALL_CONSTRAINTS；未讀業務列，未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/FactorMaintain` Vue DEV 頁、QC 基本資料選單/路由、QC109 規格及家用端 API/schema handoff。DEV 僅使用三張表的 DEMO schema/假資料，正式環境由 API metadata 驅動。
- [ ] QC201／QC202 所需 MES Parameter Reference 的缺少契約集中於本檔頂部來源補交；補到本模組／QC110 既有 MD 供共用，不另建第二份 enum 或 provider 規格。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端先核對 YMES `TBLSYSMESPARAMETER`/`FC_*` 表，再依 QC109 handoff 建立必要 migration 與 FunctionKey QC109 的安全動態資料 API。QC110 已完成公司端 UI/規格，待家用端完成 API/schema 後再對接。

### QC108 計量品管法則基本資料（公司端 2026-11-17）

- [x] 依 `frmQCRuleBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照主 Grid、四狀態、RuleType、七種 RuleOption、RuleNo/RuleSerial、DataStamp、刪除/核准與 Issue Job。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀取得 `MESTEST.TBLQCRULEBASIS` table/index DDL 與 ALL_CONSTRAINTS；RuleSerial 為唯一 PK，僅 PK unique index，無 FK/額外索引；未讀業務列、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/VariableRuleBasis` Vue DEV 頁、選單/路由、QC108 規格及家用端 API/schema handoff。RuleOption UI/Oracle 代碼映射、舊 Other=9、FunctionName 目錄來源均已記錄。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端核對 YMES schema/migration 並實作 API/ValueList/FunctionName options 與 Issue Job。QC110 公司端頁面與規格已完成。

### QC107 計數判別法則基本資料（公司端 2026-11-17）

- [x] 依 `frmQCAttRuleBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照主/明細雙 Grid、RuleNo LIKE、四狀態、ComponentCollect、Action/ReasonType/ReasonLevel、三種 RuleType、複製、刪除及 Issue Job 簽核行為。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀查 `TBLQCATTRULEBASIS`、`TBLQCATTRULEDETAIL`、共用查找 `TBLQCAQL` 的 table/index DDL 及 ALL_CONSTRAINTS；已確認複合 PK、沒有外鍵/其他索引、沒有 DataStamp；未讀業務列或在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PG CREATE 參考、`/YMES/QC/AttributeRuleBasis` Vue 主從 DEV 頁、選單/路由、QC107 規格及家用端 API/schema handoff。自訂 AQL 依 `SysInfo/UserDefineAQL`，選項來自 TBLQCAQL。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端核對 YMES 共用 TBLQCAQL/既有 QC104–QC106 API 與 schema，依待辦開發；QC108 已完成，公司端下一項接續 QC110「特殊主檔維護」。

### QC106 品質原因基本資料（公司端 2026-11-17）

- [x] 依 WinForms `frmReasonBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照清單、CRUD、複製、ReasonType ValueList、QC105 子類別查找、Issue Control、Issue Job 簽核與 DataStamp 規則。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess，經本機受保護連線唯讀取得 `MESTEST.TBLQCREASONBASIS` table/index DDL 與 ALL_CONSTRAINTS；ReasonNo 為 PK，僅 PK unique index，無 FK/額外索引；未讀業務列、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/ReasonBasis` Vue DEV 頁、QC 基本資料選單/路由、QC106 規格及家用端 API/schema handoff。ReasonType 正式選項須由 MES ValueList API 提供；子類別查找只顯示同類別且 IssueState=2。
- [ ] 尚未執行 build/test/瀏覽器驗收；家用端依待辦核對 YMES live schema 並實作 API。QC107 已完成；公司端下一項接續 QC110「特殊主檔維護」。
### QC105 品質原因子類別資料（公司端 2026-11-17）

- [x] 依 WinForms `frmReasonSubTypeBasis`/Def、wsQC、`clsQCBasis`、`clsBasis_UPD` 對照查詢、欄位、ValueList、CRUD、Copy、Issue Control、刪除及 Issue Job 簽核規則。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護設定唯讀查 `MESTEST.TBLQCREASONSUBTYPEBASIS` table/index DDL 與 ALL_CONSTRAINTS；ReasonSubType 為 PK，只有 PK unique index，沒有 FK/額外索引；未讀業務資料、未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL 與 PostgreSQL CREATE 參考；完成 `/YMES/QC/ReasonSubTypeBasis` Vue DEV 頁、QC 基本資料選單/路由、QC105 規格、DB 查詢來源及家用端 API/schema handoff。ReasonType 的 DEV 選項有標示 DEMO，正式環境需由 ValueList API 供應。
- [ ] 未執行 build/test/瀏覽器驗收；家用端比對 live schema 並完成 API；QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。

### QC104 Wafer 抽樣計劃（公司端 2026-11-17）

- [x] 對照 `frmSamplingPlan_Wafer`/Def、wsQC、`clsQCBasis`/`clsBasis_UPD`：四狀態、PlanNo 查詢、六種可見 SampleType、區間/區域明細、CRUD/Copy/Approve/View、DataStamp、Issue Job 與首錯停止流程。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護設定唯讀查 `TBLQCSAMPLINGPLAN_WAFER`、`TBLQCSAMPLINGRANGE`、`TBLQCSAMPLINGAREA_WAFER` table/index DDL、ALL_CONSTRAINTS；主檔 PK PlanNo、Area PK (PlanNo,Area)、Range 無 PK/index、沒有 FK；未讀業務列，未在 Oracle 執行 DDL。
- [x] 建立 Oracle source DDL、PostgreSQL CREATE 參考、`/YMES/QC/SamplingPlanWafer` Vue DEV 頁、QC → 基本資料選單/路由及 QC104 規格/家用端 API/schema handoff。AQL DEV options 為假資料；正式 API 對照 `wsSYS.LoadMesParameterRef`。
- [ ] 未執行 build/test/瀏覽器驗收；家用端先核對 `TBLQCSAMPLINGRANGE` 與 QC103 共用表的既有 schema/API，再依待辦實作 QC104；QC105–QC107 已完成；公司端下一項為 QC110「特殊主檔維護」。


### QC103 抽樣計劃（公司端 2026-11-17）

- [x] 對照 `frmSamplingPlan`/`frmSamplingPlanDef`、wsQC、`clsQCBasis`/`clsBasis_UPD`：四狀態與 PlanNo 查詢、PlanType 動態參數、ADD/EDIT/DEL/Copy/Approve、DataStamp、Issue Job、Simulation 入口及多筆首錯停止規則。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經既有受保護連線唯讀取得 `MESTEST.TBLQCSAMPLINGPLAN`、`TBLQCSAMPLINGPARAMETER` table/index DDL 與 ALL_CONSTRAINTS；兩表各有 PK index，沒有 FK/額外索引。未讀業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立兩表 Oracle source DDL 與 PostgreSQL CREATE 參考；新增 `/YMES/QC/SamplingPlan` DEV 頁、QC → 基本資料選單/路由、QC103 規格、家用 API/schema handoff。
- [x] DEV UI 有主檔/參數編輯、狀態篩選、查詢、複製、簽核、刪除、套用及 Simulation 操作；本地 Simulation 只示範 Fixed Sample Size，其餘由正式 API 使用 Core 規則計算。
- [ ] 未執行 build/test/瀏覽器驗收；QC104、QC105 已完成，QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。


### QC102 抽樣計劃設定_1916（公司端 2026-11-17）

- [x] 對照 `frmSampling1916`/`frmSampling1916Def`、wsQC、`clsQCBasis` 與 `clsBasis_UPD`：全表載入、Attributes/Variables 前端切換、欄位顯示、複合鍵、驗證、ADD/EDIT/DEL/Copy/Apply 與多筆刪除首錯停止行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 使用既有受保護連線，直連唯讀取得 `MESTEST.TBLQCSAMPLING1916` table/index DDL 與 ALL_CONSTRAINTS；確認四個 NOT NULL、唯一 index `(SamplingType,MinNum,MaxNum,SLevel)`，沒有 P constraint、FK 或其他 index。未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立來源 DDL 與 PostgreSQL 空表 CREATE 參考；新增 `/YMES/QC/Sampling1916` DEV 頁、QC → 基本資料選單/路由、QC102 規格及家用 API/schema handoff。
- [x] 更新規格索引、資料庫查詢來源紀錄及兩台電腦待辦。DEV 假資料限開發預覽 localStorage。
- [ ] 未執行 build/test/瀏覽器驗收；QC103–QC105 已完成，QC106、QC107 已完成；公司端接續 QC110「特殊主檔維護」。


### QC101 抽樣表格（公司端 2026-11-17）

- [x] 對照 frmSamplingTable、frmSamplingTableDef、frmSamplingTableCopy、wsQC 與 clsQCBasis/clsBasis_UPD，整理查詢、複合鍵、範圍驗證、Apply、單列 Copy、整表 Copy 的部分成功行為。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過受保護設定唯讀查得 MESTEST.TBLQCSAMPLINGTABLE table/index DDL 及 ALL_CONSTRAINTS；確認複合 PK (SAMPLINGTABLE, MINNUM, MAXNUM)、唯一 PK index、四個 NOT NULL、無 FK/其他索引。未讀業務資料，未在 Oracle 執行 DDL，未輸出憑證。
- [x] 新增 PostgreSQL 空表 CREATE 參考、/YMES/QC/SamplingTable Vue DEV 畫面、QC → 基本資料 → 抽樣表格 fallback 選單與 API/schema handoff 規格。
- [x] 更新 QC 規格索引、資料庫來源紀錄與兩台電腦待辦；未執行 build/test/瀏覽器驗收。
- [x] QC102–QC107 已完成；公司端接續 QC110「特殊主檔維護」，依 MES_MENU_FUNCTION_INVENTORY.md 檢查 WinForms Form/Service、資料表及邏輯。
### QC111 8D 原因基本資料（公司端 2026-11-17）

- [x] 從 8D 原因基本資料 WinForms Form、兩個 Def Form、wsQC 與 clsQCBasis 整理雙 Grid、欄位、查詢、狀態、CRUD、複製、刪除、核准與 Issue/Job 流程。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 的既有受保護連線，直接唯讀查 MESTEST.TBLQCREASONBASIS_8D、TBLQCREASONSUBTYPE_8D 的 DBMS_METADATA DDL、欄位 NULL/default、PK、unique index 與 FK；確認僅 PK index、沒有 FK。未讀 MES 業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立 Oracle source DDL 與 PostgreSQL 空表 CREATE 參考，檔案位於 database/reference/mestest_qc_8d_reason_source_ddl.sql、mestest_qc_8d_reason_create.sql。
- [x] 新增 Vue 雙 Grid DEV 頁 web/src/components/Qc8DReasonBasisMaster.vue，建立 QC → 基本資料 → 8D 原因基本資料 fallback 選單及 /YMES/QC/8DReasonBasis；包含四狀態、查詢、主/明細 CRUD/Copy/Approve、KD 黃色輸入欄及 localStorage 假資料。
- [x] 新增 docs/modules/QC/8D原因基本資料.md，記錄 Oracle 欄位、WinForms/Form/Service/Core 邏輯、Issue Job 簽核、刪除孤兒明細行為及 API/schema handoff；更新規格索引、資料庫連線紀錄及兩台電腦 TODO。
- [ ] 未執行 build/test/瀏覽器驗收。ReasonType 代碼與繁中資源已直接查證；DEV approval 只模擬無簽核 Job 的直接核准分支，Production 必須實作 Issue/Job。
- [x] 原先排定的下一項為 QC406；確認目錄後完成 QC406，再按功能目錄接續完成 QC407「最後處理群組（FINAL GROUP）」。

### QC406 第一處理群組（公司端 2026-11-17）

- [x] 修正 QC 選單功能識別：FunctionNo `FIRST GROUP` / FunctionKey `QC406`；`FINAL GROUP` 才是 QC407。
- [x] 對照 `frmFirstGroup`、`frmFirstGroupDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`，整理狀態篩選、複合鍵、查找來源、多 OP 批次新增/複製、`OPNo='*'`、刪除、DataStamp 與 `FIRST GROUP` Issue Job 簽核。
- [x] 用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀查得 `TBLQCFIRSTGROUP` DDL、欄位 nullable/default、複合 PK、unique index 與 FK；並查核三張已存在查找表的 key/必要欄位。未讀業務資料，未執行 Oracle DDL，未輸出憑證。
- [x] 建立來源 DDL、PostgreSQL 空表 CREATE、Vue DEV 畫面 `/YMES/QC/FirstGroup`、規格文件及家用端 API/schema handoff；更新 DB 連線紀錄與索引。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准僅模擬無簽核流程直接轉 Active。
- [x] 下一個功能 QC407「最後處理群組（FINAL GROUP）」已於本次完成，詳見下節。

### QC407 最後處理群組（公司端 2026-11-17）

- [x] 對照 `frmFinalGroup`、`frmFinalGroupDef`、`wsQC.LoadFinalGroup*`/`*FinalGroup`、`clsQCBasis` 與 `clsBasis_UPD`；記錄四狀態篩選、複合鍵、查找、多 OP 新增/複製、`OPNo='*'`、刪除、DataStamp、Issue Job 與附件/通知流程。
- [x] 使用既有受保護連線，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 `TBLQCFINALGROUP` DDL、欄位/null/default、PK/index/FK；確認無 FK、只有複合 PK unique index。未讀業務列、未在 Oracle 執行 DDL、未輸出憑證。
- [x] 建立 Oracle source DDL、PostgreSQL 空表 CREATE、Vue DEV 頁 `/YMES/QC/FinalGroup`、QC → 異常設定 → 最後處理群組路由、規格文件及家用 API/schema handoff；共用 QC406 lookup table 定義。
- [x] 保留核准 Job Map key 的原始字串 `Final GROUP`，與 FunctionNo `FINAL GROUP` 明確區分。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准僅模擬沒有 Job/Detail 時直接轉 Active。
- [x] 下一個功能 QC404「異常說明（HOLD DESCRIPTION）」已於本次完成，詳見下節。

### QC404 異常說明（公司端 2026-11-17）

- [x] 對照 `frmHoldDescription`、`frmHoldDescriptionDef`、`wsQC`、`clsHoldBasis` 與 `clsBasis_UPD`；整理單 Grid、四狀態、MClassNo 查詢、GUID 主鍵、唯讀欄位、複製/套用、刪除、DataStamp 與 `HOLD DESCRIPTION` Issue Job/附件通知規則。
- [x] 用既有受保護連線，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀查得 `TBLQCHOLDDESCRIPTION` table/index DDL、欄位/null/default、PK/index/FK。確認 PK 為 HoldDescriptionID、MClassNo 有非唯一索引、沒有 FK；未讀業務列、未執行 Oracle DDL、未輸出憑證。
- [x] 建立 Oracle source DDL、PostgreSQL 空表 CREATE、Vue DEV 頁 `/YMES/QC/HoldDescription`、QC → 異常設定 → 異常說明三層選單、功能規格及家用端 schema/API handoff。
- [ ] 未執行 build/test/瀏覽器驗收；DEV 核准只模擬無 Job/Detail 時直接轉 Active。
- [x] QC405「異常處理群組（HOLD DISP GROUP）」已完成，詳見下節。

### QC405 異常處理群組（公司端 2026-11-17）

- [x] 對照 `frmHoldDispGroupBasis_THEIL_BU1`（來源 Form 位於 QC_HoldDispGroupBasis 專案）、`frmHoldDispGroupBasisDef`、`wsQC`、`clsHoldBasis`、`clsBasis_UPD`；完成上方群組 Grid 與下方三 Grid 人員分配版面、四狀態/GroupNo 查詢、CRUD/Copy/Delete/Approve、主處理/CC 切換、代理成員同步及 DataStamp/Issue Job 規格。
- [x] 直接沿用已由 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得的兩張 Oracle DDL：`TBLQCHOLDDISPGROUPBASIS` 在 QC406 lookup source DDL，`TBLUSRUSERGROUP` 在設備工程群組成員 source DDL；其 PostgreSQL 空表 CREATE 參考已存在。沒有新表或新 CREATE SQL 要交付。
- [x] 新增 Vue DEV 頁 `web/src/components/QcHoldDispGroupMaster.vue`，建立 QC → 異常設定 → 異常處理群組與 `/YMES/QC/HoldDispGroup`；加入四狀態、群組 CRUD/Copy、組員多選移動、Type 5 主處理/Type 6 CC、套用式儲存及 localStorage DEV 假資料。
- [x] 新增 `docs/modules/QC/異常處理群組.md`，記錄 Form/Service/Core、兩張表與 DDL 參照、精確 `HOLD DISP GROUP` Issue key 及 Production API/schema handoff；同步索引、DB 查詢紀錄與家用端 TODO。
- [ ] 未執行 build、test 或瀏覽器驗收。DEV 核准只演示 0→2；Production 必須依 Issue/Job 處理。家用端比對 live schema/migrations 後實作 API；不可在 MES Oracle 執行 CREATE。
- [x] QC403「異常原因基本資料（HOLDITEM BASIS）」已完成公司端 DEV UI、WinForms 規格與 Oracle/PG CREATE 參照，詳見下節。

### QC403 異常原因基本資料（公司端 2026-11-17）

- [x] 對照主清單/Def/匯入 Form、HoldItemDataImporter、wsQC、clsHoldBasis；完成四狀態與三欄查詢、欄位/lookup、CRUD/Copy/Delete/Approve、DataStamp、HoldSystem、OCAP/通知群組及 Excel 匯入規則。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 既有受保護唯讀連線，查得 MESTEST.TBLQCHOLDITEMBASIS DBMS_METADATA DDL、欄位 NULL/default、PK/index/FK。只有 HoldItemNo PK unique index，沒有其他 index/FK；未讀業務列，未在 Oracle 執行 DDL，未輸出憑證。
- [x] 新增 Oracle source DDL 與 PostgreSQL 空表 CREATE：`database/reference/mestest_qc_hold_item_basis_source_ddl.sql`、`mestest_qc_hold_item_basis_create.sql`。
- [x] 新增 Vue DEV 頁 `web/src/components/QcHoldItemBasisMaster.vue`，路由 `/YMES/QC/HoldItemBasis`；含查詢、四狀態 Grid、CRUD/複製、欄位 picker、核准/刪除及 CSV 假資料匯入。
- [x] 新增 `docs/modules/QC/異常原因基本資料.md`，更新規格索引、DB 連線查詢紀錄及 home/office TODO；正式 API、Issue Job 與 `.xls/.xlsx` 匯入由家用端接手。
- [ ] 未執行 build、test 或瀏覽器驗收。DEV 核准僅模擬無 Job/Detail 的 0→2；已簽核/不使用欄位權限仍需家用端對接 IssueControl 規則。
- [x] QC401「主分類基本資料」已完成公司端 DEV UI 與 WinForms/Service/Core 規格，詳見下節。
- [x] QC402「次分類基本資料」已完成公司端 DEV UI、WinForms/Service/Core/Importer 規格與 Oracle DDL/PostgreSQL CREATE 交接，詳見本檔 QC402 區段。
- [ ] 下一項依功能目錄與未遷移頁面，接續尚未完成的 MES Function。

### QC401 主分類基本資料（公司端 2026-11-17）

- [x] 讀取 frmMClassBasis、frmMClassBasisDef、wsQC 與 clsHoldBasis；確認四狀態、MClassNo 查詢、Grid 欄位、Def 表單、Copy/Apply、批次 Delete/Approve、DataStamp 和 MCLASS BASIS Issue Job 規則。
- [x] TBLQCMCLASSBASIS Oracle source DDL 與 PostgreSQL CREATE SQL 已由先前 PowerShell 5.1 + Oracle.ManagedDataAccess 直連查得並與 QC406 共用，不新增重複 DDL/CREATE 檔；MClassNo 為 PK，僅 PK unique index，無 FK/額外索引。
- [x] 新增 Vue DEV 頁 web/src/components/QcMClassBasisMaster.vue；加入三層 fallback menu、FunctionNo MCLASS BASIS / FunctionKey QC401 和 /YMES/QC/MClassBasis 路由。表格標頭與欄位使用 KD 黃色系；同步修正 QC403 藍色 Grid 表頭。DEV localStorage 假資料含四種狀態。
- [x] 新增 docs/modules/QC/主分類基本資料.md，更新公司/家用 TODO、兩份功能索引及 Oracle 連線紀錄；家用端僅需核對 live schema，缺表才建立 migration。
- [x] 找到並對照獨立 frmMClassDataImport 與 DataImporter.MClassDataImport。確認 NPOI 第一工作表、首列跳過、前兩欄、distinct pair、MClassNo 大小寫不敏感去重、每編號獨立部分成功及結果 Grid；更新 QC401 規格與 Home API 契約。正式範本二進位檔未在來源樹找到，Production 需沿用系統 template storage。
- [ ] 未執行 build、test、瀏覽器驗收或 YMES PostgreSQL 查詢。家用端 API、schema 比對與正式 Issue Job 仍待完成。

### EQP B_EQP AUTOLOADER SETUP 設備 AutoLoader 格式檔設定（2026-11-17）

- [x] 已確認舊 WinForms source tree 有完整原始碼：`Industry_Base/MESWin_Industry_Base/EQP/EP_B_EQPAutoLoaderSetup/`，並對照主畫面/編輯器/Default、相關子表單、`wsEQP`、`clsEQP` 及 `wsSYS` AutoLoader 檢查。先前 TODO 寫「找不到 Form」是錯誤，本次已修正認知。
- [x] 使用既有 Oracle 受保護設定，PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 4 張表的 source DDL、欄位、NULL/default、PK/index/FK；未讀 MES 業務資料，也未在 Oracle 執行 DDL。
- [x] 加入 Vue DEV 頁 `web/src/components/EquipmentAutoLoaderSetupMaster.vue`，涵蓋三層選單、查詢/選取、完整可見欄位、格式腳本、CRUD/Copy/Default、5 個設定頁籤及 KD 黃色欄位。
- [x] 加入功能規格、Oracle source DDL、PostgreSQL 空表 CREATE 參考、規格索引與家用端 API/schema handoff。詳細差異與簡化項目見 `docs/modules/EQP/設備AutoLoader格式檔設定.md`。
- [ ] 未執行 build/test/瀏覽器驗收。家用端先完成 schema/API/migration 並部署後，公司端 pull 最新 main，逐欄對接 Production，再補完整 TestFile parser、公式/ValueList 與設備查找 UI。

### 2026-11-17 MTL ML101 物料供應商基本資料

- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀查得 `TBLMTLMATERIALVENDOR`、`TBLMTLMATERIALVENDORCONTACTOR` 完整欄位、NULL/default、主鍵、索引及 FK；沒有讀取 MES 業務資料或在 Oracle 執行 DDL。
- [x] 對照 `frmMaterialVendor`、主檔/連絡人 Def、`wsMTL`、`clsMTLBasis`，完成四狀態查詢、雙 Grid、CRUD/複製/多筆刪除/核准、連絡人 Mail、Issue/Job、audit 與 DataStamp 規格；新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL 及獨立規格文件。
- [x] 新增 `web/src/components/MaterialVendorMaster.vue`，依 WinForms 雙 Grid 與 KD 黃色欄位完成 DEV localStorage 互動；新增 MTL fallback 三層選單與 `MTL VENDOR` 路由 `/YMES/MTL/MaterialVendors`。
- [x] 已將 API、schema、migration handoff 寫入 `todolist_homepc.md`；提醒家用端修正 material vendor map 名稱查詢誤 join EQP vendor 的問題。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML102 物料類別資料

- [x] 對照 `frmMaterialType`、`frmMaterialTypeDef`、`frmMaterialGumType`、`frmMaterialProperty`、`wsMTL`、`clsMTLBasis`；整理畫面、狀態、CRUD、Copy GumHour、Property/MTL PROPERTY、固晶膠核准檢查與 GumHour 數值驗證。
- [x] 確認 Oracle DDL/PG CREATE SQL 已存在於 `mestest_mtl_master_*`，包含 MaterialType、Property、TypeQCItem、TypeAcclimation；此功能沒有新增 table/CREATE SQL。
- [x] 新增 `web/src/components/MaterialTypeMaster.vue`，建立 `/YMES/MTL/MaterialTypes`、`MTL TYPE` 動態/fallback 路由；包含狀態查詢、主 Grid、Add/Edit/Copy/Delete/Approve、Property 與 GumHour 視窗，Copy 會一併複製類別時效，核准後 GumHour 唯讀，DEV 假參數不代表正式系統參數值。
- [x] 更新 ML102 規格、索引及家用端 schema/API/Issue handoff。
- [x] 重新確認 origin/main 與公司端基線相同；已把 Copy 的單交易 API 契約與 GumHour POST/PUT 區分補進家用端待辦。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端完成後再對接與驗收。

### 2026-11-17 MTL ML105 物料類別品管項目設定

- [x] 對照 `frmMaterialTypeQCItem`、`wsMTL`、`wsQC`、`clsMTLBasis`；確認前綴查詢、三種 QC 候選清單、雙向多選、類別範本儲存及同步至所有關聯物料/IQC 項目。
- [x] 確認 `TBLMTLTYPEQCITEM`、`TBLMTLQCITEM` 的 Oracle DDL/PG CREATE 已在 MTL reference 和 0013 migration；無新增 CREATE SQL。
- [x] 新增 ML105 Vue DEV 頁與三層 MTL 路由，並建立 Production API/transaction handoff 規格。
- [ ] 未執行 build、test 或瀏覽器驗收；等待家用端完成 API 後再接 Production endpoint。

- Oracle MESTEST 直接查詢已可用，資料字典可見 1,287 張表。查詢方式及憑證規則見 `docs/DATABASE_CONNECTION.md`。
- 2026-11-16 已用 PowerShell 5.1 + Oracle.ManagedDataAccess 查得 `TBLUSRUSERGROUP` 原始 DDL、6 欄 NULL/default、PK 與索引資訊；來源 DDL、空表 PostgreSQL CREATE SQL、設備工程群組規格及家用端待辦均已更新。
- 2026-11-16 已查得 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` 原始 DDL、欄位/PK/索引/FK；SMD 空表 CREATE SQL 與設備群組規格已補齊。
- 設備群組 Vue 已依 WinForms 對齊：編號失焦轉大寫；查詢/狀態切換時清除不可見列選取，DEV 核准的 Issue flow 簡化已記錄。
- 2026-11-16 已從 WinForms/Service 確認配件類別選項表為 TBLEQPACCESSORYTYPE，Oracle 9 欄 DDL/PK 索引已直連核實；來源與空表 PostgreSQL CREATE SQL 已交付家用端。
- WinForms 正式選單目錄已查得 20 個 Module、601 筆 MenuItem、553 筆 Function；繁中第一層名稱取自 `D:\iMES\CSiMES\Customer\Production\MESClient\mesresource.zh-cht.resources`。完整 ModuleOrder、分類、FunctionNo、FormName、ExecutionFile 見 `docs/MES_MENU_FUNCTION_INVENTORY.md`。
- 公司電腦開工前已 pull GitHub `origin/main` 最新版本；家用端已部署 20 個模組的三層選單、物料子功能 API、設備供應商 API 與設備狀態 API。每次接續仍先 pull，並對照新 endpoint/options 的 Vue 呼叫契約與來源 WinForms。
- 作業站主表、群組明細及 Service 明確使用的 16 張子表，連同 3 張系統目錄表，共 21 張來源 DDL/空表 CREATE SQL 已交付；物料 8 張來源 DDL/空表 CREATE SQL 也已交付。檔案索引見 `docs/DATABASE_CONNECTION.md`、`docs/modules/OP/作業站主檔.md`、`docs/modules/MTL/物料主檔.md`。
- `TBLOPBASIS` 等 21 張作業站/系統表已確認每表一個 PK unique index，沒有額外獨立索引；本次查的 29 張 OP/系統/MTL 表沒有 Oracle FK constraint。PK/NOT NULL 保留在 CREATE SQL。
- `tblmtlmaterialbasis` 的 YMES bootstrap 已補齊 7 個來源 DEFAULT；多出的 7 個 YMES 欄位仍保留，等家用電腦比對實際 schema，不要自行刪除。
- 物料主檔及其子功能已有 DEV localStorage 互動雛形；物料匯入已補 CSV/`.xlsx` 預覽、9 欄解析、物料編號/名稱分組、結果列、開發範本下載及逐組假資料進度/取消；正式範本下載、`.xls` 預覽與群組 API 規則待家用端。詳細規格見 `docs/modules/MTL/物料資料匯入.md`。作業站子功能也有簡化 DEV 視窗。設備設定已有七個功能頁雛形。設備狀態的兩張刪除引用表 DDL、空表 PostgreSQL CREATE SQL、正式 ValueList 和繁中翻譯已於 2026-11-16 交付家用端；家用端仍需 migration 和 API 刪除檢查部署。其他頁面仍待逐項 WinForms 對照。
- 本次未執行 build 或測試；未完成整套 MES 遷移，也未把尚未完成功能標為正式上線。

## 2026-11-16 SMD SD104 區域基本資料公司端交接

- [x] Pull `origin/main` 最新版本，確認 `82de6f4` 為當前基準。
- [x] 從 `frmAreaBasis`、`frmAreaBasisDef`、`frmAreaLayout`、`frmNodeDef`、`wsSMD`、`clsSMDBasis` 邏輯完成 SD104 主 Grid/表單/狀態/核准/刪除規則整理；AddFlow 版面在 Vue 先做可操作簡化，差異已列規格。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線直查 `TBLSMDAREABASIS`、`TBLSMDAREAINVENTORY`、`TBLSMDAREAMAP` DDL、欄位、PK、索引與 FK；建立 source DDL 和 PG 空表 CREATE SQL。關聯配置兩表使用設備群組來源 DDL/CREATE SQL。
- [x] 建立 SMD fallback 三層目錄（8 個 WinForms Function）；SD104 接上 DEV Vue 頁，其他 7 個仍由 FunctionPending 說明。
- [x] 新增 `docs/modules/SMD/區域基本資料.md`，並更新規格索引、資料庫連線紀錄及家用端待辦。
- [x] 修正 SD104 DEV 預設列，使未簽核子區可選已簽核 Base Area；套用新增後轉入 Edit 狀態；核准頁採用 API 回傳實際 IssueState；配置保存不限制 IssueState，符合 WinForms 對應程式。
- [ ] 本段尚未執行 build/test/瀏覽器驗收；家用端 API/schema 完成後需 pull、逐欄對接並檢查正式行為。

## 2026-11-16 SMD SD105 區域庫房基本設定公司端交接

- [x] 對照 `frmAreaInventory`、`frmAreaInventoryDef`、wsSMD 與 clsSMDBasis；記錄 Base Area 選擇、兩種 AreaRelation picker、Default 唯一規則、CRUD/Apply/Copy/Delete 與來源參數不一致處。
- [x] Oracle `TBLSMDAREAINVENTORY` DDL/欄位/PK/index 已由 SD104 PowerShell 5.1 + ODP.NET 直連查得，且已附 `mestest_smd_area_basis_source_ddl.sql`、`mestest_smd_area_basis_create.sql`；本功能沿用既有表，沒有重複查詢或另造 SQL。
- [x] 新增 Vue DEV 頁 `/YMES/SMD/AreaInventories`、Area/Inventory 查找彈窗、Default/重複驗證、Apply、稽核、localStorage 假資料及 `AREA INV MAP` 路由。
- [x] 新增 `docs/modules/SMD/區域庫房基本設定.md`，更新 SMD 功能索引、資料庫查詢紀錄與家用端 API/schema handoff。
- [ ] 尚未執行 build、test 或瀏覽器驗收；家用 API 完成後再 pull 對接。
- [x] SD106「過帳電腦之區域」已完成 WinForms Form/Service/Core 對照、Oracle DDL 直查、DEV Vue 頁及 API/CREATE SQL handoff；進度見下方 SD106 區塊。
- [x] SD101「工廠基本資料」已完成，WinForms/Service/Core 對照、Vue DEV 頁與家用端 API/schema handoff 見下方區塊。Oracle DDL 沿用 2026-11-16 EP303 已直連查詢的結果，沒有重查。

## 家用電腦完成後，公司端先做

- [x] 2026-11-16 Pull 家用電腦最新提交 `944b793`，閱讀更新後的 DTO/endpoint/migration 與 `todolist_homepc.md`；設備狀態 Production endpoint 使用 `GET/options/POST/PUT/DELETE`，Vue 呼叫路徑已對上。
- [ ] 用公司端 Development 假資料頁逐頁核對 API 欄位名稱、狀態值、DataStamp、錯誤訊息與選項，不讓 Production 回退 localStorage。
- [ ] 以目前已查到的 20 個模組與資源檔標題整合三層側欄：Module → Menu Group → Function；維持 ModuleOrder、MenuOrder、FunctionOrder。未遷移功能要明確顯示尚未遷移，不能導向空白頁。
- [ ] 按 `docs/MES_MENU_FUNCTION_INVENTORY.md` 逐筆核對 FunctionNo、FormName、ExecutionFile 與 `D:\iMES\CSiMES\STD\MESWin` Form/Designer/Service；一個 Function 建一份或更新一份獨立規格，不以截圖代替程式碼/DDL。
- [ ] 先完成核心五模組既有頁面與子功能的 WinForms 對照：EQP 設備設定七項、OP 作業站子功能、MTL 屬性/QC Item/供應商/GumHour/匯入、PRS/PRD 擴充頁面。每個頁面復刻查詢欄位、資料欄位、按鈕、表單、狀態規則與互動。
- [x] MTL 匯入 DEV 畫面改依 WinForms 呈現：`.xlsx`/CSV 預覽、9 欄欄序、按物料編號/名稱分組、逐組結果、開發範本、進度與取消。規格見 `docs/modules/MTL/物料資料匯入.md`。
- [ ] MTL 匯入仍需家用端提供正式範本下載、`.xls` 預覽、ErpUnitNo 及與 WinForms 一致的群組寫入/結果契約；之後再做 Production UI 對接。
- [x] 設備點檢表設定 FunctionNo/FunctionKey 已核實為 `EQP QCLIST`/`EP107`；透過 PowerShell 5.1 唯讀查 MESTEST ValueList 並核對繁中資源，QCType 合法值為 0「開始生產時點檢」、2「手動點檢」。已修正 DEV 選項及新增預設值；Production options API 交由家用端依規格提供。
- [x] 設備工程群組、設備類別、設備群組 FunctionNo/FunctionKey 已由目錄 inventory 核實：`EQP ENGINEER GROUP`/`EP102`、`EQP TYPE`/`EP103`、`EQP GROUP BASIS`/`EP105`。
- [x] 設備類別匯入 DEV UI 改依 WinForms：第一工作表固定 8 欄、略過標題列、依 EquipmentType/EquipmentClass 顯示結果、產生 .xlsx 開發範本、逐類別進度與取消。假資料不模擬每類別多列或舊 Checker，明確提示且不寫入；.xls/正式 Checker 交家用 API。規格與 API handoff 已更新設備類別文件。
- [ ] 按 ModuleOrder 繼續其餘 15 個模組、476 個 Function。每頁先查 WinForms Form/Designer/Service；需新表時由公司端 PowerShell 查原始 DDL，再提供家用端 CREATE SQL/API 規格。不要猜欄位或流程。
- [ ] DEV 互動使用清楚標示的本機假資料；正式 API 未提供時停用或顯示未接通，不能回報假成功。
- [ ] 每完成一個功能，更新對應 `docs/modules/<ModuleNo>/...md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md`、`todolist_homepc.md`/本檔與 Function 遷移狀態。
- [ ] 如遇 WinForms 隱藏的複雜功能，先做可操作的簡化版並記錄未復刻規則，繼續下一頁；把需要使用者決策的精確差異集中列出。
- [ ] 全部功能完成後核對三層選單、FunctionNo 路由、UI/API 契約、規格文件與資料庫交接；完成後 push GitHub。

## Oracle 來源交付給家用電腦



- [x] **設備工程群組成員表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `MESTEST.TBLUSRUSERGROUP` DDL、欄位 NULL/default、PK、索引/FK 字典。來源 Oracle DDL、PostgreSQL 空表 CREATE SQL 與規格已更新；家用端需核對 YMES schema，缺表才新增 migration 並實作 GroupType=2 成員 API。
- [x] **設備群組 SMD 映射表已交付（2026-11-16）**：以 PowerShell 5.1 + ODP.NET 唯讀查詢 `TBLSMDAREARELATION`、`TBLSMDSMTAREAEQPMAP` DDL 與欄位/PK/索引/FK。家用端比對 YMES schema，實作新增/移除設備明細時的舊 SMD 同步語意。

- [x] **設備狀態設定來源資料已交付（2026-11-16）**：以 PowerShell 5.1 唯讀查 MESTEST 的 `TBLEQPSTATECONTROL` / `TBLEMSEQUIPMENTSTATE` 物件、欄位、PK/唯一索引、NOT NULL/default 與 DDL；查 `TBLSYSVALUELIST` 的兩組選項，並核對 `mesresource.zh-cht.resources` 繁中翻譯。來源 DDL、PG 空表 CREATE SQL 與規格已更新；家用端仍待比對引用表 schema、migration/SELECT 權限及 DELETE 完整驗收，CRUD/options API 已部署。

- [x] 設備類別配件目錄已交付（2026-11-16）：已核實 MESTEST.TBLEQPACCESSORYTYPE 原始 Oracle DDL、9 欄 nullable/default、PK 唯一索引與無 FK，並建立 PostgreSQL 空表 CREATE SQL。家用端仍需比對 YMES 目標表與完成 API options。

## 2026-11-16 SMD SD106 過帳電腦之區域公司端進度

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，確認沒有新提交。
- [x] 對照 `frmComputerArea`、`frmComputerAreaDef`、wsSMD 與 clsSMDBasis，記錄主 Grid/定義 Form、查詢欄位、按鈕、工廠與區域選擇、複合鍵、Copy 與批次 Delete 行為；指出 Copy 舊 Core 只按 ComputerNo 找來源，以及 ws/Core DataStamp 簽章不一致。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護連線直查 `TBLSMDCOMPUTERAREABASIS`、`TBLSMDCOMPUTERAREADETAIL` DDL、欄位 NULL/default、PK/index/FK；只查結構，未讀業務資料，未在 Oracle 執行 DDL，憑證未輸出。
- [x] 新增 Oracle logical source DDL 與 PostgreSQL 空表 CREATE SQL；校正 first-phase reference 中 ComputerArea DataStamp DEFAULT 及 SD104 AreaBasis DataStamp DEFAULT，移除 ComputerArea 欄位推測註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ComputerAreaMaster.vue`、`/YMES/SMD/ComputerAreas` 路徑及 FunctionNo=`COMPUTER AREA`；實作查詢、多選、Add/Edit/Copy/Delete、approved Factory/Area 選擇、稽核與 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/過帳電腦之區域.md`，更新 SMD 規格索引、Oracle 連線查詢紀錄與家用端 schema/API handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/migration 待家用端完成。
- [x] SD101「工廠基本資料」已接上 Vue DEV 路由；既有 Oracle DDL/PG CREATE SQL 已確認可沿用，完成規格及 API/schema handoff。
- [x] SD103「儲位基本資料」公司端 UI、WinForms 規格與 Oracle/PG CREATE SQL 已完成；詳本文下方 2026-11-16 SD103 進度。

## 2026-11-16 SMD SD101 工廠基本資料公司端進度

- [x] 對照 `frmFactoryBasis`、`frmFactoryBasisDef`、wsSMD 與 `clsSMDBasis`/`clsBasis_UPD`；記錄四種 IssueState、FactoryNo 搜尋、IC_FT_PKG FactoryName 顯示、欄位/按鈕、Copy、批次 Delete/Approve、DataStamp 與 Issue/Job 規則。
- [x] Oracle `TBLSMDFACTORYBASIS` 原始 DDL、欄位 NULL/default、PK/index/FK 已於 2026-11-16 EP303 查詢直連取得；沿用 `mestest_carrier_basis_dependencies_*` 和 first-phase CREATE SQL，未重複查詢/新增 SQL。
- [x] 新增 Vue DEV 頁 `web/src/components/FactoryBasisMaster.vue`，接上 `/YMES/SMD/Factories` 與 FunctionNo=`FACTORY BASIS`；包含查詢/狀態篩選、選列/多選、Add/Edit/Copy/Delete/Approve、KD 黃色定義表單、稽核及 Apply/Confirm。
- [x] 新增 `docs/modules/SMD/工廠基本資料.md`，更新功能索引、Oracle 查詢記錄與家用端 API/schema/簽核 handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；Production API/schema 待家用端比對現況並補齊，核准需串正式 Issue/Job 流程。

## 結束規則

- 不要把「已建立 Vue 元件」視為「已完整復刻」；需對照舊 Form 的欄位、按鈕、流程與 Service 規則。
- 不要刪除來源 DDL 尚未核實的 YMES 欄位或資料表。對既有表先比對，缺表才建，差異用 forward migration。
- 連線密碼、Token、SSH 私鑰不可放入 `todolist_*.md`、SQL、程式碼或 Git。

## 2026-11-16 EP202 配件類別資料進度與下一項

- [x] GitHub 最新 main 已 pull；新增 web/src/components/AccessoryTypeMaster.vue，接上「設備資料 → 配件 → 配件類別資料」與 /YMES/Equipment/AccessoryTypes。
- [x] 依 WinForms 建立狀態篩選、AccessoryType 查詢、清單、Add/Edit/Copy、批次 Delete/Approve、屬性管理、壽命管理、匯入預覽及 KD 黃色系表單。
- [x] 建立 docs/modules/EQP/配件/配件類別資料.md；查詢 MESTEST 四表 DDL 並附原始 Oracle DDL / PostgreSQL 空表 CREATE SQL，家用 API 工作已寫入 todolist_homepc.md。
- [ ] 待家用 API 合併後，pull 並對接 EP202 Production endpoints；補 PropertyNo 系統選項選擇器、壽命公式編輯器及 Excel .xlsx/.xls 前端互動。
- [ ] 家用端 2026-11-16 執行最新 Web build 時，`AccessoryTypeMaster.vue` 第 52、59 行各有 TS1109 語法錯誤（copy 模式 description ternary 缺少 true 分支的空字串值）。修正並重新執行 `npm run build`，再做頁面瀏覽器驗收；家用端未直接修改 UI。
- [x] 已完成 EP201 配件分類與 EP204 可用狀態的 WinForms/Service 對照、Vue Development 畫面、規格及 Oracle DDL/PG CREATE SQL 交接；詳細完成狀態見本文下方 2026-11-16 更新。
- [ ] 本次未執行 build 或測試；完成後需在後續驗證階段處理，不代表 Production 功能已完成。

## 2026-11-16 EP201 配件分類與 EP204 可用狀態完成公司端交接

- [x] GitHub main 已確認為最新；本次保留先前未提交的 Oracle DDL 並繼續公司端工作。
- [x] 新增 web/src/components/AccessoryCategoryMaster.vue，接入設備資料 → 配件 → 配件分類資料與 /YMES/Equipment/AccessoryCategories；提供狀態/分類查詢、CRUD/複製、核准、狀態依來源刪除/停用、CSV 匯入預覽。
- [x] 新增 web/src/components/AccessoryCategoryStateMaster.vue，接入 EP204 獨立功能路徑，也可由 EP201 以 Vue 彈窗傳入分類；包括狀態選項、初始旗標、作用名稱/介面驗證、CRUD/複製、核准與刪除/停用。
- [x] App.vue 三層選單 fallback、動態 API menu merge、FunctionNo route 與 Vue component 分派已加入 ACC CATEGORY / ACC CATEGORY STATE。
- [x] 依 WinForms Form/Designer、wsEQP、clsAccBasis/clsBasis_UPD 追出按鈕、欄位、狀態、簽核與 DataStamp 規則；規格分別為 docs/modules/EQP/配件/配件分類資料.md、配件分類狀態定義設定.md。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連 MESTEST 唯讀取得 TBLEQPACCCATEGORYSTATEBASIS 與 TBLEQPACCSTATEBASIS DDL/欄位/PK/index；建立兩份 PostgreSQL 空表 CREATE SQL，修正 first_phase_create_tables.sql 中曾標成推測的 lookup 欄位 default。
- [x] todolist_homepc.md 與 docs/DATABASE_CONNECTION.md 已列出三表比對/migration、API/options、簽核、InitialFlag 交易/唯一性及欄位契約。
- [ ] 家用 API 完成後，公司電腦 pull 並逐欄對接/核對；EP201 Excel .xls/.xlsx 與舊 DataChecker 在 Vue Development 先簡化為 CSV，須依正式 API 再補前端體驗。
- [x] ACC BASIS / EP203「配件基本資料」已完成公司端復刻與 Oracle DDL 交接；詳見本文 EP203 進度及獨立規格。
- [ ] 未執行 build 或測試；整套 MES 其餘 Function 尚未完成，繼續依 ModuleOrder 往下逐項復刻。

## EP203 配件基本資料公司端進度（2026-11-16）

- [x] 已 pull GitHub main（f569a3f，當時與 origin/main 同步）。
- [x] 透過 PowerShell 5.1 + ODP.NET 使用本機受保護 Oracle 設定，唯讀取得 TBLEQPACCESSORYBASIS 與五張關聯/狀態表 DDL；並查得八個 Oracle index DDL，未讀 MES 業務資料。
- [x] 已將六表 Oracle 原始 DDL、PG 空表 CREATE SQL 及 schema 比對/API 工作交給家用端：database/reference/mestest_accessory_basis_source_ddl.sql、mestest_accessory_basis_create.sql、todolist_homepc.md。
- [x] 已依 WinForms frmAccessoryBasis/Def、EP_ACCProperty、AccessoryDataImport 追出狀態、清單顯示欄位、資料表、核准/刪除、複製、屬性及匯入規則；規格為 docs/modules/EQP/配件/配件基本資料.md。
- [x] Vue EP203 已加入設備資料 → 配件第三層選單、/YMES/Equipment/AccessoryBasis 頁面及 localStorage DEV 假資料互動；包括全 Grid 欄位、狀態篩選、多選按鈕、表單、屬性視窗、CSV 預覽與 Production Excel API 契約。
- [ ] 家用端比對六張 YMES 表並實作 Production API 後，公司端 pull 並對接正式 API。DEV 核准/簽核與 CSV 匯入僅作 UI 預覽，Excel 正式解析由 API 完成。
- [x] EP205「配件狀態設定」已完成 WinForms Form/Def/Service 對照、Vue Development 頁與 API/schema handoff；詳見 docs/modules/EQP/配件/配件狀態設定.md。
- [ ] 不執行 build/test，除非使用者提出驗證要求；不要將 Oracle 連線密碼寫進文件或 Git。

## 2026-11-16 EP205 配件狀態設定公司端進度

- [x] 依 frmEQPACCStateBasis、frmEQPACCStateBasisDef、wsEQP、clsAccBasis 核對主 Grid、ADD/EDIT/DEL/Copy/Close、Apply/Confirm、數值/StateType/色彩/稽核/DataStamp 規則。
- [x] 核對 Oracle TBLEQPACCSTATEBASIS DDL、欄位/PK/index；沿用既有來源 DDL 與 PostgreSQL 空表 SQL，不重複建立表。
- [x] 新增 web/src/components/AccessoryStateBasisMaster.vue，接上「設備資料 → 配件 → 配件狀態設定」和 /YMES/Equipment/AccessoryStates；Development localStorage 假資料提供新增、修改、複製、刪除、多選及 Apply/Confirm。
- [x] 建立 docs/modules/EQP/配件/配件狀態設定.md，更新規格索引與家用端 schema/API TODO。
- [ ] 家用端核對 YMES schema 並完成 Production API 後，公司端 pull 對接實際 API。
- [ ] 下一項繼續按設備 FunctionNo 追 WinForms Form/Service；需新增資料表時由 PowerShell + ODP.NET 唯讀查 Oracle DDL。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP101 設備供應商基本資料補正（公司端 2026-11-16）

- [x] 重新核對 frmEQPVendor、frmEQPVendorDef、frmEQPVendorContactorDef：補上新增/複製供應商「...」帶入編號按鈕，以及供應商與聯絡人表單的「確定、關閉、套用」流程。
- [x] Development 假資料改由應用程式環境選擇控制；切到 Development 才讀寫 localStorage，切換環境或 API 位址時重新載入。正式/測試環境使用 API。
- [x] 儲存後更新表單 DataStamp 與稽核欄位；更新 EP101 規格、規格索引及本檔。
- [ ] 下一項按設備功能清單繼續對照 WinForms Form/Designer/Service；只有發現未有規格的資料表時才查 Oracle DDL 並交付家用端。
- [ ] 本次未執行 build 或測試；未變更 API 與資料庫 schema。


## EP102 設備工程群組資料補正（公司端 2026-11-16）

- [x] 直接核對 frmEQPEngineerGroupBasis/Def 與 Designer：主按鈕順序 Add/Edit/Delete/Copy/Close；Find/Print 隱藏；Def 支援 Confirm/Close/Apply；編號失焦轉大寫。
- [x] Vue 表單加入套用並保留表單、儲存後更新 DataStamp/稽核欄位；主按鈕依 WinForms 排序。
- [x] Development 假資料依應用程式選取環境啟用；切換環境/API 位址時重新載入；Test/Production 呼叫 API。
- [x] 更新 EP102 規格、索引與本檔。正式 API及 TBLUSRUSERGROUP schema 工作仍留給家用端既有 TODO。
- [ ] 下一項繼續依設備設定清單對照 WinForms Form/Designer/Service；需要新資料表才查 Oracle DDL 並交付家用端。
- [ ] 未執行 build 或測試；沒有更動 API/schema。


## EP103 設備類別資料公司端 UI 規格完成（2026-11-16）

- [x] 直接對照 frmEquipmentType/Def、ConsumeBasis/Def、EQPProperty/Def、圖片選擇器及工程群組查詢事件；完成主表單、耗材表單、屬性表單的確定/關閉/套用與套用異動狀態。
- [x] EngineerGroupNo、PictureName 改為唯讀欄位與 WinForms「...」選擇視窗；設備圖片上傳與圖片庫選擇保留。
- [x] Development 假資料只在 Development preview 啟用；EP103 頁使用共用 api/equipment/engineer-groups 契約，其他環境走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備類別資料.md、規格索引及家用端 API 待辦；沒有新增資料表或 SQL。
- [ ] 家用電腦比對五張設備類別表與配件目錄表 schema，完成 API/圖片/匯入/簽核流程後，拉回最新版本做 Production 契約核對。
- [x] EP105「設備群組資料」WinForms Form/Designer/Service 已核對並完成公司端 UI 規格補正；EP106 後續進度見本文 EP106 區塊。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP105 設備群組資料公司端 UI 補正（2026-11-16）

- [x] 直接對照 frmEquipmentGroup/Def、Designer、clsEQPBasis 與 wsEQP；保留主從清單、隱藏 Copy 按鈕、未核准群組才能編輯/加入/移除、設備只能加入一次及 Chamber 排除規則。
- [x] 群組 Def 補上 WinForms 確定/關閉/套用、異動後才啟用 Apply、建立/修改稽核欄位、編號失焦轉大寫及儲存後更新 DataStamp/清單。
- [x] Development 假資料改由所選 Development preview 控制；環境/API 位址切換會重新載入。Production 走 API。
- [x] 更新 docs/modules/EQP/設備設定/設備群組資料.md、規格索引與 todolist_homepc.md。主從與 SMD 表 Oracle DDL/CREATE SQL 已先前查得，本次無新表/無需再查 DDL。
- [ ] 家用端按 todolist_homepc.md EP105 區塊比對 schema、完成 CRUD/Approve/SMD API，部署後公司端再對接並驗證。
- [x] EP106 設備狀態設定 UI/互動已完成本輪 WinForms 對照；詳見下方 2026-11-16 進度。
- [ ] 本次未執行 build 或測試；未更動 API、資料庫或部署。


## EP106 設備狀態設定公司端補正（2026-11-16）

- [x] 開工前 pull GitHub `main`，確認與 `origin/main` 同步於 `894d5a4`；來源 WinForms 位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_EQPStateBasis`。
- [x] 按 Designer 還原表單尺寸與單欄欄位排列，校正主功能按鈕順序及 Def 表單 Confirm/Close/Apply 順序；Set Color、Add/Copy Apply 保留欄位與編號焦點行為已補齊。
- [x] Development 假資料僅在 App 環境選為 Development 且執行 DEV build 時啟用；切換環境/API 位址會重載。Test/Production 使用 API。
- [x] Delete 會把目前列合併到多選項目，逐筆檢查引用與刪除；部分成功後重載並保留錯誤，符合舊表單逐筆處理行為。
- [x] 更新 EP106 規格、索引及家用端 EP106 待辦；本次未新增表/DDL，既有兩張引用表 Oracle DDL/PG CREATE SQL 已交付。
- [ ] 家用端比對兩張引用表 schema/migration、授予最小 SELECT 權限，移除 DELETE fail-closed 503；部署後驗證 409/204 引用檢查。
- [x] EP107「設備點檢表設定」已於後續進度完成 WinForms Form/Designer/Service 對照與 Vue 補正。
- [ ] 未執行 build 或測試；不代表 Production 刪除驗收已完成。


## EP107 設備點檢表設定公司端 UI 補正（2026-11-16）

- [x] 開工前 pull GitHub `main`；最新已推送基準為 `1ab42be`，`git pull --ff-only origin main` 確認已同步。來源 WinForms 為 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_QCListBasis`。
- [x] 對照主 Form、Def、Designer 與 Service：主從 Grid、作用列+多選刪除、複製、明細多選刪除/重排、主檔/明細驗證及 Confirm/Close/Apply 流程已補齊。
- [x] Def 表單加入建立/修改稽核 GroupBox，按鈕順序 Confirm/Close/Apply；明細操作按鈕移至清單右側並依 WinForms 排序。新增/複製 Apply 保持原模式。
- [x] Development 假資料只有 DEV build 且選取 Development preview 時啟用；Production/Test 走 API。修正明細刪除重複宣告，環境/API 切換關閉編輯器並取消舊明細請求。
- [x] 更新 `docs/modules/EQP/設備設定/設備點檢表設定.md`、規格索引及家用端 EP107 API TODO；既有兩表 PostgreSQL CREATE SQL 已存在，本輪沒有新增資料表或 DDL。
- [ ] 家用端逐欄比對兩張 YMES 表、實作主從原子 CRUD/options API；完成後公司端 pull 對接並核對 DataStamp/稽核欄位。
- [x] EP301「載具分類資料」公司端 UI、規格、DDL 與 API handoff 已推送；Production schema/API 仍由家用端待辦追蹤。
- [ ] 未執行 build 或測試；不代表 Production API 已完成。


## EP301 載具分類資料公司端進度（2026-11-16）

- [x] EP107 推送後重新 pull，確認 GitHub `main` 已同步於 `f6cb3d9`；來源位於 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierCategory`。
- [x] 直接讀取 WinForms 主/Def Form、Designer、`wsEQP`、`clsCarrierBasis`；記錄狀態查詢、按鈕、批次作用列、核准工作流、Delete 0/-1 實刪/2 停用/1 拒絕、DataStamp、Audit 與表單尺寸。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 及本機受保護設定唯讀取得 MESTEST.TBLEQPCARRIERCATEGORY DDL、欄位、constraint 與 index；建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL。8 欄，PK 唯一索引，無其他 index/FK。
- [x] 新增 Vue DEV 頁、Development-only 假資料、三層載具分類選單與 EP301 route。Production API 呼叫契約及 Issue/Job 簽核複雜度已記錄，DEV 核准明確簡化為直接 0→2。
- [x] 建立 `docs/modules/EQP/載具/載具分類資料.md`，更新 `docs/DATABASE_CONNECTION.md`、規格索引及 `todolist_homepc.md` API/schema 交接。
- [ ] 家用端確認 YMES schema/migration、實作 Production CRUD/簽核 API；公司端 pull 後對接並核對部分成功、DataStamp 與狀態轉換。
- [x] 已開始 `CARRIER TYPE` / EP302「載具類別資料」；進度見下方 EP302 區塊。
- [ ] 未執行 build 或測試；Production API 未建置。

## 2026-11-16 ENT ET101 客戶基本資料公司端交接

- [x] 開始前 pull GitHub `origin/main`；當時最新提交為 `c6c7895`，工作樹乾淨。
- [x] 追查 `frmCustomerBasis`、`frmCustomerBasisDef`、Designer、ET_ETDataImport、`clsCustomerBasis` 與 `wsENT`，記錄清單欄位、表單欄位/鎖定規則、批次刪除/核准、Customer BASIS 簽核與 Excel 匯入流程。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從本機受保護連線唯讀查詢 `TBLENTCUSTOMERBASIS`、`TBLENTCUSTOMERCONTACTOR`、`TBLENTCUSTOMERSHIPADDR` 的 DDL、NULL/default、PK、索引/FK；沒有查客戶業務資料或執行 Oracle DDL。
- [x] 新增 Vue DEV 頁 `web/src/components/CustomerBasisMaster.vue`、FunctionNo `CUSTOMER BASIS` 路由 `/YMES/Customer/Basis` 及 fallback 三層選單；Production 經 API，Development 使用標明的 localStorage 假資料。
- [x] 新增 `docs/modules/ENT/客戶基本資料.md`、Oracle source logical DDL、PostgreSQL 空表 CREATE SQL、規格索引、DB 連線查詢紀錄及家用端 migration/API/簽核/匯入待辦。
- [ ] 未執行 build/test 或瀏覽器人工驗收；待後續依要求驗證。
- [ ] 依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續下一個未遷移 Function；EQP AutoLoader 原始碼已確認並完成 DEV 頁，避免再標成 binary-only。

## EP302 載具類別資料公司端進度（2026-11-16）

- [x] 從 `D:\iMES\CSiMES\STD\MESWin\EQP\EP_CarrierType` 讀取 Form/Def/Designer；讀取 `wsEQP.Load/Add/Edit/Delete/ApproveCarrierType` 與 `clsCarrierBasis` CRUD/query 實作。
- [x] 使用 PowerShell 5.1 + ODP.NET、本機受保護設定，只讀查詢 MESTEST.TBLEQPCARRIERTYPE 的 DBMS_METADATA DDL 與欄位/constraint/index 字典。13 欄，PK 唯一索引，無外鍵或額外索引。
- [x] 建立 Oracle source DDL 和 PostgreSQL 空表 CREATE SQL；UI 規格記錄 UseLayer 0/1、已核准 CarrierCategory 查找、NUMBER(3,0) 數值限制、狀態和 Issue/Job 核准流程。
- [x] 新增 Vue DEV 頁、carrier type route 與主選單 FunctionNo 映射；DEV 核准明確簡化 0→2，分類查找只列已核准類別。
- [ ] 家用端確認 YMES schema/migration、實作 CRUD/分類選項/正式核准 API；公司端 pull 後對接與核對。
- [ ] 下一個公司端功能依既有全部模組待辦順序續做；先以 WinForms Form/Designer/Service 判斷功能完整範圍，結構缺漏時用 PowerShell 5.1 直連 Oracle 查詢。
- [ ] 未執行 build 或測試；Production API 未建置。

## EP303 載具基本資料公司端進度（2026-11-16）

- [x] 先 pull GitHub main，確認最新基準 a1fa7cd；工作樹初始乾淨。
- [x] 依序核對 frmCarrierBasis/Def、frmBasisTemplat、wsEQP、clsCarrierBasis；完成清單欄位、唯讀/選擇器、狀態篩選、Add/Edit/Copy/Delete/Approve、DataStamp、WIP 同步及簽核規則規格。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護設定唯讀查詢 TBLEQPCARRIERBASIS、TBLWIPCarrierState、TBLSMDFactoryBasis 的 DDL/欄位/約束/索引；未查詢業務資料，未在 Oracle 執行 DDL。
- [x] 建立主表與依賴表 Oracle source DDL、PostgreSQL 空表 CREATE SQL；更正 first-phase SQL 的 DEFAULT，移除這三表未證實欄位的猜測註解。
- [x] 新增 Vue DEV 畫面與 /YMES/Equipment/CarrierBasis 路由，三層選單 FunctionNo 為 CARRIER BASIS；加入三個選項視窗、KD 黃色欄位、Apply/Audit 與批次處理。
- [x] 建立 docs/modules/EQP/載具/載具基本資料.md，更新規格索引、資料庫連線記錄及家用端 API/DDL 待辦。

## EP401 設備參數資料公司端進度（2026-11-16）

- [x] 開始前 pull GitHub 最新 main；遠端已是 `4628cea`，無家用電腦新提交。
- [x] 追查 `frmEQPRecipeBasis`、Basis/Detail Define、wsEQP 與 clsEQPBasis/kcBasis_UPD，核對雙 UltraGrid、欄位/按鈕、狀態限制、簽核、刪除與 Copy/Version 明細實際複製欄位。
- [x] Oracle DDL 直接查詢 MESTEST；明確以 Windows PowerShell 5.1 啟動 Oracle.ManagedDataAccess。查詢 `TBLEQPRECIPEBASIS`、`TBLEQPRECIPEDETAIL` 的 DDL、NOT NULL/default、PK、索引/FK，並查明 Type=-1/0/1 對應 NONE/TYPEVALUE/RANGE。沒有查 MES 業務資料或在 Oracle 執行 DDL。
- [x] Vue DEV 頁 `web/src/components/EquipmentRecipeBasisMaster.vue`：雙清單、狀態篩選/RecipeGroup 查詢、主檔 Add/Edit/Copy/Delete/Approve/Version、明細 Add/Edit/Delete、ElementType=9 本地選擇器、KD 黃色表單與本機假資料；Production 顯示 API 尚未建置，不使用假資料寫入。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL，並校正 first-phase Recipe Basis/Detail 的欄位 DEFAULT/NOT NULL/額外索引。
- [x] 建立 `docs/modules/EQP/設備設定/設備參數資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 與 `todolist_homepc.md`。
- [ ] 尚未執行 build/test 或瀏覽器人工驗收；Production schema/API/簽核仍由家用電腦依交接清單完成。
- [ ] 尚未以瀏覽器人工驗收；未執行 build 或測試。家用端 API/schema 完成後，pull 最新版並對接正式端點。
- [ ] 後續繼續依 docs/MES_MENU_FUNCTION_INVENTORY.md 逐功能復刻；每個功能先讀 WinForms Form/Designer/Service，必要時以 PowerShell 5.1 直連 Oracle 查 DDL，完成後同步規格與兩台電腦待辦。

## ENT ET102 客戶連絡人資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` pull；遠端已是 `f5dce82`，工作樹乾淨。
- [x] 核對 `frmCustomerContactor`、`frmCustomerContactorDef`、`wsENT.Load/Add/Edit/DelCustomerContactor`、`kcENT.clsCustomerBasis`：客戶查找、欄位、排序、多選刪除、Copy、Apply/Confirm、主鍵唯讀等規則已寫入功能規格。
- [x] 沿用 ET101 直連 Oracle 已取得的 `TBLENTCUSTOMERCONTACTOR` DDL；沒有重複查詢、沒有增加第二份 Create SQL，也沒有讀取 MES 客戶業務資料。
- [x] 新增 Vue DEV 畫面 `web/src/components/CustomerContactorMaster.vue`，路徑 `/YMES/Customer/Contactors`、FunctionNo `CUSTOMER CONTACTOR`，Development localStorage 假資料含客戶選擇、CRUD/Copy/多筆刪除及稽核欄位。
- [x] 新增 `docs/modules/ENT/客戶連絡人資料.md`，更新規格索引、`docs/DATABASE_CONNECTION.md` 及本家用端 API 待辦；沿用 ET101 的既有 `database/reference/mestest_customer_basis_create.sql`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端建立。
- [ ] 下一段依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 往後核對未遷移的 ENT/EQP 功能與其 WinForms 程式；缺少 DDL 時直接以 PowerShell 5.1 + ODP.NET 查詢 Oracle 結構。

## ENT ET106 客戶出貨地址資料公司端進度（2026-11-16）

- [x] 依序核對 `frmCustomerShipAddr`、`frmCustomerShipAddrDef`、`frmCustomerShipDeclarationDef`、ENT WebService/Core，確認主畫面雙 Grid、只選已核准客戶、按客戶載入全部報關列、出貨地址表單與報關子功能規則。
- [x] 出貨地址表 `TBLENTCUSTOMERSHIPADDR` 沿用 ET101 已直連取得的 DDL；缺少的 `TBLENTCUSTOMERSHIPDECLARATION` 已使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線直接唯讀查得。
- [x] 報關表原始 DDL 有 13 欄及 8 個 NOT NULL constraint；資料字典沒有 PK、unique/一般索引或 FK。已將 Oracle logical DDL 與不加 PK 的 PostgreSQL 空表 CREATE SQL 追加到客戶 DDL 檔案。
- [x] 新增 Vue DEV 雙 Grid 頁 `web/src/components/CustomerShipAddressMaster.vue`、路徑 `/YMES/Customer/ShipAddresses`、FunctionNo `CUSTOMER SHIP ADDRESS`；含已核准客戶選擇、地址/報關 CRUD、報關預設值、Taker/Forwarder/Broker DEV 選擇器、稽核欄位與 KD 黃色表單。
- [x] 建立 `docs/modules/ENT/客戶出貨地址資料.md`，更新規格索引、DB 連線記錄及家用端 migration/API 待辦。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 下一功能已依選單開始 ET103 報關行基本資料；最新狀態見下方 ET103 區塊。

## ENT ET103 報關行基本資料公司端進度（2026-11-16）

- [x] 開始 ET103 前已完成 ET106 並推送 `49fbd9c`；其後讀取 `frmBrokerBasis`、`frmBrokerBasisDef`、`wsENT`、`kcBasis_Query`、`kcBasis_UPD`、`kcENT.clsCustomerBasis`。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從先前提供的受保護連線直接唯讀查詢 `MESTEST.TBLENTBROKERBASIS` DDL、欄位 NULL/default、PK、索引/FK；未查業務資料、未執行 Oracle DDL，憑證未輸出/存檔。
- [x] 查得 13 欄、BrokerNo 單欄 PK/唯一索引，無 FK/其他索引。補 Oracle source DDL 與 `ymes.tblentbrokerbasis` PostgreSQL CREATE SQL，並校正 first-phase reference 中此表遺漏的三個 DEFAULT。
- [x] 新增 `web/src/components/BrokerBasisMaster.vue`、`/YMES/Customer/Brokers` 路徑與 `BROKER BASIS` 對應；DEV 假資料支援狀態篩選、搜尋、CRUD、停用/刪除、Apply/Confirm、稽核及直接核准預覽。
- [x] 新增 `docs/modules/ENT/報關行基本資料.md`，記錄 WinForms 版面、欄位限制、狀態刪除規則、`BROKER BASIS` 簽核流程及 YMES API 契約；更新規格索引、DB 連線記錄及家用端待辦。
- [ ] 家用端比對 YMES schema/migrations，依 DDL 只新增缺少的表；實作 Production CRUD、DataStamp、IssueState 軟刪除/硬刪除與既有簽核服務 API。
- [ ] 本次未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] 後續已依 ENT FunctionOrder 開始 ET104「貨運行基本資料」；詳細進度與待辦見下方 ET104 區塊。

## ENT ET104 貨運行基本資料公司端進度（2026-11-16）

- [x] 開始前從 GitHub `main` 執行 `git pull --ff-only origin main`，遠端已是 `8d87cd8`；前一段 ET104 DDL 草稿保留。
- [x] 核對 `frmForwarderBasis`、`frmForwarderBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveForwarderBasis` 與 `kcBasis_Query`、`kcBasis_UPD`：狀態篩選、多選操作、欄位/唯讀、Broker 選擇、Apply/Confirm、狀態2舊服務缺陷都記入功能規格。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從既有受保護連線唯讀取得 `MESTEST.TBLENTFORWARDERBASIS` DDL、欄位 nullable/default、PK/index/FK；只查結構，未查業務列、未在 Oracle 執行 DDL，憑證未輸出。
- [x] Oracle 原始 logical DDL 與 PostgreSQL `ymes.tblentforwarderbasis` 空表 CREATE SQL 已加入客戶主檔 reference；同步校正 first-phase reference 的 DEFAULT 與欄位註解。
- [x] 新增 Vue DEV 頁 `web/src/components/ForwarderBasisMaster.vue`、`/YMES/Customer/Forwarders` 路徑、`FORWARDER BASIS` 對應；含狀態篩選、動態選列、多選、CRUD、停用/刪除、核准預覽、稽核及只顯示已簽核報關行的選擇器。
- [x] 新增 `docs/modules/ENT/貨運行基本資料.md`，更新功能索引、DB 連線記錄、ET103 報關行 lookup query 規格與家用端 ET104 schema/API/簽核 TODO。
- [ ] 家用端比對 YMES schema/migrations；只新增缺漏表或欄位，實作 ET104 Production CRUD、BrokerNo=已簽核報關行驗證、DataStamp、狀態刪除及 `FORWARDER BASIS` 簽核 API。
- [x] ET103 radio/checkbox KD 黃色 accent 與簽核完成後 audit/DataStamp 規格修正已包含於 ET104 提交 `39b2973`。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [x] ET104 與 ET103 收尾修正已提交並推送 GitHub：`39b2973 feat: add forwarder basis UI`。
- [x] 後續已依 ENT FunctionOrder 開始 ET105「取貨公司基本資料」；最新進度見下方 ET105 區塊。

## ENT ET105 取貨公司基本資料公司端進度（2026-11-16）

- [x] 依 WinForms `frmTakerBasis`、`frmTakerBasisDef` inline Designer、`wsENT.Load/Add/Edit/Del/ApproveTakerBasisIssuestate` 與 `kcENT.clsCustomerBasis` 核對狀態、欄位、按鈕、稽核與來源版本不一致處。
- [x] Oracle DDL 原先不在 repo。以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線只讀取得 `MESTEST.TBLENTTAKERBASIS` 的 DBMS_METADATA DDL/欄位 NULL-default/constraints/indexes；只查結構，不讀業務列、不執行 Oracle DDL，連線值未輸出。
- [x] Oracle source logical DDL、PostgreSQL 空表 CREATE SQL 已追加 customer reference；first-phase SQL 預設值與猜測性欄位註解已修正。
- [x] 新增 Vue DEV 頁 `web/src/components/TakerBasisMaster.vue`、路徑 `/YMES/Customer/Takers` 與 `TAKER BASIS` 路由；含狀態篩選、即時選列、多選 CRUD、停用/刪除、核准預覽、KD 黃色欄位與稽核。
- [x] 新增 `docs/modules/ENT/取貨公司基本資料.md`，更新功能索引、DB 連線記錄與家用端 API/schema TODO。
- [ ] 家用端先比對 YMES schema/migrations；缺少才新增 Taker table migration。實作 GET/POST/PUT/DELETE/approve、DataStamp compare-and-swap、稽核與 `TAKER BASIS` 共用簽核服務。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API 待家用端完成。
- [x] 下一項 ET201 已開始；完成狀態見下方 ET201 區塊。

## ENT ET201 外包商基本資料公司端進度（2026-11-16）

- [x] 本次先 pull `origin/main`，確認 GitHub 最新版已同步且工作樹乾淨。
- [x] 對照 `frmSubcontractor`、`frmSubcontractorDef`、`frmSubcontractorContDef`、wsENT 與 clsCustomerBasis；記錄雙 Grid、按鈕、欄位、主從刪除、複製、套用與 Issue/Job 簽核流程。
- [x] Oracle 兩張表結構已由既有 PowerShell 5.1 + ODP.NET 唯讀查詢取得；新增 source DDL 與 PostgreSQL 空表 CREATE SQL。未讀取業務資料，也未對 MES Oracle 寫入。
- [x] 新增 `web/src/components/SubcontractorBasisMaster.vue`，Development 使用獨立 localStorage 假資料，實作上下雙 Grid、狀態/編號查詢、多選、主檔/連絡人新增修改複製刪除、核准、Mail、KD 黃色欄位與稽核區。
- [x] 加入 `/YMES/Customer/Subcontractors` 路由及 FunctionNo `SUBCONTRACTOR BASIS`；建立外包商規格並更新模組索引、Oracle 連線紀錄及家用端待辦。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/migration/Issue service 仍待家用端完成。
- [x] 後續依 ENT Function inventory 開始 ET202 `ENTOSITEM` 外包項目設定；以下為完成紀錄。

## ENT ET202 外包項目設定公司端進度（2026-11-16）

- [x] 開始前執行 `git pull --ff-only origin main`；GitHub `main` 已是最新版。
- [x] 對照 `frmENTOSItem`、`frmENTOSItemDef`、wsENT、`clsCustomerBasis`，記錄查詢列、Grid 欄位/寬度、Add/Edit/Delete/Copy/Close、表單欄位驗證、Apply/Confirm 與外包流程選取條件。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護連線唯讀查到 `MESTEST.TBLENTOSITEM` DDL、欄位 nullable/default、constraints、PK/index；未讀業務資料、未在 Oracle 執行 DDL、憑證未輸出。
- [x] 新增 Oracle source DDL、PostgreSQL 空表 CREATE SQL、ET202 模組規格與規格索引/連線查詢紀錄。
- [x] 新增 Vue DEV 頁 `web/src/components/ENTOSItemMaster.vue`，路徑 `/YMES/Customer/OSItems`、FunctionNo `ENTOSITEM`；含單 Grid、列選取、多選刪除、Add/Edit/Copy、KD 黃色欄位、Audit、Apply/Confirm、外包流程 lookup 與 localStorage 假資料。
- [ ] 家用端比較實際 YMES schema/migrations；缺表時依 `database/reference/mestest_entositem_create.sql` 建 migration，不得以 IF NOT EXISTS 代替欄位差異檢查。
- [ ] 家用端實作 ENTOSITEM GET/POST/PUT/DELETE、DataStamp CAS、伺服器 audit、ProcessClass=3 + IssueState=2 流程驗證及權限檢查；遵守 API/DB 分離，Vue Production 不可使用假資料。
- [ ] 公司端尚未執行 build、測試或瀏覽器驗收；Production API 尚待家用端完成。
- [ ] ENT 八個 Function 已有 ET101/102/103/104/105/106/201/202 規格與 DEV 頁；下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 繼續盤點下一個尚未完整復刻的 MES 功能，先核對 Form/Service/Core，再查 Oracle DDL。

## 2026-11-16 SMD SD103 儲位基本資料公司端進度

- [x] 從 GitHub main 快轉檢查最新版；本地與 origin/main 均為 3ab417d，沒有新提交。
- [x] 直接讀 WinForms 主畫面、定義窗、wsINV、clsInventory 與更新版 clsBasis_UPD；記錄四狀態篩選、欄位/按鈕、InventoryNo 選取、複合鍵、Copy、批次 Delete/Approve、IssueNo 與 DataStamp 版本差異。
- [x] 用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過本機受保護連線唯讀查 MESTEST.TBLINVLOCATORBASIS DBMS_METADATA 與資料字典；確認複合 PK、沒有 FK/額外索引。新增原始 Oracle DDL 和 PostgreSQL 空表 CREATE SQL，校正 first-phase bootstrap 預設值並刪除未核實欄位註解。
- [x] 新增 Vue DEV 頁 web/src/components/LocatorBasisMaster.vue，路由 /YMES/SMD/Locators、FunctionNo=LOCATOR BASIS；依 WinForms 具備查詢、狀態切換、選列、多選、Add/Edit/Copy/Delete/Approve、已核准庫存挑選器、KD 黃色欄位、稽核及 Apply/Confirm。
- [x] 新增 docs/modules/SMD/儲位基本資料.md；更新資料庫連線查詢紀錄、功能索引、家用端 API/schema handoff。
- [ ] 未執行 build、test 或瀏覽器驗收；家用端需比對已部署 YMES schema 並補 forward migration/API 和正式 Issue/Job 核准流程。
- [x] SD107「生產線別基本資料」已依 Form/Service/Core 完成 DEV 頁、規格、Oracle DDL/PG CREATE 與家用端 API/schema handoff。下一個尚未完成的 SMD FunctionKey 為 SD108「設備狀態監控_區域」。
## 2026-11-16 SMD SD107 生產線別基本資料公司端進度

- [x] 直接對照 frmPDLineBasis / frmPDLineBasisDef、wsSMD、clsSMDBasis 與 clsBasis_UPD；整理欄位、按鈕、ValueList、四種 IssueState、Apply、批次操作、Delete 與 Issue/Job 核准邏輯。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 經本機受保護連線唯讀查得 TBLSMDPDLINEBASIS 完整 DDL、欄位 null/default、constraints、PK/index；未讀業務資料，未執行 Oracle DDL。
- [x] 新增 web/src/components/ProductionLineBasisMaster.vue，路由 /YMES/SMD/ProductionLines，FunctionNo=SMD PDLINE BASIS；Development 使用 localStorage 假資料，Production 只呼叫 API。
- [x] 新增 docs/modules/SMD/生產線別基本資料.md、Oracle source DDL 與 PostgreSQL 空表 CREATE SQL；修正 first-phase bootstrap 的來源 defaults 並移除猜測欄位註解。
- [x] 更新 SMD 規格索引、Oracle 連線查詢紀錄及 todolist_homepc.md API/schema handoff。
- [ ] 家用端 2026-11-17 app-browser 驗收：主 menu 可登入；SMD → 廠區設定 → 生產線別基本資料仍開到「尚未遷移」占位畫面。Production Web 尚未佈署公司端的 Vue 主檔頁。
- [ ] 修復 `web/src/components/AccessoryTypeMaster.vue` 第 52、59 行的 TypeScript 語法錯誤（`row.description||:` 缺少 `||` 後的運算元），再執行 Web build；目前 `npm.cmd run build` 因 TS1109 失敗，阻擋 Vue 主檔部署。
- [ ] `ProductionLineBasisMaster.vue` 已在 main；待 Web build 通過後通知家用端以 SSH 只更新 `ymes-web`，再驗收 Production 頁與 API 串接。家用端 API 健康檢查 200；登入後 GET `/api/smd/pd-line-basis` 回 `[]`；正式 `/options` 回 503，等 ValueList handoff；Approve 也等 Issue/Job handoff。
- [ ] 後續 SMD 頁面：SD108 設備狀態監控_區域、SD109 工位基本資料；依序讀 WinForms Form/Designer/Service/Core，再查缺少的 Oracle DDL。
- [ ] 請以 Oracle ValueListProvider 同一來源查 `TBLSYSVALUELIST` 中 `TABLENAME='TBLSMDPDLINEBASIS'`、`FIELDNAME='PDLINESTATUS'` 的正式值、排序與顯示文字，並交付繁中資源翻譯；不要以 Vue DEV 標籤或舊註解代替。
- [ ] 供家用端實作 Production Approve：核實 Issue/Job/IssueDetail/IssueJobMap 等簽核資料表的 Oracle DDL、資料欄位與 Service 建立流程；目前 YMES 尚無 Issue/Job 表，不能用 DEV 的直接 0→2 模擬代替。

## SMD SD108、SD109 公司端交接（2026-11-17）

- [x] 開始前 pull GitHub main，遠端新版本為 09dfd65；包含家用端 SD106 過帳電腦區域 migration/API 及 SD104/SD105 schema handoff，已一併納入。
- [x] 依 WinForms frmAreaShow、frmAreaShift、frmAreaShiftDef、frmSMDPDLinePositionBasis、frmSMDPDLinePositionBasisDef 與 clsSMDBasis 核對畫面、事件、班別登入狀態檢查及複合鍵。
- [x] SD108 區域圖、縮放/網格、右鍵狀態/歷史、雙擊班別維護與班別人員增刪已做 Vue Development Preview；SD109 搜尋、複合鍵 CRUD/Copy、多選刪除、作業區與 eSOP 選擇、Apply/Confirm 已做 Vue Development Preview。
- [x] Oracle DDL 由本機受保護連線以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得；新增兩組 source DDL 與 PostgreSQL 空表 CREATE，並修正 first-phase SQL 中 SD109 defaults/猜測性欄位註解。
- [x] 完成 SMD SD108/SD109 規格、功能索引、DB 查詢紀錄與家用端 API/schema handoff。補明 nullable DataStamp 用 `dataStampIsNull=true` 做精確 NULL CAS，並修正 SD109 刪除請求漏傳版本值。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/schema 需家用端按 todolist_homepc.md 完成。
- [ ] 下一步依 MES_MENU_FUNCTION_INVENTORY 的 ModuleOrder/FunctionOrder 繼續找下一個尚未完整復刻的 Function（接續 MTL），先核對 WinForms Form/Designer/Service/Core，再查 Oracle DDL。


## 2026-11-17 MTL ML106 物料品管項目設定公司端交接

- [x] Pull GitHub main，確認遠端包含 ML102、ML105 最新公司端提交；之後開始 ML106。
- [x] 依 frmMaterialQCItemSet.vb/Designer、wsMTL.LoadMTLQCItem/UpdMTLQCItem、clsMTLBasis、clsQCBasis 核對 Form/Service/Core：前綴查詢與全 IssueState、三種啟用 QC catalog、雙清單多選/雙擊、按 Confirm 才保存、IQC 虛擬站及 QC bind log 副作用。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 既有保護連線，直接唯讀查 MESTEST 三個 QC catalog 與 TBLQCITEMBINDLOG DDL；新增 source DDL 與 PostgreSQL 空表 CREATE SQL。現有 TBLMTLQCITEM、TBLOPQCITEM 沿用已交付 reference SQL。
- [x] 新增 Vue DEV 頁 /YMES/MTL/MaterialQCItems，FunctionNo=MTL QCITEM；保持舊 GET /qc-items 類別範本契約，ML106 改走獨立 GET /qc-item-library；避免載入失敗以空集合覆寫。
- [x] 新增 ML106 規格、索引列、家用端 schema/API handoff。
- [ ] 沒有執行 build、test 或瀏覽器驗收。Production API/schema 尚待家用端，完成後公司端 pull 並逐項對接。
- [ ] 下一步按 MES_MENU_FUNCTION_INVENTORY.md MTL FunctionOrder 繼續下一個未完整遷移功能；不得把簡化 DEV 佔位頁算成完成。


## 2026-11-17 MTL ML103 物料基本資料公司端進度

- [x] Pull GitHub main，確認目前最新為 0e50278。
- [x] 依 frmMaterialBasis / frmMaterialBasisDef、WinForms Designer、wsMTL、clsMTLBasis 核對狀態篩選、MaterialNo 查詢、ValueList 欄位、供應商子 Grid、Add/Edit/Copy、多選 Delete/Approve、Property/QC/Import/GumHour 功能。
- [x] Vue 主 Grid 加入多選核取方塊；核准與刪除逐筆呼叫現有單筆 API，DEV 假資料執行相同狀態轉換及關聯假資料清理。已簽核 Delete 設為不使用，未簽核/不使用 Delete 實體移除。
- [x] Vue 已接上 options API 的 CountWay/CheckLotNo 選項與 GumHour 顯示參數契約；Production API 目前回傳空 CountWay/CheckLotNo，且尚未回傳 MATERIALMULTITIMECTRL/GumMaterialType。另發現現有 Approve API 尚未執行條件式 GumHour 檢查，詳見 todolist_homepc.md。
- [x] 更新 docs/modules/MTL/物料主檔.md 與規格索引；沿用已查 8 張物料表 Oracle DDL/PG CREATE，ML103 無新增資料表。
- [ ] 未執行 build、test 或瀏覽器驗收。Production options API 待家用端補齊來源 ValueList/參數後 pull 回公司端再對照。
- [x] ML104 物料群組資料及後續 FunctionOrder 中的 ML107「物料標籤對應設定」均已完成公司端 DEV 頁與規格；Production API/schema 交家用端。ML107 規格見本檔較後方區塊。

## 2026-11-17 MTL ML104 物料群組資料公司端進度

- [x] 對照 `frmMaterialGroup`、Designer、`frmMaterialGroupBasisDef`、`wsMTL`、`clsMTLBasis`：GroupNo 查詢、無 IssueState、主 Grid 欄位/排序、Add/Edit/Copy/Delete、DataStamp、Apply/Confirm 及下方雙清單替換式成員編輯已記錄。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀取得 `TBLMTLMATERIALGROUPBASIS` DBMS_METADATA DDL、欄位 Nullable/default、constraints、PK/index；確認兩個欄位以 CHECK constraint 強制 NOT NULL、DataStamp DEFAULT 0 nullable。未讀業務資料、未執行 Oracle DDL，連線秘密未輸出。
- [x] 新增 `database/reference/mestest_mtl_material_group_source_ddl.sql` 及 PostgreSQL 空表參考 `mestest_mtl_material_group_create.sql`；校正 first-phase 物料群組主檔 DEFAULT/NULL 與欄位註解，明細沿用已查來源 SQL。
- [x] 新增 `web/src/components/MaterialGroupMaster.vue`、三層選單 `MTL GROUP` 與 `/YMES/MTL/MaterialGroups` 路徑；DEV 假資料頁包含主 Grid、多選刪除、編輯/複製/套用、稽核欄、類別 lookup、同類已核准物料雙清單與整組儲存。
- [x] 新增 `docs/modules/MTL/物料群組資料.md`，記錄 Form/Service/Core、Oracle DDL、API 交易與 Vue 規則；更新索引、Oracle 連線查詢紀錄及家用端 API/schema handoff。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API/schema 仍待家用端完成。
- [x] ML107「物料標籤對應設定」已完成公司端 WinForms/Service/Core 對照、Oracle DDL 直查、Vue DEV 頁、功能規格及家用端 API/schema handoff，詳見本檔 ML107 區塊。

## 2026-11-17 MTL ML107 物料標籤對應設定公司端進度

- [x] 開始前 pull GitHub main，確認 YMES 已是 `71fef1e`，工作樹乾淨。
- [x] 對照 `frmMTLLabelBasis`、Designer、`frmMTLLabelBasisDef`、`wsMTL`、`wsPRD`、`clsMTLBasis`、`clsBasis_UPD`、`clsProduct`：主畫面 Filter/Like 條件、初始不查詢、Grid 欄位、四個資料操作按鈕、Add/Edit/Copy/Delete、LabelSource=5、MaterialNo=`*`、DataStamp/Audit、Apply/Confirm 與 lookup 限制。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機既有受保護連線直接唯讀呼叫 DBMS_METADATA 並查 ALL_TAB_COLUMNS/ALL_CONSTRAINTS/ALL_INDEXES/ALL_IND_COLUMNS；取得 `TBLMTLLABELMAP` 與 label lookup `TBLPRDLABELBASIS` DDL。只查結構，未讀 MES 業務資料、未執行 Oracle DDL；連線值未輸出。
- [x] 新增兩張表 Oracle source DDL、PostgreSQL 空表 CREATE SQL、`docs/modules/MTL/物料標籤對應設定.md`，並更新功能規格索引、Oracle 連線查詢紀錄及家用端 API/schema handoff。
- [x] 新增 `web/src/components/MaterialLabelMapMaster.vue`，加入 FunctionNo `MTL LABEL MAP BASIS` 三層選單及 `/YMES/MTL/MaterialLabelMaps` route；DEV 頁包含初始空 Grid、三組查詢/lookup、多選刪除、Add/Edit/Copy、LabelSource/物料 lookup、稽核資訊及 KD 黃色輸入樣式。
- [ ] 未執行 build、測試或瀏覽器驗收；Production schema/API 待家用端依 `todolist_homepc.md` 比對及完成。
- [ ] 下一步依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 確認其餘未完成 FunctionNo，再讀 WinForms Form/Designer/Service/Core，查明缺少的 Oracle DDL 並續做 DEV UI。

## 2026-11-17 MTL MLB01 來料 AutoLoader 格式檔設定公司端進度

- [x] Pull GitHub main，從 `99c9fef` 開始；家用端先前已推送的 MTL ML107 內容已同步。
- [x] 直接對照 `frmMaterialVendorAutoLoaderSetup`、`frmMaterialVendorAutoLoaderSetupDef`、相關 Dynamic Parameter/Test File 表單、`wsMTL_Base`、`clsMTLBasis_Industry_Base`；記錄初始不查詢、精確 Folder/Vendor 條件、Grid 可見欄、Add/Edit/Delete/Copy/Query/Close、格式生成、基本檢查、Apply/Confirm、CLOB 與 DataStamp 原始碼差異。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護設定直連 MESTEST 唯讀取得 `TBLMTLVENDORALSCRIPT` DBMS_METADATA DDL 與欄位/NULL/default/PK/index；無業務列查詢、無 Oracle 寫入，連線值未輸出。
- [x] 新增 Oracle source DDL、PostgreSQL 空表參考、MLB01 功能規格、Vue DEV 頁與三層 `物料資料 → 行業包 → 來料AutoLoader格式檔設定` 選單，路徑 `/YMES/MTL/MaterialVendorAutoLoaderSetup`。
- [x] Vue DEV 頁具有初始空 Grid、精確查詢、右側 WinForms 操作按鈕、多選逐筆刪除、唯讀 FormatScript 預覽、Add/Edit/Copy 定義窗、KD 淡黃輸入欄位、格式生成/檢查、CSV/TXT 預覽、動態測試參數、供應商/允許目錄/測試配方選擇器與 localStorage 假資料；Production 呼叫 API，不用假資料。
- [x] 更新 `todolist_homepc.md` 的 schema/API/parser handoff、`docs/DATABASE_CONNECTION.md` 及功能規格索引。
- [ ] DEV 測試檢查、build 與瀏覽器驗收未執行；使用者未要求測試。
- [ ] 家用端完成 YMES live schema 比對、migration/API 後，公司端 pull 並核對 Production 契約。
- [x] 2026-11-17 已完成 EQP `B_EQP AUTOLOADER SETUP` WinForms Form/Service/Core/DDL 對照、Vue DEV 頁及家用端 API/schema handoff；後續項目改從功能目錄中下一個尚未完成的 Function 選取。

## 2026-11-17 QC402 次分類基本資料公司端進度

- [x] 開工前 pull `origin/main`，確認 `0d16dac` 最新且工作樹乾淨。
- [x] 對照 `frmSClassBasis`/`frmSClassBasisDef`、`wsQC`、`clsHoldBasis`：主 Grid、MClassNo 已簽核 lookup、CRUD/Copy/Apply、四種狀態、DataStamp、`SCLASS BASIS` Issue Job 簽核及 `QC_SClassDataImport`/`SClassDataImporter` 規則。
- [x] Windows PowerShell 7 不相容舊版 ODP.NET 初始化；改用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，透過公司端既有受保護設定直連唯讀查 `MESTEST.TBLQCSCLASSBASIS` table DDL 及 `PK_TBLQCSCLASSBASIS` unique index DDL。只讀結構，沒有查業務列/執行 Oracle DDL，連線值未輸出。
- [x] 已建立 Oracle source DDL 與 PostgreSQL 空表 CREATE SQL；只保留 SClassNo PK，來源沒有 FK。MClassNo approved rule 由 QC402 API 查核，沒有在 SQL 中加 FK。
- [x] 已加入 Vue `/YMES/QC/SClassBasis` DEV 假資料頁、三層 QC 選單、`QC402` route、MClass 已簽核 lookup、主 Grid/表單、四狀態、多選刪除/核准及 SClassImport 預覽/逐組結果/取消 UI。
- [x] 已新增 `docs/modules/QC/次分類基本資料.md`，並更新規格索引、Oracle 查詢紀錄與 `todolist_homepc.md` API/schema handoff。
- [ ] 未執行 build、測試或瀏覽器驗收；Production API/schema 依家用端交接完成後再 pull 核對。沒有新增 API implementation。
- [ ] 下一段依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 接續 QC 尚未完成 Function；每頁先讀 WinForms Form/Designer/Service/Core，再查缺少的 Oracle DDL 與實際 CREATE SQL/API handoff。


## QC202 計數管制內容（公司端 2026-11-17）

- [x] 已 pull 最新 main；公司端完成 QC202 Vue DEV 頁、三層選單與 WinForms / Service / Core 規格。
- [x] Oracle TBLSPCATTLIMIT 沿用 QC201 透過 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀查 MESTEST 取得的 DBMS_METADATA/欄位/索引結果；新增 QC202 單表 source DDL reference，未讀業務資料、未在 Oracle 執行 DDL，未將連線秘密寫入 Git。
- [x] 建立 PostgreSQL JSONB 動態因子空表 CREATE reference；明確標示這是 YMES schema 適配，供家用端比對，不是可直接執行的 migration。
- [ ] 非自由文字 Factor 的正式 Reference 契約，統一依本檔頂部「QC201／QC202 公司端來源補交」補到 QC109／QC110 共用來源文件；不重交已存在的 QC202 CREATE SQL。QC202 API、JSONB／nullable CAS、部署與 app-browser 驗收仍屬家用端工作，本條不代表已完成正式驗收。
- [x] 後續接續 QC203 計數品管項目判別法則對應：已先讀 Form/Service/Core，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 從 MESTEST 唯讀取得三張專用表 DDL；完成 Vue DEV 畫面與 API/CREATE SQL handoff。

## 2026-11-17 QC203 計數品管項目判別法則對應公司端進度

- [x] 直接閱讀 QC_AttRuleMap 的四個 WinForms Form/Designer 程式、wsQC、clsQCBasis；追查狀態查詢、FactorOnline、RuleNo/ComponentCollect、RetestLot、Plan/Rule 選擇、抽樣參數、CRUD、Issue Job 與歷史刪除副作用。
- [x] 使用本機既有受保護連線設定，以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接連至 MESTEST，唯讀取得 TBLSPCQCATTRULEMAPBASIS、TBLSPCQCATTRULEMAPDETAIL、TBLSPCQCATTRULEMAPPARAMETER 的 DDL 與獨立索引；無業務列查詢、無 Oracle DDL 執行、連線秘密未輸出。
- [x] 新增 Oracle source DDL 與 PostgreSQL JSONB 因子值 CREATE 參考；標註三張 Oracle 表沒有 FK、serial sequence 是 YMES 建議，家用端需先比較 live schema 與共用 ID service。
- [x] 新增 /YMES/QC/AttributeRuleMap Vue DEV 頁與品質資料三層選單；完成已核准 QC Item 查找、四狀態、動態因子欄、主/Detail/Parameter 清單、Add/Edit/Copy/Delete/Approve、RetestLot/Plan/Rule/Parameter 編輯及獨立假資料。
- [x] 新增 QC203 功能規格並更新規格索引、Oracle 查詢紀錄、本待辦與家用端 API/schema handoff。舊 Core 刪除未刪 Parameter 的 orphan 行為和 YMES 完整交易清理差異已記錄。
- [ ] 尚未執行 build、測試或瀏覽器驗收；Production API/schema 待家用端完成後，公司端 pull 最新 main 再對接驗收。
- [x] 已於 2026-11-17 pull 最新 main，完成 QC204「刪除計數型品管項目量測記錄」Vue DEV 畫面、WinForms/Core 規格與 API handoff，使用既有 QC201 Oracle DDL reference，未新增表。
- [x] QC205「計量品管項目」已完成公司端 Vue DEV 頁與 API/schema 規格交接；進度與家用端待辦見本檔末段及 `todolist_homepc.md`。

## 2026-11-17 QC204 刪除計數型品管項目量測記錄公司端進度

- [x] 閱讀 frmQCItemDeleteForAttributes Form/Designer、wsQC Load/Delete methods、clsQCBasis Load/Delete Core；確認狀態範圍、Log 旗標、四種日誌刪除表、Tape Form 特例及歷史成功訊息被包裝成失敗的行為。
- [x] 沿用 `database/reference/mestest_qc_item_attributes_source_ddl.sql` 已有 QC Item master/BasisLog/DetailLog/EditLog/Tape DetailLog Oracle DDL；確認本功能不新增表，因此不另造 CREATE SQL，也未再次連 Oracle。
- [x] 新增 `/YMES/QC/AttributeMeasurementDelete` Vue DEV 頁、QC204 品質資料三層選單路由、操作確認、多選逐筆刪除與假資料回饋。
- [x] 新增 QC204 功能規格、模組索引、資料庫紀錄，以及家用端 log schema/API handoff。
- [ ] 尚未執行 build、測試或瀏覽器驗收；等待家用端 API/schema 完成後回公司端 pull 驗收。
- [x] QC209 的 WinForms Form/Designer/Service/Core 已核對，UI 與 API/schema handoff 已完成；詳本檔末段。
## QC205 計量品管項目公司端進度（2026-11-17）

- [x] 已 pull 最新 `origin/main`（本次 Already up to date），基於目前 THEIL BU1 WinForms 客製版直接讀主表、Define、Memo Forms、Designer、wsQC/Core 與共用 Parameter/Factor/OP/Simulation/SamplePlanChange 實作；非僅依截圖。
- [x] 已新增 `/YMES/QC/VariableQcItem` Vue 3 DEV 頁，含主 Grid、四 IssueState、CRUD/Copy/Delete/Approve、欄位驗證、Factor/Parameter/OP/Simulation/SamplePlanChange/Memo 視窗、假資料；複製會比對 QCItem/目標 Plan 參數並提供 WinForms 來源選擇器。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護設定直連 MESTEST，只讀取得 Memo/變數量測/引用檢查所需 Oracle DDL；不讀業務列、不執行 Oracle DDL、不輸出或提交連線秘密。四個 Oracle 舊刪除目標在 MESTEST 不存在，已記錄。
- [x] 已新增 Oracle source DDL、Memo PostgreSQL Create 參考、`docs/modules/QC/計量品管項目.md`、索引及 DB 連線查詢紀錄，並在 `todolist_homepc.md` 寫明 API/schema handoff。
- [ ] 尚未執行 build、測試或瀏覽器驗收；家用端完成 PostgreSQL schema/API 後，公司端 pull 最新 main 並做 Production API 對接驗收。
- [x] QC209「計量品管項目管制法則對應」公司端 Vue DEV 頁、規格與 Create/API handoff 已完成，見本檔末段。

## QC209 計量品管項目管制法則對應公司端進度（2026-11-17）

- [x] 已直接閱讀 `STD/MESWin/QC/QC_ItemRule/frmQCItemRule.vb`、`frmQCItemRuleDef.vb`、wsQC 與 `clsQCBasis`：確認 QC Item lookup 預設已核准、SPCID 依 ChartType、候選規則只載已核准、四個 Action 分類、SPK 停用 Wait、Confirm/Apply 完整替換整組映射。
- [x] Oracle `TBLQCITEMRULE` DDL/PK 沿用 QC205 之前由 PowerShell 5.1 + Oracle.ManagedDataAccess 直接查得的 `mestest_qc205_variable_dependencies_source_ddl.sql`；`TBLQCRULEBASIS` 沿用 QC108 source DDL。已找到，不需重新查 Oracle，也未使用 DBeaver UI。
- [x] 建立 `/YMES/QC/VariableItemRuleMap` Vue 3 DEV 頁、QC209 route/三層 fallback menu、獨立 mock 資料、QC Item 查詢、四分類多選移動、ChartType/SPCID 與 Apply/Confirm。
- [x] 新增 `mestest_qc209_variable_item_rule_map_create.sql`、QC209 功能規格、模組索引、Oracle 查詢紀錄與 `todolist_homepc.md` API/schema handoff。
- [ ] Production API/schema 待家用端完成；本頁未執行 build、測試或瀏覽器驗收。家用端完成後公司端 pull 最新 main，以 QC209 Production API 驗收。
- [x] QC210「刪除計量型品管項目量測記錄」公司端 Vue DEV 頁、WinForms 規格、Oracle DDL 來源引用與 PostgreSQL Create/API handoff 已完成，詳本檔末段。
- [ ] QC 頁面 Production API/schema 待家用端完成。公司端未執行 build、測試或瀏覽器驗收；家用端完成後 pull 最新 main 驗收，再按 MES_MENU_FUNCTION_INVENTORY.md 繼續尚未遷移功能。

## QC210 刪除計量型品管項目量測記錄公司端進度（2026-11-17）

- [x] 已直接讀取 QC_ItemDeleteForVariables Form/Designer、wsQC Load/Delete Service、clsQCBasis Load/Delete Core；確認 IssueState -1、0 清單、Visible Grid columns、Log=Log 分支和逐筆多選操作。
- [x] 已核對保存的 MESTEST Oracle DDL：計量量測 log 來源沿用 QC205/201 的直接唯讀 DDL reference；本次未重連 Oracle，也未查看業務列或使用 DBeaver。
- [x] 已新增 /YMES/QC/VariableMeasurementDelete Vue 3 DEV 頁，含 QC210 三層選單、-1/0 清單合併、WinForms 欄位與 ValueList、點列/多選、確認刪除、逐筆結果及獨立 DEV mock。表頭與選取色使用黃橘色系。
- [x] 已新增 QC210 功能規格及 PostgreSQL Create SQL 參考，並更新 MODULE_REPLICATION_SPEC_INDEX.md、docs/DATABASE_CONNECTION.md 與 todolist_homepc.md。
- [ ] 家用端尚未完成 QC205/209/210 Production API/schema；公司端未做 build、測試或瀏覽器驗收。

## QC212 計量品管項目群組公司端交接（2026-11-17）

- [x] pull `origin/main`（Already up to date），依 Customer/Production THEIL BU1 原始碼閱讀群組主 Form、Define Form、Memo Form/Designer、共用 Include/Parameter/OP/Calculate Script 子視窗，以及 `wsQC`、`clsQCBasis`；依據不是截圖。
- [x] 完成 `/YMES/QC/VariableItemGroup` Vue 3 DEV 畫面與三層選單，包含四狀態/GroupNo 查詢、上下主/明細 Grid、Add/Edit/Delete/Copy/Approve、雙清單 Add QC/順序、Remove QC、Parameter、Op、Calculate Script、Memo；使用獨立 localStorage 假資料及 KD 黃橘配色。
- [x] 主/明細/核准/刪除/Copy/Memo 的實際限制與 legacy 差異已寫入 `docs/modules/QC/計量品管項目群組.md`。已整理 API handoff，並記錄 QCITEM_G Issue Job、WIP 刪除檢查、DataStamp 與 Memo Type=1。
- [x] Oracle DDL 沿用既有 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀 metadata 擷取：主表、明細、計算腳本、Memo 及共用表都已有 Oracle source reference；本次未再次連 Oracle、未讀業務資料、未在 Oracle 執行 DDL。
- [x] 新增 `database/reference/mestest_qc212_item_group_create.sql`，供家用端核對並補缺的 `tblqcitemgroupdetail`、`tblqccalculatescript` PostgreSQL 建表參考；主表與 Memo 既有 Create reference 已在規格中引用。
- [x] 更新 `todolist_homepc.md` 最新 QC212 schema/API handoff、`docs/DATABASE_CONNECTION.md`、`docs/MODULE_REPLICATION_SPEC_INDEX.md`。
- [ ] 公司端未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收；家用端完成 migration/API 後公司端 pull 再驗收。下一項依目錄為 QC213 計數品管項目群組。

## QC213 計數品管項目群組公司端交接（2026-11-17）

- [x] 依 STD WinForms frmQCItemAttOrderGroup/Define、共用 QC_ItemGroupInclude（IsAttributes=True）、QC_ItemOp（QCType=3）、wsQC 與 clsQCBasis 核對欄位、按鈕、狀態、刪除/簽核規則；沒有用截圖代替原始碼。
- [x] 新增 /YMES/QC/AttributeItemGroup Vue 3 DEV 頁並掛到品質資料 > 品管設定 > 計數品管項目_群組。包含四狀態及 GroupNo 查詢、上下 Grid、Add/Edit/Delete/Copy/Approve、雙清單 Add QC、移除/排序及共用 OP 對應欄位；使用獨立 localStorage 假資料與 KD 黃橘色系。
- [x] 依 PowerShell 5.1 + Oracle.ManagedDataAccess 透過本機受保護連線唯讀取得 QC213 主/明細及硬刪 OP/PRD 依賴 DDL/欄位/PK/index；WIP 檢查表沿用既有來源 DDL。沒有查詢 MES 業務列、沒有在 Oracle 執行 DDL，連線資料未輸出。
- [x] 新增 Oracle source DDL reference、含 WIP/OP/PRD 關聯表的 PostgreSQL schema-only CREATE 參考、QC213 功能規格、DATABASE_CONNECTION.md 與 MODULE_REPLICATION_SPEC_INDEX.md 更新。
- [x] 更新 todolist_homepc.md：先完成既有 QC212 API/schema，再依 QC213 handoff 比對所有主/明細/硬刪依賴及 YMES live schema，僅補缺 migration，實作 QC213 API/Issue Job/共用 QCType=3 operation。
- [ ] 公司端未連 YMES PostgreSQL，也未執行 build、測試或瀏覽器驗收。家用端完成 migration/API 後 pull 再做整合驗收；之後依 MES_MENU_FUNCTION_INVENTORY.md 接下一個未遷移功能。

## 2026-11-18 QC409 OCAP異常單處置調整公司端進度

- [x] 已直接閱讀 STD/MESWin/QC/QC_OCAPDispositionAdjust/frmOCAPDispositionAdjust.vb、Designer、frmOCAPProcessDraw.vb、wsQC.LoadERFJoinOCAPDisposition、wsWIP.Exe_ERF_Disposition 與 clsLot.UpdOCAPAndERF_Disposition/OCAP helper。已記錄兩種動作、查詢條件、單筆作用列、同 ERF 多 Hold Item 的最終結案差異、LotStamp、WIP/OCAP side effects。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 使用既有受保護 MESTEST 設定，唯讀取得三張 OCAP 專用表 DDL 與 11 張共用 ERF/WIP/通知相依表 DDL/索引；未讀業務資料、未在 Oracle 執行 DDL，未輸出或提交連線秘密。
- [x] 已建立 /YMES/QC/OCAPDispositionAdjust Vue DEV 頁、QC409 menu/route/元件掛載、功能規格、Oracle DDL reference、三張專用表 PG CREATE-only handoff，更新 module index、DATABASE_CONNECTION.md 及家用端 API/schema TODO。
- [ ] Production API/schema 尚未實作；公司端未連 YMES PostgreSQL，未執行 build、測試或瀏覽器驗收。家用端完成 QC409 API/schema 後 pull，對照真 API/錯誤與交易行為整合驗收。
- [ ] 接續依 docs/MES_MENU_FUNCTION_INVENTORY.md 遷移下一個尚未完成的 Function；各功能先追 WinForms Form/Service/Core，再查缺的 Oracle DDL，完成 DEV UI/規格/API handoff 並 push。

## 2026-11-18 公司端進度：OP103 作業站類別資料

- [x] 直接對照 frmOPType/Designer、frmOPTypeDef/Designer、wsOP.Load/Add/Edit/Delete/ApproveOPType 與 clsOpBasis；記錄四狀態查詢、Grid 隱藏狀態/DataStamp、定義窗欄位、OP namespace 衝突、Property cascade、IssueNo 與狀態轉移。
- [x] 使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護連線唯讀取得 MESTEST.TBLOPTYPE 原始 DDL/index；沒有讀業務資料、沒有在 Oracle 執行 DDL、未輸出連線秘密。完成 Oracle source DDL 和 PostgreSQL CREATE-only 參考。
- [x] 新增 /YMES/MasterData/OperationTypes Vue DEV mock UI 與 OP → 作業站設定 → 作業站類別資料路由、四狀態/查詢/清單/新增/修改/複製/刪除/核准/Property，KD 黃色欄位與 WinForms 版型。
- [x] 新增 OP103 規格、模組索引、資料庫連線 provenance、家用端 live schema/API handoff，並在 OP Basis 規格互連。
- [ ] Production API/schema 由家用端按 todolist_homepc.md 實作；未連 YMES PostgreSQL，未執行 build、test 或 browser 驗收。公司端持續依 docs/MES_MENU_FUNCTION_INVENTORY.md 下一個未遷移 WinForms 功能直接閱讀程式碼。

## OP101 作業站法則設定（公司端 2026-11-18）

- [x] 直接閱讀 `frmOPControlRule`/Def、`wsOP.Load/Add/Edit/DeleteOPControlRule`、`clsOpBasis` 與 `clsBasis_UPD.EditOPControlRule`；確認查詢 RuleNo、只顯示 RuleClass=0、無 IssueState/Approve、實體刪除及 DataStamp CAS。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直連受保護設定，只讀取得 `MESTEST.TBLOPCONTROLRULE` Oracle DDL、constraint/index metadata；僅 RuleNo PK unique index，無 FK/額外 index。存 Oracle source DDL 與 PostgreSQL CREATE-only 對照。
- [x] 新增 `/YMES/OP/ControlRules` Vue DEV 頁和 OP 三層選單。清單欄位、Add/Edit/Copy/Delete、定義欄位、Send Mail 群組 lookup、DataStamp/audit、legacy Apply payload 差異均已整理。
- [x] 新增 `docs/modules/OP/作業站法則設定.md`、模組索引、Oracle 連線查詢紀錄，以及本段家用端 API/schema handoff。
- [ ] 本段依工作規則未執行 build、test 或瀏覽器驗收；推送後下一項接續 OP102「企業邏輯設定」，先從 WinForms Form/Service/Core 原始碼開始。

## OP102 企業邏輯設定（公司端 2026-11-18）

- [x] 直接閱讀 frmBusinessRuleBasis/Def、wsOP、clsOpBasis、clsBasis_UPD；記錄三種 RuleMethod、plugin/interface/scenario options、四狀態 CRUD/簽核/刪除、DataStamp 與 Apply/Confirm。
- [x] Windows PowerShell 5.1 + Oracle.ManagedDataAccess 受保護連線唯讀取得 MESTEST.TBLOPBUSINESSRULEBASIS 與 TBLOPSCENARIOBASIS DDL/index/constraint metadata；各表只有 PK index、無 FK/額外索引；未讀業務列、未執行 Oracle DDL。
- [x] 建立 /YMES/OP/BusinessRules Vue DEV 假資料頁與 OP 設定三層選單、路由、狀態/查詢/多選/CRUD/核准、欄位條件、稽核展示。
- [x] 建立 OP102 詳細規格、兩張 Oracle source DDL 與 PostgreSQL CREATE-only 參考、索引與資料庫連線 provenance、家用端 API/schema handoff；補上作業站主檔規格交互連結。
- [ ] 按工作規則未執行 build、test 或 browser 驗收；Production API/schema 待家用端依 todolist_homepc.md 完成。家用端 push 後公司端 pull 對接。
- [ ] 接續依 docs/MES_MENU_FUNCTION_INVENTORY.md，直接閱讀下一項尚未遷移 WinForms Form/Service/Core，查缺漏 Oracle DDL，完成 UI/spec/handoff 並 push。

## 2026-11-18 公司端進度：OP105 作業站群組資料

- [x] 直接讀取 frmOPGroupBasis / frmOPGroupBasisDef / frmOPGroupDetailDef、wsOP、clsOpBasis、clsBasis_UPD；整理主檔/明細欄位、雙 Grid、ADD/EDIT/DEL/Copy/Check、DataStamp、已核准作業站與區段 lookup。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 MESTEST.TBLOPGROUPBASIS、TBLOPGROUPDETAIL DDL 與 ALL_TABLES/ALL_INDEXES/ALL_CONSTRAINTS；兩表僅 PK unique index，無 FK/額外 index。未查業務列、未在 Oracle 執行 DDL/DML。
- [x] 新增 /YMES/OP/Groups Vue DEV 假資料雙 Grid/表單/lookup/檢核並接上作業站資料→作業站設定→作業站群組資料選單。
- [x] 新增 OP105 規格、Oracle source DDL、PG CREATE-only SQL、校正 first-phase 新庫 DEFAULT 0，更新 module index、資料庫 provenance 與 todolist_homepc.md。
- [ ] 補 `frmOPGroupBasisDef` → `LoadPSDefine` 的真實生產區段狀態篩選／缺值規則；家用端對新建／變更 PS 採已核准 state2 的明載 YMES 保守規則並持有引用鎖，未變既有 raw／失效 PS 保留，`*` 沿用來源全區段語意。本條只要程式／系統 metadata 證據，不需重交已有 DDL、連 PostgreSQL 或 SSH，也不阻斷家用端現有 writer。
- [ ] 尚未連 YMES PostgreSQL、未實作 API/schema；未執行 build、test 或 browser 驗收。已列明 API 路徑與 live schema/defaults 核對項目給家用端。
- [ ] 推送 OP105 後依 docs/MES_MENU_FUNCTION_INVENTORY.md 接續下一個尚未完成 WinForms 功能；家用端 API/schema push 後 pull 並整合。

# 2026-11-18 辦公室端續作：產品主檔 PD102

公司端依 frmProductBasis_THEIL_BU1 建立產品專用 Vue DEV 頁並接入 product-master 路由；整理主表單、IssueState、Add/Edit/Copy/Version/Approve/Delete、Process/BOM/EQPConstraint、WinForms 服務邏輯與七張 Oracle 主/關聯表 DDL/字典。規格為 docs/modules/PRD/產品主檔.md，Oracle/PG 參照為 database/reference/mestest_prd_product_basis_*。Oracle ProductProperty 的 DBMS_METADATA 發生 ORA-03135，欄位/約束依字典重建並已標註。家用端 API/schema 未完成。

下一個公司端工作：
1. PD_ProductProcess、frmProductBOM_THEIL_BU1 的流程與 BOM DEV 子表單已完成；產品屬性 DEV 子表單也已完成，分別見 `產品主檔子表單.md`、`產品主檔產品屬性.md`。
2. Business Rule、OPDescription、RecipeGroup、Accessory DEV 已完成並有家用端 API/schema handoff；接續 Resource、SubOP、Program、Default、RunTime、QC Item、Packing Set、EQPCapItem、eSOP、EquipmentSpec、APS、Bin、GBOM Import/Export、Product Import。每項直接閱讀 WinForms Form/Designer/Core/Service，記錄欄位、狀態規則、資料表和服務；DDL 缺漏時用受保護 PowerShell Oracle 連線查字典，不可猜。
3. Product API/關聯 schema 由家用端接手後，回來核對 API contract 與 UI，避免公司端重複改 API。
4. 依 MES 全功能目錄繼續下一個待遷 Function；每一段維護規格索引/TODO，假資料限定 DEV。

頁面底部 Set ... 與 Expand All 帶入選取流程列上下文；BOM Set 開啟該產品 BOM 遷移入口。WinForms Designer 設 Visible=False 的 QTY Conversion 與 DisApprove 沒顯示。window.print() 只是產品包裝 BOM 報表的 DEV 簡化。正式模式目前能載現有產品/流程/BOM，其餘欄位與子功能仍待家用端補 API 契約。

## PRD PD102 產品流程／BOM 子表單續作（2026-11-18）

- [x] 直接對照 `STD/MESWin/PRD/PD_ProductProcess/frmProductProcess.vb`：544×462 視窗、MOType/PS/Process 查找、Current/Specified、Default 唯一、暫存 Add/Del、Apply/Confirm/Close 與 Net Flow 檢查。
- [x] 直接對照客製 `Customer/Production/MESWin/PRD/PD_ProductBasis_THEIL_BU1/frmProductBOM_THEIL_BU1.vb`：719×516 Tree/Node Define、右鍵節點權限、產品/材料版本、投入點、數量、損耗率、Specified、LOTCREATE、材料群組/替代料與遞迴樹。
- [x] 新增 `ProductProcessEditor.vue`、`ProductBomEditor.vue` 並接入 `ProductBasisMaster.vue`。DEV 假資料的流程與 BOM 可新增/刪除/修改/檢視並保存至產品專用 localStorage；正式 API 模式唯讀。LOOKUP 只用 DEV 假資料，不能當正式選項。
- [x] 新增 `docs/modules/PRD/產品主檔子表單.md`，更新家用端 API/schema 待辦。
- [ ] 產品主檔其他側邊子功能仍待閱讀各自 WinForms Form/Core/Service 後實作；不可用本次子表單完成狀態代表 PD102 所有側功能已完成。
- [ ] 未執行 build/test/browser 驗收。完成同步 YMES 後 commit/push，再依 `docs/MES_MENU_FUNCTION_INVENTORY.md` 排定下一個未完成 FunctionNo。

## PRD PD102 產品屬性子表單續作（2026-11-18）

- [x] 直接閱讀 `frmProductProperty`/Designer、`frmProductPropertyDef`/Designer、`wsPRD` 及 `clsProduct`：四狀態、清單欄位與隱藏 DataStamp、ADD/EDIT/DEL/Copy/Approve、PropertyType=5 已核准 SYS Element 查找、MES Parameter 值選取、Apply/Confirm/Close、`PRD PROPERTY` 簽核 Job 分流。
- [x] 新增 `ProductPropertyMaster.vue`：684×486 清單、496×276 定義窗、MES/KD 黃色輸入、狀態篩選、多選/逐筆刪除、ACTIVE 標 UNUSED、Apply/Confirm、Property 與 Parameter DEV lookup、DEV localStorage。
- [x] 正式 API 模式唯讀；DEV 假核准採未配置簽核 Job 的直接 ACTIVE 情境。Controlled 僅為 SYS Element Basis 資訊，產品表不存 Controlled。
- [x] 新增 `docs/modules/PRD/產品主檔產品屬性.md`，更新 PRD 主規格/索引、`todolist_homepc.md` API/schema handoff。
- [ ] 未執行 build/test/browser；按工作規則僅做靜態檢閱。現有 TBLPRDPRODUCTPROPERTY Oracle DDL 由字典重建且已存在，家用端先查 YMES schema/migration，再決定 API mapping/migration。
- [x] PD102 `Parameter` 已依 WinForms Form/Designer、wsPRD、clsProduct 完成 DEV 子視窗及 Oracle 字典/SQL handoff，詳見本檔新增段落。

## PRD PD102 產品 Parameter Setup（公司端 2026-11-18）

- [x] 直接閱讀 `PD_ParameterSetup/frmParameterSetup.vb`、Designer、`wsPRD`、`clsProduct`。依實際右鍵旗標復刻 Product Process OP/All PS OP、PS/OP/Attrib/Rule/Equipment/Recipe 階層，Condition OP，右側 OP/Attrib/Rule/Equipment/Condition OP tabs；QCItem Tab 在 WinForms Load 時移除。
- [x] 新增 `web/src/components/ProductParameterSetup.vue` 並接到 PD102 Parameter；DEV 支援 OP 多選、Condition OP、Lot/Component Attrib、一般 Rule、設備多選、RecipeGroup 及唯讀 Recipe Detail；DEV localStorage；正式 API 模式唯讀。
- [x] 以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 對 MESTEST 直接查 `ALL_TAB_COLUMNS`、`ALL_CONSTRAINTS`、`ALL_CONS_COLUMNS`、`ALL_INDEXES`、`ALL_IND_COLUMNS`、`ALL_TRIGGERS`。8 張表結構 metadata 已取得；沒有 FK/Trigger。`DBMS_METADATA.GET_DDL` 回 SocketException，故 Oracle source DDL 依 Oracle 字典欄位/約束/索引邏輯重建，不含實體 tablespace/storage。
- [x] 新增 Oracle source DDL 與 PostgreSQL CREATE-only reference、`docs/modules/PRD/產品參數設定.md`，更新 PRD 主規格、索引、家用端 handoff 及 DB query provenance。
- [ ] 未執行 build/test/browser；未連 YMES PostgreSQL，未部署 SQL，正式 API 模式仍唯讀。
- [x] PD102 Business Rule 已依真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、wsPRD/Core/Service 完成 WinForms 規格、DEV UI 與 homepc API/schema handoff；注意真正入口不是名稱相近的 `frmPRDOPRule_THEIL_BU1`。
- [x] PD102 OPDescription DEV/Oracle DDL 已由 commit `dde7973` checkpoint，完成靜態 code review；此段未執行 build/test/browser，Production API/schema 由家用端依 handoff 完成。


## PRD PD102 Business Rule DEV UI（2026-11-18）

- [x] 核對真實入口 `btnBusinessRule_Click`、`frmProductOPBusinessRule`/Draw、WinForms Designer、`wsPRD`、`clsProduct`、Service graph validation；排除名稱相近但不同的 `PD_PRDOPRule_THEIL_BU1`。
- [x] 完成產品 OP 清單/圖形預覽及 Default/Close 版面、Add/Edit/多選 Del、圖形節點/連線/Phase 編輯、NetFlow readonly 的 DEV UI，接至 ProductBasisMaster 並只寫專用 localStorage。
- [x] 新增 `docs/modules/PRD/產品商業規則.md`；重用 Parameter 階段已查到的三張 Oracle 表與既有 DDL/PG CREATE 參照，沒有重複 Oracle query、沒有新增表或 CREATE SQL。
- [ ] Production API、OP/Phase/Business Rule/Template lookup、Lassalle XML serializer 尚待家用端完成；Default、樣板及 catalog 現在仍為 DEV 假資料。
- [x] OPDescription DEV UI/DDL/規格 checkpoint 已提交為 `dde7973`；靜態檢閱已完成，未執行 build/test/browser。Production API/schema handoff 見本檔末尾。

## PRD PD102 OPDescription 公司端檢查點（2026-11-18）

- [x] 依 PD102 真實 `btnDescription_Click` 入口，閱讀 `frmProductOPDescription`/Def/Designer、`wsPRD.Load/Add/Edit/DelPRDOPDescription`、`clsProduct`/`clsBasis_UPD`，確認 Add/Apply/Confirm/多選 Delete、PS lookup、IssueState、DataStamp 與產品 Default/Copy 的邊界。
- [x] 以 Windows PowerShell 5.1 + 受保護 Oracle 設定直連 MESTEST，唯讀取得 `TBLPRDOPDESCRIPTION` DBMS_METADATA DDL、欄位、PK/index、FK/trigger metadata；無業務資料列，未執行 Oracle DDL/DML。
- [x] 直接讀取 WinForms、wsPRD、Core，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 取得 `TBLPRDOPDESCRIPTION` DDL/字典；只讀 metadata，未讀業務列或執行 Oracle DDL/DML。
- [x] 已在公司端工作區建立 `ProductOpDescriptionSetup.vue` 並接至 PD102；DEV 支援清單、流程/OP 假查找、Add/Edit/多選 Delete、audit/DataStamp 與狀態唯讀，只寫 localStorage。Oracle source DDL、PostgreSQL CREATE-only 與規格文件已建立。
- [x] 已靜態複核元件、WinForms 操作流程與資料欄位；修正輸入欄位字型宣告。未執行 build/test/browser，未連 YMES PostgreSQL；Production API/schema/lookup 尚待家用端。
- [x] OPDescription 已完成靜態複核並建立 homepc handoff；RecipeGroup WinForms/Service/Core/Oracle metadata、DEV UI 與 handoff 已於本次段落完成。

## PRD PD102 RecipeGroup 指派公司端檢查點（2026-11-18）

- [x] 依 `btnRecipeGroup_Click` 追至 `PD_ProductRecipe`、`frmProductRecipe`/Def、`wsPRD.Load/Add/Edit/DelPRDRecipe`、`clsProduct`；確認 ProductNo/Version 關聯、hidden Edit/Find、Apply/Confirm/Delete、IssueState 未被子視窗使用及刪除省略 RecipeGroup 會刪整個產品版本的邊界。
- [x] Windows PowerShell 5.1 + 受保護 Oracle profile 直連 MESTEST，只讀取得 `TBLPRDRECIPE`、`TBLPRDRECIPEBASIS` DDL/字典；無業務資料列，未執行 Oracle DDL/DML。
- [x] 新增 `ProductRecipeGroupSetup.vue` 並接到 PD102，DEV 依產品版本保存關聯 localStorage，含多選刪除、已核准目前版查找、Add/Apply/Confirm/Close；Production API 未接線時唯讀。
- [x] 新增 `docs/modules/PRD/產品RecipeGroup.md`、Oracle source DDL、PostgreSQL CREATE-only 參照，更新主檔規格、DB connection provenance、兩個規格索引及 homepc API/schema handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。RecipeGroup DEV/DDL handoff 已完成；Accessory checkpoint 接續如下。

## PRD PD102 Accessory 公司端檢查點（2026-11-18）

- [x] 閱讀 THEIL BU1 PD102 `btnAccessory_Click`、`frmPRDAccessory_THEIL_BU1` Form/Designer、`frmPRDAccessoryDef_THEIL_BU1`，核對主/明細雙 Grid、ADD/EDIT/DEL、產品流程 OP 查找、配件類別與配件編號查找、All `*` / Specified、Apply/Confirm 和多選逐筆 Delete。
- [x] 追出 WinForms client 呼叫 `Load/Upd/DelPRDAccessory`、`LoadAccessoryType/Basis`、`LoadProductProcess(PSPrivilege=True)`、`LoadNodeBasis(NodeType=0)`、`LoadOPBasis` 及 `PD_ExpandProcess`。wsPRD/clsProduct server/Core 實作未包含於目前 client source，規格明列待家用端核對的替換/空清單/交易行為。
- [x] Windows PowerShell 5.1 + 受保護 Oracle profile 唯讀直連 MESTEST，取得 `TBLPRDACCESSORY` DDL、欄位/constraint/index/trigger 字典；未讀業務資料、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 `ProductAccessorySetup.vue` 接至 PD102；DEV 假資料含兩個作業站、兩種配件類別、指定配件與 `*` 全部模式；只寫產品關聯 localStorage，Production API 未接線時唯讀。
- [x] 新增 `docs/modules/PRD/產品配件設定.md`、Oracle source DDL、PostgreSQL CREATE-only，更新主檔規格、DB metadata provenance、兩個索引與 homepc API/schema handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。下一個 PD102 側邊功能為 `Resource`。

## PRD PD102 Resource 公司端檢查點（2026-11-18）

- [x] 直接對照 PD102 btnOPArea_Click (Handles btnResource.Click)、frmProductOPArea/Def Form 與 Designer；記錄兩視窗尺寸、PSPrivilege 查找、OP/Area 雙 Grid、Add/Edit/多選 Delete、Confirm 僅能一個 Default Area、全量更新呼叫及錯誤停止行為。
- [x] 重用既有 TBLPRDOPAREA、TBLOPAREA、TBLSMDAREABASIS Oracle DDL / PostgreSQL CREATE 參照；未重查 Oracle、未新增重複 CREATE SQL。
- [x] 完成 ProductResourceSetup.vue，接至 PD102 Resource；DEV 有產品流程/OP/Area 假查找、Area 左右多選移動、雙擊移動、Enter 精確搜尋、Default Area 驗證及 OP 多選刪除；假資料存 ymes.dev.prd-product-basis-relations.v1 的 opAreas。
- [x] 新增 docs/modules/PRD/產品作業區域設定.md，更新 PRD 主規格、規格索引、DB metadata provenance 及家用端 handoff。
- [ ] wsPRD/wsOP Core/Service 的 transaction、空 multivalue、Delete 與 Copy/Version 邊界待家用端查核；Production API 尚未接線。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL或部署 SQL。接續 PD102 SubOP，再依側邊功能順序遷移。

## PRD PD102 SubOP 公司端檢查點（2026-11-18）

- [x] 讀取 PD102 `btnSubOP_Click`、`frmPRDSubOP` Form/Designer、`wsPRD`、`clsProduct` 與 `clsBasis_UPD`；核對視窗尺寸、IssueState 行為、OP PS lookup、ADD/INSERT/EDIT/多選 DEL、DataStamp、局部成功及排序。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess，從本機受保護 MESTEST profile 唯讀取得 `TBLPRDOPSUBOP` DDL 與欄位/constraint/index metadata；未讀業務資料列、未執行 Oracle DDL/DML、未輸出連線秘密。
- [x] 新增 `ProductSubOpSetup.vue` 並接至 PD102；DEV 假資料支援 OP lookup、新增/插入/修改/多選刪除與序號重排，寫入產品關聯 localStorage；正式模式唯讀。
- [x] 建立 `docs/modules/PRD/產品子作業設定.md`、Oracle source DDL、PostgreSQL CREATE-only 參照，並更新產品主規格、模組索引及 DB metadata provenance。
- [ ] 家用端仍須核對 live schema/migration 並實作 Production API/schema；詳見 `todolist_homepc.md` 的 SubOP handoff。
- [ ] 未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，未部署 CREATE SQL。下一個 PD102 側邊功能依 WinForms 順序為 Program。

## PRD PD102 Program 公司端檢查點（2026-11-18）

- [x] 閱讀 PD102 `btnProgram_Click`、`frmProductProgram` Form/Designer、`wsPRD.LoadProductProgramsDefine/UpdProductProgram` 及 `tcPRD_IC.clsProgram`；確認 checkbox Grid、產品 IssueState=0 才能 Confirm、全量替換、空選取清除指派。
- [x] 使用 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 從受保護 MESTEST profile 唯讀取得 `TBLPRDPROGRAMBASIS`、`TBLPRDPRODUCTPROGRAMS` DDL/字典；無業務列、未執行 Oracle DDL/DML、連線秘密未輸出。
- [x] 新增 `ProductProgramSetup.vue` 並接至 PD102；DEV 可勾選程式並保存至關聯 localStorage，非未核准狀態保留可勾選但 Confirm disabled 的舊行為；Production 模式唯讀。
- [x] 建立 Program Oracle source DDL、PostgreSQL CREATE-only 參照、功能規格及 Home handoff，更新主檔規格、模組索引、資料庫 metadata provenance。
- [ ] 家用端須核對 `tblprdprogrambasis` first-phase bootstrap/live schema，僅在關聯表缺漏時新增 migration/API；詳見 `todolist_homepc.md`。
- [ ] 未執行 build、test 或 browser 驗收，未連 YMES PostgreSQL，未部署 SQL。Default 檢查點接續見下節；完成交接後依 PD102 側功能順序做 RunTime。

## PRD PD102 Default 公司端交接檢查點（2026-11-18）

- [x] 依 PD102 `btnDefault_Click`、`frmProductLoadDefault`/Designer、`wsPRD.UpdPRDOP_BatchDefault`、`clsProduct.UpdPRDOP_BatchDefault` 整理入口、兩種 OP 範圍、七類資料複製、IssueState、確認與錯誤關閉行為。
- [x] 新增 `ProductDefaultSetup.vue` 接至 PD102 Default，DEV 假樣板只寫 localStorage；正式 API 未接線時唯讀。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 直連 MESTEST 查 APS 產品表資料字典：DBMS_METADATA.GET_DDL 回 ORA-03135，改查 ALL_TAB_COLUMNS/CONSTRAINTS/CONS_COLUMNS/INDEXES 重建邏輯 DDL；沒有讀業務資料列或執行 DDL/DML。
- [x] 新增 `docs/modules/PRD/產品預設設定.md`、APS Oracle source DDL、PostgreSQL CREATE-only 參照，更新產品主規格、規格索引、DB metadata provenance 及家用端 API handoff。
- [ ] 未執行 build/test/browser，未連 YMES PostgreSQL，未部署 SQL。桌電 pull 此檢查點後先看 `git status` 和最新 commit，靜態檢閱 Default 變更，再做 PD102 `RunTime`。Vue UI 在 `web/`、API 在 `src/YMes.Api/`、共用 DTO 在 `src/YMes.Contracts/`。
- [ ] 遇到 DBMS_METADATA ORA-03135 時，使用 Windows PowerShell 5.1 + ODP.NET 查 ALL_* dictionary；不要用 DBeaver GUI 抄 DDL。連線秘密不進 Git；桌電若沒有受保護 profile，要向使用者安全索取，不要猜連線資訊。

## 公司桌機與筆電共同分工

- 公司桌機和公司筆電都是公司端：用 DEV 假資料完成全部 Vue UI/互動邏輯，核對 WinForms 原始碼，唯讀查 Oracle MESTEST metadata，並提交模組規格及家用端 API/SQL handoff。
- 公司端不連 YMES PostgreSQL、不設定 `ConnectionStrings__YMes`、不建立 PostgreSQL schema/migration、不實作或驗收家用端 API，也不 SSH/部署。這些工作只由家用電腦依 `todolist_homepc.md` 接手。
- 桌機只需為 Vue DEV 配置 Node.js 22.x（22.12+）與 npm 並安裝 `web/` dependencies；.NET 9 SDK/API/PostgreSQL 連線不屬於公司端前置設定。
- Oracle MESTEST 只可使用本機受保護設定唯讀查詢 DDL/metadata；如果桌機沒有該設定，安全重新設定後再查，不在 Git 或聊天輸出帳密。

## PRD PD102 RunTime 公司端檢查點（2026-11-18）

- [x] 核對 PD102 `btnRunTime_Click`、`frmProductRunTime` 和 `frmProductRunTimeDef`（含 Designer）、`wsPRD` 及 `clsProduct`。確認預設／已保存合併、六個欄位、無設備時的 `N/A` 列及全量儲存。
- [x] 以桌機受保護 Oracle profile 經 Windows PowerShell 5.1 + ODP.NET 驗證 MESTEST 登入，唯讀核對 `TBLPRDRUNTIMESETUP` 及六張 lookup 表的資料字典。`GET_DDL` 回 `ORA-03135`，已依字典重建 RunTime DDL。查詢限 metadata。
- [x] 完成產品主檔 RunTime 的 DEV 表格與單列編輯、Apply、Confirm、未儲存關閉提示，使用產品版次 localStorage。補齊模組規格、Oracle DDL／PostgreSQL CREATE-only 參照及家用端 API／schema handoff。
- [ ] 家用端接續正式 API、schema 及瀏覽器驗收。

## PRD PD102 QC Item 公司端檢查點（2026-11-18）

- [x] 依 `btnQCItem_Click`、`frmProductQCItem`／Designer、`wsOP`、`wsPRD`、`clsOpBasis`、`clsProduct` 與 `clsQCBasis`，完成四類 DEV 品管項目、InLine／OffLine、多選移除、檢驗旗標、Apply／Confirm 及 localStorage。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 唯讀核對 MESTEST 相關資料字典。以 13 欄版本重建 `TBLPRDQCITEM` 邏輯 DDL，包含舊參照缺少的 `XX_FREQ`。
- [ ] QC Import／QC Chart 目前提供選檔視窗，完整 Excel 解析、檢查、選取及儲存待接續。正式 API、schema 和瀏覽器驗收由家用端執行。

## PRD PD102 Packing Set 公司端檢查點（2026-11-18）

- [x] 核對 `btnPackingSet_Click`、`PackOperationMode`、傳統及多階 Form／Designer、`wsPRD`／`wsWIP`、`clsProduct`／`clsBasis_UPD`。完成 DEV 三階數量與配件、Copy、多階規則對應和使用中阻擋示例。
- [x] 以 Windows PowerShell 5.1 + ODP.NET 唯讀核對七張 Oracle 表，收錄邏輯 DDL／CREATE-only 及家用端交接。序號主檔來源為獨立唯一索引。
- [x] 完成傳統 Packing Set 的 Serial Rule、Label Rule、Multi View 與 DEV 欄位預覽。序號／標籤主明細寫入 DEV localStorage；正式標籤引擎及瀏覽器驗收待接續。

## PRD PD102 EQPCapItem 公司端檢查點（2026-11-18）

- [x] 核對 `btnEQPCapItem_Click`、`frmPRDOPCapItem`、Def／Designer、`wsPRD`／`wsEMS`／`wsOP` 及 Core。完成 DEV 清單、選擇 OP／CapItem、Add／Edit／Copy／Delete、Apply／Confirm 與 IssueState 限制。
- [x] 使用 PowerShell 5.1 + ODP.NET 唯讀查 `TBLPRDOPCAPITEM` 與 `TBLEMSEQPCAPBASIS`。依字典建立 Oracle／PostgreSQL CREATE-only 參照，已補家用端交接。
- [ ] 瀏覽器與正式 API 待家用端驗收。

## PRD PD102 eSOP 公司端檢查點（2026-11-18）

- [x] 核對主／Def Form、Designer、`wsPRD`、Core 與 eSOP EAI 呼叫。完成 DEV Position／Sub OP、文件選擇、Add／Edit／Copy、Delete 及 Apply／Confirm。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對 `TBLPRDESOPBASIS`，依資料字典重建 DDL 參照並交接家用端。
- [ ] 正式 eSOP、文件存取、API 與瀏覽器驗收待接續。

## PRD PD102 EquipmentSpec 公司端檢查點（2026-11-18）

- [x] 核對 EquipmentSpec 入口、Form／Designer、Service、Core，完成 DEV 流程／設備查找、規格明細、四欄順序／精度檢查和 Confirm。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對 `TBLPRDEQUIPMENTSPEC`，補 Oracle DDL、CREATE-only 和家用端 API/schema handoff。
- [ ] 正式流程展開、設備權限、API 與瀏覽器驗收待接續。

## PRD PD102 APS Integration 公司端檢查點（2026-11-18）

- [x] 核對 APS 主窗／EQPTIME 的 Form／Designer、Service、Core。完成 DEV Node、主／設備明細、Update／Delete、明細重建與數值驗證。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀核對四張 APS 表欄數與鍵，沿用 Default／PS104 的 DDL 參照並補 API 交接。
- [ ] 正式流程、群組展開、API 與瀏覽器驗收待接續。

## PRD PD102 Bin 公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service 和 Core。完成 DEV Bin 勾選、Good/Fail、說明、OS Bin、版本狀態和本機儲存。
- [x] 以 PowerShell 5.1 + ODP.NET 唯讀查兩張表，補 Oracle 邏輯 DDL 及 PostgreSQL CREATE-only 交接。`GET_DDL` 失敗碼已記錄。

## PRD PD102 GBOM 公司端檢查點（2026-11-19）

- [x] 核對 GBOM WinForms／Designer、Service、Core，完成 DEV 依版次匯入檔案備份、檔名與標頭檢查、原檔下載。
- [x] 用 PowerShell 5.1 + ODP.NET 確認兩張客製表及一個 INVALID view，補 Oracle 邏輯 DDL、CREATE-only 及 API 交接。
- [ ] 完整工作簿解析、複合資料異動與正式備份下載待接續。DEV XLSX 已檢查識別列與 `版次REV`。

## PRD PD102 產品 Import 公司端檢查點（2026-11-19）

- [x] 核對 `PD_ProductDataImport_PRS`、Designer、Service／Core，完成 DEV XLSX 兩組預覽、主檔逐列驗證與新增。
- [x] Oracle 唯讀核對產品主檔、類別、流程與節點鍵，補類別 DDL／CREATE-only 及匯入 API 交接。
- [ ] 舊式 XLS、流程／子表寫入與範本服務待接續。

## PRD PD102 Expand All 公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、一般與網狀 Service／Core。以 DEV 流程、區段和作業假資料完成唯讀結果表格。
- [x] Oracle 唯讀核對共用表，沿用產品、區段、流程與作業主檔的 CREATE-only 參考。
- [ ] 正式流程解析與背景作業 lookup 待接續。

## PRD PD101 產品類別資料公司端檢查點（2026-11-19）

- [x] 核對主畫面、定義 Form／Designer、Service／Core；完成 DEV 查詢、ADD／EDIT／Copy、刪除／停用／核准及三個子視窗。
- [x] 唯讀核對 Oracle 主表和共用子表，整理 API/DDL 交接並將 PD102 DEV 類別查找接到已核准類別。
- [ ] 正式完整副本與資料權限待接續。

## PRD PD103 製程模組資料公司端檢查點（2026-11-19）

- [x] 完成 DEV 主檔、版次、查詢、流程、共用子頁及 XLSX 主檔匯入；核對主 Form／Designer／Service／Core。
- [x] Oracle 唯讀確認 `TBLPRDMODULEBASIS`，補來源邏輯 DDL、CREATE-only 比對與家用端 API 交接。
- [ ] 正式權限、簽核、完整匯入與共用資料解析待接續。

## PRD PD104 產品內容查詢公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 三類查找、條件交集及唯讀結果。重用 PD102 假資料。
- [x] Oracle 唯讀確認四張既有表，沿用 PD102 DDL，記錄家用端即時 API 與分頁交接。
- [ ] 正式查詢、資料權限和完整主檔報表欄位待接入。

## PRD PD105 產品參數群組公司端檢查點（2026-11-19）

- [x] 完成群組版次、明細、複製、狀態與核准目錄，供 PD102／PD103 DEV 指派使用。
- [x] Oracle 唯讀核對群組與明細，補明細 Oracle DDL 和 CREATE-only。
- [ ] 正式簽核、完整權限與候選查詢待接續。

## PRD PD106 生產時間限制公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core；完成 DEV Max/Min、參照、起迄站與 BR 檢查、Hold 原因、CRUD 與狀態操作。
- [x] Oracle 唯讀確認主檔、歷程與 Hold 原因結構；補來源邏輯 DDL、CREATE-only 與 API 交接。
- [ ] 正式流程展開、簽核、歷程、設備檢核待接續。

## PRD PD107 標籤設定公司端檢查點（2026-11-19）

- [x] 完成 DEV 主檔與明細 CRUD、Copy、順序和欄位值 Test／Preview。核對 Form、Designer、Service 與 Core。
- [x] Oracle 唯讀核對主／明細，補明細邏輯 DDL、CREATE-only 及 API 交接。
- [ ] 正式標籤引擎、腳本產值、印表機和格式檔待接續。

## PRD PD108 產品標籤對應設定公司端檢查點（2026-11-19）

- [x] 核對主窗、編輯與批次窗、Service／Core，完成 DEV 對應 CRUD、Copy、來源查找及過帳批次預設。
- [x] Oracle 唯讀確認對應表；補來源 DDL、CREATE-only 及家用端 API 交接。
- [ ] 正式跨頁 PrintLabel 參數與權限待接續。

## PRD PD109 產品作業站預設公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core。完成 DEV 前綴查詢、作業範圍、批次 Default／Amend、逐筆進度與取消。
- [x] Oracle 唯讀核對共用表，沿用既有 DDL 參考。
- [ ] 正式候選、權限、批次 API、動態作業預設及 APS 待接續。

## PRD PD110 產品作業站修正公司端檢查點（2026-11-19）

- [x] 核對獨立 Form／Designer 與 PD109 共用 Service／Core。完成 DEV 路徑、固定六項 Amend、進度／取消與 handoff。
- [ ] 正式動態預設、權限與 API 待接續。

## PRD PD111 產品作業站批次複製公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 來源／目標版次、可選複製項目、目標參數序號、進度與取消。
- [x] 沿用 PD109 Oracle 字典及共用表 CREATE-only，整理家用端複製 API 交接。
- [ ] 正式權限、ECN 開關、APS 與 API 待接入。

## PRD PD112 包裝規則設定公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成 DEV 包裝版次、1–6 層定義、序號／標籤／配件及 PD102 候選串接。
- [x] Oracle 唯讀核對七張規則表，補來源邏輯 DDL、CREATE-only 與 API 交接。
- [ ] 正式用量檢查、站點規則及標籤引擎待接續。

## PRD PD113 產品包裝規則對應公司端檢查點（2026-11-19）

- [x] 核對 Form／Designer、Service／Core，完成獨立 DEV 路徑與 PD102 對應資料、PD112 候選共用。
- [x] Oracle 唯讀核對對應主鍵與 WIP／包裝狀態依賴，沿用既有對應 DDL。
- [ ] 正式使用中卡控與 API 待家用端接續。

## PRD PD114 自定義流程卡公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core 和 MESTEST 字典。
- [x] 完成主檔、欄位明細、複製、刪除和 DEV 批號欄位預覽，整理 [規格與交接](docs/modules/PRD/自定義流程卡.md)。
- [ ] 正式 WIP 產值及報表引擎待家用端接續。

## PRD PD115 產品流程卡對應公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core、MESTEST 欄位和主鍵。
- [x] 完成 CRUD、複製、核准產品與主流程卡候選，整理 [規格與交接](docs/modules/PRD/產品流程卡對應設定.md)。
- [ ] 正式候選及 API 待家用端接續。

## PRD PD116 產品品管項目規格匯入公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、客製 Service／Core、MESTEST 指派與 SPC 表。
- [x] 完成 DEV 51 欄 XLSX 範本、預覽驗證、New Plan／Override 本機套用，整理 [規格與交接](docs/modules/PRD/產品品管項目規格匯入.md)。
- [ ] 正式業務檢查與匯入待家用端接續。

## PRD PD117 GBOM 批次匯入公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、客製 Service／Core、MESTEST 流程、BOM 與客製表。
- [x] 完成 DEV 多檔選取、逐檔預檢、流程連線預覽及本機套用，整理 [規格與交接](docs/modules/PRD/GBOM批次匯入.md)。
- [ ] 正式節點與 BOM 轉換、原檔管理待家用端接續。

## PRD PD202 FT 測試程式公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core 和 MESTEST 程式／Bin 字典。
- [x] 完成 DEV 主檔、版次、核准、Bin 文字檔預覽／匯入及程式檔名選擇，整理 [規格與交接](docs/modules/PRD/FT測試程式.md)。
- [ ] 正式簽核、Bin 目錄、檔案及 API 待家用端接續。

## PRD PD204 電性抽樣規則公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core 以及 MESTEST 主檔和 Bin 表。
- [x] 完成兩種抽樣方式、Bin 選取和調序、狀態及 CRUD，整理 [規格與交接](docs/modules/PRD/電性抽樣規則.md)。
- [ ] 正式抽樣執行、核准流程及選單待家用端接續。

## PRD PD206 產品抽樣頻率規則公司端檢查點（2026-11-19）

- [x] 核對 WinForms／Designer、PRD Service／Core、WIP 呼叫端與 MESTEST。
- [x] 完成範圍鍵、三種頻率、班別、CRUD／複製及 [規格與交接](docs/modules/PRD/產品抽樣頻率規則.md)。
- [ ] 正式候選、抽樣執行與 API 待家用端接續。

## PRD PD901 產品作業站標工維護公司端檢查點（2026-11-19）

- [x] 核對客製 WinForms／Designer、PRD Service／Core、MESTEST 標工主表、歷史與科技類別 view 字典。
- [x] 完成 DEV 查詢、XLSX 預覽／本機套用、轉 SAP 預覽和 [規格與交接](docs/modules/PRD/產品作業站標工維護.md)。
- [ ] 正式工時、版次對應、SAP 與 API 待家用端接續。

## PRD OVERSTAY 產品作業站 Overstay 公司端檢查點（2026-11-19）

- [x] 核對客製 WinForms／Designer、Service／Core、MESTEST 主表與歷史表。
- [x] 完成 DEV 查詢、逐列 XLSX 預檢、本機套用與批次刪除，整理 [規格與交接](docs/modules/PRD/產品作業站Overstay維護.md)。
- [ ] 正式範圍鍵、歷史交易及 Overstay 執行待家用端接續。

## PRD 產品設定後續項目

- [ ] `PD_BATCHMODIFYAREA` 列於功能清冊，Form 為 `frmBatchModifyArea`、執行檔為 `PD_BatchModifyArea_THEIL.dll`。桌機的 `D:\iMES\CSiMES` 找不到該 Form、專案或對應 Service／Core 名稱，需等客製原始碼或業務規格補齊。
- [x] `PRD OP RULE` 已完成原始碼與字典核對、DEV 法則新增、一般法則批量替換、刪除、XLSX 預檢及 [規格與交接](docs/modules/PRD/產品作業站法則維護.md)。
- [ ] `PRD OP RULE` 的正式 CP／FT 條件、腳本編輯器與匯入轉換待接續。
- [ ] `PDCUSTOMERDEVICE`、`PRDGBOMEXPORT_OLD` 與 `PRDGBOMEXPORT` 的獨立 Form 原始碼未在桌機來源樹找到。部署目錄的 `UR_GBOMEx.dll` 包含 `frmGBOMEx`。PD102 的 GBOM 匯出子功能另有既有交接。
- [x] PDP01 晶圓良率與出貨報告維護已完成原始碼、四表 metadata、DEV 出貨規則及 [規格與交接](docs/modules/PRD/晶圓良率與出貨報告維護.md)。
- [ ] 正式報告產出、候選及檢查待接續。
- [x] PDP02 已完成 DEV 程式版次、核准、CP Bin 設定／XLSX 匯入及 [規格與交接](docs/modules/PRD/晶圓測試程式定義.md)。
- [ ] 正式 CP Bin、簽核及檔案管理待家用端接續。
- [x] PDP03 已完成 DEV 查詢、摘要與明細、XLSX 預檢及 [規格與交接](docs/modules/PRD/產品晶圓法則管制管理.md)。
- [ ] 正式晶圓腳本與合併資料匯入待家用端接續。
- [x] PDP04 完成原始碼／MESTEST 核對、DEV 疊圖主檔、條件、核准、批次複製及 [規格與交接](docs/modules/PRD/產品晶圓圖疊圖規則設置.md)。
- [ ] 正式疊圖與圖譜候選待家用端接續。
- [x] PDB01 已完成 DEV 主檔、倉庫／供應商、條件與核准，以及 [規格與交接](docs/modules/PRD/挑片規則設定.md)。
- [ ] 正式條件執行與候選待家用端接續。
- [x] PDB02 完成共用封測主檔入口、DEV 工程變更與清線查詢，以及 [規格與六表交接](docs/modules/PRD/產品設定封測.md)。
- [ ] PDB02 封測專用 BOM、EquipmentRelease、產線清線執行與正式 ECN 明細待接續。
- [x] PDB04 已完成 DEV 設定、欄位檢查、核准與 [規格及 Oracle 交接](docs/modules/PRD/檔案上傳基本資料.md)。
- [ ] 正式檔案服務及憑證管理待家用端接續。
- [x] PDB05 完成 DEV 程式查詢、檔案登錄、更新歷程與本頁下載，以及 [規格與雙表交接](docs/modules/PRD/測試程式檔案管理.md)。
- [ ] PDI01 產品設備限制維護已找到部署用 `PD_I_PRD_EquipmentRelease.dll` 與 PDB02 的查詢呼叫。公司端來源樹尚未找到 `frmPRDEquipmentRelease` 或獨立專案，正式維護欄位待補 Form／Designer 或業務規格。
- [ ] PDI03 配件群組維護已找到部署用 `PD_I_PRD_ACCGroupMaintain.dll`，公司端來源樹尚未找到 `frmPRDACCGroupMaintain` 或獨立專案。待補 Form／Designer 或業務規格。
- 靜態核對 `D:\iMES\CSiMES\Customer\Production\MESClient` 部署檔，PDI01 可見 `LoadEquipmentReleaseDetail`、`LoadAccessoryRelease`、`EditAccessoryRelease`、`DelAccessoryReleaseOneRow`；PDI03 可見 `LoadACCGroupRelationACCType` 與 `LoadPRDAccessoryGroup_Sub`。公司端可讀來源樹未找到這些方法的 Service 定義。另有 `Customer\Production\MES_C_DLL` 目錄在檔案清點時回傳 Windows 錯誤 1392，未能檢閱。

## PD206 父主檔生命週期來源查證（家用端 2026-11-19 追加）

- [x] 完成產品、設備及站點的 Form → Service → Core 查證，記錄硬刪／停用／取消核准條件、Oracle 約束與觸發器資料字典、WIP 八種範圍比對順序。可讀來源未見 PD206 guard、cascade 或最後版特例。並發與暫無合格父項時的政策仍由 YMES 決定。見 [PD206 維護文件](docs/modules/PRD/產品抽樣頻率規則.md#父主檔生命週期來源查證)。

## OE101 訂單基本資料

- [x] 以 Form／Designer、wsOE、Core 完成 DEV 訂單、屬性與明細操作，並核對 MESTEST 主檔、明細及兩張屬性表的 DDL。
- [x] [OE101 規格](docs/modules/OE/訂單基本資料.md) 含 API／schema handoff 及 CREATE-only 參考。正式字典、屬性映射、Issue Control／Job 及廠別差異列為後續。

## OE102 訂單結案

- [x] 核對 `frmROClose` Form／Designer、`wsOE.SetROBasisState` 與 `clsOEBasis`。DEV 支援多選、確認及原訂單轉結案，已結案列保留於清單。
- [x] [OE102 規格](docs/modules/OE/訂單結案.md) 記錄結案條件、逐筆失敗規則、家用端 API 交接及共用 OE101 DDL。

## OE201 工單基本資料

- [x] 追查 THEIL Form／Designer、標準與客製 Service、Core；DEV 支援主檔與流程、元件、屬性、用料預覽、核准及發放。
- [x] [OE201 規格](docs/modules/OE/工單基本資料.md) 含五張 MESTEST 表的 DDL、索引及家用端 API／schema 交接。跨模組寫入、正式簽核、挑片與 ERP 相依列為後續。

## OE202 工單刪除

- [x] 核對 `frmMODelete` Form／Designer、`wsOE.DelMOBasis` 與 Core。DEV 支援多條件查詢、多選、停用／硬刪與出錯停止。
- [x] [OE202 規格](docs/modules/OE/工單刪除.md) 記錄 ERP、廠別、批號與發料限制。DDL 共用已查證的 OE201 主檔，跨模組回復由家用端接續。

## OE203 工單查詢

- [x] 依 `frmMOQuery` Form／Designer、`wsOE.LoadMOBasis` 及 Core 完成 DEV 多條件查詢、屬性唯讀及六種工單狀態篩選。
- [x] [OE203 規格](docs/modules/OE/工單查詢.md) 記錄來源參數、欄位、手動查找差異與正式 API 交接。DDL 共用 OE201。

## OE204 工單達交率（準時開工）

- [x] 依 `frmMOQuery2` 及原 Core 完成 DEV 起迄日、產品類別、開工差異與實際比率規則。
- [x] [OE204 規格](docs/modules/OE/工單達交率_準時開工.md) 核實缺日期分母、產品版本關聯及家用端查詢交接。DDL 共用 OE201。

## OE205 工單達交率（準時完工）

- [x] 依 `frmMOQuery3` 及原 Core 完成 DEV 計畫完工日期、產品類別、完工達交率與平均生產天數。
- [x] [OE205 規格](docs/modules/OE/工單達交率_準時完工.md) 記錄 NULL、比率及生產天數分母，DDL 共用 OE201。

## OE206 工單用料清單

- [x] 核對 Form／Designer、替代料、發料入口及 Service／Core。DEV 支援兩種用料層級、增修、批次刪除、替代料與模擬發料。
- [x] [OE206 規格](docs/modules/OE/工單用料清單.md) 交付兩張表的 Oracle DDL、CREATE-only 與庫房 API handoff。

## OE207 工單存料現況

- [x] 依 Form／Designer、Service 與 Core 交付 DEV 存料與元件雙表。上層與 OE206 模擬發料共用資料。
- [x] [OE207 規格](docs/modules/OE/工單存料現況.md) 補元件表 Oracle DDL、CREATE-only 與唯讀 API 交接。

## OE208 工單結案

- [x] 依 Form／Designer、Service 與 Core 交付 DEV 選取、確認、WIP／ERP 阻擋及結案。
- [x] [OE208 規格](docs/modules/OE/工單結案.md) 記錄結案 CAS 和在製品、半成品、報廢等守門。DDL 共用 OE201。

## OE209 工單元件資料維護

- [x] 依 Form／Designer、Service、Core 交付 DEV 工單資格查詢、元件編號、草稿增刪與確認。
- [x] [OE209 規格](docs/modules/OE/工單元件資料維護作業.md) 記錄發放狀態及序號規則，補 Oracle 異動紀錄 DDL、CREATE-only 與 API handoff。

## OEB01 工單挑片資料查詢

- [x] 依 Industry Base Form／Service／Core 交付 DEV 工單主查詢、挑片明細及動態測項示例。
- [x] [OEB01 規格](docs/modules/OE/工單挑片資料查詢.md) 補政策來源、測試 Recipe Oracle DDL 與正式唯讀 API handoff。

## OEP01 工單開立_晶片測試

- [x] 依 IC_CP_PKG Form／Designer、Industry Base Service、Core 交付 DEV 晶片測試工單及流程、模擬挑片、元件驗證與發放。
- [x] [OEP01 規格](docs/modules/OE/工單開立_晶片測試.md) 交付半成品批次關聯表 Oracle DDL、CREATE-only 及 PSS API handoff。
- [x] WP101、WP102、WP201、WP202 已完成；接續功能為 WP204，進度記錄於上方。

## WP308 載具管理_元件已完成公司端 DEV（FunctionKey WP308，2026-11-20）

- [x] 依 frmCarrierManagement、frmCarrierIn、frmCarrierChange 及 Service/Core 完成 CarrierState / Detail 雙 Grid、查詢、Carrier In / Out / Change Carrier 子視窗、Carrier lookup、掃 Lot、容量／狀態驗證、位置 Up/Down 與確認互動。
- [x] Vue DEV 路徑 /YMES/WIP/CarrierManagement，掛入 WIP → 設備與載具 → 載具管理_元件；和 WP302 共用 localStorage 假資料。
- [x] 完成 docs/modules/WIP/載具管理_元件.md、模組索引、資料庫連線紀錄與兩端 TODO。WP308 沒有新資料表，沿用 WP302、WP101、Carrier Basis/Type 已查證 DDL；不重複產生 CREATE SQL。
- [x] 本輪未連 PostgreSQL，未執行 Oracle DDL/DML、build、test 或 browser。未改動 YMES API。
- [x] WP303 設備耗品現況已接續完成，詳見下一節；WP304 設備耗品歷程亦已接續完成，詳見下方。

## WP303 設備耗品現況已完成公司端 DEV（FunctionKey WP303，2026-11-20）

- [x] 依 frmEQPConsumeStatus／Define、WP_EQPConsumeReset 外掛、wsWIP/wsEQP、clsWIP/clsEQPBasis 完成主 Grid、Add/Edit/Copy/Delete、Equipment/ConsumeBasis lookup 與 ConsumReset 模擬。
- [x] Vue DEV `/YMES/WIP/EquipmentConsumeStatus`，掛入 WIP → 設備與載具 → 設備耗品現況；假資料只寫 localStorage `ymes.dev.wp303-consume-status.v1`。
- [x] PowerShell 5.1 + 受保護 Oracle profile 直連取得 Status/Log 兩張表 DDL 與 metadata；新增 source DDL、metadata、PostgreSQL CREATE-only、模組規格、索引、DB 記錄與家用端 API handoff。
- [x] ConsumeBasis 與 EquipmentBasis 沿用已存在 reference；未重複輸出 Create SQL。標明 Reset 的 ToleranceQty fallback／達上限清零行為，以及 WinForms 多筆逐列刪除的部分成功可能。
- [x] 本輪未查業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL，未部署／修改 API，未執行 build/test/browser。
- [x] WP304 設備耗品歷程已接續完成，詳見下一節；接續 WP305 設備線上點檢。

## WP304 設備耗品歷程已完成公司端 DEV（FunctionKey WP304，2026-11-20）

- [x] 依 frmEQPConsumeStatusLog／Designer、wsWIP.LoadEQPConsumeLog、wsEQP 設備與耗品 lookup、clsWIP 查詢邏輯完成 Vue DEV 唯讀頁。
- [x] Vue 路徑 `/YMES/WIP/EquipmentConsumeStatusLog`，掛入 WIP → 設備與載具 → 設備耗品歷程；設備／耗品核取篩選、From/End Date、lookup、Query、Grid 七欄與 Close 已復刻，初始不自動查詢。
- [x] 與 WP303 共用假 log localStorage。沿用 `TBLWIPEQPCONSUMELOG` 已完成 Oracle DDL、metadata 與 CREATE-only；本功能沒有新表，不重複查 DDL／產 SQL。
- [x] 規格、索引、DB 記錄、handoff/TODO 已更新；指出來源可無篩選全表查詢，正式 API 需登入保護、參數化、分頁及權限範圍。
- [x] 未查 Oracle 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL、未修改 API、未執行 build/test/browser。
- [x] WP305「設備線上點檢」已核對 WinForms／Service/Core、六張 Oracle source DDL、索引/欄位 metadata，完成 Vue DEV、Create-only 與家用 API handoff；詳下節。

## WP305 設備線上點檢已完成公司端 DEV（FunctionKey WP305，2026-11-20）

- [x] 依 `frmEQPCheckQCList`、`wsEQP/wsWIP`、`clsWIP`、`clsLot` 核對設備/點檢表載入、QCType 判定、部分存檔、候檢/補驗、Task status 及 NG 設備狀態/下機副作用。
- [x] Vue DEV 路徑 `/YMES/WIP/EquipmentOnlineQC`，掛入 WIP → 設備與載具 → 設備線上點檢；涵蓋設備 lookup、Wait QC、Re QC、四種 QCType、D01-D10、Confirm/Close 與稽核欄位。假資料存 `ymes.dev.wp305-equipment-online-qc.v1`。
- [x] PowerShell 5.1 + 受保護 Oracle profile 直連 MESTEST，查出 Log、Detail、Task 及三張狀態/下機副作用表的 DBMS_METADATA DDL、欄位/約束/索引/trigger metadata；新增 source/index DDL、metadata 與 PostgreSQL CREATE-only。
- [x] 完成 `docs/modules/WIP/設備線上點檢.md`、模組索引、連線查詢紀錄、handoff 及 home/office TODO。WP305 fake NG 不直接改設備/WIP 狀態；正式端須接 WP201/WP204 下機交易。
- [x] 未讀 Oracle 業務資料列、未執行 Oracle DDL/DML、未連 YMES PostgreSQL、未實作 API/migration/deploy，未執行 build/test/browser。
- [x] WP306「設備點檢歷程」已依 WinForms／Service／Core 完成 DEV 查詢與規格，沿用 WP305 Log/Detail；不新增 Oracle 表。
- [x] WP307「設備狀態監控_區域」已依 WP_AreaShow、LoginLogout、OperatorState、OperatorLog 及 wsSMD/clsWIP 完成 DEV 登入登出與狀態／歷史篩選；State/History 的 ShiftNo、UserNo `...` lookup 對應 `LoadShiftBasis(IssueState=2)`、`LoadUserSecurity(IssueState=2)`，選取時會自動啟用篩選；沿用 SD108 畫布並保留兩種 Function 的操作差異。
- [x] WP307 使用受保護 Oracle profile 只讀查 TBLSMDAREARELATION、TBLWIPAREAOPERATORSTATE、TBLWIPAREAOPERATORLOG DDL/metadata。TBLSMDAREARELATION 沿用已存在的 source/Create reference；新增兩張 WIP 表的 source DDL、metadata 與 PostgreSQL CREATE-only，未查 business rows、未連 YMES PostgreSQL、未執行 DDL/DML、build/test/browser。
- [x] WP305–WP307 及後續至 WP207 的 staged bundle 已逐檔核對並同步至 `D:\YMes` 工作樹；七個原有差異檔先備份並核實 SHA256，六個完全相同檔略過，未重置既有 WP305 修改。備份在 `D:\iMES\CSiMES\ymestage\wp207_sync_backup_20261120`。
- [x] WP305–WP307、WP203／WP103／WP113／WP104／WP105／WP205–WP207 已保留家用端新增歷史，rebase 後以 `b3f0f06` 正常推送 GitHub main（2026-11-20）；未執行 build/test/browser。
- [x] WP203「過帳設備設定（EQP FAVORITE SETUP）」已核對 `frmEquipmentFavorite`/`frmEquipmentFavoriteDef`、`wsEQP`、`clsEQPBasis`。DEV 頁有 Host 收藏頁 CRUD/複製、設備類別篩選、設備雙 Grid、15 台上限、重名取代及部分刪除呈現；規格與家用端 API handoff 已完成。`TBLEQPFAVORITE` DDL/CREATE-only 沿用 WP204，不重複查表或產 SQL。
- [x] WP103「列印流程卡（RUN CARD）」依 `frmBRRunCard_General`、`wsWIP`、`clsLot`、`wsPRD`、`clsProduct`、`wsRPT`、`wsSYS` 完成 source trace、DEV 假資料頁與 API/報表 handoff。Preview 多 Lot，包含 Current Process/Current PS/All、Include Log；RunCardStyle 0 預覽及只列印作用列；非 0 的 Product Map 精確／`*` fallback 已交由家用端移植 renderer。WP103 無新專用表；未新增 DDL。
- [x] Vue DEV `/YMES/WIP/RunCard` 在 Form_Load 不自動查詢；查詢條件、lookups、Status 0/1/4、LotState 過濾／排序、印表機假選擇、報表預覽、ShippingNO inert button 均記錄於 `docs/modules/WIP/列印流程卡.md`。只使用假 lot/報表資料；未執行 build/test/browser，未連 YMES PostgreSQL。
- [x] WP113「LOT SHEET PRINT」獨立於 WP103；已依 `Customer/Production/MESWin/WIP/WP_BRRunCard_General_THEIL_BU1`、`Customer/Production/wsTHEIL/wsWIP/wsWIP_Runcard.cs`、`Customer/Production/MEStc_THEIL/tcWIP/clsRuncard.cs` 完成 source trace、DEV 假資料畫面與 handoff。來源差異、查詢/流程/報表/列印規則見 `docs/modules/WIP/LOT_SHEET_PRINT.md`。重用既有 `TBLWIPLOTPROPERTY` 與 `XX_TBLLOTMARITX_THEIL_BU1`，不新增 DDL；未執行 build/test/browser，未連 YMES PostgreSQL。
- [x] WP104「使用者登入作業（上工）」依 `STD/MESWin/WIP/WP_OperatorClockIn`、`wsUSR`、`wsOP`、`wsWIP_OperatorState`、`clsOperatorState` 完成 source trace、DEV 假資料畫面與 API handoff。Oracle MESTEST 只讀查得新表 `TBLWIPOPERATORSTATE`、`TBLWIPOPERATORLOG` 的 DDL、欄位、PK/index、trigger metadata；無 FK/trigger/非 PK index。source/metadata/CREATE-only 見 `database/reference/mestest_wip_wp104_*`。LotLoginState 重用 WP305 reference；未連 PG、未執行 build/test/browser。
- [x] WP105「使用者登出作業（下工）」依 `STD/MESWin/WIP/WP_OperatorClockOut`、`wsUSR`、`wsWIP_OperatorState`、`clsOperatorState` 完成 source trace、DEV 假資料批次選取/逐筆登出及 API handoff。重用 WP104 OperatorState／OperatorLog 與 WP305 LotLoginState；無新 Oracle table/DDL。未連 PG、未執行 build/test/browser。
- [x] 核對 `WP_OperatorOnline` 是 BR 報工多處呼叫的共用查詢／登出對話框，並非獨立選單頁；暫不複製成獨立路由，後續按其宿主報工功能整合。
- [x] `WP_LotOperatorClockIn` 已核對 WinForms／Service／Core，完成 Lot status／clock status／PS／OP 查詢、Grid 預設全選、員工密碼/班別與按 mode/place 彙整的 Vue DEV 假頁和規格。重用 WP104/WP305 table references，沒有新 DDL。
- [x] `WP_LotOperatorClockOut` 已核對 WinForms／Service／Core，完成固定已上工查詢、可選員工篩選、UserNo+LoginPlace 同步勾選、批次去重及假下工頁。重用 WP104/WP305 references，沒有新 DDL。
- [ ] `WP_LotOperatorClockIn` 的正式 FunctionNo/FunctionKey 尚未從可用 WinForms 原始碼確認；目前 fallback FunctionNo `LOT OPERATOR CLOCKIN` 只由 Form 標題推導。正式整合前應核對 menu metadata。
- [ ] `WP_LotOperatorClockOut` 的正式 FunctionNo/FunctionKey 尚未從可用 WinForms 原始碼確認；目前 fallback FunctionNo `LOT OPERATOR CLOCKOUT` 只由 Form 標題推導。正式整合前應核對 menu metadata。
- [x] `WP_OperatorLogin` OP／Area 人員登入登出已依 Form、`wsUSR`、`wsOP/wsSMD`、`clsWorkTime` 完成 Vue DEV、路由與規格；WorkDay 班別預設、±1日、LoginSerial、Server time/WorkingHour 均已記錄。MESTEST 直連只讀查 `TBLUSROPERATORLOGINSTATE`、`TBLUSROPERATORLOGINLOG`，跨所有可見 Owner/Synonym 仍找不到；保留 NOT FOUND 證據，不推測 DDL/CREATE。
- [x] `WP_OperatorLogin_Lot` 已依 Form、Service、Core 完成 CallType=MenuItem 的 Lot mode 5 DEV 頁；規格記錄 CallType 1/2/3 BR 呼叫差異、時間區間、Queue/Running 與 LotLoginState/OperatorState 副作用。重用 WP104/WP305 tables，無新 DDL。
- [ ] `WP_OperatorLogin` 正式 API/schema 前，需要正確 Oracle MESTEST/來源環境的 `TBLUSROPERATORLOGINSTATE` 和 `TBLUSROPERATORLOGINLOG` DDL；目前受保護連線在所有可見 Owner/Synonym 查不到，Create SQL 不可臆造。
- [ ] `WP_OperatorLogin`、`WP_OperatorLogin_Lot` 正式 FunctionNo/FunctionKey 尚須由可靠 menu metadata 核實；目前 DEV fallback 由 Form title 推導。
- [x] 核對 `WP_OperatorOnline` 是 BR 報工共用 popup，沿其實際呼叫端整理為宿主流程需求，不另建錯誤的單獨選單頁。
- [x] WP205「生產批選用生產設備」依 WinForms／Service／Core 完成 Vue DEV 假頁、WIP 三層選單路由及規格；Lot status=1／OPStatus=1、設備 AllowExecute、容量與 LoadPort 選擇、多設備限制、LotStamp 交易守門均已記錄。重用 WP101／WP201／WP204 共用表，無新 Oracle DDL／CREATE SQL；未連 YMES PostgreSQL、未實作 API、未執行 build/test/browser。
- [x] WP206「計數不良原因收集」依 frmIPQC／frmABPosition、wsOP／wsWIP、clsOpBasis／clsLot 完成 Vue DEV、路由與規格；四張 Oracle DDL／metadata 已直連唯讀取得。`TBLWIPCONT_ABNORMAL` 無 PK，保留原唯一性；新增三張 CREATE-only，TBLOPABNORMAL 重用既有 Operation/System SQL。未查業務資料、未連 PostgreSQL、未實作 API、未執行 build/test/browser。
- [x] WP207「分批處理」已對照 THEIL WinForms、標準 wsWIP／clsLot 及 THEIL 後續更新，完成 WIP → 生產報工選單、Vue DEV Count／Qty 草稿／四組數量驗證、路由／產品選擇與規格。MESTEST 唯讀取得六張缺少 reference 的表 DDL／metadata，新增 PostgreSQL CREATE-only；`TBLWIPCONT_EMPLOYEE` 無 PK/unique index。標準拆批成功後客製更新另行提交的部分成功風險已交接。未查業務列、未連 PostgreSQL、未實作正式 API、未執行 build/test/browser。
- [x] WP208「分批處理_元件」（FunctionNo `LOT SPLIT_COMP`）已依獨立 WinForms／元件選取視窗／Service／Core 完成 WIP → 生產報工路由及 Vue DEV 左右雙 Grid、Count／Qty、母批 Yes／No、元件分配／Select All、ReturnType／路由／產品與確認守門。Core 直接引用 27 表，26 表已有 refs；`TBLSYSPARAMETER` 缺 SQL reference，已用 PowerShell 5.1／Oracle profile 唯讀查 DDL／metadata並補 CREATE-only。規格與家用端 handoff 已補。
- [x] WP209「分批處理_等級」（FunctionNo `LOT SPLIT_BIN`）已依獨立 WinForms／Bin Assign 對話框／Service／Core 完成 WIP → 生產報工路由、Vue DEV SplitLot／SplitBin 雙 Grid、Count／Qty、母批 Yes／No、Bin 分配、路由／產品／靜電箱入口與確認守門。Core 直接引用 25 表，23 表已有 refs；`TBLWIPSPLITCONTENT_BIN` 已用 Oracle profile 唯讀查 DDL／metadata並補 CREATE-only；規格與家用端 API handoff 已補。未執行 build/test/browser。
- [ ] 取得 `TBLWIPSORTERBINDATA` 的正確 Oracle schema／環境或 vendor DDL。目前 MESTEST 的 `ALL_TABLES`／`ALL_SYNONYMS`／`ALL_OBJECTS` 均查無此物件；LED_PKG 分支不可臆造 CREATE SQL 或視為可正式上線。
- [x] WP210「分批處理_載具」（`LOT SPLIT_CARRIER`）已依主／元件 Slot WinForms、Service 與兩條 Core 分支完成 WIP → 生產報工路由、Vue DEV 無元件 Count／Qty／載具指派、有元件固定 Count=2／Slot 移動／容量檢查，以及規格／家用端 API handoff。兩 Core 直接引用 17 表，逐名核對均已有 CREATE TABLE reference，本功能無新 SQL DDL；Change Product 不傳 ProductNo／Version 的原生差異已記錄。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP211「併批處理」（`LOT MERGE`）已直接核對 WinForms／Service／Core，完成 WIP → 生產報工 Vue DEV 頁、三層選單路由、Reference／Merge Grid、MO 篩選、Go／Assign、委外同單與包裝／節點提示。八張 Core 直接表均有 source DDL／CREATE-only reference，無新缺表；API 交易與原生條件已寫入規格和家用端 handoff。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP212「併批處理_元件」（`LOT MERGE_COMP`）已核原 Form／Service／Core，完成 WIP → 生產報工路由、Vue DEV 雙 Grid（候選 Lot／點選列元件）、MO 多選、Go／Assign、至少兩批、載具拒絕及 NodeID 提示。Core 直接引用十張表均已有 source DDL／CREATE-only，沒有新 SQL。載具檢查迴圈誤用 `txtLotNo` 的來源缺陷與跨庫房元件／BR 後續轉站已寫規格及家用端 handoff；未連 PostgreSQL、未執行 build/test/browser。
- [x] WP213「併批處理_等級」（`LOT MERGE_BIN`）已核原 Form／Service／Core，完成動態 SysBin 欄的候選 Grid、GoodBin 參考批、全選、Go／Assign、靜電箱入口與 NodeID 提示的 Vue DEV 頁、選單路由和規格。Core 八張直接表中七張已有 refs；`TBLWIPMERGECONTENT_BIN` 以受保護 Oracle MESTEST 唯讀取得 22 欄 DDL／metadata／index DDL，已補 PostgreSQL CREATE-only 並核 22/22 欄。未連 PG、未執行 build/test/browser。
- [x] WP214「併批處理_載具」（`LOT MERGE_CARRIER`）已核主 Form／有元件 Slot Form／Service／Core，完成參考載具、候選批、MO 多選、Go／Assign、目標空載具、Slot 逐顆／全選／退回、元件數與容量守門的 Vue DEV 頁和規格。Core 直接引用 11 表，均已有 Oracle source DDL／PostgreSQL CREATE-only reference，無新缺表，不重複建 CREATE SQL。原 Form OS 檢查把 CarrierNo 當 LotNo、Core 同 OS 條件為雙重 AND 的疑點已交家用端修正。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP215「元件分片處理」（`LOT COMP SPLIT`）已直接核 `frmSplitComponent`、`wsWIP`、`clsWIP`，完成 Vue DEV 上下雙 Grid、Select／SplitWafer、2–26 新元件、Good／Scrap 總量守恆、載具拒絕與規格／家用端 API handoff。Core 14 張直接表中 12 張已有 refs；`TBLWIPCOMPONENTBASIS`（32 欄）及 `TBLWIPREVERSEHISTORY`（11 欄）已用受保護 Oracle profile 唯讀取得 source DDL／metadata／index DDL，提供 PostgreSQL CREATE-only。CarrierNo 原 Grid 隱藏、新元件上載具 Core SQL 已註解；未連 PG、未執行 build/test/browser。
- [ ] 接續 `docs/MES_MENU_FUNCTION_INVENTORY.md` 尚未完成的 WIP 功能；先核原 Form／Service／Core，再做 Vue DEV 與規格。仍需處理先前 `TBLWIPSORTERBINDATA` 的正確來源 DDL（MESTEST 不可見）及原有來源核對待辦。
- [x] WP914「生產批緊急狀態／優先權調整」（`LOT HOTLOT AND PRIPORITY CHANGE`）已核 Form／Designer、Service／Core，完成六欄查詢、四種 lookup、18 欄可勾選 Grid、全選／取消、掃 Lot 勾選、HotLot／Priority Set 的 Vue DEV 頁與規格／家用端 API handoff。核心直接 UPDATE 已有 WP101 source DDL／CREATE-only 的 `TBLWIPLOTBASIS`，無新 SQL。原 Form 逐筆以 BaseLotNo 更新可部分成功、`funLoadOPBasis` 誤刪 Area 快取的差異已記錄；未連 Oracle 業務資料或 PostgreSQL，未執行 build/test/browser。
- [x] WP501「立即異常設定」已核 `frmImmediateHoldSet`／共用 `frmBRERFCreate`、Service/Core，完成六欄查詢、14 欄 Grid、Set／異常單主次分類、HoldItem／HoldReason、元件／Area 與 Confirm 的 Vue DEV 假交易。MESTEST 唯讀取得三張新源表 DDL／metadata，沿用 QC409 三表 source DDL，六表 PG CREATE-only 與 API handoff 已備；附件僅檔名、PDF／通知／設備多 Lot 守門與完整 Core 副作用待家用端。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP502「立即等待設定」已核主 Form／Wait Description 彈窗／Service／Core，完成七欄查詢、15 欄勾選 Grid、Select All/UnSelect All/掃描、逐批假 Wait 與 BatchWaitNo 的 Vue DEV、規格及家用端 API handoff。原 Form 包裝預檢把逗號 LotNo 當單值、逐批中途失敗會部分完成；七張 Core 直接表均已有 source/CREATE refs，無新 SQL。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP503「批次立即異常設定」已核主 Form、專屬 Batch ERF 彈窗、Service／Core，完成十欄＋CheckInTime 查詢、16 欄多選 Grid、Hold Set、0 一單多批／1 一單一批的 Vue DEV、規格及家用端 API handoff。`TBLWIPERFBATCHLOT` 源 DDL 沿用 QC409，新增 PG CREATE-only；Future Hold 兩表屬 ERFType3，不列 WP503 migration。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP504「預先異常設定」已核 WinForms／Service／Core，完成六欄 Query、八欄 Lot Grid、Selected Lot/掃描、PS→多 OP、HoldTime Start/End、分類/原因的 Vue DEV 和規格。來源 `TBLWIPFUTUREHOLDREASON` 八欄 PK、`TBLWIPFUTUREHOLDLOG` 十欄無 PK 已以受保護 Oracle profile 唯讀取得，source/metadata/index/PG CREATE-only 與家用端交接已備。Set 僅替換主/原因資料；原 Form 多批包裝預檢有逗號單值缺陷。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP505「預先等待設定」已核 WinForms／Service／Core，完成六欄 Query、六欄 Lot Grid、Selected Lot/掃描、PS→多 OP、WaitTime Start/End、可空說明的 Vue DEV 與規格。`TBLWIPFUTUREWAIT` 七欄/三鍵 PK 的 Oracle source 與 PG CREATE-only 沿用 WP201，無新缺表；原 Form 多批包裝預檢的逗號單值缺陷已交家用端。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP506「預先異常解除」已核 Form／Service／Core，完成四種勾選過濾、九欄單列 Grid、Release/Close、WP504 共享 DEV 假資料的 Vue 與規格。來源只檢選取列 LockFlag，Core 三鍵刪主表/原因可波及同鍵其他 Module/Serial/鎖定列；DEV 檢整個刪除範圍，正式 API 交家用端先決策。主/原因表 Oracle source／PG CREATE-only 沿用 WP201/WP504，無新表。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP507「預先等待解除」已核 Form／Service／Core，完成 Lot/OP/WaitTime 勾選查詢、六欄單列 Grid、Release/Close、WP505 共享 DEV 假資料的 Vue 與規格。來源三鍵剛好是 FutureWait PK；Core 通用缺鍵 DELETE 風險已交家用端。Oracle source/PG CREATE-only 沿用 WP201，無新表；未連 PostgreSQL、未執行 build/test/browser。
- [x] WP508「異常單資料」（ERF BASIS）已核主 Form／Service／Core，完成六條件＋雙日期、批次成員 Lot 查詢、25 欄 Grid。追 `WP_ERFInformation` 確認附件 Grid 可刪非產生檔，已把五頁欄位、ERF/OCAP 假附件 View/Delete、確認與檔名保護補入 Vue DEV；非真 BLOB/正式 API。已唯讀查 `TBLWIP8DBASIS_FACTORY` 及補 `TBLWIPERFLOTDISPOSITION` 16 欄/PK/LOTSERIAL 索引，後者與 OCAP 附件表交 PG CREATE-only。完整真資料、附件 API 及 WP801 8D 建立維護待家用端。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP801「廠內 8D 矯正單」已核主/Def Form、Service、Core、流水號；新增 DEV 四狀態/QRSNo 查詢、40 欄 Grid、ADD/EDIT/DEL/Copy/Approve/ERFINFO、ERF→8D 假連結。Oracle 唯讀補 SerialRuleBasis/Detail source DDL/metadata，交準確 PG CREATE-only；現有 first_phase CREATE 少五個 default。Issue Job 模式僅 DEV 控制，正式 server 決定。未連 PG、未 build/test/browser。
- [x] WP802「8D 矯正單維護作業」已核主 Form、十頁明細、D1–D8 對話窗入口及 Service/Core；Vue DEV 接 38 欄 Active 未結案查詢、十頁、子表假 CRUD/Verify/Track、D8 總結/假自動結案、階段附件與 WP508 直達。公司端受保護 Oracle MESTEST 唯讀取得 12 張 8D 子表 source DDL/metadata/index，交 PG CREATE-only；TeamMember 只有 unique index 無 PK constraint。專屬對話窗與魚骨圖細節及真 BLOB 已在規格明列簡化，家用端補正式 API/PG。未連 PostgreSQL、未 build/test/browser。
- [x] WP803「8D 矯正單結案作業」已核主/Def Form、Service/Core；Vue DEV 接四狀態/五條件/九欄、8DClose 編輯、D1 選擇、Approve 直接/送簽、8DQuery 唯讀。Release/ReleaseLog/TeamMember/Basis source 沿 WP802/WP508；部門 lookup `TBLUSRDEPARTMENTBASIS` 由 MESTEST 唯讀取得 14 欄 DDL/metadata，交 PG CREATE-only，原 first_phase 缺四個 default。來源 Pending 立即結案、簽核 SQL 改錯表、ActiveRow 錯列及非原子寫入已記規格；正式 API 交家用端。未連 PG、未執行 build/test/browser。
- [x] WP804「8D 矯正單查詢」已核 `frmWP8DQuery`、`wsWIP.Load8DBasisFactory`、`clsWIP.Load8DBasisFactory`；Vue DEV 接十組條件、39 欄全狀態 Grid、ERFNo 外部參數與 WP802 十頁唯讀。QRSClass 單勾 WIPERF/Other 來源漏送條件、CloseState 全勾放大查詢已記規格並於 DEV 修正。主表 source DDL/PG CREATE-only 沿 WP508，無新直接表。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP501 共用異常單 Release EQP 分支已追 `frmBRERFCreate` → `wsWIP.LoadWIPTemp_Equipment` → `clsLot.LoadWIPTemp_Equipment`/`UpdERF_Create`：Form 對 0 行錯稱「其他 Lot」且警告後仍提交。DEV 補 Vendor/Material 唯讀欄、設備狀態主檔數字選項、共用設備拒絕／獨佔成功及 v1 假資料增量升級；交家用端交易內設備占用守門。直接表 DDL/CREATE 已在 WP201／設備狀態 refs，無新 SQL。未連 PostgreSQL、未執行 build/test/browser。

- [x] WP510「等待單資料」已核 `frmWaitBasis`／`frmWaitInformation`、wsWIP/clsLot，完成六組勾選條件、15 欄主 Grid、七欄 Wait Info、WP502 假 Wait 唯讀合併的 Vue DEV 頁與規格。受保護 MESTEST 唯讀取得 `TBLWIPWAITLOTDISPOSITION` 13 欄 source/index DDL、metadata；無 PK/trigger、WAITNO 非唯一 index，交 PG CREATE-only。主表沿 WP201、NodeBasis 沿 PRS104。未連 PostgreSQL、未執行 build/test/browser。
- [x] WP511「等待處置」已核 THEIL BU1 主窗／七個分併批子窗入口、Service/Core、權限表；完成 `/YMES/WIP/WaitDisposition` Vue DEV 六欄查詢、17 欄 Grid、十個可見處置、Partial/同流程/同 PS 守門、簡化分併批子窗，WP510 可讀假解除結果。MESTEST 唯讀取得 `XX_TBLWAITPRIV` 14 欄／GROUPNO PK source/index DDL 與 metadata，交 PG CREATE-only/API 規格。七個子窗的 Bin/Component/Carrier 交易仍須家用端依來源實作。未連 PG、未執行 build/test/browser。
- [x] WP509「未解除異常單」已核 THEIL BU1 主/處置 Form、七個分併批子窗入口及 Service/Core；Vue DEV 六條件、23 欄 Grid、ERF Disp/Abort/Create/Info、狀態 0–3 與 11 分頁、14 個 Lot 選項、群組/OCAP/Partial 守門及簡化子窗。MESTEST 唯讀取得 `XX_TBLQCHOLDPRIV` 18 欄／GROUPNO PK source/index DDL 與 metadata，交 PG CREATE-only/API 規格。七個子窗、OCAP 全流程、ERF/Lot 原子交易與 WP508 同步由家用端補；公司端未連 PG、未執行 build/test/browser。
- [ ] 接續核對 `docs/MES_MENU_FUNCTION_INVENTORY.md` 下一個未完成 WIP FunctionNo；從 Form/Designer/Service/Core 查，再做 DEV 及規格。公司端仍只讀 Oracle DDL/metadata，不接正式 PostgreSQL/API。
- [x] WP106「使用者登入（上工）資訊」已核 `frmOperatorOnline`／Designer、`wsWIP_OperatorState`/`clsOperatorState`，完成三種地點多選並集查詢、七欄 Grid、逐筆 SignOut 與唯讀外部入口的 Vue DEV；與 WP104/105 共用假 State/Log。直接表的 Oracle source/PG CREATE-only 沿 WP104/WP305，無新 DDL。正式 API/PG 交家用端；未連 PostgreSQL、未執行 build/test/browser。
- [ ] 下一個可接 WP109「特殊生產需求維護」：先核 `STD/MESWin/WIP/WP_SWRBasis/frmWIPSWRBasis.vb` 與 `frmWIPSWRBasisDef.vb`、Service/Core、SWR 直接表；若無 source DDL 以公司 Oracle profile/PowerShell 5.1 唯讀查。公司端只做 DEV UI/邏輯及規格，不連 YMES PostgreSQL/API。


本版限定GUI的獨立證據peer `c21d7e7e220a09f64d320cc9eff0309e9be8df0a21088cceece0eecbbcc08d52` 無阻擋；Root接受回條 `69b38e831ac1a469bea728665bee2e97c05414a48b13527ab0fcc4f587cfc172`，保留90raw／83通過／7原false及復驗，非全系統驗收。程式提交 X `aa3be9528880d03f74370b22ac979dff4227570d` 已通過461來源綁定 `032ea499e39664af8558b6d148ed25fe74face9b462a9f7af7e8a77f5d88b555`，正常推送功能分支並直接讀回同ref（`30e5dd7e1b5832e3f3c4fbb08cb5956bcab15a26730af6fc164cc35890dca2cb`）。接續本份純文件status提交與main正常快轉；原checkpoint33保持至新metadata真結案，不預填已完成。


### 遠端驗證檔封存待授權（2026-11-21）

- [ ] HOME：71份版本驗證檔整包上傳在程序建立前遭自動核准審查拒絕；原因是缺這批內部程式／部署／驗證資料與指定私有目的地的明確外傳授權。未連線、未上傳、未改checkpoint；已另送精確授權問題，不等待使用者而繼續其他開發。
- [ ] 原CP33及Ready=false計畫保持，9224／f2c1工具均未遠端執行。若後續網站或API版本已前進，禁止直接執行綁舊497／4f runtime的73e結案器；須以新live版本與既有ET202歷史實證重新審查整合結案，不倒退服務或改寫舊證據。


## IV101 庫房工程群組已完成公司端 DEV／規格交接（2026-11-24）

- [x] 依 `frmINVEngineerGroupBasis`／Def、`wsINV`、`clsInventory`、`clsBasis_UPD` 完成群組 CRUD／Copy、GroupType=3 直接成員雙清單、整組 Confirm 與 DataStamp 假互動；Vue 路徑 `/YMES/INV/EngineerGroups`，資料只存瀏覽器 localStorage。
- [x] 更新 `docs/modules/INV/庫房工程群組.md`、模組索引、選單清冊及家用端 handoff。共用 `TBLUSRUSERGROUP` 沿用已查證 DDL；`TBLINVENGINEERGROUPBASIS` 缺 Oracle source DDL/metadata，現有 first-phase CREATE 是未核實推測，未複製為正式 CREATE-only。
- [x] 本桌機受保護 Oracle profile 不存在；未連 YMES PostgreSQL、未執行 Oracle DDL/DML、未修改正式 API/schema，未執行 build/test/browser。
- [ ] 家用端先查 live schema／取得可信來源 DDL，再依模組規格完成主檔、Copy、Delete、成員替換與代理人同步 API，確保交易及 DataStamp CAS；勿直接沿用未驗證 CREATE。
- [x] IV102、IV103、IV201、IV202、IV207、IV203、IV208、IV205、IV210、IV206、IV211、IV302 均已完成公司端交接；目前下一個清冊項目為 IV303「退料單記錄(WIP)」。

## IV102 庫房基本資料已完成公司端 DEV／規格交接（2026-11-24）

- [x] 依 `frmInventoryBasis`／Def、Property／Locator 子畫面、`wsSMD`／`wsINV`、`clsINVBasis`／`clsBasis_UPD` 完成主檔查詢、四狀態、Add/Edit/Copy/Delete/Approve、Property 子畫面及重用 Locator DEV。路徑 `/YMES/INV/InventoryBasis`；假資料使用 localStorage，Locator 固定於所選 InventoryNo。
- [x] 更新 `docs/modules/INV/庫房基本資料.md`、模組索引、選單清冊及家用端 handoff。Locator 沿用已查證 `TBLINVLOCATORBASIS` DDL；`TBLINVINVENTORYBASIS` 和 `TBLINVInventoryProperty` 缺 Oracle source DDL/metadata，repo 的 first-phase 主檔 CREATE 欄位未核實，不另產正式 CREATE-only。
- [x] 本桌機受保護 Oracle profile 不存在；未連 YMES PostgreSQL、未執行 Oracle DDL/DML，未實作正式 API/schema，未執行 build/test/browser。
- [ ] 家用端先核 live Inventory Basis／Property schema，再按模組規格接 IssueManagement、DataStamp、狀態 0/-1 實體刪除／狀態 2 軟刪與同交易 Property／Locator cascade。不可照抄推測 CREATE。
- [x] IV103「在製貨架基本資料」及後續 IV201、IV202、IV207、IV203、IV208、IV205、IV210 已完成；目前下一個清冊項目為 IV206「開立退料單」。

## IV103 在製貨架基本資料已完成公司端 DEV／規格交接（2026-11-24）

- [x] 依 `frmINVStockerBasis`／Def／LocatorNoEdit、`wsINV`／`wsSMD`、`clsInventory`／`clsBasis_UPD` 完成四狀態、StockerNo 查詢、主檔 CRUD／Copy／Approve、V/H 動態 Locator 格位及逐格 LocatorNo 編輯、下方明細 DEV。路徑 `/YMES/INV/StockerBasis`，localStorage 假資料。
- [x] 更新 `docs/modules/INV/在製貨架基本資料.md`、規格索引、選單清冊與家用端 handoff。確認 `TBLINVSTOCKERBASIS` first-phase CREATE 為推測；`TBLINVSTOCKERDETAIL` 沒有已核實 Oracle DDL／metadata；未新增 CREATE-only。
- [x] 本桌機受保護 Oracle profile 不存在；未連 YMES PostgreSQL、未執行 Oracle DDL/DML、未實作正式 API/schema、未執行 build/test/browser。
- [ ] 家用端先核 live `tblinvstockerbasis`／`tblinvstockerdetail` schema；正式 API 需 DataStamp CAS，並交易保護主檔及明細整組替換／刪除。簽核須確認舊 Service 目前直接 Active 的規則是否仍適用。
- [x] IV201「原物料倉庫房資料」與後續 IV202「開立進料單」、IV207「進料單記錄」、IV203「開立領料單」、IV208「領料單記錄」、IV205「開立預訂單」、IV210「預訂單記錄」、IV206「開立退料單」、IV211「退料單記錄」、IV302「開立退料單(WIP)」均已交付；目前下一個清冊項目為 IV303「退料單記錄(WIP)」。

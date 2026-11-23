# MES WinForms 復刻規格索引

本索引追蹤從舊 MES WinForms 與 Oracle schema 還原出的畫面、操作規則、資料表及 YMES API 規格。每個功能以獨立 Markdown 規格記錄；未從程式碼或資料庫確認的內容標示「待查」，不以畫面推測成正式規則。

2026-11-19 家用端 checkpoint：forward migrations 0034–0041 已實際套用，三份備份已驗證，詳見 [資料庫部署紀錄](DATABASE_CONNECTION.md)。QC103／QC107／QC108 以各模組的實測範圍為準，未交付的分類／Core／metadata／函式不因 schema 建立而完成；0038–0040 僅為 PRD schema groundwork，Program／RecipeGroup 指派 API 尚未部署。三個 QC 模組的專屬 fixtures 已逐筆從 UI／API 刪除，最終精確本輪識別鍵 SQL 殘留各0；詳見模組驗收紀錄。

- QC501「檢測儀器類別」：[功能規格](modules/QC/檢測儀器類別.md)。共用六表 migration 0027、登入保護 API 已部署；app-browser 已驗證新增/套用/修改、複製/Driver lookup、取消/確定核准、引用刪除防護、狀態 1 唯讀、-1/2 刪除、四狀態/Close、錯誤文字與 Demo 登入。本輪測試資料已清空；共用簽核/Issue Control 限制見規格。公司端 QC503 UI 已於 `b572f3b` 提交，下一步為家用端 API handoff。
- QC502「檢測儀器基本資料」：[功能規格](modules/QC/檢測儀器基本資料.md)。主表 `TBLQCINSPMACHINEPARAMETER`；CRUD/lookup API 已部署，類別通訊參數以交易內鎖定重讀為準、DataStamp 保留 NULL 並精確 CAS。app-browser 已驗證新增/套用/修改、RS232→UserDefine 類別更換、複製/Driver lookup、核准、取消/確定刪除、舊版本衝突、四狀態/Close、錯誤文字與 Demo 登入。本輪測試資料已清空，共用資源限制依規格追蹤。
- QC503「檢測設備連線程式」：[功能規格](modules/QC/檢測設備連線程式.md)。登入保護 API 已部署並完成本輪正式 app-browser CRUD/查找/核准/狀態/引用防護/nullable CAS 驗證；沿用 0027 Driver 表與共用查找，測試資料已清空。共用簽核與解析函式限制見規格。
- QC408「OCAP基本資料」：[功能規格](modules/QC/OCAP基本資料.md)。已對照四個 WinForms Form、`wsQC`、`clsQCBasis`，以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連查得三張 OCAP 表 DDL，新增 Oracle source DDL/PG CREATE-only 參考及 QC → 異常設定三層選單、Vue DEV 清單/CRUD/核准/刪除/流程預覽與簡化節點繪製器。家用端待比對 YMES live schema、實作三表交易與 API，並先釐清 binary `iMESXMLFlow.dll` 的 XML 相容策略。

- PS103「流程類別資料」：[功能規格](modules/PRS/流程類別資料.md)。已直接對照主/Def Form、`wsPRS`、`clsProcess`、`clsBasis_UPD` 與共用 `PS_ProcessProperty`，並以受保護 Oracle 連線直查 `TBLPRSTYPE` DDL；新增 Vue DEV 頁、三層選單、Oracle/PG SQL 參考及家用端 API/schema handoff。PS104 ProcessType lookup 共用同一張表。
- PS104「流程基本資料」：[功能規格](modules/PRS/流程基本資料.md)。已直接閱讀主/Def/Draw/Exclude OP、Property、Import WinForms 與 `wsPRS`/`clsProcess`，並直連查得九張 Oracle DDL，完成 Vue DEV 畫面、PRS 路由、Oracle source DDL/PG CREATE-only；家用端核對 live schema/default、補缺 migrations、完成流程 API/共用簽核及 XML 相容方案。
- PS101「生產區段資料」：[功能規格](modules/PRS/生產區段資料.md)。已直接閱讀主/Def WinForms、wsPRS、clsProcess、clsBasis_UPD 與 SYS LOTNO 序號規則 lookup，並直連取得 Oracle table/index DDL；完成 Vue DEV 清單/定義/選擇器、三層路由與 Oracle/PG SQL/API handoff。Production API/live schema 比對交由家用端，PS104 重用此表。 家用端2026-11-19：API/schema及上述模組文件所列正式頁面操作已驗證；具體未完成項以模組最新驗收紀錄為準。
- PS102「製造層別設定」：[功能規格](modules/PRS/製造層別設定.md)。migration 0028 與登入保護 API/Web 已部署；正式 app-browser 驗證新增/套用/修改/複製防重、多選核准/停用/刪除、四狀態與 Close，測試資料已清空。共用簽核限制見規格。
- OP101「作業站法則設定」：[功能規格](modules/OP/作業站法則設定.md)。已閱讀 WinForms Form/Def、wsOP、clsOpBasis、clsBasis_UPD，並以 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 TBLOPCONTROLRULE DDL，完成作業站資料→作業站設定三層選單、Vue DEV CRUD/複製及 PostgreSQL CREATE handoff；Production API 與 OP_Script 完整編輯器待家用端。
- OP102「企業邏輯設定」：[功能規格](modules/OP/企業邏輯設定.md)。已直接閱讀 WinForms 清單/定義 Form、wsOP、clsOpBasis、clsBasis_UPD，PowerShell 5.1/ODP.NET 唯讀取得主表與情境 lookup Oracle DDL，完成 OP 三層選單、Vue DEV CRUD/簽核/狀態頁及 PostgreSQL CREATE-only handoff；Production API 與 YMES live schema 比對待家用端。
- OP103「作業站類別資料」：[功能規格](modules/OP/作業站類別資料.md)。已直接對照 frmOPType/Def、wsOP 與 clsOpBasis，完成 /YMES/MasterData/OperationTypes Vue DEV 清單/新增/修改/複製/刪除/核准及 Property 假資料視窗。Oracle TBLOPTYPE DDL 與 PostgreSQL CREATE-only 參考已保存；Production schema/API 與共享簽核待家用端實作。

- OP105「作業站群組資料」：[功能規格](modules/OP/作業站群組資料.md)。已直接對照 WinForms 主/明細 Form、wsOP、clsOpBasis、clsBasis_UPD，並用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 TBLOPGROUPBASIS/TBLOPGROUPDETAIL DDL、PK/index/constraint；完成 OP 三層選單與 Vue DEV 雙 Grid/主明細 CRUD/Check。Oracle source DDL 與 PG CREATE-only 已保存；家用端待核對 live schema、補缺 defaults migration，實作 group/member API 與交易刪除。
- OPB01「作業站參數群組設定」：[功能規格](modules/OP/作業站參數群組設定.md)。已直接閱讀主/Def WinForms、Designer、wsOP、clsOpBasis、更新 Core 及 SYS Element 驗證；PowerShell 5.1 + Oracle.ManagedDataAccess 直連唯讀取得 TBLOPGROUPATTRIB/TBLSYSELEMENTBASIS DDL，完成行業包三層選單、Vue DEV 主 Grid/多列 Element 編輯、CRUD/Copy、lookup 與 API handoff。主表 Oracle 只有複合 PK；共用 Element 表沿用既有 schema。家用端待核對 YMES live schema並實作 batch/DataStamp/Element values API。
- OPB02「ECN 作業站群組設定」：[功能規格](modules/OP/ECN作業站群組設定.md)。已直接閱讀主/Def WinForms、`wsOP`、`clsOpBasis`、DataStamp update 並以 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查得兩表 DDL/constraints/indexes；公司端完成行業包三層選單、Vue DEV 主/明細 Grid、CRUD/Copy/核准、狀態刪除與 lookup。Detail 的 PK 是全表唯一 OPNo；Oracle source DDL/PG CREATE handoff 已保存，家用端待核對 YMES live schema 並實作共享簽核與 API。
- OPB03「客戶站點對應設定」：[功能規格](modules/OP/客戶站點對應設定.md)。已逐段閱讀三個 WinForms Form/Designer、wsOP、clsOpBasis、clsB_Basis_UPD；PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得兩表 DDL 並查 PK/index/constraint；完成行業包三層選單與 Vue DEV 雙 Grid、主/明細編輯/lookup/CRUD。Oracle source DDL 與 PostgreSQL CREATE-only handoff 已保存；家用端待比對 live schema 並實作 lookup/API/交易刪除。

 - WP101「生產批開立」：[功能規格](modules/WIP/生產批開立.md)。已核對 WinForms／wsWIP／clsLot、七張 Oracle WIP 表與下線／刪除交易；Vue DEV 路由 /YMES/WIP/LotBasisCreate 完成。Oracle source DDL 與 PostgreSQL CREATE-only 參考已交付；家用端需比對 live schema/migrations 並實作含物料扣帳、CloseLot、StartLot 和逐批下線的正式 API。
- WP102「生產批開立_批次」：[功能規格](modules/WIP/生產批開立_批次.md)。已對照 WinForms／THEIL Service／Core，唯讀查得 Lot Property TEMP 與正式表 DDL；Vue DEV 路由 /YMES/WIP/LotBasisBatch 完成。WP102 CREATE-only 僅含新增需求 TBLWIPLOTPROPERTY_TEMP，正式 TBLWIPLOTPROPERTY 沿用 WP101；家用端待核對 live schema 並實作逐批結果、序號保留與交易式下線 API。
- WP201「生產批執行」：[功能規格](modules/WIP/生產批執行.md)。已對照 WinForms／Service／Core，Oracle 唯讀查得 39 張執行相依表 DDL，39 表欄位數符合 ALL_TAB_COLUMNS；兩個缺失客製引用已註記。Vue DEV 路由 /YMES/WIP/LotExecution 完成；家用端待核對 live schema 並決定動態 BR、QTime/LMS、Group OP 與元件批次 API。
- WP202「生產批執行_元件」：[功能規格](modules/WIP/生產批執行_元件.md)。已對照標準 WinForms／Service／Core；Component 查詢沿用 WP101 已查證的 TBLWIPLOTSTATE、TBLWIPCOMPONENTSTATE、TBLWIPLOTBASIS，不新增重複 DDL。Vue DEV 路由 /YMES/WIP/LotExecutionByComponent 完成；家用端需比對 live schema，並將元件查詢接入 WP201 共用的 Lot Execute command。
- WP204「生產批執行_設備」：[功能規格](modules/WIP/生產批執行_設備.md)。依 frmEquipmentLotState／CI／CO／狀態控制 Form、wsEQP／wsEMS／wsWIP 與 Core 完成 DEV 設備看板；唯讀取得四張缺少的 MESTEST DDL，提供 CREATE-only 參考。家用端需先比對 PostgreSQL schema，再將看板、設備／LoadPort、候選批次、CI／CO、Hold 及狀態控制接入 WP201 共用執行 API。
- WP205「生產批選用生產設備」：[功能規格](modules/WIP/生產批選用生產設備.md)。依 frmLotChoiceEqp、wsWIP、clsLot、clsOperatorState 完成 Lot／設備／LoadPort 假資料互動；重用 WP101／WP201／WP204 共用表，不新增 DDL。家用端需以交易式 API 重新驗證 LotStamp、設備容量／可用狀態與 LoadPort，再接入既有設備進站與多人加工核心。
- WP206「計數不良原因收集」：[功能規格](modules/WIP/計數不良原因收集.md)。依 frmIPQC／frmABPosition、wsOP／wsWIP、clsOpBasis／clsLot 完成 OP 異常與 Position 多選 DEV；Oracle 查得四表 metadata，`TBLWIPCONT_ABNORMAL` 無 PK 且僅有 LotSerial 非唯一索引；新增三張缺少的 CREATE-only，`TBLOPABNORMAL` 重用 Operation/System reference。
- WP207「分批處理」：[功能規格](modules/WIP/分批處理.md)。依 THEIL WinForms／標準 wsWIP/clsLot 與客製 UPD_SplitLot 完成 Count/Qty、母批保留、數量守恆、路由／產品選擇的 Vue DEV 頁；Oracle 唯讀查得六張缺少 reference 的表 DDL/metadata 並提供 CREATE-only。家用端須將標準拆批與 THEIL SAP/Matrix 更新視為同一個可恢復交易契約，並比對 live schema。
- WP208「分批處理_元件」：[功能規格](modules/WIP/分批處理_元件.md)。依獨立標準 WinForms、元件選取視窗、wsWIP/clsLot 完成 Count/Qty、母批選項、左右雙 Grid、候選元件分配、ReturnType／路由／產品選擇的 Vue DEV 頁。Core 直接引用 27 表；其中缺 SQL reference 的 `TBLSYSPARAMETER` 已唯讀查得 Oracle DDL／metadata 並補 PostgreSQL CREATE-only，另外 26 表沿用既有 refs。家用端須實作元件守恆與多表拆批交易，並檢查 BR／SEMI_Wafer 後續轉站。
- WP209「分批處理_等級」：[功能規格](modules/WIP/分批處理_等級.md)。依獨立 WinForms、Bin Assign 視窗、wsWIP/clsLot 完成 Count/Qty、母批選項、SplitLot／SplitBin 雙 Grid、動態 Bin 分配、ReturnType／路由／產品及靜電箱入口的 Vue DEV 頁。Core 直接引用 25 表；23 表沿用既有 refs，`TBLWIPSPLITCONTENT_BIN` 已唯讀取得 Oracle DDL／metadata 並補 CREATE-only。`TBLWIPSORTERBINDATA` 在目前 MESTEST 可見物件中查無，LED_PKG 分支待正確 DDL；家用端須實作 Bin 守恆、多表交易與 BR 後續轉站。
- WP210「分批處理_載具」：[功能規格](modules/WIP/分批處理_載具.md)。依 `frmBR_CL_Split_GEN`、`frmCarrierSplit_Comp`、`Exe_Manual_LotSplit_Carrier` 及兩條 Core 分支完成參考載具查詢、無元件 Count／Qty 分批、有元件固定兩批及雙載具 Slot／元件移動的 Vue DEV 頁。Core 直接引用 17 表，均已有 Oracle／PostgreSQL source references；沒有新增 CREATE SQL。家用端須核對 live schema 並實作載具、位置、元件與 Lot 的一致交易；原 Change Product 按鈕未將產品欄傳給 Service 的來源差異已明載。
- WP211「併批處理」：[功能規格](modules/WIP/併批處理.md)。依 `frmBR_L_Merge_GEN`、`Exe_Manual_LotMerge`、`BR_Manual_LotMerge` 完成參考批／候選批、MO 多選、Go／Assign、同外包單、包裝拒絕與 NodeID 提示的 Vue DEV 頁。Core 直接引用八張表，均已有 source DDL／CREATE-only reference，不新增重複 SQL。家用端須核 live schema、共用 helper 間接表與原子併批／BR 後續轉站 API。
- WP212「併批處理_元件」：[功能規格](modules/WIP/併批處理_元件.md)。依 `frmBR_LC_Merge_GEN`、`Exe_Manual_LotMerge_Component`、`BR_Manual_LotMerge_Component` 完成左側候選、右側元件明細、MO 多選、Go／Assign、至少兩批、載具守門與 NodeID 提示的 Vue DEV 頁。Core 直接引用十張表，均有 Oracle source DDL／PostgreSQL CREATE-only；原載具檢查迴圈重複查輸出批號的來源缺陷已記錄，正式 API 必須逐批重查。
- WP213「併批處理_等級」：[功能規格](modules/WIP/併批處理_等級.md)。依 `frmBR_LB_Merge_FT`、`Exe_Manual_LotMerge_Bin`、`BR_Manual_LotMerge_Bin` 完成動態 Bin 候選 Grid、GoodBin 參考批、全選、Go／Assign、靜電箱入口與 NodeID 提示的 Vue DEV 頁。Core 直接引用八表，七表沿用既有 refs；新查 `TBLWIPMERGECONTENT_BIN` Oracle DDL／metadata（22 欄）並提供 PostgreSQL CREATE-only。家用端須重讀 Bin/GoodBin 與 LotStamp、完成交易及 BR 後續轉站。
- WP214「併批處理_載具」：[功能規格](modules/WIP/併批處理_載具.md)。依 `frmBR_CL_Merge_GEN`／`frmCarrierMerge_Comp`、`Exe_Manual_LotMerge_Carrier`、`BR_Manual_LotMerge_Carrier` 完成載具參考／候選 Grid、MO 多選、Go／Assign、目標空載具與有元件 Slot 移動視窗的 Vue DEV 頁。Core 直接引用 11 表，均已有 Oracle source DDL／PostgreSQL CREATE-only reference；沒有新缺表。原 Form 的 OS helper 傳錯 CarrierNo 與 Core 的同 OS 守門條件疑點已記錄，正式 API 必須依 LotNo 與每筆來源重查。
- WP215「元件分片處理」：[功能規格](modules/WIP/元件分片處理.md)。依 `frmSplitComponent`、`wsWIP.EXE_SplitComponent`、`clsWIP.EXE_SplitComponent` 完成上方來源元件、下方新元件 Grid、2–26 分片、兩種數量守恆及載具拒絕的 Vue DEV 頁。Core 直接引用 14 表，其中 12 表已有 refs；新增 `TBLWIPCOMPONENTBASIS` 與 `TBLWIPREVERSEHISTORY` 的 Oracle 唯讀 DDL／metadata及 PostgreSQL CREATE-only。家用端核 live schema、完成原子編號與交易 API；CarrierNo 原畫面隱藏且新元件上載具 Core 已註解。
- WP914「生產批緊急狀態／優先權調整」：[功能規格](modules/WIP/生產批緊急狀態與優先權調整.md)。依 WinForms Form/Designer、`wsWIP.EditLotBasis_Confirm`、`clsLot.EditLotBasis` 完成查詢、四種 lookup、可勾選 Grid、掃 Lot 勾選、HotLot 與 0–99 Priority 的 Vue DEV 頁。原 Form 逐列以 BaseLotNo 呼叫，可能部分成功；Core 只更新 `TBLWIPLOTBASIS`，直接表與 RO／Area／OP／Product lookup 表均已有 source DDL／CREATE 參考，無新 SQL。家用端須核 live schema 並提供權限、版本與原子批次 API。
- WP501「立即異常設定」：[功能規格](modules/WIP/立即異常設定.md)。依 `frmImmediateHoldSet`、共用 `frmBRERFCreate`、`wsWIP.Exe_ERF_Create`、`clsLot.UpdERF_Create` 完成 DEV 六欄查詢、14 欄 Lot Grid、Set／異常單分類、HoldItem／HoldReason、元件／Area、Confirm 假交易。Oracle 唯讀新查三張 ERF/備份表，另沿用 QC409 三張原始 DDL；六表 PostgreSQL CREATE-only 參考已備。家用端須核 live schema 並實作跨 ERF、Lot、原因、附件、歷程／OCAP 的正式交易。DEV 只記檔名、不上傳或產報表。
- WP502「立即等待設定」：[功能規格](modules/WIP/立即等待設定.md)。依 `frmImmediateWaitSet`、`WP_WaitDescription`、`wsWIP.Exe_Wait_Set`、`clsLot.SetLotStateToWait` 完成 DEV 七欄查詢、15 欄可勾選 Lot Grid、Select All／掃描、共用說明視窗及逐批假 Wait。原 Form 包裝預檢把逗號 LotNo 當單值，且逐批中途失敗可部分完成；正式 API 須逐筆重查並明定批次交易語義。直接表都已有 WP101/WP201/QC409 source 與 CREATE 參考，無新增 SQL。
- WP503「批次立即異常設定」：[功能規格](modules/WIP/批次立即異常設定.md)。依 `frmImmediateHoldSet_Batch`／專屬 `frmBRERFCreate_Batch`、`wsWIP.Exe_ERF_Create_Batch`、`clsLot.UpdERF_Create_Batch` 完成十欄＋時間查詢、16 欄多選 Grid、Hold Set 與批次異常彈窗 DEV。`ERFBatchHoldGenType=0` 一單多批寫 BatchLot；`1` 一單一批轉 ERFType 2，原視窗可能部分成功。BatchLot Oracle source 沿用 QC409，新增單表 PostgreSQL CREATE-only；Future Hold 分支表不屬 WP503。
- WP504「預先異常設定」：[功能規格](modules/WIP/預先異常設定.md)。依 `frmFutureHoldSet`、`wsWIP.Exe_FutureHold_Set`、`clsLot.UpdFutureHold_Set` 完成六欄 Query、八欄 Lot Grid、勾選/掃描、PS→多 OP、Start/End、分類/原因與 Confirm DEV。Core 按 Lot×OP 鍵替換 FutureHold/Reason，Set 時不改 Lot 狀態；原 Form 多批包裝預檢逗號字串有漏檢風險。Oracle 唯讀取得 Reason/Log 兩表 DDL，提供 CREATE-only；Log 無 PK，屬後續 ERFType3 生效路徑。
- WP505「預先等待設定」：[功能規格](modules/WIP/預先等待設定.md)。依 `frmFutureWaitSet`、`wsWIP.Exe_FutureWait_Set`、`clsLot.UpdFutureWait_Set` 完成六欄 Query、六欄 Lot Grid、Selected Lot/掃描、PS→多 OP、Start/End、可空 Description 與 Confirm DEV。Core 按 Lot×OP×WaitTime 替換 FutureWait，Set 時不改 Lot 狀態；原 Form 多批包裝預檢有逗號單值缺陷。FutureWait Oracle DDL/PG CREATE-only 已有 WP201 refs，無新 SQL。
- WP601「包裝作業」：[功能規格](modules/WIP/包裝作業.md)。依 `frmBR_L_CO_Packing_MOLine`／PackConfig、`wsWIP`／`clsLot` 完成 Vue DEV 包裝設定、掃 Lot/包裝、階層滿箱、封箱、標籤及拆併箱模擬；Oracle MESTEST 唯讀查證 19 張功能表 DDL/metadata 並提供 CREATE-only。家用端需先比對 live schema，實作 Lot split、L1–L6 Relation/PackState/Log、庫存狀態與 label job 的一致交易；Combine/Unboxing 外掛契約仍需處理。
- WP629「在製品庫包裝作業」：[功能規格](modules/WIP/在製品庫包裝作業.md)。依 `frmBR_L_CO_Packing_MOLine_INV`、`wsWIP.ExePacking` 與 `clsLot` 完成獨立路由及 WP601 共用 WinForms 版型的 Vue DEV 頁；強制 Status=11、WIP InventoryNo 一致，封箱保持在製品狀態。沒有專屬新表；沿用 WP601 包裝表與 WP201 的 `TBLINVWIPINVENTORY_SEMI` Oracle DDL／CREATE reference。家用端須核對 live schema並以 inline inventory mode 接入相同交易核心，不新增重複表。
- WP301「設備上料」：[功能規格](modules/WIP/設備上料.md)。依 `frmEQPMaterialLoading`、`wsWIP_Material` 與 `clsMaterialHandler` 完成設備／工單查找、物料／元件／半成品三模式、階層現況、上下移序及下料 DEV；PowerShell 5.1 + ODP.NET 唯讀取得四張未有現成 reference 的 MESTEST 表 DDL／metadata，提供 Oracle source DDL、PostgreSQL CREATE-only。其餘 13 張相依表沿用既有 references；家用端待核對 live schema並實作跨庫存／設備現況交易 API。
- WP302「載具現況管理」：[功能規格](modules/WIP/載具現況管理.md)。依 `frmCarrierState`／`wsEQP`／`wsWIP`／`wsINV` 與 `clsCarrierBasis` 完成 CarrierState／Detail 雙 Grid、Lot／Component 放入、整批取出、清空及受權限控管的隱藏狀態切換 DEV。PowerShell 5.1 + ODP.NET 唯讀查得 Detail、DetailLog、StockerState 三表 DDL／metadata，提供 source DDL 與 CREATE-only；Detail/Log 無 PK 的限制與刪除後可能留舊 CarrierState 的來源行為均已註明。家用端待比對 live schema、完成鎖定交易與 API。
- WP603「在製品庫帳務維護」：[功能規格](modules/WIP/在製品庫帳務維護.md)。依共用 WinForms／Service／Core 完成 8 個 Designer 可見分頁、DEV 假資料操作及九張來源表 DDL／CREATE-only。THEIL 專用 Form 原始碼未在公司來源樹，差異與限制已標註；家用端待比對 PostgreSQL schema 並實作庫存、報廢、轉庫、拆併批及入庫交易 API。
- WP604「半成品庫帳務維護」：[功能規格](modules/WIP/半成品庫帳務維護.md)。已對照 `frmInventory_SEMI`／Turn／SEMI In Form、Designer、wsINV／wsWIP／wsSYS 與 clsInventory／clsLot；完成 Vue DEV 庫存查詢、Component/Bin 明細、SEMI In 假確認及 WIP/FGD/SEMI 轉庫互動。Oracle 唯讀核實四張 SEMI 專用表 DDL／索引／約束／Trigger，提供 CREATE-only 與家用端 API/schema handoff。
- WP605「成品庫帳務維護」：[功能規格](modules/WIP/成品庫帳務維護.md)。依 frmInventory_FGD／FGDIn／Turn／Define、wsINV／wsWIP／wsSYS 與 Core 完成 Vue DEV 庫存、動態 Bin、人工調整、轉庫及入庫確認畫面。Oracle 唯讀核對六張新增表，含交易 Log；DDL／CREATE-only handoff 已建立。家用端需先核對 PostgreSQL live schema，再建置完整交易、庫存過帳及 WIP／SEMI／Lot history API。WP603、WP604 已完成公司端 DEV 與交接。

## 規格產出原則

- 以 WinForms Form、Service/Core 類別及 MES Oracle 資料字典為依據；截圖用於核對版面，不取代程式碼與資料表證據。
- 每個功能頁記錄選單階層、Form 名稱、欄位、查詢條件、按鈕、狀態轉換、驗證、交易與資料表。
- 同時記錄 Vue UI 狀態、API 契約及資料庫待辦。公司電腦 Development 可用明確標示的本地假資料完成互動；Production API 尚未完成時，必須如實標示待建置。
- 共用文件：[功能規格範本](MODULE_SPEC_TEMPLATE.md)、[資料庫連線與憑證處理](DATABASE_CONNECTION.md)、[開發分工與 UI 準則](DEVELOPMENT_STYLE.md)。

## 2026-11-17 品管項目匯入規格

- QC301「管制圖內容」：[功能規格](modules/QC/管制圖內容.md)。已直接閱讀 QC301 Form/Define/Copy Form、QC Rule Form、wsQC 與 clsQCBasis；透過 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得抽樣參數表 DDL，沿用 QC302 四張 chart DDL，完成三區 Vue DEV 頁與 API handoff。家用端待查 live schema/migrations、確認 Issue/Job、刪除引用與 factor JSONB，並建置登入保護 API。
- QC303「品管項目規格」：[功能規格](modules/QC/品管項目規格.md)。已完成 Vue 正式頁、登入保護 API、QC303 PostgreSQL migration 並部署 YMES；依使用者指示只要求登入、不實作 FunctionKey/角色 ACL。正式頁及狀態篩選已驗收；live QC Item 為空，CRUD 無業務資料可端到端驗收；QCSN、參數目錄、Issue/Job 共用資源未建立，限制詳見模組文件。
- QC304「物料規格設定」：[功能規格](modules/QC/物料規格設定.md)。已直接讀 `frmQCMaterialSpec`、wsQC 與 clsQCBasis，完成 Material search、SPEC detail editor、批次套用/貼上/驗證的 Vue DEV 頁；沿用 QC302 的 Chart Basis/Detail/DetailLog 表與既有 MTL/QC Item/Group references，無 QC304 專屬新表或 CREATE SQL。家用端待比對 live schema 並實作批次 API。
- QC305「產品規格設定」：[功能規格](modules/QC/產品規格設定.md)。公司端依 `frmQCProductSpec` 與 `clsQCBasis` 完成產品類別/編號查詢、ProductData、SPEC 編輯與多產品套用 Vue DEV 頁；規格記錄 Product 因子、GroupNo、QC_Count、SPCID 映射及舊 Core 的 ProductNo-only 行為。沿用 QC302 圖表表與既有產品/QC Item 表，無 QC305 專屬新表或 CREATE SQL；家用端待核對 live schema 並實作批次 API。
- QC302「匯入管制圖內容」：[功能規格](modules/QC/匯入管制圖內容.md)。已直接閱讀 WinForms Form、wsQC、clsQCBasis，透過 PowerShell 5.1 唯讀取得四張 SPC QC Chart Oracle DDL，並完成 Vue DEV 頁與 PostgreSQL CREATE-only 參考；家用端待比對 live schema、建立缺漏 migration/API。
- QC215「計數品管項目匯入」：[功能規格](modules/QC/計數品管項目匯入.md)。已直接對照 WinForms Form、wsQC、clsQCBasis，完成 QC215 Vue DEV 頁與逐列匯入規則文件。沿用 QC201/QC205/QC213 的主子表與依賴表，不需要新表或 CREATE SQL；家用端待核對 YMES schema 並實作 API。
- QC214「計量品管項目匯入」：[功能規格](modules/QC/計量品管項目匯入.md)。已直接對照 WinForms Form、wsQC、clsQCBasis，完成 Vue DEV 頁與逐列匯入規則文件。沿用 QC201/QC205/QC210 的主子表與依賴表，不需要新表或 CREATE SQL；家用端待核對 YMES schema 並實作 API。

## 2026-11-17 新增完成規格

- EQP `B_EQP AUTOLOADER SETUP`（EPB01）設備 AutoLoader 格式檔設定：[`modules/EQP/設備AutoLoader格式檔設定.md`](modules/EQP/設備AutoLoader格式檔設定.md)。Oracle source DDL 與 YMES 空表 CREATE 參考：`database/reference/mestest_eqp_autoloader_source_ddl.sql`、`database/reference/mestest_eqp_autoloader_create.sql`。公司端 Vue DEV 頁掛載 `/YMES/EQP/EquipmentAutoLoaderSetup`；API、schema migration、選項與解析驗證待家用端交接。
- Oracle 來源為 `TBLEQPALSCRIPT`、`TBLEQPALSCRIPT_TESTRECIPE`、`TBLEQPALSCRIPT_UNNUMERICFIELD`、`TBLEQPALSCRIPT_COORDINFO`；主表唯一索引是 `(EquipmentNo,FileNameFormat)`，來源無 FK。沒有在 Oracle 執行 DDL 或讀取 MES 業務列。

## 2026-11-17 新增功能規格

- QC216「計量品管項目群組_減項」：[功能規格](modules/QC/計量品管項目群組_減項.md)。公司端已對照 `frmPMI_QCItemGroup`、Define/Include/Deduction Form、wsQC/Core，新增 `/YMES/QC/PmiItemGroup` 三區 Vue DEV 頁與選單路由。Oracle source DDL 覆蓋 PMI 主/明細/扣除基準及三張 SPC 刪除依賴；PostgreSQL CREATE 參考使用 JSONB 表示 Oracle 動態要因欄位。家用端需比對 live schema、實作 QC216 API/Issue Job/WIP 引用檢查與安全 FactorOnline 白名單。
- QC201「計數品管項目」：[功能規格](modules/QC/計數品管項目.md)。已依主/Define/Reason Form、QC_ItemOp 共用 Form 與 WinForms Service/Core 建立 `/YMES/QC/AttributeQcItem` DEV 頁、三層選單及主/子資源 API handoff。Oracle source DDL 涵蓋主表、直接子表與刪除流程關聯；PostgreSQL CREATE 參考只列 4 張直接維護子表，主表沿用 MTL 共用 SQL。家用端需先核對 YMES live schema，再實作 API、Issue Job、DataStamp 與引用檢查。
- QC112「品質原因產品指導書」：[功能規格](modules/QC/品質原因產品指導書.md)。公司端依主畫面、Maintain/Import 表單、Oracle DDL 完成 Vue DEV 雙 Grid、產品對應維護、附件匯入 UI 與 API handoff。Oracle 關聯表以 `(ReasonNo, ProductNo)` 為 PK、無 FK/DataStamp；舊 DataImporter checker 未包含在來源樹，檔案共用/刪除規則及 YMES 檔案儲存整合待家用端查證。
- QC110「特殊主檔維護」：[功能規格](modules/QC/特殊主檔維護.md)。公司端依 `frmSpecialBasisMaintain` 與共用 SYS/QC Service/Core 完成選列即載入的 metadata 動態 Grid DEV 頁、QC 基本資料選單及 API handoff。其 `ParameterClass=0`，與 QC109 class=1 分別授權；沿用 QC109 已查的 `TBLSYSMESPARAMETER`/`FC_%` Oracle DDL 及 PostgreSQL CREATE 參考，不重複建立 DDL。家用端需核對 YMES schema，實作 FunctionKey `QC110` 保護的安全動態讀寫 API。
- QC109「要因資料維護」：[功能規格](modules/QC/要因資料維護.md)。公司端依 `frmQCFactorMaintain`、SYS/QC Service/Core 與 Oracle DDL 完成 metadata 驅動的動態 Grid DEV 頁、Import/Paste/整表 Save、QC 基本資料選單及 API handoff。來源以 `TBLSYSMESPARAMETER` 選出核准的 `FC_%` Table；本次字典查到三張 FC 表，沒有讀業務列。家用端需核對 YMES 既有參數目錄/動態表，再提供限制 `QC109` 權限的參數化整表 API。
- QC108「計量品管法則基本資料」：[功能規格](modules/QC/計量品管法則基本資料.md)。migration0036 與登入保護 API／Web 已部署；正式實測七類 System 新增、Apply 模式／編輯、多選取消／確定、核准2、停用-1／硬刪及 NULL stamp stale409 後重載。RuleSerial PK／RuleNo 分離與 nullable CAS 保留；FUNQR、Issue Control／完整 Issue Job 仍待來源與整合，詳見模組驗收界線。
- QC107「計數判別法則基本資料」：[功能規格](modules/QC/計數判別法則基本資料.md)。migration0035 主／明細及0037共用AQL空表已套用，登入保護 aggregate API／Web 已部署。正式實測空 aggregate CRUD／複製／多選刪除、缺metadata503、原明細NULL／audit保留及child snapshot stale409；無來源DataStamp，使用YMES aggregate hash。不曾成功新增明細或核准，Action／ReasonType／UserDefineAQL、Issue Control／Job 仍待交付。
- QC106「品質原因基本資料」：[功能規格](modules/QC/品質原因基本資料.md)。公司端已對照 WinForms Form/Def、wsQC、Core、Oracle DDL，完成 `/YMES/QC/ReasonBasis` DEV 頁與 QC 基本資料選單。來源主表 ReasonNo 為 PK，只有 PK unique index、無 FK；家用端需核對 YMES schema、提供 MES ValueList ReasonType、同類且已核准 QC105 子類別 lookup，並實作 CRUD、Issue Control、nullable DataStamp CAS 與 `REASON BASIS` Issue Job。
- QC105「品質原因子類別資料」：[功能規格](modules/QC/品質原因子類別資料.md)。公司端已對照 `frmReasonSubTypeBasis`/Def、wsQC、Core 與 Oracle DDL，完成 `/YMES/QC/ReasonSubTypeBasis` DEV 頁和 QC 基本資料選單。來源表以 ReasonSubType 為 PK，沒有 FK；家用端需比對既有 schema、提供正式 ReasonType ValueList，並實作 QC105 CRUD、Issue Control 編輯權限及 `ReasonSubType BASIS` Issue Job。
- QC104「抽樣計劃_元件」：[功能規格](modules/QC/Wafer抽樣計劃.md)。公司端依 `frmSamplingPlan_Wafer`/Def、wsQC 與 Core 完成 `/YMES/QC/SamplingPlanWafer` DEV 頁和 QC 基本資料選單。Oracle source DDL / PostgreSQL CREATE 參考包含 Wafer 主檔、共用 Range、Area 三表；Range 無 PK/index/FK，家用端需先協調和 QC103 共用表的 schema/API 行為，再補 QC104 transaction CRUD、DataStamp CAS 及 `WAFER SAMPLING PLAN` Issue Job。

- QC103「抽樣計劃」：[功能規格](modules/QC/抽樣計劃.md)。migration0034 與登入保護主檔／參數交易 API／Web 已部署；正式實測已知0／1／4／5類型、參數／範圍、核准及state2編輯／停用／硬刪、合法PLAN-TYPES等主鍵路由及NULL／0 CAS。QC301分類／原Core／FUNSA／部分SYS選項、Simulation與完整Issue Job仍未完成，不以四類管理通過代表抽樣引擎完成。


- QC102「抽樣計劃設定_1916」：[功能規格](modules/QC/抽樣計劃設定_1916.md)。公司端已對照 WinForms Form/Def、wsQC、`clsQCBasis`/`clsBasis_UPD`，完成 `/YMES/QC/Sampling1916` DEV 頁與 QC 基本資料選單。Oracle source DDL/PostgreSQL 空表 CREATE 參考已建立；唯一索引為 SamplingType/MinNum/MaxNum/SLevel，沒有宣告 PK constraint、FK 或簽核。家用端需核對 YMES schema 並實作 QC102 CRUD、DataStamp CAS 及抽樣解析規則。 家用端2026-11-19：API/schema及上述模組文件所列正式頁面操作已驗證；具體未完成項以模組最新驗收紀錄為準。


- QC101「抽樣表格」：modules/QC/抽樣表格.md。公司端已完成 Vue DEV 頁 /YMES/QC/SamplingTable、三欄查詢、區間 CRUD、單列/整表複製及 QC 基本資料選單；PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 TBLQCSAMPLINGTABLE DDL/index/constraints，建立來源 DDL 與 PostgreSQL 空表 CREATE 參考。家用端需先比對 YMES schema/migrations，再實作 QC101 CRUD、DataStamp CAS 與 CopyTable 部分成功 API；沒有簽核或 lookup。 家用端2026-11-19：API/schema及上述模組文件所列正式頁面操作已驗證；具體未完成項以模組最新驗收紀錄為準。


- QC401「主分類基本資料」：[功能規格](modules/QC/主分類基本資料.md)。公司端已完成 `/YMES/QC/MClassBasis` DEV 頁；重用 QC406 已查得的 `TBLQCMCLASSBASIS` DDL/CREATE。匯入器原碼已找到並記錄第一工作表/前兩欄/逐編號部分成功規則；官方 template binary 未找到，需由家用端接 YMES template storage。 家用端2026-11-19：API/schema及上述模組文件所列正式頁面操作已驗證；具體未完成項以模組最新驗收紀錄為準。
- QC402「次分類基本資料」：[功能規格](modules/QC/次分類基本資料.md)。公司端已完成 `/YMES/QC/SClassBasis` DEV 頁、QC402 路由/選單、已簽核主分類 lookup 及匯入器 UI。Oracle source DDL 和 PostgreSQL 空表 CREATE 參考已由公司端受保護 Oracle 連線直接查得；主鍵僅 SClassNo，沒有 FK。家用端需比對 live schema/migrations，實作 QC402 CRUD、`SCLASS BASIS` Issue Job 簽核、MClass lookup 與 `.xls/.xlsx` template/import API。 家用端2026-11-19：API/schema及上述模組文件所列正式頁面操作已驗證；具體未完成項以模組最新驗收紀錄為準。

- QC403「異常原因基本資料」：[功能規格](modules/QC/異常原因基本資料.md)。公司端已完成 `/YMES/QC/HoldItemBasis` DEV 頁；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。`TBLQCHOLDITEMBASIS` 以 HoldItemNo 為 PK，只有 PK unique index，無 FK/額外索引；家用端需比對 schema，實作 QC403 CRUD、HoldSystem/分類/群組/OCAP 查找、`HOLDITEM BASIS` Issue Job 與 Excel 匯入 API。

- QC404「異常說明」：[功能規格](modules/QC/異常說明.md)。公司端已依 WinForms 建立 DEV 頁 `/YMES/QC/HoldDescription`；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。來源主鍵是 HoldDescriptionID，另有 MClassNo 非唯一索引、沒有 FK；家用端需比對 YMES schema 並實作 `HOLD DESCRIPTION` Issue Job 核准流程。
- QC405「異常處理群組」：[功能規格](modules/QC/異常處理群組.md)。公司端 Vue DEV 頁 `/YMES/QC/HoldDispGroup` 包含群組主檔、GroupType 5 主處理人員與 GroupType 6 CC 人員；沿用已查的 `TBLQCHOLDDISPGROUPBASIS`、`TBLUSRUSERGROUP` DDL/CREATE 參考，無新增資料表；家用端需實作群組/成員原子更新及 `HOLD DISP GROUP` Issue Job。
- QC406「第一處理群組」：[功能規格](modules/QC/第一處理群組.md)。公司端已依 WinForms 建立 DEV 頁 `/YMES/QC/FirstGroup`；主表與查找表 Oracle source DDL、PostgreSQL 空表 CREATE 參考位於 [`database/reference`](../database/reference/) 下的 `mestest_qc_first_group*_*.sql`。來源主表是 `(MClassNo, OPNo)` 複合 PK，沒有 FK；家用端需比對 YMES schema 並實作 Issue Job 核准與查找端點。
- QC407「最後處理群組」：[功能規格](modules/QC/最後處理群組.md)。公司端已依 `frmFinalGroup`/Service/Core 建立 DEV 頁 `/YMES/QC/FinalGroup`；Oracle source DDL 與 PostgreSQL 空表 CREATE 參考已建立。來源主表也是 `(MClassNo, OPNo)` 複合 PK，沒有 FK；Issue Job Map 的來源識別字串為 `Final GROUP`。
- QC111「8D 原因基本資料」：[功能規格](modules/QC/8D原因基本資料.md)。公司端已接 WinForms 雙 Grid DEV 頁 `/YMES/QC/8DReasonBasis`；[Oracle source DDL](../database/reference/mestest_qc_8d_reason_source_ddl.sql) 與 [PostgreSQL 空表 CREATE 參考](../database/reference/mestest_qc_8d_reason_create.sql) 已建立。兩張來源表只有 PK unique index，沒有 FK；家用端需比對 YMES schema 並補 API/Issue Job 核准流程。

## 已確認的第一層模組

以下清單已由公司端 PowerShell 5.1 唯讀查詢 MESTEST.TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION 取得。第一層名稱依 WinForms 的 ModuleNo + Module 翻譯鍵，從 Customer/Production/MESClient/mesresource.zh-cht.resources 核實。詳細功能編號、分類、FormName 及 ExecutionFile 見 [MES Oracle 選單與功能目錄](MES_MENU_FUNCTION_INVENTORY.md)。目錄資料也不等同使用者實際可見權限，WinForms 另以使用者權限篩選。

| 順序 | ModuleNo | 第一層名稱（已核實者） | MenuItem | Function |
| ---: | --- | --- | ---: | ---: |
| 1 | ENT | 客戶資料 | 10 | 8 |
| 2 | EQP | 設備資料 | 22 | 18 |
| 3 | SMD | 區域資料 | 9 | 8 |
| 4 | MTL | 物料資料 | 10 | 8 |
| 5 | QC | 品質資料 | 49 | 44 |
| 6 | OP | 作業站資料 | 12 | 10 |
| 7 | PRS | 流程資料 | 5 | 4 |
| 8 | PRD | 產品資料 | 41 | 37 |
| 9 | OE | 工單管理 | 16 | 13 |
| 10 | WIP | 在製品管理 | 98 | 112 |
| 11 | EMS | 設備管理 | 28 | 26 |
| 12 | SPC | 品質管理 | 37 | 34 |
| 13 | INV | 庫存管理 | 44 | 38 |
| 14 | PM | 維修保養管理 | 47 | 42 |
| 15 | RPT | 報表管理 | 83 | 74 |
| 16 | IEW | 預警管理 | 6 | 5 |
| 17 | USR | 使用者管理 | 17 | 14 |
| 18 | SYS | 系統管理 | 52 | 47 |
| 19 | ISU | 簽核管理 | 9 | 7 |
| 20 | AUT | 自動執行 | 6 | 4 |

### 標準 WinForms 原始碼模組盤點（候選清單）

以下是 `STD/MESWin` 第一層原始碼目錄及 `frm*.vb` 數量，只用來安排後續搜尋。這些目錄不等於正式 `TBLSYSMODULE` 菜單列，也不能據此推斷第一層顯示名稱、排序或使用者可見權限。

| 原始碼目錄候選 | Form 數 | 原始碼目錄候選 | Form 數 |
| --- | ---: | --- | ---: |
| AUT | 8 | BR | 85 |
| DT | 7 | EMS | 59 |
| ENT | 20 | EQP | 70 |
| FMA | 19 | IEW | 20 |
| INV | 81 | ISU | 18 |
| MSA | 10 | MTL | 25 |
| OE | 32 | OP | 36 |
| PM | 68 | PRD | 144 |
| PRS | 21 | QC | 153 |
| RPT | 143 | SMD | 25 |
| SPC | 135 | SYS | 125 |
| USR | 35 | WIP | 355 |

Oracle 目錄共有 20 個 Module、601 筆 MenuItem、553 筆 Function；MenuType=1 分類 82 筆，MenuType=0 功能項 519 筆，其中 518 筆能對應 TBLSYSFUNCTION。完整來源目錄與 ModuleOrder 已保存至 MES_MENU_FUNCTION_INVENTORY.md。STD/MESWin Form 數仍是輔助盤點值，下一步逐筆依 FunctionNo/FormName/ExecutionFile 對照原始碼和 WinForms 畫面，並用實際權限確認可見性。

## 現有核心主檔遷移狀態

| ModuleNo | 已有畫面 | 規格與來源 DDL | 家用端 Production API |
| --- | --- | --- | --- |
| ENT | 客戶基本資料 ET101 DEV 頁面 | [客戶基本資料規格](modules/ENT/客戶基本資料.md)；3 張來源表 DDL/空表 CREATE SQL 已交付 | YMES schema/API/匯入尚待家用端 |
| ENT | 客戶連絡人資料 ET102 DEV 頁面 | [客戶連絡人規格](modules/ENT/客戶連絡人資料.md)；沿用 ET101 已查的 `TBLENTCUSTOMERCONTACTOR` DDL/CREATE SQL | YMES API 待家用端；不新增重複資料表 |
| ENT | 客戶出貨地址資料 ET106 DEV 頁面 | [客戶出貨地址規格](modules/ENT/客戶出貨地址資料.md)；出貨地址沿用 ET101 DDL，報關表已另查 DDL/CREATE SQL | 地址與報關 API、查找選項待家用端 |
| ENT | 報關行基本資料 ET103 DEV 頁面 | [報關行基本資料規格](modules/ENT/報關行基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、DataStamp 與簽核 API 待家用端 |
| ENT | 貨運行基本資料 ET104 DEV 頁面 | [貨運行基本資料規格](modules/ENT/貨運行基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、已簽核報關行查找與簽核 API 待家用端 |
| ENT | 取貨公司基本資料 ET105 DEV 頁面 | [取貨公司基本資料規格](modules/ENT/取貨公司基本資料.md)；已由 Oracle 直連核對 DDL，補 PostgreSQL 空表 CREATE SQL | Production CRUD、DataStamp 與簽核 API 待家用端 |
| ENT | 外包商基本資料 ET201 DEV 雙 Grid 頁面 | [外包商基本資料規格](modules/ENT/外包商基本資料.md)；兩張 Oracle 來源 DDL/PG 空表 CREATE SQL 已建立 | YMES schema 比對、雙表 CRUD/交易與 Issue/Job 核准 API 待家用端 |
| ENT | 外包項目設定 ET202 DEV 單 Grid 頁面 | [外包項目設定規格](modules/ENT/外包項目設定.md)；Oracle 來源 DDL/PG 空表 CREATE SQL 已建立 | YMES schema 比對、CRUD/DataStamp 與已核准外包流程查找 API 待家用端 |
| EQP | 設備設定七項 DEV 頁面 | 設備設定功能規格已分頁；多張設備表已查 DDL | 待逐功能完成 |
| SMD | SD101 工廠、SD103 儲位、SD104 區域、SD105 區域庫房、SD106 過帳電腦之區域、SD107 生產線別、SD108 區域設備狀態監控、SD109 工位基本資料 DEV 頁面；SMD 第一層三層選單列出 8 個功能 | [SD101](modules/SMD/工廠基本資料.md)、[SD103](modules/SMD/儲位基本資料.md)、[SD104](modules/SMD/區域基本資料.md)、[SD105](modules/SMD/區域庫房基本設定.md)、[SD106](modules/SMD/過帳電腦之區域.md)、[SD107](modules/SMD/生產線別基本資料.md)、[SD108](modules/SMD/設備狀態監控_區域.md)、[SD109](modules/SMD/工位基本資料.md)；來源 Oracle DDL 與 PostgreSQL 空表 CREATE 已交付 | SD101、SD103–SD109 Production API/schema 待家用端 |
| OP | 作業站主檔及簡化 DEV 子功能 | [作業站主檔規格](modules/OP/作業站主檔.md)；18 張作業站相關表來源 DDL/CREATE SQL 已交付 | 待補子功能 API |
| MTL | 物料主檔與 DEV 子功能；ML101、ML102、ML104、ML105、ML106、ML107、MLB01 畫面/規格已建立 | [物料主檔](modules/MTL/物料主檔.md)、[ML101](modules/MTL/物料供應商基本資料.md)、[ML102](modules/MTL/物料類別資料.md)、[ML104](modules/MTL/物料群組資料.md)、[ML105](modules/MTL/物料類別品管項目設定.md)、[ML106](modules/MTL/物料品管項目設定.md)、[ML107](modules/MTL/物料標籤對應設定.md)、[MLB01](modules/MTL/來料AutoLoader格式檔設定.md)；MLB01 Oracle DDL/PG CREATE 已交付 | MTL API 部分部署；ML101、102、104、105、106、107、MLB01 Production API/schema 依 `todolist_homepc.md` 待家用端完成 |
| PRS | 流程主檔 | 既有主檔規格與程式待逐項核對 | 部分主檔 API 已有，擴充待補 |
| PRD | [產品主檔](modules/PRD/產品主檔.md)、[RecipeGroup 指派](modules/PRD/產品RecipeGroup.md)、[配件設定](modules/PRD/產品配件設定.md)、[產品作業區域](modules/PRD/產品作業區域設定.md)、[產品子作業](modules/PRD/產品子作業設定.md)、[產品程式指派](modules/PRD/產品程式指派.md)、[產品預設設定](modules/PRD/產品預設設定.md)、[產品標準工時](modules/PRD/產品標準工時.md)、[產品品管項目](modules/PRD/產品品管項目設定.md)、[產品包裝設定](modules/PRD/產品包裝設定.md)、[產品設備能力項目](modules/PRD/產品設備能力項目.md)、[產品電子作業指導書](modules/PRD/產品電子作業指導書.md)、[產品設備參數規格](modules/PRD/產品設備參數規格.md)、[產品 APS 整合](modules/PRD/產品APS整合.md) | 基本表單及流程、BOM、產品屬性、Parameter、Business Rule、OPDescription、RecipeGroup、Accessory、Resource、SubOP、Program、Default、RunTime、QC Item 指派、Packing Set、EQPCapItem、eSOP、EquipmentSpec 與 APS Integration 已完成 DEV 主流程 | 正式標籤引擎、QC Import／QC Chart 與 Production API/schema 待接續 |

PD102 的 [產品 Bin 定義](modules/PRD/產品Bin定義.md) 已完成 DEV 主流程、Oracle metadata 和 CREATE SQL 交接。

[產品 GBOM 匯入／匯出](modules/PRD/產品GBOM匯入匯出.md) 已完成 DEV 選檔與備份下載，工作簿解析和資料匯入待接入。

[產品資料匯入](modules/PRD/產品資料匯入.md) 已完成 DEV XLSX 預覽與主檔新增；正式流程／子表與範本服務待接入。

[產品流程展開](modules/PRD/產品流程展開.md) 已完成一般／網狀流程的 DEV 唯讀結果與共用 schema 交接。

[PD101 產品類別資料](modules/PRD/產品類別資料.md) 已完成 DEV 清單、CRUD、狀態操作及流程／屬性／參數；與 PD102 DEV 共用核准類別目錄。

[PD103 製程模組資料](modules/PRD/製程模組資料.md) 已完成 DEV 主檔、版次、流程與子頁、XLSX 主檔匯入和 SQL handoff。

[PD104 產品內容查詢](modules/PRD/產品內容查詢.md) 已完成 DEV 條件查找、交集篩選與唯讀結果表格。

[PD105 產品參數群組](modules/PRD/產品參數群組.md) 已完成 DEV 群組版次、明細維護及核准目錄，供 PD102／PD103 查找。

[PD106 生產時間限制](modules/PRD/生產時間限制.md) 已完成 DEV 站與站限制、Hold 選取、狀態操作及三表 SQL 交接。

[PD107 標籤設定](modules/PRD/標籤設定.md) 已完成 DEV 主檔、明細、欄位值預覽及 SQL handoff。

[PD108 產品標籤對應設定](modules/PRD/產品標籤對應設定.md) 已完成 DEV 對應、PD107 目錄查找、批次預設與 SQL 交接。

[PD109 產品作業站預設](modules/PRD/產品作業站預設.md) 已完成 DEV 產品查詢、範圍、批次預設／補缺項與共用表交接。

[PD110 產品作業站修正](modules/PRD/產品作業站修正.md) 已完成獨立 DEV 選單及固定六項補缺流程。

[PD111 產品作業站批次複製](modules/PRD/產品作業站批次複製.md) 已完成 DEV 來源／目標版次、旗標與作業參數關聯複製。

[PD112 包裝規則設定](modules/PRD/包裝規則設定.md) 已完成 DEV 版次、層級、序號／標籤／配件及七表交接。

[PD113 產品包裝規則對應設定](modules/PRD/產品包裝規則對應設定.md) 已完成獨立 DEV 路徑、共用對應編輯與使用中規則交接。

[PD114 自定義流程卡](modules/PRD/自定義流程卡.md) 已完成主檔、欄位明細、DEV 批號欄位預覽與報表相依交接。

[PD115 產品流程卡對應設定](modules/PRD/產品流程卡對應設定.md) 已完成 DEV 對應維護、PD102／PD114 候選與家用端交接。

[PD116 產品品管項目規格匯入](modules/PRD/產品品管項目規格匯入.md) 已完成 DEV 範本、51 欄 XLSX 預覽、本機套用與 SPC 寫入交接。

[PD117 GBOM 批次匯入](modules/PRD/GBOM批次匯入.md) 已完成多檔預檢、流程連線預覽、本機套用與客製流程及 BOM 交接。

[PD202 FT 測試程式](modules/PRD/FT測試程式.md) 已完成 DEV 程式版次、Bin 匯入及檔案管理交接。

[PD204 電性抽樣規則](modules/PRD/電性抽樣規則.md) 已完成 DEV 抽樣模式、Bin 排序、狀態和家用端交接。

[PD206 產品抽樣頻率規則](modules/PRD/產品抽樣頻率規則.md) 已完成 DEV 範圍鍵、三種頻率、班別及家用端交接。

[PD901 產品作業站標工維護](modules/PRD/產品作業站標工維護.md) 已完成 DEV 查詢、XLSX 工時批次與轉 SAP 預覽交接。

[產品作業站 Overstay 維護](modules/PRD/產品作業站Overstay維護.md) 已完成 DEV 查詢、XLSX 預檢、歷史與多列刪除交接。

[產品作業站法則維護](modules/PRD/產品作業站法則維護.md) 已完成 DEV 法則清單、新增、批量替換／刪除及 XLSX 交接。

[PDP01 晶圓良率與出貨報告維護](modules/PRD/晶圓良率與出貨報告維護.md) 已完成 DEV 報告規則、來源站點、停用與四表交接。

[PDP02 晶圓測試程式定義](modules/PRD/晶圓測試程式定義.md) 已完成 DEV 版次、核准、Bin 設定和 XLSX 匯入交接。

[PDP03 產品晶圓法則管制管理](modules/PRD/產品晶圓法則管制管理.md) 已完成 DEV 摘要、明細、XLSX 範本與匯入交接。

[PDP04 產品晶圓圖疊圖規則設置](modules/PRD/產品晶圓圖疊圖規則設置.md) 已完成 DEV 測前／測後主從規則、核准、批次複製與三表交接。

[PDB01 挑片規則設定](modules/PRD/挑片規則設定.md) 已完成 DEV 挑片主檔、同類倉庫、條件工具與資料字典交接。

[PDB02 產品設定_封測](modules/PRD/產品設定封測.md) 已完成 DEV 封測入口、ECN／TECN／SEN 流程與 Oracle 六表交接。

[PDB04 檔案上傳基本資料](modules/PRD/檔案上傳基本資料.md) 已完成 DEV 檔案類型、FTP 欄位預檢、核准與資料字典交接。

[PDB05 測試程式檔案管理](modules/PRD/測試程式檔案管理.md) 已完成 DEV 程式與檔案設定共用、檔案更新歷程及雙表交接。

其餘 15 個 Module 的 476 個 Function 尚未逐一遷移。先以 MES_MENU_FUNCTION_INVENTORY.md 為工作目錄，再按 ModuleOrder、MenuNo、FunctionNo 追 WinForms Form/Service、相關表、API、Create SQL 及 DEV 畫面。

## 設備資料 → 設備設定

以下順序依使用者提供的 WinForms 選單畫面：

| 功能 | WinForms / 資料來源狀態 | Web 規格狀態 |
| --- | --- | --- |
| 設備供應商基本資料 | Form、Designer 與 Service 已對照；Oracle DDL 已查 | [規格](modules/EQP/設備設定/設備供應商基本資料.md)與 Vue 已依 WinForms 對齊；供應商/聯絡人 API 已部署 |
| 設備工程群組資料 | Form、Designer、Service 與 GroupType=2 成員規則已對照；兩張來源表 DDL 已查 | [規格](modules/EQP/設備設定/設備工程群組資料.md)及 Vue UI 已依 WinForms 補正；YMES schema 比對及 API 待家用電腦 |
| 設備類別資料 | WinForms/Service、5 張設備類別相關表及配件目錄表 Oracle DDL、FunctionNo/FunctionKey 已核實 | [規格](modules/EQP/設備設定/設備類別資料.md)與 Vue Development UI 已依 WinForms 補齊主/子表單套用、稽核與工程群組/圖片選擇器；家用端待比對 schema 並實作 API |
| 配件類別資料 | ACC TYPE / EP202；frmAccessoryType、定義、壽命、屬性及匯入原始碼已核對；4 張來源表 DDL/PK 已由 PowerShell 直連 Oracle 查得 | [規格](modules/EQP/配件/配件類別資料.md)與 Vue DEV 頁已建立；Excel/舊 Checker 簡化已記錄，Production API/schema 比對待家用端 |
| 配件分類資料 | ACC CATEGORY / EP201；frmAccessoryCategory/Def/Import 與 Service 狀態規則已核對；主表 DDL 沿用 EP202 依賴檔 | [規格](modules/EQP/配件/配件分類資料.md)與 Vue DEV 頁已建立；CSV 暫代 Excel，API/schema 待家用端 |
| 配件分類狀態定義設定 | ACC CATEGORY STATE / EP204；Form/Def、狀態/介面選項、InitialFlag 唯一性與簽核刪除規則已核對；兩張 Oracle DDL 已直連查得 | [規格](modules/EQP/配件/配件分類狀態定義設定.md)、PG 空表 CREATE SQL 與 Vue DEV 彈窗/獨立頁已建立；API/schema 待家用端 |
| 配件狀態設定 | EQP ACC STATE / EP205；Form/Def、CRUD/Copy/Apply、欄位驗證與既有狀態表 DDL 已核對 | [規格](modules/EQP/配件/配件狀態設定.md)、Vue DEV 頁已建立；既有表 schema 比對及 API 待家用端 |
| 配件基本資料 | ACC BASIS / EP203；主/Def、配件屬性、匯入及 Service 原碼已核對；六張 Oracle DDL 與八個 index 已由 PowerShell 直連查得 | [規格](modules/EQP/配件/配件基本資料.md)、Oracle 原始 DDL/PG CREATE SQL 與 Vue DEV 頁已建立；YMES schema/API 待家用電腦 |
| 設備基本資料 | WinForms 主/編輯/Chamber Form、屬性、參數、QC 關聯、匯入 Form 與 Service 均已追 | [設備基本資料](modules/EQP/設備設定/設備基本資料.md)、[設備屬性](modules/EQP/設備設定/設備屬性設定.md)、[設備參數](modules/EQP/設備設定/設備參數設定.md)、[設備點檢表關聯](modules/EQP/設備設定/設備點檢表關聯.md)、[設備匯入](modules/EQP/設備設定/設備資料匯入.md)規格已建立；設備子功能 DEV Vue 已接線；設備主表與參數表原始 DDL 已由使用者提供並轉成單表 CREATE SQL；其他未匯出關聯表仍待查 |
| 設備群組資料 | WinForms/Service、主從表與兩張 SMD 區域映射表 Oracle DDL/索引已核對 | [規格](modules/EQP/設備設定/設備群組資料.md)與 PostgreSQL 空表 CREATE SQL 已建立；Vue UI 已補齊環境限定假資料、Apply 與稽核；YMES schema 比對及 Production API 待家用電腦 |
| 設備狀態設定 | WinForms / Service、主表及兩張刪除引用表 Oracle DDL 已追；完整 ValueList/繁中翻譯已核實 | [規格](modules/EQP/設備設定/設備狀態設定.md)與 Vue UI 已對齊；Production CRUD/options API 已部署，家用端待建立引用表 migration/SELECT 權限並啟用精確刪除檢查 |
| 設備點檢表設定 | WinForms 主從 Form / Designer / Service、主從 Oracle DDL、FunctionNo/FunctionKey、QCType ValueList 已核實 | [規格](modules/EQP/設備設定/設備點檢表設定.md)與 Vue DEV 畫面/互動已按來源補齊；Production API 待家用電腦 |
| 載具分類資料 | EP301；WinForms Form/Def/Designer、wsEQP/clsCarrierBasis、Oracle DDL 與 IssueState/簽核規則已核實 | [規格](modules/EQP/載具/載具分類資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 載具類別資料 | EP302；WinForms Form/Def/Designer、wsEQP/clsCarrierBasis、Oracle DDL 與 UseLayer/簽核規則已核實 | [規格](modules/EQP/載具/載具類別資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 載具基本資料 | EP303；frmCarrierBasis/Def、wsEQP/clsCarrierBasis、主表/WIP 狀態/廠區 DDL 與交易/簽核規則已核實 | [規格](modules/EQP/載具/載具基本資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL 與 Vue DEV UI 已建立；Production schema/API/核准流程待家用電腦 |
| 設備參數資料 | EP401；frmEQPRecipeBasis/Def、雙清單與 Recipe Detail/Copy/Version/簽核 Service 規則已核對；兩張 Oracle DDL、索引及 Type ValueList 已直連查證 | [規格](modules/EQP/設備設定/設備參數資料.md)、Oracle 原始 DDL/PG 空表 CREATE SQL、bootstrap 修正與 Vue DEV UI 已建立；Production schema/API/簽核待家用電腦 |

## 物料資料 → 物料設定

| 功能 | WinForms / 資料來源狀態 | Web 規格狀態 |
| --- | --- | --- |
| ML101 物料供應商基本資料 | Form/Service/Core、主從表 DDL 已核對 | [規格](modules/MTL/物料供應商基本資料.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML102 物料類別資料 | Form/Service/Core 與 MTL 主表/屬性/QC/GumHour 結構已核對 | [規格](modules/MTL/物料類別資料.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML104 物料群組資料 | 主從 Form/Service/Core、主表 Oracle DDL 已核對 | [規格](modules/MTL/物料群組資料.md)、Oracle DDL/PG CREATE 與 Vue DEV 頁已建立；Production API 待家用端 |
| ML105 物料類別品管項目設定 | Form/Service/Core、資料同步與 IQC 副作用已核對 | [規格](modules/MTL/物料類別品管項目設定.md)與 Vue DEV 頁已建立；Production API 待家用端 |
| ML106 物料品管項目設定 | Form/Service/Core、catalog 與 bind log 結構已核對 | [規格](modules/MTL/物料品管項目設定.md)、Oracle DDL/PG CREATE 與 Vue DEV 頁已建立；Production API 待家用端 |
| ML107 物料標籤對應設定 | Form/Service/Core；TBLMTLLABELMAP 與 TBLPRDLABELBASIS Oracle DDL 已由 PowerShell 直連核對 | [規格](modules/MTL/物料標籤對應設定.md)、兩張表 CREATE 參考與 Vue DEV 頁已建立；Production API 待家用端 |

## 工單管理 → 訂單

- [OE101 訂單基本資料](modules/OE/訂單基本資料.md) 已完成公司端 DEV 操作與 Oracle DDL、家用端 API／schema 交接。
- [OE102 訂單結案](modules/OE/訂單結案.md) 已完成公司端 DEV 多選結案與家用端 API 交接，共用 OE101 表結構。

## 工單管理 → 工單

- [OE201 工單基本資料](modules/OE/工單基本資料.md) 已完成公司端 DEV 日常維護、核准及發放流程，交付五張 Oracle 表結構與 API／schema 參考。
- [OE202 工單刪除](modules/OE/工單刪除.md) 已完成公司端 DEV 多條件查詢、批次選取、刪除／停用和原 Core 守門條件交接。
- [OE203 工單查詢](modules/OE/工單查詢.md) 已完成公司端 DEV 多條件查詢、六種工單狀態與屬性查看，沿用 OE201 DDL。
- [OE204 工單達交率（準時開工）](modules/OE/工單達交率_準時開工.md) 已完成 DEV 開工差異、查詢、總筆數及準時／提前／延遲統計。
- [OE205 工單達交率（準時完工）](modules/OE/工單達交率_準時完工.md) 已完成 DEV 完工率與平均生產天數，沿用 OE201 DDL。
- [OE206 工單用料清單](modules/OE/工單用料清單.md) 已完成 DEV 用料、替代料、模擬發料及兩張表的 Oracle DDL。
- [OE207 工單存料現況](modules/OE/工單存料現況.md) 已完成 DEV 存料與元件唯讀檢視及元件表 DDL。
- [OE208 工單結案](modules/OE/工單結案.md) 已完成 DEV 待結案查詢、多選結案與正式 WIP／ERP 守門規格。
- [OE209 工單元件資料維護作業](modules/OE/工單元件資料維護作業.md) 已完成 DEV 暫存增刪、取號示範與 Oracle 異動紀錄 DDL。

## 工單管理 → 行業包

- [OEB01 工單挑片資料查詢](modules/OE/工單挑片資料查詢.md) 已完成 DEV 主表篩選、挑片明細、測項動態欄位及 Oracle 交接。
- [OEP01 工單開立_晶片測試](modules/OE/工單開立_晶片測試.md) 已完成 DEV 晶片測試工單、挑片與元件規則，以及半成品關聯表 Oracle 交接。

## 全 MES 盤點狀態

- Oracle 第一層 Module 清單、排序及功能目錄筆數已查；ModuleNo 翻譯標題與實際使用者權限仍待核實，553 個 Function 的 WinForms/API 遷移狀態尚需逐項追蹤。
- 每發現一個 Function，新增或更新對應 Markdown，並在本索引登記 Form、主要資料表、API 狀態及待確認事項。
- 本索引及功能規格需與 UI/API 變更同批更新，供公司與家用電腦共同開發。


- QC202「計數管制內容」：modules/QC/計數管制內容.md。公司端依 frmSPCAttLimit/Def、wsQC、clsQCBasis/clsBasis_UPD 完成 /YMES/QC/AttributeControlLimit DEV 頁、三層選單及 API handoff。Oracle TBLSPCATTLIMIT source DDL 沿用 QC201 直接 PowerShell/Oracle.ManagedDataAccess 查詢成果；來源無 PK/FK，Factor 欄依 FactorOnline 動態增加。PostgreSQL CREATE reference 以 JSONB 保存要因組合並明確標示為 YMES 適配，家用端需核對 live schema 後再設計 migration/API。
- QC203「計數品管項目判別法則對應」：[功能規格](modules/QC/計數品管項目判別法則對應.md)。公司端已閱讀四個 WinForms Form、wsQC/Core，使用 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得三張 MESTEST source DDL，完成 /YMES/QC/AttributeRuleMap Vue DEV 頁與 Create SQL/API handoff；家用端待核 live schema、ID service、實作 transaction API 與 Issue Job。
- QC204「刪除計數型品管項目量測記錄」：[功能規格](modules/QC/刪除計數型品管項目量測記錄.md)。依 WinForms、wsQC/Core 完成 /YMES/QC/AttributeMeasurementDelete Vue DEV 頁與 API handoff；所需 Oracle DDL 已在 QC201 source reference，功能不新增表，家用端須核對 PostgreSQL 現有量測 log schema 並提供交易刪除 API。
- QC210「刪除計量型品管項目量測記錄」：[功能規格](modules/QC/刪除計量型品管項目量測記錄.md)。依 WinForms、wsQC/Core 完成 Vue DEV 頁與 API handoff；計量量測 log Oracle DDL 沿用 QC205/201 reference，新增 PostgreSQL schema-only Create 參考，家用端需比對 YMES 現有 schema 後補交易刪除 API。
- QC211「二次檢驗抽樣計畫」：[功能規格](modules/QC/二次檢驗抽樣計畫.md)。已對照 Form/Define、Designer、wsQC/Core；PowerShell 5.1 + Oracle.ManagedDataAccess 直接唯讀取得 TBLQCSAMPLINGFOR2NDINSP DDL/唯一索引 metadata，新增 Vue DEV 頁與 PostgreSQL Create/API handoff。家用端待核對 live schema、解決 DataStamp Service/Core 落差並實作 QC211 API。
- QC212「計量品管項目群組」：[功能規格](modules/QC/計量品管項目群組.md)。已對照 THEIL BU1 主/Define/Memo 與共用子視窗、wsQC/Core；新增 Vue DEV 主檔/明細頁及 PostgreSQL CREATE handoff。群組主檔、Memo DDL 沿用既有 Oracle 直查 reference；新增缺少的群組明細及計算腳本 CREATE 參考。家用端待核對 live schema 並實作 API。
- QC213「計數品管項目群組」：[功能規格](modules/QC/計數品管項目群組.md)。已對照 Attribute 群組 Form/Define、共用 QC_ItemGroupInclude/Op、wsQC/Core，新增 Vue DEV 頁、主/明細及硬刪依賴 Oracle source DDL、PostgreSQL CREATE/API handoff。家用端待核對 live schema 並實作 QC213 API。

- QC205「計量品管項目」：[功能規格](modules/QC/計量品管項目.md)。THEIL BU1 WinForms 主/Define/Memo Form、wsQC/Core 已核對；已接入 /YMES/QC/VariableQcItem Vue DEV 頁及三層選單。Memo 的 Oracle 表與變數量測依賴 DDL 已由 PowerShell 5.1 + Oracle.ManagedDataAccess 直連取得，並提供 Memo PG Create 參考；家用端待核 live schema、建置主/子資源/Issue Job API 與參數 JSONB 適配。
- QC209「計量品管項目管制法則對應」：[功能規格](modules/QC/計量品管項目管制法則對應.md)。標準 QC_ItemRule Form/Service/Core 已核對；Vue DEV 頁已接三層選單。Oracle `TBLQCITEMRULE` DDL 沿用 QC205 前次 PowerShell/ODP.NET 直連擷取，QCRuleBasis 沿用 QC108；新增 QC209 PostgreSQL CREATE 參考與整組替換 API handoff，家用端待核 live schema/實作 API。

- QC409「OCAP異常單處置調整」：[功能規格](modules/QC/OCAP異常單處置調整.md)。直接核對 WinForms Form/流程圖選擇 Form、wsWIP/clsLot 業務交易，取得 3 張 OCAP WIP 表及 11 張共用 ERF/WIP side-effect Oracle DDL；Vue DEV 頁與專用表 PG CREATE/API handoff 已完成。此頁是生產異常單處置交易，不是三表 CRUD；Production API 需保留 LotStamp、ERF final disposition、WIP 狀態及 extension/通知副作用。
- WP308「載具管理_元件」：[功能規格](modules/WIP/載具管理_元件.md)。依 frmCarrierManagement／CarrierIn／CarrierChange、wsEQP／wsWIP／wsINV 與 clsLot 完成雙 Grid、載具放入／整載具卸載／更換載具的 Vue DEV；資料與 WP302 共用。重用 WP302、WP101、CarrierBasis/Type 已查證表，沒有新增 Oracle table 或 CREATE-only；家用端待核對 PostgreSQL live schema 並完成登入保護的 lookup、POST command 及一致交易。
- WP303「設備耗品現況」：[功能規格](modules/WIP/設備耗品現況.md)。依 frmEQPConsumeStatus、Define 與 WP_EQPConsumeReset 外掛、wsWIP/wsEQP、clsWIP/clsEQPBasis 完成 Vue DEV 主頁、維護／設備挑選／重置視窗。Oracle DDL、metadata 與 PostgreSQL CREATE-only 包含兩張專用表；ConsumeBasis/EQP Basis 沿用既有 reference。家用端待核對 live schema，再實作列表、維護及原子耗品重置 command。
- WP304「設備耗品歷程」：[功能規格](modules/WIP/設備耗品歷程.md)。依 frmEQPConsumeStatusLog、wsWIP/wsEQP 與 clsWIP 復刻設備／耗品／時間區間查詢及七欄唯讀 Grid。重用 WP303 已查證的 `TBLWIPEQPCONSUMELOG`，沒有新增 Oracle table、DDL 或 CREATE-only；家用端待以安全分頁 GET API 讀取既有 log table。
- WP305「設備線上點檢」：[功能規格](modules/WIP/設備線上點檢.md)。依 frmEQPCheckQCList、wsEQP/wsWIP、clsWIP/clsLot 復刻設備 lookup、候檢／補驗、QCType 0–3 量測與點檢結果；Oracle source DDL/metadata/Index DDL 及 PostgreSQL CREATE-only 涵蓋 Log、Detail、Task 與三張下機／狀態副作用表。NG 狀態切換與生產下機交易交由家用端串接共用 WIP command。
- WP306「設備點檢歷程」：[功能規格](modules/WIP/設備點檢歷程.md)。依 WinForms／Service／Core 完成設備、日期、點檢單與結果查詢、Header/Detail 雙 Grid；沿用 WP305 已查證 Log/Detail 表，不新增 DDL。
- WP307「設備狀態監控_區域」：[功能規格](modules/WIP/設備狀態監控_區域.md)。共用 SMD SD108 Base Area 畫布，依 WP_AreaShow／LoginLogout／OperatorState／OperatorLog 與 wsSMD/clsWIP 完成區域登入登出、State/History 篩選；新查 `TBLSMDAREARELATION` 已有 CREATE reference，新增狀態與歷史兩表 Oracle DDL/metadata 及 PostgreSQL CREATE-only；家用端需做原子登入登出 API。
- WP203「過帳設備設定」：[功能規格](modules/WIP/過帳設備設定.md)。依 `frmEquipmentFavorite`/`frmEquipmentFavoriteDef`、`wsEQP`、`clsEQPBasis` 完成工作站收藏頁 CRUD/複製、設備類別雙 Grid、多選移動、最多 15 台與重名取代；`TBLEQPFAVORITE` 沿用 WP204 source DDL/CREATE-only，設備主檔/類別也沿用既有 reference，沒有新增 Oracle DDL。
- WP103「列印流程卡」：[功能規格](modules/WIP/列印流程卡.md)。依 `frmBRRunCard_General`、`wsWIP`、`clsLot`、`wsPRD`、`clsProduct` 完成 DEV Lot 查詢、多選預覽、Current Process/PS/All、Include Log 與模擬印表機／預覽；預設 RunCardStyle=0 使用假報表，產品 RunCardMap/PD_Kit 為家用端待移植分支。WP103 無新增專用表或 DDL。
- WP113「LOT SHEET PRINT」：[功能規格](modules/WIP/LOT_SHEET_PRINT.md)。依 THEIL 專用 Form、`wsWIP_RunCard`、`tcWIP.clsRuncard` 分別復刻 MO／WO 建立日期查詢、流程選擇、Lot Sheet 預覽/列印及空白版本。DEV 路徑 `/YMES/WIP/LotSheetPrint`；重用既有 WIP Lot Property 與 THEIL Matrix 表，未新增專用表或 DDL。
- WORKORDER PRINT「工令單列印」：[功能規格](modules/WIP/工令單列印.md)。依 THEIL Form/inline Designer、`wsWIP.LoadWorkorder`／`LoadMOTypeDesc`／`LoadLotDetail`、`clsLot` 與 ActiveReport 完成工單／建立日期篩選、批次勾選、同 MO 守門、數量彙總及 DEV 工令單預覽。清冊未提供 FunctionKey；重用既有 Oracle references，未新增 DDL。家用端需確認正式 ACL 對應並提供唯讀查詢／預覽。
- Skip Inspection Setting「免檢批設定」：[功能規格](modules/WIP/免檢批設定.md)。核對 THEIL Form／新增與狀態視窗、`wsWIP`／`clsLot` 的查詢、新增、狀態切換與 WP201 執行檢查，完成 Lot×OP、原因必填及狀態往返 DEV。重用 `XX_TBLSPCWAIVE` Oracle DDL／CREATE-only reference，沒有查業務資料或新增 DDL；家用端需核 Function ACL 並接入 WP201 的 Status=0 原因查詢。
- WPP07「Bump MAP 疊圖作業」：[功能規格](modules/WIP/Bump_MAP疊圖作業.md)。核對 IC/CP Form、`wsWIP`／`wsPRD`／`wsSYS`、`clsLot`／`clsProduct`／`clsSYSEng`；完成 Lot/元件/檔案狀態、規則與模板檢視、選取、Merge／UndoFrozen 假資料互動。重用既有 PRD Merge MAP Rule、ComponentState／Basis refs；六張 CP MAP state/file/template 表缺 Oracle metadata，未猜 CREATE-only；實際 MAP binary／FTP 僅記錄為 DEV 簡化。
- WP621「入庫至 E10ICD ERP」：[功能規格](modules/WIP/入庫至E10ICD_ERP.md)。依 Form／Designer、`wsERP_E10ICD`、`wsMES`、`clsERP`、`clsWIP`、`clsINV` 核對 MO／Lot 候選、ERP 建單→MES 入庫、W 警告、失敗補償與預約釋放；完成 DEV 假資料流程頁。沿用 WP605／WP201／WP603、Lot／MO／產品／OP／Locator refs；明確缺 `TBLINVFGDINBIN` Oracle DDL／metadata，受保護 Oracle profile 不在此桌機，未猜 CREATE-only。家用端須核對 live schema、交易原子性、ACL 與 ERP 補償命令。
- WP622「不良品入庫至 E10ICD ERP」：[功能規格](modules/WIP/不良品入庫至E10ICD_ERP.md)。依 Scrap Stock In Form／inline Designer、`wsERP_E10ICD`、`wsMES`、`clsERP`、`clsWIP`、`clsINV` 核對 Source 0/1/2、LotSerial 整列入庫、錯誤原因／Component／FailBin、ERP→MES 補償與 BookingFlag；完成 DEV 假資料流程頁。沿用 WP201／WP603／WP215 及既有 QC／PRD／MO references；明列缺 `TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` Oracle DDL／metadata，未猜 CREATE-only。家用端須核對 live schema、原子性、來源預約鎖及 ERP 取消補償。
- WP623「取消入庫 E10ICD ERP」：[功能規格](modules/WIP/取消入庫E10ICD_ERP.md)。依取消 Form／inline Designer、`wsINV`、`wsERP_E10ICD`、`clsInventory` 核對 state=2+ERPNo 守門、FGD／SCRAP RequestType、ERP code=0、MES 庫存回復及 State 1／-2／-1；完成 DEV 假資料整單取消與 ERP／MES 成敗情境。重用 WP201／WP603／WP605／WP215；缺 `TBLINVFGDINBIN` Oracle DDL／metadata 沿 WP621，並記錄 `OrgState` 被舊流程逐步覆寫與 State=-2 恢復需求。家用端須核查 live schema、取消冪等、外部 unknown outcome 及庫存回復交易。
- WP616「入庫至 T100 ERP」：[功能規格](modules/WIP/入庫至T100_ERP.md)。依 Form／Designer、`wsERP_T100`、`wsMES`、`clsWIP`／`clsINV` 核對 T100 Sheet Type、成品／線邊庫、MO／整列 Lot 選取、BookingFlag、ERP `code=W` 警告續行、MES Core 數量／元件／Lot 更新與 ERP 失敗補償；完成 DEV 假資料頁。沿用 WP201／WP605 Oracle／CREATE refs，未新增缺表或 CREATE-only；記錄現行整合未傳 `auto=N` 及 ERP cancel／BookingFlag compensation gap。家用端需核實 ERP default、live schema、冪等與對帳。
- WP620「取消入庫 T100 ERP」：[功能規格](modules/WIP/取消入庫T100_ERP.md)。依 Form／Designer、`wsINV`、`wsERP_T100`、`clsInventory` 核對 State=2／ERPNo 檢核、未篩 ERP source 的查詢缺口、FGD／SCRIN RequestType 0／1、ERP code=0 及 MES 庫存回復狀態；完成 DEV 假資料取消與 ERP／MES 失敗情境。沿用 WP201／WP603／WP605／WP215 references；`TBLINVFGDINBIN` Oracle metadata 缺項沿 WP621／WP623。家用端需加 ERP source guard、冪等取消、unknown-outcome 對帳與 State=-2 恢復。
- WP609「入庫至 TipTop ERP」：[功能規格](modules/WIP/入庫至TipTop_ERP.md)。依 Form／Designer、TipTop `CreateStockInData`、EAI `wsMES`、`clsWIP`／`clsINV` 核對 Sheet Type、MO／Ref Lot、雙庫別、部分數量、`BookingFlag`、Component、包裝 Qty、ERP Undo 及 MES 庫存／結批／產品操作；完成 DEV 假資料頁。重用 WP605／WP201 refs，沒有新增 Oracle table／DDL。記錄 Good Bin active Core 未找到更新路徑、ERP Undo 失敗仍可能釋放 BookingFlag、部分入庫缺 reservation；家用端處理 Bin 規則、ERP unknown outcome／undo reconciliation 與交易原子性。
- WP610「取消入庫至 TipTop ERP」：[功能規格](modules/WIP/取消入庫至TipTop_ERP.md)。依取消 Form／Designer、`wsINV`、TipTop `UndoCreateStockInData`、`clsInventory` 核對 State=2／ERPNo、MOState=99、FGD／SCRIN Core 回復、狀態 1／-2／-1；完成 DEV 假資料取消與跨 ERP source guard。沿用 WP201／WP603／WP605／WP215 references；`TBLINVFGDINBIN` metadata 缺項沿 WP621／WP623。家用端需做 TipTop source guard、冪等、unknown-outcome 對帳及 State=-2 恢復。
- WP614「入庫至 WorkFlow ERP」：[功能規格](modules/WIP/入庫至WorkFlow_ERP.md)。依主 Form／Designer、ERP 重拋 Form／Designer、EAI `wsMES`、`clsWIP`／`clsINV` 核對 ALLOWINV2ERP 0／1／2、MO／WIP Semi-Scrap、多 MO 整列入庫、ERP XMLAdapter、State=1 重拋票及 ERP failure MES undo；完成 DEV 入庫與重拋互動。重用 WP201／WP603／WP605／OE／PRD references；缺 `TBLINVWIP_SCRAP`、`TBLOEMOMATERIALINBASIS_ERP`、`TBLERPRETHROWTICKET` Oracle metadata，無可用受保護 profile，未猜 CREATE-only。記錄 ALLOWINV2ERP=2 SheetType 解析缺陷及 Loss-only Core 分流缺口。
- WP619「取消入庫 WorkFlow ERP」：[功能規格](modules/WIP/取消入庫WorkFlow_ERP.md)。依取消 Form／Designer、`wsINV`／`kcINV.clsInventory`、EAI `wsMES`／`clsINV` 核對 State=2／ERPNo 清單、WorkFlow `ReferenceCase=4` Undo、FGD／SCRIN 庫存回復及 State 1／-2／3／-1；完成 DEV 假資料取消與跨 ERP source guard。重用 WP201／WP603／WP605 refs；`TBLINVSCRINVENTORY`、`TBLINVSCRINVENTORYCOMPONENT`、`TBLINVSCRINVENTORYFAILBIN` Oracle metadata 沿 WP622 缺項。記錄查詢無 ERP source filter、ERP timeout 被舊 Form 當明確失敗、MES 回復失敗停在 State=-2，以及無條件覆寫狀態風險。
- WP608「盤點調帳」：[功能規格](modules/WIP/盤點調帳.md)。依主／三個調帳 WinForms、`wsWIP` 與 `clsINVCount` 完成六種 WIP／SEMIWIP／FGD／SEMI／RAW／MTLWIP 的 DEV 查詢、調帳暫存、Confirm、部分失敗情境及報表預覽。記錄 MTL 的 `MaterialLotNo` request typo、單位換算及逐筆非原子更新風險。Oracle source DDL／metadata 缺項且桌機無受保護 Oracle profile；家用端唯讀查 YMES PostgreSQL catalog 確認盤點 state/detail 四表均不存在、未找到可辨識 migration-history table，沒有查業務列或執行 DDL/DML。目前尚缺 Oracle DDL 與 RAW／MTLWIP 目標表映射，故未新增 migration/API。
- WP607「列印盤點清冊」：[功能規格](modules/WIP/盤點清冊列印.md)。依本機 Form／內嵌 Designer、`wsWIP.LoadInvCountStateWithDetail`、`clsINVCount` 和 General／MTL／PRD ActiveReports 核對 State=0 六類盤點清冊、Grid 欄位／排序、Barcode 預設勾選及六種 barcode/no-barcode 版型；完成 DEV 假資料查詢、版型切換與列印預覽。無新增表；沿用 WP608 所列四張盤點 state/detail metadata 缺項，不產生重複 CREATE SQL。正式 Web 列印版型、條碼與頁面排版尚需家用端決定可部署的報表方案。
- WP606「盤點關／開帳」：[功能規格](modules/WIP/盤點關開帳.md)。依本機 Form／內嵌 Designer、`wsWIP`、`clsINVCount` 和 `clsSYSParameter` 核對 State=0／1 生命週期、六種關帳快照、`AllowExecuteLot` 系統級過站鎖及未確認調帳守門；完成 DEV 關帳／開帳與失敗復原情境。四張盤點 state/detail Oracle metadata 沿 WP608 缺項、且家用端唯讀 PostgreSQL catalog 核對均不存在；沒有產生推測 SQL。家用端須把 State、snapshot、全域過站鎖納入交易／恢復流程。
- BARTENDER「Bartender print」：[功能規格](modules/WIP/Bartender列印.md)。依 THEIL Form／內嵌 Designer、預覽 Form、ApiClient／ApiExecResult 核對 LOT → 標籤選項 → 動態列印資料 → 預覽／送印及 TAG_FILE_MAP 設定。完成純 DEV 標籤選擇、假資料、checkbox 批次選擇、預覽／列印確認及對照編輯。可讀 checkout 僅有 consumer ApiClient，未找到 InLabel／LabelApi server handler 或對應 SQL/Core；TAG_FILE_MAP Oracle DDL 缺項且桌機無受保護 Oracle profile，未連 Oracle／YMES PostgreSQL、未猜 CREATE-only，未下載或執行 vendor 二進位檔。家用端須核實 endpoints／schema／權限並定義部署列印 provider。
- WIPUNINV「取消入庫作業」：[功能規格](modules/WIP/WIPUNINV取消入庫.md)。依 THEIL Form／Designer／WEBAPI consumer 核對自動查詢、單選 Grid、Outbound payload、欄位改名及 `GRSNO` 預設；完成 DEV 假資料 Search／Outbound 成功失敗／逾時對帳。可讀來源未含 controller／Service／Core 或 SAP transaction，也沒有直接 Oracle source table/SQL；未連 Oracle／YMES PostgreSQL、未猜 DDL。家用端接續核對正式 endpoint、授權、冪等與逾時 reconciliation。
- WPP06「Bump MAP 檔案管理」：[功能規格](modules/WIP/Bump_MAP檔案管理.md)。依 THEIL 主／子 Form、`wsWIP`／`wsSYS`、`clsLot`／`clsSYSEng`／`clsMergeMAPHandler` 核對兩個查詢條件、State／History、template encoding 與 MAP parse、FTP upload／backup／Scrap／download。完成純 DEV template/file fixtures、多選、逐檔結果及失敗對帳。四張來源表 Oracle DDL／metadata 缺項且桌機受保護設定檔不可用，未連 Oracle／YMES PostgreSQL、未猜 CREATE-only；家用端接續核 live schema、API 與 File Service 一致性。
- WPP01「生產批開立_晶片測試」：[功能規格](modules/WIP/生產批開立_晶片測試.md)。依 CP 主／定義 Form、`IC_CP_PKG.wsWIP`／共用 Service、`MEStc_IC_CP_PKG.clsLot` 核對 MOState 3/6、wafer／領料分支、隱藏 Process 頁、Component／Property／OP ECN／Carrier、逐筆 Delete／Release；完成獨立 DEV 頁。沿用 WP101／WP207／WP215／WP302 與 OE DDL references，沒有新增 SQL；`TBLWIPLOTBASIS` 舊 reference 版本差異待目標環境核實。桌機無受保護 Oracle profile，未連 Oracle／YMES PostgreSQL；API/schema handoff 已交家用端。
- WPP02「晶圓針測資料匯出」：[功能規格](modules/WIP/晶圓針測資料匯出.md)。依 THEIL `frmWIPWaferExport`（Designer 內嵌）、`IC_CP_PKG.wsWIP`／`wsSYS`、`clsLot`／`clsSYSEng` 與 `clsWaferExportHanlder` 核對 Lot／Ship MAP 查詢、Basis wildcard precedence、FunctionFlag 0／1、Wafer 多選、Bin Define／Summary、Detail／Header 對應、檔名 token、MAP／文字／Excel／ZIP 與 FTP。完成 DEV fixtures、查詢／模式切換、檔案 manifest 與錯誤情境；不建立本機檔案或上傳。三張 Wafer Export 設定表及 CP Wafer MAP/Bin 同版 Oracle DDL／metadata 缺項；桌機受保護 profile 不存在，未查 Oracle／YMES PostgreSQL，未猜 CREATE-only。原始 FunctionOrder 未保存在可讀清冊，WPP02 依 FunctionKey 數值先交付；WPP02、WPP04、WPP08 均已完成 DEV／規格／handoff，待 profile 可用時再核實精確排序。
- WPP03「設備設置單維護_晶片測試」：[功能規格](modules/WIP/設備設置單維護_晶片測試.md)。依 THEIL CP 主／ADD-EDIT-CLOSE Form、VIEW Form、`IC_CP_PKG.wsWIP`、`clsLot` 核對狀態 0／1／2／99、Setup serial、PROBER／TESTER、Accessory Release、實物配件確認、設備 Link State／Log 與結單。完成 fallback `/YMES/WIP/EqpSetupBasisCp` 的 DEV 查詢及 ADD／EDIT／VIEW／CLOSE 流程。WPP03 專屬 Setup／Accessory／Equipment Link tables 缺同版 Oracle DDL／metadata；serial、WIPTemp equipment、EMS accessory state 沿用 WP109／WP201／accessory references。桌機受保護 profile 不可用，未連 Oracle／YMES PostgreSQL，沒有猜 CREATE-only；家用端 handoff 已交接。WPP01–WPP08 CP 功能目前均已完成公司端 DEV／規格／handoff；各 FunctionOrder 原值仍待安全 profile 核實。
- WPP04「晶圓針測紀錄查詢」：[功能規格](modules/WIP/晶圓針測紀錄查詢.md)。依 THEIL `frmProbingLog`（Designer 內嵌）、`IC_CP_PKG.wsWIP.LoadProbingLog_Ext`、`clsLot.LoadProbingLog_Ext` 核對 Customer／Lot／MO 必填其一、OP 多選、Component、EndTestTime、Final／History、動態 Bin 1+／Bin 101+。完成 DEV 唯讀假資料查詢與篩選、Bin 動態欄、Final/History、Customer／OP lookup。Core ComponentNo filter 會重複累加既有 `strCondition`；DEV 按預期條件查詢，家用端 handoff 列明修正。`tbl_CPWIPWAFERBINSUMMARY`／`tbl_CPSYSBin`／`tbl_PWIPWaferBin_Ext` 缺同版 Oracle DDL／metadata；桌機受保護 profile 不存在，未查 Oracle／YMES PostgreSQL、未猜 CREATE-only。
- WPP08「生產批自主產生元件」：[功能規格](modules/WIP/生產批自主產生元件.md)。依 THEIL `frmLotComponentGen`／`frmNewComponent` 與 Designer、CP `wsWIP.UpdComponent`／`clsLot.UpdComponent`、共用 `LoadMOBasis`／`LoadLotBasisJoinState`／`LoadComponentState`／`LoadComponentBasis`／`LoadProductProcess`／`LoadUnitTransferQty`／`LoadSerialConfig`／`GetComponentNo_Multiple` 核對 MOState 3／6、LotState 0、既有 Component 阻擋、HaveComponent／Enterprise gate、單位換算、Wafer ID、GoodQty 守恆及三表新增。完成 fallback `/YMES/WIP/LotComponentGen` DEV fixtures、序號模式、Unit conversion、逐列 GoodQty／Wafer ID 編輯及 Confirm。Core 新增 `TBLOEMOCOMPONENT`、`TBLWIPCOMPONENTBASIS`、`TBLWIPCOMPONENTSTATE`；分別沿用 OE201／WP215／WP101 refs，未新增 CREATE-only。受保護 Oracle profile 不可用，本次未查 MESTEST／YMES PostgreSQL；家用端須核 live schema、序號唯一與三表交易。原 Form 只檢查第一筆的部分數值及流程首筆 HaveComponent，DEV 增加逐筆驗證並列明來源差異。
- WPP05「Golden MAP 檔案管理」：[功能規格](modules/WIP/Golden_MAP檔案管理.md)。依 THEIL 主／Update 子 Form、IC_CP_PKG.wsWIP、clsLot 核對 DeviceNo 被 ProductNo 覆蓋的來源缺陷與 Device／Product／UploadTime 的 State／History 查詢、Org File Update／Assign Product Info、逐檔替換及備份、Scrap、下載。完成 DEV fixtures、產品 lookup、檔名驗證、State／History 多選與 upload／backup／scrap／download 模擬。來源表 TBL_PWIPGoldenMAPFileState／TBL_PWIPGoldenMAPFileLog 缺同版 Oracle DDL／metadata，桌機受保護 profile 不存在；未連 Oracle／YMES PostgreSQL、未猜 CREATE-only。共享 TBLPRDPRODUCTBASIS 沿既有 references；家用端須先核 live schema，再建正式 API 與 File Service reconciliation。
- WP104「使用者登入作業（上工）」：[功能規格](modules/WIP/使用者登入作業_上工.md)。依 `frmOperatorClockIn`、`wsUSR`、`wsOP`、`wsWIP_OperatorState` 與 `clsOperatorState` 完成 DEV 模擬驗證、班別日期、OP／Area／Equipment 地點及自動下工提示。DEV 路徑 `/YMES/WIP/OperatorClockIn`。公司端 Oracle 唯讀查得 `TBLWIPOPERATORSTATE`／`TBLWIPOPERATORLOG` source DDL 與 CREATE-only；Live PG schema 待家用端核對。
- WP105「使用者登出作業（下工）」：[功能規格](modules/WIP/使用者登出作業_下工.md)。依 `frmOperatorClockOut`、`wsUSR`、`wsWIP_OperatorState`、`clsOperatorState` 完成 DEV 假資料批次選取與逐列下工；沿用 WP104 OperatorState／OperatorLog 與 WP305 LotLoginState Oracle references，沒有新增 table/DDL。DEV 路徑 `/YMES/WIP/OperatorClockOut`；Production API 和 live PG schema 交家用端核對。
- WP106「使用者登入（上工）資訊」：[功能規格](modules/WIP/使用者登入資訊.md)。依 `frmOperatorOnline`、Designer、`wsWIP_OperatorState`/`clsOperatorState` 完成 OP/Area/Equipment 多選並集查詢、七欄 Grid、勾選逐列 SignOut、外部唯讀入口；與 WP104/WP105 共用 DEV State/Log。無新直接表，OperatorState/Log DDL/PG CREATE-only 沿 WP104，LotLoginState 沿 WP305；正式 API 交家用端。
- WP109「特殊生產需求維護」：[功能規格](modules/WIP/特殊生產需求維護.md)。依 `frmWIPSWRBasis`／Def、`wsWIP`／`clsWIP` 完成七條件、四狀態、14 欄 JOIN Grid、右側按鈕、ADD/EDIT/Copy/Approve/Void 與假附件。公司端唯讀取得 SWR Basis/Detail/Serial 三表 Oracle 原 DDL/index/metadata，交 `database/reference/mestest_wip_wp109_create.sql` PG CREATE-only；正式交易、附件、EventLog、serial lock 與原程式缺陷修正由家用端接手。
- WP401「外包單開立」見 [功能規格](modules/WIP/外包單開立.md)。Vue DEV 支援主明細、刷入批號、回貨站、增修、刪除和作廢。Oracle OS Basis、OS Detail 和 FutureOS 沿用 WP207／WP201 references。
- WP402「外包單確認」見 [功能規格](modules/WIP/外包單確認.md)。Vue DEV 共用開立資料，補 FutureOSLog 與 ERP 設定表 Oracle 結構。
- WP403「外包回貨」見 [功能規格](modules/WIP/外包回貨.md)。Vue DEV 支援分批回貨及良品／報廢／其他量，補兩張 Oracle 表的 DDL。
- WP404「取消外包出貨 ERP」見 [功能規格](modules/WIP/取消外包出貨ERP.md)。DEV 可將合格的臨時外包單轉回未確認；正式 ERP／Reverse 分支已交接。
- WP901「回復處理」見 [功能規格](modules/WIP/回復處理.md)。DEV 已交付歷程與回復守門，三張補表以 Oracle metadata 整理 CREATE-only；正式跨批回放及 SAP 由家用端接續。
- WP902「刪除已下線生產批」見 [功能規格](modules/WIP/刪除已下線生產批.md)。DEV 完成雙 Grid、回復與刪除守門；五張補表的 Oracle metadata、CREATE-only 和正式交易已交接。
- WP903「批次刪除已下線生產批」見 [功能規格](modules/WIP/批次刪除已下線生產批.md)。DEV 已交付批次過濾、勾選與部分成功結果，正式交易重用 WP902。
- WP904「生產批作業區變更」見 [功能規格](modules/WIP/生產批作業區變更.md)。DEV 已交付流程節點／區域 override，正式整批替換與 LotState 交易使用既有表。
- WP905「產品版本變更」見 [功能規格](modules/WIP/產品版本變更.md)。DEV 已交付基礎批版本查詢與變更，正式 API 需一併提交對應回復歷程。
- WP906「ECN 設定精靈」見 [功能規格](modules/WIP/ECN設定精靈.md)。DEV 已交付六步與受影響批選擇，正式建立交易沿用 WP201 三張表。
- WP907「生產批數量調整」見 [功能規格](modules/WIP/生產批數量調整.md)。DEV 已交付整批／Bin／元件分流，調整歷程與原因表附 Oracle metadata 及 CREATE-only。
- WP916「生產批強制結批作業」見 [功能規格](modules/WIP/生產批強制結批作業.md)。DEV 已交付單批結批，正式 EndOP／工單用料及清理交易與補表 metadata 已交接。
- ES204「改變設備狀態」見 [功能規格](modules/EMS/改變設備狀態.md)。依 THEIL Form、標準 Service/Core 完成 DEV 設備狀態、互斥、原因、PM 工單守門及生產批綁定；原因歷程表已唯讀查 metadata 並提供 CREATE-only。家用端需核對 live schema，實作具序號並發保護及整個設備／Lot／人員副作用的原子 API。
- ES102「設備狀態控制設定」見 [功能規格](modules/EMS/設備狀態控制設定.md)。依標準 Form／Designer、wsEQP／clsEQPStateControl 完成三分頁、七控制項及 DEV lookup／Apply／Confirm；重用既有 TBLEQPSTATECONTROL Oracle DDL／CREATE-only。家用端需比對 live schema 並以交易原子更新已修改設定群組。
- ES409「設備狀態期間統計查詢」見 [功能規格](modules/EMS/設備狀態期間統計查詢.md)。依標準 Form、wsRPT 與 clsReport 完成日期／設備／狀態篩選、區間分鐘統計及 Chamber 投影 DEV；所需 Oracle DDL/metadata 重用既有狀態歷程、狀態基礎、設備、Chamber 與區域參照。家用端接續登入保護的參數化唯讀 API。
- ES106「設備狀態變更原因資料」見 [功能規格](modules/EMS/設備狀態變更原因資料.md)。依 Form/Def、wsQC 與 clsQCBasis 完成 EQP 原因主檔 CRUD、狀態篩選、複製、核准／停用 DEV；重用已核實原因及子類別 Oracle DDL/metadata。家用端需比對 live schema、修復 Service/Core DataStamp 來源差異並以交易實作簽核／狀態 API。
- WP915「生產批產品變更作業」見 [功能規格](modules/WIP/生產批產品變更作業.md)。DEV 已交付聯產品查找及逐批換料號，正式 ERP／ECN／流程重掛待家用端。
- WP913「生產批屬性調整」見 [功能規格](modules/WIP/生產批屬性調整.md)。DEV 已交付批與元件屬性 CRUD，沿用既有直接表。
- WP912「生產批作業站製程特性值調整」見 [功能規格](modules/WIP/生產批作業站製程特性值調整.md)。DEV 已交付批及元件特性調整；Oracle 稽核表 metadata／CREATE-only 已交接。
- WP911「生產批作業站用料記錄調整」見 [功能規格](modules/WIP/生產批作業站用料記錄調整.md)。DEV 已交付報工用料主明細及加改刪，正式用料聚合及並發規則已交接。
- WP909「換廠作業」見 [功能規格](modules/WIP/換廠作業.md)。DEV 已交付群組守門與換廠，沿用現有來源表。
- WP908「標籤列印」見 [功能規格](modules/WIP/標籤列印.md)。DEV 已交付多批對應與預覽，列印歷程附 metadata／CREATE-only。
- WP112「工單撤派工」：[功能規格](modules/WIP/工單撤派工.md)。依 THEIL Form/Service/Core/SAP 流程完成兩模式、MOState 3/6 lookup、13 欄 Grid、逐批假 Confirm。公司端唯讀補 MaterialLot、SYSSerialCreateMax、MO OS、ZPPT0025A/B/C1 六表原 DDL/index/metadata，PG CREATE-only 在 `database/reference/mestest_wip_wp112_create.sql`；原實際寫 THSAP DB link，正式 SAP/Log/庫房交易及部分成功處理由家用端接手。
- 「批次作業員上工（Lot Operator Clockin）」：[功能規格](modules/WIP/批次作業員上工.md)。依 `frmLotOperatorClockIn`、`wsWIP_OperatorState` 與 `clsOperatorState` 完成 DEV Lot 查詢篩選、全選、多 mode location 彙整及批次上工；重用 WP104/WP305 Oracle references，無新增 table/DDL。路徑 `/YMES/WIP/LotOperatorClockIn`。Form 原始碼未含數值 FunctionKey，fallback FunctionNo 尚待實際 menu metadata 確認。
- 「批次作業員下工（Lot Operator ClockOut）」：[功能規格](modules/WIP/批次作業員下工.md)。依 `frmLotOperatorClockOut`、`wsWIP_OperatorState` 與 `clsOperatorState` 完成 DEV 查詢、UserNo+LoginPlace 同步勾選及批次假下工；重用 WP104/WP305 references，沒有新 table/DDL。路徑 `/YMES/WIP/LotOperatorClockOut`；FunctionKey/FunctionNo 尚待實際 menu metadata 核實。
- 「作業員登入／登出（OP／Area）」：[功能規格](modules/WIP/作業員登入登出_OPArea.md)。依 `frmOperatorLogin`、`wsUSR`、`clsWorkTime` 完成 Employee/Shift 讀取、OP／Area 選擇、班別 WorkDay、LoginSerial 登入與工時登出 DEV。路徑 `/YMES/WIP/OperatorLogin`。MESTEST 所有可見 Owner/Synonym 都找不到 Core 使用的兩張 USR Login 表，source DDL 尚缺；不得推測 CREATE。
- 「生產批作業員登入／登出（OPERATOR LOGIN_LOT）」：[功能規格](modules/WIP/生產批作業員登入登出.md)。依 `frmOperatorLogin_Lot`、`wsWIP`、`wsWIP_OperatorState`、`clsOperatorState/clsWIP` 完成 CallType=MenuItem 的 DEV；共用 BR CallType 1/2/3 邏輯也已記錄。路徑 `/YMES/WIP/OperatorLoginLot`；重用 WP104/WP305 tables，無新增 Oracle DDL。正式 menu FunctionKey 尚待 metadata 核實。
- WP506「預先異常解除」：[功能規格](modules/WIP/預先異常解除.md)。依 `frmFutureHoldRelease`／`wsWIP.DelFutureHold`／`clsLot.DelFutureHold` 完成四種勾選查詢、九欄 Grid、單列 Release 的 Vue DEV 頁。來源 Form 只檢選取列 LockFlag，Core 卻按 Lot/OP/HoldTime 刪所有同鍵主表與原因，未帶 Module/Serial；DEV 檢查整個刪除範圍，正式 API 須先定精確或批量解除契約。沿用 WP201/WP504 Oracle DDL/PG CREATE，無新直接表。
- WP507「預先等待解除」：[功能規格](modules/WIP/預先等待解除.md)。依 `frmFutureWaitRelease`／`wsWIP.DelFutureWait`／`clsLot.DelFutureWait` 完成三種勾選查詢、六欄 Grid、單筆 Release 的 Vue DEV 頁；與 WP505 共用本機假資料。FutureWait 三鍵 PK 的 DDL/CREATE 沿用 WP201，無新直接表；正式 API 應強制完整三鍵，避免 Core 通用方法缺鍵時擴大 DELETE。
- WP508「異常單資料」：[功能規格](modules/WIP/異常單資料.md)。依 `frmERFBasis`、Service/Core 完成六條件、日期、batch member Lot 查詢、25 欄 Grid；再核 `WP_ERFInformation` 補五頁假資料 Grid、ERF/OCAP 假附件 View/Delete、異常單產生檔保護。Oracle 唯讀補 8D Basis 與 LotDisposition DDL/metadata，PG CREATE-only 補 LotDisposition、OCAP 附件；主 ERF/關聯表沿 QC409/WP refs。無 QRSNo 的 DEV 8D 連 WP801 ADD，已有 QRSNo 的 DEV 8D 連 WP802 維護；真資料/附件 API 待家用端。
- WP801「廠內 8D 矯正單」：[功能規格](modules/WIP/廠內8D矯正單.md)。核 `frmWP8DBasisFactory`／Def、`wsWIP`、`clsWIP`、`clsBasis_UPD`、`clsSYSEng`；Vue DEV 已接四狀態/QRSNo、40 欄 Grid、ADD/EDIT/DEL/Copy/Approve/ERFINFO、ERF→8D 假建立關聯。Oracle 唯讀補兩張流水號規則表 source DDL/metadata，PG CREATE-only 對齊五個漏掉的 Oracle default。真資料、流水號/ERF 原子交易與 Issue Job 待家用端。
- WP802「8D 矯正單維護作業」：[功能規格](modules/WIP/8D矯正單維護作業.md)。核主 Form、十頁明細及子 Form／Service／Core；Vue DEV 已接 38 欄主查詢、十頁明細、D1–D7 假 CRUD、D3 Verify、D7 Track、D8 總結、階段附件及 WP508 直達。Oracle MESTEST 唯讀補 12 張 8D 子表 source DDL/metadata/index，PG CREATE-only 照真鍵建立；TeamMember 只有三欄 unique index 無 PK constraint。正式交易、附件 BLOB、FishBone 細節、WP803 結案流程待接。
- WP803「8D 矯正單結案作業」：[功能規格](modules/WIP/8D矯正單結案作業.md)。核 `WP_8DClose` 主/Def Form、Service/Core，Vue DEV 已接四狀態、五條件、九欄 Grid、結案編輯、D1 團隊查找、多選 Approve／直接及 Pending 分支、8DQuery 唯讀。Release/ReleaseLog/TeamMember/Basis source 沿 WP802/WP508；另從 MESTEST 唯讀取得部門表 DDL/metadata，PG CREATE-only 對齊四個原 bootstrap 遺漏的 default。正式 API 須解決 Pending 被提前結案、Issue SQL 改錯主單表、多選 ActiveRow 錯列與跨命令部分成功；WP804 待接。
- WP804「8D 矯正單查詢」：[功能規格](modules/WIP/8D矯正單查詢.md)。核主 Form/Service/Core；Vue DEV 接跨簽核狀態十組條件、39 欄與 WP802 十頁唯讀，含 ERFNo 外部入口參數。原 Form 單勾 WIPERF/Other 類別會漏送篩選，新查詢按畫面意圖補正；僅勾無效 CloseState 不允許無界查詢。沿 WP508 的 8D Basis Oracle/PG source，無新直接表。
- WP510「等待單資料」：[功能規格](modules/WIP/等待單資料.md)。依 `frmWaitBasis`／`frmWaitInformation`、`wsWIP`、`clsLot` 完成六組勾選查詢、15 欄唯讀 Grid、Wait Info 七欄、WP502 假 Wait 合併。Oracle MESTEST 唯讀取得 `TBLWIPWAITLOTDISPOSITION` 13 欄 source/index DDL 與 metadata；無 PK，WAITNO 非唯一 index，PG CREATE-only 已交。主表沿 WP201、NodeBasis 沿 PRS104，家用端接正式授權分頁 GET。
- WP511「等待處置」：[功能規格](modules/WIP/等待處置.md)。依 THEIL BU1 主 Form／七個分併批子 Form、標準查詢、THEIL 解除 Service/Core 完成 DEV 十種可見選項、17 欄 Grid、查詢/勾選/路由條件與 WP510 假解除結果聯動。Oracle MESTEST 唯讀取得 `XX_TBLWAITPRIV` 14 欄／GROUPNO PK source/index DDL、metadata，交 PostgreSQL CREATE-only；正式端需群組權限、Wait/Lot 原子交易與七種子窗完整邏輯。DEV 分併批子窗已明列為簡化示意。
- WP509「未解除異常單」：[功能規格](modules/WIP/未解除異常單.md)。核 THEIL BU1 主/處置 Form、七個分併批子窗入口及查詢/處置 Service/Core；Vue DEV 六條件、23 欄 Grid、ERF Disp/Abort/Create/Info、狀態分頁、14 個 Lot 處置選項、OCAP/群組守門與簡化子窗。MESTEST 唯讀取得 `XX_TBLQCHOLDPRIV` 18 欄／GROUPNO PK source/index DDL、metadata，交 PG CREATE-only；家用端補正式權限、ERF/Lot/OCAP 原子交易及完整子窗。
- WPI05「配件領用單維護作業」：[來源狀態](modules/WIP/配件領用單維護作業.md)。檢查 `D:\iMES\CSiMES`、THEIL 與 DEV_SILVA 三份來源樹均未找到選單指定 Form／WPI05 專案或直接 Service/Core；設備配件主檔、配件上機及 CP 設置單查詢皆非此功能。來源未補齊前不臆造 UI、交易或 schema；ES402、ES302、ES404、ES403、ES203、ES202、ES104、ES304、ES305、ES103、ES406 已完成公司端交付；ES303「配件壽命維護」已完成；後續依清冊接續。
- ES302「配件下機」：[功能規格](modules/EMS/配件下機作業.md)。依 THEIL／DEV_SILVA／CSiMES 一致的 `frmReturnEQPAccessory`、`wsEMS.ReturnEQPACC` 查找／提交方法、`clsEMSEquipment.UpdEQPACCState_Return` 及 `clsAccState` helpers 完成配件 lookup、狀態互斥／權限／生命提醒與下機 DEV。沿用 accessory-basis／WP201 DDL references；`TBLWIPCONT_ACCESSORY`、`TBLEMSACCVIOLATESTATUSCHANGE` 缺同版 Oracle source DDL／PG CREATE-only，未猜建表。監控入口只傳 AccessoryNo，legacy Form 未補 Version 與設備 context；Core SQL 串接、壽命 helper 可能先於權限獨立更新 TimeCycleStart；正式端依規格反查目前掛載資料並原子下機。
- ES103「配件狀態控制設定」：[功能規格](modules/EMS/配件狀態控制設定.md)。核對三份一致的 `frmEQPACCStateControlSet`、THEIL `wsEQP.LoadEQPACCStateBasis/LoadACCStateControl/UpdACCStateControl` 與 `clsAccBasis`／`clsEQPStateControl`／`clsAccState`；完成 7 項固定流程、`IC_FT_PKG` 下選擇性的 `I_SetupClose` 與 DEV 狀態選擇／Apply／Confirm。沿用 `TBLEQPACCSTATECONTROL`／`TBLEQPACCSTATEBASIS` Oracle DDL 及 CREATE-only；桌機 profile 不可用，本次無須新查 DDL。Legacy 更新會遷移舊狀態配件、寫 log／current／新序號。Form 每次送出整組設定，Legacy Core 也會重寫未變更控制值的配件現況、寫 log 與換發序號；逐列替換設定且沒有涵蓋整批副作用的交易。家用端只處理實際變更的 ControlNo，需參數化並以單一交易回滾完整變更。ES303「配件壽命維護」已完成；後續依清冊接續。
- ES304「配件狀態監控」：[功能規格](modules/EMS/配件狀態監控.md)。核三份一致的 `frmAccessoryStateMonitor`／Designer、`wsEQP`／`wsEMS` 與 `clsEMSEquipment`／`clsAccState`；完成分類／類型查詢、每頁 20 筆卡片、狀態色、互斥停用、DEV 記憶體狀態變更及右鍵／雙擊預覽。沿用 accessory category/type/basis/state/current/log Oracle DDL／CREATE-only；`TBLEMSACCVIOLATESTATUSCHANGE` 與選擇性 reasons 表缺同版 DDL，桌機 profile 不可用，未猜 schema。Core 狀態轉換不重查互斥，正式 API 必須交易內重驗授權／互斥／expected state 並原子記歷程；Apply／Return 走 ES301／ES302 專用流程。ES305 已完成；ES303「配件壽命維護」已完成；後續依清冊接續。
- ES305「配件狀態監控_設備」：[功能規格](modules/EMS/配件狀態監控設備.md)。核三份一致的 `frmAccessoryStateMonitorByEQP`／Designer、`wsEQP.LoadEquipmentType/LoadEquipmentBasis`、`wsEMS.LoadAccessoryStatebyEquipment/ChangeAccessoryState` 與 `clsAccState.LoadAccessoryNoByEquipment/ChangeACCState`；完成設備類別／編號查詢、每頁 20 筆掛載卡片、狀態色、分類互斥、狀態 DEV 變更及右鍵／雙擊預覽。沿用 accessory relation／state 與 equipment basis DDL；`TBLEQPEQUIPMENTTYPE` 缺 Oracle source DDL，`TBLEMSACCVIOLATESTATUSCHANGE` 缺 source DDL／CREATE-only，桌機 profile 不可用。Core 按 AccessoryNo join 多版本 basis/state，正式 API 應用掛載 relation 的 AccessoryNo+Version；需授權、分頁與伺服器端互斥／權限重驗。ES303「配件壽命維護」已完成；後續依清冊接續。
- ES406「配件歷史資料查詢」：[功能規格](modules/EMS/配件歷史資料查詢.md)。核對三份一致的 `frmAccessoryStateLog`、THEIL `wsEMS.LoadAccessoryStateLog/LoadAccessoryReasons` 與 `clsAccState`；完成 AccessoryType／AccessoryNo／UserNo／StartTime 查詢、10 欄歷程 Grid 與按 ACCSERIALNO 載入理由明細 DEV。重用 `TBLEMSACCESSORYSTATELOG`／`TBLEQPACCSTATEBASIS` 既有 Oracle DDL／CREATE-only；`TBLEMSACCESSORYREASONS` 缺 repo DDL／CREATE-only，桌機 profile 不可用，沒有猜 schema。Legacy query SQL 拼接且無分頁，User selector 下載整份 UserSecurity 後才移除密碼欄；正式 API 需最小化欄位、參數化、授權及穩定分頁。ES303「配件壽命維護」已完成；後續依清冊接續。
- ES405「配件狀態查詢」：[功能規格](modules/EMS/配件狀態查詢.md)。公司端已完成 DEV；家用端已核對 live schema、以 `0065_ems_accessory_state_read.sql` 建立缺少的空白 state／life 表並補 `ymes_app` 最小 SELECT 權限，實作登入保護、參數化及穩定分頁 `GET /api/ems/accessory-states`，正式 UI 顯示版本且以 API 讀取。app-browser 驗證空結果及重查，沒有 error/warn；遠端目前無現況資料，未驗證有資料／下一頁。理由子列待 `TBLEMSACCESSORYREASONS` Oracle DDL／metadata；未猜建表。ES303「配件壽命維護」已完成；後續依清冊接續。
- ACC MONITOR_BU1「配件狀態監控_管理」：[功能規格](modules/EMS/配件狀態監控管理.md)。依 DEV_SILVA Customer Form、共用 `wsEQP`／`wsEMS.ChangeAccessoryState` 與 `clsAccState.ChangeACCState` 完成類別／類型選擇、STATES／LOG Grid、列多選及 0–5 狀態變更／歷程 DEV。標準 ES304 Form 是另一功能。現況／歷程／basis／state DDL 重用既有 references；`GETACC_STATE`／`GET_ACCLOG`／`GETACCSTATE_FIELD`／`AccessoryApi.ChangeAccessoryState` REST server source 未在本機來源樹找到，未猜查詢 SQL 或 JSON 欄位。正式端需取得 REST 契約、登入授權、伺服器端 SAP scope、目前版與 expected state 重驗、分頁及明確部分成功結果。ES303「配件壽命維護」已完成；後續依清冊接續。
- ES105「配件狀態互斥設定」：[功能規格](modules/EMS/配件狀態互斥設定.md)。依三份相同 `frmEQPACCStateControl`／內嵌 Designer、`wsEQP.LoadEQPACCStateBasis`、`wsEMS.LoadViolateStateAndShowNowName/UpdACCViolatStateChangeData` 與 `clsAccBasis`／`clsEMSEquipment` 完成類別選擇、彩色 N×N 狀態矩陣、雙擊切換 forbidden pair 及 Confirm DEV 假套用。家用端 app-browser 已驗證 Demo、三層選單、路由、類別、6×6 矩陣、格子切換、Confirm 及 Close；修正 Confirm 成功後需再次編輯才重啟。互斥設定／歷程表 `TBLEMSACCVIOLATESTATUSCHANGE`／`TBLEMSACCVIOLATESTATUSLOG` 缺 Oracle source DDL／metadata 與 PG CREATE-only；未猜 schema。Legacy 逐來源狀態 Delete+Add、無全矩陣交易，名稱也由 client 傳入；正式 API 待 DDL 後以參數化、server-derived 名稱及原子矩陣交易實作。ES303「配件壽命維護」已完成；後續依清冊接續。
- ES402「設備狀態記錄查詢」：[功能規格](modules/EMS/設備狀態記錄查詢.md)。依 THEIL `frmEquipmentStateLog`／Gantt 子窗、`wsEMS.LoadEquipmentStateLog`、`clsEMSEquipment.LoadEquipmentStateLogJoinStateBasis` 完成日期／設備／使用者／狀態篩選、結果欄位及 Gantt DEV。重用 WP305 設備狀態／設備使用關聯 DDL 與 EQP state basis；為既有 Oracle source `TBLWIPLOTLOG` 補 PG CREATE-only。Oracle Core／非 Oracle 分支 Equipment join 欄位不一致，正式 API 需按完整 equipment/start/end 關聯、參數化、授權及分頁；ES302、ES404 與 ES403 已完成公司端交付。
- ES404「設備歷史資料查詢」：[功能規格](modules/EMS/設備歷史資料查詢.md)。依 THEIL 主 Form、Top Ten Grid 與三種圖表子 Form、`wsEMS.LoadEquipmentStateLogTopTen`、`clsEMSEquipment.LoadEquipmentStateLogTopTen` 完成設備／人員／狀態及 Start／End 時間篩選、狀態時數 Top 9+Others 表格與圖表 DEV。沿用 WP305 `TBLEMSEQUIPMENTSTATELOG`、equipment-state basis DDL／CREATE-only；Oracle／非 Oracle 時數精度不同，正式 API 要統一參數化聚合、授權、日期範圍及掃描限制。
- ES403「設備歷史資料查詢_狀態」：[功能規格](modules/EMS/設備歷史資料查詢狀態.md)。依 THEIL `frmEquipmentStateLogByState`／資料表與趨勢圖子窗、`wsEMS.LoadEquipmentStateLogByState`、`clsEMSEquipment.LoadEquipmentStateLogByState` 完成必選狀態、設備／人員與可見 StartTime 篩選、StartTime 日彙總表及帶值長條圖 DEV。Designer 中 EndTime 控制項隱藏，Service/Core 雖有 EndTime 參數分支，一般畫面無法使用。直接表 `TBLEMSEQUIPMENTSTATELOG` 沿用 WP305 Oracle／CREATE-only references，無新 DDL；legacy Oracle 與非 Oracle 小時計算精度不同，正式 API 應統一計算、參數化、授權及日期／結果限制。
- ES203「設備狀態監控_區域」：[功能規格](modules/EMS/設備狀態監控區域.md)。依三份一致的 Customer `frmAreaShowEMS_THEIL_BU1`、`wsSMD.LoadBaseArea/LoadAreaRelationDefine`、`wsEMS.LoadEquipmentStateBySMD` 與 Core 完成區域關聯圖、Area/Object 色彩模式、設備現況色、Zoom/Grid/Refresh 與 DEV 假節點 drilldown。沿用 SD108／設備狀態現有 Oracle／CREATE-only references；`TBLEQPGROUPDETAIL` repo 只有 CREATE-only、缺同版 Oracle source DDL，受保護 profile 不可用，本次無法補查。正式 GET 必須參數化並授權 BaseArea／Equipment，外部視窗在 DEV 只以對話框簡化預覽。ES202、ES203、ES104、ES304、ES305 均已完成，ES303「配件壽命維護」已完成；後續依清冊接續。
- ES202「設備狀態監控_設備類別」：[功能規格](modules/EMS/設備狀態監控設備類別.md)。依三份一致的 `frmEquipmentStateQueryByType_THEIL_BU1`、`wsEQP.LoadEquipmentType`／`clsEQPBasis.LoadEquipmentType`、共用 `frmEquipmentStateMonitor_THEIL_BU1` 及 `wsEMS.LoadEquipmentState`／`EditEquipmentState`、Equipment／State basis Core 完成類別選擇、每頁 10 台、狀態色、Refresh、Chamber、狀態變更及右鍵預覽。重用設備類別、設備基礎、現況、狀態與互斥表既有 references，無新增 CREATE SQL；設備類別表只有 PG CREATE-only，桌機 Oracle profile 不可用，未補查 raw Oracle metadata。家用端 API 應保護設備可見範圍，狀態更新沿用 ES204 交易及轉換規則。
- ES104「設備狀態互斥設定」：[功能規格](modules/EMS/設備狀態互斥設定.md)。依三份一致的 `frmViolateStateChangeSetMatrix`、`wsEQP.LoadEQPStateBasis`／`wsEMS.LoadViolateStateChange`／`UpdViolatStateChangeData`、`clsEQPBasis`／`clsEMSEquipment` 完成狀態互斥矩陣、既有 pair 標記、雙擊編輯與 Confirm DEV 假儲存。Grid 全域及 cell Activation 設定與 DoubleClick handler 有衝突，DEV 依 handler 的預期行為；家用端需 runtime 確認。直接表 `TBLEMSVIOLATESTATUSCHANGE`、`TBLEMSVIOLATESTATUSLOG` 缺 Oracle source／PG CREATE-only；桌機 profile 不可用、未查 metadata，不猜建表。正式 API 需取得 DDL 後參數化讀寫並以交易原子替換狀態 pair 與 history。ES304、ES305 已完成；ES303「配件壽命維護」已完成；後續依清冊接續。

- ES101「設備控管設定」：[功能規格](modules/EMS/設備控管設定.md)。依標準 `frmEquipmentControl`／內嵌 Designer、`wsEMS.LoadEquipmentStateDefine/UpdEquipmentState` 與 `clsEQPBasis`／`clsEQPStateControl`／`clsEMSEquipment` 完成 EquipmentType 篩選、父設備與 Chamber 多選搬移、Apply/Confirm DEV 假套用。重用設備類別、設備主檔、Idle 控制值、設備現況及歷程的既有 Oracle DDL／PostgreSQL CREATE-only references，無新增 DDL；Legacy Service 逐筆刪除／新增、沒有整批交易，且查無 Idle 的處理需修正。家用端依規格核對 live schema 並實作授權、伺服器端 Chamber 展開、expected-state 檢查及原子批次 API。
- ES303「配件壽命維護」：[功能規格](modules/EMS/配件壽命維護.md)。核對本機 STD `frmAccessoryRepair`／Designer、`wsEMS.LoadAccessoryStateJoinLife/UpdEMSAccesssoryRepair`、`wsEQP.LoadAccessoryBasis/LoadACCStateControlJoinStateBasis`、`wsSYS.GetAccessoryRepairNo` 與 `clsAccState`／`clsSYSEng`；完成多配件／多版本查詢、調帳、歸零、壽命數量及 Scrap 狀態假套用 DEV。沿用現有配件主檔／狀態／生命／控制／互斥表 DDL references；`TBLEMSACCESSORYREPAIR` 缺 Oracle source DDL／metadata 與 PG CREATE-only，桌機受保護 profile 不可用，未猜 schema。Core 的 `CollectionSQL` 交易語意無法從可讀來源確認；家用端需核對 Serial Rule、補 schema/API，並以單一交易更新維修紀錄、現況與必要狀態歷程。

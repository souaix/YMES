# OCAP 異常單處置調整（QC409）

## 功能識別

- Module：QC（品質資料）→ 異常設定
- FunctionNo：OCAP DISPOSITION ADJUST
- FunctionKey：QC409
- WinForms Form：frmOCAPDispositionAdjust
- 流程圖選擇 Form：frmOCAPProcessDraw
- WinForms 專案：STD/MESWin/QC/QC_OCAPDispositionAdjust
- 核心查詢：clsQCBasis.LoadERFJoinOCAPDisposition
- 寫入入口：wsWIP.Exe_ERF_Disposition
- 核心交易：clsLot.UpdOCAPAndERF_Disposition、UpdateWIPOCAPDisposition

本功能處理進行中的 OCAP ERF，不是 OCAP 流程主檔 CRUD。畫面只改目前選取 ERF 的處置節點，或結束目前 Hold Item 的 OCAP 處置；實際業務狀態更新由 WIP 核心交易負責。

## WinForms 畫面

視窗標題 OCAP Disposition Adjust，Form ClientSize 為 1008×591。畫面包含：

1. **查詢條件**
   - ERF No 勾選框及文字欄；勾選後才送出條件。
   - Lot No 勾選框及文字欄；勾選後才送出條件。
   - 建立日期 From/To，各自以日期欄位勾選狀態決定是否套用。
   - Query 按鈕。Form 開啟時不自動查詢。
2. **ERF Query Grid**
   - WinForms 回傳資料保留完整 ERF/OCAP/WIP 狀態欄位，但畫面主要顯示 Lot No、OCAP No、ERF No、Product No；OCAP Version 等內部欄位隱藏。
   - 依 Lot No 排序；一次只使用目前作用列，不支援多選批次處置。
3. **Disposition 選項**
   - Change Disp Group No：選擇 OCAP 下一處置節點/群組。
   - Hold Item Disposition Done：將目前 Hold Item 導向流程 END。
   - Jump OP 按鈕開啟 frmOCAPProcessDraw，選擇目的 OCAP 節點；START/END 不可作為一般跳轉目的。
   - Disp Description 為必填。
4. **操作按鈕**
   - Confirm(&C) 執行單筆處置。
   - Close(&X) 關閉視窗。

## 查詢邏輯

Form 呼叫 wsQC.LoadERFJoinOCAPDisposition，核心 SQL 由 clsQCBasis.LoadERFJoinOCAPDisposition 組成：

    SELECT *
    FROM tblWIPERFBasis A
    LEFT JOIN tblWIPERFOCAPState B ON A.ERFNO = B.ERFNO
    WHERE B.OCAPSTATUS <> 11
      AND A.STATUS <> 2
      AND B.ERFNO IS NOT NULL

- 查詢實際效果只包含有 OCAP State 且 OCAPStatus 不等於 11、ERF Status 不等於 2 的資料。
- ERF No、Lot No 使用 contains 搜尋（舊 SQL LIKE '%值%'）；日期 From 使用 CreateDate >=。
- 日期 To 若值是整日，舊 Core 延伸到當日 23:59:59；若含時間則用輸入時間。
- 查詢結果在 Grid 端依 Lot No 排序。舊 Core 使用字串組 SQL；YMES API 必須改為參數化條件，搜尋值仍須保留 contains 語義。
- 每筆資料以 ERF No、HoldItemNo、OCAP No、OCAP Version 識別處置目標。ERF No 可能有多個 Hold Item/OCAP State，不能只以 ERF No 當 API 更新鍵。

## 操作與狀態轉換

### Change Disp Group No

1. 使用者選取一筆 Grid 資料，選 Change Disp Group No，輸入說明，並從 OCAP 流程圖選目標節點。
2. 舊視窗載入目前 OCAP 版本的 XML/節點圖，以圖上節點對應處置群組。對應節點須屬於目前 OCAP No/Version；不能接受用戶端任意輸入的 GroupNo。
3. 儲存前呼叫 wsWIP.LoadOCAPLinkName 取得指向目標節點的 relation，將 ToNodeID、ToNodeNo、OCAPLinkName 一併傳送。若目標是群組節點，處置群組取其 OCAPGroupNo；空群組按舊行為以節點編號表示。
4. 當 ERFSOURCE=0，重新讀 TBLWIPLOTSTATE，只接受仍處於 Hold 的 Lot 狀態，取最新 LotStamp。ERFSOURCE 非 0 時舊 Form 不讀 LotState。
5. 呼叫 wsWIP.Exe_ERF_Disposition，一般跳轉參數為 DispType=0、OCAPStatus=2、JumpProcess=True，並傳目前群組、目標群組、目標節點/連線、HoldItemNo、說明及 LotStamp。成功後清除選項/輸入並重查。

### Hold Item Disposition Done

1. 讀取該 ERF/OCAP 的目前 OCAP State；當狀態為 1 時，使用狀態列目前的 GroupNo，若無 GroupNo 則以 NodeNo 代表群組。
2. 載入導向 END 的 OCAP relation；若找不到 relation，舊 Form 傳送 END 作為 NodeID、NodeNo、NextGroupNo 及最終連線替代值。
3. 重查同 ERF No 的目前待處置列數。若不超過 1，呼叫 DispType=2、OCAPFinaldisp=True、OCAPStatus=11；若還有其他 Hold Item，仍以 OCAPStatus=11 導向 END，但 DispType=0、OCAPFinaldisp=False。
4. 成功後清空表單欄位/選項並重查。此處「完成一個 Hold Item」和「ERF 最終處置」是不同狀態；API 不可把每次 Done 都當成解 Hold/結案。

### 共通驗證與並行控制

- 必須選取作用列、選一種處置方式、輸入非空 Disp Description；來源欄位 DESCRIPTION 上限 255。
- 系統 ERF（ERFSOURCE=0）必須能查到 Hold 中的 Lot State；使用讀取到的 LotStamp 做併發檢查。YMES API 必須伺服器端重新讀取並檢核狀態與 LotStamp，不能信任請求本文的 LotStamp。
- 一個 ERF 對應多個 Hold Item 時，結束單筆不得誤結束整張 ERF。最終處置判斷必須在交易內重新計數並鎖定/採等價併發策略，以避免同時完成造成錯誤結案。
- 群組、節點、版本及連線都由伺服器依 OCAP 流程資料驗證。API 不接受前端直接指定 DispType、OCAPStatus、下一群組或 arbitrary relation 值。

## Service/Core 寫入範圍

WinForms 的確認不是單表 CRUD。呼叫鏈為：

frmOCAPDispositionAdjust → wsWIP.Exe_ERF_Disposition → clsLot.UpdOCAPAndERF_Disposition → UpdateWIPOCAPDisposition / SetWIPERFOCAPGroupState

核心組合資料庫交易及既有 hook，依 ERF Source、ERF 類型、目前/目的處置及附件等條件可能讀寫：

- TBLWIPERFOCAPDISPOSITION：每個 Hold Item 的 OCAP 處置歷程，DispOrder 逐 ERF 增加；紀錄目前/下一群組、處置節點、連線、使用者、時間、NextDispType 和說明。
- TBLWIPERFOCAPSTATE：目前 Hold Item/OCAP 的狀態、節點、群組及事件時間。
- TBLWIPERFOCAPGROUPSTATE：更新群組節點執行旗標，必要時移除已完成群組執行狀態。
- TBLWIPERFBASIS、TBLWIPERFDISPOSITION：更新 ERF 狀態、目前處理群組、處置說明、處置順序及處置紀錄；最後處置依 ERFSource/ERF 類別走不同 Release/Final 狀態。
- TBLWIPERFHOLDREASONDISP：核心若收到新的 Hold reason disposition，可能以整組替換方式寫入。
- TBLWIPLOTSTATE、TBLWIPLOTLOG、TBLWIPGOODBINSTATE：系統來源 ERF 的 WIP Hold/事件/站內紀錄與最終處置分支。
- TBLWIPERFBATCHLOT：批次 ERF 分支相依。
- TBLWIPERF_ATTACHFILE、TBLWIPERFOCAP_ATTACHFILE：當附件集合有提交時確認附件狀態；舊 Form 本身不提供附件編輯，但共用 Service 有此條件分支。
- TBLUSRUSERBASIS、TBLENTCUSTOMERCONTACTOR：共用 ERF 通知處理會依環境/收件人查詢使用者與聯絡人。
- udWIP.UpdERF_Disposition 客製 hook、稽核及通知可能產生額外副作用；API 上線前要查 YMES 對應的 extension/event/mail 實作，避免重複寄信或漏掉現場自訂處理。

以上表清單依 STD/MESkc/kcWIP/clsLot.cs 的核心方法及相關 OCAP helper 追查；實際分支須以 ERFSource、ERF type、disp type 及是否有附件判斷。整個狀態更新應在 YMES 的一個 DB transaction 內完成；外部 mail/extension 用可重試 outbox 或 YMES 既有機制，避免資料交易成功但副作用不一致。不可只 insert OCAP disposition 後回成功。

## Oracle 結構

已以 Windows PowerShell 5.1 + Oracle.ManagedDataAccess 使用公司端既有受保護連線，直接唯讀執行 DBMS_METADATA.GET_DDL 和索引 metadata；沒有讀業務資料列，也沒有在 Oracle 執行 CREATE/ALTER/DML。

- 三張 OCAP WIP 專用表 DDL：database/reference/mestest_qc409_wip_ocap_source_ddl.sql
- 共用 ERF/WIP/通知副作用相關表 DDL：database/reference/mestest_qc409_wip_disposition_dependencies_source_ddl.sql（11 張表及 Oracle indexes）
- PostgreSQL 空表 CREATE-only 對照（只涵蓋 3 張 OCAP WIP 專用表）：database/reference/mestest_qc409_wip_ocap_create.sql

三張 OCAP 專用表的 PK：
- TBLWIPERFOCAPDISPOSITION: (ERFNO,HOLDITEMNO,OCAPNO,OCAPVERSION,DISPORDER)
- TBLWIPERFOCAPSTATE: (ERFNO,OCAPNO,OCAPVERSION)
- TBLWIPERFOCAPGROUPSTATE: (ERFNO,OCAPNO,OCAPVERSION,OCAPNODEID)

Oracle source DDL 未宣告 FK。一般 WIP 表是多模組共用，不在此頁重複提供 PostgreSQL CREATE migration；家用端必須逐表比對 YMES live schema 與已部署 WIP migration，只有缺少必要 mapping 才做向前 migration，保留其既有 schema/data。不得在 Oracle 執行 CREATE-only 參考。

## Vue DEV 實作與已知差異

Vue 3 DEV 元件 web/src/components/QcOcapDispositionAdjust.vue 掛在 /YMES/QC/OCAPDispositionAdjust，於「品質資料 → 異常設定 → OCAP異常單處置調整」顯示；只使用獨立 localStorage 假資料。

- 畫面提供 WinForms 同名查詢條件、結果欄位、單列作用資料、兩種處置單選、目標節點選擇、說明及 Confirm/Close。
- Production 初次開啟不自動查詢，使用者按 Query 才讀 API；DEV Preview 為了顯示假資料會載入標記為 DEV 的 ERF，不呼叫資料庫。
- WinForms 的流程選擇是 AddFlow 圖；目前 Vue 以節點清單彈窗替代。節點跳轉仍必須由 OCAP graph route-options API 依版本與目前位置回傳有效路徑。
- 實際 mail、客製 WIP hook、完整批次 ERF/附件處理、database transaction 尚未實作，等待家用端 API handoff。

## YMES API 合約交接

授權要求登入使用者具有 FunctionNo=OCAP DISPOSITION ADJUST / FunctionKey=QC409。所有 SQL 參數化、使用 server-side audit identity，業務寫入依本頁所述核心交易，不得只維護三張 OCAP 表。

| Method / route | 用途 |
| --- | --- |
| GET /api/qc/ocap-dispositions?erfNo=&lotNo=&createDateFrom=&createDateTo= | 查待處置列，帶回 ERFSource/Status、LotNo、ProductNo、HoldItemNo、OCAP No/Version/Status、目前節點/群組、系統來源 LotStamp。 |
| GET /api/qc/ocap-dispositions/{erfNo}/{holdItemNo}/route-options?ocapNo=&ocapVersion=&currentNodeId= | 依 OCAP version 和目前節點，回傳有效可跳轉的節點/群組/Relation link；排除 START/END 作一般目的。 |
| POST /api/qc/ocap-dispositions/{erfNo}/{holdItemNo}/adjust | body { ocapNo, ocapVersion, action: "changeGroup" | "finish", targetNodeId?, description, lotStamp? }；server 從 DB 決定 DispType、狀態、群組、relation、最終 ERF 判斷與所有交易副作用。 |

- 每個端點查核 QC409 ACL。拒絕重複/不存在/已完成或狀態變更的操作；並行 LotStamp 過期回 409。
- route option 與 POST 都要重查 OCAP 節點/relation、目前 OCAP State 和 ERF/hold 狀態，不能相信 UI 先前載到的路徑。
- Finish 的最後一個 Hold Item 判斷、ERF/WIP 最終狀態以及附件/hold-reason/hook 等副作用要在交易範圍內一致處理；通知採 YMES 既有或 outbox 機制。
- Write audit/log 結果含 ERFNo、HoldItemNo、OCAP/version、操作人、LotStamp/結果和 request id，不記錄連線秘密或任意 SQL。
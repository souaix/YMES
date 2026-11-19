# PRD OVERSTAY 產品作業站 Overstay 維護

- `PRD / PRD OVERSTAY`，Web `/YMES/PRD/Overstay`。核對 `Customer/Production/MESWin/PRD/PD_OverstaySetup_THEIL_BU1/frmOverstaySetup_THEIL_BU1.vb`（含內嵌 Designer）。可依產品、版次或站點查詢，產品與站點皆留白時先確認。結果顯示站名、警告時數、異常群組、Hold 時數，支援選多列刪除。
- 匯入讀第一張工作表 A–G，依序為產品、版次、站名、站點、警告時數、異常群組、Hold 時數。前三個識別欄位為 A、B、D，其餘可空。原程式核對核准產品與實際流程，產品識別含 `ALL` 時跳過產品及流程檢查。群組以逗號分隔，從 `TBLUSRUSERGROUP` 的 `GROUPTYPE='5'` 核對。
- DEV 使用 XLSX、假產品／流程與固定異常群組，預檢通過才允許套用。同產品號、版次、站點的舊資料另存本機歷史；多選刪除只移除現有設定。正式的目的站、分支及版次鍵、舊版 XLS、資料量限制、群組和 Overstay 執行由服務承接。

## Oracle 與家用端交接

- PowerShell 5.1 + ODP.NET 唯讀核對 `XX_TBLOVERSETUP_THEIL_BU1` 與 `XX_TBLOVERSETUPLOG_THEIL_BU1` 各 15 欄。主表 PK `(PRODUCTNO, PRODUCTVERSION, OPNO, TOOPNO, FROM_BR, TO_BR)`；歷史表未查到 PK。沿用 [GBOM 主表 DDL](../../../database/reference/mestest_prd_gbom_source_ddl.sql) 與 [CREATE-only](../../../database/reference/mestest_prd_gbom_create.sql)，補上 [歷史 Oracle DDL](../../../database/reference/mestest_prd_overstay_history_source_ddl.sql) 與 [歷史 CREATE-only](../../../database/reference/mestest_prd_overstay_history_create.sql)。
- 客製 `wsTHEIL/wsPRD/wsPRD.cs` 轉交 `MEStc_THEIL/tcPRD/clsProduct.cs` 的 `LoadOverstaySetup`、`ExcelDataValidate`、`AddOverStay` 與 `DelOverStay`。舊服務以產品／版次／站點三欄範圍歷史化、替換與刪除。Oracle 主表目的站和分支為 NOT NULL，而舊匯入 SQL 省略這些欄位。正式 API 前須核對觸發器、部署差異與既有資料的鍵值。
- 建議 `/api/prd/overstay` 提供查詢、匯入預檢、套用及多列刪除。交易需原子保存歷史及替換整個選定範圍；確認實際是否以三欄範圍或完整六欄操作，再決定刪除鍵。服務應對照正式流程與群組，並核對核准狀態及 `ALL` 資料規則。

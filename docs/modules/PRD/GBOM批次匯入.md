# PD117 GBOM 批次匯入

- `PRD / PRD GBOM IMPORT`，Web `/YMES/PRD/GbomBatchImport`。來源 `Customer/Production/MESWin/PRD/PD_GBOMImport_THEIL_BU1/frmMultGBOMImport_THEIL_BU1.vb`（含 Designer），批次讀來源目錄的工作簿，逐檔檢查、回報，並依原程式搬至 success 或 fail 目錄。
- 檔名為產品編號與版次，以 `-` 分隔。A2、B2 需同為檔名本體，版次 REV 要一致；只接受未簽核產品。讀第一張工作表，第五列起以 A–V 取得流程類型、站點、順序、路徑、SAP 控制碼、物料、SOP、eSOP 與參數等資訊。欄位空白時按既有站點、順序及 SAP 控制碼規則補值。
- DEV 可選目錄或多份 XLSX，顯示進度、逐檔結果與流程連線預覽，只將通過的工作簿資料套用到本機。DEV 用最後一個連字號分隔產品號與版次，容許假產品號中的連字號；舊程式取前兩段。DEV 保留列號和 A–V 值，使用工作簿內已有公式值。節點圖形、節點編號與矩陣的完整轉換、公式重算、XLS、原檔搬移／備份與正式產品異動由服務端接續。

## Oracle 與家用端交接

- `wsPRD.LoadProduct` 核對產品版次，`wsOP.LoadOPBasis` 提供站點。客製 `Customer/Production/wsTHEIL/wsPRD/wsPRD.cs` 的 `GBOMImport` 將工作簿資料交給 `MEStc_THEIL/tcPRD/clsProduct.cs` 同名方法。服務建立／更新版次 01 的流程、節點與路徑，建立產品流程對應，依原規則修改產品描述與 SOP、重建 BOM，並處理相關 Overstay、參數與 SAP 相依。原程式另用 `LoadGBOM_PATHUrl` 取得備份設定。
- PowerShell 5.1 + ODP.NET 唯讀確認 `TBLPRSPROCESSBASIS` 16 欄、`TBLPRSNODEBASIS` 17 欄、`TBLPRSNODERELATION` 7 欄、`TBLPRDPRODUCTPROCESS` 16 欄、`TBLPRDBOM` 19 欄；核對 `XX_TBLPRDPRARKEY_THEIL_BU1`、`XX_TBLOVERSETUP_THEIL_BU1` 與 `XX_TBLWIPINFORECORD`。最後一項在既有交接列為無效 view，字典可見 26 欄，正式 SAP 欄位相依待核對。沿用 [PD102 產品 CREATE](../../../database/reference/mestest_prd_product_basis_create.sql)、[PRS104 流程 CREATE](../../../database/reference/mestest_prs104_process_basis_create.sql)、[客製 Oracle DDL](../../../database/reference/mestest_prd_gbom_source_ddl.sql) 與 [客製 CREATE](../../../database/reference/mestest_prd_gbom_create.sql)。
- 建議 `/api/prd/gbom-batch-import` 提供逐檔預檢與確認，交易以產品／版次為單位。正式提交時重查未簽核、站點／物料及既有流程，相同批次的重送須可辨識。原檔處理結果與資料寫入結果各自回報。

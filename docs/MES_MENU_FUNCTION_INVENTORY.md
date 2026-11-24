# MES Oracle 選單與功能目錄

來源：2026-11-13 由公司電腦 PowerShell 5.1 + Oracle.ManagedDataAccess 唯讀查詢 MESTEST.TBLSYSMODULE、TBLSYSMENUITEM、TBLSYSFUNCTION；第一層中文名稱取自 Customer/Production/MESClient/mesresource.zh-cht.resources。只包含系統選單中繼資料，不含 MES 業務資料。

WinForms 的 MDI_Panel.funGenerateMainMenu 以 ModuleNo + Module 作為第一層翻譯鍵；TBLSYSMODULE 本身沒有 ModuleName 欄位。此目錄以正式繁中資源檔對應 ModuleNoModule，並依 TBLSYSMODULE.ModuleOrder 排序。

## 來源筆數

| 類型 | 筆數 |
| --- | ---: |
| 第一層 Module | 20 |
| MenuItem | 601（MenuType=1 分類 82，MenuType=0 功能項 519） |
| Function | 553 |
| MenuType=0 與 FunctionNo 完整關聯 | 518 / 519 |

## 第一層與功能目錄

第一層排序以 ModuleOrder 為準。每個功能列附來源 FunctionNo、FunctionName、所屬分類、FormName、ExecutionFile、FunctionKey、ExecutionMode；分類和功能名稱均直接來自 MESTEST。

### ENT — 客戶資料（順序 1）

分類：客戶資料設定 [CUSTOMER CONFIG]、外包商資料設定 [SUBCONTRACTOR CONFIG]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| BROKER BASIS | 報關行基本資料 | 客戶資料設定 | frmBrokerBasis | ET_BrokerBasis.dll | ET103 | 0 |
| CUSTOMER BASIS | 客戶基本資料 | 客戶資料設定 | frmCustomerBasis | ET_CustomerBasis.dll | ET101 | 0 |
| CUSTOMER CONTACTOR | 客戶連絡人資料 | 客戶資料設定 | frmCustomerContactor | ET_CustomerContactor.dll | ET102 | 0 |
| CUSTOMER SHIP ADDRESS | 客戶出貨地址資料 | 客戶資料設定 | frmCustomerShipAddr | ET_CustomerShipAddr.dll | ET106 | 0 |
| ENTOSITEM | 外包項目設定 | 外包商資料設定 | frmENTOSItem | ET_OSItem.dll | ET202 | 0 |
| FORWARDER BASIS | 貨運行基本資料 | 客戶資料設定 | frmForwarderBasis | ET_ForwarderBasis.dll | ET104 | 0 |
| SUBCONTRACTOR BASIS | 外包商基本資料 | 外包商資料設定 | frmSubcontractor | ET_Subcontractor.dll | ET201 | 0 |
| TAKER BASIS | 取貨公司基本資料 | 客戶資料設定 | frmTakerBasis | ET_TakerBasis.dll | ET105 | 0 |

### EQP — 設備資料（順序 2）

分類：設備設定 [EQP CONFIG]、行業包 [INDUSTRY]、配件 [ACC CONFIG]、載具 [CARRIER]、參數 [RECIPE]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| EQP ACC STATE | 配件狀態設定 | 配件 | frmEQPACCStateBasis | EP_ACCStateBasis.dll | EP205 | 0 |
| CARRIER CATEGORY | 載具分類資料 | 載具 | frmCarrierCategory | EP_CarrierCategory.dll | EP301 | 0 |
| CARRIER TYPE | 載具類別資料 | 載具 | frmCarrierType | EP_CarrierType.dll | EP302 | 0 |
| EQP ENGINEER GROUP | 設備工程群組資料 | 設備設定 | frmEQPEngineerGroupBasis | EP_EngineerGroupBasis.dll | EP102 | 0 |
| EQP GROUP BASIS | 設備群組資料 | 設備設定 | frmEquipmentGroup | EP_EquipmentGroup.dll | EP105 | 0 |
| EQP QCLIST | 設備點檢表設定 | 設備設定 | frmEQPQCListBasis | EP_QCListBasis.dll | EP107 | 0 |
| EQP STATE | 設備狀態設定 | 設備設定 | frmEQPStateBasis | EP_EQPStateBasis.dll | EP106 | 0 |
| EQP TYPE | 設備類別資料 | 設備設定 | frmEquipmentType | EP_EquipmentType.dll | EP103 | 0 |
| EQP VENDOR | 設備供應商基本資料 | 設備設定 | frmEQPVendor | EP_EQPVendor.dll | EP101 | 0 |
| B_EQP AUTOLOADER SETUP | 設備AutoLoader格式檔設定 | 行業包 | frmEquipmentAutoLoaderSetup | EP_B_EQPAUTOLOADERSETUP.DLL | EPB01 | 0 |
| ACC BASIS | 配件基本資料 | 配件 | frmAccessoryBasis | EP_AccessoryBasis_BU1.dll | EP203 | 0 |
| ACC CATEGORY | 配件分類資料 | 配件 | frmAccessoryCategory | EP_AccessoryCategory.dll | EP201 | 0 |
| ACC CATEGORY STATE | 配件分類狀態定義設定 | 配件 | frmAccessoryCategoryStateBasis | EP_AccessoryCategoryStateBasis.dll | EP204 | 0 |
| ACC TYPE | 配件類別資料 | 配件 | frmAccessoryType | EP_AccessoryType.dll | EP202 | 0 |
| RECIPE BASIS | 設備參數資料 | 參數 | frmEQPRecipeBasis | EP_RecipeBasis.dll | EP401 | 0 |
| CARRIER BASIS | 載具基本資料 | 載具 | frmCarrierBasis | EP_CarrierBasis_BU1.dll | EP303 | 0 |
| EQP BASIS | 設備基本資料 | 設備設定 | frmEquipmentBasis | EP_EquipmentBasis_BU1.dll | EP104 | 0 |
| COM_MESAPI | COM_MESAPI | 未掛入 MenuItem | 123 | EP_COM_MesApiClient.dll |  | 0 |

### SMD — 區域資料（順序 3）

分類：廠區設定 [SMD CONFIG]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| AREA BASIS | 區域基本資料 | 廠區設定 | frmAreaBasis | SD_AreaBasis.dll | SD104 | 0 |
| AREA INV MAP | 區域庫房基本設定 | 廠區設定 | frmAreaInventory | SD_AreaInventory.dll | SD105 | 0 |
| COMPUTER AREA | 過帳電腦之區域 | 廠區設定 | frmComputerArea | SD_ComputerArea.dll | SD106 | 0 |
| FACTORY BASIS | 工廠基本資料 | 廠區設定 | frmFactoryBasis | SD_FactoryBasis.dll | SD101 | 0 |
| LOCATOR BASIS | 儲位基本資料 | 廠區設定 | frmINVLocatorBasis | SD_LocatorBasis.dll | SD103 | 0 |
| POSITION BASIS | 工位基本資料 | 廠區設定 | frmSMDPDLinePositionBasis | SD_PDLinePositionBasis.dll | SD109 | 0 |
| PRODUCTION CONFIG | 設備狀態監控_區域 | 廠區設定 | frmAreaShow | SD_AreaShow.dll | SD108 | 0 |
| SMD PDLINE BASIS | 生產線別基本資料 | 廠區設定 | frmPDLineBasis | SD_PDLineBasis.dll | SD107 | 0 |

### MTL — 物料資料（順序 4）

分類：物料設定 [MATERIAL CONFIG]、行業包 [INDUSTRY]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_MTL AUTOLOADER SETUP | 來料AutoLoader格式檔設定 | 行業包 | frmMaterialVendorAutoLoaderSetup | ML_B_MTLAUTOLOADERSETUP.DLL | MLB01 | 0 |
| MTL BASIS | 物料基本資料 | 物料設定 | frmMaterialBasis | ML_MaterialBasis.dll | ML103 | 0 |
| MTL GROUP | 物料群組資料 | 物料設定 | frmMaterialGroup | ML_MaterialGroup.dll | ML104 | 0 |
| MTL LABEL MAP BASIS | 物料標籤對應設定 | 物料設定 | frmMTLLabelBasis | ML_MTLLabelBasis.dll | ML107 | 0 |
| MTL QCITEM | 物料品管項目設定 | 物料設定 | frmMaterialQCItemSet | ML_MaterialQCItemSet.dll | ML106 | 0 |
| MTL TYPE | 物料類別資料 | 物料設定 | frmMaterialType | ML_MaterialType.dll | ML102 | 0 |
| MTL TYPE QCITEM | 物料類別品管項目設定 | 物料設定 | frmMaterialTypeQCItem | ML_MaterialTypeQCItem.dll | ML105 | 0 |
| MTL VENDOR | 物料供應商基本資料 | 物料設定 | frmMaterialVendor | ML_MaterialVendor.dll | ML101 | 0 |

### QC — 品質資料（順序 5）

分類：基本資料 [QC CONFIG]、品管設定 [QC DATA]、計量規格設定 [QC DATA_V]、異常設定 [HOLD CONFIG]、檢測儀器 [INSPECTION MACHINE]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| 8D REASON BASIS | 8D 原因基本資料 | 基本資料 | frmReasonBasis_8D | QC_ReasonBasis_8D.dll | QC111 | 0 |
| FINAL GROUP | 最後處理群組 | 異常設定 | frmFinalGroup | QC_FinalGroup.dll | QC407 | 0 |
| FIRST GROUP | 第一處理群組 | 異常設定 | frmFirstGroup | QC_FirstGroup.dll | QC406 | 0 |
| HOLD DESCRIPTION | 異常說明 | 異常設定 | frmHoldDescription | QC_HoldDescription.dll | QC404 | 0 |
| HOLD DISP GROUP | 異常處理群組 | 異常設定 | frmHoldDispGroupBasis_THEIL_BU1 | QC_HoldDispGroupBasis_THEIL_BU1.dll | QC405 | 0 |
| HOLDITEM BASIS | 異常原因基本資料 | 異常設定 | frmHoldItemBasis | QC_HoldItemBasis.dll | QC403 | 0 |
| IMPORT QC CHART DATA | 匯入管制圖內容 | 計量規格設定 | frmImport_QCChartBasis | QC_Import_QCChartBasis.dll | QC302 | 0 |
| IMPORT QCITEM ATTRIBUTES | 計數品管項目匯入 | 品管設定 | frmImport_QCItem_Attributes | QC_Import_QCItem_Attributes.dll | QC215 | 0 |
| IMPORT QCITEM VARIABLES | 計量品管項目匯入 | 品管設定 | frmImport_QCItem_Variables | QC_Import_QCItem_Variables.dll | QC214 | 0 |
| INSPMACHINE DRIVER | 檢測設備連線程式 | 檢測儀器 | frmInspDriver | QC_InspDriver.dll | QC503 | 0 |
| INSPMACHINE PARAMETER | 檢測儀器基本資料 | 檢測儀器 | frmInspMachineParameter | QC_InspMachineParameter.dll | QC502 | 0 |
| INSPMACHINE PARAMETER TYPE | 檢測儀器類別 | 檢測儀器 | frmInspMachineParameterType | QC_InspMachineParameterType.dll | QC501 | 0 |
| MCLASS BASIS | 主分類基本資料 | 異常設定 | frmMClassBasis | QC_MClassBasis.dll | QC401 | 0 |
| MTLQCITEM | 物料規格設定 | 計量規格設定 | frmQCMaterialSpec | QC_MaterialSpec.dll | QC304 | 0 |
| OCAP BASIS | OCAP基本資料 | 異常設定 | frmOCAPBasis_THEIL_BU1 | QC_OCAPBasis_THEIL_BU1.dll | QC408 | 0 |
| OCAP DISPOSITION ADJUST | OCAP異常單處置調整 | 異常設定 | frmOCAPDispositionAdjust | QC_OCAPDispositionAdjust.dll | QC409 | 0 |
| PRDQCITEM | 產品規格設定 | 計量規格設定 | frmQCProductSpec | QC_ProductSpec.dll | QC305 | 0 |
| QC CHART BASIS | 管制圖內容 | 計量規格設定 | frmQCChartBasis | QC_ChartBasis.dll | QC301 | 0 |
| QC INSPECTION | 設備品檢管理設定 | 品管設定 | frmQCInspection | QC_Inspection.dll | QC217 | 0 |
| QC REASON PRD SOP FILE | 品質原因產品指導書 | 基本資料 | frmReasonPrdSOPFile | QC_ReasonPrdSOPFile.dll | QC112 | 0 |
| QC RULE BASIS | 計量品管法則基本資料 | 基本資料 | frmQCRuleBasis | QC_VarRuleBasis.dll | QC108 | 0 |
| QC SPEC BASIS | 品管項目規格 | 計量規格設定 | frmQCSpecBasis | QC_SpecBasis.dll | QC303 | 0 |
| QCFACTORMAINTAIN | 要因資料維護 | 基本資料 | frmQCFactorMaintain | QC_FactorMaintain.dll | QC109 | 0 |
| QCITEM PRD REASON MAP | 計數品管項目產品原因對應設定 | 品管設定 | frmItemPRDReasonMap | QC_ItemPRDReasonMap.dll | QC218 | 0 |
| QCITEM RULE MAP | 計量品管項目管制法則對應 | 品管設定 | frmQCItemRule | QC_ItemRule.dll | QC209 | 0 |
| QCITEM_A | 計數品管項目 | 品管設定 | frmQCItemForAttributes | QC_ItemForAttributes.dll | QC201 | 0 |
| QCITEM_A RULE DATA | 計數判別法則基本資料 | 基本資料 | frmQCAttRuleBasis | QC_AttRuleBasis.dll | QC107 | 0 |
| QCITEM_A RULE MAP | 計數品管項目判別法則對應 | 品管設定 | frmQCAttRuleMap | QC_AttRuleMap.dll | QC203 | 0 |
| QCITEM_ADELETE | 刪除計數型品管項目量測記錄 | 品管設定 | frmQCItemDeleteForAttributes | QC_ItemDeleteForAttributes.dll | QC204 | 0 |
| QCITEM_G | 計量品管項目_群組 | 品管設定 | frmQCItemGroup_THEIL_BU1 | QC_ItemGroup_THEIL_BU1.dll | QC212 | 0 |
| QCITEM_G_A_ORDER | 計數品管項目_群組 | 品管設定 | frmQCItemAttOrderGroup | QC_ItemAttOrderGroup.dll | QC213 | 0 |
| QCITEM_G_PMI | 計量品管項目群組_減項 | 品管設定 | frmPMI_QCItemGroup | QC_PMI_QCItemGroup.dll | QC216 | 0 |
| QCITEM_V | 計量品管項目 | 品管設定 | frmQCItemForVariables_THEIL_BU1 | QC_ItemForVariables_THEIL_BU1.dll | QC205 | 0 |
| QCITEM_VDELETE | 刪除計量型品管項目量測記錄 | 品管設定 | frmQCItemDeleteForVariables | QC_ItemDeleteForVariables.dll | QC210 | 0 |
| REASON BASIS | 品質原因基本資料 | 基本資料 | frmReasonBasis | QC_ReasonBasis.dll | QC106 | 0 |
| REASON SUBTYPE BASIS | 品質原因子類別資料 | 基本資料 | frmReasonSubTypeBasis | QC_ReasonSubTypeBasis.dll | QC105 | 0 |
| SAMPLING 1916 | 抽樣計劃設定_1916 | 基本資料 | frmSampling1916 | QC_Sampling1916.dll | QC102 | 0 |
| SAMPLING PLAN | 抽樣計劃 | 基本資料 | frmSamplingPlan | QC_SamplingPlan.dll | QC103 | 0 |
| SAMPLING TABLE | 抽樣表格 | 基本資料 | frmSamplingTable | QC_SamplingTable.dll | QC101 | 0 |
| SAMPLINGFOR2NDINSP | 二次檢驗抽樣計畫 | 品管設定 | frmSamplingFor2ndInsp | QC_SamplingFor2ndInsp.dll | QC211 | 0 |
| SCLASS BASIS | 次分類基本資料 | 異常設定 | frmSClassBasis | QC_SClassBasis.dll | QC402 | 0 |
| SPC ATT LIMIT | 計數管制內容 | 品管設定 | frmSPCAttLimit | QC_SPCAttLimit.dll | QC202 | 0 |
| SPECIALBASISMAINTAIN | 特殊主檔維護 | 基本資料 | frmSpecialBasisMaintain | QC_SpecialBasisMaintain.dll | QC110 | 0 |
| WAFER SAMPLING PLAN | 抽樣計劃_元件 | 基本資料 | frmSamplingPlan_Wafer | QC_SamplingPlan_Wafer.dll | QC104 | 0 |

### OP — 作業站資料（順序 6）

分類：作業站設定 [OP CONFIG]、行業包 [INDUSTRY]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_CUSTOMER OP MAPPING | 客戶站點對應設定 | 行業包 | frmB_OPCustomerOPMapping | OP_B_CustomerOPMapping.dll | OPB03 | 0 |
| B_ECN OP GROUP BASIS | ECN 作業站群組設定 | 行業包 | frmOP_B_ECNOPGroupBasis | OP_B_ECNOPGroupBasis.dll | OPB02 | 0 |
| B_OP GROUP ATTRIB | 作業站參數群組設定 | 行業包 | frmOPGroupAttrib | OP_B_OPGROUPATTRIB.DLL | OPB01 | 0 |
| BR BASIS | 企業邏輯設定 | 作業站設定 | frmBusinessRuleBasis | OP_BusinessRuleBasis.dll | OP102 | 0 |
| I_OP RULE BASIS_FT | 集成電路測試法則設定 | 行業包 | frmOPControlRule_FT | OP_I_CONTROLRULE.dll | OPI01 | 0 |
| OP BASIS | 作業站基本資料 | 作業站設定 | frmOPBasis | OP_OPBasis.dll | OP104 | 0 |
| OP GROUP BASIS | 作業站群組資料 | 作業站設定 | frmOPGroupBasis | OP_GroupBasis.dll | OP105 | 0 |
| OP RULE BASIS | 作業站法則設定 | 作業站設定 | frmOPControlRule | OP_ControlRule.dll | OP101 | 0 |
| OP TYPE | 作業站類別資料 | 作業站設定 | frmOPType | OP_OPType.dll | OP103 | 0 |
| P_OP RULE BASIS_CP | 晶圓測試法則設定 | 行業包 | frmOPControlRule_CP | OP_P_CONTROLRULE.DLL | OPP01 | 0 |

### PRS — 流程資料（順序 7）

分類：流程設定 [PRS CONFIG]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| PRS BASIS | 流程基本資料 | 流程設定 | frmProcessBasis | PS_ProcessBasis.dll | PS104 | 0 |
| PRS TYPE | 流程類別資料 | 流程設定 | frmProcessType | PS_ProcessType.dll | PS103 | 0 |
| PS BASIS | 生產區段資料 | 流程設定 | frmProductionSegment | PS_ProductionSegment.dll | PS101 | 0 |
| STAGE BASIS | 製造層別設定 | 流程設定 | frmFABPRSStageBasis | PS_FABPRSStageBasis.dll | PS102 | 0 |

### PRD — 產品資料（順序 8）

分類：產品設定 [PRD CONFIG]、等級設定 [BIN CONFIG]、挑片規則 [WAFERRULES]、CP行業設定 [CP_SETUP]、封測行業設定 [PKG_SETUP]、產品文件管理 [PG_MGT]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_UPLOAD FILE BASIS | 檔案上傳基本資料 | 產品文件管理 | frmUploadFileBasis | PD_B_UploadFileBasis.dll | PDB04 | 0 |
| PRD PACKING RULE BASIS | 包裝規則設定 | 產品設定 | frmProductPackingRuleBasis | PD_ProductPackingRuleBasis.dll | PD112 | 0 |
| PRD PACKINGRULE MAP | 產品包裝規則對應設定 | 產品設定 | frmProductPackingRuleMap | PD_ProductPackingRuleMap.dll | PD113 | 0 |
| PRD QCRULE BASIS | 產品抽樣頻率規則 | 等級設定 | frmPRDQcRuleBasis | PD_QCRuleBasis.dll | PD206 | 0 |
| PRD RECIPE BASIS | 產品參數群組 | 產品設定 | frmPRDRecipe | PD_Recipe.dll | PD105 | 0 |
| PRD TYPE | 產品類別資料 | 產品設定 | frmProductTypeBasis | PD_ProductTypeBasis.dll | PD101 | 0 |
| PRODUCT LABEL MAP BASIS | 產品標籤對應設定 | 產品設定 | frmProductLabelBasis | PD_ProductLabelBasis.dll | PD108 | 0 |
| PRODUCT OP AMEND | 產品作業站修正 | 產品設定 | frmProductOPAmend | PD_ProductOPAmend.dll | PD110 | 0 |
| PRODUCT OP COPY | 產品作業站批次複製 | 產品設定 | frmProductOPCopy | PD_ProductOPCopy.dll | PD111 | 0 |
| PRODUCT OP DEFAULT | 產品作業站預設 | 產品設定 | frmProductOPDeafult | PD_ProductOPDeafult.dll | PD109 | 0 |
| PRODUCT RUN CARD MAP BASIS | 產品流程卡對應設定 | 產品設定 | frmProductRunCardBasis | PD_ProductRunCardBasis.dll | PD115 | 0 |
| PRODUCT SEARCH | 產品內容查詢 | 產品設定 | frmProductSearch | PD_ProductSearch.dll | PD104 | 0 |
| USER DEFINED RUNCARD | 自定義流程卡 | 產品設定 | frmUserDefinedRunCardData | PD_UserDefinedRunCard.dll | PD114 | 0 |
| PRD OVERSTAY | 產品作業站Overstay維護 | 產品設定 | frmOverstaySetup_THEIL_BU1 | PD_OverstaySetup_THEIL_BU1.dll |  | 0 |
| PRD WORKTIME | 產品作業站標工維護 | 產品設定 | frmRunTimeSetup_THEIL_BU1 | PD_RunTimeSetup_THEIL_BU1.dll | PD901 | 0 |
| EQC SAMPLE RULE | 電性(EQC)抽樣規則 | 等級設定 | frmEQCSampleRule | PD_EQCSampleRule.dll | PD204 | 0 |
| FT PROGRAM BASIS | FT測試程式 | 等級設定 | frmFTProgramBasis | PD_FTProgramBasis.dll | PD202 | 0 |
| LABEL DATA | 標籤設定 | 產品設定 | frmLabelData | PD_LabelData.dll | PD107 | 0 |
| LIMITED TIME CONTROL | 生產時間限制(站與站) | 產品設定 | frmPRDMaxTime | PD_MaxTime.dll | PD106 | 0 |
| P_CP PROGRAM DEFINE | 晶圓測試程式定義 | CP行業設定 | frmPRDCPProgram | PD_P_CPPROGRAMDEFINE.DLL | PDP02 | 0 |
| P_CP WAFER YIELD SHIP REPORT | 晶圓良率與出貨報告維護 | CP行業設定 | frmCPWaferYieldShipReport | PD_P_CPWAFERYIELDSHIPREPORT.DLL | PDP01 | 0 |
| P_PRD WAFERMAP MERGE RULE | 產品晶圓圖疊圖規則設置 | CP行業設定 | frmPRDMergeMAPRule | PD_P_WaferMAPMergeRule.dll | PDP04 | 0 |
| P_WAFER RULE MANAGER | 產品晶圓法則管制管理 | CP行業設定 | frmPRDWaferRuleManager | PD_P_PRDWaferRuleManager.dll | PDP03 | 0 |
| I_ACCGROUPMAINTAIN | 配件群組維護作業 | 封測行業設定 | frmPRDACCGroupMaintain | PD_I_PRD_ACCGroupMaintain.dll | PDI03 | 0 |
| I_EQUIPMENT RELEASE | 產品設備限制維護作業 | 封測行業設定 | frmPRDEquipmentRelease | PD_I_PRD_EQUIPMENTRELEASE.DLL | PDI01 | 0 |
| B_CHOOSE WAFER RULES | 挑片規則設定 | 挑片規則 | frmChooseWaferRules | PD_B_CHOOSEWAFERRULES.DLL | PDB01 | 0 |
| B_PRD BASIS_IC | 產品設定_封測 | 產品設定 | frmProductBasisPKG | PD_B_ProductBasis_IC_PKG.DLL | PDB02 | 0 |
| B_TEST PROGRAM FILE MANAGEMENT | 測試程式檔案管理 | 產品文件管理 | frmTestProgramFileManagement | PD_B_TestProgramFileManagement.dll | PDB05 | 0 |
| MODULE BASIS | 製程模組資料 | 產品設定 | frmModuleBasis | PD_ModuleBasis.dll | PD103 | 0 |
| PRD QCITEM SPEC IMPORT | 產品品管項目規格匯入 | 產品設定 | frmPRDQCSPECImport_THEIL_BU1 | PD_ProductQCSPECImport_THEIL_BU1.dll | PD116 | 0 |
| PRDGBOMEXPORT_OLD | 產品GBOM匯出_ | 產品設定 | frmGBOMEx | UR_GBOMEx.dll | PRD99 | 0 |
| PRD BASIS | 產品基本資料 | 產品設定 | frmProductBasis_THEIL_BU1 | PD_ProductBasis_THEIL_BU1.dll | PD102 | 0 |
| PRDGBOMEXPORT | 產品GBOM匯出 | 未掛入 MenuItem | frmGBOMEx | UR_GBOMEx.dll | PRD99 | 0 |
| PRD GBOM IMPORT | GBOM批次匯入 | 產品設定 | frmMultGBOMImport_THEIL_BU1 | PD_GBOMImport_THEIL_BU1.dll | PD117 | 0 |
| PD_BATCHMODIFYAREA | PD_BatchModifyArea | 產品設定 | frmBatchModifyArea | PD_BatchModifyArea_THEIL.dll |  | 0 |
| PRD OP RULE | 產品作業站法則維護 | 產品設定 | frmPRDOPRule_THEIL_BU1 | PD_PRDOPRule_THEIL_BU1.dll |  | 0 |
| PDCUSTOMERDEVICE | 客戶DEVICE | 未掛入 MenuItem | frmCustomerDevice | PD_CustomerDevice_THEIL_BU1.dll |  | 0 |

### OE — 工單管理（順序 9）

分類：訂單 [RO]、工單 [MO]、行業包 [INDUSTRY]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_MO CHOOSE WAFER | 工單挑片資料查詢 | 行業包 | frmMOChooseWafer | OE_B_MOCHOOSEWAFER.DLL | OEB01 | 0 |
| MO BASIS | 工單基本資料 | 工單 | frmMOBasis_THEIL_BU1 | OE_MOBasis_THEIL_BU1.dll | OE201 | 0 |
| MO CLOSE | 工單結案 | 工單 | frmMOClose | OE_MOClose.dll | OE208 | 0 |
| MO COMPDATA MAINTAIN | 工單元件資料維護作業 | 工單 | frmMOComponentMaintain | OE_MOComponentMaintain.dll | OE209 | 0 |
| MO DELETE | 工單刪除 | 工單 | frmMODelete | OE_MODelete.dll | OE202 | 0 |
| MO MATERIAL LIST | 工單用料清單 | 工單 | frmMOMaterialList | OE_MOMaterialList.dll | OE206 | 0 |
| MO MATERIAL STATUS | 工單存料現況 | 工單 | frmMOMaterialState | OE_MOMaterialState.dll | OE207 | 0 |
| MO QUERY | 工單查詢 | 工單 | frmMOQuery | OE_MOQuery.dll | OE203 | 0 |
| MO QUERY2 | 工單達交率(準時開工) | 工單 | frmMOQuery2 | OE_MOQuery2.dll | OE204 | 0 |
| MO QUERY3 | 工單達交率(準時完工) | 工單 | frmMOQuery3 | OE_MOQuery3.dll | OE205 | 0 |
| P_MO BASIS PKG | 工單開立_晶片測試 | 行業包 | frmMOBasis_IC_CP_PKG | OE_P_MOBasis.dll | OEP01 | 0 |
| RO BASIS | 訂單基本資料 | 訂單 | frmROBasis | OE_ROBasis.dll | OE101 | 0 |
| RO CLOSE | 訂單結案 | 訂單 | frmROClose | OE_ROClose.dll | OE102 | 0 |

### WIP — 在製品管理（順序 10）

分類：在製品 [WIP]、芯片測試生產管理 [WIP_IC_CP_PKG]、封測生產管理 [WIP_PKG]、生產報工 [LOTEXECUTE]、設備與載具 [EQP_CARRIER]、外包處理 [OUT SOURCE]、異常與等待 [HOLD AND WAIT]、包裝與庫房 [PACK AND INV]、8D 管理 [8D]、例外處理 [EXCEPTION]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| WIPINV_CS | 入庫作業 | 包裝與庫房 | frmWP_INV_THEIL_BU1 | WP_INV_THEIL_BU1.dll |  | 0 |
| BR_L_COOS_GEN | BR_L_COOS_GEN | 未掛入 MenuItem | frmBR_L_COOS_GEN | BR_L_COOS_GEN.dll |  | 0 |
| LOT OP CONT ATTRIB ADJUST | 生產批作業站製程特性值調整 | 例外處理 | frmCont_Attrib_Adjust | WP_Cont_Attrib_Adjust.dll | WP912 | 0 |
| LOT OP CONT MATERIAL ADJUST | 生產批作業站用料記錄調整 | 例外處理 | frmCont_Material_Adjust | WP_Cont_Material_Adjust.dll | WP911 | 0 |
| LOT OPERATOR CLOCKIN | 使用者批次上工作業 | 在製品 | frmLotOperatorClockIn | WP_LotOperatorClockIn.dll | WP107 | 0 |
| LOT OPERATOR CLOCKOUT | 使用者批次下工作業 | 在製品 | frmLotOperatorClockOut | WP_LotOperatorClockOut.dll | WP108 | 0 |
| LOT PROPERTY ADJUST | 生產批屬性調整 | 例外處理 | frmLotPropertyAdjust | WP_LotPropertyAdjust.dll | WP913 | 0 |
| LOT QTY ADJUST | 生產批數量調整 | 例外處理 | frmLotQtyAdjustSelect | WP_LotQtyAdjustSelect.dll | WP907 | 0 |
| LOT SPLIT | 分批處理 | 生產報工 | frmBR_L_Split_GEN_THEIL_BU1 | BR_L_Split_GEN_THEIL_BU1.dll | WP207 | 0 |
| LOT SPLIT_BIN | 分批處理_等級 | 生產報工 | frmBR_LB_Split_FT | BR_LB_Split_FT.dll | WP209 | 0 |
| LOT SPLIT_CARRIER | 分批處理_載具 | 生產報工 | frmBR_CL_Split_GEN | BR_CL_Split_GEN.dll | WP210 | 0 |
| LOT SPLIT_COMP | 分批處理_元件 | 生產報工 | frmBR_LC_Split_GEN | BR_LC_Split_GEN.dll | WP208 | 0 |
| MOMATERIALRETURNERP_T100 | 工單退料至T100 ERP | 未掛入 MenuItem | frmMOMaterialReturnERP_T100 | WP_MOMaterialReturnERP_T100.dll | WP617 | 0 |
| MOMATERIALRETURNERP_TP | 工單退料至TipTop ERP | 未掛入 MenuItem | frmMOMaterialReturnERP_TP | WP_MOMaterialReturnERP_TP.dll | WP612 | 0 |
| MOMATERIALRETURNERP_WF | 工單退料至WorkFlow ERP | 未掛入 MenuItem | frmMOMaterialReturnERP_WF | WP_MOMaterialReturnERP_WF.dll | WP615 | 0 |
| OPERATOR CLOCKIN | 使用者登入作業(上工) | 在製品 | frmOperatorClockIn | WP_OperatorClockIn.dll | WP104 | 0 |
| OPERATOR CLOCKOUT | 使用者登出作業(下工) | 在製品 | frmOperatorClockOut | WP_OperatorClockOut.dll | WP105 | 0 |
| OPERATOR LOGIN_LOT | 使用者多人加工登錄_生產批 | 在製品 | frmOperatorLogin_Lot | WP_OperatorLogIn_Lot.dll | WP111 | 0 |
| OPERATOR ONLINE | 使用者登入(上工)資訊 | 在製品 | frmOperatorOnline | WP_OperatorOnline.dll | WP106 | 0 |
| OS CONFIRM | 外包單確認 | 外包處理 | frmWIPOSConfirm | WP_OSConfirm.dll | WP402 | 0 |
| OS CREATE | 外包單開立 | 外包處理 | frmWIPOS | WP_OS.dll | WP401 | 0 |
| OS RETURN | 外包回貨 | 外包處理 | frmWIPOSUnReturn | WP_OSUnReturn.dll | WP403 | 0 |
| PACKING_MOLINE | 包裝作業 | 包裝與庫房 | frmBR_L_CO_Packing_MOLine | BR_L_CO_Packing_MOLine.dll | WP601 | 0 |
| PACKING_MOLINE LABEL PRINT | 包裝標籤補列印 | 未掛入 MenuItem | frmPacking_MOLine_LabelPrint | WP_Packing_MOLine_LabelPrint.dll | WP602 | 0 |
| PRODUCT VERSION CHANGE | 產品版本變更 | 例外處理 | frmProductVersionChange | WP_ProductVersionChange.dll | WP905 | 0 |
| REVERSE LOT | 回復處理 | 例外處理 | frmReverseHistory | WP_ReverseHistory_THEIL_BU1.dll | WP901 | 0 |
| RUN CARD | 列印流程卡 | 在製品 | frmBRRunCard_General | WP_BRRunCard_General.dll | WP103 | 0 |
| SCRAPSTOCKIN_E10ICD | 不良品入庫至E10ICD ERP | 未掛入 MenuItem | frmInventory_ScrapStockIN_E10ICD | WP_Inventory_ScrapStockIN_E10ICD.dll | WP622 | 0 |
| SCRAPSTOCKIN_T100 | 不良品入庫至T100 ERP | 未掛入 MenuItem | frmInventory_ScrapStockIN_T100 | WP_Inventory_ScrapStockIN_T100.dll | WP618 | 0 |
| SCRAPSTOCKIN_TP | 不良品入庫至Tip Top ERP | 未掛入 MenuItem | frmInventory_ScrapStockIN_TP | WP_Inventory_ScrapStockIN_TP.dll | WP611 | 0 |
| SEMI INV | 半成品庫帳務維護 | 包裝與庫房 | frmInventory_SEMI | WP_Inventory_SEMI.dll | WP604 | 0 |
| SWR BASIS | 特殊生產需求維護 | 在製品 | frmWIPSWRBasis | WP_SWRBasis.dll | WP109 | 0 |
| TRANSFER FACTORY | 換廠作業 | 例外處理 | frmTransferFactory | WP_TransferFactory.dll | WP909 | 0 |
| WAIT BASIS | 等待單資料 | 異常與等待 | frmWaitBasis | WP_WaitBasis.dll | WP510 | 0 |
| WIP ERF UNRELEASED | 未解除異常單 | 異常與等待 | frmERFUnReleased_THEIL_BU1 | WP_ERFUnReleased_THEIL_BU1.dll | WP509 | 0 |
| WIP INV | 在製品庫帳務維護 | 包裝與庫房 | frmInventory_WIP_THEIL_BU1 | WP_Inventory_WIP_THEIL_BU1.dll | WP603 | 0 |
| WIP WAIT DISPOSITION | 等待處置 | 異常與等待 | frmWaitDisposition_THEIL_BU1 | WP_WaitDisposition_THEIL_BU1.dll | WP511 | 0 |
| MTL PRINT HISTORY AND REPRINT | 物料列印歷程查詢及補印作業 | 未掛入 MenuItem | frmMTLPrtHisAndRePrt | WP_MTLPrtHisAndRePrt.dll | WP625 | 0 |
| PRD PRINT HISTORY AND REPRINT | 產品列印歷程查詢及補印作業 | 未掛入 MenuItem | frmPRDPrtHisAndRePrt | WP_PRDPrtHisAndRePrt.dll | WP624 | 0 |
| FGDSTOCKIN_TTICD | 入庫至TipTopICD ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_TTICD | WP_Inventory_FGDStockIN_TTICD.dll | WP626 | 0 |
| FGDSTOCKIN_TTICD_CANCEL | 取消入庫至TipTopICD ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_TTICD_Cancel | WP_Inventory_FGDStockIN_TTICD_Cancel.dll | WP628 | 0 |
| OS CONFIRM_ERP_CANCEL | 取消外包出貨 ERP | 外包處理 | frmWIPOSConfirm_ERP_Cancel | WP_OSConfirm_ERP_Cancel.dll | WP404 | 0 |
| SCRAPSTOCKIN_TTICD | 不良品入庫至TipTopICD ERP | 未掛入 MenuItem | frmInventory_ScrapStockIN_TTICD | WP_Inventory_ScrapStockIN_TTICD.dll | WP627 | 0 |
| CLOSE LOT FORCIBLY | 生產批強制結批作業 | 例外處理 | frmWIPCloseLotForcibly | WP_CloseLotForcibly.dll | WP916 | 0 |
| EQP MOUNT MATERIAL | 設備上料 | 設備與載具 | frmEQPMaterialLoading | WP_EQPMaterialLoading.dll | WP301 | 0 |
| PACKING_MOLINE_INV | 在製品庫包裝作業 | 包裝與庫房 | frmBR_L_CO_Packing_MOLine_INV | BR_L_CO_Packing_MOLine_INV.dll | WP629 | 0 |
| UNDO DISPATCH | 工單撤派工 | 在製品 | frmUndoDispatch_THEIL_BU1 | WP_UndoDispatch_THEIL_BU1.dll | WP112 | 0 |
| WORKORDER PRINT | 工令單列印 | 在製品 | frmWP_WorkorderPrint_THEIL_BU1 | WP_WorkorderPrint_THEIL_BU1.dll |  | 0 |
| Skip Inspection Setting | 免檢批設定 | 生產報工 | frmWP_SkipInspectionSetting_THEIL_BU1 | WP_SkipInspectionSetting_THEIL_BU1.dll |  | 0 |
| LOT SHEET PRINT | LOT SHEET 列印 | 在製品 | frmBRRunCard_General_THEIL_BU1 | WP_BRRunCard_General_THEIL_BU1.dll | WP113 | 0 |
| PRODUCTION STATE BY AREA | 設備狀態監控_區域 | 設備與載具 | frmAreaShowWIP | WP_AreaShow.dll | WP307 | 0 |
| P_BUMP MAP MERGE | Bump MAP疊圖作業 | 芯片測試生產管理 | frmMergeStandardMAP | WP_P_MergeStandardMAP.dll | WPP07 | 0 |
| 8D BASIS_FACTORY | 廠內 8D 矯正單 | 8D 管理 | frmWP8DBasisFactory | WP_8DBasis_Factory.dll | WP801 | 0 |
| 8D CLOSE | 8D 矯正單結案作業 | 8D 管理 | frmWP8DClose | WP_8DClose.dll | WP803 | 0 |
| 8D MAINTAIN | 8D 矯正單維護作業 | 8D 管理 | frmWP8DMaintain | WP_8DMaintain.dll | WP802 | 0 |
| 8D QUERY | 8D 矯正單查詢 | 8D 管理 | frmWP8DQuery | WP_8DQuery.dll | WP804 | 0 |
| CARRIER MANAGEMENT COMP | 載具管理_元件 | 設備與載具 | frmCarrierManagement | WP_CarrierManagement.dll | WP308 | 0 |
| CARRIER STATE | 載具現況管理 | 設備與載具 | frmCarrierState | WP_CarrierState.dll | WP302 | 0 |
| CHANGELOTPRODUCT | 生產批產品變更作業 | 例外處理 | frmChangeLotProduct | WP_ChangeLotProductOperation.dll | WP915 | 0 |
| CREATE LOT | 生產批開立 | 在製品 | frmLotBasis | WP_LotBasis.dll | WP101 | 0 |
| CREATE LOT_BATCH | 生產批開立_批次 | 在製品 | frmLotBasisDef_Batch_THEIL_BU1 | WP_LotBasisDef_Batch_THEIL_BU1.dll | WP102 | 0 |
| DELETE BATCH LOT | 批次刪除已下線生產批 | 例外處理 | frmDELBatchLot | WP_DelBatchLot.dll | WP903 | 0 |
| DELETE RELEASED LOT | 刪除已下線生產批 | 例外處理 | frmDELReleasedLot | WP_DelReleasedLot.dll | WP902 | 0 |
| ECN WIZARD | ECN 設定精靈 | 例外處理 | frmECNWizard | WP_ECNWizard.dll | WP906 | 0 |
| EQP CONSUME STATUS | 設備耗品現況 | 設備與載具 | frmEQPConsumeStatus | WP_EQPConsumeStatus.dll | WP303 | 0 |
| EQP CONSUME STATUS LOG | 設備耗品歷程 | 設備與載具 | frmEQPConsumeStatusLog | WP_EQPConsumeStatusLog.dll | WP304 | 0 |
| EQP FAVORITE SETUP | 過帳設備設定 | 生產報工 | frmEquipmentFavorite | WP_EquipmentFavorite.dll | WP203 | 0 |
| EQP QCLIST LOG | 設備點檢歷程 | 設備與載具 | frmEQPQCListLog | WP_EQPQCListLog.dll | WP306 | 0 |
| EQP QCLIST ONLINE CHECK | 設備線上點檢 | 設備與載具 | frmEQPCheckQCList | WP_EQPCheckQCList.dll | WP305 | 0 |
| ERF BASIS | 異常單資料 | 異常與等待 | frmERFBasis | WP_ERFBasis.dll | WP508 | 0 |
| EXECUTE LOT | 生產批執行 | 生產報工 | frmWIPLotSelect_THEIL_BU1 | WP_LotSelect_THEIL_BU1.dll | WP201 | 0 |
| EXECUTE LOT_COMP | 生產批執行_元件 | 生產報工 | frmWIPLotSelectbyComp | WP_LotSelectbyComp.dll | WP202 | 0 |
| EXECUTE LOT_EQP | 生產批執行_設備 | 生產報工 | frmEquipmentLotState | WP_EquipmentLotState.dll | WP204 | 0 |
| FGD INV | 成品庫帳務維護 | 包裝與庫房 | frmInventory_FGD | WP_Inventory_FGD.dll | WP605 | 0 |
| FGDSTOCKIN_E10ICD | 入庫至E10ICD ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_E10ICD | WP_Inventory_FGDStockIN_E10ICD.dll | WP621 | 0 |
| FGDSTOCKIN_E10ICD_CANCEL | 取消入庫 E10ICD ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_E10ICD_Cancel | WP_Inventory_FGDStockIN_E10ICD_Cancel.dll | WP623 | 0 |
| FGDSTOCKIN_T100 | 入庫至T100 ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_T100 | WP_Inventory_FGDStockIN_T100.dll | WP616 | 0 |
| FGDSTOCKIN_T100_CANCEL | 取消入庫 T100 ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_T100_Cancel | WP_Inventory_FGDStockIN_T100_Cancel.dll | WP620 | 0 |
| FGDSTOCKIN_TP | 入庫至TipTop ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_TP | WP_Inventory_FGDStockIN_TP.dll | WP609 | 0 |
| FGDSTOCKIN_TP_CANCEL | 取消入庫至TipTop ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_TP_Cancel | WP_Inventory_FGDStockIN_TP_Cancel.dll | WP610 | 0 |
| FGDSTOCKIN_WF | 入庫至WorkFlow ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_WF | WP_Inventory_FGDStockIN_WF.dll | WP614 | 0 |
| FGDSTOCKIN_WF_CANCEL | 取消入庫 WorkFlow ERP | 未掛入 MenuItem | frmInventory_FGDStockIN_WF_Cancel | WP_Inventory_FGDStockIN_WF_Cancel.dll | WP619 | 0 |
| FUTURE HOLD RELEASE | 預先異常解除 | 異常與等待 | frmFutureHoldRelease | WP_FutureHoldRelease.dll | WP506 | 0 |
| FUTURE HOLD SET | 預先異常設定 | 異常與等待 | frmFutureHoldSet | WP_FutureHoldSet.dll | WP504 | 0 |
| FUTURE WAIT RELEASE | 預先等待解除 | 異常與等待 | frmFutureWaitRelease | WP_FutureWaitRelease.dll | WP507 | 0 |
| FUTURE WAIT SET | 預先等待設定 | 異常與等待 | frmFutureWaitSet | WP_FutureWaitSet.dll | WP505 | 0 |
| IMMEDIATE HOLD SET | 立即異常設定 | 異常與等待 | frmImmediateHoldSet | WP_ImmediateHoldSet.dll | WP501 | 0 |
| IMMEDIATE HOLD SET_BATCH | 批次立即異常設定 | 異常與等待 | frmImmediateHoldSet_Batch | WP_ImmediateHoldSet_Batch.dll | WP503 | 0 |
| IMMEDIATE WAIT SET | 立即等待設定 | 異常與等待 | frmImmediateWaitSet | WP_ImmediateWaitSet.dll | WP502 | 0 |
| INV COUNT ADJUST | 盤點調帳 | 未掛入 MenuItem | frmInvCountStateAdjust | WP_InvCountStateAdjust.dll | WP608 | 0 |
| INV COUNT PRINT | 列印盤點清冊 | 未掛入 MenuItem | frmInvCountStatePrint | WP_InvCountStatePrint.dll | WP607 | 0 |
| INV COUNT STATE | 盤點關/開帳 | 未掛入 MenuItem | frmInvCountState | WP_InvCountState.dll | WP606 | 0 |
| IPQC | 計數不良原因收集 | 生產報工 | frmIPQC | WP_IPQC.dll | WP206 | 0 |
| LABEL PRINT | 標籤列印 | 例外處理 | frmLabelPrint | WP_LabelPrint.dll | WP908 | 0 |
| LOT CHOICE EQP | 生產批選用生產設備 | 生產報工 | frmLotChoiceEqp | WP_LotChoiceEqp.dll | WP205 | 0 |
| LOT COMP SPLIT | 元件分片處理 | 生產報工 | frmSplitComponent | WP_SplitComponent.dll | WP215 | 0 |
| LOT HOTLOT AND PRIPORITY CHANGE | 生產批緊急狀態/優先權調整 | 例外處理 | frmWIPLot_HotLotAndPriorityChange | WP_HotLotAndPriorityChange.dll | WP914 | 0 |
| LOT MERGE | 併批處理 | 生產報工 | frmBR_L_Merge_GEN | BR_L_Merge_GEN.dll | WP211 | 0 |
| BARTENDER | Bartender print | 包裝與庫房 | frmWP_BartenderPrint_THEIL_BU1 | WP_BartenderPrint_THEIL_BU1.dll |  | 0 |
| WIPUNINV | 取消入庫作業 | 包裝與庫房 | frmWP_UNINV_THEIL_BU1 | WP_UNINV_THEIL_BU1.dll |  | 0 |
| LOT MERGE_BIN | 併批處理_等級 | 生產報工 | frmBR_LB_Merge_FT | BR_LB_Merge_FT.dll | WP213 | 0 |
| LOT MERGE_CARRIER | 併批處理_載具 | 生產報工 | frmBR_CL_Merge_GEN | BR_CL_Merge_GEN.dll | WP214 | 0 |
| LOT MERGE_COMP | 併批處理_元件 | 生產報工 | frmBR_LC_Merge_GEN | BR_LC_Merge_GEN.dll | WP212 | 0 |
| LOT OP AREA CHANGE | 生產批作業區變更 | 例外處理 | frmLotOPAreaChange | WP_LotOPAreaChange.dll | WP904 | 0 |
| P_BUMPMAP FILE MANAGEMENT | Bump MAP 檔案管理 | 芯片測試生產管理 | frmBumpMAPFileManage | WP_P_BumpMAPFileManage.dll | WPP06 | 0 |
| P_CREATE LOT_CP | 生產批開立_晶片測試 | 芯片測試生產管理 | frmLotBasis_CP | WP_P_LOTBASIS.DLL | WPP01 | 0 |
| P_EQP SETUP BASIS | 設備設置單維護_晶片測試 | 芯片測試生產管理 | frmEQPSetupBasis_CP | WP_P_EQPSetupBasis.dll | WPP03 | 0 |
| P_GOLDENMAP FILE MANAGEMENT | Golden MAP檔案管理 | 芯片測試生產管理 | frmGoldenMAPFileManage | WP_P_GoldenMAPFileManage.dll | WPP05 | 0 |
| P_LOT COMP GEN | 生產批自主產生元件 | 芯片測試生產管理 | frmLotComponentGen | WP_P_LotComponentGen.dll | WPP08 | 0 |
| P_PROBING LOG | 晶圓針測紀錄查詢 | 芯片測試生產管理 | frmProbingLog | WP_P_ProbingLog.dll | WPP04 | 0 |
| P_WAFERMAP EXPORT | 晶圓針測資料匯出 | 芯片測試生產管理 | frmWIPWaferExport | WP_P_WAFEREXPORT.DLL | WPP02 | 0 |
| I_EQP ACCESSORY BASIS | 配件領用單維護作業 | 封測生產管理 | frmEQPAccessoryBasis | WP_I_EQP_AccessoryBasis.dll | WPI05 | 0 |

### EMS — 設備管理（順序 11）

分類：EMS設定 [EMS CONFIG]、設備_執行 [EQP EXECUTION]、配件_執行 [ACC EXECUTION]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| EQP STATE CHANGE | 改變設備狀態 | 設備_執行 | frmEquipmentStateChange_THEIL_BU1 | ES_EquipmentStateChange_THEIL_BU1.dll | ES204 | 0 |
| EQP STATE CONTROL SETTING | 設備狀態控制設定 | EMS設定 | frmEQPStateControlSet | ES_EQPStateControlSet.dll | ES102 | 0 |
| EQP STATE STATISTICS_EMS | 設備狀態期間統計查詢 | 配件_執行 | frmEQPStateStatistics | RT_EQPStateStatistics.dll | ES409 | 0 |
| EQUIPMENT REASON BASIS | 設備狀態變更原因資料 | EMS設定 | frmEquipmentReasonBasis | ES_EquipmentReasonBasis.dll | ES106 | 0 |
| EQUIPMENT STATE LOG | 設備狀態記錄查詢 | 配件_執行 | frmEquipmentStateLog | ES_EquipmentStateLog.dll | ES402 | 0 |
| RETURN EQP ACCESSORY | 配件下機 | 配件_執行 | frmReturnEQPAccessory | ES_ReturnEQPAccessory.dll | ES302 | 0 |
| SHOW EQP LOG | 設備歷史資料查詢 | 配件_執行 | frmEquipmentStateLogQuery | ES_EquipmentStateLogQuery.dll | ES404 | 0 |
| SHOW EQP LOG BY STATE | 設備歷史資料查詢_狀態 | 配件_執行 | frmEquipmentStateLogByState | ES_EquipmentStateLogByState.dll | ES403 | 0 |
| SHOW EQP STATUS BY AREA | 設備狀態監控_區域 | 設備_執行 | frmAreaShowEMS_THEIL_BU1 | ES_AreaShow_THEIL_BU1.dll | ES203 | 0 |
| SHOW EQP STATUS BY TYPE | 設備狀態監控_設備類別 | 設備_執行 | frmEquipmentStateQueryByType_THEIL_BU1 | ES_EquipmentStateQueryByType_THEIL_BU1.dll | ES202 | 0 |
| VIOLATIVE STATE SETTING | 設備狀態互斥設定 | EMS設定 | frmViolateStateChangeSetMatrix | ES_ViolateStateChangeSetMatrix.dll | ES104 | 0 |
| ACC MONITOR | 配件狀態監控 | 配件_執行 | frmAccessoryStateMonitor | ES_AccessoryStateMonitor.dll | ES304 | 0 |
| ACC MONITOR BY EQP | 配件狀態監控_設備 | 配件_執行 | frmAccessoryStateMonitorByEQP | ES_AccessoryStateMonitorByEQP.dll | ES305 | 0 |
| ACC STATE CONTROL SETTING | 配件狀態控制設定 | EMS設定 | frmEQPACCStateControlSet | ES_EQPACCStateControlSet.dll | ES103 | 0 |
| ACC STATE LOG QUERY | 配件歷史資料查詢 | 配件_執行 | frmAccessoryStateLog | ES_AccessoryStateLog.dll | ES406 | 0 |
| ACC STATE QUERY | 配件狀態查詢 | 配件_執行 | frmAccessoryState | ES_AccessoryState.dll | ES405 | 0 |
| ACC MONITOR_BU1 | 配件狀態監控_管理 | 配件_執行 | frmAccessoryStateMonitor | ES_AccessoryStateMonitor_THEIL_BU1.dll |  | 0 |
| ACC VIOLATIVE STATE SETTING | 配件狀態互斥設定 | EMS設定 | frmEQPACCStateControl | ES_EQPACCViolateStateConfig.dll | ES105 | 0 |
| ACCESSORY REPAIR | 配件壽命維護 | 配件_執行 | frmAccessoryRepair | ES_AccessoryRepair.dll | ES303 | 0 |
| APPLY EQP ACCESSORY | 配件上機 | 配件_執行 | frmApplyEQPAccessory | ES_ApplyEQPAccessory.dll | ES301 | 0 |
| EMS CONTROL | 設備控管設定 | EMS設定 | frmEquipmentControl | ES_EquipmentControl.dll | ES101 | 0 |
| EQP ACC STATE LOG QUERY | 設備配件歷史資料查詢 | 配件_執行 | frmEquipmentAccStateLog | ES_EquipmentAccStateLog.dll | ES408 | 0 |
| EQP ACC STATE QUERY | 設備配件現況查詢 | 配件_執行 | frmEquipmentAccState | ES_EquipmentAccState.dll | ES407 | 0 |
| EQP CAP BASIS | 設備能力項目設定 | EMS設定 | frmEQPCapBasis | ES_EQPCapBasis.dll | ES107 | 0 |
| EQP CAP MAPPING | 設備能力設定 | EMS設定 | frmEQPCapMapping | ES_EQPCapMapping.dll | ES108 | 0 |
| ACC STATE CHANGE DESCRIPTION | 配件狀態變更原因描述 | 未掛入 MenuItem | frmACCStateChangeDescription | ES_ACCStateChangeDescription.dll |  | 0 |


公司端復刻進度（2026-11-23）：WP916 後依清冊順序略過已完成 WP301、WP629、WP112、LOT SHEET PRINT（WP113）及 PRODUCTION STATE BY AREA；WORKORDER PRINT、無 FunctionKey 的 Skip Inspection Setting、P_BUMP MAP MERGE（WPP07）、FGDSTOCKIN_E10ICD（WP621）、SCRAPSTOCKIN_E10ICD（WP622）、FGDSTOCKIN_E10ICD_CANCEL（WP623）、FGDSTOCKIN_T100（WP616）及 FGDSTOCKIN_T100_CANCEL（WP620）、FGDSTOCKIN_TP（WP609）與 FGDSTOCKIN_TP_CANCEL（WP610）、FGDSTOCKIN_WF（WP614）及 FGDSTOCKIN_WF_CANCEL（WP619）、INV COUNT ADJUST（WP608）、INV COUNT PRINT（WP607）、INV COUNT STATE（WP606）、BARTENDER（Bartender print）、WIPUNINV（取消入庫作業）、WPP06（Bump MAP 檔案管理）均已完成 DEV、規格與家用端 handoff。WPP07 六張 CP MAP state/file/template 表、WP621／623／620／610 的 `TBLINVFGDINBIN`、WP614 的 `TBLINVWIP_SCRAP`／`TBLOEMOMATERIALINBASIS_ERP`／`TBLERPRETHROWTICKET`、WP622／WP619 的 `TBLINVSCRINVENTORY`／`TBLINVSCRINVENTORYCOMPONENT`／`TBLINVSCRINVENTORYFAILBIN`、WP608／WP607／WP606 共用的四張盤點 state/detail 表、BARTENDER `TBLINLABEL_TAG_FILE_MAP` 及 WPP06 `TBL_PWIPBumpMAPFileState`／`TBL_PWIPBumpMAPFileLog`／`TBL_BSYSMAPTemplateBasis`／`TBL_BSYSMAPTemplateDetail` 缺 Oracle DDL／metadata；桌機受保護 Oracle attachment/profile 不可用，未猜 CREATE-only。外部 BarTender 執行檔不在可讀 checkout；THEIL Form 含 SFTP 下載程式，但未執行；InLabel／LabelApi server handlers 不在可讀 checkout。ERP／MES 交易、補償、State=-2 恢復及舊流程 `OrgState` 覆寫差異均有記錄；WP620／WP610／WP619 查詢沒有 ERP source filter，家用端須補來源守門。FUTURE／IMMEDIATE HOLD／WAIT、IPQC、LABEL PRINT、LOT CHOICE EQP、LOT COMP SPLIT、LOT HOTLOT、LOT MERGE 與其 BIN／CARRIER／COMP 變體已完成。WIPUNINV 只有 API consumer，未找到 server controller／Service／Core 或 SAP 交易實作；來源沒有直接 Oracle 表名／SQL，家用端須核實 endpoint、授權、冪等及逾時對帳。WPP01「P_CREATE LOT_CP／生產批開立_晶片測試」與 WPP03「P_EQP SETUP BASIS／設備設置單維護_晶片測試」已完成；WPP05「P_GOLDENMAP FILE MANAGEMENT／Golden MAP檔案管理」已完成公司端 DEV／規格／handoff；WPP02「P_WAFERMAP EXPORT／晶圓針測資料匯出」及 WPP04「P_PROBING LOG／晶圓針測紀錄查詢」均已完成 DEV／規格／handoff；WPP08「P_LOT COMP GEN／生產批自主產生元件」已完成 DEV／規格／handoff；WPP01–WPP08 CP 功能均已完成公司端交付。原始 FunctionOrder 未保存在可讀清冊，桌機受保護 Oracle profile 不存在；WPP02、WPP04、WPP08 依 FunctionKey 數值接續，日後有安全 profile 再核實實際排序。

### SPC — 品質管理（順序 12）

分類：SPC管制 [SPC CONTROL]、SPC分析 [SPC ANALYSIS]、SPC報表 [SPC REPORT]、SPC異常單 [SPC ERF]、SPC檢測設備 [SPC INSPECTION MACHINE]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| SPCDATAEDIT | 計量資料修改權限 | 未掛入 MenuItem |  |  |  | 0 |
| SPCDATAEDITATT | 計數資料修改權限 | 未掛入 MenuItem |  |  |  | 0 |
| ATT-CHART | 計數型管制圖 | SPC分析 | frmQueryAttChart | SC_QueryAttChart.dll | SC212 | 0 |
| CHART ALL CAPABILITY | 製程能力報表 | SPC報表 | frmQueryChartAllCapability | SC_QueryChartAllCapability.dll | SC301 | 0 |
| CHART ALL CAPABILITY BY FACTOR | 製程能力報表_以要因查詢 | SPC報表 | frmQueryChartAllCapabilityByFactor | SC_QueryChartAllCapabilityByFactor.dll | SC311 | 0 |
| CHART LINE EDIT LOG | 管制線修改紀錄 | SPC分析 | frmChartLineEditLog | SC_ChartLineEditLog.dll | SC215 | 0 |
| CHART SIGNBOARD | 管制圖看板 | SPC分析 | frmQueryChartSignBoard | SC_QueryChartSignBoard.dll | SC205 | 0 |
| CONTROL CHART ANALYSIS | 管制圖分析 | SPC分析 | frmQueryBarControlChart | SC_QueryBarControlChart.dll | SC206 | 0 |
| CONTROL CHART ANALYSIS BY FACTOR | 管制圖分析_以要因查詢 | SPC分析 | frmQueryBarControlChartByFactor | SC_QueryBarControlChartByFactor.dll | SC214 | 0 |
| DATA EDIT LOG | 資料修改記錄 | SPC分析 | frmQueryDataEditLog | SC_QueryDataEditLog.dll | SC203 | 0 |
| FQC FORM | 終檢檢驗單 | SPC管制 | frmFQCForm | SC_FQCForm.dll | SC104 | 0 |
| IMPORT VAR MEASURE | 匯入計量檢測數據 | SPC管制 | frmImport_VarMeasureData | SC_Import_VarMeasureData.dll | SC107 | 0 |
| INCOMING QC FORM | 進料檢驗單 | SPC管制 | frmIncomingQCForm | SC_IncomingQCForm.dll | SC103 | 0 |
| INSP MACHINE PARAMETER SETUP | 品管項目檢測儀器對應 | SPC檢測設備 | frmInspMachineParameterSetup | SC_InspMachineParameterSetup.dll | SC501 | 0 |
| IPQC FORM | PQC 檢驗單 | SPC管制 | frmIPQCForm | SC_IPQCForm.dll | SC105 | 0 |
| IQC FORM FOR COMPONENT | 進料檢驗_元件 | SPC管制 | frmIncomingQCFormComponent | SC_IncomingQCFormComponent.dll | SC108 | 0 |
| LINE CHART ANALYSIS | 彙整分析圖 | SPC分析 | frmQueryLineBarChart | SC_QueryLineBarChart.dll | SC207 | 0 |
| LRR / PPM CHART | 拒收率 / 百萬不良數管制圖 | SPC分析 | frmQueryLRRandPPMChart | SC_QueryLRRandPPMChart.dll | SC211 | 0 |
| MONTHLY CHART REPORT | 管制圖月報表 | SPC報表 | frmQueryMnthVarChartRPT | SC_QueryMnthVarChartRPT.dll | SC303 | 0 |
| MONTHLY CHART REPORT BY FACTOR | 管制圖月報表_以要因查詢 | SPC報表 | frmQueryMnthVarChartRPTByFactor | SC_QueryMnthVarChartRPTByFactor.dll | SC312 | 0 |
| MONTHLY CHART REPORT CODE | 管制圖月報表編碼 | SPC報表 | frmMonthRPTCodeBasis | SC_MonthRPTCodeBasis.dll | SC302 | 0 |
| OQC FORM | 出貨檢驗單 | SPC管制 | frmOQCForm | SC_OQCForm.dll | SC106 | 0 |
| PARETO DIAGRAM | 柏拉圖分析 | SPC分析 | frmQueryPareto | SC_QueryPareto.dll | SC210 | 0 |
| PCHART SIGNBOARD | 不良率管制圖看板 | SPC分析 | frmQueryPChartSignBoard | SC_QueryPChartSignBoard.dll | SC209 | 0 |
| QCITEM BINDING HIS LOG | 品管項目綁定紀錄表 | SPC分析 | frmQcItemBindingHisLog | SC_QcItemBindingHisLog.dll | SC216 | 0 |
| SCATTER DIAGRAM | 散佈圖分析 | SPC分析 | frmQueryScatterChart | SC_QueryScatterChart.dll | SC208 | 0 |
| SPC COA | 出貨報告 | SPC分析 | frmSPC_COA | SC_COA.dll | SC213 | 0 |
| SPC DATA | SPC 資料維護 | SPC分析 | frmQueryData_THEIL_BU1 | SC_QueryData_THEIL_BU1.dll | SC201 | 0 |
| SPC DATA BY LOTNO | Lot 檢測資料 | SPC分析 | frmQueryDataByLotNo_THEIL_BU1 | SC_QueryDataByLotNo_THEIL_BU1.dll | SC202 | 0 |
| SPC ERF QUERY | SPC 異常單查詢 | SPC異常單 | frmSPC_ERFBasis | SC_ERFBasis.dll | SC402 | 0 |
| SPC ERF UNRELEASED | 未解除異常單 | SPC異常單 | frmSPC_ERFUnReleased | SC_ERFUnReleased.dll | SC401 | 0 |
| SPC ONLIE CONTROL | SPC 量測數據收集 | SPC管制 | frmSPCOnlineControl | SC_OnlineControl_THEIL_BU1.dll | SC101 | 0 |
| SPC ONLIE CONTROL CONTINUE | SPC 量測數據收集_計量連續 | SPC管制 | frmSPCOnlineControl_test | SC_OnlineControl_Test.dll | SC102 | 0 |
| XLS SOURCE FILE CONTROL | Excel匯出樣板 | SPC分析 | frmXlsSourceFileControl | SC_XlsSourceFileControl.dll | SC204 | 0 |

公司端復刻進度（2026-11-24）：SC101–SC108、SC201–SC216、SC301–SC303、SC311–SC312、SC401–SC402 已完成 DEV、WinForms／Service／Core 規格與家用端 handoff。SC101／SC102 共用計量執行表、SC103 `TBLSPCINCOMINGQCFORM`、SC104 `TBLSPCFQCFORM` 缺同版 Oracle DDL／metadata；SC105 主表沿用 repo WP201 reference、Map DDL／metadata 缺失；SC106 OQC Map 沿用 QC216 Oracle／PG CREATE reference，但 Header `TBLSPCOQCFORM` DDL／metadata／CREATE-only 缺失；SC107 的 `TBLSPCImportData` 只見於 Form 未呼叫的 `LoadSPCImportData`，缺 source DDL／metadata／CREATE-only；SC108 三張元件檢驗表、SC201 三張 WIP SPC 關聯表及 SC202 `TBLSPCAttDetailLogComponent` 缺同版 DDL／metadata，缺表未猜建。SC203 Attribute edit-log 有 PG CREATE-only reference；SC204／SC213 使用的 `TBLSPCXLSFILE` 缺 DDL／metadata／CREATE-only；SC206／SC207 `TBLSPCLIMIT` fallback 缺 DDL／metadata／CREATE-only；SC205–SC210 沿用既有 QC302／QC205／QC210／Attribute／Reason／Limit references。SC209 `TBLSPCATTVIOLATIONCONTENT`、SC210 `TBLSPCATTDETAILLOGSECTION`、SC211 VISERA 專用 `TBLSYSCALENDAR`、SC212 `TBLSPCATTCHARTLIMIT`，以及 SC213 `TBLSPCATTDETAILLOGCOMPONENT` 缺 Oracle DDL／metadata／PG CREATE-only；SC214 共用 QC302／QC205／QC210 references，無新增表；群組 Service／Core 未套用 QCItemNo／日期篩選，Factor SQL 需參數化及 allowlist。SC215 沿用 QC302 Basis／DetailLog references，日期由舊 Form 在重建歷程後嚴格篩選；舊 Core Customer 欄位檢查與 DDL 不符，歷程比較未按 SPCID 配對，無新增表。SC216 沿用 MTL QC Item table references；Core 父子 DataRelation 缺 ProductNo／ProductVersion，SQL 無上限且條件字串串接。SC301 沿用 QC302／QC205／QC210 references，無新增表；SC302 使用 `TBLSPCMONTHRPTCODE`，repo 缺 Oracle DDL／metadata／PostgreSQL CREATE-only，Core 使用五個欄位但不足以推導完整 schema；Core RawData 固定 Excluded=0，LotState 篩選可能不一致，Factor SQL 字串拼接、無上限且 timeout 6,000,000ms。SC303 沿用 QC302／QC205／QC210 references，`TBLSPCMONTHRPTCODE` 的 DDL／metadata 缺失；Core 同樣固定 Excluded=0，Factor 條件字串拼接且查詢無上限，Service timeout 6,000,000ms。SC401 共享 QC409／WP101／PRD／OP／MTL／Customer references；新增 `TBLWIPERFDISPOSITION` PostgreSQL CREATE-only，依既有 Oracle source DDL。Material/Vendor filter 舊 Service 漏傳，Lot Disposition 與 WP509 THEIL 客製版不同。SC402 沿用 ERF／Lot／8D references，沒有新增 schema；桌機未連 YMES PostgreSQL；下一個未完成 SPC FunctionKey 為 SC501。

最新補記（2026-11-24）：SC501「品管項目檢測儀器對應」已完成公司端 DEV／來源規格交接。SC501 僅將操作站對應寫入 `%LocalAppData%\iMESTech\InspMachineSetup.xml`；Web DEV 使用 localStorage／XML 預覽，沒有 MES 對應表或 CRUD Service。SPC 清冊目前所有 FunctionKey 均已交付；依總清冊順序接續 INV。

公司端復刻進度（2026-11-24）：INV 的 IV101「庫房工程群組」、IV102「庫房基本資料」、IV103「在製貨架基本資料」、IV201「原物料倉庫房資料」、IV202「開立進料單」、IV207「進料單記錄」、IV203「開立領料單」、IV208「領料單記錄」、IV205「開立預訂單」、IV210「預訂單記錄」、IV206「開立退料單」、IV211「退料單記錄」、IV302「開立退料單(WIP)」、IV303「退料單記錄(WIP)」及 IV304「退料單確認(WIP)」已完成公司端 DEV、WinForms／Service／Core 規格與家用端 handoff。IV202／IV205／IV206／IV302 是 State=0 單頭／明細維護；IV207／IV208／IV210／IV211／IV303 是唯讀查詢；IV203 是 State=0 草稿 CRUD，真正出庫確認仍在 IV201 Source=1；IV205 草稿不改 AllocateQty，預訂確認仍在 IV201；IV206 草稿不扣庫存，退料確認在 IV201 Source=3；IV302 草稿不扣庫存，WIP 退料確認由 IV304 Source=10 完成。IV304 會改動來源／目標 Raw／Semi 庫存與元件，Type 7 多目標分流及舊 Add Service 吞錯問題見規格。`TBLINVENGINEERGROUPBASIS`、`TBLINVInventoryBasis`、`TBLINVInventoryProperty`、`TBLINVSTOCKERBASIS`、`TBLINVRAWINVENTORY`、`TBLINVRAWINVENTORYLOC`、`TBLINVMATERIALINBASIS`、`TBLINVMATERIALINDETAIL`、`TBLINVMATERIALOUTBASIS`、`TBLINVMATERIALOUTDETAIL`、`TBLINVMATERIALOUTCOMPONENT`、`TBLINVRAWINVENTORYCOMPONENT`、`TBLINVPREORDERBASIS`、`TBLINVPREORDERDETAIL`、`TBLINVMATERIALRETURNBASIS`、`TBLINVMATERIALRETURNDETAIL`、`TBLINVMATERIALRETURNCOMPONENT`、`TBLINVMATERIALWIPRETURNBASIS`、`TBLINVMATERIALWIPRETURNDETAIL`、`TBLINVMTLWIPRETURNCOMP`、`TBLINVWIP_RAW_COMPONENT`、`TBLINVWIPINVENTORY_COMPONENT`、`TBLOEMOMTLSTATE_COMPONENT`、`TBLSPCINCOMINGQCFORM` 缺本版本可核實 DDL／metadata；桌機受保護 Oracle profile 不存在，未猜 CREATE SQL。IV206／IV302／IV303 元件查詢、關聯與多段 Service 寫入，以及 IV304 的確認交易風險見模組規格。家用端先核 live schema；明細 API 必須限定父單號，IV203／IV206／IV302 明細、元件與 BookingFlag 寫入需單一交易，IV304 Source=10 需原子過帳、狀態與交易記錄；IV310「包裝拆並箱作業」與 IV501「成品庫庫房資料」均已完成 DEV／規格交接，分別沿用 WP601 與 WP201／WP605／QC409／PRD Bin references，沒有新增資料表；IV502「開立成品/半成品入庫單」已完成；目前下一項為 IV503「成品入庫單記錄」。

公司端已完成 IV501「成品庫庫房資料」：以 `frmINVFGDInventory`／`frmINVFGDInConfirm`、`wsINV`／`wsWIP`／`wsSYS`／`clsInventory` 為來源，Vue DEV 支援成品庫查詢、Component／動態 Bin 明細及 Source=30 FGD In 假過帳。IV310 與 IV501 均有模組 MD 與家用端 handoff；IV502 也已完成，目前下一項為 IV503「成品入庫單記錄」。

最新補記（2026-11-24）：IV502「開立成品／半成品入庫單」已完成公司端 DEV、WinForms／Service／Core 規格與家用端 API handoff。單據新增、編輯、作廢只維護 Source=1 草稿及來源 BookingFlag；FGD 完整包裝限制與 SEMI 禁選包裝均已復刻，實際過帳仍由 IV501 Source=30 處理。FGD／SEMI 入庫表沿用 WP605／WP604，其他來源表沿用 WP201／603／101／215／601／PRD112 refs，無新增 DDL。下一項依清冊為 IV503「成品入庫單記錄」。

### INV — 庫存管理（順序 13）

分類：庫房設定 [INV CONFIG]、原物料倉 [RAW INV]、在製品庫 [WIP INV]、半成品庫 [SEMI INV]、成品庫 [FGD INV]、報廢倉 [SCR INV]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| CUSRECEIVE | 收料 | 原物料倉 | frmIV_CUSReceive_THEIL | IV_CUSReceive_THEIL.dll |  | 0 |
| B_RAW WAFER RECEIVING | 來料庫房管理 | 原物料倉 | frmINVWaferReceiving | IV_B_WAFERRECEIVING.DLL | IVB01 | 0 |
| MATERIAL IN | 開立進料單 | 原物料倉 | frmINVMaterialIn | IV_MaterialIn.dll | IV202 | 0 |
| MATERIAL IN LOG | 進料單記錄 | 原物料倉 | frmINVMaterialInLog | IV_MaterialInLog.dll | IV207 | 0 |
| MATERIAL OUT | 開立領料單 | 原物料倉 | frmINVMaterialOut | IV_MaterialOut.dll | IV203 | 0 |
| MATERIAL OUT LOG | 領料單記錄 | 原物料倉 | frmINVMaterialOutLog | IV_MaterialOutLog.dll | IV208 | 0 |
| MATERIAL PREORDER | 開立預訂單 | 原物料倉 | frmINVPreOrder | IV_PreOrder.dll | IV205 | 0 |
| MATERIAL PREORDER LOG | 預訂單記錄 | 原物料倉 | frmINVPreOrderLog | IV_PreOrderLog.dll | IV210 | 0 |
| MATERIAL RETURN | 開立退料單 | 原物料倉 | frmINVMaterialReturn | IV_MaterialReturn.dll | IV206 | 0 |
| MATERIAL RETURN LOG | 退料單記錄 | 原物料倉 | frmINVMaterialReturnLog | IV_MaterialReturnLog.dll | IV211 | 0 |
| RAW INVENTORY | 原物料倉庫房資料 | 原物料倉 | frmINVRawInventory | IV_RawInventory.dll | IV201 | 0 |
| SCR INVENTORY | 報廢倉庫房資料 | 報廢倉 | frmINVSCRInventory | IV_SCRInventory.dll | IV601 | 0 |
| SCR PRODUCT IN | 不良品入庫 | 報廢倉 | frmINVSCRIn | IV_SCRIn.dll | IV602 | 0 |
| SEMI IN | 開立半成品進料單 | 半成品庫 | frmINVSEMIIn | IV_SEMIIn.dll | IV404 | 0 |
| SEMI IN LOG | 半成品進料單記錄 | 半成品庫 | frmINVSEMIInLog | IV_SEMIInLog.dll | IV407 | 0 |
| SEMI INVENTORY | 半成品庫庫房資料 | 半成品庫 | frmINVSEMIInventory | IV_SEMIInventory.dll | IV401 | 0 |
| SEMI OUT | 開立半成品出庫單 | 半成品庫 | frmINVSEMIOut | IV_SEMIOut.dll | IV403 | 0 |
| SEMI OUT LOG | 半成品出庫單記錄 | 半成品庫 | frmINVSEMIOutLog | IV_SEMIOutLog.dll | IV406 | 0 |
| SEMI PRODUCT IN | 開立半成品入庫單 | 半成品庫 | frmINVProductIn | IV_ProductIn.dll | IV402 | 0 |
| SEMI PRODUCT IN LOG | 成品/半成品入庫單紀錄 | 半成品庫 | frmINVProductInLog | IV_ProductInLog.dll | IV405 | 0 |
| STOCKER BASIS | 在製貨架基本資料 | 庫房設定 | frmINVStockerBasis | IV_StockerBasis.dll | IV103 | 0 |
| STOCKER HISTORY | 在製貨架歷程查詢 | 在製品庫 | frmIV_StockerHistory | IV_StockerHistory.dll | IV307 | 0 |
| STOCKER MANAGER | 在製貨架上下架作業 | 在製品庫 | frmStockManagement | IV_StockerManager.dll | IV305 | 0 |
| STOCKER STATE | 在製貨架查詢作業 | 在製品庫 | frmINVStockerState | IV_StockerState.dll | IV306 | 0 |
| WIP INVENTORY | 在製品庫庫房資料 | 在製品庫 | frmINVWIPInventory | IV_WIPInventory.dll | IV301 | 0 |
| WIP MATERIAL RETURN | 開立退料單(WIP) | 在製品庫 | frmINVMaterialWIPReturn | IV_MaterialWIPMOReturn.dll | IV302 | 0 |
| WIP MATERIAL RETURN LOG | 退料單記錄(WIP) | 在製品庫 | frmINVMaterialWIPReturnLog | IV_MaterialWIPReturnLog.dll | IV303 | 0 |
| WIP RETURN CONFIRM | 退料單確認(WIP) | 在製品庫 | frmINVWIPReturnConfirm | IV_WIPReturnConfirm.dll | IV304 | 0 |
| WIPINV MO PACKING COMBINE DISMANTLE | 包裝拆並箱作業 | 在製品庫 | frmINVMOPackingCombineDismantle | IV_MOPackingCombineDismantle.dll | IV310 | 0 |
| FGD INVENTORY | 成品庫庫房資料 | 成品庫 | frmINVFGDInventory | IV_FGDInventory.dll | IV501 | 0 |
| FGD PRODUCT IN | 開立成品/半成品入庫單 | 成品庫 | frmINVProductIn | IV_ProductIn.dll | IV502 | 0 |
| FGD PRODUCT IN LOG | 成品入庫單記錄 | 成品庫 | frmINVProductInLog | IV_ProductInLog.dll | IV503 | 0 |
| INV BASIS FOR INV | 庫房基本資料 | 庫房設定 | frmInventoryBasis | IV_InventoryBasis.dll | IV102 | 0 |
| INV ENGINEER GROUP | 庫房工程群組 | 庫房設定 | frmINVEngineerGroupBasis | IV_EngineerGroupBasis.dll | IV101 | 0 |
| INV_RAW_ACCLIMATION | 回溫回冰作業(膠) | 原物料倉 | frmINVRaw_Acclimation | IV_Inventory_Raw_Acclimation.dll | IV212 | 0 |
| MATERIAL ADJUST | 開立庫存調整單 | 原物料倉 | frmINVMaterialAdjust | IV_MaterialAdjust.dll | IV204 | 0 |
| MATERIAL ADJUST LOG | 庫存調整單記錄 | 原物料倉 | frmINVMaterialAdjustLog | IV_MaterialAdjustLog.dll | IV209 | 0 |
| WAFERBANK | 晶圓庫存 | 原物料倉 | frmIV_WaferBank_THEIL | IV_WaferBank_THEIL.dll |  | 0 |

### PM — 維修保養管理（順序 14）

分類：PM設定 [PM CONFIG]、設備PM設定 [EQP PM CONFIG]、保養工單 [PM WO]、設備維修 [EQP MAINTENANCE]、查詢 [PM QUERY]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| XX_PMACC_WOBASISRELEASE | PM_ACC保養工單下線 | 保養工單 | frmPMWORelease | PM_WORelease_THEIL.dll | PM099 | 0 |
| XX_PMACC_WOBASIS | PM_ACC保養工單開立 | 保養工單 | frmPMACCWoBasis_THEIL | PM_ACCWOBasis_THEIL.dll | PM099 | 0 |
| BREAKDOWN_REPAIRQUERY | 故障維修登錄查詢 | 查詢 | frmBreakDown_RepairQuery | PM_BreakDown_RepairQuery.dll | PM507 | 0 |
| EQP BREAKDOWN REGISTER | 設備故障登錄 | 設備維修 | frmEQPBreakDownRegister_THEIL_BU1 | PM_EQPBreakDownRegister_THEIL_BU1.dll | PM404 | 0 |
| EQP FIXED QUANTITY SETUP | 設備定量保養設定 | 設備PM設定 | frmFixedQuantitySetUp | PM_FixedQuantitySetUp.dll | PM203 | 0 |
| EQP FIXED TIME SETUP | 設備定次定時保養設定 | 設備PM設定 | frmFixedTimesSetUp | PM_FixedTimesSetUp.dll | PM204 | 0 |
| EQP REPAIR REGISTER | 設備修復登錄 | 設備維修 | frmEQPRepairRegister_THEIL_BU1 | PM_EQPRepairRegister_THEIL_BU1.dll | PM405 | 0 |
| EQPTYPE BREAKDOWN MAP | 設備與故障原因對應 | 設備維修 | frmEQPTypeBreakDownMap | PM_EQPTypeBreakDownMap.dll | PM403 | 0 |
| EQPTYPE SOP BASIS | 設備保養標準程序 | 設備PM設定 | frmEQPTypeSOPBasis | PM_EQPTypeSOPBasis.dll | PM201 | 0 |
| XX_PMACC_WOFINISH | PM_ACC保養工單結案 | 保養工單 | frmXXPMWoFinish | PM_WOFinish_THEIL_BU1.dll |  | 0 |
| FREQUENCY BASIS | 頻率基本資料 | PM設定 | frmFrequencyBasis | PM_FrequencyBasis.dll | PM101 | 0 |
| JOB TYPE | 工作形式設定 | PM設定 | frmJobType | PM_JobType.dll | PM106 | 0 |
| PM CALENDAR | 保養日曆設定 | 查詢 | frmPMPlanCalendar | PM_PlanCalendar.dll | PM502 | 0 |
| PM ITEM | 預防保養項目資料 | PM設定 | frmMaintenanceItem | PM_MaintenanceItem.dll | PM108 | 0 |
| PM MAINTENANCE QUERY | 設備保養計畫查詢 | 查詢 | frmPMMaintenancePlanQuery | PM_MaintenancePlanQuery.dll | PM501 | 0 |
| PM PERFORMANCE QUERY | 設備保養效率查詢 | 查詢 | frmPMPerformance | PM_Performance.dll | PM506 | 0 |
| PM PLAN CHANGE | 保養計劃變更 | 設備PM設定 | frmEQPMaintenancePlanAdjust | PM_EQPMaintenancePlanAdjust.dll | PM202 | 0 |
| PM PLAN HISTORY | 保養計劃歷史資料查詢 | 查詢 | frmEQPMaintenancePlanHistory | PM_EQPMaintenancePlanHistory.dll | PM503 | 0 |
| PM TYPE | 預防保養維護方式設定 | PM設定 | frmMaintenanceType | PM_MaintenanceType.dll | PM107 | 0 |
| PMBRAEKDOWN QUERY | 故障原因統計 | 查詢 | frmBreakDownQuery | PM_BreakDownQuery.dll | PM505 | 0 |
| PMWO BASIS | 開立保養工單 | 保養工單 | frmPMWoBasis | PM_WOBasis.dll | PM301 | 0 |
| PMWO COMPLETE | 保養工單完成與回報 | 保養工單 | frmPMWoFinish | PM_WOFinish_EQM_THEIL.dll | PM303 | 0 |
| PMWO HISTORY | 保養工單歷史資料查詢 | 查詢 | frmPMWoHistory | PM_WOHistory.dll | PM504 | 0 |
| PMWO RELEASE | 保養工單下線 | 保養工單 | frmPMWORelease | PM_WORelease_EQM_THEIL.dll | PM302 | 0 |
| REPAIR BASIS | 維修項目基本資料 | 設備維修 | frmRepairBasis | PM_RepairBasis.dll | PM402 | 0 |
| SOP BASIS | 標準作業程序 | PM設定 | frmSOPBasis | PM_SOPBasis.dll | PM109 | 0 |
| SUBSTITUTION BASIS | 備品基本資料 | PM設定 | frmSubstitutionBasis | PM_SubstitutionBasis.dll | PM104 | 0 |
| SUBSTITUTION GROUP | 備品群組資料 | PM設定 | frmSubstitutionGroup | PM_SubstitutionGroup.dll | PM105 | 0 |
| SUBSTITUTION TYPE BASIS | 備品類別資料 | PM設定 | frmSubstitutionType | PM_SubstitutionType.dll | PM103 | 0 |
| SUBSTITUTION VENDOR | 備品供應商基本資料 | PM設定 | frmSubstitutionVendor | PM_SubstitutionVendor.dll | PM102 | 0 |
| TOOL BASIS | 工具基本資料 | PM設定 | frmEQPToolBasis | EP_ToolBasis.dll | PM110 | 0 |
| BREAKDOWN BASIS | 故障原因基本資料 | 設備維修 | frmBreakDownBasis | PM_BreakDownBasis.dll | PM401 | 0 |
| PM_ACCPLAN | PM_ACCPLAN | 保養工單 | frmACCMaintenancePlan | PM_ACCMaintenancePlan_THEIL_BU1.dll | PM403 | 0 |
| PM_ACCPLANADJ | PM_ACCPLANADJ | 保養工單 | frmACCMaintenancePlanAdjust | PM_ACCMaintenancePlanAdjust_THEIL_BU1.dll |  | 0 |
| PM_ACCPLANSET | PM_ACCPLANSET | 保養工單 | frmACCMaintenancePlanSet_THEIL_BU1 | PM_ACCMaintenancePlanSet_THEIL_BU1.dll |  | 0 |
| PM_ACCSOP | PM_ACCSOP | 保養工單 | frmEQPTypeSOPBasis | PM_ACCTypeSOPBasis_THEIL_BU1.dll |  | 0 |
| PM_ACCSOPBATCH | PM_ACCSOPBATCH | 保養工單 | frmACCTypeSOPBatchCopy | PM_ACCTypeSOPBatchCopy_THEIL_BU1.dll |  | 0 |
| XX_PMCARRIERBATCHMAINTENANCE_THEIL | PM_載具批次保養 | 保養工單 | frmCarrierBatchMaintenance | PM_CarrierBatchMaintenance_THEIL_BU1.dll |  | 0 |
| XX_PMACCBATCHMAINTENANCE_THEIL | PM_ACC批次保養 | 保養工單 | frmACCBatchMaintenance | PM_ACCBatchMaintenance_THEIL_BU1.dll |  | 0 |
| XX_PMCARRIERRECYCLEGROUP_THEIL | PM_載具保養回收功能 | 保養工單 | frmCarrierRecycleGroup | PM_CarrierRecycleGroup_THEIL_BU1.dll |  | 0 |
| XX_PMACCRECYCLEGROUP_THEIL | PM_ACC保養回收功能 | 保養工單 | frmACCRecycleGroup | PM_ACCRecycleGroup_THEIL_BU1.dll |  | 0 |
| PM_JACK | PM_JACK | 保養工單 | frmPMWoFinish | PM_WOFinish_EQM_THEIL.dll |  | 0 |

### RPT — 報表管理（順序 15）

分類：報表設定 [RPT CONFIG]、看板 [BILL BOARD]、現況 [CURRENT STATE]、在製品 [WIP]、生產批 [LOT]、資源 [RESOURCE]、報表引擎 [REPORT ENGINEER]、異動查詢 [ADJUST QUERY]、芯片測試報表 [CP_REPORT]、行業包 [INDUSTRY]、指標樹 [KPI Tree]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_ELECTRICAL TEST DATA QUERY | 電性測試資料查詢 | 行業包 | frmElectricalTestDataQuery | RT_B_ElectricalTestData.dll | RTS01 | 0 |
| B_WAFER EDC NORMALITY CHART | Wafer電性數據常態分析圖 | 行業包 | frmWaferEDCNormalityChartQuery | RT_B_WaferEDCNormalityChart.dll | RTB02 | 0 |
| B_WAFER EDC RUUNING CHART | Wafer電性數據運行圖 | 行業包 | frmWaferEDCRunChartQuery | RT_B_WaferEDCRunningChart.dll | RTB01 | 0 |
| B_WAFER MAP TEST CHART | Wafer Map電性測試圖表 | 行業包 | frmWaferMAPTestChart | RT_B_WaferMAPTestChart.dll | RTB03 | 0 |
| BILLBOARD OPOUTPUT SUMMARY | 作業站時段產出看板 | 看板 | frmBillBoard_OPOutputSummary | RT_BillBoard_OPOutputSummary.dll | RT205 | 0 |
| BILLBOARD OPOUTPUT SUMMARY GAUGE | 作業站時段產出_良率儀表板 | 看板 | frmBillBoard_OPOutputSummary_Gauge | RT_BillBoard_OPOutputSummary_Gauge.dll | RT206 | 0 |
| BILLBOARD OPOUTPUT SUMMARY TRENDCHART | 作業站時段產出_趨勢圖 | 看板 | frmBillBoard_OPOutputSummary_TrendChart | RT_BillBoard_OPOutputSummary_TrendChart.dll | RT207 | 0 |
| BILLBOARD OPSTATE | 作業站現況看板 | 看板 | frmBillBoard_OPState | RT_BillBoard_OPState.dll | RT201 | 0 |
| BILLBOARD OPSTATE_PRDTYPE | 作業站現況看板_產品類別 | 看板 | frmBillBoard_OPState_PRDType | RT_BillBoard_OPState_PRDType.dll | RT202 | 0 |
| BILLBOARD OPSTATE_PRODUCTNO | 作業站現況看板_產品 | 看板 | frmBillBoard_OPState_ProductNo | RT_BillBoard_OPState_ProductNo.dll | RT203 | 0 |
| BILLBOARD TIME_STEP BASIS | 產出看板時段主檔 | 看板 | frmBillBoardTimeStepBasis | RT_BillBoardTimeStepBasis.dll | RT204 | 0 |
| COMP MATERIAL BY COMPONENT | 元件使用物料查詢 | 資源 | frmCont_CompMaterial_byComp | RT_Cont_CompMaterial_byComp.dll | RT603 | 0 |
| COMP MATERIAL BY MATERIAL | 物料用於元件查詢 | 資源 | frmCont_CompMaterial_byMaterial | RT_Cont_CompMaterial_byMaterial.dll | RT604 | 0 |
| CONFIRM LOT | 生產批狀態查詢 | 生產批 | frmLotState | RT_LotState.dll | RT501 | 0 |
| EQP DAILY REPORT | 設備稼動查詢 | 在製品 | frmEQPDailyReport | RT_EQPDailyReport.dll | RT410 | 0 |
| EQP DAILY UTILIZATION CHART | 每日設備稼動長條圖 | 在製品 | frmEQPDailyUtilizationChart | RT_EQPDailyUtilizationChart.dll | RT411 | 0 |
| EQP MTTR SUMMARY | 設備MTTR統計表 | 在製品 | frmEQP_MTTRSummary | RT_EQP_MTTRSummary.dll | RT412 | 0 |
| EQP MTTR SUMMARY BYMONTH | 設備MTTR統計表_以月份匯總 | 在製品 | frmEQP_MTTRSummary_byMonth | RT_EQP_MTTRSummary_byMonth.dll | RT413 | 0 |
| EQP OEE SUMMARY | 整體設備效率(OEE) | 在製品 | frmEQPOEESummary | RT_EQPOEESummary.dll | RT416 | 0 |
| EQP STATE STATISTICS | 設備狀態期間統計 | 在製品 | frmEQPStateStatistics | RT_EQPStateStatistics.dll | RT414 | 0 |
| ERFPARETO | 異常單柏拉圖分析 | 在製品 | frmERFPareto_Query | RT_ERFPareto_Query.dll | RT415 | 0 |
| ERPWORKREPORT CANCEL | 取消報工單_TipTop | 資源 | frmERPWorkReport_Cancel | RT_ERPWorkReport_Cancel.dll | RT609 | 0 |
| ERPWORKREPORT CANCEL_E10ICD | 取消報工單_E10ICD | 資源 | frmERPWorkReport_Cancel_E10ICD | RT_ERPWorkReport_Cancel_E10ICD.dll | RT611 | 0 |
| ERPWORKREPORT CANCEL_T100 | 取消報工單_T100 | 資源 | frmERPWorkReport_Cancel_T100 | RT_ERPWorkReport_Cancel_T100.dll | RT610 | 0 |
| GUM LOG QUERY | 膠類歷程資料查詢 | 資源 | frmGumLogQuery | RT_GumLogQuery.dll | RT612 | 0 |
| HOLD LOT | 異常生產批查詢 | 生產批 | frmLotState_Hold | RT_LotState_Hold.dll | RT502 | 0 |
| KPI Tree Basis | 指標樹基本資料設定 | 指標樹 | frmKPITreeBasis | RT_KPITreeBasis.dll | RT904 | 0 |
| KPI TREE LOG | 指標樹歷史資料查詢 | 指標樹 | frmKPITreeLog | RT_KPITreeLog.dll | RT906 | 0 |
| KPI Tree Query | 指標樹查詢 | 指標樹 | frmKPITreeQuery | RT_KPITreeQuery.dll | RT905 | 0 |
| LOT HISTORY | 生產批歷程及現況查詢 | 在製品 | frmRPTLotHistory | RT_LotHistory.dll | RT405 | 0 |
| LOT HISTORY DETAIL | 生產批歷程明細查詢 | 在製品 | frmLotLog_Report | RT_LotLog_Report.dll | RT404 | 0 |
| LOT HISTORY_FACTORY | 生產批跨廠區歷程明細查詢 | 在製品 | frmLotLog_Report_Factory | RT_LotLog_Report_Factory.dll | RT406 | 0 |
| LOT OP CONT ATTRIB ADJUSTLOG | 生產批作業站製程特性值調整記錄 | 異動查詢 | frmCont_Attrib_AdjustLogQuery | RT_Cont_Attrib_AdjustLogQuery.dll | RT801 | 0 |
| LOT RELEASE LOG | 生產批下線後歷程總表 | 在製品 | frmRPTLotReleaseLog | RT_LotReleaseLog.dll | RT417 | 0 |
| LOT RESOURCE SHARE | 生產批資源檢視(分攤) | 資源 | frmRPTLotResource_Share | RT_LotResource_Share.dll | RT608 | 0 |
| MATERIAL QUERY BY LOT | 生產批使用物料查詢 | 資源 | frmCont_Material_byLot | RT_Cont_Material_byLot.dll | RT601 | 0 |
| MATERIAL QUERY BY MATERIAL | 物料用於生產批查詢 | 資源 | frmCont_Material_byMaterial | RT_Cont_Material_byMaterial.dll | RT602 | 0 |
| MO OPRESOURCE QUERY | 工單作業站資源檢視 | 未掛入 MenuItem | frmRPT_Script | RT_Script.dll | RT902 | 0 |
| MO PACKING COMBINE DISMANTLE REPORT | 包裝拆並箱歷程查詢 | 現況 | frmRPT_MOPackingCombineDismantle | RT_MOPackingCombineDismantle.dll | RT310 | 0 |
| MO PACKING STATUS LOG REPORT | 包裝歷程查詢 | 現況 | frmRPT_MOPackingLog | RT_MOPackingLog.dll | RT309 | 0 |
| MO PACKING STATUS STATE REPORT | 包裝現況查詢 | 現況 | frmRPT_MOPackingState | RT_MOPackingState.dll | RT308 | 0 |
| MO RESOURCE QUERY | 工單耗用資源檢視 | 資源 | frmResourceQuery_byMO | RT_ResourceQuery_byMO.dll | RT605 | 0 |
| MO STATE REPORT | 工單現況資訊查詢 | 現況 | frmMOStateReport | RT_MOStateReport.dll | RT301 | 0 |
| ONLINE CYCLETIME | 產品生產週期查詢 | 報表引擎 | frmRPT_On_CycleTime | RT_On_CycleTime.dll | RT702 | 0 |
| ONLINE LOT STATE | 生產現況查詢 | 現況 | frmRPT_On_LotState | RT_On_LotState.dll | RT302 | 0 |
| ONLINE OP LOTQTY | 作業站生產現況查詢 | 現況 | frmRPT_On_OPLotQty | RT_On_OPLotQty.dll | RT303 | 0 |
| ONLINE PRD OP LOT | 產品作業站總批數查詢 | 報表引擎 | frmRPT_On_PRDOPLot | RT_On_PRDOPLot.dll | RT704 | 0 |
| ONLINE PRODUCTION | 產品產能查詢 | 報表引擎 | frmRPT_On_Production | RT_On_Production.dll | RT701 | 0 |
| ONLINE REASON TOP TEN | 十大不良原因查詢 | 報表引擎 | frmRPT_On_ReasonTopTen | RT_On_ReasonTopTen.dll | RT705 | 0 |
| ONLINE YIELD | 產品良率查詢 | 報表引擎 | frmRPT_On_Yield | RT_On_Yield.dll | RT703 | 0 |
| P_WAFER DEFECT CHART | 晶圓缺陷報表 | 芯片測試報表 | frmRPTWaferDefectChart | RT_P_RPTWaferDefectChart.dll | RTP03 | 0 |
| P_WAFER VISUALIZATION REPORT | 晶圓視覺化報表 | 芯片測試報表 | frmRPTWaferVisualReport | RT_P_RPTWaferMAP_VisualReport.dll | RTP02 | 0 |
| P_WAFERMAP REPORT | WaferMap 報表查詢 | 芯片測試報表 | frmRPTWaferMAPReport | RT_P_RPTWaferMAP_Report.dll | RTP01 | 0 |
| PLAN FINISH TIME | 預計完工查詢 | 資源 | frmPlanFinishTime_byLot | RT_PlanFinishTime_byLot.dll | RT607 | 0 |
| PRD WIP STATE AND YIELD REPORT | 產品作業站現況&良率報表 | 現況 | frmProductYieldAndWIPState | RT_ProductYieldAndWIPState.dll | RT306 | 0 |
| PRODUCT OPRESOURCE QUERY | 產品作業站資源檢視 | 未掛入 MenuItem | frmRPT_Script | RT_Script.dll | RT903 | 0 |
| PRODUCT OUTPUT MONTH | 產品入庫月報明細查詢 | 在製品 | frmProductOutput_Month | RT_ProductOutput_Month.dll | RT403 | 0 |
| PRODUCT YIELD | 產品良率查詢 | 在製品 | frmProductYield | RT_ProductYield.dll | RT402 | 0 |
| QC DAILY OUTPUT | 品管產量日報查詢 | 在製品 | frmQCDailyOutput | RT_QCDailyOutput.dll | RT408 | 0 |
| QC WEEKLY OUTPUT | 品管產量週報查詢 | 在製品 | frmQCWeeklyOutput | RT_QCWeeklyOutput.dll | RT409 | 0 |
| REPORT BASIS | 報表基本資料 | 報表設定 | frmReportBasis | RT_ReportBasis.dll | RT102 | 0 |
| REPORT MAP | 報表索引 | 報表引擎 | frmReportMap | RT_ReportMap.dll | RT708 | 0 |
| REPORT PRINCIPLE | 報表原則設定 | 報表設定 | frmReportPrinciple | RT_ReportPrinciple.dll | RT103 | 0 |
| RESOURCE QUERY | 生產批資源檢視 | 資源 | frmResourceQuery | RT_ResourceQuery.dll | RT606 | 0 |
| RPT REPORT GROUP | 報表群組基本資料 | 報表設定 | frmReportGroupBasis | RT_ReportGroupBasis.dll | RT101 | 0 |
| RULE LOG QUERY | 生產批執行法則查詢 | 生產批 | frmRT_RuleLogQuery | RT_RuleLogQuery.dll | RT504 | 0 |
| SCRIPT REPORT | 描述語言報表查詢 | 報表引擎 | frmRPT_Script | RT_Script.dll | RT706 | 0 |
| SPLIT MERGE DRAW | 分併批歷程圖 | 在製品 | frmSplitMerge_Draw | RT_SplitMerge_Draw.dll | RT407 | 0 |
| SUBSCRIPTION MANAGEMENT | 訂閱管理 | 報表引擎 | frmSubscriptionManagement | RT_SubscriptionManagement.dll | RT707 | 0 |
| USER DEFINED REPORT | 使用者定義報表 | 報表設定 | frmUserDefinedReport | RT_UserDefinedReport.dll | RT104 | 0 |
| WAIT LOT | 等待生產批查詢 | 生產批 | frmLotState_Wait | RT_LotState_Wait.dll | RT503 | 0 |
| WIP STAGE DAILY | 客戶產品作業站現況查詢 | 現況 | frmWIPStageDailyReport | RT_WIPStageDailyReport.dll | RT305 | 0 |
| WIP STATE | 產品作業站現況查詢 | 現況 | frmWIPStateReport | RT_WIPStateReport.dll | RT304 | 0 |
| YIELD REPORT | 產品作業站良率查詢 | 在製品 | frmYieldReport | RT_YieldReport.dll | RT401 | 0 |

### IEW — 預警管理（順序 16）

分類：警示設定 [ALERTCONFIG]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| ALERT NOTICE LOG | 查詢警示通知記錄 | 警示設定 | frmAlertNoticeLog | IW_AlertNoticeLog.dll | IW105 | 0 |
| ALERTBASIS | 警示資料設定 | 警示設定 | frmAlertBasis | IW_AlertBasis.dll | IW102 | 0 |
| ALERTEVENTBASIS | 警示事件設定 | 警示設定 | frmAlertEventBasis | IW_AlertEventBasis.dll | IW101 | 0 |
| ALERTEVENTLOG | 查詢警示觸發記錄 | 警示設定 | frmAlertEventLog | IW_AlertEventLog.dll | IW103 | 0 |
| ALERTEXECUTIONLOG | 查詢警示執行記錄 | 警示設定 | frmAlertExecutionLog | IW_AlertExecutionLog.dll | IW104 | 0 |

### USR — 使用者管理（順序 17）

分類：使用者 [USER CONFIG]、部門 [DEPARTMENT]、代理人 [AGENT]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| AGENT DELETE | 取消使用者代理人 | 代理人 | frmUserAgentDel | UR_UserAgentDel.dll | UR302 | 0 |
| AGENT SET | 使用者代理人設定 | 代理人 | frmUserAgentSet | UR_UserAgentSet.dll | UR301 | 0 |
| CHANGE PASSWORD | 變更密碼 | 使用者 | frmChangePassword | UR_ChangePassword.dll | UR106 | 0 |
| DEPARTMENT BASIS | 部門基本資料 | 部門 | frmDepartmentBasis | UR_DepartmentBasis.dll | UR201 | 0 |
| GROUP BASIS | 群組基本資料 | 使用者 | frmGroupBasis_THEIL_BU1 | UR_GroupBasis_THEIL_BU1.dll | UR102 | 0 |
| GROUP PRIVILEGE | 群組權限設定 | 使用者 | frmGroupPrivilege | UR_GroupPrivilege.dll | UR104 | 0 |
| SHIFT BASIS | 班別基本資料 | 部門 | frmShiftBasis | UR_ShiftBasis.dll | UR202 | 0 |
| SHOW CURRENT AGENT | 代理現況查詢 | 代理人 | frmUserAgentCur | UR_UserAgentCur.dll | UR303 | 0 |
| SHOW GROUP USER | 顯示群組使用者 | 使用者 | frmUserGroupShow | UR_UserGroupShow.dll | UR105 | 0 |
| SHOW USER AGENT | 代理人查詢 | 代理人 | frmUserAgentShow | UR_UserAgentShow.dll | UR304 | 0 |
| SITE WORK TIME | 廠區工作日曆 | 部門 | frmWorkTime | UR_WorkTime.dll | UR204 | 0 |
| USER BASIS | 使用者基本資料 | 使用者 | frmUserSecurity | UR_UserSecurity.dll | UR101 | 0 |
| USER EXCEPTIONITEM | 除外工時項目基本資料 | 使用者 | frmUSRExceptionItem | UR_ExceptionItem.dll | UR103 | 0 |
| WORKTIME TEMPLATE | 工作日曆樣板 | 部門 | frmWorkTimeTemp | UR_WorkTimeTemp.dll | UR203 | 0 |

### SYS — 系統管理（順序 18）

分類：模組 [MODULES]、參數 [PARAMETER]、工程設定 [ENG SETUP]、行業包 [INDUSTRY]、系統紀錄 [SYSTEM LOG]、工具 [TOOLS]、系统监控管理 [MONITOR BASIS]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| TESTDLLWIPLBL0610 | 測試 | 未掛入 MenuItem | frmNewFun | UR_NewFun.dll | SSC01 | 0 |
| MES PARAMETER | MES參數 | 參數 | frmMesParameter | SS_MesParameter.dll | SS202 | 0 |
| MO PROPERTY BASIS | 工單屬性設定 | 工程設定 | frmMOPropertyBasis | SS_MOPropertyBasis.dll | SS304 | 0 |
| MOTYPE BASIS | 工單類別設定 | 工程設定 | frmMOTypeBasis | SS_MOTypeBasis.dll | SS307 | 0 |
| PASSWORD ECODE BASE64 | BASE64 密碼編解譯 | 工具 | frmPasswordEncode_Base64 | SS_PasswordEncode_Base64.dll | SS512 | 0 |
| PHASE BASIS | 階段設定 | 工程設定 | frmPhaseBasis | SS_PhaseBasis.dll | SS301 | 0 |
| RO PROPERTY BASIS | 訂單屬性設定 | 工程設定 | frmROPropertyBasis | SS_ROPropertyBasis.dll | SS303 | 0 |
| SERIAL MAX ADJUST | 序號調整 | 工程設定 | frmSerialMaxAdjust | SS_SerialMaxAdjust.dll | SS311 | 0 |
| SERIAL RULE CONFIG | 序號規則設定 | 工程設定 | frmSerialRuleConfig_THEIL_BU1 | SS_SerialRuleConfig_THEIL_BU1.dll | SS309 | 0 |
| SERIAL SPLITLOT | 分批序號規則 | 工程設定 | frmSerialSplitLot | SS_SerialSplitLot.dll | SS310 | 0 |
| SYS FLOW | 系統建模 | 模組 | frmFlowBasis | SS_FlowBasis.dll | SS104 | 0 |
| SYS FUNCTION | 系統功能 | 模組 | frmSystemFunction | SS_SystemFunction.dll | SS102 | 0 |
| SYS FUNCTION CONTROL | 表單控制項設定 | 工具 | frmSYSFunctionControl | SS_FunctionControl.dll | SS510 | 0 |
| SYS MODULE | 系統模組 | 模組 | frmSYSModuleBasis | SS_ModuleBasis.dll | SS101 | 0 |
| SYS OBJECT FILE FORM | 系統物件對應表 | 模組 | frmObjectFileForm | SS_ObjectFileForm.dll | SS105 | 0 |
| SYS PARAMETER | 系統參數 | 參數 | frmSYSParameterBasis | SS_ParameterBasis.dll | SS201 | 0 |
| SYSTEM TABLE | 系統資料表 | 模組 | frmSystemTable | SS_SystemTable.dll | SS103 | 0 |
| UNIT BASIS | 單位設定 | 參數 | frmUnitBasis | SS_UnitBasis.dll | SS204 | 0 |
| YEAR BASIS | 年度定義 | 參數 | frmYearBasis | SS_YearBasis.dll | SS205 | 0 |
| INTEGRATED TESTING | 外部集成測試 | 工具 | frmIntegratedTesting | SS_IntegratedTesting.dll | SS513 | 0 |
| LOG OVERVIEW | 日誌總覽 | 系統紀錄 | frmSystemLogOverview | SS_SystemLogOverview.dll | SS404 | 0 |
| EAP_CHECKREQUEST | EAP查核規則設定 | 參數 | frmeapcheckrequest | SS_EAPCheckRequest.dll | SS206 | 0 |
| MONITOR NODE BASIS | 監控節點設定 | 系统监控管理 | frmMonitorNodeBasis | SS_MonitorNodeBasis.dll | SS601 | 0 |
| MONITOR Node LOG QUERY | 監控節點日誌查詢 | 系统监控管理 | frmMonitorNodelogQuery | SS_MonitorNodelogQuery.dll | SS604 | 0 |
| MONITOR SERVICE BASIS | 監控程序設定 | 系统监控管理 | frmMonitorServiceBasis | SS_MonitorServiceBasis.dll | SS602 | 0 |
| MONITOR Service LOG QUERY | 監控程序日誌查詢 | 系统监控管理 | frmMonitorServicelogQuery | SS_MonitorServicelogQuery.dll | SS603 | 0 |
| BIN BASIS | Bin基本資料 | 工程設定 | frmBinBasis | SS_BinBasis.dll | SS308 | 0 |
| BULLETIN | 網頁佈告欄 | 工具 | frmBulletin | SS_Bulletin.dll | SS509 | 0 |
| COMPONENT PROPERTY BASIS | 元件屬性設定 | 工程設定 | frmComponentPropertyBasis | SS_ComponentPropertyBasis.dll | SS306 | 0 |
| CONNECTIONMONITOR | 連線狀況 | 工具 | frmConnectionMonitor | SS_ConnectionMonitor.dll | SS508 | 0 |
| DATA ARCHIVE | 數據封存 | 工具 | frmDataArchive | SS_DataArchive.dll | SS511 | 0 |
| ELEMENT BASIS | 特性設定 | 工程設定 | frmElementBasis | SS_ElementBasis.dll | SS302 | 0 |
| ERP SETTING | 外部系統整合設定 | 參數 | frmERPSetting | SS_ERPSetting.dll | SS203 | 0 |
| ERP TRANSACTION LOG | 外部系統整合交易紀錄 | 系統紀錄 | frmERPTransactionLog | SS_ERPTransactionLog.dll | SS403 | 0 |
| ERROR LOG | 錯誤記錄 | 系統紀錄 | frmUserErrorLog | SS_UserErrorLog.dll | SS402 | 0 |
| EVENT LOG | 事件記錄 | 系統紀錄 | frmUserEventLog | SS_UserEventLog.dll | SS401 | 0 |
| ISSUE CHANGE | 更改簽核狀態 | 工具 | frmIssueStateChange | SS_IssueStateChange.dll | SS502 | 0 |
| ISSUE FORM | 簽核控制表單 | 工具 | frmIssueForm | SS_IssueForm.dll | SS503 | 0 |
| P_CP BIN BASIS | 晶圓針測等級基本資料 | 行業包 | frmSYSCPBinBasis | SS_P_CPBINBASIS.DLL | SSP02 | 0 |
| P_WAFER EXPORT CONFIG | 晶圓針測資料匯出設定 | 行業包 | frmWaferExportConfig | SS_P_WAFEREXPORT.DLL | SSP01 | 0 |
| P_WAFERMAP MERGE PARAMETER | 晶圓疊圖參數設定 | 行業包 | frmMergeMAPParameter | SS_P_WaferMAPMergeParameter.dll | SSP03 | 0 |
| B_AL FIELD CONVERT SETUP | Autoloader欄名轉換設定 | 行業包 | frmALFieldConvertBasis | SS_B_FIELDCONVERTBASIS.DLL | SSB02 | 0 |
| B_MO CHOOSE WAFER TABLE | 工單挑片資料來源維護表主檔 | 行業包 | frmMOChooseWaferTableBasis | SS_B_MOCHOOSEWAFERTABLEBASIS.dll | SSB03 | 0 |
| B_TEST RECIPE BASIS | 測試項目主檔 | 行業包 | frmTestRecipeBasis | SS_B_TESTRECIPEBASIS.DLL | SSB01 | 0 |
| B_TEST RECIPE BASIS_RAW DATA | 測試項目主檔_測試原始數據 | 行業包 | frmTestRecipeBasis_RawData | SS_B_TESTRECIPEBASIS_RAWDATA.DLL | SSB04 | 0 |
| LOT PROPERTY BASIS | 生產批屬性設定 | 工程設定 | frmLotPropertyBasis | SS_LotPropertyBasis.dll | SS305 | 0 |
| TESTEXEWIPLBL0610 | 測試 | 未掛入 MenuItem |  | WIPLBL0610.EXE |  | 0 |

### ISU — 簽核管理（順序 19）

分類：簽核設定 [ISSUE CONFIG]、簽核資料查詢 [ISSUE QUERY]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| ISSUE GROUP | 簽核群組資料 | 簽核設定 | frmIssueGroup | IU_IssueGroup.dll | IU101 | 0 |
| ISSUE JOB | 簽核工作定義 | 簽核設定 | frmIssueJob | IU_IssueJob.dll | IU102 | 0 |
| ISSUE JOB MAP | 簽核流程對應 | 簽核設定 | frmIssueJobMap | IU_IssueJobMap.dll | IU103 | 0 |
| ISSUE LIST | 簽核明細列表 | 簽核設定 | frmIssueStateDef | IU_IssueStateDef.dll | IU104 | 0 |
| ISSUE STATE | 簽核狀態查詢 | 簽核資料查詢 | frmIssueState | IU_IssueState.dll | IU201 | 0 |
| SHOW APPLICANT | 送簽者資料查詢 | 簽核資料查詢 | frmCreatorIssue | IU_CreatorIssue.dll | IU203 | 0 |
| SHOW ISSUE LOG | 簽核記錄查詢 | 簽核資料查詢 | frmIssueLog | IU_IssueLog.dll | IU202 | 0 |

### AUT — 自動執行（順序 20）

分類：AUTO RUN設定 [AUTORUN CONFIG]、B2B平台設定 [B2B CONFIG]

| FunctionNo | 功能名稱 | 分類 | FormName | ExecutionFile | FunctionKey | ExecutionMode |
| --- | --- | --- | --- | --- | --- | ---: |
| B_B2B SCHEDULE | B2B排程設定 | B2B平台設定 | frmB2BScheduleSet | AT_B_B2BSCHEDULE.DLL | ATB01 | 0 |
| B_B2B SCHEDULE HISTORY | B2B排程歷史紀錄查詢 | B2B平台設定 | frmB2BScheduleHistory | AT_B_B2BSCHEDULE_HISTORY.DLL | ATB02 | 0 |
| HISTORY | 排程歷史紀錄查詢 | AUTO RUN設定 | frmHistory | AT_History.dll | AT102 | 0 |
| SCHEDULESET | 排程設定 | AUTO RUN設定 | frmScheduleSet | AT_ScheduleSet.dll | AT101 | 0 |

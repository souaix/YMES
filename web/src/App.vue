<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watchEffect } from 'vue'
import MenuTree from './components/MenuTree.vue'
import MenuIcon from './components/MenuIcon.vue'
import EquipmentMaster from './components/EquipmentMaster.vue'
import EquipmentCarrierCategoryMaster from './components/EquipmentCarrierCategoryMaster.vue'
import EquipmentCarrierTypeMaster from './components/EquipmentCarrierTypeMaster.vue'
import EquipmentCarrierBasisMaster from './components/EquipmentCarrierBasisMaster.vue'
import EquipmentRecipeBasisMaster from './components/EquipmentRecipeBasisMaster.vue'
import EquipmentGroupMaster from './components/EquipmentGroupMaster.vue'
import EquipmentStateMaster from './components/EquipmentStateMaster.vue'
import EquipmentQCListMaster from './components/EquipmentQCListMaster.vue'
import EquipmentVendorMaster from './components/EquipmentVendorMaster.vue'
import EquipmentEngineerGroupMaster from './components/EquipmentEngineerGroupMaster.vue'
import EquipmentTypeMaster from './components/EquipmentTypeMaster.vue'
import AccessoryTypeMaster from './components/AccessoryTypeMaster.vue'
import AccessoryBasisMaster from './components/AccessoryBasisMaster.vue'
import AccessoryStateBasisMaster from './components/AccessoryStateBasisMaster.vue'
import AccessoryCategoryMaster from './components/AccessoryCategoryMaster.vue'
import AccessoryCategoryStateMaster from './components/AccessoryCategoryStateMaster.vue'
import CustomerBasisMaster from './components/CustomerBasisMaster.vue'
import CustomerContactorMaster from './components/CustomerContactorMaster.vue'
import CustomerShipAddressMaster from './components/CustomerShipAddressMaster.vue'
import BrokerBasisMaster from './components/BrokerBasisMaster.vue'
import ForwarderBasisMaster from './components/ForwarderBasisMaster.vue'
import TakerBasisMaster from './components/TakerBasisMaster.vue'
import SubcontractorBasisMaster from './components/SubcontractorBasisMaster.vue'
import ENTOSItemMaster from './components/ENTOSItemMaster.vue'
import AreaBasisMaster from './components/AreaBasisMaster.vue'
import AreaInventoryMapMaster from './components/AreaInventoryMapMaster.vue'
import ComputerAreaMaster from './components/ComputerAreaMaster.vue'
import FactoryBasisMaster from './components/FactoryBasisMaster.vue'
import LocatorBasisMaster from './components/LocatorBasisMaster.vue'
import ProductionLineBasisMaster from './components/ProductionLineBasisMaster.vue'
import PositionBasisMaster from './components/PositionBasisMaster.vue'
import AreaShowMaster from './components/AreaShowMaster.vue'
import MasterDataMaster from './components/MasterDataMaster.vue'
import OperationTypeMaster from './components/OperationTypeMaster.vue'
import OperationControlRuleMaster from './components/OperationControlRuleMaster.vue'
import BusinessRuleBasisMaster from './components/BusinessRuleBasisMaster.vue'
import OperationGroupMaster from './components/OperationGroupMaster.vue'
import CustomerOPMappingMaster from './components/CustomerOPMappingMaster.vue'
import EcnOperationGroupMaster from './components/EcnOperationGroupMaster.vue'
import OperationGroupAttribMaster from './components/OperationGroupAttribMaster.vue'
import MaterialVendorMaster from './components/MaterialVendorMaster.vue'
import MaterialTypeMaster from './components/MaterialTypeMaster.vue'
import MaterialTypeQCItemMaster from './components/MaterialTypeQCItemMaster.vue'
import MaterialQCItemMaster from './components/MaterialQCItemMaster.vue'
import MaterialGroupMaster from './components/MaterialGroupMaster.vue'
import MaterialLabelMapMaster from './components/MaterialLabelMapMaster.vue'
import MaterialVendorAutoLoaderSetupMaster from './components/MaterialVendorAutoLoaderSetupMaster.vue'
import EquipmentAutoLoaderSetupMaster from './components/EquipmentAutoLoaderSetupMaster.vue'
import Qc8DReasonBasisMaster from './components/Qc8DReasonBasisMaster.vue'
import QcReasonBasisMaster from './components/QcReasonBasisMaster.vue'
import QcReasonSubTypeMaster from './components/QcReasonSubTypeMaster.vue'
import QcAttributeQcItemMaster from './components/QcAttributeQcItemMaster.vue'
import QcVariableQcItemMaster from './components/QcVariableQcItemMaster.vue'
import QcVariableItemRuleMap from './components/QcVariableItemRuleMap.vue'
import QcSPCAttLimitMaster from './components/QcSPCAttLimitMaster.vue'
import QcAttributeRuleMapMaster from './components/QcAttributeRuleMapMaster.vue'
import QcAttributeMeasurementDelete from './components/QcAttributeMeasurementDelete.vue'
import QcVariableMeasurementDelete from './components/QcVariableMeasurementDelete.vue'
import QcAttributeRuleBasisMaster from './components/QcAttributeRuleBasisMaster.vue'
import QcVariableRuleBasisMaster from './components/QcVariableRuleBasisMaster.vue'
import QcFactorMaintainMaster from './components/QcFactorMaintainMaster.vue'
import QcSpecialBasisMaintainMaster from './components/QcSpecialBasisMaintainMaster.vue'
import QcReasonProductSopFileMaster from './components/QcReasonProductSopFileMaster.vue'
import QcFirstGroupMaster from './components/QcFirstGroupMaster.vue'
import QcFinalGroupMaster from './components/QcFinalGroupMaster.vue'
import QcHoldDescriptionMaster from './components/QcHoldDescriptionMaster.vue'
import QcHoldDispGroupMaster from './components/QcHoldDispGroupMaster.vue'
import QcHoldItemBasisMaster from './components/QcHoldItemBasisMaster.vue'
import QcMClassBasisMaster from './components/QcMClassBasisMaster.vue'
import QcSClassBasisMaster from './components/QcSClassBasisMaster.vue'
import QcSamplingTableMaster from './components/QcSamplingTableMaster.vue'
import QcSampling1916Master from './components/QcSampling1916Master.vue'
import QcSamplingPlanMaster from './components/QcSamplingPlanMaster.vue'
import QcSamplingPlanWaferMaster from './components/QcSamplingPlanWaferMaster.vue'
import QcSecondInspectionSampling from './components/QcSecondInspectionSampling.vue'
import QcVariableItemGroup from './components/QcVariableItemGroup.vue'
import QcAttributeItemGroup from './components/QcAttributeItemGroup.vue'
import QcPmiItemGroup from './components/QcPmiItemGroup.vue'
import QcInspectionMaster from './components/QcInspectionMaster.vue'
import QcItemProductReasonMap from './components/QcItemProductReasonMap.vue'
import QcVariableItemImport from './components/QcVariableItemImport.vue'
import QcAttributeItemImport from './components/QcAttributeItemImport.vue'
import QcChartDataImport from './components/QcChartDataImport.vue'
import QcChartBasisMaster from './components/QcChartBasisMaster.vue'
import QcSpecBasisMaster from './components/QcSpecBasisMaster.vue'
import QcMaterialSpecMaster from './components/QcMaterialSpecMaster.vue'
import QcProductSpecMaster from './components/QcProductSpecMaster.vue'
import QcInspMachineParameterTypeMaster from './components/QcInspMachineParameterTypeMaster.vue'
import QcInspectionMachineMaster from './components/QcInspectionMachineMaster.vue'
import QcInspDriverMaster from './components/QcInspDriverMaster.vue'
import QcOcapBasisMaster from './components/QcOcapBasisMaster.vue'
import QcOcapDispositionAdjust from './components/QcOcapDispositionAdjust.vue'
import FunctionPending from './components/FunctionPending.vue'
const brandLogoUrl = `${import.meta.env.BASE_URL}kd-logo.png`
const appHomeRoute = import.meta.env.BASE_URL
const appBasePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const equipmentTypeRoute = appBasePath + '/Equipment/Types'
const equipmentGroupRoute = appBasePath + '/Equipment/Groups'
const equipmentStateRoute = appBasePath + '/Equipment/States'
const equipmentInspectionRoute = appBasePath + '/Equipment/Inspection'
const equipmentAccessoryTypeRoute = appBasePath + '/Equipment/AccessoryTypes'
const equipmentAccessoryCategoryRoute = appBasePath + '/Equipment/AccessoryCategories'
const equipmentAccessoryCategoryStateRoute = appBasePath + '/Equipment/AccessoryCategoryStates'
const equipmentAccessoryBasisRoute = appBasePath + '/Equipment/AccessoryBasis'
const equipmentAccessoryStateBasisRoute = appBasePath + '/Equipment/AccessoryStates'
const equipmentRoute = `${appBasePath}/Equipment`
const equipmentVendorRoute = `${appBasePath}/Equipment/Vendors`
const equipmentEngineerGroupRoute = `${appBasePath}/Equipment/EngineerGroups`
const equipmentCarrierCategoryRoute = `${appBasePath}/Equipment/CarrierCategories`
const equipmentCarrierTypeRoute = `${appBasePath}/Equipment/CarrierTypes`

const equipmentCarrierBasisRoute = appBasePath + '/Equipment/CarrierBasis'
const equipmentRecipeBasisRoute = appBasePath + '/Equipment/Recipes'
const customerBasisRoute = appBasePath + '/Customer/Basis'
const customerContactorRoute = appBasePath + '/Customer/Contactors'
const customerShipAddressRoute = appBasePath + '/Customer/ShipAddresses'
const brokerBasisRoute = appBasePath + '/Customer/Brokers'
const forwarderBasisRoute = appBasePath + '/Customer/Forwarders'
const takerBasisRoute = appBasePath + '/Customer/Takers'
const subcontractorBasisRoute = appBasePath + '/Customer/Subcontractors'
const osItemRoute = appBasePath + '/Customer/OSItems'
const areaBasisRoute = appBasePath + '/SMD/Areas'
const areaInventoryRoute = appBasePath + '/SMD/AreaInventories'
const computerAreaRoute = appBasePath + '/SMD/ComputerAreas'
const factoryBasisRoute = appBasePath + '/SMD/Factories'
const locatorBasisRoute = appBasePath + '/SMD/Locators'
const pdLineBasisRoute = appBasePath + '/SMD/ProductionLines'
const positionBasisRoute = appBasePath + '/SMD/Positions'
const areaShowRoute = appBasePath + '/SMD/AreaShow'
const materialVendorRoute = appBasePath + '/MTL/MaterialVendors'
const materialTypeRoute = appBasePath + '/MTL/MaterialTypes'
const materialTypeQCItemRoute = appBasePath + '/MTL/MaterialTypeQCItems'
const materialQCItemRoute = appBasePath + '/MTL/MaterialQCItems'
const materialGroupRoute = appBasePath + '/MTL/MaterialGroups'
const materialLabelMapRoute = appBasePath + '/MTL/MaterialLabelMaps'
const materialVendorAutoLoaderRoute = appBasePath + '/MTL/MaterialVendorAutoLoaderSetup'
const equipmentAutoLoaderRoute = appBasePath + '/EQP/EquipmentAutoLoaderSetup'

type EnvironmentKey = 'production' | 'test' | 'development'
type Screen = 'login' | 'change-password' | 'signed-in'
type MasterDataKey = 'operation-master' | 'material-master' | 'process-master' | 'product-master'

interface EnvironmentConfig {
  label: string
  apiBaseUrl: string
}

interface RuntimeConfig {
  production: EnvironmentConfig
  test: EnvironmentConfig
  development: EnvironmentConfig
}

interface AuthResponse {
  userNo: string
  displayName: string
  mustChangePassword: boolean
  message?: string
}

interface MenuNode {
  id: string
  label: string
  shortLabel: string
  moduleNo?: string
  menuNo?: string
  functionNo?: string
  tableName?: string
  children?: MenuNode[]
}

interface MenuTreeApiResponse {
  modules: {
    moduleNo: string
    moduleName: string
    moduleOrder: number
    menuGroups: {
      menuNo: string
      menuName: string
      menuOrder: number
      items: {
        menuNo: string
        menuName: string
        functionNo?: string | null
        functionName?: string | null
        functionOrder: number
      }[]
    }[]
  }[]
}

const environmentKeys: EnvironmentKey[] = ['production', 'test', 'development']
const environmentLabels: Record<EnvironmentKey, string> = {
  production: '正式區',
  test: '測試區',
  development: '開發區',
}

const fallbackMenuTree: MenuNode[] = [
  {
    id: 'customer-data',
    label: '客戶資料',
    shortLabel: '客',
    moduleNo: 'ENT',
    children: [
      { id: 'customer-config', label: '客戶資料設定', shortLabel: '客', children: [
        { id: 'function:ENT:BROKER BASIS', label: '報關行基本資料', shortLabel: '報', functionNo: 'BROKER BASIS', moduleNo: 'ENT' },
        { id: 'function:ENT:CUSTOMER BASIS', label: '客戶基本資料', shortLabel: '客', functionNo: 'CUSTOMER BASIS', moduleNo: 'ENT' },
        { id: 'function:ENT:CUSTOMER CONTACTOR', label: '客戶連絡人資料', shortLabel: '連', functionNo: 'CUSTOMER CONTACTOR', moduleNo: 'ENT' },
        { id: 'function:ENT:CUSTOMER SHIP ADDRESS', label: '客戶出貨地址資料', shortLabel: '址', functionNo: 'CUSTOMER SHIP ADDRESS', moduleNo: 'ENT' },
        { id: 'function:ENT:FORWARDER BASIS', label: '貨運行基本資料', shortLabel: '貨', functionNo: 'FORWARDER BASIS', moduleNo: 'ENT' },
        { id: 'function:ENT:TAKER BASIS', label: '取貨公司基本資料', shortLabel: '取', functionNo: 'TAKER BASIS', moduleNo: 'ENT' },
      ] },
      { id: 'subcontractor-config', label: '外包商資料設定', shortLabel: '外', children: [
        { id: 'function:ENT:SUBCONTRACTOR BASIS', label: '外包商基本資料', shortLabel: '商', functionNo: 'SUBCONTRACTOR BASIS', moduleNo: 'ENT' },
        { id: 'function:ENT:ENTOSITEM', label: '外包項目設定', shortLabel: '項', functionNo: 'ENTOSITEM', moduleNo: 'ENT' },
      ] },
    ],
  },
  {
    id: 'equipment-data',
    label: '設備資料',
    shortLabel: '設',
    moduleNo: 'EQP',
    children: [{ id: 'equipment-settings', label: '設備設定', shortLabel: '設', children: [
      { id: 'equipment-vendor-master', label: '設備供應商基本資料', shortLabel: '供', moduleNo: 'EQP' },
      { id: 'equipment-engineer-group', label: '設備工程群組資料', shortLabel: '群', moduleNo: 'EQP' },
      { id: 'equipment-category', label: '設備類別資料', shortLabel: '類', moduleNo: 'EQP' },
      { id: 'equipment-master', label: '設備基本資料', tableName: 'tbleqpequipmentbasis', shortLabel: '主', functionNo: 'EQP BASIS', moduleNo: 'EQP' },
      { id: 'equipment-group', label: '設備群組資料', shortLabel: '群', moduleNo: 'EQP' },
      { id: 'equipment-state', label: '設備狀態設定', shortLabel: '狀', moduleNo: 'EQP' },
      { id: 'equipment-inspection', label: '設備點檢表設定', shortLabel: '檢', moduleNo: 'EQP' },
    ] }, { id: 'equipment-industry-package', label: '行業包', shortLabel: '行', moduleNo: 'EQP', children: [
      { id: 'function:EQP:B_EQP AUTOLOADER SETUP', label: '設備AutoLoader格式檔設定', shortLabel: '自', functionNo: 'B_EQP AUTOLOADER SETUP', moduleNo: 'EQP', tableName: 'tbleqpalscript' },
    ] }, { id: 'equipment-carrier-menu', label: '載具', shortLabel: '載', moduleNo: 'EQP', children: [{ id: 'equipment-carrier-categories', label: '載具分類資料', shortLabel: '分', functionNo: 'CARRIER CATEGORY', moduleNo: 'EQP' }, { id: 'equipment-carrier-types', label: '載具類別資料', shortLabel: '類', functionNo: 'CARRIER TYPE', moduleNo: 'EQP' }, { id: 'equipment-carrier-basis', label: '載具基本資料', shortLabel: '主', functionNo: 'CARRIER BASIS', moduleNo: 'EQP' }] }, { id: 'equipment-accessory-menu', label: '配件', shortLabel: '配', moduleNo: 'EQP', children: [{ id: 'equipment-accessory-categories', label: '配件分類資料', shortLabel: '分', functionNo: 'ACC CATEGORY', moduleNo: 'EQP' }, { id: 'equipment-accessory-types', label: '配件類別資料', shortLabel: '類', functionNo: 'ACC TYPE', moduleNo: 'EQP' }, { id: 'equipment-accessory-basis', label: '配件基本資料', shortLabel: '主', functionNo: 'ACC BASIS', moduleNo: 'EQP' }, { id: 'equipment-accessory-category-states', label: '配件分類狀態定義設定', shortLabel: '狀', functionNo: 'ACC CATEGORY STATE', moduleNo: 'EQP' }, { id: 'equipment-accessory-state-basis', label: '配件狀態設定', shortLabel: '狀', functionNo: 'EQP ACC STATE', moduleNo: 'EQP' }] },
    { id: 'equipment-recipe-menu', label: '參數', shortLabel: '參', moduleNo: 'EQP', children: [{ id: 'equipment-recipe-basis', label: '設備參數資料', shortLabel: '參', functionNo: 'RECIPE BASIS', moduleNo: 'EQP' }] }],
  },
  {
    id: 'smd-data',
    label: '區域資料',
    shortLabel: '區',
    moduleNo: 'SMD',
    children: [{ id: 'smd-factory-settings', label: '廠區設定', shortLabel: '廠', children: [
      { id: 'function:SMD:AREA BASIS', label: '區域基本資料', shortLabel: '區', functionNo: 'AREA BASIS', moduleNo: 'SMD' },
      { id: 'function:SMD:AREA INV MAP', label: '區域庫房基本設定', shortLabel: '庫', functionNo: 'AREA INV MAP', moduleNo: 'SMD' },
      { id: 'function:SMD:COMPUTER AREA', label: '過帳電腦之區域', shortLabel: '電', functionNo: 'COMPUTER AREA', moduleNo: 'SMD' },
      { id: 'function:SMD:FACTORY BASIS', label: '工廠基本資料', shortLabel: '工', functionNo: 'FACTORY BASIS', moduleNo: 'SMD' },
      { id: 'function:SMD:LOCATOR BASIS', label: '儲位基本資料', shortLabel: '儲', functionNo: 'LOCATOR BASIS', moduleNo: 'SMD' },
      { id: 'function:SMD:POSITION BASIS', label: '工位基本資料', shortLabel: '位', functionNo: 'POSITION BASIS', moduleNo: 'SMD' },
      { id: 'function:SMD:PRODUCTION CONFIG', label: '設備狀態監控_區域', shortLabel: '監', functionNo: 'PRODUCTION CONFIG', moduleNo: 'SMD' },
      { id: 'function:SMD:SMD PDLINE BASIS', label: '生產線別基本資料', shortLabel: '線', functionNo: 'SMD PDLINE BASIS', moduleNo: 'SMD' },
    ] }],
  },
  {
    id: 'operation-data',
    label: '作業站資料',
    shortLabel: '站',
    moduleNo: 'OP',
    children: [{ id: 'operation-settings', label: '作業站設定', shortLabel: '設', children: [{ id: 'operation-master', label: '作業站主檔', tableName: 'tblopbasis', shortLabel: '主', functionNo: 'OP BASIS', moduleNo: 'OP' }, { id: 'function:OP:OP TYPE', label: '作業站類別資料', tableName: 'tbloptype', shortLabel: '類', functionNo: 'OP TYPE', moduleNo: 'OP' }, { id: 'function:OP:OP RULE BASIS', label: '作業站法則設定', tableName: 'tblopcontrolrule', shortLabel: '則', functionNo: 'OP RULE BASIS', moduleNo: 'OP' }, { id: 'function:OP:BR BASIS', label: '企業邏輯設定', tableName: 'tblopbusinessrulebasis', shortLabel: '企', functionNo: 'BR BASIS', moduleNo: 'OP' }, { id: 'function:OP:OP GROUP BASIS', label: '作業站群組資料', tableName: 'tblopgroupbasis', shortLabel: '群', functionNo: 'OP GROUP BASIS', moduleNo: 'OP' }] }, { id: 'operation-industry-package', label: '行業包', shortLabel: '行', children: [{ id: 'function:OP:B_CUSTOMER OP MAPPING', label: '客戶站點對應設定', tableName: 'tbl_bopcustopmapbasis', shortLabel: '客', functionNo: 'B_CUSTOMER OP MAPPING', moduleNo: 'OP' }, { id: 'function:OP:B_ECN OP GROUP BASIS', label: 'ECN 作業站群組設定', tableName: 'tblopecngroupbasis', shortLabel: 'E', functionNo: 'B_ECN OP GROUP BASIS', moduleNo: 'OP' }] }],
  },
  {
    id: 'material-data',
    label: '物料資料',
    shortLabel: '料',
    moduleNo: 'MTL',
    children: [{ id: 'material-settings', label: '物料設定', shortLabel: '設', children: [
      { id: 'material-master', label: '物料基本資料', tableName: 'tblmtlmaterialbasis', shortLabel: '主', functionNo: 'MTL BASIS', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL GROUP', label: '物料群組資料', shortLabel: '群', functionNo: 'MTL GROUP', moduleNo: 'MTL', tableName: 'tblmtlmaterialgroupbasis' },
      { id: 'function:MTL:MTL LABEL MAP BASIS', label: '物料標籤對應設定', shortLabel: '標', functionNo: 'MTL LABEL MAP BASIS', moduleNo: 'MTL', tableName: 'tblmtllabelmap' },
      { id: 'function:MTL:MTL VENDOR', label: '物料供應商基本資料', shortLabel: '供', functionNo: 'MTL VENDOR', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL TYPE', label: '物料類別資料', shortLabel: '類', functionNo: 'MTL TYPE', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL TYPE QCITEM', label: '物料類別品管項目設定', shortLabel: '品', functionNo: 'MTL TYPE QCITEM', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL QCITEM', label: '物料品管項目設定', shortLabel: '品', functionNo: 'MTL QCITEM', moduleNo: 'MTL' },
    ] }, { id: 'material-industry-package', label: '行業包', shortLabel: '行', children: [
      { id: 'function:MTL:B_MTL AUTOLOADER SETUP', label: '來料AutoLoader格式檔設定', shortLabel: '來', functionNo: 'B_MTL AUTOLOADER SETUP', moduleNo: 'MTL', tableName: 'tblmtlvendoralscript' },
    ] }],
  },
  {
    id: 'quality-data',
    label: '品質資料',
    shortLabel: '品',
    moduleNo: 'QC',
    children: [{ id: 'quality-inspection-machine', label: '檢測儀器', shortLabel: '儀', children: [
      { id: 'function:QC:INSPMACHINE PARAMETER TYPE', label: '檢測儀器類別', shortLabel: '類', functionNo: 'INSPMACHINE PARAMETER TYPE', moduleNo: 'QC', tableName: 'tblqcinspmachineparametertype' },
      { id: 'function:QC:INSPMACHINE PARAMETER', label: '檢測儀器基本資料', shortLabel: '機', functionNo: 'INSPMACHINE PARAMETER', moduleNo: 'QC', tableName: 'tblqcinspmachineparameter' },
      { id: 'function:QC:INSPMACHINE DRIVER', label: '檢測設備連線程式', shortLabel: '連', functionNo: 'INSPMACHINE DRIVER', moduleNo: 'QC', tableName: 'tblqcinspdriver' },
    ] }, { id: 'quality-qc-settings', label: '品管設定', shortLabel: '管', children: [
      { id: 'function:QC:QCITEM_A', label: '計數品管項目', shortLabel: '數', functionNo: 'QCITEM_A', moduleNo: 'QC', tableName: 'tblqcitemforattributes' },
      { id: 'function:QC:IMPORT QCITEM ATTRIBUTES', label: '計數品管項目匯入', shortLabel: '匯', functionNo: 'IMPORT QCITEM ATTRIBUTES', moduleNo: 'QC', tableName: 'tblqcitemforattributes' },
      { id: 'function:QC:QCITEM PRD REASON MAP', label: '計數品管項目產品原因對應設定', shortLabel: '對', functionNo: 'QCITEM PRD REASON MAP', moduleNo: 'QC' },
      { id: 'function:QC:SPC ATT LIMIT', label: '計數管制內容', shortLabel: '管', functionNo: 'SPC ATT LIMIT', moduleNo: 'QC', tableName: 'tblspcattlimit' },
      { id: 'function:QC:QCITEM_A RULE MAP', label: '計數品管項目判別法則對應', shortLabel: '對', functionNo: 'QCITEM_A RULE MAP', moduleNo: 'QC', tableName: 'tblspcqcattrulemapbasis' },
      { id: 'function:QC:QCITEM_ADELETE', label: '刪除計數型品管項目量測記錄', shortLabel: '刪', functionNo: 'QCITEM_ADELETE', moduleNo: 'QC', tableName: 'tblspcattbasislog' },
      { id: 'function:QC:QCITEM_V', label: '計量品管項目', shortLabel: '量', functionNo: 'QCITEM_V', moduleNo: 'QC', tableName: 'tblqcitemforvariables' },
      { id: 'function:QC:IMPORT QCITEM VARIABLES', label: '計量品管項目匯入', shortLabel: '匯', functionNo: 'IMPORT QCITEM VARIABLES', moduleNo: 'QC', tableName: 'tblqcitemforvariables' },
      { id: 'function:QC:QCITEM_VDELETE', label: '刪除計量型品管項目量測記錄', shortLabel: '刪', functionNo: 'QCITEM_VDELETE', moduleNo: 'QC', tableName: 'tblspcvarfactorlog' },
      { id: 'function:QC:QCITEM RULE MAP', label: '計量品管項目管制法則對應', shortLabel: '則', functionNo: 'QCITEM RULE MAP', moduleNo: 'QC', tableName: 'tblqcitemrule' },
      { id: 'function:QC:QCITEM_G', label: '計量品管項目_群組', shortLabel: '群', functionNo: 'QCITEM_G', moduleNo: 'QC', tableName: 'tblqcitemgroupbasis' },
      { id: 'function:QC:QCITEM_G_PMI', label: '計量品管項目群組_減項', shortLabel: '減', functionNo: 'QCITEM_G_PMI', moduleNo: 'QC', tableName: 'tblqcitemgroupbasis_pmi' },
      { id: 'function:QC:QCITEM_G_A_ORDER', label: '計數品管項目_群組', shortLabel: '群', functionNo: 'QCITEM_G_A_ORDER', moduleNo: 'QC', tableName: 'tblqcitemattgrouporderbasis' },
      { id: 'function:QC:QC INSPECTION', label: '設備品檢管理設定', shortLabel: '檢', functionNo: 'QC INSPECTION', moduleNo: 'QC', tableName: 'tblqcinspectionbasis' },
      { id: 'function:QC:SAMPLINGFOR2NDINSP', label: '二次檢驗抽樣計畫', shortLabel: '二', functionNo: 'SAMPLINGFOR2NDINSP', moduleNo: 'QC', tableName: 'tblqcsamplingfor2ndinsp' },
    ] }, { id: 'quality-chart-settings', label: '計量規格設定', shortLabel: '量', children: [
      { id: 'function:QC:QC CHART BASIS', label: '管制圖內容', shortLabel: '圖', functionNo: 'QC CHART BASIS', moduleNo: 'QC', tableName: 'tblspcqcchartbasis' },
      { id: 'function:QC:QC SPEC BASIS', label: '品管項目規格', shortLabel: '規', functionNo: 'QC SPEC BASIS', moduleNo: 'QC', tableName: 'tblqcspecbasis' },
      { id: 'function:QC:MTLQCITEM', label: '物料規格設定', shortLabel: '料', functionNo: 'MTLQCITEM', moduleNo: 'QC', tableName: 'tblspcqcchartbasis' },
      { id: 'function:QC:PRDQCITEM', label: '產品規格設定', shortLabel: '產', functionNo: 'PRDQCITEM', moduleNo: 'QC', tableName: 'tblspcqcchartbasis' },
      { id: 'function:QC:IMPORT QC CHART DATA', label: '匯入管制圖內容', shortLabel: '匯', functionNo: 'IMPORT QC CHART DATA', moduleNo: 'QC', tableName: 'tblspcqcchartbasis' },
    ] }, { id: 'quality-basic-settings', label: '基本資料', shortLabel: '基', children: [
      { id: 'function:QC:8D REASON BASIS', label: '8D 原因基本資料', shortLabel: '8', functionNo: '8D REASON BASIS', moduleNo: 'QC', tableName: 'tblqcreasonbasis_8d' },
      { id: 'function:QC:REASON BASIS', label: '品質原因基本資料', shortLabel: '原', functionNo: 'REASON BASIS', moduleNo: 'QC', tableName: 'tblqcreasonbasis' },
      { id: 'function:QC:REASON SUBTYPE BASIS', label: '品質原因子類別資料', shortLabel: '因', functionNo: 'REASON SUBTYPE BASIS', moduleNo: 'QC', tableName: 'tblqcreasonsubtypebasis' },
      { id: 'function:QC:QCITEM_A RULE DATA', label: '計數判別法則基本資料', shortLabel: '則', functionNo: 'QCITEM_A RULE DATA', moduleNo: 'QC', tableName: 'tblqcattrulebasis' },
      { id: 'function:QC:QC RULE BASIS', label: '計量品管法則基本資料', shortLabel: '量', functionNo: 'QC RULE BASIS', moduleNo: 'QC', tableName: 'tblqcrulebasis' },
      { id: 'function:QC:QCFACTORMAINTAIN', label: '要因資料維護', shortLabel: '要', functionNo: 'QCFACTORMAINTAIN', moduleNo: 'QC', tableName: 'tblsysmesparameter' },
      { id: 'function:QC:SPECIALBASISMAINTAIN', label: '特殊主檔維護', shortLabel: '特', functionNo: 'SPECIALBASISMAINTAIN', moduleNo: 'QC', tableName: 'tblsysmesparameter' },
      { id: 'function:QC:QC REASON PRD SOP FILE', label: '品質原因產品指導書', shortLabel: '書', functionNo: 'QC REASON PRD SOP FILE', moduleNo: 'QC', tableName: 'tblqcreasonprdsopfile' },
      { id: 'function:QC:SAMPLING TABLE', label: '抽樣表格', shortLabel: '抽', functionNo: 'SAMPLING TABLE', moduleNo: 'QC', tableName: 'tblqcsamplingtable' },
      { id: 'function:QC:SAMPLING 1916', label: '抽樣計劃設定_1916', shortLabel: '抽', functionNo: 'SAMPLING 1916', moduleNo: 'QC', tableName: 'tblqcsampling1916' },
      { id: 'function:QC:SAMPLING PLAN', label: '抽樣計劃', shortLabel: '計', functionNo: 'SAMPLING PLAN', moduleNo: 'QC', tableName: 'tblqcsamplingplan' },
      { id: 'function:QC:WAFER SAMPLING PLAN', label: '抽樣計劃_元件', shortLabel: '片', functionNo: 'WAFER SAMPLING PLAN', moduleNo: 'QC', tableName: 'tblqcsamplingplan_wafer' },
    ] }, { id: 'quality-exception-settings', label: '異常設定', shortLabel: '異', children: [
      { id: 'function:QC:MCLASS BASIS', label: '主分類基本資料', shortLabel: '主', functionNo: 'MCLASS BASIS', moduleNo: 'QC', tableName: 'tblqcmclassbasis' },
      { id: 'function:QC:SCLASS BASIS', label: '次分類基本資料', shortLabel: '次', functionNo: 'SCLASS BASIS', moduleNo: 'QC', tableName: 'tblqcsclassbasis' },
      { id: 'function:QC:HOLDITEM BASIS', label: '異常原因基本資料', shortLabel: '因', functionNo: 'HOLDITEM BASIS', moduleNo: 'QC', tableName: 'tblqcholditembasis' },
      { id: 'function:QC:HOLD DESCRIPTION', label: '異常說明', shortLabel: '說', functionNo: 'HOLD DESCRIPTION', moduleNo: 'QC', tableName: 'tblqcholddescription' },
      { id: 'function:QC:HOLD DISP GROUP', label: '異常處理群組', shortLabel: '群', functionNo: 'HOLD DISP GROUP', moduleNo: 'QC', tableName: 'tblqcholddispgroupbasis' },
      { id: 'function:QC:OCAP BASIS', label: 'OCAP基本資料', shortLabel: 'O', functionNo: 'OCAP BASIS', moduleNo: 'QC', tableName: 'tblqcocapbasis' },
      { id: 'function:QC:OCAP DISPOSITION ADJUST', label: 'OCAP異常單處置調整', shortLabel: '處', functionNo: 'OCAP DISPOSITION ADJUST', moduleNo: 'QC', tableName: 'tblwiperfocapdisposition' },
      { id: 'function:QC:FIRST GROUP', label: '第一處理群組', shortLabel: '一', functionNo: 'FIRST GROUP', moduleNo: 'QC', tableName: 'tblqcfirstgroup' },
      { id: 'function:QC:FINAL GROUP', label: '最後處理群組', shortLabel: '末', functionNo: 'FINAL GROUP', moduleNo: 'QC', tableName: 'tblqcfinalgroup' },
    ] }],
  },
  {
    id: 'process-data',
    label: '流程資料',
    shortLabel: '流',
    moduleNo: 'PRS',
    children: [{ id: 'process-settings', label: '流程設定', shortLabel: '設', children: [{ id: 'process-master', label: '流程主檔', tableName: 'tblprsprocessbasis', shortLabel: '主', functionNo: 'PRS BASIS', moduleNo: 'PRS' }] }],
  },
  {
    id: 'product-data',
    label: '產品資料',
    shortLabel: '品',
    moduleNo: 'PRD',
    children: [{ id: 'product-settings', label: '產品設定', shortLabel: '設', children: [{ id: 'product-master', label: '產品主檔', tableName: 'tblprdproductbasis', shortLabel: '主', functionNo: 'PRD BASIS', moduleNo: 'PRD' }] }],
  },
]

const menuTree = ref<MenuNode[]>(fallbackMenuTree)
const menuSource = ref<'loading' | 'api' | 'fallback' | 'error'>('fallback')
const menuError = ref('')

const menuRoutes: Record<string, string> = {
  'function:ENT:BROKER BASIS': brokerBasisRoute,
  'function:ENT:FORWARDER BASIS': forwarderBasisRoute,
  'function:ENT:TAKER BASIS': takerBasisRoute,
  'function:ENT:SUBCONTRACTOR BASIS': subcontractorBasisRoute,
  'function:ENT:ENTOSITEM': osItemRoute,
  'function:ENT:CUSTOMER BASIS': customerBasisRoute,
  'function:ENT:CUSTOMER CONTACTOR': customerContactorRoute,
  'function:ENT:CUSTOMER SHIP ADDRESS': customerShipAddressRoute,
  'function:MTL:MTL VENDOR': materialVendorRoute,
  'function:MTL:MTL TYPE': materialTypeRoute,
  'function:MTL:MTL TYPE QCITEM': materialTypeQCItemRoute,
  'function:MTL:MTL QCITEM': materialQCItemRoute,
  'function:MTL:MTL GROUP': materialGroupRoute,
  'function:MTL:MTL LABEL MAP BASIS': materialLabelMapRoute,
  'function:MTL:B_MTL AUTOLOADER SETUP': materialVendorAutoLoaderRoute,
  'function:EQP:B_EQP AUTOLOADER SETUP': equipmentAutoLoaderRoute,
  'function:QC:8D REASON BASIS': appBasePath + '/QC/8DReasonBasis',
  'function:QC:REASON BASIS': appBasePath + '/QC/ReasonBasis',
  'function:QC:REASON SUBTYPE BASIS': appBasePath + '/QC/ReasonSubTypeBasis',
  'function:QC:QCITEM_A': appBasePath + '/QC/AttributeQcItem',
  'function:QC:IMPORT QCITEM ATTRIBUTES': appBasePath + '/QC/AttributeItemImport',
  'function:QC:IMPORT QC CHART DATA': appBasePath + '/QC/ChartDataImport',
  'function:QC:QC CHART BASIS': appBasePath + '/QC/ChartBasis',
  'function:QC:QC SPEC BASIS': appBasePath + '/QC/SpecBasis',
  'function:QC:MTLQCITEM': appBasePath + '/QC/MaterialSpec',
  'function:QC:PRDQCITEM': appBasePath + '/QC/ProductSpec',
  'function:QC:INSPMACHINE PARAMETER TYPE': appBasePath + '/QC/InspectionMachineParameterType',
  'function:QC:INSPMACHINE PARAMETER': appBasePath + '/QC/InspectionMachine',
  'function:QC:INSPMACHINE DRIVER': appBasePath + '/QC/InspectionDrivers',
  'function:QC:OCAP BASIS': appBasePath + '/QC/OCAPBasis',
  'function:QC:OCAP DISPOSITION ADJUST': appBasePath + '/QC/OCAPDispositionAdjust',
  'function:QC:QCITEM_V': appBasePath + '/QC/VariableQcItem',
  'function:QC:IMPORT QCITEM VARIABLES': appBasePath + '/QC/VariableItemImport',
  'function:QC:QCITEM RULE MAP': appBasePath + '/QC/VariableItemRuleMap',
  'function:QC:SPC ATT LIMIT': appBasePath + '/QC/AttributeControlLimit',
  'function:QC:QCITEM_A RULE MAP': appBasePath + '/QC/AttributeRuleMap',
  'function:QC:QCITEM_ADELETE': appBasePath + '/QC/AttributeMeasurementDelete',
  'function:QC:QCITEM_VDELETE': appBasePath + '/QC/VariableMeasurementDelete',
  'function:QC:QCITEM_A RULE DATA': appBasePath + '/QC/AttributeRuleBasis',
  'function:QC:QC RULE BASIS': appBasePath + '/QC/VariableRuleBasis',
  'function:QC:QCFACTORMAINTAIN': appBasePath + '/QC/FactorMaintain',
  'function:QC:SPECIALBASISMAINTAIN': appBasePath + '/QC/SpecialBasisMaintain',
  'function:QC:QC REASON PRD SOP FILE': appBasePath + '/QC/ReasonProductSOPFile',
  'function:QC:SAMPLING TABLE': appBasePath + '/QC/SamplingTable',
  'function:QC:SAMPLING 1916': appBasePath + '/QC/Sampling1916',
  'function:QC:SAMPLING PLAN': appBasePath + '/QC/SamplingPlan',
  'function:QC:WAFER SAMPLING PLAN': appBasePath + '/QC/SamplingPlanWafer',
  'function:QC:SAMPLINGFOR2NDINSP': appBasePath + '/QC/SecondInspectionSampling',
  'function:QC:QCITEM_G': appBasePath + '/QC/VariableItemGroup',
  'function:QC:QCITEM_G_PMI': appBasePath + '/QC/PmiItemGroup',
  'function:QC:QCITEM_G_A_ORDER': appBasePath + '/QC/AttributeItemGroup',
  'function:QC:QC INSPECTION': appBasePath + '/QC/Inspection',
  'function:QC:QCITEM PRD REASON MAP': appBasePath + '/QC/ItemProductReasonMap',
  'function:QC:FIRST GROUP': appBasePath + '/QC/FirstGroup',
  'function:QC:FINAL GROUP': appBasePath + '/QC/FinalGroup',
  'function:QC:HOLD DESCRIPTION': appBasePath + '/QC/HoldDescription',
  'function:QC:HOLD DISP GROUP': appBasePath + '/QC/HoldDispGroup',
  'function:QC:MCLASS BASIS': appBasePath + '/QC/MClassBasis',
  'function:QC:SCLASS BASIS': appBasePath + '/QC/SClassBasis',
  'function:QC:HOLDITEM BASIS': appBasePath + '/QC/HoldItemBasis',
  'equipment-master': equipmentRoute,
  'equipment-vendor-master': equipmentVendorRoute,
  'equipment-engineer-group': equipmentEngineerGroupRoute,
  'equipment-group': equipmentGroupRoute,
  'equipment-state': equipmentStateRoute,
  'equipment-inspection': equipmentInspectionRoute,
  'equipment-category': equipmentTypeRoute,
  'equipment-carrier-categories': equipmentCarrierCategoryRoute,
  'function:EQP:CARRIER CATEGORY': equipmentCarrierCategoryRoute,
  'equipment-carrier-types': equipmentCarrierTypeRoute,
  'function:EQP:CARRIER TYPE': equipmentCarrierTypeRoute,
  'equipment-carrier-basis': equipmentCarrierBasisRoute,
  'function:EQP:CARRIER BASIS': equipmentCarrierBasisRoute,
  'equipment-recipe-basis': equipmentRecipeBasisRoute,
  'function:SMD:AREA BASIS': areaBasisRoute,
  'function:SMD:AREA INV MAP': areaInventoryRoute,
  'function:SMD:COMPUTER AREA': computerAreaRoute,
  'function:SMD:FACTORY BASIS': factoryBasisRoute,
  'function:SMD:LOCATOR BASIS': locatorBasisRoute,
  'function:SMD:SMD PDLINE BASIS': pdLineBasisRoute,
  'function:SMD:POSITION BASIS': positionBasisRoute,
  'function:SMD:PRODUCTION CONFIG': areaShowRoute,
  'function:EQP:RECIPE BASIS': equipmentRecipeBasisRoute,
  'equipment-accessory-categories': equipmentAccessoryCategoryRoute,
  'equipment-accessory-category-states': equipmentAccessoryCategoryStateRoute,
  'equipment-accessory-types': equipmentAccessoryTypeRoute,
  'equipment-accessory-basis': equipmentAccessoryBasisRoute,
  'equipment-accessory-state-basis': equipmentAccessoryStateBasisRoute,
  'function:EQP:EQP ACC STATE': equipmentAccessoryStateBasisRoute,
  'function:EQP:ACC BASIS': equipmentAccessoryBasisRoute,
  'function:EQP:ACC CATEGORY': equipmentAccessoryCategoryRoute,
  'function:EQP:ACC CATEGORY STATE': equipmentAccessoryCategoryStateRoute,
  'function:EQP:ACC TYPE': equipmentAccessoryTypeRoute,
  'operation-master': `${appBasePath}/MasterData/Operations`,
  'function:OP:OP TYPE': appBasePath + '/MasterData/OperationTypes',
  'function:OP:OP RULE BASIS': appBasePath + '/OP/ControlRules',
  'function:OP:BR BASIS': appBasePath + '/OP/BusinessRules',
  'function:OP:OP GROUP BASIS': appBasePath + '/OP/Groups',
  'function:OP:B_CUSTOMER OP MAPPING': appBasePath + '/OP/CustomerOPMapping',
  'function:OP:B_ECN OP GROUP BASIS': appBasePath + '/OP/ECNOperationGroups',
  'function:OP:B_OP GROUP ATTRIB': appBasePath + '/OP/OperationGroupAttributes',
  'material-master': `${appBasePath}/MasterData/Materials`,
  'process-master': `${appBasePath}/MasterData/Processes`,
  'product-master': `${appBasePath}/MasterData/Products`,
}

const functionRoutes: Record<string, string> = {
  'BROKER BASIS': brokerBasisRoute,
  'FORWARDER BASIS': forwarderBasisRoute,
  'TAKER BASIS': takerBasisRoute,
  'SUBCONTRACTOR BASIS': subcontractorBasisRoute,
  'ENTOSITEM': osItemRoute,
  'CUSTOMER BASIS': customerBasisRoute,
  'CUSTOMER CONTACTOR': customerContactorRoute,
  'CUSTOMER SHIP ADDRESS': customerShipAddressRoute,
  'AREA BASIS': areaBasisRoute,
  'AREA INV MAP': areaInventoryRoute,
  'COMPUTER AREA': computerAreaRoute,
  'FACTORY BASIS': factoryBasisRoute,
  'LOCATOR BASIS': locatorBasisRoute,
  'SMD PDLINE BASIS': pdLineBasisRoute,
  '8D REASON BASIS': appBasePath + '/QC/8DReasonBasis',
  'REASON BASIS': appBasePath + '/QC/ReasonBasis',
  'REASON SUBTYPE BASIS': appBasePath + '/QC/ReasonSubTypeBasis',
  QCITEM_A: appBasePath + '/QC/AttributeQcItem',
  QCITEM_V: appBasePath + '/QC/VariableQcItem',
  'QCITEM RULE MAP': appBasePath + '/QC/VariableItemRuleMap',
  'QCITEM_A RULE MAP': appBasePath + '/QC/AttributeRuleMap',
  QCITEM_ADELETE: appBasePath + '/QC/AttributeMeasurementDelete',
  QCITEM_VDELETE: appBasePath + '/QC/VariableMeasurementDelete',
  'QCITEM_A RULE DATA': appBasePath + '/QC/AttributeRuleBasis',
  'QC RULE BASIS': appBasePath + '/QC/VariableRuleBasis',
  QCFACTORMAINTAIN: appBasePath + '/QC/FactorMaintain',
  SPECIALBASISMAINTAIN: appBasePath + '/QC/SpecialBasisMaintain',
  'QC REASON PRD SOP FILE': appBasePath + '/QC/ReasonProductSOPFile',
  'SAMPLING TABLE': appBasePath + '/QC/SamplingTable',
  'SAMPLING 1916': appBasePath + '/QC/Sampling1916',
  'SAMPLING PLAN': appBasePath + '/QC/SamplingPlan',
  'WAFER SAMPLING PLAN': appBasePath + '/QC/SamplingPlanWafer',
  'FIRST GROUP': appBasePath + '/QC/FirstGroup',
  'FINAL GROUP': appBasePath + '/QC/FinalGroup',
  'HOLD DESCRIPTION': appBasePath + '/QC/HoldDescription',
  'HOLD DISP GROUP': appBasePath + '/QC/HoldDispGroup',
  'MCLASS BASIS': appBasePath + '/QC/MClassBasis',
  'HOLDITEM BASIS': appBasePath + '/QC/HoldItemBasis',
  'OCAP DISPOSITION ADJUST': appBasePath + '/QC/OCAPDispositionAdjust',
  'POSITION BASIS': positionBasisRoute,
  'PRODUCTION CONFIG': areaShowRoute,
  'EQP BASIS': equipmentRoute,
  'CARRIER CATEGORY': equipmentCarrierCategoryRoute,
  'CARRIER TYPE': equipmentCarrierTypeRoute,
  'CARRIER BASIS': equipmentCarrierBasisRoute,
  'RECIPE BASIS': equipmentRecipeBasisRoute,
  'EQP STATE': equipmentStateRoute,
  'ACC CATEGORY': equipmentAccessoryCategoryRoute,
  'ACC CATEGORY STATE': equipmentAccessoryCategoryStateRoute,
  'ACC TYPE': equipmentAccessoryTypeRoute,
  'ACC BASIS': equipmentAccessoryBasisRoute,
  'EQP ACC STATE': equipmentAccessoryStateBasisRoute,
  'OP BASIS': `${appBasePath}/MasterData/Operations`,
  'OP TYPE': appBasePath + '/MasterData/OperationTypes',
  'OP RULE BASIS': appBasePath + '/OP/ControlRules',
  'BR BASIS': appBasePath + '/OP/BusinessRules',
  'OP GROUP BASIS': appBasePath + '/OP/Groups',
  'B_CUSTOMER OP MAPPING': appBasePath + '/OP/CustomerOPMapping',
  'B_ECN OP GROUP BASIS': appBasePath + '/OP/ECNOperationGroups',
  'B_OP GROUP ATTRIB': appBasePath + '/OP/OperationGroupAttributes',
  'MTL BASIS': `${appBasePath}/MasterData/Materials`,
  'MTL VENDOR': materialVendorRoute,
  'MTL TYPE': materialTypeRoute,
  'MTL TYPE QCITEM': materialTypeQCItemRoute,
  'MTL QCITEM': materialQCItemRoute,
  'MTL GROUP': materialGroupRoute,
  'MTL LABEL MAP BASIS': materialLabelMapRoute,
  'B_MTL AUTOLOADER SETUP': materialVendorAutoLoaderRoute,
  'B_EQP AUTOLOADER SETUP': equipmentAutoLoaderRoute,
  'PRS BASIS': `${appBasePath}/MasterData/Processes`,
  'PRD BASIS': `${appBasePath}/MasterData/Products`,
  'INSPMACHINE PARAMETER TYPE': appBasePath + '/QC/InspectionMachineParameterType',
  'INSPMACHINE PARAMETER': appBasePath + '/QC/InspectionMachine',
  'INSPMACHINE DRIVER': appBasePath + '/QC/InspectionDrivers',
}

const runtimeConfig = ref<RuntimeConfig>({
  production: { label: '正式區', apiBaseUrl: '/YMES/API' },
  test: { label: '測試區', apiBaseUrl: '' },
  development: { label: '開發區', apiBaseUrl: 'http://localhost:5080' },
})

const selectedEnvironment = ref<EnvironmentKey>('production')
const isDevelopmentPreview = computed(() => import.meta.env.DEV && selectedEnvironment.value === 'development')
const screen = ref<Screen>('login')
const username = ref('')
const password = ref('')
const passwordVisible = ref(false)
const nextPassword = ref('')
const confirmPassword = ref('')
const nextPasswordVisible = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const currentUser = ref<AuthResponse | null>(null)
const configLoaded = ref(false)
const selectedMenuKey = ref<string | null>(null)
const expandedMenuKeys = ref<string[]>([])
const sidebarOpen = ref(true)
const currentPath = ref(window.location.pathname)
const activeMasterKey = computed(() => {
  const path = currentPath.value.replace(/\/+$/, '')
  const match = Object.entries(menuRoutes)
    .filter(([, route]) => {
      const base = route.replace(/\/+$/, '')
      return path === base || path.startsWith(base + '/')
    })
    .sort((a, b) => b[1].length - a[1].length)[0]
  return match?.[0] ?? null
})
const activeMasterDataKey = computed<MasterDataKey | null>(() => {
  const key = activeMasterKey.value
  return key === 'operation-master' || key === 'material-master' || key === 'process-master' || key === 'product-master' ? key : null
})
const isEquipmentPage = computed(() => activeMasterKey.value === 'equipment-master')
const isOperationTypePage = computed(() => activeMasterKey.value === 'function:OP:OP TYPE')
const isOperationControlRulePage = computed(() => activeMasterKey.value === 'function:OP:OP RULE BASIS')
const isBusinessRuleBasisPage = computed(() => activeMasterKey.value === 'function:OP:BR BASIS')
const isOperationGroupPage = computed(() => activeMasterKey.value === 'function:OP:OP GROUP BASIS')
const isCustomerOPMappingPage = computed(() => activeMasterKey.value === 'function:OP:B_CUSTOMER OP MAPPING')
const isEcnOperationGroupPage = computed(() => activeMasterKey.value === 'function:OP:B_ECN OP GROUP BASIS')
const isOperationGroupAttribPage = computed(() => activeMasterKey.value === 'function:OP:B_OP GROUP ATTRIB')
const isMaterialVendorPage = computed(() => activeMasterKey.value === 'function:MTL:MTL VENDOR')
const isMaterialTypePage = computed(() => activeMasterKey.value === 'function:MTL:MTL TYPE')
const isMaterialTypeQCItemPage = computed(() => activeMasterKey.value === 'function:MTL:MTL TYPE QCITEM')
const isMaterialQCItemPage = computed(() => activeMasterKey.value === 'function:MTL:MTL QCITEM')
const isMaterialGroupPage = computed(() => activeMasterKey.value === 'function:MTL:MTL GROUP')
const isMaterialLabelMapPage = computed(() => activeMasterKey.value === 'function:MTL:MTL LABEL MAP BASIS')
const isMaterialVendorAutoLoaderPage = computed(() => activeMasterKey.value === 'function:MTL:B_MTL AUTOLOADER SETUP')
const isEquipmentAutoLoaderPage = computed(() => activeMasterKey.value === 'function:EQP:B_EQP AUTOLOADER SETUP')
const isQc8DReasonBasisPage = computed(() => activeMasterKey.value === 'function:QC:8D REASON BASIS')
const isQcReasonBasisPage = computed(() => activeMasterKey.value === 'function:QC:REASON BASIS')
const isQcReasonSubTypeBasisPage = computed(() => activeMasterKey.value === 'function:QC:REASON SUBTYPE BASIS')
const isQcAttributeQcItemPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_A')
const isQcAttributeItemImportPage = computed(() => activeMasterKey.value === 'function:QC:IMPORT QCITEM ATTRIBUTES')
const isQcChartDataImportPage = computed(() => activeMasterKey.value === 'function:QC:IMPORT QC CHART DATA')
const isQcChartBasisPage = computed(() => activeMasterKey.value === 'function:QC:QC CHART BASIS')
const isQcSpecBasisPage = computed(() => activeMasterKey.value === 'function:QC:QC SPEC BASIS')
const isQcMaterialSpecPage = computed(() => activeMasterKey.value === 'function:QC:MTLQCITEM')
const isQcProductSpecPage = computed(() => activeMasterKey.value === 'function:QC:PRDQCITEM')
const isQcInspMachineParameterTypePage = computed(() => activeMasterKey.value === 'function:QC:INSPMACHINE PARAMETER TYPE')
const isQcInspectionMachinePage = computed(() => activeMasterKey.value === 'function:QC:INSPMACHINE PARAMETER')
const isQcInspDriverPage = computed(() => activeMasterKey.value === 'function:QC:INSPMACHINE DRIVER')
const isQcOcapBasisPage = computed(() => activeMasterKey.value === 'function:QC:OCAP BASIS')
const isQcOcapDispositionAdjustPage = computed(() => activeMasterKey.value === 'function:QC:OCAP DISPOSITION ADJUST')
const isQcVariableQcItemPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_V')
const isQcVariableItemImportPage = computed(() => activeMasterKey.value === 'function:QC:IMPORT QCITEM VARIABLES')
const isQcVariableItemRuleMapPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM RULE MAP')
const isQcSPCAttLimitPage = computed(() => activeMasterKey.value === 'function:QC:SPC ATT LIMIT')
const isQcAttributeRuleMapPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_A RULE MAP')
const isQcAttributeMeasurementDeletePage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_ADELETE')
const isQcVariableMeasurementDeletePage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_VDELETE')
const isQcAttributeRuleBasisPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_A RULE DATA')
const isQcVariableRuleBasisPage = computed(() => activeMasterKey.value === 'function:QC:QC RULE BASIS')
const isQcFactorMaintainPage = computed(() => activeMasterKey.value === 'function:QC:QCFACTORMAINTAIN')
const isQcSpecialBasisMaintainPage = computed(() => activeMasterKey.value === 'function:QC:SPECIALBASISMAINTAIN')
const isQcReasonProductSopFilePage = computed(() => activeMasterKey.value === 'function:QC:QC REASON PRD SOP FILE')
const isQcSamplingTablePage = computed(() => activeMasterKey.value === 'function:QC:SAMPLING TABLE')
const isQcSampling1916Page = computed(() => activeMasterKey.value === 'function:QC:SAMPLING 1916')
const isQcSamplingPlanPage = computed(() => activeMasterKey.value === 'function:QC:SAMPLING PLAN')
const isQcSamplingPlanWaferPage = computed(() => activeMasterKey.value === 'function:QC:WAFER SAMPLING PLAN')
const isQcSecondInspectionSamplingPage = computed(() => activeMasterKey.value === 'function:QC:SAMPLINGFOR2NDINSP')
const isQcVariableItemGroupPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_G')
const isQcPmiItemGroupPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_G_PMI')
const isQcAttributeItemGroupPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM_G_A_ORDER')
const isQcInspectionMasterPage = computed(() => activeMasterKey.value === 'function:QC:QC INSPECTION')
const isQcItemProductReasonMapPage = computed(() => activeMasterKey.value === 'function:QC:QCITEM PRD REASON MAP')
const isQcFirstGroupPage = computed(() => activeMasterKey.value === 'function:QC:FIRST GROUP')
const isQcFinalGroupPage = computed(() => activeMasterKey.value === 'function:QC:FINAL GROUP')
const isQcHoldDescriptionPage = computed(() => activeMasterKey.value === 'function:QC:HOLD DESCRIPTION')
const isQcHoldDispGroupPage = computed(() => activeMasterKey.value === 'function:QC:HOLD DISP GROUP')
const isQcMClassBasisPage = computed(() => activeMasterKey.value === 'function:QC:MCLASS BASIS')
const isQcSClassBasisPage = computed(() => activeMasterKey.value === 'function:QC:SCLASS BASIS')
const isQcHoldItemBasisPage = computed(() => activeMasterKey.value === 'function:QC:HOLDITEM BASIS')
const isCustomerBasisPage = computed(() => activeMasterKey.value === 'function:ENT:CUSTOMER BASIS')
const isCustomerContactorPage = computed(() => activeMasterKey.value === 'function:ENT:CUSTOMER CONTACTOR')
const isCustomerShipAddressPage = computed(() => activeMasterKey.value === 'function:ENT:CUSTOMER SHIP ADDRESS')
const isBrokerBasisPage = computed(() => activeMasterKey.value === 'function:ENT:BROKER BASIS')
const isForwarderBasisPage = computed(() => activeMasterKey.value === 'function:ENT:FORWARDER BASIS')
const isTakerBasisPage = computed(() => activeMasterKey.value === 'function:ENT:TAKER BASIS')
const isSubcontractorBasisPage = computed(() => activeMasterKey.value === 'function:ENT:SUBCONTRACTOR BASIS')
const isOsItemPage = computed(() => activeMasterKey.value === 'function:ENT:ENTOSITEM')
const isAreaBasisPage = computed(() => activeMasterKey.value === 'function:SMD:AREA BASIS')
const isAreaInventoryPage = computed(() => activeMasterKey.value === 'function:SMD:AREA INV MAP')
const isComputerAreaPage = computed(() => activeMasterKey.value === 'function:SMD:COMPUTER AREA')
const isFactoryBasisPage = computed(() => activeMasterKey.value === 'function:SMD:FACTORY BASIS')
const isLocatorBasisPage = computed(() => activeMasterKey.value === 'function:SMD:LOCATOR BASIS')
const isPdLineBasisPage = computed(() => activeMasterKey.value === 'function:SMD:SMD PDLINE BASIS')
const isPositionBasisPage = computed(() => activeMasterKey.value === 'function:SMD:POSITION BASIS')
const isAreaShowPage = computed(() => activeMasterKey.value === 'function:SMD:PRODUCTION CONFIG')
const isEquipmentVendorPage = computed(() => activeMasterKey.value === 'equipment-vendor-master')
const isEquipmentEngineerGroupPage = computed(() => activeMasterKey.value === 'equipment-engineer-group')
const isEquipmentGroupPage = computed(() => activeMasterKey.value === 'equipment-group')
const isEquipmentStatePage = computed(() => activeMasterKey.value === 'equipment-state')
const isEquipmentInspectionPage = computed(() => activeMasterKey.value === 'equipment-inspection')
const isEquipmentTypePage = computed(() => activeMasterKey.value === 'equipment-category')
const isCarrierCategoryPage = computed(() => activeMasterKey.value === 'equipment-carrier-categories' || activeMasterKey.value === 'function:EQP:CARRIER CATEGORY')
const isCarrierTypePage = computed(() => activeMasterKey.value === 'equipment-carrier-types' || activeMasterKey.value === 'function:EQP:CARRIER TYPE')
const isCarrierBasisPage = computed(() => activeMasterKey.value === 'equipment-carrier-basis' || activeMasterKey.value === 'function:EQP:CARRIER BASIS')
const isRecipeBasisPage = computed(() => activeMasterKey.value === 'equipment-recipe-basis' || activeMasterKey.value === 'function:EQP:RECIPE BASIS')
const isAccessoryCategoryPage = computed(() => activeMasterKey.value === 'equipment-accessory-categories' || activeMasterKey.value === 'function:EQP:ACC CATEGORY')
const isAccessoryCategoryStatePage = computed(() => activeMasterKey.value === 'equipment-accessory-category-states' || activeMasterKey.value === 'function:EQP:ACC CATEGORY STATE')
const isAccessoryTypePage = computed(() => activeMasterKey.value === 'equipment-accessory-types' || activeMasterKey.value === 'function:EQP:ACC TYPE')
const isAccessoryBasisPage = computed(() => activeMasterKey.value === 'equipment-accessory-basis' || activeMasterKey.value === 'function:EQP:ACC BASIS')
const isAccessoryStateBasisPage = computed(() => activeMasterKey.value === 'equipment-accessory-state-basis' || activeMasterKey.value === 'function:EQP:EQP ACC STATE')
const currentMenuNode = computed(() => findMenuNodeByRoute(menuTree.value, currentPath.value))
const activeFunctionNode = computed(() => currentMenuNode.value && !activeMasterKey.value ? currentMenuNode.value : null)
const currentMasterTitle = computed(() => currentMenuNode.value?.label ?? (activeMasterKey.value === 'equipment-vendor-master' ? '設備供應商基本資料' : activeMasterKey.value === 'equipment-engineer-group' ? '設備工程群組資料' : activeMasterKey.value === 'equipment-group' ? '設備群組資料' : activeMasterKey.value === 'equipment-state' ? '設備狀態設定' : activeMasterKey.value === 'equipment-inspection' ? '設備點檢表設定' : activeMasterKey.value === 'equipment-accessory-state-basis' || activeMasterKey.value === 'function:EQP:EQP ACC STATE' ? '配件狀態設定' : '主選單'))
const localDemoApi = import.meta.env.DEV && import.meta.env.VITE_LOCAL_DEMO_API === 'true'

watchEffect(() => {
  document.title = screen.value === 'signed-in'
    ? `${currentMasterTitle.value}｜YMES`
    : '登入｜YMES 製造執行系統'
})

function findMenuNode(items: MenuNode[], id: string): MenuNode | null {
  for (const item of items) {
    if (item.id === id) return item
    const child = item.children ? findMenuNode(item.children, id) : null
    if (child) return child
  }
  return null
}

function shortLabel(label: string): string {
  return Array.from(label.trim())[0] || '功'
}

function mapMenuTree(payload: MenuTreeApiResponse): MenuNode[] {
  if (!payload || !Array.isArray(payload.modules)) throw new Error('主選單 API 回應格式不正確。')
  return payload.modules.map((module) => {
    const mapItem = (entry: MenuTreeApiResponse['modules'][number]['menuGroups'][number]['items'][number]): MenuNode => ({
      id: `function:${module.moduleNo}:${entry.functionNo || entry.menuNo}`,
      label: entry.functionName?.trim() || entry.menuName,
      shortLabel: shortLabel(entry.functionName?.trim() || entry.menuName),
      moduleNo: module.moduleNo,
      menuNo: entry.menuNo,
      functionNo: entry.functionNo || undefined,
    })
    const children = (module.menuGroups ?? []).flatMap((group) => {
      const items = (group.items ?? []).map(mapItem)
      if (group.menuName.trim() === module.moduleName.trim()) return items
      return [{
        id: `menu:${module.moduleNo}:${group.menuNo}`,
        label: group.menuName,
        shortLabel: shortLabel(group.menuName),
        moduleNo: module.moduleNo,
        menuNo: group.menuNo,
        children: items,
      }]
    })
    return {
      id: `module:${module.moduleNo}`,
      label: module.moduleName,
      shortLabel: shortLabel(module.moduleName),
      moduleNo: module.moduleNo,
      children,
    }
  })
}

function addDevelopmentEquipmentFunctions(items: MenuNode[]): MenuNode[] {
  const result: MenuNode[] = items.map((item): MenuNode => ({ ...item, children: item.children?.map((child): MenuNode => ({ ...child, children: child.children ? [...child.children] : undefined })) }))
  const fallbackEquipment: MenuNode | undefined = fallbackMenuTree.find((item) => item.moduleNo === 'EQP')
  const fallbackSettings: MenuNode | undefined = fallbackEquipment?.children?.find((item) => item.label === '設備設定')
  if (!fallbackEquipment || !fallbackSettings) return result
  let equipment = result.find((item) => item.moduleNo?.toUpperCase() === 'EQP')
  if (!equipment) {
    result.push(fallbackEquipment)
    return result
  }
  const containsLabel = (nodes: MenuNode[] | undefined, label: string): boolean =>
    Boolean(nodes?.some((node) => node.label.trim() === label || containsLabel(node.children, label)))
  let settings = equipment.children?.find((item) => item.label.trim() === '設備設定')
  if (!settings) {
    settings = { id: 'development:EQP:settings', label: '設備設定', shortLabel: '設', moduleNo: 'EQP', children: [] }
    equipment.children = [...(equipment.children ?? []), settings]
  }
  settings.children = [...(settings.children ?? [])]
  for (const feature of fallbackSettings.children ?? []) {
    if (!containsLabel(equipment.children, feature.label)) settings.children.push({ ...feature })
  }
  const fallbackAccessoryMenu = fallbackEquipment.children?.find((item) => item.label.trim() === '配件')
  if (fallbackAccessoryMenu) {
    const accessoryMenu = equipment.children?.find((item) => item.label.trim() === '配件')
    if (!accessoryMenu) {
      equipment.children = [...(equipment.children ?? []), { ...fallbackAccessoryMenu, children: [...(fallbackAccessoryMenu.children ?? [])] }]
    } else {
      const missing = (fallbackAccessoryMenu.children ?? []).filter((feature) => !containsLabel(accessoryMenu.children, feature.label)); accessoryMenu.children = [...(accessoryMenu.children ?? []), ...missing]
    }
  }
  return result
}

function addDevelopmentOperationIndustryFunctions(items: MenuNode[]): MenuNode[] {
  const cloneTree = (nodes: MenuNode[]): MenuNode[] => nodes.map((item) => ({ ...item, children: item.children ? cloneTree(item.children) : undefined }))
  const result = cloneTree(items)
  const fallbackOperation = fallbackMenuTree.find((item) => item.moduleNo?.toUpperCase() === 'OP')
  const fallbackIndustry = fallbackOperation?.children?.find((item) => item.label.trim() === '行業包')
  if (!fallbackOperation || !fallbackIndustry) return result
  let operation = result.find((item) => item.moduleNo?.toUpperCase() === 'OP')
  if (!operation) { result.push(cloneTree([fallbackOperation])[0]); return result }
  let industry = operation.children?.find((item) => item.label.trim() === '行業包')
  if (!industry) {
    industry = { id: 'development:OP:industry', label: '行業包', shortLabel: '行', moduleNo: 'OP', children: [] }
    operation.children = [...(operation.children ?? []), industry]
  }
  const existing = new Set((industry.children ?? []).map((item) => item.functionNo?.toUpperCase() || item.id))
  const fallbackFeatures = fallbackIndustry.children ?? []
  industry.children = [...(industry.children ?? []), ...fallbackFeatures
    .filter((item) => !existing.has(item.functionNo?.toUpperCase() || item.id))
    .map((item) => cloneTree([item])[0])]
  return result
}
function addDevelopmentInspectionMachineFunctions(items: MenuNode[]): MenuNode[] {
  const cloneTree = (nodes: MenuNode[]): MenuNode[] => nodes.map((item) => ({ ...item, children: item.children ? cloneTree(item.children) : undefined }))
  const result = cloneTree(items)
  const fallbackQuality = fallbackMenuTree.find((item) => item.moduleNo?.toUpperCase() === 'QC')
  const fallbackGroup = fallbackQuality?.children?.find((item) => item.label.trim() === '檢測儀器')
  if (!fallbackQuality || !fallbackGroup) return result
  const quality = result.find((item) => item.moduleNo?.toUpperCase() === 'QC')
  if (!quality) { result.push({ ...fallbackQuality, children: cloneTree(fallbackQuality.children ?? []) }); return result }
  let group = quality.children?.find((item) => item.label.trim() === '檢測儀器')
  if (!group) {
    group = { id: 'development:QC:inspection-machine', label: '檢測儀器', shortLabel: '儀', moduleNo: 'QC', children: [] }
    quality.children = [...(quality.children ?? []), group]
  }
  const hasFunction = (nodes: MenuNode[] | undefined, functionNo: string): boolean => Boolean(nodes?.some((node) =>
    node.functionNo?.trim().toUpperCase() === functionNo || hasFunction(node.children, functionNo)))
  for (const leaf of fallbackGroup.children ?? []) {
    if (leaf.functionNo && !hasFunction(quality.children, leaf.functionNo.trim().toUpperCase())) group.children = [...(group.children ?? []), { ...leaf }]
  }
  return result
}

function routeForMenuNode(item: MenuNode): string {
  const functionNo = item.functionNo?.trim().toUpperCase()
  const functionRouteKey = item.moduleNo && functionNo
    ? `function:${item.moduleNo.trim().toUpperCase()}:${functionNo}`
    : ''
  const knownRoute = item.moduleNo?.toUpperCase() === 'EQP' && item.label.trim() === '設備供應商基本資料'
    ? equipmentVendorRoute
    : item.moduleNo?.toUpperCase() === 'EQP' && item.label.trim() === '設備工程群組資料'
      ? equipmentEngineerGroupRoute
      : undefined
  return menuRoutes[item.id] ?? (functionRouteKey ? menuRoutes[functionRouteKey] : undefined) ?? knownRoute ?? (functionNo ? functionRoutes[functionNo] : undefined) ??
    `${appBasePath}/Function/${encodeURIComponent(item.moduleNo || 'module')}/${encodeURIComponent(item.functionNo || item.menuNo || item.id)}`
}

function findMenuNodeByRoute(items: MenuNode[], route: string): MenuNode | null {
  for (const item of items) {
    if (!item.children?.length && routeForMenuNode(item) === route) return item
    const child = item.children ? findMenuNodeByRoute(item.children, route) : null
    if (child) return child
  }
  return null
}

async function loadMenuTree(): Promise<void> {
  menuSource.value = 'loading'
  menuError.value = ''
  let url: string | null
  try {
    url = apiUrl('/api/navigation/menu-tree')
  } catch (error) {
    url = null
    menuError.value = error instanceof Error ? error.message : '主選單環境設定錯誤。'
  }
  if (!url) {
    menuTree.value = import.meta.env.DEV ? fallbackMenuTree : []
    menuSource.value = import.meta.env.DEV ? 'fallback' : 'error'
    if (!menuError.value) menuError.value = '尚未設定主選單 API 位址。'
    syncRouteState()
    return
  }
  try {
    const response = await fetch(url, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error(`主選單 API 回應錯誤 (${response.status})。`)
    const apiMenuTree = mapMenuTree(await response.json() as MenuTreeApiResponse)
    menuTree.value = import.meta.env.DEV ? addDevelopmentOperationIndustryFunctions(addDevelopmentInspectionMachineFunctions(addDevelopmentEquipmentFunctions(apiMenuTree))) : apiMenuTree
    menuSource.value = 'api'
  } catch (error) {
    menuError.value = error instanceof Error ? error.message : '主選單載入失敗。'
    menuTree.value = import.meta.env.DEV ? fallbackMenuTree : []
    menuSource.value = import.meta.env.DEV ? 'fallback' : 'error'
  }
  syncRouteState()
}

function syncRouteState(): void {
  currentPath.value = window.location.pathname
  selectedMenuKey.value = findMenuNodeByRoute(menuTree.value, currentPath.value)?.id ?? null
}

function toggleMenuNode(id: string): void {
  expandedMenuKeys.value = expandedMenuKeys.value.includes(id)
    ? expandedMenuKeys.value.filter((expandedId) => expandedId !== id)
    : [...expandedMenuKeys.value, id]
}

function selectMenuNode(id: string): void {
  selectedMenuKey.value = id
  const item = findMenuNode(menuTree.value, id)
  if (!item || item.children?.length) return
  const route = routeForMenuNode(item)
  window.history.pushState({}, '', route)
  syncRouteState()
}

function navigateHome(): void {
  window.history.pushState({}, '', appHomeRoute)
  syncRouteState()
}

const selectedEnvironmentConfig = computed(
  () => runtimeConfig.value[selectedEnvironment.value],
)

const selectedEnvironmentLabel = computed(
  () => environmentLabels[selectedEnvironment.value],
)

const passwordLength = computed(() => Array.from(nextPassword.value).length)

onMounted(async () => {
  syncRouteState()
  window.addEventListener('popstate', syncRouteState)
  try {
    const response = await fetch(`${import.meta.env.BASE_URL}environments.json`, { cache: 'no-store' })
    if (response.ok) {
      const config = (await response.json()) as Partial<RuntimeConfig>
      for (const key of environmentKeys) {
        const entry = config[key]
        if (entry && typeof entry.apiBaseUrl === 'string') {
          runtimeConfig.value[key] = {
            label: environmentLabels[key],
            apiBaseUrl: entry.apiBaseUrl.trim(),
          }
        }
      }
    }
  } catch {
    // Keep the safe defaults. A missing environment file must never redirect credentials.
  } finally {
    configLoaded.value = true
  }

  if (localDemoApi) {
    runtimeConfig.value.development.apiBaseUrl = '/YMES/API'
  }

  let sessionUrl: string | null
  try {
    sessionUrl = apiUrl('/auth/me')
  } catch {
    return
  }
  if (!sessionUrl) return

  try {
    const response = await fetch(sessionUrl, { credentials: 'include' })
    if (response.ok) {
      const user = (await response.json()) as AuthResponse
      if (!user.mustChangePassword) {
        currentUser.value = user
        screen.value = 'signed-in'
        await loadMenuTree()
      }
    }
  } catch {
    // Stay on the login screen when there is no reachable session.
  }
})

onBeforeUnmount(() => window.removeEventListener('popstate', syncRouteState))

function apiUrl(path: string): string | null {
  const baseUrl = selectedEnvironmentConfig.value.apiBaseUrl.trim()
  if (!baseUrl) {
    return null
  }

  const resolved = new URL(baseUrl, window.location.origin)
  const isLocalDevelopment =
    resolved.hostname === 'localhost' || resolved.hostname === '127.0.0.1'
  if (resolved.protocol !== 'https:' && !isLocalDevelopment) {
    throw new Error('登入服務必須使用 HTTPS。')
  }

  return resolved.toString().replace(/\/+$/, '') + path
}

async function readMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string }
    if (body.message) return body.message
  } catch {
    // The API may return an empty body for successful actions.
  }

  if (response.status === 401) return '帳號或密碼不正確，或帳號目前無法使用。'
  if (response.status === 429) return '登入嘗試過於頻繁，請稍後再試。'
  if (response.status >= 500) return selectedEnvironmentLabel.value + '登入服務目前無法連線。'
  return '目前無法完成操作，請稍後再試。'
}

async function submitLogin(): Promise<void> {
  errorMessage.value = ''
  successMessage.value = ''

  if (!configLoaded.value) {
    errorMessage.value = '環境設定載入中，請稍候。'
    return
  }
  if (!username.value.trim() || !password.value) {
    errorMessage.value = '請輸入帳號和密碼。'
    return
  }

  let url: string | null
  try {
    url = apiUrl('/auth/login')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '登入環境設定錯誤。'
    return
  }

  if (!url) {
    errorMessage.value = selectedEnvironmentLabel.value + '尚未設定登入服務位址；帳號密碼未送出。'
    return
  }

  busy.value = true
  try {
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: username.value.trim(),
        password: password.value,
      }),
    })

    if (!response.ok) {
      errorMessage.value = await readMessage(response)
      return
    }

    const result = (await response.json()) as AuthResponse
    currentUser.value = result
    if (result.mustChangePassword) {
      screen.value = 'change-password'
      successMessage.value = '首次登入請先設定新密碼。'
    } else {
      screen.value = 'signed-in'
      password.value = ''
      void loadMenuTree()
    }
  } catch {
    errorMessage.value = selectedEnvironmentLabel.value + '登入服務目前無法連線。'
  } finally {
    busy.value = false
  }
}

async function submitDemoLogin(): Promise<void> {
  selectedEnvironment.value = localDemoApi ? 'development' : 'production'
  username.value = 'ymes'
  password.value = '1234'
  await submitLogin()
}

async function submitPasswordChange(): Promise<void> {
  errorMessage.value = ''
  successMessage.value = ''

  if (passwordLength.value < 4) {
    errorMessage.value = '新密碼至少需要 4 個字元。'
    return
  }
  if (passwordLength.value > 128) {
    errorMessage.value = '新密碼最多可輸入 128 個字元。'
    return
  }
  if (nextPassword.value !== confirmPassword.value) {
    errorMessage.value = '兩次輸入的新密碼不一致。'
    return
  }

  let url: string | null
  try {
    url = apiUrl('/auth/change-password')
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '登入環境設定錯誤。'
    return
  }
  if (!url) {
    errorMessage.value = selectedEnvironmentLabel.value + '尚未設定登入服務位址。'
    return
  }

  busy.value = true
  try {
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        currentPassword: password.value,
        newPassword: nextPassword.value,
      }),
    })
    if (!response.ok) {
      errorMessage.value = await readMessage(response)
      return
    }

    password.value = ''
    nextPassword.value = ''
    confirmPassword.value = ''
    if (currentUser.value) currentUser.value.mustChangePassword = false
    screen.value = 'signed-in'
    successMessage.value = '密碼已更新。'
    void loadMenuTree()
  } catch {
    errorMessage.value = selectedEnvironmentLabel.value + '登入服務目前無法連線。'
  } finally {
    busy.value = false
  }
}

async function signOut(): Promise<void> {
  let url: string | null
  try {
    url = apiUrl('/auth/logout')
  } catch {
    url = null
  }
  if (url) {
    try {
      await fetch(url, { method: 'POST', credentials: 'include' })
    } catch {
      // Clear the local screen even if the server cannot be reached.
    }
  }
  currentUser.value = null
  username.value = ''
  password.value = ''
  nextPassword.value = ''
  confirmPassword.value = ''
  errorMessage.value = ''
  successMessage.value = ''
  selectedMenuKey.value = null
  menuTree.value = fallbackMenuTree
  menuSource.value = 'fallback'
  menuError.value = ''
  screen.value = 'login'
}
</script>

<template>
  <div class="app-shell">
    <header v-if="screen !== 'signed-in'" class="topbar">
      <a class="brand" href="/YMES/" aria-label="YMES 首頁">
        <img class="brand-logo" :src="brandLogoUrl" alt="KD 科定企業" />
      </a>
    </header>

    <main v-if="screen === 'login' || screen === 'change-password'" class="login-page">
      <section class="auth-panel" aria-labelledby="login-title">
        <div class="auth-card">
          <div class="card-heading">
            <h1 id="login-title">{{ screen === 'change-password' ? '設定新密碼' : '登入 YMES' }}</h1>
          </div>

          <form v-if="screen === 'login'" class="login-form" @submit.prevent="submitLogin">
            <fieldset class="environment-fieldset">
              <legend>作業環境</legend>
              <div class="environment-options">
                <label
                  v-for="key in environmentKeys"
                  :key="key"
                  class="environment-option"
                  :class="{ selected: selectedEnvironment === key }"
                >
                  <input
                    v-model="selectedEnvironment"
                    type="radio"
                    name="environment"
                    :value="key"
                  />
                  <span class="radio-indicator"></span>
                  <span class="environment-text">{{ environmentLabels[key] }}</span>
                </label>
              </div>
            </fieldset>

            <label class="input-label" for="username">帳號</label>
            <div class="input-wrap">
              <input
                id="username"
                v-model="username"
                name="username"
                type="text"
                autocomplete="username"
                maxlength="10"
                placeholder="請輸入帳號"
                required
              />
            </div>

            <div class="password-label-row">
              <label class="input-label" for="password">密碼</label>
            </div>
            <div class="input-wrap">
              <input
                id="password"
                v-model="password"
                name="password"
                :type="passwordVisible ? 'text' : 'password'"
                autocomplete="current-password"
                placeholder="請輸入密碼"
                required
              />
              <button
                class="visibility-button"
                type="button"
                :aria-label="passwordVisible ? '隱藏密碼' : '顯示密碼'"
                @click="passwordVisible = !passwordVisible"
              >
                {{ passwordVisible ? '隱藏' : '顯示' }}
              </button>
            </div>

            <p v-if="errorMessage" class="form-message error-message" role="alert">
              {{ errorMessage }}
            </p>
            <p v-if="successMessage" class="form-message success-message" role="status">
              {{ successMessage }}
            </p>

            <button class="submit-button" type="submit" :disabled="busy || !configLoaded">
              {{ busy ? '正在驗證…' : '登入' }}
            </button>
            <button class="demo-button" type="button" :disabled="busy || !configLoaded" @click="submitDemoLogin">
              Demo 登入
            </button>
          </form>

          <form
            v-else-if="screen === 'change-password'"
            class="login-form"
            @submit.prevent="submitPasswordChange"
          >
            <p class="first-login-note">首次登入請設定新密碼。</p>

            <label class="input-label" for="new-password">新密碼</label>
            <div class="input-wrap">
              <input
                id="new-password"
                v-model="nextPassword"
                :type="nextPasswordVisible ? 'text' : 'password'"
                autocomplete="new-password"
                placeholder="4–128 個字元"
                required
              />
              <button
                class="visibility-button"
                type="button"
                :aria-label="nextPasswordVisible ? '隱藏密碼' : '顯示密碼'"
                @click="nextPasswordVisible = !nextPasswordVisible"
              >
                {{ nextPasswordVisible ? '隱藏' : '顯示' }}
              </button>
            </div>

            <label class="input-label confirm-label" for="confirm-password">再次輸入新密碼</label>
            <div class="input-wrap">
              <input
                id="confirm-password"
                v-model="confirmPassword"
                type="password"
                autocomplete="new-password"
                placeholder="再次輸入新密碼"
                required
              />
            </div>

            <p v-if="errorMessage" class="form-message error-message" role="alert">
              {{ errorMessage }}
            </p>
            <button class="submit-button" type="submit" :disabled="busy">
              {{ busy ? '正在更新…' : '更新密碼並繼續' }}
            </button>
          </form>
        </div>
      </section>
    </main>

    <main v-else-if="screen === 'signed-in'" class="framework-shell" :class="{ 'sidebar-collapsed': !sidebarOpen }">
      <aside class="framework-sidebar" :class="{ collapsed: !sidebarOpen }">
        <div class="framework-brand">
          <img class="brand-logo" :src="brandLogoUrl" alt="KD 科定企業" />
        </div>
        <button class="sidebar-toggle" type="button" :aria-label="sidebarOpen ? '收合選單' : '展開選單'" @click="sidebarOpen = !sidebarOpen">
          {{ sidebarOpen ? '‹' : '›' }}
        </button>
        <nav>
          <button
            type="button"
            class="framework-menu-item"
            :class="{ active: selectedMenuKey === null }"
            @click="navigateHome"
          >
            <MenuIcon kind="home" />
            <span class="menu-full-label">主選單</span>
          </button>
          <MenuTree
            :items="menuTree"
            :expanded-ids="expandedMenuKeys"
            :selected-id="selectedMenuKey"
            @toggle="toggleMenuNode"
            @select="selectMenuNode"
          />
        </nav>
        <div class="framework-sidebar-foot">
          <div class="framework-profile">
            <span class="framework-avatar">{{ (currentUser?.displayName || currentUser?.userNo || 'Y').slice(0, 1) }}</span>
            <span class="framework-user-name">{{ currentUser?.displayName || currentUser?.userNo }}</span>
          </div>
          <button type="button" class="framework-logout" @click="signOut"><MenuIcon kind="logout" /><span class="menu-full-label">登出</span></button>
        </div>
      </aside>

      <section class="framework-main">
        <header class="framework-header">
          <h1>{{ currentMasterTitle }}</h1>
          <div class="framework-header-user">
            <span>{{ currentUser?.displayName || currentUser?.userNo }}</span>
            <button type="button" @click="signOut">登出</button>
          </div>
        </header>
        <div v-if="menuSource === 'fallback'" class="framework-menu-note" role="status">
          目前顯示簡化選單；尚未遷移的功能會標示待處理。
        </div>
        <div v-else-if="menuSource === 'error'" class="framework-menu-note error" role="alert">
          主選單載入失敗：{{ menuError }}
        </div>
        <EquipmentMaster
          v-if="isEquipmentPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <CustomerBasisMaster
          v-else-if="isCustomerBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <CustomerContactorMaster
          v-else-if="isCustomerContactorPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <CustomerShipAddressMaster
          v-else-if="isCustomerShipAddressPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <BrokerBasisMaster
          v-else-if="isBrokerBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <ForwarderBasisMaster
          v-else-if="isForwarderBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <TakerBasisMaster
          v-else-if="isTakerBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <SubcontractorBasisMaster
          v-else-if="isSubcontractorBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <ENTOSItemMaster
          v-else-if="isOsItemPage"
          class="framework-content"
          @close="navigateHome"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <AreaBasisMaster
          v-else-if="isAreaBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <AreaInventoryMapMaster
          v-else-if="isAreaInventoryPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ComputerAreaMaster
          v-else-if="isComputerAreaPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <FactoryBasisMaster
          v-else-if="isFactoryBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <LocatorBasisMaster
          v-else-if="isLocatorBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductionLineBasisMaster
          v-else-if="isPdLineBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <PositionBasisMaster
          v-else-if="isPositionBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <AreaShowMaster
          v-else-if="isAreaShowPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <EquipmentCarrierCategoryMaster
          v-else-if="isCarrierCategoryPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentCarrierTypeMaster
          v-else-if="isCarrierTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentCarrierBasisMaster
          v-else-if="isCarrierBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentRecipeBasisMaster
          v-else-if="isRecipeBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentVendorMaster
          v-else-if="isEquipmentVendorPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentEngineerGroupMaster
          v-else-if="isEquipmentEngineerGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
        />
        <EquipmentGroupMaster
          v-else-if="isEquipmentGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentStateMaster
          v-else-if="isEquipmentStatePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentQCListMaster
          v-else-if="isEquipmentInspectionPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <EquipmentTypeMaster
          v-else-if="isEquipmentTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
        />
        <AccessoryCategoryMaster
          v-else-if="isAccessoryCategoryPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <AccessoryCategoryStateMaster
          v-else-if="isAccessoryCategoryStatePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <AccessoryTypeMaster
          v-else-if="isAccessoryTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <AccessoryBasisMaster
          v-else-if="isAccessoryBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <AccessoryStateBasisMaster
          v-else-if="isAccessoryStateBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
        />
        <MaterialTypeMaster
          v-else-if="isMaterialTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialTypeQCItemMaster
          v-else-if="isMaterialTypeQCItemPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialQCItemMaster
          v-else-if="isMaterialQCItemPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialGroupMaster
          v-else-if="isMaterialGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialLabelMapMaster
          v-else-if="isMaterialLabelMapPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <EquipmentAutoLoaderSetupMaster
          v-else-if="isEquipmentAutoLoaderPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialVendorAutoLoaderSetupMaster
          v-else-if="isMaterialVendorAutoLoaderPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MaterialVendorMaster
          v-else-if="isMaterialVendorPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSamplingTableMaster
          v-else-if="isQcSamplingTablePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSampling1916Master
          v-else-if="isQcSampling1916Page"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSamplingPlanMaster
          v-else-if="isQcSamplingPlanPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSamplingPlanWaferMaster
          v-else-if="isQcSamplingPlanWaferPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcReasonBasisMaster
          v-else-if="isQcReasonBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcReasonSubTypeMaster
          v-else-if="isQcReasonSubTypeBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeQcItemMaster
          v-else-if="isQcAttributeQcItemPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeItemImport
          v-else-if="isQcAttributeItemImportPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcChartDataImport
          v-else-if="isQcChartDataImportPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcChartBasisMaster
          v-else-if="isQcChartBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSpecBasisMaster
          v-else-if="isQcSpecBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcMaterialSpecMaster
          v-else-if="isQcMaterialSpecPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcProductSpecMaster
          v-else-if="isQcProductSpecPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcInspMachineParameterTypeMaster
          v-else-if="isQcInspMachineParameterTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcInspectionMachineMaster
          v-else-if="isQcInspectionMachinePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcInspDriverMaster
          v-else-if="isQcInspDriverPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcOcapBasisMaster
          v-else-if="isQcOcapBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />

        <QcOcapDispositionAdjust
          v-else-if="isQcOcapDispositionAdjustPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableQcItemMaster
          v-else-if="isQcVariableQcItemPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableItemRuleMap
          v-else-if="isQcVariableItemRuleMapPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSPCAttLimitMaster
          v-else-if="isQcSPCAttLimitPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeRuleMapMaster
          v-else-if="isQcAttributeRuleMapPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeMeasurementDelete
          v-else-if="isQcAttributeMeasurementDeletePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableMeasurementDelete
          v-else-if="isQcVariableMeasurementDeletePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSecondInspectionSampling
          v-else-if="isQcSecondInspectionSamplingPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableItemGroup
          v-else-if="isQcVariableItemGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcPmiItemGroup
          v-else-if="isQcPmiItemGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeItemGroup
          v-else-if="isQcAttributeItemGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcInspectionMaster
          v-else-if="isQcInspectionMasterPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcItemProductReasonMap
          v-else-if="isQcItemProductReasonMapPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableItemImport
          v-else-if="isQcVariableItemImportPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcAttributeRuleBasisMaster
          v-else-if="isQcAttributeRuleBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcVariableRuleBasisMaster
          v-else-if="isQcVariableRuleBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcFactorMaintainMaster
          v-else-if="isQcFactorMaintainPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSpecialBasisMaintainMaster
          v-else-if="isQcSpecialBasisMaintainPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcReasonProductSopFileMaster
          v-else-if="isQcReasonProductSopFilePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <Qc8DReasonBasisMaster
          v-else-if="isQc8DReasonBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcFirstGroupMaster
          v-else-if="isQcFirstGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcFinalGroupMaster
          v-else-if="isQcFinalGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcHoldDispGroupMaster
          v-else-if="isQcHoldDispGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcHoldItemBasisMaster
          v-else-if="isQcHoldItemBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcMClassBasisMaster
          v-else-if="isQcMClassBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSClassBasisMaster
          v-else-if="isQcSClassBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcHoldDescriptionMaster
          v-else-if="isQcHoldDescriptionPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <OperationTypeMaster
          v-else-if="isOperationTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <OperationControlRuleMaster
          v-else-if="isOperationControlRulePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <BusinessRuleBasisMaster
          v-else-if="isBusinessRuleBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <OperationGroupMaster
          v-else-if="isOperationGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <CustomerOPMappingMaster
          v-else-if="isCustomerOPMappingPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <EcnOperationGroupMaster
          v-else-if="isEcnOperationGroupPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <OperationGroupAttribMaster
          v-else-if="isOperationGroupAttribPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <MasterDataMaster
          v-else-if="activeMasterDataKey"
          class="framework-content"
          :master-key="activeMasterDataKey"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
        />
        <FunctionPending
          v-else-if="activeFunctionNode"
          class="framework-content"
          :module-no="activeFunctionNode.moduleNo || ''"
          :module-name="menuTree.find((item) => item.moduleNo === activeFunctionNode?.moduleNo)?.label || ''"
          :function-no="activeFunctionNode.functionNo || activeFunctionNode.menuNo || ''"
          :function-name="activeFunctionNode.label"
        />
      </section>

    </main>
  </div>
</template>

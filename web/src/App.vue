<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch, watchEffect } from 'vue'
import MenuTree from './components/MenuTree.vue'
import MenuIcon from './components/MenuIcon.vue'
import EquipmentMaster from './components/EquipmentMaster.vue'
import ProcessBasisMaster from './components/ProcessBasisMaster.vue'
import ProcessTypeMaster from './components/ProcessTypeMaster.vue'
import ProductionSegmentMaster from './components/ProductionSegmentMaster.vue'
import StageBasisMaster from './components/StageBasisMaster.vue'
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
import ProductBasisMaster from './components/ProductBasisMaster.vue'
import ProductTypeMaster from './components/ProductTypeMaster.vue'
import ProductModuleMaster from './components/ProductModuleMaster.vue'
import ProductContentsSearch from './components/ProductContentsSearch.vue'
import RecipeGroupMaster from './components/RecipeGroupMaster.vue'
import ProductTimeLimitMaster from './components/ProductTimeLimitMaster.vue'
import LabelDataMaster from './components/LabelDataMaster.vue'
import ProductLabelMapMaster from './components/ProductLabelMapMaster.vue'
import ProductOperationBatch from './components/ProductOperationBatch.vue'
import PackingRuleBasisMaster from './components/PackingRuleBasisMaster.vue'
import PackingRuleMapPage from './components/PackingRuleMapPage.vue'
import UserDefinedRunCardMaster from './components/UserDefinedRunCardMaster.vue'
import ProductRunCardMapMaster from './components/ProductRunCardMapMaster.vue'
import ReceivingOrderMaster from './components/ReceivingOrderMaster.vue'
import ManufacturingOrderMaster from './components/ManufacturingOrderMaster.vue'
import WipLotBasisCreate from './components/WipLotBasisCreate.vue'
import WipLotBasisBatch from './components/WipLotBasisBatch.vue'
import WipLotExecution from './components/WipLotExecution.vue'
import WipLotExecutionByComponent from './components/WipLotExecutionByComponent.vue'
import WipEquipmentLotState from './components/WipEquipmentLotState.vue'
import WipEquipmentMaterialLoading from './components/WipEquipmentMaterialLoading.vue'
import WipCarrierState from './components/WipCarrierState.vue'
import WipCarrierManagement from './components/WipCarrierManagement.vue'
import WipEquipmentConsumeStatus from './components/WipEquipmentConsumeStatus.vue'
import WipEquipmentConsumeStatusLog from './components/WipEquipmentConsumeStatusLog.vue'
import WipEquipmentOnlineQc from './components/WipEquipmentOnlineQc.vue'
import WipEquipmentQcListLog from './components/WipEquipmentQcListLog.vue'
import WipRunCard from './components/WipRunCard.vue'
import WipLotSheetPrint from './components/WipLotSheetPrint.vue'
import WipWorkorderPrint from './components/WipWorkorderPrint.vue'
import WipSkipInspectionSetting from './components/WipSkipInspectionSetting.vue'
import WipBumpMapMerge from './components/WipBumpMapMerge.vue'
import WipBumpMapFileManage from './components/WipBumpMapFileManage.vue'
import WipGoldenMapFileManage from './components/WipGoldenMapFileManage.vue'
import WipWaferMapExport from './components/WipWaferMapExport.vue'
import WipProbingLog from './components/WipProbingLog.vue'
import WipLotComponentGen from './components/WipLotComponentGen.vue'
import WipLotBasisCreateCp from './components/WipLotBasisCreateCp.vue'
import WipEqpSetupBasisCp from './components/WipEqpSetupBasisCp.vue'
import WipFgdStockInE10Icd from './components/WipFgdStockInE10Icd.vue'
import WipScrapStockInE10Icd from './components/WipScrapStockInE10Icd.vue'
import WipFgdStockInE10IcdCancel from './components/WipFgdStockInE10IcdCancel.vue'
import WipFgdStockInT100 from './components/WipFgdStockInT100.vue'
import WipFgdStockInT100Cancel from './components/WipFgdStockInT100Cancel.vue'
import WipFgdStockInTp from './components/WipFgdStockInTp.vue'
import WipFgdStockInTpCancel from './components/WipFgdStockInTpCancel.vue'
import WipFgdStockInWorkFlow from './components/WipFgdStockInWorkFlow.vue'
import WipFgdStockInWorkFlowCancel from './components/WipFgdStockInWorkFlowCancel.vue'
import WipInvCountStateAdjust from './components/WipInvCountStateAdjust.vue'
import WipInvCountStatePrint from './components/WipInvCountStatePrint.vue'
import WipInvCountState from './components/WipInvCountState.vue'
import WipOperatorClockIn from './components/WipOperatorClockIn.vue'
import WipOperatorClockOut from './components/WipOperatorClockOut.vue'
import WipOperatorOnline from './components/WipOperatorOnline.vue'
import WipSwrBasis from './components/WipSwrBasis.vue'
import WipUndoDispatch from './components/WipUndoDispatch.vue'
import WipOutsourceCreate from './components/WipOutsourceCreate.vue'
import WipLotReverse from './components/WipLotReverse.vue'
import WipReleasedLotDelete from './components/WipReleasedLotDelete.vue'
import WipReleasedLotBatchDelete from './components/WipReleasedLotBatchDelete.vue'
import WipLotAreaChange from './components/WipLotAreaChange.vue'
import WipProductVersionChange from './components/WipProductVersionChange.vue'
import WipEcnWizard from './components/WipEcnWizard.vue'
import WipCloseLotForcibly from './components/WipCloseLotForcibly.vue'
import WipChangeLotProduct from './components/WipChangeLotProduct.vue'
import WipLotPropertyAdjust from './components/WipLotPropertyAdjust.vue'
import WipLotProcessAttributeAdjust from './components/WipLotProcessAttributeAdjust.vue'
import WipLotMaterialAdjust from './components/WipLotMaterialAdjust.vue'
import WipTransferFactory from './components/WipTransferFactory.vue'
import WipLabelPrint from './components/WipLabelPrint.vue'
import WipBartenderPrint from './components/WipBartenderPrint.vue'
import WipUnInventory from './components/WipUnInventory.vue'
import WipLotQuantityAdjust from './components/WipLotQuantityAdjust.vue'
import EmsEquipmentStateChange from './components/EmsEquipmentStateChange.vue'
import EmsEquipmentControl from './components/EmsEquipmentControl.vue'
import EmsEquipmentAccStateLogQuery from './components/EmsEquipmentAccStateLogQuery.vue'
import EmsEquipmentAccStateQuery from './components/EmsEquipmentAccStateQuery.vue'
import EmsEquipmentCapabilityBasis from './components/EmsEquipmentCapabilityBasis.vue'
import EmsEquipmentCapabilityMapping from './components/EmsEquipmentCapabilityMapping.vue'
import EmsEquipmentStateByArea from './components/EmsEquipmentStateByArea.vue'
import EmsEquipmentStateByType from './components/EmsEquipmentStateByType.vue'
import EmsAccessoryStateMonitor from './components/EmsAccessoryStateMonitor.vue'
import EmsAccessoryStateMonitorByEquipment from './components/EmsAccessoryStateMonitorByEquipment.vue'
import EmsAccessoryStateControlSetting from './components/EmsAccessoryStateControlSetting.vue'
import EmsAccessoryStateLogQuery from './components/EmsAccessoryStateLogQuery.vue'
import EmsAccessoryStateQuery from './components/EmsAccessoryStateQuery.vue'
import EmsAccessoryStateMonitorBu1 from './components/EmsAccessoryStateMonitorBu1.vue'
import EmsAccessoryStateViolativeSetting from './components/EmsAccessoryStateViolativeSetting.vue'
import EmsAccessoryRepair from './components/EmsAccessoryRepair.vue'
import EmsEquipmentStateViolativeSetting from './components/EmsEquipmentStateViolativeSetting.vue'
import EmsEquipmentStateLog from './components/EmsEquipmentStateLog.vue'
import EmsEquipmentStateLogQuery from './components/EmsEquipmentStateLogQuery.vue'
import EmsEquipmentStateLogByState from './components/EmsEquipmentStateLogByState.vue'
import EmsReturnEquipmentAccessory from './components/EmsReturnEquipmentAccessory.vue'
import EmsApplyEquipmentAccessory from './components/EmsApplyEquipmentAccessory.vue'
import EmsEquipmentStateControlSetting from './components/EmsEquipmentStateControlSetting.vue'
import EmsEquipmentStateStatistics from './components/EmsEquipmentStateStatistics.vue'
import EmsEquipmentReasonBasis from './components/EmsEquipmentReasonBasis.vue'
import WipLotOperatorClockIn from './components/WipLotOperatorClockIn.vue'
import WipLotOperatorClockOut from './components/WipLotOperatorClockOut.vue'
import WipOperatorLogin from './components/WipOperatorLogin.vue'
import WipOperatorLoginLot from './components/WipOperatorLoginLot.vue'
import WipLotChoiceEquipment from './components/WipLotChoiceEquipment.vue'
import WipIpqc from './components/WipIpqc.vue'
import WipLotSplit from './components/WipLotSplit.vue'
import WipLotSplitComponent from './components/WipLotSplitComponent.vue'
import WipLotSplitBin from './components/WipLotSplitBin.vue'
import WipLotSplitCarrier from './components/WipLotSplitCarrier.vue'
import WipLotMerge from './components/WipLotMerge.vue'
import WipLotMergeComponent from './components/WipLotMergeComponent.vue'
import WipLotMergeBin from './components/WipLotMergeBin.vue'
import WipLotMergeCarrier from './components/WipLotMergeCarrier.vue'
import WipComponentSplit from './components/WipComponentSplit.vue'
import WipHotLotPriority from './components/WipHotLotPriority.vue'
import WipImmediateHoldSet from './components/WipImmediateHoldSet.vue'
import WipImmediateWaitSet from './components/WipImmediateWaitSet.vue'
import WipBatchImmediateHold from './components/WipBatchImmediateHold.vue'
import WipFutureHoldSet from './components/WipFutureHoldSet.vue'
import WipFutureWaitSet from './components/WipFutureWaitSet.vue'
import WipFutureHoldRelease from './components/WipFutureHoldRelease.vue'
import WipFutureWaitRelease from './components/WipFutureWaitRelease.vue'
import WipErfBasis from './components/WipErfBasis.vue'
import WipErfUnreleased from './components/WipErfUnreleased.vue'
import WipWaitBasis from './components/WipWaitBasis.vue'
import WipWaitDisposition from './components/WipWaitDisposition.vue'
import Wip8DBasisFactory from './components/Wip8DBasisFactory.vue'
import Wip8DMaintain from './components/Wip8DMaintain.vue'
import Wip8DClose from './components/Wip8DClose.vue'
import Wip8DQuery from './components/Wip8DQuery.vue'
import WipAreaShow from './components/AreaShowMaster.vue'
import WipEquipmentFavorite from './components/WipEquipmentFavorite.vue'
import WipFgdInventory from './components/WipFgdInventory.vue'
import SemiInventory from './components/SemiInventory.vue'
import PackingMOLine from './components/PackingMOLine.vue'
import WipInventory from './components/WipInventory.vue'
import ProductQCSpecImport from './components/ProductQCSpecImport.vue'
import ProductGbomBatchImport from './components/ProductGbomBatchImport.vue'
import FtProgramMaster from './components/FtProgramMaster.vue'
import EqcSampleRuleMaster from './components/EqcSampleRuleMaster.vue'
import ProductQcFrequencyMaster from './components/ProductQcFrequencyMaster.vue'
import ProductWorkTimeMaster from './components/ProductWorkTimeMaster.vue'
import ProductOverstayMaster from './components/ProductOverstayMaster.vue'
import ProductStationRuleMaster from './components/ProductStationRuleMaster.vue'
import CpWaferShipReportMaster from './components/CpWaferShipReportMaster.vue'
import CpWaferRuleMaster from './components/CpWaferRuleMaster.vue'
import CpMergeMapRuleMaster from './components/CpMergeMapRuleMaster.vue'
import ChooseWaferRuleMaster from './components/ChooseWaferRuleMaster.vue'
import UploadFileTypeMaster from './components/UploadFileTypeMaster.vue'
import TestProgramFileMaster from './components/TestProgramFileMaster.vue'
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
import QcSpcOnlineControl from './components/QcSpcOnlineControl.vue'
import QcSpcOnlineControlContinue from './components/QcSpcOnlineControlContinue.vue'
import QcIncomingInspectionForm from './components/QcIncomingInspectionForm.vue'
import QcFinalInspectionForm from './components/QcFinalInspectionForm.vue'
import QcInProcessInspectionForm from './components/QcInProcessInspectionForm.vue'
import QcOutgoingInspectionForm from './components/QcOutgoingInspectionForm.vue'
import QcIncomingComponentInspectionForm from './components/QcIncomingComponentInspectionForm.vue'
import QcSpcDataMaintenance from './components/QcSpcDataMaintenance.vue'
import SpcLotDataByLotNo from './components/SpcLotDataByLotNo.vue'
import SpcDataEditLog from './components/SpcDataEditLog.vue'
import SpcChartLineEditLog from './components/SpcChartLineEditLog.vue'
import SpcQcItemBindingHisLog from './components/SpcQcItemBindingHisLog.vue'
import SpcChartAllCapability from './components/SpcChartAllCapability.vue'
import SpcChartAllCapabilityByFactor from './components/SpcChartAllCapabilityByFactor.vue'
import SpcMonthRPTCodeBasis from './components/SpcMonthRPTCodeBasis.vue'
import SpcMonthChartReport from './components/SpcMonthChartReport.vue'
import SpcMonthChartReportByFactor from './components/SpcMonthChartReportByFactor.vue'
import SpcXlsSourceFileControl from './components/SpcXlsSourceFileControl.vue'
import SpcChartSignboard from './components/SpcChartSignboard.vue'
import SpcControlChartAnalysis from './components/SpcControlChartAnalysis.vue'
import SpcControlChartByFactor from './components/SpcControlChartByFactor.vue'
import SpcLineChartAnalysis from './components/SpcLineChartAnalysis.vue'
import SpcScatterChartAnalysis from './components/SpcScatterChartAnalysis.vue'
import SpcPChartSignboard from './components/SpcPChartSignboard.vue'
import SpcParetoAnalysis from './components/SpcParetoAnalysis.vue'
import SpcLrrPpmChart from './components/SpcLrrPpmChart.vue'
import SpcAttributeControlChart from './components/SpcAttributeControlChart.vue'
import SpcCoaReport from './components/SpcCoaReport.vue'
import SpcVariableMeasurementImport from './components/SpcVariableMeasurementImport.vue'
import SpcInspMachineParameterSetup from './components/SpcInspMachineParameterSetup.vue'
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
  enterpriseNo?: string
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
    ] }, { id: 'spc-collection', label: 'SPC 管制', shortLabel: 'SPC', children: [
      { id: 'function:SPC:SPC ONLIE CONTROL', label: 'SPC 量測數據收集', shortLabel: '收集', functionNo: 'SPC ONLIE CONTROL', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC ONLIE CONTROL CONTINUE', label: 'SPC 量測數據收集_計量連續', shortLabel: '連續', functionNo: 'SPC ONLIE CONTROL CONTINUE', moduleNo: 'SPC' },
      { id: 'function:SPC:INCOMING QC FORM', label: '進料檢驗單', shortLabel: 'IQC', functionNo: 'INCOMING QC FORM', moduleNo: 'SPC' },
      { id: 'function:SPC:IQC FORM FOR COMPONENT', label: '進料檢驗_元件', shortLabel: '元件IQC', functionNo: 'IQC FORM FOR COMPONENT', moduleNo: 'SPC' },
      { id: 'function:SPC:FQC FORM', label: '終檢檢驗單', shortLabel: 'FQC', functionNo: 'FQC FORM', moduleNo: 'SPC' },
      { id: 'function:SPC:IPQC FORM', label: 'PQC 檢驗單', shortLabel: 'PQC', functionNo: 'IPQC FORM', moduleNo: 'SPC' },
      { id: 'function:SPC:OQC FORM', label: '出貨檢驗單', shortLabel: 'OQC', functionNo: 'OQC FORM', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC ERF QUERY', label: 'SPC 異常單查詢', tableName: 'tblwiperfbasis', shortLabel: 'SPC查詢', functionNo: 'SPC ERF QUERY', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC ERF UNRELEASED', label: '未解除異常單（SPC）', tableName: 'tblwiperfbasis', shortLabel: 'SPC異常', functionNo: 'SPC ERF UNRELEASED', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC DATA', label: 'SPC 資料維護', shortLabel: 'SPC資料', functionNo: 'SPC DATA', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC DATA BY LOTNO', label: 'Lot 檢測資料', shortLabel: 'Lot檢測', functionNo: 'SPC DATA BY LOTNO', moduleNo: 'SPC' },
      { id: 'function:SPC:DATA EDIT LOG', label: '資料修改記錄', shortLabel: '修改記錄', functionNo: 'DATA EDIT LOG', moduleNo: 'SPC' },
      { id: 'function:SPC:XLS SOURCE FILE CONTROL', label: 'Excel 匯出樣板', shortLabel: 'Excel樣板', functionNo: 'XLS SOURCE FILE CONTROL', moduleNo: 'SPC' },
      { id: 'function:SPC:SPC COA', label: '出貨報告', shortLabel: 'COA', functionNo: 'SPC COA', moduleNo: 'SPC' },
      { id: 'function:SPC:CHART SIGNBOARD', label: '管制圖看板', shortLabel: '管制圖看板', functionNo: 'CHART SIGNBOARD', moduleNo: 'SPC' },
      { id: 'function:SPC:CONTROL CHART ANALYSIS', label: '管制圖分析', shortLabel: '管制圖分析', functionNo: 'CONTROL CHART ANALYSIS', moduleNo: 'SPC' },
      { id: 'function:SPC:CONTROL CHART ANALYSIS BY FACTOR', label: '管制圖分析_以要因查詢', shortLabel: '要因管制圖', functionNo: 'CONTROL CHART ANALYSIS BY FACTOR', moduleNo: 'SPC' },
      { id: 'function:SPC:LINE CHART ANALYSIS', label: '彙整分析圖', shortLabel: '彙整分析圖', functionNo: 'LINE CHART ANALYSIS', moduleNo: 'SPC' },
      { id: 'function:SPC:SCATTER DIAGRAM', label: '散佈圖分析', shortLabel: '散佈圖分析', functionNo: 'SCATTER DIAGRAM', moduleNo: 'SPC' },
      { id: 'function:SPC:PCHART SIGNBOARD', label: '不良率管制圖看板', shortLabel: '不良率看板', functionNo: 'PCHART SIGNBOARD', moduleNo: 'SPC' },
      { id: 'function:SPC:PARETO DIAGRAM', label: '柏拉圖分析', shortLabel: '柏拉圖', functionNo: 'PARETO DIAGRAM', moduleNo: 'SPC' },
      { id: 'function:SPC:IMPORT VAR MEASURE', label: '匯入計量檢測數據', shortLabel: '匯入', functionNo: 'IMPORT VAR MEASURE', moduleNo: 'SPC' },
    ] }, { id: 'spc-inspection-machine', label: 'SPC 檢測設備', shortLabel: '設備', children: [
      { id: 'function:SPC:INSP MACHINE PARAMETER SETUP', label: '品管項目檢測儀器對應', shortLabel: '儀器對應', functionNo: 'INSP MACHINE PARAMETER SETUP', moduleNo: 'SPC' },
    ] }],
  },
  {
    id: 'process-data',
    label: '流程資料',
    shortLabel: '流',
    moduleNo: 'PRS',
    children: [{ id: 'process-settings', label: '流程設定', shortLabel: '設', children: [{ id: 'process-master', label: '流程主檔', tableName: 'tblprsprocessbasis', shortLabel: '主', functionNo: 'PRS BASIS', moduleNo: 'PRS' }, { id: 'function:PRS:PRS TYPE', label: '流程類別資料', tableName: 'tblprstype', shortLabel: '類', functionNo: 'PRS TYPE', moduleNo: 'PRS' }, { id: 'function:PRS:PS BASIS', label: '生產區段資料', tableName: 'tblprsproductionsegment', shortLabel: '區', functionNo: 'PS BASIS', moduleNo: 'PRS' }, { id: 'function:PRS:STAGE BASIS', label: '製造層別設定', tableName: 'tbl_fabprsstagebasis', shortLabel: '層', functionNo: 'STAGE BASIS', moduleNo: 'PRS' }] }],
  },
  {
    id: 'product-data',
    label: '產品資料',
    shortLabel: '品',
    moduleNo: 'PRD',
    children: [{ id: 'product-settings', label: '產品設定', shortLabel: '設', children: [
      { id: 'function:PRD:PRD TYPE', label: '產品類別資料', tableName: 'tblprdtype', shortLabel: '類', functionNo: 'PRD TYPE', moduleNo: 'PRD' },
      { id: 'product-master', label: '產品主檔', tableName: 'tblprdproductbasis', shortLabel: '主', functionNo: 'PRD BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:B_PRD BASIS_IC', label: '產品設定_封測', tableName: 'tblprdproductbasis', shortLabel: '封', functionNo: 'B_PRD BASIS_IC', moduleNo: 'PRD' },
      { id: 'function:PRD:MODULE BASIS', label: '製程模組資料', tableName: 'tblprdmodulebasis', shortLabel: '模', functionNo: 'MODULE BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT SEARCH', label: '產品內容查詢', tableName: 'tblprdproductbasis', shortLabel: '查', functionNo: 'PRODUCT SEARCH', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD RECIPE BASIS', label: '產品參數群組', tableName: 'tblprdrecipebasis', shortLabel: '參', functionNo: 'PRD RECIPE BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:LIMITED TIME CONTROL', label: '生產時間限制(站與站)', tableName: 'tblprdlimitedtimecontrol', shortLabel: '時', functionNo: 'LIMITED TIME CONTROL', moduleNo: 'PRD' },
      { id: 'function:PRD:LABEL DATA', label: '標籤設定', tableName: 'tblprdlabelbasis', shortLabel: '標', functionNo: 'LABEL DATA', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT LABEL MAP BASIS', label: '產品標籤對應設定', tableName: 'tblprdlabelmap', shortLabel: '對', functionNo: 'PRODUCT LABEL MAP BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT OP DEFAULT', label: '產品作業站預設', tableName: 'tblprdop', shortLabel: '預', functionNo: 'PRODUCT OP DEFAULT', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT OP AMEND', label: '產品作業站修正', tableName: 'tblprdop', shortLabel: '修', functionNo: 'PRODUCT OP AMEND', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT OP COPY', label: '產品作業站批次複製', tableName: 'tblprdop', shortLabel: '複', functionNo: 'PRODUCT OP COPY', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD PACKING RULE BASIS', label: '包裝規則設定', tableName: 'tblprdpackingrulebasis', shortLabel: '包', functionNo: 'PRD PACKING RULE BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD PACKINGRULE MAP', label: '產品包裝規則對應設定', tableName: 'tblprdpackingrulemap', shortLabel: '配', functionNo: 'PRD PACKINGRULE MAP', moduleNo: 'PRD' },
      { id: 'function:PRD:USER DEFINED RUNCARD', label: '自定義流程卡', tableName: 'tblprddefineruncardbasis', shortLabel: '卡', functionNo: 'USER DEFINED RUNCARD', moduleNo: 'PRD' },
      { id: 'function:PRD:PRODUCT RUN CARD MAP BASIS', label: '產品流程卡對應設定', tableName: 'tblprdruncardmap', shortLabel: '對', functionNo: 'PRODUCT RUN CARD MAP BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD QCITEM SPEC IMPORT', label: '產品品管項目規格匯入', tableName: 'tblprdqcitem', shortLabel: '檢', functionNo: 'PRD QCITEM SPEC IMPORT', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD GBOM IMPORT', label: 'GBOM批次匯入', tableName: 'tblprdbom', shortLabel: '匯', functionNo: 'PRD GBOM IMPORT', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD WORKTIME', label: '產品作業站標工維護', tableName: 'xx_tblprdruntime_theil_bu1', shortLabel: '工', functionNo: 'PRD WORKTIME', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD OVERSTAY', label: '產品作業站Overstay維護', tableName: 'xx_tbloversetup_theil_bu1', shortLabel: '候', functionNo: 'PRD OVERSTAY', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD OP RULE', label: '產品作業站法則維護', tableName: 'tblprdoprule', shortLabel: '則', functionNo: 'PRD OP RULE', moduleNo: 'PRD' },
    ] }, { id: 'product-bin-settings', label: '等級設定', shortLabel: '級', children: [
      { id: 'function:PRD:FT PROGRAM BASIS', label: 'FT測試程式', tableName: 'tblprdftprogrambasis', shortLabel: '測', functionNo: 'FT PROGRAM BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:EQC SAMPLE RULE', label: '電性(EQC)抽樣規則', tableName: 'tblprdeqcsamplerule', shortLabel: '抽', functionNo: 'EQC SAMPLE RULE', moduleNo: 'PRD' },
      { id: 'function:PRD:PRD QCRULE BASIS', label: '產品抽樣頻率規則', tableName: 'tblprdqcrulebasis', shortLabel: '頻', functionNo: 'PRD QCRULE BASIS', moduleNo: 'PRD' },
    ] }, { id: 'product-cp-settings', label: 'CP行業設定', shortLabel: 'CP', children: [
      { id: 'function:PRD:P_CP WAFER YIELD SHIP REPORT', label: '晶圓良率與出貨報告維護', tableName: 'tbl_cpsyscombomapbasis', shortLabel: '報', functionNo: 'P_CP WAFER YIELD SHIP REPORT', moduleNo: 'PRD' },
      { id: 'function:PRD:P_CP PROGRAM DEFINE', label: '晶圓測試程式定義', tableName: 'tbl_cpprdprogrambasis', shortLabel: '測', functionNo: 'P_CP PROGRAM DEFINE', moduleNo: 'PRD' },
      { id: 'function:PRD:P_WAFER RULE MANAGER', label: '產品晶圓法則管制管理', tableName: 'tbl_pprdwaferruleimportsummary', shortLabel: '管', functionNo: 'P_WAFER RULE MANAGER', moduleNo: 'PRD' },
      { id: 'function:PRD:P_PRD WAFERMAP MERGE RULE', label: '產品晶圓圖疊圖規則設置', tableName: 'tbl_bprdmergemaprule', shortLabel: '疊', functionNo: 'P_PRD WAFERMAP MERGE RULE', moduleNo: 'PRD' },
    ] }, { id: 'product-wafer-settings', label: '挑片規則', shortLabel: '挑', children: [
      { id: 'function:PRD:B_CHOOSE WAFER RULES', label: '挑片規則設定', tableName: 'tblprdchoosewaferrules', shortLabel: '挑', functionNo: 'B_CHOOSE WAFER RULES', moduleNo: 'PRD' },
    ] }, { id: 'product-document-settings', label: '產品文件管理', shortLabel: '檔', children: [
      { id: 'function:PRD:B_UPLOAD FILE BASIS', label: '檔案上傳基本資料', tableName: 'tbl_bprdmesfiletype', shortLabel: '傳', functionNo: 'B_UPLOAD FILE BASIS', moduleNo: 'PRD' },
      { id: 'function:PRD:B_TEST PROGRAM FILE MANAGEMENT', label: '測試程式檔案管理', tableName: 'tbl_bprdmesfilemanagement', shortLabel: '檔', functionNo: 'B_TEST PROGRAM FILE MANAGEMENT', moduleNo: 'PRD' },
    ] }],
  },
  {
    id: 'module:OE', label: '工單管理', shortLabel: '單', moduleNo: 'OE', children: [{
      id: 'menu:OE:RO', label: '訂單', shortLabel: '訂', children: [
        { id: 'function:OE:RO BASIS', label: '訂單基本資料', tableName: 'tbloerobasis', shortLabel: '基', functionNo: 'RO BASIS', moduleNo: 'OE' },
        { id: 'function:OE:RO CLOSE', label: '訂單結案', tableName: 'tbloerobasis', shortLabel: '結', functionNo: 'RO CLOSE', moduleNo: 'OE' },
      ],
    }, {
      id: 'menu:OE:MO', label: '工單', shortLabel: '工', children: [
        { id: 'function:OE:MO BASIS', label: '工單基本資料', tableName: 'tbloemobasis', shortLabel: '基', functionNo: 'MO BASIS', moduleNo: 'OE' },
        { id: 'function:OE:MO DELETE', label: '工單刪除', tableName: 'tbloemobasis', shortLabel: '刪', functionNo: 'MO DELETE', moduleNo: 'OE' },
        { id: 'function:OE:MO QUERY', label: '工單查詢', tableName: 'tbloemobasis', shortLabel: '查', functionNo: 'MO QUERY', moduleNo: 'OE' },
        { id: 'function:OE:MO QUERY2', label: '工單達交率（準時開工）', tableName: 'tbloemobasis', shortLabel: '開', functionNo: 'MO QUERY2', moduleNo: 'OE' },
        { id: 'function:OE:MO QUERY3', label: '工單達交率（準時完工）', tableName: 'tbloemobasis', shortLabel: '完', functionNo: 'MO QUERY3', moduleNo: 'OE' },
        { id: 'function:OE:MO MATERIAL LIST', label: '工單用料清單', tableName: 'tbloemomateriallist', shortLabel: '料', functionNo: 'MO MATERIAL LIST', moduleNo: 'OE' },
        { id: 'function:OE:MO MATERIAL STATUS', label: '工單存料現況', tableName: 'tbloemomaterialstate', shortLabel: '存', functionNo: 'MO MATERIAL STATUS', moduleNo: 'OE' },
        { id: 'function:OE:MO CLOSE', label: '工單結案', tableName: 'tbloemobasis', shortLabel: '結', functionNo: 'MO CLOSE', moduleNo: 'OE' },
        { id: 'function:OE:MO COMPDATA MAINTAIN', label: '工單元件資料維護作業', tableName: 'tbloemocomponent', shortLabel: '元', functionNo: 'MO COMPDATA MAINTAIN', moduleNo: 'OE' },
      ],
    }, {
      id: 'menu:OE:INDUSTRY', label: '行業包', shortLabel: '行', children: [
        { id: 'function:OE:B_MO CHOOSE WAFER', label: '工單挑片資料查詢', tableName: 'tbloemocomponent', shortLabel: '挑', functionNo: 'B_MO CHOOSE WAFER', moduleNo: 'OE' },
        { id: 'function:OE:P_MO BASIS PKG', label: '工單開立_晶片測試', tableName: 'tbloemobasis', shortLabel: '晶', functionNo: 'P_MO BASIS PKG', moduleNo: 'OE' },
      ],
    }],
  },
  { id: 'module:WIP', label: '在製品管理', shortLabel: '製', moduleNo: 'WIP', children: [{
    id: 'menu:WIP:IN PROCESS', label: '在製品', shortLabel: '製', children: [
      { id: 'function:WIP:OPERATOR LOGIN', label: '作業員登入／登出（OP／Area）', tableName: 'tblusroperatorloginstate', shortLabel: 'OP登入', functionNo: 'OPERATOR LOGIN', moduleNo: 'WIP' },
      { id: 'function:WIP:OPERATOR LOGIN_LOT', label: '生產批作業員登入／登出', tableName: 'tblwiplotloginstate', shortLabel: 'Lot登入', functionNo: 'OPERATOR LOGIN_LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:OPERATOR CLOCKIN', label: '使用者登入作業（上工）', tableName: 'tblwipoperatorstate', shortLabel: '上工', functionNo: 'OPERATOR CLOCKIN', moduleNo: 'WIP' },
      { id: 'function:WIP:OPERATOR CLOCKOUT', label: '使用者登出作業（下工）', tableName: 'tblwipoperatorstate', shortLabel: '下工', functionNo: 'OPERATOR CLOCKOUT', moduleNo: 'WIP' },
      { id: 'function:WIP:OPERATOR ONLINE', label: '使用者登入(上工)資訊', tableName: 'tblwipoperatorstate', shortLabel: '上工資訊', functionNo: 'OPERATOR ONLINE', moduleNo: 'WIP' },
      { id: 'function:WIP:SWR BASIS', label: '特殊生產需求維護', tableName: 'tblwipswrbasis', shortLabel: '特殊需求', functionNo: 'SWR BASIS', moduleNo: 'WIP' },
      { id: 'function:WIP:UNDO DISPATCH', label: '工單撤派工', tableName: 'tblwiplotstate', shortLabel: '撤派工', functionNo: 'UNDO DISPATCH', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT OPERATOR CLOCKIN', label: '批次作業員上工', tableName: 'tblwipoperatorstate', shortLabel: '批次上工', functionNo: 'LOT OPERATOR CLOCKIN', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT OPERATOR CLOCKOUT', label: '批次作業員下工', tableName: 'tblwipoperatorstate', shortLabel: '批次下工', functionNo: 'LOT OPERATOR CLOCKOUT', moduleNo: 'WIP' },
      { id: 'function:WIP:CREATE LOT', label: '生產批開立', tableName: 'tblwiplotbasis', shortLabel: '批', functionNo: 'CREATE LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:CREATE LOT_BATCH', label: '生產批開立_批次', tableName: 'tblwiplotbasis', shortLabel: '批次', functionNo: 'CREATE LOT_BATCH', moduleNo: 'WIP' },
      { id: 'function:WIP:WORKORDER PRINT', label: '工令單列印', tableName: 'tblwiplotstate', shortLabel: '工令', functionNo: 'WORKORDER PRINT', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:PRODUCTION REPORTING', label: '生產報工', shortLabel: '報', children: [
      { id: 'function:WIP:EXECUTE LOT', label: '生產批執行', tableName: 'tblwiplotstate', shortLabel: '執', functionNo: 'EXECUTE LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:SKIP INSPECTION SETTING', label: '免檢批設定', tableName: 'xx_tblspcwaive', shortLabel: '免檢', functionNo: 'Skip Inspection Setting', moduleNo: 'WIP' },
      { id: 'function:WIP:EXECUTE LOT_COMP', label: '生產批執行_元件', tableName: 'tblwipcomponentstate', shortLabel: '元', functionNo: 'EXECUTE LOT_COMP', moduleNo: 'WIP' },
      { id: 'function:WIP:EXECUTE LOT_EQP', label: '生產批執行_設備', tableName: 'tbleqpfavorite', shortLabel: '設備', functionNo: 'EXECUTE LOT_EQP', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT CHOICE EQP', label: '生產批選用生產設備', tableName: 'tblwiplotstate', shortLabel: '選機', functionNo: 'LOT CHOICE EQP', moduleNo: 'WIP' },
      { id: 'function:WIP:IPQC', label: '計數不良原因收集', tableName: 'tblwipcont_abnormal', shortLabel: '不良', functionNo: 'IPQC', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT SPLIT', label: '分批處理', tableName: 'tblwiplotstate', shortLabel: '分批', functionNo: 'LOT SPLIT', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT SPLIT_COMP', label: '分批處理_元件', tableName: 'tblwipcomponentstate', shortLabel: '元件分批', functionNo: 'LOT SPLIT_COMP', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT SPLIT_BIN', label: '分批處理_等級', tableName: 'tblwipgoodbinstate', shortLabel: '等級分批', functionNo: 'LOT SPLIT_BIN', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT SPLIT_CARRIER', label: '分批處理_載具', tableName: 'tblwipcarrierstatedetail', shortLabel: '載具分批', functionNo: 'LOT SPLIT_CARRIER', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT MERGE', label: '併批處理', tableName: 'tblwiplotstate', shortLabel: '併批', functionNo: 'LOT MERGE', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT MERGE_COMP', label: '併批處理_元件', tableName: 'tblwipcomponentstate', shortLabel: '元件併批', functionNo: 'LOT MERGE_COMP', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT MERGE_BIN', label: '併批處理_等級', tableName: 'tblwipgoodbinstate', shortLabel: '等級併批', functionNo: 'LOT MERGE_BIN', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:CHIP TEST PRODUCTION MANAGEMENT', label: '芯片測試生產管理', shortLabel: '芯片', children: [
      { id: 'function:WIP:WPP01', label: '生產批開立_晶片測試（DEV）', shortLabel: 'CP開批', functionNo: 'WPP01', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP02', label: '晶圓針測資料匯出（DEV）', shortLabel: 'Wafer匯出', functionNo: 'WPP02', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP03', label: '設備設置單維護_晶片測試（DEV）', shortLabel: '設備設置', functionNo: 'WPP03', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP04', label: '晶圓針測紀錄查詢（DEV）', shortLabel: '針測紀錄', functionNo: 'WPP04', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP05', label: 'Golden MAP 檔案管理（DEV）', shortLabel: 'Golden MAP', functionNo: 'WPP05', moduleNo: 'WIP' },
      { id: 'function:WIP:P_BUMP MAP MERGE', label: 'Bump MAP疊圖作業', shortLabel: 'MAP疊圖', functionNo: 'P_BUMP MAP MERGE', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP06', label: 'Bump MAP 檔案管理（DEV）', shortLabel: 'MAP檔案', functionNo: 'WPP06', moduleNo: 'WIP' },
      { id: 'function:WIP:WPP08', label: '生產批自主產生元件（DEV）', shortLabel: '批次元件', functionNo: 'WPP08', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:EQUIPMENT AND CARRIER', label: '設備與載具', shortLabel: '設備', children: [
      { id: 'function:WIP:EQP MOUNT MATERIAL', label: '設備上料', tableName: 'tblwipeqpmaterialstate', shortLabel: '上料', functionNo: 'EQP MOUNT MATERIAL', moduleNo: 'WIP' },
      { id: 'function:WIP:EQP CONSUME STATUS', label: '設備耗品現況', tableName: 'tblwipeqpconsumestatus', shortLabel: '耗品', functionNo: 'EQP CONSUME STATUS', moduleNo: 'WIP' },
      { id: 'function:WIP:EQP CONSUME STATUS LOG', label: '設備耗品歷程', tableName: 'tblwipeqpconsumelog', shortLabel: '耗品歷程', functionNo: 'EQP CONSUME STATUS LOG', moduleNo: 'WIP' },
      { id: 'function:WIP:CARRIER STATE', label: '載具現況管理', tableName: 'tblwipcarrierstate', shortLabel: '載具', functionNo: 'CARRIER STATE', moduleNo: 'WIP' },
      { id: 'function:WIP:CARRIER MANAGEMENT COMP', label: '載具管理_元件', tableName: 'tblwipcarrierstatedetail', shortLabel: '載具元件', functionNo: 'CARRIER MANAGEMENT COMP', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:OUT SOURCE', label: '外包處理', shortLabel: '包', children: [
      { id: 'function:WIP:OS CREATE', label: '外包單開立', tableName: 'tblwiposbasis', shortLabel: '開立', functionNo: 'OS CREATE', moduleNo: 'WIP' },
      { id: 'function:WIP:OS CONFIRM', label: '外包單確認', tableName: 'tblwiposbasis', shortLabel: '確認', functionNo: 'OS CONFIRM', moduleNo: 'WIP' },
      { id: 'function:WIP:OS RETURN', label: '外包回貨', tableName: 'tblwiposdetail', shortLabel: '回貨', functionNo: 'OS RETURN', moduleNo: 'WIP' },
      { id: 'function:WIP:OS CONFIRM_ERP_CANCEL', label: '取消外包出貨 ERP', tableName: 'tblwiposbasis', shortLabel: '取消', functionNo: 'OS CONFIRM_ERP_CANCEL', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:PACKAGING', label: '包裝與庫房', shortLabel: '庫', children: [
      { id: 'function:WIP:FGDSTOCKIN_TP', label: '入庫至 TipTop ERP（DEV）', shortLabel: 'TipTop入庫', functionNo: 'FGDSTOCKIN_TP', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_TP_CANCEL', label: '取消入庫至 TipTop ERP（DEV）', shortLabel: 'TipTop取消', functionNo: 'FGDSTOCKIN_TP_CANCEL', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_WF', label: '入庫至 WorkFlow ERP（DEV）', shortLabel: 'WF入庫', functionNo: 'FGDSTOCKIN_WF', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_WF_CANCEL', label: '取消入庫 WorkFlow ERP（DEV）', shortLabel: 'WF取消', functionNo: 'FGDSTOCKIN_WF_CANCEL', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_T100', label: '入庫至 T100 ERP（DEV）', shortLabel: 'T100入庫', functionNo: 'FGDSTOCKIN_T100', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_T100_CANCEL', label: '取消入庫 T100 ERP（DEV）', shortLabel: 'T100取消', functionNo: 'FGDSTOCKIN_T100_CANCEL', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_E10ICD', label: '入庫至 E10ICD ERP（DEV）', shortLabel: 'ERP入庫', functionNo: 'FGDSTOCKIN_E10ICD', moduleNo: 'WIP' },
      { id: 'function:WIP:FGDSTOCKIN_E10ICD_CANCEL', label: '取消入庫 E10ICD ERP（DEV）', shortLabel: '取消入庫', functionNo: 'FGDSTOCKIN_E10ICD_CANCEL', moduleNo: 'WIP' },
      { id: 'function:WIP:SCRAPSTOCKIN_E10ICD', label: '不良品入庫至 E10ICD ERP（DEV）', shortLabel: '不良入庫', functionNo: 'SCRAPSTOCKIN_E10ICD', moduleNo: 'WIP' },
      { id: 'function:WIP:INV COUNT ADJUST', label: '盤點調帳（DEV）', shortLabel: '盤點調帳', functionNo: 'INV COUNT ADJUST', moduleNo: 'WIP' },
      { id: 'function:WIP:INV COUNT PRINT', label: '列印盤點清冊（DEV）', shortLabel: '盤點列印', functionNo: 'INV COUNT PRINT', moduleNo: 'WIP' },
      { id: 'function:WIP:INV COUNT STATE', label: '盤點關／開帳（DEV）', shortLabel: '盤點狀態', functionNo: 'INV COUNT STATE', moduleNo: 'WIP' },
      { id: 'function:WIP:BARTENDER', label: 'Bartender print（DEV）', shortLabel: '標籤列印', functionNo: 'BARTENDER', moduleNo: 'WIP' },
      { id: 'function:WIP:WIPUNINV', label: '取消入庫作業（DEV）', shortLabel: '取消入庫', functionNo: 'WIPUNINV', moduleNo: 'WIP' },
      { id: 'function:WIP:PACKING_MOLINE', label: '包裝作業', tableName: 'tblwipmolinepackl1', shortLabel: '包裝', functionNo: 'PACKING_MOLINE', moduleNo: 'WIP' },
      { id: 'function:WIP:PACKING_MOLINE_INV', label: '在製品庫包裝作業', tableName: 'tblinvpackstate', shortLabel: '庫包', functionNo: 'PACKING_MOLINE_INV', moduleNo: 'WIP' },
      { id: 'function:WIP:WIP INV', label: '在製品庫帳務維護', tableName: 'tblinvwipinventory_semi', shortLabel: '在製品庫', functionNo: 'WIP INV', moduleNo: 'WIP' },
      { id: 'function:WIP:SEMI INV', label: '半成品庫帳務維護', tableName: 'tblinvsemiinventory', shortLabel: '半成品庫', functionNo: 'SEMI INV', moduleNo: 'WIP' },
      { id: 'function:WIP:FGD INV', label: '成品庫帳務維護', tableName: 'tblinvfgdinventory', shortLabel: '成品庫', functionNo: 'FGD INV', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:HOLD AND WAIT', label: '異常與等待', shortLabel: '異常', children: [
      { id: 'function:WIP:ERF BASIS', label: '異常單資料', tableName: 'tblwiperfbasis', shortLabel: '異常', functionNo: 'ERF BASIS', moduleNo: 'WIP' },
      { id: 'function:WIP:WIP ERF UNRELEASED', label: '未解除異常單', tableName: 'tblwiperfbasis', shortLabel: '未解除', functionNo: 'WIP ERF UNRELEASED', moduleNo: 'WIP' },
      { id: 'function:WIP:WAIT BASIS', label: '等待單資料', tableName: 'tblwipwaitbasis', shortLabel: '等待', functionNo: 'WAIT BASIS', moduleNo: 'WIP' },
      { id: 'function:WIP:WIP WAIT DISPOSITION', label: '等待處置', tableName: 'tblwipwaitbasis', shortLabel: '處置', functionNo: 'WIP WAIT DISPOSITION', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:8D MANAGEMENT', label: '8D 管理', shortLabel: '8D', children: [
      { id: 'function:WIP:8D BASIS_FACTORY', label: '廠內 8D 矯正單', tableName: 'tblwip8dbasis_factory', shortLabel: '8D', functionNo: '8D BASIS_FACTORY', moduleNo: 'WIP' },
      { id: 'function:WIP:8D MAINTAIN', label: '8D 矯正單維護作業', tableName: 'tblwip8dbasis_factory', shortLabel: '維護', functionNo: '8D MAINTAIN', moduleNo: 'WIP' },
      { id: 'function:WIP:8D CLOSE', label: '8D 矯正單結案作業', tableName: 'tblwip8d_release', shortLabel: '結案', functionNo: '8D CLOSE', moduleNo: 'WIP' },
      { id: 'function:WIP:8D QUERY', label: '8D 矯正單查詢', tableName: 'tblwip8dbasis_factory', shortLabel: '查詢', functionNo: '8D QUERY', moduleNo: 'WIP' },
    ],
  }, {
    id: 'menu:WIP:EXCEPTION', label: '例外處理', shortLabel: '例外', children: [
      { id: 'function:WIP:REVERSE LOT', label: '回復處理', tableName: 'tblwipreversehistory', shortLabel: '回復', functionNo: 'REVERSE LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:DELETE RELEASED LOT', label: '刪除已下線生產批', tableName: 'tblwiplotstate', shortLabel: '刪批', functionNo: 'DELETE RELEASED LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:DELETE BATCH LOT', label: '批次刪除已下線生產批', tableName: 'tblwiplotstate', shortLabel: '批刪', functionNo: 'DELETE BATCH LOT', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT OP AREA CHANGE', label: '生產批作業區變更', tableName: 'tblwiplotoparea', shortLabel: '作業區', functionNo: 'LOT OP AREA CHANGE', moduleNo: 'WIP' },
      { id: 'function:WIP:PRODUCT VERSION CHANGE', label: '產品版本變更', tableName: 'tblwiplotbasis', shortLabel: '版本', functionNo: 'PRODUCT VERSION CHANGE', moduleNo: 'WIP' },
      { id: 'function:WIP:ECN WIZARD', label: 'ECN 設定精靈', tableName: 'tblwipecnbasis', shortLabel: 'ECN', functionNo: 'ECN WIZARD', moduleNo: 'WIP' },
      { id: 'function:WIP:CLOSE LOT FORCIBLY', label: '生產批強制結批作業', tableName: 'tblwiplotbasis', shortLabel: '結批', functionNo: 'CLOSE LOT FORCIBLY', moduleNo: 'WIP' },
      { id: 'function:WIP:CHANGELOTPRODUCT', label: '生產批產品變更作業', tableName: 'tblwiplotbasis', shortLabel: '轉產品', functionNo: 'CHANGELOTPRODUCT', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT PROPERTY ADJUST', label: '生產批屬性調整', tableName: 'tblwiplotproperty', shortLabel: '屬性', functionNo: 'LOT PROPERTY ADJUST', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT OP CONT ATTRIB ADJUST', label: '生產批作業站製程特性值調整', tableName: 'tblwipcont_attrib', shortLabel: '特性', functionNo: 'LOT OP CONT ATTRIB ADJUST', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT OP CONT MATERIAL ADJUST', label: '生產批作業站用料記錄調整', tableName: 'tblwipcont_material', shortLabel: '用料', functionNo: 'LOT OP CONT MATERIAL ADJUST', moduleNo: 'WIP' },
      { id: 'function:WIP:TRANSFER FACTORY', label: '換廠作業', tableName: 'tblwiplotstate', shortLabel: '換廠', functionNo: 'TRANSFER FACTORY', moduleNo: 'WIP' },
      { id: 'function:WIP:LABEL PRINT', label: '標籤列印', tableName: 'tblwiplabelprintlog', shortLabel: '標籤', functionNo: 'LABEL PRINT', moduleNo: 'WIP' },
      { id: 'function:WIP:LOT QTY ADJUST', label: '生產批數量調整', tableName: 'tblwipadjustlog', shortLabel: '數量', functionNo: 'LOT QTY ADJUST', moduleNo: 'WIP' },
    ],
  }] },
  {
    id: 'module:EMS', label: '設備管理', shortLabel: '設', moduleNo: 'EMS', children: [{
      id: 'menu:EMS:EQP EXECUTION', label: '設備_執行', shortLabel: '設', children: [
        { id: 'function:EMS:SHOW EQP STATUS BY AREA', label: '設備狀態監控_區域', shortLabel: '區監', functionNo: 'SHOW EQP STATUS BY AREA', moduleNo: 'EMS' },
        { id: 'function:EMS:SHOW EQP STATUS BY TYPE', label: '設備狀態監控_設備類別', shortLabel: '類監', functionNo: 'SHOW EQP STATUS BY TYPE', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP STATE CHANGE', label: '改變設備狀態', shortLabel: '狀', functionNo: 'EQP STATE CHANGE', moduleNo: 'EMS' },
      ],
    }, {
      id: 'menu:EMS:EMS CONFIG', label: 'EMS設定', shortLabel: '設', children: [
        { id: 'function:EMS:EMS CONTROL', label: '設備控管設定', shortLabel: '設控', functionNo: 'EMS CONTROL', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP STATE CONTROL SETTING', label: '設備狀態控制設定', shortLabel: '控', functionNo: 'EQP STATE CONTROL SETTING', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC STATE CONTROL SETTING', label: '配件狀態控制設定', shortLabel: '配控', functionNo: 'ACC STATE CONTROL SETTING', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC VIOLATIVE STATE SETTING', label: '配件狀態互斥設定', shortLabel: '配件互斥', functionNo: 'ACC VIOLATIVE STATE SETTING', moduleNo: 'EMS' },
        { id: 'function:EMS:VIOLATIVE STATE SETTING', label: '設備狀態互斥設定', shortLabel: '互斥', functionNo: 'VIOLATIVE STATE SETTING', moduleNo: 'EMS' },
        { id: 'function:EMS:EQUIPMENT REASON BASIS', label: '設備狀態變更原因資料', shortLabel: '原因', functionNo: 'EQUIPMENT REASON BASIS', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP CAP BASIS', label: '設備能力項目設定', shortLabel: '能力項目', functionNo: 'EQP CAP BASIS', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP CAP MAPPING', label: '設備能力設定', shortLabel: '設備能力', functionNo: 'EQP CAP MAPPING', moduleNo: 'EMS' },
      ],
    }, {
      id: 'menu:EMS:ACC EXECUTION', label: '配件_執行', shortLabel: '配', children: [
        { id: 'function:EMS:ACC MONITOR', label: '配件狀態監控', shortLabel: '監控', functionNo: 'ACC MONITOR', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC MONITOR BY EQP', label: '配件狀態監控_設備', shortLabel: '設備配', functionNo: 'ACC MONITOR BY EQP', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC STATE LOG QUERY', label: '配件歷史資料查詢', shortLabel: '配件歷程', functionNo: 'ACC STATE LOG QUERY', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP ACC STATE LOG QUERY', label: '設備配件歷史資料查詢', shortLabel: '設備配件歷程', functionNo: 'EQP ACC STATE LOG QUERY', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP ACC STATE QUERY', label: '設備配件現況查詢', shortLabel: '配件現況', functionNo: 'EQP ACC STATE QUERY', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC STATE QUERY', label: '配件狀態查詢', shortLabel: '配件狀態', functionNo: 'ACC STATE QUERY', moduleNo: 'EMS' },
        { id: 'function:EMS:ACC MONITOR_BU1', label: '配件狀態監控_管理', shortLabel: '管理配件', functionNo: 'ACC MONITOR_BU1', moduleNo: 'EMS' },
        { id: 'function:EMS:ACCESSORY REPAIR', label: '配件壽命維護', shortLabel: '壽命', functionNo: 'ACCESSORY REPAIR', moduleNo: 'EMS' },
        { id: 'function:EMS:APPLY EQP ACCESSORY', label: '配件上機', shortLabel: '上機', functionNo: 'APPLY EQP ACCESSORY', moduleNo: 'EMS' },
        { id: 'function:EMS:RETURN EQP ACCESSORY', label: '配件下機', shortLabel: '下機', functionNo: 'RETURN EQP ACCESSORY', moduleNo: 'EMS' },
        { id: 'function:EMS:SHOW EQP LOG', label: '設備歷史資料查詢', shortLabel: '統計', functionNo: 'SHOW EQP LOG', moduleNo: 'EMS' },
        { id: 'function:EMS:SHOW EQP LOG BY STATE', label: '設備歷史資料查詢_狀態', shortLabel: '日統計', functionNo: 'SHOW EQP LOG BY STATE', moduleNo: 'EMS' },
        { id: 'function:EMS:EQP STATE STATISTICS_EMS', label: '設備狀態期間統計查詢', shortLabel: '統計', functionNo: 'EQP STATE STATISTICS_EMS', moduleNo: 'EMS' },
        { id: 'function:EMS:EQUIPMENT STATE LOG', label: '設備狀態記錄查詢', shortLabel: '歷程', functionNo: 'EQUIPMENT STATE LOG', moduleNo: 'EMS' },
      ],
    }],
  },
]

const menuTree = ref<MenuNode[]>(fallbackMenuTree)
const menuSource = ref<'loading' | 'api' | 'fallback' | 'error'>('fallback')
const menuError = ref('')

const menuRoutes: Record<string, string> = {
  'function:EMS:SHOW EQP STATUS BY AREA': appBasePath + '/EMS/EquipmentStateByArea',
  'function:EMS:SHOW EQP STATUS BY TYPE': appBasePath + '/EMS/EquipmentStateByType',
  'function:EMS:ACC MONITOR': appBasePath + '/EMS/AccessoryStateMonitor',
  'function:EMS:ACC MONITOR BY EQP': appBasePath + '/EMS/AccessoryStateMonitorByEquipment',
  'function:EMS:ACC STATE CONTROL SETTING': appBasePath + '/EMS/AccessoryStateControlSet',
  'function:EMS:ACC STATE LOG QUERY': appBasePath + '/EMS/AccessoryStateLogQuery',
  'function:EMS:ACC STATE QUERY': appBasePath + '/EMS/AccessoryStateQuery',
  'function:EMS:ACC MONITOR_BU1': appBasePath + '/EMS/AccessoryStateMonitorBu1',
  'function:EMS:ACCESSORY REPAIR': appBasePath + '/EMS/AccessoryRepair',
  'function:EMS:APPLY EQP ACCESSORY': appBasePath + '/EMS/ApplyEquipmentAccessory',
  'function:EMS:ACC VIOLATIVE STATE SETTING': appBasePath + '/EMS/AccessoryStateViolativeSetting',
  'function:EMS:VIOLATIVE STATE SETTING': appBasePath + '/EMS/ViolateStateChangeSetMatrix',
  'function:EMS:RETURN EQP ACCESSORY': appBasePath + '/EMS/ReturnEquipmentAccessory',
  'function:EMS:SHOW EQP LOG': appBasePath + '/EMS/EquipmentStateLogQuery',
  'function:EMS:SHOW EQP LOG BY STATE': appBasePath + '/EMS/EquipmentStateLogByState',
  'function:EMS:EQP STATE CHANGE': appBasePath + '/EMS/EquipmentStateChange',
  'function:EMS:EMS CONTROL': appBasePath + '/EMS/EquipmentControl',
  'function:EMS:EQP ACC STATE LOG QUERY': appBasePath + '/EMS/EquipmentAccStateLogQuery',
  'function:EMS:EQP ACC STATE QUERY': appBasePath + '/EMS/EquipmentAccStateQuery',
  'function:EMS:EQP CAP BASIS': appBasePath + '/EMS/EquipmentCapabilityBasis',
  'function:EMS:EQP CAP MAPPING': appBasePath + '/EMS/EquipmentCapabilityMapping',
  'function:EMS:EQUIPMENT STATE LOG': appBasePath + '/EMS/EquipmentStateLog',
  'function:EMS:EQP STATE CONTROL SETTING': appBasePath + '/EMS/EqpStateControlSet',
  'function:EMS:EQP STATE STATISTICS_EMS': appBasePath + '/EMS/EquipmentStateStatistics',
  'function:EMS:EQUIPMENT REASON BASIS': appBasePath + '/EMS/EquipmentReasonBasis',
  'function:WIP:RUN CARD': appBasePath + '/WIP/RunCard',
  'function:WIP:LOT SHEET PRINT': appBasePath + '/WIP/LotSheetPrint',
  'function:WIP:WORKORDER PRINT': appBasePath + '/WIP/WorkorderPrint',
  'function:WIP:SKIP INSPECTION SETTING': appBasePath + '/WIP/SkipInspectionSetting',
  'function:WIP:P_BUMP MAP MERGE': appBasePath + '/WIP/BumpMapMerge',
  'function:WIP:WPP01': appBasePath + '/WIP/LotBasisCreateCp',
  'function:WIP:WPP02': appBasePath + '/WIP/WaferMapExport',
  'function:WIP:WPP03': appBasePath + '/WIP/EqpSetupBasisCp',
  'function:WIP:WPP04': appBasePath + '/WIP/ProbingLog',
  'function:WIP:WPP05': appBasePath + '/WIP/GoldenMapFileManage',
  'function:WIP:WPP06': appBasePath + '/WIP/BumpMapFileManage',
  'function:WIP:WPP08': appBasePath + '/WIP/LotComponentGen',
  'function:WIP:FGDSTOCKIN_E10ICD': appBasePath + '/WIP/FgdStockInE10Icd',
  'function:WIP:FGDSTOCKIN_T100': appBasePath + '/WIP/FgdStockInT100',
  'function:WIP:FGDSTOCKIN_TP': appBasePath + '/WIP/FgdStockInTp',
  'function:WIP:FGDSTOCKIN_TP_CANCEL': appBasePath + '/WIP/FgdStockInTpCancel',
  'function:WIP:FGDSTOCKIN_WF': appBasePath + '/WIP/FgdStockInWorkFlow',
  'function:WIP:FGDSTOCKIN_WF_CANCEL': appBasePath + '/WIP/FgdStockInWorkFlowCancel',
  'function:WIP:FGDSTOCKIN_T100_CANCEL': appBasePath + '/WIP/FgdStockInT100Cancel',
  'function:WIP:FGDSTOCKIN_E10ICD_CANCEL': appBasePath + '/WIP/FgdStockInE10IcdCancel',
  'function:WIP:SCRAPSTOCKIN_E10ICD': appBasePath + '/WIP/ScrapStockInE10Icd',
  'function:WIP:INV COUNT ADJUST': appBasePath + '/WIP/InvCountStateAdjust',
  'function:WIP:INV COUNT PRINT': appBasePath + '/WIP/InvCountStatePrint',
  'function:WIP:INV COUNT STATE': appBasePath + '/WIP/InvCountState',
  'function:WIP:BARTENDER': appBasePath + '/WIP/BartenderPrint',
  'function:WIP:WIPUNINV': appBasePath + '/WIP/UnInventory',
  'function:WIP:EQP FAVORITE SETUP': appBasePath + '/WIP/EquipmentFavorites',
  'function:WIP:EQP QCLIST ONLINE CHECK': appBasePath + '/WIP/EquipmentOnlineQC',
  'function:WIP:EQP QCLIST LOG': appBasePath + '/WIP/EquipmentQcListLog',
  'function:WIP:PRODUCTION STATE BY AREA': appBasePath + '/WIP/AreaShow',
  'function:WIP:OPERATOR LOGIN': appBasePath + '/WIP/OperatorLogin',
  'function:WIP:OPERATOR LOGIN_LOT': appBasePath + '/WIP/OperatorLoginLot',
  'function:WIP:OPERATOR CLOCKIN': appBasePath + '/WIP/OperatorClockIn',
  'function:WIP:OPERATOR CLOCKOUT': appBasePath + '/WIP/OperatorClockOut',
  'function:WIP:OPERATOR ONLINE': appBasePath + '/WIP/OperatorOnline',
  'function:WIP:SWR BASIS': appBasePath + '/WIP/SwrBasis',
  'function:WIP:UNDO DISPATCH': appBasePath + '/WIP/UndoDispatch',
  'function:WIP:OS CREATE': appBasePath + '/WIP/OutsourceCreate',
  'function:WIP:OS CONFIRM': appBasePath + '/WIP/OutsourceConfirm',
  'function:WIP:OS RETURN': appBasePath + '/WIP/OutsourceReturn',
  'function:WIP:OS CONFIRM_ERP_CANCEL': appBasePath + '/WIP/OutsourceErpCancel',
  'function:WIP:REVERSE LOT': appBasePath + '/WIP/LotReverse',
  'function:WIP:DELETE RELEASED LOT': appBasePath + '/WIP/ReleasedLotDelete',
  'function:WIP:DELETE BATCH LOT': appBasePath + '/WIP/ReleasedLotBatchDelete',
  'function:WIP:LOT OP AREA CHANGE': appBasePath + '/WIP/LotAreaChange',
  'function:WIP:PRODUCT VERSION CHANGE': appBasePath + '/WIP/ProductVersionChange',
  'function:WIP:ECN WIZARD': appBasePath + '/WIP/EcnWizard',
  'function:WIP:CLOSE LOT FORCIBLY': appBasePath + '/WIP/CloseLotForcibly',
  'function:WIP:CHANGELOTPRODUCT': appBasePath + '/WIP/ChangeLotProduct',
  'function:WIP:LOT PROPERTY ADJUST': appBasePath + '/WIP/LotPropertyAdjust',
  'function:WIP:LOT OP CONT ATTRIB ADJUST': appBasePath + '/WIP/LotProcessAttributeAdjust',
  'function:WIP:LOT OP CONT MATERIAL ADJUST': appBasePath + '/WIP/LotMaterialAdjust',
  'function:WIP:TRANSFER FACTORY': appBasePath + '/WIP/TransferFactory',
  'function:WIP:LABEL PRINT': appBasePath + '/WIP/LabelPrint',
  'function:WIP:LOT QTY ADJUST': appBasePath + '/WIP/LotQuantityAdjust',
  'function:WIP:LOT OPERATOR CLOCKIN': appBasePath + '/WIP/LotOperatorClockIn',
  'function:WIP:LOT OPERATOR CLOCKOUT': appBasePath + '/WIP/LotOperatorClockOut',
  'function:WIP:CREATE LOT': appBasePath + '/WIP/LotBasisCreate',
  'function:WIP:CREATE LOT_BATCH': appBasePath + '/WIP/LotBasisBatch',
  'function:WIP:EXECUTE LOT': appBasePath + '/WIP/LotExecution',
  'function:WIP:EXECUTE LOT_COMP': appBasePath + '/WIP/LotExecutionByComponent',
  'function:WIP:EXECUTE LOT_EQP': appBasePath + '/WIP/EquipmentLotState',
  'function:WIP:LOT CHOICE EQP': appBasePath + '/WIP/LotChoiceEquipment',
  'function:WIP:IPQC': appBasePath + '/WIP/IPQC',
  'function:WIP:LOT SPLIT': appBasePath + '/WIP/LotSplit',
  'function:WIP:LOT SPLIT_COMP': appBasePath + '/WIP/LotSplitComponent',
  'function:WIP:LOT SPLIT_BIN': appBasePath + '/WIP/LotSplitBin',
  'function:WIP:LOT SPLIT_CARRIER': appBasePath + '/WIP/LotSplitCarrier',
  'function:WIP:LOT MERGE': appBasePath + '/WIP/LotMerge',
  'function:WIP:LOT MERGE_COMP': appBasePath + '/WIP/LotMergeComponent',
  'function:WIP:LOT MERGE_BIN': appBasePath + '/WIP/LotMergeBin',
  'function:WIP:LOT MERGE_CARRIER': appBasePath + '/WIP/LotMergeCarrier',
  'function:WIP:LOT COMP SPLIT': appBasePath + '/WIP/ComponentSplit',
  'function:WIP:LOT HOTLOT AND PRIPORITY CHANGE': appBasePath + '/WIP/HotLotPriority',
  'function:WIP:IMMEDIATE HOLD SET': appBasePath + '/WIP/ImmediateHoldSet',
  'function:WIP:IMMEDIATE WAIT SET': appBasePath + '/WIP/ImmediateWaitSet',
  'function:WIP:IMMEDIATE HOLD SET_BATCH': appBasePath + '/WIP/BatchImmediateHold',
  'function:WIP:FUTURE HOLD SET': appBasePath + '/WIP/FutureHoldSet',
  'function:WIP:FUTURE WAIT SET': appBasePath + '/WIP/FutureWaitSet',
  'function:WIP:FUTURE HOLD RELEASE': appBasePath + '/WIP/FutureHoldRelease',
  'function:WIP:FUTURE WAIT RELEASE': appBasePath + '/WIP/FutureWaitRelease',
  'function:WIP:ERF BASIS': appBasePath + '/WIP/ErfBasis',
  'function:WIP:WIP ERF UNRELEASED': appBasePath + '/WIP/ErfUnreleased',
  'function:WIP:WAIT BASIS': appBasePath + '/WIP/WaitBasis',
  'function:WIP:WIP WAIT DISPOSITION': appBasePath + '/WIP/WaitDisposition',
  'function:WIP:8D BASIS_FACTORY': appBasePath + '/WIP/8DBasisFactory',
  'function:WIP:8D MAINTAIN': appBasePath + '/WIP/8DMaintain',
  'function:WIP:8D CLOSE': appBasePath + '/WIP/8DClose',
  'function:WIP:8D QUERY': appBasePath + '/WIP/8DQuery',
  'function:WIP:EQP MOUNT MATERIAL': appBasePath + '/WIP/EquipmentMaterialLoading',
  'function:WIP:EQP CONSUME STATUS': appBasePath + '/WIP/EquipmentConsumeStatus',
  'function:WIP:EQP CONSUME STATUS LOG': appBasePath + '/WIP/EquipmentConsumeStatusLog',
  'function:WIP:CARRIER STATE': appBasePath + '/WIP/CarrierState',
  'function:WIP:CARRIER MANAGEMENT COMP': appBasePath + '/WIP/CarrierManagement',
  'function:WIP:PACKING_MOLINE': appBasePath + '/WIP/PackingMOLine',
  'function:WIP:PACKING_MOLINE_INV': appBasePath + '/WIP/PackingMOLineInv',
  'function:WIP:WIP INV': appBasePath + '/WIP/WipInventory',
  'function:WIP:SEMI INV': appBasePath + '/WIP/SemiInventory',
  'function:WIP:FGD INV': appBasePath + '/WIP/FgdInventory',
  'function:OE:RO BASIS': appBasePath + '/OE/ReceivingOrders',
  'function:OE:RO CLOSE': appBasePath + '/OE/ReceivingOrderClose',
  'function:OE:MO BASIS': appBasePath + '/OE/ManufacturingOrders',
  'function:OE:MO DELETE': appBasePath + '/OE/ManufacturingOrderDelete',
  'function:OE:MO QUERY': appBasePath + '/OE/ManufacturingOrderQuery',
  'function:OE:MO QUERY2': appBasePath + '/OE/ManufacturingOrderStartRate',
  'function:OE:MO QUERY3': appBasePath + '/OE/ManufacturingOrderFinishRate',
  'function:OE:MO MATERIAL LIST': appBasePath + '/OE/ManufacturingOrderMaterials',
  'function:OE:MO MATERIAL STATUS': appBasePath + '/OE/ManufacturingOrderMaterialStock',
  'function:OE:MO CLOSE': appBasePath + '/OE/ManufacturingOrderClose',
  'function:OE:MO COMPDATA MAINTAIN': appBasePath + '/OE/ManufacturingOrderComponents',
  'function:OE:B_MO CHOOSE WAFER': appBasePath + '/OE/ManufacturingOrderWaferQuery',
  'function:OE:P_MO BASIS PKG': appBasePath + '/OE/ChipTestManufacturingOrders',
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
  'function:SPC:SPC ONLIE CONTROL': appBasePath + '/SPC/OnlineControl',
  'function:SPC:SPC ONLIE CONTROL CONTINUE': appBasePath + '/SPC/OnlineControlContinue',
  'function:SPC:INCOMING QC FORM': appBasePath + '/SPC/IncomingQCForm',
  'function:SPC:IQC FORM FOR COMPONENT': appBasePath + '/SPC/IncomingQCFormComponent',
  'function:SPC:FQC FORM': appBasePath + '/SPC/FinalQCForm',
  'function:SPC:IPQC FORM': appBasePath + '/SPC/InProcessQCForm',
  'function:SPC:OQC FORM': appBasePath + '/SPC/OutgoingQCForm',
  'function:SPC:SPC DATA': appBasePath + '/SPC/DataMaintenance',
  'function:SPC:SPC DATA BY LOTNO': appBasePath + '/SPC/DataByLotNo',
  'function:SPC:DATA EDIT LOG': appBasePath + '/SPC/DataEditLog',
  'function:SPC:CHART LINE EDIT LOG': appBasePath + '/SPC/ChartLineEditLog',
  'function:SPC:QCITEM BINDING HIS LOG': appBasePath + '/SPC/QcItemBindingHisLog',
  'function:SPC:CHART ALL CAPABILITY': appBasePath + '/SPC/ChartAllCapability',
  'function:SPC:CHART ALL CAPABILITY BY FACTOR': appBasePath + '/SPC/ChartAllCapabilityByFactor',
  'function:SPC:MONTHLY CHART REPORT CODE': appBasePath + '/SPC/MonthRPTCodeBasis',
  'function:SPC:MONTHLY CHART REPORT': appBasePath + '/SPC/MonthChartReport',
  'function:SPC:MONTHLY CHART REPORT BY FACTOR': appBasePath + '/SPC/MonthChartReportByFactor',
  'function:SPC:SPC ERF QUERY': appBasePath + '/SPC/ErfBasis',
  'function:SPC:SPC ERF UNRELEASED': appBasePath + '/SPC/ErfUnreleased',
  'function:SPC:XLS SOURCE FILE CONTROL': appBasePath + '/SPC/XlsSourceFileControl',
  'function:SPC:CHART SIGNBOARD': appBasePath + '/SPC/ChartSignboard',
  'function:SPC:CONTROL CHART ANALYSIS': appBasePath + '/SPC/ControlChartAnalysis',
  'function:SPC:CONTROL CHART ANALYSIS BY FACTOR': appBasePath + '/SPC/ControlChartAnalysisByFactor',
  'function:SPC:LINE CHART ANALYSIS': appBasePath + '/SPC/LineChartAnalysis',
  'function:SPC:SCATTER DIAGRAM': appBasePath + '/SPC/ScatterDiagram',
  'function:SPC:PCHART SIGNBOARD': appBasePath + '/SPC/PChartSignboard',
  'function:SPC:PARETO DIAGRAM': appBasePath + '/SPC/ParetoDiagram',
  'function:SPC:LRR / PPM CHART': appBasePath + '/SPC/LrrPpmChart',
  'function:SPC:ATT-CHART': appBasePath + '/SPC/AttributeChart',
  'function:SPC:SPC COA': appBasePath + '/SPC/CoaReport',
  'function:SPC:IMPORT VAR MEASURE': appBasePath + '/SPC/VariableMeasurementImport',
  'function:SPC:INSP MACHINE PARAMETER SETUP': appBasePath + '/SPC/InspMachineParameterSetup',
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
  'function:PRS:PRS BASIS': `${appBasePath}/PRS/ProcessBasis`,
  'function:PRS:PRS TYPE': `${appBasePath}/PRS/ProcessType`,
  'function:PRS:PS BASIS': `${appBasePath}/PRS/ProductionSegments`,
  'function:PRS:STAGE BASIS': `${appBasePath}/PRS/FabPrsStageBasis`,
  'operation-master': `${appBasePath}/MasterData/Operations`,
  'function:OP:OP TYPE': appBasePath + '/MasterData/OperationTypes',
  'function:OP:OP RULE BASIS': appBasePath + '/OP/ControlRules',
  'function:OP:BR BASIS': appBasePath + '/OP/BusinessRules',
  'function:OP:OP GROUP BASIS': appBasePath + '/OP/Groups',
  'function:OP:B_CUSTOMER OP MAPPING': appBasePath + '/OP/CustomerOPMapping',
  'function:OP:B_ECN OP GROUP BASIS': appBasePath + '/OP/ECNOperationGroups',
  'function:OP:B_OP GROUP ATTRIB': appBasePath + '/OP/OperationGroupAttributes',
  'material-master': `${appBasePath}/MasterData/Materials`,
  'product-master': `${appBasePath}/MasterData/Products`,
  'function:PRD:PRD TYPE': `${appBasePath}/MasterData/ProductTypes`,
  'function:PRD:MODULE BASIS': `${appBasePath}/MasterData/ProductModules`,
  'function:PRD:PRODUCT SEARCH': `${appBasePath}/PRD/ProductSearch`,
  'function:PRD:PRD RECIPE BASIS': `${appBasePath}/PRD/RecipeGroups`,
  'function:PRD:LIMITED TIME CONTROL': `${appBasePath}/PRD/TimeLimits`,
  'function:PRD:LABEL DATA': `${appBasePath}/PRD/LabelData`,
  'function:PRD:PRODUCT LABEL MAP BASIS': `${appBasePath}/PRD/ProductLabelMap`,
  'function:PRD:PRODUCT OP DEFAULT': `${appBasePath}/PRD/OperationDefaults`,
  'function:PRD:PRODUCT OP AMEND': `${appBasePath}/PRD/OperationAmend`,
  'function:PRD:PRODUCT OP COPY': `${appBasePath}/PRD/OperationCopy`,
  'function:PRD:PRD PACKING RULE BASIS': `${appBasePath}/PRD/PackingRules`,
  'function:PRD:PRD PACKINGRULE MAP': `${appBasePath}/PRD/PackingRuleMap`,
  'function:PRD:USER DEFINED RUNCARD': `${appBasePath}/PRD/RunCards`,
  'function:PRD:PRODUCT RUN CARD MAP BASIS': `${appBasePath}/PRD/ProductRunCardMap`,
  'function:PRD:PRD QCITEM SPEC IMPORT': `${appBasePath}/PRD/QCSpecImport`,
  'function:PRD:PRD GBOM IMPORT': `${appBasePath}/PRD/GbomBatchImport`,
  'function:PRD:FT PROGRAM BASIS': `${appBasePath}/PRD/FTPrograms`,
  'function:PRD:EQC SAMPLE RULE': `${appBasePath}/PRD/EQCSampleRules`,
  'function:PRD:PRD QCRULE BASIS': `${appBasePath}/PRD/QCFrequencyRules`,
  'function:PRD:PRD WORKTIME': `${appBasePath}/PRD/WorkTime`,
  'function:PRD:PRD OVERSTAY': `${appBasePath}/PRD/Overstay`,
  'function:PRD:PRD OP RULE': `${appBasePath}/PRD/StationRules`,
  'function:PRD:P_CP WAFER YIELD SHIP REPORT': `${appBasePath}/PRD/CPWaferShipReports`,
  'function:PRD:P_CP PROGRAM DEFINE': `${appBasePath}/PRD/CPPrograms`,
  'function:PRD:P_WAFER RULE MANAGER': `${appBasePath}/PRD/CPWaferRules`,
  'function:PRD:P_PRD WAFERMAP MERGE RULE': `${appBasePath}/PRD/CPMergeMapRules`,
  'function:PRD:B_CHOOSE WAFER RULES': `${appBasePath}/PRD/ChooseWaferRules`,
  'function:PRD:B_PRD BASIS_IC': `${appBasePath}/PRD/PackagingProducts`,
  'function:PRD:B_UPLOAD FILE BASIS': `${appBasePath}/PRD/UploadFileTypes`,
  'function:PRD:B_TEST PROGRAM FILE MANAGEMENT': `${appBasePath}/PRD/TestProgramFiles`,
}

const functionRoutes: Record<string, string> = {
  'OPERATOR LOGIN': appBasePath + '/WIP/OperatorLogin',
  'OPERATOR LOGIN_LOT': appBasePath + '/WIP/OperatorLoginLot',
  'OPERATOR CLOCKIN': appBasePath + '/WIP/OperatorClockIn',
  'OPERATOR CLOCKOUT': appBasePath + '/WIP/OperatorClockOut',
  'OPERATOR ONLINE': appBasePath + '/WIP/OperatorOnline',
  'SWR BASIS': appBasePath + '/WIP/SwrBasis',
  'UNDO DISPATCH': appBasePath + '/WIP/UndoDispatch',
  'OS CREATE': appBasePath + '/WIP/OutsourceCreate',
  'OS CONFIRM': appBasePath + '/WIP/OutsourceConfirm',
  'OS RETURN': appBasePath + '/WIP/OutsourceReturn',
  'OS CONFIRM_ERP_CANCEL': appBasePath + '/WIP/OutsourceErpCancel',
  'REVERSE LOT': appBasePath + '/WIP/LotReverse',
  'DELETE RELEASED LOT': appBasePath + '/WIP/ReleasedLotDelete',
  'DELETE BATCH LOT': appBasePath + '/WIP/ReleasedLotBatchDelete',
  'LOT OP AREA CHANGE': appBasePath + '/WIP/LotAreaChange',
  'PRODUCT VERSION CHANGE': appBasePath + '/WIP/ProductVersionChange',
  'ECN WIZARD': appBasePath + '/WIP/EcnWizard',
  'CLOSE LOT FORCIBLY': appBasePath + '/WIP/CloseLotForcibly',
  'CHANGELOTPRODUCT': appBasePath + '/WIP/ChangeLotProduct',
  'LOT PROPERTY ADJUST': appBasePath + '/WIP/LotPropertyAdjust',
  'LOT OP CONT ATTRIB ADJUST': appBasePath + '/WIP/LotProcessAttributeAdjust',
  'LOT OP CONT MATERIAL ADJUST': appBasePath + '/WIP/LotMaterialAdjust',
  'TRANSFER FACTORY': appBasePath + '/WIP/TransferFactory',
  'LABEL PRINT': appBasePath + '/WIP/LabelPrint',
  BARTENDER: appBasePath + '/WIP/BartenderPrint',
  WIPUNINV: appBasePath + '/WIP/UnInventory',
  'LOT QTY ADJUST': appBasePath + '/WIP/LotQuantityAdjust',
  'ACC MONITOR': appBasePath + '/EMS/AccessoryStateMonitor',
  'ACC MONITOR BY EQP': appBasePath + '/EMS/AccessoryStateMonitorByEquipment',
  'ACC STATE CONTROL SETTING': appBasePath + '/EMS/AccessoryStateControlSet',
  'ACC STATE LOG QUERY': appBasePath + '/EMS/AccessoryStateLogQuery',
  'ACC STATE QUERY': appBasePath + '/EMS/AccessoryStateQuery',
  'ACC MONITOR_BU1': appBasePath + '/EMS/AccessoryStateMonitorBu1',
  'ACCESSORY REPAIR': appBasePath + '/EMS/AccessoryRepair',
  'ACC VIOLATIVE STATE SETTING': appBasePath + '/EMS/AccessoryStateViolativeSetting',
  'LOT OPERATOR CLOCKIN': appBasePath + '/WIP/LotOperatorClockIn',
  'LOT OPERATOR CLOCKOUT': appBasePath + '/WIP/LotOperatorClockOut',
  'LOT SPLIT': appBasePath + '/WIP/LotSplit',
  'LOT SPLIT_COMP': appBasePath + '/WIP/LotSplitComponent',
  'LOT SPLIT_BIN': appBasePath + '/WIP/LotSplitBin',
  'LOT SPLIT_CARRIER': appBasePath + '/WIP/LotSplitCarrier',
  'LOT MERGE': appBasePath + '/WIP/LotMerge',
  'LOT MERGE_COMP': appBasePath + '/WIP/LotMergeComponent',
  'LOT MERGE_BIN': appBasePath + '/WIP/LotMergeBin',
  'LOT MERGE_CARRIER': appBasePath + '/WIP/LotMergeCarrier',
  'LOT COMP SPLIT': appBasePath + '/WIP/ComponentSplit',
  'LOT HOTLOT AND PRIPORITY CHANGE': appBasePath + '/WIP/HotLotPriority',
  'IMMEDIATE HOLD SET': appBasePath + '/WIP/ImmediateHoldSet',
  'IMMEDIATE WAIT SET': appBasePath + '/WIP/ImmediateWaitSet',
  'IMMEDIATE HOLD SET_BATCH': appBasePath + '/WIP/BatchImmediateHold',
  'FUTURE HOLD SET': appBasePath + '/WIP/FutureHoldSet',
  'FUTURE WAIT SET': appBasePath + '/WIP/FutureWaitSet',
  'FUTURE HOLD RELEASE': appBasePath + '/WIP/FutureHoldRelease',
  'FUTURE WAIT RELEASE': appBasePath + '/WIP/FutureWaitRelease',
  'ERF BASIS': appBasePath + '/WIP/ErfBasis',
  'WIP ERF UNRELEASED': appBasePath + '/WIP/ErfUnreleased',
  'SPC ERF QUERY': appBasePath + '/SPC/ErfBasis',
  'SPC ERF UNRELEASED': appBasePath + '/SPC/ErfUnreleased',
  'WAIT BASIS': appBasePath + '/WIP/WaitBasis',
  'WIP WAIT DISPOSITION': appBasePath + '/WIP/WaitDisposition',
  '8D BASIS_FACTORY': appBasePath + '/WIP/8DBasisFactory',
  '8D MAINTAIN': appBasePath + '/WIP/8DMaintain',
  '8D CLOSE': appBasePath + '/WIP/8DClose',
  '8D QUERY': appBasePath + '/WIP/8DQuery',
  'RUN CARD': appBasePath + '/WIP/RunCard',
  'LOT SHEET PRINT': appBasePath + '/WIP/LotSheetPrint',
  'WORKORDER PRINT': appBasePath + '/WIP/WorkorderPrint',
  'SKIP INSPECTION SETTING': appBasePath + '/WIP/SkipInspectionSetting',
  'Skip Inspection Setting': appBasePath + '/WIP/SkipInspectionSetting',
  'P_BUMP MAP MERGE': appBasePath + '/WIP/BumpMapMerge',
  WPP01: appBasePath + '/WIP/LotBasisCreateCp',
  WPP02: appBasePath + '/WIP/WaferMapExport',
  WPP03: appBasePath + '/WIP/EqpSetupBasisCp',
  WPP04: appBasePath + '/WIP/ProbingLog',
  WPP05: appBasePath + '/WIP/GoldenMapFileManage',
  WPP06: appBasePath + '/WIP/BumpMapFileManage',
  WPP08: appBasePath + '/WIP/LotComponentGen',
  'FGDSTOCKIN_E10ICD': appBasePath + '/WIP/FgdStockInE10Icd',
  'FGDSTOCKIN_T100': appBasePath + '/WIP/FgdStockInT100',
  'FGDSTOCKIN_TP': appBasePath + '/WIP/FgdStockInTp',
  'FGDSTOCKIN_T100_CANCEL': appBasePath + '/WIP/FgdStockInT100Cancel',
  'FGDSTOCKIN_E10ICD_CANCEL': appBasePath + '/WIP/FgdStockInE10IcdCancel',
  'SCRAPSTOCKIN_E10ICD': appBasePath + '/WIP/ScrapStockInE10Icd',
  'INV COUNT ADJUST': appBasePath + '/WIP/InvCountStateAdjust',
  'INV COUNT PRINT': appBasePath + '/WIP/InvCountStatePrint',
  'INV COUNT STATE': appBasePath + '/WIP/InvCountState',
  'EQP FAVORITE SETUP': appBasePath + '/WIP/EquipmentFavorites',
  'CARRIER STATE': appBasePath + '/WIP/CarrierState',
  'CARRIER MANAGEMENT COMP': appBasePath + '/WIP/CarrierManagement',
  'EQP QCLIST ONLINE CHECK': appBasePath + '/WIP/EquipmentOnlineQC',
  'EQP QCLIST LOG': appBasePath + '/WIP/EquipmentQcListLog',
  'PRODUCTION STATE BY AREA': appBasePath + '/WIP/AreaShow',
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
  'PRS BASIS': `${appBasePath}/PRS/ProcessBasis`,
  'PRS TYPE': `${appBasePath}/PRS/ProcessType`,
  'PS BASIS': `${appBasePath}/PRS/ProductionSegments`,
  'STAGE BASIS': `${appBasePath}/PRS/FabPrsStageBasis`,
  'PRD BASIS': `${appBasePath}/MasterData/Products`,
  'RO BASIS': appBasePath + '/OE/ReceivingOrders',
  'RO CLOSE': appBasePath + '/OE/ReceivingOrderClose',
  'MO BASIS': appBasePath + '/OE/ManufacturingOrders',
  'MO DELETE': appBasePath + '/OE/ManufacturingOrderDelete',
  'MO QUERY': appBasePath + '/OE/ManufacturingOrderQuery',
  'MO QUERY2': appBasePath + '/OE/ManufacturingOrderStartRate',
  'MO QUERY3': appBasePath + '/OE/ManufacturingOrderFinishRate',
  'MO MATERIAL LIST': appBasePath + '/OE/ManufacturingOrderMaterials',
  'MO MATERIAL STATUS': appBasePath + '/OE/ManufacturingOrderMaterialStock',
  'MO CLOSE': appBasePath + '/OE/ManufacturingOrderClose',
  'MO COMPDATA MAINTAIN': appBasePath + '/OE/ManufacturingOrderComponents',
  'B_MO CHOOSE WAFER': appBasePath + '/OE/ManufacturingOrderWaferQuery',
  'P_MO BASIS PKG': appBasePath + '/OE/ChipTestManufacturingOrders',
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
const restoringSession = ref(true)
const restoreRetryAvailable = ref(false)
const configurationError = ref('')
const authBlocked = computed(() => busy.value || restoringSession.value || !configLoaded.value)
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
const isPrsProcessBasisPage = computed(() => activeMasterKey.value === 'function:PRS:PRS BASIS')
const isPrsProcessTypePage = computed(() => activeMasterKey.value === 'function:PRS:PRS TYPE')
const isProductionSegmentPage = computed(() => activeMasterKey.value === 'function:PRS:PS BASIS')
const isProductTypePage = computed(() => activeMasterKey.value === 'function:PRD:PRD TYPE')
const isProductModulePage = computed(() => activeMasterKey.value === 'function:PRD:MODULE BASIS')
const isProductContentsSearchPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT SEARCH')
const isRecipeGroupPage = computed(() => activeMasterKey.value === 'function:PRD:PRD RECIPE BASIS')
const isProductTimeLimitPage = computed(() => activeMasterKey.value === 'function:PRD:LIMITED TIME CONTROL')
const isLabelDataPage = computed(() => activeMasterKey.value === 'function:PRD:LABEL DATA')
const isProductLabelMapPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT LABEL MAP BASIS')
const isProductOperationBatchPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT OP DEFAULT')
const isProductOperationAmendPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT OP AMEND')
const isProductOperationCopyPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT OP COPY')
const isPackingRuleBasisPage = computed(() => activeMasterKey.value === 'function:PRD:PRD PACKING RULE BASIS')
const isPackingRuleMapPage = computed(() => activeMasterKey.value === 'function:PRD:PRD PACKINGRULE MAP')
const isUserDefinedRunCardPage = computed(() => activeMasterKey.value === 'function:PRD:USER DEFINED RUNCARD')
const isProductRunCardMapPage = computed(() => activeMasterKey.value === 'function:PRD:PRODUCT RUN CARD MAP BASIS')
const isReceivingOrderPage = computed(() => activeMasterKey.value === 'function:OE:RO BASIS')
const isReceivingOrderClosePage = computed(() => activeMasterKey.value === 'function:OE:RO CLOSE')
const isEmsEquipmentStateChangePage = computed(() => activeMasterKey.value === 'function:EMS:EQP STATE CHANGE')
const isEmsEquipmentControlPage = computed(() => activeMasterKey.value === 'function:EMS:EMS CONTROL')
const isEmsEquipmentAccStateLogQueryPage = computed(() => activeMasterKey.value === 'function:EMS:EQP ACC STATE LOG QUERY')
const isEmsEquipmentAccStateQueryPage = computed(() => activeMasterKey.value === 'function:EMS:EQP ACC STATE QUERY')
const isEmsEquipmentCapabilityBasisPage = computed(() => activeMasterKey.value === 'function:EMS:EQP CAP BASIS')
const isEmsEquipmentCapabilityMappingPage = computed(() => activeMasterKey.value === 'function:EMS:EQP CAP MAPPING')
const isEmsEquipmentStateByAreaPage = computed(() => activeMasterKey.value === 'function:EMS:SHOW EQP STATUS BY AREA')
const isEmsEquipmentStateByTypePage = computed(() => activeMasterKey.value === 'function:EMS:SHOW EQP STATUS BY TYPE')
const isEmsAccessoryStateMonitorPage = computed(() => activeMasterKey.value === 'function:EMS:ACC MONITOR')
const isEmsAccessoryStateMonitorByEquipmentPage = computed(() => activeMasterKey.value === 'function:EMS:ACC MONITOR BY EQP')
const isEmsAccessoryStateControlSettingPage = computed(() => activeMasterKey.value === 'function:EMS:ACC STATE CONTROL SETTING')
const isEmsAccessoryStateLogQueryPage = computed(() => activeMasterKey.value === 'function:EMS:ACC STATE LOG QUERY')
const isEmsAccessoryStateQueryPage = computed(() => activeMasterKey.value === 'function:EMS:ACC STATE QUERY')
const isEmsAccessoryStateMonitorBu1Page = computed(() => activeMasterKey.value === 'function:EMS:ACC MONITOR_BU1')
const isEmsAccessoryRepairPage = computed(() => activeMasterKey.value === 'function:EMS:ACCESSORY REPAIR')
const isEmsAccessoryStateViolativeSettingPage = computed(() => activeMasterKey.value === 'function:EMS:ACC VIOLATIVE STATE SETTING')
const isEmsEquipmentStateViolativeSettingPage = computed(() => activeMasterKey.value === 'function:EMS:VIOLATIVE STATE SETTING')
const isEmsReturnEquipmentAccessoryPage = computed(() => activeMasterKey.value === 'function:EMS:RETURN EQP ACCESSORY')
const isEmsApplyEquipmentAccessoryPage = computed(() => activeMasterKey.value === 'function:EMS:APPLY EQP ACCESSORY')
const isEmsEquipmentStateLogPage = computed(() => activeMasterKey.value === 'function:EMS:EQUIPMENT STATE LOG')
const isEmsEquipmentStateLogQueryPage = computed(() => activeMasterKey.value === 'function:EMS:SHOW EQP LOG')
const isEmsEquipmentStateLogByStatePage = computed(() => activeMasterKey.value === 'function:EMS:SHOW EQP LOG BY STATE')
const isEmsEquipmentStateControlSettingPage = computed(() => activeMasterKey.value === 'function:EMS:EQP STATE CONTROL SETTING')
const isEmsEquipmentStateStatisticsPage = computed(() => activeMasterKey.value === 'function:EMS:EQP STATE STATISTICS_EMS')
const isEmsEquipmentReasonBasisPage = computed(() => activeMasterKey.value === 'function:EMS:EQUIPMENT REASON BASIS')
const isWipLotBasisCreatePage = computed(() => activeMasterKey.value === 'function:WIP:CREATE LOT')
const isWipLotBasisBatchPage = computed(() => activeMasterKey.value === 'function:WIP:CREATE LOT_BATCH')
const isWipLotExecutionPage = computed(() => activeMasterKey.value === 'function:WIP:EXECUTE LOT')
const isWipLotExecutionByComponentPage = computed(() => activeMasterKey.value === 'function:WIP:EXECUTE LOT_COMP')
const isWipLotChoiceEquipmentPage = computed(() => activeMasterKey.value === 'function:WIP:LOT CHOICE EQP')
const isWipIpqcPage = computed(() => activeMasterKey.value === 'function:WIP:IPQC')
const isWipLotSplitPage = computed(() => activeMasterKey.value === 'function:WIP:LOT SPLIT')
const isWipLotSplitComponentPage = computed(() => activeMasterKey.value === 'function:WIP:LOT SPLIT_COMP')
const isWipLotSplitBinPage = computed(() => activeMasterKey.value === 'function:WIP:LOT SPLIT_BIN')
const isWipLotSplitCarrierPage = computed(() => activeMasterKey.value === 'function:WIP:LOT SPLIT_CARRIER')
const isWipLotMergePage = computed(() => activeMasterKey.value === 'function:WIP:LOT MERGE')
const isWipLotMergeComponentPage = computed(() => activeMasterKey.value === 'function:WIP:LOT MERGE_COMP')
const isWipLotMergeBinPage = computed(() => activeMasterKey.value === 'function:WIP:LOT MERGE_BIN')
const isWipLotMergeCarrierPage = computed(() => activeMasterKey.value === 'function:WIP:LOT MERGE_CARRIER')
const isWipComponentSplitPage = computed(() => activeMasterKey.value === 'function:WIP:LOT COMP SPLIT')
const isWipHotLotPriorityPage = computed(() => activeMasterKey.value === 'function:WIP:LOT HOTLOT AND PRIPORITY CHANGE')
const isWipImmediateHoldSetPage = computed(() => activeMasterKey.value === 'function:WIP:IMMEDIATE HOLD SET')
const isWipImmediateWaitSetPage = computed(() => activeMasterKey.value === 'function:WIP:IMMEDIATE WAIT SET')
const isWipBatchImmediateHoldPage = computed(() => activeMasterKey.value === 'function:WIP:IMMEDIATE HOLD SET_BATCH')
const isWipFutureHoldSetPage = computed(() => activeMasterKey.value === 'function:WIP:FUTURE HOLD SET')
const isWipFutureWaitSetPage = computed(() => activeMasterKey.value === 'function:WIP:FUTURE WAIT SET')
const isWipFutureHoldReleasePage = computed(() => activeMasterKey.value === 'function:WIP:FUTURE HOLD RELEASE')
const isWipFutureWaitReleasePage = computed(() => activeMasterKey.value === 'function:WIP:FUTURE WAIT RELEASE')
const isWipErfBasisPage = computed(() => activeMasterKey.value === 'function:WIP:ERF BASIS')
const isSpcErfBasisPage = computed(() => activeMasterKey.value === 'function:SPC:SPC ERF QUERY')
const isSpcInspMachineParameterSetupPage = computed(() => activeMasterKey.value === 'function:SPC:INSP MACHINE PARAMETER SETUP')
const isWipErfUnreleasedPage = computed(() => activeMasterKey.value === 'function:WIP:WIP ERF UNRELEASED')
const isSpcErfUnreleasedPage = computed(() => activeMasterKey.value === 'function:SPC:SPC ERF UNRELEASED')
const isWipWaitBasisPage = computed(() => activeMasterKey.value === 'function:WIP:WAIT BASIS')
const isWipWaitDispositionPage = computed(() => activeMasterKey.value === 'function:WIP:WIP WAIT DISPOSITION')
const isWip8DBasisFactoryPage = computed(() => activeMasterKey.value === 'function:WIP:8D BASIS_FACTORY')
const isWip8DMaintainPage = computed(() => activeMasterKey.value === 'function:WIP:8D MAINTAIN')
const isWip8DClosePage = computed(() => activeMasterKey.value === 'function:WIP:8D CLOSE')
const isWip8DQueryPage = computed(() => activeMasterKey.value === 'function:WIP:8D QUERY')
const isWipEquipmentLotStatePage = computed(() => activeMasterKey.value === 'function:WIP:EXECUTE LOT_EQP')
const isWipEquipmentMaterialLoadingPage = computed(() => activeMasterKey.value === 'function:WIP:EQP MOUNT MATERIAL')
const isWipEquipmentConsumeStatusPage = computed(() => activeMasterKey.value === 'function:WIP:EQP CONSUME STATUS')
const isWipEquipmentConsumeStatusLogPage = computed(() => activeMasterKey.value === 'function:WIP:EQP CONSUME STATUS LOG')
const isWipEquipmentOnlineQcPage = computed(() => activeMasterKey.value === 'function:WIP:EQP QCLIST ONLINE CHECK')
const isWipEquipmentQcListLogPage = computed(() => activeMasterKey.value === 'function:WIP:EQP QCLIST LOG')
const isWipAreaShowPage = computed(() => activeMasterKey.value === 'function:WIP:PRODUCTION STATE BY AREA')
const isWipEquipmentFavoritePage = computed(() => activeMasterKey.value === 'function:WIP:EQP FAVORITE SETUP')
const isWipRunCardPage = computed(() => activeMasterKey.value === 'function:WIP:RUN CARD')
const isWipLotSheetPrintPage = computed(() => activeMasterKey.value === 'function:WIP:LOT SHEET PRINT')
const isWipWorkorderPrintPage = computed(() => activeMasterKey.value === 'function:WIP:WORKORDER PRINT')
const isWipSkipInspectionSettingPage = computed(() => activeMasterKey.value === 'function:WIP:SKIP INSPECTION SETTING')
const isWipBumpMapMergePage = computed(() => activeMasterKey.value === 'function:WIP:P_BUMP MAP MERGE')
const isWipBumpMapFileManagePage = computed(() => activeMasterKey.value === 'function:WIP:WPP06')
const isWipGoldenMapFileManagePage = computed(() => activeMasterKey.value === 'function:WIP:WPP05')
const isWipWaferMapExportPage = computed(() => activeMasterKey.value === 'function:WIP:WPP02')
const isWipProbingLogPage = computed(() => activeMasterKey.value === 'function:WIP:WPP04')
const isWipLotComponentGenPage = computed(() => activeMasterKey.value === 'function:WIP:WPP08')
const isWipLotBasisCreateCpPage = computed(() => activeMasterKey.value === 'function:WIP:WPP01')
const isWipEqpSetupBasisCpPage = computed(() => activeMasterKey.value === 'function:WIP:WPP03')
const isWipFgdStockInE10IcdPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_E10ICD')
const isWipFgdStockInT100Page = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_T100')
const isWipFgdStockInTpPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_TP')
const isWipFgdStockInTpCancelPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_TP_CANCEL')
const isWipFgdStockInWorkFlowPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_WF')
const isWipFgdStockInWorkFlowCancelPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_WF_CANCEL')
const isWipFgdStockInT100CancelPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_T100_CANCEL')
const isWipFgdStockInE10IcdCancelPage = computed(() => activeMasterKey.value === 'function:WIP:FGDSTOCKIN_E10ICD_CANCEL')
const isWipScrapStockInE10IcdPage = computed(() => activeMasterKey.value === 'function:WIP:SCRAPSTOCKIN_E10ICD')
const isWipInvCountStateAdjustPage = computed(() => activeMasterKey.value === 'function:WIP:INV COUNT ADJUST')
const isWipInvCountStatePrintPage = computed(() => activeMasterKey.value === 'function:WIP:INV COUNT PRINT')
const isWipInvCountStatePage = computed(() => activeMasterKey.value === 'function:WIP:INV COUNT STATE')
const isWipBartenderPrintPage = computed(() => activeMasterKey.value === 'function:WIP:BARTENDER')
const isWipUnInventoryPage = computed(() => activeMasterKey.value === 'function:WIP:WIPUNINV')
const isWipOperatorLoginPage = computed(() => activeMasterKey.value === 'function:WIP:OPERATOR LOGIN')
const isWipOperatorLoginLotPage = computed(() => activeMasterKey.value === 'function:WIP:OPERATOR LOGIN_LOT')
const isWipOperatorClockInPage = computed(() => activeMasterKey.value === 'function:WIP:OPERATOR CLOCKIN')
const isWipOperatorClockOutPage = computed(() => activeMasterKey.value === 'function:WIP:OPERATOR CLOCKOUT')
const isWipOperatorOnlinePage = computed(() => activeMasterKey.value === 'function:WIP:OPERATOR ONLINE')
const isWipSwrBasisPage = computed(() => activeMasterKey.value === 'function:WIP:SWR BASIS')
const isWipUndoDispatchPage = computed(() => activeMasterKey.value === 'function:WIP:UNDO DISPATCH')
const isWipOutsourceCreatePage = computed(() => activeMasterKey.value === 'function:WIP:OS CREATE')
const isWipOutsourceConfirmPage = computed(() => activeMasterKey.value === 'function:WIP:OS CONFIRM')
const isWipOutsourceReturnPage = computed(() => activeMasterKey.value === 'function:WIP:OS RETURN')
const isWipOutsourceCancelPage = computed(() => activeMasterKey.value === 'function:WIP:OS CONFIRM_ERP_CANCEL')
const isWipLotReversePage = computed(() => activeMasterKey.value === 'function:WIP:REVERSE LOT')
const isWipReleasedLotDeletePage = computed(() => activeMasterKey.value === 'function:WIP:DELETE RELEASED LOT')
const isWipReleasedLotBatchDeletePage = computed(() => activeMasterKey.value === 'function:WIP:DELETE BATCH LOT')
const isWipLotAreaChangePage = computed(() => activeMasterKey.value === 'function:WIP:LOT OP AREA CHANGE')
const isWipProductVersionChangePage = computed(() => activeMasterKey.value === 'function:WIP:PRODUCT VERSION CHANGE')
const isWipEcnWizardPage = computed(() => activeMasterKey.value === 'function:WIP:ECN WIZARD')
const isWp916Page = computed(() => activeMasterKey.value === 'function:WIP:CLOSE LOT FORCIBLY')
const isWp915Page = computed(() => activeMasterKey.value === 'function:WIP:CHANGELOTPRODUCT')
const isWp913Page = computed(() => activeMasterKey.value === 'function:WIP:LOT PROPERTY ADJUST')
const isWp912Page = computed(() => activeMasterKey.value === 'function:WIP:LOT OP CONT ATTRIB ADJUST')
const isWp911Page = computed(() => activeMasterKey.value === 'function:WIP:LOT OP CONT MATERIAL ADJUST')
const isWp909Page = computed(() => activeMasterKey.value === 'function:WIP:TRANSFER FACTORY')
const isWp908Page = computed(() => activeMasterKey.value === 'function:WIP:LABEL PRINT')
const isWp907Page = computed(() => activeMasterKey.value === 'function:WIP:LOT QTY ADJUST')
const isWipLotOperatorClockInPage = computed(() => activeMasterKey.value === 'function:WIP:LOT OPERATOR CLOCKIN')
const isWipLotOperatorClockOutPage = computed(() => activeMasterKey.value === 'function:WIP:LOT OPERATOR CLOCKOUT')
const isWipCarrierStatePage = computed(() => activeMasterKey.value === 'function:WIP:CARRIER STATE')
const isWipCarrierManagementPage = computed(() => activeMasterKey.value === 'function:WIP:CARRIER MANAGEMENT COMP')
const isWipFgdInventoryPage = computed(() => activeMasterKey.value === 'function:WIP:FGD INV')
const isWipPackingMOLinePage = computed(() => activeMasterKey.value === 'function:WIP:PACKING_MOLINE')
const isWipPackingMOLineInvPage = computed(() => activeMasterKey.value === 'function:WIP:PACKING_MOLINE_INV')
const isWipInventoryPage = computed(() => activeMasterKey.value === 'function:WIP:WIP INV')
const isWipSemiInventoryPage = computed(() => activeMasterKey.value === 'function:WIP:SEMI INV')
const manufacturingOrderPages = {
  'function:OE:MO BASIS': undefined,
  'function:OE:MO DELETE': 'delete',
  'function:OE:MO QUERY': 'query',
  'function:OE:MO QUERY2': 'start-report',
  'function:OE:MO QUERY3': 'finish-report',
  'function:OE:MO MATERIAL LIST': 'materials',
  'function:OE:MO MATERIAL STATUS': 'material-stock',
  'function:OE:MO CLOSE': 'close',
  'function:OE:MO COMPDATA MAINTAIN': 'components',
  'function:OE:B_MO CHOOSE WAFER': 'wafer-query',
  'function:OE:P_MO BASIS PKG': 'chip-test',
} as const
const manufacturingOrderPage = computed(() => {
  const key = activeMasterKey.value
  return key && Object.hasOwn(manufacturingOrderPages, key) ? key as keyof typeof manufacturingOrderPages : null
})
const isProductQCSpecImportPage = computed(() => activeMasterKey.value === 'function:PRD:PRD QCITEM SPEC IMPORT')
const isProductGbomBatchImportPage = computed(() => activeMasterKey.value === 'function:PRD:PRD GBOM IMPORT')
const isFtProgramPage = computed(() => activeMasterKey.value === 'function:PRD:FT PROGRAM BASIS')
const isEqcSampleRulePage = computed(() => activeMasterKey.value === 'function:PRD:EQC SAMPLE RULE')
const isProductQcFrequencyPage = computed(() => activeMasterKey.value === 'function:PRD:PRD QCRULE BASIS')
const isProductWorkTimePage = computed(() => activeMasterKey.value === 'function:PRD:PRD WORKTIME')
const isProductOverstayPage = computed(() => activeMasterKey.value === 'function:PRD:PRD OVERSTAY')
const isProductStationRulePage = computed(() => activeMasterKey.value === 'function:PRD:PRD OP RULE')
const isCpWaferShipReportPage = computed(() => activeMasterKey.value === 'function:PRD:P_CP WAFER YIELD SHIP REPORT')
const isCpProgramPage = computed(() => activeMasterKey.value === 'function:PRD:P_CP PROGRAM DEFINE')
const isCpWaferRulePage = computed(() => activeMasterKey.value === 'function:PRD:P_WAFER RULE MANAGER')
const isCpMergeMapRulePage = computed(() => activeMasterKey.value === 'function:PRD:P_PRD WAFERMAP MERGE RULE')
const isChooseWaferRulePage = computed(() => activeMasterKey.value === 'function:PRD:B_CHOOSE WAFER RULES')
const isPackagingProductPage = computed(() => activeMasterKey.value === 'function:PRD:B_PRD BASIS_IC')
const isUploadFileTypePage = computed(() => activeMasterKey.value === 'function:PRD:B_UPLOAD FILE BASIS')
const isTestProgramFilePage = computed(() => activeMasterKey.value === 'function:PRD:B_TEST PROGRAM FILE MANAGEMENT')
const isFabPrsStageBasisPage = computed(() => activeMasterKey.value === 'function:PRS:STAGE BASIS')
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
const isSpcOnlineControlPage = computed(() => activeMasterKey.value === 'function:SPC:SPC ONLIE CONTROL')
const isSpcOnlineControlContinuePage = computed(() => activeMasterKey.value === 'function:SPC:SPC ONLIE CONTROL CONTINUE')
const isIncomingQCFormPage = computed(() => activeMasterKey.value === 'function:SPC:INCOMING QC FORM')
const isIncomingQCFormComponentPage = computed(() => activeMasterKey.value === 'function:SPC:IQC FORM FOR COMPONENT')
const isFinalQCFormPage = computed(() => activeMasterKey.value === 'function:SPC:FQC FORM')
const isInProcessQCFormPage = computed(() => activeMasterKey.value === 'function:SPC:IPQC FORM')
const isOutgoingQCFormPage = computed(() => activeMasterKey.value === 'function:SPC:OQC FORM')
const isSpcVariableMeasurementImportPage = computed(() => activeMasterKey.value === 'function:SPC:IMPORT VAR MEASURE')
const isSpcDataMaintenancePage = computed(() => activeMasterKey.value === 'function:SPC:SPC DATA')
const isSpcLotDataByLotNoPage = computed(() => activeMasterKey.value === 'function:SPC:SPC DATA BY LOTNO')
const isSpcDataEditLogPage = computed(() => activeMasterKey.value === 'function:SPC:DATA EDIT LOG')
const isSpcChartLineEditLogPage = computed(() => activeMasterKey.value === 'function:SPC:CHART LINE EDIT LOG')
const isSpcQcItemBindingHisLogPage = computed(() => activeMasterKey.value === 'function:SPC:QCITEM BINDING HIS LOG')
const isSpcChartAllCapabilityPage = computed(() => activeMasterKey.value === 'function:SPC:CHART ALL CAPABILITY')
const isSpcChartAllCapabilityByFactorPage = computed(() => activeMasterKey.value === 'function:SPC:CHART ALL CAPABILITY BY FACTOR')
const isSpcMonthRPTCodeBasisPage = computed(() => activeMasterKey.value === 'function:SPC:MONTHLY CHART REPORT CODE')
const isSpcMonthChartReportPage = computed(() => activeMasterKey.value === 'function:SPC:MONTHLY CHART REPORT')
const isSpcMonthChartReportByFactorPage = computed(() => activeMasterKey.value === 'function:SPC:MONTHLY CHART REPORT BY FACTOR')
const isSpcXlsSourceFileControlPage = computed(() => activeMasterKey.value === 'function:SPC:XLS SOURCE FILE CONTROL')
const isSpcChartSignboardPage = computed(() => activeMasterKey.value === 'function:SPC:CHART SIGNBOARD')
const isSpcControlChartAnalysisPage = computed(() => activeMasterKey.value === 'function:SPC:CONTROL CHART ANALYSIS')
const isSpcControlChartByFactorPage = computed(() => activeMasterKey.value === 'function:SPC:CONTROL CHART ANALYSIS BY FACTOR')
const isSpcLineChartAnalysisPage = computed(() => activeMasterKey.value === 'function:SPC:LINE CHART ANALYSIS')
const isSpcScatterChartAnalysisPage = computed(() => activeMasterKey.value === 'function:SPC:SCATTER DIAGRAM')
const isSpcPChartSignboardPage = computed(() => activeMasterKey.value === 'function:SPC:PCHART SIGNBOARD')
const isSpcParetoAnalysisPage = computed(() => activeMasterKey.value === 'function:SPC:PARETO DIAGRAM')
const isSpcLrrPpmChartPage = computed(() => activeMasterKey.value === 'function:SPC:LRR / PPM CHART')
const isSpcAttributeControlChartPage = computed(() => activeMasterKey.value === 'function:SPC:ATT-CHART')
const isSpcCoaReportPage = computed(() => activeMasterKey.value === 'function:SPC:SPC COA')
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

function addDevelopmentEmsFunctions(items: MenuNode[]): MenuNode[] {
  const cloneTree = (nodes: MenuNode[]): MenuNode[] => nodes.map((item) => ({ ...item, children: item.children ? cloneTree(item.children) : undefined }))
  const result = cloneTree(items)
  const fallbackEms = fallbackMenuTree.find((item) => item.moduleNo?.toUpperCase() === 'EMS')
  if (!fallbackEms) return result
  const ems = result.find((item) => item.moduleNo?.toUpperCase() === 'EMS')
  if (!ems) { result.push(cloneTree([fallbackEms])[0]); return result }
  const hasFunction = (nodes: MenuNode[] | undefined, functionNo: string): boolean => Boolean(nodes?.some((node) =>
    node.functionNo?.trim().toUpperCase() === functionNo || hasFunction(node.children, functionNo)))
  for (const fallbackGroup of fallbackEms.children ?? []) {
    let group = ems.children?.find((item) => item.label.trim() === fallbackGroup.label.trim())
    if (!group) {
      group = { ...fallbackGroup, children: [] }
      ems.children = [...(ems.children ?? []), group]
    }
    for (const feature of fallbackGroup.children ?? []) {
      if (feature.functionNo && !hasFunction(ems.children, feature.functionNo.trim().toUpperCase())) {
        group.children = [...(group.children ?? []), cloneTree([feature])[0]]
      }
    }
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
  if (appDisposed || screen.value !== 'signed-in' || !currentUser.value) return
  invalidateMenuRequests()
  const context = authContext()
  const requestEpoch = menuRequestEpoch
  const current = () => isCurrentAuth(context) && requestEpoch === menuRequestEpoch && screen.value === 'signed-in'
  menuSource.value = 'loading'
  menuError.value = ''
  try {
    const url = apiUrl('/api/navigation/menu-tree')
    if (!url) throw new Error('尚未設定主選單 API 位址。')
    const tree = await boundedRequest(url, { credentials: 'include', cache: 'no-store' }, async (response) => {
      if (!response.ok) throw new Error(`主選單 API 回應錯誤 (${response.status})。`)
      return mapMenuTree(await response.json() as MenuTreeApiResponse)
    }, '主選單載入', menuControllers, 20000)
    if (!current()) return
    menuTree.value = import.meta.env.DEV ? addDevelopmentEmsFunctions(addDevelopmentOperationIndustryFunctions(addDevelopmentInspectionMachineFunctions(addDevelopmentEquipmentFunctions(tree)))) : tree
    menuSource.value = 'api'
  } catch (error) {
    if (!current()) return
    menuError.value = error instanceof Error ? error.message : '主選單載入失敗，請重試。'
    menuTree.value = import.meta.env.DEV ? fallbackMenuTree : []
    menuSource.value = import.meta.env.DEV ? 'fallback' : 'error'
  }
  if (current()) syncRouteState()
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
  pendingErfInfoNo.value = ''
  pending8dErf.value = null
  pending8dMaintainNo.value = ''
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  selectedMenuKey.value = id
  const item = findMenuNode(menuTree.value, id)
  if (!item || item.children?.length) return
  const route = routeForMenuNode(item)
  window.history.pushState({}, '', route)
  syncRouteState()
}

function navigateHome(): void {
  pendingErfInfoNo.value = ''
  pending8dErf.value = null
  pending8dMaintainNo.value = ''
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  window.history.pushState({}, '', appHomeRoute)
  syncRouteState()
}

const pendingErfInfoNo = ref('')
const pending8dErf = ref<{ erfNo: string; lotNo: string; productNo: string; opNo: string; customerNo: string; lotQty: number; errorQty: number; holdDescription: string } | null>(null)
const pending8dMaintainNo = ref('')
const pending8dMaintainReadOnly = ref(false)
const pending8dQueryReturnPath = ref('')
function openErfInfoFrom8d(erfNo: string): void {
  if (!isDevelopmentPreview.value) return
  pendingErfInfoNo.value = erfNo
  pending8dErf.value = null
  pending8dMaintainNo.value = ''
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  window.history.pushState({}, '', appBasePath + '/WIP/ErfBasis')
  syncRouteState()
}
function open8dFromErf(erf: NonNullable<typeof pending8dErf.value>): void {
  if (!isDevelopmentPreview.value) return
  pending8dErf.value = erf
  pendingErfInfoNo.value = ''
  pending8dMaintainNo.value = ''
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  window.history.pushState({}, '', appBasePath + '/WIP/8DBasisFactory')
  syncRouteState()
}
function open8dMaintainFromErf(qrsNo: string): void {
  if (!isDevelopmentPreview.value) return
  pending8dMaintainNo.value = qrsNo
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  pending8dErf.value = null
  pendingErfInfoNo.value = ''
  window.history.pushState({}, '', appBasePath + '/WIP/8DMaintain')
  syncRouteState()
}
function open8dQueryFromClose(qrsNo: string): void {
  if (!isDevelopmentPreview.value) return
  pending8dMaintainNo.value = qrsNo
  pending8dMaintainReadOnly.value = true
  pending8dQueryReturnPath.value = appBasePath + '/WIP/8DClose'
  window.history.pushState({}, '', appBasePath + '/WIP/8DMaintain')
  syncRouteState()
}
function open8dQueryFromQuery(qrsNo: string): void {
  if (!isDevelopmentPreview.value) return
  pending8dMaintainNo.value = qrsNo
  pending8dMaintainReadOnly.value = true
  pending8dQueryReturnPath.value = appBasePath + '/WIP/8DQuery'
  window.history.pushState({}, '', appBasePath + '/WIP/8DMaintain')
  syncRouteState()
}
function close8dMaintain(): void {
  if (!isDevelopmentPreview.value || !pending8dMaintainReadOnly.value) { navigateHome(); return }
  const returnPath = pending8dQueryReturnPath.value || appBasePath + '/WIP/8DClose'
  pending8dMaintainNo.value = ''
  pending8dMaintainReadOnly.value = false
  pending8dQueryReturnPath.value = ''
  window.history.pushState({}, '', returnPath)
  syncRouteState()
}

const selectedEnvironmentConfig = computed(
  () => runtimeConfig.value[selectedEnvironment.value],
)

const selectedEnvironmentLabel = computed(
  () => environmentLabels[selectedEnvironment.value],
)

const passwordLength = computed(() => Array.from(nextPassword.value).length)

type AuthContext = { epoch: number; environment: EnvironmentKey; apiBaseUrl: string }
let sessionEpoch = 0
let menuRequestEpoch = 0
let appDisposed = false
let applyingConfiguration = false
const authControllers = new Set<AbortController>()
const menuControllers = new Set<AbortController>()

function invalidateMenuRequests(): void {
  menuRequestEpoch += 1
  for (const controller of menuControllers) controller.abort()
  menuControllers.clear()
}

function invalidateAuthRequests(): void {
  sessionEpoch += 1
  for (const controller of authControllers) controller.abort()
  authControllers.clear()
  invalidateMenuRequests()
}

function authContext(): AuthContext {
  return { epoch: sessionEpoch, environment: selectedEnvironment.value, apiBaseUrl: selectedEnvironmentConfig.value.apiBaseUrl }
}

function isCurrentAuth(context: AuthContext): boolean {
  return !appDisposed && context.epoch === sessionEpoch && context.environment === selectedEnvironment.value && context.apiBaseUrl === selectedEnvironmentConfig.value.apiBaseUrl
}

async function boundedRequest<T>(url: string, init: RequestInit, consume: (response: Response) => Promise<T>, role: string,
  controllers = authControllers, timeoutMs = 15000): Promise<T> {
  const controller = new AbortController()
  controllers.add(controller)
  let timedOut = false
  const timer = window.setTimeout(() => { timedOut = true; controller.abort() }, timeoutMs)
  try {
    return await consume(await fetch(url, { ...init, signal: controller.signal }))
  } catch (error) {
    if (timedOut) throw new Error(`${role}逾時，請重試。`)
    throw error
  } finally {
    window.clearTimeout(timer)
    controllers.delete(controller)
  }
}

function resetLocalSession(): void {
  currentUser.value = null
  username.value = ''
  password.value = ''
  nextPassword.value = ''
  confirmPassword.value = ''
  selectedMenuKey.value = null
  expandedMenuKeys.value = []
  menuTree.value = import.meta.env.DEV ? fallbackMenuTree : []
  menuSource.value = import.meta.env.DEV ? 'fallback' : 'error'
  menuError.value = ''
  screen.value = 'login'
}

watch([selectedEnvironment, () => selectedEnvironmentConfig.value.apiBaseUrl], () => {
  if (appDisposed || !configLoaded.value || applyingConfiguration) return
  invalidateAuthRequests()
  resetLocalSession()
  busy.value = false
  restoringSession.value = false
  errorMessage.value = ''
  successMessage.value = ''
  restoreRetryAvailable.value = true
}, { flush: 'sync' })

async function restoreSession(reloadConfiguration = false): Promise<void> {
  invalidateAuthRequests()
  const epoch = sessionEpoch
  restoringSession.value = true
  restoreRetryAvailable.value = false
  errorMessage.value = ''
  successMessage.value = ''
  try {
    if (!configLoaded.value || reloadConfiguration) {
      try {
        const config = await boundedRequest(`${import.meta.env.BASE_URL}environments.json`, { cache: 'no-store' }, async (response) => {
          if (!response.ok) throw new Error(`環境設定載入失敗 (${response.status})。`)
          return await response.json() as Partial<RuntimeConfig>
        }, '環境設定載入')
        if (appDisposed || epoch !== sessionEpoch) return
        applyingConfiguration = true
        try {
          for (const key of environmentKeys) {
            const entry = config[key]
            if (entry && typeof entry.apiBaseUrl === 'string') runtimeConfig.value[key] = { label: environmentLabels[key], apiBaseUrl: entry.apiBaseUrl.trim() }
          }
          configurationError.value = ''
        } finally { applyingConfiguration = false }
      } catch (error) {
        if (appDisposed || epoch !== sessionEpoch) return
        configurationError.value = error instanceof Error ? error.message : '環境設定載入失敗，可重試。'
        // Keep the original safe defaults; never substitute a remote credential target.
      }
      if (appDisposed || epoch !== sessionEpoch) return
      applyingConfiguration = true
      try { if (localDemoApi) runtimeConfig.value.development.apiBaseUrl = '/YMES/API' }
      finally { applyingConfiguration = false }
      configLoaded.value = true
    }
    const context = authContext()
    const url = apiUrl('/auth/me')
    if (!url) throw new Error(selectedEnvironmentLabel.value + '尚未設定登入服務位址。')
    const user = await boundedRequest(url, { credentials: 'include', cache: 'no-store' }, async (response) => {
      if (response.status === 401) return null
      if (!response.ok) throw new Error(`登入狀態確認失敗 (${response.status})，可重試。`)
      return await response.json() as AuthResponse
    }, '登入狀態確認')
    if (!isCurrentAuth(context)) return
    if (user && !user.mustChangePassword) {
      currentUser.value = user
      screen.value = 'signed-in'
      await loadMenuTree()
    }
  } catch (error) {
    if (appDisposed || epoch !== sessionEpoch) return
    errorMessage.value = error instanceof Error ? error.message : '無法確認登入狀態，請重試。'
    restoreRetryAvailable.value = true
  } finally {
    if (!appDisposed && epoch === sessionEpoch) restoringSession.value = false
  }
}

async function retrySessionRestore(): Promise<void> {
  if (authBlocked.value || appDisposed || screen.value !== 'login') return
  await restoreSession(true)
}

onMounted(() => {
  syncRouteState()
  window.addEventListener('popstate', syncRouteState)
  void restoreSession()
})

onBeforeUnmount(() => {
  appDisposed = true
  invalidateAuthRequests()
  window.removeEventListener('popstate', syncRouteState)
})

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
  if (authBlocked.value || appDisposed || screen.value !== 'login') return
  errorMessage.value = ''
  successMessage.value = ''
  if (!username.value.trim() || !password.value) {
    errorMessage.value = '請輸入帳號和密碼。'
    return
  }
  let url: string | null
  try { url = apiUrl('/auth/login') }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : '登入環境設定錯誤。'; return }
  if (!url) { errorMessage.value = selectedEnvironmentLabel.value + '尚未設定登入服務位址；帳號密碼未送出。'; return }
  invalidateAuthRequests()
  const context = authContext()
  const credentials = { username: username.value.trim(), password: password.value }
  busy.value = true
  restoreRetryAvailable.value = false
  try {
    const result = await boundedRequest(url, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(credentials) }, async (response) => {
      if (!response.ok) throw new Error(await readMessage(response))
      return await response.json() as AuthResponse
    }, '登入驗證')
    if (!isCurrentAuth(context)) return
    currentUser.value = result
    if (result.mustChangePassword) {
      screen.value = 'change-password'
      successMessage.value = '首次登入請先設定新密碼。'
    } else {
      screen.value = 'signed-in'
      password.value = ''
      await loadMenuTree()
    }
  } catch (error) {
    if (isCurrentAuth(context)) errorMessage.value = error instanceof Error ? error.message : selectedEnvironmentLabel.value + '登入服務目前無法連線。'
  } finally {
    if (isCurrentAuth(context)) busy.value = false
  }
}

async function submitDemoLogin(): Promise<void> {
  if (authBlocked.value || appDisposed || screen.value !== 'login') return
  selectedEnvironment.value = localDemoApi ? 'development' : 'production'
  username.value = 'ymes'
  password.value = '1234'
  await submitLogin()
}

async function submitPasswordChange(): Promise<void> {
  if (authBlocked.value || appDisposed || screen.value !== 'change-password') return
  errorMessage.value = ''
  successMessage.value = ''
  if (passwordLength.value < 4) { errorMessage.value = '新密碼至少需要 4 個字元。'; return }
  if (passwordLength.value > 128) { errorMessage.value = '新密碼最多可輸入 128 個字元。'; return }
  if (nextPassword.value !== confirmPassword.value) { errorMessage.value = '兩次輸入的新密碼不一致。'; return }
  let url: string | null
  try { url = apiUrl('/auth/change-password') }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : '登入環境設定錯誤。'; return }
  if (!url) { errorMessage.value = selectedEnvironmentLabel.value + '尚未設定登入服務位址。'; return }
  invalidateAuthRequests()
  const context = authContext()
  const body = JSON.stringify({ currentPassword: password.value, newPassword: nextPassword.value })
  busy.value = true
  try {
    await boundedRequest(url, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body }, async (response) => {
      if (!response.ok) throw new Error(await readMessage(response))
    }, '密碼更新')
    if (!isCurrentAuth(context)) return
    password.value = ''
    nextPassword.value = ''
    confirmPassword.value = ''
    if (currentUser.value) currentUser.value.mustChangePassword = false
    screen.value = 'signed-in'
    successMessage.value = '密碼已更新。'
    await loadMenuTree()
  } catch (error) {
    if (isCurrentAuth(context)) errorMessage.value = error instanceof Error ? error.message : selectedEnvironmentLabel.value + '登入服務目前無法連線。'
  } finally {
    if (isCurrentAuth(context)) busy.value = false
  }
}

async function signOut(): Promise<void> {
  if (busy.value || restoringSession.value || appDisposed || screen.value !== 'signed-in') return
  let url: string | null
  try { url = apiUrl('/auth/logout') }
  catch (error) { errorMessage.value = error instanceof Error ? error.message : '登出環境設定錯誤。'; return }
  if (!url) { errorMessage.value = '尚未設定登出服務位址，登出未確認。'; return }
  invalidateAuthRequests()
  const context = authContext()
  busy.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    await boundedRequest(url, { method: 'POST', credentials: 'include', cache: 'no-store' }, async (response) => {
      if (!response.ok) throw new Error(`登出未確認 (${response.status})，請再按登出。`)
    }, '登出')
    if (!isCurrentAuth(context)) return
    resetLocalSession()
    restoreRetryAvailable.value = false
  } catch (error) {
    if (isCurrentAuth(context)) {
      errorMessage.value = error instanceof Error ? error.message : '登出未確認，請再按登出。'
      if (menuSource.value === 'loading') {
        menuSource.value = 'error'
        menuError.value = '選單載入已取消，請重試。'
      }
    }
  } finally {
    if (isCurrentAuth(context)) busy.value = false
  }
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

          <form v-if="screen === 'login'" class="login-form" :aria-busy="authBlocked" @submit.prevent="submitLogin">
            <fieldset class="environment-fieldset" :disabled="authBlocked">
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

            <p v-if="restoringSession" class="form-message" role="status">正在確認登入狀態…</p>
            <p v-if="configurationError" class="form-message error-message" role="alert">{{ configurationError }}</p>
            <p v-if="errorMessage" class="form-message error-message" role="alert">
              {{ errorMessage }}
            </p>
            <p v-if="successMessage" class="form-message success-message" role="status">
              {{ successMessage }}
            </p>

            <button class="submit-button" type="submit" :disabled="authBlocked">
              {{ restoringSession ? '正在確認登入狀態…' : busy ? '正在驗證…' : '登入' }}
            </button>
            <button class="demo-button" type="button" :disabled="authBlocked" @click="submitDemoLogin">
              Demo 登入
            </button>
            <button v-if="restoreRetryAvailable || configurationError" class="demo-button" type="button" :disabled="authBlocked" @click="retrySessionRestore">重新確認登入狀態</button>
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
            <button class="submit-button" type="submit" :disabled="authBlocked">
              {{ busy ? '正在更新…' : '更新密碼並繼續' }}
            </button>
          </form>
        </div>
      </section>
    </main>

    <main v-else-if="screen === 'signed-in'" class="framework-shell" :class="{ 'sidebar-collapsed': !sidebarOpen }" :aria-busy="busy || restoringSession || menuSource === 'loading'" :inert="busy || restoringSession || menuSource === 'loading'">
      <aside class="framework-sidebar" :class="{ collapsed: !sidebarOpen }">
        <div class="framework-brand">
          <img class="brand-logo" :src="brandLogoUrl" alt="KD 科定企業" />
        </div>
        <button class="sidebar-toggle" type="button" :aria-label="sidebarOpen ? '收合選單' : '展開選單'" :title="sidebarOpen ? '收合選單' : '展開選單'" @click="sidebarOpen = !sidebarOpen">
          {{ sidebarOpen ? '‹' : '›' }}
        </button>
        <nav aria-label="主選單">
          <button
            type="button"
            class="framework-menu-item"
            :class="{ active: selectedMenuKey === null }"
            aria-label="主選單"
            title="主選單"
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
          <button type="button" class="framework-logout" aria-label="登出" title="登出" :disabled="busy || restoringSession" @click="signOut"><MenuIcon kind="logout" /><span class="menu-full-label">登出</span></button>
        </div>
      </aside>

      <section class="framework-main">
        <header class="framework-header">
          <h1>{{ currentMasterTitle }}</h1>
          <div class="framework-header-user">
            <span>{{ currentUser?.displayName || currentUser?.userNo }}</span>
            <button type="button" aria-label="登出" title="登出" :disabled="busy || restoringSession" @click="signOut">登出</button>
          </div>
        </header>
        <div v-if="errorMessage" class="framework-menu-note error" role="alert">{{ errorMessage }}</div>
        <div v-if="menuSource === 'loading'" class="framework-menu-note" role="status">主選單載入中…</div>
        <div v-if="menuSource === 'fallback'" class="framework-menu-note" role="status">
          目前顯示簡化選單；尚未遷移的功能會標示待處理。
        </div>
        <div v-if="menuSource === 'error' || menuError" class="framework-menu-note error" role="alert">
          主選單載入失敗：{{ menuError }}
          <button type="button" :disabled="busy || restoringSession" @click="loadMenuTree">重試</button>
        </div>
        <ProcessBasisMaster
          v-if="isPrsProcessBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProcessTypeMaster
          v-else-if="isPrsProcessTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductionSegmentMaster
          v-else-if="isProductionSegmentPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :enterprise-no="currentUser?.enterpriseNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <StageBasisMaster
          v-else-if="isFabPrsStageBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <EquipmentMaster
          v-else-if="isEquipmentPage"
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
          @close="navigateHome"
        />
        <CustomerContactorMaster
          v-else-if="isCustomerContactorPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <CustomerShipAddressMaster
          v-else-if="isCustomerShipAddressPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <BrokerBasisMaster
          v-else-if="isBrokerBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ForwarderBasisMaster
          v-else-if="isForwarderBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <TakerBasisMaster
          v-else-if="isTakerBasisPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
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
        <QcSpcOnlineControl
          v-else-if="isSpcOnlineControlPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSpcOnlineControlContinue
          v-else-if="isSpcOnlineControlContinuePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcIncomingInspectionForm
          v-else-if="isIncomingQCFormPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcIncomingComponentInspectionForm
          v-else-if="isIncomingQCFormComponentPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcFinalInspectionForm
          v-else-if="isFinalQCFormPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcInProcessInspectionForm
          v-else-if="isInProcessQCFormPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcOutgoingInspectionForm
          v-else-if="isOutgoingQCFormPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcVariableMeasurementImport
          v-else-if="isSpcVariableMeasurementImportPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <QcSpcDataMaintenance
          v-else-if="isSpcDataMaintenancePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcLotDataByLotNo
          v-else-if="isSpcLotDataByLotNoPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcDataEditLog
          v-else-if="isSpcDataEditLogPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcChartLineEditLog
          v-else-if="isSpcChartLineEditLogPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcQcItemBindingHisLog
          v-else-if="isSpcQcItemBindingHisLogPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcChartAllCapability
          v-else-if="isSpcChartAllCapabilityPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcChartAllCapabilityByFactor
          v-else-if="isSpcChartAllCapabilityByFactorPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcMonthRPTCodeBasis
          v-else-if="isSpcMonthRPTCodeBasisPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcMonthChartReport
          v-else-if="isSpcMonthChartReportPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcMonthChartReportByFactor
          v-else-if="isSpcMonthChartReportByFactorPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcInspMachineParameterSetup
          v-else-if="isSpcInspMachineParameterSetupPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <WipErfBasis
          v-else-if="isSpcErfBasisPage"
          class="framework-content office-wip-module"
          function-variant="spc"
          :development-preview="isDevelopmentPreview"
          :current-user-no="currentUser?.userNo || ''"
          @create8d="open8dFromErf"
          @open8d-maintain="open8dMaintainFromErf"
          @close="navigateHome"
        />
        <WipErfUnreleased
          v-else-if="isSpcErfUnreleasedPage"
          class="framework-content office-wip-module"
          function-variant="spc"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :development-preview="isDevelopmentPreview"
          :current-user-no="currentUser?.userNo || ''"
          @close="navigateHome"
        />
        <SpcXlsSourceFileControl
          v-else-if="isSpcXlsSourceFileControlPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcChartSignboard
          v-else-if="isSpcChartSignboardPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcControlChartAnalysis
          v-else-if="isSpcControlChartAnalysisPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcControlChartByFactor
          v-else-if="isSpcControlChartByFactorPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcLineChartAnalysis
          v-else-if="isSpcLineChartAnalysisPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcScatterChartAnalysis
          v-else-if="isSpcScatterChartAnalysisPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcPChartSignboard
          v-else-if="isSpcPChartSignboardPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcParetoAnalysis
          v-else-if="isSpcParetoAnalysisPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcLrrPpmChart
          v-else-if="isSpcLrrPpmChartPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcAttributeControlChart
          v-else-if="isSpcAttributeControlChartPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <SpcCoaReport
          v-else-if="isSpcCoaReportPage"
          class="framework-content"
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
        <ProductTypeMaster
          v-else-if="isProductTypePage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductModuleMaster
          v-else-if="isProductModulePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductContentsSearch
          v-else-if="isProductContentsSearchPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <RecipeGroupMaster
          v-else-if="isRecipeGroupPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductTimeLimitMaster
          v-else-if="isProductTimeLimitPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <LabelDataMaster
          v-else-if="isLabelDataPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductLabelMapMaster
          v-else-if="isProductLabelMapPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductOperationBatch
          v-else-if="isProductOperationBatchPage || isProductOperationAmendPage || isProductOperationCopyPage"
          :key="activeMasterKey"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          :amend-only="isProductOperationAmendPage"
          :copy-only="isProductOperationCopyPage"
          @close="navigateHome"
        />
        <PackingRuleBasisMaster
          v-else-if="isPackingRuleBasisPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <PackingRuleMapPage
          v-else-if="isPackingRuleMapPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <UserDefinedRunCardMaster
          v-else-if="isUserDefinedRunCardPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <WipLotBasisCreate v-else-if="isWipLotBasisCreatePage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotBasisBatch v-else-if="isWipLotBasisBatchPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotExecution v-else-if="isWipLotExecutionPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotExecutionByComponent v-else-if="isWipLotExecutionByComponentPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotChoiceEquipment v-else-if="isWipLotChoiceEquipmentPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipIpqc v-else-if="isWipIpqcPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotSplit v-else-if="isWipLotSplitPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotSplitComponent v-else-if="isWipLotSplitComponentPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotSplitBin v-else-if="isWipLotSplitBinPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotSplitCarrier v-else-if="isWipLotSplitCarrierPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotMerge v-else-if="isWipLotMergePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotMergeComponent v-else-if="isWipLotMergeComponentPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotMergeBin v-else-if="isWipLotMergeBinPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotMergeCarrier v-else-if="isWipLotMergeCarrierPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipComponentSplit v-else-if="isWipComponentSplitPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipHotLotPriority v-else-if="isWipHotLotPriorityPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipImmediateHoldSet v-else-if="isWipImmediateHoldSetPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipImmediateWaitSet v-else-if="isWipImmediateWaitSetPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipBatchImmediateHold v-else-if="isWipBatchImmediateHoldPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipFutureHoldSet v-else-if="isWipFutureHoldSetPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipFutureWaitSet v-else-if="isWipFutureWaitSetPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipFutureHoldRelease v-else-if="isWipFutureHoldReleasePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFutureWaitRelease v-else-if="isWipFutureWaitReleasePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipErfBasis v-else-if="isWipErfBasisPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" :initial-erf-no="pendingErfInfoNo" @create8d="open8dFromErf" @open8d-maintain="open8dMaintainFromErf" @close="navigateHome" />
        <WipErfUnreleased v-else-if="isWipErfUnreleasedPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipWaitBasis v-else-if="isWipWaitBasisPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipWaitDisposition v-else-if="isWipWaitDispositionPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <Wip8DBasisFactory v-else-if="isWip8DBasisFactoryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" :linked-erf="pending8dErf" @open-erf-info="openErfInfoFrom8d" @close="navigateHome" />
        <Wip8DMaintain v-else-if="isWip8DMaintainPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" :initial-qrs-no="pending8dMaintainNo" :read-only="pending8dMaintainReadOnly" @open-erf-info="openErfInfoFrom8d" @close="close8dMaintain" />
        <Wip8DClose v-else-if="isWip8DClosePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @open8d-query="open8dQueryFromClose" @close="navigateHome" />
        <Wip8DQuery v-else-if="isWip8DQueryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @open8d-query="open8dQueryFromQuery" @close="navigateHome" />
        <WipEquipmentLotState v-else-if="isWipEquipmentLotStatePage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentMaterialLoading v-else-if="isWipEquipmentMaterialLoadingPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentConsumeStatus v-else-if="isWipEquipmentConsumeStatusPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentConsumeStatusLog v-else-if="isWipEquipmentConsumeStatusLogPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentOnlineQc v-else-if="isWipEquipmentOnlineQcPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentQcListLog v-else-if="isWipEquipmentQcListLogPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipAreaShow v-else-if="isWipAreaShowPage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :current-user-no="currentUser?.userNo || ''" :enterprise-no="currentUser?.enterpriseNo || ''" :development-preview="isDevelopmentPreview" :work-area-mode="true" @close="navigateHome" />
        <WipRunCard v-else-if="isWipRunCardPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotSheetPrint v-else-if="isWipLotSheetPrintPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipWorkorderPrint v-else-if="isWipWorkorderPrintPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipSkipInspectionSetting v-else-if="isWipSkipInspectionSettingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipBumpMapMerge v-else-if="isWipBumpMapMergePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipBumpMapFileManage v-else-if="isWipBumpMapFileManagePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipGoldenMapFileManage v-else-if="isWipGoldenMapFileManagePage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipWaferMapExport v-else-if="isWipWaferMapExportPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipProbingLog v-else-if="isWipProbingLogPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotComponentGen v-else-if="isWipLotComponentGenPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotBasisCreateCp v-else-if="isWipLotBasisCreateCpPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEqpSetupBasisCp v-else-if="isWipEqpSetupBasisCpPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInE10Icd v-else-if="isWipFgdStockInE10IcdPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInT100 v-else-if="isWipFgdStockInT100Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInTp v-else-if="isWipFgdStockInTpPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInTpCancel v-else-if="isWipFgdStockInTpCancelPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInWorkFlow v-else-if="isWipFgdStockInWorkFlowPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInWorkFlowCancel v-else-if="isWipFgdStockInWorkFlowCancelPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInT100Cancel v-else-if="isWipFgdStockInT100CancelPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdStockInE10IcdCancel v-else-if="isWipFgdStockInE10IcdCancelPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipScrapStockInE10Icd v-else-if="isWipScrapStockInE10IcdPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipInvCountStateAdjust v-else-if="isWipInvCountStateAdjustPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipInvCountStatePrint v-else-if="isWipInvCountStatePrintPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipInvCountState v-else-if="isWipInvCountStatePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipBartenderPrint v-else-if="isWipBartenderPrintPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipUnInventory v-else-if="isWipUnInventoryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipOperatorLogin v-else-if="isWipOperatorLoginPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipOperatorLoginLot v-else-if="isWipOperatorLoginLotPage" class="framework-content office-wip-module" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipOperatorClockIn v-else-if="isWipOperatorClockInPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipOperatorClockOut v-else-if="isWipOperatorClockOutPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipOperatorOnline v-else-if="isWipOperatorOnlinePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipSwrBasis v-else-if="isWipSwrBasisPage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipUndoDispatch v-else-if="isWipUndoDispatchPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipOutsourceCreate v-else-if="isWipOutsourceCreatePage || isWipOutsourceConfirmPage || isWipOutsourceReturnPage || isWipOutsourceCancelPage" :key="activeMasterKey" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :page-mode="isWipOutsourceConfirmPage ? 'confirm' : isWipOutsourceReturnPage ? 'return' : isWipOutsourceCancelPage ? 'cancel' : undefined" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <WipLotReverse v-else-if="isWipLotReversePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipReleasedLotDelete v-else-if="isWipReleasedLotDeletePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipReleasedLotBatchDelete v-else-if="isWipReleasedLotBatchDeletePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotAreaChange v-else-if="isWipLotAreaChangePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipProductVersionChange v-else-if="isWipProductVersionChangePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEcnWizard v-else-if="isWipEcnWizardPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipCloseLotForcibly v-else-if="isWp916Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipChangeLotProduct v-else-if="isWp915Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotPropertyAdjust v-else-if="isWp913Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotProcessAttributeAdjust v-else-if="isWp912Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotMaterialAdjust v-else-if="isWp911Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipTransferFactory v-else-if="isWp909Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLabelPrint v-else-if="isWp908Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotQuantityAdjust v-else-if="isWp907Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateChange v-else-if="isEmsEquipmentStateChangePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <EmsEquipmentControl v-else-if="isEmsEquipmentControlPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentAccStateLogQuery v-else-if="isEmsEquipmentAccStateLogQueryPage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentAccStateQuery v-else-if="isEmsEquipmentAccStateQueryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentCapabilityBasis v-else-if="isEmsEquipmentCapabilityBasisPage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentCapabilityMapping v-else-if="isEmsEquipmentCapabilityMappingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateByArea v-else-if="isEmsEquipmentStateByAreaPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateByType v-else-if="isEmsEquipmentStateByTypePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateMonitor v-else-if="isEmsAccessoryStateMonitorPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateMonitorByEquipment v-else-if="isEmsAccessoryStateMonitorByEquipmentPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateControlSetting v-else-if="isEmsAccessoryStateControlSettingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateLogQuery v-else-if="isEmsAccessoryStateLogQueryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateQuery v-else-if="isEmsAccessoryStateQueryPage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryStateMonitorBu1 v-else-if="isEmsAccessoryStateMonitorBu1Page" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsAccessoryRepair v-else-if="isEmsAccessoryRepairPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsApplyEquipmentAccessory v-else-if="isEmsApplyEquipmentAccessoryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <EmsAccessoryStateViolativeSetting v-else-if="isEmsAccessoryStateViolativeSettingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateViolativeSetting v-else-if="isEmsEquipmentStateViolativeSettingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsReturnEquipmentAccessory v-else-if="isEmsReturnEquipmentAccessoryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" :current-user-no="currentUser?.userNo || ''" @close="navigateHome" />
        <EmsEquipmentStateLogQuery v-else-if="isEmsEquipmentStateLogQueryPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateLogByState v-else-if="isEmsEquipmentStateLogByStatePage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateLog v-else-if="isEmsEquipmentStateLogPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateControlSetting v-else-if="isEmsEquipmentStateControlSettingPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentStateStatistics v-else-if="isEmsEquipmentStateStatisticsPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <EmsEquipmentReasonBasis v-else-if="isEmsEquipmentReasonBasisPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotOperatorClockIn v-else-if="isWipLotOperatorClockInPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipLotOperatorClockOut v-else-if="isWipLotOperatorClockOutPage" class="framework-content office-wip-module" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipEquipmentFavorite v-else-if="isWipEquipmentFavoritePage" class="framework-content office-wip-module" :api-base-url="selectedEnvironmentConfig.apiBaseUrl" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipCarrierState v-else-if="isWipCarrierStatePage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipCarrierManagement v-else-if="isWipCarrierManagementPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipInventory v-else-if="isWipInventoryPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <WipFgdInventory v-else-if="isWipFgdInventoryPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <PackingMOLine v-else-if="isWipPackingMOLinePage" key="wp601" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <PackingMOLine v-else-if="isWipPackingMOLineInvPage" key="wp629" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" :inventory-mode="true" @close="navigateHome" />
        <SemiInventory v-else-if="isWipSemiInventoryPage" class="framework-content" :current-user-no="currentUser?.userNo || ''" :development-preview="isDevelopmentPreview" @close="navigateHome" />
        <ReceivingOrderMaster
          v-else-if="isReceivingOrderPage || isReceivingOrderClosePage"
          :key="activeMasterKey"
          :closing-only="isReceivingOrderClosePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ManufacturingOrderMaster
          v-else-if="manufacturingOrderPage"
          :key="activeMasterKey"
          :page-mode="manufacturingOrderPages[manufacturingOrderPage]"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductRunCardMapMaster
          v-else-if="isProductRunCardMapPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductQCSpecImport
          v-else-if="isProductQCSpecImportPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductGbomBatchImport
          v-else-if="isProductGbomBatchImportPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <FtProgramMaster
          v-else-if="isFtProgramPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <FtProgramMaster
          v-else-if="isCpProgramPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          cp
          @close="navigateHome"
        />
        <EqcSampleRuleMaster
          v-else-if="isEqcSampleRulePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductQcFrequencyMaster
          v-else-if="isProductQcFrequencyPage"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductWorkTimeMaster
          v-else-if="isProductWorkTimePage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductOverstayMaster
          v-else-if="isProductOverstayPage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductStationRuleMaster
          v-else-if="isProductStationRulePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <CpWaferShipReportMaster
          v-else-if="isCpWaferShipReportPage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <CpWaferRuleMaster
          v-else-if="isCpWaferRulePage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <CpMergeMapRuleMaster
          v-else-if="isCpMergeMapRulePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ChooseWaferRuleMaster
          v-else-if="isChooseWaferRulePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <UploadFileTypeMaster
          v-else-if="isUploadFileTypePage"
          class="framework-content"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <TestProgramFileMaster
          v-else-if="isTestProgramFilePage"
          class="framework-content"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          @close="navigateHome"
        />
        <ProductBasisMaster
          v-else-if="isPackagingProductPage && isDevelopmentPreview"
          class="framework-content"
          :api-base-url="selectedEnvironmentConfig.apiBaseUrl"
          :current-user-no="currentUser?.userNo || ''"
          :development-preview="isDevelopmentPreview"
          packaging
          @close="navigateHome"
        />
        <main v-else-if="isPackagingProductPage" class="framework-content">封測產品服務待接續。</main>
        <ProductBasisMaster
          v-else-if="activeMasterDataKey === 'product-master'"
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

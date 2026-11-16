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
import MaterialVendorMaster from './components/MaterialVendorMaster.vue'
import MaterialTypeMaster from './components/MaterialTypeMaster.vue'
import MaterialTypeQCItemMaster from './components/MaterialTypeQCItemMaster.vue'
import MaterialQCItemMaster from './components/MaterialQCItemMaster.vue'
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
    children: [{ id: 'operation-settings', label: '作業站設定', shortLabel: '設', children: [{ id: 'operation-master', label: '作業站主檔', tableName: 'tblopbasis', shortLabel: '主', functionNo: 'OP BASIS', moduleNo: 'OP' }] }],
  },
  {
    id: 'material-data',
    label: '物料資料',
    shortLabel: '料',
    moduleNo: 'MTL',
    children: [{ id: 'material-settings', label: '物料設定', shortLabel: '設', children: [
      { id: 'material-master', label: '物料基本資料', tableName: 'tblmtlmaterialbasis', shortLabel: '主', functionNo: 'MTL BASIS', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL VENDOR', label: '物料供應商基本資料', shortLabel: '供', functionNo: 'MTL VENDOR', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL TYPE', label: '物料類別資料', shortLabel: '類', functionNo: 'MTL TYPE', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL TYPE QCITEM', label: '物料類別品管項目設定', shortLabel: '品', functionNo: 'MTL TYPE QCITEM', moduleNo: 'MTL' },
      { id: 'function:MTL:MTL QCITEM', label: '物料品管項目設定', shortLabel: '品', functionNo: 'MTL QCITEM', moduleNo: 'MTL' },
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
  'MTL BASIS': `${appBasePath}/MasterData/Materials`,
  'MTL VENDOR': materialVendorRoute,
  'MTL TYPE': materialTypeRoute,
  'MTL TYPE QCITEM': materialTypeQCItemRoute,
  'MTL QCITEM': materialQCItemRoute,
  'PRS BASIS': `${appBasePath}/MasterData/Processes`,
  'PRD BASIS': `${appBasePath}/MasterData/Products`,
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
const isMaterialVendorPage = computed(() => activeMasterKey.value === 'function:MTL:MTL VENDOR')
const isMaterialTypePage = computed(() => activeMasterKey.value === 'function:MTL:MTL TYPE')
const isMaterialTypeQCItemPage = computed(() => activeMasterKey.value === 'function:MTL:MTL TYPE QCITEM')
const isMaterialQCItemPage = computed(() => activeMasterKey.value === 'function:MTL:MTL QCITEM')
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

function routeForMenuNode(item: MenuNode): string {
  const functionNo = item.functionNo?.trim().toUpperCase()
  const knownRoute = item.moduleNo?.toUpperCase() === 'EQP' && item.label.trim() === '設備供應商基本資料'
    ? equipmentVendorRoute
    : item.moduleNo?.toUpperCase() === 'EQP' && item.label.trim() === '設備工程群組資料'
      ? equipmentEngineerGroupRoute
      : undefined
  return menuRoutes[item.id] ?? knownRoute ?? (functionNo ? functionRoutes[functionNo] : undefined) ??
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
    menuTree.value = import.meta.env.DEV ? addDevelopmentEquipmentFunctions(apiMenuTree) : apiMenuTree
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
        <MaterialVendorMaster
          v-else-if="isMaterialVendorPage"
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

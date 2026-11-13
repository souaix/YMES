<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import MaterialFeatureDialog from './MaterialFeatureDialog.vue'
import OperationFeatureDialog from './OperationFeatureDialog.vue'

type MasterKey = 'operation-master' | 'material-master' | 'process-master' | 'product-master'
type Value = string | number | boolean | null
type Row = Record<string, Value>
type Field = { key: string; label: string; type?: 'text' | 'number' | 'quantity' | 'checkbox' | 'radio' | 'textarea' | 'select'; required?: boolean; yellow?: boolean; step?: string; wide?: boolean; readonly?: boolean; lookup?: boolean; min?: number; max?: number; options?: Array<{ label: string; value: Value }> }

type OperationFeature = 'property' | 'resource' | 'business-rule' | 'parameter' | 'qc-item' | 'error' | 'copy-op-data' | 'sub-op' | 'import'
type MaterialFeature = 'property' | 'qc-item' | 'import' | 'gum-hour' | 'vendor-map'
const props = defineProps<{ masterKey: MasterKey; apiBaseUrl: string; developmentPreview?: boolean }>()
const window = globalThis.window
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const configs: Record<MasterKey, { title: string; plural: string; endpoint: string; keyFields: string[]; columns: Field[]; sections: { title: string; fields: Field[] }[] }> = {
  'operation-master': {
    title: '作業站主檔', plural: '作業站', endpoint: 'operations', keyFields: ['operationNo'],
    columns: [{ key: 'operationNo', label: '作業編號' }, { key: 'operationName', label: '作業名稱' }, { key: 'shortName', label: '簡稱' }, { key: 'operationType', label: '作業類別' }, { key: 'operationClass', label: '作業分類' }, { key: 'productionSegmentNo', label: '製程段' }, { key: 'operationOrder', label: '工序順序' }, { key: 'countOperationUnitQty', label: '單位批量' }, { key: 'standardUnitRunTime', label: '標準工時' }, { key: 'standardQueueTime', label: '標準等待時間' }, { key: 'materialOption', label: '物料選項' }, { key: 'multiOperatorMode', label: '多人作業' }, { key: 'printOutOnRunCard', label: 'Run Card' }, { key: 'spcInspAbnormalHoldLot', label: 'SPC Hold' }, { key: 'errorQtyToSpc', label: 'SPC 異常數量' }],
    sections: [
      { title: '作業識別', fields: [{ key: 'operationNo', label: '作業編號', required: true, yellow: true }, { key: 'operationType', label: '作業類別', required: true, yellow: true, readonly: true, lookup: true }, { key: 'operationName', label: '作業名稱', required: true, yellow: true, wide: true }, { key: 'operationClass', label: '作業分類', type: 'select', options: [{ label: '一般作業站', value: 0 }, { label: '品質管理', value: 1 }, { label: '其他作業站', value: 2 }] }, { key: 'shortName', label: '簡稱' }, { key: 'productionSegmentNo', label: '製程段', required: true, readonly: true, lookup: true }, { key: 'operationOrder', label: '工序順序', type: 'number' }, { key: 'description', label: '說明', type: 'textarea', wide: true }] },
      { title: '製程與品質設定', fields: [{ key: 'standardUnitRunTime', label: '標準單位工時', type: 'number', step: '0.01', min: 0 }, { key: 'standardQueueTime', label: '標準等待時間', type: 'number', step: '0.01', min: 0 }, { key: 'countOperationUnitQty', label: '作業單位批量', type: 'number', min: 1 }, { key: 'materialOption', label: '物料選項', type: 'select' }, { key: 'multiOperatorMode', label: '多人作業模式', type: 'select' }, { key: 'printOutOnRunCard', label: '列印於 Run Card', type: 'checkbox' }, { key: 'spcInspAbnormalHoldLot', label: 'SPC 異常時 Hold Lot', type: 'checkbox' }] },
    ],
  },
  'material-master': {
    title: '物料主檔', plural: '物料', endpoint: 'materials', keyFields: ['materialNo'],
    columns: [{ key: 'materialNo', label: '物料編號' }, { key: 'materialType', label: '物料類別' }, { key: 'materialName', label: '物料名稱' }, { key: 'materialSpec', label: '物料規格' }, { key: 'description', label: '說明' }, { key: 'putInPlace', label: '投料點' }, { key: 'countWay', label: '計量方法' }, { key: 'checkLotNo', label: '檢查批號' }, { key: 'unitType', label: '單位類別' }, { key: 'unitNo', label: '單位編號' }, { key: 'safeQty', label: '安全存量' }, { key: 'keyMaterials', label: '關鍵物料' }, { key: 'openLimitTimeFlag', label: '開封時限管理' }, { key: 'openLimitTime', label: '開封時限' }, { key: 'resetOpenDate', label: '下料後重啟計時' }, { key: 'expireDateFlag', label: '保存期限管理' }, { key: 'erpNo', label: 'ERP 編號' }],
    sections: [{ title: '物料主檔資料', fields: [{ key: 'materialNo', label: '物料編號', required: true, yellow: true }, { key: 'materialName', label: '物料名稱', required: true, yellow: true }, { key: 'materialSpec', label: '物料規格' }, { key: 'materialType', label: '物料類別', required: true, yellow: true, readonly: true, lookup: true }, { key: 'putInPlace', label: '投料點', type: 'select', required: true, yellow: true }, { key: 'countWay', label: '計量方法', type: 'select' }, { key: 'checkLotNo', label: '是否檢查批號', type: 'select' }, { key: 'unitType', label: '單位類別', readonly: true, lookup: true }, { key: 'unitNo', label: '單位編號', required: true, yellow: true, readonly: true, lookup: true }, { key: 'safeQty', label: '安全存量', type: 'number', step: '0.001', min: 0 }, { key: 'expireDateFlag', label: '保存期限管理', type: 'checkbox' }, { key: 'openLimitTimeFlag', label: '開封後有效時間管理', type: 'checkbox' }, { key: 'openLimitTime', label: '開封時限（小時）', type: 'number', step: '0.1', min: 0 }, { key: 'resetOpenDate', label: '下料後重啟開封計時日', type: 'checkbox' }, { key: 'keyMaterials', label: '是否關鍵物料', type: 'radio', options: [{ label: '是', value: true }, { label: '否', value: false }] }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
  'process-master': {
    title: '流程主檔', plural: '流程', endpoint: 'processes', keyFields: ['processNo', 'processVersion'],
    columns: [{ key: 'processNo', label: '流程編號' }, { key: 'processVersion', label: '流程版本' }, { key: 'currentVersion', label: '目前版本' }, { key: 'processClass', label: '流程分類' }, { key: 'processType', label: '流程類別' }, { key: 'productionSegmentNo', label: '製程段' }, { key: 'description', label: '說明' }],
    sections: [{ title: '流程主檔資料', fields: [{ key: 'processNo', label: '流程編號', required: true, yellow: true }, { key: 'processVersion', label: '流程版本', required: true, yellow: true }, { key: 'processType', label: '流程類別', required: true, readonly: true, lookup: true }, { key: 'productionSegmentNo', label: '製程段', required: true, readonly: true, lookup: true }, { key: 'processClass', label: '流程分類', type: 'select', required: true }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
  'product-master': {
    title: '產品主檔', plural: '產品', endpoint: 'products', keyFields: ['productNo', 'productVersion'],
    columns: [{ key: 'productNo', label: '產品編號' }, { key: 'productName', label: '產品名稱' }, { key: 'productCode', label: '產品代碼' }, { key: 'productVersion', label: '產品版本' }, { key: 'productType', label: '產品類別' }, { key: 'unitType', label: '單位類別' }, { key: 'unitNo', label: '庫存單位' }, { key: 'lotStdQty', label: 'Lot 標準量' }, { key: 'specNo', label: '規格編號' }, { key: 'erpNo', label: 'ERP 編號' }, { key: 'serialTypeNoLot', label: 'Lot 序號類型' }, { key: 'serialTypeNoComp', label: 'Component 序號類型' }, { key: 'currentVersion', label: '目前版本' }],
    sections: [{ title: '產品主檔資料', fields: [{ key: 'productNo', label: '產品編號', required: true, yellow: true }, { key: 'specNo', label: '規格編號' }, { key: 'productVersion', label: '產品版本', required: true, yellow: true }, { key: 'productName', label: '產品名稱', required: true, yellow: true, wide: true }, { key: 'productCode', label: '產品代碼' }, { key: 'productType', label: '產品類別', required: true, readonly: true, lookup: true }, { key: 'lotStdQty', label: 'Lot 標準量', type: 'quantity', step: '0.001', min: 0 }, { key: 'unitType', label: '單位類別', readonly: true, lookup: true }, { key: 'unitNo', label: '庫存單位', required: true, readonly: true, lookup: true }, { key: 'serialTypeNoLot', label: 'Lot 序號類型', readonly: true, lookup: true }, { key: 'serialTypeNoComp', label: 'Component 序號類型', readonly: true, lookup: true }, { key: 'description', label: '說明', type: 'textarea', wide: true }] }],
  },
}
const config = computed(() => configs[props.masterKey])
const searchField = computed(() => ({
  'operation-master': { key: 'OPNo', label: '作業編號' },
  'material-master': { key: 'MaterialNo', label: '物料編號' },
  'process-master': { key: 'PROCESSNO', label: '流程編號' },
  'product-master': { key: 'PRODUCTNO', label: '產品編號' },
}[props.masterKey]))
const pageTitle = computed(() => config.value.title)
const rows = ref<Row[]>([])
const selected = ref<Row | null>(null)
const issueState = ref(0)
const keyword = ref('')
const busy = ref(false)
const error = ref('')
const notice = ref('')
const formOpen = ref(false)
const formMode = ref<'new' | 'edit' | 'copy'>('new')
const form = ref<Row>({})
const lotStdQtyEnabled = ref(false)
const processes = ref<Row[]>([])
const bom = ref<Row[]>([])
const availableProcesses = ref<Row[]>([])
const availableOperations = ref<Row[]>([])
const availableMaterials = ref<Row[]>([])
const availableProducts = ref<Row[]>([])
const graph = ref<{ nodes: Row[]; edges: Row[]; dataStamp: number }>({ nodes: [], edges: [], dataStamp: 0 })
const processDraft = ref<Row>({ moTypeNo: 10, productionSegmentNo: 'FRONT', processNo: '', processVersionSelector: '*', defaultProcess: true })
const bomDraft = ref<Row>({ componentType: 'MATERIAL', componentNo: '', inputOperationNo: '', standardQty: 1, putInPlaceType: 'At OP', specified: false, decreaseRate: 0, description: '' })
const newNode = ref({ nodeId: '', nodeNo: '', nodeType: 'OP', sequence: 10, operationNo: '', description: '' })
const newEdge = ref({ fromNodeId: '', toNodeId: '', action: 'pass' })
let initialSelection = ''

const selectedKey = computed(() => selected.value ? config.value.keyFields.map((key) => String(selected.value?.[key] ?? '')).join('/') : '')
const unsupportedGraphNodeTypes = computed(() => graph.value.nodes.filter((node) => !['START', 'OP', 'END'].includes(String(node.nodeType).toUpperCase())))
const availableProcessNos = computed(() => [...new Set(availableProcesses.value.map((item) => String(item.processNo ?? '')))].filter(Boolean))
const availableProcessVersions = computed(() => availableProcesses.value.filter((item) => String(item.processNo) === String(processDraft.value.processNo)).map((item) => String(item.processVersion ?? '')).filter(Boolean))
const isEditable = computed(() => selected.value?.issueState === 0)
const selectedRouteKey = (row: Row) => config.value.keyFields.map((key) => String(row[key] ?? '')).join('/')
const stateLabel = (value: Value) => ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知'
const displayValue = (value: Value) => typeof value === 'boolean' ? (value ? '是' : '否') : value === null || value === undefined || value === '' ? '—' : String(value)
const fieldValue = (field: Field) => {
  const value = form.value[field.key]
  return value === null || value === undefined ? '' : String(value)
}
const hasFieldOptions = (field: Field) => Boolean(field.options?.length)
  || (props.masterKey === 'operation-master' && props.developmentPreview && ['materialOption', 'multiOperatorMode'].includes(field.key))
  || (props.masterKey === 'material-master' && props.developmentPreview && ['putInPlace', 'countWay', 'checkLotNo'].includes(field.key))
const optionsForField = (field: Field) => {
  if (field.options?.length) return field.options
  if (props.masterKey === 'material-master' && props.developmentPreview && field.key === 'putInPlace') return [
    { label: 'EQP（DEV）', value: 0 }, { label: 'OP（DEV）', value: 1 }, { label: 'WIP INV（DEV）', value: 2 }, { label: 'MO（DEV）', value: 3 },
  ]
  if (props.masterKey === 'material-master' && props.developmentPreview && field.key === 'countWay') return [
    { label: 'Standard（DEV 代碼，ValueList 待核實）', value: 0 }, { label: 'Real（DEV 代碼，ValueList 待核實）', value: 1 },
  ]
  if (props.masterKey === 'material-master' && props.developmentPreview && field.key === 'checkLotNo') return [
    { label: '否（DEV 代碼，ValueList 待核實）', value: 0 }, { label: '是（DEV 代碼，ValueList 待核實）', value: 1 },
  ]
  if (props.masterKey === 'operation-master' && props.developmentPreview && field.key === 'materialOption') return [
    { label: '發料點（DEV 來源碼選項）', value: 0 }, { label: '設備（DEV 來源碼選項）', value: 1 }, { label: '產線（DEV 來源碼選項）', value: 2 }, { label: '工位（DEV 來源碼選項）', value: 3 },
  ]
  if (props.masterKey === 'operation-master' && props.developmentPreview && field.key === 'multiOperatorMode') return [
    { label: '作業站（DEV 來源碼選項）', value: 0 }, { label: '區域（DEV 來源碼選項）', value: 1 }, { label: '設備（DEV 來源碼選項）', value: 2 }, { label: '產線（DEV 來源碼選項）', value: 3 }, { label: '工位（DEV 來源碼選項）', value: 4 },
  ]
  return []
}
function setLotStdQtyEnabled(enabled: boolean): void {
  lotStdQtyEnabled.value = enabled
  if (!enabled) form.value.lotStdQty = 0
}

type MasterAction = { key: string; label: string }
const relationTab = ref<'processes' | 'bom' | 'equipment'>('processes')
const processDrawOpen = ref(false)
const operationFeature = ref<OperationFeature | null>(null)
const materialFeature = ref<{ feature: MaterialFeature; mode: 'add' | 'edit'; vendor: Row | null } | null>(null)
const vendorRows = ref<Row[]>([])
const selectedVendor = ref<Row | null>(null)
const materialLookupField = ref<string | null>(null)
const isMaterialPreview = computed(() => props.masterKey === 'material-master' && Boolean(props.developmentPreview))
const materialFeatureKeys = new Set<MaterialFeature>(['property', 'qc-item', 'import', 'gum-hour'])
const devMaterialStorageKey = 'ymes:dev:master-data:materials:v1'

function seedDevMaterials(): Row[] {
  return [
    { materialNo: 'MAT-GUM-001', materialType: 'GUM', materialName: '固晶膠 A（DEV）', materialSpec: 'KG-100', description: '開發區示範資料', putInPlace: 0, countWay: 0, checkLotNo: 1, unitType: '重量', unitNo: 'KG', safeQty: 5, keyMaterials: true, openLimitTimeFlag: true, openLimitTime: 24, resetOpenDate: false, expireDateFlag: true, erpNo: 'DEV-GUM-001', issueState: 0, dataStamp: 0 },
    { materialNo: 'MAT-CHEM-002', materialType: 'CHEM', materialName: '清洗液 B（DEV）', materialSpec: 'CL-250', description: '開發區示範資料', putInPlace: 1, countWay: 0, checkLotNo: 1, unitType: '體積', unitNo: 'L', safeQty: 12, keyMaterials: false, openLimitTimeFlag: false, openLimitTime: 0, resetOpenDate: false, expireDateFlag: true, erpNo: 'DEV-CHEM-002', issueState: 2, dataStamp: 1 },
    { materialNo: 'MAT-PACK-003', materialType: 'PACK', materialName: '包裝盒 C（DEV）', materialSpec: 'BX-20', description: '開發區示範資料', putInPlace: 2, countWay: 0, checkLotNo: 0, unitType: '數量', unitNo: 'EA', safeQty: 50, keyMaterials: false, openLimitTimeFlag: false, openLimitTime: 0, resetOpenDate: false, expireDateFlag: false, erpNo: 'DEV-PACK-003', issueState: 1, dataStamp: 0 },
    { materialNo: 'MAT-WIP-004', materialType: 'CHEM', materialName: '研發試料 D（DEV）', materialSpec: 'LAB-01', description: '開發區示範資料', putInPlace: 3, countWay: 0, checkLotNo: 0, unitType: '數量', unitNo: 'EA', safeQty: 0, keyMaterials: false, openLimitTimeFlag: false, openLimitTime: 0, resetOpenDate: false, expireDateFlag: false, erpNo: 'DEV-WIP-004', issueState: -1, dataStamp: 1 },
  ]
}

function readDevMaterials(): Row[] {
  try {
    const stored = window.localStorage.getItem(devMaterialStorageKey)
    if (stored) return JSON.parse(stored) as Row[]
  } catch { /* use the local preview fixture when browser storage is unavailable */ }
  const seeded = seedDevMaterials()
  try { window.localStorage.setItem(devMaterialStorageKey, JSON.stringify(seeded)) } catch { /* the preview remains usable for this page session */ }
  return seeded
}

function writeDevMaterials(items: Row[]): void {
  window.localStorage.setItem(devMaterialStorageKey, JSON.stringify(items))
}

function devVendorStorageKey(materialNo: string): string {
  return `ymes:dev:material-vendors:${encodeURIComponent(materialNo)}:v1`
}

function readDevVendorRows(materialNo: string): Row[] {
  if (!materialNo) return []
  const key = devVendorStorageKey(materialNo)
  try {
    const stored = window.localStorage.getItem(key)
    if (stored) return JSON.parse(stored) as Row[]
    const seeded = [{ materialNo, vendorNo: 'V-DEV-01', vendorName: '示範供應商（DEV）', description: '開發區假資料' }]
    window.localStorage.setItem(key, JSON.stringify(seeded))
    return seeded
  } catch { return [] }
}

function loadVendorRows(): void {
  vendorRows.value = isMaterialPreview.value && selected.value
    ? readDevVendorRows(String(selected.value.materialNo ?? ''))
    : []
  selectedVendor.value = null
}

function openMaterialFeature(feature: MaterialFeature, mode: 'add' | 'edit' = 'add', vendor: Row | null = null): void {
  if (!isMaterialPreview.value || (!selected.value && feature !== 'import')) return
  materialFeature.value = { feature, mode, vendor }
}

function deleteMaterialVendor(): void {
  const materialNo = String(selected.value?.materialNo ?? '')
  const vendorNo = String(selectedVendor.value?.vendorNo ?? '')
  if (!materialNo || !vendorNo || !window.confirm(`確定刪除供應商對應 ${vendorNo}？`)) return
  const next = readDevVendorRows(materialNo).filter((row) => String(row.vendorNo) !== vendorNo)
  window.localStorage.setItem(devVendorStorageKey(materialNo), JSON.stringify(next))
  loadVendorRows()
  notice.value = '物料供應商對應已從本機 DEV 假資料移除。'
}

function handleMaterialFeatureSaved(): void {
  if (materialFeature.value?.feature === 'import') {
    issueState.value = 0
    keyword.value = ''
  }
  if (materialFeature.value?.feature === 'vendor-map') loadVendorRows()
  notice.value = '物料子功能 DEV 假資料已更新並儲存在此瀏覽器。'
  void loadRows()
}

const materialLookupOptions = computed(() => {
  if (materialLookupField.value === 'materialType') return [
    { value: 'GUM', label: 'GUM 固晶膠類別（DEV）' }, { value: 'CHEM', label: 'CHEM 化學物料（DEV）' }, { value: 'PACK', label: 'PACK 包裝物料（DEV）' },
  ]
  if (materialLookupField.value === 'unitType') return [
    { value: '重量', label: '重量（DEV）' }, { value: '體積', label: '體積（DEV）' }, { value: '數量', label: '數量（DEV）' },
  ]
  if (materialLookupField.value === 'unitNo') return [
    { value: 'KG', label: 'KG 公斤（DEV）' }, { value: 'L', label: 'L 公升（DEV）' }, { value: 'EA', label: 'EA 個（DEV）' }, { value: 'ROLL', label: 'ROLL 卷（DEV）' },
  ]
  return []
})
function materialLookupTitle(fieldKey: string | null): string {
  return ({ materialType: '物料類別', unitType: '單位類別', unitNo: '單位編號' } as Record<string, string>)[fieldKey ?? ''] ?? '系統選項'
}
const masterActionSets: Record<MasterKey, MasterAction[]> = {
  'operation-master': [
    { key: 'add', label: '新增(A)' }, { key: 'edit', label: '修改(E)' }, { key: 'delete', label: '刪除(D)' },
    { key: 'copy', label: '複製(C)' }, { key: 'approve', label: '核准' }, { key: 'property', label: '屬性' },
    { key: 'resource', label: '資源' }, { key: 'business-rule', label: '商業規則' }, { key: 'parameter', label: '參數' },
    { key: 'qc-item', label: 'QC Item' }, { key: 'error', label: 'Error' }, { key: 'copy-op-data', label: '複製作業資料' },
    { key: 'sub-op', label: 'SubOP' }, { key: 'import', label: 'Import' }, { key: 'close', label: '關閉' },
  ],
  'material-master': [
    { key: 'add', label: '新增(A)' }, { key: 'edit', label: '修改(E)' }, { key: 'delete', label: '刪除(D)' },
    { key: 'copy', label: '複製(C)' }, { key: 'approve', label: '核准' }, { key: 'property', label: '屬性' },
    { key: 'qc-item', label: 'QC Item' }, { key: 'import', label: 'Import' }, { key: 'gum-hour', label: 'GumHour' },
  ],
  'process-master': [
    { key: 'add', label: '新增(A)' }, { key: 'edit', label: '修改(E)' }, { key: 'delete', label: '刪除(D)' },
    { key: 'copy', label: '複製(C)' }, { key: 'version', label: 'Version(V)' }, { key: 'approve', label: '核准' },
    { key: 'property', label: 'Property(S)' }, { key: 'exclude-op', label: 'Exclude OP' }, { key: 'aps', label: 'APS Integration' }, { key: 'import', label: 'Import' },
    { key: 'close', label: '關閉' },
  ],
  'product-master': [
    { key: 'add', label: '新增(A)' }, { key: 'edit', label: '修改(E)' }, { key: 'delete', label: '刪除(D)' },
    { key: 'copy', label: '複製(C)' }, { key: 'new-version', label: 'Version(V)' }, { key: 'approve', label: '核准' },
    { key: 'property', label: '屬性(S)' }, { key: 'parameter', label: 'Parameter(P)' }, { key: 'business-rule', label: 'Business Rule' },
    { key: 'op-description', label: 'OPDescription' }, { key: 'recipe-group', label: 'RecipeGroup' }, { key: 'accessory', label: 'Accessory' },
    { key: 'resource', label: 'Resource' }, { key: 'sub-op', label: 'SubOP' }, { key: 'import', label: 'Import' },
    { key: 'default', label: 'Default' }, { key: 'run-time', label: 'RunTime' }, { key: 'qc-item', label: 'QC Item' },
    { key: 'packing-set', label: 'Packing Set' }, { key: 'eqp-cap-item', label: 'EQPCapItem' }, { key: 'esop', label: 'eSOP' },
    { key: 'equipment-spec', label: 'EquipmentSpec' }, { key: 'aps', label: 'APS Integration' },
    { key: 'close', label: '關閉' },
  ],
}
const toolbarActions = computed(() => masterActionSets[props.masterKey])
const supportedToolbarActions = new Set(['add', 'edit', 'copy', 'approve', 'delete', 'close', 'new-version', 'draw', 'property', 'resource', 'business-rule', 'parameter', 'qc-item', 'error', 'copy-op-data', 'sub-op', 'import', 'gum-hour'])

const operationFeatureKeys = new Set<OperationFeature>(['property', 'resource', 'business-rule', 'parameter', 'qc-item', 'error', 'copy-op-data', 'sub-op', 'import'])

function actionDisabled(key: string): boolean {
  if (busy.value) return true
  const row = selected.value
  if (!supportedToolbarActions.has(key)) return true
  if (props.masterKey === 'operation-master' && operationFeatureKeys.has(key as OperationFeature)) {
    if (!props.developmentPreview) return true
    return key !== 'import' && !row
  }
  if (materialFeatureKeys.has(key as MaterialFeature)) {
    if (props.masterKey !== 'material-master' || !props.developmentPreview) return true
    if (key !== 'import' && !row) return true
    if (key === 'gum-hour' && String(row?.materialType ?? '').toUpperCase() !== 'GUM') return true
  } else if (operationFeatureKeys.has(key as OperationFeature)) return true
  if (['edit', 'copy', 'approve', 'delete', 'new-version', 'set-current'].includes(key) && !row) return true
  if (key === 'edit' || key === 'approve') return row?.issueState !== 0
  if (key === 'delete') return ![0, 2, -1].includes(Number(row?.issueState))
  if (key === 'new-version') return props.masterKey !== 'product-master' || row?.issueState !== 2
  if (key === 'set-current') return props.masterKey !== 'product-master' || row?.issueState !== 2 || row?.currentVersion !== false
  if (key === 'draw') return props.masterKey !== 'process-master' || !row || row.issueState !== 0
  return false
}

function closeMasterPage(): void {
  window.history.pushState({}, '', appBase + '/')
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function runToolbarAction(key: string): void {
  if (actionDisabled(key)) return
  if (key === 'add') openForm('new')
  else if (key === 'edit' && selected.value) openForm('edit', selected.value)
  else if (key === 'copy' && selected.value) openForm('copy', selected.value)
  else if (key === 'approve') void command('approve')
  else if (key === 'delete') void command('delete')
  else if (key === 'new-version') void command('new-version')
  else if (key === 'set-current') void command('set-current')
  else if (key === 'download') downloadCsv()
  else if (key === 'print') printPage()
  else if (key === 'close') closeMasterPage()
  else if (key === 'draw') processDrawOpen.value = true
  else if (props.masterKey === 'operation-master' && operationFeatureKeys.has(key as OperationFeature)) operationFeature.value = key as OperationFeature
  else if (props.masterKey === 'material-master' && materialFeatureKeys.has(key as MaterialFeature)) openMaterialFeature(key as MaterialFeature)
}

function openMaterialLookup(fieldKey: string): void {
  if (!isMaterialPreview.value) return
  materialLookupField.value = fieldKey
}

function chooseMaterialLookup(value: string): void {
  if (materialLookupField.value) form.value[materialLookupField.value] = value
  materialLookupField.value = null
}
const orderedGraphNodes = computed(() => [...graph.value.nodes].sort((left, right) => Number(left.sequence ?? 0) - Number(right.sequence ?? 0)))

function updateFormField(field: Field, event: Event): void {
  const target = event.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
  const value = target.value
  const options = optionsForField(field)
  const numeric = field.type === 'number' || field.type === 'quantity' || (field.type === 'select' && options.some((item) => typeof item.value === 'number'))
  const option = options.find((item) => String(item.value) === value)
  form.value[field.key] = numeric ? (value === '' ? null : Number(value)) : option && typeof option.value === 'boolean' ? option.value : value
}

function normalizeKeyField(key: string): void {
  if (['operationNo', 'materialNo', 'processNo', 'productNo'].includes(key)) {
    form.value[key] = String(form.value[key] ?? '').trim().toUpperCase()
  }
}

function updateCheckboxField(key: string, event: Event): void {
  form.value[key] = (event.target as HTMLInputElement).checked
}

function makeUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const resolvedBase = new URL(base, window.location.origin)
  return new URL(path.replace(/^\/+/, ''), resolvedBase.toString().replace(/\/+$/, '') + '/').toString()
}

async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeUrl(`api/${path}`), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { detail?: string; title?: string; message?: string } | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function rowPath(row: Row): string {
  const key = config.value.keyFields.map((field) => encodeURIComponent(String(row[field] ?? '')))
  if (props.masterKey === 'product-master') return `products/${key[0]}/versions/${key[1]}`
  return `${config.value.endpoint}/${key.join('/')}`
}

async function loadRows(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    if (isMaterialPreview.value) {
      const url = new URL(window.location.href)
      const hasAddressForm = url.searchParams.has('form')
      const wanted = url.searchParams.get('selected') || initialSelection
      const needle = keyword.value.trim().toUpperCase()
      rows.value = readDevMaterials().filter((row) => Number(row.issueState) === issueState.value
        && (!needle || String(row.materialNo ?? '').toUpperCase().includes(needle)))
      selected.value = rows.value.find((row) => selectedRouteKey(row) === wanted) ?? rows.value[0] ?? null
      initialSelection = ''
      loadVendorRows()
      if (!hasAddressForm) writeAddress()
      restoreFormFromAddress()
      return
    }
    const query = new URLSearchParams({ issueState: String(issueState.value) })
    if (keyword.value.trim()) query.set('keyword', keyword.value.trim())
    rows.value = await api<Row[]>(`${config.value.endpoint}?${query}`)
    const url = new URL(window.location.href)
    const hasAddressForm = url.searchParams.has('form')
    const wanted = url.searchParams.get('selected') || initialSelection
    selected.value = rows.value.find((row) => selectedRouteKey(row) === wanted) ?? rows.value[0] ?? null
    initialSelection = ''
    if (!hasAddressForm) writeAddress()
    await loadRelated()
    restoreFormFromAddress()
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : `${config.value.plural}資料載入失敗。`
  } finally {
    busy.value = false
  }
}

async function loadRelated(): Promise<void> {
  processes.value = []
  bom.value = []
  availableProcesses.value = []
  availableOperations.value = []
  availableMaterials.value = []
  availableProducts.value = []
  graph.value = { nodes: [], edges: [], dataStamp: 0 }
  if (!selected.value) return
  try {
    if (props.masterKey === 'product-master') {
      const base = rowPath(selected.value)
      const [p, b, processList, operationList, materialList, productList] = await Promise.all([
        api<Row[]>(`${base}/processes`), api<Row[]>(`${base}/bom`),
        api<Row[]>('processes?issueState=2'), api<Row[]>('operations?issueState=2'), api<Row[]>('materials?issueState=2'), api<Row[]>('products?issueState=2'),
      ])
      processes.value = p; bom.value = b; availableProcesses.value = processList; availableOperations.value = operationList; availableMaterials.value = materialList; availableProducts.value = productList
    }
    if (props.masterKey === 'process-master') {
      const [processGraph, operationList] = await Promise.all([api<{ nodes: Row[]; edges: Row[]; dataStamp: number }>(`${rowPath(selected.value)}/graph`), api<Row[]>('operations?issueState=2')])
      graph.value = processGraph; availableOperations.value = operationList
    }
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '關聯資料載入失敗。'
  }
}

function writeAddress(formQuery?: string): void {
  const url = new URL(window.location.href)
  url.searchParams.set('issueState', String(issueState.value))
  if (keyword.value.trim()) url.searchParams.set('keyword', keyword.value.trim()); else url.searchParams.delete('keyword')
  if (selected.value) url.searchParams.set('selected', selectedRouteKey(selected.value)); else url.searchParams.delete('selected')
  if (formQuery) url.searchParams.set('form', formQuery); else { url.searchParams.delete('form'); url.searchParams.delete('id'); url.searchParams.delete('version') }
  window.history.replaceState(window.history.state, '', url.pathname + url.search)
}

function selectMasterRow(row: Row): void {
  selected.value = row
  writeAddress()
  loadVendorRows()
  void loadRelated()
}

function defaults(): Row {
  const result: Row = {}
  for (const section of config.value.sections) for (const field of section.fields) {
    result[field.key] = field.type === 'checkbox' || field.type === 'radio' ? false : field.type === 'number' || field.type === 'quantity' ? 0 : ''
  }
  result.dataStamp = 0
  if (props.masterKey === 'operation-master') { result.countOperationUnitQty = 1; result.operationClass = 0; result.materialOption = 0; result.multiOperatorMode = 0 }
  if (props.masterKey === 'material-master') { result.countWay = 0; result.checkLotNo = 0; result.putInPlace = 0; result.safeQty = 0; result.erpNo = '' }
  if (props.masterKey === 'process-master') { result.processVersion = '01'; result.processClass = 0 }
  if (props.masterKey === 'product-master') { result.productVersion = '01'; result.lotStdQty = 0 }
  return result
}

function openForm(mode: 'new' | 'edit' | 'copy', row: Row | null = null): void {
  formMode.value = mode
  form.value = mode === 'new' ? defaults() : { ...row }
  if (mode === 'copy') {
    form.value.dataStamp = 0
    if (props.masterKey === 'operation-master') form.value.operationNo = ''
    if (props.masterKey === 'material-master') form.value.materialNo = ''
    if (props.masterKey === 'process-master') { form.value.processNo = ''; form.value.processVersion = '01' }
    if (props.masterKey === 'product-master') { form.value.productNo = ''; form.value.productVersion = '01' }
  }
  lotStdQtyEnabled.value = Number(form.value.lotStdQty ?? 0) > 0
  formOpen.value = true
  const id = row ? String(row[config.value.keyFields[0]] ?? '') : ''
  const version = row && config.value.keyFields[1] ? String(row[config.value.keyFields[1]] ?? '') : ''
  const url = new URL(window.location.href)
  url.searchParams.set('form', mode)
  if (id) url.searchParams.set('id', id)
  if (version) url.searchParams.set('version', version)
  window.history.pushState({ masterForm: true }, '', url.pathname + url.search)
}

function restoreFormFromAddress(): void {
  const url = new URL(window.location.href)
  const mode = url.searchParams.get('form')
  if (mode !== 'new' && mode !== 'edit' && mode !== 'copy') return
  const id = url.searchParams.get('id')
  const version = url.searchParams.get('version')
  const row = rows.value.find((item) => String(item[config.value.keyFields[0]]) === id && (!version || String(item[config.value.keyFields[1]]) === version)) ?? null
  formMode.value = mode
  form.value = mode === 'new' ? defaults() : { ...(row ?? defaults()) }
  if (mode === 'copy') {
    form.value.dataStamp = 0
    for (const key of config.value.keyFields) form.value[key] = key.endsWith('Version') ? '01' : ''
  }
  lotStdQtyEnabled.value = Number(form.value.lotStdQty ?? 0) > 0
  formOpen.value = true
}

function closeForm(fromHistory = false): void {
  formOpen.value = false
  if (!fromHistory) {
    const url = new URL(window.location.href)
    url.searchParams.delete('form'); url.searchParams.delete('id'); url.searchParams.delete('version')
    window.history.replaceState(window.history.state, '', url.pathname + url.search)
  }
}

function validateMasterForm(): boolean {
  const missingLookup = config.value.sections.flatMap((section) => section.fields).find((field) =>
    field.required && field.type === 'select' && !field.options?.length && !String(form.value[field.key] ?? '').trim())
  if (missingLookup) {
    error.value = missingLookup.label + '需要從系統選項中選取；目前 API 尚未提供選項清單。'
    return false
  }
  const key = String(form.value[config.value.keyFields[0]] ?? '').trim()
  if (props.masterKey === 'operation-master' && (key.includes(',') || key.toUpperCase() === 'LOTCREATE')) {
    error.value = '作業編號不可包含逗號，也不可使用 LOTCREATE。'
    return false
  }
  if (props.masterKey === 'process-master' && /[&<>]/.test(key)) {
    error.value = '流程編號不可包含 &、<、> 特殊字元。'
    return false
  }
  if (props.masterKey === 'material-master') {
    if (!String(form.value.materialName ?? '').trim() || !String(form.value.materialType ?? '').trim() || !String(form.value.unitNo ?? '').trim()) {
      error.value = '物料名稱、物料類別與單位編號為必填欄位。'
      return false
    }
    const safeQty = Number(form.value.safeQty ?? 0)
    if (!Number.isFinite(safeQty) || safeQty < 0) {
      error.value = '安全存量必須是大於或等於 0 的數值。'
      return false
    }
    if (form.value.openLimitTimeFlag) {
      const openLimitTime = Number(form.value.openLimitTime)
      if (!Number.isFinite(openLimitTime) || openLimitTime < 0) {
        error.value = '啟用開封時限管理時，開封時限必須是大於或等於 0 的數值。'
        return false
      }
    }
  }
  return true
}
async function save(): Promise<void> {
  if (!validateMasterForm()) return
  busy.value = true; error.value = ''
  try {
    const isEdit = formMode.value === 'edit'
    if (isMaterialPreview.value) {
      const items = readDevMaterials()
      const materialNo = String(form.value.materialNo ?? '').trim().toUpperCase()
      const oldMaterialNo = String(selected.value?.materialNo ?? '')
      const duplicate = items.some((item) => String(item.materialNo ?? '').toUpperCase() === materialNo
        && (!isEdit || String(item.materialNo ?? '').toUpperCase() !== oldMaterialNo.toUpperCase()))
      if (duplicate) throw new Error(`物料編號 ${materialNo} 已存在於 DEV 假資料。`)
      const old = isEdit ? items.find((item) => String(item.materialNo) === oldMaterialNo) : undefined
      const saved: Row = {
        ...form.value,
        materialNo,
        safeQty: Number(form.value.safeQty ?? 0),
        openLimitTime: form.value.openLimitTimeFlag ? Number(form.value.openLimitTime ?? 0) : 0,
        issueState: old?.issueState ?? 0,
        dataStamp: Number(old?.dataStamp ?? 0) + (old ? 1 : 0),
      }
      const next = old
        ? items.map((item) => String(item.materialNo) === oldMaterialNo ? saved : item)
        : [...items, saved]
      writeDevMaterials(next)
      issueState.value = Number(saved.issueState ?? 0)
      keyword.value = ''
      notice.value = `DEV 假資料：物料 ${materialNo} 已${isEdit ? '更新' : '建立'}。`
      closeForm()
      await loadRows()
      selected.value = rows.value.find((item) => String(item.materialNo) === materialNo) ?? saved
      loadVendorRows()
      return
    }
    const body = JSON.stringify(form.value)
    const method = isEdit ? 'PUT' : 'POST'
    const path = isEdit ? rowPath(form.value) : config.value.endpoint
    const saved = await api<Row>(path, { method, headers: { 'Content-Type': 'application/json' }, body })
    notice.value = `${config.value.plural} ${config.value.keyFields.map((key) => String(saved[key])).join('/')} 已${isEdit ? '更新' : '建立'}。`
    closeForm()
    await loadRows()
    selected.value = rows.value.find((row) => selectedRouteKey(row) === selectedRouteKey(saved)) ?? saved
    await loadRelated()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '資料儲存失敗。' }
  finally { busy.value = false }
}

async function command(action: 'approve' | 'delete' | 'new-version' | 'set-current'): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return
  if (action === 'delete' && !window.confirm(`確定${item.issueState === 2 ? '停用' : '刪除'}${config.value.plural} ${selectedRouteKey(item)}？`)) return
  busy.value = true; error.value = ''
  try {
    if (isMaterialPreview.value && (action === 'approve' || action === 'delete')) {
      const items = readDevMaterials()
      const materialNo = String(item.materialNo ?? '')
      const current = items.find((row) => String(row.materialNo) === materialNo)
      if (!current) throw new Error('找不到選取的 DEV 物料資料。')
      if (action === 'approve') {
        if (Number(current.issueState) !== 0) throw new Error('只有未簽核物料可以核准。')
        const propertyKey = `ymes:dev:material-properties:${encodeURIComponent(materialNo)}:v1`
        const propertyRows = JSON.parse(window.localStorage.getItem(propertyKey) || '[]') as Row[]
        const missingRequired = propertyRows.filter((row) => Number(row.controlled) === 1 && !String(row.value ?? row.defaultValue ?? '').trim())
        if (missingRequired.length) throw new Error(`必填屬性尚未設定：${missingRequired.map((row) => String(row.propertyNo ?? '')).join(', ')}`)
        const optionalWarnings = propertyRows.filter((row) => Number(row.controlled) === 2 && !String(row.value ?? row.defaultValue ?? '').trim())
        if (optionalWarnings.length && !window.confirm(`以下屬性未填，仍要核准嗎？ ${optionalWarnings.map((row) => String(row.propertyNo ?? '')).join(', ')}`)) return
        if (String(current.materialType).toUpperCase() === 'GUM') {
          const gumKey = `ymes:dev:material-gum-hour:${encodeURIComponent(materialNo)}:v1`
          if (!window.localStorage.getItem(gumKey)) throw new Error('固晶膠物料核准前必須先完成 GumHour 設定。')
        }
        if (!window.confirm(`確定核准物料 ${materialNo}？`)) return
        writeDevMaterials(items.map((row) => String(row.materialNo) === materialNo
          ? { ...row, issueState: 2, dataStamp: Number(row.dataStamp ?? 0) + 1 }
          : row))
        notice.value = `DEV 假資料：物料 ${materialNo} 已核准。`
      } else {
        if (![0, 2, -1].includes(Number(current.issueState))) throw new Error('此簽核狀態不允許刪除。')
        if (Number(current.issueState) === 2) {
          writeDevMaterials(items.map((row) => String(row.materialNo) === materialNo
            ? { ...row, issueState: -1, dataStamp: Number(row.dataStamp ?? 0) + 1 }
            : row))
          notice.value = `DEV 假資料：物料 ${materialNo} 已設為不使用。`
        } else {
          writeDevMaterials(items.filter((row) => String(row.materialNo) !== materialNo))
          notice.value = `DEV 假資料：物料 ${materialNo} 已刪除。`
        }
      }
      await loadRows()
      return
    }
    const suffix = action === 'approve' ? '/approve' : action === 'new-version' ? '/new-version' : action === 'set-current' ? '/set-current' : ''
    if (action === 'delete') {
      const query = new URLSearchParams({ issueState: String(item.issueState), dataStamp: String(item.dataStamp) })
      await api(`${rowPath(item)}?${query}`, { method: 'DELETE' })
    } else if (action === 'new-version') {
      const next = await api<Row>(`${rowPath(item)}${suffix}`, { method: 'POST' })
      notice.value = `已建立新版本 ${selectedRouteKey(next)}。`
    } else {
      const next = await api<Row>(`${rowPath(item)}${suffix}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: item.dataStamp }) })
      notice.value = action === 'approve' ? '資料已核准。' : '已設為目前版本。'
      selected.value = next
    }
    if (action === 'delete') notice.value = `${config.value.plural}已${item.issueState === 2 ? '停用' : '刪除'}。`
    await loadRows()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '操作失敗。' }
  finally { busy.value = false }
}

async function saveGraph(): Promise<void> {
  if (!selected.value) return
  if (unsupportedGraphNodeTypes.value.length) {
    error.value = '目前 API 僅接受 START、OP、END 節點。'
    return
  }
  busy.value = true
  try {
    graph.value = await api(`${rowPath(selected.value)}/graph`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(graph.value) })
    notice.value = '流程圖已儲存。'
    processDrawOpen.value = false
    await loadRows()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '流程圖儲存失敗。' }
  finally { busy.value = false }
}

async function addProductProcess(): Promise<void> {
  if (!selected.value) return
  busy.value = true
  try {
    await api(`${rowPath(selected.value)}/processes`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(processDraft.value) })
    notice.value = '產品流程已加入。'; await loadRelated()
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '產品流程新增失敗。' }
  finally { busy.value = false }
}

async function deleteProductProcess(item: Row): Promise<void> {
  if (!selected.value) return
  const query = new URLSearchParams({ moTypeNo: String(item.moTypeNo), productionSegmentNo: String(item.productionSegmentNo) })
  try { await api(`${rowPath(selected.value)}/processes/${encodeURIComponent(String(item.processNo))}/${encodeURIComponent(String(item.processVersionSelector))}?${query}`, { method: 'DELETE' }); await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : '產品流程移除失敗。' }
}

async function addBom(): Promise<void> {
  if (!selected.value) return
  busy.value = true
  try { await api(`${rowPath(selected.value)}/bom`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(bomDraft.value) }); notice.value = 'BOM 項目已加入。'; await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'BOM 新增失敗。' }
  finally { busy.value = false }
}

async function deleteBom(item: Row): Promise<void> {
  if (!selected.value) return
  try { await api(`${rowPath(selected.value)}/bom/${encodeURIComponent(String(item.id))}`, { method: 'DELETE' }); await loadRelated() }
  catch (reason) { error.value = reason instanceof Error ? reason.message : 'BOM 移除失敗。' }
}

function appendNode(): void {
  if (!newNode.value.nodeId || !newNode.value.nodeNo) return
  graph.value.nodes.push({ ...newNode.value })
  newNode.value = { nodeId: '', nodeNo: '', nodeType: 'OP', sequence: graph.value.nodes.length * 10, operationNo: '', description: '' }
}

function removeNode(nodeId: string): void {
  graph.value.nodes = graph.value.nodes.filter((node) => String(node.nodeId) !== nodeId)
  graph.value.edges = graph.value.edges.filter((edge) => String(edge.fromNodeId) !== nodeId && String(edge.toNodeId) !== nodeId)
}

function handlePopState(): void {
  const url = new URL(window.location.href)
  const mode = url.searchParams.get('form')
  if (!mode) { closeForm(true); return }
  const id = url.searchParams.get('id')
  const version = url.searchParams.get('version')
  const row = rows.value.find((item) => String(item[config.value.keyFields[0]]) === id && (!version || String(item[config.value.keyFields[1]]) === version))
  const normalizedMode = mode === 'edit' ? 'edit' : mode === 'copy' ? 'copy' : 'new'
  formMode.value = normalizedMode
  form.value = normalizedMode === 'new' ? defaults() : { ...(row ?? defaults()) }
  lotStdQtyEnabled.value = Number(form.value.lotStdQty ?? 0) > 0
  formOpen.value = true
}

function printPage(): void { window.print() }

function downloadCsv(): void {
  const headings = config.value.columns.map((column) => column.label)
  const body = rows.value.map((row) => config.value.columns.map((column) => String(row[column.key] ?? '').replaceAll('"', '""')))
  const csv = [headings, ...body].map((line) => line.map((value) => `"${value}"`).join(',')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a'); link.href = url; link.download = `${config.value.endpoint}.csv`; link.click(); URL.revokeObjectURL(url)
}

onMounted(() => {
  const url = new URL(window.location.href)
  issueState.value = Number(url.searchParams.get('issueState') ?? 0)
  keyword.value = url.searchParams.get('keyword') ?? ''
  initialSelection = url.searchParams.get('selected') ?? ''
  void loadRows()
  window.addEventListener('popstate', handlePopState)
})
onBeforeUnmount(() => window.removeEventListener('popstate', handlePopState))
watch(() => props.masterKey, () => { selected.value = null; void loadRows() })
</script>

<template>
  <div class="md-master" :class="'md-' + masterKey">
    <form class="md-query" @submit.prevent="loadRows">
      <fieldset class="md-state-filter">
        <legend>{{ config.plural }}</legend>
        <label v-for="state in [{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]" :key="state.value">
          <input v-model.number="issueState" type="radio" name="issueState" :value="state.value" />{{ state.label }}
        </label>
      </fieldset>
      <label class="md-search"><select :aria-label="config.plural + '查詢欄位'"><option :value="searchField.key">{{ searchField.label }}</option></select><input v-model="keyword" type="search" :aria-label="searchField.label + '查詢值'" /></label><span>%</span>
      <button type="submit" :disabled="busy">查詢</button>
    </form>
    <p v-if="error" class="md-message error" role="alert">{{ error }}</p><p v-if="notice" class="md-message" role="status">{{ notice }}</p>
    <p v-if="isMaterialPreview" class="md-message md-dev-note">開發區本機假資料：只存於目前瀏覽器，不會寫入 MES 或 YMES 資料庫。</p>

    <section class="md-screen" :class="{ 'md-screen-process': masterKey === 'process-master', 'md-screen-product': masterKey === 'product-master' }">
      <div class="md-list-panel">
        <header class="md-panel-heading"><h2>{{ config.plural }}</h2><span>{{ config.title }}</span></header>
        <div class="md-table-scroll"><table>
          <thead><tr><th v-for="col in config.columns" :key="col.key">{{ col.label }}</th><th>狀態</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="selectedRouteKey(row)" :class="{ selected: selectedKey === selectedRouteKey(row) }" :aria-selected="selectedKey === selectedRouteKey(row)" tabindex="0"
                @click="selectMasterRow(row)" @keydown.enter.prevent="selectMasterRow(row)" @keydown.space.prevent="selectMasterRow(row)"
                @dblclick="row.issueState === 0 && openForm('edit', row)">
              <td v-for="col in config.columns" :key="col.key">{{ displayValue(row[col.key]) }}</td><td>{{ stateLabel(row.issueState) }}</td>
            </tr>
            <tr v-if="!busy && rows.length === 0"><td :colspan="config.columns.length + 1" class="empty">沒有符合條件的{{ config.plural }}資料</td></tr>
            <tr v-if="busy && rows.length === 0"><td :colspan="config.columns.length + 1" class="empty">資料載入中…</td></tr>
          </tbody>
        </table></div>
        <footer>資料總筆數 <b>{{ rows.length }}</b></footer>
      </div>

      <section v-if="masterKey === 'process-master'" class="md-process-preview">
        <header class="md-panel-heading">
          <h2>流程圖</h2>
          <span v-if="selected">{{ selected.processNo }} / {{ selected.processVersion }}</span><span v-else>請先選取流程</span>
          <button class="md-button" type="button" :disabled="actionDisabled('draw')" @click="runToolbarAction('draw')">Draw</button>
        </header>
        <div v-if="selected" class="md-process-canvas" aria-label="依工序排列的流程預覽">
          <template v-if="orderedGraphNodes.length">
            <div v-for="(node,index) in orderedGraphNodes" :key="String(node.nodeId)" class="md-flow-step">
              <article class="md-flow-node" :class="'node-' + String(node.nodeType).toLowerCase()"><small>{{ node.nodeType }}</small><strong>{{ node.nodeNo }}</strong><span v-if="node.operationNo">{{ node.operationNo }}</span><span v-if="node.description">{{ node.description }}</span></article>
              <span v-if="index < orderedGraphNodes.length - 1" class="md-flow-arrow" aria-hidden="true">→</span>
            </div>
          </template><p v-else class="md-panel-empty">此流程尚未建立節點。</p>
        </div>
        <p v-else class="md-panel-empty">選取流程後顯示節點與連線。</p>
      </section>

      <aside class="md-actions" :aria-label="config.title + '功能'">
        <button v-for="action in toolbarActions" :key="action.key" class="md-button" :class="{ primary: action.key === 'add', danger: action.key === 'delete' }" type="button"
                :disabled="actionDisabled(action.key)"
                :title="actionDisabled(action.key) && !supportedToolbarActions.has(action.key) ? '此功能的 API 尚未提供' : actionDisabled(action.key) && masterKey==='operation-master' && operationFeatureKeys.has(action.key as OperationFeature) ? (developmentPreview ? '請先選取作業站' : '正式 API 尚未提供') : actionDisabled(action.key) && masterKey==='material-master' && materialFeatureKeys.has(action.key as MaterialFeature) ? (developmentPreview ? (action.key==='gum-hour' ? '僅固晶膠物料可設定 GumHour' : action.key==='import' ? '' : '請先選取物料') : '正式 API 尚未提供') : ''"
                @click="runToolbarAction(action.key)">{{ action.label }}</button>
      </aside>

      <section v-if="masterKey === 'material-master'" class="md-vendor-panel">
        <header class="md-panel-heading"><h2>供應商資料</h2><span>{{ selected ? selected.materialNo : '請先選取物料' }}</span></header>
        <div class="md-vendor-actions"><button class="md-button" type="button" :disabled="!isMaterialPreview || !selected" :title="isMaterialPreview ? '' : '物料供應商 API 尚未提供'" @click="openMaterialFeature('vendor-map')">新增</button><button class="md-button" type="button" :disabled="!isMaterialPreview || !selectedVendor" :title="isMaterialPreview ? '' : '物料供應商 API 尚未提供'" @click="openMaterialFeature('vendor-map','edit',selectedVendor)">修改</button><button class="md-button" type="button" :disabled="!isMaterialPreview || !selectedVendor" :title="isMaterialPreview ? '' : '物料供應商 API 尚未提供'" @click="deleteMaterialVendor">刪除</button><button class="md-button" type="button" @click="closeMasterPage">關閉</button></div>
        <div class="md-grid-scroll"><table><thead><tr><th>供應商編號</th><th>供應商名稱</th><th>說明</th></tr></thead><tbody><tr v-for="vendor in vendorRows" :key="String(vendor.vendorNo)" :class="{selected:selectedVendor?.vendorNo===vendor.vendorNo}" @click="selectedVendor=vendor"><td>{{ vendor.vendorNo }}</td><td>{{ vendor.vendorName }}</td><td>{{ vendor.description }}</td></tr><tr v-if="vendorRows.length===0"><td colspan="3" class="empty">{{ isMaterialPreview ? '此物料尚無供應商對應。' : '物料供應商資料 API 尚未提供。' }}</td></tr></tbody></table></div>
      </section>

      <section v-if="masterKey === 'product-master'" class="md-product-relations">
        <nav class="md-tabs" aria-label="產品關聯資料">
          <button type="button" :class="{active:relationTab==='processes'}" @click="relationTab='processes'">流程</button><button type="button" :class="{active:relationTab==='bom'}" @click="relationTab='bom'">BOM</button><button type="button" :class="{active:relationTab==='equipment'}" @click="relationTab='equipment'">設備限制</button>
        </nav>
        <section v-if="relationTab==='processes'" class="md-relation-panel">
          <header class="md-panel-heading"><h2>產品流程</h2><span v-if="selected">{{ selected.productNo }} / {{ selected.productVersion }}</span><div class="md-relation-toolbar"><button class="md-button" type="button" disabled title="流程階層 API 尚未提供">Expand All</button><button class="md-button" type="button" disabled title="產品流程設定 API 尚未提供">Set ...</button></div></header>
          <div class="md-grid-scroll"><table><thead><tr><th>工單類別</th><th>製程段</th><th>工序</th><th>流程編號</th><th>版本選擇</th><th>實際版本</th><th>預設流程</th><th>有組件</th><th>有階層</th><th>操作</th></tr></thead>
            <tbody><tr v-for="item in processes" :key="String(item.moTypeNo)+'|'+String(item.productionSegmentNo)+'|'+String(item.processNo)+'|'+String(item.processVersionSelector)">
              <td>{{ item.moTypeNo }}</td><td>{{ item.productionSegmentNo }}</td><td>{{ item.segmentOrder }}</td><td>{{ item.processNo }}</td><td>{{ item.processVersionSelector }}</td><td>{{ item.resolvedProcessVersion }}</td><td>{{ displayValue(item.defaultProcess) }}</td><td>{{ displayValue(item.haveComponent) }}</td><td>{{ displayValue(item.haveLevel) }}</td><td><button class="md-button" type="button" :disabled="!isEditable || busy" @click="deleteProductProcess(item)">移除</button></td>
            </tr><tr v-if="processes.length===0"><td colspan="10" class="empty">此產品尚未設定流程。</td></tr></tbody>
          </table></div>
          <form v-if="selected && isEditable" class="md-relation-form" @submit.prevent="addProductProcess">
            <label>工單類別<input v-model.number="processDraft.moTypeNo" type="number" min="0" required /></label><label>製程段<input v-model="processDraft.productionSegmentNo" required /></label>
            <label>流程編號<select v-model="processDraft.processNo" required><option value="">選擇流程</option><option v-for="item in availableProcesses" :key="String(item.processNo)+'|'+String(item.processVersion)" :value="item.processNo">{{ item.processNo }} / {{ item.processVersion }}</option></select></label>
            <label>流程版本（* 為目前版）<input v-model="processDraft.processVersionSelector" required maxlength="10" /></label><label class="md-check-field"><input v-model="processDraft.defaultProcess" type="checkbox" />預設流程</label><button class="md-button primary" type="submit" :disabled="busy">新增流程</button>
          </form>
        </section>
        <section v-else-if="relationTab==='bom'" class="md-relation-panel">
          <header class="md-panel-heading"><h2>產品 BOM</h2><span v-if="selected">{{ selected.productNo }} / {{ selected.productVersion }}</span><div class="md-relation-toolbar"><button class="md-button" type="button" @click="printPage">Print</button><button class="md-button" type="button" disabled title="BOM 設定 API 尚未提供">Set ...</button></div></header>
          <div class="md-grid-scroll"><table><thead><tr><th>類型</th><th>元件編號</th><th>投入作業</th><th>標準用量</th><th>投料位置</th><th>指定</th><th>損耗率</th><th>說明</th><th>操作</th></tr></thead>
            <tbody><tr v-for="item in bom" :key="String(item.id)"><td>{{ item.componentType }}</td><td>{{ item.componentNo }}</td><td>{{ item.inputOperationNo }}</td><td>{{ item.standardQty }}</td><td>{{ item.putInPlaceType }}</td><td>{{ displayValue(item.specified) }}</td><td>{{ item.decreaseRate }}</td><td>{{ item.description }}</td><td><button class="md-button" type="button" :disabled="!isEditable || busy" @click="deleteBom(item)">移除</button></td></tr><tr v-if="bom.length===0"><td colspan="9" class="empty">此產品尚未設定 BOM。</td></tr></tbody>
          </table></div>
          <form v-if="selected && isEditable" class="md-relation-form" @submit.prevent="addBom">
            <label>元件類型<select v-model="bomDraft.componentType"><option value="MATERIAL">物料</option><option value="PRODUCT">產品</option></select></label>
            <label>元件編號<select v-model="bomDraft.componentNo" required><option value="">選擇元件</option><template v-if="bomDraft.componentType==='MATERIAL'"><option v-for="item in availableMaterials" :key="String(item.materialNo)" :value="item.materialNo">{{ item.materialNo }} {{ item.materialName }}</option></template><template v-else><option v-for="item in availableProducts" :key="String(item.productNo)" :value="item.productNo">{{ item.productNo }} {{ item.productName }}</option></template></select></label>
            <label>投入作業<select v-model="bomDraft.inputOperationNo" required><option value="">選擇作業</option><option v-for="item in availableOperations" :key="String(item.operationNo)" :value="item.operationNo">{{ item.operationNo }} {{ item.operationName }}</option></select></label>
            <label>標準用量<input v-model.number="bomDraft.standardQty" type="number" min="0.000001" step="0.001" required /></label><label>投料位置<input v-model="bomDraft.putInPlaceType" /></label>
            <label class="md-check-field"><input v-model="bomDraft.specified" type="checkbox" />指定用料</label><label>損耗率 %<input v-model.number="bomDraft.decreaseRate" type="number" min="0" max="100" step="0.1" /></label><label class="md-relation-description">說明<input v-model="bomDraft.description" /></label>
            <button class="md-button primary" type="submit" :disabled="busy">新增 BOM</button>
          </form>
        </section>
        <section v-else class="md-relation-panel md-api-unavailable"><header class="md-panel-heading"><h2>EQPConstraint</h2><span class="md-relation-toolbar"><button class="md-button" type="button" disabled title="設備限制 API 尚未提供">新增</button><button class="md-button" type="button" disabled title="設備限制 API 尚未提供">刪除</button></span></header><p>WinForms 有設備限制設定頁；目前獨立 API 尚未提供此資料，因此保留分頁並清楚標示未接通。</p></section>
      </section>
    </section>

    <div v-if="processDrawOpen && masterKey==='process-master' && selected" class="md-modal-backdrop" @click.self="processDrawOpen=false">
      <section class="md-form-dialog md-process-dialog" role="dialog" aria-modal="true" aria-labelledby="process-draw-title">
        <header><div><small>流程圖定義</small><h2 id="process-draw-title">{{ selected.processNo }} / {{ selected.processVersion }}</h2></div><button type="button" aria-label="關閉" @click="processDrawOpen=false">×</button></header>
        <div class="md-form-scroll">
          <section class="md-graph-editor">
            <div class="md-section-heading"><h3>節點</h3></div>
            <p v-if="unsupportedGraphNodeTypes.length" class="md-message error">目前 API 僅接受 START、OP、END。先移除未支援的 WinForms 節點後才能儲存。</p>
            <form class="md-inline-form" @submit.prevent="appendNode">
              <label>Node ID<input v-model="newNode.nodeId" required placeholder="例：op-10" /></label><label>節點編號<input v-model="newNode.nodeNo" required /></label>
              <label>類型<select v-model="newNode.nodeType"><option value="START">START</option><option value="OP">作業</option><option value="PROCESS" disabled>流程（API 尚未支援）</option><option value="MODULE" disabled>模組（API 尚未支援）</option><option value="AND_Group" disabled>AND 群組（API 尚未支援）</option><option value="OR_Group" disabled>OR 群組（API 尚未支援）</option><option value="END">END</option></select></label>
              <label>順序<input v-model.number="newNode.sequence" type="number" min="0" step="10" /></label>
              <label>作業編號<select v-model="newNode.operationNo"><option value="">不指定</option><option v-for="item in availableOperations" :key="String(item.operationNo)" :value="item.operationNo">{{ item.operationNo }} {{ item.operationName }}</option></select></label>
              <label>說明<input v-model="newNode.description" /></label><button class="md-button" type="submit" :disabled="!isEditable || busy">新增節點</button>
            </form>
            <div class="md-grid-scroll"><table><thead><tr><th>節點編號</th><th>類型</th><th>順序</th><th>作業</th><th>說明</th><th>操作</th></tr></thead>
              <tbody><tr v-for="node in orderedGraphNodes" :key="String(node.nodeId)"><td>{{ node.nodeNo }}</td><td>{{ node.nodeType }}</td><td>{{ node.sequence }}</td><td>{{ node.operationNo }}</td><td>{{ node.description }}</td><td><button class="md-button" type="button" :disabled="!isEditable" @click="removeNode(String(node.nodeId))">刪除</button></td></tr></tbody>
            </table></div>
            <div class="md-section-heading"><h3>連線</h3></div>
            <form class="md-inline-form" @submit.prevent="graph.edges.push({...newEdge}); newEdge={fromNodeId:'',toNodeId:'',action:'pass'}">
              <label>起始節點<select v-model="newEdge.fromNodeId" required><option value="">選擇</option><option v-for="node in graph.nodes" :key="String(node.nodeId)" :value="node.nodeId">{{ node.nodeNo }}</option></select></label>
              <label>目標節點<select v-model="newEdge.toNodeId" required><option value="">選擇</option><option v-for="node in graph.nodes" :key="String(node.nodeId)" :value="node.nodeId">{{ node.nodeNo }}</option></select></label>
              <label>通過條件<input v-model="newEdge.action" placeholder="pass" /></label><button class="md-button" type="submit" :disabled="!isEditable || busy">新增連線</button>
            </form>
            <div class="md-grid-scroll"><table><thead><tr><th>起始節點</th><th>目標節點</th><th>條件</th><th>操作</th></tr></thead>
              <tbody><tr v-for="(edge,index) in graph.edges" :key="index"><td>{{ edge.fromNodeId }}</td><td>{{ edge.toNodeId }}</td><td>{{ edge.action }}</td><td><button class="md-button" type="button" :disabled="!isEditable" @click="graph.edges.splice(index,1)">刪除</button></td></tr><tr v-if="graph.edges.length===0"><td colspan="4" class="empty">尚無連線</td></tr></tbody>
            </table></div>
          </section>
        </div>
        <footer><button class="md-button primary" type="button" :disabled="!isEditable || busy || unsupportedGraphNodeTypes.length > 0" @click="saveGraph">{{ busy ? '儲存中…' : '確定' }}</button><button class="md-button" type="button" @click="processDrawOpen=false">關閉</button></footer>
      </section>
    </div>

    <OperationFeatureDialog v-if="operationFeature && masterKey==='operation-master'" :feature="operationFeature" :operation-no="String(selected?.operationNo ?? '')" :operation-name="String(selected?.operationName ?? '')" @close="operationFeature=null" @saved="notice='作業站子功能的 DEV 假資料已更新。'" />
    <MaterialFeatureDialog v-if="materialFeature && masterKey==='material-master'" :feature="materialFeature.feature" :material="selected" :mode="materialFeature.mode" :vendor="materialFeature.vendor" @close="materialFeature=null" @saved="handleMaterialFeatureSaved" />

    <div v-if="materialLookupField && masterKey==='material-master'" class="md-modal-backdrop" @click.self="materialLookupField=null">
      <section class="md-form-dialog md-lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="material-lookup-title">
        <header><div><small>開發區假資料選項</small><h2 id="material-lookup-title">{{ materialLookupTitle(materialLookupField) }}</h2></div><button type="button" aria-label="關閉" @click="materialLookupField=null">×</button></header>
        <div class="md-form-scroll"><div class="md-lookup-list"><button v-for="option in materialLookupOptions" :key="option.value" type="button" class="md-button" @click="chooseMaterialLookup(option.value)">{{ option.label }}</button></div><p class="md-field-note">這些選項僅供開發畫面操作；正式 ValueList 與查詢 API 尚未對接。</p></div>
        <footer><button class="md-button" type="button" @click="materialLookupField=null">關閉</button></footer>
      </section>
    </div>

    <div v-if="formOpen" class="md-modal-backdrop" @click.self="closeForm()">
      <form class="md-form-dialog" @submit.prevent="save">
        <header><div><small>{{ config.title }}定義（{{ formMode === 'edit' ? 'EDIT' : formMode === 'copy' ? 'COPY' : 'ADD' }}）</small><h2>{{ config.plural }}基本資料</h2></div><button type="button" aria-label="關閉" @click="closeForm()">×</button></header>
        <div class="md-form-scroll">
          <fieldset v-for="section in config.sections" :key="section.title" class="md-form-section"><legend>{{ section.title }}</legend><div class="md-form-grid">
            <div v-for="field in section.fields" :key="field.key" class="md-form-field" :class="{wide:field.wide, checkbox:field.type==='checkbox' || field.type==='radio', yellow:field.yellow}">
              <span :id="'md-label-' + field.key">{{ field.label }}<i v-if="field.required">*</i></span>
              <input v-if="field.type==='checkbox'" :aria-labelledby="'md-label-' + field.key" type="checkbox" :checked="Boolean(form[field.key])" @change="updateCheckboxField(field.key,$event)" />
              <div v-else-if="field.type==='radio'" class="md-radio-options"><label v-for="option in field.options || []" :key="String(option.value)"><input type="radio" :name="field.key" :value="String(option.value)" :checked="String(form[field.key]) === String(option.value)" @change="updateFormField(field,$event)" />{{ option.label }}</label></div>
              <select v-else-if="field.type==='select'" :aria-labelledby="'md-label-' + field.key" :value="fieldValue(field)" :required="field.required" :disabled="!hasFieldOptions(field)" @change="updateFormField(field,$event)"><option v-if="!hasFieldOptions(field)" :value="fieldValue(field)">{{ fieldValue(field) || '系統選項 API 尚未提供' }}</option><option v-for="option in optionsForField(field)" :key="String(option.value)" :value="option.value">{{ option.label }}</option></select>
              <div v-else-if="field.type==='quantity'" class="md-quantity-field"><div class="md-radio-options"><label><input type="radio" name="lotStdQtyMode" :checked="!lotStdQtyEnabled" @change="setLotStdQtyEnabled(false)" />Not Set</label><label><input type="radio" name="lotStdQtyMode" :checked="lotStdQtyEnabled" @change="setLotStdQtyEnabled(true)" />Std Qty</label></div><input :value="fieldValue(field)" type="number" :step="field.step" :min="field.min" :disabled="!lotStdQtyEnabled" @input="updateFormField(field,$event)" /></div>
              <textarea v-else-if="field.type==='textarea'" :aria-labelledby="'md-label-' + field.key" :value="fieldValue(field)" rows="3" :required="field.required" @input="updateFormField(field,$event)" />
              <div v-else class="md-field-control"><input :aria-labelledby="'md-label-' + field.key" :value="fieldValue(field)" :type="field.type || 'text'" :step="field.step" :min="field.min" :max="field.max" :required="field.required" :readonly="(field.readonly && !(isMaterialPreview && ['materialType','unitType','unitNo'].includes(field.key))) || (formMode==='edit' && config.keyFields.includes(field.key))" @input="updateFormField(field,$event)" @blur="normalizeKeyField(field.key)" /><button v-if="field.lookup" type="button" :disabled="!isMaterialPreview" :title="isMaterialPreview ? '選擇開發區假資料' : '查詢 API 尚未提供'" @click="openMaterialLookup(field.key)">…</button><small v-if="field.lookup">{{ isMaterialPreview ? 'DEV 選項' : '查詢 API 尚未提供' }}</small></div>
              <small v-if="field.type==='select' && !hasFieldOptions(field)" class="md-field-note">ValueList API 尚未提供</small>
            </div>
          </div></fieldset>
          <fieldset v-if="masterKey==='product-master'" class="md-form-section"><legend>Detail Source</legend><div class="md-copy-source"><label><input type="radio" disabled />Copy from ProductType</label><label><input type="radio" :checked="formMode==='copy'" disabled />Copy from Other Product</label><label><input type="radio" :checked="formMode!=='copy'" disabled />None</label><label>ProductNo<input readonly :value="selected ? String(selected.productNo) : '—'" /></label><label>ProductVersion<input readonly :value="selected ? String(selected.productVersion) : '—'" /><button type="button" disabled title="產品複製 API 尚未提供">…</button></label><small>目前 API 尚未提供關聯明細複製功能。</small></div></fieldset>
          <fieldset v-if="masterKey==='product-master'" class="md-form-section"><legend>產品圖片</legend><div class="md-product-picture"><label>PictureName<div class="md-field-control"><input readonly value="API 尚未提供" /><button type="button" disabled title="產品圖片 API 尚未提供">…</button></div></label><div class="md-picture-preview">產品圖片預覽<br />圖片 API 尚未提供</div></div></fieldset>
          <div class="md-audit"><label>建立人<input readonly value="—" /></label><label>建立日期<input readonly :value="form.createdAt ? String(form.createdAt) : '—'" /></label><label>修改人<input readonly value="—" /></label><label>修改日期<input readonly :value="form.revisedAt ? String(form.revisedAt) : '—'" /></label></div>
        </div>
        <footer><button class="md-button primary" type="submit" :disabled="busy">{{ busy ? '儲存中…' : '確定' }}</button><button class="md-button" type="button" @click="closeForm()">關閉</button><button class="md-button" type="button" disabled>套用</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.md-master{min-width:0;padding:10px;background:#fff;color:var(--ink);font:12px 'Segoe UI','Microsoft JhengHei',sans-serif}
.md-query{display:flex;align-items:center;gap:10px;flex-wrap:wrap;margin-bottom:8px;padding:7px 9px;border:1px solid var(--line);background:#fff}
.md-state-filter{display:flex;align-items:center;gap:12px;margin:0;padding:0 4px;border:0}
.md-state-filter legend{position:absolute;clip:rect(0 0 0 0)}
.md-state-filter label,.md-search,.md-query button{display:inline-flex;align-items:center;gap:5px}
.md-state-filter input{accent-color:#a58d00}
.md-search input{height:28px;width:190px;border:1px solid #aaa58f;padding:4px 7px;background:#fff}.md-search select{height:28px;max-width:210px;border:1px solid #aaa58f;padding:4px 7px;background:#fff;color:var(--ink);font:inherit}
.md-search input:focus,.md-form-grid input:focus,.md-form-grid textarea:focus,.md-form-grid select:focus{outline:2px solid rgb(181 154 0 / 22%);border-color:#a58d00}
.md-query>button,.md-button{min-height:29px;padding:5px 10px;border:1px solid #c9c4b7;border-radius:3px;background:#fff;color:var(--ink);font:inherit}
.md-button:hover:not(:disabled){border-color:#a58d00;background:#fffbea}
.md-button.primary{border-color:#3b3930;background:#3b3930;color:#fff}
.md-button.primary:hover:not(:disabled){background:#514e44}
.md-button.danger{color:#8a3329}
.md-button:disabled{cursor:not-allowed;opacity:.52}
.md-message{margin:5px 0;padding:7px 10px;border:1px solid #bfc7b7;background:#f4f7ef;color:#3e5033}
.md-message.error{border-color:#d6aaa3;background:#fff4f1;color:#8a3329}
.md-screen{display:grid;grid-template-columns:minmax(0,1fr) 132px;gap:8px;align-items:start}
.md-screen-process{grid-template-columns:minmax(0,1fr) 132px}
.md-list-panel,.md-process-preview,.md-vendor-panel,.md-product-relations{min-width:0;border:1px solid var(--line);padding:6px;background:#fff}
.md-panel-heading{display:flex;align-items:center;justify-content:space-between;gap:10px;min-height:28px;padding:2px 4px 6px}
.md-panel-heading h2,.md-api-unavailable h2{margin:0;font-size:14px;font-weight:600}
.md-panel-heading span{color:var(--muted)}
.md-table-scroll,.md-grid-scroll{min-width:0;max-height:55vh;overflow:auto;border:1px solid #d3cfc3}
.md-master table{width:100%;border-collapse:collapse;white-space:nowrap}
.md-master th,.md-master td{padding:5px 8px;border:1px solid #d5d1c6;text-align:left}
.md-master th{position:sticky;top:0;z-index:1;background:#f1ead0;color:#26251f;font-weight:600}
.md-master tbody tr{cursor:pointer}
.md-master tbody tr:hover{background:#fff8de}
.md-master tbody tr.selected{background:#ffedaa}
.md-master .empty{text-align:center;padding:17px;color:var(--muted)}
.md-list-panel footer{padding:7px 3px;color:var(--muted)}
.md-actions{display:flex;max-height:76vh;flex-direction:column;gap:5px;overflow:auto;padding:5px;border:1px solid var(--line);background:#fff}
.md-actions .md-button{text-align:center}
.md-vendor-panel{grid-column:1/-1}
.md-product-relations{grid-column:1}
.md-screen-process .md-process-preview{grid-column:1}
.md-screen-process .md-actions,.md-screen-product .md-actions{grid-column:2;grid-row:1 / span 2}
.md-vendor-actions,.md-relation-toolbar{display:flex;align-items:center;gap:5px;margin:2px 0 6px}
.md-relation-toolbar{margin:0}
.md-panel-empty{padding:22px;color:var(--muted);text-align:center}
.md-process-canvas{display:flex;min-height:96px;align-items:center;gap:8px;overflow:auto;padding:8px;border:1px solid var(--line);background:#faf9f5}
.md-flow-step{display:flex;flex:0 0 auto;align-items:center;gap:8px}
.md-flow-node{display:grid;min-width:110px;max-width:180px;gap:4px;padding:9px 10px;border:1px solid #bcb7a9;border-radius:4px;background:#fff;color:var(--ink)}
.md-flow-node small{color:var(--muted);font-size:10px;text-transform:uppercase}
.md-flow-node strong{font-size:12px}
.md-flow-node span{overflow:hidden;color:#686458;text-overflow:ellipsis}
.md-flow-node.node-start,.md-flow-node.node-end{border-color:#b59a00;background:#fff9dc}
.md-flow-node.node-and,.md-flow-node.node-or{border-style:dashed}
.md-flow-arrow{color:#857300;font-size:20px}
.md-graph-editor{display:grid;gap:6px;margin-top:10px}
.md-section-heading{display:flex;align-items:center;justify-content:space-between;gap:8px}
.md-section-heading h3{margin:5px 0;font-size:13px}
.md-inline-form,.md-relation-form{display:flex;align-items:end;gap:7px;flex-wrap:wrap;padding:7px;background:#faf9f5;border:1px solid var(--line)}
.md-inline-form label,.md-relation-form label{display:grid;min-width:100px;gap:3px;color:#5f5b50}
.md-inline-form input,.md-inline-form select,.md-relation-form input,.md-relation-form select{height:29px;max-width:220px;min-width:110px;padding:4px 6px;border:1px solid #bcb7a9;background:#fff;color:var(--ink);font:inherit}
.md-tabs{display:flex;gap:2px;border-bottom:1px solid var(--line)}
.md-tabs button{min-height:34px;padding:7px 16px;border:1px solid transparent;border-bottom:0;background:transparent;color:var(--muted);font:inherit}
.md-tabs button.active{border-color:var(--line);background:#fff;color:var(--ink);font-weight:600;box-shadow:inset 0 -2px #e7c600}
.md-relation-panel{padding:9px 4px 4px}
.md-relation-panel .md-grid-scroll{max-height:35vh}
.md-relation-form{margin-top:8px}
.md-relation-description{flex:1}
.md-check-field{display:flex!important;align-items:center;min-height:29px}
.md-api-unavailable{min-height:150px;padding:24px}
.md-api-unavailable p{color:var(--muted)}
.md-modal-backdrop{position:fixed;inset:0;z-index:20;display:grid;place-items:center;padding:18px;background:rgb(30 29 25 / 42%)}
.md-form-dialog{display:grid;width:min(980px,100%);max-height:92vh;grid-template-rows:auto minmax(0,1fr) auto;background:#fff;border:1px solid #bdb8aa;box-shadow:0 12px 40px rgb(0 0 0 / 28%)}
.md-process-dialog{width:min(1180px,100%)}
.md-form-dialog>header{display:flex;align-items:center;justify-content:space-between;padding:12px 18px;border-bottom:1px solid var(--line)}
.md-form-dialog h2{margin:3px 0;font-size:19px}
.md-form-dialog>header small{color:var(--muted)}
.md-form-dialog>header button{width:32px;height:32px;border:1px solid var(--line);background:#faf9f5;color:var(--ink);font-size:20px}
.md-form-scroll{overflow:auto;padding:12px 18px}
.md-form-section{margin:0 0 12px;padding:10px 14px 14px;border:1px solid #c6c1b4}
.md-form-section legend{padding:0 5px;color:var(--ink);font-size:14px;font-weight:600}
.md-form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px 24px}
.md-form-grid .md-form-field{display:grid;grid-template-columns:minmax(115px,36%) minmax(0,1fr);align-items:center;gap:8px}
.md-form-grid .md-form-field.wide{grid-column:1/-1}
.md-form-grid .md-form-field.checkbox{display:flex;align-items:center}
.md-form-grid .md-form-field>span{color:#4d4a41}
.md-form-grid .md-form-field i{margin-left:3px;color:#9b3d30;font-style:normal}
.md-form-grid input:not([type=checkbox]),.md-form-grid textarea,.md-form-grid select{width:100%;min-height:30px;padding:4px 7px;border:1px solid #aaa58f;background:#fff;color:var(--ink);font:inherit}
.md-form-grid .md-form-field.yellow input:not([type=checkbox]),.md-form-grid .md-form-field.yellow textarea,.md-form-grid .md-form-field.yellow select{background:#fff1aa}
.md-form-grid input[readonly]{background:#eee!important;color:#6d695f}
.md-form-grid input[type=checkbox],.md-radio-options input[type=radio]{width:15px;height:15px;accent-color:#a58d00}.md-radio-options{display:flex;align-items:center;gap:12px;flex-wrap:wrap}.md-radio-options label{display:flex;grid-template-columns:none;align-items:center;gap:5px}.md-quantity-field{display:grid;gap:6px}.md-field-note,.md-field-control small{grid-column:1/-1;color:var(--muted);font-size:10px}.md-copy-source{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px}.md-copy-source label{display:flex;align-items:center;gap:6px}.md-copy-source label:last-of-type{grid-column:1/-1}.md-copy-source>small{grid-column:1/-1;color:var(--muted)}.md-copy-source button,.md-product-picture button{height:30px;width:30px;border:1px solid #c9c4b7;background:#f8f7f2}.md-product-picture{display:grid;grid-template-columns:minmax(240px,1fr) 270px;gap:14px;align-items:start}.md-product-picture>label{display:grid;gap:7px}.md-picture-preview{display:grid;min-height:145px;place-content:center;border:1px solid #c7c2b7;background:#faf9f5;color:var(--muted);text-align:center}
.md-field-control{display:flex;min-width:0;gap:4px}
.md-field-control input{min-width:0;flex:1}
.md-field-control button{width:30px;border:1px solid #c9c4b7;background:#f8f7f2;color:#555;font:inherit}
.md-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:10px;border:1px solid var(--line)}
.md-audit label{display:grid;grid-template-columns:auto minmax(0,1fr);align-items:center;gap:5px}
.md-audit input{min-width:0;height:28px;border:1px solid #dedacf;background:#f1f0ec;padding:4px;font:inherit}
.md-form-dialog>footer{display:flex;justify-content:flex-end;gap:8px;padding:10px 16px;border-top:1px solid var(--line)}
.md-form-dialog>footer button{min-width:86px;height:34px}
@media(max-width:800px){.md-screen,.md-screen-process{grid-template-columns:minmax(0,1fr)}.md-actions{max-height:none;flex-direction:row;flex-wrap:wrap}.md-actions .md-button{flex:1 0 auto}.md-vendor-panel,.md-product-relations,.md-screen-process .md-process-preview{grid-column:1}.md-screen-process .md-actions,.md-screen-product .md-actions{grid-column:1;grid-row:auto}.md-table-scroll,.md-grid-scroll{max-height:44vh}}
@media(max-width:600px){.md-copy-source{grid-template-columns:minmax(0,1fr)}.md-product-picture{grid-template-columns:minmax(0,1fr)}.md-form-grid{grid-template-columns:minmax(0,1fr)}.md-form-grid .md-form-field.wide{grid-column:auto}.md-audit{grid-template-columns:repeat(2,minmax(0,1fr))}.md-form-dialog{max-height:96vh}.md-query{align-items:flex-start}.md-state-filter{flex-wrap:wrap}.md-relation-form{align-items:stretch}.md-inline-form label,.md-relation-form label{flex:1 1 140px}}
</style>

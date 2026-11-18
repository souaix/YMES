<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import ProductBomEditor from './ProductBomEditor.vue'
import ProductBusinessRuleSetup from './ProductBusinessRuleSetup.vue'
import ProductOpDescriptionSetup from './ProductOpDescriptionSetup.vue'
import ProductParameterSetup from './ProductParameterSetup.vue'
import ProductProcessEditor from './ProductProcessEditor.vue'
import ProductPropertyMaster from './ProductPropertyMaster.vue'

const window = globalThis.window
type IssueState = -1 | 0 | 1 | 2
type ProductRow = Record<string, any>
type ProductRelation = {
  processes: ProductRow[]
  bom: ProductRow[]
  constraints: ProductRow[]
  properties: ProductRow[]
  parameters?: ProductRow
  businessRules?: ProductRow
  opDescriptions?: ProductRow[]
  materialGroups?: ProductRow[]
}
type BomTreeLine = { id: string; label: string; kind: 'root' | 'material' | 'product' | 'group'; depth: number }
type Envelope<T> = { items?: T[]; rows?: T[]; data?: T[]; detail?: string; message?: string; title?: string }

const props = withDefaults(defineProps<{
  apiBaseUrl: string
  currentUserNo?: string
  developmentPreview?: boolean
}>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const rowsKey = 'ymes.dev.prd-product-basis.v1'
const relationKey = 'ymes.dev.prd-product-basis-relations.v1'
const states: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未核准' }, { value: 1, label: '簽核中' },
  { value: 2, label: '已核准' }, { value: -1, label: '不使用' },
]

const productColumns = [
  'ProductNo','ProductVersion','ProductName','ProductType','IssueState','Creator','CreateDate','Description','CurVersion',
  'UnitNo','UnitType','SpecNo','CartonQty','PalletQty','PackOIPath','BoxQty','ProductCode','SerialTypeNo_Lot',
  'SerialTypeNo_Comp','PictureName','LotStdQty','ERPNo',
  ...Array.from({ length: 10 }, (_, i) => 'User_Defined' + String(i + 1).padStart(2, '0')),
  ...Array.from({ length: 10 }, (_, i) => 'User_Defined' + String(i + 11).padStart(2, '0')),
  ...Array.from({ length: 10 }, (_, i) => 'User_Defined' + String(i + 21).padStart(2, '0')),
  ...Array.from({ length: 4 }, (_, i) => 'User_Defined' + String(i + 31).padStart(2, '0')),
  'GraphNo','QCCategory','ItemSpec','CustomerNo','DeviceNo','ECNControl','I_CUSTOMERNO','BatchUserNo',
  'BatchECNLock','DataStamp','Reviser','ReviseDate','SAP_PLNAL','SAP_KTEXT','SAP_AENNR','SAP_DATUV',
  'SAP_UPLOAD','SAP_UPLOADDATE','SAP_BOMAENNR','XX_SOPSPEC','XX_SOPREV','SOPSpec','SOPRev','SAP_WERKS',
  'SAP_IEUPLOAD','SAP_IEUPLOADDATE','SAP_IEUPLOADUSER','SAP_SAPUPDATEDATETIME',
]
const hiddenColumns = new Set(['IssueState','DataStamp','CartonQty','PalletQty','BoxQty','PackOIPath'])
const gridColumns = computed(() => productColumns.filter(column => !hiddenColumns.has(column)))
const sapPlnalOptions = [
  { value: '11', label: 'Homemade_自製' },
  { value: '21', label: 'Partial Process Outsourcing_半委' },
  { value: '51', label: 'Whole Process Outsourcing_全製程委外' },
  { value: '31', label: 'Rework_重工' },
  { value: '61', label: 'Total Outsource Rework_重工全委' },
]
const toolbar = [
  { key: 'add', label: 'ADD (&A)' }, { key: 'edit', label: 'EDIT (&E)' },
  { key: 'delete', label: 'DEL (&D)' }, { key: 'copy', label: 'Copy (&C)' },
  { key: 'version', label: 'Version (&V)' }, { key: 'approve', label: 'Approve (&Y)' },
  { key: 'property', label: 'Property (&S)' }, { key: 'parameter', label: 'Parameter (&P)' },
  { key: 'business-rule', label: 'Business Rule' }, { key: 'op-description', label: 'OPDescription' },
  { key: 'recipe-group', label: 'RecipeGroup' }, { key: 'accessory', label: 'Accessory' },
  { key: 'resource', label: 'Resource' }, { key: 'sub-op', label: 'SubOP' },
  { key: 'program', label: 'Program' }, { key: 'default', label: 'Default' },
  { key: 'run-time', label: 'RunTime' }, { key: 'qc-item', label: 'QC Item' },
  { key: 'packing-set', label: 'Packing Set' }, { key: 'eqp-cap-item', label: 'EQPCapItem' },
  { key: 'esop', label: 'eSOP' }, { key: 'equipment-spec', label: 'EquipmentSpec' },
  { key: 'aps', label: 'APS Integration' }, { key: 'bin', label: 'Bin' },
  { key: 'gbom-import', label: 'GBOMImport' }, { key: 'gbom-export', label: 'GBOMExport' },
]
const featureInfo: Record<string, { title: string; source: string; description: string }> = {
  'process-setup': { title: 'Product Process', source: 'PD_ProductProcess', description: 'Set 只允許未核准產品；以產品編號、版本及目前流程資料開啟流程設定。儲存後 WinForms 重新載入產品流程。' },
  'process-expand': { title: 'Expand Process', source: 'PD_ExpandProcess', description: '需先選取產品流程列並載入其 Production Segment。區段 IsNetFlow 與 MOTypeNo、ProcessNo、ProcessVersion 會傳入流程展開功能。' },
  'bom-editor': { title: 'Product BOM', source: 'frmProductBOM_THEIL_BU1', description: 'Set 只允許未核准產品；帶入產品編號、版本及該產品的流程資料。儲存後重建 BOM 樹。' },
  property: { title: 'Property', source: 'PD_ProductProperty', description: '依產品版本設定產品屬性。核准時會檢查 Controlled=1 必填屬性，Controlled=2 未填時提示確認。' },
  parameter: { title: 'Parameter', source: 'PD_ParameterSetup', description: '依產品、版本與流程設定製程參數；WinForms 會先確認已設定產品流程及 PS 權限。' },
  'business-rule': { title: 'Business Rule', source: 'PD_ProductOPBusinessRule', description: '維護產品流程作業站商業規則；需要產品流程資料及 PS 權限。' },
  'op-description': { title: 'OPDescription', source: 'PD_ProductOPDescription', description: '設定產品流程作業站說明，帶入產品目前流程與流程版本。' },
  'recipe-group': { title: 'RecipeGroup', source: 'PD_ProductRecipe', description: '維護產品流程 Recipe Group。' },
  accessory: { title: 'Accessory', source: 'frmPRDAccessory_THEIL_BU1', description: '維護產品版本使用的模治具/配件。' },
  resource: { title: 'Resource', source: 'PD_ProductOPArea', description: '設定產品流程作業區域；需要產品流程資料及 PS 權限。' },
  'sub-op': { title: 'SubOP', source: 'PD_PRDSubOP', description: '由產品編號與版本開啟產品子作業設定。' },
  program: { title: 'Program', source: 'frmProductProgram_THEIL_BU1', description: '設定產品使用的程式與版本。' },
  default: { title: 'Default', source: 'PD_ProductLoadDefault', description: '設定產品流程的預設值。' },
  'run-time': { title: 'RunTime', source: 'PD_ProductRunTime', description: '設定產品各作業站的標準工時。' },
  'qc-item': { title: 'QC Item', source: 'PD_ProductQCItem', description: '設定產品對應的品管項目。' },
  'packing-set': { title: 'Packing Set', source: 'PD_ProductPackingSet', description: '設定產品包裝規則與包裝配件。' },
  'eqp-cap-item': { title: 'EQPCapItem', source: 'frmProductEQPConstraint_THEIL_BU1', description: '維護產品版本、作業站與設備的限制對應。' },
  esop: { title: 'eSOP', source: 'eSOP product function', description: '依產品編號、版本開啟 eSOP 內容。' },
  'equipment-spec': { title: 'EquipmentSpec', source: 'frmPRDEquipmentSpec_THEIL_BU1', description: '維護產品版本的設備規格。' },
  aps: { title: 'APS Integration', source: 'frmPRDAPSIntegration_THEIL_BU1', description: '維護產品流程與 APS 整合資料；按鈕受 ExecuteAPS 參數控制。' },
  bin: { title: 'Bin', source: 'PRD bin function', description: '維護產品程式使用的 Bin 資料。' },
  'gbom-import': { title: 'GBOMImport', source: 'wsPRD.UpdGBOMExcelFile', description: '僅允許未核准產品；Excel 檔名須為 ProductNo-ProductVersion，讀取內容後上傳檔案位元組。' },
  'gbom-export': { title: 'GBOMExport', source: 'Product GBOM export', description: '匯出已匯入的 ProductNo-ProductVersion GBOM Excel。' },
  'product-import': { title: 'Import', source: 'PD_ProductDataImport_PRS', description: '透過 ObjectFileHandler 開啟產品資料匯入功能；成功回傳 success 後重新載入產品清單。' },
}

const seedRows: ProductRow[] = [
  {
    productNo: 'DEV-PRD-1001', productVersion: '01', productName: 'DEV 控制器組件', productType: 'DEV-FINISHED',
    issueState: 0, curVersion: 0, creator: 'DEV001', createDate: '2026-11-01T09:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'DEV-SPEC-01', productCode: 'DEV1001', serialTypeNoLot: 'DEV-LOT-A',
    serialTypeNoComp: 'DEV-COMP-A', pictureName: '', lotStdQty: 0, erpNo: 'DEV-ERP-1001',
    description: '開發用假資料。未核准產品，含流程、BOM、屬性及設備限制示例。',
    qccategory: 'DEV-QC', customerNo: 'DEV-CUSTOMER', deviceNo: 'DEV-DEVICE', ecnControl: 'N',
    sapKtext: 'DEV-PRD-1001-01', sapPlnal: '11', dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-01T09:00:00',
  },
  {
    productNo: 'DEV-PRD-1002', productVersion: '02', productName: 'DEV 核准產品', productType: 'DEV-SEMIFINISHED',
    issueState: 2, curVersion: 1, creator: 'DEV002', createDate: '2026-10-21T10:10:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1002', lotStdQty: 120,
    description: '開發用假資料。已核准且為目前版本。',
    dataStamp: 3, reviser: 'DEV002', reviseDate: '2026-11-03T13:20:00', sapPlnal: '21',
  },
  {
    productNo: 'DEV-PRD-1003', productVersion: '01', productName: 'DEV 簽核中產品', productType: 'DEV-FINISHED',
    issueState: 1, curVersion: 0, creator: 'DEV003', createDate: '2026-11-05T11:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1003', lotStdQty: 0,
    description: '開發用假資料。簽核中不可修改或刪除。',
    dataStamp: 2, reviser: 'DEV003', reviseDate: '2026-11-05T11:00:00', sapPlnal: '51',
  },
  {
    productNo: 'DEV-PRD-1004', productVersion: '01', productName: 'DEV 停用產品', productType: 'DEV-FINISHED',
    issueState: -1, curVersion: 0, creator: 'DEV001', createDate: '2026-08-12T14:00:00',
    unitNo: 'EA', unitType: 'PCS', specNo: 'N/A', productCode: 'DEV1004', lotStdQty: 0,
    description: '開發用假資料。不使用狀態可實體刪除。',
    dataStamp: 5, reviser: 'DEV002', reviseDate: '2026-10-02T08:20:00', sapPlnal: '31',
  },
]
const seedRelations: Record<string, ProductRelation> = {
  'DEV-PRD-1001|01': {
    processes: [{ moTypeNo: 1, psNo: 'DEV-PS-10', psOrder: 10, processNo: 'DEV-PROC-01', processVersion: '01', defaultProcess: 1, haveComponent: 1, haveLevel: 0, equipmentGroup: 'DEV-EQP-GRP' }],
    opDescriptions: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', opNo: 'DEV-OP-10', opName: '點膠作業', description: 'DEV 產品版本作業說明範例。', dataStamp: 2, creator: 'DEV001', createDate: '2026-11-01T09:10:00', reviser: 'DEV001', reviseDate: '2026-11-04T10:30:00' },
    ],
    bom: [
      { nodeNo: 'DEV-MTL-001', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-10', stdQty: 2, unitNo: 'EA', decreaseRate: 0, specified: 1, description: 'DEV 材料明細' },
      { nodeNo: 'DEV-PRD-SUB', nodeType: 1, nodeVersion: '01', opNo: 'DEV-OP-20', stdQty: 1, unitNo: 'EA', decreaseRate: 0.02, specified: 0, description: 'DEV 子產品節點' },
    ],
    constraints: [{ opNo: 'DEV-OP-10', equipmentNo: 'DEV-EQP-01', description: '限定設備示例', creator: 'DEV001', createDate: '2026-11-01T09:00:00' }],
    properties: [
      { productNo: 'DEV-PRD-1001', productVersion: '01', propertyNo: 'DEV-MODEL', controlled: 1, defaultValue: 'CTRL-A', propertySequence: 1, description: 'DEV 型號屬性', issueState: 0, dataStamp: 1, creator: 'DEV001', createDate: '2026-10-01T08:30:00', reviser: '', reviseDate: '' },
      { productNo: 'DEV-PRD-1001', productVersion: '01', propertyNo: 'DEV-COLOR', controlled: 2, defaultValue: '', propertySequence: 2, description: 'DEV 顏色提示屬性', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-10-01T08:31:00', reviser: '', reviseDate: '' },
    ],
    parameters: {
      ops: [
        { serialNo: 'DEV-SER-OP-10', psNo: 'DEV-PS-10', opNo: 'DEV-OP-10', opName: '點膠作業', opOrder: 10, haveComponent: 1, nodeId: '', opScript: '', dataStamp: 1 },
        { serialNo: 'DEV-SER-OP-20', psNo: 'DEV-PS-10', opNo: 'DEV-OP-20', opName: '組裝作業', opOrder: 20, haveComponent: 1, nodeId: '', opScript: '', dataStamp: 1 },
      ],
      attributes: [
        { serialNo: 'DEV-SER-OP-10', attribType: 0, attribNo: 'DEV-LOT-QTY', attribValue: '24', attribSequence: 1, attribSource: 3, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
        { serialNo: 'DEV-SER-OP-10', attribType: 0, attribNo: 'DEV-LOT-TEMP', attribValue: '', attribSequence: 2, attribSource: 0, attribPhase: 2, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
        { serialNo: 'DEV-SER-OP-10', attribType: 1, attribNo: 'DEV-COMP-LOT', attribValue: '', attribSequence: 1, attribSource: 0, attribPhase: 1, attribScript: '', saveToLotPropertyNo: '', qcItemNo: '' },
      ],
      rules: [{ serialNo: 'DEV-SER-OP-10', ruleNo: 'DEV-RULE-LOT-001', ruleSequence: 1, rulePhase: 2, ruleScript: 'return lot.QTY > 0;', ruleType: 0, ruleClass: 0 }],
      equipments: [{ serialNo: 'DEV-SER-OP-10', psNo: 'DEV-PS-10', opNo: 'DEV-OP-10', equipmentNo: 'DEV-EQP-01', recipeGroup: 'DEV-RECIPE-GROUP-A', recipes: [{ recipeNo: 'DEV-RECIPE-001', recipeValue: 'DEV-SET-01' }] }],
    },
    businessRules: { operations: [{ opNo: 'DEV-OP-10', opName: '點膠作業', processNo: 'DEV-PROC-01', processVersion: '01', nodes: [
      { id: 'DEV-BR-START', nodeNo: 'START', kind: 'start', x: 70, y: 155 },
      { id: 'DEV-BR-001', nodeNo: 'DEV-BR-001', kind: 'rule', x: 270, y: 155, phaseNo: 1, phaseName: 'Before OP' },
      { id: 'DEV-BR-002', nodeNo: 'DEV-BR-002', kind: 'rule', x: 500, y: 155, phaseNo: 2, phaseName: 'During OP' },
      { id: 'DEV-BR-END', nodeNo: 'END', kind: 'end', x: 720, y: 155 },
    ], links: [
      { id: 'DEV-BRL-1', fromNode: 'DEV-BR-START', toNode: 'DEV-BR-001', linkName: 'pass', lineStyle: 'VH', phaseNo: 99 },
      { id: 'DEV-BRL-2', fromNode: 'DEV-BR-001', toNode: 'DEV-BR-002', linkName: 'pass', lineStyle: 'HV', phaseNo: 1 },
      { id: 'DEV-BRL-3', fromNode: 'DEV-BR-002', toNode: 'DEV-BR-END', linkName: 'pass', lineStyle: 'polyline', phaseNo: 2 },
    ] }] },
    materialGroups: [{ nodeNo: 'DEV-MTL-001', opNo: 'DEV-OP-10', materialNo: 'DEV-MTL-GROUP-A' }, { nodeNo: 'DEV-MTL-001', opNo: 'DEV-OP-10', materialNo: 'DEV-MTL-GROUP-B' }],
  },
  'DEV-PRD-SUB|01': {
    processes: [],
    bom: [
      { nodeNo: 'DEV-MTL-SUB-01', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-30', stdQty: 4, unitNo: 'EA', decreaseRate: 0, specified: 1, description: 'DEV 子產品材料' },
      { nodeNo: 'DEV-PRD-COMP', nodeType: 1, nodeVersion: '02', opNo: 'DEV-OP-40', stdQty: 1, unitNo: 'EA', decreaseRate: 0.01, specified: 0, description: 'DEV 第二層子產品' },
    ],
    constraints: [], properties: [],
  },
  'DEV-PRD-COMP|02': {
    processes: [],
    bom: [{ nodeNo: 'DEV-MTL-COMP-01', nodeType: 0, nodeVersion: '001', opNo: 'DEV-OP-50', stdQty: 2, unitNo: 'EA', decreaseRate: 0.005, specified: 1, description: 'DEV 第三層材料' }],
    constraints: [], properties: [],
  },
  'DEV-PRD-1002|02': { processes: [], bom: [], constraints: [], properties: [] },
  'DEV-PRD-1003|01': { processes: [], bom: [], constraints: [], properties: [] },
  'DEV-PRD-1004|01': { processes: [], bom: [], constraints: [], properties: [] },
}

const rows = ref<ProductRow[]>([])
const relations = ref<Record<string, ProductRelation>>({})
const issueState = ref<IssueState>(0)
const productQuery = ref('')
const selectedKey = ref('')
const selectedKeys = ref<string[]>([])
const selectedProcessIndex = ref(0)
const activeTab = ref<'Process' | 'BOM' | 'EQPConstraint'>('Process')
const formOpen = ref(false)
const mode = ref<'add' | 'edit' | 'copy' | 'version'>('add')
const form = ref<ProductRow>(blankForm())
const detailSource = ref<'productType' | 'otherProduct' | 'none'>('none')
const sourceProductKey = ref('')
const selectedFile = ref<File | null>(null)
const picturePreview = ref('')
const lookupField = ref('')
const featureKey = ref('')
const error = ref('')
const notice = ref('')
const busy = ref(false)

function blankForm(): ProductRow {
  return {
    productNo: '', productVersion: '01', productName: '', productType: '', productCode: '',
    unitType: '', unitNo: '', description: '', specNo: 'N/A', serialTypeNoLot: '', serialTypeNoComp: '',
    pictureName: '', lotStdQty: 0, sapKtext: '', sapBomAennr: '', sapAennr: '', sapDatuv: '',
    sapPlnal: '', xxSopSpec: '', xxSopRev: '', erpNo: '', qccategory: '', customerNo: '',
    deviceNo: '', itemSpec: '', ecnControl: 'N', lotStdQtyMode: 'none',
  }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function keyOf(row: ProductRow): string { return String(row.productNo ?? '') + '|' + String(row.productVersion ?? '') }
function curVersionLabel(value: number): string { return Number(value) === 1 ? 'Yes' : 'No' }
function text(value: unknown): string { return value == null ? '' : String(value) }
function formatDate(value: unknown): string {
  const raw = text(value)
  if (!raw) return ''
  const date = new Date(raw)
  if (Number.isNaN(date.getTime())) return raw.replace('T', ' ').replaceAll('-', '/').slice(0, 19)
  const pad = (n: number) => String(n).padStart(2, '0')
  return date.getFullYear() + '/' + pad(date.getMonth() + 1) + '/' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds())
}
function rowCell(row: ProductRow, column: string): string {
  const key = column.charAt(0).toLowerCase() + column.slice(1)
  const value = column === 'CurVersion' ? row.curVersion ?? row.currentVersion : row[key]
  if (column === 'CurVersion') return curVersionLabel(Number(value ?? 0))
  if (column.endsWith('Date') || column.endsWith('DATETIME')) return formatDate(value)
  return text(value)
}
function normalizeProduct(row: ProductRow): ProductRow {
  const currentVersion = row.curVersion ?? row.currentVersion
  return {
    ...row,
    curVersion: typeof currentVersion === 'boolean' ? Number(currentVersion) : Number(currentVersion ?? 0),
    createDate: row.createDate ?? row.createdAt ?? '',
    dataStamp: Number(row.dataStamp ?? 0),
  }
}
function normalizeProcess(row: ProductRow): ProductRow {
  return {
    ...row,
    psNo: row.psNo ?? row.productionSegmentNo ?? '',
    psOrder: row.psOrder ?? row.segmentOrder ?? 0,
    processVersion: row.processVersion ?? row.resolvedProcessVersion ?? row.processVersionSelector ?? '',
  }
}
function normalizeBom(row: ProductRow): ProductRow {
  const componentType = text(row.componentType).toUpperCase()
  return {
    ...row,
    nodeNo: row.nodeNo ?? row.componentNo ?? '',
    nodeType: row.nodeType ?? (componentType === 'PRODUCT' ? 1 : 0),
    opNo: row.opNo ?? row.inputOperationNo ?? '',
    stdQty: row.stdQty ?? row.standardQty ?? 0,
    unitNo: row.unitNo ?? '',
    nodeVersion: row.nodeVersion ?? row.componentVersion ?? '',
  }
}
function appendBomChildren(productNo: string, productVersion: string, depth: number, ancestors: Set<string>, result: BomTreeLine[]): void {
  if (depth > 5) return
  const productKey = productNo + '|' + productVersion
  const relation = relations.value[productKey]
  for (const item of [...(relation?.bom ?? [])].sort((left, right) => Number(right.nodeType) - Number(left.nodeType))) {
    const isProduct = Number(item.nodeType) === 1
    const nodeKey = productKey + '|' + text(item.nodeNo) + '|' + text(item.opNo)
    const decreaseRate = Number(item.decreaseRate ?? 0) * 100
    const decreaseText = Number.isFinite(decreaseRate) ? decreaseRate.toFixed(8).replace(/0+$/, '').replace(/\.$/, '') : '0'
    const specified = item.specified === true || Number(item.specified) === 1 ? '指定用料' : '非指定用料'
    const description = text(item.description) ? ', ' + text(item.description) : ''
    result.push({
      id: nodeKey,
      label: text(item.nodeNo) + ' (' + text(item.opNo) + ':' + text(item.stdQty) + '/' + text(item.unitNo) + ', 損耗率:' + decreaseText + '%, ' + specified + ')' + description,
      kind: isProduct ? 'product' : 'material',
      depth,
    })
    if (!isProduct) {
      const groups = relation?.materialGroups?.filter(group => text(group.nodeNo) === text(item.nodeNo) && text(group.opNo) === text(item.opNo)) ?? []
      for (const group of groups) result.push({ id: nodeKey + '|group|' + text(group.materialNo), label: text(group.materialNo), kind: 'group', depth: depth + 1 })
      continue
    }
    const childNo = text(item.nodeNo)
    const childVersion = text(item.nodeVersion) || '01'
    const childKey = childNo + '|' + childVersion
    if (depth < 5 && childNo && !ancestors.has(childKey)) {
      const nextAncestors = new Set(ancestors)
      nextAncestors.add(childKey)
      appendBomChildren(childNo, childVersion, depth + 1, nextAncestors, result)
    }
  }
}
function currentUser(): string { return props.currentUserNo || (mock.value ? 'DEV001' : '') }
function relatedFor(row: ProductRow | null): ProductRelation {
  if (!row) return { processes: [], bom: [], constraints: [], properties: [], opDescriptions: [] }
  return relations.value[keyOf(row)] ?? { processes: [], bom: [], constraints: [], properties: [], opDescriptions: [] }
}
const visibleRows = computed(() => rows.value
  .filter(row => Number(row.issueState) === issueState.value)
  .filter(row => !productQuery.value.trim() || text(row.productNo).toLocaleLowerCase().includes(productQuery.value.trim().replaceAll('%', '').replaceAll('*', '').toLocaleLowerCase())))
const active = computed(() => rows.value.find(row => keyOf(row) === selectedKey.value) ?? null)
const activeRelations = computed(() => relatedFor(active.value))
const selectedProcess = computed(() => activeRelations.value.processes[selectedProcessIndex.value] ?? null)
const bomTreeLines = computed(() => {
  if (!active.value) return []
  const result: BomTreeLine[] = [{ id: keyOf(active.value), label: keyOf(active.value).replace('|', ':'), kind: 'root', depth: 0 }]
  appendBomChildren(active.value.productNo, active.value.productVersion, 1, new Set([keyOf(active.value)]), result)
  return result
})
const selectedRows = computed(() => rows.value.filter(row => selectedKeys.value.includes(keyOf(row))))
const feature = computed(() => featureInfo[featureKey.value] ?? null)
const editable = computed(() => Boolean(active.value && Number(active.value.issueState) === 0))
const lookupOptions = computed(() => {
  if (lookupField.value === 'productType') return ['DEV-FINISHED', 'DEV-SEMIFINISHED', 'DEV-RAW']
  if (lookupField.value === 'unitType') return ['PCS', 'WEIGHT', 'LENGTH']
  if (lookupField.value === 'unitNo') return ['EA', 'KG', 'M']
  if (lookupField.value === 'serialTypeNoLot') return ['DEV-LOT-A', 'DEV-LOT-B']
  if (lookupField.value === 'serialTypeNoComp') return ['DEV-COMP-A', 'DEV-COMP-B']
  return []
})

function persistMock(): void {
  localStorage.setItem(rowsKey, JSON.stringify(rows.value))
  localStorage.setItem(relationKey, JSON.stringify(relations.value))
}
function loadMock(): void {
  try {
    const savedRows = localStorage.getItem(rowsKey)
    const savedRelations = localStorage.getItem(relationKey)
    rows.value = savedRows ? JSON.parse(savedRows) as ProductRow[] : clone(seedRows)
    relations.value = savedRelations ? JSON.parse(savedRelations) as Record<string, ProductRelation> : clone(seedRelations)
    let migrated = !savedRows || !savedRelations
    for (const [key, seed] of Object.entries(seedRelations)) {
      const existing = relations.value[key]
      if (existing && !existing.parameters && seed.parameters) {
        existing.parameters = clone(seed.parameters)
        migrated = true
      }
      if (existing && !existing.businessRules && seed.businessRules) {
        existing.businessRules = clone(seed.businessRules)
        migrated = true
      }
      if (existing && !existing.opDescriptions && seed.opDescriptions) {
        existing.opDescriptions = clone(seed.opDescriptions)
        migrated = true
      }
    }
    if (migrated) persistMock()
  } catch {
    rows.value = clone(seedRows)
    relations.value = clone(seedRelations)
    persistMock()
  }
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const base = props.apiBaseUrl.replace(/\/+$/, '')
  const response = await fetch(base + '/api/' + path.replace(/^\/+/, ''), { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL + 'login')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as Envelope<unknown> | null
    throw new Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + response.status + ')')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}
function selectRow(row: ProductRow): void {
  selectedKey.value = keyOf(row)
  selectedProcessIndex.value = 0
  if (!selectedKeys.value.includes(keyOf(row))) selectedKeys.value = [keyOf(row)]
  if (!mock.value) void loadRelations(row)
}
function toggleSelection(row: ProductRow, event: Event): void {
  selectRow(row)
  const checked = (event.target as HTMLInputElement).checked
  selectedKeys.value = checked
    ? Array.from(new Set([...selectedKeys.value, keyOf(row)]))
    : selectedKeys.value.filter(key => key !== keyOf(row))
}
async function load(): Promise<void> {
  busy.value = true
  error.value = ''
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (productQuery.value.trim()) query.set('keyword', productQuery.value.trim())
      const result = await api<ProductRow[] | Envelope<ProductRow>>('products?' + query.toString())
      rows.value = (Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []).map(normalizeProduct)
    }
    if (!visibleRows.value.some(row => keyOf(row) === selectedKey.value)) {
      selectedKey.value = visibleRows.value[0] ? keyOf(visibleRows.value[0]) : ''
      selectedKeys.value = selectedKey.value ? [selectedKey.value] : []
    }
    selectedKeys.value = selectedKeys.value.filter(key => visibleRows.value.some(row => keyOf(row) === key))
    const selected = active.value
    if (!mock.value && selected) await loadRelations(selected)
    else if (!selected) relations.value = {}
    notice.value = '資料總筆數 ' + visibleRows.value.length
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '產品資料載入失敗。'
  } finally { busy.value = false }
}
async function loadRelations(row: ProductRow): Promise<void> {
  const key = keyOf(row)
  try {
    const prefix = 'products/' + encodeURIComponent(text(row.productNo)) + '/versions/' + encodeURIComponent(text(row.productVersion))
    const [processResult, bomResult] = await Promise.all([
      api<ProductRow[] | Envelope<ProductRow>>(prefix + '/processes'),
      api<ProductRow[] | Envelope<ProductRow>>(prefix + '/bom'),
    ])
    if (selectedKey.value !== key) return
    const processRows = Array.isArray(processResult) ? processResult : processResult.items ?? processResult.rows ?? processResult.data ?? []
    const bomRows = Array.isArray(bomResult) ? bomResult : bomResult.items ?? bomResult.rows ?? bomResult.data ?? []
    relations.value = {
      ...relations.value,
      [key]: {
        processes: processRows.map(normalizeProcess),
        bom: bomRows.map(normalizeBom),
        constraints: relations.value[key]?.constraints ?? [],
        properties: relations.value[key]?.properties ?? [],
        opDescriptions: relations.value[key]?.opDescriptions ?? [],
      },
    }
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '產品流程與 BOM 載入失敗。'
  }
}
function openForm(nextMode: 'add' | 'edit' | 'copy' | 'version'): void {
  error.value = ''
  mode.value = nextMode
  selectedFile.value = null
  picturePreview.value = ''
  detailSource.value = nextMode === 'copy' || nextMode === 'version' ? 'otherProduct' : 'none'
  sourceProductKey.value = active.value ? keyOf(active.value) : ''
  if (nextMode === 'edit' && active.value) form.value = { ...blankForm(), ...clone(active.value) }
  else if ((nextMode === 'copy' || nextMode === 'version') && active.value) {
    form.value = { ...blankForm(), ...clone(active.value) }
    if (nextMode === 'copy') form.value.productNo = ''
    if (nextMode === 'version') {
      form.value.productVersion = ''
      form.value.productType = active.value.productType
    }
  } else form.value = blankForm()
  form.value.lotStdQtyMode = Number(form.value.lotStdQty ?? 0) === 0 ? 'none' : 'set'
  formOpen.value = true
}
function closeForm(): void { formOpen.value = false; lookupField.value = ''; selectedFile.value = null; picturePreview.value = '' }
function openLookup(field: string): void { lookupField.value = field }
function chooseLookup(value: string): void { form.value[lookupField.value] = value; lookupField.value = '' }
function onPictureChange(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0] ?? null
  selectedFile.value = file
  form.value.pictureName = file?.name ?? ''
  picturePreview.value = file ? URL.createObjectURL(file) : ''
}
function validateForm(): boolean {
  const required = ['productNo', 'productVersion', 'productName', 'productType', 'unitType', 'unitNo', 'sapPlnal']
  const missing = required.filter(key => !text(form.value[key]).trim())
  if (missing.length) {
    const labels: Record<string, string> = { productNo: 'ProductNo', productVersion: 'ProductVersion', productName: 'ProductName', productType: 'ProductType', unitType: 'UnitType', unitNo: 'UnitNo', sapPlnal: 'SAP_PLNAL' }
    error.value = '必填欄位未填：' + missing.map(key => labels[key]).join('、')
    return false
  }
  if (Number(form.value.lotStdQty ?? 0) < 0) { error.value = 'LotStdQty 不可小於 0。'; return false }
  const duplicate = rows.value.some(row => keyOf(row) === text(form.value.productNo).trim() + '|' + text(form.value.productVersion).trim()
    && (mode.value !== 'edit' || keyOf(row) !== keyOf(active.value ?? {})))
  if (duplicate) { error.value = '此產品編號與版本已存在。'; return false }
  return true
}
async function saveForm(closeAfter = true): Promise<void> {
  error.value = ''
  if (!validateForm()) return
  busy.value = true
  try {
    if (!mock.value) {
      const formPayload = { ...form.value }
      delete formPayload.lotStdQtyMode
      const payload = {
        ...formPayload,
        detailSource: detailSource.value,
        sourceProductNo: sourceProductKey.value.split('|')[0],
        sourceProductVersion: sourceProductKey.value.split('|')[1],
      }
      const isEdit = mode.value === 'edit'
      const isVersion = mode.value === 'version'
      const path = isEdit
        ? 'products/' + encodeURIComponent(text(active.value?.productNo)) + '/versions/' + encodeURIComponent(text(active.value?.productVersion))
        : isVersion
          ? 'products/' + encodeURIComponent(text(active.value?.productNo)) + '/versions/' + encodeURIComponent(text(active.value?.productVersion)) + '/new-version'
          : 'products'
      const saved = await api<ProductRow>(path, { method: isEdit ? 'PUT' : 'POST', body: JSON.stringify(payload) })
      if (closeAfter) closeForm()
      await load()
      selectRow(saved)
      notice.value = '產品 ' + text(saved.productNo) + '/' + text(saved.productVersion) + ' 已' + (isEdit ? '更新' : isVersion ? '建立新版本' : '建立') + '。'
      return
    }
    const old = mode.value === 'edit' ? active.value : null
    const saved: ProductRow = {
      ...clone(form.value),
      productNo: text(form.value.productNo).trim().toUpperCase(),
      productVersion: text(form.value.productVersion).trim().toUpperCase(),
      issueState: old?.issueState ?? 0,
      curVersion: old?.curVersion ?? 0,
      creator: old?.creator ?? currentUser(),
      createDate: old?.createDate ?? new Date().toISOString(),
      reviser: currentUser(),
      reviseDate: new Date().toISOString(),
      dataStamp: Number(old?.dataStamp ?? 0) + (old ? 1 : 0),
    }
    const savedKey = keyOf(saved)
    if (old) rows.value = rows.value.map(row => keyOf(row) === keyOf(old) ? saved : row)
    else rows.value = [...rows.value, saved]
    if (!old) {
      const sourceRelations = relations.value[sourceProductKey.value]
      relations.value[savedKey] = detailSource.value === 'otherProduct' && sourceRelations
        ? clone(sourceRelations)
        : { processes: [], bom: [], constraints: [], properties: [], opDescriptions: [] }
    }
    persistMock()
    issueState.value = Number(saved.issueState ?? 0) as IssueState
    productQuery.value = ''
    selectedKey.value = savedKey
    selectedKeys.value = [savedKey]
    notice.value = 'DEV 假資料：產品 ' + saved.productNo + '/' + saved.productVersion + ' 已' + (old ? '更新' : '建立') + '。'
    if (closeAfter) closeForm()
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '產品資料儲存失敗。'
  } finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value.length ? selectedRows.value : active.value ? [active.value] : []
  const allowed = targets.filter(row => [0, 2, -1].includes(Number(row.issueState)))
  if (!allowed.length) { error.value = '沒有可刪除的產品；簽核中的資料不可刪除。'; return }
  const ask = allowed.map(row => Number(row.issueState) === 2 ? text(row.productNo) + '/' + text(row.productVersion) + ' 設為不使用' : text(row.productNo) + '/' + text(row.productVersion) + ' 實體刪除').join('\n')
  if (!window.confirm('確定執行以下操作？\n' + ask)) return
  busy.value = true
  try {
    if (mock.value) {
      const removeKeys = new Set<string>()
      rows.value = rows.value.flatMap(row => {
        if (!allowed.some(item => keyOf(item) === keyOf(row))) return [row]
        if (Number(row.issueState) === 2) return [{ ...row, issueState: -1, reviser: currentUser(), reviseDate: new Date().toISOString(), dataStamp: Number(row.dataStamp ?? 0) + 1 }]
        removeKeys.add(keyOf(row)); return []
      })
      for (const key of removeKeys) delete relations.value[key]
      persistMock()
      notice.value = 'DEV 假資料：已按簽核狀態停用或刪除產品。'
    } else {
      for (const row of allowed) {
        const path = 'products/' + encodeURIComponent(text(row.productNo)) + '/versions/' + encodeURIComponent(text(row.productVersion))
        await api(path + '?issueState=' + row.issueState + '&dataStamp=' + row.dataStamp, { method: 'DELETE' })
      }
      notice.value = '產品刪除操作完成。'
    }
    selectedKeys.value = []
    await load()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '產品刪除失敗。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = selectedRows.value.length ? selectedRows.value : active.value ? [active.value] : []
  const pending = targets.filter(row => Number(row.issueState) === 0)
  if (!pending.length) { error.value = '核准只處理 IssueState=0 的產品。'; return }
  if (!window.confirm('確定核准選取的 ' + pending.length + ' 筆產品？')) return
  busy.value = true
  try {
    if (mock.value) {
      for (const row of pending) {
        const rel = relatedFor(row)
        const missing = rel.properties.filter(property => Number(property.controlled) === 1 && !text(property.value || property.defaultValue).trim())
        if (missing.length) throw new Error('必填產品屬性尚未設定：' + missing.map(property => text(property.propertyNo)).join('、'))
        const warnings = rel.properties.filter(property => Number(property.controlled) === 2 && !text(property.value || property.defaultValue).trim())
        if (warnings.length && !window.confirm(text(row.productNo) + '/' + text(row.productVersion) + ' 尚有提示屬性未設定，仍要核准嗎？')) return
      }
      rows.value = rows.value.map(row => {
        const target = pending.some(item => keyOf(item) === keyOf(row))
        if (target) return { ...row, issueState: 2, curVersion: 1, reviser: currentUser(), reviseDate: new Date().toISOString(), dataStamp: Number(row.dataStamp ?? 0) + 1 }
        if (pending.some(item => text(item.productNo) === text(row.productNo))) return { ...row, curVersion: 0 }
        return row
      })
      persistMock()
      issueState.value = 2
      notice.value = 'DEV 假資料：產品已核准並設為目前版本。'
      await load()
    } else {
      for (const row of pending) {
        const path = 'products/' + encodeURIComponent(text(row.productNo)) + '/versions/' + encodeURIComponent(text(row.productVersion)) + '/approve'
        await api(path, { method: 'POST', body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
      }
      issueState.value = 2
      await load()
      notice.value = '產品核准操作完成。'
    }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '產品核准失敗。' }
  finally { busy.value = false }
}
async function makeVersion(): Promise<void> {
  if (!active.value) { error.value = '請先選取產品。'; return }
  openForm('version')
}
function actionDisabled(action: string): boolean {
  if (busy.value) return true
  if (action === 'edit') return !editable.value
  if (action === 'delete') return !selectedRows.value.length && !active.value
  if (action === 'approve') return !(selectedRows.value.some(row => Number(row.issueState) === 0) || Number(active.value?.issueState) === 0)
  if (['process-setup','bom-editor'].includes(action)) return !editable.value
  if (action === 'process-expand') return !active.value || !selectedProcess.value
  if (['copy','version', ...Object.keys(featureInfo)].includes(action)) return !active.value
  return false
}
function runAction(action: string): void {
  error.value = ''
  if (actionDisabled(action) && action !== 'add') { error.value = '請先選取符合狀態的產品資料。'; return }
  if (action === 'add') return openForm('add')
  if (action === 'edit') return openForm('edit')
  if (action === 'copy') return openForm('copy')
  if (action === 'version') return void makeVersion()
  if (action === 'delete') return void deleteSelected()
  if (action === 'approve') return void approveSelected()
  if (action === 'close') return emit('close')
  if (featureInfo[action]) {
    if (action === 'process-expand' && !selectedProcess.value) {
      error.value = '請先選取產品流程列。'
      return
    }
    if (['parameter','business-rule','op-description','resource'].includes(action) && !activeRelations.value.processes.length) {
      error.value = '請先為產品設定流程，或確認 PS 權限。'
      return
    }
    featureKey.value = action
  }
}
function closeFeature(): void { featureKey.value = '' }
function saveProductProcess(processes: ProductRow[]): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), processes: clone(processes) }
  persistMock()
  notice.value = '產品流程假資料已儲存。'
}
function saveProductBom(bom: ProductRow[], materialGroups: ProductRow[]): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), bom: clone(bom), materialGroups: clone(materialGroups) }
  persistMock()
  notice.value = '產品 BOM 假資料已儲存。'
}
function saveProductProperties(properties: ProductRow[]): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), properties: clone(properties) }
  persistMock()
  notice.value = '產品屬性假資料已儲存。'
}
function saveProductParameters(parameters: ProductRow): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), parameters: clone(parameters) }
  persistMock()
  notice.value = '產品 Parameter 假資料已儲存。'
}
function saveProductBusinessRules(businessRules: ProductRow): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), businessRules: clone(businessRules) }
  persistMock()
  notice.value = '產品 Business Rule 假資料已儲存。'
}
function saveProductOpDescriptions(opDescriptions: ProductRow[]): void {
  if (!active.value || !mock.value) return
  const key = keyOf(active.value)
  relations.value[key] = { ...relatedFor(active.value), opDescriptions: clone(opDescriptions) }
  persistMock()
  notice.value = '產品 OP 說明假資料已儲存。'
}
function addEqpConstraint(): void {
  if (!active.value || !editable.value) return
  const rel = relatedFor(active.value)
  rel.constraints = [...rel.constraints, { opNo: 'DEV-OP-10', equipmentNo: 'DEV-EQP-02', description: 'DEV 新增限制', creator: currentUser(), createDate: new Date().toISOString() }]
  relations.value[keyOf(active.value)] = rel
  if (mock.value) persistMock()
}
function removeEqpConstraint(row: ProductRow): void {
  if (!active.value || !editable.value) return
  const rel = relatedFor(active.value)
  rel.constraints = rel.constraints.filter(item => item !== row)
  relations.value[keyOf(active.value)] = rel
  if (mock.value) persistMock()
}
onMounted(() => { void load() })
</script>

<template>
  <main class="prd-master">
    <div class="prd-page-title"><strong>產品設定</strong><span>產品基本資料</span></div>
    <p v-if="mock" class="prd-dev-note">Development 假資料：資料只保存在這台電腦的瀏覽器 localStorage，不連 Oracle 或 YMES Production 資料庫。</p>
    <div class="prd-query">
      <fieldset class="prd-state">
        <label v-for="state in states" :key="state.value"><input v-model.number="issueState" type="radio" :value="state.value" @change="load" />{{ state.label }}</label>
      </fieldset>
      <label class="prd-search">產品編號<input v-model="productQuery" @keydown.enter.prevent="load" /></label>
      <span>%</span><button type="button" class="prd-button" @click="load">查詢</button>
    </div>
    <div v-if="error" class="prd-message error">{{ error }}</div>
    <div v-if="notice" class="prd-message">{{ notice }}</div>
    <div class="prd-workspace">
      <div class="prd-main-column">
        <div class="prd-grid-wrap">
          <table class="prd-grid">
            <thead><tr><th class="select-col">選取</th><th v-for="column in gridColumns" :key="column">{{ column }}</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleRows" :key="keyOf(row)" :class="{ focused: keyOf(row) === selectedKey }" @click="selectRow(row)" @dblclick="Number(row.issueState) === 0 && openForm('edit')">
                <td class="select-col"><input type="checkbox" :checked="selectedKeys.includes(keyOf(row))" :aria-label="'選取 ' + keyOf(row)" @click.stop @change="toggleSelection(row, $event)" /></td>
                <td v-for="column in gridColumns" :key="column">{{ rowCell(row, column) }}</td>
              </tr>
              <tr v-if="!busy && visibleRows.length === 0"><td :colspan="gridColumns.length + 1" class="empty">沒有符合條件的產品資料</td></tr>
              <tr v-if="busy && visibleRows.length === 0"><td :colspan="gridColumns.length + 1" class="empty">資料載入中…</td></tr>
            </tbody>
          </table>
        </div>
        <section class="prd-details">
          <nav class="prd-tabs" aria-label="產品明細">
            <button v-for="tab in ['Process','BOM','EQPConstraint'] as const" :key="tab" type="button" :class="{ active: activeTab === tab }" @click="activeTab = tab">{{ tab }}</button>
          </nav>
          <div v-if="!active" class="prd-detail-empty">先選擇一筆產品資料。</div>
          <div v-else-if="activeTab === 'Process'" class="prd-detail-content">
            <div class="prd-detail-actions"><button class="prd-button" type="button" :disabled="!editable" @click="runAction('process-setup')">Set ...</button><button class="prd-button" type="button" :disabled="!activeRelations.processes.length" @click="runAction('process-expand')">Expand All</button></div>
            <div class="prd-detail-grid"><table><thead><tr><th>MOTypeNo</th><th>PSNo</th><th>PSOrder</th><th>ProcessNo</th><th>ProcessVersion</th><th>DefaultProcess</th><th>HaveComponent</th><th>HaveLevel</th><th>EquipmentGroup</th></tr></thead>
              <tbody><tr v-for="(item,index) in activeRelations.processes" :key="index" :class="{ focused: selectedProcessIndex === index }" @click="selectedProcessIndex=index"><td>{{ item.moTypeNo }}</td><td>{{ item.psNo }}</td><td>{{ item.psOrder }}</td><td>{{ item.processNo }}</td><td>{{ item.processVersion }}</td><td>{{ item.defaultProcess ? 'Yes' : 'No' }}</td><td>{{ item.haveComponent ? 'Yes' : 'No' }}</td><td>{{ item.haveLevel ? 'Yes' : 'No' }}</td><td>{{ item.equipmentGroup }}</td></tr><tr v-if="!activeRelations.processes.length"><td colspan="9" class="empty">此產品版本尚未設定流程。</td></tr></tbody>
            </table></div>
          </div>
          <div v-else-if="activeTab === 'BOM'" class="prd-detail-content">
            <div class="prd-detail-actions"><button class="prd-button" type="button" :disabled="!editable" @click="runAction('bom-editor')">Set ...</button><button class="prd-button" type="button" @click="window.print()">Print</button></div>
            <div class="prd-bom-tree" role="tree">
              <div v-for="line in bomTreeLines" :key="line.id" class="prd-bom-line" :class="'bom-' + line.kind" role="treeitem" :aria-level="line.depth + 1" :style="{ paddingLeft: (8 + line.depth * 18) + 'px' }">
                <span v-if="line.depth" class="prd-bom-branch">├─</span><span v-else class="prd-bom-root-icon">▾</span>{{ line.label }}
              </div>
              <div v-if="!activeRelations.bom.length" class="prd-bom-empty">此產品版本尚未設定 BOM。</div>
            </div>
          </div>
          <div v-else class="prd-detail-content">
            <div class="prd-detail-actions"><button class="prd-button" type="button" :disabled="!editable" @click="addEqpConstraint">ADD</button><button class="prd-button" type="button" :disabled="!editable || !activeRelations.constraints.length" @click="removeEqpConstraint(activeRelations.constraints[activeRelations.constraints.length - 1])">DEL</button></div>
            <div class="prd-detail-grid"><table><thead><tr><th>OPNo</th><th>EquipmentNo</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
              <tbody><tr v-for="(item,index) in activeRelations.constraints" :key="index"><td>{{ item.opNo }}</td><td>{{ item.equipmentNo }}</td><td>{{ item.description }}</td><td>{{ item.creator }}</td><td>{{ formatDate(item.createDate) }}</td></tr><tr v-if="!activeRelations.constraints.length"><td colspan="5" class="empty">沒有設備限制資料。</td></tr></tbody>
            </table></div>
          </div>
        </section>
        <footer class="prd-count">資料總筆數 {{ visibleRows.length }}<span v-if="selectedKeys.length">　已選取 {{ selectedKeys.length }} 筆</span></footer>
      </div>
      <aside class="prd-actions" aria-label="產品功能">
        <button v-for="action in toolbar" :key="action.key" class="prd-button" type="button" :disabled="actionDisabled(action.key)" @click="runAction(action.key)">{{ action.label }}</button>
        <button class="prd-button" type="button" @click="featureKey='product-import'">Import</button>
        <button class="prd-button" type="button" @click="emit('close')">Close</button>
      </aside>
    </div>

    <div v-if="formOpen" class="prd-overlay" @click.self="closeForm">
      <form class="prd-form" @submit.prevent="saveForm(true)">
        <header class="prd-form-header"><div><small>產品基本資料定義（{{ mode.toUpperCase() }}）</small><h2>Product Basis Data</h2></div><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <div class="prd-form-scroll">
          <fieldset class="prd-fieldset"><legend>Product Basis Data</legend>
            <div class="prd-form-grid">
              <label class="field"><span>Product No *</span><input v-model.trim="form.productNo" :readonly="mode === 'edit' || mode === 'version'" /></label>
              <label class="field"><span>Product Version *</span><input v-model.trim="form.productVersion" :readonly="mode === 'edit'" /></label>
              <label class="field"><span>SPECNo</span><input v-model="form.specNo" /></label>
              <label class="field"><span>PictureName</span><div class="prd-control"><input readonly :value="form.pictureName" /><label class="prd-button file-button">...<input type="file" accept="image/*" @change="onPictureChange" /></label></div></label>
              <label class="field"><span>Product Name *</span><input v-model.trim="form.productName" required /></label>
              <label class="field"><span>Product Code</span><input v-model="form.productCode" /></label>
              <label class="field"><span>Product Type *</span><div class="prd-control"><input v-model="form.productType" readonly /><button type="button" class="prd-button" @click="openLookup('productType')">...</button></div></label>
              <label class="field"><span>LotStdQty</span><div class="prd-lotqty"><label><input v-model="form.lotStdQtyMode" type="radio" value="none" @change="form.lotStdQty=0" />Not Set</label><label><input v-model="form.lotStdQtyMode" type="radio" value="set" />Std Qty</label><input v-model.number="form.lotStdQty" type="number" min="0" step="1" :disabled="form.lotStdQtyMode !== 'set'" /></div></label>
              <label class="field"><span>Unit Type *</span><div class="prd-control"><input v-model="form.unitType" readonly /><button type="button" class="prd-button" @click="openLookup('unitType')">...</button></div></label>
              <label class="field"><span>Unit No *</span><div class="prd-control"><input v-model="form.unitNo" readonly /><button type="button" class="prd-button" @click="openLookup('unitNo')">...</button></div></label>
              <label class="field"><span>SAP_KTEXT</span><input v-model="form.sapKtext" /></label>
              <label class="field"><span>SAP_BOMAENNR</span><select v-model="form.sapBomAennr"><option value="">(無)</option><option>DEV-CHANGE-01</option><option>DEV-CHANGE-02</option></select></label>
              <label class="field"><span>SAP_AENNR</span><input v-model="form.sapAennr" /></label>
              <label class="field"><span>SAP_DATUV</span><input v-model="form.sapDatuv" /></label>
              <label class="field"><span>XX_SOPSPEC</span><input v-model="form.xxSopSpec" /></label>
              <label class="field"><span>XX_SOPREV</span><input v-model="form.xxSopRev" /></label>
              <label class="field"><span>SAP_PLNAL *</span><select v-model="form.sapPlnal" required><option value="">請選擇</option><option v-for="option in sapPlnalOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
              <label class="field"><span>ERPNo</span><input v-model="form.erpNo" /></label>
              <label class="field wide"><span>Description</span><textarea v-model="form.description" rows="3" /></label>
              <label class="field"><span>SerialTypeNo_Lot</span><div class="prd-control"><input v-model="form.serialTypeNoLot" readonly /><button type="button" class="prd-button" @click="openLookup('serialTypeNoLot')">...</button></div></label>
              <label class="field"><span>SerialTypeNo_Comp</span><div class="prd-control"><input v-model="form.serialTypeNoComp" readonly /><button type="button" class="prd-button" @click="openLookup('serialTypeNoComp')">...</button></div></label>
              <label class="field"><span>QCCategory</span><input v-model="form.qccategory" /></label>
              <label class="field"><span>ItemSpec</span><input v-model="form.itemSpec" /></label>
              <label class="field"><span>CustomerNo</span><input v-model="form.customerNo" /></label>
              <label class="field"><span>DeviceNo</span><input v-model="form.deviceNo" /></label>
              <label class="field"><span>ECNControl</span><select v-model="form.ecnControl"><option value="N">N</option><option value="Y">Y</option></select></label>
            </div>
            <div v-if="picturePreview" class="prd-picture-preview"><img :src="picturePreview" alt="產品圖片預覽" /></div>
          </fieldset>
          <fieldset v-if="mode === 'copy' || mode === 'version' || mode === 'add'" class="prd-fieldset"><legend>Detail Source</legend>
            <div class="prd-source-options"><label><input v-model="detailSource" type="radio" value="productType" />Copy from ProductType</label><label><input v-model="detailSource" type="radio" value="otherProduct" />Copy from Other Product</label><label><input v-model="detailSource" type="radio" value="none" />None</label>
              <span>ProductNo <input readonly :value="sourceProductKey.split('|')[0] || '—'" /></span><span>ProductVersion <input readonly :value="sourceProductKey.split('|')[1] || '—'" /></span>
            </div>
          </fieldset>
          <div class="prd-audit"><label>建立人<input readonly :value="form.creator || currentUser() || '—'" /></label><label>建立日期<input readonly :value="formatDate(form.createDate) || '—'" /></label><label>修改人<input readonly :value="form.reviser || '—'" /></label><label>修改日期<input readonly :value="formatDate(form.reviseDate) || '—'" /></label></div>
        </div>
        <footer class="prd-form-footer"><button class="prd-button confirm" type="submit" :disabled="busy">確定</button><button class="prd-button" type="button" :disabled="busy" @click="closeForm">關閉</button><button class="prd-button" type="button" :disabled="busy" @click="saveForm(false)">套用</button></footer>
      </form>
    </div>

    <div v-if="lookupField" class="prd-overlay lookup-overlay" @click.self="lookupField=''">
      <section class="prd-feature-dialog"><header><h2>選擇 {{ lookupField }}</h2><button type="button" @click="lookupField=''">×</button></header><div class="prd-lookup-list"><button v-for="value in lookupOptions" :key="value" class="prd-button" type="button" @click="chooseLookup(value)">{{ value }}</button></div><p class="prd-dev-note">以上為 Development 假資料選項。</p></section>
    </div>

    <div v-if="feature && featureKey === 'process-setup'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductProcessEditor :product="active ?? {}" :processes="activeRelations.processes" :development-preview="mock" @save="saveProductProcess" @close="closeFeature" />
    </div>
    <div v-if="feature && featureKey === 'bom-editor'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductBomEditor :product="active ?? {}" :bom="activeRelations.bom" :material-groups="activeRelations.materialGroups ?? []" :products="rows" :relations="relations" :development-preview="mock" @save="saveProductBom" @close="closeFeature" />
    </div>
    <div v-if="feature && featureKey === 'property'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductPropertyMaster :product="active ?? {}" :properties="activeRelations.properties" :development-preview="mock" :can-edit="editable" @save="saveProductProperties" @close="closeFeature" />
    </div>
    <div v-if="feature && featureKey === 'parameter'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductParameterSetup :product="active ?? {}" :processes="activeRelations.processes" :parameters="activeRelations.parameters ?? null" :development-preview="mock" :can-edit="editable" @save="saveProductParameters" @close="closeFeature" />
    </div>
    <div v-if="feature && featureKey === 'business-rule'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductBusinessRuleSetup :product="active ?? {}" :processes="activeRelations.processes" :business-rules="activeRelations.businessRules ?? null" :development-preview="mock" :can-edit="editable" @save="saveProductBusinessRules" @close="closeFeature" />
    </div>
    <div v-if="feature && featureKey === 'op-description'" class="prd-overlay child-feature-overlay" @click.self="closeFeature">
      <ProductOpDescriptionSetup :product="active ?? {}" :processes="activeRelations.processes" :op-descriptions="activeRelations.opDescriptions ?? null" :development-preview="mock" :can-edit="editable" @save="saveProductOpDescriptions" @close="closeFeature" />
    </div>
    <div v-if="feature && !['process-setup','bom-editor','property','parameter','business-rule','op-description'].includes(featureKey)" class="prd-overlay" @click.self="closeFeature">
      <section class="prd-feature-dialog">
        <header><div><small>{{ feature.source }}</small><h2>{{ feature.title }}</h2></div><button type="button" aria-label="關閉" @click="closeFeature">×</button></header>
        <p class="prd-context">產品 {{ active?.productNo }} / {{ active?.productVersion }}　{{ active?.productName }}</p>
        <p>{{ feature.description }}</p>
        <p v-if="featureKey === 'process-setup' || featureKey === 'process-expand'" class="prd-context">流程 {{ selectedProcess?.psNo || '—' }} / MOType {{ selectedProcess?.moTypeNo ?? '—' }} / {{ selectedProcess?.processNo || '—' }} / {{ selectedProcess?.processVersion || '—' }}</p>
        <template v-if="featureKey === 'eqp-cap-item' || featureKey === 'equipment-spec'">
          <div class="prd-detail-grid"><table><thead><tr><th>OPNo</th><th>EquipmentNo</th><th>Description</th></tr></thead><tbody><tr v-for="(item,index) in activeRelations.constraints" :key="index"><td>{{ item.opNo }}</td><td>{{ item.equipmentNo }}</td><td>{{ item.description }}</td></tr><tr v-if="!activeRelations.constraints.length"><td colspan="3" class="empty">尚無設備資料。</td></tr></tbody></table></div>
        </template>
        <div v-else class="prd-feature-placeholder"><strong>WinForms 功能入口已依原功能名稱保留。</strong><p>此子功能由另一個 ObjectFileHandler/WinForms Form 載入；其專屬欄位、資料表與操作會在對應功能規格中拆分，API 尚待家用端實作。</p><p>Development 目前只展示產品上下文，不會寫入 MES 或 YMES 正式資料。</p></div>
        <footer><button class="prd-button" type="button" @click="closeFeature">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.prd-master{min-width:0;height:100%;padding:9px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;display:flex;flex-direction:column;gap:6px}
.child-feature-overlay{z-index:91}
.prd-page-title{display:flex;gap:16px;align-items:center;padding:0 4px 5px;border-bottom:1px solid #e2dfd4;font-size:16px}.prd-page-title span{font-size:13px;color:#655f4d}
.prd-dev-note{margin:0;padding:5px 8px;border:1px solid #e4d7a3;background:#fff8dc;color:#5f5427}
.prd-query{display:flex;align-items:center;gap:8px;min-height:39px;padding:4px 7px;border:1px solid #c9c7bd;background:#faf9f5}.prd-state{display:flex;gap:12px;margin:0;padding:0 4px;border:0}.prd-state label{display:flex;align-items:center;gap:3px}.prd-state input{accent-color:#b4831d}
.prd-search{display:flex;align-items:center;gap:6px}.prd-search input{width:180px;height:26px;border:1px solid #aaa797;background:#fff;padding:3px 6px}
.prd-button{min-height:28px;padding:4px 8px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f3f1eb);color:#25241e;font:inherit;white-space:nowrap;cursor:pointer}.prd-button:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.prd-button:disabled{opacity:.5;cursor:not-allowed}.prd-button.confirm{background:#f7a92c;border-color:#e1971f;color:#fff;font-weight:600}
.prd-message{padding:5px 8px;border:1px solid #d6c793;background:#fffbeb}.prd-message.error{border-color:#d29a8e;background:#fff1ed;color:#7c281d}
.prd-workspace{display:grid;grid-template-columns:minmax(0,1fr) 114px;gap:8px;flex:1;min-height:0}
.prd-main-column{min-width:0;display:flex;flex-direction:column;min-height:0}
.prd-grid-wrap{height:min(43vh,380px);min-height:190px;overflow:auto;border:1px solid #aaa79f}.prd-grid,.prd-detail-grid table{border-collapse:collapse;white-space:nowrap;font-size:12px}.prd-grid th,.prd-detail-grid th{height:24px;padding:3px 7px;border:1px solid #c9c5b7;background:#f0b023;color:#27251e;font-weight:600;text-align:left}.prd-grid td,.prd-detail-grid td{height:25px;padding:3px 7px;border:1px solid #d4d1c8;background:#fff}.prd-grid tbody tr:nth-child(even) td{background:#faf9f5}.prd-grid tbody tr.focused td{background:#ffeaa1}.prd-grid tbody tr:hover td{background:#fff1bd}.prd-grid .select-col{position:sticky;left:0;z-index:2;background:#f5f2e8}.prd-grid th.select-col{z-index:3}.prd-grid td.empty,.prd-detail-grid td.empty{padding:12px;text-align:center;color:#777}
.prd-details{margin-top:6px;border:1px solid #aaa79f;min-height:140px;display:flex;flex-direction:column}.prd-tabs{display:flex;height:29px;border-bottom:1px solid #c8c4b7;background:#f5f3ec}.prd-tabs button{min-width:90px;padding:4px 11px;border:0;border-right:1px solid #d7d3c7;background:transparent;font:inherit;cursor:pointer}.prd-tabs button.active{background:#ffedba;border-bottom:2px solid #d69d22;font-weight:600}.prd-detail-content{min-height:105px;display:flex;flex-direction:column}.prd-detail-actions{display:flex;gap:5px;padding:5px}.prd-detail-grid{overflow:auto;flex:1;min-height:65px}.prd-detail-empty{padding:24px;text-align:center;color:#777}.prd-count{padding:5px 3px;color:#5f5b50}.prd-actions{min-height:0;overflow:auto;display:flex;flex-direction:column;align-items:stretch;gap:4px;padding:1px 3px}.prd-actions .prd-button{min-height:31px}
.prd-detail-grid tbody tr.focused td{background:#ffeaa1}.prd-detail-grid tbody tr{cursor:pointer}
.prd-bom-tree{overflow:auto;flex:1;min-height:65px;padding:4px 2px;font-size:12px}.prd-bom-line{min-height:21px;line-height:21px;white-space:nowrap}.prd-bom-root-icon{display:inline-block;width:18px;color:#666}.prd-bom-branch{display:inline-block;width:18px;color:#888}.prd-bom-line.bom-material{color:darkred}.prd-bom-line.bom-product{color:blue}.prd-bom-line.bom-group{color:green}.prd-bom-empty{padding:14px;text-align:center;color:#777}
.prd-overlay{position:fixed;inset:0;z-index:90;display:grid;place-items:center;background:#0005;padding:12px}.prd-form,.prd-feature-dialog{width:min(766px,calc(100vw - 24px));max-height:calc(100vh - 24px);display:flex;flex-direction:column;background:#fdfcf9;border:1px solid #a9a69d;box-shadow:0 8px 30px #0004}.prd-form-header,.prd-feature-dialog>header{display:flex;justify-content:space-between;align-items:center;padding:10px 14px;border-bottom:1px solid #d7d3c7;background:#f7f5ef}.prd-form-header h2,.prd-feature-dialog h2{margin:3px 0;font-size:17px}.prd-form-header small,.prd-feature-dialog small{color:#797461}.prd-form-header>button,.prd-feature-dialog header>button{border:0;background:transparent;font-size:22px;cursor:pointer}.prd-form-scroll{overflow:auto;padding:8px 12px}.prd-fieldset{margin:0 0 8px;padding:10px 10px 12px;border:1px solid #b7b4ab}.prd-fieldset legend{padding:0 5px;font-weight:600;color:#514d40}.prd-form-grid{display:grid;grid-template-columns:1fr 1fr;column-gap:14px;row-gap:8px}.field{display:flex;flex-direction:column;gap:3px;min-width:0}.field>span{color:#433f34}.field input,.field select,.field textarea,.prd-audit input,.prd-source-options input{min-width:0;height:25px;border:1px solid #99968d;background:#fff2bd;padding:3px 6px;color:#292820;font:inherit}.field input:read-only{background:#efeee9}.field textarea{height:auto;resize:vertical}.field.wide{grid-column:1 / -1}.prd-control{display:flex;gap:4px}.prd-control input{flex:1}.prd-control button{min-width:32px}.prd-lotqty{display:flex;align-items:center;gap:8px;min-height:25px}.prd-lotqty label,.prd-source-options label{display:flex;align-items:center;gap:3px}.prd-lotqty input[type=radio],.prd-source-options input[type=radio]{accent-color:#b4831d}.prd-lotqty>input{width:100px}.prd-lotqty>input:disabled{background:#efeee9}.prd-source-options{display:flex;align-items:center;flex-wrap:wrap;gap:10px}.prd-source-options span{display:flex;align-items:center;gap:4px}.prd-source-options span input{width:120px}.prd-audit{display:grid;grid-template-columns:1fr 1fr;gap:7px;padding:8px;border:1px solid #b7b4ab}.prd-audit label{display:flex;align-items:center;gap:8px}.prd-audit input{flex:1;background:#eee}.prd-picture-preview{margin-top:8px;display:flex;justify-content:center}.prd-picture-preview img{max-width:250px;max-height:130px;object-fit:contain;border:1px solid #ccc}.file-button{position:relative;display:grid;place-items:center;min-width:34px}.file-button input{position:absolute;inset:0;width:100%;height:100%;opacity:0;cursor:pointer}.prd-form-footer,.prd-feature-dialog footer{display:flex;justify-content:flex-end;gap:7px;padding:8px 12px;border-top:1px solid #d7d3c7}.prd-feature-dialog{padding-bottom:0}.prd-feature-dialog>p,.prd-feature-placeholder{margin:10px 14px}.prd-context{padding:7px;background:#fff4ce;border:1px solid #e0d3a2}.prd-feature-placeholder{padding:14px;border:1px solid #d7d3c7;background:#faf9f5;line-height:1.6}.prd-feature-dialog .prd-detail-grid{margin:0 14px 8px;max-height:300px}.prd-detail-grid input{min-width:110px;border:1px solid #aaa;background:#fff2bd;padding:4px;font:inherit}.lookup-overlay{z-index:100}.prd-lookup-list{display:flex;gap:6px;flex-wrap:wrap;padding:12px 14px}
@media(max-width:1100px){.prd-workspace{grid-template-columns:minmax(0,1fr) 102px}.prd-actions .prd-button{white-space:normal}.prd-grid-wrap{height:38vh}}
@media(max-width:760px){.prd-workspace{grid-template-columns:minmax(0,1fr)}.prd-actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));max-height:190px}.prd-query{flex-wrap:wrap}.prd-state{flex-wrap:wrap}.prd-form-grid{grid-template-columns:1fr}.field.wide{grid-column:auto}.prd-audit{grid-template-columns:1fr}.prd-details{min-height:200px}}
</style>

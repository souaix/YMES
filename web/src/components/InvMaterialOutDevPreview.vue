<script setup lang="ts">
import { computed, ref } from 'vue'
const emit = defineEmits<{ close: [] }>()

type PutInPlaceType = 0 | 1 | 2 | 3 | 4
type DialogMode = 'basis-add' | 'basis-edit' | 'basis-copy' | 'detail-add' | 'detail-edit' | 'detail-copy' | 'delete-basis' | 'delete-detail' | 'print' | null
interface MaterialOutBasis {
  materialOutNo: string
  inventoryNo: string
  preOrderNo: string
  state: number
  confirmUserNo: string
  description: string
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface ComponentRow {
  componentId: string
  componentNo: string
  componentLotNo: string
  laserMark: string
  cassetteNo: string
  slotNo: string
  goodQty: number
  scrapQty: number
  unitNo: string
}
interface MaterialOutDetail {
  key: string
  materialOutNo: string
  materialNo: string
  materialName: string
  materialLotNo: string
  locatorNo: string
  vendorNo: string
  putInPlaceType: PutInPlaceType
  putInPlaceNo: string
  unitNo: string
  qty: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
  components: ComponentRow[]
}
interface StockComponent extends ComponentRow { bookingFlag: 0 | 1 }
interface StockLot {
  stockId: string
  inventoryNo: string
  materialNo: string
  materialName: string
  materialLotNo: string
  locatorNo: string
  unitNo: string
  vendorNo: string
  curQty: number
  allocateQty: number
  components: StockComponent[]
}
interface PreOrder { preOrderNo: string; inventoryNo: string; state: number; description: string }
interface DevState { bases: MaterialOutBasis[]; details: MaterialOutDetail[] }
interface BasisDraft { materialOutNo: string; inventoryNo: string; preOrderNo: string; description: string }
interface DetailDraft {
  stockId: string
  putInPlaceType: PutInPlaceType | ''
  putInPlaceNo: string
  qty: string
}

const storageKey = 'ymes.dev.inv.material-out.v1'
const inventories = [
  { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉' },
  { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉' },
]
const preOrders: PreOrder[] = [
  { preOrderNo: 'PRE-DEV-26001', inventoryNo: 'RAW-A', state: 2, description: '核准預訂：Resin line' },
  { preOrderNo: 'PRE-DEV-26002', inventoryNo: 'RAW-A', state: 1, description: '處理中預訂（不可選）' },
  { preOrderNo: 'PRE-DEV-26003', inventoryNo: 'RAW-B', state: 2, description: '核准預訂：Copper lot' },
]
const stockLots: StockLot[] = [
  { stockId: 'raw-a-res-001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', unitNo: 'KG', vendorNo: 'V-DEV-01', curQty: 120, allocateQty: 25, components: [] },
  { stockId: 'raw-a-cu-010', inventoryNo: 'RAW-A', materialNo: 'RM-COPPER-03', materialName: '銅箔材料 C', materialLotNo: 'CU-2611-010', locatorNo: 'A-02-03', unitNo: 'RL', vendorNo: 'V-DEV-02', curQty: 48, allocateQty: 8, components: [] },
  { stockId: 'raw-a-sensor-005', inventoryNo: 'RAW-A', materialNo: 'RM-SENSOR-05', materialName: '感測器模組 S', materialLotNo: 'SNS-2611-005', locatorNo: 'A-03-01', unitNo: 'EA', vendorNo: 'V-DEV-01', curQty: 4, allocateQty: 0, components: [
    { componentId: 'sns-001', componentNo: 'COMP-SNS-A', componentLotNo: 'C-A-001', laserMark: 'LASER-A1', cassetteNo: 'CAS-01', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', bookingFlag: 0 },
    { componentId: 'sns-002', componentNo: 'COMP-SNS-B', componentLotNo: 'C-B-002', laserMark: 'LASER-B2', cassetteNo: 'CAS-01', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', bookingFlag: 0 },
    { componentId: 'sns-003', componentNo: 'COMP-SNS-C', componentLotNo: 'C-C-003', laserMark: 'LASER-C3', cassetteNo: 'CAS-02', slotNo: '01', goodQty: 1, scrapQty: 0, unitNo: 'EA', bookingFlag: 0 },
    { componentId: 'sns-004', componentNo: 'COMP-SNS-D', componentLotNo: 'C-D-004', laserMark: 'LASER-D4', cassetteNo: 'CAS-02', slotNo: '02', goodQty: 1, scrapQty: 0, unitNo: 'EA', bookingFlag: 0 },
  ] },
  { stockId: 'raw-b-cu-020', inventoryNo: 'RAW-B', materialNo: 'RM-COPPER-03', materialName: '銅箔材料 C', materialLotNo: 'CU-2611-020', locatorNo: 'B-01-02', unitNo: 'RL', vendorNo: 'V-DEV-02', curQty: 32, allocateQty: 4, components: [] },
]
const targetOptions: Record<PutInPlaceType, { value: string; label: string }[]> = {
  0: [{ value: 'EQP-DEV-01', label: 'EQP-DEV-01 · 組裝機台 A' }, { value: 'EQP-DEV-02', label: 'EQP-DEV-02 · 包裝機台 B' }],
  1: [{ value: 'OP-DEV-01', label: 'OP-DEV-01 · 操作員 A' }, { value: 'OP-DEV-02', label: 'OP-DEV-02 · 操作員 B' }],
  2: [{ value: 'WIP-DEV-A', label: 'WIP-DEV-A · 在製庫 A' }],
  3: [{ value: 'MO-DEV-9001', label: 'MO-DEV-9001 · 已下線工單 A' }, { value: 'MO-DEV-9002', label: 'MO-DEV-9002 · 已下線工單 B' }],
  4: [],
}
const moMaterialMap: Record<string, string[]> = {
  'MO-DEV-9001': ['RM-RESIN-01', 'RM-SENSOR-05'],
  'MO-DEV-9002': ['RM-COPPER-03'],
}
const putInPlaceLabels: Record<PutInPlaceType, string> = { 0: 'EQP', 1: 'OP', 2: 'WIP INV', 3: 'MO', 4: 'Unproductive' }

const initialState: DevState = {
  bases: [
    { materialOutNo: 'MO-DEV-26001', inventoryNo: 'RAW-A', preOrderNo: 'PRE-DEV-26001', state: 0, confirmUserNo: '', description: 'DEV 假資料：依核准預訂領用樹脂。', creator: 'DEV001', createDate: '2026-11-20 09:12:00', reviser: '', reviseDate: '', dataStamp: 0 },
    { materialOutNo: 'MO-DEV-26002', inventoryNo: 'RAW-A', preOrderNo: '', state: 0, confirmUserNo: '', description: 'DEV 假資料：一般領料，尚未確認。', creator: 'DEV002', createDate: '2026-11-22 14:36:00', reviser: '', reviseDate: '', dataStamp: 0 },
    { materialOutNo: 'MO-DEV-25998', inventoryNo: 'RAW-B', preOrderNo: '', state: 2, confirmUserNo: 'DEV003', description: '已確認歷史單，主畫面不載入。', creator: 'DEV003', createDate: '2026-11-18 11:20:00', reviser: '', reviseDate: '', dataStamp: 1 },
  ],
  details: [
    { key: 'MO-DEV-26001|RM-RESIN-01|RES-2611-001|A-01-01|1|OP-DEV-01', materialOutNo: 'MO-DEV-26001', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2611-001', locatorNo: 'A-01-01', vendorNo: 'V-DEV-01', putInPlaceType: 1, putInPlaceNo: 'OP-DEV-01', unitNo: 'KG', qty: 5, creator: 'DEV001', createDate: '2026-11-20 09:20:00', reviser: '', reviseDate: '', dataStamp: 0, components: [] },
    { key: 'MO-DEV-26002|RM-COPPER-03|CU-2611-010|A-02-03|3|MO-DEV-9002', materialOutNo: 'MO-DEV-26002', materialNo: 'RM-COPPER-03', materialName: '銅箔材料 C', materialLotNo: 'CU-2611-010', locatorNo: 'A-02-03', vendorNo: 'V-DEV-02', putInPlaceType: 3, putInPlaceNo: 'MO-DEV-9002', unitNo: 'RL', qty: 2, creator: 'DEV002', createDate: '2026-11-22 14:45:00', reviser: '', reviseDate: '', dataStamp: 0, components: [] },
  ],
}

const records = ref<DevState>(loadState())
const searchText = ref('')
const appliedSearch = ref('')
const selectedBasisNo = ref('')
const selectedBasisNos = ref<string[]>([])
const selectedDetailKeys = ref<string[]>([])
const expandedDetailKeys = ref<string[]>([])
const dialogMode = ref<DialogMode>(null)
const notice = ref('DEV 假資料：單頭／明細只保存在本機；此頁不執行確認或庫存扣帳。')
const pendingDeleteBasisNos = ref<string[]>([])
const pendingDeleteDetailKeys = ref<string[]>([])
const basisDraft = ref<BasisDraft>({ materialOutNo: '', inventoryNo: '', preOrderNo: '', description: '' })
const detailDraft = ref<DetailDraft>({ stockId: '', putInPlaceType: '', putInPlaceNo: '', qty: '' })
const componentSelection = ref<string[]>([])
const editingDetailKey = ref('')

const visibleBases = computed(() => records.value.bases
  .filter((row) => row.state === 0 && (!appliedSearch.value || row.materialOutNo.toLowerCase().includes(appliedSearch.value.toLowerCase())))
  .slice().sort((a, b) => a.materialOutNo.localeCompare(b.materialOutNo)))
const activeBasis = computed(() => visibleBases.value.find((row) => row.materialOutNo === selectedBasisNo.value) ?? visibleBases.value[0] ?? null)
const visibleDetails = computed(() => activeBasis.value
  ? records.value.details.filter((row) => row.materialOutNo === activeBasis.value?.materialOutNo)
    .slice().sort((a, b) => [a.materialNo, a.materialLotNo, a.locatorNo, a.putInPlaceType, a.putInPlaceNo].join('|').localeCompare([b.materialNo, b.materialLotNo, b.locatorNo, b.putInPlaceType, b.putInPlaceNo].join('|')))
  : [])
const activeDetail = computed(() => visibleDetails.value.find((row) => row.key === selectedDetailKeys.value[0]) ?? visibleDetails.value[0] ?? null)
const stockOptions = computed(() => stockLots.filter((row) => row.inventoryNo === activeBasis.value?.inventoryNo))
const selectedStock = computed(() => stockLots.find((row) => row.stockId === detailDraft.value.stockId) ?? null)
const selectedTargetOptions = computed(() => detailDraft.value.putInPlaceType === '' ? [] : targetOptions[detailDraft.value.putInPlaceType])
const availablePreOrders = computed(() => preOrders.filter((row) => row.inventoryNo === basisDraft.value.inventoryNo && row.state === 2))
const detailComponentOptions = computed(() => {
  if (!selectedStock.value) return []
  const bookedElsewhere = new Set(records.value.details
    .filter((row) => row.key !== editingDetailKey.value)
    .flatMap((row) => row.components.map((item) => item.componentId)))
  return selectedStock.value.components.filter((item) => !bookedElsewhere.has(item.componentId) || componentSelection.value.includes(item.componentId))
})
const basisDialogTitle = computed(() => dialogMode.value === 'basis-edit' ? 'Edit MaterialOut Basis' : dialogMode.value === 'basis-copy' ? 'Copy MaterialOut Basis' : 'Add MaterialOut Basis')
const detailDialogTitle = computed(() => dialogMode.value === 'detail-edit' ? 'Edit MaterialOut Detail' : dialogMode.value === 'detail-copy' ? 'Copy MaterialOut Detail' : 'Add MaterialOut Detail')
const currentTime = () => new Date().toLocaleString('sv-SE')

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function loadState(): DevState {
  if (typeof window === 'undefined') return clone(initialState)
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) return clone(initialState)
    const parsed = JSON.parse(raw) as DevState
    if (!Array.isArray(parsed.bases) || !Array.isArray(parsed.details)) return clone(initialState)
    return parsed
  } catch {
    return clone(initialState)
  }
}
function persistState(message: string): void {
  try {
    window.localStorage.setItem(storageKey, JSON.stringify(records.value))
    notice.value = message
  } catch {
    notice.value = '本機儲存不可用；目前變更只保留在本次頁面。'
  }
}
function runQuery(): void {
  appliedSearch.value = searchText.value.trim()
  selectedBasisNo.value = visibleBases.value[0]?.materialOutNo ?? ''
  selectedBasisNos.value = []
  selectedDetailKeys.value = []
  notice.value = `DEV 假資料查詢完成：${visibleBases.value.length} 張未確認領料單。`
}
function selectBasis(row: MaterialOutBasis): void {
  selectedBasisNo.value = row.materialOutNo
  selectedDetailKeys.value = []
}
function toggleBasisSelection(no: string): void {
  selectedBasisNos.value = selectedBasisNos.value.includes(no) ? selectedBasisNos.value.filter((item) => item !== no) : [...selectedBasisNos.value, no]
}
function toggleDetailSelection(key: string): void {
  selectedDetailKeys.value = selectedDetailKeys.value.includes(key) ? selectedDetailKeys.value.filter((item) => item !== key) : [key, ...selectedDetailKeys.value.filter((item) => item !== key)]
}
function activateDetail(row: MaterialOutDetail): void { selectedDetailKeys.value = [row.key] }
function toggleExpandedDetail(key: string): void {
  expandedDetailKeys.value = expandedDetailKeys.value.includes(key) ? expandedDetailKeys.value.filter((item) => item !== key) : [...expandedDetailKeys.value, key]
}
function stateName(state: number): string { return state === 0 ? 'UnConfirm' : state === 1 ? 'Handling' : state === 2 ? 'Confirm' : 'Fail' }
function placeName(type: PutInPlaceType): string { return putInPlaceLabels[type] }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function inventoryName(no: string): string { return inventories.find((row) => row.inventoryNo === no)?.inventoryName ?? no }
function availableQty(stock: StockLot): number { return stock.curQty - stock.allocateQty }
function displayStock(stock: StockLot): string { return `${stock.materialNo} · ${stock.materialLotNo} · ${stock.locatorNo} (${stock.unitNo})` }
function nextMaterialOutNo(): string {
  const current = records.value.bases.map((row) => Number(row.materialOutNo.match(/(\d+)$/)?.[1] ?? 0)).reduce((max, value) => Math.max(max, value), 26000)
  return `MO-DEV-${String(current + 1).padStart(5, '0')}`
}

function openBasisDialog(mode: 'add' | 'edit' | 'copy'): void {
  const selected = activeBasis.value
  if ((mode === 'edit' || mode === 'copy') && !selected) return
  if (mode === 'edit' && selected?.state !== 0) return
  basisDraft.value = mode === 'edit' && selected
    ? { materialOutNo: selected.materialOutNo, inventoryNo: selected.inventoryNo, preOrderNo: selected.preOrderNo, description: selected.description }
    : { materialOutNo: nextMaterialOutNo(), inventoryNo: mode === 'copy' ? selected?.inventoryNo ?? '' : '', preOrderNo: mode === 'copy' ? selected?.preOrderNo ?? '' : '', description: mode === 'copy' ? selected?.description ?? '' : '' }
  dialogMode.value = mode === 'add' ? 'basis-add' : mode === 'edit' ? 'basis-edit' : 'basis-copy'
}
function changeBasisInventory(): void {
  if (!availablePreOrders.value.some((row) => row.preOrderNo === basisDraft.value.preOrderNo)) basisDraft.value.preOrderNo = ''
}
function saveBasis(): void {
  const draft = basisDraft.value
  if (!draft.materialOutNo || !draft.inventoryNo) { notice.value = 'MaterialOutNo 和 InventoryNo 為必填欄位。'; return }
  const isNew = dialogMode.value === 'basis-add' || dialogMode.value === 'basis-copy'
  if (isNew && records.value.bases.some((row) => row.materialOutNo === draft.materialOutNo)) { notice.value = '單號重複，請重新取得 DEV 假單號。'; return }
  if (draft.preOrderNo && !availablePreOrders.value.some((row) => row.preOrderNo === draft.preOrderNo)) { notice.value = '預訂單必須是同一庫房且 State=2 的預訂單。'; return }
  if (isNew) {
    const row: MaterialOutBasis = { ...draft, state: 0, confirmUserNo: '', creator: 'DEV001', createDate: currentTime(), reviser: '', reviseDate: '', dataStamp: 0 }
    records.value.bases.push(row)
    selectedBasisNo.value = row.materialOutNo
    appliedSearch.value = ''
    searchText.value = ''
    persistState(dialogMode.value === 'basis-copy' ? '已建立單頭複本；來源明細不會一併複製。' : '已建立 DEV 未確認領料單。')
  } else {
    const row = records.value.bases.find((item) => item.materialOutNo === draft.materialOutNo)
    if (!row || row.state !== 0) { notice.value = '只有 State=0 單頭可修改。'; return }
    row.preOrderNo = draft.preOrderNo
    row.description = draft.description
    row.reviser = 'DEV001'
    row.reviseDate = currentTime()
    row.dataStamp += 1
    persistState('已更新 DEV 單頭；InventoryNo 和 MaterialOutNo 維持不變。')
  }
  dialogMode.value = null
}

function requestDeleteBasis(): void {
  const ids = selectedBasisNos.value.length ? selectedBasisNos.value : activeBasis.value ? [activeBasis.value.materialOutNo] : []
  pendingDeleteBasisNos.value = ids.filter((no) => visibleBases.value.some((row) => row.materialOutNo === no && row.state === 0))
  if (pendingDeleteBasisNos.value.length) dialogMode.value = 'delete-basis'
}
function confirmDeleteBasis(): void {
  const deleting = new Set(pendingDeleteBasisNos.value)
  records.value.bases = records.value.bases.filter((row) => !deleting.has(row.materialOutNo))
  records.value.details = records.value.details.filter((row) => !deleting.has(row.materialOutNo))
  selectedBasisNos.value = []
  selectedBasisNo.value = ''
  selectedDetailKeys.value = []
  persistState(`已刪除 ${deleting.size} 張 DEV 單頭及其明細、元件假資料。`)
  dialogMode.value = null
  pendingDeleteBasisNos.value = []
}

function openDetailDialog(mode: 'add' | 'edit' | 'copy'): void {
  const basis = activeBasis.value
  const selected = activeDetail.value
  if (!basis) return
  if ((mode === 'edit' || mode === 'copy') && !selected) return
  if (mode === 'edit' && basis.state !== 0) return
  editingDetailKey.value = mode === 'edit' ? selected?.key ?? '' : ''
  componentSelection.value = mode === 'edit' ? selected?.components.map((item) => item.componentId) ?? [] : []
  detailDraft.value = mode === 'edit' && selected
    ? { stockId: stockLots.find((stock) => stock.inventoryNo === basis.inventoryNo && stock.materialNo === selected.materialNo && stock.materialLotNo === selected.materialLotNo && stock.locatorNo === selected.locatorNo)?.stockId ?? '', putInPlaceType: selected.putInPlaceType, putInPlaceNo: selected.putInPlaceNo, qty: String(selected.qty) }
    : mode === 'copy' && selected
      ? { stockId: stockLots.find((stock) => stock.inventoryNo === basis.inventoryNo && stock.materialNo === selected.materialNo && stock.materialLotNo === selected.materialLotNo && stock.locatorNo === selected.locatorNo)?.stockId ?? '', putInPlaceType: selected.putInPlaceType, putInPlaceNo: selected.putInPlaceNo, qty: selected.components.length ? '0' : String(selected.qty) }
      : { stockId: '', putInPlaceType: '', putInPlaceNo: '', qty: '' }
  dialogMode.value = mode === 'add' ? 'detail-add' : mode === 'edit' ? 'detail-edit' : 'detail-copy'
}
function onStockChanged(): void {
  const stock = selectedStock.value
  componentSelection.value = []
  if (!stock) return
  detailDraft.value.qty = stock.components.length ? '0' : '1'
}
function toggleComponent(id: string): void {
  componentSelection.value = componentSelection.value.includes(id) ? componentSelection.value.filter((item) => item !== id) : [...componentSelection.value, id]
  if (selectedStock.value?.components.length) detailDraft.value.qty = String(componentSelection.value.length)
}
function onPlaceTypeChanged(): void {
  const type = detailDraft.value.putInPlaceType
  detailDraft.value.putInPlaceNo = type === '' || type === 4 ? '' : targetOptions[type][0]?.value ?? ''
}
function makeDetailKey(row: Pick<MaterialOutDetail, 'materialOutNo' | 'materialNo' | 'materialLotNo' | 'locatorNo' | 'putInPlaceType' | 'putInPlaceNo'>): string {
  return [row.materialOutNo, row.materialNo, row.materialLotNo, row.locatorNo, row.putInPlaceType, row.putInPlaceNo].join('|')
}
function saveDetail(): void {
  const basis = activeBasis.value
  const stock = selectedStock.value
  const draft = detailDraft.value
  const qty = Number(draft.qty)
  if (!basis || basis.state !== 0) { notice.value = '只有 State=0 單頭可以維護明細。'; return }
  if (!stock || draft.putInPlaceType === '' || !draft.putInPlaceNo || !Number.isFinite(qty) || qty <= 0) { notice.value = '物料批號、去向、去向編號及大於 0 的數量均為必填。'; return }
  if (draft.putInPlaceType !== 4 && !targetOptions[draft.putInPlaceType].some((row) => row.value === draft.putInPlaceNo)) { notice.value = '此 DEV 去向不存在或不是有效主檔。'; return }
  if (draft.putInPlaceType === 3 && !(moMaterialMap[draft.putInPlaceNo] ?? []).includes(stock.materialNo)) { notice.value = '此物料不在所選工單可領用的物料／替代料清單中。'; return }
  if (stock.components.length && (componentSelection.value.length === 0 || qty !== componentSelection.value.length || !Number.isInteger(qty))) { notice.value = '含元件的批號需至少選一筆元件，且明細 Qty 必須等於選取元件筆數。'; return }
  const components = detailComponentOptions.value.filter((item) => componentSelection.value.includes(item.componentId)).map((item) => ({ componentId: item.componentId, componentNo: item.componentNo, componentLotNo: item.componentLotNo, laserMark: item.laserMark, cassetteNo: item.cassetteNo, slotNo: item.slotNo, goodQty: item.goodQty, scrapQty: item.scrapQty, unitNo: item.unitNo }))
  const fields = { materialOutNo: basis.materialOutNo, materialNo: stock.materialNo, materialName: stock.materialName, materialLotNo: stock.materialLotNo, locatorNo: stock.locatorNo, vendorNo: stock.vendorNo, putInPlaceType: draft.putInPlaceType as PutInPlaceType, putInPlaceNo: draft.putInPlaceNo, unitNo: stock.unitNo, qty, components }
  if (dialogMode.value === 'detail-edit') {
    const row = records.value.details.find((item) => item.key === editingDetailKey.value)
    if (!row) { notice.value = '找不到要修改的 DEV 明細。'; return }
    const nextKey = makeDetailKey({ ...row, ...fields })
    if (records.value.details.some((item) => item.key !== row.key && item.key === nextKey)) { notice.value = '相同單據、物料、批號、儲位與去向已存在。'; return }
    Object.assign(row, fields, { reviser: 'DEV001', reviseDate: currentTime(), dataStamp: row.dataStamp + 1 })
    row.key = makeDetailKey(row)
    selectedDetailKeys.value = [row.key]
    persistState('已更新 DEV 明細；實際庫存與預訂量尚未變更。')
  } else {
    const row: MaterialOutDetail = { key: '', ...fields, creator: 'DEV001', createDate: currentTime(), reviser: '', reviseDate: '', dataStamp: 0 }
    row.key = makeDetailKey(row)
    if (records.value.details.some((item) => item.key === row.key)) { notice.value = '相同單據、物料、批號、儲位與去向已存在。'; return }
    records.value.details.push(row)
    selectedDetailKeys.value = [row.key]
    persistState(dialogMode.value === 'detail-copy' ? '已複製 DEV 明細；實際庫存與預訂量尚未變更。' : '已新增 DEV 明細；實際庫存與預訂量尚未變更。')
  }
  dialogMode.value = null
  editingDetailKey.value = ''
}
function requestDeleteDetail(): void {
  const keys = selectedDetailKeys.value.length ? selectedDetailKeys.value : activeDetail.value ? [activeDetail.value.key] : []
  pendingDeleteDetailKeys.value = keys.filter((key) => visibleDetails.value.some((row) => row.key === key))
  if (pendingDeleteDetailKeys.value.length) dialogMode.value = 'delete-detail'
}
function confirmDeleteDetail(): void {
  const deleting = new Set(pendingDeleteDetailKeys.value)
  records.value.details = records.value.details.filter((row) => !deleting.has(row.key))
  selectedDetailKeys.value = []
  persistState(`已刪除 ${deleting.size} 筆 DEV 明細和元件假資料；沒有執行庫存異動。`)
  dialogMode.value = null
  pendingDeleteDetailKeys.value = []
}
function openPrintPreview(): void { if (activeBasis.value) dialogMode.value = 'print' }
function setClose(): void { dialogMode.value = null }
</script>

<template>
  <main class="material-out-page" aria-label="開立領料單 DEV">
    <header class="page-heading">
      <div><p>INVENTORY · IV203</p><h1>Material Out <span>開立領料單</span></h1><small>維護未確認領料單；實際扣帳由 IV201 確認流程處理</small></div>
      <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
    </header>

    <p class="notice" role="status">{{ notice }}</p>

    <section class="query-panel" aria-label="領料單篩選">
      <label>MaterialOutNo
        <span class="query-control"><input v-model="searchText" autocomplete="off" placeholder="搜尋未確認單號" @keyup.enter="runQuery" /><button type="button" class="primary" @click="runQuery">Query</button></span>
      </label>
      <span class="query-hint">Form 開啟時固定查 State=0；結果依 MaterialOutNo 升冪。</span>
    </section>

    <section class="panel basis-panel" aria-label="領料單主檔">
      <header class="panel-heading">
        <div><h2>MaterialOut Basis</h2><span>{{ visibleBases.length }} 張未確認單</span></div>
        <div class="action-row">
          <button type="button" class="primary" @click="openBasisDialog('add')">ADD</button>
          <button type="button" :disabled="!activeBasis" @click="openBasisDialog('edit')">EDIT</button>
          <button type="button" :disabled="!activeBasis" @click="requestDeleteBasis">DEL</button>
          <button type="button" :disabled="!activeBasis" @click="openBasisDialog('copy')">COPY</button>
          <button type="button" :disabled="!activeBasis" @click="openPrintPreview">PRINT</button>
        </div>
      </header>
      <div class="table-wrap basis-table-wrap">
        <table><thead><tr><th class="select-col">選</th><th>MaterialOutNo</th><th>InventoryNo</th><th>PreOrderNo</th><th>State</th><th>ConfirmUserNo</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleBases" :key="row.materialOutNo" :class="{ active: activeBasis?.materialOutNo === row.materialOutNo }" @click="selectBasis(row)">
              <td class="select-col"><input type="checkbox" :checked="selectedBasisNos.includes(row.materialOutNo)" :aria-label="`選取 ${row.materialOutNo}`" @click.stop @change="toggleBasisSelection(row.materialOutNo)" /></td>
              <td class="mono">{{ row.materialOutNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.preOrderNo || '—' }}</td><td><span class="state-badge">{{ stateName(row.state) }}</span></td><td>{{ row.confirmUserNo || '—' }}</td><td class="description">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="visibleBases.length === 0"><td colspan="11" class="empty">沒有符合單號條件的 State=0 領料單。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>{{ activeBasis ? `${activeBasis.materialOutNo} · ${inventoryName(activeBasis.inventoryNo)}` : '尚未選取單據' }}</span><span>未確認單頭 · localStorage DEV fixtures</span></footer>
    </section>

    <section class="panel detail-panel" aria-label="領料單明細">
      <header class="panel-heading">
        <div><h2>MaterialOut Detail</h2><span>{{ activeBasis?.materialOutNo ?? '請先選取單頭' }} · {{ visibleDetails.length }} 筆明細</span></div>
        <div class="action-row">
          <button type="button" class="primary" :disabled="!activeBasis" @click="openDetailDialog('add')">ADD</button>
          <button type="button" :disabled="!activeDetail" @click="openDetailDialog('edit')">EDIT</button>
          <button type="button" :disabled="!activeDetail" @click="requestDeleteDetail">DEL</button>
          <button type="button" :disabled="!activeDetail" @click="openDetailDialog('copy')">COPY</button>
        </div>
      </header>
      <div class="table-wrap detail-table-wrap">
        <table><thead><tr><th class="select-col">選</th><th>MaterialNo</th><th>MaterialName</th><th>MaterialLotNo</th><th>LocatorNo</th><th>PutInPlaceType</th><th>PutInPlaceNo</th><th>UnitNo</th><th class="numeric">Qty</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <template v-for="row in visibleDetails" :key="row.key">
              <tr :class="{ active: activeDetail?.key === row.key }" @click="activateDetail(row)">
                <td class="select-col"><input type="checkbox" :checked="selectedDetailKeys.includes(row.key)" :aria-label="`選取 ${row.materialNo} ${row.materialLotNo}`" @click.stop @change="toggleDetailSelection(row.key)" /></td>
                <td class="mono">{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td class="mono">{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ placeName(row.putInPlaceType) }}</td><td>{{ row.putInPlaceNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ formatQty(row.qty) }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
              </tr>
              <tr v-if="row.components.length && expandedDetailKeys.includes(row.key)" class="component-row"><td colspan="13"><strong>MaterialOut Component</strong><span v-for="item in row.components" :key="item.componentId">{{ item.componentNo }} · {{ item.componentLotNo }} · Slot {{ item.slotNo }}</span></td></tr>
            </template>
            <tr v-if="activeBasis && visibleDetails.length === 0"><td colspan="13" class="empty">此 DEV 單據尚無明細。</td></tr>
            <tr v-else-if="!activeBasis"><td colspan="13" class="empty">選取未確認單頭後載入明細。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>{{ visibleDetails.length }} 筆明細</span><button type="button" class="text-button" :disabled="!activeDetail?.components.length" @click="activeDetail && toggleExpandedDetail(activeDetail.key)">展開／收合選取列元件</button></footer>
    </section>

    <div v-if="dialogMode === 'basis-add' || dialogMode === 'basis-edit' || dialogMode === 'basis-copy'" class="modal-backdrop" @click.self="setClose">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="MaterialOut Basis">
        <header class="dialog-heading"><div><p>IV203 · MATERIAL OUT</p><h2>{{ basisDialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="setClose">×</button></header>
        <div class="form-grid">
          <label>MaterialOutNo <span class="serial-control"><input v-model="basisDraft.materialOutNo" readonly /><button v-if="dialogMode !== 'basis-edit'" type="button" @click="basisDraft.materialOutNo = nextMaterialOutNo()">取得 DEV 假單號</button></span><small>DEV 模擬 `MATERIALOUTNO` Serial Rule。</small></label>
          <label>InventoryNo <select v-model="basisDraft.inventoryNo" :disabled="dialogMode === 'basis-edit'" @change="changeBasisInventory"><option value="">請選庫房</option><option v-for="row in inventories" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option></select></label>
          <label>PreOrderNo <select v-model="basisDraft.preOrderNo"><option value="">不使用預訂單</option><option v-for="row in availablePreOrders" :key="row.preOrderNo" :value="row.preOrderNo">{{ row.preOrderNo }} · {{ row.description }}</option></select><small>只列同庫房、State=2 的 DEV 預訂單。</small></label>
          <label class="span-two">Description <textarea v-model="basisDraft.description" rows="3" /></label>
        </div>
        <footer class="dialog-actions"><button type="button" @click="setClose">Close (X)</button><button type="button" class="primary" @click="saveBasis">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'detail-add' || dialogMode === 'detail-edit' || dialogMode === 'detail-copy'" class="modal-backdrop" @click.self="setClose">
      <section class="dialog detail-dialog" role="dialog" aria-modal="true" aria-label="MaterialOut Detail">
        <header class="dialog-heading"><div><p>IV203 · MATERIAL OUT DETAIL</p><h2>{{ detailDialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="setClose">×</button></header>
        <p class="dialog-context">{{ activeBasis?.materialOutNo }} · {{ activeBasis?.inventoryNo }} {{ inventoryName(activeBasis?.inventoryNo ?? '') }}</p>
        <div class="form-grid">
          <label class="span-two">Material / Lot / Locator
            <select v-model="detailDraft.stockId" :disabled="dialogMode === 'detail-edit'" @change="onStockChanged"><option value="">請由此庫房目前庫存選取</option><option v-for="row in stockOptions" :key="row.stockId" :value="row.stockId">{{ displayStock(row) }}</option></select>
          </label>
          <div class="stock-card span-two" v-if="selectedStock"><strong>{{ selectedStock.materialNo }} · {{ selectedStock.materialName }}</strong><span>Lot {{ selectedStock.materialLotNo }} · Locator {{ selectedStock.locatorNo }} · {{ selectedStock.vendorNo }}</span><span>Unit {{ selectedStock.unitNo }} · CurQty {{ formatQty(selectedStock.curQty) }} · AllocateQty {{ formatQty(selectedStock.allocateQty) }} · 可用 {{ formatQty(availableQty(selectedStock)) }}</span></div>
          <label>PutInPlaceType<select v-model.number="detailDraft.putInPlaceType" @change="onPlaceTypeChanged"><option value="">請選領料去向</option><option value="0">EQP · 設備</option><option value="1">OP · 作業員</option><option value="2">WIP INV · 在製庫</option><option value="3">MO · 工單</option><option value="4">Unproductive · 非生產性</option></select></label>
          <label>PutInPlaceNo
            <select v-if="detailDraft.putInPlaceType !== '' && detailDraft.putInPlaceType !== 4" v-model="detailDraft.putInPlaceNo"><option value="">請選有效去向</option><option v-for="row in selectedTargetOptions" :key="row.value" :value="row.value">{{ row.label }}</option></select>
            <input v-else v-model="detailDraft.putInPlaceNo" :disabled="detailDraft.putInPlaceType === ''" placeholder="非生產性用途編號" />
          </label>
          <label>UnitNo <input :value="selectedStock?.unitNo ?? ''" readonly /></label>
          <label>Qty <input v-model="detailDraft.qty" :readonly="Boolean(selectedStock?.components.length)" type="number" min="0" step="0.000001" /></label>
        </div>
        <section v-if="selectedStock?.components.length" class="component-picker">
          <header><div><h3>Material Out Component</h3><span>{{ componentSelection.length }} 筆已選；明細 Qty 必須相等</span></div></header>
          <div class="component-list">
            <label v-for="row in detailComponentOptions" :key="row.componentId" class="component-option"><input type="checkbox" :checked="componentSelection.includes(row.componentId)" @change="toggleComponent(row.componentId)" /><span>{{ row.componentNo }} · {{ row.componentLotNo }}</span><small>Laser {{ row.laserMark }} · Cassette {{ row.cassetteNo }} · Slot {{ row.slotNo }} · Good {{ row.goodQty }} / Scrap {{ row.scrapQty }} {{ row.unitNo }}</small></label>
            <p v-if="detailComponentOptions.length === 0" class="empty">此庫存批號的元件皆已被其他 DEV 領料明細預約。</p>
          </div>
        </section>
        <p class="rule-note">本頁只建待確認領料單。Draft 階段不扣庫存，也不保證可領數量；IV201 確認時才重驗可用庫存／預訂量並扣帳。</p>
        <footer class="dialog-actions"><button type="button" @click="setClose">Close (X)</button><button type="button" class="primary" @click="saveDetail">Confirm</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'delete-basis' || dialogMode === 'delete-detail'" class="modal-backdrop" @click.self="setClose">
      <section class="dialog confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除">
        <header class="dialog-heading"><div><p>IV203 · DEV 假資料</p><h2>確認刪除</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="setClose">×</button></header>
        <p v-if="dialogMode === 'delete-basis'">將刪除 {{ pendingDeleteBasisNos.length }} 張 State=0 單頭、其明細與 DEV 元件假資料。此操作只修改本機假資料。</p>
        <p v-else>將刪除 {{ pendingDeleteDetailKeys.length }} 筆 State=0 單頭的 DEV 明細。此操作只修改本機假資料。</p>
        <footer class="dialog-actions"><button type="button" @click="setClose">Cancel</button><button type="button" class="danger" @click="dialogMode === 'delete-basis' ? confirmDeleteBasis() : confirmDeleteDetail()">Delete</button></footer>
      </section>
    </div>

    <div v-if="dialogMode === 'print' && activeBasis" class="modal-backdrop print-backdrop" @click.self="setClose">
      <section class="print-dialog" role="dialog" aria-modal="true" aria-label="領料單列印預覽">
        <header class="dialog-heading"><div><p>DEV REPORT PREVIEW · arMaterialOut</p><h2>領料單 {{ activeBasis.materialOutNo }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="setClose">×</button></header>
        <dl class="print-meta"><div><dt>InventoryNo</dt><dd>{{ activeBasis.inventoryNo }} · {{ inventoryName(activeBasis.inventoryNo) }}</dd></div><div><dt>PreOrderNo</dt><dd>{{ activeBasis.preOrderNo || '—' }}</dd></div><div><dt>State</dt><dd>{{ stateName(activeBasis.state) }}</dd></div><div><dt>Description</dt><dd>{{ activeBasis.description || '—' }}</dd></div></dl>
        <div class="table-wrap"><table><thead><tr><th>MaterialNo</th><th>Lot</th><th>Locator</th><th>PutInPlace</th><th>Unit</th><th>Qty</th></tr></thead><tbody><tr v-for="row in visibleDetails" :key="row.key"><td>{{ row.materialNo }}</td><td>{{ row.materialLotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ placeName(row.putInPlaceType) }} · {{ row.putInPlaceNo }}</td><td>{{ row.unitNo }}</td><td class="numeric">{{ formatQty(row.qty) }}</td></tr><tr v-if="!visibleDetails.length"><td colspan="6" class="empty">此單據沒有明細。</td></tr></tbody></table></div>
        <footer class="dialog-actions"><span>DEV 預覽不連印表機，亦不產生正式報表。</span><button type="button" @click="setClose">Close (X)</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.material-out-page { display:flex; flex-direction:column; gap:11px; width:100%; height:100%; min-height:0; padding:14px; box-sizing:border-box; overflow:auto; background:#f1efe6; color:#28271f; font:inherit; }
.page-heading { display:flex; align-items:center; justify-content:space-between; gap:16px; padding:2px 2px 4px; }
.page-heading p,.dialog-heading p { margin:0 0 4px; color:#827d6c; font-size:11px; font-weight:800; letter-spacing:.12em; }
.page-heading h1 { margin:0; font-size:22px; line-height:1.2; }
.page-heading h1 span { margin-left:8px; color:#696655; font-size:15px; font-weight:500; }
.page-heading small { display:block; margin-top:4px; color:#777366; }
.notice { margin:0; padding:8px 10px; border:1px solid #d7d1bd; border-radius:4px; background:#faf7ed; color:#625d4d; font-size:12px; }
.query-panel,.panel { border:1px solid #c9c5b7; border-radius:5px; background:#fbfaf5; box-shadow:0 1px 3px #302d2112; }
.query-panel { display:flex; align-items:end; justify-content:space-between; gap:18px; padding:10px 12px; }
.query-panel label { display:flex; flex-direction:column; gap:5px; width:min(490px,100%); color:#676353; font-size:11px; font-weight:700; }
.query-control { display:flex; gap:6px; }
.query-control input,.form-grid input,.form-grid select,.form-grid textarea { width:100%; min-width:0; min-height:32px; padding:6px 8px; box-sizing:border-box; border:1px solid #c9c5b7; border-radius:3px; background:white; color:#2d2b23; font:inherit; font-size:12px; }
.query-control .primary { min-width:82px; }
.query-hint,.dialog-context { color:#777366; font-size:11px; }
.close-button,.action-row button,.dialog-actions button,.query-control button,.serial-control button { min-height:31px; padding:5px 10px; border:1px solid #c4c0b2; border-radius:4px; background:#fffdf7; color:#39372e; font:inherit; font-size:11px; cursor:pointer; }
.action-row { display:flex; flex-wrap:wrap; gap:6px; }
.action-row button:disabled,.dialog-actions button:disabled,.text-button:disabled { opacity:.45; cursor:not-allowed; }
.primary { border-color:#3c4c3b !important; background:#3c4c3b !important; color:white !important; }
.panel { display:flex; flex-direction:column; min-height:0; overflow:hidden; }
.basis-panel { flex:1.15 1 255px; }
.detail-panel { flex:.85 1 220px; }
.panel-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; min-height:44px; padding:7px 10px; border-bottom:1px solid #d8d4c7; background:#f7f5ee; }
.panel-heading h2 { display:inline; margin:0; font-size:14px; }
.panel-heading span { margin-left:9px; color:#777366; font-size:11px; }
.table-wrap { flex:1; min-height:0; overflow:auto; }
table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:11px; }
th { position:sticky; top:0; z-index:1; padding:7px 8px; border-bottom:1px solid #c9c5b7; background:#efede4; color:#625e50; text-align:left; font-weight:700; }
td { padding:7px 8px; border-bottom:1px solid #ece9df; }
tbody tr { cursor:pointer; }
tbody tr:hover { background:#f3f0e6; }
tbody tr.active { background:#e7eadf; }
.select-col { width:34px; text-align:center; }
.select-col input,.component-option input { width:14px; height:14px; accent-color:#3c4c3b; }
.mono { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-variant-numeric:tabular-nums; }
.numeric { text-align:right; font-variant-numeric:tabular-nums; }
.description { max-width:230px; white-space:normal; }
.state-badge { display:inline-flex; justify-content:center; min-width:77px; padding:2px 6px; border-radius:10px; background:#f5e6c6; color:#77551d; font-size:10px; }
.grid-footer { display:flex; align-items:center; justify-content:space-between; gap:10px; min-height:26px; padding:4px 10px; border-top:1px solid #e1ded3; color:#777366; font-size:10px; }
.text-button { border:0; background:transparent; color:#456244; font:inherit; font-size:10px; cursor:pointer; }
.empty { padding:18px; color:#8a8577; text-align:center; cursor:default; }
.component-row { background:#f6f4ec; }
.component-row td { display:flex; flex-wrap:wrap; align-items:center; gap:9px; white-space:normal; color:#625d4d; }
.component-row strong { color:#39372e; }
.modal-backdrop { position:fixed; inset:0; z-index:30; display:grid; place-items:center; padding:18px; background:#24221866; }
.dialog,.print-dialog { display:flex; flex-direction:column; gap:13px; width:min(720px,100%); max-height:92vh; overflow:auto; padding:14px; border:1px solid #c9c5b7; border-radius:6px; background:#fbfaf5; box-shadow:0 12px 40px #0003; }
.detail-dialog { width:min(860px,100%); }
.confirm-dialog { width:min(490px,100%); }
.dialog-heading { display:flex; align-items:center; justify-content:space-between; gap:12px; padding-bottom:9px; border-bottom:1px solid #d8d4c7; }
.dialog-heading h2 { margin:0; font-size:17px; }
.dialog-heading p { margin-bottom:3px; }
.icon-button { width:31px; height:31px; border:0; border-radius:4px; background:transparent; color:#514f45; font-size:24px; cursor:pointer; }
.form-grid { display:grid; grid-template-columns:repeat(2,minmax(180px,1fr)); gap:12px; }
.form-grid label { display:flex; flex-direction:column; gap:5px; min-width:0; color:#676353; font-size:11px; font-weight:700; }
.form-grid label small { color:#777366; font-size:10px; font-weight:400; }
.span-two { grid-column:span 2; }
.serial-control { display:flex; gap:6px; }
.serial-control input { flex:1; }
.stock-card { display:flex; flex-direction:column; gap:4px; padding:9px 10px; border:1px solid #d9d4c6; border-radius:4px; background:#f6f4ec; color:#625d4d; font-size:11px; }
.stock-card strong { color:#35342d; }
.component-picker { border:1px solid #d4d0c3; border-radius:4px; background:#f8f6ef; }
.component-picker header { padding:8px 10px; border-bottom:1px solid #dedacd; }
.component-picker h3 { display:inline; margin:0; font-size:12px; }
.component-picker header span { margin-left:8px; color:#777366; font-size:10px; }
.component-list { display:flex; flex-direction:column; max-height:180px; overflow:auto; padding:6px 9px; }
.component-option { display:grid !important; grid-template-columns:18px 1fr; align-items:center; gap:4px 7px !important; padding:5px 2px; border-bottom:1px solid #e9e5da; font-weight:400 !important; }
.component-option span { color:#34332c; }
.component-option small { grid-column:2; color:#777366; }
.rule-note { margin:0; padding:8px 10px; border-left:3px solid #b39248; background:#f5efdE; color:#645638; font-size:11px; }
.dialog-actions { display:flex; align-items:center; justify-content:flex-end; gap:7px; padding-top:4px; }
.dialog-actions span { margin-right:auto; color:#777366; font-size:10px; }
.dialog-actions .danger { border-color:#8b3f39; background:#8b3f39; color:white; }
.confirm-dialog p { margin:0; color:#5c584d; font-size:13px; line-height:1.5; }
.print-dialog { width:min(920px,100%); background:#fff; }
.print-meta { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:9px 16px; margin:0; }
.print-meta div { display:grid; grid-template-columns:110px 1fr; gap:8px; padding-bottom:6px; border-bottom:1px solid #eee; }
.print-meta dt { color:#777366; font-size:11px; }
.print-meta dd { margin:0; color:#28271f; font-size:12px; }
@media(max-width:950px) { .query-panel { align-items:flex-start; flex-direction:column; gap:6px; } .panel-heading { align-items:flex-start; flex-direction:column; } .basis-panel { min-height:260px; } .detail-panel { min-height:250px; } }
@media(max-width:620px) { .material-out-page { padding:9px; } .page-heading h1 { font-size:18px; } .page-heading h1 span { display:block; margin:3px 0 0; } .form-grid { grid-template-columns:1fr; } .span-two { grid-column:span 1; } .print-meta { grid-template-columns:1fr; } .query-panel label { width:100%; } .query-control input { flex:1; } .grid-footer { align-items:flex-start; flex-direction:column; } }
</style>

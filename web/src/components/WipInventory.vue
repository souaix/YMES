<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type TabKey = 'raw' | 'rawComp' | 'semiLot' | 'semiComp' | 'semiBin' | 'scrap' | 'componentScrap' | 'failBin'
type GridRow = { rowId: string; inventoryNo: string; [key: string]: string | number }
type Inventory = { inventoryNo: string; inventoryName: string }
type BinDefinition = { binNo: string; binOrder: number }
type ComponentDetail = { parentRowId: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type BinDetail = { parentRowId: string; quantities: Record<string, number> }
type ScrapError = { parentRowId: string; errorNo: string; errorName: string; errorQty: number; reasonType: number; description: string }
type Snapshot = {
  version: 1
  inventories: Inventory[]
  bins: BinDefinition[]
  rawRows: GridRow[]
  rawCompRows: GridRow[]
  semiLotRows: GridRow[]
  semiCompRows: GridRow[]
  semiBinRows: GridRow[]
  scrapRows: GridRow[]
  componentScrapRows: GridRow[]
  failBinRows: GridRow[]
  rawComponents: ComponentDetail[]
  semiComponents: ComponentDetail[]
  semiBinDetails: BinDetail[]
  scrapErrors: ScrapError[]
  nextNo: number
}
type Column = { key: string; label: string; number?: boolean }
type ActionMode = null | 'entry' | 'delete' | 'operation'

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp603-wip-inventory.v1'
const tabs: { key: TabKey; label: string }[] = [
  { key: 'raw', label: 'Raw' },
  { key: 'rawComp', label: 'Raw_Comp' },
  { key: 'semiLot', label: 'SEMI_Lot' },
  { key: 'semiComp', label: 'SEMI_Comp' },
  { key: 'semiBin', label: 'SEMI_BIN' },
  { key: 'scrap', label: 'Scrap' },
  { key: 'componentScrap', label: 'Scrap Component' },
  { key: 'failBin', label: 'Fail Bin' },
]
const definitions: Record<TabKey, Column[]> = {
  raw: [
    { key: 'materialNo', label: 'MaterialNo' }, { key: 'materialName', label: 'MaterialName' },
    { key: 'materialLotNo', label: 'MaterialLotNo' }, { key: 'locatorNo', label: 'LocatorNo' },
    { key: 'qty', label: 'Qty', number: true }, { key: 'unitNo', label: 'UnitNo' },
    { key: 'materialType', label: 'MaterialType' }, { key: 'manufactureDate', label: 'ManufactureDate' },
    { key: 'expireDate', label: 'ExpireDate' }, { key: 'graceDate', label: 'GraceDate' },
  ],
  rawComp: [
    { key: 'materialNo', label: 'MaterialNo' }, { key: 'materialName', label: 'MaterialName' },
    { key: 'materialLotNo', label: 'MaterialLotNo' }, { key: 'locatorNo', label: 'LocatorNo' },
    { key: 'qty', label: 'Qty', number: true }, { key: 'unitNo', label: 'UnitNo' },
    { key: 'materialType', label: 'MaterialType' }, { key: 'manufactureDate', label: 'ManufactureDate' },
    { key: 'expireDate', label: 'ExpireDate' }, { key: 'graceDate', label: 'GraceDate' },
  ],
  semiLot: [
    { key: 'productNo', label: 'ProductNo' }, { key: 'productName', label: 'ProductName' },
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' },
    { key: 'moNo', label: 'MONo' }, { key: 'qty', label: 'Qty', number: true },
    { key: 'unitNo', label: 'UnitNo' }, { key: 'customerNo', label: 'CustomerNo' }, { key: 'inputDate', label: 'InputDate' },
  ],
  semiComp: [
    { key: 'productNo', label: 'ProductNo' }, { key: 'productName', label: 'ProductName' },
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' },
    { key: 'moNo', label: 'MONo' }, { key: 'qty', label: 'Qty', number: true },
    { key: 'unitNo', label: 'UnitNo' }, { key: 'customerNo', label: 'CustomerNo' }, { key: 'inputDate', label: 'InputDate' },
  ],
  semiBin: [
    { key: 'productNo', label: 'ProductNo' }, { key: 'productName', label: 'ProductName' },
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' },
    { key: 'moNo', label: 'MONo' }, { key: 'qty', label: 'Qty', number: true },
    { key: 'unitNo', label: 'UnitNo' }, { key: 'customerNo', label: 'CustomerNo' }, { key: 'inputDate', label: 'InputDate' },
  ],
  scrap: [
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' }, { key: 'lotSerial', label: 'LotSerial' },
    { key: 'moNo', label: 'MONo' }, { key: 'opNo', label: 'OPNo' }, { key: 'scrapQty', label: 'ScrapQty', number: true },
    { key: 'lossQty', label: 'LossQty', number: true }, { key: 'unitNo', label: 'UnitNo' },
    { key: 'customerNo', label: 'CustomerNo' }, { key: 'productNo', label: 'ProductNo' },
    { key: 'productName', label: 'ProductName' }, { key: 'inputDate', label: 'InputDate' },
  ],
  componentScrap: [
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' }, { key: 'lotSerial', label: 'LotSerial' },
    { key: 'componentNo', label: 'ComponentNo' }, { key: 'opNo', label: 'OPNo' }, { key: 'goodQty', label: 'GoodQty', number: true },
    { key: 'scrapQty', label: 'ScrapQty', number: true }, { key: 'unitNo', label: 'UnitNo' }, { key: 'baseLotNo', label: 'BaseLotNo' },
    { key: 'inputDate', label: 'InputDate' },
  ],
  failBin: [
    { key: 'lotNo', label: 'LotNo' }, { key: 'locatorNo', label: 'LocatorNo' }, { key: 'lotSerial', label: 'LotSerial' },
    { key: 'opNo', label: 'OPNo' }, { key: 'unitNo', label: 'UnitNo' }, { key: 'baseLotNo', label: 'BaseLotNo' },
    ...Array.from({ length: 8 }, (_, index) => ({ key: `bin${index + 1}`, label: `BIN${index + 1}`, number: true })),
  ],
}
const actionLabels: Record<TabKey, string[]> = {
  raw: ['Lot Split', 'Scrap', 'DEL(&D)', 'EDIT(&E)', 'ADD(&A)', 'Label Print'],
  rawComp: ['Lot Split', 'Label Print'],
  semiLot: ['DEL(&D)', 'EDIT(&E)', 'ADD(&A)', 'Merge Lot', 'Split Lot', 'Re Run', 'Turn Inv'],
  semiComp: ['DEL(&D)', 'ADD(&A)', 'Merge Comp', 'Split Comp', 'Re Run', 'Turn Inv'],
  semiBin: ['DEL(&D)', 'ADD(&A)', 'Merge Bin', 'Split Bin', 'Re Run', 'Turn Inv'],
  scrap: ['DEL(&D)', 'Turn Inv', 'Re Run'],
  componentScrap: ['DEL(&D)', 'Turn Inv', 'Re Run'],
  failBin: ['Turn Inv', 'Re Run'],
}

function seed(): Snapshot {
  const inventories = [
    { inventoryNo: 'DEV-WIP-01', inventoryName: '在製品主倉' },
    { inventoryNo: 'DEV-WIP-02', inventoryName: '線邊暫存倉' },
  ]
  const rawRows: GridRow[] = Array.from({ length: 8 }, (_, i) => {
    const n = i + 1
    return {
      rowId: `raw-${n}`, inventoryNo: n < 7 ? 'DEV-WIP-01' : 'DEV-WIP-02', materialNo: `MAT-${String(n).padStart(4, '0')}`,
      materialName: ['導電膠', '保護膜', '清洗劑', '載帶'][i % 4], materialLotNo: `ML-${String(260110 + n).padStart(6, '0')}`,
      locatorNo: n % 2 ? 'A-01' : 'B-02', qty: 580 - n * 19, unitNo: n % 3 ? 'PCS' : 'KG', materialType: n % 2 ? 'RAW' : 'CHEMICAL',
      manufactureDate: `2026/10/${String(4 + n).padStart(2, '0')}`, expireDate: `2027/10/${String(4 + n).padStart(2, '0')}`,
      graceDate: `2027/11/${String(4 + n).padStart(2, '0')}`, inputDate: `2026/11/${String(10 + n).padStart(2, '0')} 08:30:00`,
    }
  })
  const rawCompRows = rawRows.filter((_, i) => i !== 3).map((row, index) => ({ ...row, rowId: `rawcomp-${index + 1}` }))
  const makeSemiRows = (prefix: string): GridRow[] => Array.from({ length: 9 }, (_, i) => {
    const n = i + 1
    return {
      rowId: `${prefix}-${n}`, inventoryNo: n < 8 ? 'DEV-WIP-01' : 'DEV-WIP-02', productNo: `PRD-${String(420 + Math.ceil(n / 2)).padStart(5, '0')}`,
      productName: ['封裝測試成品', '控制板半成品', '電源模組'][i % 3], lotNo: `DEV-WLOT-${String(n).padStart(4, '0')}`,
      locatorNo: ['A-01', 'A-02', 'B-01'][i % 3], moNo: `MO-26-${String(1300 + Math.floor(i / 2)).padStart(5, '0')}`,
      qty: 2400 - n * 113, unitNo: 'PCS', customerNo: i % 3 ? 'CUST-001' : 'CUST-008',
      inputDate: `2026/11/${String(11 + n).padStart(2, '0')} 1${i % 3}:20:00`, baseLotNo: `BASE-${String(900 + n).padStart(4, '0')}`,
    }
  })
  const semiLotRows = makeSemiRows('semilot')
  const semiCompRows = makeSemiRows('semicomp').filter((_, i) => i !== 4)
  const semiBinRows = makeSemiRows('semibin').filter((_, i) => i % 4 !== 2)
  const scrapRows: GridRow[] = Array.from({ length: 6 }, (_, i) => ({
    rowId: `scrap-${i + 1}`, inventoryNo: i < 5 ? 'DEV-WIP-01' : 'DEV-WIP-02', lotNo: `DEV-LOT-S${String(i + 1).padStart(3, '0')}`,
    locatorNo: i % 2 ? 'SCRAP-B' : 'SCRAP-A', lotSerial: `LS-2026-${String(700 + i).padStart(5, '0')}`, moNo: `MO-26-${1308 + i}`,
    opNo: ['OP-ASSY', 'OP-TEST', 'OP-PACK'][i % 3], scrapQty: 8 + i * 2, lossQty: i % 2 ? 1 : 0, unitNo: 'PCS',
    customerNo: i % 2 ? 'CUST-001' : 'CUST-008', productNo: `PRD-${String(420 + i).padStart(5, '0')}`,
    productName: '封裝測試成品', inputDate: `2026/11/${String(13 + i).padStart(2, '0')} 14:10:00`,
  }))
  const componentScrapRows: GridRow[] = Array.from({ length: 7 }, (_, i) => ({
    rowId: `compscrap-${i + 1}`, inventoryNo: 'DEV-WIP-01', lotNo: `DEV-LOT-C${String(i + 1).padStart(3, '0')}`,
    locatorNo: 'SCRAP-A', lotSerial: `LS-C-${String(810 + i).padStart(5, '0')}`, componentNo: `COMP-${String(20 + i).padStart(4, '0')}`,
    opNo: 'OP-ASSY', goodQty: 96 - i * 3, scrapQty: 2 + i, unitNo: 'PCS', baseLotNo: `BASE-${String(990 + i).padStart(4, '0')}`,
    inputDate: `2026/11/${String(12 + i).padStart(2, '0')} 11:00:00`,
  }))
  const failBinRows: GridRow[] = Array.from({ length: 6 }, (_, i) => {
    const row: GridRow = {
      rowId: `failbin-${i + 1}`, inventoryNo: 'DEV-WIP-01', lotNo: `DEV-LOT-F${String(i + 1).padStart(3, '0')}`,
      locatorNo: 'FAIL-01', lotSerial: `LS-F-${String(930 + i).padStart(5, '0')}`, opNo: 'OP-TEST', unitNo: 'PCS',
      baseLotNo: `BASE-${String(1100 + i).padStart(4, '0')}`,
    }
    for (let bin = 1; bin <= 8; bin++) row[`bin${bin}`] = bin === ((i % 8) + 1) ? 4 + i : 0
    return row
  })
  const rawComponents: ComponentDetail[] = rawCompRows.slice(0, 5).flatMap((row, index) => [
    { parentRowId: row.rowId, componentNo: `MAT-COMP-${index + 1}A`, goodQty: 120 - index * 8, scrapQty: 2, unitNo: 'PCS' },
    { parentRowId: row.rowId, componentNo: `MAT-COMP-${index + 1}B`, goodQty: 118 - index * 8, scrapQty: 1, unitNo: 'PCS' },
  ])
  const semiComponents: ComponentDetail[] = semiCompRows.flatMap((row, index) => [
    { parentRowId: row.rowId, componentNo: `COMP-${String(index * 2 + 1).padStart(4, '0')}`, goodQty: 2240 - index * 72, scrapQty: index * 2, unitNo: 'PCS' },
    { parentRowId: row.rowId, componentNo: `COMP-${String(index * 2 + 2).padStart(4, '0')}`, goodQty: 2228 - index * 72, scrapQty: index, unitNo: 'PCS' },
  ])
  const bins: BinDefinition[] = Array.from({ length: 8 }, (_, i) => ({ binNo: `BIN${i + 1}`, binOrder: (i + 1) * 10 }))
  const semiBinDetails: BinDetail[] = semiBinRows.map((row, index) => ({
    parentRowId: row.rowId,
    quantities: Object.fromEntries(bins.map((bin, binIndex) => [bin.binNo, binIndex === (index % 8) ? 25 + index * 3 : 0])),
  }))
  const scrapErrors: ScrapError[] = scrapRows.flatMap((row, index) => [
    { parentRowId: row.rowId, errorNo: `ERR-${String(index + 1).padStart(3, '0')}`, errorName: ['外觀刮傷', '尺寸偏差', '測試失敗'][index % 3], errorQty: 2 + index, reasonType: index % 2, description: 'DEV 不良原因明細' },
    ...(index % 2 ? [{ parentRowId: row.rowId, errorNo: `ERR-${String(index + 31).padStart(3, '0')}`, errorName: '包裝異常', errorQty: 1, reasonType: 1, description: '第二筆錯誤明細' }] : []),
  ])
  return { version: 1, inventories, bins, rawRows, rawCompRows, semiLotRows, semiCompRows, semiBinRows, scrapRows, componentScrapRows, failBinRows, rawComponents, semiComponents, semiBinDetails, scrapErrors, nextNo: 1 }
}

const state = reactive((() => {
  const initial = seed()
  if (!dev.value) return initial
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<Snapshot> | null
    if (saved?.version === 1 && Array.isArray(saved.rawRows) && Array.isArray(saved.semiLotRows)) return { ...initial, ...saved } as Snapshot
  } catch { /* bad DEV data falls back to seeded rows */ }
  return initial
})())
const activeTab = ref<TabKey>('raw')
const inventoryNo = ref('')
const lotFilterEnabled = ref(false)
const lotFilter = ref('')
const dateFilterEnabled = ref(false)
const startDate = ref(defaultStartDate())
const endDate = ref(defaultEndDate())
const queriedInventory = ref('')
const activeRowId = ref('')
const selectedRowIds = ref<string[]>([])
const modal = ref<ActionMode>(null)
const activeAction = ref('')
const actionMessage = ref('')
const actionError = ref('')
const form = reactive({ itemNo: '', itemName: '', lotNo: '', locatorNo: '', qty: '', unitNo: '', inputDate: '', description: '' })
const operationForm = reactive({ destinationInventoryNo: '', destinationLocatorNo: '', qty: '', note: '' })

function localDateTimeInput(date: Date): string {
  const two = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${two(date.getMonth() + 1)}-${two(date.getDate())}T${two(date.getHours())}:${two(date.getMinutes())}`
}
function defaultStartDate(): string { const date = new Date(); date.setDate(date.getDate() - 7); date.setHours(0, 0, 0, 0); return localDateTimeInput(date) }
function defaultEndDate(): string { const date = new Date(); date.setHours(23, 59, 0, 0); return localDateTimeInput(date) }
function normalizeDateInput(value: string): string { return value.replaceAll('/', '-').replace(' ', 'T').slice(0, 16) }
function rowKey(row: GridRow): string { return row.rowId }
function rowsFor(tab: TabKey): GridRow[] {
  switch (tab) {
    case 'raw': return state.rawRows
    case 'rawComp': return state.rawCompRows
    case 'semiLot': return state.semiLotRows
    case 'semiComp': return state.semiCompRows
    case 'semiBin': return state.semiBinRows
    case 'scrap': return state.scrapRows
    case 'componentScrap': return state.componentScrapRows
    case 'failBin': return state.failBinRows
  }
}
const columns = computed(() => definitions[activeTab.value])
const visibleRows = computed(() => {
  const lotNeedle = lotFilter.value.trim().toLocaleUpperCase()
  return rowsFor(activeTab.value)
    .filter(row => row.inventoryNo.toUpperCase() === queriedInventory.value.toUpperCase())
    .filter(row => !lotFilterEnabled.value || !lotNeedle || String(row.lotNo ?? row.materialLotNo ?? row.lotSerial ?? '').toLocaleUpperCase().includes(lotNeedle))
    .filter(row => {
      if (!dateFilterEnabled.value) return true
      const dateTime = normalizeDateInput(String(row.inputDate ?? ''))
      return dateTime >= startDate.value.slice(0, 16) && dateTime <= endDate.value.slice(0, 16)
    })
    .sort((a, b) => String(a.materialNo ?? a.lotNo ?? '').localeCompare(String(b.materialNo ?? b.lotNo ?? '')) || String(a.materialLotNo ?? a.locatorNo ?? '').localeCompare(String(b.materialLotNo ?? b.locatorNo ?? '')))
})
const activeRow = computed(() => visibleRows.value.find(row => row.rowId === activeRowId.value) ?? null)
const selectedRows = computed(() => rowsFor(activeTab.value).filter(row => selectedRowIds.value.includes(row.rowId)))
const tabCount = computed(() => visibleRows.value.length)
const activeComponents = computed(() => {
  if (!activeRow.value) return []
  const source = activeTab.value === 'rawComp' ? state.rawComponents : state.semiComponents
  return source.filter(item => item.parentRowId === activeRow.value?.rowId)
})
const activeBins = computed(() => state.semiBinDetails.find(item => item.parentRowId === activeRow.value?.rowId) ?? null)
const activeScrapErrors = computed(() => state.scrapErrors.filter(item => item.parentRowId === activeRow.value?.rowId))
const orderedBins = computed(() => [...state.bins].sort((a, b) => a.binOrder - b.binOrder))
const allowedActions = computed(() => actionLabels[activeTab.value])

function persist(): void { if (dev.value) localStorage.setItem(storageKey, JSON.stringify(state)) }
function clearMessage(): void { actionMessage.value = ''; actionError.value = '' }
function queryInventory(): void {
  clearMessage()
  const value = inventoryNo.value.trim().toUpperCase()
  if (!value) { actionError.value = '請選擇在製品庫編號。'; return }
  if (!state.inventories.some(item => item.inventoryNo.toUpperCase() === value)) { actionError.value = '查無此在製品庫。'; return }
  if (dateFilterEnabled.value && startDate.value > endDate.value) { actionError.value = '起始日期不可晚於結束日期。'; return }
  queriedInventory.value = value
  activeRowId.value = ''
  selectedRowIds.value = []
  actionMessage.value = `已載入 ${value} 的庫存資料。`
}
function changeTab(tab: TabKey): void { activeTab.value = tab; activeRowId.value = ''; selectedRowIds.value = []; clearMessage() }
function selectRow(row: GridRow, event: MouseEvent): void {
  activeRowId.value = row.rowId
  if (event.ctrlKey || event.metaKey) {
    selectedRowIds.value = selectedRowIds.value.includes(row.rowId) ? selectedRowIds.value.filter(id => id !== row.rowId) : [...selectedRowIds.value, row.rowId]
  } else if (!selectedRowIds.value.includes(row.rowId)) selectedRowIds.value = [row.rowId]
}
function toggleRow(row: GridRow): void {
  selectedRowIds.value = selectedRowIds.value.includes(row.rowId) ? selectedRowIds.value.filter(id => id !== row.rowId) : [...selectedRowIds.value, row.rowId]
  activeRowId.value = row.rowId
}
function showAction(label: string): void {
  if (!dev.value) return
  clearMessage()
  activeAction.value = label.replace(/\(&[A-Z]\)/i, '')
  if (['ADD', 'EDIT'].includes(activeAction.value.toUpperCase())) {
    if (activeAction.value.toUpperCase() === 'EDIT' && !activeRow.value) { actionError.value = '請先選取一筆資料。'; return }
    const row = activeRow.value
    form.itemNo = String(row?.materialNo ?? row?.productNo ?? '')
    form.itemName = String(row?.materialName ?? row?.productName ?? '')
    form.lotNo = String(row?.materialLotNo ?? row?.lotNo ?? '')
    form.locatorNo = String(row?.locatorNo ?? '')
    form.qty = row ? String(row.qty ?? '') : ''
    form.unitNo = String(row?.unitNo ?? 'PCS')
    form.inputDate = String(row?.inputDate ? normalizeDateInput(String(row.inputDate)) : localDateTimeInput(new Date()))
    form.description = ''
    modal.value = 'entry'
    return
  }
  if (activeAction.value.toUpperCase() === 'DEL') {
    if (!selectedRows.value.length && !activeRow.value) { actionError.value = '請先選取資料。'; return }
    if (!selectedRowIds.value.length && activeRow.value) selectedRowIds.value = [activeRow.value.rowId]
    modal.value = 'delete'
    return
  }
  const requiresRow = !['Label Print'].includes(activeAction.value)
  if (requiresRow && !activeRow.value && !selectedRows.value.length) { actionError.value = '請先選取資料。'; return }
  operationForm.destinationInventoryNo = state.inventories.find(item => item.inventoryNo !== queriedInventory.value)?.inventoryNo ?? ''
  operationForm.destinationLocatorNo = ''
  operationForm.qty = String(activeRow.value?.qty ?? activeRow.value?.scrapQty ?? '')
  operationForm.note = ''
  modal.value = 'operation'
}
function saveEntry(): void {
  const itemNo = form.itemNo.trim().toUpperCase()
  const lotNo = form.lotNo.trim().toUpperCase()
  const qty = Number(form.qty)
  if (!itemNo || !lotNo || !form.locatorNo.trim() || !Number.isFinite(qty) || qty <= 0) { actionError.value = '品號、Lot／MaterialLot、儲位及大於 0 的數量皆必填。'; return }
  const isEdit = activeAction.value.toUpperCase() === 'EDIT'
  const row = activeRow.value
  if (activeTab.value === 'raw' || activeTab.value === 'rawComp') {
    if (isEdit && row) Object.assign(row, { materialNo: itemNo, materialName: form.itemName, materialLotNo: lotNo, locatorNo: form.locatorNo.trim().toUpperCase(), qty, unitNo: form.unitNo.toUpperCase() })
    else state.rawRows.push({ rowId: `raw-dev-${state.nextNo++}`, inventoryNo: queriedInventory.value, materialNo: itemNo, materialName: form.itemName || 'DEV 物料', materialLotNo: lotNo, locatorNo: form.locatorNo.toUpperCase(), qty, unitNo: form.unitNo.toUpperCase(), materialType: 'RAW', manufactureDate: '', expireDate: '', graceDate: '', inputDate: form.inputDate })
  } else {
    if (isEdit && row) Object.assign(row, { productNo: itemNo, productName: form.itemName, lotNo, locatorNo: form.locatorNo.trim().toUpperCase(), qty, unitNo: form.unitNo.toUpperCase() })
    else {
      const rowId = `${activeTab.value}-dev-${state.nextNo++}`
      const newRow: GridRow = { rowId, inventoryNo: queriedInventory.value, productNo: itemNo, productName: form.itemName || 'DEV 產品', lotNo, locatorNo: form.locatorNo.toUpperCase(), qty, unitNo: form.unitNo.toUpperCase(), moNo: 'DEV-MO', customerNo: 'DEV-CUSTOMER', inputDate: form.inputDate, baseLotNo: 'DEV-BASE' }
      if (activeTab.value === 'semiComp') {
        state.semiCompRows.push(newRow)
        state.semiComponents.push({ parentRowId: rowId, componentNo: `DEV-COMP-${rowId}`, goodQty: qty, scrapQty: 0, unitNo: form.unitNo.toUpperCase() })
      } else if (activeTab.value === 'semiBin') {
        state.semiBinRows.push(newRow)
        state.semiBinDetails.push({ parentRowId: rowId, quantities: Object.fromEntries(state.bins.map((bin, index) => [bin.binNo, index === 0 ? qty : 0])) })
      } else state.semiLotRows.push(newRow)
    }
  }
  persist(); modal.value = null; actionMessage.value = `${activeAction.value} 完成（DEV 假資料）。`
}
function deleteRows(): void {
  const doomed = new Set(selectedRowIds.value)
  const list = rowsFor(activeTab.value)
  const remaining = list.filter(row => !doomed.has(row.rowId))
  switch (activeTab.value) {
    case 'raw': state.rawRows = remaining; break
    case 'rawComp': state.rawCompRows = remaining; break
    case 'semiLot': state.semiLotRows = remaining; break
    case 'semiComp': state.semiCompRows = remaining; break
    case 'semiBin': state.semiBinRows = remaining; break
    case 'scrap': state.scrapRows = remaining; break
    case 'componentScrap': state.componentScrapRows = remaining; break
    case 'failBin': state.failBinRows = remaining; break
  }
  activeRowId.value = ''; selectedRowIds.value = []; persist(); modal.value = null
  actionMessage.value = `已刪除 ${doomed.size} 筆 DEV 假資料。`
}
function confirmOperation(): void {
  if (activeAction.value === 'Label Print') { modal.value = null; actionMessage.value = 'DEV 預覽不會呼叫印表機或外部標籤程式。'; return }
  const targets = selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : []
  if (activeAction.value === 'Scrap') {
    const row = targets[0]
    const amount = Number(operationForm.qty)
    if (!row || !Number.isFinite(amount) || amount <= 0) { actionError.value = '請輸入大於 0 的報廢數量。'; return }
    row.qty = Math.max(0, Number(row.qty) - amount)
    state.scrapRows.unshift({ rowId: `scrap-dev-${state.nextNo++}`, inventoryNo: queriedInventory.value, lotNo: String(row.materialLotNo ?? row.lotNo ?? 'DEV-LOT'), locatorNo: String(row.locatorNo ?? ''), lotSerial: `DEV-SCR-${state.nextNo}`, moNo: 'DEV-MO', opNo: 'DEV-OP', scrapQty: amount, lossQty: 0, unitNo: String(row.unitNo ?? 'PCS'), customerNo: 'DEV-CUSTOMER', productNo: String(row.materialNo ?? row.productNo ?? ''), productName: String(row.materialName ?? row.productName ?? ''), inputDate: new Date().toLocaleString() })
    state.scrapErrors.push({ parentRowId: `scrap-dev-${state.nextNo - 1}`, errorNo: 'DEV-ERR', errorName: 'DEV 不良原因', errorQty: amount, reasonType: 0, description: operationForm.note || 'DEV 預覽' })
  } else if (['Turn Inv', 'Transfer Inv'].includes(activeAction.value)) {
    const targetInventory = operationForm.destinationInventoryNo
    if (!targetInventory || targetInventory === queriedInventory.value) { actionError.value = '請選擇不同的目的庫房。'; return }
    for (const row of targets) { row.inventoryNo = targetInventory; if (operationForm.destinationLocatorNo) row.locatorNo = operationForm.destinationLocatorNo }
  } else if (activeAction.value.toLowerCase().includes('split')) {
    for (const row of targets) {
      const quantityKey = 'qty' in row ? 'qty' : 'scrapQty'
      const quantity = Number(row[quantityKey] ?? 0)
      if (quantity > 1) row[quantityKey] = Math.floor(quantity / 2)
    }
  } else if (activeAction.value.toLowerCase().includes('merge')) {
    const amount = targets.reduce((sum, row) => sum + Number(row.qty ?? row.scrapQty ?? 0), 0)
    if (targets.length > 1 && 'qty' in targets[0]) targets[0].qty = amount
  }
  persist(); modal.value = null
  actionMessage.value = `${activeAction.value} 已記錄於 DEV 預覽；完整過帳、Lot/Component 與歷程交易交由 Production API 實作。`
}
function closeDialog(): void { modal.value = null; actionError.value = '' }
function formatValue(value: string | number): string { return typeof value === 'number' ? value.toLocaleString(undefined, { maximumFractionDigits: 6 }) : value }
function closePage(): void { emit('close') }
</script>

<template>
  <section class="wip-inventory-page">
    <header class="page-heading"><div><h1>在製品庫帳務維護</h1><p>Wip Inventory · WP603</p></div><span v-if="dev" class="dev-tag">DEV 假資料</span></header>

    <section class="query-box" aria-label="Query">
      <div class="query-grid">
        <label class="inventory-field"><span>InventoryNo</span><select v-model="inventoryNo"><option value="">請選擇</option><option v-for="item in state.inventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} — {{ item.inventoryName }}</option></select></label>
        <label class="check-field"><input v-model="lotFilterEnabled" type="checkbox"><span>Lot No</span></label>
        <input v-model="lotFilter" class="filter-input" :disabled="!lotFilterEnabled" aria-label="Lot No filter" @keydown.enter.prevent="queryInventory">
        <label class="check-field"><input v-model="dateFilterEnabled" type="checkbox"><span>INVDate</span></label>
        <label class="date-field"><span>From</span><input v-model="startDate" type="datetime-local" :disabled="!dateFilterEnabled"></label>
        <label class="date-field"><span>To</span><input v-model="endDate" type="datetime-local" :disabled="!dateFilterEnabled"></label>
        <button class="query-button" type="button" @click="queryInventory">Query</button>
      </div>
    </section>

    <nav class="inventory-tabs" aria-label="Inventory tabs">
      <button v-for="tab in tabs" :key="tab.key" type="button" :class="{ selected: activeTab === tab.key }" @click="changeTab(tab.key)">{{ tab.label }}</button>
    </nav>

    <section class="inventory-workspace">
      <div class="grid-toolbar">
        <div class="grid-caption"><strong>{{ tabs.find(tab => tab.key === activeTab)?.label }}</strong><span>{{ queriedInventory || '尚未查詢' }}</span></div>
        <div class="actions"><button v-for="label in allowedActions" :key="label" type="button" :disabled="!dev || !queriedInventory" :class="{ primary: ['ADD(&A)', 'ADD'].includes(label) }" @click="showAction(label)">{{ label }}</button></div>
      </div>
      <div class="grid-scroll">
        <table class="inventory-grid">
          <thead><tr><th class="check-column"></th><th v-for="column in columns" :key="column.key" :class="{ numeric: column.number }">{{ column.label }}</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.rowId" :class="{ active: activeRowId === row.rowId, checked: selectedRowIds.includes(row.rowId) }" @click="selectRow(row, $event)">
              <td class="check-column" @click.stop><input type="checkbox" :checked="selectedRowIds.includes(row.rowId)" :aria-label="`選取 ${row.rowId}`" @change="toggleRow(row)"></td>
              <td v-for="column in columns" :key="column.key" :class="{ numeric: column.number }">{{ formatValue(row[column.key] ?? '') }}</td>
            </tr>
            <tr v-if="!visibleRows.length"><td :colspan="columns.length + 1" class="empty-row">{{ queriedInventory ? '沒有符合查詢條件的資料' : '選擇 InventoryNo 後按 Query 載入資料' }}</td></tr>
          </tbody>
        </table>
      </div>

      <section v-if="['rawComp', 'semiComp'].includes(activeTab)" class="detail-panel">
        <div class="detail-caption"><strong>{{ activeTab === 'rawComp' ? 'Component State' : 'Component State' }}</strong><span>{{ activeRow?.lotNo ?? activeRow?.materialLotNo ?? '請選取上方資料列' }}</span></div>
        <div class="grid-scroll detail-scroll"><table class="inventory-grid compact-grid"><thead><tr><th>ComponentNo</th><th class="numeric">GoodQty</th><th class="numeric">ScrapQty</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="row in activeComponents" :key="row.parentRowId + row.componentNo"><td>{{ row.componentNo }}</td><td class="numeric">{{ formatValue(row.goodQty) }}</td><td class="numeric">{{ formatValue(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr>
          <tr v-if="!activeComponents.length"><td colspan="4" class="empty-row">{{ activeRow ? '此 Lot 沒有 Component 明細' : '選取上方資料列以載入 Component 明細' }}</td></tr>
        </tbody></table></div>
      </section>
      <section v-else-if="activeTab === 'semiBin'" class="detail-panel">
        <div class="detail-caption"><strong>Good Bin State</strong><span>{{ activeRow?.lotNo ?? '請選取上方資料列' }}</span></div>
        <div class="grid-scroll detail-scroll"><table class="inventory-grid compact-grid"><thead><tr><th v-for="bin in orderedBins" :key="bin.binNo" class="numeric">{{ bin.binNo }}</th></tr></thead><tbody>
          <tr v-if="activeBins"><td v-for="bin in orderedBins" :key="bin.binNo" class="numeric">{{ formatValue(activeBins.quantities[bin.binNo] ?? 0) }}</td></tr>
          <tr v-else><td :colspan="orderedBins.length" class="empty-row">選取上方資料列以載入 Bin 明細</td></tr>
        </tbody></table></div>
      </section>
      <section v-else-if="activeTab === 'scrap'" class="detail-panel error-detail-panel">
        <div class="detail-caption"><strong>Error Detail</strong><span>{{ activeRow?.lotSerial ?? '選取報廢批號' }}</span></div>
        <div class="grid-scroll detail-scroll"><table class="inventory-grid compact-grid"><thead><tr><th>ErrorNo</th><th>ErrorName</th><th class="numeric">ErrorQty</th><th>ReasonType</th><th>Description</th></tr></thead><tbody>
          <tr v-for="error in activeScrapErrors" :key="error.parentRowId + error.errorNo"><td>{{ error.errorNo }}</td><td>{{ error.errorName }}</td><td class="numeric">{{ formatValue(error.errorQty) }}</td><td>{{ error.reasonType }}</td><td>{{ error.description }}</td></tr>
          <tr v-if="!activeScrapErrors.length"><td colspan="5" class="empty-row">此報廢資料沒有錯誤明細</td></tr>
        </tbody></table></div>
      </section>
    </section>

    <footer class="page-footer"><span>資料總筆數：{{ tabCount }}</span><span class="footer-spacer"></span><span>UserNo：{{ currentUserNo || 'DEV-USER' }}</span><button type="button" @click="closePage">Close</button></footer>
    <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>
    <p v-if="!dev" class="production-note">此頁目前是公司端 DEV 預覽，正式庫存操作需由家用端完成 API 與 schema。</p>

    <div v-if="modal" class="modal-backdrop" @click.self="closeDialog">
      <section class="modal-card" role="dialog" aria-modal="true" :aria-label="activeAction">
        <header><h2>{{ modal === 'entry' ? `${activeAction} WIP Inventory` : modal === 'delete' ? 'Confirm Delete' : activeAction }}</h2><button type="button" class="modal-close" @click="closeDialog">×</button></header>
        <template v-if="modal === 'entry'">
          <div class="form-grid">
            <label><span>{{ ['raw', 'rawComp'].includes(activeTab) ? 'MaterialNo' : 'ProductNo' }}</span><input v-model="form.itemNo"></label>
            <label><span>{{ ['raw', 'rawComp'].includes(activeTab) ? 'MaterialName' : 'ProductName' }}</span><input v-model="form.itemName"></label>
            <label><span>{{ ['raw', 'rawComp'].includes(activeTab) ? 'MaterialLotNo' : 'LotNo' }}</span><input v-model="form.lotNo"></label>
            <label><span>LocatorNo</span><input v-model="form.locatorNo"></label>
            <label><span>Qty</span><input v-model="form.qty" inputmode="decimal"></label>
            <label><span>UnitNo</span><input v-model="form.unitNo"></label>
            <label><span>InputDate</span><input v-model="form.inputDate" type="datetime-local"></label>
          </div>
        </template>
        <template v-else-if="modal === 'delete'"><p>確定刪除選取的 {{ selectedRows.length }} 筆資料？DEV 預覽會直接移除假資料。</p></template>
        <template v-else>
          <p class="modal-intro">{{ activeAction }} · 僅操作本機 DEV 假資料，不會送出庫存交易。</p>
          <div v-if="['Turn Inv', 'Transfer Inv'].includes(activeAction)" class="form-grid">
            <label><span>目的 InventoryNo</span><select v-model="operationForm.destinationInventoryNo"><option v-for="item in state.inventories.filter(item => item.inventoryNo !== queriedInventory)" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }}</option></select></label>
            <label><span>目的 LocatorNo</span><input v-model="operationForm.destinationLocatorNo"></label>
          </div>
          <div v-else-if="activeAction === 'Scrap'" class="form-grid">
            <label><span>報廢數量</span><input v-model="operationForm.qty" inputmode="decimal"></label>
            <label class="wide-field"><span>原因</span><input v-model="operationForm.note" placeholder="輸入 DEV 原因"></label>
          </div>
          <div v-else class="form-grid"><label class="wide-field"><span>備註</span><input v-model="operationForm.note" placeholder="DEV 預覽備註"></label></div>
        </template>
        <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p>
        <footer><button type="button" @click="closeDialog">Close</button><button class="primary-action" type="button" @click="modal === 'entry' ? saveEntry() : modal === 'delete' ? deleteRows() : confirmOperation()">Confirm</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wip-inventory-page{height:100%;min-height:540px;display:flex;flex-direction:column;gap:9px;color:#30343a;background:#fff;font-size:13px}.page-heading{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d8dce0;padding:0 2px 8px}.page-heading h1{font-size:21px;margin:0;font-weight:600}.page-heading p{margin:3px 0 0;color:#78808a;font-size:12px}.dev-tag{padding:4px 9px;border:1px solid #e4c260;background:#fff4c8;color:#705300;border-radius:3px;font-size:12px}.query-box{border:1px solid #c9cdd1;background:#f7f8f9;padding:9px}.query-grid{display:grid;grid-template-columns:minmax(190px,1.3fr) auto minmax(100px,.8fr) auto minmax(190px,1fr) minmax(190px,1fr) auto;align-items:center;gap:8px}.inventory-field,.date-field,.check-field{display:flex;align-items:center;gap:7px;white-space:nowrap}.inventory-field select,.filter-input,.date-field input,.form-grid input,.form-grid select{height:30px;border:1px solid #a8adb3;background:#fff0b5;padding:4px 7px;color:#272b30;min-width:0}.inventory-field select{flex:1;min-width:80px}.date-field input{width:100%}.check-field input{accent-color:#2183c6}.query-button,.actions button,.page-footer button,.modal-card footer button{min-height:30px;border:1px solid #b8bdc2;background:#fafafa;padding:4px 10px;border-radius:3px;color:#25292e}.query-button:hover,.actions button:hover,.page-footer button:hover,.modal-card footer button:hover{background:#edf0f2}.inventory-tabs{display:flex;gap:3px;overflow-x:auto;border-bottom:1px solid #aeb4ba;white-space:nowrap}.inventory-tabs button{padding:8px 13px;border:1px solid transparent;border-bottom:0;background:#edf0f2;color:#3d454d;border-radius:3px 3px 0 0}.inventory-tabs button.selected{background:#fff;border-color:#aeb4ba;color:#222;font-weight:600}.inventory-workspace{display:flex;flex:1;min-height:250px;flex-direction:column;border:1px solid #b9bec3;overflow:hidden}.grid-toolbar{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:6px;background:#f7f8f9;border-bottom:1px solid #c7cbd0}.grid-caption,.detail-caption{display:flex;align-items:center;gap:12px;white-space:nowrap}.grid-caption span,.detail-caption span{color:#727a82;font-size:12px}.actions{display:flex;gap:4px;flex-wrap:wrap;justify-content:flex-end}.actions button{min-height:27px;padding:3px 8px;font-size:12px}.actions button:disabled{opacity:.55}.actions button.primary,.primary-action{background:#ffab31!important;border-color:#ea9a24!important;color:#fff!important}.grid-scroll{overflow:auto;flex:1}.inventory-grid{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap}.inventory-grid th,.inventory-grid td{border:1px solid #c8ccd0;padding:5px 8px;height:27px}.inventory-grid th{position:sticky;top:0;z-index:1;background:#62aee9;color:#fff;font-weight:600;text-align:left}.inventory-grid tbody tr{cursor:pointer}.inventory-grid tbody tr:nth-child(even){background:#fff4c7}.inventory-grid tbody tr:hover{background:#f8e9ab}.inventory-grid tbody tr.active{outline:2px solid #739ec3;outline-offset:-2px}.inventory-grid tbody tr.checked{background:#ffedaa}.inventory-grid .numeric{text-align:right;font-variant-numeric:tabular-nums}.check-column{width:28px;text-align:center}.empty-row{text-align:center!important;color:#7a8086;padding:18px!important}.detail-panel{height:148px;min-height:110px;display:flex;flex-direction:column;border-top:1px solid #b9bec3}.detail-caption{padding:6px 8px;background:#f7f8f9;border-bottom:1px solid #c8ccd0}.detail-scroll{max-height:110px}.compact-grid th,.compact-grid td{height:25px;padding:4px 8px}.page-footer{display:flex;align-items:center;gap:13px;padding:0 3px;color:#656d74}.footer-spacer{flex:1}.page-footer button{min-width:72px}.feedback{margin:0;padding:7px 10px;border-radius:3px}.feedback.error{background:#fde8e8;color:#962d2d}.feedback.success{background:#e8f4e9;color:#245b2a}.production-note{margin:0;color:#8b6a16;font-size:12px}.modal-backdrop{position:fixed;inset:0;z-index:1000;background:rgba(26,31,36,.38);display:grid;place-items:center;padding:18px}.modal-card{width:min(690px,96vw);max-height:90vh;overflow:auto;background:white;border:1px solid #aeb3b8;box-shadow:0 8px 28px #0003;border-radius:4px}.modal-card>header{display:flex;align-items:center;justify-content:space-between;padding:11px 14px;border-bottom:1px solid #d5d8db}.modal-card h2{font-size:17px;margin:0}.modal-close{border:0;background:transparent;font-size:22px}.modal-intro,.modal-card>p{margin:14px}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px;padding:15px}.form-grid label{display:flex;flex-direction:column;gap:4px}.form-grid label span{font-size:12px;color:#525a62}.form-grid input,.form-grid select{width:100%}.wide-field{grid-column:1/-1}.modal-card>footer{display:flex;justify-content:flex-end;gap:8px;padding:11px 14px;border-top:1px solid #d5d8db}.primary-action{min-width:95px;border:1px solid #e89a24;border-radius:3px}.modal-card .feedback{margin:0 14px 10px}@media(max-width:1100px){.query-grid{grid-template-columns:1fr 1fr 1fr}.query-button{justify-self:start}}@media(max-width:700px){.query-grid{grid-template-columns:1fr 1fr}.date-field{flex-direction:column;align-items:stretch}.grid-toolbar{align-items:flex-start;flex-direction:column}.actions{justify-content:flex-start}.form-grid{grid-template-columns:1fr}}
</style>

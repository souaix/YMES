<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type InventoryType = 1 | 2 | 6
type InventoryBasis = { inventoryNo: string; inventoryName: string; inventoryType: InventoryType }
type LocatorBasis = { inventoryNo: string; locatorNo: string; capacity: number }
type StockRow = {
  rowId: string; inventoryNo: string; productNo: string; productName: string; lotNo: string
  locatorNo: string; locQty: number; unitNo: string; belongToType: number; belongToNo: string
  inputDate: string; baseLotNo: string; semiInNo: string
}
type SemiComponent = { rowId: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type BinDefinition = { binNo: string; binOrder: number }
type BinState = { rowId: string; quantities: Record<string, number> }
type TicketComponent = { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type TicketDetail = {
  productNo: string; productName: string; lotNo: string; qty: number; unitNo: string
  locatorNo: string; baseLotNo: string; components: TicketComponent[]
}
type SemiInTicket = {
  semiInNo: string; state: number; source: number; inventoryNo: string; fromInventoryNo: string
  description: string; creator: string; createDate: string; inputDate: string; details: TicketDetail[]
}
type MoveLog = { id: number; lotNo: string; fromInventoryNo: string; toInventoryNo: string; qty: number; at: string }
type Snapshot = {
  version: 2; inventories: InventoryBasis[]; locators: LocatorBasis[]; bins: BinDefinition[]
  rows: StockRow[]; components: SemiComponent[]; binStates: BinState[]; tickets: SemiInTicket[]
  moveLogs: MoveLog[]; nextId: number
}

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp604-semi-inventory.v2'

function seedSnapshot(): Snapshot {
  const inventories: InventoryBasis[] = [
    { inventoryNo: 'DEV-SEMI-01', inventoryName: '半成品主倉', inventoryType: 2 },
    { inventoryNo: 'DEV-SEMI-02', inventoryName: '半成品待驗倉', inventoryType: 2 },
    { inventoryNo: 'DEV-WIP-01', inventoryName: '在製品線邊倉', inventoryType: 6 },
    { inventoryNo: 'DEV-FGD-01', inventoryName: '成品主倉', inventoryType: 1 },
  ]
  const locators: LocatorBasis[] = [
    { inventoryNo: 'DEV-SEMI-01', locatorNo: 'A-01', capacity: 7000 },
    { inventoryNo: 'DEV-SEMI-01', locatorNo: 'A-02', capacity: 0 },
    { inventoryNo: 'DEV-SEMI-02', locatorNo: 'B-01', capacity: 3500 },
    { inventoryNo: 'DEV-WIP-01', locatorNo: 'WIP-01', capacity: 2500 },
    { inventoryNo: 'DEV-FGD-01', locatorNo: 'FGD-01', capacity: 0 },
  ]
  const rows: StockRow[] = Array.from({ length: 10 }, (_, index) => {
    const n = index + 1
    return {
      rowId: `semi-${n}`, inventoryNo: n < 8 ? 'DEV-SEMI-01' : 'DEV-SEMI-02',
      productNo: `PRD-${String(42100 + n).padStart(7, '0')}`, productName: ['控制板半成品', '電源模組組件', '封裝測試中間品'][index % 3],
      lotNo: `SL-26-${String(1700 + n).padStart(5, '0')}`, locatorNo: n < 8 ? (index % 2 ? 'A-02' : 'A-01') : 'B-01',
      locQty: 1840 - n * 97, unitNo: index % 4 ? 'PCS' : 'TRAY', belongToType: index % 3 ? 0 : 1,
      belongToNo: index % 3 ? `MO-26-${String(2100 + n).padStart(5, '0')}` : 'N/A',
      inputDate: `2026/11/${String(9 + (n % 10)).padStart(2, '0')} ${String(8 + (n % 9)).padStart(2, '0')}:15:00`,
      baseLotNo: `BASE-26-${String(580 + n).padStart(5, '0')}`, semiInNo: `SI-26-${String(500 + Math.ceil(n / 2)).padStart(5, '0')}`,
    }
  })
  rows.push({ rowId: 'wip-source-1', inventoryNo: 'DEV-WIP-01', productNo: 'PRD-0042101', productName: '控制板半成品', lotNo: 'SL-26-01751', locatorNo: 'WIP-01', locQty: 800, unitNo: 'PCS', belongToType: 0, belongToNo: 'MO-26-02101', inputDate: '2026/11/19 09:30:00', baseLotNo: 'BASE-26-00581', semiInNo: '' })
  const components: SemiComponent[] = rows.slice(0, 5).flatMap((row, index) => [
    { rowId: row.rowId, componentNo: `CMP-${String(index * 2 + 11).padStart(5, '0')}`, goodQty: 860 - index * 37, scrapQty: index + 2, unitNo: 'PCS' },
    { rowId: row.rowId, componentNo: `CMP-${String(index * 2 + 12).padStart(5, '0')}`, goodQty: 840 - index * 31, scrapQty: index, unitNo: 'PCS' },
  ])
  components.push(
    { rowId: 'wip-source-1', componentNo: 'CMP-00021', goodQty: 790, scrapQty: 10, unitNo: 'PCS' },
    { rowId: 'wip-source-1', componentNo: 'CMP-00022', goodQty: 800, scrapQty: 0, unitNo: 'PCS' },
  )
  const bins: BinDefinition[] = [
    { binNo: 'BIN1', binOrder: 10 }, { binNo: 'BIN3', binOrder: 20 }, { binNo: 'BIN5', binOrder: 30 },
    { binNo: 'BIN7', binOrder: 40 }, { binNo: 'BIN9', binOrder: 50 },
  ]
  const binStates: BinState[] = rows.slice(5).map((row, index) => ({
    rowId: row.rowId,
    quantities: Object.fromEntries(bins.map((bin, binIndex) => [bin.binNo, binIndex === index % bins.length ? 18 + index * 7 : 0])),
  }))
  const tickets: SemiInTicket[] = [
    {
      semiInNo: 'SI-26-00531', state: 0, source: 1, inventoryNo: 'DEV-SEMI-01', fromInventoryNo: 'DEV-WIP-01',
      description: 'DEV 在製品批入半成品庫', creator: 'A11019', createDate: '2026/11/19 09:35:12', inputDate: '2026/11/19 09:30:00',
      details: [{ productNo: 'PRD-0042101', productName: '控制板半成品', lotNo: 'SL-26-01751', qty: 620, unitNo: 'PCS', locatorNo: 'A-01', baseLotNo: 'BASE-26-00581', components: [
        { componentNo: 'CMP-00021', goodQty: 612, scrapQty: 8, unitNo: 'PCS' },
        { componentNo: 'CMP-00022', goodQty: 620, scrapQty: 0, unitNo: 'PCS' },
      ] }],
    },
    {
      semiInNo: 'SI-26-00532', state: 0, source: 0, inventoryNo: 'DEV-SEMI-02', fromInventoryNo: '',
      description: 'DEV 外部入庫待確認', creator: 'A11282', createDate: '2026/11/19 10:12:48', inputDate: '2026/11/19 10:00:00',
      details: [{ productNo: 'PRD-0042104', productName: '電源模組組件', lotNo: 'SL-26-01788', qty: 340, unitNo: 'PCS', locatorNo: 'B-01', baseLotNo: 'BASE-26-00602', components: [] }],
    },
  ]
  return { version: 2, inventories, locators, bins, rows, components, binStates, tickets, moveLogs: [], nextId: 1 }
}

function emptySnapshot(): Snapshot {
  return { version: 2, inventories: [], locators: [], bins: [], rows: [], components: [], binStates: [], tickets: [], moveLogs: [], nextId: 1 }
}

const state = reactive((() => {
  if (!dev.value) return emptySnapshot()
  const initial = seedSnapshot()
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<Snapshot> | null
    if (saved?.version === 2 && Array.isArray(saved.rows) && Array.isArray(saved.tickets)) return { ...initial, ...saved } as Snapshot
  } catch { /* invalid DEV data falls back to seeded records */ }
  return initial
})())

const inventoryNo = ref('')
const queriedInventoryNo = ref('')
const activeRowId = ref('')
const inventoryLookupOpen = ref(false)
const inventorySearch = ref('')
const turnOpen = ref(false)
const turnLotSearch = ref('')
const turnComponentSearch = ref('')
const turnSelectedIds = ref<string[]>([])
const destinationType = ref<InventoryType>(2)
const destinationInventoryNo = ref('')
const destinationLocatorNo = ref('')
const semiInOpen = ref(false)
const ticketPickerOpen = ref(false)
const ticketSearch = ref('')
const selectedTicketNo = ref('')
const actionError = ref('')
const actionMessage = ref('')

const visibleInventoryBases = computed(() => state.inventories
  .filter(item => item.inventoryType === 2)
  .filter(item => `${item.inventoryNo} ${item.inventoryName}`.toLocaleUpperCase().includes(inventorySearch.value.trim().toLocaleUpperCase())))
const visibleRows = computed(() => state.rows
  .filter(row => row.inventoryNo.toUpperCase() === queriedInventoryNo.value.toUpperCase())
  .sort((a, b) => a.productNo.localeCompare(b.productNo) || a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo)))
const activeRow = computed(() => visibleRows.value.find(row => row.rowId === activeRowId.value) ?? null)
const orderedBins = computed(() => [...state.bins].sort((a, b) => a.binOrder - b.binOrder))
const activeComponents = computed(() => activeRow.value ? state.components.filter(item => item.rowId === activeRow.value?.rowId) : [])
const activeBinState = computed(() => activeRow.value ? state.binStates.find(item => item.rowId === activeRow.value?.rowId) ?? null : null)
const detailMode = computed(() => activeComponents.value.length ? 'component' : activeBinState.value ? 'bin' : 'none')
const destinationInventories = computed(() => state.inventories
  .filter(item => item.inventoryType === destinationType.value)
  .filter(item => destinationType.value !== 2 || item.inventoryNo !== queriedInventoryNo.value))
const destinationLocators = computed(() => state.locators.filter(item => item.inventoryNo === destinationInventoryNo.value))
const destinationLocator = computed(() => destinationLocators.value.find(item => item.locatorNo === destinationLocatorNo.value) ?? null)
const turnRows = computed(() => {
  const lotNeedle = turnLotSearch.value.trim().toLocaleUpperCase()
  const componentNeedle = turnComponentSearch.value.trim().toLocaleUpperCase()
  return state.rows.filter(row => row.inventoryNo === queriedInventoryNo.value)
    .filter(row => !lotNeedle || row.lotNo.toLocaleUpperCase().includes(lotNeedle))
    .filter(row => !componentNeedle || state.components.some(component => component.rowId === row.rowId && component.componentNo.toLocaleUpperCase().includes(componentNeedle)))
    .sort((a, b) => a.productNo.localeCompare(b.productNo) || a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo))
})
const selectedTurnRows = computed(() => state.rows.filter(row => turnSelectedIds.value.includes(row.rowId)))
const selectedTurnQty = computed(() => selectedTurnRows.value.reduce((sum, row) => sum + row.locQty, 0))
const destinationUsedQty = computed(() => destinationType.value === 2
  ? state.rows.filter(row => row.inventoryNo === destinationInventoryNo.value && row.locatorNo === destinationLocatorNo.value).reduce((sum, row) => sum + row.locQty, 0)
  : 0)
const visibleTickets = computed(() => state.tickets.filter(ticket => ticket.state === 0)
  .filter(ticket => `${ticket.semiInNo} ${ticket.inventoryNo} ${ticket.description}`.toLocaleUpperCase().includes(ticketSearch.value.trim().toLocaleUpperCase()))
  .sort((a, b) => a.semiInNo.localeCompare(b.semiInNo)))
const selectedTicket = computed(() => state.tickets.find(ticket => ticket.semiInNo === selectedTicketNo.value) ?? null)

function persist(): void { if (dev.value) localStorage.setItem(storageKey, JSON.stringify(state)) }
function clearFeedback(): void { actionError.value = ''; actionMessage.value = '' }
function queryInventory(): void {
  clearFeedback()
  const value = inventoryNo.value.trim().toUpperCase()
  const basis = state.inventories.find(item => item.inventoryNo.toUpperCase() === value && item.inventoryType === 2)
  if (!basis) { actionError.value = '請輸入或選取有效的半成品庫編號。'; return }
  inventoryNo.value = basis.inventoryNo
  queriedInventoryNo.value = basis.inventoryNo
  activeRowId.value = ''
  actionMessage.value = `已載入 ${basis.inventoryNo} 的半成品庫存。`
}
function chooseInventory(item: InventoryBasis): void {
  inventoryNo.value = item.inventoryNo
  inventoryLookupOpen.value = false
  queryInventory()
}
function selectRow(row: StockRow): void { activeRowId.value = row.rowId; clearFeedback() }
function displayBelongTo(value: number): string { return value === 0 ? 'MO' : String(value) }
function formatQty(value: number): string { return Number(value).toLocaleString(undefined, { maximumFractionDigits: 6 }) }
function openTurn(): void {
  clearFeedback()
  if (!activeRow.value) { actionError.value = '請先選取一筆半成品庫存。'; return }
  turnSelectedIds.value = [activeRow.value.rowId]
  turnLotSearch.value = ''
  turnComponentSearch.value = ''
  destinationType.value = 2
  destinationInventoryNo.value = state.inventories.find(item => item.inventoryType === 2 && item.inventoryNo !== queriedInventoryNo.value)?.inventoryNo ?? ''
  destinationLocatorNo.value = state.locators.find(item => item.inventoryNo === destinationInventoryNo.value)?.locatorNo ?? ''
  turnOpen.value = true
}
function setDestinationType(value: InventoryType): void {
  destinationType.value = value
  destinationInventoryNo.value = state.inventories.find(item => item.inventoryType === value && (value !== 2 || item.inventoryNo !== queriedInventoryNo.value))?.inventoryNo ?? ''
  destinationLocatorNo.value = state.locators.find(item => item.inventoryNo === destinationInventoryNo.value)?.locatorNo ?? ''
}
function syncDestinationLocator(): void {
  destinationLocatorNo.value = state.locators.find(item => item.inventoryNo === destinationInventoryNo.value)?.locatorNo ?? ''
}
function toggleTurnRow(row: StockRow): void {
  turnSelectedIds.value = turnSelectedIds.value.includes(row.rowId)
    ? turnSelectedIds.value.filter(id => id !== row.rowId)
    : [...turnSelectedIds.value, row.rowId]
}
function selectByLot(): void {
  const needle = turnLotSearch.value.trim().toLocaleUpperCase()
  if (!needle) return
  const matches = turnRows.value.filter(row => row.lotNo.toLocaleUpperCase() === needle)
  if (!matches.length) { actionError.value = '查無此 Lot No。'; return }
  turnSelectedIds.value = [...new Set([...turnSelectedIds.value, ...matches.map(row => row.rowId)])]
  actionError.value = ''
}
function selectByComponent(): void {
  const needle = turnComponentSearch.value.trim().toLocaleUpperCase()
  if (!needle) return
  const componentRows = state.components.filter(item => item.componentNo.toLocaleUpperCase() === needle)
  const rowIds = componentRows.map(item => item.rowId).filter(id => state.rows.some(row => row.rowId === id && row.inventoryNo === queriedInventoryNo.value))
  if (!rowIds.length) { actionError.value = '查無符合此 Component No 的半成品庫存批號。'; return }
  turnSelectedIds.value = [...new Set([...turnSelectedIds.value, ...rowIds])]
  actionError.value = ''
}
function confirmTurn(): void {
  clearFeedback()
  const movingRows = [...selectedTurnRows.value]
  if (!movingRows.length) { actionError.value = '請勾選至少一筆 Lot。'; return }
  if (!destinationInventoryNo.value || !destinationLocatorNo.value) { actionError.value = '請指定目的庫房及儲位。'; return }
  const capacity = destinationLocator.value?.capacity ?? 0
  const movingQty = movingRows.reduce((sum, row) => sum + row.locQty, 0)
  if (capacity > 0 && destinationUsedQty.value + movingQty > capacity) { actionError.value = `目的儲位容量不足（現有 ${destinationUsedQty.value}，本次 ${movingQty}，容量 ${capacity}）。`; return }
  const at = new Date().toLocaleString()
  for (const row of movingRows) {
    state.moveLogs.unshift({ id: state.nextId++, lotNo: row.lotNo, fromInventoryNo: row.inventoryNo, toInventoryNo: destinationInventoryNo.value, qty: row.locQty, at })
    if (destinationType.value === 2) {
      const target = state.rows.find(item => item.inventoryNo === destinationInventoryNo.value && item.lotNo === row.lotNo && item.locatorNo === destinationLocatorNo.value)
      if (target) {
        target.locQty += row.locQty
        const movingComponents = state.components.filter(item => item.rowId === row.rowId)
        state.components.push(...movingComponents.map(item => ({ ...item, rowId: target.rowId })))
        state.components = state.components.filter(item => item.rowId !== row.rowId)
        const bin = state.binStates.find(item => item.rowId === row.rowId)
        if (bin) {
          const targetBin = state.binStates.find(item => item.rowId === target.rowId)
          if (targetBin) for (const key of Object.keys(bin.quantities)) targetBin.quantities[key] = (targetBin.quantities[key] ?? 0) + (bin.quantities[key] ?? 0)
          else state.binStates.push({ rowId: target.rowId, quantities: { ...bin.quantities } })
          state.binStates = state.binStates.filter(item => item.rowId !== row.rowId)
        }
        state.rows = state.rows.filter(item => item.rowId !== row.rowId)
      } else {
        row.inventoryNo = destinationInventoryNo.value
        row.locatorNo = destinationLocatorNo.value
      }
    } else {
      state.rows = state.rows.filter(item => item.rowId !== row.rowId)
      state.components = state.components.filter(item => item.rowId !== row.rowId)
      state.binStates = state.binStates.filter(item => item.rowId !== row.rowId)
    }
  }
  persist()
  activeRowId.value = ''
  queriedInventoryNo.value = inventoryNo.value
  turnOpen.value = false
  actionMessage.value = `已記錄 ${movingRows.length} 筆 DEV 移轉；正式過帳由 API 處理。`
}
function openSemiIn(): void {
  clearFeedback()
  selectedTicketNo.value = ''
  ticketSearch.value = ''
  semiInOpen.value = true
}
function openTicketPicker(): void {
  ticketSearch.value = ''
  ticketPickerOpen.value = true
}
function chooseTicket(ticket: SemiInTicket): void {
  selectedTicketNo.value = ticket.semiInNo
  ticketPickerOpen.value = false
}
function confirmSemiIn(): void {
  clearFeedback()
  const ticket = selectedTicket.value
  if (!ticket) { actionError.value = '請選取一張未確認的 SEMI In 單。'; return }
  if (ticket.state !== 0) { actionError.value = '此 SEMI In 單已不在未確認狀態，請重新查詢。'; return }
  if (!ticket.details.length) { actionError.value = '此 SEMI In 單沒有 Lot 明細。'; return }
  const incomingByLocator = new Map<string, number>()
  for (const detail of ticket.details) incomingByLocator.set(detail.locatorNo, (incomingByLocator.get(detail.locatorNo) ?? 0) + detail.qty)
  for (const [locatorNo, incomingQty] of incomingByLocator) {
    const capacity = state.locators.find(locator => locator.inventoryNo === ticket.inventoryNo && locator.locatorNo === locatorNo)?.capacity ?? 0
    const occupied = state.rows.filter(row => row.inventoryNo === ticket.inventoryNo && row.locatorNo === locatorNo).reduce((sum, row) => sum + row.locQty, 0)
    if (capacity > 0 && occupied + incomingQty > capacity) { actionError.value = `目的儲位 ${locatorNo} 容量不足（現有 ${occupied}，本次 ${incomingQty}，容量 ${capacity}）。`; return }
  }
  if (ticket.source === 1) {
    for (const detail of ticket.details) {
      const sourceQty = state.rows.filter(row => row.inventoryNo === ticket.fromInventoryNo && row.lotNo === detail.lotNo).reduce((sum, row) => sum + row.locQty, 0)
      if (sourceQty < detail.qty) { actionError.value = `來源庫 ${ticket.fromInventoryNo} 的 Lot ${detail.lotNo} 數量不足。`; return }
    }
  }
  for (const detail of ticket.details) {
    if (ticket.source === 1) {
      let remaining = detail.qty
      for (const sourceRow of state.rows.filter(row => row.inventoryNo === ticket.fromInventoryNo && row.lotNo === detail.lotNo)) {
        const deducted = Math.min(sourceRow.locQty, remaining)
        sourceRow.locQty -= deducted
        remaining -= deducted
        for (const component of detail.components) {
          const sourceComponent = state.components.find(item => item.rowId === sourceRow.rowId && item.componentNo === component.componentNo)
          if (sourceComponent) {
            sourceComponent.goodQty = Math.max(0, sourceComponent.goodQty - component.goodQty)
            sourceComponent.scrapQty = Math.max(0, sourceComponent.scrapQty - component.scrapQty)
          }
        }
        if (sourceRow.locQty <= 0) {
          state.rows = state.rows.filter(row => row.rowId !== sourceRow.rowId)
          state.components = state.components.filter(item => item.rowId !== sourceRow.rowId)
          state.binStates = state.binStates.filter(item => item.rowId !== sourceRow.rowId)
        }
        if (remaining <= 0) break
      }
    }
    const target = state.rows.find(row => row.inventoryNo === ticket.inventoryNo && row.lotNo === detail.lotNo && row.locatorNo === detail.locatorNo)
    if (target) {
      target.locQty += detail.qty
      for (const component of detail.components) {
        const existing = state.components.find(item => item.rowId === target.rowId && item.componentNo === component.componentNo)
        if (existing) { existing.goodQty += component.goodQty; existing.scrapQty += component.scrapQty }
        else state.components.push({ rowId: target.rowId, ...component })
      }
    } else {
      const rowId = `semi-dev-${state.nextId++}`
      state.rows.push({ rowId, inventoryNo: ticket.inventoryNo, productNo: detail.productNo, productName: detail.productName, lotNo: detail.lotNo, locatorNo: detail.locatorNo, locQty: detail.qty, unitNo: detail.unitNo, belongToType: 0, belongToNo: 'N/A', inputDate: ticket.inputDate, baseLotNo: detail.baseLotNo, semiInNo: ticket.semiInNo })
      state.components.push(...detail.components.map(item => ({ rowId, ...item })))
    }
  }
  ticket.state = 2
  state.moveLogs.unshift({ id: state.nextId++, lotNo: ticket.details[0].lotNo, fromInventoryNo: ticket.fromInventoryNo || 'EXTERNAL', toInventoryNo: ticket.inventoryNo, qty: ticket.details.reduce((sum, detail) => sum + detail.qty, 0), at: new Date().toLocaleString() })
  persist()
  semiInOpen.value = false
  if (queriedInventoryNo.value === ticket.inventoryNo) queryInventory()
  actionMessage.value = `${ticket.semiInNo} 已在 DEV 假資料中模擬確認；正式交易與 WIP 過帳需由 API 執行。`
}
function closePage(): void { emit('close') }
</script>

<template>
  <main class="semi-inventory-page">
    <header class="page-heading">
      <div><h1>SEMI Inventory</h1><p>半成品庫帳務維護 · WP604</p></div>
      <span v-if="dev" class="dev-badge">DEV 假資料</span>
    </header>

    <section class="inventory-query">
      <label for="semi-inventory-no">Inventory No</label>
      <input id="semi-inventory-no" v-model="inventoryNo" maxlength="20" autocomplete="off" @blur="inventoryNo = inventoryNo.toUpperCase()" @keydown.enter.prevent="queryInventory">
      <button type="button" class="lookup-button" aria-label="選擇半成品庫" @click="inventoryLookupOpen = true">...</button>
      <button type="button" class="query-button" @click="queryInventory">Query</button>
    </section>

    <section class="inventory-layout">
      <div class="grids-column">
        <section class="grid-frame stock-frame">
          <div class="grid-title">{{ queriedInventoryNo || '半成品庫存清單' }}<span>資料 {{ visibleRows.length }} 筆</span></div>
          <div class="grid-scroll stock-scroll">
            <table class="data-grid stock-grid">
              <thead><tr>
                <th>ProductNo</th><th>LotNo</th><th>LocatorNo</th><th class="number">LOCQty</th>
                <th>UnitNo</th><th>BelongToType</th><th>BelongToNo</th><th>InputDate</th>
              </tr></thead>
              <tbody>
                <tr v-for="row in visibleRows" :key="row.rowId" :class="{ active: activeRowId === row.rowId }" @click="selectRow(row)">
                  <td>{{ row.productNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td class="number">{{ formatQty(row.locQty) }}</td>
                  <td>{{ row.unitNo }}</td><td>{{ displayBelongTo(row.belongToType) }}</td><td>{{ row.belongToNo }}</td><td>{{ row.inputDate }}</td>
                </tr>
                <tr v-if="!visibleRows.length"><td colspan="8" class="empty-row">{{ queriedInventoryNo ? '此庫房沒有庫存資料' : '輸入半成品庫編號，按 Enter 或 ... 選擇庫房' }}</td></tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="grid-frame detail-frame">
          <div class="grid-title">{{ detailMode === 'component' ? 'Component' : detailMode === 'bin' ? 'Good Bin' : '明細' }}<span>{{ activeRow?.lotNo || '選取庫存列以載入明細' }}</span></div>
          <div v-if="detailMode === 'component'" class="grid-scroll detail-scroll">
            <table class="data-grid detail-grid"><thead><tr><th>ComponentNo</th><th class="number">GoodQty</th><th class="number">ScrapQty</th><th>UnitNo</th></tr></thead>
              <tbody><tr v-for="item in activeComponents" :key="item.rowId + item.componentNo"><td>{{ item.componentNo }}</td><td class="number">{{ formatQty(item.goodQty) }}</td><td class="number">{{ formatQty(item.scrapQty) }}</td><td>{{ item.unitNo }}</td></tr>
                <tr v-if="!activeComponents.length"><td colspan="4" class="empty-row">無元件明細</td></tr></tbody>
            </table>
          </div>
          <div v-else-if="detailMode === 'bin'" class="grid-scroll detail-scroll">
            <table class="data-grid detail-grid"><thead><tr><th v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ bin.binNo }}</th></tr></thead>
              <tbody><tr><td v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ formatQty(activeBinState?.quantities[bin.binNo] ?? 0) }}</td></tr></tbody>
            </table>
          </div>
          <div v-else class="detail-placeholder">{{ activeRow ? '此 Lot 沒有 Component 或 Bin 明細' : '選取上方半成品庫存列' }}</div>
        </section>
      </div>

      <aside class="form-actions">
        <button type="button" :disabled="!dev" @click="openSemiIn">SEMI In</button>
        <button type="button" :disabled="!dev || !activeRow" @click="openTurn">Turn Inv</button>
        <span class="action-spacer"></span>
        <button type="button" @click="closePage">Close</button>
      </aside>
    </section>

    <div v-if="actionError" class="feedback error" role="alert">{{ actionError }}</div>
    <div v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</div>
    <div v-if="!dev" class="production-note">此頁目前是公司端 DEV 預覽；正式庫存移轉與 SEMI In 過帳由家用端 API 接手。</div>

    <div v-if="inventoryLookupOpen" class="overlay" @click.self="inventoryLookupOpen = false">
      <section class="dialog lookup-dialog" role="dialog" aria-modal="true" aria-label="選擇半成品庫">
        <header><h2>Inventory Search</h2><button type="button" class="x-button" @click="inventoryLookupOpen = false">×</button></header>
        <label class="dialog-search"><span>Inventory No / Name</span><input v-model="inventorySearch" autofocus></label>
        <div class="grid-scroll lookup-scroll"><table class="data-grid"><thead><tr><th>InventoryNo</th><th>InventoryName</th></tr></thead><tbody>
          <tr v-for="item in visibleInventoryBases" :key="item.inventoryNo" tabindex="0" @click="chooseInventory(item)" @keydown.enter="chooseInventory(item)"><td>{{ item.inventoryNo }}</td><td>{{ item.inventoryName }}</td></tr>
          <tr v-if="!visibleInventoryBases.length"><td colspan="2" class="empty-row">沒有符合的半成品庫</td></tr>
        </tbody></table></div>
        <footer><button type="button" @click="inventoryLookupOpen = false">Close</button></footer>
      </section>
    </div>

    <div v-if="turnOpen" class="overlay" @click.self="turnOpen = false">
      <section class="dialog turn-dialog" role="dialog" aria-modal="true" aria-label="SEMI Turn Inventory">
        <header><div><h2>SEMI Turn Inventory</h2><small>來源庫房：{{ queriedInventoryNo }}</small></div><button type="button" class="x-button" @click="turnOpen = false">×</button></header>
        <div class="turn-search-row">
          <label><span>LotNo</span><input v-model="turnLotSearch" @keydown.enter.prevent="selectByLot"></label><button type="button" @click="selectByLot">Select Lot</button>
          <label><span>ComponentNo</span><input v-model="turnComponentSearch" @keydown.enter.prevent="selectByComponent"></label><button type="button" @click="selectByComponent">Select Component</button>
        </div>
        <div class="grid-scroll turn-grid-scroll"><table class="data-grid turn-grid"><thead><tr><th>選取</th><th>LotNo</th><th>LocatorNo</th><th>ProductNo</th><th>BaseLotNo</th><th class="number">LOCQty</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="row in turnRows" :key="row.rowId" :class="{ checked: turnSelectedIds.includes(row.rowId) }" @click="toggleTurnRow(row)">
            <td><input type="checkbox" :checked="turnSelectedIds.includes(row.rowId)" @click.stop @change="toggleTurnRow(row)"></td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.productNo }}</td><td>{{ row.baseLotNo }}</td><td class="number">{{ formatQty(row.locQty) }}</td><td>{{ row.unitNo }}</td>
          </tr>
          <tr v-if="!turnRows.length"><td colspan="7" class="empty-row">沒有符合條件的庫存批</td></tr>
        </tbody></table></div>
        <section class="destination-box">
          <h3>Turn to Inventory</h3>
          <div class="destination-types">
            <label><input type="radio" :checked="destinationType === 6" @change="setDestinationType(6)">WIP INV</label>
            <label><input type="radio" :checked="destinationType === 1" @change="setDestinationType(1)">FGD INV</label>
            <label><input type="radio" :checked="destinationType === 2" @change="setDestinationType(2)">SEMI INV</label>
          </div>
          <div class="destination-fields">
            <label><span>InventoryNo</span><select v-model="destinationInventoryNo" @change="syncDestinationLocator"><option value="">請選擇</option><option v-for="item in destinationInventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} — {{ item.inventoryName }}</option></select></label>
            <label><span>LocatorNo</span><select v-model="destinationLocatorNo"><option value="">請選擇</option><option v-for="item in destinationLocators" :key="item.locatorNo" :value="item.locatorNo">{{ item.locatorNo }}<template v-if="item.capacity">（容量 {{ item.capacity }}）</template></option></select></label>
          </div>
          <div class="selected-summary">已選 {{ selectedTurnRows.length }} 筆，總數量 {{ formatQty(selectedTurnQty) }}</div>
        </section>
        <p v-if="actionError" class="feedback error">{{ actionError }}</p>
        <footer><button type="button" @click="turnOpen = false">Close</button><button type="button" class="primary-button" @click="confirmTurn">Confirm</button></footer>
      </section>
    </div>

    <div v-if="semiInOpen" class="overlay" @click.self="semiInOpen = false">
      <section class="dialog semi-in-dialog" role="dialog" aria-modal="true" aria-label="INV SEMI In Confirm">
        <header><h2>INV SEMI In Confirm</h2><button type="button" class="x-button" @click="semiInOpen = false">×</button></header>
        <section class="ticket-basis">
          <label class="ticket-number"><span>SEMIIn No</span><input :value="selectedTicket?.semiInNo || ''" readonly><button type="button" class="lookup-button" @click="openTicketPicker">...</button></label>
          <label><span>Inventory No</span><input :value="selectedTicket?.inventoryNo || ''" readonly></label>
          <label class="description-field"><span>Description</span><textarea :value="selectedTicket?.description || ''" readonly></textarea></label>
          <label><span>Creator</span><input :value="selectedTicket?.creator || ''" readonly></label>
          <label><span>Create Date</span><input :value="selectedTicket?.createDate || ''" readonly></label>
        </section>
        <section class="ticket-detail-box"><h3>Detail</h3><div class="grid-scroll ticket-grid-scroll"><table class="data-grid ticket-grid"><thead><tr><th>ProductNo</th><th>LotNo</th><th class="number">Qty</th><th>UnitNo</th><th>LocatorNo</th></tr></thead><tbody>
          <template v-for="detail in selectedTicket?.details ?? []" :key="detail.lotNo + detail.locatorNo">
            <tr><td>{{ detail.productNo }}</td><td>{{ detail.lotNo }}</td><td class="number">{{ formatQty(detail.qty) }}</td><td>{{ detail.unitNo }}</td><td>{{ detail.locatorNo }}</td></tr>
            <tr v-for="component in detail.components" :key="detail.lotNo + component.componentNo" class="component-child"><td colspan="2">↳ {{ component.componentNo }}</td><td class="number">{{ formatQty(component.goodQty) }} / {{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td><td>GoodQty / ScrapQty</td></tr>
          </template>
          <tr v-if="!selectedTicket?.details.length"><td colspan="5" class="empty-row">選取未確認的 SEMI In 單後顯示明細</td></tr>
        </tbody></table></div></section>
        <p v-if="actionError" class="feedback error">{{ actionError }}</p>
        <footer><button type="button" @click="semiInOpen = false">Close</button><button type="button" class="primary-button" :disabled="!dev" @click="confirmSemiIn">Confirm</button></footer>
      </section>
    </div>

    <div v-if="ticketPickerOpen" class="overlay nested-overlay" @click.self="ticketPickerOpen = false">
      <section class="dialog lookup-dialog ticket-picker" role="dialog" aria-modal="true" aria-label="選擇未確認 SEMI In 單">
        <header><h2>Unconfirmed SEMI In</h2><button type="button" class="x-button" @click="ticketPickerOpen = false">×</button></header>
        <label class="dialog-search"><span>SEMIInNo / InventoryNo</span><input v-model="ticketSearch" autofocus></label>
        <div class="grid-scroll lookup-scroll"><table class="data-grid"><thead><tr><th>SEMIInNo</th><th>InventoryNo</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
          <tr v-for="ticket in visibleTickets" :key="ticket.semiInNo" tabindex="0" @click="chooseTicket(ticket)" @keydown.enter="chooseTicket(ticket)"><td>{{ ticket.semiInNo }}</td><td>{{ ticket.inventoryNo }}</td><td>{{ ticket.description }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td></tr>
          <tr v-if="!visibleTickets.length"><td colspan="5" class="empty-row">目前沒有未確認的 SEMI In 單</td></tr>
        </tbody></table></div>
        <footer><button type="button" @click="ticketPickerOpen = false">Close</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.semi-inventory-page{height:100%;min-height:540px;display:flex;flex-direction:column;gap:8px;background:#fff;color:#30343a;font-size:13px}.page-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 7px;border-bottom:1px solid #d7dade}.page-heading h1{margin:0;font-size:20px;font-weight:600}.page-heading p{margin:3px 0 0;color:#77808a;font-size:12px}.dev-badge{padding:4px 9px;background:#fff2be;border:1px solid #e3c05e;border-radius:3px;color:#715300}.inventory-query{display:flex;align-items:center;gap:7px;min-height:43px;padding:5px 8px;border:1px solid #bfc4c9;background:#f7f8f9}.inventory-query label{width:82px;text-align:right}.inventory-query input,.ticket-basis input,.ticket-basis textarea,.dialog-search input,.turn-search-row input,.destination-fields select{height:29px;border:1px solid #969da4;background:#fff0b5;padding:4px 7px;color:#25292e}.inventory-query input{width:180px}.lookup-button,.query-button,.form-actions button,.dialog footer button,.turn-search-row button{min-height:30px;padding:4px 11px;border:1px solid #b8bdc2;border-radius:3px;background:#fafafa;color:#25292e}.lookup-button{width:32px;padding:2px 5px}.query-button,.primary-button{background:#ffab31!important;border-color:#ea9a24!important;color:#fff!important}.inventory-layout{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:8px;flex:1;min-height:0}.grids-column{display:flex;flex-direction:column;gap:7px;min-width:0}.grid-frame{display:flex;flex-direction:column;min-height:0;border:1px solid #b8bdc2;overflow:hidden}.stock-frame{height:58%;min-height:230px}.detail-frame{height:42%;min-height:145px}.grid-title{display:flex;align-items:center;justify-content:space-between;gap:8px;min-height:26px;padding:3px 7px;background:#f4f5f6;border-bottom:1px solid #c8ccd0;font-weight:600}.grid-title span{font-size:11px;font-weight:400;color:#747b82}.grid-scroll{overflow:auto;min-height:0;flex:1}.data-grid{width:100%;border-collapse:collapse;white-space:nowrap}.data-grid th,.data-grid td{height:26px;padding:4px 7px;border:1px solid #c8ccd0;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#62aee9;color:#fff;font-weight:600}.stock-grid{min-width:850px}.stock-grid tbody tr,.lookup-scroll tbody tr{cursor:pointer}.stock-grid tbody tr:nth-child(even),.ticket-grid tbody tr:nth-child(even){background:#fff2bf}.stock-grid tbody tr:hover,.lookup-scroll tbody tr:hover{background:#f7e7a4}.stock-grid tbody tr.active{outline:2px solid #779fc0;outline-offset:-2px;background:#ffedaa}.data-grid .number{text-align:right;font-variant-numeric:tabular-nums}.empty-row{text-align:center!important;color:#777f87;padding:14px!important}.detail-grid{min-width:100%}.detail-placeholder{display:grid;place-items:center;flex:1;color:#858b91}.form-actions{display:flex;flex-direction:column;gap:8px;padding-top:2px}.form-actions button{width:100%;min-height:34px}.form-actions button:disabled{opacity:.5}.action-spacer{flex:1}.feedback{margin:0;padding:7px 10px;border-radius:3px}.feedback.error{background:#fde8e8;color:#a33434}.feedback.success{background:#e5f5e9;color:#246438}.production-note{margin:0;color:#79601a;font-size:12px}.overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#20262c66}.nested-overlay{z-index:1010}.dialog{display:flex;flex-direction:column;max-height:min(90vh,760px);border:1px solid #b6bbc0;background:white;box-shadow:0 12px 36px #0003}.dialog>header{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:10px 14px;border-bottom:1px solid #d1d4d7;background:#f7f8f9}.dialog h2{margin:0;font-size:17px;font-weight:600}.dialog header small{display:block;margin-top:3px;color:#6f7780}.x-button{width:30px;height:30px;border:0;background:transparent;font-size:22px;color:#59616a}.dialog footer{display:flex;justify-content:flex-end;gap:7px;padding:9px 12px;border-top:1px solid #d2d5d8}.lookup-dialog{width:min(700px,96vw);height:min(490px,88vh)}.dialog-search{display:flex;align-items:center;gap:9px;padding:10px 12px}.dialog-search span{white-space:nowrap}.dialog-search input{flex:1}.lookup-scroll{margin:0 12px 8px}.lookup-scroll tbody tr:focus{outline:2px solid #6197c0;outline-offset:-2px}.turn-dialog{width:min(860px,97vw);height:min(760px,92vh)}.turn-search-row{display:grid;grid-template-columns:auto minmax(100px,1fr) auto auto minmax(110px,1fr) auto;align-items:center;gap:6px;padding:9px 12px}.turn-search-row label{display:contents}.turn-search-row label span{white-space:nowrap}.turn-search-row input{width:100%;min-width:0}.turn-search-row button{font-size:11px;padding:3px 7px}.turn-grid-scroll{margin:0 12px;min-height:115px;max-height:34vh}.turn-grid{min-width:710px}.turn-grid tbody tr{cursor:pointer}.turn-grid tbody tr.checked{background:#ffedaa}.destination-box{margin:9px 12px 0;padding:8px 10px;border:1px solid #c2c7cc}.destination-box h3,.ticket-detail-box h3{margin:0 0 7px;font-size:13px;font-weight:600}.destination-types{display:flex;gap:18px;margin-bottom:9px}.destination-types label{display:flex;align-items:center;gap:5px}.destination-fields{display:grid;grid-template-columns:1fr 1fr;gap:12px}.destination-fields label,.ticket-basis label{display:grid;grid-template-columns:100px minmax(0,1fr);align-items:center;gap:8px}.destination-fields select{width:100%}.selected-summary{margin-top:8px;text-align:right;color:#6f7780}.turn-dialog>.feedback{margin:6px 12px 0}.semi-in-dialog{width:min(720px,96vw);height:min(720px,92vh)}.ticket-basis{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;padding:11px 13px}.ticket-basis label span{text-align:right}.ticket-basis input{width:100%;background:#eee}.ticket-basis textarea{height:49px;resize:none;background:#fff0b5}.ticket-number{grid-column:1 / -1;grid-template-columns:100px minmax(0,1fr) 34px!important}.description-field{grid-row:2 / span 2}.description-field textarea{grid-row:1 / span 2}.ticket-detail-box{display:flex;flex-direction:column;min-height:160px;flex:1;margin:0 13px 8px;border:1px solid #c2c7cc}.ticket-detail-box h3{padding:7px 8px;margin:0;border-bottom:1px solid #c2c7cc;background:#f7f8f9}.ticket-grid-scroll{flex:1;overflow:auto}.ticket-grid{min-width:600px}.component-child{background:#f4f5f6!important;color:#59636b;font-size:12px}.semi-in-dialog>.feedback{margin:0 13px 6px}.ticket-picker{width:min(860px,97vw);height:min(520px,88vh)}
@media(max-width:850px){.semi-inventory-page{min-height:460px}.inventory-layout{grid-template-columns:76px minmax(0,1fr)}.grids-column{grid-column:2;grid-row:1}.form-actions{grid-column:1;grid-row:1}.inventory-query{flex-wrap:wrap}.stock-frame{height:55%}.detail-frame{height:45%}.ticket-basis{grid-template-columns:1fr}.ticket-number,.description-field{grid-column:auto;grid-row:auto}.description-field textarea{grid-row:auto}.destination-fields{grid-template-columns:1fr}.turn-search-row{grid-template-columns:auto 1fr auto}.turn-search-row label:nth-of-type(2){grid-column:1 / 3}.turn-search-row button:last-child{grid-column:3}}
</style>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type InventoryType = 1 | 2 | 6
type InventoryRow = {
  inventoryNo: string
  productNo: string
  moNo: string
  lotNo: string
  locatorNo: string
  quantity: number
  unitNo: string
  belongToType: string
  belongToNo: string
  inputDate: string
  baseLotNo: string
}
type InventoryBasis = { inventoryNo: string; inventoryName: string; inventoryType: InventoryType }
type Locator = { inventoryNo: string; locatorNo: string; capacity: number; issueState: number }
type LotBasis = { lotNo: string; productNo: string; curUnitNo: string; baseLotNo: string; status: number }
type ComponentRow = { inventoryNo: string; lotNo: string; locatorNo: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type BinDefinition = { binNo: string; binOrder: number }
type BinStateRow = { lotNo: string; quantities: Record<string, number> }
type FgdInDetail = { lotNo: string; productNo: string; productVersion: string; quantity: number; unitNo: string; locatorNo: string; baseLotNo: string }
type FgdInComponent = { lotNo: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
type FgdInTicket = {
  fgdInNo: string; state: number; description: string; creator: string; createDate: string
  inventoryNo: string; inputDate: string; source: number; fromInventoryNo: string; toErp: number
  details: FgdInDetail[]; components: FgdInComponent[]
}
type TransactionState = { transactionId: string; source: number; ticketNo: string; creator: string; createDate: string }
type Movement = { lotNo: string; fromInventoryNo: string; fromLocatorNo: string; toInventoryNo: string; toLocatorNo: string; inventoryType: InventoryType; quantity: number; time: string }
type Snapshot = {
  version: 2
  inventories: InventoryBasis[]
  locators: Locator[]
  lots: LotBasis[]
  inventoryRows: InventoryRow[]
  components: ComponentRow[]
  binDefinitions: BinDefinition[]
  bins: BinStateRow[]
  fgdInTickets: FgdInTicket[]
  transactions: TransactionState[]
  movements: Movement[]
  transactionSequence: number
}
type Dialog = null | 'inventoryLookup' | 'locatorLookup' | 'lotLookup' | 'entry' | 'turn' | 'fgdIn'

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp605-fgd-inventory.v2'
const inventoryTypeNames: Record<InventoryType, string> = { 1: '成品庫', 2: '半成品庫', 6: '在製品庫' }

function seedSnapshot(): Snapshot {
  const inventories: InventoryBasis[] = [
    { inventoryNo: 'DEV-FGD-01', inventoryName: '成品主倉', inventoryType: 1 },
    { inventoryNo: 'DEV-FGD-02', inventoryName: '成品待驗倉', inventoryType: 1 },
    { inventoryNo: 'DEV-SEMI-01', inventoryName: '半成品倉', inventoryType: 2 },
    { inventoryNo: 'DEV-WIP-01', inventoryName: '在製品線邊倉', inventoryType: 6 },
  ]
  const inventoryRows: InventoryRow[] = Array.from({ length: 12 }, (_, index) => {
    const number = index + 1
    return {
      inventoryNo: number < 9 ? 'DEV-FGD-01' : 'DEV-FGD-02',
      productNo: 'DEV-PRD-' + String(Math.ceil(number / 3)).padStart(3, '0'),
      moNo: 'DEV-MO-' + String(Math.ceil(number / 3)).padStart(4, '0'),
      lotNo: 'DEV-LOT-' + String(number).padStart(3, '0'),
      locatorNo: number % 2 ? 'A-' + String(Math.ceil(number / 2)).padStart(2, '0') : 'B-' + String(Math.ceil(number / 2)).padStart(2, '0'),
      quantity: 1200 - number * 37,
      unitNo: number % 3 ? 'PCS' : 'TRAY',
      belongToType: number % 4 ? 'MO' : '',
      belongToNo: number % 4 ? 'DEV-MO-' + String(Math.ceil(number / 3)).padStart(4, '0') : '',
      inputDate: '2026-11-' + String(10 + (number % 9)).padStart(2, '0') + ' 09:30:00',
      baseLotNo: 'DEV-BASE-' + String(Math.ceil(number / 2)).padStart(3, '0'),
    }
  })
  const components: ComponentRow[] = [
    { inventoryNo: 'DEV-FGD-01', lotNo: 'DEV-LOT-001', locatorNo: 'A-01', componentNo: 'DEV-COMP-001', goodQty: 1180, scrapQty: 20, unitNo: 'PCS' },
    { inventoryNo: 'DEV-FGD-01', lotNo: 'DEV-LOT-001', locatorNo: 'A-01', componentNo: 'DEV-COMP-002', goodQty: 1170, scrapQty: 30, unitNo: 'PCS' },
    { inventoryNo: 'DEV-FGD-01', lotNo: 'DEV-LOT-004', locatorNo: 'B-02', componentNo: 'DEV-COMP-003', goodQty: 1052, scrapQty: 0, unitNo: 'PCS' },
  ]
  const locators: Locator[] = inventories.flatMap(inventory =>
    ['A-01', 'A-02', 'B-01', 'B-02'].map((locatorNo, index) => ({
      inventoryNo: inventory.inventoryNo, locatorNo, capacity: index === 3 ? 0 : 5000 + index * 1000, issueState: 2,
    })),
  )
  const lots: LotBasis[] = inventoryRows.map((row, index) => ({
    lotNo: row.lotNo, productNo: row.productNo, curUnitNo: row.unitNo, baseLotNo: row.baseLotNo,
    status: index % 4 === 0 ? 99 : 11,
  }))
  const binDefinitions: BinDefinition[] = [
    { binNo: 'DEV-BIN-001', binOrder: 10 },
    { binNo: 'DEV-BIN-002', binOrder: 20 },
    { binNo: 'DEV-BIN-003', binOrder: 30 },
  ]
  const bins: BinStateRow[] = [
    { lotNo: 'DEV-LOT-002', quantities: { 'DEV-BIN-001': 220, 'DEV-BIN-002': 314, 'DEV-BIN-003': 0 } },
    { lotNo: 'DEV-LOT-003', quantities: { 'DEV-BIN-001': 0, 'DEV-BIN-002': 0, 'DEV-BIN-003': 1089 } },
  ]
  const fgdInTickets: FgdInTicket[] = [
    {
      fgdInNo: 'DEV-FGDIN-0001', state: 0, description: '完工入庫', creator: 'DEV-USER', createDate: '2026-11-18 08:20:00',
      inventoryNo: 'DEV-FGD-01', inputDate: '2026-11-18 08:00:00', source: 30, fromInventoryNo: '',
      toErp: 0,
      details: [{ lotNo: 'DEV-LOT-011', productNo: 'DEV-PRD-004', productVersion: 'A01', quantity: 756, unitNo: 'PCS', locatorNo: 'A-01', baseLotNo: 'DEV-BASE-006' }],
      components: [{ lotNo: 'DEV-LOT-011', componentNo: 'DEV-COMP-011', goodQty: 750, scrapQty: 6, unitNo: 'PCS' }],
    },
    {
      fgdInNo: 'DEV-FGDIN-0002', state: 0, description: '重工完成入庫', creator: 'DEV-USER', createDate: '2026-11-18 13:45:00',
      inventoryNo: 'DEV-FGD-02', inputDate: '2026-11-18 13:30:00', source: 30, fromInventoryNo: '',
      toErp: 0,
      details: [{ lotNo: 'DEV-LOT-012', productNo: 'DEV-PRD-004', productVersion: 'A01', quantity: 518, unitNo: 'PCS', locatorNo: 'B-02', baseLotNo: 'DEV-BASE-006' }],
      components: [],
    },
    {
      fgdInNo: 'DEV-FGDIN-0003', state: 0, description: '待 ERP 過帳，不顯示於可確認清單', creator: 'DEV-USER', createDate: '2026-11-18 15:10:00',
      inventoryNo: 'DEV-FGD-01', inputDate: '2026-11-18 15:00:00', source: 30, fromInventoryNo: '',
      toErp: 1,
      details: [{ lotNo: 'DEV-LOT-013', productNo: 'DEV-PRD-005', productVersion: 'A02', quantity: 302, unitNo: 'PCS', locatorNo: 'A-02', baseLotNo: 'DEV-BASE-007' }],
      components: [],
    },
  ]
  return { version: 2, inventories, locators, lots, inventoryRows, components, binDefinitions, bins, fgdInTickets, transactions: [], movements: [], transactionSequence: 1 }
}

function readSnapshot(): Snapshot {
  const initial = seedSnapshot()
  if (!dev.value) return initial
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<Snapshot> | null
    if (saved?.version === 2 && Array.isArray(saved.inventoryRows) && Array.isArray(saved.transactions))
      return { ...initial, ...saved } as Snapshot
  } catch { /* invalid DEV snapshot falls back to source-shaped seed rows */ }
  return initial
}

const state = reactive(readSnapshot())
const inventoryInput = ref('')
const currentInventoryNo = ref('')
const activeRowKey = ref('')
const selectedRowKeys = ref<string[]>([])
const actionMessage = ref('')
const actionError = ref('')
const dialog = ref<Dialog>(null)
const entryMode = ref<'add' | 'edit'>('add')
const entry = reactive({ inventoryNo: '', productNo: '', lotNo: '', unitNo: '', quantity: '', locatorNo: '', inputDate: '', baseLotNo: '' })
const turn = reactive({ inventoryType: 1 as InventoryType, inventoryNo: '', locatorNo: '', lotNoScan: '' })
const turnRowKeys = ref<string[]>([])
const selectedTicketNo = ref('')
const lookupTarget = ref<'main' | 'entry' | 'turn'>('main')
const lookupError = ref('')

const fgdInventories = computed(() => state.inventories.filter(item => item.inventoryType === 1))
const visibleRows = computed(() => state.inventoryRows
  .filter(row => row.inventoryNo.toUpperCase() === currentInventoryNo.value.toUpperCase() && row.quantity !== 0)
  .sort((a, b) => a.productNo.localeCompare(b.productNo) || a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo)))
const activeRow = computed(() => visibleRows.value.find(row => rowKey(row) === activeRowKey.value) ?? null)
const selectedRows = computed(() => state.inventoryRows.filter(row => selectedRowKeys.value.includes(rowKey(row))))
const activeComponents = computed(() => activeRow.value
  ? state.components.filter(item => item.inventoryNo === activeRow.value?.inventoryNo && item.lotNo === activeRow.value?.lotNo && item.locatorNo === activeRow.value?.locatorNo)
    .sort((a, b) => a.componentNo.localeCompare(b.componentNo))
  : [])
const activeBins = computed(() => activeRow.value ? state.bins.find(item => item.lotNo === activeRow.value?.lotNo) ?? null : null)
const binDefinitions = computed(() => [...state.binDefinitions].sort((a, b) => a.binOrder - b.binOrder))
const destinationInventories = computed(() => state.inventories.filter(item => item.inventoryType === turn.inventoryType))
const destinationLocators = computed(() => state.locators.filter(item => item.inventoryNo === turn.inventoryNo && item.issueState === 2))
const pendingTickets = computed(() => state.fgdInTickets
  .filter(ticket => ticket.state === 0 && ticket.toErp !== 1 && !state.transactions.some(transaction => transaction.ticketNo === ticket.fgdInNo && transaction.source === 30))
  .sort((a, b) => a.fgdInNo.localeCompare(b.fgdInNo)))
const selectedTicket = computed(() => pendingTickets.value.find(ticket => ticket.fgdInNo === selectedTicketNo.value) ?? null)
const turnRows = computed(() => visibleRows.value.filter(row => turnRowKeys.value.includes(rowKey(row))))
const entryLocators = computed(() => state.locators.filter(item => item.inventoryNo === entry.inventoryNo && item.issueState === 2))
const inventoryTypeOptions: InventoryType[] = [6, 2, 1]

function rowKey(row: InventoryRow): string { return [row.inventoryNo, row.lotNo, row.locatorNo].join('|') }
function persist(): void {
  if (dev.value) localStorage.setItem(storageKey, JSON.stringify(state))
}
function closePage(): void { emit('close') }
function clearMessages(): void { actionError.value = ''; actionMessage.value = '' }
function queryInventory(): void {
  clearMessages()
  const inventoryNo = inventoryInput.value.trim().toUpperCase()
  if (!inventoryNo) { actionError.value = '請輸入或選取成品庫編號。'; return }
  if (!fgdInventories.value.some(item => item.inventoryNo.toUpperCase() === inventoryNo)) {
    actionError.value = '找不到有效的成品庫編號。'
    return
  }
  currentInventoryNo.value = inventoryNo
  inventoryInput.value = inventoryNo
  activeRowKey.value = ''
  selectedRowKeys.value = []
}
function selectInventory(inventoryNo: string): void {
  inventoryInput.value = inventoryNo
  dialog.value = null
  queryInventory()
}
function openInventoryLookup(target: 'entry' | 'turn' | 'main' = 'main'): void {
  lookupTarget.value = target
  dialog.value = 'inventoryLookup'
}
function chooseInventory(item: InventoryBasis): void {
  if (dialog.value === 'inventoryLookup' && lookupTarget.value === 'main') {
    selectInventory(item.inventoryNo)
  } else if (lookupTarget.value === 'turn') {
    turn.inventoryNo = item.inventoryNo
    turn.locatorNo = ''
    dialog.value = 'turn'
  } else {
    entry.inventoryNo = item.inventoryNo
    entry.locatorNo = ''
    dialog.value = 'entry'
  }
}
function selectRow(row: InventoryRow, event?: MouseEvent): void {
  clearMessages()
  const key = rowKey(row)
  activeRowKey.value = key
  if (event?.ctrlKey || event?.metaKey) {
    selectedRowKeys.value = selectedRowKeys.value.includes(key)
      ? selectedRowKeys.value.filter(item => item !== key)
      : [...selectedRowKeys.value, key]
  }
}
function toggleRowSelection(row: InventoryRow): void {
  const key = rowKey(row)
  selectedRowKeys.value = selectedRowKeys.value.includes(key)
    ? selectedRowKeys.value.filter(item => item !== key)
    : [...selectedRowKeys.value, key]
  activeRowKey.value = key
}
function openAdd(): void {
  clearMessages()
  entryMode.value = 'add'
  Object.assign(entry, {
    inventoryNo: currentInventoryNo.value, productNo: '', lotNo: '', unitNo: '', quantity: '',
    locatorNo: '', inputDate: new Date().toISOString().slice(0, 16), baseLotNo: '',
  })
  dialog.value = 'entry'
}
function openEdit(): void {
  if (!activeRow.value) return
  entryMode.value = 'edit'
  Object.assign(entry, { ...activeRow.value, quantity: String(activeRow.value.quantity) })
  dialog.value = 'entry'
}
function applyLotBasis(lotNo: string): void {
  const lot = state.lots.find(item => item.lotNo.toUpperCase() === lotNo.trim().toUpperCase() && [11, 99].includes(item.status))
  if (!lot) {
    actionError.value = 'Lot No 不存在，或狀態不符合可入成品庫條件（11／99）。'
    return
  }
  entry.lotNo = lot.lotNo
  entry.productNo = lot.productNo
  entry.unitNo = lot.curUnitNo
  entry.baseLotNo = lot.baseLotNo
  actionError.value = ''
}
function saveEntry(): void {
  clearMessages()
  const inventory = fgdInventories.value.find(item => item.inventoryNo === entry.inventoryNo)
  const locator = entryLocators.value.find(item => item.locatorNo === entry.locatorNo)
  const lot = state.lots.find(item => item.lotNo === entry.lotNo && [11, 99].includes(item.status))
  const quantity = Number(entry.quantity)
  if (!inventory) { actionError.value = '請選取有效的成品庫。'; return }
  if (!locator) { actionError.value = '儲位必須屬於此成品庫且狀態為已核准。'; return }
  if (!lot) { actionError.value = 'Lot No 必須存在且狀態為 11 或 99。'; return }
  if (!Number.isFinite(quantity) || quantity <= 0) { actionError.value = '數量必須是大於 0 的數值。'; return }
  const existing = state.inventoryRows.find(row => row.inventoryNo === entry.inventoryNo && row.lotNo === lot.lotNo && row.locatorNo === entry.locatorNo)
  const value: InventoryRow = {
    inventoryNo: entry.inventoryNo, productNo: lot.productNo, moNo: existing?.moNo ?? '',
    lotNo: lot.lotNo, locatorNo: entry.locatorNo, quantity, unitNo: lot.curUnitNo,
    belongToType: existing?.belongToType ?? '', belongToNo: existing?.belongToNo ?? '',
    inputDate: entry.inputDate.replace('T', ' ') + (entry.inputDate.length === 16 ? ':00' : ''), baseLotNo: lot.baseLotNo,
  }
  if (existing) Object.assign(existing, value)
  else state.inventoryRows.push(value)
  currentInventoryNo.value = value.inventoryNo
  inventoryInput.value = value.inventoryNo
  activeRowKey.value = rowKey(value)
  selectedRowKeys.value = []
  persist()
  dialog.value = null
  actionMessage.value = existing ? '已依 InventoryNo／LotNo／LocatorNo 更新庫存列。' : '已新增成品庫存列。'
}
function openDelete(): void {
  clearMessages()
  const rows = selectedRows.value.length ? selectedRows.value : activeRow.value ? [activeRow.value] : []
  if (!rows.length) { actionError.value = '請先選取要刪除的庫存資料。'; return }
  const keys = new Set(rows.map(rowKey))
  state.inventoryRows.splice(0, state.inventoryRows.length, ...state.inventoryRows.filter(row => !keys.has(rowKey(row))))
  selectedRowKeys.value = []
  activeRowKey.value = ''
  persist()
  actionMessage.value = '已刪除 ' + rows.length + ' 筆成品庫存資料。'
}
function openTurn(): void {
  clearMessages()
  if (!activeRow.value) { actionError.value = '請先選取要轉庫的資料列。'; return }
  turn.inventoryType = 1
  turn.inventoryNo = ''
  turn.locatorNo = ''
  turn.lotNoScan = ''
  turnRowKeys.value = [rowKey(activeRow.value)]
  dialog.value = 'turn'
}
function toggleTurnRow(row: InventoryRow): void {
  const key = rowKey(row)
  turnRowKeys.value = turnRowKeys.value.includes(key) ? turnRowKeys.value.filter(item => item !== key) : [...turnRowKeys.value, key]
}
function scanTurnLot(): void {
  const lotNo = turn.lotNoScan.trim().toUpperCase()
  if (!lotNo) return
  const matched = visibleRows.value.filter(row => row.lotNo.toUpperCase() === lotNo)
  if (!matched.length) { actionError.value = '目前成品庫找不到此 Lot No。'; return }
  turnRowKeys.value = [...new Set([...turnRowKeys.value, ...matched.map(rowKey)])]
  activeRowKey.value = rowKey(matched[0])
  turn.lotNoScan = ''
  actionError.value = ''
}
function executeTurn(): void {
  clearMessages()
  if (!turnRows.value.length) { actionError.value = '請至少勾選一筆轉庫資料。'; return }
  if (!turn.inventoryNo) { actionError.value = '請選擇目的庫房。'; return }
  if (!turn.locatorNo) { actionError.value = '請選擇目的儲位。'; return }
  const locator = destinationLocators.value.find(item => item.locatorNo === turn.locatorNo)
  if (!locator) { actionError.value = '目的儲位必須屬於目的庫房且已核准。'; return }
  const totalQty = turnRows.value.reduce((sum, row) => sum + row.quantity, 0)
  const movedCount = turnRows.value.length
  const occupied = state.inventoryRows
    .filter(row => row.inventoryNo === turn.inventoryNo && row.locatorNo === turn.locatorNo && !turnRows.value.some(source => rowKey(source) === rowKey(row)))
    .reduce((sum, row) => sum + row.quantity, 0)
  if (locator.capacity !== 0 && occupied + totalQty > locator.capacity) {
    actionError.value = '目的儲位容量不足；此儲位容量為 ' + locator.capacity + '。'
    return
  }
  if (turn.inventoryType === 1) {
    const conflict = turnRows.value.find(source => state.inventoryRows.some(target =>
      target !== source && target.inventoryNo === turn.inventoryNo && target.lotNo === source.lotNo && target.locatorNo === turn.locatorNo))
    if (conflict) { actionError.value = '目的庫房已有相同 Lot／儲位資料，無法直接更新主鍵。'; return }
  }
  const now = new Date().toISOString().slice(0, 19).replace('T', ' ')
  for (const row of turnRows.value) {
    state.movements.unshift({
      lotNo: row.lotNo, fromInventoryNo: row.inventoryNo, fromLocatorNo: row.locatorNo,
      toInventoryNo: turn.inventoryNo, toLocatorNo: turn.locatorNo, inventoryType: turn.inventoryType,
      quantity: row.quantity, time: now,
    })
    if (turn.inventoryType === 1) {
      const fromInventoryNo = row.inventoryNo
      const fromLocatorNo = row.locatorNo
      row.inventoryNo = turn.inventoryNo
      row.locatorNo = turn.locatorNo
      row.inputDate = now
      for (const component of state.components.filter(item => item.inventoryNo === fromInventoryNo && item.lotNo === row.lotNo && item.locatorNo === fromLocatorNo))
        component.inventoryNo = turn.inventoryNo
    } else {
      state.inventoryRows.splice(state.inventoryRows.indexOf(row), 1)
    }
  }
  const returnInventoryNo = turn.inventoryType === 1 ? turn.inventoryNo : currentInventoryNo.value
  currentInventoryNo.value = returnInventoryNo
  inventoryInput.value = returnInventoryNo
  turnRowKeys.value = []
  activeRowKey.value = ''
  selectedRowKeys.value = []
  persist()
  dialog.value = null
  actionMessage.value = 'DEV 已完成 ' + movedCount + ' 筆轉庫模擬。目的類別：' + inventoryTypeNames[turn.inventoryType] + '。'
}
function openFgdIn(): void {
  clearMessages()
  selectedTicketNo.value = pendingTickets.value[0]?.fgdInNo ?? ''
  dialog.value = 'fgdIn'
}
function confirmFgdIn(): void {
  clearMessages()
  const ticket = selectedTicket.value
  if (!ticket) { actionError.value = '請先選取一張未確認且未過 ERP 的入庫單。'; return }
  if (state.transactions.some(item => item.ticketNo === ticket.fgdInNo && item.source === 30)) {
    actionError.value = '此入庫單已送出確認。'
    return
  }
  state.transactions.push({
    transactionId: 'DEV-INVTX-' + String(state.transactionSequence++).padStart(5, '0'),
    source: 30, ticketNo: ticket.fgdInNo, creator: props.currentUserNo || 'DEV-USER',
    createDate: new Date().toISOString().slice(0, 19).replace('T', ' '),
  })
  persist()
  selectedTicketNo.value = pendingTickets.value[0]?.fgdInNo ?? ''
  actionMessage.value = '已建立 Source=30 的成品入庫確認交易。'
  if (!pendingTickets.value.length) dialog.value = null
}
function openLocatorLookup(target: 'entry' | 'turn'): void {
  lookupTarget.value = target
  dialog.value = 'locatorLookup'
}
function chooseLocator(item: Locator): void {
  if (lookupTarget.value === 'turn') {
    turn.locatorNo = item.locatorNo
    dialog.value = 'turn'
  } else {
    entry.locatorNo = item.locatorNo
    dialog.value = 'entry'
  }
}
function openLotLookup(): void { lookupError.value = ''; dialog.value = 'lotLookup' }
function chooseLot(lot: LotBasis): void { applyLotBasis(lot.lotNo); dialog.value = 'entry' }
function closeDialog(): void { dialog.value = null; lookupError.value = '' }
</script>

<template>
  <section class="fgd-page">
    <header class="page-header">
      <div>
        <h1>成品庫帳務維護</h1>
        <p>FGD Inventory</p>
      </div>
      <span v-if="dev" class="dev-badge">DEV 假資料</span>
    </header>

    <div class="query-bar">
      <label for="fgd-inventory-no">成品庫編號</label>
      <input id="fgd-inventory-no" v-model="inventoryInput" type="text" @keydown.enter="queryInventory">
      <button class="lookup-button" type="button" title="選取成品庫" @click="openInventoryLookup('main')">…</button>
      <button class="primary-button" type="button" @click="queryInventory">查詢</button>
      <span v-if="currentInventoryNo" class="selected-inventory">{{ currentInventoryNo }}</span>
    </div>

    <div class="inventory-workspace">
      <div class="grid-stack">
        <section class="grid-panel main-grid-panel">
          <div class="grid-caption">
            <span>成品庫存</span>
            <span>{{ currentInventoryNo ? currentInventoryNo : '請先選取成品庫' }}</span>
          </div>
          <div class="table-scroll">
            <table class="data-grid">
              <thead>
                <tr>
                  <th class="select-column" aria-label="選取"></th>
                  <th>ProductNo</th><th>MONo</th><th>LotNo</th><th>LocatorNo</th>
                  <th class="number-column">Quantity</th><th>UnitNo</th><th>BelongToType</th><th>BelongToNo</th><th>Input Date</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ active: activeRowKey === rowKey(row), checked: selectedRowKeys.includes(rowKey(row)) }" @click="selectRow(row, $event)">
                  <td class="select-column" @click.stop><input :checked="selectedRowKeys.includes(rowKey(row))" type="checkbox" aria-label="選取刪除資料" @change="toggleRowSelection(row)"></td>
                  <td>{{ row.productNo }}</td><td>{{ row.moNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td>
                  <td class="number-column">{{ row.quantity.toLocaleString() }}</td><td>{{ row.unitNo }}</td>
                  <td>{{ row.belongToType }}</td><td>{{ row.belongToNo }}</td><td>{{ row.inputDate }}</td>
                </tr>
                <tr v-if="!visibleRows.length">
                  <td colspan="10" class="empty-row">{{ currentInventoryNo ? '沒有符合的庫存資料' : '選取成品庫並查詢後顯示庫存資料' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <div class="splitter" aria-hidden="true"></div>

        <section class="grid-panel detail-grid-panel">
          <div class="grid-caption">
            <span>{{ activeComponents.length ? 'Component 明細' : 'Bin 明細' }}</span>
            <span v-if="activeRow">{{ activeRow.lotNo }} / {{ activeRow.locatorNo }}</span>
          </div>
          <div v-if="activeComponents.length" class="table-scroll">
            <table class="data-grid compact-grid">
              <thead><tr><th>ComponentNo</th><th class="number-column">Good Qty</th><th class="number-column">Scrap Qty</th><th>UnitNo</th></tr></thead>
              <tbody><tr v-for="item in activeComponents" :key="item.componentNo"><td>{{ item.componentNo }}</td><td class="number-column">{{ item.goodQty }}</td><td class="number-column">{{ item.scrapQty }}</td><td>{{ item.unitNo }}</td></tr></tbody>
            </table>
          </div>
          <div v-else-if="activeBins" class="table-scroll">
            <table class="data-grid compact-grid">
              <thead><tr><th v-for="bin in binDefinitions" :key="bin.binNo" class="number-column">{{ bin.binNo }}</th></tr></thead>
              <tbody><tr><td v-for="bin in binDefinitions" :key="bin.binNo" class="number-column">{{ activeBins.quantities[bin.binNo] ?? '' }}</td></tr></tbody>
            </table>
          </div>
          <div v-else class="empty-detail">{{ activeRow ? '此 Lot 沒有元件或 Bin 明細' : '選取上方資料列以顯示元件或 Bin 明細' }}</div>
        </section>
      </div>

      <aside class="action-bar">
        <button type="button" @click="openFgdIn">FGD In</button>
        <button type="button" :disabled="!activeRow" @click="openTurn">Turn INV</button>
        <button class="primary-button" type="button" @click="openAdd">Add</button>
        <button type="button" :disabled="!activeRow && !selectedRows.length" @click="openDelete">Del</button>
        <span class="action-spacer"></span>
        <button type="button" @click="closePage">Close</button>
      </aside>
    </div>

    <p v-if="actionError" class="message error-message" role="alert">{{ actionError }}</p>
    <p v-else-if="actionMessage" class="message success-message" role="status">{{ actionMessage }}</p>

    <div v-if="dialog" class="modal-backdrop" @click.self="closeDialog">
      <section v-if="dialog === 'inventoryLookup'" class="modal lookup-modal" role="dialog" aria-modal="true" aria-labelledby="inventory-lookup-title">
        <header class="modal-header"><h2 id="inventory-lookup-title">選取庫房</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="table-scroll modal-table">
          <table class="data-grid">
            <thead><tr><th>InventoryNo</th><th>InventoryName</th><th>InventoryType</th><th></th></tr></thead>
            <tbody><tr v-for="item in (lookupTarget === 'turn' ? destinationInventories : fgdInventories)" :key="item.inventoryNo">
              <td>{{ item.inventoryNo }}</td><td>{{ item.inventoryName }}</td><td>{{ inventoryTypeNames[item.inventoryType] }}</td>
              <td><button type="button" class="small-button" @click="chooseInventory(item)">選取</button></td>
            </tr></tbody>
          </table>
        </div>
        <footer class="modal-footer"><button type="button" @click="closeDialog">關閉</button></footer>
      </section>

      <section v-else-if="dialog === 'locatorLookup'" class="modal lookup-modal" role="dialog" aria-modal="true" aria-labelledby="locator-lookup-title">
        <header class="modal-header"><h2 id="locator-lookup-title">選取已核准儲位</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="table-scroll modal-table">
          <table class="data-grid">
            <thead><tr><th>InventoryNo</th><th>LocatorNo</th><th class="number-column">Capacity</th><th></th></tr></thead>
            <tbody><tr v-for="item in (lookupTarget === 'turn' ? destinationLocators : entryLocators)" :key="item.inventoryNo + item.locatorNo">
              <td>{{ item.inventoryNo }}</td><td>{{ item.locatorNo }}</td><td class="number-column">{{ item.capacity || '不限' }}</td>
              <td><button type="button" class="small-button" @click="chooseLocator(item)">選取</button></td>
            </tr></tbody>
          </table>
        </div>
        <footer class="modal-footer"><button type="button" @click="closeDialog">關閉</button></footer>
      </section>

      <section v-else-if="dialog === 'lotLookup'" class="modal lookup-modal" role="dialog" aria-modal="true" aria-labelledby="lot-lookup-title">
        <header class="modal-header"><h2 id="lot-lookup-title">選取可入成品庫批號</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="table-scroll modal-table">
          <table class="data-grid">
            <thead><tr><th>LotNo</th><th>ProductNo</th><th>UnitNo</th><th>BaseLotNo</th><th>Status</th><th></th></tr></thead>
            <tbody><tr v-for="lot in state.lots.filter(item => [11, 99].includes(item.status))" :key="lot.lotNo">
              <td>{{ lot.lotNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.curUnitNo }}</td><td>{{ lot.baseLotNo }}</td><td>{{ lot.status }}</td>
              <td><button type="button" class="small-button" @click="chooseLot(lot)">選取</button></td>
            </tr></tbody>
          </table>
        </div>
        <footer class="modal-footer"><button type="button" @click="closeDialog">關閉</button></footer>
      </section>

      <section v-else-if="dialog === 'entry'" class="modal entry-modal" role="dialog" aria-modal="true" aria-labelledby="entry-title">
        <header class="modal-header"><h2 id="entry-title">{{ entryMode === 'add' ? 'FGD Inventory Define' : 'FGD Inventory Edit' }}</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="entry-form">
          <label>InventoryNo</label><div class="input-with-button"><input v-model="entry.inventoryNo" :readonly="entryMode === 'edit'"><button type="button" :disabled="entryMode === 'edit'" @click="openInventoryLookup('entry')">…</button></div>
          <label>ProductNo</label><input v-model="entry.productNo" readonly>
          <label>LotNo</label><div class="input-with-button"><input v-model="entry.lotNo" @change="applyLotBasis(entry.lotNo)"><button type="button" @click="openLotLookup">…</button></div>
          <label>UnitNo</label><input v-model="entry.unitNo" readonly>
          <label>Quantity</label><input v-model="entry.quantity" type="number" min="0" step="0.000001">
          <label>LocatorNo</label><div class="input-with-button"><input v-model="entry.locatorNo" :readonly="entryMode === 'edit'"><button type="button" :disabled="entryMode === 'edit'" @click="openLocatorLookup('entry')">…</button></div>
          <label>InputDate</label><input v-model="entry.inputDate" type="datetime-local">
          <label>BaseLotNo</label><input v-model="entry.baseLotNo" readonly>
        </div>
        <p v-if="actionError" class="message error-message">{{ actionError }}</p>
        <footer class="modal-footer"><button class="primary-button" type="button" @click="saveEntry">Confirm</button><button type="button" @click="closeDialog">Close</button></footer>
      </section>

      <section v-else-if="dialog === 'turn'" class="modal turn-modal" role="dialog" aria-modal="true" aria-labelledby="turn-title">
        <header class="modal-header"><h2 id="turn-title">FGD Turn Inventory</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="turn-destination">
          <div class="inventory-types">
            <label v-for="type in inventoryTypeOptions" :key="type"><input v-model="turn.inventoryType" type="radio" :value="type" @change="turn.inventoryNo = ''; turn.locatorNo = ''">{{ inventoryTypeNames[type] }}</label>
          </div>
          <label>目的庫房</label><div class="input-with-button"><input v-model="turn.inventoryNo" readonly><button type="button" @click="openInventoryLookup('turn')">…</button></div>
          <label>目的儲位</label><div class="input-with-button"><input v-model="turn.locatorNo" readonly><button type="button" :disabled="!turn.inventoryNo" @click="openLocatorLookup('turn')">…</button></div>
          <label>LotNo 掃描</label><input v-model="turn.lotNoScan" @keydown.enter.prevent="scanTurnLot">
          <button type="button" class="small-button scan-button" @click="scanTurnLot">加入</button>
        </div>
        <div class="table-scroll turn-table">
          <table class="data-grid">
            <thead><tr><th>選取</th><th>InventoryNo</th><th>ProductNo</th><th>LotNo</th><th>LocatorNo</th><th class="number-column">Quantity</th><th>UnitNo</th></tr></thead>
            <tbody><tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ active: turnRowKeys.includes(rowKey(row)) }">
              <td><input type="checkbox" :checked="turnRowKeys.includes(rowKey(row))" @change="toggleTurnRow(row)"></td>
              <td>{{ row.inventoryNo }}</td><td>{{ row.productNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td class="number-column">{{ row.quantity }}</td><td>{{ row.unitNo }}</td>
            </tr></tbody>
          </table>
        </div>
        <p v-if="actionError" class="message error-message">{{ actionError }}</p>
        <footer class="modal-footer"><button class="primary-button" type="button" @click="executeTurn">Confirm</button><button type="button" @click="closeDialog">Close</button></footer>
      </section>

      <section v-else-if="dialog === 'fgdIn'" class="modal fgd-in-modal" role="dialog" aria-modal="true" aria-labelledby="fgdin-title">
        <header class="modal-header"><h2 id="fgdin-title">FGD In Confirm</h2><button class="icon-close" type="button" @click="closeDialog">×</button></header>
        <div class="fgd-in-content">
          <div class="ticket-list table-scroll">
            <table class="data-grid">
              <thead><tr><th>FGDInNo</th><th>InventoryNo</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
              <tbody><tr v-for="ticket in pendingTickets" :key="ticket.fgdInNo" :class="{ active: selectedTicketNo === ticket.fgdInNo }" @click="selectedTicketNo = ticket.fgdInNo">
                <td>{{ ticket.fgdInNo }}</td><td>{{ ticket.inventoryNo }}</td><td>{{ ticket.description }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td>
              </tr></tbody>
            </table>
            <div v-if="!pendingTickets.length" class="empty-detail">目前沒有可確認的 FGD In 單據</div>
          </div>
          <template v-if="selectedTicket">
            <div class="ticket-summary">
              <label>FGDInNo <input :value="selectedTicket.fgdInNo" readonly></label>
              <label>InventoryNo <input :value="selectedTicket.inventoryNo" readonly></label>
              <label>Description <input :value="selectedTicket.description" readonly></label>
              <label>CreateDate <input :value="selectedTicket.createDate" readonly></label>
            </div>
            <div class="table-scroll ticket-detail-grid">
              <table class="data-grid">
                <thead><tr><th>ProductNo</th><th>ProductVersion</th><th>LotNo</th><th class="number-column">Quantity</th><th>UnitNo</th><th>LocatorNo</th><th>BaseLotNo</th></tr></thead>
                <tbody><tr v-for="detail in selectedTicket.details" :key="detail.lotNo + detail.locatorNo">
                  <td>{{ detail.productNo }}</td><td>{{ detail.productVersion }}</td><td>{{ detail.lotNo }}</td><td class="number-column">{{ detail.quantity }}</td><td>{{ detail.unitNo }}</td><td>{{ detail.locatorNo }}</td><td>{{ detail.baseLotNo }}</td>
                </tr></tbody>
              </table>
            </div>
            <div class="table-scroll ticket-component-grid">
              <table class="data-grid compact-grid">
                <thead><tr><th>LotNo</th><th>ComponentNo</th><th class="number-column">GoodQty</th><th class="number-column">ScrapQty</th><th>UnitNo</th></tr></thead>
                <tbody><tr v-for="item in selectedTicket.components" :key="item.lotNo + item.componentNo">
                  <td>{{ item.lotNo }}</td><td>{{ item.componentNo }}</td><td class="number-column">{{ item.goodQty }}</td><td class="number-column">{{ item.scrapQty }}</td><td>{{ item.unitNo }}</td>
                </tr><tr v-if="!selectedTicket.components.length"><td colspan="5" class="empty-row">此入庫單沒有 Component 明細</td></tr></tbody>
              </table>
            </div>
          </template>
        </div>
        <p v-if="actionError" class="message error-message">{{ actionError }}</p>
        <footer class="modal-footer"><button class="primary-button" type="button" :disabled="!selectedTicket" @click="confirmFgdIn">Confirm</button><button type="button" @click="closeDialog">Close</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.fgd-page{height:100%;min-height:560px;display:flex;flex-direction:column;gap:12px;color:#282a2d;background:#fff;font-size:14px}
.page-header{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid #d8dce0;padding:0 2px 10px}
.page-header h1{font-size:22px;font-weight:600;margin:0;color:#34383d}.page-header p{font-size:12px;color:#747a80;margin:3px 0 0}
.dev-badge{border:1px solid #dfb951;background:#fff3c2;color:#765500;border-radius:3px;padding:4px 9px;font-size:12px}
.query-bar{display:flex;align-items:center;gap:7px;min-height:40px;padding:5px 8px;border:1px solid #d6d9dc;background:#f7f8f9}
.query-bar label{font-weight:500;margin-right:3px}.query-bar input,.entry-form input,.input-with-button input,.ticket-summary input,.turn-destination>input{height:29px;border:1px solid #9fa4aa;background:#fff0b3;padding:4px 7px;color:#202327;min-width:0}
.query-bar input{width:180px}.query-bar input:focus,.entry-form input:focus,.input-with-button input:focus{outline:2px solid #d4aa41;outline-offset:0}
button{font:inherit;cursor:pointer;border:1px solid #adb2b7;border-radius:3px;background:#f7f7f7;color:#292d31;padding:6px 13px;min-height:30px}
button:hover:not(:disabled){background:#eceff1;border-color:#878e94}button:disabled{opacity:.48;cursor:not-allowed}
.primary-button{background:#f7a62b;border-color:#e39620;color:white;font-weight:600}.primary-button:hover:not(:disabled){background:#e8921c}
.lookup-button{width:30px;padding:3px;font-size:18px;line-height:20px}.selected-inventory{margin-left:8px;color:#61676c}
.inventory-workspace{display:flex;align-items:stretch;gap:9px;flex:1;min-height:0}
.grid-stack{display:flex;flex-direction:column;gap:5px;flex:1;min-width:0;min-height:0;border:1px solid #c5c9cd;padding:4px}
.grid-panel{display:flex;flex-direction:column;min-height:0}.main-grid-panel{height:58%;min-height:190px}.detail-grid-panel{flex:1;min-height:115px}
.grid-caption{display:flex;justify-content:space-between;align-items:center;height:25px;padding:3px 7px;border-bottom:1px solid #c7cbd0;color:#4d5358;font-size:12px;background:#f6f7f8}
.table-scroll{overflow:auto;min-height:0;flex:1}
.data-grid{width:100%;border-collapse:collapse;white-space:nowrap;font-size:13px}
.data-grid th{position:sticky;top:0;z-index:1;background:#65a9e4;color:#fff;font-weight:500;text-align:left;border:1px solid #97a9b7;padding:5px 7px}
.data-grid td{border:1px solid #c9cdd1;padding:4px 7px;background:#fff0b3;height:25px}
.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover td{background:#ffed9e}.data-grid tbody tr.active td{background:#f8df7d}.data-grid tbody tr.checked td{background:#ffe99b}
.data-grid .select-column{width:31px;text-align:center;padding:2px}.select-column input,.turn-table input{accent-color:#d89a21}
.number-column{text-align:right;font-variant-numeric:tabular-nums}
.empty-row,.empty-detail{color:#757b81;text-align:center!important;background:#fafafa!important;padding:12px!important}
.splitter{height:6px;flex:0 0 6px;background:#e4e6e8;border-top:1px solid #c8ccd0;border-bottom:1px solid #c8ccd0;cursor:row-resize}
.compact-grid{max-width:100%}
.action-bar{display:flex;flex-direction:column;gap:6px;width:95px;flex:0 0 95px}
.action-bar button{min-height:32px;text-align:center;padding:5px 4px}
.action-spacer{flex:1}
.message{margin:0;padding:8px 11px;border:1px solid;border-radius:3px;font-size:13px}.error-message{color:#8b2424;background:#fff0ee;border-color:#dfb5af}.success-message{color:#365d33;background:#eff8ec;border-color:#b8d2b3}
.modal-backdrop{position:fixed;inset:0;z-index:1000;background:rgba(22,26,30,.42);display:flex;align-items:center;justify-content:center;padding:20px}
.modal{display:flex;flex-direction:column;background:#fff;border:1px solid #b7bcc1;box-shadow:0 8px 28px rgba(0,0,0,.25);max-height:94vh;min-width:360px}
.modal-header{height:46px;flex:0 0 46px;display:flex;align-items:center;justify-content:space-between;padding:0 13px;border-bottom:1px solid #d1d4d7;background:#f7f7f7}
.modal-header h2{font-size:17px;font-weight:600;margin:0}.icon-close{border:0;background:transparent;font-size:24px;padding:2px 8px;min-height:0}
.modal-footer{display:flex;align-items:center;justify-content:flex-end;gap:8px;padding:10px 13px;border-top:1px solid #d1d4d7}
.lookup-modal{width:min(760px,94vw);height:min(490px,88vh)}.modal-table{padding:10px 12px}
.small-button{min-height:25px;padding:3px 8px;font-size:12px}
.entry-modal{width:min(610px,94vw)}.entry-form{display:grid;grid-template-columns:125px minmax(0,1fr);gap:10px 9px;padding:17px 20px}
.entry-form>label{align-self:center;text-align:right}.entry-form>input{width:100%;height:30px}
.input-with-button{display:flex;gap:5px;min-width:0}.input-with-button input{flex:1;width:1px}.input-with-button button{width:34px;padding:2px;font-size:17px}
.turn-modal{width:min(930px,96vw);height:min(650px,92vh)}
.turn-destination{display:grid;grid-template-columns:105px minmax(180px,1fr) 100px minmax(180px,1fr);gap:8px;align-items:center;padding:12px 14px;border-bottom:1px solid #d7dadd}
.turn-destination>label{text-align:right}.turn-destination input:not([type=radio]){width:100%;height:29px;border:1px solid #9fa4aa;background:#fff0b3;padding:4px 7px}
.inventory-types{grid-column:1/-1;display:flex;gap:20px;padding:1px 0 5px}.inventory-types label{display:flex;align-items:center;gap:5px}.inventory-types input{accent-color:#d69620}
.scan-button{grid-column:4;justify-self:start}.turn-table{margin:10px 12px}
.fgd-in-modal{width:min(1080px,97vw);height:min(730px,94vh)}.fgd-in-content{display:flex;flex-direction:column;gap:9px;padding:10px 12px;min-height:0;flex:1}
.ticket-list{height:31%;flex:0 0 31%;border:1px solid #d0d4d8}.ticket-summary{display:grid;grid-template-columns:1fr 1fr;gap:7px 12px}
.ticket-summary label{display:grid;grid-template-columns:95px minmax(0,1fr);align-items:center;gap:5px}.ticket-summary input{background:#f3f4f4;width:100%}
.ticket-detail-grid{height:28%;flex:0 0 28%;border:1px solid #d0d4d8}.ticket-component-grid{height:20%;flex:0 0 20%;border:1px solid #d0d4d8}
@media(max-width:850px){.inventory-workspace{flex-direction:column}.action-bar{width:auto;flex:0 0 auto;flex-direction:row;flex-wrap:wrap}.action-spacer{display:none}.main-grid-panel{height:320px}.detail-grid-panel{min-height:180px}.turn-destination{grid-template-columns:90px 1fr}.turn-destination>label{text-align:left}.scan-button{grid-column:2}}
</style>

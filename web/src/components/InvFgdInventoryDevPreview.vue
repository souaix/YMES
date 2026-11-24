<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()

interface StockRow {
  inventoryNo: string
  productNo: string
  locatorNo: string
  lotNo: string
  locQty: number
  unitNo: string
  belongToType: number
  belongToNo: string
  inputDate: string
  fgdInNo: string
}
interface ComponentRow { inventoryNo: string; lotNo: string; locatorNo: string; componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface BinState { lotNo: string; values: Record<string, number> }
interface TicketComponent { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface TicketDetail {
  lotNo: string
  productNo: string
  productVersion: string
  baseLotNo: string
  qty: number
  unitNo: string
  locatorNo: string
  components: TicketComponent[]
}
interface FgdInTicket {
  fgdInNo: string
  inventoryNo: string
  fromInventoryNo: string
  source: 0 | 1
  state: 0 | 1 | 2 | 3
  toERP: number
  description: string
  creator: string
  createDate: string
  inputDate: string
  details: TicketDetail[]
}
interface SourceStock { inventoryNo: string; lotNo: string; qty: number }
interface Activity { fgdInNo: string; inventoryNo: string; rowCount: number; totalQty: number; happenedAt: string; result: string }
interface DevState {
  stock: StockRow[]
  components: ComponentRow[]
  bins: BinState[]
  tickets: FgdInTicket[]
  sourceStock: SourceStock[]
  activities: Activity[]
}

const storageKey = 'ymes.dev.iv501-fgd-inventory.v1'
const bins = [{ binNo: 'BIN1', binOrder: 1 }, { binNo: 'BIN2', binOrder: 2 }, { binNo: 'BIN3', binOrder: 3 }, { binNo: 'BIN5', binOrder: 5 }]
const inventoryBasis = [
  { inventoryNo: 'FGD-DEV-01', inventoryName: '成品庫 A' },
  { inventoryNo: 'FGD-DEV-02', inventoryName: '成品庫 B' },
]
const ticketDetailsA: TicketDetail[] = [
  { lotNo: 'LOT-FGD-001', productNo: 'PRD-CONTROL-10', productVersion: 'A2', baseLotNo: 'LOT-FGD-001', qty: 4, unitNo: 'EA', locatorNo: 'FGD-A-01', components: [{ componentNo: 'CMP-CONTROL-01', goodQty: 3, scrapQty: 0, unitNo: 'EA' }, { componentNo: 'CMP-CONTROL-02', goodQty: 1, scrapQty: 0, unitNo: 'EA' }] },
  { lotNo: 'LOT-FGD-005', productNo: 'PRD-PANEL-07', productVersion: 'B1', baseLotNo: 'LOT-FGD-005', qty: 6, unitNo: 'EA', locatorNo: 'FGD-A-02', components: [] },
]
const ticketDetailsB: TicketDetail[] = [
  { lotNo: 'LOT-SEMI-002', productNo: 'PRD-SENSOR-04', productVersion: 'C1', baseLotNo: 'LOT-SEMI-002', qty: 3, unitNo: 'EA', locatorNo: 'FGD-A-03', components: [{ componentNo: 'CMP-SENSOR-01', goodQty: 2, scrapQty: 0, unitNo: 'EA' }] },
]

function initialState(): DevState {
  return {
    stock: [
      { inventoryNo: 'FGD-DEV-01', productNo: 'PRD-CONTROL-10', locatorNo: 'FGD-A-01', lotNo: 'LOT-FGD-001', locQty: 36, unitNo: 'EA', belongToType: 0, belongToNo: 'MO-DEV-2601', inputDate: '2026-11-20 15:20:00', fgdInNo: 'FGD-DEV-HIST-001' },
      { inventoryNo: 'FGD-DEV-01', productNo: 'PRD-PANEL-07', locatorNo: 'FGD-A-02', lotNo: 'LOT-FGD-002', locQty: 12, unitNo: 'EA', belongToType: 0, belongToNo: 'MO-DEV-2602', inputDate: '2026-11-22 09:10:00', fgdInNo: 'FGD-DEV-HIST-002' },
      { inventoryNo: 'FGD-DEV-02', productNo: 'PRD-SENSOR-04', locatorNo: 'FGD-B-01', lotNo: 'LOT-FGD-003', locQty: 20, unitNo: 'EA', belongToType: 0, belongToNo: 'MO-DEV-2603', inputDate: '2026-11-23 10:30:00', fgdInNo: 'FGD-DEV-HIST-003' },
    ],
    components: [
      { inventoryNo: 'FGD-DEV-01', lotNo: 'LOT-FGD-001', locatorNo: 'FGD-A-01', componentNo: 'CMP-CONTROL-01', goodQty: 35, scrapQty: 1, unitNo: 'EA' },
      { inventoryNo: 'FGD-DEV-01', lotNo: 'LOT-FGD-001', locatorNo: 'FGD-A-01', componentNo: 'CMP-CONTROL-02', goodQty: 35, scrapQty: 0, unitNo: 'EA' },
    ],
    bins: [{ lotNo: 'LOT-FGD-002', values: { BIN1: 8, BIN2: 3, BIN3: 1, BIN5: 0 } }, { lotNo: 'LOT-FGD-003', values: { BIN1: 16, BIN2: 4, BIN3: 0, BIN5: 0 } }],
    tickets: [
      { fgdInNo: 'FI-DEV-260001', inventoryNo: 'FGD-DEV-01', fromInventoryNo: '', source: 0, state: 0, toERP: 0, description: 'DEV 外購成品入庫', creator: 'DEV001', createDate: '2026-11-24 09:12:00', inputDate: '', details: ticketDetailsA },
      { fgdInNo: 'FI-DEV-260002', inventoryNo: 'FGD-DEV-02', fromInventoryNo: 'SEMI-DEV-01', source: 1, state: 0, toERP: 0, description: 'DEV 半成品轉成品入庫', creator: 'DEV002', createDate: '2026-11-24 10:35:00', inputDate: '', details: ticketDetailsB },
      { fgdInNo: 'FI-DEV-ERP-003', inventoryNo: 'FGD-DEV-01', fromInventoryNo: '', source: 0, state: 0, toERP: 1, description: 'DEV 已拋轉 ERP，不列入確認清單', creator: 'DEV001', createDate: '2026-11-24 08:00:00', inputDate: '', details: ticketDetailsA },
      { fgdInNo: 'FI-DEV-HANDLING-004', inventoryNo: 'FGD-DEV-01', fromInventoryNo: '', source: 0, state: 1, toERP: 0, description: 'DEV 處理中，不列入待確認清單', creator: 'DEV003', createDate: '2026-11-24 08:30:00', inputDate: '', details: ticketDetailsA },
    ],
    sourceStock: [{ inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-002', qty: 10 }],
    activities: [],
  }
}
function readState(): DevState {
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as DevState
      if (Array.isArray(parsed.stock) && Array.isArray(parsed.tickets)) {
        return {
          stock: parsed.stock,
          components: Array.isArray(parsed.components) ? parsed.components : [],
          bins: Array.isArray(parsed.bins) ? parsed.bins : [],
          tickets: parsed.tickets,
          sourceStock: Array.isArray(parsed.sourceStock) ? parsed.sourceStock : [],
          activities: Array.isArray(parsed.activities) ? parsed.activities : [],
        }
      }
    }
  } catch { /* Use fixture data if browser storage is unavailable. */ }
  return initialState()
}

const records = ref<DevState>(readState())
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventoryNo = ref('FGD-DEV-01')
const hasQueried = ref(false)
const activeStockKey = ref('')
const inventoryPickerOpen = ref(false)
const confirmationOpen = ref(false)
const activeTicketNo = ref('')
const notice = ref('DEV 假資料預覽；選擇庫房後按 Query 載入成品庫存。')
const visibleStock = computed(() => {
  if (!hasQueried.value) return []
  const selectedInventory = inventoryNo.value.trim().toUpperCase()
  return records.value.stock
    .filter((row) => row.locQty > 0 && (!selectedInventory || row.inventoryNo.toUpperCase() === selectedInventory))
    .sort((a, b) => a.productNo.localeCompare(b.productNo) || a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo))
})
const activeStock = computed(() => visibleStock.value.find((row) => stockKey(row) === activeStockKey.value) ?? null)
const activeComponents = computed(() => activeStock.value
  ? records.value.components.filter((row) => row.inventoryNo === activeStock.value?.inventoryNo && row.lotNo === activeStock.value?.lotNo && row.locatorNo === activeStock.value?.locatorNo)
  : [])
const activeBins = computed(() => activeStock.value ? records.value.bins.find((row) => row.lotNo === activeStock.value?.lotNo) ?? null : null)
const visibleTickets = computed(() => records.value.tickets
  .filter((ticket) => ticket.state === 0 && ticket.toERP !== 1)
  .slice()
  .sort((a, b) => a.fgdInNo.localeCompare(b.fgdInNo)))
const activeTicket = computed(() => visibleTickets.value.find((ticket) => ticket.fgdInNo === activeTicketNo.value) ?? null)
const activeTicketQty = computed(() => activeTicket.value?.details.reduce((sum, row) => sum + row.qty, 0) ?? 0)
const activeTicketComponentCount = computed(() => activeTicket.value?.details.reduce((sum, row) => sum + row.components.length, 0) ?? 0)
const orderedBins = computed(() => bins.slice().sort((a, b) => a.binOrder - b.binOrder))
const stateLabels: Record<number, string> = { 0: 'UnConfirm', 1: 'Handling', 2: 'Confirm', 3: 'Failed' }
const formatQty = (value: number) => new Intl.NumberFormat('en-US', { maximumFractionDigits: 4 }).format(value)
const stateLabel = (state: number) => stateLabels[state] ?? 'Unknown'
const ticketSourceLabel = (source: number) => source === 1 ? 'WIP / SEMI 來源' : '手動開立'
function stockKey(row: Pick<StockRow, 'inventoryNo' | 'lotNo' | 'locatorNo'>): string { return `${row.inventoryNo}|${row.lotNo}|${row.locatorNo}` }
function persist(message: string): void {
  try { localStorage.setItem(storageKey, JSON.stringify(records.value)) } catch { /* Keep this preview available in memory. */ }
  notice.value = message
}
function queryStock(): void {
  const value = inventoryNo.value.trim().toUpperCase()
  if (value && !inventoryBasis.some((row) => row.inventoryNo === value)) {
    hasQueried.value = true
    activeStockKey.value = ''
    notice.value = `找不到 InventoryType=1 的成品庫：${value}`
    return
  }
  inventoryNo.value = value
  hasQueried.value = true
  activeStockKey.value = visibleStock.value[0] ? stockKey(visibleStock.value[0]) : ''
  notice.value = `查詢完成：${visibleStock.value.length} 筆庫存列（僅顯示 LocQty > 0）。DEV 假資料未呼叫 API。`
}
function chooseInventory(value: string): void {
  inventoryNo.value = value
  inventoryPickerOpen.value = false
  queryStock()
}
function selectStock(row: StockRow): void { activeStockKey.value = stockKey(row) }
function openConfirmation(): void {
  activeTicketNo.value = ''
  confirmationOpen.value = true
}
function closeConfirmation(): void {
  confirmationOpen.value = false
  activeTicketNo.value = ''
}
function canPost(ticket: FgdInTicket): boolean {
  if (!ticket.details.length) return false
  if (ticket.source !== 1) return true
  const needed = new Map<string, number>()
  for (const detail of ticket.details) needed.set(detail.lotNo, (needed.get(detail.lotNo) ?? 0) + detail.qty)
  return [...needed.entries()].every(([lotNo, qty]) => {
    const source = records.value.sourceStock.find((row) => row.inventoryNo === ticket.fromInventoryNo && row.lotNo === lotNo)
    return (source?.qty ?? 0) >= qty
  })
}
function addPostedStock(ticket: FgdInTicket, detail: TicketDetail, happenedAt: string): void {
  const existing = records.value.stock.find((row) => row.inventoryNo === ticket.inventoryNo && row.lotNo === detail.lotNo && row.locatorNo === detail.locatorNo)
  if (existing) {
    existing.locQty += detail.qty
    existing.inputDate = happenedAt
    existing.fgdInNo = ticket.fgdInNo
  } else {
    records.value.stock.push({
      inventoryNo: ticket.inventoryNo, productNo: detail.productNo, locatorNo: detail.locatorNo,
      lotNo: detail.lotNo, locQty: detail.qty, unitNo: detail.unitNo,
      belongToType: 0, belongToNo: 'MO-DEV-2601', inputDate: happenedAt, fgdInNo: ticket.fgdInNo,
    })
  }
  for (const component of detail.components) {
    const existingComponent = records.value.components.find((row) => row.inventoryNo === ticket.inventoryNo && row.lotNo === detail.lotNo && row.locatorNo === detail.locatorNo && row.componentNo === component.componentNo)
    if (existingComponent) {
      existingComponent.goodQty += component.goodQty
      existingComponent.scrapQty += component.scrapQty
    } else {
      records.value.components.push({ inventoryNo: ticket.inventoryNo, lotNo: detail.lotNo, locatorNo: detail.locatorNo, ...component })
    }
  }
}
function confirmTicket(): void {
  const ticket = activeTicket.value
  if (!ticket) return
  if (!canPost(ticket)) {
    ticket.state = 3
    persist(`模擬過帳失敗：${ticket.fgdInNo} 的來源量不足或沒有明細，單據改為 Failed。`)
    closeConfirmation()
    return
  }
  if (!window.confirm(`確認成品入庫 ${ticket.fgdInNo}？此 DEV 動作只更新本機假資料。`)) return

  const happenedAt = new Date().toLocaleString('sv-SE', { hour12: false }).replace('T', ' ')
  const totalQty = ticket.details.reduce((sum, row) => sum + row.qty, 0)
  ticket.state = 1
  if (ticket.source === 1) {
    for (const detail of ticket.details) {
      const source = records.value.sourceStock.find((row) => row.inventoryNo === ticket.fromInventoryNo && row.lotNo === detail.lotNo)
      if (source) source.qty -= detail.qty
    }
  }
  for (const detail of ticket.details) addPostedStock(ticket, detail, happenedAt)
  ticket.state = 2
  ticket.inputDate = happenedAt
  records.value.activities.unshift({
    fgdInNo: ticket.fgdInNo, inventoryNo: ticket.inventoryNo, rowCount: ticket.details.length,
    totalQty, happenedAt, result: 'DEV simulated success',
  })
  if (hasQueried.value) {
    activeStockKey.value = visibleStock.value[0] ? activeStockKey.value : ''
  }
  persist(`DEV 模擬確認完成：${ticket.fgdInNo}，${ticket.details.length} 筆 Lot 明細，${formatQty(totalQty)} ${ticket.details[0]?.unitNo ?? ''}。正式入庫過帳未呼叫。`)
  closeConfirmation()
}
function resetDevData(): void {
  records.value = initialState()
  inventoryNo.value = 'FGD-DEV-01'
  hasQueried.value = false
  activeStockKey.value = ''
  confirmationOpen.value = false
  activeTicketNo.value = ''
  try { localStorage.removeItem(storageKey) } catch { /* The in-memory fixture was reset. */ }
  notice.value = 'DEV 假資料已重設；沒有呼叫 API 或資料庫。'
}
</script>

<template>
  <section class="iv501" aria-label="成品庫庫房資料 DEV 預覽">
    <header class="iv501__toolbar">
      <div class="iv501__lookup">
        <label for="iv501-inventory">Inventory No</label>
        <input id="iv501-inventory" v-model="inventoryNo" list="iv501-inventory-list" @keydown.enter.prevent="queryStock" />
        <datalist id="iv501-inventory-list"><option v-for="row in inventoryBasis" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryName }}</option></datalist>
        <button type="button" class="iv501__button iv501__button--light" aria-label="Find Inventory No" @click="inventoryPickerOpen = true">…</button>
        <button type="button" class="iv501__button" @click="queryStock">Query</button>
      </div>
      <div class="iv501__actions">
        <button type="button" class="iv501__button iv501__button--accent" @click="openConfirmation">FGD In</button>
        <button type="button" class="iv501__button iv501__button--light" @click="resetDevData">Reset DEV</button>
        <button type="button" class="iv501__button iv501__button--light" @click="emit('close')">Close (X)</button>
      </div>
    </header>

    <div class="iv501__content">
      <section class="iv501__panel iv501__panel--stock">
        <div class="iv501__panel-heading"><div><p class="iv501__eyebrow">IV501 · FGD INVENTORY · {{ previewLabel }}</p><h1>成品庫庫房資料</h1></div><span class="iv501__count">{{ visibleStock.length }} 筆</span></div>
        <p class="iv501__hint">主表依 InventoryNo 查詢；只顯示 LocQty &gt; 0。選取庫存列後，下方載入 Component；沒有 Component 才顯示 Lot Bin。</p>
        <div class="iv501__table-wrap">
          <table>
            <thead><tr><th>InventoryNo</th><th>ProductNo</th><th>LocatorNo</th><th>LotNo</th><th class="number">Qty</th><th>UnitNo</th><th>Belong To Type</th><th>Belong To No</th><th>InputDate</th><th>FGDInNo</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleStock" :key="stockKey(row)" :class="{ selected: activeStockKey === stockKey(row) }" tabindex="0" @click="selectStock(row)" @keydown.enter="selectStock(row)">
                <td>{{ row.inventoryNo }}</td><td>{{ row.productNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.lotNo }}</td><td class="number">{{ formatQty(row.locQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.belongToType === 0 ? 'MO' : '' }}</td><td>{{ row.belongToNo }}</td><td>{{ row.inputDate }}</td><td>{{ row.fgdInNo }}</td>
              </tr>
              <tr v-if="hasQueried && visibleStock.length === 0"><td colspan="10" class="iv501__empty">No record</td></tr>
              <tr v-if="!hasQueried"><td colspan="10" class="iv501__empty">請輸入或選擇成品庫，再按 Query。</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="iv501__panel iv501__panel--detail">
        <div class="iv501__detail-heading"><div><p class="iv501__eyebrow">選取列明細</p><h2>{{ activeStock ? `${activeStock.lotNo} · ${activeStock.locatorNo}` : '尚未選取庫存列' }}</h2></div><span v-if="activeStock" class="iv501__count">{{ activeComponents.length ? 'Component' : activeBins ? 'Bin State' : '無明細' }}</span></div>
        <template v-if="activeStock && activeComponents.length">
          <div class="iv501__table-wrap"><table class="iv501__compact"><thead><tr><th>ComponentNo</th><th class="number">GoodQty</th><th class="number">ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="row in activeComponents" :key="row.componentNo"><td>{{ row.componentNo }}</td><td class="number">{{ formatQty(row.goodQty) }}</td><td class="number">{{ formatQty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr></tbody></table></div>
        </template>
        <template v-else-if="activeStock && activeBins">
          <div class="iv501__table-wrap"><table class="iv501__compact"><thead><tr><th v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ bin.binNo }}</th></tr></thead><tbody><tr><td v-for="bin in orderedBins" :key="bin.binNo" class="number">{{ formatQty(activeBins.values[bin.binNo] ?? 0) }}</td></tr></tbody></table></div>
          <p class="iv501__hint">Bin 欄位由假 `TBLSYSBIN.BinOrder` 清單動態排序。</p>
        </template>
        <p v-else class="iv501__empty iv501__empty--detail">{{ activeStock ? '此 Lot 沒有 Component 或 Bin 資料。' : '選取上方一列以載入 Component／Bin 明細。' }}</p>
      </section>

      <section class="iv501__history">
        <div class="iv501__detail-heading"><div><p class="iv501__eyebrow">本機 DEV 紀錄</p><h2>最近入庫模擬</h2></div><span class="iv501__count">{{ records.activities.length }} 筆</span></div>
        <div class="iv501__table-wrap"><table class="iv501__compact"><thead><tr><th>FGDInNo</th><th>InventoryNo</th><th>Lot 筆數</th><th>合計 Qty</th><th>處理時間</th><th>結果</th></tr></thead><tbody><tr v-for="row in records.activities.slice(0, 4)" :key="row.fgdInNo"><td>{{ row.fgdInNo }}</td><td>{{ row.inventoryNo }}</td><td>{{ row.rowCount }}</td><td>{{ formatQty(row.totalQty) }}</td><td>{{ row.happenedAt }}</td><td>{{ row.result }}</td></tr><tr v-if="records.activities.length === 0"><td colspan="6" class="iv501__empty">尚無本機模擬紀錄。</td></tr></tbody></table></div>
      </section>
    </div>
    <p class="iv501__notice" role="status">{{ notice }}</p>

    <div v-if="inventoryPickerOpen" class="iv501__overlay" role="presentation" @click.self="inventoryPickerOpen = false">
      <section class="iv501__dialog iv501__dialog--small" role="dialog" aria-modal="true" aria-labelledby="iv501-inventory-title">
        <header class="iv501__dialog-heading"><div><p class="iv501__eyebrow">InventoryType = 1</p><h2 id="iv501-inventory-title">選擇成品庫</h2></div><button class="iv501__icon-button" type="button" aria-label="Close" @click="inventoryPickerOpen = false">×</button></header>
        <div class="iv501__picker-list"><button v-for="row in inventoryBasis" :key="row.inventoryNo" type="button" class="iv501__picker-row" @click="chooseInventory(row.inventoryNo)"><strong>{{ row.inventoryNo }}</strong><span>{{ row.inventoryName }}</span></button></div>
        <footer class="iv501__dialog-actions"><button type="button" class="iv501__button iv501__button--light" @click="inventoryPickerOpen = false">Cancel</button></footer>
      </section>
    </div>

    <div v-if="confirmationOpen" class="iv501__overlay" role="presentation" @click.self="closeConfirmation">
      <section class="iv501__dialog" role="dialog" aria-modal="true" aria-labelledby="iv501-confirm-title">
        <header class="iv501__dialog-heading"><div><p class="iv501__eyebrow">Source = 30 · FGD In</p><h2 id="iv501-confirm-title">成品入庫確認</h2></div><button class="iv501__icon-button" type="button" aria-label="Close" @click="closeConfirmation">×</button></header>
        <div class="iv501__confirm-grid">
          <section class="iv501__ticket-list"><div class="iv501__subheading"><h3>待確認單據</h3><span>{{ visibleTickets.length }}</span></div><p class="iv501__hint">State=UnConfirm 且 ToERP≠1；依 FGDInNo 排序。</p>
            <div class="iv501__table-wrap"><table class="iv501__compact"><thead><tr><th>FGDInNo</th><th>InventoryNo</th><th>Source</th><th>CreateDate</th></tr></thead><tbody><tr v-for="ticket in visibleTickets" :key="ticket.fgdInNo" :class="{ selected: activeTicketNo === ticket.fgdInNo }" tabindex="0" @click="activeTicketNo = ticket.fgdInNo" @keydown.enter="activeTicketNo = ticket.fgdInNo"><td>{{ ticket.fgdInNo }}</td><td>{{ ticket.inventoryNo }}</td><td>{{ ticketSourceLabel(ticket.source) }}</td><td>{{ ticket.createDate }}</td></tr><tr v-if="visibleTickets.length === 0"><td colspan="4" class="iv501__empty">沒有待確認單據。</td></tr></tbody></table></div>
          </section>
          <section class="iv501__ticket-detail"><div class="iv501__subheading"><h3>單據與明細</h3><span v-if="activeTicket">{{ stateLabel(activeTicket.state) }}</span></div>
            <template v-if="activeTicket">
              <dl class="iv501__ticket-meta"><div><dt>FGDInNo</dt><dd>{{ activeTicket.fgdInNo }}</dd></div><div><dt>InventoryNo</dt><dd>{{ activeTicket.inventoryNo }}</dd></div><div><dt>From InventoryNo</dt><dd>{{ activeTicket.fromInventoryNo || '—' }}</dd></div><div><dt>Creator / CreateDate</dt><dd>{{ activeTicket.creator }} · {{ activeTicket.createDate }}</dd></div><div class="wide"><dt>Description</dt><dd>{{ activeTicket.description }}</dd></div></dl>
              <div class="iv501__table-wrap"><table class="iv501__compact"><thead><tr><th>LotNo</th><th>ProductNo</th><th>LocatorNo</th><th class="number">Qty</th><th>UnitNo</th></tr></thead><tbody><template v-for="detail in activeTicket.details" :key="detail.lotNo"><tr><td>{{ detail.lotNo }}</td><td>{{ detail.productNo }} {{ detail.productVersion }}</td><td>{{ detail.locatorNo }}</td><td class="number">{{ formatQty(detail.qty) }}</td><td>{{ detail.unitNo }}</td></tr><tr v-for="component in detail.components" :key="`${detail.lotNo}-${component.componentNo}`" class="iv501__component-subrow"><td colspan="2">↳ {{ component.componentNo }}</td><td>Component</td><td class="number">Good {{ formatQty(component.goodQty) }} / Scrap {{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td></tr></template></tbody></table></div>
              <p class="iv501__summary">{{ activeTicket.details.length }} Lot 明細 · {{ activeTicketComponentCount }} Component · {{ formatQty(activeTicketQty) }} {{ activeTicket.details[0]?.unitNo }}</p>
              <p v-if="activeTicket.source === 1" class="iv501__source-note">來源庫存 {{ activeTicket.fromInventoryNo }} 將在本機 DEV 模擬扣量。</p>
            </template>
            <p v-else class="iv501__empty iv501__empty--detail">從左側選一張待確認單。</p>
          </section>
        </div>
        <footer class="iv501__dialog-actions"><button type="button" class="iv501__button iv501__button--light" @click="closeConfirmation">Cancel</button><button type="button" class="iv501__button iv501__button--accent" :disabled="!activeTicket" @click="confirmTicket">Confirm FGD In</button></footer>
        <p class="iv501__dialog-footnote">此預覽以 localStorage 模擬入庫數量及狀態；不連正式 API、Oracle 或 PostgreSQL。</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.iv501 { display:flex; flex-direction:column; min-height:0; height:100%; background:#f3f2ed; color:#272820; font:inherit; }
.iv501 * { box-sizing:border-box; }
.iv501__toolbar { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:10px 14px; border-bottom:1px solid #c8c8bd; background:#e8e7df; }
.iv501__lookup,.iv501__actions { display:flex; align-items:center; gap:7px; }
.iv501__lookup label { color:#4f5048; font-size:12px; font-weight:700; }
.iv501__lookup input { width:190px; height:29px; padding:4px 7px; border:1px solid #99998d; background:#fff; font:inherit; }
.iv501__button { min-height:29px; padding:4px 11px; border:1px solid #56574e; border-radius:2px; background:#44463e; color:#fff; font:inherit; font-size:12px; cursor:pointer; }
.iv501__button:hover { background:#303229; }
.iv501__button:disabled { opacity:.45; cursor:not-allowed; }
.iv501__button--light { border-color:#a4a498; background:#fafaf7; color:#33342d; }
.iv501__button--light:hover { background:#ededdf; }
.iv501__button--accent { border-color:#466443; background:#557552; }
.iv501__content { display:grid; grid-template-rows:minmax(220px,1.55fr) minmax(115px,.8fr) minmax(95px,.6fr); gap:9px; padding:10px 12px; flex:1; min-height:0; }
.iv501__panel,.iv501__history { display:flex; flex-direction:column; min-height:0; padding:10px; border:1px solid #bcbcb1; background:#fff; }
.iv501__panel-heading,.iv501__detail-heading,.iv501__dialog-heading { display:flex; align-items:center; justify-content:space-between; gap:10px; }
.iv501__panel-heading h1,.iv501__detail-heading h2,.iv501__dialog-heading h2 { margin:0; font-size:17px; font-weight:650; }
.iv501__eyebrow { margin:0 0 3px; color:#767769; font-size:10px; font-weight:700; letter-spacing:.08em; text-transform:uppercase; }
.iv501__count { flex:none; padding:3px 7px; border:1px solid #d2d2c9; border-radius:12px; color:#636459; font-size:11px; }
.iv501__hint { margin:6px 0 8px; color:#78796e; font-size:11px; }
.iv501__table-wrap { width:100%; min-height:0; overflow:auto; border:1px solid #deded6; }
.iv501 table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }
.iv501 th,.iv501 td { padding:5px 7px; border-bottom:1px solid #e5e5dd; text-align:left; }
.iv501 th { position:sticky; top:0; z-index:1; background:#eeeee8; color:#54554b; font-weight:700; }
.iv501 td.number,.iv501 th.number { text-align:right; font-variant-numeric:tabular-nums; }
.iv501 tbody tr { cursor:pointer; }
.iv501 tbody tr:hover { background:#f5f5ed; }
.iv501 tbody tr.selected { background:#e6eddc; }
.iv501 .iv501__compact { font-size:10.5px; }
.iv501 .iv501__compact th,.iv501 .iv501__compact td { padding:4px 7px; }
.iv501__empty { height:40px; color:#898a80; text-align:center!important; cursor:default; }
.iv501__empty--detail { display:grid; flex:1; place-items:center; margin:8px 0 0; border:1px dashed #d5d5ca; color:#88897e; font-size:11px; }
.iv501__history .iv501__detail-heading { margin-bottom:7px; }
.iv501__notice { flex:none; margin:0; padding:6px 13px; border-top:1px solid #cecec3; background:#eeeee7; color:#66675c; font-size:11px; }
.iv501__overlay { position:fixed; inset:0; z-index:1000; display:grid; place-items:center; padding:18px; background:rgba(20,22,18,.48); }
.iv501__dialog { display:flex; flex-direction:column; width:min(1120px,96vw); max-height:92vh; overflow:hidden; padding:14px; border:1px solid #77786e; background:#f8f8f4; box-shadow:0 15px 46px #0004; }
.iv501__dialog--small { width:min(460px,95vw); }
.iv501__dialog-heading { padding-bottom:10px; border-bottom:1px solid #d4d4cb; }
.iv501__icon-button { width:27px; height:27px; border:1px solid #b8b8ad; background:#fff; color:#444; font-size:19px; line-height:1; cursor:pointer; }
.iv501__picker-list { display:grid; gap:5px; padding:12px 0; }
.iv501__picker-row { display:flex; justify-content:space-between; gap:10px; padding:9px; border:1px solid #dadad1; background:#fff; color:#303129; text-align:left; cursor:pointer; }
.iv501__picker-row:hover { background:#edf1e8; }
.iv501__picker-row span { color:#77786d; }
.iv501__confirm-grid { display:grid; grid-template-columns:minmax(350px,.8fr) minmax(480px,1.2fr); gap:12px; min-height:0; overflow:auto; padding:12px 0; }
.iv501__ticket-list,.iv501__ticket-detail { display:flex; flex-direction:column; min-width:0; gap:7px; }
.iv501__subheading { display:flex; justify-content:space-between; align-items:center; gap:8px; }
.iv501__subheading h3 { margin:0; font-size:13px; }
.iv501__subheading span { color:#737469; font-size:11px; }
.iv501__ticket-meta { display:grid; grid-template-columns:1fr 1fr; gap:6px 12px; margin:0; padding:8px; border:1px solid #e1e1d8; background:#fff; font-size:10.5px; }
.iv501__ticket-meta .wide { grid-column:1 / -1; }
.iv501__ticket-meta dt { color:#7d7e73; }
.iv501__ticket-meta dd { margin:2px 0 0; color:#303129; }
.iv501__component-subrow td { background:#f6f6f1; color:#717267; font-size:10px; }
.iv501__summary,.iv501__source-note { margin:0; color:#5f6859; font-size:11px; }
.iv501__source-note { padding:6px; background:#eef2e8; }
.iv501__dialog-actions { display:flex; justify-content:flex-end; gap:7px; padding-top:9px; border-top:1px solid #d4d4cb; }
.iv501__dialog-footnote { margin:8px 0 0; color:#838478; font-size:10px; }
@media (max-width:900px) {
  .iv501__toolbar { align-items:flex-start; flex-direction:column; }
  .iv501__lookup { width:100%; flex-wrap:wrap; }
  .iv501__lookup input { flex:1; min-width:120px; }
  .iv501__content { grid-template-rows:minmax(250px,1.4fr) minmax(130px,.8fr) minmax(100px,.6fr); }
  .iv501__confirm-grid { grid-template-columns:1fr; }
}
</style>

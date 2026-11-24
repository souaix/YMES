<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type SourceType = 'Material In' | 'Material Out' | 'Material Adjust' | 'PreOrder' | 'Material Return' | 'WIP Return'
interface InventoryRow {
  inventoryNo: string
  inventoryName: string
  materialNo: string
  materialName: string
  safeQty: number
  curQty: number
  allocateQty: number
  unitNo: string
}
interface LocationRow {
  inventoryNo: string
  locatorNo: string
  materialNo: string
  materialName: string
  materialLotNo: string
  locQty: number
  inputDate: string
  vendorNo: string
  expireDate: string
  manufactureDate: string
  graceDate: string
}
interface TicketRow {
  sourceType: SourceType
  ticketNo: string
  inventoryNo: string
  materialNo: string
  materialLotNo: string
  locatorNo: string
  qty: number
  unitNo: string
  state: 0 | 2
  memo: string
  adjustType?: number
  diffQty?: number
  fromLocatorNo?: string
  toLocatorNo?: string
  vendorNo?: string
  preOrderNo?: string
  moNo?: string
  fromPlaceNo?: string
  fromPlaceType?: number
}
interface TransactionRow { transactionNo: string; ticketNo: string; sourceType: SourceType; processedAt: string }
interface DevState { version: 1; inventory: InventoryRow[]; locations: LocationRow[]; tickets: TicketRow[]; transactions: TransactionRow[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.raw-inventory.v1'
const sourceTypes: { type: SourceType; label: string; source: number }[] = [
  { type: 'Material In', label: 'Material In', source: 0 },
  { type: 'Material Out', label: 'Material Out', source: 1 },
  { type: 'Material Adjust', label: 'Material Adjust', source: 3 },
  { type: 'PreOrder', label: 'Pre Order', source: 4 },
  { type: 'Material Return', label: 'Material Return', source: 2 },
  { type: 'WIP Return', label: 'WIP Return', source: 10 },
]

function createSeed(): DevState {
  return {
    version: 1,
    inventory: [
      { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', safeQty: 40, curQty: 88, allocateQty: 14, unitNo: 'KG' },
      { inventoryNo: 'RAW-A', inventoryName: '一廠原物料倉', materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑 B', safeQty: 20, curQty: 26, allocateQty: 4, unitNo: 'L' },
      { inventoryNo: 'RAW-B', inventoryName: '二廠原物料倉', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', safeQty: 15, curQty: 32, allocateQty: 0, unitNo: 'RL' },
    ],
    locations: [
      { inventoryNo: 'RAW-A', locatorNo: 'A-01-01', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2609-001', locQty: 52, inputDate: '2026-10-18', vendorNo: 'V-DEV-01', expireDate: '2027-10-18', manufactureDate: '2026-10-01', graceDate: '2027-09-18' },
      { inventoryNo: 'RAW-A', locatorNo: 'A-01-02', materialNo: 'RM-RESIN-01', materialName: '環氧樹脂 A', materialLotNo: 'RES-2609-001', locQty: 36, inputDate: '2026-10-18', vendorNo: 'V-DEV-01', expireDate: '2027-10-18', manufactureDate: '2026-10-01', graceDate: '2027-09-18' },
      { inventoryNo: 'RAW-A', locatorNo: 'A-02-01', materialNo: 'RM-SOLVENT-02', materialName: '清洗溶劑 B', materialLotNo: 'SOL-2610-014', locQty: 26, inputDate: '2026-10-22', vendorNo: 'V-DEV-02', expireDate: '2027-04-22', manufactureDate: '2026-10-10', graceDate: '2027-03-22' },
      { inventoryNo: 'RAW-B', locatorNo: 'B-01-01', materialNo: 'RM-COPPER-03', materialName: '銅箔 C', materialLotNo: 'CU-2611-003', locQty: 32, inputDate: '2026-11-02', vendorNo: 'V-DEV-03', expireDate: '', manufactureDate: '2026-10-28', graceDate: '' },
    ],
    tickets: [
      { sourceType: 'Material In', ticketNo: 'MIN-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2611-006', locatorNo: 'A-01-01', qty: 12, unitNo: 'KG', state: 0, memo: 'DEV 假資料：供應商進料。', vendorNo: 'V-DEV-01' },
      { sourceType: 'Material Out', ticketNo: 'MOUT-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2609-001', locatorNo: 'A-01-01', qty: 6, unitNo: 'KG', state: 0, memo: 'DEV 假資料：工單領料。', preOrderNo: 'PRE-DEV-26002', moNo: 'MO-DEV-26001' },
      { sourceType: 'Material Adjust', ticketNo: 'MADJ-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2609-001', locatorNo: 'A-01-01', qty: 2, unitNo: 'KG', state: 0, memo: 'DEV 假資料：帳務盤盈。', adjustType: 0, diffQty: 2 },
      { sourceType: 'Material Adjust', ticketNo: 'MADJ-DEV-26002', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2609-001', locatorNo: 'A-01-01', qty: 3, unitNo: 'KG', state: 0, memo: 'DEV 假資料：同倉儲位移轉。', adjustType: 1, fromLocatorNo: 'A-01-01', toLocatorNo: 'A-01-02' },
      { sourceType: 'PreOrder', ticketNo: 'PRE-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-SOLVENT-02', materialLotNo: '', locatorNo: '', qty: 4, unitNo: 'L', state: 0, memo: 'DEV 假資料：增加預訂量，不移動現有數量。' },
      { sourceType: 'Material Return', ticketNo: 'MRET-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2609-001', locatorNo: 'A-01-02', qty: 2, unitNo: 'KG', state: 0, memo: 'DEV 假資料：退回供應商。', vendorNo: 'V-DEV-01' },
      { sourceType: 'WIP Return', ticketNo: 'WRET-DEV-26001', inventoryNo: 'RAW-A', materialNo: 'RM-RESIN-01', materialLotNo: 'RES-2609-001', locatorNo: 'A-01-02', qty: 3, unitNo: 'KG', state: 0, memo: 'DEV 假資料：在製退料回庫。', fromPlaceNo: 'WIP-AREA-01', fromPlaceType: 1 },
    ],
    transactions: [],
  }
}

function readState(): DevState {
  try {
    const saved = window.localStorage.getItem(storageKey)
    if (saved) {
      const parsed = JSON.parse(saved) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.inventory) && Array.isArray(parsed.locations) && Array.isArray(parsed.tickets) && Array.isArray(parsed.transactions)) return parsed
    }
  } catch { /* use DEV fixtures when local storage is unavailable */ }
  return createSeed()
}

const state = ref<DevState>(readState())
const inventoryNo = ref(state.value.inventory[0]?.inventoryNo ?? '')
const selectedMaterialNo = ref('')
const notice = ref('DEV 假資料：只在本機瀏覽器模擬，不呼叫 MES 或正式 API。')
const activeSourceType = ref<SourceType | null>(null)
const selectedTicketNo = ref('')
const selectedSource = computed(() => sourceTypes.find((item) => item.type === activeSourceType.value) ?? null)
const inventoryRows = computed(() => state.value.inventory.filter((row) => row.inventoryNo === inventoryNo.value && row.curQty > 0))
const selectedInventory = computed(() => inventoryRows.value.find((row) => row.materialNo === selectedMaterialNo.value) ?? inventoryRows.value[0] ?? null)
const locationRows = computed(() => selectedInventory.value
  ? state.value.locations.filter((row) => row.inventoryNo === selectedInventory.value?.inventoryNo && row.materialNo === selectedInventory.value?.materialNo)
  : [])
const availableTickets = computed(() => state.value.tickets.filter((row) => row.sourceType === activeSourceType.value && row.state === 0))
const selectedTicket = computed(() => availableTickets.value.find((row) => row.ticketNo === selectedTicketNo.value) ?? availableTickets.value[0] ?? null)
const totalQty = computed(() => inventoryRows.value.reduce((sum, row) => sum + row.curQty, 0))
const belowSafeCount = computed(() => inventoryRows.value.filter((row) => row.curQty < row.safeQty).length)
const latestTransactions = computed(() => [...state.value.transactions].reverse().slice(0, 4))

watch(inventoryRows, (rows) => {
  if (!rows.some((row) => row.materialNo === selectedMaterialNo.value)) selectedMaterialNo.value = rows[0]?.materialNo ?? ''
}, { immediate: true })
watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV may run with storage disabled */ }
}, { deep: true })
watch(availableTickets, (rows) => {
  if (!rows.some((row) => row.ticketNo === selectedTicketNo.value)) selectedTicketNo.value = rows[0]?.ticketNo ?? ''
})

function openSource(sourceType: SourceType): void {
  const pending = state.value.tickets.find((row) => row.sourceType === sourceType && row.state === 0)
  if (!pending) {
    notice.value = `目前沒有待確認的 ${sourceType} DEV 單據。`
    return
  }
  activeSourceType.value = sourceType
  selectedTicketNo.value = pending.ticketNo
}

function roundQty(value: number): number { return Math.round((value + Number.EPSILON) * 1_000_000) / 1_000_000 }
function getInventory(ticket: TicketRow): InventoryRow | undefined {
  return state.value.inventory.find((row) => row.inventoryNo === ticket.inventoryNo && row.materialNo === ticket.materialNo)
}
function getLocation(ticket: TicketRow, locatorNo = ticket.locatorNo): LocationRow | undefined {
  return state.value.locations.find((row) => row.inventoryNo === ticket.inventoryNo && row.materialNo === ticket.materialNo && row.materialLotNo === ticket.materialLotNo && row.locatorNo === locatorNo)
}
function changeLocation(ticket: TicketRow, delta: number, locatorNo = ticket.locatorNo): void {
  let row = getLocation(ticket, locatorNo)
  if (!row && delta > 0) {
    row = {
      inventoryNo: ticket.inventoryNo, locatorNo, materialNo: ticket.materialNo,
      materialName: getInventory(ticket)?.materialName ?? ticket.materialNo, materialLotNo: ticket.materialLotNo,
      locQty: 0, inputDate: new Date().toISOString().slice(0, 10), vendorNo: 'V-DEV-01',
      expireDate: '', manufactureDate: '', graceDate: '',
    }
    state.value.locations.push(row)
  }
  if (row) row.locQty = roundQty(row.locQty + delta)
}
function closeDialog(): void { activeSourceType.value = null }

function confirmTicket(): void {
  const ticket = selectedTicket.value
  const master = ticket ? getInventory(ticket) : undefined
  if (!ticket || !master) {
    notice.value = 'DEV 單據找不到對應的庫房物料資料。'
    return
  }
  const qty = ticket.qty
  const source = ticket.sourceType
  if (source === 'Material Out' || source === 'Material Return') {
    const location = getLocation(ticket)
    if (!location || location.locQty < qty || master.curQty < qty) {
      notice.value = `DEV ${source} 數量超過該批號可用庫存，請先重新整理。`
      return
    }
    changeLocation(ticket, -qty)
    master.curQty = roundQty(master.curQty - qty)
    if (source === 'Material Out') master.allocateQty = roundQty(Math.max(0, master.allocateQty - qty))
  } else if (source === 'PreOrder') {
    master.allocateQty = roundQty(master.allocateQty + qty)
  } else if (source === 'Material Adjust' && ticket.adjustType !== 0) {
    const from = getLocation(ticket, ticket.fromLocatorNo)
    if (!from || from.locQty < qty || !ticket.toLocatorNo) {
      notice.value = 'DEV 儲位調整的來源儲位數量不足。'
      return
    }
    changeLocation(ticket, -qty, ticket.fromLocatorNo)
    changeLocation(ticket, qty, ticket.toLocatorNo)
  } else {
    const delta = source === 'Material Adjust' ? (ticket.diffQty ?? qty) : qty
    if (master.curQty + delta < 0 || (getLocation(ticket)?.locQty ?? 0) + delta < 0) {
      notice.value = 'DEV 調整量不可大於目前庫存。'
      return
    }
    changeLocation(ticket, delta)
    master.curQty = roundQty(master.curQty + delta)
  }

  ticket.state = 2
  const transactionNo = `DEV-${Date.now().toString(36).toUpperCase()}`
  state.value.transactions.push({ transactionNo, ticketNo: ticket.ticketNo, sourceType: source, processedAt: new Date().toLocaleString('zh-TW', { hour12: false }) })
  notice.value = `DEV 已模擬 ${ticket.ticketNo} 確認並更新畫面數量。`
  closeDialog()
}

function refresh(): void { notice.value = 'DEV 假資料已重新顯示；資料保存在本機瀏覽器。' }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value) }
function stateLabel(ticket: TicketRow): string { return ticket.state === 0 ? '待確認' : '已完成' }
</script>

<template>
  <main class="raw-inventory" aria-label="原物料倉庫房資料 DEV" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading">
      <div><p>INVENTORY · IV201</p><h1>原物料倉庫房資料</h1></div>
      <span class="dev-badge">DEV MOCK</span>
    </header>

    <section class="toolbar" aria-label="庫房篩選">
      <label>原物料庫房
        <select v-model="inventoryNo">
          <option v-for="row in [...new Map(state.inventory.map((item) => [item.inventoryNo, item])).values()]" :key="row.inventoryNo" :value="row.inventoryNo">{{ row.inventoryNo }} · {{ row.inventoryName }}</option>
        </select>
      </label>
      <button type="button" @click="refresh">Refresh</button>
      <span class="notice" role="status">{{ notice }}</span>
    </section>

    <section class="summary" aria-label="庫存摘要">
      <article><span>品項數</span><strong>{{ inventoryRows.length }}</strong><small>目前庫房</small></article>
      <article><span>現有庫存</span><strong>{{ formatQty(totalQty) }}</strong><small>各品項單位分別計算</small></article>
      <article :class="{ warning: belowSafeCount > 0 }"><span>低於安全量</span><strong>{{ belowSafeCount }}</strong><small>CurQty &lt; SafeQty</small></article>
      <article><span>預訂量</span><strong>{{ formatQty(inventoryRows.reduce((sum, row) => sum + row.allocateQty, 0)) }}</strong><small>AllocateQty</small></article>
    </section>

    <section class="workspace">
      <div class="main-grid panel">
        <header class="panel-heading"><div><h2>Raw Inventory</h2><span>{{ inventoryNo || '未選擇庫房' }}</span></div><small>選取品項以檢視批號與儲位</small></header>
        <div class="table-wrap">
          <table>
            <thead><tr><th>InventoryNo</th><th>MaterialNo</th><th>SafeQty</th><th>CurQty</th><th>AllocateQty</th><th>UnitNo</th></tr></thead>
            <tbody>
              <tr v-for="row in inventoryRows" :key="`${row.inventoryNo}-${row.materialNo}`" :class="{ active: selectedInventory?.materialNo === row.materialNo }" @click="selectedMaterialNo = row.materialNo">
                <td class="mono">{{ row.inventoryNo }}</td><td><strong>{{ row.materialNo }}</strong><small class="subtext">{{ row.materialName }}</small></td>
                <td class="number">{{ formatQty(row.safeQty) }}</td><td class="number" :class="{ low: row.curQty < row.safeQty }">{{ formatQty(row.curQty) }}</td><td class="number">{{ formatQty(row.allocateQty) }}</td><td>{{ row.unitNo }}</td>
              </tr>
              <tr v-if="inventoryRows.length === 0"><td colspan="6" class="empty">此庫房目前沒有 CurQty 大於 0 的品項。</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="grid-footer">{{ inventoryRows.length }} 筆 · {{ selectedInventory?.materialNo ?? '未選取品項' }}</footer>
      </div>

      <aside class="actions" aria-label="原料單據操作">
        <h2>單據確認</h2>
        <button v-for="item in sourceTypes" :key="item.type" type="button" @click="openSource(item.type)">{{ item.label }}</button>
        <small>只列出 DEV 待確認單據</small>
        <button type="button" class="close-button" @click="emit('close')">Close (X)</button>
      </aside>
    </section>

    <section class="panel locator-panel">
      <header class="panel-heading"><div><h2>Locator / Lot</h2><span>{{ selectedInventory?.materialName ?? '請先選取庫存品項' }}</span></div><small>{{ locationRows.length }} 筆儲位批號明細</small></header>
      <div class="table-wrap">
        <table>
          <thead><tr><th>LocatorNo</th><th>MaterialNo</th><th>MaterialLotNo</th><th>LocQty</th><th>ExpireDate</th><th>ManufactureDate</th><th>GraceDate</th></tr></thead>
          <tbody>
            <tr v-for="row in locationRows" :key="`${row.locatorNo}-${row.materialLotNo}`">
              <td class="mono">{{ row.locatorNo }}</td><td>{{ row.materialNo }}</td><td class="mono">{{ row.materialLotNo }}</td><td class="number">{{ formatQty(row.locQty) }}</td><td>{{ row.expireDate || '—' }}</td><td>{{ row.manufactureDate || '—' }}</td><td>{{ row.graceDate || '—' }}</td>
            </tr>
            <tr v-if="locationRows.length === 0"><td colspan="7" class="empty">沒有符合的儲位明細。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>顯示欄位依 WinForms Locator Grid</span><span>DEV 資料會保存在 localStorage</span></footer>
    </section>

    <section v-if="latestTransactions.length" class="recent-activity" aria-label="DEV 最近確認紀錄">
      <span>最近 DEV 確認</span><div v-for="item in latestTransactions" :key="item.transactionNo"><strong>{{ item.ticketNo }}</strong><small>{{ item.sourceType }} · {{ item.processedAt }}</small></div>
    </section>

    <div v-if="activeSourceType" class="modal-backdrop" @click.self="closeDialog">
      <section class="ticket-dialog" role="dialog" aria-modal="true" :aria-label="`${activeSourceType} Confirm`">
        <header><div><p>DEV TICKET CONFIRM · Source {{ selectedSource?.source }}</p><h2>{{ activeSourceType }} Confirm</h2></div><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
        <label>待確認單據
          <select v-model="selectedTicketNo">
            <option v-for="ticket in availableTickets" :key="ticket.ticketNo" :value="ticket.ticketNo">{{ ticket.ticketNo }} · {{ ticket.inventoryNo }} · {{ ticket.materialNo }}</option>
          </select>
        </label>
        <template v-if="selectedTicket">
          <div class="ticket-meta"><span>狀態 <b>{{ stateLabel(selectedTicket) }}</b></span><span>單位 <b>{{ selectedTicket.unitNo }}</b></span></div>
          <div class="ticket-detail">
            <div><span>庫房</span><strong>{{ selectedTicket.inventoryNo }}</strong></div><div><span>物料</span><strong>{{ selectedTicket.materialNo }}</strong></div>
            <div><span>批號</span><strong>{{ selectedTicket.materialLotNo || '—' }}</strong></div><div><span>數量</span><strong>{{ formatQty(selectedTicket.qty) }}</strong></div>
            <div v-if="selectedTicket.vendorNo"><span>VendorNo</span><strong>{{ selectedTicket.vendorNo }}</strong></div>
            <div v-if="selectedTicket.preOrderNo"><span>PreOrderNo / MONo</span><strong>{{ selectedTicket.preOrderNo }} / {{ selectedTicket.moNo || '—' }}</strong></div>
            <div v-if="selectedTicket.fromPlaceNo"><span>FromPlace</span><strong>{{ selectedTicket.fromPlaceNo }} · Type {{ selectedTicket.fromPlaceType }}</strong></div>
            <div v-if="selectedSource?.type === 'Material Adjust'"><span>調整模式</span><strong>{{ selectedTicket.adjustType === 0 ? '帳務數量調整' : '儲位調整' }}</strong></div>
            <div v-if="selectedTicket.adjustType !== undefined"><span>數量差異</span><strong>{{ selectedTicket.adjustType === 0 ? formatQty(selectedTicket.diffQty ?? 0) : `${selectedTicket.fromLocatorNo} → ${selectedTicket.toLocatorNo}` }}</strong></div>
            <div><span>儲位</span><strong>{{ selectedTicket.locatorNo || '依預訂單明細' }}</strong></div><div><span>說明</span><strong>{{ selectedTicket.memo }}</strong></div>
          </div>
          <p class="mock-note">DEV 假資料：Confirm 會在本機模擬狀態與庫存變化，不會送出正式交易。</p>
        </template>
        <p v-else class="empty">目前沒有待確認的 {{ activeSourceType }} 單據。</p>
        <footer><button type="button" @click="closeDialog">Cancel</button><button type="button" class="primary" :disabled="!selectedTicket" @click="confirmTicket">Confirm</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.raw-inventory{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;gap:8px;padding:10px;box-sizing:border-box;background:#f4f2eb;color:#292820;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 6px;border-bottom:1px solid #cbc8bb}.page-heading p{margin:0;color:#777366;font-size:11px;letter-spacing:.07em}.page-heading h1{margin:0;font-size:20px;font-weight:600}.dev-badge{padding:3px 8px;border:1px solid #8e946e;background:#e8ecd9;color:#4c5c39;font-size:10px;font-weight:700;letter-spacing:.08em}.toolbar{display:flex;align-items:center;gap:10px;min-height:36px;flex-wrap:wrap}.toolbar label{display:flex;align-items:center;gap:8px;font-weight:600}.toolbar select,.ticket-dialog select{min-height:29px;min-width:245px;padding:4px 8px;border:1px solid #999587;background:#fffef8;color:#292820;font:inherit}.raw-inventory button{min-height:28px;padding:4px 9px;border:1px solid #8c887b;border-radius:2px;background:#fbfaf5;color:#292820;font:inherit;cursor:pointer;white-space:nowrap}.raw-inventory button:hover:not(:disabled){background:#e7e4d9}.raw-inventory button:disabled{opacity:.5;cursor:not-allowed}.raw-inventory button.primary{border-color:#48634d;background:#506c54;color:white}.notice{flex:1;min-width:250px;color:#625f55;font-size:11px}.summary{display:grid;grid-template-columns:repeat(4,minmax(130px,1fr));gap:8px}.summary article{display:grid;grid-template-columns:1fr auto;align-items:center;gap:0 8px;padding:7px 10px;border:1px solid #d2cfc4;background:#fbfaf5}.summary article>span{color:#716d62;font-size:11px}.summary article strong{grid-column:2;grid-row:1/3;font-size:21px;font-weight:600;font-variant-numeric:tabular-nums}.summary article small{font-size:10px;color:#8a867b}.summary article.warning strong{color:#a65036}.workspace{display:flex;flex:1;min-height:220px;gap:8px}.panel{display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid #c6c2b6;background:#fffef9}.main-grid{flex:1}.panel-heading{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:7px 9px;border-bottom:1px solid #d6d3c8;background:#eeece4}.panel-heading h2{margin:0;font-size:13px;font-weight:600}.panel-heading span,.panel-heading small{color:#777366;font-size:10px}.panel-heading>div{display:flex;align-items:baseline;gap:9px}.table-wrap{flex:1;min-height:0;overflow:auto}.raw-inventory table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.raw-inventory th,.raw-inventory td{height:26px;padding:3px 7px;border-right:1px solid #e0ded6;border-bottom:1px solid #e3e1da;text-align:left}.raw-inventory th{position:sticky;top:0;z-index:1;background:#e8e6dd;color:#514f46;font-weight:600}.raw-inventory tbody tr{cursor:pointer}.raw-inventory tbody tr:hover{background:#f5f2e5}.raw-inventory tbody tr.active{background:#e4ecde}.number{text-align:right!important;font-variant-numeric:tabular-nums}.low{color:#a65036;font-weight:700}.mono{font-family:Consolas,"Courier New",monospace}.subtext{display:block;margin-top:1px;color:#817d72;font-size:10px}.empty{text-align:center;color:#817d72;padding:16px!important}.grid-footer{display:flex;justify-content:space-between;gap:8px;min-height:22px;padding:3px 8px;border-top:1px solid #d6d3c8;color:#777366;font-size:10px}.actions{display:flex;flex:none;flex-direction:column;gap:5px;width:126px;padding:8px;border:1px solid #c6c2b6;background:#eeece4}.actions h2{margin:0 0 2px;font-size:12px}.actions button{width:100%;min-height:30px;text-align:left}.actions small{margin-top:2px;color:#777366;font-size:10px}.actions .close-button{margin-top:auto;text-align:center}.locator-panel{height:30%;min-height:132px}.recent-activity{display:flex;align-items:center;gap:10px;min-height:25px;overflow-x:auto;padding:1px 3px;color:#777366;font-size:10px}.recent-activity>span{flex:none;font-weight:600}.recent-activity>div{display:flex;flex:none;gap:6px;align-items:baseline;padding:2px 7px;border-left:1px solid #cbc8bb}.recent-activity strong{color:#504d43}.modal-backdrop{position:fixed;inset:0;z-index:40;display:flex;align-items:center;justify-content:center;padding:16px;background:rgb(25 24 20 / 44%)}.ticket-dialog{display:flex;flex-direction:column;gap:10px;width:min(620px,96vw);max-height:92vh;overflow:auto;padding:13px;border:1px solid #898578;background:#f8f6ef;box-shadow:0 8px 24px #0005}.ticket-dialog>header{display:flex;align-items:center;justify-content:space-between;padding-bottom:7px;border-bottom:1px solid #cfccbf}.ticket-dialog>header p{margin:0;color:#777366;font-size:10px;letter-spacing:.06em}.ticket-dialog>header h2{margin:0;font-size:17px;font-weight:600}.ticket-dialog>header button{width:29px;height:27px;padding:0;font-size:20px}.ticket-dialog>label{display:flex;align-items:center;gap:10px}.ticket-meta{display:flex;gap:20px;color:#777366;font-size:11px}.ticket-meta b{color:#333127}.ticket-detail{display:grid;grid-template-columns:1fr 1fr;gap:7px;padding:10px;border:1px solid #d0cdc2;background:#fffef9}.ticket-detail>div{display:flex;flex-direction:column;gap:2px;min-width:0}.ticket-detail span{color:#777366;font-size:10px}.ticket-detail strong{overflow-wrap:anywhere;font-size:12px;font-weight:600}.mock-note{margin:0;color:#756e53;font-size:11px}.ticket-dialog>footer{display:flex;justify-content:flex-end;gap:7px;padding-top:8px;border-top:1px solid #cfccbf}@media(max-width:900px){.workspace{gap:5px}.actions{width:108px}.summary{grid-template-columns:repeat(2,minmax(120px,1fr))}.locator-panel{height:34%}}@media(max-width:600px){.workspace{flex-direction:column;min-height:430px}.main-grid{min-height:280px}.actions{width:auto;flex-direction:row;flex-wrap:wrap}.actions h2{width:100%}.actions button{width:auto}.actions small{width:100%}.actions .close-button{margin:0}.locator-panel{height:32%;min-height:145px}.ticket-dialog>label{align-items:stretch;flex-direction:column;gap:4px}.ticket-dialog select{width:100%;min-width:0}.ticket-detail{grid-template-columns:1fr}}
</style>

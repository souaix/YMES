<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type TurnType = 0 | 1 | 2
type TicketState = 0 | 1 | -1
interface ScrapSource {
  inventoryNo: string; lotNo: string; lotSerial: string; opNo: string; scrapQty: number
  lossQty: number; unitNo: string; baseLotNo: string; inputDate: string; bookingFlag: boolean
}
interface ComponentSource {
  inventoryNo: string; lotNo: string; lotSerial: string; opNo: string; componentNo: string
  goodQty: number; scrapQty: number; unitNo: string; bookingFlag: boolean
}
interface FailBinSource {
  inventoryNo: string; lotNo: string; lotSerial: string; opNo: string; unitNo: string
  bins: Record<string, number>; bookingFlag: boolean
}
interface Ticket {
  scrInNo: string; toInventoryNo: string; fromInventoryNo: string; description: string
  creator: string; createDate: string; inputDate: string; state: TicketState; turnType: TurnType
  sourceKeys: string[]
}
interface DevState {
  version: 1; tickets: Ticket[]; scrapSources: ScrapSource[]
  componentSources: ComponentSource[]; failBinSources: FailBinSource[]; nextNo: number
}
interface TicketDetail {
  lotNo: string; lotSerial: string; opNo: string; unitNo: string
  scrapQty: number; lossQty: number; goodQty: number; componentCount: number
}

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.scr-in.v1'
const toInventories = [
  { inventoryNo: 'SCR-01', inventoryName: '一廠不良品倉', inventoryType: 3 },
  { inventoryNo: 'SCR-02', inventoryName: '二廠報廢倉', inventoryType: 3 },
]
const fromInventories = [
  { inventoryNo: 'WIP-01', inventoryName: '一廠線邊半成品庫', inventoryType: 6 },
  { inventoryNo: 'WIP-02', inventoryName: '二廠線邊半成品庫', inventoryType: 6 },
]
const bins = [{ binNo: 'BIN01', binOrder: 1 }, { binNo: 'BIN02', binOrder: 2 }, { binNo: 'BIN03', binOrder: 3 }]

function createSeed(): DevState {
  return {
    version: 1,
    tickets: [{
      scrInNo: 'SCRIN-DEV-26001', toInventoryNo: 'SCR-01', fromInventoryNo: 'WIP-01',
      description: '線邊退回待檢不良品', creator: 'DEV01', createDate: '2026-11-25 08:35:00',
      inputDate: '', state: 0, turnType: 0, sourceKeys: ['SCR|WIP-01|S001'],
    }],
    scrapSources: [
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26001', lotSerial: 'S001', opNo: 'OP-ETCH-20', scrapQty: 3, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-26001', inputDate: '2026-11-24 09:14:20', bookingFlag: true },
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26002', lotSerial: 'S002', opNo: 'OP-TEST-70', scrapQty: 0, lossQty: 1, unitNo: 'PCS', baseLotNo: 'BASE-26002', inputDate: '2026-11-24 10:30:05', bookingFlag: false },
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26003', lotSerial: 'S003', opNo: 'OP-INSPECT-80', scrapQty: 2, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-26003', inputDate: '2026-11-25 08:02:00', bookingFlag: false },
      { inventoryNo: 'WIP-02', lotNo: 'LOT-DEV-26011', lotSerial: 'S011', opNo: 'OP-TEST-70', scrapQty: 4, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-26011', inputDate: '2026-11-22 16:47:10', bookingFlag: false },
    ],
    componentSources: [
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26002', lotSerial: 'S002', opNo: 'OP-TEST-70', componentNo: 'COMP-DEV-A', goodQty: 4, scrapQty: 1, unitNo: 'PCS', bookingFlag: false },
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26003', lotSerial: 'S003', opNo: 'OP-INSPECT-80', componentNo: 'COMP-DEV-B', goodQty: 2, scrapQty: 0.5, unitNo: 'PCS', bookingFlag: false },
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26003', lotSerial: 'S003', opNo: 'OP-INSPECT-80', componentNo: 'COMP-DEV-C', goodQty: 1, scrapQty: 0.25, unitNo: 'PCS', bookingFlag: false },
    ],
    failBinSources: [
      { inventoryNo: 'WIP-01', lotNo: 'LOT-DEV-26003', lotSerial: 'S003', opNo: 'OP-INSPECT-80', unitNo: 'PCS', bins: { BIN01: 1.25, BIN02: 0.75, BIN03: 0 }, bookingFlag: false },
      { inventoryNo: 'WIP-02', lotNo: 'LOT-DEV-26011', lotSerial: 'S011', opNo: 'OP-TEST-70', unitNo: 'PCS', bins: { BIN01: 2, BIN02: 1, BIN03: 1 }, bookingFlag: false },
    ],
    nextNo: 26002,
  }
}
function readState(): DevState {
  try {
    const saved = window.localStorage.getItem(storageKey)
    if (saved) {
      const parsed = JSON.parse(saved) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.tickets) && Array.isArray(parsed.scrapSources) && Array.isArray(parsed.componentSources) && Array.isArray(parsed.failBinSources)) return parsed
    }
  } catch { /* Fall back to local DEV fixtures if storage is unavailable. */ }
  return createSeed()
}

const state = ref<DevState>(readState())
const selectedTicketNo = ref('')
const selectedTicketNos = ref<string[]>([])
const search = ref('')
const notice = ref('DEV 假資料：此頁只建立／維護不良品入庫草稿與來源預約，不呼叫正式 API。')
const dialogOpen = ref(false)
const deleteOpen = ref(false)
const showSelectedOnly = ref(false)
const mode = ref<'add' | 'edit'>('add')
const draft = ref({ scrInNo: '', toInventoryNo: 'SCR-01', fromInventoryNo: 'WIP-01', description: '', turnType: 0 as TurnType })
const draftSelectedKeys = ref<string[]>([])

const pendingTickets = computed(() => state.value.tickets
  .filter((ticket) => ticket.state === 0)
  .filter((ticket) => !search.value.trim() || ticket.scrInNo.toLowerCase().includes(search.value.trim().toLowerCase()) || ticket.toInventoryNo.toLowerCase().includes(search.value.trim().toLowerCase()))
  .sort((a, b) => a.scrInNo.localeCompare(b.scrInNo)))
const activeTicket = computed(() => pendingTickets.value.find((ticket) => ticket.scrInNo === selectedTicketNo.value) ?? pendingTickets.value[0] ?? null)
const ticketDetails = computed(() => activeTicket.value ? detailsFor(activeTicket.value.turnType, activeTicket.value.sourceKeys) : [])
const ticketComponents = computed(() => activeTicket.value?.turnType === 1
  ? state.value.componentSources.filter((row) => activeTicket.value?.sourceKeys.includes(componentKey(row)))
  : [])
const ticketFailBins = computed(() => activeTicket.value?.turnType === 2
  ? state.value.failBinSources.filter((row) => activeTicket.value?.sourceKeys.includes(failBinKey(row)))
  : [])
const pendingCount = computed(() => state.value.tickets.filter((ticket) => ticket.state === 0).length)
const sourceRows = computed(() => {
  let rows: Array<ScrapSource | ComponentSource | FailBinSource>
  if (draft.value.turnType === 0) rows = state.value.scrapSources.filter((row) => row.inventoryNo === draft.value.fromInventoryNo && (row.scrapQty > 0 || row.lossQty > 0) && (!row.bookingFlag || currentOwns(scrapKey(row))))
  else if (draft.value.turnType === 1) rows = state.value.componentSources.filter((row) => row.inventoryNo === draft.value.fromInventoryNo && (row.scrapQty > 0 || row.goodQty > 0) && (!row.bookingFlag || currentOwns(componentKey(row))))
  else rows = state.value.failBinSources.filter((row) => row.inventoryNo === draft.value.fromInventoryNo && (!row.bookingFlag || currentOwns(failBinKey(row))))
  if (showSelectedOnly.value) rows = rows.filter((row) => draftSelectedKeys.value.includes(sourceKey(row, draft.value.turnType)))
  return rows
})
const selectedSourceCount = computed(() => draftSelectedKeys.value.length)

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep the current DEV session usable. */ }
}, { deep: true })
watch(pendingTickets, (rows) => {
  if (!rows.some((ticket) => ticket.scrInNo === selectedTicketNo.value)) selectedTicketNo.value = rows[0]?.scrInNo ?? ''
  selectedTicketNos.value = selectedTicketNos.value.filter((no) => rows.some((ticket) => ticket.scrInNo === no))
}, { immediate: true })

function scrapKey(row: ScrapSource): string { return 'SCR|' + row.inventoryNo + '|' + row.lotSerial }
function componentKey(row: ComponentSource): string { return 'COMP|' + row.inventoryNo + '|' + row.lotSerial + '|' + row.componentNo }
function failBinKey(row: FailBinSource): string { return 'BIN|' + row.inventoryNo + '|' + row.lotSerial }
function sourceKey(row: ScrapSource | ComponentSource | FailBinSource, turnType: TurnType): string {
  if (turnType === 0) return scrapKey(row as ScrapSource)
  if (turnType === 1) return componentKey(row as ComponentSource)
  return failBinKey(row as FailBinSource)
}
function currentOwns(key: string): boolean {
  if (mode.value !== 'edit') return false
  return state.value.tickets.some((ticket) => ticket.scrInNo === draft.value.scrInNo && ticket.state === 0 && ticket.sourceKeys.includes(key))
}
function detailsFor(turnType: TurnType, keys: string[]): TicketDetail[] {
  if (turnType === 0) return state.value.scrapSources.filter((row) => keys.includes(scrapKey(row))).map((row) => ({
    lotNo: row.lotNo, lotSerial: row.lotSerial, opNo: row.opNo, unitNo: row.unitNo, scrapQty: row.scrapQty, lossQty: row.lossQty, goodQty: 0, componentCount: 0,
  }))
  if (turnType === 1) {
    const grouped = new Map<string, TicketDetail>()
    for (const row of state.value.componentSources.filter((item) => keys.includes(componentKey(item)))) {
      const item = grouped.get(row.lotSerial) ?? { lotNo: row.lotNo, lotSerial: row.lotSerial, opNo: row.opNo, unitNo: row.unitNo, scrapQty: 0, lossQty: 0, goodQty: 0, componentCount: 0 }
      item.scrapQty += row.scrapQty
      item.goodQty += row.goodQty
      item.componentCount += 1
      grouped.set(row.lotSerial, item)
    }
    return [...grouped.values()]
  }
  return state.value.failBinSources.filter((row) => keys.includes(failBinKey(row))).map((row) => ({
    lotNo: row.lotNo, lotSerial: row.lotSerial, opNo: row.opNo, unitNo: row.unitNo,
    scrapQty: bins.reduce((total, bin) => total + (row.bins[bin.binNo] ?? 0), 0), lossQty: 0, goodQty: 0, componentCount: 0,
  }))
}
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value) }
function typeLabel(turnType: TurnType): string { return ['線邊 Scrap Lot', 'Scrap Component', 'Fail Bin'][turnType] }
function stateLabel(stateValue: TicketState): string { return stateValue === 0 ? 'UnConfirm' : stateValue === 1 ? 'Handling' : 'Void' }
function toggleTicket(ticket: Ticket, checked: boolean): void {
  selectedTicketNos.value = checked ? [...new Set([...selectedTicketNos.value, ticket.scrInNo])] : selectedTicketNos.value.filter((no) => no !== ticket.scrInNo)
  selectedTicketNo.value = ticket.scrInNo
}
function toggleAllTickets(checked: boolean): void { selectedTicketNos.value = checked ? pendingTickets.value.map((ticket) => ticket.scrInNo) : [] }
function toggleSource(key: string, checked: boolean): void {
  draftSelectedKeys.value = checked ? [...new Set([...draftSelectedKeys.value, key])] : draftSelectedKeys.value.filter((item) => item !== key)
}
function toggleAllSources(checked: boolean): void {
  const keys = sourceRows.value.map((row) => sourceKey(row, draft.value.turnType))
  draftSelectedKeys.value = checked ? [...new Set([...draftSelectedKeys.value, ...keys])] : draftSelectedKeys.value.filter((key) => !keys.includes(key))
}
function selectTicket(ticket: Ticket): void { selectedTicketNo.value = ticket.scrInNo }
function reserveNumber(): string {
  const value = 'SCRIN-DEV-' + String(state.value.nextNo).padStart(5, '0')
  state.value.nextNo += 1
  return value
}
function openAdd(): void {
  mode.value = 'add'
  draft.value = { scrInNo: reserveNumber(), toInventoryNo: 'SCR-01', fromInventoryNo: 'WIP-01', description: '', turnType: 0 }
  draftSelectedKeys.value = []
  showSelectedOnly.value = false
  dialogOpen.value = true
}
function openEdit(): void {
  const ticket = activeTicket.value
  if (!ticket || ticket.state !== 0) return
  mode.value = 'edit'
  draft.value = { scrInNo: ticket.scrInNo, toInventoryNo: ticket.toInventoryNo, fromInventoryNo: ticket.fromInventoryNo, description: ticket.description, turnType: ticket.turnType }
  draftSelectedKeys.value = [...ticket.sourceKeys]
  showSelectedOnly.value = false
  dialogOpen.value = true
}
function setTurnType(value: number): void {
  if (mode.value === 'edit') return
  draft.value.turnType = value as TurnType
  draftSelectedKeys.value = []
}
function setFromInventory(value: string): void {
  if (mode.value === 'edit') return
  draft.value.fromInventoryNo = value
  draftSelectedKeys.value = []
}
function saveDraft(): void {
  if (!draft.value.scrInNo.trim() || !draft.value.toInventoryNo || !draft.value.fromInventoryNo || !draftSelectedKeys.value.length) {
    notice.value = '單號、來源／目標庫房必填，且至少選取一筆來源資料。'
    return
  }
  const oldTicket = state.value.tickets.find((ticket) => ticket.scrInNo === draft.value.scrInNo && ticket.state === 0)
  if (mode.value === 'edit' && !oldTicket) {
    notice.value = 'DEV 草稿已不存在或已非 UnConfirm，請重新查詢。'
    return
  }
  if (oldTicket) releaseBookings(oldTicket)
  const ticket: Ticket = {
    scrInNo: draft.value.scrInNo.trim(), toInventoryNo: draft.value.toInventoryNo, fromInventoryNo: draft.value.fromInventoryNo,
    description: draft.value.description.trim(), creator: oldTicket?.creator ?? 'DEV01', createDate: oldTicket?.createDate ?? new Date().toLocaleString('zh-TW', { hour12: false }),
    inputDate: oldTicket?.inputDate ?? '', state: 0, turnType: draft.value.turnType, sourceKeys: [...draftSelectedKeys.value],
  }
  if (oldTicket) Object.assign(oldTicket, ticket)
  else state.value.tickets.push(ticket)
  applyBookings(ticket)
  selectedTicketNo.value = ticket.scrInNo
  selectedTicketNos.value = []
  dialogOpen.value = false
  notice.value = (oldTicket ? 'DEV 已更新草稿 ' : 'DEV 已新增草稿 ') + ticket.scrInNo + '；目前只維護單據與來源預約。'
}
function setSourceBooking(key: string, turnType: TurnType, bookingFlag: boolean): void {
  if (turnType === 0) {
    const row = state.value.scrapSources.find((item) => scrapKey(item) === key)
    if (row) row.bookingFlag = bookingFlag
  } else if (turnType === 1) {
    const row = state.value.componentSources.find((item) => componentKey(item) === key)
    if (row) row.bookingFlag = bookingFlag
  } else {
    const row = state.value.failBinSources.find((item) => failBinKey(item) === key)
    if (row) row.bookingFlag = bookingFlag
  }
}
function releaseBookings(ticket: Ticket): void { ticket.sourceKeys.forEach((key) => setSourceBooking(key, ticket.turnType, false)) }
function applyBookings(ticket: Ticket): void { ticket.sourceKeys.forEach((key) => setSourceBooking(key, ticket.turnType, true)) }
function requestDelete(): void {
  if (!selectedTicketNos.value.length && activeTicket.value) selectedTicketNos.value = [activeTicket.value.scrInNo]
  if (!selectedTicketNos.value.length) { notice.value = '請先選取要作廢的 UnConfirm 單據。'; return }
  deleteOpen.value = true
}
function confirmDelete(): void {
  const chosen = new Set(selectedTicketNos.value)
  const rows = state.value.tickets.filter((ticket) => chosen.has(ticket.scrInNo) && ticket.state === 0)
  for (const ticket of rows) {
    releaseBookings(ticket)
    ticket.state = -1
  }
  deleteOpen.value = false
  selectedTicketNos.value = []
  notice.value = 'DEV 已將 ' + rows.length + ' 張單據標記為 State=-1，保留單據明細並解除來源 BookingFlag。'
}
function resetDevData(): void {
  state.value = createSeed()
  search.value = ''
  selectedTicketNo.value = ''
  selectedTicketNos.value = []
  notice.value = 'DEV 範例資料已重設。'
}
</script>

<template>
  <main class="scr-in-page" aria-label="不良品入庫 DEV" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="heading">
      <div><p class="eyebrow">INVENTORY · IV602</p><h1>不良品入庫</h1><p class="subtitle">從 Type=6 WIP 線邊庫選取 Scrap Lot、Component Scrap 或 Fail Bin，建立 Type=3 報廢倉草稿</p></div>
      <div class="heading-actions"><span class="dev-pill">DEV MOCK</span><button type="button" @click="emit('close')">關閉</button></div>
    </header>

    <section class="toolbar card" aria-label="不良品入庫單查詢與操作">
      <label>SCRInNo／目標庫房
        <input v-model="search" type="search" placeholder="輸入單號或 InventoryNo">
      </label>
      <button type="button" class="primary" @click="openAdd">新增</button>
      <button type="button" :disabled="!activeTicket" @click="openEdit">修改</button>
      <button type="button" class="danger" :disabled="!pendingTickets.length" @click="requestDelete">刪除／作廢</button>
      <button type="button" @click="resetDevData">重設 DEV</button>
    </section>
    <p class="notice" role="status">{{ notice }}</p>

    <section class="summary-grid" aria-label="單據統計">
      <article class="metric"><span>待處理草稿</span><strong>{{ pendingCount }}</strong><small>Load State=0</small></article>
      <article class="metric"><span>查詢結果</span><strong>{{ pendingTickets.length }}</strong><small>依 SCRInNo 排序</small></article>
      <article class="metric"><span>目前單號</span><strong class="metric-code">{{ activeTicket?.scrInNo ?? '—' }}</strong><small>{{ activeTicket ? typeLabel(activeTicket.turnType) : '請新增或選取單據' }}</small></article>
      <article class="metric"><span>IV601 確認</span><strong class="metric-text">獨立入口</strong><small>本頁不執行庫存過帳</small></article>
    </section>

    <section class="card">
      <div class="card-heading"><div><h2>SCR In 單頭</h2><p>舊 Form 只載 State=0，並將 Basis.InputDate 顯示為 ConfirmDate。</p></div><span class="key-note">{{ pendingTickets.length }} 張可編輯草稿</span></div>
      <div class="table-wrap"><table>
        <thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選待處理單據" :checked="pendingTickets.length > 0 && selectedTicketNos.length === pendingTickets.length" @change="toggleAllTickets(($event.target as HTMLInputElement).checked)"></th><th>SCRInNo</th><th>To Inventory</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>From Inventory</th><th>Creator</th><th>CreateDate</th></tr></thead>
        <tbody>
          <tr v-for="ticket in pendingTickets" :key="ticket.scrInNo" :class="{ active: activeTicket?.scrInNo === ticket.scrInNo }" @click="selectTicket(ticket)">
            <td class="check-cell"><input type="checkbox" :aria-label="'選取 ' + ticket.scrInNo" :checked="selectedTicketNos.includes(ticket.scrInNo)" @click.stop @change="toggleTicket(ticket, ($event.target as HTMLInputElement).checked)"></td>
            <td class="mono">{{ ticket.scrInNo }}</td><td>{{ ticket.toInventoryNo }}</td><td><span class="state-pill">{{ stateLabel(ticket.state) }}</span></td><td>{{ ticket.inputDate || '—' }}</td><td>{{ ticket.description || '—' }}</td><td>{{ ticket.fromInventoryNo }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td>
          </tr>
          <tr v-if="!pendingTickets.length"><td colspan="9" class="empty">沒有符合條件的 UnConfirm 單據</td></tr>
        </tbody>
      </table></div>
    </section>

    <section class="card detail-card">
      <div class="card-heading"><div><h2>入庫明細</h2><p>{{ activeTicket ? activeTicket.scrInNo + ' · ' + typeLabel(activeTicket.turnType) : '尚未選取單據' }}</p></div><span v-if="activeTicket" class="key-note">{{ activeTicket.fromInventoryNo }} → {{ activeTicket.toInventoryNo }}</span></div>
      <div v-if="activeTicket" class="detail-summary"><span>來源庫房 <b>{{ activeTicket.fromInventoryNo }}</b></span><span>目標庫房 <b>{{ activeTicket.toInventoryNo }}</b></span><span>來源類型 <b>{{ typeLabel(activeTicket.turnType) }}</b></span><span>說明 <b>{{ activeTicket.description || '—' }}</b></span></div>
      <template v-if="activeTicket && activeTicket.turnType === 1">
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th>ComponentNo</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="row in ticketComponents" :key="componentKey(row)"><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td class="mono">{{ row.componentNo }}</td><td class="num">{{ formatQty(row.goodQty) }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr>
          <tr v-if="!ticketComponents.length"><td colspan="7" class="empty">沒有 Component 明細</td></tr>
        </tbody></table></div>
        <p class="subnote">舊 Core 會依 LotSerial 彙總 Detail，並另存每個 Component；編輯時來源明細仍以 Component key 選取。</p>
      </template>
      <template v-else-if="activeTicket && activeTicket.turnType === 2">
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th v-for="bin in bins" :key="bin.binNo" class="num">{{ bin.binNo }}</th><th class="num">ScrapQty 合計</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="row in ticketFailBins" :key="failBinKey(row)"><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td v-for="bin in bins" :key="bin.binNo" class="num">{{ formatQty(row.bins[bin.binNo] ?? 0) }}</td><td class="num">{{ formatQty(bins.reduce((sum, bin) => sum + (row.bins[bin.binNo] ?? 0), 0)) }}</td><td>{{ row.unitNo }}</td></tr>
          <tr v-if="!ticketFailBins.length"><td :colspan="6 + bins.length" class="empty">沒有 Fail Bin 明細</td></tr>
        </tbody></table></div>
        <p class="subnote">欄位依 DEV SYS Bin BinOrder 排列；數量欄只作互動示例。</p>
      </template>
      <template v-else-if="activeTicket">
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="row in ticketDetails" :key="row.lotSerial"><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td class="num">{{ formatQty(row.lossQty) }}</td><td>{{ row.unitNo }}</td></tr>
          <tr v-if="!ticketDetails.length"><td colspan="6" class="empty">沒有 Lot 明細</td></tr>
        </tbody></table></div>
      </template>
      <p v-else class="empty detail-empty">選取一張待處理單據以檢視明細。</p>
    </section>

    <section class="handoff-note"><strong>確認／過帳分工</strong><span>本頁新增、修改、作廢草稿並模擬來源 BookingFlag；要排入 Source=40 交易及執行後續入庫，請使用 IV601「報廢倉庫房資料」的 SCR In 確認入口。</span></section>

    <div v-if="dialogOpen" class="overlay" role="presentation" @click.self="dialogOpen = false">
      <section class="dialog editor" role="dialog" aria-modal="true" :aria-label="mode === 'add' ? '新增不良品入庫單' : '修改不良品入庫單'">
        <header class="dialog-heading"><div><p class="eyebrow">SCR IN DRAFT</p><h2>{{ mode === 'add' ? '新增不良品入庫單' : '修改不良品入庫單' }}</h2></div><button type="button" aria-label="關閉" @click="dialogOpen = false">×</button></header>
        <div class="form-grid">
          <label>SCRInNo <span class="reserved-number">{{ draft.scrInNo }} <small>DEV 取號已保留</small></span></label>
          <label>目標報廢庫房（Type=3）
            <select v-model="draft.toInventoryNo" :disabled="mode === 'edit'"><option v-for="item in toInventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.inventoryName }}</option></select>
          </label>
          <label>來源 WIP 半成品庫（Type=6）
            <select :value="draft.fromInventoryNo" :disabled="mode === 'edit'" @change="setFromInventory(($event.target as HTMLSelectElement).value)"><option v-for="item in fromInventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.inventoryName }}</option></select>
          </label>
          <label>來源類型
            <select :value="draft.turnType" :disabled="mode === 'edit'" @change="setTurnType(Number(($event.target as HTMLSelectElement).value))"><option :value="0">0 · 線邊 WIP Scrap Lot</option><option :value="1">1 · WIP Component Scrap</option><option :value="2">2 · WIP Fail Bin</option></select>
          </label>
          <label class="wide">Description <input v-model="draft.description" :readonly="mode === 'edit'" maxlength="50" placeholder="輸入入庫說明"></label>
        </div>
        <div class="source-heading"><div><h3>可選來源</h3><p>{{ typeLabel(draft.turnType) }} · BookingFlag=0；修改時包含本單已預約列</p></div><label class="inline-check"><input v-model="showSelectedOnly" type="checkbox">只顯示已選取</label></div>
        <div class="table-wrap source-table">
          <table v-if="draft.turnType === 0"><thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選來源" :checked="sourceRows.length > 0 && sourceRows.every((row) => draftSelectedKeys.includes(sourceKey(row, 0)))" @change="toggleAllSources(($event.target as HTMLInputElement).checked)"></th><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>UnitNo</th><th>BookingFlag</th></tr></thead><tbody>
            <tr v-for="row in sourceRows as ScrapSource[]" :key="scrapKey(row)"><td class="check-cell"><input type="checkbox" :checked="draftSelectedKeys.includes(scrapKey(row))" @change="toggleSource(scrapKey(row), ($event.target as HTMLInputElement).checked)"></td><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td class="num">{{ formatQty(row.lossQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.bookingFlag ? '1' : '0' }}</td></tr>
            <tr v-if="!sourceRows.length"><td colspan="8" class="empty">沒有可選來源列</td></tr>
          </tbody></table>
          <table v-else-if="draft.turnType === 1"><thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選來源" :checked="sourceRows.length > 0 && sourceRows.every((row) => draftSelectedKeys.includes(sourceKey(row, 1)))" @change="toggleAllSources(($event.target as HTMLInputElement).checked)"></th><th>LotNo</th><th>LotSerial</th><th>ComponentNo</th><th>OPNo</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th>BookingFlag</th></tr></thead><tbody>
            <tr v-for="row in sourceRows as ComponentSource[]" :key="componentKey(row)"><td class="check-cell"><input type="checkbox" :checked="draftSelectedKeys.includes(componentKey(row))" @change="toggleSource(componentKey(row), ($event.target as HTMLInputElement).checked)"></td><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td class="mono">{{ row.componentNo }}</td><td>{{ row.opNo }}</td><td class="num">{{ formatQty(row.goodQty) }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td>{{ row.bookingFlag ? '1' : '0' }}</td></tr>
            <tr v-if="!sourceRows.length"><td colspan="8" class="empty">沒有可選來源列</td></tr>
          </tbody></table>
          <table v-else><thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選來源" :checked="sourceRows.length > 0 && sourceRows.every((row) => draftSelectedKeys.includes(sourceKey(row, 2)))" @change="toggleAllSources(($event.target as HTMLInputElement).checked)"></th><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th v-for="bin in bins" :key="bin.binNo" class="num">{{ bin.binNo }}</th><th>BookingFlag</th></tr></thead><tbody>
            <tr v-for="row in sourceRows as FailBinSource[]" :key="failBinKey(row)"><td class="check-cell"><input type="checkbox" :checked="draftSelectedKeys.includes(failBinKey(row))" @change="toggleSource(failBinKey(row), ($event.target as HTMLInputElement).checked)"></td><td>{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td v-for="bin in bins" :key="bin.binNo" class="num">{{ formatQty(row.bins[bin.binNo] ?? 0) }}</td><td>{{ row.bookingFlag ? '1' : '0' }}</td></tr>
            <tr v-if="!sourceRows.length"><td :colspan="5 + bins.length" class="empty">沒有可選來源列</td></tr>
          </tbody></table>
        </div>
        <footer class="dialog-footer"><span>已選 {{ selectedSourceCount }} 列</span><div><button type="button" @click="dialogOpen = false">取消</button><button type="button" class="primary" @click="saveDraft">{{ mode === 'add' ? '新增草稿' : '套用修改' }}</button></div></footer>
      </section>
    </div>

    <div v-if="deleteOpen" class="overlay" role="presentation" @click.self="deleteOpen = false">
      <section class="dialog confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="void-title">
        <h2 id="void-title">作廢選取草稿？</h2><p>將 {{ selectedTicketNos.length || 1 }} 張 UnConfirm 單據標記為 State=-1，保留 detail 歷史並解除來源 BookingFlag。</p>
        <footer class="dialog-footer"><span>DEV 本機模擬</span><div><button type="button" @click="deleteOpen = false">取消</button><button type="button" class="danger" @click="confirmDelete">確認作廢</button></div></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.scr-in-page { min-height: 100%; padding: 18px; box-sizing: border-box; color: #292b30; background: #f2f4f7; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.heading,.card-heading,.dialog-heading,.source-heading,.dialog-footer,.heading-actions,.toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.heading { margin: 0 auto 16px; max-width: 1500px; }
.eyebrow { margin: 0 0 3px; color: #697386; font-size: 11px; font-weight: 700; letter-spacing: .1em; }
h1,h2,h3,p { margin-top: 0; }
h1 { margin-bottom: 2px; font-size: 23px; }
.subtitle,.card-heading p,.source-heading p,.subnote { margin: 0; color: #697386; font-size: 12px; }
.heading-actions { justify-content: flex-end; }
button,input,select { font: inherit; }
button { min-height: 32px; padding: 5px 11px; border: 1px solid #c4cad4; border-radius: 5px; color: #303642; background: white; cursor: pointer; }
button:hover:not(:disabled) { border-color: #7184a0; background: #f8faff; }
button:disabled { opacity: .45; cursor: not-allowed; }
button.primary { border-color: #245a9a; color: white; background: #245a9a; }
button.danger { border-color: #bb4545; color: #a52d32; }
button.danger:hover:not(:disabled) { color: white; background: #bb4545; }
.dev-pill,.state-pill { display: inline-flex; align-items: center; border-radius: 999px; padding: 4px 9px; color: #15634d; background: #dff4ea; font-size: 11px; font-weight: 700; }
.state-pill { padding: 3px 8px; color: #8b5a14; background: #fff0d5; }
.card { max-width: 1500px; margin: 0 auto 13px; padding: 14px; border: 1px solid #dfe3ea; border-radius: 8px; background: white; box-shadow: 0 2px 7px #1c2b4410; }
.toolbar { justify-content: flex-start; flex-wrap: wrap; }
.toolbar label { display: flex; align-items: center; gap: 9px; margin-right: auto; color: #555d6b; font-weight: 600; }
.toolbar input { width: 280px; min-height: 31px; box-sizing: border-box; padding: 5px 9px; border: 1px solid #cbd1dc; border-radius: 5px; }
.notice { max-width: 1500px; margin: 0 auto 12px; padding: 8px 11px; border: 1px solid #d7e3f5; border-radius: 6px; color: #355579; background: #edf4ff; }
.summary-grid { display: grid; grid-template-columns: repeat(4,minmax(0,1fr)); gap: 10px; max-width: 1500px; margin: 0 auto 13px; }
.metric { display: flex; min-height: 82px; flex-direction: column; justify-content: center; padding: 11px 14px; border: 1px solid #dfe3ea; border-radius: 8px; background: white; }
.metric span,.metric small { color: #697386; font-size: 11px; }
.metric strong { margin: 3px 0 1px; color: #28334a; font-size: 22px; }
.metric .metric-code { font-size: 15px; }
.metric .metric-text { font-size: 17px; }
.card-heading { margin-bottom: 11px; }
.card-heading h2,.dialog-heading h2 { margin: 0 0 2px; font-size: 16px; }
.key-note { color: #56647a; font-size: 11px; font-family: Consolas, monospace; }
.table-wrap { width: 100%; overflow: auto; border: 1px solid #e5e8ee; border-radius: 5px; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; }
th,td { height: 34px; padding: 5px 9px; border-bottom: 1px solid #e9ecf1; text-align: left; }
th { position: sticky; top: 0; z-index: 1; color: #50596b; background: #f3f5f8; font-size: 11px; font-weight: 700; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f7faff; }
tbody tr.active { background: #eaf2ff; }
tbody tr:last-child td { border-bottom: 0; }
.check-cell { width: 32px; text-align: center; }
.check-cell input,.inline-check input { accent-color: #245a9a; }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.mono { font-family: Consolas, "Courier New", monospace; }
.empty { padding: 20px; color: #818897; text-align: center; cursor: default; }
.detail-card { min-height: 180px; }
.detail-summary { display: flex; flex-wrap: wrap; gap: 9px 22px; margin-bottom: 12px; padding: 9px 11px; border-radius: 5px; color: #626b7b; background: #f6f7f9; }
.detail-summary b { color: #303948; font-weight: 600; }
.subnote { margin-top: 8px; }
.detail-empty { margin-bottom: 0; }
.handoff-note { display: flex; max-width: 1500px; gap: 12px; margin: 0 auto; padding: 11px 14px; border-left: 3px solid #6f87aa; border-radius: 4px; color: #516078; background: #e9edf4; }
.handoff-note strong { flex: 0 0 auto; color: #35445c; }
.overlay { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 20px; background: #18223580; }
.dialog { width: min(860px,96vw); max-height: 92vh; overflow: auto; box-sizing: border-box; padding: 18px; border-radius: 9px; background: white; box-shadow: 0 18px 55px #0004; }
.dialog.editor { width: min(1100px,96vw); }
.dialog-heading { margin-bottom: 15px; }
.dialog-heading button { min-width: 33px; padding: 2px; font-size: 22px; }
.form-grid { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: 11px 14px; margin-bottom: 17px; }
.form-grid label { display: grid; gap: 5px; color: #555d6b; font-size: 11px; font-weight: 700; }
.form-grid input,.form-grid select { width: 100%; min-height: 34px; box-sizing: border-box; padding: 5px 9px; border: 1px solid #cbd1dc; border-radius: 5px; color: #303642; background: white; font-size: 13px; font-weight: 400; }
.form-grid select:disabled { color: #697386; background: #f2f4f7; }
.form-grid .wide { grid-column: 1 / -1; }
.reserved-number { display: flex; min-height: 34px; align-items: center; justify-content: space-between; padding: 0 9px; border: 1px solid #dbe2ed; border-radius: 5px; color: #33445d; background: #f5f7fa; font: 13px Consolas,monospace; }
.reserved-number small { color: #718096; font: 10px "Segoe UI","Microsoft JhengHei",sans-serif; }
.source-heading { margin: 0 0 8px; }
.source-heading h3 { margin: 0 0 2px; font-size: 14px; }
.inline-check { display: flex; align-items: center; gap: 6px; color: #5d6675; font-size: 11px; white-space: nowrap; }
.source-table { max-height: 300px; }
.dialog-footer { margin-top: 14px; color: #707989; font-size: 11px; }
.dialog-footer div { display: flex; gap: 8px; }
.confirm-dialog { width: min(540px,96vw); }
.confirm-dialog h2 { margin: 0 0 8px; font-size: 18px; }
.confirm-dialog p { color: #697386; }
@media (max-width: 850px) { .heading { align-items: flex-start; } .summary-grid { grid-template-columns: repeat(2,minmax(0,1fr)); } .toolbar label { width: 100%; margin-right: 0; } .toolbar input { flex: 1; min-width: 100px; } }
@media (max-width: 560px) { .scr-in-page { padding: 10px; } .heading { gap: 8px; } h1 { font-size: 19px; } .subtitle { max-width: 260px; } .summary-grid { gap: 7px; } .metric { padding: 9px; } .metric strong { font-size: 18px; } .form-grid { grid-template-columns: 1fr; } .form-grid .wide { grid-column: auto; } .detail-summary { display: grid; } .handoff-note { flex-direction: column; gap: 4px; } }
</style>

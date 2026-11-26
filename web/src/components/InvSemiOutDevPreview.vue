<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type DestinationType = 2 | 3 | 4 | 5
type DialogMode = 'add' | 'edit' | 'copy' | null
interface ComponentRow { componentNo: string; goodQty: number; scrapQty: number; unitNo: string }
interface SourceLot {
  inventoryNo: string; lotNo: string; productNo: string; productName: string; locatorNo: string
  availableQty: number; unitNo: string; hasLevel: boolean; components: ComponentRow[]; reservedBy: string
}
interface DetailRow {
  rowId: string; lotNo: string; productNo: string; productName: string; locatorNo: string
  qty: number; unitNo: string; putInPlaceType: DestinationType; putInPlaceNo: string
}
interface Ticket {
  semiOutNo: string; inventoryNo: string; state: number; description: string; creator: string
  createDate: string; reviser: string; reviseDate: string; dataStamp: number; details: DetailRow[]
}
interface DevState { serial: number; tickets: Ticket[]; sourceLots: SourceLot[] }
interface HeaderDraft { semiOutNo: string; inventoryNo: string; description: string; dataStamp: number }
interface DetailDraft { rowId: string; lotNo: string; qty: number; putInPlaceType: DestinationType; putInPlaceNo: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories = [
  { inventoryNo: 'SEMI-DEV-01', inventoryName: '半成品庫 A' },
  { inventoryNo: 'SEMI-DEV-02', inventoryName: '半成品庫 B' },
]
const destinationTypes: { value: DestinationType; label: string }[] = [
  { value: 2, label: 'WIP 庫房' }, { value: 3, label: '工單' },
  { value: 4, label: '非生產性' }, { value: 5, label: 'Material Return' },
]

function seedState(): DevState {
  const sourceLots: SourceLot[] = [
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'SL-26001', productNo: 'PRD-CONTROL-10', productName: '控制板總成', locatorNo: 'S-A-01', availableQty: 12, unitNo: 'EA', hasLevel: false, components: [{ componentNo: 'CMP-PCB-01', goodQty: 11, scrapQty: 1, unitNo: 'EA' }, { componentNo: 'CMP-CONN-02', goodQty: 12, scrapQty: 0, unitNo: 'EA' }], reservedBy: '' },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'SL-26002', productNo: 'PRD-SENSOR-04', productName: '感測器模組', locatorNo: 'S-A-02', availableQty: 6, unitNo: 'EA', hasLevel: false, components: [], reservedBy: 'SO-DEV-260001' },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'SL-26003', productNo: 'PRD-PANEL-07', productName: '顯示面板', locatorNo: 'S-A-03', availableQty: 10, unitNo: 'EA', hasLevel: true, components: [{ componentNo: 'CMP-PANEL-01', goodQty: 10, scrapQty: 0, unitNo: 'EA' }], reservedBy: '' },
    { inventoryNo: 'SEMI-DEV-02', lotNo: 'SL-26011', productNo: 'PRD-VALVE-06', productName: '閥件模組', locatorNo: 'S-B-01', availableQty: 7, unitNo: 'EA', hasLevel: false, components: [], reservedBy: 'SO-DEV-260002' },
    { inventoryNo: 'SEMI-DEV-02', lotNo: 'SL-26012', productNo: 'PRD-VALVE-06', productName: '閥件模組', locatorNo: 'S-B-02', availableQty: 3, unitNo: 'EA', hasLevel: false, components: [{ componentNo: 'CMP-VALVE-01', goodQty: 3, scrapQty: 0, unitNo: 'EA' }], reservedBy: '' },
  ]
  return {
    serial: 3,
    sourceLots,
    tickets: [
      { semiOutNo: 'SO-DEV-260001', inventoryNo: 'SEMI-DEV-01', state: 0, description: '工單領料待確認', creator: 'DEV001', createDate: '2026-11-24 09:35:00', reviser: '', reviseDate: '', dataStamp: 1, details: [{ rowId: 'd-1', lotNo: 'SL-26002', productNo: 'PRD-SENSOR-04', productName: '感測器模組', locatorNo: 'S-A-02', qty: 6, unitNo: 'EA', putInPlaceType: 3, putInPlaceNo: 'MO-DEV-26001' }] },
      { semiOutNo: 'SO-DEV-260002', inventoryNo: 'SEMI-DEV-02', state: 0, description: 'DEV 非生產領用', creator: 'DEV002', createDate: '2026-11-24 14:10:00', reviser: '', reviseDate: '', dataStamp: 1, details: [{ rowId: 'd-2', lotNo: 'SL-26011', productNo: 'PRD-VALVE-06', productName: '閥件模組', locatorNo: 'S-B-01', qty: 4, unitNo: 'EA', putInPlaceType: 4, putInPlaceNo: 'N/A' }] },
      { semiOutNo: 'SO-DEV-259999', inventoryNo: 'SEMI-DEV-01', state: 2, description: '已確認範例（依 IV403 查詢條件不顯示）', creator: 'DEV001', createDate: '2026-11-20 08:00:00', reviser: 'DEV002', reviseDate: '2026-11-20 10:00:00', dataStamp: 2, details: [] },
    ],
  }
}

const state = ref<DevState>(seedState())
const queryNo = ref('')
const activeNo = ref('')
const selectedDetailId = ref('')
const headerMode = ref<DialogMode>(null)
const detailMode = ref<DialogMode>(null)
const headerDraft = ref<HeaderDraft>(blankHeader())
const detailDraft = ref<DetailDraft>(blankDetail())
const formError = ref('')
const notice = ref('DEV 假資料：出庫單只在本頁模擬，不呼叫 MES 或正式 API。')
const printOpen = ref(false)
const deletePending = ref<'header' | 'detail' | null>(null)

const visibleTickets = computed(() => {
  const query = queryNo.value.trim().toLocaleUpperCase()
  return state.value.tickets.filter((ticket) => ticket.state === 0 && (!query || ticket.semiOutNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.semiOutNo.localeCompare(b.semiOutNo))
})
const activeTicket = computed(() => visibleTickets.value.find((ticket) => ticket.semiOutNo === activeNo.value) ?? visibleTickets.value[0] ?? null)
const activeDetails = computed(() => activeTicket.value?.details.slice().sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo)) ?? [])
const activeDetail = computed(() => activeDetails.value.find((row) => row.rowId === selectedDetailId.value) ?? activeDetails.value[0] ?? null)
const selectedSourceLot = computed(() => state.value.sourceLots.find((lot) => lot.lotNo === activeDetail.value?.lotNo && lot.inventoryNo === activeTicket.value?.inventoryNo) ?? null)
const draftSourceLots = computed(() => state.value.sourceLots.filter((lot) => lot.inventoryNo === activeTicket.value?.inventoryNo && lot.availableQty > 0 && (!lot.reservedBy || lot.reservedBy === activeTicket.value?.semiOutNo || lot.lotNo === detailDraft.value.lotNo)))
const draftLot = computed(() => state.value.sourceLots.find((lot) => lot.inventoryNo === activeTicket.value?.inventoryNo && lot.lotNo === detailDraft.value.lotNo) ?? null)
const ticketTotal = computed(() => activeDetails.value.reduce((total, row) => total + row.qty, 0))
const displayTicket = computed(() => activeTicket.value)
const destinationLabel = (value: number) => destinationTypes.find((item) => item.value === value)?.label ?? '—'
const inventoryName = (value: string) => inventories.find((row) => row.inventoryNo === value)?.inventoryName ?? value
const stateLabel = (value: number) => value === 0 ? 'UnConfirm' : value === 1 ? 'Handling' : value === 2 ? 'Confirm' : value === 3 ? 'Fail' : 'Other'
const formatQty = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)

watch(visibleTickets, (tickets) => {
  if (!tickets.some((ticket) => ticket.semiOutNo === activeNo.value)) activeNo.value = tickets[0]?.semiOutNo ?? ''
}, { immediate: true })
watch(activeDetails, (rows) => {
  if (!rows.some((row) => row.rowId === selectedDetailId.value)) selectedDetailId.value = rows[0]?.rowId ?? ''
}, { immediate: true })
watch(draftLot, (lot) => {
  if (!lot || detailMode.value === 'edit') return
  detailDraft.value.qty = lot.hasLevel || lot.components.length > 0 ? lot.availableQty : Math.min(detailDraft.value.qty || lot.availableQty, lot.availableQty)
})

function blankHeader(): HeaderDraft { return { semiOutNo: '', inventoryNo: '', description: '', dataStamp: 0 } }
function blankDetail(): DetailDraft { return { rowId: '', lotNo: '', qty: 0, putInPlaceType: 2, putInPlaceNo: '' } }
function nextTicketNo(): string { return `SO-DEV-26${String(state.value.serial).padStart(4, '0')}` }
function selectTicket(ticket: Ticket): void { activeNo.value = ticket.semiOutNo }
function selectDetail(row: DetailRow): void { selectedDetailId.value = row.rowId }
function runQuery(): void { notice.value = `查詢完成：僅顯示 State=0 的半成品出庫草稿（${visibleTickets.value.length} 筆）。` }

function openHeader(mode: Exclude<DialogMode, null>): void {
  formError.value = ''
  const ticket = activeTicket.value
  if (mode !== 'add' && !ticket) return
  if (mode === 'edit' && ticket?.state !== 0) return
  if (mode === 'add') headerDraft.value = { ...blankHeader(), semiOutNo: nextTicketNo(), inventoryNo: inventories[0].inventoryNo }
  else if (ticket) headerDraft.value = { semiOutNo: mode === 'copy' ? nextTicketNo() : ticket.semiOutNo, inventoryNo: ticket.inventoryNo, description: mode === 'copy' ? `${ticket.description} (Copy)` : ticket.description, dataStamp: ticket.dataStamp }
  headerMode.value = mode
}

function saveHeader(): void {
  formError.value = ''
  const draft = headerDraft.value
  const semiOutNo = draft.semiOutNo.trim().toLocaleUpperCase()
  if (!semiOutNo) { formError.value = 'SEMIOutNo 不可空白。'; return }
  if (!draft.inventoryNo) { formError.value = '請選擇半成品庫房。'; return }
  const existing = state.value.tickets.find((ticket) => ticket.semiOutNo === semiOutNo)
  if (headerMode.value !== 'edit' && existing) { formError.value = '單號重複，請重新產生。'; return }
  if (headerMode.value === 'edit') {
    const target = state.value.tickets.find((ticket) => ticket.semiOutNo === draft.semiOutNo)
    if (!target || target.state !== 0 || target.dataStamp !== draft.dataStamp) { formError.value = '單據狀態或版本已變更，請重新查詢後再編輯。'; return }
    target.description = draft.description.trim()
    target.dataStamp += 1
    target.reviser = 'DEV001'
    target.reviseDate = new Date().toLocaleString('sv-SE').replace('T', ' ')
    activeNo.value = target.semiOutNo
    notice.value = `已模擬修改單頭 ${target.semiOutNo}。`
  } else {
    if (semiOutNo === nextTicketNo()) state.value.serial += 1
    const ticket: Ticket = { semiOutNo, inventoryNo: draft.inventoryNo, state: 0, description: draft.description.trim(), creator: 'DEV001', createDate: new Date().toLocaleString('sv-SE').replace('T', ' '), reviser: '', reviseDate: '', dataStamp: 1, details: [] }
    state.value.tickets.push(ticket)
    activeNo.value = semiOutNo
    notice.value = `已模擬新增單頭 ${semiOutNo}；明細尚未建立。`
  }
  headerMode.value = null
}

function deleteHeader(): void {
  const ticket = activeTicket.value
  if (!ticket || ticket.state !== 0) return
  for (const lot of state.value.sourceLots) if (lot.reservedBy === ticket.semiOutNo) lot.reservedBy = ''
  state.value.tickets = state.value.tickets.filter((row) => row.semiOutNo !== ticket.semiOutNo)
  activeNo.value = ''
  notice.value = `已模擬刪除草稿 ${ticket.semiOutNo}，並釋放假資料預約。`
}

function openDetail(mode: Exclude<DialogMode, null>): void {
  const ticket = activeTicket.value
  if (!ticket || ticket.state !== 0) return
  formError.value = ''
  const row = activeDetail.value
  if (mode !== 'add' && !row) return
  if (mode === 'add') detailDraft.value = { ...blankDetail(), qty: draftSourceLots.value[0]?.availableQty ?? 0, lotNo: draftSourceLots.value[0]?.lotNo ?? '' }
  else if (row) detailDraft.value = { rowId: mode === 'copy' ? '' : row.rowId, lotNo: row.lotNo, qty: row.qty, putInPlaceType: row.putInPlaceType, putInPlaceNo: row.putInPlaceNo }
  detailMode.value = mode
}

function saveDetail(): void {
  const ticket = activeTicket.value
  const lot = draftLot.value
  formError.value = ''
  if (!ticket || ticket.state !== 0) { formError.value = '只有未確認單據可維護明細。'; return }
  if (!lot) { formError.value = '請選擇來源半成品 Lot。'; return }
  if (!Number.isFinite(detailDraft.value.qty) || detailDraft.value.qty <= 0) { formError.value = '數量必須大於 0。'; return }
  if (detailDraft.value.qty > lot.availableQty) { formError.value = `數量不可大於庫存量 ${formatQty(lot.availableQty)}。`; return }
  if ((lot.hasLevel || lot.components.length > 0) && detailDraft.value.qty !== lot.availableQty) { formError.value = '此 Lot 有層級或 Component，出庫數量需為整批庫存量。'; return }
  if ([2, 3].includes(detailDraft.value.putInPlaceType) && !detailDraft.value.putInPlaceNo.trim()) { formError.value = '請輸入投料目的編號。'; return }
  if (detailDraft.value.putInPlaceType === 3 && !detailDraft.value.putInPlaceNo.toLocaleUpperCase().startsWith('MO-')) { formError.value = '工單類型請輸入 MO 編號。'; return }
  const original = ticket.details.find((row) => row.rowId === detailDraft.value.rowId)
  if (detailMode.value !== 'edit' && ticket.details.some((row) => row.lotNo === lot.lotNo && row.locatorNo === lot.locatorNo)) { formError.value = '同一來源 Lot／Locator 已在此單據，無法重複加入。'; return }
  if (lot.reservedBy && lot.reservedBy !== ticket.semiOutNo) { formError.value = `此 Lot 已預約在 ${lot.reservedBy}。`; return }
  const destinationNo = [4, 5].includes(detailDraft.value.putInPlaceType) ? 'N/A' : detailDraft.value.putInPlaceNo.trim().toLocaleUpperCase()
  const saved: DetailRow = {
    rowId: original?.rowId ?? `d-${Date.now()}-${Math.random().toString(16).slice(2, 6)}`,
    lotNo: lot.lotNo, productNo: lot.productNo, productName: lot.productName, locatorNo: lot.locatorNo,
    qty: detailDraft.value.qty, unitNo: lot.unitNo, putInPlaceType: detailDraft.value.putInPlaceType, putInPlaceNo: destinationNo,
  }
  if (original) ticket.details = ticket.details.map((row) => row.rowId === original.rowId ? saved : row)
  else ticket.details.push(saved)
  lot.reservedBy = ticket.semiOutNo
  ticket.dataStamp += 1
  selectedDetailId.value = saved.rowId
  detailMode.value = null
  notice.value = `已模擬${original ? '修改' : '新增'}明細 ${lot.lotNo}。${lot.components.length ? '此 Lot 的 Component 明細整批帶入。' : ''}`
}

function deleteDetail(): void {
  const ticket = activeTicket.value
  const row = activeDetail.value
  if (!ticket || !row || ticket.state !== 0) return
  ticket.details = ticket.details.filter((item) => item.rowId !== row.rowId)
  const lot = state.value.sourceLots.find((item) => item.lotNo === row.lotNo && item.inventoryNo === ticket.inventoryNo)
  if (lot && !ticket.details.some((item) => item.lotNo === row.lotNo)) lot.reservedBy = ''
  ticket.dataStamp += 1
  notice.value = `已模擬刪除明細 ${row.lotNo}，並更新本機假資料預約。`
}

function confirmDelete(): void {
  const pending = deletePending.value
  deletePending.value = null
  if (pending === 'header') deleteHeader()
  if (pending === 'detail') deleteDetail()
}

function openPrint(): void {
  if (!activeTicket.value) return
  if (!activeTicket.value.details.length) { notice.value = '此單據沒有明細，依舊程式規則不能列印。'; return }
  printOpen.value = true
}
function closeDialogs(): void { headerMode.value = null; detailMode.value = null; printOpen.value = false; deletePending.value = null; formError.value = '' }
</script>

<template>
  <section class="semi-out" aria-label="開立半成品出庫單 DEV 預覽">
    <header class="semi-out__topbar">
      <div><div class="semi-out__eyebrow">INVENTORY / SEMI INV</div><h1>開立半成品出庫單 <span>{{ previewLabel }}</span></h1></div>
      <button class="semi-out__close" type="button" @click="emit('close')">Close (X)</button>
    </header>
    <div class="semi-out__toolbar">
      <label>SEMIOutNo <input v-model="queryNo" aria-label="SEMIOutNo 查詢" @keyup.enter="runQuery" /></label>
      <button type="button" class="semi-out__button" @click="runQuery">Find</button>
      <i></i>
      <button type="button" class="semi-out__button" @click="openHeader('add')">ADD</button>
      <button type="button" class="semi-out__button" :disabled="!activeTicket" @click="openHeader('edit')">EDIT</button>
      <button type="button" class="semi-out__button semi-out__button--danger" :disabled="!activeTicket" @click="deletePending = 'header'">DEL</button>
      <button type="button" class="semi-out__button" :disabled="!activeTicket" @click="openHeader('copy')">Copy</button>
      <button type="button" class="semi-out__button" :disabled="!activeTicket" @click="openPrint">Print</button>
      <small>清單條件：State = UnConfirm（0）</small>
    </div>

    <div class="semi-out__notice" role="status">{{ notice }}</div>
    <div class="semi-out__section-head"><h2>SEMI Out Basis</h2><span>{{ visibleTickets.length }} 筆未確認草稿</span></div>
    <div class="semi-out__grid-wrap semi-out__basis-grid">
      <table class="semi-out__grid">
        <thead><tr><th>SEMIOutNo</th><th>InventoryNo</th><th>State</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody>
          <tr v-for="ticket in visibleTickets" :key="ticket.semiOutNo" :class="{ 'is-active': activeTicket?.semiOutNo === ticket.semiOutNo }" @click="selectTicket(ticket)">
            <td>{{ ticket.semiOutNo }}</td><td>{{ ticket.inventoryNo }}</td><td><span class="semi-out__state">{{ stateLabel(ticket.state) }}</span></td><td>{{ ticket.description }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td><td>{{ ticket.reviser || '—' }}</td><td>{{ ticket.reviseDate || '—' }}</td>
          </tr>
          <tr v-if="!visibleTickets.length"><td colspan="8" class="semi-out__empty">沒有符合條件的未確認出庫單。</td></tr>
        </tbody>
      </table>
    </div>

    <div class="semi-out__section-head semi-out__detail-heading"><div><h2>SEMI Out Detail</h2><small v-if="activeTicket">{{ activeTicket.semiOutNo }} · {{ inventoryName(activeTicket.inventoryNo) }}</small></div><div class="semi-out__detail-actions">
      <button type="button" class="semi-out__button" :disabled="!activeTicket" @click="openDetail('add')">ADD</button>
      <button type="button" class="semi-out__button" :disabled="!activeDetail" @click="openDetail('edit')">EDIT</button>
      <button type="button" class="semi-out__button semi-out__button--danger" :disabled="!activeDetail" @click="deletePending = 'detail'">DEL</button>
      <button type="button" class="semi-out__button" :disabled="!activeDetail" @click="openDetail('copy')">Copy</button>
    </div></div>
    <div class="semi-out__grid-wrap semi-out__detail-grid">
      <table class="semi-out__grid">
        <thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th>LocatorNo</th><th class="is-number">Qty</th><th>UnitNo</th><th>PutInPlaceType</th><th>PutInPlaceNo</th><th>Component</th></tr></thead>
        <tbody>
          <tr v-for="row in activeDetails" :key="row.rowId" :class="{ 'is-active': activeDetail?.rowId === row.rowId }" @click="selectDetail(row)">
            <td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td class="is-number">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ destinationLabel(row.putInPlaceType) }}</td><td>{{ row.putInPlaceNo }}</td><td>{{ state.sourceLots.find((lot) => lot.lotNo === row.lotNo && lot.inventoryNo === activeTicket?.inventoryNo)?.components.length ? '整批帶入' : '—' }}</td>
          </tr>
          <tr v-if="!activeDetails.length"><td colspan="9" class="semi-out__empty">{{ activeTicket ? '此單據尚無明細。' : '請先選取一張出庫單。' }}</td></tr>
        </tbody>
      </table>
    </div>
    <div class="semi-out__footer">
      <div class="semi-out__component-summary"><strong>選取明細 Component</strong><span v-if="activeDetail">Lot {{ activeDetail.lotNo }} 的 component 由來源假資料載入；舊 Form 會整批帶入，沒有逐項勾選。</span><span v-else>選取一筆明細後顯示來源 Component。</span></div>
      <div class="semi-out__component-grid">
        <table class="semi-out__grid"><thead><tr><th>ComponentNo</th><th class="is-number">GoodQty</th><th class="is-number">ScrapQty</th><th>UnitNo</th></tr></thead><tbody>
          <tr v-for="component in selectedSourceLot?.components ?? []" :key="component.componentNo"><td>{{ component.componentNo }}</td><td class="is-number">{{ formatQty(component.goodQty) }}</td><td class="is-number">{{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td></tr>
          <tr v-if="!selectedSourceLot?.components.length"><td colspan="4" class="semi-out__empty">沒有 Component 假資料。</td></tr>
        </tbody></table>
      </div>
      <div class="semi-out__total">未確認明細合計 <strong>{{ formatQty(ticketTotal) }}</strong></div>
    </div>

    <div v-if="headerMode" class="semi-out__overlay" role="presentation" @click.self="closeDialogs">
      <section class="semi-out__dialog" role="dialog" aria-modal="true" :aria-label="headerMode === 'edit' ? '編輯出庫單單頭' : '新增出庫單單頭'">
        <header><div><small>SEMI OUT BASIS</small><h2>{{ headerMode === 'edit' ? '編輯' : headerMode === 'copy' ? '複製' : '新增' }}出庫單單頭</h2></div><button type="button" @click="closeDialogs">×</button></header>
        <label>SEMIOutNo <input v-model="headerDraft.semiOutNo" :readonly="headerMode === 'edit'" /></label>
        <label>InventoryNo <select v-model="headerDraft.inventoryNo" :disabled="headerMode === 'edit'"><option value="" disabled>請選擇半成品庫房</option><option v-for="inventory in inventories" :key="inventory.inventoryNo" :value="inventory.inventoryNo">{{ inventory.inventoryNo }} · {{ inventory.inventoryName }}</option></select></label>
        <label>Description <textarea v-model="headerDraft.description" rows="3" /></label>
        <p class="semi-out__hint">新增單據 State 固定為 UnConfirm；單頭 Copy 只複製單頭欄位，明細需另行建立。</p>
        <p v-if="formError" class="semi-out__error" role="alert">{{ formError }}</p>
        <footer><button type="button" class="semi-out__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-out__button semi-out__button--primary" @click="saveHeader">Save</button></footer>
      </section>
    </div>

    <div v-if="detailMode" class="semi-out__overlay" role="presentation" @click.self="closeDialogs">
      <section class="semi-out__dialog semi-out__dialog--wide" role="dialog" aria-modal="true" :aria-label="detailMode === 'edit' ? '編輯出庫單明細' : '新增出庫單明細'">
        <header><div><small>{{ activeTicket?.semiOutNo }} · SEMI OUT DETAIL</small><h2>{{ detailMode === 'edit' ? '編輯' : detailMode === 'copy' ? '複製' : '新增' }}出庫明細</h2></div><button type="button" @click="closeDialogs">×</button></header>
        <div class="semi-out__form-grid">
          <label>來源 Lot / Product <select v-model="detailDraft.lotNo" :disabled="detailMode === 'edit'"><option value="" disabled>請選擇來源 Lot</option><option v-for="lot in draftSourceLots" :key="lot.lotNo" :value="lot.lotNo">{{ lot.lotNo }} · {{ lot.productNo }} · {{ lot.locatorNo }} · {{ formatQty(lot.availableQty) }} {{ lot.unitNo }}</option></select></label>
          <label>可用量 / 單位 <input :value="draftLot ? `${formatQty(draftLot.availableQty)} ${draftLot.unitNo}` : '—'" readonly /></label>
          <label>ProductNo <input :value="draftLot?.productNo ?? ''" readonly /></label>
          <label>LocatorNo <input :value="draftLot?.locatorNo ?? ''" readonly /></label>
          <label>Qty <input v-model.number="detailDraft.qty" type="number" min="0" step="0.0001" :readonly="Boolean(draftLot?.hasLevel || draftLot?.components.length)" /></label>
          <label>UnitNo <input :value="draftLot?.unitNo ?? ''" readonly /></label>
        </div>
        <fieldset><legend>Put In Place Type</legend><label v-for="option in destinationTypes" :key="option.value" class="semi-out__radio"><input v-model.number="detailDraft.putInPlaceType" type="radio" :value="option.value" />{{ option.label }}</label></fieldset>
        <label v-if="[2, 3].includes(detailDraft.putInPlaceType)">PutInPlaceNo <input v-model="detailDraft.putInPlaceNo" :placeholder="detailDraft.putInPlaceType === 3 ? 'MO-DEV-26001' : 'WIP-DEV-01'" /></label>
        <p v-else class="semi-out__hint">此類型由舊 Form 固定送出 PutInPlaceNo = N/A。</p>
        <div v-if="draftLot?.components.length" class="semi-out__components-box"><strong>Component（整批出庫）</strong><span>該 Lot 含 {{ draftLot.components.length }} 筆 Component，舊 Form 將所有 Component 一起帶入，並鎖定 Qty。</span></div>
        <p v-if="detailDraft.putInPlaceType === 3" class="semi-out__hint">工單正式流程會查工單用料及替代料；DEV 預覽僅保留目的編號與明細欄位，不驗證 MO Material List。</p>
        <p v-if="formError" class="semi-out__error" role="alert">{{ formError }}</p>
        <footer><button type="button" class="semi-out__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-out__button semi-out__button--primary" @click="saveDetail">Save</button></footer>
      </section>
    </div>

    <div v-if="printOpen && displayTicket" class="semi-out__overlay" role="presentation" @click.self="closeDialogs">
      <section class="semi-out__dialog semi-out__print" role="dialog" aria-modal="true" aria-label="半成品出庫單列印預覽">
        <header><div><small>PRINT PREVIEW · DEV</small><h2>半成品出庫單</h2></div><button type="button" @click="closeDialogs">×</button></header>
        <p><strong>出庫單號：</strong>{{ displayTicket.semiOutNo }}　<strong>庫房：</strong>{{ displayTicket.inventoryNo }}</p>
        <p><strong>開單人：</strong>{{ displayTicket.creator }}　<strong>日期：</strong>{{ displayTicket.createDate }}</p>
        <table class="semi-out__grid"><thead><tr><th>ProductNo</th><th>LotNo</th><th>LocatorNo</th><th>Qty</th><th>UnitNo</th><th>投料目的</th></tr></thead><tbody><tr v-for="row in activeDetails" :key="row.rowId"><td>{{ row.productNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.locatorNo }}</td><td>{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ destinationLabel(row.putInPlaceType) }} · {{ row.putInPlaceNo }}</td></tr></tbody></table>
        <p class="semi-out__hint">僅顯示 DEV 假資料，不連接印表機或 MES 報表服務。</p>
        <footer><button type="button" class="semi-out__button" @click="closeDialogs">Close</button></footer>
      </section>
    </div>

    <div v-if="deletePending" class="semi-out__overlay" role="presentation" @click.self="closeDialogs">
      <section class="semi-out__dialog" role="alertdialog" aria-modal="true" aria-label="確認刪除">
        <header><div><small>CONFIRM DELETE</small><h2>刪除{{ deletePending === 'header' ? '出庫單及其明細' : '出庫明細' }}？</h2></div><button type="button" @click="closeDialogs">×</button></header>
        <p class="semi-out__hint">此動作只修改本頁 DEV 假資料，會同步釋放該 Lot 的模擬預約。</p>
        <footer><button type="button" class="semi-out__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-out__button semi-out__button--danger" @click="confirmDelete">Delete</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.semi-out { min-height: 0; height: 100%; box-sizing: border-box; display: flex; flex-direction: column; gap: 8px; padding: 12px; overflow: auto; background: #f4f2ea; color: #24231e; font: inherit; }
.semi-out__topbar { display: flex; align-items: center; justify-content: space-between; min-height: 44px; }
.semi-out__topbar h1 { margin: 2px 0 0; font-size: 20px; line-height: 1.2; }
.semi-out__topbar h1 span { margin-left: 8px; color: #786c42; font-size: 12px; font-weight: 500; }
.semi-out__eyebrow { color: #817a69; font-size: 10px; letter-spacing: .12em; }
.semi-out__close, .semi-out__button { min-height: 28px; border: 1px solid #777366; border-radius: 3px; background: #fffefa; color: #292820; padding: 4px 10px; font: inherit; cursor: pointer; }
.semi-out__button--primary { border-color: #344e42; background: #344e42; color: #fff; }
.semi-out__button--danger { border-color: #a35a4c; color: #913e32; }
.semi-out button:disabled { opacity: .45; cursor: default; }
.semi-out__toolbar { display: flex; align-items: end; gap: 6px; flex-wrap: wrap; padding: 8px; border: 1px solid #d2cdbd; background: #eae7dc; }
.semi-out__toolbar label { display: grid; gap: 3px; font-size: 11px; color: #575448; }
.semi-out input, .semi-out select, .semi-out textarea { min-height: 30px; box-sizing: border-box; border: 1px solid #c9c4b5; border-radius: 2px; background: #fff; color: #28271f; padding: 4px 7px; font: inherit; }
.semi-out__toolbar input { min-width: 210px; }
.semi-out__toolbar i { height: 27px; margin: 0 3px; border-left: 1px solid #c1bdaf; }
.semi-out__toolbar small { margin: 0 0 6px auto; color: #6a665b; font-size: 11px; }
.semi-out__notice { min-height: 20px; color: #615d50; font-size: 12px; }
.semi-out__section-head { display: flex; align-items: baseline; justify-content: space-between; padding: 0 2px; }
.semi-out__section-head h2 { margin: 0; font-size: 14px; }
.semi-out__section-head > span, .semi-out__section-head small { color: #777366; font-size: 11px; }
.semi-out__grid-wrap { min-height: 80px; overflow: auto; border: 1px solid #cec9ba; background: #fffefa; }
.semi-out__basis-grid { max-height: 230px; }
.semi-out__detail-grid { max-height: 220px; }
.semi-out__grid { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }
.semi-out__grid th { position: sticky; top: 0; z-index: 1; background: #e4e1d5; color: #4d4a40; text-align: left; font-weight: 600; }
.semi-out__grid th, .semi-out__grid td { padding: 6px 8px; border-right: 1px solid #e5e1d5; border-bottom: 1px solid #e5e1d5; }
.semi-out__grid tbody tr { cursor: pointer; }
.semi-out__grid tbody tr:hover { background: #f5f2e8; }
.semi-out__grid tbody tr.is-active { background: #e5eadf; }
.semi-out__grid .is-number { text-align: right; font-variant-numeric: tabular-nums; }
.semi-out__empty { padding: 18px !important; color: #7c786c; text-align: center; }
.semi-out__state { display: inline-block; padding: 2px 6px; border: 1px solid #c4b979; border-radius: 10px; background: #f4efd3; color: #67591f; font-size: 10px; }
.semi-out__detail-heading { margin-top: 1px; }
.semi-out__detail-heading > div:first-child { display: flex; align-items: baseline; gap: 9px; }
.semi-out__detail-actions { display: flex; gap: 5px; }
.semi-out__footer { display: grid; grid-template-columns: 1fr minmax(270px, 38%) auto; align-items: stretch; gap: 8px; margin-top: auto; }
.semi-out__component-summary, .semi-out__total { display: flex; flex-direction: column; justify-content: center; gap: 5px; padding: 9px; border: 1px solid #d2cdbd; background: #eeece3; font-size: 11px; }
.semi-out__component-summary span { color: #716d60; line-height: 1.4; }
.semi-out__component-grid { max-height: 130px; overflow: auto; border: 1px solid #cec9ba; background: #fffefa; }
.semi-out__component-grid .semi-out__grid { font-size: 11px; }
.semi-out__component-grid .semi-out__grid th, .semi-out__component-grid .semi-out__grid td { padding: 4px 7px; }
.semi-out__total { min-width: 110px; align-items: flex-end; color: #706c60; }
.semi-out__total strong { color: #26251f; font-size: 17px; }
.semi-out__overlay { position: fixed; inset: 0; z-index: 30; display: grid; place-items: center; padding: 20px; background: rgb(24 24 20 / 46%); }
.semi-out__dialog { width: min(520px, 100%); max-height: min(90vh, 760px); overflow: auto; box-sizing: border-box; padding: 18px; border: 1px solid #c5c0b2; border-radius: 4px; background: #f8f6ef; box-shadow: 0 18px 55px rgb(0 0 0 / 20%); }
.semi-out__dialog--wide { width: min(720px, 100%); }
.semi-out__dialog > header { display: flex; align-items: center; justify-content: space-between; margin: -2px 0 16px; }
.semi-out__dialog > header small { color: #807a69; font-size: 10px; letter-spacing: .1em; }
.semi-out__dialog h2 { margin: 3px 0 0; font-size: 18px; }
.semi-out__dialog > header button { width: 30px; height: 30px; border: 0; background: transparent; color: #625f54; font-size: 23px; cursor: pointer; }
.semi-out__dialog > label, .semi-out__form-grid label { display: grid; gap: 4px; margin: 10px 0; color: #5c584d; font-size: 12px; }
.semi-out__dialog textarea { resize: vertical; }
.semi-out__dialog fieldset { display: flex; flex-wrap: wrap; gap: 12px; margin: 12px 0; padding: 9px 11px; border: 1px solid #d2cdbd; }
.semi-out__dialog legend { padding: 0 5px; color: #5c584d; font-size: 12px; }
.semi-out__radio { display: inline-flex; align-items: center; gap: 5px; color: #333127; font-size: 12px; }
.semi-out__radio input { min-height: auto; }
.semi-out__form-grid { display: grid; grid-template-columns: 1fr 1fr; column-gap: 12px; }
.semi-out__hint { margin: 10px 0; padding: 8px 9px; border-left: 3px solid #b3a76c; background: #efeddf; color: #6a6554; font-size: 11px; line-height: 1.45; }
.semi-out__error { padding: 8px 10px; border: 1px solid #d9a59a; background: #fbefec; color: #9a3c30; font-size: 12px; }
.semi-out__components-box { display: flex; flex-direction: column; gap: 4px; padding: 9px; border: 1px solid #b9cab9; background: #e9efe6; font-size: 11px; }
.semi-out__components-box span { color: #536351; }
.semi-out__dialog > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 15px; }
.semi-out__print { width: min(790px, 100%); }
.semi-out__print p { color: #514e43; font-size: 12px; }
@media (max-width: 800px) { .semi-out__footer { grid-template-columns: 1fr; } .semi-out__toolbar small { width: 100%; margin: 0; } }
@media (max-width: 560px) { .semi-out { padding: 8px; } .semi-out__form-grid { grid-template-columns: 1fr; } .semi-out__detail-heading { align-items: flex-start; gap: 8px; flex-direction: column; } .semi-out__detail-actions { flex-wrap: wrap; } }
</style>

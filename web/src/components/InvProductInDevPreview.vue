<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type ProductInType = 0 | 1
type FormMode = 'add' | 'edit' | null
interface InventoryOption { inventoryNo: string; inventoryName: string; inventoryType: 1 | 6 }
interface ComponentStock { componentNo: string; goodQty: number; scrapQty: number; unitNo: string; bookingFlag: boolean }
interface SourceLot {
  inventoryNo: string; lotNo: string; productNo: string; productName: string; productVersion: string
  qty: number; unitNo: string; locatorNo: string; packageNo: string; packageName: string; packageParentNo?: string
  bookingFlag: boolean; components: ComponentStock[]
}
interface ProductInDetail {
  lotNo: string; productNo: string; productName: string; productVersion: string; qty: number
  unitNo: string; locatorNo: string; packageNo: string; packageName: string; components: ComponentStock[]
}
interface ProductInTicket {
  productInNo: string; productInType: ProductInType; inventoryNo: string; fromInventoryNo: string
  source: 1; state: number; description: string; creator: string; createDate: string
  confirmDate: string; reviser: string; reviseDate: string; dataStamp: number; details: ProductInDetail[]
}
interface DevState { version: 1; fgdSerial: number; semiSerial: number; tickets: ProductInTicket[]; sourceLots: SourceLot[]; audit: string[] }
interface Draft { productInNo: string; productInType: ProductInType; inventoryNo: string; fromInventoryNo: string; description: string; dataStamp: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.iv502-product-in.v1'
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories: InventoryOption[] = [
  { inventoryNo: 'FGD-DEV-01', inventoryName: '成品庫 A', inventoryType: 1 },
  { inventoryNo: 'FGD-DEV-02', inventoryName: '成品庫 B', inventoryType: 1 },
  { inventoryNo: 'SEMI-DEV-01', inventoryName: '半成品庫 A', inventoryType: 6 },
  { inventoryNo: 'SEMI-DEV-02', inventoryName: '半成品庫 B', inventoryType: 6 },
]

function makeSeed(): DevState {
  const sourceLots: SourceLot[] = [
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-101', productNo: 'PRD-CONTROL-10', productName: '控制板總成', productVersion: 'A2', qty: 12, unitNo: 'EA', locatorNo: 'S-A-01', packageNo: 'PACK-DEV-101', packageName: '外箱 2 入', bookingFlag: true, components: [{ componentNo: 'CMP-PCB-01', goodQty: 11, scrapQty: 1, unitNo: 'EA', bookingFlag: true }, { componentNo: 'CMP-CONN-02', goodQty: 12, scrapQty: 0, unitNo: 'EA', bookingFlag: true }] },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-102', productNo: 'PRD-CONTROL-10', productName: '控制板總成', productVersion: 'A2', qty: 8, unitNo: 'EA', locatorNo: 'S-A-01', packageNo: 'PACK-DEV-101', packageName: '外箱 2 入', bookingFlag: true, components: [{ componentNo: 'CMP-PCB-01', goodQty: 8, scrapQty: 0, unitNo: 'EA', bookingFlag: true }] },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-201', productNo: 'PRD-SENSOR-04', productName: '感測器模組', productVersion: 'C1', qty: 6, unitNo: 'EA', locatorNo: 'S-A-02', packageNo: '', packageName: '', bookingFlag: true, components: [{ componentNo: 'CMP-SENSOR-01', goodQty: 6, scrapQty: 0, unitNo: 'EA', bookingFlag: true }] },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-301', productNo: 'PRD-SENSOR-04', productName: '感測器模組', productVersion: 'C1', qty: 6, unitNo: 'EA', locatorNo: 'S-A-02', packageNo: '', packageName: '', bookingFlag: false, components: [] },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-401', productNo: 'PRD-PANEL-07', productName: '顯示面板', productVersion: 'B1', qty: 10, unitNo: 'EA', locatorNo: 'S-A-03', packageNo: 'PACK-DEV-402', packageName: '外箱 2 入', bookingFlag: false, components: [{ componentNo: 'CMP-PANEL-01', goodQty: 10, scrapQty: 0, unitNo: 'EA', bookingFlag: false }] },
    { inventoryNo: 'SEMI-DEV-01', lotNo: 'LOT-SEMI-402', productNo: 'PRD-PANEL-07', productName: '顯示面板', productVersion: 'B1', qty: 5, unitNo: 'EA', locatorNo: 'S-A-03', packageNo: 'PACK-DEV-402', packageName: '外箱 2 入', bookingFlag: false, components: [] },
    { inventoryNo: 'SEMI-DEV-02', lotNo: 'LOT-SEMI-501', productNo: 'PRD-VALVE-06', productName: '閥件模組', productVersion: 'D1', qty: 7, unitNo: 'EA', locatorNo: 'S-B-01', packageNo: '', packageName: '', bookingFlag: false, components: [] },
    { inventoryNo: 'SEMI-DEV-02', lotNo: 'LOT-SEMI-601', productNo: 'PRD-VALVE-06', productName: '閥件模組', productVersion: 'D1', qty: 3, unitNo: 'EA', locatorNo: 'S-B-02', packageNo: 'PACK-DEV-601-INNER', packageName: '內盒', packageParentNo: 'PACK-DEV-601-OUTER', bookingFlag: false, components: [] },
  ]
  const details = (lotNos: string[], fgdDestination = true): ProductInDetail[] => lotNos.map((lotNo) => {
    const lot = sourceLots.find((row) => row.lotNo === lotNo)!
    return { lotNo: lot.lotNo, productNo: lot.productNo, productName: lot.productName, productVersion: lot.productVersion, qty: lot.qty, unitNo: lot.unitNo, locatorNo: fgdDestination ? 'FGD-A-01' : 'S-DEST-01', packageNo: lot.packageNo, packageName: lot.packageName, components: lot.components.map((row) => ({ ...row })) }
  })
  return {
    version: 1, fgdSerial: 4, semiSerial: 2, sourceLots,
    tickets: [
      { productInNo: 'FI-DEV-260001', productInType: 0, inventoryNo: 'FGD-DEV-01', fromInventoryNo: 'SEMI-DEV-01', source: 1, state: 0, description: 'DEV 半成品轉成品；完整包裝入選', creator: 'DEV001', createDate: '2026-11-22 09:20:00', confirmDate: '', reviser: '', reviseDate: '', dataStamp: 2, details: details(['LOT-SEMI-101', 'LOT-SEMI-102']) },
      { productInNo: 'SI-DEV-260001', productInType: 1, inventoryNo: 'SEMI-DEV-02', fromInventoryNo: 'SEMI-DEV-01', source: 1, state: 0, description: 'DEV 半成品轉倉', creator: 'DEV002', createDate: '2026-11-23 14:10:00', confirmDate: '', reviser: '', reviseDate: '', dataStamp: 1, details: details(['LOT-SEMI-201'], false) },
    ], audit: ['DEV 假資料已載入。']
  }
}
function readState(): DevState {
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) {
      const data = JSON.parse(raw) as DevState
      if (data?.version === 1 && Array.isArray(data.tickets) && Array.isArray(data.sourceLots)) return data
    }
  } catch { /* Use the DEV fixture if browser storage is unavailable. */ }
  return makeSeed()
}

const state = ref<DevState>(readState())
const productInType = ref<ProductInType>(0)
const queryNo = ref('')
const activeNo = ref('')
const selectedNos = ref<string[]>([])
const mode = ref<FormMode>(null)
const draft = ref<Draft>(blankDraft())
const selectedLotNos = ref<string[]>([])
const formError = ref('')
const notice = ref('DEV 假資料：僅在本機瀏覽器模擬，不呼叫 MES 或正式 API。')
const printOpen = ref(false)
const activeDetailLot = ref('')

const visibleTickets = computed(() => {
  const query = queryNo.value.trim().toLocaleUpperCase()
  return state.value.tickets.filter((row) => row.source === 1 && row.state === 0 && row.productInType === productInType.value && (!query || row.productInNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.productInNo.localeCompare(b.productInNo))
})
const activeTicket = computed(() => visibleTickets.value.find((row) => row.productInNo === activeNo.value) ?? visibleTickets.value[0] ?? null)
const activeDetails = computed(() => activeTicket.value?.details ?? [])
const activeDetail = computed(() => activeDetails.value.find((row) => row.lotNo === activeDetailLot.value) ?? activeDetails.value[0] ?? null)
const selectedTickets = computed(() => visibleTickets.value.filter((row) => selectedNos.value.includes(row.productInNo)))
const targetInventories = computed(() => inventories.filter((row) => row.inventoryType === (draft.value.productInType === 0 ? 1 : 6)))
const sourceInventories = computed(() => inventories.filter((row) => row.inventoryType === 6))
const draftSourceLots = computed(() => state.value.sourceLots.filter((row) => row.inventoryNo === draft.value.fromInventoryNo && row.qty > 0 && (!row.bookingFlag || (mode.value === 'edit' && state.value.tickets.find((ticket) => ticket.productInNo === draft.value.productInNo)?.details.some((detail) => detail.lotNo === row.lotNo)))))
const selectedSourceLots = computed(() => draftSourceLots.value.filter((row) => selectedLotNos.value.includes(row.lotNo)))
const selectedPackageNos = computed(() => [...new Set(selectedSourceLots.value.map((row) => row.packageNo).filter(Boolean))])
const detailTotal = computed(() => activeDetails.value.reduce((sum, row) => sum + row.qty, 0))
const printTicket = computed(() => selectedTickets.value[0] ?? activeTicket.value)

watch(visibleTickets, (rows) => {
  if (!rows.some((row) => row.productInNo === activeNo.value)) activeNo.value = rows[0]?.productInNo ?? ''
  selectedNos.value = selectedNos.value.filter((no) => rows.some((row) => row.productInNo === no))
}, { immediate: true })
watch(activeDetails, (rows) => {
  if (!rows.some((row) => row.lotNo === activeDetailLot.value)) activeDetailLot.value = rows[0]?.lotNo ?? ''
}, { immediate: true })
watch(() => draft.value.productInType, () => {
  if (targetInventories.value.every((row) => row.inventoryNo !== draft.value.inventoryNo)) draft.value.inventoryNo = targetInventories.value[0]?.inventoryNo ?? ''
})
watch(state, (value) => { try { localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV may run with storage disabled. */ } }, { deep: true })

function blankDraft(): Draft { return { productInNo: '', productInType: 0, inventoryNo: '', fromInventoryNo: '', description: '', dataStamp: 0 } }
function inventoryName(no: string): string { return inventories.find((row) => row.inventoryNo === no)?.inventoryName ?? no }
function typeLabel(type: ProductInType): string { return type === 0 ? 'FGD INV' : 'SEMI INV' }
function formatQty(value: number): string { return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value) }
function stateLabel(value: number): string { return value === 0 ? 'UnConfirm' : value === 1 ? 'Handling' : value === 2 ? 'Confirm' : value === 3 ? 'Fail' : 'Scrap' }
function setTicketSelection(ticket: ProductInTicket, checked: boolean): void {
  selectedNos.value = checked ? [...new Set([...selectedNos.value, ticket.productInNo])] : selectedNos.value.filter((no) => no !== ticket.productInNo)
  activeNo.value = ticket.productInNo
}
function openAdd(): void {
  const type = productInType.value
  draft.value = { ...blankDraft(), productInType: type, inventoryNo: type === 0 ? 'FGD-DEV-01' : 'SEMI-DEV-02', fromInventoryNo: 'SEMI-DEV-01' }
  selectedLotNos.value = []
  formError.value = ''
  mode.value = 'add'
}
function openEdit(): void {
  const ticket = activeTicket.value
  if (!ticket || ticket.state !== 0) return
  draft.value = { productInNo: ticket.productInNo, productInType: ticket.productInType, inventoryNo: ticket.inventoryNo, fromInventoryNo: ticket.fromInventoryNo, description: ticket.description, dataStamp: ticket.dataStamp }
  selectedLotNos.value = ticket.details.map((row) => row.lotNo)
  formError.value = ''
  mode.value = 'edit'
}
function cancelForm(): void { mode.value = null; formError.value = '' }
function toggleLot(lotNo: string, checked: boolean): void {
  selectedLotNos.value = checked ? [...new Set([...selectedLotNos.value, lotNo])] : selectedLotNos.value.filter((value) => value !== lotNo)
}
function togglePackage(packageNo: string): void {
  const members = draftSourceLots.value.filter((row) => row.packageNo === packageNo).map((row) => row.lotNo)
  const allSelected = members.every((lotNo) => selectedLotNos.value.includes(lotNo))
  selectedLotNos.value = allSelected ? selectedLotNos.value.filter((lotNo) => !members.includes(lotNo)) : [...new Set([...selectedLotNos.value, ...members])]
}
function saveDraft(): void {
  formError.value = ''
  if (!draft.value.productInNo.trim() && mode.value === 'edit') { formError.value = '單據編號不可空白。'; return }
  if (!draft.value.inventoryNo) { formError.value = '請選擇目的庫房。'; return }
  if (!draft.value.fromInventoryNo) { formError.value = '請選擇來源 WIP 半成品庫房。'; return }
  if (targetInventories.value.every((row) => row.inventoryNo !== draft.value.inventoryNo)) { formError.value = '目的庫房類型與入庫類型不符。'; return }
  if (!selectedSourceLots.value.length) { formError.value = '至少勾選一筆可用的來源 Lot。'; return }
  if (draft.value.productInType === 1 && selectedSourceLots.value.some((row) => row.packageNo)) { formError.value = '含包裝編號的生產批只能開立成品入庫單，不能開立半成品入庫單。'; return }
  for (const packageNo of selectedPackageNos.value) {
    const allMembers = state.value.sourceLots.filter((row) => row.inventoryNo === draft.value.fromInventoryNo && row.qty > 0 && row.packageNo === packageNo)
    if (allMembers.some((row) => row.packageParentNo)) { formError.value = `包裝 ${packageNo} 仍在更高層包裝內，請改選最高層包裝。`; return }
    if (allMembers.some((row) => !selectedLotNos.value.includes(row.lotNo))) { formError.value = `包裝 ${packageNo} 尚未選取全部內容 Lot。`; return }
    if (!allMembers.length) { formError.value = `包裝 ${packageNo} 不存在於來源庫房。`; return }
  }
  const current = mode.value === 'edit' ? state.value.tickets.find((row) => row.productInNo === draft.value.productInNo) : null
  if (mode.value === 'edit' && (!current || current.state !== 0 || current.dataStamp !== draft.value.dataStamp)) { formError.value = '單據已被其他作業修改，請重新載入後再編輯。'; return }
  const serial = draft.value.productInType === 0 ? state.value.fgdSerial : state.value.semiSerial
  const productInNo = mode.value === 'add' ? `${draft.value.productInType === 0 ? 'FI' : 'SI'}-DEV-${String(serial).padStart(6, '0')}` : draft.value.productInNo
  if (mode.value === 'add' && state.value.tickets.some((row) => row.productInNo === productInNo)) { formError.value = '單號重複，請重新產生。'; return }
  if (current) for (const old of current.details) setLotBooking(old.lotNo, false)
  for (const lot of selectedSourceLots.value) setLotBooking(lot.lotNo, true)
  const details: ProductInDetail[] = selectedSourceLots.value.map((row) => ({
    lotNo: row.lotNo, productNo: row.productNo, productName: row.productName, productVersion: row.productVersion,
    qty: row.qty, unitNo: row.unitNo, locatorNo: draft.value.inventoryNo === 'FGD-DEV-01' ? 'FGD-A-01' : draft.value.inventoryNo === 'FGD-DEV-02' ? 'FGD-B-01' : 'S-DEST-01',
    packageNo: row.packageNo, packageName: row.packageName, components: row.components.map((component) => ({ ...component, bookingFlag: true }))
  }))
  const now = new Date().toLocaleString('sv-SE', { timeZone: 'Asia/Taipei', hour12: false })
  if (current) {
    Object.assign(current, { inventoryNo: draft.value.inventoryNo, fromInventoryNo: draft.value.fromInventoryNo, description: draft.value.description, details, reviser: 'DEV-USER', reviseDate: now, dataStamp: current.dataStamp + 1 })
    state.value.audit.unshift(`${productInNo} 已修改；DataStamp ${current.dataStamp}，更新來源預約。`)
  } else {
    state.value.tickets.unshift({ productInNo, productInType: draft.value.productInType, inventoryNo: draft.value.inventoryNo, fromInventoryNo: draft.value.fromInventoryNo, source: 1, state: 0, description: draft.value.description, creator: 'DEV-USER', createDate: now, confirmDate: '', reviser: '', reviseDate: '', dataStamp: 1, details })
    if (draft.value.productInType === 0) state.value.fgdSerial += 1
    else state.value.semiSerial += 1
    state.value.audit.unshift(`${productInNo} 已建立草稿並預約 ${details.length} 筆 WIP 半成品 Lot。`)
  }
  activeNo.value = productInNo
  selectedNos.value = []
  mode.value = null
  notice.value = `${productInNo} 已在 DEV 假資料中${current ? '更新' : '建立'}；尚未過帳庫存。`
}
function setLotBooking(lotNo: string, booked: boolean): void {
  const source = state.value.sourceLots.find((row) => row.lotNo === lotNo)
  if (source) {
    source.bookingFlag = booked
    source.components.forEach((component) => { component.bookingFlag = booked })
  }
}
function deleteSelected(): void {
  const targets = selectedTickets.value.length ? selectedTickets.value : activeTicket.value ? [activeTicket.value] : []
  if (!targets.length) return
  for (const ticket of targets) {
    if (ticket.state !== 0) { notice.value = `${ticket.productInNo} 非 UnConfirm，不能作廢。`; return }
  }
  for (const ticket of targets) {
    ticket.state = -1
    ticket.dataStamp += 1
    ticket.reviser = 'DEV-USER'
    ticket.reviseDate = new Date().toLocaleString('sv-SE', { timeZone: 'Asia/Taipei', hour12: false })
    ticket.details.forEach((detail) => setLotBooking(detail.lotNo, false))
    state.value.audit.unshift(`${ticket.productInNo} 已作廢；單頭／明細保留供歷史查詢並解除來源預約。`)
  }
  selectedNos.value = []
  notice.value = `已作廢 ${targets.length} 張 DEV 草稿；原始明細保留。`
}
function resetDev(): void {
  state.value = makeSeed()
  queryNo.value = ''
  selectedNos.value = []
  notice.value = 'DEV 假資料已重設。'
}
</script>

<template>
  <main class="product-in" aria-label="開立成品/半成品入庫單 DEV 預覽">
    <header class="page-head">
      <div><p class="eyebrow">INV / IV502 · {{ previewLabel }}</p><h1>開立成品／半成品入庫單</h1><p class="sub">來源：WIP 半成品庫 · 草稿管理 · DEV 假資料</p></div>
      <button class="close" type="button" title="Close (X)" @click="emit('close')">×</button>
    </header>
    <div class="type-switch" role="tablist" aria-label="入庫類型">
      <button type="button" :class="{ active: productInType === 0 }" @click="productInType = 0">FGD INV</button>
      <button type="button" :class="{ active: productInType === 1 }" @click="productInType = 1">SEMI INV</button>
      <span>主畫面預設 FGD INV；只查 Source=1、State=0 草稿</span>
    </div>
    <div class="toolbar">
      <label class="query">單據編號 <input v-model="queryNo" placeholder="輸入 ProductInNo" @keydown.enter.prevent /></label>
      <button type="button" @click="activeNo = visibleTickets[0]?.productInNo ?? ''">Query</button>
      <span class="spacer"></span>
      <button type="button" class="primary" @click="openAdd">Add</button>
      <button type="button" :disabled="!activeTicket" @click="openEdit">Edit</button>
      <button type="button" :disabled="!activeTicket && !selectedNos.length" @click="deleteSelected">Delete</button>
      <button type="button" :disabled="!activeTicket" @click="printOpen = true">Print</button>
      <button type="button" @click="resetDev">Reset DEV</button>
    </div>
    <p class="notice" role="status">{{ notice }}</p>
    <section class="panel basis-panel">
      <div class="panel-title"><h2>未確認入庫單</h2><span>{{ visibleTickets.length }} 張</span></div>
      <div class="table-wrap">
        <table><thead><tr><th>選</th><th>ProductInNo</th><th>InventoryNo</th><th>State</th><th>ConfirmDate</th><th>Description</th><th>FromInventoryNo</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody><tr v-for="ticket in visibleTickets" :key="ticket.productInNo" :class="{ selected: ticket.productInNo === activeTicket?.productInNo }" @click="activeNo = ticket.productInNo">
            <td><input type="checkbox" :checked="selectedNos.includes(ticket.productInNo)" :aria-label="`選取 ${ticket.productInNo}`" @click.stop @change="setTicketSelection(ticket, ($event.target as HTMLInputElement).checked)" /></td>
            <td class="mono">{{ ticket.productInNo }}</td><td>{{ ticket.inventoryNo }}</td><td><span class="state">{{ stateLabel(ticket.state) }}</span></td><td>{{ ticket.confirmDate || '—' }}</td><td>{{ ticket.description || '—' }}</td><td>{{ ticket.fromInventoryNo }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td><td>{{ ticket.reviser || '—' }}</td><td>{{ ticket.reviseDate || '—' }}</td>
          </tr><tr v-if="!visibleTickets.length"><td colspan="11" class="empty">沒有符合條件的未確認單據。</td></tr></tbody>
        </table>
      </div>
    </section>
    <section class="panel detail-panel">
      <div class="panel-title"><div><h2>Lot 明細</h2><p v-if="activeTicket">{{ activeTicket.productInNo }} · 目的 {{ inventoryName(activeTicket.inventoryNo) }} · 來源 {{ inventoryName(activeTicket.fromInventoryNo) }}</p></div><span>合計 {{ formatQty(detailTotal) }}</span></div>
      <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>ProductNo</th><th>ProductName</th><th>Version</th><th>Qty</th><th>UnitNo</th><th>LocatorNo</th><th>PackingNo</th><th>PackingName</th></tr></thead>
        <tbody><tr v-for="row in activeDetails" :key="row.lotNo" :class="{ selected: activeDetail?.lotNo === row.lotNo }" @click="activeDetailLot = row.lotNo"><td class="mono">{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.productVersion }}</td><td class="numeric">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.packageNo || '—' }}</td><td>{{ row.packageName || '—' }}</td></tr><tr v-if="!activeDetails.length"><td colspan="9" class="empty">選取一張草稿以查看明細。</td></tr></tbody>
      </table></div>
      <div class="component-strip"><strong>Component</strong><span v-if="activeDetail">{{ activeDetail.lotNo }}</span><span v-else>選取 Lot 查看元件明細</span>
        <div class="table-wrap"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="component in activeDetail?.components ?? []" :key="component.componentNo"><td class="mono">{{ component.componentNo }}</td><td>{{ formatQty(component.goodQty) }}</td><td>{{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td></tr><tr v-if="!activeDetail?.components.length"><td colspan="4" class="empty">無元件或未選取 Lot。</td></tr></tbody></table></div>
      </div>
    </section>
    <footer class="audit"><span>DEV 最近操作</span><span v-for="(item, index) in state.audit.slice(0, 3)" :key="index">{{ item }}</span></footer>

    <div v-if="mode" class="overlay" role="presentation" @click.self="cancelForm">
      <section class="dialog form-dialog" role="dialog" aria-modal="true" aria-labelledby="form-title">
        <header><div><p class="eyebrow">{{ mode === 'add' ? 'ADD' : 'EDIT' }} · SOURCE=1</p><h2 id="form-title">{{ mode === 'add' ? '開立入庫草稿' : '編輯入庫草稿' }}</h2></div><button type="button" class="close" @click="cancelForm">×</button></header>
        <div class="form-grid">
          <fieldset :disabled="mode === 'edit'"><legend>入庫類型</legend><label><input v-model.number="draft.productInType" type="radio" :value="0" /> FGD INV</label><label><input v-model.number="draft.productInType" type="radio" :value="1" /> SEMI INV</label></fieldset>
          <label>單據編號<input v-model.trim="draft.productInNo" :placeholder="mode === 'add' ? '儲存時依序產生' : ''" readonly /></label>
          <label>目的庫房<select v-model="draft.inventoryNo" :disabled="mode === 'edit'"><option value="">請選擇</option><option v-for="item in targetInventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.inventoryName }}</option></select></label>
          <label>來源 WIP 半成品庫房<select v-model="draft.fromInventoryNo" :disabled="mode === 'edit'"><option value="">請選擇</option><option v-for="item in sourceInventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryNo }} · {{ item.inventoryName }}</option></select></label>
          <label class="wide">Description<input v-model.trim="draft.description" maxlength="200" placeholder="輸入入庫說明" /></label>
        </div>
        <div class="source-heading"><div><h3>可用來源 Lot</h3><p>僅列 Qty &gt; 0 且 BookingFlag=0 的 WIP 半成品；編輯時保留本單已預約 Lot。</p></div><span>{{ selectedSourceLots.length }} 筆已選</span></div>
        <div class="package-actions"><span>包裝規則：成品單需選滿最高層包裝所有 Lot；半成品單不能選有包裝編號的 Lot。</span><button v-for="pack in [...new Set(draftSourceLots.map((row) => row.packageNo).filter(Boolean))]" :key="pack" type="button" @click="togglePackage(pack)">整包 {{ pack }}</button></div>
        <div class="table-wrap source-table"><table><thead><tr><th>選取</th><th>LotNo</th><th>ProductNo</th><th>ProductName</th><th>Qty</th><th>UnitNo</th><th>LocatorNo</th><th>BookingFlag</th><th>PackingNo</th><th>Component</th></tr></thead>
          <tbody><tr v-for="lot in draftSourceLots" :key="lot.lotNo"><td><input type="checkbox" :checked="selectedLotNos.includes(lot.lotNo)" @change="toggleLot(lot.lotNo, ($event.target as HTMLInputElement).checked)" /></td><td class="mono">{{ lot.lotNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.productName }}</td><td>{{ formatQty(lot.qty) }}</td><td>{{ lot.unitNo }}</td><td>{{ lot.locatorNo }}</td><td>{{ lot.bookingFlag ? '1 已預約' : '0 可用' }}</td><td>{{ lot.packageNo || '—' }}</td><td>{{ lot.components.length }}</td></tr><tr v-if="!draftSourceLots.length"><td colspan="10" class="empty">此來源庫房目前沒有可用 Lot。</td></tr></tbody></table></div>
        <p v-if="formError" class="error" role="alert">{{ formError }}</p>
        <footer><span>儲存只建立草稿並預約來源 Lot／Component，不會過帳庫存。</span><div><button type="button" @click="cancelForm">Cancel</button><button type="button" class="primary" @click="saveDraft">{{ mode === 'add' ? 'Apply' : 'Save' }}</button></div></footer>
      </section>
    </div>
    <div v-if="printOpen" class="overlay" role="presentation" @click.self="printOpen = false"><section class="dialog print-dialog" role="dialog" aria-modal="true" aria-label="入庫單列印預覽"><header><div><p class="eyebrow">PRINT PREVIEW · DEV</p><h2>{{ printTicket?.productInNo || '未選取單據' }}</h2></div><button type="button" class="close" @click="printOpen = false">×</button></header><p v-if="printTicket">{{ typeLabel(printTicket.productInType) }} · {{ inventoryName(printTicket.inventoryNo) }} · {{ printTicket.description || '—' }}</p><div v-if="printTicket" class="table-wrap"><table><thead><tr><th>LotNo</th><th>ProductNo</th><th>Qty</th><th>Unit</th><th>Locator</th></tr></thead><tbody><tr v-for="row in printTicket.details" :key="row.lotNo"><td>{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td>{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td></tr></tbody></table></div><footer><span>僅為 DEV 畫面預覽，不產生正式報表。</span><button type="button" @click="printOpen = false">Close</button></footer></section></div>
  </main>
</template>

<style scoped>
.product-in { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 10px; padding: 14px; box-sizing: border-box; color: #24231e; background: #f3f1e9; font: inherit; overflow: auto; }
.page-head, .page-head > div, .panel-title, .panel-title > div, .toolbar, .source-heading, .source-heading > div, .dialog header, .dialog footer { display: flex; align-items: center; }
.page-head, .panel-title, .dialog header, .dialog footer { justify-content: space-between; gap: 12px; }
.page-head h1, .dialog h2, .panel h2, .source-heading h3 { margin: 0; font-weight: 600; }
.page-head h1 { font-size: 20px; }.eyebrow { margin: 0 0 3px; color: #77715f; font-size: 10px; letter-spacing: .12em; }.sub, .panel-title p { margin: 3px 0 0; color: #6e6b60; font-size: 12px; }
.close { border: 0; background: transparent; font-size: 22px; cursor: pointer; color: #625f54; }.type-switch { display: flex; align-items: center; gap: 6px; padding: 5px; background: #e9e6da; border: 1px solid #d3d0c5; }.type-switch button { padding: 6px 14px; border: 1px solid #c6c2b6; background: #f7f5ee; color: #343229; cursor: pointer; }.type-switch button.active { background: #34342d; border-color: #34342d; color: #fff; }.type-switch span { margin-left: 8px; color: #6b685d; font-size: 12px; }
.toolbar { flex-wrap: wrap; gap: 6px; }.toolbar .spacer { flex: 1; }.toolbar button, .dialog button { min-height: 28px; padding: 4px 10px; border: 1px solid #aaa698; border-radius: 3px; background: #fbfaf6; color: #302f29; font: inherit; cursor: pointer; }.toolbar button.primary, .dialog button.primary { border-color: #35342d; background: #35342d; color: white; }.toolbar button:disabled { opacity: .45; cursor: default; }.query { display: flex; align-items: center; gap: 8px; color: #575448; font-size: 12px; }.query input, .form-grid input:not([type=radio]), .form-grid select { min-height: 28px; padding: 3px 7px; border: 1px solid #c7c3b7; border-radius: 3px; background: #fffefa; color: #26251f; font: inherit; }.query input { width: 200px; }
.notice { margin: 0; min-height: 17px; color: #365b43; font-size: 12px; }.panel { min-height: 0; border: 1px solid #cecabd; background: #fbfaf6; }.basis-panel { flex: 1 1 44%; }.detail-panel { flex: 1 1 42%; }.panel-title { min-height: 34px; padding: 5px 10px; border-bottom: 1px solid #dedbce; }.panel-title h2 { font-size: 14px; }.panel-title > span { color: #6e6b60; font-size: 12px; }.table-wrap { overflow: auto; max-width: 100%; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }th, td { padding: 6px 8px; border: 1px solid #e1ded3; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #efede4; color: #555246; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f2f0e8; }tbody tr.selected { background: #e8ecdf; }.mono { font-family: Consolas, monospace; }.numeric { text-align: right; }.state { color: #6a5322; }.empty { padding: 20px; text-align: center; color: #858173; }
.component-strip { padding: 8px 10px 10px; border-top: 1px solid #dedbce; }.component-strip > strong { margin-right: 10px; font-size: 12px; }.component-strip > span { color: #777366; font-size: 11px; }.component-strip .table-wrap { margin-top: 6px; }.audit { display: flex; gap: 9px; overflow: auto; white-space: nowrap; color: #706d62; font-size: 10px; }.audit span:first-child { color: #4c4a40; font-weight: 600; }
.overlay { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 20px; background: rgb(24 23 20 / 42%); }.dialog { width: min(1080px, 96vw); max-height: 92vh; display: flex; flex-direction: column; gap: 12px; padding: 16px; border: 1px solid #aaa698; border-radius: 5px; background: #f8f6ef; box-shadow: 0 16px 40px rgb(0 0 0 / 24%); }.dialog h2 { font-size: 17px; }.form-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; }.form-grid label { display: flex; flex-direction: column; gap: 4px; color: #555246; font-size: 11px; }.form-grid label input, .form-grid label select { width: 100%; box-sizing: border-box; }.form-grid .wide { grid-column: span 2; }.form-grid fieldset { display: flex; align-items: center; gap: 12px; margin: 0; padding: 5px 9px; border: 1px solid #cbc7bb; color: #555246; font-size: 12px; }.form-grid legend { padding: 0 4px; }.source-heading { justify-content: space-between; gap: 12px; }.source-heading h3 { font-size: 13px; }.source-heading p { margin: 3px 0 0; color: #706d62; font-size: 11px; }.source-heading > span { color: #365b43; font-size: 12px; }.package-actions { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; color: #716d60; font-size: 11px; }.package-actions span { flex: 1; }.package-actions button { min-height: 24px; padding: 2px 8px; }.source-table { flex: 1; min-height: 100px; border: 1px solid #dedbce; }.source-table input { accent-color: #475b41; }.dialog footer { padding-top: 8px; border-top: 1px solid #dedbce; color: #706d62; font-size: 11px; }.dialog footer div { display: flex; gap: 6px; }.error { margin: 0; color: #9c302c; font-size: 12px; }.print-dialog { width: min(760px, 94vw); }.print-dialog footer { justify-content: space-between; }
@media (max-width: 850px) { .form-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }.form-grid .wide { grid-column: span 2; }.type-switch span { flex-basis: 100%; margin: 4px 0 0; }.toolbar .spacer { display: none; } }
</style>

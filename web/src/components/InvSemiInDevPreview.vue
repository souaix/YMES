<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type FormMode = 'add' | 'edit' | null
interface UnitOption { unitNo: string; name: string }
interface ProductOption { productNo: string; productName: string; productVersion: string; unitNo: string; unitOptions: UnitOption[]; conversionTo: Record<string, number> }
interface ComponentRow { componentNo: string; goodQty: number; scrapQty: number; unitNo: string; minLifeGrade: string }
interface DetailRow { rowKey: string; lotNo: string; productNo: string; productName: string; productVersion: string; qty: number; unitNo: string; locatorNo: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number; components: ComponentRow[] }
interface Ticket { semiInNo: string; source: 0; inventoryNo: string; state: number; inputDate: string; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number; details: DetailRow[] }
interface DevState { tickets: Ticket[] }
interface HeaderDraft { semiInNo: string; inventoryNo: string; description: string; dataStamp: number }
interface DetailDraft { rowKey: string; productNo: string; customerLotNo: string; locatorNo: string; qty: number; unitNo: string; componentUnitNo: string; components: ComponentRow[]; dataStamp: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: true })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const inventories = [
  { inventoryNo: 'SEMI-DEV-01', inventoryName: '半成品庫 A', locators: ['N/A', 'S-A-01', 'S-A-02'] },
  { inventoryNo: 'SEMI-DEV-02', inventoryName: '半成品庫 B', locators: ['N/A', 'S-B-01', 'S-B-02'] },
]
const products: ProductOption[] = [
  { productNo: 'PRD-CONTROL-10', productName: '控制板總成', productVersion: 'A2', unitNo: 'EA', unitOptions: [{ unitNo: 'EA', name: '個' }, { unitNo: 'BOX', name: '盒' }, { unitNo: 'PCS', name: '片' }], conversionTo: { EA: 1, BOX: 0.1, PCS: 1 } },
  { productNo: 'PRD-SENSOR-04', productName: '感測器模組', productVersion: 'C1', unitNo: 'EA', unitOptions: [{ unitNo: 'EA', name: '個' }, { unitNo: 'PACK', name: '包' }, { unitNo: 'PCS', name: '片' }], conversionTo: { EA: 1, PACK: 0.2, PCS: 1 } },
  { productNo: 'PRD-PANEL-07', productName: '顯示面板', productVersion: 'B1', unitNo: 'PCS', unitOptions: [{ unitNo: 'PCS', name: '片' }, { unitNo: 'EA', name: '個' }, { unitNo: 'BOX', name: '盒' }], conversionTo: { PCS: 1, EA: 1, BOX: 0.05 } },
]

function seedState(): DevState {
  return {
    tickets: [
      { semiInNo: 'SI-DEV-260001', source: 0, inventoryNo: 'SEMI-DEV-01', state: 0, inputDate: '', description: '外購控制板進料', creator: 'DEV001', createDate: '2026-11-24 08:45:00', reviser: '', reviseDate: '', dataStamp: 1, details: [
        { rowKey: 'SL-26021|S-A-01', lotNo: 'SL-26021', productNo: 'PRD-CONTROL-10', productName: '控制板總成', productVersion: 'A2', qty: 2, unitNo: 'EA', locatorNo: 'S-A-01', creator: 'DEV001', createDate: '2026-11-24 08:50:00', reviser: '', reviseDate: '', dataStamp: 1, components: [{ componentNo: 'CMP-PCB-01', goodQty: 1, scrapQty: 0, unitNo: 'EA', minLifeGrade: '' }, { componentNo: 'CMP-CONN-02', goodQty: 1, scrapQty: 0, unitNo: 'EA', minLifeGrade: '' }] },
      ] },
      { semiInNo: 'SI-DEV-260002', source: 0, inventoryNo: 'SEMI-DEV-02', state: 0, inputDate: '', description: '感測器批次入庫', creator: 'DEV002', createDate: '2026-11-24 13:25:00', reviser: '', reviseDate: '', dataStamp: 1, details: [
        { rowKey: 'VND-88421|N/A', lotNo: 'VND-88421', productNo: 'PRD-SENSOR-04', productName: '感測器模組', productVersion: 'C1', qty: 48, unitNo: 'EA', locatorNo: 'N/A', creator: 'DEV002', createDate: '2026-11-24 13:27:00', reviser: '', reviseDate: '', dataStamp: 1, components: [] },
      ] },
      { semiInNo: 'SI-DEV-259998', source: 0, inventoryNo: 'SEMI-DEV-01', state: 2, inputDate: '2026-11-20 15:05:00', description: '已確認範例（依 IV404 查詢條件不顯示）', creator: 'DEV001', createDate: '2026-11-20 14:30:00', reviser: 'DEV001', reviseDate: '2026-11-20 15:05:00', dataStamp: 2, details: [] },
    ],
  }
}

const state = ref<DevState>(seedState())
const queryNo = ref('')
const activeNo = ref('')
const selectedDetailKey = ref('')
const headerMode = ref<FormMode>(null)
const detailMode = ref<FormMode>(null)
const headerDraft = ref<HeaderDraft>(blankHeader())
const detailDraft = ref<DetailDraft>(blankDetail())
const formError = ref('')
const notice = ref('DEV 假資料：模擬外購半成品進料草稿，不呼叫 MES 或正式 API。')
const deletePending = ref<'header' | 'detail' | null>(null)

const visibleTickets = computed(() => {
  const query = queryNo.value.trim().toLocaleUpperCase()
  return state.value.tickets.filter((ticket) => ticket.source === 0 && ticket.state === 0 && (!query || ticket.semiInNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.semiInNo.localeCompare(b.semiInNo))
})
const activeTicket = computed(() => visibleTickets.value.find((ticket) => ticket.semiInNo === activeNo.value) ?? visibleTickets.value[0] ?? null)
const activeDetails = computed(() => activeTicket.value?.details.slice().sort((a, b) => a.lotNo.localeCompare(b.lotNo) || a.locatorNo.localeCompare(b.locatorNo)) ?? [])
const activeDetail = computed(() => activeDetails.value.find((row) => row.rowKey === selectedDetailKey.value) ?? activeDetails.value[0] ?? null)
const activeComponents = computed(() => activeDetail.value?.components ?? [])
const selectedProduct = computed(() => products.find((product) => product.productNo === detailDraft.value.productNo) ?? null)
const currentInventory = computed(() => inventories.find((inventory) => inventory.inventoryNo === activeTicket.value?.inventoryNo) ?? inventories[0])
const componentTransferQty = computed(() => {
  if (!selectedProduct.value || !detailDraft.value.componentUnitNo) return 0
  return selectedProduct.value.conversionTo[detailDraft.value.componentUnitNo] ?? 1
})
const activeTotal = computed(() => activeDetails.value.reduce((total, row) => total + row.qty, 0))
const stateLabel = (value: number) => value === 0 ? 'UnConfirm' : value === 1 ? 'Handling' : value === 2 ? 'Confirm' : value === 3 ? 'Fail' : 'Other'
const productName = (productNo: string) => products.find((product) => product.productNo === productNo)?.productName ?? productNo
const formatQty = (value: number) => new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)
function vbCInt(value: number): number {
  // Visual Basic CInt rounds midpoint values to the nearest even integer.
  const lower = Math.floor(value)
  const fraction = value - lower
  if (fraction < 0.5) return lower
  if (fraction > 0.5) return lower + 1
  return lower % 2 === 0 ? lower : lower + 1
}

watch(visibleTickets, (tickets) => {
  if (!tickets.some((ticket) => ticket.semiInNo === activeNo.value)) activeNo.value = tickets[0]?.semiInNo ?? ''
}, { immediate: true })
watch(activeDetails, (rows) => {
  if (!rows.some((row) => row.rowKey === selectedDetailKey.value)) selectedDetailKey.value = rows[0]?.rowKey ?? ''
}, { immediate: true })

function blankHeader(): HeaderDraft { return { semiInNo: '', inventoryNo: '', description: '', dataStamp: 0 } }
function blankDetail(): DetailDraft { return { rowKey: '', productNo: products[0].productNo, customerLotNo: '', locatorNo: 'N/A', qty: 1, unitNo: products[0].unitNo, componentUnitNo: '', components: [], dataStamp: 0 } }
function selectTicket(ticket: Ticket): void { activeNo.value = ticket.semiInNo }
function selectDetail(row: DetailRow): void { selectedDetailKey.value = row.rowKey }
function runQuery(): void { notice.value = `查詢完成：Source=0、State=0 的外購半成品進料草稿（${visibleTickets.value.length} 筆）。` }

function openHeader(mode: Exclude<FormMode, null>): void {
  formError.value = ''
  const ticket = activeTicket.value
  if (mode === 'edit' && (!ticket || ticket.state !== 0)) return
  if (mode === 'add') headerDraft.value = { ...blankHeader(), inventoryNo: inventories[0].inventoryNo }
  else if (ticket) headerDraft.value = { semiInNo: ticket.semiInNo, inventoryNo: ticket.inventoryNo, description: ticket.description, dataStamp: ticket.dataStamp }
  headerMode.value = mode
}

function saveHeader(): void {
  formError.value = ''
  const draft = headerDraft.value
  const semiInNo = draft.semiInNo.trim().toLocaleUpperCase()
  if (!semiInNo) { formError.value = 'SEMIInNo 不可空白。'; return }
  if (!draft.inventoryNo) { formError.value = '請選擇半成品庫房。'; return }
  const duplicate = state.value.tickets.find((ticket) => ticket.semiInNo === semiInNo)
  if (headerMode.value === 'add' && duplicate) { formError.value = '單號重複，請輸入另一個單號。'; return }
  if (headerMode.value === 'edit') {
    const ticket = state.value.tickets.find((row) => row.semiInNo === draft.semiInNo)
    if (!ticket || ticket.state !== 0 || ticket.dataStamp !== draft.dataStamp) { formError.value = '單據狀態或版本已變更，請重新查詢後再編輯。'; return }
    ticket.description = draft.description.trim()
    ticket.reviser = 'DEV001'
    ticket.reviseDate = new Date().toLocaleString('sv-SE').replace('T', ' ')
    ticket.dataStamp += 1
    notice.value = `已模擬修改單頭 ${ticket.semiInNo}。`
  } else {
    const ticket: Ticket = { semiInNo, source: 0, inventoryNo: draft.inventoryNo, state: 0, inputDate: '', description: draft.description.trim(), creator: 'DEV001', createDate: new Date().toLocaleString('sv-SE').replace('T', ' '), reviser: '', reviseDate: '', dataStamp: 1, details: [] }
    state.value.tickets.push(ticket)
    queryNo.value = ''
    activeNo.value = semiInNo
    notice.value = `已模擬新增單頭 ${semiInNo}；Source 固定為外購（0），請再新增明細。`
  }
  headerMode.value = null
}

function openDetail(mode: Exclude<FormMode, null>): void {
  const ticket = activeTicket.value
  const row = activeDetail.value
  if (!ticket || ticket.state !== 0 || (mode === 'edit' && !row)) return
  formError.value = ''
  if (mode === 'add') detailDraft.value = { ...blankDetail(), locatorNo: currentInventory.value.locators[0] }
  else if (row) detailDraft.value = { rowKey: row.rowKey, productNo: row.productNo, customerLotNo: row.lotNo, locatorNo: row.locatorNo, qty: row.qty, unitNo: row.unitNo, componentUnitNo: row.components[0]?.unitNo ?? '', components: row.components.map((component) => ({ ...component })), dataStamp: row.dataStamp }
  detailMode.value = mode
}

function changeProduct(productNo: string): void {
  const product = products.find((row) => row.productNo === productNo)
  if (!product) return
  detailDraft.value.productNo = product.productNo
  detailDraft.value.unitNo = product.unitNo
  detailDraft.value.customerLotNo = ''
  detailDraft.value.locatorNo = currentInventory.value.locators[0]
  detailDraft.value.qty = 1
  detailDraft.value.components = []
  detailDraft.value.componentUnitNo = ''
}

function changeComponentUnit(unitNo: string): void {
  detailDraft.value.componentUnitNo = unitNo
  detailDraft.value.components = []
  if (unitNo) notice.value = `單位換算預覽：1 ${detailDraft.value.unitNo} = ${formatQty(componentTransferQty.value)} ${unitNo}（DEV fixture）。`
}

function addComponent(): void {
  const componentNo = componentInput.value.trim().toLocaleUpperCase()
  if (!componentNo) { formError.value = '請輸入 ComponentNo。'; return }
  if (!detailDraft.value.componentUnitNo) { formError.value = '請先選 Component Unit。'; return }
  if (detailDraft.value.components.some((component) => component.componentNo === componentNo)) { formError.value = '此 ComponentNo 已在清單中。'; return }
  detailDraft.value.components.push({ componentNo, goodQty: componentTransferQty.value, scrapQty: 0, unitNo: detailDraft.value.componentUnitNo, minLifeGrade: '' })
  componentInput.value = ''
  formError.value = ''
}

const componentInput = ref('')
function removeComponent(index: number): void { detailDraft.value.components.splice(index, 1) }

function saveDetail(): void {
  const ticket = activeTicket.value
  const product = selectedProduct.value
  formError.value = ''
  if (!ticket || ticket.state !== 0) { formError.value = '只有未確認單據可以維護明細。'; return }
  if (!product) { formError.value = '請選擇 ProductNo。'; return }
  if (!detailDraft.value.customerLotNo.trim()) { formError.value = 'CustomerLotNo 不可空白。'; return }
  if (!detailDraft.value.unitNo) { formError.value = 'UnitNo 不可空白。'; return }
  if (!Number.isFinite(detailDraft.value.qty)) { formError.value = 'Qty 必須是數值。'; return }
  if (!detailDraft.value.locatorNo) { formError.value = '請選擇 LocatorNo。'; return }
  if (detailDraft.value.components.length && vbCInt(detailDraft.value.qty) !== detailDraft.value.components.length) { formError.value = `有 Component 時，Qty 的 CInt 結果必須等於 Component 筆數（${detailDraft.value.components.length}）。`; return }
  const lotNo = detailDraft.value.customerLotNo.trim().toLocaleUpperCase()
  const rowKey = `${lotNo}|${detailDraft.value.locatorNo}`
  const duplicate = ticket.details.some((row) => row.rowKey === rowKey && row.rowKey !== detailDraft.value.rowKey)
  if (duplicate) { formError.value = '同一 CustomerLotNo／LocatorNo 已在此單據。'; return }
  const existing = detailMode.value === 'edit' ? ticket.details.find((row) => row.rowKey === detailDraft.value.rowKey) : null
  if (existing && existing.dataStamp !== detailDraft.value.dataStamp) { formError.value = '明細版本已變更，請重新查詢後再編輯。'; return }
  const saved: DetailRow = {
    rowKey, lotNo, productNo: product.productNo, productName: product.productName, productVersion: product.productVersion,
    qty: detailDraft.value.qty, unitNo: product.unitNo, locatorNo: detailDraft.value.locatorNo,
    creator: existing?.creator ?? 'DEV001', createDate: existing?.createDate ?? new Date().toLocaleString('sv-SE').replace('T', ' '),
    reviser: existing ? 'DEV001' : '', reviseDate: existing ? new Date().toLocaleString('sv-SE').replace('T', ' ') : '',
    dataStamp: (existing?.dataStamp ?? 0) + 1, components: detailDraft.value.components.map((component) => ({ ...component })),
  }
  if (existing) ticket.details = ticket.details.map((row) => row.rowKey === existing.rowKey ? saved : row)
  else ticket.details.push(saved)
  selectedDetailKey.value = saved.rowKey
  detailMode.value = null
  notice.value = `已模擬${existing ? '修改' : '新增'} ${saved.productNo}／${saved.lotNo} 明細；正式庫存須等 IV401 確認後入帳。`
}

function deleteHeader(): void {
  const ticket = activeTicket.value
  if (!ticket || ticket.state !== 0) return
  state.value.tickets = state.value.tickets.filter((row) => row.semiInNo !== ticket.semiInNo)
  activeNo.value = ''
  notice.value = `已模擬刪除未確認單頭 ${ticket.semiInNo} 及其明細。`
}
function deleteDetail(): void {
  const ticket = activeTicket.value
  const row = activeDetail.value
  if (!ticket || !row || ticket.state !== 0) return
  ticket.details = ticket.details.filter((item) => item.rowKey !== row.rowKey)
  notice.value = `已模擬刪除明細 ${row.lotNo}／${row.locatorNo}。`
}
function confirmDelete(): void {
  const pending = deletePending.value
  deletePending.value = null
  if (pending === 'header') deleteHeader()
  if (pending === 'detail') deleteDetail()
}
function closeDialogs(): void { headerMode.value = null; detailMode.value = null; deletePending.value = null; formError.value = ''; componentInput.value = '' }
</script>

<template>
  <section class="semi-in" aria-label="開立半成品進料單 DEV 預覽">
    <header class="semi-in__topbar"><div><div class="semi-in__eyebrow">INVENTORY / SEMI INV</div><h1>開立半成品進料單 <span>{{ previewLabel }}</span></h1></div><button type="button" class="semi-in__close" @click="emit('close')">Close (X)</button></header>
    <div class="semi-in__toolbar">
      <label>SEMIInNo <input v-model="queryNo" aria-label="SEMIInNo 查詢" @keyup.enter="runQuery" /></label>
      <button type="button" class="semi-in__button" @click="runQuery">Find</button><i></i>
      <button type="button" class="semi-in__button" @click="openHeader('add')">ADD</button>
      <button type="button" class="semi-in__button" :disabled="!activeTicket" @click="openHeader('edit')">EDIT</button>
      <button type="button" class="semi-in__button semi-in__button--danger" :disabled="!activeTicket" @click="deletePending = 'header'">DEL</button>
      <small>條件：Source = 0（外購）、State = UnConfirm（0）</small>
    </div>
    <div class="semi-in__notice" role="status">{{ notice }}</div>
    <div class="semi-in__section-head"><h2>SEMI In Basis</h2><span>{{ visibleTickets.length }} 筆草稿</span></div>
    <div class="semi-in__grid-wrap semi-in__basis-grid"><table class="semi-in__grid"><thead><tr><th>SEMIInNo</th><th>InventoryNo</th><th>State</th><th>InputDate</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
      <tr v-for="ticket in visibleTickets" :key="ticket.semiInNo" :class="{ 'is-active': activeTicket?.semiInNo === ticket.semiInNo }" @click="selectTicket(ticket)"><td>{{ ticket.semiInNo }}</td><td>{{ ticket.inventoryNo }}</td><td><span class="semi-in__state">{{ stateLabel(ticket.state) }}</span></td><td>{{ ticket.inputDate || '—' }}</td><td>{{ ticket.description }}</td><td>{{ ticket.creator }}</td><td>{{ ticket.createDate }}</td><td>{{ ticket.reviser || '—' }}</td><td>{{ ticket.reviseDate || '—' }}</td></tr>
      <tr v-if="!visibleTickets.length"><td colspan="9" class="semi-in__empty">沒有符合條件的外購半成品進料單。</td></tr>
    </tbody></table></div>

    <div class="semi-in__section-head semi-in__detail-head"><div><h2>SEMI In Detail</h2><small v-if="activeTicket">{{ activeTicket.semiInNo }} · {{ activeTicket.inventoryNo }}</small></div><div class="semi-in__detail-actions"><button type="button" class="semi-in__button" :disabled="!activeTicket" @click="openDetail('add')">ADD</button><button type="button" class="semi-in__button" :disabled="!activeDetail" @click="openDetail('edit')">EDIT</button><button type="button" class="semi-in__button semi-in__button--danger" :disabled="!activeDetail" @click="deletePending = 'detail'">DEL</button></div></div>
    <div class="semi-in__grid-wrap semi-in__detail-grid"><table class="semi-in__grid"><thead><tr><th>ProductNo</th><th>ProductName</th><th>LotNo</th><th class="is-number">Qty</th><th>UnitNo</th><th>LocatorNo</th><th>Creator</th><th>CreateDate</th><th>Component</th></tr></thead><tbody>
      <tr v-for="row in activeDetails" :key="row.rowKey" :class="{ 'is-active': activeDetail?.rowKey === row.rowKey }" @click="selectDetail(row)"><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.lotNo }}</td><td class="is-number">{{ formatQty(row.qty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.locatorNo }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.components.length ? `${row.components.length} 筆` : '—' }}</td></tr>
      <tr v-if="!activeDetails.length"><td colspan="9" class="semi-in__empty">{{ activeTicket ? '此單據尚無明細。' : '請先選取一張進料單。' }}</td></tr>
    </tbody></table></div>
    <div class="semi-in__footer"><div class="semi-in__component-title"><strong>選取明細 Component</strong><span v-if="activeDetail">{{ activeDetail.productNo }}／{{ activeDetail.lotNo }} · {{ activeComponents.length }} 筆</span><span v-else>選取一筆明細後顯示元件資料。</span></div><div class="semi-in__component-grid"><table class="semi-in__grid"><thead><tr><th>ComponentNo</th><th class="is-number">GoodQty</th><th class="is-number">ScrapQty</th><th>UnitNo</th><th>MinLifeGrade</th></tr></thead><tbody><tr v-for="component in activeComponents" :key="component.componentNo"><td>{{ component.componentNo }}</td><td class="is-number">{{ formatQty(component.goodQty) }}</td><td class="is-number">{{ formatQty(component.scrapQty) }}</td><td>{{ component.unitNo }}</td><td>{{ component.minLifeGrade || '—' }}</td></tr><tr v-if="!activeComponents.length"><td colspan="5" class="semi-in__empty">沒有 Component 假資料。</td></tr></tbody></table></div><div class="semi-in__total">草稿明細合計<strong>{{ formatQty(activeTotal) }}</strong></div></div>

    <div v-if="headerMode" class="semi-in__overlay" role="presentation" @click.self="closeDialogs"><section class="semi-in__dialog" role="dialog" aria-modal="true" aria-label="維護進料單單頭"><header><div><small>SEMI IN BASIS · SOURCE 0</small><h2>{{ headerMode === 'edit' ? '編輯' : '新增' }}外購進料單單頭</h2></div><button type="button" @click="closeDialogs">×</button></header>
      <label>SEMIInNo <input v-model="headerDraft.semiInNo" :readonly="headerMode === 'edit'" placeholder="請輸入進料單號" /></label><label>InventoryNo <select v-model="headerDraft.inventoryNo" :disabled="headerMode === 'edit'"><option value="" disabled>選擇半成品庫房</option><option v-for="inventory in inventories" :key="inventory.inventoryNo" :value="inventory.inventoryNo">{{ inventory.inventoryNo }} · {{ inventory.inventoryName }}</option></select></label><label>Description <textarea v-model="headerDraft.description" rows="3" /></label><p class="semi-in__hint">IV404 的 Source 固定為 0（外購）；新增單據 State 固定為 UnConfirm。InventoryNo 在編輯時不可變更。</p><p v-if="formError" class="semi-in__error" role="alert">{{ formError }}</p><footer><button type="button" class="semi-in__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-in__button semi-in__button--primary" @click="saveHeader">Save</button></footer></section></div>

    <div v-if="detailMode" class="semi-in__overlay" role="presentation" @click.self="closeDialogs"><section class="semi-in__dialog semi-in__dialog--wide" role="dialog" aria-modal="true" aria-label="維護半成品進料明細"><header><div><small>{{ activeTicket?.semiInNo }} · SOURCE 0</small><h2>{{ detailMode === 'edit' ? '編輯' : '新增' }}進料明細</h2></div><button type="button" @click="closeDialogs">×</button></header>
      <div class="semi-in__form-grid"><label>ProductNo <select :value="detailDraft.productNo" :disabled="detailMode === 'edit'" @change="changeProduct(($event.target as HTMLSelectElement).value)"><option value="" disabled>選擇產品</option><option v-for="product in products" :key="product.productNo" :value="product.productNo">{{ product.productNo }} · {{ product.productName }} · {{ product.productVersion }}</option></select></label><label>Product UnitNo <input :value="selectedProduct?.unitNo ?? ''" readonly /></label><label>CustomerLotNo <input v-model="detailDraft.customerLotNo" :readonly="detailMode === 'edit'" placeholder="輸入供應商批號" /></label><label>LocatorNo <select v-model="detailDraft.locatorNo" :disabled="detailMode === 'edit'"><option v-for="locator in currentInventory.locators" :key="locator" :value="locator">{{ locator }}</option></select></label><label>Qty <input v-model.number="detailDraft.qty" type="number" step="0.0001" /></label><label>InventoryNo <input :value="activeTicket?.inventoryNo ?? ''" readonly /></label></div>
      <fieldset><legend>Component（選填，手動輸入）</legend><div class="semi-in__component-entry"><label>Component Unit <select :value="detailDraft.componentUnitNo" @change="changeComponentUnit(($event.target as HTMLSelectElement).value)"><option value="">請選換算單位</option><option v-for="unit in selectedProduct?.unitOptions ?? []" :key="unit.unitNo" :value="unit.unitNo">{{ unit.unitNo }} · {{ unit.name }}</option></select></label><span class="semi-in__factor">換算：1 {{ detailDraft.unitNo }} → {{ formatQty(componentTransferQty) }} {{ detailDraft.componentUnitNo || '—' }}</span><label>ComponentNo <input v-model="componentInput" @keyup.enter.prevent="addComponent" placeholder="輸入元件編號" /></label><button type="button" class="semi-in__button" @click="addComponent">Add Component</button></div>
        <div class="semi-in__component-draft"><table class="semi-in__grid"><thead><tr><th>刪除</th><th>ComponentNo</th><th class="is-number">GoodQty</th><th class="is-number">ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="(component, index) in detailDraft.components" :key="component.componentNo"><td><button type="button" class="semi-in__remove" @click="removeComponent(index)">×</button></td><td>{{ component.componentNo }}</td><td class="is-number">{{ formatQty(component.goodQty) }}</td><td class="is-number">0</td><td>{{ component.unitNo }}</td></tr><tr v-if="!detailDraft.components.length"><td colspan="5" class="semi-in__empty">尚未加入元件；沒有元件時以一般進料明細儲存。</td></tr></tbody></table></div></fieldset>
      <p class="semi-in__hint">原 Form 以 Product 的目前版 UnitNo 做基準；可選 Component Unit 後以 Unit Transfer Qty 計算每筆 GoodQty。若有元件列，舊程式要求整數 Qty 等於元件列數；正式入庫仍由 IV401 SEMI In 確認流程處理。</p><p v-if="formError" class="semi-in__error" role="alert">{{ formError }}</p><footer><button type="button" class="semi-in__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-in__button semi-in__button--primary" @click="saveDetail">Save</button></footer></section></div>

    <div v-if="deletePending" class="semi-in__overlay" role="presentation" @click.self="closeDialogs"><section class="semi-in__dialog" role="alertdialog" aria-modal="true" aria-label="確認刪除"><header><div><small>CONFIRM DELETE</small><h2>刪除{{ deletePending === 'header' ? '進料單及其明細' : '進料明細' }}？</h2></div><button type="button" @click="closeDialogs">×</button></header><p class="semi-in__hint">只會修改本頁 DEV 假資料，不會增加半成品庫存。</p><footer><button type="button" class="semi-in__button" @click="closeDialogs">Cancel</button><button type="button" class="semi-in__button semi-in__button--danger" @click="confirmDelete">Delete</button></footer></section></div>
  </section>
</template>

<style scoped>
.semi-in { min-height: 0; height: 100%; box-sizing: border-box; display: flex; flex-direction: column; gap: 8px; padding: 12px; overflow: auto; background: #f4f2ea; color: #24231e; font: inherit; }
.semi-in__topbar { display: flex; align-items: center; justify-content: space-between; min-height: 44px; }
.semi-in__topbar h1 { margin: 2px 0 0; font-size: 20px; line-height: 1.2; }
.semi-in__topbar h1 span { margin-left: 8px; color: #786c42; font-size: 12px; font-weight: 500; }
.semi-in__eyebrow { color: #817a69; font-size: 10px; letter-spacing: .12em; }
.semi-in__close, .semi-in__button { min-height: 28px; border: 1px solid #777366; border-radius: 3px; background: #fffefa; color: #292820; padding: 4px 10px; font: inherit; cursor: pointer; }
.semi-in__button--primary { border-color: #344e42; background: #344e42; color: #fff; }
.semi-in__button--danger { border-color: #a35a4c; color: #913e32; }
.semi-in button:disabled { opacity: .45; cursor: default; }
.semi-in__toolbar { display: flex; align-items: end; gap: 6px; flex-wrap: wrap; padding: 8px; border: 1px solid #d2cdbd; background: #eae7dc; }
.semi-in__toolbar label { display: grid; gap: 3px; color: #575448; font-size: 11px; }
.semi-in input, .semi-in select, .semi-in textarea { min-height: 30px; box-sizing: border-box; border: 1px solid #c9c4b5; border-radius: 2px; background: #fff; color: #28271f; padding: 4px 7px; font: inherit; }
.semi-in__toolbar input { min-width: 210px; }
.semi-in__toolbar i { height: 27px; margin: 0 3px; border-left: 1px solid #c1bdaf; }
.semi-in__toolbar small { margin: 0 0 6px auto; color: #6a665b; font-size: 11px; }
.semi-in__notice { min-height: 20px; color: #615d50; font-size: 12px; }
.semi-in__section-head { display: flex; align-items: baseline; justify-content: space-between; padding: 0 2px; }
.semi-in__section-head h2 { margin: 0; font-size: 14px; }
.semi-in__section-head > span, .semi-in__section-head small { color: #777366; font-size: 11px; }
.semi-in__grid-wrap { min-height: 80px; overflow: auto; border: 1px solid #cec9ba; background: #fffefa; }
.semi-in__basis-grid { max-height: 220px; }
.semi-in__detail-grid { max-height: 210px; }
.semi-in__grid { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }
.semi-in__grid th { position: sticky; top: 0; z-index: 1; background: #e4e1d5; color: #4d4a40; text-align: left; font-weight: 600; }
.semi-in__grid th, .semi-in__grid td { padding: 6px 8px; border-right: 1px solid #e5e1d5; border-bottom: 1px solid #e5e1d5; }
.semi-in__grid tbody tr { cursor: pointer; }
.semi-in__grid tbody tr:hover { background: #f5f2e8; }
.semi-in__grid tbody tr.is-active { background: #e5eadf; }
.semi-in__grid .is-number { text-align: right; font-variant-numeric: tabular-nums; }
.semi-in__empty { padding: 18px !important; color: #7c786c; text-align: center; }
.semi-in__state { display: inline-block; padding: 2px 6px; border: 1px solid #c4b979; border-radius: 10px; background: #f4efd3; color: #67591f; font-size: 10px; }
.semi-in__detail-head { margin-top: 1px; }
.semi-in__detail-head > div:first-child { display: flex; align-items: baseline; gap: 9px; }
.semi-in__detail-actions { display: flex; gap: 5px; }
.semi-in__footer { display: grid; grid-template-columns: 1fr minmax(280px, 42%) auto; align-items: stretch; gap: 8px; margin-top: auto; }
.semi-in__component-title, .semi-in__total { display: flex; flex-direction: column; justify-content: center; gap: 5px; padding: 9px; border: 1px solid #d2cdbd; background: #eeece3; font-size: 11px; }
.semi-in__component-title span { color: #716d60; }
.semi-in__component-grid { max-height: 125px; overflow: auto; border: 1px solid #cec9ba; background: #fffefa; }
.semi-in__component-grid .semi-in__grid th, .semi-in__component-grid .semi-in__grid td { padding: 4px 7px; }
.semi-in__total { min-width: 105px; align-items: flex-end; color: #706c60; }
.semi-in__total strong { color: #26251f; font-size: 17px; }
.semi-in__overlay { position: fixed; inset: 0; z-index: 30; display: grid; place-items: center; padding: 20px; background: rgb(24 24 20 / 46%); }
.semi-in__dialog { width: min(520px, 100%); max-height: min(90vh, 760px); overflow: auto; box-sizing: border-box; padding: 18px; border: 1px solid #c5c0b2; border-radius: 4px; background: #f8f6ef; box-shadow: 0 18px 55px rgb(0 0 0 / 20%); }
.semi-in__dialog--wide { width: min(800px, 100%); }
.semi-in__dialog > header { display: flex; align-items: center; justify-content: space-between; margin: -2px 0 16px; }
.semi-in__dialog > header small { color: #807a69; font-size: 10px; letter-spacing: .1em; }
.semi-in__dialog h2 { margin: 3px 0 0; font-size: 18px; }
.semi-in__dialog > header button { width: 30px; height: 30px; border: 0; background: transparent; color: #625f54; font-size: 23px; cursor: pointer; }
.semi-in__dialog > label, .semi-in__form-grid label { display: grid; gap: 4px; margin: 10px 0; color: #5c584d; font-size: 12px; }
.semi-in__dialog textarea { resize: vertical; }
.semi-in__form-grid { display: grid; grid-template-columns: 1fr 1fr; column-gap: 12px; }
.semi-in__dialog fieldset { margin: 12px 0; padding: 10px; border: 1px solid #d2cdbd; }
.semi-in__dialog legend { padding: 0 5px; color: #5c584d; font-size: 12px; }
.semi-in__component-entry { display: grid; grid-template-columns: minmax(130px, .85fr) minmax(150px, 1fr) minmax(170px, 1.2fr) auto; align-items: end; gap: 8px; }
.semi-in__component-entry label { display: grid; gap: 4px; color: #5c584d; font-size: 11px; }
.semi-in__factor { align-self: center; padding-top: 14px; color: #6a6556; font-size: 11px; white-space: nowrap; }
.semi-in__component-draft { max-height: 150px; overflow: auto; margin-top: 9px; border: 1px solid #d2cdbd; background: #fffefa; }
.semi-in__remove { width: 24px; height: 22px; border: 1px solid #c9c4b5; background: #fff; color: #9a4538; cursor: pointer; }
.semi-in__hint { margin: 10px 0; padding: 8px 9px; border-left: 3px solid #b3a76c; background: #efeddf; color: #6a6554; font-size: 11px; line-height: 1.45; }
.semi-in__error { padding: 8px 10px; border: 1px solid #d9a59a; background: #fbefec; color: #9a3c30; font-size: 12px; }
.semi-in__dialog > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 15px; }
@media (max-width: 820px) { .semi-in__footer { grid-template-columns: 1fr; } .semi-in__toolbar small { width: 100%; margin: 0; } .semi-in__component-entry { grid-template-columns: 1fr 1fr; } }
@media (max-width: 560px) { .semi-in { padding: 8px; } .semi-in__form-grid { grid-template-columns: 1fr; } .semi-in__component-entry { grid-template-columns: 1fr; } .semi-in__detail-head { align-items: flex-start; gap: 8px; flex-direction: column; } .semi-in__detail-actions { flex-wrap: wrap; } }
</style>

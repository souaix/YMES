<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Action = 0 | 1 | 2
type ReturnType = 0 | 1 | 2
type Lot = {
  carrierNo: string; lotNo: string; curQty: number; lotStamp: number; status: number
  haveComponent: boolean; productNo: string; productVersion: string; processNo: string; opNo: string
  components: string[]
}
type Row = {
  splitLotNo: string; splitLotQty: number; carrierNo: string; mother: boolean
  action: Action; returnType: ReturnType; nodeId: string; processNo: string; processVersion: string; opNo: string
  productNo: string; productVersion: string
}
type Slot = { position: number; componentNo: string; originPosition: number }
type SplitRecord = {
  lotNo: string; carrierNo: string; lotStamp: number; rows: Row[]
  slots: [Slot[], Slot[]] | null; savedAt: string
}
type Persisted = { records: SplitRecord[]; nextSequence: number }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-lot-split-carrier:state:v1'

const fixtures: Lot[] = [
  { carrierNo: 'DEV-CAR-101', lotNo: 'DEV-LOT-CAR-101', curQty: 12, lotStamp: 2101, status: 1, haveComponent: false, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ASSY', opNo: 'DEV-OP-10', components: [] },
  { carrierNo: 'DEV-CAR-202', lotNo: 'DEV-LOT-CAR-202', curQty: 4, lotStamp: 2102, status: 1, haveComponent: true, productNo: 'DEV-PRODUCT-B', productVersion: '02', processNo: 'DEV-PROC-CP', opNo: 'DEV-OP-20', components: ['DEV-COMP-01', 'DEV-COMP-02', 'DEV-COMP-03', 'DEV-COMP-04'] },
  { carrierNo: 'DEV-CAR-HOLD', lotNo: 'DEV-LOT-CAR-HOLD', curQty: 6, lotStamp: 2103, status: 3, haveComponent: false, productNo: 'DEV-PRODUCT-A', productVersion: '01', processNo: 'DEV-PROC-ASSY', opNo: 'DEV-OP-10', components: [] },
]
const emptyCarriers = [
  { carrierNo: 'DEV-CAR-EMPTY-1', carrierType: 'DEV-TRAY', capacity: 6 },
  { carrierNo: 'DEV-CAR-EMPTY-2', carrierType: 'DEV-TRAY', capacity: 4 },
  { carrierNo: 'DEV-CAR-EMPTY-3', carrierType: 'DEV-TRAY', capacity: 8 },
]
const routes = [
  { action: 1 as Action, processNo: 'DEV-PROC-ASSY', processVersion: '02', opNo: 'DEV-OP-30', nodeId: 'DEV-NODE-30', label: 'Jump OP · DEV-OP-30' },
  { action: 2 as Action, processNo: 'DEV-PROC-PACK', processVersion: '01', opNo: 'DEV-OP-PACK', nodeId: 'DEV-NODE-PACK', label: 'Jump Process · DEV-PROC-PACK' },
]
const products = [
  { productNo: 'DEV-PRODUCT-A', productVersion: '01' },
  { productNo: 'DEV-PRODUCT-B', productVersion: '02' },
]

const carrierInput = ref('')
const lot = ref<Lot | null>(null)
const mode = ref<'count' | 'qty'>('count')
const amountInput = ref('2')
const includeMother = ref(true)
const rows = ref<Row[]>([])
const activeLotNo = ref('')
const dialog = ref<'slots' | 'route' | 'product' | 'carrierLookup' | null>(null)
const carrierPickerSide = ref<0 | 1 | null>(null)
const slots = ref<[Slot[], Slot[]]>([[], []])
const slotCarriers = ref<[string, string]>(['', ''])
const selectedSlot1 = ref<number | null>(null)
const selectedSlot2 = ref<number | null>(null)
const slotsConfirmed = ref(false)
const settingsSnapshot = ref('')
const records = ref<SplitRecord[]>([])
const nextSequence = ref(1)
const submitted = ref(false)
const errorText = ref('')
const message = ref('請輸入參考載具編號，查詢後設定分批。')

const activeRow = computed(() => rows.value.find(row => row.splitLotNo === activeLotNo.value) ?? null)
const canEdit = computed(() => dev.value && lot.value !== null && !submitted.value)
const totalQty = computed(() => rows.value.reduce((sum, row) => sum + row.splitLotQty, 0))
const slotCounts = computed(() => [slots.value[0].filter(slot => !!slot.componentNo).length, slots.value[1].filter(slot => !!slot.componentNo).length])
const availableEmptyCarriers = computed(() => {
  const occupied = new Set(records.value.flatMap(record => record.rows.map(row => row.carrierNo)))
  return emptyCarriers.filter(item => !occupied.has(item.carrierNo))
})
const sourceCarrierOptions = computed(() => lot.value ? [{ carrierNo: lot.value.carrierNo, carrierType: 'DEV-TRAY', capacity: 6 }, ...availableEmptyCarriers.value] : availableEmptyCarriers.value)

function readState(): Persisted {
  const raw = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as Partial<Persisted> | null
  const next = raw?.nextSequence
  const rawRecords = raw?.records
  return { records: Array.isArray(rawRecords) ? rawRecords : [], nextSequence: typeof next === 'number' && Number.isSafeInteger(next) && next > 0 ? next : 1 }
}
function loadState(): void {
  if (!dev.value) return
  try { const saved = readState(); records.value = saved.records; nextSequence.value = saved.nextSequence }
  catch { records.value = []; nextSequence.value = 1 }
}
function clearDraft(): void {
  rows.value = []; activeLotNo.value = ''; slots.value = [[], []]; slotCarriers.value = ['', '']
  selectedSlot1.value = null; selectedSlot2.value = null; slotsConfirmed.value = false
  settingsSnapshot.value = ''; submitted.value = false; dialog.value = null; carrierPickerSide.value = null
}
function queryCarrier(): void {
  clearDraft(); errorText.value = ''; lot.value = null
  if (!dev.value) { errorText.value = '此頁目前僅提供 DEV 假資料預覽。'; return }
  const source = fixtures.find(item => item.carrierNo === carrierInput.value.trim().toUpperCase())
  if (!source) { errorText.value = '找不到有批次的 DEV 載具；可試 DEV-CAR-101 或 DEV-CAR-202。'; return }
  if ([2, 3, 4].includes(source.status)) { errorText.value = '參考批為 Wait／Hold／Complete，不允許載具分批。'; return }
  if (records.value.some(item => item.lotNo === source.lotNo && item.lotStamp === source.lotStamp)) { errorText.value = '此 DEV 批次已在本瀏覽器模擬分批。'; return }
  lot.value = source; carrierInput.value = source.carrierNo; mode.value = 'count'; amountInput.value = '2'; includeMother.value = true
  message.value = source.haveComponent ? '有元件批：固定以 Count 2 拆分，Slot 視窗須分配每顆元件。' : '無元件批：可選 Count／Qty 並指定每個子批的載具。'
}
function chooseReferenceCarrier(carrierNo: string): void { carrierInput.value = carrierNo; queryCarrier() }
function childLotNo(sequence: number): string { return `DEV-CAR-SPLIT-${String(sequence).padStart(6, '0')}` }
function emptySlots(capacity: number): Slot[] { return Array.from({ length: capacity }, (_, i) => ({ position: i + 1, componentNo: '', originPosition: 0 })) }
function carrierCapacity(carrierNo: string): number { return sourceCarrierOptions.value.find(item => item.carrierNo === carrierNo)?.capacity ?? 0 }
function setupSlots(source: Lot): void {
  const left = emptySlots(carrierCapacity(source.carrierNo))
  source.components.forEach((componentNo, i) => { if (left[i]) { left[i].componentNo = componentNo; left[i].originPosition = i + 1 } })
  slots.value = [left, []]
  slotCarriers.value = [source.carrierNo, '']
  selectedSlot1.value = 1; selectedSlot2.value = null; slotsConfirmed.value = false; carrierPickerSide.value = null
  dialog.value = 'slots'
}
function split(): void {
  errorText.value = ''
  const source = lot.value
  if (!canEdit.value || !source) { errorText.value = '請先查詢有批次的參考載具。'; return }
  const amount = Number(amountInput.value)
  if (!Number.isInteger(amount) || amount <= 0) { errorText.value = 'DEV Count／Qty 請輸入大於 0 的整數。'; return }
  if (source.haveComponent && (mode.value !== 'count' || amount !== 2)) { errorText.value = '有元件批只允許 Count 2。'; return }
  const quantities: number[] = []
  if (mode.value === 'count') {
    if (amount < 2 || amount > source.curQty) { errorText.value = '批數至少 2，且不可超過 CurQty。'; return }
    const base = Math.floor(source.curQty / amount)
    for (let i = 0; i < amount; i++) quantities.push(i === amount - 1 ? source.curQty - base * (amount - 1) : base)
  } else {
    if (amount >= source.curQty) { errorText.value = 'Split Qty 必須小於 CurQty。'; return }
    const full = Math.floor(source.curQty / amount)
    for (let i = 0; i < full; i++) quantities.push(amount)
    const last = source.curQty - full * amount
    if (last > 0) quantities.push(last)
  }
  if (quantities.length < 2 || quantities.length > 100) { errorText.value = 'DEV 最多 100 列，至少 2 列。'; return }
  let sequence = nextSequence.value
  rows.value = quantities.map((qty, i) => ({
    splitLotNo: includeMother.value && i === 0 ? source.lotNo : childLotNo(sequence++),
    splitLotQty: qty, carrierNo: includeMother.value && i === 0 && !source.haveComponent ? source.carrierNo : '',
    mother: includeMother.value && i === 0, action: 0 as Action, returnType: 0 as ReturnType,
    nodeId: '', processNo: '', processVersion: '', opNo: '', productNo: source.productNo, productVersion: source.productVersion,
  }))
  activeLotNo.value = rows.value[0]?.splitLotNo || ''
  slots.value = [[], []]; slotsConfirmed.value = false
  settingsSnapshot.value = `${mode.value}|${amountInput.value}|${includeMother.value}`
  if (source.haveComponent) { setupSlots(source); message.value = '請在 Slot 視窗選擇第二載具並將元件移至空位置。' }
  else message.value = `已產生 ${rows.value.length} 個子批；請逐列指定載具。`
}
function updateQty(row: Row, raw: string): void {
  const value = Number(raw)
  if (!Number.isInteger(value) || value < 0) { errorText.value = 'Split Lot Qty 必須是非負整數。'; return }
  row.splitLotQty = rows.value.length === 2 && lot.value ? Math.min(value, lot.value.curQty) : value
  if (rows.value.length === 2 && lot.value) {
    const other = rows.value.find(item => item !== row)
    if (other) other.splitLotQty = lot.value.curQty - row.splitLotQty
  }
  errorText.value = ''
}
function changeAction(row: Row, raw: string): void {
  row.action = Number(raw) as Action; row.nodeId = ''; row.processNo = ''; row.processVersion = ''; row.opNo = ''
}
function changeReturnType(row: Row, raw: string): void { row.returnType = Number(raw) as ReturnType }
function chooseRoute(option: (typeof routes)[number]): void {
  const row = activeRow.value
  if (!row) return
  row.action = option.action; row.nodeId = option.nodeId; row.processNo = option.processNo
  row.processVersion = option.processVersion; row.opNo = option.opNo; dialog.value = null
}
function chooseProduct(option: (typeof products)[number]): void {
  const row = activeRow.value
  if (!row) return
  row.productNo = option.productNo; row.productVersion = option.productVersion; dialog.value = null
  message.value = 'DEV 已顯示產品選擇；原 WinForms Confirm XML 未送出 ProductNo／Version，正式相容性待確認。'
}
function chooseSlotCarrier(side: 0 | 1, carrierNo: string): void {
  errorText.value = ''
  if (carrierNo && carrierNo === slotCarriers.value[side === 0 ? 1 : 0]) { errorText.value = '兩個子批不可使用同一載具。'; return }
  const capacity = carrierCapacity(carrierNo)
  if (carrierNo && capacity === 0) { errorText.value = '載具不存在於 DEV 可用清單。'; return }
  const current = slots.value[side]
  if (current.some(slot => !!slot.componentNo && slot.position > capacity)) { errorText.value = '目標載具容量不足，請先調整元件位置。'; return }
  const replacement = emptySlots(capacity)
  for (const slot of current) if (slot.componentNo && replacement[slot.position - 1]) replacement[slot.position - 1] = { ...slot }
  slots.value[side] = replacement; slotCarriers.value[side] = carrierNo; slotsConfirmed.value = false
  carrierPickerSide.value = null
}
function chooseSlotCarrierFromPicker(carrierNo: string): void {
  if (carrierPickerSide.value !== null) chooseSlotCarrier(carrierPickerSide.value, carrierNo)
}
function moveComponent(fromSide: 0 | 1): void {
  errorText.value = ''
  const toSide: 0 | 1 = fromSide === 0 ? 1 : 0
  const fromPos = fromSide === 0 ? selectedSlot1.value : selectedSlot2.value
  const toPos = toSide === 0 ? selectedSlot1.value : selectedSlot2.value
  if (!slotCarriers.value[toSide] || !fromPos || !toPos) { errorText.value = '請選擇目標載具與兩側的位置。'; return }
  const from = slots.value[fromSide].find(slot => slot.position === fromPos)
  const to = slots.value[toSide].find(slot => slot.position === toPos)
  if (!from?.componentNo || !to || to.componentNo) { errorText.value = '來源須有元件，目標須是空位置。'; return }
  to.componentNo = from.componentNo; to.originPosition = from.originPosition
  from.componentNo = ''; from.originPosition = 0; slotsConfirmed.value = false
}
function moveWithin(side: 0 | 1, direction: -1 | 1): void {
  errorText.value = ''
  const selected = side === 0 ? selectedSlot1 : selectedSlot2
  const position = selected.value
  if (!position) return
  const from = slots.value[side].find(slot => slot.position === position)
  const to = slots.value[side].find(slot => slot.position === position + direction)
  if (!from || !to) return
  const componentNo = from.componentNo, originPosition = from.originPosition
  from.componentNo = to.componentNo; from.originPosition = to.originPosition
  to.componentNo = componentNo; to.originPosition = originPosition
  selected.value = to.position; slotsConfirmed.value = false
}
function confirmSlots(): void {
  errorText.value = ''
  const source = lot.value
  if (!source || rows.value.length !== 2 || !slotCarriers.value[0] || !slotCarriers.value[1] || slotCarriers.value[0] === slotCarriers.value[1]) { errorText.value = '請先選兩個不同的載具與兩個子批。'; return }
  if (!slotCounts.value[0] || !slotCounts.value[1]) { errorText.value = '兩個子批載具都必須有元件。'; return }
  const assigned = slots.value.flatMap(side => side.filter(slot => !!slot.componentNo).map(slot => slot.componentNo))
  if (assigned.length !== source.components.length || new Set(assigned).size !== source.components.length || assigned.some(id => !source.components.includes(id))) { errorText.value = '每個原始元件都必須且只能分配一次。'; return }
  rows.value[0]!.splitLotQty = slotCounts.value[0]!
  rows.value[1]!.splitLotQty = slotCounts.value[1]!
  rows.value[0]!.carrierNo = slotCarriers.value[0]
  rows.value[1]!.carrierNo = slotCarriers.value[1]
  slotsConfirmed.value = true; dialog.value = null
  message.value = 'Slot 分配已套用；請核對子批數量與 CarrierNo 後確認。'
}
function confirmSplit(): void {
  errorText.value = ''
  const source = lot.value
  if (!canEdit.value || !source || rows.value.length < 2) { errorText.value = '請先查詢參考載具並按 Split。'; return }
  if (settingsSnapshot.value !== `${mode.value}|${amountInput.value}|${includeMother.value}`) { errorText.value = '分批條件已變更，請重新按 Split。'; return }
  if (source.haveComponent && !slotsConfirmed.value) { errorText.value = '有元件批須先在 Slot 視窗確認元件分配。'; return }
  const carriers = new Set<string>()
  for (const row of rows.value) {
    if (!Number.isInteger(row.splitLotQty) || row.splitLotQty <= 0) { errorText.value = `${row.splitLotNo} 的 Split Lot Qty 必須大於 0。`; return }
    if (!row.carrierNo || !sourceCarrierOptions.value.some(item => item.carrierNo === row.carrierNo)) { errorText.value = `${row.splitLotNo} 請指定可用載具。`; return }
    if (carriers.has(row.carrierNo)) { errorText.value = '每個子批必須使用不同載具。'; return }
    carriers.add(row.carrierNo)
    if (!row.mother && row.carrierNo === source.carrierNo && includeMother.value) { errorText.value = '參考載具已保留給母批。'; return }
    if (row.mother && row.action !== 0) { errorText.value = '保留母批只允許 Go。'; return }
    if ((row.action === 0 || row.action === 1) && row.returnType !== 0) { errorText.value = `${row.splitLotNo} 的 Go／Jump OP 只能選 Not Return。`; return }
    if (row.action !== 0 && !row.nodeId) { errorText.value = `${row.splitLotNo} 請用 Change Routing 指定節點。`; return }
  }
  if (totalQty.value !== source.curQty) { errorText.value = `子批 Qty 合計須等於 ${source.curQty}。`; return }
  if (source.haveComponent && (rows.value[0]?.splitLotQty !== slotCounts.value[0] || rows.value[1]?.splitLotQty !== slotCounts.value[1])) { errorText.value = '子批 Qty 須等於各載具的元件數。'; return }
  let saved: Persisted
  try { saved = readState() } catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  if (saved.records.some(item => item.lotNo === source.lotNo && item.lotStamp === source.lotStamp)) { errorText.value = '此批次已在另一個分頁模擬分批。'; return }
  const occupied = new Set(saved.records.flatMap(record => record.rows.map(row => row.carrierNo)))
  if (rows.value.some(row => occupied.has(row.carrierNo))) { errorText.value = '選取的空載具已被另一個 DEV 分批使用，請重新查詢。'; return }
  const children = rows.value.filter(row => !row.mother)
  if (saved.nextSequence !== nextSequence.value || children.some((row, i) => row.splitLotNo !== childLotNo(saved.nextSequence + i))) { nextSequence.value = saved.nextSequence; errorText.value = '子批預覽號已變動，請重新按 Split。'; return }
  records.value = saved.records
  records.value.push({ lotNo: source.lotNo, carrierNo: source.carrierNo, lotStamp: source.lotStamp, rows: rows.value.map(row => ({ ...row })), slots: source.haveComponent ? [slots.value[0].map(slot => ({ ...slot })), slots.value[1].map(slot => ({ ...slot }))] : null, savedAt: new Date().toISOString() })
  nextSequence.value += children.length
  try { window.localStorage.setItem(storageKey, JSON.stringify({ records: records.value, nextSequence: nextSequence.value })) }
  catch { records.value.pop(); nextSequence.value -= children.length; errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; return }
  submitted.value = true; message.value = 'DEV 已模擬載具分批；結果只保存在此瀏覽器，未更新 MES。'
}
onMounted(loadState)
</script>

<template>
  <section class="wp210-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；載具、元件位置與拆批結果只保存在此瀏覽器。</div>
    <h1>分批處理_載具 <small>LOT SPLIT_CARRIER · WP210</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>

    <fieldset class="reference-frame"><legend>Reference Lot</legend>
      <div class="query-row"><label for="wp210-carrier">Carrier No</label><input id="wp210-carrier" v-model="carrierInput" class="mes-input editable" :disabled="!dev" @keydown.enter.prevent="queryCarrier"><button type="button" class="mes-button" :disabled="!dev" @click="dialog = 'carrierLookup'">...</button><button type="button" class="mes-button" :disabled="!dev" @click="queryCarrier">Query</button></div>
      <div class="grid-wrap reference-grid"><table><thead><tr><th>Lot No</th><th>Carrier No</th><th>Cur Qty</th><th>Product No</th><th>Process No</th><th>OP No</th><th>Have Component</th><th>Status</th></tr></thead><tbody><tr v-if="lot"><td>{{ lot.lotNo }}</td><td>{{ lot.carrierNo }}</td><td class="number">{{ lot.curQty }}</td><td>{{ lot.productNo }}</td><td>{{ lot.processNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.haveComponent ? 'Yes' : 'No' }}</td><td>{{ lot.status }}</td></tr><tr v-else><td colspan="8" class="empty">請輸入參考載具並按 Query。</td></tr></tbody></table></div>
    </fieldset>

    <fieldset class="split-frame"><legend>Split</legend>
      <div class="controls"><span class="field-label">Split Type</span><fieldset class="radios"><label><input v-model="mode" type="radio" value="count" :disabled="!canEdit || !!lot?.haveComponent">Split by Count</label><label><input v-model="mode" type="radio" value="qty" :disabled="!canEdit || !!lot?.haveComponent">Split by Qty</label></fieldset><label for="wp210-amount">Split Count / Qty</label><input id="wp210-amount" v-model="amountInput" class="mes-input editable amount" :disabled="!canEdit || !!lot?.haveComponent" inputmode="numeric"></div>
      <div class="controls"><span class="field-label">Include Mother Lot No</span><fieldset class="radios"><label><input v-model="includeMother" type="radio" :value="true" :disabled="!canEdit">Yes</label><label><input v-model="includeMother" type="radio" :value="false" :disabled="!canEdit">No</label></fieldset><span class="spacer"></span><button type="button" class="mes-button" :disabled="!canEdit" @click="split">Split</button><button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="dialog = 'route'">Change Routing</button><button type="button" class="mes-button" :disabled="!canEdit || !activeRow" @click="dialog = 'product'">Change Product</button><button v-if="lot?.haveComponent" type="button" class="mes-button" :disabled="!canEdit || rows.length !== 2" @click="dialog = 'slots'">Slot 配置</button></div>
      <div class="grid-wrap split-grid"><table><thead><tr><th>Split Lot No</th><th>Carrier No</th><th>Split Lot Qty</th><th>Action</th><th>Process No</th><th>Process Version</th><th>OP No</th><th>Return Type</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.splitLotNo" :class="{ selected: activeLotNo === row.splitLotNo }" @click="activeLotNo = row.splitLotNo">
          <td>{{ row.splitLotNo }} <small v-if="row.mother">母批</small></td>
          <td><select v-if="!lot?.haveComponent && !(row.mother && row.carrierNo === lot?.carrierNo)" v-model="row.carrierNo" class="grid-select" :disabled="!canEdit"><option value="">選擇載具</option><option v-for="candidate in availableEmptyCarriers" :key="candidate.carrierNo" :value="candidate.carrierNo">{{ candidate.carrierNo }}</option></select><span v-else>{{ row.carrierNo || '請在 Slot 配置指定' }}</span></td>
          <td class="number"><input v-if="!lot?.haveComponent" :value="row.splitLotQty" class="mes-input qty-input editable" :disabled="!canEdit" inputmode="numeric" @change="updateQty(row, ($event.target as HTMLInputElement).value)"><span v-else>{{ row.splitLotQty }}</span></td>
          <td><select class="grid-select" :value="row.action" :disabled="!canEdit" @change="changeAction(row, ($event.target as HTMLSelectElement).value)"><option value="0">Go</option><option value="1">Jump OP</option><option value="2">Jump Process</option></select></td>
          <td class="readonly">{{ row.processNo }}</td><td class="readonly">{{ row.processVersion }}</td><td class="readonly">{{ row.opNo }}</td>
          <td><select class="grid-select" :value="row.returnType" :disabled="!canEdit" @change="changeReturnType(row, ($event.target as HTMLSelectElement).value)"><option value="0">Not Return</option><option value="1">Return to Current OP</option><option value="2">Return to Next OP</option></select></td>
        </tr>
        <tr v-if="!rows.length"><td colspan="8" class="empty">按 Split 建立子批；有元件批會開啟 Slot 配置。</td></tr>
      </tbody></table></div>
      <div class="status-row">{{ rows.length }} row(s) · 子批 Qty {{ totalQty }} / {{ lot?.curQty ?? 0 }} <span v-if="lot?.haveComponent">· Slot {{ slotsConfirmed ? '已確認' : '未確認' }}</span></div>
    </fieldset>
    <footer class="form-footer"><span>原生確認會檢查 LotStamp、載具與數量守恆。</span><button type="button" class="mes-button confirm" :disabled="!canEdit || rows.length === 0" @click="confirmSplit">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button></footer>

    <div v-if="dialog" class="modal-backdrop" @click.self="dialog = null">
      <section v-if="dialog === 'slots'" class="dialog slot-dialog" role="dialog" aria-modal="true" aria-label="Manual Split"><header><strong>Manual Split · 載具元件 Slot 配置</strong><span>{{ lot?.lotNo }}</span></header><p class="dialog-hint">左側為子批 1，右側為子批 2。選取有元件的位置及另一側空位置，再按箭頭。</p>
        <div class="slot-columns"><div class="slot-side"><label>Split Lot No 1 <input :value="rows[0]?.splitLotNo" class="mes-input" readonly></label><label>Split Lot 1 Carrier <select :value="slotCarriers[0]" class="mes-input editable" @change="chooseSlotCarrier(0, ($event.target as HTMLSelectElement).value)"><option v-for="item in sourceCarrierOptions" :key="item.carrierNo" :value="item.carrierNo" :disabled="item.carrierNo === slotCarriers[1]">{{ item.carrierNo }}</option></select><button type="button" class="mes-button" @click="carrierPickerSide = carrierPickerSide === 0 ? null : 0">...</button></label><div class="grid-wrap slot-grid"><table><thead><tr><th>Position</th><th>Component No</th><th>Original Pos.</th></tr></thead><tbody><tr v-for="slot in slots[0]" :key="slot.position" :class="{ selected: selectedSlot1 === slot.position }" @click="selectedSlot1 = slot.position"><td>{{ slot.position }}</td><td>{{ slot.componentNo }}</td><td>{{ slot.originPosition || '' }}</td></tr></tbody></table></div><div class="slot-actions"><button type="button" class="mes-button" @click="moveWithin(0,-1)">↑</button><button type="button" class="mes-button" @click="moveWithin(0,1)">↓</button><span>元件 {{ slotCounts[0] }}</span></div></div>
          <div class="transfer-buttons"><button type="button" class="mes-button" @click="moveComponent(0)">→</button><button type="button" class="mes-button" @click="moveComponent(1)">←</button></div>
          <div class="slot-side"><label>Split Lot No 2 <input :value="rows[1]?.splitLotNo" class="mes-input" readonly></label><label>Split Lot 2 Carrier <select :value="slotCarriers[1]" class="mes-input editable" @change="chooseSlotCarrier(1, ($event.target as HTMLSelectElement).value)"><option value="">選擇空載具</option><option v-for="item in availableEmptyCarriers" :key="item.carrierNo" :value="item.carrierNo" :disabled="item.carrierNo === slotCarriers[0]">{{ item.carrierNo }} · {{ item.capacity }} slots</option></select><button type="button" class="mes-button" @click="carrierPickerSide = carrierPickerSide === 1 ? null : 1">...</button></label><div class="grid-wrap slot-grid"><table><thead><tr><th>Position</th><th>Component No</th><th>Original Pos.</th></tr></thead><tbody><tr v-for="slot in slots[1]" :key="slot.position" :class="{ selected: selectedSlot2 === slot.position }" @click="selectedSlot2 = slot.position"><td>{{ slot.position }}</td><td>{{ slot.componentNo }}</td><td>{{ slot.originPosition || '' }}</td></tr></tbody></table></div><div class="slot-actions"><button type="button" class="mes-button" @click="moveWithin(1,-1)">↑</button><button type="button" class="mes-button" @click="moveWithin(1,1)">↓</button><span>元件 {{ slotCounts[1] }}</span></div></div></div>
        <div v-if="carrierPickerSide !== null" class="carrier-picker"><strong>{{ carrierPickerSide === 0 ? 'Split Lot 1 Carrier' : 'Split Lot 2 Carrier' }}</strong><button v-for="item in carrierPickerSide === 0 ? sourceCarrierOptions : availableEmptyCarriers" :key="item.carrierNo" type="button" class="mes-button" :disabled="item.carrierNo === slotCarriers[carrierPickerSide === 0 ? 1 : 0]" @click="chooseSlotCarrierFromPicker(item.carrierNo)">{{ item.carrierNo }} · {{ item.capacity }} slots</button></div>
        <footer><button type="button" class="mes-button confirm" @click="confirmSlots">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="dialog = null">Close(&amp;X)</button></footer></section>
      <section v-else-if="dialog === 'route'" class="dialog option-dialog" role="dialog" aria-modal="true" aria-label="Change Routing"><header><strong>Change Routing</strong><span>{{ activeLotNo }}</span></header><button v-for="option in routes" :key="option.nodeId" type="button" class="option" @click="chooseRoute(option)">{{ option.label }}<small>{{ option.processNo }} / {{ option.processVersion }} · {{ option.nodeId }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'carrierLookup'" class="dialog option-dialog" role="dialog" aria-modal="true" aria-label="Carrier Lookup"><header><strong>Carrier Lookup</strong></header><button v-for="item in fixtures" :key="item.carrierNo" type="button" class="option" @click="chooseReferenceCarrier(item.carrierNo)">{{ item.carrierNo }}<small>{{ item.lotNo }} · Status {{ item.status }} · {{ item.haveComponent ? 'Have Component' : 'No Component' }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else class="dialog option-dialog" role="dialog" aria-modal="true" aria-label="Change Product"><header><strong>Change Product</strong><span>{{ activeLotNo }}</span></header><p class="dialog-hint">原 WinForms Confirm XML 未送出 ProductNo／Version；此選擇僅為 DEV 畫面預覽。</p><button v-for="option in products" :key="option.productNo" type="button" class="option" @click="chooseProduct(option)">{{ option.productNo }} / {{ option.productVersion }}</button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
    </div>
  </section>
</template>

<style scoped>
.wp210-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}
.reference-frame,.split-frame{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}.reference-frame{height:150px;flex:0 0 150px}.split-frame{flex:1;min-height:285px;display:flex;flex-direction:column}legend{padding:0 5px}.query-row{min-height:42px;display:flex;align-items:center;gap:8px}.query-row label{width:105px;text-align:right}.mes-input{height:26px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.query-row .mes-input{width:175px}.editable{background:#fff0b3}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}
.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.reference-grid{height:88px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.selected td{background:#ffedb3}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:40px}
.controls{min-height:40px;display:flex;align-items:center;gap:8px;flex-wrap:wrap}.field-label{width:130px;text-align:right}.radios{display:flex;gap:12px;margin:0;padding:4px 8px;border:1px solid #ccc}.radios label{display:flex;align-items:center;gap:3px;white-space:nowrap}.amount{width:65px;text-align:right}.spacer{flex:1}.split-grid{flex:1;min-height:130px}.split-grid tr{cursor:pointer}.split-grid tr:hover td{background:#fff0bd}.split-grid td{height:27px}.split-grid small{padding:1px 4px;border:1px solid #d2ad55;background:#fff4c7;color:#765e20}.readonly{background:#ececec!important}.grid-select{min-width:105px;max-width:160px;width:100%;height:26px;border:1px solid #999;background:#fff;color:#333;font:inherit}.qty-input{width:72px;text-align:right}.status-row{min-height:22px;padding:4px 6px;border:1px solid #c5c5c5;background:#eee;text-align:right;color:#555;box-sizing:border-box}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}.form-footer span{margin-right:auto;color:#777}
.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.dialog{width:min(960px,96vw);max-height:88vh;display:flex;flex-direction:column;gap:9px;padding:13px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;gap:12px}.dialog header span{color:#666}.dialog footer{display:flex;justify-content:flex-end;gap:8px}.dialog-hint{margin:0;color:#666}.slot-columns{min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 42px minmax(0,1fr);gap:10px}.slot-side{min-width:0;display:flex;flex-direction:column;gap:6px}.slot-side label{display:flex;align-items:center;justify-content:space-between;gap:8px}.slot-side label .mes-input{width:52%}.slot-grid{height:min(330px,43vh)}.slot-grid tr{cursor:pointer}.slot-grid tr:hover td{background:#fff0bd}.slot-actions{display:flex;align-items:center;gap:5px}.slot-actions span{margin-left:auto;color:#666}.carrier-picker{display:flex;align-items:center;gap:5px;flex-wrap:wrap;padding:6px;border:1px solid #d3bf89;background:#fff7df}.carrier-picker strong{margin-right:4px}.transfer-buttons{display:flex;flex-direction:column;justify-content:center;gap:8px}.option-dialog{width:min(600px,94vw)}.option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.option:hover{background:#fff3cc}.option small{color:#666}@media(max-width:740px){.slot-columns{grid-template-columns:1fr}.transfer-buttons{flex-direction:row}.slot-grid{height:160px}.field-label{width:100px}.form-footer span{display:none}}
</style>

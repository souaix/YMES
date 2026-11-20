<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Component = { componentNo: string; position: number }
type Lot = {
  lotNo: string; carrierNo: string; moNo: string; curQty: number; lotStamp: number
  productNo: string; processNo: string; processVersion: string; opNo: string; opName: string
  nodeId: string; brNo: string; unitNo: string; status: number; osNo: string
  haveComponent: boolean; haveLevel: boolean; allowDiffMO: boolean; components: Component[]
}
type Carrier = { carrierNo: string; capacity: number; available: boolean }
type Slot = { position: number; lotNo: string; componentNo: string; orgCarrierNo: string; orgPosition: number }
type Route = { processNo: string; processVersion: string; opNo: string; nodeId: string }
type MergeRecord = {
  refLotNo: string; refCarrierNo: string; newLotNo: string; newCarrierNo: string
  sourceLots: string[]; lotStamps: number[]; slots: Slot[]; action: 0 | 1
  route: Route | null; qty: number; savedAt: string
}

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '載具併批服務尚未提供，暫無法查詢或操作。'
const storageKey = 'ymes-dev-mock:wip-lot-merge-carrier:records:v1'
const carriers: Carrier[] = [
  { carrierNo: 'DEV-CAR-A', capacity: 6, available: false },
  { carrierNo: 'DEV-CAR-B', capacity: 6, available: false },
  { carrierNo: 'DEV-CAR-C', capacity: 6, available: false },
  { carrierNo: 'DEV-CAR-CR', capacity: 4, available: false },
  { carrierNo: 'DEV-CAR-CC', capacity: 4, available: false },
  { carrierNo: 'DEV-CAR-MIX', capacity: 4, available: false },
  { carrierNo: 'DEV-CAR-OS1', capacity: 6, available: false },
  { carrierNo: 'DEV-CAR-OS2', capacity: 6, available: false },
  { carrierNo: 'DEV-CAR-EMPTY4', capacity: 4, available: true },
  { carrierNo: 'DEV-CAR-EMPTY8', capacity: 8, available: true },
  { carrierNo: 'DEV-CAR-SMALL', capacity: 2, available: true },
]
// DEV fixtures model the two WinForms paths. All values are isolated from live MES.
const fixtures: Lot[] = [
  { lotNo: 'DEV-MCAR-001', carrierNo: 'DEV-CAR-A', moNo: 'DEV-MO-A', curQty: 5, lotStamp: 21401, productNo: 'DEV-PROD-A', processNo: 'DEV-PS-A', processVersion: '01', opNo: 'DEV-OP-A', opName: '組裝', nodeId: 'DEV-NODE-A', brNo: 'DEV-BR-A', unitNo: 'PCS', status: 1, osNo: '', haveComponent: false, haveLevel: false, allowDiffMO: true, components: [] },
  { lotNo: 'DEV-MCAR-002', carrierNo: 'DEV-CAR-B', moNo: 'DEV-MO-A', curQty: 3, lotStamp: 21402, productNo: 'DEV-PROD-A', processNo: 'DEV-PS-A', processVersion: '01', opNo: 'DEV-OP-A', opName: '組裝', nodeId: 'DEV-NODE-A', brNo: 'DEV-BR-A', unitNo: 'PCS', status: 1, osNo: '', haveComponent: false, haveLevel: false, allowDiffMO: true, components: [] },
  { lotNo: 'DEV-MCAR-003', carrierNo: 'DEV-CAR-C', moNo: 'DEV-MO-B', curQty: 2, lotStamp: 21403, productNo: 'DEV-PROD-A', processNo: 'DEV-PS-A', processVersion: '01', opNo: 'DEV-OP-A', opName: '組裝', nodeId: 'DEV-NODE-B', brNo: 'DEV-BR-A', unitNo: 'PCS', status: 1, osNo: '', haveComponent: false, haveLevel: false, allowDiffMO: true, components: [] },
  { lotNo: 'DEV-MCAR-C001', carrierNo: 'DEV-CAR-CR', moNo: 'DEV-MO-C', curQty: 2, lotStamp: 21411, productNo: 'DEV-PROD-C', processNo: 'DEV-PS-C', processVersion: '01', opNo: 'DEV-OP-C', opName: '封裝', nodeId: 'DEV-NODE-C', brNo: 'DEV-BR-C', unitNo: 'PCS', status: 1, osNo: '', haveComponent: true, haveLevel: false, allowDiffMO: false, components: [{ componentNo: 'DEV-COMP-R1', position: 1 }, { componentNo: 'DEV-COMP-R2', position: 2 }] },
  { lotNo: 'DEV-MCAR-C002', carrierNo: 'DEV-CAR-CC', moNo: 'DEV-MO-C', curQty: 2, lotStamp: 21412, productNo: 'DEV-PROD-C', processNo: 'DEV-PS-C', processVersion: '01', opNo: 'DEV-OP-C', opName: '封裝', nodeId: 'DEV-NODE-C', brNo: 'DEV-BR-C', unitNo: 'PCS', status: 1, osNo: '', haveComponent: true, haveLevel: false, allowDiffMO: false, components: [{ componentNo: 'DEV-COMP-I1', position: 1 }, { componentNo: 'DEV-COMP-I2', position: 2 }] },
  { lotNo: 'DEV-MCAR-MIX1', carrierNo: 'DEV-CAR-MIX', moNo: 'DEV-MO-C', curQty: 1, lotStamp: 21413, productNo: 'DEV-PROD-C', processNo: 'DEV-PS-C', processVersion: '01', opNo: 'DEV-OP-C', opName: '封裝', nodeId: 'DEV-NODE-C', brNo: 'DEV-BR-C', unitNo: 'PCS', status: 1, osNo: '', haveComponent: true, haveLevel: false, allowDiffMO: false, components: [{ componentNo: 'DEV-COMP-M1', position: 1 }] },
  { lotNo: 'DEV-MCAR-MIX2', carrierNo: 'DEV-CAR-MIX', moNo: 'DEV-MO-C', curQty: 1, lotStamp: 21414, productNo: 'DEV-PROD-C', processNo: 'DEV-PS-C', processVersion: '01', opNo: 'DEV-OP-C', opName: '封裝', nodeId: 'DEV-NODE-C', brNo: 'DEV-BR-C', unitNo: 'PCS', status: 1, osNo: '', haveComponent: true, haveLevel: false, allowDiffMO: false, components: [{ componentNo: 'DEV-COMP-M2', position: 2 }] },
  { lotNo: 'DEV-MCAR-OS1', carrierNo: 'DEV-CAR-OS1', moNo: 'DEV-MO-OS', curQty: 4, lotStamp: 21421, productNo: 'DEV-PROD-OS', processNo: 'DEV-PS-OS', processVersion: '01', opNo: 'DEV-OP-OS', opName: '外包', nodeId: 'DEV-NODE-OS', brNo: 'DEV-BR-OS', unitNo: 'PCS', status: 5, osNo: 'DEV-OS-01', haveComponent: false, haveLevel: false, allowDiffMO: false, components: [] },
  { lotNo: 'DEV-MCAR-OS2', carrierNo: 'DEV-CAR-OS2', moNo: 'DEV-MO-OS', curQty: 2, lotStamp: 21422, productNo: 'DEV-PROD-OS', processNo: 'DEV-PS-OS', processVersion: '01', opNo: 'DEV-OP-OS', opName: '外包', nodeId: 'DEV-NODE-OS', brNo: 'DEV-BR-OS', unitNo: 'PCS', status: 5, osNo: 'DEV-OS-01', haveComponent: false, haveLevel: false, allowDiffMO: false, components: [] },
]
const routes: Route[] = [
  { processNo: 'DEV-PS-A', processVersion: '01', opNo: 'DEV-OP-A20', nodeId: 'DEV-NODE-A20' },
  { processNo: 'DEV-PS-C', processVersion: '01', opNo: 'DEV-OP-C20', nodeId: 'DEV-NODE-C20' },
]

const refInput = ref('')
const reference = ref<Lot | null>(null)
const candidates = ref<Lot[]>([])
const selectedLotNos = ref<string[]>([])
const newLotNo = ref('')
const newCarrierNo = ref('')
const action = ref<0 | 1>(0)
const targetRoute = ref<Route | null>(null)
const moFilter = ref<string[] | null>(null)
const moDraft = ref<string[]>([])
const slotAssignments = ref<Slot[]>([])
const slotsConfirmed = ref(false)
const incomingLotNo = ref('')
const incomingComponentNo = ref('')
const targetPosition = ref<number | null>(null)
const activeSlotPosition = ref<number | null>(null)
const records = ref<MergeRecord[]>([])
const submitted = ref(false)
const dialog = ref<'reference' | 'carrier' | 'route' | 'mo' | 'slots' | 'nodeWarning' | null>(null)
const errorText = ref('')
const message = ref('請輸入參考載具編號並按 Query。')

const canEdit = computed(() => dev.value && reference.value !== null && !submitted.value)
const componentMode = computed(() => reference.value?.haveComponent === true)
const allMOs = computed(() => [...new Set(candidates.value.map(item => item.moNo))])
const visibleLots = computed(() => candidates.value.filter(item => moFilter.value === null || moFilter.value.includes(item.moNo)))
const selectedLots = computed(() => candidates.value.filter(item => selectedLotNos.value.includes(item.lotNo)))
const totalQty = computed(() => selectedLots.value.reduce((total, item) => total + item.curQty, 0))
const capacity = computed(() => carriers.find(item => item.carrierNo === reference.value?.carrierNo)?.capacity || 0)
const slotRows = computed(() => Array.from({ length: capacity.value }, (_, index) => slotAssignments.value.find(slot => slot.position === index + 1) || { position: index + 1, lotNo: '', componentNo: '', orgCarrierNo: '', orgPosition: -1 }))
const incomingLot = computed(() => candidates.value.find(item => item.lotNo === incomingLotNo.value) || null)
const incomingRows = computed(() => incomingLot.value?.components.filter(component => !slotAssignments.value.some(slot => slot.componentNo === component.componentNo)) || [])
const availableTargets = computed(() => carriers.filter(item => item.available || item.carrierNo === reference.value?.carrierNo))

function loadRecords(): void {
  if (!dev.value) return
  try { const data = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; records.value = Array.isArray(data) ? data as MergeRecord[] : [] }
  catch { records.value = [] }
}
function resetQuery(): void {
  reference.value = null; candidates.value = []; selectedLotNos.value = []; slotAssignments.value = []
  slotsConfirmed.value = false; incomingLotNo.value = ''; incomingComponentNo.value = ''
  targetPosition.value = null; activeSlotPosition.value = null; newLotNo.value = ''; newCarrierNo.value = ''
  moFilter.value = null; action.value = 0; targetRoute.value = null; submitted.value = false; dialog.value = null
}
function invalidateReference(): void {
  if (reference.value && refInput.value.trim().toUpperCase() !== reference.value.carrierNo) {
    resetQuery(); message.value = '參考載具已變更，請重新查詢。'
  }
}
function eligible(refLot: Lot, item: Lot): boolean {
  return !!item.carrierNo && item.haveComponent === refLot.haveComponent && !item.haveLevel &&
    item.productNo === refLot.productNo && item.opNo === refLot.opNo && item.brNo === refLot.brNo &&
    item.status === refLot.status && item.unitNo === refLot.unitNo &&
    (refLot.allowDiffMO || item.moNo === refLot.moNo) &&
    (refLot.status !== 5 || (!!item.osNo && item.osNo === refLot.osNo))
}
function queryReference(): void {
  errorText.value = ''; const carrierNo = refInput.value.trim().toUpperCase(); resetQuery()
  if (!dev.value) { errorText.value = unavailableMessage; return }
  if (!carrierNo) { errorText.value = 'RefCarrier No 不可空白。'; return }
  const carrierLots = fixtures.filter(item => item.carrierNo === carrierNo)
  if (!carrierLots.length) { errorText.value = '找不到 DEV 參考載具；可試 DEV-CAR-A 或 DEV-CAR-CR。'; return }
  if (carrierLots.length > 1 && carrierLots.some(item => item.haveComponent)) { errorText.value = '此載具有元件且包含多筆 Lot，原表單禁止執行載具併批。'; return }
  const source = carrierLots[0]!
  if (source.haveLevel) { errorText.value = '此參考批含等級資料，請使用等級併批功能。'; return }
  if (records.value.some(record => record.sourceLots.includes(source.lotNo))) { errorText.value = '此 DEV 批次已模擬併批。'; return }
  reference.value = source; refInput.value = carrierNo; newCarrierNo.value = carrierNo; newLotNo.value = source.lotNo
  candidates.value = fixtures.filter(item => eligible(source, item) && !records.value.some(record => record.sourceLots.includes(item.lotNo)))
  selectedLotNos.value = [source.lotNo]
  if (source.haveComponent) {
    if (source.components.length !== source.curQty) { errorText.value = '參考批元件數與 CurQty 不符。'; resetQuery(); return }
    slotAssignments.value = source.components.map(item => ({ position: item.position, lotNo: source.lotNo, componentNo: item.componentNo, orgCarrierNo: source.carrierNo, orgPosition: item.position }))
    if (new Set(slotAssignments.value.map(item => item.position)).size !== slotAssignments.value.length || slotAssignments.value.some(item => item.position < 1 || item.position > capacity.value)) { errorText.value = '參考載具 Slot 現況不一致。'; resetQuery(); return }
    dialog.value = 'slots'
    message.value = '此參考批有元件；請在 Slot 視窗將子批元件移至參考載具，再按 Confirm。'
  } else message.value = `已載入 ${candidates.value.length} 筆有載具候選批；黃色列為固定參考批。`
}
function chooseReference(carrierNo: string): void { refInput.value = carrierNo; queryReference() }
function setSelected(lotNo: string, checked: boolean): void {
  if (!canEdit.value || componentMode.value || lotNo === reference.value?.lotNo) return
  selectedLotNos.value = checked ? [...new Set([...selectedLotNos.value, lotNo])] : selectedLotNos.value.filter(item => item !== lotNo)
}
function selectAll(): void {
  if (!canEdit.value) return
  if (componentMode.value) { errorText.value = '有元件批請在 Slot 視窗挑選，不能在主畫面全選。'; return }
  selectedLotNos.value = [...new Set([...visibleLots.value.map(item => item.lotNo), reference.value!.lotNo])]
}
function unselectAll(): void {
  if (!canEdit.value) return
  if (componentMode.value) { errorText.value = '有元件批請在 Slot 視窗挑選，不能在主畫面取消全選。'; return }
  selectedLotNos.value = [reference.value!.lotNo]
}
function openMO(): void { if (canEdit.value) { moDraft.value = moFilter.value ? [...moFilter.value] : [...allMOs.value]; dialog.value = 'mo' } }
function applyMO(): void {
  if (!reference.value) return
  const allowed = new Set([...moDraft.value, reference.value.moNo]); moFilter.value = [...allowed]
  if (componentMode.value && slotAssignments.value.some(slot => !allowed.has(candidates.value.find(item => item.lotNo === slot.lotNo)?.moNo || ''))) {
    slotAssignments.value = slotAssignments.value.filter(slot => slot.lotNo === reference.value!.lotNo)
    slotsConfirmed.value = false
  }
  selectedLotNos.value = selectedLotNos.value.filter(lotNo => allowed.has(candidates.value.find(item => item.lotNo === lotNo)?.moNo || ''))
  if (!selectedLotNos.value.includes(reference.value.lotNo)) selectedLotNos.value.push(reference.value.lotNo)
  dialog.value = null
}
function chooseIncoming(lotNo: string): void {
  if (!canEdit.value || !componentMode.value) return
  incomingLotNo.value = lotNo; incomingComponentNo.value = ''; activeSlotPosition.value = null; errorText.value = ''
}
function openSlots(): void { if (canEdit.value && componentMode.value) { slotsConfirmed.value = false; dialog.value = 'slots' } }
function moveOne(): void {
  errorText.value = ''
  const lot = incomingLot.value; const item = incomingRows.value.find(row => row.componentNo === incomingComponentNo.value)
  const position = targetPosition.value
  if (!lot || !item || position === null) { errorText.value = '請選取子批元件與參考載具的空位置。'; return }
  if (slotAssignments.value.some(slot => slot.position === position)) { errorText.value = '目標 Slot 已被占用。'; return }
  slotAssignments.value = [...slotAssignments.value, { position, lotNo: lot.lotNo, componentNo: item.componentNo, orgCarrierNo: lot.carrierNo, orgPosition: item.position }]
  incomingComponentNo.value = ''; targetPosition.value = slotRows.value.find(row => !row.componentNo)?.position ?? null
}
function moveAll(): void {
  errorText.value = ''
  const lot = incomingLot.value; if (!lot) { errorText.value = '請先選取子批 Lot。'; return }
  const pending = incomingRows.value
  const free = slotRows.value.filter(row => !row.componentNo).map(row => row.position)
  if (pending.length > free.length) { errorText.value = `參考載具剩餘 ${free.length} 個 Slot，無法放入 ${pending.length} 顆元件。`; return }
  slotAssignments.value = [...slotAssignments.value, ...pending.map((item, index) => ({ position: free[index]!, lotNo: lot.lotNo, componentNo: item.componentNo, orgCarrierNo: lot.carrierNo, orgPosition: item.position }))]
  targetPosition.value = slotRows.value.find(row => !row.componentNo)?.position ?? null
}
function unmoveOne(): void {
  const slot = slotAssignments.value.find(row => row.position === activeSlotPosition.value)
  if (!slot || slot.lotNo === reference.value?.lotNo || slot.lotNo !== incomingLotNo.value) { errorText.value = '請選取目前子批已移入的元件位置。'; return }
  slotAssignments.value = slotAssignments.value.filter(row => row.position !== slot.position); activeSlotPosition.value = null; errorText.value = ''
}
function unmoveAll(): void {
  if (!incomingLotNo.value) { errorText.value = '請先選取子批 Lot。'; return }
  slotAssignments.value = slotAssignments.value.filter(row => row.lotNo !== incomingLotNo.value)
  activeSlotPosition.value = null; errorText.value = ''
}
function confirmSlots(): void {
  errorText.value = ''
  const source = reference.value; if (!source) return
  const lots = [...new Set(slotAssignments.value.map(slot => slot.lotNo))]
  if (lots.length < 2) { errorText.value = '請至少移入一筆子批的全部元件。'; return }
  if (lots.some(lotNo => slotAssignments.value.filter(slot => slot.lotNo === lotNo).length !== candidates.value.find(item => item.lotNo === lotNo)?.curQty)) { errorText.value = '每筆參與併批的元件數必須等於其 CurQty；請把該子批元件全部移入。'; return }
  selectedLotNos.value = lots; slotsConfirmed.value = true; dialog.value = null
  message.value = `已安排 ${slotAssignments.value.length} 顆元件與 ${lots.length} 筆 Lot；主畫面可再確認目標載具與輸出批號。`
}
function changeAction(next: 0 | 1): void { action.value = next; if (next === 0) targetRoute.value = null }
function chooseRoute(route: Route): void { targetRoute.value = route; dialog.value = null }
function chooseTarget(carrierNo: string): void { newCarrierNo.value = carrierNo; dialog.value = null }
function confirmMerge(ackNodeDifference = false): void {
  errorText.value = ''
  const source = reference.value
  if (!canEdit.value || !source) { errorText.value = '請先查詢參考載具。'; return }
  const outputLot = newLotNo.value.trim().toUpperCase()
  if (!outputLot) { errorText.value = 'Merge LotNo 不可空白。'; return }
  if (!outputLot.startsWith('DEV-')) { errorText.value = 'DEV 預覽的輸出批號需以 DEV- 開頭。'; return }
  if (outputLot !== source.lotNo && fixtures.some(item => item.lotNo === outputLot)) { errorText.value = 'Merge LotNo 已存在。'; return }
  const target = availableTargets.value.find(item => item.carrierNo === newCarrierNo.value)
  if (!target) { errorText.value = '請選擇參考載具或可用空載具作為 Merge Carrier。'; return }
  if (action.value === 1 && (!targetRoute.value || source.status === 5)) { errorText.value = source.status === 5 ? '外包未回貨併批只能使用 Go。' : 'Assign 需要 Process No、Version、OP No 與 NodeID。'; return }
  const selected = selectedLots.value
  if (selected.length < 2 || !selected.some(item => item.lotNo === source.lotNo)) { errorText.value = '必須包含參考批及至少一筆子批。'; return }
  if (selected.some(item => item.curQty <= 0)) { errorText.value = '來源批 CurQty 必須大於 0。'; return }
  if (selected.some(item => !eligible(source, item))) { errorText.value = '來源批條件已不符合參考批。'; return }
  if (componentMode.value) {
    if (!slotsConfirmed.value) { errorText.value = '請先在 Component Slot 視窗確認元件配置。'; return }
    if (slotAssignments.value.length !== selected.reduce((sum, item) => sum + item.curQty, 0)) { errorText.value = 'Slot 元件總數與來源批 CurQty 不符。'; return }
    if (slotAssignments.value.some(slot => slot.position < 1 || slot.position > target.capacity) || new Set(slotAssignments.value.map(slot => slot.position)).size !== slotAssignments.value.length) { errorText.value = '目標載具容量不足或 Slot 位置重複。'; return }
    const expected = selected.flatMap(item => item.components.map(component => component.componentNo)).sort()
    const actual = slotAssignments.value.map(slot => slot.componentNo).sort()
    if (expected.join('|') !== actual.join('|')) { errorText.value = 'Slot 配置沒有完整且唯一地包含每顆來源元件。'; return }
  }
  if (source.status === 5 && selected.some(item => item.osNo !== source.osNo)) { errorText.value = '外包批必須屬於同一張未回貨外包單。'; return }
  if (!ackNodeDifference && selected.some(item => item.opNo === source.opNo && item.nodeId !== source.nodeId)) { dialog.value = 'nodeWarning'; return }
  let current: MergeRecord[]
  try { const data = JSON.parse(window.localStorage.getItem(storageKey) || '[]') as unknown; current = Array.isArray(data) ? data as MergeRecord[] : [] }
  catch { errorText.value = '無法讀取 DEV 模擬紀錄。'; return }
  if (current.some(record => record.newLotNo === outputLot || selected.some(item => record.sourceLots.includes(item.lotNo)))) { errorText.value = '輸出批號或來源批已被 DEV 模擬使用。'; dialog.value = null; return }
  const record: MergeRecord = { refLotNo: source.lotNo, refCarrierNo: source.carrierNo, newLotNo: outputLot, newCarrierNo: target.carrierNo, sourceLots: selected.map(item => item.lotNo), lotStamps: selected.map(item => item.lotStamp), slots: componentMode.value ? slotAssignments.value.map(item => ({ ...item })) : [], action: action.value, route: action.value === 1 ? targetRoute.value : null, qty: totalQty.value, savedAt: new Date().toISOString() }
  try { window.localStorage.setItem(storageKey, JSON.stringify([...current, record])) }
  catch { errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; dialog.value = null; return }
  records.value = [...current, record]; submitted.value = true; dialog.value = null
  message.value = `DEV 已模擬 ${selected.length} 筆 Lot、總量 ${totalQty.value} 併至 ${outputLot} / ${target.carrierNo}；未更新 MES。`
}
onMounted(loadRecords)
</script>

<template>
  <section class="wp214-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽：一般載具 DEV-CAR-A、有元件 DEV-CAR-CR、多批拒絕 DEV-CAR-MIX、外包 DEV-CAR-OS1。</div>
    <h1>併批處理_載具 <small>LOT MERGE_CARRIER · WP214</small></h1>
    <p v-if="!dev" class="notice" role="status">{{ unavailableMessage }}</p><p v-else-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="reference-frame"><legend>Reference Carrier</legend>
      <div class="control-row"><label for="wp214-ref">RefCarrier No</label><input id="wp214-ref" v-model="refInput" class="mes-input editable" :disabled="!dev" @input="invalidateReference" @keydown.enter.prevent="queryReference"><button type="button" class="mes-button" :disabled="!dev" @click="dialog = 'reference'">...</button><button type="button" class="mes-button" :disabled="!dev" @click="queryReference">Query</button></div>
      <div class="grid-wrap ref-grid"><table><thead><tr><th>LotNo</th><th>CurQty</th><th>ProcessNo</th><th>OPNo</th><th>OPName</th><th>NodeId</th><th>BaseLotNo</th><th>OrgLotNo</th></tr></thead><tbody><tr v-if="reference"><td>{{ reference.lotNo }}</td><td class="number">{{ reference.curQty }}</td><td>{{ reference.processNo }}</td><td>{{ reference.opNo }}</td><td>{{ reference.opName }}</td><td>{{ reference.nodeId }}</td><td>{{ reference.lotNo }}</td><td>{{ reference.lotNo }}</td></tr><tr v-else><td colspan="8" class="empty">請查詢參考載具。</td></tr></tbody></table></div>
    </fieldset>
    <fieldset class="merge-frame"><legend>Merge Carrier</legend>
      <div class="control-row"><label for="wp214-carrier">Carrier No</label><input id="wp214-carrier" :value="newCarrierNo" class="mes-input" readonly><button type="button" class="mes-button" :disabled="!canEdit" @click="dialog = 'carrier'">...</button><button type="button" class="mes-button" :disabled="!canEdit" @click="selectAll">Select All</button><button type="button" class="mes-button" :disabled="!canEdit" @click="unselectAll">UnSelect All</button><label for="wp214-mo">MO No</label><input id="wp214-mo" :value="moFilter?.join(', ') ?? ''" class="mes-input" readonly><button type="button" class="mes-button" :disabled="!canEdit" @click="openMO">...</button><button v-if="componentMode" type="button" class="mes-button" :disabled="!canEdit" @click="openSlots">Component Slot...</button></div>
      <div class="control-row"><label for="wp214-output">LotNo</label><input id="wp214-output" v-model="newLotNo" class="mes-input editable" :disabled="!canEdit"></div>
      <fieldset class="transfer-frame"><legend>Transfer Type</legend><label><input type="radio" :checked="action === 0" :disabled="!canEdit" @change="changeAction(0)">Go</label><label><input type="radio" :checked="action === 1" :disabled="!canEdit" @change="changeAction(1)">Assign</label><button type="button" class="mes-button" :disabled="!canEdit || action !== 1" @click="dialog = 'route'">...</button><label>Process No <input :value="targetRoute?.processNo || ''" class="mes-input" readonly></label><label>Version <input :value="targetRoute?.processVersion || ''" class="mes-input small" readonly></label><label>OP No <input :value="targetRoute?.opNo || ''" class="mes-input" readonly></label></fieldset>
      <div class="grid-wrap merge-grid"><table><thead><tr><th>Check</th><th>CarrierNo</th><th>LotNo</th><th>CurQty</th><th>MONo</th><th>NodeId</th></tr></thead><tbody><tr v-for="item in visibleLots" :key="item.lotNo" :class="{ reference: item.lotNo === reference?.lotNo }"><td><input type="checkbox" :checked="selectedLotNos.includes(item.lotNo)" :disabled="!canEdit || componentMode || item.lotNo === reference?.lotNo" :aria-label="'選取 ' + item.lotNo" @change="setSelected(item.lotNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ item.carrierNo }}</td><td>{{ item.lotNo }}</td><td class="number">{{ item.curQty }}</td><td>{{ item.moNo }}</td><td>{{ item.nodeId }}</td></tr><tr v-if="!visibleLots.length"><td colspan="6" class="empty">查詢參考載具後顯示候選批。</td></tr></tbody><tfoot><tr><th>已選</th><th>{{ selectedLots.length }} 筆</th><th>CurQty Sum</th><th class="number">{{ totalQty }}</th><th colspan="2">{{ componentMode ? '元件由 Slot 視窗決定' : '' }}</th></tr></tfoot></table></div>
    </fieldset>
    <footer class="form-footer"><button type="button" class="mes-button confirm" :disabled="!canEdit" @click="confirmMerge()">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="emit('close')">Close(&amp;X)</button></footer>
    <div v-if="dev && dialog" class="modal-backdrop" @click.self="dialog = null">
      <section v-if="dialog === 'reference'" class="dialog picker" role="dialog" aria-modal="true" aria-label="Reference Carrier"><header><strong>Reference Carrier</strong></header><button v-for="carrierNo in [...new Set(fixtures.map(item => item.carrierNo))]" :key="carrierNo" type="button" class="option" @click="chooseReference(carrierNo)">{{ carrierNo }}<small>{{ fixtures.filter(item => item.carrierNo === carrierNo).map(item => item.lotNo).join(', ') }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'carrier'" class="dialog picker" role="dialog" aria-modal="true" aria-label="Merge Carrier"><header><strong>Merge Carrier</strong></header><button v-for="item in availableTargets" :key="item.carrierNo" type="button" class="option" @click="chooseTarget(item.carrierNo)">{{ item.carrierNo }}<small>容量 {{ item.capacity }} · {{ item.available ? '空載具' : '參考載具' }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'route'" class="dialog picker" role="dialog" aria-modal="true" aria-label="Assign Routing"><header><strong>Assign Routing</strong></header><button v-for="route in routes" :key="route.nodeId" type="button" class="option" @click="chooseRoute(route)">{{ route.processNo }} / {{ route.processVersion }} / {{ route.opNo }}<small>{{ route.nodeId }}</small></button><footer><button type="button" class="mes-button" @click="dialog = null">Close</button></footer></section>
      <section v-else-if="dialog === 'mo'" class="dialog picker" role="dialog" aria-modal="true" aria-label="MO No"><header><strong>MO No</strong><small>參考批工單固定保留</small></header><label v-for="mo in allMOs" :key="mo" class="mo-option"><input v-model="moDraft" type="checkbox" :value="mo" :disabled="mo === reference?.moNo">{{ mo }}</label><footer><button type="button" class="mes-button confirm" @click="applyMO">確定</button><button type="button" class="mes-button" @click="moFilter = null; dialog = null">清除篩選</button><button type="button" class="mes-button" @click="dialog = null">取消</button></footer></section>
      <section v-else-if="dialog === 'slots'" class="dialog slot-dialog" role="dialog" aria-modal="true" aria-label="Component Slot"><header><strong>Manual Merge · Component Slot</strong><small>Current Carrier No {{ reference?.carrierNo }}</small></header>
        <fieldset class="slot-info"><legend>Reference Lot Info</legend><span>Reference LotNo {{ reference?.lotNo }}</span><span>Current Carrier No {{ reference?.carrierNo }}</span><span>Capacity {{ capacity }}</span></fieldset>
        <div class="grid-wrap popup-ref-grid"><table><thead><tr><th>LotNo</th><th>CurQty</th><th>ProcessNo</th><th>OPNo</th><th>OPName</th><th>BaseLotNo</th><th>OrgLotNo</th></tr></thead><tbody><tr v-if="reference"><td>{{ reference.lotNo }}</td><td class="number">{{ reference.curQty }}</td><td>{{ reference.processNo }}</td><td>{{ reference.opNo }}</td><td>{{ reference.opName }}</td><td>{{ reference.lotNo }}</td><td>{{ reference.lotNo }}</td></tr></tbody></table></div>
        <div class="slot-columns"><fieldset><legend>Current Carrier No · Slot</legend><div class="grid-wrap slot-grid"><table><thead><tr><th>CarrierPosition</th><th>LotNo</th><th>ComponentNo</th></tr></thead><tbody><tr v-for="row in slotRows" :key="row.position" :class="{ active: targetPosition === row.position || activeSlotPosition === row.position, original: row.lotNo === reference?.lotNo, moved: row.lotNo && row.lotNo !== reference?.lotNo }" @click="row.componentNo ? activeSlotPosition = row.position : targetPosition = row.position"><td class="number">{{ row.position }}</td><td>{{ row.lotNo }}</td><td>{{ row.componentNo }}</td></tr></tbody></table></div></fieldset>
          <div class="move-buttons"><button type="button" class="mes-button" @click="moveOne">&gt;</button><button type="button" class="mes-button" @click="moveAll">&gt;&gt;</button><button type="button" class="mes-button" @click="unmoveOne">&lt;</button><button type="button" class="mes-button" @click="unmoveAll">&lt;&lt;</button></div>
          <fieldset><legend>Merge Lot · Incoming Carrier</legend><div class="control-row"><label for="wp214-incoming">Lot No</label><input id="wp214-incoming" :value="incomingLotNo" class="mes-input" readonly><select :value="incomingLotNo" class="mes-input" aria-label="選擇子批" @change="chooseIncoming(($event.target as HTMLSelectElement).value)"><option value="">選擇子批</option><option v-for="item in visibleLots.filter(row => row.lotNo !== reference?.lotNo)" :key="item.lotNo" :value="item.lotNo">{{ item.lotNo }}</option></select></div><p class="slot-caption">Carrier No {{ incomingLot?.carrierNo || '—' }}</p><div class="grid-wrap slot-grid"><table><thead><tr><th>CarrierPosition</th><th>LotNo</th><th>ComponentNo</th></tr></thead><tbody><tr v-for="item in incomingRows" :key="item.componentNo" :class="{ active: incomingComponentNo === item.componentNo }" @click="incomingComponentNo = item.componentNo"><td class="number">{{ item.position }}</td><td>{{ incomingLotNo }}</td><td>{{ item.componentNo }}</td></tr><tr v-if="!incomingRows.length"><td colspan="3" class="empty">選取子批，或元件已全部移入。</td></tr></tbody></table></div></fieldset></div>
        <p class="slot-caption">已配置 {{ slotAssignments.length }} / {{ capacity }} Slot；每筆來源批的全部元件都須移入。</p><footer><button type="button" class="mes-button confirm" @click="confirmSlots">Confirm(&amp;C)</button><button type="button" class="mes-button" @click="dialog = null">Close(&amp;X)</button></footer></section>
      <section v-else class="dialog picker" role="dialog" aria-modal="true" aria-label="Node ID 不同"><header><strong>相同作業站，不同 NodeID</strong></header><p>來源批與參考批的 OP No 相同，但 NodeID 不同。仍要執行 DEV 載具併批嗎？</p><footer><button type="button" class="mes-button confirm" @click="confirmMerge(true)">繼續</button><button type="button" class="mes-button" @click="dialog = null">取消</button></footer></section>
    </div>
  </section>
</template>

<style scoped>
.wp214-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}.reference-frame,.merge-frame,.transfer-frame,.slot-info,.slot-columns fieldset{min-width:0;margin:0;padding:5px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}legend{padding:0 5px}.reference-frame{height:145px;flex:0 0 145px}.merge-frame{flex:1;min-height:260px;display:flex;flex-direction:column}.control-row,.transfer-frame,.slot-info{display:flex;align-items:center;gap:8px;flex-wrap:wrap}.control-row{min-height:36px}.control-row label:first-child{min-width:92px;text-align:right}.mes-input{height:26px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box;max-width:100%}.control-row .mes-input{width:168px}.mes-input.small{width:50px}.editable{background:#fff0b3}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.ref-grid{height:80px}.transfer-frame{min-height:69px;margin:5px 0 7px}.transfer-frame label{display:flex;align-items:center;gap:4px}.transfer-frame .mes-input{width:105px}.merge-grid{flex:1;min-height:130px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.reference td{background:#ffea78}.grid-wrap tfoot th{position:sticky;bottom:0;top:auto;background:#e8e4d7}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:40px}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.dialog{max-height:88vh;overflow:auto;display:flex;flex-direction:column;gap:9px;padding:14px;border:1px solid #999;background:#fff;box-shadow:0 8px 28px #0004;box-sizing:border-box}.picker{width:min(600px,94vw)}.dialog header{display:flex;justify-content:space-between;gap:12px}.dialog header small{color:#666}.dialog footer{display:flex;justify-content:flex-end;gap:8px}.dialog p{margin:0}.option{display:flex;flex-direction:column;gap:4px;padding:9px;border:1px solid #ccc;background:#fff;text-align:left;color:#333;font:inherit;cursor:pointer}.option:hover{background:#fff3cc}.option small{color:#666}.mo-option{display:flex;align-items:center;gap:8px}.slot-dialog{width:min(980px,96vw)}.slot-info{min-height:49px;gap:25px}.slot-columns{display:grid;grid-template-columns:minmax(0,1fr) 55px minmax(0,1fr);gap:8px;min-height:350px}.slot-columns fieldset{display:flex;flex-direction:column;gap:7px}.slot-grid{flex:1;min-height:200px}.slot-grid tr{cursor:pointer}.slot-grid tr.active td{background:#f0d074}.slot-grid tr.original td{background:#e1e1e1}.slot-grid tr.moved td{background:#fff4bc}.slot-grid tr.active td{outline:1px solid #b6811f}.move-buttons{display:flex;flex-direction:column;justify-content:center;gap:8px}.slot-caption{color:#666}.slot-columns select{min-width:115px}@media(max-width:760px){.slot-columns{grid-template-columns:1fr}.move-buttons{flex-direction:row}.reference-frame{height:135px;flex-basis:135px}}
</style>

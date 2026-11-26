<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type WoStatus = 0 | 1 | 2 | 3 | 4
type FinishMode = 'item' | 'work-order'
interface WorkOrder { woNo: string; woType: number; issueState: number; description: string; creator: string; createDate: string }
interface WorkOrderDetail { woNo: string; woItemNo: number; equipmentNo: string; serialNo: string; pmSerialNo: number | null; pmeqpSopNo: string; pmSopNo: string; frequencyNo: string; pmEstDateTime: string; woStatus: WoStatus; releaseDate: string; finishedDate: string; executor: string; executeDesc: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface WorkOrderState { version: 1; orders: WorkOrder[]; details: WorkOrderDetail[] }
interface PlanBridge { version: 1; records: unknown[]; plans: Array<Record<string, unknown>>; planLogs: Array<Record<string, unknown>> }
interface SopRecord { sopNo: string; sopName: string; description: string; steps: Array<{ pmItemNo: string; serialNo: number }> }
interface PmItem { pmItemNo: string; pmItemName: string; tools: Array<{ toolNo: string; toolQty: number }>; substitutions: Array<{ substitutionNo: string; substitutionQty: number; unitNo: string }> }
interface HistoryState { version: 1; orders: WorkOrder[]; details: WorkOrderDetail[] }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const orderStorageKey = 'ymes.dev.pm.work-orders.v1'
const planStorageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const equipmentStateStorageKey = 'ymes.dev.pm.wo-release-equipment-state.v1'
const historyStorageKey = 'ymes.dev.pm.work-orders.history.v1'
const woStatusNames: Record<number, string> = { 0: '待簽核', 1: '待料', 2: '已下線', 3: '已完成', 4: '已撤線' }
const equipmentStateNames: Record<number, string> = { 1: '待機', 2: '運轉中', 3: '維修中', 4: '停機' }
const equipmentNames: Record<string, string> = { 'CNC-02': 'CNC 加工中心 02', 'CNC-03': 'CNC 加工中心 03', 'PUMP-07': '製程冷卻泵 07', 'OVEN-05': '真空烘箱 05' }
const fallbackSops: SopRecord[] = [
  { sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序', description: '清潔腔體與載台，完成後確認無殘留物。', steps: [{ pmItemNo: 'PM-CLEAN-01', serialNo: 1 }, { pmItemNo: 'PM-LUBE-03', serialNo: 2 }] },
  { sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', description: '以標準器確認輸出值並記錄結果。', steps: [{ pmItemNo: 'PM-CAL-02', serialNo: 1 }] },
  { sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', description: '依週期補充規定油脂並記錄用量。', steps: [{ pmItemNo: 'PM-LUBE-03', serialNo: 1 }] },
]
const finishStateControl = { configured: true, state: 1, label: '待機' }

function localDate(value = new Date()) { return value.toLocaleDateString('sv-SE') }
function localTime(value = new Date()) { return value.toTimeString().slice(0, 8) }
function nowText() { return `${localDate()} ${localTime()}` }
function dateOnly(value: string) { return (value ?? '').slice(0, 10) }
function makeFinishFixture(): WorkOrderState {
  const createdAt = '2026-11-24 16:10:00'
  const releasedAt = '2026-11-25 10:20:00'
  const order: WorkOrder = { woNo: 'EW26110027', woType: 1, issueState: 2, description: 'CNC-03 感測器回裝後確認', creator: 'PMUSER', createDate: createdAt }
  const detail = (woItemNo: number, equipmentNo: string, pmSopNo: string): WorkOrderDetail => ({
    woNo: order.woNo, woItemNo, equipmentNo, serialNo: '', pmSerialNo: null, pmeqpSopNo: '', pmSopNo,
    frequencyNo: '', pmEstDateTime: '2026-11-25', woStatus: 2, releaseDate: releasedAt, finishedDate: '',
    executor: '', executeDesc: '', creator: order.creator, createDate: createdAt, reviser: '', reviseDate: '', dataStamp: 0,
  })
  return { version: 1, orders: [order], details: [detail(1, 'CNC-03', 'SOP-CAL-02'), detail(2, 'OVEN-05', 'SOP-CLEAN-01')] }
}
function loadWorkOrders(): WorkOrderState {
  let value: WorkOrderState | null = null
  try {
    const parsed = JSON.parse(window.localStorage.getItem(orderStorageKey) ?? 'null') as WorkOrderState | null
    if (parsed?.version === 1 && Array.isArray(parsed.orders) && Array.isArray(parsed.details)) value = parsed
  } catch { /* Use local DEV fixture. */ }
  if (!value) {
    const seeded = makeFinishFixture()
    try { window.localStorage.setItem(orderStorageKey, JSON.stringify(seeded)) } catch { /* Keep fixture in memory. */ }
    return seeded
  }
  if (value.details.some((row) => row.woStatus === 2)) return value
  const fixture = makeFinishFixture()
  return { version: 1, orders: [...value.orders, ...fixture.orders], details: [...value.details, ...fixture.details] }
}
function readPlanBridge(): PlanBridge {
  try {
    const value = JSON.parse(window.localStorage.getItem(planStorageKey) ?? 'null') as PlanBridge | null
    if (value?.version === 1 && Array.isArray(value.records) && Array.isArray(value.plans) && Array.isArray(value.planLogs)) return value
  } catch { /* Use empty DEV bridge. */ }
  return { version: 1, records: [], plans: [], planLogs: [] }
}
function readEquipmentStates() {
  try {
    const value = JSON.parse(window.localStorage.getItem(equipmentStateStorageKey) ?? 'null') as Record<string, number> | null
    if (value && typeof value === 'object') return value
  } catch { /* Use local fixture. */ }
  return { 'CNC-02': 2, 'CNC-03': 1, 'PUMP-07': 2, 'OVEN-05': 1 }
}
function readHistory(): HistoryState {
  try {
    const value = JSON.parse(window.localStorage.getItem(historyStorageKey) ?? 'null') as HistoryState | null
    if (value?.version === 1 && Array.isArray(value.orders) && Array.isArray(value.details)) return value
  } catch { /* Use empty history. */ }
  return { version: 1, orders: [], details: [] }
}
function readStoredValue(key: string): string | null { try { return window.localStorage.getItem(key) } catch { return null } }
function readSops(): SopRecord[] {
  try {
    const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) return value.records.map((row) => ({
      sopNo: String(row.sopNo ?? ''), sopName: String(row.sopName ?? ''), description: String(row.description ?? ''),
      steps: Array.isArray(row.steps) ? row.steps.map((step) => ({ pmItemNo: String((step as Record<string, unknown>).pmItemNo ?? ''), serialNo: Number((step as Record<string, unknown>).serialNo ?? 0) })) : [],
    })).filter((row) => row.sopNo)
  } catch { /* Use local SOP fixture. */ }
  return fallbackSops
}
function readItems(): PmItem[] {
  try {
    const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.maintenance-items.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) return value.records.map((row) => ({
      pmItemNo: String(row.pmItemNo ?? ''), pmItemName: String(row.pmItemName ?? ''),
      tools: Array.isArray(row.tools) ? row.tools.map((tool) => ({ toolNo: String((tool as Record<string, unknown>).toolNo ?? ''), toolQty: Number((tool as Record<string, unknown>).toolQty ?? 0) })) : [],
      substitutions: Array.isArray(row.substitutions) ? row.substitutions.map((item) => ({ substitutionNo: String((item as Record<string, unknown>).substitutionNo ?? ''), substitutionQty: Number((item as Record<string, unknown>).substitutionQty ?? 0), unitNo: String((item as Record<string, unknown>).unitNo ?? '') })) : [],
    }))
  } catch { /* Use no item details. */ }
  return []
}
function loadEquipmentSopMap(): Record<string, boolean> {
  try {
    const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.eqp-type-sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) return Object.fromEntries(value.records.map((row) => [String(row.pmeqpSopNo ?? ''), Boolean(row.lockEquipment)]))
  } catch { /* Treat unknown SOP as unlocked. */ }
  return {}
}

const state = ref(loadWorkOrders())
const equipmentState = ref(readEquipmentStates())
const equipmentSops = computed(() => loadEquipmentSopMap())
const sopRecords = computed(() => readSops())
const pmItems = computed(() => readItems())
const filters = reactive({ woNos: [] as string[], creators: [] as string[], woType: 'all', fromDate: '', toDate: '' })
const applied = ref({ ...filters })
const resultWoNos = ref<string[]>([])
const selectedWoNo = ref('')
const focusedItemNo = ref<number | null>(null)
const selectedSopNo = ref('')
const sopOpen = ref(false)
const finishMode = ref<FinishMode | null>(null)
const finishDate = ref(localDate())
const finishTime = ref(localTime())
const finishDescription = ref('')
const statusMessage = ref('')
const messageKind = ref<'success' | 'error'>('success')
const activeOrder = computed(() => state.value.orders.find((row) => row.woNo === selectedWoNo.value) ?? null)
const creators = computed(() => Array.from(new Set(state.value.orders.map((row) => row.creator))).sort())
const orderRows = computed(() => resultWoNos.value.map((no) => state.value.orders.find((row) => row.woNo === no)).filter((row): row is WorkOrder => Boolean(row)))
const detailRows = computed(() => state.value.details.filter((row) => row.woNo === activeOrder.value?.woNo && row.woStatus === 2).sort((a, b) => a.woItemNo - b.woItemNo))
const focusedDetail = computed(() => detailRows.value.find((row) => row.woItemNo === focusedItemNo.value) ?? detailRows.value[0] ?? null)
const pendingCount = computed(() => activeOrder.value ? state.value.details.filter((row) => row.woNo === activeOrder.value?.woNo && row.woStatus === 2).length : 0)
const selectedSop = computed(() => sopRecords.value.find((row) => row.sopNo === selectedSopNo.value) ?? null)
const sopItems = computed(() => (selectedSop.value?.steps ?? []).map((step) => ({ step, item: pmItems.value.find((row) => row.pmItemNo === step.pmItemNo) })).filter((row) => Boolean(row.item)))
const finishRows = computed(() => {
  if (!activeOrder.value || !finishMode.value) return []
  if (finishMode.value === 'item') return focusedDetail.value ? [focusedDetail.value] : []
  return state.value.details.filter((row) => row.woNo === activeOrder.value?.woNo && row.woStatus === 2).sort((a, b) => a.woItemNo - b.woItemNo)
})
const finishAt = computed(() => new Date(`${finishDate.value}T${finishTime.value}`))
const earlyFinishRows = computed(() => finishRows.value.filter((row) => row.releaseDate && finishAt.value.getTime() < new Date(row.releaseDate.replace(' ', 'T')).getTime()))
function toast(text: string, kind: 'success' | 'error' = 'success') { statusMessage.value = text; messageKind.value = kind }
function typeName(value: number) { return value === 0 ? 'PM 工單' : value === 1 ? '需求工單' : `工單 ${value}` }
function equipmentStateName(no: string) { const value = equipmentState.value[no]; return value ? equipmentStateNames[value] ?? `狀態 ${value}` : '未設定' }
function statusName(value: number) { return woStatusNames[value] ?? `狀態 ${value}` }
function runQuery() {
  applied.value = { ...filters }
  const q = applied.value
  const hasReleased = new Set(state.value.details.filter((row) => row.woStatus === 2).map((row) => row.woNo))
  resultWoNos.value = state.value.orders.filter((row) => row.issueState === 2 && hasReleased.has(row.woNo)
    && (q.woType === 'all' || row.woType === Number(q.woType))
    && (!q.woNos.length || q.woNos.includes(row.woNo))
    && (!q.creators.length || q.creators.includes(row.creator))
    && (!q.fromDate || dateOnly(row.createDate) >= q.fromDate)
    && (!q.toDate || dateOnly(row.createDate) <= q.toDate))
    .sort((a, b) => a.woNo.localeCompare(b.woNo)).map((row) => row.woNo)
  if (!resultWoNos.value.includes(selectedWoNo.value)) selectedWoNo.value = resultWoNos.value[0] ?? ''
  focusedItemNo.value = state.value.details.find((row) => row.woNo === selectedWoNo.value && row.woStatus === 2)?.woItemNo ?? null
  toast(resultWoNos.value.length ? `查到 ${resultWoNos.value.length} 張含已下線明細的工單。` : '沒有符合條件且含已下線明細的已生效工單。')
}
function selectOrder(no: string) { selectedWoNo.value = no; focusedItemNo.value = detailRows.value[0]?.woItemNo ?? null }
function selectDetail(row: WorkOrderDetail) { focusedItemNo.value = row.woItemNo }
function openFinish(mode: FinishMode) {
  if (!activeOrder.value) return toast('請先選取工單主檔。', 'error')
  if (mode === 'item' && !focusedDetail.value) return toast('請先選取已下線的工單明細。', 'error')
  if (mode === 'work-order' && !pendingCount.value) return toast('目前工單沒有已下線明細。', 'error')
  finishMode.value = mode
  finishDate.value = localDate()
  finishTime.value = localTime()
  finishDescription.value = ''
  statusMessage.value = ''
}
function closeFinish() { finishMode.value = null }
function openSop(row?: WorkOrderDetail) {
  const detail = row ?? focusedDetail.value
  if (!detail) return toast('請先選取工單明細。', 'error')
  selectedSopNo.value = detail.pmSopNo
  sopOpen.value = true
}
function submitFinish() {
  const order = activeOrder.value
  const rows = finishRows.value
  if (!order || order.issueState !== 2) return toast('只能回報已生效工單。', 'error')
  if (!rows.length || rows.some((row) => row.woNo !== order.woNo || row.woStatus !== 2)) return toast('回報列必須屬於目前工單且狀態為已下線。', 'error')
  if (Number.isNaN(finishAt.value.getTime())) return toast('完成日期或時間無效。', 'error')

  const finishDateText = `${finishDate.value} ${finishTime.value}`
  const previousOrders = readStoredValue(orderStorageKey)
  const previousPlan = readStoredValue(planStorageKey)
  const previousStates = readStoredValue(equipmentStateStorageKey)
  const previousHistory = readStoredValue(historyStorageKey)
  const nextState = JSON.parse(JSON.stringify(state.value)) as WorkOrderState
  let planBridge = readPlanBridge()
  if (order.woType === 0) {
    for (const row of rows) {
      const log = planBridge.planLogs.find((item) => String(item.serialNo ?? '') === row.serialNo)
      if (!row.serialNo || !log || Number(log.executeState ?? -1) !== 3) return toast(`PM 明細 ${row.woItemNo} 找不到已下線的 PlanLog，未完成回報。`, 'error')
    }
    const serials = new Set(rows.map((row) => row.serialNo))
    planBridge.planLogs = planBridge.planLogs.map((item) => serials.has(String(item.serialNo ?? '')) ? { ...item, executeState: 4, woFinishedDate: finishDateText } : item)
  }

  const rowKeys = new Set(rows.map((row) => `${row.woNo}:${row.woItemNo}`))
  nextState.details = nextState.details.map((row) => rowKeys.has(`${row.woNo}:${row.woItemNo}`)
    ? { ...row, woStatus: 3, finishedDate: finishDateText, executor: 'DEVUSER', executeDesc: finishDescription.value.trim() || row.executeDesc }
    : row)
  const nextEquipmentState = { ...equipmentState.value }
  if (finishStateControl.configured) {
    for (const row of rows) if (equipmentSops.value[row.pmeqpSopNo] !== false) nextEquipmentState[row.equipmentNo] = finishStateControl.state
  }
  const archiveOrder = !nextState.details.some((row) => row.woNo === order.woNo && row.woStatus !== 3)
  const history = readHistory()
  if (archiveOrder) {
    history.orders.push(order)
    history.details.push(...nextState.details.filter((row) => row.woNo === order.woNo))
    nextState.orders = nextState.orders.filter((row) => row.woNo !== order.woNo)
    nextState.details = nextState.details.filter((row) => row.woNo !== order.woNo)
  }

  const restore = (key: string, value: string | null) => { try { if (value === null) window.localStorage.removeItem(key); else window.localStorage.setItem(key, value) } catch { /* Keep in-memory DEV state. */ } }
  try {
    if (order.woType === 0) window.localStorage.setItem(planStorageKey, JSON.stringify(planBridge))
    window.localStorage.setItem(orderStorageKey, JSON.stringify(nextState))
    if (finishStateControl.configured) window.localStorage.setItem(equipmentStateStorageKey, JSON.stringify(nextEquipmentState))
    if (archiveOrder) window.localStorage.setItem(historyStorageKey, JSON.stringify(history))
  } catch {
    if (order.woType === 0) restore(planStorageKey, previousPlan)
    restore(orderStorageKey, previousOrders)
    if (finishStateControl.configured) restore(equipmentStateStorageKey, previousStates)
    if (archiveOrder) restore(historyStorageKey, previousHistory)
    return toast('DEV 資料無法保存，工單沒有完成回報。', 'error')
  }

  state.value = nextState
  equipmentState.value = nextEquipmentState
  finishMode.value = null
  if (archiveOrder) {
    selectedWoNo.value = ''
    resultWoNos.value = resultWoNos.value.filter((no) => no !== order.woNo)
    toast(`已回報 ${rows.length} 筆明細；全部明細完成，工單已移入 DEV 歷史。`)
  } else {
    toast(`已回報 ${rows.length} 筆明細（DEV）；設備狀態模擬設為「${finishStateControl.label}」。`)
    focusedItemNo.value = detailRows.value[0]?.woItemNo ?? null
  }
}
watch(state, () => {
  try { window.localStorage.setItem(orderStorageKey, JSON.stringify(state.value)) } catch { /* Keep interactions in memory. */ }
}, { deep: true })
runQuery()
</script>

<template>
  <main class="pm-finish-page" aria-label="保養工單完成與回報 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">PM WO FINISH / PM303</p><h1>保養工單完成與回報</h1><p class="subtitle">回報已下線的工單明細，並在整張工單完成時移入歷史。</p></div>
      <button type="button" class="button close" @click="emit('close')">Close (X)</button>
    </header>

    <section class="query-panel">
      <div class="query-title"><div><p class="eyebrow">QUERY</p><h2>工單條件</h2></div><span>只列出已生效且含已下線明細的工單</span></div>
      <div class="query-grid">
        <label>WO No<select v-model="filters.woNos" multiple size="3"><option v-for="row in state.orders" :key="row.woNo" :value="row.woNo">{{ row.woNo }}</option></select></label>
        <label>Creator<select v-model="filters.creators" multiple size="3"><option v-for="creator in creators" :key="creator" :value="creator">{{ creator }}</option></select></label>
        <fieldset class="type-filter"><legend>WO Type</legend><label><input v-model="filters.woType" type="radio" value="all">全部</label><label><input v-model="filters.woType" type="radio" value="0">PM Order</label><label><input v-model="filters.woType" type="radio" value="1">REQ Order</label></fieldset>
        <label>Create Date From<input v-model="filters.fromDate" type="date"></label>
        <label>Create Date To<input v-model="filters.toDate" type="date"></label>
        <button type="button" class="button primary" @click="runQuery">Query</button>
      </div>
    </section>

    <section class="summary-row"><div><span>符合工單</span><strong>{{ orderRows.length }}</strong></div><div><span>目前工單</span><strong>{{ activeOrder?.woNo ?? '—' }}</strong></div><div><span>已下線明細</span><strong>{{ pendingCount }}</strong></div><div><span>目前設備狀態</span><strong>{{ focusedDetail ? equipmentStateName(focusedDetail.equipmentNo) : '—' }}</strong></div><div><span>完工狀態控制</span><strong>PMWoFinished → {{ finishStateControl.label }}</strong></div></section>

    <section class="data-panel">
      <header class="panel-heading"><div><p class="eyebrow">WORK ORDER</p><h2>工單主檔 <small>{{ orderRows.length }} 張</small></h2></div><button type="button" class="button primary" :disabled="!activeOrder || !pendingCount" @click="openFinish('work-order')">Repay by WO</button></header>
      <div class="table-scroll"><table><thead><tr><th></th><th>WONo</th><th>WOType</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
        <tr v-for="row in orderRows" :key="row.woNo" :class="{ active: selectedWoNo === row.woNo }" @click="selectOrder(row.woNo)"><td><input type="radio" name="active-finish-order" :checked="selectedWoNo === row.woNo" :aria-label="`選取工單 ${row.woNo}`" @click.stop="selectOrder(row.woNo)"></td><td><strong>{{ row.woNo }}</strong></td><td>{{ typeName(row.woType) }}</td><td>{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr>
        <tr v-if="!orderRows.length"><td colspan="6" class="empty">沒有符合條件且含已下線明細的已生效工單。</td></tr>
      </tbody></table></div>
    </section>

    <section class="data-panel detail-panel">
      <header class="panel-heading detail-heading"><div><p class="eyebrow">RELEASED WORK ORDER ITEMS</p><h2>{{ activeOrder?.woNo ?? '請先選取工單' }} <small>已下線明細</small></h2></div></header>
      <div class="detail-actions"><span>Repay by Item 只回報作用中明細；Repay by WO 回報本工單所有 WOStatus=2 明細。</span><div><button type="button" class="button secondary" :disabled="!focusedDetail" @click="openSop()">Show Detail</button><button type="button" class="button primary" :disabled="!focusedDetail" @click="openFinish('item')">Repay by Item</button></div></div>
      <div class="table-scroll details-table"><table><thead><tr><th></th><th>Item</th><th>EquipmentNo</th><th>PMSOPNo</th><th>PMEstDateTime</th><th>WOStatus</th><th>ReleaseDate</th><th>FinishedDate</th><th>Executor</th><th>ExecuteDesc</th></tr></thead><tbody>
        <tr v-for="row in detailRows" :key="`${row.woNo}:${row.woItemNo}`" :class="{ active: focusedItemNo === row.woItemNo }" @click="selectDetail(row)"><td><input type="radio" name="active-finish-item" :checked="focusedItemNo === row.woItemNo" :aria-label="`選取明細 ${row.woItemNo}`" @click.stop="selectDetail(row)"></td><td>{{ row.woItemNo }}</td><td>{{ row.equipmentNo }}<small>{{ equipmentNames[row.equipmentNo] ?? '' }}</small></td><td>{{ row.pmSopNo }}</td><td>{{ row.pmEstDateTime }}</td><td><span class="status-pill status-2">{{ statusName(row.woStatus) }}</span></td><td>{{ row.releaseDate || '—' }}</td><td>{{ row.finishedDate || '—' }}</td><td>{{ row.executor || '—' }}</td><td>{{ row.executeDesc || '—' }}</td></tr>
        <tr v-if="!detailRows.length"><td colspan="10" class="empty">這張工單目前沒有 WOStatus=2 的明細。</td></tr>
      </tbody></table></div>
      <p class="source-note">DEV 模擬 `PMWoFinished` 設備狀態控制與 localStorage；未連 EQP Service 或資料庫。</p>
    </section>

    <p v-if="statusMessage" class="toast" :class="{ error: messageKind === 'error' }" role="status">{{ statusMessage }}</p>
    <footer class="page-footer"><span>來源：frmPMWoFinish／frmPMWoFinishDef · wsPM.RepayForWoDetail · clsPMBasis.RepayForWoDetail</span><span>DEV 假資料</span></footer>

    <div v-if="finishMode" class="modal-backdrop" @click.self="closeFinish"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="finish-title"><header><div><p class="eyebrow">FINISH WORK ORDER</p><h2 id="finish-title">{{ finishMode === 'item' ? 'Repay by Item' : 'Repay by WO' }}</h2></div><button type="button" class="button secondary" @click="closeFinish">Close (X)</button></header>
      <div class="finish-fields"><label>PMWONo<input :value="activeOrder?.woNo ?? ''" readonly></label><label v-if="finishMode === 'item'">Item No<input :value="finishRows[0]?.woItemNo ?? ''" readonly></label><label>WO Type<input :value="activeOrder ? typeName(activeOrder.woType) : ''" readonly></label><label>Repay By<input value="DEVUSER" readonly></label><label>Finished Date<input v-model="finishDate" type="date"></label><label>Finished Time<input v-model="finishTime" type="time" step="1"></label><label class="description-field">Execute Description<textarea v-model="finishDescription" rows="4"></textarea></label></div>
      <p v-if="earlyFinishRows.length" class="warning">完成時間早於 {{ earlyFinishRows.map(row => `明細 ${row.woItemNo} 下線時間`).join('、') }}。舊 WinForms 會提示，但仍允許送出。</p>
      <p class="source-note">本次回報 {{ finishRows.length }} 筆；若工單所有明細均完成，舊 Core 會搬移主檔／明細至歷史表並刪除活動資料。</p><footer><button type="button" class="button secondary" @click="closeFinish">Cancel</button><button type="button" class="button primary" @click="submitFinish">Confirm</button></footer>
    </section></div>

    <div v-if="sopOpen" class="modal-backdrop" @click.self="sopOpen = false"><section class="dialog sop-dialog" role="dialog" aria-modal="true" aria-labelledby="sop-title"><header><div><p class="eyebrow">PM SOP</p><h2 id="sop-title">{{ selectedSop?.sopNo ?? selectedSopNo }}｜{{ selectedSop?.sopName ?? 'SOP 假資料' }}</h2></div><button type="button" class="button secondary" @click="sopOpen = false">Close (X)</button></header><p>{{ selectedSop?.description || '此 SOP 在 DEV 假資料中沒有說明。' }}</p><div class="sop-steps"><article v-for="row in sopItems" :key="`${row.step.pmItemNo}:${row.step.serialNo}`"><strong>{{ row.step.serialNo }}. {{ row.item?.pmItemName || row.step.pmItemNo }}</strong><small>{{ row.item?.pmItemNo }} · 工具 {{ row.item?.tools.map(tool => `${tool.toolNo} × ${tool.toolQty}`).join('、') || '無' }} · 替代備品 {{ row.item?.substitutions.map(item => `${item.substitutionNo} × ${item.substitutionQty} ${item.unitNo}`).join('、') || '無' }}</small></article><p v-if="sopItems.length === 0" class="empty">沒有可顯示的 SOP 項目。</p></div></section></div>
  </main>
</template>

<style scoped>
.pm-finish-page { display: flex; flex-direction: column; gap: 10px; height: 100%; min-height: 0; overflow: auto; padding: 14px; box-sizing: border-box; background: #f7f5ee; color: #2e2d27; font: 12px/1.4 Arial, "Microsoft JhengHei", sans-serif; }
.page-heading, .query-title, .panel-heading { display: flex; align-items: center; justify-content: space-between; gap: 14px; }.page-heading h1 { margin: 2px 0; font-size: 23px; }.subtitle { margin: 0; color: #706e64; }.eyebrow { margin: 0; color: #8a877b; font-size: 10px; font-weight: 700; letter-spacing: .13em; }
.button { min-height: 30px; padding: 5px 12px; border: 1px solid #b7b3a7; border-radius: 4px; background: #fff; color: #37362f; font: inherit; cursor: pointer; }.button:disabled { opacity: .45; cursor: not-allowed; }.button.primary { border-color: #3b5f51; background: #3b5f51; color: #fff; }.button.secondary { background: #efeee8; }
.query-panel, .data-panel, .summary-row > div { border: 1px solid #dfddd4; border-radius: 6px; background: #fff; }.query-panel { padding: 13px; }.query-title h2, .panel-heading h2 { margin: 2px 0 0; font-size: 16px; }.query-title > span { color: #777469; font-size: 11px; }.query-grid { display: grid; grid-template-columns: minmax(145px, 1fr) minmax(115px, .8fr) minmax(260px, 1.4fr) minmax(135px, .8fr) minmax(135px, .8fr) auto; gap: 9px; align-items: end; margin-top: 12px; }.query-grid > label, .finish-fields > label { display: grid; gap: 4px; color: #68665d; font-size: 11px; }.query-grid input:not([type=radio]), .query-grid select, .finish-fields input, .finish-fields textarea { box-sizing: border-box; width: 100%; min-height: 31px; padding: 5px 7px; border: 1px solid #d8d5ca; border-radius: 3px; font: inherit; }.query-grid select[multiple] { height: 80px; padding: 3px; }.type-filter { display: flex; gap: 9px; min-height: 31px; align-items: center; margin: 0; padding: 2px 8px; border: 1px solid #e0ddd3; border-radius: 3px; color: #68665d; }.type-filter legend, .type-filter label { font-size: 10px; white-space: nowrap; }
.summary-row { display: grid; grid-template-columns: repeat(5, minmax(100px, 1fr)); gap: 8px; }.summary-row > div { display: flex; flex-direction: column; align-items: flex-start; justify-content: center; padding: 8px 11px; color: #777469; font-size: 11px; }.summary-row strong { color: #34332c; font-size: 15px; }.data-panel { overflow: hidden; }.panel-heading { min-height: 46px; padding: 9px 12px; border-bottom: 1px solid #ebe9e2; }.panel-heading h2 small { margin-left: 6px; color: #858277; font-size: 11px; font-weight: 400; }.table-scroll { max-height: 260px; overflow: auto; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }th { position: sticky; top: 0; z-index: 1; background: #f1f0eb; color: #69675e; text-align: left; font-weight: 600; }th, td { padding: 7px 8px; border-bottom: 1px solid #efeee9; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f7f8f3; }tr.active { background: #edf3ed; }td strong { font-weight: 600; }td small { display: block; margin-top: 2px; color: #89867a; font-size: 10px; }.empty { padding: 18px; color: #89867a; text-align: center; cursor: default; }
.detail-panel { flex: 1; min-height: 220px; }.detail-actions { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 8px 12px; border-bottom: 1px solid #ebe9e2; }.detail-actions > span { color: #777469; font-size: 11px; }.detail-actions > div { display: flex; flex-wrap: wrap; gap: 6px; }.status-pill { display: inline-block; min-width: 48px; padding: 2px 6px; border-radius: 10px; text-align: center; }.status-2 { background: #e7eef6; color: #42637f; }.source-note { margin: 0; padding: 7px 12px; color: #7b786e; font-size: 10px; background: #faf9f6; }.toast { position: sticky; bottom: 4px; align-self: flex-end; max-width: min(560px, 100%); margin: 0; padding: 8px 12px; border: 1px solid #d6dfd5; border-radius: 5px; background: #f0f5ef; color: #47614a; box-shadow: 0 3px 10px #00000010; }.toast.error { border-color: #ead0cc; background: #fff2ef; color: #904d42; }.page-footer { display: flex; justify-content: space-between; gap: 12px; color: #89867a; font-size: 10px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 18px; background: #24231f88; }.dialog { width: min(620px, 100%); max-height: 85vh; overflow: auto; padding: 16px; border-radius: 7px; background: #fff; box-shadow: 0 16px 55px #0004; }.dialog > header, .dialog > footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }.dialog h2 { margin: 4px 0 10px; font-size: 18px; }.finish-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }.description-field { grid-column: 1 / -1; }.finish-fields textarea { resize: vertical; }.warning { padding: 8px 10px; border: 1px solid #e8d39e; border-radius: 4px; background: #fff8e5; color: #77591c; }.dialog .source-note { margin: 9px -16px; }.dialog > footer { justify-content: flex-end; margin-top: 12px; }.sop-dialog { width: min(760px, 100%); }.sop-steps { display: grid; gap: 8px; }.sop-steps article { display: grid; gap: 3px; padding: 9px; border: 1px solid #e7e4db; border-radius: 4px; }.sop-steps small { color: #777469; white-space: normal; }
@media (max-width: 980px) { .query-grid { grid-template-columns: repeat(3, minmax(120px, 1fr)); }.summary-row { grid-template-columns: repeat(3, minmax(100px, 1fr)); } }
@media (max-width: 650px) { .pm-finish-page { padding: 10px; }.query-grid, .finish-fields { grid-template-columns: 1fr 1fr; }.summary-row { grid-template-columns: 1fr 1fr; }.detail-actions { align-items: flex-start; flex-direction: column; }.page-footer { flex-direction: column; } }
</style>

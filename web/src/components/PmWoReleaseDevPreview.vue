<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type WoStatus = 0 | 1 | 2 | 3 | 4
interface WorkOrder { woNo: string; woType: 0 | 1; issueState: number; description: string; creator: string; createDate: string }
interface WorkOrderDetail { woNo: string; woItemNo: number; equipmentNo: string; serialNo: string; pmSerialNo: number | null; pmeqpSopNo: string; pmSopNo: string; frequencyNo: string; pmEstDateTime: string; woStatus: WoStatus; releaseDate: string; finishedDate: string; executor: string; executeDesc: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface WorkOrderState { version: 1; orders: WorkOrder[]; details: WorkOrderDetail[] }
interface PlanBridge { version: 1; records: unknown[]; plans: Array<Record<string, unknown>>; planLogs: Array<Record<string, unknown>> }
interface PmEquipmentSummary { sincePMQty: number; sincePMTimes: number; lastPMDate: string }
interface EquipmentSop { pmeqpSopNo: string; lockEquipment: boolean; equipmentState: number | null; stateName: string }
interface SopRecord { sopNo: string; sopName: string; description: string; steps: Array<{ pmItemNo: string; serialNo: number }> }
interface PmItem { pmItemNo: string; pmItemName: string; tools: Array<{ toolNo: string; toolQty: number }>; substitutions: Array<{ substitutionNo: string; substitutionQty: number; unitNo: string }> }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.work-orders.v1'
const planStorageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const equipmentStateStorageKey = 'ymes.dev.pm.wo-release-equipment-state.v1'
const pmEquipmentStorageKey = 'ymes.dev.pm.wo-release-pm-equipment.v1'
const statusOptions: Array<{ value: WoStatus; label: string }> = [
  { value: 0, label: '待簽核' }, { value: 1, label: '待料' }, { value: 2, label: '已下線' }, { value: 3, label: '已完成' }, { value: 4, label: '已撤線' },
]
const statusFilter = reactive<Record<WoStatus, boolean>>({ 0: true, 1: true, 2: true, 3: true, 4: true })
const equipmentNames: Record<string, string> = { 'CNC-02': 'CNC 加工中心 02', 'CNC-03': 'CNC 加工中心 03', 'PUMP-07': '製程冷卻泵 07', 'OVEN-05': '真空烘箱 05' }
const fallbackEquipmentSops: EquipmentSop[] = [
  { pmeqpSopNo: 'EQPSOP2600001', lockEquipment: true, equipmentState: 3, stateName: '維修中' },
  { pmeqpSopNo: 'EQPSOP2600002', lockEquipment: false, equipmentState: null, stateName: '' },
]
const fallbackSops: SopRecord[] = [
  { sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序', description: '清潔腔體與載台，完成後確認無殘留物。', steps: [{ pmItemNo: 'PM-CLEAN-01', serialNo: 1 }, { pmItemNo: 'PM-LUBE-03', serialNo: 2 }] },
  { sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', description: '以標準器確認輸出值並記錄結果。', steps: [{ pmItemNo: 'PM-CAL-02', serialNo: 1 }] },
  { sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', description: '依週期補充規定油脂並記錄用量。', steps: [{ pmItemNo: 'PM-LUBE-03', serialNo: 1 }] },
]
function nowText() { return new Date().toLocaleString('sv-SE') }
function dateOnly(value: string) { return (value ?? '').slice(0, 10) }
function seedState(): WorkOrderState {
  const stamp = '2026-11-24 16:10:00'
  const detail = (woNo: string, equipmentNo: string, pmeqpSopNo: string, pmSopNo: string, woStatus: WoStatus, serialNo = ''): WorkOrderDetail => ({
    woNo, woItemNo: 1, equipmentNo, serialNo, pmSerialNo: serialNo ? 260001 : null, pmeqpSopNo, pmSopNo, frequencyNo: 'PM-MONTHLY-15',
    pmEstDateTime: '2026-11-28', woStatus, releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: 'PMUSER', createDate: stamp, reviser: '', reviseDate: '', dataStamp: 0,
  })
  return { version: 1,
    orders: [
      { woNo: 'EW26110021', woType: 0, issueState: 2, description: 'CNC-02 月中設備校驗', creator: 'PMUSER', createDate: stamp },
      { woNo: 'EW26110012', woType: 1, issueState: 0, description: 'CNC-02 感測器追加檢查', creator: 'PMUSER', createDate: stamp },
      { woNo: 'EW26110010', woType: 1, issueState: 1, description: 'PUMP-07 振動檢查', creator: 'DEV02', createDate: '2026-11-22 09:20:00' },
      { woNo: 'EW26110008', woType: 1, issueState: 2, description: 'OVEN-05 溫控器校正', creator: 'PMUSER', createDate: '2026-11-19 10:30:00' },
    ],
    details: [
      detail('EW26110021', 'CNC-02', 'EQPSOP2600001', 'SOP-CAL-02', 1, 'PMPLAN-260001'),
      detail('EW26110012', 'CNC-02', 'EQPSOP2600001', 'SOP-CAL-02', 0),
      detail('EW26110010', 'PUMP-07', 'EQPSOP2600002', 'SOP-LUBE-03', 0),
      detail('EW26110008', 'OVEN-05', 'EQPSOP2600001', 'SOP-CAL-02', 1),
    ],
  }
}
function loadState(): WorkOrderState {
  try { const value = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as WorkOrderState | null; if (value?.version === 1 && Array.isArray(value.orders) && Array.isArray(value.details)) return value }
  catch { /* Keep DEV interactions available in memory. */ }
  return seedState()
}
function loadPlanBridge(): PlanBridge {
  try { const value = JSON.parse(window.localStorage.getItem(planStorageKey) ?? 'null') as PlanBridge | null; if (value?.version === 1 && Array.isArray(value.records) && Array.isArray(value.plans) && Array.isArray(value.planLogs)) return value }
  catch { /* Use a local fixture. */ }
  return { version: 1, records: [], plans: [], planLogs: [{ serialNo: 'PMPLAN-260001', executeState: 2, pmWoNo: 'EW26110021', pmWoItemNo: 1, woCreateDate: '2026-11-24', equipmentNo: 'CNC-02' }] }
}
function loadPmEquipmentSummary(): Record<string, PmEquipmentSummary> {
  try {
    const value = JSON.parse(window.localStorage.getItem(pmEquipmentStorageKey) ?? 'null') as Record<string, PmEquipmentSummary> | null
    if (value && typeof value === 'object') return value
  } catch { /* Keep DEV fixture. */ }
  return {
    'CNC-02': { sincePMQty: 12, sincePMTimes: 2, lastPMDate: '2026-10-28' },
    'CNC-03': { sincePMQty: 5, sincePMTimes: 1, lastPMDate: '2026-11-04' },
    'PUMP-07': { sincePMQty: 18, sincePMTimes: 3, lastPMDate: '2026-10-21' },
    'OVEN-05': { sincePMQty: 7, sincePMTimes: 1, lastPMDate: '2026-10-30' },
  }
}
function readStoredValue(key: string): string | null { try { return window.localStorage.getItem(key) } catch { return null } }
function loadEquipmentSops(): EquipmentSop[] {
  try {
    const value = JSON.parse(window.localStorage.getItem(planStorageKey) ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) { const rows = value.records.map((row) => ({ pmeqpSopNo: String(row.pmeqpSopNo ?? ''), lockEquipment: Boolean(row.lockEquipment), equipmentState: row.equipmentState == null ? null : Number(row.equipmentState), stateName: String(row.stateName ?? '') })).filter((row) => row.pmeqpSopNo); if (rows.length) return rows }
  } catch { /* Use fallback state rules. */ }
  return fallbackEquipmentSops
}
function readSops(): SopRecord[] {
  try {
    const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) { const rows = value.records.map((row) => ({ sopNo: String(row.sopNo ?? ''), sopName: String(row.sopName ?? ''), description: String(row.description ?? ''), steps: Array.isArray(row.steps) ? row.steps.map((step) => ({ pmItemNo: String(step.pmItemNo ?? ''), serialNo: Number(step.serialNo ?? 0) })) : [] })).filter((row) => row.sopNo); if (rows.length) return rows }
  } catch { /* Use fallback SOPs. */ }
  return fallbackSops
}
function readItems(): PmItem[] {
  try {
    const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.maintenance-items.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(value?.records)) return value.records.map((row) => ({ pmItemNo: String(row.pmItemNo ?? ''), pmItemName: String(row.pmItemName ?? ''), tools: Array.isArray(row.tools) ? row.tools.map((tool) => ({ toolNo: String(tool.toolNo ?? ''), toolQty: Number(tool.toolQty ?? 0) })) : [], substitutions: Array.isArray(row.substitutions) ? row.substitutions.map((item) => ({ substitutionNo: String(item.substitutionNo ?? ''), substitutionQty: Number(item.substitutionQty ?? 0), unitNo: String(item.unitNo ?? '') })) : [] }))
  } catch { /* Use no item details. */ }
  return []
}

const state = ref(loadState())
const statusNames: Record<number, string> = { 1: '待機', 2: '運轉中', 3: '維修中', 4: '停機' }
const equipmentState = ref<Record<string, number>>({ 'CNC-02': 2, 'CNC-03': 1, 'PUMP-07': 2, 'OVEN-05': 1 })
try { const value = JSON.parse(window.localStorage.getItem('ymes.dev.pm.wo-release-equipment-state.v1') ?? 'null') as Record<string, number> | null; if (value) equipmentState.value = value } catch { /* Keep DEV fixture. */ }
const pmEquipmentSummary = ref(loadPmEquipmentSummary())
const statusMessage = ref('')
const messageKind = ref<'success' | 'error'>('success')
const filters = reactive({ woNos: [] as string[], creators: [] as string[], woType: 'all', fromDate: '', toDate: '' })
const applied = ref({ ...filters })
const resultWoNos = ref<string[]>([])
const selectedWoNo = ref('')
const focusedItemNo = ref<number | null>(null)
const selectedItemNos = ref<number[]>([])
const sopOpen = ref(false)
const sopNoForView = ref('')
const activeOrder = computed(() => state.value.orders.find((row) => row.woNo === selectedWoNo.value) ?? null)
const creators = computed(() => Array.from(new Set(state.value.orders.map((row) => row.creator))).sort())
const orderRows = computed(() => resultWoNos.value.map((no) => state.value.orders.find((row) => row.woNo === no)).filter((row): row is WorkOrder => Boolean(row)))
const detailRows = computed(() => { const order = activeOrder.value; if (!order) return []; const allowed = statusOptions.filter((row) => statusFilter[row.value]).map((row) => row.value); return state.value.details.filter((row) => row.woNo === order.woNo && allowed.includes(row.woStatus)).sort((a, b) => a.woItemNo - b.woItemNo) })
const selectedDetails = computed(() => detailRows.value.filter((row) => selectedItemNos.value.includes(row.woItemNo)))
const equipmentSops = computed(() => loadEquipmentSops())
const focusedPmEquipmentSummary = computed(() => focusedDetail.value ? pmEquipmentSummary.value[focusedDetail.value.equipmentNo] ?? null : null)
const sops = computed(() => readSops())
const items = computed(() => readItems())
const selectedSop = computed(() => sops.value.find((row) => row.sopNo === sopNoForView.value) ?? null)
const sopItems = computed(() => (selectedSop.value?.steps ?? []).map((step) => ({ step, item: items.value.find((row) => row.pmItemNo === step.pmItemNo) })).filter((row) => Boolean(row.item)))
const focusedDetail = computed(() => state.value.details.find((row) => row.woNo === activeOrder.value?.woNo && row.woItemNo === focusedItemNo.value) ?? null)
const pendingCount = computed(() => activeOrder.value ? state.value.details.filter((row) => row.woNo === activeOrder.value?.woNo && row.woStatus === 1).length : 0)
function toast(text: string, kind: 'success' | 'error' = 'success') { statusMessage.value = text; messageKind.value = kind }
function typeName(value: number) { return value === 0 ? 'PM 工單' : '需求工單' }
function statusName(value: number) { return statusOptions.find((row) => row.value === value)?.label ?? `狀態 ${value}` }
function equipmentStateName(no: string) { const value = equipmentState.value[no]; return value ? statusNames[value] ?? `狀態 ${value}` : '未設定' }
function runQuery() {
  applied.value = { ...filters }
  const q = applied.value
  const hasPrepare = new Set(state.value.details.filter((row) => row.woStatus === 1).map((row) => row.woNo))
  resultWoNos.value = state.value.orders.filter((row) => row.issueState === 2 && hasPrepare.has(row.woNo)
    && (q.woType === 'all' || row.woType === Number(q.woType))
    && (!q.woNos.length || q.woNos.includes(row.woNo))
    && (!q.creators.length || q.creators.includes(row.creator))
    && (!q.fromDate || dateOnly(row.createDate) >= q.fromDate)
    && (!q.toDate || dateOnly(row.createDate) <= q.toDate))
    .sort((a, b) => a.woNo.localeCompare(b.woNo)).map((row) => row.woNo)
  if (!resultWoNos.value.includes(selectedWoNo.value)) selectedWoNo.value = resultWoNos.value[0] ?? ''
  focusedItemNo.value = state.value.details.find((row) => row.woNo === selectedWoNo.value)?.woItemNo ?? null
  selectedItemNos.value = []
  toast(resultWoNos.value.length ? `查到 ${resultWoNos.value.length} 張工單。` : '沒有符合條件且含待料明細的已生效工單。')
}
function selectOrder(no: string) { selectedWoNo.value = no; selectedItemNos.value = []; focusedItemNo.value = detailRows.value[0]?.woItemNo ?? null }
function toggleDetail(row: WorkOrderDetail, checked: boolean) { focusedItemNo.value = row.woItemNo; selectedItemNos.value = checked ? Array.from(new Set([...selectedItemNos.value, row.woItemNo])) : selectedItemNos.value.filter((no) => no !== row.woItemNo) }
function equipmentSopFor(row: WorkOrderDetail) { return equipmentSops.value.find((sop) => sop.pmeqpSopNo === row.pmeqpSopNo) ?? null }
function releaseRows(rows: WorkOrderDetail[]) {
  const order = activeOrder.value
  if (!order || order.issueState !== 2) return toast('只能下線已生效的工單。', 'error')
  if (!rows.length) return toast('沒有選取待料明細。', 'error')
  if (rows.some((row) => row.woNo !== order.woNo || row.woStatus !== 1)) return toast('所選明細必須全部屬於目前工單且狀態為待料。', 'error')

  const now = nowText()
  const activeRow = state.value.details.find((row) => row.woNo === order.woNo && row.woItemNo === focusedItemNo.value) ?? rows[0]
  const activeSop = equipmentSopFor(activeRow)
  const applyEquipmentState = activeSop ? activeSop.equipmentState !== null : true
  const requestedEquipmentState = activeSop?.equipmentState ?? 3
  const nextState: WorkOrderState = JSON.parse(JSON.stringify(state.value)) as WorkOrderState
  nextState.details = nextState.details.map((row) => rows.some((item) => item.woNo === row.woNo && item.woItemNo === row.woItemNo)
    ? { ...row, woStatus: 2, releaseDate: now }
    : row)
  const nextEquipmentState = { ...equipmentState.value }
  const nextPmEquipmentSummary = JSON.parse(JSON.stringify(pmEquipmentSummary.value)) as Record<string, PmEquipmentSummary>
  if (applyEquipmentState) {
    for (const row of rows) if (equipmentSopFor(row)?.lockEquipment !== false) nextEquipmentState[row.equipmentNo] = requestedEquipmentState
  }
  for (const row of rows) nextPmEquipmentSummary[row.equipmentNo] = { ...(nextPmEquipmentSummary[row.equipmentNo] ?? { sincePMQty: 0, sincePMTimes: 0, lastPMDate: '' }), sincePMQty: 0, sincePMTimes: 0, lastPMDate: now }

  const previousWorkOrders = readStoredValue(storageKey)
  const previousPlanBridge = readStoredValue(planStorageKey)
  const previousEquipmentStates = readStoredValue(equipmentStateStorageKey)
  const previousPmEquipmentSummary = readStoredValue(pmEquipmentStorageKey)
  let bridge = loadPlanBridge()
  if (order.woType === 0) {
    for (const row of rows) {
      const log = bridge.planLogs.find((item) => String(item.serialNo ?? '') === row.serialNo)
      if (!row.serialNo || !log || Number(log.executeState ?? -1) !== 2) return toast(`PM 明細 ${row.woItemNo} 找不到待執行的 PlanLog，未下線。`, 'error')
    }
    const serials = new Set(rows.map((row) => row.serialNo))
    bridge.planLogs = bridge.planLogs.map((item) => serials.has(String(item.serialNo ?? '')) ? { ...item, executeState: 3, woReleaseDate: now } : item)
  }

  try {
    if (order.woType === 0) window.localStorage.setItem(planStorageKey, JSON.stringify(bridge))
    window.localStorage.setItem(storageKey, JSON.stringify(nextState))
    if (applyEquipmentState) window.localStorage.setItem(equipmentStateStorageKey, JSON.stringify(nextEquipmentState))
    window.localStorage.setItem(pmEquipmentStorageKey, JSON.stringify(nextPmEquipmentSummary))
  } catch {
    const restore = (key: string, value: string | null) => { try { if (value === null) window.localStorage.removeItem(key); else window.localStorage.setItem(key, value) } catch { /* Preserve the previous in-memory DEV view. */ } }
    if (order.woType === 0) restore(planStorageKey, previousPlanBridge)
    restore(storageKey, previousWorkOrders)
    if (applyEquipmentState) restore(equipmentStateStorageKey, previousEquipmentStates)
    restore(pmEquipmentStorageKey, previousPmEquipmentSummary)
    return toast('DEV 資料無法保存，工單沒有完成下線。', 'error')
  }

  state.value = nextState
  if (applyEquipmentState) equipmentState.value = nextEquipmentState
  pmEquipmentSummary.value = nextPmEquipmentSummary
  selectedItemNos.value = []
  const stateText = applyEquipmentState
    ? `；設備狀態模擬設為「${activeSop?.stateName || statusNames[requestedEquipmentState] || requestedEquipmentState}」`
    : '；此 SOP 未要求變更設備狀態'
  toast(`已下線 ${rows.length} 筆明細（DEV）${stateText}。`)
}
function releaseSelected() {
  const rows = selectedDetails.value.length ? selectedDetails.value : detailRows.value.filter((row) => row.woItemNo === focusedItemNo.value).slice(0, 1)
  releaseRows(rows)
}
function releaseAll() {
  if (!activeOrder.value) return toast('請先選取工單主檔。', 'error')
  releaseRows(state.value.details.filter((row) => row.woNo === activeOrder.value?.woNo && row.woStatus === 1))
}
function openSop(row?: WorkOrderDetail) {
  const detail = row ?? selectedDetails.value[0] ?? focusedDetail.value
  if (!detail) return toast('請先選取工單明細。', 'error')
  sopNoForView.value = detail.pmSopNo
  sopOpen.value = true
}
watch(state, () => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(state.value)) }
  catch { /* Keep DEV interactions in memory when storage is unavailable. */ }
}, { deep: true })
runQuery()
</script>

<template>
  <main class="pm-release-page" aria-label="保養工單下線 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">PM WO RELEASE / PM302</p><h1>保養工單下線</h1><p class="subtitle">依舊 WinForms Release 流程處理已生效工單的待料明細。</p></div>
      <button type="button" class="button close" @click="emit('close')">Close (X)</button>
    </header>

    <section class="query-panel">
      <div class="query-title"><div><p class="eyebrow">QUERY</p><h2>工單條件</h2></div><span>只列出已生效且含待料明細的工單</span></div>
      <div class="query-grid">
        <label>PMWONo<select v-model="filters.woNos" multiple size="3"><option v-for="row in state.orders" :key="row.woNo" :value="row.woNo">{{ row.woNo }}</option></select></label>
        <label>Creator<select v-model="filters.creators" multiple size="3"><option v-for="creator in creators" :key="creator" :value="creator">{{ creator }}</option></select></label>
        <fieldset class="type-filter"><legend>工單型態</legend><label><input v-model="filters.woType" type="radio" value="all">全部</label><label><input v-model="filters.woType" type="radio" value="0">PM Order</label><label><input v-model="filters.woType" type="radio" value="1">REQ Order</label></fieldset>
        <label>Create Date From<input v-model="filters.fromDate" type="date"></label>
        <label>Create Date To<input v-model="filters.toDate" type="date"></label>
        <button type="button" class="button primary" @click="runQuery">Query</button>
      </div>
    </section>

    <section class="summary-row"><div><span>符合工單</span><strong>{{ orderRows.length }}</strong></div><div><span>目前工單</span><strong>{{ activeOrder?.woNo ?? '—' }}</strong></div><div><span>待下線明細</span><strong>{{ pendingCount }}</strong></div><div><span>目前設備狀態</span><strong>{{ focusedDetail ? equipmentStateName(focusedDetail.equipmentNo) : '—' }}</strong></div><div><span>上次保養後數量／次數</span><strong>{{ focusedPmEquipmentSummary ? `${focusedPmEquipmentSummary.sincePMQty}／${focusedPmEquipmentSummary.sincePMTimes}` : '—' }}</strong><small>{{ focusedPmEquipmentSummary?.lastPMDate ? `上次保養 ${focusedPmEquipmentSummary.lastPMDate}` : '尚無保養紀錄' }}</small></div></section>

    <section class="data-panel">
      <header class="panel-heading"><div><p class="eyebrow">WORK ORDER</p><h2>工單主檔 <small>{{ orderRows.length }} 張</small></h2></div><span class="read-only-badge">IssueState = 2</span></header>
      <div class="table-scroll"><table><thead><tr><th></th><th>WONo</th><th>WOType</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody>
        <tr v-for="row in orderRows" :key="row.woNo" :class="{ active: selectedWoNo === row.woNo }" @click="selectOrder(row.woNo)"><td><input type="radio" name="active-order" :checked="selectedWoNo === row.woNo" :aria-label="`選取工單 ${row.woNo}`" @click.stop="selectOrder(row.woNo)"></td><td><strong>{{ row.woNo }}</strong></td><td>{{ typeName(row.woType) }}</td><td>{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr>
        <tr v-if="!orderRows.length"><td colspan="6" class="empty">沒有符合條件且含待料明細的已生效工單。</td></tr>
      </tbody></table></div>
    </section>

    <section class="data-panel detail-panel">
      <header class="panel-heading detail-heading"><div><p class="eyebrow">WORK ORDER ITEMS</p><h2>{{ activeOrder?.woNo ?? '請先選取工單' }} <small>下線明細</small></h2></div><div class="status-filters"><label v-for="option in statusOptions" :key="option.value"><input v-model="statusFilter[option.value]" type="checkbox">{{ option.label }}</label></div></header>
      <div class="detail-actions"><span>Release 處理選取列；Release All 處理此工單全部 WOStatus=1 明細。</span><div><button type="button" class="button secondary" :disabled="!detailRows.length" @click="openSop()">Show Detail</button><button type="button" class="button primary" :disabled="!activeOrder || !detailRows.some(row => row.woStatus === 1)" @click="releaseSelected">Release</button><button type="button" class="button primary" :disabled="!activeOrder || !pendingCount" @click="releaseAll">Release All</button></div></div>
      <div class="table-scroll"><table class="details-table"><thead><tr><th>選取</th><th>Item</th><th>EquipmentNo</th><th>PMSOPNo</th><th>PMEstDateTime</th><th>WOStatus</th><th>ReleaseDate</th><th>FinishedDate</th><th>Executor</th><th>ExecuteDesc</th></tr></thead><tbody>
        <tr v-for="row in detailRows" :key="row.woItemNo" :class="{ selected: selectedItemNos.includes(row.woItemNo), focused: focusedItemNo === row.woItemNo }" @click="toggleDetail(row, !selectedItemNos.includes(row.woItemNo))"><td @click.stop><input type="checkbox" :checked="selectedItemNos.includes(row.woItemNo)" :aria-label="`選取明細 ${row.woItemNo}`" @change="toggleDetail(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.woItemNo }}</td><td><strong>{{ row.equipmentNo }}</strong><small>{{ equipmentNames[row.equipmentNo] ?? row.equipmentNo }}</small></td><td>{{ row.pmSopNo }}</td><td>{{ dateOnly(row.pmEstDateTime) }}</td><td><span class="status-pill" :class="`status-${row.woStatus}`">{{ statusName(row.woStatus) }}</span></td><td>{{ row.releaseDate || '—' }}</td><td>{{ row.finishedDate || '—' }}</td><td>{{ row.executor || '—' }}</td><td>{{ row.executeDesc || '—' }}</td></tr>
        <tr v-if="!detailRows.length"><td colspan="10" class="empty">沒有符合狀態篩選的明細。</td></tr>
      </tbody></table></div>
      <p class="source-note">舊程式依 SOP 決定設備狀態，工單下線後再逐台呼叫 EQP 更新；DEV 使用作用列 SOP 狀態作近似模擬，不跑設備狀態轉換規則或 Chamber 子設備更新。</p>
    </section>

    <p v-if="statusMessage" class="toast" :class="messageKind" role="status">{{ statusMessage }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>僅模擬 WOStatus 1 → 2；正式 API／設備狀態交易未接。</span></footer>

    <div v-if="sopOpen" class="modal-backdrop" @click.self="sopOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="sop-title"><header><div><p class="eyebrow">SOP DETAIL</p><h2 id="sop-title">{{ selectedSop?.sopNo ?? sopNoForView }}｜{{ selectedSop?.sopName ?? '程序資料' }}</h2></div><button type="button" class="button secondary" @click="sopOpen = false">Close</button></header><p>{{ selectedSop?.description || '此 DEV 樣例沒有 SOP 說明。' }}</p><div v-if="sopItems.length" class="sop-steps"><article v-for="row in sopItems" :key="row.step.serialNo"><strong>{{ row.step.serialNo }}. {{ row.item?.pmItemNo }}</strong><span>{{ row.item?.pmItemName }}</span><small>工具：{{ row.item?.tools.length ? row.item.tools.map(item => `${item.toolNo} × ${item.toolQty}`).join('、') : '無' }}｜備品：{{ row.item?.substitutions.length ? row.item.substitutions.map(item => `${item.substitutionNo} × ${item.substitutionQty} ${item.unitNo}`).join('、') : '無' }}</small></article></div><p v-else class="empty">沒有可顯示的 SOP 步驟。</p></section></div>
  </main>
</template>

<style scoped>
.pm-release-page { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px; display: flex; flex-direction: column; gap: 12px; color: #28271f; background: #f5f3ec; font: 13px/1.4 Arial, "Microsoft JhengHei", sans-serif; }
.page-heading, .panel-heading, .detail-actions, .query-title { display: flex; align-items: center; justify-content: space-between; gap: 14px; }.page-heading h1 { margin: 2px 0; font-size: 23px; }.subtitle { margin: 0; color: #706e64; }.eyebrow { margin: 0; color: #8a877b; font-size: 10px; font-weight: 700; letter-spacing: .13em; }
.button { min-height: 30px; padding: 5px 12px; border: 1px solid #b7b3a7; border-radius: 4px; background: #fff; color: #37362f; font: inherit; cursor: pointer; }.button:disabled { opacity: .45; cursor: not-allowed; }.button.primary { border-color: #3b5f51; background: #3b5f51; color: #fff; }.button.secondary { background: #efeee8; }.query-panel, .data-panel, .summary-row > div { border: 1px solid #dfddd4; border-radius: 6px; background: #fff; }.query-panel { padding: 13px; }.query-title h2, .panel-heading h2 { margin: 2px 0 0; font-size: 16px; }.query-title > span { color: #777469; font-size: 11px; }.query-grid { display: grid; grid-template-columns: minmax(150px, 1.3fr) minmax(120px, .8fr) minmax(270px, 1.5fr) minmax(135px, .8fr) minmax(135px, .8fr) auto; gap: 9px; align-items: end; margin-top: 12px; }.query-grid > label { display: grid; gap: 4px; color: #68665d; font-size: 11px; }.query-grid input:not([type=radio]), .query-grid select { box-sizing: border-box; width: 100%; height: 31px; padding: 4px 7px; border: 1px solid #d8d5ca; border-radius: 3px; font: inherit; }.query-grid select[multiple] { height: auto; min-height: 78px; padding: 3px; }.type-filter { display: flex; gap: 9px; min-height: 31px; align-items: center; margin: 0; padding: 2px 8px; border: 1px solid #e0ddd3; border-radius: 3px; color: #68665d; }.type-filter legend, .type-filter label { font-size: 10px; white-space: nowrap; }.summary-row { display: grid; grid-template-columns: repeat(5, minmax(100px, 1fr)); gap: 8px; }.summary-row > div { display: flex; flex-direction: column; align-items: flex-start; justify-content: center; padding: 8px 11px; color: #777469; font-size: 11px; }.summary-row strong { color: #34332c; font-size: 16px; }.summary-row small { margin-top: 2px; color: #89867a; font-size: 9px; }
.data-panel { overflow: hidden; }.panel-heading { min-height: 46px; padding: 9px 12px; border-bottom: 1px solid #ebe9e2; }.panel-heading h2 small { margin-left: 6px; color: #858277; font-size: 11px; font-weight: 400; }.read-only-badge { padding: 3px 8px; border-radius: 12px; background: #eef3ef; color: #496451; font-size: 10px; }.table-scroll { max-height: 260px; overflow: auto; }table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 11px; }th { position: sticky; top: 0; z-index: 1; background: #f1f0eb; color: #69675e; text-align: left; font-weight: 600; }th, td { padding: 7px 8px; border-bottom: 1px solid #efeee9; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f7f8f3; }tr.active { background: #edf3ed; }td strong { font-weight: 600; }td small { display: block; margin-top: 2px; color: #89867a; font-size: 10px; }.empty { padding: 18px; color: #89867a; text-align: center; cursor: default; }
.detail-panel { flex: 1; min-height: 220px; }.detail-heading { align-items: flex-start; }.status-filters { display: flex; flex-wrap: wrap; gap: 7px; justify-content: flex-end; }.status-filters label { display: inline-flex; align-items: center; gap: 3px; color: #6c6a61; font-size: 10px; white-space: nowrap; }.detail-actions { padding: 8px 12px; border-bottom: 1px solid #ebe9e2; }.detail-actions > span { color: #777469; font-size: 11px; }.detail-actions > div { display: flex; flex-wrap: wrap; gap: 6px; }.details-table .focused { outline: 1px solid #a5bba9; outline-offset: -1px; }.details-table .selected { background: #eef3ed; }.status-pill { display: inline-block; min-width: 48px; padding: 2px 6px; border-radius: 10px; text-align: center; }.status-0 { background: #faf0da; color: #8a651e; }.status-1 { background: #e9f1e9; color: #496451; }.status-2 { background: #e7eef6; color: #42637f; }.status-3 { background: #ece9f4; color: #69577f; }.status-4 { background: #f4e8e7; color: #8a5550; }.source-note { margin: 0; padding: 7px 12px; color: #7b786e; font-size: 10px; background: #faf9f6; }
.toast { position: sticky; bottom: 4px; align-self: flex-end; max-width: min(560px, 100%); margin: 0; padding: 8px 12px; border: 1px solid #d6dfd5; border-radius: 5px; background: #f0f5ef; color: #47614a; box-shadow: 0 3px 10px #00000010; }.toast.error { border-color: #ead0cc; background: #fff2ef; color: #904d42; }.page-footer { display: flex; justify-content: space-between; gap: 12px; color: #89867a; font-size: 10px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 18px; background: #24231f88; }.dialog { width: min(760px, 100%); max-height: 85vh; overflow: auto; padding: 16px; border-radius: 7px; background: #fff; box-shadow: 0 16px 55px #0004; }.dialog > header { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }.dialog h2 { margin: 4px 0 10px; font-size: 18px; }.dialog p { color: #69675e; }.sop-steps { display: grid; gap: 8px; }.sop-steps article { display: grid; gap: 3px; padding: 9px; border: 1px solid #e7e4db; border-radius: 4px; }.sop-steps small { color: #777469; white-space: normal; }
@media (max-width: 980px) { .query-grid { grid-template-columns: repeat(3, minmax(120px, 1fr)); }.summary-row { grid-template-columns: repeat(3, minmax(100px, 1fr)); }.detail-heading { flex-direction: column; }.status-filters { justify-content: flex-start; } }
@media (max-width: 650px) { .pm-release-page { padding: 10px; }.query-grid { grid-template-columns: 1fr 1fr; }.summary-row { grid-template-columns: 1fr 1fr; }.detail-actions { align-items: flex-start; flex-direction: column; }.page-footer { flex-direction: column; } }
</style>

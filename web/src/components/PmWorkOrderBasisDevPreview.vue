<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type WoType = 0 | 1
type WoStatus = 0 | 1 | 2 | 3 | 4
interface WorkOrder { woNo: string; woType: WoType; issueState: IssueState; description: string; creator: string; createDate: string }
interface WorkOrderDetail {
  woNo: string; woItemNo: number; equipmentNo: string; pmSerialNo: number | null; pmeqpSopNo: string
  pmSopNo: string; frequencyNo: string; pmEstDateTime: string; woStatus: WoStatus
  releaseDate: string; finishedDate: string; executor: string; executeDesc: string
  creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number
}
interface DevState { version: 1; orders: WorkOrder[]; details: WorkOrderDetail[] }
interface PlanBridge { version: 1; records: unknown[]; plans: Array<Record<string, unknown>>; planLogs: Array<Record<string, unknown>> }
interface PlanChoice { serialNo: string; pmSerialNo: number; equipmentNo: string; pmeqpSopNo: string; pmSopNo: string; frequencyNo: string; pmEstDateTime: string; creator: string; createDate: string }
interface SopStep { pmItemNo: string; serialNo: number }
interface SopRecord { sopNo: string; sopName: string; description: string; estDoneTime: number; steps: SopStep[] }
interface PmItem { pmItemNo: string; pmItemName: string; tools?: Array<{ toolNo: string; toolQty: number }>; substitutions?: Array<{ substitutionNo: string; substitutionQty: number; unitNo: string }> }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.work-orders.v1'
const planStorageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const currentUser = 'DEV01'
const issueOptions: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
const woStatusLabels: Record<WoStatus, string> = { 0: '待簽核', 1: '待料', 2: '已下線', 3: '已完成', 4: '已撤線' }
const equipment = [
  { equipmentNo: 'CNC-02', equipmentName: 'CNC 加工中心 02', equipmentType: 'CNC-200' },
  { equipmentNo: 'CNC-03', equipmentName: 'CNC 加工中心 03', equipmentType: 'CNC-200' },
  { equipmentNo: 'PUMP-07', equipmentName: '製程冷卻泵 07', equipmentType: 'PUMP-120' },
  { equipmentNo: 'PUMP-08', equipmentName: '製程冷卻泵 08', equipmentType: 'PUMP-120' },
  { equipmentNo: 'OVEN-05', equipmentName: '真空烘箱 05', equipmentType: 'OVEN-50' },
]
const fallbackSops: SopRecord[] = [
  { sopNo: 'SOP-CLEAN-01', sopName: '腔體定期清潔程序', description: '清潔腔體與載台，完成後確認無殘留物。', estDoneTime: 45, steps: [{ pmItemNo: 'PM-CLEAN-01', serialNo: 1 }, { pmItemNo: 'PM-LUBE-03', serialNo: 2 }] },
  { sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', description: '以標準器確認輸出值並記錄結果。', estDoneTime: 30, steps: [{ pmItemNo: 'PM-CAL-02', serialNo: 1 }] },
  { sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', description: '依週期補充規定油脂並記錄用量。', estDoneTime: 25, steps: [{ pmItemNo: 'PM-LUBE-03', serialNo: 1 }] },
]
function nowText() { return new Date().toLocaleString('sv-SE') }
function dateOnly(value: string) { return value.slice(0, 10) }
function seedState(): DevState {
  return {
    version: 1,
    orders: [
      { woNo: 'EW26110012', woType: 1, issueState: 0, description: 'CNC-02 感測器異常後追加檢查', creator: 'PMUSER', createDate: '2026-11-24 16:10:00' },
      { woNo: 'EW26110010', woType: 1, issueState: 1, description: 'PUMP-07 振動檢查', creator: 'DEV02', createDate: '2026-11-22 09:20:00' },
      { woNo: 'EW26110008', woType: 1, issueState: 2, description: 'OVEN-05 溫控器校正', creator: 'PMUSER', createDate: '2026-11-19 10:30:00' },
      { woNo: 'EW26110003', woType: 1, issueState: -1, description: '已取消的臨時保養需求', creator: 'PMUSER', createDate: '2026-11-10 11:15:00' },
    ],
    details: [
      { woNo: 'EW26110012', woItemNo: 1, equipmentNo: 'CNC-02', pmSerialNo: null, pmeqpSopNo: '', pmSopNo: 'SOP-CAL-02', frequencyNo: '', pmEstDateTime: '2026-11-28', woStatus: 0, releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: 'PMUSER', createDate: '2026-11-24 16:10:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { woNo: 'EW26110010', woItemNo: 1, equipmentNo: 'PUMP-07', pmSerialNo: null, pmeqpSopNo: '', pmSopNo: 'SOP-LUBE-03', frequencyNo: '', pmEstDateTime: '2026-11-25', woStatus: 0, releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: 'DEV02', createDate: '2026-11-22 09:20:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { woNo: 'EW26110008', woItemNo: 1, equipmentNo: 'OVEN-05', pmSerialNo: null, pmeqpSopNo: '', pmSopNo: 'SOP-CAL-02', frequencyNo: '', pmEstDateTime: '2026-11-20', woStatus: 1, releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: 'PMUSER', createDate: '2026-11-19 10:30:00', reviser: '', reviseDate: '', dataStamp: 0 },
      { woNo: 'EW26110003', woItemNo: 1, equipmentNo: 'CNC-03', pmSerialNo: null, pmeqpSopNo: '', pmSopNo: 'SOP-CLEAN-01', frequencyNo: '', pmEstDateTime: '2026-11-12', woStatus: 0, releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: 'PMUSER', createDate: '2026-11-10 11:15:00', reviser: '', reviseDate: '', dataStamp: 0 },
    ],
  }
}
function loadState(): DevState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (parsed?.version === 1 && Array.isArray(parsed.orders) && Array.isArray(parsed.details)) return parsed
  } catch { /* Keep DEV interactions in memory when browser storage is unavailable. */ }
  return seedState()
}
function readSops(): SopRecord[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem('ymes.dev.pm.sop-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(parsed?.records)) {
      const rows = parsed.records.map((row) => ({
        sopNo: String(row.sopNo ?? ''), sopName: String(row.sopName ?? ''), description: String(row.description ?? ''),
        estDoneTime: Number(row.estDoneTime ?? 0), steps: Array.isArray(row.steps) ? row.steps.map((step) => ({ pmItemNo: String(step.pmItemNo ?? ''), serialNo: Number(step.serialNo ?? 0) })) : [],
      })).filter((row) => row.sopNo)
      if (rows.length) return rows
    }
  } catch { /* Use DEV SOP fixtures. */ }
  return fallbackSops
}
function readPmItems(): PmItem[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem('ymes.dev.pm.maintenance-item.v1') ?? 'null') as { records?: PmItem[] } | null
    if (Array.isArray(parsed?.records)) return parsed.records
  } catch { /* Use step numbers when PM108 preview data is unavailable. */ }
  return []
}
function seedPlanBridge(): PlanBridge {
  return {
    version: 1,
    records: [
      { pmeqpSopNo: 'EQPSOP2600001', pmYearNo: '2026', equipmentType: 'CNC-200', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { pmeqpSopNo: 'EQPSOP2600002', pmYearNo: '2026', equipmentType: 'PUMP-120', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', creator: 'DEV02', createDate: '2026-02-10 11:15' },
    ],
    plans: [
      { serialNo: 'DEV-PLAN-0002', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 2, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', sopNo: 'SOP-CAL-02', estimatedDate: '2026-12-15', executeState: 0, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { serialNo: 'DEV-PLAN-0003', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 1, equipmentNo: 'PUMP-07', frequencyNo: 'PM-WEEKLY-01', sopNo: 'SOP-LUBE-03', estimatedDate: '2026-11-30', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15' },
    ],
    planLogs: [],
  }
}
function readPlanBridge(): PlanBridge {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(planStorageKey) ?? 'null') as PlanBridge | null
    if (parsed?.version === 1 && Array.isArray(parsed.records) && Array.isArray(parsed.plans) && Array.isArray(parsed.planLogs)) return parsed
  } catch { /* Use DEV plan fixtures. */ }
  return seedPlanBridge()
}

const state = ref<DevState>(loadState())
const sopOptions = ref(readSops())
const pmItems = ref(readPmItems())
const issueFilter = ref<IssueState>(0)
const searchText = ref('')
const selectedWoNo = ref('')
const selectedOrderNos = ref<string[]>([])
const selectedItemNos = ref<number[]>([])
const createOpen = ref(false)
const candidatePlans = ref<PlanChoice[]>([])
const queuedPlans = ref<PlanChoice[]>([])
const selectedPlanSerials = ref<string[]>([])
const equipmentFilters = ref<string[]>([])
const sopFilters = ref<string[]>([])
const fromDate = ref('')
const toDate = ref('')
const queryEquipmentNo = ref('')
const querySopNo = ref('')
const description = ref('')
const editOpen = ref(false)
const editDraft = reactive({ pmSopNo: '', pmEstDateTime: '' })
const sopDialogOpen = ref(false)
const sopDialogNo = ref('')
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const currentOrder = computed(() => state.value.orders.find((row) => row.woNo === selectedWoNo.value) ?? null)
const visibleOrders = computed(() => state.value.orders.filter((row) => row.issueState === issueFilter.value && (!searchText.value.trim() || row.woNo.toLowerCase().includes(searchText.value.trim().toLowerCase()))).sort((a, b) => b.woNo.localeCompare(a.woNo)))
const detailRows = computed(() => state.value.details.filter((row) => row.woNo === currentOrder.value?.woNo).sort((a, b) => a.woItemNo - b.woItemNo))
const selectedDetails = computed(() => detailRows.value.filter((row) => selectedItemNos.value.includes(row.woItemNo)))
const statusLabel = computed(() => issueOptions.find((row) => row.value === issueFilter.value)?.label ?? '')
const selectedOrderNosForAction = computed(() => selectedOrderNos.value.length ? selectedOrderNos.value : (currentOrder.value ? [currentOrder.value.woNo] : []))
const filteredPlans = computed(() => candidatePlans.value.filter((row) =>
  (!equipmentFilters.value.length || equipmentFilters.value.includes(row.equipmentNo)) &&
  (!sopFilters.value.length || sopFilters.value.includes(row.pmSopNo)) &&
  (!fromDate.value || row.pmEstDateTime >= fromDate.value) && (!toDate.value || row.pmEstDateTime <= toDate.value),
))
const sopDialog = computed(() => sopOptions.value.find((row) => row.sopNo === sopDialogNo.value) ?? null)
const sopSteps = computed(() => (sopDialog.value?.steps ?? []).map((step) => {
  const item = pmItems.value.find((row) => row.pmItemNo === step.pmItemNo)
  return { ...step, itemName: item?.pmItemName ?? step.pmItemNo, tools: item?.tools ?? [], substitutions: item?.substitutions ?? [] }
}))
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV changes in memory. */ } }, { deep: true })
watch(visibleOrders, (rows) => {
  if (!rows.some((row) => row.woNo === selectedWoNo.value)) {
    selectedWoNo.value = rows[0]?.woNo ?? ''
    selectedItemNos.value = []
  }
}, { immediate: true })
watch(currentOrder, (row) => { if (row) selectedItemNos.value = [] })

function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function equipmentName(no: string) { return equipment.find((row) => row.equipmentNo === no)?.equipmentName ?? no }
function issueName(value: IssueState) { return issueOptions.find((row) => row.value === value)?.label ?? `狀態 ${value}` }
function woTypeName(value: WoType) { return value === 0 ? 'PM Work Order' : 'Required Work Order' }
function selectOrder(row: WorkOrder) { selectedWoNo.value = row.woNo; selectedItemNos.value = [] }
function toggleOrderSelection(woNo: string, checked: boolean) {
  selectedOrderNos.value = checked ? Array.from(new Set([...selectedOrderNos.value, woNo])) : selectedOrderNos.value.filter((no) => no !== woNo)
}
function toggleItemSelection(itemNo: number, checked: boolean) {
  selectedItemNos.value = checked ? Array.from(new Set([...selectedItemNos.value, itemNo])) : selectedItemNos.value.filter((no) => no !== itemNo)
}
function selectIssueState(value: IssueState) { issueFilter.value = value; selectedOrderNos.value = []; selectedItemNos.value = [] }
function openCreate() {
  candidatePlans.value = []
  queuedPlans.value = []
  selectedPlanSerials.value = []
  equipmentFilters.value = []
  sopFilters.value = []
  fromDate.value = ''
  toDate.value = ''
  queryEquipmentNo.value = ''
  querySopNo.value = ''
  description.value = ''
  createOpen.value = true
}
function queryPlans() {
  if (fromDate.value && toDate.value && fromDate.value > toDate.value) return toast('預計日期起日不可晚於迄日。', 'error')
  const bridge = readPlanBridge()
  candidatePlans.value = bridge.plans.filter((row) => Number(row.executeState ?? 0) === 0).map((row) => ({
    serialNo: String(row.serialNo ?? ''), pmSerialNo: Number(row.pmSerialNo ?? 0), equipmentNo: String(row.equipmentNo ?? ''),
    pmeqpSopNo: String(row.pmeqpSopNo ?? ''), pmSopNo: String(row.sopNo ?? row.pmSopNo ?? ''), frequencyNo: String(row.frequencyNo ?? ''),
    pmEstDateTime: dateOnly(String(row.estimatedDate ?? row.pmEstDateTime ?? '')), creator: String(row.creator ?? ''), createDate: String(row.createDate ?? ''),
  })).filter((row) => row.serialNo && !queuedPlans.value.some((queued) => queued.serialNo === row.serialNo))
  selectedPlanSerials.value = []
  toast(`已載入 ${candidatePlans.value.length} 筆尚未開立工單的 DEV 保養計畫。`)
}
function addSelectedPlans() {
  const selected = filteredPlans.value.filter((row) => selectedPlanSerials.value.includes(row.serialNo))
  for (const row of selected) {
    queuedPlans.value.push(row)
    candidatePlans.value = candidatePlans.value.filter((item) => item.serialNo !== row.serialNo)
  }
  selectedPlanSerials.value = []
}
function removeQueuedPlan(serialNo: string) {
  const [row] = queuedPlans.value.filter((item) => item.serialNo === serialNo)
  if (!row) return
  queuedPlans.value = queuedPlans.value.filter((item) => item.serialNo !== serialNo)
  candidatePlans.value = [row, ...candidatePlans.value.filter((item) => item.serialNo !== serialNo)]
}
function createWoNo(): string {
  const date = new Date()
  const prefix = `EW${String(date.getFullYear()).slice(-2)}${String(date.getMonth() + 1).padStart(2, '0')}`
  const serials = state.value.orders.filter((row) => row.woNo.startsWith(prefix)).map((row) => Number(row.woNo.slice(6))).filter(Number.isFinite)
  const next = Math.max(0, ...serials) >= 9999 ? 1 : Math.max(0, ...serials) + 1
  return `${prefix}${String(next).padStart(4, '0')}`
}
function confirmCreate() {
  if (!queuedPlans.value.length) return toast('請至少加入一筆保養計畫明細。', 'error')
  const now = nowText()
  const woNo = createWoNo()
  const order: WorkOrder = { woNo, woType: 0, issueState: 2, description: description.value.trim(), creator: currentUser, createDate: now }
  const details = queuedPlans.value.map((row, index): WorkOrderDetail => ({
    woNo, woItemNo: index + 1, equipmentNo: row.equipmentNo, pmSerialNo: row.pmSerialNo, pmeqpSopNo: row.pmeqpSopNo,
    pmSopNo: row.pmSopNo, frequencyNo: row.frequencyNo, pmEstDateTime: dateOnly(row.pmEstDateTime), woStatus: 1,
    releaseDate: '', finishedDate: '', executor: '', executeDesc: '', creator: currentUser, createDate: now, reviser: '', reviseDate: '', dataStamp: 0,
  }))
  try {
    const bridge = readPlanBridge()
    const selected = new Set(queuedPlans.value.map((row) => row.serialNo))
    const available = new Set(bridge.plans.filter((row) => Number(row.executeState ?? 0) === 0).map((row) => String(row.serialNo ?? '')))
    if (Array.from(selected).some((serialNo) => !available.has(serialNo))) return toast('計畫清單已變更，請重新查詢後再建立工單。', 'error')
    const moved = bridge.plans.filter((row) => selected.has(String(row.serialNo ?? '')))
    bridge.planLogs.push(...moved.map((row, index) => ({ ...row, executeState: 2, pmWoNo: woNo, pmWoItemNo: index + 1, woCreateDate: dateOnly(now), logCreator: currentUser, logCreateDate: now })))
    bridge.plans = bridge.plans.filter((row) => !selected.has(String(row.serialNo ?? '')))
    window.localStorage.setItem(planStorageKey, JSON.stringify(bridge))
  } catch {
    return toast('DEV 保養計畫無法保存，工單未建立；請稍後重試。', 'error')
  }
  state.value.orders.unshift(order)
  state.value.details.push(...details)
  issueFilter.value = 2
  selectedWoNo.value = woNo
  selectedOrderNos.value = []
  selectedItemNos.value = []
  createOpen.value = false
  toast(`已建立 DEV 保養工單 ${woNo}；選取計畫已轉入保養計畫歷史。`)
}
function approveOrders() {
  const targets = selectedOrderNosForAction.value
  if (!targets.length) return toast('請先選取未凍結工單。', 'error')
  let changed = 0
  for (const woNo of targets) {
    const row = state.value.orders.find((item) => item.woNo === woNo)
    if (!row || row.issueState !== 0) break
    row.issueState = 2
    for (const detail of state.value.details.filter((item) => item.woNo === woNo)) detail.woStatus = 1
    changed++
  }
  if (changed) toast(`已將 ${changed} 筆未凍結工單設為已生效（DEV 略過 Issue Management 簽核流程）。`)
  else toast('選取項目沒有可核准的未凍結工單。', 'error')
}
function deleteOrders() {
  const targets = selectedOrderNosForAction.value
  if (!targets.length) return toast('請先選取工單。', 'error')
  if (!window.confirm(`確定刪除／下線 ${targets.length} 筆工單？`)) return
  let changed = 0
  for (const woNo of targets) {
    const row = state.value.orders.find((item) => item.woNo === woNo)
    if (!row) continue
    if (row.issueState === 1) { toast(`工單 ${woNo} 正在簽核中，不可刪除；之前已完成的列不會回復。`, 'error'); break }
    if (row.issueState === 2) {
      const lines = state.value.details.filter((item) => item.woNo === woNo)
      if (lines.some((item) => item.woStatus !== 1)) { toast(`工單 ${woNo} 已有明細不是待料狀態，不能下線。`, 'error'); break }
      row.issueState = -1
      for (const detail of lines) detail.woStatus = 4
      changed++
    } else {
      state.value.orders = state.value.orders.filter((item) => item.woNo !== woNo)
      state.value.details = state.value.details.filter((item) => item.woNo !== woNo)
      changed++
    }
  }
  selectedOrderNos.value = []
  if (changed) toast(`已處理 ${changed} 筆工單；已生效工單改為未使用，其餘可刪狀態為實體刪除。`)
}
function renumberDetails(woNo: string) {
  state.value.details.filter((row) => row.woNo === woNo).sort((a, b) => a.woItemNo - b.woItemNo).forEach((row, index) => { row.woItemNo = index + 1 })
}
function deleteDetails() {
  const order = currentOrder.value
  if (!order || order.issueState !== 0) return toast('只有未凍結工單可以刪除明細。', 'error')
  const targets = selectedItemNos.value.length ? selectedItemNos.value : detailRows.value[0] ? [detailRows.value[0].woItemNo] : []
  if (!targets.length) return toast('請先選取工單明細。', 'error')
  if (!window.confirm(`確定刪除 ${targets.length} 筆工單明細？`)) return
  state.value.details = state.value.details.filter((row) => row.woNo !== order.woNo || !targets.includes(row.woItemNo))
  renumberDetails(order.woNo)
  selectedItemNos.value = []
  toast('工單明細已刪除並重新排序。')
}
function openEditDetail() {
  const order = currentOrder.value
  const row = selectedDetails.value[0] ?? detailRows.value[0]
  if (!order || order.issueState !== 0 || !row) return toast('只有未凍結工單明細可以修改。', 'error')
  selectedItemNos.value = [row.woItemNo]
  Object.assign(editDraft, { pmSopNo: row.pmSopNo, pmEstDateTime: dateOnly(row.pmEstDateTime) })
  editOpen.value = true
}
function saveDetailEdit() {
  const order = currentOrder.value
  const row = selectedDetails.value[0]
  if (!order || order.issueState !== 0 || !row) return
  if (!sopOptions.value.some((item) => item.sopNo === editDraft.pmSopNo)) return toast('請選擇有效的 PM SOP。', 'error')
  if (!/^\d{4}-\d{2}-\d{2}$/.test(editDraft.pmEstDateTime)) return toast('預計日期格式不正確。', 'error')
  row.pmSopNo = editDraft.pmSopNo
  row.pmEstDateTime = editDraft.pmEstDateTime
  row.reviser = currentUser
  row.reviseDate = nowText()
  row.dataStamp++
  editOpen.value = false
  toast(`工單 ${row.woNo} 明細 ${row.woItemNo} 已更新 DEV 假資料。`)
}
function showSopForOrder() {
  const row = detailRows.value[0]
  if (!row) return toast('所選工單沒有明細。', 'error')
  sopDialogNo.value = row.pmSopNo
  sopDialogOpen.value = true
}
function showSopForDetail() {
  const row = selectedDetails.value[0] ?? detailRows.value[0]
  if (!row) return toast('請先選取工單明細。', 'error')
  sopDialogNo.value = row.pmSopNo
  sopDialogOpen.value = true
}
</script>

<template>
  <section class="pm-work-order" aria-label="開立保養工單 DEV 預覽">
    <header class="page-header"><div><p>保養工單／PMWO BASIS</p><h1>開立保養工單</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="toolbar"><label>工單編號 <input v-model="searchText" type="search" placeholder="輸入 WoNo 搜尋"></label><button type="button" @click="openCreate">ADD 保養工單</button><button type="button" class="secondary" :disabled="!selectedOrderNosForAction.length" @click="approveOrders">Approve</button><button type="button" class="danger" :disabled="!selectedOrderNosForAction.length" @click="deleteOrders">DEL／下線</button><button type="button" class="secondary" :disabled="!currentOrder" @click="showSopForOrder">Show SOP Detail</button></div>
    <nav class="issue-tabs" aria-label="工單簽核狀態"><button v-for="item in issueOptions" :key="item.value" type="button" :class="{ active: issueFilter === item.value }" @click="selectIssueState(item.value)">{{ item.label }}<span>{{ state.orders.filter((row) => row.issueState === item.value).length }}</span></button></nav>
    <div class="work-panels">
      <section class="data-panel master-panel"><header><div><h2>PM Work Order</h2><span>{{ statusLabel }}｜{{ visibleOrders.length }} 筆</span></div></header><div class="table-wrap"><table><thead><tr><th class="select-col">選取</th><th>WoNo</th><th>WO Type</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody><tr v-for="row in visibleOrders" :key="row.woNo" :class="{ active: row.woNo === selectedWoNo }" @click="selectOrder(row)"><td @click.stop><input type="checkbox" :checked="selectedOrderNos.includes(row.woNo)" :aria-label="`選取 ${row.woNo}`" @change="toggleOrderSelection(row.woNo, ($event.target as HTMLInputElement).checked)"></td><td><strong>{{ row.woNo }}</strong><small>{{ issueName(row.issueState) }}</small></td><td>{{ woTypeName(row.woType) }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td></tr><tr v-if="visibleOrders.length === 0"><td colspan="6" class="empty">此簽核狀態沒有工單；使用 ADD 從未開立的保養計畫建立 PM Work Order。</td></tr></tbody></table></div></section>
      <section class="data-panel detail-panel"><header><div><h2>工單明細 <small>{{ currentOrder?.woNo ?? '請選工單' }}</small></h2><span>{{ detailRows.length }} 筆明細</span></div><div class="row-actions"><button type="button" class="secondary" :disabled="!currentOrder || currentOrder.issueState !== 0 || !detailRows.length" @click="openEditDetail">EDIT</button><button type="button" class="danger" :disabled="!currentOrder || currentOrder.issueState !== 0 || !detailRows.length" @click="deleteDetails">DEL</button><button type="button" class="secondary" :disabled="!detailRows.length" @click="showSopForDetail">Show Detail</button></div></header><div class="table-wrap"><table><thead><tr><th class="select-col">選取</th><th>Item</th><th>EquipmentNo</th><th>PMSOPNo</th><th>PMEstDateTime</th><th>WOStatus</th><th>ReleaseDate</th><th>FinishedDate</th><th>Executor</th><th>ExecuteDesc</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody><tr v-for="row in detailRows" :key="row.woItemNo" :class="{ active: selectedItemNos.includes(row.woItemNo) }" @click="toggleItemSelection(row.woItemNo, !selectedItemNos.includes(row.woItemNo))"><td @click.stop><input type="checkbox" :checked="selectedItemNos.includes(row.woItemNo)" :aria-label="`選取明細 ${row.woItemNo}`" @change="toggleItemSelection(row.woItemNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.woItemNo }}</td><td><strong>{{ row.equipmentNo }}</strong><small>{{ equipmentName(row.equipmentNo) }}</small></td><td>{{ row.pmSopNo }}</td><td>{{ dateOnly(row.pmEstDateTime) }}</td><td><span class="status-pill" :class="`status-${row.woStatus}`">{{ woStatusLabels[row.woStatus] }}</span></td><td>{{ row.releaseDate || '—' }}</td><td>{{ row.finishedDate || '—' }}</td><td>{{ row.executor || '—' }}</td><td>{{ row.executeDesc || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td></tr><tr v-if="detailRows.length === 0"><td colspan="14" class="empty">請選取工單主檔以載入明細。</td></tr></tbody></table></div></section>
    </div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>PM Work Order 建立後為已生效／明細待料；Required Work Order 原始分頁在此版 WinForms 隱藏。</span></footer>

    <div v-if="createOpen" class="modal-backdrop" @click.self="createOpen = false"><section class="dialog create-dialog" role="dialog" aria-modal="true" aria-labelledby="create-title"><header><div><p>PM WORK ORDER / ADD</p><h2 id="create-title">由設備保養計畫開立工單</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="createOpen = false">×</button></header><p class="form-note">此 WinForms 版本的 Required Work Order 分頁設定為隱藏；本頁只模擬可見的 PM Work Order 流程。確認後工單直接生效、明細為待料，所選計畫轉入 ExecuteState=2 歷史。</p><div class="plan-filters"><label>Equipment No<select v-model="equipmentFilters" multiple size="4"><option v-for="row in equipment" :key="row.equipmentNo" :value="row.equipmentNo">{{ row.equipmentNo }}｜{{ row.equipmentName }}</option></select></label><label>PM SOP<select v-model="sopFilters" multiple size="4"><option v-for="row in sopOptions" :key="row.sopNo" :value="row.sopNo">{{ row.sopNo }}｜{{ row.sopName }}</option></select></label><label>預計日期起<input v-model="fromDate" type="date"></label><label>預計日期迄<input v-model="toDate" type="date"></label><button type="button" @click="queryPlans">Query 未開立計畫</button></div><div class="plan-grid"><div class="data-panel"><header><h3>可選保養計畫 <small>{{ filteredPlans.length }} 筆</small></h3><button type="button" class="secondary" :disabled="!selectedPlanSerials.length" @click="addSelectedPlans">加入明細 →</button></header><div class="table-wrap"><table><thead><tr><th></th><th>EquipmentNo</th><th>FrequencyNo</th><th>PMSOPNo</th><th>PMEstDateTime</th><th>PM Serial</th></tr></thead><tbody><tr v-for="row in filteredPlans" :key="row.serialNo"><td><input v-model="selectedPlanSerials" type="checkbox" :value="row.serialNo" :aria-label="`選取計畫 ${row.serialNo}`"></td><td>{{ row.equipmentNo }}<small>{{ equipmentName(row.equipmentNo) }}</small></td><td>{{ row.frequencyNo }}</td><td>{{ row.pmSopNo }}</td><td>{{ row.pmEstDateTime }}</td><td>{{ row.pmSerialNo }}</td></tr><tr v-if="filteredPlans.length === 0"><td colspan="6" class="empty">沒有符合條件的未開立計畫。先設定篩選條件並執行 Query。</td></tr></tbody></table></div></div><div class="data-panel"><header><h3>工單明細草稿 <small>{{ queuedPlans.length }} 筆</small></h3></header><div class="table-wrap"><table><thead><tr><th>Item</th><th>EquipmentNo</th><th>PMSOPNo</th><th>PMEstDateTime</th><th></th></tr></thead><tbody><tr v-for="(row, index) in queuedPlans" :key="row.serialNo"><td>{{ index + 1 }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.pmSopNo }}</td><td>{{ row.pmEstDateTime }}</td><td><button type="button" class="link-button" @click="removeQueuedPlan(row.serialNo)">移除</button></td></tr><tr v-if="queuedPlans.length === 0"><td colspan="5" class="empty">將上方計畫加入工單。</td></tr></tbody></table></div></div></div><label class="description-label">Description<textarea v-model="description" rows="2" maxlength="500"></textarea></label><footer><span class="footer-gap"></span><button type="button" class="secondary" @click="createOpen = false">取消</button><button type="button" @click="confirmCreate">Confirm 開立</button></footer></section></div>

    <div v-if="editOpen && selectedDetails[0]" class="modal-backdrop" @click.self="editOpen = false"><section class="dialog small-dialog" role="dialog" aria-modal="true" aria-labelledby="edit-title"><header><div><p>PM WO DETAIL / EDIT</p><h2 id="edit-title">修改工單明細</h2></div><button type="button" class="secondary close-x" @click="editOpen = false">×</button></header><p class="form-note">{{ currentOrder?.woNo }} / Item {{ selectedDetails[0].woItemNo }} / {{ selectedDetails[0].equipmentNo }}</p><label>PM SOP<select v-model="editDraft.pmSopNo"><option v-for="row in sopOptions" :key="row.sopNo" :value="row.sopNo">{{ row.sopNo }}｜{{ row.sopName }}</option></select></label><label>預計保養日期<input v-model="editDraft.pmEstDateTime" type="date"></label><footer><span class="footer-gap"></span><button type="button" class="secondary" @click="editOpen = false">取消</button><button type="button" @click="saveDetailEdit">儲存</button></footer></section></div>

    <div v-if="sopDialogOpen && sopDialog" class="modal-backdrop" @click.self="sopDialogOpen = false"><section class="dialog sop-dialog" role="dialog" aria-modal="true" aria-labelledby="sop-title"><header><div><p>SOP BASIS / {{ sopDialog.sopNo }}</p><h2 id="sop-title">{{ sopDialog.sopName }}</h2></div><button type="button" class="secondary close-x" @click="sopDialogOpen = false">×</button></header><p>{{ sopDialog.description || '此 SOP 沒有 DEV 說明文字。' }}</p><p class="form-note">預估工時 {{ sopDialog.estDoneTime || '—' }} 分鐘｜以下步驟來自 PM109／PM108 DEV 假資料。</p><div class="table-wrap"><table><thead><tr><th>Serial</th><th>PM Item</th><th>工具</th><th>備品</th></tr></thead><tbody><tr v-for="step in sopSteps" :key="`${step.serialNo}-${step.pmItemNo}`"><td>{{ step.serialNo }}</td><td>{{ step.pmItemNo }}｜{{ step.itemName }}</td><td>{{ step.tools.map((row) => `${row.toolNo} × ${row.toolQty}`).join('、') || '—' }}</td><td>{{ step.substitutions.map((row) => `${row.substitutionNo} × ${row.substitutionQty} ${row.unitNo}`).join('、') || '—' }}</td></tr><tr v-if="sopSteps.length === 0"><td colspan="4" class="empty">此 SOP 尚無步驟假資料。</td></tr></tbody></table></div><footer><span class="footer-gap"></span><button type="button" @click="sopDialogOpen = false">關閉</button></footer></section></div>
  </section>
</template>

<style scoped>
.pm-work-order { height:100%; min-height:0; display:flex; flex-direction:column; gap:8px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.toolbar,.issue-tabs,.data-panel>header,.row-actions,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:9px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }button.danger { border-color:#a34a38; background:#a34a38; }.toolbar { gap:7px; flex-wrap:wrap; padding:8px; border:1px solid #d0ccbf; background:#eeece4; }.toolbar label { display:flex; align-items:center; gap:7px; margin-right:auto; color:#514f45; }.toolbar input { width:190px; min-height:28px; padding:4px 7px; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; font:inherit; }
.issue-tabs { gap:5px; flex-wrap:wrap; }.issue-tabs button { display:flex; align-items:center; gap:7px; border-color:#c4c0b4; background:#eeece4; color:#49473e; }.issue-tabs button.active { border-color:#555144; background:#555144; color:#fff; }.issue-tabs span { min-width:18px; padding:0 4px; border-radius:10px; background:#ffffff33; text-align:center; font-size:10px; }.work-panels { min-height:0; flex:1; display:grid; grid-template-rows:minmax(180px,0.9fr) minmax(220px,1.1fr); gap:8px; }.data-panel { min-height:0; display:flex; flex-direction:column; gap:6px; padding:8px; border:1px solid #d0ccbf; background:#fffef9; }.data-panel>header { justify-content:space-between; gap:9px; }.data-panel h2,.data-panel h3 { margin:0; font-size:14px; }.data-panel h2 small,.data-panel h3 small { margin-left:6px; color:#8a877c; font-size:10px; font-weight:400; }.data-panel>header span { display:block; margin-top:2px; color:#777366; font-size:10px; }.row-actions { gap:5px; }.table-wrap { min-height:0; flex:1; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:11px; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; text-align:left; }th,td { padding:5px 7px; border-bottom:1px solid #e5e2d9; }tbody tr { cursor:pointer; }tbody tr:hover { background:#f7f4eb; }tbody tr.active { background:#e9eee9; }td strong,td small { display:block; }td small { color:#888478; font-size:10px; }.select-col { width:38px; }.description-cell { min-width:180px; white-space:normal; }.empty { padding:18px; color:#888478; text-align:center; }.status-pill { padding:2px 6px; border-radius:10px; background:#eeece4; color:#555246; }.status-0 { background:#f4e5df; color:#8a3527; }.status-1 { background:#e5eee5; color:#28533a; }.status-2 { background:#e3e9f0; color:#334d6c; }.status-3 { background:#dce9de; color:#28533a; }.status-4 { background:#efede6; color:#777366; }.toast { margin:0; padding:7px 9px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.dialog { width:min(760px,100%); max-height:calc(100vh - 32px); overflow:auto; display:grid; gap:10px; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.dialog h2 { margin:2px 0 0; font-size:17px; }.close-x { min-width:30px; font-size:18px; }.dialog footer { gap:6px; }.footer-gap { flex:1; }.form-note { margin:0; padding:6px 8px; background:#eeece4; color:#777366; font-size:11px; }.plan-filters { display:grid; grid-template-columns:1.2fr 1.2fr 1fr 1fr auto; align-items:end; gap:8px; }.plan-filters label,.small-dialog label,.description-label { display:grid; gap:3px; color:#514f45; }.plan-filters select[multiple] { min-height:95px; padding:3px; border:1px solid #bbb7a9; background:#fffef9; font:inherit; }.plan-filters input,.small-dialog input,.small-dialog select,.description-label textarea { min-height:29px; padding:4px 7px; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.plan-grid { display:grid; grid-template-columns:1.1fr 0.9fr; gap:8px; min-height:220px; }.plan-grid .data-panel>header { min-height:30px; }.plan-grid .data-panel>header button { flex:none; }.link-button { min-height:auto; padding:0; border:0; background:none; color:#775343; text-decoration:underline; }.description-label textarea { resize:vertical; }.small-dialog { width:min(480px,100%); }.sop-dialog { width:min(800px,100%); }.sop-dialog>p { margin:0; }.sop-dialog .table-wrap { max-height:45vh; }
@media(max-width:1000px) { .pm-work-order { padding:7px; }.work-panels { grid-template-rows:minmax(220px,0.9fr) minmax(260px,1.1fr); }.plan-filters { grid-template-columns:1fr 1fr; }.plan-grid { grid-template-columns:1fr; min-height:360px; }.page-footer { align-items:flex-start; flex-direction:column; }.toolbar label { width:100%; }.toolbar input { flex:1; min-width:80px; } }
</style>

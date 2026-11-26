<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

interface PerformanceRow {
  equipmentNo: string
  frequencyNo: string
  sopNo: string
  estimatedPmDate: string
  workOrderNo: string
  itemNo: string
  estimatedHours: number
  actualHours: number
  executor: string
  releaseDate: string
  finishedDate: string
  workOrderType: 0 | 1
}

type FilterKey = 'equipment' | 'workOrder' | 'frequency' | 'executor'
type MoveDirection = 'right' | 'left'
interface DualListState {
  key: FilterKey
  title: string
  availableLabel: string
  selectedLabel: string
  available: string[]
  selected: string[]
  availablePicked: string[]
  selectedPicked: string[]
}

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const chamberManage = false
const chamberEquipmentNos = new Set(['CNC-02-CH1', 'PRESS-04-CH1'])
const equipmentOptions = ['CNC-02', 'CNC-03', 'CNC-02-CH1', 'PUMP-07', 'PUMP-08', 'PRESS-04', 'PRESS-04-CH1', 'ROBOT-01']
const workOrderOptions = ['EW26100018', 'EW26100023', 'EW26110007', 'EW26110012', 'EW26110021', 'EW26110029', 'REQ26100004', 'REQ26100011', 'REQ26110003']
const frequencyOptions = ['D01', 'W01', 'M01', 'Q01']
const executorOptions = ['CHEN01', 'LIN02', 'LIU03', 'WANG04', 'WU05']

const rows: PerformanceRow[] = [
  { equipmentNo: 'CNC-02', frequencyNo: 'D01', sopNo: 'SOP-CNC-DAILY', estimatedPmDate: '2026-10-29', workOrderNo: 'EW26100018', itemNo: '10', estimatedHours: 1.5, actualHours: elapsedHours('2026-10-29T08:00:00', '2026-10-29T09:24:00'), executor: 'CHEN01', releaseDate: '2026-10-29 08:00:00', finishedDate: '2026-10-29 09:24:00', workOrderType: 0 },
  { equipmentNo: 'PUMP-07', frequencyNo: 'W01', sopNo: 'SOP-PUMP-WEEKLY', estimatedPmDate: '2026-11-03', workOrderNo: 'EW26110007', itemNo: '10', estimatedHours: 2, actualHours: elapsedHours('2026-11-03T10:15:00', '2026-11-03T12:48:00'), executor: 'LIN02', releaseDate: '2026-11-03 10:15:00', finishedDate: '2026-11-03 12:48:00', workOrderType: 0 },
  { equipmentNo: 'CNC-03', frequencyNo: 'D01', sopNo: 'SOP-CNC-DAILY', estimatedPmDate: '2026-11-05', workOrderNo: 'EW26110012', itemNo: '10', estimatedHours: 1.5, actualHours: elapsedHours('2026-11-05T07:45:00', '2026-11-05T08:57:00'), executor: 'CHEN01', releaseDate: '2026-11-05 07:45:00', finishedDate: '2026-11-05 08:57:00', workOrderType: 0 },
  { equipmentNo: 'PRESS-04', frequencyNo: 'M01', sopNo: 'SOP-PRESS-MONTHLY', estimatedPmDate: '2026-11-07', workOrderNo: 'EW26110021', itemNo: '20', estimatedHours: 3, actualHours: elapsedHours('2026-11-07T13:00:00', '2026-11-07T16:16:00'), executor: 'WANG04', releaseDate: '2026-11-07 13:00:00', finishedDate: '2026-11-07 16:16:00', workOrderType: 0 },
  { equipmentNo: 'ROBOT-01', frequencyNo: 'Q01', sopNo: 'SOP-ROBOT-QUARTER', estimatedPmDate: '2026-11-09', workOrderNo: 'EW26110029', itemNo: '10', estimatedHours: 4.5, actualHours: elapsedHours('2026-11-09T08:30:00', '2026-11-09T12:42:00'), executor: 'WU05', releaseDate: '2026-11-09 08:30:00', finishedDate: '2026-11-09 12:42:00', workOrderType: 0 },
  { equipmentNo: 'CNC-02', frequencyNo: 'D01', sopNo: 'SOP-CNC-DAILY', estimatedPmDate: '2026-11-12', workOrderNo: 'EW26110012', itemNo: '20', estimatedHours: 1, actualHours: elapsedHours('2026-11-12T09:10:00', '2026-11-12T10:46:00'), executor: 'LIU03', releaseDate: '2026-11-12 09:10:00', finishedDate: '2026-11-12 10:46:00', workOrderType: 0 },
  { equipmentNo: 'PUMP-08', frequencyNo: 'W01', sopNo: 'SOP-PUMP-WEEKLY', estimatedPmDate: '2026-11-15', workOrderNo: 'EW26110021', itemNo: '10', estimatedHours: 2, actualHours: elapsedHours('2026-11-15T14:20:00', '2026-11-15T16:02:00'), executor: 'LIN02', releaseDate: '2026-11-15 14:20:00', finishedDate: '2026-11-15 16:02:00', workOrderType: 0 },
  { equipmentNo: 'CNC-02-CH1', frequencyNo: 'D01', sopNo: 'SOP-CNC-CHAMBER', estimatedPmDate: '2026-11-17', workOrderNo: 'EW26110029', itemNo: '20', estimatedHours: 1, actualHours: elapsedHours('2026-11-17T11:00:00', '2026-11-17T11:52:00'), executor: 'CHEN01', releaseDate: '2026-11-17 11:00:00', finishedDate: '2026-11-17 11:52:00', workOrderType: 0 },
  { equipmentNo: 'PRESS-04-CH1', frequencyNo: 'M01', sopNo: 'SOP-PRESS-CHAMBER', estimatedPmDate: '2026-11-19', workOrderNo: 'EW26110029', itemNo: '30', estimatedHours: 1.5, actualHours: elapsedHours('2026-11-19T09:00:00', '2026-11-19T10:34:00'), executor: 'WANG04', releaseDate: '2026-11-19 09:00:00', finishedDate: '2026-11-19 10:34:00', workOrderType: 0 },
  { equipmentNo: 'PUMP-07', frequencyNo: '', sopNo: 'SOP-PUMP-REQ', estimatedPmDate: '2026-10-15', workOrderNo: 'REQ26100004', itemNo: '10', estimatedHours: 2.5, actualHours: elapsedHours('2026-10-15T13:00:00', '2026-10-15T16:22:00'), executor: 'WANG04', releaseDate: '2026-10-15 13:00:00', finishedDate: '2026-10-15 16:22:00', workOrderType: 1 },
  { equipmentNo: 'CNC-03', frequencyNo: '', sopNo: 'SOP-CNC-REQ', estimatedPmDate: '2026-11-01', workOrderNo: 'REQ26100011', itemNo: '10', estimatedHours: 2, actualHours: elapsedHours('2026-11-01T08:15:00', '2026-11-01T10:07:00'), executor: 'LIU03', releaseDate: '2026-11-01 08:15:00', finishedDate: '2026-11-01 10:07:00', workOrderType: 1 },
  { equipmentNo: 'ROBOT-01', frequencyNo: '', sopNo: 'SOP-ROBOT-REQ', estimatedPmDate: '2026-11-11', workOrderNo: 'REQ26110003', itemNo: '10', estimatedHours: 3.5, actualHours: elapsedHours('2026-11-11T15:00:00', '2026-11-11T18:26:00'), executor: 'WU05', releaseDate: '2026-11-11 15:00:00', finishedDate: '2026-11-11 18:26:00', workOrderType: 1 },
]

const lists = reactive<DualListState[]>([
  { key: 'equipment', title: 'Equipment No', availableLabel: 'Available', selectedLabel: 'Selected', available: visibleEquipmentOptions(), selected: [], availablePicked: [], selectedPicked: [] },
  { key: 'workOrder', title: 'PM WO No', availableLabel: 'Available', selectedLabel: 'Selected', available: [...workOrderOptions], selected: [], availablePicked: [], selectedPicked: [] },
  { key: 'frequency', title: 'Frequency No', availableLabel: 'Available', selectedLabel: 'Selected', available: [...frequencyOptions], selected: [], availablePicked: [], selectedPicked: [] },
  { key: 'executor', title: 'PM Executor', availableLabel: 'Available', selectedLabel: 'Selected', available: [...executorOptions], selected: [], availablePicked: [], selectedPicked: [] },
])

const estimatedStartEnabled = ref(false)
const estimatedEndEnabled = ref(false)
const finishedStartEnabled = ref(false)
const finishedEndEnabled = ref(false)
const estimatedStart = ref('2026-11-01')
const estimatedEnd = ref('2026-11-26')
const finishedStart = ref('2026-11-01')
const finishedEnd = ref('2026-11-26')
const workOrderType = ref<'all' | 'pm' | 'required'>('all')
const resultRows = ref<PerformanceRow[]>([])
const queried = ref(false)
const notice = ref('設定條件後按 Query。')
const noticeIsError = ref(false)

const equipmentFilter = computed(() => lists.find((list) => list.key === 'equipment')?.selected ?? [])
const workOrderFilter = computed(() => lists.find((list) => list.key === 'workOrder')?.selected ?? [])
const frequencyFilter = computed(() => lists.find((list) => list.key === 'frequency')?.selected ?? [])
const executorFilter = computed(() => lists.find((list) => list.key === 'executor')?.selected ?? [])
const summary = computed(() => queried.value ? `符合 ${resultRows.value.length} 筆工單明細` : '尚未查詢')

function elapsedHours(start: string, finish: string) {
  return Math.round((new Date(finish).getTime() - new Date(start).getTime()) / 36_000) / 100
}

function visibleEquipmentOptions() {
  return equipmentOptions.filter((equipmentNo) => chamberManage || !chamberEquipmentNos.has(equipmentNo))
}

function moveItems(list: DualListState, direction: MoveDirection) {
  const moving = direction === 'right' ? [...list.availablePicked] : [...list.selectedPicked]
  if (!moving.length) return
  if (direction === 'right') {
    list.selected = [...new Set([...list.selected, ...moving])].sort((left, right) => left.localeCompare(right))
    list.available = list.available.filter((item) => !moving.includes(item))
    list.availablePicked = []
  } else {
    list.available = [...new Set([...list.available, ...moving])].sort((left, right) => left.localeCompare(right))
    list.selected = list.selected.filter((item) => !moving.includes(item))
    list.selectedPicked = []
  }
}

function matchesDateRange(value: string, fromEnabled: boolean, from: string, toEnabled: boolean, to: string) {
  const date = value.slice(0, 10)
  if (fromEnabled && (!date || date < from)) return false
  if (toEnabled && (!date || date > to)) return false
  return true
}

function queryPerformance() {
  noticeIsError.value = false
  if ((estimatedStartEnabled.value && estimatedEndEnabled.value && estimatedStart.value > estimatedEnd.value)
    || (finishedStartEnabled.value && finishedEndEnabled.value && finishedStart.value > finishedEnd.value)) {
    notice.value = '日期起日不可晚於迄日。'
    noticeIsError.value = true
    queried.value = false
    resultRows.value = []
    return
  }

  resultRows.value = rows.filter((row) => {
    if (!chamberManage && chamberEquipmentNos.has(row.equipmentNo)) return false
    if (workOrderType.value === 'pm' && row.workOrderType !== 0) return false
    if (workOrderType.value === 'required' && row.workOrderType !== 1) return false
    if (equipmentFilter.value.length && !equipmentFilter.value.includes(row.equipmentNo)) return false
    if (workOrderFilter.value.length && !workOrderFilter.value.includes(row.workOrderNo)) return false
    if (frequencyFilter.value.length && !frequencyFilter.value.includes(row.frequencyNo)) return false
    if (executorFilter.value.length && !executorFilter.value.includes(row.executor)) return false
    if (!matchesDateRange(row.estimatedPmDate, estimatedStartEnabled.value, estimatedStart.value, estimatedEndEnabled.value, estimatedEnd.value)) return false
    if (!matchesDateRange(row.finishedDate, finishedStartEnabled.value, finishedStart.value, finishedEndEnabled.value, finishedEnd.value)) return false
    return true
  })
  queried.value = true
  notice.value = `查詢完成：${resultRows.value.length} 筆工單明細。`
}

function formatDate(value: string) { return value ? value.slice(0, 10).replaceAll('-', '/') : '—' }
function formatDateTime(value: string) { return value ? value.replaceAll('-', '/') : '—' }
function formatHours(value: number) { return value.toFixed(2) }
function workOrderTypeName(value: PerformanceRow['workOrderType']) { return value === 1 ? 'Required Work Order' : 'PM Work Order' }
</script>

<template>
  <section class="pm-performance" aria-label="設備保養效率查詢 DEV 預覽">
    <header class="page-header">
      <div><p>PM PERFORMANCE QUERY / PM506</p><h1>設備保養效率查詢</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (&amp;X)</button></div>
    </header>

    <section class="filter-lists" aria-label="多選查詢條件">
      <fieldset v-for="list in lists" :key="list.key" class="filter-list">
        <legend>{{ list.title }}</legend>
        <div class="list-pair">
          <label class="list-column">
            <span>{{ list.availableLabel }} ({{ list.available.length }})</span>
            <select v-model="list.availablePicked" multiple size="5" :aria-label="`${list.title} available`" @dblclick="moveItems(list, 'right')">
              <option v-for="value in list.available" :key="value" :value="value">{{ value }}</option>
            </select>
          </label>
          <div class="move-buttons" :aria-label="`${list.title} selection controls`">
            <button type="button" :aria-label="`Add selected ${list.title}`" title="Add selected" @click="moveItems(list, 'right')">›</button>
            <button type="button" :aria-label="`Remove selected ${list.title}`" title="Remove selected" @click="moveItems(list, 'left')">‹</button>
          </div>
          <label class="list-column">
            <span>{{ list.selectedLabel }} ({{ list.selected.length }})</span>
            <select v-model="list.selectedPicked" multiple size="5" :aria-label="`${list.title} selected`" @dblclick="moveItems(list, 'left')">
              <option v-for="value in list.selected" :key="value" :value="value">{{ value }}</option>
            </select>
          </label>
        </div>
      </fieldset>
    </section>

    <section class="criteria-row" aria-label="日期與工單類型">
      <fieldset class="date-panel">
        <legend>Estimated PM Date</legend>
        <label><input v-model="estimatedStartEnabled" type="checkbox"><span>From</span><input v-model="estimatedStart" type="date" :disabled="!estimatedStartEnabled"></label>
        <label><input v-model="estimatedEndEnabled" type="checkbox"><span>To</span><input v-model="estimatedEnd" type="date" :disabled="!estimatedEndEnabled"></label>
      </fieldset>
      <fieldset class="date-panel">
        <legend>WO Finished Date</legend>
        <label><input v-model="finishedStartEnabled" type="checkbox"><span>From</span><input v-model="finishedStart" type="date" :disabled="!finishedStartEnabled"></label>
        <label><input v-model="finishedEndEnabled" type="checkbox"><span>To</span><input v-model="finishedEnd" type="date" :disabled="!finishedEndEnabled"></label>
      </fieldset>
      <fieldset class="wo-type-panel">
        <legend>WO Type</legend>
        <label><input v-model="workOrderType" type="radio" value="all">All</label>
        <label><input v-model="workOrderType" type="radio" value="pm">PM Work Order</label>
        <label><input v-model="workOrderType" type="radio" value="required">Required Work Order</label>
      </fieldset>
      <div class="query-actions">
        <button type="button" class="primary" @click="queryPerformance">Query</button>
      </div>
    </section>

    <section class="results-panel" aria-label="保養效率結果">
      <div class="result-heading"><h2>PM Performance</h2><span>{{ summary }}</span></div>
      <p class="notice" :class="{ error: noticeIsError }" role="status">{{ notice }}</p>
      <div v-if="!queried" class="empty-state">按 Query 後顯示符合條件的完工保養工單。</div>
      <div v-else-if="resultRows.length === 0" class="empty-state">沒有符合條件的工單。</div>
      <div v-else class="table-scroll">
        <table>
          <thead><tr>
            <th>Equipment No</th><th>Frequency</th><th>SOP</th><th>EST Date</th><th>WO No</th><th>Item No</th>
            <th>Est Done (hr)</th><th>Act Done (hr)</th><th>Executor</th><th>Release Date</th><th>Finished Date</th><th>WO Type</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in resultRows" :key="`${row.workOrderNo}-${row.itemNo}-${row.equipmentNo}`">
              <td>{{ row.equipmentNo }}</td><td>{{ row.frequencyNo || '—' }}</td><td>{{ row.sopNo }}</td><td>{{ formatDate(row.estimatedPmDate) }}</td>
              <td>{{ row.workOrderNo }}</td><td>{{ row.itemNo }}</td><td class="numeric">{{ formatHours(row.estimatedHours) }}</td><td class="numeric">{{ formatHours(row.actualHours) }}</td>
              <td>{{ row.executor }}</td><td>{{ formatDateTime(row.releaseDate) }}</td><td>{{ formatDateTime(row.finishedDate) }}</td><td>{{ workOrderTypeName(row.workOrderType) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p class="formula-note">Act Done 依下線時間至完工時間換算小時；DEV 範例資料為本機假資料。</p>
    </section>
  </section>
</template>

<style scoped>
.pm-performance { display: flex; flex-direction: column; gap: 10px; min-height: 0; padding: 10px 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-header { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header p { margin: 0 0 2px; color: #777366; font-size: 11px; letter-spacing: .08em; }
.page-header h1 { margin: 0; font-size: 21px; font-weight: 650; }
.header-actions { display: flex; align-items: center; gap: 8px; }
.dev-tag { padding: 3px 7px; border: 1px solid #ba9850; border-radius: 3px; color: #71551b; background: #f8edce; font-size: 11px; }
button, input, select { font: inherit; }
button { cursor: pointer; }
.secondary, .primary { min-height: 28px; padding: 4px 11px; border: 1px solid #3b3930; border-radius: 3px; background: #fff; color: #3b3930; }
.primary { background: #3b3930; color: #fff; }
.filter-lists { display: grid; grid-template-columns: repeat(4, minmax(200px, 1fr)); gap: 8px; }
fieldset { min-width: 0; margin: 0; border: 1px solid #c9c5b8; border-radius: 3px; }
legend { padding: 0 5px; color: #514d42; font-size: 12px; font-weight: 650; }
.filter-list { padding: 5px 7px 8px; }
.list-pair { display: grid; grid-template-columns: minmax(0, 1fr) 28px minmax(0, 1fr); gap: 4px; align-items: center; }
.list-column { display: flex; min-width: 0; flex-direction: column; gap: 3px; color: #686457; font-size: 11px; }
.list-column select { width: 100%; min-width: 0; height: 104px; padding: 3px; border: 1px solid #c9c5b8; border-radius: 2px; background: #fff; color: #26251f; font-size: 12px; }
.move-buttons { display: flex; flex-direction: column; gap: 5px; }
.move-buttons button { width: 27px; min-height: 27px; border: 1px solid #b7b2a4; border-radius: 3px; background: #fff; color: #3b3930; font-size: 18px; line-height: 1; }
.criteria-row { display: grid; grid-template-columns: minmax(205px, 1fr) minmax(205px, 1fr) minmax(285px, 1.2fr) auto; gap: 8px; align-items: stretch; }
.date-panel, .wo-type-panel { display: flex; flex-direction: column; gap: 6px; padding: 6px 9px 8px; }
.date-panel label { display: grid; grid-template-columns: 16px 42px minmax(0, 1fr); gap: 5px; align-items: center; font-size: 12px; }
.date-panel input[type="date"] { min-width: 0; width: 100%; height: 27px; padding: 2px 5px; border: 1px solid #c9c5b8; border-radius: 2px; background: #fff; color: #26251f; }
.date-panel input:disabled { color: #938f83; background: #eeeae0; }
.wo-type-panel { flex-direction: row; flex-wrap: wrap; align-items: center; gap: 8px 14px; }
.wo-type-panel legend { width: auto; }
.wo-type-panel label { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; white-space: nowrap; }
.query-actions { display: flex; align-items: center; justify-content: flex-end; }
.query-actions .primary { min-width: 88px; min-height: 34px; }
.results-panel { display: flex; min-height: 175px; flex-direction: column; padding: 8px 9px 7px; border: 1px solid #c9c5b8; border-radius: 3px; background: #fbfaf6; }
.result-heading { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.result-heading h2 { margin: 0; font-size: 15px; }
.result-heading span, .notice, .formula-note { color: #777366; font-size: 11px; }
.notice { margin: 4px 0 7px; }
.notice.error { color: #a1352d; }
.empty-state { display: grid; min-height: 96px; place-items: center; color: #777366; font-size: 13px; }
.table-scroll { overflow: auto; border: 1px solid #d4d0c5; background: white; }
table { width: 100%; min-width: 1220px; border-collapse: collapse; font-size: 12px; }
th, td { padding: 6px 7px; border-right: 1px solid #e1ded5; border-bottom: 1px solid #e1ded5; text-align: left; white-space: nowrap; }
th { position: sticky; top: 0; z-index: 1; background: #efede5; color: #403d34; font-weight: 650; }
tbody tr:nth-child(even) { background: #faf9f5; }
td.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.formula-note { margin: 7px 0 0; }
@media (max-width: 1120px) { .filter-lists { grid-template-columns: repeat(2, minmax(210px, 1fr)); } .criteria-row { grid-template-columns: repeat(2, minmax(220px, 1fr)); } }
@media (max-width: 650px) { .page-header { align-items: flex-start; } .filter-lists, .criteria-row { grid-template-columns: 1fr; } .wo-type-panel { justify-content: flex-start; } }
</style>

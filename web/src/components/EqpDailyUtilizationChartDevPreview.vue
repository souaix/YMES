<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type ViewMode = 'main' | 'chamber'
type FilterKey = 'areaNo' | 'equipmentType' | 'equipmentNo' | 'mainEquipmentNo'
type PickerKey = FilterKey
interface EquipmentState { id: number; name: string; color: string; utilizationType: number }
interface SegmentPattern { stateId: number; from: number; to: number; reason: string }
interface EquipmentPattern { equipmentNo: string; equipmentType: string; areaNo: string; areaName: string; belongEquipmentNo: string; segments: SegmentPattern[] }
interface UtilSegment { id: string; name: string; color: string; utilizationType: number; start: Date; end: Date; left: number; width: number; reason: string }
type UtilizationRow = Omit<EquipmentPattern, 'segments'> & { segments: UtilSegment[]; utilizationRate: number }
interface LookupOption { value: string; label: string }

const emit = defineEmits<{ close: [] }>()
const states: EquipmentState[] = [
  { id: 1, name: '生產', color: '#4f9c68', utilizationType: 1 },
  { id: 2, name: '閒置', color: '#9ca3a5', utilizationType: 0 },
  { id: 3, name: '待機', color: '#56a7c1', utilizationType: 0 },
  { id: 4, name: '待檢', color: '#e1b84d', utilizationType: 0 },
  { id: 5, name: '保養', color: '#8e75bf', utilizationType: 0 },
  { id: 6, name: '維修', color: '#e28c4b', utilizationType: 0 },
  { id: 7, name: '故障', color: '#c65b5b', utilizationType: 0 },
]
const stateById = new Map(states.map((state) => [state.id, state]))

const mainPatterns: EquipmentPattern[] = [
  { equipmentNo: 'EQ-ASM-001', equipmentType: 'Bonder', areaNo: 'AREA-A', areaName: '一廠組裝區', belongEquipmentNo: '', segments: [
    { stateId: 1, from: 0, to: .27, reason: '正常生產' }, { stateId: 2, from: .27, to: .32, reason: '等候排程' }, { stateId: 1, from: .32, to: .67, reason: '正常生產' }, { stateId: 5, from: .67, to: .72, reason: '定期保養' }, { stateId: 1, from: .72, to: .91, reason: '正常生產' }, { stateId: 7, from: .91, to: .94, reason: '設備警報' }, { stateId: 1, from: .94, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-ASM-002', equipmentType: 'Die Attach', areaNo: 'AREA-A', areaName: '一廠組裝區', belongEquipmentNo: '', segments: [
    { stateId: 1, from: 0, to: .18, reason: '正常生產' }, { stateId: 3, from: .18, to: .24, reason: '待機' }, { stateId: 1, from: .24, to: .48, reason: '正常生產' }, { stateId: 4, from: .48, to: .53, reason: '待品質確認' }, { stateId: 1, from: .53, to: .82, reason: '正常生產' }, { stateId: 6, from: .82, to: .9, reason: '更換治具' }, { stateId: 1, from: .9, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-TST-001', equipmentType: 'Handler', areaNo: 'AREA-B', areaName: '二廠測試區', belongEquipmentNo: '', segments: [
    { stateId: 1, from: 0, to: .41, reason: '正常生產' }, { stateId: 2, from: .41, to: .44, reason: '待料' }, { stateId: 1, from: .44, to: .77, reason: '正常生產' }, { stateId: 5, from: .77, to: .81, reason: '日常點檢' }, { stateId: 1, from: .81, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-TST-002', equipmentType: 'Tester', areaNo: 'AREA-B', areaName: '二廠測試區', belongEquipmentNo: '', segments: [
    { stateId: 1, from: 0, to: .22, reason: '正常生產' }, { stateId: 7, from: .22, to: .3, reason: '通訊告警' }, { stateId: 6, from: .3, to: .35, reason: '工程處理' }, { stateId: 1, from: .35, to: .63, reason: '正常生產' }, { stateId: 3, from: .63, to: .72, reason: '等待工程設定' }, { stateId: 1, from: .72, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-PKG-001', equipmentType: 'Packer', areaNo: 'AREA-C', areaName: '三廠包裝區', belongEquipmentNo: '', segments: [
    { stateId: 2, from: 0, to: .35, reason: '待命' }, { stateId: 1, from: .35, to: .75, reason: '正常生產' }, { stateId: 4, from: .75, to: .8, reason: '待檢' }, { stateId: 1, from: .8, to: 1, reason: '正常生產' },
  ] },
]
const chamberPatterns: EquipmentPattern[] = [
  { equipmentNo: 'EQ-ASM-001-01', equipmentType: 'Bonder', areaNo: 'AREA-A', areaName: '一廠組裝區', belongEquipmentNo: 'EQ-ASM-001', segments: [
    { stateId: 1, from: 0, to: .31, reason: '正常生產' }, { stateId: 2, from: .31, to: .35, reason: '等候排程' }, { stateId: 1, from: .35, to: .73, reason: '正常生產' }, { stateId: 5, from: .73, to: .78, reason: '定期保養' }, { stateId: 1, from: .78, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-ASM-001-02', equipmentType: 'Bonder', areaNo: 'AREA-A', areaName: '一廠組裝區', belongEquipmentNo: 'EQ-ASM-001', segments: [
    { stateId: 1, from: 0, to: .24, reason: '正常生產' }, { stateId: 7, from: .24, to: .29, reason: '設備警報' }, { stateId: 1, from: .29, to: .63, reason: '正常生產' }, { stateId: 3, from: .63, to: .71, reason: '待機' }, { stateId: 1, from: .71, to: 1, reason: '正常生產' },
  ] },
  { equipmentNo: 'EQ-TST-001-CH01', equipmentType: 'Handler', areaNo: 'AREA-B', areaName: '二廠測試區', belongEquipmentNo: 'EQ-TST-001', segments: [
    { stateId: 1, from: 0, to: .54, reason: '正常生產' }, { stateId: 4, from: .54, to: .61, reason: '待品質確認' }, { stateId: 1, from: .61, to: .9, reason: '正常生產' }, { stateId: 2, from: .9, to: 1, reason: '待料' },
  ] },
]

const now = new Date()
const defaultEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 8)
if (now < defaultEnd) defaultEnd.setTime(now.getTime())
const defaultStart = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 1, 8)
function toLocalInput(value: Date) {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
}
function values(text: string) { return new Set(text.split(',').map((item) => item.trim().toUpperCase()).filter(Boolean)) }
const mainFilters = reactive<Record<FilterKey, string>>({ areaNo: '', equipmentType: '', equipmentNo: '', mainEquipmentNo: '' })
const chamberFilters = reactive<Record<FilterKey, string>>({ areaNo: '', equipmentType: '', equipmentNo: '', mainEquipmentNo: '' })
const mainEnabled = reactive<Record<FilterKey, boolean>>({ areaNo: false, equipmentType: false, equipmentNo: false, mainEquipmentNo: false })
const chamberEnabled = reactive<Record<FilterKey, boolean>>({ areaNo: false, equipmentType: false, equipmentNo: false, mainEquipmentNo: false })
const mainStartTime = ref(toLocalInput(defaultStart))
const mainEndTime = ref(toLocalInput(defaultEnd))
const chamberStartTime = ref(toLocalInput(defaultStart))
const chamberEndTime = ref(toLocalInput(defaultEnd))
const viewMode = ref<ViewMode>('main')
const chamberManage = ref(false)
const hasQueried = ref(false)
const resultRows = ref<UtilizationRow[]>([])
const queriedStart = ref('')
const queriedEnd = ref('')
const selectedEquipmentNo = ref('')
const selectedSegment = ref<UtilSegment | null>(null)
const queryError = ref('')
const notice = ref('')
const showInfo = ref(false)
const pickerTarget = ref<ViewMode>('main')
const pickerKey = ref<PickerKey>('areaNo')
const pickerOpen = ref(false)
const pickerSearch = ref('')
const stagedValues = reactive(new Set<string>())
const boldLabels = ref(true)
const labelSize = ref(11)

const targetFilters = computed(() => pickerTarget.value === 'main' ? mainFilters : chamberFilters)
const targetEnabled = computed(() => pickerTarget.value === 'main' ? mainEnabled : chamberEnabled)
const activeFilters = computed(() => viewMode.value === 'main' ? mainFilters : chamberFilters)
const activeEnabled = computed(() => viewMode.value === 'main' ? mainEnabled : chamberEnabled)
const activeStartTime = computed({ get: () => viewMode.value === 'main' ? mainStartTime.value : chamberStartTime.value, set: (value: string) => { if (viewMode.value === 'main') mainStartTime.value = value; else chamberStartTime.value = value } })
const activeEndTime = computed({ get: () => viewMode.value === 'main' ? mainEndTime.value : chamberEndTime.value, set: (value: string) => { if (viewMode.value === 'main') mainEndTime.value = value; else chamberEndTime.value = value } })
const areaOptions: LookupOption[] = Array.from(new Map(mainPatterns.map((row) => [row.areaNo, { value: row.areaNo, label: `${row.areaNo} · ${row.areaName}` }])).values())
const typeOptions: LookupOption[] = Array.from(new Set(mainPatterns.map((row) => row.equipmentType))).map((item) => ({ value: item, label: item }))
const pickerOptions = computed(() => {
  const search = pickerSearch.value.trim().toUpperCase()
  const filters = targetFilters.value
  let options: LookupOption[]
  if (pickerKey.value === 'areaNo') options = areaOptions
  else if (pickerKey.value === 'equipmentType') options = typeOptions
  else if (pickerKey.value === 'mainEquipmentNo') options = mainPatterns.filter((row) => !values(filters.equipmentType).size || values(filters.equipmentType).has(row.equipmentType.toUpperCase())).map((row) => ({ value: row.equipmentNo, label: `${row.equipmentNo} · ${row.equipmentType}` }))
  else {
    const source = pickerTarget.value === 'main' ? mainPatterns : chamberPatterns
    options = source.filter((row) => !values(filters.equipmentType).size || values(filters.equipmentType).has(row.equipmentType.toUpperCase())).map((row) => ({ value: row.equipmentNo, label: `${row.equipmentNo} · ${row.equipmentType}` }))
  }
  return options.filter((option) => !search || option.label.toUpperCase().includes(search))
})
const pickerTitle = computed(() => ({ areaNo: '選擇 Area No', equipmentType: '選擇 Equipment Type', equipmentNo: viewMode.value === 'main' ? '選擇 Equipment No' : '選擇 Chamber Equipment No', mainEquipmentNo: '選擇 Main Equipment No' }[pickerKey.value]))
const chartRangeMinutes = computed(() => queriedStart.value && queriedEnd.value ? Math.max(1, (new Date(queriedEnd.value).getTime() - new Date(queriedStart.value).getTime()) / 60000) : 1)
const chartTicks = computed(() => {
  if (!queriedStart.value || !queriedEnd.value) return []
  const count = Math.ceil(chartRangeMinutes.value / 60)
  return Array.from({ length: count + 1 }, (_, index) => {
    const offsetMinutes = Math.min(chartRangeMinutes.value, index * 60)
    const at = new Date(new Date(queriedStart.value).getTime() + offsetMinutes * 60000)
    return { index, label: at.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false }), left: Math.min(100, offsetMinutes / chartRangeMinutes.value * 100) }
  })
})
const chartWidth = computed(() => `${Math.max(760, chartTicks.value.length * 67)}px`)
const selectedRow = computed(() => resultRows.value.find((row) => row.equipmentNo === selectedEquipmentNo.value) ?? null)

function setChecked(target: ViewMode, key: FilterKey, checked: boolean) {
  const enabled = target === 'main' ? mainEnabled : chamberEnabled
  const filters = target === 'main' ? mainFilters : chamberFilters
  enabled[key] = checked
  if (!checked) filters[key] = ''
}
function setEquipmentText(target: ViewMode, key: 'equipmentNo', text: string) {
  const filters = target === 'main' ? mainFilters : chamberFilters
  const enabled = target === 'main' ? mainEnabled : chamberEnabled
  filters[key] = text.toUpperCase()
  enabled[key] = Boolean(filters[key].trim())
}
function openPicker(target: ViewMode, key: PickerKey) {
  pickerTarget.value = target
  pickerKey.value = key
  pickerSearch.value = ''
  stagedValues.clear()
  values(target === 'main' ? mainFilters[key] : chamberFilters[key]).forEach((value) => stagedValues.add(value))
  pickerOpen.value = true
}
function setStaged(value: string, checked: boolean) { checked ? stagedValues.add(value.toUpperCase()) : stagedValues.delete(value.toUpperCase()) }
function applyPicker() {
  const filters = targetFilters.value
  const enabled = targetEnabled.value
  filters[pickerKey.value] = Array.from(stagedValues).join(',')
  enabled[pickerKey.value] = stagedValues.size > 0
  pickerOpen.value = false
}
function createRows(source: EquipmentPattern[], start: Date, end: Date): UtilizationRow[] {
  const rangeMs = end.getTime() - start.getTime()
  const rangeMinutes = Math.floor(end.getTime() / 60000) - Math.floor(start.getTime() / 60000)
  return source.map((row) => {
    const segments = row.segments.map((pattern, index) => {
      const state = stateById.get(pattern.stateId) ?? states[0]
      const segStartMs = start.getTime() + rangeMs * pattern.from
      const segEndMs = start.getTime() + rangeMs * pattern.to
      return { ...state, id: `${row.equipmentNo}-${index}`, start: new Date(segStartMs), end: new Date(segEndMs), left: pattern.from * 100, width: (pattern.to - pattern.from) * 100, reason: pattern.reason }
    })
    const offMinutes = segments.filter((segment) => segment.utilizationType === 0).reduce((sum, segment) => sum + Math.floor(segment.end.getTime() / 60000) - Math.floor(segment.start.getTime() / 60000), 0)
    const utilizationRate = rangeMinutes ? Math.max(0, Math.min(1, (rangeMinutes - offMinutes) / rangeMinutes)) * 100 : 0
    return { ...row, segments, utilizationRate }
  })
}
function validateRange(startText: string, endText: string): { start?: Date; end?: Date; error: string } {
  const start = new Date(startText)
  const end = new Date(endText)
  if (!startText || !endText || Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return { error: '請輸入有效的 StartTime 與 EndTime。' }
  if (start > new Date() || end > new Date()) return { error: 'StartTime／EndTime 不可晚於現在。' }
  if (start >= end) return { error: 'StartTime 必須早於 EndTime。' }
  const startHour = new Date(start.getFullYear(), start.getMonth(), start.getDate(), start.getHours()).getTime()
  const endHour = new Date(end.getFullYear(), end.getMonth(), end.getDate(), end.getHours()).getTime()
  if ((endHour - startHour) / 3600000 > 24) return { error: '查詢區間不可超過 24 小時。' }
  return { start, end, error: '' }
}
function query() {
  queryError.value = ''
  notice.value = ''
  const range = validateRange(activeStartTime.value, activeEndTime.value)
  if (range.error || !range.start || !range.end) { queryError.value = range.error; return }
  const filters = activeFilters.value
  const enabled = activeEnabled.value
  const areas = enabled.areaNo ? values(filters.areaNo) : new Set<string>()
  const types = enabled.equipmentType ? values(filters.equipmentType) : new Set<string>()
  const equipmentNos = enabled.equipmentNo ? values(filters.equipmentNo) : new Set<string>()
  const parentNos = enabled.mainEquipmentNo ? values(filters.mainEquipmentNo) : new Set<string>()
  const source = viewMode.value === 'main' ? mainPatterns : chamberPatterns
  const filtered = source.filter((row) =>
    (!areas.size || areas.has(row.areaNo.toUpperCase())) &&
    (!types.size || types.has(row.equipmentType.toUpperCase())) &&
    (!equipmentNos.size || equipmentNos.has(row.equipmentNo.toUpperCase())) &&
    (viewMode.value === 'main' || !parentNos.size || parentNos.has(row.belongEquipmentNo.toUpperCase())),
  ).sort((a, b) => a.areaNo.localeCompare(b.areaNo) || a.equipmentType.localeCompare(b.equipmentType) || a.equipmentNo.localeCompare(b.equipmentNo))
  resultRows.value = createRows(filtered, range.start, range.end)
  queriedStart.value = range.start.toISOString()
  queriedEnd.value = range.end.toISOString()
  selectedEquipmentNo.value = ''
  selectedSegment.value = null
  hasQueried.value = true
  if (!resultRows.value.length) notice.value = '查無符合設備狀態資料。'
}
function showChambers() {
  viewMode.value = 'chamber'
  hasQueried.value = false
  resultRows.value = []
  selectedEquipmentNo.value = ''
  selectedSegment.value = null
}
function showMainChart() {
  viewMode.value = 'main'
  hasQueried.value = false
  resultRows.value = []
  selectedEquipmentNo.value = ''
  selectedSegment.value = null
}
function selectRow(row: UtilizationRow) { selectedEquipmentNo.value = row.equipmentNo; selectedSegment.value = null }
function selectSegment(row: UtilizationRow, segment: UtilSegment) { selectedEquipmentNo.value = row.equipmentNo; selectedSegment.value = segment }
function formatDate(value: Date) { return value.toLocaleString('zh-TW', { hour12: false }) }
function copyChart() { notice.value = 'DEV 已模擬複製圖表；未存取系統剪貼簿。' }
function increaseFont() { labelSize.value = Math.min(16, labelSize.value + 1) }
function decreaseFont() { labelSize.value = Math.max(9, labelSize.value - 1) }
</script>

<template>
  <main class="utilization-report" aria-label="每日設備稼動長條圖 DEV" @keydown.ctrl.c.prevent="copyChart">
    <header class="page-heading">
      <div><div class="eyebrow">RPT / RT411</div><h1>{{ viewMode === 'main' ? '每日設備稼動長條圖' : '子設備每日稼動長條圖' }}</h1><p>以設備狀態歷程顯示各設備在所選區間的時間軸。</p></div>
      <div class="header-actions"><span class="dev-badge">DEV 假資料</span><button type="button" class="secondary-button" @click="showInfo = true">Info</button><button v-if="viewMode === 'chamber'" type="button" class="secondary-button" @click="showMainChart">返回主設備</button><button v-else-if="chamberManage" type="button" class="secondary-button" @click="showChambers">Chamber 子設備</button><button type="button" class="secondary-button" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="filter-grid">
        <div class="filter-field"><label><input type="checkbox" :checked="activeEnabled.areaNo" @change="setChecked(viewMode, 'areaNo', ($event.target as HTMLInputElement).checked)"> Area No</label><div class="picker-input"><input :value="activeFilters.areaNo" readonly placeholder="不限區域"><button type="button" aria-label="選擇 Area No" @click="openPicker(viewMode, 'areaNo')">…</button></div></div>
        <div class="filter-field"><label><input type="checkbox" :checked="activeEnabled.equipmentType" @change="setChecked(viewMode, 'equipmentType', ($event.target as HTMLInputElement).checked)"> Equipment Type</label><div class="picker-input"><input :value="activeFilters.equipmentType" readonly placeholder="不限設備類別"><button type="button" aria-label="選擇 Equipment Type" @click="openPicker(viewMode, 'equipmentType')">…</button></div></div>
        <div class="filter-field"><label><input type="checkbox" :checked="activeEnabled.equipmentNo" @change="setChecked(viewMode, 'equipmentNo', ($event.target as HTMLInputElement).checked)"> {{ viewMode === 'main' ? 'Equipment No' : 'Chamber Equipment No' }}</label><div class="picker-input"><input :value="activeFilters.equipmentNo" placeholder="輸入或選擇設備編號" @input="setEquipmentText(viewMode, 'equipmentNo', ($event.target as HTMLInputElement).value)" @blur="setEquipmentText(viewMode, 'equipmentNo', activeFilters.equipmentNo.trim())"><button type="button" aria-label="選擇設備" @click="openPicker(viewMode, 'equipmentNo')">…</button></div></div>
        <div v-if="viewMode === 'chamber'" class="filter-field"><label><input type="checkbox" :checked="activeEnabled.mainEquipmentNo" @change="setChecked(viewMode, 'mainEquipmentNo', ($event.target as HTMLInputElement).checked)"> Main Equipment No</label><div class="picker-input"><input :value="activeFilters.mainEquipmentNo" readonly placeholder="不限主設備"><button type="button" aria-label="選擇主設備" @click="openPicker(viewMode, 'mainEquipmentNo')">…</button></div></div>
        <label class="date-field"><span>StartTime</span><input v-model="activeStartTime" type="datetime-local"></label>
        <label class="date-field"><span>EndTime</span><input v-model="activeEndTime" type="datetime-local"></label>
      </div>
      <div class="query-actions"><label v-if="viewMode === 'main'" class="dev-setting"><input v-model="chamberManage" type="checkbox"> DEV 模擬 ChamberManage</label><span v-else class="chamber-hint">子設備條件以 MainEquipmentNo 篩選 BelongEquipmentNo。</span><button type="button" class="query-button" @click="query">Query</button><button type="button" class="secondary-button" @click="emit('close')">Close (X)</button></div>
      <p v-if="queryError" class="error-message" role="alert">{{ queryError }}</p>
    </section>

    <section class="state-legend" aria-label="設備狀態圖例"><strong>Equipment State</strong><span v-for="state in states" :key="state.id" class="legend-item"><i :style="{ background: state.color }"></i>{{ state.name }}</span><span v-if="queriedStart" class="range-caption">{{ formatDate(new Date(queriedStart)) }} 至 {{ formatDate(new Date(queriedEnd)) }}</span></section>

    <section class="chart-panel">
      <header class="chart-toolbar"><div><strong>{{ viewMode === 'main' ? '設備狀態時間軸' : '子設備狀態時間軸' }}</strong><span v-if="hasQueried">{{ resultRows.length }} 台設備 · 最長 24 小時</span></div><div class="chart-tools"><button type="button" @click="boldLabels = !boldLabels">{{ boldLabels ? '一般字體' : '粗體標籤' }}</button><button type="button" aria-label="放大設備標籤" @click="increaseFont">A+</button><button type="button" aria-label="縮小設備標籤" @click="decreaseFont">A−</button><button type="button" @click="copyChart">複製圖表</button></div></header>
      <div v-if="hasQueried && resultRows.length" class="gantt-scroll" tabindex="0">
        <div class="gantt-content" :style="{ width: chartWidth }">
          <div class="axis-row"><div class="equipment-axis">Equipment No · Type · Utilization Rate</div><div class="time-axis"><span v-for="tick in chartTicks" :key="tick.index" :style="{ left: `${tick.left}%` }">{{ tick.label }}</span></div></div>
          <div v-for="row in resultRows" :key="row.equipmentNo" class="gantt-row" :class="{ selected: selectedEquipmentNo === row.equipmentNo }" @click="selectRow(row)">
            <div class="equipment-label" :class="{ bold: boldLabels }" :style="{ fontSize: `${labelSize}px` }"><strong>{{ row.equipmentNo }}</strong><span>{{ row.equipmentType }}</span><b>{{ Math.round(row.utilizationRate) }}%</b></div>
            <div class="timeline">
              <i v-for="tick in chartTicks" :key="tick.index" class="time-grid-line" :style="{ left: `${tick.left}%` }"></i>
              <button v-for="segment in row.segments" :key="segment.id" type="button" class="state-segment" :class="{ active: selectedSegment?.id === segment.id }" :style="{ left: `${segment.left}%`, width: `${segment.width}%`, background: segment.color }" :title="`${segment.name} · ${segment.reason}\n${formatDate(segment.start)} — ${formatDate(segment.end)}`" :aria-label="`${row.equipmentNo} ${segment.name} ${segment.reason} ${formatDate(segment.start)} 至 ${formatDate(segment.end)}`" @click.stop="selectSegment(row, segment)"><span>{{ segment.width > 10 ? segment.name : '' }}</span></button>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="chart-empty"><strong>{{ hasQueried ? 'NO RECORD' : '尚未查詢' }}</strong><span>{{ hasQueried ? (notice || '沒有符合的設備狀態資料。') : '設定條件與時間範圍後按 Query。' }}</span></div>
      <footer class="chart-caption">左側 Utilization Rate =（區間分鐘數 − UtilizationType=0 的狀態分鐘數）÷ 區間分鐘數。狀態片段、Reason、時間與顏色為有限 DEV 假資料。</footer>
    </section>

    <section class="selection-panel" aria-live="polite">
      <template v-if="selectedSegment && selectedRow"><div class="selection-title"><span class="state-dot" :style="{ background: selectedSegment.color }"></span><strong>{{ selectedRow.equipmentNo }} · {{ selectedSegment.name }}</strong><span>{{ selectedSegment.reason }}</span></div><div class="selection-times"><span>Start: {{ formatDate(selectedSegment.start) }}</span><span>End: {{ formatDate(selectedSegment.end) }}</span><span>Duration: {{ Math.floor((selectedSegment.end.getTime() - selectedSegment.start.getTime()) / 60000) }} min</span></div></template>
      <span v-else-if="selectedRow">{{ selectedRow.equipmentNo }} · {{ selectedRow.areaName }} · Utilization Rate {{ Math.round(selectedRow.utilizationRate) }}% · 點選彩色區段查看原因與時間。</span>
      <span v-else>將游標停在彩色區段可查看狀態、原因與時間；點選區段可固定明細。</span>
      <span v-if="notice" class="notice-message">{{ notice }}</span>
    </section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false">
      <section class="picker-modal" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><div><span class="eyebrow">MASTER LOOKUP</span><h2>{{ pickerTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <input v-model="pickerSearch" class="picker-search" type="search" placeholder="輸入關鍵字篩選">
        <div class="picker-options"><label v-for="option in pickerOptions" :key="option.value" class="picker-option"><input type="checkbox" :checked="stagedValues.has(option.value.toUpperCase())" @change="setStaged(option.value, ($event.target as HTMLInputElement).checked)"><strong>{{ option.value }}</strong><span>{{ option.label }}</span></label><p v-if="!pickerOptions.length" class="no-options">沒有符合選項。</p></div>
        <footer><span>已選 {{ stagedValues.size }} 項</span><button type="button" class="query-button" @click="applyPicker">套用選取</button></footer>
      </section>
    </div>
    <div v-if="showInfo" class="modal-backdrop" @click.self="showInfo = false">
      <section class="info-modal" role="dialog" aria-modal="true" aria-label="設備稼動長條圖資訊">
        <header><div><span class="eyebrow">RT_Information</span><h2>每日設備稼動長條圖</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="showInfo = false">×</button></header>
        <p>圖表顯示所選區間內設備狀態歷程。左側利用設備狀態主檔的 UtilizationType 計算稼動率；狀態色彩、原因及區間皆為 DEV 固定假資料。</p>
        <footer><button type="button" class="query-button" @click="showInfo = false">Close</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.utilization-report{box-sizing:border-box;height:100%;min-height:0;overflow:auto;padding:14px;background:#f4f2eb;color:#292820;font:12px/1.45 Arial,"Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:12px;margin-bottom:10px}.eyebrow{color:#827d6d;font-size:9px;font-weight:700;letter-spacing:.12em}.page-heading h1{margin:2px 0;font-size:19px}.page-heading p{margin:0;color:#777366}.header-actions{display:flex;align-items:center;gap:6px}.dev-badge{padding:4px 8px;border:1px solid #d8c98d;border-radius:12px;background:#f8edc5;color:#715c15;font-size:10px;font-weight:700}.query-panel,.chart-panel,.state-legend,.selection-panel{border:1px solid #d4d1c5;background:#fffefa;box-shadow:0 1px 2px #26251f0a}.query-panel{padding:11px}.filter-grid{display:grid;grid-template-columns:repeat(3,minmax(210px,1fr)) repeat(2,minmax(220px,.9fr));gap:8px 12px;align-items:end}.filter-field>label,.date-field>span{display:flex;align-items:center;gap:5px;margin-bottom:4px;color:#565347;font-size:10px;font-weight:700}.filter-field input[type=checkbox],.dev-setting input{accent-color:#3c6a54}.picker-input{display:flex;min-width:0}.picker-input input,.date-field input,.picker-search{box-sizing:border-box;width:100%;height:29px;padding:4px 7px;border:1px solid #cbc8bc;background:#fff;color:#292820;font:inherit}.picker-input button{width:30px;border:1px solid #cbc8bc;border-left:0;background:#eeeae0;font:inherit;cursor:pointer}.query-actions{display:flex;align-items:center;gap:7px;margin-top:9px}.dev-setting{display:flex;align-items:center;gap:5px;margin-right:auto;color:#706c5e;font-size:10px}.chamber-hint{margin-right:auto;color:#777366;font-size:10px}.query-button,.secondary-button{min-height:28px;padding:4px 12px;border:1px solid #3c6a54;border-radius:3px;background:#3c6a54;color:#fff;font:inherit;font-weight:700;cursor:pointer}.secondary-button{border-color:#aaa697;background:#fffefa;color:#4e4a40}.error-message{margin:8px 0 0;color:#b03939}.state-legend{display:flex;align-items:center;flex-wrap:wrap;gap:7px 12px;margin:8px 0;padding:7px 9px}.state-legend>strong{margin-right:3px;color:#6c6758;font-size:10px}.legend-item{display:flex;align-items:center;gap:4px;color:#5b584d;font-size:10px}.legend-item i,.state-dot{display:inline-block;width:11px;height:11px;border-radius:2px}.range-caption{margin-left:auto;color:#777366;font-size:10px}.chart-panel{min-height:260px}.chart-toolbar{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 10px;border-bottom:1px solid #e1ded4}.chart-toolbar>div:first-child{display:flex;align-items:center;gap:10px}.chart-toolbar>div:first-child span{color:#777366;font-size:10px}.chart-tools{display:flex;gap:4px}.chart-tools button{min-height:23px;padding:2px 7px;border:1px solid #d0cdbf;background:#faf9f5;color:#4e4a40;font:inherit;font-size:10px;cursor:pointer}.gantt-scroll{max-height:calc(100vh - 390px);min-height:230px;overflow:auto}.gantt-content{min-width:760px}.axis-row,.gantt-row{display:grid;grid-template-columns:250px minmax(0,1fr)}.axis-row{position:sticky;top:0;z-index:2;background:#eeece4}.equipment-axis{display:flex;align-items:center;padding:6px 8px;color:#514e43;font-size:10px;font-weight:700}.time-axis{position:relative;height:28px;border-left:1px solid #d8d5ca}.time-axis span{position:absolute;top:5px;transform:translateX(-50%);color:#514e43;font-size:10px;font-weight:700;white-space:nowrap}.gantt-row{min-height:38px;border-top:1px solid #ece9df;cursor:pointer}.gantt-row:hover,.gantt-row.selected{background:#f3f6f0}.equipment-label{display:grid;grid-template-columns:minmax(100px,1fr) minmax(48px,.6fr) 40px;align-items:center;gap:5px;overflow:hidden;padding:4px 7px;border-right:1px solid #e2dfd5;color:#49463c;white-space:nowrap}.equipment-label strong,.equipment-label span{overflow:hidden;text-overflow:ellipsis}.equipment-label span{color:#777366}.equipment-label b{text-align:right;font-variant-numeric:tabular-nums}.equipment-label.bold strong,.equipment-label.bold b{font-weight:700}.timeline{position:relative;min-height:38px;background:#fffefa}.time-grid-line{position:absolute;top:0;bottom:0;z-index:0;border-left:1px dashed #d8d5ca}.state-segment{position:absolute;top:8px;z-index:1;height:21px;overflow:hidden;padding:0 3px;border:0;border-radius:2px;color:#fff;font:9px/21px Arial,"Microsoft JhengHei",sans-serif;text-align:center;white-space:nowrap;text-shadow:0 1px 1px #0005;cursor:pointer}.state-segment.active{outline:2px solid #302e28;outline-offset:1px}.chart-empty{display:flex;min-height:230px;flex-direction:column;align-items:center;justify-content:center;gap:5px;color:#827d6d}.chart-empty strong{font-size:13px}.chart-caption{padding:6px 9px;border-top:1px solid #e5e2d8;color:#777366;font-size:9px}.selection-panel{display:flex;min-height:36px;align-items:center;justify-content:space-between;gap:8px;margin-top:8px;padding:7px 9px;color:#5b584d;font-size:10px}.selection-title,.selection-times{display:flex;align-items:center;gap:7px;flex-wrap:wrap}.selection-title>span:last-child{color:#777366}.selection-times{margin-left:auto;font-variant-numeric:tabular-nums}.notice-message{color:#9a6d17}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#201f1bcc}.picker-modal,.info-modal{display:flex;width:min(480px,100%);max-height:min(600px,90vh);flex-direction:column;border:1px solid #c9c5b8;background:#fffefa;box-shadow:0 16px 55px #0005}.picker-modal>header,.picker-modal>footer,.info-modal>header,.info-modal>footer{display:flex;align-items:center;justify-content:space-between;padding:11px;border-bottom:1px solid #e1ded4}.picker-modal>header h2,.info-modal>header h2{margin:3px 0 0;font-size:15px}.picker-modal>footer,.info-modal>footer{border-top:1px solid #e1ded4;border-bottom:0;color:#777366}.icon-button{width:27px;height:27px;border:0;background:transparent;font-size:22px;cursor:pointer}.picker-search{width:calc(100% - 22px);margin:9px 11px}.picker-options{overflow:auto;min-height:100px;padding:0 11px}.picker-option{display:flex;min-height:34px;align-items:center;gap:8px;padding:5px 7px;border-bottom:1px solid #efede6;color:#514e43;font-size:10px;cursor:pointer}.picker-option input{accent-color:#3c6a54}.picker-option strong{min-width:115px;color:#343229}.picker-option span{color:#777366}.no-options{padding:25px;text-align:center;color:#827d6d}.info-modal p{margin:12px;line-height:1.6}
@media(max-width:1080px){.filter-grid{grid-template-columns:repeat(2,minmax(200px,1fr))}.axis-row,.gantt-row{grid-template-columns:205px minmax(0,1fr)}}
@media(max-width:650px){.utilization-report{padding:8px}.page-heading{flex-direction:column}.header-actions{flex-wrap:wrap}.filter-grid{grid-template-columns:1fr}.query-actions{flex-wrap:wrap}.dev-setting,.chamber-hint{width:100%;margin:0 0 4px}.chart-toolbar{align-items:flex-start;flex-direction:column}.selection-panel{align-items:flex-start;flex-direction:column}.selection-times{margin-left:0}.axis-row,.gantt-row{grid-template-columns:165px minmax(0,1fr)}}
</style>

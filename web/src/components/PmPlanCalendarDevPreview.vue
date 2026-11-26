<script setup lang="ts">
import { computed, ref } from 'vue'

interface PlanRow {
  serialNo: string; pmeqpSopNo: string; pmSerialNo: number; equipmentNo: string; equipmentType: string
  frequencyNo: string; frequencyName: string; sopNo: string; sopName: string; estimatedDate: string
  executeState: number; creator: string; createDate: string
}
interface PlanState { plans?: Array<Record<string, unknown>>; records?: Array<Record<string, unknown>> }
interface CalendarCell { date: string; day: number; inMonth: boolean }
type CalendarView = 'day' | 'week' | 'month'
type FilterList = 'available' | 'selected'
type PrintStyle = CalendarView | 'memo'
const printStyleNames: Record<PrintStyle, string> = { day: 'Daily Style', week: 'Weekly Style', month: 'Monthly Style', memo: 'Memo Style' }

const props = withDefaults(defineProps<{
  developmentPreview: boolean
  initialPlans?: object[]
  embedded?: boolean
}>(), { embedded: false })
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const chamberPattern = /-CH\d+$/i
const equipmentTypeByNo: Record<string, string> = { 'CNC-02': 'CNC-200', 'CNC-02-CH1': 'CNC-200', 'PUMP-07': 'PUMP-120', 'OVEN-05': 'OVEN-50' }
const fallbackRows: PlanRow[] = [
  { serialNo: 'DEV-PLAN-0001', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 1, equipmentNo: 'CNC-02', equipmentType: 'CNC-200', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', estimatedDate: '2026-11-15', executeState: 1, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
  { serialNo: 'DEV-PLAN-0002', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 2, equipmentNo: 'CNC-02', equipmentType: 'CNC-200', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', estimatedDate: '2026-12-15', executeState: 0, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
  { serialNo: 'DEV-PLAN-0003', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 1, equipmentNo: 'PUMP-07', equipmentType: 'PUMP-120', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', estimatedDate: '2026-11-30', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15' },
  { serialNo: 'DEV-PLAN-0004', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 2, equipmentNo: 'CNC-02-CH1', equipmentType: 'CNC-200', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', estimatedDate: '2026-11-22', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15' },
]
const frequencyNameFallback: Record<string, string> = { 'PM-MONTHLY-15': '月中設備校驗', 'PM-WEEKLY-01': '每週保養', 'PM-DAILY-01': '每日點檢', 'PM-YEARLY-06': '年度保養' }
const sopNameFallback: Record<string, string> = { 'SOP-CAL-02': '感測器校正程序', 'SOP-LUBE-03': '主軸潤滑保養程序' }
const todayKey = localDateKey(new Date())

function normalizeDate(value: unknown) {
  const text = String(value ?? '').trim()
  if (!text) return ''
  const normalized = text.replaceAll('/', '-').slice(0, 10)
  return /^\d{4}-\d{2}-\d{2}$/.test(normalized) ? normalized : ''
}
function mapRows(rawRows: object[], records: Array<Record<string, unknown>> = []): PlanRow[] {
  const recordByNo = new Map(records.map((row) => [String(row.pmeqpSopNo ?? ''), row]))
  return rawRows.map((value) => {
    const raw = value as Record<string, unknown>
    const parent = recordByNo.get(String(raw.pmeqpSopNo ?? '')) ?? {}
    const equipmentNo = String(raw.equipmentNo ?? '')
    const frequencyNo = String(raw.frequencyNo ?? '')
    const sopNo = String(raw.sopNo ?? parent.sopNo ?? '')
    return {
      serialNo: String(raw.serialNo ?? ''), pmeqpSopNo: String(raw.pmeqpSopNo ?? ''), pmSerialNo: Number(raw.pmSerialNo ?? 0),
      equipmentNo, equipmentType: String(raw.equipmentType ?? parent.equipmentType ?? equipmentTypeByNo[equipmentNo] ?? ''),
      frequencyNo, frequencyName: String(raw.frequencyName ?? parent.frequencyName ?? frequencyNameFallback[frequencyNo] ?? frequencyNo),
      sopNo, sopName: String(raw.sopName ?? parent.sopName ?? sopNameFallback[sopNo] ?? sopNo),
      estimatedDate: normalizeDate(raw.estimatedDate ?? raw.pmEstDateTime), executeState: Number(raw.executeState ?? 0),
      creator: String(raw.creator ?? ''), createDate: String(raw.createDate ?? ''),
    }
  }).filter((row) => row.serialNo && row.equipmentNo && row.frequencyNo && row.estimatedDate)
}
function readSavedRows() {
  try {
    const saved = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as PlanState | null
    return Array.isArray(saved?.plans) ? mapRows(saved.plans, saved.records ?? []) : fallbackRows
  } catch { return fallbackRows }
}
function localDateKey(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
function calendarCells(monthKey: string): CalendarCell[] {
  const [year, month] = monthKey.split('-').map(Number)
  const first = new Date(year, month - 1, 1)
  const start = new Date(year, month - 1, 1 - first.getDay())
  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index)
    return { date: localDateKey(date), day: date.getDate(), inMonth: date.getMonth() === month - 1 }
  })
}
function monthKey(value: string) { return value.slice(0, 7) }
function addMonths(dateKey: string, offset: number) {
  const [year, month, day] = dateKey.split('-').map(Number)
  const target = new Date(year, month - 1 + offset, 1)
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate()
  target.setDate(Math.min(day, lastDay))
  return localDateKey(target)
}
function addDays(dateKey: string, offset: number) {
  const [year, month, day] = dateKey.split('-').map(Number)
  const date = new Date(year, month - 1, day + offset)
  return localDateKey(date)
}

const allRows = ref(readSavedRows())
const incomingRows = props.initialPlans ? mapRows(props.initialPlans) : []
const queryRows = ref<PlanRow[]>(incomingRows)
const queried = ref(Boolean(props.initialPlans?.length))
const activeDate = ref(incomingRows[0]?.estimatedDate ?? todayKey)
const view = ref<CalendarView>('month')
const views: Array<{ key: CalendarView; label: string }> = [{ key: 'day', label: 'Day View' }, { key: 'week', label: 'Week View' }, { key: 'month', label: 'Month View' }]
const equipmentType = ref('')
const frequencyNo = ref('')
const availableSelection = ref<string[]>([])
const selectedSelection = ref<string[]>([])
const selectedEquipment = ref<string[]>([])
const lookupKind = ref<'equipmentType' | 'frequencyNo' | ''>('')
const selectedAppointment = ref<PlanRow | null>(null)
const printPreviewOpen = ref(false)
const printMenuOpen = ref(false)
const printStyle = ref<PrintStyle>('month')
const printStyleLabel = computed(() => printStyleNames[printStyle.value])
const notice = ref(queried.value ? `已載入呼叫端傳入的 ${queryRows.value.length} 筆保養計畫。` : '')
const chamberManage = false

const equipmentTypeOptions = computed(() => [...new Set(allRows.value.map((row) => row.equipmentType).filter(Boolean))].sort())
const frequencyOptions = computed(() => [...new Set(allRows.value.map((row) => row.frequencyNo).filter(Boolean))].sort())
const equipmentOptions = computed(() => [...new Set(allRows.value
  .filter((row) => !equipmentType.value || row.equipmentType === equipmentType.value)
  .map((row) => row.equipmentNo)
  .filter((value) => value && (chamberManage || !chamberPattern.test(value))))].sort())
const visibleEquipmentOptions = computed(() => equipmentOptions.value.filter((value) => !selectedEquipment.value.includes(value)))
const lookupOptions = computed(() => lookupKind.value === 'equipmentType' ? equipmentTypeOptions.value : frequencyOptions.value)
const miniMonths = computed(() => [0, 1].map((offset) => {
  const key = monthKey(addMonths(`${monthKey(activeDate.value)}-01`, offset))
  return { key, label: key.replace('-', '/'), cells: calendarCells(key) }
}))
const currentMonthCells = computed(() => calendarCells(monthKey(activeDate.value)))
const weekCells = computed(() => {
  const [year, month, day] = activeDate.value.split('-').map(Number)
  const start = new Date(year, month - 1, day)
  start.setDate(start.getDate() - start.getDay())
  return Array.from({ length: 7 }, (_, index) => {
    const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index)
    return { date: localDateKey(date), day: date.getDate(), inMonth: date.getMonth() === start.getMonth() }
  })
})
const periodLabel = computed(() => {
  if (view.value === 'day') return activeDate.value.replaceAll('-', '/')
  if (view.value === 'month') return monthKey(activeDate.value).replace('-', '/')
  return `${weekCells.value[0]?.date.replaceAll('-', '/')} – ${weekCells.value[6]?.date.replaceAll('-', '/')}`
})
const activeDayRows = computed(() => rowsForDate(activeDate.value))
const selectedRowsForPrint = computed(() => queryRows.value.filter((row) => {
  if (printStyle.value === 'day') return row.estimatedDate === activeDate.value
  if (printStyle.value === 'week') return weekCells.value.some((cell) => cell.date === row.estimatedDate)
  if (printStyle.value === 'month') return monthKey(row.estimatedDate) === monthKey(activeDate.value)
  return true
}))

function rowsForDate(date: string) { return queryRows.value.filter((row) => row.estimatedDate === date).sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo) || a.pmSerialNo - b.pmSerialNo) }
function moveEquipment(direction: FilterList) {
  if (direction === 'selected') {
    selectedEquipment.value = [...new Set([...selectedEquipment.value, ...availableSelection.value])].sort()
    availableSelection.value = []
  } else {
    const moving = new Set(selectedSelection.value)
    selectedEquipment.value = selectedEquipment.value.filter((value) => !moving.has(value))
    selectedSelection.value = []
  }
}
function chooseLookup(value: string) {
  if (lookupKind.value === 'equipmentType') {
    equipmentType.value = value
    selectedEquipment.value = []
    availableSelection.value = []
    selectedSelection.value = []
  } else if (lookupKind.value === 'frequencyNo') frequencyNo.value = value
  lookupKind.value = ''
}
function clearFilters() {
  equipmentType.value = ''
  frequencyNo.value = ''
  selectedEquipment.value = []
  availableSelection.value = []
  selectedSelection.value = []
  notice.value = '查詢條件已清除；按 Query 重新載入日曆。'
}
function queryPlans() {
  const equipmentSet = new Set(selectedEquipment.value)
  const matches = allRows.value.filter((row) => (!equipmentType.value || row.equipmentType === equipmentType.value)
    && (!frequencyNo.value || row.frequencyNo === frequencyNo.value)
    && (!equipmentSet.size || equipmentSet.has(row.equipmentNo))
    && (equipmentType.value || chamberManage || !chamberPattern.test(row.equipmentNo)))
    .sort((a, b) => a.estimatedDate.localeCompare(b.estimatedDate) || a.equipmentNo.localeCompare(b.equipmentNo) || a.pmSerialNo - b.pmSerialNo)
  queried.value = true
  if (matches.length) {
    queryRows.value = matches
    activeDate.value = queryRows.value[0].estimatedDate
    notice.value = `查詢完成：${queryRows.value.length} 筆保養計畫已載入日曆。`
  } else notice.value = '找不到符合條件的保養計畫；保留目前日曆資料供比對。'
}
function shiftPeriod(direction: number) {
  activeDate.value = view.value === 'day' ? addDays(activeDate.value, direction)
    : view.value === 'week' ? addDays(activeDate.value, direction * 7) : addMonths(activeDate.value, direction)
}
function selectDate(date: string) {
  activeDate.value = date
}
function eventSubject(row: PlanRow) { return `${row.equipmentNo}-${row.frequencyName}` }
function eventDescription(row: PlanRow) { return `${row.frequencyNo}:${row.frequencyName}-${row.sopName}:${row.sopName}` }
function openPrint(style: PrintStyle = view.value) {
  printStyle.value = style
  printMenuOpen.value = false
  printPreviewOpen.value = true
}
function printNow() {
  printPreviewOpen.value = false
  window.print()
}
function dateLabel(value: string) { return value.replaceAll('-', '/') }

</script>

<template>
  <section class="pm-plan-calendar" :class="{ embedded }" aria-label="保養日曆設定 DEV 預覽">
    <header class="page-header">
      <div><p>PM CALENDAR / PM502</p><h1>保養日曆設定</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">{{ embedded ? '關閉日曆' : '關閉' }}</button></div>
    </header>

    <div class="calendar-workspace">
      <aside class="filter-panel">
        <label class="field-label" for="equipment-type">Equipment Type</label>
        <div class="lookup-row"><input id="equipment-type" v-model="equipmentType" list="pm-eqp-type-options" autocomplete="off"><datalist id="pm-eqp-type-options"><option v-for="option in equipmentTypeOptions" :key="option" :value="option" /></datalist><button type="button" class="secondary lookup-button" aria-label="查找設備類別" @click="lookupKind = 'equipmentType'">...</button></div>
        <label class="field-label" for="frequency-no">Frequency No</label>
        <div class="lookup-row"><input id="frequency-no" v-model="frequencyNo" list="pm-frequency-options" autocomplete="off"><datalist id="pm-frequency-options"><option v-for="option in frequencyOptions" :key="option" :value="option" /></datalist><button type="button" class="secondary lookup-button" aria-label="查找保養頻率" @click="lookupKind = 'frequencyNo'">...</button></div>
        <div class="list-heading"><span>Equipment No</span><small>雙擊或用箭頭移動多選設備</small></div>
        <div class="equipment-lists">
          <select v-model="availableSelection" multiple aria-label="未選設備" @dblclick="moveEquipment('selected')"><option v-for="item in visibleEquipmentOptions" :key="item" :value="item">{{ item }}</option></select>
          <div class="list-arrows"><button type="button" class="secondary" aria-label="選取設備" @click="moveEquipment('selected')">›</button><button type="button" class="secondary" aria-label="取消設備選取" @click="moveEquipment('available')">‹</button></div>
          <select v-model="selectedSelection" multiple aria-label="已選設備" @dblclick="moveEquipment('available')"><option v-for="item in selectedEquipment" :key="item" :value="item">{{ item }}</option></select>
        </div>
        <div class="filter-actions"><button type="button" class="secondary" @click="clearFilters">Clear</button><button type="button" @click="queryPlans">Query</button></div>
        <p class="filter-note">未選設備代表不限制設備。未輸入 Equipment Type 時，ChamberManage=0 模擬排除子設備。</p>
      </aside>

      <main class="calendar-main">
        <header class="calendar-toolbar"><div class="period-tools"><button type="button" class="secondary" aria-label="上一期間" @click="shiftPeriod(-1)">‹</button><button type="button" class="secondary" @click="activeDate = todayKey">Today</button><button type="button" class="secondary" aria-label="下一期間" @click="shiftPeriod(1)">›</button><h2>{{ periodLabel }}</h2></div><div class="view-tabs"><button v-for="item in views" :key="item.key" type="button" :class="{ active: view === item.key }" @click="view = item.key">{{ item.label }}</button></div><div class="print-tools"><button type="button" class="secondary" @click="printMenuOpen = !printMenuOpen">列印／預覽 ▾</button><div v-if="printMenuOpen" class="print-menu"><button type="button" @click="openPrint(view)">Page Setup</button><button type="button" @click="openPrint('day')">Daily Style</button><button type="button" @click="openPrint('week')">Weekly Style</button><button type="button" @click="openPrint('month')">Monthly Style</button><button type="button" @click="openPrint('memo')">Memo Style</button><button type="button" @click="openPrint(view)">Print Preview</button><button type="button" @click="printNow">Print</button></div></div></header>
        <div class="calendar-content">
          <section v-if="view === 'day'" class="day-view"><h3>All day · {{ dateLabel(activeDate) }}</h3><button v-for="row in activeDayRows" :key="row.serialNo" type="button" class="appointment-row" @click="selectedAppointment = row"><strong>{{ eventSubject(row) }}</strong><span>{{ row.frequencyNo }} · {{ row.sopName }}</span></button><p v-if="!activeDayRows.length" class="empty">{{ queried ? '當日沒有保養計畫。' : '請先設定條件並按 Query 載入保養計畫。' }}</p></section>
          <section v-else-if="view === 'week'" class="week-view"><div v-for="cell in weekCells" :key="cell.date" class="week-day" :class="{ selected: activeDate === cell.date }" @click="selectDate(cell.date)"><button type="button" class="day-heading" @click.stop="selectDate(cell.date)">{{ ['日','一','二','三','四','五','六'][new Date(`${cell.date}T00:00:00`).getDay()] }} {{ cell.day }}</button><button v-for="row in rowsForDate(cell.date)" :key="row.serialNo" type="button" class="appointment-chip" @click.stop="selectedAppointment = row">{{ eventSubject(row) }}</button></div></section>
          <section v-else class="month-view"><div v-for="name in ['日','一','二','三','四','五','六']" :key="name" class="weekday">{{ name }}</div><div v-for="cell in currentMonthCells" :key="cell.date" class="month-cell" :class="{ outside: !cell.inMonth, selected: activeDate === cell.date }" @click="selectDate(cell.date)"><button type="button" class="day-heading" @click.stop="selectDate(cell.date)">{{ cell.day }}</button><button v-for="row in rowsForDate(cell.date)" :key="row.serialNo" type="button" class="appointment-chip" @click.stop="selectedAppointment = row">{{ eventSubject(row) }}</button></div></section>
          <p v-if="notice" class="notice" role="status">{{ notice }}</p>
        </div>
      </main>

      <aside class="mini-panel"><h2>Calendar</h2><section v-for="month in miniMonths" :key="month.key" class="mini-month"><h3>{{ month.label }}</h3><div class="mini-grid"><span v-for="name in ['日','一','二','三','四','五','六']" :key="`${month.key}-${name}`" class="mini-weekday">{{ name }}</span><button v-for="cell in month.cells" :key="cell.date" type="button" :class="{ outside: !cell.inMonth, selected: activeDate === cell.date, planned: rowsForDate(cell.date).length > 0 }" @click="selectDate(cell.date)">{{ cell.day }}</button></div></section><p class="print-help">日曆右鍵選單及列印／預覽按鈕可檢視保養計畫。</p><p class="source-note">{{ queried ? `${queryRows.length} 筆日曆假資料` : '日曆尚未查詢' }}<br>DEV localStorage：{{ storageKey }}</p></aside>
    </div>

    <div v-if="lookupKind" class="modal-backdrop" @click.self="lookupKind = ''"><section class="dialog lookup-dialog" role="dialog" aria-modal="true"><header><div><p>MES LOOKUP</p><h2>{{ lookupKind === 'equipmentType' ? '選取 Equipment Type' : '選取 Frequency No' }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉 lookup" @click="lookupKind = ''">×</button></header><button v-for="option in lookupOptions" :key="option" type="button" class="lookup-option" @click="chooseLookup(option)">{{ option }}</button><p v-if="lookupOptions.length === 0" class="empty">沒有 DEV 樣例選項。</p><footer><button type="button" class="secondary" @click="lookupKind = ''">取消</button></footer></section></div>

    <div v-if="selectedAppointment" class="modal-backdrop" @click.self="selectedAppointment = null"><section class="dialog appointment-dialog" role="dialog" aria-modal="true" aria-labelledby="appointment-title"><header><div><p>PM PLAN · ALL-DAY APPOINTMENT</p><h2 id="appointment-title">{{ eventSubject(selectedAppointment) }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉預約明細" @click="selectedAppointment = null">×</button></header><dl><dt>PM Est Date</dt><dd>{{ dateLabel(selectedAppointment.estimatedDate) }}</dd><dt>Frequency No</dt><dd>{{ selectedAppointment.frequencyNo }} · {{ selectedAppointment.frequencyName }}</dd><dt>SOP</dt><dd>{{ selectedAppointment.sopNo }} · {{ selectedAppointment.sopName }}</dd><dt>Description</dt><dd>{{ eventDescription(selectedAppointment) }}</dd></dl><p class="form-note">WinForms 詳細對話框為唯讀；此 DEV 日曆不修改保養計畫或儲存拖曳變更。</p><footer><button type="button" @click="selectedAppointment = null">關閉</button></footer></section></div>

    <div v-if="printPreviewOpen" class="modal-backdrop print-backdrop" @click.self="printPreviewOpen = false"><section class="dialog print-dialog" role="dialog" aria-modal="true"><header><div><p>PRINT PREVIEW</p><h2>{{ printStyleLabel }} · {{ periodLabel }}</h2></div><button type="button" class="secondary close-x" aria-label="關閉列印預覽" @click="printPreviewOpen = false">×</button></header><p class="form-note">Page Setup / {{ printStyleLabel }} — 僅預覽目前 DEV 日曆資料。</p><div class="table-wrap"><table><thead><tr><th>日期</th><th>設備／頻率</th><th>保養頻率</th><th>SOP</th></tr></thead><tbody><tr v-for="row in selectedRowsForPrint" :key="row.serialNo"><td>{{ dateLabel(row.estimatedDate) }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.frequencyName }}</td><td>{{ row.sopName }}</td></tr><tr v-if="!selectedRowsForPrint.length"><td colspan="4" class="empty">沒有可列印的計畫。</td></tr></tbody></table></div><footer><button type="button" class="secondary" @click="printPreviewOpen = false">取消</button><button type="button" @click="printNow">列印</button></footer></section></div>
  </section>
</template>

<style scoped>
.pm-plan-calendar { height:100%; min-height:0; display:flex; flex-direction:column; gap:8px; padding:10px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.4 "Segoe UI","Microsoft JhengHei",sans-serif; }.pm-plan-calendar.embedded { height:min(860px,calc(100vh - 40px)); width:min(1420px,calc(100vw - 32px)); }.page-header,.header-actions,.calendar-toolbar,.period-tools,.view-tabs,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }.page-header,.calendar-toolbar,.dialog header,.dialog footer { justify-content:space-between; gap:8px; }.page-header { border-bottom:1px solid #d6d2c6; padding-bottom:7px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:18px; font-weight:600; }.header-actions { gap:7px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:27px; padding:4px 9px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.45; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }
.calendar-workspace { display:grid; grid-template-columns:218px minmax(0,1fr) 186px; gap:7px; flex:1; min-height:0; }.filter-panel,.calendar-main,.mini-panel { min-height:0; border:1px solid #d0ccbf; background:#fffef9; }.filter-panel { display:flex; flex-direction:column; gap:5px; padding:8px; background:#eeece4; }.field-label { color:#514f45; font-weight:600; }.lookup-row { display:grid; grid-template-columns:minmax(0,1fr) 31px; gap:4px; }.lookup-row input { min-width:0; height:28px; padding:4px 6px; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.lookup-button { padding:2px; }.list-heading { display:flex; flex-direction:column; gap:1px; margin-top:4px; color:#514f45; font-weight:600; }.list-heading small { color:#777366; font-size:9px; font-weight:400; }.equipment-lists { display:grid; grid-template-columns:minmax(0,1fr) 28px minmax(0,1fr); gap:4px; height:135px; }.equipment-lists select { width:100%; min-width:0; padding:4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.list-arrows { display:flex; flex-direction:column; align-items:stretch; justify-content:center; gap:5px; }.list-arrows button { padding:2px; font-size:16px; }.filter-actions { display:flex; justify-content:space-between; gap:8px; margin-top:5px; }.filter-note { margin:auto 0 0; color:#777366; font-size:10px; }
.calendar-main { display:flex; flex-direction:column; overflow:hidden; }.calendar-toolbar { position:relative; flex-wrap:wrap; padding:6px; border-bottom:1px solid #d6d2c6; background:#f3f1e9; }.period-tools { gap:5px; }.period-tools h2 { min-width:150px; margin:0 0 0 5px; text-align:center; font-size:15px; }.view-tabs { gap:3px; }.view-tabs button { border-color:#bab6a8; background:#fffef9; color:#333126; }.view-tabs button.active { border-color:#454237; background:#454237; color:#fff; }.print-tools { position:relative; }.print-menu { position:absolute; right:0; top:31px; z-index:10; display:grid; min-width:150px; padding:4px; border:1px solid #b8b4a7; background:#fffef9; box-shadow:0 5px 16px #0002; }.print-menu button { border:0; background:transparent; color:#333126; text-align:left; }.print-menu button:hover,.lookup-option:hover { background:#eeece4; }.calendar-content { flex:1; min-height:0; display:flex; flex-direction:column; overflow:auto; }.month-view { min-height:0; flex:1; display:grid; grid-template-columns:repeat(7,minmax(0,1fr)); grid-template-rows:25px repeat(6,minmax(50px,1fr)); gap:1px; background:#ddd9ce; }.weekday { display:grid; place-items:center; background:#e9e6dc; color:#555246; font-weight:600; }.month-cell { min-width:0; overflow:hidden; padding:2px; background:#fffef9; }.month-cell.outside { background:#f4f2ec; color:#aaa79b; }.month-cell.selected { outline:2px solid #607d8b; outline-offset:-2px; }.day-heading { min-width:24px; min-height:22px; padding:1px 4px; border:0; background:transparent; color:#454237; font-weight:600; text-align:left; }.appointment-chip { display:block; width:100%; min-height:20px; overflow:hidden; margin:2px 0; padding:2px 4px; border:0; border-radius:2px; background:#c95735; color:#fff; font-size:10px; text-align:left; text-overflow:ellipsis; white-space:nowrap; }.week-view { flex:1; min-height:0; display:grid; grid-template-columns:repeat(7,minmax(0,1fr)); gap:1px; background:#ddd9ce; }.week-day { min-width:0; overflow:auto; background:#fffef9; }.week-day.selected { box-shadow:inset 0 0 0 2px #607d8b; }.week-day .day-heading { display:block; width:100%; padding:8px 5px; border-bottom:1px solid #e0ddd4; }.day-view { flex:1; overflow:auto; padding:10px; }.day-view h3 { margin:0 0 8px; padding:6px; border-bottom:1px solid #d6d2c6; font-size:13px; }.appointment-row { display:flex; width:100%; flex-direction:column; gap:3px; margin:4px 0; padding:9px; border:1px solid #d0ccbf; background:#f7f5ee; color:#333126; text-align:left; }.appointment-row strong { color:#a3482f; }.appointment-row span { color:#777366; }.empty { grid-column:1/-1; align-self:center; margin:0; padding:14px; color:#777366; text-align:center; }.notice { margin:0; padding:6px 8px; background:#eeece4; color:#555246; }
.mini-panel { display:flex; flex-direction:column; gap:7px; overflow:auto; padding:7px; }.mini-panel > h2 { margin:0; padding:3px 5px 7px; border-bottom:1px solid #d6d2c6; font-size:15px; }.mini-month h3 { margin:0 0 3px; text-align:center; font-size:11px; }.mini-grid { display:grid; grid-template-columns:repeat(7,minmax(0,1fr)); gap:1px; }.mini-weekday,.mini-grid button { display:grid; min-width:0; min-height:19px; place-items:center; padding:0; border:0; border-radius:10px; background:transparent; color:#555246; font-size:9px; }.mini-weekday { color:#999588; }.mini-grid button.outside { color:#c4c0b6; }.mini-grid button.selected { background:#607d8b; color:#fff; }.mini-grid button.planned:not(.selected) { box-shadow:inset 0 0 0 1px #c95735; }.print-help,.source-note { margin:2px 0 0; padding:7px; background:#eeece4; color:#777366; font-size:10px; }.source-note { margin-top:auto; overflow-wrap:anywhere; }.page-footer { justify-content:space-between; color:#8a877c; font-size:9px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.dialog { width:min(520px,100%); max-height:calc(100vh - 32px); display:flex; flex-direction:column; gap:9px; overflow:auto; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.lookup-dialog h2,.dialog h2 { margin:2px 0 0; font-size:16px; }.close-x { min-width:30px; font-size:18px; }.lookup-option { min-height:32px; border-color:#ddd9ce; background:#fffef9; color:#333126; text-align:left; }.dialog footer { justify-content:flex-end; }.appointment-dialog dl { display:grid; grid-template-columns:125px 1fr; gap:6px 10px; margin:0; padding:9px; background:#fffef9; }.appointment-dialog dt { color:#777366; }.appointment-dialog dd { margin:0; }.form-note { margin:0; padding:7px 9px; background:#eeece4; color:#777366; font-size:10px; }.print-dialog { width:min(760px,100%); }.table-wrap { min-height:0; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:6px 8px; border-bottom:1px solid #e7e4db; text-align:left; }th { background:#e9e6dc; color:#555246; }
@media(max-width:1050px) { .calendar-workspace { grid-template-columns:205px minmax(0,1fr); }.mini-panel { display:none; }.calendar-toolbar { gap:5px; }.period-tools h2 { min-width:120px; } }
@media(max-width:700px) { .pm-plan-calendar { padding:6px; }.pm-plan-calendar.embedded { width:calc(100vw - 16px); height:calc(100vh - 16px); }.calendar-workspace { grid-template-columns:1fr; grid-template-rows:auto minmax(300px,1fr); }.filter-panel { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); }.filter-panel .list-heading,.equipment-lists,.filter-actions,.filter-note { grid-column:1/-1; }.equipment-lists { height:96px; }.calendar-toolbar { align-items:flex-start; }.period-tools { width:100%; }.view-tabs { flex:1; }.view-tabs button { flex:1; padding:4px 3px; font-size:10px; }.print-tools { margin-left:auto; }.month-view { grid-template-rows:22px repeat(6,minmax(43px,1fr)); }.appointment-chip { font-size:8px; }.page-header p { font-size:9px; } }
@media print { .page-header,.filter-panel,.mini-panel,.calendar-toolbar,.page-footer,.notice,.modal-backdrop { display:none !important; }.pm-plan-calendar,.calendar-workspace,.calendar-main,.calendar-content { display:block !important; height:auto !important; width:auto !important; overflow:visible !important; padding:0 !important; border:0 !important; }.month-view { min-height:90vh; }.appointment-chip { color:#222; background:#eee; border:1px solid #aaa; } }
</style>

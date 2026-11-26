<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface BreakdownEvent {
  equipmentNo: string
  equipmentType: string
  breakdownNo: string
  breakdownCause: string
  breakdownStartTime: string
  repairEndTime: string
}
interface BreakdownCount {
  breakdownNo: string
  breakdownCause: string
  count: number
}
interface EventState { version: number; events: BreakdownEvent[] }
type ChartType = 'ColumnChart' | 'BarChart' | 'LineChart' | 'PieChart' | 'AreaChart'
type Direction = 'right' | 'left'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.breakdown-statistics.v1'
const defaultEvents: BreakdownEvent[] = [
  { equipmentNo: 'CNC-02', equipmentType: 'CNC', breakdownNo: 'BD-001', breakdownCause: 'Sensor failure', breakdownStartTime: '2026-10-02 09:10:00', repairEndTime: '2026-10-02 10:42:00' },
  { equipmentNo: 'CNC-03', equipmentType: 'CNC', breakdownNo: 'BD-001', breakdownCause: 'Sensor failure', breakdownStartTime: '2026-10-06 14:25:00', repairEndTime: '2026-10-06 16:10:00' },
  { equipmentNo: 'CNC-02', equipmentType: 'CNC', breakdownNo: 'BD-001', breakdownCause: 'Sensor failure', breakdownStartTime: '2026-10-18 08:45:00', repairEndTime: '2026-10-18 10:20:00' },
  { equipmentNo: 'CNC-03', equipmentType: 'CNC', breakdownNo: 'BD-001', breakdownCause: 'Sensor failure', breakdownStartTime: '2026-11-04 11:05:00', repairEndTime: '2026-11-04 13:15:00' },
  { equipmentNo: 'CNC-02', equipmentType: 'CNC', breakdownNo: 'BD-002', breakdownCause: 'Electrical fault', breakdownStartTime: '2026-09-29 12:30:00', repairEndTime: '2026-09-29 14:05:00' },
  { equipmentNo: 'CNC-03', equipmentType: 'CNC', breakdownNo: 'BD-002', breakdownCause: 'Electrical fault', breakdownStartTime: '2026-10-20 16:50:00', repairEndTime: '2026-10-20 18:05:00' },
  { equipmentNo: 'CNC-02', equipmentType: 'CNC', breakdownNo: 'BD-002', breakdownCause: 'Electrical fault', breakdownStartTime: '2026-11-12 07:22:00', repairEndTime: '2026-11-12 09:06:00' },
  { equipmentNo: 'PUMP-07', equipmentType: 'PUMP', breakdownNo: 'BD-003', breakdownCause: 'Hydraulic leak', breakdownStartTime: '2026-10-04 08:00:00', repairEndTime: '2026-10-04 11:34:00' },
  { equipmentNo: 'PUMP-08', equipmentType: 'PUMP', breakdownNo: 'BD-003', breakdownCause: 'Hydraulic leak', breakdownStartTime: '2026-10-28 15:10:00', repairEndTime: '2026-10-28 17:30:00' },
  { equipmentNo: 'PUMP-07', equipmentType: 'PUMP', breakdownNo: 'BD-003', breakdownCause: 'Hydraulic leak', breakdownStartTime: '2026-11-16 09:42:00', repairEndTime: '2026-11-16 12:55:00' },
  { equipmentNo: 'PUMP-08', equipmentType: 'PUMP', breakdownNo: 'BD-004', breakdownCause: 'Lubrication issue', breakdownStartTime: '2026-10-09 13:17:00', repairEndTime: '2026-10-09 15:02:00' },
  { equipmentNo: 'PUMP-07', equipmentType: 'PUMP', breakdownNo: 'BD-004', breakdownCause: 'Lubrication issue', breakdownStartTime: '2026-11-20 10:25:00', repairEndTime: '2026-11-20 11:40:00' },
  { equipmentNo: 'PRESS-04', equipmentType: 'PRESS', breakdownNo: 'BD-005', breakdownCause: 'Safety interlock', breakdownStartTime: '2026-10-13 07:58:00', repairEndTime: '2026-10-13 10:12:00' },
  { equipmentNo: 'PRESS-04', equipmentType: 'PRESS', breakdownNo: 'BD-005', breakdownCause: 'Safety interlock', breakdownStartTime: '2026-11-02 15:40:00', repairEndTime: '2026-11-02 17:05:00' },
  { equipmentNo: 'PRESS-04-CH1', equipmentType: 'PRESS', breakdownNo: 'BD-005', breakdownCause: 'Safety interlock', breakdownStartTime: '2026-10-29 12:20:00', repairEndTime: '2026-10-29 13:50:00' },
  { equipmentNo: 'ROBOT-01', equipmentType: 'ROBOT', breakdownNo: 'BD-006', breakdownCause: 'Axis position error', breakdownStartTime: '2026-11-07 16:11:00', repairEndTime: '2026-11-07 18:20:00' },
  { equipmentNo: 'ROBOT-01', equipmentType: 'ROBOT', breakdownNo: 'BD-006', breakdownCause: 'Axis position error', breakdownStartTime: '2026-11-18 09:03:00', repairEndTime: '2026-11-18 10:44:00' },
  { equipmentNo: 'CNC-02-CH1', equipmentType: 'CNC', breakdownNo: 'BD-007', breakdownCause: 'Chamber pressure alarm', breakdownStartTime: '2026-10-23 11:25:00', repairEndTime: '2026-10-23 12:30:00' },
]
const equipmentBasis = [
  { equipmentNo: 'CNC-02', equipmentType: 'CNC' }, { equipmentNo: 'CNC-03', equipmentType: 'CNC' },
  { equipmentNo: 'CNC-02-CH1', equipmentType: 'CNC' }, { equipmentNo: 'PUMP-07', equipmentType: 'PUMP' },
  { equipmentNo: 'PUMP-08', equipmentType: 'PUMP' }, { equipmentNo: 'PRESS-04', equipmentType: 'PRESS' },
  { equipmentNo: 'PRESS-04-CH1', equipmentType: 'PRESS' }, { equipmentNo: 'ROBOT-01', equipmentType: 'ROBOT' },
]
const chartTypes: Array<{ value: ChartType; label: string }> = [
  { value: 'ColumnChart', label: 'Column' }, { value: 'BarChart', label: 'Bar' }, { value: 'LineChart', label: 'Line' },
  { value: 'PieChart', label: 'Pie' }, { value: 'AreaChart', label: 'Area' },
]
const chamberManage = false
const events = ref(readEvents())
const equipmentType = ref('')
const poolSelection = ref<string[]>([])
const selectedEquipment = ref<string[]>([])
const breakdownFromEnabled = ref(true)
const breakdownToEnabled = ref(true)
const repairFromEnabled = ref(true)
const repairToEnabled = ref(true)
const defaultRanges = getDefaultRanges()
const breakdownFrom = ref(defaultRanges.from)
const breakdownTo = ref(defaultRanges.to)
const repairFrom = ref(defaultRanges.from)
const repairTo = ref(defaultRanges.to)
const chartType = ref<ChartType>('ColumnChart')
const resultRows = ref<BreakdownCount[]>([])
const queried = ref(false)
const notice = ref('設定條件後按 Query。')

const equipmentTypes = computed(() => [...new Set(equipmentBasis.map((row) => row.equipmentType))].sort((a, b) => a.localeCompare(b)))
const equipmentOptions = computed(() => equipmentBasis.filter((row) => row.equipmentType === equipmentType.value && (chamberManage || !isChamber(row.equipmentNo))).map((row) => row.equipmentNo).sort((a, b) => a.localeCompare(b)))
const maxCount = computed(() => Math.max(1, ...resultRows.value.map((row) => row.count)))
const totalCount = computed(() => resultRows.value.reduce((sum, row) => sum + row.count, 0))
const chartPoints = computed(() => resultRows.value.map((row, index) => {
  const x = resultRows.value.length < 2 ? 380 : 68 + index * (624 / (resultRows.value.length - 1))
  const y = 284 - (row.count / maxCount.value) * 220
  return { ...row, x, y }
}))
const linePoints = computed(() => chartPoints.value.map((point) => `${point.x},${point.y}`).join(' '))
const areaPath = computed(() => chartPoints.value.length ? `M ${chartPoints.value[0].x} 284 L ${chartPoints.value.map((point) => `${point.x} ${point.y}`).join(' L ')} L ${chartPoints.value[chartPoints.value.length - 1].x} 284 Z` : '')
const verticalBars = computed(() => resultRows.value.map((row, index) => {
  const slot = 660 / Math.max(resultRows.value.length, 1)
  const height = (row.count / maxCount.value) * 220
  return { ...row, x: 60 + index * slot + slot * 0.2, width: slot * 0.6, y: 284 - height, height }
}))
const pieGradient = computed(() => {
  let cursor = 0
  const colors = ['#385f79', '#b26f48', '#718755', '#88719a', '#ba9850', '#4b9992', '#a45c6a']
  const stops = resultRows.value.map((row, index) => {
    const start = cursor
    cursor += totalCount.value ? row.count / totalCount.value * 100 : 0
    return `${colors[index % colors.length]} ${start}% ${cursor}%`
  })
  return `conic-gradient(${stops.join(', ')})`
})

function readEvents(): BreakdownEvent[] {
  try {
    const saved = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as EventState | null
    if (saved?.version === 1 && Array.isArray(saved.events)) return saved.events
    window.localStorage.setItem(storageKey, JSON.stringify({ version: 1, events: defaultEvents } satisfies EventState))
  } catch { /* Keep the DEV fixture available when browser storage is blocked. */ }
  return defaultEvents
}
function getDefaultRanges() {
  const now = new Date()
  const toInput = (value: Date) => {
    const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000)
    return local.toISOString().slice(0, 16)
  }
  const from = new Date(now)
  from.setMonth(from.getMonth() - 2)
  from.setHours(0, 0, 0, 0)
  now.setHours(23, 59, 0, 0)
  return { from: toInput(from), to: toInput(now) }
}
function isChamber(value: string) { return /-CH\d+$/i.test(value) }
function onEquipmentTypeChange() {
  poolSelection.value = []
  selectedEquipment.value = []
}
function transfer(direction: Direction) {
  const source = direction === 'right' ? poolSelection.value : selectedEquipment.value
  const target = direction === 'right' ? selectedEquipment.value : poolSelection.value
  const moving = new Set(source)
  const combined = [...target, ...source].filter((item, index, all) => all.indexOf(item) === index).sort((a, b) => a.localeCompare(b))
  if (direction === 'right') selectedEquipment.value = combined
  else poolSelection.value = combined
  if (direction === 'right') poolSelection.value = poolSelection.value.filter((item) => !moving.has(item))
  else selectedEquipment.value = selectedEquipment.value.filter((item) => !moving.has(item))
  source.splice(0)
}
function matchesDate(value: string, fromEnabled: boolean, from: string, toEnabled: boolean, to: string) {
  if (!fromEnabled && !toEnabled) return true
  if (!value) return false
  const dateTime = value.replace(' ', 'T')
  return (!fromEnabled || dateTime >= from) && (!toEnabled || dateTime <= to)
}
function queryBreakdowns() {
  const filtered = events.value.filter((event) => {
    const equipmentMatches = selectedEquipment.value.length
      ? selectedEquipment.value.includes(event.equipmentNo)
      : chamberManage || !isChamber(event.equipmentNo)
    return equipmentMatches
      && matchesDate(event.breakdownStartTime, breakdownFromEnabled.value, breakdownFrom.value, breakdownToEnabled.value, breakdownTo.value)
      && matchesDate(event.repairEndTime, repairFromEnabled.value, repairFrom.value, repairToEnabled.value, repairTo.value)
  })
  const counts = new Map<string, BreakdownCount>()
  for (const event of filtered) {
    const key = `${event.breakdownNo}\u0000${event.breakdownCause}`
    const current = counts.get(key)
    if (current) current.count += 1
    else counts.set(key, { breakdownNo: event.breakdownNo, breakdownCause: event.breakdownCause, count: 1 })
  }
  resultRows.value = [...counts.values()].sort((a, b) => a.breakdownNo.localeCompare(b.breakdownNo))
  queried.value = true
  chartType.value = 'ColumnChart'
  notice.value = `查詢完成：符合 ${filtered.length} 筆故障／維修事件，彙總 ${resultRows.value.length} 組原因。`
}
function barWidth(row: BreakdownCount) { return `${(row.count / maxCount.value) * 100}%` }
function printChart() { window.print() }

watch(equipmentType, onEquipmentTypeChange)
</script>

<template>
  <section class="pm-breakdown-query" aria-label="故障原因統計 DEV 預覽">
    <header class="page-header">
      <div><p>PMBRAEKDOWN QUERY / PM505</p><h1>故障原因統計</h1></div>
      <div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (&amp;X)</button></div>
    </header>

    <section class="criteria-panel" aria-label="查詢條件">
      <div class="type-and-equipment">
        <label class="filter-title" for="equipment-type">Equipment Type</label>
        <select id="equipment-type" v-model="equipmentType"><option value="">Select equipment type</option><option v-for="item in equipmentTypes" :key="item" :value="item">{{ item }}</option></select>
        <label class="filter-title equipment-title">Equipment No</label>
        <div class="list-pair">
          <select v-model="poolSelection" multiple aria-label="未選設備清單" @dblclick="transfer('right')">
            <option v-for="item in equipmentOptions.filter((value) => !selectedEquipment.includes(value))" :key="item" :value="item">{{ item }}</option>
          </select>
          <div class="list-arrows"><button type="button" class="secondary" aria-label="加入設備篩選" @click="transfer('right')">›</button><button type="button" class="secondary" aria-label="移除設備篩選" @click="transfer('left')">‹</button></div>
          <select v-model="selectedEquipment" multiple aria-label="已選設備清單" @dblclick="transfer('left')">
            <option v-for="item in selectedEquipment" :key="item" :value="item">{{ item }}</option>
          </select>
        </div>
        <p class="filter-note">類型只載入設備選項；需把設備移至右側才會限定統計結果。</p>
      </div>
      <div class="date-criteria">
        <div class="date-filter"><span class="filter-title">Break Down Time</span><label><input v-model="breakdownFromEnabled" type="checkbox"> From <input v-model="breakdownFrom" type="datetime-local" step="60" :disabled="!breakdownFromEnabled"></label><label><input v-model="breakdownToEnabled" type="checkbox"> To <input v-model="breakdownTo" type="datetime-local" step="60" :disabled="!breakdownToEnabled"></label></div>
        <div class="date-filter"><span class="filter-title">Repair Time</span><label><input v-model="repairFromEnabled" type="checkbox"> From <input v-model="repairFrom" type="datetime-local" step="60" :disabled="!repairFromEnabled"></label><label><input v-model="repairToEnabled" type="checkbox"> To <input v-model="repairTo" type="datetime-local" step="60" :disabled="!repairToEnabled"></label></div>
        <button type="button" @click="queryBreakdowns">Query</button>
      </div>
    </section>

    <section class="results-layout">
      <section class="grid-panel" aria-label="故障原因統計表">
        <header class="panel-header"><div><h2>Summary</h2><p>{{ notice }}</p></div><strong>{{ totalCount }} events</strong></header>
        <div class="table-wrap"><table>
          <thead><tr><th>Break Down No</th><th>Break Down Cause</th><th>Count</th></tr></thead>
          <tbody>
            <tr v-for="row in resultRows" :key="`${row.breakdownNo}-${row.breakdownCause}`"><td>{{ row.breakdownNo }}</td><td>{{ row.breakdownCause }}</td><td class="count-cell">{{ row.count }}</td></tr>
            <tr v-if="resultRows.length === 0"><td colspan="3" class="empty">{{ queried ? '沒有符合條件的資料。' : '設定條件後按 Query。' }}</td></tr>
          </tbody>
        </table></div>
      </section>

      <section class="chart-panel" aria-label="故障原因統計圖">
        <header class="panel-header"><div><h2>設備故障統計圖</h2><p>依故障代碼／原因彙總事件次數</p></div></header>
        <div v-if="resultRows.length" class="chart-stage">
          <div v-if="chartType === 'PieChart'" class="pie-layout">
            <div class="pie-chart" :style="{ background: pieGradient }" role="img" aria-label="故障原因次數圓餅圖"></div>
            <ul class="legend"><li v-for="(row, index) in resultRows" :key="row.breakdownNo"><i :style="{ background: ['#385f79','#b26f48','#718755','#88719a','#ba9850','#4b9992','#a45c6a'][index % 7] }"></i><span>{{ row.breakdownCause }}</span><b>{{ row.count }}</b></li></ul>
          </div>
          <div v-else-if="chartType === 'BarChart'" class="horizontal-bars">
            <div v-for="row in resultRows" :key="row.breakdownNo" class="bar-row"><span class="bar-label">{{ row.breakdownCause }}</span><div class="bar-track"><div class="bar-fill" :style="{ width: barWidth(row) }"></div></div><b>{{ row.count }}</b></div>
          </div>
          <svg v-else class="axis-chart" viewBox="0 0 760 350" role="img" :aria-label="`${chartTypes.find((item) => item.value === chartType)?.label} 故障原因圖`">
            <line x1="54" y1="284" x2="728" y2="284" class="axis-line" /><line x1="54" y1="42" x2="54" y2="284" class="axis-line" />
            <line v-for="tick in [0, 1, 2, 3, 4]" :key="tick" x1="54" :y1="284 - tick * 55" x2="728" :y2="284 - tick * 55" class="grid-line" />
            <path v-if="chartType === 'AreaChart'" :d="areaPath" class="area-fill" />
            <polyline v-if="chartType === 'LineChart' || chartType === 'AreaChart'" :points="linePoints" class="line-path" />
            <template v-if="chartType === 'ColumnChart'"><g v-for="bar in verticalBars" :key="bar.breakdownNo">
                <rect :x="bar.x" :y="bar.y" :width="bar.width" :height="bar.height" rx="2" class="column-bar" />
                <text :x="bar.x + bar.width / 2" :y="bar.y - 7" class="bar-value" text-anchor="middle">{{ bar.count }}</text>
              </g></template>
            <g v-for="point in chartPoints" :key="point.breakdownNo">
              <circle v-if="chartType !== 'ColumnChart'" :cx="point.x" :cy="point.y" r="4" class="line-dot" />
              <text :x="point.x" y="309" class="x-label" text-anchor="middle">{{ point.breakdownNo }}</text>
              <text :x="point.x" y="327" class="cause-label" text-anchor="middle">{{ point.breakdownCause }}</text>
            </g>
          </svg>
          <p v-if="chartType === 'ColumnChart' || chartType === 'LineChart' || chartType === 'AreaChart'" class="chart-caption">Break Down No / Cause · Count</p>
        </div>
        <div v-else class="empty chart-empty">{{ queried ? '沒有可繪製的統計資料。' : '設定條件後按 Query。' }}</div>
        <footer class="chart-actions"><label>Chart Type <select v-model="chartType" :disabled="!resultRows.length"><option v-for="item in chartTypes" :key="`footer-${item.value}`" :value="item.value">{{ item.label }}</option></select></label><button type="button" class="secondary" :disabled="!resultRows.length" @click="printChart">Print (&amp;P)</button><button type="button" class="secondary" @click="emit('close')">Close (&amp;X)</button></footer>
      </section>
    </section>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>Repair Time 條件依來源實際比對 RepairEndTime；不呼叫正式 API。</span></footer>
  </section>
</template>

<style scoped>
.pm-breakdown-query { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.panel-header,.page-footer,.chart-actions { display:flex; align-items:center; }.page-header,.panel-header { justify-content:space-between; gap:10px; }.page-header { border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:8px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }button:disabled { opacity:.45; cursor:default; }.filter-title { display:block; margin-bottom:4px; color:#514f45; font-weight:600; }
.criteria-panel { display:grid; grid-template-columns:minmax(230px,.85fr) minmax(520px,2fr); gap:14px; padding:9px; border:1px solid #d0ccbf; background:#eeece4; }.type-and-equipment { min-width:0; }.type-and-equipment select:not([multiple]) { width:100%; min-height:27px; padding:3px 5px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.equipment-title { margin-top:8px; }.list-pair { display:grid; grid-template-columns:minmax(0,1fr) 28px minmax(0,1fr); gap:4px; height:92px; }.list-pair select { width:100%; min-width:0; padding:4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.list-arrows { display:flex; flex-direction:column; justify-content:center; gap:5px; }.list-arrows button { min-height:25px; padding:2px; font-size:16px; }.filter-note { margin:4px 0 0; color:#777366; font-size:10px; }.date-criteria { display:flex; flex-wrap:wrap; align-content:space-around; align-items:center; gap:9px 14px; }.date-filter { display:grid; grid-template-columns:auto auto; align-items:center; gap:5px 9px; }.date-filter .filter-title { grid-column:1/-1; margin:0; }.date-filter label { display:flex; align-items:center; gap:4px; white-space:nowrap; color:#514f45; }.date-filter input[type=datetime-local] { min-height:27px; min-width:0; padding:3px 4px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }.date-filter input:disabled { opacity:.5; }
.results-layout { flex:1; min-height:0; display:grid; grid-template-columns:minmax(225px,.78fr) minmax(0,2.2fr); gap:9px; }.grid-panel,.chart-panel { min-height:0; display:flex; flex-direction:column; gap:8px; padding:9px; border:1px solid #d0ccbf; background:#fffef9; }.panel-header h2 { margin:0; font-size:15px; }.panel-header p { margin:2px 0 0; color:#777366; font-size:10px; }.panel-header strong { white-space:nowrap; color:#514f45; }.table-wrap { flex:1; min-height:0; overflow:auto; border:1px solid #e0ddd4; }table { width:100%; border-collapse:collapse; white-space:nowrap; }th,td { padding:6px 7px; border-bottom:1px solid #e7e4db; text-align:left; }th { position:sticky; top:0; z-index:1; background:#e9e6dc; color:#555246; font-weight:600; }tbody tr:nth-child(even) { background:#f8f7f1; }.count-cell { text-align:right; font-weight:600; }.empty { padding:20px; text-align:center; color:#777366; }.chart-actions select { min-height:27px; padding:3px 6px; border:1px solid #bbb7a9; background:#fffef9; color:#26251f; font:inherit; }
.chart-stage { flex:1; min-height:0; display:flex; flex-direction:column; justify-content:center; overflow:auto; }.axis-chart { width:100%; min-height:240px; max-height:360px; }.axis-line { stroke:#746f61; stroke-width:1.3; }.grid-line { stroke:#e3e0d7; stroke-width:1; }.column-bar { fill:#476c84; }.bar-value { fill:#3f4c55; font-size:12px; }.line-path { fill:none; stroke:#456f85; stroke-width:3; stroke-linecap:round; stroke-linejoin:round; }.line-dot { fill:#b26f48; stroke:#fffef9; stroke-width:2; }.area-fill { fill:#c6d6df99; stroke:none; }.x-label { fill:#656154; font-size:10px; }.cause-label { fill:#888477; font-size:9px; }.chart-caption { margin:0; text-align:center; color:#777366; font-size:10px; }.horizontal-bars { display:flex; flex-direction:column; gap:12px; padding:16px 10px; }.bar-row { display:grid; grid-template-columns:minmax(100px,1.2fr) minmax(80px,3fr) 30px; align-items:center; gap:8px; }.bar-label { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; color:#514f45; }.bar-track { height:17px; border-radius:2px; background:#e9e6dc; }.bar-fill { height:100%; border-radius:2px; background:#476c84; }.pie-layout { display:flex; align-items:center; justify-content:center; flex-wrap:wrap; gap:26px; padding:18px; }.pie-chart { width:min(42vw,300px); aspect-ratio:1; border-radius:50%; border:1px solid #fffef9; box-shadow:0 0 0 1px #d0ccbf; }.legend { display:grid; gap:8px; margin:0; padding:0; list-style:none; }.legend li { display:grid; grid-template-columns:11px minmax(90px,180px) 28px; align-items:center; gap:7px; }.legend i { width:10px; height:10px; border-radius:2px; }.legend span { overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }.legend b { text-align:right; }.chart-empty { flex:1; display:grid; place-items:center; }.chart-actions { justify-content:flex-end; flex-wrap:wrap; gap:7px; padding-top:6px; border-top:1px solid #e4e1d8; }.chart-actions label { display:flex; align-items:center; gap:5px; margin-right:auto; color:#514f45; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
@media(max-width:940px) { .criteria-panel { grid-template-columns:1fr; }.type-and-equipment { max-width:420px; }.date-criteria { justify-content:flex-start; }.results-layout { min-height:440px; grid-template-columns:minmax(190px,.8fr) minmax(0,2fr); }.page-footer { align-items:flex-start; flex-direction:column; } }
@media(max-width:640px) { .pm-breakdown-query { padding:7px; }.results-layout { grid-template-columns:1fr; grid-template-rows:minmax(180px,.7fr) minmax(280px,1.3fr); }.date-criteria { align-items:flex-start; flex-direction:column; }.date-filter { grid-template-columns:1fr; }.date-filter .filter-title { grid-column:auto; }.pie-layout { gap:12px; padding:8px; } }
@media print { .page-header,.criteria-panel,.grid-panel,.page-footer,.chart-actions { display:none !important; }.pm-breakdown-query { height:auto; background:white; }.results-layout { display:block; }.chart-panel { border:0; }.chart-stage { height:90vh; } }
</style>

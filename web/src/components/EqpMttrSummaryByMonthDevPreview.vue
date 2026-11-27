<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type ViewMode = 'main' | 'chamber'
type FilterKey = 'areaNo' | 'equipmentType' | 'equipmentNo' | 'mainEquipmentNo'
type Metric = 'MTTR' | 'MTBF' | 'Availability'
interface EquipmentSeed { areaNo: string; areaName: string; equipmentType: string; equipmentNo: string; mainEquipmentNo: string; chamberFlag: boolean }
interface SummaryRow { equipmentNo: string; areaNo: string; equipmentType: string; mainEquipmentNo: string; chamberFlag: boolean; metric: Metric; values: Record<string, number | null> }
interface Option { value: string; label: string }
interface ChartPoint { x: number; y: number; month: string; value: number }
interface TrendMetric { name: Metric; unit: string; color: string; segments: string[]; points: ChartPoint[]; labels: { x: number; text: string }[] }

const emit = defineEmits<{ close: [] }>()
const chamberManageEnabled = true // DEV 的 TBLSYSPARAMETER.ChamberManage 假設定值
const equipment: EquipmentSeed[] = [
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'EQ-ASM-001', mainEquipmentNo: '', chamberFlag: false },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Die Attach', equipmentNo: 'EQ-ASM-002', mainEquipmentNo: '', chamberFlag: false },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Handler', equipmentNo: 'EQ-TST-001', mainEquipmentNo: '', chamberFlag: false },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Tester', equipmentNo: 'EQ-TST-002', mainEquipmentNo: '', chamberFlag: false },
  { areaNo: 'AREA-C', areaName: '三廠包裝區', equipmentType: 'Packer', equipmentNo: 'EQ-PKG-001', mainEquipmentNo: '', chamberFlag: false },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'CH-ASM-001-01', mainEquipmentNo: 'EQ-ASM-001', chamberFlag: true },
  { areaNo: 'AREA-A', areaName: '一廠組裝區', equipmentType: 'Bonder', equipmentNo: 'CH-ASM-001-02', mainEquipmentNo: 'EQ-ASM-001', chamberFlag: true },
  { areaNo: 'AREA-B', areaName: '二廠測試區', equipmentType: 'Tester', equipmentNo: 'CH-TST-002-01', mainEquipmentNo: 'EQ-TST-002', chamberFlag: true },
]
const metrics: Metric[] = ['MTTR', 'MTBF', 'Availability']
const metricColor: Record<Metric, string> = { MTTR: '#ba4b4b', MTBF: '#558d61', Availability: '#4978b7' }
const now = new Date()
const currentMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
const fromMonth = reactive<Record<ViewMode, string>>({ main: currentMonth, chamber: currentMonth })
const toMonth = reactive<Record<ViewMode, string>>({ main: currentMonth, chamber: currentMonth })
const filters = reactive<Record<ViewMode, Record<FilterKey, string>>>({
  main: { areaNo: '', equipmentType: '', equipmentNo: '', mainEquipmentNo: '' },
  chamber: { areaNo: '', equipmentType: '', equipmentNo: '', mainEquipmentNo: '' },
})
const enabled = reactive<Record<ViewMode, Record<FilterKey, boolean>>>({
  main: { areaNo: false, equipmentType: false, equipmentNo: false, mainEquipmentNo: false },
  chamber: { areaNo: false, equipmentType: false, equipmentNo: false, mainEquipmentNo: false },
})
const resultMonths = reactive<Record<ViewMode, string[]>>({ main: [], chamber: [] })
const results = reactive<Record<ViewMode, SummaryRow[]>>({ main: [], chamber: [] })
const rangeLabels = reactive<Record<ViewMode, string>>({ main: '', chamber: '' })
const notices = reactive<Record<ViewMode, string>>({ main: '', chamber: '' })
const errors = reactive<Record<ViewMode, string>>({ main: '', chamber: '' })
const mode = ref<ViewMode>('main')
const pickerOpen = ref(false)
const pickerTarget = ref<ViewMode>('main')
const pickerKey = ref<FilterKey>('areaNo')
const pickerSearch = ref('')
const stagedValues = reactive(new Set<string>())
const trendOpen = ref(false)
const infoOpen = ref(false)
const copiedChart = ref('')

const activeRows = computed(() => results[mode.value])
const activeMonths = computed(() => resultMonths[mode.value])
const activeRange = computed(() => rangeLabels[mode.value])
const activeNotice = computed(() => notices[mode.value])
const activeError = computed(() => errors[mode.value])
const filterKeys = computed<FilterKey[]>(() => mode.value === 'main' ? ['areaNo', 'equipmentType', 'equipmentNo'] : ['areaNo', 'equipmentType', 'mainEquipmentNo', 'equipmentNo'])
const filterLabels: Record<FilterKey, string> = { areaNo: 'Area No', equipmentType: 'Equipment Type', equipmentNo: 'Equipment No', mainEquipmentNo: 'Main Equipment No' }
const areas: Option[] = Array.from(new Map(equipment.map((row) => [row.areaNo, { value: row.areaNo, label: `${row.areaNo} · ${row.areaName}` }])).values())
const types: Option[] = Array.from(new Set(equipment.map((row) => row.equipmentType))).map((value) => ({ value, label: value }))
function chosen(target: ViewMode, key: FilterKey) { return new Set(filters[target][key].split(',').map((value) => value.trim().toUpperCase()).filter(Boolean)) }
const pickerOptions = computed(() => {
  const target = pickerTarget.value
  const key = pickerKey.value
  const term = pickerSearch.value.trim().toUpperCase()
  const typeFilter = chosen(target, 'equipmentType')
  let options: Option[]
  if (key === 'areaNo') options = areas
  else if (key === 'equipmentType') options = types
  else if (key === 'mainEquipmentNo') options = equipment.filter((row) => !row.chamberFlag && (!typeFilter.size || typeFilter.has(row.equipmentType.toUpperCase()))).map((row) => ({ value: row.equipmentNo, label: `${row.equipmentNo} · ${row.equipmentType}` }))
  else options = equipment.filter((row) => row.chamberFlag === (target === 'chamber') && (!typeFilter.size || typeFilter.has(row.equipmentType.toUpperCase()))).map((row) => ({ value: row.equipmentNo, label: `${row.equipmentNo} · ${row.equipmentType}` }))
  return options.filter((option) => !term || option.label.toUpperCase().includes(term))
})
const pickerTitle = computed(() => `選擇 ${filterLabels[pickerKey.value]}`)

function clearResults(target: ViewMode) {
  results[target] = []
  resultMonths[target] = []
  rangeLabels[target] = ''
  notices[target] = ''
  errors[target] = ''
  if (target === mode.value) trendOpen.value = false
}
function setChecked(target: ViewMode, key: FilterKey, value: boolean) {
  enabled[target][key] = value
  if (!value) filters[target][key] = ''
  clearResults(target)
}
function setManualEquipment(target: ViewMode, value: string) {
  filters[target].equipmentNo = value.toUpperCase()
  enabled[target].equipmentNo = Boolean(value.trim())
  clearResults(target)
}
function openPicker(key: FilterKey) {
  pickerTarget.value = mode.value
  pickerKey.value = key
  pickerSearch.value = ''
  stagedValues.clear()
  chosen(mode.value, key).forEach((value) => stagedValues.add(value))
  pickerOpen.value = true
}
function toggleStaged(value: string, isChecked: boolean) { isChecked ? stagedValues.add(value.toUpperCase()) : stagedValues.delete(value.toUpperCase()) }
function applyPicker() {
  filters[pickerTarget.value][pickerKey.value] = Array.from(stagedValues).join(',')
  enabled[pickerTarget.value][pickerKey.value] = stagedValues.size > 0
  pickerOpen.value = false
  clearResults(pickerTarget.value)
}
function monthIndex(value: string) {
  const [year, month] = value.replace('/', '-').split('-').map(Number)
  return year * 12 + month - 1
}
function monthLabel(index: number) { return `${Math.floor(index / 12)}/${String(index % 12 + 1).padStart(2, '0')}` }
function monthSequence(from: string, to: string) {
  const start = monthIndex(from)
  const end = monthIndex(to)
  return Array.from({ length: end - start + 1 }, (_, offset) => monthLabel(start + offset))
}
function match(target: ViewMode, key: FilterKey, value: string) {
  if (!enabled[target][key] || !filters[target][key].trim()) return true
  return chosen(target, key).has(value.toUpperCase())
}
function metricValues(seed: EquipmentSeed, seedIndex: number, months: string[]): Record<Metric, Record<string, number | null>> {
  const values: Record<Metric, Record<string, number | null>> = { MTTR: {}, MTBF: {}, Availability: {} }
  months.forEach((month, index) => {
    if ((seedIndex + index) % 17 === 16) {
      metrics.forEach((metric) => { values[metric][month] = null })
      return
    }
    const monthNumber = monthIndex(month)
    const mttr = Number((1.15 + ((seedIndex * 3 + monthNumber) % 11) * 0.19).toFixed(2))
    const mtbf = Number((29 + ((seedIndex * 7 + monthNumber * 2) % 31) * 1.35).toFixed(2))
    values.MTTR[month] = mttr
    values.MTBF[month] = mtbf
    values.Availability[month] = Number((mtbf / (mtbf + mttr)).toFixed(4))
  })
  return values
}
function runQuery() {
  const target = mode.value
  clearResults(target)
  const fromIndex = monthIndex(fromMonth[target])
  const toIndex = monthIndex(toMonth[target])
  const currentIndex = now.getFullYear() * 12 + now.getMonth()
  if (!fromMonth[target] || !toMonth[target]) { errors[target] = '請選擇開始與結束月份。'; return }
  if (fromIndex > currentIndex || toIndex > currentIndex) { errors[target] = '開始月份與結束月份不可晚於本月。'; return }
  if (fromIndex > toIndex) { errors[target] = '開始月份不可晚於結束月份。'; return }
  if (toIndex - fromIndex + 1 > 120) { errors[target] = 'DEV 預覽限制單次查詢最多 120 個月份。'; return }
  const months = monthSequence(fromMonth[target], toMonth[target])
  const selectedEquipment = equipment
    .map((seed, index) => ({ seed, index }))
    .filter(({ seed }) => seed.chamberFlag === (target === 'chamber')
      && match(target, 'areaNo', seed.areaNo)
      && match(target, 'equipmentType', seed.equipmentType)
      && match(target, 'equipmentNo', seed.equipmentNo)
      && (target !== 'chamber' || match(target, 'mainEquipmentNo', seed.mainEquipmentNo)))
    .sort((left, right) => left.seed.areaNo.localeCompare(right.seed.areaNo) || left.seed.equipmentType.localeCompare(right.seed.equipmentType) || (left.seed.mainEquipmentNo || left.seed.equipmentNo).localeCompare(right.seed.mainEquipmentNo || right.seed.equipmentNo) || left.seed.equipmentNo.localeCompare(right.seed.equipmentNo))
  const rows: SummaryRow[] = []
  for (const { seed, index } of selectedEquipment) {
    const byMetric = metricValues(seed, index, months)
    if (months.every((month) => metrics.every((metric) => byMetric[metric][month] === null))) continue
    for (const metric of metrics) {
      rows.push({ equipmentNo: seed.equipmentNo, areaNo: seed.areaNo, equipmentType: seed.equipmentType, mainEquipmentNo: seed.mainEquipmentNo, chamberFlag: seed.chamberFlag, metric, values: byMetric[metric] })
    }
  }
  results[target] = rows
  resultMonths[target] = months
  rangeLabels[target] = `${monthLabel(fromIndex)} ～ ${monthLabel(toIndex)}`
  notices[target] = rows.length ? `查詢完成：${selectedEquipment.length} 台設備，${months.length} 個月。` : '查無符合條件的設備。'
}
function displayValue(metric: Metric, value: number | null | undefined) {
  if (value === null || value === undefined) return '—'
  return metric === 'Availability' ? `${(value * 100).toFixed(2)}%` : value.toFixed(2)
}
function openTrend() { trendOpen.value = true; copiedChart.value = '' }
const trendCharts = computed(() => {
  const targetRows = activeRows.value
  const months = activeMonths.value
  const deviceMap = new Map<string, SummaryRow[]>()
  targetRows.forEach((row) => deviceMap.set(row.equipmentNo, [...(deviceMap.get(row.equipmentNo) ?? []), row]))
  return Array.from(deviceMap.entries()).map(([equipmentNo, rows]) => {
    const first = rows[0]
    const charts = metrics.map((metric): TrendMetric => {
      const row = rows.find((item) => item.metric === metric)
      const raw = months.map((month) => row?.values[month] ?? null)
      const series = metric === 'Availability' ? raw.map((value) => value === null ? null : value * 100) : raw
      const visible = series.filter((value): value is number => value !== null)
      const max = metric === 'Availability' ? 110 : Math.max(10, Math.ceil(Math.max(...visible, 0) * 1.18))
      const left = 36; const top = 10; const width = 310; const height = 86
      const points: ChartPoint[] = series.flatMap((value, index) => value === null ? [] : [{ x: left + (months.length <= 1 ? width / 2 : index / (months.length - 1) * width), y: top + height - value / max * height, month: months[index], value }])
      const segments: string[] = []
      let segment: ChartPoint[] = []
      series.forEach((value, index) => {
        if (value === null) { if (segment.length) segments.push(segment.map((point) => `${point.x},${point.y}`).join(' ')); segment = []; return }
        const point = points.find((item) => item.month === months[index])
        if (point) segment.push(point)
      })
      if (segment.length) segments.push(segment.map((point) => `${point.x},${point.y}`).join(' '))
      const interval = Math.max(1, Math.ceil(months.length / 7))
      const labels = months.flatMap((month, index) => index % interval === 0 || index === months.length - 1 ? [{ x: left + (months.length <= 1 ? width / 2 : index / (months.length - 1) * width), text: month.slice(2) }] : [])
      return { name: metric, unit: metric === 'Availability' ? '%' : 'hr', color: metricColor[metric], segments, points, labels }
    })
    return { equipmentNo, areaNo: first.areaNo, equipmentType: first.equipmentType, mainEquipmentNo: first.mainEquipmentNo, charts }
  })
})
function notifyCopy(equipmentNo: string, metric: Metric) {
  copiedChart.value = `${equipmentNo} · ${metric} 圖表已複製（DEV 提示；未使用系統剪貼簿）。`
  window.setTimeout(() => { copiedChart.value = '' }, 3000)
}
</script>

<template>
  <section class="monthly" aria-label="設備 MTTR 統計表月報 DEV 預覽">
    <header class="titlebar">
      <div><p class="eyebrow">RPT / 在製品 / RT413</p><h1>設備 MTTR 統計表_以月份匯總 <span v-if="mode === 'chamber'">· Chamber</span></h1></div>
      <div class="title-actions"><button class="subtle" type="button" @click="infoOpen = true">? 說明</button><button v-if="mode === 'chamber'" class="subtle" type="button" @click="mode = 'main'">返回主設備</button><button v-else-if="chamberManageEnabled" class="subtle" type="button" @click="mode = 'chamber'">Chamber 月報</button><button class="close" type="button" @click="emit('close')">關閉 ×</button></div>
    </header>

    <section class="filters" aria-label="查詢條件">
      <div class="filter-grid">
        <div v-for="key in filterKeys" :key="key" class="pick-row">
          <label class="check-label"><input type="checkbox" :checked="enabled[mode][key]" @change="setChecked(mode, key, ($event.target as HTMLInputElement).checked)"> {{ filterLabels[key] }}</label>
          <input v-if="key === 'equipmentNo'" :value="filters[mode][key]" placeholder="可輸入逗號分隔編號" @input="setManualEquipment(mode, ($event.target as HTMLInputElement).value)">
          <input v-else class="readonly" :value="filters[mode][key]" placeholder="未指定" readonly>
          <button class="picker-button" type="button" @click="openPicker(key)">...</button>
        </div>
        <div class="date-row"><label :for="`${mode}-from`">開始月份</label><input :id="`${mode}-from`" v-model="fromMonth[mode]" type="month" @change="clearResults(mode)"></div>
        <div class="date-row"><label :for="`${mode}-to`">結束月份</label><input :id="`${mode}-to`" v-model="toMonth[mode]" type="month" @change="clearResults(mode)"></div>
        <button class="query" type="button" @click="runQuery">查詢</button>
      </div>
      <p class="source-note">DEV 固定假資料；月份按完整區間計算，本月現況只累計到目前時間。ChamberManage 假設定為啟用，主／子報表分開查詢。</p>
    </section>

    <div v-if="activeError" class="feedback error" role="alert">{{ activeError }}</div>
    <div v-else-if="activeNotice" class="feedback" role="status">{{ activeNotice }}</div>
    <div v-if="activeRange" class="range-note">統計月份：{{ activeRange }} <span>｜{{ activeRows.length / 3 }} 台設備 · {{ activeRows.length }} 列</span></div>

    <div class="grid-wrap">
      <table>
        <thead><tr><th>Equipment No</th><th>Area No</th><th>Equipment Type</th><th v-if="mode === 'chamber'">Main Equipment No</th><th>Type</th><th v-for="month in activeMonths" :key="month" class="numeric month-head">{{ month }}</th></tr></thead>
        <tbody v-if="activeRows.length">
          <tr v-for="(row, index) in activeRows" :key="`${row.equipmentNo}-${row.metric}`" :class="{ 'alternate-group': Math.floor(index / 3) % 2 === 1 }">
            <td>{{ row.equipmentNo }}</td><td>{{ row.areaNo }}</td><td>{{ row.equipmentType }}</td><td v-if="mode === 'chamber'">{{ row.mainEquipmentNo }}</td><td class="metric-name">{{ row.metric }}</td>
            <td v-for="month in activeMonths" :key="month" class="numeric">{{ displayValue(row.metric, row.values[month]) }}</td>
          </tr>
        </tbody>
        <tbody v-else><tr><td class="empty" :colspan="4 + activeMonths.length + (mode === 'chamber' ? 1 : 0)">尚未查詢</td></tr></tbody>
      </table>
    </div>

    <footer class="statusbar"><span>設備 MTTR 月報 · DEV 假資料</span><button type="button" class="trend-button" :disabled="!activeRows.length" @click="openTrend">Trend Chart</button></footer>

    <div v-if="pickerOpen" class="overlay" @click.self="pickerOpen = false">
      <section class="dialog picker" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><h2>{{ pickerTitle }}</h2><button type="button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <input v-model="pickerSearch" class="search" type="search" placeholder="搜尋...">
        <div class="option-list"><label v-for="option in pickerOptions" :key="option.value"><input type="checkbox" :checked="stagedValues.has(option.value.toUpperCase())" @change="toggleStaged(option.value, ($event.target as HTMLInputElement).checked)"><span>{{ option.label }}</span></label><p v-if="!pickerOptions.length" class="empty-options">沒有符合項目</p></div>
        <footer><span>已選 {{ stagedValues.size }} 項</span><div><button class="subtle" type="button" @click="stagedValues.clear()">清除</button><button class="query" type="button" @click="applyPicker">套用</button></div></footer>
      </section>
    </div>

    <div v-if="trendOpen" class="overlay trend-overlay" @click.self="trendOpen = false">
      <section class="dialog trend-dialog" role="dialog" aria-modal="true" aria-label="MTTR 月趨勢圖">
        <header><div><p class="eyebrow">{{ activeRange }}</p><h2>設備 MTTR 月趨勢</h2></div><button type="button" aria-label="關閉" @click="trendOpen = false">×</button></header>
        <p v-if="copiedChart" class="copy-notice" role="status">{{ copiedChart }}</p>
        <div class="trend-list">
          <article v-for="device in trendCharts" :key="device.equipmentNo" class="device-card">
            <h3>{{ device.equipmentNo }} <small>{{ device.areaNo }} · {{ device.equipmentType }}<template v-if="device.mainEquipmentNo"> · Main {{ device.mainEquipmentNo }}</template></small></h3>
            <div class="chart-grid">
              <section v-for="chart in device.charts" :key="chart.name" class="chart-card">
                <div class="chart-title"><strong :style="{ color: chart.color }">{{ chart.name }}</strong><button type="button" @click="notifyCopy(device.equipmentNo, chart.name)">複製圖表</button></div>
                <svg viewBox="0 0 360 126" role="img" :aria-label="`${device.equipmentNo} ${chart.name} 趨勢`">
                  <line x1="36" y1="10" x2="36" y2="96" stroke="#b9b5a9"/><line x1="36" y1="96" x2="346" y2="96" stroke="#b9b5a9"/>
                  <line v-for="y in [31, 53, 75]" :key="y" x1="36" :y1="y" x2="346" :y2="y" stroke="#ece9e1"/>
                  <polyline v-for="(segment, segmentIndex) in chart.segments" :key="segmentIndex" :points="segment" fill="none" :stroke="chart.color" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round"/>
                  <circle v-for="point in chart.points" :key="point.month" :cx="point.x" :cy="point.y" r="3" :fill="chart.color"><title>{{ point.month }} · {{ point.value.toFixed(2) }} {{ chart.unit }}</title></circle>
                  <text v-for="label in chart.labels" :key="label.x" :x="label.x" y="116" text-anchor="middle">{{ label.text }}</text>
                </svg>
                <p class="unit-note">單位：{{ chart.unit }}{{ chart.name === 'Availability' ? '，縱軸 0–110%' : '' }}</p>
              </section>
            </div>
          </article>
        </div>
      </section>
    </div>

    <div v-if="infoOpen" class="overlay" @click.self="infoOpen = false">
      <section class="dialog info" role="dialog" aria-modal="true" aria-label="MTTR 月報說明">
        <header><h2>月報說明</h2><button type="button" aria-label="關閉" @click="infoOpen = false">×</button></header>
        <ul><li>開始月與結束月均包含在查詢範圍中；來源將每月拆成各自的完整月區間計算。</li><li>每台設備依序有 MTTR、MTBF、Availability 三列；Availability 的來源值為 0–1 比率，表格與趨勢圖顯示百分比。</li><li>主設備結果只列 `ChamberFlag=0`；Chamber 月報另提供 Main Equipment No 與 Chamber Equipment No 篩選。</li><li>Trend Chart 每台設備顯示 MTTR、MTBF 與 Availability 三張折線圖。複製按鈕只顯示 DEV 提示，不操作系統剪貼簿。</li><li>設備、月份指標與缺值均為合成假資料，不代表實際設備表現。</li></ul>
        <button class="query" type="button" @click="infoOpen = false">關閉</button>
      </section>
    </div>
  </section>
</template>

<style scoped>
.monthly{box-sizing:border-box;min-height:0;height:100%;display:flex;flex-direction:column;gap:10px;padding:12px;background:#f7f5ee;color:#26251f;font:13px/1.4 "Segoe UI",Arial,sans-serif;overflow:auto}.titlebar{display:flex;justify-content:space-between;align-items:center;gap:12px;border-bottom:1px solid #d4d0c4;padding:0 0 9px}.eyebrow{margin:0 0 2px;color:#817b6b;font-size:11px;letter-spacing:.08em}.titlebar h1{margin:0;font-size:19px;font-weight:600}.titlebar h1 span{color:#777365;font-size:15px}.title-actions{display:flex;gap:6px;flex-wrap:wrap}.title-actions button,.picker-button,.query,.subtle,.close,.trend-button{min-height:28px;border:1px solid #aaa596;border-radius:3px;padding:4px 10px;background:#fff;color:#343229;font:inherit;cursor:pointer}.title-actions .close,.query{background:#3b3930;border-color:#3b3930;color:white}.filters{border:1px solid #d0ccbf;background:#efede5;padding:10px}.filter-grid{display:grid;grid-template-columns:repeat(3,minmax(260px,1fr));gap:8px 12px;align-items:center}.pick-row{display:grid;grid-template-columns:145px minmax(80px,1fr) 32px;gap:5px;align-items:center}.check-label{display:flex;align-items:center;gap:6px;white-space:nowrap}.check-label input{accent-color:#526d63}.pick-row input,.date-row input,.search{box-sizing:border-box;min-width:0;height:29px;border:1px solid #c5c0b2;border-radius:2px;padding:4px 7px;background:#fff;color:#2d2c26;font:inherit}.pick-row input.readonly{background:#f8f7f2;color:#625f54}.picker-button{min-width:30px;padding:2px}.date-row{display:flex;align-items:center;gap:8px}.date-row label{width:145px;text-align:right}.date-row input{width:150px}.query{justify-self:end;min-width:74px}.source-note{margin:8px 0 0;color:#777365;font-size:11px}.feedback{padding:7px 9px;border-left:3px solid #697f68;background:#e8eee5;color:#435745}.feedback.error{border-color:#b65045;background:#f7e9e5;color:#8c342b}.range-note{font-weight:600;color:#4b493f}.range-note span{font-weight:400;color:#777365}.grid-wrap{min-height:170px;flex:1;overflow:auto;border:1px solid #c8c3b5;background:white}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{border-right:1px solid #d9d5c9;border-bottom:1px solid #ddd9ce;padding:7px 9px;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e4da;color:#444137;font-weight:600}th.month-head{min-width:88px}tbody tr:nth-child(even){background:#f8f7f2}tbody tr.alternate-group,tbody tr.alternate-group:nth-child(even){background:#eaf4f4}td.numeric,th.numeric{text-align:right}.metric-name{font-weight:600;color:#565247}.empty{text-align:center!important;color:#827d70;padding:26px}.statusbar{display:flex;justify-content:space-between;align-items:center;border-top:1px solid #d4d0c4;padding-top:6px;color:#777365;font-size:11px}.trend-button{background:#fff;color:#343229}.trend-button:disabled{color:#a19c8f;cursor:not-allowed;opacity:.65}.overlay{position:fixed;inset:0;z-index:40;display:grid;place-items:center;padding:18px;background:#16150f66}.dialog{box-sizing:border-box;width:min(460px,100%);max-height:min(80vh,640px);display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #9b9688;border-radius:4px;background:#f8f7f2;box-shadow:0 8px 30px #0003}.dialog header,.dialog footer{display:flex;justify-content:space-between;align-items:center;gap:10px}.dialog h2{margin:0;font-size:16px;font-weight:600}.dialog header button{width:27px;height:27px;border:1px solid #c4bfb1;background:white;border-radius:3px;font-size:18px;cursor:pointer}.search{width:100%}.option-list{min-height:100px;overflow:auto;border:1px solid #d2cec1;background:white}.option-list label{display:flex;gap:8px;align-items:center;padding:7px 9px;border-bottom:1px solid #eeeae0;cursor:pointer}.option-list label:hover{background:#f1efe7}.option-list input{accent-color:#526d63}.empty-options{padding:12px;color:#777365}.dialog footer{border-top:1px solid #dedace;padding-top:9px;color:#777365;font-size:12px}.dialog footer div{display:flex;gap:7px}.info{align-items:flex-start}.info ul{margin:0;padding-left:20px;line-height:1.65}.info li+li{margin-top:5px}.info>.query{align-self:flex-end}.trend-overlay{place-items:stretch}.trend-dialog{width:min(1400px,100%);height:min(94vh,1100px);max-height:none}.trend-dialog header h2{font-size:18px}.trend-list{min-height:0;flex:1;overflow:auto}.device-card{margin:0 0 12px;border:1px solid #d1ccbf;background:#fff}.device-card>h3{position:sticky;top:0;z-index:1;margin:0;padding:8px 11px;border-bottom:1px solid #d9d5ca;background:#eeeae0;font-size:14px}.device-card small{margin-left:8px;color:#777365;font-size:11px;font-weight:400}.chart-grid{display:grid;grid-template-columns:repeat(3,minmax(260px,1fr));gap:8px;padding:9px}.chart-card{min-width:0;border:1px solid #e1ddd2;background:#fcfbf8}.chart-title{display:flex;justify-content:space-between;align-items:center;padding:5px 8px;border-bottom:1px solid #ece9e1}.chart-title button{border:1px solid #d3cebf;border-radius:2px;background:#fff;padding:2px 6px;color:#49463d;font:inherit;font-size:10px;cursor:pointer}.chart-card svg{display:block;width:100%;height:auto;padding:3px 4px 0;overflow:visible}.chart-card svg text{fill:#686458;font-size:8px}.unit-note{margin:0;padding:0 9px 6px;color:#8a8578;font-size:10px}.copy-notice{margin:0;padding:6px 9px;border-left:3px solid #697f68;background:#e8eee5;color:#435745}.trend-dialog>.copy-notice{flex:none}@media(max-width:1050px){.filter-grid{grid-template-columns:1fr 1fr}.chart-grid{grid-template-columns:1fr}.chart-card svg{max-height:240px}.pick-row{grid-template-columns:135px minmax(80px,1fr) 32px}}@media(max-width:680px){.titlebar{align-items:flex-start;flex-direction:column}.filter-grid{grid-template-columns:1fr}.pick-row{grid-template-columns:130px minmax(50px,1fr) 32px}.date-row label{width:130px}.query{justify-self:start}.statusbar{align-items:flex-start;flex-direction:column}}
</style>

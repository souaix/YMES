<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'

type DataType = 0 | 1 | 2
type ChartType = 0 | 1 | 2
type QCItem = { qcItemNo: string; qcItemName: string; qcDataType: DataType; chartType: ChartType; sampleSize: number; center: number; sigma: number; unit: string; lsl: number; usl: number }
type ChartGroup = { factorSerial: string; qcItemNo: string; firstLotDate: string; lastLotDate: string; lotCount: number; keyNode: string; factors: Record<string, string> }
type Factor = { factorNo: string; factorName: string; value: string; factorOnLine: boolean }
type Point = { x: number; y: number; serial: string; lot: string; time: string; value: number; violation: string }
type ChartKey = 'xbar' | 'spread' | 'trend'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const itemFixtures: QCItem[] = [
  { qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', qcDataType: 1, chartType: 0, sampleSize: 5, center: 0.412, sigma: 0.028, unit: 'mm', lsl: 0.320, usl: 0.500 },
  { qcItemNo: 'QC-V-2002', qcItemName: 'Die Shear Strength', qcDataType: 2, chartType: 1, sampleSize: 5, center: 14.8, sigma: 0.62, unit: 'N', lsl: 12.5, usl: 17.2 },
  { qcItemNo: 'QC-V-3001', qcItemName: 'Wafer Thickness', qcDataType: 0, chartType: 2, sampleSize: 1, center: 725.0, sigma: 2.1, unit: 'μm', lsl: 718.5, usl: 731.5 },
  { qcItemNo: 'QC-V-3108', qcItemName: 'Mold Flash Height', qcDataType: 1, chartType: 0, sampleSize: 5, center: 0.072, sigma: 0.006, unit: 'mm', lsl: 0.052, usl: 0.092 },
]
const factorFixtures: Record<string, Factor[]> = {
  'QC-V-1001': [
    { factorNo: 'MACHINE', factorName: '機台', value: '', factorOnLine: true },
    { factorNo: 'LINE', factorName: '產線', value: '', factorOnLine: true },
    { factorNo: 'OPERATOR', factorName: '作業員', value: '', factorOnLine: true },
  ],
  'QC-V-2002': [
    { factorNo: 'MACHINE', factorName: '機台', value: '', factorOnLine: true },
    { factorNo: 'MATERIAL', factorName: '材料批號', value: '', factorOnLine: true },
  ],
  'QC-V-3001': [
    { factorNo: 'EQUIPMENT', factorName: '設備', value: '', factorOnLine: true },
    { factorNo: 'CHAMBER', factorName: '腔體', value: '', factorOnLine: true },
  ],
  'QC-V-3108': [
    { factorNo: 'MACHINE', factorName: '機台', value: '', factorOnLine: true },
    { factorNo: 'MOLD', factorName: '模具', value: '', factorOnLine: true },
  ],
}
const groupFixtures: ChartGroup[] = [
  { factorSerial: 'FS-2604-0182', qcItemNo: 'QC-V-1001', firstLotDate: '2026/11/22 06:10:22', lastLotDate: '2026/11/23 08:42:17', lotCount: 28, keyNode: 'MOLD-2', factors: { MACHINE: 'MCH-03', LINE: 'LN-A', OPERATOR: 'U11019' } },
  { factorSerial: 'FS-2604-0181', qcItemNo: 'QC-V-1001', firstLotDate: '2026/11/21 14:05:02', lastLotDate: '2026/11/22 05:53:31', lotCount: 22, keyNode: 'MOLD-1', factors: { MACHINE: 'MCH-02', LINE: 'LN-A', OPERATOR: 'U11023' } },
  { factorSerial: 'FS-2604-0179', qcItemNo: 'QC-V-1001', firstLotDate: '2026/11/20 08:32:41', lastLotDate: '2026/11/21 13:50:04', lotCount: 24, keyNode: 'MOLD-2', factors: { MACHINE: 'MCH-03', LINE: 'LN-B', OPERATOR: 'U11021' } },
  { factorSerial: 'FS-2604-0288', qcItemNo: 'QC-V-2002', firstLotDate: '2026/11/22 07:45:20', lastLotDate: '2026/11/23 08:13:51', lotCount: 19, keyNode: 'DIE-A', factors: { MACHINE: 'DS-08', MATERIAL: 'MAT-26K-031' } },
  { factorSerial: 'FS-2604-0041', qcItemNo: 'QC-V-3001', firstLotDate: '2026/11/22 02:11:08', lastLotDate: '2026/11/23 08:26:47', lotCount: 31, keyNode: 'CH-04', factors: { EQUIPMENT: 'THK-12', CHAMBER: 'CH-04' } },
  { factorSerial: 'FS-2604-0318', qcItemNo: 'QC-V-3108', firstLotDate: '2026/11/21 16:35:11', lastLotDate: '2026/11/23 08:18:55', lotCount: 26, keyNode: 'MOLD-7', factors: { MACHINE: 'MOLD-05', MOLD: 'MOLD-7' } },
]
const qcDataTypes = [{ value: '', label: '全部' }, { value: '0', label: 'Particle' }, { value: '1', label: 'InLine' }, { value: '2', label: 'OffLine' }]
const selectedType = ref('')
const selectedItem = ref<QCItem | null>(null)
const factors = ref<Factor[]>([])
const groups = ref<ChartGroup[]>([])
const activeGroup = ref<ChartGroup | null>(null)
const frequency = ref(6)
const showTrend = ref(false)
const itemPickerOpen = ref(false)
const itemSearch = ref('')
const chartOpen = ref(false)
const selectedChartPoint = ref<{ chart: ChartKey; index: number } | null>(null)
const notice = ref('請選擇 QC Item 後載入在線管制圖群組。')
const refreshCount = ref(0)
const lastRefresh = ref('尚未載入')
let refreshTimer: ReturnType<typeof setInterval> | undefined

const dataTypeLabel = (value: DataType) => qcDataTypes.find((item) => item.value === String(value))?.label || 'InLine'
const pickerItems = computed(() => itemFixtures.filter((item) => (selectedType.value === '' || item.qcDataType === Number(selectedType.value)) && `${item.qcItemNo} ${item.qcItemName}`.toLowerCase().includes(itemSearch.value.trim().toLowerCase())))
const itemGroups = computed(() => groups.value.filter((group) => !selectedItem.value || group.qcItemNo === selectedItem.value.qcItemNo))
const visibleFactors = computed(() => factors.value.filter((factor) => factor.factorOnLine))
const selectedGroup = computed(() => groups.value.find((group) => group.factorSerial === activeGroup.value?.factorSerial) || null)
const chartFactorLabel = computed(() => {
  const group = selectedGroup.value
  return group ? visibleFactors.value.map((factor) => `${factor.factorName}: ${group.factors[factor.factorNo] || '—'}`).join(' / ') : ''
})
const itemDataType = computed(() => selectedItem.value ? dataTypeLabel(selectedItem.value.qcDataType) : '—')
const chartModeName = computed(() => selectedItem.value?.chartType === 2 ? 'X-Rm' : selectedItem.value?.chartType === 1 ? 'X-S' : 'X-R')
const chartDefinitions = computed(() => {
  if (!selectedItem.value) return []
  const main: { key: ChartKey; title: string }[] = []
  if (selectedItem.value.chartType !== 2) main.push({ key: 'xbar', title: 'X-Bar Chart' })
  main.push({ key: 'spread', title: selectedItem.value.chartType === 1 ? 'S-Chart' : selectedItem.value.chartType === 2 ? 'Rm-Chart' : 'R-Chart' })
  if (showTrend.value || selectedItem.value.chartType === 2) main.push({ key: 'trend', title: 'Trend Chart' })
  return main
})

function selectItem(item: QCItem): void {
  selectedItem.value = item
  factors.value = (factorFixtures[item.qcItemNo] || []).map((factor) => ({ ...factor }))
  groups.value = groupFixtures.filter((group) => group.qcItemNo === item.qcItemNo).map((group) => ({ ...group }))
  activeGroup.value = groups.value[0] || null
  itemPickerOpen.value = false
  notice.value = `${item.qcItemNo} 已載入 ${groups.value.length} 組管制圖；表格依 FactorSerial 顯示在線要因、首末批次與批數。`
}
function openItemPicker(): void { itemSearch.value = ''; itemPickerOpen.value = true }
function reloadSelected(): void { if (selectedItem.value) selectItem(selectedItem.value) }
function chooseGroup(group: ChartGroup): void { activeGroup.value = group }
function openChart(group = activeGroup.value): void {
  if (!selectedItem.value || !group) { notice.value = '請先選擇 QC Item 與管制圖群組。'; return }
  frequency.value = Math.max(1, Math.min(60, Number(frequency.value) || 6))
  activeGroup.value = group
  selectedChartPoint.value = null
  chartOpen.value = true
  refreshCount.value = 0
  markRefreshed()
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = setInterval(() => { refreshCount.value += 1; markRefreshed() }, Math.max(60_000, frequency.value * 60_000))
}
function closeChart(): void { chartOpen.value = false; if (refreshTimer) clearInterval(refreshTimer); refreshTimer = undefined; notice.value = '管制圖視窗已關閉；主清單保留目前選取群組。' }
function refreshNow(): void { refreshCount.value += 1; markRefreshed() }
function markRefreshed(): void { lastRefresh.value = new Date().toLocaleString('zh-TW', { hour12: false }) }
function saveGraph(): void { notice.value = 'DEV 已模擬儲存圖檔到伺服器；不會建立或上傳影像檔。' }
function selectPoint(chart: ChartKey, index: number): void { selectedChartPoint.value = { chart, index } }
function asFactorValue(group: ChartGroup, factor: Factor): string { return group.factors[factor.factorNo] || '—' }
function yLabel(value: number): string { return Number(value.toFixed(3)).toString() }

const chartData = computed(() => {
  const item = selectedItem.value
  const group = selectedGroup.value
  if (!item || !group) return null
  const count = Math.min(group.lotCount, 32)
  const sampleSize = item.chartType === 2 ? 1 : item.sampleSize
  const batches: Point[][] = []
  for (let index = 0; index < count; index += 1) {
    const drift = Math.sin(index * 0.67 + group.factorSerial.length) * item.sigma * 0.42
    const breach = index === count - 6 ? item.sigma * 3.8 : 0
    const batch: Point[] = []
    for (let sub = 0; sub < sampleSize; sub += 1) {
      const variation = sampleSize === 1 ? Math.sin(index * 0.9) * item.sigma * 1.18 : (sub - (sampleSize - 1) / 2) * item.sigma * 0.46 + Math.cos(index + sub * 1.7) * item.sigma * 0.16
      const value = item.center + drift + breach + variation
      const stamp = new Date(Date.now() - (count - index) * 57 * 60_000 - sub * 4 * 60_000)
      const violation = value < item.lsl || value > item.usl ? 'SPEC' : breach > 0 && sub === 0 ? 'R-04' : ''
      batch.push({ x: index, y: value, serial: `SPC-${group.factorSerial.slice(-4)}-${String(index + 1).padStart(3, '0')}`, lot: `LOT-26K-${String((index * 13 + 108) % 999).padStart(3, '0')}`, time: stamp.toLocaleString('zh-TW', { hour12: false }), value, violation })
    }
    batches.push(batch)
  }
  const means = batches.map((batch) => batch.reduce((sum, point) => sum + point.value, 0) / batch.length)
  const ranges = batches.map((batch, index) => item.chartType === 2 && index > 0
    ? Math.abs(batch[0].value - batches[index - 1][0].value)
    : Math.max(...batch.map((point) => point.value)) - Math.min(...batch.map((point) => point.value)))
  const trend = batches.flat()
  const flat = trend.map((point) => point.value)
  const mean = flat.reduce((sum, value) => sum + value, 0) / flat.length
  const stdDev = Math.sqrt(flat.reduce((sum, value) => sum + (value - mean) ** 2, 0) / flat.length)
  const xbarHalf = item.sigma * (sampleSize === 1 ? 3 : 3 / Math.sqrt(sampleSize))
  const xbarLimits = { ucl: item.center + xbarHalf, cl: item.center, lcl: item.center - xbarHalf }
  const spreadCenter = ranges.reduce((sum, value) => sum + value, 0) / ranges.length
  const spreadLimits = { ucl: spreadCenter * 2.4, cl: spreadCenter, lcl: 0 }
  const trendLimits = { ucl: item.center + item.sigma * 3, cl: item.center, lcl: item.center - item.sigma * 3 }
  const points: Record<ChartKey, Point[]> = {
    xbar: means.map((value, index) => ({ ...batches[index][0], value, violation: value < xbarLimits.lcl || value > xbarLimits.ucl ? 'X-01' : '' })),
    spread: ranges.map((value, index) => ({ ...batches[index][0], value, violation: value > spreadLimits.ucl ? 'R-01' : '' })),
    trend,
  }
  const violations = Object.values(points).flat().filter((point) => point.violation)
  return { batches, points, limits: { xbar: xbarLimits, spread: spreadLimits, trend: trendLimits }, mean, stdDev, max: Math.max(...flat), min: Math.min(...flat), total: flat.length, sampleSize, violations }
})
const selectedPoint = computed(() => {
  if (!selectedChartPoint.value || !chartData.value) return null
  return chartData.value.points[selectedChartPoint.value.chart][selectedChartPoint.value.index] || null
})
const processCapability = computed(() => {
  const item = selectedItem.value
  const data = chartData.value
  if (!item || !data || data.stdDev === 0) return { cp: '—', cpk: '—' }
  const cp = (item.usl - item.lsl) / (6 * data.stdDev)
  const cpk = Math.min((item.usl - data.mean) / (3 * data.stdDev), (data.mean - item.lsl) / (3 * data.stdDev))
  return { cp: cp.toFixed(2), cpk: cpk.toFixed(2) }
})

function seriesFor(key: ChartKey): Point[] { return chartData.value?.points[key] || [] }
function limitsFor(key: ChartKey): { ucl: number; cl: number; lcl: number } { return chartData.value?.limits[key] || { ucl: 1, cl: 0, lcl: -1 } }
function scaleFor(key: ChartKey): { min: number; max: number } {
  const points = seriesFor(key).map((point) => point.value)
  const limit = limitsFor(key)
  const min = Math.min(...points, limit.lcl) - (Math.max(...points, limit.ucl) - Math.min(...points, limit.lcl)) * 0.13
  const max = Math.max(...points, limit.ucl) + (Math.max(...points, limit.ucl) - Math.min(...points, limit.lcl)) * 0.13
  return { min, max }
}
function coordX(key: ChartKey, index: number): number { const length = seriesFor(key).length; return length < 2 ? 430 : 44 + index * (772 / (length - 1)) }
function coordY(key: ChartKey, value: number): number { const { min, max } = scaleFor(key); return 165 - ((value - min) / Math.max(max - min, 0.000001)) * 132 }
function polylineFor(key: ChartKey): string { return seriesFor(key).map((point, index) => `${coordX(key, index)},${coordY(key, point.value)}`).join(' ') }
function violationCount(key: ChartKey): number { return seriesFor(key).filter((point) => !!point.violation).length }
function activeChartPoint(key: ChartKey, index: number): boolean { return selectedChartPoint.value?.chart === key && selectedChartPoint.value.index === index }
function visibleChartLabel(key: ChartKey): string { return key === 'xbar' ? 'X-Bar' : key === 'trend' ? 'Trend' : selectedItem.value?.chartType === 1 ? 'S' : selectedItem.value?.chartType === 2 ? 'Rm' : 'R' }
onBeforeUnmount(() => { if (refreshTimer) clearInterval(refreshTimer) })
</script>

<template>
  <section v-if="props.developmentPreview" class="signboard-page" aria-label="管制圖看板">
    <header class="page-head"><div><p class="eyebrow">SPC / SC205</p><h1>管制圖看板</h1><p class="subtitle">選 QC Item 與 FactorSerial，查看在線量測管制圖及違規點。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="query-panel panel">
      <div class="query-fields">
        <label>QC Data Type<select v-model="selectedType"><option v-for="type in qcDataTypes" :key="type.value" :value="type.value">{{ type.label }}</option></select></label>
        <label class="qc-item-field">QC Item<input :value="selectedItem ? `${selectedItem.qcItemNo} · ${selectedItem.qcItemName}` : ''" readonly placeholder="請按 Get 選擇品管項目"><button class="button" @click="openItemPicker">Get</button></label>
        <label>Frequency (min)<input v-model.number="frequency" type="number" min="1" max="60"></label>
        <label class="trend-check"><input v-model="showTrend" type="checkbox"> Show Trend Chart</label>
        <button class="button primary query-refresh" :disabled="!selectedItem" @click="reloadSelected">Refresh List</button>
      </div>
      <div class="item-strip" v-if="selectedItem"><span><small>QC ITEM</small><b>{{ selectedItem.qcItemNo }}</b><span>{{ selectedItem.qcItemName }}</span></span><span><small>DATA TYPE</small><b>{{ itemDataType }}</b></span><span><small>CHART</small><b>{{ chartModeName }}</b></span><span><small>SAMPLE SIZE</small><b>{{ selectedItem.sampleSize }}</b></span><span><small>SPEC LIMITS</small><b>{{ yLabel(selectedItem.lsl) }} – {{ yLabel(selectedItem.usl) }} {{ selectedItem.unit }}</b></span></div>
    </section>

    <section class="factor-panel panel">
      <header class="panel-head"><div><h2>在線管制圖群組</h2><span>{{ itemGroups.length }} 筆 · 雙擊資料列開啟管制圖</span></div><button class="button" :disabled="!activeGroup" @click="openChart()">Query Chart</button></header>
      <div class="table-scroll"><table><thead><tr><th>FactorSerial</th><th v-for="factor in visibleFactors" :key="factor.factorNo">{{ factor.factorName }}</th><th>First Lot</th><th>Last Lot</th><th>Lot Count</th><th>Key Node</th></tr></thead><tbody>
        <tr v-for="group in itemGroups" :key="group.factorSerial" :class="{ active: activeGroup?.factorSerial === group.factorSerial }" @click="chooseGroup(group)" @dblclick="openChart(group)"><td class="serial">{{ group.factorSerial }}</td><td v-for="factor in visibleFactors" :key="factor.factorNo">{{ asFactorValue(group, factor) }}</td><td>{{ group.firstLotDate }}</td><td>{{ group.lastLotDate }}</td><td class="number">{{ group.lotCount }}</td><td>{{ group.keyNode }}</td></tr>
        <tr v-if="!itemGroups.length"><td :colspan="visibleFactors.length + 5" class="empty">先選擇 QC Item，載入管制圖群組。</td></tr>
      </tbody></table></div>
      <footer class="grid-foot"><span>資料來源模擬：QC Chart Basis + Variable Factor Log</span><span>雙擊列或選取後按 Query Chart</span></footer>
    </section>
    <p class="notice" role="status">{{ notice }}</p>

    <div v-if="itemPickerOpen" class="modal-backdrop" @click.self="itemPickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 QC Item"><header><div><p class="eyebrow">QC ITEM LOOKUP</p><h2>選擇品管項目</h2></div><button class="button" @click="itemPickerOpen = false">Close</button></header><div class="picker-filters"><label>Search<input v-model="itemSearch" autofocus placeholder="QC Item 編號或名稱"></label><span>{{ pickerItems.length }} 個已核准且啟用管制圖的項目</span></div><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" @click="selectItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ dataTypeLabel(item.qcDataType) }}</span><span>{{ item.chartType === 2 ? 'X-Rm' : item.chartType === 1 ? 'X-S' : 'X-R' }} · n={{ item.sampleSize }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item</p></div></section></div>

    <div v-if="chartOpen && selectedGroup && selectedItem && chartData" class="modal-backdrop chart-backdrop" @click.self="closeChart"><section class="chart-dialog" role="dialog" aria-modal="true" aria-label="在線管制圖"><header class="chart-header"><div><p class="eyebrow">LIVE SPC CHART · {{ selectedItem.qcItemNo }}</p><h2>{{ selectedItem.qcItemName }} <span>{{ chartModeName }}</span></h2><p>{{ selectedGroup.factorSerial }} · {{ chartFactorLabel }}</p></div><div class="chart-actions"><span class="refresh-state"><i></i> Updated {{ lastRefresh }}<small>自動刷新間隔 {{ frequency }} 分鐘 · {{ refreshCount }} 次刷新</small></span><button class="button" @click="refreshNow">↻ Refresh</button><button class="button" @click="saveGraph">Save Graph</button><button class="button close-chart" @click="closeChart">Close (X)</button></div></header>
        <div class="chart-status"><div><small>TOTAL SAMPLE</small><b>{{ chartData.total }}</b></div><div><small>SAMPLE SIZE</small><b>{{ chartData.sampleSize }}</b></div><div><small>Cp</small><b>{{ processCapability.cp }}</b></div><div><small>Cpk</small><b>{{ processCapability.cpk }}</b></div><div><small>MAX</small><b>{{ yLabel(chartData.max) }} {{ selectedItem.unit }}</b></div><div><small>MIN</small><b>{{ yLabel(chartData.min) }} {{ selectedItem.unit }}</b></div><div><small>MEAN</small><b>{{ yLabel(chartData.mean) }} {{ selectedItem.unit }}</b></div><div><small>STD DEV</small><b>{{ yLabel(chartData.stdDev) }}</b></div></div>
        <div class="chart-main"><div class="chart-grid" :class="{ 'single-chart': chartDefinitions.length === 1 }"><article v-for="chart in chartDefinitions" :key="chart.key" class="chart-card"><header><div><h3>{{ chart.title }}</h3><span>{{ violationCount(chart.key) }} 個違規點</span></div><span class="chart-legend"><i></i> {{ visibleChartLabel(chart.key) }} <i class="limit-key"></i> UCL / LCL</span></header><svg viewBox="0 0 860 210" preserveAspectRatio="none" role="img" :aria-label="`${chart.title} 假資料折線圖`"><line v-for="line in 4" :key="line" x1="44" :y1="25 + line * 36" x2="816" :y2="25 + line * 36" class="grid-line"/><line x1="44" :y1="coordY(chart.key, limitsFor(chart.key).ucl)" x2="816" :y2="coordY(chart.key, limitsFor(chart.key).ucl)" class="limit-line"/><line x1="44" :y1="coordY(chart.key, limitsFor(chart.key).cl)" x2="816" :y2="coordY(chart.key, limitsFor(chart.key).cl)" class="center-line"/><line x1="44" :y1="coordY(chart.key, limitsFor(chart.key).lcl)" x2="816" :y2="coordY(chart.key, limitsFor(chart.key).lcl)" class="limit-line"/><text x="4" :y="coordY(chart.key, limitsFor(chart.key).ucl) - 4" class="limit-label">UCL {{ yLabel(limitsFor(chart.key).ucl) }}</text><text x="4" :y="coordY(chart.key, limitsFor(chart.key).cl) - 4" class="limit-label center-label">CL {{ yLabel(limitsFor(chart.key).cl) }}</text><text x="4" :y="coordY(chart.key, limitsFor(chart.key).lcl) - 4" class="limit-label">LCL {{ yLabel(limitsFor(chart.key).lcl) }}</text><polyline :points="polylineFor(chart.key)" class="chart-line"/><g v-for="(point, index) in seriesFor(chart.key)" :key="`${point.serial}-${index}`" class="point-hit" @click="selectPoint(chart.key, index)"><circle :cx="coordX(chart.key, index)" :cy="coordY(chart.key, point.value)" :r="activeChartPoint(chart.key, index) ? 6 : point.violation ? 5 : 3.5" :class="{ breached: point.violation, chosen: activeChartPoint(chart.key, index) }"/><title>{{ point.lot }} · {{ point.value.toFixed(4) }} {{ selectedItem.unit }}{{ point.violation ? ` · ${point.violation}` : '' }}</title></g><text x="44" y="202" class="axis-label">Lot / SPC Serial sequence →</text></svg></article></div>
          <aside class="point-detail"><header><h3>選取點明細</h3><span>{{ selectedPoint ? selectedPoint.violation || 'IN CONTROL' : '點圖表資料點' }}</span></header><div v-if="selectedPoint" class="detail-body"><dl><dt>SPC Serial</dt><dd>{{ selectedPoint.serial }}</dd><dt>Lot No</dt><dd>{{ selectedPoint.lot }}</dd><dt>量測時間</dt><dd>{{ selectedPoint.time }}</dd><dt>{{ selectedChartPoint?.chart === 'spread' ? 'Range / S' : '量測值' }}</dt><dd>{{ selectedPoint.value.toFixed(4) }} {{ selectedItem.unit }}</dd><dt>控制判定</dt><dd :class="selectedPoint.violation ? 'bad' : 'good'">{{ selectedPoint.violation ? `違規 ${selectedPoint.violation}` : '管制內' }}</dd></dl><div v-if="selectedPoint.violation" class="rule-note">({{ selectedPoint.violation }}) 模擬超出管制界線或特殊原因法則觸發。</div></div><p v-else class="detail-empty">選取圖上的一個點，查看 Lot、量測時間與違規狀態。</p><div class="legend-note"><span><i class="legend-normal"></i> 管制內</span><span><i class="legend-breach"></i> 違規</span></div></aside>
        </div><footer class="chart-foot"><span>UCL / CL / LCL 及能力指標為 DEV 假資料計算，非正式 SPC 判定。</span><span>最後刷新：{{ lastRefresh }}</span></footer>
      </section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.signboard-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f3f4f5;color:#232a34;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#147d86;font-size:10px;font-weight:700;letter-spacing:.1em}.page-head h1{margin:2px 0;font-size:22px;letter-spacing:.01em}.subtitle{margin:0;color:#737f8c}.button{min-height:29px;padding:4px 10px;border:1px solid #bdc6ce;border-radius:4px;background:#fff;color:#303a45;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#e8f4f4;border-color:#318e95}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{border-color:#147d86;background:#147d86;color:#fff;font-weight:700}.panel{border:1px solid #d8dee3;border-radius:5px;background:#fff;overflow:hidden}.query-panel{margin-bottom:10px}.query-fields{display:flex;align-items:flex-end;gap:10px;padding:11px}.query-fields>label{display:flex;flex-direction:column;gap:4px;color:#5e6974;font-size:10px}.query-fields input:not([type=checkbox]),.query-fields select{height:31px;box-sizing:border-box;border:1px solid #c6cdd3;border-radius:4px;background:#fff;color:#27313c;padding:4px 8px;font:12px "Segoe UI",Arial,sans-serif}.query-fields select{min-width:128px}.qc-item-field{width:min(440px,36vw);position:relative}.qc-item-field input{width:100%;padding-right:58px!important}.qc-item-field .button{position:absolute;right:3px;bottom:3px;min-height:25px;padding:2px 10px}.query-fields>label:nth-child(3) input{width:104px}.trend-check{display:flex;align-items:center;gap:6px;height:31px;white-space:nowrap}.trend-check input{accent-color:#147d86}.query-refresh{min-width:98px}.item-strip{display:flex;align-items:center;gap:0;padding:8px 12px;border-top:1px solid #e7ebee;background:#f8fafb}.item-strip>span{display:flex;flex-direction:column;gap:1px;padding:0 15px;border-right:1px solid #e0e5e8;color:#52606d;font-size:10px}.item-strip>span:first-child{padding-left:0}.item-strip>span:last-child{border-right:0}.item-strip small{font-size:8px;color:#8a959f;font-weight:700;letter-spacing:.08em}.item-strip b{color:#263743;font-size:11px}.panel-head{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:8px 11px;border-bottom:1px solid #e4e9ec;background:#f9fbfc}.panel-head h2{margin:0;font-size:14px}.panel-head span{display:block;margin-top:2px;color:#7a858f;font-size:10px}.table-scroll{max-height:calc(100% - 75px);overflow:auto}.table-scroll table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.table-scroll th,.table-scroll td{padding:8px 9px;border-bottom:1px solid #edf0f2;text-align:left}.table-scroll th{position:sticky;top:0;z-index:1;background:#eef3f5;color:#56616b;font-size:10px;font-weight:700}.table-scroll tbody tr{cursor:pointer}.table-scroll tbody tr:hover,.table-scroll tbody tr.active{background:#e6f3f3}.table-scroll td.serial{color:#176d75;font-weight:700}.table-scroll td.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;color:#89939b!important;padding:24px!important}.grid-foot{display:flex;justify-content:space-between;padding:7px 10px;color:#82909a;font-size:9px}.notice{margin:10px 0 0;padding:8px 10px;border:1px solid #c9dddd;border-radius:4px;background:#eef7f7;color:#34676c;font-size:11px}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:grid;place-items:center;padding:14px;background:#16212d88}.picker-dialog,.chart-dialog{width:min(850px,96vw);max-height:94vh;overflow:auto;border:1px solid #c7d0d6;border-radius:6px;background:#f8fafb;box-shadow:0 18px 60px #0d1a2480}.picker-dialog{padding:14px}.picker-dialog>header{display:flex;justify-content:space-between;align-items:center}.picker-dialog h2{margin:2px 0;font-size:19px}.picker-filters{display:flex;align-items:flex-end;gap:15px;padding:12px 0}.picker-filters label{display:flex;flex:1;flex-direction:column;gap:4px;color:#697580;font-size:10px}.picker-filters input{height:32px;border:1px solid #c8d0d6;border-radius:4px;padding:5px 9px;font:12px "Segoe UI",Arial,sans-serif}.picker-filters>span{padding-bottom:8px;color:#82909a;font-size:10px}.picker-list{border:1px solid #dce2e6;border-radius:4px;overflow:hidden}.picker-row{display:grid;grid-template-columns:150px 1fr 90px 115px;gap:10px;width:100%;padding:10px;border:0;border-bottom:1px solid #e7ebee;background:#fff;text-align:left;color:#47545e;font:11px "Segoe UI",Arial,sans-serif;cursor:pointer}.picker-row:last-child{border-bottom:0}.picker-row:hover{background:#e8f5f4}.picker-row b{color:#176d75}.chart-backdrop{z-index:1250}.chart-dialog{width:min(1480px,98vw);padding:0}.chart-header{display:flex;justify-content:space-between;align-items:center;gap:15px;padding:12px 15px;border-bottom:1px solid #dce3e7;background:#fff}.chart-header h2{margin:2px 0;font-size:18px}.chart-header h2 span{padding:3px 7px;border-radius:3px;background:#e6f2f3;color:#16727a;font-size:10px;vertical-align:middle}.chart-header p:last-child{margin:4px 0 0;color:#75828d;font-size:10px}.chart-actions{display:flex;align-items:center;gap:6px}.chart-actions .button{white-space:nowrap;font-size:10px}.refresh-state{display:flex;flex-direction:column;align-items:flex-end;padding-right:6px;color:#53626c;font-size:9px;white-space:nowrap}.refresh-state i{display:inline-block;width:7px;height:7px;margin-right:4px;border-radius:50%;background:#35a36d;box-shadow:0 0 0 3px #e4f4ea}.refresh-state small{margin-top:3px;color:#87939b;font-size:8px}.close-chart{border-color:#3c4c58;background:#3c4c58;color:#fff}.chart-status{display:grid;grid-template-columns:repeat(8,minmax(70px,1fr));gap:1px;border-bottom:1px solid #dce3e7;background:#dce3e7}.chart-status>div{display:flex;flex-direction:column;gap:3px;padding:8px 10px;background:#f4f7f8}.chart-status small{color:#89949b;font-size:8px;font-weight:700;letter-spacing:.08em}.chart-status b{color:#33424c;font-size:12px;font-variant-numeric:tabular-nums}.chart-main{display:grid;grid-template-columns:minmax(0,1fr) 250px;gap:9px;padding:10px}.chart-grid{display:grid;grid-template-columns:1fr 1fr;gap:9px}.chart-grid.single-chart{grid-template-columns:1fr}.chart-card{min-width:0;border:1px solid #dde4e8;border-radius:4px;background:#fff;overflow:hidden}.chart-card header{display:flex;align-items:center;justify-content:space-between;padding:6px 9px;border-bottom:1px solid #edf0f2}.chart-card h3{display:inline;margin:0;font-size:11px}.chart-card header span{margin-left:8px;color:#849099;font-size:9px}.chart-legend{display:inline-flex!important;align-items:center;gap:5px}.chart-legend i{width:7px;height:7px;border-radius:50%;background:#168590}.chart-legend i.limit-key{width:12px;height:0;border-top:1px dashed #d79645;border-radius:0;background:transparent}.chart-card svg{display:block;width:100%;height:165px}.grid-line{stroke:#edf1f3;stroke-width:1}.limit-line{stroke:#d79645;stroke-width:1;stroke-dasharray:5 3}.center-line{stroke:#a4b1b8;stroke-width:1;stroke-dasharray:3 3}.limit-label{fill:#b57528;font-size:9px}.center-label{fill:#71818b}.chart-line{fill:none;stroke:#168590;stroke-width:2}.point-hit{cursor:pointer}.point-hit circle{fill:#168590;stroke:#fff;stroke-width:1.5}.point-hit circle.breached{fill:#cf4c45}.point-hit circle.chosen{stroke:#1d2e3a;stroke-width:2.5}.axis-label{fill:#8b969d;font-size:9px}.point-detail{display:flex;flex-direction:column;min-height:100%;border:1px solid #dde4e8;border-radius:4px;background:#fff}.point-detail>header{display:flex;justify-content:space-between;align-items:center;padding:8px;border-bottom:1px solid #edf0f2}.point-detail h3{margin:0;font-size:11px}.point-detail>header span{color:#839099;font-size:8px}.detail-body{padding:9px}.detail-body dl{display:grid;grid-template-columns:76px 1fr;gap:7px 5px;margin:0;font-size:9px}.detail-body dt{color:#839099}.detail-body dd{margin:0;color:#42525d;overflow-wrap:anywhere}.detail-body dd.bad{color:#bb3933;font-weight:700}.detail-body dd.good{color:#438155}.rule-note{margin-top:12px;padding:8px;border:1px solid #efc7c2;border-radius:3px;background:#fff3f1;color:#a24239;font-size:9px}.detail-empty{margin:auto;padding:14px;color:#929ca3;text-align:center;font-size:10px}.legend-note{display:flex;justify-content:center;gap:12px;margin-top:auto;padding:8px;border-top:1px solid #edf0f2;color:#829099;font-size:8px}.legend-note span{display:flex;align-items:center;gap:4px}.legend-note i{width:7px;height:7px;border-radius:50%}.legend-normal{background:#168590}.legend-breach{background:#cf4c45}.chart-foot{display:flex;justify-content:space-between;padding:7px 12px;border-top:1px solid #e0e6e9;color:#8a969d;font-size:8px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1040px){.query-fields{flex-wrap:wrap}.qc-item-field{width:min(460px,100%)}.item-strip{flex-wrap:wrap;gap:8px}.item-strip>span{padding:0 8px}.chart-header{align-items:flex-start;flex-direction:column}.chart-actions{width:100%;justify-content:flex-end}.chart-main{grid-template-columns:1fr}.point-detail{min-height:175px}.chart-grid{grid-template-columns:1fr}}
@media(max-width:680px){.chart-status{grid-template-columns:repeat(4,1fr)}.chart-actions{flex-wrap:wrap}.refresh-state{width:100%;align-items:flex-start}.picker-row{grid-template-columns:1fr 1fr}.query-fields>label{width:calc(50% - 7px)}.qc-item-field{width:100%!important}.query-refresh{flex:1}}
</style>

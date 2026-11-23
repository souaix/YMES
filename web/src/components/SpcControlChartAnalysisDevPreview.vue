<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'

type DataType = 0 | 1 | 2
type ChartType = 0 | 1 | 2
type ViewMode = 'chart' | 'data' | 'histogram'
type ChartKey = 'xbar' | 'spread' | 'trend'
type QCItem = { qcItemNo: string; qcItemName: string; qcDataType: DataType; chartType: ChartType; sampleSize: number; center: number; sigma: number; unit: string; lsl: number; usl: number; issueState: 2 | -1 }
type Factor = { factorNo: string; factorName: string; online: boolean; shortRun: boolean }
type ChartGroup = { factorSerial: string; qcItemNo: string; isShortRun: boolean; keyNode: 'I' | 'C' | 'N'; firstLotDate: string; lastLotDate: string; lotCount: number; factors: Record<string, string> }
type Measure = { index: number; serialNo: string; spcSerial: string; lotNo: string; createDate: string; createdAt: number; factorValues: Record<string, string>; value: number; lotState: 0 | 1; erfNo: string; holdRule: string; violation: string }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const itemFixtures: QCItem[] = [
  { qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', qcDataType: 1, chartType: 0, sampleSize: 5, center: 0.412, sigma: 0.028, unit: 'mm', lsl: 0.320, usl: 0.500, issueState: 2 },
  { qcItemNo: 'QC-V-2002', qcItemName: 'Die Shear Strength', qcDataType: 2, chartType: 1, sampleSize: 5, center: 14.8, sigma: 0.62, unit: 'N', lsl: 12.5, usl: 17.2, issueState: 2 },
  { qcItemNo: 'QC-V-3001', qcItemName: 'Wafer Thickness', qcDataType: 0, chartType: 2, sampleSize: 1, center: 725.0, sigma: 2.1, unit: 'μm', lsl: 718.5, usl: 731.5, issueState: 2 },
  { qcItemNo: 'QC-V-3108', qcItemName: 'Mold Flash Height', qcDataType: 1, chartType: 0, sampleSize: 5, center: 0.072, sigma: 0.006, unit: 'mm', lsl: 0.052, usl: 0.092, issueState: -1 },
]
const factorFixtures: Record<string, Factor[]> = {
  'QC-V-1001': [{ factorNo: 'MACHINE', factorName: '機台', online: true, shortRun: true }, { factorNo: 'LINE', factorName: '產線', online: true, shortRun: false }, { factorNo: 'OPERATOR', factorName: '作業員', online: false, shortRun: true }],
  'QC-V-2002': [{ factorNo: 'MACHINE', factorName: '機台', online: true, shortRun: true }, { factorNo: 'MATERIAL', factorName: '材料批號', online: true, shortRun: true }],
  'QC-V-3001': [{ factorNo: 'EQUIPMENT', factorName: '設備', online: true, shortRun: true }, { factorNo: 'CHAMBER', factorName: '腔體', online: true, shortRun: true }],
  'QC-V-3108': [{ factorNo: 'MACHINE', factorName: '機台', online: true, shortRun: true }, { factorNo: 'MOLD', factorName: '模具', online: true, shortRun: true }],
}
const groupFixtures: ChartGroup[] = [
  { factorSerial: 'FS-2604-0182', qcItemNo: 'QC-V-1001', isShortRun: false, keyNode: 'I', firstLotDate: '2026/11/22 06:10:22', lastLotDate: '2026/11/23 08:42:17', lotCount: 28, factors: { MACHINE: 'MCH-03', LINE: 'LN-A', OPERATOR: 'U11019' } },
  { factorSerial: 'FS-2604-0181', qcItemNo: 'QC-V-1001', isShortRun: false, keyNode: 'C', firstLotDate: '2026/11/21 14:05:02', lastLotDate: '2026/11/22 05:53:31', lotCount: 22, factors: { MACHINE: 'MCH-02', LINE: 'LN-A', OPERATOR: 'U11023' } },
  { factorSerial: 'FS-2604-0179', qcItemNo: 'QC-V-1001', isShortRun: false, keyNode: 'N', firstLotDate: '2026/11/20 08:32:41', lastLotDate: '2026/11/21 13:50:04', lotCount: 24, factors: { MACHINE: 'MCH-03', LINE: 'LN-B', OPERATOR: 'U11021' } },
  { factorSerial: 'FS-SR-0042', qcItemNo: 'QC-V-1001', isShortRun: true, keyNode: 'I', firstLotDate: '2026/11/18 10:10:08', lastLotDate: '2026/11/23 08:04:36', lotCount: 18, factors: { MACHINE: 'MCH-07', LINE: 'LN-C', OPERATOR: 'U11007' } },
  { factorSerial: 'FS-2604-0288', qcItemNo: 'QC-V-2002', isShortRun: false, keyNode: 'I', firstLotDate: '2026/11/22 07:45:20', lastLotDate: '2026/11/23 08:13:51', lotCount: 19, factors: { MACHINE: 'DS-08', MATERIAL: 'MAT-26K-031' } },
  { factorSerial: 'FS-SR-0068', qcItemNo: 'QC-V-2002', isShortRun: true, keyNode: 'C', firstLotDate: '2026/11/19 09:31:08', lastLotDate: '2026/11/22 23:58:33', lotCount: 16, factors: { MACHINE: 'DS-11', MATERIAL: 'MAT-26K-034' } },
  { factorSerial: 'FS-2604-0041', qcItemNo: 'QC-V-3001', isShortRun: false, keyNode: 'C', firstLotDate: '2026/11/22 02:11:08', lastLotDate: '2026/11/23 08:26:47', lotCount: 31, factors: { EQUIPMENT: 'THK-12', CHAMBER: 'CH-04' } },
  { factorSerial: 'FS-SR-0016', qcItemNo: 'QC-V-3001', isShortRun: true, keyNode: 'N', firstLotDate: '2026/11/20 17:42:31', lastLotDate: '2026/11/22 07:39:10', lotCount: 15, factors: { EQUIPMENT: 'THK-15', CHAMBER: 'CH-03' } },
  { factorSerial: 'FS-2604-0318', qcItemNo: 'QC-V-3108', isShortRun: false, keyNode: 'I', firstLotDate: '2026/11/21 16:35:11', lastLotDate: '2026/11/23 08:18:55', lotCount: 26, factors: { MACHINE: 'MOLD-05', MOLD: 'MOLD-7' } },
]
const dataTypeOptions = [{ value: '', label: '全部' }, { value: '0', label: 'Particle' }, { value: '1', label: 'InLine' }, { value: '2', label: 'OffLine' }]
const keyNodeOptions = [{ value: '', label: '全部' }, { value: 'I', label: 'I' }, { value: 'C', label: 'C' }, { value: 'N', label: 'N' }]
const analysisModes: ViewMode[] = ['chart', 'data', 'histogram']
const itemType = ref('')
const pickerSearch = ref('')
const pickerOpen = ref(false)
const selectedItem = ref<QCItem | null>(null)
const factors = ref<Factor[]>([])
const groups = ref<ChartGroup[]>([])
const activeGroup = ref<ChartGroup | null>(null)
const chartTab = ref<'standard' | 'shortRun'>('standard')
const keyNode = ref('')
const queryByDate = ref(false)
const queryByLot = ref(false)
const fromDate = ref('')
const endDate = ref('')
const lastLots = ref<number | ''>('')
const showAllData = ref(false)
const showErfPoint = ref(true)
const dataInOnePage = ref(true)
const showBarChart = ref(true)
const showHoldRules = ref(true)
const analysisOpen = ref(false)
const analysisMode = ref<ViewMode>('chart')
const selectedPoint = ref<{ chart: ChartKey; index: number } | null>(null)
const showAllInAnalysis = ref(false)
const showErfInAnalysis = ref(true)
const showHoldInAnalysis = ref(true)
const analysisPage = ref(0)
const refreshMinutes = ref(5)
const refreshCount = ref(0)
const refreshedAt = ref('尚未載入')
const notice = ref('請選擇 QC Item 後載入一般管制圖與短程圖群組。')
let refreshTimer: ReturnType<typeof setInterval> | undefined

function dateOffset(days: number): string {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
fromDate.value = dateOffset(-60)
endDate.value = dateOffset(0)
const selectedGroups = computed(() => groups.value.filter((group) => group.qcItemNo === selectedItem.value?.qcItemNo && group.isShortRun === (chartTab.value === 'shortRun') && (!keyNode.value || group.keyNode === keyNode.value)))
const pickerItems = computed(() => itemFixtures.filter((item) => (itemType.value === '' || item.qcDataType === Number(itemType.value)) && item.issueState === 2 && `${item.qcItemNo} ${item.qcItemName}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const onlineFactors = computed(() => factors.value.filter((factor) => chartTab.value === 'standard' ? factor.online : factor.shortRun))
const selectedChartMode = computed(() => selectedItem.value?.chartType === 2 ? 'X-Rm' : selectedItem.value?.chartType === 1 ? 'X-S' : 'X-R')
const selectedGroup = computed(() => groups.value.find((group) => group.factorSerial === activeGroup.value?.factorSerial) || null)
const selectedGroupRow = computed(() => selectedGroups.value.find((group) => group.factorSerial === activeGroup.value?.factorSerial) || null)
const selectedFactorSummary = computed(() => {
  const group = selectedGroup.value
  return group ? onlineFactors.value.map((factor) => `${factor.factorName}: ${factorValue(group, factor)}`).join(' / ') || '無在線要因' : '—'
})

function dataTypeName(type: DataType): string { return dataTypeOptions.find((option) => option.value === String(type))?.label || 'InLine' }
function chartTypeName(type: ChartType): string { return type === 2 ? 'X-Rm' : type === 1 ? 'X-S' : 'X-R' }
function factorValue(group: ChartGroup, factor: Factor): string { return group.factors[factor.factorNo] || '—' }
function chooseItem(item: QCItem): void {
  selectedItem.value = item
  factors.value = (factorFixtures[item.qcItemNo] || []).map((factor) => ({ ...factor }))
  groups.value = groupFixtures.filter((group) => group.qcItemNo === item.qcItemNo).map((group) => ({ ...group }))
  keyNode.value = ''
  chartTab.value = 'standard'
  activeGroup.value = groups.value.find((group) => !group.isShortRun) || groups.value[0] || null
  pickerOpen.value = false
  notice.value = `${item.qcItemNo} 載入 ${groups.value.filter((group) => !group.isShortRun).length} 張一般管制圖、${groups.value.filter((group) => group.isShortRun).length} 張短程圖。`
}
function reloadList(): void { if (selectedItem.value) chooseItem(selectedItem.value) }
function openPicker(): void { pickerSearch.value = ''; pickerOpen.value = true }
function changeTab(tab: 'standard' | 'shortRun'): void {
  chartTab.value = tab
  activeGroup.value = selectedGroups.value[0] || null
}
function activateGroup(group: ChartGroup): void { activeGroup.value = group }
function startRefreshTimer(): void {
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = setInterval(() => { refreshCount.value += 1; markRefreshed() }, Math.max(60_000, refreshMinutes.value * 60_000))
}
function markRefreshed(): void { refreshedAt.value = new Date().toLocaleString('zh-TW', { hour12: false }) }
function refreshAnalysis(): void { refreshCount.value += 1; markRefreshed(); notice.value = 'DEV 假資料已重新計算圖表，不重新查詢服務或資料庫。' }
function closeAnalysis(): void {
  analysisOpen.value = false
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = undefined
  selectedPoint.value = null
}
function openAnalysis(mode: ViewMode): void {
  if (!selectedItem.value || !selectedGroupRow.value) { notice.value = '請先選取一筆管制圖群組。'; return }
  if (queryByLot.value && (!Number.isInteger(Number(lastLots.value)) || Number(lastLots.value) <= 0)) { notice.value = '啟用 Query by Lot 時，請輸入大於 0 的整數批數。'; return }
  if (queryByDate.value && (!fromDate.value || !endDate.value || fromDate.value > endDate.value)) { notice.value = '日期區間起訖必須完整且起日不得晚於迄日。'; return }
  analysisMode.value = mode
  showAllInAnalysis.value = showAllData.value
  showErfInAnalysis.value = showErfPoint.value
  showHoldInAnalysis.value = showHoldRules.value
  analysisPage.value = 0
  selectedPoint.value = null
  refreshCount.value = 0
  markRefreshed()
  analysisOpen.value = true
  startRefreshTimer()
}
function typeLabel(mode: ViewMode): string { return mode === 'data' ? 'Raw Data' : mode === 'histogram' ? 'Histogram' : 'Control Chart' }
function saveChart(): void { notice.value = 'DEV 已模擬 Save Graph；不會產生或上傳圖檔。' }
function printChart(): void { notice.value = 'DEV 已模擬列印圖表；未呼叫本機列印服務。' }
function fmt(value: number): string { return Number(value.toFixed(3)).toString() }

const allMeasures = computed<Measure[]>(() => {
  const item = selectedItem.value
  const group = selectedGroup.value
  if (!item || !group) return []
  const count = Math.max(8, Math.min(group.lotCount, 40))
  const sampleSize = item.chartType === 2 ? 1 : item.sampleSize
  const measures: Measure[] = []
  for (let batch = 0; batch < count; batch += 1) {
    const drift = Math.sin(batch * 0.61 + group.factorSerial.length) * item.sigma * 0.38
    const special = batch === count - 5 ? item.sigma * 3.7 : 0
    const lotState: 0 | 1 = batch % 11 === 8 ? 1 : 0
    for (let sample = 0; sample < sampleSize; sample += 1) {
      const within = sampleSize === 1 ? Math.cos(batch * 0.8) * item.sigma * 1.1 : (sample - (sampleSize - 1) / 2) * item.sigma * 0.42 + Math.cos(batch + sample * 1.3) * item.sigma * 0.12
      const value = item.center + drift + special + within
      const timestamp = new Date(Date.now() - (count - batch) * 58 * 60_000 - sample * 4 * 60_000)
      const date = timestamp.toLocaleString('zh-TW', { hour12: false })
      const factorValues = { ...group.factors }
      measures.push({
        index: batch * sampleSize + sample,
        serialNo: String(sample + 1),
        spcSerial: `SPC-${group.factorSerial.slice(-4)}-${String(batch + 1).padStart(3, '0')}`,
        lotNo: `LOT-26K-${String((batch * 13 + 108) % 999).padStart(3, '0')}`,
        createDate: date,
        createdAt: timestamp.getTime(),
        factorValues,
        value,
        lotState,
        erfNo: batch % 13 === 4 && sample === 0 ? `ERF-26K-${String(batch + 182).padStart(4, '0')}` : '',
        holdRule: batch === count - 8 ? 'R-07 · 連續 7 點單側分布' : '',
        violation: value < item.lsl || value > item.usl ? 'SPEC' : special > 0 && sample === 0 ? 'R-04' : '',
      })
    }
  }
  return measures
})
const filteredMeasures = computed(() => {
  let rows = allMeasures.value.filter((measure) => showAllInAnalysis.value || measure.lotState === 0)
  if (queryByDate.value) {
    const from = new Date(`${fromDate.value}T00:00:00`).getTime()
    const end = new Date(`${endDate.value}T23:59:59`).getTime()
    rows = rows.filter((measure) => measure.createdAt >= from && measure.createdAt <= end)
  }
  if (queryByLot.value && Number(lastLots.value) > 0) {
    const lotNos = [...new Set(rows.map((measure) => measure.lotNo))].slice(-Number(lastLots.value))
    rows = rows.filter((measure) => lotNos.includes(measure.lotNo))
  }
  return rows
})
const batches = computed(() => {
  const grouped = new Map<string, Measure[]>()
  for (const measure of filteredMeasures.value) {
    const values = grouped.get(measure.spcSerial) || []
    values.push(measure)
    grouped.set(measure.spcSerial, values)
  }
  return [...grouped.values()].sort((left, right) => left[0].index - right[0].index)
})
const batchMeans = computed(() => batches.value.map((batch) => batch.reduce((sum, point) => sum + point.value, 0) / batch.length))
const spreads = computed(() => batches.value.map((batch, index) => selectedItem.value?.chartType === 2 && index > 0
  ? Math.abs(batch[0].value - batches.value[index - 1][0].value)
  : Math.max(...batch.map((point) => point.value)) - Math.min(...batch.map((point) => point.value))))
const chartDefinitions = computed(() => {
  if (!selectedItem.value) return []
  const charts: { key: ChartKey; title: string }[] = []
  if (selectedItem.value.chartType !== 2) charts.push({ key: 'xbar', title: 'X-Bar Chart' })
  charts.push({ key: 'spread', title: selectedItem.value.chartType === 1 ? 'S-Chart' : selectedItem.value.chartType === 2 ? 'Rm-Chart' : 'R-Chart' })
  charts.push({ key: 'trend', title: 'Trend Chart' })
  return charts
})
function fullSeries(key: ChartKey): number[] {
  if (key === 'xbar') return batchMeans.value
  if (key === 'spread') return spreads.value
  return filteredMeasures.value.map((measure) => measure.value)
}
const chartPageSize = 18
function seriesFor(key: ChartKey): number[] {
  const series = fullSeries(key)
  if (dataInOnePage.value) return series
  if (key === 'trend') {
    const sampleSize = Math.max(1, selectedItem.value?.sampleSize || 1)
    return series.slice(analysisPage.value * chartPageSize * sampleSize, (analysisPage.value + 1) * chartPageSize * sampleSize)
  }
  return series.slice(analysisPage.value * chartPageSize, (analysisPage.value + 1) * chartPageSize)
}
function limitsFor(key: ChartKey): { ucl: number; cl: number; lcl: number } {
  const item = selectedItem.value
  const values = fullSeries(key)
  if (!item || !values.length) return { ucl: 1, cl: 0, lcl: -1 }
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length
  if (key === 'spread') {
    const ucl = mean * (item.chartType === 1 ? 1.65 : 2.35)
    return { ucl, cl: mean, lcl: 0 }
  }
  const half = item.sigma * (key === 'trend' || item.sampleSize === 1 ? 3 : 3 / Math.sqrt(item.sampleSize))
  return { ucl: item.center + half, cl: item.center, lcl: item.center - half }
}
function scaleFor(key: ChartKey): { min: number; max: number } {
  const values = fullSeries(key)
  const limits = limitsFor(key)
  const range = Math.max(Math.max(...values, limits.ucl) - Math.min(...values, limits.lcl), 0.00001)
  return { min: Math.min(...values, limits.lcl) - range * 0.15, max: Math.max(...values, limits.ucl) + range * 0.15 }
}
function pointX(key: ChartKey, index: number): number { const length = seriesFor(key).length; return length <= 1 ? 430 : 48 + index * (760 / (length - 1)) }
function pointY(key: ChartKey, value: number): number { const scale = scaleFor(key); return 159 - ((value - scale.min) / Math.max(scale.max - scale.min, 0.000001)) * 128 }
function linePoints(key: ChartKey): string { return seriesFor(key).map((value, index) => `${pointX(key, index)},${pointY(key, value)}`).join(' ') }
function seriesViolation(key: ChartKey, index: number): string {
  if (key === 'trend') return seriesFor(key)[index] < (selectedItem.value?.lsl || -Infinity) || seriesFor(key)[index] > (selectedItem.value?.usl || Infinity) ? 'SPEC' : ''
  const limits = limitsFor(key)
  const value = seriesFor(key)[index]
  return value < limits.lcl || value > limits.ucl ? key === 'xbar' ? 'X-01' : 'R-01' : ''
}
function selectChartPoint(key: ChartKey, index: number): void { selectedPoint.value = { chart: key, index } }
function selectedMeasureBatch(): Measure[] {
  if (!selectedPoint.value) return []
  if (selectedPoint.value.chart === 'trend') {
    const series = seriesFor('trend')
    const pageStart = dataInOnePage.value ? 0 : analysisPage.value * chartPageSize * Math.max(1, selectedItem.value?.sampleSize || 1)
    const row = filteredMeasures.value[pageStart + selectedPoint.value.index]
    return row ? [row] : []
  }
  const chartPointIndex = dataInOnePage.value ? selectedPoint.value.index : analysisPage.value * chartPageSize + selectedPoint.value.index
  return batches.value[chartPointIndex] || []
}
const selectedPointMeasures = computed(() => selectedMeasureBatch())
const selectedPointValue = computed(() => selectedPointMeasures.value.length
  ? selectedPointMeasures.value.reduce((sum, point) => sum + point.value, 0) / selectedPointMeasures.value.length
  : null)
const selectedPointAlert = computed(() => selectedPointMeasures.value.find((point) => point.violation || point.erfNo || point.holdRule) || null)
const statistics = computed(() => {
  const item = selectedItem.value
  const values = filteredMeasures.value.map((measure) => measure.value)
  if (!item || !values.length) return { total: 0, lots: 0, mean: 0, stdDev: 0, min: 0, max: 0, cp: '—', cpk: '—', excluded: allMeasures.value.length }
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length
  const stdDev = Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length)
  const cp = stdDev ? (item.usl - item.lsl) / (6 * stdDev) : 0
  const cpk = stdDev ? Math.min((item.usl - mean) / (3 * stdDev), (mean - item.lsl) / (3 * stdDev)) : 0
  return { total: values.length, lots: batches.value.length, mean, stdDev, min: Math.min(...values), max: Math.max(...values), cp: cp.toFixed(2), cpk: cpk.toFixed(2), excluded: allMeasures.value.length - values.length }
})
const histogramBins = computed(() => {
  const values = filteredMeasures.value.map((measure) => measure.value)
  if (!values.length) return []
  const min = Math.min(...values)
  const max = Math.max(...values)
  const span = Math.max(max - min, selectedItem.value?.sigma || 1)
  const size = span / 8
  return Array.from({ length: 8 }, (_, index) => {
    const start = min + index * size
    const end = index === 7 ? max + 0.000001 : start + size
    const count = values.filter((value) => value >= start && value < end).length
    return { index, start, end, count, height: count }
  })
})
const violationCount = computed(() => filteredMeasures.value.filter((measure) => measure.violation).length)
const erfCount = computed(() => filteredMeasures.value.filter((measure) => measure.erfNo).length)
const ruleCount = computed(() => filteredMeasures.value.filter((measure) => measure.holdRule).length)
const maxPage = computed(() => Math.max(0, Math.ceil(batches.value.length / chartPageSize) - 1))
const canGoNext = computed(() => !dataInOnePage.value && analysisPage.value < maxPage.value)

function changeAnalysisMode(mode: ViewMode): void { analysisMode.value = mode; selectedPoint.value = null }
function adjustPage(direction: -1 | 1): void { analysisPage.value = Math.min(maxPage.value, Math.max(0, analysisPage.value + direction)); selectedPoint.value = null }
function indicator(key: ChartKey, index: number): string {
  if (key === 'trend') {
    const sampleSize = Math.max(1, selectedItem.value?.sampleSize || 1)
    const pageStart = dataInOnePage.value ? 0 : analysisPage.value * chartPageSize * sampleSize
    return filteredMeasures.value[pageStart + index]?.erfNo || ''
  }
  const batchIndex = dataInOnePage.value ? index : analysisPage.value * chartPageSize + index
  return batches.value[batchIndex]?.[0]?.erfNo || ''
}
function pointIsErf(key: ChartKey, index: number): boolean { return !!indicator(key, index) }
function pointIsRule(key: ChartKey, index: number): boolean {
  if (key === 'trend') {
    const sampleSize = Math.max(1, selectedItem.value?.sampleSize || 1)
    const pageStart = dataInOnePage.value ? 0 : analysisPage.value * chartPageSize * sampleSize
    return !!filteredMeasures.value[pageStart + index]?.holdRule
  }
  const batchIndex = dataInOnePage.value ? index : analysisPage.value * chartPageSize + index
  return !!batches.value[batchIndex]?.some((point) => point.holdRule)
}
function viewModeLabel(mode: ViewMode): string { return mode === 'chart' ? '圖表' : mode === 'data' ? '檢測資料' : 'Histogram' }
function shortDate(value: string): string { return value.replace(/:\d{2}$/, '') }
function displayViolation(measure: Measure): string { return measure.violation || (showHoldInAnalysis.value ? measure.holdRule : '') || '' }
function chartLabel(key: ChartKey): string { return key === 'xbar' ? 'X-Bar' : key === 'trend' ? 'Trend' : selectedItem.value?.chartType === 1 ? 'S' : selectedItem.value?.chartType === 2 ? 'Rm' : 'R' }
function yTick(key: ChartKey, value: number): string { return fmt(value) }
function pointSelected(key: ChartKey, index: number): boolean { return selectedPoint.value?.chart === key && selectedPoint.value.index === index }

onBeforeUnmount(() => { if (refreshTimer) clearInterval(refreshTimer) })
</script>

<template>
  <section v-if="props.developmentPreview" class="analysis-page" aria-label="管制圖分析">
    <header class="page-head"><div><p class="eyebrow">SPC / SC206</p><h1>管制圖分析</h1><p class="subtitle">查詢一般／短程管制圖，並以圖表、檢測資料或 Histogram 檢視假資料。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="criteria panel">
      <div class="criteria-row">
        <label>QC Data Type<select v-model="itemType"><option v-for="option in dataTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <label class="item-field">QC Item<input :value="selectedItem ? `${selectedItem.qcItemNo} · ${selectedItem.qcItemName}` : ''" readonly placeholder="請選擇已設定管制圖的 QC Item"><button class="button" @click="openPicker">Get</button></label>
        <label>KeyNode<select v-model="keyNode"><option v-for="option in keyNodeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <button class="button primary" :disabled="!selectedItem" @click="reloadList">Refresh List</button>
      </div>
      <div class="filter-row">
        <label class="check"><input v-model="queryByDate" type="checkbox"> Query by Date</label><label>From<input v-model="fromDate" type="date" :disabled="!queryByDate"></label><label>End<input v-model="endDate" type="date" :disabled="!queryByDate"></label>
        <span class="divider"></span><label class="check"><input v-model="queryByLot" type="checkbox"> Query by Lot</label><label>Last Lots<input v-model.number="lastLots" type="number" min="1" :disabled="!queryByLot" placeholder="批數"></label>
        <span class="filter-hint">日期／批數條件套用於開啟圖表的資料範圍</span>
      </div>
      <div class="options-row">
        <label class="check"><input v-model="showAllData" type="checkbox"> Show All Data</label>
        <label class="check"><input v-model="showErfPoint" type="checkbox"> Show ERF Marks</label>
        <label class="check"><input v-model="dataInOnePage" type="checkbox"> Data in One Page</label>
        <label class="check"><input v-model="showBarChart" type="checkbox"> Show BarChart</label>
        <label class="check hold-check"><input v-model="showHoldRules" type="checkbox"> Show Collect Hold Rules judged at the moment</label>
      </div>
      <div v-if="selectedItem" class="item-summary"><span><small>QC ITEM</small><b>{{ selectedItem.qcItemNo }}</b><span>{{ selectedItem.qcItemName }}</span></span><span><small>TYPE</small><b>{{ dataTypeName(selectedItem.qcDataType) }}</b></span><span><small>CHART</small><b>{{ selectedChartMode }}</b></span><span><small>ISSUE STATE</small><b>{{ selectedItem.issueState === 2 ? 'Released' : 'Stopped' }}</b></span></div>
    </section>

    <section class="groups-panel panel">
      <header class="panel-head"><div><h2>{{ chartTab === 'standard' ? 'QCChart' : 'ShortrunChart' }}</h2><span>{{ selectedGroups.length }} 張管制圖 · 點選列後使用下方檢視按鈕</span></div><div class="view-actions"><button class="button" :disabled="!selectedGroupRow" @click="openAnalysis('data')">Query Data</button><button class="button primary" :disabled="!selectedGroupRow" @click="openAnalysis('chart')">Query Chart</button><button class="button" :disabled="!selectedGroupRow" @click="openAnalysis('histogram')">Histogram</button></div></header>
      <div class="tabs" role="tablist"><button role="tab" :aria-selected="chartTab === 'standard'" :class="{ active: chartTab === 'standard' }" @click="changeTab('standard')">QCChart <span>{{ groups.filter((group) => group.qcItemNo === selectedItem?.qcItemNo && !group.isShortRun).length }}</span></button><button role="tab" :aria-selected="chartTab === 'shortRun'" :class="{ active: chartTab === 'shortRun' }" @click="changeTab('shortRun')">ShortrunChart <span>{{ groups.filter((group) => group.qcItemNo === selectedItem?.qcItemNo && group.isShortRun).length }}</span></button><span class="tabs-note">雙擊列開啟圖表</span></div>
      <div class="grid-scroll"><table><thead><tr><th>FactorSerial</th><th v-for="factor in onlineFactors" :key="factor.factorNo">{{ factor.factorName }}</th><th>First Lot Date</th><th>Last Lot Date</th><th>Lot Qty</th><th>KeyNode</th><th>Chart</th></tr></thead><tbody>
        <tr v-for="group in selectedGroups" :key="group.factorSerial" :class="{ active: activeGroup?.factorSerial === group.factorSerial }" @click="activateGroup(group)" @dblclick="openAnalysis('chart')"><td class="serial">{{ group.factorSerial }}</td><td v-for="factor in onlineFactors" :key="factor.factorNo">{{ factorValue(group, factor) }}</td><td>{{ group.firstLotDate }}</td><td>{{ group.lastLotDate }}</td><td class="number">{{ group.lotCount }}</td><td><span class="node-chip" :class="`node-${group.keyNode.toLowerCase()}`">{{ group.keyNode }}</span></td><td>{{ chartTypeName(selectedItem?.chartType || 0) }}</td></tr>
        <tr v-if="!selectedGroups.length"><td :colspan="onlineFactors.length + 6" class="empty">{{ selectedItem ? '沒有符合 Tab／KeyNode 的管制圖。' : '先選擇 QC Item。' }}</td></tr>
      </tbody></table></div>
      <footer class="grid-foot"><span>清單使用 Chart Basis 與 Factor Log 摘要</span><span v-if="selectedGroupRow">目前選取：{{ selectedGroupRow.factorSerial }} · {{ selectedGroupRow.lotCount }} lots</span></footer>
    </section>
    <p class="notice" role="status">{{ notice }}</p>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 QC Item"><header><div><p class="eyebrow">QC ITEM LOOKUP</p><h2>選擇品管項目</h2></div><button class="button" @click="pickerOpen = false">Close</button></header><div class="picker-filters"><label>Search<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><span>{{ pickerItems.length }} 個 IssueState 2 且已設定管制圖的假資料</span></div><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" @click="chooseItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ dataTypeName(item.qcDataType) }}</span><span>{{ chartTypeName(item.chartType) }} · n={{ item.sampleSize }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item</p></div></section></div>

    <div v-if="analysisOpen && selectedGroup && selectedItem" class="modal-backdrop analysis-backdrop" @click.self="closeAnalysis"><section class="analysis-dialog" role="dialog" aria-modal="true" aria-label="管制圖分析明細">
      <header class="analysis-header"><div><p class="eyebrow">{{ selectedGroup.isShortRun ? 'SHORT RUN' : 'QC CHART' }} · {{ selectedGroup.factorSerial }}</p><h2>{{ selectedItem.qcItemNo }} · {{ selectedItem.qcItemName }}</h2><p>{{ chartTypeName(selectedItem.chartType) }} · {{ selectedGroup.keyNode }} · {{ selectedFactorSummary }}</p></div><div class="analysis-actions"><span class="refresh-state"><i></i> {{ refreshedAt }}<small>DEV 自動刷新 {{ refreshMinutes }} 分鐘 · {{ refreshCount }} 次</small></span><button class="button" @click="refreshAnalysis">↻ Refresh</button><button class="button" @click="saveChart">Save Graph</button><button class="button" @click="printChart">Print Graph</button><button class="button close-analysis" @click="closeAnalysis">Close (X)</button></div></header>
      <nav class="analysis-tabs"><button v-for="mode in analysisModes" :key="mode" :class="{ active: analysisMode === mode }" @click="changeAnalysisMode(mode)">{{ viewModeLabel(mode) }}</button><span class="selected-window">{{ filteredMeasures.length }} 筆量測 · {{ batches.length }} lots<span v-if="statistics.excluded"> · 排除 {{ statistics.excluded }} 筆非有效資料</span></span></nav>
      <div class="stats-row"><div><small>TOTAL SAMPLE</small><b>{{ statistics.total }}</b></div><div><small>VALID LOTS</small><b>{{ statistics.lots }}</b></div><div><small>SAMPLE SIZE</small><b>{{ batches[0]?.length || 0 }}</b></div><div><small>Cp</small><b>{{ statistics.cp }}</b></div><div><small>Cpk</small><b>{{ statistics.cpk }}</b></div><div><small>MAX / MIN</small><b>{{ fmt(statistics.max) }} / {{ fmt(statistics.min) }} {{ selectedItem.unit }}</b></div><div><small>MEAN / STD DEV</small><b>{{ fmt(statistics.mean) }} / {{ fmt(statistics.stdDev) }}</b></div></div>
      <div class="analysis-settings"><label class="check"><input v-model="showAllInAnalysis" type="checkbox"> Show All Data</label><label class="check"><input v-model="showErfInAnalysis" type="checkbox"> Show ERF Marks <span class="mini-count">{{ erfCount }}</span></label><label class="check"><input v-model="showHoldInAnalysis" type="checkbox"> Hold Rules at the moment <span class="mini-count">{{ ruleCount }}</span></label><label class="check"><input v-model="showBarChart" type="checkbox"> Show BarChart</label><div class="refresh-setting"><label>Refresh (min)<input v-model.number="refreshMinutes" type="number" min="1" max="60" @change="startRefreshTimer"></label></div></div>

      <div v-if="analysisMode === 'chart'" class="analysis-content"><div class="chart-area"><div class="page-control" v-if="!dataInOnePage"><span>Showing chart section {{ analysisPage + 1 }} of {{ maxPage + 1 }}</span><div><button class="button" :disabled="analysisPage <= 0" @click="adjustPage(-1)">‹ Previous</button><button class="button" :disabled="!canGoNext" @click="adjustPage(1)">Next ›</button></div></div><div class="chart-grid" :class="{ compact: chartDefinitions.length > 2 }"><article v-for="chart in chartDefinitions" :key="chart.key" class="chart-card"><header><h3>{{ chart.title }}</h3><span>{{ fullSeries(chart.key).length }} points · {{ chartLabel(chart.key) }}</span></header><svg viewBox="0 0 860 205" preserveAspectRatio="none" role="img" :aria-label="`${chart.title} DEV 假資料圖`"><line v-for="line in 4" :key="line" x1="48" :y1="28 + line * 34" x2="810" :y2="28 + line * 34" class="grid-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).ucl)" x2="810" :y2="pointY(chart.key, limitsFor(chart.key).ucl)" class="limit-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).cl)" x2="810" :y2="pointY(chart.key, limitsFor(chart.key).cl)" class="center-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).lcl)" x2="810" :y2="pointY(chart.key, limitsFor(chart.key).lcl)" class="limit-line"/><text x="4" :y="pointY(chart.key, limitsFor(chart.key).ucl) - 4" class="limit-label">UCL {{ yTick(chart.key, limitsFor(chart.key).ucl) }}</text><text x="4" :y="pointY(chart.key, limitsFor(chart.key).cl) - 4" class="limit-label center">CL {{ yTick(chart.key, limitsFor(chart.key).cl) }}</text><text x="4" :y="pointY(chart.key, limitsFor(chart.key).lcl) - 4" class="limit-label">LCL {{ yTick(chart.key, limitsFor(chart.key).lcl) }}</text><polyline :points="linePoints(chart.key)" class="chart-line"/><g v-for="(value, index) in seriesFor(chart.key)" :key="`${chart.key}-${index}`" class="point-hit" @click="selectChartPoint(chart.key, index)"><circle :cx="pointX(chart.key, index)" :cy="pointY(chart.key, value)" :r="pointSelected(chart.key, index) ? 6 : seriesViolation(chart.key, index) ? 4.7 : 3.2" :class="{ breach: seriesViolation(chart.key, index), erf: showErfInAnalysis && pointIsErf(chart.key, index), hold: showHoldInAnalysis && pointIsRule(chart.key, index), selected: pointSelected(chart.key, index) }"/><title>Point {{ index + 1 }} · {{ value.toFixed(4) }} {{ selectedItem.unit }}{{ seriesViolation(chart.key, index) ? ` · ${seriesViolation(chart.key, index)}` : '' }}</title></g><text x="48" y="199" class="axis-label">Lot / SPC Serial sequence →</text></svg></article></div><div v-if="showBarChart" class="mini-histogram"><header><h3>Distribution / Histogram</h3><span>{{ filteredMeasures.length }} samples</span></header><div class="hist-bars"><div v-for="bin in histogramBins" :key="bin.index" class="hist-bin"><b>{{ bin.count }}</b><i :style="{ height: `${Math.max(bin.count ? 7 : 2, (bin.count / Math.max(...histogramBins.map((item) => item.count), 1)) * 62)}px` }"></i><small>{{ fmt(bin.start) }}</small></div></div></div></div>
        <aside class="point-panel"><header><h3>Selected Point</h3><span>{{ selectedPointAlert ? displayViolation(selectedPointAlert) || selectedPointAlert.erfNo : selectedPointValue === null ? '請點選資料點' : 'IN CONTROL' }}</span></header><div v-if="selectedPointMeasures.length" class="point-body"><dl><dt>SPC Serial</dt><dd>{{ selectedPointMeasures[0].spcSerial }}</dd><dt>Lot No</dt><dd>{{ selectedPointMeasures[0].lotNo }}</dd><dt>Create Date</dt><dd>{{ shortDate(selectedPointMeasures[0].createDate) }}</dd><dt>Value</dt><dd>{{ fmt(selectedPointValue || 0) }} {{ selectedItem.unit }}</dd><dt>Lot State</dt><dd>{{ selectedPointMeasures[0].lotState === 0 ? 'Collected' : 'Rejected / excluded' }}</dd><dt v-if="showErfInAnalysis && selectedPointMeasures[0].erfNo">ERF No</dt><dd v-if="showErfInAnalysis && selectedPointMeasures[0].erfNo" class="alert">{{ selectedPointMeasures[0].erfNo }}</dd><dt v-if="showHoldInAnalysis && selectedPointAlert?.holdRule">Hold Rule</dt><dd v-if="showHoldInAnalysis && selectedPointAlert?.holdRule" class="alert">{{ selectedPointAlert.holdRule }}</dd><dt>Rule</dt><dd :class="selectedPointAlert?.violation ? 'alert' : 'good'">{{ selectedPointAlert?.violation || (selectedPointAlert?.holdRule ? 'Hold rule' : 'None') }}</dd></dl><h4>Subgroup values</h4><div v-for="point in selectedPointMeasures" :key="`${point.serialNo}-${point.spcSerial}`" class="subgroup-value"><span>Serial {{ point.serialNo }}</span><b>{{ fmt(point.value) }} {{ selectedItem.unit }}</b></div></div><p v-else class="point-empty">點選管制圖資料點，查看 Lot、日期、量測值、ERF 與違規標記。</p><div class="legend"><span><i class="normal-dot"></i>Data</span><span><i class="breach-dot"></i>Rule / Spec</span><span><i class="erf-dot"></i>ERF</span><span><i class="hold-dot"></i>Hold</span></div></aside></div>

      <div v-else-if="analysisMode === 'data'" class="data-view"><div class="data-tools"><span>Raw measurement log · {{ filteredMeasures.length }} rows</span><span v-if="queryByDate">{{ fromDate }} — {{ endDate }}</span><span v-if="queryByLot">Last {{ lastLots }} lots</span></div><div class="data-scroll"><table><thead><tr><th>SPC Serial</th><th>Lot No</th><th>Serial No</th><th>Create Date</th><th>Test Value</th><th>Lot State</th><th>ERF</th><th>Rule / Limit</th><th v-for="factor in onlineFactors" :key="factor.factorNo">{{ factor.factorName }}</th></tr></thead><tbody><tr v-for="measure in filteredMeasures" :key="`${measure.spcSerial}-${measure.serialNo}`" :class="{ excluded: measure.lotState !== 0, violation: measure.violation }"><td>{{ measure.spcSerial }}</td><td>{{ measure.lotNo }}</td><td class="number">{{ measure.serialNo }}</td><td>{{ measure.createDate }}</td><td class="number">{{ measure.value.toFixed(4) }} {{ selectedItem.unit }}</td><td>{{ measure.lotState === 0 ? 'Collected' : 'Excluded' }}</td><td>{{ showErfInAnalysis ? measure.erfNo || '—' : 'Hidden' }}</td><td>{{ measure.violation || (showHoldInAnalysis ? measure.holdRule : '') || '—' }}</td><td v-for="factor in onlineFactors" :key="factor.factorNo">{{ measure.factorValues[factor.factorNo] || '—' }}</td></tr><tr v-if="!filteredMeasures.length"><td :colspan="8 + onlineFactors.length" class="empty">此條件沒有檢測資料</td></tr></tbody></table></div></div>

      <div v-else class="histogram-view"><div class="histogram-heading"><div><h3>Histogram / Process Distribution</h3><span>{{ selectedItem.qcItemName }} · {{ statistics.total }} samples</span></div><div class="spec-labels"><span>LSL {{ fmt(selectedItem.lsl) }}</span><span>Mean {{ fmt(statistics.mean) }}</span><span>USL {{ fmt(selectedItem.usl) }}</span></div></div><svg viewBox="0 0 940 340" preserveAspectRatio="none" role="img" aria-label="DEV 假資料直方圖"><line x1="70" y1="285" x2="915" y2="285" class="axis-line"/><line x1="70" y1="45" x2="70" y2="285" class="axis-line"/><line x1="70" y1="48" x2="915" y2="48" class="grid-line"/><line x1="70" y1="108" x2="915" y2="108" class="grid-line"/><line x1="70" y1="168" x2="915" y2="168" class="grid-line"/><line x1="70" y1="228" x2="915" y2="228" class="grid-line"/><line :x1="70 + ((selectedItem.lsl - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y1="40" :x2="70 + ((selectedItem.lsl - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y2="285" class="spec-line"/><line :x1="70 + ((statistics.mean - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y1="40" :x2="70 + ((statistics.mean - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y2="285" class="mean-line"/><line :x1="70 + ((selectedItem.usl - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y1="40" :x2="70 + ((selectedItem.usl - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl)) / Math.max(Math.max(...filteredMeasures.map((item) => item.value), selectedItem.usl) - Math.min(...filteredMeasures.map((item) => item.value), selectedItem.lsl), 0.00001)) * 845" y2="285" class="spec-line"/><g v-for="bin in histogramBins" :key="bin.index"><rect :x="88 + bin.index * 101" :y="270 - (bin.count / Math.max(...histogramBins.map((item) => item.count), 1)) * 196" width="72" :height="(bin.count / Math.max(...histogramBins.map((item) => item.count), 1)) * 196" :class="{ 'hist-bar': true, 'hist-alert': bin.start < selectedItem.lsl || bin.end > selectedItem.usl }"/><text :x="124 + bin.index * 101" :y="260 - (bin.count / Math.max(...histogramBins.map((item) => item.count), 1)) * 196" text-anchor="middle" class="bar-count">{{ bin.count }}</text><text :x="124 + bin.index * 101" y="304" text-anchor="middle" class="axis-label">{{ fmt(bin.start) }}</text></g><text x="70" y="23" class="axis-label">Sample count</text><text x="460" y="332" text-anchor="middle" class="axis-label">{{ selectedItem.unit }} · LSL {{ fmt(selectedItem.lsl) }} · USL {{ fmt(selectedItem.usl) }}</text></svg><div class="hist-stats"><span>LSL <b>{{ fmt(selectedItem.lsl) }} {{ selectedItem.unit }}</b></span><span>Mean <b>{{ fmt(statistics.mean) }} {{ selectedItem.unit }}</b></span><span>USL <b>{{ fmt(selectedItem.usl) }} {{ selectedItem.unit }}</b></span><span>Cp <b>{{ statistics.cp }}</b></span><span>Cpk <b>{{ statistics.cpk }}</b></span><span>Std Dev <b>{{ fmt(statistics.stdDev) }}</b></span></div></div>
      <footer class="analysis-foot"><span>DEV 使用本機假量測值簡化計算；非 MES 統計／規則引擎輸出。</span><span>{{ filteredMeasures.length }} samples · {{ violationCount }} violation marks · {{ showErfInAnalysis ? erfCount : 0 }} ERF marks</span></footer>
    </section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.analysis-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f1f3f5;color:#28323b;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#237b7b;font-size:10px;font-weight:700;letter-spacing:.1em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#77838c}.button{min-height:29px;padding:4px 10px;border:1px solid #bec8ce;border-radius:4px;background:#fff;color:#35424b;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#e8f3f2;border-color:#3b8b8b}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{border-color:#1f7b80;background:#1f7b80;color:#fff;font-weight:700}.panel{border:1px solid #d6dfe3;border-radius:5px;background:#fff;overflow:hidden}.criteria{margin-bottom:10px}.criteria-row,.filter-row,.options-row{display:flex;align-items:flex-end;gap:9px;padding:9px 11px}.criteria-row>label,.filter-row>label{display:flex;flex-direction:column;gap:4px;color:#5e6b74;font-size:10px}.criteria-row select,.criteria-row input:not([type=checkbox]),.filter-row input:not([type=checkbox]){height:30px;box-sizing:border-box;border:1px solid #c7d0d5;border-radius:4px;background:#fff;color:#2b3740;padding:4px 7px;font:11px "Segoe UI",Arial,sans-serif}.criteria-row select{min-width:112px}.item-field{position:relative;width:min(395px,35vw)}.item-field input{width:100%;padding-right:57px!important}.item-field .button{position:absolute;right:3px;bottom:3px;min-height:24px;padding:2px 9px}.criteria-row>.button{height:30px}.filter-row{align-items:center;border-top:1px solid #e8edef;background:#fafbfc}.filter-row label.check,.options-row label.check{display:inline-flex;align-items:center;gap:5px;white-space:nowrap}.check input{accent-color:#237b7b}.filter-row input[type=date]{width:132px}.filter-row>label:nth-last-child(2) input{width:86px}.divider{width:1px;height:25px;margin:0 5px;background:#dce3e6}.filter-hint{margin-left:auto;color:#96a1a8;font-size:9px}.options-row{flex-wrap:wrap;align-items:center;border-top:1px solid #edf0f1;color:#697680;font-size:10px}.hold-check{margin-left:auto}.item-summary{display:flex;align-items:center;gap:0;padding:7px 11px;border-top:1px solid #e8edef;background:#f8fafb}.item-summary>span{display:flex;flex-direction:column;gap:1px;padding:0 14px;border-right:1px solid #e0e6e8;color:#61707a;font-size:10px}.item-summary>span:first-child{padding-left:0}.item-summary>span:last-child{border:0}.item-summary small{color:#98a3aa;font-size:8px;font-weight:700;letter-spacing:.07em}.item-summary b{color:#34434d;font-size:11px}.panel-head{display:flex;justify-content:space-between;align-items:center;gap:10px;padding:8px 11px;border-bottom:1px solid #e4eaed;background:#f9fbfc}.panel-head h2{margin:0;font-size:14px}.panel-head span{display:block;margin-top:2px;color:#7e8991;font-size:10px}.view-actions{display:flex;gap:5px}.view-actions .button{font-size:10px}.tabs{display:flex;align-items:center;gap:4px;padding:5px 9px 0;border-bottom:1px solid #dce4e7;background:#f7f9fa}.tabs button{padding:7px 12px;border:1px solid #dbe2e5;border-bottom:0;border-radius:4px 4px 0 0;background:#eef2f4;color:#65727b;font:11px "Segoe UI",Arial,sans-serif;cursor:pointer}.tabs button.active{background:#fff;color:#26767b;font-weight:700}.tabs button span{margin-left:5px;padding:1px 5px;border-radius:8px;background:#e6ecee;font-size:8px}.tabs-note{margin-left:auto;padding:6px;color:#929da3;font-size:9px}.grid-scroll{max-height:calc(100% - 95px);overflow:auto}.grid-scroll table,.data-scroll table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}.grid-scroll th,.grid-scroll td,.data-scroll th,.data-scroll td{padding:7px 8px;border-bottom:1px solid #edf0f1;text-align:left}.grid-scroll th,.data-scroll th{position:sticky;top:0;z-index:2;background:#eef3f5;color:#5a6770;font-size:9px;font-weight:700}.grid-scroll tbody tr{cursor:pointer}.grid-scroll tbody tr:hover,.grid-scroll tbody tr.active{background:#e5f2f2}.serial{color:#1c7077;font-weight:700}.number{text-align:right!important;font-variant-numeric:tabular-nums}.node-chip{display:inline-block;min-width:20px;padding:1px 5px;border-radius:3px;background:#e8eff2;text-align:center;font-size:9px;font-weight:700}.node-i{background:#e2f2ef;color:#277265}.node-c{background:#fff1d7;color:#946315}.node-n{background:#e8ebf4;color:#55638b}.empty{text-align:center!important;padding:22px!important;color:#929da3!important}.grid-foot{display:flex;justify-content:space-between;padding:7px 10px;color:#89959c;font-size:9px}.notice{margin:10px 0 0;padding:8px 10px;border:1px solid #cddddd;border-radius:4px;background:#eef7f6;color:#3d7374;font-size:10px}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:grid;place-items:center;padding:14px;background:#14202b88}.picker-dialog,.analysis-dialog{width:min(900px,96vw);max-height:95vh;overflow:auto;border:1px solid #c7d1d6;border-radius:6px;background:#f5f7f8;box-shadow:0 18px 60px #101d2880}.picker-dialog{padding:14px}.picker-dialog>header{display:flex;justify-content:space-between;align-items:center}.picker-dialog h2{margin:2px 0;font-size:18px}.picker-filters{display:flex;align-items:flex-end;gap:12px;padding:11px 0}.picker-filters label{display:flex;flex:1;flex-direction:column;gap:4px;color:#697780;font-size:10px}.picker-filters input{height:30px;border:1px solid #c8d0d5;border-radius:4px;padding:5px 8px;font:11px "Segoe UI",Arial,sans-serif}.picker-filters>span{padding-bottom:8px;color:#929da3;font-size:9px}.picker-list{border:1px solid #dde4e7;border-radius:4px;overflow:hidden}.picker-row{display:grid;grid-template-columns:130px 1fr 80px 105px 85px;gap:8px;width:100%;padding:9px;border:0;border-bottom:1px solid #e7ecee;background:#fff;text-align:left;color:#596771;font:10px "Segoe UI",Arial,sans-serif;cursor:pointer}.picker-row:last-child{border-bottom:0}.picker-row:hover{background:#e9f5f3}.picker-row b{color:#1c7077}.analysis-backdrop{z-index:1250}.analysis-dialog{width:min(1520px,98vw);padding:0;background:#f3f5f6}.analysis-header{display:flex;justify-content:space-between;align-items:center;gap:14px;padding:11px 14px;border-bottom:1px solid #dce3e7;background:#fff}.analysis-header h2{margin:2px 0;font-size:17px}.analysis-header p:last-child{margin:3px 0 0;color:#76838b;font-size:9px}.analysis-actions{display:flex;align-items:center;gap:5px}.analysis-actions .button{font-size:9px;white-space:nowrap}.close-analysis{background:#3e4c55;color:#fff;border-color:#3e4c55}.refresh-state{display:flex;flex-direction:column;align-items:flex-end;padding-right:5px;color:#63717a;font-size:8px;white-space:nowrap}.refresh-state i{display:inline-block;width:7px;height:7px;margin-right:4px;border-radius:50%;background:#35a36d;box-shadow:0 0 0 3px #e4f4ea}.refresh-state small{margin-top:3px;color:#97a1a7;font-size:8px}.analysis-tabs{display:flex;align-items:center;gap:3px;padding:6px 10px 0;border-bottom:1px solid #dbe3e7;background:#f7f9fa}.analysis-tabs button{padding:7px 12px;border:1px solid #dce3e6;border-bottom:0;border-radius:4px 4px 0 0;background:#edf1f3;color:#68747c;font:10px "Segoe UI",Arial,sans-serif;cursor:pointer}.analysis-tabs button.active{background:#fff;color:#1f7377;font-weight:700}.selected-window{margin-left:auto;padding:7px;color:#839099;font-size:8px}.selected-window span{margin-left:4px}.stats-row{display:grid;grid-template-columns:repeat(7,minmax(70px,1fr));gap:1px;border-bottom:1px solid #dce3e6;background:#dce3e6}.stats-row>div{display:flex;flex-direction:column;gap:3px;padding:7px 9px;background:#f7f9fa}.stats-row small{color:#95a0a6;font-size:7px;font-weight:700;letter-spacing:.07em}.stats-row b{color:#394851;font-size:10px;font-variant-numeric:tabular-nums}.analysis-settings{display:flex;align-items:center;gap:12px;padding:6px 10px;border-bottom:1px solid #e2e7e9;background:#fff;color:#6e7a82;font-size:8px}.analysis-settings .check{display:flex;align-items:center;gap:4px;white-space:nowrap}.analysis-settings input[type=checkbox]{accent-color:#287e80}.mini-count{color:#9aa4aa}.refresh-setting{margin-left:auto}.refresh-setting label{display:flex;align-items:center;gap:5px}.refresh-setting input{width:48px;height:23px;border:1px solid #ccd4d8;border-radius:3px;padding:2px 4px;font:9px "Segoe UI",Arial,sans-serif}.analysis-content{display:grid;grid-template-columns:minmax(0,1fr) 230px;gap:8px;padding:9px}.chart-area{min-width:0}.page-control{display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;color:#87939b;font-size:8px}.page-control>div{display:flex;gap:4px}.page-control .button{min-height:24px;font-size:8px}.chart-grid{display:grid;grid-template-columns:1fr 1fr;gap:7px}.chart-grid.compact{grid-template-columns:1fr 1fr}.chart-card{min-width:0;border:1px solid #dce3e6;border-radius:4px;background:#fff;overflow:hidden}.chart-card header,.mini-histogram header{display:flex;justify-content:space-between;align-items:center;padding:6px 8px;border-bottom:1px solid #edf0f1}.chart-card h3,.mini-histogram h3{margin:0;font-size:10px}.chart-card header span,.mini-histogram header span{color:#8a969d;font-size:8px}.chart-card svg{display:block;width:100%;height:146px}.grid-line{stroke:#edf0f2;stroke-width:1}.limit-line{stroke:#d69a4f;stroke-width:1;stroke-dasharray:5 3}.center-line{stroke:#a4b0b6;stroke-width:1;stroke-dasharray:3 3}.limit-label{fill:#b27d38;font-size:8px}.limit-label.center{fill:#7e8b92}.chart-line{fill:none;stroke:#21828a;stroke-width:2}.point-hit{cursor:pointer}.point-hit circle{fill:#21828a;stroke:#fff;stroke-width:1.5}.point-hit circle.breach{fill:#c64b45}.point-hit circle.erf{stroke:#d18b31;stroke-width:2}.point-hit circle.hold{stroke:#7864aa;stroke-width:2}.point-hit circle.selected{stroke:#1c2931;stroke-width:2.5}.axis-label{fill:#8b969d;font-size:8px}.mini-histogram{margin-top:7px;border:1px solid #dce3e6;border-radius:4px;background:#fff}.hist-bars{display:flex;align-items:flex-end;gap:8px;height:90px;padding:8px 13px 4px}.hist-bin{display:flex;flex:1;flex-direction:column;justify-content:flex-end;align-items:center;height:100%;gap:2px}.hist-bin b{color:#78858d;font-size:7px}.hist-bin i{width:min(30px,72%);border-radius:2px 2px 0 0;background:#6bb1b0}.hist-bin small{color:#939da3;font-size:6px}.point-panel{display:flex;flex-direction:column;min-height:235px;border:1px solid #dce3e6;border-radius:4px;background:#fff}.point-panel>header{display:flex;justify-content:space-between;align-items:center;padding:7px 8px;border-bottom:1px solid #edf0f1}.point-panel h3{margin:0;font-size:10px}.point-panel>header span{color:#8b969d;font-size:8px}.point-body{padding:8px}.point-body dl{display:grid;grid-template-columns:68px 1fr;gap:6px 4px;margin:0;font-size:8px}.point-body dt{color:#89969d}.point-body dd{margin:0;color:#46565f;overflow-wrap:anywhere}.point-body dd.alert{color:#bf433d;font-weight:700}.point-body dd.good{color:#4f815d}.point-body h4{margin:10px 0 4px;color:#7d8991;font-size:8px}.subgroup-value{display:flex;justify-content:space-between;padding:3px 0;border-top:1px solid #eff2f3;color:#7a8790;font-size:8px}.subgroup-value b{color:#495961;font-variant-numeric:tabular-nums}.point-empty{margin:auto;padding:14px;color:#929da3;text-align:center;font-size:9px}.legend{display:flex;flex-wrap:wrap;justify-content:center;gap:8px;margin-top:auto;padding:7px;border-top:1px solid #edf0f1;color:#86929a;font-size:7px}.legend span{display:flex;align-items:center;gap:3px}.legend i{width:7px;height:7px;border-radius:50%}.normal-dot{background:#21828a}.breach-dot{background:#c64b45}.erf-dot{background:#d18b31}.hold-dot{background:#7864aa}.data-view{padding:9px}.data-tools{display:flex;gap:12px;padding:6px 8px;border:1px solid #dfe5e8;border-bottom:0;border-radius:4px 4px 0 0;background:#fff;color:#7b8890;font-size:8px}.data-scroll{max-height:calc(95vh - 260px);overflow:auto;border:1px solid #dfe5e8;border-radius:0 0 4px 4px;background:#fff}.data-scroll table{font-size:8px}.data-scroll th{top:0}.data-scroll td{padding:6px}.data-scroll tr.excluded{color:#a1a9ae;background:#fafbfb}.data-scroll tr.violation{background:#fff0ee}.histogram-view{padding:14px;background:#fff}.histogram-heading{display:flex;justify-content:space-between;align-items:flex-end;padding:2px 8px 10px}.histogram-heading h3{margin:0;font-size:14px}.histogram-heading span{display:block;margin-top:3px;color:#7c8991;font-size:9px}.spec-labels{display:flex;gap:10px;color:#78858c;font-size:8px}.histogram-view svg{display:block;width:100%;height:330px;border:1px solid #edf0f1;border-radius:4px;background:#fff}.axis-line{stroke:#85939a;stroke-width:1}.spec-line{stroke:#d69a4f;stroke-width:2;stroke-dasharray:5 3}.mean-line{stroke:#21828a;stroke-width:2}.hist-bar{fill:#78b9b7;opacity:.85}.hist-alert{fill:#e59b82}.bar-count{fill:#63727b;font-size:9px}.hist-stats{display:flex;flex-wrap:wrap;justify-content:space-around;gap:8px;padding:12px 0;color:#89949b;font-size:9px}.hist-stats b{color:#43525a}.analysis-foot{display:flex;justify-content:space-between;padding:7px 12px;border-top:1px solid #dfe5e8;background:#fafbfc;color:#8b979e;font-size:7px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1060px){.criteria-row,.filter-row{flex-wrap:wrap}.item-field{width:min(420px,100%)}.filter-hint{width:100%;margin-left:0}.analysis-header{align-items:flex-start;flex-direction:column}.analysis-actions{width:100%;justify-content:flex-end}.analysis-content{grid-template-columns:1fr}.point-panel{min-height:200px}.chart-grid{grid-template-columns:1fr}.chart-grid.compact{grid-template-columns:1fr 1fr}.stats-row{grid-template-columns:repeat(4,1fr)}}
@media(max-width:680px){.analysis-actions{flex-wrap:wrap}.refresh-state{width:100%;align-items:flex-start}.chart-grid.compact{grid-template-columns:1fr}.stats-row{grid-template-columns:repeat(2,1fr)}.options-row{align-items:flex-start;flex-direction:column;gap:7px}.hold-check{margin-left:0}.filter-row .divider{display:none}.item-summary{flex-wrap:wrap;gap:7px}.item-summary>span{padding:0 8px}.picker-row{grid-template-columns:1fr 1fr}.view-actions{flex-wrap:wrap;justify-content:flex-end}}
</style>

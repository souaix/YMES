<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'

type DataType = 0 | 1 | 2
type ChartType = 0 | 1 | 2
type ViewMode = 'data' | 'chart' | 'histogram' | 'factor' | 'summary'
type ChartKey = 'xbar' | 'spread' | 'trend'
type QCItem = { qcItemNo: string; qcItemName: string; qcDataType: DataType; chartType: ChartType; sampleSize: number; target: number; sigma: number; unit: string; lsl: number; usl: number; issueState: 2 | -1 }
type FactorDefinition = { factorNo: string; factorName: string; parameterType: 0 | 1 | 2; options: string[]; sampleValues: string[] }
type FactorState = FactorDefinition & { selectedValues: string[]; freeValue: string }
type Measurement = { index: number; batchIndex: number; serialNo: string; spcSerial: string; lotNo: string; createDate: string; createdAt: number; factorValues: Record<string, string>; value: number; lotState: 0 | 1; erfNo: string }
type LotGroup = { index: number; spcSerial: string; lotNo: string; createDate: string; lotState: 0 | 1; erfNo: string; measurements: Measurement[]; mean: number; range: number; standardDeviation: number }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const itemFixtures: QCItem[] = [
  { qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', qcDataType: 1, chartType: 0, sampleSize: 5, target: 0.412, sigma: 0.028, unit: 'mm', lsl: 0.320, usl: 0.500, issueState: 2 },
  { qcItemNo: 'QC-V-2002', qcItemName: 'Die Shear Strength', qcDataType: 2, chartType: 1, sampleSize: 5, target: 14.8, sigma: 0.62, unit: 'N', lsl: 12.5, usl: 17.2, issueState: 2 },
  { qcItemNo: 'QC-V-3001', qcItemName: 'Wafer Thickness', qcDataType: 0, chartType: 2, sampleSize: 1, target: 725, sigma: 2.1, unit: 'μm', lsl: 718.5, usl: 731.5, issueState: 2 },
  { qcItemNo: 'QC-V-3108', qcItemName: 'Mold Flash Height', qcDataType: 1, chartType: 0, sampleSize: 5, target: 0.072, sigma: 0.006, unit: 'mm', lsl: 0.052, usl: 0.092, issueState: -1 },
]
const factorFixtures: Record<string, FactorDefinition[]> = {
  'QC-V-1001': [
    { factorNo: 'MACHINE', factorName: '機台', parameterType: 0, options: ['MCH-01', 'MCH-02', 'MCH-03'], sampleValues: ['MCH-01', 'MCH-02', 'MCH-03'] },
    { factorNo: 'LINE', factorName: '產線', parameterType: 1, options: ['LN-A', 'LN-B', 'LN-C'], sampleValues: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 2, options: [], sampleValues: ['USR-11019', 'USR-11023', 'USR-11007'] },
  ],
  'QC-V-2002': [
    { factorNo: 'MACHINE', factorName: '設備', parameterType: 0, options: ['DS-08', 'DS-11', 'DS-14'], sampleValues: ['DS-08', 'DS-11', 'DS-14'] },
    { factorNo: 'MATERIAL', factorName: '材料批號', parameterType: 1, options: ['MAT-26K-031', 'MAT-26K-034'], sampleValues: ['MAT-26K-031', 'MAT-26K-034'] },
  ],
  'QC-V-3001': [
    { factorNo: 'EQUIPMENT', factorName: '設備', parameterType: 0, options: ['THK-12', 'THK-15'], sampleValues: ['THK-12', 'THK-15'] },
    { factorNo: 'CHAMBER', factorName: '腔體', parameterType: 0, options: ['CH-03', 'CH-04'], sampleValues: ['CH-03', 'CH-04'] },
  ],
  'QC-V-3108': [
    { factorNo: 'MACHINE', factorName: '機台', parameterType: 0, options: ['MOLD-05', 'MOLD-07'], sampleValues: ['MOLD-05', 'MOLD-07'] },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [], sampleValues: ['A', 'B', 'C'] },
  ],
}
const dataTypeOptions = [{ value: '', label: '全部' }, { value: '0', label: 'Particle' }, { value: '1', label: 'InLine' }, { value: '2', label: 'OffLine' }]
const viewModes: ViewMode[] = ['data', 'chart', 'histogram', 'factor', 'summary']

const itemType = ref('')
const pickerSearch = ref('')
const pickerOpen = ref(false)
const selectedItem = ref<QCItem | null>(null)
const factors = ref<FactorState[]>([])
const queryByDate = ref(false)
const fromDate = ref('')
const endDate = ref('')
const showAllData = ref(false)
const dataInOnePage = ref(true)
const showBarChart = ref(true)
const transToShortRun = ref(false)
const shortRunType = ref<'relative' | 'absolute'>('relative')
const analysisOpen = ref(false)
const analysisMode = ref<ViewMode>('chart')
const showAllInAnalysis = ref(false)
const analysisPage = ref(0)
const selectedPoint = ref<{ chart: ChartKey; seriesIndex: number; lotIndex: number; recordIndex: number } | null>(null)
const refreshMinutes = ref(5)
const refreshCount = ref(0)
const refreshedAt = ref('尚未載入')
const notice = ref('請選擇 QC Item，再設定收集要因與查詢條件。')
let refreshTimer: ReturnType<typeof setInterval> | undefined

function dateOffsetMonths(months: number, days: number): string {
  const date = new Date()
  date.setMonth(date.getMonth() + months)
  date.setDate(date.getDate() + days)
  return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0')
}
fromDate.value = dateOffsetMonths(-2, 1)
endDate.value = dateOffsetMonths(0, 0)

const pickerItems = computed(() => itemFixtures.filter((item) => (itemType.value === '' || item.qcDataType === Number(itemType.value)) && (item.issueState === 2 || item.issueState === -1) && (item.qcItemNo + ' ' + item.qcItemName).toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const dataTypeName = (type: DataType): string => dataTypeOptions.find((option) => option.value === String(type))?.label || 'InLine'
const chartTypeName = (type: ChartType): string => type === 2 ? 'X-Rm' : type === 1 ? 'X-S' : 'X-R'

function chooseItem(item: QCItem): void {
  selectedItem.value = item
  factors.value = (factorFixtures[item.qcItemNo] || []).map((factor) => ({ ...factor, selectedValues: [], freeValue: '' }))
  pickerOpen.value = false
  notice.value = item.qcItemNo + ' 載入 ' + factors.value.length + ' 個收集要因；DEV 僅使用本機假資料。'
}
function openPicker(): void { pickerSearch.value = ''; pickerOpen.value = true }
function clearFactorFilters(): void { factors.value.forEach((factor) => { factor.selectedValues = []; factor.freeValue = '' }) }
function resetCriteria(): void {
  clearFactorFilters()
  queryByDate.value = false
  fromDate.value = dateOffsetMonths(-2, 1)
  endDate.value = dateOffsetMonths(0, 0)
  transToShortRun.value = false
  shortRunType.value = 'relative'
  notice.value = '已清除收集要因並還原查詢條件。'
}

const allMeasurements = computed<Measurement[]>(() => {
  const item = selectedItem.value
  if (!item) return []
  const count = 36
  const result: Measurement[] = []
  for (let batch = 0; batch < count; batch += 1) {
    const createdAt = Date.now() - (count - batch) * 47 * 60_000
    const factorValues: Record<string, string> = {}
    factors.value.forEach((factor, factorIndex) => {
      const samples = factor.sampleValues.length ? factor.sampleValues : ['—']
      factorValues[factor.factorNo] = samples[(batch + factorIndex) % samples.length]
    })
    const lotState: 0 | 1 = batch % 13 === 9 ? 1 : 0
    const outOfControl = batch === count - 5
    const size = item.chartType === 2 ? 1 : item.sampleSize
    for (let serial = 0; serial < size; serial += 1) {
      const drift = Math.sin(batch * 0.55 + item.qcItemNo.length) * item.sigma * 0.35
      const special = outOfControl ? item.sigma * 3.6 : 0
      const subgroup = size === 1 ? Math.cos(batch * 0.8) * item.sigma * 0.35 : (serial - (size - 1) / 2) * item.sigma * 0.38 + Math.cos(batch + serial * 1.2) * item.sigma * 0.11
      const value = item.target + drift + special + subgroup
      const timestamp = new Date(createdAt + serial * 3 * 60_000)
      result.push({
        index: batch * size + serial,
        batchIndex: batch,
        serialNo: String(serial + 1),
        spcSerial: 'SPC-LN-' + String(batch + 1).padStart(3, '0'),
        lotNo: 'LOT-26K-' + String((batch * 19 + 231) % 999).padStart(3, '0'),
        createDate: timestamp.toLocaleString('zh-TW', { hour12: false }),
        createdAt: timestamp.getTime(),
        factorValues: { ...factorValues },
        value,
        lotState,
        erfNo: batch % 15 === 7 && serial === 0 ? 'ERF-26K-' + String(batch + 210).padStart(4, '0') : '',
      })
    }
  }
  return result
})

const queryRows = computed(() => {
  let rows = allMeasurements.value
  if (queryByDate.value && fromDate.value && endDate.value) {
    const from = new Date(fromDate.value + 'T00:00:00').getTime()
    const end = new Date(endDate.value + 'T23:59:59').getTime()
    rows = rows.filter((row) => row.createdAt >= from && row.createdAt <= end)
  }
  return rows.filter((row) => factors.value.every((factor) => {
    const selected = factor.parameterType === 2 ? (factor.freeValue.trim() ? [factor.freeValue.trim()] : []) : factor.selectedValues
    return selected.length === 0 || selected.includes(row.factorValues[factor.factorNo] || '')
  }))
})
const analysisRows = computed(() => queryRows.value.filter((row) => showAllInAnalysis.value || row.lotState === 0))
const lotGroups = computed<LotGroup[]>(() => {
  const grouped = new Map<string, Measurement[]>()
  for (const row of analysisRows.value) {
    const current = grouped.get(row.spcSerial) || []
    current.push(row)
    grouped.set(row.spcSerial, current)
  }
  return Array.from(grouped.values()).map((measurements) => {
    const values = measurements.map((row) => row.value)
    const mean = values.reduce((sum, value) => sum + value, 0) / values.length
    const variance = values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length
    return {
      index: measurements[0].batchIndex,
      spcSerial: measurements[0].spcSerial,
      lotNo: measurements[0].lotNo,
      createDate: measurements[0].createDate,
      lotState: measurements[0].lotState,
      erfNo: measurements[0].erfNo,
      measurements,
      mean,
      range: Math.max(...values) - Math.min(...values),
      standardDeviation: Math.sqrt(variance),
    }
  }).sort((left, right) => left.index - right.index)
})
const dataStatistics = computed(() => {
  const item = selectedItem.value
  const values = analysisRows.value.map((row) => row.value)
  if (!item || !values.length) return { total: 0, lots: 0, mean: 0, stdDev: 0, min: 0, max: 0, cp: '—', cpk: '—', excluded: queryRows.value.length }
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length
  const stdDev = Math.sqrt(values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / values.length)
  const cp = stdDev > 0 ? (item.usl - item.lsl) / (6 * stdDev) : 0
  const cpk = stdDev > 0 ? Math.min((item.usl - mean) / (3 * stdDev), (mean - item.lsl) / (3 * stdDev)) : 0
  return { total: values.length, lots: lotGroups.value.length, mean, stdDev, min: Math.min(...values), max: Math.max(...values), cp: cp.toFixed(2), cpk: cpk.toFixed(2), excluded: queryRows.value.length - values.length }
})
const chartDefinitions = computed(() => {
  if (!selectedItem.value) return []
  return [
    { key: 'xbar' as ChartKey, title: selectedItem.value.chartType === 2 ? 'X / Individual Chart' : 'X-Bar Chart' },
    { key: 'spread' as ChartKey, title: selectedItem.value.chartType === 1 ? 'S-Chart' : selectedItem.value.chartType === 2 ? 'Moving Range' : 'R-Chart' },
    { key: 'trend' as ChartKey, title: 'Measurement Trend' },
  ]
})
const chartPageSize = 16
const maxPage = computed(() => Math.max(0, Math.ceil(lotGroups.value.length / chartPageSize) - 1))
function fullSeries(key: ChartKey): number[] {
  if (key === 'xbar') return lotGroups.value.map((lot) => lot.mean)
  if (key === 'spread') return lotGroups.value.map((lot) => selectedItem.value?.chartType === 1 ? lot.standardDeviation : lot.range)
  return analysisRows.value.map((row) => row.value)
}
function seriesFor(key: ChartKey): number[] {
  const series = fullSeries(key)
  if (dataInOnePage.value) return series
  const sampleSize = Math.max(1, selectedItem.value?.sampleSize || 1)
  const pageSize = key === 'trend' ? chartPageSize * sampleSize : chartPageSize
  return series.slice(analysisPage.value * pageSize, (analysisPage.value + 1) * pageSize)
}
function limitsFor(key: ChartKey): { ucl: number; cl: number; lcl: number } {
  const item = selectedItem.value
  if (!item) return { ucl: 1, cl: 0, lcl: -1 }
  if (key === 'trend') return { ucl: item.usl, cl: item.target, lcl: item.lsl }
  if (key === 'spread') {
    const values = fullSeries(key)
    const center = values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : item.sigma
    const upper = center * (item.chartType === 1 ? 1.65 : 2.35)
    return { ucl: upper, cl: center, lcl: 0 }
  }
  const spread = item.sigma * (transToShortRun.value ? (shortRunType.value === 'relative' ? 2.6 : 3.2) : 3 / Math.sqrt(Math.max(1, item.sampleSize)))
  return { ucl: item.target + spread, cl: item.target, lcl: item.target - spread }
}
function scaleFor(key: ChartKey): { min: number; max: number } {
  const values = fullSeries(key)
  const limits = limitsFor(key)
  if (!values.length) return { min: limits.lcl - 1, max: limits.ucl + 1 }
  const range = Math.max(Math.max(...values, limits.ucl) - Math.min(...values, limits.lcl), 0.00001)
  return { min: Math.min(...values, limits.lcl) - range * 0.12, max: Math.max(...values, limits.ucl) + range * 0.12 }
}
function pointX(key: ChartKey, index: number): number {
  const length = seriesFor(key).length
  return length <= 1 ? 420 : 48 + index * (744 / (length - 1))
}
function pointY(key: ChartKey, value: number): number {
  const scale = scaleFor(key)
  return 164 - ((value - scale.min) / Math.max(scale.max - scale.min, 0.000001)) * 132
}
function linePoints(key: ChartKey): string { return seriesFor(key).map((value, index) => pointX(key, index) + ',' + pointY(key, value)).join(' ') }
function seriesViolation(key: ChartKey, index: number): string {
  const value = seriesFor(key)[index]
  const limits = limitsFor(key)
  if (key === 'trend') return value < limits.lcl || value > limits.ucl ? 'SPEC' : ''
  return value < limits.lcl || value > limits.ucl ? 'CONTROL LIMIT' : ''
}
function selectPoint(key: ChartKey, visibleIndex: number): void {
  const start = dataInOnePage.value ? 0 : analysisPage.value * (key === 'trend' ? chartPageSize * Math.max(1, selectedItem.value?.sampleSize || 1) : chartPageSize)
  const recordIndex = key === 'trend' ? visibleIndex + start : -1
  const batchIndex = key === 'trend' ? analysisRows.value[recordIndex]?.batchIndex : lotGroups.value[visibleIndex + start]?.index
  const lotIndex = key === 'trend' ? lotGroups.value.findIndex((lot) => lot.index === batchIndex) : visibleIndex + start
  selectedPoint.value = { chart: key, seriesIndex: visibleIndex, lotIndex, recordIndex }
}
const selectedLot = computed(() => selectedPoint.value ? lotGroups.value[selectedPoint.value.lotIndex] || null : null)
const selectedValue = computed(() => {
  if (!selectedPoint.value) return null
  if (selectedPoint.value.chart === 'trend') return analysisRows.value[selectedPoint.value.recordIndex]?.value ?? null
  const lot = selectedLot.value
  if (!lot) return null
  return selectedPoint.value.chart === 'spread' ? lot.range : lot.mean
})
const histogramBins = computed(() => {
  const values = analysisRows.value.map((row) => row.value)
  if (!values.length) return []
  const min = Math.min(...values)
  const max = Math.max(...values)
  const size = Math.max(max - min, selectedItem.value?.sigma || 1) / 10
  return Array.from({ length: 10 }, (_, index) => {
    const start = min + index * size
    const end = index === 9 ? max + 0.000001 : start + size
    return { index, start, count: values.filter((value) => value >= start && value < end).length }
  })
})
const factorAnalysisRows = computed(() => factors.value.flatMap((factor) => {
  const groups = new Map<string, number[]>()
  for (const row of analysisRows.value) {
    const value = row.factorValues[factor.factorNo] || '—'
    const list = groups.get(value) || []
    list.push(row.value)
    groups.set(value, list)
  }
  return Array.from(groups.entries()).map(([value, values]) => ({
    factorNo: factor.factorNo,
    factorName: factor.factorName,
    value,
    count: values.length,
    mean: values.reduce((sum, entry) => sum + entry, 0) / values.length,
    min: Math.min(...values),
    max: Math.max(...values),
  }))
}))
function formatValue(value: number): string { return Number(value.toFixed(3)).toString() }
function shortDate(value: string): string { return value.replace(/:\d{2}$/, '') }
function chartLabel(key: ChartKey): string { return key === 'xbar' ? 'X' : key === 'trend' ? 'Trend' : selectedItem.value?.chartType === 1 ? 'S' : selectedItem.value?.chartType === 2 ? 'Rm' : 'R' }

function markRefreshed(): void { refreshedAt.value = new Date().toLocaleString('zh-TW', { hour12: false }) }
function startRefreshTimer(): void {
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = setInterval(() => { refreshCount.value += 1; markRefreshed() }, Math.max(60_000, refreshMinutes.value * 60_000))
}
function openAnalysis(mode: ViewMode): void {
  if (!selectedItem.value) { notice.value = '請先選取 QC Item。'; return }
  if (queryByDate.value && (!fromDate.value || !endDate.value || fromDate.value > endDate.value)) { notice.value = '日期區間起訖必須完整，且起日不得晚於迄日。'; return }
  analysisMode.value = mode
  showAllInAnalysis.value = showAllData.value
  analysisPage.value = 0
  selectedPoint.value = null
  refreshCount.value = 0
  markRefreshed()
  analysisOpen.value = true
  startRefreshTimer()
}
function closeAnalysis(): void {
  analysisOpen.value = false
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = undefined
  selectedPoint.value = null
}
function changePage(direction: -1 | 1): void {
  analysisPage.value = Math.min(maxPage.value, Math.max(0, analysisPage.value + direction))
  selectedPoint.value = null
}
function saveGraph(): void { notice.value = 'DEV 已模擬 Save Graph；沒有產生或上傳圖檔。' }
function printGraph(): void { notice.value = 'DEV 已模擬 Print；未呼叫本機列印服務。' }
function refreshAnalysis(): void { refreshCount.value += 1; markRefreshed(); notice.value = 'DEV 假資料已重新載入；沒有呼叫服務或資料庫。' }

onBeforeUnmount(() => { if (refreshTimer) clearInterval(refreshTimer) })
</script>

<template>
  <section v-if="props.developmentPreview" class="line-analysis-page" aria-label="彙整分析圖">
    <header class="page-head"><div><p class="eyebrow">SPC / SC207</p><h1>彙整分析圖</h1><p class="subtitle">依 QC Item、收集要因與日期範圍彙整計量資料。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="criteria panel">
      <div class="criteria-row">
        <label>QC Item<input :value="selectedItem ? selectedItem.qcItemNo + ' · ' + selectedItem.qcItemName : ''" readonly placeholder="請選擇品管項目"><button class="button get-button" @click="openPicker">Get</button></label>
        <label>Data Type<select v-model="itemType"><option v-for="option in dataTypeOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></label>
        <label class="check"><input v-model="queryByDate" type="checkbox"> Query by Date</label>
        <label>From<input v-model="fromDate" type="date" :disabled="!queryByDate"></label>
        <label>End<input v-model="endDate" type="date" :disabled="!queryByDate"></label>
        <button class="button" :disabled="!selectedItem" @click="resetCriteria">Reset Criteria</button>
      </div>

      <div class="short-run-row">
        <label class="check"><input v-model="transToShortRun" type="checkbox"> Trans to Short-Run Chart</label>
        <fieldset :disabled="!transToShortRun"><legend>Short-Run Limit Type</legend><label class="check"><input v-model="shortRunType" type="radio" value="relative"> Relative tolerance</label><label class="check"><input v-model="shortRunType" type="radio" value="absolute"> Absolute tolerance</label></fieldset>
        <span>此處短程轉換只模擬彙整分析圖模式，不等於線上 Short-Run Chart。</span>
      </div>

      <div class="factor-header"><div><h2>Collect Factors</h2><span>{{ factors.length }} 個要因 · 空白代表不篩選</span></div><button class="button" :disabled="!factors.length" @click="clearFactorFilters">Clear selections</button></div>
      <div class="factor-scroll"><table><thead><tr><th>Factor No</th><th>要因</th><th>Lookup Type</th><th>Factor Value / multi-select</th><th>已選</th></tr></thead><tbody>
        <tr v-for="factor in factors" :key="factor.factorNo"><td class="serial">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType === 2 ? 'Free input' : factor.parameterType === 1 ? 'Function list' : 'Table list' }}</td><td><input v-if="factor.parameterType === 2" v-model="factor.freeValue" class="factor-input" :placeholder="factor.factorNo === 'OPERATOR' ? '例：USR-11019' : '輸入要因值'"><select v-else v-model="factor.selectedValues" multiple size="3" class="factor-select"><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select></td><td><span v-if="factor.parameterType === 2">{{ factor.freeValue || '全部' }}</span><span v-else>{{ factor.selectedValues.length ? factor.selectedValues.join(', ') : '全部' }}</span></td></tr>
        <tr v-if="!factors.length"><td colspan="5" class="empty">先選擇 QC Item 載入對應要因。</td></tr>
      </tbody></table></div>
      <div class="options-row"><label class="check"><input v-model="showAllData" type="checkbox"> Show All Data</label><label class="check"><input v-model="dataInOnePage" type="checkbox"> Data in One Page</label><label class="check"><input v-model="showBarChart" type="checkbox"> Show BarChart</label><span class="option-note">日期條件預設未啟用；量測與圖表為本機 DEV 假資料。</span></div>
    </section>

    <section class="query-panel panel"><header><div><h2>Query</h2><span v-if="selectedItem">{{ selectedItem.qcItemNo }} · {{ chartTypeName(selectedItem.chartType) }} · Sample Size {{ selectedItem.sampleSize }}</span><span v-else>選擇 QC Item 後可執行查詢</span></div><div class="query-actions"><button class="button" :disabled="!selectedItem" @click="openAnalysis('data')">Query Data</button><button class="button primary" :disabled="!selectedItem" @click="openAnalysis('chart')">Query Chart</button><button class="button" :disabled="!selectedItem" @click="openAnalysis('histogram')">Histogram</button></div></header><p class="notice" role="status">{{ notice }}</p></section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 QC Item"><header><div><p class="eyebrow">QC ITEM LOOKUP</p><h2>選擇品管項目</h2></div><button class="button" @click="pickerOpen = false">Close</button></header><div class="picker-filters"><label>Search<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><span>搜尋 IssueState 2／-1 的 QC Item 假資料</span></div><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" @click="chooseItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ dataTypeName(item.qcDataType) }}</span><span>{{ chartTypeName(item.chartType) }} · n={{ item.sampleSize }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item。</p></div></section></div>

    <div v-if="analysisOpen && selectedItem" class="modal-backdrop analysis-backdrop" @click.self="closeAnalysis"><section class="analysis-dialog" role="dialog" aria-modal="true" aria-label="彙整分析結果">
      <header class="analysis-header"><div><p class="eyebrow">SC207 · {{ selectedItem.qcItemNo }}</p><h2>{{ selectedItem.qcItemName }} · {{ chartTypeName(selectedItem.chartType) }}</h2><p>收集要因：{{ factors.filter((factor) => factor.parameterType === 2 ? factor.freeValue : factor.selectedValues.length).map((factor) => factor.factorName).join('、') || '全部' }} · {{ queryByDate ? fromDate + ' — ' + endDate : '全部日期' }} · {{ transToShortRun ? (shortRunType === 'relative' ? 'Relative Short-Run' : 'Absolute Short-Run') : '標準彙整分析' }}</p></div><div class="analysis-actions"><span class="refresh-state"><i></i>{{ refreshedAt }}<small>DEV refresh {{ refreshMinutes }} min · {{ refreshCount }} 次</small></span><button class="button" @click="refreshAnalysis">Refresh</button><button class="button" @click="saveGraph">Save Graph</button><button class="button" @click="printGraph">Print</button><button class="button close-analysis" @click="closeAnalysis">Close (X)</button></div></header>
      <nav class="analysis-tabs"><button v-for="mode in viewModes" :key="mode" :class="{ active: analysisMode === mode }" @click="analysisMode = mode">{{ mode === 'data' ? 'QCItem Test Data' : mode === 'chart' ? 'Line Chart Analysis' : mode === 'histogram' ? 'Histogram' : mode === 'factor' ? 'FactorAnalysis' : 'MaxMinMean' }}</button><span>{{ dataStatistics.total }} samples · {{ lotGroups.length }} lots<span v-if="dataStatistics.excluded"> · 排除 {{ dataStatistics.excluded }}</span></span></nav>
      <div class="stats-row"><div><small>TOTAL SAMPLE</small><b>{{ dataStatistics.total }}</b></div><div><small>VALID LOTS</small><b>{{ dataStatistics.lots }}</b></div><div><small>SAMPLE SIZE</small><b>{{ lotGroups[0]?.measurements.length || 0 }}</b></div><div><small>Cp</small><b>{{ dataStatistics.cp }}</b></div><div><small>Cpk</small><b>{{ dataStatistics.cpk }}</b></div><div><small>MAX / MIN</small><b>{{ formatValue(dataStatistics.max) }} / {{ formatValue(dataStatistics.min) }} {{ selectedItem.unit }}</b></div><div><small>MEAN / STD DEV</small><b>{{ formatValue(dataStatistics.mean) }} / {{ formatValue(dataStatistics.stdDev) }}</b></div></div>
      <div class="analysis-settings"><label class="check"><input v-model="showAllInAnalysis" type="checkbox"> Show All Data</label><label class="check"><input v-model="dataInOnePage" type="checkbox"> Data in One Page</label><label class="check"><input v-model="showBarChart" type="checkbox"> Show BarChart</label><label class="refresh-setting">Refresh (min)<input v-model.number="refreshMinutes" type="number" min="1" max="60" @change="startRefreshTimer"></label></div>

      <div v-if="analysisMode === 'chart'" class="chart-content"><div v-if="!dataInOnePage" class="page-control"><span>圖表頁 {{ analysisPage + 1 }} / {{ maxPage + 1 }}</span><div><button class="button" :disabled="analysisPage <= 0" @click="changePage(-1)">‹ Previous</button><button class="button" :disabled="analysisPage >= maxPage" @click="changePage(1)">Next ›</button></div></div><div class="chart-layout"><div class="chart-area"><article v-for="chart in chartDefinitions" :key="chart.key" class="chart-card"><header><h3>{{ chart.title }}</h3><span>{{ fullSeries(chart.key).length }} points · {{ chartLabel(chart.key) }}</span></header><svg viewBox="0 0 840 190" preserveAspectRatio="none" role="img" :aria-label="chart.title + ' DEV 假資料圖'"><line v-for="line in 4" :key="line" x1="48" :y1="28 + line * 34" x2="792" :y2="28 + line * 34" class="grid-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).ucl)" x2="792" :y2="pointY(chart.key, limitsFor(chart.key).ucl)" class="limit-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).cl)" x2="792" :y2="pointY(chart.key, limitsFor(chart.key).cl)" class="center-line"/><line x1="48" :y1="pointY(chart.key, limitsFor(chart.key).lcl)" x2="792" :y2="pointY(chart.key, limitsFor(chart.key).lcl)" class="limit-line"/><text x="3" :y="pointY(chart.key, limitsFor(chart.key).ucl) - 3" class="limit-label">UCL {{ formatValue(limitsFor(chart.key).ucl) }}</text><text x="3" :y="pointY(chart.key, limitsFor(chart.key).cl) - 3" class="limit-label center">CL {{ formatValue(limitsFor(chart.key).cl) }}</text><text x="3" :y="pointY(chart.key, limitsFor(chart.key).lcl) - 3" class="limit-label">LCL {{ formatValue(limitsFor(chart.key).lcl) }}</text><polyline :points="linePoints(chart.key)" class="chart-line"/><g v-for="(value, index) in seriesFor(chart.key)" :key="chart.key + '-' + index" class="point-hit" @click="selectPoint(chart.key, index)"><circle :cx="pointX(chart.key, index)" :cy="pointY(chart.key, value)" :r="seriesViolation(chart.key, index) ? 5 : selectedPoint?.chart === chart.key && selectedPoint?.seriesIndex === index ? 6 : 3.4" :class="{ breach: !!seriesViolation(chart.key, index), selected: selectedPoint?.chart === chart.key && selectedPoint?.seriesIndex === index }"/><title>{{ chart.title }} · {{ formatValue(value) }} {{ selectedItem.unit }}{{ seriesViolation(chart.key, index) ? ' · ' + seriesViolation(chart.key, index) : '' }}</title></g><text x="48" y="184" class="axis-label">Lot / SPC Serial sequence →</text></svg></article><div v-if="showBarChart" class="mini-bars"><header><h3>Distribution / BarChart</h3><span>{{ analysisRows.length }} samples</span></header><div class="bar-row"><div v-for="bin in histogramBins" :key="bin.index" class="bar-bin"><b>{{ bin.count }}</b><i :style="{ height: Math.max(4, bin.count * 4) + 'px' }"></i><small>{{ formatValue(bin.start) }}</small></div></div></div></div><aside class="point-panel"><header><h3>Selected Point</h3><span>{{ selectedValue === null ? '請點選圖表資料點' : selectedPoint && selectedLot ? seriesViolation(selectedPoint.chart, selectedPoint.seriesIndex) || 'IN CONTROL' : 'IN CONTROL' }}</span></header><div v-if="selectedLot" class="point-body"><dl><dt>SPC Serial</dt><dd>{{ selectedLot.spcSerial }}</dd><dt>Lot No</dt><dd>{{ selectedLot.lotNo }}</dd><dt>Create Date</dt><dd>{{ shortDate(selectedLot.createDate) }}</dd><dt>Value</dt><dd>{{ selectedValue === null ? '—' : formatValue(selectedValue) + ' ' + selectedItem.unit }}</dd><dt>Lot State</dt><dd>{{ selectedLot.lotState === 0 ? 'Collected' : 'Rejected / excluded' }}</dd><dt v-if="selectedLot.erfNo">ERF</dt><dd v-if="selectedLot.erfNo" class="alert">{{ selectedLot.erfNo }}</dd></dl><h4>Subgroup data</h4><div v-for="row in selectedLot.measurements" :key="row.serialNo" class="subgroup"><span>Serial {{ row.serialNo }}</span><b>{{ formatValue(row.value) }} {{ selectedItem.unit }}</b></div></div><p v-else class="point-empty">選取圖表資料點以檢視批次、量測、要因與 ERF。</p></aside></div></div>

      <div v-else-if="analysisMode === 'data'" class="data-view"><div class="data-tools"><span>QCItem Test Data · {{ queryRows.length }} rows</span><span v-if="queryByDate">{{ fromDate }} — {{ endDate }}</span><span>空白 Factor 不限制；Show All Data {{ showAllInAnalysis ? 'ON' : 'OFF' }}</span></div><div class="data-scroll"><table><thead><tr><th>SPC Serial</th><th>Lot No</th><th>Serial No</th><th>Create Date</th><th>Test Value</th><th>Lot State</th><th>ERF</th><th v-for="factor in factors" :key="factor.factorNo">{{ factor.factorName }}</th></tr></thead><tbody><tr v-for="row in queryRows" :key="row.spcSerial + '-' + row.serialNo" :class="{ excluded: row.lotState !== 0 }"><td>{{ row.spcSerial }}</td><td>{{ row.lotNo }}</td><td class="number">{{ row.serialNo }}</td><td>{{ row.createDate }}</td><td class="number">{{ row.value.toFixed(4) }} {{ selectedItem.unit }}</td><td>{{ row.lotState === 0 ? 'Collected' : 'Excluded' }}</td><td>{{ row.erfNo || '—' }}</td><td v-for="factor in factors" :key="factor.factorNo">{{ row.factorValues[factor.factorNo] || '—' }}</td></tr><tr v-if="!queryRows.length"><td :colspan="7 + factors.length" class="empty">此條件沒有量測資料。</td></tr></tbody></table></div></div>

      <div v-else-if="analysisMode === 'histogram'" class="histogram-view"><div class="histogram-heading"><div><h3>Histogram / Process Distribution</h3><span>{{ selectedItem.qcItemName }} · {{ dataStatistics.total }} samples</span></div><div><span>LSL {{ formatValue(selectedItem.lsl) }}</span><span>Mean {{ formatValue(dataStatistics.mean) }}</span><span>USL {{ formatValue(selectedItem.usl) }}</span></div></div><svg viewBox="0 0 900 315" preserveAspectRatio="none" role="img" aria-label="DEV 假資料直方圖"><line x1="62" y1="266" x2="880" y2="266" class="axis-line"/><line x1="62" y1="40" x2="62" y2="266" class="axis-line"/><line x1="62" y1="92" x2="880" y2="92" class="grid-line"/><line x1="62" y1="148" x2="880" y2="148" class="grid-line"/><line x1="62" y1="204" x2="880" y2="204" class="grid-line"/><g v-for="bin in histogramBins" :key="bin.index"><rect :x="78 + bin.index * 79" :y="250 - (bin.count / Math.max(...histogramBins.map((entry) => entry.count), 1)) * 174" width="58" :height="(bin.count / Math.max(...histogramBins.map((entry) => entry.count), 1)) * 174" :class="{ 'hist-bar': true, 'hist-alert': bin.start < selectedItem.lsl || bin.start > selectedItem.usl }"/><text :x="107 + bin.index * 79" :y="243 - (bin.count / Math.max(...histogramBins.map((entry) => entry.count), 1)) * 174" text-anchor="middle" class="bar-count">{{ bin.count }}</text><text :x="107 + bin.index * 79" y="284" text-anchor="middle" class="axis-label">{{ formatValue(bin.start) }}</text></g><text x="62" y="26" class="axis-label">Samples</text><text x="470" y="308" text-anchor="middle" class="axis-label">{{ selectedItem.unit }} · LSL {{ formatValue(selectedItem.lsl) }} · USL {{ formatValue(selectedItem.usl) }}</text></svg><div class="hist-stats"><span>LSL <b>{{ formatValue(selectedItem.lsl) }}</b></span><span>Mean <b>{{ formatValue(dataStatistics.mean) }}</b></span><span>USL <b>{{ formatValue(selectedItem.usl) }}</b></span><span>Cp <b>{{ dataStatistics.cp }}</b></span><span>Cpk <b>{{ dataStatistics.cpk }}</b></span><span>Std Dev <b>{{ formatValue(dataStatistics.stdDev) }}</b></span></div></div>

      <div v-else-if="analysisMode === 'factor'" class="factor-analysis-view"><header><h3>FactorAnalysis</h3><span>依目前假資料比較要因值的量測分布。</span></header><div class="data-scroll"><table><thead><tr><th>Factor No</th><th>要因</th><th>Value</th><th>Data Qty</th><th>Mean</th><th>Min</th><th>Max</th></tr></thead><tbody><tr v-for="row in factorAnalysisRows" :key="row.factorNo + row.value"><td>{{ row.factorNo }}</td><td>{{ row.factorName }}</td><td>{{ row.value }}</td><td class="number">{{ row.count }}</td><td class="number">{{ formatValue(row.mean) }} {{ selectedItem.unit }}</td><td class="number">{{ formatValue(row.min) }}</td><td class="number">{{ formatValue(row.max) }}</td></tr><tr v-if="!factorAnalysisRows.length"><td colspan="7" class="empty">沒有可比較的要因資料。</td></tr></tbody></table></div></div>

      <div v-else class="summary-view"><header><h3>MaxMinMean · Statistic / Limit / Lot summary</h3><span>統計與限值僅為展示用算法。</span></header><div class="summary-cards"><div><small>MAX</small><b>{{ formatValue(dataStatistics.max) }} {{ selectedItem.unit }}</b></div><div><small>MIN</small><b>{{ formatValue(dataStatistics.min) }} {{ selectedItem.unit }}</b></div><div><small>MEAN</small><b>{{ formatValue(dataStatistics.mean) }} {{ selectedItem.unit }}</b></div><div><small>STD DEV</small><b>{{ formatValue(dataStatistics.stdDev) }}</b></div><div><small>LSL / TARGET / USL</small><b>{{ formatValue(selectedItem.lsl) }} / {{ formatValue(selectedItem.target) }} / {{ formatValue(selectedItem.usl) }}</b></div></div><div class="data-scroll"><table><thead><tr><th>SPC Serial</th><th>Lot No</th><th>Create Date</th><th>Sample Qty</th><th>Mean</th><th>Range</th><th>Std Dev</th><th>Lot State</th></tr></thead><tbody><tr v-for="lot in lotGroups" :key="lot.spcSerial"><td>{{ lot.spcSerial }}</td><td>{{ lot.lotNo }}</td><td>{{ lot.createDate }}</td><td class="number">{{ lot.measurements.length }}</td><td class="number">{{ formatValue(lot.mean) }}</td><td class="number">{{ formatValue(lot.range) }}</td><td class="number">{{ formatValue(lot.standardDeviation) }}</td><td>{{ lot.lotState === 0 ? 'Collected' : 'Excluded' }}</td></tr></tbody></table></div></div>

      <footer class="analysis-foot"><span>DEV 假資料 · 未呼叫 Service／資料庫 · 圖表、能力指標與界線不代表正式 Core 計算</span><span>{{ selectedItem.qcItemNo }} · {{ queryRows.length }} samples</span></footer>
    </section></div>
  </section>
  <section v-else class="pending" aria-label="彙整分析圖"><p>正式環境尚未開放此功能。</p><button class="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.line-analysis-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f1f3f5;color:#28323b;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#237b7b;font-size:10px;font-weight:700;letter-spacing:.1em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#77838c}.button{min-height:29px;padding:4px 10px;border:1px solid #bec8ce;border-radius:4px;background:#fff;color:#35424b;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#e8f3f2;border-color:#3b8b8b}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{border-color:#1f7b80;background:#1f7b80;color:#fff;font-weight:700}.panel{border:1px solid #d6dfe3;border-radius:5px;background:#fff;overflow:hidden}.criteria{margin-bottom:10px}.criteria-row{display:flex;align-items:flex-end;gap:10px;flex-wrap:wrap;padding:9px 11px}.criteria-row>label:not(.check){position:relative;display:flex;min-width:130px;flex-direction:column;gap:4px;color:#5e6b74;font-size:10px}.criteria-row>label:first-child{width:min(370px,45vw)}.criteria-row input:not([type=checkbox]):not([type=radio]),.criteria-row select{height:30px;box-sizing:border-box;border:1px solid #c7d0d5;border-radius:4px;background:#fff;color:#2b3740;padding:4px 7px;font:11px "Segoe UI",Arial,sans-serif}.criteria-row>label:first-child input{padding-right:54px}.get-button{position:absolute;right:3px;bottom:3px;min-height:24px;padding:2px 9px}.check{display:flex;align-items:center;gap:5px;white-space:nowrap;color:#5e6b74;font-size:10px}.check input{accent-color:#237b7b}.short-run-row{display:flex;align-items:center;gap:14px;flex-wrap:wrap;padding:8px 11px;border-top:1px solid #e8edef;background:#fafbfc;color:#65727b;font-size:9px}.short-run-row fieldset{display:flex;align-items:center;gap:8px;margin:0;padding:3px 8px;border:1px solid #d9e1e5;border-radius:4px}.short-run-row legend{padding:0 3px;color:#8c979d;font-size:8px}.short-run-row>span{margin-left:auto;color:#8b969d}.factor-header,.query-panel>header,.factor-analysis-view>header,.summary-view>header{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:8px 11px;border-top:1px solid #e4eaed;background:#f9fbfc}.factor-header h2,.query-panel h2,.factor-analysis-view h3,.summary-view h3{margin:0;font-size:13px}.factor-header span,.query-panel header span,.factor-analysis-view header span,.summary-view header span{display:block;margin-top:2px;color:#7e8991;font-size:9px}.factor-scroll,.data-scroll{max-height:260px;overflow:auto}.factor-scroll table,.data-scroll table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}.factor-scroll th,.factor-scroll td,.data-scroll th,.data-scroll td{padding:6px 9px;border-bottom:1px solid #edf0f1;text-align:left}.factor-scroll th,.data-scroll th{position:sticky;top:0;z-index:2;background:#eef3f5;color:#5a6770;font-size:9px;font-weight:700}.factor-select{min-width:230px;min-height:54px;border:1px solid #c7d0d5;border-radius:3px;background:#fff;padding:3px;color:#35424b;font:10px "Segoe UI",Arial,sans-serif}.factor-input{width:230px;height:29px;border:1px solid #c7d0d5;border-radius:3px;padding:4px 7px;font:10px "Segoe UI",Arial,sans-serif}.serial{color:#1c7077;font-weight:700}.empty{text-align:center!important;padding:18px!important;color:#929da3!important}.options-row{display:flex;align-items:center;gap:18px;flex-wrap:wrap;padding:8px 11px;border-top:1px solid #edf0f1;color:#68757d;font-size:10px}.option-note{margin-left:auto;color:#929da3;font-size:9px}.query-panel>header{border-top:0}.query-actions{display:flex;gap:6px}.notice{margin:8px 11px 10px;padding:8px 10px;border:1px solid #cddddd;border-radius:4px;background:#eef7f6;color:#3d7374;font-size:10px}.modal-backdrop{position:fixed;inset:0;z-index:1200;display:grid;place-items:center;padding:14px;background:#14202b88}.picker-dialog,.analysis-dialog{width:min(1000px,96vw);max-height:95vh;overflow:auto;border:1px solid #c7d1d6;border-radius:6px;background:#f5f7f8;box-shadow:0 18px 60px #101d2880}.picker-dialog{padding:14px}.picker-dialog>header{display:flex;justify-content:space-between;align-items:center}.picker-dialog h2{margin:2px 0;font-size:18px}.picker-filters{display:flex;align-items:flex-end;gap:12px;padding:11px 0}.picker-filters label{display:flex;flex:1;flex-direction:column;gap:4px;color:#697780;font-size:10px}.picker-filters input{height:30px;border:1px solid #c8d0d5;border-radius:4px;padding:5px 8px;font:11px "Segoe UI",Arial,sans-serif}.picker-filters>span{padding-bottom:8px;color:#929da3;font-size:9px}.picker-list{border:1px solid #dde4e7;border-radius:4px;overflow:hidden}.picker-row{display:grid;grid-template-columns:130px 1fr 82px 110px 78px;gap:8px;width:100%;padding:9px;border:0;border-bottom:1px solid #e7ecee;background:#fff;text-align:left;color:#596771;font:10px "Segoe UI",Arial,sans-serif;cursor:pointer}.picker-row:hover{background:#e9f5f3}.picker-row b{color:#1c7077}.analysis-backdrop{z-index:1250}.analysis-dialog{width:min(1460px,98vw);padding:0;background:#f3f5f6}.analysis-header{display:flex;justify-content:space-between;align-items:center;gap:14px;padding:11px 14px;border-bottom:1px solid #dce3e7;background:#fff}.analysis-header h2{margin:2px 0;font-size:17px}.analysis-header p:last-child{margin:3px 0 0;color:#76838b;font-size:9px}.analysis-actions{display:flex;align-items:center;gap:5px}.analysis-actions .button{font-size:9px;white-space:nowrap}.close-analysis{background:#3e4c55;color:#fff;border-color:#3e4c55}.refresh-state{display:flex;flex-direction:column;align-items:flex-end;padding-right:5px;color:#63717a;font-size:8px;white-space:nowrap}.refresh-state i{display:inline-block;width:7px;height:7px;margin-right:4px;border-radius:50%;background:#35a36d;box-shadow:0 0 0 3px #e4f4ea}.refresh-state small{margin-top:3px;color:#97a1a7;font-size:8px}.analysis-tabs{display:flex;align-items:center;gap:3px;overflow:auto;padding:6px 10px 0;border-bottom:1px solid #dbe3e7;background:#f7f9fa}.analysis-tabs button{flex:none;padding:7px 11px;border:1px solid #dce3e6;border-bottom:0;border-radius:4px 4px 0 0;background:#edf1f3;color:#68747c;font:9px "Segoe UI",Arial,sans-serif;cursor:pointer}.analysis-tabs button.active{background:#fff;color:#1f7377;font-weight:700}.analysis-tabs>span{margin-left:auto;padding:7px;color:#839099;font-size:8px;white-space:nowrap}.stats-row{display:grid;grid-template-columns:repeat(7,minmax(70px,1fr));gap:1px;border-bottom:1px solid #dce3e6;background:#dce3e6}.stats-row>div{display:flex;flex-direction:column;gap:3px;padding:7px 9px;background:#f7f9fa}.stats-row small,.summary-cards small{color:#95a0a6;font-size:7px;font-weight:700;letter-spacing:.07em}.stats-row b{color:#394851;font-size:10px;font-variant-numeric:tabular-nums}.analysis-settings{display:flex;align-items:center;gap:14px;flex-wrap:wrap;padding:6px 10px;border-bottom:1px solid #e2e7e9;background:#fff;color:#6e7a82;font-size:8px}.analysis-settings .check{font-size:8px}.refresh-setting{display:flex;align-items:center;gap:5px;margin-left:auto;color:#6e7a82}.refresh-setting input{width:48px;height:23px;border:1px solid #ccd4d8;border-radius:3px;padding:2px 4px;font:9px "Segoe UI",Arial,sans-serif}.chart-content{padding:9px}.page-control{display:flex;justify-content:space-between;align-items:center;margin-bottom:6px;color:#87939b;font-size:8px}.page-control>div{display:flex;gap:4px}.page-control .button{min-height:24px;font-size:8px}.chart-layout{display:grid;grid-template-columns:minmax(0,1fr) 230px;gap:8px}.chart-area{display:grid;grid-template-columns:1fr 1fr;gap:7px;min-width:0}.chart-card,.mini-bars,.point-panel{min-width:0;border:1px solid #dce3e6;border-radius:4px;background:#fff;overflow:hidden}.chart-card header,.mini-bars header,.point-panel>header{display:flex;justify-content:space-between;align-items:center;padding:6px 8px;border-bottom:1px solid #edf0f1}.chart-card h3,.mini-bars h3,.point-panel h3{margin:0;font-size:10px}.chart-card header span,.mini-bars header span,.point-panel>header span{color:#8a969d;font-size:8px}.chart-card svg{display:block;width:100%;height:148px}.grid-line{stroke:#edf0f2;stroke-width:1}.limit-line{stroke:#d69a4f;stroke-width:1;stroke-dasharray:5 3}.center-line{stroke:#a4b0b6;stroke-width:1;stroke-dasharray:3 3}.limit-label{fill:#b27d38;font-size:8px}.limit-label.center{fill:#7e8b92}.chart-line{fill:none;stroke:#21828a;stroke-width:2}.point-hit{cursor:pointer}.point-hit circle{fill:#21828a;stroke:#fff;stroke-width:1.5}.point-hit circle.breach{fill:#c64b45}.point-hit circle.selected{stroke:#1c2931;stroke-width:2.5}.axis-label{fill:#8b969d;font-size:8px}.mini-bars{grid-column:1/-1}.bar-row{display:flex;align-items:flex-end;gap:7px;height:76px;padding:7px 10px 3px}.bar-bin{display:flex;flex:1;flex-direction:column;align-items:center;justify-content:flex-end;height:100%;gap:2px}.bar-bin b{color:#78858d;font-size:7px}.bar-bin i{width:min(24px,70%);border-radius:2px 2px 0 0;background:#6bb1b0}.bar-bin small{color:#939da3;font-size:6px}.point-panel{display:flex;flex-direction:column;min-height:260px}.point-body{padding:8px}.point-body dl{display:grid;grid-template-columns:64px 1fr;gap:6px 4px;margin:0;font-size:8px}.point-body dt{color:#89969d}.point-body dd{margin:0;color:#46565f;overflow-wrap:anywhere}.point-body h4{margin:10px 0 4px;color:#7d8991;font-size:8px}.subgroup{display:flex;justify-content:space-between;padding:3px 0;border-top:1px solid #eff2f3;color:#7a8790;font-size:8px}.subgroup b{color:#495961;font-variant-numeric:tabular-nums}.alert{color:#bf433d!important;font-weight:700}.point-empty{margin:auto;padding:14px;color:#929da3;text-align:center;font-size:9px}.data-view,.factor-analysis-view,.summary-view{padding:10px}.data-tools{display:flex;gap:12px;padding:6px 8px;border:1px solid #dfe5e8;border-bottom:0;border-radius:4px 4px 0 0;background:#fff;color:#7b8890;font-size:8px}.data-scroll{max-height:calc(95vh - 310px);border:1px solid #dfe5e8;border-radius:0 0 4px 4px;background:#fff}.data-scroll td{padding:6px}.data-scroll tr.excluded{color:#a1a9ae;background:#fafbfb}.histogram-view{padding:13px;background:#fff}.histogram-heading{display:flex;justify-content:space-between;align-items:flex-end;padding:2px 8px 10px}.histogram-heading h3{margin:0;font-size:14px}.histogram-heading span{display:inline-block;margin-left:9px;color:#7c8991;font-size:9px}.histogram-view svg{display:block;width:100%;height:310px;border:1px solid #edf0f1;border-radius:4px;background:#fff}.axis-line{stroke:#85939a;stroke-width:1}.hist-bar{fill:#78b9b7;opacity:.85}.hist-alert{fill:#e59b82}.bar-count{fill:#63727b;font-size:8px}.hist-stats{display:flex;flex-wrap:wrap;justify-content:space-around;gap:8px;padding:10px 0;color:#89949b;font-size:9px}.hist-stats b{color:#43525a}.factor-analysis-view>header,.summary-view>header{border-top:0;border-bottom:1px solid #e2e7e9}.summary-cards{display:flex;justify-content:space-around;gap:10px;flex-wrap:wrap;padding:12px 5px}.summary-cards>div{display:flex;flex-direction:column;gap:3px;color:#495961;font-size:9px}.analysis-foot{display:flex;justify-content:space-between;gap:10px;padding:7px 12px;border-top:1px solid #dfe5e8;background:#fafbfc;color:#8b979e;font-size:7px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1020px){.criteria-row>label:first-child{width:min(370px,100%)}.short-run-row>span{width:100%;margin-left:0}.chart-layout{grid-template-columns:1fr}.chart-area{grid-template-columns:1fr}.stats-row{grid-template-columns:repeat(4,1fr)}.analysis-header{align-items:flex-start;flex-direction:column}.analysis-actions{width:100%;justify-content:flex-end}.analysis-tabs>span{margin-left:0}}
</style>

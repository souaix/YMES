<script setup lang="ts">
import { computed, ref } from 'vue'

type ParameterType = 0 | 1 | 2
type ReasonType = 0 | 1
type FactorDefinition = { factorNo: string; factorName: string; parameterType: ParameterType; options: string[] }
type FactorState = FactorDefinition & { values: string[]; freeValue: string }
type QCItem = { itemNo: string; itemName: string; factors: FactorDefinition[] }
type ErrorDefinition = { errorNo: string; errorName: string; reasonType: ReasonType }
type ErrorLog = ErrorDefinition & { errorQty: number; dimension: number | null; sectionNo: string; rank1: string; rank2: string; rank3: string; rank4: string }
type LotLog = { serial: string; itemNo: string; lotNo: string; createDate: number; excluded: 0 | 1 | 2; retestLotNo: string; sampleQty: number; scrapQty: number; defectQty: number; product: string; pcsMultiplier: number; factors: Record<string, string>; errors: ErrorLog[] }
type ParetoRow = { errorNo: string; errorName: string; errorQty: number; errorLotNum: number; errorRate: number; errorDimension: number; errorPcs: number; lotNos: string[]; cumulative: number }
type Metric = 'piece' | 'lot' | 'rate' | 'dimension' | 'pcs'
type DetailPanel = 'detail' | 'more' | 'fishbone' | 'rank' | 'section' | 'pie' | 'weeks' | 'export' | 'save' | 'print' | ''
type ResultFilters = { normal: boolean; excluded: boolean; retest: boolean; scrap: boolean; defect: boolean }
type QueryCriteria = { itemNos: string[]; start: number; end: number; factors: { factorNo: string; values: string[] }[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const items: QCItem[] = [
  { itemNo: 'QC-A-2201', itemName: 'Lead Frame Surface Defect', factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'PRESS', factorName: '沖壓機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
    { factorNo: 'MATERIAL_LOT', factorName: '材料批號', parameterType: 2, options: [] },
  ] },
  { itemNo: 'QC-A-2237', itemName: 'Wire Bond Appearance', factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'BONDER', factorName: '焊線機', parameterType: 1, options: ['WB-12', 'WB-15', 'WB-20'] },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [] },
  ] },
  { itemNo: 'QC-A-2290', itemName: 'Mold Compound Contamination', factors: [
    { factorNo: 'PRESS', factorName: '成型機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
    { factorNo: 'MATERIAL_LOT', factorName: '材料批號', parameterType: 2, options: [] },
  ] },
]
const reasonDefinitions: ErrorDefinition[] = [
  { errorNo: 'E-110', errorName: '表面刮傷', reasonType: 0 },
  { errorNo: 'E-204', errorName: '鍍層異物', reasonType: 0 },
  { errorNo: 'E-321', errorName: '端子變形', reasonType: 1 },
  { errorNo: 'E-408', errorName: '焊線偏移', reasonType: 1 },
  { errorNo: 'E-511', errorName: '模封氣泡', reasonType: 0 },
  { errorNo: 'E-620', errorName: '尺寸超差', reasonType: 1 },
]
const lotRows: LotLog[] = items.flatMap((item, itemIndex) => Array.from({ length: 64 }, (_, index) => {
  const sampleQty = 72 + ((index * 13 + itemIndex * 7) % 39)
  const reasons = [reasonDefinitions[(index + itemIndex * 2) % reasonDefinitions.length], reasonDefinitions[(index * 3 + itemIndex + 1) % reasonDefinitions.length]]
  const errors: ErrorLog[] = index % 7 === 0 ? [] : reasons.map((reason, reasonIndex) => ({
    ...reason,
    errorQty: 1 + ((index * 5 + reasonIndex * 3 + itemIndex) % 11),
    dimension: (index + reasonIndex) % 17 === 0 ? null : Number((0.15 + ((index * 7 + reasonIndex) % 20) / 10).toFixed(2)),
    sectionNo: 'SEC-' + String(1 + ((index + reasonIndex) % 8)).padStart(2, '0'),
    rank1: ['Machine', 'Material', 'Method', 'Man'][index % 4],
    rank2: ['Speed', 'Supplier', 'Setup', 'Shift'][(index + reasonIndex) % 4],
    rank3: ['Wear', 'Cleanliness', 'Training', 'Handling'][(index * 2 + reasonIndex) % 4],
    rank4: ['A', 'B', 'C', 'D'][(index + itemIndex) % 4],
  }))
  const excluded: LotLog['excluded'] = index % 19 === 6 ? 1 : index % 23 === 9 ? 2 : 0
  return {
    serial: item.itemNo + '-SPC-' + String(index + 1).padStart(4, '0'), itemNo: item.itemNo,
    lotNo: 'LOT-26K-' + String((index * 29 + itemIndex * 113 + 101) % 997).padStart(3, '0'),
    createDate: Date.now() - ((34 - index) * 13 + itemIndex * 4) * 60 * 60 * 1000,
    excluded,
    retestLotNo: index % 11 === 5 ? 'RT-' + String(index + 1).padStart(3, '0') : '',
    sampleQty, scrapQty: errors.filter((error) => error.reasonType === 0).reduce((sum, error) => sum + error.errorQty, 0),
    defectQty: errors.filter((error) => error.reasonType === 1).reduce((sum, error) => sum + error.errorQty, 0),
    product: ['PRD-2201-A', 'PRD-2237-B', 'PRD-2290-C'][itemIndex], pcsMultiplier: 1 + (index % 3),
    factors: {
      LINE: ['LN-A', 'LN-B', 'LN-C'][index % 3], PRESS: ['PRESS-03', 'PRESS-05', 'PRESS-08'][Math.floor(index / 2) % 3],
      MATERIAL_LOT: 'MAT-26K-' + String(310 + Math.floor(index / 3)).padStart(3, '0'), BONDER: ['WB-12', 'WB-15', 'WB-20'][index % 3], SHIFT: ['A', 'B', 'C'][index % 3],
    }, errors,
  }
}))

const pickerOpen = ref(false)
const pickerSearch = ref('')
const selectedItemNos = ref<string[]>([])
const factors = ref<FactorState[]>([])
const fromDate = ref('')
const endDate = ref('')
const includeNormal = ref(true)
const includeExcluded = ref(false)
const includeRetest = ref(false)
const includeScrap = ref(true)
const includeDefect = ref(true)
const resultFilterDraft = ref<ResultFilters>({ normal: true, excluded: false, retest: false, scrap: true, defect: true })
const resultFilterApplied = ref<ResultFilters>({ ...resultFilterDraft.value })
const queryCriteria = ref<QueryCriteria | null>(null)
const metric = ref<Metric>('piece')
const barNumber = ref(10)
const queried = ref(false)
const selectedErrorNos = ref<string[]>([])
const activePanel = ref<DetailPanel>('')
const notice = ref('請選擇 Attribute QC Item，設定日期與 Collect Factor 後執行 Query。')
const refreshedAt = ref('尚未查詢')

const pickerItems = computed(() => items.filter((item) => item.itemNo.toLowerCase().includes(pickerSearch.value.trim().toLowerCase()) || item.itemName.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const selectedItems = computed(() => items.filter((item) => selectedItemNos.value.includes(item.itemNo)))
const selectedItemLabel = computed(() => selectedItems.value.map((item) => item.itemNo + ' · ' + item.itemName).join('；'))
const unionFactors = computed(() => {
  const seen = new Map<string, FactorDefinition>()
  for (const item of selectedItems.value) for (const factor of item.factors) {
    const previous = seen.get(factor.factorNo)
    seen.set(factor.factorNo, previous ? { ...previous, options: [...new Set([...previous.options, ...factor.options])] } : { ...factor })
  }
  return [...seen.values()]
})
const filteredQueryRows = computed(() => {
  const criteria = queryCriteria.value
  if (!queried.value || !criteria) return []
  return lotRows.filter((row) => criteria.itemNos.includes(row.itemNo) && row.createDate >= criteria.start && row.createDate <= criteria.end && criteria.factors.every((factor) => {
    const selected = factor.values
    return selected.length === 0 || selected.includes(row.factors[factor.factorNo] || '')
  }))
})
const eligibleLots = computed(() => filteredQueryRows.value.filter((row) => (resultFilterApplied.value.normal || row.excluded !== 0) && (resultFilterApplied.value.excluded || row.excluded !== 1) && (resultFilterApplied.value.retest || !row.retestLotNo)))
const eligibleErrors = computed(() => eligibleLots.value.flatMap((row) => row.errors.filter((error) => (resultFilterApplied.value.scrap && error.reasonType === 0) || (resultFilterApplied.value.defect && error.reasonType === 1)).map((error) => ({ ...error, lotNo: row.lotNo, serial: row.serial, sampleQty: row.sampleQty, pcsMultiplier: row.pcsMultiplier, createDate: row.createDate, excluded: row.excluded, itemNo: row.itemNo, factors: row.factors }))))
const summary = computed(() => ({
  sampleLots: new Set(eligibleLots.value.map((row) => row.serial)).size,
  sampleQty: eligibleLots.value.reduce((sum, row) => sum + row.sampleQty, 0),
  errorQty: eligibleErrors.value.reduce((sum, row) => sum + row.errorQty * (metric.value === 'pcs' ? row.pcsMultiplier : 1), 0),
  scrapLots: eligibleLots.value.filter((row) => row.scrapQty > 0).length,
  defectLots: eligibleLots.value.filter((row) => row.defectQty > 0).length,
}))
const dimensionAvailable = computed(() => eligibleErrors.value.length > 0 && eligibleErrors.value.every((row) => row.dimension !== null))
const paretoRows = computed<ParetoRow[]>(() => {
  const byReason = new Map<string, ParetoRow>()
  for (const error of eligibleErrors.value) {
    const row = byReason.get(error.errorNo) || { errorNo: error.errorNo, errorName: error.errorName, errorQty: 0, errorLotNum: 0, errorRate: 0, errorDimension: 0, errorPcs: 0, lotNos: [], cumulative: 0 }
    row.errorQty += error.errorQty
    row.errorLotNum += 1
    row.errorPcs += error.errorQty * error.pcsMultiplier
    row.errorRate += error.errorQty * 100 / Math.max(summary.value.sampleQty, 1)
    if (error.dimension !== null) row.errorDimension += error.dimension * error.errorQty
    row.lotNos.push(error.lotNo)
    byReason.set(error.errorNo, row)
  }
  const rows = [...byReason.values()]
  for (const row of rows) if (metric.value === 'pcs') row.errorQty = row.errorPcs
  const metricValue = (row: ParetoRow): number => metric.value === 'lot' ? row.errorLotNum : metric.value === 'rate' ? row.errorRate : metric.value === 'dimension' ? row.errorDimension : row.errorQty
  rows.sort((a, b) => metricValue(b) - metricValue(a) || a.errorNo.localeCompare(b.errorNo))
  const total = rows.reduce((sum, row) => sum + metricValue(row), 0)
  let cumulative = 0
  for (const row of rows) {
    cumulative += metricValue(row)
    row.cumulative = total ? cumulative * 100 / total : 0
  }
  const count = Number.isFinite(barNumber.value) && barNumber.value > 0 ? Math.min(rows.length, Math.floor(barNumber.value)) : rows.length
  return rows.slice(0, count)
})
const selectedErrors = computed(() => eligibleErrors.value.filter((error) => selectedErrorNos.value.includes(error.errorNo)))
const selectedReasons = computed(() => paretoRows.value.filter((row) => selectedErrorNos.value.includes(row.errorNo)))
const pieColors = ['#297d58', '#4e9b73', '#74b18c', '#e3a544', '#ca684a', '#8497aa']
const maxBarValue = computed(() => Math.max(1, ...paretoRows.value.map((row) => valueFor(row))))
const cumulativePoints = computed(() => paretoRows.value.map((row, index) => `${paretoRows.value.length <= 1 ? 50 : 42 + (index * 720) / (paretoRows.value.length - 1)},${224 - row.cumulative * 1.62}`).join(' '))
const pieGradient = computed(() => {
  const total = paretoRows.value.reduce((sum, row) => sum + valueFor(row), 0)
  let cursor = 0
  const slices = paretoRows.value.map((row, index) => {
    const next = cursor + (total ? valueFor(row) * 100 / total : 0)
    const slice = `${pieColors[index % pieColors.length]} ${cursor}% ${next}%`
    cursor = next
    return slice
  })
  return `conic-gradient(${slices.join(', ')})`
})
const modalTitles: Record<Exclude<DetailPanel, ''>, string> = {
  detail: 'Reason Detail', more: 'Selected Reason Comparison', fishbone: 'Fishbone Analysis',
  rank: 'Reason Rank Detail', section: 'Section Detail', pie: 'Pie Chart', weeks: 'Last Four Weeks',
  export: 'Export All Error', save: 'Save Graph', print: 'Print Graph',
}
const modalTitle = computed(() => activePanel.value ? modalTitles[activePanel.value] : '')

function selectItems(): void {
  factors.value = unionFactors.value.map((factor) => ({ ...factor, values: [], freeValue: '' }))
  pickerOpen.value = false
  queried.value = false
  queryCriteria.value = null
  selectedErrorNos.value = []
  notice.value = selectedItems.value.length + ' 個 Attribute QC Item 已選取；Factor 欄位為選取 QC Item 的聯集。'
}
function clearFactors(): void { for (const factor of factors.value) { factor.values = []; factor.freeValue = '' } }
function runQuery(): void {
  if (!selectedItems.value.length) { notice.value = '請先選擇 QC Item。'; return }
  if (fromDate.value && endDate.value && new Date(fromDate.value) > new Date(endDate.value)) { notice.value = '起始日期不可晚於結束日期。'; return }
  const filterSnapshot = { normal: includeNormal.value, excluded: includeExcluded.value, retest: includeRetest.value, scrap: includeScrap.value, defect: includeDefect.value }
  resultFilterDraft.value = { ...filterSnapshot }
  resultFilterApplied.value = { ...filterSnapshot }
  queryCriteria.value = {
    itemNos: [...selectedItemNos.value],
    start: fromDate.value ? new Date(fromDate.value).getTime() : Number.NEGATIVE_INFINITY,
    end: endDate.value ? new Date(endDate.value).getTime() + 86_399_999 : Number.POSITIVE_INFINITY,
    factors: factors.value.map((factor) => ({ factorNo: factor.factorNo, values: factor.parameterType === 2 ? factor.freeValue.split(',').map((entry) => entry.trim().toUpperCase()).filter(Boolean) : [...factor.values] })),
  }
  queried.value = true
  selectedErrorNos.value = []
  refreshedAt.value = new Date().toLocaleString('zh-TW', { hour12: false })
  notice.value = 'DEV 假資料查詢完成：載入 ' + filteredQueryRows.value.length + ' 筆 SPC 批次；統計由目前篩選條件重算。'
}
function refreshChart(): void {
  if (!Number.isFinite(barNumber.value) || barNumber.value < 0) { notice.value = 'Bar Number 請輸入大於或等於 0 的數字。'; return }
  resultFilterApplied.value = { ...resultFilterDraft.value }
  selectedErrorNos.value = selectedErrorNos.value.filter((errorNo) => paretoRows.value.some((row) => row.errorNo === errorNo))
  refreshedAt.value = new Date().toLocaleString('zh-TW', { hour12: false })
  notice.value = 'DEV 已重新計算目前的篩選條件與圖表。'
}
function toggleReason(errorNo: string): void {
  selectedErrorNos.value = selectedErrorNos.value.includes(errorNo) ? selectedErrorNos.value.filter((value) => value !== errorNo) : [...selectedErrorNos.value, errorNo]
}
function valueFor(row: ParetoRow): number { return metric.value === 'lot' ? row.errorLotNum : metric.value === 'rate' ? row.errorRate : metric.value === 'dimension' ? row.errorDimension : metric.value === 'pcs' ? row.errorPcs : row.errorQty }
function valueLabel(value: number): string { return metric.value === 'rate' ? value.toFixed(2) + '%' : value.toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function stateLabel(state: LotLog['excluded']): string { return state === 0 ? 'Normal' : state === 1 ? 'Excluded' : 'Isolated' }
function openPanel(panel: DetailPanel): void {
  if (panel === 'detail' && selectedErrorNos.value.length !== 1) { notice.value = '請先選一個 Reason Bar 查看明細。'; return }
  if (panel === 'more' && selectedErrorNos.value.length < 2) { notice.value = '請至少選兩個 Reason Bar 比較。'; return }
  activePanel.value = panel
}
function closePanel(): void { activePanel.value = '' }
function closePage(): void { emit('close') }

const weeklyRows = computed(() => Array.from({ length: 4 }, (_, index) => {
  const start = Date.now() - (4 - index) * 7 * 86_400_000
  const end = start + 7 * 86_400_000
  const logs = eligibleLots.value.filter((row) => row.createDate >= start && row.createDate < end)
  const errors = eligibleErrors.value.filter((row) => row.createDate >= start && row.createDate < end)
  return { label: 'W-' + (3 - index), lots: logs.length, qty: errors.reduce((sum, row) => sum + row.errorQty, 0) }
}))

</script>

<template>
  <section v-if="props.developmentPreview" class="pareto-page" aria-label="柏拉圖分析">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC210 · ATTRIBUTE PARETO</p><h1>柏拉圖分析</h1><p class="subtitle">依計數型 QC Item、Error Reason 與 Lot 條件排序檢視不良分布。</p></div>
      <button class="button" @click="closePage">Close (X)</button>
    </header>

    <section class="criteria panel">
      <div class="query-grid">
        <label class="wide">QC Item<input :value="selectedItemLabel" readonly placeholder="選一個或多個 Attribute QC Item"><button class="button get" @click="pickerSearch = ''; pickerOpen = true">Get</button></label>
        <label>From Date<input v-model="fromDate" type="date"></label>
        <label>End Date<input v-model="endDate" type="date"></label>
        <div class="checks"><b>Count Unit</b><label><input v-model="metric" type="radio" value="piece"> By Piece Qty</label><label><input v-model="metric" type="radio" value="lot"> By Lot Number</label></div>
        <div class="checks"><b>Reason Type</b><label><input v-model="includeScrap" type="checkbox"> Scrap</label><label><input v-model="includeDefect" type="checkbox"> Defect</label></div>
        <div class="checks"><b>Lot Report State</b><label><input v-model="includeNormal" type="checkbox"> Normal</label><label><input v-model="includeExcluded" type="checkbox"> Excluded</label></div>
        <label class="ret-test"><input v-model="includeRetest" type="checkbox"> Include ReTest Lot</label>
        <button class="button primary query" :disabled="!selectedItems.length" @click="runQuery">Query</button>
      </div>
      <div class="factor-head"><div><h2>Collect Factor</h2><span>{{ factors.length }} 個 QC Item 要因；空白代表不篩選</span></div><button class="button" :disabled="!factors.length" @click="clearFactors">Clear selections</button></div>
      <div class="table-scroll"><table><thead><tr><th>Factor No</th><th>要因</th><th>ParameterType</th><th>Factor Value</th></tr></thead><tbody>
        <tr v-for="factor in factors" :key="factor.factorNo"><td class="serial">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType === 2 ? 'Free input' : factor.parameterType === 1 ? 'Function list' : 'Table list' }}</td><td><input v-if="factor.parameterType === 2" v-model="factor.freeValue" class="factor-input" placeholder="多值以逗號分隔"><select v-else v-model="factor.values" multiple size="2" class="factor-select"><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select></td></tr>
        <tr v-if="!factors.length"><td colspan="4" class="empty">先選 QC Item 載入 Factor metadata。</td></tr>
      </tbody></table></div>
      <p class="note">Query 依 QC Item／日期／Factor 載入假 log；狀態、重測批與 Scrap／Defect 條件在結果畫面重新計數。</p>
    </section>

    <section class="notice panel" role="status">{{ notice }}</section>

    <section v-if="queried" class="results panel">
      <header class="result-head"><div><p class="eyebrow">{{ selectedItems.length }} QC Items · {{ refreshedAt }}</p><h2>Reason Distribution · Pareto Chart</h2></div><div class="bar-limit"><label>Bar Number<input v-model.number="barNumber" type="number" min="0"></label><button class="button" @click="refreshChart">Re-Query</button></div></header>
      <div class="summary-grid"><div><small>TOTAL ERROR QTY</small><b>{{ summary.errorQty.toLocaleString('zh-TW') }}</b></div><div><small>SAMPLE QTY</small><b>{{ summary.sampleQty.toLocaleString('zh-TW') }}</b></div><div><small>SAMPLE LOTS</small><b>{{ summary.sampleLots }}</b></div><div><small>SCRAP LOTS</small><b>{{ summary.scrapLots }}</b></div><div><small>DEFECT LOTS</small><b>{{ summary.defectLots }}</b></div><div><small>REASONS</small><b>{{ paretoRows.length }}</b></div></div>
      <div class="result-filters"><span>Result Filters</span><label><input v-model="resultFilterDraft.normal" type="checkbox"> Normal</label><label><input v-model="resultFilterDraft.excluded" type="checkbox"> Excluded</label><label><input v-model="resultFilterDraft.retest" type="checkbox"> Include ReTest Lot</label><label><input v-model="resultFilterDraft.scrap" type="checkbox"> Scrap</label><label><input v-model="resultFilterDraft.defect" type="checkbox"> Defect</label><small>按 Re-Query 套用</small></div>
      <div class="result-toolbar"><div class="metric-choice"><span>Rank by</span><label><input v-model="metric" type="radio" value="piece"> Piece Qty</label><label><input v-model="metric" type="radio" value="lot"> Lot Number</label><label><input v-model="metric" type="radio" value="rate"> Defect Rate</label><label><input v-model="metric" type="radio" value="dimension" :disabled="!dimensionAvailable"> Dimension</label><label><input v-model="metric" type="radio" value="pcs"> Defect PCs</label></div></div>
      <div class="analytics-layout">
        <section class="chart-card"><div class="chart-title"><div><h3>{{ metric === 'lot' ? 'Lot Number' : metric === 'rate' ? 'Defect Rate' : metric === 'dimension' ? 'Defect Dimension' : metric === 'pcs' ? 'Defect PCs' : 'Reason Qty' }}</h3><span>依指標由大到小排列；圖表點選 Reason 可查看批次。</span></div><span>Top {{ paretoRows.length }}</span></div>
          <div v-if="paretoRows.length" class="chart-scroll"><div class="pareto-chart"><div class="bar-grid"><div v-for="row in paretoRows" :key="row.errorNo" class="bar-column" :class="{ chosen: selectedErrorNos.includes(row.errorNo) }" @click="toggleReason(row.errorNo)"><span class="bar-value">{{ valueLabel(valueFor(row)) }}</span><div class="bar-area"><div class="bar" :style="{ height: Math.max(3, valueFor(row) / maxBarValue * 100) + '%' }"></div></div><span class="bar-label">{{ row.errorName }}</span><small>{{ row.errorNo }}</small></div></div><svg class="cumulative" viewBox="0 0 804 250" preserveAspectRatio="none" aria-label="累積百分比"><line x1="40" y1="224" x2="764" y2="224"/><polyline :points="cumulativePoints"/><circle v-for="(row, index) in paretoRows" :key="row.errorNo" :cx="paretoRows.length <= 1 ? 50 : 42 + (index * 720) / (paretoRows.length - 1)" :cy="224 - row.cumulative * 1.62" r="3"/><text x="735" y="14">100%</text></svg></div></div>
          <p v-else class="empty chart-empty">目前條件下沒有 ErrorQty &gt; 0 的 Reason。</p>
          <div class="legend"><span><i></i>Reason Qty / Lot / Rate</span><span><b>—</b> cumulative % (Piece／Lot／Dimension／PCS)</span></div>
        </section>
        <section class="reason-card"><header><h3>Reason Summary</h3><span>{{ selectedErrorNos.length }} selected</span></header><div class="reason-scroll"><table><thead><tr><th>Select</th><th>Error No</th><th>Reason</th><th>Qty</th><th>Lots</th><th>Rate</th></tr></thead><tbody><tr v-for="row in paretoRows" :key="row.errorNo" :class="{ chosen: selectedErrorNos.includes(row.errorNo) }"><td><input type="checkbox" :checked="selectedErrorNos.includes(row.errorNo)" @change="toggleReason(row.errorNo)"></td><td class="serial">{{ row.errorNo }}</td><td>{{ row.errorName }}</td><td class="number">{{ row.errorQty.toLocaleString('zh-TW') }}</td><td class="number">{{ row.errorLotNum }}</td><td class="number">{{ row.errorRate.toFixed(2) }}%</td></tr><tr v-if="!paretoRows.length"><td colspan="6" class="empty">尚無可繪製資料。</td></tr></tbody></table></div>
          <div class="reason-detail"><h4>Selected Reason Lot List</h4><p v-if="!selectedErrorNos.length" class="empty">點選圖表柱或勾選 Reason。</p><template v-else><p v-for="reason in selectedReasons" :key="reason.errorNo"><b>{{ reason.errorName }}</b> · {{ reason.lotNos.length }} 筆 reason log</p></template><button class="button" :disabled="selectedErrorNos.length !== 1" @click="openPanel('detail')">Reason Detail</button></div>
        </section>
      </div>
      <div class="action-row"><button class="button" @click="openPanel('more')">Compare Selected</button><button class="button" @click="openPanel('fishbone')">Fishbone</button><button class="button" @click="openPanel('rank')">Rank Detail</button><button class="button" @click="openPanel('section')">Section Detail</button><button class="button" @click="openPanel('pie')">Pie Chart</button><button class="button" @click="openPanel('weeks')">Last 4 Weeks</button><button class="button" @click="openPanel('export')">Export All Error</button><button class="button" @click="openPanel('save')">Save Graph</button><button class="button" @click="openPanel('print')">Print</button></div>
      <p class="disclaimer">圖表、數量、Factor、Limit／Property、Lot、Reason 與分析結果均為 DEV 假資料；PCS、Dimension、魚骨、Rank、Section 與近四週視圖是互動示意，不連 Service／Oracle／API／資料庫。Export／Save／Print 只顯示模擬說明，不建立檔案或呼叫印表機。</p>
    </section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="dialog picker" role="dialog" aria-modal="true" aria-label="選擇 Attribute QC Item"><header><div><p class="eyebrow">ATTRIBUTE QC ITEM LOOKUP</p><h2>選擇一個或多個 QC Item</h2></div><button class="button" @click="pickerOpen = false">Close</button></header><label class="search">Search<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><div class="picker-rows"><label v-for="item in pickerItems" :key="item.itemNo" class="picker-row"><input v-model="selectedItemNos" type="checkbox" :value="item.itemNo"><b>{{ item.itemNo }}</b><span>{{ item.itemName }}</span><span>Attribute</span></label></div><footer><span>{{ selectedItemNos.length }} selected</span><button class="button primary" @click="selectItems">Apply Items</button></footer></section></div>

    <div v-if="activePanel" class="modal-backdrop" @click.self="closePanel"><section class="dialog analysis-dialog" role="dialog" aria-modal="true"><header><div><p class="eyebrow">SC210 · DEV ANALYSIS PREVIEW</p><h2>{{ modalTitle }}</h2></div><button class="button" @click="closePanel">Close</button></header>
      <template v-if="activePanel === 'detail'"><div class="panel-summary" v-for="reason in selectedReasons" :key="reason.errorNo"><b>{{ reason.errorNo }} · {{ reason.errorName }}</b><span>Qty {{ reason.errorQty }} · {{ reason.errorLotNum }} reason log · {{ reason.errorRate.toFixed(2) }}%</span></div><div class="table-scroll"><table><thead><tr><th>Lot No</th><th>QC Item</th><th>Date</th><th>Qty</th><th>Lot State</th><th>Section</th></tr></thead><tbody><tr v-for="(error, errorIndex) in selectedErrors" :key="error.serial + error.errorNo + errorIndex"><td>{{ error.lotNo }}</td><td>{{ error.itemNo }}</td><td>{{ new Date(error.createDate).toLocaleDateString('zh-TW') }}</td><td>{{ error.errorQty }}</td><td>{{ stateLabel(error.excluded) }}</td><td>{{ error.sectionNo }}</td></tr></tbody></table></div></template>
      <template v-else-if="activePanel === 'more'"><p class="panel-note">選取 Reason 的假資料比較：數量與批次 log。</p><div class="comparison-grid"><div v-for="reason in selectedReasons" :key="reason.errorNo"><small>{{ reason.errorNo }}</small><b>{{ reason.errorName }}</b><strong>{{ valueLabel(valueFor(reason)) }}</strong><span>{{ reason.errorLotNum }} logs</span></div></div></template>
      <template v-else-if="activePanel === 'fishbone'"><p class="panel-note">來源魚骨畫面會依選取 Error 與 Collect Factor 分析；此處只用本機假 Factor counts。</p><div class="fishbone"><div v-for="factor in factors" :key="factor.factorNo"><b>{{ factor.factorName }}</b><span>{{ [...new Set(selectedErrors.map((error) => error.factors[factor.factorNo]).filter(Boolean))].join('、') || '所有假資料' }}</span></div><strong>{{ selectedReasons.map((reason) => reason.errorName).join('、') || '全部 Reason' }}</strong></div></template>
      <template v-else-if="activePanel === 'rank'"><p class="panel-note">Top Reason 與其 Rank1–Rank4 類別摘要。</p><div class="table-scroll"><table><thead><tr><th>Rank</th><th>Reason</th><th>Qty</th><th>Rank1</th><th>Rank2</th><th>Rank3</th><th>Rank4</th></tr></thead><tbody><tr v-for="(row, index) in paretoRows" :key="row.errorNo"><td>{{ index + 1 }}</td><td>{{ row.errorName }}</td><td>{{ row.errorQty }}</td><td>{{ eligibleErrors.find((error) => error.errorNo === row.errorNo)?.rank1 || 'Fixture' }}</td><td>{{ eligibleErrors.find((error) => error.errorNo === row.errorNo)?.rank2 || 'Fixture' }}</td><td>{{ eligibleErrors.find((error) => error.errorNo === row.errorNo)?.rank3 || 'Fixture' }}</td><td>{{ eligibleErrors.find((error) => error.errorNo === row.errorNo)?.rank4 || 'Fixture' }}</td></tr></tbody></table></div></template>
      <template v-else-if="activePanel === 'section'"><p class="panel-note">舊 Service 會另外查 TBLSPCATTDETAILLOGSECTION；此 DEV 結果為假 SectionNo 分布，正式 DDL／metadata 尚缺。</p><div class="table-scroll"><table><thead><tr><th>Section</th><th>Related Reasons</th><th>Error Qty</th><th>Lots</th></tr></thead><tbody><tr v-for="section in [...new Set(eligibleErrors.map((error) => error.sectionNo))].map((sectionNo) => ({ sectionNo, errors: eligibleErrors.filter((error) => error.sectionNo === sectionNo) }))" :key="section.sectionNo"><td>{{ section.sectionNo }}</td><td>{{ [...new Set(section.errors.map((error) => error.errorName))].join('、') }}</td><td>{{ section.errors.reduce((sum, error) => sum + error.errorQty, 0) }}</td><td>{{ new Set(section.errors.map((error) => error.serial)).size }}</td></tr></tbody></table></div></template>
      <template v-else-if="activePanel === 'pie'"><div v-if="paretoRows.length" class="pie-content"><div class="pie" :style="{ background: pieGradient }"></div><ul><li v-for="(row, index) in paretoRows" :key="row.errorNo"><i :style="{ background: pieColors[index % pieColors.length] }"></i>{{ row.errorName }} · {{ valueLabel(valueFor(row)) }}</li></ul></div><p v-else class="empty">沒有可繪製的 Reason。</p></template>
      <template v-else-if="activePanel === 'weeks'"><p class="panel-note">模擬依最近四週重查同一 QC／Factor 條件。</p><div class="week-grid"><div v-for="week in weeklyRows" :key="week.label"><small>{{ week.label }}</small><b>{{ week.qty }} errors</b><span>{{ week.lots }} lots</span></div></div></template>
      <template v-else><div class="notice-box">{{ activePanel === 'export' ? 'DEV 已模擬 Export All Error；未產生 Excel 檔。' : activePanel === 'save' ? 'DEV 已模擬 Save Graph；未產生圖片檔。' : 'DEV 已模擬 Print；沒有呼叫印表機。' }}</div></template>
    </section></div>
  </section>
</template>

<style scoped>
.pareto-page { --ink:#25312a; --muted:#68756d; --line:#d4ddd6; --paper:#fff; --canvas:#f1f5f1; --green:#287552; min-height:100%; height:100%; overflow:auto; box-sizing:border-box; padding:16px; background:var(--canvas); color:var(--ink); font:inherit; }
.page-head,.result-head,.factor-head,.chart-title,.reason-card>header { display:flex; align-items:center; justify-content:space-between; gap:14px; }.page-head { margin-bottom:12px; }.page-head h1,.result-head h2 { margin:2px 0; font-size:22px; }.page-head p,.result-head p { margin:3px 0; color:var(--muted); }.eyebrow { color:var(--green)!important; font-size:10px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:12px; }.panel { margin-bottom:11px; border:1px solid var(--line); border-radius:6px; background:var(--paper); box-shadow:0 1px 3px #28342b0b; }.criteria { padding:13px; }.query-grid { display:grid; grid-template-columns:minmax(280px,1.7fr) minmax(130px,.8fr) minmax(130px,.8fr) minmax(120px,.8fr) minmax(120px,.8fr) minmax(130px,.8fr) auto auto; align-items:end; gap:9px; }.query-grid>label:not(.ret-test) { display:grid; gap:4px; color:var(--muted); font-size:11px; font-weight:700; }.query-grid input:not([type=checkbox]):not([type=radio]) { min-height:32px; border:1px solid #c9d2cb; border-radius:4px; padding:4px 8px; box-sizing:border-box; background:white; color:var(--ink); font:inherit; }.query-grid .wide { position:relative; }.wide input { padding-right:52px!important; }.get { position:absolute; right:3px; bottom:3px; min-height:25px!important; padding:2px 8px!important; }.checks { display:grid; align-content:start; gap:3px; color:var(--muted); font-size:10px; }.checks b { color:var(--ink); }.checks label,.ret-test { white-space:nowrap; }.ret-test { display:flex; gap:4px; align-items:center; color:var(--muted); font-size:10px; }.query { min-width:76px; }.factor-head { margin:13px 0 7px; }.factor-head h2 { margin:0; font-size:14px; }.factor-head span,.note { color:var(--muted); font-size:10px; }.table-scroll,.reason-scroll { max-width:100%; overflow:auto; border:1px solid var(--line); border-radius:4px; }table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }th,td { padding:6px 8px; border-bottom:1px solid #e8ede9; text-align:left; }th { position:sticky; top:0; z-index:1; background:#f2f6f2; color:#58665d; font-size:10px; }tbody tr:last-child td { border-bottom:0; }.serial { color:#286a4d; font-family:Consolas,monospace; }.factor-input,.factor-select { min-width:220px; min-height:31px; border:1px solid #c9d2cb; border-radius:4px; padding:4px 7px; background:white; font:inherit; }.note { margin:7px 0 0; }.notice { padding:8px 12px; color:#536158; font-size:11px; }.results { padding:13px; }.result-head h2 { font-size:17px; }.bar-limit { display:flex; align-items:end; gap:8px; }.bar-limit label { display:grid; gap:3px; color:var(--muted); font-size:10px; }.bar-limit input { width:78px; min-height:30px; border:1px solid #c9d2cb; border-radius:4px; padding:4px 7px; font:inherit; }.summary-grid { display:grid; grid-template-columns:repeat(6,minmax(85px,1fr)); gap:7px; margin:10px 0; }.summary-grid>div { display:grid; gap:3px; padding:8px; border:1px solid var(--line); border-radius:4px; background:#fafcfb; }.summary-grid small { color:var(--muted); font-size:9px; font-weight:800; }.summary-grid b { font-size:14px; }.result-toolbar { padding:8px 0; border-top:1px solid #e5ebe6; }.metric-choice { display:flex; flex-wrap:wrap; gap:12px; color:var(--muted); font-size:10px; }.metric-choice span { color:var(--ink); font-weight:750; }.analytics-layout { display:grid; grid-template-columns:minmax(360px,1.2fr) minmax(350px,.8fr); gap:10px; }.chart-card,.reason-card { min-width:0; padding:9px; border:1px solid var(--line); border-radius:4px; background:white; }.chart-title { margin-bottom:6px; }.chart-title h3,.reason-card h3 { margin:0; font-size:13px; }.chart-title span,.reason-card>header span { color:var(--muted); font-size:10px; }.chart-title div span { display:block; margin-top:3px; }.chart-scroll { overflow:auto; }.pareto-chart { position:relative; min-width:530px; height:285px; padding:10px 12px 0; }.bar-grid { position:absolute; inset:10px 12px 28px; display:flex; align-items:stretch; justify-content:space-around; border-bottom:1px solid #65736a; background:repeating-linear-gradient(to bottom,transparent 0,transparent 51px,#edf1ed 52px,#edf1ed 53px); }.bar-column { position:relative; z-index:2; flex:1; min-width:34px; max-width:90px; display:grid; grid-template-rows:18px 1fr 28px 13px; justify-items:center; cursor:pointer; }.bar-column.chosen .bar { background:#c36d4a; }.bar-value { color:#4e5e53; font-size:9px; }.bar-area { width:min(58px,70%); height:100%; display:flex; align-items:end; }.bar { width:100%; min-height:3px; border-radius:3px 3px 0 0; background:#32825b; transition:height .18s ease; }.bar-label { max-width:90px; overflow:hidden; padding-top:3px; color:#536057; font-size:9px; text-overflow:ellipsis; white-space:nowrap; }.bar-column small { color:#78847b; font-size:8px; }.cumulative { position:absolute; z-index:3; inset:16px 12px 31px; width:calc(100% - 24px); height:238px; overflow:visible; pointer-events:none; }.cumulative line { stroke:#d99532; stroke-width:1; stroke-dasharray:3 3; }.cumulative polyline { fill:none; stroke:#d99532; stroke-width:2; }.cumulative circle { fill:#d99532; stroke:white; stroke-width:1; }.cumulative text { fill:#9a6c24; font-size:10px; }.legend { display:flex; flex-wrap:wrap; gap:13px; padding:5px 2px 0; color:var(--muted); font-size:9px; }.legend i { display:inline-block; width:10px; height:10px; margin-right:4px; border-radius:2px; background:#32825b; }.legend b { color:#d99532; }.chart-empty { min-height:210px; display:grid; place-items:center; }.reason-card { display:grid; grid-template-rows:auto minmax(0,1fr) auto; }.reason-card>header { padding:1px 2px 7px; }.reason-scroll { max-height:290px; }.reason-card tbody tr.chosen { background:#edf6ef; }.reason-card input { accent-color:var(--green); }.number { text-align:right; font-variant-numeric:tabular-nums; }.reason-detail { padding:8px 2px 0; border-top:1px solid var(--line); }.reason-detail h4 { margin:0 0 5px; font-size:11px; }.reason-detail p { margin:3px 0; color:var(--muted); font-size:10px; }.empty { color:#8c968e; text-align:center; }.reason-detail .empty { padding:3px; }.action-row { display:flex; flex-wrap:wrap; gap:6px; margin-top:9px; }.disclaimer { margin:7px 0 0; color:var(--muted); font-size:9px; }.button { min-height:30px; padding:4px 9px; border:1px solid #c4cec6; border-radius:4px; background:#fff; color:#344137; font:inherit; font-size:10px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:white; }.button:disabled { cursor:not-allowed; opacity:.48; }.modal-backdrop { position:fixed; inset:0; z-index:130; display:grid; place-items:center; padding:18px; background:#1d2822a8; }.dialog { width:min(900px,96vw); max-height:88vh; display:grid; grid-template-rows:auto minmax(0,1fr) auto; overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.dialog>header { display:flex; align-items:center; justify-content:space-between; padding:12px 15px; border-bottom:1px solid var(--line); }.dialog h2 { margin:2px 0 0; font-size:17px; }.picker { grid-template-rows:auto auto minmax(0,1fr) auto; }.search { display:grid; gap:4px; padding:10px 15px; color:var(--muted); font-size:10px; }.search input { min-height:30px; padding:4px 8px; border:1px solid var(--line); border-radius:4px; font:inherit; }.picker-rows { overflow:auto; padding:0 15px; }.picker-row { display:grid; grid-template-columns:22px 150px 1fr 100px; align-items:center; gap:8px; padding:9px 5px; border-bottom:1px solid #edf1ed; font-size:11px; }.picker-row b { color:var(--green); font-family:Consolas,monospace; }.picker footer { display:flex; justify-content:space-between; align-items:center; padding:10px 15px; border-top:1px solid var(--line); color:var(--muted); font-size:10px; }.analysis-dialog { padding-bottom:12px; }.analysis-dialog>header { margin-bottom:8px; }.analysis-dialog>template { min-height:0; overflow:auto; }.analysis-dialog :deep(.table-scroll) { margin:0 13px; max-height:52vh; }.panel-summary { display:flex; justify-content:space-between; gap:12px; padding:8px 14px; font-size:11px; }.panel-summary span,.panel-note { color:var(--muted); font-size:10px; }.panel-note { margin:4px 14px 10px; }.comparison-grid,.week-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(140px,1fr)); gap:8px; padding:8px 14px; }.comparison-grid>div,.week-grid>div { display:grid; gap:5px; padding:10px; border:1px solid var(--line); border-radius:4px; }.comparison-grid small,.week-grid small { color:var(--green); font-size:9px; font-weight:800; }.comparison-grid b,.week-grid b { font-size:11px; }.comparison-grid strong { color:var(--green); font-size:20px; }.comparison-grid span,.week-grid span { color:var(--muted); font-size:10px; }.fishbone { display:grid; grid-template-columns:1fr 1fr; gap:8px; padding:10px 15px; }.fishbone>div { display:grid; gap:5px; padding:10px; border:1px solid var(--line); border-radius:4px; background:#f8faf8; font-size:11px; }.fishbone span { color:var(--muted); font-size:10px; }.fishbone>strong { grid-column:1/-1; padding:10px; background:#eaf3ec; color:var(--green); text-align:center; }.pie-content { display:flex; align-items:center; gap:24px; padding:14px 20px; }.pie { width:220px; aspect-ratio:1; border-radius:50%; border:4px solid white; box-shadow:0 0 0 1px var(--line); }.pie-content ul { display:grid; gap:7px; padding:0; list-style:none; color:var(--muted); font-size:11px; }.pie-content li { display:flex; align-items:center; gap:7px; }.pie-content i { width:10px; height:10px; border-radius:50%; background:var(--green); }.notice-box { margin:14px; padding:18px; border-radius:5px; background:#f3f6f3; color:var(--muted); text-align:center; font-size:12px; }
@media(max-width:1100px) { .query-grid { grid-template-columns:repeat(4,minmax(120px,1fr)); }.query-grid .wide { grid-column:span 2; }.ret-test { grid-column:span 2; }.analytics-layout { grid-template-columns:1fr; }.summary-grid { grid-template-columns:repeat(3,minmax(85px,1fr)); } }
@media(max-width:650px) { .pareto-page { padding:9px; }.page-head,.result-head { align-items:flex-start; flex-direction:column; }.query-grid { grid-template-columns:repeat(2,minmax(110px,1fr)); }.query-grid .wide { grid-column:1/-1; }.summary-grid { grid-template-columns:repeat(2,minmax(90px,1fr)); }.bar-limit { width:100%; justify-content:flex-start; }.metric-choice { gap:8px; }.picker-row { grid-template-columns:22px 1fr; }.pie-content { align-items:flex-start; flex-direction:column; }.pie { width:min(220px,70vw); } }
</style>

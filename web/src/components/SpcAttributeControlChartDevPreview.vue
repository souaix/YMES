<script setup lang="ts">
import { computed, ref } from 'vue'

type ChartKey = 'pScrap' | 'npScrap' | 'cDefect' | 'uDefect'
type ReportFilter = 'all' | 'scrap' | 'defect'
type LimitField = 'us' | 'ls' | 'ucl' | 'cl' | 'lcl'
const limitFields: LimitField[] = ['us', 'ls', 'ucl', 'cl', 'lcl']
type Factor = { factorNo: string; factorName: string; parameterType: 0 | 1 | 2; options: string[]; factorOnline: boolean; factorSpec: boolean }
type LimitSet = Partial<Record<LimitField, number>>
type Detail = { errorNo: string; errorName: string; errorQty: number; errorType: 0 | 1 }
type QCItem = { qcItemNo: string; qcItemName: string; issueState: 2 | -1; limitLotNum: number; factors: Factor[]; seedLimits: Record<ChartKey, LimitSet> }
type Lot = { spcSerial: string; lotNo: string; createDate: Date; sampleQty: number; scrapQty: number; defectQty: number; result: number; excluded: boolean; erfNo: string; factors: Record<string, string>; details: Detail[] }
type ChartModel = { key: ChartKey; title: string; unit: 'count' | 'ratio'; values: number[]; uclValues: number[]; lclValues: number[]; limits: LimitSet; lineColor: string; mean: number }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const today = new Date()
const defaultFrom = new Date(today.getFullYear(), today.getMonth() - 2, today.getDate() + 1)
const fromDate = ref(toDateInput(defaultFrom))
const endDate = ref(toDateInput(today))
const useFromDate = ref(false)
const useEndDate = ref(false)
const normalState = ref(true)
const excludedState = ref(false)
const showBarChart = ref(true)
const showInOnePage = ref(false)
const selectedItem = ref<QCItem | null>(null)
const selectedFactors = ref<Record<string, string[]>>({})
const factorText = ref<Record<string, string>>({})
const pickerOpen = ref(false)
const pickerSearch = ref('')
const reportFilter = ref<ReportFilter>('all')
const axisLabel = ref('LotNo')
const queryRows = ref<Lot[]>([])
const queried = ref(false)
const selectedSerial = ref('')
const stateOverrides = ref<Record<string, boolean>>({})
const changedSerials = ref<string[]>([])
const limitOverrides = ref<Partial<Record<ChartKey, LimitSet>>>({})
const savedLimitByItem = ref<Record<string, Partial<Record<ChartKey, LimitSet>>>>({})
const limitDialogOpen = ref(false)
const limitDraft = ref<Record<ChartKey, Record<LimitField, string>>>(emptyLimitDraft())
const notice = ref('請選擇 Attribute QC Item、Lot 狀態及要因，再執行 Query。日期起訖預設不套用。')
const feedback = ref('')
const lotActionOpen = ref(false)

function emptyLimitDraft(): Record<ChartKey, Record<LimitField, string>> {
  const fields = (): Record<LimitField, string> => ({ us: '', ls: '', ucl: '', cl: '', lcl: '' })
  return { pScrap: fields(), npScrap: fields(), cDefect: fields(), uDefect: fields() }
}
function toDateInput(value: Date): string {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
}
function makeLimits(us: number, ls = 0): LimitSet { return { us, ls } }
const items: QCItem[] = [
  { qcItemNo: 'QC-A-2201', qcItemName: 'Lead Frame Surface Defect', issueState: 2, limitLotNum: 36, seedLimits: { pScrap: makeLimits(0.075), npScrap: makeLimits(17), cDefect: makeLimits(22), uDefect: makeLimits(0.065) }, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'], factorOnline: true, factorSpec: true },
    { factorNo: 'MACHINE', factorName: '沖壓機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'], factorOnline: true, factorSpec: false },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [], factorOnline: false, factorSpec: false },
  ] },
  { qcItemNo: 'QC-A-2237', qcItemName: 'Wire Bond Appearance', issueState: 2, limitLotNum: 32, seedLimits: { pScrap: makeLimits(0.082), npScrap: makeLimits(19), cDefect: makeLimits(25), uDefect: makeLimits(0.075) }, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'], factorOnline: true, factorSpec: true },
    { factorNo: 'BONDER', factorName: '焊線機', parameterType: 1, options: ['WB-12', 'WB-15', 'WB-20'], factorOnline: true, factorSpec: false },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [], factorOnline: false, factorSpec: false },
  ] },
  { qcItemNo: 'QC-A-2290', qcItemName: 'Mold Compound Contamination', issueState: -1, limitLotNum: 28, seedLimits: { pScrap: makeLimits(0.09), npScrap: makeLimits(16), cDefect: makeLimits(24), uDefect: makeLimits(0.08) }, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'], factorOnline: true, factorSpec: true },
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'], factorOnline: true, factorSpec: false },
  ] },
]

function chooseItem(item: QCItem): void {
  selectedItem.value = item
  selectedFactors.value = {}
  factorText.value = {}
  queryRows.value = []
  stateOverrides.value = {}
  changedSerials.value = []
  limitOverrides.value = structuredClone(savedLimitByItem.value[item.qcItemNo] || item.seedLimits)
  selectedSerial.value = ''
  queried.value = false
  pickerOpen.value = false
  notice.value = `${item.qcItemNo} 已載入 Attribute QC Item 與 Factor metadata；DEV 使用本機假資料。`
}
function readFactorValues(factor: Factor): string[] {
  return factor.parameterType === 2
    ? (factorText.value[factor.factorNo] || '').split(',').map((value) => value.trim().toUpperCase()).filter(Boolean)
    : selectedFactors.value[factor.factorNo] || []
}
function toggleFactor(factorNo: string, value: string): void {
  const selected = selectedFactors.value[factorNo] || []
  selectedFactors.value = { ...selectedFactors.value, [factorNo]: selected.includes(value) ? selected.filter((entry) => entry !== value) : [...selected, value] }
}
function upperCaseFactor(factorNo: string): void { factorText.value[factorNo] = (factorText.value[factorNo] || '').toUpperCase() }
const pickerItems = computed(() => items.filter((item) => `${item.qcItemNo} ${item.qcItemName}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const factorCaption = computed(() => {
  if (!selectedItem.value) return '尚未載入 Factor'
  const active = selectedItem.value.factors.flatMap((factor) => {
    const values = readFactorValues(factor)
    return values.length ? [`${factor.factorName}: ${values.join(', ')}`] : []
  })
  return active.length ? active.join(' · ') : '全部 Factor'
})

function generatedLots(item: QCItem): Lot[] {
  const rows: Lot[] = []
  const firstDay = new Date(today.getFullYear(), today.getMonth() - 7, today.getDate())
  for (let index = 0; index < 156; index += 1) {
    const createDate = new Date(firstDay.getFullYear(), firstDay.getMonth(), firstDay.getDate() + index * 1.4, 7 + index % 12, index * 11 % 60)
    const sampleQty = 64 + (index * 37) % 121
    const exceptional = index % 37 === 12
    const scrapQty = exceptional ? Math.ceil(sampleQty * 0.22) : (index * 7 + Math.floor(index / 3)) % 7
    const defectQty = exceptional ? Math.ceil(sampleQty * 0.16) : Math.max(0, (index * 5 + Math.floor(index / 2)) % Math.max(scrapQty + 3, 1))
    const excluded = index % 19 === 8
    const factors: Record<string, string> = {
      LINE: ['LN-A', 'LN-B', 'LN-C'][index % 3],
      MACHINE: ['PRESS-03', 'PRESS-05', 'PRESS-08'][Math.floor(index / 2) % 3],
      BONDER: ['WB-12', 'WB-15', 'WB-20'][index % 3],
      SHIFT: ['A', 'B', 'C'][Math.floor(index / 3) % 3],
    }
    const details: Detail[] = []
    if (scrapQty > 0) details.push({ errorNo: index % 2 ? 'E-110' : 'E-204', errorName: index % 2 ? '表面刮傷' : '鍍層異物', errorQty: scrapQty, errorType: 0 })
    if (defectQty > 0) details.push({ errorNo: index % 3 ? 'E-321' : 'E-415', errorName: index % 3 ? '端子變形' : '焊點偏移', errorQty: defectQty, errorType: 1 })
    rows.push({
      spcSerial: `SPC-ATT-${String(index + 1).padStart(5, '0')}`,
      lotNo: `LOT-26K-${String((index * 23 + 157) % 999).padStart(3, '0')}`,
      createDate,
      sampleQty,
      scrapQty,
      defectQty,
      result: exceptional || index % 8 === 3 ? 1 : 0,
      excluded,
      erfNo: index % 37 === 12 ? `ERF-26-${String(1040 + index)}` : '',
      factors,
      details,
    })
  }
  return item.qcItemNo.endsWith('2237') ? rows.map((row, index) => ({ ...row, defectQty: Math.min(row.sampleQty, row.defectQty + index % 4) })) : rows
}
function matchesState(row: Lot): boolean {
  if (normalState.value && excludedState.value) return true
  if (normalState.value) return !row.excluded
  return excludedState.value && row.excluded
}
function runQuery(): void {
  if (!selectedItem.value) { notice.value = '請先選擇 QC Item。'; return }
  if (!normalState.value && !excludedState.value) { notice.value = '請至少選取 Normal 或 Excluded。'; return }
  if (useFromDate.value && useEndDate.value && fromDate.value > endDate.value) { notice.value = '起始日期不可晚於結束日期。'; return }
  const from = useFromDate.value ? new Date(`${fromDate.value}T00:00:00`) : null
  const to = useEndDate.value ? new Date(`${endDate.value}T23:59:59.999`) : null
  const factorFilters = selectedItem.value.factors.map((factor) => [factor.factorNo, readFactorValues(factor)] as const).filter(([, values]) => values.length)
  queryRows.value = generatedLots(selectedItem.value).filter((row) => (!from || row.createDate >= from) && (!to || row.createDate <= to))
    .filter(matchesState)
    .filter((row) => factorFilters.every(([factorNo, values]) => values.includes(row.factors[factorNo] || '')))
    .sort((a, b) => a.spcSerial.localeCompare(b.spcSerial))
  queried.value = true
  stateOverrides.value = {}
  changedSerials.value = []
  selectedSerial.value = queryRows.value[0]?.spcSerial || ''
  feedback.value = ''
  notice.value = queryRows.value.length ? `查詢完成：${queryRows.value.length} 個計數批次；${factorCaption.value}。` : '查無符合條件的批次。'
}
function excluded(row: Lot): boolean { return stateOverrides.value[row.spcSerial] ?? row.excluded }
function baselineRows(): Lot[] { return queryRows.value.filter((row) => !excluded(row)) }
function ratioLimits(mean: number, sampleQty: number): { ucl: number; lcl: number } {
  if (sampleQty <= 0) return { ucl: mean, lcl: 0 }
  const spread = 3 * Math.sqrt(Math.max(mean * (1 - mean), 0) / sampleQty)
  return { ucl: mean + spread, lcl: Math.max(mean - spread, 0) }
}
function poissonLimits(mean: number): { ucl: number; lcl: number } {
  const spread = 3 * Math.sqrt(Math.max(mean, 0))
  return { ucl: mean + spread, lcl: Math.max(mean - spread, 0) }
}
const chartModels = computed<ChartModel[]>(() => {
  const rows = queryRows.value
  const baseline = baselineRows()
  const sampleSum = baseline.reduce((sum, row) => sum + row.sampleQty, 0)
  const scrapSum = baseline.reduce((sum, row) => sum + row.scrapQty, 0)
  const defectSum = baseline.reduce((sum, row) => sum + row.defectQty, 0)
  const pMean = sampleSum ? scrapSum / sampleSum : 0
  const uMean = sampleSum ? defectSum / sampleSum : 0
  const nMean = baseline.length ? scrapSum / baseline.length : 0
  const cMean = baseline.length ? defectSum / baseline.length : 0
  const setup = selectedItem.value?.seedLimits
  const keys: { key: ChartKey; title: string; unit: 'count' | 'ratio'; values: number[]; mean: number; lineColor: string }[] = [
    { key: 'pScrap', title: 'P Chart · Scrap 比率', unit: 'ratio', values: rows.map((row) => row.sampleQty ? row.scrapQty / row.sampleQty : 0), mean: pMean, lineColor: '#276c9e' },
    { key: 'npScrap', title: 'np Chart · Scrap 數量', unit: 'count', values: rows.map((row) => row.scrapQty), mean: nMean, lineColor: '#507d43' },
    { key: 'cDefect', title: 'C Chart · Defect 數量', unit: 'count', values: rows.map((row) => row.defectQty), mean: cMean, lineColor: '#946a35' },
    { key: 'uDefect', title: 'U Chart · Defect 比率', unit: 'ratio', values: rows.map((row) => row.sampleQty ? row.defectQty / row.sampleQty : 0), mean: uMean, lineColor: '#81508c' },
  ]
  return keys.map((entry) => {
    const overrides = limitOverrides.value[entry.key] || {}
    const variableLimits = entry.key === 'pScrap'
      ? rows.map((row) => ratioLimits(pMean, row.sampleQty))
      : entry.key === 'uDefect'
        ? rows.map((row) => ({ ucl: uMean + (row.sampleQty ? 3 * Math.sqrt(Math.max(uMean, 0) / row.sampleQty) : 0), lcl: row.sampleQty ? Math.max(uMean - 3 * Math.sqrt(Math.max(uMean, 0) / row.sampleQty), 0) : 0 }))
        : rows.map(() => poissonLimits(entry.mean))
    const avgUcl = variableLimits.length ? variableLimits.reduce((sum, value) => sum + value.ucl, 0) / variableLimits.length : entry.mean
    const avgLcl = variableLimits.length ? variableLimits.reduce((sum, value) => sum + value.lcl, 0) / variableLimits.length : 0
    const limits: LimitSet = { ...(setup?.[entry.key] || {}), ...overrides }
    const uclValues = rows.map((_, index) => overrides.ucl ?? variableLimits[index]?.ucl ?? avgUcl)
    const lclValues = rows.map((_, index) => overrides.lcl ?? variableLimits[index]?.lcl ?? avgLcl)
    return { ...entry, limits: { ...limits, ucl: overrides.ucl ?? avgUcl, cl: overrides.cl ?? entry.mean, lcl: overrides.lcl ?? avgLcl }, uclValues, lclValues }
  })
})
const shownCharts = computed(() => chartModels.value.filter((chart) => reportFilter.value === 'all' || (reportFilter.value === 'scrap' && (chart.key === 'pScrap' || chart.key === 'npScrap')) || (reportFilter.value === 'defect' && (chart.key === 'cDefect' || chart.key === 'uDefect'))))
const selectedLot = computed(() => queryRows.value.find((row) => row.spcSerial === selectedSerial.value) || null)
const summary = computed(() => {
  const rows = queryRows.value
  const baseline = baselineRows()
  const sampleQty = rows.reduce((sum, row) => sum + row.sampleQty, 0)
  const scrapQty = rows.reduce((sum, row) => sum + row.scrapQty, 0)
  const defectQty = rows.reduce((sum, row) => sum + row.defectQty, 0)
  return { lots: rows.length, baselineLots: baseline.length, sampleQty, scrapQty, defectQty, scrapRate: sampleQty ? scrapQty / sampleQty : 0, defectRate: sampleQty ? defectQty / sampleQty : 0, excluded: rows.filter(excluded).length }
})
const visibleDetails = computed(() => selectedLot.value?.details || [])
const changedCount = computed(() => changedSerials.value.length)
function chartYMax(chart: ChartModel): number {
  const max = Math.max(0.001, ...chart.values, ...chart.uclValues, ...(chart.limits.us === undefined ? [] : [chart.limits.us]), ...(chart.limits.ucl === undefined ? [] : [chart.limits.ucl]))
  return max * 1.2
}
function chartX(index: number): number { return queryRows.value.length <= 1 ? 460 : 50 + index * (820 / (queryRows.value.length - 1)) }
function chartY(chart: ChartModel, value: number): number { return 288 - value / chartYMax(chart) * 238 }
function chartPath(chart: ChartModel, values: number[]): string { return values.map((value, index) => `${chartX(index)},${chartY(chart, value)}`).join(' ') }
function pointState(row: Lot, chart: ChartModel, value: number): string {
  if (row.erfNo) return 'erf-point'
  if ((chart.limits.us !== undefined && value > chart.limits.us) || (chart.limits.ls !== undefined && value < chart.limits.ls)) return 'out-point'
  return excluded(row) ? 'excluded-point' : ''
}
function axisValue(row: Lot): string {
  if (axisLabel.value === 'CreateDate') return row.createDate.toLocaleDateString('zh-TW')
  return row.factors[axisLabel.value] || row.lotNo
}
function histogram(chart: ChartModel): { label: string; count: number; height: number }[] {
  if (!chart.values.length) return []
  const maxValue = Math.max(...chart.values, 0.0001)
  const bins = Array.from({ length: 8 }, (_, index) => ({ label: String(index + 1), count: 0, height: 0 }))
  chart.values.forEach((value) => { bins[Math.min(7, Math.floor(value / maxValue * 7))].count += 1 })
  const maxCount = Math.max(1, ...bins.map((bin) => bin.count))
  return bins.map((bin) => ({ ...bin, height: bin.count / maxCount * 100 }))
}
function formatValue(value: number, unit: 'count' | 'ratio'): string { return unit === 'ratio' ? `${(value * 100).toFixed(2)}%` : value.toFixed(2) }
function formatQty(value: number): string { return value.toLocaleString('en-US') }

function markState(isExcluded: boolean): void {
  if (!selectedLot.value) return
  stateOverrides.value = { ...stateOverrides.value, [selectedLot.value.spcSerial]: isExcluded }
  if (!changedSerials.value.includes(selectedLot.value.spcSerial)) changedSerials.value = [...changedSerials.value, selectedLot.value.spcSerial]
  lotActionOpen.value = false
  feedback.value = `${selectedLot.value.spcSerial} 已在本機標記為 ${isExcluded ? 'Excluded' : 'Normal'}；尚未回存。`
}
function saveLotStates(): void {
  if (!changedSerials.value.length) return
  feedback.value = `${changedSerials.value.length} 筆 Lot 狀態已模擬回存；DEV 沒有呼叫 UpdAttLotState。`
  changedSerials.value = []
}
function recalculateLimits(): void {
  for (const key of ['pScrap', 'npScrap', 'cDefect', 'uDefect'] as ChartKey[]) {
    const current = { ...(limitOverrides.value[key] || {}) }
    delete current.ucl; delete current.cl; delete current.lcl
    limitOverrides.value = { ...limitOverrides.value, [key]: current }
  }
  feedback.value = '已依 Normal lots 重算本機 P／np／C／U 控制中心線與界限；未讀寫正式設定。'
}
function openLimitEditor(): void {
  const draft = emptyLimitDraft()
  for (const chart of chartModels.value) {
    for (const field of ['us', 'ls', 'ucl', 'cl', 'lcl'] as LimitField[]) {
      const value = chart.limits[field]
      draft[chart.key][field] = value === undefined ? '' : String(value)
    }
  }
  limitDraft.value = draft
  limitDialogOpen.value = true
}
function applyLimits(save: boolean): void {
  const next: Partial<Record<ChartKey, LimitSet>> = {}
  for (const key of ['pScrap', 'npScrap', 'cDefect', 'uDefect'] as ChartKey[]) {
    const row: LimitSet = {}
    for (const field of ['us', 'ls', 'ucl', 'cl', 'lcl'] as LimitField[]) {
      const raw = limitDraft.value[key][field].trim()
      if (!raw) continue
      const parsed = Number(raw)
      if (Number.isFinite(parsed)) row[field] = parsed
    }
    next[key] = row
  }
  limitOverrides.value = next
  if (save && selectedItem.value) {
    savedLimitByItem.value = { ...savedLimitByItem.value, [selectedItem.value.qcItemNo]: structuredClone(next) }
    feedback.value = '已將目前界線保存在本機 DEV 狀態；沒有呼叫 UpdSPCAttChartLimit。'
  } else feedback.value = '已套用本機界線設定；尚未回存。'
  limitDialogOpen.value = false
}
function simulateFileAction(action: string): void { feedback.value = `${action} 僅為 DEV 示意，沒有建立檔案。` }
</script>

<template>
  <main class="att-chart-page" :data-development-preview="props.developmentPreview">
    <header class="page-head"><div><p class="eyebrow">SPC / SC212 · ATTRIBUTE CONTROL CHART</p><h1>計數型管制圖</h1><p class="subtitle">依批次的 Scrap／Defect 數量與樣本數，查看 P、np、C、U 管制圖及原因明細。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>

    <section class="panel criteria">
      <div class="query-row"><label class="item-field">Attribute QC Item<span class="item-display"><span>{{ selectedItem?.qcItemNo || '尚未選擇' }}</span><button class="button" type="button" @click="pickerOpen = true">...</button></span></label>
        <label class="date-field"><span><input v-model="useFromDate" type="checkbox"> 起始日期</span><input v-model="fromDate" type="date" :disabled="!useFromDate"></label>
        <label class="date-field"><span><input v-model="useEndDate" type="checkbox"> 結束日期</span><input v-model="endDate" type="date" :disabled="!useEndDate"></label><button class="button primary" type="button" @click="runQuery">Query</button>
      </div>
      <div class="item-name">{{ selectedItem?.qcItemName || '按 ... 選取計數型 Attribute QC Item' }}</div>
      <div class="option-row"><fieldset><legend>Lot Report State</legend><label><input v-model="normalState" type="checkbox"> Normal</label><label><input v-model="excludedState" type="checkbox"> Excluded</label></fieldset><fieldset><legend>Result View</legend><label><input v-model="reportFilter" type="radio" value="all"> All</label><label><input v-model="reportFilter" type="radio" value="scrap"> Scrap</label><label><input v-model="reportFilter" type="radio" value="defect"> Defect</label></fieldset><label class="option-check"><input v-model="showBarChart" type="checkbox"> ShowBarChart</label><label class="option-check"><input v-model="showInOnePage" type="checkbox"> ShowInOnePage</label></div>
      <div class="factor-head"><h2>Collect Factor</h2><span>{{ factorCaption }}</span></div>
      <div class="table-wrap"><table class="factor-table"><thead><tr><th>Factor No</th><th>Factor Name</th><th>Online / Spec</th><th>Parameter Type</th><th>Value</th></tr></thead><tbody>
        <tr v-for="factor in selectedItem?.factors || []" :key="factor.factorNo"><td class="code">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.factorOnline ? 'Online' : 'Collect' }} / {{ factor.factorSpec ? 'Spec' : '—' }}</td><td>{{ factor.parameterType === 2 ? 'Free text' : factor.parameterType === 1 ? 'Function lookup' : 'Parameter lookup' }}</td><td>
          <input v-if="factor.parameterType === 2" v-model="factorText[factor.factorNo]" class="factor-input" placeholder="逗號分隔多值；空白代表全部" @input="upperCaseFactor(factor.factorNo)">
          <div v-else class="factor-options"><label v-for="option in factor.options" :key="option"><input type="checkbox" :checked="(selectedFactors[factor.factorNo] || []).includes(option)" @change="toggleFactor(factor.factorNo, option)">{{ option }}</label><span v-if="!(selectedFactors[factor.factorNo]?.length)" class="muted">未選擇 = 全部</span></div>
        </td></tr><tr v-if="!selectedItem"><td colspan="5" class="empty">選取 QC Item 後載入 FactorNo／FactorName／ParameterType。</td></tr>
      </tbody></table></div>
      <p class="helper">日期起訖各自可選擇是否啟用；來源預設兩者都不限制。ParameterType 0／1 多選 lookup，2 自行輸入並轉大寫。</p>
    </section>

    <section class="panel notice-panel"><p role="status">{{ notice }}</p><p v-if="feedback" class="feedback" role="status">{{ feedback }}</p></section>

    <section v-if="queried" class="panel result-panel">
      <header class="result-head"><div><p class="eyebrow">QUERY RESULT · {{ reportFilter.toUpperCase() }}</p><h2>{{ selectedItem?.qcItemNo }} · Attribute Chart</h2><p>{{ useFromDate ? fromDate : '不限起日' }} ~ {{ useEndDate ? endDate : '不限迄日' }} · {{ normalState && excludedState ? 'Normal + Excluded' : normalState ? 'Normal' : 'Excluded' }} · {{ factorCaption }}</p></div>
        <div class="toolbar"><label class="axis-select">X Axis<select v-model="axisLabel"><option value="LotNo">LotNo</option><option value="CreateDate">CreateDate</option><option v-for="factor in selectedItem?.factors || []" :key="factor.factorNo" :value="factor.factorNo">{{ factor.factorNo }}</option></select></label><button class="button" type="button" @click="recalculateLimits" :disabled="!queryRows.length">管制界線重算</button><button class="button" type="button" @click="openLimitEditor" :disabled="!queryRows.length">設定界線</button><button class="button" type="button" @click="saveLotStates" :disabled="!changedCount">回存 Lot 狀態 ({{ changedCount }})</button><button class="button" type="button" @click="simulateFileAction('Save Graph')">Save Graph</button><button class="button" type="button" @click="simulateFileAction('Export')">Export</button><button class="button" type="button" @click="simulateFileAction('Print')">Print</button></div>
      </header>
      <div class="stats-grid"><div><small>QUERY LOTS</small><b>{{ formatQty(summary.lots) }}</b></div><div><small>NORMAL BASELINE LOTS</small><b>{{ formatQty(summary.baselineLots) }}</b></div><div><small>EXCLUDED IN VIEW</small><b>{{ formatQty(summary.excluded) }}</b></div><div><small>SAMPLE QTY</small><b>{{ formatQty(summary.sampleQty) }}</b></div><div><small>SCRAP QTY / RATE</small><b>{{ formatQty(summary.scrapQty) }} / {{ (summary.scrapRate * 100).toFixed(2) }}%</b></div><div><small>DEFECT QTY / RATE</small><b>{{ formatQty(summary.defectQty) }} / {{ (summary.defectRate * 100).toFixed(2) }}%</b></div></div>
      <p v-if="!summary.baselineLots" class="warning">目前結果沒有 Normal lots；來源的中心線與變動界限以 Normal 批次計算，因此此 DEV 頁顯示安全的空基準。</p>
      <div v-if="shownCharts.length" class="chart-grid" :class="{ onepage: showInOnePage }">
        <article v-for="chart in shownCharts" :key="chart.key" class="chart-card"><header><div><h3>{{ chart.title }}</h3><span>點位依 SPCSerial 由舊到新 · Normal lots 計算基準</span></div><span class="chart-count">{{ chart.values.length }} points</span></header>
          <div v-if="chart.values.length" class="chart-scroll"><svg viewBox="0 0 900 340" role="img" :aria-label="chart.title">
            <g v-for="tick in 5" :key="`tick-${chart.key}-${tick}`"><line x1="50" x2="870" :y1="chartY(chart, chartYMax(chart) * (tick - 1) / 4)" :y2="chartY(chart, chartYMax(chart) * (tick - 1) / 4)" class="grid-line"/><text x="43" :y="chartY(chart, chartYMax(chart) * (tick - 1) / 4) + 4" text-anchor="end" class="axis-label">{{ formatValue(chartYMax(chart) * (tick - 1) / 4, chart.unit) }}</text></g>
            <line x1="50" y1="288" x2="870" y2="288" class="axis-line"/><line x1="50" y1="50" x2="50" y2="288" class="axis-line"/>
            <polyline :points="chartPath(chart, chart.uclValues)" class="control-limit"/><polyline :points="chartPath(chart, chart.lclValues)" class="control-limit"/><polyline :points="chartPath(chart, queryRows.map(() => chart.limits.cl ?? chart.mean))" class="center-line"/>
            <line v-if="chart.limits.us !== undefined" x1="50" x2="870" :y1="chartY(chart, chart.limits.us)" :y2="chartY(chart, chart.limits.us)" class="spec-line"/><line v-if="chart.limits.ls !== undefined" x1="50" x2="870" :y1="chartY(chart, chart.limits.ls)" :y2="chartY(chart, chart.limits.ls)" class="spec-line"/>
            <polyline :points="chartPath(chart, chart.values)" fill="none" :stroke="chart.lineColor" stroke-width="2"/>
            <g v-for="(value, index) in chart.values" :key="`${chart.key}-${queryRows[index]?.spcSerial}`" class="chart-point" @click="selectedSerial = queryRows[index]?.spcSerial || ''"><title>{{ queryRows[index]?.spcSerial }} · {{ axisValue(queryRows[index]) }} · {{ formatValue(value, chart.unit) }} · {{ excluded(queryRows[index]) ? 'Excluded' : 'Normal' }}{{ queryRows[index]?.erfNo ? ` · ERF ${queryRows[index]?.erfNo}` : '' }}</title><circle :cx="chartX(index)" :cy="chartY(chart, value)" :r="selectedSerial === queryRows[index]?.spcSerial ? 5.5 : 3.4" :class="[pointState(queryRows[index], chart, value), { selected: selectedSerial === queryRows[index]?.spcSerial }]" :fill="queryRows[index]?.erfNo ? '#734b91' : pointState(queryRows[index], chart, value) === 'out-point' ? '#c9413f' : excluded(queryRows[index]) ? '#a5aea7' : chart.lineColor"/><text v-if="index === 0 || index === chart.values.length - 1 || index % Math.max(1, Math.ceil(chart.values.length / 9)) === 0" :x="chartX(index)" y="310" text-anchor="middle" class="axis-label">{{ axisValue(queryRows[index]) }}</text></g>
          </svg></div><div v-else class="empty chart-empty">沒有可繪製的批次。</div>
          <div class="legend"><span><i :style="{ background: chart.lineColor }"></i>Value</span><span><i class="control-key"></i>UCL / CL / LCL</span><span><i class="spec-key"></i>US / LS</span><span><i class="out-key"></i>Out of spec</span><span><i class="erf-key"></i>ERF</span><span><i class="excluded-key"></i>Excluded</span></div>
          <div class="limit-summary"><span>US <b>{{ chart.limits.us === undefined ? '—' : formatValue(chart.limits.us, chart.unit) }}</b></span><span>UCL <b>{{ formatValue(chart.limits.ucl ?? (chart.uclValues.reduce((sum, value) => sum + value, 0) / Math.max(chart.uclValues.length, 1)), chart.unit) }}{{ chart.key === 'pScrap' || chart.key === 'uDefect' ? ' avg*' : '' }}</b></span><span>CL <b>{{ formatValue(chart.limits.cl ?? chart.mean, chart.unit) }}</b></span><span>LCL <b>{{ formatValue(chart.limits.lcl ?? (chart.lclValues.reduce((sum, value) => sum + value, 0) / Math.max(chart.lclValues.length, 1)), chart.unit) }}{{ chart.key === 'pScrap' || chart.key === 'uDefect' ? ' avg*' : '' }}</b></span><span>LS <b>{{ chart.limits.ls === undefined ? '—' : formatValue(chart.limits.ls, chart.unit) }}</b></span></div>
          <div v-if="showBarChart && chart.values.length" class="histogram" aria-label="Histogram"><span v-for="bin in histogram(chart)" :key="`${chart.key}-${bin.label}`" class="hist-bin"><i :style="{ height: `${Math.max(bin.height, 3)}%` }"></i><small>{{ bin.count }}</small></span></div>
        </article>
      </div>
      <p v-else class="empty">目前篩選沒有可繪製的圖表。</p>
      <div class="detail-layout"><section class="detail-card"><header><h3>Selected Lot</h3><button class="button" type="button" @click="lotActionOpen = !lotActionOpen" :disabled="!selectedLot">Lot 狀態 ▾</button></header><div v-if="lotActionOpen" class="lot-menu"><button type="button" @click="markState(false)">設為 Normal</button><button type="button" @click="markState(true)">設為 Excluded</button></div><dl v-if="selectedLot"><dt>SPC Serial</dt><dd class="code">{{ selectedLot.spcSerial }}</dd><dt>Lot No</dt><dd>{{ selectedLot.lotNo }}</dd><dt>Create Date</dt><dd>{{ selectedLot.createDate.toLocaleString('zh-TW', { hour12: false }) }}</dd><dt>State / Result</dt><dd>{{ excluded(selectedLot) ? 'Excluded' : 'Normal' }} / {{ selectedLot.result === 0 ? 'Accept' : 'Reject' }}</dd><dt>Sample / Scrap / Defect</dt><dd>{{ selectedLot.sampleQty }} / {{ selectedLot.scrapQty }} / {{ selectedLot.defectQty }}</dd><dt>ERF No</dt><dd>{{ selectedLot.erfNo || '—' }}</dd><template v-for="factor in selectedItem?.factors || []" :key="factor.factorNo"><dt>{{ factor.factorNo }}</dt><dd>{{ selectedLot.factors[factor.factorNo] || '—' }}</dd></template></dl><p v-else class="empty">點選任一圖表點位以檢視 Lot 資訊。</p></section>
        <section class="detail-card"><header><h3>Scrap / Defect Reason</h3><span>{{ visibleDetails.length }} rows · ErrorQty &gt; 0</span></header><div class="table-scroll"><table><thead><tr><th>Type</th><th>Reason No</th><th>Error Name</th><th>Error Qty</th></tr></thead><tbody><tr v-for="detail in visibleDetails" :key="`${detail.errorNo}-${detail.errorType}`"><td>{{ detail.errorType === 0 ? 'Scrap' : 'Defect' }}</td><td class="code">{{ detail.errorNo }}</td><td>{{ detail.errorName }}</td><td class="number">{{ formatQty(detail.errorQty) }}</td></tr><tr v-if="!selectedLot"><td colspan="4" class="empty">選擇圖表點位後顯示原因明細。</td></tr><tr v-else-if="!visibleDetails.length"><td colspan="4" class="empty">此 Lot 沒有 ErrorQty &gt; 0 的明細。</td></tr></tbody></table></div></section></div>
      <section class="detail-card lot-table-card"><header><h3>Lot Data</h3><span>{{ queryRows.length }} rows · click a row to select</span></header><div class="table-scroll"><table><thead><tr><th>SPCSerial</th><th>LotNo</th><th>CreateDate</th><th>State</th><th>Result</th><th>SampleQty</th><th>ScrapQty</th><th>DefectQty</th><th>ERFNo</th></tr></thead><tbody><tr v-for="row in queryRows" :key="row.spcSerial" :class="{ active: row.spcSerial === selectedSerial }" @click="selectedSerial = row.spcSerial"><td class="code">{{ row.spcSerial }}</td><td>{{ row.lotNo }}</td><td>{{ row.createDate.toLocaleString('zh-TW', { hour12: false }) }}</td><td>{{ excluded(row) ? 'Excluded' : 'Normal' }}</td><td>{{ row.result === 0 ? 'Accept' : 'Reject' }}</td><td class="number">{{ formatQty(row.sampleQty) }}</td><td class="number">{{ formatQty(row.scrapQty) }}</td><td class="number">{{ formatQty(row.defectQty) }}</td><td>{{ row.erfNo || '—' }}</td></tr><tr v-if="!queryRows.length"><td colspan="9" class="empty">沒有符合條件的計數批次。</td></tr></tbody></table></div></section>
      <p class="disclaimer">Core 的資料點與查詢列依 SPCSerial 由新到舊取得後，畫面按 SPCSerial 由舊到新繪圖。Normal lots 計算基準；Excluded 仍可顯示。自動限界、直方圖、資料、Factor、限值與 Error reason 均為 DEV 假資料或簡化示意。狀態／界線回存及 Save Graph／Export／Print 不呼叫正式服務。</p>
    </section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 Attribute QC Item"><header><div><p class="eyebrow">ATTRIBUTE QC ITEM LOOKUP · CHARTCONTROL=1</p><h2>選擇 QC Item</h2></div><button class="button" type="button" @click="pickerOpen = false">Close</button></header><label class="search">搜尋 QC Item<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" type="button" @click="chooseItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item。</p></div></section></div>
    <div v-if="limitDialogOpen" class="modal-backdrop" @click.self="limitDialogOpen = false"><section class="dialog limit-dialog" role="dialog" aria-modal="true" aria-label="設定控制界線"><header><div><p class="eyebrow">LOCAL DEV LIMIT CONFIGURATION</p><h2>設定界線</h2></div><button class="button" type="button" @click="limitDialogOpen = false">Close</button></header><p class="limit-help">空白代表使用本機計算的界線；P／U 圖仍依各批 SampleQty 計算變動 UCL／LCL，平均值顯示於下方。</p><div class="table-scroll"><table><thead><tr><th>Chart</th><th>US</th><th>LS</th><th>UCL</th><th>CL</th><th>LCL</th></tr></thead><tbody><tr v-for="chart in chartModels" :key="chart.key"><td>{{ chart.title }}</td><td v-for="field in limitFields" :key="`${chart.key}-${field}`"><input v-model="limitDraft[chart.key][field]" type="number" step="any" :aria-label="`${chart.title} ${field}`"></td></tr></tbody></table></div><footer><button class="button" type="button" @click="applyLimits(false)">Apply</button><button class="button primary" type="button" @click="applyLimits(true)">Save Limit</button></footer></section></div>
  </main>
</template>

<style scoped>
.att-chart-page { --ink:#252c27; --muted:#6d766f; --line:#d4dcd6; --paper:#fff; --canvas:#f2f5f2; --green:#287552; height:100%; min-height:0; overflow:auto; padding:18px; box-sizing:border-box; background:var(--canvas); color:var(--ink); font:inherit; }.page-head,.query-row,.option-row,.factor-head,.result-head,.chart-card>header,.detail-card>header { display:flex; align-items:center; justify-content:space-between; gap:12px; }.page-head { margin-bottom:14px; }.page-head h1,.result-head h2 { margin:2px 0; font-size:23px; }.page-head p,.result-head p { margin:4px 0 0; color:var(--muted); }.eyebrow { margin:0; color:var(--green); font-size:11px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:13px; }.panel { margin-bottom:12px; border:1px solid var(--line); border-radius:6px; background:var(--paper); box-shadow:0 1px 3px #28342b0b; }.criteria,.result-panel { padding:14px; }.query-row { align-items:end; flex-wrap:wrap; }.query-row label,.search,.axis-select { display:grid; gap:5px; color:var(--muted); font-size:12px; font-weight:700; }.item-field { flex:1; min-width:260px; }.item-display { display:flex; gap:5px; }.item-display>span { display:block; flex:1; overflow:hidden; padding:8px 9px; border:1px solid #c8d0ca; border-radius:4px; background:#fafcfa; color:var(--ink); white-space:nowrap; text-overflow:ellipsis; }.date-field { min-width:166px; }.date-field>span { display:flex; align-items:center; gap:5px; }.query-row input,.factor-input,.search input,.axis-select select,.limit-dialog input { min-height:33px; border:1px solid #c8d0ca; border-radius:4px; padding:5px 8px; box-sizing:border-box; background:white; color:var(--ink); font:inherit; }.query-row input:disabled { background:#f0f2f0; color:#89928b; }.item-name { margin:7px 0 4px; color:var(--muted); font-size:12px; }.option-row { justify-content:flex-start; flex-wrap:wrap; margin:10px 0; }.option-row fieldset { display:flex; gap:12px; align-items:center; min-height:42px; margin:0; padding:3px 10px 7px; border:1px solid var(--line); border-radius:4px; }.option-row legend { padding:0 5px; color:var(--muted); font-size:11px; font-weight:700; }.option-row label,.factor-options label { display:inline-flex; align-items:center; gap:4px; color:#39453d; font-size:12px; }.option-check { display:flex!important; align-items:center; gap:5px!important; padding:10px 4px; }.factor-head { margin:12px 0 7px; }.factor-head h2 { margin:0; font-size:14px; }.factor-head span,.helper,.muted { color:var(--muted); font-size:11px; }.table-wrap,.table-scroll { max-width:100%; overflow:auto; border:1px solid var(--line); border-radius:4px; }table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }th,td { border-bottom:1px solid #e8ece8; padding:7px 9px; text-align:left; }th { position:sticky; top:0; z-index:1; background:#f2f6f2; color:#59645c; font-size:10px; }tbody tr:last-child td { border-bottom:0; }.factor-table { font-size:12px; }.factor-options { display:flex; flex-wrap:wrap; gap:10px; }.factor-input { min-width:245px; min-height:29px!important; }.helper { margin:7px 0 0; }.notice-panel { padding:9px 14px; color:#536158; font-size:12px; }.notice-panel p { margin:0; }.notice-panel .feedback { margin-top:5px; color:#287552; }.result-head { align-items:flex-start; margin-bottom:12px; }.result-head h2 { font-size:18px; }.result-head p:last-child { font-size:11px; }.toolbar { display:flex; flex-wrap:wrap; align-items:end; justify-content:flex-end; gap:6px; }.axis-select { min-width:110px; }.stats-grid { display:grid; grid-template-columns:repeat(6,minmax(100px,1fr)); gap:8px; margin-bottom:12px; }.stats-grid>div { min-width:0; display:grid; gap:4px; padding:8px 10px; border:1px solid var(--line); border-radius:4px; background:#fbfcfb; }.stats-grid small { color:var(--muted); font-size:9px; font-weight:800; letter-spacing:.04em; }.stats-grid b { overflow:hidden; text-overflow:ellipsis; font-size:13px; white-space:nowrap; }.warning { margin:4px 0 10px; color:#9b5a25; font-size:11px; }.chart-grid { display:grid; grid-template-columns:1fr; gap:10px; }.chart-grid.onepage { grid-template-columns:repeat(2,minmax(0,1fr)); }.chart-card { min-width:0; padding:10px; border:1px solid var(--line); border-radius:5px; background:#fff; }.chart-card>header { align-items:flex-start; margin-bottom:5px; }.chart-card h3 { margin:0; font-size:14px; }.chart-card>header span { display:block; margin-top:3px; color:var(--muted); font-size:10px; }.chart-card .chart-count { white-space:nowrap; }.chart-scroll { height:285px; min-width:550px; overflow:auto hidden; }.chart-scroll svg { width:100%; min-width:550px; height:100%; }.grid-line { stroke:#e7ece8; stroke-width:1; }.axis-line { stroke:#829087; stroke-width:1.2; }.axis-label { fill:#69746c; font-size:9px; }.chart-point { cursor:pointer; }.chart-point circle { stroke:white; stroke-width:1.2; }.chart-point circle.selected { stroke:#222; stroke-width:2.2; }.chart-point circle.out-point { fill:#c9413f; }.chart-point circle.erf-point { fill:#734b91; }.chart-point circle.excluded-point { opacity:.6; }.control-limit { fill:none; stroke:#4b9a72; stroke-width:1.3; stroke-dasharray:5 3; }.center-line { fill:none; stroke:#287552; stroke-width:1.3; }.spec-line { stroke:#c05a55; stroke-width:1.4; stroke-dasharray:2 4; }.legend { display:flex; flex-wrap:wrap; gap:10px; margin:2px 0 5px; color:var(--muted); font-size:9px; }.legend span { display:inline-flex; align-items:center; gap:4px; }.legend i { width:9px; height:9px; border-radius:50%; background:#276c9e; }.legend .control-key,.legend .spec-key { width:16px; height:0; border-top:2px dashed #4b9a72; border-radius:0; background:transparent; }.legend .spec-key { border-color:#c05a55; }.legend .out-key,.legend .erf-key,.legend .excluded-key { width:9px; height:9px; background:#c9413f; }.legend .erf-key { background:#734b91; }.legend .excluded-key { background:#a5aea7; }.limit-summary { display:flex; flex-wrap:wrap; gap:6px 14px; padding:6px 7px; border:1px solid #e6ebe7; border-radius:4px; background:#f8faf8; color:var(--muted); font-size:9px; }.limit-summary b { margin-left:3px; color:var(--ink); }.histogram { height:58px; display:flex; align-items:end; gap:5px; margin-top:6px; padding:4px 6px 0; border-bottom:1px solid #b9c4bc; background:repeating-linear-gradient(to top,#fff 0,#fff 18px,#eef1ee 19px,#fff 20px); }.hist-bin { height:100%; flex:1; display:flex; flex-direction:column; justify-content:end; align-items:center; gap:1px; }.hist-bin i { width:68%; min-height:2px; background:#91ad99; border-radius:2px 2px 0 0; }.hist-bin small { color:var(--muted); font-size:8px; }.detail-layout { display:grid; grid-template-columns:minmax(260px,.8fr) minmax(0,1.2fr); gap:10px; margin-top:11px; }.detail-card { position:relative; min-width:0; overflow:hidden; border:1px solid var(--line); border-radius:5px; background:#fff; }.detail-card>header { min-height:34px; padding:7px 10px; border-bottom:1px solid var(--line); background:#f8faf8; }.detail-card h3 { margin:0; font-size:12px; }.detail-card>header span { color:var(--muted); font-size:10px; }.detail-card dl { display:grid; grid-template-columns:130px 1fr; gap:6px 9px; margin:0; padding:10px 11px; font-size:10px; }.detail-card dt { color:var(--muted); }.detail-card dd { margin:0; font-weight:650; }.lot-menu { position:absolute; right:7px; top:39px; z-index:2; display:grid; padding:4px; border:1px solid var(--line); border-radius:4px; background:#fff; box-shadow:0 6px 16px #0002; }.lot-menu button { padding:7px 10px; border:0; background:white; color:var(--ink); text-align:left; cursor:pointer; }.lot-menu button:hover { background:#eaf3ec; }.lot-table-card { margin-top:10px; }.table-scroll { max-height:280px; border:0; border-radius:0; }.table-scroll tbody tr { cursor:pointer; }.table-scroll tbody tr:hover,.table-scroll tbody tr.active { background:#eaf3ec; }.code { color:#246a4e; font-family:Consolas,monospace; }.number { text-align:right!important; font-variant-numeric:tabular-nums; }.empty { padding:12px; color:#89928b; text-align:center; }.chart-empty { min-height:120px; display:grid; place-items:center; }.disclaimer { margin:9px 0 0; color:var(--muted); font-size:10px; }.button { min-height:31px; padding:5px 10px; border:1px solid #bfc9c1; border-radius:4px; background:#fff; color:#313d34; font:inherit; font-size:11px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:#fff; }.button:disabled { cursor:not-allowed; opacity:.5; }.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:20px; background:#1d2822a8; }.dialog { width:min(760px,96vw); max-height:85vh; overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.dialog>header { display:flex; justify-content:space-between; align-items:center; padding:13px 15px; border-bottom:1px solid var(--line); }.dialog h2 { margin:2px 0 0; font-size:18px; }.search { padding:11px 15px; }.search input { width:100%; }.picker-list { max-height:58vh; overflow:auto; padding:5px 10px 12px; }.picker-row { width:100%; display:grid; grid-template-columns:155px 1fr 85px; gap:10px; padding:10px; border:0; border-bottom:1px solid #edf0ed; background:white; color:var(--ink); text-align:left; cursor:pointer; }.picker-row:hover { background:#e8f3ec; }.picker-row b { color:var(--green); font-family:Consolas,monospace; }.limit-dialog { padding-bottom:12px; }.limit-help { margin:11px 15px; color:var(--muted); font-size:11px; }.limit-dialog table { min-width:680px; }.limit-dialog input { width:95px; min-height:28px; }.limit-dialog footer { display:flex; justify-content:flex-end; gap:7px; padding:11px 15px 0; }
@media (max-width:1000px) { .stats-grid { grid-template-columns:repeat(3,minmax(90px,1fr)); }.chart-grid.onepage,.detail-layout { grid-template-columns:1fr; }.query-row { align-items:stretch; }.item-field { min-width:200px; }.result-head { flex-direction:column; }.toolbar { justify-content:flex-start; } }
@media (max-width:650px) { .att-chart-page { padding:10px; }.page-head { align-items:flex-start; flex-direction:column; }.stats-grid { grid-template-columns:repeat(2,minmax(100px,1fr)); }.option-row fieldset { flex-wrap:wrap; }.chart-scroll { min-width:600px; }.picker-row { grid-template-columns:1fr; } }
</style>

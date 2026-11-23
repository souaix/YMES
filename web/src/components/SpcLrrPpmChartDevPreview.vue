<script setup lang="ts">
import { computed, ref } from 'vue'

type ReportType = 'LRR' | 'PPM'
type PeriodType = 'week' | 'month' | 'year'
type FactorDefinition = { factorNo: string; factorName: string; parameterType: 0 | 1 | 2; options: string[] }
type QCItem = { qcItemNo: string; qcItemName: string; issueState: 2 | -1; factors: FactorDefinition[] }
type LotRow = { createDate: Date; sampleQty: number; scrapQty: number; defectQty: number; result: number; excluded: boolean; factors: Record<string, string> }
type PeriodRow = { period: string; date: string; start: Date; end: Date; sampleQty: number; scrapQty: number; defectQty: number; lotQty: number; rejectLotQty: number }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const now = new Date()
const defaultFrom = new Date(now.getFullYear(), now.getMonth() - 2, now.getDate() + 1)
const fromDate = ref(toDateInput(defaultFrom))
const endDate = ref(toDateInput(now))
const reportType = ref<ReportType>('PPM')
const periodType = ref<PeriodType>('month')
const normalState = ref(true)
const excludedState = ref(false)
const itemPickerOpen = ref(false)
const itemSearch = ref('')
const selectedItem = ref<QCItem | null>(null)
const selectedFactors = ref<Record<string, string[]>>({})
const factorText = ref<Record<string, string>>({})
const queried = ref(false)
const targetDialogOpen = ref(false)
const targetValue = ref('')
const targetFrom = ref(0)
const targetTo = ref(0)
const targetLimits = ref<Record<number, number>>({})
const selectedPeriodIndex = ref(0)
const notice = ref('請選擇 Attribute QC Item、日期及期間，再執行 Query。')
const feedback = ref('')

const items: QCItem[] = [
  { qcItemNo: 'QC-A-2201', qcItemName: 'Lead Frame Surface Defect', issueState: 2, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'MACHINE', factorName: '沖壓機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [] },
  ] },
  { qcItemNo: 'QC-A-2237', qcItemName: 'Wire Bond Appearance', issueState: 2, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'BONDER', factorName: '焊線機', parameterType: 1, options: ['WB-12', 'WB-15', 'WB-20'] },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, options: [] },
  ] },
  { qcItemNo: 'QC-A-2290', qcItemName: 'Mold Compound Contamination', issueState: -1, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 1, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
  ] },
]

function toDateInput(value: Date): string {
  const year = value.getFullYear()
  const month = String(value.getMonth() + 1).padStart(2, '0')
  const day = String(value.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
function chooseItem(item: QCItem): void {
  selectedItem.value = item
  selectedFactors.value = {}
  factorText.value = {}
  queried.value = false
  targetLimits.value = {}
  itemPickerOpen.value = false
  notice.value = `${item.qcItemNo} 已載入 QC Item 與 Factor metadata；DEV 使用本機假資料。`
}
function toggleFactor(factorNo: string, value: string): void {
  const values = selectedFactors.value[factorNo] || []
  selectedFactors.value = { ...selectedFactors.value, [factorNo]: values.includes(value) ? values.filter((entry) => entry !== value) : [...values, value] }
}
function uppercaseFactor(factorNo: string): void {
  factorText.value[factorNo] = (factorText.value[factorNo] || '').toUpperCase()
}
function readFactorValues(factor: FactorDefinition): string[] {
  if (factor.parameterType === 2) return (factorText.value[factor.factorNo] || '').split(',').map((entry) => entry.trim()).filter(Boolean)
  return selectedFactors.value[factor.factorNo] || []
}
const filteredItems = computed(() => items.filter((item) => `${item.qcItemNo} ${item.qcItemName}`.toLowerCase().includes(itemSearch.value.trim().toLowerCase())))
const factorSummary = computed(() => {
  const values = selectedItem.value?.factors.flatMap((factor) => {
    const chosen = readFactorValues(factor)
    return chosen.length ? [`${factor.factorName}: ${chosen.join(', ')}`] : []
  }) || []
  return values.length ? values.join(' · ') : '全部 Factor'
})

function generatedLots(item: QCItem): LotRow[] {
  const rows: LotRow[] = []
  const firstDate = new Date(now.getFullYear(), now.getMonth() - 11, 1)
  for (let index = 0; index < 220; index += 1) {
    const createDate = new Date(firstDate.getFullYear(), firstDate.getMonth(), firstDate.getDate() + index * 1.7, 10 + (index % 9), (index * 13) % 60)
    const sampleQty = 780 + (index * 173) % 1220
    const exceptional = index % 41 === 9
    const scrapQty = exceptional ? Math.ceil(sampleQty * 0.045) : (index * 17 + Math.floor(index / 3)) % 11
    const defectQty = Math.min(scrapQty, (index * 7 + Math.floor(index / 2)) % (scrapQty + 1))
    const factors: Record<string, string> = {
      LINE: ['LN-A', 'LN-B', 'LN-C'][index % 3],
      MACHINE: ['PRESS-03', 'PRESS-05', 'PRESS-08'][Math.floor(index / 2) % 3],
      BONDER: ['WB-12', 'WB-15', 'WB-20'][index % 3],
      SHIFT: ['A', 'B', 'C'][Math.floor(index / 3) % 3],
    }
    rows.push({ createDate, sampleQty, scrapQty, defectQty, result: exceptional || index % 8 === 3 ? 1 : 0, excluded: index % 17 === 4, factors })
  }
  return item.qcItemNo.endsWith('2237') ? rows.map((row, index) => ({ ...row, defectQty: Math.max(0, row.scrapQty - (index % 4)) })) : rows
}
function weekStart(value: Date): Date {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate() - value.getDay())
}
function periodStart(value: Date): Date {
  if (periodType.value === 'week') return weekStart(value)
  if (periodType.value === 'year') return new Date(value.getFullYear(), 0, 1)
  return new Date(value.getFullYear(), value.getMonth(), 1)
}
function periodEnd(start: Date): Date {
  if (periodType.value === 'week') return new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6, 23, 59, 59, 999)
  if (periodType.value === 'year') return new Date(start.getFullYear(), 11, 31, 23, 59, 59, 999)
  return new Date(start.getFullYear(), start.getMonth() + 1, 0, 23, 59, 59, 999)
}
function periodLabel(start: Date): string {
  if (periodType.value === 'year') return `Y${start.getFullYear()}`
  if (periodType.value === 'month') return `M${start.getMonth() + 1}`
  const firstDay = new Date(start.getFullYear(), 0, 1)
  const weekNo = Math.ceil((((start.getTime() - firstDay.getTime()) / 86_400_000) + firstDay.getDay() + 1) / 7)
  return `W${weekNo}`
}
const queriedRows = computed<PeriodRow[]>(() => {
  if (!queried.value || !selectedItem.value || !fromDate.value || !endDate.value) return []
  const from = new Date(`${fromDate.value}T00:00:00`)
  const to = new Date(`${endDate.value}T23:59:59.999`)
  const chosenFactors = selectedItem.value.factors.map((factor) => [factor.factorNo, readFactorValues(factor)] as const).filter(([, values]) => values.length)
  const filtered = generatedLots(selectedItem.value).filter((row) => row.createDate >= from && row.createDate <= to)
    .filter((row) => (normalState.value && excludedState.value) || (normalState.value && !row.excluded) || (excludedState.value && row.excluded))
    .filter((row) => chosenFactors.every(([factorNo, values]) => values.includes(row.factors[factorNo] || '')))
  const groups = new Map<number, PeriodRow>()
  for (const row of filtered) {
    const start = periodStart(row.createDate)
    const key = start.getTime()
    const aggregate = groups.get(key) || { period: periodLabel(start), date: '', start, end: periodEnd(start), sampleQty: 0, scrapQty: 0, defectQty: 0, lotQty: 0, rejectLotQty: 0 }
    aggregate.sampleQty += row.sampleQty
    aggregate.scrapQty += row.scrapQty
    aggregate.defectQty += row.defectQty
    aggregate.lotQty += 1
    aggregate.rejectLotQty += row.result === 0 ? 0 : 1
    groups.set(key, aggregate)
  }
  return [...groups.values()].sort((a, b) => a.start.getTime() - b.start.getTime()).map((row) => ({
    ...row,
    date: `${toDateInput(row.start < from ? from : row.start)} ~ ${toDateInput(row.end > to ? to : row.end)}`,
  }))
})
const displayRows = computed(() => queriedRows.value.map((row) => ({ ...row, lrr: row.lotQty ? row.rejectLotQty / row.lotQty : 0, scrapPpm: row.sampleQty ? row.scrapQty / row.sampleQty * 1_000_000 : 0, defectPpm: row.sampleQty ? row.defectQty / row.sampleQty * 1_000_000 : 0 })))
const selectedPeriod = computed(() => displayRows.value[selectedPeriodIndex.value] || null)
const stats = computed(() => {
  const totals = displayRows.value.reduce((sum, row) => ({ sampleQty: sum.sampleQty + row.sampleQty, scrapQty: sum.scrapQty + row.scrapQty, defectQty: sum.defectQty + row.defectQty, lotQty: sum.lotQty + row.lotQty, rejectLotQty: sum.rejectLotQty + row.rejectLotQty }), { sampleQty: 0, scrapQty: 0, defectQty: 0, lotQty: 0, rejectLotQty: 0 })
  return { ...totals, lrr: totals.lotQty ? totals.rejectLotQty / totals.lotQty : 0, scrapPpm: totals.sampleQty ? totals.scrapQty / totals.sampleQty * 1_000_000 : 0, defectPpm: totals.sampleQty ? totals.defectQty / totals.sampleQty * 1_000_000 : 0 }
})
const series = computed(() => reportType.value === 'LRR'
  ? [{ key: 'lrr', name: 'LRR', color: '#365f9e', values: displayRows.value.map((row) => row.lrr) }]
  : [{ key: 'scrapPpm', name: 'Scrap PPM', color: '#9a507e', values: displayRows.value.map((row) => row.scrapPpm) }, { key: 'defectPpm', name: 'Defect PPM', color: '#365f9e', values: displayRows.value.map((row) => row.defectPpm) }])
const targetMax = computed(() => Math.max(0, ...Object.values(targetLimits.value)))
const yMax = computed(() => Math.max(0.01, ...series.value.flatMap((line) => line.values), targetMax.value) * 1.2)
function chartX(index: number): number { return displayRows.value.length <= 1 ? 450 : 58 + index * (800 / (displayRows.value.length - 1)) }
function chartY(value: number): number { return 310 - (value / yMax.value) * 260 }
function pointClass(value: number, index: number): string { return targetLimits.value[index] !== undefined && value > targetLimits.value[index] ? 'out-of-target' : '' }
const chartTicks = computed(() => Array.from({ length: 5 }, (_, index) => yMax.value * index / 4))
const targetPath = computed(() => displayRows.value.map((row, index) => targetLimits.value[index] === undefined ? '' : `${chartX(index)},${chartY(targetLimits.value[index])}`).filter(Boolean).join(' '))
const filteredItemsCount = computed(() => queriedRows.value.length)

function runQuery(): void {
  if (!selectedItem.value) { notice.value = '請先選擇 QC Item。'; return }
  if (!fromDate.value || !endDate.value || fromDate.value > endDate.value) { notice.value = '請輸入有效的起訖日期，起日不可晚於迄日。'; return }
  if (!normalState.value && !excludedState.value) { notice.value = '請至少選取 Normal 或 Excluded。'; return }
  queried.value = true
  selectedPeriodIndex.value = 0
  targetLimits.value = {}
  if (!queriedRows.value.length) notice.value = '查無符合條件的彙總資料。'
  else notice.value = `查詢完成：${selectedItem.value.qcItemNo}，${queriedRows.value.length} 個期間；${factorSummary.value}。`
  feedback.value = ''
}
function openTargetDialog(): void {
  if (!displayRows.value.length) { feedback.value = '請先執行查詢。'; return }
  targetValue.value = ''
  targetFrom.value = 0
  targetTo.value = displayRows.value.length - 1
  targetDialogOpen.value = true
}
function applyTarget(): void {
  const value = Number(targetValue.value)
  if (!Number.isFinite(value) || value <= 0 || targetFrom.value > targetTo.value) return
  const next: Record<number, number> = { ...targetLimits.value }
  for (let index = targetFrom.value; index <= targetTo.value; index += 1) next[index] = value
  targetLimits.value = next
  targetDialogOpen.value = false
  feedback.value = `已套用目標線 ${formatValue(value)} 至所選期間；高於目標的資料點以紅色標示。`
}
function removeTargets(): void { targetLimits.value = {}; feedback.value = '已清除目標線。' }
function simulate(action: string): void { feedback.value = `${action} 僅為 DEV 示意，沒有建立檔案或呼叫正式服務。` }
function formatQty(value: number): string { return value.toLocaleString('en-US') }
function formatValue(value: number): string { return reportType.value === 'LRR' ? `${(value * 100).toFixed(2)}%` : value.toFixed(1) }
function formatYAxis(value: number): string { return reportType.value === 'LRR' ? `${(value * 100).toFixed(1)}%` : Math.round(value).toLocaleString('en-US') }
</script>

<template>
  <main class="lrr-page" :data-development-preview="props.developmentPreview">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC211 · LRR / PPM CHART</p><h1>拒收率／百萬不良數管制圖</h1><p class="subtitle">依 Attribute QC Item 的檢驗批次彙總拒收率或每百萬樣本的不良數。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <section class="panel criteria" aria-label="查詢條件">
      <div class="query-row">
        <label class="item-field">Attribute QC Item
          <span class="item-display"><span>{{ selectedItem?.qcItemNo || '尚未選擇' }}</span><button class="button" type="button" @click="itemPickerOpen = true">...</button></span>
        </label>
        <label>起始日期<input v-model="fromDate" type="date"></label>
        <label>結束日期<input v-model="endDate" type="date"></label>
        <button class="button primary" type="button" @click="runQuery">Query</button>
      </div>
      <div class="item-name">{{ selectedItem?.qcItemName || '按 ... 選取 IssueState = 2 或 -1 的 QC Item' }}</div>
      <div class="filter-row">
        <fieldset><legend>Report Type</legend><label><input v-model="reportType" type="radio" value="LRR"> LRR</label><label><input v-model="reportType" type="radio" value="PPM"> PPM</label></fieldset>
        <fieldset><legend>Period</legend><label><input v-model="periodType" type="radio" value="week"> WEEK</label><label><input v-model="periodType" type="radio" value="month"> MONTH</label><label><input v-model="periodType" type="radio" value="year"> YEAR</label></fieldset>
        <fieldset><legend>Lot Report State</legend><label><input v-model="normalState" type="checkbox"> Normal</label><label><input v-model="excludedState" type="checkbox"> Excluded</label></fieldset>
      </div>
      <div class="factor-head"><h2>Collect Factor</h2><span>{{ factorSummary }}</span></div>
      <div class="factor-table-wrap"><table class="factor-table"><thead><tr><th>Factor No</th><th>Factor Name</th><th>Parameter Type</th><th>Factor Value</th></tr></thead><tbody>
        <tr v-for="factor in selectedItem?.factors || []" :key="factor.factorNo"><td class="code">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType === 2 ? 'Free text' : factor.parameterType === 1 ? 'Function lookup' : 'Parameter lookup' }}</td><td>
          <input v-if="factor.parameterType === 2" v-model="factorText[factor.factorNo]" class="factor-input" placeholder="逗號分隔多值；空白代表全部" @input="uppercaseFactor(factor.factorNo)">
          <div v-else class="factor-options"><label v-for="option in factor.options" :key="option"><input type="checkbox" :checked="(selectedFactors[factor.factorNo] || []).includes(option)" @change="toggleFactor(factor.factorNo, option)">{{ option }}</label></div>
          <span v-if="factor.parameterType !== 2 && !(selectedFactors[factor.factorNo]?.length)" class="muted">未選擇 = 全部</span>
        </td></tr>
        <tr v-if="!selectedItem"><td colspan="4" class="empty">選取 QC Item 後載入該 Item 的 Factor metadata。</td></tr>
      </tbody></table></div>
      <p class="factor-note">多值條件以逗號清單表示；未填要因代表不限制該 Factor。Normal 與 Excluded 同時勾選會包含兩種狀態。</p>
    </section>

    <section class="panel notice-panel"><p class="notice" role="status">{{ notice }}</p><p v-if="feedback" class="feedback" role="status">{{ feedback }}</p></section>

    <section v-if="queried" class="panel result-panel">
      <header class="result-head"><div><p class="eyebrow">QUERY RESULT</p><h2>{{ selectedItem?.qcItemNo }} · {{ reportType }} Chart</h2><p>{{ fromDate }} ~ {{ endDate }} · {{ periodType.toUpperCase() }} · {{ normalState && excludedState ? 'Normal + Excluded' : normalState ? 'Normal' : 'Excluded' }} · {{ factorSummary }}</p></div>
        <div class="result-actions"><button class="button" type="button" @click="openTargetDialog">Set Limit Line</button><button class="button" type="button" @click="removeTargets" :disabled="!Object.keys(targetLimits).length">Clear Limit</button><button class="button" type="button" @click="simulate('Save Graph')">Save Graph</button><button class="button" type="button" @click="simulate('Export')">Export</button><button class="button" type="button" @click="simulate('Print')">Print</button></div>
      </header>
      <div class="stats-grid"><div><small>PERIODS</small><b>{{ filteredItemsCount }}</b></div><div><small>TOTAL LOTS</small><b>{{ formatQty(stats.lotQty) }}</b></div><div><small>REJECT LOTS</small><b>{{ formatQty(stats.rejectLotQty) }}</b></div><div><small>LRR</small><b>{{ (stats.lrr * 100).toFixed(2) }}%</b></div><div><small>SAMPLE QTY</small><b>{{ formatQty(stats.sampleQty) }}</b></div><div><small>SCRAP / DEFECT PPM</small><b>{{ stats.scrapPpm.toFixed(1) }} / {{ stats.defectPpm.toFixed(1) }}</b></div></div>

      <section class="chart-card"><div class="chart-title"><div><h3>{{ reportType === 'LRR' ? 'Lot Reject Rate' : 'Scrap PPM and Defect PPM' }}</h3><span>{{ reportType === 'LRR' ? 'Rejected Lot Count / Total Lot Count' : '(ScrapQty or DefectQty) / SampleQty × 1,000,000' }}</span></div><span>Click a point to inspect its period</span></div>
        <div v-if="displayRows.length" class="chart-scroll"><svg viewBox="0 0 900 360" role="img" :aria-label="`${reportType} chart`">
          <g v-for="tick in chartTicks" :key="tick"><line x1="58" x2="858" :y1="chartY(tick)" :y2="chartY(tick)" class="grid-line"/><text x="48" :y="chartY(tick) + 4" text-anchor="end" class="axis-label">{{ formatYAxis(tick) }}</text></g>
          <line x1="58" y1="310" x2="858" y2="310" class="axis-line"/><line x1="58" y1="50" x2="58" y2="310" class="axis-line"/>
          <polyline v-if="targetPath" :points="targetPath" class="target-line"/><g v-for="(line, lineIndex) in series" :key="line.key"><polyline :points="line.values.map((value, index) => `${chartX(index)},${chartY(value)}`).join(' ')" fill="none" :stroke="line.color" stroke-width="2.5"/>
            <g v-for="(value, index) in line.values" :key="`${line.key}-${index}`" class="chart-point" @click="selectedPeriodIndex = index"><title>{{ line.name }} · {{ displayRows[index]?.period }} · {{ formatValue(value) }}{{ targetLimits[index] !== undefined ? ` · Limit ${formatValue(targetLimits[index])}` : '' }}</title><circle :cx="chartX(index)" :cy="chartY(value)" :r="selectedPeriodIndex === index ? 6 : 4" :class="[pointClass(value, index), { selected: selectedPeriodIndex === index }]" :fill="pointClass(value, index) ? '#c5413f' : line.color"/><text v-if="lineIndex === 0" :x="chartX(index)" y="332" text-anchor="middle" class="axis-label">{{ displayRows[index]?.period }}</text></g>
          </g>
          <g v-for="(limit, index) in targetLimits" :key="`limit-${index}`"><circle :cx="chartX(Number(index))" :cy="chartY(limit)" r="2.5" class="limit-point"/></g>
        </svg></div>
        <div v-else class="chart-empty">沒有符合日期、Lot Report State 與 Factor 條件的週期資料。</div>
        <div class="legend"><span v-for="line in series" :key="line.key"><i :style="{ background: line.color }"></i>{{ line.name }}</span><span v-if="Object.keys(targetLimits).length"><i class="target-swatch"></i>Manual target</span><span><i class="out-swatch"></i>Over target</span></div>
      </section>

      <div class="detail-grid"><section class="detail-card"><header><h3>Selected Period</h3><span>{{ selectedPeriod?.period || '—' }}</span></header><dl v-if="selectedPeriod"><dt>Date Range</dt><dd>{{ selectedPeriod.date }}</dd><dt>Lot Count</dt><dd>{{ formatQty(selectedPeriod.lotQty) }}</dd><dt>Rejected Lot Count</dt><dd>{{ formatQty(selectedPeriod.rejectLotQty) }}</dd><dt>LRR</dt><dd>{{ (selectedPeriod.lrr * 100).toFixed(2) }}%</dd><dt>Sample / Scrap / Defect</dt><dd>{{ formatQty(selectedPeriod.sampleQty) }} / {{ formatQty(selectedPeriod.scrapQty) }} / {{ formatQty(selectedPeriod.defectQty) }}</dd><dt>Scrap / Defect PPM</dt><dd>{{ selectedPeriod.scrapPpm.toFixed(1) }} / {{ selectedPeriod.defectPpm.toFixed(1) }}</dd><dt>Applied Limit</dt><dd>{{ targetLimits[selectedPeriodIndex] === undefined ? '無' : formatValue(targetLimits[selectedPeriodIndex]) }}</dd></dl><p v-else class="empty">尚無期間可顯示。</p></section>
        <section class="detail-card"><header><h3>Period Summary</h3><span>{{ displayRows.length }} periods</span></header><div class="table-scroll"><table><thead><tr><th>Period</th><th>Date</th><th>Lots</th><th>Rejected</th><th>LRR</th><th>Sample Qty</th><th>Scrap Qty</th><th>Defect Qty</th><th>Scrap PPM</th><th>Defect PPM</th></tr></thead><tbody><tr v-for="(row, index) in displayRows" :key="row.start.getTime()" :class="{ active: index === selectedPeriodIndex }" @click="selectedPeriodIndex = index"><td class="code">{{ row.period }}</td><td>{{ row.date }}</td><td class="number">{{ formatQty(row.lotQty) }}</td><td class="number">{{ formatQty(row.rejectLotQty) }}</td><td class="number">{{ (row.lrr * 100).toFixed(2) }}%</td><td class="number">{{ formatQty(row.sampleQty) }}</td><td class="number">{{ formatQty(row.scrapQty) }}</td><td class="number">{{ formatQty(row.defectQty) }}</td><td class="number">{{ row.scrapPpm.toFixed(1) }}</td><td class="number">{{ row.defectPpm.toFixed(1) }}</td></tr><tr v-if="!displayRows.length"><td colspan="10" class="empty">沒有彙總列。</td></tr></tbody></table></div></section></div>
      <p class="disclaimer">圖表公式依舊 Core 彙總欄位模擬；批次、QC Item、Factor 值與彙總數均為本機 DEV 假資料。手動 Limit 只作用於目前圖表區間，Export／Save／Print 不產生檔案。</p>
    </section>

    <div v-if="itemPickerOpen" class="modal-backdrop" @click.self="itemPickerOpen = false"><section class="dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 Attribute QC Item"><header><div><p class="eyebrow">ATTRIBUTE QC ITEM LOOKUP · CHARTCONTROL=1</p><h2>選擇 QC Item</h2></div><button class="button" type="button" @click="itemPickerOpen = false">Close</button></header><label class="search">搜尋 QC Item<input v-model="itemSearch" autofocus placeholder="編號或名稱"></label><div class="picker-list"><button v-for="item in filteredItems" :key="item.qcItemNo" type="button" class="picker-row" @click="chooseItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!filteredItems.length" class="empty">沒有符合的 QC Item。</p></div></section></div>

    <div v-if="targetDialogOpen" class="modal-backdrop" @click.self="targetDialogOpen = false"><section class="dialog target-dialog" role="dialog" aria-modal="true" aria-label="設定手動目標線"><header><div><p class="eyebrow">MANUAL LIMIT LINE</p><h2>Set Limit Line</h2></div><button class="button" type="button" @click="targetDialogOpen = false">Close</button></header><p>設定目標值與期間範圍；超過目標的點會標紅。LRR 使用比例（0–1），PPM 使用每百萬樣本數。</p><label>Limit Target<input v-model="targetValue" type="number" min="0.000001" step="any" :placeholder="reportType === 'LRR' ? '例如 0.05 = 5%' : '例如 1200'"></label><div class="target-range"><label>Start Period<select v-model.number="targetFrom"><option v-for="(row, index) in displayRows" :key="`from-${index}`" :value="index">{{ row.period }} ({{ row.date }})</option></select></label><label>End Period<select v-model.number="targetTo"><option v-for="(row, index) in displayRows" :key="`to-${index}`" :value="index">{{ row.period }} ({{ row.date }})</option></select></label></div><footer><button class="button" type="button" @click="targetDialogOpen = false">Cancel</button><button class="button primary" type="button" :disabled="!Number(targetValue) || targetFrom > targetTo" @click="applyTarget">Apply</button></footer></section></div>
  </main>
</template>

<style scoped>
.lrr-page { --ink:#252c27; --muted:#6d766f; --line:#d4dcd6; --paper:#fff; --canvas:#f2f5f2; --green:#287552; height:100%; min-height:0; overflow:auto; padding:18px; box-sizing:border-box; background:var(--canvas); color:var(--ink); font:inherit; }
.page-head,.query-row,.filter-row,.factor-head,.result-head,.chart-title,.detail-card>header { display:flex; align-items:center; justify-content:space-between; gap:12px; }.page-head { margin-bottom:14px; }.page-head h1,.result-head h2 { margin:2px 0; font-size:23px; }.page-head p,.result-head p { margin:4px 0 0; color:var(--muted); }.eyebrow { margin:0; color:var(--green); font-size:11px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:13px; }.panel { margin-bottom:12px; border:1px solid var(--line); border-radius:6px; background:var(--paper); box-shadow:0 1px 3px #28342b0b; }.criteria,.result-panel { padding:14px; }.query-row { align-items:end; flex-wrap:wrap; }.query-row label,.search,.target-dialog label { display:grid; gap:5px; color:var(--muted); font-size:12px; font-weight:700; }.query-row>label:not(.item-field) { min-width:145px; }.item-field { flex:1; min-width:260px; }.item-display { display:flex; gap:5px; }.item-display>span { display:block; flex:1; overflow:hidden; padding:8px 9px; border:1px solid #c8d0ca; border-radius:4px; background:#fafcfa; color:var(--ink); white-space:nowrap; text-overflow:ellipsis; }.query-row input,.factor-input,.search input,.target-dialog input,.target-dialog select { min-height:34px; border:1px solid #c8d0ca; border-radius:4px; padding:5px 9px; box-sizing:border-box; background:white; color:var(--ink); font:inherit; }.item-name { margin:7px 0 2px; color:var(--muted); font-size:12px; }.filter-row { justify-content:flex-start; flex-wrap:wrap; margin:11px 0; }.filter-row fieldset { display:flex; gap:12px; align-items:center; min-height:48px; margin:0; padding:3px 10px 7px; border:1px solid var(--line); border-radius:4px; }.filter-row legend { padding:0 5px; color:var(--muted); font-size:11px; font-weight:700; }.filter-row label,.factor-options label { display:inline-flex; align-items:center; gap:4px; color:#39453d; font-size:12px; }.factor-head { margin:12px 0 7px; }.factor-head h2 { margin:0; font-size:14px; }.factor-head span,.factor-note,.muted { color:var(--muted); font-size:11px; }.factor-table-wrap,.table-scroll { max-width:100%; overflow:auto; border:1px solid var(--line); border-radius:4px; }.factor-table { width:100%; border-collapse:collapse; font-size:12px; }.factor-table th,.factor-table td,table th,table td { border-bottom:1px solid #e8ece8; padding:7px 9px; text-align:left; white-space:nowrap; }.factor-table th,table th { position:sticky; top:0; z-index:1; background:#f2f6f2; color:#59645c; font-size:11px; }.factor-table tbody tr:last-child td,table tbody tr:last-child td { border-bottom:0; }.factor-options { display:flex; flex-wrap:wrap; gap:12px; }.factor-input { min-width:260px; min-height:29px!important; }.factor-note { margin:7px 0 0; }.notice-panel { padding:9px 14px; }.notice,.feedback { margin:0; color:#536158; font-size:12px; }.feedback { margin-top:5px; color:#287552; }.result-head { align-items:flex-start; margin-bottom:13px; }.result-head h2 { font-size:18px; }.result-head p:last-child { font-size:11px; }.result-actions { display:flex; flex-wrap:wrap; justify-content:flex-end; gap:6px; }.stats-grid { display:grid; grid-template-columns:repeat(6,minmax(100px,1fr)); gap:8px; margin-bottom:12px; }.stats-grid>div { min-width:0; display:grid; gap:4px; padding:8px 10px; border:1px solid var(--line); border-radius:4px; background:#fbfcfb; }.stats-grid small { color:var(--muted); font-size:9px; font-weight:800; letter-spacing:.04em; }.stats-grid b { overflow:hidden; text-overflow:ellipsis; font-size:14px; white-space:nowrap; }.chart-card { padding:10px; border:1px solid var(--line); border-radius:4px; }.chart-title { margin-bottom:6px; }.chart-title h3 { margin:0; font-size:15px; }.chart-title span { display:block; margin-top:3px; color:var(--muted); font-size:11px; }.chart-title>span { white-space:nowrap; }.chart-scroll { height:355px; min-width:580px; overflow:auto hidden; }.chart-scroll svg { width:100%; min-width:580px; height:100%; }.grid-line { stroke:#e8ece8; stroke-width:1; }.axis-line { stroke:#78847b; stroke-width:1.2; }.axis-label { fill:#58645b; font-size:11px; }.chart-point { cursor:pointer; }.chart-point circle { stroke:white; stroke-width:1.5; }.chart-point circle.selected { stroke:#252c27; stroke-width:2.5; }.chart-point circle.out-of-target { fill:#c5413f; }.target-line { fill:none; stroke:#ca5148; stroke-width:2; stroke-dasharray:7 4; }.limit-point { fill:#ca5148; }.chart-empty { min-height:150px; display:grid; place-items:center; color:var(--muted); }.legend { display:flex; flex-wrap:wrap; gap:12px; margin:6px 0 0; color:var(--muted); font-size:10px; }.legend span { display:inline-flex; align-items:center; gap:5px; }.legend i { width:12px; height:12px; display:inline-block; border-radius:50%; background:#365f9e; }.legend .target-swatch,.legend .out-swatch { width:18px; height:0; border-top:2px dashed #ca5148; border-radius:0; background:transparent; }.legend .out-swatch { width:10px; height:10px; border:0; border-radius:50%; background:#c5413f; }.detail-grid { display:grid; grid-template-columns:minmax(245px,.65fr) minmax(0,1.7fr); gap:10px; margin-top:10px; }.detail-card { min-width:0; overflow:hidden; border:1px solid var(--line); border-radius:4px; }.detail-card>header { padding:9px 11px; border-bottom:1px solid var(--line); background:#f8faf8; }.detail-card h3 { margin:0; font-size:13px; }.detail-card>header span { color:var(--muted); font-size:10px; }.detail-card dl { display:grid; grid-template-columns:130px 1fr; gap:6px 10px; margin:0; padding:10px 12px; font-size:11px; }.detail-card dt { color:var(--muted); }.detail-card dd { margin:0; font-weight:650; }.table-scroll { max-height:330px; border:0; border-radius:0; }table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }tbody tr { cursor:pointer; }tbody tr:hover,tbody tr.active { background:#eaf3ec; }.code { color:#246a4e; font-family:Consolas,monospace; }.number { text-align:right!important; font-variant-numeric:tabular-nums; }.empty { padding:12px; color:#89928b; text-align:center; }.disclaimer { margin:8px 0 0; color:var(--muted); font-size:10px; }.button { min-height:32px; padding:5px 11px; border:1px solid #bfc9c1; border-radius:4px; background:#fff; color:#313d34; font:inherit; font-size:12px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:white; }.button:disabled { cursor:not-allowed; opacity:.5; }.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:20px; background:#1d2822a8; }.dialog { width:min(740px,96vw); max-height:84vh; overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.dialog>header { display:flex; justify-content:space-between; align-items:center; padding:14px 16px; border-bottom:1px solid var(--line); }.dialog h2 { margin:2px 0 0; font-size:18px; }.search { padding:12px 16px; }.picker-list { max-height:55vh; overflow:auto; padding:5px 10px 12px; }.picker-row { width:100%; display:grid; grid-template-columns:155px 1fr 90px; gap:10px; padding:11px 10px; border:0; border-bottom:1px solid #edf0ed; background:white; color:var(--ink); text-align:left; cursor:pointer; }.picker-row:hover { background:#e8f3ec; }.picker-row b { color:var(--green); font-family:Consolas,monospace; }.target-dialog { padding-bottom:14px; }.target-dialog>p,.target-dialog>label,.target-range { margin:12px 16px; }.target-dialog>p { color:var(--muted); font-size:12px; }.target-dialog input,.target-dialog select { width:100%; }.target-range { display:grid; grid-template-columns:1fr 1fr; gap:10px; }.target-dialog footer { display:flex; justify-content:flex-end; gap:8px; padding:0 16px; }
@media (max-width:1000px) { .stats-grid { grid-template-columns:repeat(3,minmax(90px,1fr)); }.detail-grid { grid-template-columns:1fr; }.query-row { align-items:stretch; }.item-field { min-width:200px; } }
@media (max-width:650px) { .lrr-page { padding:10px; }.page-head,.result-head { align-items:flex-start; flex-direction:column; }.stats-grid { grid-template-columns:repeat(2,minmax(100px,1fr)); }.filter-row fieldset { align-items:flex-start; flex-direction:column; }.chart-scroll { min-width:600px; }.picker-row { grid-template-columns:1fr; }.target-range { grid-template-columns:1fr; } }
</style>

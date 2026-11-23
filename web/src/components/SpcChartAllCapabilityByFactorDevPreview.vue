<script setup lang="ts">
import { computed, ref } from 'vue'

type ReportType = 'WEEK' | 'MONTH' | 'YEAR' | 'ALL'
type LotState = '0' | '1' | '2'
type LotResult = '0' | '1'
type MetricCode = 'PP' | 'PPK' | 'CP' | 'CPK' | 'CA' | 'Mean' | 'EstSd' | 'Sd'
type MetricValues = Record<MetricCode, number>
interface FactorDefinition { factorNo: string; factorName: string; parameterType: 0 | 1 | 2; online: 'Y' | 'N'; options: string[] }
interface Sample { date: string; state: LotState; result: LotResult; lots: number; metrics: MetricValues }
interface CapabilityFixture { id: string; qcItemNo: string; qcItemName: string; factors: Record<string, string>; us: string; ls: string; samples: Sample[] }
interface Interval { key: string; label: string; start: Date; end: Date }
interface ResultPeriod { intervalKey: string; lotCount: number; metrics: MetricValues | null }
interface ResultRow { id: string; qcItemNo: string; qcItemName: string; factors: Record<string, string>; us: string; ls: string; periods: ResultPeriod[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const factors: FactorDefinition[] = [
  { factorNo: 'MO', factorName: '工單', parameterType: 2, online: 'Y', options: [] },
  { factorNo: 'OP', factorName: '作業站', parameterType: 0, online: 'Y', options: ['OP-10', 'OP-20', 'OP-35'] },
  { factorNo: 'EQUIPMENT', factorName: '設備', parameterType: 1, online: 'Y', options: ['EQ-020', 'EQ-035', 'EQ-041'] },
  { factorNo: 'PRODUCT', factorName: '產品', parameterType: 0, online: 'Y', options: ['PRD-AX17', 'PRD-PH08'] },
  { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, online: 'N', options: [] },
]
const allMetrics: Array<{ value: MetricCode; label: string }> = [
  { value: 'PP', label: 'PP' }, { value: 'PPK', label: 'PPK' }, { value: 'CP', label: 'CP' }, { value: 'CPK', label: 'CPK' },
  { value: 'CA', label: 'CA' }, { value: 'Mean', label: 'Mean' }, { value: 'EstSd', label: 'EstSd' }, { value: 'Sd', label: 'Sd' },
]
function sampleDate(daysAgo: number, hour: number): string {
  const value = new Date()
  value.setDate(value.getDate() - daysAgo)
  value.setHours(hour, 15, 0, 0)
  return value.toISOString()
}
function metrics(values: Partial<MetricValues>): MetricValues {
  return { PP: 1.54, PPK: 1.31, CP: 1.67, CPK: 1.42, CA: 8.4, Mean: 10.016, EstSd: 0.04, Sd: 0.043, ...values }
}
const fixtureRows: CapabilityFixture[] = [
  { id: 'cap-a', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', factors: { MO: 'MO-2618', OP: 'OP-20', EQUIPMENT: 'EQ-020', PRODUCT: 'PRD-AX17', SHIFT: 'A' }, us: '10.200', ls: '9.800', samples: [
    { date: sampleDate(58, 9), state: '0', result: '0', lots: 28, metrics: metrics({ CP: 1.67, CPK: 1.42, Mean: 10.016 }) },
    { date: sampleDate(36, 14), state: '0', result: '1', lots: 9, metrics: metrics({ CP: 1.73, CPK: 1.51, Mean: 10.013 }) },
    { date: sampleDate(11, 10), state: '0', result: '0', lots: 31, metrics: metrics({ CP: 1.69, CPK: 1.46, Mean: 10.014 }) },
    { date: sampleDate(5, 16), state: '1', result: '1', lots: 2, metrics: metrics({ CP: 1.61, CPK: 1.38 }) },
  ] },
  { id: 'cap-b', qcItemNo: 'VAR-DEV-202', qcItemName: '壓合高度', factors: { MO: 'MO-2621', OP: 'OP-35', EQUIPMENT: 'EQ-035', PRODUCT: 'PRD-PH08', SHIFT: 'B' }, us: '2.200', ls: '1.800', samples: [
    { date: sampleDate(55, 8), state: '0', result: '0', lots: 18, metrics: metrics({ CP: 1.31, CPK: 1.08, Mean: 2.034 }) },
    { date: sampleDate(30, 15), state: '0', result: '0', lots: 22, metrics: metrics({ CP: 1.38, CPK: 1.16, Mean: 2.030 }) },
    { date: sampleDate(9, 11), state: '0', result: '1', lots: 6, metrics: metrics({ CP: 1.41, CPK: 1.18, Mean: 2.028 }) },
    { date: sampleDate(3, 17), state: '2', result: '0', lots: 1, metrics: metrics({ CP: 1.29, CPK: 1.04 }) },
  ] },
  { id: 'cap-c', qcItemNo: 'VAR-DEV-305', qcItemName: '端面平面度', factors: { MO: 'MO-2624', OP: 'OP-10', EQUIPMENT: 'EQ-041', PRODUCT: 'PRD-AX17', SHIFT: 'C' }, us: '0.050', ls: '0.000', samples: [
    { date: sampleDate(52, 12), state: '0', result: '0', lots: 15, metrics: metrics({ CP: 1.59, CPK: 1.43, Mean: 0.019 }) },
    { date: sampleDate(32, 16), state: '0', result: '0', lots: 17, metrics: metrics({ CP: 1.62, CPK: 1.46, Mean: 0.020 }) },
    { date: sampleDate(7, 10), state: '0', result: '1', lots: 5, metrics: metrics({ CP: 1.64, CPK: 1.48, Mean: 0.020 }) },
    { date: sampleDate(2, 13), state: '1', result: '0', lots: 1, metrics: metrics({ CP: 1.55, CPK: 1.33 }) },
  ] },
]
function toLocalInput(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
function defaultRange(): { fromDate: string; endDate: string } {
  const now = new Date()
  const from = new Date(now)
  from.setDate(from.getDate() + 1)
  from.setMonth(from.getMonth() - 2)
  from.setHours(0, 0, 0, 0)
  const end = new Date(now)
  end.setHours(23, 59, 59, 0)
  return { fromDate: toLocalInput(from), endDate: toLocalInput(end) }
}
const range = defaultRange()
const query = ref({
  reportType: 'MONTH' as ReportType, ...range, fromEnabled: false, endEnabled: false,
  lotStates: ['0', '1', '2'] as LotState[], lotResults: ['0', '1'] as LotResult[], calRule: 0 as 0 | 1,
  metrics: allMetrics.map((metric) => metric.value), factorValues: Object.fromEntries(factors.map((factor) => [factor.factorNo, ''])) as Record<string, string>,
})
const applied = ref<typeof query.value | null>(null)
const searched = ref(false)
const dateConfirmPending = ref(false)
const notice = ref('輸入至少一項要因值後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
const chartRowId = ref('')
const chartMetric = ref<MetricCode>('CPK')
const chartOpen = ref(false)
const onlineFactorNos = computed(() => factors.filter((factor) => factor.online === 'Y').map((factor) => factor.factorNo))
const activeMetrics = computed(() => allMetrics.filter((metric) => (applied.value?.metrics ?? query.value.metrics).includes(metric.value)))
const intervals = computed(() => applied.value ? makeIntervals(applied.value) : [])
const resultRows = computed<ResultRow[]>(() => {
  const active = applied.value
  if (!active) return []
  return fixtureRows.filter((row) => matchesFactors(row, active)).map((row) => ({
    id: row.id, qcItemNo: row.qcItemNo, qcItemName: row.qcItemName, factors: row.factors, us: row.us, ls: row.ls,
    periods: intervals.value.map((interval) => calculatePeriod(row, interval, active)),
  })).filter((row) => row.periods.some((period) => period.lotCount > 0))
})
const selectedChartRow = computed(() => resultRows.value.find((row) => row.id === chartRowId.value) ?? null)
const chartPoints = computed(() => selectedChartRow.value?.periods.map((period) => ({
  label: intervals.value.find((interval) => interval.key === period.intervalKey)?.label ?? period.intervalKey,
  value: period.metrics?.[chartMetric.value] ?? null,
})) ?? [])
const resultColumnCount = computed(() => 5 + onlineFactorNos.value.length + Math.max(1, intervals.value.length * Math.max(1, activeMetrics.value.length)))

function makeIntervals(active: typeof query.value): Interval[] {
  const samples = fixtureRows.flatMap((row) => row.samples.map((sample) => new Date(sample.date).getTime()))
  let start = active.reportType === 'ALL' && !active.fromEnabled ? new Date(Math.min(...samples)) : new Date(active.fromDate)
  let end = active.reportType === 'ALL' && !active.endEnabled ? new Date(Math.max(...samples)) : new Date(active.endDate)
  if (active.reportType !== 'ALL') { start.setHours(0, 0, 0, 0); end.setHours(23, 59, 59, 999) }
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start >= end) return []
  if (active.reportType === 'ALL') return [{ key: 'all', label: 'ALL', start, end }]
  const result: Interval[] = []
  let cursor = new Date(start)
  while (cursor <= end && result.length < 37) {
    const periodStart = new Date(cursor)
    let periodEnd: Date
    if (active.reportType === 'WEEK') { periodEnd = new Date(cursor); periodEnd.setDate(periodEnd.getDate() + 6 - periodEnd.getDay()) }
    else if (active.reportType === 'MONTH') periodEnd = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0)
    else periodEnd = new Date(cursor.getFullYear(), 11, 31)
    periodEnd.setHours(23, 59, 59, 999)
    if (periodEnd > end) periodEnd = new Date(end)
    result.push({ key: String(periodStart.getTime()), label: periodLabel(active.reportType, periodStart, periodEnd), start: periodStart, end: periodEnd })
    cursor = new Date(periodEnd); cursor.setDate(cursor.getDate() + 1); cursor.setHours(0, 0, 0, 0)
  }
  return result
}
function periodLabel(type: ReportType, start: Date, end: Date): string {
  if (type === 'WEEK') return `${shortDate(start)}–${shortDate(end)}`
  if (type === 'MONTH') return `${start.getFullYear()}/${String(start.getMonth() + 1).padStart(2, '0')}`
  return String(start.getFullYear())
}
function shortDate(value: Date): string { return `${String(value.getMonth() + 1).padStart(2, '0')}/${String(value.getDate()).padStart(2, '0')}` }
function matchesFactors(row: CapabilityFixture, active: typeof query.value): boolean {
  return factors.every((factor) => {
    const value = active.factorValues[factor.factorNo]?.trim()
    if (!value) return true
    return value.split(',').map((part) => part.trim().toUpperCase()).filter(Boolean).includes((row.factors[factor.factorNo] ?? '').toUpperCase())
  })
}
function selectedLotCount(sample: Sample, active: typeof query.value): number {
  return active.lotStates.includes(sample.state) && active.lotResults.includes(sample.result) ? sample.lots : 0
}
function calculatePeriod(row: CapabilityFixture, interval: Interval, active: typeof query.value): ResultPeriod {
  const dated = row.samples.filter((sample) => { const time = new Date(sample.date); return time >= interval.start && time <= interval.end })
  const lotCount = dated.reduce((total, sample) => total + selectedLotCount(sample, active), 0)
  if (lotCount === 0) return { intervalKey: interval.key, lotCount, metrics: null }
  const raw = dated.filter((sample) => sample.state === '0')
  if (!raw.length) return { intervalKey: interval.key, lotCount, metrics: null }
  const values = {} as MetricValues
  for (const metric of allMetrics) {
    const weight = raw.reduce((total, sample) => total + sample.lots, 0) || raw.length
    values[metric.value] = raw.reduce((total, sample) => total + sample.metrics[metric.value] * sample.lots, 0) / weight
  }
  return { intervalKey: interval.key, lotCount, metrics: values }
}
function runQuery(confirmed = false): void {
  dateConfirmPending.value = false
  if (!Object.values(query.value.factorValues).some((value) => value.trim())) return fail('請至少輸入一項要因查詢值。')
  if (!query.value.lotStates.length) return fail('請至少選擇一種 Lot Report State。')
  if (!query.value.lotResults.length) return fail('請至少選擇一種 Lot Result。')
  if (!query.value.metrics.length) return fail('請至少選擇一項 Capability Type。')
  if (query.value.reportType !== 'ALL' && (!query.value.fromEnabled || !query.value.endEnabled) && !confirmed) {
    dateConfirmPending.value = true
    noticeKind.value = 'info'
    notice.value = '原表單會詢問是否設定日期；可沿用畫面日期繼續，或返回勾選日期欄位。'
    return
  }
  const from = new Date(query.value.fromDate); const end = new Date(query.value.endDate)
  if (query.value.reportType !== 'ALL' || (query.value.fromEnabled && query.value.endEnabled)) {
    if (Number.isNaN(from.getTime()) || Number.isNaN(end.getTime()) || from >= end) return fail('查詢起迄時間無效，From 必須早於 End。')
  }
  const snapshot = { ...query.value, lotStates: [...query.value.lotStates], lotResults: [...query.value.lotResults], metrics: [...query.value.metrics], factorValues: { ...query.value.factorValues } }
  const periodCount = makeIntervals(snapshot).length
  if (periodCount > 36) return fail('DEV 預覽最多顯示 36 個週期，請縮短日期區間。')
  applied.value = snapshot; searched.value = true; chartOpen.value = false; chartRowId.value = ''
  if (!snapshot.metrics.includes(chartMetric.value)) chartMetric.value = snapshot.metrics[0] ?? 'CPK'
  noticeKind.value = 'success'; notice.value = `查詢完成：${resultRows.value.length} 組 Factor，${intervals.value.length} 個報表區間（DEV 假資料）。`
}
function fail(message: string): void { dateConfirmPending.value = false; noticeKind.value = 'error'; notice.value = message }
function clearFactors(): void { for (const factor of factors) query.value.factorValues[factor.factorNo] = '' }
function updateFactor(factor: FactorDefinition, event: Event): void {
  const input = event.target as HTMLInputElement
  query.value.factorValues[factor.factorNo] = factor.parameterType === 2 ? input.value.toUpperCase() : input.value
}
function onFactorSelect(factor: FactorDefinition, event: Event): void {
  const selected = [...(event.target as HTMLSelectElement).selectedOptions].map((option) => option.value)
  query.value.factorValues[factor.factorNo] = selected.join(',')
}
function selectRow(id: string): void { chartRowId.value = id }
function openChart(): void { if (!chartRowId.value) return; chartOpen.value = true }
function closeChart(): void { chartOpen.value = false }
function metric(value: number | null): string { return value === null ? '—' : value.toFixed(3) }
function dateTime(value: Date): string { return toLocalInput(value).replace('T', ' ') }
function barWidth(value: number | null): string { return value === null ? '0%' : `${Math.max(3, Math.min(100, value / 2.5 * 100))}%` }
const stateOptions: Array<{ value: LotState; label: string }> = [{ value: '0', label: 'Normal' }, { value: '1', label: 'Excluded' }, { value: '2', label: 'Isolated' }]
const resultOptions: Array<{ value: LotResult; label: string }> = [{ value: '0', label: 'Pass' }, { value: '1', label: 'Fail' }]
</script>

<template>
  <section v-if="props.developmentPreview" class="capability-page" aria-label="SPC 製程能力報表_以要因查詢">
    <header class="page-head"><div><p class="eyebrow">SPC REPORT / SC311</p><h1>製程能力報表_以要因查詢</h1><p class="subtitle">此查詢不選 QC Item；從全部 Variable QC Item 的要因中輸入條件。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>
    <section class="filter-panel panel">
      <div class="top-grid">
        <fieldset class="factor-panel"><legend>Collect Factor</legend><div class="factor-table-wrap"><table class="factor-table"><thead><tr><th>FactorNo</th><th>Factor Name</th><th>Factor Value</th><th>Input Type</th></tr></thead><tbody>
          <tr v-for="factor in factors" :key="factor.factorNo"><td>{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>
            <select v-if="factor.parameterType !== 2" multiple size="2" :aria-label="`${factor.factorName} 多選值`" @change="onFactorSelect(factor, $event)"><option v-for="option in factor.options" :key="option" :value="option" :selected="query.factorValues[factor.factorNo].split(',').includes(option)">{{ option }}</option></select>
            <input v-else :value="query.factorValues[factor.factorNo]" :aria-label="`${factor.factorName} 值`" placeholder="輸入值；多值以逗號分隔" @input="updateFactor(factor, $event)">
          </td><td>{{ factor.parameterType === 2 ? '手動輸入' : 'MES Parameter' }}<span v-if="factor.online !== 'Y'"> · 不顯示於結果欄</span></td></tr>
        </tbody></table><small>要因清單及 lookup 僅為 DEV 假資料。至少輸入一項 Factor Value 才能查詢；ParameterType=2 會轉大寫。</small></div></fieldset>
        <fieldset class="date-field"><legend>Time Interval</legend><label><input v-model="query.fromEnabled" type="checkbox"> From</label><input v-model="query.fromDate" type="datetime-local" step="1"><label><input v-model="query.endEnabled" type="checkbox"> End</label><input v-model="query.endDate" type="datetime-local" step="1"><small>預設為兩個月前起至今天；ALL 可取消日期端點，查詢全部 DEV 假資料。</small></fieldset>
        <fieldset class="report-field"><legend>Report Type</legend><label><input v-model="query.reportType" type="radio" value="ALL"> ALL</label><label><input v-model="query.reportType" type="radio" value="WEEK"> WEEK</label><label><input v-model="query.reportType" type="radio" value="MONTH"> MONTH</label><label><input v-model="query.reportType" type="radio" value="YEAR"> YEAR</label></fieldset>
      </div>
      <div class="option-grid">
        <fieldset><legend>Lot Report State</legend><label v-for="option in stateOptions" :key="option.value"><input v-model="query.lotStates" type="checkbox" :value="option.value"> {{ option.label }}</label></fieldset>
        <fieldset><legend>Lot Result</legend><label v-for="option in resultOptions" :key="option.value"><input v-model="query.lotResults" type="checkbox" :value="option.value"> {{ option.label }}</label></fieldset>
        <fieldset class="cal-rule"><legend>Chart Calculate Principle</legend><label><input v-model="query.calRule" type="radio" :value="0"> Cumulative Sample Size ≥ 25 Only</label><label><input v-model="query.calRule" type="radio" :value="1"> ALL</label></fieldset>
        <fieldset class="metrics-field"><legend>Capability Type</legend><label v-for="item in allMetrics" :key="item.value"><input v-model="query.metrics" type="checkbox" :value="item.value"> {{ item.label }}</label></fieldset>
      </div>
      <footer class="filter-actions"><span>QC Item 欄位與選擇器依舊 Form 設定隱藏；Report Type 預設 MONTH。</span><span><button class="button" type="button" @click="clearFactors">Clear</button> <button class="button primary" type="button" @click="runQuery()">Query</button></span></footer>
      <div v-if="dateConfirmPending" class="confirm-line" role="group" aria-label="確認日期區間"><span>日期未勾選。要沿用畫面日期繼續嗎？</span><button class="button primary" type="button" @click="runQuery(true)">繼續</button><button class="button" type="button" @click="dateConfirmPending = false">返回</button></div>
    </section>
    <section class="result-panel panel"><nav class="result-toolbar"><div><strong>Capability Result</strong><span v-if="searched">{{ resultRows.length }} 組 · {{ intervals.length }} 個區間</span></div><button class="button" type="button" :disabled="!chartRowId" @click="openChart">Chart</button></nav>
      <p v-if="!searched" class="empty-state">輸入至少一項要因值後按 Query 顯示能力報表。</p>
      <div v-else class="table-scroll"><table class="result-table"><thead><tr><th rowspan="2">QCItemNo</th><th rowspan="2">QCItemName</th><th v-for="factorNo in onlineFactorNos" :key="factorNo" rowspan="2">{{ factorNo }}</th><th rowspan="2">US</th><th rowspan="2">LS</th><th rowspan="2">LotCount</th><th v-for="period in intervals" :key="period.key" :colspan="Math.max(1, activeMetrics.length)">{{ period.label }}<small>{{ dateTime(period.start) }} – {{ dateTime(period.end) }}</small></th></tr><tr><template v-for="period in intervals" :key="`metrics-${period.key}`"><th v-for="item in activeMetrics" :key="`${period.key}-${item.value}`">{{ item.label }}</th></template></tr></thead>
        <tbody><tr v-for="row in resultRows" :key="row.id" :class="{ selected: chartRowId === row.id }" @click="selectRow(row.id)"><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td v-for="factorNo in onlineFactorNos" :key="`${row.id}-${factorNo}`">{{ row.factors[factorNo] ?? '—' }}</td><td>{{ row.us }}</td><td>{{ row.ls }}</td><td class="number">{{ row.periods.reduce((total, period) => total + period.lotCount, 0) }}</td><template v-for="period in row.periods" :key="period.intervalKey"><td v-for="item in activeMetrics" :key="`${period.intervalKey}-${item.value}`" class="number">{{ metric(period.metrics?.[item.value] ?? null) }}</td></template></tr><tr v-if="resultRows.length === 0"><td class="empty-cell" :colspan="resultColumnCount">沒有符合條件的 DEV 假資料。</td></tr></tbody>
      </table></div><footer v-if="searched" class="result-foot">選取結果列後按 Chart 查看週／月／年或 ALL 能力指標；數值為固定 DEV 假資料，不重算正式 SPC 統計。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>
    <div v-if="chartOpen && selectedChartRow" class="modal-backdrop" @click.self="closeChart"><section class="chart-dialog" role="dialog" aria-modal="true" :aria-label="`${selectedChartRow.qcItemNo} 能力趨勢`"><header><div><p class="eyebrow">DEV TREND / {{ selectedChartRow.qcItemNo }}</p><h2>{{ selectedChartRow.qcItemName }} 能力趨勢</h2><p>{{ onlineFactorNos.map((factorNo) => `${factorNo}: ${selectedChartRow?.factors[factorNo] ?? '—'}`).join(' · ') }}</p></div><button class="button" type="button" @click="closeChart">Close (X)</button></header><label class="chart-metric">Chart Metric <select v-model="chartMetric"><option v-for="item in activeMetrics" :key="item.value" :value="item.value">{{ item.label }}</option></select></label><div class="chart-bars"><div v-for="(point, index) in chartPoints" :key="`${point.label}-${index}`" class="chart-point"><div class="bar-value">{{ metric(point.value) }}</div><div class="bar-track"><span :style="{ width: barWidth(point.value) }"></span></div><div class="bar-label">{{ point.label }}</div></div><p v-if="!chartPoints.length" class="empty-hint">此區間沒有資料。</p></div><footer>週期欄位與趨勢只示範 DEV 假資料，未連正式服務。</footer></section></div>
  </section>
</template>

<style scoped>
.capability-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;box-sizing:border-box;background:#f5f4ef;color:#292820;font:12px/1.35 Arial,"Microsoft JhengHei",sans-serif;overflow:auto}.panel{border:1px solid #d0cec4;border-radius:4px;background:#fff;box-shadow:0 1px 2px #0000000a}.page-head{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:0;font-size:19px;font-weight:600}.eyebrow{margin:0 0 2px;color:#8b7952;font-size:10px;letter-spacing:.08em}.subtitle{margin:3px 0 0;color:#777366;font-size:11px}.button{min-height:27px;padding:4px 12px;border:1px solid #3b3930;border-radius:3px;background:#fff;color:#3b3930;font:inherit;cursor:pointer}.button.primary{background:#3b3930;color:white}.button:disabled{opacity:.45;cursor:not-allowed}.filter-panel{padding:9px}.top-grid{display:grid;grid-template-columns:minmax(390px,1.45fr) minmax(260px,1fr) minmax(145px,.5fr);gap:8px}.top-grid fieldset,.option-grid fieldset{min-width:0;margin:0;padding:7px;border:1px solid #dedbd1;border-radius:3px}.top-grid legend,.option-grid legend{padding:0 4px;color:#686458;font-size:11px;font-weight:600}.factor-panel{grid-row:span 1}.factor-table-wrap{max-height:145px;overflow:auto}.factor-table,.result-table{width:100%;border-collapse:collapse}.factor-table th,.factor-table td{padding:4px 6px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:middle}.factor-table th{position:sticky;top:0;background:#f4f2eb;color:#625e52;font-weight:600}.factor-table input,.factor-table select{width:100%;min-width:130px;min-height:28px;box-sizing:border-box;border:1px solid #cbc8bd;background:white;font:inherit}.factor-table select{min-height:45px}.factor-table-wrap small{display:block;margin-top:4px;color:#827e71;font-size:10px}.date-field{display:grid;grid-template-columns:54px 1fr;align-items:center;gap:5px}.date-field legend{grid-column:1/-1}.date-field input[type="datetime-local"]{min-width:0;width:100%;height:25px;box-sizing:border-box;border:1px solid #cbc8bd;border-radius:2px;font:inherit}.date-field small{grid-column:1/-1;color:#827e71;font-size:10px}.report-field{display:flex;flex-wrap:wrap;align-content:flex-start;gap:7px 12px}.report-field legend{width:100%}.report-field label,.option-grid fieldset>label{display:flex;align-items:center;gap:4px;white-space:nowrap}.option-grid{display:grid;grid-template-columns:1fr .7fr 1.55fr 2fr;gap:8px;margin-top:7px}.option-grid fieldset{display:flex;align-content:flex-start;align-items:center;flex-wrap:wrap;gap:5px 10px}.option-grid fieldset legend{width:100%}.cal-rule label{white-space:normal!important}.metrics-field{gap:5px 9px!important}.filter-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:7px;color:#777366;font-size:10px}.confirm-line{display:flex;align-items:center;gap:6px;margin-top:7px;padding:7px;border:1px solid #e3d9a8;background:#fff8d8}.result-panel{display:flex;flex-direction:column;min-height:180px;flex:1;overflow:hidden}.result-toolbar{display:flex;justify-content:space-between;align-items:center;padding:7px 9px;border-bottom:1px solid #e6e3da}.result-toolbar div{display:flex;align-items:center;gap:10px}.result-toolbar span{color:#7b7669;font-size:10px}.table-scroll{flex:1;overflow:auto}.result-table{font-size:11px;white-space:nowrap}.result-table th,.result-table td{padding:5px 7px;border-right:1px solid #eceae4;border-bottom:1px solid #e7e4dc;text-align:left;vertical-align:middle}.result-table thead th{position:sticky;top:0;z-index:1;background:#f1efe8;color:#5f5b50;font-weight:600}.result-table thead th small{display:block;margin-top:2px;color:#858174;font-size:9px;font-weight:400}.result-table tbody tr{cursor:pointer}.result-table tbody tr:hover{background:#fffbed}.result-table tbody tr.selected{background:#fff5cf}.number{text-align:right!important;font-variant-numeric:tabular-nums}.empty-state,.empty-cell{text-align:center!important;color:#888;padding:24px!important}.result-foot{padding:6px 9px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{margin:0;padding:7px 9px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b;font-size:11px}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:8000;display:grid;place-items:center;padding:20px;background:#17171080}.chart-dialog{width:min(860px,96vw);max-height:88vh;overflow:auto;padding:14px;border:1px solid #bbb7aa;border-radius:5px;background:#fff;box-shadow:0 12px 40px #0004}.chart-dialog header{display:flex;justify-content:space-between;gap:12px}.chart-dialog h2{margin:0;font-size:18px}.chart-dialog header p:last-child{margin:4px 0;color:#777366}.chart-metric{display:flex;align-items:center;gap:8px;margin:12px 0}.chart-metric select{height:28px;border:1px solid #cbc8bd;background:white;font:inherit}.chart-bars{display:flex;align-items:flex-end;gap:12px;min-height:180px;padding:12px 8px;border:1px solid #ebe8df;background:repeating-linear-gradient(to bottom,#fff 0,#fff 44px,#eceae4 45px)}.chart-point{display:flex;flex:1;min-width:45px;flex-direction:column;justify-content:flex-end;gap:5px}.bar-value{text-align:center;color:#4f4b3f;font-size:10px}.bar-track{height:110px;display:flex;align-items:flex-end;border-bottom:1px solid #969184}.bar-track span{display:block;min-height:3px;background:#69836b;border-radius:3px 3px 0 0}.bar-label{text-align:center;color:#777366;font-size:10px}.chart-dialog footer{margin-top:10px;color:#858174;font-size:10px}
@media(max-width:1050px){.top-grid{grid-template-columns:1fr 1fr}.factor-panel{grid-column:1/-1}.option-grid{grid-template-columns:1fr 1fr}.report-field{grid-column:1/-1}}
@media(max-width:680px){.top-grid,.option-grid{grid-template-columns:1fr}.factor-panel{grid-column:auto}.filter-actions{align-items:flex-start;flex-direction:column}}
</style>

<script setup lang="ts">
import { computed, ref } from 'vue'

type ReportType = 'WEEK' | 'MONTH' | 'YEAR' | 'ALL'
type LotState = '0' | '1' | '2'
type LotResult = '0' | '1'
type MetricCode = 'CP' | 'CPK' | 'PP' | 'PPK' | 'CA' | 'Mean' | 'EstSd' | 'Sd'
interface MetricValues { CP: number; CPK: number; PP: number; PPK: number; CA: number; Mean: number; EstSd: number; Sd: number }
interface LotCounts { '0': Record<LotResult, number>; '1': Record<LotResult, number>; '2': Record<LotResult, number> }
interface CapabilitySample { date: string; counts: LotCounts; metrics: MetricValues }
interface CapabilityFixture { id: string; qcItemNo: string; qcItemName: string; factors: Record<string, string>; us: string; ls: string; samples: CapabilitySample[] }
interface FactorDefinition { qcItemNo: string; factorNo: string; label: string; online: 'Y' | 'N'; options: string[]; source: string }
interface FactorCriterion extends FactorDefinition { key: string }
interface QueryState {
  qcItemNos: string[]; reportType: ReportType; fromDate: string; endDate: string; fromEnabled: boolean; endEnabled: boolean
  lotStates: LotState[]; lotResults: LotResult[]; calRule: 0 | 1; metrics: MetricCode[]; factorValues: Record<string, string>
}
interface ReportInterval { key: string; label: string; start: Date; end: Date }
interface PeriodResult { intervalKey: string; lotCount: number; metrics: MetricValues | null }
interface ReportRow { id: string; qcItemNo: string; qcItemName: string; factors: Record<string, string>; us: string; ls: string; periods: PeriodResult[] }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const qcItems = [
  { no: 'VAR-DEV-101', name: '軸心直徑', issueState: 2, chartControl: 1 },
  { no: 'VAR-DEV-202', name: '壓合高度', issueState: 2, chartControl: 1 },
  { no: 'VAR-DEV-305', name: '端面平面度', issueState: 2, chartControl: 1 },
  { no: 'VAR-OLD-008', name: '舊型孔徑', issueState: -1, chartControl: 1 },
]
const factorDefinitions: FactorDefinition[] = [
  { qcItemNo: 'VAR-DEV-101', factorNo: 'MO', label: '工單', online: 'Y', source: 'MES Parameter', options: ['MO-2618', 'MO-2619', 'MO-2620'] },
  { qcItemNo: 'VAR-DEV-101', factorNo: 'OP', label: '作業站', online: 'Y', source: 'MES Parameter', options: ['OP-20', 'OP-35'] },
  { qcItemNo: 'VAR-DEV-101', factorNo: 'EQUIPMENT', label: '設備', online: 'N', source: 'MES Parameter', options: ['EQ-020', 'EQ-035'] },
  { qcItemNo: 'VAR-DEV-202', factorNo: 'MO', label: '工單', online: 'Y', source: 'MES Parameter', options: ['MO-2618', 'MO-2621'] },
  { qcItemNo: 'VAR-DEV-202', factorNo: 'CUSTOMER', label: '客戶', online: 'Y', source: 'MES Parameter', options: ['CUS-DEV-01', 'CUS-DEV-02'] },
  { qcItemNo: 'VAR-DEV-202', factorNo: 'BR', label: '批別', online: 'N', source: '手動輸入', options: ['BR-A', 'BR-C'] },
  { qcItemNo: 'VAR-DEV-305', factorNo: 'OP', label: '作業站', online: 'Y', source: 'MES Parameter', options: ['OP-10', 'OP-20'] },
  { qcItemNo: 'VAR-DEV-305', factorNo: 'PRODUCT', label: '產品', online: 'Y', source: 'MES Parameter', options: ['PRD-AX17', 'PRD-PH08'] },
]
const allMetrics: Array<{ value: MetricCode; label: string; scale: number }> = [
  { value: 'CP', label: 'CP', scale: 2.5 }, { value: 'CPK', label: 'CPK', scale: 2.5 },
  { value: 'PP', label: 'PP', scale: 2.5 }, { value: 'PPK', label: 'PPK', scale: 2.5 },
  { value: 'CA', label: 'CA', scale: 100 }, { value: 'Mean', label: 'Mean', scale: 12 },
  { value: 'EstSd', label: 'EstSd', scale: 0.25 }, { value: 'Sd', label: 'Sd', scale: 0.25 },
]
const fixtureRows: CapabilityFixture[] = [
  {
    id: 'cap-101-1', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', factors: { MO: 'MO-2618', OP: 'OP-20', EQUIPMENT: 'EQ-020' }, us: '10.200', ls: '9.800', samples: [
      { date: sampleDate(55, 10, 10), counts: { '0': { '0': 28, '1': 2 }, '1': { '0': 1, '1': 1 }, '2': { '0': 0, '1': 0 } }, metrics: { CP: 1.67, CPK: 1.42, PP: 1.54, PPK: 1.31, CA: 8.4, Mean: 10.016, EstSd: 0.040, Sd: 0.043 } },
      { date: sampleDate(36, 11, 25), counts: { '0': { '0': 34, '1': 1 }, '1': { '0': 1, '1': 0 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.73, CPK: 1.51, PP: 1.60, PPK: 1.39, CA: 6.8, Mean: 10.013, EstSd: 0.039, Sd: 0.042 } },
      { date: sampleDate(12, 9, 40), counts: { '0': { '0': 31, '1': 2 }, '1': { '0': 0, '1': 1 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.69, CPK: 1.46, PP: 1.58, PPK: 1.36, CA: 7.2, Mean: 10.014, EstSd: 0.040, Sd: 0.043 } },
    ],
  },
  {
    id: 'cap-101-2', qcItemNo: 'VAR-DEV-101', qcItemName: '軸心直徑', factors: { MO: 'MO-2619', OP: 'OP-35', EQUIPMENT: 'EQ-035' }, us: '10.200', ls: '9.800', samples: [
      { date: sampleDate(54, 14, 5), counts: { '0': { '0': 22, '1': 3 }, '1': { '0': 2, '1': 1 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.48, CPK: 1.22, PP: 1.40, PPK: 1.13, CA: 13.0, Mean: 10.026, EstSd: 0.045, Sd: 0.048 } },
      { date: sampleDate(34, 8, 35), counts: { '0': { '0': 25, '1': 2 }, '1': { '0': 1, '1': 1 }, '2': { '0': 0, '1': 1 } }, metrics: { CP: 1.55, CPK: 1.30, PP: 1.43, PPK: 1.20, CA: 11.6, Mean: 10.023, EstSd: 0.043, Sd: 0.047 } },
      { date: sampleDate(7, 13, 20), counts: { '0': { '0': 29, '1': 2 }, '1': { '0': 1, '1': 0 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.59, CPK: 1.34, PP: 1.48, PPK: 1.24, CA: 10.8, Mean: 10.022, EstSd: 0.042, Sd: 0.045 } },
    ],
  },
  {
    id: 'cap-202-1', qcItemNo: 'VAR-DEV-202', qcItemName: '壓合高度', factors: { MO: 'MO-2621', CUSTOMER: 'CUS-DEV-02', BR: 'BR-C' }, us: '2.200', ls: '1.800', samples: [
      { date: sampleDate(53, 7, 50), counts: { '0': { '0': 18, '1': 2 }, '1': { '0': 1, '1': 1 }, '2': { '0': 0, '1': 1 } }, metrics: { CP: 1.31, CPK: 1.08, PP: 1.22, PPK: 1.01, CA: 17.2, Mean: 2.034, EstSd: 0.051, Sd: 0.054 } },
      { date: sampleDate(30, 16, 10), counts: { '0': { '0': 21, '1': 2 }, '1': { '0': 1, '1': 0 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.38, CPK: 1.16, PP: 1.29, PPK: 1.08, CA: 14.9, Mean: 2.030, EstSd: 0.049, Sd: 0.052 } },
      { date: sampleDate(10, 9, 15), counts: { '0': { '0': 24, '1': 1 }, '1': { '0': 0, '1': 1 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.41, CPK: 1.18, PP: 1.32, PPK: 1.10, CA: 14.2, Mean: 2.028, EstSd: 0.048, Sd: 0.051 } },
    ],
  },
  {
    id: 'cap-305-1', qcItemNo: 'VAR-DEV-305', qcItemName: '端面平面度', factors: { OP: 'OP-10', PRODUCT: 'PRD-AX17' }, us: '0.050', ls: '0.000', samples: [
      { date: sampleDate(56, 12, 20), counts: { '0': { '0': 15, '1': 1 }, '1': { '0': 1, '1': 0 }, '2': { '0': 0, '1': 0 } }, metrics: { CP: 1.59, CPK: 1.43, PP: 1.48, PPK: 1.31, CA: 12.0, Mean: 0.019, EstSd: 0.0052, Sd: 0.0056 } },
      { date: sampleDate(32, 15, 45), counts: { '0': { '0': 17, '1': 1 }, '1': { '0': 0, '1': 1 }, '2': { '0': 1, '1': 0 } }, metrics: { CP: 1.62, CPK: 1.46, PP: 1.51, PPK: 1.34, CA: 10.8, Mean: 0.020, EstSd: 0.0051, Sd: 0.0055 } },
      { date: sampleDate(4, 10, 50), counts: { '0': { '0': 18, '1': 1 }, '1': { '0': 1, '1': 0 }, '2': { '0': 0, '1': 1 } }, metrics: { CP: 1.64, CPK: 1.48, PP: 1.54, PPK: 1.38, CA: 9.6, Mean: 0.020, EstSd: 0.0050, Sd: 0.0054 } },
    ],
  },
]

function toLocalInput(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
function sampleDate(daysAgo: number, hour: number, minute: number): string {
  const value = new Date()
  value.setDate(value.getDate() - daysAgo)
  value.setHours(hour, minute, 0, 0)
  return toLocalInput(value)
}
function makeDefaultRange(): { fromDate: string; endDate: string } {
  const now = new Date()
  const from = new Date(now)
  from.setDate(from.getDate() + 1)
  from.setMonth(from.getMonth() - 2)
  from.setHours(0, 0, 0, 0)
  const end = new Date(now)
  end.setHours(23, 59, 59, 0)
  return { fromDate: toLocalInput(from), endDate: toLocalInput(end) }
}
const initialRange = makeDefaultRange()
const query = ref<QueryState>({
  qcItemNos: [], reportType: 'MONTH', ...initialRange, fromEnabled: false, endEnabled: false,
  lotStates: ['0', '1', '2'], lotResults: ['0', '1'], calRule: 0,
  metrics: ['CP', 'CPK', 'PP', 'PPK', 'CA', 'Mean', 'EstSd', 'Sd'], factorValues: {},
})
const appliedQuery = ref<QueryState | null>(null)
const searched = ref(false)
const notice = ref('選取 Variable QC Item 並設定條件後按 Query；此頁只使用 DEV 假資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
const chartMetric = ref<MetricCode>('CPK')
const chartRowId = ref('')
const chartOpen = ref(false)

const itemOptions = computed(() => qcItems.filter((item) => item.issueState === 2 && item.chartControl === 1))
const selectedItems = computed(() => qcItems.filter((item) => query.value.qcItemNos.includes(item.no)))
const factorCriteria = computed<FactorCriterion[]>(() => selectedItems.value.flatMap((item) => factorDefinitions
  .filter((factor) => factor.qcItemNo === item.no)
  .map((factor) => ({ ...factor, key: `${factor.qcItemNo}:${factor.factorNo}` }))))
const appliedItemNos = computed(() => appliedQuery.value?.qcItemNos ?? query.value.qcItemNos)
const onlineFactorNos = computed(() => [...new Set(factorDefinitions.filter((factor) => appliedItemNos.value.includes(factor.qcItemNo) && factor.online === 'Y').map((factor) => factor.factorNo))])
const activeMetrics = computed(() => allMetrics.filter((metric) => (appliedQuery.value?.metrics ?? query.value.metrics).includes(metric.value)))
const reportIntervals = computed(() => appliedQuery.value ? buildIntervals(appliedQuery.value) : [])
const reportRows = computed<ReportRow[]>(() => {
  const applied = appliedQuery.value
  if (!applied) return []
  const intervals = reportIntervals.value
  return fixtureRows.filter((row) => applied.qcItemNos.includes(row.qcItemNo) && matchesFactors(row, applied)).map((row) => ({
    id: row.id, qcItemNo: row.qcItemNo, qcItemName: row.qcItemName, factors: row.factors, us: row.us, ls: row.ls,
    periods: intervals.map((interval) => calculatePeriod(row, interval, applied)),
  })).filter((row) => row.periods.some((period) => period.lotCount > 0))
})
const selectedChartRow = computed(() => reportRows.value.find((row) => row.id === chartRowId.value) ?? null)
const chartPoints = computed(() => selectedChartRow.value?.periods.map((period) => ({
  label: reportIntervals.value.find((interval) => interval.key === period.intervalKey)?.label ?? period.intervalKey,
  value: period.metrics?.[chartMetric.value] ?? null,
})) ?? [])
const resultColumnCount = computed(() => 5 + onlineFactorNos.value.length + Math.max(1, reportIntervals.value.length * Math.max(1, activeMetrics.value.length)))

function buildIntervals(applied: QueryState): ReportInterval[] {
  const allDates = fixtureRows.flatMap((row) => row.samples.map((sample) => new Date(sample.date).getTime()))
  const start = applied.reportType === 'ALL'
    ? (applied.fromEnabled ? new Date(applied.fromDate) : new Date(Math.min(...allDates)))
    : new Date(applied.fromDate)
  const end = applied.reportType === 'ALL'
    ? (applied.endEnabled ? new Date(applied.endDate) : new Date(Math.max(...allDates)))
    : new Date(applied.endDate)
  if (applied.reportType !== 'ALL') {
    start.setHours(0, 0, 0, 0)
    end.setHours(23, 59, 59, 999)
  }
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start >= end) return []
  if (applied.reportType === 'ALL') return [{ key: 'all', label: 'ALL', start, end }]
  const intervals: ReportInterval[] = []
  let cursor = new Date(start)
  while (cursor <= end && intervals.length < 37) {
    const intervalStart = new Date(cursor)
    let intervalEnd: Date
    if (applied.reportType === 'WEEK') {
      intervalEnd = new Date(cursor)
      intervalEnd.setDate(intervalEnd.getDate() + (6 - intervalEnd.getDay()))
    } else if (applied.reportType === 'MONTH') {
      intervalEnd = new Date(cursor.getFullYear(), cursor.getMonth() + 1, 0)
    } else {
      intervalEnd = new Date(cursor.getFullYear(), 11, 31)
    }
    intervalEnd.setHours(23, 59, 59, 999)
    if (intervalEnd > end) intervalEnd = new Date(end)
    intervals.push({ key: `${intervalStart.getTime()}`, label: intervalLabel(applied.reportType, intervalStart, intervalEnd), start: intervalStart, end: intervalEnd })
    cursor = new Date(intervalEnd)
    cursor.setDate(cursor.getDate() + 1)
    cursor.setHours(0, 0, 0, 0)
  }
  return intervals
}
function intervalLabel(type: ReportType, start: Date, end: Date): string {
  if (type === 'WEEK') return `${shortDate(start)}–${shortDate(end)}`
  if (type === 'MONTH') return `${start.getFullYear()}/${String(start.getMonth() + 1).padStart(2, '0')}`
  return `${start.getFullYear()}`
}
function shortDate(value: Date): string { return `${String(value.getMonth() + 1).padStart(2, '0')}/${String(value.getDate()).padStart(2, '0')}` }
function matchesFactors(row: CapabilityFixture, applied: QueryState): boolean {
  return factorDefinitions.filter((factor) => applied.qcItemNos.includes(factor.qcItemNo)).every((factor) => {
    const value = applied.factorValues[`${factor.qcItemNo}:${factor.factorNo}`]?.trim()
    if (!value) return true
    const requestedValues = value.split(',').map((part) => part.trim().toUpperCase()).filter(Boolean)
    return requestedValues.includes((row.factors[factor.factorNo] ?? '').toUpperCase()) || factor.qcItemNo !== row.qcItemNo
  })
}
function sampleLotCount(sample: CapabilitySample, applied: QueryState): number {
  return applied.lotStates.reduce((stateTotal, state) => stateTotal + applied.lotResults.reduce((resultTotal, result) => resultTotal + sample.counts[state][result], 0), 0)
}
function calculatePeriod(row: CapabilityFixture, interval: ReportInterval, applied: QueryState): PeriodResult {
  const matching = row.samples.filter((sample) => {
    const time = new Date(sample.date)
    return time >= interval.start && time <= interval.end && sampleLotCount(sample, applied) > 0
  })
  const lotCount = matching.reduce((total, sample) => total + sampleLotCount(sample, applied), 0)
  if (lotCount === 0) return { intervalKey: interval.key, lotCount, metrics: null }
  const values = {} as MetricValues
  for (const metric of allMetrics) {
    const weighted = matching.reduce((total, sample) => total + sample.metrics[metric.value] * sampleLotCount(sample, applied), 0)
    values[metric.value] = weighted / lotCount
  }
  return { intervalKey: interval.key, lotCount, metrics: values }
}
function runQuery(): void {
  if (query.value.qcItemNos.length === 0) return fail('請至少選擇一個 Issue 狀態的 Variable QC Item。')
  if (query.value.reportType !== 'ALL' || (query.value.fromEnabled && query.value.endEnabled)) {
    const from = new Date(query.value.fromDate)
    const to = new Date(query.value.endDate)
    if (Number.isNaN(from.getTime()) || Number.isNaN(to.getTime()) || from >= to) return fail('查詢起迄時間無效，From 必須早於 To。')
  }
  if (query.value.lotStates.length === 0) return fail('請至少選擇一種 Lot Report State。')
  if (query.value.lotResults.length === 0) return fail('請至少選擇一種 Lot Result。')
  if (query.value.metrics.length === 0) return fail('請至少選擇一項 Capability Type。')
  const candidate: QueryState = {
    ...query.value, qcItemNos: [...query.value.qcItemNos], lotStates: [...query.value.lotStates],
    lotResults: [...query.value.lotResults], metrics: [...query.value.metrics], factorValues: { ...query.value.factorValues },
  }
  if (buildIntervals(candidate).length > 36) return fail('DEV 預覽最多顯示 36 個週期，請縮短日期區間。')
  appliedQuery.value = candidate
  searched.value = true
  chartOpen.value = false
  chartRowId.value = ''
  if (!query.value.metrics.includes(chartMetric.value)) chartMetric.value = query.value.metrics[0] ?? 'CPK'
  noticeKind.value = 'success'
  notice.value = query.value.reportType !== 'ALL' && (!query.value.fromEnabled || !query.value.endEnabled)
    ? '查詢完成。原表單會提示日期未勾選；DEV 使用目前顯示的預設日期區間。'
    : `查詢完成：${reportRows.value.length} 組 Factor，${reportIntervals.value.length} 個報表區間（DEV 假資料）。`
}
function fail(message: string): void { notice.value = message; noticeKind.value = 'error' }
function updateFactorValue(key: string, event: Event): void { query.value.factorValues[key] = (event.target as HTMLInputElement).value }
function toggleAllItems(select: boolean): void { query.value.qcItemNos = select ? itemOptions.value.map((item) => item.no) : [] }
function formatMetric(value: number | null): string { return value === null ? '—' : value.toFixed(3) }
function formatDateTime(value: string): string { return value.replace('T', ' ') }
function formatDateObject(value: Date): string { return formatDateTime(toLocalInput(value)) }
function openChart(rowId: string): void { if (!rowId) return; chartRowId.value = rowId; chartOpen.value = true }
function closeChart(): void { chartOpen.value = false }
function chartBarWidth(value: number | null): string {
  if (value === null) return '0%'
  const scale = allMetrics.find((metric) => metric.value === chartMetric.value)?.scale ?? 2.5
  return `${Math.max(3, Math.min(100, value / scale * 100))}%`
}

const lotStateOptions: Array<{ value: LotState; label: string }> = [
  { value: '0', label: 'Normal' }, { value: '1', label: 'Excluded' }, { value: '2', label: 'Isolated' },
]
const lotResultOptions: Array<{ value: LotResult; label: string }> = [{ value: '0', label: 'Pass' }, { value: '1', label: 'Fail' }]
</script>

<template>
  <section v-if="props.developmentPreview" class="capability-page" aria-label="SPC 製程能力報表">
    <header class="page-head">
      <div><p class="eyebrow">SPC REPORT / SC301</p><h1>製程能力報表</h1><p class="subtitle">依 Variable QC Item、Lot 狀態、結果與要因檢視能力指標。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <section class="filter-panel panel">
      <div class="top-grid">
        <fieldset class="item-field"><legend>QC Item · Variable</legend>
          <div class="field-tools"><span>僅載入 ChartControl=1、IssueState=2</span><span><button type="button" @click="toggleAllItems(true)">全選</button><button type="button" @click="toggleAllItems(false)">清除</button></span></div>
          <select v-model="query.qcItemNos" multiple size="3" aria-label="多選 QC Item">
            <option v-for="item in itemOptions" :key="item.no" :value="item.no">{{ item.no }} — {{ item.name }}</option>
          </select>
        </fieldset>
        <fieldset class="date-field"><legend>Time Interval</legend>
          <label><input v-model="query.fromEnabled" type="checkbox"> From</label><input v-model="query.fromDate" type="datetime-local" step="1">
          <label><input v-model="query.endEnabled" type="checkbox"> End</label><input v-model="query.endDate" type="datetime-local" step="1">
          <small>週／月／年模式沿用表單日期值；ALL 可選擇是否套用端點。</small>
        </fieldset>
        <fieldset class="report-field"><legend>Report Type</legend>
          <label><input v-model="query.reportType" type="radio" value="WEEK"> WEEK</label>
          <label><input v-model="query.reportType" type="radio" value="MONTH"> MONTH</label>
          <label><input v-model="query.reportType" type="radio" value="YEAR"> YEAR</label>
          <label><input v-model="query.reportType" type="radio" value="ALL"> ALL</label>
        </fieldset>
      </div>

      <div class="option-grid">
        <fieldset><legend>Lot Report State</legend><label v-for="option in lotStateOptions" :key="option.value"><input v-model="query.lotStates" type="checkbox" :value="option.value"> {{ option.label }}</label></fieldset>
        <fieldset><legend>Lot Result</legend><label v-for="option in lotResultOptions" :key="option.value"><input v-model="query.lotResults" type="checkbox" :value="option.value"> {{ option.label }}</label></fieldset>
        <fieldset class="cal-rule"><legend>Chart Calculate Principle</legend><label><input v-model="query.calRule" type="radio" :value="0"> Cumulative Sample Size ≥ 25 Only</label><label><input v-model="query.calRule" type="radio" :value="1"> ALL</label></fieldset>
        <fieldset class="metrics-field"><legend>Capability Type</legend><label v-for="metric in allMetrics" :key="metric.value"><input v-model="query.metrics" type="checkbox" :value="metric.value"> {{ metric.label }}</label></fieldset>
      </div>

      <fieldset class="factor-panel"><legend>Collect Factor</legend>
        <p v-if="factorCriteria.length === 0" class="empty-hint">選取 QC Item 後載入要因條件。</p>
        <div v-else class="factor-table-wrap"><table class="factor-table"><thead><tr><th>QC Item</th><th>FactorNo</th><th>Factor</th><th>Value</th><th>Chart column</th></tr></thead><tbody>
          <tr v-for="criterion in factorCriteria" :key="criterion.key"><td>{{ criterion.qcItemNo }}</td><td>{{ criterion.factorNo }}</td><td>{{ criterion.label }}</td><td><input :value="query.factorValues[criterion.key] ?? ''" :list="`factor-options-${criterion.key}`" placeholder="全部；多值以逗號分隔" @input="updateFactorValue(criterion.key, $event)"><datalist :id="`factor-options-${criterion.key}`"><option v-for="option in criterion.options" :key="option" :value="option"></option></datalist></td><td>{{ criterion.online }}</td></tr>
        </tbody></table><small>只有 FactorOnline=Y 的欄位會顯示在報表；此表中的值與 MES Parameter lookup 都是 DEV 假資料。</small></div>
      </fieldset>
      <footer class="filter-actions"><span>預設 MONTH、最近兩個月、全部 Lot 狀態／結果／能力指標。</span><button class="button primary" type="button" @click="runQuery">Query</button></footer>
    </section>

    <section class="result-panel panel">
      <nav class="result-toolbar"><div><strong>Capability Result</strong><span v-if="searched">{{ reportRows.length }} 組 · {{ reportIntervals.length }} 個區間</span></div><button class="button" type="button" :disabled="!chartRowId" @click="openChart(chartRowId)">Chart</button></nav>
      <p v-if="!searched" class="empty-state">選擇一個以上 QC Item 後按 Query 顯示能力報表。</p>
      <div v-else class="table-scroll"><table class="result-table">
        <thead><tr><th rowspan="2">QCItemNo</th><th rowspan="2">QCItemName</th><th v-for="factorNo in onlineFactorNos" :key="`h-${factorNo}`" rowspan="2">{{ factorNo }}</th><th rowspan="2">US</th><th rowspan="2">LS</th><th rowspan="2">LotCount</th><th v-for="interval in reportIntervals" :key="interval.key" :colspan="Math.max(1, activeMetrics.length)">{{ interval.label }}<small>{{ formatDateObject(interval.start) }} – {{ formatDateObject(interval.end) }}</small></th></tr>
          <tr><template v-for="interval in reportIntervals" :key="`m-${interval.key}`"><th v-for="metric in activeMetrics" :key="`${interval.key}-${metric.value}`">{{ metric.label }}</th></template></tr></thead>
        <tbody><tr v-for="row in reportRows" :key="row.id" :class="{ selected: chartRowId === row.id }" @click="chartRowId = row.id"><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td><td v-for="factorNo in onlineFactorNos" :key="`${row.id}-${factorNo}`">{{ row.factors[factorNo] ?? '—' }}</td><td>{{ row.us }}</td><td>{{ row.ls }}</td><td class="number">{{ row.periods.reduce((total, period) => total + period.lotCount, 0) }}</td><template v-for="period in row.periods" :key="period.intervalKey"><td v-for="metric in activeMetrics" :key="`${period.intervalKey}-${metric.value}`" class="number">{{ formatMetric(period.metrics?.[metric.value] ?? null) }}</td></template></tr>
          <tr v-if="reportRows.length === 0"><td class="empty-cell" :colspan="resultColumnCount">沒有符合條件的 DEV 樣本。</td></tr>
        </tbody>
      </table></div>
      <footer v-if="searched" class="result-foot">CORE 示意：週區間依星期日到星期六切分；ALL 顯示單一整體區間。各列需選取後按 Chart 查看區間趨勢。</footer>
    </section>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>

    <div v-if="chartOpen && selectedChartRow" class="modal-backdrop" @click.self="closeChart">
      <section class="chart-dialog" role="dialog" aria-modal="true" :aria-label="`${selectedChartRow.qcItemNo} 能力趨勢`">
        <header><div><p class="eyebrow">DEV TREND / {{ selectedChartRow.qcItemNo }}</p><h2>{{ selectedChartRow.qcItemName }} 能力趨勢</h2><p>{{ onlineFactorNos.map((factorNo) => `${factorNo}: ${selectedChartRow?.factors[factorNo] ?? '—'}`).join(' · ') }}</p></div><button class="button" type="button" @click="closeChart">Close (X)</button></header>
        <label class="chart-metric">Chart Metric <select v-model="chartMetric"><option v-for="metric in activeMetrics" :key="metric.value" :value="metric.value">{{ metric.label }}</option></select></label>
        <div class="chart-bars"><div v-for="(point, index) in chartPoints" :key="`${point.label}-${index}`" class="chart-point"><div class="bar-value">{{ formatMetric(point.value) }}</div><div class="bar-track"><span :style="{ width: chartBarWidth(point.value) }"></span></div><div class="bar-label">{{ point.label }}</div></div><p v-if="chartPoints.length === 0" class="empty-hint">此區間沒有資料。</p></div>
        <footer>此圖以假資料示範週／月／年或 ALL 區間指標，未重算正式製程能力公式。</footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.capability-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;box-sizing:border-box;background:#f5f4ef;color:#292820;font:12px/1.35 Arial,"Microsoft JhengHei",sans-serif;overflow:auto}.panel{border:1px solid #d0cec4;border-radius:4px;background:#fff;box-shadow:0 1px 2px #0000000a}.page-head{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:0;font-size:19px;font-weight:600}.eyebrow{margin:0 0 2px;color:#8b7952;font-size:10px;letter-spacing:.08em}.subtitle{margin:3px 0 0;color:#777366;font-size:11px}.button{min-height:27px;padding:4px 12px;border:1px solid #3b3930;border-radius:3px;background:#fff;color:#3b3930;font:inherit;cursor:pointer}.button.primary{background:#3b3930;color:white}.button:disabled{opacity:.45;cursor:not-allowed}.filter-panel{padding:9px}.top-grid{display:grid;grid-template-columns:minmax(280px,1.15fr) minmax(280px,1fr) minmax(160px,.52fr);gap:8px}.option-grid{display:grid;grid-template-columns:1fr .7fr 1.55fr 2fr;gap:8px;margin-top:7px}.top-grid fieldset,.option-grid fieldset,.factor-panel{min-width:0;margin:0;padding:7px;border:1px solid #dedbd1;border-radius:3px}.top-grid legend,.option-grid legend,.factor-panel legend{padding:0 4px;color:#686458;font-size:11px;font-weight:600}.item-field select{width:100%;min-height:62px;border:1px solid #c9c6bb;border-radius:2px;background:white;font:inherit}.field-tools{display:flex;justify-content:space-between;gap:4px;margin-bottom:4px;color:#858174;font-size:10px}.field-tools button{border:0;background:transparent;color:#6d5b26;text-decoration:underline;font:inherit;cursor:pointer}.date-field{display:grid;grid-template-columns:54px 1fr;align-items:center;gap:5px}.date-field legend{grid-column:1/-1}.date-field input[type="datetime-local"]{min-width:0;width:100%;height:25px;box-sizing:border-box;border:1px solid #cbc8bd;border-radius:2px;font:inherit}.date-field small{grid-column:1/-1;color:#827e71;font-size:10px}.report-field{display:flex;flex-wrap:wrap;align-content:flex-start;gap:7px 12px}.report-field legend{width:100%}.report-field label,.option-grid fieldset>label{display:flex;align-items:center;gap:4px;white-space:nowrap}.option-grid fieldset{display:flex;align-content:flex-start;align-items:center;flex-wrap:wrap;gap:5px 10px}.option-grid fieldset legend{width:100%}.cal-rule label{white-space:normal!important}.metrics-field{gap:5px 9px!important}.factor-panel{margin-top:7px}.empty-hint{margin:4px;color:#888}.factor-table-wrap{overflow:auto}.factor-table,.result-table{width:100%;border-collapse:collapse}.factor-table th,.factor-table td{padding:4px 6px;border-bottom:1px solid #eceae4;text-align:left}.factor-table th{background:#f4f2eb;color:#625e52;font-weight:600}.factor-table input{width:100%;min-width:130px;height:24px;box-sizing:border-box;border:1px solid #cbc8bd;font:inherit}.factor-table-wrap small{display:block;margin-top:4px;color:#827e71;font-size:10px}.filter-actions{display:flex;justify-content:space-between;align-items:center;gap:8px;margin-top:7px;color:#777366;font-size:10px}.result-panel{display:flex;flex-direction:column;min-height:180px;flex:1;overflow:hidden}.result-toolbar{display:flex;justify-content:space-between;align-items:center;padding:7px 9px;border-bottom:1px solid #e6e3da}.result-toolbar div{display:flex;align-items:center;gap:10px}.result-toolbar span{color:#7b7669;font-size:10px}.table-scroll{flex:1;overflow:auto}.result-table{font-size:11px;white-space:nowrap}.result-table th,.result-table td{padding:5px 7px;border-right:1px solid #eceae4;border-bottom:1px solid #e7e4dc;text-align:left;vertical-align:middle}.result-table thead th{position:sticky;top:0;z-index:1;background:#f1efe8;color:#5f5b50;font-weight:600}.result-table thead th small{display:block;margin-top:2px;color:#858174;font-size:9px;font-weight:400}.result-table tbody tr{cursor:pointer}.result-table tbody tr:hover{background:#fffbed}.result-table tbody tr.selected{background:#fff5cf}.number{text-align:right!important;font-variant-numeric:tabular-nums}.empty-state,.empty-cell{text-align:center!important;color:#888;padding:24px!important}.result-foot{padding:6px 9px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.notice{margin:0;padding:7px 9px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b;font-size:11px}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:8000;display:grid;place-items:center;padding:20px;background:#17171080}.chart-dialog{width:min(860px,96vw);max-height:88vh;overflow:auto;padding:14px;border:1px solid #bbb7aa;border-radius:5px;background:#fff;box-shadow:0 12px 40px #0004}.chart-dialog header{display:flex;justify-content:space-between;gap:12px}.chart-dialog h2{margin:0;font-size:18px}.chart-dialog header p:last-child{margin:4px 0;color:#777366}.chart-metric{display:flex;align-items:center;gap:8px;margin:12px 0}.chart-metric select{height:28px;border:1px solid #cbc8bd;background:white;font:inherit}.chart-bars{display:flex;align-items:flex-end;gap:12px;min-height:180px;padding:12px 8px;border:1px solid #ebe8df;background:repeating-linear-gradient(to bottom,#fff 0,#fff 44px,#eceae4 45px)}.chart-point{display:flex;flex:1;min-width:45px;flex-direction:column;justify-content:flex-end;gap:5px}.bar-value{text-align:center;color:#4f4b3f;font-size:10px}.bar-track{height:110px;display:flex;align-items:flex-end;border-bottom:1px solid #969184}.bar-track span{display:block;min-height:3px;background:#69836b;border-radius:3px 3px 0 0}.bar-label{text-align:center;color:#777366;font-size:10px}.chart-dialog footer{margin-top:10px;color:#858174;font-size:10px}
@media(max-width:1050px){.top-grid{grid-template-columns:1fr 1fr}.option-grid{grid-template-columns:1fr 1fr}.report-field{grid-column:1/-1}}
@media(max-width:680px){.top-grid,.option-grid{grid-template-columns:1fr}.filter-actions{align-items:flex-start;flex-direction:column}}
</style>

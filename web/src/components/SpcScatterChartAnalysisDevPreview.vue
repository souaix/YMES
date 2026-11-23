<script setup lang="ts">
import { computed, ref } from 'vue'

type ParameterType = 0 | 1 | 2
type LotState = 0 | 1 | 2
type LotResult = 0 | 1
type FactorDefinition = { factorNo: string; factorName: string; parameterType: ParameterType; options: string[] }
type FactorState = FactorDefinition & { factorValue: string }
type QCItem = { qcItemNo: string; qcItemName: string; unit: string; issueState: 2 | -1; base: number; sigma: number; factors: FactorDefinition[] }
type Measurement = { key: string; lotNo: string; serialNo: number; spcSerial: string; createDate: string; createdAt: number; lotState: LotState; lotResult: LotResult; factorValues: Record<string, string>; value: number }
type ScatterPoint = { key: string; lotNo: string; serialNo: number; spcSerial1: string; spcSerial2: string; createDate: string; lotState: LotState; lotResult: LotResult; x: number; y: number }
type PickerSlot = 1 | 2

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const itemFixtures: QCItem[] = [
  { qcItemNo: 'QC-V-4101', qcItemName: 'Mold Cure Hardness', unit: 'Shore A', issueState: 2, base: 82.4, sigma: 3.8, factors: [
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 0, options: ['MOLD-05', 'MOLD-07', 'MOLD-09'] },
    { factorNo: 'RESIN_LOT', factorName: '樹脂批號', parameterType: 1, options: ['RES-26K-21', 'RES-26K-25', 'RES-26K-31'] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 2, options: [] },
    { factorNo: 'CURE_PROFILE', factorName: '固化條件', parameterType: 1, options: ['PROFILE-A', 'PROFILE-B', 'PROFILE-C'] },
  ] },
  { qcItemNo: 'QC-V-4102', qcItemName: 'Package Warpage', unit: 'μm', issueState: 2, base: 118.6, sigma: 12.2, factors: [
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 0, options: ['MOLD-05', 'MOLD-07', 'MOLD-09'] },
    { factorNo: 'RESIN_LOT', factorName: '樹脂批號', parameterType: 1, options: ['RES-26K-21', 'RES-26K-25', 'RES-26K-31'] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 2, options: [] },
    { factorNo: 'MOLD_PRESSURE', factorName: '合模壓力', parameterType: 0, options: ['PRESS-08', 'PRESS-10'] },
  ] },
  { qcItemNo: 'QC-V-4103', qcItemName: 'Resin Viscosity', unit: 'Pa·s', issueState: 2, base: 14.8, sigma: 1.6, factors: [
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 0, options: ['MOLD-05', 'MOLD-07', 'MOLD-09'] },
    { factorNo: 'RESIN_LOT', factorName: '樹脂批號', parameterType: 1, options: ['RES-26K-21', 'RES-26K-25', 'RES-26K-31'] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 2, options: [] },
    { factorNo: 'MIXER', factorName: '混料機', parameterType: 0, options: ['MIX-02', 'MIX-04'] },
  ] },
  { qcItemNo: 'QC-V-4199', qcItemName: 'Lead Coplanarity', unit: 'mm', issueState: -1, base: 0.412, sigma: 0.026, factors: [
    { factorNo: 'MACHINE', factorName: '量測機台', parameterType: 0, options: ['MOLD-05', 'MOLD-07'] },
    { factorNo: 'RESIN_LOT', factorName: '樹脂批號', parameterType: 1, options: ['RES-26K-21', 'RES-26K-25'] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 2, options: [] },
  ] },
]

const pickerSearch = ref('')
const pickerOpen = ref(false)
const pickerSlot = ref<PickerSlot>(1)
const itemOne = ref<QCItem | null>(null)
const itemTwo = ref<QCItem | null>(null)
const factors = ref<FactorState[]>([])
const fromDateEnabled = ref(false)
const endDateEnabled = ref(false)
const fromDate = ref('')
const endDate = ref('')
const lotStates = ref<LotState[]>([0, 1, 2])
const lotResults = ref<LotResult[]>([0, 1])
const queried = ref(false)
const selectedPointKey = ref('')
const notice = ref('請選擇兩個不同的 QC Item，再設定至少一個共同收集要因。')

function dateValue(offsetDays: number): string {
  const date = new Date()
  date.setDate(date.getDate() + offsetDays)
  return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0')
}
fromDate.value = dateValue(-60)
endDate.value = dateValue(0)

const pickerItems = computed(() => itemFixtures.filter((item) => item.issueState === 2 || item.issueState === -1)
  .filter((item) => !((pickerSlot.value === 1 ? itemTwo.value : itemOne.value)?.qcItemNo === item.qcItemNo))
  .filter((item) => item.qcItemNo.toLowerCase().includes(pickerSearch.value.trim().toLowerCase()) || item.qcItemName.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))

function syncCommonFactors(): void {
  if (!itemOne.value || !itemTwo.value) { factors.value = []; return }
  const existing = new Map<string, string>()
  factors.value.forEach((factor) => existing.set(factor.factorNo, factor.factorValue))
  factors.value = itemOne.value.factors.flatMap((left) => {
    const right = itemTwo.value?.factors.find((factor) => factor.factorNo === left.factorNo)
    if (!right) return []
    return [{ ...left, factorValue: existing.get(left.factorNo) || '' }]
  })
}
function openPicker(slot: PickerSlot): void { pickerSlot.value = slot; pickerSearch.value = ''; pickerOpen.value = true }
function chooseItem(item: QCItem): void {
  if (pickerSlot.value === 1) itemOne.value = item
  else itemTwo.value = item
  syncCommonFactors()
  queried.value = false
  selectedPointKey.value = ''
  pickerOpen.value = false
  notice.value = '已載入兩個 QC Item 的共同要因；只有共同 FactorNo 可作為比較條件。'
}

const allMeasurements = computed(() => {
  const items = [itemOne.value, itemTwo.value].filter((item): item is QCItem => item !== null)
  const byItem: Record<string, Measurement[]> = {}
  const count = 48
  const now = Date.now()
  for (const item of items) {
    const rows: Measurement[] = []
    for (let lot = 0; lot < count; lot += 1) {
      const timestamp = now - (count - lot) * 2.6 * 86_400_000
      const lotState: LotState = lot % 17 === 8 ? 2 : lot % 11 === 6 ? 1 : 0
      const lotResult: LotResult = lot % 13 === 9 ? 1 : 0
      const factorValues: Record<string, string> = {
        MACHINE: ['MOLD-05', 'MOLD-07', 'MOLD-09'][lot % 3],
        RESIN_LOT: ['RES-26K-21', 'RES-26K-25', 'RES-26K-31'][Math.floor(lot / 3) % 3],
        OPERATOR: ['USR-11019', 'USR-11023', 'USR-11007'][lot % 3],
        CURE_PROFILE: ['PROFILE-A', 'PROFILE-B', 'PROFILE-C'][lot % 3],
        MOLD_PRESSURE: ['PRESS-08', 'PRESS-10'][lot % 2],
        MIXER: ['MIX-02', 'MIX-04'][lot % 2],
      }
      for (let serial = 1; serial <= 3; serial += 1) {
        const index = lot * 3 + serial
        const shared = Math.sin(index * 0.31) * 0.62 + Math.cos(index * 0.13) * 0.38 + (lot % 5 - 2) * 0.16
        const itemShift = item.qcItemNo === 'QC-V-4102' ? Math.sin(index * 0.79) * 0.3 : item.qcItemNo === 'QC-V-4103' ? Math.cos(index * 0.47) * 0.21 : 0
        const factorShift = (lot % 3 - 1) * item.sigma * 0.14
        const value = item.base + factorShift + item.sigma * (shared + itemShift + (serial - 2) * 0.2)
        const created = new Date(timestamp + serial * 2 * 60 * 60_000)
        rows.push({
          key: item.qcItemNo + ':' + 'LOT-26K-' + String((lot * 19 + 231) % 999).padStart(3, '0') + ':' + serial,
          lotNo: 'LOT-26K-' + String((lot * 19 + 231) % 999).padStart(3, '0'),
          serialNo: serial,
          spcSerial: 'SPC-SC-' + item.qcItemNo.slice(-2) + '-' + String(lot + 1).padStart(3, '0'),
          createDate: created.toLocaleString('zh-TW', { hour12: false }),
          createdAt: created.getTime(),
          lotState,
          lotResult,
          factorValues: { ...factorValues },
          value,
        })
      }
    }
    byItem[item.qcItemNo] = rows
  }
  return byItem
})

const pairedPoints = computed<ScatterPoint[]>(() => {
  if (!queried.value || !itemOne.value || !itemTwo.value) return []
  const selectedFactorValues: Record<string, string> = {}
  factors.value.forEach((factor) => { selectedFactorValues[factor.factorNo] = factor.factorValue.trim().toUpperCase() })
  const matchesFilters = (row: Measurement): boolean => {
    if (fromDateEnabled.value && fromDate.value && row.createdAt < new Date(fromDate.value + 'T00:00:00').getTime()) return false
    if (endDateEnabled.value && endDate.value && row.createdAt > new Date(endDate.value + 'T23:59:59').getTime()) return false
    if (!lotStates.value.includes(row.lotState) || !lotResults.value.includes(row.lotResult)) return false
    return factors.value.every((factor) => {
      const selected = selectedFactorValues[factor.factorNo] || ''
      return selected === '' || selected === (row.factorValues[factor.factorNo] || '').toUpperCase()
    })
  }
  const first = (allMeasurements.value[itemOne.value.qcItemNo] || []).filter(matchesFilters)
  const second = new Map<string, Measurement>()
  const secondRows = (allMeasurements.value[itemTwo.value.qcItemNo] || []).filter(matchesFilters)
  secondRows.forEach((row) => second.set(row.lotNo + ':' + row.serialNo, row))
  return first.flatMap((left) => {
    const right = second.get(left.lotNo + ':' + left.serialNo)
    return right ? [{ key: left.lotNo + ':' + left.serialNo, lotNo: left.lotNo, serialNo: left.serialNo, spcSerial1: left.spcSerial, spcSerial2: right.spcSerial, createDate: left.createDate, lotState: left.lotState, lotResult: left.lotResult, x: left.value, y: right.value }] : []
  })
})

const statistics = computed(() => {
  const points = pairedPoints.value
  const count = points.length
  if (!count) return { count: 0, correlation: null as number | null, slope: null as number | null, meanX: null as number | null, meanY: null as number | null, intercept: null as number | null, varianceX: null as number | null, varianceY: null as number | null }
  const sumX = points.reduce((sum, point) => sum + point.x, 0)
  const sumY = points.reduce((sum, point) => sum + point.y, 0)
  const sumX2 = points.reduce((sum, point) => sum + point.x * point.x, 0)
  const sumY2 = points.reduce((sum, point) => sum + point.y * point.y, 0)
  const sumXY = points.reduce((sum, point) => sum + point.x * point.y, 0)
  const meanX = sumX / count
  const meanY = sumY / count
  const denominator = Math.sqrt((sumX2 - sumX * sumX / count) * (sumY2 - sumY * sumY / count))
  const correlation = denominator > 0 ? (sumXY - sumX * sumY / count) / denominator : 0
  const sumXM = points.reduce((sum, point) => sum + (Math.round(point.x) - meanX) ** 2, 0)
  const sumYM = points.reduce((sum, point) => sum + (Math.round(point.y) - meanY) ** 2, 0)
  const stdDevX = Math.sqrt(sumXM / count)
  const stdDevY = Math.sqrt(sumYM / count)
  const slope = stdDevX > 0 ? correlation * stdDevY / stdDevX : 0
  return { count, correlation, slope, meanX, meanY, intercept: meanY - slope * meanX, varianceX: count > 1 ? sumXM / (count - 1) : 0, varianceY: count > 1 ? sumYM / (count - 1) : 0 }
})

const selectedPoint = computed(() => pairedPoints.value.find((point) => point.key === selectedPointKey.value) || null)
const xScale = computed(() => {
  const values = pairedPoints.value.map((point) => point.x)
  if (!values.length) return { min: 0, max: 1 }
  const min = Math.min(...values)
  const max = Math.max(...values)
  const pad = Math.max((max - min) * 0.1, Math.abs(min) * 0.015, 0.01)
  return { min: min - pad, max: max + pad }
})
const yScale = computed(() => {
  const values = pairedPoints.value.map((point) => point.y)
  if (!values.length) return { min: 0, max: 1 }
  const min = Math.min(...values)
  const max = Math.max(...values)
  const pad = Math.max((max - min) * 0.1, Math.abs(min) * 0.015, 0.01)
  return { min: min - pad, max: max + pad }
})
function xPosition(value: number): number { return 72 + (value - xScale.value.min) / Math.max(xScale.value.max - xScale.value.min, 0.000001) * 742 }
function yPosition(value: number): number { return 330 - (value - yScale.value.min) / Math.max(yScale.value.max - yScale.value.min, 0.000001) * 278 }
const regressionPoints = computed(() => statistics.value.slope === null || statistics.value.intercept === null || !pairedPoints.value.length ? '' : `${xPosition(xScale.value.min)},${yPosition(statistics.value.intercept + statistics.value.slope * xScale.value.min)} ${xPosition(xScale.value.max)},${yPosition(statistics.value.intercept + statistics.value.slope * xScale.value.max)}`)
function formatValue(value: number | null | undefined): string { return value === null || value === undefined || !Number.isFinite(value) ? '—' : Number(value.toFixed(4)).toString() }
function itemLabel(item: QCItem | null): string { return item ? item.qcItemNo + ' · ' + item.qcItemName : '未選擇' }
function stateLabel(state: LotState): string { return state === 0 ? 'Normal' : state === 1 ? 'Excluded' : 'Isolated' }

function runQuery(): void {
  if (!itemOne.value || !itemTwo.value) { notice.value = '請先選擇兩個 QC Item。'; return }
  if (itemOne.value.qcItemNo === itemTwo.value.qcItemNo) { notice.value = '兩個 QC Item 必須不同。'; return }
  if (fromDateEnabled.value && endDateEnabled.value && (!fromDate.value || !endDate.value || fromDate.value >= endDate.value)) { notice.value = '日期起訖必須完整，且起日必須早於迄日。'; return }
  if (!lotStates.value.length) { notice.value = 'Lot Report State 至少選一種。'; return }
  if (!lotResults.value.length) { notice.value = 'Lot Result 至少選一種。'; return }
  if (!factors.value.length || !factors.value.some((factor) => factor.factorValue.trim())) { notice.value = '至少選擇或輸入一個共同 Collect Factor。'; return }
  queried.value = true
  selectedPointKey.value = ''
  notice.value = fromDateEnabled.value !== endDateEnabled.value ? 'DEV 假資料已依目前日期端點查詢；來源畫面允許僅指定單側日期。' : 'DEV 假資料已完成兩組 QC Item 的批次與 Serial 配對。'
  if (!pairedPoints.value.length) notice.value = '條件未找到可配對的 LotNo + SerialNo；請調整要因或日期。'
}
function resetCriteria(): void {
  factors.value.forEach((factor) => { factor.factorValue = '' })
  fromDateEnabled.value = false
  endDateEnabled.value = false
  fromDate.value = dateValue(-60)
  endDate.value = dateValue(0)
  lotStates.value = [0, 1, 2]
  lotResults.value = [0, 1]
  queried.value = false
  selectedPointKey.value = ''
  notice.value = '查詢條件已重設；請重新設定至少一個共同要因。'
}
function saveGraph(): void { notice.value = 'DEV 已模擬 Save Graph；沒有建立圖檔。' }
function exportRows(): void { notice.value = 'DEV 已模擬 Export；沒有建立 Excel 檔。' }
</script>

<template>
  <section v-if="props.developmentPreview" class="scatter-page" aria-label="散佈圖分析">
    <header class="page-head"><div><p class="eyebrow">SPC / SC208</p><h1>散佈圖分析</h1><p class="subtitle">選擇兩個 QC Item，比較共同批次與量測序號的計量關係。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>

    <section class="criteria panel">
      <div class="item-pair">
        <label>QC Item 1<input :value="itemLabel(itemOne)" readonly placeholder="請選擇第一個品管項目"><button class="button get-button" @click="openPicker(1)">Get 1</button></label>
        <span class="pair-mark">X ↔ Y</span>
        <label>QC Item 2<input :value="itemLabel(itemTwo)" readonly placeholder="請選擇第二個品管項目"><button class="button get-button" @click="openPicker(2)">Get 2</button></label>
      </div>

      <div class="criteria-row">
        <fieldset><legend>Time Interval</legend><label class="date-check"><input v-model="fromDateEnabled" type="checkbox"> From Query Date</label><input v-model="fromDate" type="date" :disabled="!fromDateEnabled"><label class="date-check"><input v-model="endDateEnabled" type="checkbox"> End Query Date</label><input v-model="endDate" type="date" :disabled="!endDateEnabled"></fieldset>
        <fieldset><legend>Lot Report State</legend><label><input v-model="lotStates" type="checkbox" :value="0"> Normal</label><label><input v-model="lotStates" type="checkbox" :value="1"> Excluded</label><label><input v-model="lotStates" type="checkbox" :value="2"> Isolated</label></fieldset>
        <fieldset><legend>Lot Result</legend><label><input v-model="lotResults" type="checkbox" :value="0"> Pass</label><label><input v-model="lotResults" type="checkbox" :value="1"> Fail</label></fieldset>
        <button class="button reset-button" @click="resetCriteria">Reset Criteria</button>
      </div>

      <div class="factor-header"><div><h2>Collect Factors</h2><span>{{ factors.length }} 個兩個 QC Item 共用的 Factor；至少填一項才能查詢</span></div></div>
      <div class="factor-scroll"><table><thead><tr><th>Factor No</th><th>要因</th><th>Lookup Type</th><th>Factor Value</th><th>關聯</th></tr></thead><tbody>
        <tr v-for="factor in factors" :key="factor.factorNo"><td class="serial">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType === 2 ? 'Free input' : factor.parameterType === 1 ? 'Function list' : 'Table list' }}</td><td><input v-if="factor.parameterType === 2" v-model="factor.factorValue" class="factor-input" maxlength="80" placeholder="輸入要因值；查詢時轉大寫"><select v-else v-model="factor.factorValue" class="factor-select"><option value="">未指定</option><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select></td><td><span v-if="factor.factorValue">{{ factor.factorValue }}</span><span v-else class="muted">不篩選</span></td></tr>
        <tr v-if="!factors.length"><td colspan="5" class="empty">選擇兩個 QC Item 後顯示共同 FactorNo；來源只允許設定共同要因。</td></tr>
      </tbody></table></div>
      <div class="query-row"><span class="option-note">日期預設未啟用；Lot State／Result 預設全選。資料為本機 DEV 假資料。</span><button class="button primary" :disabled="!itemOne || !itemTwo" @click="runQuery">Query Scatter Chart</button></div>
    </section>

    <section class="query-panel panel"><p class="notice" role="status">{{ notice }}</p></section>

    <section v-if="queried" class="analysis panel">
      <header class="analysis-head"><div><p class="eyebrow">SC208 · PAIRED MEASUREMENTS</p><h2>{{ itemLabel(itemOne) }} <span>vs</span> {{ itemLabel(itemTwo) }}</h2><p>配對鍵：LotNo + SerialNo · X 軸 {{ itemOne?.qcItemNo }} · Y 軸 {{ itemTwo?.qcItemNo }}</p></div><div class="analysis-actions"><button class="button" @click="saveGraph">Save Graph</button><button class="button" @click="exportRows">Export</button></div></header>
      <div class="stats-row"><div><small>PAIRED SAMPLES</small><b>{{ statistics.count }}</b></div><div><small>CORRELATION</small><b>{{ formatValue(statistics.correlation) }}</b></div><div><small>SLOPE</small><b>{{ formatValue(statistics.slope) }}</b></div><div><small>INTERCEPT</small><b>{{ formatValue(statistics.intercept) }}</b></div><div><small>MEAN X</small><b>{{ formatValue(statistics.meanX) }} {{ itemOne?.unit }}</b></div><div><small>MEAN Y</small><b>{{ formatValue(statistics.meanY) }} {{ itemTwo?.unit }}</b></div><div><small>VARIANCE X / Y</small><b>{{ formatValue(statistics.varianceX) }} / {{ formatValue(statistics.varianceY) }}</b></div></div>
      <div class="chart-panel"><div class="chart-title"><div><h3>Scatter Chart</h3><span>點選資料點可在明細表定位。相關線為 DEV 示意。</span></div><span>{{ statistics.count }} paired points</span></div>
        <div v-if="pairedPoints.length" class="chart-wrap"><svg viewBox="0 0 860 390" preserveAspectRatio="none" role="img" aria-label="QC Item 散佈圖 DEV 假資料"><line v-for="line in 5" :key="line" x1="72" :y1="52 + line * 55" x2="814" :y2="52 + line * 55" class="grid-line"/><line x1="72" y1="330" x2="814" y2="330" class="axis-line"/><line x1="72" y1="52" x2="72" y2="330" class="axis-line"/><text x="74" y="30" class="axis-label">{{ itemTwo?.qcItemNo }} · {{ itemTwo?.unit }}</text><text x="660" y="372" class="axis-label">{{ itemOne?.qcItemNo }} · {{ itemOne?.unit }} →</text><polyline :points="regressionPoints" class="regression-line"/><g v-for="point in pairedPoints" :key="point.key" class="point-hit" @click="selectedPointKey = point.key"><circle :cx="xPosition(point.x)" :cy="yPosition(point.y)" :r="selectedPointKey === point.key ? 7 : 4.2" :class="{ selected: selectedPointKey === point.key, fail: point.lotResult === 1, excluded: point.lotState !== 0 }"/><title>{{ point.lotNo }} · Serial {{ point.serialNo }} · X {{ formatValue(point.x) }} · Y {{ formatValue(point.y) }} · {{ stateLabel(point.lotState) }} · {{ point.lotResult === 0 ? 'Pass' : 'Fail' }}</title></g></svg></div>
        <p v-else class="empty chart-empty">沒有相同 LotNo + SerialNo 的配對量測資料。</p>
      </div>
      <div v-if="selectedPoint" class="selected-point"><b>Selected point</b><span>{{ selectedPoint.lotNo }} · Serial {{ selectedPoint.serialNo }}</span><span>X {{ formatValue(selectedPoint.x) }} {{ itemOne?.unit }}</span><span>Y {{ formatValue(selectedPoint.y) }} {{ itemTwo?.unit }}</span><span>{{ stateLabel(selectedPoint.lotState) }} / {{ selectedPoint.lotResult === 0 ? 'Pass' : 'Fail' }}</span><span>{{ selectedPoint.spcSerial1 }} / {{ selectedPoint.spcSerial2 }}</span></div>
      <div class="data-heading"><h3>Scatter Chart Data</h3><span>{{ pairedPoints.length }} rows · point click synchronizes selection</span></div>
      <div class="data-scroll"><table><thead><tr><th>Lot No</th><th>Serial No</th><th>{{ itemOne?.qcItemNo }} (X)</th><th>{{ itemTwo?.qcItemNo }} (Y)</th><th>Lot State</th><th>Lot Result</th><th>SPC Serial 1</th><th>SPC Serial 2</th><th>Create Date</th></tr></thead><tbody><tr v-for="point in pairedPoints" :key="point.key" :class="{ selected: selectedPointKey === point.key, fail: point.lotResult === 1 }" @click="selectedPointKey = point.key"><td class="serial">{{ point.lotNo }}</td><td class="number">{{ point.serialNo }}</td><td class="number">{{ formatValue(point.x) }} {{ itemOne?.unit }}</td><td class="number">{{ formatValue(point.y) }} {{ itemTwo?.unit }}</td><td>{{ stateLabel(point.lotState) }}</td><td>{{ point.lotResult === 0 ? 'Pass' : 'Fail' }}</td><td>{{ point.spcSerial1 }}</td><td>{{ point.spcSerial2 }}</td><td>{{ point.createDate }}</td></tr><tr v-if="!pairedPoints.length"><td colspan="9" class="empty">沒有符合條件的成對資料。</td></tr></tbody></table></div>
      <p class="disclaimer">統計與圖表為本機假資料；DEV 相關係數用 Pearson 公式，回歸線與 Variance 僅近似來源 ScatterChartLayer。Save Graph／Export 只顯示模擬提示。</p>
    </section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 QC Item"><header><div><p class="eyebrow">QC ITEM LOOKUP · SLOT {{ pickerSlot }}</p><h2>選擇 {{ pickerSlot === 1 ? 'X 軸' : 'Y 軸' }}品管項目</h2></div><button class="button" @click="pickerOpen = false">Close</button></header><div class="picker-filters"><label>Search<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><span>IssueState 2／-1；排除另一軸已選 QC Item</span></div><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" @click="chooseItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>{{ item.unit }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item。</p></div></section></div>
  </section>
</template>

<style scoped>
.scatter-page { --ink: #242923; --muted: #68716b; --line: #d4dbd5; --paper: #fff; --canvas: #f2f5f1; --green: #237a58; --soft-green: #e8f3ec; height: 100%; min-height: 0; overflow: auto; padding: 18px; box-sizing: border-box; background: var(--canvas); color: var(--ink); font: inherit; }
.page-head,.analysis-head,.chart-title,.factor-header,.query-row,.query-panel>header,.data-heading { display: flex; justify-content: space-between; align-items: center; gap: 14px; }
.page-head { margin-bottom: 14px; }.page-head h1,.analysis-head h2 { margin: 2px 0; font-size: 23px; }.page-head p,.analysis-head p { margin: 4px 0 0; color: var(--muted); }.eyebrow { margin: 0; color: var(--green); font-size: 11px; font-weight: 800; letter-spacing: .1em; }.subtitle { font-size: 13px; }
.panel { margin-bottom: 12px; border: 1px solid var(--line); border-radius: 6px; background: var(--paper); box-shadow: 0 1px 3px #28342b0b; }.criteria { padding: 14px; }.item-pair { display: grid; grid-template-columns: minmax(220px,1fr) 52px minmax(220px,1fr) 170px; gap: 12px; align-items: end; }.item-pair>label { position: relative; display: grid; gap: 5px; color: var(--muted); font-size: 12px; font-weight: 700; }.item-pair input,.item-pair select,.criteria-row input[type=date],.factor-select,.factor-input,.picker-filters input { min-height: 34px; border: 1px solid #c8d0ca; border-radius: 4px; padding: 5px 9px; box-sizing: border-box; background: white; color: var(--ink); font: inherit; }.item-pair input { padding-right: 76px; }.get-button { position: absolute; right: 4px; bottom: 4px; min-height: 27px!important; }.pair-mark { align-self: center; justify-self: center; color: var(--green); font-size: 13px; font-weight: 800; }.criteria-row { display: flex; align-items: stretch; gap: 12px; margin: 13px 0; }.criteria-row fieldset { display: flex; align-items: center; gap: 9px; min-width: 0; margin: 0; padding: 8px 10px; border: 1px solid var(--line); border-radius: 4px; }.criteria-row legend { padding: 0 5px; color: var(--muted); font-size: 11px; }.criteria-row label { display: inline-flex; align-items: center; gap: 4px; white-space: nowrap; font-size: 12px; }.criteria-row input[type=checkbox],.date-check input { accent-color: var(--green); }.criteria-row input[type=date] { min-height: 30px; max-width: 145px; padding: 3px 6px; }.criteria-row input:disabled { background: #f1f2f1; color: #a6aca7; }.date-check { font-size: 11px!important; }.reset-button { align-self: center; margin-left: auto; }
.factor-header { margin: 6px 0 8px; }.factor-header h2,.query-panel h2,.chart-title h3,.data-heading h3 { margin: 0; font-size: 15px; }.factor-header span,.chart-title span,.data-heading span { color: var(--muted); font-size: 11px; }.factor-scroll,.data-scroll { max-width: 100%; overflow: auto; border: 1px solid var(--line); border-radius: 4px; }table { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }th,td { border-bottom: 1px solid #e8ece8; padding: 7px 9px; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #f2f6f2; color: #59645c; font-size: 11px; }tbody tr:last-child td { border-bottom: 0; }.serial { color: #246a4e; font-family: Consolas,monospace; }.muted,.empty { color: #89928b; }.factor-input,.factor-select { width: min(310px,100%); min-height: 30px; }.query-row { margin-top: 11px; }.option-note { color: var(--muted); font-size: 11px; }.query-panel { padding: 10px 14px; }.notice { margin: 0; min-height: 18px; color: #536158; font-size: 12px; }
.analysis { padding: 14px; }.analysis-head { align-items: flex-start; margin-bottom: 14px; }.analysis-head h2 { font-size: 17px; }.analysis-head h2 span { color: var(--green); }.analysis-head p:last-child { font-size: 11px; }.analysis-actions { display: flex; gap: 7px; }.stats-row { display: grid; grid-template-columns: repeat(7,minmax(92px,1fr)); gap: 8px; margin-bottom: 12px; }.stats-row>div { min-width: 0; display: grid; gap: 4px; padding: 8px 10px; border: 1px solid var(--line); border-radius: 4px; background: #fbfcfb; }.stats-row small { color: var(--muted); font-size: 9px; font-weight: 800; letter-spacing: .04em; }.stats-row b { overflow: hidden; text-overflow: ellipsis; font-size: 14px; white-space: nowrap; }.chart-panel { padding: 10px; border: 1px solid var(--line); border-radius: 4px; }.chart-title { margin-bottom: 6px; }.chart-title span { display: block; margin-top: 3px; }.chart-title>span { white-space: nowrap; }.chart-wrap { height: 355px; min-width: 440px; overflow: hidden; }.chart-wrap svg { width: 100%; height: 100%; }.grid-line { stroke: #e8ece8; stroke-width: 1; }.axis-line { stroke: #78847b; stroke-width: 1.2; }.axis-label { fill: #58645b; font-size: 11px; }.regression-line { fill: none; stroke: #dd8832; stroke-width: 2.2; stroke-dasharray: 7 4; }.point-hit { cursor: pointer; }.point-hit circle { fill: #39896a; stroke: white; stroke-width: 1.4; opacity: .82; }.point-hit circle.fail { fill: #c65c48; }.point-hit circle.excluded { opacity: .38; }.point-hit circle.selected { stroke: #202920; stroke-width: 2.5; opacity: 1; }.chart-empty { min-height: 170px; display: grid; place-items: center; }.selected-point { display: flex; flex-wrap: wrap; gap: 8px 14px; margin: 9px 0; padding: 9px 11px; border-radius: 4px; background: var(--soft-green); color: #385b47; font-size: 11px; }.selected-point b { color: var(--green); }.data-heading { margin: 14px 0 7px; }.data-scroll { max-height: 330px; }.data-scroll tbody tr { cursor: pointer; }.data-scroll tbody tr:hover,.data-scroll tbody tr.selected { background: #e8f4ed; }.data-scroll tbody tr.fail:not(.selected) { background: #fff8f6; }.number { text-align: right; font-variant-numeric: tabular-nums; }.disclaimer { margin: 8px 0 0; color: var(--muted); font-size: 10px; }.button { min-height: 32px; padding: 5px 11px; border: 1px solid #bfc9c1; border-radius: 4px; background: #fff; color: #313d34; font: inherit; font-size: 12px; cursor: pointer; }.button:hover:not(:disabled) { border-color: var(--green); color: var(--green); }.button:disabled { cursor: not-allowed; opacity: .5; }.button.primary { border-color: var(--green); background: var(--green); color: white; }.modal-backdrop { position: fixed; inset: 0; z-index: 120; display: grid; place-items: center; padding: 20px; background: #1d2822a8; }.picker-dialog { width: min(780px,96vw); max-height: 84vh; display: grid; grid-template-rows: auto auto minmax(0,1fr); overflow: hidden; border-radius: 7px; background: white; box-shadow: 0 15px 55px #0004; }.picker-dialog>header { display: flex; justify-content: space-between; align-items: center; padding: 14px 16px; border-bottom: 1px solid var(--line); }.picker-dialog h2 { margin: 2px 0 0; font-size: 18px; }.picker-filters { display: flex; align-items: end; gap: 14px; padding: 12px 16px; border-bottom: 1px solid var(--line); }.picker-filters label { display: grid; flex: 1; gap: 4px; color: var(--muted); font-size: 11px; }.picker-filters span { padding-bottom: 8px; color: var(--muted); font-size: 11px; }.picker-list { overflow: auto; padding: 5px 10px 12px; }.picker-row { width: 100%; display: grid; grid-template-columns: 150px 1fr 110px 90px; gap: 10px; padding: 10px; border: 0; border-bottom: 1px solid #edf0ed; background: white; color: var(--ink); text-align: left; cursor: pointer; }.picker-row:hover { background: var(--soft-green); }.picker-row b { color: var(--green); font-family: Consolas,monospace; }.empty { padding: 12px; text-align: center; }
@media (max-width:1000px) { .item-pair { grid-template-columns: minmax(180px,1fr) 36px minmax(180px,1fr); }.item-pair>label:last-child { grid-column: 1 / -1; }.criteria-row { flex-wrap: wrap; }.stats-row { grid-template-columns: repeat(4,minmax(90px,1fr)); } }
@media (max-width:650px) { .scatter-page { padding: 10px; }.item-pair { grid-template-columns: 1fr; }.pair-mark { display: none; }.item-pair>label:last-child { grid-column: auto; }.criteria-row fieldset { flex-wrap: wrap; }.analysis-head { flex-direction: column; }.stats-row { grid-template-columns: repeat(2,minmax(100px,1fr)); }.picker-row { grid-template-columns: 1fr 1fr; }.picker-filters { align-items: stretch; flex-direction: column; }.chart-wrap { min-width: 640px; } }
</style>

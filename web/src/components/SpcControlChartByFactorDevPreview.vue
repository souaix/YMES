<script setup lang="ts">
import { computed, ref } from 'vue'

type ParameterType = 0 | 1 | 2
type ViewMode = 'chart' | 'data' | 'histogram'
type FactorFilter = { factorNo: string; factorName: string; parameterType: ParameterType; online: boolean; factorSpec: string; value: string; options: string[] }
type ChartGroup = { factorSerial: string; qcItemNo: string; qcItemName: string; isShortRun: boolean; keyNode: 'I' | 'C' | 'N'; firstLotDate: string; lastLotDate: string; lotQty: number; factors: Record<string, string>; chartType: 'X-R' | 'X-S' | 'X-Rm'; center: number; sigma: number; unit: string; sampleSize: number }
type Measure = { index: number; spcSerial: string; lotNo: string; serialNo: string; createdAt: number; createDate: string; value: number; excluded: boolean; erfNo: string; factorValues: Record<string, string> }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const factors = ref<FactorFilter[]>([
  { factorNo: 'EQUIPMENT', factorName: '設備', parameterType: 0, online: true, factorSpec: 'EquipmentNo', value: '', options: ['EQP-ETCH-03', 'EQP-ETCH-07', 'EQP-PLT-02', 'EQP-MOLD-05'] },
  { factorNo: 'LINE', factorName: '產線', parameterType: 0, online: true, factorSpec: 'LineNo', value: '', options: ['LN-A', 'LN-B', 'LN-C'] },
  { factorNo: 'PRODUCT', factorName: '產品', parameterType: 1, online: true, factorSpec: 'ProductNo', value: '', options: ['PRD-A100', 'PRD-A200', 'PRD-B100'] },
  { factorNo: 'MATERIAL', factorName: '材料批號', parameterType: 2, online: true, factorSpec: 'MaterialNo', value: '', options: [] },
  { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 0, online: false, factorSpec: 'UserNo', value: '', options: ['U11019', 'U11023', 'U11031'] },
])
const allGroups: ChartGroup[] = [
  { factorSerial: 'FS-2611-0182', qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', isShortRun: false, keyNode: 'I', firstLotDate: '2026/11/02 06:10:22', lastLotDate: '2026/11/23 08:42:17', lotQty: 28, factors: { EQUIPMENT: 'EQP-ETCH-03', LINE: 'LN-A', PRODUCT: 'PRD-A100', MATERIAL: 'MAT-26K-031', OPERATOR: 'U11019' }, chartType: 'X-R', center: 0.412, sigma: 0.028, unit: 'mm', sampleSize: 5 },
  { factorSerial: 'FS-2611-0181', qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', isShortRun: false, keyNode: 'C', firstLotDate: '2026/11/01 14:05:02', lastLotDate: '2026/11/22 05:53:31', lotQty: 22, factors: { EQUIPMENT: 'EQP-ETCH-07', LINE: 'LN-A', PRODUCT: 'PRD-A100', MATERIAL: 'MAT-26K-032', OPERATOR: 'U11023' }, chartType: 'X-R', center: 0.409, sigma: 0.025, unit: 'mm', sampleSize: 5 },
  { factorSerial: 'FS-2611-0179', qcItemNo: 'QC-V-1001', qcItemName: 'Lead Coplanarity', isShortRun: false, keyNode: 'N', firstLotDate: '2026/10/31 08:32:41', lastLotDate: '2026/11/21 13:50:04', lotQty: 24, factors: { EQUIPMENT: 'EQP-PLT-02', LINE: 'LN-B', PRODUCT: 'PRD-A200', MATERIAL: 'MAT-26K-034', OPERATOR: 'U11031' }, chartType: 'X-R', center: 0.415, sigma: 0.031, unit: 'mm', sampleSize: 5 },
  { factorSerial: 'FS-2611-0288', qcItemNo: 'QC-V-2002', qcItemName: 'Die Shear Strength', isShortRun: false, keyNode: 'I', firstLotDate: '2026/11/03 07:45:20', lastLotDate: '2026/11/23 08:13:51', lotQty: 19, factors: { EQUIPMENT: 'EQP-MOLD-05', LINE: 'LN-C', PRODUCT: 'PRD-B100', MATERIAL: 'MAT-26K-041', OPERATOR: 'U11019' }, chartType: 'X-S', center: 14.8, sigma: 0.62, unit: 'N', sampleSize: 8 },
  { factorSerial: 'FS-2611-0318', qcItemNo: 'QC-V-2002', qcItemName: 'Die Shear Strength', isShortRun: false, keyNode: 'C', firstLotDate: '2026/11/04 16:35:11', lastLotDate: '2026/11/22 08:18:55', lotQty: 26, factors: { EQUIPMENT: 'EQP-MOLD-05', LINE: 'LN-B', PRODUCT: 'PRD-B100', MATERIAL: 'MAT-26K-044', OPERATOR: 'U11023' }, chartType: 'X-S', center: 15, sigma: 0.58, unit: 'N', sampleSize: 8 },
  { factorSerial: 'FS-SR-0042', qcItemNo: 'QC-V-3001', qcItemName: 'Wafer Thickness', isShortRun: true, keyNode: 'I', firstLotDate: '2026/11/18 10:10:08', lastLotDate: '2026/11/23 08:04:36', lotQty: 18, factors: { EQUIPMENT: 'EQP-ETCH-03', LINE: 'LN-C', PRODUCT: 'PRD-A200', MATERIAL: 'MAT-26K-049', OPERATOR: 'U11031' }, chartType: 'X-Rm', center: 725, sigma: 2.1, unit: 'μm', sampleSize: 1 },
]

function dateTimeInput(date: Date): string {
  return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0') + 'T' + String(date.getHours()).padStart(2, '0') + ':' + String(date.getMinutes()).padStart(2, '0')
}
const now = new Date()
const fromDate = ref(dateTimeInput(new Date(now.getFullYear(), now.getMonth() - 2, now.getDate() + 1, 0, 0)))
const endDate = ref(dateTimeInput(new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59)))
const fromEnabled = ref(false)
const endEnabled = ref(false)
const latestLotsEnabled = ref(false)
const latestLots = ref<number | ''>('')
const showAllData = ref(false)
const showErf = ref(true)
const dataInOnePage = ref(true)
const showBarChart = ref(true)
const keyNode = ref('')
const resultRows = ref<ChartGroup[]>([])
const selectedSerial = ref('')
const queryComplete = ref(false)
const notice = ref('輸入至少一項 Factor 條件後查詢；查詢結果為 SPC QC Chart 群組。')
const feedback = ref('')
const pickerFactorNo = ref('')
const pickerDraft = ref<string[]>([])
const pickerOpen = ref(false)
const detailOpen = ref(false)
const viewMode = ref<ViewMode>('chart')
const selectedPoint = ref<number | null>(null)
const refreshCount = ref(0)
const modes: ViewMode[] = ['chart', 'data', 'histogram']

const factorPicker = computed(() => factors.value.find((factor) => factor.factorNo === pickerFactorNo.value) || null)
const onlineFactors = computed(() => factors.value.filter((factor) => factor.online))
const groupsForView = computed(() => resultRows.value.filter((group) => !group.isShortRun))
const selectedGroup = computed(() => groupsForView.value.find((group) => group.factorSerial === selectedSerial.value) || null)
const measures = computed<Measure[]>(() => {
  const group = selectedGroup.value
  if (!group) return []
  const count = Math.min(group.lotQty, 36)
  const rows: Measure[] = []
  for (let batch = 0; batch < count; batch += 1) {
    const drift = Math.sin(batch * 0.57 + group.factorSerial.length) * group.sigma * 0.34
    const special = batch === count - 5 ? group.sigma * 3.5 : 0
    const excluded = batch % 13 === 9
    const baseDate = new Date(Date.now() - (count - batch) * 16 * 60 * 60 * 1000)
    for (let sample = 0; sample < group.sampleSize; sample += 1) {
      const within = group.sampleSize === 1 ? Math.cos(batch * 0.8) * group.sigma * 0.8 : (sample - (group.sampleSize - 1) / 2) * group.sigma * 0.4 + Math.cos(batch + sample * 1.2) * group.sigma * 0.1
      const createdAt = baseDate.getTime() + sample * 60_000
      rows.push({
        index: rows.length,
        spcSerial: 'SPC-' + group.factorSerial.slice(-4) + '-' + String(batch + 1).padStart(3, '0'),
        lotNo: 'LOT-26K-' + String(108 + batch * 7).padStart(3, '0'),
        serialNo: String(sample + 1),
        createdAt,
        createDate: new Date(createdAt).toLocaleString('zh-TW', { hour12: false }),
        value: group.center + drift + special + within,
        excluded,
        erfNo: batch % 12 === 4 && sample === 0 ? 'ERF-26K-' + String(2200 + batch) : '',
        factorValues: { ...group.factors },
      })
    }
  }
  return rows
})
const filteredMeasures = computed(() => {
  let rows = measures.value.filter((measure) => showAllData.value || !measure.excluded)
  if (fromEnabled.value) rows = rows.filter((measure) => measure.createdAt >= new Date(fromDate.value).getTime())
  if (endEnabled.value) rows = rows.filter((measure) => measure.createdAt <= new Date(endDate.value).getTime())
  if (latestLotsEnabled.value && Number(latestLots.value) > 0) {
    const lotNos = [...new Set(rows.map((measure) => measure.lotNo))].slice(-Number(latestLots.value))
    rows = rows.filter((measure) => lotNos.includes(measure.lotNo))
  }
  return rows
})
const batches = computed(() => {
  const grouped = new Map<string, Measure[]>()
  for (const row of filteredMeasures.value) {
    const list = grouped.get(row.spcSerial) || []
    list.push(row)
    grouped.set(row.spcSerial, list)
  }
  return [...grouped.values()].sort((left, right) => left[0].index - right[0].index)
})
const erfBatchIndexes = computed(() => new Set(batches.value.map((batch, index) => batch.some((row) => row.erfNo) ? index : -1).filter((index) => index >= 0)))
const factorSummary = computed(() => Object.entries(selectedGroup.value?.factors || {}).map(([key, value]) => key + ': ' + value).join(' / '))
const plotValues = computed(() => selectedGroup.value?.chartType === 'X-Rm' ? batches.value.map((batch) => batch[0].value) : batches.value.map((batch) => batch.reduce((total, row) => total + row.value, 0) / batch.length))
const chartPoints = computed(() => {
  const values = plotValues.value
  if (!values.length) return []
  const min = Math.min(...values) - (selectedGroup.value?.sigma || 1)
  const max = Math.max(...values) + (selectedGroup.value?.sigma || 1)
  const span = max - min || 1
  return values.map((value, index) => ({ x: values.length === 1 ? 300 : 34 + index * (532 / (values.length - 1)), y: 18 + ((max - value) / span) * 132, value, index }))
})
const polyline = computed(() => chartPoints.value.map((point) => point.x + ',' + point.y).join(' '))
const selectedMeasure = computed(() => selectedPoint.value === null ? null : batches.value[selectedPoint.value]?.[0] || null)
const histogram = computed(() => {
  const values = filteredMeasures.value.map((row) => row.value)
  if (!values.length) return []
  const min = Math.min(...values)
  const max = Math.max(...values)
  const span = max - min || 1
  const buckets = Array.from({ length: 7 }, (_, index) => ({ label: (min + span * index / 7).toFixed(2), count: 0 }))
  for (const value of values) buckets[Math.min(6, Math.floor((value - min) / span * 7))].count += 1
  return buckets
})
const maxHistogramCount = computed(() => Math.max(1, ...histogram.value.map((bucket) => bucket.count)))

function valuesFor(factor: FactorFilter): string[] { return factor.value.split(',').map((value) => value.trim().toUpperCase()).filter(Boolean) }
function factorMatches(group: ChartGroup, factor: FactorFilter): boolean {
  const selected = valuesFor(factor)
  return !selected.length || selected.includes((group.factors[factor.factorNo] || '').toUpperCase())
}
function runQuery(): void {
  if (!factors.value.some((factor) => valuesFor(factor).length)) { notice.value = '請至少輸入或選擇一項 Factor 條件。'; return }
  resultRows.value = allGroups.filter((group) => factors.value.every((factor) => factorMatches(group, factor)) && (!keyNode.value || group.keyNode === keyNode.value))
  queryComplete.value = true
  selectedSerial.value = resultRows.value.find((group) => !group.isShortRun)?.factorSerial || ''
  notice.value = '查詢完成：' + groupsForView.value.length + ' 組一般管制圖。日期會送至舊 Service，但舊 Core 的 SC214 群組 SQL 沒有套用日期篩選。'
  feedback.value = ''
}
function clearFactorValues(): void { factors.value.forEach((factor) => { factor.value = '' }); feedback.value = '已清除 Factor 值；查詢結果保留至下次執行 Query。' }
function openFactorPicker(factor: FactorFilter): void { pickerFactorNo.value = factor.factorNo; pickerDraft.value = valuesFor(factor); pickerOpen.value = true }
function togglePickerValue(value: string): void { pickerDraft.value = pickerDraft.value.includes(value) ? pickerDraft.value.filter((item) => item !== value) : [...pickerDraft.value, value] }
function applyPicker(): void { if (factorPicker.value) factorPicker.value.value = pickerDraft.value.join(','); pickerOpen.value = false }
function normalizeFactorValue(factor: FactorFilter): void { factor.value = factor.value.toUpperCase() }
function openDetail(mode: ViewMode): void {
  if (!selectedGroup.value) { notice.value = '請先選取一組 QC Chart。'; return }
  if (latestLotsEnabled.value && (!Number.isInteger(Number(latestLots.value)) || Number(latestLots.value) <= 0)) { notice.value = '啟用 Query by Lot 時，批數必須為大於 0 的整數。'; return }
  if (fromEnabled.value && endEnabled.value && fromDate.value > endDate.value) { notice.value = '查詢起日不可晚於迄日。'; return }
  viewMode.value = mode
  selectedPoint.value = null
  detailOpen.value = true
}
function refreshDetail(): void { refreshCount.value += 1; feedback.value = 'DEV 假資料已重新整理；沒有呼叫 Service 或資料庫。' }
function saveGraph(): void { feedback.value = 'DEV 已模擬 Save Graph；沒有建立或上傳圖檔。' }
function printGraph(): void { feedback.value = 'DEV 已模擬列印；沒有呼叫本機列印服務。' }
function closeDetail(): void { detailOpen.value = false; selectedPoint.value = null }
function valueLabel(value: number): string { return Number(value.toFixed(3)).toString() }
</script>

<template>
  <main class="factor-page" :data-development-preview="props.developmentPreview">
    <header class="page-head"><div><p class="eyebrow">SPC / SC214 · CONTROL CHART BY FACTOR</p><h1>管制圖分析_以要因查詢</h1><p class="subtitle">依 Factor 條件查詢 SPC QC Chart 群組，開啟一般管制圖的資料、圖表或 Histogram。</p></div><button class="button" type="button" @click="emit('close')">Close (X)</button></header>

    <section class="panel criteria">
      <div class="criteria-grid">
        <label class="filter"><span><input v-model="fromEnabled" type="checkbox"> From Date</span><input v-model="fromDate" type="datetime-local" :disabled="!fromEnabled"></label>
        <label class="filter"><span><input v-model="endEnabled" type="checkbox"> End Date</span><input v-model="endDate" type="datetime-local" :disabled="!endEnabled"></label>
        <label class="filter"><span><input v-model="latestLotsEnabled" type="checkbox"> Query by Lot</span><input v-model.number="latestLots" type="number" min="1" step="1" :disabled="!latestLotsEnabled" placeholder="最近批數"></label>
        <label class="filter"><span>KeyNode</span><select v-model="keyNode"><option value="">全部</option><option value="I">I</option><option value="C">C</option><option value="N">N</option></select></label>
      </div>
      <div class="factor-head"><div><p class="eyebrow">ONLINE FACTOR FILTERS</p><h2>查詢要因</h2></div><div class="toolbar"><button class="button" type="button" @click="clearFactorValues">Clear</button><button class="button primary" type="button" @click="runQuery">Query</button></div></div>
      <div class="table-wrap"><table class="factor-table"><thead><tr><th>FactorNo</th><th>FactorName</th><th>ParameterType</th><th>FactorSpec</th><th>FactorValue</th></tr></thead><tbody>
        <tr v-for="factor in factors" :key="factor.factorNo"><td class="code">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ ['Table', 'Function', 'Free text'][factor.parameterType] }}</td><td>{{ factor.factorSpec }} <span class="tag">{{ factor.online ? '在線' : '非在線' }}</span></td><td class="factor-value"><input v-if="factor.parameterType === 2" v-model="factor.value" type="text" placeholder="輸入值，以逗號分隔" @blur="normalizeFactorValue(factor)"><template v-else><span>{{ factor.value || '不限' }}</span><button class="button" type="button" @click="openFactorPicker(factor)">選擇</button></template></td></tr>
      </tbody></table></div>
      <p class="inline-note">至少填一個 Factor 才能查詢。相同 Factor 多選值採 OR，不同 Factor 條件採 AND。QCItem 欄位在來源畫面隱藏，SC214 Service 也未將 QCItemNo 傳給 Core。</p>
      <div class="chart-options"><label><input v-model="showAllData" type="checkbox"> Show All Data</label><label><input v-model="showErf" type="checkbox"> Show ERF Marks</label><label><input v-model="dataInOnePage" type="checkbox"> Data in One Page</label><label><input v-model="showBarChart" type="checkbox"> Show BarChart</label></div>
    </section>

    <section class="panel notice"><p role="status">{{ notice }}</p><p v-if="feedback" class="feedback" role="status">{{ feedback }}</p><p class="source-note">日期不會縮小此群組清單：舊 Core 將日期 WHERE 留在註解中。日期與最近批數由 Query Chart／Data／Histogram 傳給共用分析視窗。Legacy Short-run tab 在 Form Designer 設為隱藏。</p></section>

    <section v-if="queryComplete" class="panel results">
      <header class="result-head"><div><p class="eyebrow">QC CHART BASIS · FACTOR SERIAL GROUPS</p><h2>Query Result <span class="count">{{ groupsForView.length }}</span></h2><p>依 FactorSerial 顯示群組和在線要因；雙擊或選取後開啟共用分析視窗。</p></div><div class="toolbar"><button class="button" type="button" :disabled="!selectedGroup" @click="openDetail('data')">Query Data</button><button class="button primary" type="button" :disabled="!selectedGroup" @click="openDetail('chart')">Query Chart</button><button class="button" type="button" :disabled="!selectedGroup" @click="openDetail('histogram')">Histogram</button></div></header>
      <div class="summary"><span><b>{{ groupsForView.length }}</b> 一般圖群組</span><span>Short-run：來源 tab 隱藏</span><span>日期：{{ fromEnabled || endEnabled ? '只傳至明細' : '未啟用' }}</span></div>
      <div class="table-wrap result-wrap"><table><thead><tr><th>FactorSerial</th><th>QCItemNo</th><th>QCItemName</th><th v-for="factor in onlineFactors" :key="factor.factorNo">{{ factor.factorName }}</th><th>FirstLotDate</th><th>LastLotDate</th><th>LotQty</th><th>KeyNode</th></tr></thead><tbody>
        <tr v-for="group in groupsForView" :key="group.factorSerial" :class="{ active: selectedSerial === group.factorSerial }" @click="selectedSerial = group.factorSerial" @dblclick="openDetail('chart')"><td class="code">{{ group.factorSerial }}</td><td class="code">{{ group.qcItemNo }}</td><td>{{ group.qcItemName }}</td><td v-for="factor in onlineFactors" :key="group.factorSerial + '-' + factor.factorNo">{{ group.factors[factor.factorNo] || '—' }}</td><td>{{ group.firstLotDate }}</td><td>{{ group.lastLotDate }}</td><td class="number">{{ group.lotQty }}</td><td>{{ group.keyNode }}</td></tr>
        <tr v-if="!groupsForView.length"><td :colspan="7 + onlineFactors.length" class="empty">沒有符合條件的一般管制圖群組。</td></tr>
      </tbody></table></div>
      <p class="disclaimer">群組摘要依 Chart Basis 和 FactorSerial 關聯計量 FactorLog 統計首末日期及批數。Factor 值與圖表均為 DEV 假資料；正式 SQL 必須限制 Factor 欄位並參數化值。</p>
    </section>

    <div v-if="pickerOpen && factorPicker" class="overlay" @click.self="pickerOpen = false"><section class="dialog small-dialog" role="dialog" aria-modal="true" :aria-label="'選擇 ' + factorPicker.factorName"><header><div><p class="eyebrow">FACTOR LOOKUP · TYPE {{ factorPicker.parameterType }}</p><h2>{{ factorPicker.factorName }}（{{ factorPicker.factorNo }}）</h2></div><button class="button" type="button" @click="pickerOpen = false">Close</button></header><p class="dialog-copy">多選後同一 Factor 採 OR；本頁只更新本機條件。</p><div class="choice-list"><label v-for="value in factorPicker.options" :key="value"><input type="checkbox" :checked="pickerDraft.includes(value)" @change="togglePickerValue(value)"><span>{{ value }}</span></label></div><footer><button class="button" type="button" @click="pickerDraft = []">清除</button><button class="button primary" type="button" @click="applyPicker">套用</button></footer></section></div>

    <div v-if="detailOpen && selectedGroup" class="overlay" @click.self="closeDetail"><section class="dialog analysis-dialog" role="dialog" aria-modal="true" aria-label="SPC chart detail"><header><div><p class="eyebrow">SHARED SC_AnsLineBarChart · {{ selectedGroup.factorSerial }}</p><h2>{{ selectedGroup.qcItemName }} <span class="code">{{ selectedGroup.qcItemNo }}</span></h2><p>{{ selectedGroup.chartType }} · {{ selectedGroup.sampleSize }} pcs/subgroup · {{ selectedGroup.unit }}</p></div><button class="button" type="button" @click="closeDetail">Close</button></header>
        <div class="detail-toolbar"><div class="mode-buttons"><button v-for="mode in modes" :key="mode" class="button" :class="{ selected: viewMode === mode }" type="button" @click="viewMode = mode">{{ mode === 'chart' ? 'Control Chart' : mode === 'data' ? 'Raw Data' : 'Histogram' }}</button></div><span>Show All Data {{ showAllData ? 'On' : 'Off' }} · ERF {{ showErf ? 'On' : 'Off' }}</span></div>
        <div class="detail-summary"><span>FactorSerial <b>{{ selectedGroup.factorSerial }}</b></span><span>Factor <b>{{ factorSummary }}</b></span><span>目前資料 <b>{{ filteredMeasures.length }}</b> 筆</span><span>Refresh <b>{{ refreshCount }}</b> 次</span></div>
        <div v-if="viewMode === 'chart'" class="chart-area"><div class="chart-title"><h3>{{ selectedGroup.chartType }} Control Chart</h3><span>UCL / CL / LCL 為展示用假界線</span></div><div class="svg-wrap"><svg viewBox="0 0 600 180" role="img" aria-label="DEV control chart"><line x1="28" y1="34" x2="572" y2="34" class="limit-line"/><line x1="28" y1="84" x2="572" y2="84" class="center-line"/><line x1="28" y1="134" x2="572" y2="134" class="limit-line"/><polyline :points="polyline" class="series-line"/><circle v-for="point in chartPoints" :key="point.index" :cx="point.x" :cy="point.y" r="4" :class="{ point: true, selected: selectedPoint === point.index, erf: showErf && erfBatchIndexes.has(point.index) }" @click="selectedPoint = point.index"/></svg></div><div class="chart-legend\"><span>UCL {{ valueLabel(selectedGroup.center + selectedGroup.sigma * 3) }}</span><span>CL {{ valueLabel(selectedGroup.center) }}</span><span>LCL {{ valueLabel(selectedGroup.center - selectedGroup.sigma * 3) }}</span></div><p v-if="selectedMeasure" class="point-info">點選資料：{{ selectedMeasure.lotNo }} · {{ selectedMeasure.spcSerial }} · {{ valueLabel(selectedMeasure.value) }} {{ selectedGroup.unit }} <span v-if="showErf && selectedMeasure.erfNo">· {{ selectedMeasure.erfNo }}</span></p></div>
        <div v-else-if="viewMode === 'data'" class="table-wrap detail-table"><table><thead><tr><th>SPCSerial</th><th>LotNo</th><th>SerialNo</th><th>CreateDate</th><th>TestValue</th><th>Excluded</th><th>ERF</th></tr></thead><tbody><tr v-for="row in filteredMeasures" :key="row.spcSerial + '-' + row.serialNo" :class="{ excluded: row.excluded }"><td class="code">{{ row.spcSerial }}</td><td>{{ row.lotNo }}</td><td>{{ row.serialNo }}</td><td>{{ row.createDate }}</td><td class="number">{{ valueLabel(row.value) }} {{ selectedGroup.unit }}</td><td>{{ row.excluded ? 'Yes' : 'No' }}</td><td>{{ showErf ? row.erfNo || '—' : '—' }}</td></tr></tbody></table></div>
        <div v-else class="histogram"><div v-for="bucket in histogram" :key="bucket.label" class="hist-column"><div class="bar" :style="{ height: Math.max(4, bucket.count / maxHistogramCount * 150) + 'px' }"><span>{{ bucket.count }}</span></div><small>{{ bucket.label }}</small></div></div>
        <footer><span class="inline-note">Data in One Page {{ dataInOnePage ? 'On' : 'Off' }} · BarChart {{ showBarChart ? 'On' : 'Off' }}</span><div class="toolbar"><button class="button" type="button" @click="refreshDetail">Refresh</button><button class="button" type="button" @click="saveGraph">Save Graph</button><button class="button" type="button" @click="printGraph">Print Graph</button></div></footer>
      </section></div>
  </main>
</template>

<style scoped>
.factor-page { --ink:#27332b; --muted:#66736a; --line:#d5dfd7; --green:#287552; min-height:0; height:100%; overflow:auto; padding:18px; box-sizing:border-box; background:#f2f5f2; color:var(--ink); font:inherit; }
.page-head,.factor-head,.result-head,.dialog header,.chart-title { display:flex; align-items:center; justify-content:space-between; gap:12px; }.page-head { margin-bottom:14px; align-items:flex-start; }.page-head h1,.result-head h2,.factor-head h2 { margin:2px 0; font-size:21px; }.page-head p,.result-head p { margin:4px 0 0; color:var(--muted); }.eyebrow { margin:0; color:var(--green); font-size:10px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:13px; }.panel { margin-bottom:11px; border:1px solid var(--line); border-radius:6px; background:#fff; box-shadow:0 1px 3px #28342b0b; }.criteria,.results { padding:13px; }.criteria-grid { display:grid; grid-template-columns:repeat(4,minmax(160px,1fr)); gap:9px; align-items:end; }.filter { display:grid; gap:4px; color:var(--muted); font-size:11px; }.filter>span { display:flex; align-items:center; gap:5px; color:var(--ink); font-weight:700; }.filter input:not([type=checkbox]),.filter select,.factor-value input { width:100%; min-height:31px; padding:5px 8px; box-sizing:border-box; border:1px solid #c8d0ca; border-radius:4px; background:#fff; color:var(--ink); font:inherit; }.filter input:disabled { background:#f0f2f0; }.factor-head { margin:14px 0 8px; }.factor-head h2 { font-size:16px; }.toolbar,.mode-buttons { display:flex; flex-wrap:wrap; gap:6px; }.button { min-height:29px; padding:4px 9px; border:1px solid #bcc8bf; border-radius:4px; background:#fff; color:#344238; font:inherit; font-size:10px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:#fff; }.button:disabled { cursor:not-allowed; opacity:.5; }.table-wrap { max-width:100%; max-height:290px; overflow:auto; border:1px solid #e2e8e3; border-radius:4px; }table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:10px; }th,td { padding:7px 8px; border-bottom:1px solid #e8ece8; text-align:left; }th { position:sticky; top:0; z-index:1; background:#f0f5f0; color:#5a685e; font-size:9px; }tbody tr { cursor:pointer; }tbody tr:hover,tbody tr.active { background:#e9f3eb; }.code { color:#246a4e; font-family:Consolas,monospace; }.number { text-align:right; font-variant-numeric:tabular-nums; }.factor-table tbody tr { cursor:default; }.factor-value { display:flex; align-items:center; gap:6px; min-width:250px; }.factor-value input { min-width:240px; }.tag { margin-left:5px; padding:2px 5px; border-radius:9px; background:#eef4ee; color:#55705d; font-size:9px; }.inline-note,.source-note,.disclaimer { color:var(--muted); font-size:10px; }.inline-note { margin:8px 1px 0; }.chart-options { display:flex; flex-wrap:wrap; gap:12px; margin-top:9px; color:var(--muted); font-size:10px; }.chart-options label { display:flex; align-items:center; gap:4px; }.notice { padding:9px 13px; color:#536158; font-size:11px; }.notice p { margin:0 0 4px; }.notice .feedback { color:var(--green); }.source-note { margin-top:6px!important; }.result-head { align-items:flex-start; margin-bottom:10px; }.result-head h2 { font-size:18px; }.result-head h2 .count { display:inline-grid; place-items:center; width:22px; height:22px; border-radius:12px; background:#e7f1e8; color:var(--green); font-size:11px; }.summary,.detail-summary,.chart-legend { display:flex; flex-wrap:wrap; gap:8px 18px; margin-bottom:9px; padding:8px 10px; border:1px solid #e4ebe5; border-radius:4px; background:#f8faf8; color:var(--muted); font-size:10px; }.summary b,.detail-summary b { color:var(--ink); font-size:13px; }.result-wrap { max-height:390px; }.empty { padding:16px; color:#89928b; text-align:center; }.disclaimer { margin:8px 0 0; }.overlay { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:18px; background:#1d2822a8; }.dialog { width:min(800px,96vw); max-height:88vh; overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.dialog header { padding:12px 14px; border-bottom:1px solid var(--line); }.dialog h2 { margin:2px 0; font-size:17px; }.dialog header p:not(.eyebrow) { margin:4px 0 0; color:var(--muted); font-size:10px; }.dialog footer { display:flex; justify-content:space-between; align-items:center; gap:8px; padding:9px 14px; border-top:1px solid #edf0ed; }.small-dialog { width:min(500px,96vw); }.dialog-copy { margin:10px 14px; color:var(--muted); font-size:10px; }.choice-list { max-height:50vh; overflow:auto; padding:4px 14px; }.choice-list label { display:flex; align-items:center; gap:8px; padding:9px 5px; border-bottom:1px solid #edf0ed; font-size:11px; }.analysis-dialog { width:min(1060px,97vw); }.detail-toolbar { display:flex; align-items:center; flex-wrap:wrap; gap:10px; padding:8px 14px; border-bottom:1px solid #edf0ed; color:var(--muted); font-size:10px; }.mode-buttons .selected { border-color:var(--green); background:#e7f1e8; color:var(--green); }.detail-summary { margin:9px 14px; }.chart-area { padding:3px 14px 12px; }.chart-title h3 { margin:0; font-size:13px; }.chart-title span { color:var(--muted); font-size:9px; }.svg-wrap { margin-top:8px; border:1px solid #e6ece7; background:linear-gradient(#fff 49%,#f8faf8 50%); }.svg-wrap svg { display:block; width:100%; height:210px; }.limit-line { stroke:#b96c62; stroke-width:1.5; stroke-dasharray:6 4; }.center-line { stroke:#7c9682; stroke-width:1; stroke-dasharray:3 4; }.series-line { fill:none; stroke:#287552; stroke-width:2.5; }.point { fill:#287552; stroke:white; stroke-width:1.5; cursor:pointer; }.point.selected { fill:#e89b3c; r:6; }.point.erf { stroke:#c43d36; stroke-width:3; }.chart-legend { margin:8px 0; }.point-info { margin:4px 0; padding:7px; border-radius:4px; background:#f5f8f5; color:#344238; font-size:10px; }.detail-table { max-height:49vh; margin:10px 14px; }.detail-table tr.excluded { color:#a38c68; background:#fff9ee; }.histogram { display:flex; align-items:flex-end; justify-content:center; gap:12px; min-height:220px; padding:18px 20px 14px; }.hist-column { display:grid; justify-items:center; align-content:end; gap:5px; width:50px; color:var(--muted); font-size:9px; }.bar { position:relative; display:grid; place-items:start center; width:100%; min-height:4px; border-radius:4px 4px 0 0; background:#76aa84; }.bar span { position:absolute; top:-14px; color:#425348; font-size:9px; }@media(max-width:850px){.criteria-grid{grid-template-columns:repeat(2,minmax(150px,1fr))}.result-head{flex-direction:column}.analysis-dialog{overflow:auto}.detail-summary{gap:6px 10px}}@media(max-width:580px){.factor-page{padding:10px}.page-head{flex-direction:column}.criteria-grid{grid-template-columns:1fr}.factor-value{align-items:flex-start;flex-direction:column}.dialog footer{align-items:flex-start;flex-direction:column}}
</style>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'

type ParameterType = 0 | 1 | 2
type LotState = 0 | 1 | 2
type FactorDefinition = { factorNo: string; factorName: string; parameterType: ParameterType; factorOnline: boolean; factorSpec: boolean; options: string[] }
type FactorState = FactorDefinition & { selectedValues: string[]; freeValue: string }
type QCItem = { qcItemNo: string; qcItemName: string; issueState: 2 | -1; limitLotNum: number; unit: string; limits: { us: number; ls: number; ucl: number; cl: number; lcl: number }; factors: FactorDefinition[] }
type ErrorDetail = { errorNo: string; errorName: string; reasonType: number; errorQty: number }
type SampleRow = { spcSerial: string; lotNo: string; createdAt: number; createDate: string; sampleQty: number; scrapQty: number; excluded: LotState; factorValues: Record<string, string>; details: ErrorDetail[]; violationIds: string[] }
type LimitKey = 'us' | 'ls' | 'ucl' | 'cl' | 'lcl'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const itemFixtures: QCItem[] = [
  { qcItemNo: 'QC-A-2201', qcItemName: 'Lead Frame Surface Defect', issueState: 2, limitLotNum: 24, unit: 'pcs', limits: { us: 0.24, ls: 0.015, ucl: 0.18, cl: 0.075, lcl: 0.005 }, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, factorOnline: true, factorSpec: true, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'MACHINE', factorName: '沖壓機台', parameterType: 1, factorOnline: true, factorSpec: false, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
    { factorNo: 'MATERIAL_LOT', factorName: '材料批號', parameterType: 2, factorOnline: true, factorSpec: true, options: [] },
    { factorNo: 'OPERATOR', factorName: '作業員', parameterType: 0, factorOnline: false, factorSpec: false, options: ['USR-11019', 'USR-11023', 'USR-11007'] },
  ] },
  { qcItemNo: 'QC-A-2237', qcItemName: 'Wire Bond Appearance', issueState: 2, limitLotNum: 30, unit: 'pcs', limits: { us: 0.2, ls: 0.01, ucl: 0.16, cl: 0.06, lcl: 0.004 }, factors: [
    { factorNo: 'LINE', factorName: '產線', parameterType: 0, factorOnline: true, factorSpec: true, options: ['LN-A', 'LN-B', 'LN-C'] },
    { factorNo: 'BONDER', factorName: '焊線機', parameterType: 1, factorOnline: true, factorSpec: false, options: ['WB-12', 'WB-15', 'WB-20'] },
    { factorNo: 'SHIFT', factorName: '班別', parameterType: 2, factorOnline: true, factorSpec: false, options: [] },
  ] },
  { qcItemNo: 'QC-A-2290', qcItemName: 'Mold Compound Contamination', issueState: -1, limitLotNum: 20, unit: 'pcs', limits: { us: 0.2, ls: 0.01, ucl: 0.15, cl: 0.055, lcl: 0.002 }, factors: [
    { factorNo: 'MACHINE', factorName: '成型機台', parameterType: 1, factorOnline: true, factorSpec: false, options: ['PRESS-03', 'PRESS-05', 'PRESS-08'] },
    { factorNo: 'MATERIAL_LOT', factorName: '材料批號', parameterType: 2, factorOnline: true, factorSpec: true, options: [] },
  ] },
]

const pickerSearch = ref('')
const pickerOpen = ref(false)
const selectedItem = ref<QCItem | null>(null)
const factors = ref<FactorState[]>([])
const refreshMinutes = ref(6)
const refreshCycle = ref(0)
const queried = ref(false)
const selectedSerial = ref('')
const notice = ref('請選擇 Attribute QC Item，設定刷新頻率與收集要因後執行 Query。')
const refreshedAt = ref('尚未查詢')
let refreshTimer: ReturnType<typeof setInterval> | undefined

const pickerItems = computed(() => itemFixtures.filter((item) => item.qcItemNo.toLowerCase().includes(pickerSearch.value.trim().toLowerCase()) || item.qcItemName.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
function selectItem(item: QCItem): void {
  selectedItem.value = item
  factors.value = item.factors.map((factor) => ({ ...factor, selectedValues: [], freeValue: '' }))
  pickerOpen.value = false
  queried.value = false
  selectedSerial.value = ''
  notice.value = item.qcItemNo + ' 已載入 Attribute Factor metadata；DEV 使用本機假資料。'
}
function factorValues(factor: FactorState): string[] {
  if (factor.parameterType === 2) return factor.freeValue.trim() ? factor.freeValue.split(',').map((value) => value.trim().toUpperCase()).filter(Boolean) : []
  return factor.selectedValues
}
function factorDescription(factor: FactorState): string {
  const values = factorValues(factor)
  return values.length ? values.join(', ') : '全部'
}
const factorDescriptionForHeader = computed(() => {
  const activeFactors = factors.value
    .filter((factor) => factorValues(factor).length > 0)
    .map((factor) => factor.factorName + ': ' + factorDescription(factor))
  return activeFactors.length ? activeFactors.join(' · ') : '全部 Factor'
})

const allRows = computed<SampleRow[]>(() => {
  const item = selectedItem.value
  if (!item) return []
  const count = Math.max(item.limitLotNum + 18, 42)
  const now = Date.now()
  const rows: SampleRow[] = []
  for (let index = 0; index < count; index += 1) {
    const createdAt = now - (count - index) * 42 * 60_000
    const sampleQty = 80 + (index % 5) * 10
    const exceptional = index === count - 7
    const periodicDelta = refreshCycle.value > 0 && index === count - 1 ? refreshCycle.value % 2 : 0
    const ordinaryScrap = (index * 7 + Math.floor(index / 4)) % 12
    const scrapQty = exceptional ? Math.ceil(sampleQty * 0.27) : ordinaryScrap + periodicDelta
    const excluded: LotState = index % 23 === 8 ? 2 : index % 13 === 6 ? 1 : 0
    const factorValues: Record<string, string> = {
      LINE: ['LN-A', 'LN-B', 'LN-C'][index % 3],
      MACHINE: ['PRESS-03', 'PRESS-05', 'PRESS-08'][Math.floor(index / 2) % 3],
      MATERIAL_LOT: 'MAT-26K-' + String(210 + Math.floor(index / 4)).padStart(3, '0'),
      OPERATOR: ['USR-11019', 'USR-11023', 'USR-11007'][index % 3],
      BONDER: ['WB-12', 'WB-15', 'WB-20'][index % 3],
      SHIFT: ['A', 'B', 'C'][index % 3],
    }
    const spcSerial = 'SPC-ATT-' + String(index + 1).padStart(5, '0')
    const errorQty = Math.min(scrapQty, Math.max(0, Math.floor(scrapQty * 0.7)))
    rows.push({
      spcSerial,
      lotNo: 'LOT-26K-' + String((index * 23 + 157) % 999).padStart(3, '0'),
      createdAt,
      createDate: new Date(createdAt).toLocaleString('zh-TW', { hour12: false }),
      sampleQty,
      scrapQty,
      excluded,
      factorValues: { ...factorValues },
      details: scrapQty > 0 ? [
        { errorNo: index % 2 ? 'E-110' : 'E-204', errorName: index % 2 ? '表面刮傷' : '鍍層異物', reasonType: 0, errorQty },
        ...(scrapQty > 3 ? [{ errorNo: 'E-321', errorName: '端子變形', reasonType: 0, errorQty: scrapQty - errorQty }] : []),
      ] : [],
      violationIds: exceptional ? ['P-3SIGMA', 'SPEC'] : index % 29 === 12 ? ['P-RUN-7'] : [],
    })
  }
  return rows
})

const visibleRows = computed(() => {
  if (!queried.value || !selectedItem.value) return []
  const filtered = allRows.value.filter((row) => factors.value.every((factor) => {
    const selected = factorValues(factor)
    return selected.length === 0 || selected.includes(row.factorValues[factor.factorNo] || '')
  }))
  return filtered.slice(-selectedItem.value.limitLotNum)
})
const selectedRow = computed(() => visibleRows.value.find((row) => row.spcSerial === selectedSerial.value) || null)
const chartStatistics = computed(() => {
  const sampleQty = visibleRows.value.reduce((sum, row) => sum + row.sampleQty, 0)
  const scrapQty = visibleRows.value.reduce((sum, row) => sum + row.scrapQty, 0)
  return { rows: visibleRows.value.length, sampleQty, scrapQty, pbar: sampleQty ? scrapQty / sampleQty : 0 }
})
const yScale = computed(() => {
  const item = selectedItem.value
  const points = visibleRows.value.map((row) => row.scrapQty / Math.max(row.sampleQty, 1))
  if (!item || !points.length) return { min: 0, max: 1 }
  const values = [...points, item.limits.us, item.limits.ucl, item.limits.cl, item.limits.lcl, item.limits.ls]
  const max = Math.max(...values, 0.02)
  return { min: Math.min(...values, 0), max: max + Math.max(max * 0.18, 0.02) }
})
function pointX(index: number): number { return visibleRows.value.length <= 1 ? 490 : 58 + index * (864 / (visibleRows.value.length - 1)) }
function pointY(value: number): number { return 350 - ((value - yScale.value.min) / Math.max(yScale.value.max - yScale.value.min, 0.00001)) * 294 }
const chartPoints = computed(() => visibleRows.value.map((row, index) => `${pointX(index)},${pointY(row.scrapQty / Math.max(row.sampleQty, 1))}`).join(' '))
const limitDefinitions: { key: LimitKey; label: string; color: string; kind: 'spec' | 'control' }[] = [
  { key: 'us', label: 'US', color: '#bf5551', kind: 'spec' },
  { key: 'ls', label: 'LS', color: '#bf5551', kind: 'spec' },
  { key: 'ucl', label: 'UCL', color: '#49a274', kind: 'control' },
  { key: 'cl', label: 'CL', color: '#49a274', kind: 'control' },
  { key: 'lcl', label: 'LCL', color: '#49a274', kind: 'control' },
]
function ratio(row: SampleRow): number { return row.scrapQty / Math.max(row.sampleQty, 1) }
function limitValue(key: LimitKey): number { return selectedItem.value?.limits[key] ?? 0 }
function formatRatio(value: number): string { return (value * 100).toFixed(2) + '%' }
function formatQty(value: number): string { return value.toLocaleString('zh-TW') }
function stateName(value: LotState): string { return value === 0 ? 'Normal' : value === 1 ? 'Excluded' : 'Isolated' }

function markRefreshed(): void { refreshedAt.value = new Date().toLocaleString('zh-TW', { hour12: false }) }
function startRefreshTimer(): void {
  if (refreshTimer) clearInterval(refreshTimer)
  if (!queried.value) return
  refreshTimer = setInterval(() => {
    refreshCycle.value += 1
    markRefreshed()
    notice.value = 'DEV 已重新計算本機假資料；沒有呼叫 Service 或資料庫。'
  }, Math.max(60_000, refreshMinutes.value * 60_000))
}
function updateRefreshFrequency(): void {
  if (!Number.isFinite(refreshMinutes.value) || refreshMinutes.value <= 0) {
    notice.value = '刷新頻率必須大於 0 分鐘。'
    return
  }
  startRefreshTimer()
  notice.value = '自動刷新頻率已設為 ' + refreshMinutes.value + ' 分鐘。'
}
function runQuery(): void {
  if (!selectedItem.value) { notice.value = '請先選擇 QC Item。'; return }
  if (!Number.isFinite(refreshMinutes.value) || refreshMinutes.value <= 0) { notice.value = '刷新頻率必須大於 0 分鐘。'; return }
  queried.value = true
  refreshCycle.value = 0
  selectedSerial.value = ''
  markRefreshed()
  startRefreshTimer()
  notice.value = 'DEV 假資料查詢完成；顯示最近 ' + selectedItem.value.limitLotNum + ' 筆 Lot。'
  if (visibleRows.value.length) selectedSerial.value = visibleRows.value[visibleRows.value.length - 1].spcSerial
}
function saveGraph(): void { notice.value = 'DEV 已模擬 Save Graph；沒有建立圖片檔。' }
function refreshNow(): void {
  refreshCycle.value += 1
  markRefreshed()
  notice.value = 'DEV 假資料已重新整理；沒有呼叫服務或資料庫。'
}
function closePage(): void {
  if (refreshTimer) clearInterval(refreshTimer)
  refreshTimer = undefined
  emit('close')
}

onBeforeUnmount(() => { if (refreshTimer) clearInterval(refreshTimer) })
</script>

<template>
  <section v-if="props.developmentPreview" class="pchart-page" aria-label="不良率管制圖看板">
    <header class="page-head"><div><p class="eyebrow">SPC / SC209 · ATTRIBUTE P CHART</p><h1>不良率管制圖看板</h1><p class="subtitle">依計數型 QC Item 與收集要因檢視最近批次的不良比例。</p></div><button class="button" @click="closePage">Close (X)</button></header>

    <section class="criteria panel">
      <div class="query-row">
        <label class="item-field">QC Item<input :value="selectedItem ? selectedItem.qcItemNo + ' · ' + selectedItem.qcItemName : ''" readonly placeholder="請選擇計數型 QC Item"><button class="button get-button" @click="pickerSearch = ''; pickerOpen = true">Get</button></label>
        <label class="frequency-field">Refresh (min)<input v-model.number="refreshMinutes" type="number" min="1" @change="updateRefreshFrequency"></label>
        <div class="item-meta"><span>ChartControl = 1</span><span v-if="selectedItem">LimitLotNum = {{ selectedItem.limitLotNum }}</span></div>
        <button class="button primary query-button" :disabled="!selectedItem" @click="runQuery">Query</button>
      </div>
      <div class="factor-header"><div><h2>Collect Factor</h2><span>{{ factors.length }} 個 QC Item 要因；空白代表不篩選</span></div><button class="button" :disabled="!factors.length" @click="factors.forEach((factor) => { factor.selectedValues = []; factor.freeValue = '' })">Clear selections</button></div>
      <div class="factor-scroll"><table><thead><tr><th>Factor No</th><th>要因</th><th>ParameterType</th><th>Factor Value</th><th>FactorOnline</th><th>FactorSpec</th></tr></thead><tbody>
        <tr v-for="factor in factors" :key="factor.factorNo"><td class="serial">{{ factor.factorNo }}</td><td>{{ factor.factorName }}</td><td>{{ factor.parameterType === 2 ? 'Free input' : factor.parameterType === 1 ? 'Function list' : 'Table list' }}</td><td><input v-if="factor.parameterType === 2" v-model="factor.freeValue" class="factor-input" maxlength="120" placeholder="可輸入多值，以逗號分隔"><select v-else v-model="factor.selectedValues" multiple size="2" class="factor-select"><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select></td><td><span :class="factor.factorOnline ? 'yes' : 'no'">{{ factor.factorOnline ? 'Y' : 'N' }}</span></td><td><span :class="factor.factorSpec ? 'yes' : 'no'">{{ factor.factorSpec ? 'Y' : 'N' }}</span></td></tr>
        <tr v-if="!factors.length"><td colspan="6" class="empty">先選擇 QC Item 載入 Factor metadata。</td></tr>
      </tbody></table></div>
      <p class="factor-note">ParameterType 0／1 使用 lookup；2 可自由輸入。限值 lookup 僅使用 FactorOnline = Y 的要因；圖表／明細查詢會套用已填的要因條件。</p>
    </section>

    <section class="notice-panel panel"><p class="notice" role="status">{{ notice }}</p></section>

    <section v-if="queried && selectedItem" class="dashboard panel">
      <header class="dashboard-head"><div><p class="eyebrow">{{ selectedItem.qcItemNo }} · {{ selectedItem.qcItemName }}</p><h2>P Chart · {{ selectedItem.unit }}</h2><p>最近 {{ selectedItem.limitLotNum }} 筆 · {{ factorDescriptionForHeader }} · 更新：{{ refreshedAt }}</p></div><div class="dashboard-actions"><button class="button" @click="refreshNow">Refresh now</button><button class="button" @click="saveGraph">Save Graph</button></div></header>
      <div class="stats-row"><div><small>DISPLAYED LOTS</small><b>{{ chartStatistics.rows }}</b></div><div><small>SAMPLE QTY</small><b>{{ formatQty(chartStatistics.sampleQty) }}</b></div><div><small>SCRAP QTY</small><b>{{ formatQty(chartStatistics.scrapQty) }}</b></div><div><small>SCRAP / SAMPLE</small><b>{{ formatRatio(chartStatistics.pbar) }}</b></div><div><small>QC LIMIT LOTS</small><b>{{ selectedItem.limitLotNum }}</b></div><div><small>AUTO REFRESH</small><b>{{ refreshMinutes }} min</b></div></div>

      <div class="chart-panel"><div class="chart-title"><div><h3>Scrap Proportion Chart</h3><span>每點 = ScrapQty / SampleQty；點選圖表點位查看 Lot、要因與不良明細。</span></div><span>{{ chartStatistics.rows }} lots</span></div>
        <div v-if="visibleRows.length" class="chart-wrap"><svg viewBox="0 0 960 405" preserveAspectRatio="none" role="img" aria-label="P Chart DEV 假資料"><line v-for="grid in 5" :key="grid" x1="58" :y1="62 + grid * 58" x2="922" :y2="62 + grid * 58" class="grid-line"/><line x1="58" y1="350" x2="922" y2="350" class="axis-line"/><line x1="58" y1="58" x2="58" y2="350" class="axis-line"/><text x="62" y="28" class="axis-label">Scrap / Sample</text><text x="785" y="390" class="axis-label">SPC Serial sequence →</text>
          <g v-for="line in limitDefinitions" :key="line.key" v-show="limitValue(line.key) > 0"><line x1="58" :y1="pointY(limitValue(line.key))" x2="922" :y2="pointY(limitValue(line.key))" :class="line.kind === 'spec' ? 'spec-line' : 'control-line'"/><text x="5" :y="pointY(limitValue(line.key)) - 4" :class="line.kind === 'spec' ? 'spec-label' : 'control-label'">{{ line.label }} {{ formatRatio(limitValue(line.key)) }}</text></g>
          <polyline :points="chartPoints" class="chart-line"/><g v-for="(row, index) in visibleRows" :key="row.spcSerial" class="point-hit" @click="selectedSerial = row.spcSerial"><circle :cx="pointX(index)" :cy="pointY(ratio(row))" :r="selectedSerial === row.spcSerial ? 7 : 4.5" :class="{ selected: selectedSerial === row.spcSerial, violation: row.violationIds.length > 0, excluded: row.excluded === 1 }"/><title>{{ row.spcSerial }} · {{ row.lotNo }} · {{ formatRatio(ratio(row)) }} · {{ row.scrapQty }} / {{ row.sampleQty }} · {{ stateName(row.excluded) }}{{ row.violationIds.length ? ' · ' + row.violationIds.join(', ') : '' }}</title></g></svg></div>
        <p v-else class="empty chart-empty">目前條件下沒有 Attribute SPC Log。</p>
        <div class="legend"><span><i class="point-normal"></i>Normal</span><span><i class="point-excluded"></i>Excluded</span><span><i class="point-violation"></i>Rule violation</span><span><i class="legend-spec"></i>US／LS</span><span><i class="legend-control"></i>UCL／CL／LCL</span></div>
      </div>

      <div class="detail-grid">
        <section class="detail-card"><header><h3>Selected Lot</h3><span>{{ selectedRow?.spcSerial || '請選擇資料點' }}</span></header><div v-if="selectedRow" class="detail-body"><dl><dt>Lot No</dt><dd>{{ selectedRow.lotNo }}</dd><dt>Create Date</dt><dd>{{ selectedRow.createDate }}</dd><dt>Sample Qty</dt><dd>{{ selectedRow.sampleQty }}</dd><dt>Scrap Qty</dt><dd>{{ selectedRow.scrapQty }}</dd><dt>Scrap / Sample</dt><dd>{{ formatRatio(ratio(selectedRow)) }}</dd><dt>Lot State</dt><dd>{{ stateName(selectedRow.excluded) }}</dd><dt>Rule violation</dt><dd :class="{ alert: selectedRow.violationIds.length > 0 }">{{ selectedRow.violationIds.length ? selectedRow.violationIds.join(' · ') : '無' }}</dd></dl><h4>Factor values</h4><div class="factor-values"><span v-for="factor in factors" :key="factor.factorNo"><b>{{ factor.factorName }}</b> {{ selectedRow.factorValues[factor.factorNo] || '—' }}</span></div></div><p v-else class="empty">圖表點位會同步顯示樣本與 Factor 明細。</p></section>
        <section class="detail-card"><header><h3>Scrap Error Detail · ErrorType 0</h3><span>{{ selectedRow?.details.length || 0 }} rows</span></header><div class="error-scroll"><table><thead><tr><th>Error No</th><th>Error Name</th><th>Reason Type</th><th>Qty</th></tr></thead><tbody><tr v-for="detail in selectedRow?.details || []" :key="detail.errorNo"><td class="serial">{{ detail.errorNo }}</td><td>{{ detail.errorName }}</td><td>{{ detail.reasonType }}</td><td class="number">{{ detail.errorQty }}</td></tr><tr v-if="selectedRow && !selectedRow.details.length"><td colspan="4" class="empty">此 Lot 沒有 ErrorQty &gt; 0 且 ErrorType = 0 的明細。</td></tr><tr v-if="!selectedRow"><td colspan="4" class="empty">選擇圖表點位以檢視 ErrorNo／ErrorQty／Reason。</td></tr></tbody></table></div></section>
      </div>
      <p class="disclaimer">P Chart 點位、LimitLotNum、Factor 值、Violation 與錯誤明細皆為 DEV 假資料。線條值使用本機假限值；自動 Refresh 只更新本機狀態。Save Graph 僅模擬，不建立檔案。</p>
    </section>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false"><section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇 Attribute QC Item"><header><div><p class="eyebrow">ATTRIBUTE QC ITEM LOOKUP</p><h2>選擇計數型 QC Item</h2></div><button class="button" @click="pickerOpen = false">Close</button></header><div class="picker-filters"><label>Search<input v-model="pickerSearch" autofocus placeholder="QC Item 編號或名稱"></label><span>LoadQCItemForAttributes · ChartControl = 1</span></div><div class="picker-list"><button v-for="item in pickerItems" :key="item.qcItemNo" class="picker-row" @click="selectItem(item)"><b>{{ item.qcItemNo }}</b><span>{{ item.qcItemName }}</span><span>LimitLotNum {{ item.limitLotNum }}</span><span>{{ item.issueState === 2 ? 'Released' : 'Stopped' }}</span></button><p v-if="!pickerItems.length" class="empty">沒有符合的 QC Item。</p></div></section></div>
  </section>
</template>

<style scoped>
.pchart-page { --ink:#252c27; --muted:#6d766f; --line:#d4dcd6; --paper:#fff; --canvas:#f2f5f2; --green:#287552; height:100%; min-height:0; overflow:auto; padding:18px; box-sizing:border-box; background:var(--canvas); color:var(--ink); font:inherit; }
.page-head,.query-row,.factor-header,.dashboard-head,.chart-title,.detail-card>header { display:flex; align-items:center; justify-content:space-between; gap:14px; }.page-head { margin-bottom:14px; }.page-head h1,.dashboard-head h2 { margin:2px 0; font-size:23px; }.page-head p,.dashboard-head p { margin:4px 0 0; color:var(--muted); }.eyebrow { margin:0; color:var(--green); font-size:11px; font-weight:800; letter-spacing:.1em; }.subtitle { font-size:13px; }.panel { margin-bottom:12px; border:1px solid var(--line); border-radius:6px; background:var(--paper); box-shadow:0 1px 3px #28342b0b; }.criteria { padding:14px; }.query-row { align-items:end; flex-wrap:wrap; }.item-field,.frequency-field { position:relative; display:grid; gap:5px; color:var(--muted); font-size:12px; font-weight:700; }.item-field { flex:1; min-width:260px; }.item-field input,.frequency-field input,.factor-input,.factor-select,.picker-filters input { min-height:34px; border:1px solid #c8d0ca; border-radius:4px; padding:5px 9px; box-sizing:border-box; background:white; color:var(--ink); font:inherit; }.item-field input { padding-right:75px; }.get-button { position:absolute; right:4px; bottom:4px; min-height:27px!important; }.frequency-field { width:120px; }.item-meta { display:grid; gap:4px; color:var(--muted); font-size:11px; }.query-button { min-width:90px; }.factor-header { margin:14px 0 8px; }.factor-header h2 { margin:0; font-size:15px; }.factor-header span,.factor-note { color:var(--muted); font-size:11px; }.factor-scroll,.error-scroll { max-width:100%; overflow:auto; border:1px solid var(--line); border-radius:4px; }table { width:100%; border-collapse:collapse; font-size:12px; white-space:nowrap; }th,td { border-bottom:1px solid #e8ece8; padding:7px 9px; text-align:left; }th { position:sticky; top:0; z-index:1; background:#f2f6f2; color:#59645c; font-size:11px; }tbody tr:last-child td { border-bottom:0; }.serial { color:#246a4e; font-family:Consolas,monospace; }.factor-input,.factor-select { width:min(300px,100%); min-height:32px; }.yes { color:#287552; font-weight:800; }.no { color:#9aa19b; }.factor-note { margin:8px 0 0; }.notice-panel { padding:9px 14px; }.notice { margin:0; min-height:18px; color:#536158; font-size:12px; }.dashboard { padding:14px; }.dashboard-head { align-items:flex-start; margin-bottom:13px; }.dashboard-head h2 { font-size:17px; }.dashboard-head p:last-child { font-size:11px; }.dashboard-actions { display:flex; gap:7px; }.stats-row { display:grid; grid-template-columns:repeat(6,minmax(100px,1fr)); gap:8px; margin-bottom:12px; }.stats-row>div { min-width:0; display:grid; gap:4px; padding:8px 10px; border:1px solid var(--line); border-radius:4px; background:#fbfcfb; }.stats-row small { color:var(--muted); font-size:9px; font-weight:800; letter-spacing:.04em; }.stats-row b { overflow:hidden; text-overflow:ellipsis; font-size:14px; white-space:nowrap; }.chart-panel { padding:10px; border:1px solid var(--line); border-radius:4px; }.chart-title { margin-bottom:6px; }.chart-title h3 { margin:0; font-size:15px; }.chart-title span { display:block; margin-top:3px; color:var(--muted); font-size:11px; }.chart-title>span { white-space:nowrap; }.chart-wrap { height:370px; min-width:520px; overflow:hidden; }.chart-wrap svg { width:100%; height:100%; }.grid-line { stroke:#e8ece8; stroke-width:1; }.axis-line { stroke:#78847b; stroke-width:1.2; }.axis-label { fill:#58645b; font-size:11px; }.spec-line { stroke:#c15b58; stroke-width:1.4; stroke-dasharray:5 3; }.control-line { stroke:#4c9a70; stroke-width:1.4; }.spec-label { fill:#b55350; font-size:10px; }.control-label { fill:#40895f; font-size:10px; }.chart-line { fill:none; stroke:#357d5b; stroke-width:2; }.point-hit { cursor:pointer; }.point-hit circle { fill:#529b74; stroke:white; stroke-width:1.6; }.point-hit circle.excluded { fill:#a1aaa4; opacity:.55; }.point-hit circle.violation { fill:#c5413f; }.point-hit circle.selected { stroke:#252c27; stroke-width:2.8; }.legend { display:flex; flex-wrap:wrap; gap:12px; margin:7px 0 0; color:var(--muted); font-size:10px; }.legend span { display:inline-flex; align-items:center; gap:5px; }.legend i { width:12px; height:12px; display:inline-block; border-radius:50%; background:#529b74; }.legend .point-excluded { background:#a1aaa4; }.legend .point-violation { background:#c5413f; }.legend .legend-spec,.legend .legend-control { width:18px; height:0; border-top:2px dashed #c15b58; border-radius:0; background:transparent; }.legend .legend-control { border-color:#4c9a70; border-style:solid; }.detail-grid { display:grid; grid-template-columns:minmax(0,1fr) minmax(0,1fr); gap:10px; margin-top:10px; }.detail-card { min-width:0; border:1px solid var(--line); border-radius:4px; }.detail-card>header { padding:9px 11px; border-bottom:1px solid var(--line); background:#f8faf8; }.detail-card h3 { margin:0; font-size:13px; }.detail-card>header span { color:var(--muted); font-size:10px; }.detail-body { padding:10px 12px; }.detail-body dl { display:grid; grid-template-columns:135px 1fr; gap:6px 10px; margin:0; font-size:11px; }.detail-body dt { color:var(--muted); }.detail-body dd { margin:0; font-weight:650; }.detail-body h4 { margin:12px 0 6px; font-size:11px; }.factor-values { display:flex; flex-wrap:wrap; gap:6px; }.factor-values span { padding:4px 7px; border-radius:3px; background:#eef4ef; font-size:10px; }.factor-values b { margin-right:4px; color:#347653; }.alert { color:#b13835!important; font-weight:800; }.error-scroll { max-height:250px; border:0; border-radius:0; }.number { text-align:right; font-variant-numeric:tabular-nums; }.empty { padding:12px; color:#89928b; text-align:center; }.chart-empty { min-height:180px; display:grid; place-items:center; }.disclaimer { margin:8px 0 0; color:var(--muted); font-size:10px; }.button { min-height:32px; padding:5px 11px; border:1px solid #bfc9c1; border-radius:4px; background:#fff; color:#313d34; font:inherit; font-size:12px; cursor:pointer; }.button:hover:not(:disabled) { border-color:var(--green); color:var(--green); }.button.primary { border-color:var(--green); background:var(--green); color:white; }.button:disabled { cursor:not-allowed; opacity:.5; }.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:20px; background:#1d2822a8; }.picker-dialog { width:min(780px,96vw); max-height:84vh; display:grid; grid-template-rows:auto auto minmax(0,1fr); overflow:hidden; border-radius:7px; background:white; box-shadow:0 15px 55px #0004; }.picker-dialog>header { display:flex; justify-content:space-between; align-items:center; padding:14px 16px; border-bottom:1px solid var(--line); }.picker-dialog h2 { margin:2px 0 0; font-size:18px; }.picker-filters { display:flex; align-items:end; gap:14px; padding:12px 16px; border-bottom:1px solid var(--line); }.picker-filters label { display:grid; flex:1; gap:4px; color:var(--muted); font-size:11px; }.picker-filters span { padding-bottom:8px; color:var(--muted); font-size:11px; }.picker-list { overflow:auto; padding:5px 10px 12px; }.picker-row { width:100%; display:grid; grid-template-columns:150px 1fr 130px 90px; gap:10px; padding:10px; border:0; border-bottom:1px solid #edf0ed; background:white; color:var(--ink); text-align:left; cursor:pointer; }.picker-row:hover { background:#e8f3ec; }.picker-row b { color:var(--green); font-family:Consolas,monospace; }
@media (max-width:1000px) { .stats-row { grid-template-columns:repeat(3,minmax(90px,1fr)); }.detail-grid { grid-template-columns:1fr; }.query-row { align-items:stretch; }.item-field { min-width:200px; } }
@media (max-width:650px) { .pchart-page { padding:10px; }.page-head,.dashboard-head { align-items:flex-start; flex-direction:column; }.stats-row { grid-template-columns:repeat(2,minmax(100px,1fr)); }.factor-header { align-items:flex-start; flex-direction:column; }.chart-wrap { min-width:680px; }.picker-row { grid-template-columns:1fr 1fr; }.picker-filters { align-items:stretch; flex-direction:column; } }
</style>

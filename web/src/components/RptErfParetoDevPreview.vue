<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

type FactorKey = 'customerNo' | 'opNo' | 'productNo' | 'moNo' | 'equipmentNo'
type Factor = { key: FactorKey; label: string; sourceName: string }
type CauseEvent = {
  erfNo: string
  createType: 'NORMAL' | 'BATCH'
  createDate: string
  holdItemNo: string
  holdItemName: string
  qty: number
  lotNos: string[]
  factors: Record<FactorKey, string[]>
}
type ParetoRow = { holdItemNo: string; holdItemName: string; holdQty: number; lotNos: string[]; lotNum: number; cumulativeRate: number }

const factors: Factor[] = [
  { key: 'customerNo', label: '客戶編號', sourceName: 'CustomerNo' },
  { key: 'opNo', label: '站別編號', sourceName: 'OPNo' },
  { key: 'productNo', label: '產品編號', sourceName: 'ProductNo' },
  { key: 'moNo', label: '工單編號', sourceName: 'MONo' },
  { key: 'equipmentNo', label: '設備編號', sourceName: 'EquipmentNo' },
]

function localDateTime(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
}

const now = new Date()
const monthStart = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0)
function sampleDate(daysAgo: number, hour: number): string {
  const day = Math.max(1, now.getDate() - daysAgo)
  return localDateTime(new Date(now.getFullYear(), now.getMonth(), day, hour, 20))
}

const fixtureEvents: CauseEvent[] = [
  { erfNo: 'ERF-DEV-26001', createType: 'NORMAL', createDate: sampleDate(1, 9), holdItemNo: 'HI-ETCH-01', holdItemName: '表面刮傷', qty: 18.5, lotNos: ['LOT-2601-01', 'LOT-2601-02'], factors: { customerNo: ['CUST-AURORA'], opNo: ['ETCH-01'], productNo: ['PRD-AX140'], moNo: ['MO-260101'], equipmentNo: ['EQP-ETCH-02'] } },
  { erfNo: 'ERF-DEV-26002', createType: 'BATCH', createDate: sampleDate(2, 13), holdItemNo: 'HI-CONTAM-02', holdItemName: '異物附著', qty: 16, lotNos: ['LOT-2601-03', 'LOT-2601-04', 'LOT-2601-05'], factors: { customerNo: ['CUST-AURORA', 'CUST-ORBIT'], opNo: ['SAW-02'], productNo: ['PRD-BX220', 'PRD-BX221'], moNo: ['MO-260103', 'MO-260104'], equipmentNo: ['EQP-SAW-01', 'EQP-SAW-03'] } },
  { erfNo: 'ERF-DEV-26003', createType: 'NORMAL', createDate: sampleDate(3, 10), holdItemNo: 'HI-ETCH-01', holdItemName: '表面刮傷', qty: 12.25, lotNos: ['LOT-2601-06'], factors: { customerNo: ['CUST-ORBIT'], opNo: ['ETCH-01'], productNo: ['PRD-BX220'], moNo: ['MO-260105'], equipmentNo: ['EQP-ETCH-01'] } },
  { erfNo: 'ERF-DEV-26004', createType: 'BATCH', createDate: sampleDate(4, 15), holdItemNo: 'HI-DIM-03', holdItemName: '尺寸偏移', qty: 9.75, lotNos: ['LOT-2601-07', 'LOT-2601-08'], factors: { customerNo: ['CUST-NOVA'], opNo: ['LITHO-01'], productNo: ['PRD-CX310'], moNo: ['MO-260107'], equipmentNo: ['EQP-LITHO-04'] } },
  { erfNo: 'ERF-DEV-26005', createType: 'NORMAL', createDate: sampleDate(5, 11), holdItemNo: 'HI-CONTAM-02', holdItemName: '異物附著', qty: 8, lotNos: ['LOT-2601-09', 'LOT-2601-10'], factors: { customerNo: ['CUST-AURORA'], opNo: ['SAW-02'], productNo: ['PRD-AX140'], moNo: ['MO-260109'], equipmentNo: ['EQP-SAW-03'] } },
  { erfNo: 'ERF-DEV-26006', createType: 'NORMAL', createDate: sampleDate(6, 8), holdItemNo: 'HI-CRACK-04', holdItemName: '晶粒裂痕', qty: 6.5, lotNos: ['LOT-2601-11'], factors: { customerNo: ['CUST-NOVA'], opNo: ['DIE-01'], productNo: ['PRD-CX310'], moNo: ['MO-260111'], equipmentNo: ['EQP-DIE-02'] } },
  { erfNo: 'ERF-DEV-26007', createType: 'BATCH', createDate: sampleDate(7, 14), holdItemNo: 'HI-DIM-03', holdItemName: '尺寸偏移', qty: 5.25, lotNos: ['LOT-2601-12', 'LOT-2601-13'], factors: { customerNo: ['CUST-ORBIT'], opNo: ['LITHO-01'], productNo: ['PRD-BX221'], moNo: ['MO-260113'], equipmentNo: ['EQP-LITHO-02'] } },
  { erfNo: 'ERF-DEV-26008', createType: 'NORMAL', createDate: sampleDate(8, 16), holdItemNo: 'HI-CRACK-04', holdItemName: '晶粒裂痕', qty: 3.5, lotNos: ['LOT-2601-14'], factors: { customerNo: ['CUST-AURORA'], opNo: ['DIE-01'], productNo: ['PRD-AX140'], moNo: ['MO-260115'], equipmentNo: ['EQP-DIE-01'] } },
  { erfNo: 'ERF-DEV-26009', createType: 'NORMAL', createDate: sampleDate(9, 10), holdItemNo: 'HI-OTHER-05', holdItemName: '外觀其他', qty: 2.5, lotNos: ['LOT-2601-16', 'LOT-2601-16'], factors: { customerNo: ['CUST-NOVA'], opNo: ['PKG-02'], productNo: ['PRD-DX410'], moNo: ['MO-260117'], equipmentNo: ['EQP-PKG-05'] } },
  { erfNo: 'ERF-DEV-26010', createType: 'BATCH', createDate: sampleDate(10, 12), holdItemNo: 'HI-CONTAM-02', holdItemName: '異物附著', qty: 1.75, lotNos: ['LOT-2601-18'], factors: { customerNo: ['CUST-ORBIT'], opNo: ['SAW-02'], productNo: ['PRD-BX220'], moNo: ['MO-260119'], equipmentNo: ['EQP-SAW-01'] } },
]

const filterModel = ref({
  fromTime: localDateTime(monthStart),
  toTime: localDateTime(now),
  customerNo: [] as string[],
  opNo: [] as string[],
  productNo: [] as string[],
  moNo: [] as string[],
  equipmentNo: [] as string[],
})
const pickerKey = ref<FactorKey | null>(null)
const pickerDraft = ref<string[]>([])
const pickerSearch = ref('')
const chartLimit = ref<number | null>(0)
const selectedCauseNos = ref<string[]>([])
const fishboneOpen = ref(false)
const hasQueried = ref(false)
const resultEvents = ref<CauseEvent[]>([])
const notice = ref('')
const refreshedAt = ref('尚未查詢')
const paretoSvg = ref<SVGSVGElement | null>(null)

const optionsByFactor = computed<Record<FactorKey, string[]>>(() => ({
  customerNo: uniqueValues('customerNo'), opNo: uniqueValues('opNo'), productNo: uniqueValues('productNo'), moNo: uniqueValues('moNo'), equipmentNo: uniqueValues('equipmentNo'),
}))
function uniqueValues(key: FactorKey): string[] {
  return [...new Set(fixtureEvents.flatMap((event) => event.factors[key]))].sort((a, b) => a.localeCompare(b))
}

const activeFactor = computed(() => factors.find((factor) => factor.key === pickerKey.value) ?? null)
const pickerOptions = computed(() => activeFactor.value ? optionsByFactor.value[activeFactor.value.key].filter((value) => `${value} ${activeFactor.value?.label}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase())) : [])
const matchingEvents = computed(() => fixtureEvents.filter((event) => {
  const createdAt = new Date(event.createDate).getTime()
  const from = new Date(filterModel.value.fromTime).getTime()
  const to = new Date(filterModel.value.toTime).getTime()
  if (!Number.isFinite(from) || !Number.isFinite(to) || createdAt < from || createdAt > to) return false
  return factors.every((factor) => {
    const selected = filterModel.value[factor.key]
    return selected.length === 0 || event.factors[factor.key].some((value) => selected.includes(value))
  })
}))

function aggregate(events: CauseEvent[]): ParetoRow[] {
  const rows = new Map<string, ParetoRow>()
  for (const event of events) {
    const row = rows.get(event.holdItemNo) ?? { holdItemNo: event.holdItemNo, holdItemName: event.holdItemName, holdQty: 0, lotNos: [], lotNum: 0, cumulativeRate: 0 }
    row.holdQty += event.qty
    row.lotNos.push(...event.lotNos)
    row.lotNum = row.lotNos.length
    rows.set(event.holdItemNo, row)
  }
  return [...rows.values()]
}

const resultRows = computed(() => aggregate(resultEvents.value).sort((a, b) => b.holdQty - a.holdQty || a.holdItemNo.localeCompare(b.holdItemNo)))
const totalHoldQty = computed(() => resultRows.value.reduce((sum, row) => sum + row.holdQty, 0))
const totalLotNum = computed(() => resultRows.value.reduce((sum, row) => sum + row.lotNum, 0))
const chartRows = computed(() => {
  const rawLimit = chartLimit.value ?? 0
  const limit = Math.max(0, Number.isFinite(rawLimit) ? Math.floor(rawLimit) : 0)
  const visible = resultRows.value.slice(0, limit > 0 ? limit : undefined).map((row) => ({ ...row }))
  const denominator = visible.reduce((sum, row) => sum + row.holdQty, 0)
  let cumulative = 0
  return visible.map((row) => {
    cumulative += row.holdQty
    row.cumulativeRate = denominator > 0 ? cumulative * 100 / denominator : 0
    return row
  })
})
const chartWidth = computed(() => Math.max(850, chartRows.value.length * 116 + 112))
const chartPlotHeight = 190
const chartBaseY = 224
const selectedRows = computed(() => resultRows.value.filter((row) => selectedCauseNos.value.includes(row.holdItemNo)))
const selectedEvents = computed(() => selectedCauseNos.value.length
  ? resultEvents.value.filter((event) => selectedCauseNos.value.includes(event.holdItemNo))
  : resultEvents.value)
const fishboneRoot = computed(() => selectedRows.value.length
  ? selectedRows.value.map((row) => `${row.holdItemNo} ${row.holdItemName} (${formatNumber(row.holdQty)})`).join('、')
  : `全部異常原因 (${formatNumber(totalHoldQty.value)})`)
const fishboneFactors = computed(() => factors.map((factor) => {
  const totals = new Map<string, number>()
  for (const event of selectedEvents.value) {
    const value = event.factors[factor.key].join(', ').trim()
    if (value) totals.set(value, (totals.get(value) ?? 0) + event.qty)
  }
  return { ...factor, values: [...totals.entries()].map(([value, qty]) => ({ value, qty })).sort((a, b) => b.qty - a.qty).slice(0, 5) }
}))
const queryConditionSummary = computed(() => [
  `From HoldTime：${filterModel.value.fromTime.replace('T', ' ')}`,
  `To HoldTime：${filterModel.value.toTime.replace('T', ' ')}`,
  ...factors.map((factor) => `${factor.sourceName}：${filterModel.value[factor.key].length ? filterModel.value[factor.key].join(', ') : '*'}`),
])

function formatNumber(value: number): string {
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value)
}
function formatRate(value: number): string { return `${value.toFixed(1)}%` }
function shortLabel(value: string, max = 11): string { return value.length > max ? `${value.slice(0, max - 1)}…` : value }
function barX(index: number): number { return 78 + index * 116 }
function barHeight(row: ParetoRow): number {
  const shownTotal = Math.max(chartRows.value.reduce((sum, item) => sum + item.holdQty, 0), 1)
  return Math.max(3, row.holdQty / shownTotal * chartPlotHeight)
}
function cumulativeY(row: ParetoRow): number { return chartBaseY - row.cumulativeRate / 100 * chartPlotHeight }
const cumulativePoints = computed(() => chartRows.value.map((row, index) => `${barX(index) + 31},${cumulativeY(row)}`).join(' '))

function openPicker(key: FactorKey): void {
  pickerKey.value = key
  pickerDraft.value = [...filterModel.value[key]]
  pickerSearch.value = ''
}
function toggleDraft(value: string): void {
  pickerDraft.value = pickerDraft.value.includes(value) ? pickerDraft.value.filter((item) => item !== value) : [...pickerDraft.value, value]
}
function applyPicker(): void {
  if (pickerKey.value) filterModel.value[pickerKey.value] = [...pickerDraft.value]
  pickerKey.value = null
}
function clearFactor(key: FactorKey): void { filterModel.value[key] = [] }
function selectionLabel(key: FactorKey): string {
  const selected = filterModel.value[key]
  return selected.length ? selected.join(', ') : '全部 (*)'
}
function runQuery(): void {
  notice.value = ''
  const from = new Date(filterModel.value.fromTime)
  const to = new Date(filterModel.value.toTime)
  if (!Number.isFinite(from.getTime()) || !Number.isFinite(to.getTime())) { notice.value = '請輸入有效的起訖時間。'; return }
  if (from > to) { notice.value = 'From HoldTime 必須小於或等於 To HoldTime。'; return }
  resultEvents.value = [...matchingEvents.value]
  selectedCauseNos.value = []
  hasQueried.value = true
  refreshedAt.value = localDateTime(new Date()).replace('T', ' ')
  if (!resultEvents.value.length) notice.value = '查無資料，請調整期間或條件。'
}
function redrawChart(): void {
  const value = chartLimit.value
  if (value === null) { notice.value = ''; return }
  if (!Number.isInteger(value) || value < 0) { notice.value = 'Bar Number 請輸入大於或等於 0 的整數；0 表示全部。'; return }
  notice.value = ''
}
function toggleCause(holdItemNo: string): void {
  selectedCauseNos.value = selectedCauseNos.value.includes(holdItemNo)
    ? selectedCauseNos.value.filter((item) => item !== holdItemNo)
    : [...selectedCauseNos.value, holdItemNo]
}
function openFishbone(): void { fishboneOpen.value = true }
function downloadChart(): void {
  if (!paretoSvg.value) return
  const source = new XMLSerializer().serializeToString(paretoSvg.value)
  const blob = new Blob([source], { type: 'image/svg+xml;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'rt415-erf-pareto.svg'
  anchor.click()
  URL.revokeObjectURL(url)
}
</script>

<template>
  <main class="erf-page" aria-label="RT415 異常單柏拉圖分析 DEV">
    <header class="page-head">
      <div><p class="eyebrow">RPT · RT415 · DEVELOPMENT DATA</p><h1>異常單柏拉圖分析</h1><p class="subtitle">依異常原因彙總 Hold Qty 與批數，檢視 Pareto 分布及要因值。</p></div>
      <button class="button close" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <section v-if="!hasQueried" class="query-card">
      <div class="section-title"><div><h2>查詢條件</h2><p>預設本月 1 日至目前時間；多選條件未指定時以 * 查詢。</p></div><span class="read-only">唯讀報表</span></div>
      <div class="date-grid">
        <label>From HoldTime<input v-model="filterModel.fromTime" type="datetime-local"></label>
        <label>To HoldTime<input v-model="filterModel.toTime" type="datetime-local"></label>
      </div>
      <div class="factor-grid">
        <article v-for="factor in factors" :key="factor.key" class="factor-card">
          <div><small>{{ factor.sourceName }}</small><strong>{{ factor.label }}</strong></div>
          <p :title="selectionLabel(factor.key)">{{ selectionLabel(factor.key) }}</p>
          <div class="factor-actions"><button class="button" type="button" @click="openPicker(factor.key)">選擇多筆</button><button v-if="filterModel[factor.key].length" class="text-button" type="button" @click="clearFactor(factor.key)">清除</button></div>
        </article>
      </div>
      <p v-if="notice" class="notice" role="status">{{ notice }}</p>
      <footer class="query-footer"><span>Oracle Query：一般異常單 + 批次異常單（ERFSource=0）</span><button class="button primary" type="button" @click="runQuery">Query</button></footer>
    </section>

    <section v-else class="results-page">
      <div class="results-head"><div><p class="eyebrow">{{ resultEvents.length }} hold rows · {{ refreshedAt }}</p><h2>異常原因統計與 Pareto</h2></div><div class="results-actions"><button class="button" type="button" @click="hasQueried = false">修改條件</button><button class="button" type="button" @click="runQuery">重新查詢</button></div></div>
      <div class="condition-strip"><span v-for="condition in queryConditionSummary" :key="condition">{{ condition }}</span></div>
      <p v-if="notice" class="notice" role="status">{{ notice }}</p>
      <div v-if="resultEvents.length" class="summary-grid">
        <article><small>Hold Qty</small><strong>{{ formatNumber(totalHoldQty) }}</strong><span>目前篩選結果總量</span></article>
        <article><small>LotNum</small><strong>{{ formatNumber(totalLotNum) }}</strong><span>依來源逐列計數，保留重複批號</span></article>
        <article><small>Hold Reason</small><strong>{{ resultRows.length }}</strong><span>異常原因種類</span></article>
        <article><small>Selected Reason</small><strong>{{ selectedCauseNos.length || 'All' }}</strong><span>點選長條可查看明細</span></article>
      </div>
      <div v-if="resultEvents.length" class="analysis-grid">
        <section class="chart-card">
          <header class="card-head"><div><h3>Pareto Chart</h3><p>依 Hold Qty 遞減排序；累積率分母為目前顯示的 Top N。</p></div><div class="chart-controls"><label>Bar Number<input v-model.number="chartLimit" type="number" min="0" step="1"></label><button class="button" type="button" @click="redrawChart">Re-Query</button><button class="button" type="button" @click="downloadChart">Save SVG</button></div></header>
          <div class="chart-scroll">
            <svg ref="paretoSvg" class="pareto-svg" :viewBox="`0 0 ${chartWidth} 330`" :width="chartWidth" height="330" role="img" aria-label="異常原因 Hold Qty 長條與累積 Pareto 比率">
              <defs><style>.axis{stroke:#b7c5ba;stroke-width:1}.grid{stroke:#e7ece7;stroke-width:1}.qty-bar{fill:#418361}.qty-bar.selected{fill:#d28c35}.chart-text{fill:#54645a;font:10px sans-serif}.line{fill:none;stroke:#d18b34;stroke-width:2.4}.point{fill:#d18b34;stroke:#fff;stroke-width:1.5}</style></defs>
              <g v-for="tick in [0, 25, 50, 75, 100]" :key="`tick-${tick}`"><line class="grid" x1="64" :y1="chartBaseY - chartPlotHeight * tick / 100" :x2="chartWidth - 62" :y2="chartBaseY - chartPlotHeight * tick / 100"></line><text class="chart-text" x="10" :y="chartBaseY - chartPlotHeight * tick / 100 + 4">{{ formatNumber(chartRows.reduce((sum, row) => sum + row.holdQty, 0) * tick / 100) }}</text><text class="chart-text" :x="chartWidth - 46" :y="chartBaseY - chartPlotHeight * tick / 100 + 4">{{ tick }}%</text></g>
              <line class="axis" x1="64" :y1="chartBaseY" :x2="chartWidth - 62" :y2="chartBaseY"></line>
              <g v-for="(row, index) in chartRows" :key="row.holdItemNo" class="bar-item" role="button" tabindex="0" :aria-label="`${row.holdItemNo} ${row.holdItemName}, Hold Qty ${formatNumber(row.holdQty)}`" @click="toggleCause(row.holdItemNo)" @keydown.enter.prevent="toggleCause(row.holdItemNo)">
                <rect :class="['qty-bar', { selected: selectedCauseNos.includes(row.holdItemNo) }]" :x="barX(index)" :y="chartBaseY - barHeight(row)" width="62" :height="barHeight(row)" rx="3"></rect>
                <text class="chart-text" :x="barX(index) + 31" :y="chartBaseY - barHeight(row) - 7" text-anchor="middle">{{ formatNumber(row.holdQty) }}</text>
                <text class="chart-text" :x="barX(index) + 31" y="242" text-anchor="middle">{{ shortLabel(row.holdItemName) }}</text>
                <text class="chart-text" :x="barX(index) + 31" y="258" text-anchor="middle">{{ row.lotNum }} lots</text>
                <text class="chart-text" :x="barX(index) + 31" y="274" text-anchor="middle">{{ formatRate(row.holdQty * 100 / chartRows.reduce((sum, item) => sum + item.holdQty, 0)) }}</text>
              </g>
              <polyline v-if="chartRows.length" class="line" :points="cumulativePoints"></polyline>
              <g v-for="(row, index) in chartRows" :key="`point-${row.holdItemNo}`" :transform="`translate(${barX(index) + 31},${cumulativeY(row)})`"><title>{{ row.holdItemName }} cumulative {{ formatRate(row.cumulativeRate) }}</title><circle class="point" cx="0" cy="0" r="4"></circle></g>
            </svg>
          </div>
          <div class="legend"><span><i class="bar-key"></i>Hold Qty</span><span><i class="line-key"></i>Pareto Rate</span><span>共 {{ chartRows.length }} 個顯示項目；Bar Number 為 0 顯示全部。</span></div>
          <p class="disclaimer">DEV 假資料；圖表點選可多選原因，魚骨圖依五個查詢要因統計 Top 5 值。</p>
        </section>
        <aside class="detail-card">
          <header class="card-head"><div><h3>異常原因明細</h3><p>選取列以查看批號與單項統計。</p></div><button class="button" type="button" @click="openFishbone">魚骨圖</button></header>
          <div class="table-scroll"><table><thead><tr><th></th><th>原因編號／名稱</th><th class="number">Hold Qty</th><th class="number">LotNum</th></tr></thead><tbody><tr v-for="row in resultRows" :key="row.holdItemNo" :class="{ chosen: selectedCauseNos.includes(row.holdItemNo) }" @click="toggleCause(row.holdItemNo)"><td><input type="checkbox" :checked="selectedCauseNos.includes(row.holdItemNo)" :aria-label="`選取 ${row.holdItemName}`" @click.stop="toggleCause(row.holdItemNo)"></td><td><strong>{{ row.holdItemName }}</strong><small>{{ row.holdItemNo }}</small></td><td class="number">{{ formatNumber(row.holdQty) }}</td><td class="number">{{ row.lotNum }}</td></tr></tbody></table></div>
          <div class="reason-detail"><h4>{{ selectedRows.length ? '選取異常' : '單項異常統計' }}</h4><template v-if="selectedRows.length === 1"><p>{{ selectedRows[0].holdItemNo }} · {{ selectedRows[0].holdItemName }}</p><p>Hold Qty：{{ formatNumber(selectedRows[0].holdQty) }}　LotNum：{{ selectedRows[0].lotNum }}</p><p class="lot-list">LotNo：{{ selectedRows[0].lotNos.join('、') }}</p></template><p v-else-if="selectedRows.length > 1">已選 {{ selectedRows.length }} 項，可開啟魚骨圖查看合併要因。</p><p v-else>點選長條或資料列查看單項異常資訊。</p></div>
          <div class="query-conditions"><h4>查詢條件</h4><p v-for="condition in queryConditionSummary" :key="condition">{{ condition }}</p></div>
        </aside>
      </div>
      <div v-else-if="!notice" class="empty-state">目前條件沒有異常單資料。</div>
    </section>

    <div v-if="pickerKey && activeFactor" class="modal-backdrop" @click.self="pickerKey = null">
      <section class="dialog picker-dialog" role="dialog" aria-modal="true" :aria-label="`選擇${activeFactor.label}`">
        <header><div><p class="eyebrow">MULTI SELECT</p><h2>{{ activeFactor.label }}</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="pickerKey = null">×</button></header>
        <label class="picker-search">搜尋代碼<input v-model="pickerSearch" type="search" placeholder="輸入代碼篩選"></label>
        <div class="picker-list"><label v-for="option in pickerOptions" :key="option"><input type="checkbox" :checked="pickerDraft.includes(option)" @change="toggleDraft(option)"><span>{{ option }}</span></label><p v-if="pickerOptions.length === 0" class="empty-state">沒有符合的假資料選項。</p></div>
        <footer><span>已選 {{ pickerDraft.length }} 項；不選代表全部</span><div><button class="button" type="button" @click="pickerDraft = []">清除</button><button class="button primary" type="button" @click="applyPicker">套用</button></div></footer>
      </section>
    </div>

    <div v-if="fishboneOpen" class="modal-backdrop" @click.self="fishboneOpen = false">
      <section class="dialog fishbone-dialog" role="dialog" aria-modal="true" aria-label="異常要因魚骨分析">
        <header><div><p class="eyebrow">FACTOR ANALYSIS · TOP 5</p><h2>異常要因魚骨分析</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="fishboneOpen = false">×</button></header>
        <div class="fishbone-root">{{ fishboneRoot }}</div>
        <div class="fishbone-body"><article v-for="factor in fishboneFactors" :key="factor.key"><small>{{ factor.sourceName }}</small><h3>{{ factor.label }}</h3><ol><li v-for="item in factor.values" :key="item.value"><span>{{ item.value }}</span><b>{{ formatNumber(item.qty) }}</b></li><li v-if="!factor.values.length" class="empty">沒有要因值</li></ol></article></div>
        <p class="disclaimer">依原 Form 魚骨圖的五個查詢要因彙總數量，每個要因顯示 Top 5。DEV fixture 僅供互動示意。</p>
        <footer><button class="button" type="button" @click="fishboneOpen = false">Close (X)</button></footer>
      </section>
    </div>
    <p v-if="developmentPreview" class="dev-stamp">DEVELOPMENT PREVIEW · DEV fixtures only · No MES data accessed</p>
  </main>
</template>

<style scoped>
.erf-page { min-height:0; height:100%; overflow:auto; box-sizing:border-box; padding:14px; background:#f5f7f4; color:#28372d; font:inherit; font-size:12px; }
.page-head,.results-head,.section-title,.card-head { display:flex; align-items:flex-start; justify-content:space-between; gap:12px; }
.page-head { margin-bottom:12px; }.page-head h1,.results-head h2 { margin:2px 0 4px; font-size:19px; }.subtitle,.section-title p,.card-head p { margin:0; color:#718078; font-size:10px; }
.eyebrow { margin:0; color:#74897b; font-size:9px; font-weight:800; letter-spacing:.09em; }.button { min-height:29px; padding:4px 10px; border:1px solid #c5d0c8; border-radius:4px; background:#fff; color:#304239; font:inherit; font-size:10px; cursor:pointer; }.button:hover { border-color:#4e8060; color:#2f7149; }.button.primary { border-color:#3d7853; background:#3d7853; color:#fff; }.button.close { flex:none; }
.query-card,.chart-card,.detail-card { border:1px solid #dbe3dc; border-radius:6px; background:#fff; box-shadow:0 2px 7px #243a2910; }.query-card { padding:13px; }.section-title { align-items:center; padding-bottom:11px; border-bottom:1px solid #e9eeea; }.section-title h2,.card-head h3 { margin:0 0 4px; font-size:13px; }.read-only { padding:4px 8px; border-radius:12px; background:#edf5ef; color:#3b7650; font-size:9px; font-weight:700; }
.date-grid { display:grid; grid-template-columns:repeat(2,minmax(170px,1fr)); gap:10px; padding:12px 0; }.date-grid label,.chart-controls label { display:grid; gap:4px; color:#64736a; font-size:10px; }.date-grid input,.chart-controls input { min-height:30px; padding:4px 7px; border:1px solid #d4ddd6; border-radius:4px; color:#334238; font:inherit; font-size:11px; }
.factor-grid { display:grid; grid-template-columns:repeat(auto-fit,minmax(165px,1fr)); gap:8px; }.factor-card { min-width:0; padding:9px; border:1px solid #e0e8e1; border-radius:5px; background:#fbfcfb; }.factor-card>div:first-child { display:flex; flex-direction:column; gap:3px; }.factor-card small,.summary-grid small { color:#849189; font-size:9px; }.factor-card strong { font-size:11px; }.factor-card p { min-height:28px; overflow:hidden; margin:7px 0; color:#506259; font-size:10px; text-overflow:ellipsis; }.factor-actions { display:flex; align-items:center; gap:8px; }.text-button { border:0; background:transparent; color:#64796a; font:inherit; font-size:10px; cursor:pointer; }.query-footer { display:flex; align-items:center; justify-content:space-between; gap:10px; margin-top:12px; padding-top:10px; border-top:1px solid #e9eeea; color:#859087; font-size:9px; }
.notice { margin:9px 0; padding:8px 10px; border:1px solid #e8d8ae; border-radius:4px; background:#fff8e8; color:#876322; font-size:10px; }.results-page { display:grid; gap:10px; }.results-head { align-items:center; }.results-actions,.chart-controls { display:flex; align-items:end; flex-wrap:wrap; gap:6px; }.condition-strip { display:flex; flex-wrap:wrap; gap:5px; padding:7px 9px; border:1px solid #e2e9e3; border-radius:4px; background:#fff; }.condition-strip span { padding:3px 6px; border-radius:3px; background:#f1f5f1; color:#627168; font-size:9px; }
.summary-grid { display:grid; grid-template-columns:repeat(4,minmax(120px,1fr)); gap:8px; }.summary-grid article { display:grid; gap:3px; padding:9px 10px; border:1px solid #dfe7e0; border-radius:5px; background:#fff; }.summary-grid strong { color:#345e43; font-size:18px; font-variant-numeric:tabular-nums; }.summary-grid span { color:#839087; font-size:9px; }
.analysis-grid { display:grid; grid-template-columns:minmax(0,1.7fr) minmax(280px,.9fr); gap:9px; min-height:400px; }.chart-card,.detail-card { min-width:0; padding:11px; }.card-head { align-items:center; padding-bottom:8px; }.chart-controls { align-items:end; }.chart-controls label { max-width:105px; }.chart-controls input { min-height:27px; }.chart-scroll { width:100%; overflow:auto; border:1px solid #edf1ed; border-radius:4px; background:linear-gradient(#fff,#fafcfa); }.pareto-svg { display:block; max-width:none; }.bar-item { cursor:pointer; outline:none; }.bar-item:focus .qty-bar { stroke:#263f2f; stroke-width:2; }.legend { display:flex; flex-wrap:wrap; gap:12px; padding:7px 2px 0; color:#718078; font-size:9px; }.legend i { display:inline-block; width:10px; height:10px; margin-right:4px; border-radius:2px; vertical-align:-1px; }.bar-key { background:#418361; }.line-key { height:2px!important; border-radius:0!important; background:#d18b34; }.disclaimer { margin:7px 2px 0; color:#87928a; font-size:9px; }
.detail-card { display:grid; grid-template-rows:auto minmax(120px,1fr) auto auto; }.table-scroll { max-height:300px; overflow:auto; border:1px solid #e7ece8; border-radius:4px; }.table-scroll table { width:100%; border-collapse:collapse; font-size:10px; }.table-scroll th { position:sticky; top:0; z-index:1; background:#f2f6f2; color:#67776d; text-align:left; }.table-scroll td,.table-scroll th { padding:6px 5px; border-bottom:1px solid #edf1ed; }.table-scroll tbody tr { cursor:pointer; }.table-scroll tbody tr:hover,.table-scroll tbody tr.chosen { background:#eef6f0; }.table-scroll td strong,.table-scroll td small { display:block; }.table-scroll td small { margin-top:2px; color:#839087; font-family:Consolas,monospace; font-size:9px; }.table-scroll input { accent-color:#3d7853; }.number { text-align:right; font-variant-numeric:tabular-nums; white-space:nowrap; }.reason-detail,.query-conditions { padding-top:8px; border-top:1px solid #e7ece8; }.reason-detail h4,.query-conditions h4 { margin:0 0 5px; font-size:10px; }.reason-detail p,.query-conditions p { margin:3px 0; color:#6f7d74; font-size:9px; }.lot-list { max-height:42px; overflow:auto; }.query-conditions { max-height:110px; overflow:auto; }.empty-state { display:grid; min-height:160px; place-items:center; color:#839087; font-size:11px; }
.modal-backdrop { position:fixed; inset:0; z-index:140; display:grid; place-items:center; padding:16px; background:#19251eaa; }.dialog { width:min(640px,96vw); max-height:88vh; display:grid; grid-template-rows:auto minmax(0,1fr) auto; overflow:hidden; border:1px solid #dbe3dc; border-radius:7px; background:white; box-shadow:0 16px 54px #0004; }.dialog>header { display:flex; align-items:center; justify-content:space-between; padding:12px 14px; border-bottom:1px solid #e6ece7; }.dialog h2 { margin:2px 0 0; font-size:16px; }.icon-button { width:28px; height:28px; border:1px solid #d6dfd8; border-radius:50%; background:#fff; color:#506259; font-size:18px; cursor:pointer; }.picker-dialog { grid-template-rows:auto auto minmax(0,1fr) auto; }.picker-search { display:grid; gap:5px; padding:10px 14px; color:#728077; font-size:10px; }.picker-search input { min-height:30px; padding:5px 8px; border:1px solid #d4ddd6; border-radius:4px; font:inherit; }.picker-list { min-height:100px; overflow:auto; padding:0 14px; }.picker-list>label { display:flex; align-items:center; gap:8px; padding:8px 2px; border-bottom:1px solid #edf1ed; font-size:11px; }.picker-list input { accent-color:#3d7853; }.picker-dialog footer,.fishbone-dialog footer { display:flex; align-items:center; justify-content:space-between; gap:10px; padding:10px 14px; border-top:1px solid #e6ece7; color:#748178; font-size:10px; }.picker-dialog footer>div { display:flex; gap:6px; }
.fishbone-dialog { width:min(850px,96vw); }.fishbone-root { margin:12px 14px 4px; padding:10px; border:1px solid #d9e6db; border-radius:4px; background:#eff6f0; color:#335941; text-align:center; font-weight:700; }.fishbone-body { display:grid; grid-template-columns:repeat(auto-fit,minmax(135px,1fr)); gap:7px; padding:8px 14px; overflow:auto; }.fishbone-body article { padding:9px; border:1px solid #e0e8e1; border-radius:4px; background:#fbfcfb; }.fishbone-body small { color:#859188; font-size:9px; }.fishbone-body h3 { margin:3px 0 7px; font-size:11px; }.fishbone-body ol { display:grid; gap:5px; margin:0; padding-left:19px; }.fishbone-body li { padding:4px 0; color:#5e6d63; font-size:10px; }.fishbone-body li span { display:inline-block; max-width:75%; overflow-wrap:anywhere; }.fishbone-body li b { float:right; color:#345e43; font-variant-numeric:tabular-nums; }.fishbone-body .empty { list-style:none; color:#87928a; }.fishbone-dialog>.disclaimer { padding:0 14px; }.dev-stamp { margin:9px 0 0; color:#8a968d; text-align:right; font-size:8px; }
@media(max-width:900px){.analysis-grid{grid-template-columns:1fr}.detail-card{min-height:400px}.summary-grid{grid-template-columns:repeat(2,minmax(115px,1fr))}}
@media(max-width:540px){.erf-page{padding:9px}.page-head,.results-head{align-items:flex-start}.date-grid{grid-template-columns:1fr}.factor-grid{grid-template-columns:1fr 1fr}.summary-grid{grid-template-columns:1fr 1fr}.query-footer{align-items:flex-start}.results-actions{justify-content:flex-end}.chart-card,.detail-card{padding:8px}}
</style>

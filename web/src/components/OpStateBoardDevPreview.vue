<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

type StateKey = 'queue' | 'run' | 'hold' | 'wait'
type ChartMode = 'bar' | 'pie'
interface StateDefinition { key: StateKey; label: string; code: number; color: string }
interface StateValue { definition: StateDefinition; lots: number; qty: number }
interface Operation { opNo: string; opName: string; productTypes: string[]; base: number[] }
interface OperationCard extends Operation { states: StateValue[]; totalLots: number; totalQty: number }
interface ProductRow { productNo: string; lots: number; qty: number; states: Record<StateKey, { lots: number; qty: number }> }
interface ProductTypeRow { productType: string; lots: number; qty: number; states: Record<StateKey, { lots: number; qty: number }> }
interface LotRow { lotNo: string; productNo: string; status: string; curQty: number; unitNo: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const stateDefinitions: StateDefinition[] = [
  { key: 'queue', label: 'Queue', code: 0, color: '#32a66e' },
  { key: 'run', label: 'Run', code: 1, color: '#4489d2' },
  { key: 'hold', label: 'Hold', code: 3, color: '#dd5d54' },
  { key: 'wait', label: 'Wait', code: 2, color: '#e4b03c' },
]
const operations: Operation[] = [
  { opNo: 'OP10', opName: 'Wafer Sort', productTypes: ['Product A', 'Product B', 'Product C'], base: [5, 540, 4, 392, 2, 190, 3, 275] },
  { opNo: 'OP20', opName: 'Wafer Probe', productTypes: ['Product A', 'Product C', 'Product D'], base: [3, 290, 7, 615, 1, 42, 4, 336] },
  { opNo: 'OP30', opName: 'Die Attach', productTypes: ['Product B', 'Product D'], base: [8, 735, 3, 275, 2, 108, 1, 64] },
  { opNo: 'OP40', opName: 'Wire Bond', productTypes: ['Product A', 'Product B', 'Product E'], base: [2, 175, 6, 590, 3, 230, 2, 145] },
  { opNo: 'OP50', opName: 'Mold & Cure', productTypes: ['Product C', 'Product D'], base: [4, 360, 2, 185, 1, 70, 5, 495] },
  { opNo: 'OP60', opName: 'Final Test', productTypes: ['Product A', 'Product E'], base: [0, 0, 0, 0, 0, 0, 0, 0] },
]
const selectedOps = ref(['OP10', 'OP20', 'OP30', 'OP40', 'OP50', 'OP60'])
const refreshSeconds = ref(60)
const fontSize = ref(13)
const chartMode = ref<ChartMode>('bar')
const refreshCount = ref(0)
const updatedAt = ref(new Date())
const countdown = ref('')
const settingsOpen = ref(false)
const draftOps = ref([...selectedOps.value])
const draftRefresh = ref(refreshSeconds.value)
const draftFont = ref(fontSize.value)
const settingsError = ref('')
const expandedOpNo = ref('')
const drillOpNo = ref('')
const drillProductType = ref('')
const detailOpNo = ref('')
const contextMenu = ref<{ x: number; y: number; opNo: string } | null>(null)
const notice = ref('')
let refreshTimer: ReturnType<typeof setInterval> | undefined
let countdownTimer: ReturnType<typeof setInterval> | undefined
let noticeTimer: ReturnType<typeof setTimeout> | undefined
let nextRefreshAt = Date.now() + refreshSeconds.value * 1000

const operationCards = computed<OperationCard[]>(() => operations.filter((operation) => selectedOps.value.includes(operation.opNo)).map((operation, operationIndex) => {
  const states = stateDefinitions.map((definition, stateIndex) => {
    const lots = operation.base[stateIndex * 2] + ((refreshCount.value + stateIndex + operationIndex) % 3)
    const qty = operation.base[stateIndex * 2 + 1] + (operation.base[stateIndex * 2 + 1] ? ((refreshCount.value + operationIndex + stateIndex * 2) % 13) * 3 : 0)
    return { definition, lots, qty }
  })
  return { ...operation, states, totalLots: states.reduce((sum, state) => sum + state.lots, 0), totalQty: states.reduce((sum, state) => sum + state.qty, 0) }
}))
const allMaxQty = computed(() => Math.max(1, ...operationCards.value.flatMap((card) => card.states.map((state) => state.qty))))
const selectedCard = computed(() => operations.find((operation) => operation.opNo === (drillOpNo.value || detailOpNo.value || expandedOpNo.value)))
const productTypeRows = computed<ProductTypeRow[]>(() => {
  const operation = operations.find((item) => item.opNo === drillOpNo.value)
  if (!operation) return []
  return operation.productTypes.map((productType, index) => {
    const states = Object.fromEntries(stateDefinitions.map((definition, stateIndex) => {
      const lotCount = operation.base[stateIndex * 2]
      const qtyValue = operation.base[stateIndex * 2 + 1]
      const lots = lotCount ? Math.max(0, Math.floor((lotCount + index - 1) / operation.productTypes.length)) : 0
      const qty = qtyValue ? Math.max(0, Math.floor((qtyValue * (index + 2)) / (operation.productTypes.length * 2.5))) : 0
      return [definition.key, { lots, qty }]
    })) as Record<StateKey, { lots: number; qty: number }>
    const totals = Object.values(states).reduce((sum, state) => ({ lots: sum.lots + state.lots, qty: sum.qty + state.qty }), { lots: 0, qty: 0 })
    return { productType, lots: totals.lots, qty: totals.qty, states }
  })
})
const productRows = computed<ProductRow[]>(() => {
  if (!drillProductType.value) return []
  const operation = operations.find((item) => item.opNo === drillOpNo.value)
  if (!operation) return []
  const typeIndex = operation.productTypes.indexOf(drillProductType.value)
  return Array.from({ length: 4 }, (_, index) => {
    const states = Object.fromEntries(stateDefinitions.map((definition, stateIndex) => {
      const typeState = productTypeRows.value[typeIndex]?.states[definition.key] ?? { lots: 0, qty: 0 }
      const lots = Math.max(0, Math.ceil((typeState.lots - index) / 2))
      const qty = Math.max(0, Math.floor((typeState.qty * (index + 2)) / 12))
      return [definition.key, { lots, qty }]
    })) as Record<StateKey, { lots: number; qty: number }>
    const totals = Object.values(states).reduce((sum, state) => ({ lots: sum.lots + state.lots, qty: sum.qty + state.qty }), { lots: 0, qty: 0 })
    return { productNo: `${drillProductType.value.replace('Product ', 'PRD-')}-${String(index + 1).padStart(2, '0')}`, ...totals, states }
  })
})
const breakdownMaxQty = computed(() => Math.max(1, ...(drillProductType.value ? productRows.value.map((row) => row.qty) : productTypeRows.value.map((row) => row.qty))))
const lotRows = computed<LotRow[]>(() => {
  const card = operationCards.value.find((item) => item.opNo === detailOpNo.value)
  if (!card) return []
  return [...card.states].sort((left, right) => left.definition.code - right.definition.code).flatMap((state, stateIndex) => Array.from({ length: Math.min(3, state.lots) }, (_, index) => ({
    lotNo: `L${detailOpNo.value.slice(-2)}${state.definition.code}${String(index + 1).padStart(4, '0')}`,
    productNo: `${card.productTypes[(stateIndex + index) % card.productTypes.length].replace('Product ', 'PRD-')}-${String(index + 1).padStart(2, '0')}`,
    status: state.definition.label,
    curQty: Math.max(1, Math.floor(state.qty / Math.max(state.lots, 1))),
    unitNo: index % 2 ? 'PCS' : 'DIE',
  })))
})
const updatedTime = computed(() => updatedAt.value.toLocaleTimeString('zh-TW', { hour12: false }))

function barX(index: number) { return 50 + index * 68 }
function barY(qty: number) { return 132 - (qty / allMaxQty.value) * 104 }
function barHeight(qty: number) { return Math.max(0, (qty / allMaxQty.value) * 104) }
function piePath(startAngle: number, endAngle: number) {
  const center = 92
  const radius = 68
  const point = (angle: number) => ({ x: center + radius * Math.cos((angle * Math.PI) / 180), y: center + radius * Math.sin((angle * Math.PI) / 180) })
  const from = point(startAngle)
  const to = point(endAngle)
  const largeArc = endAngle - startAngle > 180 ? 1 : 0
  return `M ${center} ${center} L ${from.x.toFixed(2)} ${from.y.toFixed(2)} A ${radius} ${radius} 0 ${largeArc} 1 ${to.x.toFixed(2)} ${to.y.toFixed(2)} Z`
}
function stateSegments(card: OperationCard) {
  const total = card.states.reduce((sum, state) => sum + state.qty, 0)
  if (!total) return []
  let angle = -90
  return card.states.map((state) => {
    const startAngle = angle
    angle += (state.qty / total) * 360
    return { ...state, startAngle, endAngle: angle, percent: (state.qty / total) * 100 }
  })
}
function formatQty(value: number) { return value.toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function openSettings() {
  draftOps.value = [...selectedOps.value]
  draftRefresh.value = refreshSeconds.value
  draftFont.value = fontSize.value
  settingsError.value = ''
  settingsOpen.value = true
}
function restartTimer() {
  if (refreshTimer) clearInterval(refreshTimer)
  nextRefreshAt = Date.now() + refreshSeconds.value * 1000
  refreshTimer = setInterval(refreshBoard, refreshSeconds.value * 1000)
  updateCountdown()
}
function refreshBoard() {
  refreshCount.value += 1
  updatedAt.value = new Date()
  restartTimer()
}
function updateCountdown() {
  const seconds = Math.max(0, Math.ceil((nextRefreshAt - Date.now()) / 1000))
  countdown.value = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}
function saveSettings() {
  if (!draftOps.value.length) {
    settingsError.value = '至少選擇一個作業站。'
    return
  }
  if (!Number.isInteger(draftRefresh.value) || draftRefresh.value < 1 || !Number.isInteger(draftFont.value) || draftFont.value < 8) {
    settingsError.value = '更新秒數需為正整數，字級至少為 8。'
    return
  }
  selectedOps.value = [...draftOps.value]
  refreshSeconds.value = draftRefresh.value
  fontSize.value = draftFont.value
  settingsOpen.value = false
  refreshBoard()
}
function openExpanded(opNo: string) {
  const card = operationCards.value.find((item) => item.opNo === opNo)
  if (card && (card.totalLots || card.totalQty)) expandedOpNo.value = opNo
}
function openDrilldown(opNo: string, qty: number) {
  if (qty <= 0) return
  drillOpNo.value = opNo
  drillProductType.value = ''
}
function openProductNo(productType: string) { drillProductType.value = productType }
function openContext(event: MouseEvent, opNo: string) {
  contextMenu.value = { x: Math.min(event.clientX, window.innerWidth - 190), y: Math.min(event.clientY, window.innerHeight - 100), opNo }
}
function announce(message: string) {
  notice.value = message
  if (noticeTimer) clearTimeout(noticeTimer)
  noticeTimer = setTimeout(() => { notice.value = '' }, 2600)
}
function simulateCopy(opNo: string) {
  contextMenu.value = null
  announce(`${opNo} 圖表已模擬複製（DEV 預覽不存取系統剪貼簿）。`)
}
function openLotDetail(opNo: string) {
  contextMenu.value = null
  const card = operationCards.value.find((item) => item.opNo === opNo)
  if (card && (card.totalLots || card.totalQty)) detailOpNo.value = opNo
}

onMounted(() => {
  restartTimer()
  countdownTimer = setInterval(updateCountdown, 1000)
})
onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer)
  if (countdownTimer) clearInterval(countdownTimer)
  if (noticeTimer) clearTimeout(noticeTimer)
})
</script>

<template>
  <section class="op-board" aria-label="作業站現況看板">
    <header class="page-heading"><div><p class="eyebrow">RPT · RT201</p><h1>作業站現況看板</h1><p class="subtitle">按 Queue、Run、Hold、Wait 檢視目前批數與在站數量。</p></div><button type="button" class="icon-button close-button" aria-label="關閉" @click="emit('close')">×</button></header>
    <div class="toolbar"><div class="status-line"><span class="live-dot" />最後更新 <strong>{{ updatedTime }}</strong><span>下次更新 {{ countdown }}</span><span>{{ operationCards.length }} 個作業站</span></div><div class="actions"><button type="button" class="secondary-button" @click="openSettings">看板設定</button><button type="button" class="secondary-button" @click="chartMode = chartMode === 'bar' ? 'pie' : 'bar'">{{ chartMode === 'bar' ? 'Pie Chart' : 'Bar Chart' }}</button><button type="button" class="primary-button" @click="refreshBoard">立即更新</button></div></div>
    <div v-if="!operationCards.length" class="empty-state"><strong>尚未設定作業站</strong><button type="button" class="primary-button" @click="openSettings">設定作業站</button></div>
    <div v-else class="board-grid" :style="{ '--tile-font': `${fontSize}px` }">
      <article v-for="card in operationCards" :key="card.opNo" class="board-card" @contextmenu.prevent="openContext($event, card.opNo)" @dblclick="openExpanded(card.opNo)">
        <header class="card-heading"><div><strong>{{ card.opNo }}</strong><span>{{ card.opName }}</span></div><span class="total-badge">{{ card.totalLots }} 批 / {{ formatQty(card.totalQty) }}</span></header>
        <svg v-if="chartMode === 'bar'" class="state-chart" viewBox="0 0 340 190" role="img" :aria-label="`${card.opNo} Queue Run Hold Wait 數量柱狀圖，點柱查看產品類別，雙擊放大`" @dblclick.stop="openExpanded(card.opNo)">
          <line x1="30" y1="132" x2="326" y2="132" class="axis-line" />
          <g v-for="(state, index) in card.states" :key="state.definition.key" tabindex="0" role="button" :aria-label="`${state.definition.label}: ${state.lots} 批 / ${formatQty(state.qty)}，查看產品類別`" @click="openDrilldown(card.opNo, state.qty)" @keydown.enter="openDrilldown(card.opNo, state.qty)">
            <title>{{ state.definition.label }} · {{ state.lots }} lots / {{ formatQty(state.qty) }} qty</title>
            <rect :x="barX(index)" :y="barY(state.qty)" width="36" :height="barHeight(state.qty)" :fill="state.definition.color" rx="3" class="bar" />
            <text :x="barX(index) + 18" :y="Math.max(18, barY(state.qty) - 6)" text-anchor="middle" class="bar-value">{{ state.lots }}/{{ formatQty(state.qty) }}</text>
            <text :x="barX(index) + 18" y="151" text-anchor="middle" class="axis-label">{{ state.definition.label }}</text>
          </g>
        </svg>
        <svg v-else class="state-pie" viewBox="0 0 340 190" role="img" :aria-label="`${card.opNo} Queue Run Hold Wait 數量圓餅圖，點區塊查看產品類別`" @dblclick.stop="openExpanded(card.opNo)">
          <g v-for="segment in stateSegments(card)" :key="segment.definition.key" class="pie-slice" tabindex="0" role="button" :aria-label="`${segment.definition.label}: ${segment.percent.toFixed(1)}%，${segment.lots} 批 / ${formatQty(segment.qty)}`" @click="openDrilldown(card.opNo, segment.qty)" @keydown.enter="openDrilldown(card.opNo, segment.qty)">
            <path :d="piePath(segment.startAngle, segment.endAngle)" :fill="segment.definition.color" stroke="#18242e" stroke-width="1.2"><title>{{ segment.definition.label }} · {{ segment.percent.toFixed(1) }}% · {{ segment.lots }} lots / {{ formatQty(segment.qty) }} qty</title></path>
            <text :x="213" :y="36 + stateDefinitions.indexOf(segment.definition) * 25" class="pie-key"><tspan class="pie-dot" :fill="segment.definition.color">●</tspan> {{ segment.definition.label }} {{ segment.percent.toFixed(0) }}%</text>
          </g>
          <text v-if="!card.totalQty" x="92" y="98" text-anchor="middle" class="empty-pie-label">No WIP</text>
        </svg>
        <div class="state-summary"><button v-for="state in card.states" :key="state.definition.key" type="button" class="state-chip" :disabled="!state.qty" @click="openDrilldown(card.opNo, state.qty)"><i :style="{ backgroundColor: state.definition.color }" /><span>{{ state.definition.label }}</span><strong>{{ state.lots }} / {{ formatQty(state.qty) }}</strong></button></div>
        <footer class="card-footer"><span>Lot / CurQty</span><span>雙擊放大 · 左鍵鑽取 · 右鍵更多</span></footer>
      </article>
    </div>
    <footer class="page-footer"><span>Legend：Queue · Run · Hold · Wait</span><span class="dev-note">DEV 假資料 · Refresh {{ refreshSeconds }} 秒 · 所有 OP 共用最大數量 {{ formatQty(allMaxQty) }}</span></footer>

    <div v-if="settingsOpen" class="modal-backdrop" @click.self="settingsOpen = false"><section class="dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title"><header class="dialog-header"><div><p class="eyebrow">SetupType 1 · DEV</p><h2 id="settings-title">作業站現況看板設定</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="settingsOpen = false">×</button></header><div class="setting-fields"><label>更新秒數<input v-model.number="draftRefresh" type="number" min="1"></label><label>字級<input v-model.number="draftFont" type="number" min="8"></label></div><div class="operation-list"><div class="section-title"><strong>作業站</strong><span>{{ draftOps.length }} 已選</span></div><label v-for="operation in operations" :key="operation.opNo" class="operation-option"><input v-model="draftOps" type="checkbox" :value="operation.opNo"><strong>{{ operation.opNo }}</strong><span>{{ operation.opName }}</span></label></div><p class="dialog-note">DEV 僅在本頁記憶體模擬共用看板設定，不寫入後端。</p><p v-if="settingsError" class="form-error" role="alert">{{ settingsError }}</p><footer class="dialog-actions"><button type="button" class="secondary-button" @click="settingsOpen = false">取消</button><button type="button" class="primary-button" @click="saveSettings">套用</button></footer></section></div>

    <div v-if="expandedOpNo" class="modal-backdrop" @click.self="expandedOpNo = ''"><section class="dialog expanded-dialog" role="dialog" aria-modal="true" :aria-label="`${expandedOpNo} 放大看板`"><header class="dialog-header"><div><p class="eyebrow">RT201 · Type 1 放大檢視</p><h2>{{ expandedOpNo }} · {{ selectedCard?.opName }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="expandedOpNo = ''">×</button></header><div class="expanded-content"><div class="large-total">{{ operationCards.find((card) => card.opNo === expandedOpNo)?.totalLots }} 批 / {{ formatQty(operationCards.find((card) => card.opNo === expandedOpNo)?.totalQty ?? 0) }}</div><div v-for="state in operationCards.find((card) => card.opNo === expandedOpNo)?.states ?? []" :key="state.definition.key" class="expanded-state"><span><i :style="{ backgroundColor: state.definition.color }" />{{ state.definition.label }}</span><strong>{{ state.lots }} 批 / {{ formatQty(state.qty) }}</strong><div class="meter"><span :style="{ width: `${(state.qty / allMaxQty) * 100}%`, backgroundColor: state.definition.color }" /></div></div></div><p class="dialog-note">WinForms 雙擊會開啟 Type 1 放大圖，且本機看板繼續按設定週期刷新。</p></section></div>

    <div v-if="drillOpNo" class="modal-backdrop" @click.self="drillOpNo = ''; drillProductType = ''"><section class="dialog drill-dialog" role="dialog" aria-modal="true" aria-labelledby="drill-title"><header class="dialog-header"><div><p class="eyebrow">RT201 · {{ drillProductType ? 'Type 3 ProductNo' : 'Type 2 ProductType' }}</p><h2 id="drill-title">{{ drillOpNo }} · {{ selectedCard?.opName }} 產品分布</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="drillOpNo = ''; drillProductType = ''">×</button></header><div class="breadcrumb"><button v-if="drillProductType" type="button" class="link-button" @click="drillProductType = ''">產品類別</button><span v-if="drillProductType">› {{ drillProductType }} · 產品編號</span><span v-else>各產品類別依 Queue／Run／Hold／Wait 分組</span></div><div class="breakdown-chart" aria-label="依產品類別或產品編號分組的狀態數量圖">
  <template v-if="!drillProductType">
  <button v-for="row in productTypeRows" :key="'chart-' + row.productType" type="button" class="breakdown-row" @click="openProductNo(row.productType)">
    <span class="breakdown-label"><strong>{{ row.productType }}</strong><small>{{ row.lots }} 批 · {{ formatQty(row.qty) }} 件</small></span>
    <span class="breakdown-track"><i v-for="state in stateDefinitions" :key="state.key" :style="{ width: (row.states[state.key].qty / breakdownMaxQty * 100) + '%', backgroundColor: state.color }" /></span>
    <strong class="breakdown-value">{{ formatQty(row.qty) }}</strong>
  </button>
  </template>
  <template v-else>
  <div v-for="row in productRows" :key="'chart-' + row.productNo" class="breakdown-row">
    <span class="breakdown-label"><strong>{{ row.productNo }}</strong><small>{{ row.lots }} 批 · {{ formatQty(row.qty) }} 件</small></span>
    <span class="breakdown-track"><i v-for="state in stateDefinitions" :key="state.key" :style="{ width: (row.states[state.key].qty / breakdownMaxQty * 100) + '%', backgroundColor: state.color }" /></span>
    <strong class="breakdown-value">{{ formatQty(row.qty) }}</strong>
  </div>
  </template>
</div>
<div class="table-wrap"><table v-if="!drillProductType"><thead><tr><th>ProductType</th><th v-for="state in stateDefinitions" :key="state.key">{{ state.label }}<small>批 / 數量</small></th><th>合計</th></tr></thead><tbody><tr v-for="row in productTypeRows" :key="row.productType" tabindex="0" @click="openProductNo(row.productType)" @keydown.enter="openProductNo(row.productType)"><th><button type="button" class="row-link" @click.stop="openProductNo(row.productType)">{{ row.productType }} ↗</button></th><td v-for="state in stateDefinitions" :key="state.key">{{ row.states[state.key].lots }} / {{ formatQty(row.states[state.key].qty) }}</td><td><strong>{{ row.lots }} / {{ formatQty(row.qty) }}</strong></td></tr></tbody></table><table v-else><thead><tr><th>ProductNo</th><th v-for="state in stateDefinitions" :key="state.key">{{ state.label }}<small>批 / 數量</small></th><th>合計</th></tr></thead><tbody><tr v-for="row in productRows" :key="row.productNo"><th>{{ row.productNo }}</th><td v-for="state in stateDefinitions" :key="state.key">{{ row.states[state.key].lots }} / {{ formatQty(row.states[state.key].qty) }}</td><td><strong>{{ row.lots }} / {{ formatQty(row.qty) }}</strong></td></tr></tbody></table></div><footer class="dialog-actions"><button type="button" class="secondary-button" @click="drillProductType ? drillProductType = '' : drillOpNo = ''">{{ drillProductType ? '返回類別' : '關閉' }}</button><button v-if="drillProductType" type="button" class="primary-button" @click="openLotDetail(drillOpNo); drillOpNo = ''; drillProductType = ''">Lot Detail</button></footer></section></div>

    <div v-if="detailOpNo" class="modal-backdrop" @click.self="detailOpNo = ''"><section class="dialog detail-dialog" role="dialog" aria-modal="true" aria-labelledby="detail-title"><header class="dialog-header"><div><p class="eyebrow">RT201 · LoadLotBasisJoinState · Status 0|1|2|3</p><h2 id="detail-title">{{ detailOpNo }} · {{ selectedCard?.opName }} 批次明細</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="detailOpNo = ''">×</button></header><p class="dialog-note">依 Status、ProductNo 排序展開；以下內容為 DEV 假資料。</p><div class="table-wrap"><table><thead><tr><th>LotNo</th><th>ProductNo</th><th>Status</th><th>CurQty</th><th>CurUnitNo</th></tr></thead><tbody><tr v-for="row in lotRows" :key="row.lotNo"><td>{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.status }}</td><td>{{ formatQty(row.curQty) }}</td><td>{{ row.unitNo }}</td></tr></tbody></table></div><footer class="dialog-actions"><button type="button" class="primary-button" @click="detailOpNo = ''">關閉</button></footer></section></div>

    <div v-if="contextMenu" class="context-backdrop" @click="contextMenu = null"><div class="context-menu" :style="{ left: `${contextMenu.x}px`, top: `${contextMenu.y}px` }" role="menu" @click.stop><button type="button" role="menuitem" @click="simulateCopy(contextMenu.opNo)">Copy To Clipboard</button><button v-if="operationCards.find((card) => card.opNo === contextMenu?.opNo)?.totalLots || operationCards.find((card) => card.opNo === contextMenu?.opNo)?.totalQty" type="button" role="menuitem" @click="openLotDetail(contextMenu.opNo)">Lot Detail</button></div></div>
    <div v-if="notice" class="toast" role="status">{{ notice }}</div>
  </section>
</template>

<style scoped>
@media(max-width:1120px){.board-grid{grid-template-columns:repeat(3,minmax(210px,1fr))}}@media(max-width:820px){.board-grid{grid-template-columns:repeat(2,minmax(205px,1fr))}}@media(max-width:600px){.op-board{padding:14px;gap:10px}.page-heading h1{font-size:19px}.toolbar{align-items:flex-start;flex-direction:column}.status-line{gap:8px}.actions{width:100%;flex-wrap:wrap}.board-grid{grid-template-columns:1fr;grid-auto-rows:minmax(270px,auto)}.board-card{min-height:260px}.page-footer{align-items:flex-start;flex-direction:column;gap:6px}.dev-note{margin-left:0}.table-wrap{overflow-x:auto}.breakdown-row{grid-template-columns:minmax(80px,1fr) minmax(60px,1.3fr) 42px;gap:6px}}
</style>

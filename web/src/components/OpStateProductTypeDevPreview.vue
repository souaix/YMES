<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

type StateKey = 'queue' | 'run' | 'hold' | 'wait'
interface StateDefinition { key: StateKey; label: string; code: number; color: string }
interface StateValue { lots: number; qty: number }
interface ProductType { name: string; color: string; base: number[] }
interface BaseOperation { opNo: string; opName: string; products: ProductType[] }
interface ChartProduct { name: string; color: string; states: Record<StateKey, StateValue> }
interface OperationCard { opNo: string; opName: string; products: ChartProduct[]; states: Record<StateKey, StateValue>; totalLots: number; totalQty: number }
interface ProductRow { productNo: string; productType: string; states: Record<StateKey, StateValue>; totalLots: number; totalQty: number }
interface LotRow { lotNo: string; productType: string; status: string; statusCode: number; curQty: number; unitNo: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const states: StateDefinition[] = [
  { key: 'queue', label: 'Queue', code: 0, color: '#32a66e' },
  { key: 'run', label: 'Run', code: 1, color: '#4489d2' },
  { key: 'hold', label: 'Hold', code: 3, color: '#dd5d54' },
  { key: 'wait', label: 'Wait', code: 2, color: '#e4b03c' },
]
const baseOperations: BaseOperation[] = [
  { opNo: 'OP10', opName: 'Wafer Sort', products: [
    { name: 'Product A', color: '#6979c8', base: [4, 310, 3, 245, 1, 62, 2, 180] },
    { name: 'Product B', color: '#e28845', base: [2, 150, 5, 465, 0, 0, 1, 74] },
    { name: 'Product C', color: '#50a994', base: [1, 88, 2, 130, 2, 106, 0, 0] },
  ] },
  { opNo: 'OP20', opName: 'Wafer Probe', products: [
    { name: 'Product A', color: '#6979c8', base: [2, 190, 6, 510, 1, 32, 2, 180] },
    { name: 'Product C', color: '#50a994', base: [3, 225, 2, 144, 0, 0, 3, 238] },
    { name: 'Product D', color: '#c95f79', base: [1, 65, 4, 320, 2, 98, 0, 0] },
  ] },
  { opNo: 'OP30', opName: 'Die Attach', products: [
    { name: 'Product B', color: '#e28845', base: [5, 445, 2, 160, 1, 50, 3, 270] },
    { name: 'Product D', color: '#c95f79', base: [2, 180, 5, 405, 2, 155, 1, 64] },
    { name: 'Product E', color: '#8d68ac', base: [1, 72, 1, 85, 0, 0, 2, 185] },
  ] },
  { opNo: 'OP40', opName: 'Wire Bond', products: [
    { name: 'Product A', color: '#6979c8', base: [3, 215, 2, 170, 1, 45, 2, 158] },
    { name: 'Product B', color: '#e28845', base: [2, 160, 6, 490, 3, 218, 1, 68] },
    { name: 'Product E', color: '#8d68ac', base: [1, 72, 3, 246, 0, 0, 2, 180] },
  ] },
  { opNo: 'OP50', opName: 'Mold & Cure', products: [
    { name: 'Product C', color: '#50a994', base: [2, 165, 4, 335, 1, 68, 3, 260] },
    { name: 'Product D', color: '#c95f79', base: [3, 250, 2, 188, 1, 70, 2, 160] },
  ] },
]
const selectedOps = ref(['OP10', 'OP20', 'OP30', 'OP40', 'OP50'])
const refreshSeconds = ref(30)
const fontSize = ref(18)
const refreshCount = ref(0)
const updatedAt = ref(new Date())
const countdown = ref('')
const settingsOpen = ref(false)
const draftOps = ref([...selectedOps.value])
const draftRefresh = ref(refreshSeconds.value)
const draftFont = ref(fontSize.value)
const settingsError = ref('')
const expandedOpNo = ref('')
const productTypeSelection = ref<{ opNo: string; name: string } | null>(null)
const detailOpNo = ref('')
const contextMenu = ref<{ x: number; y: number; opNo: string } | null>(null)
const notice = ref('')
let refreshTimer: ReturnType<typeof setInterval> | undefined
let countdownTimer: ReturnType<typeof setInterval> | undefined
let noticeTimer: ReturnType<typeof setTimeout> | undefined
let nextRefreshAt = Date.now() + refreshSeconds.value * 1000

const operationCards = computed<OperationCard[]>(() => {
  const selectedOperations = baseOperations.filter((operation) => selectedOps.value.includes(operation.opNo))
  const productCatalog = new Map<string, ProductType>()
  selectedOperations.forEach((operation) => operation.products.forEach((product) => { if (!productCatalog.has(product.name)) productCatalog.set(product.name, product) }))
  const productNames = [...productCatalog.keys()]
  return selectedOperations.map((operation, operationIndex) => {
  const products = productNames.map((productName, productIndex) => {
    const product = operation.products.find((item) => item.name === productName)
    const templateProduct = productCatalog.get(productName)!
    const base = product?.base ?? [0, 0, 0, 0, 0, 0, 0, 0]
    const values = Object.fromEntries(states.map((state, stateIndex) => {
      const baseIndex = stateIndex * 2
      const lots = base[baseIndex] ? base[baseIndex] + ((refreshCount.value + productIndex + operationIndex + stateIndex) % 2) : 0
      const qty = base[baseIndex + 1] ? base[baseIndex + 1] + ((refreshCount.value * 7 + productIndex * 3 + operationIndex) % 17) : 0
      return [state.key, { lots, qty }]
    })) as Record<StateKey, StateValue>
    return { name: productName, color: templateProduct.color, states: values }
  })
  const stateTotals = Object.fromEntries(states.map((state) => [state.key, products.reduce((total, product) => ({ lots: total.lots + product.states[state.key].lots, qty: total.qty + product.states[state.key].qty }), { lots: 0, qty: 0 })])) as Record<StateKey, StateValue>
  const totals = Object.values(stateTotals).reduce((total, value) => ({ lots: total.lots + value.lots, qty: total.qty + value.qty }), { lots: 0, qty: 0 })
  return { opNo: operation.opNo, opName: operation.opName, products, states: stateTotals, totalLots: totals.lots, totalQty: totals.qty }
  })
})
const maxStateQty = computed(() => Math.max(1, ...operationCards.value.flatMap((card) => states.map((state) => card.states[state.key].qty))))
const expandedCard = computed(() => operationCards.value.find((card) => card.opNo === expandedOpNo.value))
const selectedCard = computed(() => operationCards.value.find((card) => card.opNo === (productTypeSelection.value?.opNo || detailOpNo.value || expandedOpNo.value)))
const selectedProductType = computed(() => selectedCard.value?.products.find((product) => product.name === productTypeSelection.value?.name))
const productRows = computed<ProductRow[]>(() => {
  if (!selectedProductType.value) return []
  return Array.from({ length: 3 }, (_, index) => {
    const shares = [0.52, 0.30, 0.18]
    const rowStates = Object.fromEntries(states.map((state) => {
      const total = selectedProductType.value!.states[state.key]
      const lots = total.lots ? Math.floor(total.lots * shares[index]) + (index === 0 ? 1 : 0) : 0
      const qty = total.qty ? Math.floor(total.qty * shares[index]) : 0
      return [state.key, { lots, qty }]
    })) as Record<StateKey, StateValue>
    const totals = Object.values(rowStates).reduce((total, value) => ({ lots: total.lots + value.lots, qty: total.qty + value.qty }), { lots: 0, qty: 0 })
    return { productNo: `${productTypeSelection.value!.name.replace('Product ', 'PRD-')}-${String(index + 1).padStart(2, '0')}`, productType: selectedProductType.value!.name, states: rowStates, totalLots: totals.lots, totalQty: totals.qty }
  })
})
const lotRows = computed<LotRow[]>(() => {
  const card = operationCards.value.find((item) => item.opNo === detailOpNo.value)
  if (!card) return []
  return states.flatMap((state) => card.products.flatMap((product, productIndex) => {
    const value = product.states[state.key]
    if (!value.lots || !value.qty) return []
    return [{ lotNo: `L${detailOpNo.value.slice(-2)}${state.code}${String(productIndex + 1).padStart(4, '0')}`, productType: product.name, status: state.label, statusCode: state.code, curQty: Math.max(1, Math.floor(value.qty / value.lots)), unitNo: 'PCS' }]
  })).sort((left, right) => left.statusCode - right.statusCode || left.productType.localeCompare(right.productType))
})
const updatedTime = computed(() => updatedAt.value.toLocaleTimeString('zh-TW', { hour12: false }))

function barX(index: number) { return 45 + index * 70 }
function barSegments(card: OperationCard, state: StateDefinition) {
  let usedHeight = 0
  return card.products.map((product) => {
    const value = product.states[state.key]
    const height = (value.qty / maxStateQty.value) * 100
    const segment = { productType: product.name, color: product.color, qty: value.qty, lots: value.lots, y: 132 - usedHeight - height, height }
    usedHeight += height
    return segment
  }).filter((segment) => segment.qty > 0)
}
function totalY(card: OperationCard, state: StateDefinition) { return Math.max(18, 126 - (card.states[state.key].qty / maxStateQty.value) * 100 - 6) }
function largeBarX(index: number) { return 82 + index * 168 }
function largeBarSegments(card: OperationCard, state: StateDefinition) {
  let usedHeight = 0
  return card.products.map((product) => {
    const value = product.states[state.key]
    const height = (value.qty / maxStateQty.value) * 205
    const segment = { productType: product.name, color: product.color, qty: value.qty, lots: value.lots, y: 270 - usedHeight - height, height }
    usedHeight += height
    return segment
  }).filter((segment) => segment.qty > 0)
}
function largeTotalY(card: OperationCard, state: StateDefinition) { return Math.max(26, 260 - (card.states[state.key].qty / maxStateQty.value) * 205 - 9) }
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
  if (!draftOps.value.length) { settingsError.value = '至少選擇一個作業站。'; return }
  if (!Number.isInteger(draftRefresh.value) || draftRefresh.value < 1 || draftRefresh.value > 32767 || !Number.isInteger(draftFont.value) || draftFont.value < 8) { settingsError.value = '更新秒數需為 1–32767 秒的整數，字級至少為 8。'; return }
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
function openProductNo(opNo: string, productType: string, qty: number) {
  if (qty > 0) productTypeSelection.value = { opNo, name: productType }
}
function openContext(event: MouseEvent, opNo: string) { contextMenu.value = { x: Math.min(event.clientX, window.innerWidth - 190), y: Math.min(event.clientY, window.innerHeight - 100), opNo } }
function announce(message: string) {
  notice.value = message
  if (noticeTimer) clearTimeout(noticeTimer)
  noticeTimer = setTimeout(() => { notice.value = '' }, 2600)
}
function simulateCopy(opNo: string) { contextMenu.value = null; announce(`${opNo} 圖表已模擬複製（DEV 預覽不存取系統剪貼簿）。`) }
function openLotDetail(opNo: string) {
  contextMenu.value = null
  const card = operationCards.value.find((item) => item.opNo === opNo)
  if (card && (card.totalLots || card.totalQty)) detailOpNo.value = opNo
}

onMounted(() => { restartTimer(); countdownTimer = setInterval(updateCountdown, 1000) })
onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer)
  if (countdownTimer) clearInterval(countdownTimer)
  if (noticeTimer) clearTimeout(noticeTimer)
})
</script>

<template>
  <section class="product-type-board" aria-label="作業站現況看板產品類別 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">RPT · RT202</p><h1>作業站現況看板_產品類別</h1><p class="subtitle">每個作業站以 Queue、Run、Hold、Wait 堆疊呈現 ProductType 批數與在站數量。</p></div>
      <button type="button" class="icon-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="toolbar"><div class="status-line"><span class="live-dot" />最後更新 <strong>{{ updatedTime }}</strong><span>下次更新 {{ countdown }}</span><span>{{ operationCards.length }} 個作業站</span></div><div class="actions"><button type="button" class="secondary-button" @click="openSettings">看板設定</button><button type="button" class="primary-button" @click="refreshBoard">立即更新</button></div></div>
    <div v-if="!operationCards.length" class="empty-state"><strong>尚未設定作業站</strong><button type="button" class="primary-button" @click="openSettings">設定作業站</button></div>
    <div v-else class="board-grid" :style="{ '--tile-font': `${fontSize}px` }">
      <article v-for="card in operationCards" :key="card.opNo" class="board-card" @contextmenu.prevent="openContext($event, card.opNo)">
        <header class="card-heading"><div><strong>{{ card.opNo }}</strong><span>{{ card.opName }}</span></div><span class="total-badge">{{ card.totalLots }} 批 / {{ formatQty(card.totalQty) }}</span></header>
        <svg class="state-chart" viewBox="0 0 340 190" role="img" :aria-label="`${card.opNo} 各狀態 ProductType 堆疊數量圖`" @dblclick.stop="openExpanded(card.opNo)">
          <line x1="27" y1="132" x2="326" y2="132" class="axis-line" />
          <g v-for="(state, index) in states" :key="state.key">
            <text :x="barX(index) + 18" :y="totalY(card, state)" text-anchor="middle" class="bar-total">{{ formatQty(card.states[state.key].qty) }}</text>
            <rect v-for="segment in barSegments(card, state)" :key="`${state.key}-${segment.productType}`" :x="barX(index)" :y="segment.y" width="36" :height="segment.height" :fill="segment.color" rx="1" class="bar-segment" tabindex="0" role="button" :aria-label="`${state.label} ${segment.productType}: ${segment.lots} 批 / ${formatQty(segment.qty)} 件；開啟產品編號`" @click="openProductNo(card.opNo, segment.productType, segment.qty)" @keydown.enter="openProductNo(card.opNo, segment.productType, segment.qty)"><title>{{ state.label }} · {{ segment.productType }} · {{ segment.lots }} lots / {{ formatQty(segment.qty) }} qty</title></rect>
            <text :x="barX(index) + 18" y="151" text-anchor="middle" class="axis-label">{{ state.label }}</text>
          </g>
        </svg>
        <div class="product-legend"><span v-for="product in card.products" :key="product.name"><i :style="{ backgroundColor: product.color }" />{{ product.name }}</span></div>
        <div class="state-summary"><span v-for="state in states" :key="state.key"><i :style="{ backgroundColor: state.color }" /><strong>{{ state.label }}</strong><small>{{ card.states[state.key].lots }} 批 / {{ formatQty(card.states[state.key].qty) }}</small></span></div>
        <footer class="card-footer"><span>堆疊色彩代表 ProductType</span><span>點色塊開產品 · 雙擊放大 · 右鍵更多</span></footer>
      </article>
    </div>
    <footer class="page-footer"><span>狀態順序：Queue · Run · Hold · Wait</span><span class="dev-note">DEV 假資料 · Refresh {{ refreshSeconds }} 秒 · 全看板共用最大數量 {{ formatQty(maxStateQty) }}</span></footer>

    <div v-if="settingsOpen" class="modal-backdrop" @click.self="settingsOpen = false"><section class="dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title"><header class="dialog-header"><div><p class="eyebrow">SetupType 2 · DEV</p><h2 id="settings-title">產品類別看板設定</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="settingsOpen = false">×</button></header><div class="setting-fields"><label>更新秒數<input v-model.number="draftRefresh" type="number" min="1" max="32767"></label><label>字級<input v-model.number="draftFont" type="number" min="8"></label></div><div class="operation-list"><div class="section-title"><strong>作業站</strong><span>{{ draftOps.length }} 已選</span></div><label v-for="operation in baseOperations" :key="operation.opNo" class="operation-option"><input v-model="draftOps" type="checkbox" :value="operation.opNo"><strong>{{ operation.opNo }}</strong><span>{{ operation.opName }}</span></label></div><p class="dialog-note">DEV 僅在本頁記憶體模擬 SetupType 2 的作業站設定，不寫入後端。</p><p v-if="settingsError" class="form-error" role="alert">{{ settingsError }}</p><footer class="dialog-actions"><button type="button" class="secondary-button" @click="settingsOpen = false">取消</button><button type="button" class="primary-button" @click="saveSettings">套用</button></footer></section></div>
    <div v-if="expandedOpNo" class="modal-backdrop" @click.self="expandedOpNo = ''"><section class="dialog expanded-dialog" role="dialog" aria-label="Type 2 放大檢視"><header class="dialog-header"><div><p class="eyebrow">RT202 · Type 2</p><h2>{{ expandedOpNo }} · {{ operationCards.find((card) => card.opNo === expandedOpNo)?.opName }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="expandedOpNo = ''">×</button></header><svg v-if="expandedCard" class="expanded-chart" viewBox="0 0 780 330" role="img" :aria-label="expandedCard.opNo + ' ProductType 堆疊圖，點色塊開產品編號'">
  <line x1="38" y1="270" x2="750" y2="270" class="axis-line" />
  <g v-for="(state, index) in states" :key="state.key">
    <text :x="largeBarX(index) + 36" :y="largeTotalY(expandedCard, state)" text-anchor="middle" class="bar-total">{{ formatQty(expandedCard.states[state.key].qty) }}</text>
    <rect v-for="segment in largeBarSegments(expandedCard, state)" :key="state.key + '-' + segment.productType" :x="largeBarX(index)" :y="segment.y" width="72" :height="segment.height" :fill="segment.color" class="bar-segment" tabindex="0" role="button" :aria-label="state.label + ' ' + segment.productType + ': ' + formatQty(segment.qty)" @click="openProductNo(expandedCard.opNo, segment.productType, segment.qty)" @keydown.enter="openProductNo(expandedCard.opNo, segment.productType, segment.qty)"><title>{{ state.label }} · {{ segment.productType }} · {{ segment.lots }} lots / {{ formatQty(segment.qty) }} qty</title></rect>
    <text :x="largeBarX(index) + 36" y="296" text-anchor="middle" class="axis-label">{{ state.label }}</text>
  </g>
</svg>
<div class="expanded-legend"><span v-for="product in expandedCard?.products ?? []" :key="product.name"><i :style="{ backgroundColor: product.color }" />{{ product.name }}</span></div><p class="dialog-note">舊版雙擊會開啟放大的 Type 2 看板；refresh timer 仍依設定週期更新。</p></section></div>
    <div v-if="productTypeSelection" class="modal-backdrop" @click.self="productTypeSelection = null"><section class="dialog product-dialog" role="dialog" aria-labelledby="product-dialog-title"><header class="dialog-header"><div><p class="eyebrow">RT202 → ProductNo 鑽取</p><h2 id="product-dialog-title">{{ productTypeSelection.opNo }} · {{ productTypeSelection.name }} 產品編號分布</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="productTypeSelection = null">×</button></header><p class="dialog-note">點選堆疊色塊會由 `LoadBillBoard_OPState_PRDType` 鑽取至 ProductNo。以下為 DEV 假資料。</p><div class="table-wrap"><table><thead><tr><th>ProductNo</th><th v-for="state in states" :key="state.key">{{ state.label }}<small>批 / 數量</small></th><th>合計</th></tr></thead><tbody><tr v-for="row in productRows" :key="row.productNo"><th>{{ row.productNo }}</th><td v-for="state in states" :key="state.key">{{ row.states[state.key].lots }} / {{ formatQty(row.states[state.key].qty) }}</td><td><strong>{{ row.totalLots }} / {{ formatQty(row.totalQty) }}</strong></td></tr></tbody></table></div><footer class="dialog-actions"><button type="button" class="primary-button" @click="productTypeSelection = null">返回作業站看板</button></footer></section></div>
    <div v-if="detailOpNo" class="modal-backdrop" @click.self="detailOpNo = ''"><section class="dialog detail-dialog" role="dialog" aria-labelledby="detail-title"><header class="dialog-header"><div><p class="eyebrow">RT202 · LoadLotBasisJoinState · Status 0|1|2|3</p><h2 id="detail-title">{{ detailOpNo }} 批次明細</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="detailOpNo = ''">×</button></header><p class="dialog-note">以下為依狀態與 ProductNo 排序的 DEV 假資料。</p><div class="table-wrap"><table><thead><tr><th>LotNo</th><th>ProductType</th><th>Status</th><th>CurQty</th><th>CurUnitNo</th></tr></thead><tbody><tr v-for="row in lotRows" :key="row.lotNo"><td>{{ row.lotNo }}</td><td>{{ row.productType }}</td><td>{{ row.status }}</td><td>{{ formatQty(row.curQty) }}</td><td>{{ row.unitNo }}</td></tr></tbody></table></div><footer class="dialog-actions"><button type="button" class="primary-button" @click="detailOpNo = ''">關閉</button></footer></section></div>
    <div v-if="contextMenu" class="context-backdrop" @click="contextMenu = null"><div class="context-menu" :style="{ left: `${contextMenu.x}px`, top: `${contextMenu.y}px` }" role="menu" @click.stop><button type="button" role="menuitem" @click="simulateCopy(contextMenu.opNo)">Copy To Clipboard</button><button v-if="operationCards.find((card) => card.opNo === contextMenu?.opNo)?.totalLots || operationCards.find((card) => card.opNo === contextMenu?.opNo)?.totalQty" type="button" role="menuitem" @click="openLotDetail(contextMenu.opNo)">Lot Detail</button></div></div>
    <div v-if="notice" class="toast" role="status">{{ notice }}</div>
  </section>
</template>

<style scoped>
.product-type-board{box-sizing:border-box;height:100%;min-height:0;padding:22px 26px;display:flex;flex-direction:column;gap:14px;overflow:auto;background:#eef2f5;color:#1c2a37;font-family:Inter,"Noto Sans TC",sans-serif}
.page-heading,.toolbar,.status-line,.actions,.dialog-header,.dialog-actions,.section-title,.page-footer{display:flex;align-items:center;justify-content:space-between;gap:12px}
.page-heading h1{margin:3px 0 4px;font-size:24px}.eyebrow{margin:0;color:#527a99;font-size:11px;font-weight:800;letter-spacing:.12em}.subtitle{margin:0;color:#6f8190;font-size:12px}.icon-button{border:1px solid #d3dde5;background:#fff;color:#536777;border-radius:7px;cursor:pointer}.page-heading>.icon-button{width:36px;height:36px;font-size:24px}
.toolbar{padding:9px 13px;background:#fff;border:1px solid #dce4ea;border-radius:8px}.status-line{justify-content:flex-start;flex-wrap:wrap;color:#627789;font-size:11px}.status-line strong{color:#263a4a}.live-dot{width:8px;height:8px;border-radius:50%;background:#27ad78;box-shadow:0 0 0 3px #27ad782b}.actions{gap:8px}.primary-button,.secondary-button{height:32px;padding:0 12px;border-radius:6px;font:inherit;font-size:11px;font-weight:700;cursor:pointer}.primary-button{border:1px solid #166fbb;background:#176fb6;color:#fff}.secondary-button{border:1px solid #d0dbe3;background:#fff;color:#40596c}
.board-grid{flex:1;min-height:0;display:grid;grid-template-columns:repeat(4,minmax(220px,1fr));grid-auto-rows:minmax(280px,1fr);gap:12px;overflow:auto;padding:2px}.board-card{min-height:270px;padding:11px 12px 9px;border:1px solid #d8e1e7;border-radius:9px;background:#fff;box-shadow:0 3px 13px #1c35420b}.card-heading{display:flex;justify-content:space-between;align-items:flex-start;gap:7px}.card-heading strong,.card-heading span{display:block}.card-heading strong{font-size:var(--tile-font,18px)}.card-heading div>span{margin-top:3px;color:#718493;font-size:10px}.total-badge{padding:4px 7px;border:1px solid #dce6ec;border-radius:5px;background:#f3f7f9;color:#385267;font-size:10px;font-weight:800;white-space:nowrap}
.state-chart{display:block;width:100%;height:auto;max-height:170px;margin:5px auto 0;overflow:visible}.axis-line{stroke:#d4dee5;stroke-width:1}.axis-label{fill:#758796;font-size:calc(var(--tile-font,18px) - 6px)}.bar-total{fill:#455b6c;font-size:9px;font-weight:700}.bar-segment{cursor:pointer}.bar-segment:hover{filter:brightness(1.12);stroke:#1c4f75;stroke-width:1.5}.product-legend,.expanded-legend{display:flex;flex-wrap:wrap;gap:6px 10px;margin-top:2px}.product-legend span,.expanded-legend span{display:flex;align-items:center;gap:5px;color:#637787;font-size:9px}.product-legend i,.expanded-legend i{width:8px;height:8px;border-radius:2px}.state-summary{display:grid;grid-template-columns:repeat(4,1fr);gap:3px;margin-top:7px;padding-top:6px;border-top:1px solid #edf0f2}.state-summary span{display:grid;grid-template-columns:8px auto;align-items:center;gap:3px 4px;color:#718493;font-size:8px}.state-summary i{grid-row:span 2;width:7px;height:7px;border-radius:2px}.state-summary strong{font-size:8px}.state-summary small{grid-column:2;color:#40596c;font-size:8px}.card-footer{display:flex;justify-content:space-between;gap:6px;margin-top:7px;padding-top:6px;border-top:1px solid #edf0f2;color:#8896a0;font-size:8px}.page-footer{flex-wrap:wrap;color:#697c8a;font-size:10px}.dev-note{margin-left:auto;color:#927b4d}.empty-state{flex:1;display:grid;place-content:center;justify-items:center;gap:12px;border:1px dashed #bbc9d3;border-radius:10px;background:#fff;color:#506779}
.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:20px;background:#11233575;backdrop-filter:blur(2px)}.dialog{box-sizing:border-box;width:min(900px,100%);max-height:88vh;overflow:auto;padding:20px;border:1px solid #d4dee5;border-radius:12px;background:#fff;color:#203344;box-shadow:0 24px 70px #172a3c42}.dialog h2{margin:4px 0 0;font-size:19px}.dialog-header{margin-bottom:13px}.setting-fields{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:16px 0}.setting-fields label{display:grid;gap:7px;color:#546c7f;font-size:11px;font-weight:700}.setting-fields input{box-sizing:border-box;height:35px;padding:0 9px;border:1px solid #d3dde4;border-radius:5px;background:#fff;color:#203344;font:inherit}.operation-list{display:grid}.section-title{padding-bottom:8px;border-bottom:1px solid #e4ebef;font-size:12px}.section-title span{color:#7a8b98;font-size:10px}.operation-option{display:grid;grid-template-columns:18px 62px 1fr;align-items:center;gap:8px;padding:9px 1px;border-bottom:1px solid #edf1f3;color:#405669;font-size:11px}.operation-option input{accent-color:#176fb6}.operation-option span{color:#718493}.dialog-note{margin:12px 0;color:#7a8c99;font-size:10px;line-height:1.55}.form-error{margin:10px 0 0;color:#bb3030;font-size:11px}.dialog-actions{justify-content:flex-end;margin-top:15px}.large-total{text-align:center;color:#314d61;font-size:22px;font-weight:800}.product-dialog,.detail-dialog{width:min(1120px,96vw)}.table-wrap{max-height:56vh;overflow:auto;border:1px solid #e0e7ec;border-radius:6px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th,td{padding:9px 10px;border-bottom:1px solid #e8edf0;text-align:left}thead th{position:sticky;top:0;background:#f2f6f8;color:#4c6476}th small{display:block;margin-top:3px;color:#85949f;font-size:8px;font-weight:500}td{color:#607482}.expanded-dialog{width:min(900px,100%)}.expanded-chart{display:block;width:100%;height:auto;max-height:460px}.expanded-chart .bar-segment{cursor:pointer}.expanded-legend{justify-content:center;margin:16px}.context-backdrop{position:fixed;inset:0;z-index:90}.context-menu{position:fixed;display:grid;min-width:178px;padding:5px;border:1px solid #d3dde4;border-radius:7px;background:#fff;box-shadow:0 12px 30px #172a3c36}.context-menu button{padding:9px 11px;border:0;border-radius:4px;background:transparent;color:#31495b;text-align:left;font:inherit;font-size:11px;cursor:pointer}.context-menu button:hover{background:#edf4f8}.toast{position:fixed;right:24px;bottom:24px;z-index:110;max-width:420px;padding:11px 14px;border:1px solid #c6d4dd;border-radius:7px;background:#fff;color:#31495b;font-size:11px;box-shadow:0 8px 30px #172a3c32}
@media(max-width:1120px){.board-grid{grid-template-columns:repeat(3,minmax(210px,1fr))}}@media(max-width:820px){.board-grid{grid-template-columns:repeat(2,minmax(205px,1fr))}}@media(max-width:600px){.product-type-board{padding:14px;gap:10px}.page-heading h1{font-size:19px}.toolbar{align-items:flex-start;flex-direction:column}.status-line{gap:8px}.actions{width:100%;flex-wrap:wrap}.board-grid{grid-template-columns:1fr;grid-auto-rows:minmax(280px,auto)}.board-card{min-height:270px}.page-footer{align-items:flex-start;flex-direction:column;gap:6px}.dev-note{margin-left:0}.state-summary{grid-template-columns:repeat(2,1fr)}}
</style>

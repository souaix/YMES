<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

interface Operation { opNo: string; opName: string; standardYield: number }
interface Interval { label: string; from: number; to: number }
interface BoardConfig { refreshSeconds: number; columnWidth: number; rowHeight: number; fontSize: number; selectedOps: string[] }
type MetricName = 'GoodQty' | 'ScrapQty' | 'DefectQty' | 'Accumulate GoodQty' | 'Yield'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const operations: Operation[] = [
  { opNo: 'OP10', opName: 'Wafer Sort', standardYield: 0.985 },
  { opNo: 'OP20', opName: 'Wafer Probe', standardYield: 0.98 },
  { opNo: 'OP30', opName: 'Die Attach', standardYield: 0.975 },
  { opNo: 'OP40', opName: 'Wire Bond', standardYield: 0.99 },
  { opNo: 'OP50', opName: 'Mold & Cure', standardYield: 0.985 },
  { opNo: 'OP60', opName: 'Final Test', standardYield: 0.99 },
]
const metrics: MetricName[] = ['GoodQty', 'ScrapQty', 'DefectQty', 'Accumulate GoodQty', 'Yield']
const intervalBasis: Interval[] = Array.from({ length: 12 }, (_, index) => {
  const from = (6 + index * 2) % 24
  const to = (from + 2) % 24
  const timeText = (hour: number) => `${String(hour).padStart(2, '0')}:00`
  return { label: `${timeText(from)}\n${timeText(to)}`, from, to }
})

const config = ref<BoardConfig>({ refreshSeconds: 60, columnWidth: 104, rowHeight: 43, fontSize: 15, selectedOps: ['OP10', 'OP20', 'OP30', 'OP40'] })
const refreshSequence = ref(0)
const refreshedAt = ref(new Date())
const setupOpen = ref(false)
const historyOpen = ref(false)
const setupError = ref('')
const historyError = ref('')
const setupDraft = ref<BoardConfig>({ ...config.value, selectedOps: [...config.value.selectedOps] })
const historyDate = ref(`${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-${String(new Date().getDate()).padStart(2, '0')}`)
const historyDraftOps = ref([...config.value.selectedOps])
const historyOps = ref([...config.value.selectedOps])
const historyResultDate = ref('')
const historyRefresh = ref(0)
const timerText = ref('')
let refreshTimer: ReturnType<typeof setInterval> | undefined
let countdownTimer: ReturnType<typeof setInterval> | undefined
let nextRefreshAt = Date.now() + config.value.refreshSeconds * 1000

const currentBusinessDate = computed(() => {
  const now = new Date()
  const value = new Date(now)
  if (now.getHours() < 6) value.setDate(value.getDate() - 1)
  return value.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit' })
})
const lastUpdated = computed(() => refreshedAt.value.toLocaleTimeString('zh-TW', { hour12: false }))
const selectedOperations = computed(() => operations.filter((operation) => config.value.selectedOps.includes(operation.opNo)))
const nowHour = computed(() => new Date().getHours() + new Date().getMinutes() / 60)
const activeIntervalIndex = computed(() => {
  const relativeHour = (nowHour.value - 6 + 24) % 24
  return Math.min(11, Math.floor(relativeHour / 2))
})
const boardRows = computed(() => selectedOperations.value.map((operation, operationIndex) => {
  let accumulatedGood = 0
  const values = intervalBasis.map((interval, intervalIndex) => {
    const inCurrentProductionDay = intervalIndex <= activeIntervalIndex.value
    if (!inCurrentProductionDay) return null
    const good = 82 + operationIndex * 17 + intervalIndex * 9 + ((intervalIndex + operationIndex + refreshSequence.value) % 4) * 3
    const scrap = 1 + ((operationIndex + intervalIndex) % 3)
    const defect = (operationIndex + intervalIndex) % 2
    accumulatedGood += good
    const denominator = good + scrap
    const yieldValue = denominator === 0 ? null : (good - defect) / denominator
    return { GoodQty: good, ScrapQty: scrap, DefectQty: defect, 'Accumulate GoodQty': accumulatedGood, Yield: yieldValue }
  })
  return { operation, values }
}))
const historyRows = computed(() => {
  const ops = operations.filter((operation) => historyOps.value.includes(operation.opNo))
  return ops.map((operation, operationIndex) => {
    let accumulatedGood = 0
    const values = intervalBasis.map((_, intervalIndex) => {
      const good = 76 + operationIndex * 15 + intervalIndex * 8 + (historyRefresh.value % 3)
      const scrap = 1 + ((operationIndex + intervalIndex + 1) % 3)
      const defect = (operationIndex + intervalIndex + 1) % 2
      accumulatedGood += good
      return { GoodQty: good, ScrapQty: scrap, DefectQty: defect, 'Accumulate GoodQty': accumulatedGood, Yield: (good - defect) / (good + scrap) }
    })
    return { operation, values }
  })
})

function refreshBoard() {
  refreshSequence.value += 1
  refreshedAt.value = new Date()
  nextRefreshAt = Date.now() + config.value.refreshSeconds * 1000
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = setInterval(refreshBoard, config.value.refreshSeconds * 1000)
  }
}
function updateCountdown() {
  const seconds = Math.max(0, Math.ceil((nextRefreshAt - Date.now()) / 1000))
  timerText.value = `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}
function restartRefreshTimer() {
  if (refreshTimer) clearInterval(refreshTimer)
  nextRefreshAt = Date.now() + config.value.refreshSeconds * 1000
  refreshTimer = setInterval(refreshBoard, config.value.refreshSeconds * 1000)
  updateCountdown()
}
function openSetup() {
  setupDraft.value = { ...config.value, selectedOps: [...config.value.selectedOps] }
  setupError.value = ''
  setupOpen.value = true
}
function saveSetup() {
  const { refreshSeconds, columnWidth, rowHeight, fontSize, selectedOps } = setupDraft.value
  if (![refreshSeconds, columnWidth, rowHeight, fontSize].every((value) => Number.isInteger(value) && value > 0)) {
    setupError.value = '更新秒數、欄寬、列高和字級需為大於 0 的整數。'
    return
  }
  if (!selectedOps.length) {
    setupError.value = '請至少選擇一個作業站。'
    return
  }
  config.value = { ...setupDraft.value, selectedOps: [...setupDraft.value.selectedOps] }
  setupOpen.value = false
  restartRefreshTimer()
  refreshBoard()
}
function openHistory() {
  historyDraftOps.value = [...config.value.selectedOps]
  historyOps.value = [...config.value.selectedOps]
  historyResultDate.value = ''
  historyError.value = ''
  historyOpen.value = true
}
function queryHistory() {
  if (!historyDraftOps.value.length) {
    historyError.value = '請選擇至少一個作業站。'
    return
  }
  historyOps.value = [...historyDraftOps.value]
  historyError.value = ''
  historyResultDate.value = historyDate.value
  historyRefresh.value += 1
}
function displayMetric(metric: MetricName, value: number | null | undefined) {
  if (value == null) return '—'
  if (metric === 'Yield') return `${(value * 100).toFixed(1)}%`
  return Number(value).toLocaleString('en-US', { maximumFractionDigits: 2 })
}
function isBelowStandard(operation: Operation, metric: MetricName, value: number | null | undefined) {
  return metric === 'Yield' && value != null && operation.standardYield > 0 && value < operation.standardYield
}

onMounted(() => {
  restartRefreshTimer()
  countdownTimer = setInterval(updateCountdown, 1000)
})
onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer)
  if (countdownTimer) clearInterval(countdownTimer)
})
</script>

<template>
  <section class="op-output-page" aria-label="作業站時段產出看板">
    <header class="page-heading">
      <div>
        <p class="eyebrow">RPT · RT205</p>
        <h1>作業站時段產出看板</h1>
        <p class="subheading">依作業站彙整各時段良品、報廢、缺點、累計良品與良率。</p>
      </div>
      <button class="close-button" type="button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="toolbar">
      <div class="date-status"><span class="live-dot" />歸屬日 <strong>{{ currentBusinessDate }}</strong><span class="divider">|</span>最後更新 {{ lastUpdated }}<span class="refresh-count">下一次 {{ timerText }}</span></div>
      <div class="actions"><button type="button" class="secondary-button" @click="openHistory">歷史查詢</button><button type="button" class="secondary-button" @click="openSetup">看板設定</button><button type="button" class="primary-button" @click="refreshBoard">立即更新</button></div>
    </div>

    <div v-if="!selectedOperations.length" class="empty-state">
      <strong>尚未選擇作業站</strong>
      <p>請開啟看板設定，選取要顯示的作業站。</p>
      <button type="button" class="primary-button" @click="openSetup">設定作業站</button>
    </div>
    <div v-else class="table-frame">
      <table class="output-grid" :style="{ '--interval-width': `${config.columnWidth}px`, '--row-height': `${config.rowHeight}px`, '--board-font': `${config.fontSize}px` }">
        <thead><tr><th class="op-heading">OP No</th><th class="metric-heading">產出項目</th><th v-for="(interval, index) in intervalBasis" :key="interval.label" :class="['time-heading', { active: index === activeIntervalIndex }]">{{ interval.label.split('\n')[0] }}<span>至 {{ interval.label.split('\n')[1] }}</span><small v-if="index === activeIntervalIndex">目前時段</small></th></tr></thead>
        <tbody>
          <template v-for="(group, groupIndex) in boardRows" :key="group.operation.opNo">
            <tr v-for="(metric, metricIndex) in metrics" :key="`${group.operation.opNo}-${metric}`" :class="[{ 'group-even': groupIndex % 2 === 1 }, { 'yield-row': metric === 'Yield' }]" :style="{ height: `${config.rowHeight}px` }">
              <th v-if="metricIndex === 0" class="op-cell" rowspan="5" :title="group.operation.opName"><strong>{{ group.operation.opNo }}</strong><span>{{ group.operation.opName }}</span></th>
              <th class="metric-cell">{{ metric === 'GoodQty' ? 'GoodQty 良品' : metric === 'ScrapQty' ? 'ScrapQty 報廢' : metric === 'DefectQty' ? 'DefectQty 缺點' : metric === 'Accumulate GoodQty' ? '累計良品' : 'Yield 良率' }}</th>
              <td v-for="(value, intervalIndex) in group.values" :key="`${metric}-${intervalIndex}`" :class="{ 'below-standard': isBelowStandard(group.operation, metric, value?.[metric]), 'future-cell': !value }">{{ displayMetric(metric, value?.[metric]) }}</td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
    <footer class="board-footer"><span>顯示 {{ selectedOperations.length }} 個作業站 · 每站 5 項指標</span><span><i class="legend-red" />良率低於該作業站 StdYield 時標紅</span><span class="dev-note">DEV 假資料 · 更新間隔 {{ config.refreshSeconds }} 秒</span></footer>

    <div v-if="setupOpen" class="modal-backdrop" @click.self="setupOpen = false">
      <section class="dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="setup-title">
        <header class="dialog-header"><div><p class="eyebrow">PC 個人設定</p><h2 id="setup-title">看板設定</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="setupOpen = false">×</button></header>
        <div class="setting-fields">
          <label>更新秒數<input v-model.number="setupDraft.refreshSeconds" type="number" min="1" step="1"></label>
          <label>時段欄寬<input v-model.number="setupDraft.columnWidth" type="number" min="1" step="1"></label>
          <label>資料列高<input v-model.number="setupDraft.rowHeight" type="number" min="1" step="1"></label>
          <label>字級<input v-model.number="setupDraft.fontSize" type="number" min="1" step="1"></label>
        </div>
        <div class="operation-selector"><div class="section-title"><strong>顯示作業站</strong><span>{{ setupDraft.selectedOps.length }} 已選</span></div><label v-for="operation in operations" :key="operation.opNo" class="operation-option"><input v-model="setupDraft.selectedOps" type="checkbox" :value="operation.opNo"><span class="op-tag">{{ operation.opNo }}</span><span>{{ operation.opName }}</span><small>StdYield {{ (operation.standardYield * 100).toFixed(1) }}%</small></label></div>
        <p v-if="setupError" class="form-error" role="alert">{{ setupError }}</p>
        <footer class="dialog-actions"><button type="button" class="secondary-button" @click="setupOpen = false">取消</button><button type="button" class="primary-button" @click="saveSetup">套用設定</button></footer>
      </section>
    </div>

    <div v-if="historyOpen" class="modal-backdrop history-backdrop" @click.self="historyOpen = false">
      <section class="dialog history-dialog" role="dialog" aria-modal="true" aria-labelledby="history-title">
        <header class="dialog-header"><div><p class="eyebrow">唯讀查詢 · DEV 假資料</p><h2 id="history-title">時段產出歷史查詢</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="historyOpen = false">×</button></header>
        <div class="history-filters"><label>歸屬日期<input v-model="historyDate" type="date"></label><div><strong>作業站</strong><div class="history-op-list"><label v-for="operation in operations" :key="operation.opNo"><input v-model="historyDraftOps" type="checkbox" :value="operation.opNo">{{ operation.opNo }} {{ operation.opName }}</label></div></div><button type="button" class="primary-button query-button" @click="queryHistory">查詢</button></div>
        <p v-if="historyError" class="form-error" role="alert">{{ historyError }}</p>
        <div v-if="historyResultDate" class="history-results"><div class="history-result-title"><strong>{{ historyResultDate.replaceAll('-', '/') }} 歸屬日</strong><span>{{ historyRows.length }} 個作業站</span></div><div class="table-frame history-grid-frame"><table class="output-grid history-grid"><thead><tr><th class="op-heading">OP No</th><th class="metric-heading">產出項目</th><th v-for="interval in intervalBasis" :key="interval.label" class="time-heading">{{ interval.label.split('\n')[0] }}<span>至 {{ interval.label.split('\n')[1] }}</span></th></tr></thead><tbody><template v-for="(group, groupIndex) in historyRows" :key="group.operation.opNo"><tr v-for="(metric, metricIndex) in metrics" :key="`${group.operation.opNo}-${metric}`" :class="[{ 'group-even': groupIndex % 2 === 1 }, { 'yield-row': metric === 'Yield' }]" :style="{ height: '35px' }"><th v-if="metricIndex === 0" class="op-cell" rowspan="5"><strong>{{ group.operation.opNo }}</strong><span>{{ group.operation.opName }}</span></th><th class="metric-cell">{{ metric === 'GoodQty' ? 'GoodQty 良品' : metric === 'ScrapQty' ? 'ScrapQty 報廢' : metric === 'DefectQty' ? 'DefectQty 缺點' : metric === 'Accumulate GoodQty' ? '累計良品' : 'Yield 良率' }}</th><td v-for="(value, intervalIndex) in group.values" :key="`${metric}-${intervalIndex}`" :class="{ 'below-standard': isBelowStandard(group.operation, metric, value[metric]) }">{{ displayMetric(metric, value[metric]) }}</td></tr></template></tbody></table></div></div>
        <p v-else class="history-placeholder">選擇歸屬日期與作業站後查詢。此查詢只讀，不會修改資料。</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.op-output-page{box-sizing:border-box;height:100%;min-height:0;padding:24px 28px;display:flex;flex-direction:column;gap:18px;background:#f5f7fa;color:#172332;font-family:Inter,"Noto Sans TC",sans-serif;overflow:auto}.page-heading,.toolbar,.actions,.dialog-header,.dialog-actions,.section-title,.history-result-title,.board-footer{display:flex;align-items:center;justify-content:space-between;gap:16px}.page-heading h1{margin:3px 0 4px;font-size:25px;letter-spacing:.01em}.eyebrow{margin:0;color:#59738d;font-size:11px;font-weight:800;letter-spacing:.12em;text-transform:uppercase}.subheading{margin:0;color:#66778a;font-size:13px}.close-button,.icon-button{border:1px solid #d7dfe7;background:#fff;color:#516275;border-radius:8px;cursor:pointer}.close-button{width:36px;height:36px;font-size:24px}.toolbar{padding:12px 15px;background:#fff;border:1px solid #e0e7ee;border-radius:11px;box-shadow:0 4px 14px #18334e0b}.date-status{display:flex;align-items:center;gap:9px;color:#627489;font-size:12px}.date-status strong{color:#23384d}.live-dot{width:8px;height:8px;border-radius:50%;background:#1bad78;box-shadow:0 0 0 3px #e2f7ef}.divider{color:#d1d9e0}.refresh-count{font-variant-numeric:tabular-nums;color:#344c62}.actions{gap:8px}.primary-button,.secondary-button{height:34px;padding:0 13px;border-radius:7px;font:inherit;font-size:12px;font-weight:700;cursor:pointer}.primary-button{border:1px solid #176cbe;background:#176cbe;color:#fff}.secondary-button{border:1px solid #d4dde6;background:#fff;color:#334b61}.table-frame{width:100%;min-height:0;flex:1;overflow:auto;border:1px solid #d9e2ea;border-radius:11px;background:#fff;box-shadow:0 6px 22px #1c354c0a}.output-grid{border-collapse:separate;border-spacing:0;min-width:100%;width:max-content;font-size:var(--board-font,14px);font-variant-numeric:tabular-nums}.output-grid th,.output-grid td{border-right:1px solid #e4eaf0;border-bottom:1px solid #e4eaf0;padding:8px 10px;text-align:right;white-space:nowrap}.output-grid thead th{position:sticky;top:0;z-index:2;background:#e8f4f1;color:#24483f;font-weight:800}.output-grid .op-heading{left:0;z-index:4;min-width:105px;text-align:left}.output-grid .metric-heading{left:126px;z-index:4;min-width:165px;text-align:left}.time-heading{width:var(--interval-width,104px);min-width:var(--interval-width,104px);line-height:1.25;text-align:center!important}.time-heading span,.time-heading small{display:block;font-size:10px;font-weight:600;color:#6e807e}.time-heading small{margin-top:3px;color:#b36c19}.time-heading.active{background:#d9eee7}.op-cell{position:sticky;left:0;z-index:1;min-width:105px;background:#fff;text-align:left!important;vertical-align:middle}.op-cell strong,.op-cell span{display:block}.op-cell span{margin-top:4px;color:#728194;font-size:11px;font-weight:500}.metric-cell{position:sticky;left:126px;z-index:1;min-width:165px;background:#fff;text-align:left!important;color:#41566b;font-weight:600}.output-grid tbody tr{height:var(--row-height,43px)}.output-grid tbody tr.group-even .op-cell,.output-grid tbody tr.group-even .metric-cell{background:#f2f8fc}.output-grid tbody tr.group-even td{background:#f5f9fc}.output-grid tbody tr.yield-row .metric-cell{color:#365c4e}.output-grid td.below-standard{color:#cb3131;font-weight:800}.output-grid td.future-cell{color:#bbc5ce;background:#fbfcfd}.board-footer{flex-wrap:wrap;color:#64778a;font-size:11px;padding:0 3px}.board-footer>span{display:flex;align-items:center;gap:6px}.dev-note{margin-left:auto;color:#8a6a35}.legend-red{display:inline-block;width:8px;height:8px;border-radius:50%;background:#d64242}.empty-state{display:grid;place-items:center;align-content:center;flex:1;gap:10px;border:1px dashed #bdcad5;border-radius:12px;background:#fff;color:#486176}.empty-state p{margin:0;color:#758596;font-size:13px}.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:20px;background:#15243675;backdrop-filter:blur(2px)}.dialog{width:min(620px,100%);max-height:min(88vh,850px);overflow:auto;padding:23px;border-radius:14px;background:#fff;box-shadow:0 24px 70px #0d20384a}.dialog h2{margin:4px 0 0;font-size:21px}.icon-button{width:32px;height:32px;font-size:20px}.setting-fields{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:11px;margin:22px 0}.setting-fields label,.history-filters>label{display:grid;gap:7px;color:#50657a;font-size:12px;font-weight:700}.setting-fields input,.history-filters input[type=date]{box-sizing:border-box;width:100%;height:36px;padding:0 9px;border:1px solid #d5dee7;border-radius:6px;color:#23384d;font:inherit;font-size:13px}.section-title{padding:0 0 9px;border-bottom:1px solid #e6ecf1;font-size:13px}.section-title span,.history-result-title span{color:#748598;font-size:11px}.operation-selector{display:grid;gap:0}.operation-option{display:grid;grid-template-columns:18px 66px 1fr auto;align-items:center;gap:9px;padding:10px 2px;border-bottom:1px solid #edf1f4;color:#34495e;font-size:12px}.operation-option input,.history-op-list input{accent-color:#176cbe}.op-tag{font-weight:800;color:#244b6c}.operation-option small{color:#788899;font-size:10px}.form-error{margin:12px 0 0;color:#c22d36;font-size:12px}.dialog-actions{justify-content:flex-end;margin-top:19px}.history-dialog{width:min(1100px,100%)}.history-filters{display:grid;grid-template-columns:175px 1fr auto;align-items:end;gap:17px;margin:20px 0 15px}.history-filters>div>strong{display:block;margin-bottom:7px;color:#50657a;font-size:12px}.history-op-list{display:flex;flex-wrap:wrap;gap:7px 14px}.history-op-list label{display:flex;align-items:center;gap:5px;color:#354b60;font-size:11px}.query-button{min-width:76px}.history-results{display:grid;gap:9px}.history-result-title{font-size:12px}.history-grid-frame{max-height:56vh}.history-grid{font-size:11px}.history-grid .op-heading{min-width:95px}.history-grid .metric-heading{left:116px;min-width:145px}.history-grid .op-cell{min-width:95px}.history-grid .metric-cell{left:116px;min-width:145px}.history-placeholder{margin:28px 0 15px;padding:28px;text-align:center;border:1px dashed #ccd6df;border-radius:8px;color:#718294;font-size:12px}
@media(max-width:720px){.op-output-page{padding:15px;gap:12px}.page-heading h1{font-size:20px}.toolbar{align-items:flex-start;flex-direction:column}.date-status{flex-wrap:wrap}.actions{width:100%;flex-wrap:wrap}.setting-fields{grid-template-columns:repeat(2,minmax(0,1fr))}.history-filters{grid-template-columns:1fr}.history-op-list{max-height:115px;overflow:auto}.board-footer{align-items:flex-start;flex-direction:column;gap:7px}.dev-note{margin-left:0}}
</style>

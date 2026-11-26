<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

interface Operation { opNo: string; opName: string; standardYield: number; baseGood: number; baseScrap: number }
interface Interval { label: string }
interface GaugeCard { id: string; opNo: string; opName: string; interval: string; intervalIndex: number; good: number; scrap: number; defect: number; accumulatedGood: number; yieldValue: number; standardYield: number }
type OrderMode = 'op' | 'time'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const operations: Operation[] = [
  { opNo: 'OP10', opName: 'Wafer Sort', standardYield: 0.985, baseGood: 92, baseScrap: 2 },
  { opNo: 'OP20', opName: 'Wafer Probe', standardYield: 0.98, baseGood: 72, baseScrap: 14 },
  { opNo: 'OP30', opName: 'Die Attach', standardYield: 0.975, baseGood: 67, baseScrap: 24 },
  { opNo: 'OP40', opName: 'Wire Bond', standardYield: 0.99, baseGood: 53, baseScrap: 36 },
  { opNo: 'OP50', opName: 'Mold & Cure', standardYield: 0.985, baseGood: 87, baseScrap: 7 },
  { opNo: 'OP60', opName: 'Final Test', standardYield: 0.99, baseGood: 78, baseScrap: 11 },
]
const intervals: Interval[] = Array.from({ length: 12 }, (_, index) => {
  const from = (6 + index * 2) % 24
  const to = (from + 2) % 24
  const hhmm = (hour: number) => `${String(hour).padStart(2, '0')}:00`
  return { label: `${hhmm(from)} – ${hhmm(to)}` }
})
const yieldContainDefect = '0'

const selectedOps = ref(['OP10', 'OP20', 'OP30', 'OP40'])
const refreshSeconds = ref(60)
const fontSize = ref(13)
const orderMode = ref<OrderMode>('op')
const settingsOpen = ref(false)
const draftOps = ref([...selectedOps.value])
const draftRefresh = ref(refreshSeconds.value)
const draftFontSize = ref(fontSize.value)
const settingsError = ref('')
const refreshCount = ref(0)
const updatedAt = ref(new Date())
const countdown = ref('')
let timer: ReturnType<typeof setInterval> | undefined
let countdownTimer: ReturnType<typeof setInterval> | undefined
let nextRefreshAt = Date.now() + refreshSeconds.value * 1000

const belongDate = computed(() => {
  const now = new Date()
  const date = new Date(now)
  if (now.getHours() < 6) date.setDate(date.getDate() - 1)
  return date.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit' })
})
const activeIntervalCount = computed(() => {
  const now = new Date()
  const hourFromSix = (now.getHours() + now.getMinutes() / 60 - 6 + 24) % 24
  return Math.min(12, Math.floor(hourFromSix / 2) + 1)
})
const operationList = computed(() => operations.filter((operation) => selectedOps.value.includes(operation.opNo)))
const showDefectQty = computed(() => yieldContainDefect === '0')
const allCards = computed<GaugeCard[]>(() => {
  const output: GaugeCard[] = []
  for (const operation of operations) {
    let operationAccumulated = 0
    for (let intervalIndex = 0; intervalIndex < activeIntervalCount.value; intervalIndex += 1) {
      const good = operation.baseGood + intervalIndex * 4 + ((intervalIndex + refreshCount.value) % 3) * 2
      const scrap = operation.baseScrap + ((intervalIndex + 1) % 3)
      const defect = (intervalIndex + operations.indexOf(operation)) % 3
      operationAccumulated += good
      const total = good + scrap
      output.push({
        id: `${operation.opNo}-${intervalIndex}`,
        opNo: operation.opNo,
        opName: operation.opName,
        interval: intervals[intervalIndex].label,
        intervalIndex,
        good,
        scrap,
        defect,
        accumulatedGood: operationAccumulated,
        yieldValue: total > 0 ? ((yieldContainDefect === '0' ? good - defect : good) / total) * 100 : 0,
        standardYield: operation.standardYield * 100,
      })
    }
  }
  return output.filter((card) => selectedOps.value.includes(card.opNo))
})
const visibleCards = computed(() => orderMode.value === 'op'
  ? [...allCards.value].sort((left, right) => operations.findIndex((item) => item.opNo === left.opNo) - operations.findIndex((item) => item.opNo === right.opNo) || left.intervalIndex - right.intervalIndex)
  : [...allCards.value].sort((left, right) => left.intervalIndex - right.intervalIndex || operations.findIndex((item) => item.opNo === left.opNo) - operations.findIndex((item) => item.opNo === right.opNo)))
const updatedTime = computed(() => updatedAt.value.toLocaleTimeString('zh-TW', { hour12: false }))

function pointOnArc(angle: number, radius = 58) {
  const radians = (angle * Math.PI) / 180
  return { x: 80 + radius * Math.cos(radians), y: 78 + radius * Math.sin(radians) }
}
function arcPath(start: number, end: number, radius = 58) {
  const from = pointOnArc(start, radius)
  const to = pointOnArc(end, radius)
  const large = end - start > 180 ? 1 : 0
  return `M ${from.x.toFixed(2)} ${from.y.toFixed(2)} A ${radius} ${radius} 0 ${large} 1 ${to.x.toFixed(2)} ${to.y.toFixed(2)}`
}
function needlePoint(value: number) {
  const point = pointOnArc(135 + (Math.max(0, Math.min(100, value)) * 2.7), 45)
  return point
}
function arcForValue(value: number) {
  return arcPath(135, 135 + (Math.max(0, Math.min(100, value)) * 2.7), 52)
}
function refreshBoard() {
  refreshCount.value += 1
  updatedAt.value = new Date()
  nextRefreshAt = Date.now() + refreshSeconds.value * 1000
  if (timer) {
    clearInterval(timer)
    timer = setInterval(refreshBoard, refreshSeconds.value * 1000)
  }
}
function updateCountdown() {
  const remaining = Math.max(0, Math.ceil((nextRefreshAt - Date.now()) / 1000))
  countdown.value = `${Math.floor(remaining / 60)}:${String(remaining % 60).padStart(2, '0')}`
}
function restartTimer() {
  if (timer) clearInterval(timer)
  nextRefreshAt = Date.now() + refreshSeconds.value * 1000
  timer = setInterval(refreshBoard, refreshSeconds.value * 1000)
  updateCountdown()
}
function openSettings() {
  draftOps.value = [...selectedOps.value]
  draftRefresh.value = refreshSeconds.value
  draftFontSize.value = fontSize.value
  settingsError.value = ''
  settingsOpen.value = true
}
function saveSettings() {
  if (!Number.isInteger(draftRefresh.value) || draftRefresh.value < 1 || !Number.isInteger(draftFontSize.value) || draftFontSize.value < 8) {
    settingsError.value = '更新秒數需為正整數，字級至少為 8。'
    return
  }
  if (!draftOps.value.length) {
    settingsError.value = '至少選擇一個作業站。'
    return
  }
  selectedOps.value = [...draftOps.value]
  refreshSeconds.value = draftRefresh.value
  fontSize.value = draftFontSize.value
  settingsOpen.value = false
  restartTimer()
  refreshBoard()
}
function qualityTone(value: number) {
  if (value < 60) return 'poor'
  if (value < 80) return 'watch'
  return 'good'
}

onMounted(() => {
  restartTimer()
  countdownTimer = setInterval(updateCountdown, 1000)
})
onUnmounted(() => {
  if (timer) clearInterval(timer)
  if (countdownTimer) clearInterval(countdownTimer)
})
</script>

<template>
  <section class="yield-gauge-page" aria-label="作業站時段產出良率儀表板">
    <header class="page-heading">
      <div><p class="eyebrow">RPT · RT206</p><h1>作業站時段產出｜良率儀表板</h1><p class="subheading">每個作業站與已開始時段各顯示一座良率 Gauge。</p></div>
      <button type="button" class="close-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="toolbar">
      <div class="date-status"><span class="live-dot" />歸屬日 <strong>{{ belongDate }}</strong><span class="divider">|</span>最後更新 {{ updatedTime }}<span class="refresh-count">下次 {{ countdown }}</span></div>
      <div class="toolbar-actions"><fieldset class="sort-switch"><legend>排序</legend><label :class="{ selected: orderMode === 'op' }"><input v-model="orderMode" type="radio" value="op">依 OP</label><label :class="{ selected: orderMode === 'time' }"><input v-model="orderMode" type="radio" value="time">依時段</label></fieldset><button type="button" class="secondary-button" @click="openSettings">看板設定</button><button type="button" class="primary-button" @click="refreshBoard">立即更新</button></div>
    </div>
    <div v-if="!activeIntervalCount" class="empty-state">目前尚未進入設定的第一個生產時段。</div>
    <div v-else-if="!visibleCards.length" class="empty-state"><strong>尚未選擇作業站</strong><button type="button" class="primary-button" @click="openSettings">設定作業站</button></div>
    <div v-else class="gauge-grid" :style="{ '--tile-font': `${fontSize}px` }">
      <article v-for="card in visibleCards" :key="card.id" class="gauge-card" :class="qualityTone(card.yieldValue)" :title="`${card.opName} · ${card.interval}`">
        <header class="card-heading"><div><strong>{{ card.opNo }}</strong><span>{{ card.opName }}</span></div><span class="interval-badge">{{ card.interval }}</span></header>
        <div class="gauge-area">
          <svg class="yield-gauge" viewBox="0 0 160 128" role="img" :aria-label="`${card.opNo} ${card.interval} 良率 ${card.yieldValue.toFixed(1)}%`">
            <path class="dial-track" :d="arcPath(135, 405)" />
            <path class="dial-poor" :d="arcPath(135, 297)" />
            <path class="dial-watch" :d="arcPath(297, 351)" />
            <path class="dial-good" :d="arcPath(351, 405)" />
            <path class="dial-value" :class="qualityTone(card.yieldValue)" :d="arcForValue(card.yieldValue)" />
            <text x="19" y="103" class="gauge-label">0</text><text x="72" y="16" class="gauge-label">50</text><text x="137" y="103" class="gauge-label">100</text>
            <line class="needle" x1="80" y1="78" :x2="needlePoint(card.yieldValue).x" :y2="needlePoint(card.yieldValue).y" />
            <circle class="needle-hub" cx="80" cy="78" r="4.2" />
            <text x="80" y="105" text-anchor="middle" class="digital-value">{{ card.yieldValue.toFixed(1) }}%</text>
          </svg>
          <span class="quality-label" :class="qualityTone(card.yieldValue)">{{ card.yieldValue < 60 ? '低於 60%' : card.yieldValue < 80 ? '60–80%' : '80–100%' }}</span>
        </div>
        <div class="card-metrics"><div><span>GoodQty</span><strong>{{ card.good.toLocaleString() }}</strong></div><div><span>ScrapQty</span><strong>{{ card.scrap.toLocaleString() }}</strong></div><div v-if="showDefectQty"><span>DefectQty</span><strong>{{ card.defect.toLocaleString() }}</strong></div><div class="accumulated"><span>Accumulate GoodQty</span><strong>{{ card.accumulatedGood.toLocaleString() }}</strong></div></div>
      </article>
    </div>
    <footer class="page-footer"><span>{{ operationList.length }} 個作業站 · {{ activeIntervalCount }} 個已開始時段 · {{ visibleCards.length }} 座 Gauge</span><span class="gauge-legend"><i class="poor-key" />0–60%<i class="watch-key" />60–80%<i class="good-key" />80–100%</span><span class="dev-note">DEV 假資料 · Refresh {{ refreshSeconds }} 秒</span></footer>

    <div v-if="settingsOpen" class="modal-backdrop" @click.self="settingsOpen = false">
      <section class="dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title">
        <header class="dialog-header"><div><p class="eyebrow">SetupType 4 · DEV</p><h2 id="settings-title">良率看板設定</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="settingsOpen = false">×</button></header>
        <div class="setting-fields"><label>更新秒數<input v-model.number="draftRefresh" type="number" min="1"></label><label>字級<input v-model.number="draftFontSize" type="number" min="8"></label></div>
        <div class="operation-selector"><div class="section-title"><strong>作業站</strong><span>{{ draftOps.length }} 已選</span></div><label v-for="operation in operations" :key="operation.opNo" class="operation-option"><input v-model="draftOps" type="checkbox" :value="operation.opNo"><span class="op-tag">{{ operation.opNo }}</span><span>{{ operation.opName }}</span><small>StdYield {{ (operation.standardYield * 100).toFixed(1) }}%</small></label></div>
        <p class="settings-note">Legacy 設定透過 `wsRPT.LoadBillBoardOPStateConfig` 的 SetupType=4 載入；本 DEV 預覽不寫入任何後端。</p>
        <p v-if="settingsError" class="form-error" role="alert">{{ settingsError }}</p>
        <footer class="dialog-actions"><button type="button" class="secondary-button" @click="settingsOpen = false">取消</button><button type="button" class="primary-button" @click="saveSettings">套用</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.yield-gauge-page{box-sizing:border-box;height:100%;min-height:0;padding:22px 26px;display:flex;flex-direction:column;gap:16px;background:#f2f4f7;color:#172332;font-family:Inter,"Noto Sans TC",sans-serif;overflow:auto}.page-heading,.toolbar,.toolbar-actions,.date-status,.dialog-header,.dialog-actions,.section-title,.page-footer,.card-heading{display:flex;align-items:center;justify-content:space-between;gap:14px}.page-heading h1{margin:3px 0 4px;font-size:24px}.eyebrow{margin:0;color:#59738d;font-size:11px;font-weight:800;letter-spacing:.12em}.subheading{margin:0;color:#708093;font-size:12px}.close-button,.icon-button{border:1px solid #d7dfe7;background:#fff;color:#516275;border-radius:8px;cursor:pointer}.close-button{width:36px;height:36px;font-size:24px}.toolbar{padding:10px 13px;background:#fff;border:1px solid #e1e7ed;border-radius:10px}.date-status{justify-content:flex-start;color:#627489;font-size:11px}.date-status strong{color:#23384d}.live-dot{width:8px;height:8px;border-radius:50%;background:#1bad78;box-shadow:0 0 0 3px #e2f7ef}.divider{color:#d1d9e0}.refresh-count{font-variant-numeric:tabular-nums;color:#344c62}.toolbar-actions{gap:8px}.sort-switch{display:flex;align-items:center;gap:3px;margin:0;padding:2px;border:1px solid #d8e0e8;border-radius:7px}.sort-switch legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.sort-switch label{display:flex;align-items:center;gap:4px;padding:6px 8px;border-radius:5px;color:#627489;font-size:11px;cursor:pointer}.sort-switch label.selected{background:#e8f1f7;color:#244c6c;font-weight:800}.sort-switch input{accent-color:#176cbe}.primary-button,.secondary-button{height:32px;padding:0 12px;border-radius:7px;font:inherit;font-size:11px;font-weight:700;cursor:pointer}.primary-button{border:1px solid #176cbe;background:#176cbe;color:#fff}.secondary-button{border:1px solid #d4dde6;background:#fff;color:#334b61}.gauge-grid{flex:1;min-height:0;display:grid;grid-template-columns:repeat(4,minmax(220px,1fr));grid-auto-rows:minmax(250px,1fr);gap:13px;overflow:auto;padding:2px}.gauge-card{--tone:#258a55;display:flex;flex-direction:column;min-height:250px;padding:12px 13px 11px;border:1px solid #dbe3e9;border-radius:11px;background:#fff;box-shadow:0 4px 16px #192e410b}.gauge-card.poor{--tone:#b53d3d}.gauge-card.watch{--tone:#b0781c}.card-heading{align-items:flex-start}.card-heading strong,.card-heading span{display:block}.card-heading strong{font-size:13px;letter-spacing:.03em}.card-heading div>span{margin-top:2px;color:#7c8b99;font-size:10px}.interval-badge{padding:4px 7px;border:1px solid #e4eaf0;border-radius:5px;background:#f7f9fb;color:#53687b;font-size:10px;font-weight:700;white-space:nowrap}.gauge-area{position:relative;display:grid;place-items:center;flex:1;min-height:130px;padding-top:3px}.yield-gauge{width:min(100%,188px);height:140px;overflow:visible}.dial-track,.dial-poor,.dial-watch,.dial-good,.dial-value{fill:none;stroke-linecap:round}.dial-track{stroke:#e9edf0;stroke-width:10}.dial-poor{stroke:#c44848;stroke-width:7;opacity:.75}.dial-watch{stroke:#d69a32;stroke-width:7;opacity:.8}.dial-good{stroke:#31935d;stroke-width:7;opacity:.8}.dial-value{stroke:#234d70;stroke-width:3}.dial-value.poor{stroke:#a93636}.dial-value.watch{stroke:#a66b0e}.dial-value.good{stroke:#207e4e}.gauge-label{fill:#8d9aa5;font-size:8px;font-weight:600}.needle{stroke:#263e52;stroke-width:2.2;stroke-linecap:round}.needle-hub{fill:#263e52;stroke:#fff;stroke-width:1.3}.digital-value{fill:#243a4e;font-size:15px;font-weight:800;font-variant-numeric:tabular-nums}.quality-label{position:absolute;left:50%;bottom:1px;transform:translateX(-50%);color:#258a55;font-size:9px;font-weight:800}.quality-label.poor{color:#b53d3d}.quality-label.watch{color:#a66b0e}.card-metrics{display:grid;grid-template-columns:1fr 1fr;gap:6px 12px;padding-top:9px;border-top:1px solid #edf0f3}.card-metrics div{display:flex;justify-content:space-between;gap:4px;font-size:10px}.card-metrics span{color:#758494}.card-metrics strong{font-size:calc(var(--tile-font,13px) * .78);font-variant-numeric:tabular-nums;color:#283e52}.card-metrics .accumulated{grid-column:1/-1;padding-top:3px;border-top:1px dashed #e3e8ec}.page-footer{flex-wrap:wrap;color:#66788a;font-size:10px}.gauge-legend{display:flex;align-items:center;gap:6px}.gauge-legend i{width:8px;height:8px;border-radius:50%;display:inline-block;margin-left:5px}.poor-key{background:#c44848}.watch-key{background:#d69a32}.good-key{background:#31935d}.dev-note{margin-left:auto;color:#8a6a35}.empty-state{flex:1;display:grid;place-content:center;justify-items:center;gap:12px;border:1px dashed #bdcad5;border-radius:12px;background:#fff;color:#536a7c}.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:20px;background:#15243675;backdrop-filter:blur(2px)}.dialog{width:min(520px,100%);max-height:min(88vh,760px);overflow:auto;padding:22px;border-radius:14px;background:#fff;box-shadow:0 24px 70px #0d20384a}.dialog h2{margin:4px 0 0;font-size:20px}.icon-button{width:32px;height:32px;font-size:20px}.setting-fields{display:grid;grid-template-columns:1fr 1fr;gap:11px;margin:20px 0}.setting-fields label{display:grid;gap:7px;color:#50657a;font-size:11px;font-weight:700}.setting-fields input{box-sizing:border-box;height:35px;padding:0 9px;border:1px solid #d5dee7;border-radius:6px;font:inherit}.operation-selector{display:grid}.section-title{padding-bottom:8px;border-bottom:1px solid #e6ecf1;font-size:12px}.section-title span,.operation-option small{color:#788899;font-size:10px}.operation-option{display:grid;grid-template-columns:18px 60px 1fr auto;align-items:center;gap:8px;padding:9px 1px;border-bottom:1px solid #edf1f4;color:#354a5e;font-size:11px}.operation-option input{accent-color:#176cbe}.op-tag{font-weight:800;color:#244b6c}.settings-note{margin:12px 0 0;color:#7b8995;font-size:10px;line-height:1.5}.form-error{margin:11px 0 0;color:#bf3038;font-size:11px}.dialog-actions{justify-content:flex-end;margin-top:17px}
@media(max-width:1050px){.gauge-grid{grid-template-columns:repeat(3,minmax(205px,1fr))}}@media(max-width:760px){.yield-gauge-page{padding:14px;gap:11px}.page-heading h1{font-size:19px}.toolbar{align-items:flex-start;flex-direction:column}.date-status{flex-wrap:wrap}.toolbar-actions{flex-wrap:wrap;width:100%}.gauge-grid{grid-template-columns:repeat(2,minmax(165px,1fr));grid-auto-rows:minmax(235px,1fr)}.card-metrics div{font-size:9px}.page-footer{align-items:flex-start;flex-direction:column;gap:7px}.dev-note{margin-left:0}}@media(max-width:440px){.gauge-grid{grid-template-columns:1fr}.operation-option{grid-template-columns:18px 55px 1fr}.operation-option small{grid-column:3}}
</style>

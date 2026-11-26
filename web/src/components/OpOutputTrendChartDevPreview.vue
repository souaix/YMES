<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

interface Operation { opNo: string; opName: string; baseGood: number; baseScrap: number }
interface TimeStep { index: number; label: string; range: string; date: string }
interface Bucket extends TimeStep { active: boolean; good: number; scrap: number; defect: number; yieldValue: number }
interface OperationChart { opNo: string; opName: string; buckets: Bucket[] }
interface LotRow { lotNo: string; timeStep: string; endTime: string; goodQty: number; failQty: number; defectQty: number }
interface ChartSeries { key: 'good' | 'scrap' | 'defect'; label: string; color: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const operations: Operation[] = [
  { opNo: 'OP10', opName: 'Wafer Sort', baseGood: 156, baseScrap: 9 },
  { opNo: 'OP20', opName: 'Wafer Probe', baseGood: 132, baseScrap: 14 },
  { opNo: 'OP30', opName: 'Die Attach', baseGood: 116, baseScrap: 18 },
  { opNo: 'OP40', opName: 'Wire Bond', baseGood: 104, baseScrap: 23 },
  { opNo: 'OP50', opName: 'Mold & Cure', baseGood: 145, baseScrap: 11 },
  { opNo: 'OP60', opName: 'Final Test', baseGood: 126, baseScrap: 16 },
]
const yieldContainDefect = '0'
const showDefectQty = yieldContainDefect === '0'
const selectedOps = ref(['OP10', 'OP20', 'OP30', 'OP40'])
const refreshSeconds = ref(60)
const fontSize = ref(13)
const settingsOpen = ref(false)
const draftOps = ref([...selectedOps.value])
const draftRefresh = ref(refreshSeconds.value)
const draftFontSize = ref(fontSize.value)
const settingsError = ref('')
const refreshCount = ref(0)
const updatedAt = ref(new Date())
const businessClock = ref(new Date())
const countdown = ref('')
const expandedOpNo = ref('')
const detailOpNo = ref('')
const contextMenu = ref<{ x: number; y: number; opNo: string } | null>(null)
const notice = ref('')
let refreshTimer: ReturnType<typeof setInterval> | undefined
let countdownTimer: ReturnType<typeof setInterval> | undefined
let noticeTimer: ReturnType<typeof setTimeout> | undefined
let nextRefreshAt = Date.now() + refreshSeconds.value * 1000

const timeSteps = computed<TimeStep[]>(() => {
  const now = businessClock.value
  const start = new Date(now)
  start.setHours(6, 0, 0, 0)
  if (now.getHours() < 6) start.setDate(start.getDate() - 1)
  const pad = (value: number) => String(value).padStart(2, '0')
  return Array.from({ length: 12 }, (_, index) => {
    const from = new Date(start.getTime() + index * 2 * 60 * 60 * 1000)
    const to = new Date(from.getTime() + 2 * 60 * 60 * 1000)
    const format = (date: Date) => `${pad(date.getHours())}:00`
    return {
      index,
      label: `T${String(index + 1).padStart(2, '0')}`,
      range: `${format(from)} – ${format(to)}`,
      date: from.toLocaleDateString('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit' }),
    }
  })
})
const belongDate = computed(() => timeSteps.value[0]?.date ?? '')
const activeTimeStepCount = computed(() => {
  const now = businessClock.value
  const elapsedHours = (now.getHours() + now.getMinutes() / 60 - 6 + 24) % 24
  return Math.min(12, Math.floor(elapsedHours / 2) + 1)
})
const operationCharts = computed<OperationChart[]>(() => operations
  .filter((operation) => selectedOps.value.includes(operation.opNo))
  .map((operation, operationIndex) => {
    const buckets = timeSteps.value.map((timeStep, index): Bucket => {
      if (index >= activeTimeStepCount.value) return { ...timeStep, active: false, good: 0, scrap: 0, defect: 0, yieldValue: 0 }
      const good = operation.baseGood + index * 7 + ((index + operationIndex + refreshCount.value) % 4) * 3
      const scrap = operation.baseScrap + ((index + operationIndex) % 4)
      const defect = (index * 2 + operationIndex + refreshCount.value) % 5
      const yieldNumerator = yieldContainDefect === '0' ? good - defect : good
      const yieldValue = good + scrap === 0 ? 0 : (yieldNumerator / (good + scrap)) * 100
      return { ...timeStep, active: true, good, scrap, defect, yieldValue }
    })
    return { opNo: operation.opNo, opName: operation.opName, buckets }
  }))
const sharedQtyMax = computed(() => {
  const values = operationCharts.value.flatMap((operation) => operation.buckets.flatMap((bucket) =>
    [bucket.good, bucket.scrap, ...(showDefectQty ? [bucket.defect] : [])]))
  const largest = Math.max(1, ...values)
  return Math.ceil(largest / 50) * 50
})
const selectedOperation = computed(() => operations.find((operation) => operation.opNo === detailOpNo.value))
const detailRows = computed<LotRow[]>(() => {
  const operation = operations.find((item) => item.opNo === detailOpNo.value)
  if (!operation) return []
  const current = operationCharts.value.find((item) => item.opNo === operation.opNo)
  const active = current?.buckets.filter((bucket) => bucket.active).slice(-4) ?? []
  return active.flatMap((bucket, bucketIndex) => Array.from({ length: 2 }, (_, lotIndex) => {
    const goodQty = Math.max(1, Math.round((bucket.good ?? operation.baseGood) / (3 + lotIndex)))
    const failQty = Math.max(0, Math.round((bucket.scrap ?? operation.baseScrap) / (2 + lotIndex)))
    return {
      lotNo: `L${bucket.index + 1}${operation.opNo.slice(-2)}${String(bucketIndex * 2 + lotIndex + 1).padStart(3, '0')}`,
      timeStep: `${bucket.label} · ${bucket.range}`,
      endTime: `${bucket.date} ${bucket.range.split(' – ')[1]}`,
      goodQty,
      failQty,
      defectQty: Math.min(goodQty, (bucket.defect ?? 0) + lotIndex),
    }
  }))
})
const updatedTime = computed(() => updatedAt.value.toLocaleTimeString('zh-TW', { hour12: false }))
const chartSeries = computed<ChartSeries[]>(() => [
  { key: 'good', label: 'GoodQty', color: '#2f9862' },
  { key: 'scrap', label: 'ScrapQty', color: '#d4564d' },
  ...(showDefectQty ? [{ key: 'defect', label: 'DefectQty', color: '#4676bd' }] : []),
])

function xCenter(index: number) { return 48 + ((index + 0.5) * 694) / 12 }
function yQty(value: number) { return 198 - (value / sharedQtyMax.value) * 166 }
function yYield(value: number) { return 198 - (value / 100) * 166 }
function barX(index: number, seriesIndex: number) {
  const groupWidth = chartSeries.value.length * 11 + (chartSeries.value.length - 1) * 2
  return xCenter(index) - groupWidth / 2 + seriesIndex * 13
}
function barHeight(value: number) { return Math.max(0, (value / sharedQtyMax.value) * 166) }
function seriesValue(bucket: Bucket, key: ChartSeries['key']) { return bucket[key] ?? 0 }
function yieldPath(chart: OperationChart) {
  return chart.buckets
    .map((bucket, index) => `${index === 0 ? 'M' : 'L'} ${xCenter(bucket.index)} ${yYield(bucket.yieldValue)}`).join(' ')
}
function formatQty(value: number) { return value.toLocaleString('zh-TW', { maximumFractionDigits: 2 }) }
function refreshBoard() {
  refreshCount.value += 1
  updatedAt.value = new Date()
  businessClock.value = new Date()
  restartTimer()
}
function updateCountdown() {
  const remaining = Math.max(0, Math.ceil((nextRefreshAt - Date.now()) / 1000))
  countdown.value = `${Math.floor(remaining / 60)}:${String(remaining % 60).padStart(2, '0')}`
}
function restartTimer() {
  if (refreshTimer) clearInterval(refreshTimer)
  nextRefreshAt = Date.now() + refreshSeconds.value * 1000
  refreshTimer = setInterval(refreshBoard, refreshSeconds.value * 1000)
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
  refreshBoard()
}
function showContextMenu(event: MouseEvent, opNo: string) {
  contextMenu.value = { x: Math.min(event.clientX, window.innerWidth - 190), y: Math.min(event.clientY, window.innerHeight - 100), opNo }
}
function announce(message: string) {
  notice.value = message
  if (noticeTimer) clearTimeout(noticeTimer)
  noticeTimer = setTimeout(() => { notice.value = '' }, 2600)
}
function simulateCopy(opNo: string) {
  contextMenu.value = null
  announce(`${opNo} 圖表摘要已模擬複製（DEV 預覽不存取系統剪貼簿）。`)
}
function openLotDetail(opNo: string) {
  contextMenu.value = null
  detailOpNo.value = opNo
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
  <section class="trend-page" aria-label="作業站時段產出趨勢圖">
    <header class="page-heading">
      <div><p class="eyebrow">RPT · RT207</p><h1>作業站時段產出｜趨勢圖</h1><p class="subtitle">每個設定的作業站一張複合圖；柱狀顯示數量，折線顯示良率。</p></div>
      <button type="button" class="icon-button close-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="toolbar">
      <div class="status-line"><span class="live-dot" />歸屬日 <strong>{{ belongDate }}</strong><span class="separator">|</span>最後更新 {{ updatedTime }}<span>下次更新 {{ countdown }}</span></div>
      <div class="actions"><button type="button" class="secondary-button" @click="openSettings">看板設定</button><button type="button" class="primary-button" @click="refreshBoard">立即更新</button></div>
    </div>

    <div v-if="!operationCharts.length" class="empty-state"><strong>尚未設定作業站</strong><button type="button" class="primary-button" @click="openSettings">設定作業站</button></div>
    <div v-else class="chart-grid" :style="{ '--tile-font': `${fontSize}px` }">
      <article v-for="chart in operationCharts" :key="chart.opNo" class="chart-card" @contextmenu.prevent="showContextMenu($event, chart.opNo)" @dblclick="expandedOpNo = chart.opNo">
        <header class="chart-heading"><div><strong>{{ chart.opNo }}</strong><span>{{ chart.opName }}</span></div><span class="chart-badge">時段趨勢</span></header>
        <div class="chart-wrap">
          <svg class="trend-svg" viewBox="0 0 780 245" role="img" :aria-label="`${chart.opNo} 各時段產量與良率趨勢，雙擊放大`" @dblclick.stop="expandedOpNo = chart.opNo">
            <g v-for="tick in [0, 0.5, 1]" :key="tick">
              <line x1="48" :y1="198 - tick * 166" x2="742" :y2="198 - tick * 166" class="grid-line" />
              <text x="42" :y="202 - tick * 166" text-anchor="end" class="axis-label">{{ Math.round(sharedQtyMax * tick) }}</text>
              <text x="747" :y="202 - tick * 166" class="axis-label">{{ Math.round(100 * tick) }}%</text>
            </g>
            <line x1="48" y1="32" x2="48" y2="198" class="axis-line" /><line x1="742" y1="32" x2="742" y2="198" class="axis-line" />
            <g v-for="bucket in chart.buckets" :key="bucket.index">
              <g :aria-label="`${bucket.range}: Good ${bucket.good}, Scrap ${bucket.scrap}, Defect ${bucket.defect}`">
                <title>{{ bucket.date }} · {{ bucket.range }}<template v-if="bucket.active"> · Good {{ bucket.good }} · Scrap {{ bucket.scrap }} · Defect {{ bucket.defect }}</template><template v-else> · 尚未開始（Legacy 圖表資料顯示 0）</template></title>
                <rect v-for="(series, seriesIndex) in chartSeries" :key="series.key" :x="barX(bucket.index, seriesIndex)" :y="yQty(seriesValue(bucket, series.key))" width="11" :height="barHeight(seriesValue(bucket, series.key))" :fill="series.color" rx="1" />
              </g>
              <text :x="xCenter(bucket.index)" y="218" text-anchor="middle" class="step-label">{{ bucket.label }}</text>
            </g>
            <path v-if="yieldPath(chart)" :d="yieldPath(chart)" class="yield-line" />
            <g v-for="bucket in chart.buckets" :key="`yield-${bucket.index}`">
              <circle :cx="xCenter(bucket.index)" :cy="yYield(bucket.yieldValue)" r="3.1" class="yield-point"><title>{{ bucket.date }} · {{ bucket.range }} · Yield {{ bucket.yieldValue.toFixed(1) }}%</title></circle>
            </g>
          </svg>
        </div>
        <div class="chart-legend"><span v-for="series in chartSeries" :key="series.key"><i :style="{ backgroundColor: series.color }" />{{ series.label }}</span><span><i class="yield-key" />Yield</span></div>
        <div class="chart-hint">雙擊放大 · 右鍵複製圖表或查看 Lot Detail</div>
      </article>
    </div>
    <footer class="page-footer"><span>{{ operationCharts.length }} 個作業站 · {{ activeTimeStepCount }} / 12 個已開始時段</span><span class="dev-note">DEV 固定假資料 · Refresh {{ refreshSeconds }} 秒 · 柱狀軸共用最大值 {{ formatQty(sharedQtyMax) }}</span></footer>

    <div v-if="settingsOpen" class="modal-backdrop" @click.self="settingsOpen = false">
      <section class="dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title">
        <header class="dialog-header"><div><p class="eyebrow">SetupType 5 · DEV</p><h2 id="settings-title">趨勢圖看板設定</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="settingsOpen = false">×</button></header>
        <div class="setting-fields"><label>更新秒數<input v-model.number="draftRefresh" type="number" min="1"></label><label>字級<input v-model.number="draftFontSize" type="number" min="8"></label></div>
        <div class="operation-selector"><div class="section-title"><strong>作業站</strong><span>{{ draftOps.length }} 已選</span></div><label v-for="operation in operations" :key="operation.opNo" class="operation-option"><input v-model="draftOps" type="checkbox" :value="operation.opNo"><strong>{{ operation.opNo }}</strong><span>{{ operation.opName }}</span></label></div>
        <p class="dialog-note">DEV 只在本頁記憶體模擬設定；未呼叫舊系統或寫入後端。</p><p v-if="settingsError" class="form-error" role="alert">{{ settingsError }}</p>
        <footer class="dialog-actions"><button type="button" class="secondary-button" @click="settingsOpen = false">取消</button><button type="button" class="primary-button" @click="saveSettings">套用</button></footer>
      </section>
    </div>

    <div v-if="expandedOpNo" class="modal-backdrop" @click.self="expandedOpNo = ''">
      <section class="dialog expanded-dialog" role="dialog" aria-modal="true" :aria-label="`${expandedOpNo} 趨勢圖`">
        <header class="dialog-header"><div><p class="eyebrow">RT207 · Type 5 放大檢視</p><h2>{{ expandedOpNo }} · {{ operations.find((item) => item.opNo === expandedOpNo)?.opName }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="expandedOpNo = ''">×</button></header>
        <template v-for="chart in operationCharts.filter((item) => item.opNo === expandedOpNo)" :key="chart.opNo">
          <svg class="trend-svg expanded-svg" viewBox="0 0 780 245" role="img" :aria-label="`${chart.opNo} 放大趨勢圖`">
            <g v-for="tick in [0, 0.5, 1]" :key="tick"><line x1="48" :y1="198 - tick * 166" x2="742" :y2="198 - tick * 166" class="grid-line" /><text x="42" :y="202 - tick * 166" text-anchor="end" class="axis-label">{{ Math.round(sharedQtyMax * tick) }}</text><text x="747" :y="202 - tick * 166" class="axis-label">{{ Math.round(100 * tick) }}%</text></g>
            <g v-for="bucket in chart.buckets" :key="bucket.index"><g><title>{{ bucket.date }} · {{ bucket.range }}<template v-if="bucket.active"> · Good {{ bucket.good }} · Scrap {{ bucket.scrap }} · Defect {{ bucket.defect }}</template><template v-else> · 尚未開始</template></title><rect v-for="(series, seriesIndex) in chartSeries" :key="series.key" :x="barX(bucket.index, seriesIndex)" :y="yQty(seriesValue(bucket, series.key))" width="11" :height="barHeight(seriesValue(bucket, series.key))" :fill="series.color" rx="1" /></g><text :x="xCenter(bucket.index)" y="218" text-anchor="middle" class="step-label">{{ bucket.label }} · {{ bucket.range }}</text></g>
            <path v-if="yieldPath(chart)" :d="yieldPath(chart)" class="yield-line" /><g v-for="bucket in chart.buckets" :key="`expanded-yield-${bucket.index}`"><circle :cx="xCenter(bucket.index)" :cy="yYield(bucket.yieldValue)" r="3.1" class="yield-point" /></g>
          </svg>
          <div class="chart-legend expanded-legend"><span v-for="series in chartSeries" :key="series.key"><i :style="{ backgroundColor: series.color }" />{{ series.label }}</span><span><i class="yield-key" />Yield</span></div>
        </template>
        <p class="dialog-note">WinForms 雙擊進入 Type 5 子畫面；此處以假資料模擬放大檢視。</p>
      </section>
    </div>

    <div v-if="detailOpNo" class="modal-backdrop" @click.self="detailOpNo = ''">
      <section class="dialog detail-dialog" role="dialog" aria-modal="true" aria-labelledby="lot-detail-title">
        <header class="dialog-header"><div><p class="eyebrow">RT207 · Type 7 Lot Detail</p><h2 id="lot-detail-title">{{ detailOpNo }} · {{ selectedOperation?.opName }} 生產批明細</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="detailOpNo = ''">×</button></header>
        <p class="detail-caption">歸屬日 {{ belongDate }} · Legacy 依 OP 與已開始時段載入 LotLog；以下為 DEV 假資料。</p>
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>TimeStep</th><th>EndTime</th><th>GoodQty</th><th>FailQty</th><th>DefectQty</th></tr></thead><tbody><tr v-for="row in detailRows" :key="row.lotNo"><td>{{ row.lotNo }}</td><td>{{ row.timeStep }}</td><td>{{ row.endTime }}</td><td>{{ formatQty(row.goodQty) }}</td><td>{{ formatQty(row.failQty) }}</td><td>{{ formatQty(row.defectQty) }}</td></tr></tbody></table></div>
        <footer class="dialog-actions"><button type="button" class="primary-button" @click="detailOpNo = ''">關閉</button></footer>
      </section>
    </div>

    <div v-if="contextMenu" class="context-backdrop" @click="contextMenu = null">
      <div class="context-menu" :style="{ left: `${contextMenu.x}px`, top: `${contextMenu.y}px` }" role="menu" @click.stop>
        <button type="button" role="menuitem" @click="simulateCopy(contextMenu.opNo)">Copy To Clipboard</button>
        <button type="button" role="menuitem" @click="openLotDetail(contextMenu.opNo)">Lot Detail</button>
      </div>
    </div>
    <div v-if="notice" class="toast" role="status">{{ notice }}</div>
  </section>
</template>

<style scoped>
.trend-page{box-sizing:border-box;height:100%;min-height:0;padding:22px 26px;display:flex;flex-direction:column;gap:14px;background:#111923;color:#e9eff5;font-family:Inter,"Noto Sans TC",sans-serif;overflow:auto}.page-heading,.toolbar,.status-line,.actions,.dialog-header,.dialog-actions,.section-title,.page-footer{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-heading h1{margin:3px 0 4px;font-size:24px}.eyebrow{margin:0;color:#88a9c8;font-size:11px;font-weight:800;letter-spacing:.12em}.subtitle{margin:0;color:#9aabb9;font-size:12px}.icon-button{border:1px solid #354454;background:#1b2733;color:#c5d2dc;border-radius:7px;cursor:pointer}.close-button{width:36px;height:36px;font-size:24px}.toolbar{padding:9px 13px;background:#1b2733;border:1px solid #2c3a48;border-radius:8px}.status-line{justify-content:flex-start;flex-wrap:wrap;color:#a4b3bf;font-size:11px}.status-line strong{color:#e7f0f7}.separator{color:#465565}.live-dot{width:8px;height:8px;border-radius:50%;background:#2fc489;box-shadow:0 0 0 3px #2fc48930}.actions{gap:8px}.primary-button,.secondary-button{height:32px;padding:0 12px;border-radius:6px;font:inherit;font-size:11px;font-weight:700;cursor:pointer}.primary-button{border:1px solid #3194d1;background:#257bb4;color:#fff}.secondary-button{border:1px solid #405263;background:#243443;color:#dce6ee}.chart-grid{flex:1;min-height:0;display:grid;grid-template-columns:repeat(2,minmax(360px,1fr));grid-auto-rows:minmax(310px,1fr);gap:13px;overflow:auto;padding:2px}.chart-card{min-height:300px;padding:12px 14px 9px;border:1px solid #2e3c49;border-radius:9px;background:#1a2530;box-shadow:0 5px 18px #0003;cursor:default}.chart-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:8px}.chart-heading strong,.chart-heading span{display:block}.chart-heading strong{font-size:calc(var(--tile-font,13px) * 1.05);letter-spacing:.03em}.chart-heading div>span{margin-top:3px;color:#92a4b3;font-size:11px}.chart-badge{padding:4px 7px;border:1px solid #354a5b;border-radius:5px;background:#202f3c;color:#abc5d8;font-size:10px;white-space:nowrap}.chart-wrap{margin-top:4px}.trend-svg{display:block;width:100%;height:auto;min-height:180px;overflow:visible}.grid-line{stroke:#33414d;stroke-width:1;stroke-dasharray:3 4}.axis-line{stroke:#607181;stroke-width:1}.axis-label{fill:#94a5b2;font-size:10px}.step-label{fill:#8294a3;font-size:8px}.yield-line{fill:none;stroke:#65a8ff;stroke-width:2.4;stroke-linejoin:round;stroke-linecap:round}.yield-point{fill:#65a8ff;stroke:#e2efff;stroke-width:1.2}.chart-legend{display:flex;justify-content:center;align-items:center;flex-wrap:wrap;gap:15px;padding-top:2px;color:#bac7d1;font-size:10px}.chart-legend span{display:flex;align-items:center;gap:5px}.chart-legend i{display:inline-block;width:10px;height:10px;border-radius:2px}.chart-legend .yield-key{height:3px;border-radius:0;background:#65a8ff}.chart-hint{padding-top:6px;text-align:right;color:#738391;font-size:9px}.page-footer{flex-wrap:wrap;color:#94a5b2;font-size:10px}.dev-note{margin-left:auto;color:#b29d72}.empty-state{flex:1;display:grid;place-content:center;justify-items:center;gap:12px;border:1px dashed #40505d;border-radius:10px;background:#1a2530;color:#c3d0da}.modal-backdrop{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:20px;background:#06101cbb;backdrop-filter:blur(3px)}.dialog{box-sizing:border-box;width:min(610px,100%);max-height:88vh;overflow:auto;padding:20px;border:1px solid #405261;border-radius:12px;background:#1b2733;color:#e7eef4;box-shadow:0 24px 70px #0008}.dialog h2{margin:4px 0 0;font-size:19px}.dialog-header{margin-bottom:14px}.setting-fields{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin:16px 0}.setting-fields label{display:grid;gap:7px;color:#a9b9c6;font-size:11px;font-weight:700}.setting-fields input{box-sizing:border-box;height:35px;padding:0 9px;border:1px solid #435362;border-radius:5px;background:#111a22;color:#eef4f7;font:inherit}.operation-selector{display:grid}.section-title{padding-bottom:8px;border-bottom:1px solid #344351;font-size:12px}.section-title span,.operation-option span{color:#9aabb8;font-size:10px}.operation-option{display:grid;grid-template-columns:18px 62px 1fr;align-items:center;gap:8px;padding:9px 1px;border-bottom:1px solid #2b3946;color:#d3dee6;font-size:11px}.operation-option input{accent-color:#3194d1}.dialog-note,.detail-caption{margin:12px 0 0;color:#92a3b0;font-size:10px;line-height:1.6}.form-error{margin:10px 0 0;color:#ff8b8b;font-size:11px}.dialog-actions{justify-content:flex-end;margin-top:15px}.expanded-dialog{width:min(1100px,96vw)}.expanded-svg{width:100%;margin:14px 0 4px}.expanded-svg .step-label{font-size:9px}.expanded-legend{padding-bottom:12px}.detail-dialog{width:min(1100px,96vw)}.table-wrap{max-height:55vh;overflow:auto;margin-top:12px;border:1px solid #344452;border-radius:6px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th,td{padding:9px 10px;border-bottom:1px solid #2c3a46;text-align:left}th{position:sticky;top:0;background:#243341;color:#c8d6e0}td{color:#dce4ea}.context-backdrop{position:fixed;inset:0;z-index:90}.context-menu{position:fixed;display:grid;min-width:178px;padding:5px;border:1px solid #405261;border-radius:7px;background:#1f2d39;box-shadow:0 12px 30px #0008}.context-menu button{padding:9px 11px;border:0;border-radius:4px;background:transparent;color:#e1e9ee;text-align:left;font:inherit;font-size:11px;cursor:pointer}.context-menu button:hover{background:#2d4253}.toast{position:fixed;right:24px;bottom:24px;z-index:110;max-width:420px;padding:11px 14px;border:1px solid #4a6172;border-radius:7px;background:#233440;color:#e8eff3;font-size:11px;box-shadow:0 8px 30px #0005}
@media(max-width:900px){.chart-grid{grid-template-columns:1fr;grid-auto-rows:minmax(300px,auto)}}@media(max-width:650px){.trend-page{padding:14px;gap:10px}.page-heading h1{font-size:19px}.toolbar{align-items:flex-start;flex-direction:column}.status-line{gap:8px}.actions{width:100%}.chart-grid{grid-template-columns:1fr;grid-auto-rows:minmax(275px,auto)}.chart-card{min-height:270px;padding:9px}.page-footer{align-items:flex-start;flex-direction:column;gap:7px}.dev-note{margin-left:0}.table-wrap{overflow-x:auto}}
</style>

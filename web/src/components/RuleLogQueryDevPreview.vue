<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type FilterKey = 'lotNo' | 'componentNo' | 'ruleNo' | 'ruleAction' | 'eventTime'
interface RuleLogRow {
  logGroupSerial: string
  lotNo: string
  componentNo: string
  ruleNo: string
  ruleSequence: number
  ruleAction: string
  processDate1: string
  ruleScript: string
  brEventTime: string
}

const emit = defineEmits<{ close: [] }>()

function toLocalDateTime(value: Date) {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}
function relativeDateTime(daysAgo: number, hour: number, minute: number, second: number) {
  const value = new Date()
  value.setDate(value.getDate() - daysAgo)
  value.setHours(hour, minute, second, 0)
  return toLocalDateTime(value)
}
function legacyStartDate() {
  const now = new Date()
  const start = new Date(now)
  start.setHours(7, 30, 0, 0)
  if (now < start) start.setDate(start.getDate() - 1)
  return toLocalDateTime(start)
}

const sampleRows: RuleLogRow[] = [
  { logGroupSerial: 'LOG-261126-A', lotNo: 'LOT-261126-A01', componentNo: 'COMP-1001', ruleNo: 'PRD-ETCH-01', ruleSequence: 10, ruleAction: 'PASS', processDate1: relativeDateTime(1, 15, 22, 12), ruleScript: 'ETCH_RATE >= MIN_RATE', brEventTime: relativeDateTime(1, 15, 25, 3) },
  { logGroupSerial: 'LOG-261127-A', lotNo: 'LOT-261127-A02', componentNo: 'COMP-1002', ruleNo: 'PRD-ETCH-01', ruleSequence: 10, ruleAction: 'WARNING', processDate1: relativeDateTime(0, 8, 15, 10), ruleScript: 'ETCH_RATE < WARN_RATE', brEventTime: relativeDateTime(0, 8, 18, 0) },
  { logGroupSerial: 'LOG-261127-B', lotNo: 'LOT-261127-A02', componentNo: 'COMP-1002', ruleNo: 'QC-RESIST-02', ruleSequence: 20, ruleAction: 'HOLD', processDate1: relativeDateTime(0, 8, 15, 10), ruleScript: 'RESISTANCE > MAX_LIMIT', brEventTime: relativeDateTime(0, 8, 18, 0) },
  { logGroupSerial: 'LOG-261127-C', lotNo: 'LOT-261127-B01', componentNo: 'COMP-2001', ruleNo: 'QC-RESIST-02', ruleSequence: 20, ruleAction: 'PASS', processDate1: relativeDateTime(0, 9, 42, 6), ruleScript: 'RESISTANCE <= MAX_LIMIT', brEventTime: relativeDateTime(0, 9, 44, 51) },
  { logGroupSerial: 'LOG-261127-D', lotNo: 'LOT-261127-C04', componentNo: 'COMP-3002', ruleNo: 'PRD-BIN-03', ruleSequence: 5, ruleAction: 'WAIT', processDate1: relativeDateTime(0, 10, 5, 42), ruleScript: 'BIN_CONFIRMATION_PENDING', brEventTime: '' },
  { logGroupSerial: 'LOG-261127-E', lotNo: 'LOT-261127-D02', componentNo: 'COMP-4001', ruleNo: 'PRD-ETCH-01', ruleSequence: 10, ruleAction: 'PASS', processDate1: relativeDateTime(0, 11, 33, 24), ruleScript: 'ETCH_RATE >= MIN_RATE', brEventTime: relativeDateTime(0, 11, 36, 12) },
]

const ruleNumbers = ['PRD-BIN-03', 'PRD-ETCH-01', 'QC-RESIST-02']
const ruleActions = ['PASS', 'HOLD', 'WAIT', 'WARNING']
const filters = reactive({
  lotNo: '',
  componentNo: '',
  ruleNo: '',
  ruleAction: '',
  startEventTime: legacyStartDate(),
  endEventTime: toLocalDateTime(new Date()),
})
const enabled = reactive<Record<FilterKey, boolean>>({ lotNo: false, componentNo: false, ruleNo: false, ruleAction: false, eventTime: false })
const visibleRows = ref<RuleLogRow[]>([])
const hasSearched = ref(false)
const busy = ref(false)
const notice = ref('')
const lookupOpen = ref(false)
const lookupText = ref('')

const lookupOptions = computed(() => {
  const query = lookupText.value.trim().toUpperCase()
  return ruleNumbers.filter((ruleNo) => ruleNo.includes(query))
})
const sortedRows = computed(() => visibleRows.value.slice().sort((a, b) =>
  a.lotNo.localeCompare(b.lotNo)
  || a.componentNo.localeCompare(b.componentNo)
  || a.processDate1.localeCompare(b.processDate1)
  || a.ruleSequence - b.ruleSequence))
const activeSummary = computed(() => {
  const criteria: string[] = []
  if (enabled.lotNo && filters.lotNo.trim()) criteria.push(`Lot No=${filters.lotNo}`)
  if (enabled.componentNo && filters.componentNo.trim()) criteria.push(`Component No=${filters.componentNo}`)
  if (enabled.ruleNo && filters.ruleNo.trim()) criteria.push(`Rule No=${filters.ruleNo}`)
  if (enabled.ruleAction && filters.ruleAction) criteria.push(`Rule Action=${filters.ruleAction}`)
  if (enabled.eventTime) criteria.push(`Event Time=${filters.startEventTime} → ${filters.endEventTime}`)
  return criteria.join(' · ')
})

function setTextFilter(key: 'lotNo' | 'componentNo') {
  enabled[key] = Boolean(filters[key].trim())
}
function setEnabled(key: FilterKey, value: boolean) {
  enabled[key] = value
  if (value) return
  if (key !== 'eventTime') filters[key] = ''
}
function chooseRule(ruleNo: string) {
  filters.ruleNo = ruleNo
  enabled.ruleNo = true
  lookupOpen.value = false
}
function openLookup() {
  lookupText.value = ''
  lookupOpen.value = true
}
function matches(row: RuleLogRow) {
  if (enabled.lotNo && filters.lotNo.trim() && !row.lotNo.toUpperCase().startsWith(filters.lotNo.trim().toUpperCase())) return false
  if (enabled.componentNo && filters.componentNo.trim() && !row.componentNo.toUpperCase().startsWith(filters.componentNo.trim().toUpperCase())) return false
  if (enabled.ruleNo && filters.ruleNo.trim() && row.ruleNo !== filters.ruleNo.trim()) return false
  if (enabled.ruleAction && filters.ruleAction && !row.ruleAction.includes(filters.ruleAction)) return false
  if (enabled.eventTime && (row.processDate1 < filters.startEventTime || row.processDate1 > filters.endEventTime)) return false
  return true
}
function runQuery() {
  if (busy.value) return
  const hasCondition = Boolean(
    (enabled.lotNo && filters.lotNo.trim())
    || (enabled.componentNo && filters.componentNo.trim())
    || (enabled.ruleNo && filters.ruleNo.trim())
    || (enabled.ruleAction && filters.ruleAction)
    || enabled.eventTime,
  )
  hasSearched.value = true
  if (!hasCondition) {
    visibleRows.value = []
    notice.value = '請至少指定一項查詢條件。'
    return
  }
  busy.value = true
  filters.lotNo = filters.lotNo.trim().toUpperCase()
  filters.componentNo = filters.componentNo.trim().toUpperCase()
  visibleRows.value = sampleRows.filter(matches)
  notice.value = 'DEV 以有限假資料模擬查詢；正式 Core 無分頁或筆數上限，正式 API 應要求條件並限制結果量。'
  window.setTimeout(() => { busy.value = false }, 160)
}
function closeLookup() {
  lookupOpen.value = false
  lookupText.value = ''
}
</script>

<template>
  <main class="rule-log" aria-label="生產批執行法則查詢 DEV" :aria-busy="busy">
    <header class="page-heading">
      <div><div class="eyebrow">RPT / RT504</div><h1>生產批執行法則查詢</h1><p>依生產批、元件、法則、動作或事件時間查詢執行紀錄。</p></div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <section class="query-panel" aria-label="查詢條件">
      <div class="query-grid">
        <div class="field"><label class="check"><input type="checkbox" :checked="enabled.lotNo" @change="setEnabled('lotNo', ($event.target as HTMLInputElement).checked)"><span>Lot No</span></label><input v-model="filters.lotNo" type="text" placeholder="批號前綴" @input="setTextFilter('lotNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.lotNo = filters.lotNo.toUpperCase()"></div>
        <div class="field"><label class="check"><input type="checkbox" :checked="enabled.componentNo" @change="setEnabled('componentNo', ($event.target as HTMLInputElement).checked)"><span>Component No</span></label><input v-model="filters.componentNo" type="text" placeholder="元件編號前綴" @input="setTextFilter('componentNo')" @focus="($event.target as HTMLInputElement).select()" @blur="filters.componentNo = filters.componentNo.toUpperCase()"></div>
        <div class="field"><label class="check"><input type="checkbox" :checked="enabled.ruleNo" @change="setEnabled('ruleNo', ($event.target as HTMLInputElement).checked)"><span>Rule No</span></label><div class="picker"><input :value="filters.ruleNo" readonly placeholder="選擇法則"><button type="button" aria-label="選擇 Rule No" @click="openLookup">…</button></div></div>
        <div class="field"><label class="check"><input type="checkbox" :checked="enabled.ruleAction" @change="setEnabled('ruleAction', ($event.target as HTMLInputElement).checked)"><span>Rule Action</span></label><select v-model="filters.ruleAction" @change="enabled.ruleAction = Boolean(filters.ruleAction)"><option value="">選擇動作</option><option v-for="action in ruleActions" :key="action" :value="action">{{ action }}</option></select></div>
        <div class="field date-field"><label class="check"><input type="checkbox" :checked="enabled.eventTime" @change="setEnabled('eventTime', ($event.target as HTMLInputElement).checked)"><span>Event Time</span></label><div class="date-range"><input v-model="filters.startEventTime" type="datetime-local" step="1" aria-label="Start Event Time"><span>To</span><input v-model="filters.endEventTime" type="datetime-local" step="1" aria-label="End Event Time"></div></div>
      </div>
      <button type="button" class="query-button" :disabled="busy" @click="runQuery">{{ busy ? '查詢中…' : 'Query' }}</button>
    </section>

    <section class="result-panel" aria-label="法則執行紀錄">
      <div class="result-heading"><div><h2>Rule Check Log</h2><p>{{ hasSearched ? (activeSummary || '無條件') : '至少指定一項條件後執行查詢。' }}</p></div><span>{{ hasSearched ? `${sortedRows.length} 筆` : '尚未查詢' }}</span></div>
      <p v-if="notice" class="notice">{{ notice }}</p>
      <div class="table-wrap">
        <table>
          <thead><tr><th aria-sort="ascending">Lot No ↑</th><th>Component No</th><th>Rule No</th><th class="number">Rule Sequence</th><th>Rule Action</th><th>Process Date</th><th>Rule Script</th><th>BR Event Time</th></tr></thead>
          <tbody v-if="hasSearched && sortedRows.length">
            <tr v-for="row in sortedRows" :key="row.logGroupSerial"><td class="mono">{{ row.lotNo }}</td><td class="mono">{{ row.componentNo }}</td><td>{{ row.ruleNo }}</td><td class="number">{{ row.ruleSequence }}</td><td><span class="action-pill" :class="`action-${row.ruleAction.toLowerCase()}`">{{ row.ruleAction }}</span></td><td>{{ row.processDate1.replace('T', ' ') }}</td><td class="script">{{ row.ruleScript }}</td><td>{{ row.brEventTime ? row.brEventTime.replace('T', ' ') : '—' }}</td></tr>
          </tbody>
          <tbody v-else><tr><td colspan="8" class="empty-state">{{ hasSearched ? '沒有符合條件的紀錄。' : '尚未執行查詢' }}</td></tr></tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>排序：Lot No → Component No → Process Date → Rule Sequence</span><span>來源欄位 EventTime 在 Form 載入後改名為 ProcessDate1。</span></footer>
    </section>
    <footer class="page-footer"><span>查詢來源：有限 DEV 記憶體假資料</span><button type="button" class="close-button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupOpen" class="modal-backdrop" @click.self="closeLookup">
      <section class="lookup-dialog" role="dialog" aria-modal="true" aria-label="選擇 Rule No">
        <header><div><h2>選擇 Rule No</h2><p>舊表單載入產品作業法則主檔；此處以有限 DEV 假資料模擬。</p></div><button type="button" aria-label="關閉" @click="closeLookup">×</button></header>
        <input v-model="lookupText" type="search" placeholder="搜尋法則編號">
        <ul v-if="lookupOptions.length"><li v-for="ruleNo in lookupOptions" :key="ruleNo"><button type="button" @click="chooseRule(ruleNo)">{{ ruleNo }}</button></li></ul>
        <p v-else class="no-options">沒有符合的假資料。</p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.rule-log{box-sizing:border-box;display:flex;flex-direction:column;gap:11px;min-height:0;height:100%;padding:12px;background:#f4f3ee;color:#292820;font:inherit;overflow:auto}.page-heading{display:flex;justify-content:space-between;gap:14px}.eyebrow{margin-bottom:3px;color:#777366;font-size:10px;letter-spacing:.1em}.page-heading h1{margin:0;font-size:19px;font-weight:650}.page-heading p{margin:4px 0 0;color:#777366;font-size:11px}.dev-badge{height:max-content;padding:5px 9px;border:1px solid #d3c79c;border-radius:3px;background:#f5efd8;color:#66571f;font-size:11px;font-weight:700}.query-panel{display:flex;align-items:flex-end;gap:10px;padding:10px;border:1px solid #cbc9bd;background:#fbfaf6}.query-grid{display:grid;flex:1;grid-template-columns:repeat(3,minmax(230px,1fr));gap:9px 14px}.field{display:grid;grid-template-columns:112px minmax(0,1fr);align-items:center;gap:7px;font-size:10px}.check{display:flex;align-items:center;gap:5px;white-space:nowrap}.check input{margin:0}.field>input,.field>select,.picker input,.date-range input{box-sizing:border-box;min-width:0;width:100%;height:28px;padding:4px 7px;border:1px solid #aaa79b;border-radius:2px;background:#fff;color:#292820;font:inherit}.picker{display:flex;min-width:0}.picker input{border-radius:2px 0 0 2px;background:#f0efe9}.picker button{width:30px;flex:none;border:1px solid #aaa79b;border-left:0;border-radius:0 2px 2px 0;background:#eeede7;cursor:pointer}.date-field{grid-column:1 / span 2}.date-range{display:flex;align-items:center;gap:7px}.date-range input{flex:1}.date-range span{flex:none;color:#777366}.query-button,.close-button{min-height:30px;padding:4px 13px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer;white-space:nowrap}.query-button:disabled{opacity:.6;cursor:wait}.result-panel{display:flex;flex:1;flex-direction:column;min-height:190px;border:1px solid #cbc9bd;background:#fbfaf6}.result-heading{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:9px 11px;border-bottom:1px solid #d9d7cc}.result-heading h2{margin:0;font-size:13px}.result-heading p{margin:4px 0 0;color:#777366;font-size:10px}.result-heading>span{flex:none;color:#555247;font-size:11px}.notice{margin:8px 11px 0;padding:6px 8px;border-left:3px solid #c69b43;background:#fbf5e5;color:#6e5929;font-size:10px}.table-wrap{flex:1;min-height:140px;overflow:auto}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th{position:sticky;top:0;z-index:1;padding:7px;border-right:1px solid #d5d3c7;border-bottom:1px solid #c5c2b5;background:#eeece3;text-align:left;font-weight:650}td{padding:6px 7px;border-right:1px solid #e3e1d8;border-bottom:1px solid #e3e1d8;max-width:300px}.number{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:9px}.script{max-width:260px;overflow:hidden;text-overflow:ellipsis}.action-pill{display:inline-block;padding:2px 5px;border-radius:2px;background:#e9e8df;color:#575448;font-size:9px}.action-pass{background:#e0eee3;color:#42664b}.action-hold{background:#f4e2d8;color:#844933}.action-wait{background:#f5efd8;color:#66571f}.action-warning{background:#f8e6ca;color:#855d27}.empty-state{height:90px;text-align:center;color:#858173}.grid-footer{display:flex;justify-content:space-between;gap:8px;padding:5px 9px;border-top:1px solid #d9d7cc;color:#777366;font-size:9px}.page-footer{display:flex;justify-content:space-between;align-items:center;color:#777366;font-size:10px}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:#1d1d1980}.lookup-dialog{box-sizing:border-box;width:min(430px,100%);max-height:85vh;display:flex;flex-direction:column;gap:10px;padding:14px;border:1px solid #aaa797;border-radius:4px;background:#fbfaf6;box-shadow:0 14px 40px #0003}.lookup-dialog header{display:flex;justify-content:space-between;align-items:flex-start}.lookup-dialog h2{margin:0;font-size:15px}.lookup-dialog header p{margin:4px 0 0;color:#777366;font-size:10px}.lookup-dialog header button{width:28px;height:28px;border:1px solid #c8c5b9;background:#f2f1ec;font-size:19px;cursor:pointer}.lookup-dialog>input{height:29px;padding:4px 7px;border:1px solid #aaa797;font:inherit}.lookup-dialog ul{margin:0;padding:0;overflow:auto;list-style:none}.lookup-dialog li+li{border-top:1px solid #e2e0d7}.lookup-dialog li button{width:100%;padding:8px;text-align:left;border:0;background:transparent;font:inherit;cursor:pointer}.lookup-dialog li button:hover{background:#eeeee5}.no-options{text-align:center;padding:18px}@media(max-width:900px){.query-grid{grid-template-columns:repeat(2,minmax(210px,1fr))}.query-panel{align-items:flex-start;flex-direction:column}.query-button{align-self:flex-end}.date-field{grid-column:1 / -1}}@media(max-width:620px){.query-grid{grid-template-columns:1fr}.date-field{grid-column:1}.date-range{flex-wrap:wrap}.date-range input{min-width:220px}.grid-footer{flex-direction:column}.page-heading h1{font-size:16px}}
</style>

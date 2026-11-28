<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type LogRow = { treeNo: string; treeName: string; kpiNo: string; kpiName: string; targetType: string; targetValue: string; targetLow: string; targetUp: string; actualValue: string; script: string; creator: string; created: string }
const emit = defineEmits<{ close: [] }>()
const fixtures: LogRow[] = [
  { treeNo: 'KPI-001', treeName: '工廠營運指標', kpiNo: 'YIELD', kpiName: '良率', targetType: 'Percentage', targetValue: '98', targetLow: '95', targetUp: '100', actualValue: '96.8', script: 'SELECT GOODQTY / INPUTQTY * 100 FROM ...', creator: 'DEMO', created: '2026-11-28T10:42:10' },
  { treeNo: 'KPI-001', treeName: '工廠營運指標', kpiNo: 'OEE', kpiName: '整體設備效率', targetType: 'Percentage', targetValue: '85', targetLow: '80', targetUp: '100', actualValue: '82.4', script: 'SELECT OEE_VALUE FROM ...', creator: 'DEMO', created: '2026-11-28T10:42:10' },
  { treeNo: 'KPI-002', treeName: '品質指標', kpiNo: 'DEFECT', kpiName: '不良率', targetType: 'Percentage', targetValue: '1', targetLow: '0', targetUp: '2', actualValue: '1.4', script: 'SELECT DEFECTQTY / INPUTQTY * 100 FROM ...', creator: 'DEMO', created: '2026-11-27T15:05:40' },
  { treeNo: 'KPI-001', treeName: '工廠營運指標', kpiNo: 'YIELD', kpiName: '良率', targetType: 'Percentage', targetValue: '98', targetLow: '95', targetUp: '100', actualValue: '97.5', script: 'SELECT GOODQTY / INPUTQTY * 100 FROM ...', creator: 'DEMO', created: '2026-11-26T09:12:00' },
  { treeNo: 'KPI-003', treeName: '設備效率', kpiNo: 'MTBF', kpiName: '平均故障間隔', targetType: 'Number', targetValue: '120', targetLow: '', targetUp: '', actualValue: '135.2', script: 'SELECT MTBF FROM ...', creator: 'DEMO', created: '2026-11-25T17:28:05' },
]
function formatInput(date: Date) {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth()+1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function formatDate(value: string) {
  const d = new Date(value)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}/${pad(d.getMonth()+1)}/${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}
const today = new Date()
const startDefault = new Date(today)
startDefault.setDate(startDefault.getDate()-2)
startDefault.setHours(0,0,0,0)
const endDefault = new Date(today)
endDefault.setHours(23,59,59,0)
const nameChecked = ref(false)
const treeName = ref('')
const dateChecked = ref(false)
const startDate = ref(formatInput(startDefault))
const endDate = ref(formatInput(endDefault))
const rows = ref<LogRow[]>([])
const message = ref('請勾選樹名稱或時間範圍，再按 Query。日期端點依舊版邏輯不包含。')
const targetLabels: Record<string,string> = { Number: 'Number', Percentage: 'Percentage' }
const shownRows = computed(() => rows.value.map(row => ({ ...row, displayDate: formatDate(row.created), displayTarget: targetLabels[row.targetType] ?? row.targetType })))
watch(treeName, value => { if (value.trim()) nameChecked.value = true })
watch(nameChecked, checked => { if (!checked) treeName.value = '' })

function query() {
  if (!nameChecked.value && !dateChecked.value) { message.value = '請至少勾選樹名稱或日期條件。'; rows.value = []; return }
  if (nameChecked.value && !treeName.value.trim()) { message.value = '勾選樹名稱後請輸入 KPITreeName。'; rows.value = []; return }
  const start = dateChecked.value ? new Date(startDate.value).getTime() : null
  const end = dateChecked.value ? new Date(endDate.value).getTime() : null
  if (dateChecked.value && (!Number.isFinite(start) || !Number.isFinite(end) || start! >= end!)) { message.value = 'StartDateTime 必須早於 EndTime。'; rows.value = []; return }
  rows.value = fixtures.filter(row => {
    if (nameChecked.value && row.treeName !== treeName.value.trim()) return false
    const created = new Date(row.created).getTime()
    if (dateChecked.value && (created <= start! || created >= end!)) return false
    return true
  }).sort((a,b) => a.treeNo.localeCompare(b.treeNo) || a.kpiNo.localeCompare(b.kpiNo) || a.treeName.localeCompare(b.treeName))
  message.value = `查詢完成：${rows.value.length} 筆 DEV 假歷程。Core 原查詢無排序／分頁；此頁按舊 Form 排序，結果僅限 fixtures。`
}
</script>

<template>
  <section class="log-page" aria-label="指標樹歷史資料查詢 DEV 預覽">
    <header class="head"><div><h1>指標樹歷史資料查詢 <small>RT906</small></h1><p>DEV 假資料；公式未執行，不會連資料庫</p></div><button @click="emit('close')">Close (X)</button></header>
    <div class="filters">
      <label class="check"><input v-model="nameChecked" type="checkbox" /> KPITreeName</label><input v-model="treeName" class="name" :disabled="!nameChecked" placeholder="輸入完整的樹名稱" @keyup.enter="query" />
      <label class="check"><input v-model="dateChecked" type="checkbox" /> Save Date</label><label class="date">Start <input v-model="startDate" type="datetime-local" step="1" :disabled="!dateChecked" /></label><label class="date">End <input v-model="endDate" type="datetime-local" step="1" :disabled="!dateChecked" /></label><button class="query" @click="query">Query</button>
    </div>
    <p class="notice" role="status">{{ message }}</p>
    <div class="table-wrap"><table><thead><tr><th>KPITreeNo</th><th>KPITreeName</th><th>KPINo</th><th>KPIName</th><th>TargetType</th><th>TargetValue</th><th>TargetValueLow</th><th>TargetValueUp</th><th>ActualValue</th><th>ActualValueScript</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody><tr v-for="(row,index) in shownRows" :key="`${row.treeNo}-${row.kpiNo}-${row.created}-${index}`"><td>{{ row.treeNo }}</td><td>{{ row.treeName }}</td><td>{{ row.kpiNo }}</td><td>{{ row.kpiName }}</td><td>{{ row.displayTarget }}</td><td class="num">{{ row.targetValue }}</td><td class="num">{{ row.targetLow }}</td><td class="num">{{ row.targetUp }}</td><td class="num">{{ row.actualValue }}</td><td class="script">{{ row.script }}</td><td>{{ row.creator }}</td><td>{{ row.displayDate }}</td></tr><tr v-if="!shownRows.length"><td colspan="12" class="empty">{{ message.startsWith('查詢完成') ? '沒有符合的歷程資料' : '查詢結果會顯示在此處' }}</td></tr></tbody></table></div>
    <footer><span>{{ shownRows.length }} rows</span><span>值清單 label 為 DEV 示意；legacy 由 TBLEKBKPIDETAIL.TARGETTYPE ValueList 提供。</span></footer>
  </section>
</template>

<style scoped>
.log-page{height:100%;min-height:380px;display:flex;flex-direction:column;gap:8px;padding:12px;box-sizing:border-box;background:#f7f5ee;color:#292820;font:13px/1.4 "Segoe UI",Arial,sans-serif}.head,.head>div{display:flex;align-items:center;gap:8px}.head{justify-content:space-between;border-bottom:1px solid #cbc5b4;padding-bottom:7px}.head h1{margin:0;font-size:18px}.head h1 small{color:#777;font-size:12px}.head p{margin:0;color:#777}.head button,.filters button{min-height:28px;padding:4px 10px;border:1px solid #777365;border-radius:3px;background:#fff;color:#302f29;font:inherit;cursor:pointer}.filters{display:flex;align-items:center;flex-wrap:wrap;gap:8px}.filters .check{display:flex;align-items:center;gap:4px;white-space:nowrap}.filters .name,.filters .date input{padding:5px 6px;border:1px solid #aaa594;background:#fff;color:#292820;font:inherit}.filters .name{width:205px}.filters .date{display:flex;align-items:center;gap:5px}.filters .date input{width:195px}.filters input:disabled{background:#ece9df;color:#888}.filters .query{background:#39485e;color:#fff}.notice{margin:0;padding:5px 8px;background:#eee9d9;color:#59574b}.table-wrap{flex:1;min-height:120px;overflow:auto;border:1px solid #aaa594;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{padding:5px 7px;border:1px solid #d2cebf;text-align:left}th{position:sticky;top:0;z-index:1;background:#e9e5d7;font-weight:600}td.num{text-align:right}.script{max-width:360px;overflow:hidden;text-overflow:ellipsis}.empty{text-align:center;color:#888;padding:20px}footer{display:flex;justify-content:space-between;gap:8px;color:#777;font-size:11px}@media(max-width:760px){.filters{align-items:flex-start}.filters .date{width:100%}.filters .date input{flex:1}footer{flex-direction:column}}
</style>

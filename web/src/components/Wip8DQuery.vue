<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type Cell = string | number | null
type Basis = Record<string, Cell> & { qrsNo: string; qrsClass: number; issueState: number; closeDate: string | null }
type TextKey = 'qrsNo' | 'qrsSummary' | 'factoryNo' | 'projectDepartment' | 'projectManager'
type DateKey = 'createDate' | 'planCloseDate' | 'closeDate'
const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string; initialErfNo?: string }>()
const emit = defineEmits<{ close: []; open8dQuery: [qrsNo: string] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableText = '正式環境尚未開放此功能。'
const storageKey = 'ymes-dev-mock:wip-8d-basis-factory:state:v1'
const columns = [
  ['qrsNo','QRSNo'],['qrsClass','QRSClass'],['qrsSummary','QRSSummary'],['lotNo','LotNo'],['lotQty','LotQty'],
  ['sampleQty','SampleQty'],['errorQty','ErrorQty'],['factoryNo','FactoryNo'],['areaNo','AreaNo'],['moNo','MONo'],
  ['customerNo','CustomerNo'],['productNo','ProductNo'],['productVersion','ProductVersion'],['roNo','RONo'],['opNo','OPNo'],
  ['equipmentNo','EquipmentNo'],['erfNo','ERFNo'],['holdDescription','HoldDescription'],['description','Description'],
  ['projectDepartment','ProjectDepartment'],['projectManager','ProjectManager'],['projectManagerName','ProjectManagerName'],
  ['curStage','CurStage'],['planCloseDate','PlanCloseDate'],['lastUpdateDate','LastUpdateDate'],
  ['summaryDescription','SummaryDescription'],['dispHour','DispHour'],['dispCost','DispCost'],
  ['summaryDepartment','SummaryDepartment'],['summaryUserNo','SummaryUserNo'],['summaryUserName','SummaryUserName'],
  ['closeType','CloseType'],['closeDepartment','CloseDepartment'],['closeUserNo','CloseUserNo'],
  ['closeUserName','CloseUserName'],['closeDate','CloseDate'],['issueState','IssueState'],['creator','Creator'],['createDate','CreateDate'],
] as const
const textKeys: TextKey[] = ['qrsNo','qrsSummary','factoryNo','projectDepartment','projectManager']
const dateKeys: DateKey[] = ['createDate','planCloseDate','closeDate']
const classOptions = [{value:1,label:'In Line QC'},{value:2,label:'Off Line QC'},{value:3,label:'WIPERF'},{value:4,label:'Other'}] as const
const filters = reactive<Record<TextKey,string>>({qrsNo:'',qrsSummary:'',factoryNo:'',projectDepartment:'',projectManager:''})
const checked = reactive<Record<TextKey | DateKey | 'qrsClass' | 'closeState',boolean>>({qrsNo:false,qrsSummary:false,factoryNo:false,projectDepartment:false,projectManager:false,createDate:false,planCloseDate:false,closeDate:false,qrsClass:false,closeState:false})
const dates = reactive({createFrom:'',createTo:'',planFrom:'',planTo:'',closeFrom:'',closeTo:''})
const classes = ref<number[]>([])
const closed = ref(false)
const notClose = ref(false)
const sourceRows = ref<Basis[]>([])
const results = ref<Basis[]>([])
const activeNo = ref('')
const searched = ref(false)
const initialMessage = '選擇至少一個查詢條件後按 Query。'
const message = ref(dev.value ? initialMessage : unavailableText)
const error = ref('')
function unavailable(): boolean { if (dev.value) return false; error.value = ''; message.value = unavailableText; return true }
const active = computed(() => dev.value ? results.value.find(row => row.qrsNo === activeNo.value) || null : null)

const previewRows: Basis[] = [
  {qrsNo:'QRS-DEV-001',qrsClass:4,qrsSummary:'示範矯正單',lotNo:'',lotQty:0,sampleQty:0,errorQty:0,factoryNo:'FACTORY-A',areaNo:'A01',moNo:'',customerNo:'',productNo:'',productVersion:'',roNo:'',opNo:'',equipmentNo:'',erfNo:'',holdDescription:'',description:'DEV 主檔',projectDepartment:'QA',projectManager:'DEV-MGR',projectManagerName:'示範經理',curStage:'N/A',planCloseDate:'2026-11-27',lastUpdateDate:'2026-11-20 09:00:00',summaryDescription:'',dispHour:0,dispCost:0,summaryDepartment:'',summaryUserNo:'',summaryUserName:'',closeType:null,closeDepartment:'',closeUserNo:'',closeUserName:'',closeDate:null,creator:'DEV',createDate:'2026-11-20 09:00:00',issueState:0},
  {qrsNo:'QRS-DEV-002',qrsClass:3,qrsSummary:'設備異常 8D 示範',lotNo:'DEV-LOT-002',lotQty:5,sampleQty:0,errorQty:1,factoryNo:'FACTORY-A',areaNo:'A01',moNo:'MO-DEV-01',customerNo:'CUS-B',productNo:'PRD-B',productVersion:'1',roNo:'RO-DEV-01',opNo:'OP-TEST',equipmentNo:'EQ-DEV-01',erfNo:'DEV-ERF-002',holdDescription:'設備點檢異常',description:'完成處置',projectDepartment:'ENG',projectManager:'DEV-MGR',projectManagerName:'示範經理',curStage:'N/A',planCloseDate:'2026-11-27',lastUpdateDate:'2026-11-19 13:00:00',summaryDescription:'',dispHour:0,dispCost:0,summaryDepartment:'',summaryUserNo:'',summaryUserName:'',closeType:null,closeDepartment:'',closeUserNo:'',closeUserName:'',closeDate:null,creator:'DEV',createDate:'2026-11-19 13:00:00',issueState:2},
]

function load(): void {
  if (!dev.value) return
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) { sourceRows.value = previewRows.map(row => ({...row})); return }
    const value: unknown = JSON.parse(raw)
    sourceRows.value = Array.isArray(value) ? value.filter(row => row && typeof row.qrsNo === 'string' && typeof row.issueState === 'number') as Basis[] : []
  } catch { sourceRows.value = previewRows.map(row => ({...row})) }
}
function resetText(key: TextKey): void { if (unavailable()) return; if (!checked[key]) filters[key] = '' }
function markText(key: TextKey): void { if (unavailable()) return; checked[key] = !!filters[key].trim() }
function classChanged(): void { if (unavailable()) return; checked.qrsClass = classes.value.length > 0 }
function toggleClass(): void { if (unavailable()) return; if (!checked.qrsClass) classes.value = [] }
function closeStateChanged(): void { if (unavailable()) return; checked.closeState = closed.value || notClose.value; if (notClose.value) checked.closeDate = false }
function toggleCloseState(): void { if (unavailable()) return; if (!checked.closeState) { closed.value = false; notClose.value = false } }
function closeDateChanged(): void { if (unavailable()) return; if (checked.closeDate) { notClose.value = false; checked.closeState = closed.value } }
function day(value: Cell): string { return String(value || '').slice(0,10) }
function dateMatches(value: Cell, from: string, to: string): boolean { const date = day(value); return !!date && date >= from && date <= to }
function hasEffectiveCondition(): boolean {
  return !!props.initialErfNo || classes.value.length > 0 || textKeys.some(key => checked[key] && filters[key].trim()) ||
    dateKeys.some(key => checked[key]) || (checked.closeState && closed.value !== notClose.value)
}
function query(): void {
  error.value = ''
  if (unavailable()) return
  if (!hasEffectiveCondition()) { error.value = '請至少選擇一個有效查詢條件。'; return }
  if ((checked.createDate && (!dates.createFrom || !dates.createTo || dates.createFrom > dates.createTo)) ||
      (checked.planCloseDate && (!dates.planFrom || !dates.planTo || dates.planFrom > dates.planTo)) ||
      (checked.closeDate && (!dates.closeFrom || !dates.closeTo || dates.closeFrom > dates.closeTo))) {
    error.value = '日期起迄不正確。'; return
  }
  load()
  results.value = sourceRows.value.filter(row => {
    if (props.initialErfNo && String(row.erfNo || '').toUpperCase() !== props.initialErfNo.toUpperCase()) return false
    if (checked.qrsClass && classes.value.length && !classes.value.includes(Number(row.qrsClass))) return false
    for (const key of textKeys) {
      if (!checked[key] || !filters[key].trim()) continue
      const source = String(row[key] || '').toUpperCase()
      const wanted = filters[key].trim().toUpperCase()
      if (key === 'qrsNo' || key === 'qrsSummary' ? !source.includes(wanted) : source !== wanted) return false
    }
    if (checked.createDate && !dateMatches(row.createDate,dates.createFrom,dates.createTo)) return false
    if (checked.planCloseDate && !dateMatches(row.planCloseDate,dates.planFrom,dates.planTo)) return false
    if (checked.closeDate && !dateMatches(row.closeDate,dates.closeFrom,dates.closeTo)) return false
    if (checked.closeState && closed.value !== notClose.value && (!!row.closeDate) !== closed.value) return false
    return true
  }).sort((a,b) => a.qrsNo.localeCompare(b.qrsNo))
  searched.value = true
  activeNo.value = ''
  message.value = `查得 ${results.value.length} 筆 8D 矯正單。`
}
function openDetail(row = active.value): void { if (unavailable()) return; if (!row) { error.value = '請先選取資料。'; return }; emit('open8dQuery',row.qrsNo) }
function choose(row: Basis): void { if (unavailable()) return; activeNo.value = row.qrsNo }
onMounted(() => { if (dev.value) { load(); if (props.initialErfNo) query() } })
watch(dev, enabled => {
  if (enabled) { error.value = ''; message.value = initialMessage; return }
  sourceRows.value = []; results.value = []; activeNo.value = ''; searched.value = false
  for (const key of textKeys) filters[key] = ''
  for (const key of Object.keys(checked) as (keyof typeof checked)[]) checked[key] = false
  Object.assign(dates, { createFrom: '', createTo: '', planFrom: '', planTo: '', closeFrom: '', closeTo: '' })
  classes.value = []; closed.value = false; notClose.value = false
  error.value = ''; message.value = unavailableText
})
</script>

<template>
  <section class="wp804">
    <div v-if="dev" class="dev-note">DEV 假資料預覽 · 跨狀態查詢 WP801 主單；8DQuery 以唯讀方式開啟 WP802 十頁。</div>
    <h1>8D 矯正單查詢 <small>8D QUERY · WP804</small></h1>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>
    <fieldset><legend>Query</legend><div class="query-grid">
      <label class="query-item">QRSType <input value="0" readonly title="原 WinForms 顯示 QRSType，但查詢未送出此欄。" :disabled="!dev"></label>
      <div class="query-item"><label><input v-model="checked.qrsClass" type="checkbox" @change="toggleClass" :disabled="!dev">QRSClass</label><label v-for="item in classOptions" :key="item.value"><input v-model="classes" type="checkbox" :value="item.value" @change="classChanged" :disabled="!dev">{{ item.label }}</label></div>
      <label v-for="key in textKeys" :key="key" class="query-item"><input v-model="checked[key]" type="checkbox" @change="resetText(key)" :disabled="!dev">{{ key }} <input v-model="filters[key]" class="yellow" @input="markText(key)" @keydown.enter.prevent="query" :disabled="!dev"><span v-if="key === 'qrsNo' || key === 'qrsSummary'">%</span></label>
      <label class="query-item"><input v-model="checked.createDate" type="checkbox" :disabled="!dev">Create Date <input v-model="dates.createFrom" type="date" class="yellow" :disabled="!dev || !checked.createDate"> To <input v-model="dates.createTo" type="date" class="yellow" :disabled="!dev || !checked.createDate"></label>
      <label class="query-item"><input v-model="checked.planCloseDate" type="checkbox" :disabled="!dev">PlanCloseDate <input v-model="dates.planFrom" type="date" class="yellow" :disabled="!dev || !checked.planCloseDate"> To <input v-model="dates.planTo" type="date" class="yellow" :disabled="!dev || !checked.planCloseDate"></label>
      <label class="query-item"><input v-model="checked.closeDate" type="checkbox" @change="closeDateChanged" :disabled="!dev">CloseDate <input v-model="dates.closeFrom" type="date" class="yellow" :disabled="!dev || !checked.closeDate"> To <input v-model="dates.closeTo" type="date" class="yellow" :disabled="!dev || !checked.closeDate"></label>
      <div class="query-item"><label><input v-model="checked.closeState" type="checkbox" @change="toggleCloseState" :disabled="!dev">CloseState</label><label><input v-model="closed" type="checkbox" @change="closeStateChanged" :disabled="!dev">Closed</label><label><input v-model="notClose" type="checkbox" @change="closeStateChanged" :disabled="!dev">NotClose</label></div>
      <label v-if="initialErfNo" class="query-item">ERFNo <input :value="initialErfNo" readonly :disabled="!dev"></label>
      <button type="button" class="mes-button" :disabled="!dev" @click="query">Query (&Q)</button>
    </div></fieldset>
    <fieldset class="result"><legend>8D Basis Data</legend><div class="result-content"><div class="table-wrap"><table><thead><tr><th v-for="column in columns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody><tr v-for="row in results" :key="row.qrsNo" :class="{selected:activeNo === row.qrsNo}" tabindex="0" @click="choose(row)" @dblclick="openDetail(row)" @keydown.enter="choose(row)"><td v-for="column in columns" :key="column[0]">{{ row[column[0]] }}</td></tr><tr v-if="!results.length"><td :colspan="columns.length" class="empty">{{ !dev ? unavailableText : searched ? '沒有符合資料' : '請選條件並按 Query' }}</td></tr></tbody></table></div><aside><button type="button" class="mes-button" :disabled="!dev || !active" @click="openDetail()">8DQuery</button><button type="button" class="mes-button close" @click="emit('close')">Close (&X)</button></aside></div><div class="status">UserNo: {{ currentUserNo || (dev ? 'DEV' : '—') }}　資料總筆數 {{ results.length }}</div></fieldset>
  </section>
</template>

<style scoped>
.wp804{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,'Microsoft JhengHei',sans-serif}.dev-note{border:1px solid #d8c994;background:#fff8dc;padding:6px}.wp804 h1{font-size:18px;margin:0}.wp804 h1 small{font-size:12px;font-weight:400;color:#777}.feedback{min-height:18px;margin:0}.feedback.error{color:#a22;background:#fff0ef;padding:4px}.wp804 fieldset{min-width:0;margin:0;padding:8px;border:1px solid #aaa;background:#fff}.wp804 legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}.query-item{display:flex;align-items:center;gap:7px;min-height:28px}.query-item label{display:inline-flex;align-items:center;gap:4px;white-space:nowrap}.query-item>input.yellow{flex:1;min-width:0}.query-item input[type=date]{width:140px;flex:none}.query-item input[type=text],.query-item>input:not([type]){min-width:0}.yellow{box-sizing:border-box;min-height:26px;padding:3px 5px;border:1px solid #888;background:#fff0b3!important;color:#333;font:inherit}.mes-button{min-height:29px;padding:3px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;font:inherit;cursor:pointer}.mes-button:disabled{color:#999;cursor:default}.result{flex:1;min-height:230px;display:flex;flex-direction:column}.result-content{display:flex;gap:8px;min-height:0;flex:1}.table-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.table-wrap table{border-collapse:collapse;white-space:nowrap}.table-wrap th,.table-wrap td{padding:5px 7px;border:1px solid #d3d3d3;text-align:left}.table-wrap th{position:sticky;top:0;background:#ead591}.table-wrap td{background:#ffe89c}.table-wrap tr.selected td{background:#f4ca68}.table-wrap tr{cursor:pointer}.table-wrap .empty{text-align:center;background:#fff}.result-content aside{min-width:105px;display:flex;flex-direction:column;gap:6px}.result-content aside .mes-button{width:105px}.result-content aside .close{margin-top:auto}.status{padding-top:5px;color:#666}@media(max-width:900px){.query-grid{grid-template-columns:1fr}.query-item{flex-wrap:wrap}}
</style>

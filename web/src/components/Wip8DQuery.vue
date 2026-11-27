<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { apiErrorMessage } from '../utils/apiError'

type Cell = string | number | null
type Basis = Record<string, Cell> & { qrsNo: string; qrsClass: number; issueState: number; closeDate: string | null }
type TextKey = 'qrsNo' | 'qrsSummary' | 'factoryNo' | 'projectDepartment' | 'projectManager'
type DateKey = 'createDate' | 'planCloseDate' | 'closeDate'
interface PageResult { totalCount: number; page: number; pageSize: number; pageCount: number; items: Basis[] }
const props = defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean; currentUserNo?: string; initialErfNo?: string }>()
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
const currentPage = ref(1)
const pageCount = ref(0)
const totalCount = ref(0)
const loading = ref(false)
const activeNo = ref('')
const searched = ref(false)
const initialMessage = '選擇至少一個查詢條件後按 Query。'
const canQuery = computed(() => dev.value || !!props.apiBaseUrl?.trim())
const message = ref(canQuery.value ? initialMessage : unavailableText)
const error = ref('')
const hasMore = computed(() => currentPage.value < pageCount.value)
const active = computed(() => results.value.find(row => row.qrsNo === activeNo.value) || null)

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
function resetText(key: TextKey): void { if (!checked[key]) filters[key] = '' }
function markText(key: TextKey): void { checked[key] = !!filters[key].trim() }
function classChanged(): void { checked.qrsClass = classes.value.length > 0 }
function toggleClass(): void { if (!checked.qrsClass) classes.value = [] }
function closeStateChanged(): void { checked.closeState = closed.value || notClose.value; if (notClose.value) checked.closeDate = false }
function toggleCloseState(): void { if (!checked.closeState) { closed.value = false; notClose.value = false } }
function closeDateChanged(): void { if (checked.closeDate) { notClose.value = false; checked.closeState = closed.value } }
function day(value: Cell): string { return String(value || '').slice(0,10) }
function dateMatches(value: Cell, from: string, to: string): boolean { const date = day(value); return !!date && date >= from && date <= to }
function hasEffectiveCondition(): boolean {
  return !!props.initialErfNo || classes.value.length > 0 || textKeys.some(key => checked[key] && filters[key].trim()) ||
    dateKeys.some(key => checked[key]) || (checked.closeState && closed.value !== notClose.value)
}
function apiUrl(path: string): URL {
  const base = (props.apiBaseUrl || '').trim().replace(/\/+$/, '')
  if (!base) throw new Error('目前環境尚未設定 YMES API 位址，無法讀取正式資料。')
  const root = new URL(`${base}/`, window.location.origin)
  const local = root.hostname === 'localhost' || root.hostname === '127.0.0.1'
  if (root.protocol !== 'https:' && !local) throw new Error('正式 API 必須使用 HTTPS。')
  return new URL(path.replace(/^\/+/, ''), root)
}

function validateDates(): string {
  const ranges = [
    ...(checked.createDate ? [[dates.createFrom, dates.createTo, 'Create Date']] : []),
    ...(checked.planCloseDate ? [[dates.planFrom, dates.planTo, 'PlanCloseDate']] : []),
    ...(checked.closeDate ? [[dates.closeFrom, dates.closeTo, 'CloseDate']] : []),
  ] as const
  for (const [from, to, name] of ranges) {
    if (!from || !to || from > to) return `${name} 起迄需成對且起日不可晚於迄日。`
    if ((Date.parse(`${to}T00:00:00Z`) - Date.parse(`${from}T00:00:00Z`)) / 86_400_000 > 365) return '日期區間最多 366 天。'
  }
  return ''
}

async function query(page = 1): Promise<void> {
  error.value = ''
  if (!canQuery.value) { error.value = unavailableText; return }
  if (!hasEffectiveCondition()) { error.value = '請至少選擇一個有效查詢條件。'; return }
  const dateError = validateDates()
  if (dateError) { error.value = dateError; return }
  if (loading.value) return
  loading.value = true
  try {
    if (dev.value) {
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
      totalCount.value = results.value.length
      currentPage.value = 1
      pageCount.value = results.value.length ? 1 : 0
    } else {
      const url = apiUrl('api/wip/8d-query')
      url.searchParams.set('page', String(page))
      url.searchParams.set('pageSize', '50')
      if (checked.qrsClass) classes.value.forEach(value => url.searchParams.append('qrsClass', String(value)))
      for (const key of textKeys) if (checked[key] && filters[key].trim()) url.searchParams.set(key, filters[key].trim())
      if (checked.createDate) { url.searchParams.set('createFrom', dates.createFrom); url.searchParams.set('createTo', dates.createTo) }
      if (checked.planCloseDate) { url.searchParams.set('planFrom', dates.planFrom); url.searchParams.set('planTo', dates.planTo) }
      if (checked.closeDate) { url.searchParams.set('closeFrom', dates.closeFrom); url.searchParams.set('closeTo', dates.closeTo) }
      if (checked.closeState && closed.value !== notClose.value) url.searchParams.set('closeState', closed.value ? 'closed' : 'open')
      if (props.initialErfNo?.trim()) url.searchParams.set('erfNo', props.initialErfNo.trim())
      const response = await fetch(url, { credentials: 'include', cache: 'no-store' })
      if (!response.ok) throw new Error(await apiErrorMessage(response))
      const result = await response.json() as PageResult
      results.value = Array.isArray(result.items) ? result.items : []
      totalCount.value = result.totalCount
      currentPage.value = result.page
      pageCount.value = result.pageCount
    }
    searched.value = true
    activeNo.value = ''
    message.value = `查得 ${totalCount.value.toLocaleString()} 筆 8D 矯正單。`
  } catch (cause) {
    results.value = []; totalCount.value = 0; pageCount.value = 0; activeNo.value = ''; searched.value = true
    error.value = cause instanceof Error ? cause.message : '8D 矯正單查詢失敗，請重試。'
  } finally { loading.value = false }
}
function openDetail(row = active.value): void {
  if (!row) { error.value = '請先選取資料。'; return }
  emit('open8dQuery',row.qrsNo)
}
function choose(row: Basis): void { activeNo.value = row.qrsNo }
onMounted(() => { if (dev.value) load(); if (props.initialErfNo) void query(1) })
watch(dev, enabled => {
  if (enabled) { error.value = ''; message.value = initialMessage; return }
  sourceRows.value = []; results.value = []; activeNo.value = ''; searched.value = false
  for (const key of textKeys) filters[key] = ''
  for (const key of Object.keys(checked) as (keyof typeof checked)[]) checked[key] = false
  Object.assign(dates, { createFrom: '', createTo: '', planFrom: '', planTo: '', closeFrom: '', closeTo: '' })
  classes.value = []; closed.value = false; notClose.value = false
  totalCount.value = 0; pageCount.value = 0; error.value = ''; message.value = canQuery.value ? initialMessage : unavailableText
})
</script>

<template>
  <section class="wp804">
    <div v-if="dev" class="dev-note">DEV 假資料預覽 · 跨狀態查詢 WP801 主單；8DQuery 以唯讀方式開啟 WP802 十頁。</div>
    <h1>8D 矯正單查詢 <small>8D QUERY · WP804</small></h1>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>
    <fieldset><legend>Query</legend><div class="query-grid">
      <label class="query-item">QRSType <input value="0" readonly title="原 WinForms 顯示 QRSType，但查詢未送出此欄。" disabled></label>
      <div class="query-item"><label><input v-model="checked.qrsClass" type="checkbox" @change="toggleClass" :disabled="loading">QRSClass</label><label v-for="item in classOptions" :key="item.value"><input v-model="classes" type="checkbox" :value="item.value" @change="classChanged" :disabled="loading">{{ item.label }}</label></div>
      <label v-for="key in textKeys" :key="key" class="query-item"><input v-model="checked[key]" type="checkbox" @change="resetText(key)" :disabled="loading">{{ key }} <input v-model="filters[key]" class="yellow" @input="markText(key)" @keydown.enter.prevent="query(1)" :disabled="!checked[key] || loading"><span v-if="key === 'qrsNo' || key === 'qrsSummary'">%</span></label>
      <label class="query-item"><input v-model="checked.createDate" type="checkbox" :disabled="loading">Create Date <input v-model="dates.createFrom" type="date" class="yellow" :disabled="!checked.createDate || loading"> To <input v-model="dates.createTo" type="date" class="yellow" :disabled="!checked.createDate || loading"></label>
      <label class="query-item"><input v-model="checked.planCloseDate" type="checkbox" :disabled="loading">PlanCloseDate <input v-model="dates.planFrom" type="date" class="yellow" :disabled="!checked.planCloseDate || loading"> To <input v-model="dates.planTo" type="date" class="yellow" :disabled="!checked.planCloseDate || loading"></label>
      <label class="query-item"><input v-model="checked.closeDate" type="checkbox" @change="closeDateChanged" :disabled="loading">CloseDate <input v-model="dates.closeFrom" type="date" class="yellow" :disabled="!checked.closeDate || loading"> To <input v-model="dates.closeTo" type="date" class="yellow" :disabled="!checked.closeDate || loading"></label>
      <div class="query-item"><label><input v-model="checked.closeState" type="checkbox" @change="toggleCloseState" :disabled="loading">CloseState</label><label><input v-model="closed" type="checkbox" @change="closeStateChanged" :disabled="loading">Closed</label><label><input v-model="notClose" type="checkbox" @change="closeStateChanged" :disabled="loading">NotClose</label></div>
      <label v-if="initialErfNo" class="query-item">ERFNo <input :value="initialErfNo" readonly></label>
      <button type="button" class="mes-button" :disabled="!canQuery || loading" @click="query(1)">{{ loading ? '查詢中…' : 'Query (&Q)' }}</button>
    </div></fieldset>
      <fieldset class="result"><legend>8D Basis Data</legend><div class="result-content"><div class="table-wrap"><table><thead><tr><th v-for="column in columns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody><tr v-for="row in results" :key="row.qrsNo" :class="{selected:activeNo === row.qrsNo}" tabindex="0" @click="choose(row)" @dblclick="openDetail(row)" @keydown.enter="choose(row)"><td v-for="column in columns" :key="column[0]">{{ row[column[0]] }}</td></tr><tr v-if="!results.length"><td :colspan="columns.length" class="empty">{{ !canQuery ? unavailableText : searched ? (error ? '查詢失敗，請參閱上方訊息。' : '沒有符合資料') : '請選條件並按 Query' }}</td></tr></tbody></table></div><aside><button type="button" class="mes-button" :disabled="!canQuery || !active" @click="openDetail()">8DQuery</button><button type="button" class="mes-button close" @click="emit('close')">Close (&X)</button></aside></div><div class="pager"><span>{{ pageCount ? `第 ${currentPage} / ${pageCount} 頁` : '—' }}　共 {{ totalCount.toLocaleString() }} 筆</span><div><button type="button" class="mes-button" :disabled="loading || currentPage <= 1" @click="query(currentPage - 1)">上一頁</button><button type="button" class="mes-button" :disabled="loading || !hasMore" @click="query(currentPage + 1)">下一頁</button></div></div><div class="status">UserNo: {{ currentUserNo || (dev ? 'DEV' : '—') }}　資料總筆數 {{ totalCount }}</div></fieldset>
  </section>
</template>

<style scoped>
.wp804{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,'Microsoft JhengHei',sans-serif}.dev-note{border:1px solid #d8c994;background:#fff8dc;padding:6px}.wp804 h1{font-size:18px;margin:0}.wp804 h1 small{font-size:12px;font-weight:400;color:#777}.feedback{min-height:18px;margin:0}.feedback.error{color:#a22;background:#fff0ef;padding:4px}.wp804 fieldset{min-width:0;margin:0;padding:8px;border:1px solid #aaa;background:#fff}.wp804 legend{padding:0 5px}.query-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}.query-item{display:flex;align-items:center;gap:7px;min-height:28px}.query-item label{display:inline-flex;align-items:center;gap:4px;white-space:nowrap}.query-item>input.yellow{flex:1;min-width:0}.query-item input[type=date]{width:140px;flex:none}.query-item input[type=text],.query-item>input:not([type]){min-width:0}.yellow{box-sizing:border-box;min-height:26px;padding:3px 5px;border:1px solid #888;background:#fff0b3!important;color:#333;font:inherit}.mes-button{min-height:29px;padding:3px 9px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1f1f1);color:#333;font:inherit;cursor:pointer}.mes-button:disabled{color:#999;cursor:default}.result{flex:1;min-height:230px;display:flex;flex-direction:column}.result-content{display:flex;gap:8px;min-height:0;flex:1}.table-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.table-wrap table{border-collapse:collapse;white-space:nowrap}.table-wrap th,.table-wrap td{padding:5px 7px;border:1px solid #d3d3d3;text-align:left}.table-wrap th{position:sticky;top:0;background:#ead591}.table-wrap td{background:#ffe89c}.table-wrap tr.selected td{background:#f4ca68}.table-wrap tr{cursor:pointer}.table-wrap .empty{text-align:center;background:#fff}.result-content aside{min-width:105px;display:flex;flex-direction:column;gap:6px}.result-content aside .mes-button{width:105px}.result-content aside .close{margin-top:auto}.status{padding-top:5px;color:#666}@media(max-width:900px){.query-grid{grid-template-columns:1fr}.query-item{flex-wrap:wrap}}
</style>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type LookupKind = 'opNo' | 'resItem'
type LookupOption = { value: string; label: string }
type ResourceRow = {
  lotNo: string
  moNo: string
  logGroupSerial: string
  opNo: string
  resClass: 'EMP' | 'EQP'
  resItem: string
  resValue: number
  stdValue: number
  inputQty: number
  outputQty: number
  userNo: string
  productionTime: string
  summaryTime: string
  itemNo: number
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = import.meta.env.DEV && props.developmentPreview
const now = new Date()
const currentMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
const currentDay = `${currentMonth}-${String(now.getDate()).padStart(2, '0')}`

const filters = reactive({
  yearMonthEnabled: false,
  yearMonth: currentMonth,
  opNoEnabled: false,
  opNo: '',
  moNoEnabled: false,
  moNo: '',
  lotNoEnabled: false,
  lotNo: '',
  resClassEnabled: false,
  resClass: '',
  resItemEnabled: false,
  resItem: '',
  productionFromEnabled: false,
  productionFrom: `${currentDay}T00:00:00`,
  productionToEnabled: false,
  productionTo: `${currentDay}T23:59:59`,
  summaryFromEnabled: false,
  summaryFrom: `${currentDay}T00:00:00`,
  summaryToEnabled: false,
  summaryTo: `${currentDay}T23:59:59`,
})
const opOptions: LookupOption[] = [
  { value: 'OP10', label: 'OP10 · Die Attach' }, { value: 'OP20', label: 'OP20 · Wire Bond' },
  { value: 'OP30', label: 'OP30 · Molding' }, { value: 'OP40', label: 'OP40 · Test' }, { value: 'OP50', label: 'OP50 · Packing' },
]
const equipmentOptions: LookupOption[] = [
  { value: 'EQP-DA-01', label: 'EQP-DA-01 · Die Attach #1' }, { value: 'EQP-WB-02', label: 'EQP-WB-02 · Wire Bonder #2' },
  { value: 'EQP-MD-03', label: 'EQP-MD-03 · Molding Press #3' }, { value: 'EQP-FT-04', label: 'EQP-FT-04 · Final Tester #4' },
]
const lookupKind = ref<LookupKind | ''>('')
const lookupSearch = ref('')
const loading = ref(false)
const queried = ref(false)
const error = ref('')
const message = ref('選擇至少一項查詢條件後按 Query。')
const rows = ref<ResourceRow[]>([])

const fixtureRows: ResourceRow[] = Array.from({ length: 72 }, (_, index) => {
  const productionTime = new Date(now.getTime() - index * 4 * 60 * 60 * 1000)
  const summaryTime = new Date(productionTime.getTime() + (index % 5) * 7 * 60 * 1000)
  const resClass = index % 3 === 0 ? 'EMP' : 'EQP'
  const resItem = resClass === 'EMP' ? `U${String(11001 + (index % 5)).padStart(5, '0')}` : equipmentOptions[index % equipmentOptions.length].value
  return {
    lotNo: `LOT-${String(320001 + Math.floor(index / 2)).padStart(6, '0')}`,
    moNo: `MO-${String(51001 + Math.floor(index / 4)).padStart(6, '0')}`,
    logGroupSerial: `LGS-${String(61001 + index).padStart(6, '0')}`,
    opNo: opOptions[index % opOptions.length].value,
    resClass,
    resItem,
    resValue: Number((1.25 + (index % 11) * 0.37).toFixed(2)),
    stdValue: Number((1.5 + (index % 7) * 0.25).toFixed(2)),
    inputQty: 80 + ((index * 13) % 600),
    outputQty: 75 + ((index * 11) % 580),
    userNo: `U${String(11001 + (index % 7)).padStart(5, '0')}`,
    productionTime: productionTime.toISOString(),
    summaryTime: summaryTime.toISOString(),
    itemNo: (index % 4) + 1,
  }
})

const lookupTitle = computed(() => lookupKind.value === 'opNo' ? 'OPNo 單選' : lookupKind.value === 'resItem' ? 'EquipmentNo 單選' : '')
const lookupOptions = computed(() => {
  const options = lookupKind.value === 'opNo' ? opOptions : lookupKind.value === 'resItem' ? equipmentOptions : []
  const keyword = lookupSearch.value.trim().toUpperCase()
  return keyword ? options.filter(item => `${item.value} ${item.label}`.toUpperCase().includes(keyword)) : options
})
const resultCount = computed(() => rows.value.length)

function openLookup(kind: LookupKind): void {
  lookupKind.value = kind
  lookupSearch.value = ''
}
function applyLookup(value: string): void {
  if (lookupKind.value === 'opNo') { filters.opNo = value; filters.opNoEnabled = value !== '' }
  if (lookupKind.value === 'resItem') { filters.resItem = value; filters.resItemEnabled = value !== '' }
  lookupKind.value = ''
}
function updateResClass(value: string): void {
  filters.resClass = value
  filters.resClassEnabled = value !== ''
}
function formatAmount(value: number, digits = 2): string { return value.toLocaleString(undefined, { maximumFractionDigits: digits }) }
function formatDateTime(value: string): string { return new Date(value).toLocaleString() }

async function query(): Promise<void> {
  error.value = ''
  const hasCondition = filters.yearMonthEnabled ||
    (filters.opNoEnabled && filters.opNo.trim() !== '') || (filters.moNoEnabled && filters.moNo.trim() !== '') ||
    (filters.lotNoEnabled && filters.lotNo.trim() !== '') || (filters.resClassEnabled && filters.resClass !== '') ||
    (filters.resItemEnabled && filters.resItem.trim() !== '') || filters.productionFromEnabled || filters.productionToEnabled ||
    filters.summaryFromEnabled || filters.summaryToEnabled
  if (!hasCondition) {
    error.value = '請至少指定一項查詢條件。'
    rows.value = []
    queried.value = true
    return
  }
  if (!dev || loading.value) return

  loading.value = true
  try {
    await new Promise(resolve => window.setTimeout(resolve, 140))
    const monthStart = filters.yearMonthEnabled ? new Date(`${filters.yearMonth}-01T00:00:00`).getTime() : Number.NEGATIVE_INFINITY
    const nextMonthStart = filters.yearMonthEnabled
      ? new Date(Number(filters.yearMonth.slice(0, 4)), Number(filters.yearMonth.slice(5, 7)), 1).getTime()
      : Number.POSITIVE_INFINITY
    const productionFrom = filters.productionFromEnabled ? new Date(filters.productionFrom).getTime() : Number.NEGATIVE_INFINITY
    const productionTo = filters.productionToEnabled ? new Date(filters.productionTo).getTime() : Number.POSITIVE_INFINITY
    const summaryFrom = filters.summaryFromEnabled ? new Date(filters.summaryFrom).getTime() : Number.NEGATIVE_INFINITY
    const summaryTo = filters.summaryToEnabled ? new Date(filters.summaryTo).getTime() : Number.POSITIVE_INFINITY
    rows.value = fixtureRows.filter(row => {
      const productionTime = new Date(row.productionTime).getTime()
      const summaryTime = new Date(row.summaryTime).getTime()
      return productionTime >= monthStart && productionTime < nextMonthStart && productionTime >= productionFrom && productionTime <= productionTo &&
        summaryTime >= summaryFrom && summaryTime <= summaryTo &&
        (!filters.opNoEnabled || !filters.opNo.trim() || row.opNo.toUpperCase() === filters.opNo.trim().toUpperCase()) &&
        (!filters.moNoEnabled || !filters.moNo.trim() || row.moNo.toUpperCase() === filters.moNo.trim().toUpperCase()) &&
        (!filters.lotNoEnabled || !filters.lotNo.trim() || row.lotNo.toUpperCase() === filters.lotNo.trim().toUpperCase()) &&
        (!filters.resClassEnabled || !filters.resClass || row.resClass === filters.resClass) &&
        (!filters.resItemEnabled || !filters.resItem.trim() || row.resItem.toUpperCase() === filters.resItem.trim().toUpperCase())
    }).sort((left, right) => left.productionTime.localeCompare(right.productionTime))
    queried.value = true
    message.value = `查得 ${rows.value.length} 筆資源分攤資料。`
  } finally { loading.value = false }
}
</script>

<template>
  <section v-if="dev" class="resource-page" aria-label="生產批資源檢視分攤" :aria-busy="loading">
    <header class="page-head"><div><p class="eyebrow">RPT · RT608 · RESOURCE SHARE</p><h1>生產批資源檢視(分攤)</h1><p>查詢報工時彙整的員工／設備資源值、標準值與投入產出數量。</p></div><button type="button" class="quiet" @click="emit('close')">Close (X)</button></header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>

    <fieldset class="query-panel"><legend>Query</legend><div class="filter-grid">
      <div class="filter-control"><label><input v-model="filters.yearMonthEnabled" type="checkbox" :disabled="loading">YearMonth</label><input v-model="filters.yearMonth" type="month" :disabled="loading"></div>
      <div class="filter-control"><label><input v-model="filters.opNoEnabled" type="checkbox" :disabled="loading" @change="!filters.opNoEnabled && (filters.opNo = '')">OP No</label><div class="pick"><input v-model="filters.opNo" readonly :disabled="loading" placeholder="單選"><button type="button" :disabled="loading" aria-label="選擇 OPNo" @click="openLookup('opNo')">...</button></div></div>
      <div class="filter-control"><label><input v-model="filters.moNoEnabled" type="checkbox" :disabled="loading" @change="!filters.moNoEnabled && (filters.moNo = '')">MO No</label><input v-model.trim="filters.moNo" :disabled="loading" maxlength="60" placeholder="精確工單" @input="filters.moNoEnabled = filters.moNo.trim().length > 0"></div>
      <div class="filter-control"><label><input v-model="filters.lotNoEnabled" type="checkbox" :disabled="loading" @change="!filters.lotNoEnabled && (filters.lotNo = '')">Lot No</label><input v-model.trim="filters.lotNo" :disabled="loading" maxlength="60" placeholder="精確批號" @input="filters.lotNoEnabled = filters.lotNo.trim().length > 0"></div>
      <div class="filter-control"><label><input v-model="filters.resClassEnabled" type="checkbox" :disabled="loading" @change="!filters.resClassEnabled && (filters.resClass = '')">ResClass</label><select :value="filters.resClass" :disabled="loading" @change="updateResClass(($event.target as HTMLSelectElement).value)"><option value="">選擇</option><option value="EMP">EMP</option><option value="EQP">EQP</option></select></div>
      <div class="filter-control"><label><input v-model="filters.resItemEnabled" type="checkbox" :disabled="loading" @change="!filters.resItemEnabled && (filters.resItem = '')">ResItem</label><div class="pick"><input v-model.trim="filters.resItem" :disabled="loading" maxlength="80" placeholder="精確資源代碼" @input="filters.resItemEnabled = filters.resItem.trim().length > 0"><button type="button" :disabled="loading" aria-label="選擇 EquipmentNo" @click="openLookup('resItem')">...</button></div></div>
      <div class="filter-control date-control"><label>Production Time</label><div class="date-range"><label><input v-model="filters.productionFromEnabled" type="checkbox" :disabled="loading">From</label><input v-model="filters.productionFrom" type="datetime-local" step="1" :disabled="loading"><label><input v-model="filters.productionToEnabled" type="checkbox" :disabled="loading">To</label><input v-model="filters.productionTo" type="datetime-local" step="1" :disabled="loading"></div></div>
      <div class="filter-control date-control"><label>Summary Time</label><div class="date-range"><label><input v-model="filters.summaryFromEnabled" type="checkbox" :disabled="loading">From</label><input v-model="filters.summaryFrom" type="datetime-local" step="1" :disabled="loading"><label><input v-model="filters.summaryToEnabled" type="checkbox" :disabled="loading">To</label><input v-model="filters.summaryTo" type="datetime-local" step="1" :disabled="loading"></div></div>
      <button type="button" class="primary" :disabled="loading" @click="query">{{ loading ? '查詢中…' : 'Query' }}</button>
    </div></fieldset>

    <section class="result-panel"><div class="result-head"><div><h2>Resource Share</h2><p>ProductionTime 升冪；合成資料僅供 DEV 互動示意。</p></div><span>{{ resultCount }} 筆</span></div>
      <div class="table-scroll"><table><thead><tr><th>LotNo</th><th>MONo</th><th>OPNo</th><th>ResClass</th><th>ResItem</th><th class="num">ResValue</th><th class="num">StdValue</th><th class="num">InputQty</th><th class="num">OutputQty</th><th>UserNo</th><th>ProductionTime</th><th>SummaryTime</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="`${row.logGroupSerial}-${row.itemNo}`"><td class="mono">{{ row.lotNo }}</td><td>{{ row.moNo }}</td><td>{{ row.opNo }}</td><td>{{ row.resClass }}</td><td>{{ row.resItem }}</td><td class="num">{{ formatAmount(row.resValue) }}</td><td class="num">{{ formatAmount(row.stdValue) }}</td><td class="num">{{ formatAmount(row.inputQty, 4) }}</td><td class="num">{{ formatAmount(row.outputQty, 4) }}</td><td>{{ row.userNo }}</td><td class="mono">{{ formatDateTime(row.productionTime) }}</td><td class="mono">{{ formatDateTime(row.summaryTime) }}</td></tr>
        <tr v-if="queried && !rows.length"><td colspan="12" class="empty">沒有符合條件的資源分攤資料</td></tr><tr v-if="!queried"><td colspan="12" class="empty">設定條件後按 Query</td></tr>
      </tbody></table></div>
    </section>
    <footer class="source-note">Resource lookup fixture 模擬 `IssueState=全部`；來源目前只以 EquipmentBasis 作為 ResItem picker，EMP 資源沒有對應 picker。</footer>

    <div v-if="lookupKind" class="overlay" @click.self="lookupKind = ''"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle"><header><div><h2>{{ lookupTitle }}</h2><p>單選後回填條件。</p></div><button type="button" class="quiet" @click="lookupKind = ''">Close</button></header><input v-model.trim="lookupSearch" class="lookup-search" type="search" placeholder="搜尋代碼或名稱"><div class="lookup-list"><button v-for="option in lookupOptions" :key="option.value" type="button" @click="applyLookup(option.value)"><strong>{{ option.value }}</strong><span>{{ option.label }}</span></button><p v-if="!lookupOptions.length" class="empty">沒有符合的選項</p></div></section></div>
  </section>
  <section v-else class="pending" aria-label="生產批資源分攤正式環境狀態"><p role="status">正式資源分攤 API／資料來源尚未核實，此頁不載入或修改正式資料。</p><button type="button" class="quiet" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.resource-page,.pending{box-sizing:border-box;min-height:0;height:100%;padding:12px;background:#f5f7fa;color:#101828;font:inherit;overflow:auto}.page-head,.result-head,.lookup-dialog header{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:0;font-size:22px}.page-head p,.result-head p,.lookup-dialog header p{margin:3px 0;color:#667085;font-size:12px}.eyebrow{font-size:10px!important;letter-spacing:.08em;color:#475467!important}.quiet,.primary,.pick button{min-height:30px;padding:4px 11px;border:1px solid #98a2b3;border-radius:5px;background:#fff;color:#344054;font:inherit;cursor:pointer}.primary{border-color:#175cd3;background:#175cd3;color:#fff}.primary:disabled,.pick button:disabled{opacity:.55;cursor:default}.feedback{margin:8px 0;padding:7px 10px;border-radius:5px;background:#eff8ff;color:#175cd3;font-size:12px}.feedback.error{background:#fef3f2;color:#b42318}.query-panel{margin:0;padding:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.query-panel legend{padding:0 5px;font-weight:600}.filter-grid{display:grid;grid-template-columns:repeat(3,minmax(180px,1fr)) auto;align-items:end;gap:10px 12px}.filter-control{display:grid;gap:5px;color:#475467;font-size:11px;font-weight:600}.filter-control>label{display:flex;align-items:center;gap:5px}.filter-control>input,.filter-control>select,.pick input,.date-range input,.lookup-search{box-sizing:border-box;width:100%;height:30px;padding:4px 7px;border:1px solid #d0d5dd;border-radius:4px;background:#fff;color:#101828;font:inherit}.pick,.date-range{display:flex;align-items:center;gap:5px}.pick button{min-width:32px;padding:2px}.date-control{grid-column:span 2}.date-range label{display:flex;align-items:center;gap:3px;white-space:nowrap}.result-panel{display:flex;flex-direction:column;min-height:280px;height:calc(100% - 185px);margin-top:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.result-head{padding:9px 11px}.result-head h2{margin:0;font-size:14px}.result-head>span{color:#667085;font-size:11px}.table-scroll{min-height:0;flex:1;overflow:auto;border-top:1px solid #eaecf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th,td{padding:6px 8px;border-bottom:1px solid #eaecf0;text-align:left}th{position:sticky;top:0;z-index:1;background:#f9fafb;color:#475467}td.num,th.num{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace}.empty{text-align:center!important;color:#98a2b3;padding:18px}.source-note{display:block;padding:6px 2px;color:#667085;font-size:10px}.overlay{position:fixed;inset:0;z-index:2100;display:grid;place-items:center;padding:16px;background:#10182866}.lookup-dialog{display:flex;flex-direction:column;width:min(560px,96vw);max-height:78vh;padding:14px;border:1px solid #d0d5dd;border-radius:9px;background:#fff;box-shadow:0 18px 44px #10182840}.lookup-dialog header h2{margin:0;font-size:16px}.lookup-search{margin:10px 0}.lookup-list{min-height:0;overflow:auto}.lookup-list button{display:flex;width:100%;gap:14px;padding:8px 5px;border:0;border-bottom:1px solid #eaecf0;background:#fff;text-align:left;font:inherit;cursor:pointer}.lookup-list button:hover{background:#f9fafb}.lookup-list button strong{min-width:110px}.lookup-list .empty{margin:0}.pending{display:flex;align-items:center;justify-content:space-between;gap:12px}.pending p{margin:0;color:#667085}.pending .quiet{flex-shrink:0}@media(max-width:1050px){.filter-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.result-panel{height:45vh}}@media(max-width:680px){.page-head{align-items:flex-start}.page-head h1{font-size:18px}.filter-grid{grid-template-columns:1fr}.date-control{grid-column:auto}.date-range{flex-wrap:wrap}.result-panel{height:55vh}.pending{align-items:flex-start;flex-direction:column}}
</style>

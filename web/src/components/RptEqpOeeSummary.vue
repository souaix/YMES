<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'

type OeeRow = {
  areaNo: string
  equipmentType: string
  equipmentNo: string
  belongDate: string
  availableTime: number
  availRate: number
  realOutput: number
  stdOutput: number
  perforRate: number | null
  totalOutput: number | null
  totalInput: number | null
  yieldRate: number | null
  oee: number
}
type LookupKind = 'area' | 'equipmentType' | 'equipmentNo'
type LookupOption = { value: string; label: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = import.meta.env.DEV && props.developmentPreview
const now = new Date()
function dateText(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` }
const filters = reactive({ startDate: dateText(new Date(now.getFullYear(), now.getMonth(), 1)), endDate: dateText(now), areaNo: '', equipmentType: '', equipmentNo: '' })
const areaEnabled = ref(false)
const equipmentTypeEnabled = ref(false)
const equipmentNoEnabled = ref(false)
const loading = ref(false)
const queried = ref(false)
const error = ref('')
const message = ref('請選擇日期區間後按 Query。')
const rows = ref<OeeRow[]>([])
const lookupKind = ref<LookupKind | ''>('')
const lookupSearch = ref('')
const lookupSelection = ref<string[]>([])

const areaOptions: LookupOption[] = [
  { value: 'AREA-A', label: 'AREA-A · 組裝一區' },
  { value: 'AREA-B', label: 'AREA-B · 測試二區' },
  { value: 'AREA-C', label: 'AREA-C · 包裝區' },
]
const equipmentTypeOptions: LookupOption[] = [
  { value: 'BOND', label: 'BOND · 固晶機' },
  { value: 'ETCH', label: 'ETCH · 蝕刻機' },
  { value: 'OVEN', label: 'OVEN · 烤箱' },
  { value: 'TESTER', label: 'TESTER · 測試機' },
]
const equipmentOptions = [
  { equipmentNo: 'BOND-01', equipmentType: 'BOND', areaNo: 'AREA-A', state: 2, metrics: [540, .92, 144, 156, .92, 147, 150, .98, .83] },
  { equipmentNo: 'BOND-02', equipmentType: 'BOND', areaNo: 'AREA-A', state: 2, metrics: [515, .88, 133, 150, .89, 136, 140, .97, .76] },
  { equipmentNo: 'ETCH-01', equipmentType: 'ETCH', areaNo: 'AREA-B', state: 2, metrics: [560, .95, 180, 184, .98, 182, 184, .99, .92] },
  { equipmentNo: 'OVEN-01', equipmentType: 'OVEN', areaNo: 'AREA-C', state: 2, metrics: [500, .86, 108, 120, .90, 110, 114, .96, .74] },
  { equipmentNo: 'TEST-01', equipmentType: 'TESTER', areaNo: 'AREA-B', state: 2, metrics: [575, .97, 196, 200, .98, 198, 200, .99, .94] },
]
const fixtureRows: OeeRow[] = Array.from({ length: 21 }, (_, dayIndex) => {
  const belongDate = new Date(now.getFullYear(), now.getMonth(), now.getDate() - dayIndex)
  return equipmentOptions.map(equipment => ({
    areaNo: equipment.areaNo,
    equipmentType: equipment.equipmentType,
    equipmentNo: equipment.equipmentNo,
    belongDate: dateText(belongDate),
    availableTime: equipment.metrics[0],
    availRate: equipment.metrics[1],
    realOutput: equipment.metrics[2],
    stdOutput: equipment.metrics[3],
    perforRate: equipment.metrics[4],
    totalOutput: equipment.metrics[5],
    totalInput: equipment.metrics[6],
    yieldRate: equipment.metrics[7],
    oee: equipment.metrics[8],
  }))
}).flat()

const activeLookupTitle = computed(() => lookupKind.value === 'area' ? 'AreaNo 多選' : lookupKind.value === 'equipmentType' ? 'EquipmentType 多選' : lookupKind.value === 'equipmentNo' ? 'EquipmentNo 多選' : '')
const lookupOptions = computed(() => {
  let options: LookupOption[] = []
  if (lookupKind.value === 'area') options = areaOptions
  else if (lookupKind.value === 'equipmentType') options = equipmentTypeOptions
  else if (lookupKind.value === 'equipmentNo') {
    const types = splitValues(filters.equipmentType)
    options = equipmentOptions.filter(item => !types.length || types.includes(item.equipmentType)).map(item => ({ value: item.equipmentNo, label: `${item.equipmentNo} · ${item.equipmentType} · ${item.areaNo}` }))
  }
  const keyword = lookupSearch.value.trim().toUpperCase()
  return keyword ? options.filter(item => `${item.value} ${item.label}`.toUpperCase().includes(keyword)) : options
})
const resultCount = computed(() => rows.value.length)

watch(() => filters.equipmentType, value => { equipmentTypeEnabled.value = value.trim().length > 0 })
watch(() => filters.equipmentNo, value => { equipmentNoEnabled.value = value.trim().length > 0 })

function splitValues(value: string): string[] { return value.split(',').map(item => item.trim().toUpperCase()).filter(Boolean) }
function setEnabled(kind: LookupKind, enabled: boolean): void {
  if (kind === 'area') { areaEnabled.value = enabled; if (!enabled) filters.areaNo = '' }
  if (kind === 'equipmentType') { equipmentTypeEnabled.value = enabled; if (!enabled) filters.equipmentType = '' }
  if (kind === 'equipmentNo') { equipmentNoEnabled.value = enabled; if (!enabled) filters.equipmentNo = '' }
}
function openLookup(kind: LookupKind): void {
  lookupKind.value = kind
  lookupSearch.value = ''
  const current = kind === 'area' ? filters.areaNo : kind === 'equipmentType' ? filters.equipmentType : filters.equipmentNo
  lookupSelection.value = splitValues(current)
}
function toggleLookup(value: string): void {
  lookupSelection.value = lookupSelection.value.includes(value)
    ? lookupSelection.value.filter(item => item !== value)
    : [...lookupSelection.value, value]
}
function applyLookup(): void {
  const value = lookupSelection.value.join(', ')
  if (lookupKind.value === 'area') { filters.areaNo = value; areaEnabled.value = lookupSelection.value.length > 0 }
  if (lookupKind.value === 'equipmentType') { filters.equipmentType = value; equipmentTypeEnabled.value = lookupSelection.value.length > 0 }
  if (lookupKind.value === 'equipmentNo') { filters.equipmentNo = value; equipmentNoEnabled.value = lookupSelection.value.length > 0 }
  lookupKind.value = ''
}
function optionSelected(values: string[], value: string): boolean { return values.includes(value) }

async function query(): Promise<void> {
  error.value = ''
  if (!filters.startDate || !filters.endDate || filters.startDate >= filters.endDate) {
    error.value = '日期起迄不正確，開始日期必須早於結束日期。'
    rows.value = []
    queried.value = true
    return
  }
  const today = dateText(new Date())
  if (filters.startDate > today || filters.endDate > today) {
    error.value = '日期不可晚於今天。'
    rows.value = []
    queried.value = true
    return
  }
  if (!dev || loading.value) return
  loading.value = true
  try {
    await new Promise(resolve => window.setTimeout(resolve, 140))
    const areas = splitValues(filters.areaNo)
    const types = splitValues(filters.equipmentType)
    const equipments = splitValues(filters.equipmentNo)
    rows.value = fixtureRows.filter(row => row.belongDate >= filters.startDate && row.belongDate <= filters.endDate &&
      (!areaEnabled.value || (areas.length > 0 && areas.includes(row.areaNo))) &&
      (!equipmentTypeEnabled.value || (types.length > 0 && types.includes(row.equipmentType))) &&
      (!equipmentNoEnabled.value || (equipments.length > 0 && equipments.includes(row.equipmentNo)))
    ).sort((left, right) => left.equipmentNo.localeCompare(right.equipmentNo) || left.belongDate.localeCompare(right.belongDate))
    queried.value = true
    message.value = `查得 ${rows.value.length} 筆 OEE 彙總資料。`
  } finally { loading.value = false }
}

function displayNumber(value: number | null): string { return value === null ? '' : value.toLocaleString(undefined, { maximumFractionDigits: 2 }) }
function displayRate(value: number | null): string { return value === null ? '' : `${(value * 100).toLocaleString(undefined, { maximumFractionDigits: 2 })}%` }
</script>

<template>
  <section v-if="dev" class="oee-page" aria-label="整體設備效率 OEE 查詢" :aria-busy="loading">
    <header class="page-head"><div><p class="eyebrow">RPT · RT416 · PRECOMPUTED OEE SUMMARY</p><h1>整體設備效率 (OEE)</h1><p>依彙總日期與設備條件查詢可用率、產能效率、良率及 OEE。</p></div><button type="button" class="quiet" @click="emit('close')">Close (X)</button></header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>

    <fieldset class="query-panel"><legend>Query</legend><div class="filter-grid">
      <label>BelongDate(Start)<input v-model="filters.startDate" type="date" :disabled="loading"></label>
      <label>BelongDate(End)<input v-model="filters.endDate" type="date" :disabled="loading"></label>
      <div class="filter-control"><label><input :checked="areaEnabled" type="checkbox" :disabled="loading" @change="setEnabled('area', ($event.target as HTMLInputElement).checked)">AreaNo</label><div class="pick"><input v-model="filters.areaNo" maxlength="200" :disabled="!areaEnabled || loading" placeholder="逗號分隔"><button type="button" :disabled="loading" aria-label="選擇 AreaNo" @click="openLookup('area')">...</button></div></div>
      <div class="filter-control"><label><input :checked="equipmentTypeEnabled" type="checkbox" :disabled="loading" @change="setEnabled('equipmentType', ($event.target as HTMLInputElement).checked)">EquipmentType</label><div class="pick"><input v-model="filters.equipmentType" maxlength="200" :disabled="loading" placeholder="逗號分隔"><button type="button" :disabled="loading" aria-label="選擇 EquipmentType" @click="openLookup('equipmentType')">...</button></div></div>
      <div class="filter-control"><label><input :checked="equipmentNoEnabled" type="checkbox" :disabled="loading" @change="setEnabled('equipmentNo', ($event.target as HTMLInputElement).checked)">EquipmentNo</label><div class="pick"><input v-model="filters.equipmentNo" maxlength="500" :disabled="loading" placeholder="逗號分隔"><button type="button" :disabled="loading" aria-label="選擇 EquipmentNo" @click="openLookup('equipmentNo')">...</button></div></div>
      <button type="button" class="primary" :disabled="loading" @click="query">{{ loading ? '查詢中…' : 'Query' }}</button>
    </div></fieldset>

    <section class="result-panel"><div class="result-head"><div><h2>OEE Summary</h2><p>資料由 MESAutoRun 彙整；此頁只讀取預先計算欄位，不在瀏覽器重算指標。</p></div><span>{{ resultCount }} 筆</span></div>
      <div class="table-scroll"><table><thead><tr><th>AreaNo</th><th>EquipmentType</th><th>EquipmentNo</th><th>BelongDate</th><th class="num">EQPAvailableTime</th><th class="num">AvailRate</th><th class="num">EQPRealOutput</th><th class="num">EQPSTDOutput</th><th class="num">PerforRate</th><th class="num">EQPTotalOutput</th><th class="num">EQPTotalInput</th><th class="num">Yield</th><th class="num">OEE</th></tr></thead><tbody>
        <tr v-for="(row, index) in rows" :key="`${row.equipmentNo}-${row.belongDate}-${index}`"><td>{{ row.areaNo }}</td><td>{{ row.equipmentType }}</td><td class="mono">{{ row.equipmentNo }}</td><td>{{ row.belongDate }}</td><td class="num">{{ displayNumber(row.availableTime) }}</td><td class="num">{{ displayRate(row.availRate) }}</td><td class="num">{{ displayNumber(row.realOutput) }}</td><td class="num">{{ displayNumber(row.stdOutput) }}</td><td class="num">{{ displayRate(row.perforRate) }}</td><td class="num">{{ displayNumber(row.totalOutput) }}</td><td class="num">{{ displayNumber(row.totalInput) }}</td><td class="num">{{ displayRate(row.yieldRate) }}</td><td class="num strong">{{ displayRate(row.oee) }}</td></tr>
        <tr v-if="queried && !rows.length"><td colspan="13" class="empty">沒有符合條件的 OEE 彙總資料</td></tr><tr v-if="!queried"><td colspan="13" class="empty">設定查詢條件後按 Query</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="lookupKind" class="overlay" @click.self="lookupKind = ''"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="activeLookupTitle"><header><div><h2>{{ activeLookupTitle }}</h2><p>多選後回填逗號分隔條件。</p></div><button type="button" class="quiet" @click="lookupKind = ''">Close</button></header><input v-model.trim="lookupSearch" class="lookup-search" type="search" placeholder="搜尋代碼或名稱"><div class="lookup-list"><label v-for="option in lookupOptions" :key="option.value"><input type="checkbox" :checked="optionSelected(lookupSelection, option.value)" @change="toggleLookup(option.value)"><span>{{ option.label }}</span></label><p v-if="!lookupOptions.length" class="empty">沒有符合的設備選項</p></div><footer><span>已選 {{ lookupSelection.length }} 筆</span><div><button type="button" class="quiet" @click="lookupSelection = []">清除</button><button type="button" class="primary" @click="applyLookup">套用</button></div></footer></section></div>
    <footer class="source-note">DEV fixture 為合成資料；設備選項僅模擬 AreaClass=0、EquipmentType／Equipment IssueState=2 查找結果。</footer>
  </section>
  <section v-else class="pending" aria-label="整體設備效率正式環境狀態"><p role="status">正式 OEE API／資料來源尚未核實，此頁不載入或修改正式資料。</p><button type="button" class="quiet" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.oee-page,.pending{box-sizing:border-box;min-height:0;height:100%;padding:12px;background:#f5f7fa;color:#101828;font:inherit;overflow:auto}.page-head,.result-head,.lookup-dialog header,.lookup-dialog footer{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:0;font-size:22px}.page-head p,.result-head p,.lookup-dialog header p{margin:3px 0;color:#667085;font-size:12px}.eyebrow{font-size:10px!important;letter-spacing:.08em;color:#475467!important}.quiet,.primary,.pick button{min-height:30px;padding:4px 11px;border:1px solid #98a2b3;border-radius:5px;background:#fff;color:#344054;font:inherit;cursor:pointer}.primary{border-color:#175cd3;background:#175cd3;color:#fff}.primary:disabled,.pick button:disabled{opacity:.55;cursor:default}.feedback{margin:8px 0;padding:7px 10px;border-radius:5px;background:#eff8ff;color:#175cd3;font-size:12px}.feedback.error{background:#fef3f2;color:#b42318}.query-panel{margin:0;padding:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.query-panel legend{padding:0 5px;font-weight:600}.filter-grid{display:grid;grid-template-columns:repeat(3,minmax(190px,1fr)) auto;align-items:end;gap:11px 14px}.filter-grid>label,.filter-control{display:grid;gap:5px;color:#475467;font-size:11px;font-weight:600}.filter-grid>label input,.pick input,.lookup-search{box-sizing:border-box;width:100%;height:30px;padding:4px 7px;border:1px solid #d0d5dd;border-radius:4px;background:#fff;color:#101828;font:inherit}.filter-control>label{display:flex;align-items:center;gap:5px}.pick{display:flex;gap:5px}.pick button{min-width:32px;padding:2px}.result-panel{display:flex;flex-direction:column;min-height:260px;height:calc(100% - 150px);margin-top:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.result-head{padding:9px 11px}.result-head h2{margin:0;font-size:14px}.result-head>span{color:#667085;font-size:11px}.table-scroll{min-height:0;flex:1;overflow:auto;border-top:1px solid #eaecf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th,td{padding:6px 8px;border-bottom:1px solid #eaecf0;text-align:left}th{position:sticky;top:0;z-index:1;background:#f9fafb;color:#475467}td.num,th.num{text-align:right;font-variant-numeric:tabular-nums}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace}.strong{font-weight:700;color:#175cd3}.empty{text-align:center!important;color:#98a2b3;padding:18px}.overlay{position:fixed;inset:0;z-index:2100;display:grid;place-items:center;padding:16px;background:#10182866}.lookup-dialog{display:flex;flex-direction:column;width:min(560px,96vw);max-height:78vh;padding:14px;border:1px solid #d0d5dd;border-radius:9px;background:#fff;box-shadow:0 18px 44px #10182840}.lookup-dialog header h2{margin:0;font-size:16px}.lookup-search{margin:10px 0}.lookup-list{min-height:0;overflow:auto}.lookup-list label{display:flex;align-items:center;gap:8px;padding:7px 5px;border-bottom:1px solid #eaecf0;font-size:12px}.lookup-list label:hover{background:#f9fafb}.lookup-list .empty{margin:0}.lookup-dialog footer{padding-top:10px;border-top:1px solid #eaecf0;color:#667085;font-size:11px}.lookup-dialog footer>div{display:flex;gap:7px}.source-note{display:block;padding:6px 2px;color:#667085;font-size:10px}.pending{display:flex;align-items:center;justify-content:space-between;gap:12px}.pending p{margin:0;color:#667085}.pending .quiet{flex-shrink:0}@media(max-width:980px){.filter-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.result-panel{height:auto;min-height:360px}}@media(max-width:620px){.page-head{align-items:flex-start}.page-head h1{font-size:18px}.filter-grid{grid-template-columns:1fr}.result-panel{height:55vh}.pending{align-items:flex-start;flex-direction:column}}
</style>

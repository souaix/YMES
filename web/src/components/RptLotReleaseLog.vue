<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type LookupKind = 'productNo' | 'opNo' | 'userActiveId'
type LookupOption = { value: string; label: string }
type ReleaseRow = {
  eventTime: string
  sysActiveSequence: number
  userActiveId: number
  lotNo: string
  lotSerial: string
  customerLotNo: string
  productNo: string
  productName: string
  productVersion: string
  opNo: string
  opName: string
  moNo: string
  processNo: string
  processVersion: string
  userNo: string
  logGroupSerial: string
  dataKey: string
  dType: number
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = import.meta.env.DEV && props.developmentPreview
const now = new Date()

function dateTimeText(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
}

function initialStart(): Date {
  const start = new Date(now)
  if (now.getHours() < 7 || (now.getHours() === 7 && now.getMinutes() < 30)) start.setDate(start.getDate() - 1)
  start.setHours(7, 30, 0, 0)
  return start
}

const statusOptions: LookupOption[] = [
  { value: '0', label: 'START · 下線' }, { value: '1', label: 'CI · 進站' }, { value: '2', label: 'CO · 出站' },
  { value: '3', label: 'SPC · InLine SPC' }, { value: '4', label: 'SPLIT · 分批' }, { value: '5', label: 'MERGE · 併批' },
  { value: '6', label: 'CHANGEPRD · 產品變更' }, { value: '7', label: 'DELETE · 生產批刪除' }, { value: '8', label: 'REVERSE · 生產批還原' },
  { value: '9', label: 'HOLD · 異常開立' }, { value: '10', label: 'HOLDRELEASE · 異常解除' },
  { value: '11', label: 'WAIT · 等待開立' }, { value: '12', label: 'WAITRELEASE · 等待解除' },
  { value: '13', label: 'INVENTORY · 入庫' }, { value: '14', label: 'INVCANCEL · 入庫取消' },
  { value: '15', label: 'TURNINVENTORY · 轉庫' }, { value: '16', label: 'RERUN · 重新上線' },
  { value: '17', label: 'CLOSE · 生產批結批' }, { value: '18', label: 'QTYADJUST · 數量調整' }, { value: '19', label: 'INVDELETE · 入庫刪除' },
]

const productOptions: LookupOption[] = [
  { value: 'PRD-1001', label: 'PRD-1001 · Controller A' }, { value: 'PRD-1002', label: 'PRD-1002 · Power Module B' },
  { value: 'PRD-2001', label: 'PRD-2001 · Sensor C' }, { value: 'PRD-3001', label: 'PRD-3001 · Wafer Assembly' },
]
const opOptions: LookupOption[] = [
  { value: 'OP10', label: 'OP10 · Die Attach' }, { value: 'OP20', label: 'OP20 · Wire Bond' },
  { value: 'OP30', label: 'OP30 · Molding' }, { value: 'OP40', label: 'OP40 · Test' }, { value: 'OP50', label: 'OP50 · Packing' },
]

const filters = reactive({
  processDateEnabled: false,
  startDateTime: dateTimeText(initialStart()),
  endDateTime: dateTimeText(now),
  lotNoEnabled: false,
  lotNo: '',
  moNoEnabled: false,
  moNo: '',
  productNoEnabled: false,
  productNo: '',
  opNoEnabled: false,
  opNo: '',
  userNoEnabled: false,
  userNo: '',
  userActiveIdEnabled: false,
  userActiveIds: [] as string[],
  showErf: false,
  showWait: false,
})
const loading = ref(false)
const queried = ref(false)
const error = ref('')
const message = ref('選擇 ProcessDate 或輸入 LotNo／MONo 後查詢。')
const rows = ref<ReleaseRow[]>([])
const selectedRow = ref<ReleaseRow | null>(null)
const lookupKind = ref<LookupKind | ''>('')
const lookupSearch = ref('')
const lookupSelection = ref<string[]>([])

const fixtureRows: ReleaseRow[] = Array.from({ length: 48 }, (_, index) => {
  const userActiveId = index % 20
  const eventTime = new Date(now.getTime() - index * 90 * 60 * 1000)
  let dType = 9
  if (userActiveId === 9 || userActiveId === 10) dType = 1
  else if (userActiveId === 11 || userActiveId === 12) dType = 2
  else if (userActiveId === 4) dType = index % 2 === 0 ? 3 : 4
  else if (userActiveId === 5) dType = index % 2 === 0 ? 5 : 6
  const productNo = ['PRD-1001', 'PRD-1002', 'PRD-2001', 'PRD-3001'][index % 4]
  const opNo = ['OP10', 'OP20', 'OP30', 'OP40', 'OP50'][index % 5]
  return {
    eventTime: eventTime.toISOString(),
    sysActiveSequence: (index % 4) + 1,
    userActiveId,
    lotNo: `LOT-${String(260001 + Math.floor(index / 3)).padStart(6, '0')}`,
    lotSerial: `LS-${String(70001 + index).padStart(6, '0')}`,
    customerLotNo: `CLOT-${String(45001 + index).padStart(6, '0')}`,
    productNo,
    productName: ['Controller A', 'Power Module B', 'Sensor C', 'Wafer Assembly'][index % 4],
    productVersion: `R${(index % 3) + 1}`,
    opNo,
    opName: ['Die Attach', 'Wire Bond', 'Molding', 'Test', 'Packing'][index % 5],
    moNo: `MO-${String(9001 + Math.floor(index / 4)).padStart(6, '0')}`,
    processNo: `PROC-${String((index % 5) + 1).padStart(2, '0')}`,
    processVersion: `V${(index % 2) + 1}`,
    userNo: `U${String(11001 + (index % 7)).padStart(5, '0')}`,
    logGroupSerial: `LGS-${String(81001 + index).padStart(6, '0')}`,
    dataKey: `DK-${String(91001 + index).padStart(6, '0')}`,
    dType,
  }
})

const lookupTitle = computed(() => lookupKind.value === 'productNo' ? 'ProductNo 多選' : lookupKind.value === 'opNo' ? 'OPNo 多選' : lookupKind.value === 'userActiveId' ? 'UserActiveID 多選' : '')
const lookupOptions = computed(() => {
  const options = lookupKind.value === 'productNo' ? productOptions : lookupKind.value === 'opNo' ? opOptions : lookupKind.value === 'userActiveId' ? statusOptions : []
  const keyword = lookupSearch.value.trim().toUpperCase()
  return keyword ? options.filter(item => `${item.value} ${item.label}`.toUpperCase().includes(keyword)) : options
})
const resultCount = computed(() => rows.value.length)
const componentRows = computed(() => selectedRow.value ? [
  { componentNo: `COMP-${selectedRow.value.productNo.slice(-4)}-01`, componentType: 'Material', quantity: 24, cpass: 0 },
  { componentNo: `COMP-${selectedRow.value.productNo.slice(-4)}-02`, componentType: 'Sub Assembly', quantity: 8, cpass: 1 },
] : [])
const binRows = computed(() => selectedRow.value ? [
  { lotSerial: selectedRow.value.lotSerial, binNo: 'BIN-A01', cpass: 0, quantity: 96 },
  { lotSerial: selectedRow.value.lotSerial, binNo: 'BIN-A02', cpass: 1, quantity: 4 },
] : [])
const detailKind = computed(() => {
  const dtype = selectedRow.value?.dType
  if (dtype === 1) return 'ERF'
  if (dtype === 2) return 'Wait'
  if (dtype === 3 || dtype === 4) return 'Split'
  if (dtype === 5 || dtype === 6) return 'Merge'
  return 'Lot'
})

function splitValues(value: string): string[] { return value.split(',').map(item => item.trim().toUpperCase()).filter(Boolean) }
function selected(values: string[], value: string): boolean { return values.includes(value) }
function toggleLookup(value: string): void {
  lookupSelection.value = selected(lookupSelection.value, value) ? lookupSelection.value.filter(item => item !== value) : [...lookupSelection.value, value]
}
function openLookup(kind: LookupKind): void {
  lookupKind.value = kind
  lookupSearch.value = ''
  const current = kind === 'productNo' ? filters.productNo : kind === 'opNo' ? filters.opNo : filters.userActiveIds.join(',')
  lookupSelection.value = splitValues(current)
}
function applyLookup(): void {
  const value = lookupSelection.value.join(', ')
  if (lookupKind.value === 'productNo') { filters.productNo = value; filters.productNoEnabled = lookupSelection.value.length > 0 }
  if (lookupKind.value === 'opNo') { filters.opNo = value; filters.opNoEnabled = lookupSelection.value.length > 0 }
  if (lookupKind.value === 'userActiveId') { filters.userActiveIds = lookupSelection.value; filters.userActiveIdEnabled = lookupSelection.value.length > 0 }
  lookupKind.value = ''
}
function activeName(value: number): string { return statusOptions.find(item => item.value === String(value))?.label ?? String(value) }
function showTypeStatusFilter(): number[] | null {
  if (filters.showErf && !filters.showWait) return [9, 10]
  if (!filters.showErf && filters.showWait) return [11, 12]
  return null
}

async function query(): Promise<void> {
  error.value = ''
  const hasLotOrMo = (filters.lotNoEnabled && filters.lotNo.trim()) || (filters.moNoEnabled && filters.moNo.trim())
  if (!filters.processDateEnabled && !hasLotOrMo) {
    error.value = '請勾選 ProcessDate，或提供 LotNo／MONo 條件。'
    rows.value = []
    queried.value = true
    return
  }
  if (!hasLotOrMo && filters.processDateEnabled && (!filters.startDateTime || !filters.endDateTime || new Date(filters.startDateTime).getTime() >= new Date(filters.endDateTime).getTime())) {
    error.value = 'ProcessDate 起迄不正確，開始時間必須早於結束時間。'
    rows.value = []
    queried.value = true
    return
  }
  if (!dev || loading.value) return

  loading.value = true
  try {
    await new Promise(resolve => window.setTimeout(resolve, 150))
    const productNos = splitValues(filters.productNo)
    const opNos = splitValues(filters.opNo)
    const userActiveIds = filters.userActiveIds.map(value => Number(value))
    const showTypeIds = showTypeStatusFilter()
    const start = filters.processDateEnabled ? new Date(filters.startDateTime).getTime() : Number.NEGATIVE_INFINITY
    const end = filters.processDateEnabled ? new Date(filters.endDateTime).getTime() : Number.POSITIVE_INFINITY
    rows.value = fixtureRows.filter(row => {
      const eventTime = new Date(row.eventTime).getTime()
      return eventTime >= start && eventTime <= end &&
        (!filters.lotNoEnabled || !filters.lotNo.trim() || row.lotNo.toUpperCase() === filters.lotNo.trim().toUpperCase()) &&
        (!filters.moNoEnabled || !filters.moNo.trim() || row.moNo.toUpperCase() === filters.moNo.trim().toUpperCase()) &&
        (!filters.productNoEnabled || productNos.length === 0 || productNos.includes(row.productNo)) &&
        (!filters.opNoEnabled || opNos.length === 0 || opNos.includes(row.opNo)) &&
        (!filters.userNoEnabled || !filters.userNo.trim() || row.userNo.toUpperCase() === filters.userNo.trim().toUpperCase()) &&
        (!filters.userActiveIdEnabled || userActiveIds.length === 0 || userActiveIds.includes(row.userActiveId)) &&
        (!showTypeIds || showTypeIds.includes(row.userActiveId))
    }).sort((left, right) => left.eventTime.localeCompare(right.eventTime) || left.sysActiveSequence - right.sysActiveSequence)
    selectedRow.value = null
    queried.value = true
    message.value = `查得 ${rows.value.length} 筆生產批歷程。`
  } finally { loading.value = false }
}

function selectRow(row: ReleaseRow): void { selectedRow.value = row }
</script>

<template>
  <section v-if="dev" class="release-page" aria-label="生產批下線後歷程總表" :aria-busy="loading">
    <header class="page-head"><div><p class="eyebrow">RPT · RT417 · WIP RELEASE HISTORY</p><h1>生產批下線後歷程總表</h1><p>依生產批、工單、產品、作業站及操作人查詢歷程。</p></div><button type="button" class="quiet" @click="emit('close')">Close (X)</button></header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p><p v-else class="feedback" role="status">{{ message }}</p>

    <fieldset class="query-panel"><legend>Query</legend><div class="filter-grid">
      <div class="filter-control"><label><input v-model="filters.lotNoEnabled" type="checkbox" :disabled="loading" @change="!filters.lotNoEnabled && (filters.lotNo = '')">Lot No</label><input v-model.trim="filters.lotNo" :disabled="loading" maxlength="60" placeholder="精確批號"></div>
      <div class="filter-control"><label><input v-model="filters.moNoEnabled" type="checkbox" :disabled="loading" @change="!filters.moNoEnabled && (filters.moNo = '')">MONo</label><input v-model.trim="filters.moNo" :disabled="loading" maxlength="60" placeholder="精確工單"></div>
      <div class="filter-control"><label><input v-model="filters.productNoEnabled" type="checkbox" :disabled="loading" @change="!filters.productNoEnabled && (filters.productNo = '')">ProductNo</label><div class="pick"><input v-model="filters.productNo" :disabled="loading" maxlength="300" placeholder="逗號分隔"><button type="button" :disabled="loading" aria-label="選擇 ProductNo" @click="openLookup('productNo')">...</button></div></div>
      <div class="filter-control"><label><input v-model="filters.opNoEnabled" type="checkbox" :disabled="loading" @change="!filters.opNoEnabled && (filters.opNo = '')">OP No</label><div class="pick"><input v-model="filters.opNo" readonly :disabled="loading" placeholder="多選"><button type="button" :disabled="loading" aria-label="選擇 OPNo" @click="openLookup('opNo')">...</button></div></div>
      <div class="filter-control"><label><input v-model="filters.userNoEnabled" type="checkbox" :disabled="loading" @change="!filters.userNoEnabled && (filters.userNo = '')">User No</label><input v-model.trim="filters.userNo" :disabled="loading" maxlength="40" placeholder="精確工號"></div>
      <div class="filter-control"><label><input v-model="filters.userActiveIdEnabled" type="checkbox" :disabled="loading" @change="!filters.userActiveIdEnabled && (filters.userActiveIds = [])">UserActiveID</label><div class="pick"><input :value="filters.userActiveIds.join(', ')" readonly :disabled="loading" placeholder="多選"><button type="button" :disabled="loading" aria-label="選擇 UserActiveID" @click="openLookup('userActiveId')">...</button></div></div>
      <div class="filter-control date-control"><label><input v-model="filters.processDateEnabled" type="checkbox" :disabled="loading">ProcessDate</label><div class="date-range"><input v-model="filters.startDateTime" type="datetime-local" :disabled="loading"><span>To</span><input v-model="filters.endDateTime" type="datetime-local" :disabled="loading"></div></div>
      <div class="filter-control show-control"><label><input v-model="filters.showErf" type="checkbox" :disabled="loading">Show ERF</label><label><input v-model="filters.showWait" type="checkbox" :disabled="loading">Show Wait</label><small>DEV 依來源 ShowType 分支模擬</small></div>
      <button type="button" class="primary" :disabled="loading" @click="query">{{ loading ? '查詢中…' : 'Query' }}</button>
    </div></fieldset>

    <section class="result-panel"><div class="result-head"><div><h2>Release History</h2><p>結果依 EventTime、SysActiveSequence 排序；合成資料不代表正式批次紀錄。</p></div><span>{{ resultCount }} 筆</span></div>
      <div class="table-scroll"><table><thead><tr><th>EventTime</th><th>UserActiveID</th><th>LotNo</th><th>CustomerLotNo</th><th>ProductNo</th><th>ProductName</th><th>OPNo</th><th>OPName</th><th>MONo</th><th>ProcessNo</th><th>ProcessVersion</th><th>UserNo</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.logGroupSerial" :class="{ selected: selectedRow?.logGroupSerial === row.logGroupSerial }" @click="selectRow(row)"><td class="mono">{{ new Date(row.eventTime).toLocaleString() }}</td><td>{{ activeName(row.userActiveId) }}</td><td class="mono">{{ row.lotNo }}</td><td>{{ row.customerLotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.productName }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.moNo }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.userNo }}</td></tr>
        <tr v-if="queried && !rows.length"><td colspan="12" class="empty">沒有符合條件的歷程資料</td></tr><tr v-if="!queried"><td colspan="12" class="empty">設定條件後按 Query</td></tr>
      </tbody></table></div>
    </section>

    <section class="detail-panel"><div class="detail-head"><div><h2>Lot Detail</h2><p v-if="selectedRow">{{ selectedRow.lotNo }} · {{ selectedRow.lotSerial }} · {{ detailKind }} 明細</p><p v-else>選取歷程列後載入假資料明細。</p></div><span v-if="selectedRow" class="badge">DType {{ selectedRow.dType }}</span></div>
      <div v-if="selectedRow" class="detail-grid"><section><h3>Component</h3><div class="table-scroll"><table><thead><tr><th>ComponentNo</th><th>Type</th><th>Qty</th><th>CPASS</th></tr></thead><tbody><tr v-for="item in componentRows" :key="item.componentNo"><td>{{ item.componentNo }}</td><td>{{ item.componentType }}</td><td class="num">{{ item.quantity }}</td><td>{{ item.cpass === 0 ? 'Pass' : 'Not Pass' }}</td></tr></tbody></table></div></section><section><h3>Bin</h3><div class="table-scroll"><table><thead><tr><th>LotSerial</th><th>BinNo</th><th>CPASS</th><th>Qty</th></tr></thead><tbody><tr v-for="item in binRows" :key="item.binNo"><td class="mono">{{ item.lotSerial }}</td><td>{{ item.binNo }}</td><td>{{ item.cpass === 0 ? 'Pass' : 'Not Pass' }}</td><td class="num">{{ item.quantity }}</td></tr></tbody></table></div></section><p v-if="selectedRow.dType >= 3 && selectedRow.dType <= 6" class="source-note span">原始 Form 有呼叫 Split／Merge Service，但結果 Grid 綁定程式碼為註解；DEV 僅展示合成批次關聯摘要。</p><p v-else-if="selectedRow.dType === 1 || selectedRow.dType === 2" class="source-note span">ERF／Wait 詳情由獨立資訊表單提供；DEV 僅呈現選取列與 Lot 明細示意。</p></div>
      <p v-else class="empty detail-empty">尚未選取歷程列</p>
    </section>

    <div v-if="lookupKind" class="overlay" @click.self="lookupKind = ''"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTitle"><header><div><h2>{{ lookupTitle }}</h2><p>可多選並回填條件。</p></div><button type="button" class="quiet" @click="lookupKind = ''">Close</button></header><input v-model.trim="lookupSearch" class="lookup-search" type="search" placeholder="搜尋代碼或名稱"><div class="lookup-list"><label v-for="option in lookupOptions" :key="option.value"><input type="checkbox" :checked="selected(lookupSelection, option.value)" @change="toggleLookup(option.value)"><span>{{ option.label }}</span></label></div><footer><span>已選 {{ lookupSelection.length }} 筆</span><div><button type="button" class="quiet" @click="lookupSelection = []">清除</button><button type="button" class="primary" @click="applyLookup">套用</button></div></footer></section></div>
    <footer class="source-note">DEV fixtures 為合成資料；UserActiveID 選項模擬來源 ValueList 0–19。</footer>
  </section>
  <section v-else class="pending" aria-label="生產批下線後歷程正式環境狀態"><p role="status">正式歷程查詢 API／schema 尚未核實，此頁不載入或修改正式資料。</p><button type="button" class="quiet" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.release-page,.pending{box-sizing:border-box;min-height:0;height:100%;padding:12px;background:#f5f7fa;color:#101828;font:inherit;overflow:auto}.page-head,.result-head,.detail-head,.lookup-dialog header,.lookup-dialog footer{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:0;font-size:22px}.page-head p,.result-head p,.detail-head p,.lookup-dialog header p{margin:3px 0;color:#667085;font-size:12px}.eyebrow{font-size:10px!important;letter-spacing:.08em;color:#475467!important}.quiet,.primary,.pick button{min-height:30px;padding:4px 11px;border:1px solid #98a2b3;border-radius:5px;background:#fff;color:#344054;font:inherit;cursor:pointer}.primary{border-color:#175cd3;background:#175cd3;color:#fff}.primary:disabled,.pick button:disabled{opacity:.55;cursor:default}.feedback{margin:8px 0;padding:7px 10px;border-radius:5px;background:#eff8ff;color:#175cd3;font-size:12px}.feedback.error{background:#fef3f2;color:#b42318}.query-panel{margin:0;padding:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.query-panel legend{padding:0 5px;font-weight:600}.filter-grid{display:grid;grid-template-columns:repeat(3,minmax(170px,1fr)) auto;align-items:end;gap:10px 12px}.filter-control{display:grid;gap:5px;color:#475467;font-size:11px;font-weight:600}.filter-control>label{display:flex;align-items:center;gap:5px}.filter-control>input,.pick input,.date-range input,.lookup-search{box-sizing:border-box;width:100%;height:30px;padding:4px 7px;border:1px solid #d0d5dd;border-radius:4px;background:#fff;color:#101828;font:inherit}.pick,.date-range{display:flex;align-items:center;gap:5px}.pick button{min-width:32px;padding:2px}.date-range span{font-size:10px}.date-control{grid-column:span 2}.show-control{display:flex;align-items:center;gap:12px;flex-wrap:wrap}.show-control small{width:100%;color:#667085;font-weight:400}.result-panel,.detail-panel{display:flex;flex-direction:column;min-height:220px;margin-top:10px;border:1px solid #d0d5dd;border-radius:7px;background:#fff}.result-panel{height:45vh;min-height:280px}.result-head,.detail-head{padding:9px 11px}.result-head h2,.detail-head h2{margin:0;font-size:14px}.result-head>span,.badge{color:#667085;font-size:11px}.badge{padding:3px 7px;border:1px solid #d0d5dd;border-radius:12px}.table-scroll{min-height:0;flex:1;overflow:auto;border-top:1px solid #eaecf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}th,td{padding:6px 8px;border-bottom:1px solid #eaecf0;text-align:left}th{position:sticky;top:0;z-index:1;background:#f9fafb;color:#475467}tbody tr{cursor:pointer}tbody tr:hover{background:#f9fafb}tbody tr.selected{background:#eff8ff}.mono{font-family:ui-monospace,SFMono-Regular,Consolas,monospace}.num{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;color:#98a2b3;padding:18px}.detail-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;padding:0 10px 10px}.detail-grid h3{margin:0;padding:6px 0;font-size:12px}.detail-grid .table-scroll{height:130px;border:1px solid #eaecf0;border-radius:4px}.source-note{display:block;padding:6px 2px;color:#667085;font-size:10px}.source-note.span{grid-column:1/-1;margin:0}.detail-empty{margin:0}.overlay{position:fixed;inset:0;z-index:2100;display:grid;place-items:center;padding:16px;background:#10182866}.lookup-dialog{display:flex;flex-direction:column;width:min(560px,96vw);max-height:78vh;padding:14px;border:1px solid #d0d5dd;border-radius:9px;background:#fff;box-shadow:0 18px 44px #10182840}.lookup-dialog header h2{margin:0;font-size:16px}.lookup-search{margin:10px 0}.lookup-list{min-height:0;overflow:auto}.lookup-list label{display:flex;align-items:center;gap:8px;padding:7px 5px;border-bottom:1px solid #eaecf0;font-size:12px}.lookup-list label:hover{background:#f9fafb}.lookup-dialog footer{padding-top:10px;border-top:1px solid #eaecf0;color:#667085;font-size:11px}.lookup-dialog footer>div{display:flex;gap:7px}.pending{display:flex;align-items:center;justify-content:space-between;gap:12px}.pending p{margin:0;color:#667085}.pending .quiet{flex-shrink:0}@media(max-width:1100px){.filter-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.result-panel{height:45vh}}@media(max-width:680px){.page-head{align-items:flex-start}.page-head h1{font-size:18px}.filter-grid,.detail-grid{grid-template-columns:1fr}.date-control{grid-column:auto}.show-control{align-items:flex-start;flex-direction:column;gap:5px}.show-control small{width:auto}.result-panel{height:55vh}.pending{align-items:flex-start;flex-direction:column}}
</style>

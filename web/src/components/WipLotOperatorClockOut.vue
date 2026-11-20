<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Employee = { userNo: string; userName: string; shiftNo: string }
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }
type LotBasis = { lotNo: string; status: number; psNo: string; opNo: string; multiOperatorMode: number; areaNo: string; equipmentNo: string }
type LotRow = LotBasis & { loginPlaceNo: string; userNo: string; userName: string; loginDate: string; checkFlag: boolean }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const stateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const lotStatuses = [
  { value: -1, label: 'All' }, { value: 0, label: 'Queue' }, { value: 1, label: 'Running' },
  { value: 2, label: 'Wait' }, { value: 3, label: 'Hold' }, { value: 4, label: 'Complete' },
  { value: 5, label: 'OS' }, { value: 6, label: 'Partial Complete' }, { value: 11, label: 'Inventory' },
  { value: 12, label: 'Scrap Inventory' }, { value: 99, label: 'Closed' },
]
const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY' },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT' },
]
const lots: LotBasis[] = [
  { lotNo: 'DEV-LOT-26001', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26002', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26003', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-AREA-20', multiOperatorMode: 1, areaNo: 'DEV-AREA-GRIND', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26004', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-EQP-30', multiOperatorMode: 2, areaNo: '', equipmentNo: 'DEV-EQP-WASH-01' },
  { lotNo: 'DEV-LOT-26005', status: 0, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26006', status: 1, psNo: 'DEV-PS-PACK', opNo: 'DEV-OP-PACK-40', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
]
const psOptions = ['DEV-PS-GLASS', 'DEV-PS-PACK']
const employeeNo = ref('')
const employeeName = ref('')
const shiftNo = ref('')
const lotStatus = ref(1)
const psNo = ref('')
const opNo = ref('')
const appliedFilters = ref({ lotStatus: 1, psNo: '', opNo: '', userNo: '' })
const queryComplete = ref(false)
const selectedLots = ref<string[]>([])
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const lookupOpen = ref<'ps' | 'op' | null>(null)
const lookupSearch = ref('')
const errorText = ref('')
const message = ref('LotStatus 預設 Running、Clock Status 固定為 Clocked In；按 Query 載入資料。')
const operations = [
  { opNo: 'DEV-OP-CUT-10', psNo: 'DEV-PS-GLASS' },
  { opNo: 'DEV-OP-AREA-20', psNo: 'DEV-PS-GLASS' },
  { opNo: 'DEV-OP-EQP-30', psNo: 'DEV-PS-GLASS' },
  { opNo: 'DEV-OP-PACK-40', psNo: 'DEV-PS-PACK' },
]
const filteredOperations = computed(() => operations.filter(item => (!psNo.value || item.psNo === psNo.value) && item.opNo.toLowerCase().includes(lookupSearch.value.trim().toLowerCase())))
const baseRows = computed<LotRow[]>(() => lots.flatMap(lot => {
  const place = lot.multiOperatorMode === 0 ? lot.opNo : lot.multiOperatorMode === 1 ? lot.areaNo : lot.equipmentNo
  const matches = operatorStates.value.filter(item => item.multiOperatorMode === lot.multiOperatorMode && item.loginPlaceNo === place)
  return matches.map(state => ({ ...lot, loginPlaceNo: state.loginPlaceNo, userNo: state.userNo, userName: state.userName, loginDate: state.loginDate, checkFlag: true }))
}))
const visibleRows = computed(() => {
  if (!queryComplete.value) return []
  return baseRows.value
    .filter(row => appliedFilters.value.lotStatus === -1 || row.status === appliedFilters.value.lotStatus)
    .filter(row => !appliedFilters.value.psNo || row.psNo === appliedFilters.value.psNo)
    .filter(row => !appliedFilters.value.opNo || row.opNo === appliedFilters.value.opNo)
    .filter(row => !appliedFilters.value.userNo || row.userNo === appliedFilters.value.userNo)
    .sort((a, b) => a.psNo.localeCompare(b.psNo) || a.opNo.localeCompare(b.opNo) || a.lotNo.localeCompare(b.lotNo) || a.userNo.localeCompare(b.userNo))
})
const selectedRows = computed(() => visibleRows.value.filter(row => selectedLots.value.includes(rowKey(row))))

function nowDateTime(): string {
  const date = new Date(); const p = (v: number) => String(v).padStart(2, '0')
  return `${date.getFullYear()}/${p(date.getMonth() + 1)}/${p(date.getDate())} ${p(date.getHours())}:${p(date.getMinutes())}:${p(date.getSeconds())}`
}
function rowKey(row: LotRow): string { return `${row.lotNo}|${row.userNo}|${row.multiOperatorMode}|${row.loginPlaceNo}|${row.loginDate}` }
function readMockStorage(): void {
  if (!dev.value) return
  try {
    const stateValue = window.localStorage.getItem(stateKey)
    const logValue = window.localStorage.getItem(logKey)
    operatorStates.value = stateValue ? JSON.parse(stateValue) as OperatorState[] : [
      { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', loginDate: nowDateTime(), workDate: nowDateTime().slice(0, 10).replace(/-/g, '/'), opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, loginPlaceNo: 'DEV-OP-CUT-10' },
      { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT', loginDate: nowDateTime(), workDate: nowDateTime().slice(0, 10).replace(/-/g, '/'), opNo: 'N/A', multiOperatorMode: 2, loginPlaceNo: 'DEV-EQP-WASH-01' },
    ]
    operatorLogs.value = logValue ? JSON.parse(logValue) as OperatorLog[] : []
  } catch { operatorStates.value = []; operatorLogs.value = [] }
}
function saveMockStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(stateKey, JSON.stringify(operatorStates.value))
  window.localStorage.setItem(logKey, JSON.stringify(operatorLogs.value))
}
function loadEmployeeFilter(): void {
  employeeNo.value = employeeNo.value.trim().toUpperCase()
  const employee = employees.find(item => item.userNo === employeeNo.value)
  employeeName.value = employee?.userName || ''
  shiftNo.value = employee?.shiftNo || ''
  errorText.value = employeeNo.value && !employee ? 'DEV 假員工不存在；查詢仍可使用編號篩選。' : ''
}
function queryLots(): void {
  loadEmployeeFilter()
  appliedFilters.value = { lotStatus: lotStatus.value, psNo: psNo.value, opNo: opNo.value, userNo: employeeNo.value }
  queryComplete.value = true
  selectedLots.value = visibleRows.value.map(rowKey)
  message.value = `查詢完成：${visibleRows.value.length} 筆已上工 Lot；依來源預設全選。`
}
function groupKey(row: LotRow): string { return `${row.userNo}|${row.loginPlaceNo}` }
function toggleLot(row: LotRow, checked: boolean): void {
  const key = rowKey(row)
  const group = visibleRows.value.filter(item => groupKey(item) === groupKey(row)).map(rowKey)
  selectedLots.value = checked ? [...new Set([...selectedLots.value, ...group])] : selectedLots.value.filter(item => !group.includes(item))
}
function selectAll(flag: boolean): void { selectedLots.value = flag ? visibleRows.value.map(rowKey) : [] }
function openLookup(kind: 'ps' | 'op'): void { lookupOpen.value = kind; lookupSearch.value = '' }
function chooseLookup(value: string): void {
  if (lookupOpen.value === 'ps') { psNo.value = value; opNo.value = '' }
  if (lookupOpen.value === 'op') opNo.value = value
  lookupOpen.value = null
}
function clockOut(): void {
  errorText.value = ''
  if (!dev.value) { errorText.value = '目前只提供 DEV 假資料互動。'; return }
  if (!selectedRows.value.length) { errorText.value = '請至少選取一筆已上工資料。'; return }
  const groups = new Map<string, LotRow>()
  for (const row of selectedRows.value) groups.set(`${row.userNo}|${row.loginPlaceNo}`, row)
  const logoutDate = nowDateTime()
  let completed = 0
  for (const row of groups.values()) {
    const state = operatorStates.value.find(item => item.userNo === row.userNo && item.multiOperatorMode === row.multiOperatorMode && item.loginPlaceNo === row.loginPlaceNo)
    if (!state) continue
    const start = new Date(state.loginDate.replace(/\//g, '-')).getTime()
    const end = new Date(logoutDate.replace(/\//g, '-')).getTime()
    operatorLogs.value.unshift({ ...state, logoutDate, workTimeMinutes: Math.max(0, Math.floor((end - start) / 60_000)) })
    operatorStates.value = operatorStates.value.filter(item => !(item.userNo === state.userNo && item.multiOperatorMode === state.multiOperatorMode && item.loginPlaceNo === state.loginPlaceNo))
    completed++
  }
  if (!completed) { errorText.value = '所選資料已沒有可下工的現況。'; return }
  saveMockStorage()
  message.value = `DEV 假批次下工完成：${completed} 個不同登入地點；沒有寫入正式系統。`
  employeeNo.value = ''; employeeName.value = ''; shiftNo.value = ''
  lotStatus.value = 1; psNo.value = ''; opNo.value = ''; appliedFilters.value = { lotStatus: 1, psNo: '', opNo: '', userNo: '' }
  selectedLots.value = []; queryComplete.value = false
}
function statusName(value: number): string { return lotStatuses.find(item => item.value === value)?.label || String(value) }

onMounted(readMockStorage)
</script>

<template>
  <section class="lot-clockout-page" aria-label="Lot Operator Clockout">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：員工篩選可輸入 DEV-EMP-01／DEV-EMP-02；上工狀態與 WP104／WP105 共用 localStorage，無需密碼驗證。</div>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>
    <fieldset class="mes-frame employee-frame"><legend>Employee Info (Optional Filter)</legend><div class="employee-fields">
      <label for="lotclockout-employee">EmployeeNo</label><input id="lotclockout-employee" v-model="employeeNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="loadEmployeeFilter" @keydown.enter.prevent="loadEmployeeFilter">
      <input class="mes-input readonly employee-name" :value="employeeName" aria-label="EmployeeName" readonly><label for="lotclockout-shift">ShiftNo</label><input id="lotclockout-shift" class="mes-input readonly" :value="shiftNo" readonly>
    </div></fieldset>
    <fieldset class="mes-frame filter-frame"><legend>Query Condition</legend><div class="filters">
      <label for="lotclockout-status">LotStatus</label><select id="lotclockout-status" v-model.number="lotStatus" class="mes-input editable" :disabled="!dev"><option v-for="status in lotStatuses" :key="status.value" :value="status.value">{{ status.label }}</option></select>
      <label>Clock Status</label><input class="mes-input readonly" value="Clocked In" readonly>
      <label for="lotclockout-ps">PSNo</label><div class="input-pair"><input id="lotclockout-ps" v-model="psNo" class="mes-input editable" :disabled="!dev"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('ps')">...</button></div>
      <label for="lotclockout-op">OPNo</label><div class="input-pair"><input id="lotclockout-op" v-model="opNo" class="mes-input editable" :disabled="!dev"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('op')">...</button></div>
      <button type="button" class="mes-button query-button" :disabled="!dev" @click="queryLots">Query</button>
    </div></fieldset>
    <div class="grid-frame"><div class="grid-toolbar"><button type="button" class="mes-button" :disabled="!dev || !visibleRows.length" @click="selectAll(true)">Select All</button><button type="button" class="mes-button" :disabled="!dev || !visibleRows.length" @click="selectAll(false)">UnSelect All</button></div>
      <div class="table-scroll"><table class="lot-grid"><thead><tr><th class="check-col">Clock Out</th><th>LotNo</th><th>Status</th><th>PSNo</th><th>OPNo</th><th>EquipmentNo</th><th>LoginPlaceNo</th><th>UserNo</th><th>UserName</th><th>LoginDate</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: selectedLots.includes(rowKey(row)) }" @click="toggleLot(row, !selectedLots.includes(rowKey(row)))"><td class="check-col"><input type="checkbox" :checked="selectedLots.includes(rowKey(row))" :disabled="!dev" :aria-label="`選取 ${row.lotNo} 下工`" @click.stop @change="toggleLot(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.lotNo }}</td><td>{{ statusName(row.status) }}</td><td>{{ row.psNo }}</td><td>{{ row.opNo }}</td><td>{{ row.equipmentNo || '—' }}</td><td>{{ row.loginPlaceNo }}</td><td>{{ row.userNo }}</td><td>{{ row.userName }}</td><td>{{ row.loginDate }}</td></tr>
        <tr v-if="!visibleRows.length"><td colspan="10" class="empty">{{ queryComplete ? '沒有符合條件的已上工 Lot。' : '按 Query 載入已上工 Lot。' }}</td></tr>
      </tbody></table></div><div class="statusbar">{{ message }}<span>Rows: {{ visibleRows.length }}</span><span>Selected: {{ selectedRows.length }}</span></div>
    </div>
    <div class="bottom-row"><button type="button" class="mes-button confirm-button" :disabled="!dev || !queryComplete" @click="clockOut">Clock Out</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    <div v-if="lookupOpen" class="overlay" @click.self="lookupOpen = null"><section class="dialog" role="dialog" aria-modal="true"><header><strong>{{ lookupOpen === 'ps' ? 'Production Segment' : 'OP Basis' }}</strong><button type="button" class="mes-button" @click="lookupOpen = null">Close</button></header><input v-model.trim="lookupSearch" class="mes-input" :placeholder="lookupOpen === 'ps' ? 'PSNo' : 'OPNo'"><button v-if="lookupOpen === 'ps'" type="button" class="lookup-row" @click="chooseLookup('')">（全部）</button><button v-for="option in (lookupOpen === 'ps' ? psOptions.filter(item => item.toLowerCase().includes(lookupSearch.toLowerCase())) : filteredOperations.map(item => item.opNo))" :key="option" type="button" class="lookup-row" @click="chooseLookup(option)">{{ option }}</button></section></div>
  </section>
</template>

<style scoped>
.lot-clockout-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.notice-error{padding:6px 9px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.mes-frame{min-width:0;margin:0;padding:7px 10px 10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.mes-frame legend{padding:0 5px}.employee-frame{min-height:70px}.employee-fields{display:grid;grid-template-columns:120px 190px 150px 190px 1fr;grid-template-rows:30px;align-items:center;gap:0 4px}.employee-fields label{text-align:right;padding-right:5px}.employee-name{grid-column:3}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;padding:2px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.filters{display:grid;grid-template-columns:100px minmax(120px,190px) 110px minmax(120px,190px) 90px minmax(120px,190px) 110px minmax(120px,190px) 1fr;grid-template-rows:32px 32px;align-items:center;gap:2px 4px}.filters label{text-align:right;padding-right:5px}.filters label:nth-of-type(1){grid-column:1}.filters select{grid-column:2}.filters label:nth-of-type(2){grid-column:3}.filters input.readonly{grid-column:4}.filters label:nth-of-type(3){grid-column:5}.filters .input-pair:nth-of-type(1){grid-column:6}.filters label:nth-of-type(4){grid-column:7}.filters .input-pair:nth-of-type(2){grid-column:8}.query-button{grid-column:9;grid-row:1 / span 2;justify-self:start;min-width:76px}.input-pair{display:flex;gap:3px;min-width:0}.input-pair input{width:100%}.lookup-button{width:28px;min-width:28px;padding:0}.mes-button{min-height:28px;padding:3px 12px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.grid-frame{display:flex;flex:1;min-height:190px;flex-direction:column;border:1px solid #bdbdbd;background:#fff;padding:6px}.grid-toolbar{display:flex;gap:5px;padding-bottom:5px}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.lot-grid{width:100%;border-collapse:collapse;white-space:nowrap}.lot-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #b7b7b7;background:#dfeaf2;text-align:left;font-weight:600}.lot-grid td{height:26px;padding:3px 7px;border:1px solid #c8c8c8;background:#fff3bd}.lot-grid tbody tr{cursor:pointer}.lot-grid tbody tr.selected td{background:#ffe7a0}.check-col{width:55px;text-align:center}.lot-grid input{accent-color:#d38d22}.empty{text-align:center!important;color:#777!important;background:#fff!important;height:48px!important}.statusbar{min-height:24px;display:flex;align-items:center;gap:18px;padding:2px 4px;color:#555}.bottom-row{display:flex;justify-content:flex-end;gap:7px;padding:2px 0}.confirm-button{min-width:88px;background:#f3a62a;color:#fff;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.overlay{position:fixed;inset:0;z-index:1800;display:grid;place-items:center;padding:20px;background:#0006}.dialog{width:min(520px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:9px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;align-items:center}.lookup-row{padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff2cc}@media(max-width:1000px){.filters{grid-template-columns:90px minmax(125px,1fr) 90px minmax(125px,1fr);grid-template-rows:30px 30px 30px}.filters label:nth-of-type(1){grid-column:1;grid-row:1}.filters select{grid-column:2;grid-row:1}.filters label:nth-of-type(2){grid-column:3;grid-row:1}.filters input.readonly{grid-column:4;grid-row:1}.filters label:nth-of-type(3){grid-column:1;grid-row:2}.filters .input-pair:nth-of-type(1){grid-column:2;grid-row:2}.filters label:nth-of-type(4){grid-column:3;grid-row:2}.filters .input-pair:nth-of-type(2){grid-column:4;grid-row:2}.query-button{grid-column:4;grid-row:3;justify-self:end}}@media(max-width:650px){.employee-fields{grid-template-columns:100px minmax(130px,1fr);grid-template-rows:30px 30px}.employee-name{grid-column:2;grid-row:2}.employee-fields label[for="lotclockout-shift"]{grid-column:1;grid-row:2}.employee-fields #lotclockout-shift{grid-column:2;grid-row:2}.filters{grid-template-columns:80px minmax(0,1fr);grid-template-rows:30px 30px 30px 30px 30px}.filters label:nth-of-type(n){grid-column:1}.filters label:nth-of-type(1),.filters select{grid-row:1}.filters label:nth-of-type(2),.filters input.readonly{grid-row:2}.filters label:nth-of-type(3),.filters .input-pair:nth-of-type(1){grid-row:3}.filters label:nth-of-type(4),.filters .input-pair:nth-of-type(2){grid-row:4}.filters select,.filters input.readonly,.filters .input-pair{grid-column:2}.query-button{grid-column:2;grid-row:5;justify-self:end}}
</style>

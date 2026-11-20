<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Employee = { userNo: string; userName: string; departmentNo: string; shiftNo: string; fromTime: string; toTime: string }
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }
type LotBasis = { lotNo: string; status: number; psNo: string; opNo: string; multiOperatorMode: number; areaNo: string; equipmentNo: string }
type LotRow = LotBasis & { checkFlag: boolean; loginPlaceNo: string; userNo: string; userName: string; loginDate: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const stateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const fakePassword = 'DEMO-1234'
const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', departmentNo: 'DEV-ENG', shiftNo: 'DEV-DAY', fromTime: '07:30', toTime: '19:30' },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', departmentNo: 'DEV-OPS', shiftNo: 'DEV-NIGHT', fromTime: '19:30', toTime: '07:30' },
]
const operations = [
  { opNo: 'DEV-OP-CUT-10', opName: 'DEV 切割 10', psNo: 'DEV-PS-GLASS', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { opNo: 'DEV-OP-AREA-20', opName: 'DEV 研磨 20', psNo: 'DEV-PS-GLASS', multiOperatorMode: 1, areaNo: 'DEV-AREA-GRIND', equipmentNo: '' },
  { opNo: 'DEV-OP-EQP-30', opName: 'DEV 清洗 30', psNo: 'DEV-PS-GLASS', multiOperatorMode: 2, areaNo: '', equipmentNo: 'DEV-EQP-WASH-01' },
  { opNo: 'DEV-OP-PACK-40', opName: 'DEV 包裝 40', psNo: 'DEV-PS-PACK', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
]
const lots: LotBasis[] = [
  { lotNo: 'DEV-LOT-26001', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26002', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26003', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-AREA-20', multiOperatorMode: 1, areaNo: 'DEV-AREA-GRIND', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26004', status: 1, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-EQP-30', multiOperatorMode: 2, areaNo: '', equipmentNo: 'DEV-EQP-WASH-01' },
  { lotNo: 'DEV-LOT-26005', status: 0, psNo: 'DEV-PS-GLASS', opNo: 'DEV-OP-CUT-10', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
  { lotNo: 'DEV-LOT-26006', status: 1, psNo: 'DEV-PS-PACK', opNo: 'DEV-OP-PACK-40', multiOperatorMode: 0, areaNo: '', equipmentNo: '' },
]
const lotStatuses = [
  { value: -1, label: 'All' }, { value: 0, label: 'Queue' }, { value: 1, label: 'Running' },
  { value: 2, label: 'Wait' }, { value: 3, label: 'Hold' }, { value: 4, label: 'Complete' },
  { value: 5, label: 'OS' }, { value: 6, label: 'Partial Complete' }, { value: 11, label: 'Inventory' },
  { value: 12, label: 'Scrap Inventory' }, { value: 99, label: 'Closed' },
]
const psOptions = ['DEV-PS-GLASS', 'DEV-PS-PACK']
const employeeNo = ref('')
const password = ref('')
const passwordRequired = ref(true) // DEV stand-in for WIPChkPassword=1.
const loadedEmployee = ref<Employee | null>(null)
const workDay = ref('')
const lotStatus = ref(1)
const clockStatus = ref(0)
const psNo = ref('')
const opNo = ref('')
const appliedFilters = ref({ lotStatus: 1, clockStatus: 0, psNo: '', opNo: '' })
const queryComplete = ref(false)
const selectedLots = ref<string[]>([])
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const lookupOpen = ref<'ps' | 'op' | null>(null)
const lookupSearch = ref('')
const errorText = ref('')
const message = ref('請先查詢批次作業資料；結果依 PSNo、OPNo、LotNo 排序。')
const filteredOperations = computed(() => operations.filter(item => (!psNo.value || item.psNo === psNo.value) && `${item.opNo} ${item.opName}`.toLowerCase().includes(lookupSearch.value.trim().toLowerCase())))
const baseRows = computed<LotRow[]>(() => lots.map(lot => {
  const place = lot.multiOperatorMode === 0 ? lot.opNo : lot.multiOperatorMode === 1 ? lot.areaNo : lot.equipmentNo
  const state = operatorStates.value.find(item => item.multiOperatorMode === lot.multiOperatorMode && item.loginPlaceNo === place)
  return { ...lot, checkFlag: selectedLots.value.includes(`${lot.lotNo}|${state?.userNo || ''}|${lot.multiOperatorMode}|${place}|${state?.loginDate || ''}`), loginPlaceNo: state?.loginPlaceNo || '', userNo: state?.userNo || '', userName: state?.userName || '', loginDate: state?.loginDate || '' }
}))
const visibleRows = computed(() => {
  if (!queryComplete.value) return []
  return baseRows.value
    .filter(row => appliedFilters.value.lotStatus === -1 || row.status === appliedFilters.value.lotStatus)
    .filter(row => appliedFilters.value.clockStatus === 0 || (appliedFilters.value.clockStatus === 1 ? Boolean(row.loginPlaceNo) : !row.loginPlaceNo))
    .filter(row => !appliedFilters.value.psNo || row.psNo === appliedFilters.value.psNo)
    .filter(row => !appliedFilters.value.opNo || row.opNo === appliedFilters.value.opNo)
    .sort((a, b) => a.psNo.localeCompare(b.psNo) || a.opNo.localeCompare(b.opNo) || a.lotNo.localeCompare(b.lotNo))
})
const selectedRows = computed(() => visibleRows.value.filter(row => selectedLots.value.includes(rowKey(row))))

function dateInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
function dateTime(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function rowKey(row: LotRow): string { return `${row.lotNo}|${row.userNo}|${row.multiOperatorMode}|${row.loginPlaceNo}|${row.loginDate}` }
function workDayFor(employee: Employee): string {
  const now = new Date()
  const nowMinutes = now.getHours() * 60 + now.getMinutes()
  const [fromHour, fromMinute] = employee.fromTime.split(':').map(Number)
  const [toHour, toMinute] = employee.toTime.split(':').map(Number)
  const from = fromHour * 60 + fromMinute
  const to = toHour * 60 + toMinute
  const belongsToday = from < to ? nowMinutes > from && nowMinutes < to : nowMinutes < to || nowMinutes > from
  if (!belongsToday) now.setDate(now.getDate() - 1)
  return dateInput(now)
}
function readMockStorage(): void {
  if (!dev.value) return
  try {
    const states = window.localStorage.getItem(stateKey)
    const logs = window.localStorage.getItem(logKey)
    operatorStates.value = states ? JSON.parse(states) as OperatorState[] : [
      { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', loginDate: dateTime(new Date(Date.now() - 45 * 60_000)), workDate: dateInput(new Date()), opNo: 'DEV-OP-OLD-05', multiOperatorMode: 0, loginPlaceNo: 'DEV-OP-OLD-05' },
    ]
    operatorLogs.value = logs ? JSON.parse(logs) as OperatorLog[] : []
  } catch { operatorStates.value = []; operatorLogs.value = [] }
}
function saveMockStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(stateKey, JSON.stringify(operatorStates.value))
  window.localStorage.setItem(logKey, JSON.stringify(operatorLogs.value))
}
function clearEmployee(): void { loadedEmployee.value = null; password.value = ''; workDay.value = '' }
function employeeEnter(): void {
  employeeNo.value = employeeNo.value.trim().toUpperCase()
  clearEmployee(); errorText.value = ''
  if (!employeeNo.value) { errorText.value = 'EmployeeNo 不可空白。'; return }
  if (passwordRequired.value) { document.getElementById('lotclockin-password')?.focus(); message.value = '請輸入 DEV 員工密碼後按 Enter。'; return }
  loadEmployee()
}
function passwordEnter(): void {
  errorText.value = ''
  if (password.value !== fakePassword) { errorText.value = 'DEV 假密碼不正確。'; clearEmployee(); return }
  loadEmployee()
}
function loadEmployee(): void {
  const employee = employees.find(item => item.userNo === employeeNo.value)
  if (!employee) { errorText.value = '找不到這筆 DEV 員工資料。'; clearEmployee(); return }
  loadedEmployee.value = employee
  workDay.value = workDayFor(employee)
  message.value = `${employee.userName}／${employee.shiftNo} 已驗證，可執行批次上工。`
  errorText.value = ''
}
function queryLots(): void {
  errorText.value = ''
  appliedFilters.value = { lotStatus: lotStatus.value, clockStatus: clockStatus.value, psNo: psNo.value, opNo: opNo.value }
  queryComplete.value = true
  selectedLots.value = visibleRows.value.map(rowKey)
  message.value = `查詢完成：${visibleRows.value.length} 筆；依來源預設全選。`
}
function selectAll(flag: boolean): void { selectedLots.value = flag ? visibleRows.value.map(rowKey) : [] }
function toggleLot(row: LotRow, checked: boolean): void {
  const key = rowKey(row)
  selectedLots.value = checked ? [...new Set([...selectedLots.value, key])] : selectedLots.value.filter(item => item !== key)
}
function openLookup(kind: 'ps' | 'op'): void { lookupSearch.value = ''; lookupOpen.value = kind }
function chooseLookup(value: string): void {
  if (lookupOpen.value === 'ps') { psNo.value = value; opNo.value = '' }
  if (lookupOpen.value === 'op') opNo.value = value
  lookupOpen.value = null
}
function modePlace(row: LotRow): { mode: number; op: string; place: string } {
  if (row.multiOperatorMode === 0) return { mode: 0, op: row.opNo, place: row.opNo }
  if (row.multiOperatorMode === 1) return { mode: 1, op: '', place: row.areaNo }
  return { mode: 2, op: '', place: row.equipmentNo }
}
function confirmClockIn(): void {
  errorText.value = ''
  if (!dev.value) { errorText.value = '目前只提供 DEV 假資料互動。'; return }
  if (!loadedEmployee.value) { errorText.value = '請先完成員工資料驗證。'; return }
  if (!workDay.value) { errorText.value = 'WorkDay 不可空白。'; return }
  if (!selectedRows.value.length) { errorText.value = '請至少選取一筆批次作業資料。'; return }
  const batch = new Map<string, { mode: number; op: string; place: string }>()
  for (const row of selectedRows.value) {
    const target = modePlace(row)
    const alreadyClockedIn = operatorStates.value.some(state => state.userNo === loadedEmployee.value?.userNo && state.multiOperatorMode === target.mode && state.loginPlaceNo === target.place)
    if (!alreadyClockedIn) batch.set(`${target.mode}|${target.place}`, target)
  }
  if (!batch.size) { errorText.value = '選取的作業站／地點都已有此員工的上工現況。'; return }
  const at = dateTime(new Date())
  const created = [...batch.values()].map(target => ({
    userNo: loadedEmployee.value!.userNo,
    userName: loadedEmployee.value!.userName,
    shiftNo: loadedEmployee.value!.shiftNo,
    loginDate: at,
    workDate: workDay.value,
    opNo: target.mode === 0 ? target.op : 'N/A',
    multiOperatorMode: target.mode,
    loginPlaceNo: target.place,
  } satisfies OperatorState))
  operatorStates.value = [...operatorStates.value, ...created]
  saveMockStorage()
  const user = loadedEmployee.value
  message.value = `DEV 假批次上工完成：${user.userNo}，${created.length} 個不同登入地點；沒有寫入正式系統。`
  clearEmployee(); employeeNo.value = ''
  lotStatus.value = 1; clockStatus.value = 0; psNo.value = ''; opNo.value = ''
  appliedFilters.value = { lotStatus: 1, clockStatus: 0, psNo: '', opNo: '' }
  selectedLots.value = []; queryComplete.value = false
}
function statusName(value: number): string { return lotStatuses.find(item => item.value === value)?.label || String(value) }

onMounted(readMockStorage)
</script>

<template>
  <section class="lot-clockin-page" aria-label="Lot Operator Clockin">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：EmployeeNo <b>DEV-EMP-01</b>／<b>DEV-EMP-02</b>，Password <b>DEMO-1234</b>；上工狀態與 WP104／WP105 共用 localStorage。</div>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>
    <fieldset class="mes-frame employee-frame"><legend>Employee Info</legend><div class="employee-fields">
      <label for="lotclockin-employee">EmployeeNo</label><input id="lotclockin-employee" v-model="employeeNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="employeeNo = employeeNo.toUpperCase()" @keydown.enter.prevent="employeeEnter">
      <input class="mes-input readonly employee-name" :value="loadedEmployee?.userName || ''" aria-label="EmployeeName" readonly>
      <label v-if="passwordRequired" for="lotclockin-password">Password</label><input v-if="passwordRequired" id="lotclockin-password" v-model="password" class="mes-input editable" type="password" autocomplete="new-password" :disabled="!dev" @keydown.enter.prevent="passwordEnter">
      <label for="lotclockin-shift">ShiftNo</label><input id="lotclockin-shift" class="mes-input readonly" :value="loadedEmployee?.shiftNo || ''" readonly>
      <label for="lotclockin-workday">WorkDay</label><input id="lotclockin-workday" v-model="workDay" class="mes-input editable" type="date" :disabled="!dev || !loadedEmployee">
    </div></fieldset>
    <fieldset class="mes-frame filter-frame"><legend>Query Condition</legend><div class="filters">
      <label for="lotclockin-lot-status">LotStatus</label><select id="lotclockin-lot-status" v-model.number="lotStatus" class="mes-input editable" :disabled="!dev"><option v-for="status in lotStatuses" :key="status.value" :value="status.value">{{ status.label }}</option></select>
      <label for="lotclockin-clock-status">Clock Status</label><select id="lotclockin-clock-status" v-model.number="clockStatus" class="mes-input editable" :disabled="!dev"><option :value="0">All</option><option :value="1">Clocked In</option><option :value="2">Clocked Out</option></select>
      <label for="lotclockin-ps">PSNo</label><div class="input-pair"><input id="lotclockin-ps" v-model="psNo" class="mes-input editable" :disabled="!dev"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('ps')">...</button></div>
      <label for="lotclockin-op">OPNo</label><div class="input-pair"><input id="lotclockin-op" v-model="opNo" class="mes-input editable" :disabled="!dev"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="openLookup('op')">...</button></div>
      <button type="button" class="mes-button query-button" :disabled="!dev" @click="queryLots">Query</button>
    </div></fieldset>
    <div class="grid-frame"><div class="grid-toolbar"><button type="button" class="mes-button" :disabled="!dev || !visibleRows.length" @click="selectAll(true)">Select All</button><button type="button" class="mes-button" :disabled="!dev || !visibleRows.length" @click="selectAll(false)">UnSelect All</button></div>
      <div class="table-scroll"><table class="lot-grid"><thead><tr><th class="check-col">Clock In</th><th>LotNo</th><th>Status</th><th>PSNo</th><th>OPNo</th><th>EquipmentNo</th><th>LoginPlaceNo</th><th>UserNo</th><th>UserName</th><th>LoginDate</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: selectedLots.includes(rowKey(row)) }" @click="toggleLot(row, !selectedLots.includes(rowKey(row)))"><td class="check-col"><input type="checkbox" :checked="selectedLots.includes(rowKey(row))" :disabled="!dev" :aria-label="`選取 ${row.lotNo}`" @click.stop @change="toggleLot(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.lotNo }}</td><td>{{ statusName(row.status) }}</td><td>{{ row.psNo }}</td><td>{{ row.opNo }}</td><td>{{ row.equipmentNo || '—' }}</td><td>{{ row.loginPlaceNo || '—' }}</td><td>{{ row.userNo || '—' }}</td><td>{{ row.userName || '—' }}</td><td>{{ row.loginDate || '—' }}</td></tr>
        <tr v-if="!visibleRows.length"><td colspan="10" class="empty">{{ queryComplete ? '沒有符合條件的批次資料。' : '按 Query 載入批次作業資料。' }}</td></tr>
      </tbody></table></div><div class="statusbar">{{ message }}<span>Rows: {{ visibleRows.length }}</span><span>Selected: {{ selectedRows.length }}</span></div>
    </div>
    <div class="bottom-row"><button type="button" class="mes-button confirm-button" :disabled="!dev || !loadedEmployee" @click="confirmClockIn">Clock In</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    <div v-if="lookupOpen" class="overlay" @click.self="lookupOpen = null"><section class="dialog" role="dialog" aria-modal="true" :aria-label="lookupOpen === 'ps' ? '選擇 Production Segment' : '選擇 OP'"><header><strong>{{ lookupOpen === 'ps' ? 'Production Segment' : 'OP Basis' }}</strong><button type="button" class="mes-button" @click="lookupOpen = null">Close</button></header><input v-model.trim="lookupSearch" class="mes-input" :placeholder="lookupOpen === 'ps' ? 'PSNo' : 'OPNo / OPName'"><button v-if="lookupOpen === 'ps'" type="button" class="lookup-row" @click="chooseLookup('')">（全部）</button><button v-for="option in (lookupOpen === 'ps' ? psOptions.filter(item => item.toLowerCase().includes(lookupSearch.toLowerCase())) : filteredOperations.map(item => item.opNo))" :key="option" type="button" class="lookup-row" @click="chooseLookup(option)">{{ option }}</button></section></div>
  </section>
</template>

<style scoped>
.lot-clockin-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.notice-error{padding:6px 9px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.mes-frame{min-width:0;margin:0;padding:7px 10px 10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.mes-frame legend{padding:0 5px}.employee-frame{min-height:112px}.employee-fields{display:grid;grid-template-columns:120px 190px 150px 1fr;grid-template-rows:30px 30px 30px;align-items:center;gap:0 4px}.employee-fields label,.filters label{text-align:right;padding-right:5px}.employee-name{grid-column:3;grid-row:1}.employee-fields label[for="lotclockin-password"]{grid-column:1;grid-row:2}.employee-fields #lotclockin-password{grid-column:2;grid-row:2}.employee-fields label[for="lotclockin-shift"]{grid-column:1;grid-row:3}.employee-fields #lotclockin-shift{grid-column:2;grid-row:3}.employee-fields label[for="lotclockin-workday"]{grid-column:3;grid-row:2}.employee-fields #lotclockin-workday{grid-column:4;grid-row:2;max-width:180px}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;padding:2px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.filters{display:grid;grid-template-columns:100px minmax(120px,190px) 110px minmax(120px,190px) 90px minmax(120px,190px) 110px minmax(120px,190px) 1fr;grid-template-rows:32px 32px;align-items:center;gap:2px 4px}.filters label:nth-of-type(1){grid-column:1}.filters select:nth-of-type(1){grid-column:2}.filters label:nth-of-type(2){grid-column:3}.filters select:nth-of-type(2){grid-column:4}.filters label:nth-of-type(3){grid-column:5}.filters .input-pair:nth-of-type(1){grid-column:6}.filters label:nth-of-type(4){grid-column:7}.filters .input-pair:nth-of-type(2){grid-column:8}.query-button{grid-column:9;grid-row:1 / span 2;justify-self:start;min-width:76px}.input-pair{display:flex;gap:3px;min-width:0}.input-pair input{width:100%}.lookup-button{width:28px;min-width:28px;padding:0}.mes-button{min-height:28px;padding:3px 12px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.grid-frame{display:flex;flex:1;min-height:190px;flex-direction:column;border:1px solid #bdbdbd;background:#fff;padding:6px}.grid-toolbar{display:flex;gap:5px;padding-bottom:5px}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.lot-grid{width:100%;border-collapse:collapse;white-space:nowrap}.lot-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #b7b7b7;background:#dfeaf2;text-align:left;font-weight:600}.lot-grid td{height:26px;padding:3px 7px;border:1px solid #c8c8c8;background:#fff3bd}.lot-grid tbody tr{cursor:pointer}.lot-grid tbody tr.selected td{background:#ffe7a0}.check-col{width:55px;text-align:center}.lot-grid input{accent-color:#d38d22}.empty{text-align:center!important;color:#777!important;background:#fff!important;height:48px!important}.statusbar{min-height:24px;display:flex;align-items:center;gap:18px;padding:2px 4px;color:#555}.bottom-row{display:flex;justify-content:flex-end;gap:7px;padding:2px 0}.confirm-button{min-width:88px;background:#f3a62a;color:#fff;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.overlay{position:fixed;inset:0;z-index:1800;display:grid;place-items:center;padding:20px;background:#0006}.dialog{width:min(520px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:9px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;align-items:center}.lookup-row{padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff2cc}@media(max-width:1000px){.filters{grid-template-columns:90px minmax(125px,1fr) 90px minmax(125px,1fr);grid-template-rows:30px 30px 30px}.filters label:nth-of-type(1){grid-column:1;grid-row:1}.filters select:nth-of-type(1){grid-column:2;grid-row:1}.filters label:nth-of-type(2){grid-column:3;grid-row:1}.filters select:nth-of-type(2){grid-column:4;grid-row:1}.filters label:nth-of-type(3){grid-column:1;grid-row:2}.filters .input-pair:nth-of-type(1){grid-column:2;grid-row:2}.filters label:nth-of-type(4){grid-column:3;grid-row:2}.filters .input-pair:nth-of-type(2){grid-column:4;grid-row:2}.query-button{grid-column:4;grid-row:3;justify-self:end}}@media(max-width:650px){.employee-fields{grid-template-columns:100px minmax(130px,1fr);grid-template-rows:30px 30px 30px 30px}.employee-name{grid-column:2;grid-row:2}.employee-fields label[for="lotclockin-password"]{grid-column:1;grid-row:2}.employee-fields #lotclockin-password{grid-column:2;grid-row:3}.employee-fields label[for="lotclockin-shift"]{grid-column:1;grid-row:4}.employee-fields #lotclockin-shift{grid-column:2;grid-row:4}.employee-fields label[for="lotclockin-workday"]{grid-column:1;grid-row:1}.employee-fields #lotclockin-workday{grid-column:2;grid-row:1}.filters{grid-template-columns:80px minmax(0,1fr);grid-template-rows:30px 30px 30px 30px 30px}.filters label:nth-of-type(n){grid-column:1}.filters label:nth-of-type(1),.filters select:nth-of-type(1){grid-row:1}.filters label:nth-of-type(2),.filters select:nth-of-type(2){grid-row:2}.filters label:nth-of-type(3),.filters .input-pair:nth-of-type(1){grid-row:3}.filters label:nth-of-type(4),.filters .input-pair:nth-of-type(2){grid-row:4}.filters select:nth-of-type(n),.filters .input-pair:nth-of-type(n){grid-column:2}.query-button{grid-column:2;grid-row:5;justify-self:end}}
</style>

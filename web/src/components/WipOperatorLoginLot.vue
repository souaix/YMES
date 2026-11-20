<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type LotBasis = { lotNo: string; baseLotNo: string; status: 0 | 1; opNo: string; opName: string; multiOperatorMode: number; wipDate: string; preOPEndTime: string }
type Employee = { userNo: string; userName: string; shiftNo: string }
type LotLogin = { lotNo: string; opNo: string; userNo: string; userName: string; checkInTime: string; checkOutTime: string; ciCreator: string; ciCreateDate: string; coCreator: string; coCreateDate: string; tempFlag: boolean; workDate: string }
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const historyKey = 'ymes-dev-mock:wip-lot-operator-login:history:v1'
const operatorStateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const operatorLogKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const hostUserNo = computed(() => props.currentUserNo || 'DEV-HOST-USER')
const lots: LotBasis[] = [
  { lotNo: 'DEV-LOT-MULTI-01', baseLotNo: 'DEV-LOT-MULTI-01', status: 1, opNo: 'DEV-OP-LOT-40', opName: 'DEV 多人加工 40', multiOperatorMode: 5, wipDate: '2026/11/19 07:30:00', preOPEndTime: '2026/11/19 07:30:00' },
  { lotNo: 'DEV-LOT-QUEUE-02', baseLotNo: 'DEV-LOT-QUEUE-02', status: 0, opNo: 'DEV-OP-LOT-40', opName: 'DEV 多人加工 40', multiOperatorMode: 5, wipDate: '2026/11/19 08:00:00', preOPEndTime: '2026/11/19 08:00:00' },
  { lotNo: 'DEV-LOT-NORMAL-03', baseLotNo: 'DEV-LOT-NORMAL-03', status: 1, opNo: 'DEV-OP-CUT-10', opName: 'DEV 切割 10', multiOperatorMode: 0, wipDate: '2026/11/19 09:00:00', preOPEndTime: '2026/11/19 09:00:00' },
]
const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY' },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT' },
]
const lotNo = ref('')
const lot = ref<LotBasis | null>(null)
const queryComplete = ref(false)
const checkInEmployeeNo = ref('')
const checkInEmployeeName = ref('')
const checkOutEmployeeNo = ref('')
const checkOutEmployeeName = ref('')
const checkInTimeOverride = ref(false)
const checkInTime = ref('')
const checkOutTimeOverride = ref(false)
const checkOutTime = ref('')
const createdBy = ref('')
const createdDate = ref('')
const selectedKey = ref('')
const records = ref<LotLogin[]>([])
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const errorText = ref('')
const message = ref('輸入 DEV LotNo 後按 Enter 載入；此頁只示範 CallType=MenuItem。')

const rows = computed(() => records.value.filter(row => row.lotNo === lot.value?.lotNo)
  .slice().sort((a, b) => (a.checkOutTime || '').localeCompare(b.checkOutTime || '') || a.checkInTime.localeCompare(b.checkInTime) || a.userNo.localeCompare(b.userNo)))
const selectedRecord = computed(() => rows.value.find(row => recordKey(row) === selectedKey.value) || null)
const canOperate = computed(() => dev.value && Boolean(lot.value) && lot.value?.multiOperatorMode === 5)

function nowDateTime(): string {
  const date = new Date(); const p = (v: number) => String(v).padStart(2, '0')
  return `${date.getFullYear()}/${p(date.getMonth() + 1)}/${p(date.getDate())} ${p(date.getHours())}:${p(date.getMinutes())}:${p(date.getSeconds())}`
}
function toDateTimeInput(value = new Date()): string {
  const p = (v: number) => String(v).padStart(2, '0')
  return `${value.getFullYear()}-${p(value.getMonth() + 1)}-${p(value.getDate())}T${p(value.getHours())}:${p(value.getMinutes())}:${p(value.getSeconds())}`
}
function fromInputTime(value: string): string {
  const date = new Date(value)
  const p = (v: number) => String(v).padStart(2, '0')
  return `${date.getFullYear()}/${p(date.getMonth() + 1)}/${p(date.getDate())} ${p(date.getHours())}:${p(date.getMinutes())}:${p(date.getSeconds())}`
}
function recordKey(row: LotLogin): string { return `${row.lotNo}|${row.userNo}|${row.checkInTime}|${row.ciCreator}` }
function readStorage(): void {
  if (!dev.value) return
  try {
    records.value = JSON.parse(window.localStorage.getItem(historyKey) || '[]') as LotLogin[]
    operatorStates.value = JSON.parse(window.localStorage.getItem(operatorStateKey) || '[]') as OperatorState[]
    operatorLogs.value = JSON.parse(window.localStorage.getItem(operatorLogKey) || '[]') as OperatorLog[]
  } catch { records.value = []; operatorStates.value = []; operatorLogs.value = [] }
  createdBy.value = hostUserNo.value
  createdDate.value = nowDateTime()
  checkInTime.value = toDateTimeInput()
  checkOutTime.value = toDateTimeInput()
}
function saveStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(historyKey, JSON.stringify(records.value))
  window.localStorage.setItem(operatorStateKey, JSON.stringify(operatorStates.value))
  window.localStorage.setItem(operatorLogKey, JSON.stringify(operatorLogs.value))
}
function loadLot(): void {
  errorText.value = ''
  lotNo.value = lotNo.value.trim().toUpperCase()
  lot.value = lots.find(item => item.lotNo === lotNo.value && (item.status === 0 || item.status === 1)) || null
  queryComplete.value = Boolean(lot.value)
  selectedKey.value = ''
  checkInEmployeeNo.value = ''; checkInEmployeeName.value = ''
  checkOutEmployeeNo.value = ''; checkOutEmployeeName.value = ''
  if (!lot.value) { errorText.value = 'DEV 找不到此 Lot，或 Lot 狀態不在 Queue/Running。'; message.value = '查無 Lot。'; return }
  createdDate.value = nowDateTime()
  if (lot.value.multiOperatorMode !== 5) {
    errorText.value = '此 OP 的 MultiOperatorMode 不是 Lot（5）；來源不允許使用本功能。'
    message.value = '批次 Lot 人員登錄已停用。'
    return
  }
  const firstOpen = rows.value.find(row => !row.checkOutTime)
  const first = firstOpen || rows.value[0]
  if (first) {
    selectedKey.value = recordKey(first)
    checkOutEmployeeNo.value = first.userNo
    checkOutEmployeeName.value = first.userName
  }
  message.value = `已載入 ${lot.value.lotNo}／${lot.value.opNo}，${rows.value.length} 筆上工／歷程。`
}
function loadEmployee(kind: 'in' | 'out'): void {
  errorText.value = ''
  const userNo = (kind === 'in' ? checkInEmployeeNo.value : checkOutEmployeeNo.value).trim().toUpperCase()
  const employee = employees.find(item => item.userNo === userNo)
  if (kind === 'in') { checkInEmployeeNo.value = userNo; checkInEmployeeName.value = employee?.userName || '' }
  else { checkOutEmployeeNo.value = userNo; checkOutEmployeeName.value = employee?.userName || '' }
  if (userNo && !employee) errorText.value = '找不到 DEV 員工或有效使用者資料。'
  if (kind === 'out' && employee) {
    const active = rows.value.find(row => row.userNo === userNo && !row.checkOutTime)
    if (active) selectedKey.value = recordKey(active)
  }
}
function chooseRecord(row: LotLogin): void {
  selectedKey.value = recordKey(row)
  checkOutEmployeeNo.value = row.userNo
  checkOutEmployeeName.value = row.userName
}
function validateCheckInTime(): string | null {
  const now = new Date()
  const entered = checkInTimeOverride.value ? new Date(checkInTime.value) : now
  if (Number.isNaN(entered.getTime())) return 'Check In Time 格式無效。'
  const priorEnd = new Date((lot.value?.preOPEndTime || lot.value?.wipDate || '').replace(/\//g, '-'))
  if (entered < priorEnd) return 'Check In Time 不可早於前站結束時間或 WIPDate。'
  if (entered > now) return 'Check In Time 不可晚於目前時間。'
  const userRows = rows.value.filter(row => row.userNo === checkInEmployeeNo.value)
  if (userRows.some(row => row.checkOutTime && entered >= new Date(row.checkInTime.replace(/\//g, '-')) && entered <= new Date(row.checkOutTime.replace(/\//g, '-')))) return 'Check In Time 與同員工既有歷程重疊。'
  if (userRows.some(row => !row.checkOutTime)) return '此員工在此 Lot 已上工，請先下工。'
  return null
}
function signIn(): void {
  errorText.value = ''
  if (!canOperate.value || !lot.value) { errorText.value = '請先載入 MultiOperatorMode=5 的 Lot。'; return }
  const userNo = checkInEmployeeNo.value.trim().toUpperCase()
  const employee = employees.find(item => item.userNo === userNo)
  if (!employee || !checkInEmployeeName.value) { errorText.value = '請輸入有效 Employee No 並按 Enter。'; return }
  const validation = validateCheckInTime()
  if (validation) { errorText.value = validation; return }
  const serverTime = nowDateTime()
  const manualWorkDate = checkInTimeOverride.value ? fromInputTime(checkInTime.value).slice(0, 10) : serverTime.slice(0, 10)
  const creator = hostUserNo.value
  const row: LotLogin = { lotNo: lot.value.lotNo, opNo: lot.value.opNo, userNo, userName: employee.userName, checkInTime: serverTime, checkOutTime: '', ciCreator: creator, ciCreateDate: serverTime, coCreator: '', coCreateDate: '', tempFlag: false, workDate: manualWorkDate }
  const state: OperatorState = { userNo, userName: employee.userName, shiftNo: employee.shiftNo, loginDate: serverTime, workDate: manualWorkDate, opNo: lot.value.opNo, multiOperatorMode: 5, loginPlaceNo: lot.value.lotNo }
  records.value = [row, ...records.value]
  operatorStates.value = [...operatorStates.value.filter(item => !(item.userNo === userNo && item.multiOperatorMode === 5 && item.loginPlaceNo === lot.value?.lotNo)), state]
  saveStorage()
  checkInEmployeeNo.value = ''; checkInEmployeeName.value = ''
  checkInTimeOverride.value = false
  checkInTime.value = toDateTimeInput()
  loadLot()
  message.value = `DEV 假登入成功：${userNo} → ${lot.value?.lotNo}。登入時間由假 AP Server 產生。`
}
function validateCheckOutTime(row: LotLogin): string | null {
  const now = new Date()
  const entered = checkOutTimeOverride.value ? new Date(checkOutTime.value) : now
  const checkIn = new Date(row.checkInTime.replace(/\//g, '-'))
  if (Number.isNaN(entered.getTime())) return 'Check Out Time 格式無效。'
  if (entered <= checkIn) return 'Check Out Time 必須晚於 Check In Time。'
  if (entered > now) return 'Check Out Time 不可晚於目前時間。'
  const otherRows = rows.value.filter(item => item.userNo === row.userNo && recordKey(item) !== recordKey(row))
  if (otherRows.some(item => item.checkOutTime && entered >= new Date(item.checkInTime.replace(/\//g, '-')) && entered <= new Date(item.checkOutTime.replace(/\//g, '-')))) return 'Check Out Time 與同員工其他歷程重疊。'
  const nextCheckIn = otherRows.map(item => new Date(item.checkInTime.replace(/\//g, '-'))).filter(date => date > checkIn).sort((a, b) => a.getTime() - b.getTime())[0]
  if (nextCheckIn && entered > nextCheckIn) return 'Check Out Time 不可包含後續登入記錄。'
  return null
}
function signOut(): void {
  errorText.value = ''
  const row = selectedRecord.value
  if (!canOperate.value || !lot.value || !row) { errorText.value = '請選取一筆登入歷程。'; return }
  const employee = employees.find(item => item.userNo === checkOutEmployeeNo.value.trim().toUpperCase())
  if (!employee || !checkOutEmployeeName.value) { errorText.value = '請輸入有效 Employee No 並按 Enter。'; return }
  if (row.tempFlag) { errorText.value = '暫存資料不可下工。'; return }
  if (row.userNo !== employee.userNo) { errorText.value = '所選登入列與 Employee No 不一致。'; return }
  if (row.checkOutTime) { errorText.value = '選取資料已經下工。'; return }
  const validation = validateCheckOutTime(row)
  if (validation) { errorText.value = validation; return }
  const logoutDate = checkOutTimeOverride.value ? fromInputTime(checkOutTime.value) : nowDateTime()
  const state = operatorStates.value.find(item => item.userNo === row.userNo && item.multiOperatorMode === 5 && item.loginPlaceNo === row.lotNo)
  if (state) {
    const minutes = Math.max(0, Math.floor((new Date(logoutDate.replace(/\//g, '-')).getTime() - new Date(state.loginDate.replace(/\//g, '-')).getTime()) / 60_000))
    operatorLogs.value.unshift({ ...state, logoutDate, workTimeMinutes: minutes })
    operatorStates.value = operatorStates.value.filter(item => !(item.userNo === state.userNo && item.multiOperatorMode === 5 && item.loginPlaceNo === state.loginPlaceNo))
  }
  records.value = records.value.map(item => recordKey(item) === recordKey(row) ? { ...item, checkOutTime: logoutDate, coCreator: hostUserNo.value, coCreateDate: nowDateTime() } : item)
  saveStorage()
  checkOutTimeOverride.value = false
  checkOutTime.value = toDateTimeInput()
  loadLot()
  message.value = `DEV 假登出成功：${row.userNo} ← ${row.lotNo}。`
}

onMounted(readStorage)
</script>

<template>
  <section class="operator-login-lot-page" aria-label="Operator Login Lot">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：Lot `DEV-LOT-MULTI-01`、員工 `DEV-EMP-01`／`DEV-EMP-02`；登入歷程與 WP104/WP105 OperatorState/Log 共用假資料。</div>
    <h1 class="page-title">OPERATOR LOGIN_LOT</h1>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>
    <fieldset class="mes-frame lot-frame"><legend>Lot Information</legend><div class="lot-fields">
      <label for="op-login-lot">LotNo</label><input id="op-login-lot" v-model="lotNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="lotNo = lotNo.toUpperCase()" @keydown.enter.prevent="loadLot">
      <label for="op-login-op">OPNo</label><input id="op-login-op" class="mes-input readonly" :value="lot?.opNo || ''" readonly>
      <label for="op-login-op-name">OPName</label><input id="op-login-op-name" class="mes-input readonly" :value="lot?.opName || ''" readonly>
    </div></fieldset>
    <div class="operation-panels">
      <fieldset class="mes-frame operation-frame" :disabled="!canOperate"><legend>User Check In Operation</legend><div class="operation-fields">
        <label for="op-login-ci-employee">Employee No</label><input id="op-login-ci-employee" v-model="checkInEmployeeNo" class="mes-input editable" autocomplete="off" :disabled="!canOperate" @keydown.enter.prevent="loadEmployee('in')">
        <label>Employee Name</label><input class="mes-input readonly" :value="checkInEmployeeName" readonly>
        <label for="op-login-ci-time">Check In Time</label><div class="date-field"><input v-model="checkInTimeOverride" type="checkbox" :disabled="!canOperate"><input id="op-login-ci-time" v-model="checkInTime" class="mes-input editable" type="datetime-local" step="1" :disabled="!canOperate || !checkInTimeOverride"></div>
        <button type="button" class="mes-button confirm-button" :disabled="!canOperate" @click="signIn">Sign In</button>
      </div></fieldset>
      <fieldset class="mes-frame operation-frame" :disabled="!canOperate"><legend>User Check Out Operation</legend><div class="operation-fields">
        <label for="op-login-co-employee">Employee No</label><input id="op-login-co-employee" v-model="checkOutEmployeeNo" class="mes-input editable" autocomplete="off" :disabled="!canOperate" @keydown.enter.prevent="loadEmployee('out')">
        <label>Employee Name</label><input class="mes-input readonly" :value="checkOutEmployeeName" readonly>
        <label for="op-login-co-time">Check Out Time</label><div class="date-field"><input v-model="checkOutTimeOverride" type="checkbox" :disabled="!canOperate"><input id="op-login-co-time" v-model="checkOutTime" class="mes-input editable" type="datetime-local" step="1" :disabled="!canOperate || !checkOutTimeOverride"></div>
        <button type="button" class="mes-button confirm-button" :disabled="!canOperate || !selectedRecord || Boolean(selectedRecord.checkOutTime)" @click="signOut">Sign Out</button>
      </div></fieldset>
    </div>
    <div class="grid-frame"><div class="table-scroll"><table class="history-grid"><thead><tr><th>UserNo</th><th>UserName</th><th>CheckInTime</th><th>CheckOutTime</th><th>CICreator</th><th>CICreateDate</th><th>COCreator</th><th>COCreateDate</th><th>TempFlag</th></tr></thead><tbody>
      <tr v-for="row in rows" :key="recordKey(row)" :class="{ selected: selectedKey === recordKey(row) }" @click="chooseRecord(row)"><td>{{ row.userNo }}</td><td>{{ row.userName }}</td><td>{{ row.checkInTime || '—' }}</td><td>{{ row.checkOutTime || '—' }}</td><td>{{ row.ciCreator || '—' }}</td><td>{{ row.ciCreateDate || '—' }}</td><td>{{ row.coCreator || '—' }}</td><td>{{ row.coCreateDate || '—' }}</td><td>{{ row.tempFlag ? 'Yes' : 'No' }}</td></tr>
      <tr v-if="!rows.length"><td colspan="9" class="empty">{{ queryComplete ? '此 Lot 尚無人員登入歷程。' : '輸入 LotNo 後按 Enter 載入。' }}</td></tr>
    </tbody></table></div><div class="statusbar">{{ message }}<span>Rows: {{ rows.length }}</span></div></div>
    <div class="audit-row"><label>Creator</label><input class="mes-input readonly" :value="createdBy" readonly><label>Create Date</label><input class="mes-input readonly" :value="createdDate" readonly><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
  </section>
</template>

<style scoped>
.operator-login-lot-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.page-title{margin:0;padding:0 2px;font-size:18px;font-weight:600;color:#333}.notice-error{padding:6px 9px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.mes-frame{min-width:0;margin:0;padding:7px 10px 10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.mes-frame legend{padding:0 5px}.lot-frame{flex:0 0 auto}.lot-fields{display:grid;grid-template-columns:75px minmax(150px,1fr) 75px minmax(150px,1fr) 75px minmax(150px,1fr);grid-auto-rows:30px;align-items:center;gap:4px}.lot-fields label,.operation-fields>label{justify-self:end;padding-right:4px}.mes-input{height:25px;min-width:0;border:1px solid #8c8c8c;padding:3px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.operation-panels{display:grid;grid-template-columns:1fr 1fr;gap:8px;flex:0 0 auto}.operation-frame{min-height:168px}.operation-fields{display:grid;grid-template-columns:96px minmax(110px,1fr);grid-template-rows:30px 30px 34px 34px;align-items:center;gap:4px}.operation-fields>button{grid-column:2;justify-self:end;min-width:80px}.date-field{display:flex;min-width:0;align-items:center;gap:5px}.date-field input[type=checkbox]{accent-color:#d38d22}.date-field input[type=datetime-local]{width:100%}.mes-button{min-height:28px;padding:3px 12px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.confirm-button{background:#f3a62a;color:#fff;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.grid-frame{display:flex;flex:1;min-height:130px;flex-direction:column;border:1px solid #bdbdbd;background:#fff;padding:6px}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.history-grid{width:100%;border-collapse:collapse;white-space:nowrap}.history-grid th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #b7b7b7;background:#dfeaf2;text-align:left;font-weight:600}.history-grid td{height:26px;padding:3px 7px;border:1px solid #c8c8c8;background:#fff3bd}.history-grid tbody tr{cursor:pointer}.history-grid tbody tr.selected td{background:#ffe7a0}.empty{text-align:center!important;color:#777!important;background:#fff!important;height:48px!important}.statusbar{min-height:24px;display:flex;align-items:center;gap:18px;padding:2px 4px;color:#555}.statusbar span{margin-left:auto}.audit-row{display:grid;grid-template-columns:70px minmax(120px,170px) 90px minmax(150px,200px) 1fr;align-items:center;gap:6px}.audit-row label{text-align:right;padding-right:4px}.audit-row button{justify-self:end;min-width:80px}@media(max-width:850px){.lot-fields{grid-template-columns:72px minmax(0,1fr) 72px minmax(0,1fr)}.lot-fields label:nth-of-type(3){grid-column:1}.lot-fields input:nth-of-type(3){grid-column:2/5}.operation-panels{grid-template-columns:1fr}.operation-frame{min-height:155px}.audit-row{grid-template-columns:70px minmax(100px,1fr) 90px minmax(120px,1fr);}.audit-row button{grid-column:4}.grid-frame{min-height:230px}}@media(max-width:560px){.lot-fields{grid-template-columns:72px minmax(0,1fr);grid-auto-rows:30px}.lot-fields label:nth-of-type(n){grid-column:1}.lot-fields input:nth-of-type(n){grid-column:2}.audit-row{grid-template-columns:70px minmax(0,1fr);}.audit-row label:nth-of-type(2){grid-column:1}.audit-row input:nth-of-type(2){grid-column:2}.audit-row button{grid-column:2}}
</style>

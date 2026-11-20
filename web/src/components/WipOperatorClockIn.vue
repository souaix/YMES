<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Employee = { userNo: string; userName: string; departmentNo: string; shiftNo: string; fromTime: string; toTime: string; level: number }
type Operation = { opNo: string; opName: string; psNo: string; multiOperatorMode: number; modeName: string }
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const stateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const fakePassword = 'DEMO-1234'

const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', departmentNo: 'DEV-ENG', shiftNo: 'DEV-DAY', fromTime: '07:30', toTime: '19:30', level: 1 },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', departmentNo: 'DEV-OPS', shiftNo: 'DEV-NIGHT', fromTime: '19:30', toTime: '07:30', level: 1 },
]
const operations: Operation[] = [
  { opNo: 'DEV-OP-CUT-10', opName: 'DEV 切割 10', psNo: 'DEV-PS-GLASS', multiOperatorMode: 0, modeName: '作業站' },
  { opNo: 'DEV-OP-AREA-20', opName: 'DEV 研磨 20', psNo: 'DEV-PS-GLASS', multiOperatorMode: 1, modeName: '區域' },
  { opNo: 'DEV-OP-EQP-30', opName: 'DEV 清洗 30', psNo: 'DEV-PS-GLASS', multiOperatorMode: 2, modeName: '機台' },
  { opNo: 'DEV-OP-LOT-40', opName: 'DEV Lot 多人加工 40', psNo: 'DEV-PS-GLASS', multiOperatorMode: 5, modeName: 'Lot' },
]
const placesByOperation: Record<string, string[]> = {
  'DEV-OP-AREA-20': ['DEV-AREA-GRIND', 'DEV-AREA-POLISH'],
  'DEV-OP-EQP-30': ['DEV-EQP-WASH-01', 'DEV-EQP-WASH-02'],
}

const employeeNo = ref('')
const password = ref('')
const workDayControl = ref<HTMLInputElement | null>(null)
const passwordRequired = ref(true) // DEV mock for system parameter WIPChkPassword=1
const loadedEmployee = ref<Employee | null>(null)
const shiftNo = ref('')
const loginOpNo = ref('')
const loginPlaceNo = ref('')
const workDay = ref('')
const loginDate = ref('')
const operationPickerOpen = ref(false)
const placePickerOpen = ref(false)
const clockOutPromptOpen = ref(false)
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const message = ref('請刷入 DEV 員工編號後按 Enter。')
const errorText = ref('')
const operationSearch = ref('')
const filteredOperations = computed(() => operations.filter(item => `${item.opNo} ${item.opName}`.toLowerCase().includes(operationSearch.value.trim().toLowerCase())))
const currentOperation = computed(() => operations.find(item => item.opNo === loginOpNo.value.trim().toUpperCase()) || null)
const availablePlaces = computed(() => currentOperation.value ? placesByOperation[currentOperation.value.opNo] || [] : [])
const placeLabel = computed(() => currentOperation.value?.multiOperatorMode === 1 ? 'AreaNo' : 'EquipmentNo')
const needsPlace = computed(() => currentOperation.value?.multiOperatorMode === 1 || currentOperation.value?.multiOperatorMode === 2)
const otherActiveStates = computed(() => loadedEmployee.value ? operatorStates.value.filter(item => item.userNo === loadedEmployee.value?.userNo && item.loginPlaceNo !== targetLoginPlace()) : [])

function nowLocal(): Date { return new Date() }
function formatDateTime(date: Date): string {
  const p = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${p(date.getMonth() + 1)}/${p(date.getDate())} ${p(date.getHours())}:${p(date.getMinutes())}:${p(date.getSeconds())}`
}
function dateInput(date: Date): string {
  const p = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${p(date.getMonth() + 1)}-${p(date.getDate())}`
}
function calculateWorkDay(employee: Employee, now = nowLocal()): string {
  const currentMinutes = now.getHours() * 60 + now.getMinutes()
  const [fromHour, fromMinute] = employee.fromTime.split(':').map(Number)
  const [toHour, toMinute] = employee.toTime.split(':').map(Number)
  const from = fromHour * 60 + fromMinute
  const to = toHour * 60 + toMinute
  const todayIsWorkDay = from <= to ? currentMinutes > from && currentMinutes < to : currentMinutes >= from
  const workDate = new Date(now)
  if (!todayIsWorkDay) workDate.setDate(workDate.getDate() - 1)
  return dateInput(workDate)
}
function readMockStorage(): void {
  if (!dev.value) return
  try {
    const stateValue = window.localStorage.getItem(stateKey)
    const logValue = window.localStorage.getItem(logKey)
    operatorStates.value = stateValue ? JSON.parse(stateValue) as OperatorState[] : [
      { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', loginDate: formatDateTime(new Date(Date.now() - 45 * 60_000)), workDate: dateInput(new Date()), opNo: 'DEV-OP-OLD-05', multiOperatorMode: 0, loginPlaceNo: 'DEV-OP-OLD-05' },
    ]
    operatorLogs.value = logValue ? JSON.parse(logValue) as OperatorLog[] : []
  } catch {
    operatorStates.value = []
    operatorLogs.value = []
  }
}
function saveMockStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(stateKey, JSON.stringify(operatorStates.value))
  window.localStorage.setItem(logKey, JSON.stringify(operatorLogs.value))
}
function clearEmployee(): void {
  loadedEmployee.value = null
  shiftNo.value = ''
  password.value = ''
  loginDate.value = ''
}
function employeeEnter(): void {
  employeeNo.value = employeeNo.value.trim().toUpperCase()
  errorText.value = ''
  clearEmployee()
  if (!employeeNo.value) { errorText.value = 'EmployeeNo 不可空白。'; return }
  if (passwordRequired.value) {
    document.getElementById('wp104-password')?.focus()
    message.value = '請輸入 DEV 員工密碼後按 Enter。'
    return
  }
  loadEmployee()
}
function focusWorkDay(): void { workDayControl.value?.focus() }
function passwordEnter(): void {
  errorText.value = ''
  if (!employeeNo.value.trim()) { errorText.value = '請先輸入 EmployeeNo。'; return }
  if (password.value !== fakePassword) { errorText.value = 'DEV 假密碼不正確。'; clearEmployee(); return }
  loadEmployee()
}
function loadEmployee(): void {
  const employee = employees.find(item => item.userNo === employeeNo.value.trim().toUpperCase())
  if (!employee) { errorText.value = '找不到這筆 DEV 員工資料。'; clearEmployee(); return }
  if (!employee.departmentNo || !employee.shiftNo) { errorText.value = '員工尚未設定 DepartmentNo 或 ShiftNo。'; clearEmployee(); return }
  if (!employee.fromTime || !employee.toTime) { errorText.value = '班別未設定起訖時間。'; clearEmployee(); return }
  loadedEmployee.value = employee
  shiftNo.value = employee.shiftNo
  workDay.value = calculateWorkDay(employee)
  loginDate.value = formatDateTime(nowLocal()) // WinForms reads AP Server time; DEV uses local mock time.
  message.value = `${employee.userName}／${employee.shiftNo} 已載入，可選擇登入作業站。`
  errorText.value = ''
}
function onOperationInput(): void {
  loginOpNo.value = loginOpNo.value.toUpperCase()
  loginPlaceNo.value = ''
  errorText.value = ''
  const operation = currentOperation.value
  if (!operation) return
  if (operation.multiOperatorMode === 0) loginPlaceNo.value = operation.opNo
  if (operation.multiOperatorMode === 5) errorText.value = '此 OP 為 MultiOperatorMode=5，請改用「使用者多人加工登錄_生產批」。'
  if (![0, 1, 2, 5].includes(operation.multiOperatorMode)) errorText.value = `不支援的 MultiOperatorMode：${operation.multiOperatorMode}`
}
function chooseOperation(operation: Operation): void {
  loginOpNo.value = operation.opNo
  operationPickerOpen.value = false
  onOperationInput()
}
function openPlacePicker(): void {
  if (!currentOperation.value) { errorText.value = '請先輸入有效的 LoginOP。'; return }
  if (!needsPlace.value) return
  placePickerOpen.value = true
}
function choosePlace(place: string): void { loginPlaceNo.value = place; placePickerOpen.value = false; errorText.value = '' }
function targetLoginPlace(): string { return currentOperation.value?.multiOperatorMode === 0 ? currentOperation.value.opNo : loginPlaceNo.value }
function logOutState(state: OperatorState, logoutDate: string): void {
  const loginAt = new Date(state.loginDate.replace(/\//g, '-')).getTime()
  const logoutAt = new Date(logoutDate.replace(/\//g, '-')).getTime()
  operatorLogs.value.unshift({ ...state, logoutDate, workTimeMinutes: Math.max(0, Math.floor((logoutAt - loginAt) / 60_000)) })
}
function validateClockIn(): boolean {
  errorText.value = ''
  if (!dev.value) { errorText.value = '目前只提供 DEV 假資料互動。'; return false }
  if (!loadedEmployee.value) { errorText.value = '請先完成員工資料驗證。'; return false }
  if (!currentOperation.value) { errorText.value = 'LoginOP 不可空白或不存在。'; return false }
  if (currentOperation.value.multiOperatorMode === 5) { errorText.value = 'MultiOperatorMode=5 請使用批次 Lot 上工功能。'; return false }
  if (![0, 1, 2].includes(currentOperation.value.multiOperatorMode)) { errorText.value = '此多人加工模式未定義。'; return false }
  if (needsPlace.value && !loginPlaceNo.value) { errorText.value = `${placeLabel.value} 不可空白。`; return false }
  if (!workDay.value) { errorText.value = 'WorkDay 不可空白。'; return false }
  const chosenDate = new Date(workDay.value)
  const dateNow = new Date()
  chosenDate.setHours(0, 0, 0, 0)
  dateNow.setHours(0, 0, 0, 0)
  if (chosenDate.getTime() > dateNow.getTime()) { errorText.value = 'WorkDay 不可晚於目前工作日。'; return false }
  return true
}
function confirmClockIn(): void {
  if (!validateClockIn() || !loadedEmployee.value || !currentOperation.value) return
  const existing = operatorStates.value.filter(item => item.userNo === loadedEmployee.value?.userNo)
  if (existing.some(item => item.loginPlaceNo !== targetLoginPlace())) {
    clockOutPromptOpen.value = true // DEV system parameter CLOCKIN_AUTOCLOCKOUT=0; default button is No.
    return
  }
  finishClockIn(false)
}
function finishClockIn(autoClockOut: boolean): void {
  if (!loadedEmployee.value || !currentOperation.value) return
  const at = formatDateTime(nowLocal())
  if (autoClockOut) {
    const closing = operatorStates.value.filter(item => item.userNo === loadedEmployee.value?.userNo && item.loginPlaceNo !== targetLoginPlace())
    for (const state of closing) logOutState(state, at)
    operatorStates.value = operatorStates.value.filter(item => !closing.includes(item))
  }
  const newState: OperatorState = {
    userNo: loadedEmployee.value.userNo,
    userName: loadedEmployee.value.userName,
    shiftNo: loadedEmployee.value.shiftNo,
    loginDate: at,
    workDate: workDay.value,
    opNo: currentOperation.value.opNo,
    multiOperatorMode: currentOperation.value.multiOperatorMode,
    loginPlaceNo: targetLoginPlace(),
  }
  operatorStates.value = [...operatorStates.value.filter(item => !(item.userNo === newState.userNo && item.multiOperatorMode === newState.multiOperatorMode && item.loginPlaceNo === newState.loginPlaceNo)), newState]
  saveMockStorage()
  message.value = `DEV 假上工完成：${newState.userNo} → ${newState.loginPlaceNo}，WorkDay ${newState.workDate}。沒有寫入正式系統。`
  clockOutPromptOpen.value = false
  clearEmployee()
  employeeNo.value = ''
}
function clearForm(): void {
  clearEmployee()
  employeeNo.value = ''
  loginOpNo.value = ''
  loginPlaceNo.value = ''
  workDay.value = ''
  errorText.value = ''
  message.value = '欄位已清空。'
}
function cancelClockOutPrompt(): void {
  clockOutPromptOpen.value = false
  finishClockIn(false)
}
function acceptClockOutPrompt(): void { finishClockIn(true) }

onMounted(readMockStorage)
</script>

<template>
  <section class="wp104-page" aria-label="使用者登入作業（上工）">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：EmployeeNo <b>DEV-EMP-01</b>／<b>DEV-EMP-02</b>，Password <b>DEMO-1234</b>；不會寫入正式系統。</div>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>
    <div class="form-groups">
      <fieldset class="mes-frame login-frame"><legend>Login</legend>
        <div class="login-fields">
          <label for="wp104-employee">EmployeeNo</label>
          <input id="wp104-employee" v-model="employeeNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="employeeNo = employeeNo.toUpperCase()" @keydown.enter.prevent="employeeEnter">
          <input class="mes-input readonly employee-name" :value="loadedEmployee?.userName || ''" aria-label="EmployeeName" readonly>
          <label v-if="passwordRequired" for="wp104-password">Password</label><input v-if="passwordRequired" id="wp104-password" v-model="password" class="mes-input editable" type="password" autocomplete="new-password" :disabled="!dev" @keydown.enter.prevent="passwordEnter">
          <label for="wp104-shift">ShiftNo</label><input id="wp104-shift" class="mes-input readonly" :value="shiftNo" readonly>
        </div>
      </fieldset>

      <fieldset class="mes-frame login-data-frame"><legend>Login Data</legend>
        <div class="data-fields">
          <label for="wp104-op">LoginOP</label><div class="input-pair"><input id="wp104-op" v-model="loginOpNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @input="onOperationInput" @blur="loginOpNo = loginOpNo.toUpperCase()" @keydown.enter.prevent="focusWorkDay"><button type="button" class="mes-button lookup-button" :disabled="!dev" @click="operationSearch = ''; operationPickerOpen = true">...</button></div>
          <label for="wp104-place">LoginPlace</label><div class="input-pair"><input id="wp104-place" v-model="loginPlaceNo" class="mes-input" :class="{ editable: needsPlace, readonly: !needsPlace }" :disabled="!dev || !needsPlace" readonly><button type="button" class="mes-button lookup-button" :disabled="!dev || !needsPlace" @click="openPlacePicker">...</button></div>
          <label for="wp104-workday">WorkDay</label><input id="wp104-workday" ref="workDayControl" v-model="workDay" type="date" class="mes-input editable" :disabled="!dev">
          <label for="wp104-logindate">LoginDate</label><input id="wp104-logindate" class="mes-input readonly" :value="loginDate" readonly>
        </div>
      </fieldset>
    </div>
    <div class="bottom-row"><span class="status-message" role="status">{{ message }}</span><span class="spacer"></span><button type="button" class="mes-button confirm-button" :disabled="!dev" @click="confirmClockIn">Confirm</button><button type="button" class="mes-button" :disabled="!dev" @click="clearForm">Clear</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>

    <div v-if="operationPickerOpen" class="overlay" @click.self="operationPickerOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇 LoginOP"><header><strong>OP Basis</strong><button type="button" class="mes-button" @click="operationPickerOpen = false">Close</button></header><input v-model.trim="operationSearch" class="mes-input" placeholder="OPNo / OPName"><button v-for="operation in filteredOperations" :key="operation.opNo" type="button" class="lookup-row" @click="chooseOperation(operation)"><span><b>{{ operation.opNo }}</b>　{{ operation.opName }}</span><small>{{ operation.psNo }} · MultiOperatorMode {{ operation.multiOperatorMode }}（{{ operation.modeName }}）</small></button></section></div>
    <div v-if="placePickerOpen" class="overlay" @click.self="placePickerOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇 LoginPlace"><header><strong>{{ placeLabel }}</strong><button type="button" class="mes-button" @click="placePickerOpen = false">Close</button></header><button v-for="place in availablePlaces" :key="place" type="button" class="lookup-row" @click="choosePlace(place)">{{ place }}</button><div v-if="availablePlaces.length === 0" class="empty">此作業站沒有可選地點。</div></section></div>
    <div v-if="clockOutPromptOpen" class="overlay"><section class="dialog prompt-dialog" role="dialog" aria-modal="true" aria-label="其他作業站上工確認"><strong>此員工已在其他地點上工</strong><div class="state-list"><div v-for="state in otherActiveStates" :key="`${state.multiOperatorMode}-${state.loginPlaceNo}`">{{ state.multiOperatorMode === 0 ? '作業站' : state.multiOperatorMode === 1 ? '區域' : '機台' }}：{{ state.loginPlaceNo }}</div></div><p>是否先自動下工其他地點，再執行本次上工？</p><footer><button type="button" class="mes-button" @click="acceptClockOutPrompt">Yes</button><button type="button" class="mes-button" @click="cancelClockOutPrompt">No</button></footer><small>WinForms 預設選取 No；本 DEV 視窗也不連正式交易。</small></section></div>
  </section>
</template>

<style scoped>
.wp104-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:8px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.notice-error{padding:7px 10px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.form-groups{display:flex;flex-direction:column;gap:6px}.mes-frame{min-width:0;margin:0;padding:8px 10px 12px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.mes-frame legend{padding:0 5px}.login-frame{min-height:125px}.login-fields{display:grid;grid-template-columns:120px 150px 150px 1fr;grid-template-rows:30px 30px 30px;align-items:center;gap:0 4px}.login-fields label,.data-fields label{text-align:right;padding-right:5px}.employee-name{grid-column:3;grid-row:1}.login-fields label[for="wp104-password"]{grid-column:1;grid-row:2}.login-fields #wp104-password{grid-column:2;grid-row:2}.login-fields label[for="wp104-shift"]{grid-column:1;grid-row:3}.login-fields #wp104-shift{grid-column:2;grid-row:3}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;padding:2px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.mes-input:disabled{color:#777}.login-data-frame{min-height:180px}.data-fields{display:grid;grid-template-columns:120px 180px 30px 1fr;grid-template-rows:30px 30px 30px 30px;align-items:center;gap:0 4px}.input-pair{display:flex;gap:3px}.input-pair input{flex:1}.lookup-button{width:27px;min-width:27px;padding:0 4px}.mes-button{min-height:29px;padding:3px 14px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6;border-color:#8e8e8e}.mes-button:disabled{color:#999;cursor:default}.bottom-row{display:flex;align-items:center;gap:7px;margin-top:auto;padding:6px 0 0}.status-message{min-width:0;overflow:hidden;text-overflow:ellipsis;color:#555}.spacer{flex:1}.confirm-button{min-width:88px;background:#f3a62a;color:white;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.overlay{position:fixed;inset:0;z-index:1800;display:grid;place-items:center;padding:20px;background:#0006}.dialog{width:min(560px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:10px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog>header,.dialog>footer{display:flex;align-items:center;justify-content:space-between;gap:8px}.lookup-row{width:100%;display:flex;flex-direction:column;align-items:flex-start;gap:4px;padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff2cc}.lookup-row small,.dialog small{color:#666}.empty{padding:12px;text-align:center;color:#777}.prompt-dialog{width:min(470px,94vw)}.prompt-dialog p{margin:0}.state-list{display:flex;flex-direction:column;gap:4px;padding:8px 10px;background:#f7f5ee;border:1px solid #ddd}@media(max-width:650px){.login-fields{grid-template-columns:100px minmax(130px,1fr);grid-template-rows:30px 30px 30px 30px}.employee-name{grid-column:2;grid-row:2}.login-fields label[for="wp104-password"]{grid-column:1;grid-row:2}.login-fields #wp104-password{grid-column:2;grid-row:3}.login-fields label[for="wp104-shift"]{grid-column:1;grid-row:4}.login-fields #wp104-shift{grid-column:2;grid-row:4}.data-fields{grid-template-columns:100px minmax(140px,1fr) 30px;grid-template-rows:30px 30px 30px 30px}.bottom-row{gap:4px}.bottom-row .mes-button{padding:3px 8px}}
</style>

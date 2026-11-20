<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

type Employee = { userNo: string; userName: string; shiftNo: string; fromTime: string; toTime: string }
type LoginState = { userNo: string; userName: string; loginSerial: string; loginType: 0 | 1; loginArea: string; workDay: string; shiftNo: string; loginDate: string }
type LoginLog = LoginState & { logoutDate: string; workingHours: number }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const stateKey = 'ymes-dev-mock:wip-operator-login:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-login:log:v1'
const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', fromTime: '07:30', toTime: '19:30' },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT', fromTime: '19:30', toTime: '07:30' },
]
const operations = [
  { opNo: 'DEV-OP-CUT-10', opName: 'DEV 切割 10' },
  { opNo: 'DEV-OP-AREA-20', opName: 'DEV 研磨 20' },
]
const areas = [
  { areaNo: 'DEV-AREA-GRIND', areaName: 'DEV 研磨區' },
  { areaNo: 'DEV-AREA-PACK', areaName: 'DEV 包裝區' },
]

const employeeNo = ref('')
const employee = ref<Employee | null>(null)
const shiftNo = ref('')
const loginDate = ref('')
const workDay = ref('')
const loginType = ref<0 | 1>(0)
const loginArea = ref('')
const loginSerial = ref('')
const loggedIn = ref(false)
const currentTime = ref('')
const picker = ref<'op' | 'area' | null>(null)
const pickerSearch = ref('')
const errorText = ref('')
const message = ref('DEV 員工編號後按 Enter，載入登入狀態。')
const states = ref<LoginState[]>([])
const logs = ref<LoginLog[]>([])
let clockTimer: ReturnType<typeof setInterval> | undefined

const title = computed(() => loggedIn.value ? 'Operator Logout' : 'Operator Login')
const options = computed(() => loginType.value === 0
  ? operations.filter(item => `${item.opNo} ${item.opName}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase()))
    .map(item => ({ value: item.opNo, label: `${item.opNo}　${item.opName}` }))
  : areas.filter(item => `${item.areaNo} ${item.areaName}`.toLowerCase().includes(pickerSearch.value.trim().toLowerCase()))
    .map(item => ({ value: item.areaNo, label: `${item.areaNo}　${item.areaName}` })))

function nowDate(): string {
  const date = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
function nowDateTime(): string {
  const date = new Date()
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function setCurrentTime(): void { currentTime.value = nowDateTime() }
function workDayFor(user: Employee): string {
  const now = new Date()
  const minutes = now.getHours() * 60 + now.getMinutes()
  const [fh, fm] = user.fromTime.split(':').map(Number)
  const [th, tm] = user.toTime.split(':').map(Number)
  const from = fh * 60 + fm
  const to = th * 60 + tm
  // Keep the legacy form's overnight-shift default calculation.
  if (from >= to && (minutes > from || minutes < to)) now.setDate(now.getDate() - 1)
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}
function readStorage(): void {
  if (!dev.value) return
  try {
    const stateValue = window.localStorage.getItem(stateKey)
    const logValue = window.localStorage.getItem(logKey)
    const sampleState: LoginState = {
      userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', loginSerial: 'DEV-LOGIN-0002', loginType: 0,
      loginArea: 'DEV-OP-CUT-10', workDay: nowDate(), shiftNo: 'DEV-NIGHT', loginDate: nowDateTime(),
    }
    states.value = stateValue ? JSON.parse(stateValue) as LoginState[] : [sampleState]
    logs.value = logValue ? JSON.parse(logValue) as LoginLog[] : states.value.map(state => ({ ...state, logoutDate: '', workingHours: 0 }))
  } catch { states.value = []; logs.value = [] }
}
function saveStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(stateKey, JSON.stringify(states.value))
  window.localStorage.setItem(logKey, JSON.stringify(logs.value))
}
function clearLoaded(): void {
  employee.value = null; shiftNo.value = ''; loginDate.value = ''; workDay.value = ''
  loginArea.value = ''; loginSerial.value = ''; loginType.value = 0; loggedIn.value = false
}
function loadEmployee(): void {
  errorText.value = ''
  employeeNo.value = employeeNo.value.trim().toUpperCase()
  if (!employeeNo.value) { errorText.value = 'Employee No 不可空白。'; clearLoaded(); return }
  const found = employees.find(item => item.userNo === employeeNo.value)
  if (!found) { errorText.value = '找不到這筆 DEV 員工或有效班別。'; clearLoaded(); return }
  employee.value = found
  shiftNo.value = found.shiftNo
  const active = states.value.find(item => item.userNo === found.userNo)
  if (active) {
    loggedIn.value = true
    loginSerial.value = active.loginSerial
    loginType.value = active.loginType
    loginArea.value = active.loginArea
    workDay.value = active.workDay
    loginDate.value = active.loginDate
    message.value = `${found.userName} 已登入；載入原登入地點，Confirm 將執行登出。`
  } else {
    loggedIn.value = false
    loginSerial.value = ''
    loginType.value = 0
    loginArea.value = ''
    workDay.value = workDayFor(found)
    loginDate.value = nowDateTime()
    message.value = `${found.userName} 尚未登入；選擇 OP 或 Area 後可登入。`
  }
}
function selectLoginType(value: 0 | 1): void {
  if (loggedIn.value) return
  loginType.value = value
  loginArea.value = ''
}
function openPicker(): void { pickerSearch.value = ''; picker.value = loginType.value === 0 ? 'op' : 'area' }
function chooseLocation(value: string): void { loginArea.value = value; picker.value = null }
function confirm(): void {
  errorText.value = ''
  if (!dev.value) { errorText.value = '此頁目前只有 DEV 假資料。'; return }
  if (!employee.value) { errorText.value = '請先輸入 Employee No 並按 Enter。'; return }
  if (loggedIn.value) {
    const state = states.value.find(item => item.userNo === employee.value?.userNo && item.loginSerial === loginSerial.value)
    const log = logs.value.find(item => item.userNo === employee.value?.userNo && item.loginSerial === loginSerial.value)
    if (!state || !log) { errorText.value = '找不到目前登入狀態或對應登入歷程。'; return }
    const logoutDate = nowDateTime()
    const minutes = Math.max(0, Math.floor((new Date(logoutDate.replace(/\//g, '-')).getTime() - new Date(log.loginDate.replace(/\//g, '-')).getTime()) / 60_000))
    logs.value = logs.value.map(item => item.loginSerial === loginSerial.value ? { ...item, logoutDate, workingHours: Number((minutes / 60).toFixed(2)) } : item)
    states.value = states.value.filter(item => item.loginSerial !== loginSerial.value)
    saveStorage()
    message.value = `DEV 假登出成功：${employee.value.userNo}，工時 ${Number((minutes / 60).toFixed(2))} 小時。`
  } else {
    if (!loginArea.value) { errorText.value = `請選擇 ${loginType.value === 0 ? 'OP' : 'Area'}。`; return }
    if (!workDay.value) { errorText.value = 'Work Day 不可空白。'; return }
    const chosen = new Date(`${workDay.value}T00:00:00`)
    const defaultDay = new Date(`${workDayFor(employee.value)}T00:00:00`)
    const days = Math.round((chosen.getTime() - defaultDay.getTime()) / 86_400_000)
    if (Math.abs(days) > 1) { errorText.value = `Work Day 只能設定在班別預設日 ${defaultDay.toLocaleDateString()} 的前後一天。`; return }
    if (states.value.some(item => item.userNo === employee.value?.userNo)) { errorText.value = '此員工已有登入狀態，請重新載入。'; return }
    const serial = `DEV-LOGIN-${Date.now()}`
    const state: LoginState = { userNo: employee.value.userNo, userName: employee.value.userName, loginSerial: serial, loginType: loginType.value, loginArea: loginArea.value, workDay: workDay.value.replace(/-/g, '/'), shiftNo: employee.value.shiftNo, loginDate: nowDateTime() }
    states.value = [state, ...states.value]
    logs.value = [{ ...state, logoutDate: '', workingHours: 0 }, ...logs.value]
    saveStorage()
    message.value = `DEV 假登入成功：${employee.value.userNo} → ${loginArea.value}。`
  }
  employeeNo.value = ''
  clearLoaded()
}

onMounted(() => { readStorage(); setCurrentTime(); clockTimer = setInterval(setCurrentTime, 1000) })
onUnmounted(() => { if (clockTimer) clearInterval(clockTimer) })
</script>

<template>
  <section class="operator-login-page" aria-label="Operator Login and Logout">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：DEV-EMP-01 可登入；DEV-EMP-02 預設已有登入狀態可登出。此頁與 Operator Clockin/Clockout 是不同的舊系統登入資料。</div>
    <h1 class="page-title">{{ title }}</h1>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>
    <fieldset class="mes-frame login-frame"><legend>Employee Login</legend><div class="employee-grid">
      <label for="operator-login-employee">Employee No</label><input id="operator-login-employee" v-model="employeeNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="employeeNo = employeeNo.toUpperCase()" @keydown.enter.prevent="loadEmployee">
      <input class="mes-input readonly employee-name" :value="employee?.userName || ''" aria-label="Employee Name" readonly>
      <label for="operator-login-shift">Shift No</label><input id="operator-login-shift" class="mes-input readonly" :value="shiftNo" readonly>
      <label for="operator-login-current-time">CurrentTime</label><input id="operator-login-current-time" class="mes-input readonly" :value="currentTime" readonly>
    </div></fieldset>
    <fieldset class="mes-frame location-frame" :disabled="!dev || !employee || loggedIn"><legend>Login Location</legend><div class="location-grid">
      <label><input type="radio" :checked="loginType === 0" :disabled="!dev || !employee || loggedIn" @change="selectLoginType(0)"> Login OP</label>
      <input class="mes-input editable" :value="loginType === 0 ? loginArea : ''" readonly>
      <button type="button" class="mes-button lookup-button" :disabled="!dev || !employee || loggedIn || loginType !== 0" @click="openPicker">...</button>
      <label><input type="radio" :checked="loginType === 1" :disabled="!dev || !employee || loggedIn" @change="selectLoginType(1)"> Login Area</label>
      <input class="mes-input editable" :value="loginType === 1 ? loginArea : ''" readonly>
      <button type="button" class="mes-button lookup-button" :disabled="!dev || !employee || loggedIn || loginType !== 1" @click="openPicker">...</button>
      <label for="operator-login-workday">Work Day</label><input id="operator-login-workday" v-model="workDay" class="mes-input editable" type="date" :disabled="!dev || !employee || loggedIn">
      <label for="operator-login-logindate">Login Date</label><input id="operator-login-logindate" class="mes-input readonly" :value="loginDate" readonly>
    </div></fieldset>
    <div class="message-bar" role="status">{{ message }}</div>
    <div class="bottom-row"><button type="button" class="mes-button confirm-button" :disabled="!dev || !employee" @click="confirm">Confirm</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    <div v-if="picker" class="overlay" @click.self="picker = null"><section class="dialog" role="dialog" aria-modal="true"><header><strong>{{ picker === 'op' ? 'Operation Basis' : 'Area Basis' }}</strong><button type="button" class="mes-button" @click="picker = null">Close</button></header><input v-model.trim="pickerSearch" class="mes-input" :placeholder="picker === 'op' ? 'OP No' : 'Area No'"><button v-for="item in options" :key="item.value" type="button" class="lookup-row" @click="chooseLocation(item.value)">{{ item.label }}</button><div v-if="!options.length" class="empty">No matching data.</div></section></div>
  </section>
</template>

<style scoped>
.operator-login-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.page-title{margin:0;padding:0 2px;font-size:18px;font-weight:600;color:#333}.notice-error{padding:6px 9px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.mes-frame{min-width:0;margin:0;padding:10px 12px 14px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.mes-frame legend{padding:0 5px}.login-frame{max-width:620px}.employee-grid{display:grid;grid-template-columns:100px minmax(145px,1fr) minmax(110px,auto) minmax(145px,1fr);grid-auto-rows:32px;align-items:center;gap:4px 7px}.employee-grid label,.location-grid>label{justify-self:end;padding-right:4px}.employee-name{grid-column:3}.location-frame{max-width:620px}.location-grid{display:grid;grid-template-columns:110px minmax(160px,1fr) 32px;grid-auto-rows:34px;align-items:center;gap:4px 6px}.location-grid label:has(input[type=radio]){justify-self:start}.location-grid input[type=radio]{accent-color:#d38d22}.location-grid label[for=operator-login-workday]{grid-column:1}.location-grid #operator-login-workday{grid-column:2/4;max-width:210px}.location-grid label[for=operator-login-logindate]{grid-column:1}.location-grid #operator-login-logindate{grid-column:2/4;max-width:240px}.mes-input{height:25px;min-width:0;border:1px solid #8c8c8c;padding:3px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.mes-button{min-height:29px;padding:3px 12px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.lookup-button{width:30px;min-width:30px;padding:0}.message-bar{margin-top:auto;min-height:26px;padding:6px;border-top:1px solid #ccc;color:#555}.bottom-row{display:flex;justify-content:flex-end;gap:7px;padding:2px 0}.confirm-button{min-width:104px;background:#f3a62a;color:#fff;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.overlay{position:fixed;inset:0;z-index:1800;display:grid;place-items:center;padding:20px;background:#0006}.dialog{width:min(500px,94vw);max-height:84vh;display:flex;flex-direction:column;gap:9px;padding:14px;background:#fff;border:1px solid #888;box-shadow:0 8px 26px #0004;box-sizing:border-box}.dialog header{display:flex;justify-content:space-between;align-items:center}.lookup-row{padding:9px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.lookup-row:hover{background:#fff2cc}.empty{padding:12px;text-align:center;color:#777}@media(max-width:650px){.employee-grid{grid-template-columns:92px minmax(0,1fr);grid-auto-rows:32px}.employee-name{grid-column:2;grid-row:2}.employee-grid label:nth-of-type(2){grid-column:1;grid-row:3}.employee-grid input:nth-of-type(3){grid-column:2;grid-row:3}.employee-grid label:nth-of-type(3){grid-column:1;grid-row:4}.employee-grid input:nth-of-type(4){grid-column:2;grid-row:4}.location-grid{grid-template-columns:100px minmax(0,1fr) 30px}}
</style>

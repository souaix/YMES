<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Employee = { userNo: string; userName: string; shiftNo: string }
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const stateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const fakePassword = 'DEMO-1234'
const employees: Employee[] = [
  { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY' },
  { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT' },
]
const employeeNo = ref('')
const password = ref('')
const passwordRequired = ref(true) // DEV mock for WIPChkPassword=1
const loadedEmployee = ref<Employee | null>(null)
const logoutDate = ref('')
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const selectedKeys = ref<string[]>([])
const errorText = ref('')
const confirmDialogOpen = ref(false)
const message = ref('目前顯示所有 DEV 假上工現況；輸入員工編號後按 Enter 篩選。')
const visibleRows = computed(() => loadedEmployee.value ? operatorStates.value.filter(item => item.userNo === loadedEmployee.value?.userNo) : operatorStates.value)
const selectedRows = computed(() => visibleRows.value.filter(item => selectedKeys.value.includes(keyFor(item))))

function nowLocal(): Date { return new Date() }
function dateTime(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function dateOnly(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())}`
}
function keyFor(state: OperatorState): string { return `${state.userNo}|${state.multiOperatorMode}|${state.loginPlaceNo}` }
function readMockStorage(): void {
  if (!dev.value) return
  try {
    const states = window.localStorage.getItem(stateKey)
    const logs = window.localStorage.getItem(logKey)
    const date = new Date()
    const yesterday = new Date(date); yesterday.setDate(yesterday.getDate() - 1)
    operatorStates.value = states ? JSON.parse(states) as OperatorState[] : [
      { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', loginDate: dateTime(new Date(Date.now() - 45 * 60_000)), workDate: dateOnly(date), opNo: 'DEV-OP-OLD-05', multiOperatorMode: 0, loginPlaceNo: 'DEV-OP-OLD-05' },
      { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT', loginDate: dateTime(new Date(Date.now() - 7 * 60 * 60_000)), workDate: dateOnly(yesterday), opNo: 'DEV-OP-EQP-30', multiOperatorMode: 2, loginPlaceNo: 'DEV-EQP-WASH-01' },
    ]
    operatorLogs.value = logs ? JSON.parse(logs) as OperatorLog[] : []
    selectedKeys.value = operatorStates.value.map(keyFor)
  } catch {
    operatorStates.value = []
    operatorLogs.value = []
    selectedKeys.value = []
  }
  logoutDate.value = ''
}
function saveMockStorage(): void {
  if (!dev.value) return
  window.localStorage.setItem(stateKey, JSON.stringify(operatorStates.value))
  window.localStorage.setItem(logKey, JSON.stringify(operatorLogs.value))
}
function clearLoadedEmployee(): void {
  loadedEmployee.value = null
  password.value = ''
  logoutDate.value = ''
}
function employeeEnter(): void {
  employeeNo.value = employeeNo.value.trim().toUpperCase()
  errorText.value = ''
  clearLoadedEmployee()
  if (!employeeNo.value) { errorText.value = 'EmployeeNo 不可空白。'; return }
  if (passwordRequired.value) {
    document.getElementById('wp105-password')?.focus()
    message.value = '請輸入 DEV 員工密碼後按 Enter。'
    return
  }
  loadEmployee()
}
function passwordEnter(): void {
  errorText.value = ''
  if (!employeeNo.value.trim()) { errorText.value = '請先輸入 EmployeeNo。'; return }
  if (password.value !== fakePassword) { errorText.value = 'DEV 假密碼不正確。'; clearLoadedEmployee(); return }
  loadEmployee()
}
function loadEmployee(): void {
  const employee = employees.find(item => item.userNo === employeeNo.value.trim().toUpperCase())
  if (!employee) { errorText.value = '找不到這筆 DEV 員工資料。'; clearLoadedEmployee(); return }
  loadedEmployee.value = employee
  selectedKeys.value = operatorStates.value.filter(item => item.userNo === employee.userNo).map(keyFor)
  logoutDate.value = dateTime(nowLocal()) // WinForms reads AP Server time; DEV uses local mock time.
  message.value = `${employee.userName} 已載入 ${selectedKeys.value.length} 筆 DEV 上工現況。`
  errorText.value = ''
}
function toggleSelection(state: OperatorState, checked: boolean): void {
  const key = keyFor(state)
  if (checked && !selectedKeys.value.includes(key)) selectedKeys.value = [...selectedKeys.value, key]
  else if (!checked) selectedKeys.value = selectedKeys.value.filter(item => item !== key)
}
function minutesBetween(start: string, end: string): number {
  const startDate = new Date(start.replace(/\//g, '-')).getTime()
  const endDate = new Date(end.replace(/\//g, '-')).getTime()
  return Math.max(0, Math.floor((endDate - startDate) / 60_000))
}
function requestClockOut(): void {
  errorText.value = ''
  if (!dev.value) { errorText.value = '目前只提供 DEV 假資料互動。'; return }
  if (!loadedEmployee.value) { errorText.value = '請先完成員工資料驗證。'; return }
  if (selectedRows.value.length === 0) { errorText.value = '請至少勾選一筆上工現況。'; return }
  confirmDialogOpen.value = true
}
function confirmClockOut(): void {
  confirmDialogOpen.value = false
  if (!dev.value || !loadedEmployee.value || selectedRows.value.length === 0) return
  const logoutAt = logoutDate.value || dateTime(nowLocal())
  const rowsToClose = [...selectedRows.value]
  // WinForms submits one selected row per Service call; earlier successful rows remain if a later row fails.
  for (const row of rowsToClose) {
    const current = operatorStates.value.find(item => keyFor(item) === keyFor(row))
    if (!current) continue
    operatorLogs.value.unshift({ ...current, logoutDate: logoutAt, workTimeMinutes: minutesBetween(current.loginDate, logoutAt) })
    operatorStates.value = operatorStates.value.filter(item => keyFor(item) !== keyFor(current))
  }
  saveMockStorage()
  const employee = loadedEmployee.value
  const completed = rowsToClose.length
  clearLoadedEmployee()
  employeeNo.value = ''
  selectedKeys.value = operatorStates.value.map(keyFor) // Form reloads with UserNo empty after success.
  message.value = `DEV 假下工完成：${employee.userNo}，${completed} 筆；沒有寫入正式系統。`
}

onMounted(readMockStorage)
</script>

<template>
  <section class="wp105-page" aria-label="使用者登出作業（下工）">
    <div v-if="!dev" class="mock-notice">此頁尚未連接 Production API；DEV 假資料操作僅供畫面與流程檢視。</div>
    <div v-else class="mock-notice">DEV 假資料：EmployeeNo <b>DEV-EMP-01</b>／<b>DEV-EMP-02</b>，Password <b>DEMO-1234</b>；不會寫入正式系統。</div>
    <div v-if="errorText" class="notice-error" role="alert">{{ errorText }}</div>

    <fieldset class="mes-frame identity-frame"><div class="identity-fields">
      <label for="wp105-employee">EmployeeNo</label><input id="wp105-employee" v-model="employeeNo" class="mes-input editable" autocomplete="off" :disabled="!dev" @blur="employeeNo = employeeNo.toUpperCase()" @keydown.enter.prevent="employeeEnter">
      <input class="mes-input readonly employee-name" :value="loadedEmployee?.userName || ''" aria-label="EmployeeName" readonly>
      <label v-if="passwordRequired" for="wp105-password">Password</label><input v-if="passwordRequired" id="wp105-password" v-model="password" class="mes-input editable" type="password" autocomplete="new-password" :disabled="!dev" @keydown.enter.prevent="passwordEnter">
      <label for="wp105-shift">ShiftNo</label><input id="wp105-shift" class="mes-input readonly" :value="loadedEmployee?.shiftNo || ''" readonly>
      <label for="wp105-date">LogoutDate</label><input id="wp105-date" class="mes-input readonly" :value="logoutDate" readonly>
    </div></fieldset>

    <div class="grid-frame"><div class="grid-title">Operator State</div><div class="table-scroll"><table class="state-grid"><thead><tr><th class="check-col">下工</th><th>ShiftNo</th><th>LoginDate</th><th>WorkDate</th><th>OPNo</th><th>LoginPlaceNo</th></tr></thead><tbody>
      <tr v-for="state in visibleRows" :key="keyFor(state)" :class="{ selected: selectedKeys.includes(keyFor(state)) }" @click="toggleSelection(state, !selectedKeys.includes(keyFor(state)))">
        <td class="check-col"><input type="checkbox" :checked="selectedKeys.includes(keyFor(state))" :disabled="!dev" :aria-label="`選取 ${state.loginPlaceNo} 下工`" @click.stop @change="toggleSelection(state, ($event.target as HTMLInputElement).checked)"></td><td>{{ state.shiftNo }}</td><td>{{ state.loginDate }}</td><td>{{ state.workDate }}</td><td>{{ state.opNo }}</td><td>{{ state.loginPlaceNo }}</td>
      </tr><tr v-if="visibleRows.length === 0"><td colspan="6" class="empty">{{ loadedEmployee ? '此員工目前沒有上工現況。' : '目前沒有 DEV 假上工現況。' }}</td></tr>
    </tbody></table></div><div class="statusbar">{{ message }} <span>Rows: {{ visibleRows.length }}</span><span>Selected: {{ selectedRows.length }}</span></div></div>

    <div class="bottom-row"><button type="button" class="mes-button confirm-button" :disabled="!dev" @click="requestClockOut">SignOut</button><button type="button" class="mes-button" @click="emit('close')">Close</button></div>
    <div v-if="confirmDialogOpen" class="overlay"><section class="dialog prompt-dialog" role="alertdialog" aria-modal="true" aria-label="確認下工"><strong>確認下工</strong><p>確定要將勾選的 {{ selectedRows.length }} 筆上工現況登出嗎？</p><footer><button type="button" class="mes-button" autofocus @click="confirmDialogOpen = false">Cancel</button><button type="button" class="mes-button confirm-button" @click="confirmClockOut">OK</button></footer></section></div>
  </section>
</template>

<style scoped>
.wp105-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.mock-notice{padding:8px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.notice-error{padding:7px 10px;border:1px solid #d8a7a2;background:#fff1ef;color:#8f261f}.mes-frame{min-width:0;margin:0;padding:10px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.identity-frame{min-height:165px}.identity-fields{display:grid;grid-template-columns:120px 150px 150px 1fr;grid-template-rows:30px 30px 30px 30px;align-items:center;gap:0 4px}.identity-fields label{text-align:right;padding-right:5px}.employee-name{grid-column:3;grid-row:1}.identity-fields label[for="wp105-password"]{grid-column:1;grid-row:2}.identity-fields #wp105-password{grid-column:2;grid-row:2}.identity-fields label[for="wp105-shift"]{grid-column:1;grid-row:3}.identity-fields #wp105-shift{grid-column:2;grid-row:3}.identity-fields label[for="wp105-date"]{grid-column:1;grid-row:4}.identity-fields #wp105-date{grid-column:2;grid-row:4}.mes-input{height:24px;min-width:0;border:1px solid #8c8c8c;padding:2px 6px;background:#fff;color:#333;box-sizing:border-box}.mes-input.editable{background:#fff3bd}.mes-input.readonly{background:#eee;color:#555}.mes-input:disabled{color:#777}.grid-frame{display:flex;flex:1;min-height:180px;flex-direction:column;border:1px solid #bdbdbd;background:#fff;padding:7px}.grid-title{height:24px;flex:0 0 24px;color:#555}.table-scroll{flex:1;min-height:0;overflow:auto;border:1px solid #b7b7b7}.state-grid{width:100%;border-collapse:collapse;white-space:nowrap}.state-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #b7b7b7;background:#dfeaf2;text-align:left;font-weight:600}.state-grid td{height:28px;padding:3px 8px;border:1px solid #c8c8c8;background:#fff3bd}.state-grid tbody tr{cursor:pointer}.state-grid tbody tr.selected td{background:#ffe7a0}.check-col{width:54px;text-align:center}.state-grid input{accent-color:#d38d22}.empty{text-align:center!important;color:#777!important;background:#fff!important;height:48px!important}.statusbar{min-height:25px;display:flex;align-items:center;gap:18px;padding:2px 4px;color:#555}.bottom-row{display:flex;justify-content:flex-end;gap:7px;padding:2px 0}.mes-button{min-width:88px;min-height:30px;padding:3px 14px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6;border-color:#8e8e8e}.mes-button:disabled{color:#999;cursor:default}.confirm-button{background:#f3a62a;color:white;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}@media(max-width:700px){.identity-fields{grid-template-columns:100px minmax(130px,1fr);grid-template-rows:30px 30px 30px 30px 30px}.employee-name{grid-column:2;grid-row:2}.identity-fields label[for="wp105-password"]{grid-column:1;grid-row:2}.identity-fields #wp105-password{grid-column:2;grid-row:3}.identity-fields label[for="wp105-shift"]{grid-column:1;grid-row:4}.identity-fields #wp105-shift{grid-column:2;grid-row:4}.identity-fields label[for="wp105-date"]{grid-column:1;grid-row:5}.identity-fields #wp105-date{grid-column:2;grid-row:5}}
</style>

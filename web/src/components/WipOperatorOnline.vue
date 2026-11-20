<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type PlaceField = 'opNo' | 'areaNo' | 'equipmentNo'
type OperatorState = { userNo: string; userName: string; shiftNo: string; loginDate: string; workDate: string; opNo: string; multiOperatorMode: number; loginPlaceNo: string }
type OperatorLog = OperatorState & { logoutDate: string; workTimeMinutes: number }

const props = defineProps<{ developmentPreview?: boolean; enableSignOut?: boolean; initialLotNo?: string; initialOpNo?: string; initialAreaNo?: string; initialEquipmentNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const editable = computed(() => dev.value && props.enableSignOut !== false)
const stateKey = 'ymes-dev-mock:wip-operator-clock-in:state:v1'
const logKey = 'ymes-dev-mock:wip-operator-clock-in:log:v1'
const placeFields: { key: PlaceField; label: string }[] = [
  { key: 'opNo', label: 'OP No' }, { key: 'areaNo', label: 'Area No' }, { key: 'equipmentNo', label: 'Equipment No' },
]
const places = reactive<Record<PlaceField, string>>({ opNo: '', areaNo: '', equipmentNo: '' })
const operatorStates = ref<OperatorState[]>([])
const operatorLogs = ref<OperatorLog[]>([])
const rows = ref<OperatorState[]>([])
const selected = ref<string[]>([])
const lookup = ref<PlaceField | null>(null)
const lookupDraft = ref<string[]>([])
const confirmOpen = ref(false)
const queried = ref(false)
const error = ref('')
const message = ref('輸入 OP／Area／Equipment No 後按 Query；可用 ... 多選。')
const lookupChoices = computed(() => {
  if (!lookup.value) return []
  const field = lookup.value
  const fromStates = operatorStates.value.filter(row => field === 'opNo' ? row.multiOperatorMode === 0 : field === 'areaNo' ? row.multiOperatorMode === 1 : row.multiOperatorMode === 2).map(row => row.loginPlaceNo)
  const fixtures = field === 'opNo' ? ['DEV-OP-OLD-05', 'DEV-OP-CUT-10', 'DEV-OP-AREA-20', 'DEV-OP-EQP-30'] : field === 'areaNo' ? ['DEV-AREA-01', 'DEV-AREA-02'] : ['DEV-EQP-WASH-01', 'DEV-EQP-CUT-01']
  return [...new Set([...fixtures, ...fromStates])].filter(Boolean).sort()
})
const selectedRows = computed(() => rows.value.filter(row => selected.value.includes(keyFor(row))))

function dateTime(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function dateOnly(date: Date): string { return dateTime(date).slice(0, 10) }
function keyFor(row: OperatorState): string { return `${row.userNo}|${row.multiOperatorMode}|${row.loginPlaceNo}` }
function seed(): OperatorState[] {
  const yesterday = new Date(); yesterday.setDate(yesterday.getDate() - 1)
  return [
    { userNo: 'DEV-EMP-01', userName: 'DEV 王小明', shiftNo: 'DEV-DAY', loginDate: dateTime(new Date(Date.now() - 45 * 60_000)), workDate: dateOnly(new Date()), opNo: 'DEV-OP-OLD-05', multiOperatorMode: 0, loginPlaceNo: 'DEV-OP-OLD-05' },
    { userNo: 'DEV-EMP-02', userName: 'DEV 陳小華', shiftNo: 'DEV-NIGHT', loginDate: dateTime(new Date(Date.now() - 7 * 60 * 60_000)), workDate: dateOnly(yesterday), opNo: 'DEV-OP-EQP-30', multiOperatorMode: 2, loginPlaceNo: 'DEV-EQP-WASH-01' },
  ]
}
function load(): void {
  if (!dev.value) return
  try {
    const states = window.localStorage.getItem(stateKey)
    const logs = window.localStorage.getItem(logKey)
    const loadedStates = states ? JSON.parse(states) as unknown : seed()
    const loadedLogs = logs ? JSON.parse(logs) as unknown : []
    operatorStates.value = Array.isArray(loadedStates) ? loadedStates as OperatorState[] : []
    operatorLogs.value = Array.isArray(loadedLogs) ? loadedLogs as OperatorLog[] : []
  } catch { operatorStates.value = []; operatorLogs.value = []; error.value = 'DEV 假資料無法讀取。' }
}
function tokens(value: string): string[] { return [...new Set(value.split(',').map(item => item.trim().toUpperCase()).filter(Boolean))] }
function query(): void {
  error.value = ''
  if (!dev.value) { error.value = '此頁目前只提供 DEV 假資料查詢。'; return }
  load()
  for (const field of placeFields) places[field.key] = tokens(places[field.key]).join(',')
  const allPlaces = new Set(placeFields.flatMap(field => tokens(places[field.key])))
  if (props.initialLotNo?.trim()) allPlaces.add(props.initialLotNo.trim().toUpperCase())
  rows.value = operatorStates.value.filter(row => !allPlaces.size || allPlaces.has(row.loginPlaceNo.toUpperCase()))
    .sort((a, b) => a.loginDate.localeCompare(b.loginDate) || a.userNo.localeCompare(b.userNo) || a.multiOperatorMode - b.multiOperatorMode)
  selected.value = []; queried.value = true; message.value = `查得 ${rows.value.length} 筆 DEV 上工現況。`
}
function openLookup(field: PlaceField): void { lookup.value = field; lookupDraft.value = tokens(places[field]) }
function toggleLookup(value: string): void { lookupDraft.value = lookupDraft.value.includes(value) ? lookupDraft.value.filter(item => item !== value) : [...lookupDraft.value, value] }
function applyLookup(): void { if (!lookup.value) return; places[lookup.value] = lookupDraft.value.join(','); lookup.value = null }
function toggle(row: OperatorState): void { const key = keyFor(row); selected.value = selected.value.includes(key) ? selected.value.filter(item => item !== key) : [...selected.value, key] }
function requestSignOut(): void {
  error.value = ''
  if (!editable.value) { error.value = '此頁目前只提供 DEV 假資料。'; return }
  if (!selectedRows.value.length) { error.value = '請先勾選上工現況。'; return }
  confirmOpen.value = true
}
function minutesBetween(start: string, end: string): number {
  const minutes = (new Date(end.replace(/\//g, '-')).getTime() - new Date(start.replace(/\//g, '-')).getTime()) / 60_000
  return Number.isFinite(minutes) ? Math.max(0, Math.floor(minutes)) : 0
}
function confirmSignOut(): void {
  confirmOpen.value = false; error.value = ''
  if (!editable.value) return
  const targets = [...selectedRows.value]
  let completed = 0
  for (const row of targets) {
    const current = operatorStates.value.find(item => keyFor(item) === keyFor(row))
    if (!current) { error.value = `找不到 ${row.userNo}／${row.loginPlaceNo} 的最新上工資料；已完成 ${completed} 筆。`; break }
    const logoutDate = dateTime(new Date())
    const nextStates = operatorStates.value.filter(item => keyFor(item) !== keyFor(row))
    const nextLogs = [{ ...current, logoutDate, workTimeMinutes: minutesBetween(current.loginDate, logoutDate) }, ...operatorLogs.value]
    let previousLogs: string | null = null
    try {
      previousLogs = window.localStorage.getItem(logKey)
      window.localStorage.setItem(logKey, JSON.stringify(nextLogs))
      window.localStorage.setItem(stateKey, JSON.stringify(nextStates))
    } catch {
      try { if (previousLogs === null) window.localStorage.removeItem(logKey); else window.localStorage.setItem(logKey, previousLogs) } catch { /* DEV storage rollback is best effort. */ }
      error.value = `DEV 資料儲存失敗；前 ${completed} 筆已完成。`; break
    }
    operatorStates.value = nextStates; operatorLogs.value = nextLogs; completed += 1
  }
  const failure = error.value
  query()
  if (failure) error.value = failure
  if (completed) message.value = `DEV 已下工 ${completed} 筆；不會寫入正式 MES。`
}
onMounted(() => {
  load()
  places.opNo = props.initialOpNo || ''
  places.areaNo = props.initialAreaNo || ''
  places.equipmentNo = props.initialEquipmentNo || ''
  if (props.initialLotNo?.trim() || places.opNo || places.areaNo || places.equipmentNo) query()
})
watch(dev, enabled => { if (enabled) load(); else { rows.value = []; selected.value = [] } })
</script>

<template>
  <section class="online-page">
    <h1>使用者登入(上工)資訊 <small>OPERATOR ONLINE · WP106</small></h1>
    <p class="note">DEV 假資料；與 WP104／WP105 共用本機上工現況。SignOut 不會送往正式 MES。</p>
    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <fieldset class="query-box"><legend>Query</legend><div class="query-grid">
      <template v-for="field in placeFields" :key="field.key"><label :for="`wp106-${field.key}`">{{ field.label }}</label><input :id="`wp106-${field.key}`" v-model="places[field.key]" class="yellow" :disabled="!editable" @keydown.enter.prevent="query"><button v-if="enableSignOut !== false" type="button" class="button lookup-button" :disabled="!editable" @click="openLookup(field.key)">...</button><span v-else></span></template>
      <button v-if="enableSignOut !== false" type="button" class="button query-button" :disabled="!editable" @click="query">Query</button>
    </div></fieldset>
    <fieldset class="result-box"><legend>Operator State</legend><div class="scroll"><table><thead><tr><th v-if="enableSignOut !== false">CheckFlag</th><th>UserNo</th><th>UserName</th><th>ShiftNo</th><th>LoginDate</th><th>MultiOperatorMode</th><th>LoginPlaceNo</th></tr></thead><tbody>
      <tr v-for="row in rows" :key="keyFor(row)" :class="{ selected: selected.includes(keyFor(row)) }" @click="editable && toggle(row)"><td v-if="enableSignOut !== false"><input type="checkbox" :checked="selected.includes(keyFor(row))" :disabled="!editable" :aria-label="`選取 ${row.userNo} ${row.loginPlaceNo}`" @click.stop @change="toggle(row)"></td><td>{{ row.userNo }}</td><td>{{ row.userName }}</td><td>{{ row.shiftNo }}</td><td>{{ row.loginDate }}</td><td>{{ row.multiOperatorMode }}</td><td>{{ row.loginPlaceNo }}</td></tr>
      <tr v-if="!rows.length"><td :colspan="enableSignOut === false ? 6 : 7" class="empty">{{ queried ? '沒有符合條件的上工現況。' : '請先 Query。' }}</td></tr>
    </tbody></table></div><div class="status"><span>{{ message }}</span><span>Rows: {{ rows.length }} · Selected: {{ selectedRows.length }}</span></div></fieldset>
    <div class="bottom"><button v-if="enableSignOut !== false" type="button" class="button primary" :disabled="!editable" @click="requestSignOut">SignOut</button><button type="button" class="button" @click="emit('close')">Close</button></div>
    <div v-if="lookup" class="overlay"><div class="dialog" role="dialog" aria-modal="true"><h2>{{ lookup }} 多選</h2><div class="lookup-list"><label v-for="value in lookupChoices" :key="value"><input type="checkbox" :checked="lookupDraft.includes(value)" @change="toggleLookup(value)"> {{ value }}</label></div><div class="bottom"><button type="button" class="button primary" @click="applyLookup">OK</button><button type="button" class="button" @click="lookup = null">Cancel</button></div></div></div>
    <div v-if="confirmOpen" class="overlay"><div class="dialog" role="alertdialog" aria-modal="true"><h2>SignOut</h2><p>確定將勾選的 {{ selectedRows.length }} 筆上工現況登出？原 WinForms 逐筆呼叫 Service。</p><div class="bottom"><button type="button" class="button" @click="confirmOpen = false">Cancel</button><button type="button" class="button primary" @click="confirmSignOut">OK</button></div></div></div>
  </section>
</template>

<style scoped>
.online-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.note,.error{margin:0;padding:7px;border:1px solid #d8c994;background:#fff8dc}.error{border-color:#d8a7a2;background:#fff1ef;color:#8f261f}fieldset{min-width:0;margin:0;padding:8px;border:1px solid #bdbdbd;background:#fff}legend{padding:0 5px}.query-box{min-height:120px}.query-grid{display:grid;grid-template-columns:120px 200px 30px 1fr;grid-template-rows:repeat(3,28px);align-items:center;gap:2px 4px}.query-grid label{text-align:right;padding-right:5px}.query-grid input{height:23px;min-width:0;box-sizing:border-box;border:1px solid #8c8c8c;padding:2px 5px}.yellow{background:#fff0b3}.query-grid input:disabled{background:#eee}.button{min-height:27px;padding:3px 10px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit;cursor:pointer}.button:disabled{color:#999;cursor:default}.lookup-button{padding:0;width:26px;min-height:24px}.query-button{grid-column:4;grid-row:3;justify-self:end;min-width:80px}.result-box{display:flex;flex:1;min-height:200px;flex-direction:column}.scroll{flex:1;min-height:0;overflow:auto;border:1px solid #aaa}table{width:100%;min-width:750px;border-collapse:collapse;white-space:nowrap}th,td{padding:5px 8px;border:1px solid #ccc;text-align:left}th{position:sticky;top:0;background:#ead591;font-weight:500}td{background:#ffe89c}tr.selected td{background:#ffda76}tr:not(.empty){cursor:pointer}.empty{text-align:center;background:#fff;color:#777;height:35px}.status{display:flex;justify-content:space-between;padding-top:5px;color:#666}.bottom{display:flex;justify-content:flex-end;gap:7px}.bottom .button{min-width:80px}.primary{background:#f3a62a;border-color:#e99512;color:#fff}.overlay{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;padding:12px;background:#0007}.dialog{width:min(470px,95vw);max-height:85vh;overflow:auto;padding:13px;background:#f7f5ee;border:1px solid #888;box-shadow:0 8px 28px #0005}.dialog h2{margin:0 0 8px}.lookup-list{max-height:260px;overflow:auto;display:flex;flex-direction:column;gap:7px;margin-bottom:10px}@media(max-width:750px){.query-grid{grid-template-columns:90px minmax(130px,1fr) 30px 60px}}
</style>

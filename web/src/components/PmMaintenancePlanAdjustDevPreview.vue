<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface Equipment { equipmentNo: string; equipmentName: string; equipmentType: string }
interface Frequency { frequencyNo: string; frequencyName: string; priority: number }
interface PlanRow {
  serialNo: string; pmSerialNo: number; equipmentNo: string; pmeqpSopNo: string; frequencyNo: string; sopNo: string
  estimatedDate: string; executeState: number; creator: string; createDate: string; reviser?: string; reviseDate?: string
}
interface PlanLogRow extends PlanRow { logCreator: string; logCreateDate: string }
interface PlanState { version: 1; records: Array<Record<string, unknown>>; plans: PlanRow[]; planLogs: PlanLogRow[] }
interface YearChoice { yearNo: string; startDate: string; endDate: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-type-sop-basis.v1'
const currentUser = 'DEV01'
const today = new Date()
const yearChoices: YearChoice[] = [2025, 2026, 2027, 2028].map((year) => ({ yearNo: String(year), startDate: `${year}-01-01`, endDate: `${year}-12-31` }))
const equipmentChoices: Equipment[] = [
  { equipmentNo: 'CNC-02', equipmentName: 'CNC 加工中心 02', equipmentType: 'CNC-200' },
  { equipmentNo: 'PUMP-07', equipmentName: '製程冷卻泵 07', equipmentType: 'PUMP-120' },
  { equipmentNo: 'OVEN-05', equipmentName: '真空烘箱 05', equipmentType: 'OVEN-50' },
  { equipmentNo: 'CNC-02-CH1', equipmentName: 'CNC 02 腔體 1', equipmentType: 'CNC-200' },
]
const fallbackFrequencies: Frequency[] = [
  { frequencyNo: 'PM-DAILY-01', frequencyName: '每日點檢', priority: 1 },
  { frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', priority: 3 },
  { frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', priority: 5 },
  { frequencyNo: 'PM-YEARLY-06', frequencyName: '年度保養', priority: 8 },
]

function seedState(): PlanState {
  return {
    version: 1,
    records: [
      { pmeqpSopNo: 'EQPSOP2600001', pmYearNo: '2026', equipmentType: 'CNC-200', equipmentTypeName: 'CNC 加工中心', frequencyNo: 'PM-MONTHLY-15', frequencyName: '月中設備校驗', sopNo: 'SOP-CAL-02', sopName: '感測器校正程序', toleranceQty: 0, autoCreateWo: true, createAndReleaseWo: false, beforehandDay: 3, lockEquipment: true, equipmentState: 3, stateName: '維修中', autoEmail: true, hurryTime: 0, overDueTime: 0, description: '依設備類型與年度頻率建立保養標準。', creator: 'PMUSER', createDate: '2026-01-08 09:30', reviser: 'PMUSER', reviseDate: '2026-03-04 15:20', dataStamp: 2 },
      { pmeqpSopNo: 'EQPSOP2600002', pmYearNo: '2026', equipmentType: 'PUMP-120', equipmentTypeName: '製程冷卻泵', frequencyNo: 'PM-WEEKLY-01', frequencyName: '每週保養', sopNo: 'SOP-LUBE-03', sopName: '主軸潤滑保養程序', toleranceQty: 0, autoCreateWo: false, createAndReleaseWo: false, beforehandDay: null, lockEquipment: false, equipmentState: null, stateName: '', autoEmail: false, hurryTime: 0, overDueTime: 0, description: '週保養由現場排程人員確認。', creator: 'DEV02', createDate: '2026-02-10 11:15', reviser: '', reviseDate: '', dataStamp: 0 },
    ],
    plans: [
      { serialNo: 'DEV-PLAN-0001', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 1, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', sopNo: 'SOP-CAL-02', estimatedDate: '2026-11-15', executeState: 1, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { serialNo: 'DEV-PLAN-0002', pmeqpSopNo: 'EQPSOP2600001', pmSerialNo: 2, equipmentNo: 'CNC-02', frequencyNo: 'PM-MONTHLY-15', sopNo: 'SOP-CAL-02', estimatedDate: '2026-12-15', executeState: 0, creator: 'PMUSER', createDate: '2026-01-08 09:30' },
      { serialNo: 'DEV-PLAN-0003', pmeqpSopNo: 'EQPSOP2600002', pmSerialNo: 1, equipmentNo: 'PUMP-07', frequencyNo: 'PM-WEEKLY-01', sopNo: 'SOP-LUBE-03', estimatedDate: '2026-11-30', executeState: 0, creator: 'DEV02', createDate: '2026-02-10 11:15' },
    ],
    planLogs: [],
  }
}
function loadState(): PlanState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as PlanState | null
    if (parsed?.version === 1 && Array.isArray(parsed.records) && Array.isArray(parsed.plans) && Array.isArray(parsed.planLogs)) return parsed
  } catch { /* Keep the DEV preview available if browser storage is disabled. */ }
  return seedState()
}
function readFrequencies(): Frequency[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem('ymes.dev.pm.frequency-basis.v1') ?? 'null') as { records?: Array<Record<string, unknown>> } | null
    if (Array.isArray(parsed?.records)) {
      const rows = parsed.records.filter((row) => Number(row.issueState) === 2).map((row) => ({ frequencyNo: String(row.frequencyNo ?? ''), frequencyName: String(row.frequencyName ?? ''), priority: Number(row.priority ?? 0) })).filter((row) => row.frequencyNo)
      if (rows.length) return rows
    }
  } catch { /* Use sample frequency labels when the frequency master has not been saved. */ }
  return fallbackFrequencies
}

const state = ref<PlanState>(loadState())
const frequencies = ref(readFrequencies())
const yearText = ref(String(today.getFullYear()))
const monthText = ref(String(today.getMonth() + 1))
const equipmentText = ref('CNC-02')
const query = ref({ year: today.getFullYear(), month: today.getMonth() + 1, equipmentNo: 'CNC-02' })
const selectedDay = ref(today.getDate())
const selectedSerialNo = ref('')
const changeDate = ref('')
const dialogOpen = ref(false)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const yearOptions = computed(() => yearChoices)
const yearRange = computed(() => yearChoices.find((row) => Number(row.yearNo) === query.value.year) ?? null)
const equipment = computed(() => equipmentChoices.find((row) => row.equipmentNo === query.value.equipmentNo) ?? null)
const displayedPlans = computed(() => state.value.plans.filter((row) => row.equipmentNo === query.value.equipmentNo).sort((a, b) => a.pmSerialNo - b.pmSerialNo))
const monthPlans = computed(() => displayedPlans.value.filter((row) => {
  const date = new Date(`${row.estimatedDate}T00:00:00`)
  return date.getFullYear() === query.value.year && date.getMonth() + 1 === query.value.month
}))
const plansByDate = computed(() => monthPlans.value.reduce<Record<string, PlanRow[]>>((byDate, row) => {
  ;(byDate[row.estimatedDate] ??= []).push(row)
  return byDate
}, {}))
const monthCells = computed(() => {
  const first = new Date(query.value.year, query.value.month - 1, 1)
  const days = new Date(query.value.year, query.value.month, 0).getDate()
  const leading = first.getDay()
  const count = Math.ceil((leading + days) / 7) * 7
  return Array.from({ length: count }, (_, index) => { const day = index - leading + 1; return day > 0 && day <= days ? day : null })
})
const monthLabel = computed(() => `${query.value.year}/${String(query.value.month).padStart(2, '0')}`)
const activePlans = computed(() => plansByDate.value[dateKey(query.value.year, query.value.month, selectedDay.value)] ?? [])
const selectedPlan = computed(() => state.value.plans.find((row) => row.serialNo === selectedSerialNo.value) ?? null)
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory if storage is unavailable. */ } }, { deep: true })
watch(activePlans, (rows) => { if (!rows.some((row) => row.serialNo === selectedSerialNo.value)) selectedSerialNo.value = rows[0]?.serialNo ?? '' }, { immediate: true })

function dateKey(year: number, month: number, day: number) { return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}` }
function frequencyLabel(no: string) { const row = frequencies.value.find((value) => value.frequencyNo === no); return row ? `${row.frequencyName} (${row.priority})` : no }
function getPlansForDay(day: number | null) { return day == null ? [] : plansByDate.value[dateKey(query.value.year, query.value.month, day)] ?? [] }
function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function normalizeYear(raw: string): number | null {
  const value = raw.trim()
  if (!/^\d{1,4}$/.test(value)) return null
  const year = Number(value)
  return value.length < 4 ? year + 1911 : year
}
function showPlans() {
  const year = normalizeYear(yearText.value)
  const month = Number(monthText.value)
  const equipmentNo = equipmentText.value.trim().toUpperCase()
  if (year == null || year < 1 || yearText.value.trim().length > 4 || !Number.isInteger(month) || month < 1 || month > 12) return toast('請輸入有效年度與月份（1–12）。', 'error')
  if (!equipmentNo) return toast('Equipment No 不可空白。', 'error')
  query.value = { year, month, equipmentNo }
  yearText.value = String(year)
  monthText.value = String(month)
  equipmentText.value = equipmentNo
  selectedDay.value = 1
  toast(`${equipmentNo} ${year}/${String(month).padStart(2, '0')} 計畫已載入。`)
}
function changeMonth(step: number) {
  const date = new Date(query.value.year, query.value.month - 1 + step, 1)
  query.value = { ...query.value, year: date.getFullYear(), month: date.getMonth() + 1 }
  yearText.value = String(query.value.year)
  monthText.value = String(query.value.month)
  selectedDay.value = 1
}
function selectDay(day: number | null) { if (day != null) selectedDay.value = day }
function openAdjust(day: number | null) {
  if (day == null) return
  selectedDay.value = day
  const plans = getPlansForDay(day)
  if (!plans.length) return toast('所選日期沒有保養計畫。', 'error')
  const missing = plans.find((row) => !frequencies.value.some((frequency) => frequency.frequencyNo === row.frequencyNo))
  if (missing) return toast(`頻率 ${missing.frequencyNo} 不在已生效的頻率資料中，無法開啟調整。`, 'error')
  selectedSerialNo.value = plans[0].serialNo
  changeDate.value = plans[0].estimatedDate
  dialogOpen.value = true
}
function selectPlan(serialNo: string) {
  selectedSerialNo.value = serialNo
  const row = state.value.plans.find((item) => item.serialNo === serialNo)
  if (row) changeDate.value = row.estimatedDate
}
function saveAdjustedDate() {
  const row = selectedPlan.value
  const next = state.value.plans.find((item) => item.serialNo === selectedSerialNo.value)
  if (!row || !next) return toast('找不到要調整的保養計畫。', 'error')
  if (!/^\d{4}-\d{2}-\d{2}$/.test(changeDate.value)) return toast('請輸入有效的保養日期。', 'error')
  const [year, month, day] = changeDate.value.split('-').map(Number)
  const parsed = new Date(year, month - 1, day)
  if (parsed.getFullYear() !== year || parsed.getMonth() + 1 !== month || parsed.getDate() !== day) return toast('請輸入有效的保養日期。', 'error')
  next.estimatedDate = changeDate.value
  next.reviser = currentUser
  next.reviseDate = nowLabel.value
  dialogOpen.value = false
  toast(`已將 ${frequencyLabel(next.frequencyNo)} 調整至 ${changeDate.value}。`)
}
function cancelPlan() {
  const row = selectedPlan.value
  if (!row || !window.confirm(`確定取消 ${row.equipmentNo} ${row.estimatedDate} 的 ${frequencyLabel(row.frequencyNo)} 計畫？`)) return
  state.value.planLogs.push({ ...row, executeState: 0, logCreator: currentUser, logCreateDate: nowLabel.value })
  state.value.plans = state.value.plans.filter((item) => item.serialNo !== row.serialNo)
  dialogOpen.value = false
  selectedSerialNo.value = ''
  toast('計畫已移入 DEV 歷史，ExecuteState=0（取消）。')
}
</script>

<template>
  <section class="pm-plan-adjust" aria-label="保養計劃變更 DEV 預覽">
    <header class="page-header"><div><p>設備 PM 設定／PM PLAN CHANGE</p><h1>保養計劃變更</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="secondary" @click="emit('close')">Close (X)</button></div></header>
    <div class="query-panel"><label>年度 <input v-model="yearText" list="pm-plan-year-options" inputmode="numeric" maxlength="4"><datalist id="pm-plan-year-options"><option v-for="year in yearOptions" :key="year.yearNo" :value="year.yearNo"></option></datalist></label><label>月份 <input v-model="monthText" type="number" min="1" max="12"></label><label class="equipment-label">Equipment No <input v-model="equipmentText" list="pm-plan-equipment-options" autocomplete="off"><datalist id="pm-plan-equipment-options"><option v-for="row in equipmentChoices" :key="row.equipmentNo" :value="row.equipmentNo">{{ row.equipmentName }}</option></datalist></label><button type="button" @click="showPlans">Show</button></div>
    <div class="range-line"><span>年度區間</span><strong>{{ yearRange?.startDate ?? `${query.year}-01-01` }} — {{ yearRange?.endDate ?? `${query.year}-12-31` }}</strong><span class="equipment-caption">{{ equipment?.equipmentName ?? query.equipmentNo }}｜{{ equipment?.equipmentType ?? 'DEV 設備' }}</span></div>
    <main class="calendar-panel"><header class="calendar-header"><button type="button" class="secondary" aria-label="上一個月" @click="changeMonth(-1)">‹</button><h2>{{ monthLabel }} <small>{{ query.equipmentNo }}</small></h2><button type="button" class="secondary" aria-label="下一個月" @click="changeMonth(1)">›</button><span class="legend"><i></i>有保養計畫</span></header>
      <div class="calendar-grid"><div v-for="dayName in ['日','一','二','三','四','五','六']" :key="dayName" class="weekday">星期{{ dayName }}</div><button v-for="(day, index) in monthCells" :key="`${query.year}-${query.month}-${index}`" type="button" class="day-cell" :class="{ blank: day == null, planned: day != null && getPlansForDay(day).length, selected: day === selectedDay }" :disabled="day == null" @click="selectDay(day)" @dblclick="openAdjust(day)"><span v-if="day != null" class="day-number">{{ day }}</span><span v-for="plan in getPlansForDay(day)" :key="plan.serialNo" class="frequency-chip">{{ frequencyLabel(plan.frequencyNo) }}</span><small v-if="day != null && getPlansForDay(day).length > 1">{{ getPlansForDay(day).length }} 筆｜雙擊調整</small><small v-else-if="day != null && getPlansForDay(day).length">雙擊調整</small></button></div>
      <div class="selected-day"><div><span>所選日期</span><strong>{{ dateKey(query.year, query.month, selectedDay) }}</strong><span>{{ activePlans.length }} 筆計畫</span></div><button type="button" :disabled="!activePlans.length" @click="openAdjust(selectedDay)">調整所選日計畫</button></div>
      <div class="plan-list"><div v-for="plan in activePlans" :key="plan.serialNo" class="plan-row"><strong>{{ frequencyLabel(plan.frequencyNo) }}</strong><span>{{ plan.sopNo }}</span><span>PM Serial {{ plan.pmSerialNo }}</span><span>設備 {{ plan.equipmentNo }}</span><span>執行狀態 {{ plan.executeState }}</span><span>{{ plan.estimatedDate }}</span></div><p v-if="activePlans.length === 0" class="empty">所選日期沒有計畫；使用上一月／下一月或 Show 查詢其他日期。</p></div>
    </main>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>
    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>Frequency priority 排序｜開啟月曆時會載入該設備全部計畫後於本機按月篩選</span></footer>

    <div v-if="dialogOpen && selectedPlan" class="modal-backdrop" @click.self="dialogOpen = false"><section class="dialog" role="dialog" aria-modal="true" aria-labelledby="plan-adjust-title"><header><div><p>PM PLAN CHANGE / {{ selectedPlan.serialNo }}</p><h2 id="plan-adjust-title">調整或取消保養計畫</h2></div><button type="button" class="secondary close-x" aria-label="關閉" @click="dialogOpen = false">×</button></header><div class="plan-context"><span>設備</span><strong>{{ selectedPlan.equipmentNo }}</strong><span>頻率</span><strong>{{ frequencyLabel(selectedPlan.frequencyNo) }}</strong><span>SOP</span><strong>{{ selectedPlan.sopNo }}</strong><span>PM Serial</span><strong>{{ selectedPlan.pmSerialNo }}</strong></div><label>保養日期 <input v-model="changeDate" type="date"></label><p class="form-note">同一日期若有多筆計畫，先選擇要處理的頻率。調整只修改該 SerialNo 的 PMEstDateTime，沒有 DataStamp。</p><label>日期上的計畫 <select :value="selectedSerialNo" @change="selectPlan(($event.target as HTMLSelectElement).value)"><option v-for="plan in activePlans" :key="plan.serialNo" :value="plan.serialNo">{{ frequencyLabel(plan.frequencyNo) }} — {{ plan.sopNo }} / #{{ plan.pmSerialNo }}</option></select></label><footer><button type="button" class="danger" @click="cancelPlan">取消此計畫</button><span class="footer-gap"></span><button type="button" class="secondary" @click="dialogOpen = false">關閉</button><button type="button" @click="saveAdjustedDate">確認調整</button></footer></section></div>
  </section>
</template>

<style scoped>
.pm-plan-adjust { height:100%; min-height:0; display:flex; flex-direction:column; gap:9px; padding:12px; box-sizing:border-box; background:#f7f5ee; color:#26251f; font:12px/1.42 "Segoe UI","Microsoft JhengHei",sans-serif; }
.page-header,.header-actions,.query-panel,.range-line,.calendar-header,.selected-day,.plan-row,.page-footer,.dialog header,.dialog footer { display:flex; align-items:center; }.page-header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.page-header p,.dialog header p { margin:0; color:#777366; font-size:10px; letter-spacing:.06em; }h1 { margin:2px 0 0; font-size:20px; font-weight:600; }.header-actions { gap:9px; }.dev-tag { padding:3px 8px; border-radius:3px; background:#dce9de; color:#28533a; font-size:10px; }
button { min-height:28px; padding:4px 10px; border:1px solid #454237; border-radius:3px; background:#454237; color:#fff; font:inherit; cursor:pointer; }button:disabled { opacity:.42; cursor:default; }button.secondary { border-color:#bab6a8; background:#fffef9; color:#333126; }button.danger { border-color:#a34a38; background:#a34a38; }
.query-panel { gap:9px; flex-wrap:wrap; padding:9px; border:1px solid #d0ccbf; background:#eeece4; }.query-panel label,.dialog label { display:grid; gap:3px; color:#514f45; }.query-panel input,.dialog input,.dialog select { min-height:29px; padding:4px 7px; box-sizing:border-box; border:1px solid #bbb7a9; border-radius:3px; background:#fffef9; color:#26251f; font:inherit; }.query-panel input { width:96px; }.query-panel .equipment-label input { width:190px; }.range-line { gap:8px; color:#777366; }.range-line strong { color:#454237; }.equipment-caption { margin-left:auto; }
.calendar-panel { min-height:0; flex:1; display:flex; flex-direction:column; gap:8px; padding:9px; border:1px solid #d0ccbf; background:#fffef9; }.calendar-header { gap:8px; }.calendar-header h2 { margin:0; min-width:185px; text-align:center; font-size:17px; }.calendar-header h2 small { display:block; color:#777366; font-size:11px; font-weight:400; }.legend { display:flex; align-items:center; gap:5px; margin-left:auto; color:#777366; }.legend i { width:11px; height:11px; background:#c95735; border-radius:2px; }.calendar-grid { flex:1; min-height:250px; display:grid; grid-template-columns:repeat(7,minmax(0,1fr)); grid-auto-rows:minmax(48px,1fr); gap:3px; }.weekday { display:grid; place-items:center; background:#e9e6dc; color:#555246; font-weight:600; }.day-cell { min-width:0; display:flex; flex-direction:column; align-items:flex-start; gap:2px; padding:4px; border-color:#ddd9ce; background:#fffef9; color:#333126; text-align:left; white-space:normal; overflow:hidden; }.day-cell:hover:not(:disabled) { background:#f5f2e8; }.day-cell.planned { border-color:#c95735; background:#f8e7df; }.day-cell.selected { outline:2px solid #647985; outline-offset:-2px; }.day-cell.blank { min-height:0; border:0; background:#f5f3ed; }.day-number { align-self:flex-end; font-weight:600; }.frequency-chip { width:100%; overflow:hidden; padding:2px 4px; border-radius:2px; background:#c95735; color:white; font-size:10px; text-overflow:ellipsis; white-space:nowrap; }.day-cell small { color:#777366; font-size:9px; }.selected-day { justify-content:space-between; gap:10px; padding:7px 9px; border-top:1px solid #e2dfd5; }.selected-day div { display:flex; align-items:center; gap:10px; }.selected-day div span { color:#777366; }.selected-day strong { color:#454237; }.plan-list { display:grid; gap:4px; max-height:115px; overflow:auto; }.plan-row { gap:12px; flex-wrap:wrap; padding:5px 8px; border:1px solid #e0ddd4; background:#f8f7f1; }.plan-row strong { min-width:135px; }.plan-row span { color:#777366; }.empty { margin:0; padding:14px; text-align:center; color:#777366; }.toast { margin:0; padding:7px 9px; border-radius:3px; background:#e4eee4; color:#28533a; }.toast.error { background:#f4e5df; color:#8a3527; }.page-footer { justify-content:space-between; gap:8px; color:#8a877c; font-size:10px; }
.modal-backdrop { position:fixed; inset:0; z-index:120; display:grid; place-items:center; padding:16px; background:#28271f80; }.dialog { width:min(540px,100%); max-height:calc(100vh - 32px); overflow:auto; display:grid; gap:10px; padding:14px; border:1px solid #b8b4a7; border-radius:5px; background:#f7f5ee; box-shadow:0 18px 60px #0004; }.dialog header { justify-content:space-between; border-bottom:1px solid #d6d2c6; padding-bottom:8px; }.dialog h2 { margin:2px 0 0; font-size:17px; }.close-x { min-width:30px; font-size:18px; }.plan-context { display:grid; grid-template-columns:110px 1fr; gap:5px 9px; padding:8px; background:#fffef9; }.plan-context span { color:#777366; }.form-note { margin:0; padding:6px 8px; background:#eeece4; color:#777366; font-size:11px; }.dialog footer { gap:6px; }.footer-gap { flex:1; }
@media(max-width:760px) { .pm-plan-adjust { padding:7px; }.range-line,.page-footer { align-items:flex-start; flex-direction:column; }.equipment-caption { margin-left:0; }.calendar-grid { grid-auto-rows:minmax(40px,1fr); }.frequency-chip { font-size:8px; }.plan-row { gap:5px 9px; }.selected-day div { align-items:flex-start; flex-direction:column; gap:2px; } }
</style>
